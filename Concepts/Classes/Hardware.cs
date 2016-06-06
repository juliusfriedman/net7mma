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

        //Execution

        ///// <summary>
        ///// <see cref="CallIndirect"/> on the <see cref="InstructionPointer"/>
        ///// </summary>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //internal void CallIndirect()
        //{
        //    Concepts.Classes.CommonIntermediateLanguage.CallIndirect(InstructionPointer);
        //}

        ///// <summary>
        ///// <see cref="CallIndirect"/> on the <see cref="InstructionPointer"/> and returns the result
        ///// </summary>
        ///// <param name="result"></param>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //internal void CallIndirect(out ulong result)
        //{
        //    result = Concepts.Classes.CommonIntermediateLanguage.CallIndirect(InstructionPointer);
        //}

        ///// <summary>
        ///// <see cref="CallIndirect"/> on the <see cref="InstructionPointer"/> and returns the result
        ///// </summary>
        ///// <param name="result"></param>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //internal void CallIndirect(out System.IntPtr result)
        //{
        //    result = (System.IntPtr)Concepts.Classes.CommonIntermediateLanguage.CallIndirect(InstructionPointer);
        //}

        /// <summary>
        /// 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MachineFunction(bool shouldDispose)
            : base(shouldDispose)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instructionPointer"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MachineFunction(bool shouldDispose, System.IntPtr instructionPointer)
            : this(shouldDispose)
        {
            InstructionPointer = instructionPointer;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (this.InstructionPointer == System.IntPtr.Zero || false == ShouldDispose) return;

            this.InstructionPointer = System.IntPtr.Zero;

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A routine which has no parameters and a single return type.
    /// </summary>
    public class UnmanagedAction : MachineFunction
    {
        /// <summary>
        /// The managed delegate
        /// </summary>
        public System.Delegate ManagedDelegate;

        /// <summary>
        /// The name of <see cref="ManagedDelegate"/> as retrieved from <see cref="ManagedDelegate.Method.Name"/>
        /// </summary>
        public string Name
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return ManagedDelegate.Method.Name; }
        }

        /// <summary>
        /// The name of <see cref="System.Type"/> as retrieved from the <see cref="ManagedDelegate"/> <see cref="ManagedDelegate.Method.ReturnType"/>
        /// </summary>
        public System.Type ReturnType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ManagedDelegate.Method.ReturnType;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedDelegate"></param>
        public UnmanagedAction(System.Delegate managedDelegate, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (managedDelegate == null) throw new System.ArgumentNullException();

            //System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(ManagedDelegate = managedDelegate);

            InstructionPointer = (ManagedDelegate = managedDelegate).Method.MethodHandle.GetFunctionPointer(); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instructionPointer"></param>
        /// <param name="returnType"></param>
        public UnmanagedAction(System.IntPtr instructionPointer, System.Type returnType, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (instructionPointer == System.IntPtr.Zero) throw new System.InvalidOperationException();
            else if (returnType == null) throw new System.ArgumentNullException();

            ManagedDelegate = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(InstructionPointer = instructionPointer, returnType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instructionPointer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static UnmanagedAction Create<T>(System.IntPtr instructionPointer) { return new UnmanagedAction(instructionPointer, typeof(T)); }
    }

    /// <summary>
    /// A <see cref="UnmanagedAction"/> which may have parameters.
    /// </summary>
    public class UnmanagedFunction : UnmanagedAction
    {
        //Todo, Epilogues, Prologs and other Requirements

        /// <summary>
        /// The name of <see cref="System.Type"/> as retrieved from the <see cref="ManagedDelegate"/> <see cref="ManagedDelegate.Method.ReturnType"/>
        /// </summary>
        public System.Reflection.ParameterInfo[] ParameterInformation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ManagedDelegate.Method.GetParameters();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedDelegate"></param>
        /// <param name="shouldDispose"></param>
        public UnmanagedFunction(System.Delegate managedDelegate, bool shouldDispose = true)
            : base(managedDelegate, shouldDispose)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instructionPointer"></param>
        /// <param name="returnType"></param>
        /// <param name="shouldDispose"></param>
        public UnmanagedFunction(System.IntPtr instructionPointer, System.Type returnType, bool shouldDispose = true)
            : base(instructionPointer, returnType, shouldDispose)
        {

        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="instructionPointer"></param>
        ///// <returns></returns>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public static UnmanagedFunction Create<T>(System.IntPtr instructionPointer, System.Delegate managedDelegate)
        //{
        //    return new UnmanagedFunction(managedDelegate.Method.MethodHandle.GetFunctionPointer(), typeof(T));
        //}
    }

    /// <summary>
    /// Provides the application programing interface associated with the requirements of executing a <see cref="MachineFunction"/> which resides in managed memory
    /// </summary>
    public abstract class FunctionPointerAllocation : MachineFunction
    {
        #region Fields

        /// <summary>
        /// The byte code of the instructions the machine should execute
        /// </summary>
        internal protected byte[] Instructions;

        #endregion

        #region Constructor

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal FunctionPointerAllocation(bool shouldDispose = true)
            : base(shouldDispose)
        {

        }

        internal FunctionPointerAllocation(bool shouldDispose, byte[] instructions)
            : this(shouldDispose)
        {
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(instructions)) throw new System.InvalidOperationException("instructions IsNullOrEmpty.");

            Instructions = instructions;
            
            //Could VirtualProtect to allow execution directly from managed array with CallIndirect
        }

        internal FunctionPointerAllocation(bool shouldDispose, System.Delegate managedDelegate)
            : this(shouldDispose)
        {
            InstructionPointer = managedDelegate.Method.MethodHandle.GetFunctionPointer();
        }

        internal FunctionPointerAllocation(bool shouldDispose, System.Reflection.MethodInfo methodInfo)
            : this(shouldDispose)
        {
            InstructionPointer = methodInfo.MethodHandle.GetFunctionPointer();
        }

        #endregion

        #region Abstraction

        /// <summary>
        /// Allows to allocate the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected abstract void VirtualAllocate();

        /// <summary>
        /// Allows to free the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected abstract void VirtualFree();

        /// <summary>
        /// Allows to protect the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected abstract void VirtualProtect();

        #endregion

        #region Methods

        /// <summary>
        /// Sets <see cref="Instructions"/> to one of the given parameters based on the type of determination required.
        /// </summary>
        /// <param name="x86codeBytes"></param>
        /// <param name="x64codeBytes"></param>
        /// <param name="machine"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void SetInstructions(byte[] x86codeBytes, byte[] x64codeBytes, bool machine = true)
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

        //Invoke
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //void Invoke()
        //{

        //}

        //CallIndirect / NativeInvoke
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //void CallIndirect()
        //{
        //    Concepts.Classes.CommonIntermediateLanguage.CallIndirect(InstructionPointer);
        //}

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed || false == disposing || false == ShouldDispose) return;

            Instructions = null;

            VirtualFree();

            base.Dispose(disposing);
        }

        #endregion

        #region Try Support

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

        #endregion
    }

    /// <summary>
    /// Representes a <see cref="FunctionPointerAllocation"/> which must be protected and unprotected
    /// </summary>
    public abstract class SecureFunctionPointer : FunctionPointerAllocation
    {
        /// <summary>
        /// Allows to unprotect the memory required to execute the <see cref="Instructions"/>
        /// </summary>
        internal protected abstract void VirtualUnprotect();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shouldDispose"></param>
        public SecureFunctionPointer(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Represents a class which will automatically use the correct platform invocation calls for the implementation to correctly utilize a <see cref="FunctionPointerAllocation"/>
    /// </summary>
    public class PlatformMethod : FunctionPointerAllocation
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PlatformMethod(bool shouldDispose = true) : base(shouldDispose) { }

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
    /// Represents a call which can replace a managed method with custom assembler code.
    /// </summary>
    public class PlatformMethodReplacement : FunctionPointerAllocation
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PlatformMethodReplacement(System.Reflection.MethodInfo method, byte[] newCode, bool shouldDispose = true)
            : base(shouldDispose, newCode) //Set the instructions
        {
            //Get the handle
            System.RuntimeMethodHandle methodHandle = method.MethodHandle;

            //Ensure the method was JIT to machine code
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(methodHandle);

            //Get the pointer to the method
            InstructionPointer = methodHandle.GetFunctionPointer();

            //Replace the instructions
            System.Runtime.InteropServices.Marshal.Copy(Instructions, 0, InstructionPointer, Instructions.Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualAllocate()
        {
            //Method handle is already allocated
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualProtect()
        {
            //Method handle is already protected
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal override void VirtualFree()
        {
            //Nothing to free
        }
    }

    /// <summary>
    /// Represents intrinsic functions which must be protected and unprotected.
    /// </summary>
    public abstract class SecureIntrinsicFunctionPointer : SecureFunctionPointer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shouldDispose"></param>
        public SecureIntrinsicFunctionPointer(bool shouldDispose) : base(shouldDispose) { }

        //protected internal override void SetInstructions(byte[] x86codeBytes, byte[] x64codeBytes, bool machine = true)
        //{
        //    throw new System.NotImplementedException();
        //}

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

    //IsVirtualMachine := (Machine.IsVirtualMachine)

    #region Hardware Support

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a processor.
    /// </summary>
    public abstract class ProcessorIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to the central processor.
    /// </summary>
    public abstract class CentralProcessorIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public CentralProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a graphics processor.
    /// </summary>
    public abstract class GraphicsProcessorIntrinsic : ProcessorIntrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public GraphicsProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a sound processor.
    /// </summary>
    public abstract class SoundProcessorIntrinsic : ProcessorIntrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public SoundProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a floating point processor.
    /// </summary>
    public abstract class FloatingPointProcessorIntrinsic : ProcessorIntrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public FloatingPointProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a math processor.
    /// </summary>
    public abstract class MathProcessorIntrinsic : ProcessorIntrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MathProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a memory processor.
    /// </summary>
    public abstract class MemoryProcessorIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MemoryProcessorIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    /// <summary>
    /// Provides a class which can be derived to support an intrinsic which is specific to a chipset.
    /// </summary>
    public abstract class ChipsetIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ChipsetIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    public abstract class DirectModeAccess : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DirectModeAccess(bool shouldDispose) : base(shouldDispose) { }
    }

    public abstract class PCIIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PCIIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    public abstract class AGPIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public AGPIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    public abstract class PCIExpressIntrinsic : Intrinsic
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PCIExpressIntrinsic(bool shouldDispose) : base(shouldDispose) { }
    }

    #endregion
}
