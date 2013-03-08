using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class RtcpPacket
    {
        /// <summary>
        /// The header length (and subsequently) the minimum size of any given RtcpPacket
        /// </summary>
        public const int RtcpHeaderLength = 4;

        #region Nested Types

        public enum RtcpPacketType
        {
            //72-76  Reserved for RTCP conflict avoidance   
            SendersReport = 200,
            ReceiversReport= 201,
            SourceDescription = 202,
            Goodbye = 203,
            ApplicationSpecific = 204
        }

        #endregion

        #region Fields

        int m_Version , m_Padding, m_Count;

        short m_Length;

        byte m_Type;

        byte[] m_Data;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates the version of this RtcpPacket
        /// </summary>
        public int Version { get { return m_Version; } set { m_Version = value; } }

        /// <summary>
        /// Indicates if the RtcpPacket contains padding
        /// </summary>
        public bool Padding { get { return Convert.ToBoolean(m_Padding); } set { if (value) m_Padding = 1; else m_Padding = 0; } }

        /// <summary>
        /// The length of this Rtcp Packet in Bytes (Not including the RtcpHeader)
        /// </summary>
        public short Length { get { return (short)(m_Length * 4); } set { m_Length = (short)(value / 4); } }

        /// <summary>
        /// The length of this Rtcp Packet in Bytes Including the RtcpHeader
        /// </summary>
        public short PacketLength { get { return (short)(RtcpHeaderLength + Length); } }

        /// <summary>
        /// The type of RtcpPacket
        /// </summary>
        public RtcpPacketType PacketType { get { return (RtcpPacketType)m_Type; } set { m_Type = (byte)value; } }

        /// <summary>
        /// The data contained within the Rtcp Packet
        /// </summary>
        public byte[] Payload { get { return m_Data; } set { m_Data = value; Length = (short)value.Length; } }

        /// <summary>
        /// Typically to indicate the amount of ReportBlocks contained in Data for PacketTypes which support such (RecieversReport, SendersReport)
        /// </summary>
        public int BlockCount { get { return m_Count; } set { m_Count = value; } }

        /// <summary>
        /// The optional channel to send the RtcpPacket on or the channel the RtcpPacket was received on
        /// </summary>
        public byte? Channel { get; set; }

        public DateTime? Created {get; set;}

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a RtcpPacket from the given ArraySegment at the additional optional offset into the ArraySegment
        /// </summary>
        /// <param name="packetReference">The ArraySegment containing the RtcpPacket to parse</param>
        /// <param name="offset">The optional offset of the RtcpPacket to parse</param>
        public RtcpPacket(ArraySegment<byte> packetReference, int offset = 0)
        {
            Created = DateTime.UtcNow;

            //Frame Header {$,/0x,/0x,/0x}
            if (packetReference.Array[packetReference.Offset + offset] == Rtp.RtpClient.MAGIC) offset += 4;

            //Ensure correct length
            if (packetReference.Count <= RtcpHeaderLength) throw new ArgumentException("The packet does not conform to the Real Time Protocol. Packets must exceed 4 bytes in length.", "packetReference");

            //Get version
            m_Version = packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 4] >> 6;

            //Double check to make sure we are parsing a known format
            if (m_Version != 2) throw new ArgumentException("Only Version 2 is Defined");

            m_Padding = (0x1 & (packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 4] >> 5));

            //Count - FOR SS and RR this is exact size
            m_Count = packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 4] & 0x1F;

            //Type
            m_Type = packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 3];

            //Length in words (not including RtcpHeaderLength)
            m_Length = (short)(packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 2] << 8 | packetReference.Array[packetReference.Offset + offset + RtcpHeaderLength - 1]);

            //Extract Data
            m_Data = new byte[Length];
            Array.Copy(packetReference.Array, packetReference.Offset + offset + RtcpHeaderLength, m_Data, 0, Length);
        }

        public RtcpPacket(byte[] packet, int offset = 0) : this(new ArraySegment<byte>(packet, offset, packet.Length - offset)) { }

        public RtcpPacket(RtcpPacketType type, byte? channel = null)
        {
            Version = 2;
            Created = DateTime.UtcNow;
            m_Type = (byte)type;
            Channel = channel;
        }

        #endregion

        #region Methods

        public static bool IsKnownPacketType(byte suspect) { return suspect >= (byte)RtcpPacket.RtcpPacketType.SendersReport && suspect <= (byte)RtcpPacket.RtcpPacketType.ApplicationSpecific || suspect >= 72 && suspect <= 76; }

        public static RtcpPacket[] GetPackets(ArraySegment<byte> bufferReference)
        {
            int offset = bufferReference.Offset;
            return GetPackets(bufferReference, ref offset);
        }

        public static RtcpPacket[] GetPackets(ArraySegment<byte> bufferReference, ref int offset)
        {
            List<RtcpPacket> packets = new List<RtcpPacket>();

            int localOffset = bufferReference.Array[bufferReference.Offset + offset] == Rtp.RtpClient.MAGIC ? 5 : 1;

            while (offset + RtcpHeaderLength < bufferReference.Count && IsKnownPacketType(bufferReference.Array[localOffset + bufferReference.Offset + offset]))
            {
                try
                {
                    Rtcp.RtcpPacket packet = new Rtcp.RtcpPacket(bufferReference, offset);
                    if (packet.Length == 0) break;
                    packets.Add(packet);
                    offset += packet.PacketLength;
                }
                catch
                {
                    break;
                }
            }

            return packets.ToArray();
        }

        /// <summary>
        /// Retrieves all RtcpPackets contained in the given array
        /// </summary>
        /// <param name="buffer">The byte array to check for packets</param>
        /// <param name="offset">The offset to start checking at</param>
        /// <returns>The array of packets decoded from the buffer</returns>
        public static RtcpPacket[] GetPackets(byte[] buffer, int offset = 0) { return GetPackets(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), ref offset); }

        /// <summary>
        /// Provides the binary representation of the RtcpPacket ready to be sent on a <see cref="System.Net.Sockets.Socket"/>
        /// </summary>
        /// <returns>The packet ready to be sent</returns>
        public virtual byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            //Version
            result.Add((byte)(m_Version << 6));
            //Padding
            if (Padding) result[0] |= 0x20;
            //Blockcount
            result[0] |= (byte)(BlockCount & 0x1f);
            result.Add(m_Type);
            //Length
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(m_Length)));
            //Data
            result.AddRange(Payload);
            return result.ToArray();
        }

        #endregion
    }
}
