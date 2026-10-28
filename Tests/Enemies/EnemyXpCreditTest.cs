using System;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Source-aware damage crediting and kill-XP splitting: per-kind XP values, credited
/// (non-overkill) damage, proportional shares, burn attribution, and the pool/reconfigure
/// hygiene that keeps a reused enemy from leaking contributions.
/// </summary>
[TestSuite]
public class EnemyXpCreditTest
{
    private static Tower MakeTower(TowerType type = TowerType.Arrow)
    {
        Tower tower = type switch
        {
            TowerType.Cannon => new CannonTower(),
            TowerType.Laser => new LaserTower(),
            _ => new ArrowTower()
        };
        tower.Initialize(0, 0);
        return tower;
    }

    [TestCase]
    public void EnemyXpValues_AreAssignedPerKind()
    {
        var basic = new Enemy();
        basic.Configure(EnemyKind.Basic);
        AssertThat(basic.XpValue).IsEqual(1);

        var swarm = new Enemy();
        swarm.Configure(EnemyKind.Swarm);
        AssertThat(swarm.XpValue).IsEqual(1);

        var armored = new Enemy();
        armored.Configure(EnemyKind.Armored);
        AssertThat(armored.XpValue).IsEqual(3);

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        AssertThat(boss.XpValue).IsEqual(10);
    }

