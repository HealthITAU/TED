using System;
using System.Collections.Generic;
using System.Drawing;

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
        internal TextRun(string text, bool bold, bool italic, bool underline, Color? color)
        {
            Text = text;
            Bold = bold;
            Italic = italic;
            Underline = underline;
            Color = color;
        }

        internal string Text { get; }
        internal bool Bold { get; }
        internal bool Italic { get; }
        internal bool Underline { get; }
        internal Color? Color { get; }
    }

    internal static class RichTextInlineParser
    {
        internal static FormattedLine Parse(string input)
        {
            var runs = new List<TextRun>();
            var text = string.Empty;
            var style = TextStyle.Default;
            var styleStack = new Stack<StyleFrame>();

            for (var i = 0; i < input.Length;)
            {
                if (input[i] != '<')
                {
                    text += input[i];
                    i++;
                    continue;
                }

                var tagEnd = input.IndexOf('>', i + 1);
                if (tagEnd < 0)
                {
                    text += input[i];
                    i++;
                    continue;
                }

                var tagText = input.Substring(i + 1, tagEnd - i - 1).Trim();
                if (TryGetOpeningTag(tagText, out var openingTag, out var newStyle))
                {
                    if (!HasClosingTag(input, tagEnd + 1, openingTag))
                    {
                        text += input.Substring(i, tagEnd - i + 1);
                        i = tagEnd + 1;
                        continue;
                    }

                    AddRun(runs, text, style);
                    text = string.Empty;
                    styleStack.Push(new StyleFrame(openingTag, style));
                    style = newStyle(style);
                    i = tagEnd + 1;
                    continue;
                }

                if (TryGetClosingTag(tagText, out var closingTag) && CanCloseTag(styleStack, closingTag))
                {
                    AddRun(runs, text, style);
                    text = string.Empty;
                    style = styleStack.Pop().PreviousStyle;
                    i = tagEnd + 1;
                    continue;
                }

                text += input.Substring(i, tagEnd - i + 1);
                i = tagEnd + 1;
            }

            AddRun(runs, text, style);

            if (runs.Count == 0)
            {
                runs.Add(new TextRun(string.Empty, false, false, false, null));
            }

            return new FormattedLine(runs);
        }

        private static bool TryGetOpeningTag(string tagText, out string tagName, out Func<TextStyle, TextStyle> applyStyle)
        {
            tagName = string.Empty;
            applyStyle = style => style;
            var normalizedTag = tagText.ToLowerInvariant();

            switch (normalizedTag)
            {
                case "b":
                    tagName = "b";
                    applyStyle = style => style with { Bold = true };
                    return true;
                case "i":
                    tagName = "i";
                    applyStyle = style => style with { Italic = true };
                    return true;
                case "u":
                    tagName = "u";
                    applyStyle = style => style with { Underline = true };
                    return true;
            }

            if (normalizedTag.StartsWith("color="))
            {
                var colorValue = tagText.Substring("color=".Length).Trim().Trim('"', '\'');
                if (TryParseColor(colorValue, out var color))
                {
                    tagName = "color";
                    applyStyle = style => style with { Color = color };
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetClosingTag(string tagText, out string tagName)
        {
            tagName = string.Empty;

            if (!tagText.StartsWith("/"))
            {
                return false;
            }

            var normalizedTag = tagText.Substring(1).Trim().ToLowerInvariant();
            if (normalizedTag == "b" || normalizedTag == "i" || normalizedTag == "u" || normalizedTag == "color")
            {
                tagName = normalizedTag;
                return true;
            }

            return false;
        }

        private static bool CanCloseTag(Stack<StyleFrame> styleStack, string tagName)
        {
            return styleStack.Count > 0 && styleStack.Peek().TagName == tagName;
        }

        private static bool HasClosingTag(string input, int startIndex, string tagName)
        {
            return input.IndexOf("</" + tagName + ">", startIndex, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryParseColor(string colorValue, out Color color)
        {
            try
            {
                color = ColorTranslator.FromHtml(colorValue);
                return color.A > 0;
            }
            catch
            {
                color = Color.Empty;
                return false;
            }
        }

        private static void AddRun(List<TextRun> runs, string text, TextStyle style)
        {
            if (text.Length == 0)
            {
                return;
            }

            runs.Add(new TextRun(text, style.Bold, style.Italic, style.Underline, style.Color));
        }

        private readonly record struct TextStyle(bool Bold, bool Italic, bool Underline, Color? Color)
        {
            internal static TextStyle Default => new TextStyle(false, false, false, null);
        }

        private readonly record struct StyleFrame(string TagName, TextStyle PreviousStyle);
    }
}
