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

namespace UniversalGametypeEditor
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        private GlobalHotkey globalHotkey;


        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        private DispatcherTimer dispatcherTimer;
        private IntPtr haloWindowHandle = IntPtr.Zero;

        private bool isOverlayVisible = true;
        private Window overlayWindow;
        public Overlay()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = SystemParameters.WorkArea.Width - this.Width;
            this.Top = 200;
            // Start a timer to check the active window
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            this.Deactivated += Overlay_Deactivated;
            //Owner = Application.Current.MainWindow;
            // Disallow any mouse events on the window
            this.PreviewMouseDown += (sender, e) => e.Handled = true;
            this.PreviewMouseUp += (sender, e) => e.Handled = true;
            this.PreviewMouseMove += (sender, e) => e.Handled = true;
            this.PreviewMouseWheel += (sender, e) => e.Handled = true;
            // Hide the cursor within this window
            this.Cursor = Cursors.None;

            globalHotkey = new GlobalHotkey();
            globalHotkey.RegisterGlobalHotKey(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(Application.Current.MainWindow) as HwndSource;
            source.AddHook(WndProc);

            
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // WM_HOTKEY is 0x0312
            if (msg == 0x0312)
            {
                // Your hotkey has been triggered
                Debug.WriteLine("Hotkey pressed");
                HotkeyCommandExecuted(null, null); // Call the hotkey command when the hotkey is pressed
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            processes = Process.GetProcessesByName("MCC-Win64-Shipping");


            // Unregister the hotkey when the window is closing
            globalHotkey.UnregisterGlobalHotKey(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
        }

        //Initialize processes array with empty array
        Process[] processes = new Process[0];

        private void Overlay_Deactivated(object sender, EventArgs e)
        {
            //Hide();
        }

        private void HotkeyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // Toggle the visibility of the overlay window
            if (this.IsVisible)
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
                        //Activate the MCC window
                        
                        
                    }
                }
            }
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
                helper.Owner = haloWindowHandle;


            }
            // Get the handle of the foreground window
            IntPtr activeWindowHandle = GetForegroundWindow();

            IntPtr thisWindow = new WindowInteropHelper(this).Handle;

            // Compare the handles
            if (activeWindowHandle != haloWindowHandle && activeWindowHandle != thisWindow)
            {

                if (this.IsVisible)
                {
                    bool isActive = false;
                    if (Application.Current.MainWindow.IsActive)
                    {
                        isActive = true;
                    }
                    //this.Hide();
                    if (!Application.Current.MainWindow.IsActive && isActive)
                    {
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.Activate();
                    }

                }
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

        //Get global numbers 0-4 every 100ms

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(UpdateGlobalNumbers);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void UpdateGlobalNumbers(object sender, EventArgs e)
        {
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
