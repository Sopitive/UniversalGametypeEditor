﻿using System;
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
        private readonly GlobalHotkey globalHotkey;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


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
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            PreviewMouseDown += (sender, e) => e.Handled = true;
            PreviewMouseUp += (sender, e) => e.Handled = true;
            PreviewMouseMove += (sender, e) => e.Handled = true;
            PreviewMouseWheel += (sender, e) => e.Handled = true;
            Cursor = Cursors.None;
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
            if (msg == 0x0312)
            {
                HotkeyCommandExecuted(null, null); // Call the hotkey command when the hotkey is pressed
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            globalHotkey.UnregisterGlobalHotKey(new WindowInteropHelper(Application.Current.MainWindow).Handle, 1);
        }

        Process[] processes = Array.Empty<Process>();
        private MainWindow mw;
        private void HotkeyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            processes = Process.GetProcessesByName("EasyAntiCheat");
            if (processes.Length == 0)
            {
                anticheat = false;
            }
            if (anticheat)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                mw.UpdateLastEvent("Launch with EAC Off To Use The Overlay");
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            processes = Process.GetProcessesByName("EasyAntiCheat");
            if (processes.Length > 0)
            {
                anticheat = true;
                return;
            }
            DispatcherTimer dispatcherTimer = new();
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
