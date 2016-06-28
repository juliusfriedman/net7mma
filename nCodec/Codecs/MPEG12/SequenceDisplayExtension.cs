using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class SequenceDisplayExtension {
    public int video_format;
    public int display_horizontal_size;
    public int display_vertical_size;
    public ColorDescription colorDescription;

    public class ColorDescription {
        int colour_primaries;
        int transfer_characteristics;
        int matrix_coefficients;

        public static ColorDescription read(BitReader inb) {
            ColorDescription cd = new ColorDescription();
            cd.colour_primaries = inb.readNBit(8);
            cd.transfer_characteristics = inb.readNBit(8);
            cd.matrix_coefficients = inb.readNBit(8);
            return cd;
        }

        public void write(BitWriter outv) {
            outv.writeNBit(colour_primaries, 8);
            outv.writeNBit(transfer_characteristics, 8);
            outv.writeNBit(matrix_coefficients, 8);
        }
    }

    public static SequenceDisplayExtension read(BitReader inv) {
        SequenceDisplayExtension sde = new SequenceDisplayExtension();
        sde.video_format = inv.readNBit(3);
        if (inv.read1Bit() == 1) {
            sde.colorDescription = ColorDescription.read(inv);
        }
        sde.display_horizontal_size = inv.readNBit(14);
        inv.read1Bit();
        sde.display_vertical_size = inv.readNBit(14);

        return sde;
    }

    public void write(BitWriter outv) {
        outv.writeNBit(video_format, 3);
        outv.write1Bit(colorDescription != null ? 1 : 0);
        if (colorDescription != null)
            colorDescription.write(outv);
        outv.writeNBit(display_horizontal_size, 14);
        outv.write1Bit(1); // verify this
        outv.writeNBit(display_vertical_size, 14);
    }
}
}
