using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PoolManager : Singleton<PoolManager>
{
    private sealed class Pool
    {
        public GameObject Prefab;
        public Vector3 PrefabScale;
        public readonly Stack<InstanceState> Available = new();
        public readonly HashSet<InstanceState> Active = new();
    }

    private sealed class InstanceState
    {
        public string Key;
        public GameObject GameObject;
        public Transform Transform;
        public IPooledObject[] Callbacks;
        public int Generation;
        public bool IsActive;
    }

    private struct PendingRelease
    {
        public InstanceState State;
        public int Generation;
        public float DueTime;
    }

    private readonly Dictionary<string, Pool> pools = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> labels = new(StringComparer.Ordinal);
    private readonly Dictionary<GameObject, InstanceState> instances = new();
    private readonly List<PendingRelease> pendingReleases = new();
    private readonly List<PendingRelease> dueReleases = new();
    private readonly List<string> keyBuffer = new();
    private readonly HashSet<string> missingKeyWarnings = new(StringComparer.Ordinal);

    private Transform poolRoot;
    private int maxPooledPerKey;
    private bool useUnscaledTime;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public int RegisteredKeyCount => pools.Count;
    public int ActiveInstanceCount
    {
        get
        {
            var count = 0;
            foreach (var pool in pools.Values)
                count += pool.Active.Count;

            return count;
        }
    }
    public int MaxPooledPerKey => maxPooledPerKey;
    public bool UseUnscaledTime => useUnscaledTime;

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    protected override void OnRegistered()
    {
        var root = new GameObject("PoolRoot");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        poolRoot = root.transform;
    }

    protected override void OnUnregistering()
    {
        ClearAll();
        poolRoot = null;
        isInitialized = false;
    }

    private void Update()
    {
        if (pendingReleases.Count == 0)
            return;

        var now = CurrentTime;
        dueReleases.Clear();

        for (var i = pendingReleases.Count - 1; i >= 0; i--)
        {
            var pending = pendingReleases[i];
            var isDestroyed = pending.State.GameObject == null;

            if (!isDestroyed && now < pending.DueTime)
                continue;

            pendingReleases.RemoveAt(i);
            dueReleases.Add(pending);
        }

        for (var i = 0; i < dueReleases.Count; i++)
        {
            var pending = dueReleases[i];
            if (pending.State.Generation == pending.Generation)
                ReturnToPool(pending.State);
        }

        dueReleases.Clear();
    }

    public void Initialize(PoolSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        if (isInitialized)
            throw new InvalidOperationException("PoolManager is already initialized.");

        if (pools.Count > 0)
            throw new InvalidOperationException("PoolManager cannot be initialized after a pool is registered.");

        settings.Validate();
        maxPooledPerKey = settings.MaxPooledPerKey;
        useUnscaledTime = settings.UseUnscaledTime;
        isInitialized = true;
    }

    public void Register(string key, GameObject prefab, int prewarmCount = 0)
    {
        ValidateKey(key);

        if (prefab == null)
            throw new ArgumentNullException(nameof(prefab));

        if (pools.TryGetValue(key, out var pool))
        {
            if (pool.Prefab != prefab)
                throw new InvalidOperationException($"Pool key is already assigned to another prefab: '{key}'.");
        }
        else
        {
            pool = new Pool
            {
                Prefab = prefab,
                PrefabScale = prefab.transform.localScale
            };

            pools.Add(key, pool);
            missingKeyWarnings.Remove(key);
        }

        if (prewarmCount > 0)
            Prewarm(key, prewarmCount);
    }

    public void RegisterLabel(string label, int prewarmCount = 0)
    {
        ValidateLabel(label);

        if (!ResourceManager.TryGetInstance(out var resourceManager))
            throw new InvalidOperationException("ResourceManager is required to register a resource label.");

        if (!resourceManager.IsLabelLoaded(label))
            throw new InvalidOperationException($"Resource label is not loaded: '{label}'.");

        var resourceKeys = resourceManager.GetKeys(label);
        var prefabs = new List<KeyValuePair<string, GameObject>>(resourceKeys.Count);

        for (var i = 0; i < resourceKeys.Count; i++)
        {
            var key = resourceKeys[i];
            if (!resourceManager.TryGet(key, out GameObject prefab))
                continue;

            if (pools.TryGetValue(key, out var existing) && existing.Prefab != prefab)
                throw new InvalidOperationException($"Pool key is already assigned to another prefab: '{key}'.");

            prefabs.Add(new KeyValuePair<string, GameObject>(key, prefab));
        }

        if (prefabs.Count == 0)
            throw new InvalidOperationException($"Resource label contains no prefab: '{label}'.");

        var registeredKeys = new List<string>(prefabs.Count);

        for (var i = 0; i < prefabs.Count; i++)
        {
            Register(prefabs[i].Key, prefabs[i].Value, prewarmCount);
            registeredKeys.Add(prefabs[i].Key);
        }

        labels[label] = registeredKeys.AsReadOnly();
    }

    public void Prewarm(string key, int count)
    {
        if (count <= 0)
            return;

        if (!TryGetPool(key, true, out var pool))
            return;

        if (pool.Prefab == null)
        {
            Debug.LogWarning($"[PoolManager] Pool prefab was destroyed: '{key}'.");
            return;
        }

        var target = maxPooledPerKey > 0
            ? Mathf.Min(count, maxPooledPerKey - pool.Available.Count)
            : count;

        for (var i = 0; i < target; i++)
            pool.Available.Push(CreateInstance(key, pool));
    }

    public PooledInstance Get(string key, Transform parent = null)
    {
        var state = Acquire(key, true);
        return state == null ? PooledInstance.None : Spawn(state, parent);
    }

    public PooledInstance Get(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var state = Acquire(key, true);
        return state == null ? PooledInstance.None : Spawn(state, position, rotation, parent);
    }

    public bool TryGet(string key, out PooledInstance instance, Transform parent = null)
    {
        var state = Acquire(key, false);
        if (state == null)
        {
            instance = PooledInstance.None;
            return false;
        }

        instance = Spawn(state, parent);
        return true;
    }

    public bool TryGet(string key, Vector3 position, Quaternion rotation, out PooledInstance instance, Transform parent = null)
    {
        var state = Acquire(key, false);
        if (state == null)
        {
            instance = PooledInstance.None;
            return false;
        }

        instance = Spawn(state, position, rotation, parent);
        return true;
    }

    public void Release(PooledInstance instance)
    {
        if (!instance.IsValid)
            return;

        if (!TryGetActiveState(instance, out var state))
            return;

        ReturnToPool(state);
    }

    public void Release(PooledInstance instance, float delay)
    {
        if (!instance.IsValid)
            return;

        if (delay <= 0f)
        {
            Release(instance);
            return;
        }

        if (!TryGetActiveState(instance, out var state))
            return;

        ScheduleRelease(state, delay);
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        if (!TryGetActiveState(instance, out var state))
            return;

        ReturnToPool(state);
    }

    public void Release(GameObject instance, float delay)
    {
        if (instance == null)
            return;

        if (delay <= 0f)
        {
            Release(instance);
            return;
        }

        if (!TryGetActiveState(instance, out var state))
            return;

        ScheduleRelease(state, delay);
    }

    public void ReleaseActive()
    {
        pendingReleases.Clear();

        var states = new List<InstanceState>();
        foreach (var pool in pools.Values)
            states.AddRange(pool.Active);

        for (var i = 0; i < states.Count; i++)
            ReturnToPool(states[i]);
    }

    public void Clear(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !pools.Remove(key, out var pool))
            return;

        DestroyPoolInstances(key, pool);
        missingKeyWarnings.Remove(key);
    }

    public void ClearLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label) || !labels.Remove(label, out var registeredKeys))
            return;

        for (var i = 0; i < registeredKeys.Count; i++)
            Clear(registeredKeys[i]);
    }

    public void ClearAll()
    {
        pendingReleases.Clear();

        keyBuffer.Clear();
        foreach (var key in pools.Keys)
            keyBuffer.Add(key);

        for (var i = 0; i < keyBuffer.Count; i++)
            if (pools.Remove(keyBuffer[i], out var pool))
                DestroyPoolInstances(keyBuffer[i], pool);

        keyBuffer.Clear();
        pools.Clear();
        labels.Clear();
        instances.Clear();
        missingKeyWarnings.Clear();
    }

    public bool IsRegistered(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && pools.ContainsKey(key);
    }

    public bool IsActive(PooledInstance instance)
    {
        return instance.IsValid &&
               instances.TryGetValue(instance.GameObject, out var state) &&
               state.IsActive &&
               state.Generation == instance.Generation;
    }

    public bool IsPooled(GameObject instance)
    {
        return instance != null && instances.ContainsKey(instance);
    }

    public int GetAvailableCount(string key)
    {
        return TryGetPool(key, false, out var pool) ? pool.Available.Count : 0;
    }

    public int GetActiveCount(string key)
    {
        return TryGetPool(key, false, out var pool) ? pool.Active.Count : 0;
    }

    public IReadOnlyList<string> GetLabelKeys(string label)
    {
        return !string.IsNullOrWhiteSpace(label) && labels.TryGetValue(label, out var registeredKeys)
            ? registeredKeys
            : Array.Empty<string>();
    }

    private InstanceState Acquire(string key, bool warnOnMissing)
    {
        if (!TryGetPool(key, warnOnMissing, out var pool))
            return null;

        InstanceState state = null;

        while (pool.Available.Count > 0)
        {
            var candidate = pool.Available.Pop();
            if (candidate.GameObject == null)
            {
                instances.Remove(candidate.GameObject);
                continue;
            }

            state = candidate;
            break;
        }

        if (state == null)
        {
            if (pool.Prefab == null)
            {
                Debug.LogWarning($"[PoolManager] Pool prefab was destroyed: '{key}'.");
                return null;
            }

            state = CreateInstance(key, pool);
        }

        state.Generation++;
        state.IsActive = true;
        pool.Active.Add(state);
        return state;
    }

    private InstanceState CreateInstance(string key, Pool pool)
    {
        var instance = Instantiate(pool.Prefab, poolRoot);
        instance.name = key;
        instance.SetActive(false);

        var state = new InstanceState
        {
            Key = key,
            GameObject = instance,
            Transform = instance.transform,
            Callbacks = instance.GetComponentsInChildren<IPooledObject>(true)
        };

        instances.Add(instance, state);
        return state;
    }

    private PooledInstance Spawn(InstanceState state, Transform parent)
    {
        Place(state, parent);
        state.Transform.localPosition = Vector3.zero;
        state.Transform.localRotation = Quaternion.identity;
        return Activate(state);
    }

    private PooledInstance Spawn(InstanceState state, Vector3 position, Quaternion rotation, Transform parent)
    {
        Place(state, parent);
        state.Transform.SetPositionAndRotation(position, rotation);
        return Activate(state);
    }

    private static void Place(InstanceState state, Transform parent)
    {
        state.Transform.SetParent(parent, false);

        if (parent == null)
            SceneManager.MoveGameObjectToScene(state.GameObject, SceneManager.GetActiveScene());
    }

    private PooledInstance Activate(InstanceState state)
    {
        state.GameObject.SetActive(true);

        for (var i = 0; i < state.Callbacks.Length; i++)
            state.Callbacks[i].OnSpawned();

        return new PooledInstance(state.GameObject, state.Transform, state.Key, state.Generation);
    }

    private void ScheduleRelease(InstanceState state, float delay)
    {
        pendingReleases.Add(new PendingRelease
        {
            State = state,
            Generation = state.Generation,
            DueTime = CurrentTime + delay
        });
    }

    private void ReturnToPool(InstanceState state)
    {
        if (!state.IsActive)
            return;

        state.IsActive = false;
        pools.TryGetValue(state.Key, out var pool);
        pool?.Active.Remove(state);

        var instance = state.GameObject;

        if (instance == null)
        {
            instances.Remove(instance);
            return;
        }

        for (var i = 0; i < state.Callbacks.Length; i++)
            state.Callbacks[i].OnReleased();

        instance.SetActive(false);
        state.Transform.SetParent(poolRoot, false);

        if (pool == null)
        {
            instances.Remove(instance);
            Destroy(instance);
            return;
        }

        state.Transform.localScale = pool.PrefabScale;

        if (maxPooledPerKey > 0 && pool.Available.Count >= maxPooledPerKey)
        {
            instances.Remove(instance);
            Destroy(instance);
            return;
        }

        pool.Available.Push(state);
    }

    private void DestroyPoolInstances(string key, Pool pool)
    {
        while (pool.Available.Count > 0)
            DestroyInstance(pool.Available.Pop());

        var states = new List<InstanceState>(pool.Active);
        pool.Active.Clear();

        for (var i = 0; i < states.Count; i++)
        {
            states[i].IsActive = false;
            DestroyInstance(states[i]);
        }
    }

    private void DestroyInstance(InstanceState state)
    {
        var instance = state.GameObject;
        instances.Remove(instance);

        if (instance != null)
            Destroy(instance);
    }

    private bool TryGetActiveState(PooledInstance instance, out InstanceState state)
    {
        if (!instances.TryGetValue(instance.GameObject, out state))
        {
            WarnForeignInstance(instance.GameObject);
            return false;
        }

        if (state.Generation != instance.Generation)
        {
            state = null;
            return false;
        }

        return true;
    }

    private bool TryGetActiveState(GameObject instance, out InstanceState state)
    {
        if (instances.TryGetValue(instance, out state))
            return true;

        WarnForeignInstance(instance);
        return false;
    }

    private bool TryGetPool(string key, bool warnOnMissing, out Pool pool)
    {
        if (!string.IsNullOrWhiteSpace(key) && pools.TryGetValue(key, out pool))
            return true;

        if (warnOnMissing && missingKeyWarnings.Add(key ?? string.Empty))
            Debug.LogWarning($"[PoolManager] Pool is not registered: '{key}'.");

        pool = null;
        return false;
    }

    private static void WarnForeignInstance(GameObject instance)
    {
        Debug.LogWarning($"[PoolManager] Instance was not created by the pool: '{instance.name}'.", instance);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Pool key is empty.", nameof(key));
    }

    private static void ValidateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Resource label is empty.", nameof(label));
    }
}
