using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class CopyrightExtension
    {
        public int copyright_flag;
        public int copyright_identifier;
        public int original_or_copy;
        public int copyright_number_1;
        public int copyright_number_2;
        public int copyright_number_3;

        public static CopyrightExtension read(BitReader inb) {
        CopyrightExtension ce = new CopyrightExtension();
        ce.copyright_flag = inb.read1Bit();
        ce.copyright_identifier = inb.readNBit(8);
        ce.original_or_copy = inb.read1Bit();
        inb.skip(7);
        inb.read1Bit();
        ce.copyright_number_1 = inb.readNBit(20);
        inb.read1Bit();
        ce.copyright_number_2 = inb.readNBit(22);
        inb.read1Bit();
        ce.copyright_number_3 = inb.readNBit(22);
        return ce;
    }

        public void write(BitWriter outb)
        {
            outb.write1Bit(copyright_flag);
            outb.writeNBit(copyright_identifier, 8);
            outb.write1Bit(original_or_copy);
            outb.writeNBit(0, 7);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(copyright_number_1, 20);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(copyright_number_2, 22);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(copyright_number_3, 22);
        }
    }
}
