namespace GeometryTowerDefense;

/// <summary>
/// Pure per-tower upgrade math: coin costs, hidden XP thresholds, rank stat
/// multipliers, and per-kind kill XP. Every function is arithmetic only (no Godot
/// types), so the whole rank curve — fresh and maxed skill trees — is unit-testable.
/// </summary>
public static class TowerUpgrades
{
    /// <summary>
    /// True when the tower has reached the final rank and accepts no more upgrades.
    /// </summary>
    public static bool IsMaxRank(int rank) => rank >= GameConstants.TowerMaxRank;

    /// <summary>
    /// Coin cost to reach <paramref name="targetRank"/> (1, 2, or 3). Ranks outside
    /// that range cost nothing so a stale caller can never be charged.
    /// </summary>
    public static int CostToReachRank(int targetRank) => targetRank switch
    {
        1 => GameConstants.TowerUpgradeCostRank1,
        2 => GameConstants.TowerUpgradeCostRank2,
        3 => GameConstants.TowerUpgradeCostRank3,
        _ => 0
    };

    /// <summary>
    /// Cumulative kill XP required before the upgrade to <paramref name="targetRank"/>
    /// is offered. Ranks outside 1..3 require an unreachable amount.
    /// </summary>
    public static float XpThresholdToReachRank(int targetRank) => targetRank switch
    {
        1 => GameConstants.TowerXpThresholdRank1,
        2 => GameConstants.TowerXpThresholdRank2,
        3 => GameConstants.TowerXpThresholdRank3,
        _ => float.PositiveInfinity
    };

    /// <summary>
    /// True when the next upgrade option (a rank at 0-1, or the archetype choice at
    /// rank 2) is revealed. This is the hidden XP gate; below it the UI shows nothing.
    /// </summary>
    public static bool IsUpgradeVisible(int rank, float xp) =>
        !IsMaxRank(rank) && xp >= XpThresholdToReachRank(rank + 1);

    /// <summary>
    /// True when the generic chevron upgrade may be bought right now. Rank 2 has no
    /// generic third rank — that step is the archetype choice instead.
    /// </summary>
    public static bool CanBuyNextRank(int rank, float xp, int coins) =>
        rank < GameConstants.TowerMaxRank - 1 &&
        xp >= XpThresholdToReachRank(rank + 1) &&
        coins >= CostToReachRank(rank + 1);

    /// <summary>
    /// True when the rank 2 -> 3 archetype choice may be bought right now.
    /// </summary>
    public static bool CanBuyArchetype(int rank, float xp, int coins) =>
        rank == GameConstants.TowerMaxRank - 1 &&
        xp >= XpThresholdToReachRank(GameConstants.TowerMaxRank) &&
        coins >= CostToReachRank(GameConstants.TowerMaxRank);

    // === Rank stat layering ===

    /// <summary>Damage multiplier applied to the final skill-tree-modified base damage.</summary>
    public static float DamageMultiplier(int rank) =>
        1f + GameConstants.TowerRankDamageBonusPerRank * rank;

    /// <summary>
    /// Fire-rate multiplier: 1 + 0.03 x rank. Arrow/cannon intervals divide by it;
    /// the continuous laser has no interval, so its DPS multiplies by it instead.
    /// </summary>
    public static float FireRateMultiplier(int rank) =>
        1f + GameConstants.TowerRankFireRateBonusPerRank * rank;

    /// <summary>
    /// Fire interval for arrow/cannon: the skill-tree-modified interval divided by
    /// the rank fire-rate bonus (higher rank = shorter interval).
    /// </summary>
    public static float FireInterval(float skillInterval, int rank) =>
        skillInterval / FireRateMultiplier(rank);

    /// <summary>
    /// Final range in cells: skill-tree range plus the additive rank bonus. Applies to
    /// every tower type, including the cannon (which has no range trunk node).
    /// </summary>
    public static float RangeCells(float skillRangeCells, int rank) =>
        skillRangeCells + GameConstants.TowerRankRangeBonusPerRank * rank;

    /// <summary>
    /// Kill XP value for an enemy kind. Summons use their rolled kind's value even
    /// though they drop zero coins.
    /// </summary>
    public static int EnemyXpValue(EnemyKind kind) => kind switch
    {
        EnemyKind.Swarm => GameConstants.EnemyXpSwarm,
        EnemyKind.Armored => GameConstants.EnemyXpArmored,
        EnemyKind.Progenitor => GameConstants.EnemyXpProgenitor,
        _ => GameConstants.EnemyXpBasic
    };
}
