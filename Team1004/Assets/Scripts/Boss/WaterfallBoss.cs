using DG.Tweening;
using Game.Player;
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public sealed class WaterfallBoss : PatternBoss<WaterfallBoss, WaterfallBossState, WaterfallBossData>
    {
        [SerializeField] private Transform waterfall;
        [SerializeField] private SpriteRenderer waterfallRenderer;
        [SerializeField] private Transform[] rapids;
        [SerializeField] private Hazard[] rapidHazards;
        [SerializeField] private Transform[] rocks;
        [SerializeField] private Hazard[] rockHazards;

        private Tween cueTween;
        private Color waterfallColor = Color.white;
        private bool waterfallColorCached;

        public Transform Waterfall => waterfall;
        public bool IsWaterfallVisible => waterfallRenderer != null && waterfallRenderer.enabled;

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

        protected override WaterfallBossState InitialKey => WaterfallBossState.Intro;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CacheWaterfallColor();
            HideJumpCue();
            HideWaterfall();
            ParkAll();
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
            HideJumpCue();
            ParkAll();
        }

        protected override void OnReset()
        {
            base.OnReset();
            HideJumpCue();
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

        public void SetLaneHazard(int lane, bool enabled)
        {
            if (rapidHazards != null && lane >= 0 && lane < rapidHazards.Length && rapidHazards[lane] != null)
            {
                rapidHazards[lane].enabled = enabled;
                HazardHitbox.SetVisible(rapidHazards[lane], enabled);
            }

            if (rockHazards != null && lane >= 0 && lane < rockHazards.Length && rockHazards[lane] != null)
            {
                rockHazards[lane].enabled = enabled;
                HazardHitbox.SetVisible(rockHazards[lane], enabled);
            }
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
            SetLaneHazard(lane, true);
        }

        public void ParkLane(int lane)
        {
            SetLaneHazard(lane, false);
            SetLaneVisible(lane, false);

            if (Data == null)
                return;

            var y = GetLaneY(lane);
            var rapid = GetRapid(lane);
            var rock = GetRock(lane);

            if (rapid != null)
                rapid.position = new Vector3(Data.SpawnX, y, rapid.position.z);

            if (rock != null)
                rock.position = new Vector3(Data.SpawnX + Data.RockTrail, y, rock.position.z);
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

            if (waterfall == null)
                return;

            var position = waterfall.position;
            var x = Data != null ? Data.FinalWaterfallEnterX : position.x;
            waterfall.position = new Vector3(x, 0f, position.z);
        }

        public void ShowWaterfall()
        {
            if (waterfallRenderer != null)
                waterfallRenderer.enabled = true;
        }

        public bool CanPlayerJumpWithin(float seconds)
        {
            if (Player == null)
                return true;

            return !Player.IsAirborne && Player.JumpCooldownRemaining <= seconds;
        }

        public void ShowJumpCue()
        {
            if (waterfallRenderer == null || cueTween != null)
                return;

            CacheWaterfallColor();
            var pulse = Data != null ? Data.JumpCuePulseDuration : 0.35f;

            if (pulse <= 0f)
                return;

            var renderer = waterfallRenderer;
            cueTween = DOTween.ToAlpha(() => renderer.color, value => renderer.color = value, waterfallColor.a * 0.4f, pulse)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void HideJumpCue()
        {
            if (cueTween != null && cueTween.IsActive())
                cueTween.Kill();

            cueTween = null;

            if (waterfallRenderer != null && waterfallColorCached)
                waterfallRenderer.color = waterfallColor;
        }

        private void CacheWaterfallColor()
        {
            if (waterfallColorCached || waterfallRenderer == null)
                return;

            waterfallColorCached = true;
            waterfallColor = waterfallRenderer.color;
        }
    }
}
