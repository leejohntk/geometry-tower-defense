using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Manages wave progression, enemy spawning, and wave lifecycle.
/// Uses a Timer node for spawn timing instead of delta accumulation.
/// Uses an ObjectPool for enemy instances.
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
    private int _enemiesSpawnedThisWave = 0;
    private int _enemiesAliveThisWave = 0;
    private bool _isSpawning = false;

    // Enemy pool (set by GameManager)
    private ObjectPool<Enemy>? _enemyPool;

    // Timer-based spawn system (set up in InitTimer)
    private Timer? _spawnTimer;

    /// <summary>
    /// Current wave number (1-based).
    /// </summary>
    public int CurrentWave => _currentWave;

    /// <summary>
    /// Total number of waves in the game.
    /// </summary>
    public int TotalWaves => GameConstants.TotalWaves;

    /// <summary>
    /// True if a wave is currently in progress (spawning or enemies alive).
    /// </summary>
    public bool IsWaveActive => _currentWave > 0 && (_isSpawning || _enemiesAliveThisWave > 0);

    /// <summary>
    /// Number of enemies still alive in the current wave.
    /// </summary>
    public int EnemiesAlive => _enemiesAliveThisWave;

    /// <summary>
    /// Number of enemies that need to be spawned this wave.
    /// </summary>
    public int EnemiesInWave => _currentWave > 0 && _currentWave <= GameConstants.TotalWaves
        ? GameConstants.WaveEnemyCounts[_currentWave - 1]
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

        if (_currentWave >= GameConstants.TotalWaves)
            return false;

        _currentWave++;
        _enemiesSpawnedThisWave = 0;
        _enemiesAliveThisWave = 0;
        _isSpawning = true;

        EmitSignal(SignalName.WaveStarted, _currentWave);

        // Spawn first enemy immediately, then start timer for subsequent spawns
        SpawnEnemy();
        _spawnTimer?.Start();

        return true;
    }

    private void OnSpawnTimeout()
    {
        int totalEnemies = GameConstants.WaveEnemyCounts[_currentWave - 1];

        if (_enemiesSpawnedThisWave >= totalEnemies)
        {
            _spawnTimer?.Stop();
            _isSpawning = false;
            return;
        }

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        // Acquire enemy from pool, resetting its state for reuse
        var enemy = _enemyPool!.Acquire();
        enemy.ResetForPool();

        // Position enemy at spawn point
        enemy.Position = new Vector2(
            GameConstants.CellCenterX(0) - GameConstants.CellSize,
            GameConstants.CellCenterY(GameConstants.PathRow)
        );

        _enemiesSpawnedThisWave++;
        _enemiesAliveThisWave++;

        EmitSignal(SignalName.EnemySpawned, enemy);
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

            if (_currentWave >= GameConstants.TotalWaves)
            {
                EmitSignal(SignalName.AllWavesCompleted);
            }

            EmitSignal(SignalName.WaveCompleted, completedWave);
        }
    }
}
