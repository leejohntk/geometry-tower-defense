using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Buy/rank/cost rules for the persistent skill-tree state. Pure C# — no Godot runtime.
/// </summary>
[TestSuite]
public class SkillTreeStateTest
{
    [TestCase]
    public void NewState_HasZeroSkillPointsAndRanks()
    {
        var state = new SkillTreeState();

        AssertThat(state.SkillPoints).IsEqual(0);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(0);
    }

    [TestCase]
    public void AddSkillPoints_Accumulates()
    {
        var state = new SkillTreeState();

        state.AddSkillPoints(5);
        state.AddSkillPoints(8);

        AssertThat(state.SkillPoints).IsEqual(13);
    }

    [TestCase]
    public void AddSkillPoints_IgnoresNonPositiveAmounts()
    {
        var state = new SkillTreeState();
        state.AddSkillPoints(5);

        state.AddSkillPoints(0);
        state.AddSkillPoints(-3);

        AssertThat(state.SkillPoints).IsEqual(5);
    }

    [TestCase]
    public void BuyRank_DeductsCostAndIncrementsRank()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(30);

        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();

        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(1);
        AssertThat(state.SkillPoints).IsEqual(20);
    }

    [TestCase]
    public void BuyRank_CapsAtMaxRanks()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost * 10);

        // Buy five ranks successfully.
        for (int i = 0; i < GameConstants.SkillMaxRanks; i++)
            AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();

        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(GameConstants.SkillMaxRanks);

        // Sixth purchase is rejected at the cap; SP is unchanged.
        int spBefore = state.SkillPoints;
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsFalse();
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(GameConstants.SkillMaxRanks);
        AssertThat(state.SkillPoints).IsEqual(spBefore);
    }

    [TestCase]
    public void BuyRank_InsufficientSkillPoints_ReturnsFalse()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost - 1);

        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsFalse();
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(0);
        AssertThat(state.SkillPoints).IsEqual(GameConstants.SkillNodeRankCost - 1);
    }

    [TestCase]
    public void BuyRank_DisabledNode_ReturnsFalse()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(100);

        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowPierce)).IsFalse();
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowPierce)).IsEqual(0);
    }

    [TestCase]
    public void BuyRank_UnknownNode_ReturnsFalse()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(100);

        AssertThat(state.BuyRank("does.not.exist")).IsFalse();
        AssertThat(state.SkillPoints).IsEqual(100);
    }

    [TestCase]
    public void CanBuyRank_ReflectsTheSameRules()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost);

        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowPierce)).IsFalse();   // disabled
        AssertThat(state.CanBuyRank("does.not.exist")).IsFalse();              // unknown

        state.SetRank(SkillTreeCatalog.ArrowDamage, GameConstants.SkillMaxRanks);
        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowDamage)).IsFalse();  // maxed
    }

    [TestCase]
    public void SetRank_ClampsToMaxRanks()
    {
        var state = new SkillTreeState();

        state.SetRank(SkillTreeCatalog.ArrowDamage, 99);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(GameConstants.SkillMaxRanks);

        state.SetRank(SkillTreeCatalog.ArrowDamage, -4);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(0);
    }

    [TestCase]
    public void SetSkillPoints_ClampsNegativeToZero()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(-5);

        AssertThat(state.SkillPoints).IsEqual(0);
    }

    [TestCase]
    public void SkillPoints_SaturateAtIntMax_InsteadOfWrapping()
    {
        var atMax = new SkillTreeState();
        atMax.SetSkillPoints(int.MaxValue);
        atMax.AddSkillPoints(5);
        AssertThat(atMax.SkillPoints).IsEqual(int.MaxValue);

        var nearMax = new SkillTreeState();
        nearMax.SetSkillPoints(int.MaxValue - 2);
        nearMax.AddSkillPoints(5);
        AssertThat(nearMax.SkillPoints).IsEqual(int.MaxValue);
    }

    [TestCase]
    public void SetRank_RejectsUnknownAndDisabledNodes()
    {
        var state = new SkillTreeState();

        state.SetRank(SkillTreeCatalog.ArrowPierce, 3); // disabled mechanic node
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowPierce)).IsEqual(0);

        state.SetRank("does.not.exist", 2);
        AssertThat(state.GetRank("does.not.exist")).IsEqual(0);

        // Enabled stat nodes still round-trip normally.
        state.SetRank(SkillTreeCatalog.ArrowDamage, 4);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(4);
    }
}
