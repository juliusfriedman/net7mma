using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image.Transformations
{
    public sealed class YUV : ImageTransformation
    {

        static ImageTransform TransformToRGB = new ImageTransform(TransformYUV420ToRGB);

        //Needs a way to verify the source is actuall YUV420 and dest is actually RGB
        public static void TransformYUV420ToRGB(Image source, Image dest)
        {
            if (dest.ColorSpace != source.ColorSpace)
                using (var t = new YUV(source, dest))
                    t.Transform();
        }

        public YUV(Image source, Image dest)
            : base(source, dest, Codec.TransformationQuality.None)
        {
           
        }

        public override void Transform()
        {
            int offLuma = 0, offChroma = 0;

            int stride = m_Dest.Width;

            bool oddWidth = Common.Binary.IsOdd(m_Dest.Width);

            //Take pointers to each plane
            using (Common.MemorySegment y = new Common.MemorySegment(m_Source.Data.Array, m_Source.Data.Offset, m_Source.PlaneLength(0)))
            {
                using (Common.MemorySegment u = new Common.MemorySegment(m_Source.Data.Array, y.Offset + y.Count, m_Source.PlaneLength(1)))
                {
                    using (Common.MemorySegment v = new Common.MemorySegment(m_Source.Data.Array, u.Offset + u.Count, m_Source.PlaneLength(2)))
                    {
                        //Loop for the height
                        for (int i = 0, ie = (m_Dest.Height >> 1); i < ie; i++)
                        {
                            //Loop for the width
                            for (int k = 0, ke = (m_Dest.Width >> 1); k < ke; k++)
                            {
                                //Calulcate offset of next pixel (k * 2)
                                int j = k << 1;

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j) * 3);

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j + 1], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j + 1) * 3);

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j + stride], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j + stride) * 3);

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j + stride + 1], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j + stride + 1) * 3);

                                //Move Chroma offset
                                ++offChroma;
                            }

                            //Handle Odd Width
                            if (oddWidth)
                            {
                                //Calulcate offset of next pixel
                                int j = m_Dest.Width - 1;

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j) * 3);

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j + stride], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j + stride) * 3);

                                //Move Chroma offset
                                ++offChroma;
                            }

                            //Move luma offset
                            offLuma += 2 * stride;
                        }

                        //Handle Odd Widths and Heights
                        if (Common.Binary.IsOdd(m_Dest.Height))
                        {
                            for (int k = 0, ke = (m_Dest.Width >> 1); k < ke; k++)
                            {
                                //Calulcate offset of next pixel
                                int j = k << 1;

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j) * 3);

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j + 1], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j + 1) * 3);

                                //Move Chroma offset
                                ++offChroma;
                            }

                            if (oddWidth)
                            {
                                //Calulcate offset of next pixel
                                int j = m_Dest.Width - 1;

                                //Convert YUV to RGB
                                YUVJtoRGB(y[offLuma + j], u[offChroma], v[offChroma], m_Dest.Data.Array, m_Dest.Data.Offset + (offLuma + j) * 3);

                                //Move Chroma offset
                                ++offChroma;
                            }
                        }
                    }
                }
            }
        }

        const int SCALEBITS = 10;
        const int ONE_HALF = (1 << (SCALEBITS - 1));

        private static int FIX(double x)
        {
            return ((int)((x) * (1 << SCALEBITS) + 0.5));
        }

        static readonly int FIX_0_71414 = FIX(0.71414);
        static readonly int FIX_1_772 = FIX(1.77200);
        static readonly int _FIX_0_34414 = -FIX(0.34414);
        static readonly int FIX_1_402 = FIX(1.40200);

        public static void YUVJtoRGB(byte y, byte cb, byte cr, byte[] data, int off)
        {
            int y_ = (y + sbyte.MaxValue) << SCALEBITS;
            int add_r = FIX_1_402 * cr + ONE_HALF;
            int add_g = _FIX_0_34414 * cb - FIX_0_71414 * cr + ONE_HALF;
            int add_b = FIX_1_772 * cb + ONE_HALF;

            int r = (y_ + add_r) >> SCALEBITS;
            int g = (y_ + add_g) >> SCALEBITS;
            int b = (y_ + add_b) >> SCALEBITS;

            data[off] = (byte)Common.Binary.Clamp(b - sbyte.MaxValue, sbyte.MinValue, 127);
            data[off + 1] = (byte)Common.Binary.Clamp(g - sbyte.MaxValue, sbyte.MinValue, 127);
            data[off + 2] = (byte)Common.Binary.Clamp(r - sbyte.MaxValue, sbyte.MinValue, 127);
        }
    }
}
