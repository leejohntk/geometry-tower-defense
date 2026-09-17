using Godot;

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
    private GameManager? _gameManager;
    private bool _allowCannon = false;

    public override void _Ready()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;

        // Top bar background — spans the full viewport width, at top
        var topBarBg = new ColorRect();
        topBarBg.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        topBarBg.Color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        topBarBg.Position = new Vector2(0, 0);
        AddChild(topBarBg);

        float currentX = 16f;

        // HP display
        var hpIcon = new Label();
        hpIcon.Text = "[HP]";
        hpIcon.Position = new Vector2(currentX, 4);
        hpIcon.Size = new Vector2(40, 32);
        hpIcon.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        AddChild(hpIcon);
        currentX += 45f;

        _hpLabel = new Label();
        _hpLabel.Text = "3";
        _hpLabel.Position = new Vector2(currentX, 4);
        _hpLabel.Size = new Vector2(40, 32);
        _hpLabel.AddThemeFontSizeOverride("font_size", 20);
        _hpLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_hpLabel);
        currentX += 80f;

        // Coins display
        var coinIcon = new Label();
        coinIcon.Text = "[$]";
        coinIcon.Position = new Vector2(currentX, 4);
        coinIcon.Size = new Vector2(40, 32);
        coinIcon.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        AddChild(coinIcon);
        currentX += 45f;

        _coinsLabel = new Label();
        _coinsLabel.Text = "10";
        _coinsLabel.Position = new Vector2(currentX, 4);
        _coinsLabel.Size = new Vector2(40, 32);
        _coinsLabel.AddThemeFontSizeOverride("font_size", 20);
        _coinsLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_coinsLabel);
        currentX += 80f;

        // Wave display
        var waveIcon = new Label();
        waveIcon.Text = "Wave";
        waveIcon.Position = new Vector2(currentX, 4);
        waveIcon.Size = new Vector2(50, 32);
        waveIcon.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 1f));
        AddChild(waveIcon);
        currentX += 55f;

        _waveLabel = new Label();
        _waveLabel.Text = "0/5";
        _waveLabel.Position = new Vector2(currentX, 4);
        _waveLabel.Size = new Vector2(60, 32);
        _waveLabel.AddThemeFontSizeOverride("font_size", 20);
        _waveLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_waveLabel);

        // The sidebar sits to the RIGHT of the play area (grid), not overlapping it.
        float sidebarX = GameConstants.PlayAreaWidth;

        // Right sidebar background — full height, opaque, next to the play area
        var sidebarBg = new ColorRect();
        sidebarBg.Size = new Vector2(GameConstants.SidebarWidth, viewportSize.Y);
        sidebarBg.Color = new Color(0.05f, 0.05f, 0.1f, 1.0f);
        sidebarBg.Position = new Vector2(sidebarX, 0);
        AddChild(sidebarBg);

        // Vertical separator line between the play area and sidebar
        var sidebarBorder = new ColorRect();
        sidebarBorder.Size = new Vector2(2, viewportSize.Y);
        sidebarBorder.Color = new Color(0.3f, 0.3f, 0.5f, 0.8f);
        sidebarBorder.Position = new Vector2(sidebarX, 0);
        AddChild(sidebarBorder);

        float sidebarCenterX = sidebarX + GameConstants.SidebarWidth / 2f;

        // Place Arrow button
        _placeArrowButton = new Button();
        _placeArrowButton.Text = $"Place Arrow ({GameConstants.ArrowTowerCost}$)";
        _placeArrowButton.Position = new Vector2(sidebarCenterX - 80, 20);
        _placeArrowButton.Size = new Vector2(160, 40);
        _placeArrowButton.Disabled = true;
        _placeArrowButton.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)TowerType.Arrow);
        _placeArrowButton.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_placeArrowButton);

        // Place Cannon button (hidden for levels that don't allow it)
        _placeCannonButton = new Button();
        _placeCannonButton.Text = $"Place Cannon ({GameConstants.CannonTowerCost}$)";
        _placeCannonButton.Position = new Vector2(sidebarCenterX - 80, 70);
        _placeCannonButton.Size = new Vector2(160, 40);
        _placeCannonButton.Disabled = true;
        _placeCannonButton.Visible = false;
        _placeCannonButton.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)TowerType.Cannon);
        _placeCannonButton.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_placeCannonButton);

        // Start Wave button — in sidebar, below the tower buttons
        _startWaveButton = new Button();
        _startWaveButton.Text = "Start Wave";
        _startWaveButton.Position = new Vector2(sidebarCenterX - 70, 130);
        _startWaveButton.Size = new Vector2(140, 40);
        _startWaveButton.Pressed += OnStartWavePressed;
        _startWaveButton.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_startWaveButton);
    }

    /// <summary>
    /// Configure level-specific HUD visibility (e.g., hide the Cannon button in Level 1).
    /// </summary>
    public void ConfigureForLevel(LevelDefinition level)
    {
        _allowCannon = level.AllowCannonTower;
        if (_placeCannonButton != null)
            _placeCannonButton.Visible = _allowCannon;
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
        }

        _gameManager = gm;

        gm.CoinsChanged += OnCoinsChanged;
        gm.HpChanged += OnHpChanged;
        gm.WaveChanged += OnWaveChanged;
        gm.GameOver += OnGameOver;
        gm.Victory += OnVictory;
        gm.TowerPlacementStateChanged += OnTowerPlacementStateChanged;

        // Sync initial state so HUD reflects current GameManager state
        OnCoinsChanged(gm.Coins);
        OnHpChanged(gm.HP);
        OnWaveChanged(0);
        OnTowerPlacementStateChanged(gm.Coins >= GameConstants.ArrowTowerCost);
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
            int totalWaves = _gameManager?.Level?.Waves.Count ?? GameConstants.TotalWaves;
            _waveLabel.Text = $"{waveNumber}/{totalWaves}";
        }
    }

    private void OnGameOver()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeArrowButton?.SetDeferred("disabled", true);
        _placeCannonButton?.SetDeferred("disabled", true);
    }

    private void OnVictory()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeArrowButton?.SetDeferred("disabled", true);
        _placeCannonButton?.SetDeferred("disabled", true);
    }

    private void OnTowerPlacementStateChanged(bool canPlace)
    {
        UpdateTowerButtonStates();
    }

    private void UpdateTowerButtonStates()
    {
        int coins = _gameManager?.Coins ?? 0;

        if (_placeArrowButton != null)
            _placeArrowButton.Disabled = coins < GameConstants.ArrowTowerCost;

        if (_placeCannonButton != null)
            _placeCannonButton.Disabled = !_allowCannon || coins < GameConstants.CannonTowerCost;
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
