using System;
using System.Collections.Generic;
using Game.Animation;
using Game.View;
using UnityEngine;

namespace Game.Cutscene
{
    public sealed class CutsceneContext
    {
        private readonly Dictionary<string, CutsceneActor> lookup = new(StringComparer.Ordinal);
        private readonly HashSet<string> missingWarnings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CustomAnimation> clips = new(StringComparer.Ordinal);

        private GameCamera rigHost;
        private bool rigHostResolved;

        public CutsceneContext(
            Camera stageCamera,
            IReadOnlyList<CutsceneActor> actors,
            CutsceneDialogueView dialogue,
            GameObject link)
            : this(stageCamera, actors, dialogue, link, null)
        {
        }

        public CutsceneContext(
            Camera stageCamera,
            IReadOnlyList<CutsceneActor> actors,
            CutsceneDialogueView dialogue,
            GameObject link,
            IReadOnlyList<CutsceneClipEntry> clipEntries)
        {
            StageCamera = stageCamera;
            Dialogue = dialogue;
            Link = link;

            if (clipEntries != null)
            {
                for (var i = 0; i < clipEntries.Count; i++)
                {
                    var entry = clipEntries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.Id) || entry.Clip == null)
                        continue;

                    clips[entry.Id] = entry.Clip;
                }
            }

            if (actors == null)
                return;

            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null)
                    continue;

                if (string.IsNullOrEmpty(actor.Id))
                {
                    Debug.LogWarning("[Cutscene] Cutscene actor has an empty id and was ignored.", actor);
                    continue;
                }

                if (!lookup.TryAdd(actor.Id, actor))
                    Debug.LogWarning($"[Cutscene] Duplicate cutscene actor id: '{actor.Id}'. The first one is used.", actor);
            }
        }

        public Camera StageCamera { get; }
        public CutsceneDialogueView Dialogue { get; }
        public GameObject Link { get; }

        public CameraRig Rig
        {
            get
            {
                if (!rigHostResolved)
                {
                    rigHostResolved = true;

                    if (StageCamera != null)
                        rigHost = StageCamera.GetComponent<GameCamera>();
                }

                return rigHost != null ? rigHost.Rig : null;
            }
        }

        public CutsceneActor Player => Actor(CutsceneActorIds.Player);
        public CutsceneActor Boss => Actor(CutsceneActorIds.Boss);
        public CutsceneActor Landmark => Actor(CutsceneActorIds.Landmark);
        public CutsceneActor Child => Actor(CutsceneActorIds.Child);

        public CutsceneActor Actor(string id)
        {
            if (TryGetActor(id, out var actor))
                return actor;

            if (missingWarnings.Add(id ?? string.Empty))
                Debug.LogWarning(
                    $"[Cutscene] Cutscene actor is not found: '{id}'. Add it to the CutscenePlayer actor list.",
                    Link);

            return null;
        }

        public CustomAnimation Clip(string id)
        {
            if (TryGetClip(id, out var clip))
                return clip;

            if (missingWarnings.Add("clip:" + (id ?? string.Empty)))
                Debug.LogWarning(
                    $"[Cutscene] Cutscene clip is not found: '{id}'. Add it to the CutscenePlayer clip list.",
                    Link);

            return null;
        }

        public bool TryGetClip(string id, out CustomAnimation clip)
        {
            if (!string.IsNullOrEmpty(id) && clips.TryGetValue(id, out clip) && clip != null)
                return true;

            clip = null;
            return false;
        }

        public bool TryGetActor(string id, out CutsceneActor actor)
        {
            if (!string.IsNullOrEmpty(id) && lookup.TryGetValue(id, out actor) && actor != null)
                return true;

            actor = null;
            return false;
        }
    }
}
