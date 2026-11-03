using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Tower this node belongs to, or null for a tower-independent global node.
    /// </summary>
    public TowerType? TowerType { get; }
    public string DisplayName { get; }
    public SkillNodeKind Kind { get; }
    public bool Enabled { get; }

    /// <summary>
    /// Maximum purchasable rank for this node. Tower nodes allow
    /// <see cref="GameConstants.SkillMaxRanks"/>; the global nodes define their own caps.
    /// </summary>
    public int MaxRanks { get; }

    /// <summary>
    /// True when this node is not tied to a tower type and therefore must never appear
    /// on a tower tab (and tower nodes never appear on the global tab).
    /// </summary>
    public bool IsGlobal => TowerType == null;

    /// <summary>
    /// Player-facing description of the node's real per-rank effect, with concrete
    /// numbers. Seed nodes begin with "Archetype: ..." and explain the tower rank-3
    /// specialization relationship.
    /// </summary>
    public string Description { get; }

    public SkillNodeDefinition(
        string id,
        TowerType? towerType,
        string displayName,
        SkillNodeKind kind,
        bool enabled,
        string description,
        int maxRanks = GameConstants.SkillMaxRanks)
    {
        Id = id;
        TowerType = towerType;
        DisplayName = displayName;
        Kind = kind;
        Enabled = enabled;
        Description = description;
        MaxRanks = maxRanks;
    }
}

/// <summary>
/// Static catalog of every skill-tree node: the three tower trees plus the
/// tower-independent global branch.
///
/// All 15 tower nodes are defined here so the full tree shape renders from day one.
/// All 15 are enabled as of Part 2: the 8 stat nodes (effective since Part 1) plus
/// the 7 mechanic nodes (behavior-changing seeds and the Laser Ignite trunk).
/// The 4 global nodes spend the same SP and chain sequentially.
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

    // Global node ids (tower-independent, shared by every run)
    public const string GlobalStartingHp = "global.starting_hp";
    public const string GlobalStartingGold = "global.starting_gold";
    public const string GlobalTowerDiscount = "global.tower_discount";
    public const string GlobalUpgradeDiscount = "global.upgrade_discount";

    /// <summary>
    /// Global nodes in display order (the sequential unlock chain).
    /// </summary>
    public static readonly IReadOnlyList<SkillNodeDefinition> GlobalNodes = new SkillNodeDefinition[]
    {
        new(GlobalStartingHp, null, "Fortified Core", SkillNodeKind.Trunk, enabled: true,
            $"+{GameConstants.GlobalStartingHpPerRank} starting HP per rank.",
            maxRanks: GameConstants.GlobalStartingHpMaxRanks),
        new(GlobalStartingGold, null, "War Chest", SkillNodeKind.Trunk, enabled: true,
            $"+{GameConstants.GlobalStartingGoldPerRank} starting gold per rank.",
            maxRanks: GameConstants.GlobalStartingGoldMaxRanks),
        new(GlobalTowerDiscount, null, "Master Blueprints", SkillNodeKind.Trunk, enabled: true,
            $"Tower placement costs {GameConstants.GlobalTowerDiscountPerRank} less coin per rank, " +
            $"to a minimum of {GameConstants.MinimumEffectiveCost}.",
            maxRanks: GameConstants.GlobalTowerDiscountMaxRanks),
        new(GlobalUpgradeDiscount, null, "Advanced Training", SkillNodeKind.Trunk, enabled: true,
            $"Every tower rank upgrade costs {GameConstants.GlobalUpgradeDiscountPerRank} less coin per rank, " +
            $"to a minimum of {GameConstants.MinimumEffectiveCost}.",
            maxRanks: GameConstants.GlobalUpgradeDiscountMaxRanks),
    };

    private static readonly SkillNodeDefinition[] TowerNodes = new SkillNodeDefinition[]
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
    /// All nodes in display order (Arrow, Cannon, Laser, Global). The tower tree is
    /// composed with the global chain so neither list is duplicated by hand.
    /// </summary>
    public static readonly IReadOnlyList<SkillNodeDefinition> All =
        TowerNodes.Concat(GlobalNodes).ToArray();

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
    /// Enumerates the nodes belonging to the given tower in catalog order. Global
    /// (tower-independent) nodes are never returned.
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
    /// Global nodes chain in <see cref="GlobalNodes"/> order; tower nodes are derived
    /// uniformly from the tower's tree shape — never per-node hardcoding: the first
    /// trunk node is always unlocked, each later trunk node requires the previous
    /// trunk, and each seed requires the tower's last trunk. Unknown ids and seeds on
    /// a trunk-less tower return null.
    /// </summary>
    public static string? GetPrerequisite(string nodeId)
    {
        var def = Find(nodeId);
        if (def == null)
            return null;

        // Globals and tower trunks are both ordered chains; one walk handles both.
        IReadOnlyList<SkillNodeDefinition> chain = def.IsGlobal
            ? GlobalNodes
            : TrunksFor(def.TowerType!.Value);

        if (chain.Count == 0)
            return null;

        if (def.IsGlobal || def.Kind == SkillNodeKind.Trunk)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i].Id == nodeId)
                    return i == 0 ? null : chain[i - 1].Id;
            }
            return null; // defensive: a known trunk/global node is always in the chain
        }

        // Seed node: gated by the tower's last trunk.
        return chain[^1].Id;
    }

    /// <summary>
    /// The tower's trunk nodes in catalog order (the tower's sequential chain).
    /// </summary>
    private static List<SkillNodeDefinition> TrunksFor(TowerType towerType)
    {
        var trunks = new List<SkillNodeDefinition>();
        foreach (var node in ForTower(towerType))
        {
            if (node.Kind == SkillNodeKind.Trunk)
                trunks.Add(node);
        }
        return trunks;
    }
}
