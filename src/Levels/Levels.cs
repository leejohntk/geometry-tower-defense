using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Registry of all playable levels. Level 1 is the original straight-path level;
/// Level 2 adds the winding path, the Cannon tower, and Swarm enemies;
/// Level 3 adds Armored enemies and the Laser tower;
/// Level 4 adds a second spawn route and mixes all three enemy kinds.
/// </summary>
public static class Levels
{
    public static readonly LevelDefinition Level1 = CreateLevel1();
    public static readonly LevelDefinition Level2 = CreateLevel2();
    public static readonly LevelDefinition Level3 = CreateLevel3();
    public static readonly LevelDefinition Level4 = CreateLevel4();

    /// <summary>
    /// Resolve a level by id. Unknown ids fall back to Level 1.
    /// </summary>
    public static LevelDefinition Get(int id) => id switch
    {
        2 => Level2,
        3 => Level3,
        4 => Level4,
        _ => Level1
    };

    private static LevelDefinition CreateLevel1()
    {
        // Straight horizontal path along row 10, left to right.
        var corners = new[]
        {
            new Vector2I(0, GameConstants.PathRow),
            new Vector2I(GameConstants.GridCols - 1, GameConstants.PathRow)
        };
        var path = LevelDefinition.BuildPath(corners);

        var waves = new List<WaveDefinition>
        {
            BasicWave(3),
            BasicWave(5),
            BasicWave(7),
            BasicWave(9),
            BasicWave(12)
        };

        return new LevelDefinition(1, "Level 1", new[] { path }, allowCannonTower: false, allowLaserTower: false, waves);
    }

    private static LevelDefinition CreateLevel2()
    {
        // Winding path: spawn (0,7) -> base (19,7), three vertical direction changes.
        var corners = new[]
        {
            new Vector2I(0, 7),
            new Vector2I(3, 7),
            new Vector2I(3, 11),
            new Vector2I(7, 11),
            new Vector2I(7, 3),
            new Vector2I(11, 3),
            new Vector2I(11, 11),
            new Vector2I(15, 11),
            new Vector2I(15, 7),
            new Vector2I(19, 7)
        };
        var path = LevelDefinition.BuildPath(corners);

        var waves = new List<WaveDefinition>
        {
            // Wave 1: 4 basic
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic),
            // Wave 2: 6 basic
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic),
            // Wave 3: 4 basic + 1 swarm cluster (7 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster),
            // Wave 4: 5 basic + 2 swarm clusters (11 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster),
            // Wave 5: 6 basic + 3 swarm clusters (15 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster)
        };

        return new LevelDefinition(2, "Level 2", new[] { path }, allowCannonTower: true, allowLaserTower: false, waves);
    }

    private static LevelDefinition CreateLevel3()
    {
        // Winding path with four vertical direction changes; introduces Armored enemies
        // and the Laser tower.
        var corners = new[]
        {
            new Vector2I(0, 5),
            new Vector2I(4, 5),
            new Vector2I(4, 10),
            new Vector2I(8, 10),
            new Vector2I(8, 3),
            new Vector2I(12, 3),
            new Vector2I(12, 10),
            new Vector2I(16, 10),
            new Vector2I(16, 5),
            new Vector2I(19, 5)
        };
        var path = LevelDefinition.BuildPath(corners);

        var waves = new List<WaveDefinition>
        {
            // Wave 1: 4 basic
            BasicWave(4),
            // Wave 2: 6 basic
            BasicWave(6),
            // Wave 3: 4 basic + 1 swarm cluster (7 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster),
            // Wave 4: 3 basic + 2 armored (5 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Armored, SpawnKind.Armored),
            // Wave 5: 2 basic + 1 swarm cluster + 2 armored (7 enemies)
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.Armored, SpawnKind.Armored)
        };

        return new LevelDefinition(3, "Level 3", new[] { path }, allowCannonTower: true, allowLaserTower: true, waves);
    }

    private static LevelDefinition CreateLevel4()
    {
        // Two routes that converge in the center, split, converge, split, then a
        // final converge into a single base at (19,7). Shared converge cells are
        // (5,7), (11,7) and (17,7) plus the row-7 runs between them.
        var routeACorners = new[]
        {
            new Vector2I(0, 3), new Vector2I(5, 3), new Vector2I(5, 7), new Vector2I(8, 7),
            new Vector2I(8, 4), new Vector2I(11, 4), new Vector2I(11, 7), new Vector2I(14, 7),
            new Vector2I(14, 10), new Vector2I(17, 10), new Vector2I(17, 7), new Vector2I(19, 7)
        };
        var routeBCorners = new[]
        {
            new Vector2I(0, 11), new Vector2I(5, 11), new Vector2I(5, 7), new Vector2I(8, 7),
            new Vector2I(8, 10), new Vector2I(11, 10), new Vector2I(11, 7), new Vector2I(14, 7),
            new Vector2I(14, 4), new Vector2I(17, 4), new Vector2I(17, 7), new Vector2I(19, 7)
        };
        var paths = new IReadOnlyList<Vector2I>[]
        {
            LevelDefinition.BuildPath(routeACorners),
            LevelDefinition.BuildPath(routeBCorners)
        };

        var waves = new List<WaveDefinition>
        {
            // Wave 1: 3 Basic + 1 SwarmCluster + 1 Armored
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.Armored),
            // Wave 2: 3 Basic + 2 SwarmCluster + 1 Armored
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.Armored),
            // Wave 3: 2 Basic + 2 SwarmCluster + 2 Armored
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.Armored, SpawnKind.Armored),
            // Wave 4: 2 Basic + 3 SwarmCluster + 3 Armored
            new WaveDefinition(SpawnKind.Basic, SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.Armored, SpawnKind.Armored, SpawnKind.Armored),
            // Wave 5: 1 Basic + 3 SwarmCluster + 4 Armored
            new WaveDefinition(SpawnKind.Basic, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.SwarmCluster, SpawnKind.Armored, SpawnKind.Armored, SpawnKind.Armored, SpawnKind.Armored)
        };

        return new LevelDefinition(4, "Level 4", paths, allowCannonTower: true, allowLaserTower: true, waves);
    }

    private static WaveDefinition BasicWave(int count)
    {
        var spawns = new SpawnKind[count];
        for (int i = 0; i < count; i++)
            spawns[i] = SpawnKind.Basic;
        return new WaveDefinition(spawns);
    }
}
