using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtcp
{
    //http://www.networksorcery.com/enp/rfc/rfc2032.txt

    /*
     
     5.2.  H.261 control packets definition

5.2.1.  Full INTRA-frame Request (FIR) packet

     0                   1                   2                   3
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |V=2|P|   MBZ   |  PT=RTCP_FIR  |           length              |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                              SSRC                             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   This packet indicates that a receiver requires a full encoded image
   in order to either start decoding with an entire image or to refresh
   its image and speed the recovery after a burst of lost packets. The
   receiver requests the source to force the next image in full "INTRA-
   frame" coding mode, i.e. without using differential coding. The
   various fields are defined in the RTP specification [1]. SSRC is the
   synchronization source identifier for the sender of this packet. The
   value of the packet type (PT) identifier is the constant RTCP_FIR
   (192).
     
     */

    public class FullINTRAFrameRequest
    {

        public DateTime? Created { get; set; }

        public DateTime? Sent { get; set; }

        public byte? Channel { get; set; }

        public uint SynchronizationSourceIdentifier { get; set; }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.FullIntraFrameRequest);
            output.BlockCount = 0;
            output.Payload = ToBytes();
            output.Channel = channel ?? Channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add Ssrc
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));

            return result.ToArray();
        }

        public FullINTRAFrameRequest(byte[] packet, int offset) 
        {
            Created = DateTime.Now;

            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, offset));
        }

        public FullINTRAFrameRequest(uint ssrc) { SynchronizationSourceIdentifier = ssrc; Created = DateTime.Now; }

        public FullINTRAFrameRequest(RtcpPacket packet) : this(packet.Payload, 0) { if (packet.PacketType != RtcpPacket.RtcpPacketType.FullIntraFrameRequest) throw new Exception("Invalid Packet Type, Expected FullIntraFrameRequest. Found: '" + (byte)packet.PacketType + '\''); }

        public static implicit operator RtcpPacket(FullINTRAFrameRequest fir) { return fir.ToPacket(fir.Channel); }

        public static implicit operator FullINTRAFrameRequest(RtcpPacket packet) { return new FullINTRAFrameRequest(packet); }

    }

    /*
     
     5.2.2.  Negative ACKnowledgements (NACK) packet

   The format of the NACK packet is as follow:

     0                   1                   2                   3
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |V=2|P|   MBZ   | PT=RTCP_NACK  |           length              |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                              SSRC                             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |              FSN              |              BLP              |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+




Turletti & Huitema          Standards Track                     [Page 9]

RFC 2032           RTP Payload Format for H.261 Video       October 1996


   The various fields T, P, PT, length and SSRC are defined in the RTP
   specification [1]. The value of the packet type (PT) identifier is
   the constant RTCP_NACK (193). SSRC is the synchronization source
   identifier for the sender of this packet.

   The two remaining fields have the following meanings:

   First Sequence Number (FSN): 16 bits
     Identifies the first sequence number lost.

   Bitmask of following lost packets (BLP): 16 bits
     A bit is set to 1 if the corresponding packet has been lost,
     and set to 0 otherwise. BLP is set to 0 only if no packet
     other than that being NACKed (using the FSN field) has been
     lost. BLP is set to 0x00001 if the packet corresponding to
     the FSN and the following packet have been lost, etc.
     
     */

    public class NegativeACKnowledgement
    {
        public DateTime? Created { get; set; }

        public DateTime? Sent { get; set; }

        public byte? Channel { get; set; }

        public uint SynchronizationSourceIdentifier { get; set; }

        public ushort FirstSequenceNumber { get; set; }

        public ushort BitmapLostPackets { get; set; }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.NegativeACKnowledgement);
            output.BlockCount = 0;
            output.Payload = ToBytes();
            output.Channel = channel ?? Channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add Ssrc
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));

            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(FirstSequenceNumber)));

            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(BitmapLostPackets)));

            return result.ToArray();
        }

        public NegativeACKnowledgement(byte[] packet, int offset) 
        {
            Created = DateTime.Now;

            SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset));

            offset += 4;

            FirstSequenceNumber = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(packet, offset));

            offset += 2;

            BitmapLostPackets = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(packet, offset));
        }

        public NegativeACKnowledgement(uint ssrc, ushort firstSequenceNumber = 0, ushort bitmapLostPackets = 0) { SynchronizationSourceIdentifier = ssrc; FirstSequenceNumber = firstSequenceNumber; BitmapLostPackets = bitmapLostPackets; Created = DateTime.Now; }

        public NegativeACKnowledgement(RtcpPacket packet) : this(packet.Payload, 0) { if (packet.PacketType != RtcpPacket.RtcpPacketType.NegativeACKnowledgement) throw new Exception("Invalid Packet Type, Expected NegativeACKnowledgement. Found: '" + (byte)packet.PacketType + '\''); }

        public static implicit operator RtcpPacket(NegativeACKnowledgement nack) { return nack.ToPacket(nack.Channel); }

        public static implicit operator NegativeACKnowledgement(RtcpPacket packet) { return new NegativeACKnowledgement(packet); }

    }
}
