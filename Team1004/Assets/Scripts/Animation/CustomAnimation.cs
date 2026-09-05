using System;
using System.Text;
using UnityEngine;

namespace Game.Animation
{
    [CreateAssetMenu(menuName = "Team1004/Custom Animation", fileName = "NewCustomAnimation", order = 320)]
    public sealed class CustomAnimation : ScriptableObject
    {
        public const float DefaultFramesPerSecond = 12f;

        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField] private float framesPerSecond = DefaultFramesPerSecond;
        [SerializeField] private bool loop;
        [SerializeField] private float duration;
        [SerializeField] private CustomAnimationEvent[] events = Array.Empty<CustomAnimationEvent>();

        public int FrameCount => frames != null ? frames.Length : 0;
        public float FramesPerSecond => framesPerSecond;
        public bool Loop => loop;
        public float DurationOverride => duration;
        public int EventCount => events != null ? events.Length : 0;

        public float Length
        {
            get
            {
                if (duration > 0f)
                    return duration;

                var count = FrameCount;

                if (count <= 0 || framesPerSecond <= 0f)
                    return 0f;

                return count / framesPerSecond;
            }
        }

        public Sprite GetSprite(int index)
        {
            var count = FrameCount;

            if (count <= 0)
                return null;

            if (index < 0)
                index = 0;
            else if (index >= count)
                index = count - 1;

            return frames[index];
        }

        public int GetFrame(float time)
        {
            var count = FrameCount;

            if (count <= 1)
                return 0;

            var length = Length;

            if (length <= 0f)
                return 0;

            if (time <= 0f)
                return 0;

            if (loop)
            {
                if (time >= length)
                {
                    time -= (int)(time / length) * length;

                    if (time <= 0f)
                        return 0;
                }
            }
            else if (time >= length)
            {
                return count - 1;
            }

            var index = (int)(time / (length / count));

            if (index < 0)
                return 0;

            return index >= count ? count - 1 : index;
        }

        public CustomAnimationEvent GetEvent(int index)
        {
            if (events == null || index < 0 || index >= events.Length)
                return default;

            return events[index];
        }

        public bool Validate(out string error)
        {
            var builder = new StringBuilder();

            if (FrameCount <= 0)
                builder.Append("프레임이 없다. ");

            if (framesPerSecond <= 0f)
                builder.Append("FramesPerSecond가 0 이하다. ");

            if (duration < 0f)
                builder.Append("Duration이 음수다. ");

            if (frames != null)
            {
                for (var i = 0; i < frames.Length; i++)
                {
                    if (frames[i] != null)
                        continue;

                    builder.Append("프레임 ").Append(i).Append("이 비었다. ");
                }
            }

            if (events != null)
            {
                for (var i = 0; i < events.Length; i++)
                {
                    var frameEvent = events[i];

                    if (!frameEvent.HasId)
                        builder.Append("이벤트 ").Append(i).Append("의 id가 비었다. ");

                    if (frameEvent.Frame < 0 || frameEvent.Frame >= FrameCount)
                        builder.Append("이벤트 ").Append(i).Append("의 프레임 ").Append(frameEvent.Frame).Append("이 범위 밖이다. ");
                }
            }

            error = builder.Length > 0 ? builder.ToString().TrimEnd() : string.Empty;
            return builder.Length == 0;
        }

#if UNITY_EDITOR
        public void EditorInitialize(Sprite[] clipFrames, float fps, bool looping, float durationOverride, CustomAnimationEvent[] frameEvents)
        {
            frames = clipFrames ?? Array.Empty<Sprite>();
            framesPerSecond = fps;
            loop = looping;
            duration = durationOverride;
            events = frameEvents ?? Array.Empty<CustomAnimationEvent>();
        }
#endif
    }
}
