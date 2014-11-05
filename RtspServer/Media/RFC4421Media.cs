using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Media
{
    //Uncompressed Video 
    //http://tools.ietf.org/html/rfc4421
    class RFC4421Media
    {

        /*
         
         * 3.  Payload Format Parameters

   This memo defines six new colour sampling modes that MAY be signalled
   for use with [1].  The new modes are "RGB+", "RG+B", "R+GB", "BGR+",
   "BG+R", and "B+GR".  These sampling modes use the same packing order
   of samples as do the RGB and BGR colour sampling modes, respectively
   (Section 4.3 of [1]), except that an additional bit per sample of
   colour depth MUST be used for the component marked by the + symbol.
   The mandatory parameter "depth=N" indicates that N bits per sample
   are used by the unmarked components, but N+1 bits are used by the
   marked component.  All other features of the payload format are as
   defined in [1].

   The primary use of these colour sampling modes is to enable efficient
   packing of data into small pixel groups ("pgroups").  The most common
   use case is expected to be video with "depth=5", where the additional
   bit of colour depth for the marked component enables a single pixel
   to fit into two octets without padding.  The new colour sampling
   modes MAY be used for other depths, however, should that prove
   useful.

4.  Example

   A common uncompressed video format is RGB with 5 bits for the Red and
   Blue components and 6 bits for the Green component, for a total of 16
   bits per pixel.  Using the sampling modes defined in this memo, this
   can be signalled in Session Description Protocol (SDP) according to
   the following example:

       v=0
       o=jdoe 2890844526 2890842807 IN IP4 192.0.2.5
       s=-
       c=IN IP4 192.0.2.6
       t=2873397496 2873404696
       m=video 51372 RTP/AVP 99
       a=rtpmap:99 raw/90000
       a=fmtp:99 sampling=RG+B; width=1024; height=768; depth=5;
         colorimetry=SMPTE240M

   The last line has been wrapped due to formatting constraints of this
   memo, and forms one complete line in the SDP file.
         
         */
    }
}
