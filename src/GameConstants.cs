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

    // Progenitor (boss). The toughest, slowest, largest enemy in the game. Every
    // interval it enters a brief windup and summons a mixed squad directly ahead of
    // itself on its route; the summon cadence, squad size, and mix escalate in three
    // HP phases. Phase thresholds are percentages of max HP; HP exactly on a
    // threshold belongs to the lower (harder) phase.
    public const int ProgenitorHP = 250;
    public const int ProgenitorArmor = 3;
    public const float ProgenitorSpeed = 1.4f;
    public const int ProgenitorDiameter = 64;
    public const int ProgenitorCoinDrop = 12;
    public const int ProgenitorBaseDamage = 1;
    public const float ProgenitorSummonWindup = 0.5f;
    public const float ProgenitorSummonSlowFactor = 0.35f;
    public const float ProgenitorSummonLeadDistance = 100f;
    public const float ProgenitorSummonOffsetRadius = 20f;

    // Progenitor phases: cadence, squad size, and mix weights (Basic / Swarm / Armored).
    public const float ProgenitorPhase1SummonInterval = 3.0f;
    public const float ProgenitorPhase2SummonInterval = 2.25f;
    public const float ProgenitorPhase3SummonInterval = 1.5f;
    public const int ProgenitorPhase1SquadSize = 3;
    public const int ProgenitorPhase2SquadSize = 4;
    public const int ProgenitorPhase3SquadSize = 5;
    public const float ProgenitorPhase2HPFraction = 2f / 3f;
    public const float ProgenitorPhase3HPFraction = 1f / 3f;

    // Phase 1: 70% Basic / 30% Swarm / 0% Armored.
    public const double ProgenitorPhase1BasicWeight = 0.70;
    public const double ProgenitorPhase1SwarmWeight = 0.30;
    // Phase 2: 40% Basic / 35% Swarm / 25% Armored.
    public const double ProgenitorPhase2BasicWeight = 0.40;
    public const double ProgenitorPhase2SwarmWeight = 0.35;
    public const double ProgenitorPhase2ArmoredWeight = 0.25;
    // Phase 3: 15% Basic / 40% Swarm / 45% Armored.
    public const double ProgenitorPhase3BasicWeight = 0.15;
    public const double ProgenitorPhase3SwarmWeight = 0.40;
    public const double ProgenitorPhase3ArmoredWeight = 0.45;

    // Progenitor presentation — procedural geometry only (octagon, border, HP bar,
    // telegraph pulse ring). The border shifts white -> amber -> red as phases fall.
    public static readonly Color ProgenitorFillColor = new Color(0.55f, 0.06f, 0.4f);       // deep magenta
    public static readonly Color ProgenitorPhase1BorderColor = new Color(1f, 1f, 1f);        // white
    public static readonly Color ProgenitorPhase2BorderColor = new Color(1f, 0.72f, 0.15f); // amber
    public static readonly Color ProgenitorPhase3BorderColor = new Color(1f, 0.15f, 0.15f); // red
    public static readonly Color ProgenitorPulseColor = new Color(1f, 0.9f, 0.5f);           // pale gold telegraph
    public const float ProgenitorBorderWidth = 4f;
    public const float ProgenitorHealthBarWidthRatio = 0.8f;
    public const float ProgenitorHealthBarHeight = 6f;
    public const float ProgenitorHealthBarGap = 10f;      // px between octagon top and bar bottom
    public const float ProgenitorPulseStartScale = 0.75f; // ring radius at windup start, x octagon radius
    public const float ProgenitorPulseEndScale = 1.35f;   // ring radius at windup end
    public const int ProgenitorPulseSegments = 32;

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

    // Tower Upgrades — in-level, per-tower ranks. Every placed tower starts at rank 0
    // and can be raised by spending coins, gated by a hidden kill-XP accumulator.
    // Ranks, XP, and archetype are per tower instance and are discarded with the level.
    public const int TowerMaxRank = 3;
    public const int TowerUpgradeCostRank1 = 5;
    public const int TowerUpgradeCostRank2 = 10;
    public const int TowerUpgradeCostRank3 = 15;
    public const float TowerRankDamageBonusPerRank = 0.03f;   // multiplier = 1 + 0.03 x rank
    public const float TowerRankRangeBonusPerRank = 0.5f;     // cells, additive
    public const float TowerRankFireRateBonusPerRank = 0.03f; // interval / (1 + 0.03 x rank)
    public const float TowerXpThresholdRank1 = 5f;            // cumulative kill XP
    public const float TowerXpThresholdRank2 = 15f;
    public const float TowerXpThresholdRank3 = 30f;

    // Kill XP awarded per enemy kind. Split across every tower that damaged the
    // enemy, in proportion to credited (effective, non-overkill) damage.
    public const int EnemyXpBasic = 1;
    public const int EnemyXpSwarm = 1;
    public const int EnemyXpArmored = 3;
    public const int EnemyXpProgenitor = 10;

    // Archetype specialization: the chosen seed mechanic applies at least at this
    // rank, even when the player's skill tree has no rank in that seed.
    public const int ArchetypeMinSeedRank = 1;

    // Tower Upgrade panel (UI overlay). One panel per selection, docked to the
    // selected tower's cell. Dark translucent fill matching the HUD sidebar.
    public const float TowerUpgradePanelWidth = 180f;
    public const float TowerUpgradePanelPadding = 8f;
    public const float TowerUpgradePanelRankHeight = 24f;     // name-only row; the badge shows the rank
    public const float TowerUpgradePanelUpgradeHeight = 40f;
    public const float TowerUpgradePanelArchetypeHeight = 84f;
    public const float TowerUpgradePanelBlockGap = 6f;
    public const float TowerUpgradePanelCellGap = 8f;         // px between cell edge and panel
    public const float TowerUpgradePanelCardIconSize = 26f;

    // Rank icon: one fixed-size box for 1, 2, or 3 chevrons, with the stack centred
    // vertically inside it so the icon never jumps as ranks are gained and the icon
    // box never changes size. Chevrons are thick V polylines, never text glyphs.
    public const float RankIconWidth = 16f;
    public const float RankIconHeight = 30f;
    public const float RankChevronHeight = 8f;                // px, one V stroke slot
    public const float RankChevronVerticalGap = 1f;
    public const float RankChevronStroke = 2.5f;

    // Tiny current-rank badge at the top-right of a placed tower's cell, created by
    // the rank purchase itself. Same fixed size at every rank (rank 1/2 chevrons,
    // rank 3 archetype emblem).
    public const float TowerRankBadgeWidth = 10f;
    public const float TowerRankBadgeHeight = 18f;
    public const float TowerRankBadgeMargin = 2f;             // px inset from the cell edge
    public const float TowerRankBadgeChevronHeight = 5f;
    public const float TowerRankBadgeChevronGap = 1f;
    public const float TowerRankBadgeStroke = 1.5f;
    public static readonly Color TowerUpgradePanelBackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.92f);
    public static readonly Color TowerUpgradePanelBorderColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);
    public static readonly Color TowerUpgradePanelDimColor = new Color(0.35f, 0.35f, 0.42f, 0.9f);

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

    /// <summary>
    /// True when a screen-space point is over the visible battlefield: inside the grid
    /// bounds and not under the HUD top bar. The sidebar, top bar, and the margin below
    /// the grid are UI/outside, so clicks there must not act on the battlefield.
    /// </summary>
    public static bool IsBattlefieldPoint(Vector2 point) =>
        point.X >= 0f && point.X < PlayAreaWidth &&
        point.Y >= TopBarHeight && point.Y < PlayAreaHeight;

    // Cell center helpers
    public static float CellCenterX(int col) => col * CellSize + CellSize / 2f;
    public static float CellCenterY(int row) => row * CellSize + CellSize / 2f;
    public static float CellDistanceInPixels(float cells) => cells * CellSize;

    /// <summary>
    /// Largest Progenitor summon squad any phase can produce (phase 3). Reused as the
    /// bound for the allocation-free pending-composition buffer.
    /// </summary>
    public static int ProgenitorMaxSquadSize => ProgenitorPhase3SquadSize;

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
