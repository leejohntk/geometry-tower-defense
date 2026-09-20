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
    public void Level4Path_BlocksPlacementOnBothRoutesAndSharedConvergeCells()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level4);

        // Each route's spawn cell is a path cell.
        AssertThat(grid.IsOnPath(3, 0)).IsTrue();   // route A spawn
        AssertThat(grid.IsOnPath(11, 0)).IsTrue();  // route B spawn

        // Shared converge cells block placement once (deduped path set).
        AssertThat(grid.IsOnPath(7, 5)).IsTrue();
        AssertThat(grid.CanPlaceTower(7, 5)).IsFalse();
        AssertThat(grid.IsOnPath(7, 17)).IsTrue();
        AssertThat(grid.CanPlaceTower(7, 17)).IsFalse();

        // An off-path, in-bounds cell remains placeable.
        AssertThat(grid.CanPlaceTower(0, 0)).IsTrue();
    }

    [TestCase]
    public void GetPathWaypoints_Level4_ReturnsPerRouteWaypoints()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level4);

        var routeA = grid.GetPathWaypoints(0);
        var routeB = grid.GetPathWaypoints(1);

        AssertThat(routeA.Count).IsEqual(Levels.Level4.Paths[0].Count + 1);
        AssertThat(routeB.Count).IsEqual(Levels.Level4.Paths[1].Count + 1);

        AssertThat(routeA[0]).IsEqual(new Vector2(GameConstants.CellCenterX(0), GameConstants.CellCenterY(3)));
        AssertThat(routeB[0]).IsEqual(new Vector2(GameConstants.CellCenterX(0), GameConstants.CellCenterY(11)));

        // An out-of-range route index returns an empty list rather than throwing.
        AssertThat(grid.GetPathWaypoints(2).Count).IsEqual(0);
    }

    [TestCase]
    public void GetPathWaypoints_Level2_StartsWithSpawnAndEndsPastBase()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level2);

        var waypoints = grid.GetPathWaypoints(0);

        AssertThat(waypoints.Count).IsEqual(Levels.Level2.Paths[0].Count + 1);
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
