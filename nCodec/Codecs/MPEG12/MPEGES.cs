using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.MPEG12
{
    public class MPEGES : SegmentReader
    {

        private int frameNo;
        public long curPts;

        public MPEGES(Stream channel)
            : this(channel, 4096)
        {

        }

        public MPEGES(Stream channel, int fetchSize)
            : base(channel, fetchSize)
        {

        }

        public nVideo.Containers.MPS.MPSDemuxer.MPEGPacket getFrame(MemoryStream buffer)
        {

            MemoryStream dup = buffer.duplicate();

            while (curMarker != 0x100 && curMarker != 0x1b3 && skipToMarker())
                ;

            while (curMarker != 0x100 && readToNextMarker(dup))
                ;

            readToNextMarker(dup);

            while (curMarker != 0x100 && curMarker != 0x1b3 && readToNextMarker(dup))
                ;

            dup.flip();

            return dup.hasRemaining() ? new nVideo.Containers.MPS.MPSDemuxer.MPEGPacket(dup, curPts, 90000, 0, frameNo++, true, null) : null;
        }
    }
}
