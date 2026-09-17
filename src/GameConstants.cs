using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// All game tuning parameters in one place. No magic numbers in game code.
/// </summary>
public static class GameConstants
{
    // Grid
    public const int GridCols = 20;
    public const int GridRows = 20;
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
    public const int SwarmEnemyHP = 3;
    public const int SwarmEnemyDiameter = 24;
    public const float SwarmEnemySpeed = 2f;
    public const int SwarmClusterSize = 3;
    // Radius of the invisible circle the swarm members orbit their path anchor.
    // At 20px, adjacent members sit ~34.6px apart (clearing the 24px diameter with
    // ~10px of gap) and the central hole has ~8px of clear radius.
    public const float SwarmClusterRadius = 20f;
    public const int SwarmCoinDropPerKill = 1;

    // Projectile
    public const float ProjectileSpeed = 8f;
    public const int ProjectileSize = 12;

    // Cannon Explosion Effect — brief expanding circle at the projectile impact point.
    // The visual expands to exactly CannonTowerAoeRadius so the player sees the true
    // damage extent; GameManager drives both the damage and the visual from the same constant.
    public const float CannonExplosionDuration = 0.4f;      // seconds
    public const float CannonExplosionStrokeWidth = 2.0f;   // outline ring width, pixels
    public static readonly Color CannonExplosionFillColor = new Color(0.3f, 0.6f, 0.4f);    // matches cannon range color
    public static readonly Color CannonExplosionOutlineColor = new Color(0.1f, 0.25f, 0.15f); // matches cannon tower outline

    // Waves
    public const int TotalWaves = 5;

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
    public static int TowerCost(TowerType type) =>
        type == TowerType.Cannon ? CannonTowerCost : ArrowTowerCost;

    public static int TowerRange(TowerType type) =>
        type == TowerType.Cannon ? CannonTowerRange : ArrowTowerRange;
}
