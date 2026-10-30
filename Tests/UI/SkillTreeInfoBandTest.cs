using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Skill-tree hover-info band: idle hint, hovered name/rank/description, locked-node
/// prerequisite hint, reset on exit/tab switch/purchase, and icon-center hover
/// coverage. Hover is driven through the real MouseEntered/MouseExited signals.
/// </summary>
[TestSuite]
public class SkillTreeInfoBandTest
{
    private static Label InfoLabel(Node screen, string path) =>
        screen.GetNode<Label>($"Panel_Main/Block_SkillInfo/{path}");

    private static SkillTreeScreen BuildScreen(SkillTreeState state)
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new SkillTreeScreen(new SkillTree(state, _ => true)));
    }

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - 0.01f
        && inner.Position.Y >= outer.Position.Y - 0.01f
        && inner.End.X <= outer.End.X + 0.01f
        && inner.End.Y <= outer.End.Y + 0.01f;

    private static string SidebarKey(TowerType type) => type switch
    {
        TowerType.Cannon => "cannon",
        TowerType.Laser => "laser",
        _ => "arrow"
    };

    [TestCase]
    public void InfoBand_HasTheNamedStructureInsidePanelMain()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var band = screen.GetNodeOrNull<Control>("Panel_Main/Block_SkillInfo");
            AssertThat(band).IsNotNull();
            AssertThat(band!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            foreach (var child in new[] { "Panel", "Name", "Rank", "Description" })
            {
                var node = band.GetNodeOrNull<Control>(child);
                AssertThat(node).IsNotNull();
                AssertThat(node!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            }

            // The description wraps inside the band (no horizontal overflow).
            var description = InfoLabel(screen, "Description");
            AssertThat(description.AutowrapMode).IsEqual(TextServer.AutowrapMode.WordSmart);
            AssertThat(description.Size.X).IsLess(band.Size.X);

            // Idle state: neutral hint, no name/rank.
            AssertThat(InfoLabel(screen, "Name").Text).IsEqual(string.Empty);
            AssertThat(InfoLabel(screen, "Rank").Text).IsEqual(string.Empty);
            AssertThat(InfoLabel(screen, "Description").Text)
                .IsEqual(GameConstants.SkillInfoIdleHint);
            AssertThat(screen.GetNode<Control>("TreeArea/RowHighlight").Visible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void HoveringAnUnlockedRow_ShowsNameRankAndDescription()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            screen.GetNode<Control>("TreeArea/Row_arrow_damage")
                .EmitSignal(Control.SignalName.MouseEntered);

            AssertThat(screen.InfoBandName).IsEqual("Damage");
            AssertThat(screen.InfoBandRank).IsEqual("0/5");
            AssertThat(screen.InfoBandDescription.Contains("+2 damage")).IsTrue();
            AssertThat(screen.HoveredNodeId).IsEqual(SkillTreeCatalog.ArrowDamage);

            var highlight = screen.GetNode<Control>("TreeArea/RowHighlight");
            AssertThat(highlight.Visible).IsTrue();
            AssertThat(highlight.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void HoveringALockedRow_ShowsThePrerequisiteHint()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var lockedRow = screen.GetNode<Control>("TreeArea/Row_arrow_attack_speed");
            AssertThat(lockedRow.GetNodeOrNull<Button>("Buy")).IsNull();

            lockedRow.EmitSignal(Control.SignalName.MouseEntered);

            AssertThat(screen.InfoBandName).IsEqual("Attack Speed");
            AssertThat(screen.InfoBandRank).IsEqual("0/5");
            AssertThat(screen.InfoBandDescription.Contains("Unlocks after Damage reaches rank 1.")).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void MovingOffEveryRow_ReturnsTheBandToTheIdleHint()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var row = screen.GetNode<Control>("TreeArea/Row_arrow_damage");
            row.EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.HoveredNodeId).IsNotNull();

            row.EmitSignal(Control.SignalName.MouseExited);

            AssertThat(screen.HoveredNodeId).IsNull();
            AssertThat(screen.InfoBandName).IsEqual(string.Empty);
            AssertThat(screen.InfoBandRank).IsEqual(string.Empty);
            AssertThat(screen.InfoBandDescription).IsEqual(GameConstants.SkillInfoIdleHint);
            AssertThat(screen.GetNode<Control>("TreeArea/RowHighlight").Visible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void IconCenter_IsInsideTheRowHoverArea_AndClickThrough()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var row = screen.GetNode<Control>("TreeArea/Row_arrow_range");
            var icon = row.GetNode<Control>("Icon");

            AssertThat(icon.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(icon.Size).IsEqual(new Vector2(46f, 46f));
            AssertThat(row.GetGlobalRect().HasPoint(icon.GetGlobalRect().GetCenter())).IsTrue();
            // The row is the hover surface and fully covers the enlarged icon.
            AssertThat(Contains(row.GetGlobalRect(), icon.GetGlobalRect())).IsTrue();
            AssertThat(row.GetGlobalRect().Size.Y).IsGreaterEqual(icon.Size.Y);
            AssertThat(row.MouseFilter).IsEqual(Control.MouseFilterEnum.Pass);

            row.EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandName).IsEqual("Range");
            // Hover brightens the icon through Modulate only.
            AssertThat(icon.Modulate.R).IsGreater(1f);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void SyntheticRowExit_ResetsImmediately_WithoutAnyCursorState()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var row = screen.GetNode<Control>("TreeArea/Row_arrow_damage");
            row.EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.HoveredNodeId).IsEqual(SkillTreeCatalog.ArrowDamage);

            // No cursor probe: a synthetic exit always resets.
            row.EmitSignal(Control.SignalName.MouseExited);

            AssertThat(screen.HoveredNodeId).IsNull();
            AssertThat(screen.InfoBandName).IsEqual(string.Empty);
            AssertThat(screen.InfoBandDescription).IsEqual(GameConstants.SkillInfoIdleHint);
            AssertThat(screen.GetNode<Control>("TreeArea/RowHighlight").Visible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void RowExitAfterBuyEnter_KeepsTheBandShown()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var row = screen.GetNode<Control>("TreeArea/Row_arrow_damage");
            var buy = row.GetNode<Button>("Buy");

            row.EmitSignal(Control.SignalName.MouseEntered);
            buy.EmitSignal(Control.SignalName.MouseEntered);
            // The pointer is over the Buy child; the row's exit must not clear the band.
            row.EmitSignal(Control.SignalName.MouseExited);

            AssertThat(screen.HoveredNodeId).IsEqual(SkillTreeCatalog.ArrowDamage);
            AssertThat(screen.InfoBandName).IsEqual("Damage");
            AssertThat(screen.InfoBandRank).IsEqual("0/5");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void RowToBuyThenOut_ShowsOnBuyAndClearsOnExit()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var row = screen.GetNode<Control>("TreeArea/Row_arrow_damage");
            var buy = row.GetNode<Button>("Buy");

            // Exit-first ordering (the engine's usual order when moving onto a child).
            row.EmitSignal(Control.SignalName.MouseEntered);
            row.EmitSignal(Control.SignalName.MouseExited);
            buy.EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandName).IsEqual("Damage");

            buy.EmitSignal(Control.SignalName.MouseExited);
            AssertThat(screen.InfoBandName).IsEqual(string.Empty);
            AssertThat(screen.InfoBandDescription).IsEqual(GameConstants.SkillInfoIdleHint);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void HaloPulseProcessing_StopsWhileHidden()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            AssertThat(screen.IsProcessing()).IsTrue();

            screen.Visible = false;
            AssertThat(screen.IsProcessing()).IsFalse();

            screen.Visible = true;
            AssertThat(screen.IsProcessing()).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void EnlargedIcons_FitEveryRowAcrossAllTowers()
    {
        // Every node unlocked (rank 1) renders a Buy button, so all 15 rows are measured
        // in their full content form across the three towers.
        var state = new SkillTreeState();
        foreach (var def in SkillTreeCatalog.All)
            state.SetRank(def.Id, 1);

        var screen = BuildScreen(state);
        try
        {
            var tree = screen.GetNode<Control>("TreeArea");
            Rect2 treeRect = tree.GetGlobalRect();
            float spineX = treeRect.GetCenter().X;
            float bandTop = screen.GetNode<Control>("Panel_Main/Block_SkillInfo")
                .GetGlobalRect().Position.Y;

            foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
            {
                screen.GetNode<Button>($"Block_Sidebar/Sidebar_{SidebarKey(type)}")
                    .EmitSignal(Button.SignalName.Pressed);

                float? leftSeedCenter = null;
                float? rightSeedCenter = null;

                foreach (var def in SkillTreeCatalog.ForTower(type))
                {
                    string rowName = "Row_" + def.Id.Replace('.', '_');
                    var row = tree.GetNode<Control>(rowName);
                    var icon = row.GetNode<Control>("Icon");
                    Rect2 iconRect = icon.GetGlobalRect();
                    Rect2 rowRect = row.GetGlobalRect();

                    AssertThat(icon.Size).IsEqual(new Vector2(46f, 46f));
                    AssertThat(Contains(rowRect, iconRect)).IsTrue();
                    AssertThat(rowRect.Size.Y).IsGreaterEqual(iconRect.Size.Y);
                    // No clipping at the tree-area edges.
                    AssertThat(Contains(treeRect, rowRect)).IsTrue();
                    // Icons never overlap the name, rank, or Buy content columns.
                    AssertThat(iconRect.Intersects(row.GetNode<Label>("Name").GetGlobalRect())).IsFalse();
                    AssertThat(iconRect.Intersects(row.GetNode<Label>("Rank").GetGlobalRect())).IsFalse();
                    var buy = row.GetNodeOrNull<Button>("Buy");
                    AssertThat(buy).IsNotNull();
                    AssertThat(iconRect.Intersects(buy!.GetGlobalRect())).IsFalse();
                    // The info band still sits below every row.
                    AssertThat(rowRect.End.Y).IsLessEqual(bandTop);

                    if (def.Kind == SkillNodeKind.Trunk)
                    {
                        // Icon centers stay on the trunk spine.
                        AssertThat(iconRect.GetCenter().X).IsEqualApprox(spineX, 0.5f);
                    }
                    else if (leftSeedCenter == null)
                    {
                        leftSeedCenter = iconRect.GetCenter().X;
                    }
                    else
                    {
                        rightSeedCenter = iconRect.GetCenter().X;
                    }
                }

                // Seed branches stay mirrored about the trunk spine.
                AssertThat(leftSeedCenter.HasValue).IsTrue();
                AssertThat(rightSeedCenter.HasValue).IsTrue();
                float leftCenter = leftSeedCenter ?? 0f;
                float rightCenter = rightSeedCenter ?? 0f;
                AssertThat(spineX - leftCenter)
                    .IsEqualApprox(rightCenter - spineX, 0.5f);
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void HoveringTheBuyButton_ShowsItsRowsInfo()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            screen.GetNode<Button>("TreeArea/Row_arrow_damage/Buy")
                .EmitSignal(Control.SignalName.MouseEntered);

            AssertThat(screen.InfoBandName).IsEqual("Damage");
            AssertThat(screen.InfoBandRank).IsEqual("0/5");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void SwitchingTowerTabs_ResetsTheBand_AndNewRowsHover()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            screen.GetNode<Control>("TreeArea/Row_arrow_damage")
                .EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandName).IsEqual("Damage");

            screen.GetNode<Button>("Block_Sidebar/Sidebar_cannon")
                .EmitSignal(Button.SignalName.Pressed);

            AssertThat(screen.InfoBandName).IsEqual(string.Empty);
            AssertThat(screen.InfoBandDescription).IsEqual(GameConstants.SkillInfoIdleHint);

            // The rebuilt cannon rows hover correctly.
            screen.GetNode<Control>("TreeArea/Row_cannon_powder_charge")
                .EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandName).IsEqual("Powder Charge");
            AssertThat(screen.InfoBandRank).IsEqual("0/5");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void BuyingARank_ResetsTheBand_AndTheRebuiltRowShowsTheNewRank()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost);
        var screen = BuildScreen(state);
        try
        {
            screen.GetNode<Control>("TreeArea/Row_arrow_damage")
                .EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandName).IsEqual("Damage");

            screen.GetNode<Button>("TreeArea/Row_arrow_damage/Buy")
                .EmitSignal(Button.SignalName.Pressed);

            AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(1);
            AssertThat(screen.InfoBandName).IsEqual(string.Empty);
            AssertThat(screen.InfoBandDescription).IsEqual(GameConstants.SkillInfoIdleHint);

            screen.GetNode<Control>("TreeArea/Row_arrow_damage")
                .EmitSignal(Control.SignalName.MouseEntered);
            AssertThat(screen.InfoBandRank).IsEqual("1/5");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void MaxedRow_ShowsMaxTextAndGoldTreatment()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowDamage, GameConstants.SkillMaxRanks);
        var screen = BuildScreen(state);
        try
        {
            var buy = screen.GetNode<Button>("TreeArea/Row_arrow_damage/Buy");
            AssertThat(buy.Text).IsEqual("MAX");
            AssertThat(buy.HasThemeStyleboxOverride("normal")).IsTrue();
            AssertThat(buy.GetThemeColor("font_color")).IsEqual(GameConstants.SkillTreeMaxColor);

            screen.GetNode<Control>("TreeArea/Row_arrow_damage")
                .EmitSignal(Control.SignalName.MouseEntered);

            // Rank 5/5 with no Buy requirement for hover.
            AssertThat(screen.InfoBandRank).IsEqual("5/5");
            AssertThat(screen.InfoBandDescription.Length).IsGreater(0);
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }

    [TestCase]
    public void TreeBackdropAndHeaderEmblemExist_AndAreClickThrough()
    {
        var screen = BuildScreen(new SkillTreeState());
        try
        {
            var panel = screen.GetNode<Control>("Panel_Main");
            var backdrop = panel.GetNodeOrNull<Control>("Block_TreeBackdrop");
            AssertThat(backdrop).IsNotNull();
            AssertThat(backdrop!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(backdrop.Size).IsEqual(panel.Size);

            var emblem = screen.GetNodeOrNull<Control>("TreeArea/Block_TreeHeader/Emblem");
            AssertThat(emblem).IsNotNull();
            AssertThat(emblem!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            // The emblem sits beside the centered Title, not over it.
            var headerTitle = screen.GetNode<Label>("TreeArea/Block_TreeHeader/Title");
            AssertThat(emblem.GetGlobalRect().Intersects(headerTitle.GetGlobalRect())).IsFalse();
            AssertThat(headerTitle.HorizontalAlignment).IsEqual(HorizontalAlignment.Center);
            var treeRect = screen.GetNode<Control>("TreeArea").GetGlobalRect();
            AssertThat(headerTitle.GetGlobalRect().GetCenter().X)
                .IsEqualApprox(treeRect.GetCenter().X, 0.5f);

            var spBadge = screen.GetNodeOrNull<Control>("Block_Title/SpBadge");
            AssertThat(spBadge).IsNotNull();
            AssertThat(spBadge!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            // The SP label keeps its exact text format.
            AssertThat(screen.GetNode<Label>("Block_Title/SkillPoints").Text)
                .IsEqual("Skill Points: 0");
        }
        finally
        {
            NodeTestSupport.Cleanup(screen);
        }
    }
}
