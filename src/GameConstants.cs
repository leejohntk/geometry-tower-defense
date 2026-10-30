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
    // selected tower's cell. Dark translucent fill matching the HUD sidebar. The
    // panel is a single action (upgrade button or two archetype cards) with no
    // header, and it is hidden entirely when no upgrade is available.
    public const float TowerUpgradePanelWidth = 180f;
    public const float TowerUpgradePanelPadding = 8f;
    public const float TowerUpgradePanelUpgradeHeight = 40f;
    public const float TowerUpgradePanelArchetypeHeight = 84f;
    public const float TowerUpgradePanelCellGap = 8f;         // px between cell edge and panel
    public const float TowerUpgradePanelCardIconSize = 26f;

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

    // === Visual overhaul (presentation only) =====================================

    // Enemy silhouettes. Sides = 0 means a circle; any other value is a regular
    // polygon with that many sides. Spin and pulse are applied as node transforms
    // (Rotation / Scale), never as per-frame redraws.
    public const int EnemySwarmSides = 3;
    public const int EnemyArmoredSides = 4;
    public const int EnemyProgenitorSides = 8;
    public const float EnemySwarmSpinSpeed = 1.4f;          // radians/sec
    public const float EnemyBasicPulseAmplitude = 0.045f;   // scale delta around 1
    public const float EnemyBasicPulseSpeed = 3.2f;         // radians/sec
    public const float EnemyBasicInnerRingRatio = 0.52f;    // x outer radius
    public const float EnemySilhouetteStrokeWidth = 2f;
    public const float EnemyArmoredInsetRatio = 0.62f;      // inner border, x radius
    public const float EnemyArmoredRivetRadius = 2.5f;
    public const float EnemyProgenitorOuterRingScale = 1.16f; // x octagon radius
    public const float EnemyProgenitorOuterRingWidth = 1.5f;
    public const float EnemyProgenitorOuterRingSpinSpeed = -0.8f; // rad/sec, counter-rotating

    // Normal (non-crit) hit feedback. Deliberately weaker and shorter than the white
    // crit flash (HitFlashDuration 0.1s at strength 0.6), and distinct from the hot
    // yellow burn-proc flash: a normal hit is a short, faint brighten plus a scale pop.
    public const float EnemyHitPulseDuration = 0.07f;       // seconds
    public const float EnemyHitPulseStrength = 0.25f;       // lerp toward white
    public const float EnemyHitPulseScale = 0.08f;          // extra scale at the pop peak

    // Death burst: pooled expanding shards at a kill position, colored with the
    // enemy's fill. Shard counts scale with kind; the boss radius is > 2x basic.
    public const float DeathBurstDuration = 0.38f;          // seconds
    public const float DeathBurstBasicRadius = 26f;
    public const float DeathBurstSwarmRadius = 18f;
    public const float DeathBurstArmoredRadius = 30f;
    public const float DeathBurstProgenitorRadius = 60f;
    public const int DeathBurstBasicShards = 6;
    public const int DeathBurstSwarmShards = 4;
    public const int DeathBurstArmoredShards = 8;
    public const int DeathBurstProgenitorShards = 16;
    public const float DeathBurstShardWidth = 3f;
    public const float DeathBurstInnerRadiusRatio = 0.25f;
    public const int DeathBurstPoolSize = 16;

    // Per-type tower palette: the arrow blue, cannon green, and laser magenta used by
    // the world bodies, range rings, and UI tower glyphs. One source of truth so the
    // field and its icons never drift apart.
    public static readonly Color ArrowTowerFillColor = new Color(0.2f, 0.5f, 1f);
    public static readonly Color ArrowTowerOutlineColor = new Color(0.05f, 0.1f, 0.4f);
    public static readonly Color CannonTowerFillColor = new Color(0.3f, 0.6f, 0.4f);
    public static readonly Color CannonTowerOutlineColor = new Color(0.1f, 0.25f, 0.15f);
    public static readonly Color LaserTowerFillColor = new Color(0.8f, 0.3f, 1f);
    public static readonly Color LaserTowerOutlineColor = new Color(0.4f, 0.1f, 0.6f);

    // Tower aim and fire feedback. Aim rotates toward the target at a capped angular
    // speed instead of snapping; every successful shot kicks a short recoil.
    public const float TowerAimAngularSpeed = 8f;           // radians/sec cap
    public const float TowerShotRecoilDuration = 0.12f;     // seconds
    public const float TowerRecoilDistance = 6f;            // px along the aim axis
    public const float TowerBowFlashRadius = 30f;           // arrow bow arc radius (just past the tip)
    public const float TowerBowFlashWidth = 2f;
    public static readonly Color TowerBowFlashColor = new Color(0.88f, 0.94f, 1f);

    // Laser emitter firing pulse while the beam is active (transform only).
    public const float LaserEmitterPulseAmplitude = 0.05f;
    public const float LaserEmitterPulseSpeed = 7f;

    // Laser impact glow: a small ring at the beam endpoint (and smaller rings at chain
    // endpoints) that follows the target and fades with the ramp. Never drawn idle.
    public const float LaserImpactGlowRadius = 9f;
    public const float LaserImpactGlowStroke = 2f;
    public const float LaserChainImpactGlowScale = 0.6f;
    public const float LaserImpactGlowAlphaMin = 0.45f;
    public const float LaserImpactGlowAlphaMax = 0.95f;
    public static readonly Color LaserImpactGlowColor = new Color(1f, 0.65f, 1f);

    // Projectile trails. Visual only: they never collide or feed back into movement.
    public const int ArrowTrailSegments = 3;                // 3 segments = 4 samples
    public const float ArrowTrailLifetime = 0.12f;          // seconds
    public const float ArrowTrailWidth = 3f;
    public static readonly Color ArrowTrailColor = new Color(1f, 0.9f, 0.2f);
    public const int CannonTrailPuffs = 4;
    public const float CannonTrailLifetime = 0.2f;          // seconds
    public const float CannonTrailPuffRadius = 4.5f;
    public static readonly Color CannonTrailColor = new Color(0.24f, 0.32f, 0.26f);

    // Cannon explosion extra detail: an inner shockwave ring and radial debris spokes,
    // drawn inside the unchanged RadiusAt / AlphaAt envelope.
    public const float CannonExplosionInnerRingScale = 0.62f;
    public const float CannonExplosionInnerRingWidth = 1.5f;
    public const int CannonExplosionSpokes = 6;
    public const float CannonExplosionSpokeInnerRatio = 0.35f;
    public const float CannonExplosionSpokeWidth = 1.5f;
    public const float CannonExplosionSpokeLengthRatio = 1.14f; // x radius at full expansion

    // Spawn portals and base core: background markers that render below gameplay and
    // never intercept input. Low fill alpha keeps them clearly behind the action.
    public static readonly Color SpawnPortalFillColor = new Color(0.5f, 0.05f, 0.02f, 0.22f);
    public static readonly Color SpawnPortalRingColor = new Color(0.8f, 0.18f, 0.08f, 0.85f);
    public static readonly Color SpawnPortalArcColor = new Color(1f, 0.42f, 0.12f, 0.9f);
    public static readonly Color SpawnPortalCoreColor = new Color(1f, 0.55f, 0.2f, 0.9f);
    public const float SpawnPortalOuterRadiusRatio = 0.4f;   // x cell size
    public const float SpawnPortalInnerRadiusRatio = 0.24f;
    public const float SpawnPortalCoreRadiusRatio = 0.1f;
    public const float SpawnPortalRotationSpeed = 0.7f;     // radians/sec
    public const float SpawnPortalPulseAmplitude = 0.04f;
    public const float SpawnPortalPulseSpeed = 2.4f;
    public const int SpawnPortalArcSegments = 24;
    public static readonly Color BaseCoreFillColor = new Color(1f, 0.88f, 0.4f, 0.28f);
    public static readonly Color BaseCoreStrokeColor = new Color(1f, 0.9f, 0.5f, 0.9f);
    public static readonly Color BaseCoreShieldColor = new Color(1f, 0.98f, 0.85f, 0.75f);
    public const float BaseCoreHexRadiusRatio = 0.28f;       // x cell size
    public const float BaseCoreShieldRadiusRatio = 0.4f;
    public const float BaseCorePulseAmplitude = 0.05f;
    public const float BaseCorePulseSpeed = 1.8f;

    // HUD tower placement cards. Buttons keep their node names/paths and signals;
    // only their content and size change.
    public const float SidebarCardWidth = 160f;
    public const float SidebarCardHeight = 72f;
    public const float SidebarCardGap = 8f;
    // Start below the HUD top bar so no card overlaps it.
    public const float SidebarCardTop = TopBarHeight + 8f;
    public const float SidebarCardIconSize = 36f;
    public const float SidebarCardIconInset = 10f;
    public const float SidebarCardTextX = 54f;
    public const float SidebarStartWaveGap = 12f;
    public static readonly Color SidebarCardCostColor = new Color(1f, 0.9f, 0.2f);
    public static readonly Color SidebarCardDisabledModulate = new Color(0.42f, 0.42f, 0.48f, 0.85f);

    // Skill tree decoration.
    public const float SkillTreeLineGlowWidth = 8f;
    public const float SkillTreeLineGlowAlpha = 0.18f;
    public const float SkillTreeLineCoreWidth = 2.5f;
    public const float SkillTreeHaloRadius = 19.5f;         // inside the 46x46 icon box
    public const float SkillTreeHaloWidth = 2.5f;
    public const float SkillTreeGlyphRadius = 13f;
    public const float SkillTreeHoverBrightness = 1.2f;
    public const float SkillTreeIdlePulseAmplitude = 0.05f;
    public const float SkillTreeIdlePulseSpeed = 1.6f;
    public const float SkillTreeBackdropDotSpacing = 40f;
    public const float SkillTreeBackdropSpineGlowWidth = 60f;
    public const float SkillTreeHoverBandAlpha = 0.14f;
    public static readonly Color SkillTreeLockedColor = new Color(0.3f, 0.3f, 0.35f);
    public static readonly Color SkillTreeMaxColor = new Color(1f, 0.82f, 0.2f);
    public static readonly Color SkillTreeBackdropDotColor = new Color(0.45f, 0.55f, 0.9f, 0.07f);

    // Skill info band (bottom of the skill-tree screen). The idle hint is shown until
    // a row is hovered.
    public const string SkillInfoIdleHint = "Hover a skill to see its description.";
    public const float SkillInfoBandHeight = 104f;
    public const float SkillInfoBandBottomMargin = 12f;
    public static readonly Color SkillInfoPanelColor = new Color(0.06f, 0.06f, 0.14f, 0.95f);
    public static readonly Color SkillInfoPanelBorderColor = new Color(0.3f, 0.35f, 0.6f, 0.9f);
    public static readonly Color SkillInfoNameColor = new Color(0.95f, 0.97f, 1f);
    public static readonly Color SkillInfoRankColor = new Color(1f, 0.85f, 0.3f);
    public static readonly Color SkillInfoDescriptionColor = new Color(0.72f, 0.75f, 0.88f);
    public static readonly Color SkillInfoLockedHintColor = new Color(1f, 0.62f, 0.45f);

    // Title screen backdrop and menu decorations.
    public const float TitleBackdropRotationSpeed = 0.06f;  // radians/sec
    public const float TitleBackdropPulseSpeed = 0.8f;
    public const float TitleBackdropDriftAmplitude = 18f;
    public const float TitleBackdropDriftSpeed = 0.25f;
    public const float TitleBackdropRingRadiusRatio = 0.42f;
    public static readonly Color TitleBackdropLineColor = new Color(0.35f, 0.45f, 0.8f, 0.12f);
    public static readonly Color TitleBackdropGlowColor = new Color(0.25f, 0.4f, 0.9f, 0.10f);
    public static readonly Color TitleBackdropDotColor = new Color(0.5f, 0.6f, 0.95f, 0.2f);
    public static readonly Color TitleOrnamentColor = new Color(0.5f, 0.65f, 1f, 0.65f);

    // Result backdrops. The intro animation runs once and settles (no endless loop).
    public const float ResultBackdropDuration = 1.4f;       // seconds
    public const int ResultBackdropRings = 3;
    public const int ResultBackdropShardCount = 14;
    public static readonly Color VictoryBackdropGold = new Color(1f, 0.82f, 0.25f, 0.9f);
    public static readonly Color VictoryBackdropWhite = new Color(1f, 0.98f, 0.85f, 0.85f);
    public static readonly Color DefeatBackdropRed = new Color(0.95f, 0.25f, 0.15f, 0.9f);
    public static readonly Color DefeatBackdropOrange = new Color(1f, 0.5f, 0.1f, 0.8f);
    public static readonly Color ResultVignetteColor = new Color(0f, 0f, 0f, 0.5f);

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
