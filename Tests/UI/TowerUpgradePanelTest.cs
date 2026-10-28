using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tower upgrade panel behavior: a headerless single action, the hidden XP gate and
/// live reveal, archetype cards, docked positioning, input isolation, and the full
/// selection/close lifecycle through the real Main scene.
/// </summary>
[TestSuite]
public class TowerUpgradePanelTest
{
    // === Helpers ===

    private static Tower MakeTower(TowerType type, int row, int col)
    {
        Tower tower = type switch
        {
            TowerType.Cannon => new CannonTower(),
            TowerType.Laser => new LaserTower(),
            _ => new ArrowTower()
        };
        tower.Initialize(row, col);
        return tower;
    }

    private static void RaiseToRank2(Tower tower)
    {
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
    }

    private static TowerUpgradePanel CreatePanel(Tower tower, int coins)
    {
        var panel = new TowerUpgradePanel();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(panel);
        panel.ShowFor(tower, coins);
        return panel;
    }

    private static void Free(Node node)
    {
        node.GetParent()?.RemoveChild(node);
        node.QueueFree();
    }

    private static void AssertNoXpHintText(Node node)
    {
        switch (node)
        {
            case Label label:
                AssertThat(label.Text.ToLowerInvariant().Contains("xp")).IsFalse();
                AssertThat(label.Text.ToLowerInvariant().Contains("exp")).IsFalse();
                break;
            case Button button:
                AssertThat(button.Text.ToLowerInvariant().Contains("xp")).IsFalse();
                AssertThat(button.Text.ToLowerInvariant().Contains("exp")).IsFalse();
                break;
        }

        foreach (var child in node.GetChildren())
            AssertNoXpHintText(child);
    }

    private static Vector2 ExpectedButtonSize => new(
        GameConstants.TowerUpgradePanelWidth - 2f * GameConstants.TowerUpgradePanelPadding,
        GameConstants.TowerUpgradePanelUpgradeHeight);

    // === Standalone panel: structure, hidden gate, single action ===

