using Godot;
using System;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// In-game HUD showing HP, coins, wave counter, tower placement buttons, and Start Wave button.
/// Top bar for status info, right sidebar for action buttons.
/// </summary>
public partial class GameHUD : CanvasLayer
{
    [Signal]
    public delegate void StartWavePressedEventHandler();

    [Signal]
    public delegate void PlaceTowerPressedEventHandler(int towerType);

    private Label? _hpLabel;
    private Label? _coinsLabel;
    private Label? _waveLabel;
    private Button? _startWaveButton;
    private Button? _placeArrowButton;
    private Button? _placeCannonButton;
    private Button? _placeLaserButton;
    private SpeedupCue? _speedupCue;
    private GameManager? _gameManager;
    private SkillTreeState? _skillState;
    private bool _allowCannon = false;
    private bool _allowLaser = false;
    private int _totalWaves = 0;

    // Placement cards: the Button stays at its historical path and keeps emitting the
    // same signal; the Icon/Name/Cost children carry the card content. Dimming is
    // applied to the children so a disabled card reads as unavailable while its labels
    // stay legible. The tower type is kept with the card so every cost surface can be
    // recomputed from the effective (discounted) price.
    private readonly List<(Button Button, TowerType Type, Control Icon, Label Name, Label Cost)> _placementCards = new();

    /// <summary>
    /// Persistent skill-tree state used to resolve effective tower costs. Null means
    /// base prices. Setting it refreshes the visible card labels and enabled states, so
    /// a card never shows a base price the player is not actually charged.
    /// </summary>
    public SkillTreeState? SkillState
    {
        get => _skillState;
        set
        {
            _skillState = value;
            RefreshCostLabels();
            UpdateTowerButtonStates();
        }
    }

    // Top-bar stat layout (local to Block_TopBar). A 28x28 icon, a small uppercase
    // caption, and the value sit side by side in one 128-wide group; groups start at
    // x=16 with a 12px gap, all inside the 40px bar. The caption/value heights are
    // centred from their measured minimum sizes so no line is clipped.
    private const float TopBarStatStartX = 16f;
    private const float TopBarStatGroupGap = 12f;
    private const float TopBarStatGroupWidth = 128f;
    private const float TopBarStatIconSize = 28f;
    private const float TopBarStatTextX = 34f;
    private const float TopBarStatCaptionWidth = 50f;
    private const float TopBarStatValueX = 88f;
    private const float TopBarStatValueWidth = 40f;
    private const float TopBarStatCaptionHeight = 23f;
    private const float TopBarStatValueHeight = 26f;
    private const int TopBarCaptionFontSize = 10;
    private const int TopBarValueFontSize = 18;
    private const float TopBarAccentHeight = 2f;
    private const float TopBarPatternSpacing = 18f;

    public override void _Ready()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;

        // Top bar group — spans the full viewport width, at top. The wrapper sits at
        // the origin so its children keep absolute positions. It is a visual-only
        // overlay: every descendant is click-through.
        var topBarBlock = new Control();
        topBarBlock.Name = "Block_TopBar";
        topBarBlock.Position = Vector2.Zero;
        topBarBlock.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        topBarBlock.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(topBarBlock);

        var topBarBg = new ColorRect();
        topBarBg.Name = "Background";
        topBarBg.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        topBarBg.Color = GameConstants.HudTopBarBackgroundColor;
        topBarBg.Position = Vector2.Zero;
        topBarBg.MouseFilter = Control.MouseFilterEnum.Ignore;
        topBarBlock.AddChild(topBarBg);

        // Faint procedural diagonal lattice behind the stats (alpha <= 0.06).
        var pattern = new Control();
        pattern.Name = "Pattern";
        pattern.Position = Vector2.Zero;
        pattern.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        pattern.MouseFilter = Control.MouseFilterEnum.Ignore;
        pattern.Draw += () => DrawTopBarPattern(pattern);
        topBarBlock.AddChild(pattern);

        // Two-pixel bottom accent border.
        var border = new Control();
        border.Name = "Border";
        border.Position = new Vector2(0f, GameConstants.TopBarHeight - TopBarAccentHeight);
        border.Size = new Vector2(viewportSize.X, TopBarAccentHeight);
        border.MouseFilter = Control.MouseFilterEnum.Ignore;
        border.Draw += () => DrawTopBarBorder(border);
        topBarBlock.AddChild(border);

