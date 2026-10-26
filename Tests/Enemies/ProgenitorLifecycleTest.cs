using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// End-to-end boss wiring through the real GameManager/WaveManager lifecycle: summon
/// squads join the active-enemy list, count toward the wave, drop no coins, and keep
/// victory gated until the last summon is gone. Also covers stop-on-reset.
/// </summary>
[TestSuite]
public class ProgenitorLifecycleTest
{
    [TestCase]
    public void BossSummon_SpawnsSquadThroughNormalLifecycle_AndBlocksEarlyVictory()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        // Every phase-1 roll below 0.70 is a Basic enemy, so the squad is deterministic.
        manager.SummonRandom = new ScriptedRandom(0.0);
        manager.Initialize(level);

        AssertThat(manager.StartNextWave()).IsTrue();
        // Emulate the spawn timer finding an empty queue: the boss is the wave's only
        // wave-defined event, so from here the wave lives or dies with enemies on screen.
        manager.WaveManager!.StopSpawning();

        AssertThat(manager.ActiveEnemyCount).IsEqual(1);
        Enemy boss = manager.GetActiveEnemies()[0];
        AssertThat(boss.Kind).IsEqual(EnemyKind.Progenitor);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(1);

        // Walk the boss one tick so it has route progress and a lead point down the lane.
        boss._Process(1.0);
        AssertThat(boss.CurrentWaypointIndex > 0).IsTrue();

