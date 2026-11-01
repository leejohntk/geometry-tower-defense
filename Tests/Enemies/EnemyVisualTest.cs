using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Presentation tests for enemy silhouettes, idle transform motion, the normal-hit
/// pulse, and pooled visual-reset safety. These drive the real nodes (added to the
/// scene tree so _Ready builds the visual children) but never assert on gameplay.
/// </summary>
[TestSuite]
public class EnemyVisualTest
{
    [TestCase]
    public void SilhouetteProfiles_UseDistinctSideCounts()
    {
        // 0 = circle, 3 = triangle, 4 = diamond, 8 = octagon.
        AssertThat(EnemySilhouette.SidesFor(EnemyKind.Basic)).IsEqual(0);
        AssertThat(EnemySilhouette.SidesFor(EnemyKind.Swarm)).IsEqual(3);
        AssertThat(EnemySilhouette.SidesFor(EnemyKind.Armored)).IsEqual(4);
        AssertThat(EnemySilhouette.SidesFor(EnemyKind.Progenitor)).IsEqual(8);
    }

    [TestCase]
    public void SilhouetteProfiles_ConfigureIdleMotionAndDetailFlags()
    {
        var basic = EnemySilhouette.For(EnemyKind.Basic);
        AssertThat(basic.PulseAmplitude).IsGreater(0f);
        AssertThat(basic.SpinSpeed).IsEqual(0f);
        AssertThat(basic.InnerRing).IsTrue();

        var swarm = EnemySilhouette.For(EnemyKind.Swarm);
        AssertThat(swarm.SpinSpeed).IsGreater(0f);
        AssertThat(swarm.PulseAmplitude).IsEqual(0f);

        var armored = EnemySilhouette.For(EnemyKind.Armored);
        AssertThat(armored.SpinSpeed).IsEqual(0f);
        AssertThat(armored.PulseAmplitude).IsEqual(0f);
        AssertThat(armored.InsetBorder).IsTrue();
        AssertThat(armored.Rivets).IsTrue();

        var boss = EnemySilhouette.For(EnemyKind.Progenitor);
        AssertThat(boss.PhaseBorder).IsTrue();
        AssertThat(boss.OuterRing).IsTrue();

        // Orientation is an explicit profile field, not inferred from PhaseBorder.
        AssertThat(basic.StartAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(swarm.StartAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(armored.StartAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(boss.StartAngle).IsEqual(0f);
    }

    [TestCase]
    public void BasicEnemy_PulsesByTransform()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Basic);

            enemy._Process(0.2);

            AssertThat(enemy.BodyScale.X).IsNotEqual(1f);
            AssertThat(enemy.BodyScale.X).IsGreater(0.9f);
            AssertThat(enemy.BodyScale.X).IsLess(1.1f);
            // Circle silhouette: no spin.
            AssertThat(enemy.BodyRotation).IsEqual(0f);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void SwarmEnemy_SpinsByTransform()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Swarm);
            float before = enemy.BodyRotation;

            enemy._Process(0.5);

            AssertThat(enemy.BodyRotation).IsGreater(before);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void ArmoredEnemy_IsStatelessButStillUsesTheTransformPath()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Armored);
            enemy._Process(0.5);
            enemy._Process(0.5);

