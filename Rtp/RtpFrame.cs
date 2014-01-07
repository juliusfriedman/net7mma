﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame : IDisposable, System.Collections.IEnumerable, IEnumerable<RtpPacket>
    {
        /// <summary>
        /// The maximum amount of packets which can be contained in a frame
        /// </summary>
        internal protected int MaxPackets = 1024;

        #region Fields

        int m_Timestamp, m_Ssrc;

        byte m_PayloadByte;

        /// <summary>
        /// Using a SortedList is fast than using a SortedDictionary and allows the value to be removed in state if it is contained more than once.
        /// </summary>
        protected SortedList<int, RtpPacket> m_Packets = new SortedList<int, RtpPacket>();

        #endregion

        #region Properties

        //SourceList which should be added to each packet int the frame?

        public DateTime Created { get; set; }

        /// <summary>
        /// Indicates if all contained RtpPacket instances have a Sent Value.
        /// </summary>
        public bool Transferred { get { return m_Packets.All(p => p.Value.Transferred.HasValue); } }

        /// <summary>
        /// Gets or sets the SynchronizationSourceIdentifier of All Packets Contained
        /// </summary>
        public virtual int SynchronizationSourceIdentifier
        {
            get { return (int)m_Ssrc; }
            set
            {
                m_Ssrc = value;
                foreach (RtpPacket p in m_Packets.Values)
                {
                    p.SynchronizationSourceIdentifier = m_Ssrc;
                }
            }
        }

        /// <summary>
        /// The PayloadType of All Packets Contained
        /// </summary>
        public virtual byte PayloadTypeByte { get { return m_PayloadByte; } }

        /// <summary>
        /// The Timestamp of All Packets Contained
        /// </summary>
        public virtual int Timestamp { get { return m_Timestamp; } set { m_Timestamp = value; } }

        /// <summary>
        /// Indicates if the RtpFrame is NotEmpty, or contained a RtpPacket which has the Marker Bit Set
        /// </summary>
        public virtual bool Complete { get { return !IsEmpty && HasMarker; } }

        /// <summary>
        /// Indicates if all contained packets are sequential up the Highest Sequence Number contained in the RtpFrame.
        /// </summary>
        public virtual bool IsMissingPackets
        {
            get { return m_Packets.Count == 0 || m_Packets.Count > 1 && !m_Packets.All(a => a.Value.SequenceNumber < HighestSequenceNumber && m_Packets.ContainsKey(a.Key + 1)); }
        }

        /// <summary>
        /// Indicates if any packets have a marker
        /// </summary>
        public virtual bool HasMarker { get { return m_Packets.Any(a => a.Value.Marker); } }

        /// <summary>
        /// The amount of Packets in the RtpFrame
        /// </summary>
        public int Count { get { return m_Packets.Count; } }

        /// <summary>
        /// Indicates if there are packets in the RtpFrame
        /// </summary>
        public bool IsEmpty { get { return m_Packets.Count == 0; } }

        /// <summary>
        /// The HighestSequenceNumber in the contained Packets or 0 if no Packets are contained
        /// </summary>
        public int HighestSequenceNumber { get { return (Count > 0 ? m_Packets.Values.Last().SequenceNumber : 0); } }

        #endregion

        #region Constructor

        public RtpFrame(int payloadType)
        {
            //Indicate when this instance was created
            Created = DateTime.UtcNow;


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
            if (!referencePackets) m_Packets = f.m_Packets;
            else foreach (RtpPacket p in f) m_Packets.Add(p.SequenceNumber, p); //Otherwise make a new reference to each RtpPacket
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
        public IEnumerator<RtpPacket> GetEnumerator() { return m_Packets.Values.GetEnumerator(); }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame. The first packet added sets the SynchronizationSourceIdentifier and Timestamp if not already set.
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        public virtual void Add(RtpPacket packet)
        {
            //The first packet sets the ssrc
            if (Count == 0 && m_Ssrc != packet.SynchronizationSourceIdentifier) m_Ssrc = packet.SynchronizationSourceIdentifier;

            if (Count == 0 && m_Timestamp != packet.Timestamp) m_Timestamp = packet.Timestamp;

            if (packet.SynchronizationSourceIdentifier != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");
            
            if (packet.Timestamp != Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");

            if (Count >= MaxPackets) throw new InvalidOperationException(string.Format("The amount of packets contained in a RtpFrame cannot exceed: {0}", MaxPackets));

            if (packet.PayloadType != m_PayloadByte) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");

            //If the frame is complete or the packet is contained return
            //Note that there is no lock utilized, 
            //This is because multiple threads may very well be adding packets to the same frame and is acceptible behavior.
            //It is up to the implementation to add packets to the RtpFrame in a manner which is consistent with the state of the frame. 
            
            //E.g with respect to the above checks and finally the check below 
            //Which determineres if the RtpFrame is already complete that the packet will not be added to this frame.
            if (Complete || Contains(packet)) return;

            //Add the packet to the SortedList which will not throw any exception if the RtpPacket added already contains a value.
            m_Packets.Add(packet.SequenceNumber, packet);
        }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(RtpPacket packet) { return m_Packets.ContainsKey(packet.SequenceNumber); }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public virtual bool Contains(int sequenceNumber)
        {
            return m_Packets.ContainsKey(sequenceNumber);
        }

        /// <summary>
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the RtpPacket to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public virtual RtpPacket Remove(int sequenceNumber)
        {
            RtpPacket removed;
            try
            {
                //Checks for the packet to be contained in the SortedList and obtains a reference to the RtpPacket
                if (m_Packets.TryGetValue(sequenceNumber, out removed))
                {
                    //Remove it from the SortedList
                    m_Packets.Remove(sequenceNumber);
                }
                //Return the RtpPacket removed
                return removed;
            }
            catch { throw; }
            finally 
            {
                //Remove the reference obtained to the RtpPacket
                removed = null;
            }
        }

        /// <summary>
        /// Empties the RtpFrame by clearing the SortedList of contained RtpPackets
        /// </summary>
        public virtual void RemoveAllPackets() { m_Packets.Clear(); }

        /// <summary>
        /// Assembles the RtpFrame into a byte[] by combining the ExtensionBytes and Payload of all contained RtpPackets into a single byte array (excluding the RtpHeader)
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public virtual IEnumerable<byte> Assemble(bool useExtensions = false)
        {
            //The sequence
            IEnumerable<byte> sequence = Enumerable.Empty<byte>();

            //Iterate the packets
            foreach (RtpPacket packet in this)
            {
                //Should be handled by derived implementation because it is known if the flags are relevent to the data.
                if (useExtensions && packet.Extension)
                {
                    using (RtpExtension extension = packet.GetExtension())
                    {
                        /*if (extension.IsComplete) */
                        sequence = sequence.Concat(extension.Data);

                    }
                }

                sequence = sequence.Concat(packet.Coefficients);
            }

            return sequence;
        }

        #endregion

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Dispose()
        {
            RemoveAllPackets();
        }
    }
}
