using System.Collections.Generic;
using System.Text;

namespace Game.Dialogue
{
    public static class CsvParser
    {
        private const char Bom = '\uFEFF';

        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();

            if (string.IsNullOrEmpty(text))
                return rows;

            var length = text.Length;
            var index = text[0] == Bom ? 1 : 0;

            var fields = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;
            var rowHasContent = false;

            while (index < length)
            {
                var c = text[index];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (index + 1 < length && text[index + 1] == '"')
                        {
                            cell.Append('"');
                            index += 2;
                            continue;
                        }

                        inQuotes = false;
                        index++;
                        continue;
                    }

                    cell.Append(c);
                    index++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    rowHasContent = true;
                    index++;
                    continue;
                }

                if (c == ',')
                {
                    fields.Add(cell.ToString());
                    cell.Length = 0;
                    rowHasContent = true;
                    index++;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && index + 1 < length && text[index + 1] == '\n')
                        index++;

                    index++;
                    EndRow(rows, fields, cell, ref rowHasContent);
                    continue;
                }

                cell.Append(c);

                if (!char.IsWhiteSpace(c))
                    rowHasContent = true;

                index++;
            }

            if (inQuotes)
                rowHasContent = true;

            EndRow(rows, fields, cell, ref rowHasContent);
            return rows;
        }

        private static void EndRow(List<string[]> rows, List<string> fields, StringBuilder cell, ref bool rowHasContent)
        {
            if (!rowHasContent)
            {
                fields.Clear();
                cell.Length = 0;
                return;
            }

            fields.Add(cell.ToString());
            cell.Length = 0;

            rows.Add(fields.ToArray());
            fields.Clear();
            rowHasContent = false;
        }
    }
}
