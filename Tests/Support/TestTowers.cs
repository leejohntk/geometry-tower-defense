using GeometryTowerDefense;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Shared tower factory for unit suites: maps a <see cref="TowerType"/> to its concrete
/// tower and places it on the grid, so tests do not grow duplicate switch factories that
/// can drift apart.
/// </summary>
public static class TestTowers
{
    /// <summary>
    /// Creates and initializes a tower of the given type at the given grid cell.
    /// </summary>
    public static Tower Create(TowerType type = TowerType.Arrow, int row = 5, int col = 10)
    {
        Tower tower = type switch
        {
            TowerType.Cannon => new CannonTower(),
            TowerType.Laser => new LaserTower(),
            _ => new ArrowTower()
        };
        tower.Initialize(row, col);
        return tower;
    }
}
