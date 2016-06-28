using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class SequenceHeader
    {

        public const int Sequence_Extension = 0x1;
        public const int Sequence_Display_Extension = 0x2;
        public const int Sequence_Scalable_Extension = 0x5;
        private static bool m_hasExtensions;

        public int horizontal_size;
        public int vertical_size;
        public int aspect_ratio_information;
        public int frame_rate_code;
        public int bit_rate;
        public int marker_bit;
        public int vbv_buffer_size_value;
        public int constrained_parameters_flag;
        public int[] intra_quantiser_matrix;
        public int[] non_intra_quantiser_matrix;

        public SequenceExtension sequenceExtension;
        public SequenceScalableExtension sequenceScalableExtension;
        public SequenceDisplayExtension sequenceDisplayExtension;

        public static SequenceHeader read(MemoryStream bb)
        {
            BitReader inb = new BitReader(bb);
            SequenceHeader sh = new SequenceHeader();
            sh.horizontal_size = inb.readNBit(12);
            sh.vertical_size = inb.readNBit(12);
            sh.aspect_ratio_information = inb.readNBit(4);
            sh.frame_rate_code = inb.readNBit(4);
            sh.bit_rate = inb.readNBit(18);
            sh.marker_bit = inb.read1Bit();
            sh.vbv_buffer_size_value = inb.readNBit(10);
            sh.constrained_parameters_flag = inb.read1Bit();
            if (inb.read1Bit() != 0)
            {
                sh.intra_quantiser_matrix = new int[64];
                for (int i = 0; i < 64; i++)
                {
                    sh.intra_quantiser_matrix[i] = inb.readNBit(8);
                }
            }
            if (inb.read1Bit() != 0)
            {
                sh.non_intra_quantiser_matrix = new int[64];
                for (int i = 0; i < 64; i++)
                {
                    sh.non_intra_quantiser_matrix[i] = inb.readNBit(8);
                }
            }

            return sh;
        }

        public static void readExtension(MemoryStream bb, SequenceHeader sh) {
        m_hasExtensions = true;

        BitReader inb = new BitReader(bb);
        int extType = inb.readNBit(4);
        switch (extType) {
        case Sequence_Extension:
            sh.sequenceExtension = SequenceExtension.read(inb);
            break;
        case Sequence_Scalable_Extension:
            sh.sequenceScalableExtension = SequenceScalableExtension.read(inb);
            break;
        case Sequence_Display_Extension:
            sh.sequenceDisplayExtension = SequenceDisplayExtension.read(inb);
            break;
        default:
            throw new Exception("Unsupported extension: " + extType);
        }
    }

        public void write(MemoryStream os)
        {
            BitWriter outb = new BitWriter(os);
            outb.writeNBit(horizontal_size, 12);
            outb.writeNBit(vertical_size, 12);
            outb.writeNBit(aspect_ratio_information, 4);
            outb.writeNBit(frame_rate_code, 4);
            outb.writeNBit(bit_rate, 18);
            outb.write1Bit(marker_bit);
            outb.writeNBit(vbv_buffer_size_value, 10);
            outb.write1Bit(constrained_parameters_flag);
            outb.write1Bit(intra_quantiser_matrix != null ? 1 : 0);
            if (intra_quantiser_matrix != null)
            {
                for (int i = 0; i < 64; i++)
                {
                    outb.writeNBit(intra_quantiser_matrix[i], 8);
                }
            }
            outb.write1Bit(non_intra_quantiser_matrix != null ? 1 : 0);
            if (non_intra_quantiser_matrix != null)
            {
                for (int i = 0; i < 64; i++)
                {
                    outb.writeNBit(non_intra_quantiser_matrix[i], 8);
                }
            }

            writeExtensions(os);
        }

        private void writeExtensions(MemoryStream outb)
        {
            if (sequenceExtension != null)
            {
                outb.putInt(MPEGConst.EXTENSION_START_CODE);
                BitWriter os = new BitWriter(outb);
                os.writeNBit(Sequence_Extension, 4);
                sequenceExtension.write(os);
            }

            if (sequenceScalableExtension != null)
            {
                outb.putInt(MPEGConst.EXTENSION_START_CODE);
                BitWriter os = new BitWriter(outb);
                os.writeNBit(Sequence_Scalable_Extension, 4);
                sequenceScalableExtension.write(os);
            }

            if (sequenceDisplayExtension != null)
            {
                outb.putInt(MPEGConst.EXTENSION_START_CODE);
                BitWriter os = new BitWriter(outb);
                os.writeNBit(Sequence_Display_Extension, 4);
                sequenceDisplayExtension.write(os);
            }
        }

        public bool hasExtensions()
        {
            return m_hasExtensions;
        }

        public void copyExtensions(SequenceHeader sh)
        {
            sequenceExtension = sh.sequenceExtension;
            sequenceScalableExtension = sh.sequenceScalableExtension;
            sequenceDisplayExtension = sh.sequenceDisplayExtension;
        }
    }
}
