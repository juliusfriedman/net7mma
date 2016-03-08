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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame : Media.Common.BaseDisposable, IEnumerable<RtpPacket>// IDictionary, etc?
    {
        #region Readonly

        /// <summary>
        /// The DateTime in which the instance was created.
        /// </summary>
        public readonly DateTime Created;

        /// <summary>
        /// The maximum amount of packets which can be contained in a frame
        /// </summary>
        internal protected int MaxPackets = 1024;

        /// <summary>
        /// Indicates if the frame will ensure that all packets added have the same <see cref="RtpPacket.PayloadType"/>
        /// </summary>
        public readonly bool AllowMultiplePayloadTypes;

        #endregion

        #region Fields        

        /// <summary>
        /// Timestamp, SynchronizationSourceIdentifier and PayloadType of all contained packets.
        /// </summary>
        int m_Timestamp = -1, m_Ssrc = -1;

        byte m_PayloadByte = 0x80;

        /// <summary>
        /// Using a SortedList is fast than using a SortedDictionary and allows the value to be removed in state if it is contained more than once.
        /// </summary>
        //readonly protected SortedList<int, RtpPacket> m_Packets = new SortedList<int, RtpPacket>();

        internal readonly protected List<RtpPacket> m_Packets = new List<RtpPacket>();

        #endregion

        #region Properties

        //SourceList which should be added to each packet int the frame?

        /// <summary>
        /// Indicates if all contained RtpPacket instances have a Sent Value otherwise false.
        /// </summary>
        public bool Transferred { get { return IsEmpty ? false : m_Packets.All(p => p.Transferred.HasValue); } }

        /// <summary>
        /// Gets or sets the SynchronizationSourceIdentifier of All Packets Contained or -1 if not assigned.
        /// </summary>
        public virtual int SynchronizationSourceIdentifier
        {
            get { return IsEmpty ? -1 : (int)m_Ssrc; }
            set
            {
                m_Ssrc = value;
                foreach (RtpPacket p in m_Packets)
                {
                    p.SynchronizationSourceIdentifier = m_Ssrc;
                }
            }
        }

        /// <summary>
        /// Gets the PayloadType of All Packets Contained or 128 if unassigned.
        /// </summary>
        public virtual byte PayloadTypeByte { get { return IsEmpty ? (byte)128 : m_PayloadByte; } }

        /// <summary>
        /// The Timestamp of All Packets Contained or -1 if unassigned.
        /// </summary>
        public virtual int Timestamp { get { return IsEmpty ? -1 : m_Timestamp; } set { m_Timestamp = value; } }

        /// <summary>
        /// Indicates if the RtpFrame is NotEmpty AND contained a RtpPacket which has the Marker Bit Set AND is not <see cref="IsMissingPackets"/>
        /// </summary>
        public virtual bool IsComplete
        {
            get { return false == IsDisposed && false == IsEmpty && HasMarker && false == IsMissingPackets; }
        }

        //Todo, for Rtcp feedback one would need the sequence numbers of the missing packets...
        //Would be given any arbitrary RtpFrame and would implement that logic there.

        /// <summary>
        /// Indicates if all contained packets are sequential up the Highest Sequence Number contained in the RtpFrame.
        /// (Must have 1 packet with marker or more than 1 packet)
        /// </summary>
        public virtual bool IsMissingPackets
        {
            get
            {
                switch (m_Packets.Count)
                {
                    //No packets
                    case 0: return true;
                    //Single packet, only missing if there is no marker
                    case 1: return false == HasMarker;
                    //2 or more packets, cache the m_LowestSequenceNumber and check all packets to be sequential
                    default: RtpPacket p; int nextSeq = m_LowestSequenceNumber; for (int i = 0, e = Count; i < e; ++i)
                    {
                        //Scope the packet
                        p = m_Packets[i];

                        //obtain the sequence number to check if the packet is missing
                        if (p.SequenceNumber != nextSeq) return true;

                        //Determine the next sequence number
                        nextSeq = nextSeq == ushort.MaxValue ? ushort.MinValue : ++nextSeq;
                    }
                        //Not missing any packets.
                        return false;
                }
            }
        }

        /// <summary>
        /// Indicates if the last packet has the marker bit set
        /// </summary>
        public virtual bool HasMarker { get { return IsEmpty ? false : m_Packets.Last().Marker; } } // m_Packets.Any(a => a.Value.Marker); } }

        /// <summary>
        /// The amount of Packets in the RtpFrame
        /// </summary>
        public int Count { get { return m_Packets.Count; } }

        /// <summary>
        /// Indicates if there are packets in the RtpFrame
        /// </summary>
        public bool IsEmpty { get { return m_Packets.Count == 0; } }

        /// <summary>
        /// The Lowest and Highest SequenceNumber in the contained RtpPackets or -1 if no RtpPackets are contained
        /// </summary>
        internal int m_LowestSequenceNumber = -1, m_HighestSequenceNumber = -1;

        /// <summary>
        /// Gets the 16 bit unsigned value which is associated with the highest sequence number contained or -1 if no RtpPackets are contained.
        /// </summary>
        public int HighestSequenceNumber { get { return m_HighestSequenceNumber; } }

        /// <summary>
        /// Gets the 16 bit unsigned value which is associated with the lowest sequence number contained or -1 if no RtpPackets are contained.
        /// </summary>
        public int LowestSequenceNumber { get { return m_LowestSequenceNumber; } }

        #endregion

        #region Constructor

        public RtpFrame()
        {
            //Indicate when this instance was created
            Created = DateTime.UtcNow;
        }

        public RtpFrame(int payloadType) : this()
        {
            //Should be bound from 0 - 127 inclusive...
            if (payloadType > byte.MaxValue) throw Common.Binary.CreateOverflowException("payloadType", payloadType, byte.MinValue.ToString(), byte.MaxValue.ToString());

            //Assign the type of RtpFrame
            m_PayloadByte = (byte)payloadType;
        }

        public RtpFrame(int payloadType, int timeStamp, int ssrc) 
            :this(payloadType)
        {
            //Assign the Synconrization Source Identifier
            m_Ssrc = ssrc;

            //Assign the Timestamp
            m_Timestamp = timeStamp;    
        }

        public RtpFrame(RtpPacket packet, bool addPacket = true)
            :this(packet.PayloadType, packet.Timestamp, packet.SynchronizationSourceIdentifier)
        {
            if(addPacket) Add(packet);
        }

        /// <summary>
        /// Clone and existing RtpFrame
        /// </summary>
        /// <param name="f">The frame to clonse</param>
        /// <param name="referencePackets">Indicate if contained packets should be referenced</param>
        public RtpFrame(RtpFrame f, bool referencePackets = false)
            : this(f.PayloadTypeByte)
        {
            m_Ssrc = f.m_Ssrc; m_Timestamp = f.m_Timestamp;

            //If this is a shallow clone then just use the reference
            if (referencePackets) m_Packets = f.m_Packets;
            else foreach (RtpPacket p in f) m_Packets.Add(p); //Otherwise make a new reference to each RtpPacket
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~RtpFrame() { Dispose(); } 

        #endregion

        #region Methods

        /// <summary>
        /// Gets an enumerator of All Contained Packets at the time of the call
        /// </summary>
        /// <returns>A Yield around Packets</returns>
        public IEnumerator<RtpPacket> GetEnumerator() { return m_Packets.GetEnumerator(); }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame. The first packet added sets the SynchronizationSourceIdentifier and Timestamp if not already set.
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        /// <param name="allowPacketsAfterMarker">Indicates if the packet shouldbe allowed even if the packet's sequence number is greater than or equal to <see cref="HighestSequenceNumber"/> and <see cref="IsComplete"/> is true.</param>
        public virtual void Add(RtpPacket packet, bool allowPacketsAfterMarker = false, bool allowDuplicates = false)
        {
            if (packet == null) throw new ArgumentNullException("packet");

            int count = Count, ssrc = packet.SynchronizationSourceIdentifier, seq = packet.SequenceNumber, ts = packet.Timestamp, pt = packet.PayloadType;

            //No packets contained yet
            if (count == 0)
            {
                if (m_Ssrc == -1) m_Ssrc = ssrc;

                if (m_Timestamp == -1) m_Timestamp = ts;

                if (m_PayloadByte == 128) m_PayloadByte = (byte)pt;

                m_LowestSequenceNumber = m_HighestSequenceNumber = seq;

                m_Packets.Add(packet);

                return;
            }
            else //At least 1 packet is contained
            {
                //Check payload type if indicated
                if (AllowMultiplePayloadTypes == false && pt != m_PayloadByte) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");

                if (ssrc != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");

                if (ts != Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");

                if (count >= MaxPackets) throw new InvalidOperationException(string.Format("The amount of packets contained in a RtpFrame cannot exceed: {0}", MaxPackets));
            }

            //Check for existing marker packet
            bool hasMarker = HasMarker;

            //Determine if the packet has a marker
            bool packetMarker = packet.Marker;

            //If the packet has the marker bit set
            if (packetMarker)
            {
                //If there was already a marker packet in the frame then this is an exception
                if (hasMarker) throw new InvalidOperationException("Cannot have more than one marker packet in the same RtpFrame.");

                //Marker packet must always be last
                m_Packets.Add(packet);

                //It is the highest sequence number
                m_HighestSequenceNumber = seq;

                return;
            }
            
            //Determine where to insert
            int insert = 0, tempSeq = 0;

            //Search for insert point
            while (insert < count && (short)(seq - (tempSeq = m_Packets[insert].SequenceNumber)) >= 0)
            {
                ++insert;
            }

            //Ensure not a duplicate
            if (false == allowDuplicates && tempSeq == seq) throw new InvalidOperationException("Cannot have duplicate packets in the same frame.");

            //Handle prepend
            if (insert == 0)
            {
                m_Packets.Insert(0, packet);

                m_LowestSequenceNumber = seq;
            }
            else if (insert >= count) //Handle add
            {
                if (false == allowPacketsAfterMarker && hasMarker) throw new InvalidOperationException("Cannot add packets after the marker packet.");

                m_Packets.Add(packet);

                m_HighestSequenceNumber = seq;
            }
            else m_Packets.Insert(insert, packet); //Insert
        }

        /// <summary>
        /// Calls <see cref="Add"/> and indicates if the operations was a success
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="allowPacketsAfterMarker"></param>
        /// <returns></returns>
        public bool TryAdd(RtpPacket packet, bool allowPacketsAfterMarker = false)
        {
            try { Add(packet, allowPacketsAfterMarker); return true; }
            catch { return false; }
        }

        //TryAddOrUpdate

        //Update

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(RtpPacket packet) { return m_Packets.Contains(packet); }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(int sequenceNumber)
        {
            int count = Count;
            switch (count)
            {
                case 0: return false;
                case 1:
                    {
                        return m_HighestSequenceNumber == sequenceNumber;
                    }
                case 2:
                    {
                        if (m_LowestSequenceNumber == sequenceNumber) return true;
                        goto case 1;
                    }
                default:
                    {
                        //Fast path when no roll over occur, e.g. m_Packets[0].SequenceNumber > m_Packets.Last().SequenceNumber
                        //if (m_HighestSequenceNumber > m_LowestSequenceNumber && (sequenceNumber <= m_HighestSequenceNumber && sequenceNumber >= m_LowestSequenceNumber)) return true;

                        RtpPacket p;

                        for (int i = Common.Binary.Max(0, sequenceNumber - m_LowestSequenceNumber), e = count; i < e; ++i)
                        {
                            p = m_Packets[i];

                            if (p.SequenceNumber == sequenceNumber) return true;

                            p = m_Packets[--e];

                            if (p.SequenceNumber == sequenceNumber) return true;
                        }

                        return false;
                    }
            }
        }

        //bool Remove

        /// <summary>
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the RtpPacket to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public virtual RtpPacket Remove(int sequenceNumber)
        {
            try
            {
                int count = Count;

                if (count == 0) return null;

                for (int i = 0; i < count; ++i)
                {
                    RtpPacket p = m_Packets[i];

                    if (p.SequenceNumber == sequenceNumber)
                    {
                        m_Packets.RemoveAt(i);

                        switch (--count)
                        {
                            case 0: m_LowestSequenceNumber = m_HighestSequenceNumber = -1; break;
                            case 1: m_LowestSequenceNumber = m_HighestSequenceNumber = m_Packets[0].SequenceNumber; break;
                            default:
                                {
                                    if (sequenceNumber == m_LowestSequenceNumber)
                                    {
                                        m_LowestSequenceNumber = m_Packets.First().SequenceNumber;
                                    }
                                    else if (sequenceNumber == m_HighestSequenceNumber)
                                    {
                                        m_HighestSequenceNumber = m_Packets.Last().SequenceNumber;
                                    }

                                    break;
                                }
                        }

                        return p;
                    }
                }

                return null;
            }
            catch { throw; }
            //finally 
            //{
            //    //Remove the reference obtained to the RtpPacket
            //    removed = null;
            //}
        }

        /// <summary>
        /// Empties the RtpFrame by clearing the SortedList of contained RtpPackets
        /// </summary>
        public virtual void RemoveAllPackets()
        {
            m_Packets.Clear();
            m_HighestSequenceNumber = m_LowestSequenceNumber = -1;
        }

        internal protected virtual void DisposeAllPackets()
        {
            //Dispose all packets...
            foreach (RtpPacket p in m_Packets) p.Dispose();
        }

        /// <summary>
        /// Assembles the RtpFrame into a byte[] by combining the ExtensionBytes and Payload of all contained RtpPackets into a single byte array (excluding the RtpHeader)
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public virtual IEnumerable<byte> Assemble(bool useExtensions = false, int profileHeaderSize = 0)
        {
            //The sequence
            IEnumerable<byte> sequence = Enumerable.Empty<byte>();

            //Iterate the packets
            foreach (RtpPacket packet in this)
            {
                //Should be handled by derived implementation because it is known if the flags are relevent to the data.
                if (packet.Extension)
                {
                    using (RtpExtension extension = packet.GetExtension())
                    {
                        if (useExtensions && extension != null)
                        {
                            /*if (extension.IsComplete) */
                            sequence = sequence.Concat(extension.Data);
                        }
                        else
                        {
                            profileHeaderSize += extension.Size;
                        }
                    }
                }

                //Should chyeck PayloadData is > profileHeaderSize ?

                sequence = sequence.Concat(new Common.MemorySegment(packet.Payload.Array, packet.Payload.Offset + profileHeaderSize, packet.Payload.Count - profileHeaderSize)); //packet.PayloadData.Skip(profileHeaderSize));
            }

            return sequence;
        }

        #endregion

        //Operators... and overloads

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ShouldDispose)
            {
                //Dispose all packets...
                DisposeAllPackets();

                //Remove all packets
                RemoveAllPackets(); 
            }
        }
    }
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpFrame class is correct
    /// </summary>
    internal class RtpFrameUnitTests
    {

        //Todo, Randomize
        public void TestAddingRandomPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                //1 2 3 4 5

                //Add marker packet with seq = 2
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 2
                });

                if (frame.Count != 1) throw new Exception("Frame must have 1 packet");

                //Add marker packet with seq = 4
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 4
                });

                if (frame.Count != 2) throw new Exception("Frame must have 2 packets");

                //Add marker packet with seq = 3
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 3
                });

                if (frame.Count != 3) throw new Exception("Frame must have 3 packets");

                //Add marker packet with seq = 5
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 5
                });

                if (frame.Count != 4) throw new Exception("Frame must have 4 packets");

                //Add marker packet with seq = 1
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 1
                });

                if (frame.Count != 5) throw new Exception("Frame must have 5 packets");

                /// 6, 7, 8 9, 10Marker

                //Add marker packet with seq = 10 marker
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 10,
                    Marker = true
                });

                if (frame.Count != 6) throw new Exception("Frame must have 6 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //Add marker packet with seq = 2
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 7
                });

                if (frame.Count != 7) throw new Exception("Frame must have 7 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //Add marker packet with seq = 4
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 9
                });

                if (frame.Count != 8) throw new Exception("Frame must have 8 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //Add marker packet with seq = 3
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 8
                });

                if (frame.Count != 9) throw new Exception("Frame must have 9 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //Add marker packet with seq = 5
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 6
                });

                //Verify count
                if (frame.Count != 10) throw new Exception("Frame must have 10 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //Write out
                Console.WriteLine(string.Join(",", frame.Select(p => p.SequenceNumber).ToArray()));

                //Verify the order
                for (int i = 0; i < 10; ++i)
                {
                    if (frame.m_Packets[i].SequenceNumber != i + 1) throw new Exception("Invalid order");
                }

                //The frame cannot be missing packets.
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
            }
        }

        public void TestAddingAndRemovingPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {

                //Add marker packet with seq = 0
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                   {
                       SequenceNumber = ushort.MinValue,
                       Marker = true
                   });


                //Add a lower order packet which MAY belong to the frame, 65535 could be a previous packet or a large jump, it would depend on the timestamp.
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                }, true);

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Add a lower order packet which MAY belong to the frame, 65534
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                }, true);

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Add a higher order packet which does not belong to the frame because the marker packet was set on packet 0, the timestamp should also be different.
                try
                {
                    frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                    {
                        SequenceNumber = 1
                    });

                    throw new Exception("Should not be allowed to add packet");
                }
                catch(InvalidOperationException)
                {
                    //Expected
                }

                if (frame.HighestSequenceNumber != ushort.MinValue) throw new Exception("Unexpected HighestSequenceNumber");

                if (frame.LowestSequenceNumber != ushort.MaxValue - 1) throw new Exception("Unexpected LowestSequenceNumber");

                //Remove a non existing packet
                using (Media.Rtp.RtpPacket packet = frame.Remove(1))
                {
                    if (packet != null) throw new Exception("Packet is not null");
                }

                //Remove three existing packets
                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MaxValue))
                {
                    if(packet == null) throw new Exception("Packet is null");
                }

                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MinValue))
                {
                    if (packet == null) throw new Exception("Packet is null");
                }

                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MaxValue - 1))
                {
                    if (packet == null) throw new Exception("Packet is null");
                }

                if (false == frame.IsEmpty) throw new Exception("Frame is not empty");
            }
        }

        public void TestContains()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MinValue,
                    Marker = true
                });

                if (frame.IsEmpty) throw new Exception("Frame is empty");

                if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                if (false == frame.Contains(ushort.MaxValue)) throw new Exception("Does not contain expected sequence number");

                if (false == frame.Contains(ushort.MaxValue - 1)) throw new Exception("Does not contain expected sequence number");

                if (false == frame.Contains(ushort.MinValue)) throw new Exception("Does not contain expected sequence number");

            }
        }

        public void TestIsMissingPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                //Add packets to the frame
                for (int i = 0; i < 15; ++i)
                {

                    frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                    {
                        SequenceNumber = i,
                        Marker = i == 14
                    });
                }

                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Remove the marker packet
                using (frame.Remove(14))
                {
                    //Frame doesn't have a marker packet anymore

                    //Check for IsComplete
                    if (frame.IsComplete) throw new Exception("Frame is complete");

                    //Check for HasMarker
                    if (frame.HasMarker) throw new Exception("Frame has marker");

                    //The frame IS NOT missing packets because the frame contains 0 - 13, but has no marker packet.
                    if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                }


                //Remove the first packet
                using (frame.Remove(1))
                {
                    if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                }

                if (frame.Count != 13) throw new Exception("Frame Count is incorrect");
            }
        }

        public void TestSequenceNumberRollover()
        {
            //Create a frame
            unchecked
            {
                using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
                {
                    //Add 15 packets to the frame
                    for (ushort i = ushort.MaxValue - 5; i != 10; ++i)
                    {

                        frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                        {
                            SequenceNumber = i,
                            Marker = i == 9
                        });
                    }

                    if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                    if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                    if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                    //Remove the packet with sequence number 0
                    using (frame.Remove(0))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }

                    //Remove the packet with sequence number 2
                    using (frame.Remove(2))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }

                    //Remove the marker packet
                    using (frame.Remove(9))
                    {
                        if (frame.IsComplete) throw new Exception("Frame is complete");

                        if (frame.HasMarker) throw new Exception("Frame has marker");

                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");                        
                    }
                }
            }
        }
    }
}
