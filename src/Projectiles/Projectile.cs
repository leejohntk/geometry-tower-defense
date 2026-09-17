using Godot;

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
        _speed = GameConstants.ProjectileSpeed * GameConstants.CellSize;

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

        _hasHit = true;

        // Apply damage/side-effects synchronously — do not rely on signal timing.
        OnHit(enemy);

        // Signal for lifecycle management (pool release, list cleanup).
        EmitSignal(SignalName.EnemyHit, this, enemy);
    }

    /// <summary>
    /// Returns true if the projectile's bounding circle overlaps with an enemy's bounding circle.
    /// </summary>
    public bool CheckCollision(Enemy enemy)
    {
        if (_hasHit || enemy.IsDead)
            return false;

        float collisionRadius = GameConstants.ProjectileSize / 2f + enemy.CollisionRadius;
        return Position.DistanceSquaredTo(enemy.Position) <= collisionRadius * collisionRadius;
    }
}

/// <summary>
/// Arrow projectile: small yellow triangle that hits the first enemy on its trajectory.
/// Deals single-target damage. No piercing.
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
        enemy.TakeDamage(_damage);
    }
}
