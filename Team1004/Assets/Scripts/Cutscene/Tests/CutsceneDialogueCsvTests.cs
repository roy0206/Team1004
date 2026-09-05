using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Game.Cutscene.Tests
{
    public sealed class CutsceneDialogueCsvTests
    {
        private const string CsvRelativePath = "GameAssets/Design/Dialogue/dialogue.csv";

        private DialogueTable table;

        [SetUp]
        public void SetUp()
        {
            var path = Path.Combine(Application.dataPath, CsvRelativePath);
            Assert.IsTrue(File.Exists(path), $"'{CsvRelativePath}' is missing.");

            table = DialogueTable.Parse(File.ReadAllText(path, Encoding.UTF8));
        }

        [Test]
        public void CsvParsesWithoutWarnings()
        {
            Assert.AreEqual(0, table.Warnings.Count, Join(table.Warnings));
        }

        [Test]
        public void EveryCutsceneLineIdExistsInTheCsv()
        {
            var missing = new List<string>();

            foreach (var pair in CollectReferences())
                if (!table.TryGet(pair.Key, out _))
                    missing.Add($"{pair.Key} <- {pair.Value}");

            Assert.AreEqual(0, missing.Count, Join(missing));
        }

        [Test]
        public void EveryCsvLineIdIsUsedByACutscene()
        {
            var references = CollectReferences();
            var unused = new List<string>();

            foreach (var id in table.Ids)
                if (!references.ContainsKey(id))
                    unused.Add(id);

            Assert.AreEqual(0, unused.Count, Join(unused));
        }

        [Test]
        public void EverySpeakerIsAKnownCharacter()
        {
            var allowed = new HashSet<string>
            {
                string.Empty,
                "연이",
                "연이 (어린 시절)",
                "아이",
                "어른이 된 아이"
            };

            foreach (var id in table.Ids)
            {
                Assert.IsTrue(table.TryGet(id, out var line), id);
                Assert.IsTrue(allowed.Contains(line.Speaker ?? string.Empty), $"{id}: '{line.Speaker}'");
            }
        }

        private static Dictionary<string, string> CollectReferences()
        {
            var references = new Dictionary<string, string>();
            var cutscenes = new List<CutsceneBase>();
            CutsceneCatalog.CreateAll(cutscenes);

            for (var i = 0; i < cutscenes.Count; i++)
            {
                var cutscene = cutscenes[i];
                var lineIds = cutscene.LineIds;
                if (lineIds == null)
                    continue;

                for (var j = 0; j < lineIds.Count; j++)
                    references[lineIds[j]] = cutscene.Id;
            }

            return references;
        }

        private static string Join(IReadOnlyList<string> values)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < values.Count; i++)
                builder.Append('\n').Append(values[i]);

            return builder.ToString();
        }
    }
}
