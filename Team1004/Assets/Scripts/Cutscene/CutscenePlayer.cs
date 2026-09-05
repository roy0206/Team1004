using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Cutscene
{
    public sealed class CutscenePlayer : MonoThing
    {
        [SerializeField] private Camera stageCamera;
        [SerializeField] private CutsceneDialogueView dialogueView;
        [SerializeField] private CutsceneActor[] actors = Array.Empty<CutsceneActor>();
        [SerializeField] private string cutsceneMap = "Cutscene";
        [SerializeField] private string advanceAction = "Cutscene/Advance";
        [SerializeField] private string[] mapsToDisable = { "Player" };
        [SerializeField] private CutsceneClipEntry[] clips = Array.Empty<CutsceneClipEntry>();

        private CutscenePlaybackModule playback;

        public bool IsPlaying => playback != null && playback.IsPlaying;
        public CutsceneBase Current => playback != null ? playback.Current : null;
        public Camera StageCamera => stageCamera;
        public CutsceneDialogueView DialogueView => dialogueView;
        public IReadOnlyList<CutsceneActor> Actors => actors;
        public IReadOnlyList<CutsceneClipEntry> Clips => clips;

        public event Action Started
        {
            add => EnsurePlayback().Started += value;
            remove
            {
                if (playback != null)
                    playback.Started -= value;
            }
        }

        public event Action Finished
        {
            add => EnsurePlayback().Finished += value;
            remove
            {
                if (playback != null)
                    playback.Finished -= value;
            }
        }

        private void Awake()
        {
            if (stageCamera == null)
            {
                stageCamera = Camera.main;
                if (stageCamera != null)
                    Debug.LogWarning("[CutscenePlayer] Stage camera is not assigned. Camera.main is used.", this);
            }

            if (dialogueView != null)
                dialogueView.SetWorldCamera(stageCamera);

            EnsurePlayback();
        }

        public Awaitable PlayAsync(CutsceneBase cutscene)
        {
            return EnsurePlayback().PlayAsync(cutscene);
        }

        public Awaitable PlayAsync(string cutsceneId)
        {
            return PlayAsync(CutsceneCatalog.Create(cutsceneId));
        }

        public void Advance()
        {
            if (playback != null)
                playback.Advance();
        }

        public void Skip()
        {
            if (playback != null)
                playback.Skip();
        }

        public void ApplyFinalState(CutsceneBase cutscene)
        {
            EnsurePlayback().ApplyFinalState(cutscene);
        }

        public void ApplyFinalState(string cutsceneId)
        {
            var cutscene = CutsceneCatalog.Create(cutsceneId);
            if (cutscene != null)
                ApplyFinalState(cutscene);
        }

        public bool TryGetActor(string id, out CutsceneActor actor)
        {
            return EnsurePlayback().TryGetActor(id, out actor);
        }

        private CutscenePlaybackModule EnsurePlayback()
        {
            if (playback != null && playback.IsAttached)
                return playback;

            playback = AddModule(new CutscenePlaybackModule(
                stageCamera, actors, dialogueView, gameObject, cutsceneMap, advanceAction, mapsToDisable, clips));
            return playback;
        }
    }
}
