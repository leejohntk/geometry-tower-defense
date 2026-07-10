using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Game over overlay with red tint and return-to-title button.
/// </summary>
public partial class GameOverScreen : Control
{
    [Signal]
    public delegate void ReturnToTitleEventHandler();

    public override void _Ready()
    {
        // Red overlay
        var overlay = new ColorRect();
        overlay.Size = GetViewportRect().Size;
        overlay.Color = new Color(0.8f, 0.05f, 0.05f, 0.6f);
        overlay.MouseFilter = MouseFilterEnum.Pass; // Block clicks through
        AddChild(overlay);

        // GAME OVER text
        var gameOverLabel = new Label();
        gameOverLabel.Text = "GAME OVER";
        gameOverLabel.HorizontalAlignment = HorizontalAlignment.Center;
        gameOverLabel.VerticalAlignment = VerticalAlignment.Center;
        gameOverLabel.Position = new Vector2(0, GetViewportRect().Size.Y / 2f - 80);
        gameOverLabel.Size = new Vector2(GetViewportRect().Size.X, 100);
        gameOverLabel.AddThemeFontSizeOverride("font_size", 64);
        gameOverLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(gameOverLabel);

        // Subtitle
        var subtitle = new Label();
        subtitle.Text = "The enemies reached your base...";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, GetViewportRect().Size.Y / 2f);
        subtitle.Size = new Vector2(GetViewportRect().Size.X, 40);
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        AddChild(subtitle);

        // Return to Title button
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