        // Three stat groups, left-aligned with a uniform gap. Each group spans its
        // procedural icon, uppercase caption, and the existing value label.
        float groupX = TopBarStatStartX;
        _hpLabel = BuildStatGroup(
            topBarBlock, "Block_Health", "HpIcon", "HpLabel", "HEALTH", "3",
            GameConstants.HudHealthColor, HudIcons.DrawHealth, groupX);
        groupX += TopBarStatGroupWidth + TopBarStatGroupGap;
        _coinsLabel = BuildStatGroup(
            topBarBlock, "Block_Coins", "CoinIcon", "CoinsLabel", "COINS", "10",
            GameConstants.HudCoinColor, HudIcons.DrawCoin, groupX);
        groupX += TopBarStatGroupWidth + TopBarStatGroupGap;
        _waveLabel = BuildStatGroup(
            topBarBlock, "Block_Wave", "WaveIcon", "WaveLabel", "WAVE", "0/5",
            GameConstants.HudWaveColor, HudIcons.DrawWave, groupX);

        // Speedup cue: a fixed-rect fast-forward indicator right-aligned inside the
        // play area's portion of the bar, well clear of Block_Wave and entirely left
        // of the sidebar. Click-through and visual only; it defaults hidden and only
        // GameManager's SpeedupChanged signal shows it.
        _speedupCue = new SpeedupCue();
        _speedupCue.Name = "Block_Speedup";
        _speedupCue.Position = new Vector2(
            GameConstants.PlayAreaWidth
                - GameConstants.HudSpeedupCueRightInset
                - GameConstants.HudSpeedupCueWidth,
            (GameConstants.TopBarHeight - GameConstants.HudSpeedupCueHeight) / 2f);
        topBarBlock.AddChild(_speedupCue);

        // The sidebar sits to the RIGHT of the play area (grid), not overlapping it.
        float sidebarX = GameConstants.PlayAreaWidth;

        // Sidebar group: background, separator, and action buttons. The wrapper's
        // origin is the sidebar's top-left so children keep their global positions.
        var sidebarBlock = new Control();
        sidebarBlock.Name = "Block_Sidebar";
        sidebarBlock.Position = new Vector2(sidebarX, 0);
        sidebarBlock.Size = new Vector2(GameConstants.SidebarWidth, viewportSize.Y);
        sidebarBlock.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(sidebarBlock);

        // Right sidebar background — full height, opaque, next to the play area
        var sidebarBg = new ColorRect();
        sidebarBg.Name = "Background";
        sidebarBg.Size = new Vector2(GameConstants.SidebarWidth, viewportSize.Y);
        sidebarBg.Color = new Color(0.05f, 0.05f, 0.1f, 1.0f);
        sidebarBg.Position = Vector2.Zero;
        sidebarBlock.AddChild(sidebarBg);

        // Vertical separator line between the play area and sidebar
        var sidebarBorder = new ColorRect();
        sidebarBorder.Name = "Border";
        sidebarBorder.Size = new Vector2(2, viewportSize.Y);
        sidebarBorder.Color = new Color(0.3f, 0.3f, 0.5f, 0.8f);
        sidebarBorder.Position = Vector2.Zero;
        sidebarBlock.AddChild(sidebarBorder);

        float sidebarCenterX = GameConstants.SidebarWidth / 2f;
        float sidebarButtonX = sidebarCenterX - GameConstants.SidebarCardWidth / 2f;

        // Placement cards: Arrow, Cannon, Laser stacked vertically. Buttons keep their
        // historical names/paths and signal payloads; the content is procedural.
        float cardPitch = GameConstants.SidebarCardHeight + GameConstants.SidebarCardGap;
        _placeArrowButton = CreatePlacementCard(
            sidebarBlock, "PlaceArrow", TowerType.Arrow, "Arrow",
            new Vector2(sidebarButtonX, GameConstants.SidebarCardTop));
        _placeCannonButton = CreatePlacementCard(
            sidebarBlock, "PlaceCannon", TowerType.Cannon, "Cannon",
            new Vector2(sidebarButtonX, GameConstants.SidebarCardTop + cardPitch));
        _placeCannonButton.Visible = false;
        _placeLaserButton = CreatePlacementCard(
            sidebarBlock, "PlaceLaser", TowerType.Laser, "Laser",
            new Vector2(sidebarButtonX, GameConstants.SidebarCardTop + cardPitch * 2f));
        _placeLaserButton.Visible = false;

