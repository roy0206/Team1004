using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneSettings", menuName = "Core/Scene Settings")]
public sealed class SceneSettings : ScriptableObject
{
    [SerializeField] private SceneTransitionValues transition = new();
    [SerializeField] private ScenePath bootstrapScene = new();
    [SerializeField] private List<SceneReference> scenes = new();

    public SceneTransitionValues Transition => transition;
    public ScenePath BootstrapScene => bootstrapScene;
    public IReadOnlyList<SceneReference> Scenes => scenes;

    public bool TryGetScene(string scenePath, out SceneReference scene)
    {
        if (!string.IsNullOrWhiteSpace(scenePath))
        {
            for (var i = 0; i < scenes.Count; i++)
            {
                var candidate = scenes[i];
                if (candidate != null &&
                    string.Equals(candidate.ScenePath, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scene = candidate;
                    return true;
                }
            }
        }

        scene = null;
        return false;
    }

    public bool TryGetSceneByName(string sceneName, out SceneReference scene)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            for (var i = 0; i < scenes.Count; i++)
            {
                var candidate = scenes[i];
                if (candidate != null && candidate.Scene != null &&
                    string.Equals(candidate.Scene.Name, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    scene = candidate;
                    return true;
                }
            }
        }

        scene = null;
        return false;
    }

    public SceneTransitionPlan Resolve(SceneReference scene)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));

        var values = scene.OverrideTransition ? scene.Transition : transition;
        return values.ToPlan(scene.ScenePath);
    }

    public SceneTransitionPlan Resolve(string scenePath)
    {
        return TryGetScene(scenePath, out var scene)
            ? Resolve(scene)
            : transition.ToPlan(scenePath);
    }

    public void Validate()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            if (scene == null)
                throw new InvalidOperationException($"[SceneSettings] Scene entry {i} is empty.");

            var path = scene.ScenePath;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"[SceneSettings] '{scene.name}' has no scene assigned.");

            if (!paths.Add(path))
                throw new InvalidOperationException($"[SceneSettings] Scene is registered twice: '{path}'.");

            if (!names.Add(scene.Scene.Name))
                throw new InvalidOperationException(
                    $"[SceneSettings] Scene name is registered twice: '{scene.Scene.Name}'. " +
                    "TryGetSceneByName cannot tell them apart.");
        }

        if (!bootstrapScene.IsValid)
            return;

        if (paths.Contains(bootstrapScene.Path))
            throw new InvalidOperationException(
                $"[SceneSettings] Bootstrap scene must not be registered in Scenes: '{bootstrapScene.Path}'. " +
                "The Bootstrap scene is the entry point, not a transition target.");
    }

    private void OnValidate()
    {
        try
        {
            Validate();
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(exception.Message, this);
        }
    }
}
