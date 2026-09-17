using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for enemy stats and damage behavior across kinds.
/// </summary>
[TestSuite]
public class EnemyTest
{
    [TestCase]
    public void BasicEnemy_HasDefaultStats()
    {
        var enemy = new Enemy();
        enemy.ResetForPool(); // basic default: HP 10

        enemy.TakeDamage(10);
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void BasicEnemy_Diameter48_CoinDrop1()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        AssertThat(enemy.Diameter).IsEqual(48f);
        AssertThat(enemy.CollisionRadius).IsEqual(24f);
        AssertThat(enemy.CoinDrop).IsEqual(1);
        AssertThat(enemy.Kind).IsEqual(EnemyKind.Basic);
    }

    [TestCase]
    public void SwarmEnemy_HasThreeHP_Diameter24()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);

        AssertThat(enemy.Diameter).IsEqual(24f);
        AssertThat(enemy.CollisionRadius).IsEqual(12f);
        AssertThat(enemy.CoinDrop).IsEqual(1);
        AssertThat(enemy.Kind).IsEqual(EnemyKind.Swarm);

        // 3 HP: 2 damage leaves it alive, 1 more kills it.
        enemy.TakeDamage(2);
        AssertThat(enemy.IsDead).IsFalse();
        enemy.TakeDamage(1);
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void TakeDamage_OnDeadEnemy_IsNoOp()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.TakeDamage(3); // dead

        AssertThat(enemy.IsDead).IsTrue();
        enemy.TakeDamage(100); // should not re-kill or throw
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void BasicEnemy_ZeroOffset_LandsOnBareWaypoint()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });

        AssertThat(enemy.FormationOffset).IsEqual(Vector2.Zero);
        AssertThat(enemy.Position).IsEqual(new Vector2(32, 32));
    }

    [TestCase]
    public void SwarmFormation_OffsetPersistsAcrossWaypointArrivals()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        var offset = new Vector2(20, 0);
        enemy.SetFormationOffset(offset);

        var waypoints = new List<Vector2>
        {
            new Vector2(32, 32),
            new Vector2(96, 32),
            new Vector2(160, 32)
        };
        enemy.SetPath(waypoints);

        // Initial placement already includes the formation offset.
        AssertThat(enemy.Position).IsEqual(waypoints[0] + offset);

        int ticks = 0;
        while (!enemy.IsDead && ticks < 500)
        {
            enemy._Process(0.05);

            // No tick may land the member exactly on a bare waypoint.
            foreach (var waypoint in waypoints)
                AssertThat(enemy.Position).IsNotEqual(waypoint);

            ticks++;
        }

        AssertThat(enemy.IsDead).IsTrue();
        // The offset survived the final waypoint arrival snap.
        AssertThat(enemy.Position).IsEqual(waypoints[^1] + offset);
    }

    [TestCase]
    public void ResetForPool_ClearsFormationOffset()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.SetFormationOffset(new Vector2(20, 0));
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });

        AssertThat(enemy.Position).IsEqual(new Vector2(52, 32));

        enemy.ResetForPool();

        AssertThat(enemy.FormationOffset).IsEqual(Vector2.Zero);

        // A pooled reuse as a basic enemy must land exactly on the bare waypoint.
        enemy.Configure(EnemyKind.Basic);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });
        AssertThat(enemy.Position).IsEqual(new Vector2(32, 32));
    }
}
