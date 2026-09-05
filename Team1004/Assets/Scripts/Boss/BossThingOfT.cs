using System;
using Game.StateMachine;

namespace Game.Boss
{
    public abstract class BossThing<TSelf, TKey> : BossThing where TSelf : BossThing<TSelf, TKey>
    {
        private StateMachineModule<TSelf, TKey> fsm;
        private bool detachPending;

        protected StateMachineModule<TSelf, TKey> Fsm => fsm;
        protected StateMachine<TSelf, TKey> Machine => fsm != null ? fsm.Machine : null;

        public bool HasState => fsm != null && fsm.HasCurrent;
        public TKey CurrentKey => fsm != null ? fsm.CurrentKey : default;
        public float TimeInState => fsm != null ? fsm.TimeInState : 0f;
        public string StateLabel => fsm != null ? fsm.DebugLabel : "(none)";

        public event Action<TKey, TKey> StateChanged;

        protected abstract TKey InitialKey { get; }
        protected abstract void BuildStates(StateMachineModule<TSelf, TKey> fsm);
        protected virtual ModuleTick FsmTicks => ModuleTick.Update;
        protected virtual bool UseUnscaledTime => false;

        public void ChangeState(TKey key)
        {
            if (fsm == null)
                throw new InvalidOperationException($"{name}: The boss is not active, so there is no state to change.");

            fsm.ChangeState(key);
        }

        protected override void OnThingLateUpdate()
        {
            FlushPendingDetach();
        }

        private protected sealed override void AttachBehaviour()
        {
            RemoveFsm();

            var module = new StateMachineModule<TSelf, TKey>((TSelf)this, InitialKey, FsmTicks, UseUnscaledTime);
            module.StateChanged += OnFsmStateChanged;
            fsm = module;
            BuildStates(module);
            AddModule(module);
        }

        private protected sealed override void DetachBehaviour()
        {
            if (fsm == null)
                return;

            fsm.IsEnabled = false;

            if (fsm.Machine.IsTransitioning)
            {
                detachPending = true;
                return;
            }

            RemoveFsm();
        }

        private void FlushPendingDetach()
        {
            if (detachPending && fsm != null && !fsm.Machine.IsTransitioning)
                RemoveFsm();
        }

        private void RemoveFsm()
        {
            detachPending = false;
            var module = fsm;

            if (module == null)
                return;

            fsm = null;
            module.StateChanged -= OnFsmStateChanged;

            if (module.IsAttached)
                RemoveModule(module);
        }

        private void OnFsmStateChanged(TKey from, TKey to)
        {
            StateChanged?.Invoke(from, to);
        }
    }
}
