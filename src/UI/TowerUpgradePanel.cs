using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// In-level tower upgrade overlay, docked to the selected tower's cell. Shows the
/// tower's current rank as filled chevrons, offers the next rank (chevrons + coin
/// cost) while its hidden XP gate is reached, and at rank 2 replaces the generic
/// upgrade with the tower's two archetype cards. All marks are procedural geometry.
///
/// The panel is a UI overlay: its root consumes mouse input (MouseFilter = Stop) and
/// every child group is a named Control with MouseFilter = Ignore, so clicking an
/// upgrade action cannot also deselect the tower, toggle its range, or place a tower.
/// It repaints only when its visible state changes (rank, archetype, coins, or the
/// live XP gate), never every frame.
/// </summary>
public partial class TowerUpgradePanel : Control
{
    [Signal]
    public delegate void UpgradeRequestedEventHandler();

    [Signal]
    public delegate void ArchetypeChosenEventHandler(int archetype);

    private Tower? _tower;
    private int _coins;
    private int _anchorRow;
    private int _anchorCol;
    private bool _uiBuilt;
    private bool _hasState;
    private (int Rank, int Archetype, bool Affordable, bool Upgrade, bool Choice) _lastState;

    private ColorRect? _background;
    private Control? _rankBlock;
    private Label? _nameLabel;
    private Control? _upgradeBlock;
    private Button? _buyButton;
    private Control? _buyChevronsRoot;
    private Label? _buyCostLabel;
    private Control? _archetypeBlock;
    private Button? _leftCard;
    private Button? _rightCard;
    private Control? _leftIcon;
    private Control? _rightIcon;
    private Label? _leftName;
    private Label? _rightName;
    private Label? _leftCost;
    private Label? _rightCost;

    private TowerArchetype _leftArchetype = TowerArchetype.None;
    private TowerArchetype _rightArchetype = TowerArchetype.None;

    private Color _buyChevronColor = Colors.White;

    private readonly List<Control> _buyChevrons = new();

    /// <summary>
    /// The tower currently displayed, or null when the panel is closed.
    /// </summary>
    public Tower? DisplayedTower => _tower;

    /// <summary>
    /// True while the panel is visible for a live tower.
    /// </summary>
    public bool IsOpen => Visible && _tower != null && IsInstanceValid(_tower);

    public TowerUpgradePanel()
    {
        Name = "Panel_TowerUpgrade";
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public override void _Ready()
    {
        EnsureUi();
        // Normally built hidden; keep processing on if a caller already opened it.
        SetProcess(IsOpen);
    }

    /// <summary>
    /// Opens the panel for a tower and positions it against that tower's cell.
    /// </summary>
    public void ShowFor(Tower tower, int coins)
    {
        EnsureUi();

        _tower = tower;
        _coins = coins;
        _anchorRow = tower.GridRow;
        _anchorCol = tower.GridCol;
        _hasState = false;

        Visible = true;
        SetProcess(true);
        Refresh();
    }

    /// <summary>
    /// Updates the coin balance used for affordability. Refreshes only when the
    /// displayed state actually changes.
    /// </summary>
    public void UpdateCoins(int coins)
    {
        if (_coins == coins)
            return;

        _coins = coins;
        if (IsOpen && ComputeState() != _lastState)
            Refresh();
    }

    /// <summary>
    /// Rebuilds all labels, chevrons, cards, and the docked position from the current
    /// tower/coin state. Safe to call after an upgrade transaction.
    /// </summary>
    public void Refresh()
    {
        EnsureUi();

        var tower = _tower;
        if (tower == null || !IsInstanceValid(tower))
        {
            Close();
            return;
        }

        Color color = tower.TowerColor;

        // One state read drives every block and the layout, so the gate can never be
        // evaluated differently between the blocks and the recorded signature.
        var state = ComputeState();

        UpdateRankBlock(tower, color);
        UpdateUpgradeBlock(tower, color, state.Upgrade);
        UpdateArchetypeBlock(tower, color, state.Choice);
        LayoutPanel(state.Upgrade, state.Choice);

        _lastState = state;
        _hasState = true;
    }

    /// <summary>
    /// Closes the panel and forgets the tower. Used on deselection, placement mode,
    /// Escape, result screens, and level reset.
    /// </summary>
    public void Close()
    {
        _tower = null;
        _hasState = false;
        Visible = false;
        SetProcess(false);
    }

    /// <summary>
    /// True when a viewport-space point lands inside the visible panel. Main checks
    /// this before running its own battlefield click handling, because _Input fires
    /// before GUI consumption.
    /// </summary>
    public bool ContainsPoint(Vector2 point) => IsOpen && GetGlobalRect().HasPoint(point);

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        if (_tower == null || !IsInstanceValid(_tower))
        {
            Close();
            return;
        }

        // The XP gate is live: crossing a threshold while the panel is open reveals
        // the next option without a reselect. Repaint only on an actual state change.
        if (!_hasState || ComputeState() != _lastState)
            Refresh();
    }

