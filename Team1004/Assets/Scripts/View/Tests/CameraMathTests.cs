using Game.View;
using NUnit.Framework;
using UnityEngine;

namespace Game.View.Tests
{
    public sealed class ShakeMathTests
    {
        [Test]
        public void DecayReachesZeroAndClamps()
        {
            Assert.AreEqual(0.5f, ShakeMath.Decay(1f, 1f, 0.5f), 0.0001f);
            Assert.AreEqual(0f, ShakeMath.Decay(0.2f, 1f, 0.5f), 0.0001f);
            Assert.AreEqual(1f, ShakeMath.Decay(2f, 0f, 0.5f), 0.0001f);
        }

        [Test]
        public void AmplitudeIsQuadratic()
        {
            Assert.AreEqual(0.25f, ShakeMath.Amplitude(0.5f), 0.0001f);
            Assert.AreEqual(1f, ShakeMath.Amplitude(1f), 0.0001f);
            Assert.AreEqual(0f, ShakeMath.Amplitude(0f), 0.0001f);
        }

        [Test]
        public void OffsetStaysInsideMaxOffset()
        {
            const float max = 0.36f;

            for (var i = 0; i < 200; i++)
            {
                var offset = ShakeMath.Offset(3.5f, 1f, i * 0.37f, max);
                Assert.LessOrEqual(Mathf.Abs(offset.x), max + 0.0001f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), max + 0.0001f);
            }
        }

        [Test]
        public void OffsetIsZeroWithoutTrauma()
        {
            var offset = ShakeMath.Offset(1f, 0f, 4f, 0.36f);
            Assert.AreEqual(0f, offset.x, 0.0001f);
            Assert.AreEqual(0f, offset.y, 0.0001f);
        }

        [Test]
        public void PunchEnvelopeStartsFullAndEndsAtZero()
        {
            Assert.AreEqual(1f, ShakeMath.PunchEnvelope(0f), 0.0001f);
            Assert.AreEqual(0f, ShakeMath.PunchEnvelope(1f), 0.0001f);
            Assert.Less(ShakeMath.PunchEnvelope(0.7f), 0f);
        }

        [Test]
        public void TraumaForAmplitudeRoundTrips()
        {
            const float max = 0.36f;
            var trauma = ShakeMath.TraumaForAmplitude(0.09f, max);
            Assert.AreEqual(0.09f, ShakeMath.Amplitude(trauma) * max, 0.0001f);
        }

