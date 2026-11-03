using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// End-to-end tests for the 1/2/3 tower-placement shortcuts. Every test builds the real
/// Main scene graph, enters a level through the real TitleScreen/GameHUD signal path, and
/// drives real viewport keyboard input with <see cref="Viewport.PushInput"/>, so the
/// assertions cover Main._Input routing, the level/affordability gates, and the shared
/// placement transition rather than only manager calls.
/// </summary>
[TestSuite]
public class MainHotkeysTest
{
    // Screen-space point of a cell that is inside the grid and off every test level's
    // path: row 1, column 0. CellCenter applies the HUD top-bar offset.
    private static readonly Vector2 ValidCell = CellCenter(1, 0);

    private static float TimeScale => (float)Engine.TimeScale;

    private static Main CreateStartedGame(int levelId)
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var main = NodeTestSupport.Attach(new Main());

        // Start the requested level through the real title-screen button signal.
        main.GetChildren().OfType<TitleScreen>().Single()
            .GetNode<Button>($"Block_LevelMenu/Level{levelId}")
            .EmitSignal(Button.SignalName.Pressed);

        return main;
    }

    private static GameManager GetManager(Main main) =>
        main.GetNode<GameManager>("GameManager");

    private static GameHUD GetHud(Main main) => main.GetNode<GameHUD>("GameHUD");

    private static Button GetCard(Main main, string name) =>
        GetHud(main).GetNode<Button>($"Block_Sidebar/{name}");

    private static Button GetStartWaveButton(Main main) => GetCard(main, "StartWave");

    private static Vector2 CellCenter(int row, int col) =>
        new(
            GameConstants.CellCenterX(col),
            GameConstants.BattlefieldTop + GameConstants.CellCenterY(row));

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

    private static void PushMouseMotion(Main main, Vector2 position)
    {
        main.GetViewport().PushInput(new InputEventMouseMotion { Position = position });
    }

    private static void PushLeftClick(Main main, Vector2 position)
    {
        main.GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = position,
        });
    }

    private static void PushRightClick(Main main, Vector2 position)
    {
        main.GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right,
            Pressed = true,
            Position = position,
        });
    }

    private static void PushCancel(Main main)
    {
        main.GetViewport().PushInput(new InputEventAction
        {
            Action = "ui_cancel",
            Pressed = true,
        });
    }

    private static void Cleanup(Main main)
    {
        // Placement changes the global cursor; restore it before tearing the graph down
        // so a failed test cannot leak the cross cursor into another suite.
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        NodeTestSupport.Cleanup(main);
    }

    [TestCase]
    public void DigitKeys_SelectEachTowerTypeOnALevelThatAllowsAll()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);

            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);

            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            PushKey(main, Key.Key1); // same key cancels
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(startWave.Disabled).IsFalse();

            PushKey(main, Key.Key2);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Cannon);
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            PushKey(main, Key.Key2);
            AssertThat(manager.IsPlacingTower).IsFalse();

            PushKey(main, Key.Key3);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Laser);
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void HotkeySelection_MatchesTheSidebarCardTransition()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);

            // Card route: the shared transition sets the type, disables Start Wave,
            // and spends nothing.
            GetCard(main, "PlaceArrow").EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);

            // A second card click exits the same way the hotkey does.
            GetCard(main, "PlaceArrow").EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(startWave.Disabled).IsFalse();

            // Keyboard route produces exactly the same observable state.
            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(startWave.Disabled).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void CardSelection_ShowsThePreviewImmediatelyAtTheLastPointerPosition()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var grid = manager.Grid!;

            PushMouseMotion(main, ValidCell);
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();

            // No motion between the card press and the assertion: the ghost must be
            // drawn from the already-known pointer position.
            GetCard(main, "PlaceArrow").EmitSignal(Button.SignalName.Pressed);

            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void AffordabilityGate_BelowCostIsANoOp_ExactCostEntersAndSpendsOnPlacement()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);

            // Spend the opening balance on one tower through the placement path, then
            // rebuild a balance exactly one coin below a tower's cost.
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, 0)).IsTrue();
            AssertThat(manager.Coins).IsEqual(0);

            manager.AddCoinsForTesting(GameConstants.ArrowTowerCost - 1); // 9 coins
            AssertThat(manager.Coins).IsEqual(GameConstants.ArrowTowerCost - 1);

            PushKey(main, Key.Key1);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(manager.Coins).IsEqual(GameConstants.ArrowTowerCost - 1);
            AssertThat(startWave.Disabled).IsFalse();
            // The press is still owned by play, even though it was a no-op.
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            // Exactly the cost is eligible; selecting spends nothing.
            manager.AddCoinsForTesting(1); // 10 coins
            PushKey(main, Key.Key1);

            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.Coins).IsEqual(GameConstants.ArrowTowerCost);
            AssertThat(manager.GetActiveTowers().Count).IsEqual(1);

            // Only the click spends, through the existing PlaceTower path.
            PushLeftClick(main, CellCenter(1, 0));

            AssertThat(manager.GetActiveTowers().Count).IsEqual(2);
            AssertThat(manager.Coins).IsEqual(0);
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(startWave.Disabled).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Level1Availability_DisallowedDigitsAreNoOps()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var cannonCard = GetCard(main, "PlaceCannon");
            var laserCard = GetCard(main, "PlaceLaser");

            // Level 1 hides Cannon and Laser; their cards are invisible and disabled.
            AssertThat(cannonCard.Visible).IsFalse();
            AssertThat(cannonCard.Disabled).IsTrue();
            AssertThat(laserCard.Visible).IsFalse();
            AssertThat(laserCard.Disabled).IsTrue();

            PushKey(main, Key.Key2);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            PushKey(main, Key.Key3);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            // Arrow stays keyboard-selectable and matches its enabled card.
            AssertThat(GetCard(main, "PlaceArrow").Disabled).IsFalse();
            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Level2Availability_OnlyTheAllowedDigitSelects()
    {
        var main = CreateStartedGame(2);
        try
        {
            var manager = GetManager(main);

            AssertThat(GetCard(main, "PlaceCannon").Visible).IsTrue();
            AssertThat(GetCard(main, "PlaceCannon").Disabled).IsFalse();
            AssertThat(GetCard(main, "PlaceLaser").Visible).IsFalse();
            AssertThat(GetCard(main, "PlaceLaser").Disabled).IsTrue();

            PushKey(main, Key.Key2);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Cannon);

            // Laser is disallowed on this level: the no-op leaves Cannon placement up.
            PushKey(main, Key.Key3);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Cannon);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            PushKey(main, Key.Key2); // cancel
            AssertThat(manager.IsPlacingTower).IsFalse();

            PushKey(main, Key.Key3);
            AssertThat(manager.IsPlacingTower).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void SameKey_TogglesPlacementOffWithoutPlacingOrSpending()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var grid = manager.Grid!;

            // The pointer is already over a valid cell when the shortcut is pressed:
            // the ghost appears immediately, with no further motion event.
            PushMouseMotion(main, ValidCell);
            PushKey(main, Key.Key1);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushKey(main, Key.Key1);

            // Identical to a second card click: no tower, no spend, ghost hidden,
            // Start Wave available again between waves.
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(manager.GetActiveTowers().Count).IsEqual(0);
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void HotkeySelection_WithThePointerOffGrid_WaitsForAValidCell()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var grid = manager.Grid!;

            // The last pointer position is over the sidebar, outside the battlefield.
            PushMouseMotion(main, GetCard(main, "PlaceArrow").GetGlobalRect().GetCenter());
            PushKey(main, Key.Key1);

            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();

            // Once the pointer moves onto a valid cell, the preview appears normally.
            PushMouseMotion(main, ValidCell);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void DifferentEligibleKey_SwitchesTypeAndTheNextClickPlacesIt()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);
            var grid = manager.Grid!;

            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            // Aim at a cell so the Arrow ghost/range ring is actually showing.
            PushMouseMotion(main, ValidCell);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushKey(main, Key.Key2);

            // Switching stays in placement, spends nothing, and never places by itself.
            // The preview is redrawn for the new type at the same pointer cell instead
            // of lingering as the old ghost or disappearing until the next motion.
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Cannon);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(manager.GetActiveTowers().Count).IsEqual(0);
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();

            // No motion between the switch and the click: the pointer cell is unchanged.
            PushLeftClick(main, ValidCell);

            AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
            AssertThat(manager.GetActiveTowers()[0].Type).IsEqual(TowerType.Cannon);
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins - GameConstants.CannonTowerCost);
            AssertThat(manager.IsPlacingTower).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void IneligiblePresses_DoNotDisturbActivePlacement()
    {
        var main = CreateStartedGame(2);
        try
        {
            var manager = GetManager(main);

            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            // Laser is unavailable on Level 2: consumed, but Arrow stays selected.
            PushKey(main, Key.Key3);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            // Numpad digits and Enter are out of scope: they never act on placement.
            PushKey(main, Key.Kp2);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            PushKey(main, Key.Enter);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.IsPlacingTower).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void UnaffordableDifferentKey_WhilePlacing_LeavesPlacementActive()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);

            // Create an upgrade-ready tower through the normal placement path.
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, 0)).IsTrue(); // 10 -> 0 coins
            var tower = manager.GetActiveTowers()[0];
            tower.AddXp(GameConstants.TowerXpThresholdRank2);

            manager.AddCoinsForTesting(20);
            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            // Spend below one tower's cost while still aiming (20 - 5 - 10 = 5).
            AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
            AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
            AssertThat(manager.Coins).IsEqual(5);

            PushKey(main, Key.Key2);

            // Cannon is allowed on Level 3 but unaffordable: consumed and ignored.
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.IsPlacingTower).IsTrue();
            AssertThat(manager.Coins).IsEqual(5);
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();

            // The active type's own key still cancels even though its cost is no
            // longer affordable: an unaffordable balance can never trap the player
            // in placement mode.
            PushKey(main, Key.Key1);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();
            AssertThat(manager.Coins).IsEqual(5);
            AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void KeyRepeatAndRelease_NeverChangePlacement()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);

            // A repeat or release while idle must not start placement.
            PushKey(main, Key.Key1, pressed: true, echo: true);
            PushKey(main, Key.Key1, pressed: false);
            AssertThat(manager.IsPlacingTower).IsFalse();

            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            // While aiming, OS repeat never cancels or switches, and release is inert.
            PushKey(main, Key.Key1, pressed: true, echo: true);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            PushKey(main, Key.Key2, pressed: true, echo: true);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            PushKey(main, Key.Key1, pressed: false);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.IsPlacingTower).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Digits_AreConsumed_SoAFocusedHudButtonNeverReceivesThem()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var startWave = GetStartWaveButton(main);

            startWave.GrabFocus();
            AssertThat(ReferenceEquals(main.GetViewport().GuiGetFocusOwner(), startWave)).IsTrue();

            int buttonPresses = 0;
            startWave.Pressed += () => buttonPresses++;

            PushKey(main, Key.Key1);

            AssertThat(buttonPresses).IsEqual(0);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(main.GetViewport().IsInputHandled()).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void TitleAndSkillTreeScreens_IgnoreDigits()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        var main = NodeTestSupport.Attach(new Main());
        try
        {
            var manager = GetManager(main);
            var title = main.GetChildren().OfType<TitleScreen>().Single();

            PushKey(main, Key.Key1);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(title.Visible).IsTrue();

            title.GetNode<Button>("Block_LevelMenu/SkillTree")
                .EmitSignal(Button.SignalName.Pressed);
            var skillTreeScreen = main.GetNode<SkillTreeScreen>("SkillTreeScreen");
            AssertThat(skillTreeScreen.Visible).IsTrue();
            AssertThat(title.Visible).IsFalse();

            PushKey(main, Key.Key1);
            PushKey(main, Key.Key2);
            PushKey(main, Key.Key3);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(skillTreeScreen.Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void AfterGameOverAndVictory_DigitsAreInert_UntilANewLevelStarts()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var title = main.GetChildren().OfType<TitleScreen>().Single();

            // Game over while aiming clears placement; later digits create nothing.
            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            manager.EmitSignal(GameManager.SignalName.GameOver);
            AssertThat(main.GetNode<ResultScreen>("GameOverScreen").Visible).IsTrue();
            AssertThat(manager.IsPlacingTower).IsFalse();

            PushKey(main, Key.Key1);
            PushKey(main, Key.Key2);
            PushKey(main, Key.Key3);
            AssertThat(manager.IsPlacingTower).IsFalse();

            // Back to the title, then a fresh level: no stale type or balance.
            main.GetNode<Button>("GameOverScreen/ReturnToTitle")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(title.Visible).IsTrue();
            AssertThat(manager.IsPlacingTower).IsFalse();

            title.GetNode<Button>("Block_LevelMenu/Level3")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
            AssertThat(manager.GetActiveTowers().Count).IsEqual(0);

            PushKey(main, Key.Key3);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Laser);

            // Victory overlay is just as inert (emit the result signal directly so the
            // SP award/save path never runs).
            manager.EmitSignal(GameManager.SignalName.Victory);
            AssertThat(main.GetNode<ResultScreen>("VictoryScreen").Visible).IsTrue();
            AssertThat(manager.IsPlacingTower).IsFalse();

            PushKey(main, Key.Key1);
            AssertThat(manager.IsPlacingTower).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void StartingANewLevel_ForgetsThePreviousLevelsPointerPosition()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var title = main.GetChildren().OfType<TitleScreen>().Single();

            // Track a valid cell while Level 1 is running.
            PushMouseMotion(main, ValidCell);

            // End the run and return to the title through the real button.
            manager.EmitSignal(GameManager.SignalName.GameOver);
            main.GetNode<Button>("GameOverScreen/ReturnToTitle")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(title.Visible).IsTrue();

            // Start Level 3 and select a type without moving the mouse: the previous
            // level's pointer cell must not seed a ghost in the new level.
            title.GetNode<Button>("Block_LevelMenu/Level3")
                .EmitSignal(Button.SignalName.Pressed);

            PushKey(main, Key.Key3);

            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Laser);
            AssertThat(manager.Grid!.IsPlacementPreviewVisible).IsFalse();

            // The new level's own motion establishes a fresh pointer cell.
            PushMouseMotion(main, ValidCell);
            AssertThat(manager.Grid!.IsPlacementPreviewVisible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Digits_CannotCreatePlacementOutsideGameStatePlaying()
    {
        var main = CreateStartedGame(3);
        try
        {
            var manager = GetManager(main);

            // Main still believes the run is live; the state gate is what blocks the
            // hotkey, so no placement state can exist outside GameState.Playing.
            manager.SetStateForTesting(GameState.GameOver);

            PushKey(main, Key.Key1);
            PushKey(main, Key.Key2);
            PushKey(main, Key.Key3);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void EscapeAndRightClick_CancelHotkeyPlacement()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);
            var grid = manager.Grid!;

            PushKey(main, Key.Key1);
            PushMouseMotion(main, ValidCell);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushCancel(main);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();

            PushKey(main, Key.Key1);
            PushMouseMotion(main, ValidCell);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushRightClick(main, ValidCell);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void SpaceBehavior_IsUnchangedDuringHotkeyPlacement()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);

            PushKey(main, Key.Space); // start wave 1
            AssertThat(manager.WaveManager!.IsWaveActive).IsTrue();

            PushKey(main, Key.Key1);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);

            PushKey(main, Key.Space); // a running wave still takes Space

            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(manager.WaveManager.CurrentWave).IsEqual(1);
        }
        finally
        {
            Engine.TimeScale = GameConstants.NormalTimeScale;
            Cleanup(main);
        }
    }

    [TestCase]
    public void HotkeySelection_ClearsTheUpgradeSelectionAndPanel()
    {
        var main = CreateStartedGame(1);
        try
        {
            var manager = GetManager(main);

            // Place and select an upgrade-ready tower through the normal click path.
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, 0)).IsTrue();
            manager.GetActiveTowers()[0].AddXp(GameConstants.TowerXpThresholdRank1);

            PushLeftClick(main, CellCenter(0, 0));
            var panel = main.GetNode<TowerUpgradePanel>("Panel_TowerUpgrade");
            AssertThat(panel.IsOpen).IsTrue();

            // Restore the balance so the Arrow hotkey itself is eligible; selecting
            // must still drop the upgrade selection without spending anything.
            manager.AddCoinsForTesting(GameConstants.ArrowTowerCost);
            AssertThat(manager.Coins).IsEqual(GameConstants.ArrowTowerCost);

            PushKey(main, Key.Key1);

            // Placement wins over the panel, exactly like clicking the sidebar card.
            AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }
}
