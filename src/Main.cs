using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Main game controller. Manages scene flow: title -> level select -> game -> game over / victory -> title.
/// </summary>
public partial class Main : Node2D
{
    private TitleScreen? _titleScreen;
    private GameManager? _gameManager;
    private GameHUD? _gameHUD;
    private ResultScreen? _gameOverScreen;
    private ResultScreen? _victoryScreen;
    private bool _gameRunning = false;
    private Tower? _selectedTower = null;
    private LevelDefinition? _selectedLevel = null;

    public override void _Ready()
    {
        // Create title screen
        _titleScreen = new TitleScreen();
        _titleScreen.LevelSelected += OnLevelSelected;
        AddChild(_titleScreen);

        // Create game manager (hidden initially)
        _gameManager = new GameManager();
        _gameManager.Name = "GameManager";
        _gameManager.GameOver += OnGameOverTriggered;
        _gameManager.Victory += OnVictoryTriggered;
        _gameManager.WaveChanged += OnWaveChangedForUI;
        _gameManager.Visible = false;
        _gameManager.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(_gameManager);

        // Create HUD (hidden initially)
        _gameHUD = new GameHUD();
        _gameHUD.Name = "GameHUD";
        _gameHUD.StartWavePressed += OnStartWavePressed;
        _gameHUD.PlaceTowerPressed += OnPlaceTowerPressed;
        _gameHUD.Visible = false;
        AddChild(_gameHUD);

        // Create game over screen (hidden initially)
        _gameOverScreen = new ResultScreen(
            new Color(0.8f, 0.05f, 0.05f, 0.6f),
            "GAME OVER",
            "The enemies reached your base..."
        );
        _gameOverScreen.Name = "GameOverScreen";
        _gameOverScreen.ReturnToTitle += OnReturnToTitle;
        _gameOverScreen.Visible = false;
        AddChild(_gameOverScreen);

        // Create victory screen (hidden initially)
        _victoryScreen = new ResultScreen(
            new Color(0.9f, 0.7f, 0.1f, 0.5f),
            "VICTORY",
            "You defended your base! All waves cleared!"
        );
        _victoryScreen.Name = "VictoryScreen";
        _victoryScreen.ReturnToTitle += OnReturnToTitle;
        _victoryScreen.Visible = false;
        AddChild(_victoryScreen);
    }

    private void OnLevelSelected(int levelId)
    {
        _selectedLevel = Levels.Get(levelId);
        StartNewGame();
    }

    private void StartNewGame()
    {
        if (_selectedLevel == null)
            return;

        // Hide title
        if (_titleScreen != null)
            _titleScreen.Visible = false;

        // Initialize game
        if (_gameManager != null)
        {
            _gameManager.Visible = true;
            _gameManager.ProcessMode = ProcessModeEnum.Inherit;

            // Connect HUD to game manager BEFORE Initialize so signals are in place
            if (_gameHUD != null)
            {
                _gameHUD.Visible = true;
                _gameHUD.ConfigureForLevel(_selectedLevel);
                _gameHUD.ConnectToGameManager(_gameManager);
                _gameHUD.SetStartWaveEnabled(true);
            }

            _gameManager.Initialize(_selectedLevel);

            _gameRunning = true;
        }

        // Ensure no stale tower selection carries over
        _selectedTower = null;

        // Hide overlays
        if (_gameOverScreen != null)
            _gameOverScreen.Visible = false;
        if (_victoryScreen != null)
            _victoryScreen.Visible = false;
    }

    /// <summary>
    /// Update the start wave button enabled state based on current game conditions.
    /// Button is disabled when a wave is active OR when in tower placement mode.
    /// </summary>
    private void UpdateStartWaveButtonState()
    {
        if (_gameHUD == null || _gameManager == null)
            return;

        // Disabled outside of active play (game over / victory).
        if (_gameManager.State != GameState.Playing)
        {
            _gameHUD.SetStartWaveEnabled(false);
            return;
        }

        // Disabled during tower placement mode
        if (_gameManager.IsPlacingTower)
        {
            _gameHUD.SetStartWaveEnabled(false);
            return;
        }

        // Disabled while a wave is active
        bool waveActive = _gameManager.WaveManager?.IsWaveActive ?? false;
        _gameHUD.SetStartWaveEnabled(!waveActive);
    }

    private void OnStartWavePressed()
    {
        if (!_gameRunning || _gameManager == null) return;

        bool started = _gameManager.StartNextWave();
        if (started && _gameHUD != null)
        {
            _gameHUD.SetStartWaveEnabled(false);
        }
    }

