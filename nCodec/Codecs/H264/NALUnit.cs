using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.H264
{
   public class NALUnit {

    public NALUnitType type;
    public int nal_ref_idc;

    public NALUnit(NALUnitType type, int nal_ref_idc) {
        this.type = type;
        this.nal_ref_idc = nal_ref_idc;
    }

    public static NALUnit read(MemoryStream inb) {
        int nalu = inb.get() & 0xff;
        int nal_ref_idc = (nalu >> 5) & 0x3;
        int nb = nalu & 0x1f;

        NALUnitType type = NALUnitType.fromValue(nb);
        return new NALUnit(type, nal_ref_idc);
    }

    public void write(MemoryStream outb) {
        int nalu = type.getValue() | (nal_ref_idc << 5);
        outb.put((byte) nalu);
    }
}
}
