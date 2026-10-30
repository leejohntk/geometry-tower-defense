using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// HUD placement cards: historical Button paths and signals are preserved, the content is
/// a procedural Icon/Name/Cost card, cards stack without overlap above StartWave, level
/// visibility rules are unchanged, and disabled cards dim their children.
/// </summary>
[TestSuite]
public class GameHudPlacementCardTest
{
    private static GameHUD BuildHud()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new GameHUD());
    }

    [TestCase]
    public void PlacementControls_KeepTheirPathsAndCardStructure()
    {
        var hud = BuildHud();
        try
        {
            var cases = new (string Name, TowerType Type, string Label, string Cost)[]
            {
                ("PlaceArrow", TowerType.Arrow, "Arrow", $"{GameConstants.ArrowTowerCost}$"),
                ("PlaceCannon", TowerType.Cannon, "Cannon", $"{GameConstants.CannonTowerCost}$"),
                ("PlaceLaser", TowerType.Laser, "Laser", $"{GameConstants.LaserTowerCost}$"),
            };

            foreach (var (name, type, label, cost) in cases)
            {
                var button = hud.GetNodeOrNull<Button>($"Block_Sidebar/{name}");
                AssertThat(button).IsNotNull();
                AssertThat(button!.Text).IsEqual(string.Empty);
                AssertThat(button.Size).IsEqual(new Vector2(
                    GameConstants.SidebarCardWidth,
                    GameConstants.SidebarCardHeight));
                AssertThat(button.GetNodeOrNull<Control>("Icon")).IsNotNull();
                AssertThat(button.GetNode<Label>("Name").Text).IsEqual(label);
                AssertThat(button.GetNode<Label>("Cost").Text).IsEqual(cost);
                AssertThat(button.GetNode<Control>("Icon").MouseFilter)
                    .IsEqual(Control.MouseFilterEnum.Ignore);

                // The signal payload is the tower type (unchanged contract).
                int received = -1;
                hud.PlaceTowerPressed += value => received = value;
                button.EmitSignal(Button.SignalName.Pressed);
                AssertThat(received).IsEqual((int)type);
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Cards_StackVertically_WithTheStartWaveButtonBelowThem()
    {
        var hud = BuildHud();
        try
        {
            var arrow = hud.GetNode<Button>("Block_Sidebar/PlaceArrow").GetGlobalRect();
            var cannon = hud.GetNode<Button>("Block_Sidebar/PlaceCannon").GetGlobalRect();
            var laser = hud.GetNode<Button>("Block_Sidebar/PlaceLaser").GetGlobalRect();
            var startWave = hud.GetNode<Button>("Block_Sidebar/StartWave").GetGlobalRect();

            // The stack starts below the HUD top bar.
            AssertThat(arrow.Position.Y).IsGreaterEqual(GameConstants.TopBarHeight);
            AssertThat(cannon.Position.Y).IsGreater(arrow.End.Y - 0.01f);
            AssertThat(laser.Position.Y).IsGreater(cannon.End.Y - 0.01f);
            AssertThat(startWave.Position.Y).IsGreaterEqual(laser.End.Y - 0.01f);
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void LevelVisibilityRules_AreUnchanged()
    {
        var hud = BuildHud();
        try
        {
            hud.ConfigureForLevel(Levels.Level1);
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceCannon").Visible).IsFalse();
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceLaser").Visible).IsFalse();

            hud.ConfigureForLevel(Levels.Level2);
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceCannon").Visible).IsTrue();
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceLaser").Visible).IsFalse();

            hud.ConfigureForLevel(Levels.Level3);
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceCannon").Visible).IsTrue();
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceLaser").Visible).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void DisabledCards_DimTheirChildren_AndReenablingRestoresBrightness()
    {
        var hud = BuildHud();
        var manager = new GameManager();
        try
        {
            manager.Initialize(Levels.Level1);
            hud.ConfigureForLevel(Levels.Level1);
            hud.ConnectToGameManager(manager);

            var arrow = hud.GetNode<Button>("Block_Sidebar/PlaceArrow");
            var icon = arrow.GetNode<Control>("Icon");
            var name = arrow.GetNode<Label>("Name");
            var cost = arrow.GetNode<Label>("Cost");

            // Starting coins (10) exactly cover one Arrow tower.
            AssertThat(arrow.Disabled).IsFalse();
            AssertThat(icon.Modulate).IsEqual(Colors.White);
            AssertThat(name.Modulate).IsEqual(Colors.White);

            // Spend the coins: the card becomes disabled and visibly dims.
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, 0)).IsTrue();
            AssertThat(manager.Coins).IsEqual(0);
            AssertThat(arrow.Disabled).IsTrue();
            AssertThat(icon.Modulate).IsEqual(GameConstants.SidebarCardDisabledModulate);
            AssertThat(name.Modulate).IsEqual(GameConstants.SidebarCardDisabledModulate);
            AssertThat(cost.Modulate).IsEqual(GameConstants.SidebarCardDisabledModulate);

            // Earning coins again restores full brightness.
            manager.AddCoinsForTesting(GameConstants.ArrowTowerCost);
            AssertThat(arrow.Disabled).IsFalse();
            AssertThat(icon.Modulate).IsEqual(Colors.White);
            AssertThat(cost.Modulate).IsEqual(Colors.White);
        }
        finally
        {
            manager.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void DisallowedTowerCard_IsDisabledEvenWhenAffordable()
    {
        var hud = BuildHud();
        var manager = new GameManager();
        try
        {
            manager.Initialize(Levels.Level1); // cannon/laser not allowed on level 1
            hud.ConfigureForLevel(Levels.Level1);
            hud.ConnectToGameManager(manager);

            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceCannon").Disabled).IsTrue();
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceLaser").Disabled).IsTrue();
            AssertThat(hud.GetNode<Button>("Block_Sidebar/PlaceArrow").Disabled).IsFalse();
        }
        finally
        {
            manager.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void StartWaveButton_KeepsItsPathAndEmits()
    {
        var hud = BuildHud();
        try
        {
            int presses = 0;
            hud.StartWavePressed += () => presses++;
            var startWave = hud.GetNode<Button>("Block_Sidebar/StartWave");
            AssertThat(startWave.Text).IsEqual("Start Wave");

            startWave.EmitSignal(Button.SignalName.Pressed);

            AssertThat(presses).IsEqual(1);
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }
}
