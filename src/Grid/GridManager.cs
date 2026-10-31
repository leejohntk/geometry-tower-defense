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

    // Procedural spawn portals (one per level spawn cell) and the base core marker.
    // These live in presentation-only Controls with MouseFilter = Ignore; they never
    // block placement, pathing, clicks, or targeting.
    private readonly List<Control> _spawnPortals = new();
    private Control? _baseCore;
    private float _markerPhase = 0f;

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

        _spawnPortals.Clear();

        // One procedural portal per route spawn cell.
        foreach (var spawnCell in level.SpawnCells)
            CreateSpawnPortal(spawnCell);

        CreateBaseCore(level.BaseCell);
    }

    /// <summary>
    /// Builds the rotating spawn portal for one spawn cell: a low-alpha fill, two
    /// concentric arcs in a dark red/orange palette, and a small core. The whole
    /// control is rotated/pulsed by <see cref="_Process"/> (transform only), and it is
    /// click-through and background-layered.
    /// </summary>
    private void CreateSpawnPortal(Vector2I spawnCell)
    {
        const float cell = GameConstants.CellSize;
        float radius = cell / 2f;

        var portal = new Control();
        portal.Name = $"SpawnPortal_{spawnCell.X}_{spawnCell.Y}";
        portal.Position = new Vector2(
            spawnCell.X * GameConstants.CellSize,
            spawnCell.Y * GameConstants.CellSize);
        portal.Size = new Vector2(cell, cell);
        portal.PivotOffset = new Vector2(radius, radius);
        portal.MouseFilter = Control.MouseFilterEnum.Ignore;
        // A negative z-index keeps the marker below enemies and towers.
        portal.ZIndex = -1;
        portal.Draw += () =>
        {
            if (!IsInstanceValid(portal)) return;

            var center = new Vector2(radius, radius);
            float outer = cell * GameConstants.SpawnPortalOuterRadiusRatio;
            float inner = cell * GameConstants.SpawnPortalInnerRadiusRatio;
            float core = cell * GameConstants.SpawnPortalCoreRadiusRatio;

            portal.DrawCircle(center, outer, GameConstants.SpawnPortalFillColor);
            portal.DrawCircle(
                center,
                outer,
                GameConstants.SpawnPortalRingColor,
                false,
                2f);

            // Two concentric arcs: rotation makes them sweep, so the marker reads as
            // alive without a per-frame repaint.
            portal.DrawArc(
                center,
                outer * 0.82f,
                -Mathf.Pi / 2f,
                Mathf.Pi / 2f,
                GameConstants.SpawnPortalArcSegments,
                GameConstants.SpawnPortalArcColor,
                2f,
                antialiased: true);
            portal.DrawArc(
                center,
                inner,
                Mathf.Pi / 2f,
                Mathf.Tau,
                GameConstants.SpawnPortalArcSegments,
                GameConstants.SpawnPortalRingColor,
                1.5f,
                antialiased: true);

            portal.DrawCircle(center, core, GameConstants.SpawnPortalCoreColor);
        };

        AddChild(portal);
        _spawnPortals.Add(portal);
    }

    /// <summary>
    /// Builds the base marker: a gold/white hexagonal core inside one shield ring.
    /// Pulsing is a scale transform driven by <see cref="_Process"/>; it is
    /// click-through and background-layered.
    /// </summary>
    private void CreateBaseCore(Vector2I baseCell)
    {
        const float cell = GameConstants.CellSize;
        float radius = cell / 2f;

        // Precompute the hexagon and its closed outline once; the draw closure only
        // consumes them, so a repaint never allocates.
        var center = new Vector2(radius, radius);
        float hexRadius = cell * GameConstants.BaseCoreHexRadiusRatio;
        float shieldRadius = cell * GameConstants.BaseCoreShieldRadiusRatio;
        var hex = new Vector2[6];
        var hexOutline = new Vector2[7];
        for (int i = 0; i < hex.Length; i++)
        {
            float angle = Mathf.Tau * i / hex.Length;
            hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * hexRadius;
            hexOutline[i] = hex[i];
        }
        hexOutline[hex.Length] = hex[0];

        _baseCore = new Control();
        _baseCore.Name = "BaseCore";
        _baseCore.Position = new Vector2(
            baseCell.X * GameConstants.CellSize,
            baseCell.Y * GameConstants.CellSize);
        _baseCore.Size = new Vector2(cell, cell);
        _baseCore.PivotOffset = new Vector2(radius, radius);
        _baseCore.MouseFilter = Control.MouseFilterEnum.Ignore;
        _baseCore.ZIndex = -1;
        _baseCore.Draw += () =>
        {
            if (_baseCore == null || !IsInstanceValid(_baseCore)) return;

            _baseCore.DrawColoredPolygon(hex, GameConstants.BaseCoreFillColor);
            _baseCore.DrawPolyline(
                hexOutline,
                GameConstants.BaseCoreStrokeColor,
                2f,
                antialiased: true);

            // One shield ring.
            _baseCore.DrawCircle(
                center,
                shieldRadius,
                GameConstants.BaseCoreShieldColor,
                false,
                1.5f);
            _baseCore.DrawCircle(center, hexRadius * 0.35f, GameConstants.BaseCoreShieldColor);
        };

        AddChild(_baseCore);
    }

    /// <summary>
    /// Idle marker animation: portal spin and gentle scale pulses, all as transforms.
    /// No marker repaints per frame.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!(delta > 0f))
            return;

        _markerPhase += (float)delta;

        if (_spawnPortals.Count > 0)
        {
            float rotation = _markerPhase * GameConstants.SpawnPortalRotationSpeed;
            float scale = 1f + GameConstants.SpawnPortalPulseAmplitude
                * Mathf.Sin(_markerPhase * GameConstants.SpawnPortalPulseSpeed);
            var pulse = new Vector2(scale, scale);
            for (int i = 0; i < _spawnPortals.Count; i++)
            {
                var portal = _spawnPortals[i];
                if (portal == null || !IsInstanceValid(portal))
                    continue;
                portal.Rotation = rotation;
                portal.Scale = pulse;
            }
        }

        if (_baseCore != null && IsInstanceValid(_baseCore))
        {
            float scale = 1f + GameConstants.BaseCorePulseAmplitude
                * Mathf.Sin(_markerPhase * GameConstants.BaseCorePulseSpeed);
            _baseCore.Scale = new Vector2(scale, scale);
        }
    }

    /// <summary>Read-only view of the spawn portals for tests (one per spawn cell).</summary>
    public IReadOnlyList<Control> SpawnPortals => _spawnPortals;

    /// <summary>The base core marker, or null before _Ready.</summary>
    public Control? BaseCore => _baseCore;

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
    /// True while any part of the placement preview (cell highlight, tower ghost, or
    /// range circle) is visible. Read-only query so callers and tests can observe the
    /// preview without reaching into its private nodes.
    /// </summary>
    public bool IsPlacementPreviewVisible =>
        (_previewHighlight?.Visible ?? false) ||
        (_previewTower?.Visible ?? false) ||
        (_previewTowerCircle?.Visible ?? false) ||
        (_previewTowerLaser?.Visible ?? false) ||
        (_previewRange?.Visible ?? false);

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
