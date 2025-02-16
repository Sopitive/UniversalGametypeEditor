using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows;
using UniversalGametypeEditor;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using System.Linq;
using System.Threading;

class MemoryScanner
{
    public static string GetProcessOwner(int processId)
    {
        string query = "Select * From Win32_Process Where ProcessID = " + processId;
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        foreach (ManagementObject obj in processList)
        {
            string[] argList = new string[] { string.Empty, string.Empty };
            int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
            if (returnVal == 0)
            {
                string owner = argList[0];
                return owner;
            }
        }

        return "NO OWNER";
    }

    public static Process? process;
    private static readonly string moduleName = "haloreach.dll";

    private static void GetMCCProcess()
    {
        string processName = "MCC-Win64-Shipping";
        Process[] processArr = Process.GetProcessesByName(processName);

        try
        {
            if (processArr.Length == 0)
            {
                return;
            }
            process = processArr[0];
            foreach (Process proc in processArr)
            {
                string processOwner = GetProcessOwner(proc.Id);
                string currentUserName = Environment.UserName;

                if (processOwner == currentUserName)
                {
                    process = proc;
                }
            }
            if (!process.Responding || process.HasExited)
            {
                throw new Exception("Process not responding or has exited");
            }
        }
        catch (Exception)
        {
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    // VirtualAllocEx is used to allocate memory in the target process.
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

    public static bool ScanPointer(int[] offsets, out int result, out IntPtr address)
    {
        Process[] processes = Process.GetProcessesByName("EasyAntiCheat");
        if (processes.Length > 0)
        {
            result = 0;
            address = IntPtr.Zero;
            return false;
        }

        if (process == null)
        {
            GetMCCProcess();
        }

        if (process == null)
        {
            result = 0;
            address = IntPtr.Zero;
            return false;
        }
        IntPtr processHandle = process.Handle;

        ProcessModule targetModule = null;
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    targetModule = module;
                    break;
                }
            }
        }
        catch (Exception)
        {
            address = (IntPtr)0x0;
            result = 0;
            return false;
        }

        if (targetModule == null)
        {
            address = (IntPtr)0x0;
            result = -1;
            return false;
        }

        IntPtr moduleBaseAddress = targetModule.BaseAddress;
        IntPtr currentAddress = moduleBaseAddress;
        for (int i = 0; i < offsets.Length; i++)
        {
            int offset = offsets[i];
            currentAddress = IntPtr.Add(currentAddress, offset);
            long value = ReadInt64(processHandle, currentAddress);

            if (i < offsets.Length)
            {
                currentAddress = (IntPtr)value;
            }
        }

