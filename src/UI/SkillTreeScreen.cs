using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GeometryTowerDefense;

/// <summary>
/// Title-screen skill tree. Shows the persistent Skill Point balance, a right-hand
/// tower selector sidebar, and the selected tower's tree — trunk nodes on a vertical
/// spine with seed nodes branching left/right off the bottom trunk. Enabled (stat)
/// nodes show a buy button when unlocked, or render fully greyed (icon, name, and
/// connecting line) when still locked behind a prerequisite; disabled (mechanic)
/// nodes render greyed with a "coming soon" label. Built programmatically to match
/// the rest of the codebase's UI (no .tscn).
/// </summary>
public partial class SkillTreeScreen : Control
{
    [Signal]
    public delegate void BackPressedEventHandler();

    // Tree layout metrics (local to the tree area). The trunk spine (x = 0 in
    // relative coordinates) is centered on the tree area's horizontal middle. Seed
    // content is side-aware — mirrored left/right around the spine — so the whole
    // tree block is symmetric and centers with the spine. BranchOffset is sized so
    // mirrored seed content stays inside the tree area at the default viewport.
    private const float IconHalf = 11f;
    private const float TrunkSpacing = 120f;
    private const float BranchSpacing = 120f;
    private const float BranchOffset = 180f;
    private const float TreeTop = 90f;

    private readonly SkillTree _skillTree;
    private Label? _spLabel;
    private Control? _treeArea;
    private readonly Dictionary<string, (Label RankLabel, Button BuyButton)> _rows = new();
    private readonly Dictionary<string, Vector2> _nodeAnchors = new();
    private readonly Dictionary<TowerType, (Button Button, Label Label)> _sidebarButtons = new();
    private TowerType _selectedTower = TowerType.Arrow;

    public SkillTreeScreen(SkillTree skillTree)
    {
        _skillTree = skillTree;
    }

    public override void _Ready()
    {
        BuildUi();
        Refresh();
    }

    /// <summary>
    /// Re-reads the skill-tree state and updates SP, rank labels, and button states.
    /// Only the currently-visible tower's enabled nodes are registered, so each row
    /// maps 1:1 to a buy button.
    /// </summary>
    public void Refresh()
    {
        if (_spLabel == null)
            return;

        _spLabel.Text = $"Skill Points: {_skillTree.State.SkillPoints}";

        foreach (var (nodeId, row) in _rows)
        {
            int rank = _skillTree.State.GetRank(nodeId);
            row.RankLabel.Text = $"{rank}/{GameConstants.SkillMaxRanks}";
            row.BuyButton.Disabled = !_skillTree.State.CanBuyRank(nodeId);
            row.BuyButton.Text = rank >= GameConstants.SkillMaxRanks
                ? "MAX"
                : $"Buy ({GameConstants.SkillNodeRankCost} SP)";
        }
    }

    private void BuildUi()
    {
        var viewportSize = GetViewportRect().Size;

        // Dark background matching the title screen.
        var bg = new ColorRect();
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Header
        var title = new Label();
        title.Text = "SKILL TREE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Position = new Vector2(0, 20);
        title.Size = new Vector2(viewportSize.X, 50);
        title.AddThemeFontSizeOverride("font_size", 40);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        AddChild(title);

        // Skill Point readout
        _spLabel = new Label();
        _spLabel.Text = "Skill Points: 0";
        _spLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _spLabel.Position = new Vector2(0, 78);
        _spLabel.Size = new Vector2(viewportSize.X, 30);
        _spLabel.AddThemeFontSizeOverride("font_size", 22);
        _spLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        AddChild(_spLabel);

        // Back button
        var backButton = new Button();
        backButton.Text = "Back";
        backButton.Position = new Vector2(20, 18);
        backButton.Size = new Vector2(120, 44);
        backButton.Pressed += () => EmitSignal(SignalName.BackPressed);
        backButton.AddThemeFontSizeOverride("font_size", 18);
        AddChild(backButton);

        // Layout: main tree area on the left/center, narrow tower selector on the right.
        const float margin = 24f;
        const float sidebarWidth = 120f;
        float mainTop = 130f;
        float sidebarX = viewportSize.X - sidebarWidth - margin;
        float mainWidth = sidebarX - margin - margin;
        float mainHeight = viewportSize.Y - mainTop - 40f;

        // Main tree panel.
        var mainPanel = new ColorRect();
        mainPanel.Position = new Vector2(margin - 10f, mainTop - 10f);
        mainPanel.Size = new Vector2(mainWidth + 20f, mainHeight + 20f);
        mainPanel.Color = new Color(0.08f, 0.08f, 0.16f, 0.9f);
        AddChild(mainPanel);

        _treeArea = new Control();
        _treeArea.Position = new Vector2(margin, mainTop);
        _treeArea.Size = new Vector2(mainWidth, mainHeight);
        _treeArea.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_treeArea);

