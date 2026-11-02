using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Top-bar speedup cue: a named, click-through fast-forward group that is hidden at
/// the normal clock and visible (animating, processing) at the speedup clock. Its rect
/// is fixed across toggles, it sits right of Block_Wave inside the play area, and it
/// syncs through GameManager's SpeedupChanged signal.
/// </summary>
[TestSuite]
public class GameHudSpeedupCueTest
{
    private static GameHUD BuildHud()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new GameHUD());
    }

    private static void AssertClickThrough(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control control)
            {
                AssertThat(control.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertClickThrough(control);
            }
        }
    }

    [TestCase]
    public void Cue_HasStableNamesIsClickThroughAndIsHiddenAtTheNormalClock()
    {
        var hud = BuildHud();
        try
        {
            var bar = hud.GetNode<Control>("Block_TopBar");
            var cue = hud.GetNodeOrNull<Control>("Block_TopBar/Block_Speedup");
            AssertThat(cue).IsNotNull();
            AssertThat(cue!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            var icon = cue.GetNodeOrNull<Control>("FastForwardIcon");
            AssertThat(icon).IsNotNull();
            AssertThat(icon!.GetType() == typeof(Control)).IsTrue();
            AssertThat(icon.GetChildCount()).IsEqual(0); // procedural draw only

            AssertClickThrough(cue);

            // Hidden at 1.0x, and the whole group stays inside the 40px bar.
            AssertThat(cue.Visible).IsFalse();
            AssertThat(bar.GetGlobalRect().Encloses(cue.GetGlobalRect())).IsTrue();
            AssertThat(cue.IsProcessing()).IsFalse();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Cue_SitsRightOfTheWaveStatsAndLeftOfTheSidebar()
    {
        var hud = BuildHud();
        try
        {
            var wave = hud.GetNode<Control>("Block_TopBar/Block_Wave").GetGlobalRect();
            var cue = hud.GetNode<Control>("Block_TopBar/Block_Speedup").GetGlobalRect();

            AssertThat(cue.Position.X).IsGreater(wave.End.X);
            AssertThat(wave.Intersects(cue)).IsFalse();
            // Entirely inside the play area (the sidebar begins at PlayAreaWidth).
            AssertThat(cue.Position.X).IsGreaterEqual(0f);
            AssertThat(cue.End.X).IsLessEqual(GameConstants.PlayAreaWidth);
            // Same fixed width whether hidden or visible: the rect never shifts.
            AssertThat(cue.Size.X).IsEqual(GameConstants.HudSpeedupCueWidth);
            AssertThat(cue.Size.Y).IsEqual(GameConstants.HudSpeedupCueHeight);
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Cue_TogglesWithTheManagerSignal_AndKeepsItsRect()
    {
        var hud = BuildHud();
        var manager = new GameManager();
        try
        {
            manager.Initialize(Levels.Level1);
            hud.ConfigureForLevel(Levels.Level1);
            hud.ConnectToGameManager(manager);

            var cue = hud.GetNode<SpeedupCue>("Block_TopBar/Block_Speedup");
            Rect2 hiddenRect = cue.GetGlobalRect();
            bool hiddenProcessing = cue.IsProcessing();

            AssertThat(cue.Visible).IsFalse();
            AssertThat(hiddenProcessing).IsFalse();

            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(cue.Visible).IsTrue();
            AssertThat(cue.IsProcessing()).IsTrue();
            AssertThat(cue.GetGlobalRect().Position).IsEqual(hiddenRect.Position);
            AssertThat(cue.GetGlobalRect().Size).IsEqual(hiddenRect.Size);

            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(cue.Visible).IsFalse();
            AssertThat(cue.IsProcessing()).IsFalse();
            AssertThat(cue.GetGlobalRect().Position).IsEqual(hiddenRect.Position);
            AssertThat(cue.GetGlobalRect().Size).IsEqual(hiddenRect.Size);
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            manager.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Cue_SyncsTheInitialStateAndStopsFollowingTheOldManager()
    {
        var hud = BuildHud();
        var first = new GameManager();
        var second = new GameManager();
        try
        {
            first.Initialize(Levels.Level1);
            second.Initialize(Levels.Level1);
            AssertThat(first.ToggleSpeedup()).IsTrue();
            AssertThat(second.IsSpeedupActive).IsFalse();

            hud.ConnectToGameManager(first);
            var cue = hud.GetNode<SpeedupCue>("Block_TopBar/Block_Speedup");
            AssertThat(cue.Visible).IsTrue();

            // Reconnecting syncs to the new manager (normal clock) and drops the old
            // subscription. Flip the old manager OFF and back ON: a leaked subscription
            // would show the cue on the second flip, so staying hidden across both
            // changes actually proves the unsubscribe.
            hud.ConnectToGameManager(second);
            AssertThat(cue.Visible).IsFalse();

            AssertThat(first.ToggleSpeedup()).IsTrue(); // active -> inactive
            AssertThat(cue.Visible).IsFalse();
            AssertThat(first.ToggleSpeedup()).IsTrue(); // inactive -> active
            AssertThat(first.IsSpeedupActive).IsTrue();
            AssertThat(cue.Visible).IsFalse();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            first.QueueFree();
            second.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Cue_AnimatesOnlyWhileVisible()
    {
        var hud = BuildHud();
        var manager = new GameManager();
        try
        {
            manager.Initialize(Levels.Level1);
            hud.ConnectToGameManager(manager);
            var cue = hud.GetNode<SpeedupCue>("Block_TopBar/Block_Speedup");

            // Hidden: a manual tick is a no-op (and the engine would not call it).
            cue._Process(0.2);
            AssertThat(cue.Phase).IsEqual(0f);

            AssertThat(manager.ToggleSpeedup()).IsTrue();
            cue._Process(0.2);
            float afterOneTick = cue.Phase;
            AssertThat(afterOneTick).IsGreater(0f);

            cue._Process(0.2);
            AssertThat(cue.Phase).IsGreater(afterOneTick);

            // The loop wraps back into [0, 1) and the sweep/alpha curves stay bounded.
            for (int i = 0; i < 20; i++)
                cue._Process(0.2);
            AssertThat(cue.Phase).IsGreaterEqual(0f);
            AssertThat(cue.Phase).IsLess(1f);

            AssertThat(SpeedupCue.SweepOffsetAt(0f)).IsEqual(0f);
            AssertThat(SpeedupCue.SweepOffsetAt(0.5f))
                .IsEqualApprox(GameConstants.HudSpeedupSweepDistance / 2f, 0.0001f);
            AssertThat(SpeedupCue.AlphaAt(0.25f))
                .IsEqualApprox(GameConstants.HudSpeedupAlphaMax, 0.0001f);
            AssertThat(SpeedupCue.AlphaAt(0.75f))
                .IsEqualApprox(GameConstants.HudSpeedupAlphaMin, 0.0001f);

            // The authored loop is wall-clock paced: the engine scales the delta it
            // delivers, so the same real interval must land on the same phase at 1.5x
            // and at 1.0x (0.2 real seconds is a quarter of the 0.8 s cycle).
            cue.SetActive(true); // restart at phase 0 (clock currently at 1.5x)
            cue._Process(0.3);   // 0.2 real seconds delivered by a 1.5x clock
            AssertThat(cue.Phase).IsEqualApprox(0.25f, 0.0001f);

            AssertThat(manager.ToggleSpeedup()).IsTrue(); // back to the normal clock
            cue.SetActive(true); // restart at phase 0
            cue._Process(0.2);   // 0.2 real seconds at 1.0x
            AssertThat(cue.Phase).IsEqualApprox(0.25f, 0.0001f);

            // Hiding an ancestor stops the per-frame body even though the cue itself
            // is still visible and processing is enabled.
            hud.Visible = false;
            cue._Process(0.2);
            AssertThat(cue.Phase).IsEqualApprox(0.25f, 0.0001f);

            hud.Visible = true;
            cue._Process(0.2);
            AssertThat(cue.Phase).IsGreater(0.25f);
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            manager.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }
}
