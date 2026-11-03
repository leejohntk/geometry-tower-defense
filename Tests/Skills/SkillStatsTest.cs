using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Exact stat-modifier math at rank 0 and rank 5, pinned to the spec tables.
/// Pure C# — no Godot runtime.
/// </summary>
[TestSuite]
public class SkillStatsTest
{
    [TestCase]
    public void ArrowDamage_Rank0AndRank5()
    {
        AssertThat(SkillStats.ArrowDamage(0)).IsEqual(10);
        AssertThat(SkillStats.ArrowDamage(5)).IsEqual(20);
    }

    [TestCase]
    public void ArrowFireRate_Rank0AndRank5()
    {
        AssertThat(SkillStats.ArrowFireRate(0)).IsEqual(1.5f);
        AssertThat(SkillStats.ArrowFireRate(5)).IsEqual(1.0f);
    }

    [TestCase]
    public void ArrowRange_Rank0AndRank5()
    {
        AssertThat(SkillStats.ArrowRangeCells(0)).IsEqual(4f);
        AssertThat(SkillStats.ArrowRangeCells(5)).IsEqual(6.5f);
    }

    [TestCase]
    public void CannonDamage_Rank0AndRank5()
    {
        AssertThat(SkillStats.CannonDamage(0)).IsEqual(15);
        AssertThat(SkillStats.CannonDamage(5)).IsEqual(20);
    }

    [TestCase]
    public void CannonProjectileSpeedMultiplier_Rank0AndRank5()
    {
        AssertThat(SkillStats.CannonProjectileSpeedMultiplier(0)).IsEqual(1f);
        AssertThat(SkillStats.CannonProjectileSpeedMultiplier(5)).IsEqual(1.5f);
    }

    [TestCase]
    public void CannonFireRate_Rank0AndRank5()
    {
        AssertThat(SkillStats.CannonFireRate(0)).IsEqual(2.5f);
        // ~1.67s at rank 5: 2.5 / (1 + 5*0.10).
        AssertThat(SkillStats.CannonFireRate(5)).IsEqual(2.5f / 1.5f);
    }

    [TestCase]
    public void CannonSplashRadius_Rank0AndRank5()
    {
        AssertThat(SkillStats.CannonSplashRadius(0)).IsEqual(64);
        AssertThat(SkillStats.CannonSplashRadius(5)).IsEqual(104);
    }

    [TestCase]
    public void LaserDps_Rank0AndRank5()
    {
        AssertThat(SkillStats.LaserDps(0)).IsEqual(4f);
        AssertThat(SkillStats.LaserDps(5)).IsEqual(8f);
    }

    [TestCase]
    public void LaserRange_Rank0AndRank5()
    {
        AssertThat(SkillStats.LaserRangeCells(0)).IsEqual(3f);
        AssertThat(SkillStats.LaserRangeCells(5)).IsEqual(5.5f);
    }

    [TestCase]
    public void Cannon_RemainsSlowestEvenAtMaxAttackSpeed()
    {
        // Cannon maxed (~1.67s) must stay slower than Arrow maxed (1.0s).
        AssertThat(SkillStats.CannonFireRate(5) > SkillStats.ArrowFireRate(5)).IsTrue();
    }

