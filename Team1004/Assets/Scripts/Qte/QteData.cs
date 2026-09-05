using UnityEngine;

namespace Game.Qte
{
    [CreateAssetMenu(menuName = "Team1004/Qte Data", fileName = "QteData")]
    public sealed class QteData : ScriptableObject
    {
        [SerializeField] private int requiredPresses = 6;
        [SerializeField] private bool firstKeyUp = true;
        [SerializeField] private float pressFlashDuration = 0.18f;
        [SerializeField] private string upAction = "Player/LaneUp";
        [SerializeField] private string downAction = "Player/LaneDown";

        public int RequiredPresses => Mathf.Max(1, requiredPresses);
        public bool FirstKeyUp => firstKeyUp;
        public float PressFlashDuration => Mathf.Max(0f, pressFlashDuration);
        public string UpAction => upAction;
        public string DownAction => downAction;
    }
}
