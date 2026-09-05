using System;
using Game.Animation;
using Game.Config;
using Game.Water;
using UnityEngine;

namespace Game.Player
{
    public sealed class LanePlayer : MonoThing
    {
        private const string LaneUpAction = "Player/LaneUp";
        private const string LaneDownAction = "Player/LaneDown";
        private const int StartLane = 1;
        private const float HitEffectDuration = 0.25f;

        [SerializeField] private CustomAnimation swimClip;
        [SerializeField] private CustomAnimation laneUpClip;
        [SerializeField] private CustomAnimation laneDownClip;
        [SerializeField] private CustomAnimation jumpClip;

        private LaneMoveModule laneMove;
        private HurtboxModule hurtbox;
        private JumpModule jump;
        private HitReactionModule hitReaction;
        private SpriteAnimatorModule animator;
        private WaterInteractorModule water;
        private bool listening;
        private bool inputEnabled = true;

        public LaneMoveModule LaneMove => laneMove;
        public HurtboxModule Hurtbox => hurtbox;
        public JumpModule Jump => jump;
        public HitReactionModule HitReaction => hitReaction;
        public SpriteAnimatorModule Animator => animator;
        public WaterInteractorModule Water => water;
        public CustomAnimation SwimClip => swimClip;
        public CustomAnimation LaneUpClip => laneUpClip;
        public CustomAnimation LaneDownClip => laneDownClip;
        public CustomAnimation JumpClip => jumpClip;
        public int CurrentLane => laneMove != null ? laneMove.CurrentLane : StartLane;
        public bool IsMoving => laneMove != null && laneMove.IsMoving;
        public bool IsAirborne => jump != null && jump.IsAirborne;
        public float JumpCooldownRemaining => jump != null ? jump.CooldownRemaining : 0f;
        public float AirborneRemaining => jump != null ? jump.AirborneRemaining : 0f;
        public float MoveRemaining => laneMove != null ? laneMove.MoveRemaining : 0f;
        public bool CanJump => jump != null && jump.CanJump && !IsMoving && inputEnabled;
        public bool IsVulnerable => !IsMoving && !IsAirborne && InputEnabled;
        public bool HasHit => hurtbox != null && hurtbox.HasHit;
        public int TopLane => laneMove != null ? laneMove.TopLane : 0;
        public int BottomLane => laneMove != null ? laneMove.BottomLane : 0;

        public bool InputEnabled
        {
            get => inputEnabled;
            set
            {
                inputEnabled = value;

                if (laneMove != null)
                    laneMove.IsEnabled = value;

                if (jump != null)
                    jump.IsEnabled = value;

                if (animator != null)
                    animator.IsEnabled = value;
            }
        }

        public event Action JumpRequested;
        public event Action Jumped;
        public event Action Landed;
        public event Action<int> LaneChanged;
        public event Action<Hazard> Hit;

        private void Awake()
        {
            laneMove = AddModule(new LaneMoveModule(transform, StartLane));
            laneMove.LaneChanged += OnModuleLaneChanged;
            laneMove.MoveStarted += OnModuleMoveStarted;
            laneMove.MoveFinished += OnModuleMoveFinished;
            laneMove.IsEnabled = inputEnabled;

            jump = AddModule(new JumpModule(transform));
            jump.Jumped += OnModuleJumped;
            jump.Landed += OnModuleLanded;
            jump.IsEnabled = inputEnabled;

            hurtbox = AddModule(new HurtboxModule(this));
            hurtbox.Hit += OnModuleHit;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            hitReaction = AddModule(new HitReactionModule(transform, spriteRenderer));

            animator = AddModule(new SpriteAnimatorModule(spriteRenderer));
            animator.IsEnabled = inputEnabled;

            var settings = WaterSettings.currentSettings;
            var smoothing = settings != null ? settings.velocitySmoothing : 0f;
            var box = GetComponent<BoxCollider2D>();
            water = box != null
                ? AddModule(new WaterInteractorModule(transform, box, smoothing))
                : AddModule(new WaterInteractorModule(transform, Vector2.one, Vector2.zero, smoothing));
        }

