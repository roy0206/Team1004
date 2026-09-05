using Game.Player;

namespace Game.Spawner
{
    public sealed class ObstacleThing : Hazard, IPooledObject
    {
        public ObstacleRuntimeModule Runtime => GetModule<ObstacleRuntimeModule>();

        public void OnSpawned()
        {
            ClearModules();
        }

        public void OnReleased()
        {
            ClearModules();
        }
    }
}
