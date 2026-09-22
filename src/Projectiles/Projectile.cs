using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Base class for all projectiles. Handles straight-line movement, max range,
/// collision detection, and the synchronous hit lifecycle. Variants supply damage
/// application and visuals.
/// </summary>
public abstract partial class Projectile : Node2D
{
    // Signal emitted when projectile hits an enemy (lifecycle management)
    [Signal]
    public delegate void EnemyHitEventHandler(Projectile projectile, Enemy enemy);

    // Signal emitted when projectile dissipates (max range or miss)
    [Signal]
    public delegate void DissipatedEventHandler(Projectile projectile);

    protected Vector2 _direction = Vector2.Zero;
    protected float _distanceTraveled = 0f;
    protected float _maxRangePixels;
    protected float _speed;
    protected int _damage;
    private bool _hasHit = false;

    // Pierce state: how many enemies this projectile may still hit, and which enemies
    // it has already passed through (so a piercing arrow never re-hits the same target).
    // The list is allocated once per pooled instance and cleared on Initialize.
    private int _hitsRemaining = 1;
    private readonly List<Enemy> _hitEnemies = new();

    /// <summary>
    /// The tower that fired this projectile.
    /// </summary>
    public Tower? SourceTower { get; private set; }

    /// <summary>
    /// The original intended target of this projectile.
    /// </summary>
    public Enemy? IntendedTarget { get; private set; }

    /// <summary>
    /// True if this projectile has hit something or dissipated.
    /// </summary>
    public bool IsDone => _hasHit;

    public override void _Ready()
    {
        BuildVisual();
    }

    /// <summary>
    /// Build this projectile's visual representation. Called once from _Ready.
    /// </summary>
    protected abstract void BuildVisual();

    /// <summary>
    /// Apply damage/side-effects on first contact with a live enemy.
    /// </summary>
    protected abstract void OnHit(Enemy enemy);

    /// <summary>
    /// Initialize the projectile with source, direction, and intended target.
    /// Safe to call multiple times (for pool reuse) — resets all per-shot state.
    /// </summary>
    public void Initialize(Tower tower, Vector2 targetPosition, Enemy target)
    {
        _hasHit = false;
        _distanceTraveled = 0f;
        SourceTower = tower;
        IntendedTarget = target;

        _damage = tower.Damage;
        _maxRangePixels = tower.RangePixels;
        _speed = GameConstants.ProjectileSpeed * GameConstants.CellSize * tower.ProjectileSpeedMultiplier;

        // A projectile can hit (PierceCount + 1) enemies before it is consumed.
        _hitsRemaining = tower.PierceCount + 1;
        _hitEnemies.Clear();

        Position = tower.Position;
        _direction = (targetPosition - tower.Position).Normalized();

        // Rotate to face direction
        float angle = Mathf.Atan2(_direction.Y, _direction.X);
        Rotation = angle;
    }

    public override void _Process(double delta)
    {
        if (_hasHit)
            return;

        float moveDistance = _speed * (float)delta;
        Position += _direction * moveDistance;
        _distanceTraveled += moveDistance;

        if (_distanceTraveled >= _maxRangePixels)
        {
            _hasHit = true;
            EmitSignal(SignalName.Dissipated, this);
            // GameManager.OnProjectileDissipated handles pool release
        }
    }

    /// <summary>
    /// Called by GameManager when this projectile collides with an enemy.
    /// Applies damage synchronously, then emits EnemyHit for lifecycle management.
    ///
    /// If the enemy is already dead (from another projectile hitting it first this frame),
    /// returns immediately without consuming this projectile, so it continues flying past.
    /// </summary>
    public void HitEnemy(Enemy enemy)
    {
        if (_hasHit)
            return;

        // If the enemy is already dead (from another projectile this frame),
        // don't consume this projectile — let it fly past and dissipate naturally.
        if (enemy.IsDead)
            return;

        // A piercing arrow never re-hits an enemy it already passed through.
        if (_hitEnemies.Contains(enemy))
            return;

        _hitEnemies.Add(enemy);

        // Apply damage/side-effects synchronously — do not rely on signal timing.
        OnHit(enemy);

        _hitsRemaining--;
        if (_hitsRemaining <= 0)
        {
            _hasHit = true;
            // Signal for lifecycle management (pool release, list cleanup).
            EmitSignal(SignalName.EnemyHit, this, enemy);
        }
    }

    /// <summary>
    /// Returns true if the projectile's bounding circle overlaps with an enemy's bounding circle.
    /// </summary>
    public bool CheckCollision(Enemy enemy)
    {
        if (_hasHit || enemy.IsDead)
            return false;

        // Skip enemies this projectile has already pierced this flight.
        if (_hitEnemies.Contains(enemy))
            return false;

        float collisionRadius = GameConstants.ProjectileSize / 2f + enemy.CollisionRadius;
        return Position.DistanceSquaredTo(enemy.Position) <= collisionRadius * collisionRadius;
    }
}

/// <summary>
/// Arrow projectile: small yellow triangle that hits the first enemy on its trajectory.
/// With the Pierce mechanic it continues through up to N further enemies; the Crit
/// mechanic adds an independent per-hit double-damage roll.
/// </summary>
public partial class ArrowProjectile : Projectile
{
    protected override void BuildVisual()
    {
        // Draw the projectile as a small yellow triangle
        var arrowPoints = new Vector2[]
        {
            new Vector2(0, -(GameConstants.ProjectileSize / 2f)),               // Tip (points upward, centered)
            new Vector2(-(GameConstants.ProjectileSize / 2f), GameConstants.ProjectileSize / 2f), // Bottom left
            new Vector2(GameConstants.ProjectileSize / 2f, GameConstants.ProjectileSize / 2f)     // Bottom right
        };

        var arrow = new Polygon2D();
        arrow.Polygon = arrowPoints;
        arrow.Color = new Color(1f, 0.9f, 0.2f); // Yellow fill
        AddChild(arrow);

        // Small outline
        var outline = new Polygon2D();
        outline.Polygon = new Vector2[]
        {
            new Vector2(0, -(GameConstants.ProjectileSize / 2f + 1)),               // Tip
            new Vector2(-(GameConstants.ProjectileSize / 2f + 1), GameConstants.ProjectileSize / 2f), // Bottom left
            new Vector2(GameConstants.ProjectileSize / 2f + 1, GameConstants.ProjectileSize / 2f)     // Bottom right
        };
        outline.Color = new Color(0.8f, 0.5f, 0.05f);
        arrow.AddChild(outline);
    }

    protected override void OnHit(Enemy enemy)
    {
        // Apply single-target damage synchronously. This ensures that when two
        // projectiles hit the same enemy in one frame, the second hit correctly
        // sees the enemy as dead (via HitEnemy's IsDead guard).
        float damage = _damage;

        // Independent per-hit crit roll: 2x the (already skill-boosted) damage.
        // Each pierced hit rolls its own crit via the tower's shared roll source.
        var tower = SourceTower;
        if (tower != null && tower.CritChance > 0f && tower.Rolls.Roll(tower.CritChance))
        {
            damage *= 2f;
            enemy.ApplyHitFlash();
        }

        enemy.TakeDamage(damage);
    }
}
