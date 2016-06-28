using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public interface DemuxerTrack
    {
        Packet nextFrame();

        DemuxerTrackMeta getMeta();
    }
}
