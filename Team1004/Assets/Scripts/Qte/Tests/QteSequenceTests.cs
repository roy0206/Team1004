using NUnit.Framework;
using UnityEngine;

namespace Game.Qte.Tests
{
    public sealed class QteSequenceTests
    {
        private const int Presses = 6;
        private const float Budget = 2f;

        private static QteSequence CreateSequence(bool firstKeyUp = true, float budget = Budget, int presses = Presses)
        {
            var sequence = new QteSequence();
            sequence.Begin(presses, firstKeyUp, budget);
            return sequence;
        }

        private static QteKey Other(QteKey key)
        {
            return key == QteKey.Up ? QteKey.Down : QteKey.Up;
        }

        [Test]
        public void Begin_StartsRunning_AndExpectsTheFirstKey()
        {
            var sequence = CreateSequence();

            Assert.AreEqual(QteState.Running, sequence.State);
            Assert.IsTrue(sequence.IsRunning);
            Assert.AreEqual(QteKey.Up, sequence.Expected);
            Assert.AreEqual(0, sequence.Progress);
            Assert.AreEqual(Presses, sequence.RequiredPresses);
            Assert.AreEqual(Budget, sequence.TimeBudget, 1e-4f);
            Assert.AreEqual(1f, sequence.Remaining01, 1e-4f);
        }

        [Test]
        public void Begin_CanStartWithDown()
        {
            var sequence = CreateSequence(false);

            Assert.AreEqual(QteKey.Down, sequence.Expected);
        }

        [Test]
        public void Press_AlternatesExpectedKey()
        {
            var sequence = CreateSequence();

            Assert.IsTrue(sequence.Press(QteKey.Up));
            Assert.AreEqual(QteKey.Down, sequence.Expected);
            Assert.AreEqual(1, sequence.Progress);

            Assert.IsTrue(sequence.Press(QteKey.Down));
            Assert.AreEqual(QteKey.Up, sequence.Expected);
            Assert.AreEqual(2, sequence.Progress);
        }

        [Test]
        public void Press_RejectsTheSameKeyTwice_WithoutProgressOrPenalty()
        {
            var sequence = CreateSequence();
            var rejected = QteKey.None;
            sequence.Rejected += key => rejected = key;

            Assert.IsTrue(sequence.Press(QteKey.Up));
            Assert.IsFalse(sequence.Press(QteKey.Up));

            Assert.AreEqual(QteKey.Up, rejected);
            Assert.AreEqual(1, sequence.Progress);
            Assert.AreEqual(QteKey.Down, sequence.Expected);
            Assert.AreEqual(QteState.Running, sequence.State);
            Assert.AreEqual(Budget, sequence.Remaining, 1e-4f);
        }

        [Test]
        public void Press_RejectsTheWrongKey_AtTheStart()
        {
            var sequence = CreateSequence();

            Assert.IsFalse(sequence.Press(QteKey.Down));
            Assert.AreEqual(0, sequence.Progress);
            Assert.AreEqual(QteKey.Up, sequence.Expected);
        }

        [Test]
        public void Press_IgnoresNone()
        {
            var sequence = CreateSequence();
            var rejects = 0;
            sequence.Rejected += _ => rejects++;

            Assert.IsFalse(sequence.Press(QteKey.None));
            Assert.AreEqual(0, rejects);
            Assert.AreEqual(0, sequence.Progress);
        }

        [Test]
        public void Completes_AfterExactlyRequiredPresses()
        {
            var sequence = CreateSequence();
            var completed = 0;
            var accepted = 0;
            sequence.Completed += () => completed++;
            sequence.Accepted += _ => accepted++;

            for (var i = 0; i < Presses; i++)
            {
                Assert.AreEqual(QteState.Running, sequence.State, "Completed too early at press " + i);
                Assert.IsTrue(sequence.Press(sequence.Expected));
            }

            Assert.AreEqual(1, completed);
            Assert.AreEqual(Presses, accepted);
            Assert.AreEqual(QteState.Completed, sequence.State);
            Assert.IsTrue(sequence.IsCompleted);
            Assert.AreEqual(Presses, sequence.Progress);
            Assert.AreEqual(1f, sequence.Progress01, 1e-4f);
            Assert.AreEqual(QteKey.None, sequence.Expected);
        }

