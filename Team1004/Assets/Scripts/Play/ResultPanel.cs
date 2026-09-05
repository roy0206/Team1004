using Game.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public sealed class ResultPanel : MonoBehaviour
    {
        private const string ClearedTitle = "처음 품은 뜻을, 끝까지.\n初志一貫";
        private const string FailedTitle = "실패";

        [SerializeField] private Text titleText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetry);

            if (titleButton != null)
                titleButton.onClick.AddListener(OnTitle);
        }

        public void Show(bool cleared)
        {
            if (titleText != null)
                titleText.text = cleared ? ClearedTitle : FailedTitle;

            if (titleButton != null)
                titleButton.gameObject.SetActive(cleared);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRetry()
        {
            UiSound.PlayClick();

            if (PlayFlow.TryGetCurrent(out var flow))
                flow.Retry();
        }

        private void OnTitle()
        {
            UiSound.PlayClick();

            if (PlayFlow.TryGetCurrent(out var flow))
                flow.GoTitle();
        }
    }
}
