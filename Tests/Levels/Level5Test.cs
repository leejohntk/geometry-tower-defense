using System.Collections.Generic;
using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure logic tests for Level 5's three-route layout and boss wave definition.
/// No Godot scene instantiation required.
/// </summary>
[TestSuite]
public class Level5Test
{
    private static readonly Vector2I BaseCell = new(19, 7);

    [TestCase]
    public void Get_ResolvesLevel5()
    {
        AssertThat(Levels.Get(5)).IsEqual(Levels.Level5);
        AssertThat(Levels.Get(5).Id).IsEqual(5);
        AssertThat(Levels.Level5.DisplayName).IsEqual("Level 5");
    }

    [TestCase]
    public void Level5_HasExactlyThreeRoutes_WithSpecSpawnCells_AndSharedBase()
    {
        var level = Levels.Level5;

        AssertThat(level.Paths.Count).IsEqual(3);
        AssertThat(level.SpawnCells.Count).IsEqual(3);
        AssertThat(level.SpawnCells[0]).IsEqual(new Vector2I(0, 2));
        AssertThat(level.SpawnCells[1]).IsEqual(new Vector2I(0, 7));
        AssertThat(level.SpawnCells[2]).IsEqual(new Vector2I(0, 11));
        AssertThat(level.BaseCell).IsEqual(BaseCell);
    }

    [TestCase]
    public void Level5_Routes_HaveSpecCellCounts_AndAreValid()
    {
        int[] expectedCounts = { 29, 20, 28 };

        for (int i = 0; i < Levels.Level5.Paths.Count; i++)
        {
            var route = Levels.Level5.Paths[i];
            AssertThat(route.Count).IsEqual(expectedCounts[i]);
            AssertThat(LevelDefinition.IsPathConnectedAndInBounds(
                route, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
            AssertThat(LevelDefinition.PathSelfIntersects(route)).IsFalse();
            AssertThat(route[^1]).IsEqual(BaseCell);
        }
    }

    [TestCase]
    public void Level5_RoutesShareOnlyTheFinalRun_GridDedupesSharedCells()
    {
        var routeA = Levels.Level5.Paths[0];
        var routeB = Levels.Level5.Paths[1];
        var routeC = Levels.Level5.Paths[2];

        // The middle lane is a straight row-7 run; the outer lanes join it at (14,7).
        var shared = new HashSet<Vector2I>
        {
            new(14, 7), new(15, 7), new(16, 7), new(17, 7), new(18, 7), new(19, 7)
        };

        foreach (var cell in shared)
        {
            AssertThat(routeA.Contains(cell)).IsTrue();
            AssertThat(routeB.Contains(cell)).IsTrue();
            AssertThat(routeC.Contains(cell)).IsTrue();
        }

        // Any other shared cell would mean the lanes overlap before the base.
        var union = new HashSet<Vector2I>();
        union.UnionWith(routeA);
        union.UnionWith(routeB);
        union.UnionWith(routeC);
        int repeatedCells = routeA.Count + routeB.Count + routeC.Count - union.Count;
        AssertThat(repeatedCells).IsEqual(shared.Count * 2); // each shared cell appears 3x
        AssertThat(union.Count).IsEqual(65);
    }

    [TestCase]
    public void Level5_GridBlocksEveryRoutesCells_IncludingSharedCells()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level5);

        // The grid dedupes shared cells into one path set, and no route cell anywhere
        // (shared or not) may accept a tower.
        foreach (var route in Levels.Level5.Paths)
        {
            foreach (var cell in route)
            {
                AssertThat(grid.IsOnPath(cell.Y, cell.X)).IsTrue();
                AssertThat(grid.CanPlaceTower(cell.Y, cell.X)).IsFalse();
            }
        }

        // Shared final-run cell: drawn once, still blocked.
        AssertThat(grid.IsOnPath(7, 16)).IsTrue();
        AssertThat(grid.CanPlaceTower(7, 16)).IsFalse();

        // Off-path cell stays buildable.
        AssertThat(grid.CanPlaceTower(0, 0)).IsTrue();
    }

    [TestCase]
    public void Level5_AllowsCannonAndLaser()
    {
        AssertThat(Levels.Level5.AllowCannonTower).IsTrue();
        AssertThat(Levels.Level5.AllowLaserTower).IsTrue();
    }

    [TestCase]
    public void Level5_WavesEscalate_12_15_18_21_24()
    {
        int[] expected = { 12, 15, 18, 21, 24 };

        AssertThat(Levels.Level5.Waves.Count).IsEqual(expected.Length);
        for (int i = 0; i < expected.Length; i++)
            AssertThat(Levels.Level5.Waves[i].TotalEnemies).IsEqual(expected[i]);
    }

    [TestCase]
    public void Level5_FinalWave_HasExactlyOneProgenitor_MidWave()
    {
        var finalWave = Levels.Level5.Waves[^1];

        int bossIndex = -1;
        int bossCount = 0;
        for (int i = 0; i < finalWave.Spawns.Count; i++)
        {
            if (finalWave.Spawns[i] == SpawnKind.Progenitor)
            {
                bossIndex = i;
                bossCount++;
            }
        }

        AssertThat(bossCount).IsEqual(1);
        AssertThat(finalWave.Spawns.Count).IsEqual(14);
        AssertThat(bossIndex).IsEqual(6); // 7th of 14 events

        int before = bossIndex;
        int after = finalWave.Spawns.Count - bossIndex - 1;
        AssertThat(before).IsEqual(6);
        AssertThat(after).IsEqual(7);
    }

