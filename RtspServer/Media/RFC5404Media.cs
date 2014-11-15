using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Media
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc5404">RFC5404</see> which is used for G.719 Audio.
    /// </summary>
    public class RFC5404Media : RFC2435Media //RtpSink
    {
        public class RFC5404Frame : Rtp.RtpFrame
        {
            public RFC5404Frame(byte payloadType) : base(payloadType) { }

            public RFC5404Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC5404Frame(RFC5404Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                /*
                          http://tools.ietf.org/html/rfc5404#page-4
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

        public RFC5404Media(int width, int height, string name, string directory = null, bool watch = true)
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
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions[0].MediaFormat + " G719"));
            //interleaving ... etc

            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));
        }
        
        #endregion
    }
}