        private void Start()
        {
            laneMove.SnapToLane(laneMove.CurrentLane);
            PlaySwim();
        }

        private void OnEnable()
        {
            if (!InputManager.TryGetInstance(out var input) || !input.IsInitialized)
            {
                Debug.LogWarning("[LanePlayer] InputManager is not initialized. Lane input is disabled.", this);
                return;
            }

            input.AddListener(LaneUpAction, InputPhase.Performed, OnLaneUp);
            input.AddListener(LaneDownAction, InputPhase.Performed, OnLaneDown);
            listening = true;
        }

        private void OnDisable()
        {
            if (!listening)
                return;

            listening = false;
            if (!InputManager.TryGetInstance(out var input))
                return;

            input.RemoveListener(LaneUpAction, InputPhase.Performed, OnLaneUp);
            input.RemoveListener(LaneDownAction, InputPhase.Performed, OnLaneDown);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hurtbox != null)
                hurtbox.AddOverlap(other.GetComponentInParent<Hazard>());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (hurtbox != null)
                hurtbox.RemoveOverlap(other.GetComponentInParent<Hazard>());
        }

        protected override void OnThingDestroy()
        {
            if (laneMove != null)
            {
                laneMove.LaneChanged -= OnModuleLaneChanged;
                laneMove.MoveStarted -= OnModuleMoveStarted;
                laneMove.MoveFinished -= OnModuleMoveFinished;
            }

            if (jump != null)
            {
                jump.Jumped -= OnModuleJumped;
                jump.Landed -= OnModuleLanded;
            }

            if (hurtbox != null)
                hurtbox.Hit -= OnModuleHit;
        }

        public void SnapToLane(int lane)
        {
            laneMove?.SnapToLane(lane);
            water?.ResetVelocity();
            PlaySwim();
        }

        public void PlaySwim()
        {
            if (animator == null || swimClip == null)
                return;

            animator.Play(swimClip);
        }

        public void ResetHit()
        {
            hurtbox?.ResetHit();
        }

        public void ResetJumpCooldown()
        {
            jump?.ResetCooldown();
        }

        public Awaitable PlayHitReactionAsync(float stopDuration, float pushDistance)
        {
            if (hitReaction == null)
            {
                var source = new AwaitableCompletionSource();
                source.SetResult();
                return source.Awaitable;
            }

            return hitReaction.PlayAsync(stopDuration, HitEffectDuration, pushDistance);
        }

        private void OnLaneUp()
        {
            if (!CanAcceptInput())
                return;

            if (laneMove.IsAtTop)
            {
                JumpRequested?.Invoke();
                jump.TryJump();
                return;
            }

            laneMove.TryMoveUp();
        }

        private void OnLaneDown()
        {
            if (!CanAcceptInput())
                return;

            laneMove.TryMoveDown();
        }

        private bool CanAcceptInput()
        {
            return laneMove != null && jump != null && inputEnabled &&
                   !laneMove.IsMoving && !jump.IsAirborne && isActiveAndEnabled;
        }

        private void OnModuleLaneChanged(int lane)
        {
            LaneChanged?.Invoke(lane);
        }

        private void OnModuleMoveStarted(int direction)
        {
            if (animator == null || (jump != null && jump.IsAirborne))
                return;

            var clip = direction < 0 ? laneUpClip : laneDownClip;

            if (clip == null)
                return;

            animator.Play(clip, GameConfig.Current.LaneMoveDuration);
        }

        private void OnModuleMoveFinished(int lane)
        {
            if (jump != null && jump.IsAirborne)
                return;

            PlaySwim();
        }

        private void OnModuleJumped()
        {
            if (animator != null && jumpClip != null)
                animator.Play(jumpClip, GameConfig.Current.JumpDuration);

            Jumped?.Invoke();
        }

        private void OnModuleLanded()
        {
            PlaySwim();
            Landed?.Invoke();
        }

        private void OnModuleHit(Hazard hazard)
        {
            Hit?.Invoke(hazard);
        }
    }
}