        BuildSidebar(sidebarX, mainTop, sidebarWidth);
        RenderTree();
    }

    /// <summary>
    /// Builds the right-hand sidebar with one toggle button per tower. Each button
    /// carries a small geometric icon matching the tower's in-game shape.
    /// </summary>
    private void BuildSidebar(float x, float top, float width)
    {
        const float buttonHeight = 60f;
        const float buttonGap = 16f;

        var panel = new ColorRect();
        panel.Position = new Vector2(x - 10f, top - 10f);
        panel.Size = new Vector2(width + 20f, 3 * buttonHeight + 2 * buttonGap + 20f);
        panel.Color = new Color(0.08f, 0.08f, 0.16f, 0.9f);
        AddChild(panel);

        var towers = new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser };
        for (int i = 0; i < towers.Length; i++)
        {
            var type = towers[i];
            var color = TowerColor(type);

            var button = new Button();
            button.Size = new Vector2(width, buttonHeight);
            button.Position = new Vector2(x, top + i * (buttonHeight + buttonGap));
            button.Pressed += () => SelectTower(type);

            var icon = CreateTowerIcon(type, color);
            icon.Position = new Vector2(10f, (buttonHeight - 22f) / 2f);
            button.AddChild(icon);

            var label = new Label();
            label.Text = TowerName(type);
            label.Position = new Vector2(40f, (buttonHeight - 24f) / 2f);
            label.Size = new Vector2(width - 60f, 24f);
            label.AddThemeFontSizeOverride("font_size", 14);
            label.MouseFilter = MouseFilterEnum.Ignore;
            button.AddChild(label);

            _sidebarButtons[type] = (button, label);
            AddChild(button);
        }

        UpdateSidebarStyles();
    }

    /// <summary>
    /// Tints the sidebar buttons so the selected tower is highlighted.
    /// </summary>
    private void UpdateSidebarStyles()
    {
        foreach (var (type, entry) in _sidebarButtons)
        {
            bool selected = type == _selectedTower;
            var color = TowerColor(type);
            ApplySidebarStyle(entry.Button, selected, color);
            entry.Label.AddThemeColorOverride(
                "font_color",
                selected ? color : new Color(0.55f, 0.55f, 0.65f));
        }
    }

    private static void ApplySidebarStyle(Button button, bool selected, Color color)
    {
        var normal = new StyleBoxFlat();
        normal.BgColor = selected
            ? new Color(color.R * 0.35f, color.G * 0.35f, color.B * 0.35f)
            : new Color(0.12f, 0.12f, 0.2f);
        normal.BorderColor = selected ? color : new Color(0.3f, 0.3f, 0.4f);
        normal.SetBorderWidthAll(selected ? 2 : 1);
        normal.SetCornerRadiusAll(6);
        button.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(color.R * 0.25f, color.G * 0.25f, color.B * 0.25f);
        hover.BorderColor = color;
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(6);
        button.AddThemeStyleboxOverride("hover", hover);

        button.AddThemeStyleboxOverride("pressed", normal);
        button.AddThemeStyleboxOverride("focus", normal);
    }

    private void SelectTower(TowerType type)
    {
        if (_treeArea == null)
            return;

        _selectedTower = type;
        UpdateSidebarStyles();
        RenderTree();
    }

    /// <summary>
    /// Rebuilds the main area for the currently-selected tower: a header, a line layer
    /// drawing the trunk spine + seed branches, and one node row per catalog entry.
    /// </summary>
    private void RenderTree()
    {
        if (_treeArea == null)
            return;

        ClearTreeArea();

        var color = TowerColor(_selectedTower);
        var nodes = SkillTreeCatalog.ForTower(_selectedTower).ToList();
        ComputeNodeAnchors(nodes, _treeArea.Size.X);

        // Connection lines first so node rows render on top of them.
        var lineLayer = new Control();
        lineLayer.Size = _treeArea.Size;
        lineLayer.MouseFilter = MouseFilterEnum.Ignore;
        lineLayer.Draw += () => DrawTreeLines(lineLayer, nodes, color);
        _treeArea.AddChild(lineLayer);

        // Tower header above the tree.
        var (name, specs) = TowerInfo(_selectedTower);
        var header = new Label();
        header.Text = $"{name}\n{specs}";
        header.HorizontalAlignment = HorizontalAlignment.Center;
        header.Position = new Vector2(0, 10);
        header.Size = new Vector2(_treeArea.Size.X, 44);
        header.AddThemeFontSizeOverride("font_size", 18);
        header.AddThemeColorOverride("font_color", color);
        _treeArea.AddChild(header);

        float spineX = _treeArea.Size.X / 2f;
        foreach (var node in nodes)
        {
            if (_nodeAnchors.TryGetValue(node.Id, out var anchor))
                BuildNodeRow(node, color, anchor, node.Kind == SkillNodeKind.Seed && anchor.X < spineX, _treeArea);
        }
    }

    private void ClearTreeArea()
    {
        if (_treeArea == null)
            return;

        foreach (var child in _treeArea.GetChildren())
        {
            _treeArea.RemoveChild(child);
            child.QueueFree();
        }

        _rows.Clear();
        _nodeAnchors.Clear();
    }

    /// <summary>
    /// Computes each node's icon-center anchor in tree-area-local coordinates and
    /// centers the resulting tree horizontally. Trunk nodes stack along the spine in
    /// catalog order; seed nodes branch off below the bottom trunk, alternating
    /// left/right (a Y shape for the current two-seed towers).
    /// </summary>
    private void ComputeNodeAnchors(IReadOnlyList<SkillNodeDefinition> nodes, float treeWidth)
    {
        _nodeAnchors.Clear();

        var trunks = nodes.Where(n => n.Kind == SkillNodeKind.Trunk).ToList();
        var seeds = nodes.Where(n => n.Kind == SkillNodeKind.Seed).ToList();

        // Relative spine positions, spine center at x = 0.
        float y = TreeTop;
        foreach (var trunk in trunks)
        {
            _nodeAnchors[trunk.Id] = new Vector2(0f, y);
            y += TrunkSpacing;
        }

        float bottomTrunkY = trunks.Count > 0 ? _nodeAnchors[trunks[^1].Id].Y : TreeTop;
        float seedY = bottomTrunkY + BranchSpacing;
        for (int i = 0; i < seeds.Count; i++)
        {
            // Alternate left/right, expanding outward if a tower ever grows more seeds.
            int ring = i / 2 + 1;
            float dx = (i % 2 == 0 ? -1f : 1f) * BranchOffset * ring;
            _nodeAnchors[seeds[i].Id] = new Vector2(dx, seedY);
        }

        // Center the trunk spine (x = 0 in relative coordinates) at the horizontal
        // middle of the tree area. Seed content is mirrored around the spine, so
        // this also centers the whole tree block horizontally.
        float offsetX = treeWidth / 2f;
        foreach (var id in _nodeAnchors.Keys.ToList())
        {
            var anchor = _nodeAnchors[id];
            _nodeAnchors[id] = new Vector2(anchor.X + offsetX, anchor.Y);
        }
    }

    private void DrawTreeLines(Control layer, IReadOnlyList<SkillNodeDefinition> nodes, Color color)
    {
        if (!IsInstanceValid(layer))
            return;

        var trunks = nodes.Where(n => n.Kind == SkillNodeKind.Trunk).ToList();
        var seeds = nodes.Where(n => n.Kind == SkillNodeKind.Seed).ToList();

        // Lines leading into a still-locked node are greyed to match the node's
        // greyed icon; lines into unlocked nodes keep the tower color.
        var lockedColor = new Color(0.3f, 0.3f, 0.35f);

        // Trunk spine: connect consecutive trunk nodes top-to-bottom.
        for (int i = 0; i < trunks.Count - 1; i++)
        {
            var from = _nodeAnchors[trunks[i].Id];
            var to = _nodeAnchors[trunks[i + 1].Id];
            var lineColor = _skillTree.State.IsUnlocked(trunks[i + 1].Id) ? color : lockedColor;
            layer.DrawLine(from, to, lineColor, 3f);
        }

        // Seed branches: each seed connects back to the bottom trunk node.
        if (trunks.Count == 0 || seeds.Count == 0)
            return;

        var bottom = _nodeAnchors[trunks[^1].Id];
        foreach (var seed in seeds)
        {
            var lineColor = _skillTree.State.IsUnlocked(seed.Id) ? color : lockedColor;
            layer.DrawLine(bottom, _nodeAnchors[seed.Id], lineColor, 3f);
        }
    }

    private void BuildNodeRow(SkillNodeDefinition node, Color color, Vector2 anchor, bool contentOnLeft, Control parent)
    {
        bool locked = node.Enabled && !_skillTree.State.IsUnlocked(node.Id);
        bool active = node.Enabled && !locked;

        // Geometric shape icon centered on the tree anchor: circle for trunk, diamond
        // for seed. Locked (not-yet-unlocked) enabled nodes render greyed.
        var icon = CreateShapeIcon(node.Kind, active, color);
        icon.Position = anchor - new Vector2(IconHalf, IconHalf);
        parent.AddChild(icon);

        var nameLabel = new Label();
        nameLabel.Text = node.DisplayName;
        nameLabel.Position = new Vector2(ContentX(anchor.X, 20f, 150f, contentOnLeft), anchor.Y - 10f);
        nameLabel.Size = new Vector2(150f, 20f);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        if (contentOnLeft)
            nameLabel.HorizontalAlignment = HorizontalAlignment.Right;
        nameLabel.AddThemeColorOverride("font_color",
            active
                ? new Color(0.9f, 0.9f, 1.0f)
                : new Color(0.45f, 0.45f, 0.5f));
        parent.AddChild(nameLabel);

        var rankLabel = new Label();
        rankLabel.Text = "0/5";
        rankLabel.Position = new Vector2(ContentX(anchor.X, 262f, 44f, contentOnLeft), anchor.Y - 10f);
        rankLabel.Size = new Vector2(44f, 20f);
        rankLabel.AddThemeFontSizeOverride("font_size", 14);
        if (contentOnLeft)
            rankLabel.HorizontalAlignment = HorizontalAlignment.Right;
        rankLabel.AddThemeColorOverride("font_color",
            active
                ? new Color(0.9f, 0.9f, 1.0f)
                : new Color(0.45f, 0.45f, 0.5f));
        parent.AddChild(rankLabel);

        if (locked)
        {
            // Locked enabled nodes render no text in the buy-button position and are
            // NOT registered in _rows: they cannot be bought until their prerequisite
            // reaches the unlock rank.
        }
        else if (node.Enabled)
        {
            var buyButton = new Button();
            buyButton.Text = $"Buy ({GameConstants.SkillNodeRankCost} SP)";
            buyButton.Position = new Vector2(ContentX(anchor.X, 310f, 124f, contentOnLeft), anchor.Y - 15f);
            buyButton.Size = new Vector2(124f, 30f);
            buyButton.AddThemeFontSizeOverride("font_size", 12);
            buyButton.Pressed += () => OnBuyPressed(node.Id);
            parent.AddChild(buyButton);
            _rows[node.Id] = (rankLabel, buyButton);
        }
        else
        {
            // Disabled mechanic nodes are greyed with a "coming soon" label and are
            // deliberately NOT registered in _rows: they can never change rank.
            var comingSoon = new Label();
            comingSoon.Text = "coming soon";
            comingSoon.Position = new Vector2(ContentX(anchor.X, 310f, 124f, contentOnLeft), anchor.Y - 10f);
            comingSoon.Size = new Vector2(124f, 20f);
            comingSoon.AddThemeFontSizeOverride("font_size", 12);
            if (contentOnLeft)
                comingSoon.HorizontalAlignment = HorizontalAlignment.Right;
            comingSoon.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
            parent.AddChild(comingSoon);
        }
    }

    /// <summary>
    /// Horizontal position of row content that normally sits to the right of the
    /// icon. For seed nodes on the left branch the content is mirrored to the left
    /// of the icon so both branches are symmetric around the trunk spine.
    /// </summary>
    private static float ContentX(float anchorX, float rightOffset, float width, bool onLeft)
        => onLeft ? anchorX - rightOffset - width : anchorX + rightOffset;

    private void OnBuyPressed(string nodeId)
    {
        // A successful purchase can unlock a child node, so rebuild the tree to show
        // its new state (buy button appearing in place of the greyed locked row) and
        // then refresh SP, rank labels, and button states.
        if (_skillTree.BuyRank(nodeId))
        {
            RenderTree();
            Refresh();
        }
    }

    /// <summary>
    /// Small geometric tower icon used in the sidebar, matching each tower's in-game
    /// shape: arrow = triangle, cannon = circle, laser = diamond/emitter.
    /// </summary>
    private static Control CreateTowerIcon(TowerType type, Color color)
    {
        var icon = new Control();
        icon.Size = new Vector2(22f, 22f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon)) return;

            var center = new Vector2(11f, 11f);
            if (type == TowerType.Cannon)
            {
                icon.DrawCircle(center, 8f, color);
                icon.DrawCircle(center, 8f, new Color(0.05f, 0.05f, 0.1f), false, 1.5f);
            }
            else if (type == TowerType.Arrow)
            {
                var triangle = new Vector2[]
                {
                    new Vector2(center.X, 3f),
                    new Vector2(19f, 19f),
                    new Vector2(3f, 19f)
                };
                icon.DrawPolygon(triangle, new[] { color });
            }
            else
            {
                var diamond = new Vector2[]
                {
                    new Vector2(center.X, 3f),
                    new Vector2(19f, center.Y),
                    new Vector2(center.X, 19f),
                    new Vector2(3f, center.Y)
                };
                icon.DrawPolygon(diamond, new[] { color });
            }
        };
        return icon;
    }

    private Control CreateShapeIcon(SkillNodeKind kind, bool enabled, Color color)
    {
        var icon = new Control();
        icon.Size = new Vector2(22f, 22f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon)) return;

            var center = new Vector2(11f, 11f);
            Color fill = enabled ? color : new Color(0.3f, 0.3f, 0.35f);

            if (kind == SkillNodeKind.Trunk)
            {
                icon.DrawCircle(center, 8f, fill);
                icon.DrawCircle(center, 8f, new Color(0.05f, 0.05f, 0.1f), false, 1.5f);
            }
            else
            {
                var diamond = new Vector2[]
                {
                    new Vector2(center.X, 3f),
                    new Vector2(19f, center.Y),
                    new Vector2(center.X, 19f),
                    new Vector2(3f, center.Y)
                };
                icon.DrawPolygon(diamond, new[] { fill });
            }
        };
        return icon;
    }

    private static string TowerName(TowerType type) => type switch
    {
        TowerType.Cannon => "Cannon",
        TowerType.Laser => "Laser",
        _ => "Arrow"
    };

    private static (string Name, string Specializations) TowerInfo(TowerType type) => type switch
    {
        TowerType.Cannon => ("CANNON TOWER", "Bombardier | Concussive"),
        TowerType.Laser => ("LASER TOWER", "Arc/Chain | Melter"),
        _ => ("ARROW TOWER", "Sharpshooter | Barrage")
    };

    private static Color TowerColor(TowerType type) => type switch
    {
        TowerType.Cannon => new Color(0.3f, 0.6f, 0.4f),
        TowerType.Laser => new Color(0.8f, 0.3f, 1.0f),
        _ => new Color(0.2f, 0.5f, 1.0f)
    };
}
