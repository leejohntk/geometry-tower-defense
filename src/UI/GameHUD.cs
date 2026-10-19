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
    private Button? _placeLaserButton;
    private GameManager? _gameManager;
    private bool _allowCannon = false;
    private bool _allowLaser = false;
    private int _totalWaves = 0;

    public override void _Ready()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;

        // Top bar group — spans the full viewport width, at top. The wrapper sits at
        // the origin so its children keep absolute positions.
        var topBarBlock = new Control();
        topBarBlock.Name = "Block_TopBar";
        topBarBlock.Position = Vector2.Zero;
        topBarBlock.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        topBarBlock.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(topBarBlock);

        var topBarBg = new ColorRect();
        topBarBg.Name = "Background";
        topBarBg.Size = new Vector2(viewportSize.X, GameConstants.TopBarHeight);
        topBarBg.Color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        topBarBg.Position = Vector2.Zero;
        topBarBlock.AddChild(topBarBg);

        float currentX = 16f;

        // HP display
        var hpIcon = new Label();
        hpIcon.Name = "HpIcon";
        hpIcon.Text = "[HP]";
        hpIcon.Position = new Vector2(currentX, 4);
        hpIcon.Size = new Vector2(40, 32);
        hpIcon.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        topBarBlock.AddChild(hpIcon);
        currentX += 45f;

        _hpLabel = new Label();
        _hpLabel.Name = "HpLabel";
        _hpLabel.Text = "3";
        _hpLabel.Position = new Vector2(currentX, 4);
        _hpLabel.Size = new Vector2(40, 32);
        _hpLabel.AddThemeFontSizeOverride("font_size", 20);
        _hpLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        topBarBlock.AddChild(_hpLabel);
        currentX += 80f;

        // Coins display
        var coinIcon = new Label();
        coinIcon.Name = "CoinIcon";
        coinIcon.Text = "[$]";
        coinIcon.Position = new Vector2(currentX, 4);
        coinIcon.Size = new Vector2(40, 32);
        coinIcon.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        topBarBlock.AddChild(coinIcon);
        currentX += 45f;

        _coinsLabel = new Label();
        _coinsLabel.Name = "CoinsLabel";
        _coinsLabel.Text = "10";
        _coinsLabel.Position = new Vector2(currentX, 4);
        _coinsLabel.Size = new Vector2(40, 32);
        _coinsLabel.AddThemeFontSizeOverride("font_size", 20);
        _coinsLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        topBarBlock.AddChild(_coinsLabel);
        currentX += 80f;

        // Wave display
        var waveIcon = new Label();
        waveIcon.Name = "WaveIcon";
        waveIcon.Text = "Wave";
        waveIcon.Position = new Vector2(currentX, 4);
        waveIcon.Size = new Vector2(50, 32);
        waveIcon.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 1f));
        topBarBlock.AddChild(waveIcon);
        currentX += 55f;

        _waveLabel = new Label();
        _waveLabel.Name = "WaveLabel";
        _waveLabel.Text = "0/5";
        _waveLabel.Position = new Vector2(currentX, 4);
        _waveLabel.Size = new Vector2(60, 32);
        _waveLabel.AddThemeFontSizeOverride("font_size", 20);
        _waveLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        topBarBlock.AddChild(_waveLabel);

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
        float sidebarButtonX = sidebarCenterX - 80f;

        // Place Arrow button
        _placeArrowButton = new Button();
        _placeArrowButton.Name = "PlaceArrow";
        _placeArrowButton.Text = $"Place Arrow ({GameConstants.ArrowTowerCost}$)";
        _placeArrowButton.Position = new Vector2(sidebarButtonX, 20);
        _placeArrowButton.Size = new Vector2(160, 40);
        _placeArrowButton.Disabled = true;
        _placeArrowButton.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)TowerType.Arrow);
        _placeArrowButton.AddThemeFontSizeOverride("font_size", 12);
        sidebarBlock.AddChild(_placeArrowButton);

        // Place Cannon button (hidden for levels that don't allow it)
        _placeCannonButton = new Button();
        _placeCannonButton.Name = "PlaceCannon";
        _placeCannonButton.Text = $"Place Cannon ({GameConstants.CannonTowerCost}$)";
        _placeCannonButton.Position = new Vector2(sidebarButtonX, 70);
        _placeCannonButton.Size = new Vector2(160, 40);
        _placeCannonButton.Disabled = true;
        _placeCannonButton.Visible = false;
        _placeCannonButton.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)TowerType.Cannon);
        _placeCannonButton.AddThemeFontSizeOverride("font_size", 12);
        sidebarBlock.AddChild(_placeCannonButton);

        // Place Laser button (hidden for levels that don't allow it)
        _placeLaserButton = new Button();
        _placeLaserButton.Name = "PlaceLaser";
        _placeLaserButton.Text = $"Place Laser ({GameConstants.LaserTowerCost}$)";
        _placeLaserButton.Position = new Vector2(sidebarButtonX, 120);
        _placeLaserButton.Size = new Vector2(160, 40);
        _placeLaserButton.Disabled = true;
        _placeLaserButton.Visible = false;
        _placeLaserButton.Pressed += () => EmitSignal(SignalName.PlaceTowerPressed, (int)TowerType.Laser);
        _placeLaserButton.AddThemeFontSizeOverride("font_size", 12);
        sidebarBlock.AddChild(_placeLaserButton);

        // Start Wave button — in sidebar, below the tower buttons
        _startWaveButton = new Button();
        _startWaveButton.Name = "StartWave";
        _startWaveButton.Text = "Start Wave";
        _startWaveButton.Position = new Vector2(sidebarCenterX - 70f, 180);
        _startWaveButton.Size = new Vector2(140, 40);
        _startWaveButton.Pressed += OnStartWavePressed;
        _startWaveButton.AddThemeFontSizeOverride("font_size", 14);
        sidebarBlock.AddChild(_startWaveButton);
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
            _waveLabel.Text = $"{waveNumber}/{_totalWaves}";
        }
    }

    private void OnGameOver()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeArrowButton?.SetDeferred("disabled", true);
        _placeCannonButton?.SetDeferred("disabled", true);
        _placeLaserButton?.SetDeferred("disabled", true);
    }

    private void OnVictory()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeArrowButton?.SetDeferred("disabled", true);
        _placeCannonButton?.SetDeferred("disabled", true);
        _placeLaserButton?.SetDeferred("disabled", true);
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

        if (_placeLaserButton != null)
            _placeLaserButton.Disabled = !_allowLaser || coins < GameConstants.LaserTowerCost;
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
