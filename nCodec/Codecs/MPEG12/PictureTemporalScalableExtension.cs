using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class PictureTemporalScalableExtension
    {
        public int reference_select_code;
        public int forward_temporal_reference;
        public int backward_temporal_reference;

        public static PictureTemporalScalableExtension read(BitReader inb)
        {
            PictureTemporalScalableExtension ptse = new PictureTemporalScalableExtension();
            ptse.reference_select_code = inb.readNBit(2);
            ptse.forward_temporal_reference = inb.readNBit(10);
            inb.read1Bit();
            ptse.backward_temporal_reference = inb.readNBit(10);

            return ptse;
        }

        public void write(BitWriter outb)
        {
            outb.writeNBit(reference_select_code, 2);
            outb.writeNBit(forward_temporal_reference, 10);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(backward_temporal_reference, 10);
        }
    }
}
