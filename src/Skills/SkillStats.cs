namespace GeometryTowerDefense;

using System;

/// <summary>
/// Pure stat-modifier math: final tower stats are the base constants layered with
/// skill-node ranks. All functions take a rank (0..SkillMaxRanks) and return the
/// final value, so the full upgrade curve is testable without any Godot runtime.
///
/// Cheap reads only — no allocation, no per-frame cost beyond arithmetic.
/// </summary>
public static class SkillStats
{
    // Arrow
    public static int ArrowDamage(int rank) =>
        GameConstants.ArrowTowerDamage + rank * GameConstants.SkillArrowDamagePerRank;

    public static float ArrowFireRate(int rank) =>
        GameConstants.ArrowTowerFireRate / (1f + rank * GameConstants.SkillArrowAttackSpeedPerRank);

    public static float ArrowRangeCells(int rank) =>
        GameConstants.TowerRange(TowerType.Arrow) + rank * GameConstants.SkillArrowRangePerRank;

    // Cannon
    public static int CannonDamage(int rank) =>
        GameConstants.CannonTowerDamage + rank * GameConstants.SkillCannonPowderDamagePerRank;

    public static float CannonProjectileSpeedMultiplier(int rank) =>
        1f + rank * GameConstants.SkillCannonPowderSpeedPerRank;

    public static float CannonFireRate(int rank) =>
        GameConstants.CannonTowerFireRate / (1f + rank * GameConstants.SkillCannonAttackSpeedPerRank);

    public static int CannonSplashRadius(int rank) =>
        GameConstants.CannonTowerAoeRadius + rank * GameConstants.SkillCannonSplashRadiusPerRank;

    // Laser
    public static float LaserDps(int rank) =>
        GameConstants.LaserTowerDps + rank * GameConstants.SkillLaserDpsPerRank;

    public static float LaserRangeCells(int rank) =>
        GameConstants.TowerRange(TowerType.Laser) + rank * GameConstants.SkillLaserRangePerRank;

    /// <summary>
    /// Final range (cells) of the given tower type against a live skill-tree state.
    /// Cannon has no range node, so its range is always the base constant. A null
    /// state (or a state with rank 0) yields the base range. This is the single
    /// source of truth shared by the placement preview and the tower variants.
    /// </summary>
    public static float RangeCells(TowerType type, SkillTreeState? state)
    {
        return type switch
        {
            TowerType.Arrow => ArrowRangeCells(state?.GetRank(SkillTreeCatalog.ArrowRange) ?? 0),
            TowerType.Cannon => GameConstants.TowerRange(TowerType.Cannon),
            TowerType.Laser => LaserRangeCells(state?.GetRank(SkillTreeCatalog.LaserRange) ?? 0),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown tower type.")
        };
    }

    // === Global (tower-independent) nodes ===

    /// <summary>
    /// Starting HP for a Fortified Core rank: 3 + rank x 1.
    /// </summary>
    public static int StartingHp(int rank) =>
        GameConstants.StartingHP + rank * GameConstants.GlobalStartingHpPerRank;

    /// <summary>
    /// Starting coins for a War Chest rank: 10 + rank x 2.
    /// </summary>
    public static int StartingCoins(int rank) =>
        GameConstants.StartingCoins + rank * GameConstants.GlobalStartingGoldPerRank;

    /// <summary>
    /// Starting HP against a live skill-tree state (rank 0 or a null state = base 3).
    /// </summary>
    public static int StartingHp(SkillTreeState? state) =>
        StartingHp(state?.GetRank(SkillTreeCatalog.GlobalStartingHp) ?? 0);

    /// <summary>
    /// Starting coins against a live skill-tree state (rank 0 or null = base 10).
    /// </summary>
    public static int StartingCoins(SkillTreeState? state) =>
        StartingCoins(state?.GetRank(SkillTreeCatalog.GlobalStartingGold) ?? 0);

    /// <summary>
    /// Master Blueprints discount rank from a live state (0 when null).
    /// </summary>
    public static int TowerDiscountRank(SkillTreeState? state) =>
        state?.GetRank(SkillTreeCatalog.GlobalTowerDiscount) ?? 0;

    /// <summary>
    /// Advanced Training discount rank from a live state (0 when null).
    /// </summary>
    public static int UpgradeDiscountRank(SkillTreeState? state) =>
        state?.GetRank(SkillTreeCatalog.GlobalUpgradeDiscount) ?? 0;

    /// <summary>
    /// Applies a coin discount with the defensive minimum-cost floor. The caller
    /// converts the node rank through the relevant per-rank tuning constant, so those
    /// constants stay the single source of truth. Base costs outside the real upgrade
    /// table (0) stay 0 so a stale caller is never charged.
    /// </summary>
    public static int ApplyDiscount(int baseCost, int discountAmount)
    {
        if (baseCost <= 0)
            return 0;

        return Math.Max(GameConstants.MinimumEffectiveCost, baseCost - discountAmount);
    }

    /// <summary>
    /// Effective coin cost to place a tower: max(1, base cost - Master Blueprints
    /// rank x <see cref="GameConstants.GlobalTowerDiscountPerRank"/>). All three bases
    /// are 10, so ranks 0-3 yield 10/9/8/7. This is the single source of truth for
    /// placement affordability, deduction, labels, and hotkeys.
    /// </summary>
    public static int EffectiveTowerCost(TowerType type, SkillTreeState? state) =>
        ApplyDiscount(
            GameConstants.TowerCost(type),
            TowerDiscountRank(state) * GameConstants.GlobalTowerDiscountPerRank);

    /// <summary>
    /// Effective coin cost to reach the given tower rank: max(1, base cost - Advanced
    /// Training rank x <see cref="GameConstants.GlobalUpgradeDiscountPerRank"/>). The
    /// three bases are 5/10/15, so at rank 3 they become 2/7/12. Ranks outside 1..3
    /// have no base cost and stay 0 (never clamped up to 1).
    /// </summary>
    public static int EffectiveUpgradeCost(int targetRank, SkillTreeState? state) =>
        ApplyDiscount(
            TowerUpgrades.CostToReachRank(targetRank),
            UpgradeDiscountRank(state) * GameConstants.GlobalUpgradeDiscountPerRank);
}
