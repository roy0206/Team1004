using System;
using Game.Config;
using UnityEngine;

namespace Game.Player
{
    public sealed class JumpModule : Module
    {
        private readonly Transform target;

        private float elapsed;
        private float baseY;

        public bool IsAirborne { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool CanJump => !IsAirborne && CooldownRemaining <= 0f;
        public float AirborneRemaining => IsAirborne ? Mathf.Max(0f, GameConfig.Current.JumpDuration - elapsed) : 0f;
        public float Progress01
        {
            get
            {
                if (!IsAirborne)
                    return 0f;

                var duration = GameConfig.Current.JumpDuration;
                return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            }
        }

        public float Cooldown01
        {
            get
            {
                var cooldown = GameConfig.Current.JumpCooldown;
                if (cooldown <= 0f || CooldownRemaining <= 0f)
                    return 1f;

                return Mathf.Clamp01(1f - CooldownRemaining / cooldown);
            }
        }

        public event Action Jumped;
        public event Action Landed;

        public JumpModule(Transform target)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (IsAirborne)
            {
                var config = GameConfig.Current;
                elapsed += Time.deltaTime;
                var t = config.JumpDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / config.JumpDuration);
                SetY(baseY + 4f * config.JumpHeight * t * (1f - t));

                if (t >= 1f)
                    Land();

                return;
            }

            if (CooldownRemaining > 0f)
                CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
        }

        public void ResetCooldown()
        {
            CooldownRemaining = 0f;
        }

        public void CancelJump()
        {
            if (!IsAirborne)
                return;

            IsAirborne = false;
            elapsed = 0f;
            SetY(baseY);
        }

        public bool TryJump()
        {
            if (!CanJump)
                return false;

            baseY = GameConfig.Current.GetLaneY(0);
            elapsed = 0f;
            IsAirborne = true;
            Jumped?.Invoke();
            return true;
        }

        private void Land()
        {
            IsAirborne = false;
            SetY(baseY);
            CooldownRemaining = Mathf.Max(0f, GameConfig.Current.JumpCooldown);
            Landed?.Invoke();
        }

        private void SetY(float y)
        {
            if (target == null)
                return;

            var position = target.position;
            position.y = y;
            target.position = position;
        }
    }
}
