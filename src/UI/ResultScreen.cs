using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Which outcome a result overlay presents. Drives the backdrop shape language and the
/// title accent so victory (gold rings + rising starburst) and defeat (red shards +
/// fractured rings + collapsing diamond) read as different events, not one recolored
/// overlay.
/// </summary>
public enum ResultScreenMode
{
    Defeat,
    Victory
}

/// <summary>
/// Result overlay shown at end of game (game over or victory).
/// Parameterized with overlay color, title text, subtitle text, and (optionally) the
/// outcome mode. The mode flag is optional so existing three-argument call sites keep
/// compiling and default to the defeat treatment.
/// </summary>
public partial class ResultScreen : Control
{
    [Signal]
    public delegate void ReturnToTitleEventHandler();

    private readonly Color _overlayColor;
    private readonly string _titleText;
    private readonly string _subtitleText;

    private Control? _backdrop;
    private float _backdropTime = 0f;
    private bool _backdropSettled = false;

    /// <summary>Outcome this overlay presents.</summary>
    public ResultScreenMode Mode { get; }

    /// <summary>
    /// Backdrop intro progress (0..1). Advances only while visible, and clamps at 1 so
    /// the animation settles instead of looping forever.
    /// </summary>
    public float BackdropProgress => GameConstants.ResultBackdropDuration > 0f
        ? Mathf.Clamp(_backdropTime / GameConstants.ResultBackdropDuration, 0f, 1f)
        : 1f;

    /// <summary>True once the intro animation has settled.</summary>
    public bool IsBackdropSettled => _backdropSettled;

    /// <summary>Accent color for this outcome's title frame and return button.</summary>
    private Color AccentColor => Mode == ResultScreenMode.Victory
        ? GameConstants.VictoryBackdropGold
        : GameConstants.DefeatBackdropRed;

    public ResultScreen(
        Color overlayColor,
        string titleText,
        string subtitleText,
        ResultScreenMode mode = ResultScreenMode.Defeat)
    {
        _overlayColor = overlayColor;
        _titleText = titleText;
        _subtitleText = subtitleText;
        Mode = mode;
    }

    public override void _Ready()
    {
        var viewportSize = GetViewportRect().Size;

        // The intro animation only costs frames while the overlay is on.
        SetProcess(Visible);

        var overlay = new ColorRect();
        overlay.Name = "Overlay";
        overlay.Size = viewportSize;
        overlay.Color = _overlayColor;
        overlay.MouseFilter = MouseFilterEnum.Pass;
        AddChild(overlay);

        // Backdrop composition, above the translucent overlay tint but behind the title
        // and return button. It is click-through, so a rapid Return click is never
        // delayed or blocked.
        _backdrop = new Control();
        _backdrop.Name = "Block_Backdrop";
        _backdrop.Position = Vector2.Zero;
        _backdrop.Size = viewportSize;
        _backdrop.MouseFilter = MouseFilterEnum.Ignore;
        _backdrop.Draw += () => DrawBackdrop(_backdrop, viewportSize);
        AddChild(_backdrop);

        // Subtle edge darkening so the backdrop and text read clearly over the
        // battlefield; never opaque, and click-through.
        var vignette = new Control();
        vignette.Name = "Vignette";
        vignette.Position = Vector2.Zero;
        vignette.Size = viewportSize;
        vignette.MouseFilter = MouseFilterEnum.Ignore;
        vignette.Draw += () => DrawVignette(vignette, viewportSize);
        AddChild(vignette);

        // Title block: result title + subtitle as one named visual group.
        var titleBlock = new Control();
        titleBlock.Name = "Block_Title";
        titleBlock.Position = new Vector2(0, viewportSize.Y / 2f - 80);
        titleBlock.Size = new Vector2(viewportSize.X, 120);
        titleBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(titleBlock);

        // Title treatment: underline + accent frame in the result color, drawn behind
        // the labels without moving or resizing them.
        var titleOrnament = new Control();
        titleOrnament.Name = "Ornament";
        titleOrnament.Position = Vector2.Zero;
        titleOrnament.Size = titleBlock.Size;
        titleOrnament.MouseFilter = MouseFilterEnum.Ignore;
        titleOrnament.Draw += () => DrawTitleOrnament(titleOrnament);
        titleBlock.AddChild(titleOrnament);

        var titleLabel = new Label();
        titleLabel.Name = "Title";
        titleLabel.Text = _titleText;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        titleLabel.Position = Vector2.Zero;
        titleLabel.Size = new Vector2(viewportSize.X, 100);
        titleLabel.AddThemeFontSizeOverride("font_size", 64);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        titleBlock.AddChild(titleLabel);

        var subtitle = new Label();
        subtitle.Name = "Subtitle";
        subtitle.Text = _subtitleText;
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, 80);
        subtitle.Size = new Vector2(viewportSize.X, 40);
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        titleBlock.AddChild(subtitle);

