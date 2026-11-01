using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Cannon explosion extra detail and lifecycle: the new inner shockwave ring and debris
/// spokes stay inside the unchanged RadiusAt/AlphaAt damage-extent envelope, and the
/// pooled effect still emits Finished exactly once per play.
/// </summary>
[TestSuite]
public class ExplosionDetailTest
{
    [TestCase]
    public void ExplosionDetailConstants_StayInsideTheBlastEnvelope()
    {
        AssertThat(GameConstants.CannonExplosionSpokes).IsEqual(6);
        AssertThat(GameConstants.CannonExplosionInnerRingScale).IsGreater(0f);
        AssertThat(GameConstants.CannonExplosionInnerRingScale).IsLess(1f);
        AssertThat(GameConstants.CannonExplosionSpokeInnerRatio).IsGreaterEqual(0f);
        AssertThat(GameConstants.CannonExplosionSpokeInnerRatio).IsLess(1f);
        AssertThat(GameConstants.CannonExplosionSpokeLengthRatio).IsGreaterEqual(1f);
        AssertThat(GameConstants.CannonExplosionSpokeWidth).IsGreater(0f);
    }

    [TestCase]
    public void DamageExtentContract_IsUnchanged()
    {
        // The visual still lands exactly on the AoE radius at progress 0.5 and holds.
        AssertThat(ExplosionEffect.RadiusAt(0.5f, GameConstants.CannonTowerAoeRadius))
            .IsEqual(GameConstants.CannonTowerAoeRadius);
        AssertThat(ExplosionEffect.RadiusAt(0.9f, GameConstants.CannonTowerAoeRadius))
            .IsEqual(GameConstants.CannonTowerAoeRadius);
        AssertThat(ExplosionEffect.AlphaAt(0.5f)).IsEqual(1f);
        AssertThat(ExplosionEffect.AlphaAt(1f)).IsEqual(0f);
        AssertThat(GameConstants.CannonExplosionDuration).IsEqual(0.4f);
    }

    [TestCase]
    public void Explosion_FinishesOnce_AndPlayResetsTheClock()
    {
        var effect = new ExplosionEffect();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(effect);
        try
        {
            int finished = 0;
            effect.Finished += _ => finished++;

            effect.Play(new Vector2(32f, 32f), GameConstants.CannonTowerAoeRadius);
            AssertThat(effect.Position).IsEqual(new Vector2(32f, 32f));
            AssertThat(effect.Progress).IsEqual(0f);

            effect._Process(GameConstants.CannonExplosionDuration + 0.001f);
            AssertThat(finished).IsEqual(1);

            effect.Play(Vector2.Zero, GameConstants.CannonTowerAoeRadius);
            AssertThat(effect.Progress).IsEqual(0f);
            effect._Process(GameConstants.CannonExplosionDuration + 0.001f);
            AssertThat(finished).IsEqual(2);
        }
        finally
        {
            effect.GetParent()?.RemoveChild(effect);
            effect.QueueFree();
        }
    }

    [TestCase]
    public void PierceSpark_RemainsVisiblySmallerThanTheExplosion()
    {
        AssertThat(GameConstants.PierceSparkRadius).IsLess(GameConstants.CannonTowerAoeRadius);
        AssertThat(GameConstants.PierceSparkDuration).IsLess(GameConstants.CannonExplosionDuration);
    }
}
