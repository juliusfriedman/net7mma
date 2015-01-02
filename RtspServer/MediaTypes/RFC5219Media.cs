/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */

//http://tools.ietf.org/html/rfc6184

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Media.Rtsp.Server.MediaTypes
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc5219">RFC5219</see> which is used for MPEG-1 or 2, layer III audio ("MP3") encoded audio.
    /// </summary>
    public class RFC5219Media : RFC2435Media //RtpSink
    {
        public class RFC5219Frame : Rtp.RtpFrame
        {
            public RFC5219Frame(byte payloadType) : base(payloadType) { }

            public RFC5219Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC5219Frame(RFC5219Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] accessUnit, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                /*
                 RFC 5219                                                   February 2008


                   An ADU descriptor consists of the following fields:

                   -  "C": Continuation flag (1 bit):  1, if the data following the ADU
                           descriptor is a continuation of an ADU frame that was too
                           large to fit within a single RTP packet; 0 otherwise.

                   -  "T": Descriptor Type flag (1 bit):
                           0 if this is a 1-byte ADU descriptor;
                           1 if this is a 2-byte ADU descriptor.

                   -  "ADU size" (6 or 14 bits):  The size (in bytes) of the ADU frame
                           that will follow this ADU descriptor (i.e., NOT including the
                           size of the descriptor itself).  A 2-byte ADU descriptor
                           (with a 14-bit "ADU size" field) is used for ADU frame sizes
                           of 64 bytes or more.  For smaller ADU frame sizes, senders
                           MAY alternatively use a 1-byte ADU descriptor (with a 6-bit
                           "ADU size" field).  Receivers MUST be able to accept an ADU
                           descriptor of either size.

                   Thus, a 1-byte ADU descriptor is formatted as follows:

                          0 1 2 3 4 5 6 7
                         +-+-+-+-+-+-+-+-+
                         |C|0|  ADU size |
                         +-+-+-+-+-+-+-+-+

                   and a 2-byte ADU descriptor is formatted as follows:

                          0                   1
                          0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
                         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                         |C|1|     ADU size (14 bits)    |
                         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                4.3.  Packing Rules

                   Each RTP packet payload begins with an "ADU descriptor", followed by
                   "ADU frame" data.  Normally, this "ADU descriptor" + "ADU frame" will
                   fit completely within the RTP packet.  In this case, more than one
                   successive "ADU descriptor" + "ADU frame" MAY be packed into a single
                   RTP packet, provided that they all fit completely.

                   If, however, a single "ADU descriptor" + "ADU frame" is too large to
                   fit within an RTP packet, then the "ADU frame" is split across two or
                   more successive RTP packets.  Each such packet begins with an ADU
                   descriptor.  The first packet's descriptor has a "C" (continuation)
                   flag of 0; the following packets' descriptors each have a "C" flag of
                   1.  Each descriptor, in this case, has the same "ADU size" value: the
                   size of the entire "ADU frame" (not just the portion that will fit
                   within a single RTP packet).  Each such packet (even the last one)
                   contains only one "ADU descriptor".

                4.4.  RTP Header Fields

                   Payload Type: The (static) payload type 14 that was defined for
                      MPEG audio [6] MUST NOT be used.  Instead, a different, dynamic
                      payload type MUST be used -- i.e., one within the range [96..127].

                   M bit: This payload format defines no use for this bit.  Senders
                      SHOULD set this bit to zero in each outgoing packet.

                   Timestamp: This is a 32-bit, 90 kHz timestamp, representing the
                      presentation time of the first ADU packed within the packet.

                4.5.  Handling Received Data

                   Note that no information is lost by converting a sequence of MP3
                   frames to a corresponding sequence of "ADU frames", so a receiving
                   RTP implementation can either feed the ADU frames directly to an
                   appropriately modified MP3 decoder, or convert them back into a
                   sequence of MP3 frames, as described in Appendix A.2 below.

                5.  Handling Multiple MPEG Audio Layers

                   The RTP payload format described here is intended only for MPEG-1 or
                   2, layer III audio ("MP3").  In contrast, layer I and layer II frames
                   are self-contained, without a back-pointer to earlier frames.
                   However, it is possible (although unusual) for a sequence of audio
                   frames to consist of a mixture of layer III frames, and layer I or II
                   frames.  When such a sequence is transmitted, only layer III frames
                   are converted to ADUs; layer I or II frames are sent 'as is' (except
                   for the prepending of an "ADU descriptor").  Similarly, the receiver
                   of a sequence of frames -- using this payload format -- leaves layer
                   I and II frames untouched (after removing the prepended "ADU
                   descriptor"), but converts layer III frames from "ADU frames" to
                   regular MP3 frames.  (Recall that each frame's layer is identified
                   from its 4-byte MPEG header.)

                   If you are transmitting a stream consisting *only* of layer I or
                   layer II frames (i.e., without any MP3 data), then there is no
                   benefit to using this payload format, *unless* you are using the
                   interleaving mechanism described in Section 7 below.

                6.  Frame Packetizing and Depacketizing

                   The transmission of a sequence of MP3 frames takes the following
                   steps:

                         MP3 frames
                                 -1-> ADU frames
                                     -2-> interleaved ADU frames
                                           -3-> RTP packets

                   Step 1 is the conversion of a sequence of MP3 frames to a
                   corresponding sequence of ADU frames, and takes place as described in
                   Sections 3 and 4.1 above.  (Note also the pseudo-code in Appendix
                   A.1.)

                   Step 2 is the reordering of the sequence of ADU frames in an
                   (optional) interleaving pattern, prior to packetization, as described
                   in section 7 below.  (Note also the pseudo-code in Appendix B.1.)
                   Interleaving helps reduce the effect of packet loss by distributing
                   consecutive ADU frames over non-consecutive packets.  (Note that
                   because of the back-pointer in MP3 frames, interleaving can be
                   applied -- in general -- only to ADU frames.  Thus, interleaving was
                   not possible for RFC 2250.)

                   Step 3 is the packetizing of a sequence of (interleaved) ADU frames
                   into RTP packets -- as described in section 4.3 above.  Each packet's
                   RTP timestamp is the presentation time of the first ADU that is
                   packed within it.  Note that if interleaving was done in step 2, the
                   RTP timestamps on outgoing packets will not necessarily be
                   monotonically nondecreasing.

                   Similarly, a sequence of received RTP packets is handled as follows:

                         RTP packets
                               -4-> RTP packets ordered by RTP sequence number
                                     -5-> interleaved ADU frames
                                           -6-> ADU frames
                                                 -7-> MP3 frames

                   Step 4 is the usual sorting of incoming RTP packets using the RTP
                   sequence number.

                   Step 5 is the depacketizing of ADU frames from RTP packets -- i.e.,
                   the reverse of step 3.  As part of this process, a receiver uses the
                   "C" (continuation) flag in the ADU descriptor to notice when an ADU
                   frame is split over more than one packet (and to discard the ADU
                   frame entirely if one of these packets is lost).

                   Step 6 is the rearranging of the sequence of ADU frames back to its
                   original order (except for ADU frames missing due to packet loss), as
                   described in Section 7 below.  (Note also the pseudo-code in Appendix
                   B.2.)

                   Step 7 is the conversion of the sequence of ADU frames into a
                   corresponding sequence of MP3 frames -- i.e., the reverse of step 1.
                   (Note also the pseudo-code in Appendix A.2.)  With an appropriately
                   modified MP3 decoder, an implementation may omit this step; instead,
                   it could feed ADU frames directly to the (modified) MP3 decoder.

                7.  ADU Frame Interleaving

                   In MPEG audio frames (MPEG-1 or 2; all layers), the high-order 11
                   bits of the 4-byte MPEG header ('syncword') are always all-one (i.e.,
                   0xFFE).  When reordering a sequence of ADU frames for transmission,
                   we reuse these 11 bits as an "Interleaving Sequence Number" (ISN).
                   (Upon reception, they are replaced with 0xFFE once again.)

                   The structure of the ISN is (a,b), where:

                         - a == bits 0-7:      8-bit Interleave Index (within Cycle)
                         - b == bits 8-10:     3-bit Interleave Cycle Count

                   That is, the 4-byte MPEG header is reused as follows:

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |Interleave Idx |CycCt|   The rest of the original MPEG header  |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                   Example: Consider the following interleave cycle (of size 8):

                            1,3,5,7,0,2,4,6

                   (This particular pattern has the property that any loss of up to four
                   consecutive ADUs in the interleaved stream will lead to a
                   deinterleaved stream with no gaps greater than one.)  This produces
                   the following sequence of ISNs:

                   (1,0) (3,0) (5,0) (7,0) (0,0) (2,0) (4,0) (6,0) (1,1) (3,1) (5,1)
                   etc.

                   So, in this example, a sequence of ADU frames

                   f0 f1 f2 f3 f4 f5 f6 f7 f8 f9 (etc.)

                   would get reordered, in step 2, into:

                   (1,0)f1 (3,0)f3 (5,0)f5 (7,0)f7 (0,0)f0 (2,0)f2 (4,0)f4 (6,0)f6
                   (1,1)f9 (3,1)f11 (5,1)f13 (etc.)

                   and the reverse reordering (along with replacement of the 0xFFE)
                   would occur upon reception.

                   The reason for breaking the ISN into "Interleave Cycle Count" and
                   "Interleave Index" (rather than just treating it as a single 11-bit
                   counter) is to give receivers a way of knowing when an ADU frame
                   should be 'released' to the ADU->MP3 conversion process (step 7
                   above), rather than waiting for more interleaved ADU frames to
                   arrive.  For instance, in the example above, when the receiver sees a
                   frame with ISN (<something>,1), it knows that it can release all
                   previously seen frames with ISN (<something>,0), even if some other
                   (<something>,0) frames remain missing due to packet loss.  An 8-bit
                   Interleave Index allows interleave cycles of size up to 256.

                   The choice of an interleaving order can be made independently of RTP
                   packetization.  Thus, a simple implementation could choose an
                   interleaving order first, reorder the ADU frames accordingly (step
                   2), then simply pack them sequentially into RTP packets (step 3).
                   However, the size of ADU frames -- and thus the number of ADU frames
                   that will fit in each RTP packet -- will typically vary in size, so a
                   more optimal implementation would combine steps 2 and 3, by choosing
                   an interleaving order that better reflected the number of ADU frames
                   packed within each RTP packet.

                   Each receiving implementation of this payload format MUST recognize
                   the ISN and be able to perform deinterleaving of incoming ADU frames
                   (step 6).  However, a sending implementation of this payload format
                   MAY choose not to perform interleaving -- i.e., by omitting step 2.
                   In this case, the high-order 11 bits in each 4-byte MPEG header would
                   remain at 0xFFE.  Receiving implementations would thus see a sequence
                   of identical ISNs (all 0xFFE).  They would handle this in the same
                   way as if the Interleave Cycle Count changed with each ADU frame, by
                   simply releasing the sequence of incoming ADU frames sequentially to
                   the ADU->MP3 conversion process (step 7), without reordering.  (Note
                   also the pseudo-code in Appendix B.2.)
                 */
            }

            internal void DisposeBuffer()
            {
                if (Buffer != null)
                {
                    Buffer.Dispose();
                    Buffer = null;
                }
            }

            public override void Dispose()
            {
                if (IsDisposed) return;
                base.Dispose();
                DisposeBuffer();
            }
        }

        #region Constructor

        public RFC5219Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            clockRate = 90;
        }

        #endregion

        #region Methods

        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove JPEG Track
            SessionDescription.RemoveMediaDescription(0);
            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the given payload type         
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.audio, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 96));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 mpa-robust/" + clockRate));
            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));
        }

        #endregion
    }
}