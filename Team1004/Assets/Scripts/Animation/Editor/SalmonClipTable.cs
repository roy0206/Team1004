using System.Collections.Generic;

namespace Game.Animation.Editor
{
    public enum SalmonClipDuration
    {
        FromFrames,
        LaneMove,
        Jump
    }

    public sealed class SalmonClipDefinition
    {
        public SalmonClipDefinition(
            string assetName,
            string folderName,
            string framePrefix,
            int frameCount,
            float framesPerSecond,
            bool loop,
            SalmonClipDuration durationSource)
        {
            AssetName = assetName;
            FolderName = folderName;
            FramePrefix = framePrefix;
            FrameCount = frameCount;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
            DurationSource = durationSource;
        }

        public string AssetName { get; }
        public string FolderName { get; }
        public string FramePrefix { get; }
        public int FrameCount { get; }
        public float FramesPerSecond { get; }
        public bool Loop { get; }
        public SalmonClipDuration DurationSource { get; }

        public string AssetPath => AnimationAssetGenerator.DesignFolder + "/" + AssetName + ".asset";
        public string FolderPath => AnimationAssetGenerator.ArtFolder + "/" + FolderName;
    }

    public static class SalmonClipTable
    {
        private static readonly SalmonClipDefinition[] Definitions =
        {
            new("Player_Swim", "기본", "물고기 기본", 2, 6f, true, SalmonClipDuration.FromFrames),
            new("Player_LaneUp", "올라가기", "물고기 올라가기", 2, 10f, false, SalmonClipDuration.LaneMove),
            new("Player_LaneDown", "내려가기", "물고기 내려가기", 2, 10f, false, SalmonClipDuration.LaneMove),
            new("Player_Jump", "점프", "물고기 점프", 5, 12f, false, SalmonClipDuration.Jump)
        };

        public static IReadOnlyList<SalmonClipDefinition> All => Definitions;
        public static SalmonClipDefinition Swim => Definitions[0];
        public static SalmonClipDefinition LaneUp => Definitions[1];
        public static SalmonClipDefinition LaneDown => Definitions[2];
        public static SalmonClipDefinition Jump => Definitions[3];

        public static int TotalFrameCount
        {
            get
            {
                var total = 0;

                for (var i = 0; i < Definitions.Length; i++)
                    total += Definitions[i].FrameCount;

                return total;
            }
        }

        public static SalmonClipDefinition Find(string assetName)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].AssetName == assetName)
                    return Definitions[i];
            }

            return null;
        }
    }
}
