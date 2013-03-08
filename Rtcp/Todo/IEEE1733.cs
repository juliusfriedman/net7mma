using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{

    //http://en.wikipedia.org/wiki/Audio_Video_Bridging

    //http://tools.ietf.org/html/draft-williams-avtext-avbsync-02

    /// <summary>
    /// If an application uses the IETF Real-time Transport Protocol (RTP), 
    /// it can use a new RTCP payload format defined in IEEE 1733[8] that correlates the RTP timestamp with the 802.1AS presentation time. 
    /// The applications at the renderer(s) then use that correlation to translate the RTP timestamp to the presentation time stamp allowing 
    /// the renderer(s) to start playing at the same time and keep playing at the same rate.
    /// </summary>
    class AVBRtcpPacket : RtcpPacket
    {
        AVBRtcpPacket() : base((RtcpPacketType)208, null) { }
    }
}
