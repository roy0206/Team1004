using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public sealed class BossTimerView : MonoBehaviour
    {
        private const string TimerFormat = "{0:0}";

        [SerializeField] private Text nameText;
        [SerializeField] private Text timerText;

        public bool IsShown => gameObject.activeSelf;

        public void SetBossName(string bossName)
        {
            if (nameText != null)
                nameText.text = bossName ?? string.Empty;
        }

        public void SetBoss(string bossName)
        {
            SetBossName(bossName);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Show(float remainingSeconds)
        {
            gameObject.SetActive(true);
            SetRemaining(remainingSeconds);
        }

        public void SetRemaining(float remainingSeconds)
        {
            if (timerText != null)
                timerText.text = string.Format(TimerFormat, Mathf.Max(0f, Mathf.Ceil(remainingSeconds)));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
