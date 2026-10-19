extends SceneTree
##
## Headless layout verifier for the Geometry Tower Defense C# UI.
##
## Run:
##   godot --headless --path . --script res://tools/layout_verifier.gd
##
## The full verifier contract (assertion rule, coverage semantics, fail-closed
## behaviour, and exit codes) lives in tools/layout_verifier.md. The short version:
##
##   * Loads the real scene (res://src/Main.tscn) so the C# UI builds exactly as it
##     does in-game. It never instantiates C# classes directly; parameterless
##     constructors are intentionally absent.
##   * Sets the root viewport from project.godot on frame 1 (the headless root is
##     64x64 no matter what the project says) and verifies the assignment took.
##   * Waits for deferred container sorting before reading any rect.
##   * Resolves named visual groups by node identity, never by y-coordinate.
##   * Every assertion compares two independently-positioned things. An assertion
##     whose expected value is derived from the same bounding box it measures is a
##     bug in this file: `rowBBox.end.x - button.end.x` is zero by construction and
##     cannot fail.
##   * Emits one finding per violated assertion and reports full coverage: every
##     assertion evaluated and every one skipped, with the reason.
##   * Fails closed: scene build failure, unresolved group, wrong-class required node,
##     zero layout assertions, or a missing viewport setting can never read as a pass.
##   * Verification state is a debug-only, explicitly enabled handshake; the verifier
##     refuses to grant SP or press Buy unless SkillTreeScreen confirms isolation.
##   * A watchdog terminates uncaught-error/stalled runs with exit 2 instead of
##     leaving the headless process alive. Bound the external run too (Bash timeout).
##
## Exit codes: 0 = pass (zero findings, layout coverage > 0), 1 = layout findings,
## 2 = verifier failure (fail-closed precondition, wrong-class node, or watchdog).
##

const MAIN_SCENE := "res://src/Main.tscn"
const SKILL_TREE_SCRIPT := "res://src/UI/SkillTreeScreen.cs"
const TITLE_SCRIPT := "res://src/UI/TitleScreen.cs"
const RESULT_SCRIPT := "res://src/UI/ResultScreen.cs"
const GAME_HUD_SCRIPT := "res://src/UI/GameHUD.cs"

# Findings cite repo-relative paths so they match the review format.
const SKILL_TREE_SOURCE := "src/UI/SkillTreeScreen.cs"
const TITLE_SOURCE := "src/UI/TitleScreen.cs"
const RESULT_SOURCE := "src/UI/ResultScreen.cs"
const GAME_HUD_SOURCE := "src/UI/GameHUD.cs"

const VIEWPORT_WIDTH_SETTING := "display/window/size/viewport_width"
const VIEWPORT_HEIGHT_SETTING := "display/window/size/viewport_height"

## Rect comparisons are allowed this much slack. Godot lays controls out in
## floats; the values under test are whole pixels, so half a pixel is generous.
const EPS := 0.51

## Mirrored-row alignment expectations. These are stated from the spec's intent
## ("left-branch rows mirror right-branch rows about the panel centre"), not read
## back from the nodes being measured.
const ALIGN_LEFT := HORIZONTAL_ALIGNMENT_LEFT
const ALIGN_RIGHT := HORIZONTAL_ALIGNMENT_RIGHT
const ALIGN_CENTER := HORIZONTAL_ALIGNMENT_CENTER

## SkillTreeScreen layout intent, stated independently of the nodes it is measured
## against: a 24px viewport margin, a 120px tower sidebar, and a 10px panel inset.
## The sidebar panel assertion derives its expected rect from these constants rather
## than from Block_Sidebar, whose size the panel copies verbatim.
const SKTREE_MARGIN := 24.0
const SKTREE_SIDEBAR_WIDTH := 120.0
const SKTREE_PANEL_INSET := 10.0
const SKTREE_MAIN_TOP := 130.0
const SKTREE_SIDEBAR_BUTTON_HEIGHT := 60.0
const SKTREE_SIDEBAR_BUTTON_GAP := 16.0
const SKTREE_SIDEBAR_BUTTONS := 3

## Row content column, expressed from the independently measured icon centre rather
## than from the row wrapper. A Buy child shifted inside a correctly sized wrapper
## moves its rendered rect and must fail.
const ROW_BUY_OFFSET := 310.0
const ROW_BUY_WIDTH := 124.0

## Zero-state gating: with every rank at 0, only the first trunk node of each
## tower has no prerequisite. Every other node is locked behind rank 1 of its
## predecessor (later trunks) or of the tower's last trunk (seeds).
const EXPECTED_UNLOCKED_INDEX := 0

## Viewport dimensions are read from project.godot. A fractional value would
## truncate to an integer and could bypass the fail-closed viewport precondition,
## so only integral values in this range are accepted.
const VIEWPORT_DIM_MIN := 1
const VIEWPORT_DIM_MAX := 16384

## Hard stop for a run that stops making progress (for example an uncaught GDScript
## error aborts the measuring coroutine before it can call quit()). The external
## Bash-tool timeout is the second bound; a timed-out invocation is exit 2 too.
## Tests may shorten this via LAYOUT_VERIFIER_WATCHDOG_MS.
const WATCHDOG_DEFAULT_MS := 90000

## Catalog expected by the verifier, in display order. This is the independent
## side of the structural assertions: names/kinds/order come from the spec, while
## the measured side comes from the instantiated C# UI.
const TOWERS := [
    {
        "key": "arrow",
        "label": "Arrow",
        "nodes": [
            {"id": "arrow.damage", "name": "Damage", "kind": "trunk"},
            {"id": "arrow.attack_speed", "name": "Attack Speed", "kind": "trunk"},
            {"id": "arrow.range", "name": "Range", "kind": "trunk"},
            {"id": "arrow.pierce", "name": "Pierce", "kind": "seed"},
            {"id": "arrow.crit_chance", "name": "Crit Chance", "kind": "seed"},
        ],
    },
    {
        "key": "cannon",
        "label": "Cannon",
        "nodes": [
            {"id": "cannon.powder_charge", "name": "Powder Charge", "kind": "trunk"},
            {"id": "cannon.attack_speed", "name": "Attack Speed", "kind": "trunk"},
            {"id": "cannon.splash_radius", "name": "Splash Radius", "kind": "trunk"},
            {"id": "cannon.cluster", "name": "Cluster", "kind": "seed"},
            {"id": "cannon.stun_chance", "name": "Stun Chance", "kind": "seed"},
        ],
    },
    {
        "key": "laser",
        "label": "Laser",
        "nodes": [
            {"id": "laser.dps", "name": "DPS", "kind": "trunk"},
            {"id": "laser.range", "name": "Range", "kind": "trunk"},
            {"id": "laser.ignite", "name": "Ignite", "kind": "trunk"},
            {"id": "laser.chain", "name": "Chain", "kind": "seed"},
            {"id": "laser.ramp_up", "name": "Ramp-Up", "kind": "seed"},
        ],
    },
]

var _evaluated: Array = []
var _skipped: Array = []
var _findings: Array = []
var _layout_assertions := 0
var _fatal_reason := ""
var _finished := false
var _started_msec := 0
var _watchdog_ms := WATCHDOG_DEFAULT_MS
var _main: Node = null
var _viewport := Vector2i.ZERO


func _initialize() -> void:
    _started_msec = Time.get_ticks_msec()

    var watchdog_env := OS.get_environment("LAYOUT_VERIFIER_WATCHDOG_MS")
    if watchdog_env.is_valid_int():
        _watchdog_ms = clampi(int(watchdog_env), 1000, 600000)

    # Frames keep advancing after an uncaught GDScript error aborts the coroutine,
    # so a SceneTreeTimer still fires and can terminate with exit 2 instead of
    # leaving the headless process running forever.
    var watchdog := create_timer(float(_watchdog_ms) / 1000.0, true, false, true)
    watchdog.timeout.connect(_on_watchdog_timeout)

    _run.call_deferred()


## Belt-and-braces: when MainLoop calls this virtual, a stalled coroutine is also
## caught here. The timer above covers engines that do not dispatch it.
func _process(_delta: float) -> bool:
    if not _finished and Time.get_ticks_msec() - _started_msec > _watchdog_ms:
        _on_watchdog_timeout()
        return true
    return false


func _on_watchdog_timeout() -> void:
    _fail_closed(
        "watchdog fired after %d ms; uncaught error or stalled coroutine" % _watchdog_ms
    )


