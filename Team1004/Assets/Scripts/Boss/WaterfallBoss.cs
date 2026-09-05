using Game.StateMachine;
using Game.Water;
using UnityEngine;

namespace Game.Boss
{
    public sealed class WaterfallBoss : PatternBoss<WaterfallBoss, WaterfallBossState, WaterfallBossData>
    {
        [SerializeField] private Transform waterfall;
        [SerializeField] private SpriteRenderer waterfallRenderer;
        [SerializeField] private Transform[] rapids;
        [SerializeField] private Transform[] rocks;

        public Transform Waterfall => waterfall;
        public bool IsWaterfallVisible => waterfallRenderer != null && waterfallRenderer.enabled;
        public bool StartsAtFinalWaterfall => EntryCheckpoint == BossCheckpoints.FinalWaterfall;
        public bool IsFinalApproach { get; private set; }
        public float FinalApproachElapsed { get; private set; }

        public float FinalApproachRemaining =>
            IsFinalApproach && Data != null ? Mathf.Max(0f, Data.FinalApproachDuration - FinalApproachElapsed) : -1f;

        public float FinalContactX =>
            Config.PlayerX + (Data != null ? Data.FinalContactHalfWidth : 0f);

        public int AttackLaneCount
        {
            get
            {
                var count = rapids != null ? rapids.Length : 0;

                if (rocks != null && rocks.Length < count)
                    count = rocks.Length;

                return count;
            }
        }

        public override string ActiveCheckpoint =>
            IsActive && HasState && CurrentKey == WaterfallBossState.Breakthrough
                ? BossCheckpoints.FinalWaterfall
                : null;

        protected override WaterfallBossState InitialKey =>
            StartsAtFinalWaterfall ? WaterfallBossState.Breakthrough : WaterfallBossState.Intro;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            HideWaterfall();
            ParkAll();
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            if (!StartsAtFinalWaterfall || Timer == null)
                return;

            Timer.Start();
            Timer.Tick(Timer.Duration);
        }

        protected override void BuildStates(StateMachineModule<WaterfallBoss, WaterfallBossState> fsm)
        {
            var machine = fsm.Machine;

            fsm.Add(WaterfallBossState.Intro,
                new TimedState<WaterfallBoss, WaterfallBossState>(machine, Data.IntroDuration, WaterfallBossState.Attack));
            fsm.Add(WaterfallBossState.Attack,
                new WaterfallAttackPhase(machine, Timer, Timing, WaterfallBossState.Breakthrough));
            fsm.Add(WaterfallBossState.Breakthrough, new WaterfallBreakthroughState());
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            base.OnComplete(outcome);
            ParkAll();
        }

        protected override void OnReset()
        {
            base.OnReset();
            HideWaterfall();
            ParkAll();
        }

        public Transform GetRapid(int lane)
        {
            return rapids != null && lane >= 0 && lane < rapids.Length ? rapids[lane] : null;
        }

        public Transform GetRock(int lane)
        {
            return rocks != null && lane >= 0 && lane < rocks.Length ? rocks[lane] : null;
        }

        public void SetLaneVisible(int lane, bool visible)
        {
            var rapid = GetRapid(lane);
            var rock = GetRock(lane);

            if (rapid != null && rapid.gameObject.activeSelf != visible)
                rapid.gameObject.SetActive(visible);

            if (rock != null && rock.gameObject.activeSelf != visible)
                rock.gameObject.SetActive(visible);
        }

        public void LaunchLane(int lane)
        {
            ParkLane(lane);
            SetLaneVisible(lane, true);
        }

        public void ParkLane(int lane)
        {
            SetLaneVisible(lane, false);

            if (Data == null)
                return;

            var y = GetLaneY(lane);
            var rapid = GetRapid(lane);
            var rock = GetRock(lane);

            if (rapid != null)
            {
                rapid.position = new Vector3(Data.SpawnX, y, rapid.position.z);
                WaterInteractor.NotifyTeleport(rapid);
            }

            if (rock != null)
            {
                rock.position = new Vector3(Data.SpawnX + Data.RockTrail, y, rock.position.z);
                WaterInteractor.NotifyTeleport(rock);
            }
        }

        public void ParkAll()
        {
            for (var lane = 0; lane < AttackLaneCount; lane++)
                ParkLane(lane);
        }

        public void HideWaterfall()
        {
            if (waterfallRenderer != null)
                waterfallRenderer.enabled = false;

            if (Data != null)
                PlaceWaterfall(Data.FinalWaterfallEnterX);

            WaterInteractor.NotifyTeleport(waterfall);
        }

        public void ShowWaterfall()
        {
            if (waterfallRenderer != null)
                waterfallRenderer.enabled = true;
        }

        public void PlaceWaterfall(float x)
        {
            if (waterfall == null)
                return;

            var position = waterfall.position;
            waterfall.position = new Vector3(x, 0f, position.z);
        }

        public bool CanPlayerJumpWithin(float seconds)
        {
            if (Player == null)
                return true;

            return !Player.IsAirborne && Player.JumpCooldownRemaining <= seconds;
        }

        public void BeginFinalApproach()
        {
            IsFinalApproach = true;
            FinalApproachElapsed = 0f;
        }

        public void AdvanceFinalApproach(float deltaTime)
        {
            if (IsFinalApproach)
                FinalApproachElapsed += deltaTime;
        }

        public void EndFinalApproach()
        {
            IsFinalApproach = false;
        }

        public bool ClearsFinalWaterfall()
        {
            if (Player == null)
                return true;

            if (!Player.IsAirborne)
                return false;

            var height = Data != null ? Data.FinalClearHeight : 0f;
            return Player.transform.position.y >= height;
        }

        public void ReportFinalWaterfallImpact()
        {
            RaiseImpact();
        }
    }
}
