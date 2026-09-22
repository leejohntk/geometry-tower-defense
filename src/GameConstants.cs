using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// All game tuning parameters in one place. No magic numbers in game code.
/// </summary>
public static class GameConstants
{
    // Grid
    public const int GridCols = 20;
    public const int GridRows = 14;
    public const int CellSize = 64;
    public const int PathRow = 10;

    // Player
    public const int StartingHP = 3;
    public const int StartingCoins = 10;

    // Enemy
    public const int EnemyHP = 10;
    public const float EnemySpeed = 2f;
    public const float SpawnInterval = 0.8f;
    public const int EnemyDiameter = 48;
    public const int CoinDropPerKill = 1;

    // Arrow Tower
    public const int ArrowTowerRange = 4;
    public const float ArrowTowerFireRate = 1.5f;
    public const int ArrowTowerDamage = 10;
    public const int ArrowTowerCost = 10;

    // Cannon Tower
    public const int CannonTowerRange = 4;
    public const float CannonTowerFireRate = 2.5f;
    public const int CannonTowerDamage = 15;   // AoE damage
    public const int CannonTowerAoeRadius = 64; // 1 cell, in pixels
    public const int CannonTowerCost = 10;

    // Swarm Enemy
    public const int SwarmEnemyHP = 5;
    public const int SwarmEnemyDiameter = 24;
    public const float SwarmEnemySpeed = 2f;
    public const int SwarmClusterSize = 3;
    // Radius of the invisible circle the swarm members orbit their path anchor.
    // At 20px, adjacent members sit ~34.6px apart (clearing the 24px diameter with
    // ~10px of gap) and the central hole has ~8px of clear radius.
    public const float SwarmClusterRadius = 20f;
    // Orbit speed of swarm members around their path anchor (radians/sec).
    // At 2.5 rad/s each member completes a full revolution in ~2.5 seconds.
    public const float SwarmClusterRotationSpeed = 2.5f;
    public const int SwarmCoinDropPerKill = 1;

    // Armored Enemy
    public const int ArmoredEnemyHP = 14;
    public const int ArmoredEnemyArmor = 5;
    public const float ArmoredEnemySpeed = 2f;
    public const int ArmoredEnemyDiameter = 48;
    public const int ArmoredCoinDropPerKill = 2;

    // Laser Tower
    public const int LaserTowerRange = 3;
    public const float LaserTowerDps = 4f;
    public const int LaserTowerCost = 10;

    // Projectile
    public const float ProjectileSpeed = 8f;
    public const int ProjectileSize = 12;

    // Skill Tree — persistent meta-progression currency (Skill Points) and node tuning.
    // SP is awarded per level outcome: victory = 2 x level, defeat = level (half a win).
    public const int SkillPointVictoryMultiplier = 2;
    public const int SkillPointLossDivisor = 2;
    public const int SkillNodeRankCost = 10;        // flat SP cost per rank
    public const int SkillMaxRanks = 5;
    public const int SkillUnlockRank = 1;           // rank a prerequisite must reach to unlock its children

    // Arrow stat nodes
    public const int SkillArrowDamagePerRank = 2;
    public const float SkillArrowAttackSpeedPerRank = 0.10f; // fire interval / (1 + 0.10*r)
    public const float SkillArrowRangePerRank = 0.5f;        // cells

    // Cannon stat nodes
    public const int SkillCannonPowderDamagePerRank = 1;
    public const float SkillCannonPowderSpeedPerRank = 0.10f; // projectile speed
    public const float SkillCannonAttackSpeedPerRank = 0.10f; // fire interval / (1 + 0.10*r)
    public const int SkillCannonSplashRadiusPerRank = 8;      // px

    // Laser stat nodes
    public const float SkillLaserDpsPerRank = 0.8f;
    public const float SkillLaserRangePerRank = 0.5f;         // cells

    // Arrow mechanic nodes (Part 2) — Pierce / Crit Chance.
    public const int SkillArrowPiercePerRank = 1;             // enemies passed through per rank
    public const float SkillArrowCritChancePerRank = 0.08f;   // crit chance per rank

