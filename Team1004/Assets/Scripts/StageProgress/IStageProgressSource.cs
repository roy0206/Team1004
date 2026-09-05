using System.Collections.Generic;

namespace Game.StageProgress
{
    public interface IStageProgressSource
    {
        bool IsAvailable { get; }

        IReadOnlyList<float> SectionDurations { get; }

        int Section { get; }

        float SectionTime { get; }

        bool IsBossEngaged { get; }
    }
}
