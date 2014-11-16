using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Media
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc4734">RFC4734</see> which is used for Modem, Fax, and Text Telephony Signals including but not limited to DTMF.
    /// </summary>
    public class RFC4734Media : RFC2435Media //RtpSink
    {
        public class RFC4734Frame : Rtp.RtpFrame
        {
            public RFC4734Frame(byte payloadType) : base(payloadType) { }

            public RFC4734Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC4734Frame(RFC4734Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                //4 byte profile header

                /*
                 
                 2.2.  Use of RTP Header Fields

                2.2.1.  Timestamp

                    The event duration described in Section 2.5 begins at the time given
                    by the RTP timestamp.  For events that span multiple RTP packets, the
                    RTP timestamp identifies the beginning of the event, i.e., several
                    RTP packets may carry the same timestamp.  For long-lasting events
                    that have to be split into segments (see below, Section 2.5.1.3), the
                    timestamp indicates the beginning of the segment.

                2.2.2.  Marker Bit

                    The RTP marker bit indicates the beginning of a new event.  For long-
                    lasting events that have to be split into segments (see below,
                    Section 2.5.1.3), only the first segment will have the marker bit
                    set.

                2.3.  Payload Format

                    The payload format for named telephone events is shown in Figure 1.

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |     event     |E|R| volume    |          duration             |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                    Figure 1: Payload Format for Named Events

                2.3.1.  Event Field

                    The event field is a number between 0 and 255 identifying a specific
                    telephony event.  An IANA registry of event codes for this field has
                    been established (see IANA Considerations, Section 7).  The initial
                    content of this registry consists of the events defined in Section 3.

                2.3.2.  E ("End") Bit

                    If set to a value of one, the "end" bit indicates that this packet
                    contains the end of the event.  For long-lasting events that have to
                    be split into segments (see below, Section 2.5.1.3), only the final
                    packet for the final segment will have the E bit set.

                2.3.3.  R Bit

                    This field is reserved for future use.  The sender MUST set it to
                    zero, and the receiver MUST ignore it.

                2.3.4.  Volume Field

                    For DTMF digits and other events representable as tones, this field
                    describes the power level of the tone, expressed in dBm0 after
                    dropping the sign.  Power levels range from 0 to -63 dBm0.  Thus,
                    larger values denote lower volume.  This value is defined only for
                    events for which the documentation indicates that volume is
                    applicable.  For other events, the sender MUST set volume to zero and
                    the receiver MUST ignore the value.

                2.3.5.  Duration Field

                    The duration field indicates the duration of the event or segment
                    being reported, in timestamp units, expressed as an unsigned integer
                    in network byte order.  For a non-zero value, the event or segment
                    began at the instant identified by the RTP timestamp and has so far
                    lasted as long as indicated by this parameter.  The event may or may
                    not have ended.  If the event duration exceeds the maximum
                    representable by the duration field, the event is split into several
                    contiguous segments as described below (Section 2.5.1.3).

                    The special duration value of zero is reserved to indicate that the
                    event lasts "forever", i.e., is a state and is considered to be
                    effective until updated.  A sender MUST NOT transmit a zero duration
                    for events other than those defined as states.  The receiver SHOULD
                    ignore an event report with zero duration if the event is not a
                    state.
                 */

                throw new NotImplementedException();
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
                if (Disposed) return;
                base.Dispose();
                DisposeBuffer();
            }
        }

        #region Constructor

        public RFC4734Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            clockRate = 80;
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
            //Should be a field set in constructor.
            //=fmtp:xx CPCF=36,1000,0,1,1,0,0,2;CUSTOM=640,480,2;CIF=1;QCIF=1
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions[0].MediaFormat + " telephone-event/" + clockRate));

            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));
        }

        #endregion
    }
}