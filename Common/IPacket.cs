using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Common
{
    /// <summary>
    /// Packets are <see cref="IDisposable">disposable</see> contigous allocations of memory which have been created or transferred.
    /// </summary>
    public interface IPacket : IDisposable
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
        /// Determines if the IPacket can be modified.
        /// </summary>
        bool IsReadOnly { get; }

        //Indicates if the packet has been previously disposed.
        bool Disposed { get; }

        /// <summary>
        /// Completes the IPacket if IsComplete and Disposed is false.
        /// </summary>
        /// <param name="socket">The socket to complete from</param>
        void CompleteFrom(System.Net.Sockets.Socket socket);
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
            if (!packet.Transferred.HasValue) return null; 
            return packet.Transferred.Value - packet.Created;
        }
    }

}