        // Apply discounted prices once the cards exist (SkillState may have been set
        // before this node entered the tree).
        RefreshCostLabels();

        // Start Wave button — below the full card stack.
        float startWaveY = GameConstants.SidebarCardTop + cardPitch * 3f
            + GameConstants.SidebarStartWaveGap;
        _startWaveButton = new Button();
        _startWaveButton.Name = "StartWave";
        _startWaveButton.Text = "Start Wave";
        _startWaveButton.Position = new Vector2(sidebarCenterX - 70f, startWaveY);
        _startWaveButton.Size = new Vector2(140, 40);
        _startWaveButton.Pressed += OnStartWavePressed;
        _startWaveButton.AddThemeFontSizeOverride("font_size", 14);
        sidebarBlock.AddChild(_startWaveButton);

        UpdateCardDims();
    }

    /// <summary>
    /// Builds one top-bar stat group: a click-through wrapper spanning a procedural
    /// icon, an uppercase caption, and the value label (returned so the existing
    /// HpLabel/CoinsLabel/WaveLabel fields can be updated by signal handlers).
    /// </summary>
    private static Label BuildStatGroup(
        Control parent,
        string groupName,
        string iconName,
        string valueName,
        string captionText,
        string valueText,
        Color color,
        Action<CanvasItem, Vector2, float, Color> drawIcon,
        float x)
    {
        var group = new Control();
        group.Name = groupName;
        group.Position = new Vector2(x, 0f);
        group.Size = new Vector2(TopBarStatGroupWidth, GameConstants.TopBarHeight);
        group.MouseFilter = Control.MouseFilterEnum.Ignore;
        parent.AddChild(group);

        float iconRadius = TopBarStatIconSize / 2f;
        var icon = new Control();
        icon.Name = iconName;
        icon.Size = new Vector2(TopBarStatIconSize, TopBarStatIconSize);
        icon.Position = new Vector2(0f, (GameConstants.TopBarHeight - TopBarStatIconSize) / 2f);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon))
                return;
            drawIcon(icon, new Vector2(iconRadius, iconRadius), iconRadius, color);
        };
        group.AddChild(icon);

        var caption = new Label();
        caption.Name = "Caption";
        caption.Text = captionText;
        caption.VerticalAlignment = VerticalAlignment.Center;
        caption.AddThemeFontSizeOverride("font_size", TopBarCaptionFontSize);
        caption.AddThemeColorOverride("font_color", color);
        caption.MouseFilter = Control.MouseFilterEnum.Ignore;
        group.AddChild(caption);

        var value = new Label();
        value.Name = valueName;
        value.Text = valueText;
        value.VerticalAlignment = VerticalAlignment.Center;
        value.AddThemeFontSizeOverride("font_size", TopBarValueFontSize);
        value.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        value.MouseFilter = Control.MouseFilterEnum.Ignore;
        group.AddChild(value);

        // Centre the text rows from their real minimum sizes so the 40px bar never
        // clips a caption or value (fonts resolve their line height from the theme).
        float captionHeight = Mathf.Max(
            TopBarStatCaptionHeight, caption.GetCombinedMinimumSize().Y);
        float valueHeight = Mathf.Max(
            TopBarStatValueHeight, value.GetCombinedMinimumSize().Y);
        caption.Position = new Vector2(
            TopBarStatTextX,
            (GameConstants.TopBarHeight - captionHeight) / 2f);
        caption.Size = new Vector2(TopBarStatCaptionWidth, captionHeight);
        value.Position = new Vector2(
            TopBarStatValueX,
            (GameConstants.TopBarHeight - valueHeight) / 2f);
        value.Size = new Vector2(TopBarStatValueWidth, valueHeight);

        return value;
    }

    /// <summary>
    /// Faint diagonal lattice behind the top-bar stats. Painted once when the bar is
    /// built (never per frame); alpha is capped by HudTopBarPatternColor.
    /// </summary>
    private static void DrawTopBarPattern(Control pattern)
    {
        if (!IsInstanceValid(pattern))
            return;

        Vector2 size = pattern.Size;
        Color line = GameConstants.HudTopBarPatternColor;
        for (float x = -size.Y; x < size.X; x += TopBarPatternSpacing)
        {
            pattern.DrawLine(
                new Vector2(x, size.Y),
                new Vector2(x + size.Y, 0f),
                line,
                1f,
                antialiased: true);
        }
    }

    /// <summary>Two-pixel accent line along the bar's bottom edge.</summary>
    private static void DrawTopBarBorder(Control border)
    {
        if (!IsInstanceValid(border))
            return;

        border.DrawRect(new Rect2(Vector2.Zero, border.Size), GameConstants.HudTopBarAccentColor);
    }

    /// <summary>
    /// Builds one tower placement card. The Button itself is the only mouse target;
    /// its Icon/Name/Cost children are click-through decorations. The button's own
    /// Text stays empty so the child labels carry all text.
    /// </summary>
    private Button CreatePlacementCard(
        Control parent,
        string nodeName,
        TowerType type,
        string displayName,
        Vector2 position)
    {
        var button = new Button();
        button.Name = nodeName;
        button.Text = string.Empty;
        button.Position = position;
        button.Size = new Vector2(GameConstants.SidebarCardWidth, GameConstants.SidebarCardHeight);
        button.Disabled = true;
        button.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)type);
        parent.AddChild(button);

        Color accent = TowerGlyphs.ColorFor(type);
        float iconSize = GameConstants.SidebarCardIconSize;

        var icon = new Control();
        icon.Name = "Icon";
        icon.Size = new Vector2(iconSize, iconSize);
        icon.Position = new Vector2(
            GameConstants.SidebarCardIconInset,
            (GameConstants.SidebarCardHeight - iconSize) / 2f);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon)) return;
            TowerGlyphs.Draw(
                icon,
                type,
                new Vector2(iconSize / 2f, iconSize / 2f),
                iconSize * 0.42f,
                accent);
        };
        button.AddChild(icon);

        var nameLabel = new Label();
        nameLabel.Name = "Name";
        nameLabel.Text = displayName;
        nameLabel.Position = new Vector2(GameConstants.SidebarCardTextX, 12);
        nameLabel.Size = new Vector2(96, 26);
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1f));
        nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        button.AddChild(nameLabel);

        var costLabel = new Label();
        costLabel.Name = "Cost";
        // The single price-label rule lives in RefreshCostLabels(), which runs for the
        // whole card stack at build time and whenever SkillState changes.
        costLabel.Text = string.Empty;
        costLabel.Position = new Vector2(GameConstants.SidebarCardTextX, 40);
        costLabel.Size = new Vector2(96, 22);
        costLabel.AddThemeFontSizeOverride("font_size", 14);
        costLabel.AddThemeColorOverride("font_color", GameConstants.SidebarCardCostColor);
        costLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        button.AddChild(costLabel);

        GeometricButtonStyle.ApplyCard(button, accent);

        _placementCards.Add((button, type, icon, nameLabel, costLabel));
        return button;
    }

    /// <summary>
    /// Rewrites every placement card's price label from the effective (discounted)
    /// cost. Called when the skill state is attached and once at build time.
    /// </summary>
    private void RefreshCostLabels()
    {
        foreach (var card in _placementCards)
        {
            if (card.Cost == null || !IsInstanceValid(card.Cost))
                continue;

            card.Cost.Text = SkillStats.EffectiveTowerCost(card.Type, _skillState).ToString();
        }
    }

    /// <summary>
    /// Dims the icon and labels of disabled cards (and restores full brightness when a
    /// card becomes affordable again). The Button's own Disabled state drives input.
    /// </summary>
    private void UpdateCardDims()
    {
        for (int i = 0; i < _placementCards.Count; i++)
        {
            var card = _placementCards[i];
            if (card.Button == null || !IsInstanceValid(card.Button))
                continue;

            Color modulate = card.Button.Disabled
                ? GameConstants.SidebarCardDisabledModulate
                : Colors.White;
            card.Icon.Modulate = modulate;
            card.Name.Modulate = modulate;
            card.Cost.Modulate = modulate;
        }
    }

    /// <summary>
    /// Configure level-specific HUD visibility (e.g., hide the Cannon button in Level 1).
    /// </summary>
    public void ConfigureForLevel(LevelDefinition level)
    {
        _allowCannon = level.AllowCannonTower;
        _allowLaser = level.AllowLaserTower;
        _totalWaves = level.Waves.Count;
        if (_placeCannonButton != null)
            _placeCannonButton.Visible = _allowCannon;
        if (_placeLaserButton != null)
            _placeLaserButton.Visible = _allowLaser;
        UpdateCardDims();
    }

    /// <summary>
    /// Connect this HUD to a GameManager for state updates.
    /// Unsubscribes from the previous GameManager first to prevent duplicates.
    /// </summary>
    public void ConnectToGameManager(GameManager gm)
    {
        // Clean up old connections
        if (_gameManager != null)
        {
            _gameManager.CoinsChanged -= OnCoinsChanged;
            _gameManager.HpChanged -= OnHpChanged;
            _gameManager.WaveChanged -= OnWaveChanged;
            _gameManager.GameOver -= OnGameOver;
            _gameManager.Victory -= OnVictory;
            _gameManager.TowerPlacementStateChanged -= OnTowerPlacementStateChanged;
            _gameManager.SpeedupChanged -= OnSpeedupChanged;
        }

        _gameManager = gm;

        gm.CoinsChanged += OnCoinsChanged;
        gm.HpChanged += OnHpChanged;
        gm.WaveChanged += OnWaveChanged;
        gm.GameOver += OnGameOver;
        gm.Victory += OnVictory;
        gm.TowerPlacementStateChanged += OnTowerPlacementStateChanged;
        gm.SpeedupChanged += OnSpeedupChanged;

        // Sync initial state so HUD reflects current GameManager state. Costs are
        // recomputed too: global ranks may have been bought on the title-screen skill
        // tree after this HUD was built.
        RefreshCostLabels();
        OnCoinsChanged(gm.Coins);
        OnHpChanged(gm.HP);
        OnWaveChanged(0);
        UpdateTowerButtonStates();
        OnSpeedupChanged(gm.IsSpeedupActive);
    }

    private void OnCoinsChanged(int coins)
    {
        if (_coinsLabel != null)
            _coinsLabel.Text = coins.ToString();

        UpdateTowerButtonStates();
    }

    private void OnHpChanged(int hp)
    {
        if (_hpLabel != null)
            _hpLabel.Text = hp.ToString();
    }

    private void OnWaveChanged(int waveNumber)
    {
        if (_waveLabel != null)
        {
            _waveLabel.Text = $"{waveNumber}/{_totalWaves}";
        }
    }

    private void OnGameOver()
    {
        DisableAllCards();
    }

    private void OnVictory()
    {
        DisableAllCards();
    }

    /// <summary>
    /// Shows the top-bar fast-forward cue only while the game clock is sped up. The
    /// cue owns its own animation and stops processing while hidden.
    /// </summary>
    private void OnSpeedupChanged(bool active)
    {
        _speedupCue?.SetActive(active);
    }

    /// <summary>
    /// Play has ended: disable every action immediately (so the dim state is applied
    /// in the same frame) and defer the Start Wave disable as before. The card state
    /// itself is derived from the GameManager's state in UpdateTowerButtonStates.
    /// </summary>
    private void DisableAllCards()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        UpdateTowerButtonStates();
    }

    private void OnTowerPlacementStateChanged(bool canPlace)
    {
        UpdateTowerButtonStates();
    }

    private void UpdateTowerButtonStates()
    {
        int coins = _gameManager?.Coins ?? 0;
        bool playEnded = _gameManager != null && _gameManager.State != GameState.Playing;

        if (_placeArrowButton != null)
        {
            _placeArrowButton.Disabled =
                playEnded || coins < SkillStats.EffectiveTowerCost(TowerType.Arrow, _skillState);
        }

        if (_placeCannonButton != null)
        {
            _placeCannonButton.Disabled =
                playEnded || !_allowCannon ||
                coins < SkillStats.EffectiveTowerCost(TowerType.Cannon, _skillState);
        }

        if (_placeLaserButton != null)
        {
            _placeLaserButton.Disabled =
                playEnded || !_allowLaser ||
                coins < SkillStats.EffectiveTowerCost(TowerType.Laser, _skillState);
        }

        UpdateCardDims();
    }

    /// <summary>
    /// Update Start Wave button enabled state based on wave activity.
    /// </summary>
    public void SetStartWaveEnabled(bool enabled)
    {
        if (_startWaveButton != null)
            _startWaveButton.Disabled = !enabled;
    }

    private void OnStartWavePressed()
    {
        EmitSignal(SignalName.StartWavePressed);
    }
}
