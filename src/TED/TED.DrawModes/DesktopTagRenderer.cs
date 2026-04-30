using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
            var imagePath = options.GetImagePath(wallpaperLuminance);
            var textColor = wallpaperLuminance > 0.5 ? Color.Black : Color.White;

            using (var font = new Font(options.FontName, options.FontSize, FontStyle.Bold))
            {
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
                    maxWidth = options.Lines.Select(line => new SizeF(graphics.MeasureString(line, font).Width, 0))
                                            .Max(size => size.Width);
                }

                var lineHeights = options.Lines.Select(line => graphics.MeasureString(line, font).Height).ToList();
                var textX = scaledWorkingAreaWidth + Screen.PrimaryScreen.WorkingArea.Width - maxWidth - options.PaddingHorizontal;
                var textY = scaledWorkingAreaHeight + Screen.PrimaryScreen.WorkingArea.Height - lineHeights.Sum() - (options.LineSpacing * (options.Lines.Count - 1)) - options.PaddingVertical;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    using (var overlayImage = Image.FromFile(imagePath))
                    {
                        ImageUtilities.ScaleImageAndMaintainAspectRatio(overlayImage.Width, overlayImage.Height, maxWidth, int.MaxValue, out int newWidth, out int newHeight);

                        var imageX = scaledWorkingAreaWidth + Screen.PrimaryScreen.WorkingArea.Width - newWidth - options.PaddingHorizontal;
                        var imageY = scaledWorkingAreaHeight + Screen.PrimaryScreen.WorkingArea.Height - newHeight - lineHeights.Sum() - (options.LineSpacing * options.Lines.Count) - options.PaddingVertical;

                        textX = imageX;
                        textY = imageY + newHeight + options.LineSpacing;

                        graphics.DrawImage(overlayImage, new RectangleF(imageX, imageY, newWidth, newHeight));
                    }
                }

                for (var i = 0; i < options.Lines.Count; i++)
                {
                    var line = options.Lines[i];

                    using (var brush = new SolidBrush(textColor))
                    using (var format = new StringFormat() { Alignment = options.TextAlignment })
                    {
                        var textRect = new RectangleF(textX, textY, maxWidth, lineHeights[i]);
                        graphics.DrawString(line, font, brush, textRect, format);
                    }

                    textY += lineHeights[i];

                    if (i < options.Lines.Count)
                    {
                        textY += options.LineSpacing;
                    }
                }
            }
        }
    }
}
