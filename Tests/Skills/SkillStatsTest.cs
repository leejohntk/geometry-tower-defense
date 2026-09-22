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
}
