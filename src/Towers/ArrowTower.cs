using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Arrow Tower: upward-pointing blue triangle.
/// Targets nearest enemy within 4-cell range, fires every 1.5s, deals 10 damage.
/// Range is shown as a circular indicator only when toggled by the player.
/// </summary>
public partial class ArrowTower : Node2D
{
    // Signal emitted when this tower fires a projectile
    [Signal]
    public delegate void ProjectileFiredEventHandler(ArrowTower tower, Vector2 targetPosition, Enemy targetEnemy);

    private float _fireCooldownTimer = 0f;
    private bool _canFire = true;
    private Area2D? _rangeArea;
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
    /// Tower range in cells.
    /// </summary>
    public int RangeCells => GameConstants.ArrowTowerRange;

    /// <summary>
    /// Damage per shot.
    /// </summary>
    public int Damage => GameConstants.ArrowTowerDamage;

    /// <summary>
    /// Fire rate in seconds between shots.
    /// </summary>
    public float FireRate => GameConstants.ArrowTowerFireRate;

    /// <summary>
    /// The current target enemy, if any.
    /// </summary>
    public Enemy? CurrentTarget { get; private set; } = null;

    public override void _Ready()
    {
        // Draw the tower as a blue upward-pointing triangle
        var trianglePoints = new Vector2[]
        {
            new Vector2(GameConstants.CellSize / 2f, 4),                        // Top center
            new Vector2(4, GameConstants.CellSize - 4),                         // Bottom left
            new Vector2(GameConstants.CellSize - 4, GameConstants.CellSize - 4) // Bottom right
        };

        var triangle = new Polygon2D();
        triangle.Polygon = trianglePoints;
        triangle.Color = new Color(0.2f, 0.5f, 1.0f); // Blue fill
        AddChild(triangle);

        // Draw outline
        var outline = new Polygon2D();
        outline.Polygon = trianglePoints;
        outline.Color = new Color(0.1f, 0.2f, 0.6f); // Dark blue outline
        outline.Offset = new Vector2(0, 0);
        // We layer a slightly smaller one behind for outline effect
        var outlineBg = new Polygon2D();
        outlineBg.Polygon = new Vector2[]
        {
            new Vector2(GameConstants.CellSize / 2f, 2),
            new Vector2(2, GameConstants.CellSize - 2),
            new Vector2(GameConstants.CellSize - 2, GameConstants.CellSize - 2)
        };
        outlineBg.Color = new Color(0.05f, 0.1f, 0.4f);
        triangle.AddChild(outlineBg);

        // Range indicator is NOT shown by default — created on demand via ShowRange()
        _rangeIndicator = null;
    }

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
        float rangePixels = GameConstants.CellDistanceInPixels(RangeCells);
        return Position.DistanceSquaredTo(enemy.Position) <= rangePixels * rangePixels;
    }

    /// <summary>
    /// Tries to fire at the current target. Returns the direction vector if firing, null otherwise.
    /// </summary>
    public bool TryFire(Enemy target, out Vector2 targetPos)
    {
        targetPos = Vector2.Zero;

        if (!_canFire) return false;
        if (target == null || target.IsDead) return false;
        if (!IsTargetInRange(target)) return false;

        _canFire = false;
        _fireCooldownTimer = FireRate;
        targetPos = target.Position;

        EmitSignal(SignalName.ProjectileFired, this, targetPos, target);
        return true;
    }

    /// <summary>
    /// Show a circular range indicator centered on this tower.
    /// No-op if already shown.
    /// </summary>
    public void ShowRange()
    {
        if (_rangeIndicator != null) return;

        float rangePixels = GameConstants.CellDistanceInPixels(RangeCells);

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
                new Color(0.2f, 0.5f, 1.0f, 0.1f)
            );

            // Border circle
            _rangeIndicator.DrawCircle(
                center,
                radius,
                new Color(0.2f, 0.5f, 1.0f, 0.4f),
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
        if (!_canFire)
        {
            _fireCooldownTimer -= (float)delta;
            if (_fireCooldownTimer <= 0f)
            {
                _canFire = true;
                _fireCooldownTimer = 0f;
            }
        }
    }
}
