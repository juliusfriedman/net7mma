using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class SendersReport
    {

        #region Properties

        public uint SynchronizationSourceIdentifier { get; set; }

        internal uint m_NtpMsw, m_NtpLsw;
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

        public SendersReport(byte[] packet, int offset/*, int blockCount = 0*/) 
        {            
            SynchronizationSourceIdentifier = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 0));

            int packetLength = packet.Length;

            if (packetLength > 8)
            {

                //Should actually be 2 words instead.... Terrible I know but stilll
                //NtpTimestamp = (ulong)System.Net.IPAddress.NetworkToHostOrder((long)BitConverter.ToInt64(packet, offset + 4)); 
                
                m_NtpMsw = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 4));
                m_NtpLsw = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 8));

                if (packetLength > 12) RtpTimestamp = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 12));
                else return;

                if (packetLength > 16) SendersPacketCount = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 16));
                else return;

                if (packetLength > 20) SendersOctetCount = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 20));
                else return;

                if (packetLength > 24) offset += 24;
                else return;

                //while(Blocks.Count < blockCount)
                while (offset + ReportBlock.Size < packet.Length)
                {
                    Blocks.Add(new ReportBlock(packet, ref offset));
                    //offset += ReportBlock.Size;
                }
            }

        }

        public SendersReport(RtcpPacket packet) : this(packet.Data, 0/*, packet.BlockCount*/) { if (packet.PacketType != RtcpPacket.RtcpPacketType.SendersReport) throw new Exception("Invalid Packet Type"); Created = packet.Created ?? DateTime.UtcNow; }

        #endregion

        public virtual RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.SendersReport, channel);            
            output.Data = ToBytes();
            output.BlockCount = Blocks.Count;
            return output;
        }

        public virtual byte[] ToBytes(uint? ssrc = null)
        {
            List<byte> result = new List<byte>();
            // SSRC
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)(ssrc ?? SynchronizationSourceIdentifier))));
            
            // NTP timestamp
            //result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((long)NtpTimestamp)));
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)m_NtpMsw)));
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)m_NtpLsw)));

            // RTP timestamp
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)RtpTimestamp)));
            // sender's packet count
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SendersPacketCount)));
            // sender's octet count
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SendersOctetCount)));
            //Report Blocks
            foreach (ReportBlock block in Blocks) result.AddRange(block.ToBytes());

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(SendersReport report) { return report.ToPacket(); }

        public static implicit operator SendersReport(RtcpPacket packet) { return new SendersReport(packet); }
    }
}
