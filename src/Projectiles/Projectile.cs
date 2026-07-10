using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Arrow projectile that travels in a straight line.
/// Hits the first enemy encountered on its trajectory.
/// Dissipates on hit or at max range (4 cells).
/// Arrow never changes direction. One hit per arrow. No piercing.
/// </summary>
public partial class Projectile : Node2D
{
    // Signal emitted when projectile hits an enemy
    [Signal]
    public delegate void EnemyHitEventHandler(Projectile projectile, Enemy enemy);

    // Signal emitted when projectile dissipates (max range or miss)
    [Signal]
    public delegate void DissipatedEventHandler(Projectile projectile);

    private Vector2 _direction = Vector2.Zero;
    private float _distanceTraveled = 0f;
    private float _maxRangePixels;
    private float _speed;
    private bool _hasHit = false;

    /// <summary>
    /// The tower that fired this projectile.
    /// </summary>
    public ArrowTower? SourceTower { get; private set; }

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
        _maxRangePixels = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);
        _speed = GameConstants.ProjectileSpeed * GameConstants.CellSize;

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

    /// <summary>
    /// Initialize the projectile with source, direction, and intended target.
    /// Safe to call multiple times (for pool reuse) — resets all per-shot state.
    /// </summary>
    public void Initialize(ArrowTower tower, Vector2 targetPosition, Enemy target)
    {
        _hasHit = false;
        _distanceTraveled = 0f;
        SourceTower = tower;
        IntendedTarget = target;

        Position = tower.Position;
        _direction = (targetPosition - tower.Position).Normalized();

        // Rotate arrow to face direction
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
    /// Called by GameManager when this projectile hits an enemy.
    /// </summary>
    public void HitEnemy(Enemy enemy)
    {
        if (_hasHit)
            return;

        _hasHit = true;
        EmitSignal(SignalName.EnemyHit, this, enemy);
        // GameManager.OnProjectileHitEnemy handles pool release
    }

    /// <summary>
    /// Returns true if the projectile's bounding circle overlaps with an enemy's bounding circle.
    /// Uses correct radius: ProjectileSize/2 + EnemyDiameter/2.
    /// </summary>
    public bool CheckCollision(Enemy enemy)
    {
        if (_hasHit || enemy.IsDead)
            return false;

        float collisionRadius = GameConstants.ProjectileSize / 2f + GameConstants.EnemyDiameter / 2f;
        return Position.DistanceSquaredTo(enemy.Position) <= collisionRadius * collisionRadius;
    }
}