        [Test]
        public void WrongPresses_DoNotCountTowardCompletion()
        {
            var sequence = CreateSequence();

            for (var i = 0; i < Presses * 2; i++)
            {
                sequence.Press(Other(sequence.Expected));

                if (!sequence.IsRunning)
                    break;
            }

            Assert.AreEqual(QteState.Running, sequence.State);
            Assert.AreEqual(0, sequence.Progress);
        }

        [Test]
        public void Press_IsIgnored_AfterCompletion()
        {
            var sequence = CreateSequence(true, Budget, 1);

            Assert.IsTrue(sequence.Press(QteKey.Up));
            Assert.AreEqual(QteState.Completed, sequence.State);
            Assert.IsFalse(sequence.Press(QteKey.Up));
            Assert.AreEqual(1, sequence.Progress);
        }

        [Test]
        public void Tick_FailsWhenTheTimeBudgetRunsOut()
        {
            var sequence = CreateSequence();
            var failed = 0;
            sequence.Failed += () => failed++;

            sequence.Tick(Budget * 0.5f);
            Assert.AreEqual(QteState.Running, sequence.State);
            Assert.AreEqual(0.5f, sequence.Remaining01, 1e-3f);

            sequence.Tick(Budget * 0.5f);

            Assert.AreEqual(1, failed);
            Assert.AreEqual(QteState.Failed, sequence.State);
            Assert.IsTrue(sequence.IsFailed);
            Assert.AreEqual(0f, sequence.Remaining01, 1e-4f);
        }

        [Test]
        public void Tick_NeverFails_WithoutATimeBudget()
        {
            var sequence = CreateSequence(true, 0f);

            Assert.IsFalse(sequence.HasTimeBudget);

            sequence.Tick(100f);

            Assert.AreEqual(QteState.Running, sequence.State);
            Assert.AreEqual(1f, sequence.Remaining01, 1e-4f);
        }

        [Test]
        public void SetRemaining_DrivesTheBarAndFailsAtZero()
        {
            var sequence = CreateSequence();

            sequence.SetRemaining(Budget * 0.25f);
            Assert.AreEqual(0.25f, sequence.Remaining01, 1e-3f);
            Assert.AreEqual(QteState.Running, sequence.State);

            sequence.SetRemaining(0f);
            Assert.AreEqual(QteState.Failed, sequence.State);
        }

        [Test]
        public void Fail_StopsTheSequence_AndIsIdempotent()
        {
            var sequence = CreateSequence();
            var failed = 0;
            sequence.Failed += () => failed++;

            sequence.Fail();
            sequence.Fail();

            Assert.AreEqual(1, failed);
            Assert.AreEqual(QteState.Failed, sequence.State);
            Assert.IsFalse(sequence.Press(QteKey.Up));
        }

        [Test]
        public void Cancel_ReturnsToIdle()
        {
            var sequence = CreateSequence();
            sequence.Press(QteKey.Up);
            sequence.Cancel();

            Assert.AreEqual(QteState.Idle, sequence.State);
            Assert.AreEqual(0, sequence.Progress);
            Assert.AreEqual(QteKey.None, sequence.Expected);
            Assert.IsFalse(sequence.Press(QteKey.Up));
        }

        [Test]
        public void Begin_ClampsPressesToAtLeastOne()
        {
            var sequence = CreateSequence(true, Budget, 0);

            Assert.AreEqual(1, sequence.RequiredPresses);
            Assert.IsTrue(sequence.Press(QteKey.Up));
            Assert.AreEqual(QteState.Completed, sequence.State);
        }

        [Test]
        public void ExpectedChanged_ReportsEveryStep()
        {
            var sequence = new QteSequence();
            var keys = new System.Collections.Generic.List<QteKey>();
            sequence.ExpectedChanged += keys.Add;

            sequence.Begin(3, true, Budget);
            sequence.Press(QteKey.Up);
            sequence.Press(QteKey.Down);
            sequence.Press(QteKey.Up);

            CollectionAssert.AreEqual(
                new[] { QteKey.Up, QteKey.Down, QteKey.Up, QteKey.None },
                keys);
        }

        [Test]
        public void Data_ExposesDefaults()
        {
            var data = ScriptableObject.CreateInstance<QteData>();

            try
            {
                Assert.AreEqual(6, data.RequiredPresses);
                Assert.IsTrue(data.FirstKeyUp);
                Assert.AreEqual(0.18f, data.PressFlashDuration, 1e-4f);
                Assert.AreEqual(QteModule.DefaultUpAction, data.UpAction);
                Assert.AreEqual(QteModule.DefaultDownAction, data.DownAction);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