            AssertThat(enemy.BodyRotation).IsEqual(0f);
            AssertThat(enemy.BodyScale).IsEqual(Vector2.One);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void ProgenitorOuterRing_CounterRotates()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Progenitor);
            float before = enemy.OuterRingRotation;

            enemy._Process(0.5);

            AssertThat(enemy.OuterRingRotation).IsLess(before);
            // The boss body itself does not spin (only the ring does).
            AssertThat(enemy.BodyRotation).IsEqual(0f);
            var ring = enemy.GetNodeOrNull<Control>("CircleContainer/OuterRing");
            AssertThat(ring).IsNotNull();
            AssertThat(ring!.Visible).IsTrue();
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void ProgenitorOverlay_RendersAfterTheBodyAndOuterRing()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Progenitor);

            var container = enemy.GetNode<Control>("CircleContainer");
            var body = container.GetNode<Control>("Body");
            var ring = container.GetNode<Control>("OuterRing");
            var overlay = container.GetNode<Control>("Overlay");

            AssertThat(overlay.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(overlay.Visible).IsTrue();
            // Added last, so the un-rotated health bar / telegraph composites above the
            // silhouette instead of being covered by it.
            AssertThat(overlay.GetIndex()).IsGreater(body.GetIndex());
            AssertThat(overlay.GetIndex()).IsGreater(ring.GetIndex());
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void NonBossEnemies_HideTheOverlay()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Armored);
            var overlay = enemy.GetNode<Control>("CircleContainer/Overlay");
            AssertThat(overlay.Visible).IsFalse();

            enemy.Configure(EnemyKind.Progenitor);
            AssertThat(enemy.GetNode<Control>("CircleContainer/Overlay").Visible).IsTrue();

            enemy.Configure(EnemyKind.Basic);
            AssertThat(enemy.GetNode<Control>("CircleContainer/Overlay").Visible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void VisualAnimation_NeverMovesTheSimulationPosition()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Swarm);
            enemy.SetPath(new System.Collections.Generic.List<Vector2>
            {
                new Vector2(32, 32),
                new Vector2(96, 32)
            });
            // Stun pauses movement; the visual animation still runs.
            enemy.ApplyStun(5f);
            Vector2 positionBefore = enemy.Position;

            enemy._Process(0.5);

            AssertThat(enemy.Position).IsEqual(positionBefore);
            AssertThat(enemy.BodyRotation).IsNotEqual(0f);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void ResetForPool_ClearsSilhouetteSpinPulseAndRing()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Progenitor);
            enemy._Process(0.5);
            AssertThat(enemy.BodyRotation).IsEqual(0f);
            AssertThat(enemy.OuterRingRotation).IsNotEqual(0f);

            enemy.ResetForPool();

            AssertThat(enemy.Kind).IsEqual(EnemyKind.Basic);
            AssertThat(enemy.Silhouette.Sides).IsEqual(0);
            AssertThat(enemy.BodyRotation).IsEqual(0f);
            AssertThat(enemy.BodyScale).IsEqual(Vector2.One);
            AssertThat(enemy.OuterRingRotation).IsEqual(0f);
            AssertThat(enemy.GetNode<Control>("CircleContainer/Overlay").Visible).IsFalse();

            // Reuse as a swarm must start from a clean spin angle and scale.
            enemy.Configure(EnemyKind.Swarm);
            AssertThat(enemy.BodyRotation).IsEqual(0f);
            AssertThat(enemy.BodyScale).IsEqual(Vector2.One);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void NormalHit_ArmsAShortPulse_AndCritFlashStaysStrongerAndLonger()
    {
        var enemy = new Enemy();
        NodeTestSupport.Attach(enemy);
        try
        {
            enemy.Configure(EnemyKind.Basic);

            enemy.TakeDamage(1f);

            AssertThat(enemy.HitPulseRemaining).IsEqual(GameConstants.EnemyHitPulseDuration);
            AssertThat(GameConstants.EnemyHitPulseDuration).IsLess(GameConstants.HitFlashDuration);
            AssertThat(GameConstants.EnemyHitPulseStrength).IsLess(0.6f);
        }
        finally
        {
            NodeTestSupport.Detach(enemy);
        }
    }

    [TestCase]
    public void NormalHitPulse_ExpiresThroughTheStatusPass()
    {
        var enemy = new Enemy();
        try
        {
            enemy.Configure(EnemyKind.Basic);
            enemy.TakeDamage(1f);
            AssertThat(enemy.HitPulseRemaining).IsGreater(0f);

            enemy.TickStatuses(GameConstants.EnemyHitPulseDuration + 0.001f);

            AssertThat(enemy.HitPulseRemaining).IsEqual(0f);
        }
        finally
        {
            enemy.Free();
        }
    }

    [TestCase]
    public void CritFlash_IsNotDowngradedByTheNormalHitPulse()
    {
        var enemy = new Enemy();
        try
        {
            enemy.Configure(EnemyKind.Basic);

            // ArrowProjectile applies the crit flash before TakeDamage; the normal
            // pulse must not overwrite the stronger crit cue.
            enemy.ApplyHitFlash();
            enemy.TakeDamage(1f);

            AssertThat(enemy.HitPulseRemaining).IsEqual(0f);
        }
        finally
        {
            enemy.Free();
        }
    }

    [TestCase]
    public void BurnDotTicks_DoNotStrobeTheNormalHitPulse()
    {
        var enemy = new Enemy();
        try
        {
            enemy.Configure(EnemyKind.Basic);
            enemy.ApplyBurn(2f, 2f);

            enemy.TickStatuses(0.1f);

            AssertThat(enemy.IsBurning).IsTrue();
            AssertThat(enemy.HitPulseRemaining).IsEqual(0f);
            AssertThat(enemy.BurnFlashRemaining).IsGreater(0f);
        }
        finally
        {
            enemy.Free();
        }
    }

    [TestCase]
    public void HitFeedback_DoesNotChangeDamageOrArmor()
    {
        var enemy = new Enemy();
        try
        {
            enemy.Configure(EnemyKind.Armored);

            enemy.TakeDamage(GameConstants.ArrowTowerDamage);

            // 10 damage - 5 armor = 5 applied; the presentation cue changes nothing.
            AssertThat(enemy.CurrentHP).IsEqual(14f - 5f);
            AssertThat(enemy.Armor).IsEqual(GameConstants.ArmoredEnemyArmor);
            AssertThat(enemy.HitPulseRemaining).IsEqual(GameConstants.EnemyHitPulseDuration);
        }
        finally
        {
            enemy.Free();
        }
    }

    [TestCase]
    public void FillColor_IsExposedForTheDeathBurst()
    {
        var enemy = new Enemy();
        try
        {
            enemy.Configure(EnemyKind.Swarm);
            AssertThat(enemy.FillColor).IsEqual(new Color(1f, 0.55f, 0.1f));

            enemy.Configure(EnemyKind.Progenitor);
            AssertThat(enemy.FillColor).IsEqual(GameConstants.ProgenitorFillColor);
        }
        finally
        {
            enemy.Free();
        }
    }
}
