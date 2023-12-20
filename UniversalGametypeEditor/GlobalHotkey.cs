using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class GlobalHotkey
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_CONTROL = 0x0002;
        public const uint VK_O = 0x4F;

        public void RegisterGlobalHotKey(IntPtr hWnd, int id)
        {
            if (!RegisterHotKey(hWnd, id, MOD_CONTROL, VK_O))
            {
               // MessageBox.Show("Hotkey registration failed");
            }
        }

        public void UnregisterGlobalHotKey(IntPtr hWnd, int id)
        {
            if (!UnregisterHotKey(hWnd, id))
            {
                //MessageBox.Show("Hotkey unregistration failed");
            }
        }
    }

}
