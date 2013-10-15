using ApplicationMover.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ApplicationMover
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private int indexOfLastMonitor;

        public struct Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        public MainForm()
        {
            InitializeComponent();

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Settings", OnMenuItemSettingsClicked);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", OnMenuItemExitClicked);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "SystemTrayTestTwo";
            trayIcon.Icon = new Icon(ApplicationMover.Properties.Resources.system_tray_icon, 40, 40);
            trayIcon.MouseClick += trayIcon_MouseClick;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            comboBoxMoveWindowShortcuts.SelectedIndex = Settings.Default.ShortcutKeyIndex;

            Visible = false;
            trayIcon.Visible = true;
            InitializeCurrentScreen();

            RegisterHotKey(this.Handle, 0, 0, GetShortcutKey().GetHashCode());
        }

        private void MinimizeAndHideSettingsForm()
        {
            WindowState = FormWindowState.Minimized;
            Visible = false;
            ShowInTaskbar = false;
            trayIcon.Visible = true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private void InitializeCurrentScreen()
        {
            Screen currentScreen = Screen.FromControl(this);

            for (int i = 0; i < Screen.AllScreens.Length; ++i)
            {
                if (Screen.AllScreens[i].DeviceName != currentScreen.DeviceName)
                    continue;

                indexOfLastMonitor = i;
                break;
            }
        }

        private void OnMenuItemExitClicked(object sender, EventArgs e)
        {
            Close();
        }

        private void OnMenuItemSettingsClicked(object sender, EventArgs e)
        {
            OpenSettingsForm();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (components != null)
                    components.Dispose();

                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;

            OpenSettingsForm();
        }

        private void OpenSettingsForm()
        {
            WindowState = FormWindowState.Normal;
            Visible = true;
            ShowInTaskbar = true;
            trayIcon.Visible = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0112 && (m.WParam.ToInt32() & 0xfff0) == 0xF020)
            {
                Visible = false;
                ShowInTaskbar = false;
                trayIcon.Visible = true;
            }

            if (m.Msg == 0x0312 && m.WParam.ToInt32() == 0)
            {
                InitializeCurrentScreen();

                if (++indexOfLastMonitor >= Screen.AllScreens.Length)
                    indexOfLastMonitor = 0;

                Rect activeWindowRect = new Rect();
                IntPtr activeWindow = GetForegroundWindow();
                GetWindowRect(activeWindow, ref activeWindowRect);

                Point newLocation = Screen.AllScreens[indexOfLastMonitor].WorkingArea.Location;
                newLocation.Y = activeWindowRect.Y;

                //! Hack-alert: For some reason GetWindowRect() always returns -8 for position X for full-size applications...
                if (activeWindowRect.X < -8)
                    newLocation.X = Screen.AllScreens[indexOfLastMonitor].Bounds.Width - (activeWindowRect.X * -1);
                else
                    newLocation.X += activeWindowRect.X;

                SetWindowPos(activeWindow, (IntPtr)0, newLocation.X, newLocation.Y, Screen.AllScreens[indexOfLastMonitor].Bounds.Width, Screen.AllScreens[indexOfLastMonitor].Bounds.Height, 0x0040 | 0x0001);
            }

            base.WndProc(ref m);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            MinimizeAndHideSettingsForm();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void SaveSettings()
        {
            Settings.Default.ShortcutKeyIndex = comboBoxMoveWindowShortcuts.SelectedIndex;
            Settings.Default.Save();

            UnregisterHotKey(this.Handle, 0);
            RegisterHotKey(this.Handle, 0, 0, GetShortcutKey().GetHashCode());
        }

        private Keys GetShortcutKey()
        {
            switch (comboBoxMoveWindowShortcuts.SelectedIndex)
            {
                case 0: return Keys.F1;
                case 1: return Keys.F2;
                case 2: return Keys.F3;
                case 3: return Keys.F4;
                case 4: return Keys.F5;
                case 5: return Keys.F6;
                case 6: return Keys.F7;
                case 7: return Keys.F8;
                case 8: return Keys.F9;
                case 9: return Keys.F10;
                case 10: return Keys.F11;
                case 11: return Keys.F12;
            }

            return Keys.F6;
        }
    }
}
