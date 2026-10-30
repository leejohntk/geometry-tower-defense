using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Distinct glyph drawn inside a skill-tree node icon. Trunk nodes get a stat glyph;
/// seed nodes reuse the existing <see cref="ArchetypeEmblem"/> marks so the skill tree
/// and the in-level archetype badges share one visual language.
/// </summary>
public enum SkillGlyphKind
{
    Blade,
    SpeedLines,
    Crosshair,
    Shell,
    ConcentricRings,
    Beam,
    BeamCapped,
    Flame,
    Seed
}

/// <summary>
/// Procedural drawing for skill-tree node glyphs, plus the pure id-to-glyph mapping.
/// Glyphs are drawn once when an icon is built (never per frame), and the mapping is
/// pure so every node's glyph identity is unit-testable without a scene.
/// </summary>
public static class SkillGlyphs
{
    /// <summary>
    /// The glyph category for a catalog node id. Every trunk node has its own required
    /// glyph; seed nodes report <see cref="SkillGlyphKind.Seed"/>.
    /// </summary>
    public static SkillGlyphKind For(string nodeId) => nodeId switch
    {
        SkillTreeCatalog.ArrowDamage => SkillGlyphKind.Blade,
        SkillTreeCatalog.ArrowAttackSpeed => SkillGlyphKind.SpeedLines,
        SkillTreeCatalog.ArrowRange => SkillGlyphKind.Crosshair,
        SkillTreeCatalog.CannonPowderCharge => SkillGlyphKind.Shell,
        SkillTreeCatalog.CannonAttackSpeed => SkillGlyphKind.SpeedLines,
        SkillTreeCatalog.CannonSplashRadius => SkillGlyphKind.ConcentricRings,
        SkillTreeCatalog.LaserDps => SkillGlyphKind.Beam,
        SkillTreeCatalog.LaserRange => SkillGlyphKind.BeamCapped,
        SkillTreeCatalog.LaserIgnite => SkillGlyphKind.Flame,
        _ => SkillGlyphKind.Seed
    };

    /// <summary>
    /// Archetype whose emblem a seed node reuses, or null for a trunk/unknown node.
    /// </summary>
    public static TowerArchetype? SeedArchetype(string nodeId) => nodeId switch
    {
        SkillTreeCatalog.ArrowPierce => TowerArchetype.Pierce,
        SkillTreeCatalog.ArrowCritChance => TowerArchetype.Crit,
        SkillTreeCatalog.CannonCluster => TowerArchetype.Cluster,
        SkillTreeCatalog.CannonStunChance => TowerArchetype.Stun,
        SkillTreeCatalog.LaserChain => TowerArchetype.Chain,
        SkillTreeCatalog.LaserRampUp => TowerArchetype.RampUp,
        _ => null
    };

    /// <summary>
    /// Draws the node's glyph inside <paramref name="radius"/> around
    /// <paramref name="center"/>. Locked/maxed styling is layered by the caller.
    /// </summary>
    public static void Draw(
        CanvasItem target,
        string nodeId,
        Vector2 center,
        float radius,
        Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        switch (For(nodeId))
        {
            case SkillGlyphKind.Seed:
                var archetype = SeedArchetype(nodeId);
                if (archetype.HasValue)
                    ArchetypeEmblem.Draw(target, archetype.Value, center, radius, color);
                else
                    target.DrawCircle(center, radius * 0.5f, color);
                break;

            case SkillGlyphKind.Blade:
                DrawBlade(target, center, radius, color);
                break;

            case SkillGlyphKind.SpeedLines:
                DrawSpeedLines(target, center, radius, color);
                break;

            case SkillGlyphKind.Crosshair:
                DrawCrosshair(target, center, radius, color);
                break;

            case SkillGlyphKind.Shell:
                DrawShell(target, center, radius, color);
                break;

            case SkillGlyphKind.ConcentricRings:
                DrawConcentricRings(target, center, radius, color);
                break;

            case SkillGlyphKind.Beam:
                DrawBeam(target, center, radius, color, capped: false);
                break;

            case SkillGlyphKind.BeamCapped:
                DrawBeam(target, center, radius, color, capped: true);
                break;

            case SkillGlyphKind.Flame:
                DrawFlame(target, center, radius, color);
                break;
        }
    }

