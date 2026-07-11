using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Simple object pool for frequently spawned/despawned Node types.
/// Pre-allocates instances that can be Acquired and Released back to the pool.
/// </summary>
public class ObjectPool<T> where T : Node2D, new()
{
    private readonly Stack<T> _available = new();
    private readonly Node2D _parent;
    private int _instanceCount = 0;

    public ObjectPool(int size, Node2D parent)
    {
        _parent = parent;
        for (int i = 0; i < size; i++)
        {
            var obj = new T();
            obj.Name = $"{typeof(T).Name}_{_instanceCount++}";
            _parent.AddChild(obj);
            Deactivate(obj);
            _available.Push(obj);
        }
    }

    /// <summary>
    /// Acquire an instance from the pool. The instance is activated (visible, processing).
    /// </summary>
    public T Acquire()
    {
        T obj;
        if (_available.Count > 0)
        {
            obj = _available.Pop();
        }
        else
        {
            // Pool exhausted -- create a new one as fallback
            obj = new T();
            obj.Name = $"{typeof(T).Name}_{_instanceCount++}";
            _parent.AddChild(obj);
        }
        Activate(obj);
        return obj;
    }

    /// <summary>
    /// Release an instance back to the pool. The instance is deactivated (hidden, disabled).
    /// </summary>
    public void Release(T obj)
    {
        if (!GodotObject.IsInstanceValid(obj))
            return;

        Deactivate(obj);
        _available.Push(obj);
    }

    private static void Activate(T obj)
    {
        obj.Visible = true;
        obj.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private static void Deactivate(T obj)
    {
        obj.Visible = false;
        obj.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}
