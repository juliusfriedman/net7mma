using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class ReceiversReport
    {
        public List<ReportBlock> Blocks = new List<ReportBlock>();

        public uint SynchronizationSourceIdentifier { get; set; }

        #region Constructor

        public ReceiversReport(uint ssrc) { SynchronizationSourceIdentifier = ssrc; }
        
        public ReceiversReport(RtcpPacket packet) : this(packet.Data, 0) { }

        //Evidence suggests that there is differences is the values so this cannot inherit from senders reprot
        public ReceiversReport(byte[] packet, int offset) 
        {
            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, offset + 0));
            offset += 4; 
            if (packet.Length > 4)
            {
                while (offset < packet.Length)
                {
                    Blocks.Add(new ReportBlock(packet, offset));
                    offset += ReportBlock.Size;
                }
            }
        }

        #endregion

        public virtual RtcpPacket ToPacket()
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.ReceiversReport);
            output.Data = ToBytes();
            output.BlockCount = Blocks.Count;
            return output;
        }

        public virtual byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            
            // SSRC
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SynchronizationSourceIdentifier)));
            
            //Report Blocks
            foreach (ReportBlock block in Blocks) result.AddRange(block.ToBytes());

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(ReceiversReport report) { return report.ToPacket(); }

        public static implicit operator ReceiversReport(RtcpPacket packet) { return new ReceiversReport(packet); }

    }
}
