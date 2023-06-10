using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class MemoryWriter
{


 

    // Import the WriteProcessMemory function from the Windows API
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    public static void WriteValue(IntPtr addressToWrite, int value)
    {
        // Set the process name or process ID of the target process
        string processName = "MCC-Win64-Shipping";
        // Alternatively, you can use the process ID
        // int processId = 1234;

        // Convert the integer value to a byte array
        byte[] buffer = BitConverter.GetBytes(value);

        // Open the target process
        
        Process? process = MemoryScanner.process;

        // Alternatively, you can use the process ID
        // Process process = Process.GetProcessById(processId);

        // Get the handle of the target process
        IntPtr processHandle = process.Handle;

        // Write the value to the specified memory address
        int bytesWritten;
        bool success = WriteProcessMemory(processHandle, addressToWrite, buffer, buffer.Length, out bytesWritten);

        if (success)
        {
            Debug.WriteLine("Value written successfully!");
        }
        else
        {
            Debug.WriteLine("Failed to write the value.");
        }
    }
}
