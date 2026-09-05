using NUnit.Framework;

namespace Game.Dialogue.Tests
{
    public sealed class DialogueServiceTests
    {
        private const string Csv = "id,speaker,text,auto_advance,cps\nintro.01,연이,…여기다.,,\nintro.02,연이,그래도 가야지.,1,45";

        [SetUp]
        public void SetUp()
        {
            DialogueService.Unload();
        }

        [TearDown]
        public void TearDown()
        {
            DialogueService.Unload();
        }

        [Test]
        public void IsNotLoadedBeforeLoad()
        {
            Assert.IsFalse(DialogueService.IsLoaded);
            Assert.AreEqual(0, DialogueService.Count);
            Assert.AreEqual(0, DialogueService.Warnings.Count);
        }

        [Test]
        public void LoadsFromText()
        {
            DialogueService.Load(Csv);

            Assert.IsTrue(DialogueService.IsLoaded);
            Assert.AreEqual(2, DialogueService.Count);
            Assert.IsTrue(DialogueService.TryGet("intro.02", out var line));
            Assert.AreEqual("그래도 가야지.", line.Text);
            Assert.AreEqual(1f, line.AutoAdvanceDelay, 0.0001f);
            Assert.AreEqual(45f, line.CharsPerSecond, 0.0001f);
        }

        [Test]
        public void ReturnsPlaceholderForMissingId()
        {
            DialogueService.Load(Csv);

            var line = DialogueService.Get("intro.99");

            Assert.AreEqual("[intro.99]", line.Text);
            Assert.AreEqual("intro.99", line.Id);
            Assert.AreEqual(string.Empty, line.Speaker);
            Assert.IsTrue(line.WaitsForAdvance);
        }

        [Test]
        public void ReturnsPlaceholderWhenNotLoaded()
        {
            var line = DialogueService.Get("intro.01");

            Assert.AreEqual("[intro.01]", line.Text);
            Assert.IsFalse(DialogueService.TryGet("intro.01", out _));
        }

        [Test]
        public void ReloadReplacesTheTable()
        {
            DialogueService.Load(Csv);
            DialogueService.Reload("id,text\nonly.01,하나");

            Assert.AreEqual(1, DialogueService.Count);
            Assert.IsFalse(DialogueService.TryGet("intro.01", out _));
            Assert.IsTrue(DialogueService.TryGet("only.01", out var line));
            Assert.AreEqual("하나", line.Text);
        }

        [Test]
        public void GetReturnsTheLoadedLine()
        {
            DialogueService.Load(Csv);

            Assert.AreEqual("…여기다.", DialogueService.Get("intro.01").Text);
        }
    }
}
