using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// End-to-end damage-source plumbing for projectiles: every arrow hit (pierced hits
/// included) and every cannon splash hit credits the firing tower, credited damage is
/// the effective (post-armor, non-overkill) amount, and rank multipliers reach enemy
/// HP as floats rather than truncated ints.
/// </summary>
[TestSuite]
public class DamageSourceCreditTest
{
    private static ArrowTower Rank3Arrow()
    {
        var tower = new ArrowTower();
        tower.Initialize(0, 0);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        return tower;
    }

    private static CannonTower Rank3Cannon()
    {
        var tower = new CannonTower();
        tower.Initialize(0, 0);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyArchetype(TowerArchetype.Cluster)).IsTrue();
        return tower;
    }

    [TestCase]
    public void ArrowHit_CreditsSource_AndFloatDamagePassesArmor()
    {
        var tower = Rank3Arrow();
        AssertThat(tower.Damage).IsEqualApprox(10.9f, 0.001f);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored); // 14 HP, 5 armor

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), enemy);
        projectile.HitEnemy(enemy);

        // 10.9 - 5 = 5.9 applied (not the truncated 5 of an int pipeline).
        AssertThat(enemy.CurrentHP).IsEqualApprox(8.1f, 0.001f);
        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(5.9f, 0.001f);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);
    }

    [TestCase]
    public void CannonExplosion_FloatDamagePassesArmor_AndCreditsTheDirectTargetOnce()
    {
        var tower = Rank3Cannon();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored); // 14 HP, 5 armor
        enemy.Position = Vector2.Zero;

        var shell = new CannonProjectile();
        shell.Initialize(tower, new Vector2(100, 0), enemy);
        AssertThat(shell.ExplosionDamage).IsEqualApprox(16.35f, 0.001f);

        shell.Explode(new List<Enemy> { enemy }, Vector2.Zero, shell.ExplosionDamage);

        // 16.35 - 5 = 11.35 applied, credited exactly once (the direct target is in
        // the blast list once, not once for contact plus once for splash).
        AssertThat(enemy.CurrentHP).IsEqualApprox(14f - 11.35f, 0.001f);
        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(11.35f, 0.001f);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);
    }

    [TestCase]
    public void CannonSplash_CreditsEveryEnemyInTheBlastIndividually()
    {
        var tower = new CannonTower();
        tower.Initialize(0, 0);

        var first = new Enemy();
        first.Configure(EnemyKind.Armored);
        first.Position = Vector2.Zero;

        var second = new Enemy();
        second.Configure(EnemyKind.Armored);
        second.Position = new Vector2(30, 0);

        var outside = new Enemy();
        outside.Configure(EnemyKind.Armored);
        outside.Position = new Vector2(500, 0);

        var shell = new CannonProjectile();
        shell.Initialize(tower, new Vector2(100, 0), first);
        shell.Explode(
            new List<Enemy> { first, second, outside },
            Vector2.Zero,
            tower.Damage);

        // 15 - 5 armor = 10 credited to each enemy actually inside the blast.
        AssertThat(first.CreditedDamage(tower)).IsEqualApprox(10f, 0.0001f);
        AssertThat(second.CreditedDamage(tower)).IsEqualApprox(10f, 0.0001f);
        AssertThat(outside.CreditedDamage(tower)).IsEqual(0f);

        // XP is per enemy on death: two Armored kills award 3 + 3.
        first.TakeDamage(1000f, ignoreArmor: true, source: tower);
        second.TakeDamage(1000f, ignoreArmor: true, source: tower);
        AssertThat(tower.Xp).IsEqualApprox(6f, 0.0001f);
    }

    [TestCase]
    public void PiercedArrowHits_EachCreditTheSameTowerEntry()
    {
        var tower = Rank3Arrow(); // Pierce archetype at zero skill rank = 1 pierce
        AssertThat(tower.PierceCount).IsEqual(1);

        var first = new Enemy();
        first.Configure(EnemyKind.Basic);
        var second = new Enemy();
        second.Configure(EnemyKind.Basic);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), first);

        projectile.HitEnemy(first);
        projectile.HitEnemy(second);
        AssertThat(projectile.IsDone).IsTrue();

        // Both 10 HP basics die to the rank-3 arrow; each credits the tower once.
        AssertThat(first.CreditedDamage(tower)).IsEqualApprox(10f, 0.0001f);
        AssertThat(second.CreditedDamage(tower)).IsEqualApprox(10f, 0.0001f);
        AssertThat(first.CreditSourceCount).IsEqual(1);
        AssertThat(second.CreditSourceCount).IsEqual(1);

        AssertThat(first.IsDead).IsTrue();
        AssertThat(second.IsDead).IsTrue();
        AssertThat(tower.Xp).IsEqualApprox(2f, 0.0001f);
    }

    [TestCase]
    public void CannonVolleyKillingManyEnemiesInOneBlast_DistributesXpPerEnemy()
    {
        var tower = new CannonTower();
        tower.Initialize(0, 0);

        // A single explosion snapshot holding 20 low-HP enemies: every one dies during
        // the loop and gets its own XP share without a collection-modified exception.
        var enemies = new List<Enemy>();
        for (int i = 0; i < 20; i++)
        {
            var enemy = new Enemy();
            enemy.Configure(EnemyKind.Swarm); // 5 HP
            enemy.Position = new Vector2(i, 0); // inside the blast radius
            enemies.Add(enemy);
        }

        var shell = new CannonProjectile();
        shell.Initialize(tower, new Vector2(100, 0), enemies[0]);
        shell.Explode(enemies, Vector2.Zero, tower.Damage);

        foreach (var enemy in enemies)
            AssertThat(enemy.IsDead).IsTrue();

        // 20 Swarm kills x 1 XP each, awarded per enemy.
        AssertThat(tower.Xp).IsEqualApprox(20f, 0.0001f);
    }

    [TestCase]
    public void ArrowKill_ThroughArmor_AwardsKindXp()
    {
        var tower = Rank3Arrow();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), enemy);

        // Three 5.9-damage hits: 14 -> 8.1 -> 2.2 -> dead on the third.
        projectile.HitEnemy(enemy);
        projectile.HitEnemy(enemy);
        enemy.TakeDamage(100f, source: tower);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(tower.Xp).IsEqualApprox(3f, 0.0001f);
    }
}
