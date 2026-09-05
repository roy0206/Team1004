using System.Collections.Generic;
using Game.Particles;
using UnityEngine;

namespace Game.Environment
{
    public sealed class UnderwaterFlowThing : MonoThing
    {
        [SerializeField] private EnvironmentThing environment;
        [SerializeField] private Transform streakRoot;
        [SerializeField] private Transform bubbleRoot;
        [SerializeField] private ParticleProfile streakProfile;
        [SerializeField] private ParticleProfile bubbleProfile;
        [SerializeField] private float holdSpeedScale = 0.35f;
        [SerializeField] private float ambientSpeedScale = 1f;

        private SpriteEmitterModule streaks;
        private SpriteEmitterModule bubbles;
        private UnderwaterFlowModule flow;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;
        public SpriteEmitterModule Streaks => streaks;
        public SpriteEmitterModule Bubbles => bubbles;
        public float FlowSpeed => flow != null ? flow.FlowSpeed : 0f;
        public float HoldSpeedScale => holdSpeedScale;

        public float AmbientSpeedScale
        {
            get => flow != null ? flow.AmbientSpeedScale : ambientSpeedScale;
            set
            {
                ambientSpeedScale = Mathf.Max(0f, value);

                if (flow != null)
                    flow.AmbientSpeedScale = ambientSpeedScale;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            if (environment == null)
                environment = GetComponentInParent<EnvironmentThing>();

            var buffer = new List<SpriteEmitterModule>();

            streaks = CreateEmitter(streakRoot, streakProfile, 0x51ED2701u);

            if (streaks != null)
                buffer.Add(streaks);

            bubbles = CreateEmitter(bubbleRoot, bubbleProfile, 0x2C9277B5u);

            if (bubbles != null)
                buffer.Add(bubbles);

            flow = AddModule(new UnderwaterFlowModule(environment, buffer.ToArray(), holdSpeedScale));
            flow.AmbientSpeedScale = ambientSpeedScale;
        }

        public void SetEmitting(bool emitting)
        {
            Initialize();

            if (streaks != null)
                streaks.IsEmitting = emitting;

            if (bubbles != null)
                bubbles.IsEmitting = emitting;
        }

        public void Clear()
        {
            Initialize();
            streaks?.Clear();
            bubbles?.Clear();
        }

        private SpriteEmitterModule CreateEmitter(Transform root, ParticleProfile profile, uint seed)
        {
            if (root == null || profile == null)
                return null;

            var renderers = new List<SpriteRenderer>();

            for (var i = 0; i < root.childCount; i++)
            {
                var renderer = root.GetChild(i).GetComponent<SpriteRenderer>();

                if (renderer != null)
                    renderers.Add(renderer);
            }

            if (renderers.Count == 0)
                return null;

            return AddModule(new SpriteEmitterModule(transform, profile, renderers.ToArray(), seed));
        }
    }
}
