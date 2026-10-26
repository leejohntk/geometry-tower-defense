using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Regression tests for the tower placement session lifecycle: a placement selection is
/// consumed by one successful placement, while an invalid battlefield click leaves the
/// player aiming. Successful placements deliberately run with coins left over, which is
/// the condition the original bug needed (placement only ended when coins ran out).
/// </summary>
[TestSuite]
public class GameManagerTest
{
    // Enough for three Arrow towers. A placement test that ends at 0 coins cannot
    // reproduce the reported bug, where placement survived while coins remained.
    private const int PlacementTestCoins = 30;

    private static GameManager CreateManagerWithCoins()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);
        manager.AddCoinsForTesting(PlacementTestCoins - GameConstants.StartingCoins);
        return manager;
    }

    [TestCase]
    public void PlaceTower_SuccessfulPlacement_ClearsPlacementModeWithCoinsRemaining()
    {
        var manager = CreateManagerWithCoins();

        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.IsPlacingTower).IsTrue();

        // (row 0, col 0) is off the Level 1 path (row 10) and in bounds.
        AssertThat(manager.PlaceTower(0, 0)).IsTrue();

        // A successful placement consumes the selection even though the player can
        // still afford another tower. The old behavior kept placement mode alive
        // whenever coins remained.
        AssertThat(manager.Coins).IsEqual(PlacementTestCoins - GameConstants.ArrowTowerCost);
        AssertThat(manager.Coins > 0).IsTrue();
        AssertThat(manager.IsPlacingTower).IsFalse();
        AssertThat(manager.PlacingTowerType).IsNull();
    }

    [TestCase]
    public void PlaceTower_InvalidBattlefieldCell_KeepsPlacementMode()
    {
        var manager = CreateManagerWithCoins();

        manager.PlacingTowerType = TowerType.Arrow;

        // Level 1's path occupies row 10; a click there is a failed battlefield
        // placement, not an exit.
        AssertThat(manager.PlaceTower(GameConstants.PathRow, 5)).IsFalse();

        AssertThat(manager.IsPlacingTower).IsTrue();
        AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
        AssertThat(manager.GetActiveTowers().Count).IsEqual(0);
    }

    [TestCase]
    public void PlaceTower_OccupiedCellAfterSuccess_ReturnsFalseAndKeepsPlacementMode()
    {
        var manager = CreateManagerWithCoins();

        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.PlaceTower(0, 0)).IsTrue();

        // Re-select and retry the same cell: it is now occupied, so the attempt fails
        // and the new selection survives for a retry elsewhere.
        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.PlaceTower(0, 0)).IsFalse();

        AssertThat(manager.IsPlacingTower).IsTrue();
        AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
        AssertThat(manager.GetActiveTowers().Count).IsEqual(1);
        AssertThat(manager.Coins).IsEqual(PlacementTestCoins - GameConstants.ArrowTowerCost);
    }

    [TestCase]
    public void PlaceTower_FreeCellWithZeroCoins_ReturnsFalseAndKeepsPlacementMode()
    {
        var manager = CreateManagerWithCoins();

        // Spend down to exactly 0 coins with successive one-per-selection placements.
        int affordableTowers = PlacementTestCoins / GameConstants.ArrowTowerCost;
        for (int col = 0; col < affordableTowers; col++)
        {
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, col)).IsTrue();
        }
        AssertThat(manager.Coins).IsEqual(0);

        // A free, in-bounds cell with no coins must fail without consuming the
        // selection, so the player can still retry after earning coins.
        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.PlaceTower(0, affordableTowers)).IsFalse();

        AssertThat(manager.IsPlacingTower).IsTrue();
        AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
        AssertThat(manager.GetActiveTowers().Count).IsEqual(affordableTowers);
    }
}
