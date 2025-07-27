using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    public class Patches
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        public static void WriteBytes(string moduleAndOffset, string bytesToWrite)
        {
            try
            {
                // Fixed process name
                const string processName = "mcc-win64-shipping";

                // Parse module and offset
                var parts = moduleAndOffset.Split('+');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid module+offset format. Expected format: 'moduleName+offset'.");
                }

                string moduleName = parts[0];
                if (!int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int offset))
                {
                    throw new ArgumentException("Invalid offset format. Offset must be a hexadecimal value.");
                }

                // Convert bytes string to byte array
                byte[] byteArray = bytesToWrite.Split(' ')
                                               .Select(b => byte.Parse(b, NumberStyles.HexNumber))
                                               .ToArray();

                // Get the process and module base address
                var (process, moduleBaseAddress) = GetProcessAndModuleBaseAddress(processName, moduleName);

                // Open the process with all access
                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
                if (hProcess == IntPtr.Zero)
                {
                    throw new Exception("Failed to open process.");
                }

                // Calculate the target address
                IntPtr targetAddress = IntPtr.Add(moduleBaseAddress, offset);

                // Write the bytes to the target address
                if (!WriteProcessMemory(hProcess, targetAddress, byteArray, (uint)byteArray.Length, out _))
                {
                    throw new Exception("Failed to write bytes to process memory.");
                }

                Console.WriteLine("Bytes written successfully.");

                // Close the process handle
                CloseHandle(hProcess);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing bytes: {ex.Message}");
            }
        }

        private static Process GetProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0 ? processes[0] : null;
        }

        private static IntPtr GetModuleBaseAddress(Process process, string moduleName)
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }

        public static (Process process, IntPtr moduleBaseAddress) GetProcessAndModuleBaseAddress(string processName, string moduleName)
        {
            // Get the process by name
            Process process = GetProcessByName(processName);
            if (process == null)
            {
                throw new Exception($"Process '{processName}' not found.");
            }

            // Get the base address of the specified module
            IntPtr moduleBaseAddress = GetModuleBaseAddress(process, moduleName);
            if (moduleBaseAddress == IntPtr.Zero)
            {
                throw new Exception($"Module '{moduleName}' not found in the process.");
            }

            return (process, moduleBaseAddress);
        }

        public static void ApplyPatches()
        {
            //No fade 4C 8B DC
            try
            {
                WriteBytes("haloreach.dll+44FD1B", "90 90"); //Instant game end
                WriteBytes("haloreach.dll+2509B8", "C3 00");
                WriteBytes("haloreach.dll+34E6C", "C3 90");

                Debug.WriteLine("Patches applied successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying patches: {ex.Message}");
            }
        }


    }
}
