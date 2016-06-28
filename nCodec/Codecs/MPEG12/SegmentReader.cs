using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.MPEG12
{
    public class SegmentReader {

    internal protected Stream channel;
    private MemoryStream buf;
    protected int curMarker;
    private int fetchSize;
    private bool done;
    private long pos;

    public SegmentReader(Stream channel) : this(channel, 4096){
        
    }

    public SegmentReader(Stream channel, int fetchSize)
    {
        this.channel = channel;
        this.fetchSize = fetchSize;
        buf = StreamExtensions.fetchFrom(channel, 4);
        pos = buf.remaining();
        curMarker = buf.getInt();
    }

    public  bool readToNextMarker(MemoryStream outb) {
        if (done)
            return false;
        int n = 1;
        do {
            while (buf.hasRemaining()) {
                if (curMarker >= 0x100 && curMarker <= 0x1ff) {
                    if (n == 0) {
                        return true;
                    }
                    --n;
                }
                outb.put((byte) (curMarker >> 24));
                curMarker = (curMarker << 8) | (buf.get() & 0xff);
            }
            buf = StreamExtensions.fetchFrom(channel, fetchSize);
            pos += buf.remaining();
        } while (buf.hasRemaining());
        outb.putInt(curMarker);
        done = true;

        return false;
    }

    public bool skipToMarker() {
        if (done)
            return false;
        do {
            while (buf.hasRemaining()) {
                curMarker = (curMarker << 8) | (buf.get() & 0xff);
                if (curMarker >= 0x100 && curMarker <= 0x1ff) {
                    return true;
                }
            }
            buf = StreamExtensions.fetchFrom(channel, fetchSize);
            pos += buf.remaining();
        } while (buf.hasRemaining());
        done = true;

        return false;
    }

    public  bool read(MemoryStream outb, int length){
        if (done)
            return false;
        do {
            while (buf.hasRemaining()) {
                if (length-- == 0)
                    return true;
                outb.put((byte) (curMarker >> 24));
                curMarker = (curMarker << 8) | (buf.get() & 0xff);
            }
            buf = StreamExtensions.fetchFrom(channel, fetchSize);
            pos += buf.remaining();
        } while (buf.hasRemaining());
        outb.putInt(curMarker);
        done = true;

        return false;
    }

    public  long curPos() {
        return pos - buf.remaining() - 4;
    }
}
}
