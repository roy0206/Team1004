using NUnit.Framework;

namespace Game.Dialogue.Tests
{
    public sealed class CsvParserTests
    {
        [Test]
        public void ParsesPlainRows()
        {
            var rows = CsvParser.Parse("a,b,c\n1,2,3");

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, rows[1]);
        }

        [Test]
        public void ReturnsEmptyForNullOrEmpty()
        {
            Assert.AreEqual(0, CsvParser.Parse(null).Count);
            Assert.AreEqual(0, CsvParser.Parse(string.Empty).Count);
        }

        [Test]
        public void HandlesCrLf()
        {
            var rows = CsvParser.Parse("a,b\r\n1,2\r\n");

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "1", "2" }, rows[1]);
        }

        [Test]
        public void SkipsBom()
        {
            var rows = CsvParser.Parse("\uFEFFid,text\nx,y");

            Assert.AreEqual("id", rows[0][0]);
        }

        [Test]
        public void SkipsBlankLines()
        {
            var rows = CsvParser.Parse("a,b\n\n\r\n   \n1,2\n");

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "1", "2" }, rows[1]);
        }

        [Test]
        public void KeepsCommaInsideQuotes()
        {
            var rows = CsvParser.Parse("a,b\n\"x, y\",z");

            CollectionAssert.AreEqual(new[] { "x, y", "z" }, rows[1]);
        }

        [Test]
        public void KeepsNewLineInsideQuotes()
        {
            var rows = CsvParser.Parse("a,b\n\"first\nsecond\",z");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("first\nsecond", rows[1][0]);
        }

        [Test]
        public void KeepsCrLfInsideQuotesAsIs()
        {
            var rows = CsvParser.Parse("a,b\r\n\"first\r\nsecond\",z\r\n");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("first\r\nsecond", rows[1][0]);
        }

        [Test]
        public void UnescapesDoubledQuotes()
        {
            var rows = CsvParser.Parse("a\n\"say \"\"hi\"\" now\"");

            Assert.AreEqual("say \"hi\" now", rows[1][0]);
        }

        [Test]
        public void KeepsQuotedEmptyCell()
        {
            var rows = CsvParser.Parse("a,b\n\"\",z");

            Assert.AreEqual(string.Empty, rows[1][0]);
            Assert.AreEqual("z", rows[1][1]);
        }

        [Test]
        public void KeepsTrailingEmptyCells()
        {
            var rows = CsvParser.Parse("a,b,c\n1,,");

            CollectionAssert.AreEqual(new[] { "1", string.Empty, string.Empty }, rows[1]);
        }

        [Test]
        public void KeepsLastRowWithoutTrailingNewLine()
        {
            var rows = CsvParser.Parse("a\nb");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("b", rows[1][0]);
        }
    }
}
