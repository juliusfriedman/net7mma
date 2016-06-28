using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class CAVLC {

    private ColorSpace color;
    private VLC chromaDCVLC;

    private int[] tokensLeft;
    private int[] tokensTop;
    private int mbWidth;
    private int mbMask;

    public CAVLC(SeqParameterSet sps, PictureParameterSet pps, int mbW, int mbH) {
        this.color = sps.chroma_format_idc;
        this.chromaDCVLC = codeTableChromaDC();
        this.mbWidth = sps.pic_width_in_mbs_minus1 + 1;

        this.mbMask = (1 << mbH) - 1;

        tokensLeft = new int[4];
        tokensTop = new int[mbWidth << mbW];
    }

    public void writeACBlock(BitWriter outb, int blkIndX, int blkIndY, MBType leftMBType, MBType topMBType, int[] coeff,
            VLC[] totalZerosTab, int firstCoeff, int maxCoeff, int[] scan) {
        VLC coeffTokenTab = getCoeffTokenVLCForLuma(blkIndX != 0, leftMBType, tokensLeft[blkIndY & mbMask],
                blkIndY != 0, topMBType, tokensTop[blkIndX]);

        int coeffToken = writeBlockGen(outb, coeff, totalZerosTab, firstCoeff, maxCoeff, scan, coeffTokenTab);

        tokensLeft[blkIndY & mbMask] = coeffToken;
        tokensTop[blkIndX] = coeffToken;
    }

    public void writeChrDCBlock(BitWriter outb, int[] coeff, VLC[] totalZerosTab, int firstCoeff, int maxCoeff,
            int[] scan) {
        writeBlockGen(outb, coeff, totalZerosTab, firstCoeff, maxCoeff, scan, getCoeffTokenVLCForChromaDC());
    }

    public void writeLumaDCBlock(BitWriter outb, int blkIndX, int blkIndY, MBType leftMBType, MBType topMBType,
            int[] coeff, VLC[] totalZerosTab, int firstCoeff, int maxCoeff, int[] scan) {
        VLC coeffTokenTab = getCoeffTokenVLCForLuma(blkIndX != 0, leftMBType, tokensLeft[blkIndY & mbMask],
                blkIndY != 0, topMBType, tokensTop[blkIndX]);

        writeBlockGen(outb, coeff, totalZerosTab, firstCoeff, maxCoeff, scan, coeffTokenTab);
    }

    private int writeBlockGen(BitWriter outb, int[] coeff, VLC[] totalZerosTab, int firstCoeff, int maxCoeff,
            int[] scan, VLC coeffTokenTab) {
        int trailingOnes = 0, totalCoeffx = 0, totalZeros = 0;
        int[] runBefore = new int[maxCoeff];
        int[] levels = new int[maxCoeff];
        for (int i = 0; i < maxCoeff; i++) {
            int c = coeff[scan[i + firstCoeff]];
            if (c == 0) {
                runBefore[totalCoeffx]++;
                totalZeros++;
            } else {
                levels[totalCoeffx++] = c;
            }
        }
        if (totalCoeffx < maxCoeff)
            totalZeros -= runBefore[totalCoeffx];

        for (trailingOnes = 0; trailingOnes < totalCoeffx && trailingOnes < 3
                && Math.Abs(levels[totalCoeffx - trailingOnes - 1]) == 1; trailingOnes++)
            ;

        int coeffTokenx = coeffToken(totalCoeffx, trailingOnes);

        coeffTokenTab.writeVLC(outb, coeffTokenx);

        if (totalCoeffx > 0) {
            writeTrailingOnes(outb, levels, totalCoeffx, trailingOnes);
            writeLevels(outb, levels, totalCoeffx, trailingOnes);

            if (totalCoeffx < maxCoeff) {
                totalZerosTab[totalCoeffx - 1].writeVLC(outb, totalZeros);
                writeRuns(outb, runBefore, totalCoeffx, totalZeros);
            }
        }
        return coeffTokenx;
    }

    private void writeTrailingOnes(BitWriter outb, int[] levels, int totalCoeff, int trailingOne) {
        for (int i = totalCoeff - 1; i >= totalCoeff - trailingOne; i--)
            outb.write1Bit(levels[i] >> 31);
    }

    private void writeLevels(BitWriter outb, int[] levels, int totalCoeff, int trailingOnes) {

        int suffixLen = totalCoeff > 10 && trailingOnes < 3 ? 1 : 0;
        for (int i = totalCoeff - trailingOnes - 1; i >= 0; i--) {
            int absLev = unsigned(levels[i]);
            if (i == totalCoeff - trailingOnes - 1 && trailingOnes < 3)
                absLev -= 2;

            int prefix = absLev >> suffixLen;
            if (suffixLen == 0 && prefix < 14 || suffixLen > 0 && prefix < 15) {
                outb.writeNBit(1, prefix + 1);
                outb.writeNBit(absLev, suffixLen);
            } else if (suffixLen == 0 && absLev < 30) {
                outb.writeNBit(1, 15);
                outb.writeNBit(absLev - 14, 4);
            } else {
                if (suffixLen == 0)
                    absLev -= 15;
                int len, code;
                for (len = 12; (code = absLev - (len + 3 << suffixLen) - (1 << len) + 4096) >= (1 << len); len++)
                    ;
                outb.writeNBit(1, len + 4);
                outb.writeNBit(code, len);
            }
            if (suffixLen == 0)
                suffixLen = 1;
            if (Math.Abs(levels[i]) > (3 << (suffixLen - 1)) && suffixLen < 6)
                suffixLen++;
        }
    }

    private int unsigned(int signed) {
        int sign = signed >> 31;
        int s = signed >> 31;

        return (((signed ^ s) - s) << 1) + sign - 2;
    }

    private void writeRuns(BitWriter outb, int[] run, int totalCoeff, int totalZeros) {
        for (int i = totalCoeff - 1; i > 0 && totalZeros > 0; i--) {
            H264Const.run[Math.Min(6, totalZeros - 1)].writeVLC(outb, run[i]);
            totalZeros -= run[i];
        }
    }

    public VLC getCoeffTokenVLCForLuma(bool leftAvailable, MBType leftMBType, int leftToken, bool topAvailable,
            MBType topMBType, int topToken) {

        int nc = codeTableLuma(leftAvailable, leftMBType, leftToken, topAvailable, topMBType, topToken);

        return H264Const.coeffToken[Math.Min(nc, 8)];
    }

    public VLC getCoeffTokenVLCForChromaDC() {
        return chromaDCVLC;
    }

    protected int codeTableLuma(bool leftAvailable, MBType leftMBType, int leftToken, bool topAvailable,
            MBType topMBType, int topToken) {

        int nA = leftMBType == null ? 0 : totalCoeff(leftToken);
        int nB = topMBType == null ? 0 : totalCoeff(topToken);

        if (leftAvailable && topAvailable)
            return (nA + nB + 1) >> 1;
        else if (leftAvailable)
            return nA;
        else if (topAvailable)
            return nB;
        else
            return 0;
    }

    protected VLC codeTableChromaDC() {
        if (color == ColorSpace.YUV420)
        {
            return H264Const.coeffTokenChromaDCY420;
        }
        else if (color == ColorSpace.YUV422)
        {
            return H264Const.coeffTokenChromaDCY422;
        } else if (color == ColorSpace.YUV444) {
            return H264Const.coeffToken[0];
        }
        return null;
    }

    public int readCoeffs(BitReader inb, VLC coeffTokenTab, VLC[] totalZerosTab, int[] coeffLevel, int firstCoeff,
            int nCoeff, int[] zigzag) {
        int coeffToken = coeffTokenTab.readVLC(inb);
        int totalCoeffx = totalCoeff(coeffToken);
        int trailingOnesx = trailingOnes(coeffToken);
//        System.out.println("Coeff token. Total: " + totalCoeff + ", trailOne: " + trailingOnes);

        // blockType.getMaxCoeffs();
        // if (blockType == BlockType.BLOCK_CHROMA_DC)
        // maxCoeff = 16 / (color.compWidth[1] * color.compHeight[1]);

        if (totalCoeffx > 0) {
            int suffixLength = totalCoeffx > 10 && trailingOnesx < 3 ? 1 : 0;

            int[] level = new int[totalCoeffx];
            int i;
            for (i = 0; i < trailingOnesx; i++)
                level[i] = 1 - 2 * inb.read1Bit();

            for (; i < totalCoeffx; i++) {
                int level_prefix = CAVLCReader.readZeroBitCount(inb, "");
                int levelSuffixSize = suffixLength;
                if (level_prefix == 14 && suffixLength == 0)
                    levelSuffixSize = 4;
                if (level_prefix >= 15)
                    levelSuffixSize = level_prefix - 3;

                int levelCode = (Min(15, level_prefix) << suffixLength);
                if (levelSuffixSize > 0) {
                    int level_suffix = CAVLCReader.readU(inb, levelSuffixSize, "RB: level_suffix");
                    levelCode += level_suffix;
                }
                if (level_prefix >= 15 && suffixLength == 0)
                    levelCode += 15;
                if (level_prefix >= 16)
                    levelCode += (1 << (level_prefix - 3)) - 4096;
                if (i == trailingOnesx && trailingOnesx < 3)
                    levelCode += 2;

                if (levelCode % 2 == 0)
                    level[i] = (levelCode + 2) >> 1;
                else
                    level[i] = (-levelCode - 1) >> 1;

                if (suffixLength == 0)
                    suffixLength = 1;
                if (Abs(level[i]) > (3 << (suffixLength - 1)) && suffixLength < 6)
                    suffixLength++;
            }

            int zerosLeft;
            if (totalCoeffx < nCoeff) {
                if (coeffLevel.Length == 4) {
                    zerosLeft = H264Const.totalZeros4[totalCoeffx - 1].readVLC(inb);
                } else if (coeffLevel.Length == 8) {
                    zerosLeft = H264Const.totalZeros8[totalCoeffx - 1].readVLC(inb);
                } else {
                    zerosLeft = H264Const.totalZeros16[totalCoeffx - 1].readVLC(inb);
                }
            } else
                zerosLeft = 0;

            int[] runs = new int[totalCoeffx];
            int r;
            for (r = 0; r < totalCoeffx - 1 && zerosLeft > 0; r++) {
                int run = H264Const.run[Math.Min(6, zerosLeft - 1)].readVLC(inb);
                zerosLeft -= run;
                runs[r] = run;
            }
            runs[r] = zerosLeft;

            for (int j = totalCoeffx - 1, cn = 0; j >= 0 && cn < nCoeff; j--, cn++) {
                cn += runs[j];
                coeffLevel[zigzag[cn + firstCoeff]] = level[j];
            }
        }

        // System.out.print("[");
        // for (int i = 0; i < nCoeff; i++)
        // System.out.print(coeffLevel[i + firstCoeff] + ", ");
        // System.out.println("]");

        return coeffToken;
    }

    private static int Min(int i, int level_prefix) {
        return i < level_prefix ? i : level_prefix;
    }

    private static int Abs(int i) {
        return i < 0 ? -i : i;
    }

    public static int coeffToken(int totalCoeff, int trailingOnes) {
        return (totalCoeff << 4) | trailingOnes;
    }

    public static int totalCoeff(int coeffToken) {
        return coeffToken >> 4;
    }

    public static int trailingOnes(int coeffToken) {
        return coeffToken & 0xf;
    }

    public static int[] NO_ZIGZAG = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    public void readChromaDCBlock(BitReader reader, int[] coeff, bool leftAvailable, bool topAvailable) {
        VLC coeffTokenTab = getCoeffTokenVLCForChromaDC();

        readCoeffs(reader, coeffTokenTab, coeff.Length == 16 ? H264Const.totalZeros16
                : (coeff.Length == 8 ? H264Const.totalZeros8 : H264Const.totalZeros4), coeff, 0, coeff.Length,
                NO_ZIGZAG);
    }

    public void readLumaDCBlock(BitReader reader, int[] coeff, int mbX, bool leftAvailable, MBType leftMbType,
            bool topAvailable, MBType topMbType, int[] zigzag4x4) {
        VLC coeffTokenTab = getCoeffTokenVLCForLuma(leftAvailable, leftMbType, tokensLeft[0], topAvailable, topMbType,
                tokensTop[mbX << 2]);

        readCoeffs(reader, coeffTokenTab, H264Const.totalZeros16, coeff, 0, 16, zigzag4x4);
    }

    public int readACBlock(BitReader reader, int[] coeff, int blkIndX, int blkIndY, bool leftAvailable,
            MBType leftMbType, bool topAvailable, MBType topMbType, int firstCoeff, int nCoeff, int[] zigzag4x4) {
        VLC coeffTokenTab = getCoeffTokenVLCForLuma(leftAvailable, leftMbType, tokensLeft[blkIndY & mbMask],
                topAvailable, topMbType, tokensTop[blkIndX]);

        int readCoeffsc = readCoeffs(reader, coeffTokenTab, H264Const.totalZeros16, coeff, firstCoeff, nCoeff, zigzag4x4);
        tokensLeft[blkIndY & mbMask] = tokensTop[blkIndX] = readCoeffsc;

        return totalCoeff(readCoeffsc);
    }

    public void setZeroCoeff(int blkIndX, int blkIndY) {
        tokensLeft[blkIndY & mbMask] = tokensTop[blkIndX] = 0;
    }
}
}
