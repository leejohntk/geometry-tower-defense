using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Arrow Tower: upward-pointing blue triangle.
/// Targets nearest enemy within 4-cell range, fires every 1.5s, deals 10 damage.
/// </summary>
public partial class ArrowTower : Tower
{
    public override TowerType Type => TowerType.Arrow;
    public override float Damage =>
        SkillStats.ArrowDamage(SkillRank(SkillTreeCatalog.ArrowDamage))
        * TowerUpgrades.DamageMultiplier(Rank);
    public override float FireRate =>
        TowerUpgrades.FireInterval(
            SkillStats.ArrowFireRate(SkillRank(SkillTreeCatalog.ArrowAttackSpeed)), Rank);
    public override int Cost => GameConstants.ArrowTowerCost;

    /// <summary>
    /// Pierce count. Seed mechanics are inert before rank 3: a rank 0-2 tower always
    /// reads zero, even with skill ranks in the seed. After choosing Pierce the count
    /// is max(1, skill rank); after choosing Crit it contributes zero.
    /// </summary>
    public override int PierceCount =>
        SkillMechanics.ArrowPierceCount(
            SeedRankFor(TowerArchetype.Pierce, SkillTreeCatalog.ArrowPierce));

    /// <summary>
    /// Crit chance. Inert (zero) before rank 3. After choosing Crit it is
    /// max(1, skill rank) x 0.08; after choosing Pierce it contributes zero.
    /// </summary>
    public override float CritChance =>
        SkillMechanics.ArrowCritChance(
            SeedRankFor(TowerArchetype.Crit, SkillTreeCatalog.ArrowCritChance));

    protected override Color RangeColor => GameConstants.ArrowTowerFillColor;

    /// <summary>
    /// The arrow rests pointing up (the triangle is drawn tip-up), so its resting
    /// aim angle is -90 degrees.
    /// </summary>
    public override float DefaultAimAngle => -Mathf.Pi / 2f;

    // The tower's own procedural body. The silhouette changes with the chosen
    // archetype (no separate accent overlay) while staying recognisably an arrow.
    private Control? _body;
    private Control? _bowFlash;
    private Vector2 _bodyBasePosition;

    private static readonly Color BodyFill = GameConstants.ArrowTowerFillColor;
    private static readonly Color BodyOutline = GameConstants.ArrowTowerOutlineColor;
    private const float OutlineWidth = 2f;

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;

        _bodyBasePosition = new Vector2(-cell / 2f, -cell / 2f);
        _body = new Control();
        _body.Name = "ArrowBody";
        _body.Size = new Vector2(cell, cell);
        _body.Position = _bodyBasePosition;
        // Rotate around the cell centre so the triangle reflects the aim direction
        // without moving the tower's simulation position.
        _body.PivotOffset = new Vector2(cell / 2f, cell / 2f);
        _body.MouseFilter = Control.MouseFilterEnum.Ignore;
        _body.Draw += DrawBody;
        AddChild(_body);

        // Short bow-flash cue shown only while a shot's recoil is playing. Repainted
        // only during that ~0.12 s window, never while idle.
        _bowFlash = new Control();
        _bowFlash.Name = "BowFlash";
        _bowFlash.Size = new Vector2(cell, cell);
        _bowFlash.Position = _bodyBasePosition;
        _bowFlash.MouseFilter = Control.MouseFilterEnum.Ignore;
        _bowFlash.Visible = false;
        _bowFlash.Draw += DrawBowFlash;
        AddChild(_bowFlash);

