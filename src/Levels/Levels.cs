using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Registry of all playable levels. Level 1 is the original straight-path level;
/// Level 2 adds the winding path, the Cannon tower, and Swarm enemies.
/// </summary>
public static class Levels
{
    public static readonly LevelDefinition Level1 = CreateLevel1();
    public static readonly LevelDefinition Level2 = CreateLevel2();

    /// <summary>
    /// Resolve a level by id. Unknown ids fall back to Level 1.
    /// </summary>
    public static LevelDefinition Get(int id) => id switch
    {
        2 => Level2,
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

        return new LevelDefinition(1, "Level 1", path, allowCannonTower: false, waves);
    }

    private static LevelDefinition CreateLevel2()
    {
        // Winding path: spawn (0,10) -> base (19,10), four vertical direction changes.
        var corners = new[]
        {
            new Vector2I(0, 10),
            new Vector2I(3, 10),
            new Vector2I(3, 14),
            new Vector2I(7, 14),
            new Vector2I(7, 6),
            new Vector2I(11, 6),
            new Vector2I(11, 14),
            new Vector2I(15, 14),
            new Vector2I(15, 10),
            new Vector2I(19, 10)
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

        return new LevelDefinition(2, "Level 2", path, allowCannonTower: true, waves);
    }

    private static WaveDefinition BasicWave(int count)
    {
        var spawns = new SpawnKind[count];
        for (int i = 0; i < count; i++)
            spawns[i] = SpawnKind.Basic;
        return new WaveDefinition(spawns);
    }
}
