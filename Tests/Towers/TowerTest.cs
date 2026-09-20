using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for tower variants' stats and firing behavior.
/// </summary>
[TestSuite]
public class TowerTest
{
    [TestCase]
    public void ArrowTower_HasCorrectStats()
    {
        var tower = new ArrowTower();

        AssertThat(tower.Type).IsEqual(TowerType.Arrow);
        AssertThat(tower.RangeCells).IsEqual(4);
        AssertThat(tower.Damage).IsEqual(10);
        AssertThat(tower.FireRate).IsEqual(1.5f);
        AssertThat(tower.Cost).IsEqual(10);
    }

    [TestCase]
    public void CannonTower_HasCorrectStats()
    {
        var tower = new CannonTower();

        AssertThat(tower.Type).IsEqual(TowerType.Cannon);
        AssertThat(tower.RangeCells).IsEqual(4);
        AssertThat(tower.Damage).IsEqual(15);
        AssertThat(tower.FireRate).IsEqual(2.5f);
        AssertThat(tower.Cost).IsEqual(10);
    }

    [TestCase]
    public void LaserTower_HasCorrectStats()
    {
        var tower = new LaserTower();

        AssertThat(tower.Type).IsEqual(TowerType.Laser);
        AssertThat(tower.RangeCells).IsEqual(3);
        AssertThat(tower.Damage).IsEqual(0);
        AssertThat(tower.FireRate).IsEqual(0f);
        AssertThat(tower.Cost).IsEqual(15);
        AssertThat(tower.Dps).IsEqual(4f);
        AssertThat(tower.IsContinuous).IsTrue();
    }

    [TestCase]
    public void DiscreteTowers_AreNotContinuous_AndHaveZeroDps()
    {
        AssertThat(new ArrowTower().IsContinuous).IsFalse();
        AssertThat(new ArrowTower().Dps).IsEqual(0f);
        AssertThat(new CannonTower().IsContinuous).IsFalse();
        AssertThat(new CannonTower().Dps).IsEqual(0f);
    }

    [TestCase]
    public void Tower_Initialize_SetsGridPosition()
    {
        var tower = new ArrowTower();
        tower.Initialize(3, 5);

        AssertThat(tower.GridRow).IsEqual(3);
        AssertThat(tower.GridCol).IsEqual(5);
        AssertThat(tower.Position).IsEqual(new Vector2(
            GameConstants.CellCenterX(5),
            GameConstants.CellCenterY(3)
        ));
    }

    [TestCase]
    public void Tower_TryFire_FiresWithinRange_ThenCooldown()
    {
        var tower = new ArrowTower();
        tower.Initialize(0, 0); // position (32, 32)

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = new Vector2(32, 100); // 68px away, within 4-cell range

        AssertThat(tower.TryFire(enemy, out _)).IsTrue();
        AssertThat(tower.TryFire(enemy, out _)).IsFalse(); // cooldown active
    }
}
