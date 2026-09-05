using UnityEngine;

namespace Game.Spawner
{
    [CreateAssetMenu(fileName = "Obstacle", menuName = "Team1004/Spawner/Obstacle Definition")]
    public sealed class ObstacleDefinition : ScriptableObject
    {
        [SerializeField] private string id = "Rock";
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int laneSpan = 1;
        [SerializeField] private LaneFlags allowedStartLanes = LaneFlags.All;
        [SerializeField, Min(0.1f)] private float bodyLength = 1f;
        [SerializeField, Min(0f)] private float collisionLength = 0.9f;
        [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
        [SerializeField] private bool usableInNormalPatterns = true;
        [SerializeField] private bool usableInJumpPatterns = true;

        public string Id => id;
        public GameObject Prefab => prefab;
        public int LaneSpan => laneSpan;
        public LaneFlags AllowedStartLanes => allowedStartLanes;
        public float BodyLength => bodyLength;
        public float CollisionLength => collisionLength;
        public float SpeedMultiplier => speedMultiplier;
        public bool UsableInNormalPatterns => usableInNormalPatterns;
        public bool UsableInJumpPatterns => usableInJumpPatterns;
        public string PoolKey => string.IsNullOrWhiteSpace(id) ? name : id;

        public ObstacleSpec ToSpec()
        {
            return new ObstacleSpec(
                PoolKey,
                PoolKey,
                laneSpan,
                (int)allowedStartLanes,
                bodyLength,
                collisionLength,
                speedMultiplier,
                usableInNormalPatterns,
                usableInJumpPatterns);
        }

#if UNITY_EDITOR
        public void EditorInitialize(ObstacleSpec spec, GameObject prefab)
        {
            id = spec.Id;
            this.prefab = prefab;
            laneSpan = spec.LaneSpan;
            allowedStartLanes = (LaneFlags)spec.AllowedStartLaneMask;
            bodyLength = spec.BodyLength;
            collisionLength = spec.CollisionLength;
            speedMultiplier = spec.SpeedMultiplier;
            usableInNormalPatterns = spec.UsableInNormalPatterns;
            usableInJumpPatterns = spec.UsableInJumpPatterns;
        }
#endif
    }
}
