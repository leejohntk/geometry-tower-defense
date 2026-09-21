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
}
