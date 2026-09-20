using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure logic tests for level definitions (no Godot node instantiation required).
/// </summary>
[TestSuite]
public class LevelDefinitionTest
{
    [TestCase]
    public void Level1_HasStraightPathOnRow10()
    {
        var path = Levels.Level1.Paths[0];

        AssertThat(path.Count).IsEqual(GameConstants.GridCols);
        AssertThat(Levels.Level1.SpawnCells[0]).IsEqual(new Vector2I(0, GameConstants.PathRow));
        AssertThat(Levels.Level1.BaseCell).IsEqual(new Vector2I(GameConstants.GridCols - 1, GameConstants.PathRow));

        foreach (var cell in path)
            AssertThat(cell.Y).IsEqual(GameConstants.PathRow);
    }

    [TestCase]
    public void Level1_IsArrowOnly_WithBasicWaves()
    {
        AssertThat(Levels.Level1.AllowCannonTower).IsFalse();
        AssertThat(Levels.Level1.Waves.Count).IsEqual(5);

        int[] expected = { 3, 5, 7, 9, 12 };
        for (int i = 0; i < expected.Length; i++)
            AssertThat(Levels.Level1.Waves[i].TotalEnemies).IsEqual(expected[i]);
    }

    [TestCase]
    public void Level2_PathIsValid_AndWindsAtLeastThreeTimes()
    {
        var path = Levels.Level2.Paths[0];

        AssertThat(path.Count > 0).IsTrue();
        AssertThat(Levels.Level2.SpawnCells[0]).IsEqual(new Vector2I(0, 7));
        AssertThat(Levels.Level2.BaseCell).IsEqual(new Vector2I(19, 7));

        AssertThat(LevelDefinition.IsPathConnectedAndInBounds(path, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsFalse();
        AssertThat(LevelDefinition.VerticalDirectionChangeCount(path) >= 3).IsTrue();
    }

    [TestCase]
    public void Level2_AllowsCannon_WithSwarmClustersFromWave3()
    {
        AssertThat(Levels.Level2.AllowCannonTower).IsTrue();
        AssertThat(Levels.Level2.Waves.Count).IsEqual(5);

        // Waves 1-2 are basic-only; waves 3+ mix in swarm clusters.
        AssertThat(Levels.Level2.Waves[0].TotalEnemies).IsEqual(4);
        AssertThat(Levels.Level2.Waves[1].TotalEnemies).IsEqual(6);
        AssertThat(Levels.Level2.Waves[2].TotalEnemies).IsEqual(7);
        AssertThat(Levels.Level2.Waves[3].TotalEnemies).IsEqual(11);
        AssertThat(Levels.Level2.Waves[4].TotalEnemies).IsEqual(15);
    }

    [TestCase]
    public void Level1And2_HaveNoArmoredSpawns_AndNoLaserTower()
    {
        AssertThat(Levels.Level1.AllowLaserTower).IsFalse();
        AssertThat(Levels.Level2.AllowLaserTower).IsFalse();

        AssertThat(HasArmoredSpawn(Levels.Level1)).IsFalse();
        AssertThat(HasArmoredSpawn(Levels.Level2)).IsFalse();
    }

    [TestCase]
    public void Level3_HasArmoredSpawns_AndAllowsLaserTower()
    {
        AssertThat(Levels.Level3.AllowLaserTower).IsTrue();
        AssertThat(Levels.Level3.AllowCannonTower).IsTrue();
        AssertThat(HasArmoredSpawn(Levels.Level3)).IsTrue();
    }

    [TestCase]
    public void Level3_PathIsValid_AndEndsAtBase()
    {
        var path = Levels.Level3.Paths[0];

        AssertThat(path.Count > 0).IsTrue();
        AssertThat(Levels.Level3.SpawnCells[0]).IsEqual(new Vector2I(0, 5));
        AssertThat(Levels.Level3.BaseCell).IsEqual(new Vector2I(19, 5));

        AssertThat(LevelDefinition.IsPathConnectedAndInBounds(path, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsFalse();
    }

    [TestCase]
    public void Level3_WaveComposition_MatchesSpec()
    {
        AssertThat(Levels.Level3.Waves.Count).IsEqual(5);

        // 4 basic
        AssertThat(Levels.Level3.Waves[0].TotalEnemies).IsEqual(4);
        // 6 basic
        AssertThat(Levels.Level3.Waves[1].TotalEnemies).IsEqual(6);
        // 4 basic + 1 swarm cluster (4 + 3)
        AssertThat(Levels.Level3.Waves[2].TotalEnemies).IsEqual(7);
        // 3 basic + 2 armored
        AssertThat(Levels.Level3.Waves[3].TotalEnemies).IsEqual(5);
        // 2 basic + 1 swarm cluster + 2 armored (2 + 3 + 2)
        AssertThat(Levels.Level3.Waves[4].TotalEnemies).IsEqual(7);
    }

    [TestCase]
    public void Levels1To3_EachHaveExactlyOneRoute_AndUnchangedSpawnBase()
    {
        AssertSingleRoute(Levels.Level1, new Vector2I(0, GameConstants.PathRow), new Vector2I(GameConstants.GridCols - 1, GameConstants.PathRow));
        AssertSingleRoute(Levels.Level2, new Vector2I(0, 7), new Vector2I(19, 7));
        AssertSingleRoute(Levels.Level3, new Vector2I(0, 5), new Vector2I(19, 5));
    }

    [TestCase]
    public void Get_ResolvesLevel4()
    {
        AssertThat(Levels.Get(4)).IsEqual(Levels.Level4);
        AssertThat(Levels.Get(4).Id).IsEqual(4);
    }

    [TestCase]
    public void Level4_HasTwoRoutes_BothStartingLeft_AndSharingBase()
    {
        var paths = Levels.Level4.Paths;

        AssertThat(paths.Count).IsEqual(2);
        AssertThat(paths[0][0].X).IsEqual(0);
        AssertThat(paths[1][0].X).IsEqual(0);
        AssertThat(paths[0][^1]).IsEqual(paths[1][^1]);

        AssertThat(Levels.Level4.SpawnCells.Count).IsEqual(2);
        AssertThat(Levels.Level4.SpawnCells[0]).IsEqual(new Vector2I(0, 3));
        AssertThat(Levels.Level4.SpawnCells[1]).IsEqual(new Vector2I(0, 11));
        AssertThat(Levels.Level4.BaseCell).IsEqual(new Vector2I(19, 7));
    }

    [TestCase]
    public void Level4_EachRouteIsValid_AndDoesNotSelfIntersect()
    {
        foreach (var route in Levels.Level4.Paths)
        {
            AssertThat(LevelDefinition.IsPathConnectedAndInBounds(route, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
            AssertThat(LevelDefinition.PathSelfIntersects(route)).IsFalse();
        }
    }

    [TestCase]
    public void Level4_RoutesShareConvergeCells_AndDivergeAtSplitCells()
    {
        var routeA = Levels.Level4.Paths[0];
        var routeB = Levels.Level4.Paths[1];

        var cellsA = new HashSet<Vector2I>(routeA);
        var cellsB = new HashSet<Vector2I>(routeB);

        // The two routes are NOT identical — they diverge between converge cells.
        AssertThat(cellsA.SetEquals(cellsB)).IsFalse();

        // Shared converge cells (and the row-7 runs between them).
        foreach (var converge in new[] { new Vector2I(5, 7), new Vector2I(11, 7), new Vector2I(17, 7) })
        {
            AssertThat(cellsA.Contains(converge)).IsTrue();
            AssertThat(cellsB.Contains(converge)).IsTrue();
        }

        // Split cells are shared, but the cell immediately after each split goes
        // to a different row on each route.
        AssertThat(cellsA.Contains(new Vector2I(8, 7)) && cellsB.Contains(new Vector2I(8, 7))).IsTrue();
        AssertThat(cellsA.Contains(new Vector2I(8, 4))).IsTrue();   // route A splits up
        AssertThat(cellsB.Contains(new Vector2I(8, 10))).IsTrue();  // route B splits down

        AssertThat(cellsA.Contains(new Vector2I(14, 7)) && cellsB.Contains(new Vector2I(14, 7))).IsTrue();
        AssertThat(cellsA.Contains(new Vector2I(14, 10))).IsTrue(); // route A splits down
        AssertThat(cellsB.Contains(new Vector2I(14, 4))).IsTrue();  // route B splits up
    }

    [TestCase]
    public void Level4_AllowsAllTowerTypes()
    {
        AssertThat(Levels.Level4.AllowCannonTower).IsTrue();
        AssertThat(Levels.Level4.AllowLaserTower).IsTrue();
    }

    [TestCase]
    public void Level4_EveryWaveMixesAllThreeEnemyKinds()
    {
        AssertThat(Levels.Level4.Waves.Count).IsEqual(5);

        foreach (var wave in Levels.Level4.Waves)
        {
            AssertThat(HasSpawn(wave, SpawnKind.Basic)).IsTrue();
            AssertThat(HasSpawn(wave, SpawnKind.SwarmCluster)).IsTrue();
            AssertThat(HasSpawn(wave, SpawnKind.Armored)).IsTrue();
        }
    }

    private static void AssertSingleRoute(LevelDefinition level, Vector2I spawnCell, Vector2I baseCell)
    {
        AssertThat(level.Paths.Count).IsEqual(1);
        AssertThat(level.SpawnCells.Count).IsEqual(1);
        AssertThat(level.SpawnCells[0]).IsEqual(spawnCell);
        AssertThat(level.BaseCell).IsEqual(baseCell);
    }

    private static bool HasSpawn(WaveDefinition wave, SpawnKind kind)
    {
        foreach (var spawn in wave.Spawns)
            if (spawn == kind)
                return true;
        return false;
    }

    private static bool HasArmoredSpawn(LevelDefinition level)
    {
        foreach (var wave in level.Waves)
            foreach (var spawn in wave.Spawns)
                if (spawn == SpawnKind.Armored)
                    return true;
        return false;
    }

    [TestCase]
    public void BuildPath_ExpandsCornersWithoutDuplicates()
    {
        var corners = new[] { new Vector2I(0, 0), new Vector2I(2, 0), new Vector2I(2, 2) };
        var path = LevelDefinition.BuildPath(corners);

        AssertThat(path.Count).IsEqual(5); // (0,0),(1,0),(2,0),(2,1),(2,2)
        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsFalse();
        AssertThat(LevelDefinition.IsPathConnectedAndInBounds(path, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
    }

    [TestCase]
    public void BuildPath_DetectsSelfIntersection()
    {
        // Loop revisits (0,0): (0,0)->(2,0)->(2,2)->(0,2)->(0,0)
        var corners = new[] { new Vector2I(0, 0), new Vector2I(2, 0), new Vector2I(2, 2), new Vector2I(0, 2), new Vector2I(0, 0) };
        var path = LevelDefinition.BuildPath(corners);

        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsTrue();
    }
}
