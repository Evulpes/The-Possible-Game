using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace The_Possible_Game
{
    //TODO: Add byte scanning.

    class Program
    {
        /// <summary>
        /// The offset for the "death" function.
        /// </summary>
        public const int CALL_RET_OFFSET = 0x3389E;
        
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
            Console.WriteLine("Attempting to patch The Impossible Game.");

            Process p = Process.GetProcessesByName("ImpossibleGame").FirstOrDefault();

            if (p == null)
                throw new Exception("Impossible Game Not Running");

            IntPtr handle = NativeMethods.Processthreadsapi.OpenProcess(0x001F0FFF, false, p.Id);

            if (handle == IntPtr.Zero)
                throw new Exception("Invalid Handle");


            IntPtr callRetAddr = p.MainModule.BaseAddress + CALL_RET_OFFSET;

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
}
