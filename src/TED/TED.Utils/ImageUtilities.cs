using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TED.Utils
{
    /// <summary>
    /// Provides utility methods for working with images
    /// </summary>
    public class ImageUtilities
    {
        /// <summary>
        /// Scales the image dimensions to fit within the specified maximum width and height, maintaining the original aspect ratio.
        /// </summary>
        /// <param name="srcWidth">The original image width.</param>
        /// <param name="srcHeight">The original image height.</param>
        /// <param name="maxWidth">The maximum allowable width for the scaled image.</param>
        /// <param name="maxHeight">The maximum allowable height for the scaled image.</param>
        /// <param name="destWidth">The calculated width for the scaled image.</param>
        /// <param name="destHeight">The calculated height for the scaled image.</param>
        public static void ScaleImageAndMaintainAspectRatio(int srcWidth, int srcHeight, float maxWidth, float maxHeight, out int destWidth, out int destHeight)
        {
            var ratioX = (double)maxWidth / srcWidth;
            var ratioY = (double)maxHeight / srcHeight;
            var ratio = Math.Min(ratioX, ratioY);

            destWidth = (int)(srcWidth * ratio);
            destHeight = (int)(srcHeight * ratio);
        }

        /// <summary>
        /// Calculates the perceived luminance of the current desktop wallpaper.
        /// </summary>
        /// <returns>A value representing the calculated perceived luminance of the wallpaper. Returns 0.0 if the wallpaper path is not found.</returns>
        public static double CalculateWallpaperLuminance()
        {
            // Get the wallpaper path from the registry
            string wallpaperPath = SystemUtilities.GetWallpaperPathFromRegistry();
            double luminance = 0.0;

            if (!string.IsNullOrEmpty(wallpaperPath))
            {
                using (var bmp = new Bitmap(wallpaperPath))
                {
                    luminance = CalculateImageLuminance01(bmp);
                }
            }

            return luminance;
        }

        /// <summary>
        /// Calculates the perceived luminance of a Bitmap image.
        /// </summary>
        /// <param name="bm">The Bitmap image to calculate the luminance for.</param>
        /// <returns>A value representing the calculated luminance of the image.</returns>
        public static double CalculateImageLuminance01(Bitmap bm)
        {
            var lum = 0.0;
            using var normalizedBitmap = new Bitmap(bm.Width, bm.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(normalizedBitmap))
            {
                graphics.DrawImage(bm, new Rectangle(0, 0, normalizedBitmap.Width, normalizedBitmap.Height));
            }

            var width = normalizedBitmap.Width;
            var height = normalizedBitmap.Height;
            var srcData = normalizedBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var stride = srcData.Stride;
            IntPtr scan0 = srcData.Scan0;

            // Luminance (standard, objective): (0.2126*R) + (0.7152*G) + (0.0722*B)
            // Luminance (perceived option 1): (0.299*R + 0.587*G + 0.114*B)
            // Luminance (perceived option 2, slower to calculate): sqrt( 0.299*R^2 + 0.587*G^2 + 0.114*B^2 )

            try
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var idx = y * stride + x * 4;
                        lum += (0.299 * Marshal.ReadByte(scan0, idx + 2) + 0.587 * Marshal.ReadByte(scan0, idx + 1) + 0.114 * Marshal.ReadByte(scan0, idx)) / 255.0;
                    }
                }
            }
            finally
            {
                normalizedBitmap.UnlockBits(srcData);
            }

            var normalized = lum / (width * height);
            return normalized;
        }
    }

}