    private void OnPlaceTowerPressed(int towerType)
    {
        if (!_gameRunning || _gameManager == null) return;

        var type = (TowerType)towerType;

        if (_gameManager.PlacingTowerType == type)
        {
            // Cancel placement
            _gameManager.PlacingTowerType = null;
            _gameManager.Grid?.HidePlacementPreview();
            UpdateStartWaveButtonState();
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        }
        else
        {
            // Enter placement mode for the requested type
            _gameManager.PlacingTowerType = type;
            _gameHUD?.SetStartWaveEnabled(false); // Disable wave start during placement
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);

            // Hide any tower range that was showing
            _selectedTower?.HideRange();
            _selectedTower = null;
        }
    }

    private void OnGameOverTriggered()
    {
        if (_gameOverScreen != null)
            _gameOverScreen.Visible = true;
        _gameRunning = false;
    }

    private void OnVictoryTriggered()
    {
        if (_victoryScreen != null)
            _victoryScreen.Visible = true;
        _gameRunning = false;
    }

    private void OnWaveChangedForUI(int waveNumber)
    {
        if (waveNumber > 0)
        {
            UpdateStartWaveButtonState();
        }
    }

    private void OnReturnToTitle()
    {
        // Reset everything
        if (_gameManager != null)
        {
            _gameManager.Visible = false;
            _gameManager.ProcessMode = ProcessModeEnum.Disabled;
            _gameManager.ResetGame();
        }

        if (_gameHUD != null)
        {
            _gameHUD.Visible = false;
        }

        if (_gameOverScreen != null)
            _gameOverScreen.Visible = false;

        if (_victoryScreen != null)
            _victoryScreen.Visible = false;

        if (_titleScreen != null)
            _titleScreen.Visible = true;

        _gameRunning = false;
        _selectedTower = null;
        _selectedLevel = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_gameRunning || _gameManager == null)
            return;

        if (_gameManager.IsPlacingTower)
        {
            // Track mouse for placement preview (shows ghost + highlight + range)
            if (@event is InputEventMouseMotion mouseMotion)
            {
                UpdateTowerPlacementPreview(mouseMotion.Position);
            }

            // Handle tower placement via mouse click
            if (@event is InputEventMouseButton mouseButton &&
                mouseButton.ButtonIndex == MouseButton.Left &&
                mouseButton.Pressed)
            {
                TryPlaceTowerAtMouse(mouseButton.Position);
            }

            // Cancel placement with right click
            if (@event is InputEventMouseButton rightClick &&
                rightClick.ButtonIndex == MouseButton.Right &&
                rightClick.Pressed)
            {
                _gameManager.PlacingTowerType = null;
                _gameManager.Grid?.HidePlacementPreview();
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                UpdateStartWaveButtonState();
            }

            return;
        }

        // Handle tower selection / range display (not in placement mode)
        if (@event is InputEventMouseButton click &&
            click.ButtonIndex == MouseButton.Left &&
            click.Pressed)
        {
            // Find which tower (if any) was clicked
            Tower? clickedTower = null;
            foreach (var tower in _gameManager.GetActiveTowers())
            {
                float towerSize = GameConstants.CellSize;
                Rect2 towerRect = new Rect2(
                    tower.Position - new Vector2(towerSize / 2f, towerSize / 2f),
                    new Vector2(towerSize, towerSize)
                );
                if (towerRect.HasPoint(click.Position))
                {
                    clickedTower = tower;
                    break;
                }
            }

            if (clickedTower != null)
            {
                if (_selectedTower == clickedTower)
                {
                    // Clicked the same tower again — toggle range off
                    clickedTower.HideRange();
                    _selectedTower = null;
                }
                else
                {
                    // Clicked a different tower — hide previous, show new
                    _selectedTower?.HideRange();
                    clickedTower.ShowRange();
                    _selectedTower = clickedTower;
                }
            }
            else
            {
                // Clicked on empty space — hide range for the selected tower
                _selectedTower?.HideRange();
                _selectedTower = null;
            }
        }
    }

    /// <summary>
    /// Update the tower placement preview (ghost + highlight + range) at the cell under the mouse.
    /// Hides the preview if the mouse is outside the grid bounds.
    /// </summary>
    private void UpdateTowerPlacementPreview(Vector2 mousePos)
    {
        if (_gameManager?.Grid == null)
            return;

        if (_gameManager.PlacingTowerType == null)
            return;

        var type = _gameManager.PlacingTowerType.Value;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(mousePos);
        int row = gridPos.Y;
        int col = gridPos.X;

        if (_gameManager.Grid.IsInBounds(row, col))
        {
            bool enoughCoins = _gameManager.Coins >= GameConstants.TowerCost(type);
            bool canPlace = enoughCoins && _gameManager.Grid.CanPlaceTower(row, col);
            _gameManager.Grid.ShowPlacementPreview(row, col, canPlace, type);
        }
        else
        {
            _gameManager.Grid.HidePlacementPreview();
        }
    }

    private void TryPlaceTowerAtMouse(Vector2 mousePos)
    {
        if (_gameManager?.Grid == null)
            return;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(mousePos);

        bool placed = _gameManager.PlaceTower(gridPos.Y, gridPos.X);

        if (placed)
        {
            // Hide preview momentarily — next mouse motion will re-show
            _gameManager.Grid.HidePlacementPreview();

            // Stay in placement mode - player can place multiple towers.
            // Re-check if we can still afford the selected type.
            var type = _gameManager.PlacingTowerType;
            if (type != null && _gameManager.Coins < GameConstants.TowerCost(type.Value))
            {
                _gameManager.PlacingTowerType = null;
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                UpdateStartWaveButtonState();
            }
        }
    }
}
