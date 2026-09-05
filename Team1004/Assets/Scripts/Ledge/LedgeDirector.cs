using System;
using System.Collections.Generic;
using Game.Config;
using Game.Environment;
using Game.Player;
using Game.Qte;
using UnityEngine;

namespace Game.Ledge
{
    [DefaultExecutionOrder(-50)]
    public sealed class LedgeDirector : MonoThing, ILedgeHandler
    {
        private const int FallbackRequiredPresses = 6;

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
        [SerializeField] private QtePanel qtePanel;

        private readonly List<Scheduled> queue = new();
        private QteModule qte;
        private float spawnGraceRemaining;
        private LedgeThing active;
        private int queueIndex;
        private float sectionElapsed;
        private bool spawnSuspended;
        private bool qteRunning;
        private bool missingQteDataReported;
        private float worldSpeedScale = 1f;
        private float appliedWorldSpeedScale = 1f;
        private float appliedEnvironmentOffset;

        public LedgeData Data => data;
        public EnvironmentThing Environment => environment;
        public ILedgeWorld World { get; set; }
        public int Section { get; private set; }
        public int PendingCount => Mathf.Max(0, queue.Count - queueIndex);
        public float SectionElapsed => sectionElapsed;
        public bool IsActive => active != null && active.IsActive;
        public bool IsHoldingWorld => false;
        public bool IsSpawnSuspended => spawnSuspended;
        public bool IsQteActive => active != null && active.IsQteActive;
        public float WorldSpeedScale => worldSpeedScale;
        public LedgePhase Phase => active != null ? active.Phase : LedgePhase.Idle;
        public QteSequence QteSequence => qte?.Sequence;
        public QtePanel Panel => qtePanel;
        public int QteProgress => qte != null ? qte.Progress : 0;
        public int QteRequiredPresses => qte != null && qteRunning ? qte.RequiredPresses : 0;
        public bool QteExpectsUp => qte != null && qte.Expected == QteKey.Up;
        public bool QteExpectsDown => qte != null && qte.Expected == QteKey.Down;

        public event Action<int> Approaching;
        public event Action<int> QteStarted;
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

            EnsureQte();
            ResolvePanel();
            ParkAll();
        }

        protected override void OnThingDestroy()
        {
            StopQte();
            ReleaseWorld();
            active = null;
            queue.Clear();
        }

