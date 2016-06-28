using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class PictureParameterSet
    {

        public class PPSExt
        {
            public bool transform_8x8_mode_flag;
            public ScalingMatrix scalindMatrix;
            public int second_chroma_qp_index_offset;
            public bool[] pic_scaling_list_present_flag;
        }

        public bool entropy_coding_mode_flag;
        public int[] num_ref_idx_active_minus1 = new int[2];
        public int slice_group_change_rate_minus1;
        public int pic_parameter_set_id;
        public int seq_parameter_set_id;
        public bool pic_order_present_flag;
        public int num_slice_groups_minus1;
        public int slice_group_map_type;
        public bool weighted_pred_flag;
        public int weighted_bipred_idc;
        public int pic_init_qp_minus26;
        public int pic_init_qs_minus26;
        public int chroma_qp_index_offset;
        public bool deblocking_filter_control_present_flag;
        public bool constrained_intra_pred_flag;
        public bool redundant_pic_cnt_present_flag;
        public int[] top_left;
        public int[] bottom_right;
        public int[] run_length_minus1;
        public bool slice_group_change_direction_flag;
        public int[] slice_group_id;
        public PPSExt extended;

        public static PictureParameterSet read(MemoryStream iss)
        {
            BitReader inb = new BitReader(iss);
            PictureParameterSet pps = new PictureParameterSet();

            pps.pic_parameter_set_id = CAVLCReader.readUE(inb, "PPS: pic_parameter_set_id");
            pps.seq_parameter_set_id = CAVLCReader.readUE(inb, "PPS: seq_parameter_set_id");
            pps.entropy_coding_mode_flag = CAVLCReader.readBool(inb, "PPS: entropy_coding_mode_flag");
            pps.pic_order_present_flag = CAVLCReader.readBool(inb, "PPS: pic_order_present_flag");
            pps.num_slice_groups_minus1 = CAVLCReader.readUE(inb, "PPS: num_slice_groups_minus1");
            if (pps.num_slice_groups_minus1 > 0)
            {
                pps.slice_group_map_type = CAVLCReader.readUE(inb, "PPS: slice_group_map_type");
                pps.top_left = new int[pps.num_slice_groups_minus1 + 1];
                pps.bottom_right = new int[pps.num_slice_groups_minus1 + 1];
                pps.run_length_minus1 = new int[pps.num_slice_groups_minus1 + 1];
                if (pps.slice_group_map_type == 0)
                    for (int iGroup = 0; iGroup <= pps.num_slice_groups_minus1; iGroup++)
                        pps.run_length_minus1[iGroup] = CAVLCReader.readUE(inb, "PPS: run_length_minus1");
                else if (pps.slice_group_map_type == 2)
                    for (int iGroup = 0; iGroup < pps.num_slice_groups_minus1; iGroup++)
                    {
                        pps.top_left[iGroup] = CAVLCReader.readUE(inb, "PPS: top_left");
                        pps.bottom_right[iGroup] = CAVLCReader.readUE(inb, "PPS: bottom_right");
                    }
                else if (pps.slice_group_map_type == 3 || pps.slice_group_map_type == 4 || pps.slice_group_map_type == 5)
                {
                    pps.slice_group_change_direction_flag = CAVLCReader.readBool(inb, "PPS: slice_group_change_direction_flag");
                    pps.slice_group_change_rate_minus1 = CAVLCReader.readUE(inb, "PPS: slice_group_change_rate_minus1");
                }
                else if (pps.slice_group_map_type == 6)
                {
                    int NumberBitsPerSliceGroupId;
                    if (pps.num_slice_groups_minus1 + 1 > 4)
                        NumberBitsPerSliceGroupId = 3;
                    else if (pps.num_slice_groups_minus1 + 1 > 2)
                        NumberBitsPerSliceGroupId = 2;
                    else
                        NumberBitsPerSliceGroupId = 1;
                    int pic_size_in_map_units_minus1 = CAVLCReader.readUE(inb, "PPS: pic_size_in_map_units_minus1");
                    pps.slice_group_id = new int[pic_size_in_map_units_minus1 + 1];
                    for (int i = 0; i <= pic_size_in_map_units_minus1; i++)
                    {
                        pps.slice_group_id[i] = CAVLCReader.readU(inb, NumberBitsPerSliceGroupId, "PPS: slice_group_id [" + i + "]f");
                    }
                }
            }
            pps.num_ref_idx_active_minus1 = new int[] { CAVLCReader.readUE(inb, "PPS: num_ref_idx_l0_active_minus1"), CAVLCReader.readUE(inb, "PPS: num_ref_idx_l1_active_minus1") };
            pps.weighted_pred_flag = CAVLCReader.readBool(inb, "PPS: weighted_pred_flag");
            pps.weighted_bipred_idc = CAVLCReader.readNBit(inb, 2, "PPS: weighted_bipred_idc");
            pps.pic_init_qp_minus26 = CAVLCReader.readSE(inb, "PPS: pic_init_qp_minus26");
            pps.pic_init_qs_minus26 = CAVLCReader.readSE(inb, "PPS: pic_init_qs_minus26");
            pps.chroma_qp_index_offset = CAVLCReader.readSE(inb, "PPS: chroma_qp_index_offset");
            pps.deblocking_filter_control_present_flag = CAVLCReader.readBool(inb, "PPS: deblocking_filter_control_present_flag");
            pps.constrained_intra_pred_flag = CAVLCReader.readBool(inb, "PPS: constrained_intra_pred_flag");
            pps.redundant_pic_cnt_present_flag = CAVLCReader.readBool(inb, "PPS: redundant_pic_cnt_present_flag");
            if (CAVLCReader.moreRBSPData(inb))
            {
                pps.extended = new PictureParameterSet.PPSExt();
                pps.extended.transform_8x8_mode_flag = CAVLCReader.readBool(inb, "PPS: transform_8x8_mode_flag");
                bool pic_scaling_matrix_present_flag = CAVLCReader.readBool(inb, "PPS: pic_scaling_matrix_present_flag");
                if (pic_scaling_matrix_present_flag)
                {
                    for (int i = 0; i < 6 + 2 * (pps.extended.transform_8x8_mode_flag ? 1 : 0); i++)
                    {
                        bool pic_scaling_list_present_flag = CAVLCReader.readBool(inb, "PPS: pic_scaling_list_present_flag");
                        if (pic_scaling_list_present_flag)
                        {
                            pps.extended.scalindMatrix.ScalingList4x4 = new ScalingList[8];
                            pps.extended.scalindMatrix.ScalingList8x8 = new ScalingList[8];
                            if (i < 6)
                            {
                                pps.extended.scalindMatrix.ScalingList4x4[i] = ScalingList.read(inb, 16);
                            }
                            else
                            {
                                pps.extended.scalindMatrix.ScalingList8x8[i - 6] = ScalingList.read(inb, 64);
                            }
                        }
                    }
                }
                pps.extended.second_chroma_qp_index_offset = CAVLCReader.readSE(inb, "PPS: second_chroma_qp_index_offset");
            }

            return pps;
        }

        public void write(MemoryStream outb)
        {
            BitWriter writer = new BitWriter(outb);

            CAVLCWriter.writeUE(writer, pic_parameter_set_id, "PPS: pic_parameter_set_id");
            CAVLCWriter.writeUE(writer, seq_parameter_set_id, "PPS: seq_parameter_set_id");
            CAVLCWriter.writeBool(writer, entropy_coding_mode_flag, "PPS: entropy_coding_mode_flag");
            CAVLCWriter.writeBool(writer, pic_order_present_flag, "PPS: pic_order_present_flag");
            CAVLCWriter.writeUE(writer, num_slice_groups_minus1, "PPS: num_slice_groups_minus1");
            if (num_slice_groups_minus1 > 0)
            {
                CAVLCWriter.writeUE(writer, slice_group_map_type, "PPS: slice_group_map_type");
                int[] top_left = new int[1];
                int[] bottom_right = new int[1];
                int[] run_length_minus1 = new int[1];
                if (slice_group_map_type == 0)
                {
                    for (int iGroup = 0; iGroup <= num_slice_groups_minus1; iGroup++)
                    {
                        CAVLCWriter.writeUE(writer, run_length_minus1[iGroup], "PPS: ");
                    }
                }
                else if (slice_group_map_type == 2)
                {
                    for (int iGroup = 0; iGroup < num_slice_groups_minus1; iGroup++)
                    {
                        CAVLCWriter.writeUE(writer, top_left[iGroup], "PPS: ");
                        CAVLCWriter.writeUE(writer, bottom_right[iGroup], "PPS: ");
                    }
                }
                else if (slice_group_map_type == 3 || slice_group_map_type == 4 || slice_group_map_type == 5)
                {
                    CAVLCWriter.writeBool(writer, slice_group_change_direction_flag, "PPS: slice_group_change_direction_flag");
                    CAVLCWriter.writeUE(writer, slice_group_change_rate_minus1, "PPS: slice_group_change_rate_minus1");
                }
                else if (slice_group_map_type == 6)
                {
                    int NumberBitsPerSliceGroupId;
                    if (num_slice_groups_minus1 + 1 > 4)
                        NumberBitsPerSliceGroupId = 3;
                    else if (num_slice_groups_minus1 + 1 > 2)
                        NumberBitsPerSliceGroupId = 2;
                    else
                        NumberBitsPerSliceGroupId = 1;
                    CAVLCWriter.writeUE(writer, slice_group_id.Length, "PPS: ");
                    for (int i = 0; i <= slice_group_id.Length; i++)
                    {
                        CAVLCWriter.writeU(writer, slice_group_id[i], NumberBitsPerSliceGroupId);
                    }
                }
            }
            CAVLCWriter.writeUE(writer, num_ref_idx_active_minus1[0], "PPS: num_ref_idx_l0_active_minus1");
            CAVLCWriter.writeUE(writer, num_ref_idx_active_minus1[1], "PPS: num_ref_idx_l1_active_minus1");
            CAVLCWriter.writeBool(writer, weighted_pred_flag, "PPS: weighted_pred_flag");
            CAVLCWriter.writeNBit(writer, weighted_bipred_idc, 2, "PPS: weighted_bipred_idc");
            CAVLCWriter.writeSE(writer, pic_init_qp_minus26, "PPS: pic_init_qp_minus26");
            CAVLCWriter.writeSE(writer, pic_init_qs_minus26, "PPS: pic_init_qs_minus26");
            CAVLCWriter.writeSE(writer, chroma_qp_index_offset, "PPS: chroma_qp_index_offset");
            CAVLCWriter.writeBool(writer, deblocking_filter_control_present_flag, "PPS: deblocking_filter_control_present_flag");
            CAVLCWriter.writeBool(writer, constrained_intra_pred_flag, "PPS: constrained_intra_pred_flag");
            CAVLCWriter.writeBool(writer, redundant_pic_cnt_present_flag, "PPS: redundant_pic_cnt_present_flag");
            if (extended != null)
            {
                CAVLCWriter.writeBool(writer, extended.transform_8x8_mode_flag, "PPS: transform_8x8_mode_flag");
                CAVLCWriter.writeBool(writer, extended.scalindMatrix != null, "PPS: scalindMatrix");
                if (extended.scalindMatrix != null)
                {
                    for (int i = 0; i < 6 + 2 * (extended.transform_8x8_mode_flag ? 1 : 0); i++)
                    {
                        if (i < 6)
                        {

                            CAVLCWriter.writeBool(writer, extended.scalindMatrix.ScalingList4x4[i] != null, "PPS: ");
                            if (extended.scalindMatrix.ScalingList4x4[i] != null)
                            {
                                extended.scalindMatrix.ScalingList4x4[i].write(writer);
                            }

                        }
                        else
                        {

                            CAVLCWriter.writeBool(writer, extended.scalindMatrix.ScalingList8x8[i - 6] != null, "PPS: ");
                            if (extended.scalindMatrix.ScalingList8x8[i - 6] != null)
                            {
                                extended.scalindMatrix.ScalingList8x8[i - 6].write(writer);
                            }
                        }
                    }
                }
                CAVLCWriter.writeSE(writer, extended.second_chroma_qp_index_offset, "PPS: ");
            }

            CAVLCWriter.writeTrailingBits(writer);
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + bottom_right.GetHashCode();
            result = prime * result + chroma_qp_index_offset;
            result = prime * result + (constrained_intra_pred_flag ? 1231 : 1237);
            result = prime * result + (deblocking_filter_control_present_flag ? 1231 : 1237);
            result = prime * result + (entropy_coding_mode_flag ? 1231 : 1237);
            result = prime * result + ((extended == null) ? 0 : extended.GetHashCode());
            result = prime * result + num_ref_idx_active_minus1[0];
            result = prime * result + num_ref_idx_active_minus1[1];
            result = prime * result + num_slice_groups_minus1;
            result = prime * result + pic_init_qp_minus26;
            result = prime * result + pic_init_qs_minus26;
            result = prime * result + (pic_order_present_flag ? 1231 : 1237);
            result = prime * result + pic_parameter_set_id;
            result = prime * result + (redundant_pic_cnt_present_flag ? 1231 : 1237);
            result = prime * result + run_length_minus1.GetHashCode();
            result = prime * result + seq_parameter_set_id;
            result = prime * result + (slice_group_change_direction_flag ? 1231 : 1237);
            result = prime * result + slice_group_change_rate_minus1;
            result = prime * result + slice_group_id.GetHashCode();
            result = prime * result + slice_group_map_type;
            result = prime * result + top_left.GetHashCode();
            result = prime * result + weighted_bipred_idc;
            result = prime * result + (weighted_pred_flag ? 1231 : 1237);
            return result;
        }

        //@Override
        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            //if (getClass() != obj.getClass())
            //return false;
            PictureParameterSet other = (PictureParameterSet)obj;
            if (!(bottom_right == other.bottom_right))
                return false;
            if (chroma_qp_index_offset != other.chroma_qp_index_offset)
                return false;
            if (constrained_intra_pred_flag != other.constrained_intra_pred_flag)
                return false;
            if (deblocking_filter_control_present_flag != other.deblocking_filter_control_present_flag)
                return false;
            if (entropy_coding_mode_flag != other.entropy_coding_mode_flag)
                return false;
            if (extended == null)
            {
                if (other.extended != null)
                    return false;
            }
            else if (!extended.Equals(other.extended))
                return false;
            if (num_ref_idx_active_minus1[0] != other.num_ref_idx_active_minus1[0])
                return false;
            if (num_ref_idx_active_minus1[1] != other.num_ref_idx_active_minus1[1])
                return false;
            if (num_slice_groups_minus1 != other.num_slice_groups_minus1)
                return false;
            if (pic_init_qp_minus26 != other.pic_init_qp_minus26)
                return false;
            if (pic_init_qs_minus26 != other.pic_init_qs_minus26)
                return false;
            if (pic_order_present_flag != other.pic_order_present_flag)
                return false;
            if (pic_parameter_set_id != other.pic_parameter_set_id)
                return false;
            if (redundant_pic_cnt_present_flag != other.redundant_pic_cnt_present_flag)
                return false;
            if (!(run_length_minus1 == other.run_length_minus1))
                return false;
            if (seq_parameter_set_id != other.seq_parameter_set_id)
                return false;
            if (slice_group_change_direction_flag != other.slice_group_change_direction_flag)
                return false;
            if (slice_group_change_rate_minus1 != other.slice_group_change_rate_minus1)
                return false;
            if (!(slice_group_id == other.slice_group_id))
                return false;
            if (slice_group_map_type != other.slice_group_map_type)
                return false;
            if (!(top_left == other.top_left))
                return false;
            if (weighted_bipred_idc != other.weighted_bipred_idc)
                return false;
            if (weighted_pred_flag != other.weighted_pred_flag)
                return false;
            return true;
        }

        public PictureParameterSet copy()
        {
            MemoryStream buf = new MemoryStream(2048);
            write(buf);
            buf.flip();
            return read(buf);
        }
    }
}
