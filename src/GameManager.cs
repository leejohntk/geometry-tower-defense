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
    private LevelDefinition? _level;

    // Active game objects
    private readonly List<Enemy> _activeEnemies = new();
    private readonly List<Tower> _activeTowers = new();
    private readonly List<Projectile> _activeProjectiles = new();
    private readonly List<ExplosionEffect> _activeExplosionEffects = new();

    // Object pools
    private ObjectPool<Enemy>? _enemyPool;
    private ObjectPool<ArrowProjectile>? _arrowProjectilePool;
    private ObjectPool<CannonProjectile>? _cannonProjectilePool;
    private ObjectPool<ExplosionEffect>? _explosionEffectPool;

    // Cached list for collision detection (avoids per-frame allocation)
    private readonly List<(Projectile, Enemy)> _projectileCollisionPairs = new();

    // Cached snapshot for AoE explosion damage (avoids collection-modified exceptions)
    private readonly List<Enemy> _aoeSnapshot = new();

    // Player state
    private int _hp;
    private int _coins;
    private GameState _state = GameState.Playing;

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
    /// The active level definition.
    /// </summary>
    public LevelDefinition? Level => _level;

    /// <summary>
    /// The tower type currently selected for placement (null = not placing).
    /// </summary>
    public TowerType? PlacingTowerType { get; set; } = null;

    /// <summary>
    /// Whether the player is in tower placement mode.
    /// </summary>
    public bool IsPlacingTower => PlacingTowerType.HasValue;

    /// <summary>
    /// The persistent skill tree. Set by Main before play. Awards SP on kills and
    /// layers skill ranks onto newly placed towers. Nullable for tests that only
    /// exercise the in-level economy.
    /// </summary>
    public SkillTree? SkillTree { get; set; }

    /// <summary>
    /// Number of active (alive) enemies.
    /// </summary>
    public int ActiveEnemyCount => _activeEnemies.Count;

    /// <summary>
    /// Returns active towers as a read-only list (no allocation).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Tower> GetActiveTowers() => _activeTowers;

    /// <summary>
    /// Called by Main to initialize a new game for the given level.
    /// Safe to call multiple times — second call returns early if already initialized.
    /// </summary>
    public void Initialize(LevelDefinition level)
    {
        if (_initialized)
            return;

        _initialized = true;
        _level = level;

        _hp = GameConstants.StartingHP;
        _coins = GameConstants.StartingCoins;
        _state = GameState.Playing;
        PlacingTowerType = null;

        // Create object pools once (persist across game restarts)
        if (!_poolsCreated)
        {
            InitializePools();
            _poolsCreated = true;
        }

        // Create grid
        _gridManager = new GridManager();
        _gridManager.Configure(level);
        AddChild(_gridManager);

        // Create wave manager
        _waveManager = new WaveManager();
        _waveManager.SetEnemyPool(_enemyPool!);
        _waveManager.SetLevel(level);
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
        // Enough enemies for Level 2's largest wave (15) plus a buffer.
        _enemyPool = new ObjectPool<Enemy>(24, this);

        // Separate pools for arrow and cannon projectiles.
        _arrowProjectilePool = new ObjectPool<ArrowProjectile>(8, this);
        _cannonProjectilePool = new ObjectPool<CannonProjectile>(8, this);

        // Explosion effects are spawned per cannonball impact.
        _explosionEffectPool = new ObjectPool<ExplosionEffect>(8, this);
    }

    public override void _Process(double delta)
    {
        if (_state != GameState.Playing)
        {
            // Flush any pending skill-tree save even once play has ended (covers the
            // game-over/victory frames and anything still dirty from the last frame).
            SkillTree?.Flush();
            return;
        }

        // Update targeting for all towers
        UpdateTowerTargeting();

        // Try to fire towers at targets
        UpdateTowerFiring();

        // Apply continuous tower drain (laser) to their current targets
        UpdateTowerDrain((float)delta);

        // Update projectile collision detection
        UpdateProjectileCollisions();

        // Persist skill-tree changes at most once per frame.
        SkillTree?.Flush();
    }

    /// <summary>
    /// Each tower targets the nearest enemy within its own range.
    /// </summary>
    private void UpdateTowerTargeting()
    {
        foreach (var tower in _activeTowers)
        {
            Enemy? nearest = null;
            float nearestDistSq = float.MaxValue;
            Vector2 towerPos = tower.Position;
            float rangePixels = tower.RangePixels;
            float rangeSq = rangePixels * rangePixels;

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
    /// Towers fire at their current target, spawning the appropriate projectile type.
    /// </summary>
    private void UpdateTowerFiring()
    {
        foreach (var tower in _activeTowers)
        {
            // Continuous towers drain instead of firing projectiles.
            if (tower.IsContinuous)
                continue;

            if (tower.CurrentTarget == null || tower.CurrentTarget.IsDead)
                continue;

            if (tower.TryFire(tower.CurrentTarget, out Vector2 targetPos))
            {
                Projectile projectile;
                if (tower is CannonTower)
                {
                    var cannon = _cannonProjectilePool!.Acquire();
                    cannon.Exploded += OnCannonExploded;
                    projectile = cannon;
                }
                else
                {
                    projectile = _arrowProjectilePool!.Acquire();
                }

                projectile.Initialize(tower, targetPos, tower.CurrentTarget);
                projectile.EnemyHit += OnProjectileHitEnemy;
                projectile.Dissipated += OnProjectileDissipated;
                _activeProjectiles.Add(projectile);
            }
        }
    }

    /// <summary>
    /// Continuous towers (laser) drain their current target every frame. The target is
    /// the nearest in-range enemy already computed by UpdateTowerTargeting — no extra scan.
    /// Laser damage ignores armor.
    /// </summary>
    private void UpdateTowerDrain(float delta)
    {
        foreach (var tower in _activeTowers)
        {
            if (!tower.IsContinuous)
                continue;

            var target = tower.CurrentTarget;
            if (target == null || target.IsDead)
                continue;

            if (!tower.IsTargetInRange(target))
                continue;

            target.TakeDamage(tower.Dps * delta, ignoreArmor: true);
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

    private void OnEnemySpawned(Enemy enemy, int routeIndex)
    {
        enemy.ReachedEnd += OnEnemyReachedEnd;
        enemy.Destroyed += OnEnemyDestroyed;

        // Set path
        if (_gridManager != null)
        {
            enemy.SetPath(_gridManager.GetPathWaypoints(routeIndex));
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
            // Flush pending skill-tree saves before leaving play.
            SkillTree?.Flush();
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

        _coins += enemy.CoinDrop;
        SkillTree?.AwardSkillPoints(enemy.SkillPointValue);

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
        ReleaseProjectile(projectile);
    }

    private void OnProjectileDissipated(Projectile projectile)
    {
        _activeProjectiles.Remove(projectile);
        projectile.EnemyHit -= OnProjectileHitEnemy;
        projectile.Dissipated -= OnProjectileDissipated;

        ReleaseProjectile(projectile);
    }

    /// <summary>
    /// Applies cannonball AoE damage and spawns the explosion visual at the same impact point.
    /// Uses a snapshot to avoid modifying the active enemy list while iterating it
    /// (TakeDamage emits Destroyed, which mutates the list).
    /// </summary>
    private void OnCannonExploded(CannonProjectile projectile, Vector2 impactPosition)
    {
        _aoeSnapshot.Clear();

        float radius = projectile.ExplosionRadius;
        foreach (var enemy in _activeEnemies)
        {
            if (!enemy.IsDead && CannonProjectile.IsWithinRadius(impactPosition, radius, enemy.Position))
                _aoeSnapshot.Add(enemy);
        }

        foreach (var enemy in _aoeSnapshot)
        {
            if (!enemy.IsDead)
                enemy.TakeDamage(projectile.ExplosionDamage);
        }

        SpawnExplosionEffect(impactPosition, radius);
    }

    /// <summary>
    /// Spawns a pooled explosion effect at the impact point, sized to the cannon's AoE radius.
    /// The same radius value drives both the damage loop above and this visual, so the shown
    /// extent can never drift from the real damage extent.
    /// </summary>
    private void SpawnExplosionEffect(Vector2 impactPosition, float radius)
    {
        var effect = _explosionEffectPool!.Acquire();
        effect.Finished += OnExplosionEffectFinished;
        _activeExplosionEffects.Add(effect);
        effect.Play(impactPosition, radius);
    }

    private void OnExplosionEffectFinished(ExplosionEffect effect)
    {
        _activeExplosionEffects.Remove(effect);
        effect.Finished -= OnExplosionEffectFinished;
        _explosionEffectPool?.Release(effect);
    }

    private void ReleaseProjectile(Projectile projectile)
    {
        if (projectile is CannonProjectile cannon)
        {
            cannon.Exploded -= OnCannonExploded;
            _cannonProjectilePool?.Release(cannon);
        }
        else if (projectile is ArrowProjectile arrow)
        {
            _arrowProjectilePool?.Release(arrow);
        }
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
        // Flush pending skill-tree saves before leaving play.
        SkillTree?.Flush();
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
    /// Place a tower of the currently selected type at the specified grid position.
    /// Returns true if placement succeeded.
    /// </summary>
    public bool PlaceTower(int row, int col)
    {
        if (_state != GameState.Playing)
            return false;

        if (PlacingTowerType == null)
            return false;

        var type = PlacingTowerType.Value;
        int cost = GameConstants.TowerCost(type);

        if (_coins < cost)
            return false;

        if (_gridManager == null || !_gridManager.CanPlaceTower(row, col))
            return false;

        // Deduct coins
        _coins -= cost;
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);

        // Place on grid
        _gridManager.PlaceTower(row, col);

        // Create tower
        Tower tower = type switch
        {
            TowerType.Cannon => new CannonTower(),
            TowerType.Laser => new LaserTower(),
            _ => new ArrowTower()
        };
        tower.Initialize(row, col);
        tower.SetSkillTree(SkillTree?.State);
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
                ReleaseProjectile(projectile);
            }
        }
        _activeProjectiles.Clear();

        // Release in-flight explosion effects back to pool (pools persist across restarts)
        foreach (var effect in _activeExplosionEffects)
        {
            if (IsInstanceValid(effect))
            {
                effect.Finished -= OnExplosionEffectFinished;
                _explosionEffectPool?.Release(effect);
            }
        }
        _activeExplosionEffects.Clear();

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
            _waveManager.StopSpawning();
            _waveManager.QueueFree();
            _waveManager = null;
        }

        _state = GameState.Playing;
        _level = null;
        PlacingTowerType = null;
        _initialized = false;
        // Do NOT call Initialize() — Main.StartNewGame() is the sole caller
    }
}
