using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Result overlay shown at end of game (game over or victory).
/// Parameterized with overlay color, title text, and subtitle text.
/// </summary>
public partial class ResultScreen : Control
{
    [Signal]
    public delegate void ReturnToTitleEventHandler();

    private readonly Color _overlayColor;
    private readonly string _titleText;
    private readonly string _subtitleText;

    public ResultScreen(Color overlayColor, string titleText, string subtitleText)
    {
        _overlayColor = overlayColor;
        _titleText = titleText;
        _subtitleText = subtitleText;
    }

    public override void _Ready()
    {
        var overlay = new ColorRect();
        overlay.Size = GetViewportRect().Size;
        overlay.Color = _overlayColor;
        overlay.MouseFilter = MouseFilterEnum.Pass;
        AddChild(overlay);

        var titleLabel = new Label();
        titleLabel.Text = _titleText;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        titleLabel.Position = new Vector2(0, GetViewportRect().Size.Y / 2f - 80);
        titleLabel.Size = new Vector2(GetViewportRect().Size.X, 100);
        titleLabel.AddThemeFontSizeOverride("font_size", 64);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(titleLabel);

        var subtitle = new Label();
        subtitle.Text = _subtitleText;
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, GetViewportRect().Size.Y / 2f);
        subtitle.Size = new Vector2(GetViewportRect().Size.X, 40);
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        AddChild(subtitle);

        var returnButton = new Button();
        returnButton.Text = "Return to Title";
        returnButton.Position = new Vector2(
            GetViewportRect().Size.X / 2f - 100,
            GetViewportRect().Size.Y / 2f + 60
        );
        returnButton.Size = new Vector2(200, 50);
        returnButton.Pressed += () => EmitSignal(SignalName.ReturnToTitle);
        returnButton.AddThemeFontSizeOverride("font_size", 18);
        AddChild(returnButton);
    }
}
