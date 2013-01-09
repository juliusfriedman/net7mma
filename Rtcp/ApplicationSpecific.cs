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
        public DateTime? Sent { get; set; }
        public uint SynchronizationSourceIdentifier { get; set; }
        public string Name { get; set; }
        byte[] Data { get; set; }
        #endregion

        #region Constructor

        public ApplicationSpecific(uint ssrc) { SynchronizationSourceIdentifier = ssrc;}

        public ApplicationSpecific(RtcpPacket packet) {
            Channel = packet.Channel;
            Created = packet.Created ?? DateTime.UtcNow;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.ApplicationSpecific) throw new Exception("Invalid Packet Type, Expected ApplicationSpecific. Found: '" + (byte)packet.PacketType + '\'');
        }

        public ApplicationSpecific(byte[] packet, int index)
        {
            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, index));
            Name = Encoding.ASCII.GetString(packet, index + 4, 4);
            int dataLen = packet.Length - (index + 8);
            /*
             * application-dependent data: variable length
             * Application-dependent data may or may not appear in an APP packet.
             * It is interpreted by the application and not RTP itself.  
             * It MUST be a multiple of 32 bits long. *
             */
            //if (dataLen % 4 != 0) { }
            if (dataLen > 0)
            {
                Data = new byte[dataLen];
                System.Array.Copy(packet, 8, Data, 0, dataLen);
            }
        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.ApplicationSpecific);
            output.Data = ToBytes();
            output.BlockCount = Data != null ? 1 : 0; //Maybe should always be 0 or maybe it doesn't really matter
            output.Channel = channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SynchronizationSourceIdentifier)));
            result.AddRange(Encoding.ASCII.GetBytes(Name));
            if (Data != null) result.AddRange(Data);
            while (result.Count % 4 != 0) result.Add(0);
            return result.ToArray();
        }

        #endregion

        public static implicit operator RtcpPacket(ApplicationSpecific appSpecific) { return appSpecific.ToPacket(appSpecific.Channel); }

        public static implicit operator ApplicationSpecific(RtcpPacket packet) { return new ApplicationSpecific(packet); }

    }
}
