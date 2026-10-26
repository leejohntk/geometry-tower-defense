using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// End-to-end input-routing tests for <see cref="Main"/>. Each test builds the real
/// Main scene graph, enters Level 1 through the real TitleScreen/GameHUD signal path,
/// and then drives real viewport input with <see cref="Viewport.PushInput"/> — so the
/// assertions cover the same <c>_Input</c> routing the player exercises, not just the
/// underlying manager calls.
/// </summary>
[TestSuite]
public class MainTest
{
    // The game's logical viewport size, as configured in project.godot. The
    // headless root viewport is 64x64 no matter what the project says, so tests
    // must apply this size themselves before pushing real input coordinates.
    private const string ViewportWidthSetting = "display/window/size/viewport_width";
    private const string ViewportHeightSetting = "display/window/size/viewport_height";

    // Cell (col 0, row 1): inside the battlefield, below the HUD top bar, and off
    // Level 1's path (row 10).
    private static readonly Vector2 BattlefieldPoint =
        new Vector2(GameConstants.CellCenterX(0), GameConstants.CellCenterY(1));

    /// <summary>
    /// Size the root window from project.godot before building <see cref="Main"/>.
    /// Input pushed through <see cref="Viewport.PushInput"/> is transformed into the
    /// viewport's coordinate space, so at the headless 64x64 default the game's real
    /// coordinates (sidebar x~1390, battlefield y=96) fall outside it and never reach
    /// <c>Main._Input</c>. Assigning the size later than this is not safe: the tests
    /// assert the assignment took so an environment mismatch fails loudly here instead
    /// of as a mysterious missing preview.
    /// </summary>
    private static void ApplyProjectViewportSize()
    {
        AssertThat(ProjectSettings.HasSetting(ViewportWidthSetting) &&
                   ProjectSettings.HasSetting(ViewportHeightSetting))
            .OverrideFailureMessage(
                $"ProjectSettings is missing {ViewportWidthSetting} / {ViewportHeightSetting}; " +
                "MainTest cannot establish the real game viewport size.")
            .IsTrue();

        var expected = new Vector2I(
            ProjectSettings.GetSetting(ViewportWidthSetting).AsInt32(),
            ProjectSettings.GetSetting(ViewportHeightSetting).AsInt32());

        var root = (Window)((SceneTree)Engine.GetMainLoop()).Root;
        root.Size = expected;

        AssertThat(root.Size)
            .OverrideFailureMessage(
                $"Root viewport size did not take: expected {expected} from " +
                $"{ViewportWidthSetting} / {ViewportHeightSetting}, got {root.Size}. " +
                "MainTest pushes real game-coordinate input, so a wrong viewport size " +
                "makes every event land outside the viewport.")
            .IsEqual(expected);
    }

    private static Main CreateStartedGame()
    {
        ApplyProjectViewportSize();

        var main = new Main();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(main);

        // Start Level 1 through the real title-screen button signal.
        var title = main.GetChildren().OfType<TitleScreen>().Single();
        title.GetNode<Button>("Block_LevelMenu/Level1")
            .EmitSignal(Button.SignalName.Pressed);

        return main;
    }

    private static GameManager GetManager(Main main) =>
        main.GetNode<GameManager>("GameManager");

    private static GridManager GetGrid(Main main) => GetManager(main).Grid!;

    private static Button GetPlaceArrowButton(Main main) =>
        main.GetNode<GameHUD>("GameHUD").GetNode<Button>("Block_Sidebar/PlaceArrow");

    private static Button GetStartWaveButton(Main main) =>
        main.GetNode<GameHUD>("GameHUD").GetNode<Button>("Block_Sidebar/StartWave");

    private static void EnterPlacement(Main main)
    {
        var placeArrow = GetPlaceArrowButton(main);
        AssertThat(placeArrow.Disabled).IsFalse();

        // The HUD's real button signal routes into Main.OnPlaceTowerPressed.
        placeArrow.EmitSignal(Button.SignalName.Pressed);

        AssertThat(GetManager(main).IsPlacingTower).IsTrue();
        AssertThat(GetStartWaveButton(main).Disabled).IsTrue();
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

    private static void PushLeftClickAndRelease(Main main, Vector2 position)
    {
        PushLeftClick(main, position);
        main.GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
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
        // Detach before queueing the free so no later test can route input into it.
        main.GetParent()?.RemoveChild(main);
        main.QueueFree();
    }

    [TestCase]
    public void SidebarClick_WhilePlacing_CancelsConsumesClickAndReEnablesStartWave()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var grid = GetGrid(main);
            EnterPlacement(main);

            // Show the preview first so "hidden after exit" is a real transition.
            PushMouseMotion(main, BattlefieldPoint);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            // A click on the sidebar's Place Arrow button must cancel placement and be
            // consumed in Main._Input before GUI, so the button cannot act on the same
            // event and immediately start a new placement.
            var placeArrow = GetPlaceArrowButton(main);
            AssertThat(placeArrow.Disabled).IsFalse();
            int buttonPresses = 0;
            placeArrow.Pressed += () => buttonPresses++;

            PushLeftClickAndRelease(main, placeArrow.GetGlobalRect().GetCenter());

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(buttonPresses).IsEqual(0);
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Escape_WhilePlacing_CancelsPlacementAndHidesPreview()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var grid = GetGrid(main);
            EnterPlacement(main);

            PushMouseMotion(main, BattlefieldPoint);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushCancel(main);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void BattlefieldClick_PlacesOneTowerAndExitsPlacementWithCoinsRemaining()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var grid = GetGrid(main);

            // Start above one tower's cost so the regression condition is live:
            // coins remain after the placement consumes the selection.
            manager.AddCoinsForTesting(20);

            EnterPlacement(main);
            PushMouseMotion(main, BattlefieldPoint);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            PushLeftClick(main, BattlefieldPoint);

            AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
            AssertThat(manager.Coins)
                .IsEqual(GameConstants.StartingCoins + 20 - GameConstants.ArrowTowerCost);
            AssertThat(manager.Coins > 0).IsTrue();
            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(manager.PlacingTowerType).IsNull();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(GetStartWaveButton(main).Disabled).IsFalse();

            // A second click lands on the placed tower (selection/range path), never
            // a second placement.
            PushLeftClick(main, BattlefieldPoint);
            AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
            AssertThat(manager.IsPlacingTower).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void GameOverWhilePlacing_ExitsPlacementAndHidesPreview()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var grid = GetGrid(main);
            EnterPlacement(main);
            PushMouseMotion(main, BattlefieldPoint);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            // Production emits GameOver from GameManager; Main's handler must leave
            // placement before the result overlay covers the battlefield.
            manager.EmitSignal(GameManager.SignalName.GameOver);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("GameOverScreen").Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void VictoryWhilePlacing_ExitsPlacementAndHidesPreview()
    {
        var main = CreateStartedGame();
        try
        {
            var manager = GetManager(main);
            var grid = GetGrid(main);
            EnterPlacement(main);
            PushMouseMotion(main, BattlefieldPoint);
            AssertThat(grid.IsPlacementPreviewVisible).IsTrue();

            manager.EmitSignal(GameManager.SignalName.Victory);

            AssertThat(manager.IsPlacingTower).IsFalse();
            AssertThat(grid.IsPlacementPreviewVisible).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("VictoryScreen").Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }
}
