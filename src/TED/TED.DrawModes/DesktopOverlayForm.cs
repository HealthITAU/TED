using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using TED.Program;
using TED.Utils;

namespace TED.DrawModes
{
    /// <summary>
    /// Persistent transparent surface hosted by the desktop WorkerW/Progman tree.
    /// </summary>
    internal sealed class DesktopOverlayForm : Form
    {
        internal const string OverlayWindowTitle = "TED.DesktopOverlay";

        private static readonly Color TransparencyColor = Color.FromArgb(255, 1, 2, 3);
        private readonly Options options;
        private readonly Timer refreshTimer;
        private readonly Timer repairTimer;
        private IntPtr desktopHost;
        private int remainingRepairAttempts;
        private Size lastVirtualScreenSize;
        private bool perPixelAlpha;

        internal DesktopOverlayForm(IntPtr desktopHost, Options options)
        {
            this.desktopHost = desktopHost;
            this.options = options;

            Text = OverlayWindowTitle;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = TransparencyColor;
            // No TransparencyKey: the layered attributes are set in OnHandleCreated, either per-pixel
            // alpha (Windows 11 24H2+) or the colour key as a fallback.

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            refreshTimer = new Timer { Interval = 30000 };
            refreshTimer.Tick += (_, _) => Invalidate();

            repairTimer = new Timer { Interval = 500 };
            repairTimer.Tick += (_, _) => ContinueDesktopRepair();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= Win32Native.WS_EX_LAYERED;
                createParams.ExStyle |= Win32Native.WS_EX_NOACTIVATE;
                createParams.ExStyle |= Win32Native.WS_EX_TOOLWINDOW;
                createParams.ExStyle |= Win32Native.WS_EX_TRANSPARENT;
                return createParams;
            }
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            lastVirtualScreenSize = SystemInformation.VirtualScreen.Size;
            Win32Native.AttachWindowToDesktopHost(Handle, desktopHost, lastVirtualScreenSize, true);
            ConfigureTransparency();
        }

        /// <summary>
        /// Prefers per-pixel alpha (DWMWA_REDIRECTIONBITMAP_ALPHA, Windows 11 build 26100+), so
        /// semi-transparent image pixels and anti-aliased text blend with the wallpaper. On older
        /// builds the attribute is rejected and the window falls back to the colour key.
        /// </summary>
        private void ConfigureTransparency()
        {
            // The window must be a layered window with constant alpha for DWM to accept the attribute
            // on a child window; a non-layered child returns E_HANDLE.
            Win32Native.SetLayeredWindowAttributes(Handle, 0, 255, Win32Native.LWA_ALPHA);

            var enable = 1;
            perPixelAlpha = Win32Native.DwmSetWindowAttribute(Handle, Win32Native.DWMWA_REDIRECTIONBITMAP_ALPHA, ref enable, sizeof(int)) == 0;

            if (!perPixelAlpha)
            {
                Win32Native.SetLayeredWindowAttributes(Handle, (uint)ColorTranslator.ToWin32(TransparencyColor), 0, Win32Native.LWA_COLORKEY);
            }

            // In per-pixel mode the frame is copied in a single BitBlt (alpha included); the
            // double buffer would only add a copy that does not preserve alpha.
            SetStyle(ControlStyles.OptimizedDoubleBuffer, !perPixelAlpha);
            Invalidate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SystemEvents.DisplaySettingsChanging += OnDesktopChanged;
            SystemEvents.DisplaySettingsChanged += OnDesktopChanged;
            SystemEvents.UserPreferenceChanged += OnDesktopChanged;
            SystemEvents.PowerModeChanged += OnDesktopChanged;
            SystemEvents.SessionSwitch += OnDesktopChanged;
            refreshTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            refreshTimer.Stop();
            repairTimer.Stop();
            SystemEvents.DisplaySettingsChanging -= OnDesktopChanged;
            SystemEvents.DisplaySettingsChanged -= OnDesktopChanged;
            SystemEvents.UserPreferenceChanged -= OnDesktopChanged;
            SystemEvents.PowerModeChanged -= OnDesktopChanged;
            SystemEvents.SessionSwitch -= OnDesktopChanged;
            refreshTimer.Dispose();
            repairTimer.Dispose();
            base.OnClosed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Suppress the default background erase; OnPaint draws the whole frame in one pass.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!perPixelAlpha)
            {
                e.Graphics.Clear(TransparencyColor);
                DesktopTagRenderer.Draw(e.Graphics, options);
                return;
            }

            // Draw into a transparent ARGB bitmap, then copy it (alpha included, premultiplied)
            // into the window's redirection bitmap, which DWM composes with per-pixel alpha.
            var size = lastVirtualScreenSize.IsEmpty ? ClientSize : lastVirtualScreenSize;
            if (size.Width <= 0 || size.Height <= 0)
            {
                return;
            }

            using var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(e.Graphics.DpiX, e.Graphics.DpiY);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // ClearType assumes an opaque background; grayscale AA yields correct alpha.
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                DesktopTagRenderer.Draw(graphics, options);
            }

            var hdc = e.Graphics.GetHdc();
            try
            {
                Win32Native.BlitPremultiplied(hdc, bitmap);
            }
            finally
            {
                e.Graphics.ReleaseHdc(hdc);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == Win32Native.WM_DISPLAYCHANGE ||
                m.Msg == Win32Native.WM_SETTINGCHANGE ||
                m.Msg == Win32Native.WM_THEMECHANGED ||
                m.Msg == Win32Native.WM_DWMCOMPOSITIONCHANGED)
            {
                ScheduleDesktopRepair();
            }
        }

        private void OnDesktopChanged(object? sender, EventArgs e)
        {
            ScheduleDesktopRepair();
        }

        private void ScheduleDesktopRepair()
        {
            remainingRepairAttempts = 12;
            repairTimer.Stop();
            repairTimer.Start();
            RepairDesktopAttachment();
        }

        private void ContinueDesktopRepair()
        {
            RepairDesktopAttachment();

            remainingRepairAttempts--;
            if (remainingRepairAttempts <= 0)
            {
                repairTimer.Stop();
            }
        }

        private void RepairDesktopAttachment()
        {
            var currentHost = Win32Native.GetDesktopHostWindow();
            if (currentHost != IntPtr.Zero)
            {
                desktopHost = currentHost;
            }

            var currentVirtualScreenSize = SystemInformation.VirtualScreen.Size;
            var sizeChanged = currentVirtualScreenSize != lastVirtualScreenSize;
            lastVirtualScreenSize = currentVirtualScreenSize;

            Win32Native.AttachWindowToDesktopHost(Handle, desktopHost, currentVirtualScreenSize, sizeChanged);
            Invalidate();
        }
    }
}
