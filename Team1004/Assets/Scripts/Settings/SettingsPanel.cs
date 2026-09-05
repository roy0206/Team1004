using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button developerButton;
        [SerializeField] private ConfigPanel configPanel;
        [SerializeField] private SceneReference titleScene;

        private bool dirty;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(OnMasterChanged);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBgmChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (titleButton != null)
                titleButton.onClick.AddListener(GoTitle);

            if (quitButton != null)
                quitButton.onClick.AddListener(Quit);

            if (developerButton != null)
                developerButton.onClick.AddListener(OpenDeveloperPanel);
        }

        public void Open()
        {
            gameObject.SetActive(true);

            if (configPanel != null)
                configPanel.gameObject.SetActive(false);

            if (titleButton != null)
                titleButton.gameObject.SetActive(titleScene != null);

            var data = SettingsService.Data;
            if (masterSlider != null)
                masterSlider.SetValueWithoutNotify(data.MasterVolume);

            if (bgmSlider != null)
                bgmSlider.SetValueWithoutNotify(data.BgmVolume);

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(data.SfxVolume);
        }

        public void Close()
        {
            UiSound.PlayClick();

            if (configPanel != null && configPanel.IsOpen)
            {
                configPanel.Close();
                return;
            }

            gameObject.SetActive(false);
            FlushSave();
        }

        private void OnMasterChanged(float value)
        {
            SettingsService.SetMasterVolume(value);
            dirty = true;
        }

        private void OnBgmChanged(float value)
        {
            SettingsService.SetBgmVolume(value);
            dirty = true;
        }

        private void OnSfxChanged(float value)
        {
            SettingsService.SetSfxVolume(value);
            dirty = true;
        }

        private void OpenDeveloperPanel()
        {
            UiSound.PlayClick();

            if (configPanel != null)
                configPanel.Open();
        }

        private async void GoTitle()
        {
            UiSound.PlayClick();

            if (titleScene == null || !SceneController.TryGetInstance(out var scenes) || scenes.IsTransitioning)
                return;

            FlushSave();
            await scenes.LoadAsync(titleScene);
        }

        private void Quit()
        {
            UiSound.PlayClick();
            FlushSave();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async void FlushSave()
        {
            if (!dirty)
                return;

            dirty = false;
            await SettingsService.SaveAsync();
        }
    }
}
