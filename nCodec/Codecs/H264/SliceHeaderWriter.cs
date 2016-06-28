﻿using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class SliceHeaderWriter
    {
        public void write(SliceHeader sliceHeader, bool idrSlice, int nalRefIdc, BitWriter writer)
        {
            SeqParameterSet sps = sliceHeader.sps;
            PictureParameterSet pps = sliceHeader.pps;
            CAVLCWriter.writeUE(writer, sliceHeader.first_mb_in_slice, "SH: first_mb_in_slice");
            CAVLCWriter.writeUE(writer, (int)sliceHeader.slice_type + (sliceHeader.slice_type_restr ? 5 : 0), "SH: slice_type");
            CAVLCWriter.writeUE(writer, sliceHeader.pic_parameter_set_id, "SH: pic_parameter_set_id");
            CAVLCWriter.writeU(writer, sliceHeader.frame_num, sps.log2_max_frame_num_minus4 + 4, "SH: frame_num");
            if (!sps.frame_mbs_only_flag)
            {
                CAVLCWriter.writeBool(writer, sliceHeader.field_pic_flag, "SH: field_pic_flag");
                if (sliceHeader.field_pic_flag)
                {
                    CAVLCWriter.writeBool(writer, sliceHeader.bottom_field_flag, "SH: bottom_field_flag");
                }
            }
            if (idrSlice)
            {
                CAVLCWriter.writeUE(writer, sliceHeader.idr_pic_id, "SH: idr_pic_id");
            }
            if (sps.pic_order_cnt_type == 0)
            {
                CAVLCWriter.writeU(writer, sliceHeader.pic_order_cnt_lsb, sps.log2_max_pic_order_cnt_lsb_minus4 + 4);
                if (pps.pic_order_present_flag && !sps.field_pic_flag)
                {
                    CAVLCWriter.writeSE(writer, sliceHeader.delta_pic_order_cnt_bottom, "SH: delta_pic_order_cnt_bottom");
                }
            }
            if (sps.pic_order_cnt_type == 1 && !sps.delta_pic_order_always_zero_flag)
            {
                CAVLCWriter.writeSE(writer, sliceHeader.delta_pic_order_cnt[0], "SH: delta_pic_order_cnt");
                if (pps.pic_order_present_flag && !sps.field_pic_flag)
                    CAVLCWriter.writeSE(writer, sliceHeader.delta_pic_order_cnt[1], "SH: delta_pic_order_cnt");
            }
            if (pps.redundant_pic_cnt_present_flag)
            {
                CAVLCWriter.writeUE(writer, sliceHeader.redundant_pic_cnt, "SH: redundant_pic_cnt");
            }
            if (sliceHeader.slice_type == SliceType.B)
            {
                CAVLCWriter.writeBool(writer, sliceHeader.direct_spatial_mv_pred_flag, "SH: direct_spatial_mv_pred_flag");
            }
            if (sliceHeader.slice_type == SliceType.P || sliceHeader.slice_type == SliceType.SP
                    || sliceHeader.slice_type == SliceType.B)
            {
                CAVLCWriter.writeBool(writer, sliceHeader.num_ref_idx_active_override_flag, "SH: num_ref_idx_active_override_flag");
                if (sliceHeader.num_ref_idx_active_override_flag)
                {
                    CAVLCWriter.writeUE(writer, sliceHeader.num_ref_idx_active_minus1[0], "SH: num_ref_idx_l0_active_minus1");
                    if (sliceHeader.slice_type == SliceType.B)
                    {
                        CAVLCWriter.writeUE(writer, sliceHeader.num_ref_idx_active_minus1[1], "SH: num_ref_idx_l1_active_minus1");
                    }
                }
            }
            writeRefPicListReordering(sliceHeader, writer);
            if ((pps.weighted_pred_flag && (sliceHeader.slice_type == SliceType.P || sliceHeader.slice_type == SliceType.SP))
                    || (pps.weighted_bipred_idc == 1 && sliceHeader.slice_type == SliceType.B))
                writePredWeightTable(sliceHeader, writer);
            if (nalRefIdc != 0)
                writeDecRefPicMarking(sliceHeader, idrSlice, writer);
            if (pps.entropy_coding_mode_flag && sliceHeader.slice_type.isInter())
            {
                CAVLCWriter.writeUE(writer, sliceHeader.cabac_init_idc, "SH: cabac_init_idc");
            }
            CAVLCWriter.writeSE(writer, sliceHeader.slice_qp_delta, "SH: slice_qp_delta");
            if (sliceHeader.slice_type == SliceType.SP || sliceHeader.slice_type == SliceType.SI)
            {
                if (sliceHeader.slice_type == SliceType.SP)
                {
                    CAVLCWriter.writeBool(writer, sliceHeader.sp_for_switch_flag, "SH: sp_for_switch_flag");
                }
                CAVLCWriter.writeSE(writer, sliceHeader.slice_qs_delta, "SH: slice_qs_delta");
            }
            if (pps.deblocking_filter_control_present_flag)
            {
                CAVLCWriter.writeUE(writer, sliceHeader.disable_deblocking_filter_idc, "SH: disable_deblocking_filter_idc");
                if (sliceHeader.disable_deblocking_filter_idc != 1)
                {
                    CAVLCWriter.writeSE(writer, sliceHeader.slice_alpha_c0_offset_div2, "SH: slice_alpha_c0_offset_div2");
                    CAVLCWriter.writeSE(writer, sliceHeader.slice_beta_offset_div2, "SH: slice_beta_offset_div2");
                }
            }
            if (pps.num_slice_groups_minus1 > 0 && pps.slice_group_map_type >= 3 && pps.slice_group_map_type <= 5)
            {
                int len = (sps.pic_height_in_map_units_minus1 + 1) * (sps.pic_width_in_mbs_minus1 + 1)
                        / (pps.slice_group_change_rate_minus1 + 1);
                if (((sps.pic_height_in_map_units_minus1 + 1) * (sps.pic_width_in_mbs_minus1 + 1))
                        % (pps.slice_group_change_rate_minus1 + 1) > 0)
                    len += 1;

                len = CeilLog2(len + 1);
                CAVLCWriter.writeU(writer, sliceHeader.slice_group_change_cycle, len);
            }

        }

        private static int CeilLog2(int uiVal)
        {
            int uiTmp = uiVal - 1;
            int uiRet = 0;

            while (uiTmp != 0)
            {
                uiTmp >>= 1;
                uiRet++;
            }
            return uiRet;
        }

        private void writeDecRefPicMarking(SliceHeader sliceHeader, bool idrSlice, BitWriter writer)
        {
            if (idrSlice)
            {
                RefPicMarkingIDR drpmidr = sliceHeader.refPicMarkingIDR;
                CAVLCWriter.writeBool(writer, drpmidr.isDiscardDecodedPics(), "SH: no_output_of_prior_pics_flag");
                CAVLCWriter.writeBool(writer, drpmidr.isUseForlongTerm(), "SH: long_term_reference_flag");
            }
            else
            {
                CAVLCWriter.writeBool(writer, sliceHeader.refPicMarkingNonIDR != null, "SH: adaptive_ref_pic_marking_mode_flag");
                if (sliceHeader.refPicMarkingNonIDR != null)
                {
                    RefPicMarking drpmidr = sliceHeader.refPicMarkingNonIDR;
                    foreach (var mmop in drpmidr.getInstructions())
                    {
                        switch (mmop.getType())
                        {
                            case RefPicMarking.InstrType.REMOVE_SHORT:
                                CAVLCWriter.writeUE(writer, 1, "SH: memory_management_control_operation");
                                CAVLCWriter.writeUE(writer, mmop.getArg1() - 1, "SH: difference_of_pic_nums_minus1");
                                break;
                            case RefPicMarking.InstrType.REMOVE_LONG:
                                CAVLCWriter.writeUE(writer, 2, "SH: memory_management_control_operation");
                                CAVLCWriter.writeUE(writer, mmop.getArg1(), "SH: long_term_pic_num");
                                break;
                            case RefPicMarking.InstrType.CONVERT_INTO_LONG:
                                CAVLCWriter.writeUE(writer, 3, "SH: memory_management_control_operation");
                                CAVLCWriter.writeUE(writer, mmop.getArg1() - 1, "SH: difference_of_pic_nums_minus1");
                                CAVLCWriter.writeUE(writer, mmop.getArg2(), "SH: long_term_frame_idx");
                                break;
                            case RefPicMarking.InstrType.TRUNK_LONG:
                                CAVLCWriter.writeUE(writer, 4, "SH: memory_management_control_operation");
                                CAVLCWriter.writeUE(writer, mmop.getArg1() + 1, "SH: max_long_term_frame_idx_plus1");
                                break;
                            case RefPicMarking.InstrType.CLEAR:
                                CAVLCWriter.writeUE(writer, 5, "SH: memory_management_control_operation");
                                break;
                            case RefPicMarking.InstrType.MARK_LONG:
                                CAVLCWriter.writeUE(writer, 6, "SH: memory_management_control_operation");
                                CAVLCWriter.writeUE(writer, mmop.getArg1(), "SH: long_term_frame_idx");
                                break;
                        }
                    }
                    CAVLCWriter.writeUE(writer, 0, "SH: memory_management_control_operation");
                }
            }
        }

        private void writePredWeightTable(SliceHeader sliceHeader, BitWriter writer)
        {
            SeqParameterSet sps = sliceHeader.sps;
            CAVLCWriter.writeUE(writer, sliceHeader.pred_weight_table.luma_log2_weight_denom, "SH: luma_log2_weight_denom");
            if (sps.chroma_format_idc != ColorSpace.MONO)
            {
                CAVLCWriter.writeUE(writer, sliceHeader.pred_weight_table.chroma_log2_weight_denom, "SH: chroma_log2_weight_denom");
            }

            writeOffsetWeight(sliceHeader, writer, 0);
            if (sliceHeader.slice_type == SliceType.B)
            {
                writeOffsetWeight(sliceHeader, writer, 1);
            }
        }

        private void writeOffsetWeight(SliceHeader sliceHeader, BitWriter writer, int list)
        {
            SeqParameterSet sps = sliceHeader.sps;
            int defaultLW = 1 << sliceHeader.pred_weight_table.luma_log2_weight_denom;
            int defaultCW = 1 << sliceHeader.pred_weight_table.chroma_log2_weight_denom;

            for (int i = 0; i < sliceHeader.pred_weight_table.luma_weight[list].Length; i++)
            {
                bool flagLuma = sliceHeader.pred_weight_table.luma_weight[list][i] != defaultLW
                        || sliceHeader.pred_weight_table.luma_offset[list][i] != 0;
                CAVLCWriter.writeBool(writer, flagLuma, "SH: luma_weight_l0_flag");
                if (flagLuma)
                {
                    CAVLCWriter.writeSE(writer, sliceHeader.pred_weight_table.luma_weight[list][i], "SH: luma_weight_l" + list);
                    CAVLCWriter.writeSE(writer, sliceHeader.pred_weight_table.luma_offset[list][i], "SH: luma_offset_l" + list);
                }
                if (sps.chroma_format_idc != ColorSpace.MONO)
                {
                    bool flagChroma = sliceHeader.pred_weight_table.chroma_weight[list][0][i] != defaultCW
                            || sliceHeader.pred_weight_table.chroma_offset[list][0][i] != 0
                            || sliceHeader.pred_weight_table.chroma_weight[list][1][i] != defaultCW
                            || sliceHeader.pred_weight_table.chroma_offset[list][1][i] != 0;
                    CAVLCWriter.writeBool(writer, flagChroma, "SH: chroma_weight_l0_flag");
                    if (flagChroma)
                        for (int j = 0; j < 2; j++)
                        {
                            CAVLCWriter.writeSE(writer, sliceHeader.pred_weight_table.chroma_weight[list][j][i], "SH: chroma_weight_l" + list);
                            CAVLCWriter.writeSE(writer, sliceHeader.pred_weight_table.chroma_offset[list][j][i], "SH: chroma_offset_l" + list);
                        }
                }
            }
        }

        private void writeRefPicListReordering(SliceHeader sliceHeader, BitWriter writer)
        {
            if (sliceHeader.slice_type.isInter())
            {
                CAVLCWriter.writeBool(writer, sliceHeader.refPicReordering[0] != null, "SH: ref_pic_list_reordering_flag_l0");
                writeReorderingList(sliceHeader.refPicReordering[0], writer);
            }
            if (sliceHeader.slice_type == SliceType.B)
            {
                CAVLCWriter.writeBool(writer, sliceHeader.refPicReordering[1] != null, "SH: ref_pic_list_reordering_flag_l1");
                writeReorderingList(sliceHeader.refPicReordering[1], writer);
            }
        }

        private void writeReorderingList(int[][] reordering, BitWriter writer)
        {
            if (reordering == null)
                return;

            for (int i = 0; i < reordering[0].Length; i++)
            {
                CAVLCWriter.writeUE(writer, reordering[0][i], "SH: reordering_of_pic_nums_idc");
                CAVLCWriter.writeUE(writer, reordering[1][i], "SH: abs_diff_pic_num_minus1");
            }
            CAVLCWriter.writeUE(writer, 3, "SH: reordering_of_pic_nums_idc");
        }
    }
}