        var returnButton = new Button();
        returnButton.Name = "ReturnToTitle";
        returnButton.Text = "Return to Title";
        returnButton.Position = new Vector2(
            viewportSize.X / 2f - 100,
            viewportSize.Y / 2f + 60
        );
        returnButton.Size = new Vector2(200, 50);
        returnButton.Pressed += () => EmitSignal(SignalName.ReturnToTitle);
        returnButton.AddThemeFontSizeOverride("font_size", 18);
        GeometricButtonStyle.Apply(returnButton, AccentColor);
        GeometricButtonStyle.AddAccentBar(returnButton, AccentColor);
        AddChild(returnButton);
    }

    /// <summary>
    /// Restarts the intro animation for this screen. Called when the screen becomes
    /// visible so a second defeat (or the other outcome) always replays from a clean
    /// state rather than freezing at the previous result's settled frame.
    /// </summary>
    public void RestartBackdrop()
    {
        _backdropTime = 0f;
        _backdropSettled = false;
        SetProcess(Visible);
        if (_backdrop != null && IsInstanceValid(_backdrop))
            _backdrop.QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationVisibilityChanged)
            return;

        SetProcess(Visible);
        if (Visible)
            RestartBackdrop();
    }

    /// <summary>
    /// Advances the short intro while visible. Redraws only during the intro, then
    /// settles and stops costing frames.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!Visible || _backdropSettled || !(delta > 0f))
            return;

        _backdropTime += (float)delta;
        if (_backdropTime >= GameConstants.ResultBackdropDuration)
        {
            _backdropTime = GameConstants.ResultBackdropDuration;
            _backdropSettled = true;
            // The intro is a one-shot: stop processing (and redrawing) entirely.
            SetProcess(false);
        }

        if (_backdrop != null && IsInstanceValid(_backdrop))
            _backdrop.QueueRedraw();
    }

    /// <summary>
    /// Draws the outcome-specific composition. Struct math only; no arrays are
    /// allocated while the timed intro redraws.
    /// </summary>
    private void DrawBackdrop(Control layer, Vector2 viewportSize)
    {
        if (!IsInstanceValid(layer))
            return;

        float progress = BackdropProgress;
        var center = new Vector2(viewportSize.X / 2f, viewportSize.Y / 2f - 40f);
        float extent = Mathf.Min(viewportSize.X, viewportSize.Y);

        if (Mode == ResultScreenMode.Victory)
            DrawVictoryBackdrop(layer, center, extent, progress);
        else
            DrawDefeatBackdrop(layer, center, extent, progress);
    }

    /// <summary>Victory: expanding gold/white rings plus a rising starburst.</summary>
    private static void DrawVictoryBackdrop(Control layer, Vector2 center, float extent, float progress)
    {
        for (int i = 0; i < GameConstants.ResultBackdropRings; i++)
        {
            float phase = Mathf.Clamp(progress * 1.3f - i * 0.18f, 0f, 1f);
            float radius = extent * (0.16f + 0.42f * phase);
            Color ring = i % 2 == 0
                ? GameConstants.VictoryBackdropGold
                : GameConstants.VictoryBackdropWhite;
            float alpha = (1f - 0.55f * progress) * (0.25f + 0.75f * (1f - phase));
            layer.DrawCircle(center, radius, ring with { A = ring.A * alpha }, false, 2.5f);
        }

        // Rising starburst: 14 rays symmetric around the centre, growing with progress.
        int rays = GameConstants.ResultBackdropShardCount;
        float rayInner = extent * (0.08f + 0.05f * progress);
        float rayOuter = extent * (0.2f + 0.26f * progress);
        float twist = progress * 0.35f;
        float rayAlpha = 0.35f + 0.5f * (1f - progress);
        for (int i = 0; i < rays; i++)
        {
            float angle = Mathf.Tau * i / rays + twist;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Color color = (i % 2 == 0
                ? GameConstants.VictoryBackdropGold
                : GameConstants.VictoryBackdropWhite) with { A = rayAlpha };
            layer.DrawLine(
                center + direction * rayInner,
                center + direction * rayOuter,
                color,
                2f);
        }

        // Upward shards: short vertical gold strokes rising above the burst.
        for (int i = 0; i < 5; i++)
        {
            float x = center.X + (i - 2) * extent * 0.06f;
            float rise = extent * (0.1f + 0.2f * progress) + i * 6f;
            layer.DrawLine(
                new Vector2(x, center.Y - extent * 0.12f),
                new Vector2(x, center.Y - extent * 0.12f - rise),
                GameConstants.VictoryBackdropGold with { A = rayAlpha * 0.8f },
                3f);
        }
    }

    /// <summary>Defeat: red/orange shards, fractured rings, and a collapsing diamond.</summary>
    private static void DrawDefeatBackdrop(Control layer, Vector2 center, float extent, float progress)
    {
        // Shattered rings: arcs with gaps, shrinking as the composition collapses.
        for (int i = 0; i < GameConstants.ResultBackdropRings; i++)
        {
            float radius = extent * (0.34f - 0.08f * progress) * (1f - i * 0.22f);
            float start = i * 1.1f + progress * 0.4f;
            Color ring = i % 2 == 0
                ? GameConstants.DefeatBackdropRed
                : GameConstants.DefeatBackdropOrange;
            float alpha = (1f - 0.6f * progress) * 0.8f;
            layer.DrawArc(
                center,
                radius,
                start,
                start + 2.1f,
                20,
                ring with { A = ring.A * alpha },
                2f,
                antialiased: true);
        }

        // Radiating shards that drift outward and rotate as the screen fractures.
        int shards = GameConstants.ResultBackdropShardCount;
        for (int i = 0; i < shards; i++)
        {
            float angle = Mathf.Pi * 0.15f + Mathf.Tau * i / shards + progress * 0.5f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float inner = extent * (0.1f + 0.12f * progress);
            float outer = extent * (0.2f + 0.25f * progress) + (i % 3) * 8f;
            Color color = (i % 3 == 0
                ? GameConstants.DefeatBackdropOrange
                : GameConstants.DefeatBackdropRed) with { A = 0.7f * (1f - 0.4f * progress) };
            layer.DrawLine(center + direction * inner, center + direction * outer, color, 2.5f);
        }

        // Collapsing diamond: shrinks and rotates as it falls apart.
        float diamondRadius = extent * (0.18f - 0.1f * progress);
        float rotation = progress * 0.9f;
        var previous = Vector2.Zero;
        for (int i = 0; i <= 4; i++)
        {
            float angle = rotation + Mathf.Tau * (i % 4) / 4f - Mathf.Pi / 2f;
            var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * diamondRadius;
            if (i > 0)
            {
                layer.DrawLine(
                    previous,
                    point,
                    GameConstants.DefeatBackdropRed with { A = 0.65f * (1f - 0.35f * progress) },
                    2f);
            }
            previous = point;
        }
    }

    private static void DrawVignette(Control layer, Vector2 viewportSize)
    {
        if (!IsInstanceValid(layer))
            return;

        Color shade = GameConstants.ResultVignetteColor;
        const float band = 120f;
        // Four edge bands: nested translucent rectangles darken the borders only.
        for (int i = 0; i < 4; i++)
        {
            float inset = i * band * 0.25f;
            float alpha = shade.A * 0.22f * (1f - i * 0.2f);
            Color color = shade with { A = alpha };
            layer.DrawRect(new Rect2(inset, inset, viewportSize.X - inset * 2f, band * 0.25f), color);
            layer.DrawRect(
                new Rect2(inset, viewportSize.Y - inset - band * 0.25f, viewportSize.X - inset * 2f, band * 0.25f),
                color);
            layer.DrawRect(new Rect2(inset, inset, band * 0.25f, viewportSize.Y - inset * 2f), color);
            layer.DrawRect(
                new Rect2(viewportSize.X - inset - band * 0.25f, inset, band * 0.25f, viewportSize.Y - inset * 2f),
                color);
        }
    }

    /// <summary>
    /// Result title treatment: an accent underline with end diamonds, drawn behind the
    /// labels. It does not move or resize the title/subtitle.
    /// </summary>
    private void DrawTitleOrnament(Control ornament)
    {
        if (!IsInstanceValid(ornament))
            return;

        float centerX = ornament.Size.X / 2f;
        float halfSpan = 260f;
        float underlineY = ornament.Size.Y - 6f;
        Color color = AccentColor;

        ornament.DrawLine(
            new Vector2(centerX - halfSpan, underlineY),
            new Vector2(centerX + halfSpan, underlineY),
            color with { A = 0.8f },
            3f);

        OrnamentDrawing.DrawDiamond(ornament, new Vector2(centerX - halfSpan - 10f, underlineY), 5f, color);
        OrnamentDrawing.DrawDiamond(ornament, new Vector2(centerX + halfSpan + 10f, underlineY), 5f, color);

        // Angular corner brackets around the title block.
        OrnamentDrawing.DrawBracket(ornament, new Vector2(centerX - halfSpan, 18f), new Vector2(-1f, 1f), 18f, color);
        OrnamentDrawing.DrawBracket(ornament, new Vector2(centerX + halfSpan, 18f), new Vector2(1f, 1f), 18f, color);
    }
}
