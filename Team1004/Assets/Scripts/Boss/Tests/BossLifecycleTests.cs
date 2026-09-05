using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Boss.Tests
{
    public sealed class BossLifecycleTests
    {
        [Test]
        public void StartsIdleWithNoOutcome()
        {
            var lifecycle = new BossLifecycle();

            Assert.AreEqual(BossStage.Idle, lifecycle.Stage);
            Assert.AreEqual(BossOutcome.None, lifecycle.Outcome);
            Assert.IsFalse(lifecycle.IsInitialized);
            Assert.IsFalse(lifecycle.IsActive);
            Assert.IsFalse(lifecycle.IsFinished);
        }

        [Test]
        public void BeginBeforeInitializeThrows()
        {
            var lifecycle = new BossLifecycle();

            Assert.Throws<InvalidOperationException>(() => lifecycle.Begin());
        }

        [Test]
        public void InitializeBeginCompleteRaisesEventsInOrder()
        {
            var lifecycle = new BossLifecycle();
            var log = new List<string>();
            lifecycle.Began += () => log.Add("began");
            lifecycle.Finished += outcome => log.Add($"finished:{outcome}");

            Assert.IsTrue(lifecycle.Initialize());
            Assert.AreEqual(BossStage.Ready, lifecycle.Stage);

            Assert.IsTrue(lifecycle.Begin());
            Assert.IsTrue(lifecycle.IsActive);

            Assert.IsTrue(lifecycle.Complete(BossOutcome.Passed));
            Assert.IsTrue(lifecycle.IsFinished);
            Assert.AreEqual(BossOutcome.Passed, lifecycle.Outcome);
            CollectionAssert.AreEqual(new[] { "began", "finished:Passed" }, log);
        }

        [Test]
        public void SecondBeginIsIgnored()
        {
            var lifecycle = new BossLifecycle();
            var beganCount = 0;
            lifecycle.Began += () => beganCount++;
            lifecycle.Initialize();

            Assert.IsTrue(lifecycle.Begin());
            Assert.IsFalse(lifecycle.Begin());
            Assert.AreEqual(1, beganCount);
        }

        [Test]
        public void AbortWhileActiveFinishesAsFailed()
        {
            var lifecycle = new BossLifecycle();
            BossOutcome? received = null;
            lifecycle.Finished += outcome => received = outcome;
            lifecycle.Initialize();
            lifecycle.Begin();

            Assert.IsTrue(lifecycle.Abort());
            Assert.IsTrue(lifecycle.IsFinished);
            Assert.AreEqual(BossOutcome.Failed, lifecycle.Outcome);
            Assert.AreEqual(BossOutcome.Failed, received);
        }

        [Test]
        public void AbortWhileReadyFinishesAsFailedWithoutBegan()
        {
            var lifecycle = new BossLifecycle();
            var beganCount = 0;
            lifecycle.Began += () => beganCount++;
            lifecycle.Initialize();

            Assert.IsTrue(lifecycle.Abort());
            Assert.AreEqual(BossOutcome.Failed, lifecycle.Outcome);
            Assert.AreEqual(0, beganCount);
        }

        [Test]
        public void AbortWhenIdleOrFinishedIsIgnored()
        {
            var lifecycle = new BossLifecycle();
            Assert.IsFalse(lifecycle.Abort());

            lifecycle.Initialize();
            lifecycle.Begin();
            lifecycle.Complete(BossOutcome.Passed);

            Assert.IsFalse(lifecycle.Abort());
            Assert.AreEqual(BossOutcome.Passed, lifecycle.Outcome);
        }

        [Test]
        public void CompleteWithNoneThrows()
        {
            var lifecycle = new BossLifecycle();
            lifecycle.Initialize();
            lifecycle.Begin();

            Assert.Throws<ArgumentException>(() => lifecycle.Complete(BossOutcome.None));
        }

        [Test]
        public void CompleteWhenNotActiveIsIgnored()
        {
            var lifecycle = new BossLifecycle();
            Assert.IsFalse(lifecycle.Complete(BossOutcome.Passed));

            lifecycle.Initialize();
            Assert.IsFalse(lifecycle.Complete(BossOutcome.Passed));
            Assert.AreEqual(BossStage.Ready, lifecycle.Stage);
        }

        [Test]
        public void InitializeWhileActiveIsRefused()
        {
            var lifecycle = new BossLifecycle();
            lifecycle.Initialize();
            lifecycle.Begin();

            Assert.IsFalse(lifecycle.Initialize());
            Assert.IsTrue(lifecycle.IsActive);
        }

        [Test]
        public void InitializeAfterFinishedRestartsWithNoOutcome()
        {
            var lifecycle = new BossLifecycle();
            lifecycle.Initialize();
            lifecycle.Begin();
            lifecycle.Complete(BossOutcome.Failed);

            Assert.IsTrue(lifecycle.Initialize());
            Assert.AreEqual(BossStage.Ready, lifecycle.Stage);
            Assert.AreEqual(BossOutcome.None, lifecycle.Outcome);
            Assert.IsTrue(lifecycle.Begin());
        }

        [Test]
        public void ResetReturnsToIdle()
        {
            var lifecycle = new BossLifecycle();
            lifecycle.Initialize();
            lifecycle.Begin();
            lifecycle.Complete(BossOutcome.Passed);

            lifecycle.Reset();

            Assert.AreEqual(BossStage.Idle, lifecycle.Stage);
            Assert.AreEqual(BossOutcome.None, lifecycle.Outcome);
            Assert.Throws<InvalidOperationException>(() => lifecycle.Begin());
        }

        [Test]
        public void ContextRequiresPlayer()
        {
            Assert.Throws<ArgumentNullException>(() => new BossContext(null));
        }
    }
}
