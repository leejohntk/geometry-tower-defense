using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Main game controller. Manages scene flow: title -> game -> game over / victory -> title.
/// </summary>
public partial class Main : Node2D
{
    private TitleScreen? _titleScreen;
    private GameManager? _gameManager;
    private GameHUD? _gameHUD;
    private GameOverScreen? _gameOverScreen;
    private VictoryScreen? _victoryScreen;
    private bool _gameRunning = false;

    public override void _Ready()
    {
        // Create title screen
        _titleScreen = new TitleScreen();
        _titleScreen.StartGame += OnStartGame;
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
        _gameOverScreen = new GameOverScreen();
        _gameOverScreen.Name = "GameOverScreen";
        _gameOverScreen.ReturnToTitle += OnReturnToTitle;
        _gameOverScreen.Visible = false;
        AddChild(_gameOverScreen);

        // Create victory screen (hidden initially)
        _victoryScreen = new VictoryScreen();
        _victoryScreen.Name = "VictoryScreen";
        _victoryScreen.ReturnToTitle += OnReturnToTitle;
        _victoryScreen.Visible = false;
        AddChild(_victoryScreen);
    }

    private void OnStartGame()
    {
        StartNewGame();
    }

    private void StartNewGame()
    {
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
                _gameHUD.ConnectToGameManager(_gameManager);
                _gameHUD.SetStartWaveEnabled(true);
            }

            _gameManager.Initialize();

            _gameRunning = true;
        }

        // Hide overlays
        if (_gameOverScreen != null)
            _gameOverScreen.Visible = false;
        if (_victoryScreen != null)
            _victoryScreen.Visible = false;
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

    private void OnPlaceTowerPressed()
    {
        if (!_gameRunning || _gameManager == null) return;

        if (_gameManager.IsPlacingTower)
        {
            // Cancel placement
            _gameManager.IsPlacingTower = false;
            _gameHUD?.SetStartWaveEnabled(true);
            // Reset cursor
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        }
        else
        {
            // Enter placement mode
            _gameManager.IsPlacingTower = true;
            _gameHUD?.SetStartWaveEnabled(false); // Disable wave start during placement
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);
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
        if (_gameHUD == null) return;

        if (waveNumber > 0)
        {
            // Check if wave is still active (if not, it just completed)
            bool waveActive = _gameManager?.WaveManager?.IsWaveActive ?? false;
            _gameHUD.SetStartWaveEnabled(!waveActive);
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
    }

    public override void _Input(InputEvent @event)
    {
        if (!_gameRunning || _gameManager == null)
            return;

        if (_gameManager.IsPlacingTower)
        {
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
                _gameManager.IsPlacingTower = false;
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                _gameHUD?.SetStartWaveEnabled(true);
            }

            return;
        }

        // Check if we clicked on an existing tower (not in placement mode)
        if (@event is InputEventMouseButton click &&
            click.ButtonIndex == MouseButton.Left &&
            click.Pressed)
        {
            foreach (var tower in _gameManager.GetActiveTowers())
            {
                float towerSize = GameConstants.CellSize;
                Rect2 towerRect = new Rect2(
                    tower.Position - new Vector2(towerSize / 2f, towerSize / 2f),
                    new Vector2(towerSize, towerSize)
                );
                if (towerRect.HasPoint(click.Position))
                {
                    tower.ToggleRange();
                    break;
                }
            }
        }
    }

    private void TryPlaceTowerAtMouse(Vector2 mousePos)
    {
        if (_gameManager?.Grid == null) return;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(mousePos);

        // Offset by the gameplay area position (no offset since GameManager is at 0,0)
        bool placed = _gameManager.PlaceTower(gridPos.Y, gridPos.X);

        if (placed)
        {
            // Stay in placement mode - player can place multiple towers
            // Re-check if we can still place (might have run out of coins)
            if (_gameManager.Coins < GameConstants.ArrowTowerCost)
            {
                _gameManager.IsPlacingTower = false;
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                _gameHUD?.SetStartWaveEnabled(true);
            }
        }
    }
}
