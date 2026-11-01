using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Fixed-capacity fading sample history for a projectile trail. Purely visual: it
/// never collides, never feeds back into movement, and the backing arrays are
/// allocated once per projectile (from the pool) so advancing it per frame allocates
/// nothing.
///
/// Sample 0 is the newest; higher indices are older. Samples older than
/// <see cref="Lifetime"/> are dropped, and a new sample is pushed at most once per
/// sample interval so the trail length is time-bounded rather than frame-bounded.
/// </summary>
public sealed class ProjectileTrail
{
    private readonly Vector2[] _positions;
    private readonly float[] _ages;
    private readonly float _sampleInterval;
    private int _count;
    private float _accumulator;

    public ProjectileTrail(int capacity, float lifetime, float sampleInterval)
    {
        _positions = new Vector2[Mathf.Max(1, capacity)];
        _ages = new float[_positions.Length];
        Lifetime = Mathf.Max(0f, lifetime);
        _sampleInterval = Mathf.Max(0.0001f, sampleInterval);
    }

    /// <summary>Arrow trail: ~3 segments over ~0.12 s.</summary>
    public static ProjectileTrail CreateArrowTrail() => new(
        GameConstants.ArrowTrailSegments + 1,
        GameConstants.ArrowTrailLifetime,
        GameConstants.ArrowTrailLifetime / GameConstants.ArrowTrailSegments);

    /// <summary>Cannon trail: ~4 smoke puffs over ~0.2 s.</summary>
    public static ProjectileTrail CreateCannonTrail() => new(
        GameConstants.CannonTrailPuffs,
        GameConstants.CannonTrailLifetime,
        GameConstants.CannonTrailLifetime / GameConstants.CannonTrailPuffs);

    /// <summary>Maximum number of samples this trail can hold.</summary>
    public int Capacity => _positions.Length;

    /// <summary>Seconds a sample remains visible.</summary>
    public float Lifetime { get; }

    /// <summary>Number of live samples (0 = nothing to draw).</summary>
    public int Count => _count;

    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Monotonic version, incremented only when the sample set actually changes (a push
    /// or an expiry) and whenever the trail is cleared/reset. Renderers compare this to
    /// skip reassigning a draw buffer on frames where nothing moved in the history.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>World position of the sample at <paramref name="index"/> (0 = newest).</summary>
    public Vector2 PositionAt(int index) =>
        index >= 0 && index < _count ? _positions[index] : Vector2.Zero;

    /// <summary>Age in seconds of the sample at <paramref name="index"/>.</summary>
    public float AgeAt(int index) =>
        index >= 0 && index < _count ? _ages[index] : 0f;

    /// <summary>
    /// Fade factor (1 = fresh, 0 = fully faded) for the sample at
    /// <paramref name="index"/>. A zero lifetime fades instantly.
    /// </summary>
    public float FadeAt(int index)
    {
        if (index < 0 || index >= _count)
            return 0f;
        if (!(Lifetime > 0f))
            return 0f;
        return Mathf.Clamp(1f - _ages[index] / Lifetime, 0f, 1f);
    }

    /// <summary>
    /// Clears all samples. Called on hit, dissipation, and pool reuse so a trail can
    /// never leak into the next shot.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _accumulator = 0f;
        Version++;
    }

    /// <summary>
    /// Clears the trail and seeds it with a single sample at the projectile's spawn
    /// position. Safe to call on every pool reuse.
    /// </summary>
    public void Reset(Vector2 position)
    {
        Clear();
        _positions[0] = position;
        _ages[0] = 0f;
        _count = 1;
    }

    /// <summary>
    /// Ages every sample by <paramref name="delta"/> and pushes the current position
    /// when the sample interval elapses. Non-positive or non-finite deltas are ignored.
    /// </summary>
    public void Advance(Vector2 position, float delta)
    {
        if (!(delta > 0f) || !float.IsFinite(delta))
            return;

        for (int i = 0; i < _count; i++)
            _ages[i] += delta;

        // Drop expired samples from the tail (oldest end).
        bool dropped = false;
        while (_count > 0 && _ages[_count - 1] > Lifetime)
        {
            _count--;
            dropped = true;
        }

        if (dropped)
            Version++;

        _accumulator += delta;

        // Bound the loop: a pathological stall must not push unbounded samples.
        int pushes = 0;
        while (_accumulator >= _sampleInterval && pushes < _positions.Length)
        {
            Push(position);
            _accumulator -= _sampleInterval;
            pushes++;
        }
    }

    private void Push(Vector2 position)
    {
        int shift = Mathf.Min(_count, _positions.Length - 1);
        if (shift > 0)
        {
            System.Array.Copy(_positions, 0, _positions, 1, shift);
            System.Array.Copy(_ages, 0, _ages, 1, shift);
        }

        _positions[0] = position;
        _ages[0] = 0f;
        if (_count < _positions.Length)
            _count++;
        Version++;
    }
}
