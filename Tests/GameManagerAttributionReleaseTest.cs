using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Regression coverage for enemy attribution cleanup on every pool-release path, and
/// for the idempotent test registration seam. A released enemy must never keep a
/// strong <see cref="Tower"/> reference in its credit table or burn source.
/// </summary>
[TestSuite]
public class GameManagerAttributionReleaseTest
{
    private static GameManager CreateManager()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level3);
        manager.AddCoinsForTesting(100);
        return manager;
    }

    private static Tower PlaceArrow(GameManager manager)
    {
        manager.PlacingTowerType = TowerType.Arrow;
        AssertThat(manager.PlaceTower(4, 2)).IsTrue();
        return manager.GetActiveTowers()[^1];
    }

    private static void RunToBase(Enemy enemy)
    {
        int ticks = 0;
        while (!enemy.IsDead && ticks < 500)
        {
            enemy._Process(1f);
            ticks++;
        }
        AssertThat(enemy.IsDead).IsTrue();
    }

    [TestCase]
    public void KilledEnemy_DropsAllTowerReferencesBeforePooling()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);
        AssertThat(manager.StartNextWave()).IsTrue();
        var enemy = manager.GetActiveEnemies()[0];

        enemy.TakeDamage(2f, source: tower);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);

        enemy.TakeDamage(1000f, source: tower);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        // The kill still paid its XP before attribution was dropped.
        AssertThat(tower.Xp).IsEqualApprox(GameConstants.EnemyXpBasic, 0.0001f);
    }

    [TestCase]
    public void EnemyReachingBase_DropsAllTowerReferencesBeforePooling()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);
        AssertThat(manager.StartNextWave()).IsTrue();
        var enemy = manager.GetActiveEnemies()[0];

        enemy.TakeDamage(2f, source: tower);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);

        RunToBase(enemy);

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        AssertThat(tower.Xp).IsEqual(0f);
    }

    [TestCase]
    public void BaseArrivalDuringGameOver_StillDropsTowerReferences()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);
        AssertThat(manager.StartNextWave()).IsTrue();
        var enemy = manager.GetActiveEnemies()[0];

        enemy.TakeDamage(2f, source: tower);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);

        // Force the early GameOver branch of OnEnemyReachedEnd.
        manager.SetStateForTesting(GameState.GameOver);
        RunToBase(enemy);

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        AssertThat(tower.Xp).IsEqual(0f);
    }

    [TestCase]
    public void LevelReset_DropsTowerReferencesOnEveryLiveEnemy()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);
        AssertThat(manager.StartNextWave()).IsTrue();
        var enemy = manager.GetActiveEnemies()[0];

        enemy.TakeDamage(2f, source: tower);
        AssertThat(enemy.CreditSourceCount).IsEqual(1);

        manager.ResetGame();

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(enemy.CreditSourceCount).IsEqual(0);
        AssertThat(tower.Xp).IsEqual(0f);
    }

    [TestCase]
    public void LevelReset_UnregistersBossBeforeItsKindIsReset()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);

        var boss = new Enemy();
        boss.Configure(EnemyKind.Progenitor);
        boss.Position = new Vector2(100f, 100f);
        manager.RegisterEnemyForTesting(boss);
        AssertThat(manager.ActiveEnemyCount).IsEqual(1);
        AssertThat(boss.Kind).IsEqual(EnemyKind.Progenitor);

        boss.TakeDamage(20f, source: tower);
        AssertThat(boss.CreditSourceCount).IsEqual(1);

        manager.ResetGame();

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        AssertThat(boss.CreditSourceCount).IsEqual(0);
        // UnregisterEnemy ran while the boss still reported Progenitor; only then did
        // the reset path return it to the neutral Basic default.
        AssertThat(boss.Kind).IsEqual(EnemyKind.Basic);
    }

    [TestCase]
    public void RegisterEnemyForTestingTwice_YieldsOneEntryAndOneReward()
    {
        var manager = CreateManager();
        var tower = PlaceArrow(manager);
        int coinsBefore = manager.Coins;

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        manager.RegisterEnemyForTesting(enemy);
        manager.RegisterEnemyForTesting(enemy);

        AssertThat(manager.ActiveEnemyCount).IsEqual(1);
        AssertThat(manager.GetActiveEnemies()[0] == enemy).IsTrue();

        enemy.TakeDamage(1000f, source: tower);

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
        // A double subscription would have paid the coin drop (and the wave/kill
        // bookkeeping) twice.
        AssertThat(manager.Coins).IsEqual(coinsBefore + enemy.CoinDrop);
        AssertThat(tower.Xp).IsEqualApprox(GameConstants.EnemyXpBasic, 0.0001f);
    }

    [TestCase]
    public void RegisterEnemyForTesting_IsIgnoredOutsidePlaying()
    {
        var manager = CreateManager();
        manager.SetStateForTesting(GameState.GameOver);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        manager.RegisterEnemyForTesting(enemy);

        AssertThat(manager.ActiveEnemyCount).IsEqual(0);
    }
}
