using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.RAW
{
    public class V210Encoder
    {
        public MemoryStream encodeFrame(MemoryStream _out, Picture frame)
        {
            MemoryStream sout = _out.duplicate();
            //sout.order(ByteOrder.LITTLE_ENDIAN);
            int tgtStride = ((frame.getPlaneWidth(0) + 47) / 48) * 48;
            int[][] data = frame.getData();

            int[] tmpY = new int[tgtStride];
            int[] tmpCb = new int[tgtStride >> 1];
            int[] tmpCr = new int[tgtStride >> 1];

            int yOff = 0, cbOff = 0, crOff = 0;
            for (int yy = 0; yy < frame.getHeight(); yy++)
            {
                System.Array.Copy(data[0], yOff, tmpY, 0, frame.getPlaneWidth(0));
                System.Array.Copy(data[1], cbOff, tmpCb, 0, frame.getPlaneWidth(1));
                System.Array.Copy(data[2], crOff, tmpCr, 0, frame.getPlaneWidth(2));

                for (int yi = 0, cbi = 0, cri = 0; yi < tgtStride; )
                {
                    int i = 0;
                    i |= clip(tmpCr[cri++]) << 20;
                    i |= clip(tmpY[yi++]) << 10;
                    i |= clip(tmpCb[cbi++]);
                    sout.putInt(i);

                    i = 0;
                    i |= clip(tmpY[yi++]);
                    i |= clip(tmpY[yi++]) << 20;
                    i |= clip(tmpCb[cbi++]) << 10;
                    sout.putInt(i);

                    i = 0;
                    i |= clip(tmpCb[cbi++]) << 20;
                    i |= clip(tmpY[yi++]) << 10;
                    i |= clip(tmpCr[cri++]);
                    sout.putInt(i);

                    i = 0;
                    i |= clip(tmpY[yi++]);
                    i |= clip(tmpY[yi++]) << 20;
                    i |= clip(tmpCr[cri++]) << 10;
                    sout.putInt(i);
                }
                yOff += frame.getPlaneWidth(0);
                cbOff += frame.getPlaneWidth(1);
                crOff += frame.getPlaneWidth(2);
            }
            sout.flip();

            return sout;
        }

        static int clip(int val)
        {
            return H264.CABAC.clip(val, 8, 1019);
        }
    }
}
