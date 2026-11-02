using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Top-bar fast-forward cue shown while the whole-game clock is sped up: two solid
/// right-pointing triangles that sweep right on a short loop with an alpha pulse.
/// Visual only — click-through, no input, and a fixed rect (hidden at the normal
/// clock) so toggling never shifts the top-bar layout. Drawing is procedural and
/// allocation-free, and the node stops processing entirely while hidden.
/// </summary>
public partial class SpeedupCue : Control
{
    private Control? _icon;
    private float _phase;

    // One reused triangle buffer: both triangles are redrawn from it on every frame,
    // so the animation never allocates.
    private readonly Vector2[] _triangle = new Vector2[3];

    /// <summary>Loop position (0..1). Exposed for tests.</summary>
    public float Phase => _phase;

    public override void _Ready()
    {
        Size = new Vector2(GameConstants.HudSpeedupCueWidth, GameConstants.HudSpeedupCueHeight);
        MouseFilter = MouseFilterEnum.Ignore;

        // Safe default: nothing draws and nothing ticks until the clock is sped up.
        // GameHUD only shows the cue in response to GameManager's SpeedupChanged.
        Visible = false;
        SetProcess(false);

        var icon = new Control();
        icon.Name = "FastForwardIcon";
        icon.Position = Vector2.Zero;
        icon.Size = Size;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () => DrawFastForward(icon);
        AddChild(icon);
        _icon = icon;
    }

    /// <summary>
    /// Shows or hides the cue. A hidden cue stops processing; showing it restarts the
    /// sweep from the loop's beginning so every activation reads the same.
    /// </summary>
    public void SetActive(bool active)
    {
        if (active)
            _phase = 0f;

        Visible = active;
        SetProcess(active);

        if (active)
            _icon?.QueueRedraw();
    }

    /// <summary>
    /// Stops processing entirely while hidden (not merely skipping the animation
    /// body), matching the other animated screens.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
            SetProcess(Visible);
    }

    public override void _Process(double delta)
    {
        // Skip when the cue or any ancestor is hidden, and when the frame carries no
        // time. The engine only calls this while processing is enabled, but the
        // ancestor check keeps a hidden HUD from doing pointless redraw work.
        if (!IsVisibleInTree() || !(delta > 0d))
            return;

        // The authored loop is wall-clock paced: Engine.TimeScale scales _Process
        // deltas, so undo it to keep the cue at ~0.8 s per cycle at any game speed.
        double unscaledDelta = delta / System.Math.Max(Engine.TimeScale, 0.001);
        _phase = Mathf.PosMod(
            _phase + (float)unscaledDelta / GameConstants.HudSpeedupCycleSeconds, 1f);
        _icon?.QueueRedraw();
    }

    /// <summary>
    /// Sweep offset (px) at a loop position: 0 at the start, the full sweep distance
    /// just before the loop wraps. Wraps out-of-range phases safely.
    /// </summary>
    public static float SweepOffsetAt(float phase) =>
        SweepOffsetForNormalizedPhase(Mathf.PosMod(phase, 1f));

    /// <summary>
    /// Opacity at a loop position: pulses between the min and max alpha once per cycle.
    /// Wraps out-of-range phases safely.
    /// </summary>
    public static float AlphaAt(float phase) =>
        AlphaForNormalizedPhase(Mathf.PosMod(phase, 1f));

    // Draw-only helpers. The render path passes _phase directly: _Process already
    // normalizes it into [0, 1), so the per-frame redraw never repeats the modulo.
    private static float SweepOffsetForNormalizedPhase(float phase) =>
        GameConstants.HudSpeedupSweepDistance * phase;

    private static float AlphaForNormalizedPhase(float phase) =>
        Mathf.Lerp(
            GameConstants.HudSpeedupAlphaMin,
            GameConstants.HudSpeedupAlphaMax,
            0.5f + 0.5f * Mathf.Sin(phase * Mathf.Tau));

    /// <summary>
    /// Draws the two triangles at the current sweep offset and pulse alpha. The fixed
    /// cue width reserves the sweep distance on both sides, so the geometry never
    /// leaves the icon rect and the wrapper never has to move.
    /// </summary>
    private void DrawFastForward(Control icon)
    {
        if (!IsInstanceValid(icon))
            return;

        Color color = GameConstants.HudSpeedupColor
            with { A = GameConstants.HudSpeedupColor.A * AlphaForNormalizedPhase(_phase) };
        float sweep = SweepOffsetForNormalizedPhase(_phase);

        float width = GameConstants.HudSpeedupTriangleWidth;
        float gap = GameConstants.HudSpeedupTriangleGap;
        float total = width * 2f + gap;
        float left = (icon.Size.X - total) / 2f + sweep;
        float centerY = icon.Size.Y / 2f;
        float halfHeight = GameConstants.HudSpeedupTriangleHeight / 2f;

        for (int i = 0; i < 2; i++)
        {
            float x = left + i * (width + gap);
            _triangle[0] = new Vector2(x, centerY - halfHeight);
            _triangle[1] = new Vector2(x + width, centerY);
            _triangle[2] = new Vector2(x, centerY + halfHeight);
            icon.DrawColoredPolygon(_triangle, color);
        }
    }
}
