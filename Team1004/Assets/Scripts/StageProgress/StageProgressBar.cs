using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.StageProgress
{
    [DisallowMultipleComponent]
    public sealed class StageProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fillArea;
        [SerializeField] private RectTransform fill;
        [SerializeField] private RectTransform handle;
        [SerializeField] private SwimmingHandle swimmer;
        [SerializeField] private RectTransform markerLayer;
        [SerializeField] private StageProgressMarker markerTemplate;
        [SerializeField] private Sprite[] bossIcons;

        [SerializeField] private bool bindPlayFlow = true;
        [SerializeField] private float followSharpness = 9f;
        [SerializeField] private float speedReference = 0.05f;
        [SerializeField, Range(0f, 1f)] private float manualProgress;

        private readonly List<StageMarker> layout = new List<StageMarker>();
        private readonly List<StageProgressMarker> markers = new List<StageProgressMarker>();

        private IStageProgressSource source;
        private bool initialized;
        private bool markersBuilt;
        private Image fillGraphic;
        private float displayed;
        private float target;
        private float layoutSignature = float.NaN;
        private float lastWidth = -1f;

        public float Progress01 => displayed;

        public int MarkerCount => markers.Count;

        public void Initialize(IStageProgressSource progressSource = null)
        {
            if (!initialized)
            {
                initialized = true;

                if (markerTemplate != null)
                    markerTemplate.gameObject.SetActive(false);

                if (fill != null)
                    fillGraphic = fill.GetComponent<Image>();

                if (swimmer != null)
                    swimmer.Initialize();
            }

            if (progressSource != null)
                source = progressSource;
            else if (source == null && bindPlayFlow)
                source = new PlayFlowProgressSource();

            RebuildMarkers();
        }

        public void SetSource(IStageProgressSource progressSource)
        {
            source = progressSource;
            RebuildMarkers();
        }

        public void SetProgress(float value)
        {
            manualProgress = Mathf.Clamp01(value);
            target = manualProgress;
        }

        public void SnapToTarget()
        {
            displayed = target;
            ApplyLayout(displayed, true);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            var hasSource = source != null && source.IsAvailable;

            if (hasSource)
            {
                RebuildMarkers();
                target = StageProgressLayout.Progress01(source.SectionDurations, source.Section, source.SectionTime);
            }
            else
            {
                target = manualProgress;
            }

            var previous = displayed;

            displayed = followSharpness <= 0f || delta <= 0f
                ? target
                : Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-followSharpness * delta));

            if (Mathf.Abs(target - displayed) < 0.0002f)
                displayed = target;

            ApplyLayout(displayed, false);
            ApplyMarkerStates(hasSource);

            if (swimmer != null && delta > 0f && speedReference > 0f)
                swimmer.SetSpeed01(Mathf.Clamp01(Mathf.Abs(displayed - previous) / delta / speedReference));
        }

        private void RebuildMarkers()
        {
            var durations = source != null ? source.SectionDurations : null;
            var signature = Signature(durations);

            if (markersBuilt && Mathf.Approximately(signature, layoutSignature))
                return;

            layoutSignature = signature;
            markersBuilt = true;

            StageProgressLayout.BuildMarkers(durations, layout);

            if (markerTemplate == null || markerLayer == null)
                return;

            for (var i = 0; i < markers.Count; i++)
                if (markers[i] != null)
                    Destroy(markers[i].gameObject);

            markers.Clear();

            for (var i = 0; i < layout.Count; i++)
            {
                var instance = Instantiate(markerTemplate, markerLayer);

                instance.gameObject.name = $"BossMarker{layout[i].Section}";
                instance.gameObject.SetActive(true);
                instance.Initialize(layout[i].Section, IconFor(i));

                markers.Add(instance);
            }

            lastWidth = -1f;
        }

        private void ApplyLayout(float progress, bool force)
        {
            if (fillArea == null)
                return;

            var width = fillArea.rect.width;

            var filled = width * progress;

            if (fillGraphic != null)
                fillGraphic.enabled = filled > 1f;

            if (fill != null)
                fill.sizeDelta = new Vector2(filled, fill.sizeDelta.y);

            if (handle != null)
                handle.anchoredPosition = new Vector2(filled, handle.anchoredPosition.y);

            if (!force && Mathf.Approximately(width, lastWidth))
                return;

            lastWidth = width;

            for (var i = 0; i < markers.Count && i < layout.Count; i++)
            {
                if (markers[i] == null)
                    continue;

                if (markers[i].transform is not RectTransform rect)
                    continue;

                rect.anchoredPosition = new Vector2(width * layout[i].Position01, rect.anchoredPosition.y);
            }
        }

        private void ApplyMarkerStates(bool hasSource)
        {
            var section = hasSource ? source.Section : 0;
            var engaged = hasSource && source.IsBossEngaged;

            for (var i = 0; i < markers.Count && i < layout.Count; i++)
            {
                if (markers[i] == null)
                    continue;

                var markerSection = layout[i].Section;
                StageMarkerState state;

                if (hasSource)
                {
                    if (section > markerSection)
                        state = StageMarkerState.Cleared;
                    else if (section == markerSection && engaged)
                        state = StageMarkerState.Engaged;
                    else
                        state = StageMarkerState.Upcoming;
                }
                else
                {
                    state = displayed > layout[i].Position01 + 0.0005f
                        ? StageMarkerState.Cleared
                        : StageMarkerState.Upcoming;
                }

                if (markers[i].State != state)
                    markers[i].SetState(state);
            }
        }

        private Sprite IconFor(int index)
        {
            if (bossIcons == null || bossIcons.Length == 0)
                return null;

            return bossIcons[Mathf.Clamp(index, 0, bossIcons.Length - 1)];
        }

        private static float Signature(IReadOnlyList<float> durations)
        {
            if (durations == null || durations.Count == 0)
                return 0f;

            var value = durations.Count * 7919f;

            for (var i = 0; i < durations.Count; i++)
                value += Mathf.Max(0f, durations[i]) * (i + 1) * 31f;

            return value;
        }
    }
}
