using NUnit.Framework;

namespace Game.Dialogue.Tests
{
    public sealed class DialogueTableTests
    {
        private const string Header = "id,speaker,text,auto_advance,cps";

        [Test]
        public void ReadsColumnsByName()
        {
            var table = DialogueTable.Parse(Header + "\nintro.01,연이,안녕.,1.5,20");

            Assert.AreEqual(1, table.Count);
            Assert.IsTrue(table.TryGet("intro.01", out var line));
            Assert.AreEqual("연이", line.Speaker);
            Assert.AreEqual("안녕.", line.Text);
            Assert.AreEqual(1.5f, line.AutoAdvanceDelay, 0.0001f);
            Assert.AreEqual(20f, line.CharsPerSecond, 0.0001f);
        }

        [Test]
        public void IgnoresColumnOrder()
        {
            var table = DialogueTable.Parse("cps,text,id,auto_advance,speaker\n40,본문,a.01,2,화자");

            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual("화자", line.Speaker);
            Assert.AreEqual("본문", line.Text);
            Assert.AreEqual(2f, line.AutoAdvanceDelay, 0.0001f);
            Assert.AreEqual(40f, line.CharsPerSecond, 0.0001f);
        }

        [Test]
        public void IgnoresHeaderCaseAndSeparators()
        {
            var table = DialogueTable.Parse("ID, Speaker ,TEXT,Auto Advance,CPS\na.01,s,t,,");

            Assert.AreEqual(1, table.Count);
            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual("s", line.Speaker);
        }

        [Test]
        public void UsesDefaultsForEmptyAutoAndCps()
        {
            var table = DialogueTable.Parse(Header + "\na.01,s,t,,");

            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual(DialogueLine.WaitForAdvance, line.AutoAdvanceDelay, 0.0001f);
            Assert.IsTrue(line.WaitsForAdvance);
            Assert.AreEqual(0f, line.CharsPerSecond, 0.0001f);
        }

        [Test]
        public void UsesDefaultsForMissingColumns()
        {
            var table = DialogueTable.Parse("id,text\na.01,t");

            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual(string.Empty, line.Speaker);
            Assert.AreEqual(DialogueLine.WaitForAdvance, line.AutoAdvanceDelay, 0.0001f);
            Assert.AreEqual(0f, line.CharsPerSecond, 0.0001f);
            Assert.AreEqual(0, table.Warnings.Count);
        }

        [Test]
        public void KeepsLastDuplicateAndWarns()
        {
            var table = DialogueTable.Parse(Header + "\na.01,s,first,,\na.01,s,second,,");

            Assert.AreEqual(1, table.Count);
            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual("second", line.Text);
            Assert.AreEqual(1, table.Warnings.Count);
            StringAssert.Contains("a.01", table.Warnings[0]);
        }

        [Test]
        public void WarnsOnMissingIdColumn()
        {
            var table = DialogueTable.Parse("key,text\na.01,t");

            Assert.AreEqual(0, table.Count);
            Assert.AreEqual(1, table.Warnings.Count);
        }

        [Test]
        public void WarnsOnEmptyCsv()
        {
            var table = DialogueTable.Parse(string.Empty);

            Assert.AreEqual(0, table.Count);
            Assert.AreEqual(1, table.Warnings.Count);
        }

        [Test]
        public void SkipsRowsWithoutId()
        {
            var table = DialogueTable.Parse(Header + "\n,s,t,,\na.01,s,t,,");

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1, table.Warnings.Count);
        }

        [Test]
        public void WarnsOnUnparsableNumber()
        {
            var table = DialogueTable.Parse(Header + "\na.01,s,t,빠름,");

            Assert.IsTrue(table.TryGet("a.01", out var line));
            Assert.AreEqual(DialogueLine.WaitForAdvance, line.AutoAdvanceDelay, 0.0001f);
            Assert.AreEqual(1, table.Warnings.Count);
        }

        [Test]
        public void KeepsQuotedTextWithCommaAndNewLine()
        {
            var table = DialogueTable.Parse(Header + "\nending.04,,\"처음 품은 뜻을, 끝까지.\n初志一貫\",,");

            Assert.IsTrue(table.TryGet("ending.04", out var line));
            Assert.AreEqual("처음 품은 뜻을, 끝까지.\n初志一貫", line.Text);
            Assert.AreEqual(string.Empty, line.Speaker);
        }

        [Test]
        public void TryGetReturnsFalseForUnknownId()
        {
            var table = DialogueTable.Parse(Header + "\na.01,s,t,,");

            Assert.IsFalse(table.TryGet("nope", out var line));
            Assert.IsNull(line);
            Assert.IsFalse(table.TryGet(null, out _));
        }
    }
}
