using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Shape category of a skill node: trunk nodes sit along the tower's main upgrade
/// line, seed nodes branch off into mechanics (archetype-defining effects).
/// </summary>
public enum SkillNodeKind
{
    Trunk,
    Seed
}

/// <summary>
/// Immutable definition of a single skill-tree node.
/// </summary>
public sealed class SkillNodeDefinition
{
    public string Id { get; }
    public TowerType TowerType { get; }
    public string DisplayName { get; }
    public SkillNodeKind Kind { get; }
    public bool Enabled { get; }

    /// <summary>
    /// Player-facing description of the node's real per-rank effect, with concrete
    /// numbers. Seed nodes begin with "Archetype: ..." and explain the tower rank-3
    /// specialization relationship.
    /// </summary>
    public string Description { get; }

    public SkillNodeDefinition(
        string id,
        TowerType towerType,
        string displayName,
        SkillNodeKind kind,
        bool enabled,
        string description)
    {
        Id = id;
        TowerType = towerType;
        DisplayName = displayName;
        Kind = kind;
        Enabled = enabled;
        Description = description;
    }
}

/// <summary>
/// Static catalog of every skill-tree node across all three towers.
///
/// All 15 nodes are defined here so the full tree shape renders from day one.
/// All 15 are enabled as of Part 2: the 8 stat nodes (effective since Part 1) plus
/// the 7 mechanic nodes (behavior-changing seeds and the Laser Ignite trunk).
/// </summary>
public static class SkillTreeCatalog
{
    // Arrow node ids
    public const string ArrowDamage = "arrow.damage";
    public const string ArrowAttackSpeed = "arrow.attack_speed";
    public const string ArrowRange = "arrow.range";
    public const string ArrowPierce = "arrow.pierce";
    public const string ArrowCritChance = "arrow.crit_chance";

    // Cannon node ids
    public const string CannonPowderCharge = "cannon.powder_charge";
    public const string CannonAttackSpeed = "cannon.attack_speed";
    public const string CannonSplashRadius = "cannon.splash_radius";
    public const string CannonCluster = "cannon.cluster";
    public const string CannonStunChance = "cannon.stun_chance";

    // Laser node ids
    public const string LaserDps = "laser.dps";
    public const string LaserRange = "laser.range";
    public const string LaserIgnite = "laser.ignite";
    public const string LaserChain = "laser.chain";
    public const string LaserRampUp = "laser.ramp_up";

    /// <summary>
    /// All nodes in display order (Arrow, Cannon, Laser).
    /// </summary>
    public static readonly IReadOnlyList<SkillNodeDefinition> All = new SkillNodeDefinition[]
    {
        // Arrow Tower — Sharpshooter | Barrage
        new(ArrowDamage, TowerType.Arrow, "Damage", SkillNodeKind.Trunk, enabled: true,
            "+2 damage per rank."),
        new(ArrowAttackSpeed, TowerType.Arrow, "Attack Speed", SkillNodeKind.Trunk, enabled: true,
            "+10% attack speed per rank."),
        new(ArrowRange, TowerType.Arrow, "Range", SkillNodeKind.Trunk, enabled: true,
            "+0.5 range per rank."),
        new(ArrowPierce, TowerType.Arrow, "Pierce", SkillNodeKind.Seed, enabled: true,
            "Archetype: Pierce. At tower rank 3 choose Pierce to pass through 1 enemy per rank before the arrow is consumed."),
        new(ArrowCritChance, TowerType.Arrow, "Crit Chance", SkillNodeKind.Seed, enabled: true,
            "Archetype: Crit. At tower rank 3 choose Crit for 8% crit chance per rank; crits deal double damage."),

        // Cannon Tower — Bombardier | Concussive
        new(CannonPowderCharge, TowerType.Cannon, "Powder Charge", SkillNodeKind.Trunk, enabled: true,
            "+1 damage and +10% projectile speed per rank."),
        new(CannonAttackSpeed, TowerType.Cannon, "Attack Speed", SkillNodeKind.Trunk, enabled: true,
            "+10% attack speed per rank."),
        new(CannonSplashRadius, TowerType.Cannon, "Splash Radius", SkillNodeKind.Trunk, enabled: true,
            "+8 splash radius per rank."),
        new(CannonCluster, TowerType.Cannon, "Cluster", SkillNodeKind.Seed, enabled: true,
            "Archetype: Cluster. At tower rank 3 choose Cluster to fire 1 + rank shells in a small fan."),
        new(CannonStunChance, TowerType.Cannon, "Stun Chance", SkillNodeKind.Seed, enabled: true,
            "Archetype: Stun. At tower rank 3 choose Stun for 6% stun chance per rank for 1s."),

        // Laser Tower — Arc/Chain | Melter
        new(LaserDps, TowerType.Laser, "DPS", SkillNodeKind.Trunk, enabled: true,
            "+0.8 damage per second per rank."),
        new(LaserRange, TowerType.Laser, "Range", SkillNodeKind.Trunk, enabled: true,
            "+0.5 range per rank."),
        new(LaserIgnite, TowerType.Laser, "Ignite", SkillNodeKind.Trunk, enabled: true,
            "5% ignite chance per second per rank: 2 damage per second for 2s, ignoring armor."),
        new(LaserChain, TowerType.Laser, "Chain", SkillNodeKind.Seed, enabled: true,
            "Archetype: Chain. At tower rank 3 choose Chain to jump to 1 extra enemy per rank within 1.5 cells at 60% damage."),
        new(LaserRampUp, TowerType.Laser, "Ramp-Up", SkillNodeKind.Seed, enabled: true,
            "Archetype: Ramp-Up. At tower rank 3 choose Ramp-Up for +20% max damage per rank while holding a target for 2s."),
    };

