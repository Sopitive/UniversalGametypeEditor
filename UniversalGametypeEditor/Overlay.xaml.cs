using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static UniversalGametypeEditor.Overlay;

namespace UniversalGametypeEditor
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        private readonly GlobalHotkey globalHotkey;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // Added missing interop declarations and constants.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // New: SetWindowPos declaration and flags.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        // Use HWND_TOPMOST (new IntPtr(-1)) to force the overlay above the game window.
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);

        // Window message constants.
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTTRANSPARENT = -1;

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private IntPtr haloWindowHandle = IntPtr.Zero;
        private bool anticheat = false;

        public Overlay()
        {
            InitializeComponent();
            // Remove window chrome to match the exact size of the target window.
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            // Optional: if you want transparency (e.g. only display parts), set AllowsTransparency:
            // AllowsTransparency = true;

            // Exclude from taskbar.
            ShowInTaskbar = false;

            // Set the overlay window as topmost
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            // Initially position the overlay (will be updated in the timer)
            Left = SystemParameters.WorkArea.Width - Width;
            Top = 200;

            DispatcherTimer dispatcherTimer = new();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();

            // Modify event handlers to conditionally handle events
            PreviewMouseDown += Overlay_PreviewMouseDown;
            PreviewMouseUp += Overlay_PreviewMouseUp;
            PreviewMouseMove += Overlay_PreviewMouseMove;
            PreviewMouseWheel += Overlay_PreviewMouseWheel;

            // Set cursor to visible state
            Cursor = Cursors.Arrow;

            globalHotkey = new GlobalHotkey();
            RegisterHotkey();
        }

        

        private void Overlay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is TextBox)
            {
                e.Handled = false; // Allow event to be handled by TextBox
            }
            else
            {
                e.Handled = true; // Handle event for other controls
            }
        }

        private void Overlay_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is TextBox)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void Overlay_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is TextBox)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void Overlay_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Source is TextBox)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Get the window handle.
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                // Modify the extended window style to include WS_EX_TOOLWINDOW, which prevents the window from being shown in Alt+Tab.
                const int GWL_EXSTYLE = -20;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                int exStyle = GetWindowLong(source.Handle, GWL_EXSTYLE);
                SetWindowLong(source.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

                // Add the window procedure hook.
                source.AddHook(WndProc);
            }
            else
            {
                Debug.WriteLine("HwndSource is null.");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle WM_MOUSEACTIVATE to prevent the overlay from losing focus when clicking game areas.
            if (msg == WM_MOUSEACTIVATE)
            {
                // Get client coordinates from lParam.
                int x = (short)(lParam.ToInt32() & 0xFFFF);
                int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
                Point pt = new Point(x, y);
                HitTestResult result = VisualTreeHelper.HitTest(this, pt);
                // If not over an interactive control (e.g. not a TextBox), do not activate.
                if (result == null || !(result.VisualHit is TextBox))
                {
                    handled = true;
                    return new IntPtr(MA_NOACTIVATE);
                }
            }

            // Make non-interactive areas click-through so that the underlying game retains input.
            if (msg == WM_NCHITTEST)
            {
                // Get mouse position in screen coordinates.
                int x = (short)(lParam.ToInt32() & 0xFFFF);
                int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
                // Convert to window coordinates.
                Point pt = this.PointFromScreen(new Point(x, y));
                // Hit test the visual tree.
                HitTestResult result = VisualTreeHelper.HitTest(this, pt);
                if (result == null)
                {
                    handled = true;
                    return new IntPtr(HTTRANSPARENT);
                }
                else
                {
                    handled = true;
                    return new IntPtr(HTCLIENT);
                }
            }

            if (msg == 0x0312) // WM_HOTKEY
            {
                int hotkeyId = wParam.ToInt32();
                switch (hotkeyId)
                {
                    case 1:
                        HotkeyCommandExecuted_O(null, null);
                        break;
                    case 2:
                        HotkeyCommandExecuted_Numpad7(null, null);
                        break;
                }
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        public void UnregisterHotkey()
        {
            globalHotkey.UnregisterGlobalHotKey(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
            globalHotkey.UnregisterGlobalHotKey(new WindowInteropHelper(Application.Current.MainWindow).Handle, 2);
        }

        public void RegisterHotkey()
        {
            globalHotkey.RegisterGlobalHotKey_O(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
            globalHotkey.RegisterGlobalHotKey_Numpad7(new WindowInteropHelper(Application.Current.MainWindow).Handle, 2);
        }

        Process[] processes = Array.Empty<Process>();
        private MainWindow mw;
        private void HotkeyCommandExecuted_O(object sender, ExecutedRoutedEventArgs e)
        {
            processes = Process.GetProcessesByName("EasyAntiCheat");
            if (processes.Length == 0)
            {
                anticheat = false;
            }
            if (anticheat || processes.Length > 0)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                mw.UpdateLastEvent("Cannot Activate Overlay With EAC Enabled");
                return;
            }
            if (processes.Length > 0)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                mw.UpdateLastEvent("Launch MCC With EAC Off To Use The Overlay");
                return;
            }
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                processes = Process.GetProcessesByName("MCC-Win64-Shipping");

                if (processes.Length > 0)
                {
                    haloWindowHandle = processes[0].MainWindowHandle;
                    IntPtr activeWindowHandle = GetForegroundWindow();
                    if (activeWindowHandle == haloWindowHandle)
                    {
                        Show();
                        SetForegroundWindow(haloWindowHandle);
                    }
                    else
                    {
                        mw = (MainWindow)Application.Current.MainWindow;
                        mw.UpdateLastEvent("MCC Must Be In Focus To Toggle Debug Overlay");
                    }
                }
            }
        }

        private void HotkeyCommandExecuted_Numpad7(object sender, ExecutedRoutedEventArgs e)
        {
            processes = Process.GetProcessesByName("EasyAntiCheat");
            if (processes.Length == 0)
            {
                anticheat = false;
            }
            if (anticheat || processes.Length > 0)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                mw.UpdateLastEvent("Cannot Activate Overlay With EAC Enabled");
                return;
            }
            if (processes.Length > 0)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                mw.UpdateLastEvent("Launch MCC With EAC Off To Use The Overlay");
                return;
            }
            MemoryWriter.WriteOpcode();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            processes = Process.GetProcessesByName("MCC-Win64-Shipping");
            if (processes.Length > 0)
            {
                haloWindowHandle = processes[0].MainWindowHandle;
                WindowInteropHelper helper = new WindowInteropHelper(this);

                // Update overlay size and position to match the game window bounds.
                if (GetWindowRect(haloWindowHandle, out RECT rect))
                {
                    // Convert from device pixels to WPF DIPs.
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source?.CompositionTarget != null)
                    {
                        var transform = source.CompositionTarget.TransformFromDevice;
                        Point topLeft = transform.Transform(new Point(rect.Left, rect.Top));
                        Point bottomRight = transform.Transform(new Point(rect.Right, rect.Bottom));

                        Left = topLeft.X;
                        Top = topLeft.Y;
                        Width = bottomRight.X - topLeft.X;
                        Height = bottomRight.Y - topLeft.Y;
                    }
                    else
                    {
                        // Fallback to using device pixels if DPI info is not available.
                        Left = rect.Left;
                        Top = rect.Top;
                        Width = rect.Right - rect.Left;
                        Height = rect.Bottom - rect.Top;
                    }
                }

                // Check if the target window is focused.
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != haloWindowHandle && foregroundWindow != helper.Handle)
                {
                    if (IsVisible)
                    {
                        Hide();
                    }
                }
                else
                {
                    if (!IsVisible)
                    {
                        try
                        {
                            Show();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }

                    }
                }

                // Ensure the overlay remains on top using HWND_TOPMOST.
                SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                // No target window found. Hide the overlay if visible.
                if (IsVisible)
                {
                    Hide();
                }
            }
        }

        private static int GetGlobalNumber(int index)
        {
            return MemoryScanner.ReadGlobalNumber(index);
        }

        private void GetMegaloObjects()
        {
            int[] megalo_objects_address = new int[7] { 0x00C13088, 0x650, 0x590, 0x140, 0x1F8, 0xF30, 0x4F0 };

            // Scan the memory to get the address of megalo_objects
            MemoryScanner.ScanPointer(megalo_objects_address, out int megalo_objects, out IntPtr address);

            if (address == IntPtr.Zero)
            {
                // Handle invalid address
                return;
            }

            MemoryScanner.ScanPointer(new int[1] { 0x28799C4 }, out int forge_count, out IntPtr forge_address);

            if (forge_address != IntPtr.Zero)
            {
                //Get the last 2 bytes of forge_address
                string forge_count_string = forge_address.ToString("X").Substring(12, 4);
                forge_count = Convert.ToInt32(forge_count_string, 16);
            }
            if (forge_address == IntPtr.Zero && processes.Length > 0)
            {
                IntPtr forge_object_count = IntPtr.Subtract(address, 0x2FC);
                forge_count = (int)MemoryScanner.ReadInt16(processes[0].Handle, forge_object_count);
            }

            if (forge_count > 0)
            {
                ForgeObjectCount.Text = $"Forge Objects: {forge_count}";
            }

            // Add 68 bytes to the address to get the count of objects
            IntPtr countAddress = IntPtr.Add(address, 0x44);

            try
            {
                // Read the integer value at the countAddress
                int object_count = (int)MemoryScanner.ReadInt64(processes[0].Handle, countAddress);

                // Update the UI or handle the object count as needed
                if (object_count > 0)
                {
                    MegaloObjectCount.Text = $"Megalo Objects: {object_count}";
                }

                //Get Static Object Count (64 bytes after the megalo_objects address)
                IntPtr staticCountAddress = IntPtr.Add(address, 0x40);
                int static_object_count = (int)MemoryScanner.ReadInt64(processes[0].Handle, staticCountAddress);
                if (static_object_count > 0)
                {
                    StaticObjectCount.Text = $"Static Objects: {static_object_count}";
                }
            }
            catch (Exception ex)
            {
                // Handle access violation
            }
        }

        private void GetCoordinates()
        {
            //Pointer haloreach.dll+4872890
            MemoryScanner.ScanPointer(new int[] { 0x4872890 }, out int x, out IntPtr address);
            MemoryScanner.ScanPointer(new int[] { 0x4872894 }, out int y, out IntPtr address2);
            MemoryScanner.ScanPointer(new int[] { 0x4872898 }, out int z, out IntPtr address3);

            //Convert 8 byte int to float
            float xcoord = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);
            float ycoord = BitConverter.ToSingle(BitConverter.GetBytes(y), 0);
            float zcoord = BitConverter.ToSingle(BitConverter.GetBytes(z), 0);

            //Round to 2 decimal places
            xcoord = (float)Math.Round(xcoord, 2);
            ycoord = (float)Math.Round(ycoord, 2);
            zcoord = (float)Math.Round(zcoord, 2);

            CoordsX.Text = $"X: {xcoord}";
            CoordsY.Text = $"Y: {ycoord}";
            CoordsZ.Text = $"Z: {zcoord}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //DispatcherTimer dispatcherTimer = new();
            //dispatcherTimer.Tick += new EventHandler(UpdateGlobalNumbers);
            //dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            //dispatcherTimer.Start();
        }

        private void GlobalNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                // Check if the text has actually changed
                if (textBox.Tag == null || !textBox.Tag.Equals(textBox.Text))
                {
                    textBox.Tag = textBox.Text; // Update the Tag with the new value

                    if (textBox.Text != "0" && !string.IsNullOrEmpty(textBox.Text))
                    {
                        int index = int.Parse(textBox.Name.Replace("GlobalNum", ""));
                        string newValue = textBox.Text;

                        // Update the value in memory
                        UpdateGlobalValue(index, newValue);
                    }
                }
            }
        }

        private void UpdateGlobalValue(int index, string newValue)
        {
            if (int.TryParse(newValue, out int intValue))
            {
                MemoryScanner.WriteGlobalNumber(index, intValue);
            }
        }

        private async void UpdateGlobalNumbers(object sender, EventArgs e)
        {
            processes = Process.GetProcessesByName("EasyAntiCheat");
            if (processes.Length > 0)
            {
                anticheat = true;
                return;
            }
            processes = Process.GetProcessesByName("MCC-Win64-Shipping");
            if (processes.Length == 0)
            {
                return;
            }

            // Offload memory reading to a background thread.
            int[] globalValues = await Task.Run(() =>
            {
                int[] values = new int[12];
                for (int i = 0; i < 12; i++)
                {
                    values[i] = GetGlobalNumber(i);
                }
                return values;
            });

            // UI update after memory reading completes.
            GlobalNum0.Text = globalValues[0].ToString();
            GlobalNum1.Text = globalValues[1].ToString();
            GlobalNum2.Text = globalValues[2].ToString();
            GlobalNum3.Text = globalValues[3].ToString();
            GlobalNum4.Text = globalValues[4].ToString();
            GlobalNum5.Text = globalValues[5].ToString();
            GlobalNum6.Text = globalValues[6].ToString();
            GlobalNum7.Text = globalValues[7].ToString();
            GlobalNum8.Text = globalValues[8].ToString();
            GlobalNum9.Text = globalValues[9].ToString();
            GlobalNum10.Text = globalValues[10].ToString();
            GlobalNum11.Text = globalValues[11].ToString();
        }
    }
}