        public void BeginSection(int section, float sectionDuration)
        {
            Stop();
            EnsureQte();
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
            EnsureQte();

            if (active == null && queueIndex >= queue.Count && spawnGraceRemaining <= 0f)
            {
                worldSpeedScale = 1f;
                return;
            }

            worldSpeedScale = ResolveWorldSpeedScale();

            var step = Mathf.Max(0f, deltaTime) * worldSpeedScale;
            sectionElapsed += step;

            if (spawnGraceRemaining > 0f)
                spawnGraceRemaining = Mathf.Max(0f, spawnGraceRemaining - step);

            var started = TryActivateNext();

            if (active == null)
            {
                SyncQte();
                ApplyWorldState();
                return;
            }

            var airborne = player != null && player.IsAirborne;
            var signal = active.Advance(started ? 0f : step, airborne);
            sectionElapsed = active.Time;

            ApplyEnvironmentOffset();
            SyncQte();
            ApplyWorldState();
            Raise(signal);

            if (active == null)
                return;

            if (active.Phase == LedgePhase.Done || active.Phase == LedgePhase.Failed)
            {
                if (active.Phase == LedgePhase.Done && data != null)
                    spawnGraceRemaining = Mathf.Max(0f, data.PostClearSafeTime);

                active.Retire();
                active = null;
                queueIndex++;
                StopQte();
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
            spawnGraceRemaining = 0f;
            StopQte();
            ParkAll();
            ReleaseWorld();
        }

        private void EnsureQte()
        {
            if (qte != null)
                return;

            qte = AddModule(new QteModule());
            qte.AutoTick = false;
            qte.Accepted += OnQteAccepted;
            qte.ExpectedChanged += OnQteExpectedChanged;
            qte.Completed += OnQteCompleted;
        }

        private void ResolvePanel()
        {
            if (qtePanel != null)
                return;

            if (QtePanel.TryGetCurrent(out var found))
                qtePanel = found;
        }

        private float ResolveWorldSpeedScale()
        {
            if (data == null || active == null || active.Phase != LedgePhase.Qte)
                return 1f;

            return data.QteWorldSpeedScale;
        }

        private void SyncQte()
        {
            var wanted = active != null && active.Phase == LedgePhase.Qte;

            if (wanted && !qteRunning)
                StartQte();
            else if (!wanted && qteRunning)
                StopQte();

            if (!qteRunning || active == null)
                return;

            qte.SetRemaining(RealSecondsToImpact());

            if (qtePanel != null)
                qtePanel.SetRemaining01(qte.Sequence.Remaining01);
        }

        private float RealSecondsToImpact()
        {
            if (active == null)
                return 0f;

            var scale = data != null ? data.QteWorldSpeedScale : 1f;
            return scale <= 0f ? 0f : active.ImpactRemaining / scale;
        }

        private void StartQte()
        {
            if (active == null || qte == null)
                return;

            var qteBudget = RealSecondsToImpact();
            var qteData = data != null ? data.Qte : null;

            if (qteData == null && !missingQteDataReported)
            {
                missingQteDataReported = true;
                Debug.LogWarning(
                    "[LedgeDirector] LedgeData has no QteData. The ledge quick time event falls back to 6 alternating presses.",
                    this);
            }

            var begun = qteData != null
                ? qte.Begin(qteData, qteBudget)
                : qte.Begin(FallbackRequiredPresses, true, qteBudget);

            if (!begun)
                return;

            qteRunning = true;

            if (player != null)
                player.QteCaptureInput = true;

            ResolvePanel();

            if (qtePanel == null)
                return;

            if (qteData != null)
                qtePanel.FlashDuration = qteData.PressFlashDuration;

            qtePanel.Show(qte.RequiredPresses, qte.Expected);
        }

        private void StopQte()
        {
            qteRunning = false;
            qte?.Stop();

            if (player != null)
                player.QteCaptureInput = false;

            if (qtePanel != null)
                qtePanel.Hide();
        }

        private void OnQteAccepted(int progress)
        {
            if (qtePanel == null)
                return;

            qtePanel.SetProgress(progress);
            qtePanel.Punch();
        }

        private void OnQteExpectedChanged(QteKey key)
        {
            if (qtePanel == null || key == QteKey.None)
                return;

            qtePanel.SetExpected(key);
        }

        private void OnQteCompleted()
        {
            if (player == null)
                return;

            player.QteCaptureInput = false;
            player.ForceJump();
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
            var suspend = (active != null && active.IsSpawnSuspended) || spawnGraceRemaining > 0f || IsNextLedgeNear();

            if (!Mathf.Approximately(worldSpeedScale, appliedWorldSpeedScale))
            {
                appliedWorldSpeedScale = worldSpeedScale;
                World?.SetWorldSpeedScale(worldSpeedScale);
                World?.SetWorldScrolling(worldSpeedScale > 0f);
            }

            if (suspend != spawnSuspended)
            {
                spawnSuspended = suspend;
                World?.SetObstacleSpawning(!suspend);
            }
        }

        private void ReleaseWorld()
        {
            worldSpeedScale = 1f;

            if (!Mathf.Approximately(appliedWorldSpeedScale, 1f))
            {
                appliedWorldSpeedScale = 1f;
                World?.SetWorldSpeedScale(1f);
                World?.SetWorldScrolling(true);
            }

            if (spawnSuspended && spawnGraceRemaining <= 0f && !IsNextLedgeNear())
            {
                spawnSuspended = false;
                World?.SetObstacleSpawning(true);
            }

            if (environment != null && !Mathf.Approximately(appliedEnvironmentOffset, 0f))
                environment.SetVerticalOffset(0f);

            appliedEnvironmentOffset = 0f;
        }

        private bool IsNextLedgeNear()
        {
            if (data == null || active != null || queueIndex >= queue.Count)
                return false;

            var lead = Mathf.Max(0f, data.SpawnSafeLead);
            return queue[queueIndex].Plan.LedgeTime - sectionElapsed <= lead;
        }

        private void Raise(LedgeSignal signal)
        {
            if (signal == LedgeSignal.None)
                return;

            if ((signal & LedgeSignal.Approaching) != 0)
                Invoke(Approaching);

            if ((signal & LedgeSignal.QteStarted) != 0)
                Invoke(QteStarted);

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
