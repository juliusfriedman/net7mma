using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame
    {
        #region Fields

        uint m_TimeStamp, m_Ssrc;

        byte m_PayloadType;

        SortedList<int, RtpPacket> m_Packets = new SortedList<int, RtpPacket>();

        #endregion

        #region Properties

        /// <summary>
        /// Accesses a packet by SequenceNumber (should be uint to also allow for index?)
        /// </summary>
        /// <param name="SequenceNumber"></param>
        /// <returns></returns>
        public RtpPacket this[int SequenceNumber] { get { return m_Packets[SequenceNumber]; } set { m_Packets[SequenceNumber] = value; } }

        /// <summary>
        /// The SynchronizationSourceIdentifier of All Packets Contained
        /// </summary>
        public virtual uint SynchronizationSourceIdentifier
        {
            get { return m_Ssrc; }
            set
            {
                m_Ssrc = (uint)value;
                for (int i = 0, e = m_Packets.Count; i < e; ++i)
                {
                    RtpPacket p = m_Packets[i];
                    p.SynchronizationSourceIdentifier = m_Ssrc;
                }
            }
        }

        /// <summary>
        /// The PayloadType of All Packets Contained
        /// </summary>
        public virtual byte PayloadType { get { return m_PayloadType; } }

        /// <summary>
        /// The Timestamp of All Packets Contained
        /// </summary>
        public virtual uint TimeStamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        /// <summary>
        /// A List of All Packets Contained Sorted by SequenceNumber
        /// </summary>
        public virtual List<RtpPacket> Packets { get { return m_Packets.Values.ToList(); } }

        /// <summary>
        /// Indicated if a Contained Packet has the Marker Bit Set
        /// </summary>
        public virtual bool Complete { get; protected set; }

        /// <summary>
        /// Indicates if All Contained Packets are sequential
        /// </summary>
        public virtual bool IsMissingPackets { get { return !m_Packets.All(a => a.Value.Marker || m_Packets.ContainsKey(a.Key + 1)); } }

        public int Count { get { return m_Packets.Count; } }

        #endregion

        #region Constructor

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

        public RtpFrame(RtpFrame f) : this(f.PayloadType) { m_Ssrc = f.m_Ssrc; m_TimeStamp = f.m_TimeStamp; m_Packets = f.m_Packets; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an enumerator of All Contained Packets at the time of the call
        /// </summary>
        /// <returns>A Yield around Packets</returns>
        public IEnumerator<RtpPacket> GetEnumerator()
        {            
            foreach (var p in Packets)
            {
                yield return p;
            }
        }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        public virtual void AddPacket(RtpPacket packet)
        {
            if (Complete) return;
            if (packet.PayloadType != m_PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");
            if (packet.SynchronizationSourceIdentifier != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");
            if (ContainsPacket(packet)) return;
            lock (m_Packets)
            {                
                m_Packets.Add(packet.SequenceNumber, packet);
                Complete = packet.Marker;
            }
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
                lock (m_Packets)
                {
                    m_Packets.Remove(sequenceNumber);
                }
            }
            return removed;
        }        

        public virtual void RemoveAllPackets() { m_Packets.Clear(); }

        /// <summary>
        /// Assembles the Frame by combining the ExtensionBytes and Payload of all RtpPackets into a single byte array (excluding the RtpHeader)
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public virtual byte[] Assemble()
        {
            List<byte> result = new List<byte>();
            lock (m_Packets)
            {
                foreach (RtpPacket packet in m_Packets.Values)
                {
                    if (packet.Extensions)
                    {
                        result.AddRange(packet.ExtensionBytes);
                    }
                    result.AddRange(packet.Payload);
                }
            }
            return result.ToArray();
        }

        #endregion
    }
}
