namespace GeometryTowerDefense;

/// <summary>
/// One immutable presentation profile per enemy kind: silhouette side count, idle
/// transform motion, layered detail flags, and the shape of the pooled death burst.
/// Drawing and animation code reads this single table instead of branching on the kind
/// in scattered if-statements, so adding a kind only means adding one profile here.
///
/// Sides = 0 means a circle; every other value is a regular polygon with that many
/// sides. Spin and pulse are applied as node transforms (Rotation / Scale), never as
/// per-frame redraws. Zero spin and zero pulse are valid: an armored diamond is a
/// deliberately static silhouette.
/// </summary>
public readonly struct EnemySilhouette
{
    /// <summary>Polygon side count; 0 renders a circle.</summary>
    public int Sides { get; }

    /// <summary>Idle spin speed around the silhouette's own center (radians/sec).</summary>
    public float SpinSpeed { get; }

    /// <summary>Gentle idle scale pulse amplitude (0 = none).</summary>
    public float PulseAmplitude { get; }

    /// <summary>Idle scale pulse angular speed (radians/sec).</summary>
    public float PulseSpeed { get; }

    /// <summary>
    /// Angle (radians) of the polygon's first vertex. Explicit per kind so shape
    /// orientation is never inferred from a color/detail flag.
    /// </summary>
    public float StartAngle { get; }

    /// <summary>Draw a thin inner ring (the Basic circle's idle cue).</summary>
    public bool InnerRing { get; }

    /// <summary>Draw an inset border polygon inside the silhouette (Armored).</summary>
    public bool InsetBorder { get; }

    /// <summary>Draw four small corner rivets inside the silhouette (Armored).</summary>
    public bool Rivets { get; }

    /// <summary>Border color follows the live Progenitor phase and is thicker.</summary>
    public bool PhaseBorder { get; }

    /// <summary>Draw a separate counter-rotating outer ring (Progenitor).</summary>
    public bool OuterRing { get; }

    /// <summary>Shard count of the death burst.</summary>
    public int BurstShards { get; }

    /// <summary>Outer shard radius of the death burst, in pixels.</summary>
    public float BurstRadius { get; }

    public EnemySilhouette(
        int sides,
        float spinSpeed,
        float pulseAmplitude,
        float pulseSpeed,
        float startAngle,
        bool innerRing,
        bool insetBorder,
        bool rivets,
        bool phaseBorder,
        bool outerRing,
        int burstShards,
        float burstRadius)
    {
        Sides = sides;
        SpinSpeed = spinSpeed;
        PulseAmplitude = pulseAmplitude;
        PulseSpeed = pulseSpeed;
        StartAngle = startAngle;
        InnerRing = innerRing;
        InsetBorder = insetBorder;
        Rivets = rivets;
        PhaseBorder = phaseBorder;
        OuterRing = outerRing;
        BurstShards = burstShards;
        BurstRadius = burstRadius;
    }

    /// <summary>
    /// Presentation profile for the given kind. Basic pulses around a circled inner
    /// ring, Swarm spins a triangle, Armored is static (inset border and rivets are
    /// its idle cue), and the Progenitor body is static while a separate outer ring
    /// counter-rotates.
    /// </summary>
    public static EnemySilhouette For(EnemyKind kind) => kind switch
    {
        EnemyKind.Swarm => new EnemySilhouette(
            GameConstants.EnemySwarmSides,
            GameConstants.EnemySwarmSpinSpeed,
            0f,
            0f,
            -System.MathF.PI / 2f,
            innerRing: false,
            insetBorder: false,
            rivets: false,
            phaseBorder: false,
            outerRing: false,
            GameConstants.DeathBurstSwarmShards,
            GameConstants.DeathBurstSwarmRadius),

        EnemyKind.Armored => new EnemySilhouette(
            GameConstants.EnemyArmoredSides,
            0f,
            0f,
            0f,
            -System.MathF.PI / 2f,
            innerRing: false,
            insetBorder: true,
            rivets: true,
            phaseBorder: false,
            outerRing: false,
            GameConstants.DeathBurstArmoredShards,
            GameConstants.DeathBurstArmoredRadius),

        EnemyKind.Progenitor => new EnemySilhouette(
            GameConstants.EnemyProgenitorSides,
            0f,
            0f,
            0f,
            0f,
            innerRing: false,
            insetBorder: false,
            rivets: false,
            phaseBorder: true,
            outerRing: true,
            GameConstants.DeathBurstProgenitorShards,
            GameConstants.DeathBurstProgenitorRadius),

        _ => new EnemySilhouette(
            0,
            0f,
            GameConstants.EnemyBasicPulseAmplitude,
            GameConstants.EnemyBasicPulseSpeed,
            -System.MathF.PI / 2f,
            innerRing: true,
            insetBorder: false,
            rivets: false,
            phaseBorder: false,
            outerRing: false,
            GameConstants.DeathBurstBasicShards,
            GameConstants.DeathBurstBasicRadius)
    };

    /// <summary>
    /// Polygon side count for the given kind (0 = circle). Kept as a convenience for
    /// callers that only need the shape.
    /// </summary>
    public static int SidesFor(EnemyKind kind) => For(kind).Sides;
}
