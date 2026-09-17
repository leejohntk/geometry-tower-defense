using Godot;
using System;

namespace GeometryTowerDefense;

/// <summary>
/// Brief, procedurally-drawn explosion ring at a cannonball impact point.
///
/// The circle expands (ease-out) to the cannon's true AoE radius so the player sees
/// the exact damage extent, then fades out. Pooled: <see cref="Play"/> resets all
/// per-play state and is safe to call repeatedly on a reused instance.
/// </summary>
public partial class ExplosionEffect : Node2D
{
    // Signal emitted when the animation completes. GameManager releases the effect to its pool.
    [Signal]
    public delegate void FinishedEventHandler(ExplosionEffect effect);

    private float _elapsed = 0f;
    private float _maxRadius = 0f;
    private bool _playing = false;

    /// <summary>
    /// Progress (0..1) through the animation duration.
    /// </summary>
    public float Progress =>
        GameConstants.CannonExplosionDuration > 0f
            ? _elapsed / GameConstants.CannonExplosionDuration
            : 1f;

    /// <summary>
    /// Start (or restart) the effect at a world position with the given maximum radius.
    /// Safe to call repeatedly on a pooled instance — resets all per-play state.
    /// </summary>
    public void Play(Vector2 position, float radius)
    {
        _elapsed = 0f;
        _maxRadius = Math.Max(0f, radius);
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

        if (_elapsed >= GameConstants.CannonExplosionDuration)
        {
            _elapsed = GameConstants.CannonExplosionDuration;
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

        float radius = RadiusAt(Progress, _maxRadius);
        float alpha = AlphaAt(Progress);

        // Filled circle marking the damage area, plus an outline ring. The filled circle
        // stays at the full radius (fill is what communicates the damage extent). The
        // outline is inset by half the stroke width because Godot centers an unfilled
        // circle's stroke on its radius; this keeps the stroke's outer edge exactly on the
        // AoE radius instead of bleeding 1px past the damage area.
        DrawCircle(
            Vector2.Zero,
            radius,
            GameConstants.CannonExplosionFillColor with { A = alpha }
        );
        DrawCircle(
            Vector2.Zero,
            Math.Max(0f, radius - GameConstants.CannonExplosionStrokeWidth / 2f),
            GameConstants.CannonExplosionOutlineColor with { A = alpha },
            false,
            GameConstants.CannonExplosionStrokeWidth
        );
    }

    /// <summary>
    /// Radius at the given animation progress (0..1). The circle expands (ease-out) to
    /// the full radius over the first half of the animation and holds there through the
    /// fade-out, landing exactly on maxRadius at progress 0.5 and beyond. Progress is
    /// clamped, so callers can pass out-of-range values safely.
    /// </summary>
    public static float RadiusAt(float progress, float maxRadius)
    {
        float p = ClampProgress(progress);
        float expand = Math.Min(p / 0.5f, 1f); // expansion completes at the halfway point
        float eased = EaseOut(expand);
        return eased * maxRadius;
    }

    /// <summary>
    /// Opacity at the given animation progress (0..1). Opaque for the first half of the
    /// animation (so the expanding ring reads clearly), then fades to fully transparent
    /// by progress 1. Progress is clamped, so callers can pass out-of-range values safely.
    /// </summary>
    public static float AlphaAt(float progress)
    {
        float p = ClampProgress(progress);

        if (p <= 0.5f)
            return 1f;

        return 1f - (p - 0.5f) * 2f;
    }

    /// <summary>
    /// Quadratic ease-out: 1 - (1 - p)^2. Starts fast, decelerates to zero velocity at p = 1.
    /// </summary>
    private static float EaseOut(float progress)
    {
        float inv = 1f - progress;
        return 1f - inv * inv;
    }

    private static float ClampProgress(float progress) => Math.Clamp(progress, 0f, 1f);
}
