using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// WaveManager's boss-summon spawn entry: on-route progress placement, zero coin
/// drops, non-orbiting single-enemy slots, live-count accounting that keeps the final
/// wave open, and the forced middle-route boss spawn that leaves round-robin alone.
/// </summary>
[TestSuite]
public class WaveManagerSummonTest
{
    [TestCase]
    public void SpawnSummon_PlacesEnemyAtBossProgress_WithZeroCoins()
    {
        var grid = new GridManager();
        grid.Configure(Levels.Level5);
        var waypoints = grid.GetPathWaypoints(1); // middle route

        var manager = CreateManager(Levels.Level5);
        var anchor = waypoints[3];
        var offset = new Vector2(20f, 0f);

        var add = manager.SpawnSummon(EnemyKind.Armored, waypoints, 3, anchor, offset);

        AssertThat(add).IsNotNull();
        AssertThat(add!.Kind).IsEqual(EnemyKind.Armored);
        AssertThat(add.CurrentWaypointIndex).IsEqual(3);
        AssertThat(add.AnchorPosition).IsEqual(anchor);
        AssertThat(add.Position).IsEqual(anchor + offset);
        AssertThat(add.FormationOffset).IsEqual(offset);
        AssertThat(add.FormationAngularSpeed).IsEqual(0f);
        AssertThat(add.CoinDrop).IsEqual(0);
        AssertThat(manager.EnemiesAlive).IsEqual(1);
    }

    [TestCase]
    public void SummonedSwarm_IsOneStaticCircle_NotAnOrbitingCluster()
    {
        var level = CreateThreeRouteLevel(new WaveDefinition(SpawnKind.SwarmCluster));
        var manager = CreateManager(level);

        var waveSpawned = new List<Enemy>();
        manager.EnemySpawned += (enemy, _) => waveSpawned.Add(enemy);
        manager.StartNextWave();
        manager.StopSpawning();

        // A wave SwarmCluster is three orbiting members.
        AssertThat(waveSpawned.Count).IsEqual(GameConstants.SwarmClusterSize);
        foreach (var member in waveSpawned)
            AssertThat(member.FormationAngularSpeed).IsEqual(GameConstants.SwarmClusterRotationSpeed);

        var grid = new GridManager();
        grid.Configure(level);
        var waypoints = grid.GetPathWaypoints(0);

        // A summoned Swarm slot is exactly one small circle with a static ring offset.
        var add = manager.SpawnSummon(
            EnemyKind.Swarm,
            waypoints,
            2,
            waypoints[2],
            new Vector2(GameConstants.ProgenitorSummonOffsetRadius, 0f));

        AssertThat(add).IsNotNull();
        AssertThat(add!.Kind).IsEqual(EnemyKind.Swarm);
        AssertThat(add.Diameter).IsEqual((float)GameConstants.SwarmEnemyDiameter);
        AssertThat(add.FormationAngularSpeed).IsEqual(0f);
        AssertThat(add.CoinDrop).IsEqual(0);
        AssertThat(manager.EnemiesAlive).IsEqual(GameConstants.SwarmClusterSize + 1);
    }

    [TestCase]
    public void Summons_CountTowardLiveTotal_BlockWaveCompletionUntilTheyDie()
    {
        // One wave, one wave-defined spawn. The boss-summoned add must keep the wave —
        // and victory — from completing after the wave-defined enemy is gone.
        var level = CreateThreeRouteLevel(new WaveDefinition(SpawnKind.Basic));
        var manager = CreateManager(level);

        int waveCompleted = 0;
        int allWavesCompleted = 0;
        manager.WaveCompleted += _ => waveCompleted++;
        manager.AllWavesCompleted += () => allWavesCompleted++;

        AssertThat(manager.StartNextWave()).IsTrue();
        AssertThat(manager.EnemiesAlive).IsEqual(1);

        // Emulate the spawn timer finding an empty queue once the wave is fully spawned.
        manager.StopSpawning();

        var grid = new GridManager();
        grid.Configure(level);
        var waypoints = grid.GetPathWaypoints(0);
        var add = manager.SpawnSummon(EnemyKind.Basic, waypoints, 1, waypoints[1], Vector2.Zero);

        AssertThat(add).IsNotNull();
        AssertThat(manager.EnemiesAlive).IsEqual(2);

        // The wave-defined enemy dies: the summon is still alive, so nothing completes.
        manager.NotifyEnemyDestroyed();
        AssertThat(manager.EnemiesAlive).IsEqual(1);
        AssertThat(waveCompleted).IsEqual(0);
        AssertThat(allWavesCompleted).IsEqual(0);
        AssertThat(manager.IsWaveActive).IsTrue();

        // The last summon dies: now the final wave completes and victory can fire.
        manager.NotifyEnemyDestroyed();
        AssertThat(manager.EnemiesAlive).IsEqual(0);
        AssertThat(waveCompleted).IsEqual(1);
        AssertThat(allWavesCompleted).IsEqual(1);
    }

