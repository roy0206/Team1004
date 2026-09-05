using NUnit.Framework;

namespace Game.Water.Tests
{
    public sealed class VerticalVelocitySamplerTests
    {
        [Test]
        public void FirstSampleHasZeroVelocity()
        {
            var sampler = new VerticalVelocitySampler();

            Assert.AreEqual(0f, sampler.Sample(3f, 0.1f));
            Assert.IsTrue(sampler.HasSample);
        }

        [Test]
        public void SecondSampleReturnsDeltaOverTime()
        {
            var sampler = new VerticalVelocitySampler();
            sampler.Sample(1f, 0.1f);

            Assert.AreEqual(5f, sampler.Sample(1.5f, 0.1f), 1e-4f);
            Assert.AreEqual(-10f, sampler.Sample(0.5f, 0.1f), 1e-4f);
        }

        [Test]
        public void ZeroDeltaTimeKeepsLastVelocity()
        {
            var sampler = new VerticalVelocitySampler();
            sampler.Sample(0f, 0.1f);
            sampler.Sample(1f, 0.1f);

            Assert.AreEqual(10f, sampler.Sample(5f, 0f), 1e-4f);
            Assert.AreEqual(10f, sampler.Velocity, 1e-4f);
        }

        [Test]
        public void SmoothingBlendsTowardRawVelocity()
        {
            var sampler = new VerticalVelocitySampler(0.5f);
            sampler.Sample(0f, 0.1f);

            Assert.AreEqual(5f, sampler.Sample(1f, 0.1f), 1e-4f);
            Assert.AreEqual(2.5f, sampler.Sample(1f, 0.1f), 1e-4f);
            Assert.AreEqual(1.25f, sampler.Sample(1f, 0.1f), 1e-4f);
        }

        [Test]
        public void SmoothingIsClampedBelowOne()
        {
            var sampler = new VerticalVelocitySampler(5f);

            Assert.LessOrEqual(sampler.Smoothing, 0.99f);

            sampler.Sample(0f, 0.1f);
            Assert.Greater(sampler.Sample(1f, 0.1f), 0f);
        }

        [Test]
        public void ResetWithPositionStartsFromThatHeight()
        {
            var sampler = new VerticalVelocitySampler();
            sampler.Sample(0f, 0.1f);
            sampler.Sample(4f, 0.1f);

            sampler.Reset(2f);

            Assert.AreEqual(0f, sampler.Velocity);
            Assert.AreEqual(10f, sampler.Sample(3f, 0.1f), 1e-4f);
        }

        [Test]
        public void ResetWithoutPositionForgetsHistory()
        {
            var sampler = new VerticalVelocitySampler();
            sampler.Sample(0f, 0.1f);
            sampler.Sample(4f, 0.1f);

            sampler.Reset();

            Assert.IsFalse(sampler.HasSample);
            Assert.AreEqual(0f, sampler.Sample(9f, 0.1f));
        }
    }
}
