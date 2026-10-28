using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Procedural geometric marks for tower upgrade visuals. Everything here is drawn with
/// Godot's CanvasItem primitives (no sprites, no textures, no external assets), and is
/// only painted when its owner redraws — never per frame.
/// </summary>
public static class ArchetypeEmblem
{
    /// <summary>
    /// Draws the distinct emblem for an archetype inside a radius around
    /// <paramref name="center"/>. Unknown/none draws nothing.
    /// </summary>
    public static void Draw(CanvasItem target, TowerArchetype archetype, Vector2 center, float radius, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(radius > 0f))
            return;

        switch (archetype)
        {
            case TowerArchetype.Pierce:
                DrawPierce(target, center, radius, color);
                break;
            case TowerArchetype.Crit:
                DrawCrit(target, center, radius, color);
                break;
            case TowerArchetype.Cluster:
                DrawCluster(target, center, radius, color);
                break;
            case TowerArchetype.Stun:
                DrawStun(target, center, radius, color);
                break;
            case TowerArchetype.Chain:
                DrawChain(target, center, radius, color);
                break;
            case TowerArchetype.RampUp:
                DrawRampUp(target, center, radius, color);
                break;
        }
    }

    /// <summary>Pierce: a double chevron (arrow trail) pointing right.</summary>
    private static void DrawPierce(CanvasItem target, Vector2 center, float radius, Color color)
    {
        float stroke = Mathf.Max(1f, radius * 0.2f);
        for (int i = 0; i < 2; i++)
        {
            float x = center.X + (i - 1) * radius * 0.5f;
            target.DrawPolyline(
                new[]
                {
                    new Vector2(x - radius * 0.3f, center.Y - radius * 0.55f),
                    new Vector2(x + radius * 0.25f, center.Y),
                    new Vector2(x - radius * 0.3f, center.Y + radius * 0.55f)
                },
                color,
                stroke,
                antialiased: true);
        }
    }

    /// <summary>Crit: a four-point star.</summary>
    private static void DrawCrit(CanvasItem target, Vector2 center, float radius, Color color)
    {
        var points = new Vector2[8];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length - Mathf.Pi / 2f;
            float length = i % 2 == 0 ? radius : radius * 0.32f;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
        }
        target.DrawPolygon(points, new[] { color });
    }

    /// <summary>Cluster: three small circles in a triangle.</summary>
    private static void DrawCluster(CanvasItem target, Vector2 center, float radius, Color color)
    {
        float nodeRadius = radius * 0.30f;
        for (int i = 0; i < 3; i++)
        {
            float angle = Mathf.Tau * i / 3f - Mathf.Pi / 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.5f;
            target.DrawCircle(center + offset, nodeRadius, color);
        }
    }

    /// <summary>Stun: concentric rings.</summary>
    private static void DrawStun(CanvasItem target, Vector2 center, float radius, Color color)
    {
        float stroke = Mathf.Max(1f, radius * 0.18f);
        target.DrawCircle(center, radius * 0.85f, color, false, stroke);
        target.DrawCircle(center, radius * 0.45f, color, false, stroke);
    }

    /// <summary>Chain: three linked nodes.</summary>
    private static void DrawChain(CanvasItem target, Vector2 center, float radius, Color color)
    {
        float nodeRadius = radius * 0.22f;
        var points = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float angle = Mathf.Tau * i / 3f - Mathf.Pi / 2f;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.55f;
        }

        float stroke = Mathf.Max(1f, radius * 0.14f);
        target.DrawLine(points[0], points[1], color, stroke);
        target.DrawLine(points[1], points[2], color, stroke);
        target.DrawLine(points[2], points[0], color, stroke);

        foreach (var point in points)
            target.DrawCircle(point, nodeRadius, color);
    }

    /// <summary>Ramp-Up: a rising wedge.</summary>
    private static void DrawRampUp(CanvasItem target, Vector2 center, float radius, Color color)
    {
        var points = new[]
        {
            new Vector2(center.X - radius * 0.85f, center.Y + radius * 0.75f),
            new Vector2(center.X + radius * 0.85f, center.Y + radius * 0.75f),
            new Vector2(center.X + radius * 0.85f, center.Y - radius * 0.75f)
        };
        target.DrawPolygon(points, new[] { color });
    }
}

/// <summary>
/// Procedural caret/chevron used for rank indicators. One control per chevron keeps
/// "rank N = N filled chevrons" directly inspectable in the scene tree.
/// </summary>
public static class RankChevron
{
    /// <summary>
    /// Draws one '^' caret inside a square of <paramref name="size"/> at
    /// <paramref name="topLeft"/>.
    /// </summary>
    public static void Draw(CanvasItem target, Vector2 topLeft, float size, Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || !(size > 0f))
            return;

        target.DrawPolyline(
            new[]
            {
                new Vector2(topLeft.X, topLeft.Y + size * 0.78f),
                new Vector2(topLeft.X + size / 2f, topLeft.Y + size * 0.28f),
                new Vector2(topLeft.X + size, topLeft.Y + size * 0.78f)
            },
            color,
            GameConstants.RankChevronStroke,
            antialiased: true);
    }
}
