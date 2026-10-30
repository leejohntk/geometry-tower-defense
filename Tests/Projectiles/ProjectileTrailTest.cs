using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Arrow orientation and projectile trails: the triangle tip points along travel, the
/// tail buffer is bounded/fading, and trail state resets on hit, dissipation, and reuse.
/// Trails are visual only and never change collision behavior.
/// </summary>
[TestSuite]
public class ProjectileTrailTest
{
    private static ArrowTower MakeArrowTower()
    {
        var tower = new ArrowTower();
        tower.Initialize(5, 5); // position (352, 352)
        return tower;
    }

    private static Enemy MakeEnemyAt(Vector2 position)
    {
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        enemy.Position = position;
        return enemy;
    }

    [TestCase]
    public void ArrowPolygon_HasItsTipForwardAlongPositiveX()
    {
        var polygon = ArrowProjectile.ArrowPolygon(GameConstants.ProjectileSize);

        AssertThat(polygon.Length).IsEqual(3);
        AssertThat(polygon[0]).IsEqual(new Vector2(GameConstants.ProjectileSize / 2f, 0f));

        float maxX = float.MinValue;
        foreach (var vertex in polygon)
            maxX = Mathf.Max(maxX, vertex.X);
        AssertThat(polygon[0].X).IsEqual(maxX);

        // Both rear vertices sit behind the tip.
        AssertThat(polygon[1].X).IsLess(0f);
        AssertThat(polygon[2].X).IsLess(0f);
    }

    [TestCase]
    public void ArrowProjectile_RotationMatchesTheTravelAngle()
    {
        var tower = MakeArrowTower();
        var enemy = MakeEnemyAt(tower.Position + new Vector2(200f, 0f));
        var arrow = new ArrowProjectile();
        try
        {
            arrow.Initialize(tower, tower.Position + new Vector2(200f, 0f), enemy);
            AssertThat(arrow.Rotation).IsEqual(0f); // +X

            arrow.Initialize(tower, tower.Position + new Vector2(0f, 200f), enemy);
            AssertThat(arrow.Rotation).IsEqualApprox(Mathf.Pi / 2f, 0.0001f); // +Y (down)

            arrow.Initialize(tower, tower.Position + new Vector2(0f, -200f), enemy);
            AssertThat(arrow.Rotation).IsEqualApprox(-Mathf.Pi / 2f, 0.0001f); // up
        }
        finally
        {
            arrow.Free();
        }
    }

    [TestCase]
    public void Trail_ResetSeedsOneSample_AndClearEmptiesIt()
    {
        var trail = ProjectileTrail.CreateArrowTrail();

        AssertThat(trail.Count).IsEqual(0);
        trail.Reset(new Vector2(4f, 9f));
        AssertThat(trail.Count).IsEqual(1);
        AssertThat(trail.PositionAt(0)).IsEqual(new Vector2(4f, 9f));
        AssertThat(trail.AgeAt(0)).IsEqual(0f);

        trail.Clear();
        AssertThat(trail.Count).IsEqual(0);
        AssertThat(trail.IsEmpty).IsTrue();
    }

    [TestCase]
    public void Trail_AdvancePushesSamplesAndStaysBounded()
    {
        var trail = ProjectileTrail.CreateArrowTrail();
        trail.Reset(Vector2.Zero);

        for (int i = 1; i <= 60; i++)
        {
            trail.Advance(new Vector2(i * 4f, 0f), 1f / 60f);
            AssertThat(trail.Count).IsLessEqual(trail.Capacity);
        }

        AssertThat(trail.Count).IsGreater(1);
        // Newest sample is first; older samples fade.
        AssertThat(trail.FadeAt(0)).IsGreaterEqual(trail.FadeAt(trail.Count - 1));
    }

    [TestCase]
    public void TrailVersion_ChangesOnlyWhenSamplesChange()
    {
        var trail = new ProjectileTrail(4, 0.12f, 0.04f);
        trail.Reset(Vector2.Zero);
        int afterReset = trail.Version;

        // Ages the existing sample but does not cross the sample interval or expire it:
        // the drawable history is unchanged, so the version must not move.
        trail.Advance(new Vector2(1f, 0f), 0.01f);
        AssertThat(trail.Version).IsEqual(afterReset);

        // Crossing the sample interval pushes a sample (the history changed).
        trail.Advance(new Vector2(2f, 0f), 0.04f);
        AssertThat(trail.Version).IsGreater(afterReset);
        int afterPush = trail.Version;

        // Another sub-interval frame with no expiry leaves the history unchanged.
        trail.Advance(new Vector2(3f, 0f), 0.001f);
        AssertThat(trail.Version).IsEqual(afterPush);

        trail.Clear();
        AssertThat(trail.Version).IsGreater(afterPush);
    }

