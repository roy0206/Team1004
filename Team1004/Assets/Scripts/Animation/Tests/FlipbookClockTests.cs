using NUnit.Framework;

namespace Game.Animation.Tests
{
    public sealed class FlipbookClockTests
    {
        private static void Record(FlipbookClock clock, FlipbookStep step, int[] counts)
        {
            if (!step.HasEnteredFrames)
                return;

            for (var global = step.FirstEntered; global <= step.LastEntered; global++)
                counts[clock.ToFrame(global)]++;
        }

        [Test]
        public void StartsOnFirstFrameAndReportsChange()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);

            var step = clock.Advance(0f);

            Assert.AreEqual(0, step.Frame);
            Assert.IsTrue(step.FrameChanged);
            Assert.AreEqual(0, step.FirstEntered);
            Assert.AreEqual(0, step.LastEntered);
        }

        [Test]
        public void AdvancesOneFramePerFrameDuration()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.Advance(0f);

            Assert.AreEqual(1, clock.Advance(0.25f).Frame);
            Assert.AreEqual(2, clock.Advance(0.25f).Frame);
            Assert.AreEqual(3, clock.Advance(0.25f).Frame);
        }

        [Test]
        public void OneShotClampsAndCompletesOnce()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.Advance(0f);
            clock.Advance(0.25f);
            clock.Advance(0.25f);
            clock.Advance(0.25f);

            var completing = clock.Advance(0.25f);

            Assert.IsTrue(completing.Completed);
            Assert.AreEqual(3, completing.Frame);
            Assert.IsTrue(clock.IsFinished);

            var after = clock.Advance(0.25f);

            Assert.IsFalse(after.Completed);
            Assert.IsFalse(after.HasEnteredFrames);
            Assert.AreEqual(3, clock.Frame);
            Assert.AreEqual(1f, clock.NormalizedTime, 0.0001f);
        }

        [Test]
        public void LoopWrapsAndReportsCycle()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, true);
            clock.Advance(0f);
            clock.Advance(0.25f);
            clock.Advance(0.25f);
            clock.Advance(0.25f);

            var wrapping = clock.Advance(0.25f);

            Assert.AreEqual(0, wrapping.Frame);
            Assert.AreEqual(1, wrapping.CyclesCompleted);
            Assert.IsFalse(wrapping.Completed);
            Assert.IsFalse(clock.IsFinished);
        }

        [Test]
        public void SpeedScalesElapsedTime()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.Speed = 2f;
            clock.Advance(0f);

            Assert.AreEqual(1, clock.Advance(0.125f).Frame);
            Assert.AreEqual(2, clock.Advance(0.125f).Frame);
        }

        [Test]
        public void NegativeSpeedIsClampedToZero()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.Speed = -3f;

            Assert.AreEqual(0f, clock.Speed);

            clock.Advance(0f);
            clock.Advance(0.9f);

            Assert.AreEqual(0, clock.Frame);
        }

        [Test]
        public void DurationOverrideChangesFrameDuration()
        {
            var clock = new FlipbookClock();
            clock.Configure(5, 0.8f, false);

            Assert.AreEqual(0.16f, clock.FrameDuration, 0.0001f);

            clock.Advance(0f);

            Assert.AreEqual(2, clock.Advance(0.4f).Frame);
        }

        [Test]
        public void OneShotEntersEveryFrameExactlyOnce()
        {
            var clock = new FlipbookClock();
            clock.Configure(5, 1f, false);
            var counts = new int[5];

            Record(clock, clock.Advance(0f), counts);

            for (var i = 0; i < 200; i++)
                Record(clock, clock.Advance(0.01f), counts);

            for (var i = 0; i < counts.Length; i++)
                Assert.AreEqual(1, counts[i], "frame " + i);
        }

        [Test]
        public void LongDeltaEntersEverySkippedFrame()
        {
            var clock = new FlipbookClock();
            clock.Configure(10, 1f, false);

            var step = clock.Advance(0.55f);

            Assert.AreEqual(0, step.FirstEntered);
            Assert.AreEqual(5, step.LastEntered);
            Assert.AreEqual(6, step.EnteredCount);
            Assert.AreEqual(5, step.Frame);
        }

        [Test]
        public void LoopEntersEveryFrameOncePerCycle()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, true);
            var counts = new int[4];

            Record(clock, clock.Advance(0f), counts);

            for (var i = 0; i < 7; i++)
                Record(clock, clock.Advance(0.25f), counts);

            for (var i = 0; i < counts.Length; i++)
                Assert.AreEqual(2, counts[i], "frame " + i);
        }

        [Test]
        public void LoopLongDeltaCapsEnteredFramesToOneCycle()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, true);

            var step = clock.Advance(10f);

            Assert.AreEqual(4, step.EnteredCount);
            Assert.AreEqual(10, step.CyclesCompleted);

            var seen = new int[4];
            Record(clock, step, seen);

            for (var i = 0; i < seen.Length; i++)
                Assert.AreEqual(1, seen[i], "frame " + i);
        }

        [Test]
        public void SeekToFrameSetsFrameAndNormalizedTime()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.SeekToFrame(2);

            Assert.AreEqual(2, clock.Frame);
            Assert.AreEqual(0.5f, clock.NormalizedTime, 0.0001f);
            Assert.IsFalse(clock.IsFinished);
        }

        [Test]
        public void SeekToFrameClampsOutOfRange()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);

            clock.SeekToFrame(-5);
            Assert.AreEqual(0, clock.Frame);

            clock.SeekToFrame(99);
            Assert.AreEqual(3, clock.Frame);
        }

        [Test]
        public void SeekedFrameIsNotEnteredAgain()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.SeekToFrame(1);

            var step = clock.Advance(0f);

            Assert.IsFalse(step.HasEnteredFrames);
            Assert.AreEqual(1, step.Frame);
        }

        [Test]
        public void EmptyClipDoesNothing()
        {
            var clock = new FlipbookClock();
            clock.Configure(0, 1f, false);

            var step = clock.Advance(0.5f);

            Assert.AreEqual(0, step.Frame);
            Assert.IsFalse(step.HasEnteredFrames);
            Assert.IsFalse(step.Completed);
            Assert.IsTrue(clock.IsFinished);
        }

        [Test]
        public void ResetReplaysFromStart()
        {
            var clock = new FlipbookClock();
            clock.Configure(4, 1f, false);
            clock.Advance(0f);
            clock.Advance(1f);

            Assert.IsTrue(clock.IsFinished);

            clock.Reset();

            Assert.IsFalse(clock.IsFinished);
            Assert.AreEqual(0, clock.Frame);

            var step = clock.Advance(0f);

            Assert.IsTrue(step.HasEnteredFrames);
            Assert.AreEqual(0, step.FirstEntered);
        }

        [Test]
        public void SingleFrameOneShotCompletesWithoutRepeatingEvents()
        {
            var clock = new FlipbookClock();
            clock.Configure(1, 0.5f, false);
            var counts = new int[1];

            Record(clock, clock.Advance(0f), counts);
            Record(clock, clock.Advance(0.25f), counts);

            var completing = clock.Advance(0.25f);
            Record(clock, completing, counts);

            Assert.IsTrue(completing.Completed);
            Assert.AreEqual(1, counts[0]);
        }
    }
}