        [Test]
        public void DecayRateEmptiesTraumaOverDuration()
        {
            var rate = ShakeMath.DecayRateFor(0.6f, 0.4f);
            Assert.AreEqual(0f, ShakeMath.Decay(0.6f, rate, 0.4f), 0.0001f);
            Assert.Greater(ShakeMath.Decay(0.6f, rate, 0.2f), 0f);
        }
    }

    public sealed class CameraEasingTests
    {
        [Test]
        public void EveryEaseStartsAtZeroAndEndsAtOne()
        {
            foreach (CameraEase ease in System.Enum.GetValues(typeof(CameraEase)))
            {
                Assert.AreEqual(0f, CameraEasing.Evaluate(ease, 0f), 0.0001f, ease.ToString());
                Assert.AreEqual(1f, CameraEasing.Evaluate(ease, 1f), 0.0001f, ease.ToString());
            }
        }

        [Test]
        public void EvaluateClampsOutsideRange()
        {
            Assert.AreEqual(0f, CameraEasing.Evaluate(CameraEase.QuadIn, -3f), 0.0001f);
            Assert.AreEqual(1f, CameraEasing.Evaluate(CameraEase.QuadIn, 4f), 0.0001f);
        }

        [Test]
        public void SineInOutIsSymmetric()
        {
            Assert.AreEqual(0.5f, CameraEasing.Evaluate(CameraEase.SineInOut, 0.5f), 0.0001f);
            Assert.AreEqual(
                1f - CameraEasing.Evaluate(CameraEase.SineInOut, 0.25f),
                CameraEasing.Evaluate(CameraEase.SineInOut, 0.75f),
                0.0001f);
        }

        [Test]
        public void BackOutOvershoots()
        {
            Assert.Greater(CameraEasing.Evaluate(CameraEase.BackOut, 0.7f), 1f);
        }

        [Test]
        public void LerpUsesEasedValue()
        {
            Assert.AreEqual(1f, CameraEasing.Lerp(1f, 0.9f, CameraEase.Linear, 0f), 0.0001f);
            Assert.AreEqual(0.95f, CameraEasing.Lerp(1f, 0.9f, CameraEase.Linear, 0.5f), 0.0001f);
            Assert.AreEqual(0.9f, CameraEasing.Lerp(1f, 0.9f, CameraEase.Linear, 1f), 0.0001f);
        }
    }

    public sealed class CameraCompositionTests
    {
        [Test]
        public void ComposeAddsPanAndShakeAndKeepsDepth()
        {
            var result = CameraComposition.Compose(
                new Vector3(1f, 2f, -10f), new Vector2(0.1f, 0.2f), new Vector2(0.01f, -0.02f));

            Assert.AreEqual(1.11f, result.x, 0.0001f);
            Assert.AreEqual(2.18f, result.y, 0.0001f);
            Assert.AreEqual(-10f, result.z, 0.0001f);
        }

        [Test]
        public void ComposeSizeMultipliesAndClamps()
        {
            Assert.AreEqual(3.816f, CameraComposition.ComposeSize(3.6f, 1.06f, 0.8f, 1.1f), 0.0001f);
            Assert.AreEqual(3.96f, CameraComposition.ComposeSize(3.6f, 1.5f, 0.8f, 1.1f), 0.0001f);
            Assert.AreEqual(2.88f, CameraComposition.ComposeSize(3.6f, 0.1f, 0.8f, 1.1f), 0.0001f);
        }

        [Test]
        public void VisibleExtentsFollowZoom()
        {
            Assert.AreEqual(3.96f, CameraComposition.VisibleHalfHeight(3.6f, 1.1f), 0.0001f);
            Assert.AreEqual(7.04f, CameraComposition.VisibleHalfWidth(3.6f, 1.1f, 16f / 9f), 0.0001f);
        }
    }

    public sealed class CameraZoomModuleTests
    {
        [Test]
        public void ZoomToReachesTargetAfterDuration()
        {
            var zoom = new CameraZoomModule();
            zoom.ZoomTo(0.9f, 0.4f, CameraEase.Linear);

            zoom.Advance(0.2f);
            Assert.AreEqual(0.95f, zoom.Factor, 0.001f);

            zoom.Advance(0.2f);
            Assert.AreEqual(0.9f, zoom.Factor, 0.0001f);
            Assert.IsFalse(zoom.IsRunning);
        }

        [Test]
        public void ZoomTargetIsClamped()
        {
            var zoom = new CameraZoomModule();
            zoom.SetLimits(0.8f, 1.1f);
            zoom.ZoomTo(1.4f, 0f);

            Assert.AreEqual(1.1f, zoom.Factor, 0.0001f);

            zoom.ZoomTo(0.2f, 0f);
            Assert.AreEqual(0.8f, zoom.Factor, 0.0001f);
        }

        [Test]
        public void ZoomPunchReturnsToOne()
        {
            var zoom = new CameraZoomModule();
            zoom.ZoomPunch(0.9f, 0.1f, 0.1f, 0.2f, CameraEase.Linear);

            zoom.Advance(0.1f);
            Assert.AreEqual(0.9f, zoom.Factor, 0.0001f);

            zoom.Advance(0.1f);
            Assert.AreEqual(0.9f, zoom.Factor, 0.0001f);

            zoom.Advance(0.2f);
            Assert.AreEqual(1f, zoom.Factor, 0.0001f);
            Assert.IsFalse(zoom.IsRunning);
        }

        [Test]
        public void ResetSnapsToOne()
        {
            var zoom = new CameraZoomModule();
            zoom.ZoomTo(0.85f, 0f);
            zoom.Reset();

            Assert.AreEqual(1f, zoom.Factor, 0.0001f);
        }
    }

    public sealed class CameraPanModuleTests
    {
        [Test]
        public void PanPulseReturnsToZero()
        {
            var pan = new CameraPanModule();
            pan.PanPulse(new Vector2(0f, 0.2f), 0.2f, 0f, 0.2f, CameraEase.Linear);

            pan.Advance(0.2f);
            Assert.AreEqual(0.2f, pan.Offset.y, 0.0001f);

            pan.Advance(0.2f);
            Assert.AreEqual(0f, pan.Offset.y, 0.0001f);
            Assert.IsFalse(pan.IsRunning);
        }

        [Test]
        public void PanTargetIsClamped()
        {
            var pan = new CameraPanModule();
            pan.SetLimit(0.5f);
            pan.PanTo(new Vector2(0f, 3f), 0f);

            Assert.AreEqual(0.5f, pan.Offset.y, 0.0001f);
        }
    }

    public sealed class CameraShakeModuleTests
    {
        [Test]
        public void TraumaDecaysToZero()
        {
            var shake = new CameraShakeModule(2f);
            shake.Configure(0.36f, 1.2f, 1.6f, 22f);
            shake.AddTrauma(0.8f);

            shake.Advance(0.2f);
            Assert.Greater(shake.Trauma, 0f);

            shake.Advance(1f);
            Assert.AreEqual(0f, shake.Trauma, 0.0001f);
            Assert.AreEqual(0f, shake.Offset.x, 0.0001f);
            Assert.AreEqual(0f, shake.Offset.y, 0.0001f);
        }

        [Test]
        public void ShakeForEmptiesAfterDuration()
        {
            var shake = new CameraShakeModule(5f);
            shake.ShakeFor(0.5f, 0.6f);

            shake.Advance(0.25f);
            Assert.Greater(shake.Trauma, 0f);

            shake.Advance(0.25f);
            Assert.AreEqual(0f, shake.Trauma, 0.0001f);
        }

        [Test]
        public void PunchDecaysAndStaysInsideLimit()
        {
            var shake = new CameraShakeModule(1f);
            shake.Configure(0.36f, 1.2f, 1.6f, 22f);
            shake.Punch(Vector2.up, 0.2f, 0.2f);

            shake.Advance(0.02f);
            Assert.Greater(shake.Offset.y, 0f);
            Assert.LessOrEqual(shake.Offset.y, 0.36f);

            shake.Advance(0.2f);
            Assert.IsFalse(shake.IsPunching);
            Assert.AreEqual(0f, shake.Offset.y, 0.0001f);
        }

        [Test]
        public void AddTraumaIsClampedToOne()
        {
            var shake = new CameraShakeModule();
            shake.AddTrauma(0.8f);
            shake.AddTrauma(0.8f);

            Assert.AreEqual(1f, shake.Trauma, 0.0001f);
        }
    }
}
