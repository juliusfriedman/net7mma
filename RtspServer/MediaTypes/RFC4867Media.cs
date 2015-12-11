using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc4867">RFC4867</see> which is used for Adaptive Multi-Rate (AMR) and Adaptive Multi-Rate Wideband (AMR-WB) Audio Codecs
    /// </summary>
    public class RFC4867Media : RFC2435Media //RtpSink
    {
        public class RFC4867Frame : Rtp.RtpFrame
        {
            public RFC4867Frame(byte payloadType) : base(payloadType) { }

            public RFC4867Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC4867Frame(RFC4867Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                /*
                 http://tools.ietf.org/html/rfc4867
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

        public RFC4867Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            clockRate = 16000;
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
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions.First().MediaFormat + " AMR/" + clockRate));
            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, sourceId));
        }
        
        #endregion
    }
}
