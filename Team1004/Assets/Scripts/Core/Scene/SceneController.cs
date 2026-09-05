using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneController : Singleton<SceneController>
{
    [SerializeField] private SceneSettings settings;
    [SerializeField] private MonoBehaviour transitionSource;

    private readonly List<ISceneTransitionListener> listeners = new();
    private readonly Dictionary<string, SceneReference> additiveScenes = new(StringComparer.OrdinalIgnoreCase);
    private ISceneTransition transition;

    public SceneSettings Settings => settings;
    public bool IsTransitioning { get; private set; }
    public float LoadingProgress { get; private set; }
    public SceneReference CurrentScene { get; private set; }

    protected override void OnRegistered()
    {
        transition = transitionSource as ISceneTransition;

        if (transitionSource != null && transition == null)
            Debug.LogError("[SceneController] Transition Source must implement ISceneTransition.", transitionSource);

        if (transitionSource != null && !transitionSource.transform.IsChildOf(transform))
            Debug.LogError(
                "[SceneController] Transition Source must be on the SceneController or one of its children. " +
                "It will be destroyed with its scene otherwise.",
                transitionSource);

        if (settings != null)
            settings.Validate();
    }

    protected override void OnUnregistering()
    {
        listeners.Clear();
        additiveScenes.Clear();
        CurrentScene = null;
        transition = null;
    }

    public void RegisterListener(ISceneTransitionListener listener)
    {
        if (listener != null && !listeners.Contains(listener))
            listeners.Add(listener);
    }

    public void UnregisterListener(ISceneTransitionListener listener)
    {
        listeners.Remove(listener);
    }

    public bool TryGetScene(string scenePath, out SceneReference scene)
    {
        if (settings != null)
            return settings.TryGetScene(scenePath, out scene);

        scene = null;
        return false;
    }

    public bool TryGetSceneByName(string sceneName, out SceneReference scene)
    {
        if (settings != null)
            return settings.TryGetSceneByName(sceneName, out scene);

        scene = null;
        return false;
    }

    public async Awaitable LoadAsync(SceneReference reference)
    {
        var plan = ResolvePlan(reference);
        if (!TryBeginTransition(plan.TargetScenePath))
            return;

        var fromScene = SceneManager.GetActiveScene();
        var context = new SceneTransitionContext(
            fromScene.IsValid() ? fromScene.name : string.Empty,
            reference.Scene.Name);
        var covered = false;

        try
        {
            NotifyLeaving(context);
            await CoverAsync(plan.CoverDuration);
            covered = true;

            await TeardownLoadedAsync(context);

            var useLoadingScene = CanUseLoadingScene(plan);
            if (useLoadingScene)
            {
                await LoadImmediateAsync(plan.LoadingScenePath, LoadSceneMode.Single);
                await Awaitable.NextFrameAsync();
                await RevealAsync(plan.RevealDuration);
                covered = false;
            }

            await reference.PrepareAsync(context);

            var operation = SceneManager.LoadSceneAsync(plan.TargetScenePath, LoadSceneMode.Single);
            if (operation == null)
                throw new InvalidOperationException($"Failed to load scene '{plan.TargetScenePath}'.");

            operation.allowSceneActivation = false;
            LoadingProgress = 0f;
            var elapsed = 0f;

            while (operation.progress < 0.9f || elapsed < plan.MinimumLoadingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                LoadingProgress = Mathf.Clamp01(operation.progress / 0.9f);
                await Awaitable.NextFrameAsync();
            }

            LoadingProgress = 1f;

            if (useLoadingScene)
            {
                await CoverAsync(plan.CoverDuration);
                covered = true;
            }

            operation.allowSceneActivation = true;
            while (!operation.isDone)
                await Awaitable.NextFrameAsync();

            CurrentScene = reference;

            await Awaitable.NextFrameAsync();
            await RevealAsync(plan.RevealDuration);
            covered = false;
            NotifyEntered(context);
        }
        finally
        {
            if (covered)
                await RevealAsync(plan.RevealDuration);

            EndTransition();
        }
    }

    public async Awaitable<Scene> LoadAdditiveAsync(SceneReference reference)
    {
        return await LoadAdditiveAsync(reference, AdditiveLoadOptions.Default);
    }

    public async Awaitable<Scene> LoadAdditiveAsync(SceneReference reference, AdditiveLoadOptions options)
    {
        var plan = ResolvePlan(reference);
        if (!TryBeginTransition(plan.TargetScenePath))
            return default;

        var covered = false;

        try
        {
            var existing = SceneManager.GetSceneByPath(plan.TargetScenePath);
            if (existing.IsValid() && existing.isLoaded)
            {
                Debug.LogWarning($"[SceneController] Scene is already loaded: '{plan.TargetScenePath}'.");
                return existing;
            }

            if (options.UseTransition)
            {
                await CoverAsync(plan.CoverDuration);
                covered = true;
            }

            var activeScene = SceneManager.GetActiveScene();
            var context = new SceneTransitionContext(
                activeScene.IsValid() ? activeScene.name : string.Empty,
                reference.Scene.Name);

            await reference.PrepareAsync(context);

            var operation = SceneManager.LoadSceneAsync(plan.TargetScenePath, LoadSceneMode.Additive);
            if (operation == null)
                throw new InvalidOperationException($"Failed to load scene '{plan.TargetScenePath}'.");

            LoadingProgress = 0f;
            while (!operation.isDone)
            {
                LoadingProgress = Mathf.Clamp01(operation.progress);
                await Awaitable.NextFrameAsync();
            }

            LoadingProgress = 1f;
            await Awaitable.NextFrameAsync();

            var loadedScene = SceneManager.GetSceneByPath(plan.TargetScenePath);
            additiveScenes[plan.TargetScenePath] = reference;

            if (options.MakeActive && loadedScene.IsValid())
                SceneManager.SetActiveScene(loadedScene);

            if (options.UseTransition)
            {
                await RevealAsync(plan.RevealDuration);
                covered = false;
            }

            return loadedScene;
        }
        finally
        {
            if (covered)
                await RevealAsync(plan.RevealDuration);

            EndTransition();
        }
    }

    public async Awaitable UnloadAdditiveAsync(Scene scene)
    {
        await UnloadAdditiveAsync(scene, AdditiveUnloadOptions.Default);
    }

    public async Awaitable UnloadAdditiveAsync(Scene scene, AdditiveUnloadOptions options)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        if (!TryBeginTransition(scene.path))
            return;

        var plan = ResolvePlan(scene.path);
        var covered = false;

        try
        {
            if (options.UseTransition)
            {
                await CoverAsync(plan.CoverDuration);
                covered = true;
            }

            if (additiveScenes.Remove(scene.path, out var reference))
                await reference.TeardownAsync(new SceneTransitionContext(scene.name, string.Empty));

            if (SceneManager.GetActiveScene() == scene)
                RestoreActiveScene(scene);

            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
                throw new InvalidOperationException($"Failed to unload scene '{scene.name}'.");

            LoadingProgress = 0f;
            while (!operation.isDone)
            {
                LoadingProgress = Mathf.Clamp01(operation.progress);
                await Awaitable.NextFrameAsync();
            }

            LoadingProgress = 1f;

            if (options.UseTransition)
            {
                await RevealAsync(plan.RevealDuration);
                covered = false;
            }
        }
        finally
        {
            if (covered)
                await RevealAsync(plan.RevealDuration);

            EndTransition();
        }
    }

    private async Awaitable TeardownLoadedAsync(SceneTransitionContext context)
    {
        var leaving = CurrentScene;
        CurrentScene = null;

        if (leaving != null)
            await leaving.TeardownAsync(context);

        if (additiveScenes.Count == 0)
            return;

        var additive = new List<SceneReference>(additiveScenes.Values);
        additiveScenes.Clear();

        for (var i = 0; i < additive.Count; i++)
            await additive[i].TeardownAsync(new SceneTransitionContext(additive[i].Scene.Name, context.ToScene));
    }

    private async Awaitable LoadImmediateAsync(string scenePath, LoadSceneMode mode)
    {
        var operation = SceneManager.LoadSceneAsync(scenePath, mode);
        if (operation == null)
            throw new InvalidOperationException($"Failed to load scene '{scenePath}'.");

        while (!operation.isDone)
            await Awaitable.NextFrameAsync();
    }

    private async Awaitable CoverAsync(float duration)
    {
        if (transition == null)
            return;

        try
        {
            await transition.CoverAsync(duration);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private async Awaitable RevealAsync(float duration)
    {
        if (transition == null)
            return;

        try
        {
            await transition.RevealAsync(duration);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private SceneTransitionPlan ResolvePlan(SceneReference reference)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        var scene = reference.Scene;
        if (scene == null || !scene.IsValid)
            throw new InvalidOperationException($"[SceneController] '{reference.name}' has no scene assigned.");

        if (!Application.CanStreamedLevelBeLoaded(scene.Path))
            throw new InvalidOperationException(
                $"[SceneController] Scene is not included in the active Build Profile: '{scene.Path}'.");

        return settings != null
            ? settings.Resolve(reference)
            : (reference.OverrideTransition ? reference.Transition : new SceneTransitionValues()).ToPlan(scene.Path);
    }

    private SceneTransitionPlan ResolvePlan(string scenePath)
    {
        return settings != null
            ? settings.Resolve(scenePath)
            : new SceneTransitionValues().ToPlan(scenePath);
    }

    private bool TryBeginTransition(string scenePath)
    {
        if (IsTransitioning)
        {
            Debug.LogWarning($"[SceneController] Another scene operation is already active. Ignored '{scenePath}'.");
            return false;
        }

        IsTransitioning = true;
        LoadingProgress = 0f;
        return true;
    }

    private void EndTransition()
    {
        IsTransitioning = false;
    }

    private static bool CanUseLoadingScene(SceneTransitionPlan plan)
    {
        return plan.UseLoadingScene &&
               !string.IsNullOrWhiteSpace(plan.LoadingScenePath) &&
               !string.Equals(plan.LoadingScenePath, plan.TargetScenePath, StringComparison.OrdinalIgnoreCase) &&
               Application.CanStreamedLevelBeLoaded(plan.LoadingScenePath);
    }

    private static void RestoreActiveScene(Scene leaving)
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var candidate = SceneManager.GetSceneAt(i);
            if (candidate == leaving || !candidate.isLoaded)
                continue;

            SceneManager.SetActiveScene(candidate);
            return;
        }
    }

    private void NotifyLeaving(SceneTransitionContext context)
    {
        Notify(context, true);
    }

    private void NotifyEntered(SceneTransitionContext context)
    {
        Notify(context, false);
    }

    private void Notify(SceneTransitionContext context, bool leaving)
    {
        var snapshot = listeners.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
        {
            var listener = snapshot[i];
            if (listener == null)
                continue;

            if (listener is UnityEngine.Object unityObject && unityObject == null)
            {
                listeners.Remove(listener);
                continue;
            }

            try
            {
                if (leaving)
                    listener.OnSceneLeaving(context);
                else
                    listener.OnSceneEntered(context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
