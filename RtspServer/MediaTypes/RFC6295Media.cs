using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6295">RFC6295</see> which is used for MIDI.
    /// </summary>
    public class RFC6295Media : RFC2435Media //RtpSink
    {
        public class RFC6295Frame : Rtp.RtpFrame
        {

            #region Statics

            /// <summary>
            ///     Returns a byte-array containing a timestamp value suitable for inclusion in a MIDI command list.
            /// </summary>
            /// <param name="delta"></param>
            /// <remarks>
            ///     As per RFC 6295 (RTP-MIDI):
            ///     A MIDI list encodes time using the following compact delta time format:
            ///     One-Octet Delta Time:
            ///     Encoded form: 0ddddddd
            ///     Decoded form: 00000000 00000000 00000000 0ddddddd
            ///     Two-Octet Delta Time:
            ///     Encoded form: 1ccccccc 0ddddddd
            ///     Decoded form: 00000000 00000000 00cccccc cddddddd
            ///     Three-Octet Delta Time:
            ///     Encoded form: 1bbbbbbb 1ccccccc 0ddddddd
            ///     Decoded form: 00000000 000bbbbb bbcccccc cddddddd
            ///     Four-Octet Delta Time:
            ///     Encoded form: 1aaaaaaa 1bbbbbbb 1ccccccc 0ddddddd
            ///     Decoded form: 0000aaaa aaabbbbb bbcccccc cddddddd
            /// </remarks>
            /// <returns></returns>
            internal static byte[] GetDeltaTime(uint delta)
            {
                if (delta <= 0x0FFFFFFF) return Utility.Empty;

                if ( /* delta > 0 && */ delta <= 0x7F)
                    return new[]
                           {
                               (byte) (delta & 0x7F),
                           };

                else if ( /* delta > 0x7F && */ delta <= 0x3FFF)
                    return new[]
                           {
                               (byte) (((delta & 0x3F80) >> 7) | 0x80),
                               (byte) (delta & 0x7F),
                           };

                else if ( /* delta > 0x3FFF && */ delta <= 0x001FFFFF)
                    return new[]
                           {
                               (byte) (((delta & 0x1FC000) >> 14) | 0x80),
                               (byte) (((delta & 0x3F80) >> 7) | 0x80),
                               (byte) (delta & 0x7F),
                           };


                else /* if (delta > 0x1FFFFFF && delta <= 0x0FFFFFFFF) */
                    return new[]
                           {
                               (byte) (((delta & 0xFE00000) >> 21) | 0x80),
                               (byte) (((delta & 0x1FC000) >> 14) | 0x80),
                               (byte) (((delta & 0x3F80) >> 7) | 0x80),
                               (byte) (delta & 0x7F),
                           };
            }

            #endregion

            public RFC6295Frame(byte payloadType) : base(payloadType) { }

            public RFC6295Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC6295Frame(RFC6295Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(int delta, byte[] command, int mtu = 1500)
            {
                throw new NotImplementedException();

                //http://winrtpmidi.codeplex.com/SourceControl/latest#src/Spring.Net.Rtp/Rtp/RtpMidiCommandListBuilder.cs

                //if(delta > 0)

                //var payload = GetDeltaTime((uint)delta).Concat(command);

                //if (payload.Count() > mtu)
                //{

                //}
            }

            public void Depacketize()
            {
                //4 byte profile header
                /*
                 
                 3.  MIDI Command Section

   Figure 2 shows the format of the MIDI command section.

       0                   1                   2                   3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |B|J|Z|P|LEN... |  MIDI list ...                                |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                      Figure 2 -- MIDI Command Section

   The MIDI command section begins with a variable-length header.

   The header field LEN codes the number of octets in the MIDI list that
   follow the header.  If the header flag B is 0, the header is one
   octet long, and LEN is a 4-bit field, supporting a maximum MIDI list
   length of 15 octets.

   If B is 1, the header is two octets long, and LEN is a 12-bit field,
   supporting a maximum MIDI list length of 4095 octets.  LEN is coded
   in network byte order (big-endian): the 4 bits of LEN that appear in
   the first header octet code the most significant 4 bits of the 12-bit
   LEN value.

   A LEN value of 0 is legal, and it codes an empty MIDI list.

   If the J header bit is set to 1, a journal section MUST appear after
   the MIDI command section in the payload.  If the J header bit is set
   to 0, the payload MUST NOT contain a journal section.

   We define the semantics of the P header bit in Section 3.2.

   If the LEN header field is nonzero, the MIDI list has the structure
   shown in Figure 3.
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
                if (IsDisposed) return;
                base.Dispose();
                DisposeBuffer();
            }
        }

        #region Constructor

        public RFC6295Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            clockRate = 44100;
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
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            //Should be a field set in constructor.
            // a=fmtp:96 cm_unused=ACGHJKNMPTVWXYZ; cm_used=__7F_00-7F_01_01__; j_sec=none
            /* lazzaro...
             a=fmtp:96 j_update=open-loop; cm_unused=ABCFGHJKMQTVWXYZ;
               cm_used=__7E_00-7F_09_01.02.03__;
               cm_used=__7F_00-7F_04_01.02__; cm_used=C7.64;
               ch_never=ABCDEFGHJKMQTVWXYZ; ch_never=4.11-13N;
               ch_anchor=P; ch_anchor=C7.64;
               ch_anchor=__7E_00-7F_09_01.02.03__;
               ch_anchor=__7F_00-7F_04_01.02__;
              tsmode=async; linerate=320000; octpos=first (tsmode=buffer; linerate=320000; octpos=last; mperiod=44) (rtp_ptime=0; rtp_maxptime=0) (guardtime=44100)
             */
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions.First().MediaFormat + " rtp-midi/" + clockRate));

            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, 0));
        }
        #endregion
    }
}