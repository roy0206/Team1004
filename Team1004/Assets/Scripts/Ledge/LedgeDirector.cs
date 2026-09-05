using System;
using System.Collections.Generic;
using Game.Config;
using Game.Environment;
using Game.Player;
using UnityEngine;

namespace Game.Ledge
{
    [DefaultExecutionOrder(-50)]
    public sealed class LedgeDirector : MonoThing, ILedgeHandler
    {
        private readonly struct Scheduled
        {
            public LedgeThing Thing { get; }
            public LedgePlan Plan { get; }
            public bool ShowHint { get; }

            public Scheduled(LedgeThing thing, in LedgePlan plan, bool showHint)
            {
                Thing = thing;
                Plan = plan;
                ShowHint = showHint;
            }
        }

        [SerializeField] private LedgeData data;
        [SerializeField] private LedgeThing[] ledges;
        [SerializeField] private EnvironmentThing environment;
        [SerializeField] private LanePlayer player;

        private readonly List<Scheduled> queue = new();
        private LedgeThing active;
        private int queueIndex;
        private float sectionElapsed;
        private bool holdingWorld;
        private bool spawnSuspended;
        private float appliedEnvironmentOffset;

        public LedgeData Data => data;
        public EnvironmentThing Environment => environment;
        public ILedgeWorld World { get; set; }
        public int Section { get; private set; }
        public int PendingCount => Mathf.Max(0, queue.Count - queueIndex);
        public float SectionElapsed => sectionElapsed;
        public bool IsActive => active != null && active.IsActive;
        public bool IsHoldingWorld => holdingWorld;
        public bool IsSpawnSuspended => spawnSuspended;
        public LedgePhase Phase => active != null ? active.Phase : LedgePhase.Idle;

        public event Action<int> Approaching;
        public event Action<int> Blocked;
        public event Action<int> Resumed;
        public event Action<int> Cleared;
        public event Action<int> Finished;
        public event Action<int> Failed;

        public LanePlayer Player
        {
            get => player;
            set => player = value;
        }

        private void Start()
        {
            if (player == null)
                player = FindAnyObjectByType<LanePlayer>();

            if (environment == null)
                environment = FindAnyObjectByType<EnvironmentThing>();

            if (data == null)
                Debug.LogWarning("[LedgeDirector] LedgeData is not assigned. Ledges are disabled.", this);

            ParkAll();
        }

        protected override void OnThingDestroy()
        {
            ReleaseWorld();
            active = null;
            queue.Clear();
        }

        public void BeginSection(int section, float sectionDuration)
        {
            Stop();
            Section = section;

            if (data == null)
                return;

            var config = GameConfig.Current;
            var hintIndex = data.HintEntryIndex;

            for (var i = 0; i < data.EntryCount; i++)
            {
                var entry = data.GetEntry(i);

                if (entry == null || !entry.IsValid || entry.Section != section)
                    continue;

                var thing = GetLedge(i);

                if (thing == null)
                {
                    Debug.LogWarning(
                        $"[LedgeDirector] Ledge entry {i} (section {section}) has no ledge object. It is skipped.", this);
                    continue;
                }

                var plan = LedgePlan.Create(
                    entry.Time,
                    data.ApproachSafeTime,
                    config.ScrollSpeed,
                    config.PlayerX,
                    data.SpawnX);

                if (!plan.IsValid)
                {
                    Debug.LogWarning(
                        $"[LedgeDirector] Section {section} has an invalid ledge schedule at entry {i}.", this);
                    continue;
                }

                if (sectionDuration > 0f && plan.LedgeTime > sectionDuration - data.SectionEndMargin)
                {
                    Debug.LogWarning(
                        $"[LedgeDirector] Section {section} ledge time {plan.LedgeTime:0.##}s does not fit in {sectionDuration:0.##}s. The ledge is skipped.",
                        this);
                    continue;
                }

                queue.Add(new Scheduled(thing, plan, i == hintIndex));
            }

            queue.Sort(CompareByLedgeTime);
        }

