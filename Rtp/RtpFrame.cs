using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtp
{

    /// <summary>
    /// A collection of packets representing a frame
    /// 
    /// From the sending side, "frame" is submitted and "packetized"
    /// From the receiving side, packets are read and "framed" when complete
    /// </summary>
    public class RtpFrame
    {

        #region Fields

        uint m_TimeStamp, m_Ssrc;

        byte m_PayloadType;

        SortedList<int, RtpPacket> m_Packets = new SortedList<int, RtpPacket>();

        #endregion

        #region Properties

        public RtpPacket this[int SequenceNumber] { get { return m_Packets[SequenceNumber]; } set { m_Packets[SequenceNumber] = value; } }

        public virtual uint SynchronizationSourceIdentifier
        {
            get { return m_Ssrc; }
            set
            {
                m_Ssrc = value;                
                for (int i = 0, e = m_Packets.Count; i < e; ++i)
                {
                    RtpPacket p = m_Packets[i];
                    p.SynchronizationSourceIdentifier = m_Ssrc;
                }
            }
        }

        public virtual byte PayloadType { get { return m_PayloadType; } }

        public virtual uint TimeStamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        public virtual List<RtpPacket> Packets { get { return m_Packets.Values.ToList(); } }

        //public virtual bool Complete { get { return m_Packets.Values.Any(p => p.Marker == true); } }
        public virtual bool Complete { get; private set; }

        #endregion

        #region Constructor

        public RtpFrame(RtpFrame frame) : this(frame.PayloadType) { this.SynchronizationSourceIdentifier = frame.SynchronizationSourceIdentifier; this.m_PayloadType = frame.PayloadType; this.m_Packets = frame.m_Packets; }

        public RtpFrame(byte payloadType)
        {
            m_PayloadType = payloadType;
        }

        public RtpFrame(byte payloadType, uint timeStamp, uint ssrc) 
            :this(payloadType)
        {
            m_Ssrc = ssrc;
            m_TimeStamp = timeStamp;    
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        public virtual void AddPacket(RtpPacket packet)
        {
            if (Complete) return;
            if (packet.PayloadType != m_PayloadType) return;// Could probably use a RtpFrameException
            if (packet.SynchronizationSourceIdentifier != m_Ssrc) return;
            m_Packets.Add(packet.SequenceNumber, packet);
            Complete = packet.Marker;
        }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool ContainsPacket(RtpPacket packet)
        {
            return ContainsPacket(packet.SequenceNumber);
        }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool ContainsPacket(int sequenceNumber)
        {
            return m_Packets.ContainsKey(sequenceNumber);
        }

        /// <summary>
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public virtual RtpPacket RemovePacket(int sequenceNumber)
        {
            RtpPacket removed;
            if (m_Packets.TryGetValue(sequenceNumber, out removed))
            {
                m_Packets.Remove(sequenceNumber);
            }
            return removed;
        }

        /// <summary>
        /// Combines the payload of all RtpPackets into a single byte array
        /// </summary>
        /// <returns>The byte array containing the payload of all packets in the frame</returns>
        public virtual byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            foreach (RtpPacket packet in m_Packets.Values)
            {
                result.AddRange(packet.Payload);
            }
            return result.ToArray();
        }

        #endregion
    }
}
