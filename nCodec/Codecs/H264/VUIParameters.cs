using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class VUIParameters
    {

        public class BitstreamRestriction
        {

            public bool motion_vectors_over_pic_boundaries_flag;
            public int max_bytes_per_pic_denom;
            public int max_bits_per_mb_denom;
            public int log2_max_mv_length_horizontal;
            public int log2_max_mv_length_vertical;
            public int num_reorder_frames;
            public int max_dec_frame_buffering;

        }

        public bool aspect_ratio_info_present_flag;
        public int sar_width;
        public int sar_height;
        public bool overscan_info_present_flag;
        public bool overscan_appropriate_flag;
        public bool video_signal_type_present_flag;
        public int video_format;
        public bool video_full_range_flag;
        public bool colour_description_present_flag;
        public int colour_primaries;
        public int transfer_characteristics;
        public int matrix_coefficients;
        public bool chroma_loc_info_present_flag;
        public int chroma_sample_loc_type_top_field;
        public int chroma_sample_loc_type_bottom_field;
        public bool timing_info_present_flag;
        public int num_units_in_tick;
        public int time_scale;
        public bool fixed_frame_rate_flag;
        public bool low_delay_hrd_flag;
        public bool pic_struct_present_flag;
        public HRDParameters nalHRDParams;
        public HRDParameters vclHRDParams;

        public BitstreamRestriction bitstreamRestriction;
        public AspectRatio aspect_ratio;

    }
}
