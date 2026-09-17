using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Identifies an enemy variant. Basic is a red circle; Swarm is a smaller orange circle.
/// </summary>
public enum EnemyKind
{
    Basic,
    Swarm
}

/// <summary>
/// Enemy that follows a path of waypoints from left to right.
/// Movement and damage logic are shared across kinds; stats and visuals are set via Configure.
/// </summary>
public partial class Enemy : Node2D
{
    [Signal]
    public delegate void ReachedEndEventHandler(Enemy enemy);

    [Signal]
    public delegate void DestroyedEventHandler(Enemy enemy);

    private List<Vector2> _waypoints = new();
    private int _currentWaypointIndex = 0;
    // The path anchor is the point on the waypoint path that this enemy follows.
    // Position is always anchor + FormationOffset, so a formation offset survives
    // waypoint arrival snaps and stays constant for the entire path.
    private Vector2 _anchorPosition = Vector2.Zero;
    private Vector2 _formationOffset = Vector2.Zero;
    private float _currentHP;
    private float _maxHP;
    private float _speed;
    private int _diameter;
    private Color _fillColor;
    private Color _borderColor;
    private bool _isDead = false;
    private Control? _circleContainer;

    /// <summary>
    /// The kind this enemy is currently configured as.
    /// </summary>
    public EnemyKind Kind { get; private set; } = EnemyKind.Basic;

    public bool IsDead => _isDead;

    /// <summary>
    /// Coin value awarded when this enemy is destroyed.
    /// </summary>
    public int CoinDrop { get; private set; } = GameConstants.CoinDropPerKill;

    /// <summary>
    /// Collision radius in pixels (half of the rendered diameter).
    /// </summary>
    public float CollisionRadius => _diameter / 2f;

    /// <summary>
    /// Rendered diameter in pixels.
    /// </summary>
    public float Diameter => _diameter;

    /// <summary>
    /// Constant offset from the path anchor applied to Position every tick.
    /// Zero for basic enemies; non-zero for swarm cluster members.
    /// </summary>
    public Vector2 FormationOffset => _formationOffset;

    public override void _Ready()
    {
        ApplyConfig(EnemyKind.Basic);
        CreateVisual();
    }

    private void ApplyConfig(EnemyKind kind)
    {
        Kind = kind;

        switch (kind)
        {
            case EnemyKind.Basic:
                _maxHP = GameConstants.EnemyHP;
                _speed = GameConstants.EnemySpeed;
                _diameter = GameConstants.EnemyDiameter;
                _fillColor = new Color(0.9f, 0.1f, 0.1f);   // Red fill
                _borderColor = new Color(0.8f, 0.05f, 0.05f); // Darker red border
                CoinDrop = GameConstants.CoinDropPerKill;
                break;

            case EnemyKind.Swarm:
                _maxHP = GameConstants.SwarmEnemyHP;
                _speed = GameConstants.SwarmEnemySpeed;
                _diameter = GameConstants.SwarmEnemyDiameter;
                _fillColor = new Color(1f, 0.55f, 0.1f);     // Orange fill
                _borderColor = new Color(0.85f, 0.4f, 0.05f); // Darker orange border
                CoinDrop = GameConstants.SwarmCoinDropPerKill;
                break;
        }

        _currentHP = _maxHP;
        _isDead = false;
    }

    /// <summary>
    /// Configures this enemy for a specific kind. Idempotent and safe to call on pooled reuse.
    /// </summary>
    public void Configure(EnemyKind kind)
    {
        ApplyConfig(kind);
        UpdateVisual();
    }

