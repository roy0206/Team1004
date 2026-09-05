using System;
using UnityEngine;

namespace Game.Boss
{
    public abstract class PatternBoss<TSelf, TKey, TData> : BossThing<TSelf, TKey>
        where TSelf : PatternBoss<TSelf, TKey, TData>
        where TData : BossData
    {
        private const string TelegraphSfxId = "boss_telegraph";
        private const string AttackSfxId = "boss_attack";

        [SerializeField] private TData data;
        [SerializeField] private LaneTelegraph telegraph;

        private readonly BossTimer timer = new(BossData.DefaultDuration);
        private BossPatternSelector selector;

        public TData Data => data;
        public bool HasData => data != null;
        public LaneTelegraph Telegraph => telegraph;
        public BossPatternSelector Patterns => selector;
        public BossAttackTiming Timing => data != null ? data.Timing : BossAttackTiming.Default;
        public override BossTimer Timer => timer;
        public override string DisplayName => data != null ? data.DisplayName : name;
        public float TelegraphImminentLead => data != null ? data.TelegraphImminentLead : 0.2f;

        public float LaneSpacing
        {
            get
            {
                if (LaneCount < 2)
                    return 1f;

                return Mathf.Abs(GetLaneY(0) - GetLaneY(1));
            }
        }

        protected override void OnInitialize()
        {
            timer.Reset();
            selector = null;

            if (data != null)
            {
                timer.Duration = data.Duration;
                selector = new BossPatternSelector(data.Patterns, data.Seed, data.AllowRepeatPattern);

                if (data.Patterns.Count == 0)
                    Debug.LogWarning($"[Boss] {name}: {data.name} has no patterns.", this);
            }

            HideTelegraph();
        }

        protected override void OnBegin()
        {
            if (data != null)
                return;

            Debug.LogError($"[Boss] {name}: data is not assigned. The boss is skipped.", this);
            Complete(BossOutcome.Passed);
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            timer.Stop();
            HideTelegraph();
        }

        protected override void OnReset()
        {
            timer.Reset();
            HideTelegraph();
        }

        public bool TryPickPattern(out BossPattern pattern)
        {
            return TryPickPattern(null, out pattern);
        }

        public bool TryPickPattern(Func<BossPattern, bool> filter, out BossPattern pattern)
        {
            if (selector != null)
                return selector.TryPick(filter, out pattern);

            pattern = null;
            return false;
        }

        public void ShowTelegraph(int laneMask)
        {
            if (telegraph != null)
                telegraph.ShowMask(laneMask);

            PlaySfx(TelegraphSfxId);
        }

        public void PlayAttackSfx()
        {
            PlaySfx(AttackSfxId);
        }

        private static void PlaySfx(string id)
        {
            if (AudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
                audio.PlayGlobal(id);
        }

        public void BrightenTelegraph()
        {
            if (telegraph != null)
                telegraph.Brighten();
        }

        public void HideTelegraph()
        {
            if (telegraph != null)
                telegraph.Hide();
        }

        public float GetLaneCenterY(int laneMask)
        {
            var sum = 0f;
            var count = 0;

            for (var lane = 0; lane < LaneCount; lane++)
            {
                if (!BossLanes.Contains(laneMask, lane))
                    continue;

                sum += GetLaneY(lane);
                count++;
            }

            return count > 0 ? sum / count : GetLaneY(MiddleLane);
        }

        public float GetLaneSpanHeight(int laneMask, float singleLaneHeight)
        {
            var count = BossLanes.Count(laneMask);

            if (count <= 1)
                return singleLaneHeight;

            return singleLaneHeight + LaneSpacing * (count - 1);
        }
    }
}
