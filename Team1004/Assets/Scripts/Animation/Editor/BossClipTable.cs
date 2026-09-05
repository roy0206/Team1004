using System.Collections.Generic;

namespace Game.Animation.Editor
{
    public static class BossClipTable
    {
        public const string RapidClipName = "Boss_Rapid";
        public const string RapidFolderName = "물결";

        private static readonly FlipbookClipDefinition[] Definitions =
        {
            new(
                RapidClipName,
                BossArtPostprocessor.ArtFolder,
                RapidFolderName,
                RapidFolderName,
                2,
                6f,
                true,
                FlipbookClipDuration.FromFrames)
        };

        public static IReadOnlyList<FlipbookClipDefinition> All => Definitions;
        public static FlipbookClipDefinition Rapid => Definitions[0];

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