    // Cannon mechanic nodes (Part 2) — Cluster / Stun Chance.
    public const int SkillCannonClusterPerRank = 1;           // extra shells per rank
    public const float SkillCannonStunChancePerRank = 0.06f;  // stun chance per rank
    public const float SkillCannonStunDuration = 1.0f;        // seconds
    // Angular offset between adjacent cluster shells (radians). The volley fans out
    // symmetrically around the direct line to the target; rank 0 still fires a single
    // straight shell so the mechanic is invisible until purchased.
    public const float SkillCannonClusterSpreadPerShellRadians = 0.08f;

    // Laser mechanic nodes (Part 2) — Ignite (burn) / Chain / Ramp-Up.
    public const float SkillLaserIgniteChancePerRank = 0.05f; // ignite roll chance per second, per rank
    public const float SkillLaserBurnDps = 2.0f;              // burn damage per second (ignores armor)
    public const float SkillLaserBurnDuration = 2.0f;         // seconds per proc (2 dps x 2s = 4 total)
    public const int SkillLaserChainPerRank = 1;              // extra beam jumps per rank
    public const float SkillLaserChainRange = 1.5f;           // cells between consecutive jump targets
    public const float SkillLaserChainFalloff = 0.60f;        // damage multiplier per jump
    public const float SkillLaserRampPerRank = 0.20f;         // max dps multiplier = 1 + 0.20 * rank
    public const float SkillLaserRampTime = 2.0f;             // seconds of continuous contact to full ramp

    // Crit hit flash: brief brightening of an enemy after a crit lands (presentation).
    public const float HitFlashDuration = 0.1f;               // seconds

    // Cannon Explosion Effect — brief expanding circle at the projectile impact point.
    // The visual expands to exactly CannonTowerAoeRadius so the player sees the true
    // damage extent; GameManager drives both the damage and the visual from the same constant.
    public const float CannonExplosionDuration = 0.4f;      // seconds
    public const float CannonExplosionStrokeWidth = 2.0f;   // outline ring width, pixels
    public static readonly Color CannonExplosionFillColor = new Color(0.3f, 0.6f, 0.4f);    // matches cannon range color
    public static readonly Color CannonExplosionOutlineColor = new Color(0.1f, 0.25f, 0.15f); // matches cannon tower outline

    // World pixel dimensions
    public static int PlayAreaWidth => GridCols * CellSize;
    public static int PlayAreaHeight => GridRows * CellSize;

    // UI Layout — sidebars and panels that overlay the game world
    public const float SidebarWidth = 180f;
    public const float TopBarHeight = 40f;

    /// <summary>
    /// Total viewport width needed to fit both the play area and the sidebar side-by-side.
    /// </summary>
    public static int TotalViewportWidth => PlayAreaWidth + (int)SidebarWidth;

    // Cell center helpers
    public static float CellCenterX(int col) => col * CellSize + CellSize / 2f;
    public static float CellCenterY(int row) => row * CellSize + CellSize / 2f;
    public static float CellDistanceInPixels(float cells) => cells * CellSize;

    // Tower type lookups (used by placement preview and UI before a tower instance exists)
    public static int TowerCost(TowerType type) => type switch
    {
        TowerType.Cannon => CannonTowerCost,
        TowerType.Laser => LaserTowerCost,
        _ => ArrowTowerCost
    };

    public static float TowerRange(TowerType type) => type switch
    {
        TowerType.Cannon => CannonTowerRange,
        TowerType.Laser => LaserTowerRange,
        _ => ArrowTowerRange
    };

    // Skill Point rewards for a level outcome. Pure helpers so the reward math is unit-testable without Godot.
    public static int SkillPointVictoryReward(int levelId) => levelId * SkillPointVictoryMultiplier;
    public static int SkillPointDefeatReward(int levelId) => levelId * SkillPointVictoryMultiplier / SkillPointLossDivisor;
}
