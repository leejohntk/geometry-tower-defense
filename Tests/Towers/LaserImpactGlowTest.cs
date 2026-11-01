using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Laser presentation: the emitter aims at its target, a firing pulse runs only while the
/// beam is active, and the impact glow follows the beam endpoint (plus chain endpoints),
/// hiding immediately when the target dies or the beam ends.
/// </summary>
[TestSuite]
public class LaserImpactGlowTest
{
    private static LaserTower BuildLaser(out Enemy enemy)
    {
        var laser = new LaserTower();
        laser.Initialize(5, 5);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(laser);

        enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = laser.Position + new Vector2(100f, 0f); // in range (3 cells = 192 px)
        return laser;
    }

    [TestCase]
    public void BeamActive_ShowsTheImpactGlowAtTheTargetEndpoint()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            laser.SetTarget(enemy);
            laser._Process(1f / 60f);

            AssertThat(laser.IsImpactGlowVisible).IsTrue();
            var glow = laser.GetNodeOrNull<Control>("ImpactGlow");
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue();
            AssertThat(glow.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            // The glow is centered on the target in tower-local space.
            Vector2 glowCenter = glow.Position + glow.Size / 2f;
            AssertThat(glowCenter.X).IsEqualApprox(enemy.Position.X - laser.Position.X, 0.001f);
            AssertThat(glowCenter.Y).IsEqualApprox(enemy.Position.Y - laser.Position.Y, 0.001f);
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }

    [TestCase]
    public void IdleLaser_NeverShowsTheGlow()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            laser.SetTarget(null);
            laser._Process(1f / 60f);
            AssertThat(laser.IsImpactGlowVisible).IsFalse();

            // Out-of-range target: the beam is not drawn, so no glow either.
            enemy.Position = laser.Position + new Vector2(laser.RangePixels + 50f, 0f);
            laser.SetTarget(enemy);
            laser._Process(1f / 60f);
            AssertThat(laser.IsImpactGlowVisible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }

    [TestCase]
    public void TargetDies_GlowHidesImmediately()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            laser.SetTarget(enemy);
            laser._Process(1f / 60f);
            AssertThat(laser.IsImpactGlowVisible).IsTrue();

            enemy.TakeDamage(1000f);
            laser._Process(1f / 60f);

            AssertThat(laser.IsImpactGlowVisible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }

    [TestCase]
    public void ChainGlows_FollowTheCurrentChain_AndHideWhenItShrinks()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            laser.SetTarget(enemy);
            laser.SetChainTargets(new List<Vector2>
            {
                laser.Position + new Vector2(120f, 20f),
                laser.Position + new Vector2(150f, -10f),
            });
            laser._Process(1f / 60f);

            AssertThat(laser.VisibleChainGlowCount).IsEqual(2);
            var first = laser.GetNodeOrNull<Control>("ChainImpactGlows/ChainImpact_0");
            AssertThat(first).IsNotNull();
            AssertThat(first!.Visible).IsTrue();

            // The chain shrinks: the extra ring must hide, not strand at a stale spot.
            laser.SetChainTargets(new List<Vector2> { laser.Position + new Vector2(120f, 20f) });
            laser._Process(1f / 60f);
            AssertThat(laser.VisibleChainGlowCount).IsEqual(1);

            laser.SetChainTargets(new List<Vector2>());
            laser._Process(1f / 60f);
            AssertThat(laser.VisibleChainGlowCount).IsEqual(0);
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }

    [TestCase]
    public void EmitterPulsesWhileBeamIsActive_AndResetsWhenItEnds()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            var body = laser.GetNodeOrNull<Control>("LaserEmitter");
            AssertThat(body).IsNotNull();

            laser.SetTarget(enemy);
            laser._Process(0.1f);
            AssertThat(body!.Scale.X).IsNotEqual(1f);

            laser.SetTarget(null);
            laser._Process(0.1f);
            AssertThat(body.Scale).IsEqual(Vector2.One);
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }

    [TestCase]
    public void EmitterPointer_ReflectsAim()
    {
        var laser = BuildLaser(out var enemy);
        try
        {
            // Target directly below: aim is +90 degrees, body rotation matches.
            enemy.Position = laser.Position + new Vector2(0f, 100f);
            laser.SetTarget(enemy);
            for (int i = 0; i < 120; i++)
                laser._Process(1f / 60f);

            AssertThat(laser.AimAngle).IsEqualApprox(Mathf.Pi / 2f, 0.0001f);
            var body = laser.GetNodeOrNull<Control>("LaserEmitter");
            AssertThat(body!.Rotation).IsEqualApprox(laser.AimAngle - laser.DefaultAimAngle, 0.0001f);
        }
        finally
        {
            NodeTestSupport.Cleanup(laser);
            enemy.Free();
        }
    }
}
