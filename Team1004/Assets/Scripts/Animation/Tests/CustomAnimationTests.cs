using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class CustomAnimationTests
    {
        private readonly List<UnityEngine.Object> spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    UnityEngine.Object.DestroyImmediate(spawned[i]);
            }

            spawned.Clear();
        }

        private CustomAnimation Create(int frameCount, float fps, bool loop, float duration, bool realSprites)
        {
            return Create(frameCount, fps, loop, duration, realSprites, Array.Empty<CustomAnimationEvent>());
        }

        private CustomAnimation Create(int frameCount, float fps, bool loop, float duration, bool realSprites, CustomAnimationEvent[] events)
        {
            var frames = new Sprite[frameCount];

            if (realSprites)
            {
                for (var i = 0; i < frameCount; i++)
                    frames[i] = CreateSprite();
            }

            var clip = ScriptableObject.CreateInstance<CustomAnimation>();
            clip.EditorInitialize(frames, fps, loop, duration, events);
            spawned.Add(clip);
            return clip;
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.Apply();
            spawned.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            spawned.Add(sprite);
            return sprite;
        }

        [Test]
        public void LengthUsesFrameCountAndFps()
        {
            var clip = Create(5, 10f, false, 0f, false);

            Assert.AreEqual(0.5f, clip.Length, 0.0001f);
            Assert.AreEqual(5, clip.FrameCount);
        }

        [Test]
        public void DurationOverrideWinsOverFps()
        {
            var clip = Create(5, 10f, false, 0.8f, false);

            Assert.AreEqual(0.8f, clip.Length, 0.0001f);
            Assert.AreEqual(0.8f, clip.DurationOverride, 0.0001f);
        }

        [Test]
        public void EmptyClipHasZeroLength()
        {
            var clip = Create(0, 12f, false, 0f, false);

            Assert.AreEqual(0f, clip.Length, 0.0001f);
            Assert.AreEqual(0, clip.GetFrame(1f));
            Assert.IsNull(clip.GetSprite(0));
        }

        [Test]
        public void GetFrameReturnsZeroAtOrBeforeStart()
        {
            var clip = Create(5, 5f, false, 0f, false);

            Assert.AreEqual(0, clip.GetFrame(-1f));
            Assert.AreEqual(0, clip.GetFrame(0f));
            Assert.AreEqual(0, clip.GetFrame(0.19f));
        }

        [Test]
        public void GetFrameClampsForOneShot()
        {
            var clip = Create(5, 5f, false, 0f, false);

            Assert.AreEqual(4, clip.GetFrame(1f));
            Assert.AreEqual(4, clip.GetFrame(5f));
        }

        [Test]
        public void GetFrameStepsThroughFrameDurations()
        {
            var clip = Create(4, 4f, false, 0f, false);

            Assert.AreEqual(0, clip.GetFrame(0.24f));
            Assert.AreEqual(1, clip.GetFrame(0.25f));
            Assert.AreEqual(2, clip.GetFrame(0.5f));
            Assert.AreEqual(3, clip.GetFrame(0.75f));
        }

        [Test]
        public void GetFrameWrapsWhenLooping()
        {
            var clip = Create(5, 5f, true, 0f, false);

            Assert.AreEqual(0, clip.GetFrame(1f));
            Assert.AreEqual(1, clip.GetFrame(1.3f));
            Assert.AreEqual(4, clip.GetFrame(2.9f));
        }

        [Test]
        public void SingleFrameClipAlwaysReturnsZero()
        {
            var clip = Create(1, 12f, true, 0f, false);

            Assert.AreEqual(0, clip.GetFrame(0f));
            Assert.AreEqual(0, clip.GetFrame(100f));
        }

        [Test]
        public void GetSpriteClampsIndex()
        {
            var clip = Create(3, 12f, false, 0f, true);

            Assert.AreSame(clip.GetSprite(0), clip.GetSprite(-4));
            Assert.AreSame(clip.GetSprite(2), clip.GetSprite(9));
            Assert.AreNotSame(clip.GetSprite(0), clip.GetSprite(1));
        }

        [Test]
        public void ValidateAcceptsCompleteClip()
        {
            var clip = Create(3, 12f, false, 0f, true, new[] { new CustomAnimationEvent(1, "step") });

            Assert.IsTrue(clip.Validate(out var error), error);
            Assert.IsEmpty(error);
        }

        [Test]
        public void ValidateRejectsEmptyFrames()
        {
            var clip = Create(0, 12f, false, 0f, false);

            Assert.IsFalse(clip.Validate(out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void ValidateRejectsNonPositiveFps()
        {
            var clip = Create(2, 0f, false, 0f, true);

            Assert.IsFalse(clip.Validate(out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void ValidateRejectsEventOutsideFrameRange()
        {
            var clip = Create(2, 12f, false, 0f, true, new[] { new CustomAnimationEvent(5, "late") });

            Assert.IsFalse(clip.Validate(out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void EventsAreReadableByIndex()
        {
            var clip = Create(3, 12f, false, 0f, true, new[]
            {
                new CustomAnimationEvent(0, "start"),
                new CustomAnimationEvent(2, "splash")
            });

            Assert.AreEqual(2, clip.EventCount);
            Assert.AreEqual("start", clip.GetEvent(0).Id);
            Assert.AreEqual(2, clip.GetEvent(1).Frame);
            Assert.IsFalse(clip.GetEvent(7).HasId);
        }
    }
}
