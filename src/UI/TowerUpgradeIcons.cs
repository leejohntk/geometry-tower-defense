using System.Collections.Generic;
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
    /// Draws one thick '^' chevron inside <paramref name="slot"/>. The stroke is inset
    /// so it stays fully inside the slot and stacked chevrons never touch.
    /// </summary>
    public static void Draw(CanvasItem target, Rect2 slot, Color color, float stroke)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;
        if (!(slot.Size.X > 0f) || !(slot.Size.Y > 0f))
            return;

        float width = Mathf.Max(1f, stroke);
        float inset = width / 2f;
        float left = slot.Position.X + inset;
        float right = slot.End.X - inset;
        float top = slot.Position.Y + inset;
        float bottom = slot.End.Y - inset;
        if (right <= left || bottom <= top)
            return;

        target.DrawPolyline(
            new[]
            {
                new Vector2(left, bottom),
                new Vector2((left + right) / 2f, top),
                new Vector2(right, bottom)
            },
            color,
            width,
            antialiased: true);
    }
}

/// <summary>
/// Layout and drawing helpers for a fixed-size rank icon box. Every icon uses the
/// same box; only the number of chevrons changes, and the stack stays centred
/// vertically inside it. Chevrons are thick V polylines, never text glyphs.
/// </summary>
public static class RankIcon
{
    /// <summary>
    /// Vertical offset of the top chevron in a stack of <paramref name="count"/>
    /// chevrons centred inside a box of <paramref name="boxHeight"/>.
    /// </summary>
    public static float StackTop(float boxHeight, int count, float chevronHeight, float gap)
    {
        if (count <= 0)
            return 0f;

        float content = count * chevronHeight + (count - 1) * gap;
        return (boxHeight - content) / 2f;
    }

    /// <summary>
    /// Shows the first <paramref name="visibleCount"/> chevron controls and positions
    /// them as a centred vertical column: all visible children share x, with
    /// increasing y.
    /// </summary>
    public static void LayoutStack(
        IReadOnlyList<Control> chevrons,
        int visibleCount,
        float boxHeight,
        float chevronHeight,
        float gap)
    {
        int count = Mathf.Clamp(visibleCount, 0, chevrons.Count);
        float top = StackTop(boxHeight, count, chevronHeight, gap);

        for (int i = 0; i < chevrons.Count; i++)
        {
            var chevron = chevrons[i];
            bool visible = i < count;
            chevron.Visible = visible;
            if (!visible)
                continue;

            chevron.Position = new Vector2(0f, top + i * (chevronHeight + gap));
            chevron.QueueRedraw();
        }
    }

    /// <summary>
    /// Draws a centred vertical stack of <paramref name="count"/> thick V chevrons
    /// inside <paramref name="box"/> (used by the single-control tower badge).
    /// </summary>
    public static void DrawStack(
        CanvasItem target,
        Rect2 box,
        int count,
        Color color,
        float chevronHeight,
        float gap,
        float stroke)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || count <= 0)
            return;

        float top = StackTop(box.Size.Y, count, chevronHeight, gap);
        for (int i = 0; i < count; i++)
        {
            var slot = new Rect2(
                box.Position.X,
                box.Position.Y + top + i * (chevronHeight + gap),
                box.Size.X,
                chevronHeight);
            RankChevron.Draw(target, slot, color, stroke);
        }
    }

    /// <summary>
    /// True when a rank badge renders the archetype emblem instead of chevrons:
    /// only a fully specialized rank-3 tower with a chosen archetype qualifies.
    /// </summary>
    public static bool BadgeUsesEmblem(int rank, TowerArchetype archetype) =>
        rank >= GameConstants.TowerMaxRank && archetype != TowerArchetype.None;

    /// <summary>
    /// Draws the tiny current-rank badge content: a centred chevron stack for rank
    /// 1/2, or the chosen archetype emblem at rank 3, inside the fixed badge box.
    /// </summary>
    public static void DrawBadge(
        CanvasItem target,
        Rect2 box,
        int rank,
        TowerArchetype archetype,
        Color color)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (BadgeUsesEmblem(rank, archetype))
        {
            ArchetypeEmblem.Draw(
                target,
                archetype,
                box.GetCenter(),
                Mathf.Min(box.Size.X, box.Size.Y) / 2f * 0.85f,
                color);
            return;
        }

        DrawStack(
            target,
            box,
            Mathf.Max(0, rank),
            color,
            GameConstants.TowerRankBadgeChevronHeight,
            GameConstants.TowerRankBadgeChevronGap,
            GameConstants.TowerRankBadgeStroke);
    }
}
