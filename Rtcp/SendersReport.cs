using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class SendersReport
    {
        #region Fields

        //2 Words which make up the NtpTimestamp
        internal uint m_NtpMsw, m_NtpLsw;

        #endregion

        #region Properties

        public uint SynchronizationSourceIdentifier { get; set; }

        public ulong NtpTimestamp { get { return (ulong)m_NtpMsw << 32 | m_NtpLsw; } set { m_NtpLsw = (uint)(value & uint.MaxValue); m_NtpMsw = (uint)(value >> 32); } }

        public uint RtpTimestamp { get; set; }

        public uint SendersPacketCount { get; set; }

        public uint SendersOctetCount { get; set; }

        public List<ReportBlock> Blocks = new List<ReportBlock>();

        public DateTime? Sent { get; set; }

        public DateTime? Created { get; set; }

        #endregion

        #region Constructor

        public SendersReport(uint ssrc)
        {
            SynchronizationSourceIdentifier = ssrc;
            Created = DateTime.UtcNow;
        }

        public SendersReport(byte[] packet, int offset) 
        {            
            SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 0));

            int packetLength = packet.Length;

            if (packetLength > 8)
            {
                //Get the MSW
                m_NtpMsw = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 4));

                //Get the LSW
                m_NtpLsw = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 8));

                if (packetLength > 12) RtpTimestamp = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 12));
                else return;

                if (packetLength > 16) SendersPacketCount = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 16));
                else return;

                if (packetLength > 20) SendersOctetCount = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset + 20));
                else return;

                if (packetLength > 24) offset += 24;
                else return;

                //never trust block count :)
                //while(Blocks.Count < blockCount)
                while (offset + ReportBlock.Size < packet.Length)
                {
                    Blocks.Add(new ReportBlock(packet, ref offset));
                }
            }

        }

        //Should check Conflict avoidance 
        public SendersReport(RtcpPacket packet) : this(packet.Payload, 0) { if (packet.PacketType != RtcpPacket.RtcpPacketType.SendersReport) throw new Exception("Invalid Packet Type, Expected SendersReport. Found: '" + (byte)packet.PacketType + '\''); Created = packet.Created ?? DateTime.UtcNow; }

        #endregion

        public virtual RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.SendersReport, channel);            
            output.Payload = ToBytes();
            output.BlockCount = Blocks.Count;
            return output;
        }

        public virtual byte[] ToBytes(uint? ssrc = null)
        {
            List<byte> result = new List<byte>();
            // SSRC
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)(ssrc ?? SynchronizationSourceIdentifier))));
            
            // NTP timestamp
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(m_NtpMsw)));
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(m_NtpLsw)));

            // RTP timestamp
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(RtpTimestamp)));
            // sender's packet count
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SendersPacketCount)));
            // sender's octet count
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SendersOctetCount)));
            
            //Report Blocks
            foreach (ReportBlock block in Blocks) result.AddRange(block.ToBytes());

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(SendersReport report) { return report.ToPacket(); }

        public static implicit operator SendersReport(RtcpPacket packet) { return new SendersReport(packet); }
    }
}
