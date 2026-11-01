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

    // Signal emitted when a hit lands but the projectile survives it and flies on
    // (pierce). Presentation only: GameManager spawns a spark at the passed-through
    // enemy. The consuming hit emits EnemyHit instead and needs no spark.
    [Signal]
    public delegate void PiercedEventHandler(Projectile projectile, Enemy enemy);

    // Signal emitted when projectile dissipates (max range or miss)
    [Signal]
    public delegate void DissipatedEventHandler(Projectile projectile);

    protected Vector2 _direction = Vector2.Zero;
    protected float _distanceTraveled = 0f;
    protected float _maxRangePixels;
    protected float _speed;
    // Damage stays a float end to end so rank multipliers (10.9, 16.35) reach the
    // enemy with no integer truncation before armor.
    protected float _damage;
    private bool _hasHit = false;

    // Pierce state: how many enemies this projectile may still hit, and which enemies
    // it has already passed through (so a piercing arrow never re-hits the same target).
    // The list is allocated once per pooled instance and cleared on Initialize.
    private int _hitsRemaining = 1;
    private readonly List<Enemy> _hitEnemies = new();

    // Visual-only fading trail. Created lazily (pooled projectiles build it once) and
    // reset on every Initialize; it never collides and never feeds back into movement.
    protected ProjectileTrail? _trail;

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

    /// <summary>
    /// Number of live trail samples. Zero after a hit, a dissipation, or a pool reuse,
    /// so tests and callers can observe that no trail state leaks across shots.
    /// </summary>
    public int TrailSampleCount => _trail?.Count ?? 0;

    public override void _Ready()
    {
        BuildVisual();
        EnsureTrail();
    }

    /// <summary>
    /// Build this projectile's visual representation. Called once from _Ready.
    /// </summary>
    protected abstract void BuildVisual();

    /// <summary>
    /// Creates this projectile's trail history (null = no trail). Called once, lazily,
    /// so tests that drive a projectile outside the scene tree still get a trail.
    /// </summary>
    protected virtual ProjectileTrail? CreateTrail() => null;

    /// <summary>Builds the trail's render nodes. Called once, after CreateTrail.</summary>
    protected virtual void BuildTrailVisual()
    {
    }

    /// <summary>Repaints/positions the trail nodes from the current trail samples.</summary>
    protected virtual void UpdateTrailVisual()
    {
    }

    private void EnsureTrail()
    {
        if (_trail != null)
            return;

        _trail = CreateTrail();
        if (_trail != null)
            BuildTrailVisual();
    }

    /// <summary>
    /// Apply damage/side-effects on first contact with a live enemy.
    /// </summary>
    protected abstract void OnHit(Enemy enemy);

    /// <summary>
    /// Initialize the projectile with source, direction, and intended target.
    /// Safe to call multiple times (for pool reuse) — resets all per-shot state,
    /// including the visual trail.
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

        // A pooled projectile must never reuse the previous shot's trail.
        EnsureTrail();
        _trail?.Reset(Position);
        UpdateTrailVisual();
    }

    public override void _Process(double delta)
    {
        if (_hasHit)
            return;

        float moveDistance = _speed * (float)delta;
        Position += _direction * moveDistance;
        _distanceTraveled += moveDistance;

        _trail?.Advance(Position, (float)delta);
        UpdateTrailVisual();

        if (_distanceTraveled >= _maxRangePixels)
        {
            _hasHit = true;
            ClearTrail();
            EmitSignal(SignalName.Dissipated, this);
            // GameManager.OnProjectileDissipated handles pool release
        }
    }

    /// <summary>
    /// Drops the visual trail (on impact, dissipation, or pool reuse).
    /// </summary>
    protected void ClearTrail()
    {
        _trail?.Clear();
        UpdateTrailVisual();
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
            // The trail ends where the shot ended; a pierce hit keeps flying, so its
            // trail is deliberately left intact instead of restarting.
            ClearTrail();
            // Signal for lifecycle management (pool release, list cleanup).
            EmitSignal(SignalName.EnemyHit, this, enemy);
        }
        else
        {
            // Pierce: this hit did not consume the projectile, so report the
            // pass-through for its per-hit spark. Emitted only here — the decision
            // that the arrow continues is made in exactly one place.
            EmitSignal(SignalName.Pierced, this, enemy);
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
    private Line2D? _trailLine;
    private readonly Vector2[] _trailBuffer = new Vector2[GameConstants.ArrowTrailSegments + 1];
    private int _lastTrailVersion = -1;

    /// <summary>
    /// Triangle vertices in local space with the tip on +X, so setting
    /// <see cref="Node2D.Rotation"/> to the travel angle makes the tip the
    /// forward-most vertex. Pure and static so the orientation is testable.
    /// </summary>
    public static Vector2[] ArrowPolygon(float size)
    {
        float half = size / 2f;
        return new Vector2[]
        {
            new Vector2(half, 0f),       // tip, forward along +X
            new Vector2(-half, -half),   // rear top
            new Vector2(-half, half),    // rear bottom
        };
    }

    /// <summary>
    /// Outer outline triangle: the same shape grown by <paramref name="grow"/> pixels.
    /// </summary>
    public static Vector2[] OutlinePolygon(float size, float grow)
    {
        float half = size / 2f;
        return new Vector2[]
        {
            new Vector2(half + grow, 0f),
            new Vector2(-half - grow, -half - grow),
            new Vector2(-half - grow, half + grow),
        };
    }

    protected override void BuildVisual()
    {
        // Dark outline first, bright fill on top: two siblings so the fill is not
        // covered by the larger outline polygon.
        var outline = new Polygon2D();
        outline.Name = "ArrowOutline";
        outline.Polygon = OutlinePolygon(GameConstants.ProjectileSize, 1f);
        outline.Color = new Color(0.8f, 0.5f, 0.05f);
        AddChild(outline);

        var arrow = new Polygon2D();
        arrow.Name = "ArrowShape";
        arrow.Polygon = ArrowPolygon(GameConstants.ProjectileSize);
        arrow.Color = new Color(1f, 0.9f, 0.2f); // Yellow fill
        AddChild(arrow);
    }

    protected override ProjectileTrail? CreateTrail() => ProjectileTrail.CreateArrowTrail();

    protected override void BuildTrailVisual()
    {
        _trailLine = new Line2D();
        _trailLine.Name = "Trail";
        // TopLevel keeps the world-space samples independent of this projectile's
        // position and rotation, so the trail stays behind instead of following.
        _trailLine.TopLevel = true;
        _trailLine.ZIndex = -1;
        _trailLine.Width = GameConstants.ArrowTrailWidth;
        _trailLine.Antialiased = true;
        _trailLine.Visible = false;

        var gradient = new Gradient();
        gradient.SetColor(0, GameConstants.ArrowTrailColor with { A = 0f });
        gradient.SetColor(1, GameConstants.ArrowTrailColor with { A = 0.85f });
        _trailLine.Gradient = gradient;
        _trailLine.DefaultColor = GameConstants.ArrowTrailColor;

        AddChild(_trailLine);
    }

    protected override void UpdateTrailVisual()
    {
        if (_trailLine == null || !IsInstanceValid(_trailLine))
            return;

        int count = _trail?.Count ?? 0;
        if (count < 2)
        {
            _trailLine.Visible = false;
            _lastTrailVersion = -1;
            return;
        }

        // Only rebuild/assign the Line2D points when the sample history actually changed;
        // a frame where the trail did not push or drop a sample skips the copy entirely.
        if (_trail!.Version != _lastTrailVersion)
        {
            // Line2D draws the points in order; oldest first so the gradient fades the tail.
            for (int i = 0; i < _trailBuffer.Length; i++)
            {
                int source = count - 1 - i;
                _trailBuffer[i] = source >= 0
                    ? _trail.PositionAt(source)
                    : _trail.PositionAt(0);
            }

            _trailLine.Points = _trailBuffer;
            _lastTrailVersion = _trail.Version;
        }

        _trailLine.Visible = true;
    }

    protected override void OnHit(Enemy enemy)
    {
        // Apply single-target damage synchronously. This ensures that when two
        // projectiles hit the same enemy in one frame, the second hit correctly
        // sees the enemy as dead (via HitEnemy's IsDead guard). The firing tower is
        // passed through so the hit credits its kill XP (pierced hits included).
        float damage = _damage;

        // Independent per-hit crit roll: 2x the (already skill-boosted) damage.
        // Each pierced hit rolls its own crit via the tower's shared roll source.
        var tower = SourceTower;
        if (tower != null && tower.CritChance > 0f && tower.Rolls.Roll(tower.CritChance))
        {
            damage *= 2f;
            enemy.ApplyHitFlash();
        }

        enemy.TakeDamage(damage, source: tower);
    }
}
