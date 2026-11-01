using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Title screen presentation: the full-viewport click-through backdrop, the title
/// decoration, and the geometric menu button styling. Existing node paths, texts, rects,
/// and signals are preserved, and the backdrop animation freezes while hidden.
/// </summary>
[TestSuite]
public class TitleScreenBackdropTest
{
    private static TitleScreen BuildTitle()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new TitleScreen());
    }

    [TestCase]
    public void Backdrop_SpansTheViewportAndIsClickThrough()
    {
        var screen = BuildTitle();
        try
        {
            var backdrop = screen.GetNodeOrNull<Control>("Block_Backdrop");
            AssertThat(backdrop).IsNotNull();
            AssertThat(backdrop!.Position).IsEqual(Vector2.Zero);
            var viewport = NodeTestSupport.ProjectViewportSize();
            AssertThat(backdrop.Size).IsEqual(new Vector2(viewport.X, viewport.Y));
            AssertThat(backdrop.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            foreach (var child in new[] { "RingDecor", "DotField", "TitleGlow" })
            {
                var layer = backdrop.GetNodeOrNull<Control>(child);
                AssertThat(layer).IsNotNull();
                AssertThat(layer!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void TitleBlock_KeepsItsLabels_AndGainsAClickThroughOrnament()
    {
        var screen = BuildTitle();
        try
        {
            var titleBlock = screen.GetNodeOrNull<Control>("Block_Title");
            AssertThat(titleBlock).IsNotNull();
            AssertThat(titleBlock!.GetNode<Label>("Title").Text).IsEqual("GEOMETRY\nTOWER DEFENSE");
            AssertThat(titleBlock.GetNode<Label>("Subtitle").Text).IsEqual("A Geometric Tower Defense Game");

            var ornament = titleBlock.GetNodeOrNull<Control>("Ornament");
            AssertThat(ornament).IsNotNull();
            AssertThat(ornament!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(ornament.Size).IsEqual(titleBlock.Size);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void MenuButtons_KeepTheirPathsSizesAndSignals()
    {
        var screen = BuildTitle();
        try
        {
            var expected = new[]
            {
                ("Level1", "Level 1"), ("Level2", "Level 2"), ("Level3", "Level 3"),
                ("Level4", "Level 4"), ("Level5", "Level 5"), ("SkillTree", "Skill Tree"),
            };

            foreach (var (name, text) in expected)
            {
                var button = screen.GetNodeOrNull<Button>($"Block_LevelMenu/{name}");
                AssertThat(button).IsNotNull();
                AssertThat(button!.Text).IsEqual(text);
                AssertThat(button.Size).IsEqual(new Vector2(240f, 50f));
                AssertThat(button.HasThemeStyleboxOverride("hover")).IsTrue();

                var accent = button.GetNodeOrNull<Control>("Accent");
                AssertThat(accent).IsNotNull();
                AssertThat(accent!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            }

            int levelId = -1;
            screen.LevelSelected += id => levelId = id;
            screen.GetNode<Button>("Block_LevelMenu/Level1")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(levelId).IsEqual(Levels.Level1.Id);

            bool skillTreePressed = false;
            screen.SkillTreePressed += () => skillTreePressed = true;
            screen.GetNode<Button>("Block_LevelMenu/SkillTree")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(skillTreePressed).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void BackdropAnimation_RunsWhileVisible_AndFreezesWhileHidden()
    {
        var screen = BuildTitle();
        try
        {
            screen._Process(0.25);
            float afterVisibleFrame = screen.BackdropTime;
            AssertThat(afterVisibleFrame).IsGreater(0f);

            screen.Visible = false;
            AssertThat(screen.IsProcessing()).IsFalse();
            screen._Process(1.0);
            AssertThat(screen.BackdropTime).IsEqual(afterVisibleFrame);

            // Returning to the title resumes from a consistent state (no catch-up).
            screen.Visible = true;
            AssertThat(screen.IsProcessing()).IsTrue();
            screen._Process(0.1);
            AssertThat(screen.BackdropTime).IsEqualApprox(afterVisibleFrame + 0.1f, 0.0001f);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void InstructionsBlock_IsUnchanged()
    {
        var screen = BuildTitle();
        try
        {
            var instructions = screen.GetNode<Control>("Block_Instructions");
            AssertThat(instructions.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            var label = instructions.GetNode<Label>("Instructions");
            AssertThat(label.Text).IsEqual(
                "Select a level to begin.\nPlace towers to defend your base. Survive 5 waves to win!");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }
}
