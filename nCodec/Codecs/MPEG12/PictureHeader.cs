using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class PictureHeader {

    public const int Quant_Matrix_Extension = 0x3;
    public const int Copyright_Extension = 0x4;
    public const int Picture_Display_Extension = 0x7;
    public const int Picture_Coding_Extension = 0x8;
    public const int Picture_Spatial_Scalable_Extension = 0x9;
    public const int Picture_Temporal_Scalable_Extension = 0x10;

    public static int IntraCoded = 0x1;
    public static int PredictiveCoded = 0x2;
    public static int BiPredictiveCoded = 0x3;

    public int temporal_reference;
    public int picture_coding_type;
    public int vbv_delay;
    public int full_pel_forward_vector;
    public int forward_f_code;
    public int full_pel_backward_vector;
    public int backward_f_code;

    public QuantMatrixExtension quantMatrixExtension;
    public CopyrightExtension copyrightExtension;
    public PictureDisplayExtension pictureDisplayExtension;
    public PictureCodingExtension pictureCodingExtension;
    public PictureSpatialScalableExtension pictureSpatialScalableExtension;
    public PictureTemporalScalableExtension pictureTemporalScalableExtension;
    private bool m_hasExtensions;

    public static PictureHeader read(MemoryStream bb) {
        BitReader inb = new BitReader(bb);
        PictureHeader ph = new PictureHeader();
        ph.temporal_reference = inb.readNBit(10);
        ph.picture_coding_type = inb.readNBit(3);
        ph.vbv_delay = inb.readNBit(16);
        if (ph.picture_coding_type == 2 || ph.picture_coding_type == 3) {
            ph.full_pel_forward_vector = inb.read1Bit();
            ph.forward_f_code = inb.readNBit(3);
        }
        if (ph.picture_coding_type == 3) {
            ph.full_pel_backward_vector = inb.read1Bit();
            ph.backward_f_code = inb.readNBit(3);
        }
        while (inb.read1Bit() == 1) {
            inb.readNBit(8);
        }

        return ph;
    }

    public static void readExtension(MemoryStream bb, PictureHeader ph, SequenceHeader sh) {
        ph.m_hasExtensions = true;
        BitReader inb = new BitReader(bb);
        int extType = inb.readNBit(4);
        switch (extType) {
        case Quant_Matrix_Extension:
            ph.quantMatrixExtension = QuantMatrixExtension.read(inb);
            break;
        case Copyright_Extension:
            ph.copyrightExtension = CopyrightExtension.read(inb);
            break;
        case Picture_Display_Extension:
            ph.pictureDisplayExtension = PictureDisplayExtension.read(inb, sh.sequenceExtension,
                    ph.pictureCodingExtension);
            break;
        case Picture_Coding_Extension:
            ph.pictureCodingExtension = PictureCodingExtension.read(inb);
            break;
        case Picture_Spatial_Scalable_Extension:
            ph.pictureSpatialScalableExtension = PictureSpatialScalableExtension.read(inb);
            break;
        case Picture_Temporal_Scalable_Extension:
            ph.pictureTemporalScalableExtension = PictureTemporalScalableExtension.read(inb);
            break;
        default:
            throw new Exception("Unsupported extension: " + extType);
        }
    }

    public void write(MemoryStream os) {
        BitWriter outb = new BitWriter(os);
        outb.writeNBit(temporal_reference, 10);
        outb.writeNBit(picture_coding_type, 3);
        outb.writeNBit(vbv_delay, 16);
        if (picture_coding_type == 2 || picture_coding_type == 3) {
            outb.write1Bit(full_pel_forward_vector);
            outb.write1Bit(forward_f_code);
        }
        if (picture_coding_type == 3) {
            outb.write1Bit(full_pel_backward_vector);
            outb.writeNBit(backward_f_code, 3);
        }
        outb.write1Bit(0);

        writeExtensions(os);
    }

    private void writeExtensions(MemoryStream outb) {
        if (quantMatrixExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Quant_Matrix_Extension, 4);
            quantMatrixExtension.write(os);
        }

        if (copyrightExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Copyright_Extension, 4);
            copyrightExtension.write(os);
        }

        if (pictureCodingExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Picture_Coding_Extension, 4);
            pictureCodingExtension.write(os);
        }

        if (pictureDisplayExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Picture_Display_Extension, 4);
            pictureDisplayExtension.write(os);
        }

        if (pictureSpatialScalableExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Picture_Spatial_Scalable_Extension, 4);
            pictureSpatialScalableExtension.write(os);
        }

        if (pictureTemporalScalableExtension != null) {
            outb.putInt(MPEGConst.EXTENSION_START_CODE);
            BitWriter os = new BitWriter(outb);
            os.writeNBit(Picture_Temporal_Scalable_Extension, 4);
            pictureTemporalScalableExtension.write(os);
        }
    }

    public bool hasExtensions() {
        return m_hasExtensions;
    }
}
}
