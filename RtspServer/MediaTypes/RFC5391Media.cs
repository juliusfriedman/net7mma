using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc5391">RFC5391</see> which is used for ITU-T Recommendation G.711.1
    /// </summary>
    public class RFC5391Media : RFC2435Media //RtpSink
    {
        public class RFC5391Frame : Rtp.RtpFrame
        {
            public RFC5391Frame(byte payloadType) : base(payloadType) { }

            public RFC5391Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC5391Frame(RFC5391Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                /*
                             2.  Background

               G.711.1 is a G.711 embedded wideband speech and audio coding
               algorithm operating at 64, 80, and 96 kbps.  At 64 kbps, G.711.1 is
               fully interoperable with G.711.  Hence, an efficient deployment in
               existing G.711-based Voice over IP (VoIP) infrastructures is
               foreseen.

               The codec operates on 5-ms frames, and the default sampling rate is
               16 kHz.  Input and output at 8 kHz are also supported for narrowband
               modes.






            Sollaud                     Standards Track                     [Page 2]
 
            RFC 5391             RTP Payload Format for G.711.1        November 2008


               The encoder produces an embedded bitstream structured in three layers
               corresponding to three available bit rates: 64, 80, and 96 kbps.  The
               bitstream can be truncated at the decoder side or by any component of
               the communication system to adjust, "on the fly", the bit rate to the
               desired value.

               The following table gives more details on these layers.

                           +-------+------------------------+----------+
                           | Layer | Description            | Bit rate |
                           +-------+------------------------+----------+
                           | L0    | G.711 compatible       | 64 kbps  |
                           | L1    | narrowband enhancement | 16 kbps  |
                           | L2    | wideband enhancement   | 16 kbps  |
                           +-------+------------------------+----------+

                                    Table 1: Layers description

               The combinations of these three layers results in the definition of
               four modes, as per the following table.

                          +------+----+----+----+------------+----------+
                          | Mode | L0 | L1 | L2 | Audio band | Bit rate |
                          +------+----+----+----+------------+----------+
                          | R1   | x  |    |    | narrowband | 64 kbps  |
                          | R2a  | x  | x  |    | narrowband | 80 kbps  |
                          | R2b  | x  |    | x  | wideband   | 80 kbps  |
                          | R3   | x  | x  | x  | wideband   | 96 kbps  |
                          +------+----+----+----+------------+----------+

                                    Table 2: Modes description

            3.  RTP Header Usage

               The format of the RTP header is specified in [RFC3550].  The payload
               format defined in this document uses the fields of the header in a
               manner consistent with that specification.

               marker (M):
                  G.711.1 does not define anything specific regarding Discontinuous
                  Transmission (DTX), a.k.a. silence suppression.  Codec-independent
                  mechanisms may be used, like the generic comfort-noise payload
                  format defined in [RFC3389].

                  For applications that send either no packets or occasional
                  comfort-noise packets during silence, the first packet of a
                  talkspurt -- that is, the first packet after a silence period
                  during which packets have not been transmitted contiguously --



            Sollaud                     Standards Track                     [Page 3]
 
            RFC 5391             RTP Payload Format for G.711.1        November 2008


                  SHOULD be distinguished by setting the marker bit in the RTP data
                  header to one.  The marker bit in all other packets is zero.  The
                  beginning of a talkspurt MAY be used to adjust the playout delay
                  to reflect changing network delays.  Applications without silence
                  suppression MUST set the marker bit to zero.

               payload type (PT):
                  The assignment of an RTP payload type for this packet format is
                  outside the scope of this document, and will not be specified
                  here.  It is expected that the RTP profile under which this
                  payload format is being used will assign a payload type for this
                  codec or specify that the payload type is to be bound dynamically
                  (see Section 5.3).

               timestamp:
                  The RTP timestamp clock frequency is the same as the default
                  sampling frequency: 16 kHz.

                  G.711.1 has also the capability to operate with 8-kHz sampled
                  input/output signals.  It does not affect the bitstream, and the
                  decoder does not require a priori knowledge about the sampling
                  rate of the original signal at the input of the encoder.
                  Therefore, depending on the implementation and the audio acoustic
                  capabilities of the devices, the input of the encoder and/or the
                  output of the decoder can be configured at 8 kHz; however, a
                  16-kHz RTP clock rate MUST always be used.

                  The duration of one frame is 5 ms, corresponding to 80 samples at
                  16 kHz.  Thus, the timestamp is increased by 80 for each
                  consecutive frame.

            4.  Payload Format

               The complete payload consists of a payload header of 1 octet,
               followed by one or more consecutive G.711.1 audio frames of the same
               mode.

               The mode may change between packets, but not within a packet.

            4.1.  Payload Header

               The payload header is illustrated below.

                  0 1 2 3 4 5 6 7
                 +-+-+-+-+-+-+-+-+
                 |0 0 0 0 0|  MI |
                 +-+-+-+-+-+-+-+-+




            Sollaud                     Standards Track                     [Page 4]
 
            RFC 5391             RTP Payload Format for G.711.1        November 2008


               The five most significant bits are reserved for further extension and
               MUST be set to zero and MUST be ignored by receivers.

               The Mode Index (MI) field (3 bits) gives the mode of the following
               frame(s) as per the table:

                            +------------+--------------+------------+
                            | Mode Index | G.711.1 mode | Frame size |
                            +------------+--------------+------------+
                            |      1     |      R1      |  40 octets |
                            |      2     |      R2a     |  50 octets |
                            |      3     |      R2b     |  50 octets |
                            |      4     |      R3      |  60 octets |
                            +------------+--------------+------------+

                                 Table 3: Modes in payload header

               All other values of MI are reserved for future use and MUST NOT be
               used.

               Payloads received with an undefined MI value MUST be discarded.

               If a restricted mode-set has been set up by the signaling (see
               Section 5), payloads received with an MI value not in this set MUST
               be discarded.

            4.2.  Audio Data

               After this payload header, the consecutive audio frames are packed in
               order of time, that is, oldest first.  All frames MUST be of the same
               mode, indicated by the MI field of the payload header.

               Within a frame, layers are always packed in the same order: L0 then
               L1 for mode R2a, L0 then L2 for mode R2b, L0 then L1 then L2 for mode
               R3.  This is illustrated below.
















            Sollaud                     Standards Track                     [Page 5]
 
            RFC 5391             RTP Payload Format for G.711.1        November 2008


                     +-------------------------------+
                 R1  |              L0               |
                     +-------------------------------+

                     +-------------------------------+--------+
                 R2a |              L0               |   L1   |
                     +-------------------------------+--------+

                     +-------------------------------+--------+
                 R2b |              L0               |   L2   |
                     +-------------------------------+--------+

                     +-------------------------------+--------+--------+
                 R3  |              L0               |   L1   |   L2   |
                     +-------------------------------+--------+--------+

               The size of one frame is given by the mode, as per Table 3, and the
               actual number of frames is easy to infer from the size of the audio
               data part:

                  nb_frames = (size_of_audio_data) / (size_of_one_frame).

               Only full frames must be considered.  So if there is a remainder to
               the division above, the corresponding remaining bytes in the received
               payload MUST be ignored.
                 */

                //Assemble all data to the buffer

                Buffer = new System.IO.MemoryStream(Assemble().ToArray());
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

        public RFC5391Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            ClockRate = 16000;
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
            //A/U law and WideBand should be a field set in constructor.
            //mode-set=4,3
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions.First().MediaFormat + " PCMA/" + ClockRate));
            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, sourceId));
        }
        
        #endregion
    }
}
