using Game.Play;
using UnityEngine;

namespace Game.Boss.Integration
{
    [DefaultExecutionOrder(-50)]
    public sealed class BossDirector : MonoThing
    {
        [SerializeField] private BossThing[] bosses;
        [SerializeField] private Camera stageCamera;

        private PlayFlow flow;
        private bool hooked;

        public BossThing Running { get; private set; }
        public int BossCount => bosses != null ? bosses.Length : 0;

        private void Start()
        {
            Hook();
        }

        protected override void OnThingDestroy()
        {
            Unhook();
        }

        public BossThing GetBoss(int section)
        {
            var index = section - 1;

            if (bosses == null || index < 0 || index >= bosses.Length)
                return null;

            return bosses[index];
        }

        public async Awaitable<bool> RunBossAsync(int section)
        {
            var boss = GetBoss(section);

            if (boss == null)
            {
                Debug.LogWarning($"[BossDirector] No boss is assigned for section {section}. The boss is skipped.", this);
                return true;
            }

            if (flow == null || flow.Player == null)
            {
                Debug.LogWarning("[BossDirector] PlayFlow or its player is missing. The boss is skipped.", this);
                return true;
            }

            var camera = stageCamera != null ? stageCamera : Camera.main;
            var scrollRoot = flow.Scroller != null ? flow.Scroller.transform : null;

            boss.EntryCheckpoint = PlayFlow.CheckpointTag;
            boss.Initialize(new BossContext(flow.Player, scrollRoot, camera));
            boss.Impact += OnBossImpact;
            Running = boss;
            flow.SetBossHoldsWorld(boss.HoldsWorld);
            var view = flow.BossTimer;
            var viewShown = false;

            if (view != null)
                view.SetBoss(boss.DisplayName);

            try
            {
                boss.Begin();

                while (boss != null && !boss.IsFinished)
                {
                    if (flow == null || flow.IsTerminal || flow.IsBossInterrupted)
                    {
                        boss.Abort();
                        break;
                    }

                    var checkpoint = boss.ActiveCheckpoint;

                    if (!string.IsNullOrEmpty(checkpoint))
                        flow.SetBossCheckpoint(checkpoint);

                    UpdateTimerView(view, boss, ref viewShown);
                    await Awaitable.NextFrameAsync();
                }
            }
            finally
            {
                Running = null;

                if (flow != null)
                    flow.SetBossHoldsWorld(false);

                if (boss != null)
                    boss.Impact -= OnBossImpact;

                if (view != null)
                    view.SetVisible(false);
            }

            return boss != null && boss.Outcome == BossOutcome.Passed;
        }

        private void OnBossImpact()
        {
            if (flow != null)
                flow.ReportImpact();
        }

        private void Hook()
        {
            if (hooked)
                return;

            if (!PlayFlow.TryGetCurrent(out var current))
            {
                Debug.LogWarning("[BossDirector] PlayFlow is not in the scene. Bosses are disabled.", this);
                return;
            }

            flow = current;
            flow.BossHandler = RunBossAsync;
            flow.BossHoldsWorldQuery = section => GetBoss(section) != null && GetBoss(section).HoldsWorld;
            hooked = true;
        }

        private void Unhook()
        {
            if (!hooked)
                return;

            hooked = false;

            if (flow != null && flow.BossHandler != null && ReferenceEquals(flow.BossHandler.Target, this))
            {
                flow.BossHandler = null;
                flow.BossHoldsWorldQuery = null;
            }

            flow = null;
        }

        private static void UpdateTimerView(BossTimerView view, BossThing boss, ref bool shown)
        {
            if (view == null)
                return;

            var timer = boss.Timer;

            if (timer == null)
            {
                if (shown)
                {
                    shown = false;
                    view.SetVisible(false);
                }

                return;
            }

            if (!shown)
            {
                shown = true;
                view.SetVisible(true);
            }

            view.SetRemaining(timer.Remaining);
        }
    }
}
