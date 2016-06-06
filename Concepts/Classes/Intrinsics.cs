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

namespace Media.Concepts.Hardware
{
    #region Intrinsic

    /// <summary>
    /// Represents a managed intrinsic and the ability to invoke it.    
    /// </summary>
    /// <remarks>
    /// The name of the derived class SHOULD represent the intrinsic you intend to call.
    /// If the intrinsic has variants then name the derivation for the most specific, derive for the variants (naming appropriatley) and check support appropriately.
    /// </remarks>
    public abstract class Intrinsic : Common.SuppressedFinalizerDisposable
    {
        /// <summary>
        /// Described the state of the intrinsic on the current system after calling Compile
        /// </summary>
        public enum IntrinsicState
        {
            Unknown,
            Compiled,
            NotAvailable,
            Available
        }

        #region Statics

        /// <summary>
        /// The functions which are responsible for memory allocation, protection and deletion.
        /// </summary>
        internal static System.Action<FunctionPointerAllocation> Allocator, Protector, ReverseAllocator;

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
                      WindowsEntryPoint.AllocationType.Commit | WindowsEntryPoint.AllocationType.Reserve, //The flags
                      WindowsEntryPoint.MemoryProtection.ExecuteReadWrite //The permissions              
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

                    if (false == WindowsEntryPoint.VirtualProtect(entryPoint.InstructionPointer, (System.IntPtr)entryPoint.Instructions.Length, WindowsEntryPoint.MemoryProtection.Execute, out oldProtection)) throw new System.ComponentModel.Win32Exception();
                };

                //Create the De-Allocator
                ReverseAllocator = (entryPoint) => WindowsEntryPoint.VirtualFree(entryPoint.InstructionPointer, 0, WindowsEntryPoint.FreeType.Release);

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

        #endregion

        #region Fields

        //Todo, could use either PlatformMethodReplacement combined with Delegate fallback.

        /// <summary>
        /// The <see cref="PlatformMethod"/> which represents the logical entry point for the intrinsic
        /// </summary>
        internal PlatformMethod EntryPoint;

        /// <summary>
        /// The state of the intrinsic
        /// </summary>
        internal IntrinsicState State;

        /// <summary>
        /// Cache of the MetadataToken of the type
        /// </summary>
        internal int MetadataToken;

        //Todo, feature check, fallback.

        //Fall back prototype can be used for EntryPoint to create method with exact desired parameters.

        //System.Delegate Fallback;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an intrinsic and the subsequent <see cref="EntryPoint"/>.
        /// </summary>
        /// <param name="shouldDipose">true if the instance should be disposed of when <see cref="Dispose"/> is called.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal Intrinsic(bool shouldDispose) 
            : base(shouldDispose)
        {
            MetadataToken = GetType().MetadataToken;

            EntryPoint = new PlatformMethod(shouldDispose);
        }

        /// <summary>
        /// Creates an instance using an existing <see cref="PlatformMethod"/>
        /// </summary>
        /// <param name="shouldDispose"></param>
        /// <param name="entryPoint">An existing <see cref="PlatformMethod"/></param>
        internal Intrinsic(bool shouldDispose, PlatformMethod entryPoint)
            : base(shouldDispose)
        {
            if(Common.IDisposedExtensions.IsNullOrDisposed(entryPoint)) throw new System.InvalidOperationException("entryPoint, IsNullOrDisposed.");

            MetadataToken = GetType().MetadataToken;

            EntryPoint = entryPoint;
        }

        //Preferred so the delegate can be known and created without having to define it in advance.
        //public Intrinsic(bool shouldDispose, System.Delegate fallback)
        //    : base(shouldDispose)
        //{
        //    MetadataToken = GetType().MetadataToken;

        //    EntryPoint = new PlatformMethod(shouldDispose);
        //}

        #endregion

        #region Methods

        /// <summary>
        /// When implemented in a derived class, allows the caller to compile an intrinsic
        /// </summary>
        /// <param name="machine">Indicates if the compilation is for the current machine</param>
        internal protected abstract void Compile(bool machine = true);
        //{
        //    State = IntrinsicState.Compiled;
        //}

        //Ensure can be called
        //internal protected abstract bool VerifySupport();

        //internal ulong NativeInvoke();

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes the intrinsic and any memory required to call it.
        /// </summary>
        /// <param name="disposing"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (false == disposing || false == ShouldDispose) return;

            base.Dispose(disposing);

            EntryPoint.Dispose();

