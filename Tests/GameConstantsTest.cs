using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure logic tests for game constants. No Godot runtime needed.
/// These tests verify all tuning parameters and helper calculations.
/// </summary>
[TestSuite]
public class GameConstantsTest
{
    [TestCase]
    public void GridConstants_AreCorrect()
    {
        AssertThat(GameConstants.GridCols).IsEqual(20);
        AssertThat(GameConstants.GridRows).IsEqual(14);
        AssertThat(GameConstants.CellSize).IsEqual(64);
        AssertThat(GameConstants.PathRow).IsEqual(10);
    }

    [TestCase]
    public void PlayerConstants_AreCorrect()
    {
        AssertThat(GameConstants.StartingHP).IsEqual(3);
        AssertThat(GameConstants.StartingCoins).IsEqual(10);
    }

    [TestCase]
    public void EnemyConstants_AreCorrect()
    {
        AssertThat(GameConstants.EnemyHP).IsEqual(10);
        AssertThat(GameConstants.EnemySpeed).IsEqual(2f);
        AssertThat(GameConstants.SpawnInterval).IsEqual(0.8f);
        AssertThat(GameConstants.EnemyDiameter).IsEqual(48);
        AssertThat(GameConstants.CoinDropPerKill).IsEqual(1);
    }

    [TestCase]
    public void ArrowTowerConstants_AreCorrect()
    {
        AssertThat(GameConstants.ArrowTowerRange).IsEqual(4);
        AssertThat(GameConstants.ArrowTowerFireRate).IsEqual(1.5f);
        AssertThat(GameConstants.ArrowTowerDamage).IsEqual(10);
        AssertThat(GameConstants.ArrowTowerCost).IsEqual(10);
    }

    [TestCase]
    public void ProjectileConstants_AreCorrect()
    {
        AssertThat(GameConstants.ProjectileSpeed).IsEqual(8f);
        AssertThat(GameConstants.ProjectileSize).IsEqual(12);
    }

    [TestCase]
    public void CellCenterX_CalculatesCorrectly()
    {
        int half = GameConstants.CellSize / 2;
        AssertThat(GameConstants.CellCenterX(0)).IsEqual(half);
        AssertThat(GameConstants.CellCenterX(5)).IsEqual(5 * GameConstants.CellSize + half);
        AssertThat(GameConstants.CellCenterX(19)).IsEqual(19 * GameConstants.CellSize + half);
    }

    [TestCase]
    public void CellCenterY_CalculatesCorrectly()
    {
        int half = GameConstants.CellSize / 2;
        AssertThat(GameConstants.CellCenterY(0)).IsEqual(half);
        AssertThat(GameConstants.CellCenterY(GameConstants.PathRow)).IsEqual(GameConstants.PathRow * GameConstants.CellSize + half);
        AssertThat(GameConstants.CellCenterY(13)).IsEqual(13 * GameConstants.CellSize + half);
    }

    [TestCase]
    public void CellDistanceInPixels_CalculatesCorrectly()
    {
        AssertThat(GameConstants.CellDistanceInPixels(0)).IsEqual(0f);
        AssertThat(GameConstants.CellDistanceInPixels(1)).IsEqual(64f);
        AssertThat(GameConstants.CellDistanceInPixels(4)).IsEqual(256f);
        AssertThat(GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange)).IsEqual(256f);
    }

    [TestCase]
    public void PlayAreaDimensions_AreCorrect()
    {
        AssertThat(GameConstants.PlayAreaWidth).IsEqual(1280);
        AssertThat(GameConstants.PlayAreaHeight).IsEqual(896);
    }

    [TestCase]
    public void SwarmEnemyHP_IsFive()
    {
        AssertThat(GameConstants.SwarmEnemyHP).IsEqual(5);
    }

    [TestCase]
    public void ArmoredEnemyConstants_AreCorrect()
    {
        AssertThat(GameConstants.ArmoredEnemyHP).IsEqual(14);
        AssertThat(GameConstants.ArmoredEnemyArmor).IsEqual(5);
        AssertThat(GameConstants.ArmoredEnemySpeed).IsEqual(2f);
        AssertThat(GameConstants.ArmoredEnemyDiameter).IsEqual(48);
        AssertThat(GameConstants.ArmoredCoinDropPerKill).IsEqual(2);
    }

    [TestCase]
    public void LaserTowerConstants_AreCorrect()
    {
        AssertThat(GameConstants.LaserTowerRange).IsEqual(3);
        AssertThat(GameConstants.LaserTowerDps).IsEqual(4f);
        AssertThat(GameConstants.LaserTowerCost).IsEqual(10);
    }

    [TestCase]
    public void TowerLookups_HandleLaser()
    {
        AssertThat(GameConstants.TowerCost(TowerType.Laser)).IsEqual(10);
        AssertThat(GameConstants.TowerRange(TowerType.Laser)).IsEqual(3);
        AssertThat(GameConstants.TowerCost(TowerType.Arrow)).IsEqual(10);
        AssertThat(GameConstants.TowerCost(TowerType.Cannon)).IsEqual(10);
    }
}
