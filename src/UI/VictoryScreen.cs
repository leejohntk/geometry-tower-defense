using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Victory overlay with gold tint and return-to-title button.
/// </summary>
public partial class VictoryScreen : Control
{
    [Signal]
    public delegate void ReturnToTitleEventHandler();

    public override void _Ready()
    {
        // Gold overlay
        var overlay = new ColorRect();
        overlay.Size = GetViewportRect().Size;
        overlay.Color = new Color(0.9f, 0.7f, 0.1f, 0.5f);
        overlay.MouseFilter = MouseFilterEnum.Pass; // Block clicks through
        AddChild(overlay);

        // VICTORY text
        var victoryLabel = new Label();
        victoryLabel.Text = "VICTORY";
        victoryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        victoryLabel.VerticalAlignment = VerticalAlignment.Center;
        victoryLabel.Position = new Vector2(0, GetViewportRect().Size.Y / 2f - 80);
        victoryLabel.Size = new Vector2(GetViewportRect().Size.X, 100);
        victoryLabel.AddThemeFontSizeOverride("font_size", 64);
        victoryLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(victoryLabel);

        // Subtitle
        var subtitle = new Label();
        subtitle.Text = "You defended your base! All waves cleared!";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, GetViewportRect().Size.Y / 2f);
        subtitle.Size = new Vector2(GetViewportRect().Size.X, 40);
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
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
