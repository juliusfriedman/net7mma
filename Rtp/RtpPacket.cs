﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtp
{
    /// <summary>
    /// http://www.ietf.org/rfc/rfc3550.txt
    /// </summary>
    public class RtpPacket
    {
        internal const int HeaderLength = 12;

        internal const int MaxSize = 1500;

        internal const int MaxPayloadSize = MaxSize - HeaderLength;

        #region Fields

        byte m_PayloadType;

        byte[] m_Payload;

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
        /// Indicates the format of the data withing the Payload
        /// </summary>
        public byte PayloadType { get { return m_PayloadType; } set { m_PayloadType = value; } }

        /// <summary>
        /// The sequence number of the RTPPacket
        /// </summary>
        public int SequenceNumber { get { return m_SequenceNumber; } set { m_SequenceNumber = value; } }

        /// <summary>
        /// The Timestamp of the RTPPacket
        /// </summary>
        public uint TimeStamp { get { return m_TimeStamp; } set { m_TimeStamp = value; } }

        /// <summary>
        /// Identifies the synchronization source for this RTPPacket (SSrc)
        /// </summary>
        public uint SynchronizationSourceIdentifier { get { return m_Ssrc; } set { m_Ssrc = value; } }

        /// <summary>
        /// The binary payload of the RTPPacket
        /// </summary>
        public byte[] Payload { get { return m_Payload; } set { m_Payload = value; } }

        /// <summary>
        /// The length of the packet in bytes
        /// </summary>
        public int Length { get { return HeaderLength + (ContributingSources.Count * 4) + (Payload != null ? Payload.Length : 0); } }

        #endregion

        #region Constructor

        public RtpPacket() { }

        public RtpPacket(ArraySegment<byte> packetReference)
        {
            //Ensure correct length
            if (packetReference.Count <= HeaderLength) throw new ArgumentException("The packet does not conform to the RTP Protocol. Packets must exceed 12 Bytes.", "packet");

            //Could get $, RtpChannel, Len here for Tcp to make reciving better

            //Extract fields
            byte compound = packetReference.Array[0];

            //Version, Padding flag, Extension flag, and Contribuing Source Count
            m_Version = compound >> 6; ;
            m_Padding = (0x1 & (compound >> 5));
            m_Extensions = (0x1 & (compound >> 4));
            m_Csc = 0x1F & compound;

            //Extract Marker flag and payload type
            compound = packetReference.Array[1];

            Marker = ((compound >> 7) == 1);
            m_PayloadType = (byte)(compound & 0x7f);

            //Extract Sequence Number
            SequenceNumber = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packetReference.Array, 2));

            //Extract Time Stamp
            m_TimeStamp = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, 4));

            m_Ssrc = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, 8));

            int position = 12;

            //Extract Contributing Sources
            for (int i = 0; i < m_Csc; ++i, position += 4) m_ContributingSources.Add(Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packetReference.Array, position)));

            //Extract Extensions
            if (Extensions)
            {
                m_ExtensionFlags = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packetReference.Array, position));
                m_ExtensionLength = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packetReference.Array, position + 2));
                m_ExtensionData = new byte[m_ExtensionLength];
                Array.Copy(packetReference.Array, position + 4, m_ExtensionData, 0, m_ExtensionLength);
                position += 4 + m_ExtensionLength;
            }

            //Extract payload
            int payloadSize = packetReference.Count - position;
            m_Payload = new byte[payloadSize];

            Array.Copy(packetReference.Array, position, m_Payload, 0, payloadSize);
        }

        public RtpPacket(byte[] packet, int offset = 0)
        {
            //Ensure correct length
            if (packet.Length <= HeaderLength) throw new ArgumentException("The packet does not conform to the RTP Protocol. Packets must exceed 12 Bytes.", "packet");

            //Could get $, RtpChannel, Len here for Tcp to make reciving better

            //Extract fields
            //Version, Padding flag, Extension flag, and Contribuing Source Count
            m_Version = packet[offset + 0] >> 6; ;
            m_Padding = (0x1 & (packet[offset + 0] >> 5));
            m_Extensions = (0x1 & (packet[offset + 0] >> 4));
            m_Csc = 0x1F & (packet[offset + 0]);

            //Extract Marker flag and payload type
            Marker = ((packet[offset + 1] >> 7) == 1);
            m_PayloadType = (byte)(packet[offset + 1] & 0x7f);

            //Extract Sequence Number
            SequenceNumber = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packet, offset + 2));

            //Extract Time Stamp
            m_TimeStamp = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet, offset + 4));

            m_Ssrc = Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet, offset + 8));

            int position = offset + 12;

            //Extract Contributing Sources
            for (int i = 0; i < m_Csc; ++i, position += 4) m_ContributingSources.Add(Utility.SwapUnsignedInt(System.BitConverter.ToUInt32(packet, position)));

            //Extract Extensions
            if (Extensions)
            {
                m_ExtensionFlags = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packet, position));
                m_ExtensionLength = Utility.HostToNetworkOrderShort(System.BitConverter.ToUInt16(packet, position + 2));
                m_ExtensionData = new byte[m_ExtensionLength];
                Array.Copy(packet, position + 4, m_ExtensionData, 0, m_ExtensionLength);
                position += 4 + m_ExtensionLength;
            }

            //Extract payload
            int payloadSize = packet.Length - position;
            m_Payload = new byte[payloadSize];

            Array.Copy(packet, position, m_Payload, 0, payloadSize);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Encodes the RTPHeader and Payload into a RTPPacket
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes(bool headerOnly = false, uint? ssrc = null)
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
            result.Add((byte)( ((Marker ? 1 : 0) << 7) | PayloadType));

            //Add the SequenceNumber
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_SequenceNumber)));

            //Add the Timestamp
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)m_TimeStamp)));

            //Add the SynchonrizationSourceIdentifier
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)(ssrc ?? m_Ssrc))));

            if (ContributingSourceCount > 0)
            {
                //Loop the sources and add them to the header
                foreach (uint src in m_ContributingSources) result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)src)));
            }

            //If extensions were flagged then include the extensions
            if (Extensions)
            {
                result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_ExtensionFlags)));

                result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)m_ExtensionLength)));

                result.AddRange(m_ExtensionData);
            }

            //Include the payload if required
            if (!headerOnly) result.AddRange(m_Payload);

            //Return the array
            return result.ToArray();
        }

        #endregion

    }    
}
