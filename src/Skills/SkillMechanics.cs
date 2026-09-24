namespace GeometryTowerDefense;

/// <summary>
/// Pure mechanic math: final behavior values layered from skill-node ranks. Every
/// function takes a rank (0..SkillMaxRanks) and returns the final value, mirroring
/// <see cref="SkillStats"/> so the whole mechanic curve is unit-testable without
/// Godot runtime. No allocation — arithmetic only.
/// </summary>
public static class SkillMechanics
{
    // Arrow
    public static int ArrowPierceCount(int rank) =>
        rank * GameConstants.SkillArrowPiercePerRank;

    public static float ArrowCritChance(int rank) =>
        rank * GameConstants.SkillArrowCritChancePerRank;

    // Cannon
    public static int CannonClusterCount(int rank) =>
        1 + rank * GameConstants.SkillCannonClusterPerRank;

    public static float CannonStunChance(int rank) =>
        rank * GameConstants.SkillCannonStunChancePerRank;

    // Laser
    public static float LaserIgniteChancePerSecond(int rank) =>
        rank * GameConstants.SkillLaserIgniteChancePerRank;

    public static int LaserChainJumps(int rank) =>
        rank * GameConstants.SkillLaserChainPerRank;

    public static float LaserRampMaxMultiplier(int rank) =>
        1f + rank * GameConstants.SkillLaserRampPerRank;
}
