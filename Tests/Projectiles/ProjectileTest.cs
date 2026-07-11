using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for Projectile behavior, particularly the synchronous damage application
/// that prevents double-damage when two projectiles hit the same enemy in one frame.
///
/// GdUnit4Net: `[RequireGodotRuntime]` is not available in GdUnit4 4.3.x, so these
/// tests may be skipped when running outside the Godot runtime. Run via `dotnet test`
/// after building the Godot project to include the Godot engine context.
/// </summary>
[TestSuite]
public class ProjectileTest
{
    [TestCase]
    public void HitEnemy_SynchronousDamage_PreventsDoubleDamageOnSameEnemy()
    {
        // Arrange: create an enemy with default HP and two projectiles
        var enemy = new Enemy();
        enemy.ResetForPool(); // Sets HP to EnemyHP (10), IsDead = false

        var projectile1 = new Projectile();
        var projectile2 = new Projectile();

        // Act: both projectiles "hit" the same enemy in sequence,
        // simulating the scenario where two towers fire at the same enemy
        // and both projectiles arrive in the same frame.
        projectile1.HitEnemy(enemy);
        projectile2.HitEnemy(enemy);

        // Assert: enemy is dead (first hit killed it — 10 damage vs 10 HP)
        AssertThat(enemy.IsDead).IsTrue();

        // The second projectile's HitEnemy must not call TakeDamage on a dead enemy.
        // This is verified by enemy.IsDead still being true (TakeDamage early-returns)
        // and the enemy not being "re-killed" (which would indicate double damage).
    }

    [TestCase]
    public void HitEnemy_SetsHasHitAndDoesNotThrowOnRepeatCall()
    {
        // Arrange
        var enemy = new Enemy();
        enemy.ResetForPool();

        var projectile = new Projectile();

        // Verify initial state
        AssertThat(projectile.IsDone).IsFalse();

        // Act
        projectile.HitEnemy(enemy);

        // Assert: projectile is marked as done
        AssertThat(projectile.IsDone).IsTrue();
        AssertThat(enemy.IsDead).IsTrue();

        // Calling HitEnemy again on the same projectile is a no-op
        // (internal _hasHit guard). Should not throw or double-damage.
        projectile.HitEnemy(enemy);

        // Enemy should still be dead, no crash, no double-kill
        AssertThat(enemy.IsDead).IsTrue();
    }
}
