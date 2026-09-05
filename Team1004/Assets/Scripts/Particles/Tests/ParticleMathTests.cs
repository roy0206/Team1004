using Game.Particles;
using NUnit.Framework;
using UnityEngine;

namespace Game.Particles.Tests
{
    public sealed class ParticleMathTests
    {
        [Test]
        public void AlphaOverLife_IsZeroAtBirthAndDeath()
        {
            Assert.AreEqual(0f, ParticleMath.AlphaOverLife(0f, 0.2f, 0.3f), 1e-5f);
            Assert.AreEqual(0f, ParticleMath.AlphaOverLife(1f, 0.2f, 0.3f), 1e-5f);
        }

        [Test]
        public void AlphaOverLife_ReachesOneOnThePlateau()
        {
            Assert.AreEqual(1f, ParticleMath.AlphaOverLife(0.5f, 0.2f, 0.3f), 1e-5f);
            Assert.AreEqual(1f, ParticleMath.AlphaOverLife(0.2f, 0.2f, 0.3f), 1e-5f);
            Assert.AreEqual(1f, ParticleMath.AlphaOverLife(0.7f, 0.2f, 0.3f), 1e-5f);
        }

        [Test]
        public void AlphaOverLife_RisesAndFallsLinearly()
        {
            Assert.AreEqual(0.5f, ParticleMath.AlphaOverLife(0.1f, 0.2f, 0.3f), 1e-5f);
            Assert.AreEqual(0.5f, ParticleMath.AlphaOverLife(0.85f, 0.2f, 0.3f), 1e-5f);
        }

        [Test]
        public void AlphaOverLife_NeverExceedsOne()
        {
            for (var i = 1; i < 100; i++)
            {
                var value = ParticleMath.AlphaOverLife(i / 100f, 0.6f, 0.6f);
                Assert.GreaterOrEqual(value, 0f);
                Assert.LessOrEqual(value, 1f);
            }
        }

        [Test]
        public void AlphaOverLife_ZeroFadeIsAHardEdge()
        {
            Assert.AreEqual(1f, ParticleMath.AlphaOverLife(0.001f, 0f, 0f), 1e-5f);
            Assert.AreEqual(1f, ParticleMath.AlphaOverLife(0.999f, 0f, 0f), 1e-5f);
        }

        [Test]
        public void SizeOverLife_GoesFromOneToEndScale()
        {
            Assert.AreEqual(1f, ParticleMath.SizeOverLife(0f, 1.5f), 1e-5f);
            Assert.AreEqual(1.25f, ParticleMath.SizeOverLife(0.5f, 1.5f), 1e-5f);
            Assert.AreEqual(1.5f, ParticleMath.SizeOverLife(1f, 1.5f), 1e-5f);
        }

        [Test]
        public void Direction_MapsDegreesToUnitVector()
        {
            var right = ParticleMath.Direction(0f);
            Assert.AreEqual(1f, right.x, 1e-5f);
            Assert.AreEqual(0f, right.y, 1e-5f);

            var up = ParticleMath.Direction(90f);
            Assert.AreEqual(0f, up.x, 1e-5f);
            Assert.AreEqual(1f, up.y, 1e-5f);

            var left = ParticleMath.Direction(180f);
            Assert.AreEqual(-1f, left.x, 1e-5f);
            Assert.AreEqual(0f, left.y, 1e-5f);
        }

        [Test]
        public void Direction_IsAlwaysUnitLength()
        {
            for (var degrees = 0; degrees < 360; degrees += 17)
                Assert.AreEqual(1f, ParticleMath.Direction(degrees).magnitude, 1e-4f);
        }

        [Test]
        public void Drift_IsZeroWithoutAmplitudeOrFrequency()
        {
            Assert.AreEqual(0f, ParticleMath.Drift(1.3f, 0.25f, 0f, 2f), 1e-6f);
            Assert.AreEqual(0f, ParticleMath.Drift(1.3f, 0.25f, 0.5f, 0f), 1e-6f);
        }

