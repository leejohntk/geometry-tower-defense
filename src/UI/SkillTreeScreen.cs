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
///
/// Presentation: a procedural backdrop lattice with a tower-colored spine glow, two-layer
/// branch lines, distinct per-node glyphs with halos/locked/max styling, a row hover
/// highlight, styled Buy buttons, and a fixed bottom info band that describes the hovered
/// skill. All decorations are click-through; rows own the hover signals.
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
    // Node icon box is 46x46 (IconHalf 23) centered exactly on the node anchor; the
    // larger box keeps the same anchor positions and row content columns.
    private const float IconHalf = 23f;
    private const float TrunkSpacing = 120f;
    private const float BranchSpacing = 120f;
    private const float BranchOffset = 180f;
    private const float TreeTop = 90f;

    // Node row metrics. Every row is a named Control wrapper whose rect spans the
    // full row (icon edge to buy-column edge), so the headless layout verifier can
    // resolve rows by identity instead of grouping them by y-coordinate. The row half
    // height (50px tall rows) covers the enlarged icon for hover.
    private const float NameOffset = 31f;
    private const float NameWidth = 150f;
    private const float RankOffset = 262f;
    private const float RankWidth = 44f;
    private const float BuyOffset = 310f;
    private const float BuyWidth = 124f;
    private const float RowHalfHeight = 25f;

    // Info band layout (inside Panel_Main, below the rows).
    private const float InfoNameX = 16f;
    private const float InfoNameY = 6f;
    private const float InfoRankWidth = 110f;
    private const float InfoDescriptionY = 34f;
    private const float InfoInset = 16f;

    private SkillTree _skillTree;
    private bool _verificationModeEnabled;
    private bool _verificationStateActive;
    private Label? _spLabel;
    private Control? _treeArea;
    private Control? _treeBackdrop;
    private Control? _rowHighlight;
    private Label? _infoName;
    private Label? _infoRank;
    private Label? _infoDescription;
    private string? _hoveredNodeId;
    // Track which row / Buy button the real pointer is over so a row's exit can tell
    // "moved onto my own Buy" from "left the row". Synthetic signal emission does not
    // depend on the cursor at all: an exit with no tracked enter always resets.
    private string? _rowHoveredNodeId;
    private string? _buyHoveredNodeId;
    private float _pulseTime = 0f;
    private readonly Dictionary<string, (Label RankLabel, Button BuyButton)> _rows = new();
    // Built-time icon state so the per-frame halo pulse never re-walks the catalog
    // (SkillTreeCatalog lookups allocate); a purchase or tab switch rebuilds the rows.
    private readonly Dictionary<string, (Control Icon, bool Unlocked)> _rowIcons = new();
    private readonly Dictionary<string, Control> _rowWrappers = new();
    private readonly Dictionary<string, Vector2> _nodeAnchors = new();
    private readonly Dictionary<TowerType, (Button Button, Label Label, Control Icon)> _sidebarButtons = new();
    private TowerType _selectedTower = TowerType.Arrow;

    public SkillTreeScreen(SkillTree skillTree)
    {
        _skillTree = skillTree;
    }

    public override void _Ready()
    {
        BuildUi();
        SetProcess(Visible);
    }

    /// <summary>
    /// Re-reads the skill-tree state and updates SP, rank labels, buy-button states,
    /// and Buy styling. Only the currently-visible tower's enabled nodes are
    /// registered, so each row maps 1:1 to a buy button.
    /// </summary>
    public void Refresh()
    {
        if (_spLabel == null)
            return;

        _spLabel.Text = $"Skill Points: {_skillTree.State.SkillPoints}";

        Color accent = TowerColor(_selectedTower);
        foreach (var (nodeId, row) in _rows)
        {
            int rank = _skillTree.State.GetRank(nodeId);
            bool maxed = rank >= GameConstants.SkillMaxRanks;
            row.RankLabel.Text = $"{rank}/{GameConstants.SkillMaxRanks}";
            row.BuyButton.Disabled = !_skillTree.State.CanBuyRank(nodeId);
            row.BuyButton.Text = maxed
                ? "MAX"
                : $"Buy ({GameConstants.SkillNodeRankCost} SP)";
            GeometricButtonStyle.ApplyBuy(row.BuyButton, accent, maxed);
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

    /// <summary>The node id whose row is currently hovered, or null in the idle state.</summary>
    public string? HoveredNodeId => _hoveredNodeId;

    /// <summary>The current info-band name text (empty in the idle state).</summary>
    public string InfoBandName => _infoName?.Text ?? string.Empty;

    /// <summary>The current info-band rank text (empty in the idle state).</summary>
    public string InfoBandRank => _infoRank?.Text ?? string.Empty;

    /// <summary>The current info-band description text (the idle hint when nothing is hovered).</summary>
    public string InfoBandDescription => _infoDescription?.Text ?? string.Empty;

    /// <summary>
    /// Shows a node's name, live rank, and description in the bottom info band, and
    /// highlights its row. Locked rows also append their prerequisite hint.
    /// </summary>
    private void ShowSkillInfo(string nodeId)
    {
        var node = SkillTreeCatalog.Find(nodeId);
        if (node == null || _infoName == null || _infoRank == null || _infoDescription == null)
            return;

        // Restore the previous icon's brightness when hover moves directly between rows
        // (locked icons are not touched by the pulse loop, so they would otherwise stay
        // bright after being unhovered).
        if (_hoveredNodeId != null && _hoveredNodeId != nodeId)
            ApplyHoverBrightness(_hoveredNodeId, false);

        _hoveredNodeId = nodeId;

        int rank = _skillTree.State.GetRank(nodeId);
        _infoName.Text = node.DisplayName;
        _infoRank.Text = $"{rank}/{GameConstants.SkillMaxRanks}";

        string text = node.Description;
        if (!_skillTree.State.IsUnlocked(nodeId))
        {
            string? unlockHint = SkillTreeCatalog.UnlockHint(nodeId);
            if (unlockHint != null)
                text = $"{text} {unlockHint}";
        }
        _infoDescription.Text = text;

        if (_rowHighlight != null && IsInstanceValid(_rowHighlight))
        {
            if (_rowWrappers.TryGetValue(nodeId, out var row) &&
                row != null && IsInstanceValid(row))
            {
                _rowHighlight.Position = row.Position + new Vector2(-4f, -3f);
                _rowHighlight.Size = row.Size + new Vector2(8f, 6f);
                _rowHighlight.Visible = true;
                _rowHighlight.QueueRedraw();
            }
            else
            {
                _rowHighlight.Visible = false;
            }
        }

        ApplyHoverBrightness(nodeId, true);
    }

    /// <summary>
    /// Clears the info band back to its idle hint and hides the row highlight.
    /// </summary>
    private void ResetSkillInfo()
    {
        _hoveredNodeId = null;
        _rowHoveredNodeId = null;
        _buyHoveredNodeId = null;

        if (_infoName != null)
            _infoName.Text = string.Empty;
        if (_infoRank != null)
            _infoRank.Text = string.Empty;
        if (_infoDescription != null)
            _infoDescription.Text = GameConstants.SkillInfoIdleHint;

        if (_rowHighlight != null && IsInstanceValid(_rowHighlight))
            _rowHighlight.Visible = false;

        foreach (var (nodeId, entry) in _rowIcons)
            ApplyHoverBrightness(nodeId, false);
    }

    /// <summary>
    /// Row MouseExited. Resets immediately unless the pointer is still tracked over this
    /// row's own Buy button (moving row -> Buy must not clear the band). No real cursor
    /// position is consulted, so a synthetic MouseExited always resets deterministically.
    /// </summary>
    private void OnRowMouseExited(string nodeId)
    {
        if (_hoveredNodeId != nodeId)
            return;

        if (_buyHoveredNodeId == nodeId)
            return;

        ResetSkillInfo();
    }

    /// <summary>
    /// Buy-button MouseExited. Mirrors the row rule: if the pointer moved back onto the
    /// row wrapper, keep the band; otherwise reset.
    /// </summary>
    private void OnBuyMouseExited(string nodeId)
    {
        if (_hoveredNodeId != nodeId)
            return;

        if (_rowHoveredNodeId == nodeId)
            return;

        ResetSkillInfo();
    }

    private void ApplyHoverBrightness(string nodeId, bool hovered)
    {
        if (!_rowIcons.TryGetValue(nodeId, out var entry) ||
            entry.Icon == null || !IsInstanceValid(entry.Icon))
        {
            return;
        }

        float brightness = hovered ? GameConstants.SkillTreeHoverBrightness : 1f;
        entry.Icon.Modulate = new Color(brightness, brightness, brightness, 1f);
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

        // Styled counter badge behind the SP readout label.
        var spBadge = new Control();
        spBadge.Name = "SpBadge";
        spBadge.Position = new Vector2(viewportSize.X / 2f - 150f, 56f);
        spBadge.Size = new Vector2(300f, 34f);
        spBadge.MouseFilter = MouseFilterEnum.Ignore;
        spBadge.Draw += () => DrawSpBadge(spBadge);
        titleBlock.AddChild(spBadge);

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
        GeometricButtonStyle.Apply(backButton, new Color(0.55f, 0.7f, 1f));
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

        // Backdrop lattice behind the tree area. It spans the panel, is click-through,
        // and repaints only on tower switches (static otherwise).
        _treeBackdrop = new Control();
        _treeBackdrop.Name = "Block_TreeBackdrop";
        _treeBackdrop.Position = Vector2.Zero;
        _treeBackdrop.Size = mainPanel.Size;
        _treeBackdrop.MouseFilter = MouseFilterEnum.Ignore;
        _treeBackdrop.Draw += () => DrawTreeBackdrop(_treeBackdrop);
        mainPanel.AddChild(_treeBackdrop);

        _treeArea = new Control();
        _treeArea.Name = "TreeArea";
        _treeArea.Position = new Vector2(margin, mainTop);
        _treeArea.Size = new Vector2(mainWidth, mainHeight);
        _treeArea.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_treeArea);

        BuildInfoBand(mainPanel, mainHeight, mainWidth);
        BuildSidebar(sidebarX, mainTop, sidebarWidth);
        RenderTree();
    }

    /// <summary>
    /// Builds the fixed info band at the bottom of Panel_Main: a framed background with
    /// name, rank, and description labels. Every node is click-through and nothing in it
    /// overlaps a tree row.
    /// </summary>
    private void BuildInfoBand(Control mainPanel, float mainHeight, float mainWidth)
    {
        float bandHeight = GameConstants.SkillInfoBandHeight;

        var band = new Control();
        band.Name = "Block_SkillInfo";
        band.Position = new Vector2(
            10f,
            10f + mainHeight - bandHeight - GameConstants.SkillInfoBandBottomMargin);
        band.Size = new Vector2(mainWidth, bandHeight);
        band.MouseFilter = MouseFilterEnum.Ignore;
        mainPanel.AddChild(band);

        var panel = new Control();
        panel.Name = "Panel";
        panel.Position = Vector2.Zero;
        panel.Size = band.Size;
        panel.MouseFilter = MouseFilterEnum.Ignore;
        panel.Draw += () => DrawInfoPanel(panel);
        band.AddChild(panel);

        _infoName = new Label();
        _infoName.Name = "Name";
        _infoName.Text = string.Empty;
        _infoName.Position = new Vector2(InfoNameX, InfoNameY);
        _infoName.Size = new Vector2(mainWidth - InfoRankWidth - InfoInset * 2f, 24f);
        _infoName.AddThemeFontSizeOverride("font_size", 17);
        _infoName.AddThemeColorOverride("font_color", GameConstants.SkillInfoNameColor);
        _infoName.MouseFilter = MouseFilterEnum.Ignore;
        band.AddChild(_infoName);

        _infoRank = new Label();
        _infoRank.Name = "Rank";
        _infoRank.Text = string.Empty;
        _infoRank.HorizontalAlignment = HorizontalAlignment.Right;
        _infoRank.Position = new Vector2(mainWidth - InfoRankWidth - InfoInset, InfoNameY);
        _infoRank.Size = new Vector2(InfoRankWidth, 24f);
        _infoRank.AddThemeFontSizeOverride("font_size", 16);
        _infoRank.AddThemeColorOverride("font_color", GameConstants.SkillInfoRankColor);
        _infoRank.MouseFilter = MouseFilterEnum.Ignore;
        band.AddChild(_infoRank);

        _infoDescription = new Label();
        _infoDescription.Name = "Description";
        _infoDescription.Text = GameConstants.SkillInfoIdleHint;
        _infoDescription.Position = new Vector2(InfoNameX, InfoDescriptionY);
        _infoDescription.Size = new Vector2(mainWidth - InfoInset * 2f, bandHeight - InfoDescriptionY - 8f);
        _infoDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _infoDescription.AddThemeFontSizeOverride("font_size", 13);
        _infoDescription.AddThemeColorOverride("font_color", GameConstants.SkillInfoDescriptionColor);
        _infoDescription.MouseFilter = MouseFilterEnum.Ignore;
        band.AddChild(_infoDescription);
    }

    private static void DrawInfoPanel(Control panel)
    {
        if (!IsInstanceValid(panel))
            return;

        var rect = new Rect2(Vector2.Zero, panel.Size);
        panel.DrawRect(rect, GameConstants.SkillInfoPanelColor);
        panel.DrawRect(
            rect,
            GameConstants.SkillInfoPanelBorderColor,
            filled: false,
            width: 1.5f);

        // Accent bar on the left edge so the band reads as part of the tree design.
        panel.DrawRect(
            new Rect2(0f, 0f, 4f, panel.Size.Y),
            GameConstants.SkillInfoRankColor with { A = 0.75f });
    }

    private void DrawSpBadge(Control badge)
    {
        if (!IsInstanceValid(badge))
            return;

        var rect = new Rect2(Vector2.Zero, badge.Size);
        badge.DrawRect(rect, new Color(0.07f, 0.07f, 0.15f, 0.9f));
        badge.DrawRect(rect, GameConstants.SkillTreeMaxColor with { A = 0.55f }, filled: false, width: 1.5f);

        float half = badge.Size.Y / 2f;
        OrnamentDrawing.DrawDiamond(badge, new Vector2(14f, half), 6f, GameConstants.SkillTreeMaxColor);
        OrnamentDrawing.DrawDiamond(badge, new Vector2(badge.Size.X - 14f, half), 6f, GameConstants.SkillTreeMaxColor with { A = 0.7f });
    }

    private void DrawTreeBackdrop(Control backdrop)
    {
        if (!IsInstanceValid(backdrop))
            return;

        Vector2 size = backdrop.Size;
        if (size.X <= 0f || size.Y <= 0f)
            return;

        // Faint lattice of dots.
        float spacing = GameConstants.SkillTreeBackdropDotSpacing;
        for (float y = spacing; y < size.Y; y += spacing)
        {
            for (float x = spacing; x < size.X; x += spacing)
                backdrop.DrawCircle(new Vector2(x, y), 1.2f, GameConstants.SkillTreeBackdropDotColor);
        }

        // Soft tower-colored glow along the trunk spine (tree area is inset 10 px).
        if (_treeArea == null || !IsInstanceValid(_treeArea))
            return;

        float spineX = 10f + _treeArea.Size.X / 2f;
        float width = GameConstants.SkillTreeBackdropSpineGlowWidth;
        Color color = TowerColor(_selectedTower);
        for (int i = 3; i >= 1; i--)
        {
            float bandWidth = width / i;
            backdrop.DrawRect(
                new Rect2(spineX - bandWidth / 2f, 0f, bandWidth, size.Y),
                color with { A = 0.035f * i });
        }
    }

    /// <summary>
    /// Builds the right-hand sidebar with one toggle button per tower. Each button
    /// carries a geometric glyph matching the tower's in-game shape; the selected
    /// button glows and unselected ones are dimmed.
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

            _sidebarButtons[type] = (button, label, icon);
            sidebarBlock.AddChild(button);
        }

        UpdateSidebarStyles();
    }

    /// <summary>
    /// Tints the sidebar buttons so the selected tower is highlighted and the others are
    /// dimmed. The icon repaints only on selection change (its halo reads the live
    /// selection), never per frame.
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
            entry.Icon.Modulate = selected
                ? Colors.White
                : new Color(0.55f, 0.55f, 0.62f, 1f);
            entry.Icon.QueueRedraw();
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
    /// drawing the trunk spine + seed branches, a hover-highlight decoration, and one
    /// node row per catalog entry. Ends by resetting the info band and repopulating rank
    /// labels and buy-button state, so every rebuild (initial load, tower switch,
    /// purchase) is self-consistent and no caller has to remember a follow-up refresh.
    /// </summary>
    private void RenderTree()
    {
        if (_treeArea == null)
            return;

        ClearTreeArea();

        var color = TowerColor(_selectedTower);
        var nodes = SkillTreeCatalog.ForTower(_selectedTower).ToList();
        ComputeNodeAnchors(nodes, _treeArea.Size.X);

        // Backdrop glow color follows the selected tower; repaint only on rebuild.
        _treeBackdrop?.QueueRedraw();

        // Connection lines first so node rows render on top of them.
        var lineLayer = new Control();
        lineLayer.Name = "Lines";
        lineLayer.Size = _treeArea.Size;
        lineLayer.MouseFilter = MouseFilterEnum.Ignore;
        lineLayer.Draw += () => DrawTreeLines(lineLayer, nodes, color);
        _treeArea.AddChild(lineLayer);

        // Row hover highlight: a single decoration band behind the rows (never a row
        // child, so the row's Icon/Name/Rank/Buy contract stays exact).
        _rowHighlight = new Control();
        _rowHighlight.Name = "RowHighlight";
        _rowHighlight.Position = Vector2.Zero;
        _rowHighlight.Size = Vector2.Zero;
        _rowHighlight.Visible = false;
        _rowHighlight.MouseFilter = MouseFilterEnum.Ignore;
        _rowHighlight.Draw += () => DrawRowHighlight(_rowHighlight);
        _treeArea.AddChild(_rowHighlight);

        // Tower header above the tree, wrapped as a named visual group.
        var (name, specs) = TowerInfo(_selectedTower);
        var headerBlock = new Control();
        headerBlock.Name = "Block_TreeHeader";
        headerBlock.Position = new Vector2(0, 10);
        headerBlock.Size = new Vector2(_treeArea.Size.X, 44);
        headerBlock.MouseFilter = MouseFilterEnum.Ignore;
        _treeArea.AddChild(headerBlock);

        var selectedType = _selectedTower;
        var emblem = new Control();
        emblem.Name = "Emblem";
        emblem.Position = new Vector2(10f, 10f);
        emblem.Size = new Vector2(24f, 24f);
        emblem.MouseFilter = MouseFilterEnum.Ignore;
        emblem.Draw += () =>
        {
            if (!IsInstanceValid(emblem)) return;
            TowerGlyphs.Draw(emblem, selectedType, new Vector2(12f, 12f), 10f, color);
        };
        headerBlock.AddChild(emblem);

        // The Title label keeps its centered alignment and two-line text format, but its
        // rect is narrowed and re-centered so the flanking emblem never overlaps it.
        const float headerLabelWidth = 600f;
        var header = new Label();
        header.Name = "Title";
        header.Text = $"{name}\n{specs}";
        header.HorizontalAlignment = HorizontalAlignment.Center;
        header.Position = new Vector2((_treeArea.Size.X - headerLabelWidth) / 2f, 0f);
        header.Size = new Vector2(headerLabelWidth, 44);
        header.AddThemeFontSizeOverride("font_size", 18);
        header.AddThemeColorOverride("font_color", color);
        headerBlock.AddChild(header);

        float spineX = _treeArea.Size.X / 2f;
        foreach (var node in nodes)
        {
            if (_nodeAnchors.TryGetValue(node.Id, out var anchor))
                BuildNodeRow(node, color, anchor, node.Kind == SkillNodeKind.Seed && anchor.X < spineX, _treeArea);
        }

        // A rebuild invalidates every old row reference: the band reverts to idle and
        // the newly built rows hover correctly through their own signals.
        ResetSkillInfo();
        Refresh();
    }

    private void DrawRowHighlight(Control highlight)
    {
        if (!IsInstanceValid(highlight) || highlight.Size.X <= 0f)
            return;

        var rect = new Rect2(Vector2.Zero, highlight.Size);
        highlight.DrawRect(
            rect,
            new Color(
                GameConstants.SkillInfoNameColor.R * 0.4f,
                GameConstants.SkillInfoNameColor.G * 0.5f,
                GameConstants.SkillInfoNameColor.B * 0.8f,
                GameConstants.SkillTreeHoverBandAlpha));
        highlight.DrawRect(
            new Rect2(0f, 0f, 2f, highlight.Size.Y),
            GameConstants.SkillInfoNameColor with { A = 0.45f });
        highlight.DrawRect(
            rect,
            GameConstants.SkillInfoPanelBorderColor with { A = 0.35f },
            filled: false,
            width: 1f);
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
        _rowIcons.Clear();
        _rowWrappers.Clear();
        _rowHighlight = null;
        _hoveredNodeId = null;
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
        var lockedColor = GameConstants.SkillTreeLockedColor;

        // Trunk spine: connect consecutive trunk nodes top-to-bottom. The shared
        // endpoints keep the spine visually continuous through both layers.
        for (int i = 0; i < trunks.Count - 1; i++)
        {
            var from = _nodeAnchors[trunks[i].Id];
            var to = _nodeAnchors[trunks[i + 1].Id];
            var lineColor = _skillTree.State.IsUnlocked(trunks[i + 1].Id) ? color : lockedColor;
            DrawGlowLine(layer, from, to, lineColor);
        }

        // Seed branches: each seed connects back to the bottom trunk node.
        if (trunks.Count == 0 || seeds.Count == 0)
            return;

        var bottom = _nodeAnchors[trunks[^1].Id];
        foreach (var seed in seeds)
        {
            var lineColor = _skillTree.State.IsUnlocked(seed.Id) ? color : lockedColor;
            DrawGlowLine(layer, bottom, _nodeAnchors[seed.Id], lineColor);
        }
    }

    /// <summary>
    /// Two-layer branch line: a wide translucent glow underlay and a thin bright core.
    /// Glow alpha is constant (the tower color's own alpha is preserved for the core).
    /// </summary>
    private static void DrawGlowLine(Control layer, Vector2 from, Vector2 to, Color color)
    {
        layer.DrawLine(
            from,
            to,
            color with { A = GameConstants.SkillTreeLineGlowAlpha },
            GameConstants.SkillTreeLineGlowWidth,
            antialiased: true);
        layer.DrawLine(
            from,
            to,
            color,
            GameConstants.SkillTreeLineCoreWidth,
            antialiased: true);
    }

    private void BuildNodeRow(SkillNodeDefinition node, Color color, Vector2 anchor, bool contentOnLeft, Control parent)
    {
        bool locked = node.Enabled && !_skillTree.State.IsUnlocked(node.Id);
        bool active = node.Enabled && !locked;
        int rank = _skillTree.State.GetRank(node.Id);
        bool maxed = rank >= GameConstants.SkillMaxRanks;

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
        // Pass (not Ignore) so the real MouseEntered/MouseExited signals fire; the row
        // has no click behavior of its own and never swallows its Buy button.
        row.MouseFilter = MouseFilterEnum.Pass;
        parent.AddChild(row);

        var labelColor = active
            ? new Color(0.9f, 0.9f, 1.0f)
            : new Color(0.45f, 0.45f, 0.5f);

        // Layered node icon: halo ring, distinct glyph, locked slash, max accent. The
        // 22x22 Icon control and its anchor are unchanged (the verifier measures them).
        var icon = CreateNodeIcon(node, locked, maxed, color);
        icon.Name = "Icon";
        icon.Position = anchor - new Vector2(IconHalf, IconHalf) - rowOrigin;
        row.AddChild(icon);
        _rowIcons[node.Id] = (icon, !locked);

        var nameLabel = new Label();
        nameLabel.Name = "Name";
        nameLabel.Text = node.DisplayName;
        nameLabel.Position = new Vector2(ContentX(anchor.X, NameOffset, NameWidth, contentOnLeft), anchor.Y - 10f) - rowOrigin;
        nameLabel.Size = new Vector2(NameWidth, 20f);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        if (contentOnLeft)
            nameLabel.HorizontalAlignment = HorizontalAlignment.Right;
        nameLabel.AddThemeColorOverride("font_color", labelColor);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
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
        rankLabel.MouseFilter = MouseFilterEnum.Ignore;
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
            // Styling is applied by Refresh(), which runs at the end of every rebuild
            // (and on open); styling here too would duplicate the work per row.
            buyButton.MouseEntered += () =>
            {
                _buyHoveredNodeId = node.Id;
                ShowSkillInfo(node.Id);
            };
            buyButton.MouseExited += () =>
            {
                if (_buyHoveredNodeId == node.Id)
                    _buyHoveredNodeId = null;
                OnBuyMouseExited(node.Id);
            };
            row.AddChild(buyButton);
            _rows[node.Id] = (rankLabel, buyButton);
        }

        // Hover surface: the row wrapper owns the signals; the icon (Ignore) is inside
        // the wrapper so its centre is part of the hit area without stealing clicks.
        row.MouseEntered += () =>
        {
            _rowHoveredNodeId = node.Id;
            ShowSkillInfo(node.Id);
        };
        row.MouseExited += () =>
        {
            if (_rowHoveredNodeId == node.Id)
                _rowHoveredNodeId = null;
            OnRowMouseExited(node.Id);
        };
        _rowWrappers[node.Id] = row;
    }

    /// <summary>
    /// Node icon at the fixed 46x46 size: an outer halo ring (tower color unlocked, grey
    /// locked, gold at rank 5), a distinct glyph (stat glyph for trunk nodes, the shared
    /// archetype emblem for seeds), a dim glyph plus slash/lock mark when locked, and a
    /// gold accent when maxed. The locked/maxed marks are sized as ratios of the icon
    /// half-extent so they scale with the box.
    /// </summary>
    private static Control CreateNodeIcon(
        SkillNodeDefinition node,
        bool locked,
        bool maxed,
        Color color)
    {
        var icon = new Control();
        icon.Size = new Vector2(IconHalf * 2f, IconHalf * 2f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon))
                return;

            var center = new Vector2(IconHalf, IconHalf);
            Color halo;
            if (locked)
                halo = GameConstants.SkillTreeLockedColor;
            else if (maxed)
                halo = GameConstants.SkillTreeMaxColor;
            else
                halo = color;

            icon.DrawCircle(
                center,
                GameConstants.SkillTreeHaloRadius,
                halo with { A = locked ? 0.55f : 0.9f },
                false,
                GameConstants.SkillTreeHaloWidth);

            if (maxed)
            {
                icon.DrawCircle(
                    center,
                    GameConstants.SkillTreeHaloRadius + 1.5f,
                    GameConstants.SkillTreeMaxColor with { A = 0.45f },
                    false,
                    1f);
            }

            Color glyphColor = locked ? GameConstants.SkillTreeLockedColor : color;
            SkillGlyphs.Draw(icon, node.Id, center, GameConstants.SkillTreeGlyphRadius, glyphColor);

            // Marks are expressed as ratios of the icon half-extent, so the slash, lock,
            // and max dot always scale with the 46x46 box (no old-pixel-size marks).
            float half = IconHalf;

            if (locked)
            {
                var slash = new Color(0.12f, 0.12f, 0.16f, 0.9f);
                float slashExtent = half * 0.55f;
                icon.DrawLine(
                    center - new Vector2(slashExtent, slashExtent),
                    center + new Vector2(slashExtent, slashExtent),
                    slash,
                    half * 0.14f);
                icon.DrawRect(
                    new Rect2(
                        center.X + half * 0.09f,
                        center.Y + half * 0.18f,
                        half * 0.55f,
                        half * 0.36f),
                    slash);
                icon.DrawArc(
                    new Vector2(center.X + half * 0.36f, center.Y + half * 0.18f),
                    half * 0.2f,
                    Mathf.Pi,
                    Mathf.Tau,
                    8,
                    slash,
                    half * 0.09f);
            }
            else if (maxed)
            {
                icon.DrawCircle(
                    center + new Vector2(half * 0.59f, -half * 0.59f),
                    half * 0.18f,
                    GameConstants.SkillTreeMaxColor);
            }
        };
        return icon;
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
        // its new state (buy button appearing in place of the greyed locked row);
        // RenderTree also refreshes SP, rank labels, and button states.
        if (_skillTree.BuyRank(nodeId))
            RenderTree();
    }

    /// <summary>
    /// Small geometric tower icon used in the sidebar, matching each tower's in-game
    /// shape: arrow = triangle, cannon = circle with barrel, laser = diamond/emitter.
    /// A selected tower gains a soft glow halo (repainted on selection change only).
    /// </summary>
    private Control CreateTowerIcon(TowerType type, Color color)
    {
        var icon = new Control();
        icon.Size = new Vector2(22f, 22f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon)) return;

            var center = new Vector2(11f, 11f);
            if (type == _selectedTower)
            {
                icon.DrawCircle(center, 10f, color with { A = 0.3f });
                icon.DrawCircle(center, 10f, color, false, 1.5f);
            }
            TowerGlyphs.Draw(icon, type, center, 7.5f, color);
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

    private static Color TowerColor(TowerType type) => TowerGlyphs.ColorFor(type);

    /// <summary>
    /// Stops the halo-pulse processing entirely while the screen is hidden (matching
    /// TitleScreen), and resumes it when the screen is shown again.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
            SetProcess(Visible);
    }

    /// <summary>
    /// Gentle per-node halo pulse via Modulate only (no redraws), and only while the
    /// screen is visible. A hovered icon stays bright; locked icons never pulse.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!Visible || !(delta > 0f))
            return;

        _pulseTime += (float)delta;
        float pulse = 1f + GameConstants.SkillTreeIdlePulseAmplitude
            * Mathf.Sin(_pulseTime * GameConstants.SkillTreeIdlePulseSpeed);

        foreach (var (nodeId, entry) in _rowIcons)
        {
            if (entry.Icon == null || !IsInstanceValid(entry.Icon))
                continue;

            // Locked icons and the currently hovered icon are constant: locked ones use
            // their drawn grey, hovered ones are owned by ShowSkillInfo's brightness.
            if (!entry.Unlocked || nodeId == _hoveredNodeId)
                continue;

            entry.Icon.Modulate = new Color(pulse, pulse, pulse, 1f);
        }
    }
}
