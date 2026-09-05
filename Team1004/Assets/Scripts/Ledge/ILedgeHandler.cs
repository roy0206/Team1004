using System;

namespace Game.Ledge
{
    public interface ILedgeHandler
    {
        bool IsActive { get; }
        bool IsHoldingWorld { get; }
        bool IsSpawnSuspended { get; }
        bool IsQteActive { get; }
        float WorldSpeedScale { get; }
        int Section { get; }

        void BeginSection(int section, float sectionDuration);
        void Tick(float deltaTime);
        void Stop();

        event Action<int> Approaching;
        event Action<int> QteStarted;
        event Action<int> Resumed;
        event Action<int> Cleared;
        event Action<int> Finished;
        event Action<int> Failed;
    }
}
