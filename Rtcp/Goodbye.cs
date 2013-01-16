using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class Goodbye
    {
        #region Properties

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public uint SynchronizationSourceIdentifier { get; set; }
        public string Reason { get; set; }

        #endregion

        #region Constructor

        public Goodbye(uint ssrc) { SynchronizationSourceIdentifier = ssrc; Reason = string.Empty; }

        public Goodbye(RtcpPacket packet) {
            Channel = packet.Channel;
            Created = packet.Created ?? DateTime.UtcNow;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.Goodbye) throw new Exception("Invalid Packet Type, Expected Goodbye. Found: '" + (byte)packet.PacketType + '\'');
        }

        public Goodbye(byte[] packet, int index)
        {
            SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
            if (packet.Length - index > 5) // We just got 0 - 3 and if we have 4 and 5 there is a length and reason
            {
                byte length = packet[index + 4];
                if (length > 0)
                {
                    Reason = Encoding.UTF8.GetString(packet, index + 5, length);
                }
            }
        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.Goodbye);
            output.Payload = ToBytes();
            output.BlockCount = 1;
            output.Channel = channel ?? Channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));

            if (!string.IsNullOrEmpty(Reason))
            {
                int length = Encoding.UTF8.GetByteCount(Reason);
                result.Add((byte)length);
                if (length > 0) result.AddRange(Encoding.UTF8.GetBytes(Reason));
            }           

            return result.ToArray();
        }

        #endregion

        public static implicit operator RtcpPacket(Goodbye goodbye) { return goodbye.ToPacket(goodbye.Channel); }

        public static implicit operator Goodbye(RtcpPacket packet) { return new Goodbye(packet); }

    }
}