    private void CreateVisual()
    {
        _circleContainer = new Control();
        _circleContainer.Name = "CircleContainer";
        _circleContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_circleContainer);
        _circleContainer.Draw += () => DrawEnemyCircle(_circleContainer);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_circleContainer == null) return;

        float radius = _diameter / 2f;
        _circleContainer.Position = new Vector2(-radius, -radius);
        _circleContainer.Size = new Vector2(_diameter, _diameter);
        _circleContainer.QueueRedraw();
    }

    private void DrawEnemyCircle(Control container)
    {
        if (!IsInstanceValid(container)) return;

        float radius = _diameter / 2f;
        Vector2 center = new Vector2(_diameter / 2f, _diameter / 2f);

        // Filled circle
        container.DrawCircle(center, radius, _fillColor);

        // Dark border
        container.DrawCircle(center, radius - 2, _borderColor, false, 2.0f);
    }

    /// <summary>
    /// Sets the formation offset applied to this enemy relative to its path anchor.
    /// Set before SetPath so the initial anchor placement already includes the offset.
    /// </summary>
    public void SetFormationOffset(Vector2 offset)
    {
        _formationOffset = offset;

        // If a path is already active, keep Position consistent with the new offset.
        if (_waypoints.Count > 0)
            Position = _anchorPosition + _formationOffset;
    }

    /// <summary>
    /// Sets the path waypoints for this enemy to follow.
    /// The cached waypoint list is treated as read-only and shared across enemies;
    /// ResetForPool reassigns (never clears) so it can't mutate the shared cache.
    /// </summary>
    public void SetPath(List<Vector2> waypoints)
    {
        _waypoints = waypoints;
        _currentWaypointIndex = 0;

        if (_waypoints.Count > 0)
        {
            _anchorPosition = _waypoints[0];
            Position = _anchorPosition + _formationOffset;
        }
    }

    public override void _Process(double delta)
    {
        if (_isDead || _waypoints.Count == 0)
            return;

        MoveAlongPath((float)delta);
    }

    private void MoveAlongPath(float delta)
    {
        if (_currentWaypointIndex >= _waypoints.Count)
        {
            _isDead = true;
            EmitSignal(SignalName.ReachedEnd, this);
            return;
        }

        Vector2 target = _waypoints[_currentWaypointIndex];
        Vector2 toTarget = target - _anchorPosition;
        float distanceToWaypoint = toTarget.Length();
        float moveDistance = _speed * GameConstants.CellSize * delta;

        if (moveDistance >= distanceToWaypoint)
        {
            // Anchor arrives at waypoint; the formation offset survives because
            // Position is recomputed from the anchor below, not snapped to target.
            _anchorPosition = target;
            _currentWaypointIndex++;
        }
        else
        {
            // Guard near-zero distance so Normalized() never produces NaN.
            Vector2 direction = distanceToWaypoint > 0.0001f
                ? toTarget / distanceToWaypoint
                : Vector2.Zero;
            _anchorPosition += direction * moveDistance;
        }

        Position = _anchorPosition + _formationOffset;
    }

    /// <summary>
    /// Reset this enemy's state for reuse from the object pool.
    /// Called after Acquire before repositioning. HP is restored from the current kind config
    /// (or the basic default if never configured).
    /// </summary>
    public void ResetForPool()
    {
        // Reassign to a fresh empty list rather than Clear() — the previous list may
        // be a shared cached path reference owned by GridManager.
        _waypoints = new List<Vector2>();
        _currentWaypointIndex = 0;
        // Clear formation state so a pooled swarm member can't leak its offset
        // into a later basic-enemy reuse.
        _formationOffset = Vector2.Zero;
        _anchorPosition = Vector2.Zero;
        _currentHP = _maxHP > 0 ? _maxHP : GameConstants.EnemyHP;
        _isDead = false;
        Position = Vector2.Zero;
    }

    /// <summary>
    /// Apply damage to this enemy. If HP reaches 0, enemy is destroyed.
    /// The Destroyed signal is emitted; GameManager handles release to pool.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _currentHP -= damage;

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            _isDead = true;
            EmitSignal(SignalName.Destroyed, this);
            // GameManager.OnEnemyDestroyed handles release to pool
        }
    }
}
