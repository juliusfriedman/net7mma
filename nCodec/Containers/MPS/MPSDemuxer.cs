using nVideo.Codecs.MPEG12;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Containers.MPS
{
    public class MPSDemuxer : SegmentReader, MPEGDemuxer {
    private static int BUFFER_SIZE = 0x100000;

    private Dictionary<int, MPEGDemuxerTrack> streams = new Dictionary<int, MPEGDemuxerTrack>();
    private FileStream channel;

    public MPSDemuxer(FileStream channel)  : base(channel){
        
        this.channel = channel;
        findStreams();
    }

    protected void findStreams()  {
        for (int i = 0; i == 0 || i < 5 * streams.Count() && streams.Count() < 2; i++) {
            PESPacket nextPackets = nextPacket(getBuffer());
            if (nextPackets == null)
                break;
            addToStream(nextPackets);
        }
    }

    public class PESPacket {
        public MemoryStream data;
        public long pts;
        public int streamId;
        public int length;
        public long pos;
        public long dts;

        public PESPacket(MemoryStream data, long pts, int streamId, int length, long pos, long dts) {
            this.data = data;
            this.pts = pts;
            this.streamId = streamId;
            this.length = length;
            this.pos = pos;
            this.dts = dts;
        }
    }

    private List<MemoryStream> bufPool = new List<MemoryStream>();

    public MemoryStream getBuffer() {
        lock (bufPool) {
            if (bufPool.Count() > 0) {
                MemoryStream r = bufPool [0];
                bufPool.RemoveAt(0);
                return r;
            }
        }
        return new MemoryStream(BUFFER_SIZE);
    }

    public void putBack(MemoryStream buffer) {
        buffer.clear();
        lock (bufPool) {
            bufPool.Add(buffer);
        }
    }

    public abstract class BaseTrack : MPEGDemuxerTrack {
        protected internal int streamId;
        protected internal List<PESPacket> m_pending = new List<PESPacket>();

        public BaseTrack(int streamId, PESPacket pkt)  {
            this.streamId = streamId;
            this.m_pending.Add(pkt);
        }

        public int getSid() {
            return streamId;
        }

        public void pending(PESPacket pkt) {
            if (m_pending != null)
                m_pending.Add(pkt);
            //else
                //putBack(pkt.data);
        }

        public List<PESPacket> getPending() {
            return m_pending;
        }

        public void ignore() {
            if (m_pending == null)
                return;
           // foreach (PESPacket pesPacket in m_pending) {
                //putBack(pesPacket.data);
            //}
            m_pending.Clear();
            //m_pending = null;
        }

        public virtual Packet nextFrame(MemoryStream buf)
        {
            throw new NotImplementedException();
        }

        public virtual DemuxerTrackMeta getMeta()
        {
            throw new NotImplementedException();
        }
    }

    public class MPEGTrack : BaseTrack /*, ReadableByteChannel*/ {

        private MPEGES es;

        internal MPSDemuxer demux;

        public MPEGTrack(int streamId, PESPacket pkt) : base(streamId, pkt){
            //FileStream?
            this.es = new MPEGES(es.channel);
        }

        public bool isOpen() {
            return true;
        }

        public MPEGES getES() {
            return es;
        }

        public void close(){
        }

        public int read(MemoryStream arg0) {
            PESPacket pes = null;

            if (m_pending.Count() > 0) m_pending.RemoveAt(0);
            else pes = getPacket();

            if (pes == null || !pes.data.hasRemaining())
                return -1;
            int toRead = Math.Min(arg0.remaining(), pes.data.remaining());
            arg0.put(StreamExtensions.read(pes.data, toRead));

            if (pes.data.hasRemaining())
                m_pending.Insert(0, pes);
            else
                demux.putBack(pes.data);

            return toRead;
        }

        private PESPacket getPacket() {
            if (m_pending.Count() > 0)
            {
                PESPacket r = m_pending[0];
                m_pending.RemoveAt(0);
                return r;
            }
            PESPacket pkt;
            while ((pkt = demux.nextPacket(demux.getBuffer())) != null)
            {
                if (pkt.streamId == streamId) {
                    if (pkt.pts != -1) {
                        es.curPts = pkt.pts;
                    }
                    return pkt;
                } else
                    demux.addToStream(pkt);
            }
            return null;
        }

        public override Packet nextFrame(MemoryStream buf) {
            return es.getFrame(buf);
        }

        public override DemuxerTrackMeta getMeta() {
            return new DemuxerTrackMeta(MPSUtils.videoStream(streamId) ? DemuxerTrackMeta.Type.VIDEO : (MPSUtils.audioStream(streamId) ? DemuxerTrackMeta.Type.AUDIO : DemuxerTrackMeta.Type.OTHER), null,
                    0, 0, null);
        }
    }

    public class PlainTrack : BaseTrack {
        private int frameNo;

        internal MPSDemuxer demux;

        public PlainTrack(int streamId, PESPacket pkt) : base(streamId, pkt) {
            
        }

        public bool isOpen() {
            return true;
        }

        public void close(){
        }

        public override Packet nextFrame(MemoryStream buf) {
            PESPacket pkt;
            if (m_pending.Count() > 0) {
                pkt = m_pending[0];
                m_pending.RemoveAt(0);
            } else {
                while ((pkt = demux.nextPacket(demux.getBuffer())) != null && pkt.streamId != streamId)
                   demux.addToStream(pkt);
            }
            return pkt == null ? null : new Packet(pkt.data, pkt.pts, 90000, 0, frameNo++, true, null);
        }

        public override DemuxerTrackMeta getMeta() {
            return new DemuxerTrackMeta(MPSUtils.videoStream(streamId) ? DemuxerTrackMeta.Type.VIDEO : (MPSUtils.audioStream(streamId) ? DemuxerTrackMeta.Type.AUDIO : DemuxerTrackMeta.Type.OTHER), null,
                    0, 0, null);
        }
    }

    public void seekByte(long offset) {
        channel.Position = (offset);
        reset();
    }

    public void reset() {
        foreach (BaseTrack track in streams.Values)
        {
            track.m_pending.Clear();
        }
    }

    private void addToStream(PESPacket pkt) {
        BaseTrack pes = (BaseTrack)streams[pkt.streamId];
        if (pes == null) {
            if (isMPEG(pkt.data))
            {
                pes = new MPEGTrack(pkt.streamId, pkt)
                {
                    demux = this
                };
            
            }
            else
                pes = new PlainTrack(pkt.streamId, pkt);
            streams.Add(pkt.streamId, pes);
        } else {
            pes.pending(pkt);
        }
    }

    public PESPacket nextPacket(MemoryStream outb) {
        MemoryStream dup = outb.duplicate();

        while (!MPSUtils.psMarker(curMarker))
        {
            if (!skipToMarker())
                return null;
        }

        MemoryStream fork = dup.duplicate();
        readToNextMarker(dup);
        PESPacket pkt = MPSUtils.readPESHeader(fork, curPos());
        if (pkt.length == 0) {
            while (!MPSUtils.psMarker(curMarker) && readToNextMarker(dup))
                ;
        } else {
            read(dup, pkt.length - dup.position() + 6);
        }
        fork.limit(dup.position());
        pkt.data = fork;
        return pkt;
    }

    

    public List<MPEGDemuxerTrack> getTracks() {
        return streams.Values.ToList();
    }

    public List<MPEGDemuxerTrack> getVideoTracks() {
        List<MPEGDemuxerTrack> result = new List<MPEGDemuxerTrack>();
        foreach (BaseTrack p in streams.Values)
        {
            if (MPSUtils.videoStream(p.streamId))
                result.Add(p);
        }
        return result;
    }

    public List<MPEGDemuxerTrack> getAudioTracks() {
        List<MPEGDemuxerTrack> result = new List<MPEGDemuxerTrack>();
        foreach (BaseTrack p in streams.Values)
        {
            if (MPSUtils.audioStream(p.streamId))
                result.Add(p);
        }
        return result;
    }

    private bool isMPEG(MemoryStream _data) {
        MemoryStream b = _data.duplicate();
        long marker = 0xffffffff;

        int score = 0;
        bool hasHeader = false, slicesStarted = false;
        while (b.hasRemaining()) {
            int code = b.get() & 0xff;
            marker = (marker << 8) | code;
            if (marker < 0x100 || marker > 0x1b8)
                continue;

            if (marker >= 0x1B0 && marker <= 0x1B8) {
                if ((hasHeader && marker != 0x1B5 && marker != 0x1B2) || slicesStarted)
                    break;
                score += 5;
            } else if (marker == 0x100) {
                if (slicesStarted)
                    break;
                hasHeader = true;
            } else if (marker > 0x100 && marker < 0x1B0) {
                if (!hasHeader)
                    break;
                if (!slicesStarted) {
                    score += 50;
                    slicesStarted = true;
                }
                score += 1;
            }
        }
        return score > 50;
    }

    public static int probe(MemoryStream b) {
        long marker = 0xffffffff;

        int score = 0;
        bool inVideoPes = false, hasHeader = false, slicesStarted = false;
        while (b.hasRemaining()) {
            int code = b.get() & 0xff;
            marker = (marker << 8) | code;
            if (marker < 0x100 || marker > 0x1ff)
                continue;

            if (MPSUtils.videoMarker(marker)) {
                if (inVideoPes)
                    break;
                else
                    inVideoPes = true;
            } else if (marker >= 0x1B0 && marker <= 0x1B8 && inVideoPes) {
                if ((hasHeader && marker != 0x1B5 && marker != 0x1B2) || slicesStarted)
                    break;
                score += 5;
            } else if (marker == 0x100 && inVideoPes) {
                if (slicesStarted)
                    break;
                hasHeader = true;
            } else if (marker > 0x100 && marker < 0x1B0) {
                if (!hasHeader)
                    break;
                if (!slicesStarted) {
                    score += 50;
                    slicesStarted = true;
                }
                score += 1;
            }
        }

        return score;
    }

    public class MPEGPacket : Packet {
        private long offset;
        private MemoryStream seq;
        private int gop;
        private int timecode;

        public MPEGPacket(MemoryStream data, long pts, long timescale, long duration, long frameNo, bool keyFrame,
                TapeTimecode tapeTimecode) : base(data, pts, timescale, duration, frameNo, keyFrame, tapeTimecode) {
            
        }

        public long getOffset() {
            return offset;
        }

        public MemoryStream getSeq() {
            return seq;
        }

        public int getGOP() {
            return gop;
        }

        public int getTimecode() {
            return timecode;
        }
    }
}
}
