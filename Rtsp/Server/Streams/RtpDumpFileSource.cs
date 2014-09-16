using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Provides the logic used to encapsulate playback of the rtpdump format.
    /// </summary>
    internal class RtpDumpFileSource : FileSourceStream
    {
        public RtpDumpFileSource(string name, Uri source, Guid? id)
            : base(name, source, id)
        {            

            //Read sdp from source directory
            SessionDescription = new Sdp.SessionDescription(System.IO.File.ReadAllText(source.AbsolutePath + "/SessionDescription.sdp"));
        }

        public override IEnumerable<FileSourceStream.TrackReference> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override Rtp.RtpFrame GetSample(TrackReference track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
