using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class MPEGPred {
        protected int[][][] mvPred = (int[][][])Array.CreateInstance(typeof(int), new int[] { 2, 2, 2, });
    private int chromaFormat;
    private int[][] fCode;
    private bool topFieldFirst;

    public MPEGPred(int[][] fCode, int chromaFormat, bool topFieldFirst) {
        this.fCode = fCode;
        this.chromaFormat = chromaFormat;
        this.topFieldFirst = topFieldFirst;
    }
    
    public MPEGPred(MPEGPred other) {
        this.fCode = other.fCode;
        this.chromaFormat = other.chromaFormat;
        this.topFieldFirst = other.topFieldFirst;
    }

    public void predictEvenEvenSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int offRef = ((refY << refVertStep) + refVertOff) * refW + refX, offTgt = tgtW * tgtY, lfRef = (refW << refVertStep)
                - tgtW, lfTgt = tgtVertStep * tgtW;

        for (int i = 0; i < tgtH; i++) {
            for (int j = 0; j < tgtW; j++)
                tgt[offTgt++] = refs[offRef++];
            offRef += lfRef;
            offTgt += lfTgt;
        }
    }

    public void predictEvenOddSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int offRef = ((refY << refVertStep) + refVertOff) * refW + refX, offTgt = tgtW * tgtY, lfRef = (refW << refVertStep)
                - tgtW, lfTgt = tgtVertStep * tgtW;

        for (int i = 0; i < tgtH; i++) {
            for (int j = 0; j < tgtW; j++) {
                tgt[offTgt++] = (refs[offRef] + refs[offRef + 1] + 1) >> 1;
                ++offRef;
            }
            offRef += lfRef;
            offTgt += lfTgt;
        }
    }

    public void predictOddEvenSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int offRef = ((refY << refVertStep) + refVertOff) * refW + refX, offTgt = tgtW * tgtY, lfRef = (refW << refVertStep)
                - tgtW, lfTgt = tgtVertStep * tgtW, stride = refW << refVertStep;

        for (int i = 0; i < tgtH; i++) {
            for (int j = 0; j < tgtW; j++) {
                tgt[offTgt++] = (refs[offRef] + refs[offRef + stride] + 1) >> 1;
                ++offRef;
            }
            offRef += lfRef;
            offTgt += lfTgt;
        }
    }

    public void predictOddOddSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int offRef = ((refY << refVertStep) + refVertOff) * refW + refX, offTgt = tgtW * tgtY, lfRef = (refW << refVertStep)
                - tgtW, lfTgt = tgtVertStep * tgtW, stride = refW << refVertStep;

        for (int i = 0; i < tgtH; i++) {
            for (int j = 0; j < tgtW; j++) {
                tgt[offTgt++] = (refs[offRef] + refs[offRef + 1] + refs[offRef + stride] + refs[offRef + stride + 1] + 3) >> 2;
                ++offRef;
            }
            offRef += lfRef;
            offTgt += lfTgt;
        }
    }

    protected int getPix1(int[] refs, int refW, int refH, int x, int y, int refVertStep, int refVertOff) {
        x = nVideo.Codecs.H264.CABAC.clip(x, 0, refW - 1);
        y = nVideo.Codecs.H264.CABAC.clip(y, 0, refH - (1 << refVertStep) + refVertOff);

        return refs[y * refW + x];
    }

    protected int getPix2(int[] refs, int refW, int refH, int x1, int y1, int x2, int y2, int refVertStep,
            int refVertOff) {
        x1 = nVideo.Codecs.H264.CABAC.clip(x1, 0, refW - 1);
        int lastLine = refH - (1 << refVertStep) + refVertOff;
        y1 = nVideo.Codecs.H264.CABAC.clip(y1, 0, lastLine);
        x2 = nVideo.Codecs.H264.CABAC.clip(x2, 0, refW - 1);
        y2 = nVideo.Codecs.H264.CABAC.clip(y2, 0, lastLine);

        return (refs[y1 * refW + x1] + refs[y2 * refW + x2] + 1) >> 1;
    }

    protected int getPix4(int[] refs, int refW, int refH, int x1, int y1, int x2, int y2, int x3, int y3, int x4,
            int y4, int refVertStep, int refVertOff) {
        int lastLine = refH - (1 << refVertStep) + refVertOff;
        x1 = nVideo.Codecs.H264.CABAC.clip(x1, 0, refW - 1);
        y1 = nVideo.Codecs.H264.CABAC.clip(y1, 0, lastLine);
        x2 = nVideo.Codecs.H264.CABAC.clip(x2, 0, refW - 1);
        y2 = nVideo.Codecs.H264.CABAC.clip(y2, 0, lastLine);
        x3 = nVideo.Codecs.H264.CABAC.clip(x3, 0, refW - 1);
        y3 = nVideo.Codecs.H264.CABAC.clip(y3, 0, lastLine);
        x4 = nVideo.Codecs.H264.CABAC.clip(x4, 0, refW - 1);
        y4 = nVideo.Codecs.H264.CABAC.clip(y4, 0, lastLine);

        return (refs[y1 * refW + x1] + refs[y2 * refW + x2] + refs[y3 * refW + x3] + refs[y4 * refW + x4] + 3) >> 2;
    }

    public void predictEvenEvenUnSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int tgtOff = tgtW * tgtY, jump = tgtVertStep * tgtW;
        for (int j = 0; j < tgtH; j++) {
            int y = ((j + refY) << refVertStep) + refVertOff;
            for (int i = 0; i < tgtW; i++) {
                tgt[tgtOff++] = getPix1(refs, refW, refH, i + refX, y, refVertStep, refVertOff);
            }
            tgtOff += jump;
        }
    }

    public void predictEvenOddUnSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int tgtOff = tgtW * tgtY, jump = tgtVertStep * tgtW;
        for (int j = 0; j < tgtH; j++) {
            int y = ((j + refY) << refVertStep) + refVertOff;
            for (int i = 0; i < tgtW; i++) {
                tgt[tgtOff++] = getPix2(refs, refW, refH, i + refX, y, i + refX + 1, y, refVertStep, refVertOff);
            }
            tgtOff += jump;
        }
    }

    public void predictOddEvenUnSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int tgtOff = tgtW * tgtY, jump = tgtVertStep * tgtW;
        for (int j = 0; j < tgtH; j++) {
            int y1 = ((j + refY) << refVertStep) + refVertOff;
            int y2 = ((j + refY + 1) << refVertStep) + refVertOff;
            for (int i = 0; i < tgtW; i++) {
                tgt[tgtOff++] = getPix2(refs, refW, refH, i + refX, y1, i + refX, y2, refVertStep, refVertOff);
            }
            tgtOff += jump;
        }
    }

    public void predictOddOddUnSafe(int[] refs, int refX, int refY, int refW, int refH, int refVertStep,
            int refVertOff, int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int tgtOff = tgtW * tgtY, jump = tgtVertStep * tgtW;
        for (int j = 0; j < tgtH; j++) {
            int y1 = ((j + refY) << refVertStep) + refVertOff;
            int y2 = ((j + refY + 1) << refVertStep) + refVertOff;
            for (int i = 0; i < tgtW; i++) {
                int ptX = i + refX;
                tgt[tgtOff++] = getPix4(refs, refW, refH, ptX, y1, ptX + 1, y1, ptX, y2, ptX + 1, y2, refVertStep,
                        refVertOff);
            }
            tgtOff += jump;
        }
    }

    public void predictPlane(int[] refs, int refX, int refY, int refW, int refH, int refVertStep, int refVertOff,
            int[] tgt, int tgtY, int tgtW, int tgtH, int tgtVertStep) {
        int rx = refX >> 1, ry = refY >> 1;

        bool safe = rx >= 0 && ry >= 0 && rx + tgtW < refW && (ry + tgtH << refVertStep) < refH;
        if ((refX & 0x1) == 0) {
            if ((refY & 0x1) == 0) {
                if (safe)
                    predictEvenEvenSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                            tgtVertStep);
                else
                    predictEvenEvenUnSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                            tgtVertStep);
            } else {
                if (safe)
                    predictOddEvenSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                            tgtVertStep);
                else
                    predictOddEvenUnSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                            tgtVertStep);
            }
        } else if ((refY & 0x1) == 0) {
            if (safe)
                predictEvenOddSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH, tgtVertStep);
            else
                predictEvenOddUnSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                        tgtVertStep);
        } else {
            if (safe)
                predictOddOddSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH, tgtVertStep);
            else
                predictOddOddUnSafe(refs, rx, ry, refW, refH, refVertStep, refVertOff, tgt, tgtY, tgtW, tgtH,
                        tgtVertStep);
        }
    }

    public void predictInField(Picture[] reference, int x, int y, int[][] mbPix, BitReader bits, int motionType,
            int backward, int fieldNo) {
        switch (motionType) {
        case 1:
            predict16x16Field(reference, x, y, bits, backward, mbPix);
            break;
        case 2:
            predict16x8MC(reference, x, y, bits, backward, mbPix, 0, 0);
            predict16x8MC(reference, x, y, bits, backward, mbPix, 8, 1);
            break;
        case 3:
            predict16x16DualPrimeField(reference, x, y, bits, mbPix, fieldNo);
            break;
        }
    }

    public void predictInFrame(Picture reference, int x, int y, int[][] mbPix, BitReader inb, int motionType,
            int backward, int spatial_temporal_weight_code) {
        Picture[] refs = new Picture[] { reference, reference };
        switch (motionType) {
        case 1:
            predictFieldInFrame(reference, x, y, mbPix, inb, backward, spatial_temporal_weight_code);
            break;
        case 2:
            predict16x16Frame(reference, x, y, inb, backward, mbPix);
            break;
        case 3:
            predict16x16DualPrimeFrame(refs, x, y, inb, backward, mbPix);
            break;
        }
    }

    private void predict16x16DualPrimeField(Picture[] reference, int x, int y, BitReader bits, int[][] mbPix,
            int fieldNo) {
        int vect1X = mvectDecode(bits, fCode[0][0], mvPred[0][0][0]);
        int dmX = MPEGConst.vlcDualPrime.readVLC(bits) - 1;

        int vect1Y = mvectDecode(bits, fCode[0][1], mvPred[0][0][1]);
        int dmY = MPEGConst.vlcDualPrime.readVLC(bits) - 1;

        int vect2X = dpXField(vect1X, dmX, 1 - fieldNo);
        int vect2Y = dpYField(vect1Y, dmY, 1 - fieldNo);

        int ch = chromaFormat == SequenceExtension.Chroma420 ? 1 : 0;
        int cw = chromaFormat == SequenceExtension.Chroma444 ? 0 : 1;
        int sh = chromaFormat == SequenceExtension.Chroma420 ? 2 : 1;
        int sw = chromaFormat == SequenceExtension.Chroma444 ? 1 : 2;

        int[][] mbPix1 = (int[][])Array.CreateInstance(typeof(int), new int[] { 3, 256 }), mbPix2 = (int[][])Array.CreateInstance(typeof(int), new int[] { 3, 256 });

        int refX1 = (x << 1) + vect1X;
        int refY1 = (y << 1) + vect1Y;
        int refX1Chr = ((x << 1) >> cw) + vect1X / sw;
        int refY1Chr = ((y << 1) >> ch) + vect1Y / sh;

        predictPlane(reference[fieldNo].getPlaneData(0), refX1, refY1, reference[fieldNo].getPlaneWidth(0),
                reference[fieldNo].getPlaneHeight(0), 1, fieldNo, mbPix1[0], 0, 16, 16, 0);
        predictPlane(reference[fieldNo].getPlaneData(1), refX1Chr, refY1Chr, reference[fieldNo].getPlaneWidth(1),
                reference[fieldNo].getPlaneHeight(1), 1, fieldNo, mbPix1[1], 0, 16 >> cw, 16 >> ch, 0);
        predictPlane(reference[fieldNo].getPlaneData(2), refX1Chr, refY1Chr, reference[fieldNo].getPlaneWidth(2),
                reference[fieldNo].getPlaneHeight(2), 1, fieldNo, mbPix1[2], 0, 16 >> cw, 16 >> ch, 0);

        int refX2 = (x << 1) + vect2X;
        int refY2 = (y << 1) + vect2Y;
        int refX2Chr = ((x << 1) >> cw) + vect2X / sw;
        int refY2Chr = ((y << 1) >> ch) + vect2Y / sh;
        int opposite = 1 - fieldNo;
        predictPlane(reference[opposite].getPlaneData(0), refX2, refY2, reference[opposite].getPlaneWidth(0),
                reference[opposite].getPlaneHeight(0), 1, opposite, mbPix2[0], 0, 16, 16, 0);
        predictPlane(reference[opposite].getPlaneData(1), refX2Chr, refY2Chr, reference[opposite].getPlaneWidth(1),
                reference[opposite].getPlaneHeight(1), 1, opposite, mbPix2[1], 0, 16 >> cw, 16 >> ch, 0);
        predictPlane(reference[opposite].getPlaneData(2), refX2Chr, refY2Chr, reference[opposite].getPlaneWidth(2),
                reference[opposite].getPlaneHeight(2), 1, opposite, mbPix2[2], 0, 16 >> cw, 16 >> ch, 0);

        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < mbPix[i].Length; j++)
                mbPix[i][j] = (mbPix1[i][j] + mbPix2[i][j] + 1) >> 1;
        }

        mvPred[1][0][0] = mvPred[0][0][0] = vect1X;
        mvPred[1][0][1] = mvPred[0][0][1] = vect1Y;
    }

    private  int dpYField(int vect1y, int dmY, int topField) {
        return ((vect1y + (vect1y > 0 ? 1 : 0)) >> 1) + (1 - (topField << 1)) + dmY;
    }

    private int dpXField(int vect1x, int dmX, int topField) {
        return ((vect1x + (vect1x > 0 ? 1 : 0)) >> 1) + dmX;
    }

    private void predict16x8MC(Picture[] reference, int x, int y, BitReader bits, int backward, int[][] mbPix,
            int vertPos, int vectIdx) {
        int field = bits.read1Bit();

        predictGeneric(reference[field], x, y + vertPos, bits, backward, mbPix, vertPos, 16, 8, 1, field, 0, vectIdx, 0);
    }

    protected void predict16x16Field(Picture[] reference, int x, int y, BitReader bits, int backward, int[][] mbPix) {
        int field = bits.read1Bit();

        predictGeneric(reference[field], x, y, bits, backward, mbPix, 0, 16, 16, 1, field, 0, 0, 0);

        mvPred[1][backward][0] = mvPred[0][backward][0];
        mvPred[1][backward][1] = mvPred[0][backward][1];
    }

    private void predict16x16DualPrimeFrame(Picture[] reference, int x, int y, BitReader bits, int backward,
            int[][] mbPix) {
        int vect1X = mvectDecode(bits, fCode[0][0], mvPred[0][0][0]);
        int dmX = MPEGConst.vlcDualPrime.readVLC(bits) - 1;

        int vect1Y = mvectDecode(bits, fCode[0][1], mvPred[0][0][1] >> 1);
        int dmY = MPEGConst.vlcDualPrime.readVLC(bits) - 1;

        int m = topFieldFirst ? 1 : 3;

        int vect2X = ((vect1X * m + (vect1X > 0 ? 1 : 0)) >> 1) + dmX;
        int vect2Y = ((vect1Y * m + (vect1Y > 0 ? 1 : 0)) >> 1) + dmY - 1;
        m = 4 - m;
        int vect3X = ((vect1X * m + (vect1X > 0 ? 1 : 0)) >> 1) + dmX;
        int vect3Y = ((vect1Y * m + (vect1Y > 0 ? 1 : 0)) >> 1) + dmY + 1;

        int ch = chromaFormat == SequenceExtension.Chroma420 ? 1 : 0;
        int cw = chromaFormat == SequenceExtension.Chroma444 ? 0 : 1;
        int sh = chromaFormat == SequenceExtension.Chroma420 ? 2 : 1;
        int sw = chromaFormat == SequenceExtension.Chroma444 ? 1 : 2;

        int[][] mbPix1 = (int[][])Array.CreateInstance(typeof(int), new int[] { 3, 256 }), mbPix2 = (int[][])Array.CreateInstance(typeof(int), new int[] { 3, 256 });

        int refX1 = (x << 1) + vect1X;
        int refY1 = y + vect1Y;
        int refX1Chr = ((x << 1) >> cw) + vect1X / sw;
        int refY1Chr = (y >> ch) + vect1Y / sh;

        predictPlane(reference[0].getPlaneData(0), refX1, refY1, reference[0].getPlaneWidth(0),
                reference[0].getPlaneHeight(0), 1, 0, mbPix1[0], 0, 16, 8, 1);
        predictPlane(reference[0].getPlaneData(1), refX1Chr, refY1Chr, reference[0].getPlaneWidth(1),
                reference[0].getPlaneHeight(1), 1, 0, mbPix1[1], 0, 16 >> cw, 8 >> ch, 1);
        predictPlane(reference[0].getPlaneData(2), refX1Chr, refY1Chr, reference[0].getPlaneWidth(2),
                reference[0].getPlaneHeight(2), 1, 0, mbPix1[2], 0, 16 >> cw, 8 >> ch, 1);

        predictPlane(reference[1].getPlaneData(0), refX1, refY1, reference[1].getPlaneWidth(0),
                reference[1].getPlaneHeight(0), 1, 1, mbPix1[0], 1, 16, 8, 1);
        predictPlane(reference[1].getPlaneData(1), refX1Chr, refY1Chr, reference[1].getPlaneWidth(1),
                reference[1].getPlaneHeight(1), 1, 1, mbPix1[1], 1, 16 >> cw, 8 >> ch, 1);
        predictPlane(reference[1].getPlaneData(2), refX1Chr, refY1Chr, reference[1].getPlaneWidth(2),
                reference[1].getPlaneHeight(2), 1, 1, mbPix1[2], 1, 16 >> cw, 8 >> ch, 1);

        int refX2 = (x << 1) + vect2X;
        int refY2 = y + vect2Y;
        int refX2Chr = ((x << 1) >> cw) + vect2X / sw;
        int refY2Chr = (y >> ch) + vect2Y / sh;
        predictPlane(reference[1].getPlaneData(0), refX2, refY2, reference[1].getPlaneWidth(0),
                reference[1].getPlaneHeight(0), 1, 1, mbPix2[0], 0, 16, 8, 1);
        predictPlane(reference[1].getPlaneData(1), refX2Chr, refY2Chr, reference[1].getPlaneWidth(1),
                reference[1].getPlaneHeight(1), 1, 1, mbPix2[1], 0, 16 >> cw, 8 >> ch, 1);
        predictPlane(reference[1].getPlaneData(2), refX2Chr, refY2Chr, reference[1].getPlaneWidth(2),
                reference[1].getPlaneHeight(2), 1, 1, mbPix2[2], 0, 16 >> cw, 8 >> ch, 1);

        int refX3 = (x << 1) + vect3X;
        int refY3 = y + vect3Y;
        int refX3Chr = ((x << 1) >> cw) + vect3X / sw;
        int refY3Chr = (y >> ch) + vect3Y / sh;
        predictPlane(reference[0].getPlaneData(0), refX3, refY3, reference[0].getPlaneWidth(0),
                reference[0].getPlaneHeight(0), 1, 0, mbPix2[0], 1, 16, 8, 1);
        predictPlane(reference[0].getPlaneData(1), refX3Chr, refY3Chr, reference[0].getPlaneWidth(1),
                reference[0].getPlaneHeight(1), 1, 0, mbPix2[1], 1, 16 >> cw, 8 >> ch, 1);
        predictPlane(reference[0].getPlaneData(2), refX3Chr, refY3Chr, reference[0].getPlaneWidth(2),
                reference[0].getPlaneHeight(2), 1, 0, mbPix2[2], 1, 16 >> cw, 8 >> ch, 1);

        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < mbPix[i].Length; j++)
                mbPix[i][j] = (mbPix1[i][j] + mbPix2[i][j] + 1) >> 1;
        }

        mvPred[1][0][0] = mvPred[0][0][0] = vect1X;
        mvPred[1][0][1] = mvPred[0][0][1] = vect1Y << 1;
    }

    protected void predict16x16Frame(Picture reference, int x, int y, BitReader bits, int backward, int[][] mbPix) {
        predictGeneric(reference, x, y, bits, backward, mbPix, 0, 16, 16, 0, 0, 0, 0, 0);

        mvPred[1][backward][0] = mvPred[0][backward][0];
        mvPred[1][backward][1] = mvPred[0][backward][1];
    }

    private int mvectDecode(BitReader bits, int fcode, int pred) {
        int code = MPEGConst.vlcMotionCode.readVLC(bits);
        if (code == 0) {
            return pred;
        }
        if (code < 0) {
            return 0xffff;
        }

        int sign, val, shift;
        sign = bits.read1Bit();
        shift = fcode - 1;
        val = code;
        if (shift > 0) {
            val = (val - 1) << shift;
            val |= bits.readNBit(shift);
            val++;
        }
        if (sign != 0)
            val = -val;
        val += pred;

        return sign_extend(val, 5 + shift);
    }

    private int sign_extend(int val, int bits) {
        int shift = 32 - bits;
        return (val << shift) >> shift;
    }

    protected void predictGeneric(Picture reference, int x, int y, BitReader bits, int backward, int[][] mbPix, int tgtY,
            int blkW, int blkH, int isSrcField, int srcField, int isDstField, int vectIdx, int predScale) {
        int vectX = mvectDecode(bits, fCode[backward][0], mvPred[vectIdx][backward][0]);
        int vectY = mvectDecode(bits, fCode[backward][1], mvPred[vectIdx][backward][1] >> predScale);

        predictMB(reference, (x << 1), vectX, (y << 1), vectY, blkW, blkH, isSrcField, srcField, mbPix, tgtY,
                isDstField);

        mvPred[vectIdx][backward][0] = vectX;
        mvPred[vectIdx][backward][1] = vectY << predScale;
    }

    private void predictFieldInFrame(Picture reference, int x, int y, int[][] mbPix, BitReader bits, int backward,
            int spatial_temporal_weight_code) {
        y >>= 1;
        int field = bits.read1Bit();
        predictGeneric(reference, x, y, bits, backward, mbPix, 0, 16, 8, 1, field, 1, 0, 1);
        if (spatial_temporal_weight_code == 0 || spatial_temporal_weight_code == 1) {
            field = bits.read1Bit();
            predictGeneric(reference, x, y, bits, backward, mbPix, 1, 16, 8, 1, field, 1, 1, 1);
        } else {
            mvPred[1][backward][0] = mvPred[0][backward][0];
            mvPred[1][backward][1] = mvPred[0][backward][1];
            predictMB(reference, mvPred[1][backward][0], 0, mvPred[1][backward][1], 0, 16, 8, 1, 1 - field, mbPix, 1, 1);
        }
    }

    public void predictMB(Picture refr, int refX, int vectX, int refY, int vectY, int blkW, int blkH, int refVertStep,
            int refVertOff, int[][] tgt, int tgtY, int tgtVertStep) {
                int ch = chromaFormat == SequenceExtension.Chroma420 ? 1 : 0;
                int cw = chromaFormat == SequenceExtension.Chroma444 ? 0 : 1;

                int sh = chromaFormat == SequenceExtension.Chroma420 ? 2 : 1;
                int sw = chromaFormat == SequenceExtension.Chroma444 ? 1 : 2;

        predictPlane(refr.getPlaneData(0), refX + vectX, refY + vectY, refr.getPlaneWidth(0), refr.getPlaneHeight(0),
                refVertStep, refVertOff, tgt[0], tgtY, blkW, blkH, tgtVertStep);
        predictPlane(refr.getPlaneData(1), (refX >> cw) + vectX / sw, (refY >> ch) + vectY / sh, refr.getPlaneWidth(1),
                refr.getPlaneHeight(1), refVertStep, refVertOff, tgt[1], tgtY, blkW >> cw, blkH >> ch, tgtVertStep);
        predictPlane(refr.getPlaneData(2), (refX >> cw) + vectX / sw, (refY >> ch) + vectY / sh, refr.getPlaneWidth(2),
                refr.getPlaneHeight(2), refVertStep, refVertOff, tgt[2], tgtY, blkW >> cw, blkH >> ch, tgtVertStep);
    }

    public void predict16x16NoMV(Picture picture, int x, int y, int pictureStructure, int backward, int[][] mbPix) {
        if (pictureStructure == 3) {
            predictMB(picture, (x << 1), mvPred[0][backward][0], (y << 1), mvPred[0][backward][1], 16, 16, 0, 0, mbPix,
                    0, 0);
        } else
            predictMB(picture, (x << 1), mvPred[0][backward][0], (y << 1), mvPred[0][backward][1], 16, 16, 1,
                    pictureStructure - 1, mbPix, 0, 0);
    }

    public void reset() {
        mvPred[0][0][0] = mvPred[0][0][1] = mvPred[0][1][0] = mvPred[0][1][1] = mvPred[1][0][0] = mvPred[1][0][1] = mvPred[1][1][0] = mvPred[1][1][1] = 0;
    }
}
}
