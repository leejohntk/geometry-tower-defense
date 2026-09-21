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
    public void SwarmEnemy_HasFiveHP_Diameter24()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);

        AssertThat(enemy.Diameter).IsEqual(24f);
        AssertThat(enemy.CollisionRadius).IsEqual(12f);
        AssertThat(enemy.CoinDrop).IsEqual(1);
        AssertThat(enemy.Kind).IsEqual(EnemyKind.Swarm);

        // 5 HP: 4 damage leaves it alive, 1 more kills it.
        enemy.TakeDamage(4);
        AssertThat(enemy.IsDead).IsFalse();
        enemy.TakeDamage(1);
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void TakeDamage_OnDeadEnemy_IsNoOp()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.TakeDamage(5); // dead (swarm HP is 5)

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
        enemy.SetFormationOffset(offset, 0f); // static offset: persistence only, no orbit

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
        enemy.SetFormationOffset(new Vector2(20, 0), 0f);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });

        AssertThat(enemy.Position).IsEqual(new Vector2(52, 32));

        enemy.ResetForPool();

        AssertThat(enemy.FormationOffset).IsEqual(Vector2.Zero);

        // A pooled reuse as a basic enemy must land exactly on the bare waypoint.
        enemy.Configure(EnemyKind.Basic);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });
        AssertThat(enemy.Position).IsEqual(new Vector2(32, 32));
    }

    [TestCase]
    public void SwarmFormation_OffsetRotatesOverTime()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.SetFormationOffset(new Vector2(20, 0), GameConstants.SwarmClusterRotationSpeed);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });

        float angleBefore = Mathf.Atan2(enemy.FormationOffset.Y, enemy.FormationOffset.X);

        // One process tick rotates the offset by SwarmClusterRotationSpeed * delta.
        enemy._Process(0.1);

        float angleAfter = Mathf.Atan2(enemy.FormationOffset.Y, enemy.FormationOffset.X);
        AssertThat(angleAfter).IsNotEqual(angleBefore);
        AssertThat(Mathf.Abs(angleAfter - angleBefore) > 0.01f).IsTrue();
    }

    [TestCase]
    public void BasicEnemy_OffsetStaysZero_NoRotation()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.SetFormationOffset(Vector2.Zero, 0f);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(96, 32) });

        enemy._Process(0.1);
        enemy._Process(0.1);

        AssertThat(enemy.FormationOffset).IsEqual(Vector2.Zero);
        AssertThat(enemy.FormationAngularSpeed).IsEqual(0f);
    }

    [TestCase]
    public void ResetForPool_ClearsFormationAngularSpeed()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.SetFormationOffset(new Vector2(20, 0), GameConstants.SwarmClusterRotationSpeed);

        AssertThat(enemy.FormationAngularSpeed).IsEqual(GameConstants.SwarmClusterRotationSpeed);

        enemy.ResetForPool();
        AssertThat(enemy.FormationAngularSpeed).IsEqual(0f);
    }

    [TestCase]
    public void ArmoredEnemy_HasCorrectStats()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        AssertThat(enemy.Kind).IsEqual(EnemyKind.Armored);
        AssertThat(enemy.CurrentHP).IsEqual(14f);
        AssertThat(enemy.Armor).IsEqual(5);
        AssertThat(enemy.Diameter).IsEqual(48f);
        AssertThat(enemy.CollisionRadius).IsEqual(24f);
        AssertThat(enemy.CoinDrop).IsEqual(2);
    }

    [TestCase]
    public void ArmoredEnemy_ArrowDamage_ReducedByArmor()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // Arrow (10 dmg) - 5 armor = 5 applied.
        enemy.TakeDamage(GameConstants.ArrowTowerDamage);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 5f);
    }

    [TestCase]
    public void ArmoredEnemy_CannonDamage_ReducedByArmor()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // Cannon (15 dmg) - 5 armor = 10 applied.
        enemy.TakeDamage(GameConstants.CannonTowerDamage);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 10f);
    }

    [TestCase]
    public void ArmoredEnemy_DamageAtOrBelowArmor_ClampsToZero()
    {
        // Exactly the armor value.
        var exact = new Enemy();
        exact.Configure(EnemyKind.Armored);
        exact.TakeDamage(5);
        AssertThat(exact.CurrentHP).IsEqual(14f);
        AssertThat(exact.IsDead).IsFalse();

        // Below the armor value: must not heal or go negative.
        var below = new Enemy();
        below.Configure(EnemyKind.Armored);
        below.TakeDamage(3);
        AssertThat(below.CurrentHP).IsEqual(14f);
        AssertThat(below.IsDead).IsFalse();
    }

    [TestCase]
    public void ArmoredEnemy_IgnoreArmor_BypassesReduction()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // 4 damage would be fully absorbed by 5 armor; ignoring armor applies it all.
        enemy.TakeDamage(4, ignoreArmor: true);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 4f);
    }

    [TestCase]
    public void ArmoredEnemy_LaserDps_IgnoresArmor()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // Laser drains Dps per second with armor bypass.
        enemy.TakeDamage(GameConstants.LaserTowerDps, ignoreArmor: true);
        AssertThat(enemy.CurrentHP).IsEqual(14f - GameConstants.LaserTowerDps);
    }

    [TestCase]
    public void ArmoredEnemy_ArrowNeedsExactlyThreeHits()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        enemy.TakeDamage(GameConstants.ArrowTowerDamage);
        enemy.TakeDamage(GameConstants.ArrowTowerDamage);
        AssertThat(enemy.IsDead).IsFalse();

        enemy.TakeDamage(GameConstants.ArrowTowerDamage);
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void ArmoredEnemy_CannonNeedsExactlyTwoHits()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        enemy.TakeDamage(GameConstants.CannonTowerDamage);
        AssertThat(enemy.IsDead).IsFalse();

        enemy.TakeDamage(GameConstants.CannonTowerDamage);
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void PooledReuse_ArmoredThenBasic_ResetsArmor()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);
        AssertThat(enemy.Armor).IsEqual(5);

        enemy.ResetForPool();
        enemy.Configure(EnemyKind.Basic);

        AssertThat(enemy.Armor).IsEqual(0);
        AssertThat(enemy.CurrentHP).IsEqual(10f);
    }

    [TestCase]
    public void SkillPointValue_IsTieredPerEnemyKind()
    {
        var basic = new Enemy();
        basic.Configure(EnemyKind.Basic);
        var swarm = new Enemy();
        swarm.Configure(EnemyKind.Swarm);
        var armored = new Enemy();
        armored.Configure(EnemyKind.Armored);

        AssertThat(basic.SkillPointValue).IsEqual(GameConstants.SkillPointBasicPerKill);
        AssertThat(swarm.SkillPointValue).IsEqual(GameConstants.SkillPointSwarmPerKill);
        AssertThat(armored.SkillPointValue).IsEqual(GameConstants.SkillPointArmoredPerKill);
    }
}