    private (int Rank, int Archetype, bool Affordable, bool Upgrade, bool Choice) ComputeState()
    {
        var tower = _tower;
        if (tower == null || !IsInstanceValid(tower))
            return (0, 0, false, false, false);

        bool maxRank = TowerUpgrades.IsMaxRank(tower.Rank);
        bool gateOpen = TowerUpgrades.IsUpgradeVisible(tower.Rank, tower.Xp);

        // Only affordability matters to the rendered state, so a kill that raises the
        // coin balance without crossing a cost never triggers a repaint.
        int targetRank = tower.Rank + 1;
        bool affordable = _coins >= TowerUpgrades.CostToReachRank(targetRank);

        return (
            tower.Rank,
            (int)tower.Archetype,
            affordable,
            !maxRank && tower.Rank < GameConstants.TowerMaxRank - 1 && gateOpen,
            tower.Rank == GameConstants.TowerMaxRank - 1 && gateOpen);
    }

    // === Build ===

    private void EnsureUi()
    {
        if (_uiBuilt)
            return;

        _uiBuilt = true;

        _background = new ColorRect();
        _background.Name = "Background";
        _background.Color = GameConstants.TowerUpgradePanelBackgroundColor;
        _background.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_background);

        var separator = new ColorRect();
        separator.Name = "Separator";
        separator.Color = GameConstants.TowerUpgradePanelBorderColor;
        separator.MouseFilter = MouseFilterEnum.Ignore;
        separator.Position = new Vector2(
            GameConstants.TowerUpgradePanelPadding,
            GameConstants.TowerUpgradePanelPadding + GameConstants.TowerUpgradePanelRankHeight + 1f);
        separator.Size = new Vector2(
            GameConstants.TowerUpgradePanelWidth - 2f * GameConstants.TowerUpgradePanelPadding,
            1f);
        AddChild(separator);

