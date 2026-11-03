using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Global-skill integration on <see cref="GameManager"/>: starting HP/coins are
/// recomputed from the persistent ranks on every Initialize, and every price surface
/// (placement, rank upgrades, archetype choice) uses the effective discounted cost for
/// both affordability and deduction.
/// </summary>
[TestSuite]
public class GameManagerGlobalSkillTest
{
    private static SkillTree AttachTree(GameManager manager, SkillTreeState state)
    {
        var tree = new SkillTree(state, _ => true);
        manager.SkillTree = tree;
        return tree;
    }

    private static GameManager CreateManager(SkillTreeState state, int extraCoins = 0)
    {
        var manager = new GameManager();
        AttachTree(manager, state);
        manager.Initialize(Levels.Level3);
        if (extraCoins > 0)
            manager.AddCoinsForTesting(extraCoins);
        return manager;
    }

    private static Tower Place(GameManager manager, TowerType type, int row, int col)
    {
        manager.PlacingTowerType = type;
        AssertThat(manager.PlaceTower(row, col)).IsTrue();
        return manager.GetActiveTowers()[^1];
    }

    [TestCase]
    public void Initialize_AppliesFortifiedCoreAndWarChestRanks()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalStartingHp, 2);
        state.SetRank(SkillTreeCatalog.GlobalStartingGold, 3);

        var manager = CreateManager(state);

        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP + 2);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins + 2 * 3);
    }

    [TestCase]
    public void Initialize_WithoutSkillTree_KeepsBaseStats()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level3);

        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);
    }

    [TestCase]
    public void Replay_RecomputesStartingStatsFromCurrentRanks()
    {
        var state = new SkillTreeState();
        var manager = CreateManager(state);
        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins);

        // Simulate buying global ranks between runs.
        manager.ResetGame();
        state.SetRank(SkillTreeCatalog.GlobalStartingHp, 4);
        state.SetRank(SkillTreeCatalog.GlobalStartingGold, 5);
        manager.Initialize(Levels.Level3);

        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP + 4);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins + 10);
    }

    [TestCase]
    public void PlaceTower_UsesDiscountedCostForEveryTowerType()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, 3);
        var manager = CreateManager(state, extraCoins: 100);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins + 100);

        int coins = manager.Coins;
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            AssertThat(manager.EffectiveTowerCost(type)).IsEqual(7);
        }

        Place(manager, TowerType.Arrow, 0, 0);
        AssertThat(manager.Coins).IsEqual(coins - 7);

        Place(manager, TowerType.Cannon, 0, 1);
        AssertThat(manager.Coins).IsEqual(coins - 14);

        Place(manager, TowerType.Laser, 0, 2);
        AssertThat(manager.Coins).IsEqual(coins - 21);
    }

    [TestCase]
    public void PlaceTower_BelowBasePriceButAtDiscountedPrice_IsAllowed()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, 3);
        var manager = CreateManager(state); // 10 coins

        // 10 -> 3 after one discounted placement.
        Place(manager, TowerType.Arrow, 0, 0);
        AssertThat(manager.Coins).IsEqual(3);
        manager.AddCoinsForTesting(4);
        AssertThat(manager.Coins).IsEqual(7);

        // 7 is below the 10 base price but covers the discounted 7; the deduction matches.
        Place(manager, TowerType.Cannon, 0, 1);
        AssertThat(manager.Coins).IsEqual(0);
        AssertThat(manager.GetActiveTowers().Count).IsEqual(2);
    }

    [TestCase]
    public void PlaceTower_OneCoinBelowTheDiscountedPrice_IsRejected()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalTowerDiscount, 3);
        var manager = CreateManager(state); // 10 coins

        Place(manager, TowerType.Arrow, 0, 0); // -> 3
        manager.AddCoinsForTesting(3);         // -> 6, one short of 7
        AssertThat(manager.Coins).IsEqual(6);

        manager.PlacingTowerType = TowerType.Cannon;
        AssertThat(manager.PlaceTower(0, 1)).IsFalse();
        AssertThat(manager.Coins).IsEqual(6);
        AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
        AssertThat(manager.IsPlacingTower).IsTrue();
    }

    [TestCase]
    public void Upgrades_DeductDiscountedTwoSevenTwelve()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 3);
        var manager = CreateManager(state, extraCoins: 100); // 110 coins

        var tower = Place(manager, TowerType.Arrow, 0, 0); // placement stays base 10 -> 100
        tower.AddXp(100f);

        AssertThat(manager.EffectiveUpgradeCost(1)).IsEqual(2);
        AssertThat(manager.EffectiveUpgradeCost(2)).IsEqual(7);
        AssertThat(manager.EffectiveUpgradeCost(3)).IsEqual(12);

        AssertThat(manager.TryUpgradeTower(tower)).IsTrue(); // rank 1, -2
        AssertThat(manager.Coins).IsEqual(98);

        AssertThat(manager.TryUpgradeTower(tower)).IsTrue(); // rank 2, -7
        AssertThat(manager.Coins).IsEqual(91);
        AssertThat(tower.Rank).IsEqual(2);

        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue(); // -12
        AssertThat(manager.Coins).IsEqual(79);
        AssertThat(tower.Rank).IsEqual(3);
    }

    [TestCase]
    public void Upgrade_BelowBasePriceButAtDiscountedPrice_IsBuyable()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 3);
        var manager = CreateManager(state); // 10 coins

        var tower = Place(manager, TowerType.Arrow, 0, 0); // arrow costs 10 with no tower discount -> 0
        tower.AddXp(GameConstants.TowerXpThresholdRank1);
        manager.AddCoinsForTesting(3); // 3 coins, below the base 5 but above the discounted 2
        AssertThat(manager.Coins).IsEqual(3);

        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(tower.Rank).IsEqual(1);
        AssertThat(manager.Coins).IsEqual(1);
    }

    [TestCase]
    public void Archetype_BelowBasePriceButAtDiscountedPrice_IsBuyable()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.GlobalUpgradeDiscount, 3);
        var manager = CreateManager(state); // 10 coins

        var tower = Place(manager, TowerType.Arrow, 0, 0); // -> 0
        tower.AddXp(100f);
        manager.AddCoinsForTesting(2); // rank 1 (-2)
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        manager.AddCoinsForTesting(7); // rank 2 (-7)
        AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
        AssertThat(manager.Coins).IsEqual(0);

        // 12 coins is below the base 15 but exactly the discounted archetype price.
        manager.AddCoinsForTesting(12);
        AssertThat(manager.TryChooseArchetype(tower, TowerArchetype.Pierce)).IsTrue();
        AssertThat(manager.Coins).IsEqual(0);
        AssertThat(tower.Rank).IsEqual(3);
    }

    [TestCase]
    public void PlaceTower_WithoutGlobalRanks_KeepsBaseCosts()
    {
        var manager = CreateManager(new SkillTreeState()); // 10 coins
        AssertThat(manager.EffectiveTowerCost(TowerType.Arrow)).IsEqual(10);

        Place(manager, TowerType.Arrow, 0, 0);
        AssertThat(manager.Coins).IsEqual(0);
    }
}
