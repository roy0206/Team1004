using System;
using System.Collections.Generic;
using Game.Boss;
using Game.Boss.Integration;
using Game.Ledge;
using Game.Play;
using Game.Player;
using Game.View;
using UnityEngine;

namespace Game.CameraFx
{
    public sealed class CameraCueBinder : MonoThing
    {
        private readonly struct BossHook
        {
            public BossThing Boss { get; }
            public Action Telegraph { get; }
            public Action Attack { get; }
            public Action Began { get; }
            public Action<BossOutcome> Finished { get; }
            public Action Impact { get; }

            public BossHook(
                BossThing boss,
                Action telegraph,
                Action attack,
                Action began,
                Action<BossOutcome> finished,
                Action impact)
            {
                Boss = boss;
                Telegraph = telegraph;
                Attack = attack;
                Began = began;
                Finished = finished;
                Impact = impact;
            }
        }

        [SerializeField] private CameraFxDirector director;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private CameraCue laneChangeCue;
        [SerializeField] private CameraCue jumpStartCue;
        [SerializeField] private CameraCue landingCue;
        [SerializeField] private CameraCue hitCue;
        [SerializeField] private CameraCue sectionStartCue;
        [SerializeField] private CameraCue bossBannerCue;
        [SerializeField] private CameraCue bossStartCue;
        [SerializeField] private CameraCue bossTelegraphCue;
        [SerializeField] private CameraCue bossClearCue;
        [SerializeField] private CameraCue finalWaterfallCue;
        [SerializeField] private CameraCue[] bossAttackCues;
        [SerializeField] private CameraCue ledgeApproachCue;
        [SerializeField] private CameraCue ledgeQteCue;
        [SerializeField] private CameraCue ledgeRiseCue;
        [SerializeField] private CameraCue ledgeFinishedCue;

        private readonly List<BossHook> bossHooks = new();

        private PlayFlow flow;
        private LanePlayer player;
        private ILedgeHandler ledge;
        private WaterfallBoss waterfall;
        private Action finalApproachHandler;
        private int lastLane = -1;
        private bool bound;

        public CameraFxDirector Director
        {
            get
            {
                if (director == null && CameraFxDirector.TryGetCurrent(out var current))
                    director = current;

                return director;
            }
        }

        private void Start()
        {
            Bind();
        }

        protected override void OnThingDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (bound)
                return;

            bound = true;

            if (!PlayFlow.TryGetCurrent(out flow))
            {
                Debug.LogWarning("[CameraFx] PlayFlow is not in the scene. Camera cues are disabled.", this);
                return;
            }

            flow.StateChanged += OnStateChanged;
            flow.SectionStarted += OnSectionStarted;

            player = flow.Player;

            if (player != null)
            {
                lastLane = player.CurrentLane;
                player.LaneChanged += OnLaneChanged;
                player.Jumped += OnJumped;
                player.Landed += OnLanded;
                player.Hit += OnHit;
            }

            ledge = flow.LedgeHandler ?? flow.Ledge;

            if (ledge != null)
            {
                ledge.Approaching += OnLedgeApproaching;
                ledge.QteStarted += OnLedgeQteStarted;
                ledge.Cleared += OnLedgeCleared;
                ledge.Finished += OnLedgeFinished;
            }

            BindBosses();
        }

        private void BindBosses()
        {
            if (bossDirector == null)
                bossDirector = FindAnyObjectByType<BossDirector>(FindObjectsInactive.Include);

            if (bossDirector == null)
                return;

            for (var index = 0; index < bossDirector.BossCount; index++)
            {
                var boss = bossDirector.GetBoss(index + 1);

                if (boss == null)
                    continue;

                var attackCue = AttackCue(index);
                Action telegraph = () => Play(bossTelegraphCue);
                Action attack = () => Play(attackCue);
                Action began = () => Play(bossStartCue);
                Action<BossOutcome> finished = outcome => OnBossFinished(outcome);
                Action impact = () => Play(hitCue);

                boss.TelegraphImminent += telegraph;
                boss.AttackBegan += attack;
                boss.Began += began;
                boss.Finished += finished;
                boss.Impact += impact;
                bossHooks.Add(new BossHook(boss, telegraph, attack, began, finished, impact));

                if (waterfall != null || boss is not WaterfallBoss waterfallBoss)
                    continue;

                waterfall = waterfallBoss;
                finalApproachHandler = OnFinalApproach;
                waterfall.FinalApproachBegan += finalApproachHandler;
            }
        }

