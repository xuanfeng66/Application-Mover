using ApplicationMover.Properties;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Configuration;

namespace ApplicationMover
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private Hashtable positionMap = new Hashtable();
        private Hashtable currentStateMap = new Hashtable();


        public struct Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        public struct CustomState
        {
            public FormWindowState state;
            public ScreenIndex screenIndex;

        }



        //显示器  1,2,3,4,  2|3 = 5，  2|3 /2 = 6, 默认0
        public enum ScreenIndex : int
        {
            NONE = 0,
            S1 = 1,
            S2 = 2,
            S3 = 3,
            S4 = 4,
            S5 = 5,
            S6 = 6
        }

        public struct State
        {
            public  Rect rect;
            public FormWindowState state;
        }

        public MainForm()
        {
            InitializeComponent();

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Settings", OnMenuItemSettingsClicked);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", OnMenuItemExitClicked);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "自定义窗口缩放";
            trayIcon.Icon = new Icon(ApplicationMover.Properties.Resources.system_tray_icon, 40, 40);
            trayIcon.MouseClick += trayIcon_MouseClick;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            Visible = false;
            trayIcon.Visible = true;

            SaveSettings();
        }

        private void MinimizeAndHideSettingsForm()
        {
           // SaveSettings();
            WindowState = FormWindowState.Minimized;
            Visible = false;
            ShowInTaskbar = false;
            trayIcon.Visible = true;
            SaveSettings();
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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool BRePaint);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
        IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);


        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        private void OnMenuItemExitClicked(object sender, EventArgs e)
        {
            UnregisterHotKey(this.Handle, 0);
            System.Environment.Exit(0);
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
            Visible = true;
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            SaveSettings();
        }

        private void UpdateWindowsProfile(IntPtr activeWindow, WINDOWPLACEMENT placement, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow))
            {
                return;
            }
                FormWindowState state = FormWindowState.Normal;
            if (placement.showCmd == ShowWindowCommands.Normal)
            {
                state = FormWindowState.Normal;
            }
            else if (placement.showCmd == ShowWindowCommands.Maximized)
            {
                state = FormWindowState.Maximized;
            }
            else if (placement.showCmd == ShowWindowCommands.Minimized)
            {
                state = FormWindowState.Minimized;
            }
            State ss = new State();
            ss.rect = activeWindowRect;
            ss.state = state;
            positionMap.Add(activeWindow, ss);
          
        }

        private void RestWindows(IntPtr activeWindow)
        {
            if(positionMap.ContainsKey(activeWindow) && currentStateMap.ContainsKey(activeWindow))
            {
                State ss = (State)positionMap[activeWindow];
                ShowWindow(activeWindow, ss.state == FormWindowState.Normal ? 1 : 3);
                MoveWindow(activeWindow, ss.rect.X, ss.rect.Y, ss.rect.Width - ss.rect.X, ss.rect.Height - ss.rect.Y, true);
                ShowWindow(activeWindow, ss.state == FormWindowState.Normal ? 1 : 3);
                CustomState cs = new CustomState();
                cs.state = ss.state;
                cs.screenIndex = ScreenIndex.NONE;
                currentStateMap[activeWindow] = cs;
            }
          
        }

        private void DealWithF6(IntPtr activeWindow, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow) 
                && currentStateMap.ContainsKey(activeWindow) 
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S1
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Maximized)
            {
                RestWindows(activeWindow);  
            }
            else
            {
                MoveWindow(activeWindow, -1080, -76, 1080, 1880, true);
                ShowWindow(activeWindow, 3);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Maximized;
                cs.screenIndex = ScreenIndex.S1;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                } else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
        }

        private void DealWithF7(IntPtr activeWindow, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow)
                && currentStateMap.ContainsKey(activeWindow)
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S2
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Maximized)
            {
                RestWindows(activeWindow);
            }
            else
            {
                MoveWindow(activeWindow, 0, 0, 1920, 2120, true);
                ShowWindow(activeWindow, 3);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Maximized;
                cs.screenIndex = ScreenIndex.S2;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                }
                else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
        }

        private void DealWithF8(IntPtr activeWindow, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow)
                && currentStateMap.ContainsKey(activeWindow)
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S3
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Maximized)
            {
                RestWindows(activeWindow);
            }
            else
            {
                MoveWindow(activeWindow, 1920, 0, 1920, 2120, true);
                ShowWindow(activeWindow, 3);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Maximized;
                cs.screenIndex = ScreenIndex.S3;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                }
                else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
        }

        private void DealWithF9(IntPtr activeWindow, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow)
                && currentStateMap.ContainsKey(activeWindow)
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S4
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Maximized)
            {
                RestWindows(activeWindow);
            }
            else
            {
                MoveWindow(activeWindow, 3840, 0, 1920, 1040, true);
                ShowWindow(activeWindow, 3);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Maximized;
                cs.screenIndex = ScreenIndex.S4;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                }
                else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
        }

        //老的->S5->S6->老的 循环
        private void DealWithF10(IntPtr activeWindow, Rect activeWindowRect)
        {
            if (positionMap.ContainsKey(activeWindow)
                && currentStateMap.ContainsKey(activeWindow)
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S6
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Normal)
            {
                RestWindows(activeWindow);
            }
            else if (positionMap.ContainsKey(activeWindow)
                && currentStateMap.ContainsKey(activeWindow)
                && ((CustomState)currentStateMap[activeWindow]).screenIndex == ScreenIndex.S5
                && ((CustomState)currentStateMap[activeWindow]).state == FormWindowState.Normal)
            {
                ShowWindow(activeWindow, 1);
                MoveWindow(activeWindow, 960, 540, 1920, 1080, true);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Normal;
                cs.screenIndex = ScreenIndex.S6;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                }
                else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
            else
            {
                ShowWindow(activeWindow, 1);
                MoveWindow(activeWindow, 0, 0, 3840, 2120, true);
                CustomState cs = new CustomState();
                cs.state = FormWindowState.Normal;
                cs.screenIndex = ScreenIndex.S5;
                if (currentStateMap.ContainsKey(activeWindow))
                {
                    currentStateMap[activeWindow] = cs;
                }
                else
                {
                    currentStateMap.Add(activeWindow, cs);
                }
            }
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
                int keyCode = m.LParam.ToInt32();
                Rect activeWindowRect = new Rect();
                IntPtr activeWindow = GetForegroundWindow();
                if(activeWindow == this.Handle || GetParent(activeWindow) == this.Handle)
                {
                    base.WndProc(ref m);
                    return;
                }
                GetWindowRect(activeWindow, ref activeWindowRect);
                WINDOWPLACEMENT placement =  GetPlacement(activeWindow);
                UpdateWindowsProfile(activeWindow, placement, activeWindowRect);
                //F6
                if (keyCode == 0x00750000)
                {
                    DealWithF6(activeWindow, activeWindowRect);
                }
                //F7
                else if (keyCode == 0x00760000)
                {
                    DealWithF7(activeWindow, activeWindowRect);

                }
                //F8
                else if (keyCode == 0x00770000)
                {
                    DealWithF8(activeWindow, activeWindowRect);

                }
                //F9
                else if (keyCode == 0x00780000)
                {
                    DealWithF9(activeWindow, activeWindowRect);

                }
                //F10
                else if (keyCode == 0x00790000)
                {
                    DealWithF10(activeWindow, activeWindowRect);
                }

               
             
            }

            base.WndProc(ref m);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            MinimizeAndHideSettingsForm();
        }

        private void SaveSettings()
        {
            UnregisterHotKey(this.Handle, 0);
            RegisterHotKey(this.Handle, 0, 0, Keys.F6.GetHashCode());
            RegisterHotKey(this.Handle, 0, 0, Keys.F7.GetHashCode());
            RegisterHotKey(this.Handle, 0, 0, Keys.F8.GetHashCode());
            RegisterHotKey(this.Handle, 0, 0, Keys.F9.GetHashCode());
            RegisterHotKey(this.Handle, 0, 0, Keys.F10.GetHashCode());
        }

        private void label1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("别点了没用");
        }

        private void label4_Click(object sender, EventArgs e)
        {
            label1_Click(sender, e);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            label1_Click(sender, e);
        }

        private void label2_Click(object sender, EventArgs e)
        {
            label1_Click(sender, e);
        }

        private void label5_Click(object sender, EventArgs e)
        {
            label1_Click(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            positionMap.Clear();
            currentStateMap.Clear();
            MessageBox.Show("清理成功！！！");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MinimizeAndHideSettingsForm();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OnMenuItemExitClicked(sender, e);
        }
    }
}
