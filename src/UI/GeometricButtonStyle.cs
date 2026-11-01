using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Shared procedural button styling for the title menu, result screens, and the skill
/// tree's Buy buttons: flat geometric fills, colored borders, readable text, and clear
/// hover / pressed / focus / disabled states. StyleBoxes are built at UI-build or
/// refresh time only — never per frame.
/// </summary>
public static class GeometricButtonStyle
{
    /// <summary>Dark base fill used by menu-style buttons.</summary>
    public static readonly Color BaseFill = new(0.08f, 0.09f, 0.18f, 0.95f);

    /// <summary>
    /// Applies the shared geometric style to a menu-style button.
    /// </summary>
    public static void Apply(Button button, Color accent)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        var normal = MakeBox(BaseFill, WithAlpha(accent, 0.55f), 1, 6);
        var hover = MakeBox(
            new Color(
                Mathf.Lerp(BaseFill.R, accent.R, 0.22f),
                Mathf.Lerp(BaseFill.G, accent.G, 0.22f),
                Mathf.Lerp(BaseFill.B, accent.B, 0.22f),
                0.97f),
            accent,
            2,
            6);
        var pressed = MakeBox(
            new Color(BaseFill.R * 0.55f, BaseFill.G * 0.55f, BaseFill.B * 0.55f, 0.98f),
            accent,
            2,
            6);
        // Content margin shift makes the label visibly depress with the pressed fill.
        pressed.ContentMarginTop = 3f;
        pressed.ContentMarginBottom = -1f;

        var focus = MakeBox(new Color(0f, 0f, 0f, 0f), accent, 2, 6);
        focus.DrawCenter = false;

        var disabled = MakeBox(
            new Color(0.09f, 0.09f, 0.12f, 0.9f),
            new Color(0.32f, 0.32f, 0.38f, 0.8f),
            1,
            6);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.85f, 0.88f, 1f));
        button.AddThemeColorOverride("font_focus_color", new Color(1f, 1f, 1f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.45f, 0.45f, 0.5f));
    }

    /// <summary>
    /// Applies the skill-tree Buy-button style: a tower-colored rounded/beveled fill
    /// with a colored border, or a gold MAX treatment at max rank.
    /// </summary>
    public static void ApplyBuy(Button button, Color accent, bool maxed)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        Color fill = maxed
            ? new Color(0.32f, 0.25f, 0.04f, 0.98f)
            : new Color(accent.R * 0.3f, accent.G * 0.3f, accent.B * 0.3f, 0.98f);
        Color border = maxed ? GameConstants.SkillTreeMaxColor : accent;

        var normal = MakeBox(fill, border, maxed ? 2 : 1, 5);
        var hover = MakeBox(
            new Color(
                Mathf.Lerp(fill.R, 1f, 0.18f),
                Mathf.Lerp(fill.G, 1f, 0.18f),
                Mathf.Lerp(fill.B, 1f, 0.18f),
                1f),
            border,
            2,
            5);
        var pressed = MakeBox(
            new Color(fill.R * 0.6f, fill.G * 0.6f, fill.B * 0.6f, 1f),
            border,
            2,
            5);
        pressed.ContentMarginTop = 2f;

        var focus = MakeBox(new Color(0f, 0f, 0f, 0f), GameConstants.SkillTreeMaxColor, 2, 5);
        focus.DrawCenter = false;

        // A maxed button is not buyable, but its MAX treatment stays gold even in the
        // disabled state (a grey MAX would lose the rank-5 gold accent).
        var disabled = maxed
            ? MakeBox(
                new Color(0.24f, 0.19f, 0.03f, 0.95f),
                GameConstants.SkillTreeMaxColor with { A = 0.75f },
                1,
                5)
            : MakeBox(
                new Color(0.14f, 0.14f, 0.19f, 0.95f),
                new Color(0.3f, 0.3f, 0.36f, 0.9f),
                1,
                5);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeColorOverride("font_color", maxed
            ? GameConstants.SkillTreeMaxColor
            : new Color(0.95f, 0.97f, 1f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.85f, 0.88f, 1f));
        button.AddThemeColorOverride("font_focus_color", new Color(1f, 1f, 1f));
        button.AddThemeColorOverride(
            "font_disabled_color",
            maxed ? GameConstants.SkillTreeMaxColor with { A = 0.85f } : new Color(0.42f, 0.42f, 0.48f));
    }

    /// <summary>
    /// Applies the HUD placement-card style: dark fill, tower-colored accent border, a
    /// brightened hover/focus state, and a dim disabled state. Cards carry no Button
    /// text (Icon/Name/Cost children do), so no font overrides are applied here.
    /// </summary>
    public static void ApplyCard(Button button, Color accent)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        var normal = MakeBox(new Color(0.09f, 0.1f, 0.2f, 0.95f), accent with { A = 0.7f }, 1, 6);
        var hover = MakeBox(
            new Color(
                Mathf.Lerp(0.09f, accent.R, 0.25f),
                Mathf.Lerp(0.1f, accent.G, 0.25f),
                Mathf.Lerp(0.2f, accent.B, 0.25f),
                1f),
            accent,
            2,
            6);
        var disabled = MakeBox(
            new Color(0.07f, 0.07f, 0.12f, 0.9f),
            new Color(0.25f, 0.25f, 0.32f, 0.9f),
            1,
            6);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", normal);
        button.AddThemeStyleboxOverride("focus", hover);
        button.AddThemeStyleboxOverride("disabled", disabled);
    }

    /// <summary>
    /// Adds a click-through accent bar down the left edge of a menu button. The bar is
    /// decorative only: it never becomes a second mouse target.
    /// </summary>
    public static Control AddAccentBar(Button button, Color accent, float width = 4f, float inset = 8f)
    {
        var bar = new Control();
        bar.Name = "Accent";
        bar.Position = new Vector2(inset, inset);
        bar.Size = new Vector2(width, Mathf.Max(0f, button.Size.Y - inset * 2f));
        bar.MouseFilter = Control.MouseFilterEnum.Ignore;
        bar.Draw += () =>
        {
            if (!GodotObject.IsInstanceValid(bar))
                return;
            bar.DrawRect(new Rect2(Vector2.Zero, bar.Size), accent with { A = 0.85f });
            bar.DrawRect(
                new Rect2(Vector2.Zero, new Vector2(bar.Size.X * 0.45f, bar.Size.Y)),
                new Color(1f, 1f, 1f, 0.35f));
        };
        button.AddChild(bar);
        return bar;
    }

    private static StyleBoxFlat MakeBox(Color fill, Color border, int borderWidth, int cornerRadius)
    {
        var box = new StyleBoxFlat();
        box.BgColor = fill;
        box.BorderColor = border;
        box.SetBorderWidthAll(borderWidth);
        box.SetCornerRadiusAll(cornerRadius);
        return box;
    }

    private static Color WithAlpha(Color color, float alpha) => color with { A = alpha };
}
