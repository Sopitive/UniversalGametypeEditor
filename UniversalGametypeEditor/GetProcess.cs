using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class GetProcess
{
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    static extern uint GetLastError();

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;

    public static void SendKey()
    {
        Process[] processes = Process.GetProcessesByName("MCC-Win64-Shipping");


        if (processes.Length > 0)
        {
            string windowCaption = "Halo: The Master Chief Collection";

            IntPtr hWnd = FindWindow("UnrealWindow", null);
            Debug.WriteLine(hWnd);

            SetForegroundWindow(hWnd);

            Thread.Sleep(20);

            // Simulate a key press
            // Hardware scan code for the escape key
        //const byte VK_ESCAPE = 0x01;

        // Set the key down
        //keybd_event(VK_ESCAPE, 0x01, 0, UIntPtr.Zero);
            //Thread.Sleep(20);
        // Set the key up
        //keybd_event(VK_ESCAPE, 0x01, 0x0002, UIntPtr.Zero);
        }
        else
        {
            Console.WriteLine("Process not found.");
        }
    }
}