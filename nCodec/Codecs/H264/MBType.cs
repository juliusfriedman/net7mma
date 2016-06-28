using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class MBType
    {

        public static MBType I_NxN = new MBType(true), I_16x16 = new MBType(true), I_PCM = new MBType(true), 
            P_16x16 = new MBType(), P_16x8, P_8x16, P_8x8, P_8x8ref0, B_Direct_16x16, B_L0_16x16, B_L1_16x16, B_Bi_16x16, B_L0_L0_16x8, B_L0_L0_8x16, B_L1_L1_16x8, B_L1_L1_8x16, B_L0_L1_16x8, B_L0_L1_8x16, B_L1_L0_16x8, B_L1_L0_8x16, B_L0_Bi_16x8, B_L0_Bi_8x16, B_L1_Bi_16x8, B_L1_Bi_8x16, B_Bi_L0_16x8, B_Bi_L0_8x16, B_Bi_L1_16x8, B_Bi_L1_8x16, B_Bi_Bi_16x8, B_Bi_Bi_8x16, B_8x8;

        public bool intra;

        private MBType(bool intra)
        {
            this.intra = intra;
        }

        private MBType() : this(false)
        {
            
        }

        public bool isIntra()
        {
            return intra;
        }
    }
}