        [Test]
        public void Drift_StaysInsideAmplitude()
        {
            for (var i = 0; i < 200; i++)
            {
                var value = ParticleMath.Drift(i * 0.037f, 0.31f, 0.12f, 1.4f);
                Assert.LessOrEqual(Mathf.Abs(value), 0.12f + 1e-5f);
            }
        }

        [Test]
        public void TakeSpawnCount_AccumulatesUntilOneWholeParticle()
        {
            var accumulator = 0f;

            Assert.AreEqual(0, ParticleMath.TakeSpawnCount(ref accumulator, 0.1f, 4f, 8));
            Assert.AreEqual(0, ParticleMath.TakeSpawnCount(ref accumulator, 0.1f, 4f, 8));
            Assert.AreEqual(1, ParticleMath.TakeSpawnCount(ref accumulator, 0.1f, 4f, 8));
            Assert.AreEqual(0.2f, accumulator, 1e-4f);
        }

        [Test]
        public void TakeSpawnCount_KeepsTheAverageRate()
        {
            var accumulator = 0f;
            var total = 0;

            for (var i = 0; i < 600; i++)
                total += ParticleMath.TakeSpawnCount(ref accumulator, 1f / 60f, 5f, 64);

            Assert.That(total, Is.InRange(49, 50));
        }

        [Test]
        public void TakeSpawnCount_ClampsToFreeSlotsAndDropsTheRemainder()
        {
            var accumulator = 0f;

            Assert.AreEqual(3, ParticleMath.TakeSpawnCount(ref accumulator, 1f, 10f, 3));
            Assert.AreEqual(0f, accumulator, 1e-5f);
        }

        [Test]
        public void TakeSpawnCount_IgnoresZeroRateOrNoRoom()
        {
            var accumulator = 0f;

            Assert.AreEqual(0, ParticleMath.TakeSpawnCount(ref accumulator, 1f, 0f, 8));
            Assert.AreEqual(0, ParticleMath.TakeSpawnCount(ref accumulator, 1f, 10f, 0));
            Assert.AreEqual(0, ParticleMath.TakeSpawnCount(ref accumulator, 0f, 10f, 8));
            Assert.AreEqual(0f, accumulator, 1e-5f);
        }

        [Test]
        public void IsOutsideBounds_UsesTheMargin()
        {
            var center = new Vector2(0f, 0.15f);
            var size = new Vector2(14f, 3.5f);

            Assert.IsFalse(ParticleMath.IsOutsideBounds(new Vector2(-7f, -1.6f), center, size, 1f));
            Assert.IsFalse(ParticleMath.IsOutsideBounds(new Vector2(-7.9f, 0f), center, size, 1f));
            Assert.IsTrue(ParticleMath.IsOutsideBounds(new Vector2(-8.1f, 0f), center, size, 1f));
            Assert.IsTrue(ParticleMath.IsOutsideBounds(new Vector2(0f, 3.5f), center, size, 1f));
        }

        [Test]
        public void LocalScaleFor_ConvertsWorldWidthWithSpriteWidth()
        {
            Assert.AreEqual(0.75f, ParticleMath.LocalScaleFor(0.12f, 0.16f), 1e-5f);
            Assert.AreEqual(1.5625f, ParticleMath.LocalScaleFor(0.5f, 0.32f), 1e-4f);
        }

        [Test]
        public void ParticleRandom_StaysInsideTheRequestedRange()
        {
            var random = new ParticleRandom(12345u);

            for (var i = 0; i < 2000; i++)
            {
                var value = random.Range(-1.5f, 2.5f);
                Assert.GreaterOrEqual(value, -1.5f);
                Assert.LessOrEqual(value, 2.5f);
            }
        }

        [Test]
        public void ParticleRandom_IsDeterministicForTheSameSeed()
        {
            var a = new ParticleRandom(777u);
            var b = new ParticleRandom(777u);

            for (var i = 0; i < 50; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void ParticleRandom_NeverStallsOnZeroSeed()
        {
            var random = new ParticleRandom(0u);
            var first = random.NextUInt();

            Assert.AreNotEqual(0u, first);
            Assert.AreNotEqual(first, random.NextUInt());
        }
    }
}
