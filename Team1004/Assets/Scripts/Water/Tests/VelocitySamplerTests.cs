using NUnit.Framework;
using UnityEngine;

namespace Game.Water.Tests
{
    public sealed class VelocitySamplerTests
    {
        [Test]
        public void FirstSampleHasZeroVelocity()
        {
            var sampler = new VelocitySampler();

            Assert.AreEqual(Vector2.zero, sampler.Sample(new Vector2(2f, 3f), 0.1f));
            Assert.IsTrue(sampler.HasSample);
        }

        [Test]
        public void SecondSampleReturnsDeltaOverTime()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(0f, 1f), 0.1f);

            var velocity = sampler.Sample(new Vector2(-0.4f, 1.5f), 0.1f);

            Assert.AreEqual(-4f, velocity.x, 1e-4f);
            Assert.AreEqual(5f, velocity.y, 1e-4f);
        }

        [Test]
        public void HorizontalAndVerticalMatchVelocityComponents()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(0f, 0f), 0.1f);
            sampler.Sample(new Vector2(0.2f, -0.3f), 0.1f);

            Assert.AreEqual(sampler.Velocity.x, sampler.Horizontal, 1e-6f);
            Assert.AreEqual(sampler.Velocity.y, sampler.Vertical, 1e-6f);
            Assert.AreEqual(2f, sampler.Horizontal, 1e-4f);
            Assert.AreEqual(-3f, sampler.Vertical, 1e-4f);
        }

        [Test]
        public void ZeroDeltaTimeKeepsLastVelocity()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(0f, 0f), 0.1f);
            sampler.Sample(new Vector2(0f, 1f), 0.1f);

            var velocity = sampler.Sample(new Vector2(9f, 5f), 0f);

            Assert.AreEqual(10f, velocity.y, 1e-4f);
            Assert.AreEqual(10f, sampler.Velocity.y, 1e-4f);
            Assert.AreEqual(0f, sampler.Velocity.x, 1e-4f);
        }

        [Test]
        public void SmoothingBlendsTowardRawVelocity()
        {
            var sampler = new VelocitySampler(0.5f);
            sampler.Sample(new Vector2(0f, 0f), 0.1f);

            Assert.AreEqual(5f, sampler.Sample(new Vector2(0f, 1f), 0.1f).y, 1e-4f);
            Assert.AreEqual(2.5f, sampler.Sample(new Vector2(0f, 1f), 0.1f).y, 1e-4f);
            Assert.AreEqual(1.25f, sampler.Sample(new Vector2(0f, 1f), 0.1f).y, 1e-4f);
        }

        [Test]
        public void SmoothingIsClampedBelowOne()
        {
            var sampler = new VelocitySampler(5f);

            Assert.LessOrEqual(sampler.Smoothing, 0.99f);

            sampler.Sample(new Vector2(0f, 0f), 0.1f);
            Assert.Greater(sampler.Sample(new Vector2(0f, 1f), 0.1f).y, 0f);
        }

        [Test]
        public void ResetWithPositionStartsFromThatPoint()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(0f, 0f), 0.1f);
            sampler.Sample(new Vector2(0f, 4f), 0.1f);

            sampler.Reset(new Vector2(6f, 2f));

            Assert.AreEqual(Vector2.zero, sampler.Velocity);
            Assert.AreEqual(10f, sampler.Sample(new Vector2(6f, 3f), 0.1f).y, 1e-4f);
        }

        [Test]
        public void ResetAfterTeleportRemovesSpike()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(-8f, 1.1f), 0.016f);

            sampler.Reset(new Vector2(9f, 1.1f));

            Assert.AreEqual(Vector2.zero, sampler.Sample(new Vector2(9f, 1.1f), 0.016f));
        }

        [Test]
        public void ResetWithoutPositionForgetsHistory()
        {
            var sampler = new VelocitySampler();
            sampler.Sample(new Vector2(0f, 0f), 0.1f);
            sampler.Sample(new Vector2(0f, 4f), 0.1f);

            sampler.Reset();

            Assert.IsFalse(sampler.HasSample);
            Assert.AreEqual(Vector2.zero, sampler.Sample(new Vector2(0f, 9f), 0.1f));
        }
    }
}
