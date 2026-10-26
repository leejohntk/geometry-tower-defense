using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Regression tests for the tower placement session lifecycle: a placement selection is
/// consumed by one successful placement, while an invalid battlefield click leaves the
/// player aiming.
/// </summary>
[TestSuite]
public class GameManagerTest
{
    [TestCase]
    public void PlaceTower_SuccessfulPlacement_ClearsPlacementMode()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);

        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.IsPlacingTower).IsTrue();

        // (col 0, row 0) is off the Level 1 path and in bounds.
        AssertThat(manager.PlaceTower(0, 0)).IsTrue();

        // A successful placement consumes the selection unconditionally. The old
        // behavior kept placement mode alive whenever coins remained.
        AssertThat(manager.IsPlacingTower).IsFalse();
        AssertThat(manager.PlacingTowerType).IsNull();
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins - GameConstants.ArrowTowerCost);
    }

    [TestCase]
    public void PlaceTower_InvalidBattlefieldCell_KeepsPlacementMode()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);

        manager.PlacingTowerType = TowerType.Arrow;

        // Level 1's path occupies row 10; a click there is a failed battlefield
        // placement, not an exit.
        AssertThat(manager.PlaceTower(GameConstants.PathRow, 5)).IsFalse();

        AssertThat(manager.IsPlacingTower).IsTrue();
        AssertThat(manager.PlacingTowerType).IsEqual(TowerType.Arrow);
    }
}