        private void Unbind()
        {
            if (!bound)
                return;

            bound = false;

            if (flow != null)
            {
                flow.StateChanged -= OnStateChanged;
                flow.SectionStarted -= OnSectionStarted;
            }

            if (player != null)
            {
                player.LaneChanged -= OnLaneChanged;
                player.Jumped -= OnJumped;
                player.Landed -= OnLanded;
                player.Hit -= OnHit;
            }

            if (ledge != null)
            {
                ledge.Approaching -= OnLedgeApproaching;
                ledge.QteStarted -= OnLedgeQteStarted;
                ledge.Cleared -= OnLedgeCleared;
                ledge.Finished -= OnLedgeFinished;
            }

            for (var i = 0; i < bossHooks.Count; i++)
            {
                var hook = bossHooks[i];

                if (hook.Boss == null)
                    continue;

                hook.Boss.TelegraphImminent -= hook.Telegraph;
                hook.Boss.AttackBegan -= hook.Attack;
                hook.Boss.Began -= hook.Began;
                hook.Boss.Finished -= hook.Finished;
                hook.Boss.Impact -= hook.Impact;
            }

            bossHooks.Clear();

            if (waterfall != null && finalApproachHandler != null)
                waterfall.FinalApproachBegan -= finalApproachHandler;

            waterfall = null;
            finalApproachHandler = null;
            flow = null;
            player = null;
            ledge = null;
        }

        private CameraCue AttackCue(int index)
        {
            if (bossAttackCues == null || index < 0 || index >= bossAttackCues.Length)
                return null;

            return bossAttackCues[index];
        }

        private void Play(CameraCue cue)
        {
            if (cue == null)
                return;

            Director?.PlayCue(cue);
        }

        private void OnLaneChanged(int lane)
        {
            var previous = lastLane;
            lastLane = lane;

            if (laneChangeCue == null || previous < 0 || previous == lane)
                return;

            var up = lane < previous;
            Director?.PlayCue(laneChangeCue, up ? Vector2.up : Vector2.down);
        }

        private void OnJumped()
        {
            Play(jumpStartCue);
        }

        private void OnLanded()
        {
            Play(landingCue);
        }

        private void OnHit(Hazard hazard)
        {
            Play(hitCue);
        }

        private void OnSectionStarted(int section)
        {
            lastLane = player != null ? player.CurrentLane : lastLane;
            Play(sectionStartCue);
        }

        private void OnStateChanged(PlayState state)
        {
            switch (state)
            {
                case PlayState.Boss:
                    Play(bossBannerCue);
                    break;
                case PlayState.Ready:
                case PlayState.Cutscene:
                case PlayState.Cleared:
                case PlayState.Failed:
                    Director?.ResetEffects();
                    break;
            }
        }

        private void OnBossFinished(BossOutcome outcome)
        {
            if (outcome == BossOutcome.Passed)
                Play(bossClearCue);
            else
                Director?.ResetEffects();
        }

        private void OnFinalApproach()
        {
            Play(finalWaterfallCue);
        }

        private void OnLedgeApproaching(int section)
        {
            Play(ledgeApproachCue);
        }

        private void OnLedgeQteStarted(int section)
        {
            Play(ledgeQteCue);
        }

        private void OnLedgeCleared(int section)
        {
            Play(ledgeRiseCue);
        }

        private void OnLedgeFinished(int section)
        {
            Play(ledgeFinishedCue);
        }
    }
}
