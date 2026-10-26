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

        CreateMenuButton(menuBlock, "Level1", "Level 1", 0 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level1.Id));
        CreateMenuButton(menuBlock, "Level2", "Level 2", 1 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level2.Id));
        CreateMenuButton(menuBlock, "Level3", "Level 3", 2 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level3.Id));
        CreateMenuButton(menuBlock, "Level4", "Level 4", 3 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level4.Id));
        CreateMenuButton(menuBlock, "Level5", "Level 5", 4 * MenuButtonPitch, () => EmitSignal(SignalName.LevelSelected, Levels.Level5.Id));

        // Skill tree navigation button
        CreateMenuButton(menuBlock, "SkillTree", "Skill Tree", 5 * MenuButtonPitch, () => EmitSignal(SignalName.SkillTreePressed));

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
        button.Size = new Vector2(MenuButtonWidth, MenuButtonHeight);
        button.Pressed += onPressed;
        button.AddThemeFontSizeOverride("font_size", 22);
        parent.AddChild(button);
    }
}
