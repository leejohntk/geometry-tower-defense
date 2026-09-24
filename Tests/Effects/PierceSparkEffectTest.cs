using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure-logic tests for the pierce spark's size/alpha math and for the property that
/// separates it from the cannon explosion. No Godot runtime needed — these only
/// exercise static pure functions and constants.
/// </summary>
[TestSuite]
public class PierceSparkEffectTest
{
    [TestCase]
    public void RadiusAt_StartsAtZero_AndEndsAtFullRadius()
    {
        AssertThat(PierceSparkEffect.RadiusAt(0f)).IsEqual(0f);
        AssertThat(PierceSparkEffect.RadiusAt(1f)).IsEqual(GameConstants.PierceSparkRadius);
    }

    [TestCase]
    public void RadiusAt_IsMonotonicNonDecreasing_AndBounded()
    {
        float previous = -1f;
        for (int i = 0; i <= 100; i++)
        {
            float radius = PierceSparkEffect.RadiusAt(i / 100f);

            AssertThat(radius).IsGreaterEqual(previous);
            AssertThat(radius).IsLessEqual(GameConstants.PierceSparkRadius);

            previous = radius;
        }
    }

    [TestCase]
    public void AlphaAt_StartsOpaque_AndEndsTransparent()
    {
        AssertThat(PierceSparkEffect.AlphaAt(0f)).IsEqual(1f);
        AssertThat(PierceSparkEffect.AlphaAt(1f)).IsEqual(0f);
    }

    [TestCase]
    public void AlphaAt_IsMonotonicNonIncreasing()
    {
        float previous = 2f;
        for (int i = 0; i <= 100; i++)
        {
            float alpha = PierceSparkEffect.AlphaAt(i / 100f);

            AssertThat(alpha).IsLessEqual(previous);
            AssertThat(alpha).IsGreaterEqual(0f);

            previous = alpha;
        }
    }

    [TestCase]
    public void RadiusAt_ClampsOutOfRangeProgress()
    {
        AssertThat(PierceSparkEffect.RadiusAt(-5f)).IsEqual(0f);
        AssertThat(PierceSparkEffect.RadiusAt(17f)).IsEqual(GameConstants.PierceSparkRadius);
        AssertThat(PierceSparkEffect.AlphaAt(-5f)).IsEqual(1f);
        AssertThat(PierceSparkEffect.AlphaAt(17f)).IsEqual(0f);
    }

    [TestCase]
    public void Spark_IsVisiblySmaller_AndShorter_ThanTheCannonExplosion()
    {
        // The whole point of a separate effect: a pierced hit must not read as a splash.
        AssertThat(GameConstants.PierceSparkRadius).IsLess(GameConstants.CannonTowerAoeRadius);
        AssertThat(GameConstants.PierceSparkDuration).IsLess(GameConstants.CannonExplosionDuration);
    }

    [TestCase]
    public void Spark_HasSpokesToDraw()
    {
        // A zero/negative spoke count would draw nothing at all.
        AssertThat(GameConstants.PierceSparkSpokes).IsGreater(0);
        AssertThat(GameConstants.PierceSparkStrokeWidth).IsGreater(0f);
        AssertThat(GameConstants.PierceSparkInnerRadiusRatio).IsGreaterEqual(0f);
        AssertThat(GameConstants.PierceSparkInnerRadiusRatio).IsLessEqual(1f);
    }
}
