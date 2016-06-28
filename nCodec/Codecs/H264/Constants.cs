using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public static class PartPredExtensions
    {
        public static bool usesList(this nVideo.Codecs.H264.H264Const.PartPred p, int l)
        {
            return p == nVideo.Codecs.H264.H264Const.PartPred.Bi ? true : (p == nVideo.Codecs.H264.H264Const.PartPred.L0 && l == 0 || p == nVideo.Codecs.H264.H264Const.PartPred.L1 && l == 1);
        }
    }


    public class H264Const
    {

       
        public static VLC[] coeffToken = new VLC[10];
        public static VLC coeffTokenChromaDCY420;
        public static VLC coeffTokenChromaDCY422;
        public static VLC[] run;



        static H264Const()
        {
            run = new VLC[] {
                new VLCBuilder().set(0, "1").set(1, "0").getVLC(),
                new VLCBuilder().set(0, "1").set(1, "01").set(2, "00").getVLC(),
                new VLCBuilder().set(0, "11").set(1, "10").set(2, "01").set(3, "00").getVLC(),
                new VLCBuilder().set(0, "11").set(1, "10").set(2, "01").set(3, "001").set(4, "000").getVLC(),
                new VLCBuilder().set(0, "11").set(1, "10").set(2, "011").set(3, "010").set(4, "001").set(5, "000")
                        .getVLC(),
                new VLCBuilder().set(0, "11").set(1, "000").set(2, "001").set(3, "011").set(4, "010").set(5, "101")
                        .set(6, "100").getVLC(),
                new VLCBuilder().set(0, "111").set(1, "110").set(2, "101").set(3, "100").set(4, "011").set(5, "010")
                        .set(6, "001").set(7, "0001").set(8, "00001").set(9, "000001").set(10, "0000001")
                        .set(11, "00000001").set(12, "000000001").set(13, "0000000001").set(14, "00000000001").getVLC() };
        }

        public static VLC[] totalZeros16 = {

            new VLCBuilder().set(0, "1").set(1, "011").set(2, "010").set(3, "0011").set(4, "0010").set(5, "00011")
                    .set(6, "00010").set(7, "000011").set(8, "000010").set(9, "0000011").set(10, "0000010")
                    .set(11, "00000011").set(12, "00000010").set(13, "000000011").set(14, "000000010")
                    .set(15, "000000001").getVLC(),

            new VLCBuilder().set(0, "111").set(1, "110").set(2, "101").set(3, "100").set(4, "011").set(5, "0101")
                    .set(6, "0100").set(7, "0011").set(8, "0010").set(9, "00011").set(10, "00010").set(11, "000011")
                    .set(12, "000010").set(13, "000001").set(14, "000000").getVLC(),

            new VLCBuilder().set(0, "0101").set(1, "111").set(2, "110").set(3, "101").set(4, "0100").set(5, "0011")
                    .set(6, "100").set(7, "011").set(8, "0010").set(9, "00011").set(10, "00010").set(11, "000001")
                    .set(12, "00001").set(13, "000000").getVLC(),

            new VLCBuilder().set(0, "00011").set(1, "111").set(2, "0101").set(3, "0100").set(4, "110").set(5, "101")
                    .set(6, "100").set(7, "0011").set(8, "011").set(9, "0010").set(10, "00010").set(11, "00001")
                    .set(12, "00000").getVLC(),

            new VLCBuilder().set(0, "0101").set(1, "0100").set(2, "0011").set(3, "111").set(4, "110").set(5, "101")
                    .set(6, "100").set(7, "011").set(8, "0010").set(9, "00001").set(10, "0001").set(11, "00000")
                    .getVLC(),

            new VLCBuilder().set(0, "000001").set(1, "00001").set(2, "111").set(3, "110").set(4, "101").set(5, "100")
                    .set(6, "011").set(7, "010").set(8, "0001").set(9, "001").set(10, "000000").getVLC(),

            new VLCBuilder().set(0, "000001").set(1, "00001").set(2, "101").set(3, "100").set(4, "011").set(5, "11")
                    .set(6, "010").set(7, "0001").set(8, "001").set(9, "000000").getVLC(),

            new VLCBuilder().set(0, "000001").set(1, "0001").set(2, "00001").set(3, "011").set(4, "11").set(5, "10")
                    .set(6, "010").set(7, "001").set(8, "000000").getVLC(),

            new VLCBuilder().set(0, "000001").set(1, "000000").set(2, "0001").set(3, "11").set(4, "10").set(5, "001")
                    .set(6, "01").set(7, "00001").getVLC(),

            new VLCBuilder().set(0, "00001").set(1, "00000").set(2, "001").set(3, "11").set(4, "10").set(5, "01")
                    .set(6, "0001").getVLC(),

            new VLCBuilder().set(0, "0000").set(1, "0001").set(2, "001").set(3, "010").set(4, "1").set(5, "011")
                    .getVLC(),

            new VLCBuilder().set(0, "0000").set(1, "0001").set(2, "01").set(3, "1").set(4, "001").getVLC(),

            new VLCBuilder().set(0, "000").set(1, "001").set(2, "1").set(3, "01").getVLC(),

            new VLCBuilder().set(0, "00").set(1, "01").set(2, "1").getVLC(),

            new VLCBuilder().set(0, "0").set(1, "1").getVLC() };

        public static VLC[] totalZeros4 = { new VLCBuilder().set(0, "1").set(1, "01").set(2, "001").set(3, "000").getVLC(),

    new VLCBuilder().set(0, "1").set(1, "01").set(2, "00").getVLC(),

    new VLCBuilder().set(0, "1").set(1, "0").getVLC() };

        public static VLC[] totalZeros8 = {
            new VLCBuilder().set(0, "1").set(1, "010").set(2, "011").set(3, "0010").set(4, "0011").set(5, "0001")
                    .set(6, "00001").set(7, "00000").getVLC(),

            new VLCBuilder().set(0, "000").set(1, "01").set(2, "001").set(3, "100").set(4, "101").set(5, "110")
                    .set(6, "111").getVLC(),

            new VLCBuilder().set(0, "000").set(1, "001").set(2, "01").set(3, "10").set(4, "110").set(5, "111").getVLC(),

            new VLCBuilder().set(0, "110").set(1, "00").set(2, "01").set(3, "10").set(4, "111").getVLC(),

            new VLCBuilder().set(0, "00").set(1, "01").set(2, "10").set(3, "11").getVLC(),

            new VLCBuilder().set(0, "00").set(1, "01").set(2, "1").getVLC(),

            new VLCBuilder().set(0, "0").set(1, "1").getVLC() };

        public enum PartPred : int
        {
            L0 = 0, L1 = 1, Bi = 2, Direct = 3
        }



        public static int[][] bPredModes = new int[][]{ null,  new int[]{ (int)PartPred.L0 }, new int[]{ (int)PartPred.L1 }, new int[]{ (int)PartPred.Bi },
            new int []{ (int)PartPred.L0, (int)PartPred.L0 }, new int []{ (int)PartPred.L0, (int)PartPred.L0 }, new int []{ (int)PartPred.L1, (int)PartPred.L1 },
            new int []{ (int)(int)PartPred.L1, (int)PartPred.L1 }, new int []{ (int)PartPred.L0, (int)PartPred.L1 }, new int []{ (int)PartPred.L0, (int)PartPred.L1 },
            new int []{ (int)PartPred.L1, (int)PartPred.L0 }, new int []{(int) PartPred.L1, (int)PartPred.L0 }, new int []{ (int)PartPred.L0, (int)PartPred.Bi },
            new int []{ (int)PartPred.L0, (int)PartPred.Bi }, new int []{ (int)PartPred.L1, (int)PartPred.Bi }, new int []{ (int)PartPred.L1, (int)PartPred.Bi },
            new int []{ (int)PartPred.Bi, (int)PartPred.L0 }, new int []{ (int)PartPred.Bi, (int)PartPred.L0 }, new int []{ (int)PartPred.Bi, (int)PartPred.L1 },
            new int []{(int) PartPred.Bi, (int)PartPred.L1 }, new int []{ (int)PartPred.Bi, (int)PartPred.Bi }, new int []{ (int)PartPred.Bi, (int)PartPred.Bi } };

        public static MBType[] bMbTypes = { MBType.B_Direct_16x16, MBType.B_L0_16x16, MBType.B_L1_16x16, MBType.B_Bi_16x16,
            MBType.B_L0_L0_16x8, MBType.B_L0_L0_8x16, MBType.B_L1_L1_16x8, MBType.B_L1_L1_8x16, MBType.B_L0_L1_16x8,
            MBType.B_L0_L1_8x16, MBType.B_L1_L0_16x8, MBType.B_L1_L0_8x16, MBType.B_L0_Bi_16x8, MBType.B_L0_Bi_8x16,
            MBType.B_L1_Bi_16x8, MBType.B_L1_Bi_8x16, MBType.B_Bi_L0_16x8, MBType.B_Bi_L0_8x16, MBType.B_Bi_L1_16x8,
            MBType.B_Bi_L1_8x16, MBType.B_Bi_Bi_16x8, MBType.B_Bi_Bi_8x16, MBType.B_8x8 };

        public static int[] bPartW = { 0, 16, 16, 16, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8 };
        public static int[] bPartH = { 0, 16, 16, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16, 8, 16 };

        public static int[] BLK_X = new int[] { 0, 4, 0, 4, 8, 12, 8, 12, 0, 4, 0, 4, 8, 12, 8, 12 };
        public static int[] BLK_Y = new int[] { 0, 0, 4, 4, 0, 0, 4, 4, 8, 8, 12, 12, 8, 8, 12, 12 };

        public static int[] BLK_INV_MAP = { 0, 1, 4, 5, 2, 3, 6, 7, 8, 9, 12, 13, 10, 11, 14, 15 };

        public static int[] MB_BLK_OFF_LEFT = new int[] { 0, 1, 0, 1, 2, 3, 2, 3, 0, 1, 0, 1, 2, 3, 2, 3 };
        public static int[] MB_BLK_OFF_TOP = new int[] { 0, 0, 1, 1, 0, 0, 1, 1, 2, 2, 3, 3, 2, 2, 3, 3 };

        public static int[] QP_SCALE_CR = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 29, 30, 31, 32, 32, 33, 34, 34, 35, 35, 36, 36, 37, 37, 37, 38, 38, 38,
            39, 39, 39, 39 };

        public static Picture NO_PIC = new Picture(0, 0, null, null);
        public static int[] BLK_8x8_MB_OFF_LUMA = new int[] { 0, 8, 128, 136 };
        public static int[] BLK_8x8_MB_OFF_CHROMA = new int[] { 0, 4, 32, 36 };
        public static int[] BLK_4x4_MB_OFF_LUMA = new int[] { 0, 4, 8, 12, 64, 68, 72, 76, 128, 132, 136, 140, 192, 196, 200, 204 };
        public static int[] BLK_8x8_IND = new int[] { 0, 0, 1, 1, 0, 0, 1, 1, 2, 2, 3, 3, 2, 2, 3, 3 };
        public static int[][] BLK8x8_BLOCKS = new int[][]{
        new int []{0, 1, 4, 5},
        new int []{2, 3, 6, 7},
        new int []{8, 9, 12, 13},
        new int []{10, 11, 14, 15}
    };
        public static int[][] ARRAY = new int[][] { new int[] { 0 }, new int[] { 1 }, new int[] { 2 }, new int[] { 3 } };

        public static int[] CODED_BLOCK_PATTERN_INTRA_COLOR = new int[] { 47, 31, 15, 0, 23, 27, 29, 30, 7, 11, 13, 14, 39,
            43, 45, 46, 16, 3, 5, 10, 12, 19, 21, 26, 28, 35, 37, 42, 44, 1, 2, 4, 8, 17, 18, 20, 24, 6, 9, 22, 25, 32,
            33, 34, 36, 40, 38, 41 };

        public static int[] coded_block_pattern_intra_monochrome = new int[] { 15, 0, 7, 11, 13, 14, 3, 5, 10, 12, 1, 2, 4,
            8, 6, 9 };

        public static int[] CODED_BLOCK_PATTERN_INTER_COLOR = new int[] { 0, 16, 1, 2, 4, 8, 32, 3, 5, 10, 12, 15, 47, 7,
            11, 13, 14, 6, 9, 31, 35, 37, 42, 44, 33, 34, 36, 40, 39, 43, 45, 46, 17, 18, 20, 24, 19, 21, 26, 28, 23,
            27, 29, 30, 22, 25, 38, 41 };

        public static int[] coded_block_pattern_inter_monochrome = new int[] { 0, 1, 2, 4, 8, 3, 5, 10, 12, 15, 7, 11, 13,
            14, 6, 9 };

        public static int[] sig_coeff_map_8x8 = { 0, 1, 2, 3, 4, 5, 5, 4, 4, 3, 3, 4, 4, 4, 5, 5, 4, 4, 4, 4, 3, 3, 6, 7,
            7, 7, 8, 9, 10, 9, 8, 7, 7, 6, 11, 12, 13, 11, 6, 7, 8, 9, 14, 10, 9, 8, 6, 11, 12, 13, 11, 6, 9, 14, 10,
            9, 11, 12, 13, 11, 14, 10, 12 };

        public static int[] sig_coeff_map_8x8_mbaff = { 0, 1, 1, 2, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 4, 5, 6, 9, 10, 10, 8,
            11, 12, 11, 9, 9, 10, 10, 8, 11, 12, 11, 9, 9, 10, 10, 8, 11, 12, 11, 9, 9, 10, 10, 8, 13, 13, 9, 9, 10,
            10, 8, 13, 13, 9, 9, 10, 10, 14, 14, 14, 14, 14 };

        public static int[] last_sig_coeff_map_8x8 = { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7,
            7, 8, 8, 8 };

        public static int[] identityMapping16 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        public static int[] identityMapping4 = { 0, 1, 2, 3 };
        public static int[] bPartPredModes = new int[] { (int)PartPred.Direct, (int)PartPred.L0, (int)PartPred.L1, (int)PartPred.Bi, (int)PartPred.L0, (int)PartPred.L0, (int)PartPred.L1, (int)PartPred.L1, (int)PartPred.Bi, (int)PartPred.Bi, (int)PartPred.L0, (int)PartPred.L1, (int)PartPred.Bi };
        public static int[] bSubMbTypes = { 0, 0, 0, 0, 1, 2, 1, 2, 1, 2, 3, 3, 3 };
    }
}
