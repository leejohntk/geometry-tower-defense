using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Result overlays: source-compatible constructors (with an optional outcome mode),
/// distinct victory/defeat backdrops, preserved node paths/texts/signals, a styled
/// return button, and an intro animation that runs only while visible and then settles.
/// </summary>
[TestSuite]
public class ResultScreenBackdropTest
{
    private static ResultScreen BuildDefeat()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var screen = new ResultScreen(
            new Color(0.8f, 0.05f, 0.05f, 0.6f),
            "GAME OVER",
            "The enemies reached your base...");
        screen.Name = "GameOverScreen";
        return NodeTestSupport.Attach(screen);
    }

    private static ResultScreen BuildVictory()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var screen = new ResultScreen(
            new Color(0.9f, 0.7f, 0.1f, 0.5f),
            "VICTORY",
            "You defended your base! All waves cleared!",
            ResultScreenMode.Victory);
        screen.Name = "VictoryScreen";
        return NodeTestSupport.Attach(screen);
    }

    [TestCase]
    public void Constructor_DefaultsToDefeat_AndAcceptsAnOutcomeMode()
    {
        var defeat = new ResultScreen(Colors.Red, "GAME OVER", "sub");
        var victory = new ResultScreen(Colors.Gold, "VICTORY", "sub", ResultScreenMode.Victory);
        try
        {
            AssertThat(defeat.Mode).IsEqual(ResultScreenMode.Defeat);
            AssertThat(victory.Mode).IsEqual(ResultScreenMode.Victory);
        }
        finally
        {
            defeat.Free();
            victory.Free();
        }
    }

    [TestCase]
    public void DefeatScreen_KeepsOverlayAndText_AndAddsABackdrop()
    {
        var screen = BuildDefeat();
        try
        {
            var overlay = screen.GetNodeOrNull<ColorRect>("Overlay");
            AssertThat(overlay).IsNotNull();
            AssertThat(overlay!.Color).IsEqual(new Color(0.8f, 0.05f, 0.05f, 0.6f));

            var backdrop = screen.GetNodeOrNull<Control>("Block_Backdrop");
            AssertThat(backdrop).IsNotNull();
            AssertThat(backdrop!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            var viewport = NodeTestSupport.ProjectViewportSize();
            AssertThat(backdrop.Size).IsEqual(new Vector2(viewport.X, viewport.Y));

            AssertThat(screen.GetNode<Label>("Block_Title/Title").Text).IsEqual("GAME OVER");
            AssertThat(screen.GetNode<Label>("Block_Title/Subtitle").Text)
                .IsEqual("The enemies reached your base...");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void VictoryScreen_KeepsOverlayAndText_AndUsesTheVictoryMode()
    {
        var screen = BuildVictory();
        try
        {
            var overlay = screen.GetNodeOrNull<ColorRect>("Overlay");
            AssertThat(overlay).IsNotNull();
            AssertThat(overlay!.Color).IsEqual(new Color(0.9f, 0.7f, 0.1f, 0.5f));

            AssertThat(screen.Mode).IsEqual(ResultScreenMode.Victory);
            AssertThat(screen.GetNode<Label>("Block_Title/Title").Text).IsEqual("VICTORY");
            AssertThat(screen.GetNode<Label>("Block_Title/Subtitle").Text)
                .IsEqual("You defended your base! All waves cleared!");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void ReturnButton_KeepsItsRect_IsStyled_AndEmits()
    {
        var screen = BuildDefeat();
        try
        {
            var button = screen.GetNodeOrNull<Button>("ReturnToTitle");
            AssertThat(button).IsNotNull();
            AssertThat(button!.Size).IsEqual(new Vector2(200f, 50f));
            AssertThat(button.Position).IsEqual(new Vector2(
                NodeTestSupport.ProjectViewportSize().X / 2f - 100f,
                NodeTestSupport.ProjectViewportSize().Y / 2f + 60f));
            AssertThat(button.HasThemeStyleboxOverride("hover")).IsTrue();
            AssertThat(button.GetNodeOrNull<Control>("Accent")).IsNotNull();
            AssertThat(button.GetNode<Control>("Accent").MouseFilter)
                .IsEqual(Control.MouseFilterEnum.Ignore);

            int returns = 0;
            screen.ReturnToTitle += () => returns++;
            // A rapid click on the first frame works: nothing in the backdrop delays it.
            button.EmitSignal(Button.SignalName.Pressed);
            AssertThat(returns).IsEqual(1);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void Backdrop_AnimatesWhileVisible_ThenSettles()
    {
        var screen = BuildVictory();
        try
        {
            AssertThat(screen.BackdropProgress).IsEqual(0f);

            screen._Process(GameConstants.ResultBackdropDuration / 2f);
            AssertThat(screen.BackdropProgress).IsGreater(0f);
            AssertThat(screen.BackdropProgress).IsLess(1f);
            AssertThat(screen.IsBackdropSettled).IsFalse();

            screen._Process(GameConstants.ResultBackdropDuration);
            AssertThat(screen.BackdropProgress).IsEqual(1f);
            AssertThat(screen.IsBackdropSettled).IsTrue();
            AssertThat(screen.IsProcessing()).IsFalse();

            // Settled: further frames do not advance or repaint.
            float settled = screen.BackdropProgress;
            screen._Process(5f);
            AssertThat(screen.BackdropProgress).IsEqual(settled);
            AssertThat(screen.IsProcessing()).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void Backdrop_StopsWhileHidden_AndRestartsWhenShownAgain()
    {
        var screen = BuildDefeat();
        try
        {
            screen._Process(GameConstants.ResultBackdropDuration);
            AssertThat(screen.IsBackdropSettled).IsTrue();
            AssertThat(screen.IsProcessing()).IsFalse();

            screen.Visible = false;
            AssertThat(screen.IsProcessing()).IsFalse();
            screen._Process(1f);
            AssertThat(screen.BackdropProgress).IsEqual(1f);

            screen.Visible = true;
            AssertThat(screen.IsProcessing()).IsTrue();
            AssertThat(screen.BackdropProgress).IsEqual(0f);
            AssertThat(screen.IsBackdropSettled).IsFalse();
            AssertThat(screen.Mode).IsEqual(ResultScreenMode.Defeat);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void VictoryAndDefeat_AreDistinguishableByModeAndShapeLanguage()
    {
        var defeat = BuildDefeat();
        var victory = BuildVictory();
        try
        {
            AssertThat(defeat.Mode).IsNotEqual(victory.Mode);
            // Both keep the shared structure; only the composition differs.
            AssertThat(defeat.GetNodeOrNull<Control>("Block_Backdrop")).IsNotNull();
            AssertThat(victory.GetNodeOrNull<Control>("Block_Backdrop")).IsNotNull();
            AssertThat(defeat.GetNode<Label>("Block_Title/Title").Text).IsNotEqual(
                victory.GetNode<Label>("Block_Title/Title").Text);
        }
        finally
        {
            NodeTestSupport.Cleanup(defeat);
            NodeTestSupport.Cleanup(victory);
        }
    }
}
