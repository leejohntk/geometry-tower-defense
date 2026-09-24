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

    [TestCase]
    public void ApplyBurn_FlashesTheProc_AndDoesNotChangeBurnTiming()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Armored);

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        // The proc itself is visible (presentation-only state) ...
        AssertThat(enemy.BurnFlashRemaining).IsEqual(GameConstants.BurnProcFlashDuration);

        // ... and the DoT still ticks exactly as before: dps * elapsed, ignoring armor.
        enemy.TickStatuses(1.0f);
        AssertThat(enemy.CurrentHP).IsEqual(14f - 2f);
        AssertThat(enemy.BurnRemaining).IsEqual(1.0f);
    }

    [TestCase]
    public void ReBurn_RefreshesTheProcFlash()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        enemy.TickStatuses(GameConstants.BurnProcFlashDuration);
        AssertThat(enemy.BurnFlashRemaining).IsEqual(0f);

        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        AssertThat(enemy.BurnFlashRemaining).IsEqual(GameConstants.BurnProcFlashDuration);
    }

    [TestCase]
    public void BurnProcFlash_DecaysToZero_WhileTheBurnOutlastsIt()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        // The flash is much shorter than the burn, so it is gone mid-DoT.
        enemy.TickStatuses(GameConstants.BurnProcFlashDuration);
        AssertThat(enemy.BurnFlashRemaining).IsEqual(0f);
        AssertThat(enemy.IsBurning).IsTrue();
    }

    [TestCase]
    public void ResetForPool_ClearsTheBurnProcFlashAndPulse()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        // A few ticks into the burn: flash still running, pulse already advanced.
        enemy.TickStatuses(0.1f);
        AssertThat(enemy.BurnFlashRemaining).IsGreater(0f);
        AssertThat(enemy.BurnPulseStep).IsGreater(0);

        enemy.ResetForPool();
        enemy.Configure(EnemyKind.Basic);

        AssertThat(enemy.BurnFlashRemaining).IsEqual(0f);
        AssertThat(enemy.BurnPulseStep).IsEqual(0);
    }

    [TestCase]
    public void BurnPulseStep_IsStableWithinAStep_AndAdvancesAtStepBoundaries()
    {
        float stepWidth = GameConstants.BurnPulsePeriod / GameConstants.BurnPulseSteps;

        // Sampling anywhere inside one step yields the same step, which is what keeps
        // the burning tint's redraw rate at one per step rather than one per frame.
        AssertThat(Enemy.BurnPulseStepAt(0f)).IsEqual(0);
        AssertThat(Enemy.BurnPulseStepAt(stepWidth * 0.5f)).IsEqual(0);
        AssertThat(Enemy.BurnPulseStepAt(stepWidth * 1.1f)).IsEqual(1);
        AssertThat(Enemy.BurnPulseStepAt(stepWidth * 2.5f)).IsEqual(2);
        AssertThat(Enemy.BurnPulseStepAt(GameConstants.BurnPulsePeriod * 0.99f))
            .IsEqual(GameConstants.BurnPulseSteps - 1);
    }

    [TestCase]
    public void BurnPulseStep_StaysBounded_ForUnboundedAndBadPhases()
    {
        // A long burn accumulates phase without wrapping it; an out-of-range or
        // non-finite phase must still resolve to a valid step (never throw, never index
        // past the palette).
        for (float phase = 0f; phase < 10f; phase += 0.017f)
        {
            int step = Enemy.BurnPulseStepAt(phase);
            AssertThat(step).IsGreaterEqual(0);
            AssertThat(step).IsLessEqual(GameConstants.BurnPulseSteps - 1);
        }

        AssertThat(Enemy.BurnPulseStepAt(float.NaN)).IsEqual(0);
        AssertThat(Enemy.BurnPulseStepAt(float.PositiveInfinity)).IsEqual(0);
    }

    [TestCase]
    public void BurnPulseStep_WrapsEachCycle()
    {
        // The phase keeps counting up across cycles; step 0 recurs one period later.
        float period = GameConstants.BurnPulsePeriod;
        AssertThat(Enemy.BurnPulseStepAt(period))
            .IsEqual(Enemy.BurnPulseStepAt(0f));
        AssertThat(Enemy.BurnPulseStepAt(period * 4f + 0.01f))
            .IsEqual(Enemy.BurnPulseStepAt(0.01f));
    }

    [TestCase]
    public void BurnPulseIntensity_IsZeroAtTheTrough_AndPeaksMidCycle()
    {
        // Step 0 being intensity 0 is what keeps the pulse's resting tint identical to
        // the static burn tint the enemy showed before the pulse existed.
        AssertThat(Enemy.BurnPulseIntensity(0)).IsEqual(0f);

        float peak = Enemy.BurnPulseIntensity(GameConstants.BurnPulseSteps / 2);
        AssertThat(peak > 0.5f).IsTrue();
        AssertThat(peak).IsLessEqual(1f);

        for (int step = 0; step < GameConstants.BurnPulseSteps; step++)
        {
            float intensity = Enemy.BurnPulseIntensity(step);
            AssertThat(intensity).IsGreaterEqual(0f);
            AssertThat(intensity).IsLessEqual(1f);
        }
    }

    [TestCase]
    public void TickStatuses_AdvancesTheBurnPulseStep_WhileBurning()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        AssertThat(enemy.BurnPulseStep).IsEqual(0);

        // Past one pulse step boundary: the step advances, which is exactly what
        // triggers the single repaint for that step.
        float step = GameConstants.BurnPulsePeriod / GameConstants.BurnPulseSteps;
        enemy.TickStatuses(step * 1.1f);
        AssertThat(enemy.BurnPulseStep).IsEqual(1);

        // The whole cycle completes and repeats (the phase wraps).
        enemy.TickStatuses(GameConstants.BurnPulsePeriod);
        AssertThat(enemy.BurnPulseStep).IsEqual(1);
    }

    [TestCase]
    public void TickStatuses_KeepsThePulseStepStable_ForManySubStepFrames()
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);

        // The redraw throttle: many small frames that together stay inside one pulse step
        // must not advance the step — so repaints stay at one per step, not per frame.
        float step = GameConstants.BurnPulsePeriod / GameConstants.BurnPulseSteps;
        for (int i = 0; i < 10; i++)
            enemy.TickStatuses(step / 20f);

        AssertThat(enemy.BurnPulseStep).IsEqual(0);
        AssertThat(enemy.IsBurning).IsTrue();
    }

    [TestCase]
    public void BurnTint_Trough_IsTheStaticBurnTint()
    {
        // Regression guard for the pulse: at step 0 the burning enemy must look exactly
        // like it did with the old static tint (base fill lerped 50% toward orange), so
        // the pulse only ever adds brightness, never changes the resting burn look.
        var fill = new Color(0.9f, 0.1f, 0.1f); // basic red
        var expected = fill.Lerp(new Color(1f, 0.5f, 0f, fill.A), 0.5f);

        var actual = Enemy.BurnTint(fill, 0);

        AssertThat(actual.R).IsEqualApprox(expected.R, 0.0001f);
        AssertThat(actual.G).IsEqualApprox(expected.G, 0.0001f);
        AssertThat(actual.B).IsEqualApprox(expected.B, 0.0001f);
        AssertThat(actual.A).IsEqual(fill.A);
    }

    [TestCase]
    public void BurnTint_ThrobsHotterThanTheTrough()
    {
        var fill = new Color(0.6f, 0.6f, 0.65f); // armored grey
        var trough = Enemy.BurnTint(fill, 0);

        bool anyStepDiffers = false;
        for (int step = 1; step < GameConstants.BurnPulseSteps; step++)
        {
            var pulsed = Enemy.BurnTint(fill, step);

            // Hotter = more of the yellow/orange tint, and never a transparent fill.
            AssertThat(pulsed.G).IsGreaterEqual(trough.G);
            AssertThat(pulsed.R).IsGreaterEqual(trough.R);
            AssertThat(pulsed.A).IsEqual(fill.A);

            if (pulsed.G > trough.G)
                anyStepDiffers = true;
        }

        // At least one step must actually differ, otherwise "pulsing" is a no-op
        // (e.g. if BurnPulseSteps or the hot color were ever tuned to match the trough).
        AssertThat(anyStepDiffers).IsTrue();
    }

    [TestCase]
    public void BurnTint_StepsAreDistinctEnoughToSee()
    {
        var fill = new Color(0.9f, 0.1f, 0.1f);
        var trough = Enemy.BurnTint(fill, 0);
        var peak = Enemy.BurnTint(fill, GameConstants.BurnPulseSteps / 2);

        float troughLuma = trough.R * 0.299f + trough.G * 0.587f + trough.B * 0.114f;
        float peakLuma = peak.R * 0.299f + peak.G * 0.587f + peak.B * 0.114f;

        // A visible throb, not a rounding-level flicker.
        AssertThat(peakLuma - troughLuma).IsGreaterEqual(0.1f);
    }

    [TestCase]
    public void BurnTint_ClampsOutOfRangeSteps()
    {
        var fill = new Color(0.9f, 0.1f, 0.1f);

        // A stale/out-of-range step must still paint a valid color, never throw.
        AssertThat(Enemy.BurnTint(fill, -3).R).IsEqual(Enemy.BurnTint(fill, 0).R);
        AssertThat(Enemy.BurnTint(fill, 99).R).IsEqual(Enemy.BurnTint(fill, GameConstants.BurnPulseSteps - 1).R);
    }

    [TestCase]
    public void BurnPulse_DoesNotChangeBurnDamage()
    {
        // The pulse is presentation: a burn ticked through many small frames must deal
        // exactly the same damage as one ticked through one big frame.
        var pulsed = new Enemy();
        pulsed.Configure(EnemyKind.Armored);
        pulsed.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        for (int i = 0; i < 125; i++)
            pulsed.TickStatuses(GameConstants.SkillLaserBurnDuration / 120f);

        var straight = new Enemy();
        straight.Configure(EnemyKind.Armored);
        straight.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
        straight.TickStatuses(GameConstants.SkillLaserBurnDuration);

        AssertThat(pulsed.CurrentHP).IsEqualApprox(straight.CurrentHP, 0.0001f);
        AssertThat(pulsed.IsBurning).IsFalse();
    }
}
