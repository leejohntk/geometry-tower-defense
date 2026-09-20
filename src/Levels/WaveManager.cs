using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Manages wave progression, enemy spawning, and wave lifecycle.
/// Uses a Timer node for spawn timing instead of delta accumulation.
/// Uses an ObjectPool for enemy instances.
/// Wave composition and enemy types come from a LevelDefinition.
/// </summary>
public partial class WaveManager : Node
{
    // Signal emitted when a new wave begins
    [Signal]
    public delegate void WaveStartedEventHandler(int waveNumber);

    // Signal emitted when an enemy is spawned during a wave, along with the
    // route index the enemy follows (round-robin across the level's routes).
    [Signal]
    public delegate void EnemySpawnedEventHandler(Enemy enemy, int routeIndex);

    // Signal emitted when all enemies in current wave are dead
    [Signal]
    public delegate void WaveCompletedEventHandler(int waveNumber);

    // Signal emitted when all waves are completed (victory)
    [Signal]
    public delegate void AllWavesCompletedEventHandler();

    private int _currentWave = 0;
    private int _enemiesAliveThisWave = 0;
    private bool _isSpawning = false;

    // Round-robin counter for assigning routes to spawn events. Persists across
    // waves so the assignment keeps alternating without a per-wave reset.
    private int _routeCounter = 0;

    private LevelDefinition? _level;
    private readonly Queue<SpawnKind> _pendingSpawns = new();

    // Enemy pool (set by GameManager)
    private ObjectPool<Enemy>? _enemyPool;

    // Timer-based spawn system (set up in InitTimer)
    private Timer? _spawnTimer;

    /// <summary>
    /// Current wave number (1-based).
    /// </summary>
    public int CurrentWave => _currentWave;

    /// <summary>
    /// Total number of waves in the current level.
    /// </summary>
    public int TotalWaves => _level?.Waves.Count ?? 0;

    /// <summary>
    /// True if a wave is currently in progress (spawning or enemies alive).
    /// </summary>
    public bool IsWaveActive => _currentWave > 0 && (_isSpawning || _enemiesAliveThisWave > 0);

    /// <summary>
    /// Number of enemies still alive in the current wave.
    /// </summary>
    public int EnemiesAlive => _enemiesAliveThisWave;

    /// <summary>
    /// Number of individual enemies in the current wave.
    /// </summary>
    public int EnemiesInWave => _level != null && _currentWave > 0 && _currentWave <= _level.Waves.Count
        ? _level.Waves[_currentWave - 1].TotalEnemies
        : 0;

    /// <summary>
    /// Set the enemy pool to use for spawning.
    /// Called by GameManager during initialization.
    /// </summary>
    public void SetEnemyPool(ObjectPool<Enemy> pool)
    {
        _enemyPool = pool;
    }

    /// <summary>
    /// Set the level whose waves this manager runs.
    /// </summary>
    public void SetLevel(LevelDefinition level)
    {
        _level = level;
    }

    /// <summary>
    /// Create the spawn timer. Called by GameManager after AddChild so the timer
    /// is properly parented before StartNextWave might be triggered.
    /// </summary>
    public void InitTimer()
    {
        _spawnTimer = new Timer();
        _spawnTimer.OneShot = false;
        _spawnTimer.WaitTime = GameConstants.SpawnInterval;
        _spawnTimer.Timeout += OnSpawnTimeout;
        AddChild(_spawnTimer);
        _spawnTimer.Stop();
    }

    /// <summary>
    /// Called to start a wave. Returns false if no more waves or wave already active.
    /// </summary>
    public bool StartNextWave()
    {
        if (IsWaveActive)
            return false;

        if (_level == null || _currentWave >= _level.Waves.Count)
            return false;

        _currentWave++;
        _enemiesAliveThisWave = 0;
        _isSpawning = true;

        // Queue this wave's spawns in order.
        _pendingSpawns.Clear();
        foreach (var spawn in _level.Waves[_currentWave - 1].Spawns)
            _pendingSpawns.Enqueue(spawn);

        EmitSignal(SignalName.WaveStarted, _currentWave);

        // Spawn first spawn event immediately, then start timer for subsequent spawns
        SpawnNext();
        _spawnTimer?.Start();

        return true;
    }

    /// <summary>
    /// Halts spawning immediately: stops the spawn timer and drops any pending
    /// spawns. Called by GameManager.ResetGame before this node is freed so the
    /// timer callback can't fire after the level has been cleared.
    /// </summary>
    public void StopSpawning()
    {
        _spawnTimer?.Stop();
        _pendingSpawns.Clear();
        _isSpawning = false;
    }

