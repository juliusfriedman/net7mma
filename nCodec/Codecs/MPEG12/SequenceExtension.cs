using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class SequenceExtension
    {

        public const int Chroma420 = 0x1;
        public const int Chroma422 = 0x2;
        public const int Chroma444 = 0x3;

        public int profile_and_level;
        public int progressive_sequence;
        public int chroma_format;
        public int horizontal_size_extension;
        public int vertical_size_extension;
        public int bit_rate_extension;
        public int vbv_buffer_size_extension;
        public int low_delay;
        public int frame_rate_extension_n;
        public int frame_rate_extension_d;

        public static SequenceExtension read(BitReader inb)
        {
            SequenceExtension se = new SequenceExtension();
            se.profile_and_level = inb.readNBit(8);
            se.progressive_sequence = inb.read1Bit();
            se.chroma_format = inb.readNBit(2);
            se.horizontal_size_extension = inb.readNBit(2);
            se.vertical_size_extension = inb.readNBit(2);
            se.bit_rate_extension = inb.readNBit(12);
            se.vbv_buffer_size_extension = inb.readNBit(8);
            se.low_delay = inb.read1Bit();
            se.frame_rate_extension_n = inb.readNBit(2);
            se.frame_rate_extension_d = inb.readNBit(5);

            return se;
        }

        public void write(BitWriter outb)
        {
            outb.writeNBit(profile_and_level, 8);
            outb.write1Bit(progressive_sequence);
            outb.writeNBit(chroma_format, 2);
            outb.writeNBit(horizontal_size_extension, 2);
            outb.writeNBit(vertical_size_extension, 2);
            outb.writeNBit(bit_rate_extension, 12);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(vbv_buffer_size_extension, 8);
            outb.write1Bit(low_delay);
            outb.writeNBit(frame_rate_extension_n, 2);
            outb.writeNBit(frame_rate_extension_d, 5);
        }
    }
}
