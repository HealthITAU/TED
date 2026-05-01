using System;
using System.Drawing;
using System.Linq;
using TED.Program;
using TED.Utils;

namespace TED.DrawModes
{
    /// <summary>
    /// Draws the TED desktop tag onto an existing graphics surface.
    /// </summary>
    internal static class DesktopTagRenderer
    {
        internal static void Draw(Graphics graphics, Options options)
        {
            var wallpaperLuminance = ImageUtilities.CalculateWallpaperLuminance();
            var primaryAreaRect = SystemUtilities.GetPrimaryScreenRect();
            var primaryWorkingArea = SystemUtilities.GetPrimaryScreenWorkingArea();
            var imagePath = options.GetImagePath(wallpaperLuminance);
            var textColor = wallpaperLuminance > 0.5 ? Color.Black : Color.White;

            using (var font = new Font(options.FontName, options.FontSize, FontStyle.Regular))
            {
                var formattedLines = options.Lines.Select(RichTextInlineParser.Parse).ToList();
                var scaleX = graphics.DpiX / 96.0f;
                var scaleY = graphics.DpiY / 96.0f;
                var scaledWorkingAreaWidth = primaryAreaRect.X / scaleX;
                var scaledWorkingAreaHeight = primaryAreaRect.Y / scaleY;

                float maxWidth;
                if (options.FixedWidth > 0)
                {
                    maxWidth = options.FixedWidth;
                }
                else
                {
                    maxWidth = formattedLines.Select(line => MeasureFormattedLine(graphics, line, font).Width)
                                             .DefaultIfEmpty(0)
                                             .Max();
                }

                var lineHeights = formattedLines.Select(line => MeasureFormattedLine(graphics, line, font).Height).ToList();
                var textX = scaledWorkingAreaWidth + primaryWorkingArea.Width - maxWidth - options.PaddingHorizontal;
                var textY = scaledWorkingAreaHeight + primaryWorkingArea.Height - lineHeights.Sum() - (options.LineSpacing * (options.Lines.Count - 1)) - options.PaddingVertical;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    using (var overlayImage = Image.FromFile(imagePath))
                    {
                        ImageUtilities.ScaleImageAndMaintainAspectRatio(overlayImage.Width, overlayImage.Height, maxWidth, int.MaxValue, out int newWidth, out int newHeight);

                        var imageX = scaledWorkingAreaWidth + primaryWorkingArea.Width - newWidth - options.PaddingHorizontal;
                        var imageY = scaledWorkingAreaHeight + primaryWorkingArea.Height - newHeight - lineHeights.Sum() - (options.LineSpacing * options.Lines.Count) - options.PaddingVertical;

                        textX = imageX;
                        textY = imageY + newHeight + options.LineSpacing;

                        graphics.DrawImage(overlayImage, new RectangleF(imageX, imageY, newWidth, newHeight));
                    }
                }

                for (var i = 0; i < options.Lines.Count; i++)
                {
                    DrawFormattedLine(graphics, formattedLines[i], font, textColor, textX, textY, maxWidth, options.TextAlignment);

                    textY += lineHeights[i];

                    if (i < options.Lines.Count)
                    {
                        textY += options.LineSpacing;
                    }
                }
            }
        }

        private static SizeF MeasureFormattedLine(Graphics graphics, FormattedLine line, Font baseFont)
        {
            var width = 0f;
            var height = graphics.MeasureString(string.Empty, baseFont, int.MaxValue, StringFormat.GenericTypographic).Height;

            foreach (var run in line.Runs)
            {
                using (var runFont = CreateRunFont(baseFont, run))
                using (var format = CreateStringFormat())
                {
                    var size = graphics.MeasureString(run.Text, runFont, int.MaxValue, format);
                    width += size.Width;
                    height = Math.Max(height, size.Height);
                }
            }

            return new SizeF(width, height);
        }

        private static void DrawFormattedLine(Graphics graphics, FormattedLine line, Font baseFont, Color defaultTextColor, float x, float y, float maxWidth, StringAlignment alignment)
        {
            var lineSize = MeasureFormattedLine(graphics, line, baseFont);
            var runX = x + GetAlignedOffset(maxWidth, lineSize.Width, alignment);

            foreach (var run in line.Runs)
            {
                using (var runFont = CreateRunFont(baseFont, run))
                using (var brush = new SolidBrush(run.Color ?? defaultTextColor))
                using (var format = CreateStringFormat())
                {
                    graphics.DrawString(run.Text, runFont, brush, new PointF(runX, y), format);
                    runX += graphics.MeasureString(run.Text, runFont, int.MaxValue, format).Width;
                }
            }
        }

        private static Font CreateRunFont(Font baseFont, TextRun run)
        {
            var style = baseFont.Style;
            if (run.Bold)
            {
                style |= FontStyle.Bold;
            }

            if (run.Italic)
            {
                style |= FontStyle.Italic;
            }

            if (run.Underline)
            {
                style |= FontStyle.Underline;
            }

            return new Font(baseFont.FontFamily, baseFont.Size, style, baseFont.Unit);
        }

        private static float GetAlignedOffset(float maxWidth, float lineWidth, StringAlignment alignment)
        {
            switch (alignment)
            {
                case StringAlignment.Center:
                    return (maxWidth - lineWidth) / 2;
                case StringAlignment.Far:
                    return maxWidth - lineWidth;
                default:
                    return 0;
            }
        }

        private static StringFormat CreateStringFormat()
        {
            var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            return format;
        }
    }
}
