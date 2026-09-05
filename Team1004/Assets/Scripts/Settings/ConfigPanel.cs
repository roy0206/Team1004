using System;
using System.Collections.Generic;
using Game.Config;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings
{
    public sealed class ConfigPanel : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private ConfigFieldRow[] rows;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text statusText;

        private bool unboundReported;

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

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

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
            ClearInvalid();

            var root = new JObject();

            if (rows != null)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    if (row == null || string.IsNullOrEmpty(row.Key))
                        continue;

                    if (!row.TryBuild(out var token, out var rowError))
                    {
                        row.SetInvalid(true);
                        SetStatus($"입력 오류: {rowError}");
                        return;
                    }

                    root[row.Key] = token;
                }
            }

            if (!GameConfig.TryApplyJson(root.ToString(), out var error))
            {
                MarkInvalidByError(error);
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
            JObject root;

            try
            {
                root = JObject.Parse(GameConfig.ToJson());
            }
            catch (Exception exception)
            {
                SetStatus($"설정을 읽지 못했습니다: {exception.Message}");
                return;
            }

            ClearInvalid();

            var bound = new HashSet<string>();

            if (rows != null)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    if (row == null || string.IsNullOrEmpty(row.Key))
                        continue;

                    bound.Add(row.Key);
                    row.Bind(root[row.Key]);
                }
            }

            ReportUnbound(root, bound);
        }

        private void ReportUnbound(JObject root, HashSet<string> bound)
        {
            if (unboundReported)
                return;

            unboundReported = true;

            var missing = new List<string>();
            foreach (var property in root.Properties())
            {
                if (!bound.Contains(property.Name))
                    missing.Add(property.Name);
            }

            if (missing.Count == 0)
                return;

            Debug.LogWarning($"[ConfigPanel] 행이 없는 설정 키 {missing.Count}개: {string.Join(", ", missing)}. " +
                             "Tools/config_panel_rows.py의 ROWS에 추가하고 다시 실행해 SettingsPanel.prefab에 행을 만든다.");
        }

        private void MarkInvalidByError(string error)
        {
            if (rows == null || string.IsNullOrEmpty(error))
                return;

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrEmpty(row.Key))
                    continue;

                if (error.Contains(row.Key))
                    row.SetInvalid(true);
            }
        }

        private void ClearInvalid()
        {
            if (rows == null)
                return;

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null)
                    rows[i].SetInvalid(false);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
