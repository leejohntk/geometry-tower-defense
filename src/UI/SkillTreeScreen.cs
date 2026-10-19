using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GeometryTowerDefense;

/// <summary>
/// Title-screen skill tree. Shows the persistent Skill Point balance, a right-hand
/// tower selector sidebar, and the selected tower's tree — trunk nodes on a vertical
/// spine with seed nodes branching left/right off the bottom trunk. All 15 nodes are
/// enabled as of Part 2: nodes show a buy button when unlocked, or render fully greyed
/// (icon, name, and connecting line) when still locked behind a prerequisite. Built
/// programmatically to match the rest of the codebase's UI (no .tscn).
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

    // Node row metrics. Every row is a named Control wrapper whose rect spans the
    // full row (icon edge to buy-column edge), so the headless layout verifier can
    // resolve rows by identity instead of grouping them by y-coordinate.
    private const float NameOffset = 20f;
    private const float NameWidth = 150f;
    private const float RankOffset = 262f;
    private const float RankWidth = 44f;
    private const float BuyOffset = 310f;
    private const float BuyWidth = 124f;
    private const float RowHalfHeight = 15f;

    private SkillTree _skillTree;
    private bool _verificationModeEnabled;
    private bool _verificationStateActive;
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

    /// <summary>
    /// Test/verification gate used by the headless layout verifier. Verification mode
    /// is only available in a debug build and must be requested explicitly; gameplay
    /// never calls it. Returns false and changes nothing otherwise, so normal play can
    /// never leave the screen with a non-persisting tree.
    /// </summary>
    public bool EnableVerificationMode()
    {
        if (!OS.IsDebugBuild())
            return false;

        _verificationModeEnabled = true;
        return true;
    }

    /// <summary>
    /// True only after <see cref="ResetProgressForVerification"/> has installed the
    /// isolated in-memory tree. The verifier refuses to grant SP or press Buy unless
    /// this is true.
    /// </summary>
    public bool IsVerificationStateActive() => _verificationStateActive;

    /// <summary>
    /// Test/verification hook used by the headless layout verifier. Resets the
    /// in-memory progression to an empty state and rebuilds the screen so locked and
    /// unlocked rows can be measured deterministically. Requires
    /// <see cref="EnableVerificationMode"/> (debug build only); returns false and
    /// changes nothing otherwise. Nothing is flushed, so the player's save file is
    /// never touched.
    /// </summary>
    public bool ResetProgressForVerification()
    {
        if (!_verificationModeEnabled)
            return false;

        // Replace the persistent tree with an isolated in-memory one whose save
        // delegate is a no-op. A verification purchase can then drive the real
        // purchase/rebuild path without ever writing user://skilltree.cfg.
        _skillTree = new SkillTree(new SkillTreeState(), _ => true);
        _verificationStateActive = true;
        RenderTree();
        Refresh();
        return true;
    }

    /// <summary>
    /// Test/verification hook: grants SP on the isolated verification tree. Refuses
    /// (returns false, mutates nothing) unless verification state is active, so it can
    /// never award SP on the player's real tree.
    /// </summary>
    public bool GrantSkillPointsForVerification(int amount)
    {
        if (!_verificationStateActive)
            return false;

        _skillTree.State.AddSkillPoints(amount);
        Refresh();
        return true;
    }

    private void BuildUi()
    {
        var viewportSize = GetViewportRect().Size;

        // Dark background matching the title screen.
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Header block: title + SP readout. Wrapped as one named visual group so the
        // layout verifier can resolve it without relying on generated node names.
        var titleBlock = new Control();
        titleBlock.Name = "Block_Title";
        titleBlock.Position = new Vector2(0, 20);
        titleBlock.Size = new Vector2(viewportSize.X, 89);
        titleBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(titleBlock);

        var title = new Label();
        title.Name = "Title";
        title.Text = "SKILL TREE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Position = Vector2.Zero;
        title.Size = new Vector2(viewportSize.X, 50);
        title.AddThemeFontSizeOverride("font_size", 40);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        titleBlock.AddChild(title);

        // Skill Point readout
        _spLabel = new Label();
        _spLabel.Name = "SkillPoints";
        _spLabel.Text = "Skill Points: 0";
        _spLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _spLabel.Position = new Vector2(0, 58);
        _spLabel.Size = new Vector2(viewportSize.X, 30);
        _spLabel.AddThemeFontSizeOverride("font_size", 22);
        _spLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        titleBlock.AddChild(_spLabel);

        // Back button
        var backButton = new Button();
        backButton.Name = "Back";
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
        mainPanel.Name = "Panel_Main";
        mainPanel.Position = new Vector2(margin - 10f, mainTop - 10f);
        mainPanel.Size = new Vector2(mainWidth + 20f, mainHeight + 20f);
        mainPanel.Color = new Color(0.08f, 0.08f, 0.16f, 0.9f);
        AddChild(mainPanel);

        _treeArea = new Control();
        _treeArea.Name = "TreeArea";
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

        // Sidebar group: panel + tower buttons. The wrapper's origin is the panel's
        // top-left; children keep their global positions because their local offsets
        // are expressed relative to that origin.
        var sidebarBlock = new Control();
        sidebarBlock.Name = "Block_Sidebar";
        sidebarBlock.Position = new Vector2(x - 10f, top - 10f);
        sidebarBlock.Size = new Vector2(width + 20f, 3 * buttonHeight + 2 * buttonGap + 20f);
        sidebarBlock.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(sidebarBlock);

        var panel = new ColorRect();
        panel.Name = "Panel";
        panel.Position = Vector2.Zero;
        panel.Size = sidebarBlock.Size;
        panel.Color = new Color(0.08f, 0.08f, 0.16f, 0.9f);
        sidebarBlock.AddChild(panel);

        var towers = new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser };
        for (int i = 0; i < towers.Length; i++)
        {
            var type = towers[i];
            var color = TowerColor(type);

            var button = new Button();
            button.Name = $"Sidebar_{TowerKey(type)}";
            button.Size = new Vector2(width, buttonHeight);
            button.Position = new Vector2(10f, 10f + i * (buttonHeight + buttonGap));
            button.Pressed += () => SelectTower(type);

            var icon = CreateTowerIcon(type, color);
            icon.Name = "Icon";
            icon.Position = new Vector2(10f, (buttonHeight - 22f) / 2f);
            button.AddChild(icon);

            var label = new Label();
            label.Name = "Label";
            label.Text = TowerName(type);
            label.Position = new Vector2(40f, (buttonHeight - 24f) / 2f);
            label.Size = new Vector2(width - 60f, 24f);
            label.AddThemeFontSizeOverride("font_size", 14);
            label.MouseFilter = MouseFilterEnum.Ignore;
            button.AddChild(label);

            _sidebarButtons[type] = (button, label);
            sidebarBlock.AddChild(button);
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
        lineLayer.Name = "Lines";
        lineLayer.Size = _treeArea.Size;
        lineLayer.MouseFilter = MouseFilterEnum.Ignore;
        lineLayer.Draw += () => DrawTreeLines(lineLayer, nodes, color);
        _treeArea.AddChild(lineLayer);

        // Tower header above the tree, wrapped as a named visual group.
        var (name, specs) = TowerInfo(_selectedTower);
        var headerBlock = new Control();
        headerBlock.Name = "Block_TreeHeader";
        headerBlock.Position = new Vector2(0, 10);
        headerBlock.Size = new Vector2(_treeArea.Size.X, 44);
        headerBlock.MouseFilter = MouseFilterEnum.Ignore;
        _treeArea.AddChild(headerBlock);

        var header = new Label();
        header.Name = "Title";
        header.Text = $"{name}\n{specs}";
        header.HorizontalAlignment = HorizontalAlignment.Center;
        header.Position = Vector2.Zero;
        header.Size = new Vector2(_treeArea.Size.X, 44);
        header.AddThemeFontSizeOverride("font_size", 18);
        header.AddThemeColorOverride("font_color", color);
        headerBlock.AddChild(header);

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

        // Row wrapper: fixed span from the icon edge on the branch side to the buy
        // column edge on the content side. Its origin is the row's top-left, and every
        // child is placed relative to it, so no child's global position changes.
        float contentLeft = ContentX(anchor.X, BuyOffset, BuyWidth, true);
        float contentRight = ContentX(anchor.X, BuyOffset, BuyWidth, false) + BuyWidth;
        float rowLeft = contentOnLeft ? contentLeft : anchor.X - IconHalf;
        float rowRight = contentOnLeft ? anchor.X + IconHalf : contentRight;
        var rowOrigin = new Vector2(rowLeft, anchor.Y - RowHalfHeight);

        var row = new Control();
        row.Name = RowNodeName(node.Id);
        row.Position = rowOrigin;
        row.Size = new Vector2(rowRight - rowLeft, RowHalfHeight * 2f);
        row.MouseFilter = MouseFilterEnum.Ignore;
        parent.AddChild(row);

        var labelColor = active
            ? new Color(0.9f, 0.9f, 1.0f)
            : new Color(0.45f, 0.45f, 0.5f);

        // Geometric shape icon centered on the tree anchor: circle for trunk, diamond
        // for seed. Locked (not-yet-unlocked) enabled nodes render greyed.
        var icon = CreateShapeIcon(node.Kind, active, color);
        icon.Name = "Icon";
        icon.Position = anchor - new Vector2(IconHalf, IconHalf) - rowOrigin;
        row.AddChild(icon);

        var nameLabel = new Label();
        nameLabel.Name = "Name";
        nameLabel.Text = node.DisplayName;
        nameLabel.Position = new Vector2(ContentX(anchor.X, NameOffset, NameWidth, contentOnLeft), anchor.Y - 10f) - rowOrigin;
        nameLabel.Size = new Vector2(NameWidth, 20f);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        if (contentOnLeft)
            nameLabel.HorizontalAlignment = HorizontalAlignment.Right;
        nameLabel.AddThemeColorOverride("font_color", labelColor);
        row.AddChild(nameLabel);

        var rankLabel = new Label();
        rankLabel.Name = "Rank";
        rankLabel.Text = "0/5";
        rankLabel.Position = new Vector2(ContentX(anchor.X, RankOffset, RankWidth, contentOnLeft), anchor.Y - 10f) - rowOrigin;
        rankLabel.Size = new Vector2(RankWidth, 20f);
        rankLabel.AddThemeFontSizeOverride("font_size", 14);
        if (contentOnLeft)
            rankLabel.HorizontalAlignment = HorizontalAlignment.Right;
        rankLabel.AddThemeColorOverride("font_color", labelColor);
        row.AddChild(rankLabel);

        // Only active (enabled + unlocked) nodes get a buy button and are registered
        // in _rows. Locked nodes stay greyed with no button; a future disabled node
        // is neither active nor locked, so it also renders no button.
        if (active)
        {
            var buyButton = new Button();
            buyButton.Name = "Buy";
            buyButton.Text = $"Buy ({GameConstants.SkillNodeRankCost} SP)";
            buyButton.Position = new Vector2(ContentX(anchor.X, BuyOffset, BuyWidth, contentOnLeft), anchor.Y - RowHalfHeight) - rowOrigin;
            buyButton.Size = new Vector2(BuyWidth, RowHalfHeight * 2f);
            buyButton.AddThemeFontSizeOverride("font_size", 12);
            buyButton.Pressed += () => OnBuyPressed(node.Id);
            row.AddChild(buyButton);
            _rows[node.Id] = (rankLabel, buyButton);
        }
    }

    /// <summary>
    /// Node id to wrapper name. Dots are not legal in Godot node names, so the id is
    /// flattened (arrow.pierce -> Row_arrow_pierce) to keep names stable and resolvable.
    /// </summary>
    private static string RowNodeName(string nodeId) => "Row_" + nodeId.Replace('.', '_');

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

    private static string TowerKey(TowerType type) => type switch
    {
        TowerType.Cannon => "cannon",
        TowerType.Laser => "laser",
        _ => "arrow"
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
