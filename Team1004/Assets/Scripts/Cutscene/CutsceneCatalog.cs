using System;
using System.Collections.Generic;
using Game.Cutscene.Scenes;
using UnityEngine;

namespace Game.Cutscene
{
    public static class CutsceneCatalog
    {
        public const string Intro = "intro";
        public const string FishingLine = "cutscene1";
        public const string Walrus = "cutscene2";
        public const string Waterfall = "cutscene3";
        public const string Ending = "ending";

        private static readonly string[] SectionIdList = { FishingLine, Walrus, Waterfall };

        private static readonly Dictionary<string, Func<CutsceneBase>> Factories =
            new(StringComparer.Ordinal)
            {
                { Intro, () => new IntroCutscene() },
                { FishingLine, () => new FishingLineCutscene() },
                { Walrus, () => new WalrusCutscene() },
                { Waterfall, () => new WaterfallCutscene() },
                { Ending, () => new EndingCutscene() }
            };

        public static IReadOnlyList<string> SectionIds => SectionIdList;
        public static IReadOnlyCollection<string> Ids => Factories.Keys;

        public static bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && Factories.ContainsKey(id);
        }

        public static bool TryCreate(string id, out CutsceneBase cutscene)
        {
            if (!string.IsNullOrEmpty(id) && Factories.TryGetValue(id, out var factory))
            {
                cutscene = factory();
                return cutscene != null;
            }

            cutscene = null;
            return false;
        }

        public static CutsceneBase Create(string id)
        {
            if (TryCreate(id, out var cutscene))
                return cutscene;

            Debug.LogWarning($"[Cutscene] Unknown cutscene id: '{id}'. Register it in CutsceneCatalog.");
            return null;
        }

        public static void CreateAll(List<CutsceneBase> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();

            foreach (var pair in Factories)
            {
                var cutscene = pair.Value();
                if (cutscene != null)
                    buffer.Add(cutscene);
            }
        }
    }
}
