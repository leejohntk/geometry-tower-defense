using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

public enum GameState { Playing, GameOver, Victory }

/// <summary>
/// Central game state manager. Orchestrates grid, enemies, towers, projectiles, economy, and waves.
/// </summary>
public partial class GameManager : Node2D
{
    // Signals for UI updates
    [Signal]
    public delegate void CoinsChangedEventHandler(int coins);

    [Signal]
    public delegate void HpChangedEventHandler(int hp);

    [Signal]
    public delegate void WaveChangedEventHandler(int waveNumber);

    [Signal]
    public delegate void GameOverEventHandler();

    [Signal]
    public delegate void VictoryEventHandler();

    [Signal]
    public delegate void TowerPlacementStateChangedEventHandler(bool canPlace);

    // Core subsystems
    private GridManager? _gridManager;
    private WaveManager? _waveManager;

    // Active game objects
    private readonly List<Enemy> _activeEnemies = new();
    private readonly List<ArrowTower> _activeTowers = new();
    private readonly List<Projectile> _activeProjectiles = new();

    // Object pools
    private ObjectPool<Enemy>? _enemyPool;
    private ObjectPool<Projectile>? _projectilePool;

    // Cached list for collision detection (avoids per-frame allocation)
    private readonly List<(Projectile, Enemy)> _projectileCollisionPairs = new();

    // Player state
    private int _hp;
    private int _coins;
    private GameState _state = GameState.Playing;
    private bool _isPlacingTower = false;

    // Guard against double initialization
    private bool _initialized = false;
    private bool _poolsCreated = false;

    /// <summary>
    /// Current player HP.
    /// </summary>
    public int HP => _hp;

    /// <summary>
    /// Current player coins.
    /// </summary>
    public int Coins => _coins;

    /// <summary>
    /// Current game state.
    /// </summary>
    public GameState State => _state;

    /// <summary>
    /// True if game is over (HP <= 0).
    /// </summary>
    public bool IsGameOver => _state == GameState.GameOver;

    /// <summary>
    /// True if player has won (all waves cleared).
    /// </summary>
    public bool IsVictory => _state == GameState.Victory;

    /// <summary>
    /// The active WaveManager.
    /// </summary>
    public WaveManager? WaveManager => _waveManager;

    /// <summary>
    /// The active GridManager.
    /// </summary>
    public GridManager? Grid => _gridManager;

    /// <summary>
    /// Whether the player is in tower placement mode.
    /// </summary>
    public bool IsPlacingTower
    {
        get => _isPlacingTower;
        set => _isPlacingTower = value;
    }

    /// <summary>
    /// Number of active (alive) enemies.
    /// </summary>
    public int ActiveEnemyCount => _activeEnemies.Count;

    /// <summary>
    /// Returns active towers as a read-only list (no allocation).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<ArrowTower> GetActiveTowers() => _activeTowers;

    /// <summary>
    /// Called by Main to initialize a new game.
    /// Safe to call multiple times — second call returns early if already initialized.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        _hp = GameConstants.StartingHP;
        _coins = GameConstants.StartingCoins;
        _state = GameState.Playing;

        // Create object pools once (persist across game restarts)
        if (!_poolsCreated)
        {
            InitializePools();
            _poolsCreated = true;
        }

        // Create grid
        _gridManager = new GridManager();
        AddChild(_gridManager);

        // Create wave manager
        _waveManager = new WaveManager();
        _waveManager.SetEnemyPool(_enemyPool!);
        AddChild(_waveManager);
        _waveManager.InitTimer();

        // Wire up wave signals
        _waveManager.WaveStarted += OnWaveStarted;
        _waveManager.WaveCompleted += OnWaveCompleted;
        _waveManager.EnemySpawned += OnEnemySpawned;
        _waveManager.AllWavesCompleted += OnAllWavesCompleted;

