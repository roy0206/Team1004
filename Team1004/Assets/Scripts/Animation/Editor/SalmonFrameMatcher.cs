using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Animation.Editor
{
    public static class SalmonFrameMatcher
    {
        public const string Extension = ".png";

        public static string StripExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - Extension.Length)
                : fileName;
        }

        public static bool TryGetFrameIndex(string framePrefix, string fileName, out int index)
        {
            index = 0;

            if (string.IsNullOrEmpty(framePrefix) || string.IsNullOrEmpty(fileName))
                return false;

            var name = Normalize(StripExtension(fileName)).Trim();
            var prefix = Normalize(framePrefix).Trim();

            if (prefix.Length == 0 || name.Length <= prefix.Length)
                return false;

            if (!name.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var cursor = SkipWhitespace(name, prefix.Length);

            if (cursor < name.Length && name[cursor] == '-')
                cursor = SkipWhitespace(name, cursor + 1);

            if (cursor >= name.Length)
                return false;

            var value = 0;

            for (; cursor < name.Length; cursor++)
            {
                var digit = name[cursor];

                if (digit < '0' || digit > '9')
                    return false;

                value = value * 10 + (digit - '0');
            }

            if (value <= 0)
                return false;

            index = value;
            return true;
        }

        public static bool IsSingleFrame(string framePrefix, string fileName)
        {
            if (string.IsNullOrEmpty(framePrefix) || string.IsNullOrEmpty(fileName))
                return false;

            var name = Normalize(StripExtension(fileName)).Trim();
            var prefix = Normalize(framePrefix).Trim();

            return prefix.Length > 0 && string.Equals(name, prefix, StringComparison.Ordinal);
        }

        public static string FindSingleFrame(IReadOnlyList<string> fileNames, string framePrefix)
        {
            if (fileNames == null)
                return null;

            for (var i = 0; i < fileNames.Count; i++)
            {
                if (IsSingleFrame(framePrefix, fileNames[i]))
                    return fileNames[i];
            }

            return null;
        }

        public static string FindFrame(IReadOnlyList<string> fileNames, string framePrefix, int index)
        {
            if (fileNames == null || index <= 0)
                return null;

            for (var i = 0; i < fileNames.Count; i++)
            {
                if (TryGetFrameIndex(framePrefix, fileNames[i], out var found) && found == index)
                    return fileNames[i];
            }

            return null;
        }

        private static int SkipWhitespace(string value, int start)
        {
            var cursor = start;

            while (cursor < value.Length && char.IsWhiteSpace(value[cursor]))
                cursor++;

            return cursor;
        }

        private static string Normalize(string value)
        {
            try
            {
                return value.IsNormalized(NormalizationForm.FormC) ? value : value.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
                return value;
            }
        }
    }
}
