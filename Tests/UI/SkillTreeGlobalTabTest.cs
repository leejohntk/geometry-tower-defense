using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// The fourth skill-tree sidebar entry (Global): same size/spacing/interaction as the
/// tower tabs, a four-node sequential chain rendered in the shared tree area, per-node
/// max ranks on the rank labels, locked/unlocked/buy/MAX states, the cyan accent, and
/// the info-band prerequisite hint.
/// </summary>
[TestSuite]
public class SkillTreeGlobalTabTest
{
    private static SkillTreeScreen BuildScreen(SkillTreeState state)
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new SkillTreeScreen(new SkillTree(state, _ => true)));
    }

    private static Button GlobalTab(SkillTreeScreen screen) =>
        screen.GetNode<Button>("Block_Sidebar/Tab_Global");

    private static Control GlobalRow(SkillTreeScreen screen, string id) =>
        screen.GetNode<Control>($"TreeArea/Row_{id.Replace('.', '_')}");

    [TestCase]
    public void GlobalTab_IsBelowTheLaserEntry_WithIdenticalSizeAndSpacing()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var arrow = screen.GetNode<Button>("Block_Sidebar/Sidebar_arrow");
            var cannon = screen.GetNode<Button>("Block_Sidebar/Sidebar_cannon");
            var laser = screen.GetNode<Button>("Block_Sidebar/Sidebar_laser");
            var global = GlobalTab(screen);

            AssertThat(global.Text).IsEqual(string.Empty);
            AssertThat(global.Size).IsEqual(laser.Size);
            AssertThat(global.Position.X).IsEqual(laser.Position.X);
            AssertThat(global.Position.Y).IsGreater(laser.Position.Y);

            // Same pitch as the tower entries that precede it.
            AssertThat(cannon.Position.Y - arrow.Position.Y)
                .IsEqualApprox(laser.Position.Y - cannon.Position.Y, 0.01f);
            AssertThat(global.Position.Y - laser.Position.Y)
                .IsEqualApprox(laser.Position.Y - cannon.Position.Y, 0.01f);

            // The sidebar wrapper stays click-through; the tab itself is interactive.
            AssertThat(screen.GetNode<Control>("Block_Sidebar").MouseFilter)
                .IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(global.GetNodeOrNull<Control>("Icon")).IsNotNull();
            AssertThat(global.GetNode<Control>("Icon").MouseFilter)
                .IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(global.GetNode<Label>("Label").Text).IsEqual("Global");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void SelectingGlobal_RendersTheFourNodeChainInUnlockOrder()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            var tree = screen.GetNode<Control>("TreeArea");
            var ids = new[]
            {
                SkillTreeCatalog.GlobalStartingHp,
                SkillTreeCatalog.GlobalStartingGold,
                SkillTreeCatalog.GlobalTowerDiscount,
                SkillTreeCatalog.GlobalUpgradeDiscount,
            };
            var names = new[] { "Fortified Core", "War Chest", "Master Blueprints", "Advanced Training" };
            var maxRanks = new[] { "0/5", "0/5", "0/3", "0/3" };

            float previousY = float.MinValue;
            for (int i = 0; i < ids.Length; i++)
            {
                var row = GlobalRow(screen, ids[i]);
                var icon = row.GetNode<Control>("Icon");
                AssertThat(icon.Size).IsEqual(new Vector2(46f, 46f));
                AssertThat(icon.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(row.GetNode<Label>("Name").Text).IsEqual(names[i]);
                AssertThat(row.GetNode<Label>("Rank").Text).IsEqual(maxRanks[i]);

                // Rows stack top-to-bottom in unlock order on the shared trunk spine.
                float rowCenterY = row.GetGlobalRect().GetCenter().Y;
                AssertThat(rowCenterY).IsGreater(previousY);
                previousY = rowCenterY;

                AssertThat(row.GetGlobalRect().Size.Y).IsGreaterEqual(icon.Size.Y);
                AssertThat(tree.GetGlobalRect().Encloses(row.GetGlobalRect())).IsTrue();
            }

            // Tower rows are not part of the global tab.
            AssertThat(tree.GetNodeOrNull<Control>("Row_arrow_damage")).IsNull();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void GlobalTab_WithNoSp_OnlyNodeOneIsUnlockedAndEveryBuyIsDisabled()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            var firstBuy = GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)
                .GetNode<Button>("Buy");
            AssertThat(firstBuy.Text).IsEqual($"Buy ({GameConstants.SkillNodeRankCost} SP)");
            AssertThat(firstBuy.Disabled).IsTrue();

            // Locked nodes render no Buy action at all.
            foreach (var id in new[]
            {
                SkillTreeCatalog.GlobalStartingGold,
                SkillTreeCatalog.GlobalTowerDiscount,
                SkillTreeCatalog.GlobalUpgradeDiscount,
            })
            {
                AssertThat(GlobalRow(screen, id).GetNodeOrNull<Button>("Buy")).IsNull();
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void HoveringALockedGlobalRow_ShowsThePrerequisiteHint()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            GlobalRow(screen, SkillTreeCatalog.GlobalTowerDiscount)
                .EmitSignal(Control.SignalName.MouseEntered);

            AssertThat(screen.InfoBandName).IsEqual("Master Blueprints");
            AssertThat(screen.InfoBandRank).IsEqual("0/3");
            AssertThat(screen.InfoBandDescription.Contains("Unlocks after War Chest reaches rank 1.")).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void GlobalRanksAndPerNodeMax_RenderOnTheRows()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalStartingHp, 5);
        state.SetRank(SkillTreeCatalog.GlobalStartingGold, 1);
        state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, 2);

        var screen = BuildScreen(state);
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)
                .GetNode<Label>("Rank").Text).IsEqual("5/5");
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalStartingGold)
                .GetNode<Label>("Rank").Text).IsEqual("1/5");
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalTowerDiscount)
                .GetNode<Label>("Rank").Text).IsEqual("2/3");
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalUpgradeDiscount)
                .GetNode<Label>("Rank").Text).IsEqual("0/3");

            // Max rank uses the existing gold MAX treatment; node 3 still has a Buy.
            var maxedBuy = GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)
                .GetNode<Button>("Buy");
            AssertThat(maxedBuy.Text).IsEqual("MAX");
            AssertThat(maxedBuy.Disabled).IsTrue();
            AssertThat(maxedBuy.GetThemeColor("font_color")).IsEqual(GameConstants.SkillTreeMaxColor);

            var discountBuy = GlobalRow(screen, SkillTreeCatalog.GlobalTowerDiscount)
                .GetNode<Button>("Buy");
            AssertThat(discountBuy.Text).IsEqual($"Buy ({GameConstants.SkillNodeRankCost} SP)");
            AssertThat(discountBuy.Disabled).IsTrue(); // 0 SP

            // Node 4 is unlocked because node 3 reached rank 1, so it gets a Buy.
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalUpgradeDiscount)
                .GetNodeOrNull<Button>("Buy")).IsNotNull();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void GlobalTab_UsesTheCyanAccent()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var arrowTitle = screen.GetNode<Label>("TreeArea/Block_TreeHeader/Title");
            AssertThat(arrowTitle.GetThemeColor("font_color"))
                .IsEqual(TowerGlyphs.ColorFor(TowerType.Arrow));

            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            var title = screen.GetNode<Label>("TreeArea/Block_TreeHeader/Title");
            AssertThat(title.GetThemeColor("font_color")).IsEqual(GameConstants.SkillGlobalAccentColor);
            AssertThat(screen.GetNode<Label>("Block_Sidebar/Tab_Global/Label")
                .GetThemeColor("font_color")).IsEqual(GameConstants.SkillGlobalAccentColor);

            // Distinct from every tower accent.
            foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
                AssertThat(GameConstants.SkillGlobalAccentColor).IsNotEqual(TowerGlyphs.ColorFor(type));
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void TwoPurchasesInOneVisit_UnlockTheNextNodeWithoutLeavingTheScreen()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost * 2);
        var screen = BuildScreen(state);
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);

            GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)
                .GetNode<Button>("Buy").EmitSignal(Button.SignalName.Pressed);

            AssertThat(state.GetRank(SkillTreeCatalog.GlobalStartingHp)).IsEqual(1);
            AssertThat(state.SkillPoints).IsEqual(GameConstants.SkillNodeRankCost);

            // The rebuilt global tab reflects the unlock immediately.
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)
                .GetNode<Label>("Rank").Text).IsEqual("1/5");
            var secondBuy = GlobalRow(screen, SkillTreeCatalog.GlobalStartingGold)
                .GetNode<Button>("Buy");
            AssertThat(secondBuy.Disabled).IsFalse();

            secondBuy.EmitSignal(Button.SignalName.Pressed);

            AssertThat(state.GetRank(SkillTreeCatalog.GlobalStartingGold)).IsEqual(1);
            AssertThat(state.SkillPoints).IsEqual(0);

            // Node 3 is now unlocked (buyable when SP returns) but unaffordable right now.
            var thirdBuy = GlobalRow(screen, SkillTreeCatalog.GlobalTowerDiscount)
                .GetNode<Button>("Buy");
            AssertThat(thirdBuy.Disabled).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void GlobalRows_AreAbsentFromTowerTabs()
    {
        var state = new SkillTreeState();
        var screen = BuildScreen(state);
        try
        {
            GlobalTab(screen).EmitSignal(Button.SignalName.Pressed);
            AssertThat(GlobalRow(screen, SkillTreeCatalog.GlobalStartingHp)).IsNotNull();

            screen.GetNode<Button>("Block_Sidebar/Sidebar_arrow")
                .EmitSignal(Button.SignalName.Pressed);

            AssertThat(screen.GetNodeOrNull<Control>("TreeArea/Row_global_starting_hp")).IsNull();
            AssertThat(screen.GetNodeOrNull<Control>("TreeArea/Row_arrow_damage")).IsNotNull();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }
}
