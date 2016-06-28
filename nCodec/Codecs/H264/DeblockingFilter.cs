﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class DeblockingFilter
    {

        static int[] alphaTab = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 5, 6, 7, 8, 9, 10, 12,
            13, 15, 17, 20, 22, 25, 28, 32, 36, 40, 45, 50, 56, 63, 71, 80, 90, 101, 113, 127, 144, 162, 182, 203, 226,
            255, 255 };
        static int[] betaTab = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 6,
            6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18 };

        static int[][] tcs = new int[][] {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                    1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 6, 6, 7, 8, 9, 10, 11, 13 },

            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2,
                    2, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 7, 8, 8, 10, 11, 12, 13, 15, 17 },

            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3,
                    3, 3, 4, 4, 4, 5, 6, 6, 7, 8, 9, 10, 11, 13, 14, 16, 18, 20, 23, 25 } };

        private int[][] nCoeff;
        private int[][][][] mvs;
        private MBType[] mbTypes;
        private int[][] mbQps;
        private SliceHeader[] shs;
        private bool[] tr8x8Used;
        private Picture[][][] refsUsed;

        public DeblockingFilter(int bitDepthLuma, int bitDepthChroma, int[][] nCoeff, int[][][][] mvs, MBType[] mbTypes,
                int[][] mbQps, SliceHeader[] shs, bool[] tr8x8Used, Picture[][][] refsUsed)
        {
            this.nCoeff = nCoeff;
            this.mvs = mvs;
            this.mbTypes = mbTypes;
            this.mbQps = mbQps;
            this.shs = shs;
            this.tr8x8Used = tr8x8Used;
            this.refsUsed = refsUsed;
        }

        public void deblockFrame(Picture result) {
        ColorSpace color = result.getColor();
        // for (int i = 0; i < shs.Length; i++)
        // printMB(result.getPlaneData(2), result.getPlaneWidth(2), i, shs[i],
        // "!--!--!--!--!--!--!--!--!--!--!--!");
//        printMB(result.getPlaneData(0), result.getPlaneWidth(0), 0, shs[0], "!--!--!--!--!--!--!--!--!--!--!--!");
        int[][] bsV = (int[][])Array.CreateInstance(typeof(int), new int[] { 4, 4 }), bsH = (int[][])Array.CreateInstance(typeof(int), new int[] { 4, 4 });
        for (int i = 0; i < shs.Length; i++) {
            calcBsH(result, i, bsH);
            calcBsV(result, i, bsV);
            for (int c = 0; c < color.nComp; c++) {
                fillVerticalEdge(result, c, i, bsV);
                fillHorizontalEdge(result, c, i, bsH);
                // printMB(result.getPlaneData(1), result.getPlaneWidth(1), i,
                // shs[i],
                // "!**!**!**!**!--!--!--!--!--!--!--!");
            }
        }
        // printMB(result.getPlaneData(0), result.getPlaneWidth(0), 235,
        // shs[235], "!**!**!**!**!--!--!--!--!--!--!--!");
    }

        //    private void printMB(int[] is, int stride, int mbAddr, SliceHeader sh, String delim) {
        //        int mbWidth = sh.sps.pic_width_in_mbs_minus1 + 1;
        //        int mbX = mbAddr % mbWidth;
        //        int mbY = mbAddr / mbWidth;
        //
        //        System.out.println("MB: " + mbX + ", " + mbY);
        //        System.out.println(delim);
        //        for (int j = 0; j < 16; j++) {
        //            for (int i = 0; i < 16; i++)
        //                System.out.print(String.format("%3d,", is[((mbY << 4) + j) * stride + (mbX << 4) + i]));
        //            System.out.println();
        //        }
        //        System.out.println(delim);
        //    }

        static int[] inverse = new int[] { 0, 1, 4, 5, 2, 3, 6, 7, 8, 9, 12, 13, 10, 11, 14, 15 };

        private int calcBoundaryStrenth(bool atMbBoundary, bool leftIntra, bool rightIntra, int leftCoeff,
                int rightCoeff, int[] mvA0, int[] mvB0, int[] mvA1, int[] mvB1, int mbAddrA, int mbAddrB)
        {

            if (atMbBoundary && (leftIntra || rightIntra))
                return 4;
            else if (leftIntra || rightIntra)
                return 3;
            else
            {

                if (leftCoeff > 0 || rightCoeff > 0)
                    return 2;

                int nA = (mvA0[2] == -1 ? 0 : 1) + (mvA1[2] == -1 ? 0 : 1);
                int nB = (mvB0[2] == -1 ? 0 : 1) + (mvB1[2] == -1 ? 0 : 1);

                if (nA != nB)
                    return 1;

                Picture ra0 = mvA0[2] < 0 ? null : refsUsed[mbAddrA][0][mvA0[2]];
                Picture ra1 = mvA1[2] < 0 ? null : refsUsed[mbAddrA][1][mvA1[2]];

                Picture rb0 = mvB0[2] < 0 ? null : refsUsed[mbAddrB][0][mvB0[2]];
                Picture rb1 = mvB1[2] < 0 ? null : refsUsed[mbAddrB][1][mvB1[2]];

                if (ra0 != rb0 && ra0 != rb1 || ra1 != rb0 && ra1 != rb1 || rb0 != ra0 && rb0 != ra1 || rb1 != ra0
                        && rb1 != ra1)
                    return 1;

                if (ra0 == ra1 && ra1 == rb0 && rb0 == rb1)
                {
                    return ra0 != null
                            && (mvThresh(mvA0, mvB0) || mvThresh(mvA1, mvB0) || mvThresh(mvA0, mvB1) || mvThresh(mvA1, mvB1)) ? 1
                            : 0;
                }
                else if (ra0 == rb0 && ra1 == rb1)
                {
                    return ra0 != null && mvThresh(mvA0, mvB0) || ra1 != null && mvThresh(mvA1, mvB1) ? 1 : 0;
                }
                else if (ra0 == rb1 && ra1 == rb0)
                {
                    return ra0 != null && mvThresh(mvA0, mvB1) || ra1 != null && mvThresh(mvA1, mvB0) ? 1 : 0;
                }
            }

            return 0;
        }

        private bool mvThresh(int[] v0, int[] v1)
        {
            return Math.Abs(v0[0] - v1[0]) >= 4 || Math.Abs(v0[1] - v1[1]) >= 4;
        }

        private static int getIdxBeta(int sliceBetaOffset, int avgQp)
        {
            return CABAC.clip(avgQp + sliceBetaOffset, 0, 51);
        }

        private static int getIdxAlpha(int sliceAlphaC0Offset, int avgQp)
        {
            return CABAC.clip(avgQp + sliceAlphaC0Offset, 0, 51);
        }

        private void calcBsH(Picture pic, int mbAddr, int[][] bs)
        {
            SliceHeader sh = shs[mbAddr];
            int mbWidth = sh.sps.pic_width_in_mbs_minus1 + 1;

            int mbX = mbAddr % mbWidth;
            int mbY = mbAddr / mbWidth;

            bool topAvailable = mbY > 0 && (sh.disable_deblocking_filter_idc != 2 || shs[mbAddr - mbWidth] == sh);
            bool thisIntra = mbTypes[mbAddr] != null && mbTypes[mbAddr].isIntra();

            if (topAvailable)
            {
                bool topIntra = mbTypes[mbAddr - mbWidth] != null && mbTypes[mbAddr - mbWidth].isIntra();
                for (int blkX = 0; blkX < 4; blkX++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2);

                    bs[0][blkX] = calcBoundaryStrenth(true, topIntra, thisIntra, nCoeff[thisBlkY][thisBlkX],
                            nCoeff[thisBlkY - 1][thisBlkX], mvs[0][thisBlkY][thisBlkX], mvs[0][thisBlkY - 1][thisBlkX],
                            mvs[1][thisBlkY][thisBlkX], mvs[1][thisBlkY - 1][thisBlkX], mbAddr, mbAddr - mbWidth);

                }
            }

            for (int blkY = 1; blkY < 4; blkY++)
            {
                for (int blkX = 0; blkX < 4; blkX++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2) + blkY;

                    bs[blkY][blkX] = calcBoundaryStrenth(false, thisIntra, thisIntra, nCoeff[thisBlkY][thisBlkX],
                            nCoeff[thisBlkY - 1][thisBlkX], mvs[0][thisBlkY][thisBlkX], mvs[0][thisBlkY - 1][thisBlkX],
                            mvs[1][thisBlkY][thisBlkX], mvs[1][thisBlkY - 1][thisBlkX], mbAddr, mbAddr);
                }
            }
        }

        private void fillHorizontalEdge(Picture pic, int comp, int mbAddr, int[][] bs)
        {
            SliceHeader sh = shs[mbAddr];
            int mbWidth = sh.sps.pic_width_in_mbs_minus1 + 1;

            int alpha = sh.slice_alpha_c0_offset_div2 << 1;
            int beta = sh.slice_beta_offset_div2 << 1;

            int mbX = mbAddr % mbWidth;
            int mbY = mbAddr / mbWidth;

            bool topAvailable = mbY > 0 && (sh.disable_deblocking_filter_idc != 2 || shs[mbAddr - mbWidth] == sh);
            int curQp = mbQps[comp][mbAddr];

            int cW = 2 - pic.getColor().compWidth[comp];
            int cH = 2 - pic.getColor().compHeight[comp];
            if (topAvailable)
            {
                int topQp = mbQps[comp][mbAddr - mbWidth];
                int avgQp = (topQp + curQp + 1) >> 1;
                for (int blkX = 0; blkX < 4; blkX++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2);

                    filterBlockEdgeHoris(pic, comp, thisBlkX << cW, thisBlkY << cH, getIdxAlpha(alpha, avgQp),
                            getIdxBeta(beta, avgQp), bs[0][blkX], 1 << cW);
                }
            }

            bool skip4x4 = comp == 0 && tr8x8Used[mbAddr] || cH == 1;

            for (int blkY = 1; blkY < 4; blkY++)
            {
                if (skip4x4 && (blkY & 1) == 1)
                    continue;

                for (int blkX = 0; blkX < 4; blkX++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2) + blkY;

                    filterBlockEdgeHoris(pic, comp, thisBlkX << cW, thisBlkY << cH, getIdxAlpha(alpha, curQp),
                            getIdxBeta(beta, curQp), bs[blkY][blkX], 1 << cW);
                }
            }
        }

        private void calcBsV(Picture pic, int mbAddr, int[][] bs)
        {

            SliceHeader sh = shs[mbAddr];
            int mbWidth = sh.sps.pic_width_in_mbs_minus1 + 1;

            int mbX = mbAddr % mbWidth;
            int mbY = mbAddr / mbWidth;

            bool leftAvailable = mbX > 0 && (sh.disable_deblocking_filter_idc != 2 || shs[mbAddr - 1] == sh);
            bool thisIntra = mbTypes[mbAddr] != null && mbTypes[mbAddr].isIntra();

            if (leftAvailable)
            {
                bool leftIntra = mbTypes[mbAddr - 1] != null && mbTypes[mbAddr - 1].isIntra();
                for (int blkY = 0; blkY < 4; blkY++)
                {
                    int thisBlkX = (mbX << 2);
                    int thisBlkY = (mbY << 2) + blkY;
                    bs[blkY][0] = calcBoundaryStrenth(true, leftIntra, thisIntra, nCoeff[thisBlkY][thisBlkX],
                            nCoeff[thisBlkY][thisBlkX - 1], mvs[0][thisBlkY][thisBlkX], mvs[0][thisBlkY][thisBlkX - 1],
                            mvs[1][thisBlkY][thisBlkX], mvs[1][thisBlkY][thisBlkX - 1], mbAddr, mbAddr - 1);
                }
            }

            for (int blkX = 1; blkX < 4; blkX++)
            {
                for (int blkY = 0; blkY < (1 << 2); blkY++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2) + blkY;
                    bs[blkY][blkX] = calcBoundaryStrenth(false, thisIntra, thisIntra, nCoeff[thisBlkY][thisBlkX],
                            nCoeff[thisBlkY][thisBlkX - 1], mvs[0][thisBlkY][thisBlkX], mvs[0][thisBlkY][thisBlkX - 1],
                            mvs[1][thisBlkY][thisBlkX], mvs[1][thisBlkY][thisBlkX - 1], mbAddr, mbAddr);
                }
            }
        }

        private void fillVerticalEdge(Picture pic, int comp, int mbAddr, int[][] bs)
        {

            SliceHeader sh = shs[mbAddr];
            int mbWidth = sh.sps.pic_width_in_mbs_minus1 + 1;

            int alpha = sh.slice_alpha_c0_offset_div2 << 1;
            int beta = sh.slice_beta_offset_div2 << 1;

            int mbX = mbAddr % mbWidth;
            int mbY = mbAddr / mbWidth;

            bool leftAvailable = mbX > 0 && (sh.disable_deblocking_filter_idc != 2 || shs[mbAddr - 1] == sh);
            int curQp = mbQps[comp][mbAddr];

            int cW = 2 - pic.getColor().compWidth[comp];
            int cH = 2 - pic.getColor().compHeight[comp];
            if (leftAvailable)
            {
                int leftQp = mbQps[comp][mbAddr - 1];
                int avgQpV = (leftQp + curQp + 1) >> 1;
                for (int blkY = 0; blkY < 4; blkY++)
                {
                    int thisBlkX = (mbX << 2);
                    int thisBlkY = (mbY << 2) + blkY;
                    filterBlockEdgeVert(pic, comp, thisBlkX << cW, thisBlkY << cH, getIdxAlpha(alpha, avgQpV),
                            getIdxBeta(beta, avgQpV), bs[blkY][0], 1 << cH);
                }
            }
            bool skip4x4 = comp == 0 && tr8x8Used[mbAddr] || cW == 1;

            for (int blkX = 1; blkX < 4; blkX++)
            {
                if (skip4x4 && (blkX & 1) == 1)
                    continue;
                for (int blkY = 0; blkY < 4; blkY++)
                {
                    int thisBlkX = (mbX << 2) + blkX;
                    int thisBlkY = (mbY << 2) + blkY;
                    filterBlockEdgeVert(pic, comp, thisBlkX << cW, thisBlkY << cH, getIdxAlpha(alpha, curQp),
                            getIdxBeta(beta, curQp), bs[blkY][blkX], 1 << cH);
                }
            }
        }

        private void filterBlockEdgeHoris(Picture pic, int comp, int x, int y, int indexAlpha, int indexBeta, int bs,
                int blkW)
        {

            int stride = pic.getPlaneWidth(comp);
            int offset = y * stride + x;

            for (int pixOff = 0; pixOff < blkW; pixOff++)
            {
                int p2Idx = offset - 3 * stride + pixOff;
                int p1Idx = offset - 2 * stride + pixOff;
                int p0Idx = offset - stride + pixOff;
                int q0Idx = offset + pixOff;
                int q1Idx = offset + stride + pixOff;
                int q2Idx = offset + 2 * stride + pixOff;

                if (bs == 4)
                {
                    int p3Idx = offset - 4 * stride + pixOff;
                    int q3Idx = offset + 3 * stride + pixOff;

                    filterBs4(indexAlpha, indexBeta, pic.getPlaneData(comp), p3Idx, p2Idx, p1Idx, p0Idx, q0Idx, q1Idx,
                            q2Idx, q3Idx, comp != 0);
                }
                else if (bs > 0)
                {

                    filterBs(bs, indexAlpha, indexBeta, pic.getPlaneData(comp), p2Idx, p1Idx, p0Idx, q0Idx, q1Idx, q2Idx,
                            comp != 0);
                }
            }
        }

        private void filterBlockEdgeVert(Picture pic, int comp, int x, int y, int indexAlpha, int indexBeta, int bs,
                int blkH)
        {

            int stride = pic.getPlaneWidth(comp);
            for (int i = 0; i < blkH; i++)
            {
                int offsetQ = (y + i) * stride + x;
                int p2Idx = offsetQ - 3;
                int p1Idx = offsetQ - 2;
                int p0Idx = offsetQ - 1;
                int q0Idx = offsetQ;
                int q1Idx = offsetQ + 1;
                int q2Idx = offsetQ + 2;

                if (bs == 4)
                {
                    int p3Idx = offsetQ - 4;
                    int q3Idx = offsetQ + 3;
                    filterBs4(indexAlpha, indexBeta, pic.getPlaneData(comp), p3Idx, p2Idx, p1Idx, p0Idx, q0Idx, q1Idx,
                            q2Idx, q3Idx, comp != 0);
                }
                else if (bs > 0)
                {
                    filterBs(bs, indexAlpha, indexBeta, pic.getPlaneData(comp), p2Idx, p1Idx, p0Idx, q0Idx, q1Idx, q2Idx,
                            comp != 0);
                }
            }
        }

        private void filterBs(int bs, int indexAlpha, int indexBeta, int[] pels, int p2Idx, int p1Idx, int p0Idx,
                int q0Idx, int q1Idx, int q2Idx, bool isChroma)
        {

            int p1 = pels[p1Idx];
            int p0 = pels[p0Idx];
            int q0 = pels[q0Idx];
            int q1 = pels[q1Idx];

            int alphaThresh = alphaTab[indexAlpha];
            int betaThresh = betaTab[indexBeta];

            bool filterEnabled = Math.Abs(p0 - q0) < alphaThresh && Math.Abs(p1 - p0) < betaThresh && Math.Abs(q1 - q0) < betaThresh;

            if (!filterEnabled)
                return;

            // System.out.printf("%h %h %h %h %h %h %h %h\n", q3, q2, q1, q0, p0,
            // p1, p2, p3);

            int tC0 = tcs[bs - 1][indexAlpha];

            bool conditionP, conditionQ;
            int tC;
            if (!isChroma)
            {
                int ap = Math.Abs(pels[p2Idx] - p0);
                int aq = Math.Abs(pels[q2Idx] - q0);
                tC = tC0 + ((ap < betaThresh) ? 1 : 0) + ((aq < betaThresh) ? 1 : 0);
                conditionP = ap < betaThresh;
                conditionQ = aq < betaThresh;
            }
            else
            {
                tC = tC0 + 1;
                conditionP = false;
                conditionQ = false;
            }

            int sigma = ((((q0 - p0) << 2) + (p1 - q1) + 4) >> 3);
            sigma = sigma < -tC ? -tC : (sigma > tC ? tC : sigma);

            int p0n = p0 + sigma;
            p0n = p0n < 0 ? 0 : p0n;
            int q0n = q0 - sigma;
            q0n = q0n < 0 ? 0 : q0n;

            if (conditionP)
            {
                int p2 = pels[p2Idx];

                int diff = (p2 + ((p0 + q0 + 1) >> 1) - (p1 << 1)) >> 1;
                diff = diff < -tC0 ? -tC0 : (diff > tC0 ? tC0 : diff);
                int p1n = p1 + diff;
                pels[p1Idx] = CABAC.clip(p1n, 0, 255);
            }

            if (conditionQ)
            {
                int q2 = pels[q2Idx];
                int diff = (q2 + ((p0 + q0 + 1) >> 1) - (q1 << 1)) >> 1;
                diff = diff < -tC0 ? -tC0 : (diff > tC0 ? tC0 : diff);
                int q1n = q1 + diff;
                pels[q1Idx] = CABAC.clip(q1n, 0, 255);
            }

            pels[q0Idx] = CABAC.clip(q0n, 0, 255);
            pels[p0Idx] = CABAC.clip(p0n, 0, 255);

        }

        private void filterBs4(int indexAlpha, int indexBeta, int[] pels, int p3Idx, int p2Idx, int p1Idx, int p0Idx,
                int q0Idx, int q1Idx, int q2Idx, int q3Idx, bool isChroma)
        {
            int p0 = pels[p0Idx];
            int q0 = pels[q0Idx];
            int p1 = pels[p1Idx];
            int q1 = pels[q1Idx];

            int alphaThresh = alphaTab[indexAlpha];
            int betaThresh = betaTab[indexBeta];

            bool filterEnabled = Math.Abs(p0 - q0) < alphaThresh && Math.Abs(p1 - p0) < betaThresh && Math.Abs(q1 - q0) < betaThresh;

            if (!filterEnabled)
                return;

            bool conditionP, conditionQ;

            if (isChroma)
            {
                conditionP = false;
                conditionQ = false;
            }
            else
            {
                int ap = Math.Abs(pels[p2Idx] - p0);
                int aq = Math.Abs(pels[q2Idx] - q0);

                conditionP = ap < betaThresh && Math.Abs(p0 - q0) < ((alphaThresh >> 2) + 2);
                conditionQ = aq < betaThresh && Math.Abs(p0 - q0) < ((alphaThresh >> 2) + 2);

            }

            if (conditionP)
            {
                int p3 = pels[p3Idx];
                int p2 = pels[p2Idx];

                int p0n = (p2 + 2 * p1 + 2 * p0 + 2 * q0 + q1 + 4) >> 3;
                int p1n = (p2 + p1 + p0 + q0 + 2) >> 2;
                int p2n = (2 * p3 + 3 * p2 + p1 + p0 + q0 + 4) >> 3;
                pels[p0Idx] = CABAC.clip(p0n, 0, 255);
                pels[p1Idx] = CABAC.clip(p1n, 0, 255);
                pels[p2Idx] = CABAC.clip(p2n, 0, 255);
            }
            else
            {
                int p0n = (2 * p1 + p0 + q1 + 2) >> 2;
                pels[p0Idx] = CABAC.clip(p0n, 0, 255);
            }

            if (conditionQ && !isChroma)
            {
                int q2 = pels[q2Idx];
                int q3 = pels[q3Idx];
                int q0n = (p1 + 2 * p0 + 2 * q0 + 2 * q1 + q2 + 4) >> 3;
                int q1n = (p0 + q0 + q1 + q2 + 2) >> 2;
                int q2n = (2 * q3 + 3 * q2 + q1 + q0 + p0 + 4) >> 3;
                pels[q0Idx] = CABAC.clip(q0n, 0, 255);
                pels[q1Idx] = CABAC.clip(q1n, 0, 255);
                pels[q2Idx] = CABAC.clip(q2n, 0, 255);
            }
            else
            {
                int q0n = (2 * q1 + q0 + p1 + 2) >> 2;
                pels[q0Idx] = CABAC.clip(q0n, 0, 255);
            }
        }
    }
}
