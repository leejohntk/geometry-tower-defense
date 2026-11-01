using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Procedural HUD stat icons (health shield, coin, wave chevrons). Everything is drawn
/// with CanvasItem primitives and antialiasing — no textures or assets — and each icon
/// is painted once when its 28x28 Control is built, never per frame.
/// </summary>
public static class HudIcons
{
    /// <summary>Health: a red shield-like hexagon with an inner cross.</summary>
    public static void DrawHealth(CanvasItem target, Vector2 center, float radius, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        float r = radius * 0.8f;
        var hex = new Vector2[6];
        for (int i = 0; i < hex.Length; i++)
        {
            float angle = -Mathf.Pi / 2f + Mathf.Tau * i / hex.Length;
            hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }

        target.DrawColoredPolygon(hex, color with { A = 0.22f });
        OutlinePolygon(target, hex, color);

        float arm = r * 0.45f;
        target.DrawLine(
            center - new Vector2(0f, arm),
            center + new Vector2(0f, arm),
            color,
            2f,
            antialiased: true);
        target.DrawLine(
            center - new Vector2(arm, 0f),
            center + new Vector2(arm, 0f),
            color,
            2f,
            antialiased: true);
    }

    /// <summary>Coins: a gold filled circle with an inner diamond and a highlight.</summary>
    public static void DrawCoin(CanvasItem target, Vector2 center, float radius, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        float r = radius * 0.75f;
        target.DrawCircle(center, r, color);
        OutlineCircle(target, center, r, color);

        Color inner = color.Lerp(new Color(0.25f, 0.15f, 0.02f), 0.45f);
        var diamond = new Vector2[]
        {
            center + new Vector2(0f, -r * 0.5f),
            center + new Vector2(r * 0.5f, 0f),
            center + new Vector2(0f, r * 0.5f),
            center + new Vector2(-r * 0.5f, 0f),
        };
        target.DrawColoredPolygon(diamond, inner);
        OutlinePolygon(target, diamond, inner);

        var highlight = center + new Vector2(-r * 0.35f, -r * 0.35f);
        target.DrawCircle(highlight, r * 0.18f, new Color(1f, 1f, 1f, 0.8f));
        OutlineCircle(target, highlight, r * 0.18f, new Color(1f, 1f, 1f, 0.8f));
    }

    /// <summary>Wave: three ascending blue chevrons.</summary>
    public static void DrawWave(CanvasItem target, Vector2 center, float radius, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        float stroke = radius * 0.16f;
        float half = radius * 0.26f;
        float rise = radius * 0.3f;
        for (int i = 0; i < 3; i++)
        {
            float cx = center.X - radius * 0.42f + i * radius * 0.36f;
            float cy = center.Y + radius * 0.28f - i * radius * 0.28f;
            target.DrawPolyline(
                new[]
                {
                    new Vector2(cx - half, cy),
                    new Vector2(cx, cy - rise),
                    new Vector2(cx + half, cy),
                },
                color,
                stroke,
                antialiased: true);
        }
    }

    private static void OutlinePolygon(CanvasItem target, Vector2[] points, Color color)
    {
        if (points.Length == 0)
            return;

        var closed = new Vector2[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            closed[i] = points[i];
        closed[points.Length] = points[0];
        target.DrawPolyline(closed, color, 2f, antialiased: true);
    }

    private static void OutlineCircle(CanvasItem target, Vector2 center, float radius, Color color)
    {
        target.DrawCircle(center, radius, color, false, 2f, antialiased: true);
    }
}