func _run() -> void:
    # ---- Fail-closed preconditions -------------------------------------------------

    # Read the project file itself, not ProjectSettings: ProjectSettings falls back to
    # engine defaults when a setting is absent, which would hide a missing viewport
    # configuration and let the verifier measure at the wrong size.
    var configured_viewport := _read_project_viewport()
    if configured_viewport.is_empty():
        _fail_closed("missing, non-integral, or out-of-range %s / %s in project.godot" % [
            VIEWPORT_WIDTH_SETTING, VIEWPORT_HEIGHT_SETTING,
        ])
        return

    _viewport = Vector2i(int(configured_viewport["width"]), int(configured_viewport["height"]))

    # The headless root viewport is 64x64 regardless of the project. Assigning in
    # _initialize is overwritten, so wait until frame 1 first.
    await process_frame
    get_root().size = _viewport
    await process_frame
    await process_frame

    var applied := get_root().size == _viewport
    _check(
        "precondition.viewport_applied",
        "root viewport",
        "root.size=%s visible_rect=%s" % [str(get_root().size), str(get_root().get_visible_rect())],
        "root.size=%s from project.godot" % str(_viewport),
        applied,
        "Assign get_root().size on frame >= 1 (in _initialize it is overwritten).",
        "tools/layout_verifier.gd",
        "CRITICAL",
        true
    )
    if not applied:
        _fail_closed("root viewport size did not take (root=%s, expected=%s); refusing to measure at the headless 64x64 size" % [
            str(get_root().size), str(_viewport),
        ])
        return

    print("LAYOUT VERIFIER - geometry-tower-defense")
    print("viewport: %dx%d (%s / %s)" % [
        _viewport.x, _viewport.y, VIEWPORT_WIDTH_SETTING, VIEWPORT_HEIGHT_SETTING,
    ])
    print("scene: %s" % MAIN_SCENE)
    print("")

    # ---- Build the real scene ------------------------------------------------------

    var packed = load(MAIN_SCENE)
    if packed == null or not (packed is PackedScene):
        _fail_closed("could not load %s as a PackedScene (run dotnet build first)" % MAIN_SCENE)
        return

    _main = packed.instantiate()
    if _main == null:
        _fail_closed("instantiating %s returned null" % MAIN_SCENE)
        return

    get_root().add_child(_main)
    await process_frame
    await process_frame

    if _main.get_child_count() == 0:
        _fail_closed("%s built no child nodes (C# assembly missing or scene script failed)" % MAIN_SCENE)
        return

    var title_screen := _find_by_script(_main, TITLE_SCRIPT)
    var skill_screen := _find_by_script(_main, SKILL_TREE_SCRIPT)
    var game_hud := _find_by_script(_main, GAME_HUD_SCRIPT)
    if title_screen == null or skill_screen == null or game_hud == null:
        _fail_closed("scene did not build the expected screens (TitleScreen=%s SkillTreeScreen=%s GameHUD=%s)" % [
            str(title_screen), str(skill_screen), str(game_hud),
        ])
        return

    _check(
        "precondition.scene_built",
        "res://src/Main.tscn",
        "children=%d TitleScreen=resolved SkillTreeScreen=resolved GameHUD=resolved" % _main.get_child_count(),
        "children>0 and all three screens present",
        true,
        "Build the C# assembly and load Main.tscn as a PackedScene.",
        "tools/layout_verifier.gd",
        "CRITICAL",
        true
    )

    # Deterministic state: the UI must be measured with known ranks, otherwise a
    # developer's local save changes which rows are locked. The hooks are debug-only
    # and require an explicit verification-mode handshake; the isolated tree never
    # flushes, so the player's save file is untouched. Refuse to continue unless the
    # screen confirms isolation BEFORE any SP is granted or Buy is pressed.
    for method in [
        "EnableVerificationMode",
        "ResetProgressForVerification",
        "GrantSkillPointsForVerification",
        "IsVerificationStateActive",
    ]:
        if not skill_screen.has_method(method):
            _fail_closed("SkillTreeScreen is missing %s(); cannot establish an isolated deterministic state" % method)
            return

    if not bool(skill_screen.call("EnableVerificationMode")):
        _fail_closed("SkillTreeScreen refused verification mode (a debug build is required); refusing to measure or mutate progression")
        return
    if not bool(skill_screen.call("ResetProgressForVerification")):
        _fail_closed("ResetProgressForVerification() did not install the isolated in-memory tree")
        return
    if not bool(skill_screen.call("IsVerificationStateActive")):
        _fail_closed("SkillTreeScreen reports verification isolation inactive after reset; refusing to grant SP or press Buy")
        return

    skill_screen.set("visible", true)
    await process_frame
    await process_frame

    # ---- Title screen assertions ---------------------------------------------------

    _evaluate_title_screen(title_screen)

    # ---- Skill tree assertions -----------------------------------------------------

    await _evaluate_skill_tree_screen(skill_screen)

    # A fail-closed path inside the skill-tree evaluation (for example isolation lost
    # before a purchase) already requested exit 2; do not keep measuring.
    if _finished:
        return

    # ---- Static HUD layout (built at startup; dynamic values are skipped below) ----

    _evaluate_game_hud(game_hud)

    # ---- Result overlays (static layout is built at startup) -----------------------

    var result_screens := _find_all_by_script(_main, RESULT_SCRIPT)
    if result_screens.is_empty():
        # A screen family that resolves to zero nodes must never silently vanish from
        # coverage: the run would exit 0 having measured nothing for it.
        _fail_closed("scene did not build any ResultScreen overlay; result layout cannot be verified")
        return
    for result_screen in result_screens:
        _evaluate_result_screen(result_screen)

    # ---- Screens that cannot be produced at startup --------------------------------

    _skipped.append({
        "id": "hud.mid_wave_layout",
        "reason": "HUD mid-wave state (live HP/coins/wave values, level-gated tower buttons) requires gameplay; static HUD layout is measured, dynamic state is not",
    })
    _skipped.append({
        "id": "victory.real_score_layout",
        "reason": "Victory with a real score requires gameplay; not reachable by instantiating Main.tscn",
    })

    _finish()


# ---------------------------------------------------------------------------------
# Scene / node helpers
# ---------------------------------------------------------------------------------

func _evaluate_title_screen(title_screen: Node) -> void:
    var title_block := _require(title_screen, "Block_Title", "TitleScreen", TITLE_SOURCE)
    var menu_block := _require(title_screen, "Block_LevelMenu", "TitleScreen", TITLE_SOURCE)
    var instructions_block := _require(title_screen, "Block_Instructions", "TitleScreen", TITLE_SOURCE)
    var viewport_rect := _viewport_rect()

    if title_block != null:
        var rect: Rect2 = title_block.get_global_rect()
        _check(
            "title.block_width",
            "TitleScreen/Block_Title",
            "width=%.2f" % rect.size.x,
            "viewport width=%.2f" % float(_viewport.x),
            _approx(rect.size.x, float(_viewport.x)),
            "Give the title block the viewport width so centered labels can center on the viewport.",
            TITLE_SOURCE
        )
        _check_centered_on_viewport(
            "title.block_centered", "TitleScreen/Block_Title", rect,
            "Center the title block on the viewport; do not offset it from a panel edge.",
            TITLE_SOURCE
        )
        _check_inside(
            "title.block_inside_viewport", "TitleScreen/Block_Title", rect, viewport_rect,
            "Keep the title block inside the viewport.", TITLE_SOURCE
        )

    if instructions_block != null:
        var rect: Rect2 = instructions_block.get_global_rect()
        _check_inside(
            "title.instructions_inside_viewport", "TitleScreen/Block_Instructions",
            rect, viewport_rect,
            "Keep the instructions block inside the viewport.", TITLE_SOURCE
        )

    if menu_block == null:
        return

    var menu_rect: Rect2 = menu_block.get_global_rect()
    var previous: Rect2 = Rect2()
    var has_previous := false
    for button_name in ["Level1", "Level2", "Level3", "Level4", "SkillTree"]:
        var button := _require(menu_block, button_name, "TitleScreen/Block_LevelMenu", TITLE_SOURCE)
        if button == null:
            continue

        var rect: Rect2 = button.get_global_rect()
        var group := "TitleScreen/Block_LevelMenu/%s" % button_name
        _check_centered_on_viewport(
            "title.menu.%s.centered" % button_name, group, rect,
            "Center each menu button on the viewport's vertical axis.",
            TITLE_SOURCE, "WARNING"
        )
        _check_inside(
            "title.menu.%s.inside_block" % button_name, group, rect, menu_rect,
            "Lay the menu buttons out inside their named menu group.",
            TITLE_SOURCE, "WARNING", 1.0
        )

        if has_previous:
            _check_stack_gap(
                "title.menu.%s.stack_gap" % button_name, group, rect, previous,
                "Stack menu buttons without overlapping and with a uniform height.",
                TITLE_SOURCE
            )
        previous = rect
        has_previous = true


