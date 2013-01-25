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

        Sdp.SessionDescription m_Sdp = new Sdp.SessionDescription(1);

        public RtpSource(string name, Uri source)
            : base(name, source)
        {
        }

        public bool DisableRtcp { get { return m_DisableQOS; } set { m_DisableQOS = value; } }

        public abstract Rtp.RtpClient RtpClient { get; }

        public virtual Sdp.SessionDescription SessionDescription { get { return m_Sdp; } protected set { m_Sdp = value; } }
    }

    //public abstract class RtpChildStream
    //{
    //}

}