    [TestCase]
    public void Trail_IgnoresNonPositiveAndNonFiniteDeltas()
    {
        var trail = ProjectileTrail.CreateCannonTrail();
        trail.Reset(Vector2.Zero);

        trail.Advance(new Vector2(10f, 0f), 0f);
        trail.Advance(new Vector2(10f, 0f), -1f);
        trail.Advance(new Vector2(10f, 0f), float.NaN);
        trail.Advance(new Vector2(10f, 0f), float.PositiveInfinity);

        AssertThat(trail.Count).IsEqual(1);
    }

    [TestCase]
    public void ArrowProjectile_TrailGrowsClearsOnHit_AndResetsOnReuse()
    {
        var tower = MakeArrowTower();
        var enemy = MakeEnemyAt(tower.Position);
        var arrow = new ArrowProjectile();
        try
        {
            arrow.Initialize(tower, tower.Position + new Vector2(200f, 0f), enemy);
            for (int i = 0; i < 8; i++)
                arrow._Process(1f / 60f);

            AssertThat(arrow.TrailSampleCount).IsGreater(1);
            AssertThat(arrow.IsDone).IsFalse();

            // Arrow damage (10) kills a basic enemy (10 HP): the consuming hit clears
            // the trail at the impact point.
            arrow.HitEnemy(enemy);
            AssertThat(enemy.IsDead).IsTrue();
            AssertThat(arrow.IsDone).IsTrue();
            AssertThat(arrow.TrailSampleCount).IsEqual(0);

            // Pool reuse resets the trail rather than appending to the old one.
            arrow.Initialize(tower, tower.Position + new Vector2(200f, 0f), enemy);
            AssertThat(arrow.TrailSampleCount).IsEqual(1);
        }
        finally
        {
            arrow.Free();
        }
    }

    [TestCase]
    public void ArrowProjectile_TrailClearsOnDissipation()
    {
        var tower = MakeArrowTower();
        var enemy = MakeEnemyAt(tower.Position);
        var arrow = new ArrowProjectile();
        try
        {
            arrow.Initialize(tower, tower.Position + new Vector2(10f, 0f), enemy);
            arrow._Process(0.05f);
            AssertThat(arrow.TrailSampleCount).IsGreater(0);

            // 8 cells/sec x 64 px x 1 s = 512 px, past the 4-cell (256 px) range.
            arrow._Process(1f);

            AssertThat(arrow.IsDone).IsTrue();
            AssertThat(arrow.TrailSampleCount).IsEqual(0);
        }
        finally
        {
            arrow.Free();
        }
    }

    [TestCase]
    public void CannonProjectile_TrailUsesFourPuffsAndClearsOnHit()
    {
        var tower = new CannonTower();
        tower.Initialize(5, 5);
        var enemy = MakeEnemyAt(tower.Position);
        var shell = new CannonProjectile();
        try
        {
            shell.Initialize(tower, tower.Position + new Vector2(200f, 0f), enemy);
            for (int i = 0; i < 20; i++)
                shell._Process(1f / 60f);

            AssertThat(shell.TrailSampleCount).IsGreater(1);
            AssertThat(shell.TrailSampleCount).IsLessEqual(GameConstants.CannonTrailPuffs);

            shell.HitEnemy(enemy);
            AssertThat(shell.IsDone).IsTrue();
            AssertThat(shell.TrailSampleCount).IsEqual(0);
        }
        finally
        {
            shell.Free();
        }
    }

    [TestCase]
    public void Trail_DoesNotChangeCollisionRadius()
    {
        var tower = MakeArrowTower();
        var enemy = MakeEnemyAt(tower.Position);
        var arrow = new ArrowProjectile();
        try
        {
            arrow.Initialize(tower, tower.Position + new Vector2(200f, 0f), enemy);

            float collisionRadius = GameConstants.ProjectileSize / 2f + enemy.CollisionRadius;
            enemy.Position = arrow.Position + new Vector2(collisionRadius - 1f, 0f);
            AssertThat(arrow.CheckCollision(enemy)).IsTrue();

            // Grow a trail; the collision envelope is unchanged.
            for (int i = 0; i < 10; i++)
                arrow._Process(1f / 60f);
            AssertThat(arrow.TrailSampleCount).IsGreater(0);

            Vector2 current = arrow.Position;
            enemy.Position = current + new Vector2(collisionRadius - 1f, 0f);
            AssertThat(arrow.CheckCollision(enemy)).IsTrue();
            enemy.Position = current + new Vector2(collisionRadius + 1f, 0f);
            AssertThat(arrow.CheckCollision(enemy)).IsFalse();
        }
        finally
        {
            arrow.Free();
        }
    }
}