    /// <summary>Damage: an upward blade/triangle.</summary>
    private static void DrawBlade(CanvasItem target, Vector2 center, float radius, Color color)
    {
        var points = new[]
        {
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X - radius * 0.55f, center.Y + radius * 0.75f),
            new Vector2(center.X + radius * 0.55f, center.Y + radius * 0.75f),
        };
        target.DrawColoredPolygon(points, color);
    }

    /// <summary>Attack Speed: three speed lines, unequal lengths.</summary>
    private static void DrawSpeedLines(CanvasItem target, Vector2 center, float radius, Color color)
    {
        target.DrawLine(
            new Vector2(center.X - radius * 0.85f, center.Y - radius * 0.55f),
            new Vector2(center.X + radius * 0.55f, center.Y - radius * 0.55f),
            color,
            1.5f);
        target.DrawLine(
            new Vector2(center.X - radius * 0.5f, center.Y),
            new Vector2(center.X + radius * 0.9f, center.Y),
            color,
            1.5f);
        target.DrawLine(
            new Vector2(center.X - radius * 0.85f, center.Y + radius * 0.55f),
            new Vector2(center.X + radius * 0.55f, center.Y + radius * 0.55f),
            color,
            1.5f);
    }

    /// <summary>Range: crosshair (ring plus four ticks).</summary>
    private static void DrawCrosshair(CanvasItem target, Vector2 center, float radius, Color color)
    {
        target.DrawCircle(center, radius * 0.55f, color, false, 1.5f);
        DrawTick(target, center, radius, color, Vector2.Up);
        DrawTick(target, center, radius, color, Vector2.Down);
        DrawTick(target, center, radius, color, Vector2.Left);
        DrawTick(target, center, radius, color, Vector2.Right);
    }

    private static void DrawTick(
        CanvasItem target,
        Vector2 center,
        float radius,
        Color color,
        Vector2 direction)
    {
        target.DrawLine(
            center + direction * (radius * 0.72f),
            center + direction * radius,
            color,
            1.5f);
    }

    /// <summary>Powder Charge: a filled shell with a spark.</summary>
    private static void DrawShell(CanvasItem target, Vector2 center, float radius, Color color)
    {
        var shell = new Vector2(center.X - radius * 0.25f, center.Y + radius * 0.1f);
        target.DrawCircle(shell, radius * 0.52f, color);
        target.DrawCircle(shell, radius * 0.52f, new Color(0.05f, 0.05f, 0.1f), false, 1f);

        var spark = new Vector2(center.X + radius * 0.45f, center.Y - radius * 0.5f);
        target.DrawLine(spark, spark + new Vector2(radius * 0.45f, -radius * 0.35f), color, 1.5f);
        target.DrawLine(spark, spark + new Vector2(radius * 0.35f, -radius * 0.05f), color, 1.5f);
    }

    /// <summary>
    /// Splash Radius: concentric rings. Three rings plus a centre dot keep this
    /// distinguishable from the Stun emblem's two plain rings.
    /// </summary>
    private static void DrawConcentricRings(CanvasItem target, Vector2 center, float radius, Color color)
    {
        target.DrawCircle(center, radius * 0.3f, color, false, 1.2f);
        target.DrawCircle(center, radius * 0.6f, color, false, 1.2f);
        target.DrawCircle(center, radius * 0.9f, color, false, 1.2f);
        target.DrawCircle(center, radius * 0.12f, color);

        // Four diagonal splash ticks outside the rings: the extra ring + ticks keep this
        // unmistakably different from the Stun emblem's two plain rings.
        for (int i = 0; i < 4; i++)
        {
            float angle = Mathf.Pi / 4f + Mathf.Pi / 2f * i;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            target.DrawLine(
                center + direction * (radius * 0.98f),
                center + direction * (radius * 1.15f),
                color,
                1.2f);
        }
    }

    /// <summary>
    /// Laser DPS (dots at both ends) and Laser Range (T-shaped end caps): two related
    /// beam glyphs that are still distinguishable at a glance.
    /// </summary>
    private static void DrawBeam(CanvasItem target, Vector2 center, float radius, Color color, bool capped)
    {
        Vector2 left = center + new Vector2(-radius * 0.85f, 0f);
        Vector2 right = center + new Vector2(radius * 0.85f, 0f);
        target.DrawLine(left, right, color, 1.5f);

        if (capped)
        {
            target.DrawLine(
                left + new Vector2(0f, -radius * 0.35f),
                left + new Vector2(0f, radius * 0.35f),
                color,
                1.5f);
            target.DrawLine(
                right + new Vector2(0f, -radius * 0.35f),
                right + new Vector2(0f, radius * 0.35f),
                color,
                1.5f);
        }
        else
        {
            target.DrawCircle(left, radius * 0.2f, color);
            target.DrawCircle(right, radius * 0.2f, color);
        }
    }

    /// <summary>Ignite: a rising flame/teardrop wedge.</summary>
    private static void DrawFlame(CanvasItem target, Vector2 center, float radius, Color color)
    {
        var points = new[]
        {
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X + radius * 0.65f, center.Y + radius * 0.2f),
            new Vector2(center.X, center.Y + radius * 0.8f),
            new Vector2(center.X - radius * 0.65f, center.Y + radius * 0.2f),
        };
        target.DrawColoredPolygon(points, color);
    }
}
