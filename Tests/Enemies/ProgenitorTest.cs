using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Progenitor boss rules: stats, phase thresholds, summon cadence/squad size,
/// weighted composition, windup behavior, lead placement, and full pool reset.
/// </summary>
[TestSuite]
public class ProgenitorTest
{
    // === Stats and configuration ===

    [TestCase]
    public void Progenitor_HasBossStats_ToughestAndSlowestEnemy()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);

        AssertThat(boss.Kind).IsEqual(EnemyKind.Progenitor);
        AssertThat(boss.IsProgenitor).IsTrue();
        AssertThat(boss.CurrentHP).IsEqual((float)GameConstants.ProgenitorHP);
        AssertThat(boss.Armor).IsEqual(GameConstants.ProgenitorArmor);
        AssertThat(boss.Speed).IsEqual(GameConstants.ProgenitorSpeed);
        AssertThat(boss.Diameter).IsEqual((float)GameConstants.ProgenitorDiameter);
        AssertThat(boss.CollisionRadius).IsEqual(GameConstants.ProgenitorDiameter / 2f);
        AssertThat(boss.CoinDrop).IsEqual(GameConstants.ProgenitorCoinDrop);
        AssertThat(boss.BaseDamage).IsEqual(GameConstants.ProgenitorBaseDamage);

        // Tougher than every normal kind.
        AssertThat(GameConstants.ProgenitorHP > GameConstants.ArmoredEnemyHP).IsTrue();
        AssertThat(GameConstants.ProgenitorHP > GameConstants.EnemyHP).IsTrue();
        AssertThat(GameConstants.ProgenitorHP > GameConstants.SwarmEnemyHP).IsTrue();

        // Slower than every normal kind.
        AssertThat(GameConstants.ProgenitorSpeed < GameConstants.EnemySpeed).IsTrue();
        AssertThat(GameConstants.ProgenitorSpeed < GameConstants.ArmoredEnemySpeed).IsTrue();
        AssertThat(GameConstants.ProgenitorSpeed < GameConstants.SwarmEnemySpeed).IsTrue();
    }

    [TestCase]
    public void NormalEnemies_HaveNoSummonState_AndCostOneBaseDamage()
    {
        foreach (var kind in new[] { EnemyKind.Basic, EnemyKind.Swarm, EnemyKind.Armored })
        {
            var enemy = new Enemy();
            enemy.Configure(kind);

            AssertThat(enemy.IsProgenitor).IsFalse();
            AssertThat(enemy.SummonCountdown).IsEqual(0f);
            AssertThat(enemy.IsSummoning).IsFalse();
            AssertThat(enemy.PendingSummonCount).IsEqual(0);
            AssertThat(enemy.BaseDamage).IsEqual(1);

            // Summon ticking is a no-op on a normal enemy.
            enemy.TickProgenitorSummon(100f);
            AssertThat(enemy.IsSummoning).IsFalse();
        }
    }

    [TestCase]
    public void Progenitor_StartsWithPhase1Cadence_AndNeverSummonsOnSpawn()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);

        AssertThat(boss.SummonPhase).IsEqual(1);
        AssertThat(boss.SummonCountdown).IsEqual(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);
        AssertThat(boss.IsProgenitor).IsTrue();

        // Just short of the first interval: still no windup.
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval - 0.01f);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);

        // Crossing the interval starts the windup; the squad spawns only at its end.
        boss.TickProgenitorSummon(0.02f);
        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(boss.PendingSummonCount).IsEqual(GameConstants.ProgenitorPhase1SquadSize);
    }

    // === Phase rules (pure) ===

    [TestCase]
    public void PhaseFor_ExactThresholds_BelongToTheHarderPhase()
    {
        const float maxHP = GameConstants.ProgenitorHP;

        AssertThat(ProgenitorSummons.Phase2ThresholdHP(maxHP)).IsEqual(166);
        AssertThat(ProgenitorSummons.Phase3ThresholdHP(maxHP)).IsEqual(83);

        AssertThat(ProgenitorSummons.PhaseFor(250f, maxHP)).IsEqual(1);
        AssertThat(ProgenitorSummons.PhaseFor(167f, maxHP)).IsEqual(1);
        AssertThat(ProgenitorSummons.PhaseFor(166f, maxHP)).IsEqual(2); // exact 66%
        AssertThat(ProgenitorSummons.PhaseFor(84f, maxHP)).IsEqual(2);
        AssertThat(ProgenitorSummons.PhaseFor(83f, maxHP)).IsEqual(3); // exact 33%
        AssertThat(ProgenitorSummons.PhaseFor(0f, maxHP)).IsEqual(3);
    }

    [TestCase]
    public void HpAndPhaseThresholds_ArePinned()
    {
        AssertThat(GameConstants.ProgenitorHP).IsEqual(250);
        AssertThat(ProgenitorSummons.Phase2ThresholdHP(GameConstants.ProgenitorHP)).IsEqual(166);
        AssertThat(ProgenitorSummons.Phase3ThresholdHP(GameConstants.ProgenitorHP)).IsEqual(83);
    }

    [TestCase]
    public void PhaseCadenceAndSquadSize_MatchSpec()
    {
        AssertThat(ProgenitorSummons.IntervalForPhase(1)).IsEqual(3.0f);
        AssertThat(ProgenitorSummons.IntervalForPhase(2)).IsEqual(2.25f);
        AssertThat(ProgenitorSummons.IntervalForPhase(3)).IsEqual(1.5f);

        AssertThat(ProgenitorSummons.SquadSizeForPhase(1)).IsEqual(3);
        AssertThat(ProgenitorSummons.SquadSizeForPhase(2)).IsEqual(4);
        AssertThat(ProgenitorSummons.SquadSizeForPhase(3)).IsEqual(5);
    }

    [TestCase]
    public void Cadence_PlaytestValues_AndFirstSummonReadyAtIntervalPlusWindup()
    {
        // Playtest-tuned cadence: the boss must summon visibly sooner than the original
        // 6.0 / 4.5 / 3.0 schedule, while every other phase rule stays the same.
        AssertThat(GameConstants.ProgenitorPhase1SummonInterval).IsEqual(3.0f);
        AssertThat(GameConstants.ProgenitorPhase2SummonInterval).IsEqual(2.25f);
        AssertThat(GameConstants.ProgenitorPhase3SummonInterval).IsEqual(1.5f);
        AssertThat(GameConstants.ProgenitorSummonWindup).IsEqual(0.5f);

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        int ready = 0;
        boss.SummonReady += _ => ready++;

        // No summon on spawn, and nothing at half the interval either.
        AssertThat(ready).IsEqual(0);
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval * 0.5f);
        AssertThat(ready).IsEqual(0);
        AssertThat(boss.IsSummoning).IsFalse();

        // At exactly one interval the windup starts; the squad is not out yet.
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval * 0.5f);
        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(ready).IsEqual(0);

        // The first SummonReady lands after interval + windup (3.0 + 0.5 = 3.5 s).
        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup);
        AssertThat(ready).IsEqual(1);
        AssertThat(boss.PendingSummonCount).IsEqual(GameConstants.ProgenitorPhase1SquadSize);
    }

    [TestCase]
    public void MixWeights_MatchTheSpecTable_AndSumToOne()
    {
        var phase1 = ProgenitorSummons.MixWeightsForPhase(1);
        AssertWeights(phase1, basic: 0.70, swarm: 0.30, armored: 0.00);

        var phase2 = ProgenitorSummons.MixWeightsForPhase(2);
        AssertWeights(phase2, basic: 0.40, swarm: 0.35, armored: 0.25);

        var phase3 = ProgenitorSummons.MixWeightsForPhase(3);
        AssertWeights(phase3, basic: 0.15, swarm: 0.40, armored: 0.45);

        // Every phase shifts weight toward the tougher enemy kinds.
        AssertThat(phase2.Basic < phase1.Basic).IsTrue();
        AssertThat(phase3.Basic < phase2.Basic).IsTrue();
        AssertThat(phase3.Armored > phase2.Armored).IsTrue();
    }

    [TestCase]
    public void Phase1_NeverRollsArmored_EvenForAnOutOfRangeRoll()
    {
        // A roll of exactly 1.0 (outside Random's [0,1) range) still stays in the
        // phase-1 bands: Swarm is the remainder, never Armored.
        AssertThat(ProgenitorSummons.RollKind(1, new ScriptedRandom(0, 1.0))).IsEqual(EnemyKind.Swarm);
    }

    [TestCase]
    public void BorderColor_ShiftsWhiteToAmberToRed()
    {
        var phase1 = ProgenitorSummons.BorderColorForPhase(1);
        var phase2 = ProgenitorSummons.BorderColorForPhase(2);
        var phase3 = ProgenitorSummons.BorderColorForPhase(3);

        AssertThat(phase1).IsEqual(GameConstants.ProgenitorPhase1BorderColor);
        AssertThat(phase2).IsEqual(GameConstants.ProgenitorPhase2BorderColor);
        AssertThat(phase3).IsEqual(GameConstants.ProgenitorPhase3BorderColor);

        // White -> amber -> red: green and blue fall away from white while red stays
        // at maximum, so the border visibly shifts hue as the boss weakens.
        AssertThat(phase2.G < phase1.G).IsTrue();
        AssertThat(phase2.B < phase1.B).IsTrue();
        AssertThat(phase3.G < phase2.G).IsTrue();
        AssertThat(phase3.B <= phase2.B).IsTrue();
        AssertThat(phase3.R).IsEqual(phase2.R);
    }

    [TestCase]
    public void Phase_UpdatesImmediatelyWhenDamageCrossesThreshold()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);

        // 87 damage - 3 armor = 84 applied -> 166 HP, exactly the 66% threshold.
        boss.TakeDamage(87);
        AssertThat(boss.CurrentHP).IsEqual(166f);
        AssertThat(boss.SummonPhase).IsEqual(2);
        AssertThat(boss.PhaseBorderColor).IsEqual(GameConstants.ProgenitorPhase2BorderColor);

        // 86 more (83 after armor) -> exactly 83 HP, the 33% threshold.
        boss.TakeDamage(86);
        AssertThat(boss.CurrentHP).IsEqual(83f);
        AssertThat(boss.SummonPhase).IsEqual(3);
        AssertThat(boss.PhaseBorderColor).IsEqual(GameConstants.ProgenitorPhase3BorderColor);
    }

    [TestCase]
    public void PhaseChange_ClampsCurrentCountdown_NeverResetsOrExtendsIt()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);

        // Half a second in: 2.5 s left of the 3.0 s phase-1 cadence.
        boss.TickProgenitorSummon(0.5f);
        AssertThat(boss.SummonCountdown).IsEqualApprox(2.5f, 0.0001f);

        // Crossing to phase 2 (84 applied by 87 damage, 250 -> 166) adopts the shorter
        // 2.25 s cadence on the current countdown.
        boss.TakeDamage(87);
        AssertThat(boss.SummonPhase).IsEqual(2);
        AssertThat(boss.SummonCountdown).IsEqualApprox(2.25f, 0.0001f);

        // Crossing to phase 3 (83 applied by 86 damage, 166 -> 83) clamps to 1.5 s,
        // still less than the remaining 2.25 s.
        boss.TakeDamage(86);
        AssertThat(boss.SummonPhase).IsEqual(3);
        AssertThat(boss.SummonCountdown).IsEqualApprox(1.5f, 0.0001f);

        // A nearly-elapsed countdown must not be extended by the faster cadence:
        // phase 3's 1.5 s interval never pushes a 1.0 s remainder back up.
        var nearSummon = new Enemy();
        nearSummon.Configure(EnemyKind.Progenitor);
        nearSummon.TickProgenitorSummon(2.0f);
        AssertThat(nearSummon.SummonCountdown).IsEqualApprox(1.0f, 0.0001f);
        nearSummon.TakeDamage(167, ignoreArmor: true); // 250 -> 83, straight to phase 3
        AssertThat(nearSummon.SummonPhase).IsEqual(3);
        AssertThat(nearSummon.SummonCountdown).IsEqualApprox(1.0f, 0.0001f);
    }

    // === Composition ===

    [TestCase]
    public void Composition_Phase1_BoundariesAndWeights()
    {
        AssertThat(ProgenitorSummons.RollKind(1, new ScriptedRandom(0, 0.0))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(1, new ScriptedRandom(0, 0.699))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(1, new ScriptedRandom(0, 0.70))).IsEqual(EnemyKind.Swarm);
        AssertThat(ProgenitorSummons.RollKind(1, new ScriptedRandom(0, 0.999))).IsEqual(EnemyKind.Swarm);
    }

    [TestCase]
    public void Composition_Phase2_BoundariesAndWeights()
    {
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.0))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.399))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.40))).IsEqual(EnemyKind.Swarm);
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.749))).IsEqual(EnemyKind.Swarm);
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.75))).IsEqual(EnemyKind.Armored);
        AssertThat(ProgenitorSummons.RollKind(2, new ScriptedRandom(0, 0.999))).IsEqual(EnemyKind.Armored);
    }

    [TestCase]
    public void Composition_Phase3_BoundariesAndWeights()
    {
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.0))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.149))).IsEqual(EnemyKind.Basic);
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.15))).IsEqual(EnemyKind.Swarm);
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.549))).IsEqual(EnemyKind.Swarm);
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.55))).IsEqual(EnemyKind.Armored);
        AssertThat(ProgenitorSummons.RollKind(3, new ScriptedRandom(0, 0.999))).IsEqual(EnemyKind.Armored);
    }

    [TestCase]
    public void Composition_SampledWeights_MatchTheSpecMix()
    {
        // A fixed seed keeps this deterministic while still exercising the real roll
        // source (not a scripted list). 12000 samples per phase, 2% tolerance.
        const int samples = 12000;
        var rolls = new SkillRandom(20261025);

        AssertMix(rolls, phase: 1, samples, basic: 0.70, swarm: 0.30, armored: 0.00);
        AssertMix(rolls, phase: 2, samples, basic: 0.40, swarm: 0.35, armored: 0.25);
        AssertMix(rolls, phase: 3, samples, basic: 0.15, swarm: 0.40, armored: 0.45);
    }

    [TestCase]
    public void SummonWindup_RollsOneKindPerSlot_AndFreezesThatComposition()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        // Phase 1: 0.1 -> Basic, 0.8 -> Swarm, 0.2 -> Basic.
        boss.SetSummonRandom(new ScriptedRandom(0.99, 0.1, 0.8, 0.2));

        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);

        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(boss.PendingSummonCount).IsEqual(3);
        AssertThat(boss.PendingSummonKind(0)).IsEqual(EnemyKind.Basic);
        AssertThat(boss.PendingSummonKind(1)).IsEqual(EnemyKind.Swarm);
        AssertThat(boss.PendingSummonKind(2)).IsEqual(EnemyKind.Basic);

        // Crossing to phase 3 mid-windup keeps the phase-1 squad: the next summon
        // (after the windup) uses the new phase.
        boss.TakeDamage(167, ignoreArmor: true); // 250 -> 83, phase 3
        AssertThat(boss.SummonPhase).IsEqual(3);
        AssertThat(boss.PendingSummonCount).IsEqual(3);

        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup + 0.0001f);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(3);
        AssertThat(boss.SummonCountdown).IsEqual(GameConstants.ProgenitorPhase3SummonInterval);

        // Phase 3 rolls a five-member squad.
        boss.ClearPendingSummon();
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase3SummonInterval);
        AssertThat(boss.PendingSummonCount).IsEqual(5);
    }

    [TestCase]
    public void SummonRandom_InjectedBeforeConfigure_IsUsedForComposition()
    {
        var boss = new Enemy();
        boss.SetSummonRandom(new ScriptedRandom(0.99, 0.05, 0.8, 0.2));
        boss.Configure(EnemyKind.Progenitor);

        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);

        AssertThat(boss.PendingSummonKind(0)).IsEqual(EnemyKind.Basic);
        AssertThat(boss.PendingSummonKind(1)).IsEqual(EnemyKind.Swarm);
        AssertThat(boss.PendingSummonKind(2)).IsEqual(EnemyKind.Basic);
    }

    [TestCase]
    public void SummonReady_FiresExactlyOnce_AtWindupEnd()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        int ready = 0;
        boss.SummonReady += _ => ready++;

        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(ready).IsEqual(0);

        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup * 0.5f);
        AssertThat(ready).IsEqual(0);

        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup * 0.5f + 0.0001f);
        AssertThat(ready).IsEqual(1);

        // Further ticks (no new windup yet) never re-emit the same squad.
        boss.TickProgenitorSummon(0.1f);
        AssertThat(ready).IsEqual(1);
    }

    // === Windup movement ===

    [TestCase]
    public void SummonWindup_SlowsMovementByTheSlowFactor()
    {
        var waypoints = new List<Vector2> { new(32f, 32f), new(96f, 32f), new(160f, 32f) };

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.StartAtProgress(waypoints, 1, waypoints[0]);

        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(boss.CurrentSpeed)
            .IsEqualApprox(GameConstants.ProgenitorSpeed * GameConstants.ProgenitorSummonSlowFactor, 0.0001f);

        Vector2 before = boss.AnchorPosition;
        boss._Process(0.1f);
        float moved = before.DistanceTo(boss.AnchorPosition);

        float expected = GameConstants.ProgenitorSpeed
            * GameConstants.ProgenitorSummonSlowFactor
            * GameConstants.CellSize
            * 0.1f;
        AssertThat(moved).IsEqualApprox(expected, 0.001f);

        // It moves rather than stopping: the spec is explicit that the boss keeps walking.
        AssertThat(moved > 0f).IsTrue();
    }

    [TestCase]
    public void OutsideWindup_BossMovesAtFullSpeed()
    {
        var waypoints = new List<Vector2> { new(32f, 32f), new(96f, 32f), new(160f, 32f) };

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.StartAtProgress(waypoints, 1, waypoints[0]);
        AssertThat(boss.CurrentSpeed).IsEqual(GameConstants.ProgenitorSpeed);

        Vector2 before = boss.AnchorPosition;
        boss._Process(0.1f);
        float moved = before.DistanceTo(boss.AnchorPosition);

        AssertThat(moved).IsEqualApprox(
            GameConstants.ProgenitorSpeed * GameConstants.CellSize * 0.1f,
            0.001f);
    }

    // === Summon lead placement ===

    [TestCase]
    public void SummonLeadPoint_IsAheadOnPath()
    {
        var waypoints = new List<Vector2> { new(32f, 32f), new(224f, 32f), new(288f, 32f) };

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.StartAtProgress(waypoints, 1, new Vector2(60f, 32f));

        Vector2 lead = boss.GetSummonLeadPoint();
        AssertThat(lead.X).IsEqualApprox(60f + GameConstants.ProgenitorSummonLeadDistance, 0.001f);
        AssertThat(lead.Y).IsEqual(32f);
    }

    [TestCase]
    public void SummonLeadPoint_ClampsAtCorner()
    {
        // A corner: the boss is 6 px from the turn, so the 100 px lead must clamp to
        // the corner waypoint instead of overshooting off the route.
        var waypoints = new List<Vector2>
        {
            new(32f, 32f),
            new(96f, 32f),
            new(96f, 160f)
        };

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.StartAtProgress(waypoints, 1, new Vector2(90f, 32f));

        Vector2 lead = boss.GetSummonLeadPoint();
        AssertThat(lead).IsEqual(new Vector2(96f, 32f));
    }

    [TestCase]
    public void SummonAtClampedCorner_ContinuesAlongTheRoute_NoCornerCut()
    {
        var waypoints = new List<Vector2>
        {
            new(32f, 32f),
            new(96f, 32f),
            new(96f, 160f)
        };

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.StartAtProgress(waypoints, 1, new Vector2(90f, 32f));

        var add = new Enemy();
        add.Configure(EnemyKind.Swarm);
        add.StartAtProgress(waypoints, boss.CurrentWaypointIndex, boss.GetSummonLeadPoint());

        AssertThat(add.CurrentWaypointIndex).IsEqual(boss.CurrentWaypointIndex);
        AssertThat(add.AnchorPosition).IsEqual(new Vector2(96f, 32f));

        // Past the corner: the add turns down the next segment, never cutting across it.
        add._Process(0.1f); // reaches the corner waypoint, advances index
        add._Process(0.1f); // moves down the vertical segment

        AssertThat(add.AnchorPosition.X).IsEqual(96f);
        AssertThat(add.AnchorPosition.Y > 32f).IsTrue();

        int guard = 0;
        while (!add.IsDead && guard++ < 500)
        {
            add._Process(0.05f);
            AssertThat(add.AnchorPosition.X).IsEqual(96f);
        }
        AssertThat(add.IsDead).IsTrue();
    }

    [TestCase]
    public void LeadPoint_PureRules_ClampAndDegenerateSegment()
    {
        // Zero-length segment: stay at the anchor rather than producing NaN.
        AssertThat(ProgenitorSummons.LeadPoint(new Vector2(5f, 5f), new Vector2(5f, 5f), 100f))
            .IsEqual(new Vector2(5f, 5f));

        // Negative lead never moves backwards off the route.
        AssertThat(ProgenitorSummons.LeadPoint(new Vector2(0f, 0f), new Vector2(64f, 0f), -10f))
            .IsEqual(Vector2.Zero);

        // Short segment clamps to the waypoint.
        AssertThat(ProgenitorSummons.LeadPoint(new Vector2(0f, 0f), new Vector2(30f, 0f), 100f))
            .IsEqual(new Vector2(30f, 0f));
    }

    [TestCase]
    public void StartAtProgress_ClampsAwayFromTheSyntheticExitWaypoint()
    {
        // GridManager appends a one-cell-past-base exit waypoint, so index Count-1 is
        // not a valid starting position: an enemy there leaks within a frame or two.
        var waypoints = new List<Vector2> { new(0f, 0f), new(64f, 0f), new(128f, 0f) };
        int lastRealIndex = waypoints.Count - 2;

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        enemy.StartAtProgress(waypoints, waypoints.Count - 1, new Vector2(128f, 0f));
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(lastRealIndex);

        enemy.StartAtProgress(waypoints, 99, new Vector2(128f, 0f));
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(lastRealIndex);

        enemy.StartAtProgress(waypoints, lastRealIndex, new Vector2(64f, 0f));
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(lastRealIndex);

        enemy.StartAtProgress(waypoints, -5, Vector2.Zero);
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(0);

        // A single-waypoint list has no exit to protect: clamp to index zero.
        enemy.StartAtProgress(new List<Vector2> { new(0f, 0f) }, 5, Vector2.Zero);
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(0);
    }

    [TestCase]
    public void HasReachedBase_IsFalseAtTheLastRouteCell_AndTrueAtTheExit()
    {
        var waypoints = new List<Vector2> { new(0f, 0f), new(64f, 0f), new(128f, 0f) };
        const int lastRealIndex = 1;

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.StartAtProgress(waypoints, lastRealIndex, Vector2.Zero);

        AssertThat(enemy.CurrentWaypointIndex).IsEqual(lastRealIndex);
        AssertThat(enemy.HasReachedBase).IsFalse();

        // One movement tick snaps the anchor onto the last real route cell and advances
        // the index to the synthetic exit waypoint: the base has been reached.
        enemy._Process(0.6f);
        AssertThat(enemy.CurrentWaypointIndex).IsEqual(lastRealIndex + 1);
        AssertThat(enemy.AnchorPosition).IsEqual(new Vector2(64f, 0f));
        AssertThat(enemy.HasReachedBase).IsTrue();

        // No path is not "reached base".
        enemy.ResetForPool();
        AssertThat(enemy.HasReachedBase).IsFalse();
    }

    // === Death and pool reuse ===

    [TestCase]
    public void BossDeathDuringWindup_CancelsTheSummon()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        int ready = 0;
        boss.SummonReady += _ => ready++;

        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(boss.PendingSummonCount).IsEqual(GameConstants.ProgenitorPhase1SquadSize);

        boss.TakeDamage(1000f, ignoreArmor: true);

        AssertThat(boss.IsDead).IsTrue();
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);

        // No squad may spawn after the boss is dead.
        boss.TickProgenitorSummon(10f);
        AssertThat(ready).IsEqual(0);
    }

    [TestCase]
    public void ResetForPool_ClearsEveryProgenitorField()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.TakeDamage(167f, ignoreArmor: true); // 250 -> 83, phase 3
        boss.SetCoinDrop(0);
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsTrue();
        AssertThat(boss.SummonPhase).IsEqual(3);

        boss.ResetForPool();

        AssertThat(boss.IsProgenitor).IsFalse();
        AssertThat(boss.SummonPhase).IsEqual(1);
        AssertThat(boss.SummonCountdown).IsEqual(0f);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);
        AssertThat(boss.CoinDrop).IsEqual(GameConstants.CoinDropPerKill);

        // Reused as a normal enemy: normal stats, no boss state, no summon state.
        boss.Configure(EnemyKind.Basic);
        AssertThat(boss.Kind).IsEqual(EnemyKind.Basic);
        AssertThat(boss.CurrentHP).IsEqual((float)GameConstants.EnemyHP);
        AssertThat(boss.Diameter).IsEqual((float)GameConstants.EnemyDiameter);
        AssertThat(boss.CoinDrop).IsEqual(GameConstants.CoinDropPerKill);
        AssertThat(boss.IsProgenitor).IsFalse();
        AssertThat(boss.BaseDamage).IsEqual(1);
        AssertThat(boss.IsSummoning).IsFalse();

        boss.TickProgenitorSummon(100f);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);
    }

    [TestCase]
    public void ReconfigureFromBossToNormal_ClearsSummonStateWithoutResetForPool()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.SetCoinDrop(0);
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);

        boss.Configure(EnemyKind.Armored);

        AssertThat(boss.Kind).IsEqual(EnemyKind.Armored);
        AssertThat(boss.IsProgenitor).IsFalse();
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(boss.PendingSummonCount).IsEqual(0);
        AssertThat(boss.SummonCountdown).IsEqual(0f);
        AssertThat(boss.CoinDrop).IsEqual(GameConstants.ArmoredCoinDropPerKill);
    }

    [TestCase]
    public void BossCoins_AndZeroCoinOverride()
    {
        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        AssertThat(boss.CoinDrop).IsEqual(GameConstants.ProgenitorCoinDrop);
        AssertThat(GameConstants.ProgenitorCoinDrop > GameConstants.CoinDropPerKill).IsTrue();

        boss.SetCoinDrop(0);
        AssertThat(boss.CoinDrop).IsEqual(0);

        // Negative overrides clamp to zero rather than paying the player.
        boss.SetCoinDrop(-5);
        AssertThat(boss.CoinDrop).IsEqual(0);
    }

    private static void AssertWeights(
        (double Basic, double Swarm, double Armored) mix,
        double basic,
        double swarm,
        double armored)
    {
        AssertThat(mix.Basic).IsEqualApprox(basic, 0.0001);
        AssertThat(mix.Swarm).IsEqualApprox(swarm, 0.0001);
        AssertThat(mix.Armored).IsEqualApprox(armored, 0.0001);
        AssertThat(mix.Basic + mix.Swarm + mix.Armored).IsEqualApprox(1.0, 0.0001);
    }

    private static void AssertMix(
        SkillRandom rolls,
        int phase,
        int samples,
        double basic,
        double swarm,
        double armored)
    {
        int basicCount = 0;
        int swarmCount = 0;
        int armoredCount = 0;

        for (int i = 0; i < samples; i++)
        {
            switch (ProgenitorSummons.RollKind(phase, rolls))
            {
                case EnemyKind.Basic: basicCount++; break;
                case EnemyKind.Swarm: swarmCount++; break;
                default: armoredCount++; break;
            }
        }

        AssertThat((double)basicCount / samples).IsEqualApprox(basic, 0.02);
        AssertThat((double)swarmCount / samples).IsEqualApprox(swarm, 0.02);
        AssertThat((double)armoredCount / samples).IsEqualApprox(armored, 0.02);
    }
}
