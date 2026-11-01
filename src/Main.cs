using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Main game controller. Manages scene flow: title -> level select -> game -> game over / victory -> title.
/// </summary>
public partial class Main : Node2D
{
    private TitleScreen? _titleScreen;
    private SkillTree? _skillTree;
    private SkillTreeScreen? _skillTreeScreen;
    private GameManager? _gameManager;
    private GameHUD? _gameHUD;
    private TowerUpgradePanel? _upgradePanel;
    private ResultScreen? _gameOverScreen;
    private ResultScreen? _victoryScreen;
    private bool _gameRunning = false;
    private Tower? _selectedTower = null;
    private LevelDefinition? _selectedLevel = null;

    public override void _Ready()
    {
        // Load the persistent skill tree before any screen or game object needs it.
        _skillTree = new SkillTree(SkillTreeSave.Load());

        // Create title screen
        _titleScreen = new TitleScreen();
        _titleScreen.LevelSelected += OnLevelSelected;
        _titleScreen.SkillTreePressed += OnSkillTreePressed;
        AddChild(_titleScreen);

        // Create skill tree screen (hidden initially)
        _skillTreeScreen = new SkillTreeScreen(_skillTree);
        _skillTreeScreen.Name = "SkillTreeScreen";
        _skillTreeScreen.BackPressed += OnSkillTreeBackPressed;
        _skillTreeScreen.Visible = false;
        AddChild(_skillTreeScreen);

        // Create game manager (hidden initially)
        _gameManager = new GameManager();
        _gameManager.Name = "GameManager";
        _gameManager.SkillTree = _skillTree;
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

        // Per-selection tower upgrade overlay (hidden until a tower is selected).
        // Added before the result screens so those overlays always draw on top; the
        // panel is also explicitly closed before a result screen is shown.
        _upgradePanel = new TowerUpgradePanel();
        _upgradePanel.UpgradeRequested += OnTowerUpgradeRequested;
        _upgradePanel.ArchetypeChosen += OnTowerArchetypeChosen;
        _gameManager.CoinsChanged += OnCoinsChangedForUpgradePanel;
        AddChild(_upgradePanel);

        // Create game over screen (hidden initially)
        _gameOverScreen = new ResultScreen(
            new Color(0.8f, 0.05f, 0.05f, 0.6f),
            "GAME OVER",
            "The enemies reached your base...",
            ResultScreenMode.Defeat
        );
        _gameOverScreen.Name = "GameOverScreen";
        _gameOverScreen.ReturnToTitle += OnReturnToTitle;
        _gameOverScreen.Visible = false;
        AddChild(_gameOverScreen);

        // Create victory screen (hidden initially)
        _victoryScreen = new ResultScreen(
            new Color(0.9f, 0.7f, 0.1f, 0.5f),
            "VICTORY",
            "You defended your base! All waves cleared!",
            ResultScreenMode.Victory
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

    private void OnSkillTreePressed()
    {
        if (_titleScreen != null)
            _titleScreen.Visible = false;

        if (_skillTreeScreen != null)
        {
            _skillTreeScreen.Refresh();
            _skillTreeScreen.Visible = true;
        }
    }

    private void OnSkillTreeBackPressed()
    {
        if (_skillTreeScreen != null)
            _skillTreeScreen.Visible = false;

        if (_titleScreen != null)
            _titleScreen.Visible = true;
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
        ClearTowerSelection();

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
            ExitTowerPlacementMode();
        }
        else
        {
            // Enter placement mode for the requested type
            _gameManager.PlacingTowerType = type;
            _gameHUD?.SetStartWaveEnabled(false); // Disable wave start during placement
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);

            // Placement wins over the upgrade panel: close it and drop the selection
            // (hiding any range that was showing).
            ClearTowerSelection();
        }
    }

    private void OnGameOverTriggered()
    {
        // The run can end while the player is aiming. Leave placement mode first so
        // the ghost/range preview and the Cross cursor cannot linger under the
        // result overlay.
        ExitTowerPlacementMode();

        // The upgrade panel must never linger over a result screen.
        ClearTowerSelection();

        if (_gameOverScreen != null)
            _gameOverScreen.Visible = true;
        _gameRunning = false;
    }

    private void OnVictoryTriggered()
    {
        // Same as game over: a victory landing mid-aim must not strand placement
        // state beneath the result overlay, and no upgrade control may survive play.
        ExitTowerPlacementMode();
        ClearTowerSelection();

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
        // Drop any per-level tower state before the level is torn down. Ranks, XP,
        // and archetypes are per tower instance and die with the towers.
        ClearTowerSelection();

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
            // Escape cancels the current placement selection.
            if (@event.IsActionPressed("ui_cancel"))
            {
                ExitTowerPlacementMode();
                GetViewport().SetInputAsHandled();
                return;
            }

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
                if (GameConstants.IsBattlefieldPoint(mouseButton.Position))
                {
                    TryPlaceTowerAtMouse(mouseButton.Position);
                }
                else
                {
                    // Clicking the side menu, HUD, or anywhere off the grid cancels
                    // placement. Consume the click so a sidebar button under the cursor
                    // cannot immediately start a new placement from the same event.
                    ExitTowerPlacementMode();
                    GetViewport().SetInputAsHandled();
                }
            }

            // Cancel placement with right click
            if (@event is InputEventMouseButton rightClick &&
                rightClick.ButtonIndex == MouseButton.Right &&
                rightClick.Pressed)
            {
                ExitTowerPlacementMode();
            }