    [TestCase]
    public void SingleSourceKill_AwardsFullKindXp()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.TakeDamage(10f, source: tower);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(tower.Xp).IsEqualApprox(1f, 0.0001f);
    }

    [TestCase]
    public void SplitDamage_ProportionsXpToCreditedDamage()
    {
        // Basic (1 XP) takes 4 from an arrow and 6 from a cannon: 0.4 / 0.6.
        var arrow = MakeTower(TowerType.Arrow);
        var cannon = MakeTower(TowerType.Cannon);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.TakeDamage(4f, source: arrow);
        enemy.TakeDamage(6f, source: cannon);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(arrow.Xp).IsEqualApprox(0.4f, 0.0001f);
        AssertThat(cannon.Xp).IsEqualApprox(0.6f, 0.0001f);
        AssertThat(arrow.Xp + cannon.Xp).IsEqualApprox(1f, 0.0001f);
    }

    [TestCase]
    public void Overkill_IsNotCredited_AndSharesUseEffectiveDamageOnly()
    {
        // Swarm has 5 HP: the second hit's 10 damage credits only the 2 HP left.
        var first = MakeTower(TowerType.Arrow);
        var second = MakeTower(TowerType.Cannon);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);

        enemy.TakeDamage(3f, source: first);
        enemy.TakeDamage(10f, source: second);

        AssertThat(enemy.CreditedDamage(first)).IsEqualApprox(3f, 0.0001f);
        AssertThat(enemy.CreditedDamage(second)).IsEqualApprox(2f, 0.0001f);
        AssertThat(first.Xp).IsEqualApprox(0.6f, 0.0001f);
        AssertThat(second.Xp).IsEqualApprox(0.4f, 0.0001f);
    }

    [TestCase]
    public void TenDamageOnThreeHpEnemy_CreditsThree()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Swarm);

        // Leave the swarm at 3 HP, then overkill it.
        enemy.TakeDamage(2f);
        enemy.TakeDamage(10f, source: tower);

        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(3f, 0.0001f);
    }

    [TestCase]
    public void ArmorReducesCreditedDamage()
    {
        // Armored 14 HP / 5 armor: a 10-damage hit credits 5, a 14-damage hit credits 9.
        var arrow = MakeTower(TowerType.Arrow);
        var cannon = MakeTower(TowerType.Cannon);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        enemy.TakeDamage(10f, source: arrow);
        enemy.TakeDamage(14f, source: cannon);

        AssertThat(enemy.CreditedDamage(arrow)).IsEqualApprox(5f, 0.0001f);
        AssertThat(enemy.CreditedDamage(cannon)).IsEqualApprox(9f, 0.0001f);
        AssertThat(enemy.IsDead).IsTrue();

        // 3 XP split 5/14 and 9/14.
        AssertThat(arrow.Xp).IsEqualApprox(3f * 5f / 14f, 0.0001f);
        AssertThat(cannon.Xp).IsEqualApprox(3f * 9f / 14f, 0.0001f);
        AssertThat(arrow.Xp + cannon.Xp).IsEqualApprox(3f, 0.0001f);
    }

    [TestCase]
    public void HitAtOrBelowArmor_CreditsNothing()
    {
        var absorbed = MakeTower(TowerType.Arrow);
        var killer = MakeTower(TowerType.Cannon);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        enemy.TakeDamage(5f, source: absorbed);   // exactly armor: 0 applied
        enemy.TakeDamage(3f, source: absorbed);   // below armor: 0 applied
        AssertThat(enemy.CreditedDamage(absorbed)).IsEqual(0f);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);

        enemy.TakeDamage(100f, source: killer);
        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(absorbed.Xp).IsEqual(0f);
        AssertThat(killer.Xp).IsEqualApprox(3f, 0.0001f);
    }

    [TestCase]
    public void RepeatedHitsFromOneTower_AccumulateOnOneEntry()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // 6 damage - 5 armor = 1 applied per hit, three hits = 3 credited.
        enemy.TakeDamage(6f, source: tower);
        enemy.TakeDamage(6f, source: tower);
        enemy.TakeDamage(6f, source: tower);

        AssertThat(enemy.CreditSourceCount).IsEqual(1);
        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(3f, 0.0001f);
    }

    [TestCase]
    public void NoSource_DealsDamageButAwardsNoXp()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.TakeDamage(10f);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
    }

    [TestCase]
    public void BurnKill_CreditsTheBurnSource_AfterContactHasEnded()
    {
        var arrow = MakeTower(TowerType.Arrow);
        var laser = MakeTower(TowerType.Laser);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic); // 10 HP

        // 8 damage from the arrow, then a burn that finishes the remaining 2 HP.
        enemy.TakeDamage(8f, source: arrow);
        enemy.ApplyBurn(2f, 2f, laser);

        enemy.TickStatuses(0.5f); // 1 burn damage (HP 1)
        AssertThat(enemy.IsDead).IsFalse();
        enemy.TickStatuses(0.5f); // 1 burn damage -> dead, credited to the laser

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(arrow.Xp).IsEqualApprox(0.8f, 0.0001f);
        AssertThat(laser.Xp).IsEqualApprox(0.2f, 0.0001f);
    }

    [TestCase]
    public void BurnAttribution_DoesNotOutliveTheBurn()
    {
        var laser = MakeTower(TowerType.Laser);
        var other = MakeTower(TowerType.Cannon);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.ApplyBurn(2f, 1f, laser);
        enemy.TickStatuses(1.5f); // burn expires
        AssertThat(enemy.IsBurning).IsFalse();

        // A later plain hit from an unrelated source credits that source only.
        enemy.TakeDamage(2f, source: other);
        AssertThat(enemy.CreditedDamage(laser)).IsEqualApprox(2f, 0.0001f); // only the burn
        AssertThat(enemy.CreditedDamage(other)).IsEqualApprox(2f, 0.0001f);
    }

    [TestCase]
    public void DamageWithoutADeath_AwardsNoXpYet()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.TakeDamage(4f, source: tower);
        AssertThat(enemy.IsDead).IsFalse();
        AssertThat(tower.Xp).IsEqual(0f);
    }

    [TestCase]
    public void KillXp_IsDistributedBeforeTheDestroyedSignal()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        float xpWhenDestroyed = -1f;
        enemy.Destroyed += dying => xpWhenDestroyed = tower.Xp;

        enemy.TakeDamage(10f, source: tower);

        // GameManager releases/resets the enemy from the Destroyed handler, so the
        // share must already be on the tower by then.
        AssertThat(xpWhenDestroyed).IsEqualApprox(1f, 0.0001f);
        AssertThat(tower.Xp).IsEqualApprox(1f, 0.0001f);
    }

    [TestCase]
    public void Configure_ClearsCredit()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);
        enemy.TakeDamage(10f, source: tower);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);

        enemy.Configure(EnemyKind.Basic);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        AssertThat(enemy.CreditedDamage(tower)).IsEqual(0f);
    }

    [TestCase]
    public void PoolReset_ClearsCredit_AndBossXpValueCannotLeak()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Progenitor);
        enemy.TakeDamage(1000f, ignoreArmor: true, source: tower);
        AssertThat(tower.Xp).IsEqualApprox(10f, 0.0001f);

        enemy.ResetForPool();
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        AssertThat(enemy.XpValue).IsEqual(1);

        // Reused as a Basic enemy: full kill awards 1 XP, not a boss's 10.
        enemy.Configure(EnemyKind.Basic);
        enemy.TakeDamage(10f, source: tower);
        AssertThat(tower.Xp).IsEqualApprox(11f, 0.0001f);
    }

    [TestCase]
    public void SummonConfiguredEnemy_KeepsKindXp_EvenWithZeroCoins()
    {
        var tower = MakeTower();
        var summon = new Enemy();
        summon.Configure(EnemyKind.Armored);
        summon.SetCoinDrop(0);

        AssertThat(summon.CoinDrop).IsEqual(0);
        AssertThat(summon.XpValue).IsEqual(3);

        summon.TakeDamage(1000f, ignoreArmor: true, source: tower);
        AssertThat(tower.Xp).IsEqualApprox(3f, 0.0001f);
    }

    [TestCase]
    public void SteadyStateCrediting_DoesNotAllocatePerHit()
    {
        var tower = MakeTower();
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        // Warm up: the first hit creates the tower's credit entry.
        enemy.TakeDamage(0.01f, ignoreArmor: true, source: tower);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            enemy.TakeDamage(0.01f, ignoreArmor: true, source: tower);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Practical proxy for "no per-hit allocation": repeated hits reuse the credit
        // entry and the pooled lookup, so the loop must not grow the heap.
        AssertThat(allocated).IsLess(4096L);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);
        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(10.01f, 0.001f);
    }
}
