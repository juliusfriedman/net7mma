using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class H264Decoder : VideoDecoder
    {

        private Dictionary<int, SeqParameterSet> sps = new Dictionary<int, SeqParameterSet>();
        private Dictionary<int, PictureParameterSet> pps = new Dictionary<int, PictureParameterSet>();
        private Frame[] sRefs;
        private Dictionary<int, Frame> lRefs;
        private List<Frame> pictureBuffer;
        private POCManager poc;
        private bool debug;

        public H264Decoder(string name, Media.Common.Binary.ByteOrder byteOrder, int defaultComponentCount, int defaultBitsPerComponent)
            : base(name, byteOrder, defaultComponentCount, defaultBitsPerComponent)
        {
            pictureBuffer = new List<Frame>();
            poc = new POCManager();
        }


        public Frame decodeFrame(MemoryStream data, int[][] buffer)
        {
            return new FrameDecoder().decodeFrame(Utility.splitFrame(data), buffer);
        }

        public Frame decodeFrame(List<MemoryStream> nalUnits, int[][] buffer)
        {
            return new FrameDecoder().decodeFrame(nalUnits, buffer);
        }

        class FrameDecoder
        {
            private H264Decoder vdecoder;
            private SliceHeaderReader shr;
            private PictureParameterSet activePps;
            private SeqParameterSet activeSps;
            private DeblockingFilter filter;
            private SliceHeader firstSliceHeader;
            private NALUnit firstNu;
            private SliceDecoder decoder;
            private int[][][][] mvs;

            public Frame decodeFrame(List<MemoryStream> nalUnits, int[][] buffer)
            {
                Frame result = null;

                foreach (var nalUnit in nalUnits)
                {
                    NALUnit marker = NALUnit.read(nalUnit);

                    Utility.unescapeNAL(nalUnit);

                    switch (marker.type.getValue())
                    {
                        case 1:
                        case 5:
                            if (result == null)
                                result = init(buffer, nalUnit, marker);
                            decoder.decode(nalUnit, marker);
                            break;
                        case 8:
                            SeqParameterSet _sps = SeqParameterSet.read(nalUnit);
                            vdecoder.sps.Add(_sps.seq_parameter_set_id, _sps);
                            break;
                        case 7:
                            PictureParameterSet _pps = PictureParameterSet.read(nalUnit);
                            vdecoder.pps.Add(_pps.pic_parameter_set_id, _pps);
                            break;
                        default:
                            break;
                    }
                }

                filter.deblockFrame(result);

                updateReferences(result);

                return result;
            }

            private void updateReferences(Frame picture)
            {
                if (firstNu.nal_ref_idc != 0)
                {
                    if (firstNu.type == NALUnitType.IDR_SLICE)
                    {
                        performIDRMarking(firstSliceHeader.refPicMarkingIDR, picture);
                    }
                    else
                    {
                        performMarking(firstSliceHeader.refPicMarkingNonIDR, picture);
                    }
                }
            }

            private Frame init(int[][] buffer, MemoryStream segment, NALUnit marker)
            {
                firstNu = marker;

                shr = new SliceHeaderReader();
                BitReader br = new BitReader(segment.duplicate());
                firstSliceHeader = shr.readPart1(br);
                activePps = vdecoder.pps[firstSliceHeader.pic_parameter_set_id];
                activeSps = vdecoder.sps[activePps.seq_parameter_set_id];
                shr.readPart2(firstSliceHeader, marker, activeSps, activePps, br);
                int picWidthInMbs = activeSps.pic_width_in_mbs_minus1 + 1;
                int picHeightInMbs = Utility.getPicHeightInMbs(activeSps);

                int[][] nCoeff = (int[][])Array.CreateInstance(typeof(int), new int[] { picHeightInMbs << 2, picWidthInMbs << 2 });//new int[picHeightInMbs << 2][picWidthInMbs << 2];

                mvs = (int[][][][])Array.CreateInstance(typeof(int), new int[] { 2, picHeightInMbs << 2, picWidthInMbs << 2, 3 });//new int[2][picHeightInMbs << 2][picWidthInMbs << 2][3];
                MBType[] mbTypes = new MBType[picHeightInMbs * picWidthInMbs];
                bool[] tr8x8Used = new bool[picHeightInMbs * picWidthInMbs];
                int[][] mbQps = (int[][])Array.CreateInstance(typeof(int), new int[] { 3, picHeightInMbs * picWidthInMbs });//new int[3][picHeightInMbs * picWidthInMbs];
                SliceHeader[] shs = new SliceHeader[picHeightInMbs * picWidthInMbs];
                Frame[][][] refsUsed = new Frame[picHeightInMbs * picWidthInMbs][][];

                if (vdecoder.sRefs == null)
                {
                    vdecoder.sRefs = new Frame[1 << (firstSliceHeader.sps.log2_max_frame_num_minus4 + 4)];
                    vdecoder.lRefs = new Dictionary<int, Frame>();
                }

                Frame result = createFrame(activeSps, buffer, firstSliceHeader.frame_num, mvs, refsUsed,
                        vdecoder.poc.calcPOC(firstSliceHeader, firstNu));

                decoder = new SliceDecoder(activeSps, activePps, nCoeff, mvs, mbTypes, mbQps, shs, tr8x8Used, refsUsed,
                        result, vdecoder.sRefs, vdecoder.lRefs);
                decoder.setDebug(vdecoder.debug);

                filter = new DeblockingFilter(picWidthInMbs, activeSps.bit_depth_chroma_minus8 + 8, nCoeff, mvs, mbTypes,
                        mbQps, shs, tr8x8Used, refsUsed);

                return result;
            }

            public void performIDRMarking(RefPicMarkingIDR refPicMarkingIDR, Frame picture)
            {
                clearAll();
                vdecoder.pictureBuffer.Clear();

                Frame saved = saveRef(picture);
                if (refPicMarkingIDR.isUseForlongTerm())
                {
                    vdecoder.lRefs.Add(0, saved);
                    saved.setShortTerm(false);
                }
                else
                    vdecoder.sRefs[firstSliceHeader.frame_num] = saved;
            }

            private Frame saveRef(Frame decoded)
            {
                Frame frame;
                if (vdecoder.pictureBuffer.Count() > 0)
                {
                    frame = vdecoder.pictureBuffer[0];
                    vdecoder.pictureBuffer.RemoveAt(0);
                }
                else frame = Frame.createFrame(decoded);
                frame.copyFrom(decoded);
                return frame;
            }

            private void releaseRef(Frame picture)
            {
                if (picture != null)
                {
                    vdecoder.pictureBuffer.Add(picture);
                }
            }

            public void clearAll()
            {
                for (int i = 0; i < vdecoder.sRefs.Length; i++)
                {
                    releaseRef(vdecoder.sRefs[i]);
                    vdecoder.sRefs[i] = null;
                }
                int[] keys = vdecoder.lRefs.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    releaseRef(vdecoder.lRefs[i]);
                }
                vdecoder.lRefs.Clear();
            }

            public void performMarking(RefPicMarking refPicMarking, Frame picture)
            {
                Frame saved = saveRef(picture);

                if (refPicMarking != null)
                {
                    foreach (var instr in refPicMarking.getInstructions())
                    {
                        switch (instr.getType())
                        {
                            case RefPicMarking.InstrType.REMOVE_SHORT:
                                unrefShortTerm(instr.getArg1());
                                break;
                            case RefPicMarking.InstrType.REMOVE_LONG:
                                unrefLongTerm(instr.getArg1());
                                break;
                            case RefPicMarking.InstrType. CONVERT_INTO_LONG:
                                convert(instr.getArg1(), instr.getArg2());
                                break;
                            case RefPicMarking.InstrType.TRUNK_LONG:
                                truncateLongTerm(instr.getArg1() - 1);
                                break;
                            case RefPicMarking.InstrType.CLEAR:
                                clearAll();
                                break;
                            case RefPicMarking.InstrType.MARK_LONG:
                                saveLong(saved, instr.getArg1());
                                saved = null;
                                break;
                        }
                    }
                }
                if (saved != null)
                    saveShort(saved);

                int maxFrames = 1 << (activeSps.log2_max_frame_num_minus4 + 4);
                if (refPicMarking == null)
                {
                    int maxShort = Math.Max(1, activeSps.num_ref_frames - vdecoder.lRefs.Count());
                    int min = int.MaxValue, num = 0, minFn = 0;
                    for (int i = 0; i < vdecoder.sRefs.Length; i++)
                    {
                        if (vdecoder.sRefs[i] != null)
                        {
                            int fnWrap = unwrap(firstSliceHeader.frame_num, vdecoder.sRefs[i].getFrameNo(), maxFrames);
                            if (fnWrap < min)
                            {
                                min = fnWrap;
                                minFn = vdecoder.sRefs[i].getFrameNo();
                            }
                            num++;
                        }
                    }
                    if (num > maxShort)
                    {
                        // System.out.println("Removing: " + minFn + ", POC: " +
                        // sRefs[minFn].getPOC());
                        releaseRef(vdecoder.sRefs[minFn]);
                        vdecoder.sRefs[minFn] = null;
                    }
                }
            }

            private int unwrap(int thisFrameNo, int refFrameNo, int maxFrames)
            {
                return refFrameNo > thisFrameNo ? refFrameNo - maxFrames : refFrameNo;
            }

            private void saveShort(Frame saved)
            {
                vdecoder.sRefs[firstSliceHeader.frame_num] = saved;
            }

            private void saveLong(Frame saved, int longNo)
            {
                Frame prev = vdecoder.lRefs[longNo];
                if (prev != null)
                    releaseRef(prev);
                saved.setShortTerm(false);

                vdecoder.lRefs.Add(longNo, saved);
            }

            private void truncateLongTerm(int maxLongNo)
            {
                int[] keys = vdecoder.lRefs.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] > maxLongNo)
                    {
                        releaseRef(vdecoder.lRefs[keys[i]]);
                        vdecoder.lRefs.Remove(keys[i]);
                    }
                }
            }

            private void convert(int shortNo, int longNo)
            {
                int ind = SliceDecoder.wrap(firstSliceHeader.frame_num - shortNo,
                        1 << (firstSliceHeader.sps.log2_max_frame_num_minus4 + 4));
                releaseRef(vdecoder.lRefs[longNo]);
                vdecoder.lRefs.Add(longNo, vdecoder.sRefs[ind]);
                vdecoder.sRefs[ind] = null;
                vdecoder.lRefs[(longNo)].setShortTerm(false);
            }

            private void unrefLongTerm(int longNo)
            {
                releaseRef(vdecoder.lRefs[(longNo)]);
                vdecoder.lRefs.Remove(longNo);
            }

            private void unrefShortTerm(int shortNo)
            {
                int ind = SliceDecoder.wrap(firstSliceHeader.frame_num - shortNo,
                        1 << (firstSliceHeader.sps.log2_max_frame_num_minus4 + 4));
                releaseRef(vdecoder.sRefs[ind]);
                vdecoder.sRefs[ind] = null;
            }
        }

        public static Frame createFrame(SeqParameterSet sps, int[][] buffer, int frame_num, int[][][][] mvs,
                Frame[][][] refsUsed, int POC)
        {
            int width = sps.pic_width_in_mbs_minus1 + 1 << 4;
            int height = Utility.getPicHeightInMbs(sps) << 4;

            Rect crop = null;
            if (sps.frame_cropping_flag)
            {
                int sX = sps.frame_crop_left_offset << 1;
                int sY = sps.frame_crop_top_offset << 1;
                int w = width - (sps.frame_crop_right_offset << 1) - sX;
                int h = height - (sps.frame_crop_bottom_offset << 1) - sY;
                crop = new Rect(sX, sY, w, h);
            }
            return new Frame(width, height, buffer, ColorSpace.YUV420, crop, frame_num, mvs, refsUsed, POC);
        }

        public void addSps(List<MemoryStream> spsList)
        {
            foreach (var byteBuffer in spsList)
            {
                MemoryStream dup = byteBuffer.duplicate();
                Utility.unescapeNAL(dup);
                SeqParameterSet s = SeqParameterSet.read(dup);
                sps.Add(s.seq_parameter_set_id, s);
            }
        }

        public void addPps(List<MemoryStream> ppsList)
        {
            foreach (var byteBuffer in ppsList)
            {
                MemoryStream dup = byteBuffer.duplicate();
                Utility.unescapeNAL(dup);
                PictureParameterSet p = PictureParameterSet.read(dup);
                pps.Add(p.pic_parameter_set_id, p);
            }
        }


        public override int probe(MemoryStream data)
        {
            bool avalidSps = false, avalidPps = false, avalidSh = false;
            foreach (var nalUnit in Utility.splitFrame(data.duplicate()))
            {
                NALUnit marker = NALUnit.read(nalUnit);
                if (marker.type == NALUnitType.IDR_SLICE || marker.type == NALUnitType.NON_IDR_SLICE)
                {
                    BitReader reader = new BitReader(nalUnit);
                    avalidSh = validSh(new SliceHeaderReader().readPart1(reader));
                    break;
                }
                else if (marker.type == NALUnitType.SPS)
                {
                    avalidSps = validSps(SeqParameterSet.read(nalUnit));
                }
                else if (marker.type == NALUnitType.PPS)
                {
                    avalidPps = validPps(PictureParameterSet.read(nalUnit));
                }
            }

            return (avalidSh ? 60 : 0) + (avalidSps ? 20 : 0) + (avalidPps ? 20 : 0);
        }

        private bool validSh(SliceHeader sh)
        {
            return sh.first_mb_in_slice == 0 && sh.pic_parameter_set_id < 2;
        }

        private bool validSps(SeqParameterSet sps)
        {
            return sps.bit_depth_chroma_minus8 < 4 && sps.bit_depth_luma_minus8 < 4 && sps.chroma_format_idc != null
                    && sps.seq_parameter_set_id < 2 && sps.pic_order_cnt_type <= 2;
        }

        private bool validPps(PictureParameterSet pps)
        {
            return pps.pic_init_qp_minus26 <= 26 && pps.seq_parameter_set_id <= 2 && pps.pic_parameter_set_id <= 2;
        }

        public void setDebug(bool b)
        {
            this.debug = b;
        }
    }
}
