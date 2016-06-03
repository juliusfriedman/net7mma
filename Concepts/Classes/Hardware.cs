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
