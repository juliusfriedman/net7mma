using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class ApplicationSpecific
    {
        #region Properties

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public uint SynchronizationSourceIdentifier { get; set; }

        #endregion

        #region Constructor

        public ApplicationSpecific(uint ssrc) { SynchronizationSourceIdentifier = ssrc;}

        public ApplicationSpecific(RtcpPacket packet) {
            Channel = packet.Channel;
            Created = packet.Created ?? DateTime.UtcNow;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.ApplicationSpecific) throw new Exception("Invalid Packet Type");
        }

        public ApplicationSpecific(byte[] packet, int index)
        {
            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, index));
            //TODO
        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.Goodbye);
            output.Data = ToBytes();
            output.BlockCount = 1;
            output.Channel = channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SynchronizationSourceIdentifier)));

            //TODO

            return result.ToArray();
        }

        #endregion

        public static implicit operator RtcpPacket(ApplicationSpecific appSpecific) { return appSpecific.ToPacket(appSpecific.Channel); }

        public static implicit operator ApplicationSpecific(RtcpPacket packet) { return new ApplicationSpecific(packet); }

    }
}
