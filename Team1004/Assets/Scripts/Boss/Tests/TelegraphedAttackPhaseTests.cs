using System.Collections.Generic;
using Game.StateMachine;
using NUnit.Framework;

namespace Game.Boss.Tests
{
    public sealed class TelegraphedAttackPhaseTests
    {
        private sealed class FakeBoss
        {
            public readonly List<string> Log = new();
            public bool AllowAttack = true;
        }

        private sealed class RecordingPhase : TelegraphedAttackPhase<FakeBoss, string>
        {
            public RecordingPhase(StateMachine<FakeBoss, string> machine, BossTimer timer, BossAttackTiming timing)
                : base(machine, timer, timing, "done")
            {
            }

            protected override bool CanStartAttack(FakeBoss boss) => boss.AllowAttack;
            protected override void OnTelegraphBegin(FakeBoss boss) => boss.Log.Add("T");
            protected override void OnAttackBegin(FakeBoss boss) => boss.Log.Add("A");
            protected override void OnAttackEnd(FakeBoss boss) => boss.Log.Add("E");
            protected override void OnRecoveryEnd(FakeBoss boss) => boss.Log.Add("R");
            protected override void OnPhaseExpired(FakeBoss boss) => boss.Log.Add("X");
            protected override void OnPhaseExit(FakeBoss boss) => boss.Log.Add("exit");
        }

        private sealed class DoneState : State<FakeBoss>
        {
        }

        private sealed class FullLanePhase : TelegraphedAttackPhase<FakeBoss, string>
        {
            public FullLanePhase(StateMachine<FakeBoss, string> machine, BossTimer timer, BossAttackTiming timing)
                : base(machine, timer, timing, "done")
            {
            }

            protected override void OnTelegraphBegin(FakeBoss boss) => boss.Log.Add("T");
            protected override float GetTelegraphDuration(FakeBoss boss) => Timing.FullLaneTelegraph;
            protected override void OnTelegraphImminent(FakeBoss boss) => boss.Log.Add("I");
            protected override void OnAttackBegin(FakeBoss boss) => boss.Log.Add("A");
            protected override void OnAttackEnd(FakeBoss boss) => boss.Log.Add("E");
        }

        [Test]
        public void FullLaneTelegraphUsesLongerDurationAndRaisesImminentOnce()
        {
            var boss = new FakeBoss();
            var machine = new StateMachine<FakeBoss, string>(boss);
            var phase = new FullLanePhase(machine, new BossTimer(10f), new BossAttackTiming(0.8f, 0.6f, 0.6f, 1f))
            {
                ImminentLead = 0.2f
            };
            machine.Add("attack", phase);
            machine.Add("done", new DoneState());
            machine.ChangeState("attack");

            machine.Update(0.1f);
            Assert.AreEqual(1f, phase.CurrentTelegraph, 0.0001f);
            Assert.AreEqual(1f, phase.StepDuration, 0.0001f);

            machine.Update(0.75f);
            CollectionAssert.AreEqual(new[] { "T" }, boss.Log);

            machine.Update(0.1f);
            CollectionAssert.AreEqual(new[] { "T", "I" }, boss.Log);

            machine.Update(0.1f);
            CollectionAssert.AreEqual(new[] { "T", "I" }, boss.Log);

            machine.Update(0.1f);
            CollectionAssert.AreEqual(new[] { "T", "I", "A" }, boss.Log);
            Assert.AreEqual(BossAttackStep.Attack, phase.Step);
        }

        private static (StateMachine<FakeBoss, string> machine, RecordingPhase phase, FakeBoss boss) Build(float duration)
        {
            var boss = new FakeBoss();
            var machine = new StateMachine<FakeBoss, string>(boss);
            var phase = new RecordingPhase(machine, new BossTimer(duration), new BossAttackTiming(0.5f, 0.5f, 0.5f));
            machine.Add("attack", phase);
            machine.Add("done", new DoneState());
            machine.ChangeState("attack");
            return (machine, phase, boss);
        }

        [Test]
        public void CyclesTelegraphAttackRecoveryAndFinishesAtBoundaryAfterExpiry()
        {
            var (machine, phase, boss) = Build(2f);

            machine.Update(0.1f);
            Assert.AreEqual(BossAttackStep.Telegraph, phase.Step);

            machine.Update(0.5f);
            Assert.AreEqual(BossAttackStep.Attack, phase.Step);
            Assert.IsTrue(phase.IsAttacking);

            machine.Update(0.5f);
            Assert.AreEqual(BossAttackStep.Recovery, phase.Step);

            machine.Update(0.5f);
            Assert.AreEqual(BossAttackStep.Telegraph, phase.Step);
            Assert.AreEqual(2, phase.AttackCount);

            machine.Update(0.5f);
            Assert.IsTrue(phase.Timer.IsExpired);
            Assert.AreEqual(BossAttackStep.Attack, phase.Step);
            Assert.AreEqual("attack", machine.CurrentKey);

            machine.Update(0.5f);
            Assert.AreEqual("attack", machine.CurrentKey);

            machine.Update(0.5f);
            Assert.AreEqual("done", machine.CurrentKey);
            CollectionAssert.AreEqual(new[] { "T", "A", "E", "R", "T", "A", "E", "R", "X", "exit" }, boss.Log);
        }

        [Test]
        public void ZeroDurationFinishesWithoutAttacking()
        {
            var (machine, phase, boss) = Build(0f);

            machine.Update(0.1f);

            Assert.AreEqual("done", machine.CurrentKey);
            Assert.AreEqual(0, phase.AttackCount);
            CollectionAssert.AreEqual(new[] { "X", "exit" }, boss.Log);
        }

        [Test]
        public void WaitsInIdleWhileAttackIsNotAllowed()
        {
            var (machine, phase, boss) = Build(10f);
            boss.AllowAttack = false;

            machine.Update(0.5f);
            machine.Update(0.5f);
            Assert.AreEqual(BossAttackStep.Idle, phase.Step);
            Assert.AreEqual(0, phase.AttackCount);

            boss.AllowAttack = true;
            machine.Update(0.1f);
            Assert.AreEqual(BossAttackStep.Telegraph, phase.Step);
        }

        [Test]
        public void ReenteringRestartsTimerAndCount()
        {
            var (machine, phase, boss) = Build(10f);
            machine.Update(0.1f);
            machine.Update(0.5f);
            Assert.AreEqual(1, phase.AttackCount);

            machine.Restart();
            Assert.AreEqual(0, phase.AttackCount);
            Assert.AreEqual(0f, phase.Timer.Elapsed);
            Assert.AreEqual(BossAttackStep.Idle, phase.Step);
            Assert.Contains("exit", boss.Log);
        }

        [Test]
        public void StepProgressIsNormalized()
        {
            var (machine, phase, _) = Build(10f);
            machine.Update(0.1f);
            machine.Update(0.25f);

            Assert.AreEqual(BossAttackStep.Telegraph, phase.Step);
            Assert.AreEqual(0.5f, phase.Step01, 0.0001f);
            Assert.AreEqual(0.5f, phase.StepDuration, 0.0001f);
        }
    }
}
