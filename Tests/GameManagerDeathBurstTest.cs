using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Death-burst pooling through GameManager: a kill spawns exactly one burst at the death
/// position with the enemy's fill color and kind scaling, reaching the base spawns none,
/// the effect releases itself on finish, and a level reset clears anything in flight.
/// </summary>
[TestSuite]
public class GameManagerDeathBurstTest
{
    private static GameManager CreateManager()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);
        return manager;
    }

    private static Enemy RegisterEnemy(GameManager manager, EnemyKind kind, Vector2 position)
    {
        var enemy = new Enemy();
        enemy.Configure(kind);
        enemy.Position = position;
        manager.RegisterEnemyForTesting(enemy);
        return enemy;
    }

    [TestCase]
    public void Kill_SpawnsOneBurst_AtTheDeathPosition_WithTheEnemyFillColor()
    {
        var manager = CreateManager();
        try
        {
            var enemy = RegisterEnemy(manager, EnemyKind.Armored, new Vector2(140f, 96f));
            Color fill = enemy.FillColor;

            enemy.TakeDamage(1000f);

            AssertThat(manager.ActiveDeathBurstCount).IsEqual(1);
            var burst = manager.ActiveDeathBursts[0];
            AssertThat(burst.Position).IsEqual(new Vector2(140f, 96f));
            AssertThat(burst.BurstColor).IsEqual(fill);
            AssertThat(burst.ShardCount).IsEqual(GameConstants.DeathBurstArmoredShards);
            AssertThat(burst.BurstRadius).IsEqual(GameConstants.DeathBurstArmoredRadius);
        }
        finally
        {
            manager.QueueFree();
        }
    }

    [TestCase]
    public void TwoProjectilesLandingOnOneFrame_ProduceExactlyOneBurst()
    {
        var manager = CreateManager();
        try
        {
            var enemy = RegisterEnemy(manager, EnemyKind.Basic, new Vector2(60f, 60f));

            // Overkill / a second hit in the same frame must not re-trigger anything.
            enemy.TakeDamage(1000f);
            enemy.TakeDamage(1000f);

            AssertThat(manager.ActiveDeathBurstCount).IsEqual(1);
        }
        finally
        {
            manager.QueueFree();
        }
    }

    [TestCase]
    public void ReachingTheBase_DoesNotSpawnABurst()
    {
        var manager = CreateManager();
        try
        {
            var enemy = RegisterEnemy(manager, EnemyKind.Basic, Vector2.Zero);
            enemy.SetPath(new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(40f, 0f),
            });

            for (int i = 0; i < 100 && !enemy.IsDead; i++)
                enemy._Process(0.1f);

            AssertThat(enemy.IsDead).IsTrue();
            AssertThat(manager.ActiveDeathBurstCount).IsEqual(0);
        }
        finally
        {
            manager.QueueFree();
        }
    }

    [TestCase]
    public void Burst_ReleasesItselfWhenTheShortAnimationFinishes()
    {
        var manager = CreateManager();
        try
        {
            var enemy = RegisterEnemy(manager, EnemyKind.Swarm, Vector2.Zero);
            enemy.TakeDamage(1000f);
            AssertThat(manager.ActiveDeathBurstCount).IsEqual(1);

            var burst = manager.ActiveDeathBursts[0];
            burst._Process(GameConstants.DeathBurstDuration + 0.01f);

            AssertThat(manager.ActiveDeathBurstCount).IsEqual(0);
            AssertThat(burst.IsPlaying).IsFalse();
        }
        finally
        {
            manager.QueueFree();
        }
    }

    [TestCase]
    public void ManySimultaneousKills_AreBoundedByThePoolSize()
    {
        var manager = CreateManager();
        try
        {
            // More kills in one batch than the pool holds: the oldest bursts are reused
            // (Play resets them), so neither the active list nor the node count grows.
            const int kills = GameConstants.DeathBurstPoolSize + 8;
            for (int i = 0; i < kills; i++)
            {
                var enemy = new Enemy();
                enemy.Configure(EnemyKind.Basic);
                enemy.Position = new Vector2(i * 4f, 0f);
                manager.RegisterEnemyForTesting(enemy);
                enemy.TakeDamage(1000f);
            }

            AssertThat(manager.ActiveDeathBurstCount)
                .IsEqual(GameConstants.DeathBurstPoolSize);

            int burstNodes = 0;
            foreach (var child in manager.GetChildren())
            {
                if (child is DeathBurstEffect)
                    burstNodes++;
            }
            AssertThat(burstNodes).IsEqual(GameConstants.DeathBurstPoolSize);
        }
        finally
        {
            manager.QueueFree();
        }
    }

    [TestCase]
    public void ResetGame_ClearsEveryInFlightBurst()
    {
        var manager = CreateManager();
        try
        {
            var first = RegisterEnemy(manager, EnemyKind.Basic, Vector2.Zero);
            var second = RegisterEnemy(manager, EnemyKind.Basic, new Vector2(10f, 0f));
            first.TakeDamage(1000f);
            second.TakeDamage(1000f);
            AssertThat(manager.ActiveDeathBurstCount).IsEqual(2);

            manager.ResetGame();

            AssertThat(manager.ActiveDeathBurstCount).IsEqual(0);
        }
        finally
        {
            manager.QueueFree();
        }
    }
}
