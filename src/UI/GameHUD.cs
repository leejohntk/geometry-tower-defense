using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// In-game HUD showing HP, coins, wave counter, tower placement button, and Start Wave button.
/// </summary>
public partial class GameHUD : CanvasLayer
{
    [Signal]
    public delegate void StartWavePressedEventHandler();

    [Signal]
    public delegate void PlaceTowerPressedEventHandler();

    private Label? _hpLabel;
    private Label? _coinsLabel;
    private Label? _waveLabel;
    private Button? _startWaveButton;
    private Button? _placeTowerButton;
    private GameManager? _gameManager;

    public override void _Ready()
    {
        // HUD background bar at the top
        var hudBg = new ColorRect();
        var viewportSize = GetViewport().GetVisibleRect().Size;
        hudBg.Size = new Vector2(viewportSize.X, 48);
        hudBg.Color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        hudBg.Position = new Vector2(0, 0);
        AddChild(hudBg);

        float currentX = 16f;

        // HP display
        var hpIcon = new Label();
        hpIcon.Text = "[HP]";
        hpIcon.Position = new Vector2(currentX, 8);
        hpIcon.Size = new Vector2(40, 32);
        hpIcon.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        AddChild(hpIcon);
        currentX += 45f;

        _hpLabel = new Label();
        _hpLabel.Text = "3";
        _hpLabel.Position = new Vector2(currentX, 8);
        _hpLabel.Size = new Vector2(40, 32);
        _hpLabel.AddThemeFontSizeOverride("font_size", 20);
        _hpLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_hpLabel);
        currentX += 80f;

        // Coins display
        var coinIcon = new Label();
        coinIcon.Text = "[$]";
        coinIcon.Position = new Vector2(currentX, 8);
        coinIcon.Size = new Vector2(40, 32);
        coinIcon.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        AddChild(coinIcon);
        currentX += 45f;

        _coinsLabel = new Label();
        _coinsLabel.Text = "10";
        _coinsLabel.Position = new Vector2(currentX, 8);
        _coinsLabel.Size = new Vector2(40, 32);
        _coinsLabel.AddThemeFontSizeOverride("font_size", 20);
        _coinsLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_coinsLabel);
        currentX += 80f;

        // Wave display
        var waveIcon = new Label();
        waveIcon.Text = "Wave";
        waveIcon.Position = new Vector2(currentX, 8);
        waveIcon.Size = new Vector2(50, 32);
        waveIcon.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 1f));
        AddChild(waveIcon);
        currentX += 55f;

        _waveLabel = new Label();
        _waveLabel.Text = "0/5";
        _waveLabel.Position = new Vector2(currentX, 8);
        _waveLabel.Size = new Vector2(60, 32);
        _waveLabel.AddThemeFontSizeOverride("font_size", 20);
        _waveLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        AddChild(_waveLabel);
        currentX += 380f;

        // Place Tower button
        _placeTowerButton = new Button();
        _placeTowerButton.Text = "Place Tower (10$)";
        _placeTowerButton.Position = new Vector2(currentX, 4);
        _placeTowerButton.Size = new Vector2(160, 40);
        _placeTowerButton.Disabled = true; // Initially check
        _placeTowerButton.Pressed += OnPlaceTowerPressed;
        _placeTowerButton.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_placeTowerButton);
        currentX += 170f;

        // Start Wave button
        _startWaveButton = new Button();
        _startWaveButton.Text = "Start Wave";
        _startWaveButton.Position = new Vector2(currentX, 4);
        _startWaveButton.Size = new Vector2(140, 40);
        _startWaveButton.Pressed += OnStartWavePressed;
        _startWaveButton.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_startWaveButton);
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
    }

    private void OnHpChanged(int hp)
    {
        if (_hpLabel != null)
            _hpLabel.Text = hp.ToString();
    }

    private void OnWaveChanged(int waveNumber)
    {
        if (_waveLabel != null)
            _waveLabel.Text = $"{waveNumber}/{GameConstants.TotalWaves}";
    }

    private void OnGameOver()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeTowerButton?.SetDeferred("disabled", true);
    }

    private void OnVictory()
    {
        _startWaveButton?.SetDeferred("disabled", true);
        _placeTowerButton?.SetDeferred("disabled", true);
    }

    private void OnTowerPlacementStateChanged(bool canPlace)
    {
        if (_placeTowerButton != null)
            _placeTowerButton.Disabled = !canPlace;
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

    private void OnPlaceTowerPressed()
    {
        EmitSignal(SignalName.PlaceTowerPressed);
    }
}
