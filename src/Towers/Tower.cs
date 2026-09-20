using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Base class for all tower variants. Owns targeting, cooldown, and range display.
/// Variants supply their stats (range, damage, fire rate, cost) and visuals.
/// </summary>
public abstract partial class Tower : Node2D
{
    private float _fireCooldownTimer = 0f;
    private Control? _rangeIndicator;

    /// <summary>
    /// Grid row position of this tower.
    /// </summary>
    public int GridRow { get; private set; }

    /// <summary>
    /// Grid column position of this tower.
    /// </summary>
    public int GridCol { get; private set; }

    /// <summary>
    /// The current target enemy, if any.
    /// </summary>
    public Enemy? CurrentTarget { get; private set; } = null;

    public abstract TowerType Type { get; }
    public abstract int RangeCells { get; }
    public abstract int Damage { get; }
    public abstract float FireRate { get; }
    public abstract int Cost { get; }
    protected abstract Color RangeColor { get; }

    /// <summary>
    /// Continuous damage per second for drain-style towers. Zero for discrete-fire towers.
    /// </summary>
    public virtual float Dps => 0f;

    /// <summary>
    /// True if this tower damages continuously every frame (no projectile, no cooldown).
    /// </summary>
    public virtual bool IsContinuous => false;

    /// <summary>
    /// Tower range in pixels.
    /// </summary>
    public float RangePixels => GameConstants.CellDistanceInPixels(RangeCells);

    /// <summary>
    /// Initialize tower at a specific grid position.
    /// </summary>
    public void Initialize(int gridRow, int gridCol)
    {
        GridRow = gridRow;
        GridCol = gridCol;
        Position = new Vector2(
            GameConstants.CellCenterX(gridCol),
            GameConstants.CellCenterY(gridRow)
        );
    }

    /// <summary>
    /// Sets the current target for this tower. Called by GameManager during targeting phase.
    /// </summary>
    public void SetTarget(Enemy? enemy)
    {
        CurrentTarget = enemy;
    }

    /// <summary>
    /// Returns true if a target is within range, based on pixel distance (not grid-snapped).
    /// </summary>
    public bool IsTargetInRange(Enemy enemy)
    {
        float rangePixels = RangePixels;
        return Position.DistanceSquaredTo(enemy.Position) <= rangePixels * rangePixels;
    }

    /// <summary>
    /// Tries to fire at the current target. Returns true and the target position if firing.
    /// </summary>
    public bool TryFire(Enemy target, out Vector2 targetPos)
    {
        targetPos = Vector2.Zero;

        if (_fireCooldownTimer > 0f) return false;
        if (target == null || target.IsDead) return false;
        if (!IsTargetInRange(target)) return false;

        _fireCooldownTimer = FireRate;
        targetPos = target.Position;

        return true;
    }

    /// <summary>
    /// Show a circular range indicator centered on this tower.
    /// No-op if already shown.
    /// </summary>
    public void ShowRange()
    {
        if (_rangeIndicator != null) return;

        float rangePixels = RangePixels;

        _rangeIndicator = new Control();
        _rangeIndicator.Name = "RangeIndicator";
        _rangeIndicator.Size = new Vector2(rangePixels * 2, rangePixels * 2);
        _rangeIndicator.Position = new Vector2(-rangePixels, -rangePixels);

        _rangeIndicator.Draw += () =>
        {
            if (_rangeIndicator == null) return;

            float radius = rangePixels;
            Vector2 center = new Vector2(rangePixels, rangePixels);

            // Filled semi-transparent circle
            _rangeIndicator.DrawCircle(
                center,
                radius,
                RangeColor with { A = 0.1f }
            );

            // Border circle
            _rangeIndicator.DrawCircle(
                center,
                radius,
                RangeColor with { A = 0.4f },
                false,
                2.0f
            );
        };

        AddChild(_rangeIndicator);
    }

    /// <summary>
    /// Hide and remove the circular range indicator.
    /// No-op if already hidden.
    /// </summary>
    public void HideRange()
    {
        if (_rangeIndicator == null) return;
        _rangeIndicator.QueueFree();
        _rangeIndicator = null;
    }

    /// <summary>
    /// Toggle the circular range indicator on or off.
    /// </summary>
    public void ToggleRange()
    {
        if (_rangeIndicator != null)
            HideRange();
        else
            ShowRange();
    }

    public override void _Process(double delta)
    {
        if (_fireCooldownTimer > 0f)
        {
            _fireCooldownTimer -= (float)delta;
            if (_fireCooldownTimer < 0f)
                _fireCooldownTimer = 0f;
        }
    }
}
