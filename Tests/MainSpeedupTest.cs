using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// End-to-end tests for the space-bar speedup control. Every test builds the real
/// Main scene graph, enters Level 1 through the real TitleScreen/GameHUD signal path,
/// and drives real viewport keyboard input with <see cref="Viewport.PushInput"/>, so
/// the assertions cover Main._Input routing rather than only manager calls.
/// Every test restores <see cref="Engine.TimeScale"/> before node teardown in a
/// finally block, so a failure can never leak a sped-up clock into another suite.
/// </summary>
[TestSuite]
public class MainSpeedupTest
{
    private static float TimeScale => (float)Engine.TimeScale;

    private static Main CreateStartedGame()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var main = NodeTestSupport.Attach(new Main());

        // Start Level 1 through the real title-screen button signal.
        var title = main.GetChildren().OfType<TitleScreen>().Single();
        title.GetNode<Button>("Block_LevelMenu/Level1")
            .EmitSignal(Button.SignalName.Pressed);

        return main;
    }

    private static GameManager GetManager(Main main) =>
        main.GetNode<GameManager>("GameManager");

    private static GameHUD GetHud(Main main) => main.GetNode<GameHUD>("GameHUD");

    private static Button GetStartWaveButton(Main main) =>
        GetHud(main).GetNode<Button>("Block_Sidebar/StartWave");

    private static Button GetPlaceArrowButton(Main main) =>
        GetHud(main).GetNode<Button>("Block_Sidebar/PlaceArrow");

    private static SpeedupCue GetCue(Main main) =>
        GetHud(main).GetNode<SpeedupCue>("Block_TopBar/Block_Speedup");

    /// <summary>Pushes one physical key event through the real viewport.</summary>
    private static void PushKey(Main main, Key key, bool pressed = true, bool echo = false)
    {
        main.GetViewport().PushInput(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = pressed,
            Echo = echo,
        });
    }

    [TestCase]
    public void Space_OnTheTitleScreen_IsInert()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var main = NodeTestSupport.Attach(new Main());
        try
        {
            PushKey(main, Key.Space);

            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(GetManager(main).WaveManager).IsNull();
            AssertThat(main.GetChildren().OfType<TitleScreen>().Single().Visible).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_WhenAWaveIsReady_StartsTheWaveAndDisablesTheButton()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);
            AssertThat(startWave.Disabled).IsFalse();

            PushKey(main, Key.Space);

            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(manager.WaveManager.IsWaveActive).IsTrue();
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            // Starting a wave never also toggles the clock on the same press.
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_WhileAWaveIsActive_TogglesSpeedWithoutStartingAnotherWave()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var cue = GetCue(main);

            PushKey(main, Key.Space); // start wave 1
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(cue.Visible).IsFalse();

            PushKey(main, Key.Space); // toggle the clock up
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(manager.IsSpeedupActive).IsTrue();
            AssertThat(manager.WaveManager.CurrentWave).IsEqual(1);
            AssertThat(cue.Visible).IsTrue();

            PushKey(main, Key.Space); // toggle the clock back down
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(manager.WaveManager.CurrentWave).IsEqual(1);
            AssertThat(cue.Visible).IsFalse();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_KeyRepeatAndRelease_AreInert()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            // A repeat or a release while the wave is ready must not start it.
            PushKey(main, Key.Space, pressed: true, echo: true);
            PushKey(main, Key.Space, pressed: false);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(0);

            PushKey(main, Key.Space); // start wave 1
            PushKey(main, Key.Space); // speed up
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            PushKey(main, Key.Space, pressed: true, echo: true);
            PushKey(main, Key.Space, pressed: false);
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(manager.IsSpeedupActive).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void EnterAndJoypadAccept_NeverTriggerTheSpaceControl()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            // Wave ready: Enter must not start it.
            PushKey(main, Key.Enter);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(0);
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);

            PushKey(main, Key.Space); // start wave 1
            AssertThat(manager.WaveManager.IsWaveActive).IsTrue();

            // Wave active: Enter must not toggle the clock.
            PushKey(main, Key.Enter);
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);

            // A gamepad ui_accept (A) must not toggle it either.
            main.GetViewport().PushInput(new InputEventJoypadButton
            {
                Device = 0,
                ButtonIndex = JoyButton.A,
                Pressed = true,
            });
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(manager.WaveManager.CurrentWave).IsEqual(1);
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_DuringPlacementWithAnActiveWave_TogglesSpeedOnly()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            PushKey(main, Key.Space); // start wave 1
            GetPlaceArrowButton(main).EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();

            PushKey(main, Key.Space);

            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(manager.IsPlacingTower).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_DuringPlacementBetweenWaves_IsANoOp()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            GetPlaceArrowButton(main).EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.IsPlacingTower).IsTrue();

            PushKey(main, Key.Space);

            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(0);
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
            // Space is still consumed during play: a focused Button never also fires.
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_WithAFocusedPlaceCardDuringAWave_TogglesSpeedAndDoesNotPressTheCard()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var placeArrow = GetPlaceArrowButton(main);

            PushKey(main, Key.Space); // start wave 1

            placeArrow.GrabFocus();
            AssertThat(ReferenceEquals(main.GetViewport().GuiGetFocusOwner(), placeArrow)).IsTrue();

            int buttonPresses = 0;
            placeArrow.Pressed += () => buttonPresses++;

            PushKey(main, Key.Space);

            // A running wave takes the press: speed toggles, placement never starts,
            // and the focused card does not receive ui_accept from the same event.
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(buttonPresses).IsEqual(0);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_DuringPlacementBetweenWaves_DoesNotAlsoPressTheFocusedCard()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var placeArrow = GetPlaceArrowButton(main);

            placeArrow.EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.IsPlacingTower).IsTrue();

            // The card that started placement keeps keyboard focus; a consumed Space
            // press must never reach it as ui_accept and toggle placement back off.
            placeArrow.GrabFocus();
            AssertThat(ReferenceEquals(main.GetViewport().GuiGetFocusOwner(), placeArrow)).IsTrue();

            int buttonPresses = 0;
            placeArrow.Pressed += () => buttonPresses++;

            PushKey(main, Key.Space);

            AssertThat(buttonPresses).IsEqual(0);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(0);
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Space_WithAFocusedHudButton_DoesNotAlsoPressIt()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);

            startWave.GrabFocus();
            AssertThat(ReferenceEquals(main.GetViewport().GuiGetFocusOwner(), startWave)).IsTrue();

            int buttonPresses = 0;
            startWave.Pressed += () => buttonPresses++;

            PushKey(main, Key.Space);

            // Main handled the press: the wave started through the shared path, and
            // the focused Button never received ui_accept for the same key event.
            AssertThat(buttonPresses).IsEqual(0);
            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void AfterAWaveEndsAtSpeedup_SpaceStartsTheNextWaveInsteadOfToggling()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var waveManager = manager.WaveManager!;

            PushKey(main, Key.Space); // start wave 1
            PushKey(main, Key.Space); // speed up
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // Finish wave 1 for real (three kills). Wave completion must not reset
            // the clock, and the Start Wave button comes back enabled.
            WaveTestSupport.SpawnRemainingWaveEnemies(manager);
            foreach (var enemy in manager.GetActiveEnemies().ToList())
                enemy.TakeDamage(1000f);

            AssertThat(waveManager.IsWaveActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();

            // With no wave running the press starts the next wave, not toggles.
            PushKey(main, Key.Space);

            AssertThat(waveManager.CurrentWave).IsEqual(2);
            AssertThat(waveManager.IsWaveActive).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void Victory_ResetsTheClockAndHidesTheCue()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            // Victory awards SP and flushes it. Isolate the persistent skill tree so
            // the test never writes to the player's real save (Main loaded it at ready).
            manager.SkillTree = new SkillTree(new SkillTreeState(), _ => true);

            var cue = GetCue(main);

            PushKey(main, Key.Space); // start wave 1
            PushKey(main, Key.Space); // speed up
            AssertThat(cue.Visible).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // WaveManager emits this on the final wave's real completion; GameManager
            // handles it as the production victory path.
            manager.WaveManager!.EmitSignal(WaveManager.SignalName.AllWavesCompleted);

            AssertThat(manager.IsVictory).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(cue.Visible).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("VictoryScreen").Visible).IsTrue();

            // Space is inert while the result overlay is up.
            PushKey(main, Key.Space);
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);

            // Returning to the title keeps the normal clock and the hidden cue.
            main.GetNode<Button>("VictoryScreen/ReturnToTitle")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(cue.Visible).IsFalse();
            AssertThat(main.GetChildren().OfType<TitleScreen>().Single().Visible).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }

    [TestCase]
    public void GameOver_ResetsTheClockAndHidesTheCue()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);

            // Defeat awards SP and flushes it. Isolate the persistent skill tree so
            // the test never writes to the player's real save (Main loaded it at ready).
            manager.SkillTree = new SkillTree(new SkillTreeState(), _ => true);

            var cue = GetCue(main);

            PushKey(main, Key.Space); // start wave 1
            PushKey(main, Key.Space); // speed up
            AssertThat(cue.Visible).IsTrue();

            // Leak every enemy of wave 1. Level 1 starts at 3 HP and each Basic costs
            // 1: three arrivals defeat the run through the real arrival path.
            WaveTestSupport.SpawnRemainingWaveEnemies(manager);
            foreach (var enemy in manager.GetActiveEnemies().ToList())
                WaveTestSupport.RunToBase(enemy);

            AssertThat(manager.IsGameOver).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
            AssertThat(cue.Visible).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("GameOverScreen").Visible).IsTrue();
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            NodeTestSupport.Cleanup(main);
        }
    }
}
