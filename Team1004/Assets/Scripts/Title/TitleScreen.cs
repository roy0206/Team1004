using Game.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Title
{
    public sealed class TitleScreen : MonoBehaviour
    {
        private const string TitleBgmId = "bgm_title";
        private const string PlayBgmId = "bgm_play";
        private const string NoRecordText = "아직 클리어 없음";
        private const string RecordFormat = "클리어 {0}회";

        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private SceneReference playScene;
        [SerializeField] private Text recordText;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStart);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (recordText != null)
                recordText.text = BuildRecordText();

            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);

            if (AudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
            {
                audio.StopBgm(PlayBgmId);
                audio.PlayBgm(TitleBgmId);
            }
        }

        private static string BuildRecordText()
        {
            var count = RecordService.ClearCount;
            return count <= 0 ? NoRecordText : string.Format(RecordFormat, count);
        }

        private async void OnStart()
        {
            UiSound.PlayClick();

            if (playScene == null)
            {
                Debug.LogError("[TitleScreen] Play scene reference is not assigned.", this);
                return;
            }

            if (!SceneController.TryGetInstance(out var scenes) || scenes.IsTransitioning)
                return;

            await scenes.LoadAsync(playScene);
        }

        private void OnSettings()
        {
            UiSound.PlayClick();

            if (settingsPanel != null)
                settingsPanel.Open();
        }

        private void OnQuit()
        {
            UiSound.PlayClick();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
