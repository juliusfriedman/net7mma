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
namespace Media.Common.Interfaces
{
    ///Mangle the name once.
    ///flags, offset, count    
    using Dimension = System.Tuple<int, ulong, ulong>;    

    //Todo, flags... Read, Write, Flush

    /// <summary>
    /// Conveys the <see cref="System.Tuple"/> which represents the dimension
    /// </summary>
    /// <param name="dimensionInformation"></param>
    /// <remarks>
    /// The index of the dimension is already known.
    /// </remarks>
    delegate void DimensionInformationDelegation(ref Dimension dimensionInformation);
    
    /// <summary>
    /// Conveys the unsigned index of the dimension being communicated.
    /// </summary>
    /// <param name="dimensionIndex"></param>
    /// <remarks>
    /// The information about the dimension is determined by futher delegation
    /// </remarks>
    delegate void DimensionIndexDelegation(ref ulong dimensionIndex);

    /// <summary>
    /// Represents an <see cref="IMutable"/>, <see cref="IShared"/> instance with access to input and output.
    /// </summary>
    interface IBuffer : IMutable, IShared
    {
        /// <summary>
        /// Where data is read
        /// </summary>
        SegmentStream Input { get; }

        /// <summary>
        /// Where data is written
        /// </summary>
        SegmentStream Output { get; }

        //it is possible this is too tightly coupled because the Buffer could be used without Dimensions also.
        //Thus a DimensionBuffer should be created to properly seperate these concepts.

        //(into either Input or Output based on flags)
        ISharedList<Dimension> Dimensions { get; }

        //ulongs

        //ulong CacheInput, CacheOutput; // > 0 is the limit, 0 == infinite, < 0 is undefined.

        //(Capacity of Dimensions), InputCapacity, OutputCapacity

        //(Count of Dimensions), CountOfInputs, CountOfOutputs

        //Capacity of Input, Output

        //Events

        /// <summary>
        /// When a dimension is created
        /// </summary>
        event DimensionInformationDelegation OnDimensionsCreated;

        /// <summary>
        /// When a dimension is changed.
        /// </summary>
        event DimensionIndexDelegation OnDimensionsChanged;

        //Methods

        DimensionInformationDelegation GetDimensionData(ref ulong index);

        //----
    }

    //Extensions => IsReadable, IsWriteable, etc.

    interface ICodecBuffer : IBuffer
    {
        //---------------------------

        //Input, Output ...

        //InputCodec, OutputCoded => 

        //Type InputTypes (Dimension, Byte[], Array, RtpPacket, IPacket etc)

        //Type OutputTypes (Dimension, Byte[], Array, RtpPacket, IPacket etc)

        //Equipment, Apparatus, Machines
        //Times, Rates, etc.
    }

}
