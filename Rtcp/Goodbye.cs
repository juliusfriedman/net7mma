using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class Goodbye
    {
        #region Properties

        public uint SynchronizationSourceIdentifier { get; set; }
        public string Reason { get; set; }

        #endregion

        #region Constructor

        public Goodbye(uint ssrc) { SynchronizationSourceIdentifier = ssrc; Reason = string.Empty; }

        public Goodbye(byte[] packet, int index)
        {
            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder((int)BitConverter.ToInt32(packet, index + 0));
            if (packet.Length - index > 4)
            {
                int length = packet[index + 4];
                if (length > 0)
                {
                    Reason = Encoding.UTF8.GetString(packet, index + 5, length);
                }
            }
        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket()
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.Goodbye);
            output.Data = ToBytes();
            output.BlockCount = 1;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SynchronizationSourceIdentifier)));

            if (!string.IsNullOrEmpty(Reason))
            {
                int lenth = Encoding.UTF8.GetByteCount(Reason);
                result.Add((byte)lenth);
                result.AddRange(Encoding.UTF8.GetBytes(Reason));
            }           

            return result.ToArray();
        }

        #endregion
    }
}
