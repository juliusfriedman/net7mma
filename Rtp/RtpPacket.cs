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
        /// The maximum size of any given RtpPacket including header overhead
        /// </summary>
        public const int MaxPacketSize = 1500;

        /// <summary>
        /// The maximum size of any given RtpPacket minus the header overhead
        /// </summary>
        public const int MaxPayloadSize = MaxPacketSize - RtpHeaderLength;

        #region Fields

        byte m_PayloadType;

        //Make list for easier writing?
        internal byte[] m_Payload;

        int m_Version, m_Padding, m_Extensions, m_Csc, m_Marker, m_SequenceNumber;

        uint m_TimeStamp, m_Ssrc;

        List<uint> m_ContributingSources = new List<uint>();

        #region Extensions

        ushort m_ExtensionFlags, m_ExtensionLength;
        byte[] m_ExtensionData;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// The Version of the RTP Protocol this packet conforms to
        /// </summary>
        public int Version { get { return m_Version; } set { m_Version = value; } }

        /// <summary>
        /// Indicates if the RTPPacket contains padding at the end which is not part of the payload
        /// </summary>
        public bool Padding { get { return m_Padding > 0; } set { m_Padding = value ? 1 : 0; } }

        /// <summary>
        /// Indicates if the RTPPacket contains extensions
        /// </summary>
        public bool Extensions { get { return m_Extensions > 0; } set { m_Extensions = value ? 1 : 0; } }

        /// <summary>
        /// Indicates the amount of contributing sources
        /// </summary>
        public int ContributingSourceCount { get { return m_Csc; } set { m_Csc = value; } }

        /// <summary>
        /// The list of contributing sources
        /// </summary>
        public List<uint> ContributingSources { get { return m_ContributingSources; } set { m_ContributingSources = value; m_Csc = value.Count; } }

        /// <summary>
        /// Indicates if the RTPPacket contains a Marker flag
        /// </summary>
        public bool Marker { get { return m_Marker > 0; } set { m_Marker = value ? 1 : 0; } }

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
        public int SequenceNumber { get { return m_SequenceNumber; } set { m_SequenceNumber = value; } }

        /// <summary>
        /// The Timestamp of the RTPPacket
        /// </summary>
        public uint TimeStamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        /// <summary>
        /// Identifies the synchronization source for this RtpPacket (SSrc)
        /// </summary>
        public uint SynchronizationSourceIdentifier { get { return m_Ssrc; } set { m_Ssrc = value; } }

        /// <summary>
        /// The binary payload of the RtpPacket
        /// </summary>
        public byte[] Payload { get { return m_Payload; } set { m_Payload = value; } }

        /// <summary>
        /// The Extension Data of the RtpPacket
        /// </summary>
        public byte[] ExtensionData { get { return m_ExtensionData; } set { Extensions = value != null; m_ExtensionData = value; m_ExtensionLength = (ushort)value.Length; } }

        /// <summary>
        /// The Extension flags of the RtpPacket
        /// </summary>
        public ushort ExtensionFlags { get { return m_ExtensionFlags; } set { m_ExtensionFlags = value; } }

        /// <summary>
        /// Gets the ExtensionData of the RtpPacket including Flags and Length
        /// </summary>
        public byte[] ExtensionBytes
        {
            get
            {

                if (!Extensions) return null;

                List<byte> result = new List<byte>();

                result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_ExtensionFlags)));

                result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_ExtensionLength)));

                result.AddRange(m_ExtensionData);

                return result.ToArray();
            }
        }

        /// <summary>
        /// The length of the packet in bytes
        /// </summary>
        public int Length { get { return RtpHeaderLength + (ContributingSources.Count * 4) + (Extensions ? ExtensionBytes.Length : 0) + (Payload != null ? Payload.Length : 0); } }

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

        public RtpPacket() { m_Payload = new byte[MaxPayloadSize]; Created = DateTime.UtcNow; Version = 2; }

        public RtpPacket(ArraySegment<byte> packet, byte? channel = null)
        {
            //Ensure correct length
            if (packet.Count <= RtpHeaderLength) throw new ArgumentException("The packet does not conform to the RTP Protocol. Packets must exceed 12 bytes in length.", "packet");

            Created = DateTime.UtcNow;

            Channel = channel;

            int localOffset = 0, payloadLen = -1;

            //Handle tcp frame headers if required
            if (packet.Array[packet.Offset] == RtpClient.MAGIC)
            {
                localOffset = 4;
                payloadLen = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(packet.Array, packet.Offset + 2)) - RtpHeaderLength;
            }

            //Extract fields
            byte compound = packet.Array[localOffset + packet.Offset];

            //Version, Padding flag, Extension flag, and Contribuing Source Count
            m_Version = compound >> 6; ;

            //We only parse version 2
            if (m_Version != 2) throw new ArgumentException("Only Version 2 is Defined");

            m_Padding = (0x1 & (compound >> 5));
            m_Extensions = (0x1 & (compound >> 4));
            m_Csc = 0x1F & compound;

            //Extract Marker flag and payload type
            compound = packet.Array[localOffset + packet.Offset + 1];

            Marker = ((compound >> 7) == 1);
            m_PayloadType = (byte)(compound & 0x7f);

            //Extract Sequence Number
            SequenceNumber = Utility.SwapUnsignedShort(System.BitConverter.ToUInt16(packet.Array, localOffset + packet.Offset + 2));

            //Extract Time Stamp
            m_TimeStamp = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet.Array, localOffset + packet.Offset + 4));

            m_Ssrc = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet.Array, localOffset + packet.Offset + 8));

            int position = localOffset + 12;

            //Extract Contributing Sources
            for (int i = 0; i < m_Csc; ++i, position += 4) m_ContributingSources.Add(Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet.Array, localOffset + packet.Offset + position)));

            //Extract Extensions
            //This might not be needed
            if (Extensions)
            {
                m_ExtensionFlags = Utility.SwapUnsignedShort(System.BitConverter.ToUInt16(packet.Array, localOffset + packet.Offset + position));
                m_ExtensionLength = Utility.SwapUnsignedShort(System.BitConverter.ToUInt16(packet.Array, localOffset + packet.Offset + position + 2));
                m_ExtensionData = new byte[m_ExtensionLength];
                Array.Copy(packet.Array, localOffset + packet.Offset + position + 4, m_ExtensionData, 0, m_ExtensionLength);
                position += 4 + m_ExtensionLength;
            }

            //Extract payload
            int payloadSize = packet.Count - localOffset - position;
            
            //If the data was recieved late on a Tcp socket then the size at the beginning may be invalid.. 
            //System.Diagnostics.Debug.WriteLine((ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(packetReference.Array, 2)));
            if (payloadSize == -1)
            {
                //The real size is the size of the slice in total
                payloadSize = packet.Array.Length - localOffset - position; /* - RtpHeaderLength*/
            }

            //If we had a known length we will use it here to prevent resizing later
            if (payloadLen != -1)
            {
                payloadSize = payloadLen;
            }

            //Create the payload
            m_Payload = new byte[payloadSize];

            //Copy the data to the payload
            Array.Copy(packet.Array, packet.Offset + position, m_Payload, 0, payloadSize);
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
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_SequenceNumber)));

            //Add the Timestamp
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)m_TimeStamp)));

            //Add the SynchonrizationSourceIdentifier
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)(ssrc ?? m_Ssrc))));

            if (ContributingSourceCount > 0)
            {
                //Loop the sources and add them to the header
                m_ContributingSources.ForEach(cs => result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)cs))));
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
