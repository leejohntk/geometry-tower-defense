using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Manages the 20x20 grid: cell occupancy, path detection, coordinate conversion.
/// The game coordinate system uses column (x) and row (y) where (0,0) is top-left.
/// The path is defined per-level as a list of cells (not a single row).
/// Grid is drawn via _Draw() with individual line segments.
/// </summary>
public partial class GridManager : Node2D
{
    // Grid state: true = occupied by tower
    private readonly bool[,] _occupied = new bool[GameConstants.GridRows, GameConstants.GridCols];

    // Path cells (col, row) for the active level
    private readonly HashSet<Vector2I> _pathCells = new();

    private LevelDefinition? _level;

    // Placement preview nodes
    private ColorRect? _previewHighlight;
    private Polygon2D? _previewTower;
    private Control? _previewTowerCircle;
    private Control? _previewRange;
    private float _previewRangePixels = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);

    // Cached path waypoints (immutable, computed once per level)
    private List<Vector2>? _cachedWaypoints;

    /// <summary>
    /// Configure this grid for a specific level's path. Must be called before AddChild.
    /// </summary>
    public void Configure(LevelDefinition level)
    {
        _level = level;

        _pathCells.Clear();
        foreach (var cell in level.PathCells)
            _pathCells.Add(cell);

        _cachedWaypoints = null;
    }

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
                Color color = _pathCells.Contains(new Vector2I(c, r))
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

        // Ghost tower for Arrow (semi-transparent blue triangle)
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

        // Ghost tower for Cannon (semi-transparent dark green circle)
        _previewTowerCircle = new Control();
        _previewTowerCircle.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        _previewTowerCircle.MouseFilter = Control.MouseFilterEnum.Ignore;
        _previewTowerCircle.Visible = false;
        _previewTowerCircle.ZIndex = previewZ + 1;
        _previewTowerCircle.Draw += () =>
        {
            if (!IsInstanceValid(_previewTowerCircle)) return;

            float radius = GameConstants.CellSize / 2f - 4;
            Vector2 center = new Vector2(GameConstants.CellSize / 2f, GameConstants.CellSize / 2f);
            _previewTowerCircle.DrawCircle(center, radius, new Color(0.3f, 0.6f, 0.4f, 0.45f));
            _previewTowerCircle.DrawCircle(center, radius, new Color(0.1f, 0.25f, 0.15f, 0.6f), false, 2.0f);
        };
        AddChild(_previewTowerCircle);

        // Range preview circle (shown on valid placements)
        _previewRange = new Control();
        _previewRange.MouseFilter = Control.MouseFilterEnum.Ignore;
        _previewRange.Visible = false;
        _previewRange.ZIndex = previewZ - 1;
        _previewRange.Draw += () =>
        {
            if (!IsInstanceValid(_previewRange)) return;

            float rangePx = _previewRangePixels;
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
        Vector2I spawnCell = _level?.SpawnCell ?? new Vector2I(0, GameConstants.PathRow);
        Vector2I baseCell = _level?.BaseCell ?? new Vector2I(GameConstants.GridCols - 1, GameConstants.PathRow);

        // Spawn zone indicator on the spawn cell
        var spawnZone = new ColorRect();
        spawnZone.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        spawnZone.Position = new Vector2(spawnCell.X * GameConstants.CellSize, spawnCell.Y * GameConstants.CellSize);
        spawnZone.Color = new Color(1f, 0.2f, 0.2f, 0.15f);
        AddChild(spawnZone);

        // Spawn label
        var spawnLabel = new Label();
        spawnLabel.Text = "SPAWN";
        spawnLabel.Position = new Vector2(
            GameConstants.CellCenterX(spawnCell.X) - 25,
            GameConstants.CellCenterY(spawnCell.Y) - 8
        );
        spawnLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f, 0.6f));
        spawnLabel.Scale = new Vector2(0.7f, 0.7f);
        AddChild(spawnLabel);

        // House/base indicator on the base cell
        float houseX = GameConstants.CellCenterX(baseCell.X);
        float houseY = GameConstants.CellCenterY(baseCell.Y);

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
    /// Show the placement preview at the given grid position for the given tower type.
    /// Green highlight + tower ghost + range circle for valid cells,
    /// red highlight (no tower) for invalid cells.
    /// </summary>
    public void ShowPlacementPreview(int row, int col, bool canPlace, TowerType towerType)
    {
        if (_previewHighlight == null || _previewTower == null || _previewTowerCircle == null || _previewRange == null)
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

        // Tower ghost — only on valid spots, shape depends on tower type
        _previewTower.Position = center;
        _previewTower.Visible = canPlace && towerType == TowerType.Arrow;

        _previewTowerCircle.Position = cellPos;
        _previewTowerCircle.Visible = canPlace && towerType == TowerType.Cannon;
        if (canPlace && towerType == TowerType.Cannon)
            _previewTowerCircle.QueueRedraw();

        // Range indicator — only on valid spots
        float rangePx = GameConstants.CellDistanceInPixels(GameConstants.TowerRange(towerType));
        _previewRangePixels = rangePx;
        _previewRange.Size = new Vector2(rangePx * 2, rangePx * 2);
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
        if (_previewTowerCircle != null)
            _previewTowerCircle.Visible = false;
        if (_previewRange != null)
            _previewRange.Visible = false;
    }

    // === Grid Queries ===

    /// <summary>
    /// Returns true if the given grid position is a path cell.
    /// </summary>
    public bool IsOnPath(int row, int col)
    {
        return _pathCells.Contains(new Vector2I(col, row));
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
    /// Gets a list of grid cell center positions along the level's path.
    /// Enemies follow these waypoints from spawn to base, then one cell past the base.
    /// </summary>
    public List<Vector2> GetPathWaypoints()
    {
        if (_cachedWaypoints != null)
            return _cachedWaypoints;

        _cachedWaypoints = new List<Vector2>();

        if (_level == null || _level.PathCells.Count == 0)
            return _cachedWaypoints;

        foreach (var cell in _level.PathCells)
        {
            _cachedWaypoints.Add(new Vector2(
                GameConstants.CellCenterX(cell.X),
                GameConstants.CellCenterY(cell.Y)
            ));
        }

        // A single-cell path has no previous cell to derive an exit direction from,
        // so skip the past-base waypoint.
        if (_level.PathCells.Count < 2)
            return _cachedWaypoints;

        // Add a final waypoint one cell past the last path cell so enemies walk off-grid.
        var last = _level.PathCells[^1];
        var prev = _level.PathCells[^2];
        int dc = System.Math.Sign(last.X - prev.X);
        int dr = System.Math.Sign(last.Y - prev.Y);

        _cachedWaypoints.Add(new Vector2(
            GameConstants.CellCenterX(last.X) + dc * GameConstants.CellSize,
            GameConstants.CellCenterY(last.Y) + dr * GameConstants.CellSize
        ));

        return _cachedWaypoints;
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
}
