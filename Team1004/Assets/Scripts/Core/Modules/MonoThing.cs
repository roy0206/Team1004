using System;
using System.Collections.Generic;
using UnityEngine;

public class MonoThing : MonoBehaviour
{
    private readonly List<Module> modules = new();
    private readonly List<Module> updateModules = new();
    private readonly List<Module> lateUpdateModules = new();
    private readonly List<Module> fixedUpdateModules = new();
    private readonly List<Module> intervalModules = new();
    private readonly List<Module> tickBuffer = new();

    private float intervalDuration = 1f;
    private float intervalTimer;

    public IReadOnlyList<Module> Modules => modules;
    public int ModuleCount => modules.Count;

    public float IntervalDuration
    {
        get => intervalDuration;
        set
        {
            intervalDuration = Mathf.Max(0.01f, value);
            intervalTimer = 0f;
        }
    }

    private void Update()
    {
        TickModules(updateModules, ModuleTick.Update);
        TickInterval();
        OnThingUpdate();
    }

    private void LateUpdate()
    {
        TickModules(lateUpdateModules, ModuleTick.LateUpdate);
        OnThingLateUpdate();
    }

    private void FixedUpdate()
    {
        TickModules(fixedUpdateModules, ModuleTick.FixedUpdate);
        OnThingFixedUpdate();
    }

    private void OnDestroy()
    {
        ClearModules();
        OnThingDestroy();
    }

    public T AddModule<T>(T module) where T : Module
    {
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        if (module.IsAttached)
            throw new InvalidOperationException(
                $"Module is already attached to a host: '{module.GetType().Name}'.");

        modules.Add(module);
        RegisterTicks(module);
        module.Attach(this);
        return module;
    }

    public bool RemoveModule(Module module)
    {
        if (module == null || module.Host != this || !modules.Remove(module))
            return false;

        UnregisterTicks(module);
        module.Detach();
        return true;
    }

    public bool RemoveModule<T>() where T : Module
    {
        var module = GetModule<T>();
        return module != null && RemoveModule(module);
    }

    public int RemoveModules<T>() where T : Module
    {
        var targets = new List<Module>();

        for (var i = 0; i < modules.Count; i++)
            if (modules[i] is T)
                targets.Add(modules[i]);

        var removed = 0;

        for (var i = 0; i < targets.Count; i++)
            if (RemoveModule(targets[i]))
                removed++;

        return removed;
    }

    public void ClearModules()
    {
        if (modules.Count == 0)
            return;

        var detached = modules.ToArray();

        modules.Clear();
        updateModules.Clear();
        lateUpdateModules.Clear();
        fixedUpdateModules.Clear();
        intervalModules.Clear();

        for (var i = detached.Length - 1; i >= 0; i--)
            detached[i].Detach();
    }

    public T GetModule<T>() where T : Module
    {
        for (var i = 0; i < modules.Count; i++)
            if (modules[i] is T typed)
                return typed;

        return null;
    }

    public bool TryGetModule<T>(out T module) where T : Module
    {
        module = GetModule<T>();
        return module != null;
    }

    public bool HasModule<T>() where T : Module
    {
        return GetModule<T>() != null;
    }

    public int GetModules<T>(List<T> buffer) where T : Module
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        buffer.Clear();

        for (var i = 0; i < modules.Count; i++)
            if (modules[i] is T typed)
                buffer.Add(typed);

        return buffer.Count;
    }

    protected virtual void OnThingUpdate()
    {
    }

    protected virtual void OnThingLateUpdate()
    {
    }

    protected virtual void OnThingFixedUpdate()
    {
    }

    protected virtual void OnThingDestroy()
    {
    }

    private void TickInterval()
    {
        if (intervalModules.Count == 0)
            return;

        intervalTimer += Time.deltaTime;

        if (intervalTimer < intervalDuration)
            return;

        intervalTimer = Mathf.Min(intervalTimer - intervalDuration, intervalDuration);
        TickModules(intervalModules, ModuleTick.Interval);
    }

    private void TickModules(List<Module> source, ModuleTick kind)
    {
        if (source.Count == 0)
            return;

        tickBuffer.Clear();
        tickBuffer.AddRange(source);

        for (var i = 0; i < tickBuffer.Count; i++)
        {
            var module = tickBuffer[i];
            if (module.Host != this || !module.IsEnabled)
                continue;

            switch (kind)
            {
                case ModuleTick.Update:
                    module.InvokeUpdate();
                    break;
                case ModuleTick.LateUpdate:
                    module.InvokeLateUpdate();
                    break;
                case ModuleTick.FixedUpdate:
                    module.InvokeFixedUpdate();
                    break;
                case ModuleTick.Interval:
                    module.InvokeInterval();
                    break;
            }
        }

        tickBuffer.Clear();
    }

    private void RegisterTicks(Module module)
    {
        var ticks = module.TickMask;

        if ((ticks & ModuleTick.Update) != 0)
            updateModules.Add(module);

        if ((ticks & ModuleTick.LateUpdate) != 0)
            lateUpdateModules.Add(module);

        if ((ticks & ModuleTick.FixedUpdate) != 0)
            fixedUpdateModules.Add(module);

        if ((ticks & ModuleTick.Interval) != 0)
            intervalModules.Add(module);
    }

    private void UnregisterTicks(Module module)
    {
        updateModules.Remove(module);
        lateUpdateModules.Remove(module);
        fixedUpdateModules.Remove(module);
        intervalModules.Remove(module);
    }
}
