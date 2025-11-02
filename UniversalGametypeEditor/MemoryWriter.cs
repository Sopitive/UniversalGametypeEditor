using System;
using System.Diagnostics;
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

    // Import the OpenProcess function from the Windows API
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    // Import VirtualQueryEx to enumerate memory regions
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public UIntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private const uint MEM_COMMIT =0x1000;
    private const uint PAGE_GUARD =0x100;
    private const uint PAGE_NOACCESS =0x01;

    public static void WriteValue(IntPtr addressToWrite, int value)
    {
        // Convert the integer value to a byte array
        byte[] buffer = BitConverter.GetBytes(value);

        // Get the process handle from the MemoryScanner helper
        Process? process = MemoryScanner.process;
        if (process == null)
        {
            Debug.WriteLine("Target process not set in MemoryScanner.");
            return;
        }

        IntPtr processHandle = process.Handle;

        // Write the value to the specified memory address
        bool success = WriteProcessMemory(processHandle, addressToWrite, buffer, buffer.Length, out int bytesWritten);

        if (success)
            Debug.WriteLine("Value written successfully!");
        else
            Debug.WriteLine($"Failed to write the value. Error: {Marshal.GetLastWin32Error()}");
    }

    public static void WriteOpcode()
    {
        // Simple example that patches a fixed offset in a module (kept for backward compatibility)
        var procs = Process.GetProcessesByName("mcc-win64-shipping");
        if (procs == null || procs.Length ==0)
        {
            Debug.WriteLine("Process 'mcc-win64-shipping' not found.");
            return;
        }

        Process process = procs[0];
        IntPtr baseAddress = process.MainModule.BaseAddress;
        const int PROCESS_ALL_ACCESS =0x1F0FFF;
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

        byte[] patch = {0x74 }; // jne -> je (example)
        int memoryOffset =0x5042B1;
        IntPtr memoryAddress = IntPtr.Add(baseAddress, memoryOffset);

        WriteProcessMemory(processHandle, memoryAddress, patch, patch.Length, out _);

        // Restore original after a delay (example)
        byte[] original = {0x75 };
        System.Threading.Thread.Sleep(1000);
        WriteProcessMemory(processHandle, memoryAddress, original, original.Length, out _);
    }

    // Scans the entire process address space (committed readable regions) for the provided AOB and patches the first byte (0x75 ->0xEB)
    public static int WriteOpcode2()
    {
        Process[] processes = Process.GetProcessesByName("MegaloEdit");
        if (processes == null || processes.Length ==0)
        {
            Debug.WriteLine("Process 'MegaloEdit' not found.");
            return 1;
        }

        Process process = processes[0];
        const int PROCESS_ALL_ACCESS =0x1F0FFF;
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

        // AOB to search for
        byte[] searchBytes = {0x75,0x53,0x48,0x8B,0x4D,0xE0,0xC7,0x41,0x38,0x07,0x00,0x00,0x00 };
        byte[] replaceFirstByte = {0xEB }; // JNE(0x75) -> JMP(0xEB)

        const int bufferSize =32768;
        byte[] buffer = new byte[bufferSize];

        IntPtr address = IntPtr.Zero;
        IntPtr mbiSize = new IntPtr(Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));

        while (true)
        {
            IntPtr result = VirtualQueryEx(processHandle, address, out MEMORY_BASIC_INFORMATION mbi, mbiSize);
            if (result == IntPtr.Zero)
                break;

            long regionSize = (long)mbi.RegionSize;

            bool isReadableCommitted = (mbi.State & MEM_COMMIT) !=0 && (mbi.Protect & PAGE_NOACCESS) ==0 && (mbi.Protect & PAGE_GUARD) ==0;

            if (isReadableCommitted)
            {
                long offset =0;
                while (offset < regionSize)
                {
                    IntPtr readAddr = new IntPtr(mbi.BaseAddress.ToInt64() + offset);
                    int bytesToRead = (int)Math.Min(bufferSize, regionSize - offset);

                    bool ok = ReadProcessMemory(processHandle, readAddr, buffer, (uint)bytesToRead, out int bytesRead);
                    if (ok && bytesRead >0)
                    {
                        int scanLimit = bytesRead - searchBytes.Length;
                        for (int i =0; i <= scanLimit; i++)
                        {
                            if (CompareAt(buffer, i, searchBytes))
                            {
                                IntPtr patchAddr = new IntPtr(readAddr.ToInt64() + i);
                                WriteProcessMemory(processHandle, patchAddr, replaceFirstByte, replaceFirstByte.Length, out int bytesWritten);
                                Debug.WriteLine($"Patched Successfully at {patchAddr}");
                                return 2; // patched
                            }
                        }
                    }

                    offset += bytesToRead;
                }
            }

            // Move to next region
            address = new IntPtr(mbi.BaseAddress.ToInt64() + regionSize);
        }

        Debug.WriteLine("No match found");
        return 3;
    }

    public static void WriteBytes(IntPtr addressToWrite, byte[] bytesToWrite)
    {
        Process process = MemoryScanner.process;
        if (process == null)
        {
            Debug.WriteLine("Target process not set in MemoryScanner.");
            return;
        }

        IntPtr processHandle = process.Handle;

        bool success = WriteProcessMemory(processHandle, addressToWrite, bytesToWrite, bytesToWrite.Length, out int bytesWritten);

        if (success)
            Debug.WriteLine($"Successfully wrote {bytesWritten} bytes to address {addressToWrite}.");
        else
            Debug.WriteLine($"Failed to write bytes to address {addressToWrite}. Error code: {Marshal.GetLastWin32Error()}");
    }

    static bool CompareData(byte[] data1, byte[] data2)
    {
        if (data1.Length != data2.Length)
            return false;

        for (int i =0; i < data1.Length; i++)
        {
            if (data1[i] != data2[i])
                return false;
        }

        return true;
    }

    // New helper that compares a pattern at a specific offset without allocations
    static bool CompareAt(byte[] buffer, int offset, byte[] pattern)
    {
        for (int j =0; j < pattern.Length; j++)
        {
            if (buffer[offset + j] != pattern[j])
                return false;
        }
        return true;
    }



}
