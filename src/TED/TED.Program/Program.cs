using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using TED.Utils;

namespace TED.Program
{
    /// <summary>
    /// The main class for the program.
    /// </summary>
    public class Program
    {

        private static Tagger? tagger;

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        public static void Main(string[] args)
        {
            tagger = new Tagger(ParseArgsIntoOptions(args));
            tagger.Tag();
        }

        /// <summary>
        /// Parses the command-line arguments into an Options object.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        /// <returns>An Options object that encapsulates application settings.</returns>
        private static Options ParseArgsIntoOptions(string[] args)
        {
            var fontName = GetArgument(args, new string[] { "-font", "-f" }, Options.Default.FontName);
            var imagePath = GetArgument(args, new string[] { "-image", "-i" }, Options.Default.ImagePath);
            var darkImagePath = GetArgument(args, new string[] { "-darkimage", "-di" }, Options.Default.DarkImagePath);
            var lightImagePath = GetArgument(args, new string[] { "-lightimage", "-li" }, Options.Default.LightImagePath);
            var alignment = GetArgument(args, new string[] { "-align", "-a" }, "left");
            var lines = Options.Default.Lines;

            if (!bool.TryParse(GetArgument(args, new string[] { "-debug", "-d" }, Options.Default.Debug.ToString()), out bool debug))
            {
                debug = Options.Default.Debug;
            }

            if (!int.TryParse(GetArgument(args, new string[] { "-fontsize", "-fs" }, Options.Default.FontSize.ToString()), out int fontSize))
            {
                fontSize = Options.Default.FontSize;
            }

            if (!int.TryParse(GetArgument(args, new string[] { "-linespacing", "-ls" }, Options.Default.LineSpacing.ToString()), out int margin))
            {
                margin = Options.Default.LineSpacing;
            }

            if (!int.TryParse(GetArgument(args, new string[] { "-hpad", "-hp" }, Options.Default.PaddingHorizontal.ToString()), out int paddingHorizontal))
            {
                paddingHorizontal = Options.Default.PaddingHorizontal;
            }

            if (!int.TryParse(GetArgument(args, new string[] { "-vpad", "-vp" }, Options.Default.PaddingHorizontal.ToString()), out int paddingVertical))
            {
                paddingVertical = Options.Default.PaddingVertical;
            }

            if (!int.TryParse(GetArgument(args, new string[] { "-width", "-w" }, Options.Default.FixedWidth.ToString()), out int fixedWidth))
            {
                fixedWidth = Options.Default.FixedWidth;
            }

            var alignmentOption = StringAlignment.Near;
            switch(alignment.ToLower())
            {
                case "left":
                    alignmentOption = StringAlignment.Near;
                    break;
                case "center":
                    alignmentOption = StringAlignment.Center;
                    break;
                case "right":
                    alignmentOption = StringAlignment.Far;
                    break;
                default:
                    alignmentOption = StringAlignment.Near;
                    break;
            }

            if (args.Any(arg => arg.Contains("-line")))
            {
                lines.Clear();
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-line")
                {

                    for (int j = i + 1; j < args.Length; j++)
                    {
                        if (args[j].StartsWith("-"))
                        {
                            break;
                        }

                        lines.Add(Tokenizer.ReplaceTokens(args[j]));
                    }
                }
            }

            imagePath = string.IsNullOrEmpty(imagePath) ? imagePath : EnsureImageExists(imagePath);
            lightImagePath = string.IsNullOrEmpty(lightImagePath) ? lightImagePath : EnsureImageExists(lightImagePath);
            darkImagePath = string.IsNullOrEmpty(darkImagePath) ? darkImagePath : EnsureImageExists(darkImagePath);

            return new Options(
                paddingHorizontal,
                paddingVertical,
                margin,
                fontSize,
                fontName,
                imagePath,
                lightImagePath,
                darkImagePath,
                lines,
                fixedWidth,
                alignmentOption,
                debug
                );
        }

        /// <summary>
        /// Validates the existence of the image file at the specified path.
        /// </summary>
        /// <param name="imagePath">Path of the image file.</param>
        /// <returns>
        /// The path of the image. If the image path points to a URL, the method attempts to download 
        /// the image, cache it, and then return the local path of the downloaded image.
        /// </returns>
        /// <exception cref="HttpRequestException">
        /// Thrown if the image path points to a URL and the image fails to download.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown if the image path does not point to a valid local file or URL.
        /// </exception>
        private static string EnsureImageExists(string imagePath)
        {
            if (!FileUtilities.PathIsLocalFile(imagePath))
            {
                if (FileUtilities.PathIsUrl(imagePath))
                {
                    var path = string.Empty;

                    try
                    {
                        path = FileUtilities.DownloadAndCacheFileAsync(imagePath).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        throw new HttpRequestException("ERROR: Failed to acquire image from provided URL");
                    }

                    return path;
                }
                else
                {
                    throw new FileNotFoundException("ERROR: Image filepath or URL is invalid.");
                }
            }

            return imagePath;
        }

        /// <summary>
        /// Retrieves the value of a specific command-line argument.
        /// </summary>
        /// <param name="args">A collection of command-line arguments.</param>
        /// <param name="options">An array of options to match against the command-line arguments.</param>
        /// <param name="defaultValue">The default value to return if the option is not found.</param>
        /// <returns>The value of the specified command-line argument, or the default value if the option is not found.</returns>
        private static string GetArgument(IEnumerable<string> args, string[] options, string defaultValue) =>
            args.SkipWhile(i => !options.Contains(i)).Skip(1).Take(1).DefaultIfEmpty(defaultValue).First();
    }
}

