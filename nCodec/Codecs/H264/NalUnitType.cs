using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class NALUnitType
    {

        public static NALUnitType NON_IDR_SLICE = new NALUnitType(1, "non IDR slice"), SLICE_PART_A = new NALUnitType(2, "slice part a"), SLICE_PART_B = new NALUnitType(3, "slice part b"), SLICE_PART_C = new NALUnitType(
            4, "slice part c"), IDR_SLICE = new NALUnitType(5, "idr slice"), SEI = new NALUnitType(6, "sei"), SPS = new NALUnitType(7, "sequence parameter set"), PPS = new NALUnitType(8,
            "picture parameter set"), ACC_UNIT_DELIM = new NALUnitType(9, "access unit delimiter"), END_OF_SEQ = new NALUnitType(10, "end of sequence"), END_OF_STREAM = new NALUnitType(
            11, "end of stream"), FILLER_DATA = new NALUnitType(12, "filter data"), SEQ_PAR_SET_EXT = new NALUnitType(13,
            "sequence parameter set extension"), AUX_SLICE = new NALUnitType(19, "auxilary slice");

        private int value;
        private String name;

        private NALUnitType(int value, String name)
        {
            this.value = value;
            this.name = name;
        }

        public String getName()
        {
            return name;
        }

        public int getValue()
        {
            return value;
        }

        public static NALUnitType fromValue(int value)
        {

            switch (value)
            {
                case 0: goto default;
                case 1: return NON_IDR_SLICE;
                case 2: return SLICE_PART_A;
                default: return null;
            }

        }
    }
}
