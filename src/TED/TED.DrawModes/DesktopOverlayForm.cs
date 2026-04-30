using Microsoft.Win32;
using System;
using System.Drawing;
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

        internal DesktopOverlayForm(IntPtr desktopHost, Options options)
        {
            this.desktopHost = desktopHost;
            this.options = options;

            Text = OverlayWindowTitle;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = TransparencyColor;
            TransparencyKey = TransparencyColor;

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
            // Suppress the default background erase; OnPaint clears and draws in one buffered pass.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(TransparencyColor);
            DesktopTagRenderer.Draw(e.Graphics, options);
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
