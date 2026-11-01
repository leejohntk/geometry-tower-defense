using Godot;
using System;

namespace GeometryTowerDefense;

/// <summary>
/// Title screen with game name and level selection buttons.
///
/// Presentation: a full-viewport procedural backdrop (rotating geometric rings, a
/// drifting dot field, and a soft glow behind the title), a decorative frame around the
/// title labels, and geometrically styled menu buttons. Everything is drawn with
/// CanvasItem primitives; idle motion is node transforms driven by a single _Process
/// that stops while the screen is hidden.
/// </summary>
public partial class TitleScreen : Control
{
    [Signal]
    public delegate void LevelSelectedEventHandler(int levelId);

    [Signal]
    public delegate void SkillTreePressedEventHandler();

    // Menu column geometry. Five level buttons plus the Skill Tree button share one
    // uniform stack; the block is sized from these constants so adding a button can
    // never silently push one outside the block or overlap its neighbour.
    private const float MenuButtonWidth = 240f;
    private const float MenuButtonHeight = 50f;
    private const float MenuButtonGap = 12f;
    private const float MenuButtonPitch = MenuButtonHeight + MenuButtonGap;
    private const int MenuButtonCount = 6;
    private const float MenuTopOffsetFromCenter = 10f;
    private const float InstructionsGap = 16f;

    // Backdrop animation state (transform-only; the drawings themselves are static).
    private float _backdropTime = 0f;
    private Control? _ringDecor;
    private Control? _dotField;
    private Control? _titleGlow;

    /// <summary>
    /// Seconds of backdrop animation elapsed. Frozen while the screen is hidden, so a
    /// returning title screen never bursts through catch-up frames.
    /// </summary>
    public float BackdropTime => _backdropTime;

