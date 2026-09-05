using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.View
{
    public sealed class CameraFxDirector : DomainSingleton<CameraFxDirector>
    {
        [SerializeField] private GameCamera gameCamera;
        [SerializeField] private CameraCue[] cues;

        private readonly Dictionary<string, CameraCue> lookup = new(StringComparer.Ordinal);
        private bool lookupBuilt;

        public CameraRig Rig
        {
            get
            {
                if (gameCamera == null)
                    gameCamera = GetComponent<GameCamera>();

                return gameCamera != null ? gameCamera.Rig : null;
            }
        }

        public CameraShakeModule ShakeModule => Rig?.Shake;
        public CameraZoomModule ZoomModule => Rig?.Zoom;
        public CameraPanModule PanModule => Rig?.Pan;

        public void Shake(float trauma)
        {
            ShakeModule?.AddTrauma(trauma);
        }

        public void Shake(float duration, float strength)
        {
            ShakeModule?.ShakeFor(duration, strength);
        }

        public void Punch(Vector2 direction, float strength, float duration)
        {
            ShakeModule?.Punch(direction, strength, duration);
        }

        public void Zoom(float factor, float duration, CameraEase ease = CameraEase.SineInOut)
        {
            ZoomModule?.ZoomTo(factor, duration, ease);
        }

        public void ZoomPunch(
            float factor,
            float inDuration,
            float holdDuration,
            float outDuration,
            CameraEase ease = CameraEase.SineInOut)
        {
            ZoomModule?.ZoomPunch(factor, inDuration, holdDuration, outDuration, ease);
        }

        public void Pan(Vector2 offset, float duration, CameraEase ease = CameraEase.SineInOut)
        {
            PanModule?.PanTo(offset, duration, ease);
        }

        public void PanPulse(
            Vector2 offset,
            float inDuration,
            float holdDuration,
            float outDuration,
            CameraEase ease = CameraEase.SineInOut)
        {
            PanModule?.PanPulse(offset, inDuration, holdDuration, outDuration, ease);
        }

        public void PlayCue(CameraCue cue)
        {
            if (cue == null)
                return;

            var rig = Rig;

            if (rig == null)
                return;

            cue.Apply(rig);
        }

        public void PlayCue(CameraCue cue, Vector2 punchDirection)
        {
            if (cue == null)
                return;

            var rig = Rig;

            if (rig == null)
                return;

            cue.Apply(rig, punchDirection);
        }

        public bool PlayCue(string cueId)
        {
            if (!TryGetCue(cueId, out var cue))
                return false;

            PlayCue(cue);
            return true;
        }

        public bool TryGetCue(string cueId, out CameraCue cue)
        {
            BuildLookup();

            if (!string.IsNullOrEmpty(cueId) && lookup.TryGetValue(cueId, out cue) && cue != null)
                return true;

            cue = null;
            return false;
        }

        public void ResetEffects()
        {
            Rig?.ResetEffects();
        }

        public void ResetAll()
        {
            Rig?.ResetAll();
        }

        public static bool TryPlay(string cueId)
        {
            return TryGetCurrent(out var director) && director.PlayCue(cueId);
        }

        public static void TryShake(float trauma)
        {
            if (TryGetCurrent(out var director))
                director.Shake(trauma);
        }

        private void BuildLookup()
        {
            if (lookupBuilt)
                return;

            lookupBuilt = true;

            if (cues == null)
                return;

            for (var i = 0; i < cues.Length; i++)
            {
                var cue = cues[i];

                if (cue == null)
                    continue;

                if (!lookup.TryAdd(cue.CueId, cue))
                    Debug.LogWarning($"[CameraFx] Duplicate camera cue id: '{cue.CueId}'. The first one is used.", this);
            }
        }
    }
}
