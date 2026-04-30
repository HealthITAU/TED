using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TED.Utils
{
    /// <summary>
    /// Provides utility functions related to system configurations.
    /// </summary>
    public class SystemUtilities
    {
        public static Screen GetPrimaryScreenOrThrow()
        {
            return Screen.PrimaryScreen ?? throw new InvalidOperationException("Could not find a primary screen - exiting...");
        }

        /// <summary>
        /// Get's the Rectangle of the Primary Screen in Virtual Screen space.
        /// </summary>
        /// <returns>The Rectangle of the Primary Screen in Virtual Screen space.</returns>
        public static Rectangle GetPrimaryScreenRect()
        {
            var primaryScreen = GetPrimaryScreenOrThrow();

            return new Rectangle(
               primaryScreen.Bounds.X - SystemInformation.VirtualScreen.Left,
               primaryScreen.Bounds.Y - SystemInformation.VirtualScreen.Top,
               primaryScreen.WorkingArea.Width,
               primaryScreen.WorkingArea.Height
           );
        }

        /// <summary>
        /// Gets the working area of the primary screen.
        /// </summary>
        /// <returns>The working area of the primary screen.</returns>
        public static Rectangle GetPrimaryScreenWorkingArea()
        {
            return GetPrimaryScreenOrThrow().WorkingArea;
        }

        /// <summary>
        /// Fetches the current wallpaper path from the system registry.
        /// </summary>
        /// <returns>The full path of the current desktop wallpaper.</returns>
        public static string GetWallpaperPathFromRegistry()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
            {
                return key?.GetValue("Wallpaper")?.ToString() ?? string.Empty;
            }
        }
    }
}
