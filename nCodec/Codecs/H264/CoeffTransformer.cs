﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class CoeffTransformer {

    private int[] fieldScan4x4 = new int[]{ 0, 4, 1, 8, 12, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 };
    private int[] fieldScan8x8 = new int[]{ 0, 8, 16, 1, 9, 24, 32, 17, 2, 25, 40, 48, 56, 33, 10, 3, 18, 41, 49, 57, 26, 11, 4,
            19, 34, 42, 50, 58, 27, 12, 5, 20, 35, 43, 51, 58, 28, 13, 6, 21, 36, 44, 52, 60, 29, 14, 22, 37, 45, 53,
            61, 30, 7, 15, 38, 46, 54, 62, 23, 31, 39, 47, 55, 63 };

    public static int[] zigzag4x4 = new int[]{ 0, 1, 4, 8, 5, 2, 3, 6, 9, 12, 13, 10, 7, 11, 14, 15 };

    static int[][] dequantCoef = new int[][]{ new int[]{ 10, 13, 10, 13, 13, 16, 13, 16, 10, 13, 10, 13, 13, 16, 13, 16 },
            new int[]{ 11, 14, 11, 14, 14, 18, 14, 18, 11, 14, 11, 14, 14, 18, 14, 18 },
            new int[]{ 13, 16, 13, 16, 16, 20, 16, 20, 13, 16, 13, 16, 16, 20, 16, 20 },
            new int[]{ 14, 18, 14, 18, 18, 23, 18, 23, 14, 18, 14, 18, 18, 23, 18, 23 },
            new int[]{ 16, 20, 16, 20, 20, 25, 20, 25, 16, 20, 16, 20, 20, 25, 20, 25 },
           new int[] { 18, 23, 18, 23, 23, 29, 23, 29, 18, 23, 18, 23, 23, 29, 23, 29 } };

    static int[][] dequantCoef8x8 = (int[][])Array.CreateInstance(typeof(int), new int[]{6, 64});

    static int[][] initDequantCoeff8x8 = new int[][]{ new int[]{ 20, 18, 32, 19, 25, 24 }, new int[]{ 22, 19, 35, 21, 28, 26 },
            new int[]{ 26, 23, 42, 24, 33, 31 }, new int[]{ 28, 25, 45, 26, 35, 33 }, new int[]{ 32, 28, 51, 30, 40, 38 },
            new int[]{ 36, 32, 58, 34, 46, 43 } };

    public static int[] zigzag8x8 = new int[] { 0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33,
            40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44,
            51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63 };

    private static int[][] quantCoeff = new int[][]{
            new int[]{ 13107, 8066, 13107, 8066, 8066, 5243, 8066, 5243, 13107, 8066, 13107, 8066, 8066, 5243, 8066, 5243 },
            new int[]{ 11916, 7490, 11916, 7490, 7490, 4660, 7490, 4660, 11916, 7490, 11916, 7490, 7490, 4660, 7490, 4660 },
            new int[]{ 10082, 6554, 10082, 6554, 6554, 4194, 6554, 4194, 10082, 6554, 10082, 6554, 6554, 4194, 6554, 4194 },
            new int[]{ 9362, 5825, 9362, 5825, 5825, 3647, 5825, 3647, 9362, 5825, 9362, 5825, 5825, 3647, 5825, 3647 },
            new int[]{ 8192, 5243, 8192, 5243, 5243, 3355, 5243, 3355, 8192, 5243, 8192, 5243, 5243, 3355, 5243, 3355 },
            new int[]{ 7282, 4559, 7282, 4559, 4559, 2893, 4559, 2893, 7282, 4559, 7282, 4559, 4559, 2893, 4559, 2893 } };

    static CoeffTransformer() {
        for (int g = 0; g < 6; g++) {
            //Arrays.fill(dequantCoef8x8[g], initDequantCoeff8x8[g][5]);
            for (int i = 0; i < 8; i += 4)
                for (int j = 0; j < 8; j += 4)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][0];
            for (int i = 1; i < 8; i += 2)
                for (int j = 1; j < 8; j += 2)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][1];
            for (int i = 2; i < 8; i += 4)
                for (int j = 2; j < 8; j += 4)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][2];
            for (int i = 0; i < 8; i += 4)
                for (int j = 1; j < 8; j += 2)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][3];
            for (int i = 1; i < 8; i += 2)
                for (int j = 0; j < 8; j += 4)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][3];
            for (int i = 0; i < 8; i += 4)
                for (int j = 2; j < 8; j += 4)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][4];
            for (int i = 2; i < 8; i += 4)
                for (int j = 0; j < 8; j += 4)
                    dequantCoef8x8[g][(i << 3) + j] = initDequantCoeff8x8[g][4];
        }
    }

    public CoeffTransformer(int[][] scalingListMatrix) {
    }

    /**
     * Inverce integer DCT transform for 4x4 block
     * 
     * @param block
     * @return
     */
    public static void idct4x4(int[] block) {
        idct4x4(block, block);
    }

    public static void idct4x4(int[] block, int[] outs) {
        // Horisontal
        for (int i = 0; i < 16; i += 4) {
            int e0 = block[i] + block[i + 2];
            int e1 = block[i] - block[i + 2];
            int e2 = (block[i + 1] >> 1) - block[i + 3];
            int e3 = block[i + 1] + (block[i + 3] >> 1);

            outs[i] = e0 + e3;
            outs[i + 1] = e1 + e2;
            outs[i + 2] = e1 - e2;
            outs[i + 3] = e0 - e3;
        }

        // Vertical
        for (int i = 0; i < 4; i++) {
            int g0 = outs[i] + outs[i + 8];
            int g1 = outs[i] - outs[i + 8];
            int g2 = (outs[i + 4] >> 1) - outs[i + 12];
            int g3 = outs[i + 4] + (outs[i + 12] >> 1);
            outs[i] = g0 + g3;
            outs[i + 4] = g1 + g2;
            outs[i + 8] = g1 - g2;
            outs[i + 12] = g0 - g3;
        }

        // scale down
        for (int i = 0; i < 16; i++) {
            outs[i] = (outs[i] + 32) >> 6;
        }
    }

    public static void fdct4x4(int[] block) {
        // Horizontal
        for (int i = 0; i < 16; i += 4) {
            int t0 = block[i] + block[i + 3];
            int t1 = block[i + 1] + block[i + 2];
            int t2 = block[i + 1] - block[i + 2];
            int t3 = block[i] - block[i + 3];

            block[i] = t0 + t1;
            block[i + 1] = (t3 << 1) + t2;
            block[i + 2] = t0 - t1;
            block[i + 3] = t3 - (t2 << 1);
        }

        // Vertical
        for (int i = 0; i < 4; i++) {
            int t0 = block[i] + block[i + 12];
            int t1 = block[i + 4] + block[i + 8];
            int t2 = block[i + 4] - block[i + 8];
            int t3 = block[i] - block[i + 12];

            block[i] = t0 + t1;
            block[i + 4] = t2 + (t3 << 1);
            block[i + 8] = t0 - t1;
            block[i + 12] = t3 - (t2 << 1);
        }
    }

    /**
     * Inverse Hadamard transform
     * 
     * @param scaled
     */
    public static void invDC4x4(int[] scaled) {
        // Horisontal
        for (int i = 0; i < 16; i += 4) {
            int e0 = scaled[i] + scaled[i + 2];
            int e1 = scaled[i] - scaled[i + 2];
            int e2 = scaled[i + 1] - scaled[i + 3];
            int e3 = scaled[i + 1] + scaled[i + 3];

            scaled[i] = e0 + e3;
            scaled[i + 1] = e1 + e2;
            scaled[i + 2] = e1 - e2;
            scaled[i + 3] = e0 - e3;
        }

        // Vertical
        for (int i = 0; i < 4; i++) {
            int g0 = scaled[i] + scaled[i + 8];
            int g1 = scaled[i] - scaled[i + 8];
            int g2 = scaled[i + 4] - scaled[i + 12];
            int g3 = scaled[i + 4] + scaled[i + 12];
            scaled[i] = g0 + g3;
            scaled[i + 4] = g1 + g2;
            scaled[i + 8] = g1 - g2;
            scaled[i + 12] = g0 - g3;
        }
    }

    /**
     * Forward Hadamard transform
     * 
     * @param scaled
     */
    public static void fvdDC4x4(int[] scaled) {
        // Horizontal
        for (int i = 0; i < 16; i += 4) {
            int t0 = scaled[i] + scaled[i + 3];
            int t1 = scaled[i + 1] + scaled[i + 2];
            int t2 = scaled[i + 1] - scaled[i + 2];
            int t3 = scaled[i] - scaled[i + 3];

            scaled[i] = t0 + t1;
            scaled[i + 1] = t3 + t2;
            scaled[i + 2] = t0 - t1;
            scaled[i + 3] = t3 - t2;
        }

        // Vertical
        for (int i = 0; i < 4; i++) {
            int t0 = scaled[i] + scaled[i + 12];
            int t1 = scaled[i + 4] + scaled[i + 8];
            int t2 = scaled[i + 4] - scaled[i + 8];
            int t3 = scaled[i] - scaled[i + 12];

            scaled[i] = (t0 + t1) >> 1;
            scaled[i + 4] = (t2 + t3) >> 1;
            scaled[i + 8] = (t0 - t1) >> 1;
            scaled[i + 12] = (t3 - t2) >> 1;
        }
    }

    public static void dequantizeAC(int[] coeffs, int qp) {
        int group = qp % 6;

        if (qp >= 24) {
            int qbits = qp / 6;
            for (int i = 0; i < 16; i++)
                coeffs[i] = (coeffs[i] * dequantCoef[group][i]) << qbits;
        } else {
            int qbits = 4 - qp / 6;
            int addition = 1 << (3 - qp / 6);
            for (int i = 0; i < 16; i++)
                coeffs[i] = (coeffs[i] * (dequantCoef[group][i] << 4) + addition) >> qbits;
        }
    }

    public static void quantizeAC(int[] coeffs, int qp) {
        int level = qp / 6;
        int offset = qp % 6;

        int addition = 682 << (qp / 6 + 4);
        int qbits = 15 + level;

        if (qp < 10) {
            for (int i = 1; i < 16; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (Math.Min((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][i] + addition) >> qbits, 2063) ^ sign)
                        - sign;
            }
        } else {
            for (int i = 1; i < 16; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][i] + addition) >> qbits) ^ sign) - sign;
            }
        }
    }

    public static int[] unzigzagAC(int[] coeffs) {
        int[] tab;
        if (coeffs.Length == 16) {
            tab = zigzag4x4;
        } else if (coeffs.Length == 64) {
            tab = zigzag8x8;
        } else
            throw new ArgumentException("Coefficients array should be of either 16 or 64 length.");

        int[] result = new int[coeffs.Length];
        for (int i = 0; i < coeffs.Length; i++) {
            result[tab[i]] = coeffs[i];
        }

        return result;
    }

    public static void dequantizeDC4x4(int[] coeffs, int qp) {
        int group = qp % 6;

        if (qp >= 36) {
            int qbits = qp / 6 - 2;
            for (int i = 0; i < 16; i++)
                coeffs[i] = (coeffs[i] * dequantCoef[group][0]) << qbits;
        } else {
            int qbits = 6 - qp / 6;
            int addition = 1 << (5 - qp / 6);
            for (int i = 0; i < 16; i++)
                coeffs[i] = (coeffs[i] * (dequantCoef[group][0] << 4) + addition) >> qbits;
        }
    }

    public static void quantizeDC4x4(int[] coeffs, int qp) {

        int level = qp / 6;
        int offset = qp % 6;

        int addition = 682 << (qp / 6 + 5);
        int qbits = 16 + level;

        if (qp < 10) {
            for (int i = 0; i < 16; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (Math.Min((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][0] + addition) >> qbits, 2063) ^ sign)
                        - sign;
            }
        } else {
            for (int i = 0; i < 16; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][0] + addition) >> qbits) ^ sign) - sign;
            }
        }
    }

    /**
     * Inverse Hadamard 2x2
     * 
     * @param block
     */
    public static void invDC2x2(int[] block) {
        int t0, t1, t2, t3;

        t0 = block[0] + block[1];
        t1 = block[0] - block[1];
        t2 = block[2] + block[3];
        t3 = block[2] - block[3];

        block[0] = (t0 + t2);
        block[1] = (t1 + t3);
        block[2] = (t0 - t2);
        block[3] = (t1 - t3);
    }

    /**
     * Forward Hadamard 2x2
     * 
     * @param dc2
     */
    public static void fvdDC2x2(int[] block) {
        invDC2x2(block);
    }

    public static void dequantizeDC2x2(int[] transformed, int qp) {

        int group = qp % 6;
        int shift = qp / 6;

        for (int i = 0; i < 4; i++) {
            transformed[i] = ((transformed[i] * dequantCoef[group][0]) << shift) >> 1;
        }
    }

    public static void quantizeDC2x2(int[] coeffs, int qp) {

        int level = qp / 6;
        int offset = qp % 6;

        int addition = 682 << (qp / 6 + 5);
        int qbits = 16 + level;

        if (qp < 4) {
            for (int i = 0; i < 4; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (Math.Min((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][0] + addition) >> qbits, 2063) ^ sign)
                        - sign;
            }
        } else {
            for (int i = 0; i < 4; i++) {
                int sign = (coeffs[i] >> 31);
                coeffs[i] = (((((coeffs[i] ^ sign) - sign) * quantCoeff[offset][0] + addition) >> qbits) ^ sign) - sign;
            }
        }
    }

    public static void reorderDC4x4(int[] dc) {

        var t = dc[2];

        dc[2] = dc[4];

        dc[4] = 2;

        t = dc[3];

        dc[3] = dc[5];

        dc[5] = 2;

        t = dc[10];

        dc[10] = dc[12];

        dc[12] = t;
        
        t = dc[11];

        dc[11] = dc[13];

        dc[13] = t;
    }

    public static void fvdDC4x2(int[] dc) {

    }

    public static void quantizeDC4x2(int[] dc, int qp) {

    }

    public static void invDC4x2(int[] dc) {
        // TODO Auto-generated method stub

    }

    public static void dequantizeDC4x2(int[] dc, int qp) {
        // TODO Auto-generated method stub

    }

    /**
     * Coefficients are <<4 on exit
     * 
     * @param coeffs
     * @param qp
     */
    public static void dequantizeAC8x8(int[] coeffs, int qp) {
        int group = qp % 6;

        if (qp >= 36) {
            int qbits = qp / 6 - 2;
            for (int i = 0; i < 64; i++)
                coeffs[i] = (coeffs[i] * dequantCoef8x8[group][i]) << qbits;
        } else {
            int qbits = 6 - qp / 6;
            int addition = 1 << (5 - qp / 6);
            for (int i = 0; i < 64; i++)
                coeffs[i] = (coeffs[i] * (dequantCoef8x8[group][i] << 4) + addition) >> qbits;
        }
    }

    public static void idct8x8(int[] ac) {
        int off = 0;

        // Horizontal
        for (int row = 0; row < 8; row++) {
            int e0 = ac[off] + ac[off + 4];
            int e1 = -ac[off + 3] + ac[off + 5] - ac[off + 7] - (ac[off + 7] >> 1);
            int e2 = ac[off] - ac[off + 4];
            int e3 = ac[off + 1] + ac[off + 7] - ac[off + 3] - (ac[off + 3] >> 1);
            int e4 = (ac[off + 2] >> 1) - ac[off + 6];
            int e5 = -ac[off + 1] + ac[off + 7] + ac[off + 5] + (ac[off + 5] >> 1);
            int e6 = ac[off + 2] + (ac[off + 6] >> 1);
            int e7 = ac[off + 3] + ac[off + 5] + ac[off + 1] + (ac[off + 1] >> 1);

            int f0 = e0 + e6;
            int f1 = e1 + (e7 >> 2);
            int f2 = e2 + e4;
            int f3 = e3 + (e5 >> 2);
            int f4 = e2 - e4;
            int f5 = (e3 >> 2) - e5;
            int f6 = e0 - e6;
            int f7 = e7 - (e1 >> 2);

            ac[off] = f0 + f7;
            ac[off + 1] = f2 + f5;
            ac[off + 2] = f4 + f3;
            ac[off + 3] = f6 + f1;
            ac[off + 4] = f6 - f1;
            ac[off + 5] = f4 - f3;
            ac[off + 6] = f2 - f5;
            ac[off + 7] = f0 - f7;

            off += 8;
        }

        // Vertical
        for (int col = 0; col < 8; col++) {
            int e0 = ac[col] + ac[col + 32];
            int e1 = -ac[col + 24] + ac[col + 40] - ac[col + 56] - (ac[col + 56] >> 1);
            int e2 = ac[col] - ac[col + 32];
            int e3 = ac[col + 8] + ac[col + 56] - ac[col + 24] - (ac[col + 24] >> 1);
            int e4 = (ac[col + 16] >> 1) - ac[col + 48];
            int e5 = -ac[col + 8] + ac[col + 56] + ac[col + 40] + (ac[col + 40] >> 1);
            int e6 = ac[col + 16] + (ac[col + 48] >> 1);
            int e7 = ac[col + 24] + ac[col + 40] + ac[col + 8] + (ac[col + 8] >> 1);

            int f0 = e0 + e6;
            int f1 = e1 + (e7 >> 2);
            int f2 = e2 + e4;
            int f3 = e3 + (e5 >> 2);
            int f4 = e2 - e4;
            int f5 = (e3 >> 2) - e5;
            int f6 = e0 - e6;
            int f7 = e7 - (e1 >> 2);

            ac[col] = f0 + f7;
            ac[col + 8] = f2 + f5;
            ac[col + 16] = f4 + f3;
            ac[col + 24] = f6 + f1;
            ac[col + 32] = f6 - f1;
            ac[col + 40] = f4 - f3;
            ac[col + 48] = f2 - f5;
            ac[col + 56] = f0 - f7;
        }

        // scale down
        for (int i = 0; i < 64; i++) {
            ac[i] = (ac[i] + 32) >> 6;
        }
    }
}
}
