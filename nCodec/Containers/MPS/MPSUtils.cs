using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Containers.MPS
{
    public class MPSUtils {

    public static  int VIDEO_MIN = 0x1E0;
    public static  int VIDEO_MAX = 0x1EF;

    public static  int AUDIO_MIN = 0x1C0;
    public static  int AUDIO_MAX = 0x1DF;

    public static  int PACK = 0x1ba;
    public static  int SYSTEM = 0x1bb;
    public static  int PSM = 0x1bc;
    public static  int PRIVATE_1 = 0x1bd;
    public static  int PRIVATE_2 = 0x1bf;

    public static  bool mediaStream(int streamId) {
        return (streamId >= _S(AUDIO_MIN) && streamId <= _S(VIDEO_MAX) || streamId == _S(PRIVATE_1) || streamId == _S(PRIVATE_2));
    }

    public static  bool mediaMarker(int marker) {
        return (marker >= AUDIO_MIN && marker <= VIDEO_MAX || marker == PRIVATE_1 || marker == PRIVATE_2);
    }

    public static  bool psMarker(int marker) {
        return marker >= PRIVATE_1 && marker <= VIDEO_MAX;
    }

    public static bool videoMarker(long marker) {
        return marker >= VIDEO_MIN && marker <= VIDEO_MAX;
    }

    public static  bool videoStream(int streamId) {
        return streamId >= _S(VIDEO_MIN) && streamId <= _S(VIDEO_MAX);
    }

    public static bool audioStream(int streamId) {
        return streamId >= _S(AUDIO_MIN) && streamId <= _S(AUDIO_MAX) || streamId == _S(PRIVATE_1)
                || streamId == _S(PRIVATE_2);
    }

    static int _S(int marker) {
        return marker & 0xff;
    }

    public abstract class PESReader {

        private int marker = -1;
        private int lenFieldLeft;
        private int pesLen;
        private long pesFileStart = -1;
        private int stream;
        private bool pesa;
        private int pesLeft;

        private MemoryStream pesBuffer = new MemoryStream(1 << 21);

        protected abstract void pes(MemoryStream pesBuffer, long start, int pesLen, int stream);

        public void analyseBuffer(MemoryStream buf, long pos) {
            int init = buf.position();
            while (buf.hasRemaining()) {
                if (pesLeft > 0) {
                    int toRead = Math.Min(buf.remaining(), pesLeft);
                    pesBuffer.put(StreamExtensions.read(buf, toRead));
                    pesLeft -= toRead;

                    if (pesLeft == 0) {
                        long filePos = pos + buf.position() - init;
                        pes1(pesBuffer, pesFileStart, (int) (filePos - pesFileStart), stream);
                        pesFileStart = -1;
                        pesa = false;
                        stream = -1;
                    }
                    continue;
                }
                int bt = buf.get() & 0xff;
                if (pesa)
                    pesBuffer.put((byte) (marker >> 24));
                marker = (marker << 8) | bt;
                if (marker >= SYSTEM && marker <= VIDEO_MAX) {
                    long filePos = pos + buf.position() - init - 4;
                    if (pesa)
                        pes1(pesBuffer, pesFileStart, (int) (filePos - pesFileStart), stream);
                    pesFileStart = filePos;

                    pesa = true;
                    stream = marker & 0xff;
                    lenFieldLeft = 2;
                    pesLen = 0;
                } else if (marker >= 0x1b9 && marker <= 0x1ff) {
                    if (pesa) {
                        long filePos = pos + buf.position() - init - 4;
                        pes1(pesBuffer, pesFileStart, (int) (filePos - pesFileStart), stream);
                    }
                    pesFileStart = -1;
                    pesa = false;
                    stream = -1;
                } else if (lenFieldLeft > 0) {
                    pesLen = (pesLen << 8) | bt;
                    lenFieldLeft--;
                    if (lenFieldLeft == 0) {
                        pesLeft = pesLen;
                        if (pesLen != 0) {
                            pesBuffer.put((byte) (marker >> 24));
                            pesBuffer.put((byte) ((marker >> 16) & 0xff));
                            pesBuffer.put((byte) ((marker >> 8) & 0xff));
                            pesBuffer.put((byte) (marker & 0xff));
                            marker = -1;
                        }
                    }
                }
            }
        }

        private void pes1(MemoryStream pesBuffer, long start, int pesLen, int stream) {
            pesBuffer.flip();
            pes(pesBuffer, start, pesLen, stream);
            pesBuffer.clear();
        }
    }

    public static nVideo.Containers.MPS.MPSDemuxer.PESPacket readPESHeader(MemoryStream iss, long pos) {
        int streamId = iss.getInt() & 0xff;
        int len = iss.getShort();
        int b0 = iss.get() & 0xff;
        if ((b0 & 0xc0) == 0x80)
            return mpeg2Pes(b0, len, streamId, iss, pos);
        else
            return mpeg1Pes(b0, len, streamId, iss, pos);
    }

    public static nVideo.Containers.MPS.MPSDemuxer.PESPacket mpeg1Pes(int b0, int len, int streamId, MemoryStream ibs, long pos) {
        int c = b0;
        while (c == 0xff) {
            c = ibs.get() & 0xff;
        }

        if ((c & 0xc0) == 0x40) {
            ibs.get();
            c = ibs.get() & 0xff;
        }
        long pts = -1, dts = -1;
        if ((c & 0xf0) == 0x20) {
            pts = readTs(ibs, c);
        } else if ((c & 0xf0) == 0x30) {
            pts = readTs(ibs, c);
            dts = readTs(ibs);
        } else {
            if (c != 0x0f)
                throw new Exception("Invalid data");
        }

        return new nVideo.Containers.MPS.MPSDemuxer.PESPacket(null, pts, streamId, len, pos, dts);
    }

    public static long readTs(MemoryStream ibs, int c) {
        return (((long) c & 0x0e) << 29) | ((ibs.get() & 0xff) << 22) | (((ibs.get() & 0xff) >> 1) << 15)
                | ((ibs.get() & 0xff) << 7) | ((ibs.get() & 0xff) >> 1);
    }

    public static nVideo.Containers.MPS.MPSDemuxer.PESPacket mpeg2Pes(int b0, int len, int streamId, MemoryStream ibs, long pos) {
        int flags1 = b0;
        int flags2 = ibs.get() & 0xff;
        int header_len = ibs.get() & 0xff;

        long pts = -1, dts = -1;
        if ((flags2 & 0xc0) == 0x80) {
            pts = readTs(ibs);
            StreamExtensions.skip(ibs, header_len - 5);
        } else if ((flags2 & 0xc0) == 0xc0) {
            pts = readTs(ibs);
            dts = readTs(ibs);
            StreamExtensions.skip(ibs, header_len - 10);
        } else
            StreamExtensions.skip(ibs, header_len);
        
        return new nVideo.Containers.MPS.MPSDemuxer.PESPacket(null, pts, streamId, len, pos, dts);
    }

    public static long readTs(MemoryStream iss) {
        return (((long) iss.get() & 0x0e) << 29) | ((iss.get() & 0xff) << 22) | (((iss.get() & 0xff) >> 1) << 15)
                | ((iss.get() & 0xff) << 7) | ((iss.get() & 0xff) >> 1);
    }

    public class MPEGMediaDescriptor {
        private int tag;
        private int len;

        public void parse(MemoryStream buf) {
            tag = buf.get() & 0xff;
            len = buf.get() & 0xff;
        }

    }

    public class VideoStreamDescriptor : MPEGMediaDescriptor {

        private int multipleFrameRate;
        private int frameRateCode;
        private bool mpeg1Only;
        private int constrainedParameter;
        private int stillPicture;
        private int profileAndLevel;
        private int chromaFormat;
        private int frameRateExtension;

        
        public void parse(MemoryStream buf) {
            base.parse(buf);
            int b0 = buf.get() & 0xff;
            multipleFrameRate = (b0 >> 7) & 1;
            frameRateCode = (b0 >> 3) & 0xf;
            mpeg1Only = ((b0 >> 2) & 1) == 0;
            constrainedParameter = (b0 >> 1) & 1;
            stillPicture = b0 & 1;
            if (!mpeg1Only) {
                profileAndLevel = buf.get() & 0xff;
                int b1 = buf.get() & 0xff;
                chromaFormat = b1 >> 6;
                frameRateExtension = (b1 >> 5) & 1;
            }
        }

        Rational[] frameRates = new Rational[] { null, new Rational(24000, 1001), new Rational(24, 1),
                new Rational(25, 1), new Rational(30000, 1001), new Rational(30, 1), new Rational(50, 1),
                new Rational(60000, 1001), new Rational(60, 1), null, null, null, null, null, null, null

        };

        public Rational getFrameRate() {
            return frameRates[frameRateCode];
        }
    }

    public class AudioStreamDescriptor : MPEGMediaDescriptor {
        
        public void parse(MemoryStream buf) {
            base.parse(buf);
            int b0 = buf.get() & 0xff;
            int free_format_flag = (b0 >> 7) & 1;
            int ID = (b0 >> 6) & 1;
            int layer = (b0 >> 5) & 3;
            int variable_rate_audio_indicator = (b0 >> 3) & 1;

        }
    }

    public class ISO639LanguageDescriptor : MPEGMediaDescriptor {
        
        public void parse(MemoryStream buf) {
            base.parse(buf);
            while (buf.remaining() >= 4) {
                int i = buf.getInt();
            }
        }
    }

    public class Mpeg4VideoDescriptor : MPEGMediaDescriptor {
        private int profileLevel;

        
        public void parse(MemoryStream buf) {
            base.parse(buf);
            profileLevel = buf.get() & 0xff;
        }
    }

    public class Mpeg4AudioDescriptor : MPEGMediaDescriptor {

        private int profileLevel;

        public void parse(MemoryStream buf) {
            base.parse(buf);
            profileLevel = buf.get() & 0xff;
        }
    }

    public class AVCVideoDescriptor : MPEGMediaDescriptor {

        private int profileIdc;
        private int flags;
        private int level;

        
        public void parse(MemoryStream buf) {
            base.parse(buf);
            profileIdc = buf.get() & 0xff;
            flags = buf.get() & 0xff;
            level = buf.get() & 0xff;
        }
    }

    public class AACAudioDescriptor : MPEGMediaDescriptor {
        private int profile;
        private int channel;
        private int flags;

        public void parse(MemoryStream buf) {
            base.parse(buf);
            profile = buf.get() & 0xff;
            channel = buf.get() & 0xff;
            flags = buf.get() & 0xff;
        }
    }

    public class MP4TextDescriptor : MPEGMediaDescriptor {

    }

    //public static Class<MPEGMediaDescriptor>[] dMapping = new Class[256];

    //static {
    //    dMapping[2] = VideoStreamDescriptor.class;
    //    dMapping[3] = AudioStreamDescriptor.class;
    //    dMapping[10] = ISO639LanguageDescriptor.class;
    //    dMapping[27] = Mpeg4VideoDescriptor.class;
    //    dMapping[28] = Mpeg4AudioDescriptor.class;
    //    dMapping[40] = AVCVideoDescriptor.class;
    //    dMapping[43] = AACAudioDescriptor.class;
    //}

    //public static List<MPEGMediaDescriptor> parseDescriptors(MemoryStream bb) {
    //    List<MPEGMediaDescriptor> result = new List<MPEGMediaDescriptor>();
    //    while (bb.remaining() >= 2) {
    //        int tag = bb.get() & 0xff;
    //        MemoryStream buf = NIOUtils.read(bb, bb.get() & 0xff);
    //        if (dMapping[tag] != null)
    //            try {
    //                dMapping[tag].newInstance().parse(buf);
    //            } catch (Exception e) {
    //                throw new RuntimeException(e);
    //            }
    //    }
    //    return result;
    //}
    }
}
