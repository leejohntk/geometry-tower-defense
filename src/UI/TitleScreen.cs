using Godot;
using System;

namespace GeometryTowerDefense;

/// <summary>
/// Title screen with game name and level selection buttons.
/// </summary>
public partial class TitleScreen : Control
{
    [Signal]
    public delegate void LevelSelectedEventHandler(int levelId);

    [Signal]
    public delegate void SkillTreePressedEventHandler();

    public override void _Ready()
    {
        var viewportSize = GetViewportRect().Size;

        // Dark background
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Title block: game name + subtitle as one named visual group.
        var titleBlock = new Control();
        titleBlock.Name = "Block_Title";
        titleBlock.Position = new Vector2(0, viewportSize.Y / 2f - 180);
        titleBlock.Size = new Vector2(viewportSize.X, 170);
        titleBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(titleBlock);

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

        // Level selection buttons, grouped as one named visual group.
        var menuBlock = new Control();
        menuBlock.Name = "Block_LevelMenu";
        menuBlock.Position = new Vector2(viewportSize.X / 2f - 120, viewportSize.Y / 2f + 40);
        menuBlock.Size = new Vector2(240, 330);
        menuBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(menuBlock);

        CreateMenuButton(menuBlock, "Level1", "Level 1", 0, () => EmitSignal(SignalName.LevelSelected, Levels.Level1.Id));
        CreateMenuButton(menuBlock, "Level2", "Level 2", 70, () => EmitSignal(SignalName.LevelSelected, Levels.Level2.Id));
        CreateMenuButton(menuBlock, "Level3", "Level 3", 140, () => EmitSignal(SignalName.LevelSelected, Levels.Level3.Id));
        CreateMenuButton(menuBlock, "Level4", "Level 4", 210, () => EmitSignal(SignalName.LevelSelected, Levels.Level4.Id));

        // Skill tree navigation button
        CreateMenuButton(menuBlock, "SkillTree", "Skill Tree", 280, () => EmitSignal(SignalName.SkillTreePressed));

        // Instructions text
        var instructionsBlock = new Control();
        instructionsBlock.Name = "Block_Instructions";
        instructionsBlock.Position = new Vector2(0, viewportSize.Y / 2f + 400);
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
    /// Creates a centered menu button with the shared layout used by the level
    /// buttons and the skill-tree navigation button. The parent group's origin is the
    /// button column's top-left, so yOffset is relative to that origin.
    /// </summary>
    private static void CreateMenuButton(Control parent, string name, string label, float yOffset, Action onPressed)
    {
        var button = new Button();
        button.Name = name;
        button.Text = label;
        button.Position = new Vector2(0, yOffset);
        button.Size = new Vector2(240, 50);
        button.Pressed += onPressed;
        button.AddThemeFontSizeOverride("font_size", 22);
        parent.AddChild(button);
    }
}
