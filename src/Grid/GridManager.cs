using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Manages the 20x14 grid: cell occupancy, path detection, coordinate conversion.
/// The game coordinate system uses column (x) and row (y) where (0,0) is top-left.
/// The paths are defined per-level as a list of routes (each a list of cells), not a single row.
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
    private Control? _previewTowerLaser;
    private Control? _previewRange;
    private float _previewRangePixels = GameConstants.CellDistanceInPixels(GameConstants.ArrowTowerRange);

    // Cached path waypoints per route (immutable, computed once per level)
    private List<List<Vector2>>? _cachedWaypoints;

    /// <summary>
    /// Configure this grid for a specific level's routes. Must be called before AddChild.
    /// </summary>
    public void Configure(LevelDefinition level)
    {
        _level = level;

        _pathCells.Clear();
        foreach (var route in level.Paths)
            foreach (var cell in route)
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

        // Ghost tower for Laser (semi-transparent magenta/purple circle)
        _previewTowerLaser = new Control();
        _previewTowerLaser.Size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
        _previewTowerLaser.MouseFilter = Control.MouseFilterEnum.Ignore;
        _previewTowerLaser.Visible = false;
        _previewTowerLaser.ZIndex = previewZ + 1;
        _previewTowerLaser.Draw += () =>
        {
            if (!IsInstanceValid(_previewTowerLaser)) return;

            float radius = GameConstants.CellSize / 2f - 4;
            Vector2 center = new Vector2(GameConstants.CellSize / 2f, GameConstants.CellSize / 2f);
            _previewTowerLaser.DrawCircle(center, radius, new Color(0.8f, 0.3f, 1.0f, 0.45f));
            _previewTowerLaser.DrawCircle(center, radius, new Color(0.4f, 0.1f, 0.6f, 0.6f), false, 2.0f);
        };
        AddChild(_previewTowerLaser);

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
        // Configure(level) runs before _Ready, so _level is always set here.
        var level = _level!;

        // One spawn indicator per route spawn cell.
        foreach (var spawnCell in level.SpawnCells)
            CreateSpawnIndicator(spawnCell);

        Vector2I baseCell = level.BaseCell;

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

    private void CreateSpawnIndicator(Vector2I spawnCell)
    {
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
    }

    // === Placement Preview ===

    /// <summary>
    /// Show the placement preview at the given grid position for the given tower type.
    /// Green highlight + tower ghost + range circle for valid cells,
    /// red highlight (no tower) for invalid cells.
    /// <paramref name="rangeCells"/> is the tower's final range (base + skill ranks),
    /// computed by the caller so GridManager stays unaware of the skill system.
    /// </summary>
    public void ShowPlacementPreview(int row, int col, bool canPlace, TowerType towerType, float rangeCells)
    {
        if (_previewHighlight == null || _previewTower == null || _previewTowerCircle == null ||
            _previewTowerLaser == null || _previewRange == null)
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

        _previewTowerLaser.Position = cellPos;
        _previewTowerLaser.Visible = canPlace && towerType == TowerType.Laser;
        if (canPlace && towerType == TowerType.Laser)
            _previewTowerLaser.QueueRedraw();

        // Range indicator — only on valid spots. Recompute the cached radius every
        // call so a tower-type (or skill-rank) change is always reflected.
        float rangePx = GameConstants.CellDistanceInPixels(rangeCells);
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
        if (_previewTowerLaser != null)
            _previewTowerLaser.Visible = false;
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
    /// Gets a list of grid cell center positions along one route of the level's paths.
    /// Enemies follow these waypoints from spawn to base, then one cell past the base.
    /// The returned list is shared (read-only) across all enemies on the same route.
    /// </summary>
    public List<Vector2> GetPathWaypoints(int routeIndex)
    {
        if (_cachedWaypoints == null)
            _cachedWaypoints = BuildAllWaypoints();

        if (routeIndex < 0 || routeIndex >= _cachedWaypoints.Count)
            return new List<Vector2>();

        return _cachedWaypoints[routeIndex];
    }

    /// <summary>
    /// Builds the waypoint list for every route, cached once per level configuration.
    /// </summary>
    private List<List<Vector2>> BuildAllWaypoints()
    {
        var allWaypoints = new List<List<Vector2>>();

        if (_level == null)
            return allWaypoints;

        foreach (var route in _level.Paths)
        {
            var waypoints = new List<Vector2>();

            foreach (var cell in route)
            {
                waypoints.Add(new Vector2(
                    GameConstants.CellCenterX(cell.X),
                    GameConstants.CellCenterY(cell.Y)
                ));
            }

            // A single-cell route has no previous cell to derive an exit direction from,
            // so skip the past-base waypoint.
            if (route.Count >= 2)
            {
                var last = route[^1];
                var prev = route[^2];
                int dc = System.Math.Sign(last.X - prev.X);
                int dr = System.Math.Sign(last.Y - prev.Y);

                waypoints.Add(new Vector2(
                    GameConstants.CellCenterX(last.X) + dc * GameConstants.CellSize,
                    GameConstants.CellCenterY(last.Y) + dr * GameConstants.CellSize
                ));
            }

            allWaypoints.Add(waypoints);
        }

        return allWaypoints;
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
