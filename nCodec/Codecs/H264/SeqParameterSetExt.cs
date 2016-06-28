using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class SeqParameterSetExt
    {

        public int seq_parameter_set_id;
        public int aux_format_idc;
        public int bit_depth_aux_minus8;
        public bool alpha_incr_flag;
        public bool additional_extension_flag;
        public int alpha_opaque_value;
        public int alpha_transparent_value;

        public static SeqParameterSetExt read(MemoryStream iss) {
        BitReader inb = new BitReader(iss);

        SeqParameterSetExt spse = new SeqParameterSetExt();
        spse.seq_parameter_set_id = CAVLCReader.readUE(inb, "SPSE: seq_parameter_set_id");
        spse.aux_format_idc = CAVLCReader.readUE(inb, "SPSE: aux_format_idc");
        if (spse.aux_format_idc != 0) {
            spse.bit_depth_aux_minus8 = CAVLCReader.readUE(inb, "SPSE: bit_depth_aux_minus8");
            spse.alpha_incr_flag = CAVLCReader.readBool(inb, "SPSE: alpha_incr_flag");
            spse.alpha_opaque_value = CAVLCReader.readU(inb, spse.bit_depth_aux_minus8 + 9, "SPSE: alpha_opaque_value");
            spse.alpha_transparent_value = CAVLCReader.readU(inb, spse.bit_depth_aux_minus8 + 9, "SPSE: alpha_transparent_value");
        }
        spse.additional_extension_flag = CAVLCReader.readBool(inb, "SPSE: additional_extension_flag");

        return spse;
    }

        public void write(MemoryStream outb)
        {
            BitWriter writer = new BitWriter(outb);
            CAVLCWriter.writeTrailingBits(writer);
        }
    }
}
