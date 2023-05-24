using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class MemoryScanner
{
    // Import the ReadProcessMemory function from the Windows API
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    public static bool ScanPointer(int[] offsets, out int result, out IntPtr address)
    {
        // Set the process name or process ID of the target process
        string processName = "MCC-Win64-Shipping";
        // Alternatively, you can use the process ID
        // int processId = 1234;

        // Set the module name of interest
        string moduleName = "haloreach.dll";

        // Set the offsets
        

        // Open the target process
        Process process = Process.GetProcessesByName(processName)[0];
        // Alternatively, you can use the process ID
        // Process process = Process.GetProcessById(processId);

        // Get the handle of the target process
        IntPtr processHandle = process.Handle;

        // Find the specific module by name
        ProcessModule targetModule = null;
        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                targetModule = module;
                break;
            }
        }

        if (targetModule == null)
        {
            //Debug.WriteLine("Module not found: " + moduleName);
            address = (IntPtr)0x0;
            result = -1;
            return false;
        }

        // Read the base address of the module
        IntPtr moduleBaseAddress = targetModule.BaseAddress;

        // Loop through the offsets
        IntPtr currentAddress = moduleBaseAddress;
        for (int i = 0; i < offsets.Length; i++)
        {
            int offset = offsets[i];

            // Calculate the address for the current offset
            currentAddress = IntPtr.Add(currentAddress, offset);

            // Read the 8-byte hexadecimal value at the current offset address
            long value = ReadInt64(processHandle, currentAddress);

            if (i < offsets.Length - 1)
            {
                currentAddress = (IntPtr)value;
            }
            

            // Convert the value to a hexadecimal string
            string hexValue = "0x" + value.ToString("X16");

            //Debug.WriteLine("Hex value at offset " + offset.ToString("X") + " (Index: " + i + "): " + hexValue);
        }

        // Read the last offset as an integer value
        int lastOffsetValue = ReadInt16(processHandle, currentAddress);
        result = lastOffsetValue;
        address = currentAddress;

        //Debug.WriteLine("Last offset value: " + lastOffsetValue);
        return true;
    }

    // Helper method to read an 8-byte integer value from process memory
    public static long ReadInt64(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[8];
        int bytesRead;

        // Read 8 bytes from the specified address in the process memory
        ReadProcessMemory(processHandle, address, buffer, 8, out bytesRead);

        // Convert the bytes to a long integer value
        long value = BitConverter.ToInt64(buffer, 0);
        return value;
    }

    public static short ReadInt16(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[2];
        int bytesRead;

        // Read 2 bytes from the specified address in the process memory
        ReadProcessMemory(processHandle, address, buffer, 2, out bytesRead);

        // Convert the bytes to a short integer value
        short value = BitConverter.ToInt16(buffer, 0);
        return value;
    }


    // Helper method to read a 4-byte integer value from process memory
    public static int ReadInt32(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[4];
        int bytesRead;

        // Read 4 bytes from the specified address in the process memory
        ReadProcessMemory(processHandle, address, buffer, 4, out bytesRead);

        // Convert the bytes to an integer value
        int value = BitConverter.ToInt32(buffer, 0);
        return value;
    }
}
