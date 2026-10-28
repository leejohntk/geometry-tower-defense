using System;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Per-tower specialization chosen at rank 3. Every archetype maps 1:1 to one of the
/// tower's skill-tree seed nodes; the tower records the choice permanently (no respec)
/// and only the chosen seed's mechanic applies afterwards.
/// </summary>
public enum TowerArchetype
{
    None = 0,
    Pierce,
    Crit,
    Cluster,
    Stun,
    Chain,
    RampUp
}

/// <summary>
/// Pure catalog mapping tower types to their two archetype branches, the matching
/// skill-tree seed node ids, and the display strings the upgrade panel renders.
/// </summary>
public static class TowerArchetypes
{
    private static readonly TowerArchetype[] ArrowOptionsValue = { TowerArchetype.Pierce, TowerArchetype.Crit };
    private static readonly TowerArchetype[] CannonOptionsValue = { TowerArchetype.Cluster, TowerArchetype.Stun };
    private static readonly TowerArchetype[] LaserOptionsValue = { TowerArchetype.Chain, TowerArchetype.RampUp };

    /// <summary>
    /// The two archetype branches offered by the given tower type, in display order.
    /// </summary>
    public static IReadOnlyList<TowerArchetype> OptionsFor(TowerType type) => type switch
    {
        TowerType.Cannon => CannonOptionsValue,
        TowerType.Laser => LaserOptionsValue,
        _ => ArrowOptionsValue
    };

    /// <summary>
    /// True when the archetype belongs to the given tower type. Used by the purchase
    /// transaction so a stale/hostile card can never install the wrong specialization.
    /// </summary>
    public static bool IsValidFor(TowerType type, TowerArchetype archetype)
    {
        foreach (var option in OptionsFor(type))
        {
            if (option == archetype)
                return true;
        }
        return false;
    }

    /// <summary>
    /// The skill-tree seed node that this archetype specializes.
    /// </summary>
    public static string SeedNodeId(TowerArchetype archetype) => archetype switch
    {
        TowerArchetype.Pierce => SkillTreeCatalog.ArrowPierce,
        TowerArchetype.Crit => SkillTreeCatalog.ArrowCritChance,
        TowerArchetype.Cluster => SkillTreeCatalog.CannonCluster,
        TowerArchetype.Stun => SkillTreeCatalog.CannonStunChance,
        TowerArchetype.Chain => SkillTreeCatalog.LaserChain,
        TowerArchetype.RampUp => SkillTreeCatalog.LaserRampUp,
        _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, "No seed node for this archetype.")
    };

    /// <summary>
    /// Player-facing archetype name ("Pierce", "Ramp-Up", ...). None returns an empty
    /// string so a panel rendered before specialization shows no archetype label.
    /// </summary>
    public static string DisplayName(TowerArchetype archetype) => archetype switch
    {
        TowerArchetype.Pierce => "Pierce",
        TowerArchetype.Crit => "Crit",
        TowerArchetype.Cluster => "Cluster",
        TowerArchetype.Stun => "Stun",
        TowerArchetype.Chain => "Chain",
        TowerArchetype.RampUp => "Ramp-Up",
        _ => string.Empty
    };

    /// <summary>
    /// Stable node name for an archetype's card/emblem: the display name with its
    /// punctuation removed ("Ramp-Up" becomes "RampUp"), or "None" before any choice.
    /// </summary>
    public static string NodeKey(TowerArchetype archetype)
    {
        string name = DisplayName(archetype).Replace("-", "");
        return name.Length == 0 ? "None" : name;
    }

    /// <summary>
    /// Effective skill-tree rank of the chosen seed after specialization: at least
    /// <see cref="GameConstants.ArchetypeMinSeedRank"/>, so the mechanic is always
    /// active even on an untouched skill tree.
    /// </summary>
    public static int EffectiveSeedRank(int skillRank) =>
        Math.Max(GameConstants.ArchetypeMinSeedRank, skillRank);
}