    [TestCase]
    public void Level5_FinalWave_SpawnOrderMatchesSpec()
    {
        var spawns = Levels.Level5.Waves[^1].Spawns;

        var expected = new[]
        {
            SpawnKind.Basic, SpawnKind.Basic,
            SpawnKind.SwarmCluster, SpawnKind.SwarmCluster,
            SpawnKind.Armored, SpawnKind.Armored,
            SpawnKind.Progenitor,
            SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster,
            SpawnKind.Armored, SpawnKind.Armored,
            SpawnKind.Basic, SpawnKind.Basic
        };

        AssertThat(spawns.Count).IsEqual(expected.Length);
        for (int i = 0; i < expected.Length; i++)
            AssertThat(spawns[i]).IsEqual(expected[i]);
    }

    [TestCase]
    public void Level5_EarlierWaves_MatchSpecComposition()
    {
        // 4 Basic + 2 SwarmCluster + 2 Armored
        AssertThat(CountSpawns(Levels.Level5.Waves[0], SpawnKind.Basic)).IsEqual(4);
        AssertThat(CountSpawns(Levels.Level5.Waves[0], SpawnKind.SwarmCluster)).IsEqual(2);
        AssertThat(CountSpawns(Levels.Level5.Waves[0], SpawnKind.Armored)).IsEqual(2);

        // 4 Basic + 3 SwarmCluster + 2 Armored
        AssertThat(CountSpawns(Levels.Level5.Waves[1], SpawnKind.Basic)).IsEqual(4);
        AssertThat(CountSpawns(Levels.Level5.Waves[1], SpawnKind.SwarmCluster)).IsEqual(3);
        AssertThat(CountSpawns(Levels.Level5.Waves[1], SpawnKind.Armored)).IsEqual(2);

        // 3 Basic + 4 SwarmCluster + 3 Armored
        AssertThat(CountSpawns(Levels.Level5.Waves[2], SpawnKind.Basic)).IsEqual(3);
        AssertThat(CountSpawns(Levels.Level5.Waves[2], SpawnKind.SwarmCluster)).IsEqual(4);
        AssertThat(CountSpawns(Levels.Level5.Waves[2], SpawnKind.Armored)).IsEqual(3);

        // 2 Basic + 5 SwarmCluster + 4 Armored
        AssertThat(CountSpawns(Levels.Level5.Waves[3], SpawnKind.Basic)).IsEqual(2);
        AssertThat(CountSpawns(Levels.Level5.Waves[3], SpawnKind.SwarmCluster)).IsEqual(5);
        AssertThat(CountSpawns(Levels.Level5.Waves[3], SpawnKind.Armored)).IsEqual(4);
    }

    [TestCase]
    public void Level5_RoundRobin_CoversAllThreeRoutes_BossIsForcedMiddle()
    {
        var level = Levels.Level5;
        int counter = 0;
        int bossRoute = WaveManager.BossRouteIndex(level.Paths.Count);
        var usedRoutes = new HashSet<int>();
        int bossEvents = 0;

        foreach (var spawn in level.Waves[^1].Spawns)
        {
            if (spawn == SpawnKind.Progenitor)
            {
                bossEvents++;
                // Forced middle route, and deliberately does not advance the counter.
                AssertThat(bossRoute).IsEqual(1);
                usedRoutes.Add(bossRoute);
                continue;
            }

            usedRoutes.Add(WaveManager.NextRouteIndex(ref counter, level.Paths.Count));
        }

        AssertThat(bossEvents).IsEqual(1);
        AssertThat(counter).IsEqual(13); // 14 events minus the boss
        AssertThat(usedRoutes.Count).IsEqual(3);
        AssertThat(usedRoutes.Contains(0)).IsTrue();
        AssertThat(usedRoutes.Contains(1)).IsTrue();
        AssertThat(usedRoutes.Contains(2)).IsTrue();
    }

    [TestCase]
    public void Resolve_Progenitor_ProducesExactlyOneProgenitorEnemy()
    {
        var (kind, count) = WaveDefinition.Resolve(SpawnKind.Progenitor);
        AssertThat(kind).IsEqual(EnemyKind.Progenitor);
        AssertThat(count).IsEqual(1);
    }

    [TestCase]
    public void Levels1To4_AreUnchangedByLevel5()
    {
        AssertThat(Levels.Get(1).Id).IsEqual(1);
        AssertThat(Levels.Get(2).Id).IsEqual(2);
        AssertThat(Levels.Get(3).Id).IsEqual(3);
        AssertThat(Levels.Get(4).Id).IsEqual(4);

        AssertThat(Levels.Level1.Paths.Count).IsEqual(1);
        AssertThat(Levels.Level2.Paths.Count).IsEqual(1);
        AssertThat(Levels.Level3.Paths.Count).IsEqual(1);
        AssertThat(Levels.Level4.Paths.Count).IsEqual(2);

        AssertThat(Levels.Level4.Waves.Count).IsEqual(5);
        AssertThat(Levels.Level4.Waves[0].TotalEnemies).IsEqual(7);
        AssertThat(Levels.Level4.Waves[4].TotalEnemies).IsEqual(14);

        // No Progenitor sneaks into earlier levels.
        foreach (var level in new[] { Levels.Level1, Levels.Level2, Levels.Level3, Levels.Level4 })
            foreach (var wave in level.Waves)
                AssertThat(CountSpawns(wave, SpawnKind.Progenitor)).IsEqual(0);
    }

    private static int CountSpawns(WaveDefinition wave, SpawnKind kind)
    {
        int count = 0;
        foreach (var spawn in wave.Spawns)
            if (spawn == kind)
                count++;
        return count;
    }
}