    /// <summary>
    /// Player-facing description for a node id, or an empty string for an unknown id.
    /// </summary>
    public static string Description(string nodeId) => Find(nodeId)?.Description ?? string.Empty;

    /// <summary>
    /// Human-readable prerequisite hint for a still-locked node, e.g.
    /// "Unlocks after Damage reaches rank 1." Returns null when the node has no
    /// prerequisite (the first trunk node) or the id is unknown.
    /// </summary>
    public static string? UnlockHint(string nodeId)
    {
        var prerequisite = GetPrerequisite(nodeId);
        if (prerequisite == null)
            return null;

        var def = Find(prerequisite);
        if (def == null)
            return null;

        return $"Unlocks after {def.DisplayName} reaches rank {GameConstants.SkillUnlockRank}.";
    }

    /// <summary>
    /// Finds a node definition by id, or null if the id is unknown.
    /// </summary>
    public static SkillNodeDefinition? Find(string id)
    {
        foreach (var node in All)
        {
            if (node.Id == id)
                return node;
        }
        return null;
    }

    /// <summary>
    /// Enumerates the nodes belonging to the given tower in catalog order.
    /// </summary>
    public static IEnumerable<SkillNodeDefinition> ForTower(TowerType towerType)
    {
        foreach (var node in All)
        {
            if (node.TowerType == towerType)
                yield return node;
        }
    }

    /// <summary>
    /// Returns the id of the node that must reach <see cref="GameConstants.SkillUnlockRank"/>
    /// before the given node can be bought, or null when there is no prerequisite.
    /// Derived uniformly from the tower's tree shape — never per-node hardcoding:
    /// the first trunk node is always unlocked, each later trunk node requires the
    /// previous trunk, and each seed requires the tower's last trunk. Unknown ids and
    /// seeds on a trunk-less tower return null.
    /// </summary>
    public static string? GetPrerequisite(string nodeId)
    {
        var def = Find(nodeId);
        if (def == null)
            return null;

        var trunks = new List<SkillNodeDefinition>();
        foreach (var node in ForTower(def.TowerType))
        {
            if (node.Kind == SkillNodeKind.Trunk)
                trunks.Add(node);
        }

        if (trunks.Count == 0)
            return null;

        if (def.Kind == SkillNodeKind.Trunk)
        {
            for (int i = 0; i < trunks.Count; i++)
            {
                if (trunks[i].Id == nodeId)
                    return i == 0 ? null : trunks[i - 1].Id;
            }
            return null; // defensive: a known trunk node is always in the list
        }

        // Seed node: gated by the tower's last trunk.
        return trunks[^1].Id;
    }
}
