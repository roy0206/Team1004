using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using Game.Animation;
using Game.Dialogue;
using UnityEngine;

namespace Game.Cutscene
{
    public abstract class CutsceneBase
    {
        private enum CutsceneMode
        {
            Idle,
            Running,
            Skipping,
            Cancelled
        }

        private sealed class Pending
        {
            public Sequence Sequence;
            public readonly AwaitableCompletionSource Source = new();
            public bool Finished;
            public bool WaitingForAdvance;
            public float AutoAdvanceDelay = DialogueLine.WaitForAdvance;
            public Action<bool> Release;

            public void Finish()
            {
                if (Finished)
                    return;

                Finished = true;
                Source.TrySetResult();
            }
        }

        private const int ShakeVibrato = 10;
        private const float ShakeRandomness = 90f;

        private static readonly string[] NoLineIds = Array.Empty<string>();

        private readonly List<Pending> active = new();

        private CutsceneContext context;
        private CutsceneMode mode = CutsceneMode.Idle;
        private Vector3 cameraPosition;
        private float cameraOrthoSize;
        private bool cameraCaptured;
        private bool dialogueWarned;

        public abstract string Id { get; }

        public virtual IReadOnlyList<string> LineIds => NoLineIds;

        public bool IsRunning => mode == CutsceneMode.Running || mode == CutsceneMode.Skipping;
        public bool IsSkipping => mode == CutsceneMode.Skipping;
        public bool IsCancelled => mode == CutsceneMode.Cancelled;

        protected virtual float DefaultCharsPerSecond => 30f;
        protected virtual bool RestoreCameraOnFinish => true;

        protected CutsceneContext Context => context;
        protected Camera StageCamera => context != null ? context.StageCamera : null;
        protected CutsceneDialogueView Dialogue => context != null ? context.Dialogue : null;
        protected CutsceneActor Player => Actor(CutsceneActorIds.Player);
        protected CutsceneActor Boss => Actor(CutsceneActorIds.Boss);
        protected CutsceneActor Landmark => Actor(CutsceneActorIds.Landmark);
        protected CutsceneActor Child => Actor(CutsceneActorIds.Child);
        protected CustomAnimation PlayerJumpClip => Clip(CutsceneClipIds.PlayerJump);
        protected CustomAnimation PlayerSwimClip => Clip(CutsceneClipIds.PlayerSwim);
        protected CustomAnimation PlayerLaneUpClip => Clip(CutsceneClipIds.PlayerLaneUp);
        protected CustomAnimation PlayerLaneDownClip => Clip(CutsceneClipIds.PlayerLaneDown);

        public void Begin(CutsceneContext cutsceneContext)
        {
            if (cutsceneContext == null)
                throw new ArgumentNullException(nameof(cutsceneContext));

            if (IsRunning)
                throw new InvalidOperationException($"Cutscene '{Id}' is already running.");

            context = cutsceneContext;
            mode = CutsceneMode.Running;
            cameraCaptured = false;
            dialogueWarned = false;
            active.Clear();

            var camera = cutsceneContext.StageCamera;
            if (camera == null)
                return;

            cameraPosition = camera.transform.position;
            cameraOrthoSize = camera.orthographicSize;
            cameraCaptured = true;
        }

        public Awaitable ExecuteAsync()
        {
            if (context == null)
                throw new InvalidOperationException($"Begin() must run before ExecuteAsync(): '{Id}'.");

            return RunAndCleanupAsync();
        }

        public void Advance()
        {
            if (mode != CutsceneMode.Running)
                return;

            var dialogue = Dialogue;

            if (dialogue != null && dialogue.IsTyping)
            {
                dialogue.CompleteTyping();
                return;
            }

            if (dialogue != null)
                dialogue.SetHintVisible(false);

            ReleaseActive(true);
        }

        public void Skip()
        {
            if (mode != CutsceneMode.Running)
                return;

            mode = CutsceneMode.Skipping;

            var dialogue = Dialogue;
            if (dialogue != null)
                dialogue.HideImmediate();

            ReleaseActive(true);
        }

        public void Cancel()
        {
            if (!IsRunning)
                return;

            mode = CutsceneMode.Cancelled;
            ReleaseActive(false);
        }

        protected abstract Awaitable Run();

        private async Awaitable RunAndCleanupAsync()
        {
            try
            {
                await Run();
            }
            finally
            {
                Cleanup();
            }
        }

        protected CutsceneActor Actor(string id)
        {
            return context != null ? context.Actor(id) : null;
        }

        protected CustomAnimation Clip(string id)
        {
            return context != null ? context.Clip(id) : null;
        }

        protected async Awaitable Animate(CutsceneActor actor, CustomAnimation clip, float duration = 0f)
        {
            if (actor == null || clip == null || mode == CutsceneMode.Cancelled)
                return;

            var animator = actor.Animator;
            if (animator == null)
            {
                WarnNoAnimator(actor);
                return;
            }

            if (mode != CutsceneMode.Running)
            {
                ApplyFinalPose(animator, clip, duration);
                return;
            }

            var pending = new Pending
            {
                Release = complete =>
                {
                    if (complete)
                        ApplyFinalPose(animator, clip, duration);
                    else
                        animator.Stop();
                }
            };
            active.Add(pending);
            WatchAnimation(pending, animator.PlayAsync(clip, duration));

            await WaitFor(pending);
        }

