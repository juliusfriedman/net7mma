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
            //Should be exceptions.
            if (source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.RedChannelId) == null) return;
            if (source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.BlueChannelId) == null) return;
            if (source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.GreenChannelId) == null) return;

            //Only can convert to YUV right now, not AYUV or YUVA or any other type.
            if (dest.ImageFormat.Components.Length > 3) return;

            //Check for the luma channel
            Media.Codec.MediaComponent component = source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.LumaChannelId);

            //Ensure it at the first component
            if (component == null || source.MediaFormat.IndexOf(component) != 0) return;

            //Check for the chroma major component
            component = source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.ChromaMajorChannelId);

            //Ensure it at the second component
            if (component == null || source.MediaFormat.IndexOf(component) != 1) return;

            //Check for the chroma minor component
            component = source.MediaFormat.GetComponentById(Media.Codecs.Image.ImageFormat.ChromaMinorChannelId);

            //Ensure it at the last component
            if (component == null || source.MediaFormat.IndexOf(component) != 2) return;

            //Ensure the destination image has been configured for sub sampling
            if (false == dest.ImageFormat.IsSubSampled) return;

            //Only can convert to Planar YUV right now (420)
            if (dest.ImageFormat.DataLayout != Codec.DataLayout.Planar) return;

            //Should be a constant in ImageFormat, 1 looks weird but >> 1 divides by 2, >> 2 by 4
            //Could also change the WidthsAndHights to be an enum or Rational... SubSampling...
            if (dest.ImageFormat.Heights[1] != 1) return;

            if (dest.ImageFormat.Widths[1] != 1) return;
        }

        public override void Transform()
        {
            switch (m_Source.ImageFormat.DataLayout)
            {
                default: return;//Exception?
                case Codec.DataLayout.Packed:
                    {
                        TransformPacked(m_Source, m_Dest, Quality);

                        return;
                    }
                case Codec.DataLayout.Planar:
                    {
                        TransformPlanar(m_Source, m_Dest, Quality);

                        return;
                    }
                case Codec.DataLayout.SemiPlanar:
                    {
                        TransformSemiPlanar(m_Source, m_Dest, Quality);

                        return;
                    }
            }
        }

        public static void TransformPacked(Image src, Image dst, Codec.TransformationQuality quality)
        {

            //Source is packed RGB in any possible format
            //For example a 1x4 image assuming 8 bpp

            //RGB
            //                   RGBR
            //                   GBRG
            //                   BRGB            

            //ARGB
            //                   ARGB
            //                   ARGB
            //                   ARGB
            //                   ARGB

            //BGRA
            //                   BGRA
            //                   BGRA
            //                   BGRA
            //                   BGRA

            //Some other format... with or without equal bit sizes for the alpha or other components.
            //Not the alpha component may have less bits than the others
            //                   BRAG
            //                   BRAG
            //                   BRAG
            //                   BRAG

            //Scope references to the data
            Common.MemorySegment source = src.Data, dest = dst.Data;

            //Create cache of 3 8 bit RGB values * 4 pixels
            byte[] cache = new byte[12];

            //Setup variables
            int offChr = 0, offLuma = 0, offSrc = 0, strideSrc = src.Width * src.ImageFormat.Length, strideDst = dst.Width, dstComponentOffset = dst.MediaFormat.Length;

            //Keeps the offset in bits of the current byte
            int bitOffset = 0;

            //1 pixel images...
            for (int i = 0, ie = src.Height >> 1; i < ie; ++i)
            {
                for (int j = 0, je = src.Width >> 1; j < je; ++j)
                {
                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src.ImageFormat, source, ref offSrc, ref bitOffset, cache, 0);

                    //Write Luma
                    dest[offLuma] = cache[0];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src.ImageFormat, source, ref offSrc, ref bitOffset, cache, 3);

                    //Write Luma and move offset
                    dest[offLuma++ + strideDst] = cache[3];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src.ImageFormat, source, ref offSrc, ref bitOffset, cache, 6);

                    //Write Luma
                    dest[offLuma] = cache[6];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src.ImageFormat, source, ref offSrc, ref bitOffset, cache, 9);

                    //Write Luma and move offset
                    dest[offLuma++ + strideDst] = cache[9];

                    //Write averaged Chroma Major
                    dest[offChr] = AverageCb(cache, quality);

                    //Write averaged Chroma Minor
                    dest[offChr + dstComponentOffset] = AverageCr(cache, quality);

                    //Move the Chroma offset
                    ++offChr;
                }

                //Move the Luma offset
                offLuma += strideDst;
            }

            cache = null;

            source = dest = null;
        }

        public static void TransformPlanar(Image src, Image dst, Codec.TransformationQuality quality)
        {
            //Source is planar RGB
            //1x4 image
            //                   RRRR
            //                   GGGG
            //                   BBBB
        }

        public static void TransformSemiPlanar(Image src, Image dst, Codec.TransformationQuality quality)
        {
            //Source is semi planar RGB????
            //1x4 image
            //                   RRRR
            //                   GBGB
            //                   GBGB
        }

        public static void ReadAndConvertRGBComponentsToYUV(ImageFormat format, Common.MemorySegment source, ref int offSrc, ref int bitOffset, byte[] cache, int offset)
        {
            byte r = 0, b = 0, g = 0;

            //Loop Components
            for (int c = 0, ce = format.Components.Length; c < ce; ++c)
            {
                //Get the component
                Codec.MediaComponent mc = format.Components[c];

                //Determine what to do with the component
                switch (mc.Id)
                {
                    //Skip the Alpha or others
                    default:
                    case Media.Codecs.Image.ImageFormat.AlphaChannelId:
                        {
                            Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false);
                            continue;
                        }
                    case Media.Codecs.Image.ImageFormat.RedChannelId:
                        {
                            r = (byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false);
                            continue;
                        }

                    case Media.Codecs.Image.ImageFormat.GreenChannelId:
                        {
                            g = (byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false);
                            continue;
                        }
                    case Media.Codecs.Image.ImageFormat.BlueChannelId:
                        {
                            b = (byte)Common.Binary.ReadBinaryInteger(source.Array, ref offSrc, ref bitOffset, mc.Size, false);
                            continue;
                        }
                }
            }

            //Convert the components from RGB to YUV and store the results in the cache at offset given
            rgb2yuv(r, g, b, cache, offset);
        }

        /// <summary>
        /// Creates a Averaged Chroma Major value based on the RGB values in the cache and given quality
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static byte AverageCb(byte[] cache, Media.Codec.TransformationQuality quality)
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

        /// <summary>
        /// Creates a Averaged Chroma Minor value based on the RGB values in the cache and given quality
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static byte AverageCr(byte[] cache, Media.Codec.TransformationQuality quality)
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
