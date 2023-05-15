using System;
using TED.Program;

namespace TED.DrawModes
{
    /// <summary>
    /// Serves as the base class for different drawing modes.
    /// </summary>
    internal abstract class DrawModeBase
    {
        // The device context to be drawn on
        protected IntPtr DeviceContext;

        // The options to be used when drawing
        protected readonly Options Options;

        /// <summary>
        /// Initializes a new instance of the DrawModeBase class with the specified device context and options.
        /// </summary>
        /// <param name="deviceContext">The device context to be drawn on.</param>
        /// <param name="options">The options to be used when drawing.</param>
        internal DrawModeBase(IntPtr deviceContext, Options options)
        {
            DeviceContext = deviceContext;
            Options = options;
        }

        /// <summary>
        /// When overridden in a derived class, performs the drawing action.
        /// </summary>
        public abstract void Draw();
    }
}