func _evaluate_result_screen(result_screen: Node) -> void:
    var screen_name := String(result_screen.name)
    var title_block := _require(result_screen, "Block_Title", screen_name, RESULT_SOURCE)
    var return_button := _require(result_screen, "ReturnToTitle", screen_name, RESULT_SOURCE)
    var viewport_rect := _viewport_rect()

    if title_block != null:
        var rect: Rect2 = title_block.get_global_rect()
        _check_centered_on_viewport(
            "result.%s.title_centered" % screen_name, "%s/Block_Title" % screen_name, rect,
            "Center the result title block on the viewport.", RESULT_SOURCE, "WARNING"
        )
        _check_inside(
            "result.%s.title_inside_viewport" % screen_name, "%s/Block_Title" % screen_name,
            rect, viewport_rect,
            "Keep the result title block inside the viewport.", RESULT_SOURCE, "WARNING"
        )

    if return_button != null:
        var rect: Rect2 = return_button.get_global_rect()
        _check_inside(
            "result.%s.return_inside_viewport" % screen_name, "%s/ReturnToTitle" % screen_name,
            rect, viewport_rect,
            "Keep the return button inside the viewport.", RESULT_SOURCE, "WARNING"
        )


func _evaluate_game_hud(game_hud: Node) -> void:
    var top_bar := _require(game_hud, "Block_TopBar", "GameHUD", GAME_HUD_SOURCE)
    var sidebar_block := _require(game_hud, "Block_Sidebar", "GameHUD", GAME_HUD_SOURCE)
    var viewport_rect := _viewport_rect()

    if top_bar != null:
        var rect: Rect2 = top_bar.get_global_rect()
        _check(
            "hud.top_bar_width",
            "GameHUD/Block_TopBar",
            "width=%.2f" % rect.size.x,
            "viewport width=%.2f" % float(_viewport.x),
            _approx(rect.size.x, float(_viewport.x)),
            "Span the top status bar across the viewport width.",
            GAME_HUD_SOURCE
        )
        _check_inside(
            "hud.top_bar_inside_viewport", "GameHUD/Block_TopBar", rect, viewport_rect,
            "Keep the top status bar inside the viewport.", GAME_HUD_SOURCE
        )

    if sidebar_block != null:
        var sidebar_rect: Rect2 = sidebar_block.get_global_rect()
        _check(
            "hud.sidebar_anchored_right",
            "GameHUD/Block_Sidebar",
            "left=%.2f right=%.2f" % [sidebar_rect.position.x, sidebar_rect.end.x],
            "right=viewport width (%.2f) and left > viewport center (%.2f)" % [
                float(_viewport.x), _viewport_center_x(),
            ],
            _approx(sidebar_rect.end.x, float(_viewport.x))
                and sidebar_rect.position.x > _viewport_center_x(),
            "Anchor the HUD sidebar to the viewport's right edge, outside the play area.",
            GAME_HUD_SOURCE
        )
        _check_inside(
            "hud.sidebar_inside_viewport", "GameHUD/Block_Sidebar", sidebar_rect, viewport_rect,
            "Keep the HUD sidebar inside the viewport.", GAME_HUD_SOURCE
        )

        for button_name in ["PlaceArrow", "PlaceCannon", "PlaceLaser", "StartWave"]:
            var button := _require_button(
                sidebar_block, button_name, "GameHUD/Block_Sidebar", GAME_HUD_SOURCE
            )
            if button == null:
                continue
            _check_inside(
                "hud.%s.inside_sidebar" % button_name.to_lower(),
                "GameHUD/Block_Sidebar/%s" % button_name,
                button.get_global_rect(), sidebar_rect,
                "Lay the HUD action buttons out inside their named sidebar group.",
                GAME_HUD_SOURCE, "WARNING"
            )


