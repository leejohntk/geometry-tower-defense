using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for per-level path occupancy and coordinate conversion (no tree required).
/// </summary>
[TestSuite]
public class GridManagerTest
{
    [TestCase]
    public void Level2Path_BlocksPlacementOnNonRow10PathCells()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level2);

        // (col 3, row 11) is on the winding vertical segment, off row 10.
        AssertThat(grid.IsOnPath(11, 3)).IsTrue();
        AssertThat(grid.CanPlaceTower(11, 3)).IsFalse();

        // A non-path, in-bounds cell is placeable.
        AssertThat(grid.CanPlaceTower(2, 2)).IsTrue();
    }

    [TestCase]
    public void Level1Path_BlocksPlacementOnRow10Only()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level1);

        AssertThat(grid.IsOnPath(10, 0)).IsTrue();
        AssertThat(grid.CanPlaceTower(10, 5)).IsFalse();
        AssertThat(grid.CanPlaceTower(9, 5)).IsTrue(); // row 9 is not on the path
    }

    [TestCase]
    public void GetPathWaypoints_Level2_StartsWithSpawnAndEndsPastBase()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level2);

        var waypoints = grid.GetPathWaypoints();

        AssertThat(waypoints.Count).IsEqual(Levels.Level2.PathCells.Count + 1);
        AssertThat(waypoints[0]).IsEqual(new Vector2(
            GameConstants.CellCenterX(0),
            GameConstants.CellCenterY(7)
        ));

        var last = waypoints[^1];
        AssertThat(last.X).IsEqual(GameConstants.CellCenterX(19) + GameConstants.CellSize);
        AssertThat(last.Y).IsEqual(GameConstants.CellCenterY(7));
    }

    [TestCase]
    public void PlaceTower_MarksCellOccupied()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level1);

        AssertThat(grid.PlaceTower(0, 0)).IsTrue();
        AssertThat(grid.CanPlaceTower(0, 0)).IsFalse();
        AssertThat(grid.PlaceTower(0, 0)).IsFalse();
    }

    [TestCase]
    public void PixelToGrid_ConvertsToCell()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level1);

        AssertThat(grid.PixelToGrid(new Vector2(0, 0))).IsEqual(new Vector2I(0, 0));
        AssertThat(grid.PixelToGrid(new Vector2(64, 64))).IsEqual(new Vector2I(1, 1));
        AssertThat(grid.PixelToGrid(new Vector2(1279, 895))).IsEqual(new Vector2I(19, 13));
    }
}
