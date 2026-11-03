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

    // Latest pointer position seen during active play, in viewport space. Lets a
    // placement selection draw its ghost immediately instead of waiting for the next
    // mouse-motion event. Null until the pointer first moves during play.
    private Vector2? _lastMouseViewportPosition = null;

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
        // Shift the whole game world below the HUD top bar. Every gameplay node
        // (grid, waves, enemies, towers, projectiles, previews, markers) is a child
        // of GameManager, so one offset positions them all.
        _gameManager.Position = new Vector2(0f, GameConstants.BattlefieldTop);
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

        // A new level starts with no known pointer position: a stale cell from the
        // previous level must not seed a placement ghost before the mouse moves here.
        _lastMouseViewportPosition = null;

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

        // Cancelling the active selection is always allowed: a balance that has since
        // dropped below the type's cost must never trap the player in placement mode.
        // Checked before the eligibility gate so a second card click or key press still
        // exits, matching the pre-shortcut behavior.
        if (_gameManager.PlacingTowerType == type)
        {
            ExitTowerPlacementMode();
            return;
        }

        // Sidebar cards and number-key shortcuts share this eligibility-gated transition.
        if (!CanSelectTowerForPlacement(type))
            return;

        // Enter placement mode for the requested type. A switch keeps the mode but
        // replaces the pending type. Either way, redraw the ghost at the last known
        // pointer position immediately: the player should not have to nudge the mouse
        // before the newly chosen type's preview appears.
        _gameManager.PlacingTowerType = type;
        RefreshTowerPlacementPreviewAtLastMouse();
        UpdateStartWaveButtonState();

        // Placement wins over the upgrade panel: close it and drop the selection
        // (hiding any range that was showing).
        ClearTowerSelection();
    }

    /// <summary>
    /// True when play is active, the selected level allows the tower type, and the
    /// current balance covers its full cost. This is the shared eligibility rule for
    /// sidebar-card clicks and number-key shortcut presses; it mirrors the HUD's card
    /// disabled state so keyboard and mouse always agree. Affordability uses
    /// <see cref="GameConstants.TowerCost"/>; availability uses the selected level.
    /// </summary>
    private bool CanSelectTowerForPlacement(TowerType type)
    {
        if (_gameManager == null || _gameManager.State != GameState.Playing)
            return false;

        bool allowedByLevel = type switch
        {
            TowerType.Cannon => _selectedLevel?.AllowCannonTower ?? false,
            TowerType.Laser => _selectedLevel?.AllowLaserTower ?? false,
            _ => true
        };

        return allowedByLevel && _gameManager.Coins >= GameConstants.TowerCost(type);
    }

    private void OnGameOverTriggered()
    {
        // The run can end while the player is aiming. Leave placement mode first so
        // the ghost/range preview cannot linger under the result overlay.
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

        // Track the pointer while play is active so a placement selection can draw its
        // ghost at the current cell immediately, without a fresh motion event.
        if (@event is InputEventMouseMotion latestMotion)
            _lastMouseViewportPosition = latestMotion.Position;

        // Space is the context-sensitive in-level control: while a wave runs it
        // toggles the whole-game clock, otherwise it starts the ready wave. Checked
        // before the placement branch so a running wave can still be sped up while
        // aiming. Every new Space press during play is consumed — even in a no-op
        // state (e.g. placement between waves) — so a focused Button can never also
        // fire ui_accept from the same physical press.
        if (IsSpacePress(@event))
        {
            OnSpacePressed();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Top-row number keys are the tower-placement shortcuts for the sidebar
        // cards. Checked before the placement branch so a digit also works while
        // aiming (same key cancels, another eligible key switches type). Every new
        // press during play is consumed — even a no-op for an unaffordable or
        // unavailable tower — so a focused Button can never receive the same event.
        if (TryGetTowerHotkey(@event, out TowerType hotkeyType))
        {
            OnPlaceTowerPressed((int)hotkeyType);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_gameManager.IsPlacingTower)
        {
            // Escape cancels the current placement selection.
            if (@event.IsActionPressed("ui_cancel"))
            {
                ExitTowerPlacementMode();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Track mouse for placement preview (shows ghost + highlight + range).
            // Preview and placement work in GameManager-local (battlefield) space.
            if (@event is InputEventMouseMotion mouseMotion)
            {
                UpdateTowerPlacementPreview(ToBattlefieldPoint(mouseMotion.Position));
            }

            // Handle tower placement via mouse click
            if (@event is InputEventMouseButton mouseButton &&
                mouseButton.ButtonIndex == MouseButton.Left &&
                mouseButton.Pressed)
            {
                Vector2 battlefieldPoint = ToBattlefieldPoint(mouseButton.Position);
                if (GameConstants.IsBattlefieldPoint(battlefieldPoint))
                {
                    TryPlaceTowerAtMouse(battlefieldPoint);
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
        // click on the battlefield can select a tower: the HUD top bar sits above
        // the grid, so a click there must not toggle a tower's range underneath it.
        if (@event is InputEventMouseButton click &&
            click.ButtonIndex == MouseButton.Left &&
            click.Pressed)
        {
            // Tower positions are GameManager-local, so the click must be converted
            // into the same space for both the bounds test and the rect hit test.
            Vector2 battlefieldPoint = ToBattlefieldPoint(click.Position);
            if (GameConstants.IsBattlefieldPoint(battlefieldPoint))
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
                    if (towerRect.HasPoint(battlefieldPoint))
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
    }

    /// <summary>
    /// True for a new key-down: key repeat and key release are inert. The keycode is
    /// returned so each caller can match its own key without re-reading the event.
    /// </summary>
    private static bool IsNewKeyPress(InputEvent @event, out Key keycode)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            keycode = key.Keycode;
            return true;
        }

        keycode = Key.None;
        return false;
    }

    /// <summary>
    /// True when the event is a new Space key press. The check is on the key itself
    /// rather than ui_accept so Enter and gamepad input never trigger the in-level
    /// control.
    /// </summary>
    private static bool IsSpacePress(InputEvent @event) =>
        IsNewKeyPress(@event, out Key keycode) && keycode == Key.Space;

    /// <summary>
    /// True for a new top-row 1/2/3 key press, mapped to its tower type (1 Arrow,
    /// 2 Cannon, 3 Laser — the same order as the sidebar cards). Only the main-row
    /// digits match: numpad digits and every other key are unaffected.
    /// </summary>
    private static bool TryGetTowerHotkey(InputEvent @event, out TowerType type)
    {
        type = TowerType.Arrow;

        if (!IsNewKeyPress(@event, out Key keycode))
            return false;

        switch (keycode)
        {
            case Key.Key1:
                type = TowerType.Arrow;
                return true;
            case Key.Key2:
                type = TowerType.Cannon;
                return true;
            case Key.Key3:
                type = TowerType.Laser;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// One context-sensitive action per Space press. Priority: a running wave toggles
    /// the clock (even in tower-placement mode); otherwise, with no wave running and
    /// not placing, the ready wave starts through the same path as the Start Wave
    /// button. A no-op state simply does nothing; the caller always consumes the press.
    /// </summary>
    private void OnSpacePressed()
    {
        if (_gameManager == null || _gameManager.State != GameState.Playing)
            return;

        // A running wave always takes the press; Space can never start a wave mid-wave.
        if (_gameManager.WaveManager?.IsWaveActive ?? false)
        {
            _gameManager.ToggleSpeedup();
            return;
        }

        // Start Wave is disabled while placing, so Space is inert between waves.
        if (_gameManager.IsPlacingTower)
            return;

        OnStartWavePressed();
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
    /// Converts a viewport-space (screen) point into the GameManager's local space, the
    /// space the battlefield, grid, and towers live in. The GameManager is offset below
    /// the HUD top bar, so this conversion is what excludes the top bar, the sidebar,
    /// and the margin below the grid from battlefield clicks. Null-safe: before the game
    /// manager exists the point is returned unchanged.
    /// </summary>
    private Vector2 ToBattlefieldPoint(Vector2 viewportPoint) =>
        _gameManager != null ? _gameManager.ToLocal(viewportPoint) : viewportPoint;

    /// <summary>
    /// Update the tower placement preview (ghost + highlight + range) at the cell under
    /// the given GameManager-local (battlefield) point. Hides the preview when the point
    /// is outside the grid — which, after Main converts viewport input, covers the HUD
    /// top bar, the sidebar, and the margin below the grid.
    /// </summary>
    private void UpdateTowerPlacementPreview(Vector2 battlefieldPoint)
    {
        if (_gameManager?.Grid == null)
            return;

        if (_gameManager.PlacingTowerType == null)
            return;

        if (!GameConstants.IsBattlefieldPoint(battlefieldPoint))
        {
            _gameManager.Grid.HidePlacementPreview();
            return;
        }

        var type = _gameManager.PlacingTowerType.Value;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(battlefieldPoint);
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

    /// <summary>
    /// Redraw the placement preview at the most recently seen pointer position. Called
    /// when a selection starts or switches so the ghost appears — or is redrawn for the
    /// newly selected type — immediately instead of on the next mouse-motion event.
    /// With no known position the preview stays hidden; an off-grid position hides it
    /// through the normal preview path.
    /// </summary>
    private void RefreshTowerPlacementPreviewAtLastMouse()
    {
        if (_lastMouseViewportPosition is Vector2 viewportPoint)
            UpdateTowerPlacementPreview(ToBattlefieldPoint(viewportPoint));
        else
            _gameManager?.Grid?.HidePlacementPreview();
    }

    private void TryPlaceTowerAtMouse(Vector2 battlefieldPoint)
    {
        if (_gameManager?.Grid == null)
            return;

        Vector2I gridPos = _gameManager.Grid.PixelToGrid(battlefieldPoint);

        bool placed = _gameManager.PlaceTower(gridPos.Y, gridPos.X);

        if (placed)
        {
            // Exactly one tower per selection: GameManager consumed the placement type,
            // so drop the preview and Start Wave state to match.
            ExitTowerPlacementMode();
        }
        // A failed battlefield click (path or occupied cell) keeps the selection so the
        // player can retry on a valid cell.
    }

    /// <summary>
    /// Leave tower placement mode: drop the selection, hide the preview ghost, and
    /// restore the Start Wave button to normal play state. Idempotent and null-safe, so
    /// it is safe to call when placement already ended — including immediately after
    /// <see cref="GameManager.PlaceTower"/> consumed the selection, and from
    /// game-over/victory handlers.
    /// </summary>
    private void ExitTowerPlacementMode()
    {
        if (_gameManager != null)
            _gameManager.PlacingTowerType = null;

        _gameManager?.Grid?.HidePlacementPreview();
        UpdateStartWaveButtonState();
    }
}
