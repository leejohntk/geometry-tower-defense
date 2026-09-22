using System;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Mutable holder for the player's persistent skill-tree progression: the Skill Point
/// balance plus the current rank of every node. Pure C# state with no Godot or I/O
/// dependencies, so the buy/rank rules can be unit-tested in isolation.
/// </summary>
public sealed class SkillTreeState
{
    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);

    /// <summary>
    /// Persistent Skill Point balance.
    /// </summary>
    public int SkillPoints { get; private set; }

    /// <summary>
    /// Node id -> current rank map. Missing ids read as rank 0.
    /// </summary>
    public IReadOnlyDictionary<string, int> Ranks => _ranks;

    /// <summary>
    /// Current rank of a node (0..SkillMaxRanks). Unknown ids return 0.
    /// </summary>
    public int GetRank(string nodeId) => _ranks.TryGetValue(nodeId, out var rank) ? rank : 0;

    /// <summary>
    /// Awards Skill Points. Negative amounts are ignored (awards never subtract), and
    /// the balance saturates at <see cref="int.MaxValue"/> rather than wrapping.
    /// </summary>
    public void AddSkillPoints(int amount)
    {
        if (amount <= 0)
            return;
        SkillPoints = (int)Math.Min((long)SkillPoints + amount, int.MaxValue);
    }

    /// <summary>
    /// True if the node has no prerequisite (the first trunk node, or a seed on a
    /// trunk-less tower, or an unknown id) or its prerequisite has reached the unlock
    /// rank. This is a structural buy-time gate; it does not care whether the node
    /// itself is enabled.
    /// </summary>
    public bool IsUnlocked(string nodeId)
    {
        var prerequisite = SkillTreeCatalog.GetPrerequisite(nodeId);
        if (prerequisite == null)
            return true;
        return GetRank(prerequisite) >= GameConstants.SkillUnlockRank;
    }

    /// <summary>
    /// True if the node exists, is enabled, is unlocked, is below max rank, and is
    /// affordable.
    /// </summary>
    public bool CanBuyRank(string nodeId)
    {
        var def = SkillTreeCatalog.Find(nodeId);
        if (def == null || !def.Enabled)
            return false;
        if (!IsUnlocked(nodeId))
            return false;
        if (GetRank(nodeId) >= GameConstants.SkillMaxRanks)
            return false;
        if (SkillPoints < GameConstants.SkillNodeRankCost)
            return false;
        return true;
    }

    /// <summary>
    /// Buys one rank for the node. Applies the cost and increments the rank on success.
    /// Returns false (no state change) when the purchase rules fail.
    /// </summary>
    public bool BuyRank(string nodeId)
    {
        if (!CanBuyRank(nodeId))
            return false;

        SkillPoints -= GameConstants.SkillNodeRankCost;
        _ranks[nodeId] = GetRank(nodeId) + 1;
        return true;
    }

    /// <summary>
    /// Sets a node's rank directly (used by save loading). Unknown node ids and
    /// currently-disabled (mechanic) nodes are rejected and zeroed so the UI can
    /// never render a non-zero rank next to "coming soon". Valid ranks clamp to
    /// 0..SkillMaxRanks.
    /// </summary>
    public void SetRank(string nodeId, int rank)
    {
        var def = SkillTreeCatalog.Find(nodeId);
        // Deliberately mirrors CanBuyRank's enabled gate: a hand-edited save must
        // never render a rank beside "coming soon", so do not "simplify" one away.
        if (def == null || !def.Enabled)
        {
            _ranks.Remove(nodeId);
            return;
        }

        _ranks[nodeId] = Math.Clamp(rank, 0, GameConstants.SkillMaxRanks);
    }

    /// <summary>
    /// Sets the Skill Point balance directly (used by save loading). Clamped to
    /// 0..int.MaxValue so a hand-edited save can never seed a negative balance.
    /// </summary>
    public void SetSkillPoints(int skillPoints)
    {
        SkillPoints = Math.Clamp(skillPoints, 0, int.MaxValue);
    }
}
