using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Cannon projectile: small dark circle that travels straight and explodes on first
/// enemy contact, dealing AoE damage to all enemies within ExplosionRadius of the impact point.
/// Dissipates with no explosion at max range.
/// </summary>
public partial class CannonProjectile : Projectile
{
    // Signal emitted when this cannonball explodes. GameManager applies AoE damage.
    [Signal]
    public delegate void ExplodedEventHandler(CannonProjectile projectile, Vector2 impactPosition);

    /// <summary>
    /// Radius (in pixels) within which the explosion damages enemies.
    /// </summary>
    public float ExplosionRadius => GameConstants.CannonTowerAoeRadius;

    /// <summary>
    /// Damage applied to every enemy within the explosion radius.
    /// </summary>
    public int ExplosionDamage => _damage;

    protected override void BuildVisual()
    {
        float size = GameConstants.ProjectileSize;

        var body = new Control();
        body.Name = "Cannonball";
        body.Size = new Vector2(size, size);
        body.Position = new Vector2(-size / 2f, -size / 2f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.Draw += () =>
        {
            if (!IsInstanceValid(body)) return;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            body.DrawCircle(center, size / 2f, new Color(0.15f, 0.15f, 0.2f));          // Dark fill
            body.DrawCircle(center, size / 2f, new Color(0.05f, 0.05f, 0.1f), false, 1.5f); // Outline
        };
        AddChild(body);
    }

    protected override void OnHit(Enemy enemy)
    {
        // No single-target damage here — the struck enemy is part of the AoE.
        // GameManager subscribes to Exploded and applies damage to every enemy in radius.
        EmitSignal(SignalName.Exploded, this, enemy.Position);
    }

    /// <summary>
    /// Applies explosion damage to every enemy in the given list within ExplosionRadius
    /// of the impact point. Callers should pass a snapshot (not a live mutable collection)
    /// because TakeDamage can trigger Destroyed signals that modify the active enemy list.
    /// </summary>
    public void Explode(IReadOnlyList<Enemy> enemies, Vector2 impactPosition, float damage)
    {
        foreach (var enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            if (IsWithinRadius(impactPosition, ExplosionRadius, enemy.Position))
                enemy.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Pure distance check: true if the point is within the radius of the center.
    /// </summary>
    public static bool IsWithinRadius(Vector2 center, float radius, Vector2 point)
    {
        return center.DistanceSquaredTo(point) <= radius * radius;
    }
}