        protected async Awaitable Say(string lineId)
        {
            if (mode == CutsceneMode.Cancelled)
                return;

            var dialogue = Dialogue;
            if (dialogue == null)
            {
                WarnNoDialogue();
                return;
            }

            if (mode != CutsceneMode.Running)
            {
                dialogue.HideImmediate();
                return;
            }

            var line = DialogueService.Get(lineId);
            var charsPerSecond = line.CharsPerSecond > 0f ? line.CharsPerSecond : DefaultCharsPerSecond;

            var pending = new Pending { AutoAdvanceDelay = line.AutoAdvanceDelay };
            active.Add(pending);
            dialogue.Show(line.Speaker, line.Text, charsPerSecond, () => OnTypingCompleted(pending));

            await WaitFor(pending);
        }

        protected async Awaitable Move(
            CutsceneActor actor,
            Vector2 target,
            float duration = 0f,
            Ease ease = Ease.InOutSine,
            bool relative = false)
        {
            if (actor == null || mode == CutsceneMode.Cancelled)
                return;

            var destination = Resolve(actor.transform.position, target, relative);

            if (duration <= 0f || mode != CutsceneMode.Running)
            {
                actor.transform.position = destination;
                return;
            }

            await Play(DOTween.Sequence().Append(actor.transform.DOMove(destination, duration).SetEase(ease)));
        }

        protected async Awaitable FadeActor(
            CutsceneActor actor,
            float alpha,
            float duration = 0f,
            Ease ease = Ease.Linear)
        {
            if (actor == null || mode == CutsceneMode.Cancelled)
                return;

            var view = actor.View;

            if (!view.HasRenderer)
            {
                Debug.LogWarning($"[Cutscene] Actor '{actor.Id}' has no SpriteRenderer to fade.", actor);
                return;
            }

            var target = Mathf.Clamp01(alpha);

            if (duration <= 0f || mode != CutsceneMode.Running)
            {
                view.Alpha = target;
                return;
            }

            DOGetter<Color> getter = () => view.Color;
            DOSetter<Color> setter = value => view.Color = value;

            await Play(DOTween.Sequence().Append(DOTween.ToAlpha(getter, setter, target, duration).SetEase(ease)));
        }

        protected async Awaitable CameraTo(
            Vector2 position,
            float orthoSize = 0f,
            float duration = 0f,
            Ease ease = Ease.InOutSine,
            bool relative = false)
        {
            if (mode == CutsceneMode.Cancelled)
                return;

            var camera = StageCamera;
            if (camera == null)
            {
                WarnNoCamera();
                return;
            }

            var cameraTransform = camera.transform;
            var destination = Resolve(cameraTransform.position, position, relative);

            if (duration <= 0f || mode != CutsceneMode.Running)
            {
                cameraTransform.position = destination;

                if (orthoSize > 0f)
                    camera.orthographicSize = orthoSize;

                return;
            }

            var sequence = DOTween.Sequence().Append(cameraTransform.DOMove(destination, duration).SetEase(ease));

            if (orthoSize > 0f)
                sequence.Join(camera.DOOrthoSize(orthoSize, duration).SetEase(ease));

            await Play(sequence);
        }

        protected async Awaitable Shake(float duration, float strength)
        {
            if (mode != CutsceneMode.Running || duration <= 0f || strength <= 0f)
                return;

            var camera = StageCamera;
            if (camera == null)
            {
                WarnNoCamera();
                return;
            }

            await Play(DOTween.Sequence().Append(camera.DOShakePosition(
                duration, strength, ShakeVibrato, ShakeRandomness, true, ShakeRandomnessMode.Full)));
        }

        protected async Awaitable FadeScreen(float alpha, float duration = 0f, Ease ease = Ease.Linear)
        {
            if (mode == CutsceneMode.Cancelled)
                return;

            var dialogue = Dialogue;
            if (dialogue == null || !dialogue.HasScreenFade)
            {
                WarnNoDialogue();
                return;
            }

            if (duration <= 0f || mode != CutsceneMode.Running)
            {
                dialogue.SetScreenAlpha(alpha);
                return;
            }

            var tween = dialogue.CreateScreenFade(alpha, duration, ease);
            if (tween == null)
            {
                dialogue.SetScreenAlpha(alpha);
                return;
            }

            await Play(DOTween.Sequence().Append(tween));
        }

        protected async Awaitable Wait(float seconds)
        {
            if (mode != CutsceneMode.Running || seconds <= 0f)
                return;

            await Play(DOTween.Sequence().AppendInterval(seconds));
        }

        protected async Awaitable Together(params Awaitable[] awaitables)
        {
            if (awaitables == null)
                return;

            for (var i = 0; i < awaitables.Length; i++)
            {
                var awaitable = awaitables[i];
                if (awaitable != null)
                    await awaitable;
            }
        }

