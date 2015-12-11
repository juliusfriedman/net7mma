using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc3952">RFC3952</see> which is used for iLBC Audio
    /// </summary>
    public class RFC3952Media : RFC2435Media //RtpSink
    {
        public class RFC3952Frame : Rtp.RtpFrame
        {
            public RFC3952Frame(byte payloadType) : base(payloadType) { }

            public RFC3952Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC3952Frame(RFC3952Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
                this.Buffer = new System.IO.MemoryStream(this.Assemble().ToArray());
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

        public RFC3952Media(int width, int height, string name, string directory = null, bool watch = true)
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
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            //Should be a field set in constructor.
            //sampling=RG+B; depth=5; colorimetry=SMPTE240M
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions.First().MediaFormat + " iLBC/" + clockRate));
            //should allow fmtp:XX mode=YY

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, sourceId));
        }
        
        #endregion
    }
}
