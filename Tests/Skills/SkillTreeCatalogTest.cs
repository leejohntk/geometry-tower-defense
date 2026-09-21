using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Catalog integrity tests: exactly 15 nodes, 8 enabled stat nodes, 7 disabled
/// mechanic nodes, 5 per tower (3 trunk + 2 seed), and unique ids.
/// </summary>
[TestSuite]
public class SkillTreeCatalogTest
{
    [TestCase]
    public void Catalog_ContainsExactlyFifteenNodes()
    {
        AssertThat(SkillTreeCatalog.All.Count).IsEqual(15);
    }

    [TestCase]
    public void Catalog_HasEightEnabledStatNodes()
    {
        int enabled = CountNodes(n => n.Enabled);
        AssertThat(enabled).IsEqual(8);
    }

    [TestCase]
    public void Catalog_HasSevenDisabledMechanicNodes()
    {
        int disabled = CountNodes(n => !n.Enabled);
        AssertThat(disabled).IsEqual(7);
    }

    [TestCase]
    public void Catalog_EachTower_HasThreeTrunkAndTwoSeedNodes()
    {
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            int trunk = 0;
            int seed = 0;
            foreach (var node in SkillTreeCatalog.ForTower(type))
            {
                if (node.Kind == SkillNodeKind.Trunk) trunk++;
                else seed++;
            }

            AssertThat(trunk).IsEqual(3);
            AssertThat(seed).IsEqual(2);
        }
    }

    [TestCase]
    public void Catalog_ForTower_ReturnsFiveNodesPerTower()
    {
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            AssertThat(CountTowerNodes(type)).IsEqual(5);
        }
    }

    [TestCase]
    public void Catalog_NodeIds_AreUnique()
    {
        var seen = new HashSet<string>();
        foreach (var node in SkillTreeCatalog.All)
        {
            AssertThat(seen.Add(node.Id)).IsTrue();
        }
    }

    [TestCase]
    public void Catalog_MechanicNodes_AreDisabled()
    {
        // The seven Part-2 nodes must all be disabled ("coming soon").
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowPierce)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowCritChance)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonCluster)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonStunChance)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserIgnite)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserChain)!.Enabled).IsFalse();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserRampUp)!.Enabled).IsFalse();
    }

    [TestCase]
    public void Catalog_StatNodes_AreEnabled()
    {
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowDamage)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowAttackSpeed)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowRange)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonPowderCharge)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonAttackSpeed)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonSplashRadius)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserDps)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserRange)!.Enabled).IsTrue();
    }

    [TestCase]
    public void Catalog_Find_ReturnsNullForUnknownId()
    {
        AssertThat(SkillTreeCatalog.Find("does.not.exist")).IsNull();
    }

    private static int CountNodes(System.Func<SkillNodeDefinition, bool> predicate)
    {
        int count = 0;
        foreach (var node in SkillTreeCatalog.All)
        {
            if (predicate(node))
                count++;
        }
        return count;
    }

    private static int CountTowerNodes(TowerType type)
    {
        int count = 0;
        foreach (var _ in SkillTreeCatalog.ForTower(type))
            count++;
        return count;
    }
}
