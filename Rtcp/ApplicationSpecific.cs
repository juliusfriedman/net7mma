using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public sealed class ApplicationSpecific : RtcpPacket
    {
        #region Properties

        public uint SynchronizationSourceIdentifier { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 0); } }
        public string Name { get { return Encoding.ASCII.GetString(Payload, 4, 4); } set { if (value.Length > 4) throw new InvalidOperationException("Name can be only 4 characters long!"); Encoding.ASCII.GetBytes(value).CopyTo(Payload, 4); } }
        byte[] Data
        {
            get { return Payload.Skip(8).ToArray(); }
            set
            {
                List<byte> temp = new List<byte>();
                temp.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));
                temp.AddRange(Encoding.ASCII.GetBytes(Name));
                temp.AddRange(value);
                Payload = temp.ToArray();
            }
        }
        #endregion

        #region Constructor

        public ApplicationSpecific(byte[] packet, int index) : base(packet, 0, RtcpPacketType.ApplicationSpecific) { }

        public ApplicationSpecific(uint ssrc, byte? channel = null) :base(RtcpPacket.RtcpPacketType.ApplicationSpecific, channel) { SynchronizationSourceIdentifier = ssrc;}

        public ApplicationSpecific(RtcpPacket packet) : base(packet)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.ApplicationSpecific) throw new Exception("Invalid Packet Type, Expected ApplicationSpecific. Found: '" + (byte)packet.PacketType + '\'');
        }

        #endregion
    }
}
