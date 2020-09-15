using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace The_Possible_Game
{
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
        public static int StartAntiDebuggingPatchThread(int pid)
        {

            IntPtr handle = NativeMethods.Processthreadsapi.OpenProcess(NativeMethods.Winnt.ProcessAccessFlags.PROCESS_ALL_ACCESS, false, pid);
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

            IntPtr remoteThreadHandle = NativeMethods.Processthreadsapi.CreateRemoteThread
            (
                handle, IntPtr.Zero, 0, remoteThreadAddr,
                IntPtr.Zero, 0, IntPtr.Zero
            );

            if (remoteThreadHandle == IntPtr.Zero)
                return Marshal.GetLastWin32Error();

            NativeMethods.Handleapi.CloseHandle(remoteThreadHandle);
            NativeMethods.Handleapi.CloseHandle(handle);

            return 0;
        }
        public enum CustomErrors
        {
            PROCESS_NOT_RUNNING = 16000,
        }
    }
}
