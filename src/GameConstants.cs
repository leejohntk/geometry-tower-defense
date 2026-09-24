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

    // Burn (ignite) visuals — a hot flash the instant the DoT procs (visually distinct
    // from the white crit flash) plus a pulsing tint while it drains, so the drain reads
    // as active instead of a static orange. The pulse is quantized into BurnPulseSteps
    // intensity levels per BurnPulsePeriod, so a burning enemy redraws only
    // BurnPulseSteps / BurnPulsePeriod times per second (~17) instead of every frame.
    // Step 0 is the trough, whose tint is exactly the static burn tint used before the
    // pulse existed, so the un-pulsed look is the pulse's resting state.
    public const float BurnProcFlashDuration = 0.18f;         // seconds
    public const float BurnProcFlashStrength = 0.8f;          // how far the proc flash pushes the fill
    public const float BurnPulsePeriod = 0.3f;                // seconds per pulse cycle
    public const int BurnPulseSteps = 5;                      // discrete levels per cycle
    // The tint strength throbs with the pulse too: the trough matches the pre-pulse
    // static tint exactly, the peak pushes further toward the hot color.
    public const float BurnPulseTintStrengthMin = 0.5f;       // matches the static burn tint
    public const float BurnPulseTintStrengthMax = 0.8f;       // peak of the throb
    public static readonly Color BurnProcFlashColor = new Color(1f, 0.95f, 0.5f);   // hot yellow, distinct from the white crit flash
    public static readonly Color BurnPulseCoolColor = new Color(1f, 0.5f, 0f);      // pulse trough
    public static readonly Color BurnPulseHotColor = new Color(1f, 0.85f, 0.3f);    // pulse peak

    // Pierce spark — brief radiating burst at each enemy an arrow passes through, so
    // every pierced hit reads instead of the arrow silently continuing. Deliberately
    // much smaller and shorter than the cannon explosion: a pierce must never look
    // like a splash.
    public const float PierceSparkDuration = 0.2f;            // seconds
    public const float PierceSparkRadius = 14f;               // spokes' outer radius (px) at full expansion
    public const float PierceSparkInnerRadiusRatio = 0.35f;   // spokes' inner end, fraction of outer radius
    public const float PierceSparkStrokeWidth = 2.0f;         // spoke width, pixels
    public const int PierceSparkSpokes = 4;                   // radial lines (X-shaped burst)
    public static readonly Color PierceSparkColor = new Color(1f, 0.85f, 0.3f);     // arrow yellow

    // Laser ramp-up feedback — the beam widens and brightens as its dps multiplier
    // climbs from 1.0x to the tower's max, making the ramp visible. The "Min" values
    // are exactly the un-ramped beam look, so a non-ramping laser is unchanged.
    public const float LaserBeamWidthMin = 3f;                // pixels at 1.0x
    public const float LaserBeamWidthMax = 6.5f;              // pixels at max ramp
    public const float LaserChainBeamWidthMin = 2f;           // pixels at 1.0x
    public const float LaserChainBeamWidthMax = 4f;           // pixels at max ramp
    public static readonly Color LaserBeamColorMin = new Color(0.9f, 0.4f, 1.0f, 0.9f);
    public static readonly Color LaserBeamColorMax = new Color(1f, 0.85f, 1f, 1f);
    public static readonly Color LaserChainBeamColorMin = new Color(0.95f, 0.5f, 1.0f, 0.8f);
    public static readonly Color LaserChainBeamColorMax = new Color(1f, 0.9f, 1f, 1f);

    // Cannon cluster muzzle flash — a short ring plus radial spikes at the barrel,
    // shown only when a volley fires more than one shell, so a cluster shot is
    // distinguishable from a single one. Purely visual state on the tower node: no
    // pool, no per-shot allocation, and nothing draws while idle.
    public const float CannonMuzzleFlashDuration = 0.22f;     // seconds
    public const float CannonMuzzleFlashRingScale = 1.6f;     // x barrel radius at full expansion
    public const float CannonMuzzleFlashStrokeWidth = 3.0f;   // ring/spoke width, pixels
    public const float CannonMuzzleFlashSpikeLength = 8f;     // px the spikes reach past the ring
    public const int CannonMuzzleFlashSpikes = 6;
    public static readonly Color CannonMuzzleFlashColor = new Color(1f, 0.93f, 0.45f); // hot yellow

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
