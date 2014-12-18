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
    public class RtpFrame : Media.Common.BaseDisposable, System.Collections.IEnumerable, IEnumerable<RtpPacket>// IDictionary, etc?
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

        public readonly DateTime Created;

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
        public virtual bool IsComplete { get { return !IsEmpty && !IsMissingPackets && HasMarker; } }

        /// <summary>
        /// Indicates if all contained packets are sequential up the Highest Sequence Number contained in the RtpFrame.
        /// (Must have 1 packet with marker or more than 1 packet)
        /// </summary>
        public virtual bool IsMissingPackets
        {
            get
            {
                int  count = m_Packets.Count,
                    highestSequenceNumber = count > 0 ? HighestSequenceNumber : -1; return (count == 1 && !HasMarker) || count >= 2 && !m_Packets.All((a) =>
                {
                    RtpPacket p = a.Value;
                    if (p.SequenceNumber == highestSequenceNumber) return true;
                    int nextKey = p.SequenceNumber + 1;
                    if (nextKey == highestSequenceNumber) return true;
                    return m_Packets.ContainsKey(nextKey);
                });
            }
        }

        /// <summary>
        /// Indicates if any packets have a marker
        /// </summary>
        public virtual bool HasMarker { get { return m_Packets.Last().Value.Marker; } } // m_Packets.Any(a => a.Value.Marker); } }

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
        public int HighestSequenceNumber { get {return (Count > 0 ? m_Packets.Values.Last().SequenceNumber : 0); }}

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
        public IEnumerator<RtpPacket> GetEnumerator() { return m_Packets.Values.Distinct().GetEnumerator(); }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame. The first packet added sets the SynchronizationSourceIdentifier and Timestamp if not already set.
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        public virtual void Add(RtpPacket packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");

            //Check payload type
            if (packet.PayloadType != m_PayloadByte) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");

            int count = Count, ssrc = packet.SynchronizationSourceIdentifier, seq = packet.SequenceNumber, ts = packet.Timestamp;

            if (count == 0)
            {
                if(m_Ssrc != ssrc) m_Ssrc = ssrc;

                if(m_Timestamp != ts) m_Timestamp = ts;
            }
            else
            {
                if (ssrc != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");
                
                if (ts != Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");
                
                if (count >= MaxPackets) throw new InvalidOperationException(string.Format("The amount of packets contained in a RtpFrame cannot exceed: {0}", MaxPackets));
            }

            //If the last packet has the marker bit then no more packets can be added unless they are from a lower sequence number
            if (count > 0 && seq > HighestSequenceNumber && HasMarker) throw new InvalidOperationException("Complete frame cannot have additional packets added");

            //Dont use a SortedDictionary just to ensure only a single key in the hash, (Use List and Distinct)
            //Add the packet to the SortedList which will not throw any exception if the RtpPacket added already contains a value.                       
            m_Packets.Add(seq, packet);
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
        public virtual void RemoveAllPackets() { if(m_Packets != null) m_Packets.Clear(); }

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
                if (useExtensions && packet.Extension)
                {
                    using (RtpExtension extension = packet.GetExtension())
                    {
                        if (extension != null)
                        {
                            /*if (extension.IsComplete) */
                            sequence = sequence.Concat(extension.Data);
                        }
                    }
                }

                //Should chyeck Coefficients is > profileHeaderSize ?

                sequence = sequence.Concat(packet.Coefficients.Skip(profileHeaderSize));
            }

            return sequence;
        }

        #endregion

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Dispose()
        {
            if (Disposed) return;
            base.Dispose();
            RemoveAllPackets();
        }
    }
}
