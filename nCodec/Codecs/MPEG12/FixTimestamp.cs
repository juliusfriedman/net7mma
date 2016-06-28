using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.MPEG12
{
    public abstract class FixTimestamp
    {
        public void fix(string file)
        {
            FileStream ra = null;
            try
            {
                ra = new FileStream(file, FileMode.Open);
                byte[] tsPkt = new byte[188];

                while (ra.Read(tsPkt, 0, 188) == 188)
                {

                    //Assert.assertEquals(0x47, tsPkt[0] & 0xff);
                    int guidFlags = ((tsPkt[1] & 0xff) << 8) | (tsPkt[2] & 0xff);
                    int guid = (int)guidFlags & 0x1fff;
                    int payloadStart = (guidFlags >> 14) & 0x1;
                    if (payloadStart == 0 || guid == 0)
                        continue;
                    MemoryStream bb = StreamExtensions.wrap(tsPkt, 4, 184);
                    if ((tsPkt[3] & 0x20) != 0)
                    {
                        StreamExtensions.skip(bb, bb.get() & 0xff);
                    }

                    long streamId = 0xffffffff;
                    while (bb.hasRemaining() && !(streamId >= 0x1bf && streamId < 0x1ef))
                    {
                        streamId <<= 8;
                        streamId |= bb.get() & 0xff;
                    }
                    if (streamId >= 0x1bf && streamId < 0x1ef)
                    {
                        int len = bb.getShort();
                        int b0 = bb.get() & 0xff;

                        bb.position(bb.position() - 1);
                        if ((b0 & 0xc0) == 0x80)
                            fixMpeg2(streamId & 0xff, bb);
                        else
                            fixMpeg1(streamId & 0xff, bb);

                        ra.Seek(ra.Position - 188, SeekOrigin.Current);
                        ra.Write(tsPkt, 0, tsPkt.Length);
                    }
                }
            }
            finally
            {
                if (ra != null)
                    ra.Dispose();
            }
        }

        public void fixMpeg1(long streamId, MemoryStream isb)
        {
            int c = isb.getInt() & 0xff;
            while (c == 0xff)
            {
                c = isb.get() & 0xff;
            }

            if ((c & 0xc0) == 0x40)
            {
                isb.get();
                c = isb.get() & 0xff;
            }
            if ((c & 0xf0) == 0x20)
            {
                isb.position(isb.position() - 1);
                fixTs(streamId, isb, true);
            }
            else if ((c & 0xf0) == 0x30)
            {
                isb.position(isb.position() - 1);
                fixTs(streamId, isb, true);
                fixTs(streamId, isb, false);
            }
            else
            {
                if (c != 0x0f)
                    throw new Exception("Invalid data");
            }
        }

        public long fixTs(long streamId, MemoryStream ibs, bool isPts)
        {
            byte b0 = ibs.get();
            byte b1 = ibs.get();
            byte b2 = ibs.get();
            byte b3 = ibs.get();
            byte b4 = ibs.get();

            long pts = (((long)b0 & 0x0e) << 29) | ((b1 & 0xff) << 22) | (((b2 & 0xff) >> 1) << 15) | ((b3 & 0xff) << 7)
                    | ((b4 & 0xff) >> 1);

            pts = doWithTimestamp(streamId, pts, isPts);

            ibs.position(ibs.position() - 5);

            ibs.put((byte)((b0 & 0xf0) | (pts >> 29) | 1));
            ibs.put((byte)(pts >> 22));
            ibs.put((byte)((pts >> 14) | 1));
            ibs.put((byte)(pts >> 7));
            ibs.put((byte)((pts << 1) | 1));

            return pts;
        }

        public void fixMpeg2(long streamId, MemoryStream isb)
        {
            int flags1 = isb.get() & 0xff;
            int flags2 = isb.get() & 0xff;
            int header_len = isb.get() & 0xff;

            if ((flags2 & 0xc0) == 0x80)
            {
                fixTs(streamId, isb, true);
            }
            else if ((flags2 & 0xc0) == 0xc0)
            {
                fixTs(streamId, isb, true);
                fixTs(streamId, isb, false);
            }
        }

        public bool isVideo(long streamId)
        {
            return streamId >= 0xe0 && streamId <= 0xef;
        }

        public bool isAudio(long streamId)
        {
            return streamId >= 0xbf && streamId <= 0xdf;
        }

        protected abstract long doWithTimestamp(long streamId, long pts, bool isPts);
    }
}