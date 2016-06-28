using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;
namespace nVideo.Codecs.H264
{
    public class NALUnitWriter {
    private Stream to;
    private static byte[] _MARKER = new byte[]{0,0,0, 1};

    public NALUnitWriter(Stream to) {
        this.to = to;
    }

    public void writeUnit(NALUnit nal, MemoryStream data) {
        MemoryStream empreva = new MemoryStream(data.remaining() + 1024);
        empreva.Write(_MARKER, 0, 4);
        nal.write(empreva);
        emprev(empreva, data);
        empreva.flip();
        empreva.WriteTo(to);
    }

    private void emprev(MemoryStream emprev, MemoryStream data)
    {
        MemoryStream dd = data.duplicate();
        int prev1 = 1, prev2 = 1;
        while (dd.hasRemaining()) {
            byte b = dd.get();
            if (prev1 == 0 && prev2 == 0 && ((b & 0x3) == b)) {
                prev2 = prev1;
                prev1 = 3;
                emprev.put((byte) 3);
            }

            prev2 = prev1;
            prev1 = b;
            emprev.put((byte) b);
        }
    }
}
}
