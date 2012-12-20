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

        public ulong NtpTimestamp { get; set; }

        public uint RtpTimestamp { get; set; }

        public uint SendersPacketCount { get; set; }

        public uint SendersOctetCount { get; set; }

        public List<ReportBlock> Blocks = new List<ReportBlock>();

        #endregion

        #region Constructor

        public SendersReport(uint ssrc)
        {
            SynchronizationSourceIdentifier = ssrc;
        }

        public SendersReport(byte[] packet, int offset) 
        {            
            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, offset + 0));

            if (packet.Length > 4)
            {

                NtpTimestamp = (ulong)System.Net.IPAddress.NetworkToHostOrder((long)BitConverter.ToInt64(packet, offset + 4));

                RtpTimestamp = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, offset + 12));

                SendersPacketCount = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, offset + 16));

                SendersOctetCount = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, offset + 20));

                offset += 24;

                while (offset < packet.Length)
                {
                    Blocks.Add(new ReportBlock(packet, offset));
                    offset += ReportBlock.Size;
                }
            }

        }

        public SendersReport(RtcpPacket packet) : this(packet.Data, 0) { }

        #endregion

        public virtual RtcpPacket ToPacket()
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.SendersReport);
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
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((long)NtpTimestamp)));
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
