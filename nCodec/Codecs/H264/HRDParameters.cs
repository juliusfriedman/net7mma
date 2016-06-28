﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class HRDParameters
    {

        public int cpb_cnt_minus1;
        public int bit_rate_scale;
        public int cpb_size_scale;
        public int[] bit_rate_value_minus1;
        public int[] cpb_size_value_minus1;
        public bool[] cbr_flag;
        public int initial_cpb_removal_delay_length_minus1;
        public int cpb_removal_delay_length_minus1;
        public int dpb_output_delay_length_minus1;
        public int time_offset_length;

    }
}
