using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Death burst: kind-scaled shard counts/radii, the ease-out/fade envelope, and the
/// pooled lifecycle (reset on Play, exactly one Finished per play, no duplicate finish).
/// </summary>
[TestSuite]
public class DeathBurstEffectTest
{
    [TestCase]
    public void ShardCounts_MatchThePerKindContract()
    {
        AssertThat(DeathBurstEffect.ShardsFor(EnemyKind.Swarm)).IsEqual(4);
        AssertThat(DeathBurstEffect.ShardsFor(EnemyKind.Basic)).IsEqual(6);
        AssertThat(DeathBurstEffect.ShardsFor(EnemyKind.Armored)).IsEqual(8);
        AssertThat(DeathBurstEffect.ShardsFor(EnemyKind.Progenitor)).IsEqual(16);
    }

    [TestCase]
    public void BossBurst_IsAtLeastTwiceTheBasicRadius()
    {
        AssertThat(DeathBurstEffect.RadiusFor(EnemyKind.Progenitor))
            .IsGreaterEqual(DeathBurstEffect.RadiusFor(EnemyKind.Basic) * 2f);
        AssertThat(DeathBurstEffect.RadiusFor(EnemyKind.Basic))
            .IsGreater(DeathBurstEffect.RadiusFor(EnemyKind.Swarm));
    }

    [TestCase]
    public void Duration_IsInTheSpecifiedWindow()
    {
        AssertThat(GameConstants.DeathBurstDuration).IsGreaterEqual(0.3f);
        AssertThat(GameConstants.DeathBurstDuration).IsLessEqual(0.45f);
    }

    [TestCase]
    public void RadiusAt_ExpandsWithEaseOut_AndClamps()
    {
        AssertThat(DeathBurstEffect.RadiusAt(0f, 40f)).IsEqual(0f);
        AssertThat(DeathBurstEffect.RadiusAt(1f, 40f)).IsEqual(40f);
        AssertThat(DeathBurstEffect.RadiusAt(-2f, 40f)).IsEqual(0f);
        AssertThat(DeathBurstEffect.RadiusAt(3f, 40f)).IsEqual(40f);

        float previous = -1f;
        for (int i = 0; i <= 100; i++)
        {
            float radius = DeathBurstEffect.RadiusAt(i / 100f, 40f);
            AssertThat(radius).IsGreaterEqual(previous);
            AssertThat(radius).IsLessEqual(40f);
            previous = radius;
        }

        // Ease-out: more than half the radius is reached at the halfway point.
        AssertThat(DeathBurstEffect.RadiusAt(0.5f, 40f)).IsGreater(20f);
    }

    [TestCase]
    public void AlphaAt_FadesToTransparent()
    {
        AssertThat(DeathBurstEffect.AlphaAt(0f)).IsEqual(1f);
        AssertThat(DeathBurstEffect.AlphaAt(0.5f)).IsEqual(0.5f);
        AssertThat(DeathBurstEffect.AlphaAt(1f)).IsEqual(0f);
    }

    [TestCase]
    public void Play_AppliesKindScalingAndTheEnemyColor()
    {
        var effect = new DeathBurstEffect();
        try
        {
            var color = new Color(0.9f, 0.1f, 0.1f);

            effect.Play(new Vector2(120f, 64f), color, EnemyKind.Armored);

            AssertThat(effect.IsPlaying).IsTrue();
            AssertThat(effect.Position).IsEqual(new Vector2(120f, 64f));
            AssertThat(effect.ShardCount).IsEqual(8);
            AssertThat(effect.BurstRadius).IsEqual(DeathBurstEffect.RadiusFor(EnemyKind.Armored));
            AssertThat(effect.BurstColor).IsEqual(color);
            AssertThat(effect.Progress).IsEqual(0f);
        }
        finally
        {
            effect.Free();
        }
    }

    [TestCase]
    public void Play_ResetsEveryPerPlayField()
    {
        var effect = new DeathBurstEffect();
        try
        {
            effect.Play(Vector2.Zero, new Color(1f, 0f, 0f), EnemyKind.Progenitor);
            effect._Process(GameConstants.DeathBurstDuration);

            effect.Play(new Vector2(10f, 10f), new Color(0f, 1f, 0f), EnemyKind.Swarm);

            AssertThat(effect.IsPlaying).IsTrue();
            AssertThat(effect.Progress).IsEqual(0f);
            AssertThat(effect.ShardCount).IsEqual(4);
            AssertThat(effect.BurstRadius).IsEqual(DeathBurstEffect.RadiusFor(EnemyKind.Swarm));
            AssertThat(effect.BurstColor).IsEqual(new Color(0f, 1f, 0f));
            AssertThat(effect.Position).IsEqual(new Vector2(10f, 10f));
        }
        finally
        {
            effect.Free();
        }
    }

    [TestCase]
    public void Finished_IsEmittedExactlyOncePerPlay()
    {
        var effect = new DeathBurstEffect();
        NodeTestSupport.Attach(effect);
        try
        {
            int finished = 0;
            effect.Finished += _ => finished++;

            effect.Play(Vector2.Zero, Colors.White, EnemyKind.Basic);
            effect._Process(GameConstants.DeathBurstDuration * 0.5f);
            AssertThat(finished).IsEqual(0);
            AssertThat(effect.IsPlaying).IsTrue();

            effect._Process(GameConstants.DeathBurstDuration);
            AssertThat(finished).IsEqual(1);
            AssertThat(effect.IsPlaying).IsFalse();

            // Extra frames after the finish must not emit again.
            effect._Process(1f);
            effect._Process(1f);
            AssertThat(finished).IsEqual(1);

            // A second play animates again and emits exactly one more finish.
            effect.Play(Vector2.Zero, Colors.White, EnemyKind.Swarm);
            AssertThat(effect.IsPlaying).IsTrue();
            effect._Process(GameConstants.DeathBurstDuration + 0.1f);
            AssertThat(finished).IsEqual(2);
        }
        finally
        {
            NodeTestSupport.Detach(effect);
        }
    }

    [TestCase]
    public void InactiveEffect_DoesNotAdvanceOrFinish()
    {
        var effect = new DeathBurstEffect();
        NodeTestSupport.Attach(effect);
        try
        {
            int finished = 0;
            effect.Finished += _ => finished++;

            effect._Process(1f);

            AssertThat(effect.IsPlaying).IsFalse();
            AssertThat(finished).IsEqual(0);
            AssertThat(effect.Progress).IsEqual(0f);
        }
        finally
        {
            NodeTestSupport.Detach(effect);
        }
    }
}
