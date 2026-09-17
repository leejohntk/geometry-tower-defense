using GeometryTowerDefense;
using GdUnit4;
using Godot;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for cannonball AoE behavior.
/// </summary>
[TestSuite]
public class CannonProjectileTest
{
    [TestCase]
    public void IsWithinRadius_ReturnsTrueInsideFalseOutside()
    {
        AssertThat(CannonProjectile.IsWithinRadius(Vector2.Zero, 64f, new Vector2(60, 0))).IsTrue();
        AssertThat(CannonProjectile.IsWithinRadius(Vector2.Zero, 64f, new Vector2(64, 0))).IsTrue(); // boundary inclusive
        AssertThat(CannonProjectile.IsWithinRadius(Vector2.Zero, 64f, new Vector2(65, 0))).IsFalse();
    }

    [TestCase]
    public void Explode_DamagesOnlyEnemiesWithinRadius()
    {
        var inside1 = new Enemy();
        inside1.Configure(EnemyKind.Swarm);
        inside1.Position = new Vector2(0, 0);

        var inside2 = new Enemy();
        inside2.Configure(EnemyKind.Swarm);
        inside2.Position = new Vector2(60, 0);

        var outside = new Enemy();
        outside.Configure(EnemyKind.Swarm);
        outside.Position = new Vector2(200, 0);

        var enemies = new List<Enemy> { inside1, inside2, outside };

        var cannon = new CannonProjectile();
        cannon.Explode(enemies, Vector2.Zero, 15f);

        AssertThat(inside1.IsDead).IsTrue();
        AssertThat(inside2.IsDead).IsTrue();
        AssertThat(outside.IsDead).IsFalse();
    }

    [TestCase]
    public void Explode_SkipsAlreadyDeadEnemy()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);
        enemy.Position = Vector2.Zero;
        enemy.TakeDamage(3); // already dead (swarm HP is 3)

        var enemies = new List<Enemy> { enemy };
        var cannon = new CannonProjectile();
        cannon.Explode(enemies, Vector2.Zero, 15f);

        // Still dead, no crash, no double-damage (TakeDamage guards on IsDead).
        AssertThat(enemy.IsDead).IsTrue();
    }
}
