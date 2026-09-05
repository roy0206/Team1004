using System;
using Game.Config;
using Game.Player;
using UnityEngine;

namespace Game.Boss
{
    public abstract class BossThing : MonoThing
    {
        [SerializeField] private bool deactivateOnFinish = true;

        private readonly BossLifecycle lifecycle = new();
        private BossContext context;
        private bool subscribed;

        public BossContext Context => context;
        public BossStage Stage => lifecycle.Stage;
        public BossOutcome Outcome => lifecycle.Outcome;
        public bool IsInitialized => lifecycle.IsInitialized;
        public bool IsActive => lifecycle.IsActive;
        public bool IsFinished => lifecycle.IsFinished;

        public bool DeactivateOnFinish
        {
            get => deactivateOnFinish;
            set => deactivateOnFinish = value;
        }

        public LanePlayer Player => context != null ? context.Player : null;
        public GameConfigValues Config => GameConfig.Current;
        public int LaneCount => Config.LaneCount;
        public int MiddleLane => Config.LaneCount / 2;
        public int PlayerLane => Player != null ? Player.CurrentLane : MiddleLane;
        public bool IsPlayerVulnerable => Player != null && Player.IsVulnerable;
        public virtual BossTimer Timer => null;
        public virtual string DisplayName => name;
        public virtual bool HoldsWorld => false;

        public string EntryCheckpoint { get; set; }
        public virtual string ActiveCheckpoint => null;

        public event Action Began;
        public event Action<BossOutcome> Finished;
        public event Action Impact;
        public event Action TelegraphImminent;
        public event Action AttackBegan;

        protected void RaiseImpact()
        {
            Raise(Impact);
        }

        protected void RaiseTelegraphImminent()
        {
            Raise(TelegraphImminent);
        }

        protected void RaiseAttackBegan()
        {
            Raise(AttackBegan);
        }

        private void Raise(Action handler)
        {
            try
            {
                handler?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        public void Initialize(BossContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (lifecycle.IsActive)
                throw new InvalidOperationException($"{name}: Abort or ResetBoss before Initialize while the boss is active.");

            EnsureSubscribed();

            if (lifecycle.IsInitialized)
                ResetBoss();

            this.context = context;
            lifecycle.Initialize();
            OnInitialize();
        }

        public bool Begin()
        {
            return lifecycle.Begin();
        }

        public bool Complete(BossOutcome outcome)
        {
            return lifecycle.Complete(outcome);
        }

        public bool Abort()
        {
            return lifecycle.Abort();
        }

        public void ResetBoss()
        {
            DetachBehaviour();
            lifecycle.Reset();
            context = null;
            OnReset();

            if (deactivateOnFinish && gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public float GetLaneY(int lane)
        {
            return Config.GetLaneY(lane);
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnBegin()
        {
        }

        protected virtual void OnComplete(BossOutcome outcome)
        {
        }

        protected virtual void OnReset()
        {
        }

        private protected virtual void AttachBehaviour()
        {
        }

        private protected virtual void DetachBehaviour()
        {
        }

        private void EnsureSubscribed()
        {
            if (subscribed)
                return;

            subscribed = true;
            lifecycle.Began += OnLifecycleBegan;
            lifecycle.Finished += OnLifecycleFinished;
        }

        private void OnLifecycleBegan()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            OnBegin();

            if (!lifecycle.IsActive)
                return;

            AttachBehaviour();

            if (!lifecycle.IsActive)
                return;

            try
            {
                Began?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OnLifecycleFinished(BossOutcome outcome)
        {
            DetachBehaviour();
            OnComplete(outcome);

            try
            {
                Finished?.Invoke(outcome);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (deactivateOnFinish && gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
