using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Cannon mechanic behavior: cluster volley shell count, boosted splash radius per
/// shell, and stun application on hit (with armor still reducing the stunning hit).
/// </summary>
[TestSuite]
public class CannonMechanicsTest
{
    [TestCase]
    public void Cluster_ShellCount_IncreasesPerRank()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonCluster, 5);

        var cannon = new CannonTower();
        cannon.SetSkillTree(state);

        AssertThat(cannon.ClusterCount).IsEqual(6);
        AssertThat(new CannonTower().ClusterCount).IsEqual(1); // rank 0 → single shell
    }

    [TestCase]
    public void Cluster_EachShell_UsesBoostedSplashRadius()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonSplashRadius, 5); // 104 px

        var cannon = new CannonTower();
        cannon.Initialize(0, 0);
        cannon.SetSkillTree(state);

        var target = new Enemy();
        target.Configure(EnemyKind.Basic);

        var shell = new CannonProjectile();
        shell.Initialize(cannon, new Vector2(100, 0), target);

        AssertThat(cannon.SplashRadius).IsEqual(104);
        AssertThat(shell.ExplosionRadius).IsEqual(104);
    }

    [TestCase]
    public void Stun_RollsPerEnemyHit_AndArmorStillReducesDamage()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonStunChance, 5); // 30%

        var cannon = new CannonTower();
        cannon.Initialize(0, 0);
        cannon.SetSkillTree(state);
        cannon.SetSkillRandom(new ScriptedRandom(0.0)); // always succeeds

        var target = new Enemy();
        target.Configure(EnemyKind.Basic);

        var shell = new CannonProjectile();
        shell.Initialize(cannon, new Vector2(100, 0), target);

        var armored = new Enemy();
        armored.Configure(EnemyKind.Armored); // 14 HP, 5 armor
        armored.Position = Vector2.Zero;

        // Cannon damage 15 - 5 armor = 10 → HP 4, survives, then stun lands.
        shell.Explode(new List<Enemy> { armored }, Vector2.Zero, cannon.Damage);

        AssertThat(armored.CurrentHP).IsEqual(4f);
        AssertThat(armored.IsStunned).IsTrue();
        AssertThat(armored.StunRemaining).IsEqual(GameConstants.SkillCannonStunDuration);
    }

    [TestCase]
    public void Stun_Miss_DoesNotStun_ButStillDamages()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonStunChance, 5); // 30%

        var cannon = new CannonTower();
        cannon.Initialize(0, 0);
        cannon.SetSkillTree(state);
        cannon.SetSkillRandom(new ScriptedRandom(1.0)); // always fails

        var target = new Enemy();
        target.Configure(EnemyKind.Basic);

        var shell = new CannonProjectile();
        shell.Initialize(cannon, new Vector2(100, 0), target);

        var armored = new Enemy();
        armored.Configure(EnemyKind.Armored);
        armored.Position = Vector2.Zero;

        shell.Explode(new List<Enemy> { armored }, Vector2.Zero, cannon.Damage);

        AssertThat(armored.CurrentHP).IsEqual(4f);
        AssertThat(armored.IsStunned).IsFalse();
    }

    [TestCase]
    public void Stun_DeadEnemy_IsNotStunned()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonStunChance, 5);

        var cannon = new CannonTower();
        cannon.Initialize(0, 0);
        cannon.SetSkillTree(state);
        cannon.SetSkillRandom(new ScriptedRandom(0.0));

        var target = new Enemy();
        target.Configure(EnemyKind.Basic);

        var shell = new CannonProjectile();
        shell.Initialize(cannon, new Vector2(100, 0), target);

        var swarm = new Enemy();
        swarm.Configure(EnemyKind.Swarm); // 5 HP — cannon damage kills it outright
        swarm.Position = Vector2.Zero;

        shell.Explode(new List<Enemy> { swarm }, Vector2.Zero, cannon.Damage);

        AssertThat(swarm.IsDead).IsTrue();
        AssertThat(swarm.IsStunned).IsFalse();
    }
}
