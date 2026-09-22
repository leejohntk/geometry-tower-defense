using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Stun and burn status behavior on enemies: movement pause, timer tick/refresh,
/// burn dps/duration/ignore-armor, and pool-reset cleanup.
/// </summary>
[TestSuite]
public class EnemyStatusTest
{
    [TestCase]
    public void ApplyStun_SetsStunnedState_WithFullDuration()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.ApplyStun(GameConstants.SkillCannonStunDuration);

        AssertThat(enemy.IsStunned).IsTrue();
        AssertThat(enemy.StunRemaining).IsEqual(GameConstants.SkillCannonStunDuration);
    }

    [TestCase]
    public void Stun_PausesMovement_UntilItExpires()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.SetPath(new List<Vector2> { new Vector2(32, 32), new Vector2(200, 32) });

        Vector2 start = enemy.Position;

        enemy.ApplyStun(GameConstants.SkillCannonStunDuration);
        enemy._Process(0.5f);

        // Stunned: no movement.
        AssertThat(enemy.Position).IsEqual(start);

        enemy.TickStatuses(GameConstants.SkillCannonStunDuration);
        AssertThat(enemy.IsStunned).IsFalse();

        // First tick advances past waypoint[0] (the anchor); the second actually moves.
        enemy._Process(0.5f);
        enemy._Process(0.5f);
        // Stun expired: movement resumes.
        AssertThat(enemy.Position.X > start.X).IsTrue();
    }

    [TestCase]
    public void ReStun_RefreshesTimer_DoesNotStack()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.ApplyStun(GameConstants.SkillCannonStunDuration);
        enemy.TickStatuses(0.5f);
        AssertThat(enemy.StunRemaining).IsEqual(0.5f);

        enemy.ApplyStun(GameConstants.SkillCannonStunDuration);
        AssertThat(enemy.StunRemaining).IsEqual(GameConstants.SkillCannonStunDuration);
    }

    [TestCase]
    public void Burn_DealsDpsOverDuration_ThenExpires()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored); // 14 HP, armor 5 — burn must bypass armor

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        // Half a second of burn: 2 dps * 0.5s = 1 damage, ignoring armor.
        enemy.TickStatuses(0.5f);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 1f);
        AssertThat(enemy.IsBurning).IsTrue();
        AssertThat(enemy.BurnRemaining).IsEqual(1.5f);

        // Another half second: 1 more damage.
        enemy.TickStatuses(0.5f);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 2f);
        AssertThat(enemy.BurnRemaining).IsEqual(1.0f);

        // Finish the burn: 2 more damage over the final 1.0s, then expired.
        enemy.TickStatuses(1.0f);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 4f);
        AssertThat(enemy.IsBurning).IsFalse();
        AssertThat(enemy.BurnRemaining).IsEqual(0f);

        // Total burn damage = dps * duration = 2 * 2 = 4, all ignoring armor.
        AssertThat(enemy.IsDead).IsFalse();
    }

    [TestCase]
    public void ReBurn_RefreshesDuration_DoesNotStack()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        enemy.TickStatuses(1.0f);
        AssertThat(enemy.BurnRemaining).IsEqual(1.0f);

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        AssertThat(enemy.BurnRemaining).IsEqual(GameConstants.SkillLaserBurnDuration);
    }

    [TestCase]
    public void Burn_IgnoresArmor()
    {
        var armored = new Enemy();
        armored.Configure(EnemyKind.Armored); // armor 5

        // A 3-damage tick below armor would be fully absorbed by a normal hit; burn
        // damage ignores armor, so the full 2 dps * 1.5s = 3 damage applies.
        armored.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        armored.TickStatuses(1.5f);

        AssertThat(armored.CurrentHP).IsEqual(14f - 3f);
    }

    [TestCase]
    public void ResetForPool_ClearsStunAndBurn()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.ApplyStun(GameConstants.SkillCannonStunDuration);
        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        enemy.ResetForPool();
        enemy.Configure(EnemyKind.Basic);

        AssertThat(enemy.IsStunned).IsFalse();
        AssertThat(enemy.IsBurning).IsFalse();
        AssertThat(enemy.StunRemaining).IsEqual(0f);
        AssertThat(enemy.BurnRemaining).IsEqual(0f);
    }
}