func _evaluate_skill_tree_screen(skill_screen: Node) -> void:
    var title_block := _require(skill_screen, "Block_Title", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var sp_label := _require_label(skill_screen, "Block_Title/SkillPoints", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var back_button := _require_button(skill_screen, "Back", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var main_panel := _require(skill_screen, "Panel_Main", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var sidebar_block := _require(skill_screen, "Block_Sidebar", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var sidebar_panel := _require(skill_screen, "Block_Sidebar/Panel", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var tree_area := _require(skill_screen, "TreeArea", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var header_block := _require(skill_screen, "TreeArea/Block_TreeHeader", "SkillTreeScreen", SKILL_TREE_SOURCE)
    var header_label: Label = null
    if header_block != null:
        header_label = _require_label(
            header_block, "Title", "SkillTreeScreen/TreeArea/Block_TreeHeader", SKILL_TREE_SOURCE
        )
    var viewport_rect := _viewport_rect()

    if title_block != null:
        var rect: Rect2 = title_block.get_global_rect()
        _check_centered_on_viewport(
            "skilltree.title_block_centered", "SkillTreeScreen/Block_Title", rect,
            "Center the title block on the viewport.", SKILL_TREE_SOURCE, "WARNING"
        )

    if sp_label != null:
        _check(
            "skilltree.skill_points_zero_state",
            "SkillTreeScreen/Block_Title/SkillPoints",
            "text=%s" % String(sp_label.text),
            "text=Skill Points: 0 (zeroed in-memory state)",
            String(sp_label.text) == "Skill Points: 0",
            "Render the live Skill Point balance in the SP label.",
            SKILL_TREE_SOURCE,
            "WARNING"
        )

    if back_button != null:
        var rect: Rect2 = back_button.get_global_rect()
        _check_inside(
            "skilltree.back_inside_viewport", "SkillTreeScreen/Back", rect, viewport_rect,
            "Keep the back button inside the viewport.", SKILL_TREE_SOURCE, "WARNING"
        )

    if main_panel != null:
        var rect: Rect2 = main_panel.get_global_rect()
        _check_inside(
            "skilltree.main_panel_inside_viewport", "SkillTreeScreen/Panel_Main",
            rect, viewport_rect,
            "Keep the main tree panel inside the viewport.", SKILL_TREE_SOURCE
        )

    if sidebar_block != null:
        var rect: Rect2 = sidebar_block.get_global_rect()
        _check(
            "skilltree.sidebar_anchored_right",
            "SkillTreeScreen/Block_Sidebar",
            "left=%.2f right=%.2f" % [rect.position.x, rect.end.x],
            "left > viewport center (%.2f) and right within %.2f..%.2f" % [
                _viewport_center_x(), float(_viewport.x) - 40.0, float(_viewport.x),
            ],
            rect.position.x > _viewport_center_x()
                and rect.end.x >= float(_viewport.x) - 40.0
                and rect.end.x <= float(_viewport.x) + EPS,
            "Anchor the tower sidebar to the viewport's right edge, never the left.",
            SKILL_TREE_SOURCE
        )
        _check_inside(
            "skilltree.sidebar_inside_viewport", "SkillTreeScreen/Block_Sidebar",
            rect, viewport_rect,
            "Keep the sidebar inside the viewport.", SKILL_TREE_SOURCE
        )

    if main_panel != null and sidebar_block != null:
        var main_rect: Rect2 = main_panel.get_global_rect()
        var sidebar_rect: Rect2 = sidebar_block.get_global_rect()
        _check(
            "skilltree.panels_do_not_overlap",
            "SkillTreeScreen/Panel_Main vs Block_Sidebar",
            "main.right=%.2f sidebar.left=%.2f" % [main_rect.end.x, sidebar_rect.position.x],
            "main.right < sidebar.left",
            main_rect.end.x < sidebar_rect.position.x,
            "Keep the tree panel and sidebar as separate columns; the pre-fix layout overlapped three panels.",
            SKILL_TREE_SOURCE
        )

    if sidebar_panel != null:
        # Expected rect is derived from the screen's stated layout intent (viewport
        # edge minus margin/sidebar/inset), never from Block_Sidebar. The panel is
        # sized by copying the wrapper, so comparing the two is algebraic.
        var panel_rect: Rect2 = sidebar_panel.get_global_rect()
        var expected_panel := Rect2(
            float(_viewport.x) - SKTREE_MARGIN - SKTREE_SIDEBAR_WIDTH - SKTREE_PANEL_INSET,
            SKTREE_MAIN_TOP - SKTREE_PANEL_INSET,
            SKTREE_SIDEBAR_WIDTH + SKTREE_PANEL_INSET * 2.0,
            SKTREE_SIDEBAR_BUTTONS * SKTREE_SIDEBAR_BUTTON_HEIGHT
                + (SKTREE_SIDEBAR_BUTTONS - 1) * SKTREE_SIDEBAR_BUTTON_GAP
                + SKTREE_PANEL_INSET * 2.0
        )
        _check(
            "skilltree.sidebar_panel_matches_block",
            "SkillTreeScreen/Block_Sidebar/Panel",
            str(panel_rect),
            "reserved sidebar rect %s (viewport edge minus margin/sidebar/inset)" % str(expected_panel),
            _approx(panel_rect.position.x, expected_panel.position.x)
                and _approx(panel_rect.position.y, expected_panel.position.y)
                and _approx(panel_rect.size.x, expected_panel.size.x)
                and _approx(panel_rect.size.y, expected_panel.size.y),
            "Size the sidebar panel to the reserved layout rect derived from viewport/margin constants, not to its own wrapper.",
            SKILL_TREE_SOURCE,
            "WARNING"
        )

    if tree_area != null and main_panel != null:
        var tree_rect: Rect2 = tree_area.get_global_rect()
        var main_rect: Rect2 = main_panel.get_global_rect()
        _check_inside(
            "skilltree.tree_area_inside_main_panel", "SkillTreeScreen/TreeArea",
            tree_rect, main_rect,
            "Keep the tree area inside the main tree panel.", SKILL_TREE_SOURCE
        )
        _check(
            "skilltree.tree_area_nonempty",
            "SkillTreeScreen/TreeArea",
            "children=%d" % tree_area.get_child_count(),
            "children>=3 (line layer, header, rows) after deferred sorting",
            tree_area.get_child_count() >= 3,
            "Wait for deferred container sorting before measuring; a stale tree reads as empty.",
            SKILL_TREE_SOURCE
        )

    if header_block != null and tree_area != null:
        var header_rect: Rect2 = header_block.get_global_rect()
        var tree_rect: Rect2 = tree_area.get_global_rect()
        _check_inside(
            "skilltree.tree_header_inside_tree", "SkillTreeScreen/TreeArea/Block_TreeHeader",
            header_rect, tree_rect,
            "Keep the tower header inside the tree area.", SKILL_TREE_SOURCE, "WARNING"
        )
        if header_label != null:
            # Measure the rendered title label, and assert its alignment. The wrapper's
            # width is copied from the tree area, so comparing the wrapper centre to the
            # tree centre is algebraic and cannot catch a width/inset defect.
            var label_rect: Rect2 = header_label.get_global_rect()
            _check(
                "skilltree.tree_header_centered",
                "SkillTreeScreen/TreeArea/Block_TreeHeader/Title",
                "label center.x=%.2f" % label_rect.get_center().x,
                "tree center.x=%.2f" % tree_rect.get_center().x,
                _approx(label_rect.get_center().x, tree_rect.get_center().x),
                "Center the rendered tower title on the tree area, not merely its wrapper.",
                SKILL_TREE_SOURCE,
                "WARNING"
            )
            _check(
                "skilltree.tree_header_alignment",
                "SkillTreeScreen/TreeArea/Block_TreeHeader/Title",
                "horizontal_alignment=%d" % header_label.horizontal_alignment,
                "horizontal_alignment=%d (center)" % ALIGN_CENTER,
                header_label.horizontal_alignment == ALIGN_CENTER,
                "Set HorizontalAlignment.Center on the tower title label.",
                SKILL_TREE_SOURCE,
                "WARNING"
            )

    # Sidebar buttons: resolve before switching, because the per-tower scenarios
    # press them to rebuild the tree through the real C# signal path.
    var sidebar_buttons := {}
    if sidebar_block != null:
        var previous: Rect2 = Rect2()
        var has_previous := false
        for tower in TOWERS:
            var key: String = tower["key"]
            var button := _require(
                sidebar_block, "Sidebar_%s" % key, "SkillTreeScreen/Block_Sidebar", SKILL_TREE_SOURCE
            )
            if button == null:
                continue
            sidebar_buttons[key] = button

            var rect: Rect2 = button.get_global_rect()
            var group := "SkillTreeScreen/Block_Sidebar/Sidebar_%s" % key
            # A missing label is an unresolved named group, not a silent skip.
            var label := _require_label(button, "Label", group, SKILL_TREE_SOURCE)
            if label != null:
                _check(
                    "skilltree.sidebar.%s.label" % key,
                    "%s/Label" % group,
                    "text=%s" % String(label.text),
                    "text=%s" % String(tower["label"]),
                    String(label.text) == String(tower["label"]),
                    "Label each tower selector with the tower's name.",
                    SKILL_TREE_SOURCE,
                    "WARNING"
                )
            if has_previous:
                _check_stack_gap(
                    "skilltree.sidebar.%s.stack" % key, group, rect, previous,
                    "Stack tower selectors without overlap and with a uniform height.",
                    SKILL_TREE_SOURCE
                )
            previous = rect
            has_previous = true

    if tree_area == null or sidebar_buttons.size() != TOWERS.size():
        return

    # Evaluate all three towers by driving the real selection buttons. Switching
    # also exercises ClearTreeArea + RenderTree, so a rebuild that drops or
    # duplicates rows is caught by the per-tower row-set assertion.
    for tower in TOWERS:
        var key: String = tower["key"]
        if key != "arrow":
            sidebar_buttons[key].emit_signal("pressed")
            await process_frame
            await process_frame
        _evaluate_tower_tree(key, tower["nodes"], tree_area)

    # Leave the screen on Arrow, then drive a real purchase through the Buy button.
    # This exercises the signal -> BuyRank -> RenderTree -> ClearTreeArea path, including
    # freeing the button that is currently emitting, without persisting anything.
    sidebar_buttons["arrow"].emit_signal("pressed")
    await process_frame
    await process_frame
    await _evaluate_purchase_rebuild(skill_screen, tree_area, sp_label)


func _evaluate_purchase_rebuild(skill_screen: Node, tree_area: Control, sp_label: Label) -> void:
    if not skill_screen.has_method("GrantSkillPointsForVerification"):
        _fail_closed("SkillTreeScreen is missing GrantSkillPointsForVerification(); cannot exercise the purchase path")
        return

    # Isolation must be confirmed immediately before any progression mutation. A
    # missing state query is a verifier failure, never an implied pass.
    if not _verification_is_active(skill_screen):
        _fail_closed("verification isolation is not active before granting SP; refusing to mutate progression")
        return
    if not bool(skill_screen.call("GrantSkillPointsForVerification", 10)):
        _fail_closed("GrantSkillPointsForVerification(10) refused: the isolated verification tree is not active")
        return
    await process_frame

    if sp_label == null:
        _check(
            "skilltree.purchase.grant_reflected",
            "SkillTreeScreen/Block_Title/SkillPoints",
            "SP label not found",
            "text=Skill Points: 10",
            false,
            "Render the live Skill Point balance in the SP label.",
            SKILL_TREE_SOURCE
        )
        return

    _check(
        "skilltree.purchase.grant_reflected",
        "SkillTreeScreen/Block_Title/SkillPoints",
        "text=%s" % String(sp_label.text),
        "text=Skill Points: 10 (granted in-memory)",
        String(sp_label.text) == "Skill Points: 10",
        "Refresh the SP label when Skill Points change.",
        SKILL_TREE_SOURCE,
        "WARNING"
    )

    var damage_row := _require(tree_area, "Row_arrow_damage", "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE)
    var buy_button := _buy_in_row(
        damage_row, "SkillTreeScreen/TreeArea/Row_arrow_damage", SKILL_TREE_SOURCE
    )
    if buy_button == null:
        _check(
            "skilltree.purchase.buy_present",
            "SkillTreeScreen/TreeArea/Row_arrow_damage/Buy",
            "buy button not found",
            "buy button present on the first unlocked trunk node",
            false,
            "Render the buy button for an unlocked, affordable node.",
            SKILL_TREE_SOURCE
        )
        return

    if not _verification_is_active(skill_screen):
        _fail_closed("verification isolation is not active before pressing Buy; refusing to mutate progression")
        return

    buy_button.emit_signal("pressed")

    # The purchase rebuild frees the row that owns the button currently emitting.
    # ClearTreeArea must defer that free (RemoveChild + QueueFree); freeing the emitter
    # synchronously is the historical "signal-lock free" crash. Checked immediately
    # after the signal returns, while a deferred free has not run yet.
    var emitter_valid_after_signal := is_instance_valid(buy_button)

    await process_frame
    await process_frame

    _check(
        "skilltree.purchase.emitter_freed_deferred",
        "SkillTreeScreen/TreeArea/Row_arrow_damage/Buy",
        "emitter valid immediately after pressed signal=%s" % str(emitter_valid_after_signal),
        "emitter valid=true (free must be deferred with QueueFree)",
        emitter_valid_after_signal,
        "Use RemoveChild + QueueFree in ClearTreeArea; never Free a node from inside its own signal emission.",
        SKILL_TREE_SOURCE
    )

    var damaged_row := _require(tree_area, "Row_arrow_damage", "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE)
    var rank_label: Label = null
    var trunk_icon: Control = null
    if damaged_row != null:
        rank_label = _require_label(
            damaged_row, "Rank", "SkillTreeScreen/TreeArea/Row_arrow_damage", SKILL_TREE_SOURCE
        )
        trunk_icon = _require(
            damaged_row, "Icon", "SkillTreeScreen/TreeArea/Row_arrow_damage", SKILL_TREE_SOURCE
        )
    var next_row := _require(
        tree_area, "Row_arrow_attack_speed", "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE
    )
    var next_buy := _buy_in_row(
        next_row, "SkillTreeScreen/TreeArea/Row_arrow_attack_speed", SKILL_TREE_SOURCE
    )

    _check(
        "skilltree.purchase.sp_spent",
        "SkillTreeScreen/Block_Title/SkillPoints",
        "text=%s" % String(sp_label.text),
        "text=Skill Points: 0 (10 granted, 10 spent)",
        String(sp_label.text) == "Skill Points: 0",
        "Apply the rank cost and refresh the SP label after a purchase.",
        SKILL_TREE_SOURCE,
        "WARNING"
    )
    _check(
        "skilltree.purchase.rank_updated",
        "SkillTreeScreen/TreeArea/Row_arrow_damage/Rank",
        "text=%s" % (String(rank_label.text) if rank_label != null else "<missing>"),
        "text=1/5",
        rank_label != null and String(rank_label.text) == "1/5",
        "Increment the rank label after a successful purchase.",
        SKILL_TREE_SOURCE,
        "WARNING"
    )
    _check(
        "skilltree.purchase.unlocks_next",
        "SkillTreeScreen/TreeArea/Row_arrow_attack_speed",
        "buy_button=%s" % str(next_buy != null),
        "buy_button=true (prerequisite reached rank 1)",
        next_buy != null,
        "Rebuild the tree after a purchase so newly unlocked nodes gain their buy button.",
        SKILL_TREE_SOURCE
    )
    if trunk_icon != null:
        var tree_rect: Rect2 = tree_area.get_global_rect()
        var icon_center: float = trunk_icon.get_global_rect().get_center().x
        _check(
            "skilltree.purchase.tree_still_centered",
            "SkillTreeScreen/TreeArea",
            "trunk icon center=%.2f" % icon_center,
            "tree area center.x=%.2f" % tree_rect.get_center().x,
            _approx(icon_center, tree_rect.get_center().x),
            "Keep the tree centered across rebuilds; recompute anchors on every RenderTree.",
            SKILL_TREE_SOURCE
        )

    # ----- Seed Buy coverage --------------------------------------------------------
    # At the zero state the seeds are locked, so they render no Buy child. Drive the
    # remaining trunk purchases (on the isolated tree) to unlock both seeds, then
    # measure the rendered seed Buy rects. Without this state a Buy shifted inside a
    # seed row is unobservable.
    if _verification_is_active(skill_screen):
        await _evaluate_seed_buy_layout(skill_screen, tree_area)


func _evaluate_seed_buy_layout(skill_screen: Node, tree_area: Control) -> void:
    if not bool(skill_screen.call("GrantSkillPointsForVerification", 20)):
        _fail_closed("verification grant refused while unlocking the seed Buy children")
        return
    await process_frame

    # Buy the remaining trunks so the tower's last trunk reaches rank 1, which is the
    # seed prerequisite.
    for node_id in ["arrow.attack_speed", "arrow.range"]:
        var row := _require(tree_area, _row_name(node_id), "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE)
        var group := "SkillTreeScreen/TreeArea/%s" % _row_name(node_id)
        var button := _buy_in_row(row, group, SKILL_TREE_SOURCE)
        if button == null:
            _check(
                "skilltree.purchase.seed_unlock_%s" % node_id.replace(".", "_"),
                group,
                "buy button not found (row resolved=%s)" % str(row != null),
                "buy button present while unlocking the last trunk",
                false,
                "Rebuild the tree after each purchase so the next trunk node gains its buy button.",
                SKILL_TREE_SOURCE
            )
            return
        if not _verification_is_active(skill_screen):
            _fail_closed("verification isolation lost while unlocking the seed Buy children")
            return
        button.emit_signal("pressed")
        await process_frame
        await process_frame

    var tree_rect: Rect2 = tree_area.get_global_rect()
    var spine_x: float = tree_rect.get_center().x
    var left_row := _require(tree_area, "Row_arrow_pierce", "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE)
    var right_row := _require(tree_area, "Row_arrow_crit_chance", "SkillTreeScreen/TreeArea", SKILL_TREE_SOURCE)
    if left_row == null or right_row == null:
        return

    var left_icon := _require(
        left_row, "Icon", "SkillTreeScreen/TreeArea/Row_arrow_pierce", SKILL_TREE_SOURCE
    )
    var right_icon := _require(
        right_row, "Icon", "SkillTreeScreen/TreeArea/Row_arrow_crit_chance", SKILL_TREE_SOURCE
    )
    var left_buy := _buy_in_row(
        left_row, "SkillTreeScreen/TreeArea/Row_arrow_pierce", SKILL_TREE_SOURCE
    )
    var right_buy := _buy_in_row(
        right_row, "SkillTreeScreen/TreeArea/Row_arrow_crit_chance", SKILL_TREE_SOURCE
    )

    var rendered: bool = left_buy != null and right_buy != null
    _check(
        "skilltree.purchase.seed_buys_rendered",
        "SkillTreeScreen/TreeArea/Row_arrow_pierce/Buy vs Row_arrow_crit_chance/Buy",
        "left=%s right=%s" % [str(left_buy != null), str(right_buy != null)],
        "both seed Buy buttons render once the last trunk reaches rank 1",
        rendered,
        "Rebuild the tree after each purchase; a rank-1 prerequisite must unlock both seed buy buttons.",
        SKILL_TREE_SOURCE
    )
    if not rendered or left_icon == null or right_icon == null:
        return

    var left_buy_rect: Rect2 = left_buy.get_global_rect()
    var right_buy_rect: Rect2 = right_buy.get_global_rect()
    var left_icon_center: float = left_icon.get_global_rect().get_center().x
    var right_icon_center: float = right_icon.get_global_rect().get_center().x

    _check(
        "skilltree.purchase.seed_buys_mirror",
        "SkillTreeScreen/TreeArea/Row_arrow_pierce/Buy vs Row_arrow_crit_chance/Buy",
        "left=[%.2f..%.2f] right=[%.2f..%.2f] spine=%.2f" % [
            left_buy_rect.position.x, left_buy_rect.end.x,
            right_buy_rect.position.x, right_buy_rect.end.x, spine_x,
        ],
        "seed Buy rects mirror about the spine",
        _rects_mirror_about(left_buy_rect, right_buy_rect, spine_x),
        "Mirror seed Buy columns about the tree-area centre; do not shift a button inside its row.",
        SKILL_TREE_SOURCE
    )

    var expected_left: float = left_icon_center - ROW_BUY_OFFSET - ROW_BUY_WIDTH
    var expected_right: float = right_icon_center + ROW_BUY_OFFSET
    _check(
        "skilltree.purchase.seed_buy_columns",
        "SkillTreeScreen/TreeArea/Row_arrow_pierce/Buy vs Row_arrow_crit_chance/Buy",
        "left=%.2f right=%.2f icons=(%.2f, %.2f) widths=%.2f/%.2f" % [
            left_buy_rect.position.x, right_buy_rect.position.x,
            left_icon_center, right_icon_center,
            left_buy_rect.size.x, right_buy_rect.size.x,
        ],
        "left=%.2f right=%.2f width=%.2f (icon centre plus content-column offset)" % [
            expected_left, expected_right, ROW_BUY_WIDTH,
        ],
        _approx(left_buy_rect.position.x, expected_left)
            and _approx(right_buy_rect.position.x, expected_right)
            and _approx(left_buy_rect.size.x, ROW_BUY_WIDTH)
            and _approx(right_buy_rect.size.x, ROW_BUY_WIDTH),
        "Place each seed Buy in its content column measured from the node anchor.",
        SKILL_TREE_SOURCE
    )


func _evaluate_tower_tree(key: String, nodes: Array, tree_area: Control) -> void:
    var screen_path := "SkillTreeScreen/TreeArea"

    # ----- Row identity (never y-coordinate grouping) -----------------------------
    var expected_rows := []
    for node in nodes:
        expected_rows.append(_row_name(node["id"]))
    expected_rows.sort()

    var actual_rows := _row_names(tree_area)
    _check(
        "tree.%s.rows" % key,
        screen_path,
        "rows=%s" % str(actual_rows),
        "rows=%s" % str(expected_rows),
        actual_rows == expected_rows,
        "Build exactly one named Row_<id> wrapper per catalog node for the selected tower.",
        SKILL_TREE_SOURCE
    )

    var tree_rect: Rect2 = tree_area.get_global_rect()
    var spine_x: float = tree_rect.get_center().x
    var rows := {}
    for node in nodes:
        # Unresolved group: fail, never skip. The named-group convention was not
        # followed. _require also type-checks the resolved node.
        var row := _require(tree_area, _row_name(node["id"]), screen_path, SKILL_TREE_SOURCE)
        if row == null:
            continue
        rows[node["id"]] = row

    var trunk_icon_centers := []
    var trunk_content_ok := true
    var trunk_content_measured := []
    var seed_left: Control = null
    var seed_right: Control = null
    var seed_left_icon: Control = null
    var seed_right_icon: Control = null

    for i in range(nodes.size()):
        var node: Dictionary = nodes[i]
        var row: Node = rows.get(node["id"])
        if row == null:
            continue

        var row_path := "%s/%s" % [screen_path, String(row.name)]
        var expected_buy: bool = i == EXPECTED_UNLOCKED_INDEX
        var expected_children := ["Icon", "Name", "Rank"]
        if expected_buy:
            expected_children.append("Buy")
        expected_children.sort()

        var actual_children := _child_names(row)
        _check(
            "tree.%s.children" % node["id"],
            row_path,
            "children=%s" % str(actual_children),
            "children=%s (one row per catalog node; no pips or coming-soon labels)" % str(expected_children),
            actual_children == expected_children,
            "Keep each named row to Icon/Name/Rank plus Buy only on unlocked nodes.",
            SKILL_TREE_SOURCE
        )

        var name_label := _require_label(row, "Name", row_path, SKILL_TREE_SOURCE)
        var rank_label := _require_label(row, "Rank", row_path, SKILL_TREE_SOURCE)
        if name_label != null and rank_label != null:
            _check(
                "tree.%s.labels" % node["id"],
                row_path,
                "name='%s' rank='%s'" % [String(name_label.text), String(rank_label.text)],
                "name='%s' rank='0/5' (zeroed in-memory state)" % String(node["name"]),
                String(name_label.text) == String(node["name"]) && String(rank_label.text) == "0/5",
                "Render the catalog display name and the live rank as text.",
                SKILL_TREE_SOURCE
            )

        var buy_node := row.get_node_or_null("Buy")
        var buy_button := _buy_in_row(row, row_path, SKILL_TREE_SOURCE)
        _check(
            "tree.%s.gating" % node["id"],
            row_path,
            "buy_button=%s" % str(buy_node != null),
            "buy_button=%s (unlocked at zero state: first trunk only)" % str(expected_buy),
            (buy_node != null) == expected_buy,
            "Gate the buy button on the node's prerequisite rank (SkillUnlockRank).",
            SKILL_TREE_SOURCE
        )

        var icon := _require(row, "Icon", row_path, SKILL_TREE_SOURCE)
        var name_rect: Rect2 = name_label.get_global_rect() if name_label != null else Rect2()
        var icon_rect: Rect2 = icon.get_global_rect() if icon != null else Rect2()

        if buy_button != null and icon != null:
            # The Buy child is measured against its sibling Icon (also independently
            # placed from the row anchor), not against the row wrapper. A Buy shifted
            # inside a correctly sized wrapper moves here and fails.
            var icon_center: Vector2 = icon_rect.get_center()
            var buy_rect: Rect2 = buy_button.get_global_rect()
            var content_on_left: bool = node["kind"] == "seed" and icon_center.x < spine_x
            var expected_buy_left: float = (
                icon_center.x - ROW_BUY_OFFSET - ROW_BUY_WIDTH
                if content_on_left
                else icon_center.x + ROW_BUY_OFFSET
            )
            _check(
                "tree.%s.%s.buy_column" % [key, node["id"]],
                "%s/Buy" % row_path,
                "left=%.2f width=%.2f center.y=%.2f; icon center=(%.2f, %.2f)" % [
                    buy_rect.position.x, buy_rect.size.x, buy_rect.get_center().y,
                    icon_center.x, icon_center.y,
                ],
                "left=%.2f width=%.2f center.y=%.2f (icon centre plus the content-column offset)" % [
                    expected_buy_left, ROW_BUY_WIDTH, icon_center.y,
                ],
                _approx(buy_rect.position.x, expected_buy_left)
                    and _approx(buy_rect.size.x, ROW_BUY_WIDTH)
                    and _approx(buy_rect.get_center().y, icon_center.y),
                "Place Buy in the row's content column measured from the node anchor; never shift it inside the wrapper.",
                SKILL_TREE_SOURCE
            )

        if node["kind"] == "trunk":
            if icon != null:
                trunk_icon_centers.append(icon_rect.get_center().x)
            # Trunk content sits to the right of the spine. Compare the label rect
            # against the independently-measured tree-area centre.
            var content_right_of_spine: bool = name_rect.position.x >= spine_x + 19.0
            trunk_content_ok = trunk_content_ok and content_right_of_spine
            trunk_content_measured.append("%s name.left=%.2f" % [node["id"], name_rect.position.x])
        elif node["kind"] == "seed":
            # Seeds alternate left then right in catalog order.
            if seed_left == null:
                seed_left = row
                seed_left_icon = icon
            else:
                seed_right = row
                seed_right_icon = icon

    # ----- Tree centering -----------------------------------------------------------
    if trunk_icon_centers.size() > 0:
        var first_center: float = trunk_icon_centers[0]
        var all_same := true
        for center in trunk_icon_centers:
            all_same = all_same && _approx(center, first_center)
        _check(
            "tree.%s.trunk_alignment" % key,
            screen_path,
            "trunk icon centers=%s" % str(trunk_icon_centers),
            "all trunk icons share one x",
            all_same,
            "Stack trunk nodes on a single vertical spine.",
            SKILL_TREE_SOURCE
        )
        _check(
            "tree.%s.trunk_centered" % key,
            screen_path,
            "spine=%.2f" % first_center,
            "tree area center.x=%.2f" % spine_x,
            _approx(first_center, spine_x),
            "Center the trunk spine on the tree area's horizontal middle.",
            SKILL_TREE_SOURCE
        )

    _check(
        "tree.%s.trunk_content_right_of_spine" % key,
        screen_path,
        "spine=%.2f; %s" % [spine_x, "; ".join(trunk_content_measured)],
        "every trunk row's name label starts at spine+20",
        trunk_content_ok,
        "Place trunk row content to the right of the spine.",
        SKILL_TREE_SOURCE
    )

    # ----- Seed mirroring -----------------------------------------------------------
    if seed_left != null and seed_right != null:
        # Measure the rendered Icon children about the independently measured spine.
        # The row wrappers are built from the same anchor/offset constants that place
        # their children, so comparing wrapper extents is algebraic. A child shifted
        # inside a correct wrapper must fail here.
        var left_buy := _buy_in_row(
            seed_left, "%s/%s" % [screen_path, String(seed_left.name)], SKILL_TREE_SOURCE
        )
        var right_buy := _buy_in_row(
            seed_right, "%s/%s" % [screen_path, String(seed_right.name)], SKILL_TREE_SOURCE
        )

        if seed_left_icon != null and seed_right_icon != null:
            var left_icon_center: float = seed_left_icon.get_global_rect().get_center().x
            var right_icon_center: float = seed_right_icon.get_global_rect().get_center().x
            var mirror_ok := _approx(spine_x - left_icon_center, right_icon_center - spine_x)

            # A seed Buy only exists once that seed is unlocked. When both render,
            # include their rendered extents in the mirror check.
            var buy_measured := ""
            if left_buy != null and right_buy != null:
                var left_buy_rect: Rect2 = left_buy.get_global_rect()
                var right_buy_rect: Rect2 = right_buy.get_global_rect()
                mirror_ok = mirror_ok \
                    and _rects_mirror_about(left_buy_rect, right_buy_rect, spine_x)
                buy_measured = " buy left=[%.2f..%.2f] right=[%.2f..%.2f]" % [
                    left_buy_rect.position.x, left_buy_rect.end.x,
                    right_buy_rect.position.x, right_buy_rect.end.x,
                ]

            _check(
                "tree.%s.seed_mirror" % key,
                "%s/%s vs %s/%s" % [
                    screen_path, String(seed_left.name), screen_path, String(seed_right.name),
                ],
                "left icon center=%.2f right icon center=%.2f spine=%.2f%s" % [
                    left_icon_center, right_icon_center, spine_x, buy_measured,
                ],
                "icon centres mirror about the spine (spine-d / spine+d)",
                mirror_ok,
                "Mirror seed content about the tree-area centre; do not shift the whole block or a child inside its row.",
                SKILL_TREE_SOURCE
            )
            _check(
                "tree.%s.seed_sides" % key,
                "%s/%s vs %s/%s" % [
                    screen_path, String(seed_left.name), screen_path, String(seed_right.name),
                ],
                "left icon center=%.2f right icon center=%.2f spine=%.2f" % [
                    left_icon_center, right_icon_center, spine_x,
                ],
                "left seed icon center < spine < right seed icon center",
                left_icon_center < spine_x && right_icon_center > spine_x,
                "Branch the first seed left and the second right of the trunk spine.",
                SKILL_TREE_SOURCE
            )

        var left_name := _require_label(
            seed_left, "Name", "%s/%s" % [screen_path, String(seed_left.name)], SKILL_TREE_SOURCE
        )
        var left_rank := _require_label(
            seed_left, "Rank", "%s/%s" % [screen_path, String(seed_left.name)], SKILL_TREE_SOURCE
        )
        var right_name := _require_label(
            seed_right, "Name", "%s/%s" % [screen_path, String(seed_right.name)], SKILL_TREE_SOURCE
        )
        var right_rank := _require_label(
            seed_right, "Rank", "%s/%s" % [screen_path, String(seed_right.name)], SKILL_TREE_SOURCE
        )

        if left_name != null and left_rank != null and right_name != null and right_rank != null:
            var alignment_ok: bool = (
                left_name.horizontal_alignment == ALIGN_RIGHT
                and left_rank.horizontal_alignment == ALIGN_RIGHT
                and right_name.horizontal_alignment == ALIGN_LEFT
                and right_rank.horizontal_alignment == ALIGN_LEFT
            )
            _check(
                "tree.%s.left_branch_alignment" % key,
                "%s/%s/Name,Rank" % [screen_path, String(seed_left.name)],
                "left name=%d rank=%d; right name=%d rank=%d" % [
                    left_name.horizontal_alignment, left_rank.horizontal_alignment,
                    right_name.horizontal_alignment, right_rank.horizontal_alignment,
                ],
                "left name=%d rank=%d (right-aligned); right name=%d rank=%d (left-aligned)" % [
                    ALIGN_RIGHT, ALIGN_RIGHT, ALIGN_LEFT, ALIGN_LEFT,
                ],
                alignment_ok,
                "Right-align left-branch name/rank labels so they mirror the right branch's left-aligned labels.",
                SKILL_TREE_SOURCE
            )

            var left_name_rect: Rect2 = left_name.get_global_rect()
            var right_name_rect: Rect2 = right_name.get_global_rect()
            var left_rank_rect: Rect2 = left_rank.get_global_rect()
            var right_rank_rect: Rect2 = right_rank.get_global_rect()
            var content_mirror_ok := _rects_mirror_about(left_name_rect, right_name_rect, spine_x) \
                and _rects_mirror_about(left_rank_rect, right_rank_rect, spine_x)
            _check(
                "tree.%s.left_branch_content_mirror" % key,
                "%s/%s vs %s/%s" % [
                    screen_path, String(seed_left.name), screen_path, String(seed_right.name),
                ],
                "name left=[%.2f..%.2f] right=[%.2f..%.2f]; rank left=[%.2f..%.2f] right=[%.2f..%.2f]; spine=%.2f" % [
                    left_name_rect.position.x, left_name_rect.end.x,
                    right_name_rect.position.x, right_name_rect.end.x,
                    left_rank_rect.position.x, left_rank_rect.end.x,
                    right_rank_rect.position.x, right_rank_rect.end.x, spine_x,
                ],
                "name/rank rectangles mirror about the spine",
                content_mirror_ok,
                "Mirror left-branch content columns about the spine (ContentX with onLeft=true).",
                SKILL_TREE_SOURCE
            )

    # ----- Locked-node greying ------------------------------------------------------
    var unlocked_row: Control = rows.get(nodes[EXPECTED_UNLOCKED_INDEX]["id"])
    var locked_row: Control = rows.get(nodes[EXPECTED_UNLOCKED_INDEX + 1]["id"]) if nodes.size() > 1 else null
    if unlocked_row != null and locked_row != null:
        var unlocked_name := _require_label(
            unlocked_row, "Name", "%s/%s" % [screen_path, String(unlocked_row.name)], SKILL_TREE_SOURCE
        )
        var locked_name := _require_label(
            locked_row, "Name", "%s/%s" % [screen_path, String(locked_row.name)], SKILL_TREE_SOURCE
        )
        var unlocked_rank := _require_label(
            unlocked_row, "Rank", "%s/%s" % [screen_path, String(unlocked_row.name)], SKILL_TREE_SOURCE
        )
        var locked_rank := _require_label(
            locked_row, "Rank", "%s/%s" % [screen_path, String(locked_row.name)], SKILL_TREE_SOURCE
        )
        if unlocked_name != null and locked_name != null and unlocked_rank != null and locked_rank != null:
            var unlocked_color: Color = unlocked_name.get_theme_color("font_color")
            var locked_color: Color = locked_name.get_theme_color("font_color")
            var unlocked_rank_color: Color = unlocked_rank.get_theme_color("font_color")
            var locked_rank_color: Color = locked_rank.get_theme_color("font_color")
            var grey_ok: bool = (
                locked_color.r <= unlocked_color.r - 0.25
                and locked_color.g <= unlocked_color.g - 0.25
                and locked_color.b <= unlocked_color.b - 0.25
                and locked_rank_color.r <= unlocked_rank_color.r - 0.25
            )
            _check(
                "tree.%s.locked_rows_greyed" % key,
                "%s/%s/Name vs %s/Name" % [
                    screen_path, String(unlocked_row.name), String(locked_row.name),
                ],
                "unlocked name=%s locked name=%s locked rank=%s" % [
                    str(unlocked_color), str(locked_color), str(locked_rank_color),
                ],
                "locked label components at least 0.25 dimmer than unlocked",
                grey_ok,
                "Grey locked-node labels (and rank labels) so locked rows read as unavailable.",
                SKILL_TREE_SOURCE,
                "WARNING"
            )

    # ----- Disabled-node affordance must not reappear -------------------------------
    var has_coming_soon := _tree_has_text(tree_area, "coming soon")
    _check(
        "tree.%s.no_coming_soon_labels" % key,
        screen_path,
        "contains 'coming soon'=%s" % str(has_coming_soon),
        "contains 'coming soon'=false (all catalog nodes are enabled)",
        not has_coming_soon,
        "Remove the disabled-node 'coming soon' label; enabled nodes use rank/buy controls.",
        SKILL_TREE_SOURCE,
        "WARNING"
    )


# ---------------------------------------------------------------------------------
# Assertion bookkeeping
# ---------------------------------------------------------------------------------

func _check(
    id: String,
    group: String,
    measured: String,
    expected: String,
    ok: bool,
    fix: String,
    source: String,
    severity := "CRITICAL",
    is_precondition := false
) -> void:
    # Preconditions prove the harness is measuring the right thing; they are not
    # layout coverage. Track the two separately so a run that measures nothing but
    # its own preconditions cannot report PASS.
    if not is_precondition:
        _layout_assertions += 1
    _evaluated.append({
        "id": id,
        "ok": ok,
        "detail": "%s: measured %s; expected %s" % [group, measured, expected],
    })
    if not ok:
        _findings.append({
            "id": id,
            "source": source,
            "group": group,
            "measured": measured,
            "expected": expected,
            "fix": fix,
            "severity": severity,
        })


## Assert an inner rect lies inside an outer rect. Both rects must come from
## independently-positioned nodes; see the assertion rule in layout_verifier.md.
func _check_inside(
    id: String,
    group: String,
    inner: Rect2,
    outer: Rect2,
    fix: String,
    source: String,
    severity := "CRITICAL",
    epsilon := EPS
) -> void:
    _check(
        id,
        group,
        str(inner),
        "inside %s" % str(outer),
        _rect_inside(inner, outer, epsilon),
        fix,
        source,
        severity
    )


## Assert a rect is centered on the viewport's vertical axis.
func _check_centered_on_viewport(
    id: String,
    group: String,
    rect: Rect2,
    fix: String,
    source: String,
    severity := "CRITICAL"
) -> void:
    var center_x := rect.get_center().x
    var viewport_center_x := _viewport_center_x()
    _check(
        id,
        group,
        "center.x=%.2f" % center_x,
        "viewport center.x=%.2f" % viewport_center_x,
        _approx(center_x, viewport_center_x),
        fix,
        source,
        severity
    )


## Assert a button stacks below the previous one without overlap and at the
## same height. Shared by every vertical button group.
func _check_stack_gap(
    id: String,
    group: String,
    rect: Rect2,
    previous: Rect2,
    fix: String,
    source: String
) -> void:
    var gap: float = rect.position.y - previous.end.y
    _check(
        id,
        group,
        "gap above=%.2f height=%.2f" % [gap, rect.size.y],
        "gap>0 and height=%.2f (previous button height)" % previous.size.y,
        gap > 0.0 and _approx(rect.size.y, previous.size.y),
        fix,
        source,
        "WARNING"
    )


func _record_missing_group(group: String, fix: String, source: String) -> void:
    _check(
        "group_resolution.%s" % group,
        group,
        "node not found",
        "named Control present in the scene tree",
        false,
        fix,
        source,
        "CRITICAL"
    )


func _record_wrong_type(group: String, expected_type: String, node: Node, source: String) -> void:
    _check(
        "group_resolution.%s" % group,
        group,
        "node is %s" % node.get_class(),
        "named %s present at this path" % expected_type,
        false,
        "Expected %s at this path; found %s. Restore the named group." % [
            expected_type, node.get_class(),
        ],
        source,
        "CRITICAL"
    )
    # A required node of the wrong class is not measurable. Continue collecting
    # findings, but force a fail-closed exit rather than letting the run read as a
    # normal layout finding (the process must never hang either).
    if _fatal_reason.is_empty():
        _fatal_reason = "%s is a %s, expected %s (required node has the wrong class)" % [
            group, node.get_class(), expected_type,
        ]


func _require(root: Node, path: String, group_prefix: String, source: String) -> Control:
    var node := root.get_node_or_null(path)
    var group := "%s/%s" % [group_prefix, path]
    if node == null:
        _record_missing_group(
            group,
            "Create the named Control group and keep it in the tree.",
            source
        )
        return null
    if not (node is Control):
        _record_wrong_type(group, "Control", node, source)
        return null
    return node


func _require_label(root: Node, path: String, group_prefix: String, source: String) -> Label:
    var node := _require(root, path, group_prefix, source)
    if node == null:
        return null
    if not (node is Label):
        _record_wrong_type("%s/%s" % [group_prefix, path], "Label", node, source)
        return null
    return node


func _require_button(root: Node, path: String, group_prefix: String, source: String) -> Button:
    var node := _require(root, path, group_prefix, source)
    if node == null:
        return null
    if not (node is Button):
        _record_wrong_type("%s/%s" % [group_prefix, path], "Button", node, source)
        return null
    return node


## Buy button of a row, if present. Absence stays null (asserted separately), but a
## present node of the wrong class is recorded and fails the run closed.
func _buy_in_row(row: Node, row_path: String, source: String) -> Button:
    if row == null:
        return null
    var node := row.get_node_or_null("Buy")
    if node == null:
        return null
    if node is Button:
        return node
    _record_wrong_type("%s/Buy" % row_path, "Button", node, source)
    return null


func _verification_is_active(skill_screen: Node) -> bool:
    if not skill_screen.has_method("IsVerificationStateActive"):
        return false
    return bool(skill_screen.call("IsVerificationStateActive"))


func _fail_closed(reason: String) -> void:
    if _finished:
        return
    _finished = true
    print("LAYOUT VERIFIER - geometry-tower-defense")
    print("scene: %s" % MAIN_SCENE)
    print("")
    print("RESULT: FAIL (fail-closed: %s)" % reason)
    print("LAYOUT ASSERTIONS: %d" % _layout_assertions)
    print("COVERAGE: %d evaluated, %d skipped, %d findings" % [
        _evaluated.size(), _skipped.size(), _findings.size(),
    ])
    quit(2)


func _finish() -> void:
    if _finished:
        return
    _finished = true
    print("--- EVALUATED (%d) ---" % _evaluated.size())
    for entry in _evaluated:
        print("%s  %s: %s" % [
            "PASS" if entry["ok"] else "FAIL", entry["id"], entry["detail"],
        ])
    print("")
    print("--- SKIPPED (%d) ---" % _skipped.size())
    for entry in _skipped:
        print("SKIP  %s: %s" % [entry["id"], entry["reason"]])
    print("")
    print("--- FINDINGS (%d) ---" % _findings.size())
    if _findings.is_empty():
        print("(none)")
    else:
        for finding in _findings:
            print("%s: %s: %s: measured %s; expected %s; %s" % [
                finding["source"], finding["group"], finding["severity"],
                finding["measured"], finding["expected"], finding["fix"],
            ])
    print("")
    print("LAYOUT ASSERTIONS: %d (preconditions excluded)" % _layout_assertions)
    print("COVERAGE: %d evaluated, %d skipped, %d findings" % [
        _evaluated.size(), _skipped.size(), _findings.size(),
    ])

    # A required node with the wrong class makes the measurement unreliable; it is
    # fail-closed even though a finding was also recorded.
    if not _fatal_reason.is_empty():
        print("RESULT: FAIL (fail-closed: %s)" % _fatal_reason)
        quit(2)
        return

    if not _findings.is_empty():
        print("RESULT: FAIL (%d findings)" % _findings.size())
        quit(1)
        return

    if _evaluated.is_empty() or _layout_assertions == 0:
        print("RESULT: FAIL (fail-closed: zero layout assertions evaluated)")
        quit(2)
        return

    print("RESULT: PASS")
    quit(0)


# ---------------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------------

func _viewport_rect() -> Rect2:
    return Rect2(Vector2.ZERO, Vector2(_viewport))


func _viewport_center_x() -> float:
    return float(_viewport.x) / 2.0


func _read_project_viewport() -> Dictionary:
    var config := ConfigFile.new()
    if config.load("res://project.godot") != OK:
        return {}

    # project.godot sections map directly: [display] window/size/viewport_width.
    var width = config.get_value("display", "window/size/viewport_width", null)
    var height = config.get_value("display", "window/size/viewport_height", null)
    if not _is_valid_viewport_dimension(width) or not _is_valid_viewport_dimension(height):
        return {}

    return {"width": int(width), "height": int(height)}


func _is_valid_viewport_dimension(value) -> bool:
    if value == null:
        return false
    var type := typeof(value)
    if type != TYPE_INT and type != TYPE_FLOAT:
        return false
    var number := float(value)
    if not is_finite(number):
        return false
    # A fractional value such as 0.5 would truncate to 0 and bypass the guard.
    if number != floorf(number):
        return false
    return number >= float(VIEWPORT_DIM_MIN) and number <= float(VIEWPORT_DIM_MAX)


func _approx(a: float, b: float, epsilon := EPS) -> bool:
    return absf(a - b) <= epsilon


func _rect_inside(inner: Rect2, outer: Rect2, epsilon := EPS) -> bool:
    return inner.position.x >= outer.position.x - epsilon \
        and inner.position.y >= outer.position.y - epsilon \
        and inner.end.x <= outer.end.x + epsilon \
        and inner.end.y <= outer.end.y + epsilon


## True when two rects mirror horizontally about `axis`: each left edge is as far
## from the axis as the matching right edge.
func _rects_mirror_about(left: Rect2, right: Rect2, axis: float) -> bool:
    return _approx(axis - left.position.x, right.end.x - axis) \
        and _approx(axis - left.end.x, right.position.x - axis)


func _find_by_script(node: Node, script_path: String) -> Node:
    for child in node.get_children():
        var script = child.get_script()
        if script != null and script.resource_path == script_path:
            return child
        var found := _find_by_script(child, script_path)
        if found != null:
            return found
    return null


func _find_all_by_script(node: Node, script_path: String) -> Array:
    var found := []
    for child in node.get_children():
        var script = child.get_script()
        if script != null and script.resource_path == script_path:
            found.append(child)
        found.append_array(_find_all_by_script(child, script_path))
    return found


func _row_name(node_id: String) -> String:
    return "Row_" + node_id.replace(".", "_")


func _child_names(node: Node) -> Array:
    var names := []
    for child in node.get_children():
        names.append(String(child.name))
    names.sort()
    return names


func _row_names(tree_area: Node) -> Array:
    var names := []
    for child in tree_area.get_children():
        var child_name := String(child.name)
        if child_name.begins_with("Row_"):
            names.append(child_name)
    names.sort()
    return names


func _tree_has_text(node: Node, needle: String) -> bool:
    for child in node.get_children():
        if child is Label and String(child.text).to_lower().contains(needle):
            return true
        if _tree_has_text(child, needle):
            return true
    return false
