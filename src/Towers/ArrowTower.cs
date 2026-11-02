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
        Vector2 kickedPosition = _bodyBasePosition - aim * recoil;
        _body.Position = kickedPosition;

        if (_bowFlash != null && IsInstanceValid(_bowFlash))
        {
            // The chevron sits at the arrow tip, so it follows the same recoil kick
            // instead of staying behind while the body kicks back.
            _bowFlash.Position = kickedPosition;
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
    /// The arrow silhouette's three vertices in body-local space, tip first. It is an
    /// equilateral triangle inscribed in a circle centred on the cell centre, so its
    /// centroid and circumcentre coincide with the rotation pivot: the body spins in
    /// place while aiming instead of orbiting the cell and looking off-centre. The
    /// circumradius (cell/2 - 4) keeps every vertex inside the cell at every aim
    /// angle. Pure and static so the centering contract is testable without pixels.
    /// </summary>
    public static Vector2[] BodyPolygon(float cell)
    {
        float center = cell / 2f;
        float radius = cell / 2f - 4f; // same 4px inset as the cannon/laser bodies
        float halfBase = radius * Mathf.Cos(Mathf.Pi / 6f);
        float baseY = center + radius / 2f;
        return new[]
        {
            new Vector2(center, center - radius), // tip, pointing up at rest
            new Vector2(center - halfBase, baseY), // bottom-left
            new Vector2(center + halfBase, baseY), // bottom-right
        };
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
        Vector2[] body = BodyPolygon(cell);

        switch (Archetype)
        {
            case TowerArchetype.Pierce:
                DrawForkedSpearhead(cell, body);
                break;
            case TowerArchetype.Crit:
                DrawFilledTriangle(body[0], body[1], body[2]);
                DrawStar(new Vector2(cell / 2f, cell / 2f), 9f, 3f);
                break;
            default:
                DrawFilledTriangle(body[0], body[1], body[2]);
                break;
        }
    }

    /// <summary>
    /// Pierce: two narrow triangle halves with their own dark outlines and a visible
    /// ~6 px vertical slit between them (constant width from tip to base). Both prongs
    /// share the base triangle's tip and base heights, so their combined centroid is
    /// the cell centre just like the plain arrow.
    /// </summary>
    private void DrawForkedSpearhead(float cell, Vector2[] body)
    {
        float center = cell / 2f;
        const float slitHalfWidth = 3f;
        float innerLeft = center - slitHalfWidth;
        float innerRight = center + slitHalfWidth;
        float tipY = body[0].Y;
        float baseY = body[1].Y;

        var leftTip = new Vector2(innerLeft, tipY);
        var leftInnerBase = new Vector2(innerLeft, baseY);
        var leftOuterBase = new Vector2(body[1].X, baseY);
        DrawFilledTriangle(leftTip, leftOuterBase, leftInnerBase);

        var rightTip = new Vector2(innerRight, tipY);
        var rightInnerBase = new Vector2(innerRight, baseY);
        var rightOuterBase = new Vector2(body[2].X, baseY);
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
