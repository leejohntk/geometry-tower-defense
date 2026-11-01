using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Shared procedural tower glyphs: the same shapes the in-world towers use (arrow
/// triangle, cannon circle with barrel, laser diamond/emitter), drawn as small UI icons.
/// Used by the HUD placement cards and the skill-tree tower sidebar so the two never
/// drift apart. Everything is CanvasItem primitives — no textures, no assets.
/// </summary>
public static class TowerGlyphs
{
    /// <summary>Tower theme color (arrow blue, cannon green, laser magenta).</summary>
    public static Color ColorFor(TowerType type) => type switch
    {
        TowerType.Cannon => GameConstants.CannonTowerFillColor,
        TowerType.Laser => GameConstants.LaserTowerFillColor,
        _ => GameConstants.ArrowTowerFillColor
    };

    /// <summary>Dark outline color that matches the in-world body outline.</summary>
    public static Color OutlineFor(TowerType type) => type switch
    {
        TowerType.Cannon => GameConstants.CannonTowerOutlineColor,
        TowerType.Laser => GameConstants.LaserTowerOutlineColor,
        _ => GameConstants.ArrowTowerOutlineColor
    };

    /// <summary>
    /// Draws the tower's glyph inside <paramref name="radius"/> around
    /// <paramref name="center"/>. Only runs on icon build, never per frame.
    /// </summary>
    public static void Draw(
        CanvasItem target,
        TowerType type,
        Vector2 center,
        float radius,
        Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        Color outline = OutlineFor(type);

        switch (type)
        {
            case TowerType.Cannon:
                target.DrawCircle(center, radius * 0.78f, color);
                target.DrawCircle(center, radius * 0.78f, outline, false, 1.5f);
                // Directional barrel along +X, matching the in-world silhouette.
                var barrel = new Rect2(
                    center.X,
                    center.Y - radius * 0.2f,
                    radius * 0.95f,
                    radius * 0.4f);
                target.DrawRect(barrel, outline);
                target.DrawCircle(
                    new Vector2(center.X + radius * 0.95f, center.Y),
                    radius * 0.2f,
                    color);
                break;

            case TowerType.Laser:
                var diamond = new Vector2[]
                {
                    new Vector2(center.X, center.Y - radius),
                    new Vector2(center.X + radius, center.Y),
                    new Vector2(center.X, center.Y + radius),
                    new Vector2(center.X - radius, center.Y),
                };
                target.DrawColoredPolygon(diamond, color);
                target.DrawPolyline(
                    new[] { diamond[0], diamond[1], diamond[2], diamond[3], diamond[0] },
                    outline,
                    1.5f,
                    antialiased: true);
                target.DrawCircle(center, radius * 0.32f, new Color(0.95f, 0.8f, 1f));
                break;

            default:
                var triangle = new Vector2[]
                {
                    new Vector2(center.X, center.Y - radius),
                    new Vector2(center.X - radius * 0.85f, center.Y + radius * 0.82f),
                    new Vector2(center.X + radius * 0.85f, center.Y + radius * 0.82f),
                };
                target.DrawColoredPolygon(triangle, color);
                target.DrawPolyline(
                    new[] { triangle[0], triangle[1], triangle[2], triangle[0] },
                    outline,
                    1.5f,
                    antialiased: true);
                target.DrawLine(
                    new Vector2(center.X, center.Y - radius * 0.15f),
                    new Vector2(center.X, center.Y + radius * 0.6f),
                    outline,
                    1.5f);
                break;
        }
    }
}
