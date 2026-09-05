using Game.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public sealed class PlayHud : MonoBehaviour
    {
        private const string DistanceFormat = "구간 {0} · 남은 {1:0}초";

        [SerializeField] private Image progressFill;
        [SerializeField] private Text distanceText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Text controlHint;
        [SerializeField] private Text banner;

        private float hintRemaining = -1f;

        public bool IsControlHintVisible => controlHint != null && controlHint.gameObject.activeSelf;
        public bool IsBannerVisible => banner != null && banner.gameObject.activeSelf;
        public string BannerText => banner != null ? banner.text : string.Empty;

        private void Awake()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (controlHint != null)
                controlHint.gameObject.SetActive(false);

            if (banner != null)
                banner.gameObject.SetActive(false);
        }

        public void ShowControlHint(float seconds)
        {
            if (controlHint == null || seconds <= 0f)
                return;

            hintRemaining = seconds;
            controlHint.gameObject.SetActive(true);
        }

        public void HideControlHint()
        {
            hintRemaining = -1f;

            if (controlHint != null)
                controlHint.gameObject.SetActive(false);
        }

        public void ShowBanner(string text)
        {
            if (banner == null)
                return;

            banner.text = text ?? string.Empty;
            banner.gameObject.SetActive(true);
        }

        public void HideBanner()
        {
            if (banner != null)
                banner.gameObject.SetActive(false);
        }

        public async Awaitable ShowBannerAsync(string text, float seconds)
        {
            if (banner == null || seconds <= 0f)
                return;

            ShowBanner(text);

            var remaining = seconds;

            while (this != null && remaining > 0f)
            {
                await Awaitable.NextFrameAsync();

                if (this == null)
                    return;

                remaining -= Time.deltaTime;
            }

            HideBanner();
        }

        private void Update()
        {
            if (hintRemaining >= 0f)
            {
                if (hintRemaining <= 0f)
                    HideControlHint();
                else
                    hintRemaining = Mathf.Max(0f, hintRemaining - Time.deltaTime);
            }

            if (!PlayFlow.TryGetCurrent(out var flow))
                return;

            if (progressFill != null)
                progressFill.fillAmount = flow.Progress01;

            if (distanceText != null)
                distanceText.text = string.Format(DistanceFormat, flow.Section, flow.SectionRemaining);
        }

        private void OnPauseClicked()
        {
            UiSound.PlayClick();

            if (PlayFlow.TryGetCurrent(out var flow))
                flow.Pause();
        }
    }
}
