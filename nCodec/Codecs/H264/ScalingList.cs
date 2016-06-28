using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class ScalingList {

    public int[] scalingList;
    public bool useDefaultScalingMatrixFlag;

    public void write(BitWriter outb)  {
        if (useDefaultScalingMatrixFlag) {
            CAVLCWriter.writeSE(outb, 0, "SPS: ");
            return;
        }

        int lastScale = 8;
        int nextScale = 8;
        for (int j = 0; j < scalingList.Length; j++) {
            if (nextScale != 0) {
                int deltaScale = scalingList[j] - lastScale - 256;
                CAVLCWriter.writeSE(outb, deltaScale, "SPS: ");
            }
            lastScale = scalingList[j];
        }
    }

    public static ScalingList read(BitReader inb, int sizeOfScalingList)  {

        ScalingList sl = new ScalingList();
        sl.scalingList = new int[sizeOfScalingList];
        int lastScale = 8;
        int nextScale = 8;
        for (int j = 0; j < sizeOfScalingList; j++) {
            if (nextScale != 0) {
                int deltaScale = CAVLCReader.readSE(inb, "deltaScale");
                nextScale = (lastScale + deltaScale + 256) % 256;
                sl.useDefaultScalingMatrixFlag = (j == 0 && nextScale == 0);
            }
            sl.scalingList[j] = nextScale == 0 ? lastScale : nextScale;
            lastScale = sl.scalingList[j];
        }
        return sl;
    }
}
}
