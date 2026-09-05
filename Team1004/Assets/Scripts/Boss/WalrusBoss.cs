using Game.Player;
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public sealed class WalrusBoss : PatternBoss<WalrusBoss, WalrusBossState, WalrusBossData>
    {
        [SerializeField] private Transform body;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Hazard bodyHazard;
        [SerializeField] private Color mouthColor = new(0.95f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color armsColor = new(0.85f, 0.25f, 0.2f, 1f);

        private Color restColor = Color.white;
        private bool restColorCached;

        public Transform Body => body;
        public bool IsBodyHazardEnabled => bodyHazard != null && bodyHazard.enabled;

        protected override WalrusBossState InitialKey => WalrusBossState.Intro;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CacheRestColor();
            ResetStance();
            ResetPosition();
        }

        protected override void BuildStates(StateMachineModule<WalrusBoss, WalrusBossState> fsm)
        {
            var machine = fsm.Machine;

            fsm.Add(WalrusBossState.Intro, new WalrusIntroState(machine, Data.IntroDuration));
            fsm.Add(WalrusBossState.Attack, new WalrusAttackPhase(machine, Timer, Timing, WalrusBossState.Outro));
            fsm.Add(WalrusBossState.Outro, new WalrusOutroState());
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            base.OnComplete(outcome);
            ResetStance();
        }

        protected override void OnReset()
        {
            base.OnReset();
            ResetStance();
            ResetPosition();
        }

        public void SetStance(BossPattern pattern)
        {
            if (Data == null || pattern == null)
                return;

            CacheRestColor();
            var wide = pattern.LaneCount > 1;
            var height = wide ? Data.DoubleBodyHeight : Data.SingleBodyHeight;
            SetBodySize(Data.BodyWidth, height);

            if (bodyRenderer != null)
                bodyRenderer.color = wide ? armsColor : mouthColor;
        }

        public void ResetStance()
        {
            SetBodyHazard(false);
            SetBodyHitboxVisible(false);

            if (Data != null)
                SetBodySize(Data.BodyWidth, Data.SingleBodyHeight);

            if (bodyRenderer != null && restColorCached)
                bodyRenderer.color = restColor;
        }

        public void ResetPosition()
        {
            var position = transform.position;
            var x = Data != null ? Data.RestX : position.x;
            transform.position = new Vector3(x, GetLaneY(MiddleLane), position.z);
        }

        public void PlaceAtX(float x)
        {
            var position = transform.position;
            transform.position = new Vector3(x, position.y, position.z);
        }

        public void SetBodyHazard(bool enabled)
        {
            if (bodyHazard != null)
                bodyHazard.enabled = enabled;
        }

        public void SetBodyHitboxVisible(bool visible)
        {
            HazardHitbox.SetVisible(bodyHazard, visible);
        }

        private void SetBodySize(float width, float height)
        {
            if (body == null)
                return;

            var spriteSize = bodyRenderer != null && bodyRenderer.sprite != null
                ? bodyRenderer.sprite.bounds.size
                : Vector3.one;
            var scaleX = spriteSize.x > 0f ? width / spriteSize.x : 1f;
            var scaleY = spriteSize.y > 0f ? height / spriteSize.y : 1f;
            body.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private void CacheRestColor()
        {
            if (restColorCached || bodyRenderer == null)
                return;

            restColorCached = true;
            restColor = bodyRenderer.color;
        }
    }
}
