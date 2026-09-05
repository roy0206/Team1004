using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputManager : Singleton<InputManager>
{
    private sealed class ActionListeners
    {
        public InputAction Action;
        public Action Started;
        public Action Performed;
        public Action Canceled;
        public Action<InputAction.CallbackContext> StartedHandler;
        public Action<InputAction.CallbackContext> PerformedHandler;
        public Action<InputAction.CallbackContext> CanceledHandler;
    }

    private readonly Dictionary<string, InputAction> actions = new(StringComparer.Ordinal);
    private readonly HashSet<string> ambiguousNames = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, ActionListeners> listeners = new();
    private readonly HashSet<string> missingActionWarnings = new(StringComparer.Ordinal);
    private readonly HashSet<string> disabledActionWarnings = new(StringComparer.Ordinal);
    private readonly HashSet<string> gestureDeviceWarnings = new(StringComparer.Ordinal);

    private const string GestureLayoutName = "Gesture";
    private const string GestureBindingPrefix = "<Gesture>";

    private InputActionAsset asset;

    public bool IsInitialized => asset != null;

    protected override void OnUnregistering()
    {
        Shutdown();
    }

    public void Initialize(InputActionAsset inputActions)
    {
        if (inputActions == null)
            throw new ArgumentNullException(nameof(inputActions));

        if (asset == inputActions)
            return;

        if (asset != null)
            throw new InvalidOperationException("InputManager is already initialized.");

        RegisterActions(inputActions);
        asset = inputActions;
    }

    public void EnableMap(string map)
    {
        var actionMap = GetMapOrThrow(map);
        actionMap.Enable();
        disabledActionWarnings.Clear();
        WarnIfGestureDeviceMissing(actionMap);
    }

    public void DisableMap(string map)
    {
        GetMapOrThrow(map).Disable();
    }

    public bool IsMapEnabled(string map)
    {
        return TryGetMap(map, out var actionMap) && actionMap.enabled;
    }

    public bool IsPressed(string action)
    {
        var value = QueryAction(action);
        return value != null && value.IsPressed();
    }

    public bool WasPressedThisFrame(string action)
    {
        var value = QueryAction(action);
        return value != null && value.WasPressedThisFrame();
    }

    public bool WasReleasedThisFrame(string action)
    {
        var value = QueryAction(action);
        return value != null && value.WasReleasedThisFrame();
    }

    public T ReadValue<T>(string action) where T : struct
    {
        var value = QueryAction(action);
        return value == null ? default : value.ReadValue<T>();
    }

    public bool TryReadValue<T>(string action, out T value) where T : struct
    {
        if (TryGetAction(action, out var found))
        {
            value = found.ReadValue<T>();
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetAction(string action, out InputAction value)
    {
        if (!string.IsNullOrWhiteSpace(action) && actions.TryGetValue(action, out value))
            return true;

        value = null;
        return false;
    }

    public void AddListener(string action, InputPhase phase, Action callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var entry = GetOrCreateListeners(GetActionOrThrow(action));

        switch (phase)
        {
            case InputPhase.Started:
                entry.Started += callback;
                break;
            case InputPhase.Performed:
                entry.Performed += callback;
                break;
            case InputPhase.Canceled:
                entry.Canceled += callback;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase));
        }
    }

    public void RemoveListener(string action, InputPhase phase, Action callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (!TryGetAction(action, out var value) || !listeners.TryGetValue(value.id, out var entry))
            return;

        switch (phase)
        {
            case InputPhase.Started:
                entry.Started -= callback;
                break;
            case InputPhase.Performed:
                entry.Performed -= callback;
                break;
            case InputPhase.Canceled:
                entry.Canceled -= callback;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase));
        }
    }

    private void RegisterActions(InputActionAsset inputActions)
    {
        var maps = inputActions.actionMaps;

        for (var i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            var mapActions = map.actions;

            for (var j = 0; j < mapActions.Count; j++)
            {
                var action = mapActions[j];
                var path = $"{map.name}/{action.name}";

                if (!actions.TryAdd(path, action))
                    throw new InvalidOperationException($"Duplicate input action path: '{path}'.");

                RegisterShortName(action);
            }
        }
    }

    private void RegisterShortName(InputAction action)
    {
        if (ambiguousNames.Contains(action.name))
            return;

        if (actions.TryGetValue(action.name, out var existing))
        {
            if (existing == action)
                return;

            actions.Remove(action.name);
            ambiguousNames.Add(action.name);
            return;
        }

        actions.Add(action.name, action);
    }

    private ActionListeners GetOrCreateListeners(InputAction action)
    {
        if (listeners.TryGetValue(action.id, out var existing))
            return existing;

        var created = new ActionListeners { Action = action };
        created.StartedHandler = _ => created.Started?.Invoke();
        created.PerformedHandler = _ => created.Performed?.Invoke();
        created.CanceledHandler = _ => created.Canceled?.Invoke();

        action.started += created.StartedHandler;
        action.performed += created.PerformedHandler;
        action.canceled += created.CanceledHandler;

        listeners.Add(action.id, created);
        return created;
    }

    private InputAction QueryAction(string action)
    {
        if (TryGetAction(action, out var value))
        {
            WarnIfDisabled(action, value);
            return value;
        }

        if (missingActionWarnings.Add(action ?? string.Empty))
            Debug.LogWarning($"[InputManager] {DescribeMissingAction(action)}");

        return null;
    }

    private void WarnIfGestureDeviceMissing(InputActionMap map)
    {
        if (gestureDeviceWarnings.Contains(map.name) || !BindsGesture(map))
            return;

        if (InputSystem.GetDevice(GestureLayoutName) != null)
            return;

        gestureDeviceWarnings.Add(map.name);
        Debug.LogWarning(
            $"[InputManager] Input action map '{map.name}' binds '{GestureBindingPrefix}' controls " +
            "but no Gesture device exists. Call GestureManager.Initialize before EnableMap.");
    }

    private static bool BindsGesture(InputActionMap map)
    {
        var bindings = map.bindings;

        for (var i = 0; i < bindings.Count; i++)
        {
            var path = bindings[i].effectivePath;
            if (path != null && path.StartsWith(GestureBindingPrefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void WarnIfDisabled(string name, InputAction action)
    {
        if (action.enabled || !disabledActionWarnings.Add(name ?? string.Empty))
            return;

        Debug.LogWarning(
            $"[InputManager] Input action is not enabled: '{name}'. " +
            $"Call EnableMap(\"{action.actionMap.name}\") before reading it.");
    }

    private InputAction GetActionOrThrow(string action)
    {
        EnsureInitialized();

        if (TryGetAction(action, out var value))
            return value;

        throw new ArgumentException(DescribeMissingAction(action), nameof(action));
    }

    private InputActionMap GetMapOrThrow(string map)
    {
        EnsureInitialized();

        if (!TryGetMap(map, out var actionMap))
            throw new ArgumentException($"Input action map is not found: '{map}'.", nameof(map));

        return actionMap;
    }

    private bool TryGetMap(string map, out InputActionMap value)
    {
        value = asset != null && !string.IsNullOrWhiteSpace(map)
            ? asset.FindActionMap(map, false)
            : null;

        return value != null;
    }

    private string DescribeMissingAction(string action)
    {
        if (asset == null)
            return "InputManager is not initialized.";

        return ambiguousNames.Contains(action ?? string.Empty)
            ? $"Input action name is ambiguous: '{action}'. Use the 'Map/Action' form."
            : $"Input action is not found: '{action}'.";
    }

    private void Shutdown()
    {
        foreach (var entry in listeners.Values)
        {
            entry.Action.started -= entry.StartedHandler;
            entry.Action.performed -= entry.PerformedHandler;
            entry.Action.canceled -= entry.CanceledHandler;
        }

        listeners.Clear();
        actions.Clear();
        ambiguousNames.Clear();
        missingActionWarnings.Clear();
        disabledActionWarnings.Clear();

        if (asset == null)
            return;

        asset.Disable();
        asset = null;
    }

    private void EnsureInitialized()
    {
        if (asset == null)
            throw new InvalidOperationException("InputManager is not initialized.");
    }
}