            EntryPoint = null;
        }

        #endregion
    }

    //Todo, SecureIntrinsic

    #endregion

    #region Platform Support

    //Todo, should derive from PlatformMemoryAllocator

    /// <summary>
    /// ...
    /// </summary>
    internal class WindowsEntryPoint : FunctionPointerAllocation
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal WindowsEntryPoint(bool shouldDispose)
            : base(shouldDispose)
        {

        }

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
    /// ., SystemV EntryPoint
    /// </summary>
    internal class UnixEntryPoint : FunctionPointerAllocation
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal UnixEntryPoint(bool shouldDispose)
            : base(shouldDispose)
        {

        }

        //Todo, Check for chroot otherwise possibly Luring

        internal const int SysconfPageSizeParameter = 30;

        internal static long PAGE_SIZE = Sysconf(SysconfPageSizeParameter);

        /// <summary>
        /// Given a poiner, returns the associated base address with respect to <see cref="PAGE_SIZE"/>
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static System.IntPtr GetPageBaseAddress(System.IntPtr pointer)
        {
            return (System.IntPtr)((long)pointer & ~(PAGE_SIZE - 1));
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

    #region Intrinsics

    public static class Intrinsics
    {
        #region Statics

        /// <summary>
        /// When an intrinsic is compiled it will store it's metadata token and state so that if it's not supported it can be known later on in runtime.
        /// </summary>
        internal static System.Collections.Generic.Dictionary<int, Intrinsic.IntrinsicState> RuntimeIntrinsicInformation = new System.Collections.Generic.Dictionary<int, Intrinsic.IntrinsicState>();

        /// <summary>
        /// Gets any state assoicted with the given Metadatatoken
        /// </summary>
        /// <param name="metadataToken"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static Intrinsic.IntrinsicState GetRuntimeState(int metadataToken)
        {
            Intrinsic.IntrinsicState state;

            Intrinsics.RuntimeIntrinsicInformation.TryGetValue(metadataToken, out state);

            return state;
        }

        /// <summary>
        /// Gets a value which indicates if the intrinsic was previously compiled.
        /// </summary>
        /// <param name="intrinsic"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static bool GetRuntimeState(Intrinsic intrinsic)
        {
            return Intrinsics.RuntimeIntrinsicInformation.TryGetValue(intrinsic.MetadataToken, out intrinsic.State);
        }

        /// <summary>
        /// Sets the runtime state for the given intrinsic
        /// </summary>
        /// <param name="intrinsic"></param>
        /// <param name="state"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void SetRuntimeState(Intrinsic intrinsic, Intrinsic.IntrinsicState state)
        {
            Intrinsics.RuntimeIntrinsicInformation[intrinsic.MetadataToken] = state;
        }

        #endregion

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

        #region ReadCpuType

        /// <summary>
        /// This class cannot be used in user mode.
        /// </summary>
        public sealed class ReadCpuType : Intrinsic
        {
            //http://www.microbe.cz/docs/CPUID.pdf

             #region Constructor

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public ReadCpuType(bool machine = true)
                : base(true)
            {
                Compile(machine);
            }

            #endregion

            [System.Security.SuppressUnmanagedCodeSecurity]
            [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
            internal delegate uint ReadCpuTypeDelegate();

            //Should return == 0 if cpuid is supported
            byte[] x86CodeBytes = new byte[]{
                0x9C, // pushf   ;Save EFLAGS
                0x9C, // pushf   ;Store EFLAGS
                0x81, 0xF4, 0x00, 0x00, 0x20, 0x00, //xor esp,0x200000 ;Invert the ID bit in stored EFLAGS
                0x9D, // popf ;Load stored EFLAGS (with ID bit inverted)
                0x9C, // pushf ;Store EFLAGS again (ID bit may or may not be inverted)
                0x58, // pop eax ;eax = modified EFLAGS (ID bit may or may not be inverted)
                0x09, 0xe0, //or eax,esp    ;eax = whichever bits were changed
                0x9D, //popf  ;Restore original EFLAGS
                0x25, 0x00, 0x00, 0x20, 0x00, //and eax, 0x200000  ;eax = zero if ID bit can't be changed, else non-zero
                0xC3 //ret
            };

            /*
             
            0:  9c                      pushf
            1:  67 8f 00                pop    QWORD PTR [eax]
            4:  89 c8                   mov    eax,ecx
            6:  31 04 25 00 00 20 00    xor    DWORD PTR ds:0x200000,eax
            b:  67 ff 30                push   QWORD PTR [eax]
            e:  9d                      popf
            f:  9c                      pushf
            10: 67 8f 00                pop    QWORD PTR [eax]
            13: 31 c1                   xor    ecx,eax
            15: 75 05                   jne    1c
            17: b8 00 00 00 00          mov    eax,0x0            
            1c: c3                      ret
             
             */
            byte[] x64CodeBytes = new byte[] { 0x9C, 0x67, 0x8F, 0x00, 0x89, 0xC8, 0x31, 0x04, 0x25, 0x00, 0x00, 0x20, 0x00, 0x67, 0xFF, 0x30, 0x9D, 0x9C, 0x67, 0x8F, 0x00, 0x31, 0xC1, 0x75, 0x05, 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 };

            ReadCpuTypeDelegate FunctionPointer;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected override void Compile(bool machine = true)
            {
                //If building for the machine
                if (machine)
                {
                    EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                    EntryPoint.VirtualAllocate();

                    EntryPoint.VirtualProtect();

                    // Create a delegate to the "function"
                    FunctionPointer = (ReadCpuTypeDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(ReadCpuTypeDelegate));

                }
                else throw new System.NotImplementedException();
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal ulong NativeInvoke()
            {
                return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
            }


            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal ulong Invoke()
            {
                return FunctionPointer();
            }
        }

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

            //http://www.microbe.cz/docs/CPUID.pdf

            //http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.ddi0432c/Bhccjgga.html

            //http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.ddi0301h/Babgbeed.html

            //http://thegeekdiary.com/how-to-find-number-of-physicallogical-cpus-cores-and-memory-in-solaris/

            #endregion

            //MCore / Freescale, Arm, MicroFx.

            //Solaris etc

            #region Nested Types

            /// <summary>
            /// Describes the values of the bits within the registers after the execution of cpuid.
            /// </summary>
            public enum CpuIdFeature : int
            {
                //Level 0 = Maximum supported feature level and vendor name.

                /// <summary>The CPUID Feature which indicates the maximum supported level and vendor name.</summary>
                [CpuIdFeatures(0x00, EAX, 0/*, new byte[]{ 0x0F, 0xA2 }*/)]
                CPUID,

                //Level 1 = CPUID Feature Bits

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
                //10 = Reserved
                /// <summary>SYSENTER and SYSEXIT Instructions</summary>
                /// <remarks>SEP Feature bit may be incorrectly set, verify with processor signature</remarks>
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
                //20 = Reserved
                /// <summary>Debug Store</summary>
                [CpuIdFeatures(0x01, EDX, 21)]
                DS,
                /// <summary>Onboard thermal control MSRs for ACPI</summary>
                [CpuIdFeatures(0x01, EDX, 22)]
                ACPI,
                /// <summary>MMX instructions</summary>
                [CpuIdFeatures(0x01, EDX, 23)]
                MMX,
                /// <summary>FXSAVE, FXRESTOR instructions, CR4 bit 9</summary>
                [CpuIdFeatures(0x01, EDX, 24)]
                FSXR,
                /// <summary>SSE instructions (a.k.a. Katmai New Instructions)</summary>
                [CpuIdFeatures(0x01, EDX, 25)]
                SSE,
                /// <summary>SSE2 instructions</summary>
                [CpuIdFeatures(0x01, EDX, 26)]
                SSE2,
                /// <summary>CPU cache supports self-snoop</summary>
                [CpuIdFeatures(0x01, EDX, 27)]
                SS,
                /// <summary>Hyper-threading</summary>
                [CpuIdFeatures(0x01, EDX, 28)]
                HTT,
                /// <summary>Thermal monitor automatically limits temperature</summary>
                [CpuIdFeatures(0x01, EDX, 29)]
                TM,
                /// <summary>IA64 processor emulating x86</summary>
                [CpuIdFeatures(0x01, EDX, 30)]
                IA64,
                /// <summary>Pending Break Enable</summary>
                [CpuIdFeatures(0x01, EDX, 31)]
                PBE,

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
                /// <summary>Silicon Debug Interface</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(0x01, ECX, 11)]
                SDBG,
                /// <summary>FMA extensions</summary>
                /// <remarks>Intel reserved</remarks>
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
                //16 = Reserved
                /// <summary>Process-context identifiers</summary>
                /// <remarks>Intel reserved</remarks>
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
                /// <remarks>Intel reserved</remarks>
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
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(0x01, ECX, 28)]
                AVX,
                /// <summary>16-bit floating-point conversion</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(0x01, ECX, 29)]
                F16C,
                /// <summary>RDRAND Instruction</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(0x01, ECX, 30)]
                RDRAND,
                /// <summary>Running on a hypervisor, always 0 on a real CPU but also with some hypervisors</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(0x01, ECX, 31)]
                HyperVisor,

                //Level 2 is Cache and TLB Descriptor Information

                //Level 3 is the processor serial number

                //Level 4 = Intel Thread / Core Cache Topology

                //Level 7 = Extended Features

                /// <summary>Access to base of %fs and %gs</summary>
                [CpuIdFeatures(0x07, EBX, 0)]
                fsgsbase,
                /// <summary>PCLMULQDQ instruction</summary>
                [CpuIdFeatures(0x07, EBX, 1)]
                IA32_TSC_ADJUST,
                /// <summary>Software Guard Extensions</summary>
                [CpuIdFeatures(0x07, EBX, 2)]
                sgx,
                /// <summary>Bit Manipulation Instruction Set 1</summary>
                [CpuIdFeatures(0x07, EBX, 3)]
                bmi1,
                /// <summary>Transactional Synchronization Extensions</summary>
                [CpuIdFeatures(0x07, EBX, 4)]
                hle,
                /// <summary>Advanced Vector Extensions 2</summary>
                [CpuIdFeatures(0x07, EBX, 5)]
                avx2,
                /// <summary>Reserved</summary>
                [CpuIdFeatures(0x07, EBX, 6)]
                reserved,
                /// <summary>Supervisor-Mode Execution Prevention</summary>
                [CpuIdFeatures(0x07, EBX, 7)]
                smep,
                /// <summary>Bit Manipulation Instruction Set 2</summary>
                [CpuIdFeatures(0x07, EBX, 8)]
                bmi2,
                /// <summary>Enhanced REP MOVSB/STOSB</summary>
                [CpuIdFeatures(0x07, EBX, 9)]
                erms,
                /// <summary>INVPCID instruction</summary>
                [CpuIdFeatures(0x07, EBX, 10)]
                invpcid,
                /// <summary>Platform Quality of Service Monitoring</summary>
                [CpuIdFeatures(0x07, EBX, 11)]
                rtm,
                [CpuIdFeatures(0x07, EBX, 12)]
                pqm,
                /// <summary>FPU CS and FPU DS deprecated</summary>
                [CpuIdFeatures(0x07, EBX, 13)]
                Deprecated,
                /// <summary>Intel MPX (Memory Protection Extensions)</summary>
                [CpuIdFeatures(0x07, EBX, 14)]
                mpx,
                /// <summary>Platform Quality of Service Enforcement</summary>
                [CpuIdFeatures(0x07, EBX, 15)]
                pqe,
                /// <summary>
                /// AVX-512 Foundation
                /// </summary>
                [CpuIdFeatures(0x07, EBX, 16)]
                avx512f,
                /// <summary>AVX-512 Doubleword and Quadword Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 17)]
                avx512dq,
                /// <summary>RDSEED instruction</summary>
                [CpuIdFeatures(0x07, EBX, 18)]
                rdseed,
                /// <summary>Intel ADX (Multi-Precision Add-Carry Instruction Extensions)</summary>
                [CpuIdFeatures(0x07, EBX, 19)]
                adx,
                /// <summary>Supervisor Mode Access Prevention</summary>
                [CpuIdFeatures(0x07, EBX, 20)]
                smap,
                /// <summary>AVX-512 Integer Fused Multiply-Add Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 21)]
                avx512ifma,
                /// <summary>	PCOMMIT instruction</summary>
                [CpuIdFeatures(0x07, EBX, 22)]
                pcommit,
                /// <summary>CLFLUSHOPT instruction</summary>
                [CpuIdFeatures(0x07, EBX, 23)]
                clflushopt,
                /// <summary>	CLWB instruction</summary>
                [CpuIdFeatures(0x07, EBX, 24)]
                clwb,
                /// <summary> Intel Processor Trace</summary>
                [CpuIdFeatures(0x07, EBX, 25)]
                IntelProcessTrace,
                /// <summary>AVX-512 Prefetch Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 26)]
                avx512pf,
                /// <summary>AVX-512 Exponential and Reciprocal Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 27)]
                avx512er,
                /// <summary>AVX-512 Conflict Detection Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 28)]
                avx512cd,
                /// <summary>Intel SHA extensions</summary>
                [CpuIdFeatures(0x07, EBX, 29)]
                sha,
                /// <summary>AVX-512 Byte and Word Instructions</summary>
                [CpuIdFeatures(0x07, EBX, 30)]
                avx512bw,
                /// <summary>AVX-512 Vector Length Extensions </summary>
                [CpuIdFeatures(0x07, EBX, 31)]
                avx512vl,

                /// <summary>PREFETCHWT1 instruction</summary>
                [CpuIdFeatures(0x07, ECX, 0)]
                prefetchwt1,
                /// <summary>AVX-512 Vector Bit Manipulation Instructions</summary>
                [CpuIdFeatures(0x07, ECX, 1)]
                avx512vbmi,
                //All others reserved


                //Extended Processor Info and Feature Bits
                //Level = 0x80000001

                /// <summary>SYSCALL/SYSENTER in 64 bit mode</summary>
                [CpuIdFeatures(2147483647, EDX, 11)]
                SYSCALL64,
                //12 - 19 Reserved
                /// <summary>Executation Disable Bit</summary>
                [CpuIdFeatures(2147483647, EDX, 20)]
                NX,
                //21 - 28 Intel Reserved

                /// <summary>Reserved</summary>
                [CpuIdFeatures(2147483647, EDX, 21)]
                Reserved,
                /// <summary>Extended MMX</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(2147483647, EDX, 22)]
                mmxext,
                /// <summary>MMX instructions</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(2147483647, EDX, 23)]
                mmx,
                /// <summary>FXSAVE, FXRSTOR instructions, CR4 bit 9</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(2147483647, EDX, 24)]
                fxsr,
                /// <summary>FXSAVE/FXRSTOR optimizations</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(2147483647, EDX, 25)]
                fxsr_opt,

                /// <summary>Gibibyte pages</summary>
                /// <remarks>Intel reserved</remarks>
                [CpuIdFeatures(2147483647, EDX, 26)]
                GBP,
                /// <summary>RDTSCP and IA32_TSC_AUX</summary>
                [CpuIdFeatures(2147483647, EDX, 27)]
                RDTSCP,
                /// <summary>Intel 64 Architecture available</summary>
                /// <remarks>Long Mode</remarks>
                [CpuIdFeatures(2147483647, EDX, 29)]
                INTEL64,
                /// <summary>3DNow!</summary>
                /// <remarks>Intel Reserevd</remarks>
                [CpuIdFeatures(2147483647, EDX, 30)]
                _3DNow,
                /// <summary>Extended 3DNow!</summary>
                /// <remarks>Intel Reserevd</remarks>
                [CpuIdFeatures(2147483647, EDX, 31)]
                _3DNowExt,

                //80000005h: L1 Cache and TLB Identifiers
                //80000006h: Extended L2 Cache Features

                //Advanced Power Management Information = 80000007

                /// <summary>Invariant TSC Available</summary>
                [CpuIdFeatures(-2147483641, EDX, 8)]
                InvariantTSC,

                //80000008h: Virtual and Physical address Sizes
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

                //The vendor to which the feature is specfific
                //Vendor

                /// <summary>
                /// An optional mask which can be applied in addition to the bit check for the feature.
                /// </summary>
                //public int Mask { get; private set; }

                //OpCodes, Array compliance...
            }

            #endregion

            #region Constants / Statics

            //Todo, it is possibly to either read the register order from a file or to determine it manually. (lldt) or further from the GDT

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
            /// Indicates if there is a corresponding <see cref="CpuId.CpuIdFeature"/> through the name of the type of the <see cref="Intrinsic"/>.
            /// </summary>
            /// <param name="intrinsic">The intrinsic to check</param>
            /// <returns>True, if the <see cref="Intrinsic"/> is supported.</returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static bool IsSupported(Intrinsic intrinsic)
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(intrinsic) || CpuId.GetMaximumFeatureLevel() == -1) return false;

                CpuId.CpuIdFeature feature;

                if (false == System.Enum.TryParse<CpuId.CpuIdFeature>(intrinsic.GetType().Name, true, out feature)) return false;

                return CpuId.Supports(feature);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static bool IsSupported() { return FeatureInformation.Count > 0 && CpuId.GetMaximumFeatureLevel() >= 0; }

            /// <summary>
            /// Builds the <see cref="FeatureInformation"/> Dictionary
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            static CpuId()
            {
                //Todo
                if (Common.Machine.IsArm())
                {
                    //Todo
                    //FeatureInformation.Add(0, null);
                }

                //If CPUID is not support and the OS is Unix, proc/cpuinfo, dmidecode, and others can be used to get the same information

                if (FeatureInformation.Count > 0) return;

                System.Type CpuIdFeatureType = typeof(CpuIdFeature), CpuIdFeaturesAtttributeType = typeof(CpuIdFeaturesAttribute);

                //Get the TypeInfo
                System.Reflection.TypeInfo typeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(CpuIdFeatureType);

                foreach (System.Reflection.MemberInfo member in typeInfo.GetMembers())
                {
                    foreach (object attribute in member.GetCustomAttributes(CpuIdFeaturesAtttributeType, false))
                    {
                        FeatureInformation.Add((CpuIdFeature)System.Enum.Parse(CpuIdFeatureType, member.Name), (CpuIdFeaturesAttribute)attribute);
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

                //GetSupportedFeatures(feature.IsExtendedFeature

                //GetSupportedFeatures();

                return FeatureInformation.ContainsKey(feature);

                //CpuIdFeaturesAttribute informationAttribute;

                //if (FeatureInformation.TryGetValue(feature, out informationAttribute))
                //{
                //    Common.MemorySegment registerMemory;

                //    if (false == CpuIdResults.TryGetValue(informationAttribute.Function, out registerMemory))
                //    {

                //        return false;

                //        //using(var id = new CpuId())
                //        //{
                //        //    registerMemory = Common.MemorySegment.CreateCopy(id.Invoke(informationAttribute.Function), 0, 16);

                //        //    CpuIdResults[informationAttribute.Function] = registerMemory;
                //        //}
                //    }

                //    //Read the memory which corresponds to the register where the feature information resides.
                //    return false == (0 == Common.Binary.ReadBits(registerMemory.Array, Common.Binary.BytesPerInteger * informationAttribute.Register + informationAttribute.Bit, 1, false));

                //    //return 1U == (Common.Binary.Read32(CpuIdResults[informationAttribute.Function], Common.Binary.BytesPerInteger * informationAttribute.Register, false) & (1U << informationAttribute.Bit));

                //    //return (CpuIdResults[informationAttribute.Function][informationAttribute.Register] & (1U << informationAttribute.Bit)) == 1;
                //}

                //return false;
            }

            static byte[] CpuIdBuffer = new byte[16];

            static int MaximumFeatureLevel = 0;

            static int MaximumExtendedFeatureLevel = -2147483648;

            static string VendorString = null;

            static string ProcessorBrandString = null;

            /// <summary>
            /// Key => First 32 bits is the level, last 32 bits is the subLeaf
            /// Value => Results from cpuid
            /// </summary>
            readonly static System.Collections.Generic.Dictionary<long, Common.MemorySegment> CpuIdResults = new System.Collections.Generic.Dictionary<long, Common.MemorySegment>();

            /// <summary>
            /// 
            /// </summary>
            readonly static System.Collections.Generic.Dictionary<CpuIdFeature, CpuIdFeaturesAttribute> FeatureInformation = new System.Collections.Generic.Dictionary<CpuIdFeature, CpuIdFeaturesAttribute>();

            /// <summary>
            /// Checks for previously retrieved results for the given level and subLeaf
            /// </summary>
            /// <param name="level"></param>
            /// <param name="subLeaf"></param>
            /// <returns>The cached result is present otherwise the result of calling cpuid.</returns>
            internal static Common.MemorySegment RetrieveInformation(int level, int subLeaf = 0)
            {
                //Key by the level
                long key = (long)(((ulong)level << 32) | (uint)subLeaf);

                Common.MemorySegment previous;

                //If the dictionary has the key then return the data already retrieved
                if (CpuIdResults.TryGetValue(key, out previous)) return previous;

                System.Array.Clear(CpuId.CpuIdBuffer, 0, 16);

                //Invoke CPUID
                using (var cpuId = new CpuId())
                {
                    //Invoke with the level and leaf
                    cpuId.Invoke(level, ref CpuId.CpuIdBuffer, subLeaf);

                    //Create a copy of the buffer, Store the result in the CpuIdResults using the key.
                    previous = CpuId.CpuIdResults[key] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);
                }

                //Return the data
                return previous;
            }

            /// <summary>
            /// Vendor string constants
            /// </summary>
            internal class VendorStrings
            {
                internal const string
                    //Actual Hardware
                    GenuineIntel = "GenuineIntel",
                    AMDisbetter_ = "AMDisbetter!", //early engineering samples of AMD K5 processor
                    AuthenticAMD = "AuthenticAMD",
                    CentaurHauls = "CentaurHauls",
                    CyrixInstead = "CyrixInstead",
                    TransmetaCPU = "TransmetaCPU",
                    GenuineTMx86 = "GenuineTMx86",
                    Geode_by_NSC = "Geode by NSC",
                    NexGen = "NexGenDriven",
                    Vortext86_SoC = "Vortex86 SoC",
                    //Virtual Machines
                    KVMKVMKVM = "KVMKVMKVM",
                    Microsoft_Hv = "Microsoft Hv",
                    _lrpepyh_vr = " lrpepyh vr",
                    VMwareVMware = "VMwareVMware",
                    XenVMMXenVMM = "XenVMMXenVMM";
            }

            /// <summary>
            /// Indicates if the execution environment is likely a virtual machine based on the result of <see cref="GetVendorString"/>
            /// </summary>
            /// <returns></returns>
            public static bool IsKnownVirtualMachine()
            {
                switch (GetVendorString())
                {
                    default: return false;
                    case VendorStrings.Microsoft_Hv:
                    case VendorStrings.KVMKVMKVM:
                    case VendorStrings.VMwareVMware:
                    case VendorStrings.XenVMMXenVMM:
                        return true;
                }
            }

            /// <summary>
            /// Gets the processor vendor string
            /// </summary>
            /// <returns></returns>
            public static string GetVendorString()
            {
                if (false == (0 == CpuId.MaximumFeatureLevel) | false == string.IsNullOrEmpty(CpuId.VendorString)) return CpuId.VendorString;

                using (CpuId cpuId = new CpuId())
                {
                    return CpuId.VendorString;
                }
            }

            public static int GetMaximumFeatureLevel()
            {
                if (false == string.IsNullOrEmpty(CpuId.VendorString) | false == (0 == CpuId.MaximumFeatureLevel)) return CpuId.MaximumFeatureLevel;

                using (CpuId cpuId = new CpuId())
                {
                    return CpuId.MaximumFeatureLevel;
                }
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static int GetMaximumExtendedFeatureLevel()
            {
                if (false == (-2147483648 == CpuId.MaximumExtendedFeatureLevel)) return CpuId.MaximumExtendedFeatureLevel;

                using (CpuId cpuId = new CpuId())
                {
                    //Invoke with the level 
                    cpuId.Invoke(MaximumExtendedFeatureLevel, ref CpuId.CpuIdBuffer);

                    //Store the result
                    CpuId.CpuIdResults[MaximumExtendedFeatureLevel] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);

                    //Store the new MaximumExtendedFeatureLevel
                    CpuId.MaximumExtendedFeatureLevel = Common.Binary.Read32(CpuIdBuffer, 0, false);

                    //Return the maximum extended feature level supported
                    return CpuId.MaximumExtendedFeatureLevel;
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
                if (false == string.IsNullOrEmpty(CpuId.ProcessorBrandString)) return CpuId.ProcessorBrandString;

                //If the feature isn't support return the VendorString.
                if (CpuId.GetMaximumExtendedFeatureLevel() == 0) return CpuId.ProcessorBrandString = CpuId.GetVendorString();

                System.Text.StringBuilder builder = new System.Text.StringBuilder(128);

                //Get the extended brand string
                //2147483646 + 4
                for (int i = -2147483646; i <= -2147483644; ++i)
                {
                    //Access the memory of the registers previously retrieved
                    Common.MemorySegment registers = RetrieveInformation(i);

                    //Append the eax, ebx, edx, ecx register values after extracting the character data
                    builder.Append(System.Text.ASCIIEncoding.ASCII.GetString(registers.Array, 0, registers.Count));
                }

                //Maybe padded with null octets at the beginning...
                return CpuId.ProcessorBrandString = builder.ToString();
            }
           
            /// <summary>
            /// Gets the number of threads per core.
            /// </summary>
            /// <returns></returns>
            public static int GetThreadsPerCore()
            {
                int maxFunctionLevel = CpuId.GetMaximumFeatureLevel();

                //if the max function level is less than 4 or the vendor is not Intel then indicate 1.
                if (maxFunctionLevel < 4 || false == (string.Compare(CpuId.GetVendorString(), VendorStrings.GenuineIntel) == 0)) return 1;

                Common.MemorySegment registers;

                int cores;

                //If the maximum feature level is less than 11
                if (maxFunctionLevel < 11)
                {
                    //Access the memory of the registers previously retrieved from the core topology
                    registers = CpuId.RetrieveInformation(1);

                    //Read the bit which indicates if hyper threading is supported
                    if (false == (Common.Binary.ReadBits(registers.Array, 28, 1, false) == 0))
                    {
                        // read the number of cores from the 2nd register
                        cores = (int)Common.Binary.ReadBits(registers.Array, 8, 8, false);

                        //If there is more than 1 logic core
                        if (cores > 1)
                        {
                            //Access the memory of the registers previously retrieved
                            registers = CpuId.RetrieveInformation(4);

                            //read the number of physical cores
                            int physicalCores = (int)Common.Binary.ReadBits(registers.Array, 0, 7, false) + 1;

                            //If that number is > 0 calculate the number of threads per core.
                            if (physicalCores > 0) return cores / physicalCores;
                        }
                    }
                }

                //Access the memory of the registers previously retrieved
                registers = CpuId.RetrieveInformation(11);

                //Read the number of cores
                cores = (int)Common.Binary.ReadBits(registers.Array, 32, 16, false);
                
                //return the amount of cores
                return cores == 0 ? 1 : cores;
            }            

            /// <summary>
            /// Gets the number of cores
            /// </summary>
            /// <returns></returns>
            public static int GetLogicalCores()
            {
                int maximumFeatureLevel = 0;

                //Determine based on the vendor string.
                switch (CpuId.GetVendorString())
                {
                    default: return maximumFeatureLevel;
                    case VendorStrings.AuthenticAMD:
                        {
                            // CPUID.1:EBX[23:16] represents the maximum number of addressable IDs (initial APIC ID)
                            // that can be assigned to logical processors in a physical package.
                            // The value may not be the same as the number of logical processors that are present in the hardware of a physical package.

                            //Access the memory of the registers previously retrieved
                            return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 16, 8, false);                            
                        }
                    case VendorStrings.GenuineIntel:
                        {
                            //Load the maximum feature level
                            maximumFeatureLevel = CpuId.GetMaximumFeatureLevel();

                            //If the maximum supported feature level is less than 11 or the vendor is AMD
                            if (maximumFeatureLevel < 11)
                            {
                                //If the maximum support feature level is less than 1 indicate 0
                                if (maximumFeatureLevel < 1) return 0;

                                //Handle as AMD
                                goto case VendorStrings.AuthenticAMD;
                            } 

                            //subLeaf => 0 = Core, 1 = Thread, 2 = Package
                            return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(11, 1).Array, 32, 16, false);
                        }
                }
            }

            /// <summary>
            /// Get the number of physical cores
            /// </summary>
            /// <returns></returns>
            public static int GetPhysicalCores()
            {
                int maximumExtendedFeatureLevel = 0;

                //Determine based on the vendor string.
                switch (CpuId.GetVendorString())
                {
                    default: return maximumExtendedFeatureLevel;
                    case VendorStrings.AuthenticAMD:
                        {
                            maximumExtendedFeatureLevel = CpuId.GetMaximumExtendedFeatureLevel();

                            if (maximumExtendedFeatureLevel >= -2147483640)
                            {
                                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(-2147483640).Array, 0, 24, false) + 1;
                            }

                            return 0;
                        }
                    case VendorStrings.GenuineIntel:
                        {
                            return CpuId.GetLogicalCores() / CpuId.GetThreadsPerCore();
                        }
                }
            }

            //internal static byte[] GetSignature()
            //{
            //    if (GetMaximumFeatureLevel() < 1) return null;

            //    return RetrieveInformation(1).Array;
            //}

            internal static int GetStepping()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(RetrieveInformation(1).Array, 0, 4, false);
            }

            internal static int GetModelFamily()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(RetrieveInformation(1).Array, 4, 4, false);
            }

            public static int GetModel()
            {
                if (GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(RetrieveInformation(1).Array, 7, 3, false);
            }

            public static int GetFamily()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 8, 4, false);
            }

            public static int GetProcessorType()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 12, 2, false);
            }

            public static int GetExtendedModel()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 16, 3, false);
            }

            public static int GetExtendedFamily()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 20, 7, false);
            }

            internal static int GetExtendedModelFamily()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                return (int)Common.Binary.ReadBits(CpuId.RetrieveInformation(1).Array, 16, 11, false);
            }

            public static int GetDisplayModel()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                int familyModel = Common.Binary.Read32(CpuId.RetrieveInformation(1).Array, 0, false);

                return ((familyModel >> 4) & 0xf) + (familyModel >> 12);
            }

            public static int GetDisplayFamily()
            {
                if (CpuId.GetMaximumFeatureLevel() < 1) return 0;

                int familyModel = Common.Binary.Read32(CpuId.RetrieveInformation(1).Array, 0, false);

                return ((familyModel >> 8) & 0xf) + (familyModel >> 20);
            }

            /// <summary>
            /// Gets the 64 bits which describe the processor information as retrieved from level 1 if supported , otherwise the value -1.
            /// </summary>
            /// <returns></returns>
            public static long ProcessorInformation()
            {
                if (CpuId.GetMaximumFeatureLevel() == -1) return -1;

                return Common.Binary.Read64(CpuId.RetrieveInformation(0).Array, 0, false);
            }

            #region Level 1

            //Could probably be accesed from the ProcessorInformation easier than reading the bits

            public static int ProcessorStepping
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    //Access the memory of the registers previously retrieved
                    Common.MemorySegment registers = CpuIdResults[0];

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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
                    Common.MemorySegment registers = RetrieveInformation(1);

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

            //Cache and TLB => Level 2

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
                    return CpuId.Supports(CpuIdFeature.HTT);
                }
            }

            public static bool IsHyperVisorSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.HyperVisor);
                }
            }

            public static bool IsAPICSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.APIC);
                }
            }

            public static bool IsAPIC2Supported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.X2APIC);
                }
            }

            public static bool IsMMXSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.MMX);
                }
            }

            public static bool IsSSESupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.SSE);
                }
            }

            public static bool IsSSE2Supported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.SSE2);
                }
            }

            public static bool IsSSE3Supported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.SSE3);
                }
            }

            public static bool IsSSE41Supported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.SSE41);
                }
            }

            public static bool IsSSE42Supported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.SSE42);
                }
            }

            public static bool IsRdtscPSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.RDTSCP);
                }
            }

            public static bool IsTSCSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.TSC);
                }
            }

            public static bool IsInvariantTSCSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.InvariantTSC);
                }
            }

            public static bool IsRdrandSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.RDRAND);

                    //Could also test here

                }
            }

            public static bool IsRdseedSupported
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return CpuId.Supports(CpuIdFeature.rdseed);
                }
            }

            #endregion

            /// <summary>
            /// Calls cpuid from 1 -> <see cref="MaximumFeatureLevel"/> and stored the results.
            /// </summary>
            /// <param name="extended">Indicates if the extended features should also be read</param>
            internal static void ReadFeatures(bool extended)
            {
                using (CpuId cpuId = new CpuId())
                {
                    //Extract values for supported features: (EAX=<1)
                    //EAX => Extended Family, Extended MOdel, Type, Family, Model and Stepping ID (PSN)
                    //EBX => Brand Index, CLFUSH line size, Count of logical processors (valid only if Hyper Threading bit is set), Processor local APIC (P4 +)
                    //ECX =< Reserved
                    //EDX => Feature Flags

                    //Get all features
                    for (int i = 1; i < CpuId.MaximumFeatureLevel; i++) CpuId.RetrieveInformation(i);

                    //Get all supported extended features
                    if (extended) for (int i = -2147483648, e = CpuId.GetMaximumExtendedFeatureLevel(); i <= e; i++) CpuId.RetrieveInformation(i);
                }
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static System.Collections.Generic.IEnumerable<CpuIdFeature> GetSupportedFeatures(bool extended = true)
            {
                //Read the features
                CpuId.ReadFeatures(extended);

                System.Type CpuIdFeatureType = typeof(CpuIdFeature), CpuIdFeaturesAtttributeType = typeof(CpuIdFeaturesAttribute);

                //Get the TypeInfo
                System.Reflection.TypeInfo typeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(CpuIdFeatureType);

                //Test for the features.
                foreach (System.Reflection.MemberInfo member in typeInfo.GetMembers())
                {
                    foreach (object attrib in member.GetCustomAttributes(CpuIdFeaturesAtttributeType, false))
                    {
                        CpuIdFeaturesAttribute attribute = (CpuIdFeaturesAttribute)attrib;

                        //CPUID etc.
                        if (attribute.Function == 0) continue;

                        //If the feature is not supported remove it
                        if (false == (0 == Common.Binary.ReadBits(CpuId.RetrieveInformation(attribute.Function).Array, Common.Binary.BytesPerInteger * attribute.Register + attribute.Bit, 1, false)))
                        {
                            FeatureInformation.Remove((CpuIdFeature)System.Enum.Parse(CpuIdFeatureType, member.Name));

                            continue;
                        }

                        //Add or update the feature
                        FeatureInformation[(CpuIdFeature)System.Enum.Parse(CpuIdFeatureType, member.Name)] = attribute;
                    }
                }

                //Return the keys
                return FeatureInformation.Keys;
            }

            /// <summary>
            /// Contains the buffer which receives the results and contains the level and subLeaf to invoke.
            /// Register order is are Eax, Ebx, Ecx, Edx
            /// </summary>
            /// <param name="buffer"></param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            static void Fallback(byte[] buffer)
            {
                //
            }

            #endregion


            /// <summary>
            ///// Provides the signature which is required to call cpuid or cpuidex.
            /// </summary>
            /// <param name="buffer">The input and output buffer</param>
            /// <remarks>Data is given for level and subLeaf by writing values in the correct offsets.</remarks>
            [System.Security.SuppressUnmanagedCodeSecurity]
            [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
            internal delegate void CpuIdExDelegate(byte[] buffer);          

            #region Fields

            CpuIdExDelegate FunctionPointer;           

            //edi = buffer
            byte[] x86CodeBytes = new byte[] { 
                                        //Make a new call frame
                 0x55,                  // push        ebp  
                 0x8B, 0xEC,            // mov         ebp,esp
                                        //Setup parameters                 
                 0x8B, 0x07,            // mov    eax,DWORD PTR [edi] (write buffer 0 to the eax)
                 0x8B, 0x5F, 0x04,      // mov    ebx,DWORD PTR [edi+4] (write buffer 4 to the ebx)
                 0x8B, 0x4F, 0x08,      // mov    ecx,DWORD PTR [edi+8] (write buffer 8 to the ecx)
                 0x8B, 0x57, 0x0C,      // mov    edx,DWORD PTR [edi+12] (write buffer 12 to the edx)
                 0x0F, 0xA2,            // cpuid
                 0x8B, 0x7D, 0x0C,      // mov  edi, dword ptr [ebp+12] (move address of buffer into edi)
                 0x89, 0x07,            // mov  dword ptr [edi], eax  (write eax, ... to buffer)
                 0x89, 0x5F, 0x04,      // mov  dword ptr [edi+4], ebx 
                 0x89, 0x4F, 0x08,      // mov  dword ptr [edi+8], ecx 
                 0x89, 0x57, 0x0C,      // mov  dword ptr [edi+12],edx 
                                        //Restore frame
                 0x89, 0xEC,            // mov  esp,ebp  
                 0x5D,                  // pop  ebp 
                 0xC3                   // ret
            };


            // rcx is buffer
            byte[] x64CodeBytes = {                                         
                0x53,                   // push   rbx (ebx is the lower half of rbx)            
                                        //Save buffer address to r9
                0x49, 0x89, 0xC9,       // mov    r9,rcx
                                        //Setup parameters
                0x41, 0x8B, 0x01,       // mov    eax,DWORD PTR [r9] (move level to eax from buffer)
                0x41, 0x8B, 0x59, 0x04, // mov    ebx,DWORD PTR [r9+0x4] (move extra level to ebx from buffer)
                0x41, 0x8B, 0x49, 0x08, // mov    ecx,DWORD PTR [r9+0x8] (move sub level to ecx from buffer)
                0x41, 0x8B, 0x51, 0x0c, // mov    edx,DWORD PTR [r9+0xc] (move last level to edx from buffer)
                0x0F, 0xA2,             // cpuid
                0x41, 0x89, 0x01,       // mov    DWORD PTR [r9],eax (store result of eax to buffer[0])
                0x41, 0x89, 0x59, 0x04, // mov    DWORD PTR [r9+0x4],ebx (store result of eax to buffer[4])
                0x41, 0x89, 0x49, 0x08, // mov    DWORD PTR [r9+0x8],ecx (store result of eax to buffer[8])
                0x41, 0x89, 0x51, 0x0C, // mov    DWORD PTR [r9+0xc],edx (store result of eax to buffer[12])
                0x5B,                   //pop rbx (restore rbx)
                0xC3                    // ret
            };

            #endregion

            #region Constructor

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CpuId(bool machine = true, bool shouldDispose = true)
                : base(shouldDispose)
            {
                if (false == Intrinsics.GetRuntimeState(this) && machine && false == CpuId.Supports(CpuId.CpuIdFeature.CPUID)) throw new System.NotSupportedException();

                Compile(machine);
            }

            #endregion

            #region Methods

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected override void Compile(bool machine = false)
            {
                //Determine based on the State
                switch (State)
                {
                    case IntrinsicState.NotAvailable:
                        {
                            //Create the delegate to use the fallback
                            FunctionPointer = Fallback;

                            //Allocate a new EntryPoint
                            EntryPoint = new PlatformMethod();

                            //Setup the InstructionPointer
                            EntryPoint.InstructionPointer = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(FunctionPointer);

                            //Indicate not supported
                            MaximumFeatureLevel = -1;

                            break;
                        }
                    //Successully ran before
                    case IntrinsicState.Unknown:
                    case IntrinsicState.Available:
                        {
                            //Use the faster code
                            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                            //Allocate the memory and allow execution
                            EntryPoint.VirtualAllocate();

                            EntryPoint.VirtualProtect();

                            // Create a delegate to the "function"
                            FunctionPointer = (CpuIdExDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(CpuIdExDelegate));

                            if (State == IntrinsicState.Unknown)
                            {
                                //Set to compiled
                                State = IntrinsicState.Compiled;

                                try
                                {
                                    //Invoke with the level 0 to the static buffer, subleaf 0
                                    FunctionPointer(CpuIdBuffer);

                                    //Store the result
                                    CpuIdResults[0] = Common.MemorySegment.CreateCopy(CpuIdBuffer, 0, 16);

                                    //Store the new maximum feature level
                                    MaximumFeatureLevel = Common.Binary.Read32(CpuIdBuffer, 0, false);

                                    System.Text.StringBuilder builder = new System.Text.StringBuilder(32);

                                    builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 4, 4));
                                    builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 12, 4));
                                    builder.Append(System.Text.ASCIIEncoding.ASCII.GetChars(CpuIdBuffer, 8, 4));

                                    //concatenate the ebx, edx, ecx register values after extracting the character data
                                    VendorString = builder.ToString();

                                    //Indicate available.
                                    Intrinsics.SetRuntimeState(this, State = IntrinsicState.Available);
                                }
                                catch
                                {
                                    Intrinsics.SetRuntimeState(this, State = IntrinsicState.NotAvailable);

                                    //SEH, Dispose the function
                                    EntryPoint.Dispose();

                                    //Set the delegate to null
                                    FunctionPointer = null;
                                }

                                //If the function is not available then allocate the fallback
                                if (State == IntrinsicState.NotAvailable) goto case IntrinsicState.NotAvailable;
                            }

                            break;
                        }
                }

            }

            //Todo, provide a way to call specifying processor desired.  (set affinity)

            //level = featureLevel =< eax
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Invoke(int level, ref byte[] buffer, int subLeaf = 0)
            {
                int length;

                if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(buffer, out length)) throw new System.ArgumentNullException("buffer");
                else if (length < 16) throw new System.InvalidOperationException("buffer must have room to store at least 4 32 bit values.");

                //eax
                Common.Binary.Write32(buffer, 0, false, level);

                //ebx
                //Unused

                //ecx
                if (subLeaf > 0) Common.Binary.Write32(buffer, 8, false, subLeaf);

                //edx
                //Unused

                FunctionPointer(buffer);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Invoke(ref byte[] buffer)
            {
                int length;

                if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(buffer, out length)) throw new System.ArgumentNullException("buffer");
                else if (length < 16) throw new System.InvalidOperationException("buffer must have room to store at least 4 32 bit values.");

                FunctionPointer(buffer);
            }

            /// <summary>
            /// Calls cpuid with the given value in the eax register
            /// </summary>
            /// <param name="level"></param>
            /// <returns></returns>
            public byte[] Invoke(int level, int subLeaf = 0)
            {
                //The input and result buffer
                byte[] buffer = new byte[16];


                //eax
                Common.Binary.Write32(buffer, 0, false, level);

                //ebx
                //Unused

                //ecx
                if (subLeaf > 0) Common.Binary.Write32(buffer, 8, false, subLeaf);

                //edx
                //Unused

                Invoke(level, ref buffer, subLeaf);

                return buffer;
            }

            //[System.CLSCompliant(false)]
            //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //public byte[] NativeInvoke()
            //{
            //    throw new System.NotImplementedException();

            //    //byte[] result = new byte[16];

            //    //Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);

            //    //return result;
            //}

            #endregion
        }

        #endregion

        #region Rtdsc

        /// <summary>
        /// Represents the `rtdsc` intrinsic
        /// </summary>
        public sealed class Rtdsc : Intrinsic
        {
            /// <summary>
            /// Fallback if Rdtsc isn't available
            /// </summary>
            /// <returns></returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            static ulong Fallback()
            {
                return unchecked((ulong)System.Diagnostics.Stopwatch.GetTimestamp());
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            [System.CLSCompliant(false)]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static ulong GetTimestampUnsigned()
            {
                using (Rtdsc rtdsc = new Rtdsc())
                {
                    return rtdsc.ReadTimestampCounterUnsigned();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static long GetTimestamp()
            {
                using (Rtdsc rtdsc = new Rtdsc())
                {
                    return rtdsc.ReadTimestampCounter();
                }
            }

            [System.Security.SuppressUnmanagedCodeSecurity]
            [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
            internal delegate ulong TimestampDelegate();

            internal TimestampDelegate ReadTimestampDelegate;

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

            //http://stackoverflow.com/questions/17401914/why-should-i-use-rdtsc-diferently-on-x86-and-x86-x64
            //In x86-64 mode, RDTSC also clears the higher 32 bits of RAX. To compensate those bits we have to shift hi left by 32 bits.

            //These functions return 64 bit values given the definition of the delegate so the return value is already 64 bits.

            /// <summary>
            /// Raw call
            /// </summary>
            byte[] x64CodeBytes = 
            {
                0x0F, 0x31, // rdtsc
                //0x48, 0xC1, 0xE2, 0x20, // shl rdx,20h 
                //0x48, 0x0B, 0xC2, // or rax,rdx 
                0xC3, // ret
            };

            /// <summary>
            /// Pipeline aware
            /// </summary>
            byte[] x64Rdtscp = new byte[] 
            { 
                0x0F, 0x01, 0xF9, //rdtscp
                //0x48, 0xC1, 0xE2, 0x20, // shl rdx, 20h
                //0x48, 0x09, 0xD0, //or rax,rdx
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
                //0x48, 0xC1, 0xE2, 0x20, //shl rdx,0x20
                //0x48, 0x09, 0xD0, //or rax,rdx
                0x5B, //pop rbx
                0xC3  //ret
            };

            //Todo
            //Arm, => Setup access call (needs Epilog, Prolog and possibly auth)

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Rtdsc(bool machine = true)
                : base(true)
            {
                Compile(machine);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected override void Compile(bool machine = true)
            {
                //If building for the machine
                if (machine)
                {
                    //Not yet supported or does not support
                    if (Common.Machine.IsArm() || false == CpuId.Supports(CpuId.CpuIdFeature.InvariantTSC)) throw new System.NotSupportedException();
                    //Check for RDTSCP support and use that if possible (Ia32TscAux returns the IA32_TSC_AUX part of the RDTSCP.)
                    //This variable is OS dependent, but on Linux contains information about the current cpu/core the code is running on.
                    else if (CpuId.Supports(CpuId.CpuIdFeature.RDTSCP)) EntryPoint.SetInstructions(x86Rdtscp, x64Rdtscp, machine);
                    //Fallback to RDTSC, could also use cpuid based function to serialize calls
                    else EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                    EntryPoint.VirtualAllocate();

                    EntryPoint.VirtualProtect();

                    // Create a delegate to the "function"
                    ReadTimestampDelegate = (TimestampDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(EntryPoint.InstructionPointer, typeof(TimestampDelegate));
                }
                else
                {
                    //Use the fallback
                    ReadTimestampDelegate = Fallback;

                    //Create the pointer for the delegate
                    EntryPoint.InstructionPointer = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(ReadTimestampDelegate);
                }
            }            

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public long ReadTimestampCounter()
            {
                return (long)ReadTimestampDelegate();
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal ulong ReadTimestampCounterUnsigned()
            {
                return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
            }
        }

        #endregion

        #region Rdrand

        /// <summary>
        /// Provides and implementation of the rdrand intrinsic
        /// </summary>
        public sealed class Rdrand : Intrinsic
        {
            #region References

            //https://software.intel.com/sites/default/files/m/d/4/1/d/8/441_Intel_R__DRNG_Software_Implementation_Guide_final_Aug7.pdf

            //https://github.com/viathefalcon/rdrand_msvc_2010/blob/master/RdRandStatic/ia_rdrand.cpp

            //https://en.wikipedia.org/wiki/RdRand

            #endregion

            internal const float MaxValue = 4294967295f;

            /// <summary>
            /// The delegate used to invoke the intrinsic
            /// </summary>
            /// <param name="status"></param>
            /// <returns></returns>
            [System.Security.SuppressUnmanagedCodeSecurity]
            [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
            internal delegate ulong RandNative([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)] ref byte status); //ref instead of out because of fallback

            #region Statics

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static ulong GetRandom64()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static long GetRandom63()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (long)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static uint GetRandom32()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (uint)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static int GetRandom31()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (int)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static ushort GetRandom16()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (ushort)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static short GetRandom15()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (short)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static byte GetRandom8()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (byte)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static sbyte GetRandom7()
            {
                using (Rdrand rtdsc = new Rdrand())
                {
                    return (sbyte)rtdsc.GenerateRandom();
                }
            }

            /// <summary>
            /// For fallback use only.
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            static ulong Fallback(ref byte s)
            {
                //s = 0;
                
                return Hardware.Intrinsics.Rtdsc.GetTimestampUnsigned();
            }

            #endregion

            #region Fields

            /// <summary>
            /// The instance delegate
            /// </summary>
            internal RandNative RandomDelegate;

            /// <summary>
            /// Calls rdrand on the rax
            /// </summary>
            byte[] x64CodeBytes = new byte[]{
                0x48, 0x0F, 0xC7, 0xF0, // rexw rdrand rax 
                0x67, 0x0F, 0x92, 0x01, // setb BYTE PTR [rcx] (set rcx = 1 when carry was set)
                0xC3  // ret
            };

            /// <summary>
            /// Calls rdrand on the eax
            /// </summary>
            byte[] x86CodeBytes = new byte[]{
                0x0F, 0xC7, 0xF0, // rdrand eax 
                0x0F, 0x92, 0x01, // setb BYTE PTR [ecx] (set ecx = 1 when carry was set)
                0xC3  // ret
            };

            #endregion

            #region Constructor

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Rdrand(bool machine = true, bool shouldDispose = true)
                : base(shouldDispose)
            {
                //if (machine && false == CpuId.Supports(CpuId.CpuIdFeature.RDRAND)) throw new System.NotSupportedException();

                //Intrinsics.CompiledIntrinsics.TryGetValue(GetType().MetadataToken, out State);

                if (false == Intrinsics.GetRuntimeState(this) && machine && false == CpuId.Supports(CpuId.CpuIdFeature.RDRAND)) throw new System.NotSupportedException();

                //Determine based on the CpuInfo Feature Bit
                State = (Common.Binary.Read32(Intrinsics.CpuId.RetrieveInformation(1).Array, 12, false) & 0x40000000) == 0x40000000 ? IntrinsicState.Available : IntrinsicState.NotAvailable;

                Compile(machine);
            }

            #endregion

            #region Methods

            /// <summary>
            /// The first time the function is compiled a special version of byte code is used to ensure that misdetection of the feature is not possible.
            /// </summary>
            /// <param name="machine"></param>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected override void Compile(bool machine = true)
            {
                //Determine based on the State
                switch (State)
                {
                    case IntrinsicState.NotAvailable:
                        {
                            //Create the delegate to use the fallback RDTSC
                            RandomDelegate = Fallback;

                            //Allocate a new EntryPoint
                            EntryPoint = new PlatformMethod(ShouldDispose);

                            //Setup the InstructionPointer
                            EntryPoint.InstructionPointer = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(RandomDelegate);

                            break;
                        }
                    //Successully ran before
                    case IntrinsicState.Unknown:
                    case IntrinsicState.Available:
                        {
                            //Use the faster code
                            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                            //Allocate the memory and allow execution
                            EntryPoint.VirtualAllocate();

                            EntryPoint.VirtualProtect();                            

                            if (State == IntrinsicState.Unknown)
                            {
                                //Set to compiled
                                State = IntrinsicState.Compiled;

                                try
                                {
                                    //Check for failure even if the function succeeds because we may be using the safe asm which ensures 0 as the result on failure
                                    if (GenerateRandom() == 0)
                                    {
                                        EntryPoint.Dispose();

                                        EntryPoint = null;

                                        RandomDelegate = null;

                                        State = IntrinsicState.NotAvailable;
                                    }
                                    else
                                    {
                                        //Indicate available.
                                        Intrinsics.SetRuntimeState(this, State = IntrinsicState.Available);

                                        //Indicate available.
                                        //Intrinsics.CompiledIntrinsics.Add(MetadataToken, State = IntrinsicState.Available);
                                    }
                                }
                                catch
                                {
                                    //Indicate not available.
                                    //Intrinsics.CompiledIntrinsics.Add(MetadataToken, State = IntrinsicState.NotAvailable);

                                    Intrinsics.SetRuntimeState(this, State = IntrinsicState.NotAvailable);

                                    //SEH, Dispose the function
                                    EntryPoint.Dispose();

                                    //Set the delegate to null
                                    RandomDelegate = null;
                                }

                                //If the function is not available then allocate the fallback
                                if (State == IntrinsicState.NotAvailable) goto case IntrinsicState.NotAvailable;
                            }

                            break;
                        }
                }
            }

            /// <summary>
            /// An alternate approach if random values are unavailable at the time of RDRAND execution is to use a retry loop. 
            /// In this approach, an additional argument allows the caller to specify the maximum number of retries before returning a failure value. 
            /// The success or failure of the function is indicated by its return value and the actual random value is passed to the caller by a reference variable.
            /// </summary>
            /// <param name="retries"></param>
            /// <returns></returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal bool TryGenerateRandom(int retries, ref ulong result)
            {
                //While retries remain
                while (--retries >= 0)
                {
                    //If the result was 1 the function failed, continue.
                    if (1 == (result = GenerateRandom())) continue;

                    //We have a result
                    break;
                }

                //Indicate success or failure.
                return false == (result == 1);
            }

            /// <summary>
            /// Generates random values from the random generator into the given array.
            /// </summary>
            /// <param name="retries">The number of attempts to use to fill the array using the generator</param>
            /// <param name="reverse">Indicates if the byte order should be reversed</param>
            /// <param name="bytes">The array to put the random data into</param>
            /// <param name="offset">The offset in <paramref name="bytes"/></param>
            /// <param name="length">The amount of random data</param>
            internal void NextBytes(int retries, bool reverse, byte[] bytes, int offset, int length)
            {
                //If no bytes return
                if (length <= 0) return;

                //Determine the max offset
                int max;

                //If the array was null or empty return
                if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(bytes, out max)) return;

                //There is already an offset
                max -= offset;

                //Set length to whatever is smaller, the max or the length.
                length = Common.Binary.Min(ref max, ref length);

                //Stack allocate
                ulong random = 0;

                //While there are bytes to fill
                while (length > 0)
                {
                    //Try to generate a random number in random
                    if (TryGenerateRandom(retries, ref random))
                    {
                        //Write the bytes needed to the result
                        Common.Binary.WriteInteger(bytes, offset, max, random, reverse);

                        //move the offset by how much to copy
                        offset += max;

                        //Decrease length
                        length -= max;
                    }
                }
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal ulong GenerateRandom()
            {
                return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
            }

            #endregion
        }

        #endregion

        #region Rdseed

        /// <summary>
        /// Provides an implementation of the rdseed intrinsic
        /// </summary>
        public class Rdseed : Intrinsic
        {

            #region References

            //https://en.wikipedia.org/wiki/RdRand

            //https://software.intel.com/sites/default/files/managed/4d/91/DRNG_Software_Implementation_Guide_2.0.pdf

            #endregion

            /// <summary>
            /// The delegate used to invoke the intrinsic
            /// </summary>
            /// <param name="status"></param>
            /// <returns></returns>
            [System.Security.SuppressUnmanagedCodeSecurity]
            [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
            internal delegate ulong SeedNative([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)] ref byte status); //ref instead of out because of fallback

            #region Statics

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static ulong GetSeed64()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static long GetSeed63()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (long)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static uint GetSeed32()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (uint)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static int GetSeed31()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (int)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static ushort GetSeed16()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (ushort)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static short GetSeed15()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (short)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static byte GetSeed8()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (byte)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// Invokes the random number generator returning the result.
            /// </summary>
            /// <returns>1 if the function was not successful, otherwise a random number</returns>
            [System.CLSCompliant(false)]
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static sbyte GetSeed7()
            {
                using (Rdseed rdseed = new Rdseed())
                {
                    return (sbyte)rdseed.GenerateSeed();
                }
            }

            /// <summary>
            /// For fallback use only.
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            static ulong Fallback(ref byte s)
            {
                //s = 0;

                return Hardware.Intrinsics.Rtdsc.GetTimestampUnsigned();
            }

            #endregion

            #region Fields

            byte[] x86CodeBytes = 
            {
                0x0F, 0xC7, 0xF8, // rdseed eax
                0x0F, 0x92, 0x01, // setb BYTE PTR [ecx]
                0xC3 // ret
            };

            byte[] x64CodeBytes = new byte[] 
            {
                0x48, 0x0F, 0xC7, 0xF8, // rexw rdseed rax
                0x0F, 0x92, 0x01, // setb BYTE PTR [rcx]
                0xC3 // ret
            };

            internal SeedNative SeedDelegate;

            #endregion

            #region Constructor

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Rdseed(bool machine = false, bool shouldDispose = true)
                : base(shouldDispose)
            {
                //if (machine && false == CpuId.Supports(CpuId.CpuIdFeature.rdseed)) throw new System.NotSupportedException();

                //Intrinsics.CompiledIntrinsics.TryGetValue(GetType().MetadataToken, out State);

                if (false == Intrinsics.GetRuntimeState(this) && machine && false == CpuId.Supports(CpuId.CpuIdFeature.rdseed)) throw new System.NotSupportedException();

                State = (Common.Binary.Read32(Intrinsics.CpuId.RetrieveInformation(7).Array, 8, false) & 0x40000) == 0x40000 ? IntrinsicState.Available : IntrinsicState.NotAvailable;

                Compile(machine);
            }

            #endregion

            #region Methods

            /// <summary>
            /// The first time the function is compiled a special version of byte code is used to ensure that misdetection of the feature is not possible.
            /// </summary>
            /// <param name="machine"></param>
            [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected override void Compile(bool machine = true)
            {
                //Determine based on the State
                switch (State)
                {
                    case IntrinsicState.NotAvailable:
                        {
                            //Create the delegate to use the fallback RDTSC
                            SeedDelegate = Fallback;

                            //Allocate a new EntryPoint
                            EntryPoint = new PlatformMethod(ShouldDispose);

                            //Setup the InstructionPointer
                            EntryPoint.InstructionPointer = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(SeedDelegate);

                            break;
                        }
                    //Successully ran before
                    case IntrinsicState.Unknown:
                    case IntrinsicState.Available:
                        {
                            EntryPoint.SetInstructions(x86CodeBytes, x64CodeBytes, machine);

                            EntryPoint.VirtualAllocate();

                            EntryPoint.VirtualProtect();

                            if (State == IntrinsicState.Unknown)
                            {
                                //Set to compiled
                                State = IntrinsicState.Compiled;

                                try
                                {
                                    //Check for failure even if the function succeeds because we may be using the safe asm which ensures 0 as the result on failure
                                    if (GenerateSeed() == 0)
                                    {
                                        EntryPoint.Dispose();

                                        EntryPoint = null;

                                        SeedDelegate = null;

                                        State = IntrinsicState.NotAvailable;
                                    }
                                    else
                                    {
                                        //Indicate available.
                                        //Intrinsics.CompiledIntrinsics.Add(GetType().MetadataToken, State = IntrinsicState.Available);
                                        Intrinsics.SetRuntimeState(this, State = IntrinsicState.Available);
                                    }
                                }
                                catch
                                {
                                    //Indicate not available.
                                    //Intrinsics.CompiledIntrinsics.Add(GetType().MetadataToken, State = IntrinsicState.NotAvailable);
                                    Intrinsics.SetRuntimeState(this, State = IntrinsicState.NotAvailable);

                                    //SEH, Dispose the function
                                    EntryPoint.Dispose();

                                    //Set the delegate to null
                                    SeedDelegate = null;
                                }

                                //If the function is not available then allocate the fallback
                                if (State == IntrinsicState.NotAvailable) goto case IntrinsicState.NotAvailable;
                            }

                            break;
                        }
                }
            }

            /// <summary>
            /// An alternate approach if random values are unavailable at the time of RDRAND execution is to use a retry loop. 
            /// In this approach, an additional argument allows the caller to specify the maximum number of retries before returning a failure value. 
            /// The success or failure of the function is indicated by its return value and the actual random value is passed to the caller by a reference variable.
            /// </summary>
            /// <param name="retries"></param>
            /// <returns></returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal bool TryGenerateSeed(int retries, ref ulong result)
            {
                //While retries remain
                while (--retries >= 0)
                {
                    //If the result was 1 the function failed, continue.
                    if (1 == (result = GenerateSeed())) continue;

                    //We have a result
                    break;
                }

                //Indicate success or failure.
                return false == (result == 1);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal ulong GenerateSeed()
            {
                return Concepts.Classes.CommonIntermediateLanguage.CallIndirect(EntryPoint.InstructionPointer);
            }

            #endregion
        }

        #endregion
    }

    #endregion
}