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
    /// Radius (in pixels) within which the explosion damages enemies. When the source
    /// tower is known this is the tower's skill-modified splash radius; otherwise it
    /// falls back to the base constant. Read generically via <see cref="Tower.SplashRadius"/>
    /// so future splash-like towers (Part 2) need no concrete-type test here.
    /// </summary>
    public float ExplosionRadius => SourceTower is { SplashRadius: > 0f } tower
        ? tower.SplashRadius
        : GameConstants.CannonTowerAoeRadius;

    /// <summary>
    /// Damage applied to every enemy within the explosion radius. Float, so a rank
    /// damage multiplier reaches armor uncut.
    /// </summary>
    public float ExplosionDamage => _damage;

    // Smoke puffs behind the cannonball. Pre-created once per pooled shell; each frame
    // only transform/modulate is written, never a redraw and never an allocation.
    private readonly Polygon2D[] _smokePuffs = new Polygon2D[GameConstants.CannonTrailPuffs];
    private int _lastSmokeVersion = -1;

    protected override void BuildTrailVisual()
    {
        var puffPolygon = BuildCirclePolygon(GameConstants.CannonTrailPuffRadius, 12);
        for (int i = 0; i < _smokePuffs.Length; i++)
        {
            var puff = new Polygon2D();
            puff.Name = $"SmokePuff_{i}";
            puff.Polygon = puffPolygon;
            puff.Color = GameConstants.CannonTrailColor;
            // World-space samples, like the arrow trail.
            puff.TopLevel = true;
            puff.ZIndex = -1;
            puff.Visible = false;
            AddChild(puff);
            _smokePuffs[i] = puff;
        }
    }

    protected override void UpdateTrailVisual()
    {
        if (_trail == null)
            return;

        int count = _trail.Count;
        bool positionsChanged = _trail.Version != _lastSmokeVersion;

        for (int i = 0; i < _smokePuffs.Length; i++)
        {
            var puff = _smokePuffs[i];
            if (puff == null || !IsInstanceValid(puff))
                continue;

            bool used = i < count;
            puff.Visible = used;
            if (!used)
                continue;

            // Sample positions only move when the history changes; the fade/growth
            // curve is a per-frame transform because sample ages advance every frame.
            if (positionsChanged)
                puff.Position = _trail.PositionAt(i);

            float fade = _trail.FadeAt(i);
            float age = 1f - fade;
            puff.Modulate = new Color(1f, 1f, 1f, fade * 0.8f);
            float scale = 0.7f + 0.8f * age;
            puff.Scale = new Vector2(scale, scale);
        }

        _lastSmokeVersion = _trail.Version;
    }

    protected override ProjectileTrail? CreateTrail() => ProjectileTrail.CreateCannonTrail();

    /// <summary>
    /// Builds a regular polygon approximating a circle. Allocated once per pooled
    /// projectile (never per frame) and shared by every smoke puff.
    /// </summary>
    private static Vector2[] BuildCirclePolygon(float radius, int sides)
    {
        var points = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.Tau * i / sides;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

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
    /// of the impact point, and rolls the source tower's stun chance per surviving
    /// enemy hit. Callers should pass a snapshot (not a live mutable collection)
    /// because TakeDamage can trigger Destroyed signals that modify the active enemy list.
    /// Index-based iteration keeps the per-explosion loop allocation-free.
    /// </summary>
    public void Explode(IReadOnlyList<Enemy> enemies, Vector2 impactPosition, float damage)
    {
        // Hoisted out of the loop: ExplosionRadius walks a virtual chain, so read it once.
        float radius = ExplosionRadius;
        float stunChance = SourceTower?.StunChance ?? 0f;
        float stunDuration = GameConstants.SkillCannonStunDuration;
        SkillRandom? rolls = SourceTower?.Rolls;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy.IsDead)
                continue;

            if (!IsWithinRadius(impactPosition, radius, enemy.Position))
                continue;

            // Armor still reduces the stunning hit's damage — stun is rolled after
            // the normal damaging hit, and only matters if the enemy survived it.
            // The source tower is passed through so every enemy in the blast credits
            // the cannon; the direct target is in this list once, so it credits once.
            enemy.TakeDamage(damage, source: SourceTower);
            if (enemy.IsDead)
                continue;

            if (stunChance > 0f && rolls != null && rolls.Roll(stunChance))
                enemy.ApplyStun(stunDuration);
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
