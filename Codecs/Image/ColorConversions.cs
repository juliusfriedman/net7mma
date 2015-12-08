#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Codecs.Image
{
    public static class ColorConversions
    {
        #region RgbYuv.cs

        /// <summary>
        /// Provides cached RGB to YUV lookup without alpha support.
        /// </summary>
        /// <remarks>
        /// This class is public so a user can manually load and unload the lookup table.
        /// Looking up a color calculates the lookup table if not present.
        /// All methods except UnloadLookupTable should be thread-safe, although there will be a performance overhead if GetYUV is called while Initialize has not finished.
        /// Table size is roughtly 65 mb...
        /// </remarks>
        public static class RgbYuv
        {
            const uint RgbMask = 0x00ffffff;

            private static volatile int[] lookupTable;
            
            private static int[] LookupTable
            {
                get
                {
                    if (lookupTable == null) Initialize();
                    return lookupTable;
                }
            }

            /// <summary>
            /// Gets whether the lookup table is ready.
            /// </summary>
            public static bool Initialized
            {
                get
                {
                    return lookupTable != null;
                }
            }

            /// <summary>
            /// Returns the 24bit YUV equivalent of the provided 24bit RGB color.
            /// <para>Any alpha component is dropped.</para>
            /// </summary>
            /// <param name="rgb">A 24bit rgb color.</param>
            /// <returns>The corresponding 24bit YUV color.</returns>
            [System.CLSCompliant(false)]
            public static int GetYuv(uint rgb)
            {
                return LookupTable[rgb & RgbMask];
            }

            public static int GetYuv(int rgb)
            {
                return GetYuv((uint)rgb);
            }

            const double R0 = .299, G0 = .587, B = .114, 
                         R1 = -.169, G1 = .331, H = .5,
                         R2 = .419, G2 = .081;

            /// <summary>
            /// Calculates the lookup table.
            /// </summary>
            public static unsafe void Initialize()
            {
                var lTable = new int[0x1000000]; // 256 * 256 * 256
                fixed (int* lookupP = lTable)
                {
                    byte* lP = (byte*)lookupP;
                    for (uint i = 0; i < lTable.Length; i++)
                    {
                        float r = (i & 0xff0000) >> 16;
                        float g = (i & 0x00ff00) >> 8;
                        float b = (i & 0x0000ff);

                        lP++; //Skip alpha byte
                        *(lP++) = (byte)(R0 * r + G0 * g + B * b);
                        *(lP++) = (byte)((int)(R1 * r - G1 * g + H * b) + sbyte.MaxValue);
                        *(lP++) = (byte)((int)(H * r - R2 * g - G2 * b) + sbyte.MaxValue);
                    }
                }
                lookupTable = lTable;
            }

            /// <summary>
            /// Releases the reference to the lookup table.
            /// <para>The table has to be calculated again for the next lookup.</para>
            /// </summary>
            public static void UnloadLookupTable()
            {
                lookupTable = null;
            }
        }

        #endregion

        #region Color Conversion Routines

        //Todo standardize

        public static unsafe void Bgr32ToBgr24(byte[] source, int srcOffset, byte[] destination, int destOffset, int pixelCount)
        {
            fixed (byte* sourcePtr = source, destinationPtr = destination)
            {
                var sourceStart = sourcePtr + srcOffset;
                var destinationStart = destinationPtr + destOffset;
                var sourceEnd = sourceStart + 4 * pixelCount;
                var src = sourceStart;
                var dest = destinationStart;
                while (src < sourceEnd)
                {
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    src++;
                }
            }
        }

        //Rotate(byte[] source, int srcOffset, byte[] destination, int destOffset, int angle, int width, int height)
        //public void Rotate(byte[] source, int srcOffset, byte[] destination, int destOffset, int a, int b)

        public static void FlipVertical(byte[] source, int srcOffset, byte[] destination, int destOffset, int height, int stride)
        {
            var src = srcOffset;
            var dest = destOffset + (height - 1) * stride;//should ensure non 0
            for (var y = 0; y < height; y++)
            {
                System.Buffer.BlockCopy(source, src, destination, dest, stride);
                src += stride;
                dest -= stride;
            }
        }

        //public static void FlipHorizontal(byte[] source, int srcOffset, byte[] destination, int destOffset, ..)
        

        //Should follow the same api as below...

        const int E = 8, S = 16, H = 100, T = 298, t = 208, N = 409, f = 516;

        internal static unsafe void YUV2RGBManaged(byte[] YUVData, byte[] RGBData, int width, int height)
        {

            //returned pixel format is 2yuv - i.e. luminance, y, is represented for every pixel and the u and v are alternated
            //like this (where Cb = u , Cr = y)
            //Y0 Cb Y1 Cr Y2 Cb Y3 

            /*http://msdn.microsoft.com/en-us/library/ms893078.aspx
             * 
             * C = Y - 16
             D = U - 128
             E = V - 128
             R = clip(( 298 * C           + 409 * E + 128) >> 8)
             G = clip(( 298 * C - 100 * D - 208 * E + 128) >> 8)
             B = clip(( 298 * C + 516 * D           + 128) >> 8)

             * here are a whole bunch more formats for doing this...
             * http://stackoverflow.com/questions/3943779/converting-to-yuv-ycbcr-colour-space-many-versions
             */


            unchecked
            {
                fixed (byte* pRGBs = RGBData, pYUVs = YUVData)
                {
                    for (int r = 0; r < height; r++)
                    {
                        byte* pRGB = pRGBs + r * width * 3;
                        byte* pYUV = pYUVs + r * width * 2;

                        //process two pixels at a time
                        for (int c = 0; c < width; c += 2)
                        {
                            //Array access
                            int C1 = pYUV[1] - S;
                            int C2 = pYUV[3] - S;
                            int D = pYUV[2] - sbyte.MaxValue;
                            int E = pYUV[0] - sbyte.MaxValue;

                            //Prepare
                            int NEs = N * E + sbyte.MaxValue;

                            int HDtE = H * D - t * E;

                            int fD = f * D;

                            //Combine coefficients
                            int TC = T * C1;

                            //Transform
                            pRGB[0] = Common.Binary.Clamp((byte)((TC + NEs) >> E), (byte)0, byte.MaxValue);
                            pRGB[1] = Common.Binary.Clamp((byte)((TC - HDtE + sbyte.MaxValue) >> E), (byte)0, byte.MaxValue);
                            pRGB[2] = Common.Binary.Clamp((byte)((TC + fD + sbyte.MaxValue) >> E), (byte)0, byte.MaxValue);

                            TC = T * C2;

                            pRGB[3] = Common.Binary.Clamp((byte)((TC + NEs) >> E), (byte)0, byte.MaxValue);
                            pRGB[4] = Common.Binary.Clamp((byte)((TC - HDtE + sbyte.MaxValue) >> E), (byte)0, byte.MaxValue);
                            pRGB[5] = Common.Binary.Clamp((byte)((TC + fD + sbyte.MaxValue) >> E), (byte)0, byte.MaxValue);

                            pRGB += 6;
                            pYUV += 4;
                        }
                    }
                }
            }
        }

        const int OT = 112, M = 129, X = 66, Q = 25, Z = 18, B = -38, C = 74, D = 94;

        public static unsafe byte[] ABGRA2YUV420Managed(int width, int height, System.IntPtr scan0)
        {
            //Parrallel
            try
            {
                int frameSize = width * height;
                int chromasize = frameSize / 4;

                int yIndex = 0;
                int uIndex = frameSize;
                int vIndex = frameSize + chromasize;
                byte[] yuv = new byte[frameSize * 3 / 2];

                uint* rgbValues = (uint*)scan0.ToPointer();

                int index = 0;

                unchecked
                {
                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            uint B = (rgbValues[index] & 0xff000000) >> 24;
                            uint G = (rgbValues[index] & 0xff0000) >> 16;
                            uint R = (rgbValues[index] & 0xff00) >> 8;
                            uint a = (rgbValues[index] & 0xff) >> 0;

                            //int yuvC = Utility.RgbYuv.GetYuv(Common.Binary.ReverseU32(rgbValues[index]));

                            uint Y = (uint)(((X * R + M * G + Q * B + sbyte.MaxValue) >> E) + S);
                            uint U = (uint)(((B * R - C * G + OT * B + sbyte.MaxValue) >> E) + sbyte.MaxValue);
                            uint V = (uint)(((OT * R - D * G - Z * B + sbyte.MaxValue) >> E) + sbyte.MaxValue);

                            yuv[yIndex++] = Common.Binary.Clamp((byte)Y, (byte)0, byte.MaxValue);// (byte)((yuvC & 0xff0000) >> 16); //

                            // > 0 && IsEven
                            //If Common.Binary.IsPowerOfTwo(j) && Common.Binary.IsPowerOfTwo(index)
                            if (j % 2 == 0 && index % 2 == 0)
                            {
                                yuv[uIndex++] = Common.Binary.Clamp((byte)U, (byte)0, byte.MaxValue);//(byte)((yuvC  & 0xff00) >> 8);//
                                yuv[vIndex++] = Common.Binary.Clamp((byte)V, (byte)0, byte.MaxValue);// (byte)((yuvC & 0xff) >> 0);//
                            }

                            index++;
                        }
                    }

                    return yuv;
                }

            }
            catch
            {
                throw;
            }
        }

        #endregion        

    }
}
