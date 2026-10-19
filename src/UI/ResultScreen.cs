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
        var viewportSize = GetViewportRect().Size;

        var overlay = new ColorRect();
        overlay.Name = "Overlay";
        overlay.Size = viewportSize;
        overlay.Color = _overlayColor;
        overlay.MouseFilter = MouseFilterEnum.Pass;
        AddChild(overlay);

        // Title block: result title + subtitle as one named visual group.
        var titleBlock = new Control();
        titleBlock.Name = "Block_Title";
        titleBlock.Position = new Vector2(0, viewportSize.Y / 2f - 80);
        titleBlock.Size = new Vector2(viewportSize.X, 120);
        titleBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(titleBlock);

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
        AddChild(returnButton);
    }
}
