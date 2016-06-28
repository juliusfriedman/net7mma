using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class SeqParameterSet
    {
        public int pic_order_cnt_type;
        public bool field_pic_flag;
        public bool delta_pic_order_always_zero_flag;
        public bool mb_adaptive_frame_field_flag;
        public bool direct_8x8_inference_flag;
        public ColorSpace chroma_format_idc;
        public int log2_max_frame_num_minus4;
        public int log2_max_pic_order_cnt_lsb_minus4;
        public int pic_height_in_map_units_minus1;
        public int pic_width_in_mbs_minus1;
        public int bit_depth_luma_minus8;
        public int bit_depth_chroma_minus8;
        public bool qpprime_y_zero_transform_bypass_flag;
        public int profile_idc;
        public bool constraint_set_0_flag;
        public bool constraint_set_1_flag;
        public bool constraint_set_2_flag;
        public bool constraint_set_3_flag;
        public int level_idc;
        public int seq_parameter_set_id;
        public bool residual_color_transform_flag;
        public int offset_for_non_ref_pic;
        public int offset_for_top_to_bottom_field;
        public int num_ref_frames;
        public bool gaps_in_frame_num_value_allowed_flag;
        public bool frame_mbs_only_flag;
        public bool frame_cropping_flag;
        public int frame_crop_left_offset;
        public int frame_crop_right_offset;
        public int frame_crop_top_offset;
        public int frame_crop_bottom_offset;
        public int[] offsetForRefFrame;
        public VUIParameters vuiParams;
        public ScalingMatrix scalingMatrix;
        public int num_ref_frames_in_pic_order_cnt_cycle;

        public static ColorSpace getColor(int id)
        {
            switch (id)
            {
                case 0:
                    return ColorSpace.MONO;
                case 1:
                    return ColorSpace.YUV420;
                case 2:
                    return ColorSpace.YUV422;
                case 3:
                    return ColorSpace.YUV444;
            }
            throw new Exception("Colorspace not supported");
        }

        public static int fromColor(ColorSpace color)
        {
            if (color == ColorSpace.MONO)
                return 0;
            else if (color == ColorSpace.YUV420)
                return 1;
            else if (color == ColorSpace.YUV422)
                return 2;
            else if (color == ColorSpace.YUV444)
                return 3;
            else 
                throw new Exception("Colorspace not supported");
        }

        public static SeqParameterSet read(MemoryStream isb)
        {
            BitReader inb = new BitReader(isb);
            SeqParameterSet sps = new SeqParameterSet();

            sps.profile_idc =CAVLCReader.readNBit(inb, 8, "SPS: profile_idc");
            sps.constraint_set_0_flag =CAVLCReader.readBool(inb, "SPS: constraint_set_0_flag");
            sps.constraint_set_1_flag =CAVLCReader.readBool(inb, "SPS: constraint_set_1_flag");
            sps.constraint_set_2_flag =CAVLCReader.readBool(inb, "SPS: constraint_set_2_flag");
            sps.constraint_set_3_flag =CAVLCReader.readBool(inb, "SPS: constraint_set_3_flag");
            CAVLCReader.readNBit(inb, 4, "SPS: reserved_zero_4bits");
            sps.level_idc = (int)CAVLCReader.readNBit(inb, 8, "SPS: level_idc");
            sps.seq_parameter_set_id =CAVLCReader.readUE(inb, "SPS: seq_parameter_set_id");

            if (sps.profile_idc == 100 || sps.profile_idc == 110 || sps.profile_idc == 122 || sps.profile_idc == 144)
            {
                sps.chroma_format_idc = getColor(CAVLCReader.readUE(inb, "SPS: chroma_format_idc"));
                if (sps.chroma_format_idc == ColorSpace.YUV444)
                {
                    sps.residual_color_transform_flag =CAVLCReader.readBool(inb, "SPS: residual_color_transform_flag");
                }
                sps.bit_depth_luma_minus8 =CAVLCReader.readUE(inb, "SPS: bit_depth_luma_minus8");
                sps.bit_depth_chroma_minus8 =CAVLCReader.readUE(inb, "SPS: bit_depth_chroma_minus8");
                sps.qpprime_y_zero_transform_bypass_flag =CAVLCReader.readBool(inb, "SPS: qpprime_y_zero_transform_bypass_flag");
                bool seqScalingMatrixPresent =CAVLCReader.readBool(inb, "SPS: seq_scaling_matrix_present_lag");
                if (seqScalingMatrixPresent)
                {
                    readScalingListMatrix(inb, sps);
                }
            }
            else
            {
                sps.chroma_format_idc = ColorSpace.YUV420;
            }
            sps.log2_max_frame_num_minus4 =CAVLCReader.readUE(inb, "SPS: log2_max_frame_num_minus4");
            sps.pic_order_cnt_type =CAVLCReader.readUE(inb, "SPS: pic_order_cnt_type");
            if (sps.pic_order_cnt_type == 0)
            {
                sps.log2_max_pic_order_cnt_lsb_minus4 =CAVLCReader.readUE(inb, "SPS: log2_max_pic_order_cnt_lsb_minus4");
            }
            else if (sps.pic_order_cnt_type == 1)
            {
                sps.delta_pic_order_always_zero_flag =CAVLCReader.readBool(inb, "SPS: delta_pic_order_always_zero_flag");
                sps.offset_for_non_ref_pic = CAVLCReader.readSE(inb, "SPS: offset_for_non_ref_pic");
                sps.offset_for_top_to_bottom_field = CAVLCReader.readSE(inb, "SPS: offset_for_top_to_bottom_field");
                sps.num_ref_frames_in_pic_order_cnt_cycle =CAVLCReader.readUE(inb, "SPS: num_ref_frames_in_pic_order_cnt_cycle");
                sps.offsetForRefFrame = new int[sps.num_ref_frames_in_pic_order_cnt_cycle];
                for (int i = 0; i < sps.num_ref_frames_in_pic_order_cnt_cycle; i++)
                {
                    sps.offsetForRefFrame[i] = CAVLCReader.readSE(inb, "SPS: offsetForRefFrame [" + i + "]");
                }
            }
            sps.num_ref_frames =CAVLCReader.readUE(inb, "SPS: num_ref_frames");
            sps.gaps_in_frame_num_value_allowed_flag =CAVLCReader.readBool(inb, "SPS: gaps_in_frame_num_value_allowed_flag");
            sps.pic_width_in_mbs_minus1 =CAVLCReader.readUE(inb, "SPS: pic_width_in_mbs_minus1");
            sps.pic_height_in_map_units_minus1 =CAVLCReader.readUE(inb, "SPS: pic_height_in_map_units_minus1");
            sps.frame_mbs_only_flag =CAVLCReader.readBool(inb, "SPS: frame_mbs_only_flag");
            if (!sps.frame_mbs_only_flag)
            {
                sps.mb_adaptive_frame_field_flag =CAVLCReader.readBool(inb, "SPS: mb_adaptive_frame_field_flag");
            }
            sps.direct_8x8_inference_flag =CAVLCReader.readBool(inb, "SPS: direct_8x8_inference_flag");
            sps.frame_cropping_flag =CAVLCReader.readBool(inb, "SPS: frame_cropping_flag");
            if (sps.frame_cropping_flag)
            {
                sps.frame_crop_left_offset =CAVLCReader.readUE(inb, "SPS: frame_crop_left_offset");
                sps.frame_crop_right_offset =CAVLCReader.readUE(inb, "SPS: frame_crop_right_offset");
                sps.frame_crop_top_offset =CAVLCReader.readUE(inb, "SPS: frame_crop_top_offset");
                sps.frame_crop_bottom_offset =CAVLCReader.readUE(inb, "SPS: frame_crop_bottom_offset");
            }
            bool vui_parameters_present_flag =CAVLCReader.readBool(inb, "SPS: vui_parameters_present_flag");
            if (vui_parameters_present_flag)
                sps.vuiParams = readVUIParameters(inb);

            return sps;
        }

        private static void readScalingListMatrix(BitReader inb, SeqParameterSet sps)
        {
            sps.scalingMatrix = new ScalingMatrix();
            for (int i = 0; i < 8; i++)
            {
                bool seqScalingListPresentFlag =CAVLCReader.readBool(inb, "SPS: seqScalingListPresentFlag");
                if (seqScalingListPresentFlag)
                {
                    sps.scalingMatrix.ScalingList4x4 = new ScalingList[8];
                    sps.scalingMatrix.ScalingList8x8 = new ScalingList[8];
                    if (i < 6)
                    {
                        sps.scalingMatrix.ScalingList4x4[i] = ScalingList.read(inb, 16);
                    }
                    else
                    {
                        sps.scalingMatrix.ScalingList8x8[i - 6] = ScalingList.read(inb, 64);
                    }
                }
            }
        }

        private static VUIParameters readVUIParameters(BitReader inb)
        {
            VUIParameters vuip = new VUIParameters();
            vuip.aspect_ratio_info_present_flag =CAVLCReader.readBool(inb, "VUI: aspect_ratio_info_present_flag");
            if (vuip.aspect_ratio_info_present_flag)
            {
                vuip.aspect_ratio = AspectRatio.fromValue((int)CAVLCReader.readNBit(inb, 8, "VUI: aspect_ratio"));
                if (vuip.aspect_ratio == AspectRatio.Extended_SAR)
                {
                    vuip.sar_width = (int)CAVLCReader.readNBit(inb, 16, "VUI: sar_width");
                    vuip.sar_height = (int)CAVLCReader.readNBit(inb, 16, "VUI: sar_height");
                }
            }
            vuip.overscan_info_present_flag =CAVLCReader.readBool(inb, "VUI: overscan_info_present_flag");
            if (vuip.overscan_info_present_flag)
            {
                vuip.overscan_appropriate_flag =CAVLCReader.readBool(inb, "VUI: overscan_appropriate_flag");
            }
            vuip.video_signal_type_present_flag =CAVLCReader.readBool(inb, "VUI: video_signal_type_present_flag");
            if (vuip.video_signal_type_present_flag)
            {
                vuip.video_format = (int)CAVLCReader.readNBit(inb, 3, "VUI: video_format");
                vuip.video_full_range_flag =CAVLCReader.readBool(inb, "VUI: video_full_range_flag");
                vuip.colour_description_present_flag =CAVLCReader.readBool(inb, "VUI: colour_description_present_flag");
                if (vuip.colour_description_present_flag)
                {
                    vuip.colour_primaries = (int)CAVLCReader.readNBit(inb, 8, "VUI: colour_primaries");
                    vuip.transfer_characteristics = (int)CAVLCReader.readNBit(inb, 8, "VUI: transfer_characteristics");
                    vuip.matrix_coefficients = (int)CAVLCReader.readNBit(inb, 8, "VUI: matrix_coefficients");
                }
            }
            vuip.chroma_loc_info_present_flag =CAVLCReader.readBool(inb, "VUI: chroma_loc_info_present_flag");
            if (vuip.chroma_loc_info_present_flag)
            {
                vuip.chroma_sample_loc_type_top_field =CAVLCReader.readUE(inb, "VUI chroma_sample_loc_type_top_field");
                vuip.chroma_sample_loc_type_bottom_field =CAVLCReader.readUE(inb, "VUI chroma_sample_loc_type_bottom_field");
            }
            vuip.timing_info_present_flag =CAVLCReader.readBool(inb, "VUI: timing_info_present_flag");
            if (vuip.timing_info_present_flag)
            {
                vuip.num_units_in_tick = (int)CAVLCReader.readNBit(inb, 32, "VUI: num_units_in_tick");
                vuip.time_scale = (int)CAVLCReader.readNBit(inb, 32, "VUI: time_scale");
                vuip.fixed_frame_rate_flag =CAVLCReader.readBool(inb, "VUI: fixed_frame_rate_flag");
            }
            bool nal_hrd_parameters_present_flag =CAVLCReader.readBool(inb, "VUI: nal_hrd_parameters_present_flag");
            if (nal_hrd_parameters_present_flag)
                vuip.nalHRDParams = readHRDParameters(inb);
            bool vcl_hrd_parameters_present_flag =CAVLCReader.readBool(inb, "VUI: vcl_hrd_parameters_present_flag");
            if (vcl_hrd_parameters_present_flag)
                vuip.vclHRDParams = readHRDParameters(inb);
            if (nal_hrd_parameters_present_flag || vcl_hrd_parameters_present_flag)
            {
                vuip.low_delay_hrd_flag =CAVLCReader.readBool(inb, "VUI: low_delay_hrd_flag");
            }
            vuip.pic_struct_present_flag =CAVLCReader.readBool(inb, "VUI: pic_struct_present_flag");
            bool bitstream_restriction_flag =CAVLCReader.readBool(inb, "VUI: bitstream_restriction_flag");
            if (bitstream_restriction_flag)
            {
                vuip.bitstreamRestriction = new VUIParameters.BitstreamRestriction();
                vuip.bitstreamRestriction.motion_vectors_over_pic_boundaries_flag =CAVLCReader.readBool(inb,
                        "VUI: motion_vectors_over_pic_boundaries_flag");
                vuip.bitstreamRestriction.max_bytes_per_pic_denom =CAVLCReader.readUE(inb, "VUI max_bytes_per_pic_denom");
                vuip.bitstreamRestriction.max_bits_per_mb_denom =CAVLCReader.readUE(inb, "VUI max_bits_per_mb_denom");
                vuip.bitstreamRestriction.log2_max_mv_length_horizontal =CAVLCReader.readUE(inb, "VUI log2_max_mv_length_horizontal");
                vuip.bitstreamRestriction.log2_max_mv_length_vertical =CAVLCReader.readUE(inb, "VUI log2_max_mv_length_vertical");
                vuip.bitstreamRestriction.num_reorder_frames =CAVLCReader.readUE(inb, "VUI num_reorder_frames");
                vuip.bitstreamRestriction.max_dec_frame_buffering =CAVLCReader.readUE(inb, "VUI max_dec_frame_buffering");
            }

            return vuip;
        }

        private static HRDParameters readHRDParameters(BitReader inb)
        {
            HRDParameters hrd = new HRDParameters();
            hrd.cpb_cnt_minus1 =CAVLCReader.readUE(inb, "SPS: cpb_cnt_minus1");
            hrd.bit_rate_scale = (int)CAVLCReader.readNBit(inb, 4, "HRD: bit_rate_scale");
            hrd.cpb_size_scale = (int)CAVLCReader.readNBit(inb, 4, "HRD: cpb_size_scale");
            hrd.bit_rate_value_minus1 = new int[hrd.cpb_cnt_minus1 + 1];
            hrd.cpb_size_value_minus1 = new int[hrd.cpb_cnt_minus1 + 1];
            hrd.cbr_flag = new bool[hrd.cpb_cnt_minus1 + 1];

            for (int SchedSelIdx = 0; SchedSelIdx <= hrd.cpb_cnt_minus1; SchedSelIdx++)
            {
                hrd.bit_rate_value_minus1[SchedSelIdx] =CAVLCReader.readUE(inb, "HRD: bit_rate_value_minus1");
                hrd.cpb_size_value_minus1[SchedSelIdx] =CAVLCReader.readUE(inb, "HRD: cpb_size_value_minus1");
                hrd.cbr_flag[SchedSelIdx] =CAVLCReader.readBool(inb, "HRD: cbr_flag");
            }
            hrd.initial_cpb_removal_delay_length_minus1 = (int)CAVLCReader.readNBit(inb, 5,
                    "HRD: initial_cpb_removal_delay_length_minus1");
            hrd.cpb_removal_delay_length_minus1 = (int)CAVLCReader.readNBit(inb, 5, "HRD: cpb_removal_delay_length_minus1");
            hrd.dpb_output_delay_length_minus1 = (int)CAVLCReader.readNBit(inb, 5, "HRD: dpb_output_delay_length_minus1");
            hrd.time_offset_length = (int)CAVLCReader.readNBit(inb, 5, "HRD: time_offset_length");
            return hrd;
        }

        public void write(MemoryStream outb)
        {
            BitWriter writer = new BitWriter(outb);

            CAVLCWriter.writeNBit(writer, profile_idc, 8, "SPS: profile_idc");
            CAVLCWriter.writeBool(writer, constraint_set_0_flag, "SPS: constraint_set_0_flag");
            CAVLCWriter.writeBool(writer, constraint_set_1_flag, "SPS: constraint_set_1_flag");
            CAVLCWriter.writeBool(writer, constraint_set_2_flag, "SPS: constraint_set_2_flag");
            CAVLCWriter.writeBool(writer, constraint_set_3_flag, "SPS: constraint_set_3_flag");
            CAVLCWriter.writeNBit(writer, 0, 4, "SPS: reserved");
            CAVLCWriter.writeNBit(writer, level_idc, 8, "SPS: level_idc");
            CAVLCWriter.writeUE(writer, seq_parameter_set_id, "SPS: seq_parameter_set_id");

            if (profile_idc == 100 || profile_idc == 110 || profile_idc == 122 || profile_idc == 144)
            {
                CAVLCWriter.writeUE(writer, fromColor(chroma_format_idc), "SPS: chroma_format_idc");
                if (chroma_format_idc == ColorSpace.YUV444)
                {
                    CAVLCWriter.writeBool(writer, residual_color_transform_flag, "SPS: residual_color_transform_flag");
                }
                CAVLCWriter.writeUE(writer, bit_depth_luma_minus8, "SPS: ");
                CAVLCWriter.writeUE(writer, bit_depth_chroma_minus8, "SPS: ");
                CAVLCWriter.writeBool(writer, qpprime_y_zero_transform_bypass_flag, "SPS: qpprime_y_zero_transform_bypass_flag");
                CAVLCWriter.writeBool(writer, scalingMatrix != null, "SPS: ");
                if (scalingMatrix != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (i < 6)
                        {
                            CAVLCWriter.writeBool(writer, (scalingMatrix.ScalingList4x4[i] != null), "SPS: ");
                            if (scalingMatrix.ScalingList4x4[i] != null)
                            {
                                scalingMatrix.ScalingList4x4[i].write(writer);
                            }
                        }
                        else
                        {
                            CAVLCWriter.writeBool(writer, (scalingMatrix.ScalingList8x8[i - 6] != null), "SPS: ");
                            if (scalingMatrix.ScalingList8x8[i - 6] != null)
                            {
                                scalingMatrix.ScalingList8x8[i - 6].write(writer);
                            }
                        }
                    }
                }
            }
            CAVLCWriter.writeUE(writer, log2_max_frame_num_minus4, "SPS: log2_max_frame_num_minus4");
            CAVLCWriter.writeUE(writer, pic_order_cnt_type, "SPS: pic_order_cnt_type");
            if (pic_order_cnt_type == 0)
            {
                CAVLCWriter.writeUE(writer, log2_max_pic_order_cnt_lsb_minus4, "SPS: log2_max_pic_order_cnt_lsb_minus4");
            }
            else if (pic_order_cnt_type == 1)
            {
                CAVLCWriter.writeBool(writer, delta_pic_order_always_zero_flag, "SPS: delta_pic_order_always_zero_flag");
                CAVLCWriter.writeSE(writer, offset_for_non_ref_pic, "SPS: offset_for_non_ref_pic");
                CAVLCWriter.writeSE(writer, offset_for_top_to_bottom_field, "SPS: offset_for_top_to_bottom_field");
                CAVLCWriter.writeUE(writer, offsetForRefFrame.Length, "SPS: ");
                for (int i = 0; i < offsetForRefFrame.Length; i++)
                    CAVLCWriter.writeSE(writer, offsetForRefFrame[i], "SPS: ");
            }
            CAVLCWriter.writeUE(writer, num_ref_frames, "SPS: num_ref_frames");
            CAVLCWriter.writeBool(writer, gaps_in_frame_num_value_allowed_flag, "SPS: gaps_in_frame_num_value_allowed_flag");
            CAVLCWriter.writeUE(writer, pic_width_in_mbs_minus1, "SPS: pic_width_in_mbs_minus1");
            CAVLCWriter.writeUE(writer, pic_height_in_map_units_minus1, "SPS: pic_height_in_map_units_minus1");
            CAVLCWriter.writeBool(writer, frame_mbs_only_flag, "SPS: frame_mbs_only_flag");
            if (!frame_mbs_only_flag)
            {
                CAVLCWriter.writeBool(writer, mb_adaptive_frame_field_flag, "SPS: mb_adaptive_frame_field_flag");
            }
            CAVLCWriter.writeBool(writer, direct_8x8_inference_flag, "SPS: direct_8x8_inference_flag");
            CAVLCWriter.writeBool(writer, frame_cropping_flag, "SPS: frame_cropping_flag");
            if (frame_cropping_flag)
            {
                CAVLCWriter.writeUE(writer, frame_crop_left_offset, "SPS: frame_crop_left_offset");
                CAVLCWriter.writeUE(writer, frame_crop_right_offset, "SPS: frame_crop_right_offset");
                CAVLCWriter.writeUE(writer, frame_crop_top_offset, "SPS: frame_crop_top_offset");
                CAVLCWriter.writeUE(writer, frame_crop_bottom_offset, "SPS: frame_crop_bottom_offset");
            }
            CAVLCWriter.writeBool(writer, vuiParams != null, "SPS: ");
            if (vuiParams != null)
                writeVUIParameters(vuiParams, writer);

            CAVLCWriter.writeTrailingBits(writer);
        }

        private void writeVUIParameters(VUIParameters vuip, BitWriter writer)
        {
            CAVLCWriter.writeBool(writer, vuip.aspect_ratio_info_present_flag, "VUI: aspect_ratio_info_present_flag");
            if (vuip.aspect_ratio_info_present_flag)
            {
                CAVLCWriter.writeNBit(writer, vuip.aspect_ratio.getValue(), 8, "VUI: aspect_ratio");
                if (vuip.aspect_ratio == AspectRatio.Extended_SAR)
                {
                    CAVLCWriter.writeNBit(writer, vuip.sar_width, 16, "VUI: sar_width");
                    CAVLCWriter.writeNBit(writer, vuip.sar_height, 16, "VUI: sar_height");
                }
            }
            CAVLCWriter.writeBool(writer, vuip.overscan_info_present_flag, "VUI: overscan_info_present_flag");
            if (vuip.overscan_info_present_flag)
            {
                CAVLCWriter.writeBool(writer, vuip.overscan_appropriate_flag, "VUI: overscan_appropriate_flag");
            }
            CAVLCWriter.writeBool(writer, vuip.video_signal_type_present_flag, "VUI: video_signal_type_present_flag");
            if (vuip.video_signal_type_present_flag)
            {
                CAVLCWriter.writeNBit(writer, vuip.video_format, 3, "VUI: video_format");
                CAVLCWriter.writeBool(writer, vuip.video_full_range_flag, "VUI: video_full_range_flag");
                CAVLCWriter.writeBool(writer, vuip.colour_description_present_flag, "VUI: colour_description_present_flag");
                if (vuip.colour_description_present_flag)
                {
                    CAVLCWriter.writeNBit(writer, vuip.colour_primaries, 8, "VUI: colour_primaries");
                    CAVLCWriter.writeNBit(writer, vuip.transfer_characteristics, 8, "VUI: transfer_characteristics");
                    CAVLCWriter.writeNBit(writer, vuip.matrix_coefficients, 8, "VUI: matrix_coefficients");
                }
            }
            CAVLCWriter.writeBool(writer, vuip.chroma_loc_info_present_flag, "VUI: chroma_loc_info_present_flag");
            if (vuip.chroma_loc_info_present_flag)
            {
                CAVLCWriter.writeUE(writer, vuip.chroma_sample_loc_type_top_field, "VUI: chroma_sample_loc_type_top_field");
                CAVLCWriter.writeUE(writer, vuip.chroma_sample_loc_type_bottom_field, "VUI: chroma_sample_loc_type_bottom_field");
            }
            CAVLCWriter.writeBool(writer, vuip.timing_info_present_flag, "VUI: timing_info_present_flag");
            if (vuip.timing_info_present_flag)
            {
                CAVLCWriter.writeNBit(writer, vuip.num_units_in_tick, 32, "VUI: num_units_in_tick");
                CAVLCWriter.writeNBit(writer, vuip.time_scale, 32, "VUI: time_scale");
                CAVLCWriter.writeBool(writer, vuip.fixed_frame_rate_flag, "VUI: fixed_frame_rate_flag");
            }
            CAVLCWriter.writeBool(writer, vuip.nalHRDParams != null, "VUI: ");
            if (vuip.nalHRDParams != null)
            {
                writeHRDParameters(vuip.nalHRDParams, writer);
            }
            CAVLCWriter.writeBool(writer, vuip.vclHRDParams != null, "VUI: ");
            if (vuip.vclHRDParams != null)
            {
                writeHRDParameters(vuip.vclHRDParams, writer);
            }

            if (vuip.nalHRDParams != null || vuip.vclHRDParams != null)
            {
                CAVLCWriter.writeBool(writer, vuip.low_delay_hrd_flag, "VUI: low_delay_hrd_flag");
            }
            CAVLCWriter.writeBool(writer, vuip.pic_struct_present_flag, "VUI: pic_struct_present_flag");
            CAVLCWriter.writeBool(writer, vuip.bitstreamRestriction != null, "VUI: ");
            if (vuip.bitstreamRestriction != null)
            {
                CAVLCWriter.writeBool(writer, vuip.bitstreamRestriction.motion_vectors_over_pic_boundaries_flag,
                        "VUI: motion_vectors_over_pic_boundaries_flag");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.max_bytes_per_pic_denom, "VUI: max_bytes_per_pic_denom");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.max_bits_per_mb_denom, "VUI: max_bits_per_mb_denom");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.log2_max_mv_length_horizontal,
                        "VUI: log2_max_mv_length_horizontal");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.log2_max_mv_length_vertical, "VUI: log2_max_mv_length_vertical");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.num_reorder_frames, "VUI: num_reorder_frames");
                CAVLCWriter.writeUE(writer, vuip.bitstreamRestriction.max_dec_frame_buffering, "VUI: max_dec_frame_buffering");
            }

        }

        private void writeHRDParameters(HRDParameters hrd, BitWriter writer)
        {
            CAVLCWriter.writeUE(writer, hrd.cpb_cnt_minus1, "HRD: cpb_cnt_minus1");
            CAVLCWriter.writeNBit(writer, hrd.bit_rate_scale, 4, "HRD: bit_rate_scale");
            CAVLCWriter.writeNBit(writer, hrd.cpb_size_scale, 4, "HRD: cpb_size_scale");

            for (int SchedSelIdx = 0; SchedSelIdx <= hrd.cpb_cnt_minus1; SchedSelIdx++)
            {
                CAVLCWriter.writeUE(writer, hrd.bit_rate_value_minus1[SchedSelIdx], "HRD: ");
                CAVLCWriter.writeUE(writer, hrd.cpb_size_value_minus1[SchedSelIdx], "HRD: ");
                CAVLCWriter.writeBool(writer, hrd.cbr_flag[SchedSelIdx], "HRD: ");
            }
            CAVLCWriter.writeNBit(writer, hrd.initial_cpb_removal_delay_length_minus1, 5,
                    "HRD: initial_cpb_removal_delay_length_minus1");
            CAVLCWriter.writeNBit(writer, hrd.cpb_removal_delay_length_minus1, 5, "HRD: cpb_removal_delay_length_minus1");
            CAVLCWriter.writeNBit(writer, hrd.dpb_output_delay_length_minus1, 5, "HRD: dpb_output_delay_length_minus1");
            CAVLCWriter.writeNBit(writer, hrd.time_offset_length, 5, "HRD: time_offset_length");
        }

        public SeqParameterSet copy()
        {
            MemoryStream buf = new MemoryStream(2048);
            write(buf);
            buf.flip();
            return read(buf);
        }
    }
}
