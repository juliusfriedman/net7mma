using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class VideoSampleEntry : SampleEntry
    {
        private static MyFactory FACTORY = new MyFactory();
        private short version;
        private short revision;
        private String vendor;
        private int temporalQual;
        private int spacialQual;
        private short width;
        private short height;
        private float hRes;
        private float vRes;
        private short frameCount;
        private String compressorName;
        private short depth;
        private short clrTbl;

        public VideoSampleEntry(Header atom, short version, short revision, String vendor, int temporalQual,
                int spacialQual, short width, short height, long hRes, long vRes, short frameCount, String compressorName,
                short depth, short drefInd, short clrTbl)
            : base(atom, drefInd)
        {
            factory = FACTORY;
            this.version = version;
            this.revision = revision;
            this.vendor = vendor;
            this.temporalQual = temporalQual;
            this.spacialQual = spacialQual;
            this.width = width;
            this.height = height;
            this.hRes = hRes;
            this.vRes = vRes;
            this.frameCount = frameCount;
            this.compressorName = compressorName;
            this.depth = depth;
            this.clrTbl = clrTbl;
        }

        public VideoSampleEntry(Header atom)
            : base(atom)
        {

            factory = FACTORY;
        }

        public override void parse(MemoryStream input)
        {
            base.parse(input);

            version = input.getShort();
            revision = input.getShort();
            vendor = StreamExtensions.readString(input, 4);
            temporalQual = input.getInt();
            spacialQual = input.getInt();

            width = input.getShort();
            height = input.getShort();

            hRes = (float)input.getInt() / 65536f;
            vRes = (float)input.getInt() / 65536f;

            input.getInt(); // Reserved

            frameCount = input.getShort();

            compressorName = StreamExtensions.readPascalString(input, 31);

            depth = input.getShort();

            clrTbl = input.getShort();

            parseExtensions(input);
        }

        protected override void doWrite(MemoryStream outb)
        {

            base.doWrite(outb);

            outb.putShort(version);
            outb.putShort(revision);
            outb.Write(Encoding.ASCII.GetBytes(vendor), 0, 4);
            outb.putInt(temporalQual);
            outb.putInt(spacialQual);

            outb.putShort((short)width);
            outb.putShort((short)height);

            outb.putInt((int)(hRes * 65536));
            outb.putInt((int)(vRes * 65536));

            outb.putInt(0); // data size

            outb.putShort(frameCount);

            StreamExtensions.writePascalString(outb, compressorName, 31);

            outb.putShort(depth);

            outb.putShort(clrTbl);

            writeExtensions(outb);
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }

        public float gethRes()
        {
            return hRes;
        }

        public float getvRes()
        {
            return vRes;
        }

        public long getFrameCount()
        {
            return frameCount;
        }

        public String getCompressorName()
        {
            return compressorName;
        }

        public long getDepth()
        {
            return depth;
        }

        public String getVendor()
        {
            return vendor;
        }

        public short getVersion()
        {
            return version;
        }

        public short getRevision()
        {
            return revision;
        }

        public int getTemporalQual()
        {
            return temporalQual;
        }

        public int getSpacialQual()
        {
            return spacialQual;
        }

        public short getClrTbl()
        {
            return clrTbl;
        }



        public class MyFactory : BoxFactory
        {
            private Dictionary<String, Box> mappings = new Dictionary<String, Box>();

            public MyFactory()
            {
                //mappings.put(PixelAspectExt.fourcc(), PixelAspectExt.gclass);
                //// mappings.put(AvcCBox.fourcc(), AvcCBox.class);
                //mappings.put(ColorExtension.fourcc(), ColorExtension.gclass);
                //mappings.put(GamaExtension.fourcc(), GamaExtension.gclass);
                //mappings.put(CleanApertureExtension.fourcc(), CleanApertureExtension.gclass);
                //mappings.put(FielExtension.fourcc(), FielExtension.gclass);
            }

            public Box toClass(String fourcc)
            {
                return mappings[(fourcc)];
            }
        }
    }
}
