using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    class Utility
    {

        private static SliceHeaderReader shr = new SliceHeaderReader();
        private static SliceHeaderWriter shw = new SliceHeaderWriter();

        public static System.IO.MemoryStream nextNALUnit(System.IO.MemoryStream buf)
        {
            skipToNALUnit(buf);
            return gotoNALUnit(buf);
        }

        public static void skipToNALUnit(System.IO.MemoryStream buf)
        {

            if (!buf.hasRemaining())
                return;

            uint val = 0xffffffff;
            while (buf.Position < buf.Length)
            {
                val <<= 8;
                val |= (uint)(buf.ReadByte() & 0xff);
                if ((val & 0xffffff) == 1)
                {
                    buf.Position = buf.Position;
                    break;
                }
            }
        }

        /**
         * Finds next Nth H.264 bitstream NAL unit (0x00000001) and returns the data
         * that preceeds it as a System.IO.MemoryStream slice
         * 
         * Segment byte order is always little endian
         * 
         * TODO: emulation prevention
         * 
         * @param buf
         * @return
         */
        public static System.IO.MemoryStream gotoNALUnit(System.IO.MemoryStream buf)
        {

            if (buf.Position >= buf.Length)
                return null;

            long from = buf.Position;

            byte[] temp = new byte[buf.Length - buf.Position];

            buf.Read(temp, 0, (int)(buf.Length - buf.Position));

            System.IO.MemoryStream result = new System.IO.MemoryStream(temp);
            //result.order(ByteOrder.BIG_ENDIAN);

            long val = 0xffffffff;
            while (buf.Position < buf.Length)
            {
                val <<= 8;
                val |= (buf.ReadByte() & 0xff);
                if ((val & 0xffffff) == 1)
                {
                    buf.Position = buf.Position - (val == 1 ? 4 : 3);
                    result.SetLength(buf.Position - from);
                    break;
                }
            }
            return result;
        }

        public static void unescapeNAL(System.IO.MemoryStream _buf)
        {
            if (_buf.Position - _buf.Length < 2)
                return;
            System.IO.MemoryStream inb = new System.IO.MemoryStream(_buf.ToArray());
            System.IO.MemoryStream outb = new System.IO.MemoryStream(_buf.ToArray());
            byte p1 = (byte)inb.ReadByte();
            outb.WriteByte(p1);
            byte p2 = (byte)inb.ReadByte();
            outb.WriteByte(p2);
            while (inb.Position < inb.Length)
            {
                byte b = (byte)inb.ReadByte();
                if (p1 != 0 || p2 != 0 || b != 3)
                    outb.WriteByte(b);
                p1 = p2;
                p2 = b;
            }
            _buf.SetLength(outb.Position);
        }

        public static void escapeNAL(System.IO.MemoryStream src)
        {
            int[] loc = searchEscapeLocations(src);

            int old = (int)src.limit();
            src.limit(src.limit() + loc.Length);

            for (int newPos = (int)src.limit() - 1, oldPos = old - 1, locIdx = loc.Length - 1; newPos >= src.Position; newPos--, oldPos--)
            {
                src.put(newPos, src.get(oldPos));
                if (locIdx >= 0 && loc[locIdx] == oldPos)
                {
                    newPos--;
                    src.put(newPos, (byte)3);
                    locIdx--;
                }
            }
        }

        private static int[] searchEscapeLocations(System.IO.MemoryStream src)
        {
            List<int> points = new List<int>();
            System.IO.MemoryStream search = src.duplicate();
            short p = search.getShort();
            while (search.hasRemaining())
            {
                byte b = (byte)search.ReadByte();
                if (p == 0 && (b & ~3) == 0)
                {
                    points.Add((int)(search.Position - 1));
                    p = 3;
                }
                p <<= 8;
                p |= (short)(b & 0xff);
            }
            int[] array = points.ToArray();
            return array;
        }

        public static void escapeNAL(System.IO.MemoryStream src, System.IO.MemoryStream dst)
        {
            byte p1 = (byte)src.ReadByte(), p2 = (byte)src.ReadByte();
            dst.put(p1);
            dst.put(p2);
            while (src.hasRemaining())
            {
                byte b = (byte)src.ReadByte();
                if (p1 == 0 && p2 == 0 && (b & 0xff) <= 3)
                {
                    dst.put((byte)3);
                    p1 = p2;
                    p2 = 3;
                }
                dst.put(b);
                p1 = p2;
                p2 = b;
            }
        }

        public static int golomb2Signed(int val)
        {
            int sign = ((val & 0x1) << 1) - 1;
            val = ((val >> 1) + (val & 0x1)) * sign;
            return val;
        }

        public static List<System.IO.MemoryStream> splitMOVPacket(System.IO.MemoryStream buf, AvcCBox avcC)
        {
            List<System.IO.MemoryStream> result = new List<System.IO.MemoryStream>();
            int nls = avcC.getNalLengthSize();
            System.IO.MemoryStream dup = buf.duplicate();
            while (dup.remaining() >= nls)
            {
                int len = readLen(dup, nls);
                if (len == 0)
                    break;
                result.Add(StreamExtensions.read(dup, len));
            }
            return result;
        }

        private static int readLen(System.IO.MemoryStream dup, int nls)
        {
            switch (nls)
            {
                case 1:
                    return dup.ReadByte() & 0xff;
                case 2:
                    return dup.getShort() & 0xffff;
                case 3:
                    return ((dup.getShort() & 0xffff) << 8) | (dup.ReadByte() & 0xff);
                case 4:
                    return dup.getInt();
                default:
                    throw new ArgumentException("NAL Unit length size can not be " + nls);
            }
        }

        /**
         * Encodes AVC frame in ISO BMF format. Takes Annex B format.
         * 
         * Scans the packet for each NAL Unit starting with 00 00 00 01 and replaces
         * this 4 byte sequence with 4 byte integer representing this NAL unit
         * length. Removes any leading SPS/PPS structures and collects them into a
         * provided storaae.
         * 
         * @param avcFrame
         *            AVC frame encoded in Annex B NAL unit format
         */
        public static void encodeMOVPacket(System.IO.MemoryStream avcFrame)
        {

            System.IO.MemoryStream dup = avcFrame.duplicate();
            System.IO.MemoryStream d1 = avcFrame.duplicate();

            for (int tot = (int)d1.Position; ; )
            {
                System.IO.MemoryStream buf = Utility.nextNALUnit(dup);
                if (buf == null)
                    break;
                d1.position(tot);
                d1.putInt(buf.remaining());
                tot += buf.remaining() + 4;
            }
        }

        /**
         * Wipes AVC parameter sets ( SPS/PPS ) from the packet
         * 
         * @param in
         *            AVC frame encoded in Annex B NAL unit format
         * @param out
         *            Buffer where packet without PS will be put
         * @param spsList
         *            Storage for leading SPS structures ( can be null, then all
         *            leading SPSs are discarded ).
         * @param ppsList
         *            Storage for leading PPS structures ( can be null, then all
         *            leading PPSs are discarded ).
         */
        public static void wipePS(System.IO.MemoryStream inb, System.IO.MemoryStream outb, List<System.IO.MemoryStream> spsList, List<System.IO.MemoryStream> ppsList)
        {

            System.IO.MemoryStream dup = inb.duplicate();
            while (dup.hasRemaining())
            {
                System.IO.MemoryStream buf = nextNALUnit(dup);
                if (buf == null)
                    break;

                NALUnit nu = NALUnit.read(buf.duplicate());
                if (nu.type == NALUnitType.PPS)
                {
                    if (ppsList != null)
                        ppsList.Add(buf);
                }
                else if (nu.type == NALUnitType.SPS)
                {
                    if (spsList != null)
                        spsList.Add(buf);
                }
                else
                {
                    outb.putInt(1);
                    outb.put(buf);
                }
            }
            outb.flip();
        }

        /**
         * Wipes AVC parameter sets ( SPS/PPS ) from the packet ( inplace operation
         * )
         * 
         * @param in
         *            AVC frame encoded in Annex B NAL unit format
         * @param spsList
         *            Storage for leading SPS structures ( can be null, then all
         *            leading SPSs are discarded ).
         * @param ppsList
         *            Storage for leading PPS structures ( can be null, then all
         *            leading PPSs are discarded ).
         */
        public static void wipePS(System.IO.MemoryStream inb, List<System.IO.MemoryStream> spsList, List<System.IO.MemoryStream> ppsList)
        {
            System.IO.MemoryStream dup = inb.duplicate();
            while (dup.hasRemaining())
            {
                System.IO.MemoryStream buf = nextNALUnit(dup);
                if (buf == null)
                    break;

                NALUnit nu = NALUnit.read(buf);
                if (nu.type == NALUnitType.PPS)
                {
                    if (ppsList != null)
                        ppsList.Add(buf);
                    inb.position(dup.Position);
                }
                else if (nu.type == NALUnitType.SPS)
                {
                    if (spsList != null)
                        spsList.Add(buf);
                    inb.position(dup.Position);
                }
                else if (nu.type == NALUnitType.IDR_SLICE || nu.type == NALUnitType.NON_IDR_SLICE)
                    break;
            }
        }

        public static SampleEntry createMOVSampleEntry(List<System.IO.MemoryStream> spsList, List<System.IO.MemoryStream> ppsList)
        {
            SeqParameterSet sps = readSPS(StreamExtensions.duplicate(spsList[0]));
            AvcCBox avcC = new AvcCBox(sps.profile_idc, 0, sps.level_idc, spsList, ppsList);

            int codedWidth = (sps.pic_width_in_mbs_minus1 + 1) << 4;
            int codedHeight = getPicHeightInMbs(sps) << 4;

            int width = sps.frame_cropping_flag ? codedWidth
                    - ((sps.frame_crop_right_offset + sps.frame_crop_left_offset) << sps.chroma_format_idc.compWidth[1])
                    : codedWidth;
            int height = sps.frame_cropping_flag ? codedHeight
                    - ((sps.frame_crop_bottom_offset + sps.frame_crop_top_offset) << sps.chroma_format_idc.compHeight[1])
                    : codedHeight;

            Size size = new Size(width, height);

            SampleEntry se = videoSampleEntry("avc1", size, "JCodec");
            se.Add(avcC);

            return se;
        }

        public static VideoSampleEntry videoSampleEntry(String fourcc, Size size, String encoderName)
        {
            return new VideoSampleEntry(new Header(fourcc), (short)0, (short)0, "jcod", 0, 768, (short)size.getWidth(),
                    (short)size.getHeight(), 72, 72, (short)1, encoderName != null ? encoderName : "jcodec", (short)24,
                    (short)1, (short)-1);
        }

        public static SampleEntry createMOVSampleEntry(SeqParameterSet initSPS, PictureParameterSet initPPS)
        {
            System.IO.MemoryStream bb1 = new System.IO.MemoryStream(512), bb2 = new System.IO.MemoryStream(512);
            initSPS.write(bb1);
            initPPS.write(bb2);
            bb1.flip();
            bb2.flip();
            return createMOVSampleEntry(new System.IO.MemoryStream[] { bb1 }.ToList(), new System.IO.MemoryStream[] { bb2 }.ToList());
        }

        public static bool idrSlice(System.IO.MemoryStream _data)
        {
            System.IO.MemoryStream data = _data.duplicate();
            System.IO.MemoryStream segment;
            while ((segment = nextNALUnit(data)) != null)
            {
                if (NALUnit.read(segment).type == NALUnitType.IDR_SLICE)
                    return true;
            }
            return false;
        }

        public static bool idrSlice(IEnumerable<MemoryStream> _data)
        {
            foreach (var segment in _data)
            {
                if (NALUnit.read(segment.duplicate()).type == NALUnitType.IDR_SLICE)
                    return true;
            }
            return false;
        }

        public static void saveRawFrame(System.IO.MemoryStream data, AvcCBox avcC, string f)
        {//throws IOException {
            Stream raw = new System.IO.FileStream(f, FileMode.OpenOrCreate);
            saveStreamParams(avcC, raw);
            data.CopyTo(raw);
            raw.Close();
        }

        public static void saveStreamParams(AvcCBox avcC, Stream raw)
        {//throws IOException {
            System.IO.MemoryStream bb = new System.IO.MemoryStream(1024);
            foreach (var byteByffer in avcC.getSpsList())
            {
                raw.Write(new byte[] { 0, 0, 0, 1, 0x67 }, 0, 5);

                Utility.escapeNAL(byteByffer.duplicate(), bb);
                bb.flip();
                raw.CopyTo(bb);
                bb.clear();
            }
            foreach (var byteBuffer in avcC.getPpsList())
            {
                raw.Write(new byte[] { 0, 0, 0, 1, 0x68 }, 0, 5);
                Utility.escapeNAL(byteBuffer.duplicate(), bb);
                bb.flip();
                raw.CopyTo(bb);
                bb.clear();
            }
        }

        public static int getPicHeightInMbs(SeqParameterSet sps)
        {
            int picHeightInMbs = (sps.pic_height_in_map_units_minus1 + 1) << (sps.frame_mbs_only_flag ? 0 : 1);
            return picHeightInMbs;
        }

        public static List<System.IO.MemoryStream> splitFrame(System.IO.MemoryStream frame)
        {
            List<System.IO.MemoryStream> result = new List<System.IO.MemoryStream>();

            System.IO.MemoryStream segment;
            while ((segment = Utility.nextNALUnit(frame)) != null)
            {
                result.Add(segment);
            }

            return result;
        }

        public static void joinNALUnits(List<System.IO.MemoryStream> nalUnits, System.IO.MemoryStream b)
        {
            foreach (System.IO.MemoryStream nal in nalUnits)
            {
                b.putInt(1);
                b.put(nal.duplicate());
            }
        }

        public static AvcCBox parseAVCC(VideoSampleEntry vse)
        {
            Box lb = Box.findFirst(vse, /*Box.bclass,*/ "avcC");
            if (lb is AvcCBox)
                return (AvcCBox)lb;
            else
            {
                AvcCBox avcC = new AvcCBox();
                avcC.parse(((LeafBox)lb).getData().duplicate());
                return avcC;
            }
        }

        public static System.IO.MemoryStream writeSPS(SeqParameterSet sps, int approxSize)
        {
            System.IO.MemoryStream output = new System.IO.MemoryStream(approxSize + 8);
            sps.write(output);
            output.flip();
            Utility.escapeNAL(output);
            return output;
        }

        public static SeqParameterSet readSPS(System.IO.MemoryStream data)
        {
            System.IO.MemoryStream input = data.duplicate();
            Utility.unescapeNAL(input);
            SeqParameterSet sps = SeqParameterSet.read(input);
            return sps;
        }

        public static System.IO.MemoryStream writePPS(PictureParameterSet pps, int approxSize)
        {
            System.IO.MemoryStream output = new System.IO.MemoryStream(approxSize + 8);
            pps.write(output);
            output.flip();
            Utility.escapeNAL(output);
            return output;
        }

        public static PictureParameterSet readPPS(System.IO.MemoryStream data)
        {
            System.IO.MemoryStream input = data.duplicate();
            Utility.unescapeNAL(input);
            PictureParameterSet pps = PictureParameterSet.read(input);
            return pps;
        }

        public static PictureParameterSet findPPS(List<PictureParameterSet> ppss, int id)
        {
            foreach (var pps in ppss)
            {
                if (pps.pic_parameter_set_id == id)
                    return pps;
            }
            return null;
        }

        public static SeqParameterSet findSPS(List<SeqParameterSet> spss, int id)
        {
            foreach (var sps in spss)
            {
                if (sps.seq_parameter_set_id == id)
                    return sps;
            }
            return null;
        }

        public abstract class SliceHeaderTweaker
        {

            private List<SeqParameterSet> sps;
            private List<PictureParameterSet> pps;

            public SliceHeaderTweaker()
            {
            }

            public SliceHeaderTweaker(List<System.IO.MemoryStream> spsList, List<System.IO.MemoryStream> ppsList)
            {
                this.sps = readSPS(spsList);
                this.pps = readPPS(ppsList);
            }

            protected abstract void tweak(SliceHeader sh);

            public SliceHeader run(System.IO.MemoryStream iss, System.IO.MemoryStream os, NALUnit nu)
            {
                System.IO.MemoryStream nal = os.duplicate();

                Utility.unescapeNAL(iss);

                BitReader reader = new BitReader(iss);
                SliceHeader sh = shr.readPart1(reader);

                PictureParameterSet pp = findPPS(pps, sh.pic_parameter_set_id);

                return part2(iss, os, nu, findSPS(sps, pp.pic_parameter_set_id), pp, nal, reader, sh);
            }

            public SliceHeader run(System.IO.MemoryStream iss, System.IO.MemoryStream os, NALUnit nu, SeqParameterSet sps, PictureParameterSet pps)
            {
                System.IO.MemoryStream nal = os.duplicate();

                Utility.unescapeNAL(iss);

                BitReader reader = new BitReader(iss);
                SliceHeader sh = shr.readPart1(reader);

                return part2(iss, os, nu, sps, pps, nal, reader, sh);
            }

            private SliceHeader part2(System.IO.MemoryStream iss, System.IO.MemoryStream os, NALUnit nu, SeqParameterSet sps,
                    PictureParameterSet pps, System.IO.MemoryStream nal, BitReader reader, SliceHeader sh)
            {
                BitWriter writer = new BitWriter(os);
                shr.readPart2(sh, nu, sps, pps, reader);

                tweak(sh);

                shw.write(sh, nu.type == NALUnitType.IDR_SLICE, nu.nal_ref_idc, writer);

                if (pps.entropy_coding_mode_flag)
                    copyDataCABAC(iss, os, reader, writer);
                else
                    copyDataCAVLC(iss, os, reader, writer);

                nal.limit(os.Position);

                Utility.escapeNAL(nal);

                os.position(nal.limit());

                return sh;
            }

            private void copyDataCAVLC(System.IO.MemoryStream iss, System.IO.MemoryStream os, BitReader reader, BitWriter writer)
            {
                int wLeft = 8 - writer.curBit();
                if (wLeft != 0)
                    writer.writeNBit(reader.readNBit(wLeft), wLeft);
                writer.flush();

                // Copy with shift
                int shift = reader.curBit();
                if (shift != 0)
                {
                    int mShift = 8 - shift;
                    int inp = reader.readNBit(mShift);
                    reader.stop();

                    while (iss.hasRemaining())
                    {
                        int outb = inp << shift;
                        inp = iss.ReadByte() & 0xff;
                        outb |= inp >> mShift;

                        os.put((byte)outb);
                    }
                    os.put((byte)(inp << shift));
                }
                else
                {
                    reader.stop();
                    os.put(iss);
                }
            }

            private void copyDataCABAC(System.IO.MemoryStream iss, System.IO.MemoryStream os, BitReader reader, BitWriter writer)
            {
                int bp = reader.curBit();
                if (bp != 0)
                {
                    int rem = reader.readNBit(8 - (int)bp);
                    if ((1 << (8 - bp)) - 1 != rem)
                        throw new Exception("Invalid CABAC padding");
                }

                if (writer.curBit() != 0)
                    writer.writeNBit(0xff, 8 - writer.curBit());
                writer.flush();
                reader.stop();

                os.put(iss);
            }
        }

        public static Size getPicSize(SeqParameterSet sps)
        {
            int w = (sps.pic_width_in_mbs_minus1 + 1) << 4;
            int h = getPicHeightInMbs(sps) << 4;
            if (sps.frame_cropping_flag)
            {
                w -= (sps.frame_crop_left_offset + sps.frame_crop_right_offset) << sps.chroma_format_idc.compWidth[1];
                h -= (sps.frame_crop_top_offset + sps.frame_crop_bottom_offset) << sps.chroma_format_idc.compHeight[1];
            }
            return new Size(w, h);
        }

        public static List<SeqParameterSet> readSPS(List<System.IO.MemoryStream> spsList)
        {
            List<SeqParameterSet> result = new List<SeqParameterSet>();
            foreach (var byteBuffer in spsList)
            {
                result.Add(readSPS(StreamExtensions.duplicate(byteBuffer)));
            }
            return result;
        }

        public static List<PictureParameterSet> readPPS(List<System.IO.MemoryStream> ppsList)
        {
            List<PictureParameterSet> result = new List<PictureParameterSet>();
            foreach (var byteBuffer in ppsList)
            {
                result.Add(readPPS(StreamExtensions.duplicate(byteBuffer)));
            }
            return result;
        }

        public static List<System.IO.MemoryStream> writePPS(List<PictureParameterSet> allPps)
        {
            List<System.IO.MemoryStream> result = new List<System.IO.MemoryStream>();
            foreach (var pps in allPps)
            {
                result.Add(writePPS(pps, 64));
            }
            return result;
        }

        public static List<System.IO.MemoryStream> writeSPS(List<SeqParameterSet> allSps)
        {
            List<System.IO.MemoryStream> result = new List<System.IO.MemoryStream>();
            foreach (var sps in allSps)
            {
                result.Add(writeSPS(sps, 256));
            }
            return result;
        }

        //public static void dumpFrame(FileStream ch, SeqParameterSet[] values, PictureParameterSet[] values2,
        //        List<System.IO.MemoryStream> nalUnits)
        //{// throws IOException {
        //    foreach (var sps in values)
        //    {
        //        NIOUtils.writeInt(ch, 1);
        //        NIOUtils.writeByte(ch, (byte)0x67);
        //        ch.write(writeSPS(sps, 128));
        //    }

        //    foreach (var pps in values2)
        //    {
        //        NIOUtils.writeInt(ch, 1);
        //        NIOUtils.writeByte(ch, (byte)0x68);
        //        ch.write(writePPS(pps, 256));
        //    }

        //    foreach (var bb in nalUnits)
        //    {
        //        NIOUtils.writeInt(ch, 1);
        //        ch.write(bb.duplicate());
        //    }
        //}
    }
}