    [TestCase]
    public void Panel_HasOnlyActionGroups_NoHeader_AndConsumesMouseInput()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(5f);
        var panel = CreatePanel(tower, 10);
        try
        {
            AssertThat(panel.Name.ToString()).IsEqual("Panel_TowerUpgrade");
            AssertThat(panel.MouseFilter).IsEqual(Control.MouseFilterEnum.Stop);
            AssertThat(panel.Visible).IsTrue();

            // No title block, separator, header label, or rank/chevron display.
            AssertThat(panel.GetNodeOrNull<Control>("Block_Rank")).IsNull();
            AssertThat(panel.GetNodeOrNull<Control>("Separator")).IsNull();
            AssertThat(panel.GetNodeOrNull<Label>("Block_Rank/Name")).IsNull();

            var upgradeBlock = panel.GetNodeOrNull<Control>("Block_Upgrade");
            var archetypeBlock = panel.GetNodeOrNull<Control>("Block_ArchetypeChoice");
            AssertThat(upgradeBlock).IsNotNull();
            AssertThat(archetypeBlock).IsNotNull();

            // Wrappers never consume input; only the panel itself and its buttons do.
            AssertThat(upgradeBlock!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(archetypeBlock!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            // The simplest panel: exactly one full-width Buy button, no extra children.
            AssertThat(upgradeBlock.GetChildCount()).IsEqual(1);
            var buy = panel.GetNodeOrNull<Button>("Block_Upgrade/Buy");
            AssertThat(buy).IsNotNull();
            AssertThat(buy!.Text).IsEqual("Upgrade 5");
            AssertThat(buy.Size).IsEqual(ExpectedButtonSize);
            AssertThat(panel.GetNodeOrNull<Control>("Block_Upgrade/Chevrons")).IsNull();
            AssertThat(panel.GetNodeOrNull<Label>("Block_Upgrade/Cost")).IsNull();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void BelowXpThreshold_ShowsNoPanelAndNoXpHint()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(4.9f);
        var panel = CreatePanel(tower, 100);
        try
        {
            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsFalse();

            AssertNoXpHintText(panel);
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void Panel_IsHidden_WheneverNoUpgradeIsAvailable()
    {
        // Rank 0 below 5 XP.
        var rankZero = MakeTower(TowerType.Arrow, 5, 10);
        rankZero.AddXp(4.9f);
        var zeroPanel = CreatePanel(rankZero, 100);
        try
        {
            AssertThat(zeroPanel.Visible).IsFalse();
            AssertThat(zeroPanel.IsOpen).IsFalse();
        }
        finally
        {
            Free(zeroPanel);
        }

        // Rank 1 below 15 XP.
        var rankOne = MakeTower(TowerType.Arrow, 5, 10);
        AssertThat(rankOne.ApplyRankUpgrade()).IsTrue();
        rankOne.AddXp(14.9f);
        var onePanel = CreatePanel(rankOne, 100);
        try
        {
            AssertThat(onePanel.Visible).IsFalse();
            AssertThat(onePanel.IsOpen).IsFalse();
        }
        finally
        {
            Free(onePanel);
        }

        // Rank 2 below 30 XP.
        var rankTwo = MakeTower(TowerType.Arrow, 5, 10);
        RaiseToRank2(rankTwo);
        rankTwo.AddXp(29.9f);
        var twoPanel = CreatePanel(rankTwo, 100);
        try
        {
            AssertThat(twoPanel.Visible).IsFalse();
            AssertThat(twoPanel.IsOpen).IsFalse();
        }
        finally
        {
            Free(twoPanel);
        }

        // Rank 3 has no further upgrade.
        var rankThree = MakeTower(TowerType.Arrow, 5, 10);
        RaiseToRank2(rankThree);
        rankThree.AddXp(30f);
        AssertThat(rankThree.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        var threePanel = CreatePanel(rankThree, 100);
        try
        {
            AssertThat(threePanel.Visible).IsFalse();
            AssertThat(threePanel.IsOpen).IsFalse();
        }
        finally
        {
            Free(threePanel);
        }
    }

    [TestCase]
    public void CrossingTheThresholdWhileSelected_RevealsThePanel_WithoutAutoBuying()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        var panel = CreatePanel(tower, 10);
        try
        {
            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();

            tower.AddXp(5f);
            panel._Process(0.016);

            var upgradeBlock = panel.GetNode<Control>("Block_Upgrade");
            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");
            AssertThat(panel.Visible).IsTrue();
            AssertThat(upgradeBlock.Visible).IsTrue();
            AssertThat(buy.Disabled).IsFalse();
            AssertThat(buy.Text).IsEqual("Upgrade 5");

            // The gate reveals the option; it never buys it.
            AssertThat(tower.Rank).IsEqual(0);
            AssertThat(panel.DisplayedTower == tower).IsTrue();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void CoinsShort_ShowsVisibleButDisabledUpgrade_ThenEnablesWhenCoinsArrive()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(5f);
        var panel = CreatePanel(tower, 4);
        try
        {
            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");
            AssertThat(panel.Visible).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsTrue();
            AssertThat(buy.Disabled).IsTrue();
            AssertThat(buy.Text).IsEqual("Upgrade 5");

            panel.UpdateCoins(5);
            AssertThat(buy.Disabled).IsFalse();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void GenericUpgradeButton_ShowsTheTargetCost_AndStaysFullWidth()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(5f);
        var panel = CreatePanel(tower, 100);
        try
        {
            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");
            AssertThat(buy.Text).IsEqual("Upgrade 5");
            AssertThat(buy.Size).IsEqual(ExpectedButtonSize);

            // Rank 1 with 15 XP previews the 10-coin second rank in the same button.
            AssertThat(tower.ApplyRankUpgrade()).IsTrue();
            tower.AddXp(10f);
            panel.Refresh();

            AssertThat(panel.Visible).IsTrue();
            AssertThat(buy.Text).IsEqual("Upgrade 10");
            AssertThat(buy.Size).IsEqual(ExpectedButtonSize);
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void Panel_HasNoHeader_AndTheTowerBadgeOwnsTheRankDisplay()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        var panel = CreatePanel(tower, 100);
        try
        {
            AssertThat(panel.GetNodeOrNull<Control>("Block_Rank")).IsNull();
            AssertThat(panel.GetNodeOrNull<Label>("Block_Rank/Name")).IsNull();
            AssertThat(panel.GetNodeOrNull<Control>("Separator")).IsNull();

            RaiseToRank2(tower);
            panel.Refresh();

            AssertThat(panel.GetNodeOrNull<Control>("Block_Rank")).IsNull();
            AssertThat(panel.GetNodeOrNull<Label>("Block_Rank/Name")).IsNull();

            // The placed tower's own badge is the single current-rank display.
            AssertThat(tower.GetNodeOrNull<Control>("RankBadge")).IsNotNull();
            AssertThat(panel.Visible).IsFalse();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void Rank2WithThirtyXp_ShowsTwoArchetypeCards_WithoutAHeader()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(30f);
        RaiseToRank2(tower);
        var panel = CreatePanel(tower, 100);
        try
        {
            AssertThat(panel.Visible).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();
            var archetypeBlock = panel.GetNode<Control>("Block_ArchetypeChoice");
            AssertThat(archetypeBlock.Visible).IsTrue();
            AssertThat(archetypeBlock.GetChildCount()).IsEqual(2);

            var left = panel.GetNode<Button>("Block_ArchetypeChoice/Card_Pierce");
            var right = panel.GetNode<Button>("Block_ArchetypeChoice/Card_Crit");
            AssertThat(left.Text).IsEqual("Pierce");
            AssertThat(right.Text).IsEqual("Crit");
            AssertThat(left.GetNode<Label>("Name").Text).IsEqual("Pierce");
            AssertThat(right.GetNode<Label>("Name").Text).IsEqual("Crit");
            AssertThat(left.GetNode<Label>("Cost").Text).IsEqual("15$");
            AssertThat(left.GetNodeOrNull<Control>("Icon")).IsNotNull();
            AssertThat(right.GetNodeOrNull<Control>("Icon")).IsNotNull();

            // No header above the cards.
            AssertThat(panel.GetNodeOrNull<Control>("Block_Rank")).IsNull();
            AssertThat(panel.GetNodeOrNull<Label>("Block_Rank/Name")).IsNull();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void Rank3_ShowsNoPanel_WithNoUpgradeControls()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(30f);
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        var panel = CreatePanel(tower, 100);
        try
        {
            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsFalse();
            AssertThat(panel.GetNodeOrNull<Control>("Block_Rank")).IsNull();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void AllThreeTowerTypes_RenderTheirOwnArchetypeCards()
    {
        var expectations = new (TowerType Type, string Left, string Right)[]
        {
            (TowerType.Arrow, "Card_Pierce", "Card_Crit"),
            (TowerType.Cannon, "Card_Cluster", "Card_Stun"),
            (TowerType.Laser, "Card_Chain", "Card_RampUp")
        };

        foreach (var (type, leftName, rightName) in expectations)
        {
            var tower = MakeTower(type, 5, 10);
            tower.AddXp(30f);
            RaiseToRank2(tower);
            var panel = CreatePanel(tower, 100);
            try
            {
                AssertThat(panel.Visible).IsTrue();
                AssertThat(panel.GetNodeOrNull<Button>($"Block_ArchetypeChoice/{leftName}")).IsNotNull();
                AssertThat(panel.GetNodeOrNull<Button>($"Block_ArchetypeChoice/{rightName}")).IsNotNull();
            }
            finally
            {
                Free(panel);
            }
        }
    }

    [TestCase]
    public void Positioning_DocksAboveCell_BelowForRowZero_AndClampsInsidePlayArea()
    {
        // Row 0: opens below the cell so it can never sit under the top bar.
        var rowZeroTower = MakeTower(TowerType.Arrow, 0, 5);
        rowZeroTower.AddXp(5f);
        var rowZero = CreatePanel(rowZeroTower, 10);
        try
        {
            float cellBottom = GameConstants.CellSize;
            AssertThat(rowZero.Visible).IsTrue();
            AssertThat(rowZero.Position.Y).IsGreaterEqual(cellBottom);
            AssertThat(rowZero.Position.Y).IsGreaterEqual(GameConstants.TopBarHeight);
        }
        finally
        {
            Free(rowZero);
        }

        // A mid-field tower: the panel sits fully above the cell.
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(5f); // upgrade visible
        var midRow = CreatePanel(tower, 100);
        try
        {
            float cellTop = 5 * GameConstants.CellSize;
            AssertThat(midRow.Visible).IsTrue();
            AssertThat(midRow.Size.Y).IsEqual(
                GameConstants.TowerUpgradePanelPadding * 2f + GameConstants.TowerUpgradePanelUpgradeHeight);
            AssertThat(midRow.Position.Y + midRow.Size.Y).IsLessEqual(cellTop);
            AssertThat(midRow.Position.Y).IsGreaterEqual(GameConstants.TopBarHeight);
        }
        finally
        {
            Free(midRow);
        }

        // Row 1 has no room above: the panel clamps below the top bar instead.
        var nearTopTower = MakeTower(TowerType.Arrow, 1, 10);
        nearTopTower.AddXp(5f);
        var nearTop = CreatePanel(nearTopTower, 10);
        try
        {
            AssertThat(nearTop.Position.Y).IsEqual(GameConstants.TopBarHeight);
        }
        finally
        {
            Free(nearTop);
        }

        // Left and right edges clamp inside the battlefield (never into the sidebar).
        var leftTower = MakeTower(TowerType.Arrow, 5, 0);
        leftTower.AddXp(5f);
        var leftEdge = CreatePanel(leftTower, 10);
        try
        {
            AssertThat(leftEdge.Position.X).IsEqual(0f);
        }
        finally
        {
            Free(leftEdge);
        }

        var rightTower = MakeTower(TowerType.Arrow, 5, GameConstants.GridCols - 1);
        rightTower.AddXp(5f);
        var rightEdge = CreatePanel(rightTower, 10);
        try
        {
            AssertThat(rightEdge.Position.X + rightEdge.Size.X)
                .IsEqual((float)GameConstants.PlayAreaWidth);
        }
        finally
        {
            Free(rightEdge);
        }

        // Archetype cards use the taller layout (padding * 2 + 84).
        var choiceTower = MakeTower(TowerType.Arrow, 5, 10);
        choiceTower.AddXp(30f);
        RaiseToRank2(choiceTower);
        var choicePanel = CreatePanel(choiceTower, 100);
        try
        {
            AssertThat(choicePanel.Visible).IsTrue();
            AssertThat(choicePanel.Size.Y).IsEqual(
                GameConstants.TowerUpgradePanelPadding * 2f + GameConstants.TowerUpgradePanelArchetypeHeight);
        }
        finally
        {
            Free(choicePanel);
        }
    }

    [TestCase]
    public void BuyAndCards_EmitThePanelSignals()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(5f);
        var panel = CreatePanel(tower, 100);
        try
        {
            int upgrades = 0;
            panel.UpgradeRequested += () => upgrades++;
            panel.GetNode<Button>("Block_Upgrade/Buy").EmitSignal(Button.SignalName.Pressed);
            AssertThat(upgrades).IsEqual(1);
        }
        finally
        {
            Free(panel);
        }

        var choiceTower = MakeTower(TowerType.Arrow, 5, 10);
        choiceTower.AddXp(30f);
        RaiseToRank2(choiceTower);
        var choicePanel = CreatePanel(choiceTower, 100);
        try
        {
            int chosen = -1;
            choicePanel.ArchetypeChosen += value => chosen = value;
            choicePanel.GetNode<Button>("Block_ArchetypeChoice/Card_Crit")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(chosen).IsEqual((int)TowerArchetype.Crit);
        }
        finally
        {
            Free(choicePanel);
        }
    }

    [TestCase]
    public void ArchetypeCards_AreVisibleButDisabledWhenCoinsAreShort()
    {
        var tower = MakeTower(TowerType.Cannon, 5, 10);
        tower.AddXp(30f);
        RaiseToRank2(tower);
        var panel = CreatePanel(tower, GameConstants.TowerUpgradeCostRank3 - 1);
        try
        {
            var block = panel.GetNode<Control>("Block_ArchetypeChoice");
            AssertThat(panel.Visible).IsTrue();
            AssertThat(block.Visible).IsTrue();
            AssertThat(panel.GetNode<Button>("Block_ArchetypeChoice/Card_Cluster").Disabled).IsTrue();
            AssertThat(panel.GetNode<Button>("Block_ArchetypeChoice/Card_Stun").Disabled).IsTrue();

            panel.UpdateCoins(GameConstants.TowerUpgradeCostRank3);
            AssertThat(panel.GetNode<Button>("Block_ArchetypeChoice/Card_Cluster").Disabled).IsFalse();
            AssertThat(panel.GetNode<Button>("Block_ArchetypeChoice/Card_Stun").Disabled).IsFalse();
        }
        finally
        {
            Free(panel);
        }
    }

    [TestCase]
    public void Rank2BelowThirtyXp_ShowsNoPanel()
    {
        var tower = MakeTower(TowerType.Arrow, 5, 10);
        tower.AddXp(29.9f);
        RaiseToRank2(tower);
        var panel = CreatePanel(tower, 100);
        try
        {
            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsFalse();
            AssertNoXpHintText(panel);
        }
        finally
        {
            Free(panel);
        }
    }

    // === Main integration: selection, transaction input, lifecycle ===

    private const string ViewportWidthSetting = "display/window/size/viewport_width";
    private const string ViewportHeightSetting = "display/window/size/viewport_height";

    private static void ApplyProjectViewportSize()
    {
        AssertThat(ProjectSettings.HasSetting(ViewportWidthSetting) &&
                   ProjectSettings.HasSetting(ViewportHeightSetting)).IsTrue();

        var expected = new Vector2I(
            ProjectSettings.GetSetting(ViewportWidthSetting).AsInt32(),
            ProjectSettings.GetSetting(ViewportHeightSetting).AsInt32());

        var root = (Window)((SceneTree)Engine.GetMainLoop()).Root;
        root.Size = expected;
        AssertThat(root.Size).IsEqual(expected);
    }

    private static Main CreateStartedGame()
    {
        ApplyProjectViewportSize();

        var main = new Main();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(main);

        main.GetChildren().OfType<TitleScreen>().Single()
            .GetNode<Button>("Block_LevelMenu/Level1")
            .EmitSignal(Button.SignalName.Pressed);

        return main;
    }

    private static GameManager GetManager(Main main) => main.GetNode<GameManager>("GameManager");

    private static TowerUpgradePanel GetPanel(Main main) =>
        main.GetNode<TowerUpgradePanel>("Panel_TowerUpgrade");

    private static Tower PlaceTower(Main main, TowerType type, int row, int col)
    {
        var manager = GetManager(main);
        manager.AddCoinsForTesting(100);
        manager.PlacingTowerType = type;
        AssertThat(manager.PlaceTower(row, col)).IsTrue();
        return manager.GetActiveTowers()[^1];
    }

    private static Vector2 CellCenter(int row, int col) =>
        new Vector2(GameConstants.CellCenterX(col), GameConstants.CellCenterY(row));

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

    private static bool HasRangeIndicator(Tower tower)
    {
        // HideRange queue-frees the indicator; within a single test frame the node is
        // still a child but already flagged for deletion.
        var indicator = tower.GetNodeOrNull<Control>("RangeIndicator");
        return indicator != null && !indicator.IsQueuedForDeletion();
    }

    private static void Cleanup(Main main)
    {
        main.GetParent()?.RemoveChild(main);
        main.QueueFree();
    }

    [TestCase]
    public void ClickingATowerWithAnUpgrade_OpensItsPanel_AndStillShowsTheRangeIndicator()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            var panel = GetPanel(main);
            AssertThat(panel.Visible).IsFalse();

            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));

            AssertThat(panel.IsOpen).IsTrue();
            AssertThat(panel.DisplayedTower == tower).IsTrue();
            AssertThat(panel.GetNode<Button>("Block_Upgrade/Buy").Text).IsEqual("Upgrade 5");
            AssertThat(HasRangeIndicator(tower)).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ClickingATowerWithNoUpgrade_SelectsItAndShowsRange_ButNoPanel()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            var panel = GetPanel(main);

            PushLeftClick(main, CellCenter(5, 10));

            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.DisplayedTower == tower).IsTrue();
            AssertThat(HasRangeIndicator(tower)).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ClickingTheUpgradeAction_BuysOnce_WithoutDeselectingTheTower()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            int coinsBefore = GetManager(main).Coins;

            PushLeftClick(main, CellCenter(5, 10));
            var panel = GetPanel(main);
            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");
            AssertThat(buy.Visible).IsTrue();
            AssertThat(buy.Disabled).IsFalse();
            AssertThat(buy.Text).IsEqual("Upgrade 5");

            PushLeftClickAndRelease(main, buy.GetGlobalRect().GetCenter());

            AssertThat(tower.Rank).IsEqual(1);
            AssertThat(GetManager(main).Coins).IsEqual(coinsBefore - 5);
            AssertThat(HasRangeIndicator(tower)).IsTrue();
            AssertThat(panel.DisplayedTower == tower).IsTrue();
            AssertThat(GetManager(main).GetActiveTowers().Count).IsEqual(1);
            AssertThat(GetManager(main).IsPlacingTower).IsFalse();

            // The next rank's 15-XP gate is closed, so the panel hides again.
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.Visible).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void BuyingRanks_RevealsEachNextAction_ThenHidesAfterTheArchetypeChoice()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(30f);
            PushLeftClick(main, CellCenter(5, 10));
            var panel = GetPanel(main);

            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");
            AssertThat(buy.Text).IsEqual("Upgrade 5");
            PushLeftClickAndRelease(main, buy.GetGlobalRect().GetCenter());
            AssertThat(tower.Rank).IsEqual(1);

            // 30 XP already opens the next gate, so the panel stays up with rank 2.
            AssertThat(panel.IsOpen).IsTrue();
            AssertThat(buy.Text).IsEqual("Upgrade 10");
            PushLeftClickAndRelease(main, buy.GetGlobalRect().GetCenter());
            AssertThat(tower.Rank).IsEqual(2);

            // Rank 2 with 30 XP switches to the archetype cards.
            AssertThat(panel.IsOpen).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();

            var card = panel.GetNode<Button>("Block_ArchetypeChoice/Card_Crit");
            PushLeftClickAndRelease(main, card.GetGlobalRect().GetCenter());
            AssertThat(tower.Rank).IsEqual(3);
            AssertThat(tower.Archetype).IsEqual(TowerArchetype.Crit);

            // Rank 3: no upgrade remains, so the panel hides.
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.Visible).IsFalse();
            AssertThat(HasRangeIndicator(tower)).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void BuyClickInsideAClampedPanel_OverTheTowerCell_DoesNotToggleRange()
    {
        var main = CreateStartedGame();
        try
        {
            // Row 1 has no room above: the panel clamps down over the tower's cell,
            // so the Buy button physically overlaps the tower click area.
            var tower = PlaceTower(main, TowerType.Arrow, row: 1, col: 10);
            tower.AddXp(5f);

            PushLeftClick(main, CellCenter(1, 10));
            var panel = GetPanel(main);
            var buy = panel.GetNode<Button>("Block_Upgrade/Buy");

            var buyCenter = buy.GetGlobalRect().GetCenter();
            AssertThat(new Rect2(
                new Vector2(GameConstants.CellCenterX(10) - GameConstants.CellSize / 2f,
                            GameConstants.CellCenterY(1) - GameConstants.CellSize / 2f),
                new Vector2(GameConstants.CellSize, GameConstants.CellSize))
                .HasPoint(buyCenter)).IsTrue();

            PushLeftClickAndRelease(main, buyCenter);

            AssertThat(tower.Rank).IsEqual(1);
            AssertThat(HasRangeIndicator(tower)).IsTrue(); // range was not toggled off
            // The next gate is closed, so the panel is gone after the purchase.
            AssertThat(panel.IsOpen).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void LiveXpGate_RevealsTheHiddenPanel_WithoutReselecting()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            PushLeftClick(main, CellCenter(5, 10));

            var panel = GetPanel(main);
            AssertThat(panel.Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();

            tower.AddXp(5f);
            panel._Process(0.016);

            AssertThat(panel.Visible).IsTrue();
            AssertThat(panel.IsOpen).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsTrue();
            AssertThat(panel.GetNode<Button>("Block_Upgrade/Buy").Text).IsEqual("Upgrade 5");
            AssertThat(tower.Rank).IsEqual(0);
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ClickingEmptyBattlefieldSpace_ClosesThePanelAndDeselects()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            PushLeftClick(main, CellCenter(8, 2));

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(HasRangeIndicator(tower)).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ClickingAnotherTower_SwitchesThePanel()
    {
        var main = CreateStartedGame();
        try
        {
            var first = PlaceTower(main, TowerType.Arrow, row: 5, col: 2);
            var second = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            first.AddXp(5f);
            second.AddXp(5f);

            PushLeftClick(main, CellCenter(5, 2));
            AssertThat(GetPanel(main).IsOpen).IsTrue();
            AssertThat(GetPanel(main).DisplayedTower == first).IsTrue();

            PushLeftClick(main, CellCenter(5, 10));

            AssertThat(GetPanel(main).IsOpen).IsTrue();
            AssertThat(GetPanel(main).DisplayedTower == second).IsTrue();
            AssertThat(HasRangeIndicator(first)).IsFalse();
            AssertThat(HasRangeIndicator(second)).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ClickingTheSameTowerAgain_TogglesRangeOffAndClosesThePanel()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();
            AssertThat(HasRangeIndicator(tower)).IsTrue();

            PushLeftClick(main, CellCenter(5, 10));

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(HasRangeIndicator(tower)).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Escape_ClosesThePanelAndTheRangeIndicator()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            PushCancel(main);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(HasRangeIndicator(tower)).IsFalse();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void EscapeWithNothingSelected_IsHarmless_AndLaterSelectionStillWorks()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            AssertThat(GetPanel(main).IsOpen).IsFalse();

            PushCancel(main);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(GetManager(main).IsPlacingTower).IsFalse();

            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();
            AssertThat(HasRangeIndicator(tower)).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void EnteringPlacementMode_ClosesThePanel()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            main.GetNode<GameHUD>("GameHUD")
                .GetNode<Button>("Block_Sidebar/PlaceArrow")
                .EmitSignal(Button.SignalName.Pressed);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(GetManager(main).IsPlacingTower).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void GameOver_ClosesThePanelBeforeTheResultOverlay()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            GetManager(main).EmitSignal(GameManager.SignalName.GameOver);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("GameOverScreen").Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void Victory_ClosesThePanelBeforeTheResultOverlay()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            GetManager(main).EmitSignal(GameManager.SignalName.Victory);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(main.GetNode<ResultScreen>("VictoryScreen").Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ReturnToTitle_ClosesThePanelAndDiscardsTowerState()
    {
        var main = CreateStartedGame();
        try
        {
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(5f);
            PushLeftClick(main, CellCenter(5, 10));
            AssertThat(GetPanel(main).IsOpen).IsTrue();

            main.GetNode<Button>("GameOverScreen/ReturnToTitle")
                .EmitSignal(Button.SignalName.Pressed);

            AssertThat(GetPanel(main).IsOpen).IsFalse();
            AssertThat(GetManager(main).GetActiveTowers().Count).IsEqual(0);
            AssertThat(main.GetChildren().OfType<TitleScreen>().Single().Visible).IsTrue();
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void ChoosingAnArchetypeCard_BuysThroughRealInput_AndIsFinal()
    {
        var main = CreateStartedGame();
        try
        {
            // 30 XP passes both rank gates; 100 extra coins cover 5 + 10 + 15.
            var tower = PlaceTower(main, TowerType.Arrow, row: 5, col: 10);
            tower.AddXp(30f);
            var manager = GetManager(main);
            AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
            AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
            AssertThat(tower.Rank).IsEqual(2);
            AssertThat(manager.Coins).IsEqual(85);

            PushLeftClick(main, CellCenter(5, 10));
            var panel = GetPanel(main);
            AssertThat(panel.IsOpen).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();

            var card = panel.GetNode<Button>("Block_ArchetypeChoice/Card_Crit");
            PushLeftClickAndRelease(main, card.GetGlobalRect().GetCenter());

            AssertThat(tower.Rank).IsEqual(3);
            AssertThat(tower.Archetype).IsEqual(TowerArchetype.Crit);
            AssertThat(manager.Coins).IsEqual(70);
            AssertThat(HasRangeIndicator(tower)).IsTrue();
            AssertThat(panel.GetNode<Control>("Block_ArchetypeChoice").Visible).IsFalse();
            AssertThat(panel.GetNode<Control>("Block_Upgrade").Visible).IsFalse();

            // Rank 3 means no upgrade remains: the panel hides entirely.
            AssertThat(panel.IsOpen).IsFalse();
            AssertThat(panel.Visible).IsFalse();

            // The stale second card cannot buy again (final, no respec, no refund).
            panel.GetNode<Button>("Block_ArchetypeChoice/Card_Pierce")
                .EmitSignal(Button.SignalName.Pressed);
            AssertThat(tower.Archetype).IsEqual(TowerArchetype.Crit);
            AssertThat(manager.Coins).IsEqual(70);
        }
        finally
        {
            Cleanup(main);
        }
    }

    [TestCase]
    public void PanelOpensForAllThreeTowerTypes_WithTheirOwnUpgradeAction()
    {
        var main = CreateStartedGame();
        try
        {
            var arrow = PlaceTower(main, TowerType.Arrow, row: 4, col: 2);
            var cannon = PlaceTower(main, TowerType.Cannon, row: 4, col: 8);
            var laser = PlaceTower(main, TowerType.Laser, row: 4, col: 14);

            var expectations = new[] { arrow, cannon, laser };
            foreach (var tower in expectations)
            {
                tower.AddXp(5f);
                PushLeftClick(main, new Vector2(tower.Position.X, tower.Position.Y));

                var panel = GetPanel(main);
                AssertThat(panel.IsOpen).IsTrue();
                AssertThat(panel.DisplayedTower == tower).IsTrue();
                AssertThat(panel.GetNode<Button>("Block_Upgrade/Buy").Text).IsEqual("Upgrade 5");
            }
        }
        finally
        {
            Cleanup(main);
        }
    }
}
