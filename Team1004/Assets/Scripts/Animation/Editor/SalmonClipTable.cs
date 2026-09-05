using System.Collections.Generic;

namespace Game.Animation.Editor
{
    public enum FlipbookClipDuration
    {
        FromFrames,
        LaneMove,
        Jump
    }

    public sealed class FlipbookClipDefinition
    {
        public FlipbookClipDefinition(
            string assetName,
            string folderName,
            string framePrefix,
            int frameCount,
            float framesPerSecond,
            bool loop,
            FlipbookClipDuration durationSource)
            : this(
                assetName,
                AnimationAssetGenerator.ArtFolder,
                folderName,
                framePrefix,
                frameCount,
                framesPerSecond,
                loop,
                durationSource)
        {
        }

        public FlipbookClipDefinition(
            string assetName,
            string rootFolder,
            string folderName,
            string framePrefix,
            int frameCount,
            float framesPerSecond,
            bool loop,
            FlipbookClipDuration durationSource)
        {
            AssetName = assetName;
            RootFolder = rootFolder;
            FolderName = folderName;
            FramePrefix = framePrefix;
            FrameCount = frameCount;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
            DurationSource = durationSource;
        }

        public string AssetName { get; }
        public string RootFolder { get; }
        public string FolderName { get; }
        public string FramePrefix { get; }
        public int FrameCount { get; }
        public float FramesPerSecond { get; }
        public bool Loop { get; }
        public FlipbookClipDuration DurationSource { get; }

        public string AssetPath => AnimationAssetGenerator.DesignFolder + "/" + AssetName + ".asset";

        public string FolderPath => string.IsNullOrEmpty(FolderName) ? RootFolder : RootFolder + "/" + FolderName;
    }

    public static class SalmonClipTable
    {
        private static readonly FlipbookClipDefinition[] Definitions =
        {
            new("Player_Swim", "기본", "물고기 기본", 2, 6f, true, FlipbookClipDuration.FromFrames),
            new("Player_LaneUp", "올라가기", "물고기 올라가기", 2, 10f, false, FlipbookClipDuration.LaneMove),
            new("Player_LaneDown", "내려가기", "물고기 내려가기", 2, 10f, false, FlipbookClipDuration.LaneMove),
            new("Player_Jump", "점프", "물고기 점프", 5, 12f, false, FlipbookClipDuration.Jump),
            new("Player_Hit", "충돌", "충돌", 1, 6f, false, FlipbookClipDuration.FromFrames)
        };

        public static IReadOnlyList<FlipbookClipDefinition> All => Definitions;
        public static FlipbookClipDefinition Swim => Definitions[0];
        public static FlipbookClipDefinition LaneUp => Definitions[1];
        public static FlipbookClipDefinition LaneDown => Definitions[2];
        public static FlipbookClipDefinition Jump => Definitions[3];
        public static FlipbookClipDefinition Hit => Definitions[4];

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

        public static FlipbookClipDefinition Find(string assetName)
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
