using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.StateMachine
{
    public sealed class StateMachineModule<TContext, TKey> : Module
    {
        private const ModuleTick AllowedTicks = ModuleTick.Update | ModuleTick.FixedUpdate;

        private readonly StateMachine<TContext, TKey> machine;
        private readonly TKey initialKey;
        private readonly ModuleTick ticks;
        private readonly bool useUnscaledTime;
        private readonly bool exitOnDetach;

        public StateMachineModule(
            TContext context,
            TKey initialKey,
            ModuleTick ticks = ModuleTick.Update,
            bool useUnscaledTime = false,
            bool exitOnDetach = true,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (ticks == ModuleTick.None || (ticks & ~AllowedTicks) != 0)
                throw new ArgumentException("Only Update and FixedUpdate ticks are supported.", nameof(ticks));

            machine = new StateMachine<TContext, TKey>(context, keyComparer);
            this.initialKey = initialKey;
            this.ticks = ticks;
            this.useUnscaledTime = useUnscaledTime;
            this.exitOnDetach = exitOnDetach;
        }

        public StateMachine<TContext, TKey> Machine => machine;
        public TContext Context => machine.Context;
        public TKey InitialKey => initialKey;
        public IState<TContext> Current => machine.Current;
        public TKey CurrentKey => machine.CurrentKey;
        public bool HasCurrent => machine.HasCurrent;
        public IState<TContext> Previous => machine.Previous;
        public TKey PreviousKey => machine.PreviousKey;
        public float TimeInState => machine.TimeInState;
        public string DebugLabel => machine.DebugLabel;

        public event Action<TKey, TKey> StateChanged
        {
            add => machine.StateChanged += value;
            remove => machine.StateChanged -= value;
        }

        protected override ModuleTick Ticks => ticks;

        public void Add(TKey key, IState<TContext> state)
        {
            machine.Add(key, state);
        }

        public void AddTransition(TKey from, TKey to, Func<TContext, bool> condition)
        {
            machine.AddTransition(from, to, condition);
        }

        public void AddAnyTransition(TKey to, Func<TContext, bool> condition)
        {
            machine.AddAnyTransition(to, condition);
        }

        public void ChangeState(TKey key)
        {
            machine.ChangeState(key);
        }

        public void Restart()
        {
            machine.Restart();
        }

        public override string ToString()
        {
            return machine.ToString();
        }

        protected override void OnAttached()
        {
            machine.ChangeState(initialKey);
        }

        protected override void OnDetached()
        {
            if (exitOnDetach && machine.HasCurrent)
                machine.Stop();
        }

        protected override void OnUpdate()
        {
            machine.Update(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        protected override void OnFixedUpdate()
        {
            machine.FixedUpdate(Time.fixedDeltaTime);
        }
    }
}
