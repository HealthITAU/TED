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
            var progman = Win32Native.GetProgmanWindow();
            Win32Native.ClearWindow(progman);
            var workerWindow = Win32Native.CreateWorkerW(progman);
            var deviceContext = Win32Native.GetWorkerWindowDeviceContext(workerWindow);
            DrawModeBase drawer = new OneTimeDrawMode(deviceContext, Options);
            drawer.Draw();
            Win32Native.ReleaseWorkerWindowDeviceContext(workerWindow, deviceContext);
        }
    }
}
