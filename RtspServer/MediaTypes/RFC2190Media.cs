using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc2190">RFC2190</see> which is used for H.263 Video Streams
    /// </summary>
    public class RFC2190Media : RFC2435Media //RtpSink
    {
        public class RFC2190Frame : Rtp.RtpFrame
        {
            public const byte RtpH323PayloadType = 34;

            public RFC2190Frame(byte payloadType) : base(payloadType) { }

            public RFC2190Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC2190Frame(RFC2190Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] data, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize()
            {
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

        public RFC2190Media(int width, int height, string name, string directory = null, bool watch = true)
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
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, RFC2190Frame.RtpH323PayloadType));

            //Add the control line
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            //Should be a field set in constructor.
            //=fmtp:xx CPCF=36,1000,0,1,1,0,0,2;CUSTOM=640,480,2;CIF=1;QCIF=1
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:" + SessionDescription.MediaDescriptions.First().MediaFormat + " H263/" + clockRate));

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, 0));
        }

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public override void Packetize(System.Drawing.Image image)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
