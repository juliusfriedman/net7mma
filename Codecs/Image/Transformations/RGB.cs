﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image.Transformations
{
    public sealed class RGB : ImageTransformation
    {

        static ImageTransform TransformToYUV420 = new ImageTransform(TransformRGBToYUV420);

        //Needs a way to verify the source is actually RGB and dest is actually YUV420
        public static void TransformRGBToYUV420(Image source, Image dest)
        {
            if (dest.MediaFormat != source.MediaFormat)
                using (var t = new RGB(source, dest))
                    t.Transform();
        }

        public RGB(Image source, Image dest)
            :base(source, dest, Codec.TransformationQuality.Highest)
        {
            if (Encoding.Default.GetString(source.MediaFormat.Ids) != "rgb") return;

            if (Encoding.Default.GetString(dest.MediaFormat.Ids) != "yuv") return;

            if (false == dest.ImageFormat.IsSubSampled) return;

            //Should be a constant in ImageFormat, 1 looks weird but >> 1 divides by 2, >> 2 by 4
            //Could also change the WidthsAndHights to be an enum or Rational... SubSampling...
            if (dest.ImageFormat.Heights[1] != 1) return;

            if (dest.ImageFormat.Widths[1] != 1) return;
        }

        public override void Transform()
        {
            
            //Scope references to the data
            Common.MemorySegment source = m_Source.Data, dest = m_Dest.Data;

            Codec.MediaComponent alphaComponent = m_Source.ImageFormat.AlphaComponent;

            //Determine if there is an alpha channel on the RGB data
            bool sourceHasAlpha = alphaComponent != null;

            //Depending on if there is or is not an alpha channel then there is an adjustment to the offsets in the source
            int alphaComponentIndex = sourceHasAlpha ? m_Source.ImageFormat.IndexOf(alphaComponent) : -1,
                beforeAlpha = sourceHasAlpha && alphaComponentIndex == 0 ? m_Source.ImageFormat.ComponentIndexToByteSize(alphaComponentIndex) : 0,
                afterAlpha = sourceHasAlpha && beforeAlpha == 0 ? m_Source.ImageFormat.BytesInComponentAfterIndex(alphaComponentIndex) : 0;

            //In cases where the alphaComponent.Size is < 8 or is not at the beginning or end of the component data this does not work.
            //See notes below

            //Create cache of 3 pixels
            byte[] cache = new byte[12];

            //Setup variables
            int offChr = 0, offLuma = 0, offSrc = 0, strideSrc = m_Source.Width * 3, strideDst = m_Dest.Width, dstComponentOffset = m_Dest.MediaFormat.Length;

            //int bitOffset = 0;

            //1 pixel images...
            for (int i = 0, ie = m_Source.Height >> 1; i < ie; ++i)
            {
                for (int j = 0, je = m_Source.Width >> 1; j < je; ++j)
                {
                    //To handle alpha correctly as well as bgr, grb or other weird formats
                    ////Loop each component in the source
                    //for (int c = 0; c < m_Source.MediaFormat.Components.Length; ++c)
                    //{
                    //    //Get the component
                    //    Codec.MediaComponent mc = m_Source.MediaFormat.Components[c];

                    //    //Skip the Alpha
                    //    if(mc.Id == Media.Codecs.Image.ImageFormat.AlphaChannelId) continue;

                    //    //needs to ensure r is the first component
                    //    //needs to ensure g is the second component
                    //    //needs to ensure b is the third component

                    //    //Convert the components from RGB to YUV and store the results in the cache at offset 0
                    //    rgb2yuv((byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false),
                    //        (byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false),
                    //        (byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false), cache, 0);
                    //}

                    //dest[offChr] = 0;
                    //dest[dstComponentOffset + offChr] = 0;

                    //Need to skip alpha component if present
                    offSrc += beforeAlpha;

                    //Convert to YUV
                    rgb2yuv(source[offSrc], source[offSrc + 1], source[offSrc + 2], cache, 0);

                    //Need to skip alpha component if present
                    offSrc += afterAlpha;

                    //Write Luma
                    dest[offLuma] = cache[0];

                    //Need to skip alpha component if present
                    offSrc += beforeAlpha;

                    //Convert to YUV
                    rgb2yuv(source[offSrc + strideSrc], source[offSrc + strideSrc + 1], source[offSrc + strideSrc + 2], cache, 3);

                    //Need to skip alpha component if present
                    offSrc += afterAlpha;

                    //Write Luma
                    dest[offLuma + strideDst] = cache[3];

                    //Move Luma offset
                    ++offLuma;

                    //Need to skip alpha component if present
                    offSrc += beforeAlpha;

                    //Convert to YUV
                    rgb2yuv(source[offSrc + 3], source[offSrc + 4], source[offSrc + 5], cache, 6);

                    //Need to skip alpha component if present
                    offSrc += afterAlpha;

                    //Write Luma
                    dest[offLuma] = cache[6];

                    //Need to skip alpha component if present
                    offSrc += beforeAlpha;

                    //Convert to YUV
                    rgb2yuv(source[offSrc + strideSrc + 3], source[offSrc + strideSrc + 4], source[offSrc + strideSrc + 5], cache, 9);

                    //Write Luma
                    dest[offLuma + strideDst] = cache[9];

                    //Need to skip alpha component if present
                    offSrc += afterAlpha;

                    //Move Luma offset
                    ++offLuma;

                    //Write averaged Chroma
                    dest[offChr] = AverageCr(cache, Quality);

                    //Write averaged Chroma
                    dest[offChr + dstComponentOffset] = AverageCb(cache, Quality);

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

        public static byte AverageCr(byte[] cache, Media.Codec.TransformationQuality quality)
        {
            switch (quality)
            {
                case Codec.TransformationQuality.Unspecified:
                case Codec.TransformationQuality.Low:
                    return cache[1];
                default:
                    {
                        byte chroma = 0;

                        switch (quality)
                        {
                            case Codec.TransformationQuality.Medium:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[1] + cache[4]);

                                    break;
                                }
                            case Codec.TransformationQuality.High:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[1] + cache[4] + cache[8]);

                                    break;
                                }
                            case Codec.TransformationQuality.Highest:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[1] + cache[4] + cache[8] + cache[10]);

                                    break;
                                }
                        }

                        //If there is not a 0 value then half the value
                        if (chroma != 0) chroma >>= 2;

                        return chroma;
                    }
            }
        }

        public static byte AverageCb(byte[] cache, Media.Codec.TransformationQuality quality)
        {
            switch (quality)
            {
                case Codec.TransformationQuality.Unspecified:
                case Codec.TransformationQuality.Low:
                    return cache[2];
                default:
                    {
                        byte chroma = 0;

                        switch (quality)
                        {
                            case Codec.TransformationQuality.Medium:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[2] + cache[5]);

                                    break;
                                }
                            case Codec.TransformationQuality.High:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[2] + cache[5] + cache[7]);

                                    break;
                                }
                            case Codec.TransformationQuality.Highest:
                                {
                                    //Calulate average chroma
                                    chroma = (byte)(cache[2] + cache[5] + cache[7] + cache[11]);

                                    break;
                                }
                        }

                        //If there is not a 0 value then half the value
                        if (chroma != 0) chroma >>= 2;

                        return chroma;
                    }
            }
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
