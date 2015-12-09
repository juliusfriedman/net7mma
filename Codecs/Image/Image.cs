using System.Collections.Generic;
using System.Linq;

namespace Media.Codecs.Image
{
    public class Image : Media.Codec.MediaBuffer
    {
        #region Statics

        //static Image Crop(Image source)

        internal static int CalculateSize(ColorSpace colorSpace, int width, int height)
        {
            //The total size in bytes
            int totalSize = 0;

            //Iterate each component in the ColorSpace
            for (int i = 0; i < colorSpace.ComponentCount; ++i)
            {
                //Increment the total size in bytes by calculating the size in bytes of that plane using the ColorSpace information
                totalSize += (width >> colorSpace.Widths[i]) * (height >> colorSpace.Heights[i]);
            }

            //Return the amount of bytes
            return totalSize;
        }

        #endregion

        #region Fields

        protected readonly ColorSpace m_Format;
        protected readonly int m_Width;
        protected readonly int m_Height;
        
        #endregion

        #region Constructor

        public Image(ColorSpace format, int width, int height,  Media.Codec.DataLayout dataLayout = Media.Codec.DataLayout.Packed, int bitsPerComponent = 8)
            : base(Media.Codec.MediaType.Image, dataLayout, CalculateSize(format, width, height), Common.Binary.SystemByteOrder, bitsPerComponent, format.ComponentCount)
        {
            m_Format = format;
            m_Width = width;
            m_Height = height;
        }

        #endregion

        #region Properties

        public ColorSpace ColorSpace { get { return m_Format; } }

        public int Width { get { return m_Width; } }
        
        public int Height { get { return m_Height; } }        

        #endregion

        #region Methods

        public int PlaneWidth(int plane)
        {
            if (plane >= m_Format.ComponentCount) return -1;

            return Width >> m_Format.Widths[plane];
        }

        public int PlaneHeight(int plane)
        {
            if (plane >= m_Format.ComponentCount) return -1;

            return Height >> m_Format.Heights[plane];
        }

        public int PlaneSize(int plane)
        {
            if (plane >= m_Format.ComponentCount) return -1;

            return PlaneWidth(plane) + PlaneHeight(plane) * BitsPerComponent;
        }

        public int PlaneLength(int plane)
        {
            if (plane >= m_Format.ComponentCount) return -1;

            return Common.Binary.BitsToBytes(PlaneSize(plane));
        }

        #endregion
    }

    //Drawing?

    //Will eventually need Font support...
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the Image class is correct
    /// </summary>
    internal class ImageUnitTests
    {
        public static void TestConstructor()
        {
            using (Media.Codecs.Image.Image image = new Codecs.Image.Image(Media.Codecs.Image.ColorSpace.RGB, 1, 1, Codec.DataLayout.Packed, 8))
            {
                if (image.SampleCount != 1) throw new System.InvalidOperationException();

                if (image.Data.Count != image.Width * image.Height * image.ComponentCount) throw new System.InvalidOperationException();
            }
        }

        public static void TestConversion()
        {
            //Create the source image
            using (Media.Codecs.Image.Image rgbImage = new Codecs.Image.Image(Media.Codecs.Image.ColorSpace.RGB, 8, 8, Codec.DataLayout.Packed, 8))
            {
                //Create the destination format, copy YUV and specify half size width and height planes
                using (Media.Codecs.Image.ColorSpace Yuv420P = new Codecs.Image.ColorSpace(Media.Codecs.Image.ColorSpace.YUV, new int[] { 0, 1, 1 }))
                {
                    //Create the destination image
                    using (Media.Codecs.Image.Image yuvImage = new Codecs.Image.Image(Yuv420P, 8, 8, Codec.DataLayout.Packed, 8))
                    {

                        //Cache the data of the source before transformation
                        byte[] left = rgbImage.Data.ToArray(), right;

                        //Transform RGB to YUV

                        using (Media.Codecs.Image.ImageTransformation it = new Media.Codecs.Image.Transformations.RGB(rgbImage, yuvImage))
                        {
                            it.Transform();

                            //Yuv Data
                            //left = dest.Data.ToArray();
                        }

                        //Transform YUV to RGB

                        using (Media.Codecs.Image.ImageTransformation it = new Media.Codecs.Image.Transformations.YUV(yuvImage, rgbImage))
                        {
                            it.Transform();

                            //Rgb Data
                            right = rgbImage.Data.ToArray();
                        }

                        //Compare the two sequences
                        if (false == left.SequenceEqual(right)) throw new System.InvalidOperationException();
                    }
                }
            }
        }
    }
}