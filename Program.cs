using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
//See https://github.com/Evulpes/Generic-Bytescan-Library
using Generic_Bytescan_Library;

namespace The_Possible_Game
{
    class Program
    {
        public static readonly byte[] nopSlideArray = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 };
        public static readonly byte[] expectedOverwriteBytes = new byte[] { 0xe8, 0xad, 0xc6, 0xff, 0xff };

        static void Main()
        {
            Console.WriteLine("Attempting to patch The Impossible Game.");
            
            Process p = Process.GetProcessesByName("ImpossibleGame").FirstOrDefault();
            if (p == null)
                throw new Exception("Impossible Game not running");

            int result = AntiAntiCheat.StartAntiDebuggingPatchThread(p.Id);
            if (result != 0)
                Console.WriteLine($"Antidebugging patch failed to apply with error {Marshal.GetLastWin32Error()}");
            
            Console.WriteLine("Antidebugging patch applied!");

            IntPtr handle = NativeMethods.Processthreadsapi.OpenProcess(NativeMethods.Winnt.ProcessAccessFlags.PROCESS_ALL_ACCESS, false, p.Id);
            if (handle == IntPtr.Zero)
                throw new Exception("Invalid Handle");

            ByteScan.FindInBaseModule(p, expectedOverwriteBytes, out IntPtr[] offsets);
            if (offsets.Length == 0)
                throw new Exception("No matching pattern found");

            IntPtr callRetAddr = p.MainModule.BaseAddress + (int)offsets[0];
            byte[] outputBytes = new byte[5];
            
            if(!NativeMethods.Memoryapi.ReadProcessMemory(handle,
                callRetAddr, outputBytes, 5, out IntPtr _))
                throw new Exception($"Failed to ReadProcessMemory at address {callRetAddr}. Last Error: {Marshal.GetLastWin32Error()}");

            if (!outputBytes.SequenceEqual(expectedOverwriteBytes))
                throw new Exception($"Expected bytes not present at address {callRetAddr}");

            Console.WriteLine("Found death function bytes!");

            if (!NativeMethods.Memoryapi.WriteProcessMemory(handle, callRetAddr, nopSlideArray, nopSlideArray.Length, out IntPtr _))
                throw new Exception($"Failed to WriteProcessMemory at address {callRetAddr}. Last Error: {Marshal.GetLastWin32Error()}");

            Console.WriteLine("Patched death function!\n Press any key to continue");
            Console.ReadKey();
        }
    }

}
