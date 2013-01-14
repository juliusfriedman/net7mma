using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Adds an abstract RtpClient To SourceStream,   
    /// This could also just be an interface
    /// This could also be a class which has the events for RtpPackets etc
    /// </summary>
    public abstract class RtpSource : SourceStream
    {

        public const string RtpMediaProtocol = "RTP/AVP";

        public RtpSource(string name, Uri source)
            : base(name, source)
        {
        }

        public bool DisableRtcp { get { return m_DisableSendStastics; } set { m_DisableSendStastics = value; } }

        public abstract Rtp.RtpClient RtpClient { get; }

        public override string MediaProtocol { get { return RtpMediaProtocol; } }

    }

    //public abstract class RtpChildStream
    //{
    //}

}
