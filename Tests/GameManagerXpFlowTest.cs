using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// GameManager-driven kill-XP flow: a real laser drain credits its tower through the
/// live damage path, a base arrival awards nothing, and boss summons carry their
/// rolled kind's XP value while still dropping zero coins.
/// </summary>
[TestSuite]
public class GameManagerXpFlowTest
{
    private static GameManager CreateLevel3Manager()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level3);
        manager.AddCoinsForTesting(100);
        return manager;
    }

    private static Tower PlaceTower(GameManager manager, TowerType type, int row, int col)
    {
        manager.PlacingTowerType = type;
        AssertThat(manager.PlaceTower(row, col)).IsTrue();
        return manager.GetActiveTowers()[^1];
    }

    [TestCase]
    public void LaserKill_CreditsItsTowerThroughTheRealDrainPath()
    {
        var manager = CreateLevel3Manager();
        var laser = PlaceTower(manager, TowerType.Laser, row: 4, col: 2);

        AssertThat(manager.StartNextWave()).IsTrue();
        AssertThat(manager.ActiveEnemyCount).IsEqual(1);

        var enemy = manager.GetActiveEnemies()[0];
        // Enemies do not run their own _Process in this harness, so park the spawn
        // inside the laser's range and let GameManager's drain pass kill it.
        enemy.Position = laser.Position + new Vector2(60f, 0f);

        for (int i = 0; i < 40 && !enemy.IsDead; i++)
            manager._Process(0.25);

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(laser.Xp).IsEqualApprox(GameConstants.EnemyXpBasic, 0.0001f);
    }

    [TestCase]
    public void EnemyReachingTheBase_AwardsNoXp_EvenAfterBeingDamaged()
    {
        var manager = CreateLevel3Manager();
        var tower = PlaceTower(manager, TowerType.Arrow, row: 4, col: 2);

        AssertThat(manager.StartNextWave()).IsTrue();
        var enemy = manager.GetActiveEnemies()[0];

        // Damage it (creating a credit entry) without killing it.
        enemy.TakeDamage(2f, source: tower);
        AssertThat(enemy.CreditedDamage(tower)).IsEqualApprox(2f, 0.0001f);

        int hpBefore = manager.HP;
        int ticks = 0;
        while (!enemy.IsDead && ticks < 500)
        {
            enemy._Process(1f);
            ticks++;
        }

        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(manager.HP).IsEqual(hpBefore - 1);
        AssertThat(tower.Xp).IsEqual(0f);
    }

    [TestCase]
    public void LaserChainJump_CreditsTheLaserForTheJumpedDamage()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level3);
        manager.AddCoinsForTesting(100);

        // One Chain rank turns on chain jumps (pre-archetype: the seed applies at its
        // skill-tree rank, exactly as before).
        var treeState = new SkillTreeState();
        treeState.SetRank(SkillTreeCatalog.LaserChain, 1);
        manager.SkillTree = new SkillTree(treeState, _ => true);

        var laser = PlaceTower(manager, TowerType.Laser, row: 4, col: 2);

        var primary = new Enemy();
        primary.Configure(EnemyKind.Basic);
        primary.Position = laser.Position + new Vector2(60f, 0f);

        var chained = new Enemy();
        chained.Configure(EnemyKind.Basic);
        chained.Position = primary.Position + new Vector2(60f, 0f);

        manager.RegisterEnemyForTesting(primary);
        manager.RegisterEnemyForTesting(chained);

        manager._Process(0.25);

        // 4 dps x 0.25s = 1 damage to the primary; the chain jump lands 60% = 0.6
        // on the second enemy. Both credit the same tower.
        AssertThat(primary.CreditedDamage(laser)).IsEqualApprox(1f, 0.0001f);
        AssertThat(chained.CreditedDamage(laser)).IsEqualApprox(0.6f, 0.0001f);

        // Finish both and confirm the per-enemy XP split still pays the laser.
        for (int i = 0; i < 80 && (!primary.IsDead || !chained.IsDead); i++)
            manager._Process(0.25);

        AssertThat(primary.IsDead).IsTrue();
        AssertThat(chained.IsDead).IsTrue();
        AssertThat(laser.Xp).IsEqualApprox(2f, 0.0001f);
    }

    [TestCase]
    public void SummonedEnemy_GrantsRolledKindXp_ButZeroCoins()
    {
        var manager = CreateLevel3Manager();
        var tower = PlaceTower(manager, TowerType.Arrow, row: 4, col: 2);

        var waveManager = manager.WaveManager!;
        var waypoints = manager.Grid!.GetPathWaypoints(0);
        var summon = waveManager.SpawnSummon(
            EnemyKind.Armored,
            waypoints,
            waypointIndex: 0,
            anchorPosition: waypoints[0],
            offset: Vector2.Zero);

        AssertThat(summon).IsNotNull();
        AssertThat(summon!.CoinDrop).IsEqual(0);
        AssertThat(summon.XpValue).IsEqual(GameConstants.EnemyXpArmored);

        summon.TakeDamage(1000f, ignoreArmor: true, source: tower);

        AssertThat(summon.IsDead).IsTrue();
        AssertThat(tower.Xp).IsEqualApprox(3f, 0.0001f);
    }
}
