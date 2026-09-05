using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Cutscene
{
    public sealed class CutscenePlaybackModule : Module
    {
        private readonly Camera stageCamera;
        private readonly IReadOnlyList<CutsceneActor> actors;
        private readonly CutsceneDialogueView dialogueView;
        private readonly GameObject link;
        private readonly string cutsceneMap;
        private readonly string advanceAction;
        private readonly IReadOnlyList<string> mapsToDisable;
        private readonly IReadOnlyList<CutsceneClipEntry> clips;

        private readonly List<string> disabledMaps = new();

        private CutsceneContext context;
        private bool listening;
        private bool cutsceneMapEnabledHere;
        private bool inputWarned;

        public bool IsPlaying { get; private set; }
        public CutsceneBase Current { get; private set; }

        public event Action Started;
        public event Action Finished;

        public CutscenePlaybackModule(
            Camera stageCamera,
            IReadOnlyList<CutsceneActor> actors,
            CutsceneDialogueView dialogueView,
            GameObject link,
            string cutsceneMap,
            string advanceAction,
            IReadOnlyList<string> mapsToDisable)
            : this(stageCamera, actors, dialogueView, link, cutsceneMap, advanceAction, mapsToDisable, null)
        {
        }

        public CutscenePlaybackModule(
            Camera stageCamera,
            IReadOnlyList<CutsceneActor> actors,
            CutsceneDialogueView dialogueView,
            GameObject link,
            string cutsceneMap,
            string advanceAction,
            IReadOnlyList<string> mapsToDisable,
            IReadOnlyList<CutsceneClipEntry> clips)
        {
            this.clips = clips ?? Array.Empty<CutsceneClipEntry>();
            this.stageCamera = stageCamera;
            this.actors = actors ?? Array.Empty<CutsceneActor>();
            this.dialogueView = dialogueView;
            this.link = link;
            this.cutsceneMap = cutsceneMap ?? string.Empty;
            this.advanceAction = advanceAction ?? string.Empty;
            this.mapsToDisable = mapsToDisable ?? Array.Empty<string>();
        }

        protected override ModuleTick Ticks => ModuleTick.None;

        protected override void OnDetached()
        {
            var current = Current;
            if (current != null)
                current.Cancel();
        }

        public async Awaitable PlayAsync(CutsceneBase cutscene)
        {
            if (cutscene == null)
                throw new ArgumentNullException(nameof(cutscene));

            if (IsPlaying)
                throw new InvalidOperationException($"A cutscene is already playing: '{Current.Id}'.");

            IsPlaying = true;
            Current = cutscene;
            cutscene.Begin(EnsureContext());
            AcquireInput();
            Raise(Started);

            try
            {
                await cutscene.ExecuteAsync();
            }
            finally
            {
                ReleaseInput();
                IsPlaying = false;
                Current = null;
                Raise(Finished);
            }
        }

        public void ApplyFinalState(CutsceneBase cutscene)
        {
            if (cutscene == null)
                throw new ArgumentNullException(nameof(cutscene));

            if (IsPlaying)
            {
                Debug.LogWarning(
                    "[CutscenePlayer] ApplyFinalState was ignored because a cutscene is playing. Call Skip() instead.",
                    link);
                return;
            }

            ApplyFinalStateAsync(cutscene);
        }

        public void Advance()
        {
            var current = Current;
            if (current != null)
                current.Advance();
        }

        public void Skip()
        {
            var current = Current;
            if (current != null)
                current.Skip();
        }

        public bool TryGetActor(string id, out CutsceneActor actor)
        {
            return EnsureContext().TryGetActor(id, out actor);
        }

        private async void ApplyFinalStateAsync(CutsceneBase cutscene)
        {
            try
            {
                cutscene.Begin(EnsureContext());
                cutscene.Skip();
                await cutscene.ExecuteAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, link);
            }
        }

        private CutsceneContext EnsureContext()
        {
            return context ??= new CutsceneContext(stageCamera, actors, dialogueView, link, clips);
        }

        private void AcquireInput()
        {
            disabledMaps.Clear();
            listening = false;
            cutsceneMapEnabledHere = false;

            if (!InputManager.TryGetInstance(out var input) || !input.IsInitialized)
            {
                WarnInput("InputManager is not initialized. Enter and mouse advance are disabled; call Advance() from code.");
                return;
            }

            for (var i = 0; i < mapsToDisable.Count; i++)
            {
                var map = mapsToDisable[i];
                if (!string.IsNullOrEmpty(map) && input.IsMapEnabled(map))
                {
                    input.DisableMap(map);
                    disabledMaps.Add(map);
                }
            }

            try
            {
                if (!input.IsMapEnabled(cutsceneMap))
                {
                    input.EnableMap(cutsceneMap);
                    cutsceneMapEnabledHere = true;
                }

                input.AddListener(advanceAction, InputPhase.Performed, OnAdvanceInput);
                listening = true;
            }
            catch (ArgumentException exception)
            {
                WarnInput($"Input map '{cutsceneMap}' or action '{advanceAction}' is missing. " +
                          "Add them to GameInput.inputactions (see Docs/Cutscene.md). " + exception.Message);
            }
        }

        private void ReleaseInput()
        {
            if (!InputManager.TryGetInstance(out var input) || !input.IsInitialized)
            {
                listening = false;
                cutsceneMapEnabledHere = false;
                disabledMaps.Clear();
                return;
            }

            if (listening)
            {
                input.RemoveListener(advanceAction, InputPhase.Performed, OnAdvanceInput);
                listening = false;
            }

            if (cutsceneMapEnabledHere)
            {
                input.DisableMap(cutsceneMap);
                cutsceneMapEnabledHere = false;
            }

            for (var i = 0; i < disabledMaps.Count; i++)
                input.EnableMap(disabledMaps[i]);

            disabledMaps.Clear();
        }

        private void OnAdvanceInput()
        {
            if (Time.timeScale <= 0f)
                return;

            Advance();
        }

        private void WarnInput(string message)
        {
            if (inputWarned)
                return;

            inputWarned = true;
            Debug.LogWarning($"[CutscenePlayer] {message}", link);
        }

        private void Raise(Action handler)
        {
            try
            {
                handler?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, link);
            }
        }
    }
}
