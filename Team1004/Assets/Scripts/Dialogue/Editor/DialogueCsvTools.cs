using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Cutscene;
using UnityEditor;
using UnityEngine;

namespace Game.Dialogue.Editor
{
    public static class DialogueCsvTools
    {
        public const string DialogueFolder = "Assets/GameAssets/Design/Dialogue";
        public const string DialogueCsvPath = DialogueFolder + "/dialogue.csv";

        private const string DocumentsFolder = "Assets/Documents";
        private const string CsvExtension = "*.csv";

        private static readonly string[] NameHints = { "dialogue", "대사" };

        [MenuItem("Team1004/Import Dialogue CSV")]
        public static void Import()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[DialogueCsv] Could not resolve the project root.");
                return;
            }

            var documents = Path.Combine(projectRoot, DocumentsFolder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(documents))
            {
                Debug.LogWarning($"[DialogueCsv] '{DocumentsFolder}' is missing. Run the Drive sync first.");
                return;
            }

            var candidates = new List<string>();
            var files = Directory.GetFiles(documents, CsvExtension, SearchOption.AllDirectories);

            for (var i = 0; i < files.Length; i++)
                if (IsDialogueCsv(files[i]))
                    candidates.Add(files[i]);

            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"[DialogueCsv] No dialogue csv was found under '{DocumentsFolder}'. " +
                    "The file name must be dialogue.csv or contain 'dialogue' or '대사'.");
                return;
            }

            var newest = candidates[0];
            var newestTime = File.GetLastWriteTimeUtc(newest);

            for (var i = 1; i < candidates.Count; i++)
            {
                var time = File.GetLastWriteTimeUtc(candidates[i]);
                if (time <= newestTime)
                    continue;

                newest = candidates[i];
                newestTime = time;
            }

            if (candidates.Count > 1)
            {
                var list = new StringBuilder();
                for (var i = 0; i < candidates.Count; i++)
                    list.Append("\n  ").Append(ToAssetPath(projectRoot, candidates[i]));

                Debug.Log($"[DialogueCsv] {candidates.Count} candidates were found. The newest one is used.{list}");
            }

            EnsureFolder(DialogueFolder);

            var target = Path.Combine(projectRoot, DialogueCsvPath.Replace('/', Path.DirectorySeparatorChar));
            File.Copy(newest, target, true);
            AssetDatabase.ImportAsset(DialogueCsvPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"[DialogueCsv] Imported '{ToAssetPath(projectRoot, newest)}' " +
                $"({newestTime.ToLocalTime():yyyy-MM-dd HH:mm}) -> '{DialogueCsvPath}'.");

            Validate();
        }

        [MenuItem("Team1004/Validate Dialogue CSV")]
        public static void Validate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DialogueCsvPath);
            if (asset == null)
            {
                Debug.LogError($"[DialogueCsv] '{DialogueCsvPath}' is missing. Run 'Team1004/Import Dialogue CSV' first.");
                return;
            }

            var table = DialogueTable.Parse(asset.text);
            var report = new StringBuilder();
            report.Append("[DialogueCsv] ").Append(DialogueCsvPath).Append(" : ").Append(table.Count).Append(" lines");

            var warnings = table.Warnings;
            report.Append("\n\n[파싱 경고] ").Append(warnings.Count);
            for (var i = 0; i < warnings.Count; i++)
                report.Append("\n  ").Append(warnings[i]);

            var references = CollectReferences();

            var missing = new List<string>();
            foreach (var pair in references)
                if (!table.TryGet(pair.Key, out _))
                    missing.Add($"{pair.Key}  <-  {string.Join(", ", pair.Value)}");

            missing.Sort(StringComparer.Ordinal);

            var unused = new List<string>();
            foreach (var id in table.Ids)
                if (!references.ContainsKey(id))
                    unused.Add(id);

            unused.Sort(StringComparer.Ordinal);

            report.Append("\n\n[컷신이 참조하지만 CSV에 없는 id] ").Append(missing.Count);
            for (var i = 0; i < missing.Count; i++)
                report.Append("\n  ").Append(missing[i]);

            report.Append("\n\n[CSV에 있지만 아무도 안 쓰는 id] ").Append(unused.Count);
            for (var i = 0; i < unused.Count; i++)
                report.Append("\n  ").Append(unused[i]);

            var text = report.ToString();

            if (missing.Count > 0)
                Debug.LogError(text);
            else if (warnings.Count > 0 || unused.Count > 0)
                Debug.LogWarning(text);
            else
                Debug.Log(text);
        }

        private static Dictionary<string, List<string>> CollectReferences()
        {
            var references = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var cutscenes = new List<CutsceneBase>();
            CutsceneCatalog.CreateAll(cutscenes);

            for (var i = 0; i < cutscenes.Count; i++)
            {
                var cutscene = cutscenes[i];
                var lineIds = cutscene.LineIds;
                if (lineIds == null)
                    continue;

                for (var j = 0; j < lineIds.Count; j++)
                {
                    var id = lineIds[j];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!references.TryGetValue(id, out var users))
                    {
                        users = new List<string>();
                        references.Add(id, users);
                    }

                    if (!users.Contains(cutscene.Id))
                        users.Add(cutscene.Id);
                }
            }

            return references;
        }

        private static bool IsDialogueCsv(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(name))
                return false;

            var lowered = name.ToLowerInvariant();
            for (var i = 0; i < NameHints.Length; i++)
                if (lowered.Contains(NameHints[i]))
                    return true;

            return false;
        }

        private static string ToAssetPath(string projectRoot, string fullPath)
        {
            var normalized = fullPath.Replace('\\', '/');
            var root = projectRoot.Replace('\\', '/');

            if (!root.EndsWith("/", StringComparison.Ordinal))
                root += "/";

            return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(root.Length)
                : normalized;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
