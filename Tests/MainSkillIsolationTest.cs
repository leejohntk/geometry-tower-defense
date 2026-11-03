using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Main's layout-verifier isolation hook: debug-only, installs one shared zero-rank
/// in-memory tree on every cost surface, and never writes the player's real save.
/// </summary>
[TestSuite]
public class MainSkillIsolationTest
{
    [TestCase]
    public void IsolateSkillTreeForVerification_InstallsOneZeroRankTreeOnEveryCostSurface()
    {
        var main = NodeTestSupport.Attach(new Main());
        try
        {
            var manager = main.GetNode<GameManager>("GameManager");
            var hud = main.GetNode<GameHUD>("GameHUD");
            var panel = main.GetNode<TowerUpgradePanel>("Panel_TowerUpgrade");
            var treeBeforeIsolation = manager.SkillTree;

            bool debugBuild = OS.IsDebugBuild();
            AssertThat(main.IsolateSkillTreeForVerification()).IsEqual(debugBuild);

            if (!debugBuild)
            {
                // Release builds refuse the hook and leave every surface untouched.
                AssertThat(manager.SkillTree == treeBeforeIsolation).IsTrue();
                return;
            }

            AssertThat(manager.SkillTree).IsNotNull();
            var isolatedState = manager.SkillTree!.State;
            AssertThat(isolatedState.SkillPoints).IsEqual(0);
            foreach (var node in SkillTreeCatalog.All)
                AssertThat(isolatedState.GetRank(node.Id)).IsEqual(0);

            // Every cost surface shares the exact same isolated state, so card prices,
            // upgrade costs, and placement previews all agree at zero ranks.
            AssertThat(hud.SkillState == isolatedState).IsTrue();
            AssertThat(panel.SkillState == isolatedState).IsTrue();
            AssertThat(hud.GetNode<Label>("Block_Sidebar/PlaceArrow/Cost").Text)
                .IsEqual("10");
        }
        finally
        {
            NodeTestSupport.Cleanup(main);
        }
    }
}
