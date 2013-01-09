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

        public DateTime? Sent { get; set; }

        public DateTime? Created { get; set; }

        public byte? Channel { get; set; }

        #region Constructor

        public ReceiversReport(uint ssrc) { SynchronizationSourceIdentifier = ssrc; }
        
        //Packet would have created property?
        public ReceiversReport(RtcpPacket packet) : this(packet.Data, 0) { if (packet.PacketType != RtcpPacket.RtcpPacketType.ReceiversReport) throw new Exception("Invalid Packet Type, Expected RecieversReport. Found: '" + (byte)packet.PacketType + '\''); Channel = packet.Channel; Created = packet.Created ?? DateTime.UtcNow; }

        public ReceiversReport(byte[] packet, int offset) 
        {
            SynchronizationSourceIdentifier = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, offset + 0));
            offset += 4; 
            if (packet.Length > 4)
            {
                while (offset < packet.Length)
                {
                    Blocks.Add(new ReportBlock(packet, ref offset));
                    //offset += ReportBlock.Size;
                }
            }
        }

        #endregion

        public virtual RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.ReceiversReport);
            output.Data = ToBytes();
            output.BlockCount = Blocks.Count;
            output.Channel = channel;
            return output;
        }

        public virtual byte[] ToBytes(uint? ssrc = null)
        {
            List<byte> result = new List<byte>();
            
            // SSRC
            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt((ssrc ?? SynchronizationSourceIdentifier))));
            
            //Report Blocks
            foreach (ReportBlock block in Blocks) result.AddRange(block.ToBytes());

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(ReceiversReport report) { return report.ToPacket(report.Channel); }

        public static implicit operator ReceiversReport(RtcpPacket packet) { return new ReceiversReport(packet); }

    }
}
