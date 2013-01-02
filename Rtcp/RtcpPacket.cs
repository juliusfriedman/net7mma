﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class RtcpPacket
    {
        #region Nested Types

        public enum RtcpPacketType
        {
            SendersReport = 200,
            ReceiversReport= 201,
            SourceDescription = 202,
            Goodbye = 203,
            ApplicationSpecific = 204
        }

        #endregion

        #region Fields

        int m_Version = 2, m_Padding, m_Count;

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
        /// The length of this Rtcp Packet in Bytes
        /// </summary>
        public short Length { get { return (short)(m_Length * 4); } set { m_Length = (short)(value / 4); } }

        /// <summary>
        /// The type of RtcpPacket
        /// </summary>
        public RtcpPacketType PacketType { get { return (RtcpPacketType)m_Type; } set { m_Type = (byte)value; } }

        /// <summary>
        /// The data contained within the Rtcp Packet
        /// </summary>
        public byte[] Data { get { return m_Data; } set { m_Data = value; Length = (short)value.Length; } }

        public int BlockCount { get { return m_Count; } set { m_Count = value; } }

        /// <summary>
        /// The optional channel to send the RtcpPacket on or the channel the RtcpPacket was received on
        /// </summary>
        public byte? Channel { get; set; }

        public DateTime? Created {get; set;}

        #endregion

        #region Constructor

        public RtcpPacket(ArraySegment<byte> packetReference, int offset = 0)
        {
            Created = DateTime.UtcNow;

            //Get version
            m_Version = packetReference.Array[offset + 0] >> 6; ;
            m_Padding = (0x1 & (packetReference.Array[offset + 0] >> 5));

            //Count - FOR SS and RR this is exact size
            m_Count = packetReference.Array[offset + 0] & 0x1F;

            //Type
            m_Type = packetReference.Array[offset + 1];

            //Length Should be Block Count?
            m_Length = (short)(packetReference.Array[offset + 2] << 8 | packetReference.Array[offset + 3]);


            if (m_Length < 0 || m_Length > 1500) m_Length = (short)m_Count;

            //Extract Data
            m_Data = new byte[Length];
            Array.Copy(packetReference.Array, offset + 4, m_Data, 0, Length);
        }

        public RtcpPacket(byte[] packet, int offset = 0) : this(new ArraySegment<byte>(packet, offset, packet.Length - offset)) { }

        public RtcpPacket(RtcpPacketType type, byte? channel = null)
        {
            Created = DateTime.Now;
            m_Type = (byte)type;
            Channel = channel;
        }

        #endregion

        #region Methods

        public static RtcpPacket[] GetPackets(ArraySegment<byte> bufferReference, int offset = 0)
        {
            List<RtcpPacket> packets = new List<RtcpPacket>();
            
            int index = offset;

            while (index < bufferReference.Count)
            {
                //Frame Header {$,/0x,/0x,/0x}
                //if (bufferReference.Array[index] == 36) index += 4;
                //Should verify packets types...
                Rtcp.RtcpPacket packet = new Rtcp.RtcpPacket(bufferReference, index);
                packets.Add(packet);
                index += packet.Length + 4;
            }

            return packets.ToArray();
        }

        public static RtcpPacket[] GetPackets(byte[] buffer, int offset = 0)
        {
            return GetPackets(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), offset);
        }

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
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(m_Length)));
            //Data
            result.AddRange(Data);
            return result.ToArray();
        }

        #endregion
    }
}
