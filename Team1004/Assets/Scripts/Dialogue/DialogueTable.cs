using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Dialogue
{
    public sealed class DialogueTable
    {
        public const string IdColumn = "id";
        public const string SpeakerColumn = "speaker";
        public const string TextColumn = "text";
        public const string AutoAdvanceColumn = "auto_advance";
        public const string CharsPerSecondColumn = "cps";

        private static readonly string[] EmptyRow = Array.Empty<string>();

        private readonly Dictionary<string, DialogueLine> lines = new(StringComparer.Ordinal);
        private readonly List<string> warnings = new();

        private DialogueTable()
        {
        }

        public int Count => lines.Count;
        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyCollection<string> Ids => lines.Keys;

        public bool TryGet(string id, out DialogueLine line)
        {
            if (string.IsNullOrEmpty(id))
            {
                line = null;
                return false;
            }

            return lines.TryGetValue(id, out line);
        }

        public static DialogueTable Parse(string csvText)
        {
            var table = new DialogueTable();
            var rows = CsvParser.Parse(csvText);

            if (rows.Count == 0)
            {
                table.warnings.Add("CSV가 비어 있다. 헤더 행이 필요하다.");
                return table;
            }

            var header = rows[0];
            var idIndex = FindColumn(header, IdColumn);
            var speakerIndex = FindColumn(header, SpeakerColumn);
            var textIndex = FindColumn(header, TextColumn);
            var autoIndex = FindColumn(header, AutoAdvanceColumn);
            var cpsIndex = FindColumn(header, CharsPerSecondColumn);

            if (idIndex < 0)
                table.warnings.Add($"'{IdColumn}' 열이 없다. 헤더: {string.Join(" | ", header)}");

            if (textIndex < 0)
                table.warnings.Add($"'{TextColumn}' 열이 없다. 헤더: {string.Join(" | ", header)}");

            if (idIndex < 0 || textIndex < 0)
                return table;

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var id = Cell(row, idIndex).Trim();

                if (id.Length == 0)
                {
                    table.warnings.Add($"{i + 1}행: id가 비어 있어 건너뛴다.");
                    continue;
                }

                var speaker = Cell(row, speakerIndex);
                var text = Cell(row, textIndex);
                var auto = table.ReadFloat(Cell(row, autoIndex), DialogueLine.WaitForAdvance, i + 1, AutoAdvanceColumn);
                var cps = table.ReadFloat(Cell(row, cpsIndex), DialogueLine.DefaultCharsPerSecond, i + 1, CharsPerSecondColumn);

                if (table.lines.ContainsKey(id))
                    table.warnings.Add($"{i + 1}행: id '{id}'가 중복이다. 마지막 행을 쓴다.");

                table.lines[id] = new DialogueLine(id, speaker, text, auto, cps);
            }

            return table;
        }

        private float ReadFloat(string value, float fallback, int line, string column)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
                return fallback;

            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            warnings.Add($"{line}행: '{column}' 값 '{trimmed}'을 숫자로 읽지 못해 {fallback}을 쓴다.");
            return fallback;
        }

        private static string Cell(string[] row, int index)
        {
            if (row == null)
                row = EmptyRow;

            if (index < 0 || index >= row.Length)
                return string.Empty;

            return row[index] ?? string.Empty;
        }

        private static int FindColumn(string[] header, string name)
        {
            var target = Normalize(name);

            for (var i = 0; i < header.Length; i++)
                if (Normalize(header[i]) == target)
                    return i;

            return -1;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var buffer = new char[value.Length];
            var length = 0;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '_' || c == '-' || char.IsWhiteSpace(c))
                    continue;

                buffer[length++] = char.ToLowerInvariant(c);
            }

            return new string(buffer, 0, length);
        }
    }
}
