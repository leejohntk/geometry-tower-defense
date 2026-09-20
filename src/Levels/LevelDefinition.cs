using Godot;
using System;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// A single spawn entry in a wave. Basic spawns one basic enemy; SwarmCluster
/// spawns a tight cluster of three swarm enemies; Armored spawns one armored enemy.
/// </summary>
public enum SpawnKind
{
    Basic,
    SwarmCluster,
    Armored
}

/// <summary>
/// Composition of a single wave: an ordered list of spawns.
/// </summary>
public class WaveDefinition
{
    public IReadOnlyList<SpawnKind> Spawns { get; }

    public WaveDefinition(params SpawnKind[] spawns)
    {
        Spawns = spawns;
    }

    /// <summary>
    /// Total number of individual enemies this wave produces.
    /// </summary>
    public int TotalEnemies
    {
        get
        {
            int total = 0;
            foreach (var spawn in Spawns)
                total += Resolve(spawn).Count;
            return total;
        }
    }

    /// <summary>
    /// Maps a spawn kind to the enemy kind it produces and how many enemies it
    /// spawns. Basic and Armored produce one enemy; SwarmCluster produces
    /// GameConstants.SwarmClusterSize swarm enemies. This is the single source of
    /// truth for the spawn-kind mapping — a new enemy kind is added here only.
    /// </summary>
    public static (EnemyKind Kind, int Count) Resolve(SpawnKind spawn) => spawn switch
    {
        SpawnKind.Basic => (EnemyKind.Basic, 1),
        SpawnKind.Armored => (EnemyKind.Armored, 1),
        SpawnKind.SwarmCluster => (EnemyKind.Swarm, GameConstants.SwarmClusterSize),
        _ => throw new System.ArgumentOutOfRangeException(nameof(spawn), spawn, "Unknown spawn kind.")
    };
}

/// <summary>
/// Per-level configuration: enemy routes, tower availability, and wave composition.
/// </summary>
public class LevelDefinition
{
    public int Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<IReadOnlyList<Vector2I>> Paths { get; }
    public IReadOnlyList<WaveDefinition> Waves { get; }
    public bool AllowCannonTower { get; }
    public bool AllowLaserTower { get; }

    /// <summary>
    /// The grid cell (col, row) where each route spawns enemies — one per route (route[0]).
    /// </summary>
    public IReadOnlyList<Vector2I> SpawnCells { get; }

    /// <summary>
    /// The grid cell (col, row) of the home base. Every route ends at this same cell.
    /// </summary>
    public Vector2I BaseCell { get; }

    public LevelDefinition(
        int id,
        string displayName,
        IReadOnlyList<IReadOnlyList<Vector2I>> paths,
        bool allowCannonTower,
        bool allowLaserTower,
        IReadOnlyList<WaveDefinition> waves)
    {
        if (paths.Count == 0)
            throw new ArgumentException("A level needs at least one route.", nameof(paths));

        Id = id;
        DisplayName = displayName;
        Paths = paths;
        AllowCannonTower = allowCannonTower;
        AllowLaserTower = allowLaserTower;
        Waves = waves;

        var spawnCells = new Vector2I[paths.Count];
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].Count == 0)
                throw new ArgumentException("Every route needs at least one cell.", nameof(paths));
            spawnCells[i] = paths[i][0];
        }
        SpawnCells = spawnCells;

        Vector2I baseCell = paths[0][^1];
        for (int i = 1; i < paths.Count; i++)
        {
            if (paths[i][^1] != baseCell)
                throw new ArgumentException("All routes must end at the same base cell.", nameof(paths));
        }
        BaseCell = baseCell;
    }

    /// <summary>
    /// Expands axis-aligned corner points into a contiguous list of cells.
    /// Each consecutive pair of corners must share a row or column.
    /// </summary>
    public static List<Vector2I> BuildPath(IReadOnlyList<Vector2I> corners)
    {
        if (corners.Count < 2)
            throw new ArgumentException("Path needs at least 2 corners.", nameof(corners));

        var cells = new List<Vector2I>();

        for (int i = 0; i < corners.Count - 1; i++)
        {
            Vector2I from = corners[i];
            Vector2I to = corners[i + 1];
            int dc = Math.Sign(to.X - from.X);
            int dr = Math.Sign(to.Y - from.Y);

            if (dc != 0 && dr != 0)
                throw new ArgumentException("Path segments must be axis-aligned.", nameof(corners));
            if (dc == 0 && dr == 0)
                throw new ArgumentException("Path corners must not repeat.", nameof(corners));

            // Skip the shared corner on every segment after the first.
            Vector2I cur = i == 0 ? from : new Vector2I(from.X + dc, from.Y + dr);

            while (true)
            {
                cells.Add(cur);
                if (cur == to)
                    break;
                cur = new Vector2I(cur.X + dc, cur.Y + dr);
            }
        }

        return cells;
    }

    /// <summary>
    /// Returns true if every cell is within [0, cols) x [0, rows) and consecutive cells are adjacent.
    /// </summary>
    public static bool IsPathConnectedAndInBounds(IReadOnlyList<Vector2I> path, int cols, int rows)
    {
        if (path.Count == 0)
            return false;

        foreach (var cell in path)
        {
            if (cell.X < 0 || cell.X >= cols || cell.Y < 0 || cell.Y >= rows)
                return false;
        }

        for (int i = 1; i < path.Count; i++)
        {
            Vector2I a = path[i - 1];
            Vector2I b = path[i];
            if (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) != 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if any cell is visited more than once.
    /// </summary>
    public static bool PathSelfIntersects(IReadOnlyList<Vector2I> path)
    {
        var seen = new HashSet<Vector2I>();
        foreach (var cell in path)
        {
            if (!seen.Add(cell))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Counts vertical direction reversals (moving down to moving up, or vice versa).
    /// Horizontal segments do not affect the count.
    /// </summary>
    public static int VerticalDirectionChangeCount(IReadOnlyList<Vector2I> path)
    {
        int changes = 0;
        int lastDr = 0;

        for (int i = 1; i < path.Count; i++)
        {
            int dr = Math.Sign(path[i].Y - path[i - 1].Y);
            if (dr == 0)
                continue;

            if (lastDr != 0 && dr != lastDr)
                changes++;

            lastDr = dr;
        }

        return changes;
    }
}
