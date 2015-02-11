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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Common;

#endregion

namespace Media.Rtcp
{
    #region ReportBlock

    /// <summary>
    /// Represents the binary data contained in the <see cref="Rtcp.SendersReport"/> and <see cref="Rtcp.ReceiversReport"/>.
    /// </summary>
    /// <remarks>
    /// ReportBlock is a fixed sized structure which must always contain 24 octets.
    /// </remarks>
    public class ReportBlock : IReportBlock, IEnumerable<byte>, ICloneable
    {
        #region Constants and Statics

       // private const int MIN_PACKETS_LOST = -8388607; // 0xFF800001
       // private const int MAX_PACKETS_LOST = 8388607; // 0x007FFFFF 

        public const int ReportBlockSize = 24;

        #endregion

        #region Fields

        readonly Common.MemorySegment Memory;

        readonly byte[] m_OwnedOctets;

        #endregion

        #region Properties

        /// <summary>
        /// The size in octets of this ReportBlock instance
        /// </summary>
        public virtual int Size { get { return ReportBlockSize; } }

        /// <summary>
        /// The ID of the participant who sent this ReportBlock
        /// </summary>
        public int SendersSynchronizationSourceIdentifier
        {
            get { return (int)Binary.ReadU32(Memory.Array, Memory.Offset, BitConverter.IsLittleEndian); }
            protected set { Binary.Write32(Memory.Array, Memory.Offset, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The fractions of <see cref="Rtp.RtpPacket"/>'s from the <see cref="ReportBlock.SendersSynchronizationSourceIdentifier"/> indicated that have been lost since the begining of reception.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A.3"> for more information </see>
        /// </summary>
        public byte FractionsLost
        {
            get { return Binary.ReadU8(Memory.Array, Memory.Offset + 4, BitConverter.IsLittleEndian); }
            protected set { Binary.WriteU8(Memory.Array, Memory.Offset + 4, BitConverter.IsLittleEndian, value); }
        }

        /// <summary>
        /// The total number of <see cref="Rtp.RtpPacket"/>'s from the <see cref="ReportBlock.SendersSynchronizationSourceIdentifier"/> indicated that have been lost since the begining of reception.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A.3"> for more information </see>
        /// </summary>
        public int CumulativePacketsLost
        {
            get { return (int)Binary.ReadU24(Memory.Array, Memory.Offset + 5, BitConverter.IsLittleEndian); }
            protected set { Binary.Write24(Memory.Array, Memory.Offset + 5, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The highest <see cref="Rtp.RtpPacket.SeqeuenceNumber"> SequenceNumber</see> received from the <see cref="ReportBlock.SendersSynchronizationSourceIdentifier"/> indicated.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A.3"> for more information </see>
        /// </summary>
        public int ExtendedHighestSequenceNumberReceived
        {
            get { return (int)Binary.ReadU32(Memory.Array, Memory.Offset + 8, BitConverter.IsLittleEndian); }
            protected set { Binary.Write32(Memory.Array, Memory.Offset + 8, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// An estimate of the statistical variance of the <see cref="Rtp.RtpPacket.Created">RtpPacket</see> interarrival time.
        /// </summary>
        public int InterarrivalJitterEstimate
        {
            get { return (int)Binary.ReadU32(Memory.Array, Memory.Offset + 12, BitConverter.IsLittleEndian); }
            protected set { Binary.Write24(Memory.Array, Memory.Offset + 12, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The Middle 32 bits out of 64 in the NtpTimestamp (as explained in <see cref="http://tools.ietf.org/html/rfc3550#section-4">Section 4</see>)
        /// </summary>
        public int LastSendersReportTimestamp
        {
            get { return (int)Binary.ReadU32(Memory.Array, Memory.Offset + 16, BitConverter.IsLittleEndian); }
            protected set { Binary.Write24(Memory.Array, Memory.Offset + 16, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The delay, [expressed in units of 1/65536 seconds,] between receiving the last <see cref="Rtcp.SendersReport"/> packet from source indicated.
        /// </summary>
        public int DelaySinceLastSendersReport
        {
            get { return (int)Binary.ReadU32(Memory.Array, Memory.Offset + 20, BitConverter.IsLittleEndian); }
            protected set { Binary.Write24(Memory.Array, Memory.Offset + 20, BitConverter.IsLittleEndian, (uint)value); }
        }

        int IReportBlock.BlockIdentifier
        {
            get { return SendersSynchronizationSourceIdentifier; }
        }

        IEnumerable<byte> IReportBlock.BlockData
        {
            get { return Memory.Array.Skip(Memory.Offset).Take(Memory.Count); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Allocates 24 octets to represent this ReportBlock instance.
        /// </summary>
        ReportBlock() { m_OwnedOctets = new byte[Size]; Memory = new Common.MemorySegment(m_OwnedOctets, 0, Size); }

        /// <summary>
        /// Allocates 24 octets of memory and sets the <see cref="ReportBlock.SendersSynchronizationSourceIdentifier"/> property to the value of <paramref name="ssrc"/>.
        /// </summary>
        /// <param name="ssrc">The id of the participant who sent this ReportBlock instance</param>
        public ReportBlock(int ssrc) : this() { SendersSynchronizationSourceIdentifier = ssrc; }

        public ReportBlock(int ssrc, byte fractionsLost) : this(ssrc) { FractionsLost = fractionsLost; }

        public ReportBlock(int ssrc, byte fractionsLost, int cumulativePacketsLost) : this(ssrc, fractionsLost) { CumulativePacketsLost = cumulativePacketsLost; }

        public ReportBlock(int ssrc, byte fractionsLost, int cumulativePacketsLost, int extendedHighestSequenceNumberReceived) : this(ssrc, fractionsLost, cumulativePacketsLost) { ExtendedHighestSequenceNumberReceived = extendedHighestSequenceNumberReceived; }

        public ReportBlock(int ssrc, byte fractionsLost, int cumulativePacketsLost, int extendedHighestSequenceNumberReceived, int interarrivalJitterEstimate) : this(ssrc, fractionsLost, cumulativePacketsLost, extendedHighestSequenceNumberReceived) { InterarrivalJitterEstimate = interarrivalJitterEstimate; }

        public ReportBlock(int ssrc, byte fractionsLost, int cumulativePacketsLost, int extendedHighestSequenceNumberReceived, int interarrivalJitterEstimate, int lastSendersReportTimestamp) : this(ssrc, fractionsLost, cumulativePacketsLost, extendedHighestSequenceNumberReceived, interarrivalJitterEstimate) { LastSendersReportTimestamp = lastSendersReportTimestamp; }

        public ReportBlock(int ssrc, byte fractionsLost, int cumulativePacketsLost, int extendedHighestSequenceNumberReceived, int interarrivalJitterEstimate, int lastSendersReportTimestamp, int delaySinceLastSendersReport) : this(ssrc, fractionsLost, cumulativePacketsLost, extendedHighestSequenceNumberReceived, interarrivalJitterEstimate, lastSendersReportTimestamp) { DelaySinceLastSendersReport = delaySinceLastSendersReport; }

        /// <summary>
        /// Creates a new ReportBlock instance from the given existing reference.
        /// Throws a ArgumentNullException if <paramref name="reference"/> is null.
        /// </summary>
        /// <param name="reference">A reference to a ReportBlock instance.</param>
        public ReportBlock(ReportBlock reference)
        {
            if (reference == null) throw new ArgumentNullException();
            Memory = reference.Memory;
        }

        internal ReportBlock(Common.MemorySegment data)
        {
            Memory = data;
        }

        #endregion

        #region Instance Methods

        public ReportBlock Clone(bool reference)
        {
            if (reference) return new ReportBlock(this);
            return (ReportBlock)this.MemberwiseClone(); //Calls the default constructor and sets each property.
        }

        /// <summary>
        /// Prepares a sequence containig the octets which represent this instance.
        /// </summary>
        /// <returns>The sequence created</returns>
        IEnumerable<byte> Prepare()
        {
            return Memory.Array.Skip(Memory.Offset).Take(Size);
        }

        #endregion

        #region Implementation Methods

        public IEnumerator<byte> GetEnumeratorImplementation()
        {
            return Prepare().GetEnumerator();
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return GetEnumeratorImplementation();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImplementation();
        }

        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    #endregion
}
