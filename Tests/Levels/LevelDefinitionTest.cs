using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Pure logic tests for level definitions (no Godot node instantiation required).
/// </summary>
[TestSuite]
public class LevelDefinitionTest
{
    [TestCase]
    public void Level1_HasStraightPathOnRow10()
    {
        var path = Levels.Level1.PathCells;

        AssertThat(path.Count).IsEqual(GameConstants.GridCols);
        AssertThat(Levels.Level1.SpawnCell).IsEqual(new Vector2I(0, GameConstants.PathRow));
        AssertThat(Levels.Level1.BaseCell).IsEqual(new Vector2I(GameConstants.GridCols - 1, GameConstants.PathRow));

        foreach (var cell in path)
            AssertThat(cell.Y).IsEqual(GameConstants.PathRow);
    }

    [TestCase]
    public void Level1_IsArrowOnly_WithBasicWaves()
    {
        AssertThat(Levels.Level1.AllowCannonTower).IsFalse();
        AssertThat(Levels.Level1.Waves.Count).IsEqual(5);

        int[] expected = { 3, 5, 7, 9, 12 };
        for (int i = 0; i < expected.Length; i++)
            AssertThat(Levels.Level1.Waves[i].TotalEnemies).IsEqual(expected[i]);
    }

    [TestCase]
    public void Level2_PathIsValid_AndWindsAtLeastThreeTimes()
    {
        var path = Levels.Level2.PathCells;

        AssertThat(path.Count > 0).IsTrue();
        AssertThat(Levels.Level2.SpawnCell).IsEqual(new Vector2I(0, 7));
        AssertThat(Levels.Level2.BaseCell).IsEqual(new Vector2I(19, 7));

        AssertThat(LevelDefinition.IsPathConnectedAndInBounds(path, GameConstants.GridCols, GameConstants.GridRows)).IsTrue();
        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsFalse();
        AssertThat(LevelDefinition.VerticalDirectionChangeCount(path) >= 3).IsTrue();
    }

    [TestCase]
    public void Level2_AllowsCannon_WithSwarmClustersFromWave3()
    {
        AssertThat(Levels.Level2.AllowCannonTower).IsTrue();
        AssertThat(Levels.Level2.Waves.Count).IsEqual(5);

        // Waves 1-2 are basic-only; waves 3+ mix in swarm clusters.
        AssertThat(Levels.Level2.Waves[0].TotalEnemies).IsEqual(4);
        AssertThat(Levels.Level2.Waves[1].TotalEnemies).IsEqual(6);
        AssertThat(Levels.Level2.Waves[2].TotalEnemies).IsEqual(7);
        AssertThat(Levels.Level2.Waves[3].TotalEnemies).IsEqual(11);
        AssertThat(Levels.Level2.Waves[4].TotalEnemies).IsEqual(15);
    }

    [TestCase]
    public void BuildPath_ExpandsCornersWithoutDuplicates()
    {
        var corners = new[] { new Vector2I(0, 0), new Vector2I(2, 0), new Vector2I(2, 2) };
        var path = LevelDefinition.BuildPath(corners);

        AssertThat(path.Count).IsEqual(5); // (0,0),(1,0),(2,0),(2,1),(2,2)
        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsFalse();
        AssertThat(LevelDefinition.IsPathConnectedAndInBounds(path, 20, 20)).IsTrue();
    }

    [TestCase]
    public void BuildPath_DetectsSelfIntersection()
    {
        // Loop revisits (0,0): (0,0)->(2,0)->(2,2)->(0,2)->(0,0)
        var corners = new[] { new Vector2I(0, 0), new Vector2I(2, 0), new Vector2I(2, 2), new Vector2I(0, 2), new Vector2I(0, 0) };
        var path = LevelDefinition.BuildPath(corners);

        AssertThat(LevelDefinition.PathSelfIntersects(path)).IsTrue();
    }
}
