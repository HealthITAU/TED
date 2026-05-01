using System;
using System.Threading;
using System.Windows.Forms;
using TED.DrawModes;
using TED.Utils;

namespace TED.Program
{
    /// <summary>
    /// Provides functionality for tagging windows in the system.
    /// </summary>
    internal class Tagger
    {
        /// <summary>
        /// Provides functionality for tagging windows in the system.
        /// </summary>
        internal Options Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tagger"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options to be used while tagging.</param>
        public Tagger(Options options)
        {
            Options = options;
        }

        /// <summary>
        /// Tags the window based on the given <see cref="Options"/>.
        /// </summary>
        public void Tag()
        {
            var desktopHost = Win32Native.GetDesktopHostWindow();
            if (desktopHost == IntPtr.Zero)
            {
                throw new InvalidOperationException("TED could not find the Windows desktop host window.");
            }

            CloseExistingOverlay(desktopHost);
            Application.Run(new DesktopOverlayForm(desktopHost, Options));
        }

        private static void CloseExistingOverlay(IntPtr desktopHost)
        {
            var existingOverlay = Win32Native.FindChildWindowByTitle(desktopHost, DesktopOverlayForm.OverlayWindowTitle);
            if (existingOverlay == IntPtr.Zero)
            {
                return;
            }

            Win32Native.CloseWindow(existingOverlay);

            for (var attempts = 0; attempts < 20; attempts++)
            {
                Thread.Sleep(100);
                if (Win32Native.FindChildWindowByTitle(desktopHost, DesktopOverlayForm.OverlayWindowTitle) == IntPtr.Zero)
                {
                    return;
                }
            }
        }
    }
}
