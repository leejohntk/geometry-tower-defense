using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Title screen with game name and Start button.
/// </summary>
public partial class TitleScreen : Control
{
    [Signal]
    public delegate void StartGameEventHandler();

    private Button? _startButton;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Size = GetViewportRect().Size;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Title text
        var title = new Label();
        title.Text = "GEOMETRY\nTOWER DEFENSE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Position = new Vector2(0, GetViewportRect().Size.Y / 2f - 160);
        title.Size = new Vector2(GetViewportRect().Size.X, 120);
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        AddChild(title);

        // Subtitle / version
        var subtitle = new Label();
        subtitle.Text = "A Geometric Tower Defense Game";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, GetViewportRect().Size.Y / 2f - 20);
        subtitle.Size = new Vector2(GetViewportRect().Size.X, 30);
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.8f));
        AddChild(subtitle);

        // Start button
        _startButton = new Button();
        _startButton.Text = "START";
        _startButton.Position = new Vector2(
            GetViewportRect().Size.X / 2f - 100,
            GetViewportRect().Size.Y / 2f + 60
        );
        _startButton.Size = new Vector2(200, 50);
        _startButton.Pressed += OnStartPressed;

        // Style the button using theme overrides
        _startButton.AddThemeFontSizeOverride("font_size", 24);
        AddChild(_startButton);

        // Instructions text
        var instructions = new Label();
        instructions.Text = "Place towers to defend your base.\nSurvive 5 waves to win!";
        instructions.HorizontalAlignment = HorizontalAlignment.Center;
        instructions.Position = new Vector2(0, GetViewportRect().Size.Y / 2f + 130);
        instructions.Size = new Vector2(GetViewportRect().Size.X, 50);
        instructions.AddThemeFontSizeOverride("font_size", 14);
        instructions.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.7f));
        AddChild(instructions);
    }

    private void OnStartPressed()
    {
        EmitSignal(SignalName.StartGame);
    }
}
