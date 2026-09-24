using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure mechanic math and the laser beam path: per-rank values, chain falloff/range/
/// no-double-hit, ramp-up curve + reset, ignite rolls bounded by elapsed seconds, and
/// a no-per-frame-allocation assertion for the status/beam hot path.
/// </summary>
[TestSuite]
public class SkillMechanicsTest
{
    [TestCase]
    public void ArrowMechanics_Rank0AndRank5()
    {
        AssertThat(SkillMechanics.ArrowPierceCount(0)).IsEqual(0);
        AssertThat(SkillMechanics.ArrowPierceCount(5)).IsEqual(5);
        AssertThat(SkillMechanics.ArrowCritChance(0)).IsEqual(0f);
        AssertThat(SkillMechanics.ArrowCritChance(5)).IsEqualApprox(0.4f, 0.0001f);
    }

    [TestCase]
    public void CannonMechanics_Rank0AndRank5()
    {
        AssertThat(SkillMechanics.CannonClusterCount(0)).IsEqual(1);
        AssertThat(SkillMechanics.CannonClusterCount(5)).IsEqual(6);
        AssertThat(SkillMechanics.CannonStunChance(0)).IsEqual(0f);
        AssertThat(SkillMechanics.CannonStunChance(5)).IsEqualApprox(0.3f, 0.0001f);
    }

    [TestCase]
    public void LaserMechanics_Rank0AndRank5()
    {
        AssertThat(SkillMechanics.LaserIgniteChancePerSecond(0)).IsEqual(0f);
        AssertThat(SkillMechanics.LaserIgniteChancePerSecond(5)).IsEqualApprox(0.25f, 0.0001f);
        AssertThat(SkillMechanics.LaserChainJumps(0)).IsEqual(0);
        AssertThat(SkillMechanics.LaserChainJumps(5)).IsEqual(5);
        AssertThat(SkillMechanics.LaserRampMaxMultiplier(0)).IsEqual(1f);
        AssertThat(SkillMechanics.LaserRampMaxMultiplier(5)).IsEqualApprox(2.0f, 0.0001f);
    }

