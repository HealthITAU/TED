using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TED.Program;
using TED.Utils;

namespace TED.DrawModes
{
    /// <summary>
    /// Represents a drawing mode that operates once and does not update.
    /// </summary>
    internal class OneTimeDrawMode : DrawModeBase
    {
        /// <summary>
        /// Initializes a new instance of the OneTimeDrawMode class with the specified device context and options.
        /// </summary>
        /// <param name="deviceContext">The device context to be drawn on.</param>
        /// <param name="options">The options to be used when drawing.</param>
        public OneTimeDrawMode(IntPtr deviceContext, Options options) : base(deviceContext, options)
        {
        }

        /// <summary>
        /// Draws the image and text on the given device context based on the options provided.
        /// </summary>
        public override void Draw()
        {
            // Calculate the luminance of the wallpaper
            var wallpaperLuminance = ImageUtilities.CalculateWallpaperLuminance();
            var primaryAreaRect = SystemUtilities.GetPrimaryScreenRect();

            if (DeviceContext != IntPtr.Zero)
            {
                using (var graphics = Graphics.FromHdc(DeviceContext))
                {
                    // Get the path of the image based on the luminance of the wallpaper
                    var imagePath = Options.GetImagePath(wallpaperLuminance);

                    // Set the text color based on the luminance of the wallpaper
                    var textColor = wallpaperLuminance > 0.5 ? Color.Black : Color.White;

                    using (var font = new Font(Options.FontName, Options.FontSize, FontStyle.Bold))
                    {
                        // Calculate the scaling factors
                        var scaleX = graphics.DpiX / 96.0f;
                        var scaleY = graphics.DpiY / 96.0f;

                        // Calculate the working area dimensions
                        var scaledWorkingAreaWidth = primaryAreaRect.X / scaleX;
                        var scaledWorkingAreaHeight = primaryAreaRect.Y / scaleY;

                        var maxWidth = 0f;
                        if(Options.FixedWidth > 0)
                        {
                            // Use the fixed width.
                            maxWidth = Options.FixedWidth;
                        }
                        else
                        {
                            // Calculate the maximum width based on the longest line.
                            maxWidth = Options.Lines.Select(line => new SizeF(graphics.MeasureString(line, font).Width, 0))
                                            .Max(size => size.Width);
                        }

                        // Calculate the positions of the text and the image
                        var textX = scaledWorkingAreaWidth + Screen.PrimaryScreen.WorkingArea.Width - maxWidth - Options.PaddingHorizontal;
                        var textY = scaledWorkingAreaHeight + Screen.PrimaryScreen.WorkingArea.Height - Options.Lines.Sum(line => graphics.MeasureString(line, font).Height) - (Options.LineSpacing * (Options.Lines.Count - 1)) - Options.PaddingVertical;

                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            using (var overlayImage = Image.FromFile(imagePath))
                            {
                                // Scale the image while maintaining its aspect ratio
                                ImageUtilities.ScaleImageAndMaintainAspectRatio(overlayImage.Width, overlayImage.Height, maxWidth, int.MaxValue, out int newWidth, out int newHeight);

                                var imageX = scaledWorkingAreaWidth + Screen.PrimaryScreen.WorkingArea.Width - newWidth - Options.PaddingHorizontal;
                                var imageY = scaledWorkingAreaHeight + Screen.PrimaryScreen.WorkingArea.Height - newHeight - Options.Lines.Sum(line => graphics.MeasureString(line, font).Height) - (Options.LineSpacing * (Options.Lines.Count)) - Options.PaddingVertical;

                                textX = imageX;
                                textY = imageY + newHeight + Options.LineSpacing;

                                // Draw the image
                                graphics.DrawImage(overlayImage, new RectangleF(imageX, imageY, newWidth, newHeight));
                            }
                        }

                        // Draw each line of text
                        for (var i = 0; i < Options.Lines.Count; i++)
                        {
                            var line = Options.Lines[i];

                            var format = new StringFormat() { Alignment = Options.TextAlignment };
                            var lineHeight = graphics.MeasureString(line, font).Height;
                            var textRect = new RectangleF(textX, textY, maxWidth, lineHeight);

                            // Draw the line
                            graphics.DrawString(line, font, new SolidBrush(textColor), textRect, format);

                            // Move the text cursor down to the next line
                            textY += graphics.MeasureString(line, font).Height;

                            // If there are more lines, add additional spacing
                            if (i < Options.Lines.Count)
                            {
                                textY += Options.LineSpacing;
                            }
                        }
                    }
                }
            }
        }
    }
}
