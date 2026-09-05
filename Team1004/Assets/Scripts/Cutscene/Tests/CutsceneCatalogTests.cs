using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Cutscene.Tests
{
    public sealed class CutsceneCatalogTests
    {
        [Test]
        public void ContainsEveryStoryCutscene()
        {
            Assert.AreEqual(5, CutsceneCatalog.Ids.Count);

            Assert.IsTrue(CutsceneCatalog.Contains(CutsceneCatalog.Intro));
            Assert.IsTrue(CutsceneCatalog.Contains(CutsceneCatalog.FishingLine));
            Assert.IsTrue(CutsceneCatalog.Contains(CutsceneCatalog.Walrus));
            Assert.IsTrue(CutsceneCatalog.Contains(CutsceneCatalog.Waterfall));
            Assert.IsTrue(CutsceneCatalog.Contains(CutsceneCatalog.Ending));
        }

        [Test]
        public void SectionIdsMatchTheBossOrder()
        {
            var ids = CutsceneCatalog.SectionIds;

            Assert.AreEqual(3, ids.Count);
            Assert.AreEqual(CutsceneCatalog.FishingLine, ids[0]);
            Assert.AreEqual(CutsceneCatalog.Walrus, ids[1]);
            Assert.AreEqual(CutsceneCatalog.Waterfall, ids[2]);
        }

        [Test]
        public void CreateReturnsAFreshInstanceWithTheRequestedId()
        {
            foreach (var id in CutsceneCatalog.Ids)
            {
                Assert.IsTrue(CutsceneCatalog.TryCreate(id, out var first), id);
                Assert.IsTrue(CutsceneCatalog.TryCreate(id, out var second), id);

                Assert.AreEqual(id, first.Id);
                Assert.AreNotSame(first, second, id);
                Assert.IsFalse(first.IsRunning, id);
            }
        }

        [Test]
        public void UnknownIdIsRejected()
        {
            Assert.IsFalse(CutsceneCatalog.Contains("nope"));
            Assert.IsFalse(CutsceneCatalog.TryCreate("nope", out _));
            Assert.IsFalse(CutsceneCatalog.TryCreate(null, out _));
            Assert.IsFalse(CutsceneCatalog.TryCreate(string.Empty, out _));
        }

        [Test]
        public void EveryCutsceneDeclaresUniqueNonEmptyLineIds()
        {
            var cutscenes = new List<CutsceneBase>();
            CutsceneCatalog.CreateAll(cutscenes);

            Assert.AreEqual(5, cutscenes.Count);

            var seen = new HashSet<string>();

            for (var i = 0; i < cutscenes.Count; i++)
            {
                var cutscene = cutscenes[i];
                var lineIds = cutscene.LineIds;

                Assert.IsNotNull(lineIds, cutscene.Id);
                Assert.Greater(lineIds.Count, 0, cutscene.Id);

                if (Contains(CutsceneCatalog.SectionIds, cutscene.Id))
                    Assert.LessOrEqual(lineIds.Count, 5, cutscene.Id);

                for (var j = 0; j < lineIds.Count; j++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(lineIds[j]), cutscene.Id);
                    Assert.IsTrue(seen.Add(lineIds[j]), lineIds[j]);
                    StringAssert.StartsWith(cutscene.Id + ".", lineIds[j]);
                }
            }
        }

        [Test]
        public void LineCountsMatchTheStory()
        {
            AssertLineCount(CutsceneCatalog.Intro, 7);
            AssertLineCount(CutsceneCatalog.FishingLine, 4);
            AssertLineCount(CutsceneCatalog.Walrus, 4);
            AssertLineCount(CutsceneCatalog.Waterfall, 4);
            AssertLineCount(CutsceneCatalog.Ending, 5);
        }

        [Test]
        public void LineIdsAreNumberedInOrder()
        {
            var cutscenes = new List<CutsceneBase>();
            CutsceneCatalog.CreateAll(cutscenes);

            for (var i = 0; i < cutscenes.Count; i++)
            {
                var cutscene = cutscenes[i];
                var lineIds = cutscene.LineIds;

                for (var j = 0; j < lineIds.Count; j++)
                    Assert.AreEqual($"{cutscene.Id}.{j + 1:00}", lineIds[j]);
            }
        }

        private static void AssertLineCount(string cutsceneId, int expected)
        {
            Assert.IsTrue(CutsceneCatalog.TryCreate(cutsceneId, out var cutscene), cutsceneId);
            Assert.AreEqual(expected, cutscene.LineIds.Count, cutsceneId);
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (var i = 0; i < ids.Count; i++)
                if (ids[i] == id)
                    return true;

            return false;
        }
    }
}
