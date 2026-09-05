using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.StateMachine.Tests
{
    public sealed class FsmContext
    {
        public List<string> Log { get; } = new();
        public bool Flag { get; set; }
    }

    internal sealed class RecordingState : State<FsmContext>
    {
        private readonly string name;

        public RecordingState(string name)
        {
            this.name = name;
        }

        public override void OnEnter(FsmContext context)
        {
            context.Log.Add(name + ":enter");
        }

        public override void OnUpdate(FsmContext context, float deltaTime)
        {
            context.Log.Add(name + ":update");
        }

        public override void OnExit(FsmContext context)
        {
            context.Log.Add(name + ":exit");
        }
    }

    internal sealed class EnterRedirectState : State<FsmContext>
    {
        private readonly string name;
        private readonly StateMachine<FsmContext, string> machine;
        private readonly string target;

        public EnterRedirectState(string name, StateMachine<FsmContext, string> machine, string target)
        {
            this.name = name;
            this.machine = machine;
            this.target = target;
        }

        public override void OnEnter(FsmContext context)
        {
            context.Log.Add(name + ":enter");
            machine.ChangeState(target);
            context.Log.Add(name + ":enter-done");
        }

        public override void OnExit(FsmContext context)
        {
            context.Log.Add(name + ":exit");
        }
    }

    internal sealed class ExitRedirectState : State<FsmContext>
    {
        private readonly string name;
        private readonly StateMachine<FsmContext, string> machine;
        private readonly string target;

        public ExitRedirectState(string name, StateMachine<FsmContext, string> machine, string target)
        {
            this.name = name;
            this.machine = machine;
            this.target = target;
        }

        public override void OnEnter(FsmContext context)
        {
            context.Log.Add(name + ":enter");
        }

        public override void OnExit(FsmContext context)
        {
            context.Log.Add(name + ":exit");
            machine.ChangeState(target);
        }
    }

    internal sealed class UpdateInEnterState : State<FsmContext>
    {
        private readonly StateMachine<FsmContext, string> machine;

        public UpdateInEnterState(StateMachine<FsmContext, string> machine)
        {
            this.machine = machine;
        }

        public override void OnEnter(FsmContext context)
        {
            machine.Update(0.1f);
        }
    }

    internal sealed class FlagFinishTimedState : TimedState<FsmContext, string>
    {
        public FlagFinishTimedState(StateMachine<FsmContext, string> machine, float duration, string next)
            : base(machine, duration, next)
        {
        }

        protected override void OnTimedUpdate(FsmContext context, float deltaTime)
        {
            if (context.Flag)
                Finish();
        }
    }

    public sealed class StateMachineTests
    {
        private FsmContext context;
        private StateMachine<FsmContext, string> machine;
        private List<(string From, string To)> changes;

        [SetUp]
        public void SetUp()
        {
            context = new FsmContext();
            machine = new StateMachine<FsmContext, string>(context);
            changes = new List<(string, string)>();
            machine.StateChanged += (from, to) => changes.Add((from, to));
        }

        private void AddRecording(params string[] keys)
        {
            foreach (var key in keys)
                machine.Add(key, new RecordingState(key));
        }

        [Test]
        public void Add_DuplicateKey_Throws()
        {
            AddRecording("A");
            Assert.Throws<ArgumentException>(() => machine.Add("A", new RecordingState("A2")));
        }

        [Test]
        public void Add_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => machine.Add("A", null));
        }

        [Test]
        public void ChangeState_UnknownKey_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => machine.ChangeState("Missing"));
        }

        [Test]
        public void ChangeState_FirstState_EntersWithoutExit()
        {
            AddRecording("A");

            machine.ChangeState("A");

            CollectionAssert.AreEqual(new[] { "A:enter" }, context.Log);
            Assert.IsTrue(machine.HasCurrent);
            Assert.AreEqual("A", machine.CurrentKey);
            Assert.IsNull(machine.Previous);
            Assert.IsNull(machine.PreviousKey);
        }

        [Test]
        public void ChangeState_ExitsCurrentThenEntersNext()
        {
            AddRecording("A", "B");

            machine.ChangeState("A");
            machine.ChangeState("B");

            CollectionAssert.AreEqual(new[] { "A:enter", "A:exit", "B:enter" }, context.Log);
            Assert.AreEqual("B", machine.CurrentKey);
            Assert.AreEqual("A", machine.PreviousKey);
        }

        [Test]
        public void ChangeState_SameKey_DoesNothing()
        {
            AddRecording("A");

            machine.ChangeState("A");
            machine.ChangeState("A");

            CollectionAssert.AreEqual(new[] { "A:enter" }, context.Log);
            Assert.AreEqual(1, changes.Count);
        }

        [Test]
        public void StateChanged_ReportsFromAndTo()
        {
            AddRecording("A", "B");

            machine.ChangeState("A");
            machine.ChangeState("B");

            Assert.AreEqual(2, changes.Count);
            Assert.IsNull(changes[0].From);
            Assert.AreEqual("A", changes[0].To);
            Assert.AreEqual("A", changes[1].From);
            Assert.AreEqual("B", changes[1].To);
        }

        [Test]
        public void Update_WithoutCurrent_DoesNothing()
        {
            AddRecording("A");

            machine.Update(1f);

            Assert.IsEmpty(context.Log);
            Assert.AreEqual(0f, machine.TimeInState);
        }

        [Test]
        public void Update_CallsCurrentStateUpdate()
        {
            AddRecording("A");
            machine.ChangeState("A");

            machine.Update(0.1f);
            machine.Update(0.1f);

            CollectionAssert.AreEqual(new[] { "A:enter", "A:update", "A:update" }, context.Log);
        }

        [Test]
        public void TimeInState_AccumulatesAndResetsOnChange()
        {
            AddRecording("A", "B");
            machine.ChangeState("A");

            machine.Update(0.25f);
            machine.Update(0.5f);
            Assert.AreEqual(0.75f, machine.TimeInState, 0.0001f);

            machine.ChangeState("B");
            Assert.AreEqual(0f, machine.TimeInState);

            machine.Update(0.1f);
            Assert.AreEqual(0.1f, machine.TimeInState, 0.0001f);
        }

        [Test]
        public void AddTransition_MovesWhenConditionTrue_AndUpdatesNewStateSameFrame()
        {
            AddRecording("A", "B");
            machine.AddTransition("A", "B", c => c.Flag);
            machine.ChangeState("A");

            machine.Update(0.1f);
            Assert.AreEqual("A", machine.CurrentKey);

            context.Flag = true;
            machine.Update(0.1f);

            Assert.AreEqual("B", machine.CurrentKey);
            CollectionAssert.AreEqual(
                new[] { "A:enter", "A:update", "A:exit", "B:enter", "B:update" },
                context.Log);
        }

        [Test]
        public void AddTransition_OnlyFiresFromSourceState()
        {
            AddRecording("A", "B", "C");
            machine.AddTransition("A", "B", c => c.Flag);
            machine.ChangeState("C");
            context.Flag = true;

            machine.Update(0.1f);

            Assert.AreEqual("C", machine.CurrentKey);
        }

        [Test]
        public void AddTransition_FirstRegisteredWins()
        {
            AddRecording("A", "B", "C");
            machine.AddTransition("A", "B", c => c.Flag);
            machine.AddTransition("A", "C", c => c.Flag);
            machine.ChangeState("A");
            context.Flag = true;

            machine.Update(0.1f);

            Assert.AreEqual("B", machine.CurrentKey);
        }

        [Test]
        public void AddTransition_SameSourceAndTarget_Throws()
        {
            AddRecording("A");
            Assert.Throws<ArgumentException>(() => machine.AddTransition("A", "A", c => true));
        }

        [Test]
        public void AddTransition_UnregisteredKey_Throws()
        {
            AddRecording("A");
            Assert.Throws<KeyNotFoundException>(() => machine.AddTransition("A", "Missing", c => true));
        }

        [Test]
        public void AddAnyTransition_FiresFromAnyState()
        {
            AddRecording("A", "B", "Dead");
            machine.AddAnyTransition("Dead", c => c.Flag);
            machine.ChangeState("B");
            context.Flag = true;

            machine.Update(0.1f);

            Assert.AreEqual("Dead", machine.CurrentKey);
        }

        [Test]
        public void AddAnyTransition_IgnoredWhenAlreadyInTarget()
        {
            AddRecording("A", "Dead");
            machine.AddAnyTransition("Dead", c => c.Flag);
            machine.ChangeState("Dead");
            context.Log.Clear();
            context.Flag = true;

            machine.Update(0.1f);

            CollectionAssert.AreEqual(new[] { "Dead:update" }, context.Log);
        }

        [Test]
        public void AddAnyTransition_TakesPriorityOverStateTransition()
        {
            AddRecording("A", "B", "C");
            machine.AddTransition("A", "B", c => c.Flag);
            machine.AddAnyTransition("C", c => c.Flag);
            machine.ChangeState("A");
            context.Flag = true;

            machine.Update(0.1f);

            Assert.AreEqual("C", machine.CurrentKey);
        }

        [Test]
        public void ChangeState_DuringEnter_IsQueuedUntilEnterCompletes()
        {
            machine.Add("A", new EnterRedirectState("A", machine, "B"));
            AddRecording("B");

            machine.ChangeState("A");

            CollectionAssert.AreEqual(
                new[] { "A:enter", "A:enter-done", "A:exit", "B:enter" },
                context.Log);
            Assert.AreEqual("B", machine.CurrentKey);
            Assert.AreEqual("A", machine.PreviousKey);
            Assert.IsFalse(machine.IsTransitioning);
            Assert.IsNull(changes[0].From);
            Assert.AreEqual("A", changes[0].To);
            Assert.AreEqual("A", changes[1].From);
            Assert.AreEqual("B", changes[1].To);
        }

        [Test]
        public void ChangeState_DuringExit_IsAppliedAfterPendingTransition()
        {
            machine.Add("A", new ExitRedirectState("A", machine, "C"));
            AddRecording("B", "C");
            machine.ChangeState("A");

            machine.ChangeState("B");

            CollectionAssert.AreEqual(
                new[] { "A:enter", "A:exit", "B:enter", "B:exit", "C:enter" },
                context.Log);
            Assert.AreEqual("C", machine.CurrentKey);
            Assert.IsFalse(machine.IsTransitioning);
        }

        [Test]
        public void ChangeState_DuringEnter_ToSameState_IsIgnored()
        {
            machine.Add("A", new EnterRedirectState("A", machine, "A"));

            machine.ChangeState("A");

            CollectionAssert.AreEqual(new[] { "A:enter", "A:enter-done" }, context.Log);
            Assert.AreEqual(1, changes.Count);
        }

        [Test]
        public void Update_DuringTransition_Throws()
        {
            machine.Add("A", new UpdateInEnterState(machine));

            Assert.Throws<InvalidOperationException>(() => machine.ChangeState("A"));
            Assert.IsFalse(machine.IsTransitioning);
        }

        [Test]
        public void Stop_ExitsCurrentAndClears()
        {
            AddRecording("A");
            machine.ChangeState("A");
            machine.Update(0.5f);

            machine.Stop();

            CollectionAssert.AreEqual(new[] { "A:enter", "A:update", "A:exit" }, context.Log);
            Assert.IsFalse(machine.HasCurrent);
            Assert.IsNull(machine.Current);
            Assert.AreEqual("A", machine.PreviousKey);
            Assert.AreEqual(0f, machine.TimeInState);
            Assert.AreEqual("A", changes[1].From);
            Assert.IsNull(changes[1].To);
        }

        [Test]
        public void Stop_WithoutCurrent_DoesNothing()
        {
            AddRecording("A");

            machine.Stop();

            Assert.IsEmpty(context.Log);
            Assert.IsEmpty(changes);
        }

        [Test]
        public void Restart_ExitsAndReentersSameState()
        {
            AddRecording("A");
            machine.ChangeState("A");
            machine.Update(0.5f);

            machine.Restart();

            CollectionAssert.AreEqual(new[] { "A:enter", "A:update", "A:exit", "A:enter" }, context.Log);
            Assert.AreEqual("A", machine.CurrentKey);
            Assert.AreEqual("A", machine.PreviousKey);
            Assert.AreEqual(0f, machine.TimeInState);
            Assert.AreEqual("A", changes[1].From);
            Assert.AreEqual("A", changes[1].To);
        }

        [Test]
        public void DebugLabel_ReflectsCurrentState()
        {
            AddRecording("A");

            Assert.AreEqual("(none)", machine.DebugLabel);

            machine.ChangeState("A");

            Assert.AreEqual("A", machine.DebugLabel);
            StringAssert.Contains("A", machine.ToString());
        }

        [Test]
        public void TimedState_TransitionsWhenDurationElapsed()
        {
            var timed = new TimedState<FsmContext, string>(machine, 1f, "B");
            machine.Add("A", timed);
            AddRecording("B");
            machine.ChangeState("A");

            machine.Update(0.5f);
            Assert.AreEqual("A", machine.CurrentKey);
            Assert.AreEqual(0.5f, timed.Elapsed, 0.0001f);
            Assert.AreEqual(0.5f, timed.Remaining, 0.0001f);

            machine.Update(0.5f);
            Assert.AreEqual("B", machine.CurrentKey);
            CollectionAssert.AreEqual(new[] { "B:enter" }, context.Log);
        }

        [Test]
        public void TimedState_Finish_TransitionsEarly()
        {
            machine.Add("A", new FlagFinishTimedState(machine, 10f, "B"));
            AddRecording("B");
            machine.ChangeState("A");

            machine.Update(0.1f);
            Assert.AreEqual("A", machine.CurrentKey);

            context.Flag = true;
            machine.Update(0.1f);

            Assert.AreEqual("B", machine.CurrentKey);
            CollectionAssert.AreEqual(new[] { "B:enter" }, context.Log);
        }

        [Test]
        public void TimedState_ResetsElapsedOnReenter()
        {
            var timed = new TimedState<FsmContext, string>(machine, 1f, "B");
            machine.Add("A", timed);
            AddRecording("B");
            machine.ChangeState("A");
            machine.Update(0.8f);

            machine.ChangeState("B");
            machine.ChangeState("A");

            Assert.AreEqual(0f, timed.Elapsed);
            machine.Update(0.8f);
            Assert.AreEqual("A", machine.CurrentKey);
        }

        [Test]
        public void TimedState_NullMachine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TimedState<FsmContext, string>(null, 1f, "B"));
        }

        [Test]
        public void Module_ExposesMachineAndDoesNotStartBeforeAttach()
        {
            var module = new StateMachineModule<FsmContext, string>(context, "A");
            module.Add("A", new RecordingState("A"));

            Assert.IsNotNull(module.Machine);
            Assert.AreSame(context, module.Context);
            Assert.AreEqual("A", module.InitialKey);
            Assert.IsFalse(module.HasCurrent);
            Assert.IsFalse(module.IsAttached);
            Assert.IsEmpty(context.Log);
        }

        [Test]
        public void Module_RejectsUnsupportedTicks()
        {
            Assert.Throws<ArgumentException>(
                () => new StateMachineModule<FsmContext, string>(context, "A", ModuleTick.Interval));
            Assert.Throws<ArgumentException>(
                () => new StateMachineModule<FsmContext, string>(context, "A", ModuleTick.None));
        }

        [Test]
        public void Module_ForwardsStateChangedEvent()
        {
            var module = new StateMachineModule<FsmContext, string>(context, "A");
            module.Add("A", new RecordingState("A"));
            module.Add("B", new RecordingState("B"));
            var received = new List<(string, string)>();
            module.StateChanged += (from, to) => received.Add((from, to));

            module.ChangeState("A");
            module.ChangeState("B");

            Assert.AreEqual(2, received.Count);
            Assert.AreEqual("B", module.CurrentKey);
            Assert.AreEqual("A", module.PreviousKey);
        }
    }
}