    [TestCase]
    public void Towers_ExposeMechanicValues_AtRank5()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowPierce, 5);
        state.SetRank(SkillTreeCatalog.ArrowCritChance, 5);
        state.SetRank(SkillTreeCatalog.CannonCluster, 5);
        state.SetRank(SkillTreeCatalog.CannonStunChance, 5);
        state.SetRank(SkillTreeCatalog.LaserIgnite, 5);
        state.SetRank(SkillTreeCatalog.LaserChain, 5);
        state.SetRank(SkillTreeCatalog.LaserRampUp, 5);

        var arrow = new ArrowTower();
        arrow.SetSkillTree(state);
        AssertThat(arrow.PierceCount).IsEqual(5);
        AssertThat(arrow.CritChance).IsEqualApprox(0.4f, 0.0001f);

        var cannon = new CannonTower();
        cannon.SetSkillTree(state);
        AssertThat(cannon.ClusterCount).IsEqual(6);
        AssertThat(cannon.StunChance).IsEqualApprox(0.3f, 0.0001f);

        var laser = new LaserTower();
        laser.SetSkillTree(state);
        AssertThat(laser.ChainJumps).IsEqual(5);
        AssertThat(laser.IgniteChancePerSecond).IsEqualApprox(0.25f, 0.0001f);
        AssertThat(laser.RampMaxMultiplier).IsEqualApprox(2.0f, 0.0001f);
    }

    [TestCase]
    public void LaserChain_JumpDamage_FallsOffSixtyPercentPerJump()
    {
        AssertThat(LaserChain.JumpDamage(100f, 0)).IsEqualApprox(60f, 0.001f);
        AssertThat(LaserChain.JumpDamage(100f, 1)).IsEqualApprox(36f, 0.001f);
        AssertThat(LaserChain.JumpDamage(100f, 2)).IsEqualApprox(21.6f, 0.001f);
    }

    [TestCase]
    public void LaserChain_FindNextTarget_OnlyPicksWithinRange()
    {
        var from = new Enemy();
        from.Configure(EnemyKind.Basic);
        from.Position = new Vector2(0, 0);

        var inRange = new Enemy();
        inRange.Configure(EnemyKind.Basic);
        inRange.Position = new Vector2(GameConstants.CellDistanceInPixels(GameConstants.SkillLaserChainRange) - 1f, 0);

        var outOfRange = new Enemy();
        outOfRange.Configure(EnemyKind.Basic);
        outOfRange.Position = new Vector2(GameConstants.CellDistanceInPixels(GameConstants.SkillLaserChainRange) + 1f, 0);

        var candidates = new List<Enemy> { inRange, outOfRange };
        var alreadyHit = new List<Enemy> { from };

        AssertThat(LaserChain.FindNextTarget(from, candidates, alreadyHit)).IsEqual(inRange);
    }

    [TestCase]
    public void LaserChain_FindNextTarget_NeverPicksAlreadyHitEnemy()
    {
        var from = new Enemy();
        from.Configure(EnemyKind.Basic);
        from.Position = new Vector2(0, 0);

        var near = new Enemy();
        near.Configure(EnemyKind.Basic);
        near.Position = new Vector2(30, 0);

        var far = new Enemy();
        far.Configure(EnemyKind.Basic);
        far.Position = new Vector2(60, 0);

        var candidates = new List<Enemy> { near, far };
        // "near" is the closest but has already been hit this chain, so "far" is chosen.
        var alreadyHit = new List<Enemy> { from, near };

        AssertThat(LaserChain.FindNextTarget(from, candidates, alreadyHit)).IsEqual(far);
    }

    [TestCase]
    public void Laser_RampUp_RampsToCap_AndResetsOnTargetSwitch()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserRampUp, 5);
        var laser = new LaserTower();
        laser.SetSkillTree(state);

        var a = new Enemy();
        a.Configure(EnemyKind.Basic);
        var b = new Enemy();
        b.Configure(EnemyKind.Basic);

        // Fresh contact starts at the base 1.0x multiplier.
        AssertThat(laser.UpdateRamp(a, 0f)).IsEqual(1f);

        float half = laser.UpdateRamp(a, GameConstants.SkillLaserRampTime / 2f);
        float full = laser.UpdateRamp(a, GameConstants.SkillLaserRampTime / 2f);
        float over = laser.UpdateRamp(a, 10f);

        AssertThat(half > 1f).IsTrue();
        AssertThat(full > half).IsTrue();
        AssertThat(full).IsEqual(laser.RampMaxMultiplier);
        AssertThat(over).IsEqual(laser.RampMaxMultiplier);

        // Switching targets resets the ramp to the base multiplier.
        AssertThat(laser.UpdateRamp(b, 0f)).IsEqual(1f);
    }

    [TestCase]
    public void Laser_RampAndIgnite_Reset_WhenPooledEnemyIsReissued()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserRampUp, 5);
        var laser = new LaserTower();
        laser.SetSkillTree(state);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        // First contact starts at 1.0x, then ramps to the cap.
        AssertThat(laser.UpdateRamp(enemy, 0f)).IsEqual(1f);
        AssertThat(laser.UpdateRamp(enemy, GameConstants.SkillLaserRampTime)).IsEqual(laser.RampMaxMultiplier);

        // Accumulate half a second of ignite progress (not yet a full second).
        AssertThat(laser.ConsumeIgniteRolls(0.5f)).IsEqual(0);

        // The LIFO pool hands the *same reference* back out as a brand-new enemy.
        enemy.ResetForPool();
        enemy.Configure(EnemyKind.Basic);

        // A new enemy must start at 1.0x, and the partial ignite progress must be
        // discarded — not inherited through the pooled identity.
        AssertThat(laser.UpdateRamp(enemy, 0f)).IsEqual(1f);
        AssertThat(laser.ConsumeIgniteRolls(0.5f)).IsEqual(0);
    }

    [TestCase]
    public void Laser_IgniteRolls_AreBoundedByElapsedSeconds_NotFrameCount()
    {
        // Four 0.25s frames = 1.0s of contact = exactly one roll (not four).
        var laser = new LaserTower();
        int total = 0;
        for (int i = 0; i < 4; i++)
            total += laser.ConsumeIgniteRolls(0.25f);
        AssertThat(total).IsEqual(1);

        // A single 1.0s frame also produces exactly one roll.
        var single = new LaserTower();
        AssertThat(single.ConsumeIgniteRolls(1.0f)).IsEqual(1);

        // Two full seconds split across frames produce exactly two rolls.
        var multi = new LaserTower();
        int total2 = 0;
        for (int i = 0; i < 8; i++)
            total2 += multi.ConsumeIgniteRolls(0.25f);
        AssertThat(total2).IsEqual(2);
    }

    [TestCase]
    public void StatusAndBeamHotPath_DoesNotAllocate()
    {
        // Inputs built once, outside the measured loop.
        // A small fleet of burning enemies plus a snapshot list reused exactly like
        // GameManager._statusSnapshot (clear + refill each pass).
        var snapshot = new List<Enemy>();
        var statusWarmup = BuildStatusFleet();
        var statusMeasured = BuildStatusFleet();

        var laser = new LaserTower();
        var target = new Enemy();
        target.Configure(EnemyKind.Basic);

        var candidates = new List<Enemy>();
        for (int i = 0; i < 6; i++)
        {
            var enemy = new Enemy();
            enemy.Configure(EnemyKind.Basic);
            enemy.Position = new Vector2(i * 50f, 0f);
            candidates.Add(enemy);
        }
        var alreadyHit = new List<Enemy> { target };

        // A beam laser with its visuals initialized so the measured loop exercises
        // the real chain-beam buffer update path, not the null-guard early return.
        var beamLaser = new LaserTower();
        beamLaser._Ready();
        var beamTarget = new Enemy();
        beamTarget.Configure(EnemyKind.Basic);
        beamLaser.SetTarget(beamTarget);
        var chainPositions = new List<Vector2>
        {
            new Vector2(10f, 0f), new Vector2(20f, 0f), new Vector2(30f, 0f),
            new Vector2(40f, 0f), new Vector2(50f, 0f)
        };

        // Warm up the JIT on the exact same code paths. 500 burn ticks at 1/120s is
        // ~4.17s of burn (~8.3 damage) — safely below the 14 HP armored pool so no
        // enemy dies mid-loop.
        for (int i = 0; i < 500; i++)
        {
            TickFleet(snapshot, statusWarmup, 1f / 120f);
            laser.UpdateRamp(target, 1f / 60f);
            laser.ConsumeIgniteRolls(1f / 60f);
            LaserChain.FindNextTarget(target, candidates, alreadyHit);
            beamLaser.SetChainTargets(chainPositions);
            beamLaser._Process(1f / 60f);
        }

        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        long before = System.GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 500; i++)
        {
            TickFleet(snapshot, statusMeasured, 1f / 120f);
            laser.UpdateRamp(target, 1f / 60f);
            laser.ConsumeIgniteRolls(1f / 60f);
            LaserChain.FindNextTarget(target, candidates, alreadyHit);
            beamLaser.SetChainTargets(chainPositions);
            beamLaser._Process(1f / 60f);
        }

        long after = System.GC.GetAllocatedBytesForCurrentThread();

        // The status pass (snapshot + multi-enemy TickStatuses, including the burn
        // damage path) and the beam path (ramp, ignite accumulator, chain selection,
        // SetChainTargets, and the chain-beam buffer update) must allocate nothing per
        // tick: cached lists, index-based iteration, and no LINQ/closures.
        // NOTE: an enemy-death tick DOES allocate (EmitSignal(Destroyed) marshals a
        // Variant) by design — that is a discrete event, not a steady-state per-frame
        // event, so this loop keeps every enemy alive rather than weakening the
        // assertion to tolerate it.
        AssertThat(after == before).IsTrue();
    }

    private static List<Enemy> BuildStatusFleet()
    {
        var fleet = new List<Enemy>();
        for (int i = 0; i < 4; i++)
        {
            var enemy = new Enemy();
            enemy.Configure(EnemyKind.Armored);
            enemy.ApplyBurn(GameConstants.SkillLaserBurnDps, GameConstants.SkillLaserBurnDuration);
            fleet.Add(enemy);
        }
        return fleet;
    }

    private static void TickFleet(List<Enemy> snapshot, List<Enemy> fleet, float delta)
    {
        snapshot.Clear();
        for (int i = 0; i < fleet.Count; i++)
            snapshot.Add(fleet[i]);
        for (int i = 0; i < snapshot.Count; i++)
            snapshot[i].TickStatuses(delta);
    }
}
