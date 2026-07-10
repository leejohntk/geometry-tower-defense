using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Manages the 20x20 grid: cell occupancy, path detection, coordinate conversion.
/// The game coordinate system uses column (x) and row (y) where (0,0) is top-left.
/// Row 10 is the horizontal path from left to right.
/// Grid is drawn via _Draw() with individual line segments (not a single Line2D polyline).
/// </summary>
public partial class GridManager : Node2D
{
    // Grid state: true = occupied by tower
    private readonly bool[,] _occupied = new bool[GameConstants.GridRows, GameConstants.GridCols];

    // Placement preview nodes
    private ColorRect? _previewHighlight;
    private Polygon2D? _previewTower;
    private Control? _previewRange;

    public override void _Ready()
    {
        // Draw the grid background and lines via _Draw()
        QueueRedraw();

        // Create placement preview (hidden initially)
        CreatePlacementPreview();

        // Create spawn zone and base indicator nodes
        CreateSpawnAndBaseIndicators();
    }

    public override void _Draw()
    {
        float cs = GameConstants.CellSize;

        // Draw cell backgrounds
        for (int r = 0; r < GameConstants.GridRows; r++)
        {
            for (int c = 0; c < GameConstants.GridCols; c++)
            {
                Vector2 pos = new Vector2(c * cs, r * cs);
                Vector2 size = new Vector2(cs, cs);
                Color color = r == GameConstants.PathRow
                    ? new Color(0.8f, 0.7f, 0.5f, 0.3f)  // Tan path background
                    : new Color(0.1f, 0.1f, 0.15f, 0.3f); // Dark tint non-path
                DrawRect(new Rect2(pos, size), color);
            }
        }

        // Draw grid lines — each segment drawn separately (NOT as a polyline)
        Color lineColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        float width = GameConstants.PlayAreaWidth;
        float height = GameConstants.PlayAreaHeight;

        // Vertical lines
        for (int c = 0; c <= GameConstants.GridCols; c++)
        {
            float x = c * cs;
            DrawLine(new Vector2(x, 0), new Vector2(x, height), lineColor, 1);
        }

        // Horizontal lines
        for (int r = 0; r <= GameConstants.GridRows; r++)
        {
            float y = r * cs;
            DrawLine(new Vector2(0, y), new Vector2(width, y), lineColor, 1);
        }
    }