        int lastOffsetValue = ReadInt32(processHandle, currentAddress);
        result = lastOffsetValue;
        address = currentAddress;
        return true;
    }

    public static long ReadInt64(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[8];
        int bytesRead;
        ReadProcessMemory(processHandle, address, buffer, 8, out bytesRead);
        long value = BitConverter.ToInt64(buffer, 0);
        return value;
    }

    public static short ReadInt16(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[2];
        int bytesRead;
        ReadProcessMemory(processHandle, address, buffer, 2, out bytesRead);
        short value = BitConverter.ToInt16(buffer, 0);
        return value;
    }

    public static int ReadInt32(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[4];
        int bytesRead;
        ReadProcessMemory(processHandle, address, buffer, 4, out bytesRead);
        int value = BitConverter.ToInt32(buffer, 0);
        return value;
    }

    public static IntPtr GetModuleBaseAddress(string moduleName)
    {
        if (process == null)
        {
            GetMCCProcess();
        }

        if (process == null)
        {
            return IntPtr.Zero;
        }
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
        }
        catch (Exception ex) { }

        return IntPtr.Zero;
    }

    public static IntPtr AOBScan(byte[] aobPattern, string moduleName)
    {
        if (process == null)
        {
            GetMCCProcess();
        }

        if (process == null)
        {
            throw new Exception("Failed to get the process.");
        }

        IntPtr moduleBase = GetModuleBaseAddress(moduleName);
        if (moduleBase == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        ProcessModule module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
        if (module == null)
        {
            throw new Exception("Failed to find module.");
        }

        IntPtr moduleEnd = IntPtr.Add(moduleBase, module.ModuleMemorySize);
        IntPtr currentAddress = moduleBase;

        byte[] buffer = new byte[4096];
        int bytesRead;

        while (currentAddress.ToInt64() < moduleEnd.ToInt64())
        {
            if (ReadProcessMemory(process.Handle, currentAddress, buffer, buffer.Length, out bytesRead))
            {
                for (int i = 0; i < bytesRead - aobPattern.Length; i++)
                {
                    bool found = true;
                    for (int j = 0; j < aobPattern.Length; j++)
                    {
                        if (aobPattern[j] != 0x00 && buffer[i + j] != aobPattern[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return IntPtr.Add(currentAddress, i);
                    }
                }
            }

            currentAddress = IntPtr.Add(currentAddress, bytesRead);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Injects shellcode that mimics our AA script.
    /// The shellcode does:
    ///   push rdx
    ///   lea rdx, [rax+rcx*4+0x1E4]
    ///   movabs rax, <outputBuffer>
    ///   mov [rax], rdx
    ///   mov ebx, [rdx]
    ///   pop rdx
    ///   jmp returnAddress
    /// 
    /// The output buffer will receive the computed pointer.
    /// </summary>
    public static void InjectAndRetrievePointer()
    {
        // AOB pattern for injection (points to the mov instruction).
        byte[] aobPattern = new byte[] { 0x8B, 0x9C, 0x88, 0xE4, 0x01, 0x00, 0x00 };
        IntPtr targetAddress = AOBScan(aobPattern, moduleName);
        if (targetAddress == IntPtr.Zero)
        {
            Debug.WriteLine("AOB pattern not found.");
            return;
        }
        Debug.WriteLine($"Found target instruction at 0x{targetAddress.ToInt64():X}");

        // In InjectAndRetrievePointer()

        // Set patchLength to 14 bytes for the absolute JMP implementation.
        int patchLength = 12;

        // Save the original patchLength bytes.
        byte[] originalBytes = new byte[patchLength];
        ReadProcessMemory(process.Handle, targetAddress, originalBytes, originalBytes.Length, out int _);

        // Instead of parsing an embedded jump, simply resume execution after the patched area.
        IntPtr returnAddress = IntPtr.Add(targetAddress, patchLength);
        Debug.WriteLine($"Execution will resume at 0x{returnAddress.ToInt64():X}");

        // Allocate memory for our global pointer.
        IntPtr globalNumbersMemory = VirtualAllocEx(process.Handle, IntPtr.Zero, (uint)IntPtr.Size,
                                                     MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (globalNumbersMemory == IntPtr.Zero)
        {
            Debug.WriteLine("Failed to allocate GlobalNumbers memory.");
            return;
        }
        Debug.WriteLine($"Allocated GlobalNumbers memory at 0x{globalNumbersMemory.ToInt64():X}");

        // First, allocate memory for our shellcode.
        int estimatedShellcodeLength = 100;
        IntPtr shellcodeAddress = VirtualAllocEx(process.Handle, IntPtr.Zero,
                                                 (uint)estimatedShellcodeLength,
                                                 MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (shellcodeAddress == IntPtr.Zero)
        {
            Debug.WriteLine("Failed to allocate shellcode memory.");
            return;
        }
        Debug.WriteLine($"Allocated shellcode memory at 0x{shellcodeAddress.ToInt64():X}");

        // Now build the shellcode with our shellcodeAddress known.
        // The shellcode will replay the overwritten instructions (originalBytes) and then jump to returnAddress.
        byte[] shellcode = BuildShellcode(globalNumbersMemory, returnAddress, originalBytes, shellcodeAddress);
        int shellcodeLength = shellcode.Length;

        // Write shellcode into allocated memory.
        if (!WriteProcessMemory(process.Handle, shellcodeAddress, shellcode, shellcodeLength, out int _))
        {
            Debug.WriteLine("Failed to write shellcode into target process.");
            return;
        }

        // Build a 12-byte patch that loads shellcodeAddress into RDX and jumps to it.
        byte[] jmpPatch = new byte[patchLength];

        // mov rdx, shellcodeAddress
        jmpPatch[0] = 0x48;
        jmpPatch[1] = 0xBA;
        BitConverter.GetBytes(shellcodeAddress.ToInt64()).CopyTo(jmpPatch, 2);

        // jmp rdx
        jmpPatch[10] = 0xFF;
        jmpPatch[11] = 0xE2;

        Debug.WriteLine($"12-byte patch: JMP to shellcode at 0x{shellcodeAddress.ToInt64():X}");

        // Overwrite the target region with our 12-byte patch.
        if (!WriteProcessMemory(process.Handle, targetAddress, jmpPatch, patchLength, out int _))
        {
            Debug.WriteLine("Failed to write jump patch at targetAddress.");
            return;
        }

        // Pause to allow shellcode execution.
        Thread.Sleep(100);

        // [Rest of the code as before...]


        // Read the computed pointer stored at GlobalNumbers memory.
        byte[] pointerBytes = new byte[IntPtr.Size];
        if (!ReadProcessMemory(process.Handle, globalNumbersMemory, pointerBytes, pointerBytes.Length, out int _))
        {
            Debug.WriteLine("Failed to read GlobalNumbers memory.");
        }
        else
        {
            long computedPtr = BitConverter.ToInt64(pointerBytes, 0);
            GlobalNumbersAddress = new IntPtr(computedPtr);
            Debug.WriteLine($"Computed pointer (via injection): 0x{GlobalNumbersAddress.ToInt64():X}");
        }

        // Optionally, restore the original bytes if needed.
        WriteProcessMemory(process.Handle, targetAddress, originalBytes, originalBytes.Length, out int _);
    }


    /// <summary>
    /// Builds shellcode that:
    ///   - push rdx
    ///   - lea rdx, [rax+rcx*4+0x1E4]
    ///   - movabs rax, <globalNumbersMemory>
    ///   - mov [rax], rdx
    ///   - pop rdx
    ///   - replays the original mov instruction (first 7 bytes)
    ///   - then appends a relative JMP to the original destination.
    /// 
    /// This shellcode mimics the original behavior and then continues execution.
    /// </summary>
    /// <param name="globalNumbersMemory">Memory to store the computed pointer.</param>
    /// <param name="returnAddress">Original JMP destination.</param>
    /// <param name="originalBytes">Saved 12 bytes from the target.</param>
    /// <param name="shellcodeAddress">Allocated base address for the shellcode.</param>
    /// <returns>Shellcode as a byte array.</returns>
    // In BuildShellcode(), update the jump to use the new returnAddress.
    private static byte[] BuildShellcode(IntPtr globalNumbersMemory, IntPtr returnAddress, byte[] originalBytes, IntPtr shellcodeAddress)
    {
        var code = new System.Collections.Generic.List<byte>();

        // --- Custom injected code ---
        // push rdx
        code.Add(0x52);

        // lea rdx, [rax+rcx*4+0x1E4]
        code.AddRange(new byte[] { 0x48, 0x8D, 0x94, 0x88, 0xE4, 0x01, 0x00, 0x00 });

        // movabs rax, globalNumbersMemory
        code.Add(0x48);
        code.Add(0xB8);
        code.AddRange(BitConverter.GetBytes(globalNumbersMemory.ToInt64()));

        // mov [rax], rdx
        code.AddRange(new byte[] { 0x48, 0x89, 0x10 });

        // pop rdx
        code.Add(0x5A);

        // --- Append an absolute jump back to (targetAddress + 12) ---
        // mov rdx, returnAddress
        code.Add(0x48);
        code.Add(0xBA);
        code.AddRange(BitConverter.GetBytes(returnAddress.ToInt64()));

        // jmp rdx
        code.Add(0xFF);
        code.Add(0xE2);

        return code.ToArray();
    }





    /// <summary>
    /// Reads a single byte from the target process at the specified address.
    /// </summary>
    public static byte ReadProcessByte(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[1];
        ReadProcessMemory(processHandle, address, buffer, 1, out int bytesRead);
        return buffer[0];
    }

    // (The following SetBreakpoint and RemoveBreakpoint methods are no longer used.)
    public static bool SetBreakpoint(IntPtr processHandle, IntPtr address, out byte originalByte)
    {
        originalByte = ReadProcessByte(processHandle, address);
        byte[] int3 = new byte[] { 0xCC };
        return WriteProcessMemory(processHandle, address, int3, int3.Length, out int _);
    }

    public static bool RemoveBreakpoint(IntPtr processHandle, IntPtr address, byte originalByte)
    {
        byte[] original = new byte[] { originalByte };
        return WriteProcessMemory(processHandle, address, original, original.Length, out int _);
    }

    public static class PrivilegeChecker
    {
        const uint TOKEN_QUERY = 0x0008;
        const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        enum TOKEN_INFORMATION_CLASS { TokenPrivileges = 3 }

        [StructLayout(LayoutKind.Sequential)]
        struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        public static bool HasSeDebugPrivilege()
        {
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_QUERY, out IntPtr tokenHandle))
                return false;

            GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, 0, out uint tokenInfoLength);
            IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, tokenInfo, tokenInfoLength, out tokenInfoLength))
                    return false;

                int privilegeCount = Marshal.ReadInt32(tokenInfo);

                if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out LUID seDebugLuid))
                    return false;

                int offset = sizeof(uint);
                int luidAndAttrSize = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();

                for (int i = 0; i < privilegeCount; i++)
                {
                    IntPtr laaPtr = IntPtr.Add(tokenInfo, offset + i * luidAndAttrSize);
                    LUID_AND_ATTRIBUTES laa = Marshal.PtrToStructure<LUID_AND_ATTRIBUTES>(laaPtr);

                    if (laa.Luid.LowPart == seDebugLuid.LowPart &&
                        laa.Luid.HighPart == seDebugLuid.HighPart &&
                        (laa.Attributes & SE_PRIVILEGE_ENABLED) == SE_PRIVILEGE_ENABLED)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInfo);
                CloseHandle(tokenHandle);
            }
            return false;
        }
    }

    public static int ReadGlobalNumber(int index)
    {
        if (GlobalNumbersAddress == IntPtr.Zero)
        {
            Debug.WriteLine("Global numbers address not set, performing injection to retrieve it.");
            InjectAndRetrievePointer(); // This sets GlobalNumbersAddress.
            if (GlobalNumbersAddress == IntPtr.Zero)
            {
                return -1;
            }
        }

        // For index 0: no offset, index 1: +4, index 2: +8, etc.
        IntPtr address = IntPtr.Add(GlobalNumbersAddress, index * 4);
        return ReadInt32(process.Handle, address);
    }


    public static void WriteGlobalNumber(int index, int value)
    {
        if (GlobalNumbersAddress == IntPtr.Zero)
        {
            Debug.WriteLine("Global numbers address not set, performing injection to retrieve it.");
            InjectAndRetrievePointer(); // This sets GlobalNumbersAddress.
            if (GlobalNumbersAddress == IntPtr.Zero)
            {
                return;
            }
        }

        IntPtr address = IntPtr.Add(GlobalNumbersAddress, index * sizeof(int));
        WriteInt32(process.Handle, address, value);
    }

    public static IntPtr GlobalNumbersAddress { get; private set; } = IntPtr.Zero;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [Flags]
    public enum ThreadAccess : int
    {
        THREAD_ALL_ACCESS = 0x001F03FF
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, [In] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

    public static void WriteInt32(IntPtr processHandle, IntPtr address, int value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        WriteProcessMemory(processHandle, address, buffer, buffer.Length, out _);
    }
}


internal static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);
}
// Replace the current CONTEXT structure with this proper 64-bit definition.
[StructLayout(LayoutKind.Sequential)]
public struct CONTEXT64
{
    public ulong P1Home;
    public ulong P2Home;
    public ulong P3Home;
    public ulong P4Home;
    public ulong P5Home;
    public ulong P6Home;
    public uint ContextFlags;
    public uint MxCsr;
    public ushort SegCs;
    public ushort SegDs;
    public ushort SegEs;
    public ushort SegFs;
    public ushort SegGs;
    public ushort SegSs;
    public uint EFlags;
    public ulong Dr0;
    public ulong Dr1;
    public ulong Dr2;
    public ulong Dr3;
    public ulong Dr6;
    public ulong Dr7;
    public ulong Rax;
    public ulong Rcx;
    public ulong Rdx;
    public ulong Rbx;
    public ulong Rsp;
    public ulong Rbp;
    public ulong Rsi;
    public ulong Rdi;
    public ulong R8;
    public ulong R9;
    public ulong R10;
    public ulong R11;
    public ulong R12;
    public ulong R13;
    public ulong R14;
    public ulong R15;
    public ulong Rip;
    // (Other fields omitted for brevity)
}
