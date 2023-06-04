using System.Collections.Generic;
using System.Drawing;
using TED.Utils;

namespace TED.Program
{
    /// <summary>
    /// Container for the various options utilized within TED.
    /// </summary>
    internal class Options
    {
        internal readonly int PaddingHorizontal;
        internal readonly int PaddingVertical;
        internal readonly int LineSpacing;
        internal readonly int FontSize;
        internal readonly string FontName;
        internal readonly string ImagePath;
        internal readonly string LightImagePath;
        internal readonly string DarkImagePath;
        internal readonly List<string> Lines;
        internal int FixedWidth;
        internal StringAlignment TextAlignment;
        internal readonly bool Debug;
        internal readonly bool AdaptiveImageMode;

        /// <summary>
        /// Gets default options
        /// </summary>
        private static Options? _default;
        internal static Options Default
        {
            get
            {
                _default ??= new Options(
                        10, 10, 8, 8, "Arial",
                        "", "", "",
                        new List<string>()
                        {
                         Tokenizer.ReplaceTokens("USERNAME: @userName"),
                         Tokenizer.ReplaceTokens("MACHINE NAME: @machineName"),
                         Tokenizer.ReplaceTokens("OS: @osName"),
                        },
                        -1,
                        StringAlignment.Near,
                        false);

                return _default;
            }
        }

        /// <summary>
        /// Initializes a new instance of the Options class with the specified settings.
        /// </summary>
        internal Options(int paddingHorizontal, int paddingVertical,
            int lineSpacing, int fontSize, string fontName,
            string imagePath, string lightImagePath,
            string darkImagePath, List<string> lines, int fixedWidth, StringAlignment textAlignment,
            bool debug)
        {
            PaddingHorizontal = paddingHorizontal;
            PaddingVertical = paddingVertical;
            LineSpacing = lineSpacing;
            FontSize = fontSize;
            FontName = fontName;
            ImagePath = imagePath;
            LightImagePath = lightImagePath;
            DarkImagePath = darkImagePath;
            Lines = lines;
            Debug = debug;
            FixedWidth = fixedWidth;
            TextAlignment = textAlignment;
            AdaptiveImageMode = !string.IsNullOrEmpty(LightImagePath) && !string.IsNullOrEmpty(DarkImagePath);
        }

        /// <summary>
        /// Determines the image path based on the wallpaper's luminance. 
        /// When in AdaptiveImageMode, chooses the light or dark image path based on the provided luminance.
        /// 
        /// Note: AdaptiveImageMode requires both a light image and dark image to be set. If only one is provided, the regular image
        /// path will be used as a fallback.
        /// </summary>
        /// <param name="wallpaperLuminance">The luminance of the wallpaper.</param>
        /// <returns>
        /// The appropriate image path considering the luminance and the AdaptiveImageMode.
        /// </returns>
        internal string GetImagePath(double wallpaperLuminance)
        {
            if (AdaptiveImageMode && !string.IsNullOrEmpty(LightImagePath) && !string.IsNullOrEmpty(DarkImagePath))
            {
                return wallpaperLuminance > 0.5 ? DarkImagePath : LightImagePath;
            }
            else
            {
                return ImagePath;
            }
        }
    }
}
