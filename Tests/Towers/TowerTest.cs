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
        AssertThat(tower.RangeCells).IsEqual(4f);
        AssertThat(tower.Damage).IsEqual(10);
        AssertThat(tower.FireRate).IsEqual(1.5f);
        AssertThat(tower.Cost).IsEqual(10);
    }

    [TestCase]
    public void CannonTower_HasCorrectStats()
    {
        var tower = new CannonTower();

        AssertThat(tower.Type).IsEqual(TowerType.Cannon);
        AssertThat(tower.RangeCells).IsEqual(4f);
        AssertThat(tower.Damage).IsEqual(15);
        AssertThat(tower.FireRate).IsEqual(2.5f);
        AssertThat(tower.Cost).IsEqual(10);
    }

    [TestCase]
    public void LaserTower_HasCorrectStats()
    {
        var tower = new LaserTower();

        AssertThat(tower.Type).IsEqual(TowerType.Laser);
        AssertThat(tower.RangeCells).IsEqual(3f);
        AssertThat(tower.Damage).IsEqual(0);
        AssertThat(tower.FireRate).IsEqual(0f);
        AssertThat(tower.Cost).IsEqual(10);
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

    [TestCase]
    public void ArrowTower_AppliesSkillModifiers_AtRank5()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.ArrowDamage, 5);
        state.SetRank(SkillTreeCatalog.ArrowAttackSpeed, 5);
        state.SetRank(SkillTreeCatalog.ArrowRange, 5);

        var tower = new ArrowTower();
        tower.SetSkillTree(state);

        AssertThat(tower.Damage).IsEqual(20);
        AssertThat(tower.FireRate).IsEqual(1.0f);
        AssertThat(tower.RangeCells).IsEqual(6.5f);
    }

    [TestCase]
    public void CannonTower_AppliesSkillModifiers_AtRank5()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonPowderCharge, 5);
        state.SetRank(SkillTreeCatalog.CannonAttackSpeed, 5);
        state.SetRank(SkillTreeCatalog.CannonSplashRadius, 5);

        var tower = new CannonTower();
        tower.SetSkillTree(state);

        AssertThat(tower.Damage).IsEqual(20);
        AssertThat(tower.FireRate).IsEqual(2.5f / 1.5f);
        AssertThat(tower.SplashRadius).IsEqual(104);
        AssertThat(tower.ProjectileSpeedMultiplier).IsEqual(1.5f);
    }

    [TestCase]
    public void LaserTower_AppliesSkillModifiers_AtRank5()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserDps, 5);
        state.SetRank(SkillTreeCatalog.LaserRange, 5);

        var tower = new LaserTower();
        tower.SetSkillTree(state);

        AssertThat(tower.Dps).IsEqual(8f);
        AssertThat(tower.RangeCells).IsEqual(5.5f);
    }

    [TestCase]
    public void Towers_WithoutSkillState_UseBaseStats()
    {
        // No SetSkillTree call: every tower reports its unmodified base stats.
        AssertThat(new ArrowTower().RangeCells).IsEqual(4f);
        AssertThat(new CannonTower().Damage).IsEqual(15);
        AssertThat(new LaserTower().Dps).IsEqual(4f);
    }

    [TestCase]
    public void LaserTower_RampProgress_TracksTheMultiplier()
    {
        // 0 at the base multiplier (nothing ramped), 1 at the max (fully ramped).
        AssertThat(LaserTower.RampProgress(1f, 2f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(1.5f, 2f)).IsEqual(0.5f);
        AssertThat(LaserTower.RampProgress(2f, 2f)).IsEqual(1f);
    }

    [TestCase]
    public void LaserTower_RampProgress_IsZeroWithoutRampNodes_OrForBadInput()
    {
        // A laser with no Ramp-Up ranks has max 1.0x: the beam must stay at its
        // un-ramped look rather than dividing by zero.
        AssertThat(LaserTower.RampProgress(1f, 1f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(1f, 0f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(1.2f, 0.8f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(float.NaN, 2f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(float.PositiveInfinity, 2f)).IsEqual(0f);
    }

    [TestCase]
    public void LaserTower_RampProgress_ClampsOutOfRangeMultipliers()
    {
        AssertThat(LaserTower.RampProgress(0.5f, 2f)).IsEqual(0f);
        AssertThat(LaserTower.RampProgress(3f, 2f)).IsEqual(1f);
    }

    [TestCase]
    public void LaserTower_RampProgress_MatchesTheMultiplierUpdateRampReturns()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.LaserRampUp, 5); // max 2.0x
        var laser = new LaserTower();
        laser.SetSkillTree(state);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);

        AssertThat(laser.UpdateRamp(enemy, 0f)).IsEqual(1f);
        AssertThat(LaserTower.RampProgress(1f, laser.RampMaxMultiplier)).IsEqual(0f);

        // Half the ramp time in: half-way to the max multiplier, so half beam intensity.
        float half = laser.UpdateRamp(enemy, GameConstants.SkillLaserRampTime / 2f);
        AssertThat(LaserTower.RampProgress(half, laser.RampMaxMultiplier)).IsEqual(0.5f);

        float full = laser.UpdateRamp(enemy, GameConstants.SkillLaserRampTime / 2f);
        AssertThat(LaserTower.RampProgress(full, laser.RampMaxMultiplier)).IsEqual(1f);
    }

    [TestCase]
    public void LaserTower_BeamLevels_IntensifyFromMinToMax()
    {
        // Guards the direction of the feedback: the ramped beam must be wider and its
        // color must keep the same alpha, so intensity reads as "brighter", not "thinner".
        AssertThat(GameConstants.LaserBeamWidthMax).IsGreater(GameConstants.LaserBeamWidthMin);
        AssertThat(GameConstants.LaserChainBeamWidthMax).IsGreater(GameConstants.LaserChainBeamWidthMin);
        AssertThat(GameConstants.LaserBeamColorMax.A).IsGreaterEqual(GameConstants.LaserBeamColorMin.A);
        AssertThat(GameConstants.LaserChainBeamColorMax.A).IsGreaterEqual(GameConstants.LaserChainBeamColorMin.A);
    }

    [TestCase]
    public void CannonTower_SingleShell_ShowsNoMuzzleFlash()
    {
        var tower = new CannonTower();
        tower.Initialize(0, 0); // position (32, 32)

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = new Vector2(32, 100); // in range

        AssertThat(tower.ClusterCount).IsEqual(1);
        AssertThat(tower.TryFire(enemy, out _)).IsTrue();

        // One shell is the un-cued baseline: the volley cue means "this was a cluster".
        AssertThat(tower.IsMuzzleFlashing).IsFalse();
    }

    [TestCase]
    public void CannonTower_ClusterVolley_FlashesTheMuzzle()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonCluster, 2); // 3 shells
        var tower = new CannonTower();
        tower.Initialize(0, 0);
        tower.SetSkillTree(state);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = new Vector2(32, 100);

        AssertThat(tower.ClusterCount).IsEqual(3);
        AssertThat(tower.IsMuzzleFlashing).IsFalse();

        AssertThat(tower.TryFire(enemy, out _)).IsTrue();
        AssertThat(tower.IsMuzzleFlashing).IsTrue();
    }

    [TestCase]
    public void CannonTower_MuzzleFlash_ExpiresAfterItsDuration()
    {
        var state = new SkillTreeState();
        state.SetRank(SkillTreeCatalog.CannonCluster, 1); // 2 shells
        var tower = new CannonTower();
        tower.Initialize(0, 0);
        tower.SetSkillTree(state);

        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = new Vector2(32, 100);

        tower.TryFire(enemy, out _);
        AssertThat(tower.IsMuzzleFlashing).IsTrue();

        // Ticked at ~60 fps: the flash is gone well before the next volley's cooldown.
        float step = 1f / 60f;
        int frames = 0;
        while (tower.IsMuzzleFlashing && frames < 1000)
        {
            tower._Process(step);
            frames++;
        }

        AssertThat(tower.IsMuzzleFlashing).IsFalse();
        AssertThat(frames * step)
            .IsGreaterEqual(GameConstants.CannonMuzzleFlashDuration);
        AssertThat(frames * step)
            .IsLessEqual(GameConstants.CannonMuzzleFlashDuration + 2f * step);
        // The cue is far shorter than the firing cooldown, so volleys never smear.
        AssertThat(GameConstants.CannonMuzzleFlashDuration).IsLess(tower.FireRate);
    }
}
