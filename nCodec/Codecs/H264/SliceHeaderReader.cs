using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class SliceHeaderReader
    {

        public SliceHeader readPart1(BitReader inb)
        {

            SliceHeader sh = new SliceHeader();
            sh.first_mb_in_slice = CAVLCReader.readUE(inb, "SH: first_mb_inb_slice");
            int sh_type = CAVLCReader.readUE(inb, "SH: slice_type");
            sh.slice_type = (SliceType)(sh_type % 5);
            sh.slice_type_restr = (sh_type / 5) > 0;

            sh.pic_parameter_set_id = CAVLCReader.readUE(inb, "SH: pic_parameter_set_id");

            return sh;
        }

        public SliceHeader readPart2(SliceHeader sh, NALUnit nalUnit, SeqParameterSet sps, PictureParameterSet pps,
                BitReader inb)
        {
            sh.pps = pps;
            sh.sps = sps;

            sh.frame_num = CAVLCReader.readU(inb, sps.log2_max_frame_num_minus4 + 4, "SH: frame_num");
            if (!sps.frame_mbs_only_flag)
            {
                sh.field_pic_flag = CAVLCReader.readBool(inb, "SH: field_pic_flag");
                if (sh.field_pic_flag)
                {
                    sh.bottom_field_flag = CAVLCReader.readBool(inb, "SH: bottom_field_flag");
                }
            }
            if (nalUnit.type == NALUnitType.IDR_SLICE)
            {
                sh.idr_pic_id = CAVLCReader.readUE(inb, "SH: idr_pic_id");
            }
            if (sps.pic_order_cnt_type == 0)
            {
                sh.pic_order_cnt_lsb = CAVLCReader.readU(inb, sps.log2_max_pic_order_cnt_lsb_minus4 + 4, "SH: pic_order_cnt_lsb");
                if (pps.pic_order_present_flag && !sps.field_pic_flag)
                {
                    sh.delta_pic_order_cnt_bottom = CAVLCReader.readSE(inb, "SH: delta_pic_order_cnt_bottom");
                }
            }
            sh.delta_pic_order_cnt = new int[2];
            if (sps.pic_order_cnt_type == 1 && !sps.delta_pic_order_always_zero_flag)
            {
                sh.delta_pic_order_cnt[0] = CAVLCReader.readSE(inb, "SH: delta_pic_order_cnt[0]");
                if (pps.pic_order_present_flag && !sps.field_pic_flag)
                    sh.delta_pic_order_cnt[1] = CAVLCReader.readSE(inb, "SH: delta_pic_order_cnt[1]");
            }
            if (pps.redundant_pic_cnt_present_flag)
            {
                sh.redundant_pic_cnt = CAVLCReader.readUE(inb, "SH: redundant_pic_cnt");
            }
            if (sh.slice_type == SliceType.B)
            {
                sh.direct_spatial_mv_pred_flag = CAVLCReader.readBool(inb, "SH: direct_spatial_mv_pred_flag");
            }
            if (sh.slice_type == SliceType.P || sh.slice_type == SliceType.SP || sh.slice_type == SliceType.B)
            {
                sh.num_ref_idx_active_override_flag = CAVLCReader.readBool(inb, "SH: num_ref_idx_active_override_flag");
                if (sh.num_ref_idx_active_override_flag)
                {
                    sh.num_ref_idx_active_minus1[0] = CAVLCReader.readUE(inb, "SH: num_ref_idx_l0_active_minus1");
                    if (sh.slice_type == SliceType.B)
                    {
                        sh.num_ref_idx_active_minus1[1] = CAVLCReader.readUE(inb, "SH: num_ref_idx_l1_active_minus1");
                    }
                }
            }
            readRefPicListReorderinbg(sh, inb);
            if ((pps.weighted_pred_flag && (sh.slice_type == SliceType.P || sh.slice_type == SliceType.SP))
                    || (pps.weighted_bipred_idc == 1 && sh.slice_type == SliceType.B))
                readPredWeightTable(sps, pps, sh, inb);
            if (nalUnit.nal_ref_idc != 0)
                readDecoderPicMarkinbg(nalUnit, sh, inb);
            if (pps.entropy_coding_mode_flag && sh.slice_type.isInter())
            {
                sh.cabac_init_idc = CAVLCReader.readUE(inb, "SH: cabac_inbit_idc");
            }
            sh.slice_qp_delta = CAVLCReader.readSE(inb, "SH: slice_qp_delta");
            if (sh.slice_type == SliceType.SP || sh.slice_type == SliceType.SI)
            {
                if (sh.slice_type == SliceType.SP)
                {
                    sh.sp_for_switch_flag = CAVLCReader.readBool(inb, "SH: sp_for_switch_flag");
                }
                sh.slice_qs_delta = CAVLCReader.readSE(inb, "SH: slice_qs_delta");
            }
            if (pps.deblocking_filter_control_present_flag)
            {
                sh.disable_deblocking_filter_idc = CAVLCReader.readUE(inb, "SH: disable_deblockinbg_filter_idc");
                if (sh.disable_deblocking_filter_idc != 1)
                {
                    sh.slice_alpha_c0_offset_div2 = CAVLCReader.readSE(inb, "SH: slice_alpha_c0_offset_div2");
                    sh.slice_beta_offset_div2 = CAVLCReader.readSE(inb, "SH: slice_beta_offset_div2");
                }
            }
            if (pps.num_slice_groups_minus1 > 0 && pps.slice_group_map_type >= 3 && pps.slice_group_map_type <= 5)
            {
                int len = Utility.getPicHeightInMbs(sps) * (sps.pic_width_in_mbs_minus1 + 1)
                        / (pps.slice_group_change_rate_minus1 + 1);
                if ((Utility.getPicHeightInMbs(sps) * (sps.pic_width_in_mbs_minus1 + 1))
                        % (pps.slice_group_change_rate_minus1 + 1) > 0)
                    len += 1;

                len = CeilLog2(len + 1);
                sh.slice_group_change_cycle = CAVLCReader.readU(inb, len, "SH: slice_group_change_cycle");
            }

            return sh;
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

        // static int i = 0;

        private static void readDecoderPicMarkinbg(NALUnit nalUnit, SliceHeader sh, BitReader inb)
        {
            if (nalUnit.type == NALUnitType.IDR_SLICE)
            {
                bool no_output_of_prior_pics_flag = CAVLCReader.readBool(inb, "SH: no_output_of_prior_pics_flag");
                bool long_term_reference_flag = CAVLCReader.readBool(inb, "SH: long_term_reference_flag");
                sh.refPicMarkingIDR = new nVideo.Codecs.H264.RefPicMarkingIDR(no_output_of_prior_pics_flag, long_term_reference_flag);
            }
            else
            {
                bool adaptive_ref_pic_markinbg_mode_flag = CAVLCReader.readBool(inb, "SH: adaptive_ref_pic_markinbg_mode_flag");
                if (adaptive_ref_pic_markinbg_mode_flag)
                {
                    List<nVideo.Codecs.H264.RefPicMarking.Instruction> mmops = new List<nVideo.Codecs.H264.RefPicMarking.Instruction>();
                    int memory_management_control_operation;
                    do
                    {
                        memory_management_control_operation = CAVLCReader.readUE(inb, "SH: memory_management_control_operation");

                        nVideo.Codecs.H264.RefPicMarking.Instruction inbstr = null;



                        switch (memory_management_control_operation)
                        {
                            case 1:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.REMOVE_SHORT, CAVLCReader.readUE(inb,
                                    "SH: difference_of_pic_nums_minus1") + 1, 0);
                                break;
                            case 2:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.REMOVE_LONG,
                                        CAVLCReader.readUE(inb, "SH: long_term_pic_num"), 0);
                                break;
                            case 3:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.CONVERT_INTO_LONG, CAVLCReader.readUE(inb,
                                        "SH: difference_of_pic_nums_minus1") + 1, CAVLCReader.readUE(inb, "SH: long_term_frame_idx"));
                                break;
                            case 4:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.TRUNK_LONG, CAVLCReader.readUE(inb,
                                        "SH: max_long_term_frame_idx_plus1") - 1, 0);
                                break;
                            case 5:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.CLEAR, 0, 0);
                                break;
                            case 6:
                                inbstr = new nVideo.Codecs.H264.RefPicMarking.Instruction(nVideo.Codecs.H264.RefPicMarking.InstrType.MARK_LONG,
                                        CAVLCReader.readUE(inb, "SH: long_term_frame_idx"), 0);
                                break;
                        }
                        if (inbstr != null)
                            mmops.Add(inbstr);
                    } while (memory_management_control_operation != 0);
                    sh.refPicMarkingNonIDR = new RefPicMarking(mmops.ToArray());
                }
            }
        }

        private static void readPredWeightTable(SeqParameterSet sps, PictureParameterSet pps, SliceHeader sh, BitReader inb)
        {
            sh.pred_weight_table = new PredictionWeightTable();
            int[] numRefsminus1 = sh.num_ref_idx_active_override_flag ? sh.num_ref_idx_active_minus1
                    : pps.num_ref_idx_active_minus1;
            int[] nr = new int[] { numRefsminus1[0] + 1, numRefsminus1[1] + 1 };

            sh.pred_weight_table.luma_log2_weight_denom = CAVLCReader.readUE(inb, "SH: luma_log2_weight_denom");
            if (sps.chroma_format_idc != ColorSpace.MONO)
            {
                sh.pred_weight_table.chroma_log2_weight_denom = CAVLCReader.readUE(inb, "SH: chroma_log2_weight_denom");
            }
            int defaultLW = 1 << sh.pred_weight_table.luma_log2_weight_denom;
            int defaultCW = 1 << sh.pred_weight_table.chroma_log2_weight_denom;

            for (int list = 0; list < 2; list++)
            {
                sh.pred_weight_table.luma_weight[list] = new int[nr[list]];
                sh.pred_weight_table.luma_offset[list] = new int[nr[list]];
                sh.pred_weight_table.chroma_weight[list] = (int[][])System.Array.CreateInstance(typeof(int), new int[] { 2, nr[list] });
                sh.pred_weight_table.chroma_offset[list] = (int[][])System.Array.CreateInstance(typeof(int), new int[] { 2, nr[list] });
                for (int i = 0; i < nr[list]; i++)
                {
                    sh.pred_weight_table.luma_weight[list][i] = defaultLW;
                    sh.pred_weight_table.luma_offset[list][i] = 0;
                    sh.pred_weight_table.chroma_weight[list][0][i] = defaultCW;
                    sh.pred_weight_table.chroma_offset[list][0][i] = 0;
                    sh.pred_weight_table.chroma_weight[list][1][i] = defaultCW;
                    sh.pred_weight_table.chroma_offset[list][1][i] = 0;
                }
            }

            readWeightOffset(sps, pps, sh, inb, nr, 0);
            if (sh.slice_type == SliceType.B)
            {
                readWeightOffset(sps, pps, sh, inb, nr, 1);
            }
        }

        private static void readWeightOffset(SeqParameterSet sps, PictureParameterSet pps, SliceHeader sh, BitReader inb,
                int[] numRefs, int list)
        {

            for (int i = 0; i < numRefs[list]; i++)
            {
                bool luma_weight_l0_flag = CAVLCReader.readBool(inb, "SH: luma_weight_l0_flag");
                if (luma_weight_l0_flag)
                {
                    sh.pred_weight_table.luma_weight[list][i] = CAVLCReader.readSE(inb, "SH: weight");
                    sh.pred_weight_table.luma_offset[list][i] = CAVLCReader.readSE(inb, "SH: offset");
                }
                if (sps.chroma_format_idc != ColorSpace.MONO)
                {
                    bool chroma_weight_l0_flag = CAVLCReader.readBool(inb, "SH: chroma_weight_l0_flag");
                    if (chroma_weight_l0_flag)
                    {
                        sh.pred_weight_table.chroma_weight[list][0][i] = CAVLCReader.readSE(inb, "SH: weight");
                        sh.pred_weight_table.chroma_offset[list][0][i] = CAVLCReader.readSE(inb, "SH: offset");
                        sh.pred_weight_table.chroma_weight[list][1][i] = CAVLCReader.readSE(inb, "SH: weight");
                        sh.pred_weight_table.chroma_offset[list][1][i] = CAVLCReader.readSE(inb, "SH: offset");
                    }
                }
            }
        }

        private static void readRefPicListReorderinbg(SliceHeader sh, BitReader inb)
        {
            sh.refPicReordering = new int[2][][];
            // System.out.println(i++);
            if (sh.slice_type.isInter())
            {
                bool ref_pic_list_reorderinbg_flag_l0 = CAVLCReader.readBool(inb, "SH: ref_pic_list_reorderinbg_flag_l0");
                if (ref_pic_list_reorderinbg_flag_l0)
                {
                    sh.refPicReordering[0] = readReorderinbgEntries(inb);
                }
            }
            if (sh.slice_type == SliceType.B)
            {
                bool ref_pic_list_reorderinbg_flag_l1 = CAVLCReader.readBool(inb, "SH: ref_pic_list_reorderinbg_flag_l1");
                if (ref_pic_list_reorderinbg_flag_l1)
                {
                    sh.refPicReordering[1] = readReorderinbgEntries(inb);
                }
            }
        }

        private static int[][] readReorderinbgEntries(BitReader inb)
        {
            List<int> ops = new List<int>();
            List<int> args = new List<int>();
            do
            {
                int idc = CAVLCReader.readUE(inb, "SH: reorderinbg_of_pic_nums_idc");
                if (idc == 3)
                    break;
                ops.Add(idc);
                args.Add(CAVLCReader.readUE(inb, "SH: abs_diff_pic_num_minus1"));
            } while (true);
            return new int[][] { ops.ToArray(), args.ToArray() };
        }
    }
}
