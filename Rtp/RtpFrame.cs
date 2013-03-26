using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame : System.Collections.IEnumerable, IEnumerable<RtpPacket>
    {
        /// <summary>
        /// The maximum amount of packets which can be contained in a frame
        /// </summary>
        internal static int MaxPackets = 1024;

        #region Fields

        uint m_TimeStamp, m_Ssrc;

        byte m_PayloadType;

        protected SortedList<uint, RtpPacket> m_Packets = new SortedList<uint, RtpPacket>();

        #endregion

        #region Properties

        public DateTime? Created { get; set; }

        /// <summary>
        /// The SynchronizationSourceIdentifier of All Packets Contained
        /// </summary>
        public virtual uint SynchronizationSourceIdentifier
        {
            get { return m_Ssrc; }
            set
            {
                m_Ssrc = (uint)value;
                foreach (RtpPacket p in m_Packets.Values)
                {
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
        public virtual uint Timestamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        /// <summary>
        /// Indicated if a Contained Packet has the Marker Bit Set
        /// </summary>
        public virtual bool Complete { get; protected set; }

        /// <summary>
        /// Indicates if All Contained Packets are sequential
        /// </summary>
        public virtual bool IsMissingPackets { get { return m_Packets.Count == 0 || !m_Packets.All(a => a.Value.Marker || m_Packets.ContainsKey(a.Key + 1)); } }

        /// <summary>
        /// The amount of Packets in the RtpFrame
        /// </summary>
        public int Count { get { return m_Packets.Count; } }

        /// <summary>
        /// Indicates if there are packets in the RtpFrame
        /// </summary>
        public bool Empty { get { return m_Packets.Count == 0; } }

        /// <summary>
        /// The HighestSequenceNumber in the contained Packets or 0 if no Packets are contained
        /// </summary>
        public ushort HighestSequenceNumber { get { return (ushort)(Count > 0 ? m_Packets.Values.Last().SequenceNumber : 0); } }

        #endregion

        #region Constructor

        public RtpFrame(byte payloadType)
        {
            m_PayloadType = payloadType;
            Created = DateTime.UtcNow;
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
            return m_Packets.Values.GetEnumerator();
        }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame, if the packet has a Marker then the frame will be Complete and no allow addition or removal of packets
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        public virtual void Add(RtpPacket packet)
        {
            if (m_Ssrc == 0) m_Ssrc = packet.SynchronizationSourceIdentifier;
            if (Timestamp == 0) Timestamp = packet.TimeStamp;
            if (Count >= MaxPackets) throw new InvalidOperationException("The amount of packets contained in a RtpFrame cannot exceed: " + RtpFrame.MaxPackets);
            if (packet.PayloadType != m_PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");
            if (packet.SynchronizationSourceIdentifier != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");
            if (packet.TimeStamp != Timestamp) throw new ArgumentException("packet.TimeStamp must match frame TimeStamp", "packet");
            lock (m_Packets)
            {
                //If the frame is complete or the packet is contained return
                if (Complete || Contains(packet)) return;
                m_Packets.Add(packet.SequenceNumber, packet);
                Complete = packet.Marker;
            }
        }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(RtpPacket packet)
        {
            return Contains(packet.SequenceNumber);
        }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(uint sequenceNumber)
        {
            return m_Packets.ContainsKey(sequenceNumber);
        }

        /// <summary>
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number if the frame is not Complete
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public virtual RtpPacket Remove(uint sequenceNumber)
        {
            if (Complete) return null;
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

        /// <summary>
        /// Empties the frame
        /// </summary>
        public virtual void RemoveAllPackets() { Complete = false;  lock (m_Packets) { m_Packets.Clear(); } }

        /// <summary>
        /// Assembles the RtpFrame into a byte[] by combining the ExtensionBytes and Payload of all contained RtpPackets into a single byte array (excluding the RtpHeader)
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public virtual byte[] Assemble()
        {
            List<byte> result = new List<byte>();
            foreach (RtpPacket packet in this)
            {
                //If there are extensions as is
                //Should be handled by derived implementation
                if (packet.Extensions)
                {
                    result.AddRange(packet.ExtensionBytes);
                }

                //If the packet has padding
                //Should be handled by derived implementation
                result.AddRange(packet.Payload);
            }

            return result.ToArray();
        }

        #endregion

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
