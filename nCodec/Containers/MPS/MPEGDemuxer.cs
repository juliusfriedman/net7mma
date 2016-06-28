using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Containers.MPS
{
    public interface MPEGDemuxer
    {
        List<MPEGDemuxerTrack> getTracks();
        List<MPEGDemuxerTrack> getVideoTracks();
        List<MPEGDemuxerTrack> getAudioTracks();

        void seekByte(long offset);
    }

    public interface MPEGDemuxerTrack
    {
        Packet nextFrame(MemoryStream buf);
        DemuxerTrackMeta getMeta();
        void ignore();
    }
}


