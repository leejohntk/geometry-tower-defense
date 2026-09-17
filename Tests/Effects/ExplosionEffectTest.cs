using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure-logic tests for the cannon explosion effect's size/alpha math.
/// No Godot runtime needed — these only exercise static pure functions.
/// </summary>
[TestSuite]
public class ExplosionEffectTest
{
    [TestCase]
    public void RadiusAt_StartsAtZero_AndEndsAtMaxRadius()
    {
        AssertThat(ExplosionEffect.RadiusAt(0f, 64f)).IsEqual(0f);
        AssertThat(ExplosionEffect.RadiusAt(1f, 64f)).IsEqual(64f);
    }

    [TestCase]
    public void RadiusAt_LandsExactlyOnAoeRadius_SoVisualNeverDivergesFromDamage()
    {
        // The whole point of the effect: the player must see the true damage extent.
        AssertThat(ExplosionEffect.RadiusAt(1f, GameConstants.CannonTowerAoeRadius))
            .IsEqual(GameConstants.CannonTowerAoeRadius);
    }

    [TestCase]
    public void CannonProjectile_ExplosionRadius_MatchesAoeConstant()
    {
        // Second half of the size-match guarantee: this pins the projectile's damage
        // radius to the constant, and GameManager now passes that same value to the
        // visual. If a float scale factor or rounding is ever introduced, this would
        // need to relax to an epsilon compare — keep it exact for now.
        AssertThat(new CannonProjectile().ExplosionRadius)
            .IsEqual(GameConstants.CannonTowerAoeRadius);
    }

    [TestCase]
    public void RadiusAt_IsMonotonicNonDecreasing()
    {
        float previous = -1f;
        for (int i = 0; i <= 100; i++)
        {
            float progress = i / 100f;
            float radius = ExplosionEffect.RadiusAt(progress, 64f);

            AssertThat(radius).IsGreaterEqual(previous);
            AssertThat(radius).IsLessEqual(64f);

            previous = radius;
        }
    }

    [TestCase]
    public void RadiusAt_HoldsFullRadiusThroughFadeOut()
    {
        // Expansion completes at the halfway point, then holds while alpha fades.
        AssertThat(ExplosionEffect.RadiusAt(0.5f, 64f)).IsEqual(64f);
        AssertThat(ExplosionEffect.RadiusAt(0.75f, 64f)).IsEqual(64f);
    }

    [TestCase]
    public void AlphaAt_StartsOpaque_AndEndsTransparent()
    {
        AssertThat(ExplosionEffect.AlphaAt(0f)).IsEqual(1f);
        AssertThat(ExplosionEffect.AlphaAt(1f)).IsEqual(0f);
    }

    [TestCase]
    public void AlphaAt_HoldsOpaqueForFirstHalf_ThenFades()
    {
        AssertThat(ExplosionEffect.AlphaAt(0.25f)).IsEqual(1f);
        AssertThat(ExplosionEffect.AlphaAt(0.5f)).IsEqual(1f);
        AssertThat(ExplosionEffect.AlphaAt(0.75f)).IsEqual(0.5f);
    }

    [TestCase]
    public void RadiusAt_AndAlphaAt_ClampOutOfRangeProgress()
    {
        // Out-of-range progress is clamped, never extrapolated or NaN.
        AssertThat(ExplosionEffect.RadiusAt(-1f, 64f)).IsEqual(0f);
        AssertThat(ExplosionEffect.RadiusAt(2f, 64f)).IsEqual(64f);
        AssertThat(ExplosionEffect.AlphaAt(-1f)).IsEqual(1f);
        AssertThat(ExplosionEffect.AlphaAt(2f)).IsEqual(0f);
    }
}
