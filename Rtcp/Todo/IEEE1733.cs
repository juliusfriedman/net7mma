using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{

    //http://en.wikipedia.org/wiki/Audio_Video_Bridging

    //http://tools.ietf.org/html/draft-williams-avtext-avbsync-01

    //http://tools.ietf.org/html/draft-williams-avtext-avbsync-02    

    //Todo write Williams since draft is expired
    //Could have been just an application specific packet with a length = 9, name = ravb, BlockCount = {0, 1, 2} for ProtocolIndication
    //Use ExtendedReports Framework if necessary.

    //Specialization of IEEE 1588 Best Master Clock Algorithm to 802.1AS 
    //http://www.ieee802.org/1/files/public/docs2006/as-garner-bmc-060606.pdf

    /*
     
     4.  IEEE 1733 / RTCP AVB Packet

   IEEE 1733 [6] defines the "AVB RTCP packet" type reproduced in
   Figure 2.  RTCP AVB packets contain a mapping between RTP timestamp
   and an 802.1AS timestamp as well as additional clock and QoS
   information.

      0                   1                   2                   3
      0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     |V=2|P|subtype=0|    PT=208     |           length=9            |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     |                           SSRC/CSRC                           |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     |                          name (ASCII)                         |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+R
     |      gmTimeBaseIndicator      |                               |T
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                               +C
     |                                                               |P
     +                      gmIdentity (80 bits)                     +
     |                                                               |A
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+V
     |                                                               |B
     +                       stream_id (64 bits)                     +
     |                                                               |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     |                          as_timestamp                         |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     |                         rtp_timestamp                         |
     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                Figure 2: IEEE 1733/RTCP AVB packet format

   A brief description of the major fields follows:

   gmIdentity  an 80 bit field uniquely identifying the current 802.1AS
      grand master clock used by the source to generate as_timestamps
      for this flow

   stream_id  a 64 bit number identifying the 802.1Qat [7] stream
      associated with this RTP flow

   as_timestamp  the 32 bit 802.1AS timestamp (Section 2) associated
      with the RTP timestamp carried in this packet

   rtp_timestamp  the RTP timestamp of a media packet

   Please consult the IEEE 1733 specification [6] for more details.
     
     */

    /// <summary>
    /// If an application uses the IETF Real-time Transport Protocol (RTP), 
    /// it can use a new RTCP payload format defined in IEEE 1733[8] that correlates the RTP timestamp with the 802.1AS presentation time. 
    /// The applications at the renderer(s) then use that correlation to translate the RTP timestamp to the presentation time stamp allowing 
    /// the renderer(s) to start playing at the same time and keep playing at the same rate.
    /// </summary>
    class AVBRtcpPacket /*: PT = 208, Length = 9, SubType = BlockCount*/
    {
        public enum ProtocolIndication
        {
            IEEE8021AS = 0,
            IEEE1588v1 = 1,
            IEEE1588v2 = 2
        }

        public ProtocolIndication Protocol { get { return (ProtocolIndication)SubType; } set { SubType = (byte)value; } }
        public byte SubType { get; set; }

        uint Ssrc;
        string Name; //ASCII
        ushort gmTimeBaseIndicator, gmPortNumber;
        uint gmClockIdentity, //EUI-64
            //After streamId
             AsTimestamp, RtpTimestamp;

        ulong StreamId;        
    }
}
