using System;
using Game.Config;
using UnityEngine;

namespace Game.Player
{
    public sealed class LaneMoveModule : Module
    {
        private readonly Transform target;

        private float elapsed;
        private float fromY;
        private float toY;

        public int CurrentLane { get; private set; }
        public bool IsMoving { get; private set; }
        public int MoveDirection { get; private set; }
        public int TopLane => 0;
        public int BottomLane => Mathf.Max(0, GameConfig.Current.LaneCount - 1);
        public bool IsAtTop => CurrentLane <= TopLane;
        public bool IsAtBottom => CurrentLane >= BottomLane;
        public float MoveRemaining => IsMoving ? Mathf.Max(0f, GameConfig.Current.LaneMoveDuration - elapsed) : 0f;

        public event Action<int> LaneChanged;
        public event Action<int> MoveStarted;
        public event Action<int> MoveFinished;

        public LaneMoveModule(Transform target, int startLane)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            CurrentLane = Mathf.Clamp(startLane, 0, Mathf.Max(0, GameConfig.Current.LaneCount - 1));
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (!IsMoving || target == null)
                return;

            var duration = GameConfig.Current.LaneMoveDuration;
            elapsed += Time.deltaTime;
            var t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            SetY(Mathf.Lerp(fromY, toY, Mathf.SmoothStep(0f, 1f, t)));

            if (t < 1f)
                return;

            IsMoving = false;
            MoveDirection = 0;
            MoveFinished?.Invoke(CurrentLane);
        }

        public bool TryMoveUp()
        {
            if (IsMoving || IsAtTop)
                return false;

            BeginMove(CurrentLane - 1);
            return true;
        }

        public bool TryMoveDown()
        {
            if (IsMoving || IsAtBottom)
                return false;

            BeginMove(CurrentLane + 1);
            return true;
        }

        public void SnapToLane(int lane)
        {
            var config = GameConfig.Current;
            CurrentLane = Mathf.Clamp(lane, 0, Mathf.Max(0, config.LaneCount - 1));
            IsMoving = false;
            MoveDirection = 0;

            if (target == null)
                return;

            var position = target.position;
            position.x = config.PlayerX;
            position.y = config.GetLaneY(CurrentLane);
            target.position = position;
        }

        private void BeginMove(int lane)
        {
            fromY = target.position.y;
            toY = GameConfig.Current.GetLaneY(lane);
            elapsed = 0f;
            IsMoving = true;
            MoveDirection = lane > CurrentLane ? 1 : -1;
            CurrentLane = lane;
            MoveStarted?.Invoke(MoveDirection);
            LaneChanged?.Invoke(lane);
        }

        private void SetY(float y)
        {
            var position = target.position;
            position.y = y;
            target.position = position;
        }
    }
}
