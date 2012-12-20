using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Adds an abstract RtpClient To SourceStream,
    /// Might not need a class for every type if the SourceStream has its own concept of packets etc. Or if it's event model was good enough    
    /// This could also just be an interface
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