    [TestCase]
    public void RangeCells_Arrow_AppliesRangeRanks()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowRange, 5);

        AssertThat(SkillStats.RangeCells(TowerType.Arrow, null)).IsEqual(4f);
        AssertThat(SkillStats.RangeCells(TowerType.Arrow, state)).IsEqual(6.5f);
    }

    [TestCase]
    public void RangeCells_Laser_AppliesRangeRanks()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserRange, 5);

        AssertThat(SkillStats.RangeCells(TowerType.Laser, null)).IsEqual(3f);
        AssertThat(SkillStats.RangeCells(TowerType.Laser, state)).IsEqual(5.5f);
    }

    [TestCase]
    public void RangeCells_Cannon_IgnoresSkillRanks()
    {
        // Cannon has no range node, so purchased ranks never change its preview range.
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonPowderCharge, 5);

        AssertThat(SkillStats.RangeCells(TowerType.Cannon, state)).IsEqual(4f);
    }

    // === Global nodes ===

    [TestCase]
    public void StartingHp_AddsOnePerFortifiedCoreRank()
    {
        AssertThat(SkillStats.StartingHp(0)).IsEqual(3);
        AssertThat(SkillStats.StartingHp(1)).IsEqual(4);
        AssertThat(SkillStats.StartingHp(5)).IsEqual(8);

        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalStartingHp, 3);
        AssertThat(SkillStats.StartingHp(state)).IsEqual(6);
        AssertThat(SkillStats.StartingHp((SkillTreeState?)null)).IsEqual(3);
    }

    [TestCase]
    public void StartingCoins_AddsTwoPerWarChestRank()
    {
        AssertThat(SkillStats.StartingCoins(0)).IsEqual(10);
        AssertThat(SkillStats.StartingCoins(1)).IsEqual(12);
        AssertThat(SkillStats.StartingCoins(5)).IsEqual(20);

        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalStartingGold, 4);
        AssertThat(SkillStats.StartingCoins(state)).IsEqual(18);
        AssertThat(SkillStats.StartingCoins((SkillTreeState?)null)).IsEqual(10);
    }

    [TestCase]
    public void EffectiveTowerCost_AllBasesDiscountToSevenAtRankThree()
    {
        var state = new SkillTreeState();

        // Rank 0: every base is 10.
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
            AssertThat(SkillStats.EffectiveTowerCost(type, state)).IsEqual(10);

        for (int rank = 1; rank <= 3; rank++)
        {
            state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, rank);
            int expected = 10 - rank;
            foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
            {
                AssertThat(SkillStats.EffectiveTowerCost(type, state)).IsEqual(expected);
            }
        }

        // Null state (and a null-safe call) keeps the base price.
        AssertThat(SkillStats.EffectiveTowerCost(TowerType.Arrow, null)).IsEqual(10);
    }

    [TestCase]
    public void EffectiveUpgradeCost_RankThreeDiscountYieldsTwoSevenTwelve()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 3);

        AssertThat(SkillStats.EffectiveUpgradeCost(1, state)).IsEqual(2);  // 5 - 3
        AssertThat(SkillStats.EffectiveUpgradeCost(2, state)).IsEqual(7);  // 10 - 3
        AssertThat(SkillStats.EffectiveUpgradeCost(3, state)).IsEqual(12); // 15 - 3
    }

    [TestCase]
    public void EffectiveUpgradeCost_BaseCostsWithoutRanks()
    {
        var state = new SkillTreeState();

        AssertThat(SkillStats.EffectiveUpgradeCost(1, state)).IsEqual(5);
        AssertThat(SkillStats.EffectiveUpgradeCost(2, state)).IsEqual(10);
        AssertThat(SkillStats.EffectiveUpgradeCost(3, state)).IsEqual(15);
    }

    [TestCase]
    public void EffectiveCosts_NeverDropBelowOne()
    {
        // The clamp is defensive only: even a tampered discount amount far above any
        // reachable rank's value can never produce a free tower or free upgrade.
        // SetRank itself clamps ranks, so the floor is exercised through the pure
        // discount helper directly.
        AssertThat(SkillStats.ApplyDiscount(10, 99)).IsEqual(1);
        AssertThat(SkillStats.ApplyDiscount(5, 99)).IsEqual(1);
        AssertThat(SkillStats.ApplyDiscount(5, 3)).IsEqual(2);
        AssertThat(SkillStats.ApplyDiscount(0, 99)).IsEqual(0);

        // Normal play never reaches the floor: rank 3 is the highest possible and the
        // cheapest base cost is 5, so the lowest observable price is 2.
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, 99);
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 99);
        AssertThat(state.GetRank(SkillTreeCatalog.GlobalUpgradeDiscount)).IsEqual(3);
        AssertThat(SkillStats.EffectiveUpgradeCost(1, state)).IsEqual(2);
    }

    [TestCase]
    public void EffectiveUpgradeCost_InvalidRanksStayZero()
    {
        // Out-of-range target ranks have no base cost; the floor must not turn them into 1.
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 3);

        AssertThat(SkillStats.EffectiveUpgradeCost(0, state)).IsEqual(0);
        AssertThat(SkillStats.EffectiveUpgradeCost(4, state)).IsEqual(0);
    }
}
