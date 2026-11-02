using System.Collections.Generic;
using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// The space-bar speedup clock owned by <see cref="GameManager"/>: toggling flips
/// exactly between the normal and speedup rates and only during active play, speed
/// survives wave completion, and every exit path restores the global engine clock.
/// Each test restores <see cref="Engine.TimeScale"/> in a finally block so a failure
/// can never leak a sped-up clock into another suite.
/// </summary>
[TestSuite]
public class GameManagerSpeedupTest
{
    private static float TimeScale => (float)Engine.TimeScale;

    private static GameManager CreatePlayingManager()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);
        return manager;
    }

    /// <summary>
    /// Restores the global clock first so a leaked 1.5x rate can never outlive the test,
    /// even if node teardown throws.
    /// </summary>
    private static void Teardown(GameManager? manager)
    {
        Engine.TimeScale = GameConstants.NormalTimeScale;
        manager?.QueueFree();
    }

    [TestCase]
    public void ToggleSpeedup_FlipsExactlyBetweenTheTwoRates()
    {
        var manager = CreatePlayingManager();
        try
        {
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);

            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(manager.IsSpeedupActive).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // Rapid presses: each flips once, and no value other than the two rates
            // is ever installed on the engine clock.
            for (int i = 0; i < 6; i++)
            {
                AssertThat(manager.ToggleSpeedup()).IsTrue();
                bool expectedActive = i % 2 == 1;
                AssertThat(manager.IsSpeedupActive).IsEqual(expectedActive);
                AssertThat(TimeScale).IsEqual(
                    expectedActive ? GameConstants.SpeedupTimeScale : GameConstants.NormalTimeScale);
            }
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void ToggleSpeedup_EmitsOneChangeSignalPerFlip()
    {
        var manager = CreatePlayingManager();
        var changes = new List<bool>();
        manager.SpeedupChanged += active => changes.Add(active);
        try
        {
            manager.ToggleSpeedup();
            manager.ToggleSpeedup();

            AssertThat(changes.Count).IsEqual(2);
            AssertThat(changes[0]).IsTrue();
            AssertThat(changes[1]).IsFalse();
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void ToggleSpeedup_DoesNothingOutsideActivePlay()
    {
        var manager = CreatePlayingManager();
        try
        {
            manager.SetStateForTesting(GameState.GameOver);
            AssertThat(manager.ToggleSpeedup()).IsFalse();
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);

            manager.SetStateForTesting(GameState.Victory);
            AssertThat(manager.ToggleSpeedup()).IsFalse();
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void Speedup_SurvivesWaveCompletionAndTheNextWave()
    {
        var manager = CreatePlayingManager();
        try
        {
            AssertThat(manager.StartNextWave()).IsTrue();
            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // Finish wave 1 for real (three kills): completion is a HUD notification
            // and must never touch the clock.
            WaveTestSupport.SpawnRemainingWaveEnemies(manager);
            foreach (var enemy in manager.GetActiveEnemies().ToList())
                enemy.TakeDamage(1000f);

            AssertThat(manager.WaveManager!.CurrentWave).IsEqual(1);
            AssertThat(manager.WaveManager.IsWaveActive).IsFalse();
            AssertThat(manager.IsSpeedupActive).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // The next wave begins at the current speed.
            AssertThat(manager.StartNextWave()).IsTrue();
            AssertThat(manager.WaveManager.CurrentWave).IsEqual(2);
            AssertThat(manager.IsSpeedupActive).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void GameOver_ResetsTheClockToNormal()
    {
        var manager = CreatePlayingManager();
        try
        {
            AssertThat(manager.StartNextWave()).IsTrue();
            WaveTestSupport.SpawnRemainingWaveEnemies(manager);
            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            // Level 1 starts at 3 HP and each leaked Basic costs 1: three arrivals
            // defeat the run through the real enemy-arrival path.
            foreach (var enemy in manager.GetActiveEnemies().ToList())
                WaveTestSupport.RunToBase(enemy);

            AssertThat(manager.IsGameOver).IsTrue();
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void Victory_ResetsTheClockToNormal()
    {
        var manager = CreatePlayingManager();
        try
        {
            AssertThat(manager.StartNextWave()).IsTrue();
            AssertThat(manager.ToggleSpeedup()).IsTrue();

            // WaveManager emits this on the final wave's real completion; GameManager's
            // handler is the production victory path.
            manager.WaveManager!.EmitSignal(WaveManager.SignalName.AllWavesCompleted);

            AssertThat(manager.IsVictory).IsTrue();
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void ResetGame_ResetsTheClockToNormal()
    {
        var manager = CreatePlayingManager();
        try
        {
            AssertThat(manager.ToggleSpeedup()).IsTrue();
            AssertThat(TimeScale).IsEqual(GameConstants.SpeedupTimeScale);

            manager.ResetGame();

            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void ToggleSpeedup_RefusesBeforeInitialization()
    {
        var manager = new GameManager();
        try
        {
            AssertThat(manager.ToggleSpeedup()).IsFalse();
            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void SpawnTimer_UsesTheScaledClock_SoSpeedupChangesCadence()
    {
        var manager = CreatePlayingManager();
        try
        {
            // Engine.TimeScale only speeds wave spawning if the spawn timer follows the
            // engine clock; an unscaled timer would keep the 1.0x cadence at 1.5x.
            var timer = manager.WaveManager!.GetChildren().OfType<Timer>().First();
            AssertThat(timer.IgnoreTimeScale).IsFalse();
        }
        finally
        {
            Teardown(manager);
        }
    }

    [TestCase]
    public void Initialize_ForcesTheNormalClockEvenWhenItWasLeftSpedUp()
    {
        GameManager? manager = null;
        try
        {
            Engine.TimeScale = GameConstants.SpeedupTimeScale;
            manager = new GameManager();
            manager.Initialize(Levels.Level1);

            AssertThat(manager.IsSpeedupActive).IsFalse();
            AssertThat(TimeScale).IsEqual(GameConstants.NormalTimeScale);
        }
        finally
        {
            Teardown(manager);
        }
    }
}
