using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Spawn/base markers: the old red spawn cell, orange house, and SPAWN/BASE labels are
/// gone; each spawn cell has a named click-through portal and the base cell has a
/// click-through BaseCore, all background-layered and animated by transforms.
/// </summary>
[TestSuite]
public class GridMarkersTest
{
    private static GridManager BuildGrid(LevelDefinition level)
    {
        var grid = new GridManager();
        grid.Configure(level);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(grid);
        return grid;
    }

    [TestCase]
    public void Level4_HasOneNamedPortalPerSpawnCell_AndABaseCore()
    {
        var grid = BuildGrid(Levels.Level4);
        try
        {
            AssertThat(grid.SpawnPortals.Count).IsEqual(Levels.Level4.SpawnCells.Count);
            AssertThat(grid.SpawnPortals.Count).IsEqual(2);

            foreach (var spawn in Levels.Level4.SpawnCells)
            {
                var portal = grid.GetNodeOrNull<Control>($"SpawnPortal_{spawn.X}_{spawn.Y}");
                AssertThat(portal).IsNotNull();
                AssertThat(portal!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(portal.ZIndex).IsLess(0);
                AssertThat(portal.Position).IsEqual(new Vector2(
                    spawn.X * GameConstants.CellSize,
                    spawn.Y * GameConstants.CellSize));
                AssertThat(portal.Size).IsEqual(new Vector2(
                    GameConstants.CellSize,
                    GameConstants.CellSize));
                // The control's centre is the spawn cell centre.
                AssertThat(portal.GetGlobalRect().GetCenter()).IsEqual(new Vector2(
                    GameConstants.CellCenterX(spawn.X),
                    GameConstants.CellCenterY(spawn.Y)));
            }

            var baseCore = grid.GetNodeOrNull<Control>("BaseCore");
            AssertThat(baseCore).IsNotNull();
            AssertThat(baseCore!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(baseCore.ZIndex).IsLess(0);
            AssertThat(baseCore.Position).IsEqual(new Vector2(
                Levels.Level4.BaseCell.X * GameConstants.CellSize,
                Levels.Level4.BaseCell.Y * GameConstants.CellSize));
            AssertThat(baseCore.GetGlobalRect().GetCenter()).IsEqual(new Vector2(
                GameConstants.CellCenterX(Levels.Level4.BaseCell.X),
                GameConstants.CellCenterY(Levels.Level4.BaseCell.Y)));
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    [TestCase]
    public void Level1_HasASinglePortalAtItsSpawnCell()
    {
        var grid = BuildGrid(Levels.Level1);
        try
        {
            var spawn = Levels.Level1.SpawnCells[0];
            AssertThat(grid.SpawnPortals.Count).IsEqual(1);
            AssertThat(grid.GetNodeOrNull<Control>($"SpawnPortal_{spawn.X}_{spawn.Y}")).IsNotNull();
            AssertThat(grid.GetNodeOrNull<Control>("SpawnPortal_19_10")).IsNull();
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    [TestCase]
    public void NoGameplayLabelMentionsSpawnOrBase()
    {
        var grid = BuildGrid(Levels.Level4);
        try
        {
            var labels = new List<Label>();
            Collect(grid, labels);

            AssertThat(labels.Count).IsEqual(0);
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    [TestCase]
    public void SpawnAndBaseMarkers_DoNotBlockPlacement()
    {
        var grid = BuildGrid(Levels.Level4);
        try
        {
            // The base cell is a path cell regardless of its marker.
            AssertThat(grid.CanPlaceTower(Levels.Level4.BaseCell.Y, Levels.Level4.BaseCell.X)).IsFalse();

            // An off-path cell remains placeable with markers present.
            AssertThat(grid.CanPlaceTower(0, 0)).IsTrue();
            AssertThat(grid.PlaceTower(0, 0)).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    [TestCase]
    public void Markers_AnimateByTransformOnly()
    {
        var grid = BuildGrid(Levels.Level4);
        try
        {
            var portal = grid.SpawnPortals[0];
            var baseCore = grid.BaseCore!;
            float rotationBefore = portal.Rotation;
            Vector2 baseScaleBefore = baseCore.Scale;

            grid._Process(0.4);

            AssertThat(portal.Rotation).IsNotEqual(rotationBefore);
            AssertThat(baseCore.Scale).IsNotEqual(baseScaleBefore);
            // The portal keeps its cell-center pivot so the pulse stays centered.
            AssertThat(portal.PivotOffset).IsEqual(new Vector2(
                GameConstants.CellSize / 2f,
                GameConstants.CellSize / 2f));
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    [TestCase]
    public void GridVisuals_ContainOnlyThePlacementPreviewColorRectAndPolygon()
    {
        var grid = BuildGrid(Levels.Level4);
        try
        {
            var colorRects = new List<ColorRect>();
            Collect(grid, colorRects);
            var polygons = new List<Polygon2D>();
            Collect(grid, polygons);

            // Exactly the placement preview set: one cell highlight and one arrow ghost.
            // Any returning spawn cell / house body / roof polygon would change these
            // counts, so this cannot pass by keying off a generated node name.
            AssertThat(colorRects.Count).IsEqual(1);
            AssertThat(polygons.Count).IsEqual(1);

            var highlight = colorRects[0];
            AssertThat(highlight.Size).IsEqual(new Vector2(
                GameConstants.CellSize,
                GameConstants.CellSize));
            AssertThat(highlight.Color).IsEqual(new Color(0f, 1f, 0f, 0.3f)); // preview green
            AssertThat(highlight.Visible).IsFalse();

            var ghost = polygons[0];
            AssertThat(ghost.Polygon.Length).IsEqual(3); // preview arrow triangle
            AssertThat(ghost.Visible).IsFalse();

            // Showing the real preview reveals exactly these two nodes, proving they are
            // the preview rather than a returning decoration.
            grid.ShowPlacementPreview(0, 0, true, TowerType.Arrow, 4f);
            AssertThat(highlight.Visible).IsTrue();
            AssertThat(ghost.Visible).IsTrue();
            grid.HidePlacementPreview();
            AssertThat(highlight.Visible).IsFalse();
            AssertThat(ghost.Visible).IsFalse();

            // No SPAWN/BASE labels survive anywhere in the grid subtree.
            var labels = new List<Label>();
            Collect(grid, labels);
            AssertThat(labels.Count).IsEqual(0);
        }
        finally
        {
            NodeTestSupport.Cleanup(grid);
        }
    }

    private static void Collect<T>(Node node, List<T> found) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T match)
                found.Add(match);
            Collect(child, found);
        }
    }
}
