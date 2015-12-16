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

            //Could probably handle packed or semi planar YUV by making a small offset change
            if (dst.ImageFormat.DataLayout == Codec.DataLayout.Packed || dst.ImageFormat.DataLayout == Codec.DataLayout.SemiPlanar)
            {
                strideDst = dst.MediaFormat.Length;

                dstComponentOffset = 1;
            }

            //Should also have a way to write the components of dst

            //Keeps the offset in bits of the current byte
            int bitOffset = 0;

            //1 pixel images...
            //Should actualy use width or height and check after each operation

            //Should definitely use PlaneHeight

            //Loop half height
            for (int i = 0, ie = src.Height >> 1; i < ie; ++i)
            {                

                //Loop for half width
                for (int j = 0, je = src.Width >> 1; j < je; ++j)
                {
                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src, ref offSrc, ref bitOffset, cache, 0);

                    //if (++j >= src.Width) break;

                    //Write Luma
                    dest[offLuma] = cache[0];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src, ref offSrc, ref bitOffset, cache, 3);

                    //Write Luma and move offset
                    dest[offLuma++ + strideDst] = cache[3];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src, ref offSrc, ref bitOffset, cache, 6);

                    //Write Luma
                    dest[offLuma] = cache[6];

                    //Read and convert component
                    ReadAndConvertRGBComponentsToYUV(src, ref offSrc, ref bitOffset, cache, 9);                    

                    //Write Luma and move offset
                    dest[offLuma++ + strideDst] = cache[9];

                    //If dest is not sub sampled then just write the components...

                    //Otherwise this will need to ensure its taking the correct amount of components for the dest SubSampling.

                    //Also needs to write in the correct plane, this assumes YUV

                    switch (dst.ImageFormat.Widths[1])
                    {
                        case 0: //No sub sampling in plane 1
                            {
                                //Needs to be written to the correct offset, this assumes YUV
                                dest[offChr++] = cache[1];

                                //This could mean 0 samples in this plane?

                                break;
                            }
                        case 1: //Half width in plane 1
                            {
                                //Write averaged Chroma Major
                                dest[offChr] = AverageCb(cache, quality);

                                //Write averaged Chroma Minor
                                dest[offChr + dstComponentOffset] = AverageCr(cache, quality);

                                //Move the Chroma offset
                                ++offChr;

                                break;
                            }
                        case 2: //Quarter width in plane 1
                            {
                                byte average = (byte)(AverageCb(cache, quality) + AverageCr(cache, quality));

                                if (average > 0) average >>= 1;

                                //Write averaged Chroma value
                                dest[offChr++] = average;

                                //Move the Chroma offset
                                ++offChr;

                                break;
                            }
                    }

                   
                }

                //should only be done when dst.ImageFormat is Planar!!!

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

        public static void ReadAndConvertRGBComponentsToYUV(Image src, ref int offSrc, ref int bitOffset, byte[] cache, int offset)
        {
            byte r = 0, b = 0, g = 0;

            int offsetStart = offSrc, usedBits = 0;

            switch (src.ImageFormat.DataLayout)
            {
                case Codec.DataLayout.Planar:
                    {
                        //Loop Components
                        for (int c = 0, ce = src.ImageFormat.Components.Length; c < ce; ++c)
                        {
                            //Get the component
                            Codec.MediaComponent mc = src.ImageFormat.Components[c];

                            //Determine what to do with the component
                            switch (mc.Id)
                            {
                                default:
                                    {
                                        //Move the offset in all cases
                                        offSrc += src.PlaneLength(c);

                                        continue;
                                    }
                                case Media.Codecs.Image.ImageFormat.RedChannelId:
                                    {
                                        //Read the component and store the result
                                        r = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, offSrc, bitOffset, mc.Size, false);

                                        //Track used bits
                                        usedBits += mc.Size;

                                        goto default;
                                    }

                                case Media.Codecs.Image.ImageFormat.GreenChannelId:
                                    {
                                        //Read the component and store the result
                                        g = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, offSrc, bitOffset, mc.Size, false);

                                        //Track used bits
                                        usedBits += mc.Size;

                                        goto default;
                                    }
                                case Media.Codecs.Image.ImageFormat.BlueChannelId:
                                    {
                                        //Read the component and store the result
                                        b = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, offSrc, bitOffset, mc.Size, false);

                                        //Track used bits
                                        usedBits += mc.Size;

                                        goto default;
                                    }
                            }
                        }

                        //If there was no byte boundary passed
                        if (bitOffset + usedBits < Common.Binary.BitsPerByte)
                        {
                            //Move the bit offset by the amount of bits used.
                            bitOffset += usedBits;

                            //Set the offset given by reference back to where it was initially
                            offSrc = offsetStart;
                        }
                        else
                        {
                            int leftOverbits = 0;

                            //Move the offset the required amount of bytes
                            offsetStart += Math.DivRem(Common.Binary.BitsPerByte, usedBits, out leftOverbits);

                            //Move the bit offset for any odd bits
                            bitOffset += leftOverbits;

                            //Set the offset given by reference
                            offSrc = offsetStart;
                        }

                        break;
                    }
                case Codec.DataLayout.SemiPlanar:
                    {
                        //Semi Planar is handled planar for the first component and packed for the last two.

                        //This should be determined by the Plane index of the Component...

                        break;
                    }
                case Codec.DataLayout.Packed:
                    {

                        //Loop Components
                        for (int c = 0, ce = src.ImageFormat.Components.Length; c < ce; ++c)
                        {
                            //Get the component
                            Codec.MediaComponent mc = src.ImageFormat.Components[c];

                            //Determine what to do with the component
                            switch (mc.Id)
                            {
                                //Skip the Alpha or others
                                default:
                                case Media.Codecs.Image.ImageFormat.AlphaChannelId:
                                    {
                                        Common.Binary.ReadBinaryInteger(src.Data.Array, ref offSrc, ref bitOffset, mc.Size, false);
                                        continue;
                                    }
                                case Media.Codecs.Image.ImageFormat.RedChannelId:
                                    {
                                        r = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, ref offSrc, ref bitOffset, mc.Size, false);
                                        continue;
                                    }

                                case Media.Codecs.Image.ImageFormat.GreenChannelId:
                                    {
                                        g = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, ref offSrc, ref bitOffset, mc.Size, false);
                                        continue;
                                    }
                                case Media.Codecs.Image.ImageFormat.BlueChannelId:
                                    {
                                        b = (byte)Common.Binary.ReadBinaryInteger(src.Data.Array, ref offSrc, ref bitOffset, mc.Size, false);
                                        continue;
                                    }
                            }
                        }

                        break;
                    }
            }

            //Convert the components from RGB to YUV and store the results in the cache at offset given, should also take dest format.
            rgb2yuv(r, g, b, cache, offset);
        }

        /// <summary>
        /// Creates a Averaged Chroma Major value based on the RGB values in the cache and given quality
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        //Should ALSO TAKE destination format or sub sampling...
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

        //should also take dest format to allow conversion to desired type.

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
