using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class POCManager
    {

        private int prevPOCMsb;
        private int prevPOCLsb;

        public int calcPOC(SliceHeader firstSliceHeader, NALUnit firstNu)
        {
            switch (firstSliceHeader.sps.pic_order_cnt_type)
            {
                case 0:
                    return calcPOC0(firstSliceHeader, firstNu);
                case 1:
                    return calcPOC1(firstSliceHeader, firstNu);
                case 2:
                    return calcPOC2(firstSliceHeader, firstNu);
                default:
                    throw new Exception("POC no!!!");
            }

        }

        private int calcPOC2(SliceHeader firstSliceHeader, NALUnit firstNu)
        {
            return firstSliceHeader.frame_num << 1;
        }

        private int calcPOC1(SliceHeader firstSliceHeader, NALUnit firstNu)
        {
            return firstSliceHeader.frame_num << 1;
        }

        private int calcPOC0(SliceHeader firstSliceHeader, NALUnit firstNu)
        {
            if (firstNu.type == NALUnitType.IDR_SLICE)
            {
                prevPOCMsb = prevPOCLsb = 0;
            }
            int maxPOCLsbDiv2 = 1 << (firstSliceHeader.sps.log2_max_pic_order_cnt_lsb_minus4 + 3), maxPOCLsb = maxPOCLsbDiv2 << 1;
            int POCLsb = firstSliceHeader.pic_order_cnt_lsb;

            int POCMsb, POC;
            if ((POCLsb < prevPOCLsb) && ((prevPOCLsb - POCLsb) >= maxPOCLsbDiv2))
                POCMsb = prevPOCMsb + maxPOCLsb;
            else if ((POCLsb > prevPOCLsb) && ((POCLsb - prevPOCLsb) > maxPOCLsbDiv2))
                POCMsb = prevPOCMsb - maxPOCLsb;
            else
                POCMsb = prevPOCMsb;

            POC = POCMsb + POCLsb;

            if (firstNu.nal_ref_idc > 0)
            {
                if (hasMMCO5(firstSliceHeader, firstNu))
                {
                    prevPOCMsb = 0;
                    prevPOCLsb = POC;
                }
                else
                {
                    prevPOCMsb = POCMsb;
                    prevPOCLsb = POCLsb;
                }
            }

            return POC;
        }

        private bool hasMMCO5(SliceHeader firstSliceHeader, NALUnit firstNu) {
        if (firstNu.type != NALUnitType.IDR_SLICE && firstSliceHeader.refPicMarkingNonIDR != null) {
            nVideo.Codecs.H264.RefPicMarking.Instruction[] instructions = firstSliceHeader.refPicMarkingNonIDR.getInstructions();
            foreach (var instruction in instructions) {
                if (instruction.getType() == nVideo.Codecs.H264.RefPicMarking.InstrType.CLEAR)
                    return true;
            }
        }
        return false;
    }
    }
}