        // First interval -> windup; windup end -> squad released through WaveManager.
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsTrue();
        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup + 0.0001f);
        AssertThat(boss.IsSummoning).IsFalse();

        int squad = GameConstants.ProgenitorPhase1SquadSize;
        AssertThat(manager.ActiveEnemyCount).IsEqual(1 + squad);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(1 + squad);

        // Every add is on the same route progress, ahead of the boss, and worth nothing.
        var adds = new List<Enemy>();
        foreach (var enemy in manager.GetActiveEnemies())
        {
            if (enemy.Kind == EnemyKind.Progenitor)
                continue;

            adds.Add(enemy);
            AssertThat(enemy.CoinDrop).IsEqual(0);
            AssertThat(enemy.CurrentWaypointIndex).IsEqual(boss.CurrentWaypointIndex);
            AssertThat(enemy.Position.X > boss.Position.X).IsTrue();
        }
        AssertThat(adds.Count).IsEqual(squad);

        // Boss death: its coin payoff lands, future summons stop, and the surviving
        // adds keep the wave — and victory — open.
        boss.TakeDamage(1000f, ignoreArmor: true);
        boss.TickProgenitorSummon(100f); // no resurrection, no extra squad

        AssertThat(manager.State).IsEqual(GameState.Playing);
        AssertThat(manager.IsVictory).IsFalse();
        AssertThat(manager.ActiveEnemyCount).IsEqual(squad);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(squad);
        AssertThat(manager.Coins).IsEqual(GameConstants.StartingCoins + GameConstants.ProgenitorCoinDrop);

        int coinsAfterBoss = manager.Coins;
        for (int i = 0; i < adds.Count; i++)
        {
            adds[i].TakeDamage(1000f, ignoreArmor: true);

            if (i < adds.Count - 1)
            {
                // At least one summon is still alive, so the final wave cannot complete.
                AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(adds.Count - i - 1);
                AssertThat(manager.WaveManager.IsWaveActive).IsTrue();
                AssertThat(manager.IsVictory).IsFalse();
            }
        }

        // Adds awarded nothing; the last death completed the final wave and won.
        AssertThat(manager.Coins).IsEqual(coinsAfterBoss);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(0);
        AssertThat(manager.IsVictory).IsTrue();
    }

    [TestCase]
    public void BossReachingBase_CostsProgenitorBaseDamage_AndStopsSummons()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        manager.SummonRandom = new ScriptedRandom(0.0);
        manager.Initialize(level);
        manager.StartNextWave();
        manager.WaveManager!.StopSpawning();

        Enemy boss = manager.GetActiveEnemies()[0];
        int guard = 0;
        while (!boss.IsDead && guard++ < 1000)
            boss._Process(0.1);

        AssertThat(boss.IsDead).IsTrue();
        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP - GameConstants.ProgenitorBaseDamage);
        AssertThat(manager.IsGameOver).IsFalse();

        // The boss is no longer tracked, so no new squad can appear.
        boss.TickProgenitorSummon(100f);
        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
    }

    [TestCase]
    public void ResetGame_StopsFutureSummons_AndReleasesSummonsSafely()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        manager.SummonRandom = new ScriptedRandom(0.0);
        manager.Initialize(level);
        manager.StartNextWave();
        manager.WaveManager!.StopSpawning();

        Enemy boss = manager.GetActiveEnemies()[0];
        boss._Process(1.0);
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);

        manager.ResetGame();

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(manager.State).IsEqual(GameState.Playing);

        // Ticking the stale boss after the reset must not spawn anything: its summon
        // signal is unhooked and the manager's boss reference is cleared.
        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup + 0.1f);
        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
    }

    [TestCase]
    public void BossAtBaseProgress_CannotSpawnAdds_AndLosesNoHP()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        manager.SummonRandom = new ScriptedRandom(0.0);
        manager.Initialize(level);
        manager.StartNextWave();
        manager.WaveManager!.StopSpawning();

        Enemy boss = manager.GetActiveEnemies()[0];
        var waypoints = manager.Grid!.GetPathWaypoints(0);

        // Walk the boss to the base cell. Its index then points at the synthetic
        // one-cell-past-base exit waypoint (Count-1) with the anchor on the base —
        // exactly the post-base state a completing windup must not summon from.
        int guard = 0;
        while (boss.CurrentWaypointIndex < waypoints.Count - 1 && guard++ < 2000)
            boss._Process(0.1);

        AssertThat(boss.CurrentWaypointIndex).IsEqual(waypoints.Count - 1);
        AssertThat(boss.AnchorPosition).IsEqual(waypoints[^2]);

        // Fire the summon path. The base guard must drop the pending squad: adds here
        // would leak within a frame or two for unavoidable HP loss.
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        AssertThat(boss.IsSummoning).IsTrue();
        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup + 0.0001f);

        AssertThat(boss.PendingSummonCount).IsEqual(0);
        AssertThat(boss.IsSummoning).IsFalse();
        AssertThat(manager.ActiveEnemyCount).IsEqual(1);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(1);
        AssertThat(manager.HP).IsEqual(GameConstants.StartingHP);
        AssertThat(manager.IsGameOver).IsFalse();
    }

    [TestCase]
    public void BossSummon_AdvancesThroughGameManagerProcess_AcrossFirstInterval()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        manager.SummonRandom = new ScriptedRandom(0.0);
        manager.Initialize(level);
        manager.StartNextWave();
        manager.WaveManager!.StopSpawning();

        AssertThat(manager.ActiveEnemyCount).IsEqual(1);

        // Drive time only through the production update loop. This fails if
        // GameManager stops ticking its tracked boss (or stops registering it).
        int ticks = 0;
        while (ticks++ < 140)
            manager._Process(0.05f);

        int expected = 1 + GameConstants.ProgenitorPhase1SquadSize;
        AssertThat(manager.ActiveEnemyCount).IsEqual(expected);
        AssertThat(manager.WaveManager.EnemiesAlive).IsEqual(expected);
    }

    [TestCase]
    public void BossSummon_SpawnsRolledKindsInSlotOrder()
    {
        var level = CreateBossLevel();
        var manager = new GameManager();
        // Phase 1 rolls: 0.1 -> Basic, 0.8 -> Swarm, 0.2 -> Basic.
        manager.SummonRandom = new ScriptedRandom(0.99, 0.1, 0.8, 0.2);
        manager.Initialize(level);
        manager.StartNextWave();
        manager.WaveManager!.StopSpawning();

        Enemy boss = manager.GetActiveEnemies()[0];
        boss.TickProgenitorSummon(GameConstants.ProgenitorPhase1SummonInterval);
        boss.TickProgenitorSummon(GameConstants.ProgenitorSummonWindup + 0.0001f);

        // One enemy per rolled slot, in slot order — the Swarm slot is a single small
        // circle, never an expanded 3-member cluster.
        var kinds = new List<EnemyKind>();
        foreach (var enemy in manager.GetActiveEnemies())
            if (enemy.Kind != EnemyKind.Progenitor)
                kinds.Add(enemy.Kind);

        AssertThat(kinds.Count).IsEqual(GameConstants.ProgenitorPhase1SquadSize);
        AssertThat(kinds[0]).IsEqual(EnemyKind.Basic);
        AssertThat(kinds[1]).IsEqual(EnemyKind.Swarm);
        AssertThat(kinds[2]).IsEqual(EnemyKind.Basic);
        AssertThat(manager.ActiveEnemyCount).IsEqual(1 + GameConstants.ProgenitorPhase1SquadSize);
    }

    [TestCase]
    public void FinalWaveDefeat_StaysGameOver_StopsSpawning_AndAwardsNoVictorySP()
    {
        // The final wave's enemies leak one by one until the player is defeated. The
        // last leak empties the final wave synchronously, so AllWavesCompleted fires
        // from inside the defeat path — it must not overwrite GameOver with Victory.
        var level = CreateSingleWaveLevel(SpawnKind.SwarmCluster);
        var manager = new GameManager();
        var skillTree = new SkillTree(new SkillTreeState(), _ => false);
        manager.SkillTree = skillTree;
        manager.Initialize(level);
        manager.StartNextWave();

        var enemies = new List<Enemy>(manager.GetActiveEnemies());
        AssertThat(enemies.Count).IsEqual(GameConstants.SwarmClusterSize);

        foreach (var enemy in enemies)
        {
            int guard = 0;
            while (!enemy.IsDead && guard++ < 1000)
                enemy._Process(0.1f);
            AssertThat(enemy.IsDead).IsTrue();
        }

        AssertThat(manager.State).IsEqual(GameState.GameOver);
        AssertThat(manager.IsVictory).IsFalse();
        AssertThat(manager.HP).IsEqual(0);

        // Defeat stopped the wave timer and dropped its pending spawns.
        AssertThat(manager.WaveManager!.IsWaveActive).IsFalse();

        // Defeat SP only — no victory award was layered on top.
        AssertThat(skillTree.State.SkillPoints).IsEqual(GameConstants.SkillPointDefeatReward(level.Id));

        // No enemy can join a finished run, and no new wave can start.
        int activeAfterDefeat = manager.ActiveEnemyCount;
        for (int i = 0; i < 20; i++)
            manager._Process(0.1f);
        AssertThat(manager.ActiveEnemyCount).IsEqual(activeAfterDefeat);
        AssertThat(manager.StartNextWave()).IsFalse();
    }

    /// <summary>
    /// One straight route and one wave whose only event is the Progenitor, so the
    /// integration tests can drive the boss's first summon without waiting out a wave.
    /// </summary>
    private static LevelDefinition CreateBossLevel() =>
        CreateSingleWaveLevel(SpawnKind.Progenitor);

    private static LevelDefinition CreateSingleWaveLevel(params SpawnKind[] spawns)
    {
        var path = LevelDefinition.BuildPath(new[]
        {
            new Vector2I(0, 7),
            new Vector2I(19, 7)
        });

        var paths = new IReadOnlyList<Vector2I>[] { path };
        var waves = new List<WaveDefinition>
        {
            new WaveDefinition(spawns)
        };

        return new LevelDefinition(99, "Test Level", paths, allowCannonTower: true, allowLaserTower: true, waves);
    }
}
