using GeometryTowerDefense;
using GdUnit4;
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
}