    public override void _Ready()
    {
        var viewportSize = GetViewportRect().Size;

        // The backdrop animation only costs frames while the screen is on.
        SetProcess(Visible);

        // Dark background
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        BuildBackdrop(viewportSize);

        // Title block: game name + subtitle as one named visual group.
        var titleBlock = new Control();
        titleBlock.Name = "Block_Title";
        titleBlock.Position = new Vector2(0, viewportSize.Y / 2f - 180);
        titleBlock.Size = new Vector2(viewportSize.X, 170);
        titleBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(titleBlock);

        // Decorative frame first so the labels draw on top of it. It never moves or
        // resizes the labels themselves.
        var ornament = new Control();
        ornament.Name = "Ornament";
        ornament.Position = Vector2.Zero;
        ornament.Size = titleBlock.Size;
        ornament.MouseFilter = MouseFilterEnum.Ignore;
        ornament.Draw += () => DrawTitleOrnament(ornament);
        titleBlock.AddChild(ornament);

        var title = new Label();
        title.Name = "Title";
        title.Text = "GEOMETRY\nTOWER DEFENSE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Position = Vector2.Zero;
        title.Size = new Vector2(viewportSize.X, 120);
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        titleBlock.AddChild(title);

        // Subtitle / version
        var subtitle = new Label();
        subtitle.Name = "Subtitle";
        subtitle.Text = "A Geometric Tower Defense Game";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, 140);
        subtitle.Size = new Vector2(viewportSize.X, 30);
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.8f));
        titleBlock.AddChild(subtitle);

        // Level selection buttons, grouped as one named visual group. The block spans
        // the whole six-button stack, so every button (including the Skill Tree) is
        // inside Block_LevelMenu with a uniform gap and the group stays centered.
        float menuTop = viewportSize.Y / 2f + MenuTopOffsetFromCenter;
        float menuHeight = (MenuButtonCount - 1) * MenuButtonPitch + MenuButtonHeight;

        var menuBlock = new Control();
        menuBlock.Name = "Block_LevelMenu";
        menuBlock.Position = new Vector2(viewportSize.X / 2f - MenuButtonWidth / 2f, menuTop);
        menuBlock.Size = new Vector2(MenuButtonWidth, menuHeight);
        menuBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(menuBlock);

        CreateMenuButton(menuBlock, "Level1", "Level 1", 0 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level1.Id), new Color(0.2f, 0.5f, 1f));
        CreateMenuButton(menuBlock, "Level2", "Level 2", 1 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level2.Id), new Color(0.3f, 0.6f, 0.4f));
        CreateMenuButton(menuBlock, "Level3", "Level 3", 2 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level3.Id), new Color(0.8f, 0.3f, 1f));
        CreateMenuButton(menuBlock, "Level4", "Level 4", 3 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level4.Id), new Color(1f, 0.7f, 0.2f));
        CreateMenuButton(menuBlock, "Level5", "Level 5", 4 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level5.Id), new Color(1f, 0.35f, 0.25f));

        // Skill tree navigation button
        CreateMenuButton(menuBlock, "SkillTree", "Skill Tree", 5 * MenuButtonPitch, () => EmitSignal(SignalName.SkillTreePressed), new Color(0.55f, 0.7f, 1f));

        // Instructions text, placed below the menu block so six buttons never overlap it.
        var instructionsBlock = new Control();
        instructionsBlock.Name = "Block_Instructions";
        instructionsBlock.Position = new Vector2(0, menuTop + menuHeight + InstructionsGap);
        instructionsBlock.Size = new Vector2(viewportSize.X, 60);
        instructionsBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(instructionsBlock);

        var instructions = new Label();
        instructions.Name = "Instructions";
        instructions.Text = "Select a level to begin.\nPlace towers to defend your base. Survive 5 waves to win!";
        instructions.HorizontalAlignment = HorizontalAlignment.Center;
        instructions.Position = Vector2.Zero;
        instructions.Size = new Vector2(viewportSize.X, 60);
        instructions.AddThemeFontSizeOverride("font_size", 14);
        instructions.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.7f));
        instructionsBlock.AddChild(instructions);
    }

    /// <summary>
    /// Builds the full-viewport backdrop group. The layer itself is click-through and
    /// draws nothing heavy; the three children are painted once and animated from
    /// <see cref="_Process"/> by transform/modulate only.
    /// </summary>
    private void BuildBackdrop(Vector2 viewportSize)
    {
        var backdrop = new Control();
        backdrop.Name = "Block_Backdrop";
        backdrop.Position = Vector2.Zero;
        backdrop.Size = viewportSize;
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        // Large faint triangle + hexagon rings, rotated slowly around the viewport centre.
        _ringDecor = new Control();
        _ringDecor.Name = "RingDecor";
        _ringDecor.Position = Vector2.Zero;
        _ringDecor.Size = viewportSize;
        _ringDecor.PivotOffset = viewportSize / 2f;
        _ringDecor.MouseFilter = MouseFilterEnum.Ignore;
        _ringDecor.Draw += () => DrawRingDecor(_ringDecor, viewportSize);
        backdrop.AddChild(_ringDecor);

        // Drifting dot/line field.
        _dotField = new Control();
        _dotField.Name = "DotField";
        _dotField.Position = Vector2.Zero;
        _dotField.Size = viewportSize;
        _dotField.MouseFilter = MouseFilterEnum.Ignore;
        _dotField.Draw += () => DrawDotField(_dotField, viewportSize);
        backdrop.AddChild(_dotField);

        // Soft glow behind the title block.
        _titleGlow = new Control();
        _titleGlow.Name = "TitleGlow";
        _titleGlow.Position = Vector2.Zero;
        _titleGlow.Size = viewportSize;
        _titleGlow.MouseFilter = MouseFilterEnum.Ignore;
        _titleGlow.Draw += () => DrawTitleGlow(_titleGlow, viewportSize);
        backdrop.AddChild(_titleGlow);
    }

    private static void DrawRingDecor(Control layer, Vector2 viewportSize)
    {
        if (!IsInstanceValid(layer))
            return;

        var center = new Vector2(viewportSize.X / 2f, viewportSize.Y / 2f);
        float radius = Mathf.Min(viewportSize.X, viewportSize.Y) * GameConstants.TitleBackdropRingRadiusRatio;
        Color color = GameConstants.TitleBackdropLineColor;

        // Nested triangle rings.
        for (int ring = 0; ring < 3; ring++)
        {
            float r = radius * (1f - ring * 0.18f);
            var points = new Vector2[4];
            for (int i = 0; i < 3; i++)
            {
                float angle = -Mathf.Pi / 2f + Mathf.Tau * i / 3f;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
            points[3] = points[0];
            layer.DrawPolyline(points, color with { A = color.A * (1f - ring * 0.2f) }, 2f, antialiased: true);
        }

        // Hexagon ring.
        var hex = new Vector2[7];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Tau * i / 6f;
            hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 1.12f;
        }
        hex[6] = hex[0];
        layer.DrawPolyline(hex, color with { A = color.A * 0.7f }, 1.5f, antialiased: true);
    }

    private static void DrawDotField(Control layer, Vector2 viewportSize)
    {
        if (!IsInstanceValid(layer))
            return;

        float spacing = 72f;
        Color dot = GameConstants.TitleBackdropDotColor;
        for (float y = spacing; y < viewportSize.Y; y += spacing)
        {
            // Offset alternate rows so the field reads as a lattice, not a grid.
            float offset = Mathf.PosMod(y / spacing, 2f) < 1f ? 0f : spacing / 2f;
            for (float x = spacing + offset; x < viewportSize.X; x += spacing)
                layer.DrawCircle(new Vector2(x, y), 1.4f, dot);
        }

        // A few faint diagonal lines to tie the dots into a composition.
        Color line = GameConstants.TitleBackdropLineColor with { A = 0.07f };
        layer.DrawLine(
            new Vector2(0f, viewportSize.Y * 0.25f),
            new Vector2(viewportSize.X, viewportSize.Y * 0.75f),
            line,
            1f);
        layer.DrawLine(
            new Vector2(0f, viewportSize.Y * 0.75f),
            new Vector2(viewportSize.X, viewportSize.Y * 0.25f),
            line,
            1f);
    }

    private static void DrawTitleGlow(Control layer, Vector2 viewportSize)
    {
        if (!IsInstanceValid(layer))
            return;

        var center = new Vector2(viewportSize.X / 2f, viewportSize.Y / 2f - 120f);
        Color glow = GameConstants.TitleBackdropGlowColor;
        for (int i = 4; i >= 1; i--)
        {
            layer.DrawCircle(
                center,
                90f * i,
                glow with { A = glow.A * (0.32f / i) },
                filled: true);
        }
    }

    /// <summary>
    /// The title frame: angular brackets flanking the title and an underline between the
    /// title and subtitle. Drawn once; it never moves or resizes the labels.
    /// </summary>
    private static void DrawTitleOrnament(Control ornament)
    {
        if (!IsInstanceValid(ornament))
            return;

        float centerX = ornament.Size.X / 2f;
        float halfSpan = 240f;
        float top = 6f;
        float bottom = 132f;
        float bracket = 22f;
        Color color = GameConstants.TitleOrnamentColor;

        // Left and right angular brackets.
        OrnamentDrawing.DrawBracket(ornament, new Vector2(centerX - halfSpan, top), new Vector2(-1f, 1f), bracket, color);
        OrnamentDrawing.DrawBracket(ornament, new Vector2(centerX + halfSpan, top), new Vector2(1f, 1f), bracket, color);

        // Underline with small end diamonds.
        ornament.DrawLine(
            new Vector2(centerX - halfSpan + 30f, bottom),
            new Vector2(centerX + halfSpan - 30f, bottom),
            color,
            2f);
        OrnamentDrawing.DrawDiamond(ornament, new Vector2(centerX - halfSpan + 24f, bottom), 4f, color);
        OrnamentDrawing.DrawDiamond(ornament, new Vector2(centerX + halfSpan - 24f, bottom), 4f, color);
    }

    /// <summary>
    /// Creates a centered menu button with the shared layout used by the level
    /// buttons and the skill-tree navigation button. The parent group's origin is the
    /// button column's top-left, so yOffset is relative to that origin.
    /// </summary>
    private static void CreateMenuButton(
        Control parent,
        string name,
        string label,
        float yOffset,
        Action onPressed,
        Color accent)
    {
        var button = new Button();
        button.Name = name;
        button.Text = label;
        button.Position = new Vector2(0, yOffset);
        button.Size = new Vector2(MenuButtonWidth, MenuButtonHeight);
        button.Pressed += onPressed;
        button.AddThemeFontSizeOverride("font_size", 22);
        GeometricButtonStyle.Apply(button, accent);
        GeometricButtonStyle.AddAccentBar(button, accent);
        parent.AddChild(button);
    }

    /// <summary>
    /// Stops processing entirely while hidden (not merely skipping the animation body),
    /// and resumes from the stored time when the screen is shown again.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
            SetProcess(Visible);
    }

    /// <summary>
    /// Slow backdrop animation. Transform/modulate only (no procedural repaint), and
    /// completely frozen while the screen is hidden.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!Visible || !(delta > 0f))
            return;

        _backdropTime += (float)delta;

        if (_ringDecor != null && IsInstanceValid(_ringDecor))
        {
            _ringDecor.Rotation = _backdropTime * GameConstants.TitleBackdropRotationSpeed;
            float pulse = 1f + 0.02f * Mathf.Sin(_backdropTime * GameConstants.TitleBackdropPulseSpeed);
            _ringDecor.Scale = new Vector2(pulse, pulse);
        }

        if (_dotField != null && IsInstanceValid(_dotField))
        {
            float drift = GameConstants.TitleBackdropDriftAmplitude
                * Mathf.Sin(_backdropTime * GameConstants.TitleBackdropDriftSpeed);
            _dotField.Position = new Vector2(drift, drift * 0.4f);
        }

        if (_titleGlow != null && IsInstanceValid(_titleGlow))
        {
            float alpha = 0.75f + 0.25f * Mathf.Sin(
                _backdropTime * GameConstants.TitleBackdropPulseSpeed);
            _titleGlow.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }
}
