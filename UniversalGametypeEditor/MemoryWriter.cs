using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

public static class MemoryWriter
{


    // Import the ReadProcessMemory function from the Windows API
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);


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

    public static int WriteOpcode2()
    {
        // Get the process
        Process process = Process.GetProcessesByName("MegaloEdit")[0];
        if (process == null)
        {
            return 1;
        }
        // Get the base address of the MegaloEdit.exe module
        IntPtr baseAddress = process.MainModule.BaseAddress;

        // Define PROCESS_ALL_ACCESS
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Open the process with all access
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

        // Define the byte sequence to search for
        byte[] searchBytes = { 0x74, 0x6E, 0x48, 0x8B, 0x4D, 0x10, 0xE8 };

        // Define the byte sequence to replace it with
        byte[] replaceBytes = { 0xEB, 0x6E, 0x48, 0x8B, 0x4D, 0x10, 0xE8 };


        // Define the size of the buffer
        // Define the size of the buffer
        const int bufferSize = 32768; // adjust this to the size you want

        //7FFAAB173A70 

        // Define the start and end addresses
        IntPtr startAddress = new IntPtr(0x7FFA78D00000); // replace with your start address
        IntPtr endAddress = new IntPtr(0x7FFF78DFFA40); // replace with your end address

        // Create the buffer
        byte[] buffer = new byte[bufferSize];

        // Loop over the memory
        for (IntPtr p = startAddress; p.ToInt64() < endAddress.ToInt64(); p = IntPtr.Add(p, bufferSize))
        {
            // Read the memory at the current address
            bool success = ReadProcessMemory(processHandle, p, buffer, (uint)buffer.Length, out int bytesRead);

            // Loop over the buffer
            for (int i = 0; i < bytesRead - searchBytes.Length; i++)
            {
                // If the bytes match the search pattern
                if (CompareData(buffer.Skip(i).Take(searchBytes.Length).ToArray(), searchBytes))
                {
                    // Write the replacement bytes
                    WriteProcessMemory(processHandle, IntPtr.Add(p, i), replaceBytes, replaceBytes.Length, out int bytesWritten);
                    Debug.WriteLine("Patched Successfully");
                    return 2;
                }
            }
            
        }
        return 3;
        Debug.WriteLine("No match found");
    }

    public static void WriteBytes(IntPtr addressToWrite, byte[] bytesToWrite)
    {
        // Get the process handle
        Process process = MemoryScanner.process;
        IntPtr processHandle = process.Handle;

        // Write the byte array to the specified memory address
        int bytesWritten;
        bool success = WriteProcessMemory(processHandle, addressToWrite, bytesToWrite, bytesToWrite.Length, out bytesWritten);

        if (success)
        {
            Debug.WriteLine($"Successfully wrote {bytesWritten} bytes to address {addressToWrite}.");
        }
        else
        {
            Debug.WriteLine($"Failed to write bytes to address {addressToWrite}. Error code: {Marshal.GetLastWin32Error()}");
        }
    }


    static bool CompareData(byte[] data1, byte[] data2)
    {
        if (data1.Length != data2.Length)
            return false;

        for (int i = 0; i < data1.Length; i++)
        {
            if (data1[i] != data2[i])
                return false;
        }

        return true;
    }




}
