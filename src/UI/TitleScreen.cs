using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Title screen with game name and level selection buttons.
/// </summary>
public partial class TitleScreen : Control
{
    [Signal]
    public delegate void LevelSelectedEventHandler(int levelId);

    public override void _Ready()
    {
        var viewportSize = GetViewportRect().Size;

        // Dark background
        var bg = new ColorRect();
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Title text
        var title = new Label();
        title.Text = "GEOMETRY\nTOWER DEFENSE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Position = new Vector2(0, viewportSize.Y / 2f - 180);
        title.Size = new Vector2(viewportSize.X, 120);
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        AddChild(title);

        // Subtitle / version
        var subtitle = new Label();
        subtitle.Text = "A Geometric Tower Defense Game";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Position = new Vector2(0, viewportSize.Y / 2f - 40);
        subtitle.Size = new Vector2(viewportSize.X, 30);
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.8f));
        AddChild(subtitle);

        // Level selection buttons
        CreateLevelButton(Levels.Level1.Id, "Level 1", 40);
        CreateLevelButton(Levels.Level2.Id, "Level 2", 110);
        CreateLevelButton(Levels.Level3.Id, "Level 3", 180);
        CreateLevelButton(Levels.Level4.Id, "Level 4", 250);

        // Instructions text
        var instructions = new Label();
        instructions.Text = "Select a level to begin.\nPlace towers to defend your base. Survive 5 waves to win!";
        instructions.HorizontalAlignment = HorizontalAlignment.Center;
        instructions.Position = new Vector2(0, viewportSize.Y / 2f + 330);
        instructions.Size = new Vector2(viewportSize.X, 60);
        instructions.AddThemeFontSizeOverride("font_size", 14);
        instructions.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.7f));
        AddChild(instructions);
    }

    /// <summary>
    /// Creates a level-select button with the shared layout used by all levels.
    /// </summary>
    private void CreateLevelButton(int levelId, string label, float yOffset)
    {
        var viewportSize = GetViewportRect().Size;

        var button = new Button();
        button.Text = label;
        button.Position = new Vector2(viewportSize.X / 2f - 120, viewportSize.Y / 2f + yOffset);
        button.Size = new Vector2(240, 50);
        button.Pressed += () => EmitSignal(SignalName.LevelSelected, levelId);
        button.AddThemeFontSizeOverride("font_size", 22);
        AddChild(button);
    }
}
