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
    public void BuyRank_MechanicNode_BuyableWhenUnlockedAndAffordable()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost * 4);

        // Unlock the arrow seed path by buying all three arrow trunks to rank 1.
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowAttackSpeed)).IsTrue();
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowRange)).IsTrue();

        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowPierce)).IsTrue();
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowPierce)).IsTrue();
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowPierce)).IsEqual(1);
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
        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowPierce)).IsFalse();   // locked behind ArrowRange
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
    public void SetRank_RejectsUnknownNodes_AndRoundsTripMechanicNodes()
    {
        var state = new SkillTreeState();

        state.SetRank("does.not.exist", 2);
        AssertThat(state.GetRank("does.not.exist")).IsEqual(0);

        // Mechanic nodes are enabled in Part 2 and round-trip like stat nodes.
        state.SetRank(SkillTreeCatalog.ArrowPierce, 3);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowPierce)).IsEqual(3);

        // Enabled stat nodes still round-trip normally.
        state.SetRank(SkillTreeCatalog.ArrowDamage, 4);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(4);
    }

    [TestCase]
    public void IsUnlocked_FirstTrunkNodes_AreUnlockedFromFreshState()
    {
        var state = new SkillTreeState();

        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.CannonPowderCharge)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.LaserDps)).IsTrue();
    }

    [TestCase]
    public void CanBuyRank_FirstTrunkNodes_AreBuyableWhenAffordable()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost);

        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.CanBuyRank(SkillTreeCatalog.CannonPowderCharge)).IsTrue();
        AssertThat(state.CanBuyRank(SkillTreeCatalog.LaserDps)).IsTrue();
    }

    [TestCase]
    public void IsUnlocked_ChildTrunk_LockedUntilParentReachesUnlockRank()
    {
        var state = new SkillTreeState();

        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowAttackSpeed)).IsFalse();

        state.SetRank(SkillTreeCatalog.ArrowDamage, GameConstants.SkillUnlockRank);
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowAttackSpeed)).IsTrue();
    }

    [TestCase]
    public void BuyRank_LockedChild_ReturnsFalseAndChangesNothing()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(100);

        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowAttackSpeed)).IsFalse();
        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowAttackSpeed)).IsFalse();
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowAttackSpeed)).IsFalse();
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowAttackSpeed)).IsEqual(0);
        AssertThat(state.SkillPoints).IsEqual(100);
    }

    [TestCase]
    public void BuyRank_BuyingParentRankOne_UnlocksChild()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(GameConstants.SkillNodeRankCost * 2);

        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowAttackSpeed)).IsFalse();

        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowAttackSpeed)).IsTrue();
        AssertThat(state.CanBuyRank(SkillTreeCatalog.ArrowAttackSpeed)).IsTrue();
    }

    [TestCase]
    public void IsUnlocked_Seeds_UnlockWhenLastTrunkReachesUnlockRank()
    {
        var state = new SkillTreeState();

        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowPierce)).IsFalse();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowCritChance)).IsFalse();

        // Buying the first two trunks still leaves the last trunk (Range) at rank 0.
        state.SetSkillPoints(GameConstants.SkillNodeRankCost * 3);
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();
        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowAttackSpeed)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowPierce)).IsFalse();

        AssertThat(state.BuyRank(SkillTreeCatalog.ArrowRange)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowPierce)).IsTrue();
        AssertThat(state.IsUnlocked(SkillTreeCatalog.ArrowCritChance)).IsTrue();
    }
}