            return;
        }

        // Escape closes the upgrade panel and drops the tower selection. Player input
        // is keyboard + mouse, so this is the always-available way out. The cheap
        // selection check runs first so mouse-motion events never pay for an InputMap
        // action lookup.
        if ((_selectedTower != null || (_upgradePanel?.IsOpen ?? false)) &&
            @event.IsActionPressed("ui_cancel"))
        {
            ClearTowerSelection();
            GetViewport().SetInputAsHandled();
            return;
        }

        // The upgrade panel consumes its own clicks. _Input runs before GUI input, so
        // a click on an upgrade action must be ignored here — otherwise it would also
        // deselect the tower, toggle its range, or start placement handling. It is
        // deliberately NOT marked handled: the button still needs the event.
        if (_upgradePanel != null &&
            @event is InputEventMouseButton panelClick &&
            panelClick.ButtonIndex == MouseButton.Left &&
            panelClick.Pressed &&
            _upgradePanel.ContainsPoint(panelClick.Position))
        {
            return;
        }

        // Handle tower selection / range display (not in placement mode). Only a
        // click on the battlefield can select a tower: the HUD top bar overlays
        // row 0, so a click there must not toggle a tower's range underneath it.
        if (@event is InputEventMouseButton click &&
            click.ButtonIndex == MouseButton.Left &&
            click.Pressed &&
            GameConstants.IsBattlefieldPoint(click.Position))
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
                    // Clicked the same tower again — toggle range off and close the
                    // panel with the selection.
                    ClearTowerSelection();
                }
                else
                {
                    // Clicked a different tower — hide previous, show new, and dock
                    // the upgrade panel to the new tower.
                    _selectedTower?.HideRange();
                    clickedTower.ShowRange();
                    _selectedTower = clickedTower;
                    OpenUpgradePanel(clickedTower);
                }
            }
            else
            {
                // Clicked on empty space — drop the selection and close the panel.
                ClearTowerSelection();
            }
        }
    }

    /// <summary>
    /// Docks the upgrade panel to the given tower. Only opens during active play, so
    /// no upgrade action can be surfaced after game over or victory.
    /// </summary>
    private void OpenUpgradePanel(Tower tower)
    {
        if (_gameManager == null || _gameManager.State != GameState.Playing)
            return;

        _upgradePanel?.ShowFor(tower, _gameManager.Coins);
    }

    /// <summary>
    /// Drops the selected tower: hides its range indicator and closes the upgrade
    /// panel. Idempotent and safe to call when nothing is selected.
    /// </summary>
    private void ClearTowerSelection()
    {
        if (_selectedTower != null && IsInstanceValid(_selectedTower))
            _selectedTower.HideRange();

        _selectedTower = null;
        _upgradePanel?.Close();
    }

    private void OnCoinsChangedForUpgradePanel(int coins)
    {
        _upgradePanel?.UpdateCoins(coins);
    }

    private void OnTowerUpgradeRequested()
    {
        if (_gameManager == null || _selectedTower == null)
            return;

        // The transaction revalidates game state, the XP gate, coins, and rank, so a
        // stale button press cannot bypass a gate. Each valid click buys exactly one
        // rank (a second click may legitimately buy the next rank if its gate is open).
        if (_gameManager.TryUpgradeTower(_selectedTower))
            _selectedTower.RefreshRangeIndicator();
        _upgradePanel?.Refresh();
    }

    private void OnTowerArchetypeChosen(int archetype)
    {
        if (_gameManager == null || _selectedTower == null)
            return;

        // Both cards firing in one frame still lands here twice; the second call is
        // rejected because the tower is already rank 3 with an archetype recorded.
        if (_gameManager.TryChooseArchetype(_selectedTower, (TowerArchetype)archetype))
            _selectedTower.RefreshRangeIndicator();
        _upgradePanel?.Refresh();
    }

    /// <summary>
    /// Update the tower placement preview (ghost + highlight + range) at the cell under the mouse.
    /// Hides the preview when the pointer is over HUD chrome or outside the grid.
    /// </summary>
    private void UpdateTowerPlacementPreview(Vector2 mousePos)
    {
        if (_gameManager?.Grid == null)
            return;

        if (_gameManager.PlacingTowerType == null)
            return;

        // Never draw the ghost over HUD chrome (top bar / sidebar) or off the grid.
        if (!GameConstants.IsBattlefieldPoint(mousePos))
        {
            _gameManager.Grid.HidePlacementPreview();
            return;
        }

        var type = _gameManager.PlacingTowerType.Value;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(mousePos);
        int row = gridPos.Y;
        int col = gridPos.X;

        if (_gameManager.Grid.IsInBounds(row, col))
        {
            bool enoughCoins = _gameManager.Coins >= GameConstants.TowerCost(type);
            bool canPlace = enoughCoins && _gameManager.Grid.CanPlaceTower(row, col);
            float rangeCells = SkillStats.RangeCells(type, _skillTree?.State);
            _gameManager.Grid.ShowPlacementPreview(row, col, canPlace, type, rangeCells);
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
            // Exactly one tower per selection: GameManager consumed the placement type,
            // so drop the preview, cursor, and Start Wave state to match.
            ExitTowerPlacementMode();
        }
        // A failed battlefield click (path or occupied cell) keeps the selection so the
        // player can retry on a valid cell.
    }

    /// <summary>
    /// Leave tower placement mode: drop the selection, hide the preview ghost, and
    /// restore the cursor and Start Wave button to normal play state. Idempotent and
    /// null-safe, so it is safe to call when placement already ended — including
    /// immediately after <see cref="GameManager.PlaceTower"/> consumed the selection,
    /// and from game-over/victory handlers.
    /// </summary>
    private void ExitTowerPlacementMode()
    {
        if (_gameManager != null)
            _gameManager.PlacingTowerType = null;

        _gameManager?.Grid?.HidePlacementPreview();
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        UpdateStartWaveButtonState();
    }
}
