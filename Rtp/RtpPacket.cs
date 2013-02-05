using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// http://www.ietf.org/rfc/rfc3550.txt
    /// </summary>
    public class RtpPacket
    {
        /// <summary>
        /// The header length (and subsequently) the minimum size of any given RtpPacket
        /// </summary>
        public const int RtpHeaderLength = 12;

        /// <summary>
        /// The maximum size of any given RtpPacket including header overhead and framing bytes
        /// </summary>
        public const int MaxPacketSize = 1500;

        /// <summary>
        /// The maximum size of any given RtpPacket minus the header overhead
        /// </summary>
        public const int MaxPayloadSize = MaxPacketSize - RtpHeaderLength;

        #region Fields

        byte m_PayloadType;

        //Make list for easier writing (.Net 4.5 ArraySegment is IEnumerable)
        internal byte[] m_Payload;// = new List<byte>();

        byte m_Version, m_Padding, m_Extensions, m_Csc, m_Marker;
        ushort m_SequenceNumber;

        uint m_TimeStamp, m_Ssrc;

        List<uint> m_ContributingSources = new List<uint>();

        #region Extensions

        internal ushort m_ExtensionFlags, m_ExtensionLength;
        internal byte[] m_ExtensionData;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// The Version of the RTP Protocol this packet conforms to
        /// </summary>
        public int Version { get { return m_Version; } set { m_Version = (byte)value; } }

        /// <summary>
        /// Indicates if the RTPPacket contains padding at the end which is not part of the payload
        /// </summary>
        public bool Padding { get { return m_Padding > 0; } set { m_Padding = value ? (byte)1 : (byte)0; } }

        /// <summary>
        /// Indicates if the RTPPacket contains extensions
        /// </summary>
        public bool Extensions { get { return m_Extensions > 0; } set { m_Extensions = value ? (byte)1 : (byte)0; } }

        /// <summary>
        /// Indicates the amount of contributing sources
        /// </summary>
        public int ContributingSourceCount { get { return m_Csc; } internal set { m_Csc = (byte)value; if (value == 0) m_ContributingSources.Clear(); } }

        /// <summary>
        /// The list of contributing sources
        /// </summary>
        public List<uint> ContributingSources { get { return m_ContributingSources; } set { m_ContributingSources = value; m_Csc = (byte)value.Count; } }

        /// <summary>
        /// Indicates if the RTPPacket contains a Marker flag
        /// </summary>
        public bool Marker { get { return m_Marker > 0; } set { m_Marker = value ? (byte)1 : (byte)0; } }

        /// <summary>
        /// Indicates the format of the data within the Payload
        /// </summary>
        public byte PayloadType
        {
            get { return m_PayloadType; }
            set
            {
                if (value > 127)
                {
                    throw new ArgumentOutOfRangeException("PayloadType" + " is a seven bit structure, and can hold values between 0 and 127");
                }
                else
                {
                    m_PayloadType = (byte)(value & 0x7f);
                }
            }
        }

        /// <summary>
        /// The sequence number of the RtpPacket
        /// </summary>
        public ushort SequenceNumber { get { return m_SequenceNumber; } set { m_SequenceNumber = value; } }

        /// <summary>
        /// The Timestamp of the RtpPacket
        /// </summary>
        public uint TimeStamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        /// <summary>
        /// Identifies the Source Id for this RtpPacket (Ssrc)
        /// </summary>
        public uint SynchronizationSourceIdentifier { get { return m_Ssrc; } set { m_Ssrc = value; } }

        /// <summary>
        /// The payload of the RtpPacket
        /// </summary>
        public byte[] Payload { get { return m_Payload; } set { m_Payload = value; } }

        /// <summary>
        /// The Extension Data of the RtpPacket (only available when Extensions is true)
        /// </summary>
        public byte[] ExtensionData { get { return m_ExtensionData; } set { Extensions = value != null; if (value != null && value.Length % 4 != 0) throw new ArgumentException("Extension data length must be a multiple of 32"); m_ExtensionData = value; m_ExtensionLength = (value == null ? ushort.MinValue : (ushort)(value.Length / 4)); } }

        /// <summary>
        /// The Extension flags of the RtpPacket
        /// </summary>
        public ushort ExtensionFlags { get { return m_ExtensionFlags; } set { m_ExtensionFlags = value; } }

        /// <summary>
        /// The Length of the ExtensionData in 32 bit (only available when Extensions is true)
        /// </summary>
        public ushort ExtensionLength { get { return m_ExtensionLength; } set { m_ExtensionLength = value; } }

        /// <summary>
        /// Gets the ExtensionData of the RtpPacket including Flags and Length
        /// </summary>
        public byte[] ExtensionBytes
        {
            get
            {

                if (!Extensions) return null;

                List<byte> result = new List<byte>();

                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(m_ExtensionFlags)));

                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(m_ExtensionLength)));

                result.AddRange(m_ExtensionData);

                return result.ToArray();
            }
            set
            {
                if (value == null)
                {
                    m_ExtensionFlags = m_ExtensionLength = m_Extensions = 0;
                }
                else
                {

                    m_ExtensionFlags = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(value, 0));

                    m_ExtensionLength = (ushort)(4 * Utility.ReverseUnsignedShort(BitConverter.ToUInt16(value, 2)));

                    m_ExtensionData = new byte[m_ExtensionLength];

                    System.Array.Copy(value, 4, m_ExtensionData, 0, m_ExtensionLength);
                }
            }
        }

        /// <summary>
        /// The length of the packet in bytes including the RtpHeader
        /// </summary>
        public int Length { get { return RtpHeaderLength + (ContributingSources.Count > 0 ? ContributingSources.Count * 4 : 0) + (m_ExtensionData != null ? 4 + m_ExtensionData.Length : 0) + (Payload != null ? Payload.Length : 0); } }

        /// <summary>
        /// The channel to send the RtpPacket on or the channel it was received from
        /// </summary>
        public byte? Channel { get; set;}

        /// <summary>
        /// The time the packet instance was created
        /// </summary>
        public DateTime? Created { get; set; }

        #endregion

        #region Constructor

        public RtpPacket(int payloadSize = MaxPayloadSize) { m_Payload = new byte[payloadSize]; Created = DateTime.UtcNow; Version = 2; }

        public RtpPacket(ArraySegment<byte> packetReference, byte? channel = null)
        {
            //Ensure correct length
            if (packetReference.Count < RtpHeaderLength) throw new ArgumentException("The packet does not conform to the Real Time Protocol. Packets must at least 12 bytes in length.", "packet");

            Created = DateTime.UtcNow;

            Channel = channel;

            int localOffset = 0, payloadLen = -1;

            //Handle tcp frame headers if required
            if (packetReference.Array[packetReference.Offset] == RtpClient.MAGIC)
            {
                localOffset = 4;
                payloadLen = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(packetReference.Array, packetReference.Offset + 2)) - RtpHeaderLength;
            }

            //Extract fields
            byte compound = packetReference.Array[localOffset + packetReference.Offset];

            //Version, Padding flag, Extension flag, and Contribuing Source Count
            m_Version = (byte)(compound >> 6);

            //We only parse version 2
            if (m_Version != 2) throw new ArgumentException("Only Version 2 is Defined");

            m_Padding = (byte)(0x1 & (compound >> 5));
            m_Extensions = (byte)(0x1 & (compound >> 4));
            m_Csc = (byte)(0x0F & compound);

            //Extract Marker flag and payload type
            compound = packetReference.Array[localOffset + packetReference.Offset + 1];

            Marker = ((compound >> 7) == 1);
            m_PayloadType = (byte)(compound & 0x7f);

            //Extract Sequence Number
            SequenceNumber = Utility.ReverseUnsignedShort(System.BitConverter.ToUInt16(packetReference.Array, localOffset + packetReference.Offset + 2));

            //Extract Time Stamp
            m_TimeStamp = Utility.ReverseUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, localOffset + packetReference.Offset + 4));

            m_Ssrc = Utility.ReverseUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, localOffset + packetReference.Offset + 8));

            if (packetReference.Count <= RtpHeaderLength) return;

            int position = localOffset + RtpHeaderLength;

            //Extract Contributing Sources
            for (int i = 0; i < m_Csc; ++i, position += 4) m_ContributingSources.Add(Utility.ReverseUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, localOffset + packetReference.Offset + position)));

            //Extract Extensions
            //This might not be needed
            if (Extensions)
            {
                m_ExtensionFlags = Utility.ReverseUnsignedShort(System.BitConverter.ToUInt16(packetReference.Array, localOffset + packetReference.Offset + position));
                m_ExtensionLength = (ushort)(4 * Utility.ReverseUnsignedShort(System.BitConverter.ToUInt16(packetReference.Array, localOffset + packetReference.Offset + position + 2)));
                m_ExtensionData = new byte[m_ExtensionLength];
                Array.Copy(packetReference.Array, localOffset + packetReference.Offset + position + 4, m_ExtensionData, 0, m_ExtensionLength);
                position += 4 + m_ExtensionLength;
            }

            //Extract payload
            int payloadSize = packetReference.Count - localOffset - position;
            
            //If the data was recieved late on a Tcp socket then the size at the beginning may be invalid.. 
            //System.Diagnostics.Debug.WriteLine((ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(packetReference.Array, 2)));
            if (payloadSize == -1)
            {
                //The real size is the size of the slice in total
                payloadSize = packetReference.Array.Length - localOffset - position; /* - RtpHeaderLength*/
            }

            //If we had a known length we will use it here to prevent resizing later
            if (payloadLen != -1)
            {
                payloadSize = payloadLen;
            }

            //Create the payload
            m_Payload = new byte[payloadSize];

            //Array segment needs to be enumerable
            //m_Payload.AddRange(new ArraySegment<byte>(packet.Array, packet.Offset + position, payloadSize));

            //Copy the data to the payload
            Array.Copy(packetReference.Array, packetReference.Offset + position, m_Payload, 0, payloadSize);
        }

        public RtpPacket(byte[] packet, int offset = 0)
            : this (new ArraySegment<byte>(packet, offset, packet.Length - offset))
        {
            
        }

        #endregion

        #region Methods        

        /// <summary>
        /// Encodes the RTPHeader and Payload into a RTPPacket
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes(uint? ssrc = null)
        {
            List<byte> result = new List<byte>();

            //Add the version
            result.Add((byte)(m_Version << 6));

            //Flag in Padding if required
            if (Padding) result[0] = (byte)(result[0] | 0x20);

            //Flag in Extensions if required
            if (Extensions) result[0] = (byte)(result[0] | 0x10);

            //Flag in the ContributingSourceCount
            result[0] = (byte)(result[0] | m_Csc & 0xff);

            //Add the PayloadType
            result.Add((byte)( ((Marker ? 1 : 0) << 7) | m_PayloadType));

            //Add the SequenceNumber
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(m_SequenceNumber)));

            //Add the Timestamp
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(m_TimeStamp)));

            //Add the SynchonrizationSourceIdentifier
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt((ssrc ?? m_Ssrc))));

            if (ContributingSourceCount > 0)
            {
                //Loop the sources and add them to the header
                m_ContributingSources.ForEach(cs => result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(cs))));
            }

            //If extensions were flagged then include the extensions
            if (Extensions)
            {
                result.AddRange(ExtensionBytes);
            }

            //Include the payload
            result.AddRange(m_Payload);

            //Return the array
            return result.ToArray();
        }

        #endregion

    }    
}
