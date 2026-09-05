using NUnit.Framework;

namespace Game.Boss.Tests
{
    public sealed class BossTimerTests
    {
        [Test]
        public void DoesNotTickUntilStarted()
        {
            var timer = new BossTimer(2f);
            timer.Tick(1f);

            Assert.AreEqual(0f, timer.Elapsed);
            Assert.AreEqual(2f, timer.Remaining);
            Assert.IsFalse(timer.IsExpired);
        }

        [Test]
        public void ExpiresOnceAndClampsElapsed()
        {
            var timer = new BossTimer(1f);
            var expiredCount = 0;
            timer.Expired += () => expiredCount++;

            timer.Start();
            timer.Tick(0.6f);
            Assert.IsFalse(timer.IsExpired);
            Assert.AreEqual(0.4f, timer.Remaining, 0.0001f);

            timer.Tick(0.6f);
            Assert.IsTrue(timer.IsExpired);
            Assert.AreEqual(1f, timer.Elapsed);
            Assert.AreEqual(0f, timer.Remaining);
            Assert.IsFalse(timer.IsRunning);

            timer.Tick(1f);
            Assert.AreEqual(1, expiredCount);
        }

        [Test]
        public void StartRestartsElapsed()
        {
            var timer = new BossTimer(1f);
            timer.Start();
            timer.Tick(0.5f);
            timer.Start();

            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsTrue(timer.IsRunning);
        }

        [Test]
        public void RemainingAndProgressAreNormalized()
        {
            var timer = new BossTimer(4f);
            timer.Start();
            timer.Tick(1f);

            Assert.AreEqual(0.75f, timer.Remaining01, 0.0001f);
            Assert.AreEqual(0.25f, timer.Progress01, 0.0001f);
        }

        [Test]
        public void ZeroDurationIsExpiredImmediately()
        {
            var timer = new BossTimer(0f);
            timer.Start();

            Assert.IsTrue(timer.IsExpired);
            Assert.AreEqual(0f, timer.Remaining01);
        }
    }
}
