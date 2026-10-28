using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Per-tower in-level upgrade math: rank stat layering (fresh and maxed skill
/// trees), purchase visibility helpers, and the rank-3 archetype overlay rules.
/// </summary>
[TestSuite]
public class TowerUpgradeTest
{
    /// <summary>
    /// Drives the real rank path for ranks 0 -> 2. Rank 3 is only reachable through
    /// an archetype, so those tests call ApplyArchetype explicitly.
    /// </summary>
    private static void RaiseToRank2(Tower tower)
    {
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.Rank).IsEqual(2);
    }

    private static SkillTreeState MaxedArrowTree()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowDamage, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.ArrowAttackSpeed, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.ArrowRange, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.ArrowPierce, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.ArrowCritChance, GameConstants.SkillMaxRanks);
        return state;
    }

    private static SkillTreeState MaxedCannonTree()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonPowderCharge, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.CannonAttackSpeed, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.CannonSplashRadius, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.CannonCluster, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.CannonStunChance, GameConstants.SkillMaxRanks);
        return state;
    }

    private static SkillTreeState MaxedLaserTree()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserDps, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.LaserRange, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.LaserIgnite, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.LaserChain, GameConstants.SkillMaxRanks);
        state.SetRank(SkillTreeCatalog.LaserRampUp, GameConstants.SkillMaxRanks);
        return state;
    }

    [TestCase]
    public void PlacedTower_StartsAtRankZero_NoXpNoArchetype()
    {
        foreach (Tower tower in new Tower[] { new ArrowTower(), new CannonTower(), new LaserTower() })
        {
            AssertThat(tower.Rank).IsEqual(0);
            AssertThat(tower.Xp).IsEqual(0f);
            AssertThat(tower.Archetype).IsEqual(TowerArchetype.None);
            AssertThat(tower.HasArchetype).IsFalse();
        }
    }

    [TestCase]
    public void Rank3Arrow_WithNoSkillRanks_MatchesSpecTotals()
    {
        var tower = new ArrowTower();
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

        AssertThat(tower.Rank).IsEqual(GameConstants.TowerMaxRank);
        AssertThat(tower.Damage).IsEqualApprox(10.9f, 0.001f);
        AssertThat(tower.RangeCells).IsEqualApprox(5.5f, 0.0001f);
        AssertThat(tower.FireRate).IsEqualApprox(1.5f / 1.09f, 0.0005f);
    }

    [TestCase]
    public void Rank3Cannon_WithNoSkillRanks_MatchesSpecTotals()
    {
        var tower = new CannonTower();
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Cluster)).IsTrue();

        AssertThat(tower.Damage).IsEqualApprox(16.35f, 0.001f);
        AssertThat(tower.RangeCells).IsEqualApprox(5.5f, 0.0001f);
        AssertThat(tower.FireRate).IsEqualApprox(2.5f / 1.09f, 0.0005f);
        // Splash radius is trunk-only: the rank range bonus must never touch it.
        AssertThat(tower.SplashRadius).IsEqual(GameConstants.CannonTowerAoeRadius);
    }

    [TestCase]
    public void Rank3Laser_WithNoSkillRanks_MatchesSpecTotals()
    {
        var tower = new LaserTower();
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Chain)).IsTrue();

        AssertThat(tower.Dps).IsEqualApprox(4.36f, 0.001f);
        AssertThat(tower.RangeCells).IsEqualApprox(4.5f, 0.0001f);
    }

    [TestCase]
    public void FireRateMultiplier_IsTheIntervalDivisor_AndTheLaserDpsMultiplier()
    {
        AssertThat(TowerUpgrades.FireRateMultiplier(0)).IsEqual(1f);
        AssertThat(TowerUpgrades.FireRateMultiplier(3))
            .IsEqualApprox(1f + GameConstants.TowerRankFireRateBonusPerRank * 3, 0.0001f);
        AssertThat(TowerUpgrades.FireInterval(1.5f, 3))
            .IsEqualApprox(1.5f / TowerUpgrades.FireRateMultiplier(3), 0.0001f);

        // The laser has no interval, so the same multiplier scales its DPS.
        var laser = new LaserTower();
        RaiseToRank2(laser);
        AssertThat(laser.ApplyArchetype(TowerArchetype.Chain)).IsTrue();
        AssertThat(laser.Dps)
            .IsEqualApprox(GameConstants.LaserTowerDps * TowerUpgrades.FireRateMultiplier(3), 0.0001f);
        AssertThat(laser.Dps).IsEqualApprox(4.36f, 0.001f);
    }

    [TestCase]
    public void Rank3Arrow_WithMaxedTree_StacksRankOnSkillModifiers()
    {
        var tower = new ArrowTower();
        tower.SetSkillTree(MaxedArrowTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Crit)).IsTrue();

        // 20 base (skill maxed) x 1.09; 1.0s interval / 1.09; 6.5 + 1.5 cells.
        AssertThat(tower.Damage).IsEqualApprox(21.8f, 0.001f);
        AssertThat(tower.FireRate).IsEqualApprox(1f / 1.09f, 0.0005f);
        AssertThat(tower.RangeCells).IsEqualApprox(8f, 0.0001f);
    }

    [TestCase]
    public void Rank3Cannon_WithMaxedTree_StacksRankOnSkillModifiers_AndLeavesSplashAlone()
    {
        var tower = new CannonTower();
        tower.SetSkillTree(MaxedCannonTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Stun)).IsTrue();

        AssertThat(tower.Damage).IsEqualApprox(21.8f, 0.001f);
        AssertThat(tower.FireRate).IsEqualApprox((2.5f / 1.5f) / 1.09f, 0.0005f);
        AssertThat(tower.RangeCells).IsEqualApprox(5.5f, 0.0001f);
        AssertThat(tower.SplashRadius).IsEqual(104);
    }

    [TestCase]
    public void Rank3Laser_WithMaxedTree_StacksRankOnSkillDpsAndRange()
    {
        var tower = new LaserTower();
        tower.SetSkillTree(MaxedLaserTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.RampUp)).IsTrue();

        AssertThat(tower.Dps).IsEqualApprox(8f * 1.09f, 0.001f);
        AssertThat(tower.RangeCells).IsEqualApprox(7f, 0.0001f);
    }

    [TestCase]
    public void RankBonuses_ArePerTowerInstance()
    {
        var upgraded = new ArrowTower();
        RaiseToRank2(upgraded);
        AssertThat(upgraded.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

        var fresh = new ArrowTower();

        AssertThat(upgraded.Damage).IsEqualApprox(10.9f, 0.001f);
        AssertThat(fresh.Damage).IsEqual(10f);
        AssertThat(upgraded.RangeCells).IsEqualApprox(5.5f, 0.0001f);
        AssertThat(fresh.RangeCells).IsEqual(4f);
    }

    [TestCase]
    public void UpgradeCostsAndThresholds_MatchTheSpecTable()
    {
        AssertThat(TowerUpgrades.CostToReachRank(1)).IsEqual(5);
        AssertThat(TowerUpgrades.CostToReachRank(2)).IsEqual(15);
        AssertThat(TowerUpgrades.CostToReachRank(3)).IsEqual(25);

        AssertThat(TowerUpgrades.XpThresholdToReachRank(1)).IsEqual(5f);
        AssertThat(TowerUpgrades.XpThresholdToReachRank(2)).IsEqual(15f);
        AssertThat(TowerUpgrades.XpThresholdToReachRank(3)).IsEqual(30f);
    }

    [TestCase]
    public void HiddenGate_OpensExactlyAtEachThreshold()
    {
        AssertThat(TowerUpgrades.IsUpgradeVisible(0, 4.999f)).IsFalse();
        AssertThat(TowerUpgrades.IsUpgradeVisible(0, 5f)).IsTrue();
        AssertThat(TowerUpgrades.IsUpgradeVisible(1, 14.999f)).IsFalse();
        AssertThat(TowerUpgrades.IsUpgradeVisible(1, 15f)).IsTrue();
        AssertThat(TowerUpgrades.IsUpgradeVisible(2, 29.999f)).IsFalse();
        AssertThat(TowerUpgrades.IsUpgradeVisible(2, 30f)).IsTrue();

        // Rank 3 is final: no upgrade option is ever visible again.
        AssertThat(TowerUpgrades.IsUpgradeVisible(3, 1000f)).IsFalse();
    }

    [TestCase]
    public void GenericUpgrade_IsNeverOfferedAtRank2()
    {
        // Rank 2 -> 3 is the archetype choice, never a generic third chevron.
        AssertThat(TowerUpgrades.CanBuyNextRank(2, 1000f, 1000)).IsFalse();
        AssertThat(TowerUpgrades.CanBuyArchetype(2, 30f, 25)).IsTrue();
        AssertThat(TowerUpgrades.CanBuyArchetype(1, 1000f, 1000)).IsFalse();
    }

    [TestCase]
    public void RankUpgradeStopsAtRank2_AndArchetypeRequiresRank2()
    {
        var tower = new ArrowTower();
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsFalse();
        AssertThat(tower.Rank).IsEqual(0);

        RaiseToRank2(tower);
        AssertThat(tower.ApplyRankUpgrade()).IsFalse();
        AssertThat(tower.Rank).IsEqual(2);

        // A wrong-tower-type archetype is rejected outright.
        AssertThat(tower.ApplyArchetype(TowerArchetype.Cluster)).IsFalse();
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        AssertThat(tower.Rank).IsEqual(3);

        // Final: a second archetype (even a valid one) is rejected.
        AssertThat(tower.ApplyArchetype(TowerArchetype.Crit)).IsFalse();
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.Pierce);
    }

    [TestCase]
    public void XpAccumulatesFractionalShares_AndIgnoresInvalidAmounts()
    {
        var tower = new ArrowTower();
        tower.AddXp(0.4f);
        tower.AddXp(0.6f);
        AssertThat(tower.Xp).IsEqualApprox(1f, 0.0001f);

        tower.AddXp(-5f);
        tower.AddXp(float.NaN);
        tower.AddXp(float.PositiveInfinity);
        AssertThat(tower.Xp).IsEqualApprox(1f, 0.0001f);
    }

    [TestCase]
    public void EnemyXpValues_MatchTheSpecTable()
    {
        AssertThat(TowerUpgrades.EnemyXpValue(EnemyKind.Basic)).IsEqual(1);
        AssertThat(TowerUpgrades.EnemyXpValue(EnemyKind.Swarm)).IsEqual(1);
        AssertThat(TowerUpgrades.EnemyXpValue(EnemyKind.Armored)).IsEqual(3);
        AssertThat(TowerUpgrades.EnemyXpValue(EnemyKind.Progenitor)).IsEqual(10);
    }

    [TestCase]
    public void ArchetypeCatalog_MapsEachTowerToItsTwoSeeds()
    {
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Arrow)[0]).IsEqual(TowerArchetype.Pierce);
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Arrow)[1]).IsEqual(TowerArchetype.Crit);
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Cannon)[0]).IsEqual(TowerArchetype.Cluster);
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Cannon)[1]).IsEqual(TowerArchetype.Stun);
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Laser)[0]).IsEqual(TowerArchetype.Chain);
        AssertThat(TowerArchetypes.OptionsFor(TowerType.Laser)[1]).IsEqual(TowerArchetype.RampUp);

        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.Pierce)).IsEqual(SkillTreeCatalog.ArrowPierce);
        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.Crit)).IsEqual(SkillTreeCatalog.ArrowCritChance);
        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.Cluster)).IsEqual(SkillTreeCatalog.CannonCluster);
        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.Stun)).IsEqual(SkillTreeCatalog.CannonStunChance);
        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.Chain)).IsEqual(SkillTreeCatalog.LaserChain);
        AssertThat(TowerArchetypes.SeedNodeId(TowerArchetype.RampUp)).IsEqual(SkillTreeCatalog.LaserRampUp);

        AssertThat(TowerArchetypes.IsValidFor(TowerType.Arrow, TowerArchetype.Chain)).IsFalse();
        AssertThat(TowerArchetypes.IsValidFor(TowerType.Cannon, TowerArchetype.Pierce)).IsFalse();
    }

    [TestCase]
    public void BeforeRank3_AllSeedMechanicsAreInert_EvenWithMaxedSeeds()
    {
        // Seed ranks do not apply until an archetype is chosen. Rank 0, 1, and 2 must
        // read zero seed effect even on a fully maxed seed tree.
        var arrow = new ArrowTower();
        arrow.SetSkillTree(MaxedArrowTree());
        for (int rank = 0; rank <= 2; rank++)
        {
            AssertThat(arrow.Rank).IsEqual(rank);
            AssertThat(arrow.PierceCount).IsEqual(0);
            AssertThat(arrow.CritChance).IsEqual(0f);
            if (rank < 2)
                AssertThat(arrow.ApplyRankUpgrade()).IsTrue();
        }

        var cannon = new CannonTower();
        cannon.SetSkillTree(MaxedCannonTree());
        for (int rank = 0; rank <= 2; rank++)
        {
            AssertThat(cannon.Rank).IsEqual(rank);
            AssertThat(cannon.ClusterCount).IsEqual(1);
            AssertThat(cannon.StunChance).IsEqual(0f);
            if (rank < 2)
                AssertThat(cannon.ApplyRankUpgrade()).IsTrue();
        }

        var laser = new LaserTower();
        laser.SetSkillTree(MaxedLaserTree());
        for (int rank = 0; rank <= 2; rank++)
        {
            AssertThat(laser.Rank).IsEqual(rank);
            AssertThat(laser.ChainJumps).IsEqual(0);
            AssertThat(laser.RampMaxMultiplier).IsEqual(1f);
            // Ignite is a trunk node: it keeps applying before rank 3.
            AssertThat(laser.IgniteChancePerSecond).IsEqualApprox(0.25f, 0.0001f);
            if (rank < 2)
                AssertThat(laser.ApplyRankUpgrade()).IsTrue();
        }
    }

    [TestCase]
    public void BeforeRank3_TrunkBonusesStillApply()
    {
        var arrow = new ArrowTower();
        arrow.SetSkillTree(MaxedArrowTree());
        AssertThat(arrow.Damage).IsEqualApprox(20f, 0.0001f);
        AssertThat(arrow.FireRate).IsEqualApprox(1f, 0.0001f);
        AssertThat(arrow.RangeCells).IsEqualApprox(6.5f, 0.0001f);

        var cannon = new CannonTower();
        cannon.SetSkillTree(MaxedCannonTree());
        AssertThat(cannon.Damage).IsEqualApprox(20f, 0.0001f);
        AssertThat(cannon.SplashRadius).IsEqual(104);

        var laser = new LaserTower();
        laser.SetSkillTree(MaxedLaserTree());
        AssertThat(laser.Dps).IsEqualApprox(8f, 0.0001f);
        AssertThat(laser.RangeCells).IsEqualApprox(5.5f, 0.0001f);
        AssertThat(laser.IgniteChancePerSecond).IsEqualApprox(0.25f, 0.0001f);

        // The same trunk values survive a rank-2 tower that has not specialized yet.
        RaiseToRank2(arrow);
        AssertThat(arrow.Damage).IsEqualApprox(
            20f * (1f + GameConstants.TowerRankDamageBonusPerRank * 2), 0.001f);
        AssertThat(arrow.RangeCells).IsEqualApprox(7.5f, 0.0001f);
        AssertThat(arrow.PierceCount).IsEqual(0);
        AssertThat(arrow.CritChance).IsEqual(0f);
    }

    [TestCase]
    public void BeforeRank3_WithNoSkillRanks_SeedsContributeNothing()
    {
        AssertThat(new ArrowTower().PierceCount).IsEqual(0);
        AssertThat(new ArrowTower().CritChance).IsEqual(0f);
        AssertThat(new CannonTower().ClusterCount).IsEqual(1);
        AssertThat(new CannonTower().StunChance).IsEqual(0f);
        AssertThat(new LaserTower().ChainJumps).IsEqual(0);
        AssertThat(new LaserTower().RampMaxMultiplier).IsEqual(1f);
    }

    [TestCase]
    public void ArrowChoosingCrit_DropsPierce_AndKeepsTrunkBonuses()
    {
        var tower = new ArrowTower();
        tower.SetSkillTree(MaxedArrowTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Crit)).IsTrue();

        AssertThat(tower.CritChance).IsEqualApprox(0.4f, 0.0001f);
        AssertThat(tower.PierceCount).IsEqual(0);
        // Trunk (damage / attack speed / range) is unaffected by specialization.
        AssertThat(tower.Damage).IsEqualApprox(21.8f, 0.001f);
        AssertThat(tower.FireRate).IsEqualApprox(1f / 1.09f, 0.0005f);
        AssertThat(tower.RangeCells).IsEqualApprox(8f, 0.0001f);
    }

    [TestCase]
    public void ArrowChoosingPierce_DropsCrit_AndKeepsTrunkBonuses()
    {
        var tower = new ArrowTower();
        tower.SetSkillTree(MaxedArrowTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

        AssertThat(tower.PierceCount).IsEqual(5);
        AssertThat(tower.CritChance).IsEqual(0f);
        AssertThat(tower.Damage).IsEqualApprox(21.8f, 0.001f);
    }

    [TestCase]
    public void CannonChoosingStun_DropsCluster_KeepsSplashTrunk()
    {
        var tower = new CannonTower();
        tower.SetSkillTree(MaxedCannonTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Stun)).IsTrue();

        AssertThat(tower.ClusterCount).IsEqual(1);
        AssertThat(tower.StunChance).IsEqualApprox(0.3f, 0.0001f);
        AssertThat(tower.SplashRadius).IsEqual(104);
    }

    [TestCase]
    public void CannonChoosingCluster_DropsStun_KeepsSplashTrunk()
    {
        var tower = new CannonTower();
        tower.SetSkillTree(MaxedCannonTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Cluster)).IsTrue();

        AssertThat(tower.ClusterCount).IsEqual(6);
        AssertThat(tower.StunChance).IsEqual(0f);
        AssertThat(tower.SplashRadius).IsEqual(104);
    }

    [TestCase]
    public void LaserChoosingChain_DropsRamp_ButKeepsIgniteTrunk()
    {
        var tower = new LaserTower();
        tower.SetSkillTree(MaxedLaserTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Chain)).IsTrue();

        AssertThat(tower.ChainJumps).IsEqual(5);
        AssertThat(tower.RampMaxMultiplier).IsEqual(1f);
        // Ignite is a trunk node: it survives any archetype choice.
        AssertThat(tower.IgniteChancePerSecond).IsEqualApprox(0.25f, 0.0001f);
    }

    [TestCase]
    public void LaserChoosingRamp_DropsChain_ButKeepsIgniteTrunk()
    {
        var tower = new LaserTower();
        tower.SetSkillTree(MaxedLaserTree());
        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.RampUp)).IsTrue();

        AssertThat(tower.ChainJumps).IsEqual(0);
        AssertThat(tower.RampMaxMultiplier).IsEqualApprox(2f, 0.0001f);
        AssertThat(tower.IgniteChancePerSecond).IsEqualApprox(0.25f, 0.0001f);
    }

    [TestCase]
    public void ZeroRankSeed_SpecializationAppliesAtMinimumRankOne()
    {
        var pierce = new ArrowTower();
        RaiseToRank2(pierce);
        AssertThat(pierce.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        AssertThat(pierce.PierceCount).IsEqual(1);
        AssertThat(pierce.CritChance).IsEqual(0f);

        var crit = new ArrowTower();
        RaiseToRank2(crit);
        AssertThat(crit.ApplyArchetype(TowerArchetype.Crit)).IsTrue();
        AssertThat(crit.CritChance).IsEqualApprox(0.08f, 0.0001f);
        AssertThat(crit.PierceCount).IsEqual(0);

        var cluster = new CannonTower();
        RaiseToRank2(cluster);
        AssertThat(cluster.ApplyArchetype(TowerArchetype.Cluster)).IsTrue();
        AssertThat(cluster.ClusterCount).IsEqual(2);
        AssertThat(cluster.StunChance).IsEqual(0f);

        var stun = new CannonTower();
        RaiseToRank2(stun);
        AssertThat(stun.ApplyArchetype(TowerArchetype.Stun)).IsTrue();
        AssertThat(stun.ClusterCount).IsEqual(1);
        AssertThat(stun.StunChance).IsEqualApprox(0.06f, 0.0001f);

        var chain = new LaserTower();
        RaiseToRank2(chain);
        AssertThat(chain.ApplyArchetype(TowerArchetype.Chain)).IsTrue();
        AssertThat(chain.ChainJumps).IsEqual(1);
        AssertThat(chain.RampMaxMultiplier).IsEqual(1f);

        var ramp = new LaserTower();
        RaiseToRank2(ramp);
        AssertThat(ramp.ApplyArchetype(TowerArchetype.RampUp)).IsTrue();
        AssertThat(ramp.ChainJumps).IsEqual(0);
        AssertThat(ramp.RampMaxMultiplier).IsEqualApprox(1.2f, 0.0001f);
    }

    [TestCase]
    public void Rank3Tower_GetsAnArchetypeAccentOnItsBody()
    {
        var tower = new ArrowTower();
        AssertThat(tower.GetNodeOrNull<Control>("ArchetypeAccent")).IsNull();

        RaiseToRank2(tower);
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

        var accent = tower.GetNodeOrNull<Control>("ArchetypeAccent");
        AssertThat(accent).IsNotNull();
        AssertThat(accent!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
    }
}
