using System;
using UnityEngine;

public readonly struct PooledInstance : IEquatable<PooledInstance>
{
    public static PooledInstance None => default;

    public GameObject GameObject { get; }
    public Transform Transform { get; }
    public string Key { get; }

    internal int Generation { get; }

    public bool IsValid => GameObject != null;

    internal PooledInstance(GameObject gameObject, Transform transform, string key, int generation)
    {
        GameObject = gameObject;
        Transform = transform;
        Key = key;
        Generation = generation;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        if (GameObject != null)
            return GameObject.TryGetComponent(out component);

        component = null;
        return false;
    }

    public bool Equals(PooledInstance other)
    {
        return GameObject == other.GameObject && Generation == other.Generation;
    }

    public override bool Equals(object obj)
    {
        return obj is PooledInstance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GameObject, Generation);
    }

    public static bool operator ==(PooledInstance left, PooledInstance right) => left.Equals(right);
    public static bool operator !=(PooledInstance left, PooledInstance right) => !left.Equals(right);
}
