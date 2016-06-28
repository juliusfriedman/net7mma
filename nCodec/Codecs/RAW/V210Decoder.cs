using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.RAW
{
    public class V210Decoder
    {

        private int width;
        private int height;

        public V210Decoder(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public Picture decode(byte[] data)
        {
            //IntBuffer dat = MemoryStream.wrap(data).order(LITTLE_ENDIAN).asIntBuffer();
            List<int> dat = new List<int>(), y = new List<int>(width * height),
            cb = new List<int>(width * height / 2),
            cr = new List<int>(width * height / 2);

            while (dat.Count > 0)
            {
                int i = dat[0];
                cr.Add(i >> 20);
                y.Add((i >> 10) & 0x3ff);
                cb.Add(i & 0x3ff);

                i = dat[0];
                y.Add(i & 0x3ff);
                y.Add(i >> 20);
                cb.Add((i >> 10) & 0x3ff);

                i = dat[0];
                cb.Add(i >> 20);
                y.Add((i >> 10) & 0x3ff);
                cr.Add(i & 0x3ff);

                i = dat[0];
                y.Add(i & 0x3ff);
                y.Add(i >> 20);
                cr.Add((i >> 10) & 0x3ff);
            }

            //return new Picture(width, height, y.Concat(cb).Concat(cr).ToArray(), ColorSpace.YUV422_10);
            return null;
        }
    }
}
