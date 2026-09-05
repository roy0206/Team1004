using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public sealed class JumpCooldownView : MonoBehaviour
    {
        [SerializeField] private Image gauge;
        [SerializeField] private Image icon;
        [SerializeField] private Color readyIconColor = Color.white;
        [SerializeField] private Color cooldownIconColor = new(0.35f, 0.35f, 0.35f, 1f);

        private void Update()
        {
            if (!PlayFlow.TryGetCurrent(out var flow) || flow.Player == null || flow.Player.Jump == null)
                return;

            var jump = flow.Player.Jump;
            var fill = jump.IsAirborne ? 0f : jump.Cooldown01;
            var ready = jump.CanJump;

            if (gauge != null)
                gauge.fillAmount = fill;

            if (icon != null)
                icon.color = ready ? readyIconColor : cooldownIconColor;
        }
    }
}
