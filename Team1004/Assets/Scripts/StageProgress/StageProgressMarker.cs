using UnityEngine;
using UnityEngine.UI;

namespace Game.StageProgress
{
    public enum StageMarkerState
    {
        Upcoming,
        Engaged,
        Cleared
    }

    [DisallowMultipleComponent]
    public sealed class StageProgressMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private Image ring;
        [SerializeField] private Image icon;

        [SerializeField] private Color upcomingRing = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color upcomingIcon = new Color(0.13f, 0.17f, 0.26f, 1f);
        [SerializeField] private Color engagedRing = new Color(0.13f, 0.17f, 0.26f, 1f);
        [SerializeField] private Color engagedIcon = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color clearedRing = new Color(0.055f, 0.353f, 0.325f, 1f);
        [SerializeField] private Color clearedIcon = new Color(1f, 1f, 1f, 1f);

        [SerializeField] private float pulseScale = 0.14f;
        [SerializeField] private float pulseFrequency = 2.6f;

        private float pulseTime;

        public RectTransform Body => body;

        public int Section { get; private set; }

        public StageMarkerState State { get; private set; } = StageMarkerState.Upcoming;

        public void Initialize(int section, Sprite iconSprite)
        {
            Section = section;
            pulseTime = 0f;

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
                icon.preserveAspect = true;
            }

            SetState(StageMarkerState.Upcoming);
        }

        public void SetState(StageMarkerState state)
        {
            State = state;

            if (state != StageMarkerState.Engaged)
            {
                pulseTime = 0f;

                if (body != null)
                    body.localScale = Vector3.one;
            }

            if (ring != null)
                ring.color = Resolve(state, upcomingRing, engagedRing, clearedRing);

            if (icon != null)
                icon.color = Resolve(state, upcomingIcon, engagedIcon, clearedIcon);
        }

        private void Update()
        {
            if (State != StageMarkerState.Engaged || body == null)
                return;

            pulseTime += Time.deltaTime;

            var wave = Mathf.Sin(pulseTime * pulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            var scale = 1f + wave * pulseScale;

            body.localScale = new Vector3(scale, scale, 1f);
        }

        private static Color Resolve(StageMarkerState state, Color upcoming, Color engaged, Color cleared)
        {
            switch (state)
            {
                case StageMarkerState.Engaged:
                    return engaged;
                case StageMarkerState.Cleared:
                    return cleared;
                default:
                    return upcoming;
            }
        }
    }
}