    private void OnSpawnTimeout()
    {
        if (_pendingSpawns.Count == 0)
        {
            _spawnTimer?.Stop();
            _isSpawning = false;
            // If all enemies already died before this tick, the wave can now complete.
            CheckWaveCompletion();
            return;
        }

        SpawnNext();
    }

    private void SpawnNext()
    {
        // Guard against spawning after the level/pool has been cleared (e.g. a
        // mid-wave reset): the timer callback can still fire before QueueFree
        // removes it, so bail out instead of dereferencing a null level.
        if (_level == null || _enemyPool == null || _pendingSpawns.Count == 0)
            return;

        var spawn = _pendingSpawns.Dequeue();

        // One route per spawn EVENT: a SwarmCluster is a single event, so all of
        // its members share this route index (they never straddle two routes).
        int routeIndex = NextRouteIndex(ref _routeCounter, _level.Paths.Count);

        var (kind, count) = WaveDefinition.Resolve(spawn);

        if (count == 1)
        {
            // Basic/Armored: single enemy fixed to the path anchor (no formation offset/orbit).
            SpawnEnemy(kind, Vector2.Zero, routeIndex);
        }
        else
        {
            // SwarmCluster: each member gets the same route index.
            foreach (var offset in GenerateClusterOffsets(count, GameConstants.SwarmClusterRadius))
                SpawnEnemy(kind, offset, routeIndex);
        }
    }

    /// <summary>
    /// Computes the round-robin route index for the next spawn event and advances
    /// the counter. Called once per spawn EVENT (a SwarmCluster is a single event),
    /// so all members of a cluster share one route index. The modulo wraps the
    /// counter cleanly no matter how many waves have run.
    /// </summary>
    public static int NextRouteIndex(ref int counter, int routeCount)
    {
        if (routeCount <= 0)
            return 0;

        int index = (int)(((uint)counter) % (uint)routeCount);
        counter++;
        return index;
    }

    /// <summary>
    /// Generates cluster-member offsets arranged evenly around a circle of the given
    /// radius. The ring layout leaves a hollow center and spaces adjacent members
    /// far enough apart to avoid overlapping swarm circles.
    /// </summary>
    public static IReadOnlyList<Vector2> GenerateClusterOffsets(int clusterSize, float radius)
    {
        var offsets = new Vector2[clusterSize];
        float angleStep = Mathf.Tau / clusterSize;

        for (int i = 0; i < clusterSize; i++)
        {
            float angle = angleStep * i;
            offsets[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return offsets;
    }

    private void SpawnEnemy(EnemyKind kind, Vector2 offset, int routeIndex)
    {
        // Acquire enemy from pool, resetting its state for reuse
        var enemy = _enemyPool!.Acquire();
        enemy.ResetForPool();
        enemy.Configure(kind);

        // Set the formation offset before the spawn signal: GameManager.OnEnemySpawned
        // calls SetPath inside the handler, which places the enemy at anchor + offset.
        // Swarm members orbit their anchor; basic enemies stay fixed (zero angular speed).
        float angularSpeed = kind == EnemyKind.Swarm
            ? GameConstants.SwarmClusterRotationSpeed
            : 0f;
        enemy.SetFormationOffset(offset, angularSpeed);

        _enemiesAliveThisWave++;

        EmitSignal(SignalName.EnemySpawned, enemy, routeIndex);
    }

    /// <summary>
    /// Called when an enemy is destroyed (killed by tower).
    /// </summary>
    public void NotifyEnemyDestroyed()
    {
        if (_enemiesAliveThisWave <= 0)
            return;

        _enemiesAliveThisWave--;
        CheckWaveCompletion();
    }

    /// <summary>
    /// Called when an enemy reaches the end of the path.
    /// </summary>
    public void NotifyEnemyReachedEnd()
    {
        if (_enemiesAliveThisWave <= 0)
            return;

        _enemiesAliveThisWave--;
        CheckWaveCompletion();
    }

    private void CheckWaveCompletion()
    {
        if (_enemiesAliveThisWave <= 0 && !_isSpawning)
        {
            int completedWave = _currentWave;

            // Emit WaveCompleted first so UI settles the wave state before the
            // final-wave victory signal transitions out of play.
            EmitSignal(SignalName.WaveCompleted, completedWave);

            if (_level != null && _currentWave >= _level.Waves.Count)
            {
                EmitSignal(SignalName.AllWavesCompleted);
            }
        }
    }
}
