using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;

namespace TED.Utils
{
    /// <summary>
    /// Provides utility functions related to system configurations.
    /// </summary>
    public class SystemUtilities
    {
        /// <summary>
        /// Get's the Rectangle of the Primary Screen in Virtual Screen space.
        /// </summary>
        /// <returns>The Rectangle of the Primary Screen in Virtual Screen space.</returns>
        public static Rectangle GetPrimaryScreenRect()
        {
            return new Rectangle(
               Screen.PrimaryScreen.Bounds.X - SystemInformation.VirtualScreen.Left,
               Screen.PrimaryScreen.Bounds.Y - SystemInformation.VirtualScreen.Top,
               Screen.PrimaryScreen.WorkingArea.Width,
               Screen.PrimaryScreen.WorkingArea.Height
           );
        }

        /// <summary>
        /// Fetches the current wallpaper path from the system registry.
        /// </summary>
        /// <returns>The full path of the current desktop wallpaper.</returns>
        public static string GetWallpaperPathFromRegistry()
        {
            string result = string.Empty;
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
            {
                if (key != null)
                {
                    result = key.GetValue("Wallpaper").ToString();
                    key.Close();
                }
            }

            return result;
        }
    }
}