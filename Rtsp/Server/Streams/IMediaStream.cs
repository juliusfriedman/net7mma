using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    public interface IMediaStream
    {
        Guid Id { get; }

        SourceStream.StreamState State { get; }

        Sdp.SessionDescription SessionDescription { get; }

        void Start();

        void Stop();
    }
}
