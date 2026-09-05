using Game.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public sealed class PausePanel : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private SettingsPanel settingsPanel;

        public bool IsSettingsOpen => settingsPanel != null && settingsPanel.IsOpen;

        private void Awake()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);

            if (titleButton != null)
                titleButton.onClick.AddListener(OnTitle);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null && settingsPanel.IsOpen)
                settingsPanel.Close();
        }

        private void OnResume()
        {
            UiSound.PlayClick();

            if (PlayFlow.TryGetCurrent(out var flow))
                flow.Resume();
        }

        private void OnSettings()
        {
            UiSound.PlayClick();

            if (settingsPanel != null)
                settingsPanel.Open();
        }

        private void OnTitle()
        {
            UiSound.PlayClick();

            if (PlayFlow.TryGetCurrent(out var flow))
                flow.GoTitle();
        }
    }
}
