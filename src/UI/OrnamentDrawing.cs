using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Shared procedural ornament primitives used by the title, result, and skill-tree
/// decorations: an angular corner bracket and a small diamond accent. Both are
/// allocation-free apart from the diamond's one small vertex array, and they are only
/// called from build-time decoration draws.
/// </summary>
public static class OrnamentDrawing
{
    /// <summary>Draws a right-angle corner bracket opening toward <paramref name="direction"/>.</summary>
    public static void DrawBracket(
        CanvasItem target,
        Vector2 corner,
        Vector2 direction,
        float size,
        Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        target.DrawLine(corner, corner + new Vector2(direction.X * size, 0f), color, 2f);
        target.DrawLine(corner, corner + new Vector2(0f, size), color, 2f);
    }

    /// <summary>Draws a small filled four-point diamond centered on <paramref name="center"/>.</summary>
    public static void DrawDiamond(CanvasItem target, Vector2 center, float radius, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        var points = new Vector2[]
        {
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X + radius, center.Y),
            new Vector2(center.X, center.Y + radius),
            new Vector2(center.X - radius, center.Y),
        };
        target.DrawColoredPolygon(points, color);
    }
}
