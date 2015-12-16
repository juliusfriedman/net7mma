using System.Collections.Generic;
using System.Linq;

namespace Media.Codecs.Image
{
    public class Image : Media.Codec.MediaBuffer
    {
        #region Statics

        //static Image Crop(Image source)

        internal static int CalculateSize(ImageFormat format, int width, int height)
        {
            //The total size in bytes
            int totalSize = 0;

            //Iterate each component in the ColorSpace
            for (int i = 0, ie = format.Components.Length; i <ie ; ++i)
            {
                //Increment the total size in bytes by calculating the size in bytes of that plane using the ColorSpace information
                totalSize += (width >> format.Widths[i]) * (height >> format.Heights[i]);
            }

            //Return the amount of bytes
            return totalSize;
        }

        #endregion

        #region Fields

        public readonly int Width;

        public readonly int Height;
        
        #endregion

        #region Constructor

        //Needs a subsampling...
        public Image(ImageFormat format, int width, int height)
            : base(format, CalculateSize(format, width, height))
        {
            Width = width;

            Height = height;
        }

        #endregion

        #region Properties

        public IEnumerable<Common.MemorySegment> this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x > Width || y > Height) yield return Common.MemorySegment.Empty;

                switch (DataLayout)
                {
                    default:
                    case Media.Codec.DataLayout.Unknown:
                        {
                            throw new System.ArgumentException("Invalid DataLayout");
                        }
                    case Media.Codec.DataLayout.Packed:
                        {
                            //Packed has all samples next to each other... could return all data in 1 shot...
                            //yield return new Common.MemorySegment(Data.Array, y * Width + x, ImageFormat.Length);

                            int offset = 0, cache = y * Width + x;

                            //Loop each component
                            for (int c = 0, ce = ImageFormat.Components.Length; c < ce; ++c)
                            {
                                //Sub sampled with no values in the plane
                                if (ImageFormat.Widths[c] < 0 || ImageFormat.Heights[c] < 0) continue;

                                //Probably Needs a BinarySegment, same as MemorySegment but with a BitOffset...

                                //Return the data which belongs to just the component being iterated
                                yield return new Common.MemorySegment(Data.Array, Data.Offset + offset + cache, ImageFormat[c].Length);

                                //Move the offset the size of the component.
                                offset += ImageFormat[c].Length;

                                //If the Length == 1 and Size <= 8 this is not correct logic... can't move offset to the next byte.
                            }

                            yield break;
                        }
                    case Media.Codec.DataLayout.Planar:
                        {
                            int offset = 0;

                            for (int c = 0, ce = ImageFormat.Components.Length; c < ce; ++c)
                            {
                                //Sub sampled with no values in the plane
                                if (ImageFormat.Widths[c] < 0 || ImageFormat.Heights[c] < 0) continue;

                                //Return the data which belongs to only the component being iterated
                                yield return new Common.MemorySegment(Data.Array, Data.Offset + offset + (y >> ImageFormat.Widths[c] * PlaneWidth(c)) + x >> ImageFormat.Heights[c], ImageFormat[c].Length);

                                //Move to the next plane
                                offset += PlaneLength(c);
                            }


                            yield break;
                        }
                    case Media.Codec.DataLayout.SemiPlanar:
                        {
                            //Planar of 1st component

                            //Packed for all remaining components?

                            yield break;
                        }
                }
            }
        }

        public ImageFormat ImageFormat { get { return MediaFormat as ImageFormat; } }

        public int Planes { get { return MediaFormat.Components.Length; } }

        #endregion

        #region Methods

        public int PlaneWidth(int plane)
        {
            if (plane >= MediaFormat.Components.Length) return -1;

            return Width >> ImageFormat.Widths[plane];
        }

        public int PlaneHeight(int plane)
        {
            if (plane >= MediaFormat.Components.Length) return -1;

            return Height >> ImageFormat.Heights[plane];
        }

        public int PlaneSize(int plane)
        {
            if (plane >= MediaFormat.Components.Length) return -1;

            return (PlaneWidth(plane) + PlaneHeight(plane)) * ImageFormat.Size;
        }

        public int PlaneLength(int plane)
        {
            if (plane >= MediaFormat.Components.Length) return -1;

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
            using (Media.Codecs.Image.Image image = new Codecs.Image.Image(Media.Codecs.Image.ImageFormat.RGB(8), 1, 1))
            {
                if (image.SampleCount != 1) throw new System.InvalidOperationException();

                if (image.Data.Count != image.Width * image.Height * image.MediaFormat.Length) throw new System.InvalidOperationException();
            }
        }

        public static void TestConversionRGB()
        {
            //Create the source image
            using (Media.Codecs.Image.Image rgbImage =  new Codecs.Image.Image(Media.Codecs.Image.ImageFormat.RGB(8), 8, 8))
            {
                if (rgbImage.ImageFormat.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be false");

                //Create the ImageFormat based on YUV packed but in Planar format with a full height luma plane and half hight chroma planes
                Media.Codecs.Image.ImageFormat Yuv420P = new Codecs.Image.ImageFormat(Media.Codecs.Image.ImageFormat.YUV(8, Common.Binary.ByteOrder.Little, Codec.DataLayout.Planar), new int[] { 0, 1, 1 });

                if (Yuv420P.IsInterleaved) throw new System.Exception("IsInterleaved should be false");

                if (Yuv420P.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be false");

                //ImageFormat could be given directly to constructor here

                //Create the destination image
                using (Media.Codecs.Image.Image yuvImage = new Codecs.Image.Image(Yuv420P, 8, 8))
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

                //Done with the format.
                Yuv420P = null;
            }
        }

        public static void TestConversionARGB()
        {
            //Create the source image
            using (Media.Codecs.Image.Image rgbImage = new Codecs.Image.Image(Media.Codecs.Image.ImageFormat.ARGB(8), 8, 8))
            {
                //Create the ImageFormat based on YUV packed but in Planar format with a full height luma plane and half hight chroma planes
                Media.Codecs.Image.ImageFormat Yuv420P = new Codecs.Image.ImageFormat(Media.Codecs.Image.ImageFormat.YUV(8, Common.Binary.ByteOrder.Little, Codec.DataLayout.Planar), new int[] { 0, 1, 1 });

                if (Yuv420P.IsInterleaved) throw new System.Exception("IsInterleaved should be false");

                if (Yuv420P.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be false");

                if (false == rgbImage.ImageFormat.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be true");

                //ImageFormat be given directly to constructor here

                //Create the destination image
                using (Media.Codecs.Image.Image yuvImage = new Codecs.Image.Image(Yuv420P, 8, 8))
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

                //Done with the format.
                Yuv420P = null;
            }
        }

        public static void TestConversionRGBA()
        {
            //Create the source image
            using (Media.Codecs.Image.Image rgbImage = new Codecs.Image.Image(Media.Codecs.Image.ImageFormat.RGBA(8), 8, 8))
            {
                //Create the ImageFormat based on YUV packed but in Planar format with a full height luma plane and half hight chroma planes
                Media.Codecs.Image.ImageFormat Yuv420P = new Codecs.Image.ImageFormat(Media.Codecs.Image.ImageFormat.YUV(8, Common.Binary.ByteOrder.Little, Codec.DataLayout.Planar), new int[] { 0, 1, 1 });

                if (Yuv420P.IsInterleaved) throw new System.Exception("IsInterleaved should be false");

                if (Yuv420P.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be false");

                if (false == rgbImage.ImageFormat.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be true");

                //ImageFormat could be given directly to constructor here

                //Create the destination image
                using (Media.Codecs.Image.Image yuvImage = new Codecs.Image.Image(Yuv420P, 8, 8))
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

                //Done with the format.
                Yuv420P = null;
            }
        }

        public static void TestConversionRGAB()
        {

            Codecs.Image.ImageFormat weird = new Codecs.Image.ImageFormat(Common.Binary.ByteOrder.Little, Codec.DataLayout.Packed, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent((byte)'r', 10),
                new Codec.MediaComponent((byte)'g', 10),
                new Codec.MediaComponent((byte)'a', 2),
                new Codec.MediaComponent((byte)'b', 10),
                
            });

            //Create the source image
            using (Media.Codecs.Image.Image rgbImage = new Codecs.Image.Image(weird, 8, 8))
            {
                //Create the ImageFormat based on YUV packed but in Planar format with a full height luma plane and half hight chroma planes
                Media.Codecs.Image.ImageFormat Yuv420P = new Codecs.Image.ImageFormat(Media.Codecs.Image.ImageFormat.YUV(8, Common.Binary.ByteOrder.Little, Codec.DataLayout.Planar), new int[] { 0, 1, 1 });

                if (Yuv420P.IsInterleaved) throw new System.Exception("IsInterleaved should be false");

                if (Yuv420P.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be false");

                if (false == rgbImage.ImageFormat.HasAlphaComponent) throw new System.Exception("HasAlphaComponent should be true");

                if (rgbImage.ImageFormat.AlphaComponent.Size != 2) throw new System.Exception("AlphaComponent.Size should be 2 (bits)");

                //ImageFormat could be given directly to constructor here

                //Create the destination image
                using (Media.Codecs.Image.Image yuvImage = new Codecs.Image.Image(Yuv420P, 8, 8))
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

                //Done with the format.
                Yuv420P = null;
            }
        }
    }
}