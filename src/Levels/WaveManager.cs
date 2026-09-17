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

    // Signal emitted when an enemy is spawned during a wave
    [Signal]
    public delegate void EnemySpawnedEventHandler(Enemy enemy);

    // Signal emitted when all enemies in current wave are dead
    [Signal]
    public delegate void WaveCompletedEventHandler(int waveNumber);

    // Signal emitted when all waves are completed (victory)
    [Signal]
    public delegate void AllWavesCompletedEventHandler();

    private int _currentWave = 0;
    private int _enemiesAliveThisWave = 0;
    private bool _isSpawning = false;

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
        if (_pendingSpawns.Count == 0)
            return;

        var spawn = _pendingSpawns.Dequeue();
        if (spawn == SpawnKind.Basic)
        {
            SpawnEnemy(EnemyKind.Basic, Vector2.Zero);
        }
        else
        {
            foreach (var offset in SwarmClusterOffsets)
                SpawnEnemy(EnemyKind.Swarm, offset);
        }
    }

    private static readonly Vector2[] SwarmClusterOffsets =
    {
        new Vector2(-8, -8),
        new Vector2(8, -8),
        new Vector2(0, 8)
    };

    private void SpawnEnemy(EnemyKind kind, Vector2 offset)
    {
        // Acquire enemy from pool, resetting its state for reuse
        var enemy = _enemyPool!.Acquire();
        enemy.ResetForPool();
        enemy.Configure(kind);

        _enemiesAliveThisWave++;

        // Emit first: GameManager.OnEnemySpawned calls SetPath, which places the
        // enemy at the first path waypoint. Apply the cluster offset afterward so it
        // is not overwritten (swarm members must land ~16px apart).
        EmitSignal(SignalName.EnemySpawned, enemy);

        if (offset != Vector2.Zero)
            enemy.Position += offset;
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