        public void Tick(float deltaTime)
        {
            if (active == null && queueIndex >= queue.Count)
                return;

            var step = holdingWorld ? 0f : Mathf.Max(0f, deltaTime);
            sectionElapsed += step;

            var started = TryActivateNext();

            if (active == null)
            {
                ApplyWorldState();
                return;
            }

            var airborne = player != null && player.IsAirborne;
            var signal = active.Advance(started ? 0f : step, airborne);
            sectionElapsed = active.Time;

            ApplyEnvironmentOffset();
            ApplyWorldState();
            Raise(signal);

            if (active == null)
                return;

            if (active.Phase == LedgePhase.Done || active.Phase == LedgePhase.Failed)
            {
                active.Retire();
                active = null;
                queueIndex++;
                ReleaseWorld();
            }
        }

        public void Stop()
        {
            active = null;
            Section = 0;
            queue.Clear();
            queueIndex = 0;
            sectionElapsed = 0f;
            ParkAll();
            ReleaseWorld();
        }

        private bool TryActivateNext()
        {
            if (active != null || queueIndex >= queue.Count || data == null)
                return false;

            var next = queue[queueIndex];

            if (sectionElapsed < next.Plan.StartTime)
                return false;

            if (sectionElapsed > next.Plan.ApproachTime)
            {
                Debug.LogWarning(
                    $"[LedgeDirector] Section {Section} ledge at {next.Plan.LedgeTime:0.##}s is skipped because the previous ledge was still running at {sectionElapsed:0.##}s.",
                    this);
                queueIndex++;
                return false;
            }

            if (!next.Thing.Schedule(next.Plan, data, next.ShowHint, sectionElapsed))
            {
                queueIndex++;
                return false;
            }

            active = next.Thing;
            return true;
        }

        private static int CompareByLedgeTime(Scheduled left, Scheduled right)
        {
            return left.Plan.LedgeTime.CompareTo(right.Plan.LedgeTime);
        }

        private LedgeThing GetLedge(int index)
        {
            if (ledges == null || index < 0 || index >= ledges.Length)
                return null;

            return ledges[index];
        }

        private void ParkAll()
        {
            if (ledges == null)
                return;

            for (var i = 0; i < ledges.Length; i++)
            {
                if (ledges[i] != null)
                    ledges[i].Retire();
            }
        }

        private void ApplyEnvironmentOffset()
        {
            if (environment == null || data == null || active == null)
                return;

            var amount = data.EnvironmentRiseAmount;

            if (amount <= 0f)
                return;

            var rise = LedgeGateModule.Ease(active.RiseProgress01);
            var settle = active.Phase == LedgePhase.Retiring || active.Phase == LedgePhase.Done
                ? LedgeGateModule.Ease(active.SettleProgress01)
                : 0f;

            var offset = -amount * rise * (1f - settle);

            if (Mathf.Approximately(offset, appliedEnvironmentOffset))
                return;

            appliedEnvironmentOffset = offset;
            environment.SetVerticalOffset(offset);
        }

        private void ApplyWorldState()
        {
            var hold = active != null && active.IsHoldingWorld;
            var suspend = active != null && active.IsSpawnSuspended;

            if (hold != holdingWorld)
            {
                holdingWorld = hold;
                World?.SetWorldScrolling(!hold);
            }

            if (suspend != spawnSuspended)
            {
                spawnSuspended = suspend;
                World?.SetObstacleSpawning(!suspend);
            }
        }

        private void ReleaseWorld()
        {
            if (holdingWorld)
            {
                holdingWorld = false;
                World?.SetWorldScrolling(true);
            }

            if (spawnSuspended)
            {
                spawnSuspended = false;
                World?.SetObstacleSpawning(true);
            }

            if (environment != null && !Mathf.Approximately(appliedEnvironmentOffset, 0f))
                environment.SetVerticalOffset(0f);

            appliedEnvironmentOffset = 0f;
        }

        private void Raise(LedgeSignal signal)
        {
            if (signal == LedgeSignal.None)
                return;

            if ((signal & LedgeSignal.Approaching) != 0)
                Invoke(Approaching);

            if ((signal & LedgeSignal.Blocked) != 0)
                Invoke(Blocked);

            if ((signal & LedgeSignal.Resumed) != 0)
                Invoke(Resumed);

            if ((signal & LedgeSignal.Cleared) != 0)
                Invoke(Cleared);

            if ((signal & LedgeSignal.Finished) != 0)
                Invoke(Finished);

            if ((signal & LedgeSignal.Failed) != 0)
                Invoke(Failed);
        }

        private void Invoke(Action<int> handler)
        {
            if (handler == null)
                return;

            try
            {
                handler(Section);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
