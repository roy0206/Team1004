using System.Collections.Generic;
using Game.Config;
using Game.Play;

namespace Game.StageProgress
{
    public sealed class PlayFlowProgressSource : IStageProgressSource
    {
        private static readonly float[] Empty = new float[0];

        public bool IsAvailable => PlayFlow.TryGetCurrent(out _);

        public IReadOnlyList<float> SectionDurations
        {
            get
            {
                if (!GameConfig.IsLoaded)
                    return Empty;

                return GameConfig.Current.SectionDurations ?? Empty;
            }
        }

        public int Section => PlayFlow.TryGetCurrent(out var flow) ? flow.Section : 1;

        public float SectionTime => PlayFlow.TryGetCurrent(out var flow) ? flow.SectionTime : 0f;

        public bool IsBossEngaged => PlayFlow.TryGetCurrent(out var flow) && flow.State == PlayState.Boss;
    }
}
