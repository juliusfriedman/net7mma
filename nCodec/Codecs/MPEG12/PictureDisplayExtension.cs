using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class PictureDisplayExtension {
    public Point[] frame_centre_offsets;

    public static PictureDisplayExtension read(BitReader bits, SequenceExtension se, PictureCodingExtension pce) {
        PictureDisplayExtension pde = new PictureDisplayExtension();
        pde.frame_centre_offsets = new Point[numberOfFrameCentreOffsets(se, pce)];
        for (int i = 0; i < pde.frame_centre_offsets.Length; i++) {
            int frame_centre_horizontal_offset = bits.readNBit(16);
            bits.read1Bit();
            int frame_centre_vertical_offset = bits.readNBit(16);
            bits.read1Bit();
            pde.frame_centre_offsets[i] = new Point(frame_centre_horizontal_offset, frame_centre_vertical_offset);
        }
        return pde;
    }

    private static int numberOfFrameCentreOffsets(SequenceExtension se, PictureCodingExtension pce) {
        if (se == null || pce == null)
            throw new ArgumentException("PictureDisplayExtension requires SequenceExtension"
                    + " and PictureCodingExtension to be present");
        if (se.progressive_sequence == 1) {
            if (pce.repeat_first_field == 1) {
                if (pce.top_field_first == 1)
                    return 3;
                else
                    return 2;
            } else {
                return 1;
            }
        } else {
            if (pce.picture_structure != PictureCodingExtension.Frame) {
                return 1;
            } else {
                if (pce.repeat_first_field == 1)
                    return 3;
                else
                    return 2;
            }
        }
    }

    public void write(BitWriter outb) {
        foreach (var point in frame_centre_offsets) {
            outb.writeNBit(point.getX(), 16);
            outb.writeNBit(point.getY(), 16);
        }
    }
}
}
