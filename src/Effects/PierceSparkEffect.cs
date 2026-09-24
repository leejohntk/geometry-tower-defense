using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Brief, procedurally-drawn spark at the enemy an arrow passed through: a small
/// X-shaped burst of spokes that expands (ease-out) then fades. Deliberately much
/// smaller and shorter than <see cref="ExplosionEffect"/> so a pierced hit reads as a
/// pass-through, never as a splash.
/// Pooled: <see cref="Play"/> resets all per-play state and is safe to call repeatedly
/// on a reused instance.
/// </summary>
public partial class PierceSparkEffect : Node2D
{
    // Signal emitted when the animation completes. GameManager releases the effect to its pool.
    [Signal]
    public delegate void FinishedEventHandler(PierceSparkEffect effect);

    private float _elapsed = 0f;
    private bool _playing = false;

    /// <summary>
    /// Progress (0..1) through the animation duration.
    /// </summary>
    public float Progress =>
        GameConstants.PierceSparkDuration > 0f
            ? _elapsed / GameConstants.PierceSparkDuration
            : 1f;

    /// <summary>
    /// Start (or restart) the effect at a world position.
    /// Safe to call repeatedly on a pooled instance — resets all per-play state.
    /// </summary>
    public void Play(Vector2 position)
    {
        _elapsed = 0f;
        _playing = false; // nothing draws until _Process advances the clock
        Position = position;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_playing)
        {
            // First frame after Play(): begin the animation now.
            _playing = true;
        }

        _elapsed += (float)delta;

        if (_elapsed >= GameConstants.PierceSparkDuration)
        {
            _elapsed = GameConstants.PierceSparkDuration;
            _playing = false;
            EmitSignal(SignalName.Finished, this);
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_playing)
            return;

        float outer = RadiusAt(Progress);
        float inner = outer * GameConstants.PierceSparkInnerRadiusRatio;
        Color color = GameConstants.PierceSparkColor with { A = AlphaAt(Progress) };

        // Spokes are recomputed from the spoke index on every draw (struct math only),
        // so the effect caches nothing and allocates nothing per frame.
        int spokes = GameConstants.PierceSparkSpokes;
        for (int i = 0; i < spokes; i++)
        {
            float angle = i * (System.MathF.Tau / spokes);
            var direction = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle));
            DrawLine(
                direction * inner,
                direction * outer,
                color,
                GameConstants.PierceSparkStrokeWidth
            );
        }
    }

    /// <summary>
    /// Spoke length at the given animation progress (0..1). The spokes expand (ease-out)
    /// from nothing to <see cref="GameConstants.PierceSparkRadius"/>. Progress is clamped,
    /// so callers can pass out-of-range values safely.
    /// </summary>
    public static float RadiusAt(float progress)
    {
        // Quadratic ease-out: 1 - (1 - p)^2. Starts fast, decelerates to rest at p = 1.
        float p = ClampProgress(progress);
        float inv = 1f - p;
        return (1f - inv * inv) * GameConstants.PierceSparkRadius;
    }

    /// <summary>
    /// Opacity at the given animation progress (0..1): fully opaque at the impact, then
    /// fading to transparent by progress 1. Progress is clamped, so callers can pass
    /// out-of-range values safely.
    /// </summary>
    public static float AlphaAt(float progress) => 1f - ClampProgress(progress);

    private static float ClampProgress(float progress) => Mathf.Clamp(progress, 0f, 1f);
}
