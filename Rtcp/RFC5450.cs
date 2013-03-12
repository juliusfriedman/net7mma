using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    //http://www.networksorcery.com/enp/rfc/rfc5450.txt    

    /*
     
     * Read from RtpPacket ExtensionData
     * 
     * The form of the transmission offset extension block is as follows:

       0                   1                   2                   3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |  ID   | len=2 |              transmission offset              |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   The length field takes the value 2 to indicate that 3 bytes follow.
     * 
     * 
     * 
     * 
     * 
     4.  Extended Jitter Reports

   The inter-arrival jitter computed as defined in Section 6.4.1 of RFC
   3550 provides inter-arrival jitter reports that include any source-
   introduced jitter (transmission time offsets).  If it is desired to



Singer & Desineni           Standards Track                     [Page 5]

RFC 5450                RTP Transmission Offsets              March 2009


   indicate the actual network jitter, excluding the source-introduced
   jitter, the new RTCP packet type defined here may be used.

   It has the following form:

        0                   1                   2                   3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   hdr |V=2|P|    RC   |   PT=IJ=195   |             length            |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |                      inter-arrival jitter                     |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       .                                                               .
       .                                                               .
       .                                                               .
       |                      inter-arrival jitter                     |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   If present, this RTCP packet must be placed after a receiver report
   (inside a compound RTCP packet), and MUST have the same value for RC
   (reception report count) as the receiver report.  The content is
   exactly that number of inter-arrival jitter calculations, calculated
   using the same formula as for sender and receiver reports, but taking
   into account the transmission offsets for the streams (if any).  That
   is, the formula uses the values T1=S1+O1, T2, etc., as defined above,
   instead of S1, S2, etc.  (If no transmission offset information is
   given for a stream, then the value of inter-arrival jitter in this
   packet and in the receiver report will be identical).

   Precisely, the replacement equation for the equation in the RTP
   specification is as follows, where Rj is the most recent arrival
   time:

   D(i,j) = (Rj - Ri) - ((Sj + Oj) - (Si + Oi))
          = (Rj - (Sj + Oj)) - (Ri - (Si + Oi))

     
     */

    public class ExtendedJitterReport
    {
        public List<uint> Jitters = new List<uint>();

        public DateTime? Sent { get; set; }

        public DateTime? Created { get; set; }

        public ExtendedJitterReport(RtcpPacket packet) : this(packet.Payload, 0) { }

        public ExtendedJitterReport(byte[] packet, int index)
        {
            while (index < packet.Length)
            {
                Jitters.Add(Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index)));
                index += 4;
            }
        }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpPacket(RtcpPacket.RtcpPacketType.ExtendedInterArrivalJitter, channel)
            {
                BlockCount = Jitters.Count,
                Payload = ToBytes()
            };
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            Jitters.ForEach(j => result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(j))));

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(ExtendedJitterReport jitter) { return jitter.ToPacket(); }

        public static implicit operator ExtendedJitterReport(RtcpPacket packet) { return new ExtendedJitterReport(packet); }

    }
}
