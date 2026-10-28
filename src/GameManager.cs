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
    private readonly List<PierceSparkEffect> _activePierceSparks = new();

    // Object pools
    private ObjectPool<Enemy>? _enemyPool;
    private ObjectPool<ArrowProjectile>? _arrowProjectilePool;
    private ObjectPool<CannonProjectile>? _cannonProjectilePool;
    private ObjectPool<ExplosionEffect>? _explosionEffectPool;
    private ObjectPool<PierceSparkEffect>? _pierceSparkPool;

    // Cached list for collision detection (avoids per-frame allocation)
    private readonly List<(Projectile, Enemy)> _projectileCollisionPairs = new();

    // Cached snapshot for AoE explosion damage (avoids collection-modified exceptions)
    private readonly List<Enemy> _aoeSnapshot = new();

    // Cached snapshots for the status pass and the laser chain path (no per-frame
    // allocation — these lists are cleared and reused every frame).
    private readonly List<Enemy> _statusSnapshot = new();
    private readonly List<Enemy> _chainHitEnemies = new();
    private readonly List<Vector2> _chainTargets = new();

    // The active Progenitor (at most one per level). Tracked by reference rather than
    // discovered by scanning _activeEnemies, so ticking the boss costs one null check
    // per frame. Nulled when the boss dies, leaks, or the level resets, which stops all
    // future summons.
    private Enemy? _activeBoss;
    private int _bossRouteIndex;

    // Cached ring offsets per squad size. Summons reuse these arrays so summoning never
    // allocates on the frame it fires; at most three arrays are ever built per game.
    private readonly Vector2[][] _summonOffsetsBySquadSize =
        new Vector2[GameConstants.ProgenitorMaxSquadSize + 1][];

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
    /// The persistent skill tree. Set by Main before play. Awards SP on level outcomes
    /// (victory/defeat) and layers skill ranks onto newly placed towers. Nullable for
    /// tests that only exercise the in-level economy.
    /// </summary>
    public SkillTree? SkillTree { get; set; }

    /// <summary>
    /// Injectable, seeded roll source for Progenitor summon composition. Production
    /// uses the shared seeded default; tests assign a scripted source to force exact
    /// summon squads.
    /// </summary>
    public SkillRandom SummonRandom { get; set; } = SkillRandom.Default;

    /// <summary>
    /// Number of active (alive) enemies.
    /// </summary>
    public int ActiveEnemyCount => _activeEnemies.Count;

    /// <summary>
    /// Returns active towers as a read-only list (no allocation).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Tower> GetActiveTowers() => _activeTowers;

    /// <summary>
    /// Returns active enemies as a read-only list (no allocation). Includes boss summons.
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Enemy> GetActiveEnemies() => _activeEnemies;

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
        _activeBoss = null;
        _bossRouteIndex = 0;

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
        // Enough enemies for Level 5's peak: wave 5 defines 24 enemies (boss included)
        // plus a phase-3 summon squad of 5, with a buffer. Prewarming the peak keeps
        // ObjectPool.Acquire from constructing nodes mid-wave when fewer than a full
        // squad has recycled.
        _enemyPool = new ObjectPool<Enemy>(36, this);

        // Separate pools for arrow and cannon projectiles.
        _arrowProjectilePool = new ObjectPool<ArrowProjectile>(8, this);
        _cannonProjectilePool = new ObjectPool<CannonProjectile>(8, this);

        // Explosion effects are spawned per cannonball impact.
        _explosionEffectPool = new ObjectPool<ExplosionEffect>(8, this);

        // Pierce sparks are spawned per passed-through enemy, so several can be alive
        // at once for a single fully-piercing arrow (pool grows on demand if exceeded).
        _pierceSparkPool = new ObjectPool<PierceSparkEffect>(12, this);
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

        // Tick enemy stun/burn/flash statuses (movement pause is read in Enemy._Process)
        UpdateEnemyStatuses((float)delta);

        // Advance the boss summon timers. Done centrally (one tracked reference, no
        // enemy-list scan) and only while playing, so game over stops all summons.
        UpdateProgenitorSummons((float)delta);

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
    /// Cannon towers fire a cluster volley of (1 + cluster rank) shells spread across
    /// a small arc; arrow towers fire a single piercing/crit-capable arrow.
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
                if (tower is CannonTower cannon)
                {
                    SpawnCannonVolley(cannon, targetPos);
                }
                else
                {
                    var arrow = _arrowProjectilePool!.Acquire();
                    arrow.Initialize(tower, targetPos, tower.CurrentTarget);
                    RegisterProjectile(arrow);
                }
            }
        }
    }

    /// <summary>
    /// Spawns the cannon's cluster volley: (1 + cluster rank) shells fanned out
    /// symmetrically around the direct line to the target. Each shell explodes
    /// independently with the tower's skill-boosted splash radius.
    /// </summary>
    private void SpawnCannonVolley(CannonTower cannon, Vector2 targetPos)
    {
        int count = cannon.ClusterCount;
        Vector2 baseDirection = (targetPos - cannon.Position).Normalized();

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * GameConstants.SkillCannonClusterSpreadPerShellRadians;
            Vector2 direction = baseDirection.Rotated(offset);
            Vector2 shellTarget = cannon.Position + direction * cannon.RangePixels;

            var shell = _cannonProjectilePool!.Acquire();
            shell.Exploded += OnCannonExploded;
            shell.Initialize(cannon, shellTarget, cannon.CurrentTarget!);
            RegisterProjectile(shell);
        }
    }

    /// <summary>
    /// Subscribes a freshly spawned projectile's lifecycle signals and tracks it in the
    /// active list. Pierce is wired for every projectile type: only a projectile with
    /// spare hits ever emits it (the cannon's pierce count is 0), so a splash shot is
    /// unaffected.
    /// </summary>
    private void RegisterProjectile(Projectile projectile)
    {
        projectile.EnemyHit += OnProjectileHitEnemy;
        projectile.Pierced += OnProjectilePierced;
        projectile.Dissipated += OnProjectileDissipated;
        _activeProjectiles.Add(projectile);
    }

    /// <summary>
    /// Inverse of <see cref="RegisterProjectile"/>: unhooks every signal and untracks
    /// the projectile.
    /// </summary>
    private void UnregisterProjectile(Projectile projectile)
    {
        projectile.EnemyHit -= OnProjectileHitEnemy;
        projectile.Pierced -= OnProjectilePierced;
        projectile.Dissipated -= OnProjectileDissipated;
    }

    /// <summary>
    /// Continuous towers (laser) drain their current target every frame. The target is
    /// the nearest in-range enemy already computed by UpdateTowerTargeting — no extra scan.
    /// Laser damage ignores armor and layers on the Part-2 mechanics: ramp-up while
    /// holding one target, chain jumps to nearby enemies, and the per-second ignite roll.
    /// </summary>
    private void UpdateTowerDrain(float delta)
    {
        foreach (var tower in _activeTowers)
        {
            if (!tower.IsContinuous)
                continue;

            if (tower is not LaserTower laser)
                continue;

            var target = laser.CurrentTarget;
            if (target == null || target.IsDead || !laser.IsTargetInRange(target))
            {
                laser.ResetBeam();
                continue;
            }

            float rampMultiplier = laser.UpdateRamp(target, delta);
            float tickDamage = laser.Dps * rampMultiplier * delta;
            target.TakeDamage(tickDamage, ignoreArmor: true, source: laser);

            if (target.IsDead)
            {
                laser.ResetBeam();
                continue;
            }

            ApplyChain(laser, target, tickDamage);

            int igniteRolls = laser.ConsumeIgniteRolls(delta);
            for (int i = 0; i < igniteRolls; i++)
            {
                if (laser.Rolls.Roll(laser.IgniteChancePerSecond))
                    target.ApplyBurn(
                        GameConstants.SkillLaserBurnDps,
                        GameConstants.SkillLaserBurnDuration,
                        laser);
            }
        }
    }

    /// <summary>
    /// Jumps the beam from the primary target to up to N further enemies, each within
    /// one chain range of the previous target and each at 60% of the previous tick's
    /// damage. No enemy is hit twice by one chain. Uses cached lists only.
    /// </summary>
    private void ApplyChain(LaserTower laser, Enemy primary, float tickDamage)
    {
        _chainHitEnemies.Clear();
        _chainHitEnemies.Add(primary);
        _chainTargets.Clear();

        int jumps = laser.ChainJumps;
        if (jumps <= 0)
        {
            laser.SetChainTargets(System.Array.Empty<Vector2>());
            return;
        }

        var from = primary;
        float damage = tickDamage;
        for (int i = 0; i < jumps; i++)
        {
            var next = LaserChain.FindNextTarget(from, _activeEnemies, _chainHitEnemies);
            if (next == null)
                break;

            damage *= GameConstants.SkillLaserChainFalloff;
            next.TakeDamage(damage, ignoreArmor: true, source: laser);
            _chainHitEnemies.Add(next);
            _chainTargets.Add(next.Position);
            from = next;
        }

        laser.SetChainTargets(_chainTargets);
    }

    /// <summary>
    /// Ticks stun/burn/flash timers and applies burn damage for every active enemy.
    /// Iterates a cached snapshot because a burn tick can kill an enemy, whose Destroyed
    /// signal mutates the live enemy list mid-pass.
    /// </summary>
    private void UpdateEnemyStatuses(float delta)
    {
        _statusSnapshot.Clear();
        foreach (var enemy in _activeEnemies)
        {
            if (!enemy.IsDead)
                _statusSnapshot.Add(enemy);
        }

        foreach (var enemy in _statusSnapshot)
        {
            if (!enemy.IsDead)
                enemy.TickStatuses(delta);
        }
    }

    /// <summary>
    /// Advances the active Progenitor's summon state machine. One tracked reference, so
    /// this never scans the enemy list. The boss's SummonReady signal fires synchronously
    /// from this tick and hands off to <see cref="OnBossSummonReady"/>.
    /// </summary>
    private void UpdateProgenitorSummons(float delta)
    {
        _activeBoss?.TickProgenitorSummon(delta);
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
        // Set path before registering so the enemy is already at its spawn anchor.
        if (_gridManager != null)
        {
            enemy.SetPath(_gridManager.GetPathWaypoints(routeIndex));
        }

        RegisterEnemy(enemy, routeIndex);
    }

    /// <summary>
    /// Hooks an enemy into the normal lifecycle: reached-end/destroyed handlers, boss
    /// tracking (reference only, never a list scan), the summon roll source, and the
    /// active list. Shared by wave spawns and boss summons.
    /// </summary>
    private void RegisterEnemy(Enemy enemy, int routeIndex)
    {
        enemy.ReachedEnd += OnEnemyReachedEnd;
        enemy.Destroyed += OnEnemyDestroyed;

        if (enemy.Kind == EnemyKind.Progenitor)
        {
            _activeBoss = enemy;
            _bossRouteIndex = routeIndex;
            enemy.SetSummonRandom(SummonRandom);
            enemy.SummonReady += OnBossSummonReady;
        }

        _activeEnemies.Add(enemy);
    }

    /// <summary>
    /// Inverse of <see cref="RegisterEnemy"/>: unhooks every lifecycle signal and, if
    /// this was the boss, stops future summons by clearing the tracked reference.
    /// </summary>
    private void UnregisterEnemy(Enemy enemy)
    {
        enemy.ReachedEnd -= OnEnemyReachedEnd;
        enemy.Destroyed -= OnEnemyDestroyed;

        if (enemy.Kind == EnemyKind.Progenitor)
        {
            enemy.SummonReady -= OnBossSummonReady;
            if (ReferenceEquals(_activeBoss, enemy))
                _activeBoss = null;
        }
    }

    /// <summary>
    /// A Progenitor finished its summon windup. Releases the rolled squad into the
    /// normal enemy lifecycle: each member is acquired from the shared enemy pool,
    /// configured as its rolled kind, started at the boss's current waypoint index, and
    /// anchored at the lead point ahead of the boss on the same route. The static ring
    /// spread is applied as the formation offset with zero orbit speed. Summons are
    /// counted by WaveManager and worth zero coins, so the wave stays active while any
    /// of them is alive.
    /// </summary>
    private void OnBossSummonReady(Enemy boss)
    {
        if (_state != GameState.Playing || _waveManager == null || _gridManager == null)
            return;

        int count = boss.PendingSummonCount;
        if (count <= 0)
            return;

        var waypoints = _gridManager.GetPathWaypoints(_bossRouteIndex);

        // A boss that has reached the base is walking off GridManager's synthetic exit
        // waypoint; spawning adds there would drop them on/past the base, where they
        // would leak within a frame or two for unavoidable HP loss. Reaching the base
        // stops all future summons, so drop the pending squad.
        if (waypoints.Count == 0 || boss.HasReachedBase)
        {
            boss.ClearPendingSummon();
            return;
        }

        var offsets = GetSummonOffsets(count);
        Vector2 leadAnchor = boss.GetSummonLeadPoint();
        int waypointIndex = boss.CurrentWaypointIndex;

        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = boss.PendingSummonKind(i);
            var add = _waveManager.SpawnSummon(kind, waypoints, waypointIndex, leadAnchor, offsets[i]);
            if (add == null)
                continue;

            RegisterEnemy(add, _bossRouteIndex);
        }

        boss.ClearPendingSummon();
    }

    /// <summary>
    /// Static ring offsets for a summon squad, cached per squad size. The layouts come
    /// from WaveManager.GenerateClusterOffsets with the Progenitor's offset radius, so a
    /// squad spreads around its lead anchor without overlapping — but, unlike a swarm
    /// cluster, these adds never orbit.
    /// </summary>
    private Vector2[] GetSummonOffsets(int count)
    {
        int size = Mathf.Clamp(count, 1, GameConstants.ProgenitorMaxSquadSize);
        var cached = _summonOffsetsBySquadSize[size];
        if (cached != null)
            return cached;

        var generated = WaveManager.GenerateClusterOffsets(size, GameConstants.ProgenitorSummonOffsetRadius);
        var offsets = new Vector2[size];
        for (int i = 0; i < size; i++)
            offsets[i] = generated[i];

        _summonOffsetsBySquadSize[size] = offsets;
        return offsets;
    }

    private void OnEnemyReachedEnd(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);

        // Disconnect signals before returning to pool
        UnregisterEnemy(enemy);

        // Don't process more HP loss if game is already over
        if (_state == GameState.GameOver)
        {
            _waveManager?.NotifyEnemyReachedEnd();
            // Drop tower credit before pooling: a released enemy must never keep a
            // strong reference to a tower (which the level reset frees).
            enemy.ClearAttribution();
            _enemyPool?.Release(enemy);
            return;
        }

        // Every kind costs one HP except a boss with its own base-damage constant.
        _hp -= enemy.BaseDamage;

        if (_hp <= 0)
        {
            _hp = 0;
            _state = GameState.GameOver;

            // Stop the wave timer immediately on defeat. Without this the spawn timer
            // keeps firing after game over and keeps adding enemies to a finished run.
            _waveManager?.StopSpawning();
        }

        EmitSignal(SignalName.HpChanged, _hp);

        // Notify wave manager (enemy removed from wave count)
        _waveManager?.NotifyEnemyReachedEnd();

        if (_state == GameState.GameOver)
        {
            // Award defeat SP once before flushing (this block runs on the single
            // enemy whose arrival drops HP to 0).
            if (_level != null)
                SkillTree?.AwardSkillPoints(GameConstants.SkillPointDefeatReward(_level.Id));

            // Flush pending skill-tree saves before leaving play.
            SkillTree?.Flush();
            EmitSignal(SignalName.GameOver);
        }

        // BaseDamage was consumed above; drop tower credit before pooling.
        enemy.ClearAttribution();
        _enemyPool?.Release(enemy);
    }

    private void OnEnemyDestroyed(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);

        // Disconnect signals before returning to pool. For the boss this also stops
        // all future summons; already-spawned adds keep walking and stay counted.
        UnregisterEnemy(enemy);

        // Summoned enemies carry CoinDrop = 0, so their kills award nothing.
        _coins += enemy.CoinDrop;

        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);

        // Notify wave manager
        _waveManager?.NotifyEnemyDestroyed();

        // Drop tower credit before pooling so a released enemy never keeps a strong
        // reference to a tower that a level reset may free.
        enemy.ClearAttribution();

        // Return enemy to pool
        _enemyPool?.Release(enemy);
    }

    // === Projectile lifecycle ===

    private void OnProjectileHitEnemy(Projectile projectile, Enemy enemy)
    {
        _activeProjectiles.Remove(projectile);
        UnregisterProjectile(projectile);

        // Damage is already applied synchronously in Projectile.HitEnemy.
        // This handler only manages lifecycle (pool release, list cleanup).
        ReleaseProjectile(projectile);
    }

    /// <summary>
    /// A hit landed but the projectile survived it (pierce), so it flies on to the next
    /// enemy. Damage is already applied synchronously in Projectile.HitEnemy; this only
    /// spawns the per-hit spark at the enemy it passed through.
    /// </summary>
    private void OnProjectilePierced(Projectile projectile, Enemy enemy)
    {
        SpawnPierceSpark(enemy.Position);
    }

    private void OnProjectileDissipated(Projectile projectile)
    {
        _activeProjectiles.Remove(projectile);
        UnregisterProjectile(projectile);

        ReleaseProjectile(projectile);
    }

    /// <summary>
    /// Applies cannonball AoE damage (plus any stun rolls) and spawns the explosion
    /// visual at the same impact point. Uses a snapshot to avoid modifying the active
    /// enemy list while iterating it (TakeDamage emits Destroyed, which mutates the list).
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

        projectile.Explode(_aoeSnapshot, impactPosition, projectile.ExplosionDamage);

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

    /// <summary>
    /// Spawns a pooled pierce spark where an arrow passed through an enemy, so each
    /// pierced hit is visible. Sized from the pierce constants (never the cannon's AoE
    /// radius), so the spark can't be mistaken for a splash.
    /// </summary>
    private void SpawnPierceSpark(Vector2 position)
    {
        var effect = _pierceSparkPool!.Acquire();
        effect.Finished += OnPierceSparkFinished;
        _activePierceSparks.Add(effect);
        effect.Play(position);
    }

    private void OnPierceSparkFinished(PierceSparkEffect effect)
    {
        _activePierceSparks.Remove(effect);
        effect.Finished -= OnPierceSparkFinished;
        _pierceSparkPool?.Release(effect);
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
        // A defeat can complete the final wave synchronously: OnEnemyReachedEnd sets
        // GameOver, then its NotifyEnemyReachedEnd call can empty the wave and fire
        // this signal. Victory must never overwrite a defeat (which would also award
        // victory SP and suppress the GameOver signal/defeat SP).
        if (_state != GameState.Playing)
            return;

        _state = GameState.Victory;

        // Award victory SP once before flushing.
        if (_level != null)
            SkillTree?.AwardSkillPoints(GameConstants.SkillPointVictoryReward(_level.Id));

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
    /// A successful placement consumes the selection (one tower per selection).
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

        // One selection = one tower. Consume the placement type here so no caller can
        // leave the player stuck in placement mode after a successful placement.
        PlacingTowerType = null;

        return true;
    }

    /// <summary>
    /// True when the tower belongs to this game's live level: a valid node that is
    /// still in the active tower list. Upgrades reject anything else so a stale panel
    /// (or a tower from a previous level) can never spend coins.
    /// </summary>
    public bool IsTowerActive(Tower? tower) =>
        tower != null && IsInstanceValid(tower) && _activeTowers.Contains(tower);

    /// <summary>
    /// Buys the next generic rank (0 -> 1 or 1 -> 2) for a live tower. Exactly one
    /// rank and one coin deduction per success. Rejected with no state change when
    /// the game is not playing, the tower is not active, XP is below the next
    /// threshold, coins are short, or the tower is at rank 2 (the third step is the
    /// archetype choice, never a generic upgrade).
    /// </summary>
    public bool TryUpgradeTower(Tower? tower)
    {
        if (_state != GameState.Playing)
            return false;

        if (!IsTowerActive(tower))
            return false;

        var target = tower!;
        if (!TowerUpgrades.CanBuyNextRank(target.Rank, target.Xp, _coins))
            return false;

        int cost = TowerUpgrades.CostToReachRank(target.Rank + 1);
        if (!target.ApplyRankUpgrade())
            return false;

        _coins -= cost;
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);
        return true;
    }

    /// <summary>
    /// Buys the rank 2 -> 3 archetype specialization. Spends the rank-3 cost, sets
    /// the tower's final rank, and records the chosen seed. Rejected with no state
    /// change unless the game is playing, the tower is live and exactly rank 2, the
    /// hidden rank-3 XP gate is reached, coins cover the cost, and the archetype
    /// belongs to that tower type. Final — there is no respec or refund.
    /// </summary>
    public bool TryChooseArchetype(Tower? tower, TowerArchetype archetype)
    {
        if (_state != GameState.Playing)
            return false;

        if (!IsTowerActive(tower))
            return false;

        var target = tower!;
        if (!TowerUpgrades.CanBuyArchetype(target.Rank, target.Xp, _coins))
            return false;

        if (!TowerArchetypes.IsValidFor(target.Type, archetype))
            return false;

        if (!target.ApplyArchetype(archetype))
            return false;

        _coins -= TowerUpgrades.CostToReachRank(GameConstants.TowerMaxRank);
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);
        return true;
    }

    /// <summary>
    /// Test-only seam: grants coins without combat so placement-lifecycle tests can
    /// start above one tower's cost and observe that a successful placement consumes
    /// the selection while coins remain. Internal to the game assembly; not part of
    /// the public gameplay API.
    /// </summary>
    internal void AddCoinsForTesting(int amount)
    {
        if (amount <= 0)
            return;

        _coins += amount;
        EmitSignal(SignalName.CoinsChanged, _coins);
        EmitSignal(SignalName.TowerPlacementStateChanged, _coins >= GameConstants.ArrowTowerCost);
    }

    /// <summary>
    /// Test-only seam: forces the game state so transaction rejection can be exercised
    /// without driving a full defeat/victory. Internal to the game assembly; production
    /// transitions only through enemy arrivals and wave completion.
    /// </summary>
    internal void SetStateForTesting(GameState state) => _state = state;

    /// <summary>
    /// Test-only seam: registers an externally constructed enemy into the live
    /// lifecycle so the centralized damage pass (laser drain, chain jumps, statuses)
    /// can be exercised without a wave timer. Internal to the game assembly;
    /// production spawns only through <see cref="WaveManager"/>.
    /// </summary>
    internal void RegisterEnemyForTesting(Enemy enemy, int routeIndex = 0)
    {
        if (_state != GameState.Playing)
            return;

        if (enemy == null || !IsInstanceValid(enemy))
            return;

        // Registering twice would double-subscribe the lifecycle signals and add a
        // duplicate list entry (double coins on death). Treat it as a no-op.
        if (_activeEnemies.Contains(enemy))
            return;

        RegisterEnemy(enemy, routeIndex);
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
                // Unregister while the enemy still reports its real Kind, so a boss
                // unhooks SummonReady before ResetForPool turns it back into a Basic.
                UnregisterEnemy(enemy);
                // Reset immediately (not just on the next Acquire) so no credit entry
                // keeps a tower reference alive after the level's towers are freed.
                // ResetForPool also clears damage attribution and the burn source.
                enemy.ResetForPool();
                _enemyPool?.Release(enemy);
            }
        }
        _activeEnemies.Clear();

        // Stop all boss summon timers: after a reset nothing may tick a stale boss or
        // emit a summon callback into a torn-down level.
        _activeBoss = null;
        _bossRouteIndex = 0;

        // Release active projectiles back to pool
        foreach (var projectile in _activeProjectiles)
        {
            if (IsInstanceValid(projectile))
            {
                UnregisterProjectile(projectile);
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

        // Release in-flight pierce sparks back to pool (pools persist across restarts)
        foreach (var spark in _activePierceSparks)
        {
            if (IsInstanceValid(spark))
            {
                spark.Finished -= OnPierceSparkFinished;
                _pierceSparkPool?.Release(spark);
            }
        }
        _activePierceSparks.Clear();

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
