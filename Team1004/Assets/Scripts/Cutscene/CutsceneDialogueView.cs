using System;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Cutscene
{
    public sealed class CutsceneDialogueView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject hint;
        [SerializeField] private Image screenFade;
        [SerializeField] private float panelFadeDuration = 0.15f;

        private Tween typingTween;
        private Tween panelTween;
        private string fullText = string.Empty;
        private int visibleCount;
        private bool typingDone = true;
        private Action typingCompleted;

        public bool IsShown { get; private set; }
        public bool IsTyping => !typingDone;
        public bool HasScreenFade => screenFade != null;
        public float ScreenAlpha => screenFade != null ? screenFade.color.a : 0f;
        public Canvas Canvas => canvas;

        public void SetWorldCamera(Camera worldCamera)
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvas == null || worldCamera == null)
                return;

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                canvas.worldCamera = worldCamera;
        }

        private void Awake()
        {
            if (panel != null)
            {
                panel.alpha = 0f;
                panel.interactable = false;
                panel.blocksRaycasts = false;
            }

            SetHintVisible(false);
        }

        private void OnDestroy()
        {
            KillTyping();
            KillPanelTween();
        }

        public void Show(string speaker, string body, float charsPerSecond, Action onTypingCompleted)
        {
            KillTyping();
            typingCompleted = onTypingCompleted;
            fullText = body ?? string.Empty;

            if (speakerText != null)
            {
                speakerText.text = speaker ?? string.Empty;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }

            SetHintVisible(false);
            SetPanelVisible(true);

            var length = fullText.Length;
            if (length == 0 || charsPerSecond <= 0f)
            {
                SetVisibleCount(length);
                FinishTyping();
                return;
            }

            typingDone = false;
            SetVisibleCount(0);

            DOGetter<int> getter = () => visibleCount;
            DOSetter<int> setter = SetVisibleCount;
            typingTween = DOTween.To(getter, setter, length, length / charsPerSecond)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(FinishTyping);
        }

        public void CompleteTyping()
        {
            if (typingDone)
                return;

            if (typingTween != null && typingTween.IsActive())
                typingTween.Complete();

            FinishTyping();
        }

        public void Hide()
        {
            KillTyping();
            typingCompleted = null;
            SetHintVisible(false);
            SetPanelVisible(false);
        }

        public void HideImmediate()
        {
            KillTyping();
            KillPanelTween();
            typingCompleted = null;
            SetHintVisible(false);
            IsShown = false;

            if (panel != null)
                panel.alpha = 0f;
        }

        public void SetHintVisible(bool visible)
        {
            if (hint != null && hint.activeSelf != visible)
                hint.SetActive(visible);
        }

        public void SetScreenAlpha(float alpha)
        {
            if (screenFade == null)
                return;

            var color = screenFade.color;
            color.a = Mathf.Clamp01(alpha);
            screenFade.color = color;
        }

        public Tween CreateScreenFade(float alpha, float duration, Ease ease)
        {
            if (screenFade == null)
                return null;

            var image = screenFade;
            DOGetter<Color> getter = () => image.color;
            DOSetter<Color> setter = value => image.color = value;
            return DOTween.ToAlpha(getter, setter, Mathf.Clamp01(alpha), duration).SetEase(ease);
        }

        private void SetPanelVisible(bool visible)
        {
            IsShown = visible;

            if (panel == null)
                return;

            KillPanelTween();
            var target = visible ? 1f : 0f;

            if (panelFadeDuration <= 0f || Mathf.Approximately(panel.alpha, target))
            {
                panel.alpha = target;
                return;
            }

            var group = panel;
            DOGetter<float> getter = () => group.alpha;
            DOSetter<float> setter = value => group.alpha = value;
            panelTween = DOTween.To(getter, setter, target, panelFadeDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        private void SetVisibleCount(int count)
        {
            visibleCount = Mathf.Clamp(count, 0, fullText.Length);

            if (bodyText != null)
                bodyText.text = visibleCount >= fullText.Length ? fullText : fullText.Substring(0, visibleCount);
        }

        private void FinishTyping()
        {
            if (typingDone)
                return;

            typingDone = true;
            typingTween = null;
            SetVisibleCount(fullText.Length);

            var callback = typingCompleted;
            typingCompleted = null;

            try
            {
                callback?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void KillTyping()
        {
            if (typingTween != null && typingTween.IsActive())
                typingTween.Kill();

            typingTween = null;
            typingDone = true;
        }

        private void KillPanelTween()
        {
            if (panelTween != null && panelTween.IsActive())
                panelTween.Kill();

            panelTween = null;
        }
    }
}
