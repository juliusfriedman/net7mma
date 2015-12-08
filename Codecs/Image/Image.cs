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
                totalSize += (width >> colorSpace.m_Widths[i]) * (height >> colorSpace.m_Heights[i]);
            }

            //Return the amount of bytes
            return totalSize;
        }

        #endregion

        #region Fields

        protected readonly ColorSpace m_Format;
        protected readonly int m_Width;
        protected readonly int m_Height;
        protected readonly int m_BitsPerComponent;

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

        public int Width { get { return m_Width; } }
        
        public int Height { get { return m_Height; } }

        public int Size { get { return Data.Count; } }

        public int ComponentSize { get { return m_BitsPerComponent; } }

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
    }
}