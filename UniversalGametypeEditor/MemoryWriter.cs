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

    // Import the OpenProcess function from the Windows API
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);


    public static void WriteOpcode()
    {
        // Get the process
        Process process = Process.GetProcessesByName("mcc-win64-shipping")[0];

        // Get the base address of the mcc-win64-shipping.exe module
        IntPtr baseAddress = process.MainModule.BaseAddress;

        // Define PROCESS_ALL_ACCESS
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Open the process with all access
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

        // Define the patch (from jne to je)
        byte[] patch = { 0x74 };


        // Define the memory offset
        int memoryOffset = 0x5042B1;

        // Calculate the absolute memory address
        IntPtr memoryAddress = IntPtr.Add(baseAddress, memoryOffset);

        // Apply the patch
        WriteProcessMemory(processHandle, memoryAddress, patch, patch.Length, out _);
        //Restore the original value
        byte[] original = { 0x75 };
        //Wait 10ms
        System.Threading.Thread.Sleep(1000);
        WriteProcessMemory(processHandle, memoryAddress, original, original.Length, out _);
    }
}
