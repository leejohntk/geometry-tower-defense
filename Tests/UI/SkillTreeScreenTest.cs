using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Regression coverage for the skill-tree screen's rebuild path. Switching tower tabs
/// must repopulate rank labels and buy-button state from the persistent
/// <see cref="SkillTreeState"/> instead of leaving the freshly built rows at their
/// hardcoded defaults.
/// </summary>
[TestSuite]
public class SkillTreeScreenTest
{
    private static Label GetLabel(Node root, string path)
    {
        var label = root.GetNodeOrNull<Label>(path);
        AssertThat(label).IsNotNull();
        return label;
    }

    private static Button GetButton(Node root, string path)
    {
        var button = root.GetNodeOrNull<Button>(path);
        AssertThat(button).IsNotNull();
        return button;
    }

    [TestCase]
    public void SwitchingTowerTabs_PreservesRankDisplayAndBuyState()
    {
        // One rank already bought on the arrow tree, leaving exactly 0 SP. The rank
        // lives in the state; the purchase limit is enforced there all along, so the
        // Buy button must come back disabled after a tab round-trip.
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost);
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        var tree = new SkillTree(state, _ => true);

        var screen = new SkillTreeScreen(tree);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(screen);
        try
        {
            // Initial load: _Ready builds the UI and the real rank is rendered.
            AssertThat(GetLabel(screen, "TreeArea/Row_arrow_damage/Rank").Text).IsEqual("1/5");

            // Drive the real sidebar signal path: switch to Cannon, then back to Arrow.
            GetButton(screen, "Block_Sidebar/Sidebar_cannon")
                .EmitSignal(Button.SignalName.Pressed);
            GetButton(screen, "Block_Sidebar/Sidebar_arrow")
                .EmitSignal(Button.SignalName.Pressed);

            // The rebuilt arrow row must read the persisted rank and affordability.
            AssertThat(GetLabel(screen, "TreeArea/Row_arrow_damage/Rank").Text).IsEqual("1/5");
            AssertThat(GetButton(screen, "TreeArea/Row_arrow_damage/Buy").Disabled).IsTrue();

            // The allocation itself was never lost — only the rebuilt labels were stale.
            AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(1);
        }
        finally
        {
            screen.GetParent()?.RemoveChild(screen);
            screen.QueueFree();
        }
    }
}
