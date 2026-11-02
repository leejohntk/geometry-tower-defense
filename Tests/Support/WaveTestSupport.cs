using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Shared wave-driving helpers for suites that exercise a <see cref="GameManager"/>
/// without a running main loop: draining a wave's spawn queue and walking an enemy to
/// the base.
/// </summary>
public static class WaveTestSupport
{
    /// <summary>
    /// Spawns every enemy of the current wave. StartNextWave spawns the first event
    /// synchronously; the rest arrive through the spawn Timer, which a test can tick
    /// without a running main loop by emitting its Timeout signal. One extra tick
    /// drains the empty queue so the wave leaves its spawning phase.
    /// </summary>
    public static void SpawnRemainingWaveEnemies(GameManager manager)
    {
        var waveManager = manager.WaveManager!;
        var timer = waveManager.GetChildren().OfType<Timer>().First();

        for (int i = 0; i < GameConstants.GridCols; i++)
        {
            if (waveManager.EnemiesAlive >= waveManager.EnemiesInWave)
                break;
            timer.EmitSignal(Timer.SignalName.Timeout);
        }

        timer.EmitSignal(Timer.SignalName.Timeout);

        AssertThat(waveManager.EnemiesAlive).IsEqual(waveManager.EnemiesInWave);
    }

    /// <summary>
    /// Ticks an enemy along its path until it reaches the base (or dies), so the real
    /// arrival path — HP loss, pooling, and any resulting game-over transition — runs.
    /// </summary>
    public static void RunToBase(Enemy enemy)
    {
        int ticks = 0;
        while (!enemy.IsDead && ticks < 500)
        {
            enemy._Process(1f);
            ticks++;
        }
        AssertThat(enemy.IsDead).IsTrue();
    }
}
