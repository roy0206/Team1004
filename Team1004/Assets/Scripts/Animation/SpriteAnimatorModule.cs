using System;
using UnityEngine;

namespace Game.Animation
{
    public sealed class SpriteAnimatorModule : Module
    {
        private readonly SpriteRenderer spriteRenderer;
        private readonly bool useUnscaledTime;
        private readonly FlipbookClock clock = new FlipbookClock();

        private CustomAnimation current;
        private Sprite appliedSprite;
        private AwaitableCompletionSource completion;
        private float requestedLength;
        private float speed = 1f;
        private bool isPlaying;

        public SpriteAnimatorModule(SpriteRenderer spriteRenderer)
            : this(spriteRenderer, false)
        {
        }

        public SpriteAnimatorModule(SpriteRenderer spriteRenderer, bool useUnscaledTime)
        {
            this.spriteRenderer = spriteRenderer;
            this.useUnscaledTime = useUnscaledTime;
        }

        public bool UseUnscaledTime => useUnscaledTime;
        public bool IsPlaying => isPlaying;
        public CustomAnimation Current => current;
        public int CurrentFrame => current != null ? clock.Frame : 0;
        public float NormalizedTime => current != null ? clock.NormalizedTime : 0f;
        public float PlaybackLength => requestedLength;

        public float Speed
        {
            get => speed;
            set
            {
                speed = value < 0f ? 0f : value;
                clock.Speed = speed;
            }
        }

        public event Action<string> FrameEvent;
        public event Action<CustomAnimation> Completed;

        protected override ModuleTick Ticks => ModuleTick.Update;

        public void Play(CustomAnimation clip)
        {
            Play(clip, 0f);
        }

        public void Play(CustomAnimation clip, float duration)
        {
            if (clip == null)
            {
                Stop();
                return;
            }

            var length = ResolveLength(clip, duration);

            if (isPlaying && ReferenceEquals(clip, current) && Mathf.Approximately(length, requestedLength))
                return;

            ReleaseCompletion();
            Begin(clip, length);
        }

        public Awaitable PlayAsync(CustomAnimation clip)
        {
            return PlayAsync(clip, 0f);
        }

        public Awaitable PlayAsync(CustomAnimation clip, float duration)
        {
            if (clip == null)
                return CompletedAwaitable();

            var length = ResolveLength(clip, duration);
            var sameClip = isPlaying && ReferenceEquals(clip, current) && Mathf.Approximately(length, requestedLength);

            if (!sameClip)
            {
                ReleaseCompletion();
                Begin(clip, length);
            }

            if (!isPlaying)
                return CompletedAwaitable();

            completion ??= new AwaitableCompletionSource();
            return completion.Awaitable;
        }

        public void Stop()
        {
            isPlaying = false;
            ReleaseCompletion();
        }

        public void SetFrame(CustomAnimation clip, int index)
        {
            if (clip == null)
                return;

            isPlaying = false;
            ReleaseCompletion();

            current = clip;
            requestedLength = ResolveLength(clip, 0f);
            clock.Configure(clip.FrameCount, requestedLength, clip.Loop);
            clock.Speed = speed;
            clock.SeekToFrame(index);
            ApplySprite(clock.Frame);
        }

        protected override void OnDetached()
        {
            isPlaying = false;
            ReleaseCompletion();
        }

        protected override void OnUpdate()
        {
            if (!isPlaying || current == null)
                return;

            var clip = current;
            var delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var step = clock.Advance(delta);

            if (step.FrameChanged)
                ApplySprite(step.Frame);

            if (step.HasEnteredFrames && !RaiseFrameEvents(clip, step))
                return;

            if (step.Completed)
            {
                isPlaying = false;
                Finish(clip);
                return;
            }

            if (step.CyclesCompleted > 0)
                Finish(clip);
        }

        private void Begin(CustomAnimation clip, float length)
        {
            current = clip;
            requestedLength = length;
            clock.Configure(clip.FrameCount, length, clip.Loop);
            clock.Speed = speed;
            isPlaying = clip.FrameCount > 0;
            ApplySprite(0);
        }

        private float ResolveLength(CustomAnimation clip, float duration)
        {
            if (duration > 0f)
                return duration;

            var length = clip.Length;
            return length > 0f ? length : FlipbookClock.MinimumLength;
        }

        private bool RaiseFrameEvents(CustomAnimation clip, FlipbookStep step)
        {
            var eventCount = clip.EventCount;

            if (eventCount <= 0)
                return true;

            for (var global = step.FirstEntered; global <= step.LastEntered; global++)
            {
                var frame = clock.ToFrame(global);

                for (var i = 0; i < eventCount; i++)
                {
                    var frameEvent = clip.GetEvent(i);

                    if (frameEvent.Frame != frame || !frameEvent.HasId)
                        continue;

                    FrameEvent?.Invoke(frameEvent.Id);

                    if (!isPlaying || !ReferenceEquals(current, clip))
                        return false;
                }
            }

            return true;
        }

        private void ApplySprite(int index)
        {
            if (spriteRenderer == null || current == null)
                return;

            var sprite = current.GetSprite(index);

            if (ReferenceEquals(sprite, appliedSprite))
                return;

            appliedSprite = sprite;
            spriteRenderer.sprite = sprite;
        }

        private void Finish(CustomAnimation clip)
        {
            var source = completion;
            completion = null;
            Completed?.Invoke(clip);
            source?.TrySetResult();
        }

        private void ReleaseCompletion()
        {
            var source = completion;
            completion = null;
            source?.TrySetResult();
        }

        private static Awaitable CompletedAwaitable()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }
    }
}
