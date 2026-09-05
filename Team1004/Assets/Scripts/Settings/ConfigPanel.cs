using Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings
{
    public sealed class ConfigPanel : MonoBehaviour
    {
        [SerializeField] private InputField jsonField;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text statusText;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(Apply);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetOverride);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
            SetStatus(GameConfig.HasOverride ? $"오버라이드 사용 중: {GameConfig.OverridePath}" : "기본값 사용 중");
        }

        public void Close()
        {
            UiSound.PlayClick();
            gameObject.SetActive(false);
        }

        private void Apply()
        {
            UiSound.PlayClick();

            var json = jsonField != null ? jsonField.text : string.Empty;
            if (!GameConfig.TryApplyJson(json, out var error))
            {
                SetStatus($"적용 실패: {error}");
                return;
            }

            if (!GameConfig.SaveOverride(out error))
            {
                SetStatus($"적용됨 (저장 실패: {error})");
                return;
            }

            Refresh();
            SetStatus($"적용·저장됨: {GameConfig.OverridePath}");
        }

        private void ResetOverride()
        {
            UiSound.PlayClick();

            if (!GameConfig.ResetOverride(out var error))
            {
                SetStatus($"초기화 실패: {error}");
                return;
            }

            Refresh();
            SetStatus("기본값으로 초기화됨");
        }

        private void Refresh()
        {
            if (jsonField != null)
                jsonField.text = GameConfig.ToJson();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
