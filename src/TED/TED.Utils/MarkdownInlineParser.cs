using System.Collections.Generic;

namespace TED.Utils
{
    internal sealed class FormattedLine
    {
        internal FormattedLine(IReadOnlyList<TextRun> runs)
        {
            Runs = runs;
        }

        internal IReadOnlyList<TextRun> Runs { get; }
    }

    internal sealed class TextRun
    {
        internal TextRun(string text, bool bold, bool italic)
        {
            Text = text;
            Bold = bold;
            Italic = italic;
        }

        internal string Text { get; }
        internal bool Bold { get; }
        internal bool Italic { get; }
    }

    internal static class MarkdownInlineParser
    {
        internal static FormattedLine Parse(string input)
        {
            return new FormattedLine(ParseRuns(input, bold: false, italic: false));
        }

        private static List<TextRun> ParseRuns(string input, bool bold, bool italic)
        {
            var runs = new List<TextRun>();
            var plainText = string.Empty;

            for (var i = 0; i < input.Length;)
            {
                var marker = GetMarker(input, i);
                if (marker == null)
                {
                    plainText += input[i];
                    i++;
                    continue;
                }

                if (!IsOpeningMarker(input, i, marker))
                {
                    plainText += marker;
                    i += marker.Length;
                    continue;
                }

                var endIndex = FindClosingMarker(input, marker, i + marker.Length);
                if (endIndex < 0)
                {
                    plainText += marker;
                    i += marker.Length;
                    continue;
                }

                AddRun(runs, plainText, bold, italic);
                plainText = string.Empty;

                var innerText = input.Substring(i + marker.Length, endIndex - i - marker.Length);
                var innerRuns = ParseRuns(
                    innerText,
                    bold || marker.Length >= 2,
                    italic || marker.Length == 1 || marker.Length == 3);
                runs.AddRange(innerRuns);

                i = endIndex + marker.Length;
            }

            AddRun(runs, plainText, bold, italic);

            if (runs.Count == 0)
            {
                runs.Add(new TextRun(string.Empty, bold, italic));
            }

            return runs;
        }

        private static string? GetMarker(string input, int index)
        {
            if (index + 2 < input.Length)
            {
                var threeCharacterMarker = input.Substring(index, 3);
                if (threeCharacterMarker == "***" || threeCharacterMarker == "___")
                {
                    return threeCharacterMarker;
                }
            }

            if (index + 1 < input.Length)
            {
                var twoCharacterMarker = input.Substring(index, 2);
                if (twoCharacterMarker == "**" || twoCharacterMarker == "__")
                {
                    return twoCharacterMarker;
                }
            }

            var oneCharacterMarker = input[index].ToString();
            if (oneCharacterMarker == "*" || oneCharacterMarker == "_")
            {
                return oneCharacterMarker;
            }

            return null;
        }

        private static int FindClosingMarker(string input, string marker, int startIndex)
        {
            for (var i = startIndex; i < input.Length;)
            {
                var endIndex = input.IndexOf(marker, i);
                if (endIndex < 0)
                {
                    return -1;
                }

                if (endIndex > startIndex && IsClosingMarker(input, endIndex, marker))
                {
                    return endIndex;
                }

                i = endIndex + marker.Length;
            }

            return -1;
        }

        private static bool IsOpeningMarker(string input, int index, string marker)
        {
            if (index + marker.Length >= input.Length)
            {
                return false;
            }

            return !IsWordCharacter(GetCharacterBefore(input, index))
                && !char.IsWhiteSpace(input[index + marker.Length]);
        }

        private static bool IsClosingMarker(string input, int index, string marker)
        {
            return !char.IsWhiteSpace(input[index - 1])
                && !IsWordCharacter(GetCharacterAfter(input, index + marker.Length));
        }

        private static char? GetCharacterBefore(string input, int index)
        {
            return index > 0 ? input[index - 1] : null;
        }

        private static char? GetCharacterAfter(string input, int index)
        {
            return index < input.Length ? input[index] : null;
        }

        private static bool IsWordCharacter(char? character)
        {
            return character.HasValue && (char.IsLetterOrDigit(character.Value) || character.Value == '_');
        }

        private static void AddRun(List<TextRun> runs, string text, bool bold, bool italic)
        {
            if (text.Length == 0)
            {
                return;
            }

            runs.Add(new TextRun(text, bold, italic));
        }
    }
}
