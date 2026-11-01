using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tower aim/recoil presentation: capped angular approach, target tracking, easing back
/// to the default orientation, placement reset, and the per-shot recoil cue that is
/// separate from the cannon's cluster-only muzzle flash. No gameplay stat is asserted
/// differently here — only that the visual state behaves.
/// </summary>
[TestSuite]
public class TowerAimTest
{
    private static Enemy MakeEnemyAt(Vector2 position)
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = position;
        return enemy;
    }

    private static void Simulate(Tower tower, int frames)
    {
        for (int i = 0; i < frames; i++)
            tower._Process(1f / 60f);
    }

    [TestCase]
    public void ApproachAngle_MovesAtMostTheCap_AndSnapsWhenClose()
    {
        AssertThat(Tower.ApproachAngle(0f, 1f, 0.25f)).IsEqual(0.25f);
        AssertThat(Tower.ApproachAngle(0f, 0.1f, 0.25f)).IsEqual(0.1f);
        AssertThat(Tower.ApproachAngle(0f, 3f, 0.5f)).IsEqual(0.5f);
        AssertThat(Tower.ApproachAngle(0f, 3f, 10f)).IsEqual(3f);
    }

    [TestCase]
    public void ApproachAngle_TakesTheShortestWayAroundTheCircle()
    {
        // 3 rad -> -3 rad is a +0.283 rad step across the seam, not a -6 rad sweep.
        AssertThat(Tower.ApproachAngle(3f, -3f, 1f)).IsEqual(-3f);
        AssertThat(Tower.ApproachAngle(-3f, 3f, 1f)).IsEqual(3f);
    }

    [TestCase]
    public void ApproachAngle_GuardsDegenerateInputs()
    {
        AssertThat(Tower.ApproachAngle(1f, 2f, 0f)).IsEqual(1f);
        AssertThat(Tower.ApproachAngle(1f, 2f, -1f)).IsEqual(1f);
        AssertThat(Tower.ApproachAngle(float.NaN, 2f, 1f)).IsEqual(2f);
        AssertThat(Tower.ApproachAngle(1f, float.NaN, 1f)).IsEqual(1f);
    }

    [TestCase]
    public void ArrowTower_RestsPointingUp()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);

        AssertThat(tower.AimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(tower.DesiredAimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(tower.DefaultAimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
    }

    [TestCase]
    public void CannonAndLaser_RestPointingRight()
    {
        var cannon = new CannonTower();
        cannon.Initialize(5, 5);
        var laser = new LaserTower();
        laser.Initialize(5, 5);

        AssertThat(cannon.DefaultAimAngle).IsEqual(0f);
        AssertThat(cannon.AimAngle).IsEqual(0f);
        AssertThat(laser.DefaultAimAngle).IsEqual(0f);
        AssertThat(laser.AimAngle).IsEqual(0f);
    }

    [TestCase]
    public void Aim_ConvergesSmoothlyInsteadOfSnapping()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
        tower.SetTarget(enemy);

        AssertThat(tower.DesiredAimAngle).IsEqualApprox(0f, 0.0001f);

        // One frame: moved toward the target by the capped step, not snapped.
        tower._Process(1f / 60f);
        float afterOneFrame = tower.AimAngle;
        AssertThat(afterOneFrame).IsGreater(-Mathf.Pi / 2f);
        AssertThat(afterOneFrame).IsLess(0f);
        AssertThat(Mathf.Abs(afterOneFrame - (-Mathf.Pi / 2f)))
            .IsLessEqual(GameConstants.TowerAimAngularSpeed / 60f + 0.0001f);

        Simulate(tower, 120);
        AssertThat(tower.AimAngle).IsEqualApprox(0f, 0.0001f);
    }

    [TestCase]
    public void Aim_EasesBackToDefault_WhenTheTargetIsLost()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
        tower.SetTarget(enemy);
        Simulate(tower, 120);
        AssertThat(tower.AimAngle).IsEqualApprox(0f, 0.0001f);

        tower.SetTarget(null);
        Simulate(tower, 120);

        AssertThat(tower.AimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
    }

    [TestCase]
    public void Aim_EasesBack_WhenTheTargetDies()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
        tower.SetTarget(enemy);
        Simulate(tower, 120);

        enemy.TakeDamage(1000f); // dead, still assigned
        AssertThat(enemy.IsDead).IsTrue();
        AssertThat(tower.DesiredAimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);

        Simulate(tower, 120);
        AssertThat(tower.AimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
    }

    [TestCase]
    public void Aim_EasesBack_WhenTheTargetLeavesRange()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
        tower.SetTarget(enemy);
        Simulate(tower, 120);

        enemy.Position = tower.Position + new Vector2(tower.RangePixels + 50f, 0f);
        AssertThat(tower.DesiredAimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
    }

    [TestCase]
    public void Placement_ResetsAimToTheDefaultOrientation()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
        tower.SetTarget(enemy);
        Simulate(tower, 120);
        AssertThat(Mathf.Abs(tower.AimAngle - (-Mathf.Pi / 2f))).IsGreater(0.01f);

        tower.Initialize(1, 1);

        AssertThat(tower.AimAngle).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f);
        AssertThat(tower.IsRecoiling).IsFalse();
    }

    [TestCase]
    public void Aiming_NeverMovesTheTower()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        Vector2 positionBefore = tower.Position;
        var enemy = MakeEnemyAt(tower.Position + new Vector2(0f, 100f));
        tower.SetTarget(enemy);

        Simulate(tower, 60);

        AssertThat(tower.Position).IsEqual(positionBefore);
        AssertThat(tower.GridRow).IsEqual(5);
        AssertThat(tower.GridCol).IsEqual(5);
    }

    [TestCase]
    public void EveryShot_StartsTheRecoilCue_AndItExpires()
    {
        var cannon = new CannonTower();
        cannon.Initialize(5, 5);
        var enemy = MakeEnemyAt(cannon.Position + new Vector2(100f, 0f));

        AssertThat(cannon.IsRecoiling).IsFalse();
        AssertThat(cannon.TryFire(enemy, out _)).IsTrue();

        AssertThat(cannon.IsRecoiling).IsTrue();
        AssertThat(cannon.RecoilProgress).IsEqual(1f);
        // Single shell: recoil is visible but the cluster-only muzzle flash stays off.
        AssertThat(cannon.IsMuzzleFlashing).IsFalse();

        cannon._Process(GameConstants.TowerShotRecoilDuration + 0.001f);

        AssertThat(cannon.IsRecoiling).IsFalse();
        AssertThat(cannon.RecoilProgress).IsEqual(0f);
    }

    [TestCase]
    public void ArrowBody_RotatesWithAim_AndKicksBackDuringRecoil()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(tower);
        try
        {
            var enemy = MakeEnemyAt(tower.Position + new Vector2(100f, 0f));
            tower.SetTarget(enemy);
            Simulate(tower, 120);

            var body = tower.GetNodeOrNull<Control>("ArrowBody");
            AssertThat(body).IsNotNull();
            // Drawn tip-up; aiming right is a +90-degree body rotation.
            AssertThat(body!.Rotation).IsEqualApprox(
                tower.AimAngle - tower.DefaultAimAngle, 0.0001f);
            AssertThat(tower.AimAngle).IsEqualApprox(0f, 0.0001f);

            // A shot kicks the body back along the aim axis, never the tower itself.
            Vector2 towerPosition = tower.Position;
            AssertThat(tower.TryFire(enemy, out _)).IsTrue();
            tower._Process(0f);
            AssertThat(body.Position.X).IsLess(-32f);
            AssertThat(tower.Position).IsEqual(towerPosition);

            // After the recoil the body returns to its base offset.
            tower._Process(GameConstants.TowerShotRecoilDuration + 0.001f);
            AssertThat(body.Position).IsEqual(new Vector2(-32f, -32f));
        }
        finally
        {
            tower.GetParent()?.RemoveChild(tower);
            tower.QueueFree();
        }
    }

    [TestCase]
    public void CannonBody_RotatesWithAim_AndTheRankBadgeStaysAbove()
    {
        var tower = new CannonTower();
        tower.Initialize(5, 5);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(tower);
        try
        {
            var enemy = MakeEnemyAt(tower.Position + new Vector2(0f, 100f));
            tower.SetTarget(enemy);
            Simulate(tower, 120);

            var body = tower.GetNodeOrNull<Control>("CannonBody");
            AssertThat(body).IsNotNull();
            // Aim down: atan2(100, 0) = +90 degrees.
            AssertThat(tower.AimAngle).IsEqualApprox(Mathf.Pi / 2f, 0.0001f);
            AssertThat(body!.Rotation).IsEqualApprox(Mathf.Pi / 2f, 0.0001f);

            // The rank badge keeps its top-right position and z-order above the body.
            tower.ApplyRankUpgrade();
            var badge = tower.GetNodeOrNull<Control>("RankBadge");
            AssertThat(badge).IsNotNull();
            AssertThat(badge!.ZIndex).IsGreater(0);
        }
        finally
        {
            tower.GetParent()?.RemoveChild(tower);
            tower.QueueFree();
        }
    }
}
