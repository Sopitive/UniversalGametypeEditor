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
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.WorkArea.Width - Width;
            Top = 200;
            DispatcherTimer dispatcherTimer = new();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
            PreviewMouseDown += (sender, e) => e.Handled = true;
            PreviewMouseUp += (sender, e) => e.Handled = true;
            PreviewMouseMove += (sender, e) => e.Handled = true;
            PreviewMouseWheel += (sender, e) => e.Handled = true;
            Cursor = Cursors.None;
            globalHotkey = new GlobalHotkey();
            RegisterHotkey();
            //globalHotkey.RegisterGlobalHotKey_O(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
            //globalHotkey.RegisterGlobalHotKey_Numpad7(new WindowInteropHelper(Application.Current.MainWindow).Handle, 2);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                HwndSource source = PresentationSource.FromVisual(mainWindow) as HwndSource;
                if (source != null)
                {
                    source.AddHook(WndProc);
                }
                else
                {
                    // Handle the case where source is null
                    Debug.WriteLine("HwndSource is null.");
                }
            }
            else
            {
                // Handle the case where mainWindow is null
                Debug.WriteLine("MainWindow is null.");
            }
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                int hotkeyId = wParam.ToInt32();
                switch (hotkeyId)
                {
                    case 1:
                        HotkeyCommandExecuted_O(null, null); // Call the hotkey command for Control + O
                        break;
                    case 2:
                        HotkeyCommandExecuted_Numpad7(null, null); // Call the hotkey command for Control + Numpad7
                        break;
                }
            }
            return IntPtr.Zero;
        }

        // Remove the unregistration from OnClosed
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        // Add a method to unregister the global hotkey
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
            if (processes.Length == 0)
            {
                processes = Process.GetProcessesByName("MCC-Win64-Shipping");
            }
            

            if (processes.Length > 0)
            {
                haloWindowHandle = processes[0].MainWindowHandle;
                WindowInteropHelper helper = new WindowInteropHelper(this);
                if (helper.Owner != haloWindowHandle)
                {
                    Hide();
                    helper.Owner = haloWindowHandle;
                }
                //Set the size of this window to the same size as the Halo window
                GetWindowRect(haloWindowHandle, out RECT rect);
                Width = rect.Right - rect.Left;
                Height = rect.Bottom - rect.Top;
                Left = rect.Left;
                Top = rect.Top;
            }
        }

        private static int GetGlobalNumber(int index)
        {
            var lastOffset = 0xBEC;
            lastOffset += index * 0x4;
            int[] offsets = { 0x02757E34, 0x150, lastOffset };
            MemoryScanner.ScanPointer(offsets, out int globalnum, out IntPtr address);
            return globalnum;
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

            
            MemoryScanner.ScanPointer(new int[1] { 0x28799C4 },out int forge_count, out IntPtr forge_address);

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
            catch (AccessViolationException)
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
            DispatcherTimer dispatcherTimer = new();
            dispatcherTimer.Tick += new EventHandler(UpdateGlobalNumbers);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }

        private void UpdateGlobalNumbers(object sender, EventArgs e)
        {
            GetMegaloObjects();
            GetCoordinates();
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
            for (int i=0; i < 12 ; i++)
            {
                int globalnum = GetGlobalNumber(i);
                switch (i)
                {
                    case 0:
                        GlobalNum0.Text = globalnum.ToString();
                        break;
                    case 1:
                        GlobalNum1.Text = globalnum.ToString();
                        break;
                    case 2:
                        GlobalNum2.Text = globalnum.ToString();
                        break;
                    case 3:
                        GlobalNum3.Text = globalnum.ToString();
                        break;
                    case 4:
                        GlobalNum4.Text = globalnum.ToString();
                        break;
                    case 5:
                        GlobalNum5.Text = globalnum.ToString();
                        break;
                    case 6:
                        GlobalNum6.Text = globalnum.ToString();
                        break;
                    case 7:
                        GlobalNum7.Text = globalnum.ToString();
                        break;
                    case 8:
                        GlobalNum8.Text = globalnum.ToString();
                        break;
                    case 9:
                        GlobalNum9.Text = globalnum.ToString();
                        break;
                    case 10:
                        GlobalNum10.Text = globalnum.ToString();
                        break;
                    case 11:
                        GlobalNum11.Text = globalnum.ToString();
                        break;
                }
            }
        }
    }
}
