#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Concepts.Classes
{
    #region Classes

    /// <summary>
    /// Provides a class which can be used to call machine code.
    /// </summary>
    public class MachineFunction : Common.SuppressedFinalizerDisposable
    {
        /// <summary>
        /// The location of the code to be called.
        /// </summary>
        internal protected System.IntPtr InstructionPointer;

        //Todo, should allow for static machineFunctions...
        public MachineFunction()
            : base(true)
        {

        }

        public override void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.InstructionPointer == System.IntPtr.Zero || false == ShouldDispose) return;

            this.InstructionPointer = System.IntPtr.Zero;

            base.Dispose(disposing);
        }

        //Todo, UnsafeCall
    }

    /// <summary>
    /// Provides the API associated with the requirements of executing machine code.
    /// </summary>
    public class FunctionPointer : MachineFunction
    {
        /// <summary>
        /// The byte code of the instructions the machine should execute
        /// </summary>
        internal protected byte[] Instructions;

        /// <summary>
        /// Allows to allocate the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected virtual void VirtualAllocate()
        {

        }

        /// <summary>
        /// Allows to free the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected virtual void VirtualFree()
        {

        }

        /// <summary>
        /// Allows to protect the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected virtual void VirtualProtect()
        {

        }

        /// <summary>
        /// Sets <see cref="Instructions"/> to one of the given parameters based on the type of determination required.
        /// </summary>
        /// <param name="x86codeBytes"></param>
        /// <param name="x64codeBytes"></param>
        /// <param name="machine"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected virtual void SetInstructions(byte[] x86codeBytes, byte[] x64codeBytes, bool machine = true)
        {
            if (machine ? false == Common.Machine.IsX64() : System.IntPtr.Size == 4)
            {
                Instructions = x86codeBytes;
            }
            else
            {
                Instructions = x64codeBytes;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed || false == disposing || false == ShouldDispose) return;

            Instructions = null;

            VirtualFree();

            base.Dispose(disposing);
        }

        //Invoke
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //void Invoke()
        //{

        //}

        //CallIndirect
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //void CallIndirect()
        //{
        //    Concepts.Classes.CommonIntermediateLanguage.CallIndirect(InstructionPointer);
        //}


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryVirtualAllocate()
        {
            try
            {
                VirtualAllocate();

                return true;
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryVirtualFree()
        {
            try
            {
                VirtualFree();

                return true;
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryVirtualProtect()
        {
            try
            {
                VirtualProtect();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Representes functions which must be protected and unprotected
    /// </summary>
    public abstract class SecureFunctionPointer : FunctionPointer
    {
        /// <summary>
        /// Allows to unprotect the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected abstract void VirtualUnprotect();
    }

    /// <summary>
    /// Describes a function call
    /// </summary>
    public class EntryPoint : FunctionPointer
    {
        //Todo,
        //Epilogues, Prologs

        /// <summary>
        /// The <see cref="System.Type"/> of the parameters
        /// </summary>
        public readonly System.Type[] Parameters;

        /// <summary>
        /// The <see cref="System.Type"/> of the return value
        /// </summary>
        public readonly System.Type ReturnType;

        /// <summary>
        /// The name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Constructs and instance
        /// </summary>
        /// <param name="name">The <see cref="Name"/></param>
        public EntryPoint(string name)
        {
            //Ensure not null or whitespace
            if (string.IsNullOrWhiteSpace(name)) throw new System.InvalidOperationException("name, cannot be null or consist only of whitespace.");

            //Set the name
            Name = name;
        }
    }

    /// <summary>
    /// Represents a class which will automatically use the correct platform invocation calls for the implementation to correctly perform a machine intrinsic
    /// </summary>
    public class IntrinsicFunctionPointer : FunctionPointer
    {
        //should store CodeSize...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualAllocate()
        {
            Intrinsic.Allocator(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualProtect()
        {
            Intrinsic.Protector(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualFree()
        {
            Intrinsic.ReverseAllocator(this);
        }
    }

    /// <summary>
    /// Represents intrinsic functions which must be protected and unprotected.
    /// </summary>
    public class SecureIntrinsicFunctionPointer : SecureFunctionPointer
    {        
        protected internal override void SetInstructions(byte[] x86codeBytes, byte[] x64codeBytes, bool machine = true)
        {
            throw new System.NotImplementedException();
        }

        protected internal override void VirtualProtect()
        {
            throw new System.NotImplementedException();
        }

        protected internal override void VirtualFree()
        {
            throw new System.NotImplementedException();
        }

        internal protected override void VirtualAllocate()
        {
            throw new System.NotImplementedException();
        }

        internal protected override void VirtualUnprotect()
        {
            throw new System.NotImplementedException();
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryVirtualUnprotect()
        {
            try
            {
                TryVirtualUnprotect();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion

    #region Intrinsic

    /// <summary>
    /// Represents a managed intrinsic and the ability to invoke it.
    /// </summary>
    public class Intrinsic : Common.SuppressedFinalizerDisposable
    {
        //Instruction
        internal static System.Action<FunctionPointer> Allocator, Protector, ReverseAllocator;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static Intrinsic()
        {
            if (Allocator != null | Protector != null | ReverseAllocator != null) return;

            if (Common.Extensions.OperatingSystemExtensions.IsWindows)
            {
                //Create the Allocator
                Allocator = (entryPoint) =>
                {
                    int codeSize;

                    if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(entryPoint.Instructions, out codeSize)) return;

                    entryPoint.InstructionPointer = WindowsEntryPoint.VirtualAlloc(
                      entryPoint.InstructionPointer, //Use whatever CodePointer was already
                      new System.UIntPtr((uint)codeSize), //The determined size
                      Media.Concepts.Classes.WindowsEntryPoint.AllocationType.Commit | Media.Concepts.Classes.WindowsEntryPoint.AllocationType.Reserve, //The flags
                      Media.Concepts.Classes.WindowsEntryPoint.MemoryProtection.ExecuteReadWrite //The permissions              
                    );

                    if (entryPoint.InstructionPointer == System.IntPtr.Zero) throw new System.ComponentModel.Win32Exception();

                    // Copy our instructions to the CodePointer
                    System.Runtime.InteropServices.Marshal.Copy(entryPoint.Instructions, 0, entryPoint.InstructionPointer, codeSize);
                };

                //Create the Protector
                Protector = (entryPoint) =>
                {
                    // Change the access of the allocated memory from R/W to Execute
                    uint oldProtection;

                    if (false == WindowsEntryPoint.VirtualProtect(entryPoint.InstructionPointer, (System.IntPtr)entryPoint.Instructions.Length, Media.Concepts.Classes.WindowsEntryPoint.MemoryProtection.Execute, out oldProtection)) throw new System.ComponentModel.Win32Exception();
                };

                //Create the De-Allocator
                ReverseAllocator = (entryPoint) => WindowsEntryPoint.VirtualFree(entryPoint.InstructionPointer, 0, Media.Concepts.Classes.WindowsEntryPoint.FreeType.Release);

            }
            else
            {
                //Create the Allocator
                Allocator = (entryPoint) =>
                {
                    int codeSize;

                    if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(entryPoint.Instructions, out codeSize)) return;

                    entryPoint.InstructionPointer = UnixEntryPoint.Mmap(entryPoint.InstructionPointer, //Use whatever pointer was already set
                        (ulong)codeSize, //The size of the code
                        UnixEntryPoint.MmapProtsExecuteReadWrite, UnixEntryPoint.MmapFlagsAnonymousPrivate, -1, (long)0);

                    if (System.IntPtr.Zero.Equals(entryPoint.InstructionPointer)) throw new System.Security.SecurityException(UnixEntryPoint.GetLastError().ToString());

                    // Copy our instructions to the CodePointer
                    System.Runtime.InteropServices.Marshal.Copy(entryPoint.Instructions, 0, entryPoint.InstructionPointer, codeSize);
                };

                //Create the Protector
                Protector = (entryPoint) =>
                {
                    int register;

                    if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(entryPoint.Instructions, out register)) return;

                    if (false == (0 == (register = UnixEntryPoint.MProtect(entryPoint.InstructionPointer, (ulong)register, UnixEntryPoint.MmapProtsExecuteWrite))))
                    {
                        int lastErrno = UnixEntryPoint.GetLastError();
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Error: mprotect failed to make page at 0x{0} executable! Result: {1}, Errno: {2}", entryPoint.InstructionPointer, register, lastErrno);
#endif

                        throw new System.Security.SecurityException(lastErrno.ToString());
                    }
                };

                //Create the De-Allocator
                ReverseAllocator = (entryPoint) =>
                {
                    int codeSize;

                    if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(entryPoint.Instructions, out codeSize)) return;

                    if (false == (0 == (UnixEntryPoint.Munmap(entryPoint.InstructionPointer, (ulong)codeSize ))))
                    {
                        int lastErrno = UnixEntryPoint.GetLastError();
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Error: munmap failed at 0x{0} Errno: {1}", entryPoint.InstructionPointer, lastErrno);
#endif

                        throw new System.Security.SecurityException(lastErrno.ToString());

                    }
                };
            }
        }

        /// <summary>
        /// The <see cref="FunctionPointer"/> which represents the logical entry point for the intrinsic
        /// </summary>
        internal FunctionPointer EntryPoint;

        /// <summary>
        /// Creates an intrinsic
        /// </summary>
        /// <param name="shouldDispose"></param>
        public Intrinsic(bool shouldDispose)
            : base(shouldDispose)
        {
            EntryPoint = new IntrinsicFunctionPointer();
        }

        /// <summary>
        /// Disposes the intrinsic and any memory required to call it.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (false == disposing || false == ShouldDispose) return;

            base.Dispose(disposing);

            EntryPoint.Dispose();

            EntryPoint = null;
        }

    }    

    #endregion

    #region Platform Support

    /// <summary>
    /// ...
    /// </summary>
    internal class WindowsEntryPoint : FunctionPointer
    {
        //Todo, Check DEP and COM otherwise possibly emulated or Luring

        /// <summary>
        /// The assembly where the functions we need are defined
        /// </summary>
        /// <remarks>
        /// <see href="https://msdn.microsoft.com/en-us/library/ff648663.aspx#c08618429_020">MSDN</see> to understand luring is possible.
        /// </remarks>
        const string Kernel32 = "kernel32.dll";

        /// <summary>
        /// 
        /// </summary>
        [System.Flags]
        internal enum AllocationType : uint
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        /// <summary>
        /// 
        /// </summary>
        [System.Flags]
        internal enum MemoryProtection : uint
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        /// <summary>
        /// 
        /// </summary>
        [System.Flags]
        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpAddress"></param>
        /// <param name="dwSize"></param>
        /// <param name="flAllocationType"></param>
        /// <param name="flProtect"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport(Kernel32, SetLastError = true)]
        internal static extern System.IntPtr VirtualAlloc(System.IntPtr lpAddress, System.UIntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpAddress"></param>
        /// <param name="dwSize"></param>
        /// <param name="dwFreeType"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport(Kernel32)]
        internal static extern bool VirtualFree(System.IntPtr lpAddress, System.UInt32 dwSize, FreeType dwFreeType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpAddress"></param>
        /// <param name="dwSize"></param>
        /// <param name="flAllocationType"></param>
        /// <param name="lpflOldProtect"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport(Kernel32, ExactSpelling = true, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool VirtualProtect(System.IntPtr lpAddress, System.IntPtr dwSize, MemoryProtection flAllocationType, out uint lpflOldProtect);

        /// <summary>
        /// Calls <see cref="VirtualAlloc"/> to ensure <see cref="MemoryProtection.ExecuteReadWrite"/> permission on the <see cref="InstructionPointer"/>.
        /// Throws a <see cref="System.ComponentModel.Win32Exception"/> if the operation was not successful.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualAllocate()
        {
            int codeSize;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(Instructions, out codeSize)) return;

            InstructionPointer = VirtualAlloc(
              InstructionPointer, //Use whatever CodePointer was already
              new System.UIntPtr((uint)codeSize), //The determined size
              AllocationType.Commit | AllocationType.Reserve, //The flags
              MemoryProtection.ExecuteReadWrite //The permissions              
            );

            //If the memory is not executable, read and write than throw an exception indicating why.
            if (System.IntPtr.Zero.Equals(InstructionPointer)) throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

            // Copy our instructions to the CodePointer
            System.Runtime.InteropServices.Marshal.Copy(Instructions, 0, InstructionPointer, codeSize);
        }

        /// <summary>
        /// Calls <see cref="VirtualFree"/> to ensure any memory associated with <see cref="InstructionPointer"/> can be released.
        /// Throws a <see cref="System.ComponentModel.Win32Exception"/> if the operation was not successful.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualFree()
        {
            if (false == VirtualFree(InstructionPointer, 0, FreeType.Release))
            {
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Error: VirtualFree failed at 0x{0} Errno: {1}", InstructionPointer, lastError);
#endif

                throw new System.ComponentModel.Win32Exception(lastError);
            }
        }

        /// <summary>
        /// Calls <see cref="VirtualProtect"/> to ensure <see cref="MemoryProtection.Execute"/> permission on the <see cref="InstructionPointer"/>.
        /// Throws a <see cref="System.ComponentModel.Win32Exception"/> if the operation was not successful.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualProtect()
        {
            // Change the access of the allocated memory to Execute
            uint oldProtection;

            if (false == VirtualProtect(InstructionPointer, (System.IntPtr)Instructions.Length, MemoryProtection.Execute, out oldProtection))
            {
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Error: VirtualProtect failed to make page at 0x{0} executable! MemoryProtection: {1}, Errno: {2}", InstructionPointer, (MemoryProtection)oldProtection, lastError);
#endif

                throw new System.ComponentModel.Win32Exception(lastError);
            }
        }

        //VirtualUnprotect

    }

    /// <summary>
    /// .
    /// </summary>
    internal sealed class UnixEntryPoint : FunctionPointer
    {
        //Todo, Check for chroot otherwise possibly Luring

        internal const int _SC_PAGESIZE = 30;

        internal static int PAGE_SIZE = (int)sysconf.Invoke(null, new object[] { _SC_PAGESIZE });

        /// <summary>
        /// Given a poiner, returns the associated base address with respect to <see cref="PAGE_SIZE"/>
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static System.IntPtr GetPageBaseAddress(System.IntPtr pointer)
        {
            return (System.IntPtr)((int)pointer & ~(PAGE_SIZE - 1));
        }

        static System.Type MonoPosix = System.Type.GetType("Mono.Posix");

        static System.Reflection.Assembly Assembly = MonoPosix.Assembly;

        static System.Type Syscall = Assembly.GetType("Mono.Unix.Native.Syscall");

        //Errno is set after calling these functions.
        //Note that Errno is first read from Marshal.GetLastWin32Error and then converted with NativeConvert.
        //https://github.com/mono/mono/blob/master/mcs/class/Mono.Posix/Mono.Unix.Native/Stdlib.cs#L430

        internal static System.Reflection.MethodInfo mmap = Syscall.GetMethod("mmap");

        internal static System.Reflection.MethodInfo munmap = Syscall.GetMethod("munmap"); 

        internal static System.Reflection.MethodInfo mprotect = Syscall.GetMethod("mprotect"); 

        //internal static System.Reflection.MethodInfo sysconf = Syscall.GetMethod("sysconf"); //public static extern long sysconf (SysconfName name, Errno defaultError);

        internal static System.Reflection.MethodInfo sysconf = Syscall.GetMethod("sysconf");

        internal static System.Reflection.MethodInfo getLastError = Syscall.GetMethod("GetLastError"); 

        //public static long sysconf (SysconfName name)

        static System.Type MmapProts = Assembly.GetType("Mono.Unix.Native.MmapProts");

        static System.Type MmapFlags = Assembly.GetType("Mono.Unix.Native.MmapFlags");

        //MmapProts.Execute
        static int mmapProtsExecute = (int)MmapProts.GetField("PROT_EXEC").GetValue(null);

        //MmapProts.Read
        static int mmapProtsRead = (int)MmapProts.GetField("PROT_READ").GetValue(null);

        //MmapProts.Write
        static int mmapProtsWrite = (int)MmapProts.GetField("PROT_WRITE").GetValue(null);

        internal static int MmapProtsExecuteReadWrite = mmapProtsExecute | mmapProtsRead | mmapProtsWrite;

        internal static int MmapProtsExecuteWrite = mmapProtsExecute | mmapProtsWrite;

        //MmapFlags.Private
        static int mmapFlagsPrivate = (int)MmapFlags.GetField("MAP_PRIVATE").GetValue(null);

        //MmapFlags.Anonymous
        static int mmapFlagsAnonymous = (int)MmapFlags.GetField("MAP_ANONYMOUS").GetValue(null);

        internal static int MmapFlagsAnonymousPrivate = mmapFlagsAnonymous | mmapFlagsPrivate;

        //Create the MmapProts parameter (EXECUTE | READ | WRITE)
        static object mmapProtsExecuteReadWrite = System.Enum.ToObject(MmapProts, MmapProtsExecuteReadWrite);

        //Create the MmapProts parameter (EXECUTE | WRITE)
        static object mmapProtsExecuteWrite = System.Enum.ToObject(MmapProts, MmapProtsExecuteWrite);

        //Create the MmapFlags parameter (ANONYMOUS | PRIVATE)
        static object mmapFlagsAnonymousPrivate = System.Enum.ToObject(MmapFlags, MmapFlagsAnonymousPrivate);

        internal static System.Func<System.IntPtr, ulong, int, int, int, long, System.IntPtr> _Mmap;

        internal static System.Func<System.IntPtr, ulong, int> _Munmap;

        internal static System.Func<System.IntPtr, ulong, int, int> _Mprotect;

        internal static System.Func<int, int> _Sysconf;

        internal static System.Func<int> _GetLastError;

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.IntPtr Mmap(System.IntPtr start, ulong length, int/*MmapProts*/ prot, int/*MmapFlags*/ flags, int fd, long offset)
        {
            return _Mmap(start, length, prot, flags, fd, offset);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Munmap(System.IntPtr start, ulong length)
        {
            return _Munmap(start, length);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int MProtect(System.IntPtr start, ulong len, int /*MmapProts*/ prot)
        {
            return _Mprotect(start, len, prot);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long Sysconf(int/*SysconfName*/ name)
        {
            return _Sysconf(name);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetLastError()
        {
            return _GetLastError();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static UnixEntryPoint()
        {
            // public static extern IntPtr mmap (IntPtr start, ulong length, MmapProts prot, MmapFlags flags, int fd, long offset);
            _Mmap = (System.Func<System.IntPtr, ulong, int, int, int, long, System.IntPtr>)System.Delegate.CreateDelegate(typeof(System.Func<System.IntPtr, ulong, int, int, int, long, System.IntPtr>), mmap);

            //public static extern int munmap (IntPtr start, ulong length);
            _Munmap = (System.Func<System.IntPtr, ulong, int>)System.Delegate.CreateDelegate(typeof(System.Func<System.IntPtr, ulong, int>), munmap);

            //public static long sysconf (SysconfName name)
            _Sysconf = (System.Func<int, int>)System.Delegate.CreateDelegate(typeof(System.Func<int, int>), sysconf);

            //public static extern int mprotect (IntPtr start, ulong len, MmapProts prot);
            _Mprotect = (System.Func<System.IntPtr, ulong, int, int>)System.Delegate.CreateDelegate(typeof(System.Func<System.IntPtr, ulong, int, int>), mprotect);

            //public static Errno GetLastError()
            _GetLastError = (System.Func<int>)System.Delegate.CreateDelegate(typeof(System.Func<int>), getLastError);
        }

        /// <summary>
        /// Calls <see cref="mmap"/> to ensure any memory associated with <see cref="InstructionPointer"/> is allocated
        /// Throws a <see cref="System.Security.SecurityException"/> if the operation was not successful.
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualAllocate()
        {
            int codeSize;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(Instructions, out codeSize)) return;

            InstructionPointer = Mmap(InstructionPointer, (ulong)codeSize, MmapProtsExecuteReadWrite, MmapFlagsAnonymousPrivate, -1, 0);

            if (System.IntPtr.Zero.Equals(InstructionPointer)) throw new System.Security.SecurityException(GetLastError().ToString());

            // Copy our instructions to the CodePointer
            System.Runtime.InteropServices.Marshal.Copy(Instructions, 0, InstructionPointer, codeSize);
        }


        /// <summary>
        /// Calls <see cref="munmap"/> to ensure any memory associated with <see cref="InstructionPointer"/> can be released.
        /// Throws a <see cref="System.Security.SecurityException"/> if the operation was not successful.
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualFree()
        {
            int codeSize;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(Instructions, out codeSize)) return;

            if (false == (0 == Munmap(InstructionPointer, (ulong)codeSize)))
            {
                int lastErrno = GetLastError();
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Error: munmap failed at 0x{0} Errno: {1}", InstructionPointer, lastErrno);
#endif

                throw new System.Security.SecurityException(lastErrno.ToString());

            }
        }

        /// <summary>
        /// Calls <see cref="mprotect"/> to ensure <see cref="mmapProtsExecute"/> permission on the <see cref="InstructionPointer"/>.
        /// Throws a <see cref="System.Security.SecurityException"/> if the operation was not successful.
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualProtect()
        {
            int register;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(Instructions, out register)) return;

            if (false == (0 == (register = MProtect(InstructionPointer, (ulong)register, MmapProtsExecuteWrite))))
            {
                int lastErrno = GetLastError();
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Error: mprotect failed to make page at 0x{0} executable! Result: {1}, Errno: {2}", InstructionPointer, register, lastErrno);
#endif

                throw new System.Security.SecurityException(lastErrno.ToString());
            }
        }

        //VirtualUnprotect
    }

    #endregion

    //SecureIntrinsic

    #region Intrinsics

    //Todo, static class Intrinsics { }

    #region References

    //Actually writes the ASM and the C# Code but then even patches the CLR function to use the ASM..
    //https://github.com/damageboy/ReJit
    //Does have a cool attribute which is laid out better than my enums

    //C# Example
    //https://searchcode.com/codesearch/view/3147326/#

    //http://stackoverflow.com/questions/29642816/is-there-a-way-to-call-the-rdtsc-assembly-instruction-from-c

    //https://github.com/sharpdx/SharpDX/blob/master/Source/SharpDX/SharpJit.cs

    //Go Example
    //https://github.com/klauspost/cpuid/blob/master/cpuid.go

    //C Reference
    //http://wiki.osdev.org/Detecting_CPU_Topology_(80x86)

    //Intel Manual
    //http://bochs.sourceforge.net/techspec/24161821.pdf

    //AMD Manual
    //https://support.amd.com/TechDocs/25481.pdf

    #endregion

    #region CpuId

    /// <summary>
    /// Represents the `cpuid` intrinsic.
    /// </summary>
    /// <remarks>
    /// <see cref="Media.Common.Extensions.Process.ProcessExtensions.SetAffinity"/> if using multiple processors are used in the system.
    /// </remarks>
    public sealed class CpuId : Intrinsic
    {
        #region References

        //https://en.wikipedia.org/wiki/CPUID

        //https://en.wikipedia.org/wiki/Control_register

        //http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.ddi0432c/Bhccjgga.html

        //http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.ddi0301h/Babgbeed.html

        #endregion

        //MCore / Freescale, Arm, MicroFx.

        #region Nested Types

        /// <summary>
        /// Describes the values of the bits within the registers after the execution of cpuid.
        /// </summary>
        public enum CpuIdFeature : int
        {
            ///// <summary>The CPUID Feature</summary>
            //[CpuIdFeatures(0x00, EAX, 0/*, new byte[]{ 0x0F, 0xA2 }*/)]
            //CPUID,
            /// <summary>Streaming SIMD Extensions 3 (SSE3)</summary>
            [CpuIdFeatures(0x01, ECX, 0)]
            SSE3,
            /// <summary>PCLMULQDQ instruction</summary>
            [CpuIdFeatures(0x01, ECX, 1)]
            PCLMULQDQ,
            /// <summary>64-bit DS Area</summary>
            [CpuIdFeatures(0x01, ECX, 2)]
            DTES64,
            /// <summary>MONITOR/MWAIT</summary>
            [CpuIdFeatures(0x01, ECX, 3)]
            Monitor,
            /// <summary>CPL Qualified Debug Store</summary>
            [CpuIdFeatures(0x01, ECX, 4)]
            DSCPL,
            /// <summary>Virtual Machine Extensions</summary>
            [CpuIdFeatures(0x01, ECX, 5)]
            VMX,
            /// <summary>Safer Mode Extensions</summary>
            [CpuIdFeatures(0x01, ECX, 6)]
            SMX,
            /// <summary>Enhanced Intel SpeedStep® technology</summary>
            [CpuIdFeatures(0x01, ECX, 7)]
            EIST,
            /// <summary>Thermal Monitor 2</summary>
            [CpuIdFeatures(0x01, ECX, 8)]
            TM2,
            /// <summary>Supplemental Streaming SIMD Extensions 3 (SSSE3)</summary>
            [CpuIdFeatures(0x01, ECX, 9)]
            SSSE3,
            /// <summary>L1 Context ID</summary>
            [CpuIdFeatures(0x01, ECX, 10)]
            CNXTID,
            /// <summary>FMA extensions</summary>
            [CpuIdFeatures(0x01, ECX, 12)]
            FMA,

            /// <summary>CMPXCHG16B Instruction</summary>
            [CpuIdFeatures(0x01, ECX, 13)]
            CX16,
            /// <summary>xTPR Update Control</summary>
            [CpuIdFeatures(0x01, ECX, 14)]
            XTPRUpdate,
            /// <summary>Perfmon and Debug Capability</summary>
            [CpuIdFeatures(0x01, ECX, 15)]
            PDCM,
            /// <summary>Process-context identifiers</summary>
            [CpuIdFeatures(0x01, ECX, 17)]
            PCID,
            /// <summary>Direct Cache Access</summary>
            [CpuIdFeatures(0x01, ECX, 18)]
            DCA,
            /// <summary>Streaming SIMD Extensions 4.1 (SSE4.1)</summary>
            [CpuIdFeatures(0x01, ECX, 19)]
            SSE41,
            /// <summary>Streaming SIMD Extensions 4.2 (SSE4.2)</summary>
            [CpuIdFeatures(0x01, ECX, 20)]
            SSE42,
            /// <summary>x2APIC</summary>
            [CpuIdFeatures(0x01, ECX, 21)]
            X2APIC,
            /// <summary>MOVBE Instruction</summary>
            [CpuIdFeatures(0x01, ECX, 22)]
            MOVBE,
            /// <summary>POPCNT Instruction</summary>
            [CpuIdFeatures(0x01, ECX, 23)]
            POPCNT,
            /// <summary>APIC timer supports TSC Deadline value</summary>
            [CpuIdFeatures(0x01, ECX, 24)]
            TSCDeadline,
            /// <summary>AESNI Instructions</summary>
            [CpuIdFeatures(0x01, ECX, 25)]
            AESNI,
            /// <summary>XSAVE/XRSTOR processor extended state feature</summary>
            [CpuIdFeatures(0x01, ECX, 26)]
            XSAVE,
            /// <summary>XSETBV/XGETBV instructions to access XCR0,</summary>
            [CpuIdFeatures(0x01, ECX, 27)]
            OSXSAVE,
            /// <summary>AVX</summary>
            [CpuIdFeatures(0x01, ECX, 28)]
            AVX,
            /// <summary>16-bit floating-point conversion</summary>
            [CpuIdFeatures(0x01, ECX, 29)]
            F16C,
            /// <summary>RDRAND Instruction</summary>
            [CpuIdFeatures(0x01, ECX, 30)]
            RDRAND,
            /// <summary>RDSEED Instruction</summary>
            [CpuIdFeatures(0x01, ECX, 31)]
            RDSEED,


            /// <summary>Floating Point Unit On-Chip</summary>
            [CpuIdFeatures(0x01, EDX, 0)]
            FPU,
            /// <summary>Virtual 8086 Mode Enhancements</summary>
            [CpuIdFeatures(0x01, EDX, 1)]
            VME,
            /// <summary>Debugging Extensions</summary>
            [CpuIdFeatures(0x01, EDX, 2)]
            DE,
            /// <summary>Page Size Extension</summary>
            [CpuIdFeatures(0x01, EDX, 3)]
            PSE,
            /// <summary>Time Stamp Counter</summary>
            [CpuIdFeatures(0x01, EDX, 4)]
            TSC,
            /// <summary>Model Specific Registers RDMSR and WRMSR Instructions</summary>
            [CpuIdFeatures(0x01, EDX, 5)]
            MSR,
            /// <summary>Physical Address Extension</summary>
            [CpuIdFeatures(0x01, EDX, 6)]
            PAE,
            /// <summary>Machine Check Exception</summary>
            [CpuIdFeatures(0x01, EDX, 7)]
            MCE,
            /// <summary>CMPXCHG8B Instruction</summary>
            [CpuIdFeatures(0x01, EDX, 8)]
            CX8,
            /// <summary>APIC On-Chip</summary>
            [CpuIdFeatures(0x01, EDX, 9)]
            APIC,
            /// <summary>SYSENTER and SYSEXIT Instructions</summary>
            [CpuIdFeatures(0x01, EDX, 11)]
            SYSENTER,
            /// <summary>Memory Type Range Registers</summary>
            [CpuIdFeatures(0x01, EDX, 12)]
            MTRR,
            /// <summary>Page Global Bit</summary>
            [CpuIdFeatures(0x01, EDX, 13)]
            PGE,
            /// <summary>Machine Check Architecture</summary>
            [CpuIdFeatures(0x01, EDX, 14)]
            MCA,
            /// <summary>Conditional Move Instructions</summary>
            [CpuIdFeatures(0x01, EDX, 15)]
            CMOV,
            /// <summary>Page Attribute Table</summary>
            [CpuIdFeatures(0x01, EDX, 16)]
            PAT,
            /// <summary>36-Bit Page Size Extension</summary>
            [CpuIdFeatures(0x01, EDX, 17)]
            PSE36,
            /// <summary>Processor Serial Number</summary>
            [CpuIdFeatures(0x01, EDX, 18)]
            PSN,
            /// <summary>CLFLUSH Instruction</summary>
            [CpuIdFeatures(0x01, EDX, 19)]
            CLFLUSH,
            /// <summary>Debug Store</summary>
            [CpuIdFeatures(0x01, EDX, 21)]
            DS,
            /// <summary>Thermal Monitor and Software Controlled Clock Facilities</summary>
            [CpuIdFeatures(0x01, EDX, 22)]
            ACPI,
            /// <summary>Intel MMX Technology</summary>
            [CpuIdFeatures(0x01, EDX, 23)]
            MMX,
            /// <summary>FXSAVE and FXRSTOR Instructions</summary>
            [CpuIdFeatures(0x01, EDX, 24)]
            FSXR,
            /// <summary>Streaming SIMD Extensions (SSE)</summary>
            [CpuIdFeatures(0x01, EDX, 25)]
            SSE,
            /// <summary>Streaming SIMD Extensions 2 (SSE2)</summary>
            [CpuIdFeatures(0x01, EDX, 26)]
            SSE2,
            /// <summary>Self Snoop</summary>
            [CpuIdFeatures(0x01, EDX, 27)]
            SS,
            /// <summary>Max APIC IDs reserved field is Valid</summary>
            [CpuIdFeatures(0x01, EDX, 28)]
            HTT,
            /// <summary>Thermal Monitor</summary>
            [CpuIdFeatures(0x01, EDX, 29)]
            TM,
            /// <summary>Pending Break Enable</summary>
            [CpuIdFeatures(0x01, EDX, 31)]
            PBE,


            /// <summary>SYSCALL/SYSENTER in 64 bit mode</summary>
            [CpuIdFeatures(unchecked((int)0x80000001), EDX, 11)]
            SYSCALL64,
            /// <summary>No Execute But</summary>
            [CpuIdFeatures(unchecked((int)0x80000001), EDX, 20)]
            NX,
            /// <summary>1GB Pages</summary>
            [CpuIdFeatures(unchecked((int)0x80000001), EDX, 26)]
            GBP,
            /// <summary>RDTSCP and IA32_TSC_AUX</summary>
            [CpuIdFeatures(unchecked((int)0x80000001), EDX, 27)]
            RDTSCP,
            /// <summary>Intel 64 Architecture available</summary>
            [CpuIdFeatures(unchecked((int)0x80000001), EDX, 29)]
            INTEL64,
            /// <summary>Invariant TSC Available</summary>
            [CpuIdFeatures(unchecked((int)0x80000007), EDX, 8)]
            InvariantTSC,

        }

        /// <summary>
        /// Provides an easy way to describe CpuId features and the bits of the registers they correspond to.
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field)]
        public class CpuIdFeaturesAttribute : System.Attribute
        {
            /// <summary>
            /// Constructs a feature attribute
            /// </summary>
            /// <param name="function"><see cref="Function"/></param>
            /// <param name="register"><see cref="Register"/></param>
            /// <param name="bit"><see cref="Bit"/></param>
            public CpuIdFeaturesAttribute(int function, int register, int bit)
            {
                Function = function;

                Register = register;

                Bit = bit;
            }

            /// <summary>
            /// The function which indicates if the feature is supported
            /// </summary>
            public int Function { get; private set; }

            /// <summary>
            /// The register index to check after calling
            /// </summary>
            public int Register { get; private set; }

            /// <summary>
            /// The bit to check for the feature
            /// </summary>
            public int Bit { get; private set; }

            //OpCodes, Array compliance...
        }

        #endregion

        #region Constants / Statics

        //Todo, it is possibly to either read the register order from a file or to determine it manually.

        //enum Register : int
        //{
        //    Eax,
        //    Ebx,
        //    Ecx,
        //    Edx
        //}

        /// <summary>
        /// The value which represents the EAX register
        /// </summary>
        public const int EAX = 0;

        /// <summary>
        /// The value which represents the EBX register
        /// </summary>
        public const int EBX = 1;

        /// <summary>
        /// The value which represents the ECX register
        /// </summary>
        public const int ECX = 2;

        /// <summary>
        /// The value which represents the EDX register
        /// </summary>
        public const int EDX = 3;

        /// <summary>
        /// Builds the <see cref="FeatureInformation"/> Dictionary
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static CpuId()
        {
            //Todo
            if (Common.Machine.IsArm()) throw new System.NotSupportedException();

            if (FeatureInformation.Count > 0) return;

            foreach (var member in typeof(CpuIdFeature).GetMembers())
            {
                foreach (var attribute in member.GetCustomAttributes(typeof(CpuIdFeaturesAttribute), false))
                {
                    FeatureInformation.Add((CpuIdFeature)System.Enum.Parse(typeof(CpuIdFeature), member.Name), (CpuIdFeaturesAttribute)attribute);
                }
            }
        }

        /// <summary>
        /// Gets a value which indicates if the given feature is supported
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool Supports(CpuIdFeature feature)
        {
            CpuIdFeaturesAttribute informationAttribute;

            if (FeatureInformation.TryGetValue(feature, out informationAttribute))
            {
                //Read the memory which corresponds to the register where the feature information resides.
                return false == (0 == Common.Binary.ReadBits(CpuIdResults[informationAttribute.Function].Array, Common.Binary.BytesPerInteger * informationAttribute.Register + informationAttribute.Bit, 1, false));

                //return 1U == (Common.Binary.Read32(CpuIdResults[informationAttribute.Function], Common.Binary.BytesPerInteger * informationAttribute.Register, false) & (1U << informationAttribute.Bit));

                //return (CpuIdResults[informationAttribute.Function][informationAttribute.Register] & (1U << informationAttribute.Bit)) == 1;
            }

            return false;
        }

        static byte[] CpuIdBuffer = new byte[16];

        static int MaximumFeatureLevel = 0;

        static int MaximumExtendedFeatureLevel = unchecked((int)0x80000000);

        static string VendorString = null;

        static string ProcessorBrandString = null;

        readonly static System.Collections.Generic.Dictionary<int, Common.MemorySegment> CpuIdResults = new System.Collections.Generic.Dictionary<int, Common.MemorySegment>();

        readonly static System.Collections.Generic.Dictionary<CpuIdFeature, CpuIdFeaturesAttribute> FeatureInformation = new System.Collections.Generic.Dictionary<CpuIdFeature, CpuIdFeaturesAttribute>();

        /// <summary>
        /// Gets the processor vendor string
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string GetVendorString()
        {
            if (false == (0 == MaximumFeatureLevel) | false == string.IsNullOrEmpty(VendorString)) return VendorString;

            //Also now in CpuIdResults[0] 4 -> 12 along with MaximumFeatureLevel @ 0

            using (CpuId cpuId = new CpuId())
            {
                //Invoke with the level 
                cpuId.Invoke(MaximumFeatureLevel, ref CpuIdBuffer);

                //Store the result
                CpuIdResults[MaximumFeatureLevel] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);

                //The maximum feature level
                MaximumFeatureLevel = Common.Binary.Read32(CpuIdBuffer, 0, false);

                System.Text.StringBuilder builder = new System.Text.StringBuilder(32);

                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 4, 4));
                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 12, 4));
                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 8, 4));

                //concatenate the ebx, edx, ecx register values after extracting the character data
                return VendorString = builder.ToString();  
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetMaximumFeatureLevel()
        {
            if (false == string.IsNullOrEmpty(VendorString) | false == (0 == MaximumFeatureLevel)) return MaximumFeatureLevel;

            //Also now in CpuIdResults[0] 4 -> 12 along with MaximumFeatureLevel @ 0

            using (CpuId cpuId = new CpuId())
            {
                //Invoke with the level 
                cpuId.Invoke(MaximumFeatureLevel, ref CpuIdBuffer);

                //Store the result
                CpuIdResults[MaximumFeatureLevel] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);

                //Store the new maximum feature level
                MaximumFeatureLevel = Common.Binary.Read32(CpuIdBuffer, 0, false);

                System.Text.StringBuilder builder = new System.Text.StringBuilder(32);

                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 4, 4));
                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 12, 4));
                builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 8, 4));

                //concatenate the ebx, edx, ecx register values after extracting the character data
                VendorString = builder.ToString();            

                return MaximumFeatureLevel;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetMaximumExtendedFeatureLevel()
        {
            if (false == (unchecked((int)0x80000000) == MaximumExtendedFeatureLevel)) return MaximumExtendedFeatureLevel;

            using (CpuId cpuId = new CpuId())
            {
                //Invoke with the level 
                cpuId.Invoke(MaximumExtendedFeatureLevel, ref CpuIdBuffer);

                //Store the result
                CpuIdResults[MaximumExtendedFeatureLevel] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);

                //Store the new MaximumExtendedFeatureLevel
                MaximumExtendedFeatureLevel = Unsafe.UInt32ToInt32Bits(Common.Binary.ReadU32(CpuIdBuffer, 0, false));

                //Get Maximum input value for extended cpuid function.
                //AMD, 80000001 and 1 are the same so this could probably save 16 bytes and the call by comparsing the value returned to MaximumFeatureLevel 
                //AMD & Intel 80000002 -> 80000004 contain any brand string

                //Get all supported extended features
                for (int i = Unsafe.UInt32ToInt32Bits(0x80000001); i <= MaximumExtendedFeatureLevel; i++)
                {
                    //Ensure not already contained somehow.
                    if (CpuIdResults.ContainsKey(i)) continue;

                    //Invoke with the level 
                    cpuId.Invoke(i, ref CpuIdBuffer);

                    //Avoid the duplication of in the dictionary where possibly by aliasing the key
                    if (Unsafe.UInt32ToInt32Bits(Common.Binary.ReadU32(CpuIdBuffer, 0, false)) == MaximumFeatureLevel)
                    {
                        CpuIdResults[i] = CpuIdResults[MaximumFeatureLevel];

                        continue;
                    }

                    //Store the result
                    CpuIdResults[i] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);
                }

                //Return the maximum extended feature level supported
                return MaximumExtendedFeatureLevel;
            }
        }        

        /// <summary>
        /// Gets the Processor Brand String
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string GetProcessorBrandString()
        {
            //If already retried return the the value.
            if (false == string.IsNullOrEmpty(ProcessorBrandString)) return ProcessorBrandString;

            //If the feature isn't support return the VendorString.
            if (GetMaximumExtendedFeatureLevel() == 0) return ProcessorBrandString = GetVendorString();

            System.Text.StringBuilder builder = new System.Text.StringBuilder(96);

            for (int i = Unsafe.UInt32ToInt32Bits(0x80000002); i <= Unsafe.UInt32ToInt32Bits(0x80000004); ++i)
            {
                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[i];

                //Scope the array
                byte[] buffer = registers.Array;

                //Append the eax, ebx, edx, ecx register values after extracting the character data
                builder.Append(System.Text.ASCIIEncoding.ASCII.GetString(buffer, 0, registers.Count));
            }

            //Maybe padded with null octets at the beginning...
            return ProcessorBrandString = builder.ToString();
        }

        /// <summary>
        /// Gets the 64 bits which describe the processor information as retrieved from level 1 if supported , otherwise the value -1.
        /// </summary>
        /// <returns></returns>
        public static long ProcessorInformation()
        {
            if (GetMaximumFeatureLevel() == 0) return -1;

            //Access the memory of the registers previously retrieved
            Common.MemorySegment registers = CpuIdResults[1];

            //Scope the array
            byte[] buffer = registers.Array;

            return Unsafe.UInt64ToInt64Bits(Common.Binary.ReadU64(buffer, 0, false));
        }

        #region Level 1

        //Could probably be accesed from the ProcessorInformation easier than reading the bits

        public static int ProcessorStepping
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 0, 3, false);
            }
        }

        public static int ProcessorModel
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 3, 5, false);
            }
        }

        public static int ProcessorFamily
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 8, 4, false);
            }
        }

        public static int ProcessorType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 12, 2, false);
            }
        }

        public static int Reserved1
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 14, 2, false);
            }
        }

        public static int ExtendedModel
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 16, 3, false);
            }
        }

        public static int ExtendedFamily
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 19, 7, false);
            }
        }

        public static int Reserved2
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {

                if (GetMaximumFeatureLevel() == 0) return -1;

                //Access the memory of the registers previously retrieved
                Common.MemorySegment registers = CpuIdResults[1];

                //Scope the array
                byte[] buffer = registers.Array;

                return (int)Common.Binary.ReadBits(buffer, 26, 6, false);
            }
        }        

        //MaxCores

        //MaxThreads

        //ApicId

        //ProcessorId

        //CoreId

        //ThreadId

        #endregion

        //Cache and TLB

        //ProcessorSerialNumber

        //Deterministic Cache Parameters 

        //Monitor MWait

        //Thermal

        //Direct Cache Access

        //Architectural Performance Monitoring

        //Extended Topology

        #region Feature Support

        public static bool IsHyperThreadingSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.HTT);
            }
        }

        public static bool IsAPICSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.APIC);
            }
        }

        public static bool IsAPIC2Supported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.X2APIC);
            }
        }

        public static bool IsMMXSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.MMX);
            }
        }

        public static bool IsSSESupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.SSE);
            }
        }

        public static bool IsSSE2Supported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.SSE2);
            }
        }

        public static bool IsSSE3Supported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.SSE3);
            }
        }

        public static bool IsSSE41Supported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.SSE41);
            }
        }

        public static bool IsSSE42Supported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.SSE42);
            }
        }

        public static bool IsRdtscPSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.RDTSCP);
            }
        }

        public static bool IsTSCSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.TSC);
            }
        }

        public static bool IsInvariantTSCSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.InvariantTSC);
            }
        }

        public static bool IsRdrandSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.RDRAND);
            }
        }

        public static bool IsRdseedSupported
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Supports(CpuIdFeature.RDSEED);
            }
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.Collections.Generic.IEnumerable<CpuIdFeature> GetSupportedFeatures()
        {
            if (CpuIdResults.Count > 0 | 0 == GetMaximumFeatureLevel()) return FeatureInformation.Keys;

            using (CpuId cpuId = new CpuId())
            {
                //Extract values for supported features: (EAX=<1)
                //EAX => Extended Family, Extended MOdel, Type, Family, Model and Stepping ID (PSN)
                //EBX => Brand Index, CLFUSH line size, Count of logical processors (valid on if Hyper Threading bit is set), Processor local APIC (P4 +)
                //ECX =< Reserved
                //EDX => Feature Flags

                //Get all features
                for (int i = 1; i < MaximumFeatureLevel; i++)
                {
                    cpuId.Invoke(i, ref CpuIdBuffer);

                    CpuIdResults[i] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);
                }

                return FeatureInformation.Keys;
            }
        }

        #endregion

        //Todo, should be unsigned.
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void CpuIdDelegate(int level, byte[] buffer);

        CpuIdDelegate FunctionPointer;        

        byte[] x86CodeBytes = 
        {
            0x55,                   // push        ebp  
            0x8B, 0xEC,             // mov         ebp,esp
            0x53,                   // push        ebx  
            0x57,                   // push        edi

            0x8B, 0x45, 0x08,       // mov         eax, dword ptr [ebp+8] (move level into eax)
            0x0F, 0xA2,             // cpuid

            0x8B, 0x7D, 0x0C,       // mov         edi, dword ptr [ebp+12] (move address of buffer into edi)
            0x89, 0x07,             // mov         dword ptr [edi+0], eax  (write eax, ... to buffer)
            0x89, 0x5F, 0x04,       // mov         dword ptr [edi+4], ebx 
            0x89, 0x4F, 0x08,       // mov         dword ptr [edi+8], ecx 
            0x89, 0x57, 0x0C,       // mov         dword ptr [edi+12],edx 

            0x5F,                   // pop         edi  
            0x5B,                   // pop         ebx  
            0x8B, 0xE5,             // mov         esp,ebp  
            0x5D,                   // pop         ebp 
            0xc3                    // ret
        };

        byte[] x64CodeBytes = 
        {
            0x53,                       // push rbx    this gets clobbered by cpuid

            // rcx is level
            // rdx is buffer.
            // Need to save buffer elsewhere, cpuid overwrites rdx
            // Put buffer in r8, use r8 to reference buffer later.

            // Save rdx (buffer addy) to r8
            0x49, 0x89, 0xd0,           // mov r8,  rdx

            // Move ecx (level) to eax to call cpuid, call cpuid
            0x89, 0xc8,                 // mov eax, ecx
            0x0F, 0xA2,                 // cpuid

            // Write eax et al to buffer
            0x41, 0x89, 0x40, 0x00,     // mov    dword ptr [r8+0],  eax
            0x41, 0x89, 0x58, 0x04,     // mov    dword ptr [r8+4],  ebx
            0x41, 0x89, 0x48, 0x08,     // mov    dword ptr [r8+8],  ecx
            0x41, 0x89, 0x50, 0x0c,     // mov    dword ptr [r8+12], edx

            0x5b,                       // pop rbx
            0xc3                        // ret
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public CpuId(bool machine = false)
            : base(true)
        {
            Compile(machine);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Compile(bool machine = false)
        {
            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

            EntryPoint.VirtualAllocate();

            FunctionPointer = (CpuIdDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(CpuIdDelegate));
        }        

        //Todo, provide a way to call specifying processor desired.  (set affinity)

        //level = featureLevel =< eax
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Invoke(int level, ref byte[] buffer)
        {
            FunctionPointer(level, buffer);
        }

        /// <summary>
        /// Calls cpuid with the given value in the eax register
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public byte[] Invoke(int level)
        {
            byte[] result = new byte[16];

            Invoke(level, ref result);

            return result;
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public byte[] NativeInvoke()
        {
            throw new System.NotImplementedException();

            //byte[] result = new byte[16];

            //Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);

            //return result;
        }
    }

    #endregion

    #region Rtdsc

    /// <summary>
    /// Represents the `rtdsc` intrinsic
    /// </summary>
    public sealed class Rtdsc : Intrinsic
    {
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong GetTimestampUnsigned()
        {
            using (Rtdsc rtdsc = new Rtdsc())
            {
                return rtdsc.NativeInvoke();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long GetTimestamp()
        {
            using (Rtdsc rtdsc = new Rtdsc())
            {
                return rtdsc.Invoke();
            }
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate ulong TimestampDelegate();

        internal TimestampDelegate Timestamp;

        byte[] x86CodeBytes = new byte[] 
            {
                0x0F, 0x31, // rdtsc
                0xC3, // ret
            };

        /// <summary>
        /// Pipeline aware
        /// </summary>
        byte[] x86Rdtscp = new byte[] 
        { 
            0x0F, 0x01, 0xF9, //rdtscp
            0xC3 //ret
        };

        /// <summary>
        /// Serialized with Cpuid
        /// </summary>
        byte[] x86RdtscCpuid = new byte[] 
        { 
           0x53, //push ebx
           0x31, 0xC0, //xor eax,eax
           0x0F, 0xA2, //cpuid
           0x0F, 0x31, // rdtsc
           0x5B, //pop ebx
           0xC3 // ret
        };

        /// <summary>
        /// Raw call
        /// </summary>
        byte[] x64CodeBytes = 
        {
            0x0F, 0x31, // rdtsc
            0x48, 0xC1, 0xE2, 0x20, // shl rdx,20h 
            0x48, 0x0B, 0xC2, // or rax,rdx 
            0xC3, // ret
        };

        /// <summary>
        /// Pipeline aware
        /// </summary>
        byte[] x64Rdtscp = new byte[] { 
            0x0F, 0x01, 0xF9, //rdtscp
            0x48, 0xC1, 0xE2, 0x20, // shl rdx, 20h
            0x48, 0x09, 0xD0, //or rax,rdx
            0xC3 //ret
        };

        /// <summary>
        /// Serialized with Cpuid
        /// </summary>
        byte[] x64RdtscCpuid = new byte[] 
        { 
            0x53, //push rbx
            0x31, 0xC0, //xor eax,eax
            0x0F, 0xA2, //cpuid
            0x0F, 0x31, //rdtsc
            0x48, 0xC1, 0xE2, 0x20, //shl rdx,0x20
            0x48, 0x09, 0xD0, //or rax,rdx
            0x5B, //pop rbx
            0xC3  //ret
        };
        
        //Todo
        //Arm, => Setup access call (needs Epilog, Prolog and possibly auth)

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Compile(bool machine = true)
        {
            //If building for the machine
            if (machine)
            {
                //Not yet supported or does not support
                if (Common.Machine.IsArm() || false == CpuId.Supports(CpuId.CpuIdFeature.InvariantTSC)) throw new System.NotSupportedException();
                //Check for RDTSCP support and use that if possible
                else if (CpuId.Supports(CpuId.CpuIdFeature.RDTSCP)) EntryPoint.SetInstructions(x86Rdtscp, x64Rdtscp, machine);
                //Fallback to RDTSC, could also use cpuid based function to serialize calls
                else EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                EntryPoint.VirtualAllocate();

                EntryPoint.VirtualProtect();

                // Create a delegate to the "function"
                Timestamp = (TimestampDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(TimestampDelegate));

            }
            else
            {
                //Use the fallback
                Timestamp = StopwatchGetTimestamp;

                //Create the pointer for the delegate
                EntryPoint.InstructionPointer = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(Timestamp);
            }
        }

        /// <summary>
        /// Fallback if Rdtsc isn't available
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static ulong StopwatchGetTimestamp()
        {
            return unchecked((ulong)System.Diagnostics.Stopwatch.GetTimestamp());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Rtdsc(bool machine = false)
            : base(true)
        {
            Compile(machine);
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong InvokeUnsigned()
        {
            return Timestamp();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public long Invoke()
        {
            return (long)Timestamp();
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong NativeInvoke()
        {
            return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
        }
    }

    #endregion

    #region Rdrand

    public sealed class Rdrand : Intrinsic
    {
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong GetRandomUnsigned()
        {
            using (Rdrand rtdsc = new Rdrand())
            {
                return rtdsc.NativeInvoke();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long GetRandom()
        {
            using (Rdrand rtdsc = new Rdrand())
            {
                return rtdsc.Invoke();
            }
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate uint RandomDelegate();

        internal RandomDelegate Random;

        //byte[] x86CodeBytes = 
        //{
        //    0x0F, 0xC7, 0x0B, // cmpexchng8b QWORD PTR [ebx]
        //    0xC3 // ret
        //};

        //byte[] x64CodeBytes = new byte[] 
        //{
        //    0x48, //rex.w, long mode only
        //    0x0F, 0xC7, 0x0B,// cmpexchng16b QWORD PTR [rbx]
        //    //0xF0, // lock
        //    0xC3 // ret
        //};

        byte[] x86CodeBytes = new byte[]
        {
            0x0F, 0xC7, 0xF0, // rdrand eax
            //0xC3  //ret 
        };

        byte[] x64CodeBytes = new byte[]
        {
            0x48, 0x0F, 0xC7, 0xF0, // rdrand rax
            //0xC3 // ret
        };


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Rdrand(bool machine = false)
            : base(true)
        {
            if (machine && false == CpuId.Supports(CpuId.CpuIdFeature.RDRAND)) throw new System.NotSupportedException();

            Compile(machine);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Compile(bool machine = false)
        {
            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

            EntryPoint.VirtualAllocate();

            EntryPoint.VirtualProtect();

            // Create a delegate to the "function"
            Random = (RandomDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(RandomDelegate));
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong InvokeUnsigned()
        {
            return Random();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public long Invoke()
        {
            return (long)Random();
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong NativeInvoke()
        {
            return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
        }
    }

    #endregion

    #region Rdseed

    public class RdSeed : Intrinsic
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate uint SeedDelegate();

        internal SeedDelegate Seed;

        byte[] x86CodeBytes = 
        {
            0x0F, 0xC7, 0xF8, // rdseed eax
            //0xC3 // ret
        };

        byte[] x64CodeBytes = new byte[] 
        {
            0x48, 0x0F, 0xC7, 0xF8, // rdseed rax
            //0xC3 // ret
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RdSeed(bool machine = false)
            : base(true)
        {
            if (machine && false == CpuId.Supports(CpuId.CpuIdFeature.RDSEED)) throw new System.NotSupportedException();

            Compile(machine);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Compile(bool machine = false)
        {
            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

            EntryPoint.VirtualAllocate();

            EntryPoint.VirtualProtect();

            // Create a delegate to the "function"
            Seed = (SeedDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(SeedDelegate));
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong InvokeUnsigned()
        {
            return Seed();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public long Invoke()
        {
            return (long)Seed();
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ulong NativeInvoke()
        {
            return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
        }
    }   

    #endregion

    #endregion
}