    [TestCase]
    public void ALeakedSummon_AlsoCountsDown_TheLiveTotal()
    {
        var level = CreateThreeRouteLevel(new WaveDefinition(SpawnKind.Basic));
        var manager = CreateManager(level);
        manager.StartNextWave();
        manager.StopSpawning();

        var grid = new GridManager();
        grid.Configure(level);
        var waypoints = grid.GetPathWaypoints(0);
        manager.SpawnSummon(EnemyKind.Basic, waypoints, 1, waypoints[1], Vector2.Zero);

        manager.NotifyEnemyDestroyed();
        AssertThat(manager.IsWaveActive).IsTrue();

        manager.NotifyEnemyReachedEnd();
        AssertThat(manager.EnemiesAlive).IsEqual(0);
    }

    [TestCase]
    public void BossRoute_IsMiddleRoute_ForAnyRouteCount()
    {
        AssertThat(WaveManager.BossRouteIndex(3)).IsEqual(1);
        AssertThat(WaveManager.BossRouteIndex(2)).IsEqual(1);
        AssertThat(WaveManager.BossRouteIndex(1)).IsEqual(0);
        AssertThat(WaveManager.BossRouteIndex(0)).IsEqual(0);
    }

    [TestCase]
    public void BossSpawn_EntersMiddleRoute_WithoutAdvancingRoundRobin()
    {
        var level = CreateThreeRouteLevel(
            new WaveDefinition(SpawnKind.Progenitor),
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic));
        var manager = CreateManager(level);

        int bossRoute = -1;
        EnemyKind bossKind = EnemyKind.Basic;
        manager.EnemySpawned += (enemy, route) =>
        {
            bossRoute = route;
            bossKind = enemy.Kind;
        };

        AssertThat(manager.StartNextWave()).IsTrue();
        AssertThat(bossKind).IsEqual(EnemyKind.Progenitor);
        AssertThat(bossRoute).IsEqual(1);

        // Finish wave 1 (the boss is its only event), then start wave 2. If the boss
        // had consumed a round-robin step, wave 2's first event would land on route 1.
        manager.StopSpawning();
        manager.NotifyEnemyDestroyed();

        int nextRoute = -1;
        manager.EnemySpawned += (_, route) => nextRoute = route;
        AssertThat(manager.StartNextWave()).IsTrue();
        AssertThat(nextRoute).IsEqual(0);
    }

    private static WaveManager CreateManager(LevelDefinition level)
    {
        var root = new Node2D();
        var pool = new ObjectPool<Enemy>(8, root);

        var manager = new WaveManager();
        manager.SetEnemyPool(pool);
        manager.SetLevel(level);
        return manager;
    }

    /// <summary>
    /// Minimal three-route level used to exercise round-robin and boss routing without
    /// depending on Level 5's exact geometry.
    /// </summary>
    private static LevelDefinition CreateThreeRouteLevel(params WaveDefinition[] waves)
    {
        var paths = new IReadOnlyList<Vector2I>[]
        {
            LevelDefinition.BuildPath(new[]
            {
                new Vector2I(0, 2), new Vector2I(19, 2), new Vector2I(19, 7)
            }),
            LevelDefinition.BuildPath(new[]
            {
                new Vector2I(0, 7), new Vector2I(19, 7)
            }),
            LevelDefinition.BuildPath(new[]
            {
                new Vector2I(0, 11), new Vector2I(19, 11), new Vector2I(19, 7)
            })
        };

        return new LevelDefinition(99, "Test Level", paths, allowCannonTower: true, allowLaserTower: true, waves);
    }
}
