using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Adds an abstract RtpClient To SourceStream
    /// </summary>
    public abstract class RtpSourceStream : SourceStream
    {
        public RtpSourceStream(string name, Uri source)
            : base(name, source)
        {
        }
        public abstract Rtp.RtpClient RtpClient { get; }
    }
}
