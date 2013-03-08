using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    //http://www.networksorcery.com/enp/rfc/rfc5450.txt

    /*
     
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

    class ExtendedJitterReport
    {
    }
}