        protected async Awaitable Play(Sequence sequence)
        {
            if (sequence == null)
                return;

            if (mode != CutsceneMode.Running)
            {
                if (!sequence.IsActive())
                    return;

                if (mode == CutsceneMode.Skipping)
                    sequence.Complete(true);
                else
                    sequence.Kill(false);

                return;
            }

            var pending = new Pending();
            active.Add(pending);
            Attach(pending, sequence);

            await WaitFor(pending);
        }

#pragma warning disable CS1998
        protected async Awaitable Show(CutsceneActor actor)
        {
            if (actor != null && mode != CutsceneMode.Cancelled)
                actor.SetVisible(true);
        }

        protected async Awaitable Hide(CutsceneActor actor)
        {
            if (actor != null && mode != CutsceneMode.Cancelled)
                actor.SetVisible(false);
        }

        protected async Awaitable SetSprite(CutsceneActor actor, Sprite sprite)
        {
            if (actor != null && mode != CutsceneMode.Cancelled)
                actor.SetSprite(sprite);
        }

        protected async Awaitable SetPose(CutsceneActor actor, CustomAnimation clip, int frameIndex)
        {
            if (actor == null || clip == null || mode == CutsceneMode.Cancelled)
                return;

            var animator = actor.Animator;
            if (animator == null)
            {
                WarnNoAnimator(actor);
                return;
            }

            animator.SetFrame(clip, frameIndex);
        }
#pragma warning restore CS1998

        private static void ApplyFinalPose(SpriteAnimatorModule animator, CustomAnimation clip, float duration)
        {
            if (clip.Loop)
                animator.Play(clip, duration);
            else
                animator.SetFrame(clip, clip.FrameCount - 1);
        }

        private static async void WatchAnimation(Pending pending, Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            pending.Finish();
        }

        private async Awaitable WaitFor(Pending pending)
        {
            if (!pending.Finished)
                await pending.Source.Awaitable;

            active.Remove(pending);
        }

        private Sequence Attach(Pending pending, Sequence sequence)
        {
            pending.Sequence = sequence;

            var link = context != null ? context.Link : null;
            if (link != null)
                sequence.SetLink(link);

            return sequence.OnComplete(pending.Finish).OnKill(pending.Finish);
        }

        private void OnTypingCompleted(Pending pending)
        {
            if (pending.Finished || mode != CutsceneMode.Running)
                return;

            var delay = pending.AutoAdvanceDelay;

            if (delay < 0f)
            {
                pending.WaitingForAdvance = true;

                var dialogue = Dialogue;
                if (dialogue != null)
                    dialogue.SetHintVisible(true);

                return;
            }

            if (delay <= 0f)
            {
                pending.Finish();
                return;
            }

            Attach(pending, DOTween.Sequence().AppendInterval(delay));
        }

        private void ReleaseActive(bool complete)
        {
            if (active.Count == 0)
                return;

            var snapshot = active.ToArray();
            active.Clear();

            for (var i = 0; i < snapshot.Length; i++)
            {
                var pending = snapshot[i];
                if (pending.Finished)
                    continue;

                pending.Finished = true;

                var release = pending.Release;
                if (release != null)
                {
                    try
                    {
                        release(complete);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                var sequence = pending.Sequence;
                if (sequence == null || !sequence.IsActive())
                    continue;

                if (complete)
                    sequence.Complete(true);
                else
                    sequence.Kill(false);
            }

            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i].Source.TrySetResult();
        }

        private void Cleanup()
        {
            ReleaseActive(mode != CutsceneMode.Cancelled);

            var dialogue = Dialogue;
            if (dialogue != null)
                dialogue.HideImmediate();

            if (RestoreCameraOnFinish && cameraCaptured && mode != CutsceneMode.Cancelled)
            {
                var camera = StageCamera;
                if (camera != null)
                {
                    camera.transform.position = cameraPosition;
                    camera.orthographicSize = cameraOrthoSize;
                }
            }

            active.Clear();
            mode = CutsceneMode.Idle;
            context = null;
        }

        private void WarnNoCamera()
        {
            Debug.LogWarning($"[Cutscene] '{Id}' has no stage camera. The camera helper was skipped.", LinkObject());
        }

        private void WarnNoAnimator(CutsceneActor actor)
        {
            Debug.LogWarning($"[Cutscene] Actor '{actor.Id}' has no SpriteRenderer to animate.", actor);
        }

        private void WarnNoDialogue()
        {
            if (dialogueWarned)
                return;

            dialogueWarned = true;
            Debug.LogWarning($"[Cutscene] '{Id}' has no dialogue view. Dialogue and screen fades are skipped.", LinkObject());
        }

        private GameObject LinkObject()
        {
            return context != null ? context.Link : null;
        }

        private static Vector3 Resolve(Vector3 current, Vector2 target, bool relative)
        {
            if (relative)
                return current + new Vector3(target.x, target.y, 0f);

            return new Vector3(target.x, target.y, current.z);
        }
    }
}
