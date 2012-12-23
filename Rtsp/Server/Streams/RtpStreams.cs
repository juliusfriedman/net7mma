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
    public abstract class RtpSourceStream : SourceStream
    {
        public RtpSourceStream(string name, Uri source)
            : base(name, source)
        {
        }

        public abstract Rtp.RtpClient RtpClient { get; }
    }

    //public abstract class RtpChildStream
    //{
    //}

}