    private void CreatePlacementPreview()
    {
        const int previewZ = 10;

        // Cell highlight rectangle (green = valid, red = invalid)
        _previewHighlight = new ColorRect();
        _previewHighlight.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        _previewHighlight.Color = new Color(0.0f, 1.0f, 0.0f, 0.3f);
        _previewHighlight.Visible = false;
        _previewHighlight.ZIndex = previewZ;
        AddChild(_previewHighlight);

        // Ghost tower (semi-transparent blue triangle)
        var triPoints = new Vector2[]
        {
            new Vector2(0, -(GameConstants.CellSize / 2f - 4)),                            // Top center
            new Vector2(-(GameConstants.CellSize / 2f - 4), GameConstants.CellSize / 2f - 4), // Bottom left
            new Vector2(GameConstants.CellSize / 2f - 4, GameConstants.CellSize / 2f - 4)     // Bottom right
        };
        _previewTower = new Polygon2D();
        _previewTower.Polygon = triPoints;
        _previewTower.Color = new Color(0.2f, 0.5f, 1.0f, 0.45f);
        _previewTower.Visible = false;
        _previewTower.ZIndex = previewZ + 1;
        AddChild(_previewTower);

        // Range preview circle (shown on valid placements)
        float rangePx = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);
        _previewRange = new Control();
        _previewRange.Size = new Vector2(rangePx * 2, rangePx * 2);
        _previewRange.MouseFilter = Control.MouseFilterEnum.Ignore;
        _previewRange.Visible = false;
        _previewRange.ZIndex = previewZ - 1;
        _previewRange.Draw += () =>
        {
            if (!IsInstanceValid(_previewRange)) return;
            _previewRange.DrawCircle(
                new Vector2(rangePx, rangePx), rangePx,
                new Color(0.2f, 0.5f, 1.0f, 0.12f)
            );
            _previewRange.DrawCircle(
                new Vector2(rangePx, rangePx), rangePx,
                new Color(0.2f, 0.5f, 1.0f, 0.45f),
                false, 2.0f
            );
        };
        AddChild(_previewRange);
    }

    private void CreateSpawnAndBaseIndicators()
    {
        // Spawn zone indicator on left edge (col 0)
        var spawnZone = new ColorRect();
        spawnZone.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        spawnZone.Position = new Vector2(0, GameConstants.PathRow * GameConstants.CellSize);
        spawnZone.Color = new Color(1f, 0.2f, 0.2f, 0.15f);
        AddChild(spawnZone);

        // Spawn label
        var spawnLabel = new Label();
        spawnLabel.Text = "SPAWN";
        spawnLabel.Position = new Vector2(
            GameConstants.CellCenterX(0) - 25,
            GameConstants.CellCenterY(GameConstants.PathRow) - 8
        );
        spawnLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f, 0.6f));
        spawnLabel.Scale = new Vector2(0.7f, 0.7f);
        AddChild(spawnLabel);

        // House/base indicator on the right edge of the path
        // Centered in the last cell so the entire house is within the grid bounds
        // and not overlapped by the sidebar.
        float houseX = GameConstants.CellCenterX(GameConstants.GridCols - 1);
        float houseY = GameConstants.CellCenterY(GameConstants.PathRow);

        // House body
        var houseBody = new ColorRect();
        houseBody.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        houseBody.Position = new Vector2(houseX - GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f);
        houseBody.Color = new Color(0.9f, 0.4f, 0.2f, 0.6f);
        AddChild(houseBody);

        // House roof (triangle)
        var roofPoints = new Vector2[]
        {
            new Vector2(houseX, houseY - GameConstants.CellSize / 2f - 16),      // Peak
            new Vector2(houseX - GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f), // Bottom left
            new Vector2(houseX + GameConstants.CellSize / 2f, houseY - GameConstants.CellSize / 2f)  // Bottom right
        };
        var roof = new Polygon2D();
        roof.Polygon = roofPoints;
        roof.Color = new Color(0.8f, 0.2f, 0.1f, 0.8f);
        AddChild(roof);

        // Base label
        var baseLabel = new Label();
        baseLabel.Text = "BASE";
        baseLabel.Position = new Vector2(houseX - 20, houseY - 8);
        baseLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.5f, 0.8f));
        baseLabel.Scale = new Vector2(0.7f, 0.7f);
        AddChild(baseLabel);
    }

    // === Placement Preview ===

    /// <summary>
    /// Show the placement preview at the given grid position.
    /// Green highlight + tower ghost + range circle for valid cells,
    /// red highlight (no tower) for invalid cells.
    /// </summary>
    public void ShowPlacementPreview(int row, int col, bool canPlace)
    {
        if (_previewHighlight == null || _previewTower == null || _previewRange == null)
            return;

        Vector2 cellPos = new Vector2(col * GameConstants.CellSize, row * GameConstants.CellSize);
        Vector2 center = new Vector2(
            GameConstants.CellCenterX(col),
            GameConstants.CellCenterY(row)
        );

        // Highlight
        _previewHighlight.Position = cellPos;
        _previewHighlight.Color = canPlace
            ? new Color(0.0f, 1.0f, 0.0f, 0.3f)   // green
            : new Color(1.0f, 0.0f, 0.0f, 0.3f);   // red
        _previewHighlight.Visible = true;

        // Tower ghost — only on valid spots
        _previewTower.Position = center;
        _previewTower.Visible = canPlace;

        // Range indicator — only on valid spots
        float rangePx = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);
        _previewRange.Position = new Vector2(center.X - rangePx, center.Y - rangePx);
        _previewRange.Visible = canPlace;
        if (canPlace)
            _previewRange.QueueRedraw();
    }

    /// <summary>
    /// Hide the placement preview.
    /// </summary>
    public void HidePlacementPreview()
    {
        if (_previewHighlight != null)
            _previewHighlight.Visible = false;
        if (_previewTower != null)
            _previewTower.Visible = false;
        if (_previewRange != null)
            _previewRange.Visible = false;
    }

    // === Grid Queries ===

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

    // === Path ===

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

    // === Coordinate Conversion ===

    /// <summary>
    /// Converts a global pixel position to grid coordinates.
    /// </summary>
    public Vector2I PixelToGrid(Vector2 pixelPos)
    {
        int col = Mathf.FloorToInt(pixelPos.X / GameConstants.CellSize);
        int row = Mathf.FloorToInt(pixelPos.Y / GameConstants.CellSize);
        return new Vector2I(col, row);
    }

    // === Range ===

    /// <summary>
    /// Returns true if the given row/col is within rangeCells of the given tower position.
    /// </summary>
    public bool IsInRange(int towerRow, int towerCol, int targetRow, int targetCol, int rangeCells)
    {
        float dx = targetCol - towerCol;
        float dy = targetRow - towerRow;
        return dx * dx + dy * dy <= rangeCells * rangeCells;
    }
}
