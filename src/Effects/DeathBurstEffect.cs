using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Pooled geometric death burst at a kill position: a ring of shards that expands
/// (ease-out) and fades. Color is the dead enemy's fill; shard count and radius scale
/// with the enemy kind through <see cref="EnemySilhouette"/>.
///
/// <see cref="Play"/> resets every per-play field, so a pooled instance never leaks a
/// previous burst's color, size, or shard count. The completion signal is emitted
/// exactly once per play.
/// </summary>
public partial class DeathBurstEffect : Node2D
{
    [Signal]
    public delegate void FinishedEventHandler(DeathBurstEffect effect);

    private float _elapsed = 0f;
    private bool _playing = false;
    private int _shards = 0;
    private float _radius = 0f;
    private Color _color = Colors.White;

    /// <summary>True while the burst is animating.</summary>
    public bool IsPlaying => _playing;

    /// <summary>Shard count of the current (or last) burst.</summary>
    public int ShardCount => _shards;

    /// <summary>Outer radius of the current (or last) burst, in pixels.</summary>
    public float BurstRadius => _radius;

    /// <summary>Color of the current (or last) burst.</summary>
    public Color BurstColor => _color;

    /// <summary>Progress (0..1) through the animation.</summary>
    public float Progress => GameConstants.DeathBurstDuration > 0f
        ? Mathf.Clamp(_elapsed / GameConstants.DeathBurstDuration, 0f, 1f)
        : 1f;

    /// <summary>
    /// Shard count for a kind's death burst. Pure, so the kind scaling is testable
    /// without instantiating an effect.
    /// </summary>
    public static int ShardsFor(EnemyKind kind) => EnemySilhouette.For(kind).BurstShards;

    /// <summary>
    /// Outer radius (px) for a kind's death burst. The Progenitor's radius is at
    /// least twice the Basic enemy's.
    /// </summary>
    public static float RadiusFor(EnemyKind kind) => EnemySilhouette.For(kind).BurstRadius;

    /// <summary>
    /// Starts (or restarts) a burst at a world position, using the kind's profile.
    /// Safe to call repeatedly on a pooled instance.
    /// </summary>
    public void Play(Vector2 position, Color color, EnemyKind kind)
    {
        _elapsed = 0f;
        _playing = true;
        _shards = Mathf.Max(0, ShardsFor(kind));
        _radius = Mathf.Max(0f, RadiusFor(kind));
        _color = color;
        Position = position;

        // Nothing is drawn until the first _Draw after Play; repaint immediately so a
        // burst triggered between frames is never invisible.
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_playing)
            return;

        _elapsed += (float)delta;

        if (_elapsed >= GameConstants.DeathBurstDuration)
        {
            _elapsed = GameConstants.DeathBurstDuration;
            _playing = false;
            QueueRedraw();
            // _playing == false makes every later _Process a no-op, so this emits
            // exactly once per Play without a separate guard flag.
            EmitSignal(SignalName.Finished, this);
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_playing)
            return;

        float progress = Progress;
        float outer = RadiusAt(progress, _radius);
        float inner = outer * GameConstants.DeathBurstInnerRadiusRatio;
        Color color = _color with { A = _color.A * AlphaAt(progress) };

        // Shards are pure trig from the index: no cached arrays and no allocation.
        int shards = _shards;
        if (shards > 0)
        {
            float twist = progress * 0.6f;
            float step = Mathf.Tau / shards;
            for (int i = 0; i < shards; i++)
            {
                float angle = step * i + twist;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(
                    direction * inner,
                    direction * outer,
                    color,
                    GameConstants.DeathBurstShardWidth);
            }
        }

        // A faint core ring ties the shards together and reads as the burst origin.
        float core = outer * 0.45f;
        if (core > 0.5f)
        {
            DrawCircle(
                Vector2.Zero,
                core,
                color with { A = color.A * 0.45f },
                false,
                GameConstants.DeathBurstShardWidth * 0.6f);
        }
    }

    /// <summary>
    /// Shard reach at the given progress: quadratic ease-out, landing exactly on
    /// <paramref name="baseRadius"/> at progress 1. Progress is clamped.
    /// </summary>
    public static float RadiusAt(float progress, float baseRadius)
    {
        float p = Mathf.Clamp(progress, 0f, 1f);
        float inv = 1f - p;
        return (1f - inv * inv) * Mathf.Max(0f, baseRadius);
    }

    /// <summary>Opacity at the given progress: opaque at impact, transparent at 1.</summary>
    public static float AlphaAt(float progress) => 1f - Mathf.Clamp(progress, 0f, 1f);
}
