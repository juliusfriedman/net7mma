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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Common
{
    /// <summary>
    /// Packets are <see cref="IDisposable">disposable</see> contigous allocations of memory which have been created or transferred.
    /// </summary>
    public interface IPacket : IDisposed
    {
        /// <summary>
        /// Gets a value indciating when the IPacket was created.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// Gets a value indicating when the NetworkPacket was sent.
        /// If null then the IPacket was not yet set.
        /// </summary>
        DateTime? Transferred { get; }

        /// <summary>
        /// Determines if the IPacket is completely in memory.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Indicates if the IPacket is compressed
        /// </summary>
        bool IsCompressed { get; }

        /// <summary>
        /// Determines if the IPacket can be modified.
        /// </summary>
        bool IsReadOnly { get; } //Should be Seperate Interface

        /// <summary>
        /// The length in bytes of the packet
        /// </summary>
        long Length { get; }

        //RawLength

        /// <summary>
        /// Completes the IPacket if IsComplete and Disposed is false.
        /// </summary>
        /// <param name="socket">The socket to complete from</param>
        /// <param name="buffer">The buffer to complete in</param>
        int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer);

        /// <summary>
        /// Creates a sequence of bytes which correspond to the IPacket in binary format suitable to be sent on a network.
        /// </summary>
        /// <returns>The sequence</returns>
        IEnumerable<byte> Prepare();

        /// <summary>
        /// Attempts to get all buffers assoicated with the data of the packet.
        /// </summary>
        /// <param name="bufferList">The list of buffers</param>
        /// <returns>True if the operation succeeds, otherwise false.</returns>
        bool TryGetBuffers(out IList<ArraySegment<byte>> bufferList);
        
        #region Notes 

        //unsafe could return byte* but the buffers are potentially seperated. (header and payload etc)

        //Could probably use the same api the Socket.Send Ilist is using but with different pointers in the list which correspond to the correct data.
        //MemorySegment is exactly like ArraySegment anyway,
        //Therefore, I Could probably unsafely change the type of the reference to ArraySegment<byte> and it would probably work in place...

        //Mono - Xamarin
        //Supports send with ArraySegment but also has a Send_nochecks and Send_internal / Xamarin may not implement.
        //It may be easier to just stub a method for sending (and recieving) rather than trying to act differently depending on the RunTime but the RunTimes are different to a degree.

        //Microsoft -
        //BufferOffsetSize is used in 4.6 http://referencesource.microsoft.com/#System/net/System/Net/Sockets/Socket.cs
        //DoMultipleSend, BeginMultipleSend, EndMultipleSend
        //MultipleConnect
        //This could call those methods with the MemorySegment especially if MemorySegment was able to used as a BufferOffsetSize structure.

        #endregion

        //May want to include source address / port and destination address / port

        //System.Net.EndPoint To { get; }

        //System.Net.EndPoint From { get; }
    }


    /// <summary>
    /// Defines commony used extension methods for IPacket instances.
    /// </summary>
    public static class IPacketExtensions 
    {
        /// <summary>
        /// Determines the amount of time taken to send the packet based on the time the packet was <see cref="IPacket.Created">created</see>
        /// </summary>
        /// <param name="packet">The packet</param>
        /// <returns>Null if the packet was not yet sent, otherwise the amount of time</returns>
        public static TimeSpan? GetConverganceTime(this IPacket packet)
        {
            if (false == packet.Transferred.HasValue) return null; 
            return packet.Transferred.Value - packet.Created;
        }
    }

}
