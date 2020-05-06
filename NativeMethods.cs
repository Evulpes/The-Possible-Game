using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace The_Possible_Game
{
    class NativeMethods
    {
        public static class Memoryapi
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
        }
        public static class Processthreadsapi
        {
            [DllImport("Kernel32", SetLastError = true)]
            public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int processId);
        }

    }
}