        BuildRankBlock();
        BuildUpgradeBlock();
        BuildArchetypeBlock();
    }

    private static Control CreateBlock(string name)
    {
        var block = new Control();
        block.Name = name;
        block.MouseFilter = MouseFilterEnum.Ignore;
        return block;
    }

    private Control CreateChevronStack(string name, List<Control> chevrons, System.Action<Control> draw)
    {
        var stack = new Control();
        stack.Name = name;
        stack.MouseFilter = MouseFilterEnum.Ignore;
        // One shared fixed-size box for 1, 2, and 3 chevrons. Children share x and are
        // repositioned into a centred vertical column whenever the rank changes.
        stack.Size = new Vector2(GameConstants.RankIconWidth, GameConstants.RankIconHeight);

        for (int i = 0; i < GameConstants.TowerMaxRank; i++)
        {
            var chevron = new Control();
            chevron.Name = $"Chevron_{i + 1}";
            chevron.Size = new Vector2(GameConstants.RankIconWidth, GameConstants.RankChevronHeight);
            chevron.Position = Vector2.Zero;
            chevron.Visible = false;
            chevron.MouseFilter = MouseFilterEnum.Ignore;
            chevron.Draw += () => draw(chevron);
            stack.AddChild(chevron);
            chevrons.Add(chevron);
        }

        return stack;
    }

    private void BuildRankBlock()
    {
        float pad = GameConstants.TowerUpgradePanelPadding;
        float width = GameConstants.TowerUpgradePanelWidth - 2f * pad;
        float height = GameConstants.TowerUpgradePanelRankHeight;

        _rankBlock = CreateBlock("Block_Rank");
        _rankBlock.Position = new Vector2(pad, pad);
        _rankBlock.Size = new Vector2(width, height);
        AddChild(_rankBlock);

        // The header is the tower (or archetype) name only. The placed tower's
        // top-right RankBadge is the single current-rank display; the panel never
        // duplicates it with its own chevrons or emblem.
        _nameLabel = new Label();
        _nameLabel.Name = "Name";
        _nameLabel.Position = Vector2.Zero;
        _nameLabel.Size = new Vector2(width, height);
        _nameLabel.VerticalAlignment = VerticalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 1f));
        _nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        _rankBlock.AddChild(_nameLabel);
    }

    private void BuildUpgradeBlock()
    {
        float pad = GameConstants.TowerUpgradePanelPadding;
        float width = GameConstants.TowerUpgradePanelWidth - 2f * pad;
        float height = GameConstants.TowerUpgradePanelUpgradeHeight;

        _upgradeBlock = CreateBlock("Block_Upgrade");
        _upgradeBlock.Position = new Vector2(
            pad,
            pad + GameConstants.TowerUpgradePanelRankHeight + GameConstants.TowerUpgradePanelBlockGap);
        _upgradeBlock.Size = new Vector2(width, height);
        AddChild(_upgradeBlock);

        _buyButton = new Button();
        _buyButton.Name = "Buy";
        _buyButton.Position = Vector2.Zero;
        _buyButton.Size = new Vector2(width, height);
        _buyButton.Text = string.Empty;
        _buyButton.FocusMode = FocusModeEnum.None;
        MakeButtonTextInvisible(_buyButton);
        _buyButton.Pressed += OnBuyPressed;
        _upgradeBlock.AddChild(_buyButton);

        // The chevrons and cost are siblings drawn above the button (the button's own
        // background paints first). Ignore mouse so the click lands on the button, and
        // keep them addressable directly under Block_Upgrade.
        _buyChevronsRoot = CreateChevronStack("Chevrons", _buyChevrons, DrawBuyChevron);
        _buyChevronsRoot.Position = new Vector2(
            10f,
            (height - GameConstants.RankIconHeight) / 2f);
        _upgradeBlock.AddChild(_buyChevronsRoot);

        _buyCostLabel = new Label();
        _buyCostLabel.Name = "Cost";
        _buyCostLabel.Position = new Vector2(width - 80f, 0f);
        _buyCostLabel.Size = new Vector2(70f, height);
        _buyCostLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _buyCostLabel.VerticalAlignment = VerticalAlignment.Center;
        _buyCostLabel.AddThemeFontSizeOverride("font_size", 14);
        _buyCostLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        _buyCostLabel.MouseFilter = MouseFilterEnum.Ignore;
        _upgradeBlock.AddChild(_buyCostLabel);
    }

    private void BuildArchetypeBlock()
    {
        float pad = GameConstants.TowerUpgradePanelPadding;
        float width = GameConstants.TowerUpgradePanelWidth - 2f * pad;
        float height = GameConstants.TowerUpgradePanelArchetypeHeight;

        _archetypeBlock = CreateBlock("Block_ArchetypeChoice");
        _archetypeBlock.Position = new Vector2(
            pad,
            pad + GameConstants.TowerUpgradePanelRankHeight + GameConstants.TowerUpgradePanelBlockGap);
        _archetypeBlock.Size = new Vector2(width, height);
        AddChild(_archetypeBlock);

        const float cardGap = 6f;
        float cardWidth = (width - cardGap) / 2f;

        var left = CreateCard(cardWidth, 0f, "Card_Left");
        _leftCard = left.Card;
        _leftIcon = left.Icon;
        _leftName = left.Name;
        _leftCost = left.Cost;
        _leftCard.Pressed += () => OnCardPressed(_leftArchetype);
        left.Icon.Draw += DrawLeftIcon;

        var right = CreateCard(cardWidth, cardWidth + cardGap, "Card_Right");
        _rightCard = right.Card;
        _rightIcon = right.Icon;
        _rightName = right.Name;
        _rightCost = right.Cost;
        _rightCard.Pressed += () => OnCardPressed(_rightArchetype);
        right.Icon.Draw += DrawRightIcon;
    }

    private (Button Card, Control Icon, Label Name, Label Cost) CreateCard(float width, float x, string nodeName)
    {
        float height = GameConstants.TowerUpgradePanelArchetypeHeight;
        float iconSize = GameConstants.TowerUpgradePanelCardIconSize;

        var card = new Button();
        card.Name = nodeName;
        card.Position = new Vector2(x, 0f);
        card.Size = new Vector2(width, height);
        card.Text = string.Empty;
        card.FocusMode = FocusModeEnum.None;
        // The visible copy lives in the named child labels; the button keeps the same
        // text as a programmatic label (screen readers / layout verification) but draws
        // it transparently so it cannot duplicate the icon + name + cost stack.
        MakeButtonTextInvisible(card);
        _archetypeBlock!.AddChild(card);

        var icon = new Control();
        icon.Name = "Icon";
        icon.Size = new Vector2(iconSize, iconSize);
        icon.Position = new Vector2((width - iconSize) / 2f, 6f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        card.AddChild(icon);

        var name = new Label();
        name.Name = "Name";
        name.Position = new Vector2(0f, 6f + iconSize + 2f);
        name.Size = new Vector2(width, 18f);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 13);
        name.MouseFilter = MouseFilterEnum.Ignore;
        card.AddChild(name);

        var cost = new Label();
        cost.Name = "Cost";
        cost.Position = new Vector2(0f, 6f + iconSize + 22f);
        cost.Size = new Vector2(width, 16f);
        cost.HorizontalAlignment = HorizontalAlignment.Center;
        cost.AddThemeFontSizeOverride("font_size", 12);
        cost.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        cost.MouseFilter = MouseFilterEnum.Ignore;
        card.AddChild(cost);

        return (card, icon, name, cost);
    }

    // === Refresh ===

    private void UpdateRankBlock(Tower tower, Color color)
    {
        if (_nameLabel == null)
            return;

        bool maxRank = TowerUpgrades.IsMaxRank(tower.Rank);
        _nameLabel.Text = maxRank
            ? TowerArchetypes.DisplayName(tower.Archetype)
            : tower.Type.ToString();
        _nameLabel.AddThemeColorOverride("font_color", maxRank ? color : new Color(0.9f, 0.9f, 1f));
    }

    private void UpdateUpgradeBlock(Tower tower, Color color, bool upgradeVisible)
    {
        if (_upgradeBlock == null || _buyButton == null || _buyCostLabel == null || _buyChevronsRoot == null)
            return;

        _upgradeBlock.Visible = upgradeVisible;
        _buyButton.Visible = upgradeVisible;
        if (!upgradeVisible)
        {
            // No generic upgrade is offered: make sure no stale chevron can read as a
            // (forbidden) three-chevron rank-3 purchase even though the block is hidden.
            RankIcon.LayoutStack(
                _buyChevrons,
                0,
                GameConstants.RankIconHeight,
                GameConstants.RankChevronHeight,
                GameConstants.RankChevronVerticalGap);
            return;
        }

        int targetRank = tower.Rank + 1;
        int cost = TowerUpgrades.CostToReachRank(targetRank);
        bool canAfford = _coins >= cost;

        _buyCostLabel.Text = $"{cost}$";
        _buyButton.Disabled = !canAfford;
        _buyButton.Text = $"{cost}$";
        ApplyButtonStyle(_buyButton, color, canAfford);

        _buyChevronColor = canAfford ? color : GameConstants.TowerUpgradePanelDimColor;
        RankIcon.LayoutStack(
            _buyChevrons,
            targetRank,
            GameConstants.RankIconHeight,
            GameConstants.RankChevronHeight,
            GameConstants.RankChevronVerticalGap);
    }

    private void UpdateArchetypeBlock(Tower tower, Color color, bool choiceVisible)
    {
        var leftCard = _leftCard;
        var rightCard = _rightCard;
        if (_archetypeBlock == null || leftCard == null || rightCard == null)
            return;

        _archetypeBlock.Visible = choiceVisible;
        leftCard.Visible = choiceVisible;
        rightCard.Visible = choiceVisible;
        if (!choiceVisible)
            return;

        var options = TowerArchetypes.OptionsFor(tower.Type);
        _leftArchetype = options[0];
        _rightArchetype = options[1];

        bool canAfford = _coins >= GameConstants.TowerUpgradeCostRank3;
        UpdateCard(leftCard, _leftIcon, _leftName, _leftCost, _leftArchetype, color, canAfford);
        UpdateCard(rightCard, _rightIcon, _rightName, _rightCost, _rightArchetype, color, canAfford);
    }

    private void UpdateCard(
        Button card,
        Control? icon,
        Label? nameLabel,
        Label? costLabel,
        TowerArchetype archetype,
        Color color,
        bool canAfford)
    {
        card.Name = $"Card_{TowerArchetypes.NodeKey(archetype)}";
        card.Disabled = !canAfford;
        card.Text = TowerArchetypes.DisplayName(archetype);
        ApplyButtonStyle(card, color, canAfford);

        if (nameLabel != null)
        {
            nameLabel.Text = TowerArchetypes.DisplayName(archetype);
            nameLabel.AddThemeColorOverride(
                "font_color", canAfford ? new Color(0.92f, 0.92f, 1f) : GameConstants.TowerUpgradePanelDimColor);
        }

        if (costLabel != null)
            costLabel.Text = $"{GameConstants.TowerUpgradeCostRank3}$";

        icon?.QueueRedraw();
    }

    private void LayoutPanel(bool upgradeVisible, bool choiceVisible)
    {
        float pad = GameConstants.TowerUpgradePanelPadding;
        float height = pad + GameConstants.TowerUpgradePanelRankHeight + pad;

        if (upgradeVisible)
            height += GameConstants.TowerUpgradePanelBlockGap + GameConstants.TowerUpgradePanelUpgradeHeight;
        else if (choiceVisible)
            height += GameConstants.TowerUpgradePanelBlockGap + GameConstants.TowerUpgradePanelArchetypeHeight;

        Size = new Vector2(GameConstants.TowerUpgradePanelWidth, height);

        if (_background != null)
            _background.Size = Size;

        // Dock to the selected tower's cell: horizontally centred and above the cell,
        // except in row 0 where it opens below so it can never sit under the top bar.
        // Clamp inside the battlefield so it never leaves the play area.
        float centerX = GameConstants.CellCenterX(_anchorCol);
        float x = Mathf.Clamp(
            centerX - Size.X / 2f,
            0f,
            GameConstants.PlayAreaWidth - Size.X);

        float cellTop = _anchorRow * GameConstants.CellSize;
        float y = _anchorRow == 0
            ? cellTop + GameConstants.CellSize + GameConstants.TowerUpgradePanelCellGap
            : cellTop - GameConstants.TowerUpgradePanelCellGap - Size.Y;

        y = Mathf.Clamp(y, GameConstants.TopBarHeight, GameConstants.PlayAreaHeight - Size.Y);

        Position = new Vector2(x, y);
    }

    // === Drawing ===

    private void DrawBuyChevron(Control chevron)
    {
        if (!IsInstanceValid(chevron))
            return;
        RankChevron.Draw(
            chevron,
            new Rect2(Vector2.Zero, chevron.Size),
            _buyChevronColor,
            GameConstants.RankChevronStroke);
    }

    private void DrawLeftIcon()
    {
        if (_leftIcon == null || !IsInstanceValid(_leftIcon))
            return;

        DrawEmblem(_leftIcon, _leftArchetype);
    }

    private void DrawRightIcon()
    {
        if (_rightIcon == null || !IsInstanceValid(_rightIcon))
            return;

        DrawEmblem(_rightIcon, _rightArchetype);
    }

    private void DrawEmblem(Control icon, TowerArchetype archetype)
    {
        if (_tower == null || !IsInstanceValid(_tower) || archetype == TowerArchetype.None)
            return;

        Vector2 center = icon.Size / 2f;
        ArchetypeEmblem.Draw(icon, archetype, center, center.X * 0.85f, _tower.TowerColor);
    }

    // === Actions ===

    private void OnBuyPressed()
    {
        if (_tower == null)
            return;

        EmitSignal(SignalName.UpgradeRequested);
    }

    private void OnCardPressed(TowerArchetype archetype)
    {
        if (archetype == TowerArchetype.None)
            return;

        EmitSignal(SignalName.ArchetypeChosen, (int)archetype);
    }

    /// <summary>
    /// Keeps a button's Text for programmatic inspection (layout verification, screen
    /// readers) while drawing it transparently — the visible copy is the named child
    /// labels stacked over the button.
    /// </summary>
    private static void MakeButtonTextInvisible(Button button)
    {
        var transparent = new Color(0f, 0f, 0f, 0f);
        button.ClipText = true;
        button.AddThemeFontSizeOverride("font_size", 10);
        button.AddThemeColorOverride("font_color", transparent);
        button.AddThemeColorOverride("font_hover_color", transparent);
        button.AddThemeColorOverride("font_pressed_color", transparent);
        button.AddThemeColorOverride("font_hover_pressed_color", transparent);
        button.AddThemeColorOverride("font_disabled_color", transparent);
        button.AddThemeColorOverride("font_focus_color", transparent);
    }

    private static void ApplyButtonStyle(Button button, Color color, bool enabled)
    {
        var normal = new StyleBoxFlat();
        normal.BgColor = enabled
            ? new Color(color.R * 0.22f, color.G * 0.22f, color.B * 0.22f, 0.95f)
            : new Color(0.1f, 0.1f, 0.16f, 0.95f);
        normal.BorderColor = enabled ? color : GameConstants.TowerUpgradePanelDimColor;
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(4);
        button.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = enabled
            ? new Color(color.R * 0.32f, color.G * 0.32f, color.B * 0.32f, 1f)
            : new Color(0.12f, 0.12f, 0.18f, 1f);
        hover.BorderColor = enabled ? color : GameConstants.TowerUpgradePanelDimColor;
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(4);
        button.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor = new Color(color.R * 0.4f, color.G * 0.4f, color.B * 0.4f, 1f);
        pressed.BorderColor = color;
        pressed.SetBorderWidthAll(1);
        pressed.SetCornerRadiusAll(4);
        button.AddThemeStyleboxOverride("pressed", pressed);

        button.AddThemeStyleboxOverride("disabled", normal);
        button.AddThemeStyleboxOverride("focus", normal);
    }
}
