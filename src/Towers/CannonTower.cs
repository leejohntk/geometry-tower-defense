using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Cannon Tower: dark green filled circle with dark outline.
/// Same range as Arrow (4 cells), slower fire rate (2.5s), projectile explodes
/// on contact dealing AoE damage.
/// </summary>
public partial class CannonTower : Tower
{
    public override TowerType Type => TowerType.Cannon;
    public override int Damage => SkillStats.CannonDamage(SkillRank(SkillTreeCatalog.CannonPowderCharge));
    public override float FireRate => SkillStats.CannonFireRate(SkillRank(SkillTreeCatalog.CannonAttackSpeed));
    public override int Cost => GameConstants.CannonTowerCost;
    public override float ProjectileSpeedMultiplier => SkillStats.CannonProjectileSpeedMultiplier(SkillRank(SkillTreeCatalog.CannonPowderCharge));
    public override int ClusterCount => SkillMechanics.CannonClusterCount(SkillRank(SkillTreeCatalog.CannonCluster));
    public override float StunChance => SkillMechanics.CannonStunChance(SkillRank(SkillTreeCatalog.CannonStunChance));

    /// <summary>
    /// Final AoE radius (px) with the Splash Radius node applied.
    /// </summary>
    public override float SplashRadius => SkillStats.CannonSplashRadius(SkillRank(SkillTreeCatalog.CannonSplashRadius));
    protected override Color RangeColor => new Color(0.3f, 0.6f, 0.4f);

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4;

        var body = new Control();
        body.Name = "CannonBody";
        body.Size = new Vector2(cell, cell);
        body.Position = new Vector2(-cell / 2f, -cell / 2f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.Draw += () =>
        {
            if (!IsInstanceValid(body)) return;

            Vector2 center = new Vector2(cell / 2f, cell / 2f);

            // Filled circle (cannon barrel) with dark outline
            body.DrawCircle(center, radius, new Color(0.3f, 0.6f, 0.4f));
            body.DrawCircle(center, radius, new Color(0.1f, 0.25f, 0.15f), false, 2.0f);

            // Small inner barrel circle
            body.DrawCircle(center, radius * 0.4f, new Color(0.12f, 0.28f, 0.17f));
        };
        AddChild(body);
    }
}
