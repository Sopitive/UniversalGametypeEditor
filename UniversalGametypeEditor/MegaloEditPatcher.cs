using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;

namespace UniversalGametypeEditor
{
    public static class MegaloEditPatcher
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        const int WM_SYSCOLORCHANGE = 0x0015;

        public static void Patch()
        {
            // Get the process called MegaloEdit
            Process[] processes = Process.GetProcessesByName("MegaloEdit");
            if (processes.Length > 0)
            {
                // Get the handle of the window
                IntPtr hWnd = processes[0].MainWindowHandle;

                if (hWnd != IntPtr.Zero)
                {
                    // Convert the color to a COLORREF
                    Color color = ColorTranslator.FromHtml("#3D3D3D");
                    int colorRef = color.R | color.G << 8 | color.B << 16;

                    // Send a message to the window to change the background color
                    SendMessage(hWnd, WM_SYSCOLORCHANGE, IntPtr.Zero, (IntPtr)colorRef);
                    Debug.WriteLine("Sent Message");
                }
                else
                {
                    Debug.WriteLine("Window not found");
                }
            }
            else
            {
                Debug.WriteLine("Process not found");
            }
        }
    }
}