        // Initial UI updates
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.HpChanged, _hp);
        EmitSignal(SignalName.WaveChanged, 0);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);
    }

    /// <summary>
    /// Pre-allocate pooled objects for enemies and projectiles.
    /// </summary>
    private void InitializePools()
    {
        // Pre-allocate 24 enemies (enough for all waves: max wave is 12)
        _enemyPool = new ObjectPool<Enemy>(24, this);

        // Pre-allocate 8 projectiles (enough for concurrent flying arrows)
        _projectilePool = new ObjectPool<Projectile>(8, this);
    }

    public override void _Process(double delta)
    {
        if (_state != GameState.Playing)
            return;

        // Update targeting for all towers
        UpdateTowerTargeting();

        // Try to fire towers at targets
        UpdateTowerFiring();

        // Update projectile collision detection
        UpdateProjectileCollisions();
    }

    /// <summary>
    /// Each tower targets the nearest enemy within range.
    /// </summary>
    private void UpdateTowerTargeting()
    {
        float rangePixels = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);
        float rangeSq = rangePixels * rangePixels;

        foreach (var tower in _activeTowers)
        {
            Enemy? nearest = null;
            float nearestDistSq = float.MaxValue;
            Vector2 towerPos = tower.Position;

            foreach (var enemy in _activeEnemies)
            {
                if (enemy.IsDead) continue;

                float distSq = enemy.Position.DistanceSquaredTo(towerPos);
                if (distSq <= rangeSq && distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = enemy;
                }
            }

            tower.SetTarget(nearest);
        }
    }

    /// <summary>
    /// Towers fire at their current target.
    /// </summary>
    private void UpdateTowerFiring()
    {
        foreach (var tower in _activeTowers)
        {
            if (tower.CurrentTarget == null || tower.CurrentTarget.IsDead)
                continue;

            if (tower.TryFire(tower.CurrentTarget, out Vector2 targetPos))
            {
                // Acquire projectile from pool
                var projectile = _projectilePool!.Acquire();
                projectile.Initialize(tower, targetPos, tower.CurrentTarget);
                projectile.EnemyHit += OnProjectileHitEnemy;
                projectile.Dissipated += OnProjectileDissipated;
                _activeProjectiles.Add(projectile);
            }
        }
    }

    /// <summary>
    /// Check all active projectiles for collision with enemies.
    /// Uses cached list to avoid per-frame allocation.
    /// </summary>
    private void UpdateProjectileCollisions()
    {
        _projectileCollisionPairs.Clear();

        foreach (var projectile in _activeProjectiles)
        {
            if (projectile.IsDone) continue;

            foreach (var enemy in _activeEnemies)
            {
                if (enemy.IsDead) continue;

                if (projectile.CheckCollision(enemy))
                {
                    _projectileCollisionPairs.Add((projectile, enemy));
                    break; // Only hit first enemy
                }
            }
        }

        foreach (var (projectile, enemy) in _projectileCollisionPairs)
        {
            projectile.HitEnemy(enemy);
        }
    }

    // === Enemy lifecycle ===

    private void OnEnemySpawned(Enemy enemy)
    {
        enemy.ReachedEnd += OnEnemyReachedEnd;
        enemy.Destroyed += OnEnemyDestroyed;

        // Set path
        if (_gridManager != null)
        {
            enemy.SetPath(_gridManager.GetPathWaypoints());
        }

        _activeEnemies.Add(enemy);
    }

    private void OnEnemyReachedEnd(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);

        // Disconnect signals before returning to pool
        enemy.ReachedEnd -= OnEnemyReachedEnd;
        enemy.Destroyed -= OnEnemyDestroyed;

        // Don't process more HP loss if game is already over
        if (_state == GameState.GameOver)
        {
            _waveManager?.NotifyEnemyReachedEnd();
            _enemyPool?.Release(enemy);
            return;
        }

        _hp--;

        if (_hp <= 0)
        {
            _hp = 0;
            _state = GameState.GameOver;
        }

        EmitSignal(SignalName.HpChanged, _hp);

        // Notify wave manager (enemy removed from wave count)
        _waveManager?.NotifyEnemyReachedEnd();

        if (_state == GameState.GameOver)
        {
            EmitSignal(SignalName.GameOver);
        }

        _enemyPool?.Release(enemy);
    }

    private void OnEnemyDestroyed(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);

        // Disconnect signals before returning to pool
        enemy.ReachedEnd -= OnEnemyReachedEnd;
        enemy.Destroyed -= OnEnemyDestroyed;

        _coins += GameConstants.CoinDropPerKill;

        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);

        // Notify wave manager
        _waveManager?.NotifyEnemyDestroyed();

        // Return enemy to pool
        _enemyPool?.Release(enemy);
    }

    // === Projectile lifecycle ===

    private void OnProjectileHitEnemy(Projectile projectile, Enemy enemy)
    {
        _activeProjectiles.Remove(projectile);
        projectile.EnemyHit -= OnProjectileHitEnemy;
        projectile.Dissipated -= OnProjectileDissipated;

        // Damage is already applied synchronously in Projectile.HitEnemy.
        // This handler only manages lifecycle (pool release, list cleanup).

        _projectilePool?.Release(projectile);
    }

    private void OnProjectileDissipated(Projectile projectile)
    {
        _activeProjectiles.Remove(projectile);
        projectile.EnemyHit -= OnProjectileHitEnemy;
        projectile.Dissipated -= OnProjectileDissipated;

        _projectilePool?.Release(projectile);
    }

    // === Wave lifecycle ===

    private void OnWaveStarted(int waveNumber)
    {
        EmitSignal(SignalName.WaveChanged, waveNumber);
    }

    private void OnWaveCompleted(int waveNumber)
    {
        EmitSignal(SignalName.WaveChanged, waveNumber);
    }

    private void OnAllWavesCompleted()
    {
        _state = GameState.Victory;
        EmitSignal(SignalName.Victory);
    }

    /// <summary>
    /// Start the next wave. Called by UI button.
    /// </summary>
    public bool StartNextWave()
    {
        if (_state != GameState.Playing)
            return false;

        return _waveManager?.StartNextWave() ?? false;
    }

    // === Tower placement ===

    /// <summary>
    /// Place a tower at the specified grid position.
    /// Returns true if placement succeeded.
    /// </summary>
    public bool PlaceTower(int row, int col)
    {
        if (_state != GameState.Playing)
            return false;

        if (_coins < GameConstants.ArrowTowerCost)
            return false;

        if (_gridManager == null || !_gridManager.CanPlaceTower(row, col))
            return false;

        // Deduct coins
        _coins -= GameConstants.ArrowTowerCost;
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);

        // Place on grid
        _gridManager.PlaceTower(row, col);

        // Create tower
        var tower = new ArrowTower();
        tower.Initialize(row, col);
        _activeTowers.Add(tower);
        AddChild(tower);

        return true;
    }

    /// <summary>
    /// Reset the game state (for starting a new game after game over / victory).
    /// Does NOT call Initialize() — Main.StartNewGame() is the sole caller of Initialize.
    /// </summary>
    public void ResetGame()
    {
        // Release active enemies back to pool
        foreach (var enemy in _activeEnemies)
        {
            if (IsInstanceValid(enemy))
            {
                enemy.ReachedEnd -= OnEnemyReachedEnd;
                enemy.Destroyed -= OnEnemyDestroyed;
                _enemyPool?.Release(enemy);
            }
        }
        _activeEnemies.Clear();

        // Release active projectiles back to pool
        foreach (var projectile in _activeProjectiles)
        {
            if (IsInstanceValid(projectile))
            {
                projectile.EnemyHit -= OnProjectileHitEnemy;
                projectile.Dissipated -= OnProjectileDissipated;
                _projectilePool?.Release(projectile);
            }
        }
        _activeProjectiles.Clear();

        // Free towers (not pooled)
        foreach (var tower in _activeTowers)
        {
            if (IsInstanceValid(tower))
                tower.QueueFree();
        }
        _activeTowers.Clear();

        // Remove old subsystems
        if (_gridManager != null && IsInstanceValid(_gridManager))
        {
            _gridManager.QueueFree();
            _gridManager = null;
        }

        if (_waveManager != null && IsInstanceValid(_waveManager))
        {
            _waveManager.QueueFree();
            _waveManager = null;
        }

        _state = GameState.Playing;
        _initialized = false;
        // Do NOT call Initialize() — Main.StartNewGame() is the sole caller
    }
}
