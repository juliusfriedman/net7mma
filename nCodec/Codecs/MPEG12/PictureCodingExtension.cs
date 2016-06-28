using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class PictureCodingExtension
    {

        public static int Top_Field = 1;
        public static int Bottom_Field = 2;
        public static int Frame = 3;

        public int[][] f_code;
        public int intra_dc_precision;
        public int picture_structure;
        public int top_field_first;
        public int frame_pred_frame_dct;
        public int concealment_motion_vectors;
        public int q_scale_type;
        public int intra_vlc_format;
        public int alternate_scan;
        public int repeat_first_field;
        public int chroma_420_type;
        public int progressive_frame;
        public CompositeDisplay compositeDisplay;

        public class CompositeDisplay
        {
            public int v_axis;
            public int field_sequence;
            public int sub_carrier;
            public int burst_amplitude;
            public int sub_carrier_phase;

            public static CompositeDisplay read(BitReader inb)
            {
                CompositeDisplay cd = new CompositeDisplay();
                cd.v_axis = inb.read1Bit();
                cd.field_sequence = inb.readNBit(3);
                cd.sub_carrier = inb.read1Bit();
                cd.burst_amplitude = inb.readNBit(7);
                cd.sub_carrier_phase = inb.readNBit(8);
                return cd;
            }

            public void write(BitWriter outb)
            {
                outb.write1Bit(v_axis);
                outb.writeNBit(field_sequence, 3);
                outb.write1Bit(sub_carrier);
                outb.writeNBit(burst_amplitude, 7);
                outb.writeNBit(sub_carrier_phase, 8);
            }
        }

        public static PictureCodingExtension read(BitReader inb) {
        PictureCodingExtension pce = new PictureCodingExtension();
        pce.f_code = (int[][])Array.CreateInstance(typeof(int), new int[]{2,2,});
        pce.f_code[0][0] = inb.readNBit(4);
        pce.f_code[0][1] = inb.readNBit(4);
        pce.f_code[1][0] = inb.readNBit(4);
        pce.f_code[1][1] = inb.readNBit(4);
        pce.intra_dc_precision = inb.readNBit(2);
        pce.picture_structure = inb.readNBit(2);
        pce.top_field_first = inb.read1Bit();
        pce.frame_pred_frame_dct = inb.read1Bit();
        pce.concealment_motion_vectors = inb.read1Bit();
        pce.q_scale_type = inb.read1Bit();
        pce.intra_vlc_format = inb.read1Bit();
        pce.alternate_scan = inb.read1Bit();
        pce.repeat_first_field = inb.read1Bit();
        pce.chroma_420_type = inb.read1Bit();
        pce.progressive_frame = inb.read1Bit();
        if (inb.read1Bit() != 0) {
            pce.compositeDisplay = CompositeDisplay.read(inb);
        }

        return pce;
    }

        public void write(BitWriter outb)
        {
            outb.writeNBit(f_code[0][0], 4);
            outb.writeNBit(f_code[0][1], 4);
            outb.writeNBit(f_code[1][0], 4);
            outb.writeNBit(f_code[1][1], 4);
            outb.writeNBit(intra_dc_precision, 2);
            outb.writeNBit(picture_structure, 2);
            outb.write1Bit(top_field_first);
            outb.write1Bit(frame_pred_frame_dct);
            outb.write1Bit(concealment_motion_vectors);
            outb.write1Bit(q_scale_type);
            outb.write1Bit(intra_vlc_format);
            outb.write1Bit(alternate_scan);
            outb.write1Bit(repeat_first_field);
            outb.write1Bit(chroma_420_type);
            outb.write1Bit(progressive_frame);
            outb.write1Bit(compositeDisplay != null ? 1 : 0);
            if (compositeDisplay != null)
                compositeDisplay.write(outb);
        }
    }
}
