using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Upgrade purchase transactions on <see cref="GameManager"/>: coin deduction happens
/// exactly once per success, the hidden XP gate blocks early purchases, rank 2 has no
/// generic third step, the archetype choice is the only rank-3 path, and rejected
/// purchases leave state untouched.
/// </summary>
[TestSuite]
public class TowerUpgradePurchaseTest
{
    private static GameManager CreateManager(int extraCoins)
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);
        if (extraCoins > 0)
            manager.AddCoinsForTesting(extraCoins);
        return manager;
    }

    private static Tower PlaceArrow(GameManager manager, int row = 0, int col = 0)
    {
        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.PlaceTower(row, col)).IsTrue();
        return manager.GetActiveTowers()[0];
    }

    private static void RaiseToRank2(GameManager manager, Tower tower)
    {
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(tower.Rank).IsEqual(2);
    }

    [TestCase]
    public void PlacedTower_StartsRankZero_NoXpNoArchetype()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);

        AssertThat(tower.Rank).IsEqual(0);
        AssertThat(tower.Xp).IsEqual(0f);
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.None);
    }

    [TestCase]
    public void Upgrade_BelowXpThreshold_IsRejectedWithoutStateChange()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(4.9f);

        int coinsBefore = manager.Coins;

        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(tower.Rank).IsEqual(0);
        AssertThat(manager.Coins).IsEqual(coinsBefore);
    }

    [TestCase]
    public void Upgrade_WithoutCoins_IsRejectedWithoutStateChange()
    {
        var manager = CreateManager(0); // starting 10 coins, arrow costs 10
        var tower = PlaceArrow(manager);
        AssertThat(manager.Coins).IsEqual(0);
        tower.AddXp(5f);

        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(tower.Rank).IsEqual(0);
        AssertThat(manager.Coins).IsEqual(0);
    }

    [TestCase]
    public void Rank1Purchase_DeductsExactlyFive_AndOnlyOnce()
    {
        var manager = CreateManager(20);
        var tower = PlaceArrow(manager); // 20 coins left
        tower.AddXp(5f);

        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(tower.Rank).IsEqual(1);
        AssertThat(manager.Coins).IsEqual(15);

        // Second click: the next gate is 15 XP, so no state change.
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(tower.Rank).IsEqual(1);
        AssertThat(manager.Coins).IsEqual(15);
    }

    [TestCase]
    public void Rank2Purchase_DeductsTen()
    {
        var manager = CreateManager(40);
        var tower = PlaceArrow(manager); // 40 coins left
        tower.AddXp(15f);

        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();  // rank 1, -5
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();  // rank 2, -10

        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(manager.Coins).IsEqual(25);
    }

    [TestCase]
    public void NoGenericThirdRank_EvenWithXpAndCoins()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(100f);
        RaiseToRank2(manager, tower); // 100 - 5 - 10 = 85

        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(manager.Coins).IsEqual(85);
    }

    [TestCase]
    public void ArchetypePurchase_DeductsFifteen_SetsRank3_AndIsFinal()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(100f);
        RaiseToRank2(manager, tower); // 85 coins

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();
        AssertThat(tower.Rank).IsEqual(3);
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.Pierce);
        AssertThat(manager.Coins).IsEqual(70);

        // No respec: a second card, even a valid one, is a no-op.
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Crit)).IsFalse();
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.Pierce);
        AssertThat(manager.Coins).IsEqual(70);

        // And no further generic upgrades.
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(manager.Coins).IsEqual(70);
    }

    [TestCase]
    public void ArchetypePurchase_RejectedWhenXpShort()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(29.9f);
        RaiseToRank2(manager, tower); // 85 coins
        int coinsBefore = manager.Coins;

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsFalse();
        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.None);
        AssertThat(manager.Coins).IsEqual(coinsBefore);
    }

    [TestCase]
    public void ArchetypePurchase_CoinsShort_IsRejectedThenSucceedsOnceCoinsArrive()
    {
        // 29 extra: after the tower and both ranks, exactly 14 coins remain (one short
        // of the 15-coin rank-3 cost).
        var manager = CreateManager(29);
        var tower = PlaceArrow(manager);
        tower.AddXp(30f);
        RaiseToRank2(manager, tower);
        AssertThat(manager.Coins).IsEqual(14);

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsFalse();
        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(manager.Coins).IsEqual(14);

        // Coins arriving later must make the same already-visible option buyable.
        manager.AddCoinsForTesting(1);
        AssertThat(manager.Coins).IsEqual(15);
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();
        AssertThat(tower.Rank).IsEqual(3);
        AssertThat(manager.Coins).IsEqual(0);
    }

    [TestCase]
    public void ArchetypePurchase_RejectedForWrongTowerType()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(30f);
        RaiseToRank2(manager, tower);

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Cluster)).IsFalse();
        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(tower.Archetype).IsEqual(TowerArchetype.None);

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();
    }

    [TestCase]
    public void Upgrades_RejectedOutsidePlayingState()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(100f);
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        int coinsBefore = manager.Coins;

        manager.SetStateForTesting(GameState.GameOver);
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsFalse();
        AssertThat(manager.Coins).IsEqual(coinsBefore);

        manager.SetStateForTesting(GameState.Victory);
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsFalse();
        AssertThat(manager.Coins).IsEqual(coinsBefore);
        AssertThat(tower.Rank).IsEqual(2);
    }

    [TestCase]
    public void Upgrades_RejectedForTowerNotInTheActiveGame()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);

        var foreign = new ArrowTower();
        foreign.AddXp(100f);
        foreign.Initialize(5, 5);

        AssertThat(manager.IsTowerActive(foreign)).IsFalse();
        AssertThat(manager.TryUpgradeTower(foreign)).IsFalse();
        AssertThat(manager.TryChooseArchetype(foreign, TowerArchetype.Pierce)).IsFalse();
        AssertThat(manager.Coins).IsEqual(100);
        AssertThat(foreign.Rank).IsEqual(0);

        AssertThat(manager.IsTowerActive(tower)).IsTrue();
    }

    [TestCase]
    public void RepeatedClicks_ChargeOnceWhenOnlyOneRankIsGated()
    {
        var manager = CreateManager(20);
        var tower = PlaceArrow(manager);
        tower.AddXp(5f);

        // A double click on the same upgrade action: the second call fails the 15-XP gate.
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();

        AssertThat(tower.Rank).IsEqual(1);
        AssertThat(manager.Coins).IsEqual(15);
    }

    [TestCase]
    public void BothArchetypeCardsInOneFrame_ChargeOnce()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(30f);
        RaiseToRank2(manager, tower); // 85 coins

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Crit)).IsFalse();

        AssertThat(tower.Rank).IsEqual(3);
        AssertThat(manager.Coins).IsEqual(70);
    }

    [TestCase]
    public void LevelReset_DiscardsTowersAndTheirUpgradeState()
    {
        var manager = CreateManager(100);
        var tower = PlaceArrow(manager);
        tower.AddXp(100f);
        RaiseToRank2(manager, tower);
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();

        manager.ResetGame();

        AssertThat(manager.GetActiveTowers().Count).IsEqual(0);
        AssertThat(manager.IsTowerActive(tower)).IsFalse();
        AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Crit)).IsFalse();
    }
}