        OnAimUpdated();
    }

    protected override void OnArchetypeChanged()
    {
        _body?.QueueRedraw();
    }

    /// <summary>
    /// Applies the aim/recoil transforms: the body rotates to the aim angle (relative
    /// to its drawn, tip-up orientation) and kicks back along the aim axis while the
    /// recoil cue runs. Purely visual — Position is never written.
    /// </summary>
    protected override void OnAimUpdated()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        _body.Rotation = AimAngle - DefaultAimAngle;

        float recoil = GameConstants.TowerRecoilDistance * RecoilProgress;
        var aim = new Vector2(Mathf.Cos(AimAngle), Mathf.Sin(AimAngle));
        _body.Position = _bodyBasePosition - aim * recoil;

        if (_bowFlash != null && IsInstanceValid(_bowFlash))
        {
            _bowFlash.Visible = IsRecoiling;
            if (IsRecoiling)
                _bowFlash.QueueRedraw();
        }
    }

    /// <summary>
    /// Bow-flash: two short strokes forming a chevron just ahead of the arrow tip,
    /// opened perpendicular to the aim. Two DrawLine calls, so no per-frame array.
    /// </summary>
    private void DrawBowFlash()
    {
        if (_bowFlash == null || !IsInstanceValid(_bowFlash))
            return;

        float cell = GameConstants.CellSize;
        var center = new Vector2(cell / 2f, cell / 2f);
        var aim = new Vector2(Mathf.Cos(AimAngle), Mathf.Sin(AimAngle));
        var perp = new Vector2(-aim.Y, aim.X);
        float radius = GameConstants.TowerBowFlashRadius;
        Vector2 tip = center + aim * radius;
        Vector2 back = center + aim * (radius * 0.55f);
        Color color = GameConstants.TowerBowFlashColor with { A = RecoilProgress };

        _bowFlash.DrawLine(tip, back + perp * 9f, color, GameConstants.TowerBowFlashWidth);
        _bowFlash.DrawLine(tip, back - perp * 9f, color, GameConstants.TowerBowFlashWidth);
    }

    /// <summary>
    /// Paints the arrow silhouette for the current archetype:
    /// none = blue triangle with a dark outline; Pierce = forked spearhead split by a
    /// vertical slit; Crit = base triangle with a light four-point star inset.
    /// </summary>
    private void DrawBody()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        float cell = GameConstants.CellSize;
        Vector2 top = new(cell / 2f, 4f);
        Vector2 bottomLeft = new(4f, cell - 4f);
        Vector2 bottomRight = new(cell - 4f, cell - 4f);

        switch (Archetype)
        {
            case TowerArchetype.Pierce:
                DrawForkedSpearhead(cell);
                break;
            case TowerArchetype.Crit:
                DrawFilledTriangle(top, bottomLeft, bottomRight);
                DrawStar(new Vector2(cell / 2f, cell / 2f), 9f, 3f);
                break;
            default:
                DrawFilledTriangle(top, bottomLeft, bottomRight);
                break;
        }
    }

    /// <summary>
    /// Pierce: two narrow triangle halves with their own dark outlines and a visible
    /// ~6 px vertical slit between them (constant width from tip to base).
    /// </summary>
    private void DrawForkedSpearhead(float cell)
    {
        float center = cell / 2f;
        const float slitHalfWidth = 3f;
        float innerLeft = center - slitHalfWidth;
        float innerRight = center + slitHalfWidth;

        var leftTip = new Vector2(innerLeft, 4f);
        var leftInnerBase = new Vector2(innerLeft, cell - 4f);
        var leftOuterBase = new Vector2(4f, cell - 4f);
        DrawFilledTriangle(leftTip, leftOuterBase, leftInnerBase);

        var rightTip = new Vector2(innerRight, 4f);
        var rightInnerBase = new Vector2(innerRight, cell - 4f);
        var rightOuterBase = new Vector2(cell - 4f, cell - 4f);
        DrawFilledTriangle(rightTip, rightInnerBase, rightOuterBase);
    }

    private void DrawFilledTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        _body.DrawColoredPolygon(new[] { a, b, c }, BodyFill);
        _body.DrawPolyline(new[] { a, b, c, a }, BodyOutline, OutlineWidth, antialiased: true);
    }

    /// <summary>
    /// Crit: a light four-point star inset at the triangle's centre.
    /// </summary>
    private void DrawStar(Vector2 center, float outerRadius, float innerRadius)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        var points = new Vector2[8];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length - Mathf.Pi / 2f;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        _body.DrawColoredPolygon(points, new Color(0.88f, 0.94f, 1f));
    }
}
