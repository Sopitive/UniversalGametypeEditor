using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class GetProcess
{
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;

    static void SendKey(string[] args)
    {
        Process[] processes = Process.GetProcessesByName("MCC-Win64-Shipping");

        if (processes.Length > 0)
        {
            IntPtr hWnd = processes[0].MainWindowHandle;

            // Send the escape key to the window
            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)Keys.Escape, IntPtr.Zero);
            SendMessage(hWnd, WM_KEYUP, (IntPtr)Keys.Escape, IntPtr.Zero);
        }
        else
        {
            Console.WriteLine("Process not found.");
        }
    }
}