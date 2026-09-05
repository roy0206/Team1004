using System.Collections.Generic;

namespace Game.Animation.Editor
{
    public static class ObstacleClipTable
    {
        public const string FishClipName = "Obstacle_Fish";
        public const string FishFolderName = "이빨물고기";
        public const string FishFramePrefix = "이빨";

        private static readonly FlipbookClipDefinition[] Definitions =
        {
            new(
                FishClipName,
                ObjectArtPostprocessor.ArtFolder,
                FishFolderName,
                FishFramePrefix,
                2,
                6f,
                true,
                FlipbookClipDuration.FromFrames)
        };

        public static IReadOnlyList<FlipbookClipDefinition> All => Definitions;
        public static FlipbookClipDefinition Fish => Definitions[0];

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
