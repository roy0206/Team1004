using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    public static class DialogueService
    {
        private static readonly IReadOnlyList<string> NoWarnings = Array.Empty<string>();
        private static readonly HashSet<string> missingWarnings = new(StringComparer.Ordinal);

        private static DialogueTable table;

        public static bool IsLoaded => table != null;
        public static int Count => table != null ? table.Count : 0;
        public static IReadOnlyList<string> Warnings => table != null ? table.Warnings : NoWarnings;
        public static IReadOnlyCollection<string> Ids => table != null ? table.Ids : Array.Empty<string>();

        public static void Load(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("[DialogueService] TextAsset is null. Dialogue table was not loaded.");
                return;
            }

            LoadText(asset.text, asset.name);
        }

        public static void Load(string csvText)
        {
            LoadText(csvText, "csv");
        }

        public static void Reload(TextAsset asset)
        {
            Unload();
            Load(asset);
        }

        public static void Reload(string csvText)
        {
            Unload();
            Load(csvText);
        }

        public static void Unload()
        {
            table = null;
            missingWarnings.Clear();
        }

        public static bool TryGet(string id, out DialogueLine line)
        {
            if (table == null)
            {
                line = null;
                return false;
            }

            return table.TryGet(id, out line);
        }

        public static DialogueLine Get(string id)
        {
            if (TryGet(id, out var line))
                return line;

            if (missingWarnings.Add(id ?? string.Empty))
            {
                if (table == null)
                    Debug.LogWarning($"[DialogueService] Dialogue table is not loaded. '{id}' falls back to a placeholder line.");
                else
                    Debug.LogWarning($"[DialogueService] Dialogue id is not found: '{id}'. Add it to Assets/GameAssets/Design/Dialogue/dialogue.csv.");
            }

            return DialogueLine.Missing(id);
        }

        private static void LoadText(string csvText, string source)
        {
            missingWarnings.Clear();
            table = DialogueTable.Parse(csvText);

            var warnings = table.Warnings;
            for (var i = 0; i < warnings.Count; i++)
                Debug.LogWarning($"[DialogueService] {source}: {warnings[i]}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            table = null;
            missingWarnings.Clear();
        }
    }
}
