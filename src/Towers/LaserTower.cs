using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Laser Tower: magenta/purple emitter that drains a single nearest target continuously.
/// No projectile and no cooldown — GameManager applies Dps every frame against the target
/// already computed by its centralized targeting pass. Damage ignores enemy armor.
/// </summary>
public partial class LaserTower : Tower
{
    public override TowerType Type => TowerType.Laser;
    public override int RangeCells => GameConstants.LaserTowerRange;
    public override int Damage => 0;
    public override float FireRate => 0f;
    public override int Cost => GameConstants.LaserTowerCost;
    public override float Dps => GameConstants.LaserTowerDps;
    public override bool IsContinuous => true;
    protected override Color RangeColor => new Color(0.8f, 0.3f, 1.0f);

    private Line2D? _beam;

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4;

        // Magenta/purple emitter body.
        var body = new Control();
        body.Name = "LaserEmitter";
        body.Size = new Vector2(cell, cell);
        body.Position = new Vector2(-cell / 2f, -cell / 2f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.Draw += () =>
        {
            if (!IsInstanceValid(body)) return;

            Vector2 center = new Vector2(cell / 2f, cell / 2f);

            body.DrawCircle(center, radius, new Color(0.8f, 0.3f, 1.0f));            // Magenta fill
            body.DrawCircle(center, radius, new Color(0.4f, 0.1f, 0.6f), false, 2.0f); // Darker outline
            body.DrawCircle(center, radius * 0.4f, new Color(0.5f, 0.15f, 0.8f));    // Inner core
        };
        AddChild(body);

        // Beam line drawn from the tower center to the current target.
        _beam = new Line2D();
        _beam.Name = "Beam";
        _beam.Width = 3f;
        _beam.DefaultColor = new Color(0.9f, 0.4f, 1.0f, 0.9f);
        _beam.Points = new Vector2[] { Vector2.Zero, Vector2.Zero };
        _beam.Visible = false;
        AddChild(_beam);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_beam == null)
            return;

        var target = CurrentTarget;
        if (target != null && !target.IsDead && IsTargetInRange(target))
        {
            // Beam endpoints are in this node's local space; the tower is at its origin.
            _beam.SetPointPosition(0, Vector2.Zero);
            _beam.SetPointPosition(1, target.Position - Position);
            _beam.Visible = true;
        }
        else
        {
            _beam.Visible = false;
        }
    }
}
