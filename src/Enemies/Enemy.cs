using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Enemy that follows a path of waypoints from left to right.
/// Rendered as a red circle with dark border.
/// </summary>
public partial class Enemy : Node2D
{
    [Signal]
    public delegate void ReachedEndEventHandler(Enemy enemy);

    [Signal]
    public delegate void DestroyedEventHandler(Enemy enemy);

    private List<Vector2> _waypoints = new();
    private int _currentWaypointIndex = 0;
    private float _currentHP;
    private float _speed;
    private bool _isDead = false;

    public bool IsDead => _isDead;

    public override void _Ready()
    {
        _currentHP = GameConstants.EnemyHP;
        _speed = GameConstants.EnemySpeed;

        // Draw the enemy as a red circle using a Control with custom drawing.
        // Offset the container so the circle drawn at (radius, radius) centers on the Enemy's origin.
        float radius = GameConstants.EnemyDiameter / 2f;
        var circleContainer = new Control();
        circleContainer.Name = "CircleContainer";
        circleContainer.Position = new Vector2(-radius, -radius);
        circleContainer.Size = new Vector2(GameConstants.EnemyDiameter, GameConstants.EnemyDiameter);
        AddChild(circleContainer);

        circleContainer.Draw += () => DrawEnemyCircle(circleContainer);
    }

    private void DrawEnemyCircle(Control container)
    {
        // Draw filled red circle
        container.DrawCircle(
            new Vector2(GameConstants.EnemyDiameter / 2f, GameConstants.EnemyDiameter / 2f),
            GameConstants.EnemyDiameter / 2f,
            new Color(0.9f, 0.1f, 0.1f) // Red fill
        );

        // Draw dark border
        container.DrawCircle(
            new Vector2(GameConstants.EnemyDiameter / 2f, GameConstants.EnemyDiameter / 2f),
            GameConstants.EnemyDiameter / 2f - 2,
            new Color(0.8f, 0.05f, 0.05f), // Slightly lighter inner
            false,
            2.0f // Border width
        );
    }

    /// <summary>
    /// Sets the path waypoints for this enemy to follow.
    /// </summary>
    public void SetPath(List<Vector2> waypoints)
    {
        _waypoints = waypoints;
        _currentWaypointIndex = 0;

        if (_waypoints.Count > 0)
        {
            Position = _waypoints[0];
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
        Vector2 direction = (target - Position).Normalized();
        float distanceToWaypoint = Position.DistanceTo(target);
        float moveDistance = _speed * GameConstants.CellSize * delta;

        if (moveDistance >= distanceToWaypoint)
        {
            // Arrived at waypoint, move to next
            Position = target;
            _currentWaypointIndex++;
        }
        else
        {
            Position += direction * moveDistance;
        }
    }

    /// <summary>
    /// Reset this enemy's state for reuse from the object pool.
    /// Called after Acquire before repositioning.
    /// </summary>
    public void ResetForPool()
    {
        _waypoints.Clear();
        _currentWaypointIndex = 0;
        _currentHP = GameConstants.EnemyHP;
        _speed = GameConstants.EnemySpeed;
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
