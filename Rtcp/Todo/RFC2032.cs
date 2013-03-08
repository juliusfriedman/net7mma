using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    class FullINTRAFrameRequest
    {
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

    class NegativeACKnowledgement
    {
    }
}
