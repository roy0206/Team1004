using System;
using System.Collections.Generic;

namespace Game.StateMachine
{
    public sealed class StateMachine<TContext, TKey>
    {
        private sealed class Entry
        {
            public Entry(TKey key, IState<TContext> state)
            {
                Key = key;
                State = state;
                Transitions = new List<Transition>();
            }

            public TKey Key { get; }
            public IState<TContext> State { get; }
            public List<Transition> Transitions { get; }
        }

        private readonly struct Transition
        {
            public Transition(Entry target, Func<TContext, bool> condition)
            {
                Target = target;
                Condition = condition;
            }

            public Entry Target { get; }
            public Func<TContext, bool> Condition { get; }
        }

        private readonly TContext context;
        private readonly Dictionary<TKey, Entry> entries;
        private readonly List<Transition> anyTransitions = new();

        private Entry current;
        private Entry previous;
        private Entry pending;
        private bool hasPending;
        private bool isTransitioning;
        private float timeInState;

        public StateMachine(TContext context, IEqualityComparer<TKey> keyComparer = null)
        {
            this.context = context;
            entries = new Dictionary<TKey, Entry>(keyComparer ?? EqualityComparer<TKey>.Default);
        }

        public event Action<TKey, TKey> StateChanged;

        public TContext Context => context;
        public IState<TContext> Current => current?.State;
        public TKey CurrentKey => current != null ? current.Key : default;
        public bool HasCurrent => current != null;
        public IState<TContext> Previous => previous?.State;
        public TKey PreviousKey => previous != null ? previous.Key : default;
        public float TimeInState => timeInState;
        public bool IsTransitioning => isTransitioning;
        public int StateCount => entries.Count;
        public string DebugLabel => current != null ? current.Key.ToString() : "(none)";

        public void Add(TKey key, IState<TContext> state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (entries.ContainsKey(key))
                throw new ArgumentException($"State key is already registered: '{key}'.", nameof(key));

            entries.Add(key, new Entry(key, state));
        }

        public bool Contains(TKey key)
        {
            return entries.ContainsKey(key);
        }

        public IState<TContext> Get(TKey key)
        {
            return GetEntry(key).State;
        }

        public bool TryGet(TKey key, out IState<TContext> state)
        {
            if (entries.TryGetValue(key, out var entry))
            {
                state = entry.State;
                return true;
            }

            state = null;
            return false;
        }

        public void AddTransition(TKey from, TKey to, Func<TContext, bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            var source = GetEntry(from);
            var target = GetEntry(to);

            if (source == target)
                throw new ArgumentException("A transition cannot target its own source state.", nameof(to));

            source.Transitions.Add(new Transition(target, condition));
        }

        public void AddAnyTransition(TKey to, Func<TContext, bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            anyTransitions.Add(new Transition(GetEntry(to), condition));
        }

        public void ClearTransitions()
        {
            anyTransitions.Clear();

            foreach (var entry in entries.Values)
                entry.Transitions.Clear();
        }

        public void ChangeState(TKey key)
        {
            var target = GetEntry(key);

            if (isTransitioning)
            {
                pending = target;
                hasPending = true;
                return;
            }

            if (target == current)
                return;

            RunTransition(target);
        }

        public void Restart()
        {
            ThrowIfTransitioning();

            if (current == null)
                return;

            RunTransition(current);
        }

        public void Stop()
        {
            ThrowIfTransitioning();

            if (current == null)
                return;

            isTransitioning = true;

            try
            {
                var from = current;
                from.State.OnExit(context);
                previous = from;
                current = null;
                timeInState = 0f;
                StateChanged?.Invoke(from.Key, default);
            }
            finally
            {
                isTransitioning = false;
            }

            FlushPending();
        }

        public void Update(float deltaTime)
        {
            ThrowIfTransitioning();

            if (current == null)
                return;

            timeInState += deltaTime;
            TryAutoTransition();

            if (current != null)
                current.State.OnUpdate(context, deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            ThrowIfTransitioning();

            if (current == null)
                return;

            current.State.OnFixedUpdate(context, fixedDeltaTime);
        }

        public override string ToString()
        {
            return $"StateMachine<{typeof(TContext).Name}>[{DebugLabel} {timeInState:0.00}s]";
        }

        private bool TryAutoTransition()
        {
            for (var i = 0; i < anyTransitions.Count; i++)
            {
                var transition = anyTransitions[i];

                if (transition.Target == current || !transition.Condition(context))
                    continue;

                RunTransition(transition.Target);
                return true;
            }

            var transitions = current.Transitions;

            for (var i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];

                if (!transition.Condition(context))
                    continue;

                RunTransition(transition.Target);
                return true;
            }

            return false;
        }

        private void RunTransition(Entry target)
        {
            isTransitioning = true;

            try
            {
                while (true)
                {
                    var from = current;
                    var fromKey = from != null ? from.Key : default;

                    if (from != null)
                        from.State.OnExit(context);

                    previous = from;
                    current = target;
                    timeInState = 0f;
                    target.State.OnEnter(context);
                    StateChanged?.Invoke(fromKey, target.Key);

                    if (!hasPending)
                        return;

                    hasPending = false;
                    target = pending;
                    pending = null;

                    if (target == current)
                        return;
                }
            }
            finally
            {
                isTransitioning = false;
                hasPending = false;
                pending = null;
            }
        }

        private void FlushPending()
        {
            if (!hasPending)
                return;

            hasPending = false;
            var target = pending;
            pending = null;

            if (target != current)
                RunTransition(target);
        }

        private Entry GetEntry(TKey key)
        {
            if (entries.TryGetValue(key, out var entry))
                return entry;

            throw new KeyNotFoundException($"State is not registered: '{key}'.");
        }

        private void ThrowIfTransitioning()
        {
            if (isTransitioning)
                throw new InvalidOperationException("Not allowed while a state transition is in progress.");
        }
    }
}
