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

        private static readonly uint IsDebuggerPresentByte = 0x8F5000;
        /// <summary>
        /// An array of 5 NOP opcodes.
        /// </summary>
        public static readonly byte[] nopSlideArray = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 };

        /// <summary>
        /// The expected bytes to be found at the read address.
        /// </summary>
        public static readonly byte[] expectedOverwriteBytes = new byte[] { 0xe8, 0xad, 0xc6, 0xff, 0xff };

        static void Main()
        {
            Console.WriteLine("The Impossible Game Launcher");

            AntiAntiCheat.StartAntiDebuggingPatchThread();



            Console.WriteLine("Attempting to patch The Impossible Game.");

            Process p = Process.GetProcessesByName("ImpossibleGame").FirstOrDefault();

            if (p == null)
                throw new Exception("Impossible Game not running");

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


            //Patch the coorect bytes to a NOP slide.
            if(!NativeMethods.Memoryapi.WriteProcessMemory(handle, callRetAddr, nopSlideArray, nopSlideArray.Length, out IntPtr _))
                throw new Exception($"Failed to WriteProcessMemory at address {callRetAddr}. Last Error: {Marshal.GetLastWin32Error()}");

            Console.WriteLine("Patched. Press any key to continue");
            Console.ReadKey();
        }
    }
    class AntiAntiCheat
    {
        private static readonly byte[] isDebuggerPresentByteCheck = new byte[]
        {
            0x64, 0xa1, 0x30, 0x00, 0x00, 0x00,         //mov eax, dword ptr fs:[30]
            0x80, 0x78, 0x2, 0x1,                       //cmp byte ptr [eax+2], 0x1
            0x75, 0xFA,                                 //jne 0xFFFFFFC
            0xc6, 0x40, 0x02, 0x00,                     //mov byte ptr [eax+2], 0x0
            0xeb, 0xf2                                  //jmp 0xfffffff4
        };
        public static int StartAntiDebuggingPatchThread()
        {
            int tigPid;
            try
            {
                tigPid = Process.GetProcessesByName("ImpossibleGame")[0].Id;
            }
            catch (IndexOutOfRangeException)
            {
                return (int)CustomErrors.PROCESS_NOT_RUNNING;
            }

            IntPtr handle = NativeMethods.Processthreadsapi.OpenProcess(NativeMethods.Winnt.ProcessAccessFlags.PROCESS_ALL_ACCESS, false, tigPid);
            if (handle == IntPtr.Zero)
                return Marshal.GetLastWin32Error();

            IntPtr remoteThreadAddr = NativeMethods.Memoryapi.VirtualAllocEx
            (
                handle, IntPtr.Zero, (uint)isDebuggerPresentByteCheck.Length, 
                NativeMethods.Winnt.AllocationType.MEM_COMMIT, 
                NativeMethods.Winnt.MemoryProtection.PAGE_EXECUTE_READWRITE
            );
            if (remoteThreadAddr == IntPtr.Zero)
                return Marshal.GetLastWin32Error();
            
            if (!NativeMethods.Memoryapi.WriteProcessMemory(handle, remoteThreadAddr, isDebuggerPresentByteCheck, isDebuggerPresentByteCheck.Length, out _))
                return Marshal.GetLastWin32Error();

            IntPtr remoteThreadHandle = NativeMethods.Memoryapi.CreateRemoteThread
            (
                handle, IntPtr.Zero, 0, remoteThreadAddr, 
                IntPtr.Zero, 0, IntPtr.Zero
            );

            if (remoteThreadHandle == IntPtr.Zero)
                return Marshal.GetLastWin32Error();

            return 0;
        }
        public enum CustomErrors
        {
            #region custom
            PROCESS_NOT_RUNNING = 16000,
            #endregion
        }
    }
}
