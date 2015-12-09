using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image.Transformations
{
    public sealed class RGB : ImageTransformation
    {

        static ImageTransform TransformToYUV420 = new ImageTransform(TransformRGBToYUV420);

        //Needs a way to verify the source is actuall RGB and dest is actually YUV420
        public static void TransformRGBToYUV420(Image source, Image dest)
        {
            if(dest.ColorSpace != source.ColorSpace)
                using (var t = new RGB(source, dest))
                    t.Transform();
        }

        public RGB(Image source, Image dest)
            :base(source, dest, Codec.TransformationQuality.None)
        {

        }

        public override void Transform()
        {
            
            //Scope references to the data
            Common.MemorySegment source = m_Source.Data, dest = m_Dest.Data;

            //Create cache of 3 pixels
            byte[] cache = new byte[12];

            //Setup variables
            int offChr = 0, offLuma = 0, offSrc = 0, strideSrc = m_Source.Width * 3, strideDst = m_Dest.Width, dstComponentOffset = Common.Binary.BitsToBytes(m_Dest.BitsPerComponent);

            //1 pixel images...
            for (int i = 0, ie = m_Source.Height >> 1; i < ie; ++i)
            {
                for (int j = 0, je = m_Source.Width >> 1; j < je; ++j)
                {
                    //dest[offChr] = 0;
                    //dest[dstComponentOffset + offChr] = 0;

                    //Convert to YUV
                    rgb2yuv(source[offSrc], source[offSrc + 1], source[offSrc + 2], cache, 0);

                    //Write Luma
                    dest[offLuma] = cache[0];

                    //Convert to YUV
                    rgb2yuv(source[offSrc + strideSrc], source[offSrc + strideSrc + 1], source[offSrc + strideSrc + 2], cache, 3);

                    //Write Luma
                    dest[offLuma + strideDst] = cache[3];

                    //Move Luma offset
                    ++offLuma;

                    //Convert to YUV
                    rgb2yuv(source[offSrc + 3], source[offSrc + 4], source[offSrc + 5], cache, 6);

                    //Write Luma
                    dest[offLuma] = cache[6];

                    //Convert to YUV
                    rgb2yuv(source[offSrc + strideSrc + 3], source[offSrc + strideSrc + 4], source[offSrc + strideSrc + 5], cache, 9);

                    //Write Luma
                    dest[offLuma + strideDst] = cache[9];

                    //Move Luma offset
                    ++offLuma;

                    //Calulate average chroma
                    byte chroma = (byte)(cache[1] + cache[4] + cache[8] + cache[10]);

                    //If there is not a 0 value then half the value
                    if (chroma != 0) chroma >>= 2;

                    //Write averaged Chroma
                    dest[offChr] = chroma; //(byte)((cache[1] + cache[4] + cache[8] + cache[10] + 2) >> 2);

                    //Calulate average chroma
                    chroma = (byte)(cache[2] + cache[5] + cache[7] + cache[11]);

                    //If there is not a 0 value then half the value
                    if (chroma != 0) chroma >>= 2;

                    //Write averaged Chroma
                    dest[offChr + dstComponentOffset] = chroma;// (byte)((cache[2] + cache[5] + cache[9] + cache[11] + 2) >> 2);

                    //Move the Chroma offset
                    ++offChr;
                    
                    //Move the source offset
                    offSrc += 6;
                }

                //Move the Luma offset
                offLuma += strideDst;

                //Move the source offset
                offSrc += strideSrc;
            }

            cache = null;

            source = dest = null;
        }

        public static void rgb2yuv(byte r, byte g, byte b, byte[] res, int offset)
        {
            int rS = r + sbyte.MaxValue;
            int gS = g + sbyte.MaxValue;
            int bS = b + sbyte.MaxValue;
            int y = 66 * rS + 129 * gS + 25 * bS;
            int u = -38 * rS - 74 * gS + 112 * bS;
            int v = 112 * rS - 94 * gS - 18 * bS;
            y = (y + sbyte.MaxValue) >> 8;
            u = (u + sbyte.MaxValue) >> 8;
            v = (v + sbyte.MaxValue) >> 8;

            //res[offset] = (byte)Common.Binary.Clamp(y - 112, sbyte.MinValue, 127);

            res[offset] = (byte)Common.Binary.Clamp(y - 112, 0, 255);

            res[offset + 1] = (byte)Common.Binary.Clamp(u, sbyte.MinValue, 127);
            res[offset + 2] = (byte)Common.Binary.Clamp(v, sbyte.MinValue, 127);
        }

    }
}
