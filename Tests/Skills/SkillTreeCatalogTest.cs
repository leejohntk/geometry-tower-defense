using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Catalog integrity tests: exactly 19 nodes (15 tower + 4 global), all enabled,
/// 5 per tower (3 trunk + 2 seed), 4 tower-independent global nodes in unlock order,
/// and unique ids.
/// </summary>
[TestSuite]
public class SkillTreeCatalogTest
{
    [TestCase]
    public void Catalog_ContainsExactlyNineteenNodes()
    {
        AssertThat(SkillTreeCatalog.All.Count).IsEqual(19);
    }

    [TestCase]
    public void Catalog_AllNineteenNodes_AreEnabled()
    {
        int enabled = CountNodes(n => n.Enabled);
        AssertThat(enabled).IsEqual(19);
    }

    [TestCase]
    public void Catalog_HasEightStatAndSevenMechanicTowerNodes()
    {
        int stat = 0;
        int mechanic = 0;
        foreach (var node in SkillTreeCatalog.All)
        {
            if (node.TowerType == null)
                continue;

            if (IsMechanicNode(node.Id)) mechanic++;
            else stat++;
        }

        AssertThat(stat).IsEqual(8);
        AssertThat(mechanic).IsEqual(7);
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
            foreach (var node in SkillTreeCatalog.ForTower(type))
            {
                // Tower tabs never leak the tower-independent global nodes.
                AssertThat(node.TowerType.HasValue).IsTrue();
                AssertThat(node.TowerType!.Value).IsEqual(type);
                AssertThat(node.IsGlobal).IsFalse();
            }
        }
    }

    [TestCase]
    public void Catalog_GlobalNodes_HaveTheSpecifiedIdsNamesAndMaxRanks()
    {
        var expected = new (string Id, string Name, int MaxRanks)[]
        {
            (SkillTreeCatalog.GlobalStartingHp, "Fortified Core", 5),
            (SkillTreeCatalog.GlobalStartingGold, "War Chest", 5),
            (SkillTreeCatalog.GlobalTowerDiscount, "Master Blueprints", 3),
            (SkillTreeCatalog.GlobalUpgradeDiscount, "Advanced Training", 3),
        };

        AssertThat(SkillTreeCatalog.GlobalNodes.Count).IsEqual(4);
        for (int i = 0; i < expected.Length; i++)
        {
            var node = SkillTreeCatalog.GlobalNodes[i];
            AssertThat(node.Id).IsEqual(expected[i].Id);
            AssertThat(node.DisplayName).IsEqual(expected[i].Name);
            AssertThat(node.MaxRanks).IsEqual(expected[i].MaxRanks);
            AssertThat(node.TowerType.HasValue).IsFalse();
            AssertThat(node.IsGlobal).IsTrue();
            AssertThat(node.Enabled).IsTrue();
        }

        // All includes the same four global nodes alongside the 15 tower nodes.
        foreach (var global in SkillTreeCatalog.GlobalNodes)
        {
            bool found = false;
            foreach (var node in SkillTreeCatalog.All)
            {
                if (ReferenceEquals(node, global))
                    found = true;
            }
            AssertThat(found).IsTrue();
        }
    }

    [TestCase]
    public void Catalog_TowerNodes_DefaultToFiveMaxRanks()
    {
        foreach (var node in SkillTreeCatalog.All)
        {
            if (node.TowerType == null)
                continue;

            AssertThat(node.MaxRanks).IsEqual(GameConstants.SkillMaxRanks);
        }
    }

    [TestCase]
    public void GetPrerequisite_GlobalNodes_ChainInUnlockOrder()
    {
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.GlobalStartingHp)).IsNull();
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.GlobalStartingGold))
            .IsEqual(SkillTreeCatalog.GlobalStartingHp);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.GlobalTowerDiscount))
            .IsEqual(SkillTreeCatalog.GlobalStartingGold);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.GlobalUpgradeDiscount))
            .IsEqual(SkillTreeCatalog.GlobalTowerDiscount);
    }

    [TestCase]
    public void UnlockHint_NamesThePreviousGlobalNode()
    {
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.GlobalStartingHp)).IsNull();
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.GlobalStartingGold))
            .IsEqual("Unlocks after Fortified Core reaches rank 1.");
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.GlobalTowerDiscount))
            .IsEqual("Unlocks after War Chest reaches rank 1.");
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.GlobalUpgradeDiscount))
            .IsEqual("Unlocks after Master Blueprints reaches rank 1.");
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
    public void Catalog_MechanicNodes_AreEnabled()
    {
        // The seven Part-2 nodes are all buyable now.
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowPierce)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.ArrowCritChance)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonCluster)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.CannonStunChance)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserIgnite)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserChain)!.Enabled).IsTrue();
        AssertThat(SkillTreeCatalog.Find(SkillTreeCatalog.LaserRampUp)!.Enabled).IsTrue();
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

    [TestCase]
    public void GetPrerequisite_FirstTrunkNodes_HaveNoPrerequisite()
    {
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.ArrowDamage)).IsNull();
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.CannonPowderCharge)).IsNull();
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.LaserDps)).IsNull();
    }

    [TestCase]
    public void GetPrerequisite_SubsequentTrunkNodes_RequirePreviousTrunk()
    {
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.ArrowAttackSpeed)).IsEqual(SkillTreeCatalog.ArrowDamage);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.ArrowRange)).IsEqual(SkillTreeCatalog.ArrowAttackSpeed);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.CannonAttackSpeed)).IsEqual(SkillTreeCatalog.CannonPowderCharge);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.CannonSplashRadius)).IsEqual(SkillTreeCatalog.CannonAttackSpeed);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.LaserRange)).IsEqual(SkillTreeCatalog.LaserDps);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.LaserIgnite)).IsEqual(SkillTreeCatalog.LaserRange);
    }

    [TestCase]
    public void GetPrerequisite_SeedNodes_RequireLastTrunk()
    {
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.ArrowPierce)).IsEqual(SkillTreeCatalog.ArrowRange);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.ArrowCritChance)).IsEqual(SkillTreeCatalog.ArrowRange);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.CannonCluster)).IsEqual(SkillTreeCatalog.CannonSplashRadius);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.CannonStunChance)).IsEqual(SkillTreeCatalog.CannonSplashRadius);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.LaserChain)).IsEqual(SkillTreeCatalog.LaserIgnite);
        AssertThat(SkillTreeCatalog.GetPrerequisite(SkillTreeCatalog.LaserRampUp)).IsEqual(SkillTreeCatalog.LaserIgnite);
    }

    [TestCase]
    public void GetPrerequisite_UnknownId_ReturnsNull()
    {
        AssertThat(SkillTreeCatalog.GetPrerequisite("does.not.exist")).IsNull();
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

    private static bool IsMechanicNode(string nodeId) => nodeId switch
    {
        SkillTreeCatalog.ArrowPierce => true,
        SkillTreeCatalog.ArrowCritChance => true,
        SkillTreeCatalog.CannonCluster => true,
        SkillTreeCatalog.CannonStunChance => true,
        SkillTreeCatalog.LaserIgnite => true,
        SkillTreeCatalog.LaserChain => true,
        SkillTreeCatalog.LaserRampUp => true,
        _ => false
    };

    private static int CountTowerNodes(TowerType type)
    {
        int count = 0;
        foreach (var _ in SkillTreeCatalog.ForTower(type))
            count++;
        return count;
    }
}
