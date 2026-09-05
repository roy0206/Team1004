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

        private float hintUntil = -1f;

        public bool IsControlHintVisible => controlHint != null && controlHint.gameObject.activeSelf;

        private void Awake()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (controlHint != null)
                controlHint.gameObject.SetActive(false);
        }

        public void ShowControlHint(float seconds)
        {
            if (controlHint == null || seconds <= 0f)
                return;

            hintUntil = Time.time + seconds;
            controlHint.gameObject.SetActive(true);
        }

        public void HideControlHint()
        {
            hintUntil = -1f;

            if (controlHint != null)
                controlHint.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (hintUntil >= 0f && Time.time >= hintUntil)
                HideControlHint();

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
