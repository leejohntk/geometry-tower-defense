using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for Projectile behavior, particularly the synchronous damage application
/// that prevents double-damage when two projectiles hit the same enemy in one frame.
/// </summary>
[TestSuite]
public class ProjectileTest
{
    [TestCase]
    public void HitEnemy_KillsEnemyAndMarksDone()
    {
        var enemy = new Enemy();
        enemy.ResetForPool(); // basic: HP 10

        var tower = new ArrowTower();
        tower.Initialize(0, 0);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(100, 0), enemy);

        AssertThat(projectile.IsDone).IsFalse();

        projectile.HitEnemy(enemy);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(projectile.IsDone).IsTrue();
    }

    [TestCase]
    public void HitEnemy_SecondProjectileOnDeadEnemy_IsNoOp()
    {
        // Arrange: a basic enemy (10 HP) and two arrow projectiles (10 damage each).
        var enemy = new Enemy();
        enemy.ResetForPool();

        var tower = new ArrowTower();
        tower.Initialize(0, 0);

        var projectile1 = new ArrowProjectile();
        var projectile2 = new ArrowProjectile();
        projectile1.Initialize(tower, new Vector2(100, 0), enemy);
        projectile2.Initialize(tower, new Vector2(100, 0), enemy);

        // Act: both projectiles "hit" the same enemy in sequence.
        projectile1.HitEnemy(enemy);
        projectile2.HitEnemy(enemy);

        // Assert: enemy is dead (first hit killed it — 10 damage vs 10 HP).
        AssertThat(enemy.IsDead).IsTrue();

        // The second projectile's HitEnemy must not consume itself on a dead enemy,
        // so it keeps flying past (IsDone stays false).
        AssertThat(projectile2.IsDone).IsFalse();
    }
}
