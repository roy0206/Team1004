using UnityEngine;

namespace Game.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Team1004/Game Config")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [SerializeField] private GameConfigValues values = new();

        public GameConfigValues Values => values;
    }
}
