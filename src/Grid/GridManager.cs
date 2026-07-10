using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Manages the 20x20 grid: cell occupancy, path detection, coordinate conversion.
/// The game coordinate system uses column (x) and row (y) where (0,0) is top-left.
/// Row 10 is the horizontal path from left to right.
/// </summary>
public partial class GridManager : Node2D
{
    // Grid state: true = occupied by tower
    private readonly bool[,] _occupied = new bool[GameConstants.GridRows, GameConstants.GridCols];

    public override void _Ready()
    {
        DrawGrid();
    }

    private void DrawGrid()
    {
        // Draw path cells (row 10) with tan/brown background
        for (int c = 0; c < GameConstants.GridCols; c++)
        {
            var cell = new ColorRect();
            cell.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
            cell.Position = new Vector2(c * GameConstants.CellSize, GameConstants.PathRow * GameConstants.CellSize);
            cell.Color = new Color(0.8f, 0.7f, 0.5f, 0.3f); // Light brown/tan
            AddChild(cell);
        }

        // Draw grid lines
        var lines = new Line2D();
        lines.DefaultColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        lines.Width = 1;

        // Vertical lines
        for (int c = 0; c <= GameConstants.GridCols; c++)
        {
            float x = c * GameConstants.CellSize;
            lines.AddPoint(new Vector2(x, 0));
            lines.AddPoint(new Vector2(x, GameConstants.PlayAreaHeight));
        }

        // Horizontal lines
        for (int r = 0; r <= GameConstants.GridRows; r++)
        {
            float y = r * GameConstants.CellSize;
            lines.AddPoint(new Vector2(0, y));
            lines.AddPoint(new Vector2(GameConstants.PlayAreaWidth, y));
        }

        AddChild(lines);

        // Draw spawn zone indicator on left edge (col 0)
        var spawnZone = new ColorRect();
        spawnZone.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        spawnZone.Position = new Vector2(0, GameConstants.PathRow * GameConstants.CellSize);
        spawnZone.Color = new Color(1f, 0.2f, 0.2f, 0.15f); // Subtle red
        AddChild(spawnZone);

        // Draw spawn text
        var spawnLabel = new Label();
        spawnLabel.Text = "SPAWN";
        spawnLabel.Position = new Vector2(
            GameConstants.CellCenterX(0) - 25,
            GameConstants.CellCenterY(GameConstants.PathRow) - 8
        );
        spawnLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f, 0.6f));
        spawnLabel.Scale = new Vector2(0.7f, 0.7f);
        AddChild(spawnLabel);

        // Draw house/base indicator on the right edge
        float houseX = GameConstants.CellCenterX(GameConstants.GridCols - 1) + GameConstants.CellSize / 2f;
        float houseY = GameConstants.CellCenterY(GameConstants.PathRow);

        // House body (square)
        var houseBody = new ColorRect();
        houseBody.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        houseBody.Position = new Vector2(houseX - GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f);
        houseBody.Color = new Color(0.9f, 0.4f, 0.2f, 0.6f); // Orange/red
        AddChild(houseBody);

        // House roof (triangle) using Polygon2D
        var roofPoints = new Vector2[]
        {
            new Vector2(houseX, houseY - GameConstants.CellSize / 2f - 16), // Peak
            new Vector2(houseX - GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f), // Bottom left
            new Vector2(houseX + GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f) // Bottom right
        };
        var roof = new Polygon2D();
        roof.Polygon = roofPoints;
        roof.Color = new Color(0.8f, 0.2f, 0.1f, 0.8f); // Dark red
        AddChild(roof);

        // "BASE" label
        var baseLabel = new Label();
        baseLabel.Text = "BASE";
        baseLabel.Position = new Vector2(houseX - 20, houseY - 8);
        baseLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.5f, 0.8f));
        baseLabel.Scale = new Vector2(0.7f, 0.7f);
        AddChild(baseLabel);
    }

    /// <summary>
    /// Returns true if the given grid position is on the path row.
    /// </summary>
    public bool IsOnPath(int row, int col)
    {
        return row == GameConstants.PathRow;
    }

    /// <summary>
    /// Returns true if the given grid position is within grid bounds.
    /// </summary>
    public bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < GameConstants.GridRows &&
               col >= 0 && col < GameConstants.GridCols;
    }

    /// <summary>
    /// Returns true if a tower can be placed at the given grid position.
    /// </summary>
    public bool CanPlaceTower(int row, int col)
    {
        return IsInBounds(row, col) && !IsOnPath(row, col) && !_occupied[row, col];
    }

    /// <summary>
    /// Places a tower at the given grid position. Returns false if placement is invalid.
    /// </summary>
    public bool PlaceTower(int row, int col)
    {
        if (!CanPlaceTower(row, col))
            return false;

        _occupied[row, col] = true;
        return true;
    }

    /// <summary>
    /// Gets a list of grid cell center positions along the path row.
    /// Enemies follow these waypoints from left (col 0) to right (col 19).
    /// </summary>
    public List<Vector2> GetPathWaypoints()
    {
        var waypoints = new List<Vector2>();
        for (int c = 0; c < GameConstants.GridCols; c++)
        {
            waypoints.Add(new Vector2(
                GameConstants.CellCenterX(c),
                GameConstants.CellCenterY(GameConstants.PathRow)
            ));
        }
        // Add an extra waypoint past the grid for enemies to reach as "end"
        waypoints.Add(new Vector2(
            GameConstants.CellCenterX(GameConstants.GridCols - 1) + GameConstants.CellSize,
            GameConstants.CellCenterY(GameConstants.PathRow)
        ));
        return waypoints;
    }

    /// <summary>
    /// Converts a global pixel position to grid coordinates.
    /// </summary>
    public Vector2I PixelToGrid(Vector2 pixelPos)
    {
        int col = Mathf.FloorToInt(pixelPos.X / GameConstants.CellSize);
        int row = Mathf.FloorToInt(pixelPos.Y / GameConstants.CellSize);
        return new Vector2I(col, row);
    }

    /// <summary>
    /// Returns true if the given row/col is within 4 cells (range) of the given tower position.
    /// </summary>
    public bool IsInRange(int towerRow, int towerCol, int targetRow, int targetCol, int rangeCells)
    {
        float dx = targetCol - towerCol;
        float dy = targetRow - towerRow;
        return dx * dx + dy * dy <= rangeCells * rangeCells;
    }
}
