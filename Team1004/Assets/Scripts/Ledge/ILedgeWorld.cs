namespace Game.Ledge
{
    public interface ILedgeWorld
    {
        void SetWorldScrolling(bool scrolling);
        void SetWorldSpeedScale(float scale);
        void SetObstacleSpawning(bool enabled);
    }
}
