using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class PredictionWeightTable
    {
        public int luma_log2_weight_denom;
        public int chroma_log2_weight_denom;

        public int[][] luma_weight = new int[2][];
        public int[][][] chroma_weight = new int[2][][];

        public int[][] luma_offset = new int[2][];
        public int[][][] chroma_offset = new int[2][][];

    }
}
