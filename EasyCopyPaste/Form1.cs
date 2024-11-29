using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EasyCopyPaste
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private bool isEnabled = true;
        private string lastCopiedText = string.Empty;
        private bool startWithWindows = true;
        private Hook _mouseHook;
        private bool _isSelecting = false;
        private CustomNotification _notification;
        private bool _settingsModified = false;
        private const int NOTIFICATION_DURATION = 1500;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public Form1()
        {
            InitializeComponent();
            InitializeNotification();
            InitializeTrayIcon();
            SetStartup(startWithWindows);
            InitializeClipboardMonitor();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();

            LoadSettings();
        }

        private void InitializeNotification()
        {
            _notification = new CustomNotification();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Enhanced Copy Paste",
                Visible = true
            };
            UpdateTrayMenu();
        }

        private void UpdateTrayMenu()
        {
            var menu = new ContextMenuStrip();
            var enabledItem = new ToolStripMenuItem("Enabled", null, OnToggleEnabled)
            {
                Checked = isEnabled,
                CheckOnClick = true
            };
            menu.Items.Add(enabledItem);
            menu.Items.Add("Settings", null, OnSettings);
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, OnExit);
            trayIcon.ContextMenuStrip = menu;
        }

        private void InitializeClipboardMonitor()
        {
            _mouseHook = new Hook();
            _mouseHook.MouseUp += (s, e) =>
            {
                if (isEnabled && _isSelecting)
                {
                    System.Threading.Thread.Sleep(100); 
                    SendKeys.Send("^c");
                    _isSelecting = false;
                }
            };

            _mouseHook.MouseDown += (s, e) =>
            {
                _isSelecting = true;
            };

            _mouseHook.MiddleClick += (s, e) =>
            {
                if (isEnabled && Clipboard.ContainsText())
                {
                    SendKeys.Send("^v");
                }
            };

            _mouseHook.Start();
        }

        private bool HasSelectedText()
        {
            try
            {
                var info = new GUITHREADINFO();
                info.cbSize = Marshal.SizeOf(info);
                GetGUIThreadInfo(0, ref info);
                return info.hwndCaret != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        private void ShowNotification(string message)
        {
            if (_notification != null && !_notification.IsDisposed)
            {
                _notification.ShowMessage(message);
            }
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\EnhancedCopyPaste"))
                {
                    if (key != null)
                    {
                        startWithWindows = (int)key.GetValue("StartWithWindows", 1) == 1;
                        isEnabled = (int)key.GetValue("Enabled", 1) == 1;
                        UpdateTrayMenu();
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\EnhancedCopyPaste"))
                {
                    key.SetValue("StartWithWindows", startWithWindows ? 1 : 0);
                    key.SetValue("Enabled", isEnabled ? 1 : 0);
                }
            }
            catch { }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                        rk.SetValue("EnhancedCopyPaste", Application.ExecutablePath);
                    else if (rk.GetValue("EnhancedCopyPaste") != null)
                        rk.DeleteValue("EnhancedCopyPaste");
                }
            }
            catch { }
        }

        private void OnToggleEnabled(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            isEnabled = menuItem.Checked;
            _settingsModified = true;
            SaveSettings();
            ShowNotification(isEnabled ? "Enabled" : "Disabled");
        }

        private void OnSettings(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.SetCurrentSettings(startWithWindows);
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    startWithWindows = settingsForm.AutoStart;
                    SetStartup(startWithWindows);
                    _settingsModified = true;
                    SaveSettings();
                    ShowNotification("Settings saved");
                }
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            if (_settingsModified)
            {
                SaveSettings();
            }
            try
            {
                _mouseHook?.Stop();
                trayIcon.Visible = false;
                _notification?.Dispose();
            }
            finally
            {
                Application.Exit();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_DRAWCLIPBOARD = 0x0308;

            if (m.Msg == WM_DRAWCLIPBOARD && isEnabled)
            {
                if (Clipboard.ContainsText())
                {
                    string newText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(newText) && newText != lastCopiedText)
                    {
                        lastCopiedText = newText;
                        ShowNotification("Text copied");
                    }
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else if (_settingsModified)
            {
                SaveSettings();
            }
        }

        
    }

    public class CustomNotification : Form
    {
        private readonly Label messageLabel;
        private readonly System.Windows.Forms.Timer fadeTimer;
        private float opacity = 1.0f;
        private static readonly object _lock = new object();

        public CustomNotification()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(64, 64, 64);
            this.Size = new Size(200, 40);
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;

            messageLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f)
            };

            fadeTimer = new System.Windows.Forms.Timer
            {
                Interval = 50
            };
            fadeTimer.Tick += FadeTimer_Tick;

            this.Controls.Add(messageLabel);
        }

        public void ShowMessage(string message)
        {
            lock (_lock)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => ShowMessage(message)));
                    return;
                }

                messageLabel.Text = message;
                PositionWindow();
                opacity = 1.0f;
                this.Opacity = opacity;
                fadeTimer.Stop();
                this.Show();
                fadeTimer.Start();
            }
        }

        private void PositionWindow()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                workingArea.Left + (workingArea.Width - this.Width) / 2,
                workingArea.Bottom - this.Height - 10
            );
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            opacity -= 0.1f;
            if (opacity <= 0)
            {
                fadeTimer.Stop();
                this.Hide();
            }
            else
            {
                this.Opacity = opacity;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                return cp;
            }
        }
    }

    public class Hook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MBUTTONDOWN = 0x0207;

        public event EventHandler MouseDown;
        public event EventHandler MouseUp;
        public event EventHandler MiddleClick;

        private readonly Win32ApiDelegate _hookProc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool disposedValue;

        public delegate IntPtr Win32ApiDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, Win32ApiDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public Hook()
        {
            _hookProc = HookCallback;
        }

        public void Start()
        {
            _hookId = SetHook(_hookProc);
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(Win32ApiDelegate proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        MouseDown?.Invoke(this, EventArgs.Empty);
                        break;
                    case WM_LBUTTONUP:
                        MouseUp?.Invoke(this, EventArgs.Empty);
                        break;
                    case WM_MBUTTONDOWN:
                        MiddleClick?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}