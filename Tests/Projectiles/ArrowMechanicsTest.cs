using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Arrow mechanic behavior: pierce continuation (count + armor per hit) and the
/// independent per-hit crit roll for double damage.
/// </summary>
[TestSuite]
public class ArrowMechanicsTest
{
    /// <summary>
    /// Seed mechanics are inert until rank 3, so pierce/crit tests must specialize
    /// the tower first (rank 3 + the matching archetype).
    /// </summary>
    private static ArrowTower Rank3Arrow(SkillTreeState state, TowerArchetype archetype)
    {
        var tower = new ArrowTower();
        tower.Initialize(0, 0);
        tower.SetSkillTree(state);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyArchetype(archetype)).IsTrue();
        return tower;
    }

    private static void HitOnce(ArrowTower tower, Enemy enemy)
    {
        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), enemy);
        projectile.HitEnemy(enemy);
    }

    [TestCase]
    public void Pierce_PassesThroughPierceCountEnemies_WithArmorPerHit()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowPierce, 5); // pierce 5 → 6 total hits
        var tower = Rank3Arrow(state, TowerArchetype.Pierce);

        var first = new Enemy();
        first.Configure(EnemyKind.Basic);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), first);

        var armored = new List<Enemy>();
        for (int i = 0; i < 7; i++)
        {
            var enemy = new Enemy();
            enemy.Configure(EnemyKind.Armored); // 14 HP, 5 armor
            armored.Add(enemy);
        }

        // Hit 6 distinct enemies; each takes rank-3 arrow 10.9 - 5 armor = 5.9.
        for (int i = 0; i < 6; i++)
            projectile.HitEnemy(armored[i]);

        AssertThat(projectile.IsDone).IsTrue();
        foreach (var enemy in armored.GetRange(0, 6))
            AssertThat(enemy.CurrentHP).IsEqualApprox(8.1f, 0.001f);

        // The 7th enemy is never touched because the arrow is spent.
        AssertThat(armored[6].CurrentHP).IsEqual(14f);
    }

    [TestCase]
    public void Pierce_HitEnemy_NeverReHitsTheSameEnemy()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowPierce, 3); // 4 total hits
        var tower = Rank3Arrow(state, TowerArchetype.Pierce);

        var target = new Enemy();
        target.Configure(EnemyKind.Armored);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), target);

        // First hit deals 5.9 damage; a second HitEnemy on the same enemy is a no-op.
        projectile.HitEnemy(target);
        projectile.HitEnemy(target);

        AssertThat(target.CurrentHP).IsEqualApprox(8.1f, 0.001f);
        AssertThat(projectile.IsDone).IsFalse(); // still has pierce remaining
    }

    [TestCase]
    public void Crit_DealsDoubleDamage_AndRollsPerHit()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowCritChance, 5);  // 40% after specialization
        var tower = Rank3Arrow(state, TowerArchetype.Crit);
        tower.SetSkillRandom(new ScriptedRandom(1.0, 0.0, 1.0, 0.0)); // crit, no-crit, crit

        var e1 = new Enemy();
        e1.Configure(EnemyKind.Armored); // 14 HP, 5 armor
        var e2 = new Enemy();
        e2.Configure(EnemyKind.Armored);
        var e3 = new Enemy();
        e3.Configure(EnemyKind.Armored);

        HitOnce(tower, e1); // crit: 10.9 x 2 - 5 = 16.8 → dead
        HitOnce(tower, e2); // no crit: 10.9 - 5 = 5.9 → HP 8.1
        HitOnce(tower, e3); // crit → dead

        AssertThat(e1.IsDead).IsTrue();
        AssertThat(e2.IsDead).IsFalse();
        AssertThat(e2.CurrentHP).IsEqualApprox(8.1f, 0.001f);
        AssertThat(e3.IsDead).IsTrue();
    }

    [TestCase]
    public void Crit_NoCritChance_DealsNormalDamage()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowPierce, 0);      // single hit
        state.SetRank(SkillTreeCatalog.ArrowCritChance, 0);  // 0% crit
        var tower = new ArrowTower();
        tower.Initialize(0, 0);
        tower.SetSkillTree(state);

        var target = new Enemy();
        target.Configure(EnemyKind.Armored);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), target);
        projectile.HitEnemy(target);

        // 10 - 5 = 5, no double damage.
        AssertThat(target.CurrentHP).IsEqual(9f);
    }

    [TestCase]
    public void Pierce_EmitsPiercedPerPassThrough_ButNotForTheConsumingHit()
    {
        // The pierce spark (presentation) hangs off the Pierced signal, so this pins the
        // rule that drives it: one Pierced per hit the arrow survives, none for the hit
        // that consumes it (that one emits EnemyHit and reads through the enemy's own
        // damage/death feedback).
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowPierce, 2); // 3 total hits
        var tower = Rank3Arrow(state, TowerArchetype.Pierce);

        var first = new Enemy();
        first.Configure(EnemyKind.Basic);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), first);

        int pierced = 0;
        int consumed = 0;
        Enemy? lastPierced = null;
        projectile.Pierced += (_, enemy) =>
        {
            pierced++;
            lastPierced = enemy;
        };
        projectile.EnemyHit += (_, _) => consumed++;

        var e1 = new Enemy();
        e1.Configure(EnemyKind.Armored); // 14 HP, 5 armor: survives a hit
        var e2 = new Enemy();
        e2.Configure(EnemyKind.Armored);
        var e3 = new Enemy();
        e3.Configure(EnemyKind.Armored);

        projectile.HitEnemy(e1);
        projectile.HitEnemy(e2);
        AssertThat(pierced).IsEqual(2);
        AssertThat(lastPierced).IsEqual(e2);
        AssertThat(projectile.IsDone).IsFalse();

        projectile.HitEnemy(e3); // final hit consumes the arrow
        AssertThat(pierced).IsEqual(2);
        AssertThat(consumed).IsEqual(1);
        AssertThat(projectile.IsDone).IsTrue();
    }

    [TestCase]
    public void NoPierce_ConsumingHit_EmitsNoPiercedSignal()
    {
        var tower = new ArrowTower();
        tower.Initialize(0, 0);

        var target = new Enemy();
        target.Configure(EnemyKind.Armored);

        var projectile = new ArrowProjectile();
        projectile.Initialize(tower, new Vector2(200, 0), target);

        int pierced = 0;
        projectile.Pierced += (_, _) => pierced++;

        projectile.HitEnemy(target);

        // Un-pierced arrow: every hit consumes it, so nothing sparks.
        AssertThat(pierced).IsEqual(0);
        AssertThat(projectile.IsDone).IsTrue();
    }
}
