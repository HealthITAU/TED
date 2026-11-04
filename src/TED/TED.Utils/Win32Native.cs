using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TED.Utils
{
    /// <summary>
    /// The information and methods stored within this class are largely attributed to <see href="https://www.pinvoke.net/"/>
    /// It has been a fantastic resource for tinkering with and learning how native methods work.
    /// </summary>
    public class Win32Native
    {
        private const int GWL_STYLE = -16;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint WM_SPAWN_WORKERW = 0x052C;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        public const uint WM_CLOSE = 0x0010;
        public const uint WM_SETTINGCHANGE = 0x001A;
        public const uint WM_DISPLAYCHANGE = 0x007E;
        public const uint WM_THEMECHANGED = 0x031A;
        public const uint WM_DWMCOMPOSITIONCHANGED = 0x031E;

        public const int WS_CHILD = 0x40000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        // Windows 11 build 26100+: honour the alpha channel of the window's redirection bitmap.
        public const int DWMWA_REDIRECTIONBITMAP_ALPHA = 39;
        public const uint LWA_COLORKEY = 0x00000001;
        public const uint LWA_ALPHA = 0x00000002;
        private const uint SRCCOPY = 0x00CC0020;

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

        /// <summary>
        /// Copies a 32bpp ARGB bitmap onto a device context as premultiplied BGRA, alpha channel included
        /// (BitBlt SRCCOPY copies all 32 bits; no blending happens here).
        /// </summary>
        public static void BlitPremultiplied(IntPtr hdcDest, Bitmap bitmap)
        {
            var memoryDc = CreateCompatibleDC(hdcDest);
            // GetHbitmap with a fully transparent background produces a premultiplied 32bpp DIB.
            var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            var previous = SelectObject(memoryDc, hBitmap);
            try
            {
                BitBlt(hdcDest, 0, 0, bitmap.Width, bitmap.Height, memoryDc, 0, 0, SRCCOPY);
            }
            finally
            {
                SelectObject(memoryDc, previous);
                DeleteObject(hBitmap);
                DeleteDC(memoryDc);
            }
        }

        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
            SMTO_ERRORONEXIT = 0x20
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, IntPtr windowTitle);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowExByTitle(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr childHandle, IntPtr newParentHandle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr windowHandle);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        public static IntPtr GetProgmanWindow()
        {
            return FindWindow("Progman", null);
        }

        public static IntPtr GetDesktopHostWindow()
        {
            var progman = GetProgmanWindow();
            if (progman == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            CreateWorkerW(progman);

            var progmanChildWorker = FindWindowEx(progman, IntPtr.Zero, "WorkerW", IntPtr.Zero);
            if (progmanChildWorker != IntPtr.Zero)
            {
                return progmanChildWorker;
            }

            var topLevelWorker = FindTopLevelWorkerWBehindDesktopIcons();
            return topLevelWorker != IntPtr.Zero ? topLevelWorker : progman;
        }

        public static IntPtr CreateWorkerW(IntPtr progman)
        {
            IntPtr result = IntPtr.Zero;

            SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
            SendMessageTimeout(progman, WM_SPAWN_WORKERW, new IntPtr(0xD), IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
            SendMessageTimeout(progman, WM_SPAWN_WORKERW, new IntPtr(0xD), new IntPtr(1), SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);

            var progmanChildWorker = FindWindowEx(progman, IntPtr.Zero, "WorkerW", IntPtr.Zero);
            return progmanChildWorker != IntPtr.Zero ? progmanChildWorker : FindTopLevelWorkerWBehindDesktopIcons();
        }

        public static void AttachWindowToDesktopHost(IntPtr windowHandle, IntPtr desktopHost, Size size, bool updateSize)
        {
            if (GetParent(windowHandle) != desktopHost)
            {
                SetParent(windowHandle, desktopHost);
            }

            var style = GetWindowLongPtr(windowHandle, GWL_STYLE).ToInt64();
            var desiredStyle = (style & ~WS_POPUP) | WS_CHILD | WS_VISIBLE;
            if (style != desiredStyle)
            {
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(desiredStyle));
            }

            var flags = SWP_NOACTIVATE | SWP_SHOWWINDOW;
            if (!updateSize)
            {
                flags |= SWP_NOMOVE | SWP_NOSIZE;
            }
            else
            {
                flags |= SWP_FRAMECHANGED;
            }

            SetWindowPos(windowHandle, HWND_TOP, 0, 0, size.Width, size.Height, flags);
        }

        public static void CloseWindow(IntPtr windowHandle)
        {
            SendMessageTimeout(windowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000, out _);
        }

        public static IntPtr FindChildWindowByTitle(IntPtr parentHandle, string windowTitle)
        {
            return FindWindowExByTitle(parentHandle, IntPtr.Zero, null, windowTitle);
        }

        private static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(windowHandle, index)
                : new IntPtr(GetWindowLong32(windowHandle, index));
        }

        private static IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr newLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(windowHandle, index, newLong)
                : new IntPtr(SetWindowLong32(windowHandle, index, newLong.ToInt32()));
        }

        private static IntPtr FindTopLevelWorkerWBehindDesktopIcons()
        {
            IntPtr workerw = IntPtr.Zero;

            EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) =>
            {
                var shellView = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);
                if (shellView == IntPtr.Zero)
                {
                    return true;
                }

                workerw = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", IntPtr.Zero);
                return workerw == IntPtr.Zero;
            }), IntPtr.Zero);

            return workerw;
        }
    }
}
