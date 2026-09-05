using UnityEngine;
using UnityEngine.UI;

namespace Game.Qte
{
    public sealed class QtePanel : MonoBehaviour
    {
        private const string UpGlyph = "↑";
        private const string DownGlyph = "↓";

        private static QtePanel current;

        [SerializeField] private GameObject content;
        [SerializeField] private Text arrow;
        [SerializeField] private Image[] dots;
        [SerializeField] private Image timeBarFill;
        [SerializeField] private Color arrowColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color arrowFlashColor = new(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private Color dotFilledColor = new(1f, 0.82f, 0.32f, 1f);
        [SerializeField] private Color dotEmptyColor = new(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color barColor = new(0.4f, 0.88f, 0.7f, 1f);
        [SerializeField] private Color barDangerColor = new(0.95f, 0.35f, 0.3f, 1f);
        [SerializeField] private float flashDuration = 0.18f;
        [SerializeField] private float punchScale = 1.35f;
        [SerializeField] private float dangerThreshold01 = 0.3f;

        private float flashRemaining;
        private int visibleDots;
        private int filledDots;

        public static bool TryGetCurrent(out QtePanel panel)
        {
            panel = current;
            return panel != null;
        }

        public bool IsVisible => content != null && content.activeSelf;
        public int VisibleDotCount => visibleDots;
        public int FilledDotCount => filledDots;

        public float FlashDuration
        {
            get => flashDuration;
            set => flashDuration = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (current == null)
                current = this;

            Hide();
        }

        private void OnDestroy()
        {
            if (current == this)
                current = null;
        }

        public void Show(int requiredPresses, QteKey expected)
        {
            visibleDots = Mathf.Max(0, requiredPresses);
            filledDots = 0;
            flashRemaining = 0f;

            ApplyDots();
            SetExpected(expected);
            SetRemaining01(1f);
            ApplyArrowVisual(0f);

            if (content != null && !content.activeSelf)
                content.SetActive(true);
        }

        public void Hide()
        {
            flashRemaining = 0f;

            if (content != null && content.activeSelf)
                content.SetActive(false);
        }

        public void SetExpected(QteKey key)
        {
            if (arrow == null)
                return;

            arrow.text = key == QteKey.Down ? DownGlyph : UpGlyph;
        }

        public void SetProgress(int progress)
        {
            filledDots = Mathf.Clamp(progress, 0, visibleDots);
            ApplyDots();
        }

        public void SetRemaining01(float value)
        {
            if (timeBarFill == null)
                return;

            var amount = Mathf.Clamp01(value);
            timeBarFill.fillAmount = amount;
            timeBarFill.color = amount <= dangerThreshold01 ? barDangerColor : barColor;
        }

        public void Punch()
        {
            flashRemaining = flashDuration;
            ApplyArrowVisual(1f);
        }

        private void Update()
        {
            if (flashRemaining <= 0f)
                return;

            flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
            ApplyArrowVisual(flashDuration <= 0f ? 0f : flashRemaining / flashDuration);
        }

        private void ApplyArrowVisual(float flash01)
        {
            if (arrow == null)
                return;

            var t = Mathf.Clamp01(flash01);
            arrow.color = Color.Lerp(arrowColor, arrowFlashColor, t);

            var scale = Mathf.Lerp(1f, Mathf.Max(1f, punchScale), t);
            arrow.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyDots()
        {
            if (dots == null)
                return;

            for (var i = 0; i < dots.Length; i++)
            {
                var dot = dots[i];

                if (dot == null)
                    continue;

                var used = i < visibleDots;

                if (dot.gameObject.activeSelf != used)
                    dot.gameObject.SetActive(used);

                if (used)
                    dot.color = i < filledDots ? dotFilledColor : dotEmptyColor;
            }
        }
    }
}
