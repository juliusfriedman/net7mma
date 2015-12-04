using System.Collections.Generic;
using System.Linq;
namespace Media.Codecs.Image
{
    //Odd bit sizes will be harder to represent or will require padding
    //https://en.wikipedia.org/wiki/Chroma_subsampling

    public enum ColorSpace
    {
        Unknown,
        BW, // 1 or 8
        GRAY, // 8, 16, 24?
        RGB,
        ARGB,
        BGR,
        BGRA,
        YUV,
        //Really the sub sampling and further more each has a packed and planar variant.
        YUV_300,
        YUV_310,
        YUV_311,
        YUV_312,
        YUV_320,
        YUV_321,
        YUV_322,
        //Needs to have a seperate definition for sub sampling
        YUV_400, // Greyscale
        YUV_410,
        YUV_411,
        YUV_412,
        YUV_420, //Half Width
        YUV_421,
        YUV_424,
        YUV_222,
        YUV_224,
        YUV_422,
        YUV_444 
        // Could Also be something like 16 bit=>{5,5,5,1}
    }

    /// <summary>
    /// Indicates how individual colors are extracted from a Image in memory.
    /// </summary>
    public enum Packing
    {
        Unknown = 0,
        //Each color is next to each other in memory
        Packed = 1,
        //Each color is offset by a fixed amount depending on the SamplingRatio
        Planar = 2,
        Other
    }

    //public class FormatInfo
    //{
    //    ColorSpace ColorSpace;
    //    int BitsPerPixel;
    //    Packing Packing;
    //    //FieldOrder

    //    //SamplingRatio
    //}

    //needs Basics => Size, Point, Ratio, Fraction, Shape, Rectangle

    public class Ratio { }//GCD, Reduce

    public class SamplingRatio
    {
        //Ugly because might want FullHeight or FullWidth not to mention alpha....
        //public static readonly SamplingRatio FullAlpha = new SamplingRatio() { J = 4, a = 4, b = 4, Alpha = true};

        //public static readonly SamplingRatio FullNoAlpha = new SamplingRatio() { J = 4, a = 4, b = 4 };

        //public static readonly SamplingRatio Half = new SamplingRatio() { J = 4, a = 2, b = 2, Alpha = true };

        //Mark readonly since it doesn't make sense to change a SamplingRatio after construction.

        /// <summary>
        /// horizontal sampling reference (width of the conceptual region). Usually, 4.
        /// </summary>
        public double J;

        /// <summary>
        /// number of chrominance samples (Cr, Cb) in the first row of J pixels.
        /// </summary>
        public double a;

        /// <summary>
        /// number of changes of chrominance samples (Cr, Cb) between first and second row of J pixels.
        /// </summary>
        public double b;

        //Maybe should just be double

        /// <summary>
        /// Alpha: horizontal factor (relative to first digit). May be omitted if alpha component is not present, and is equal to J when present.
        /// </summary>
        public bool Alpha;

        /// <summary>
        /// Determines how many components are present
        /// </summary>
        public int Components
        {
            get
            {
                int result = 0;

                if (J != 0) ++result;

                if (a != 0) ++result;

                if (b != 0) ++result;

                if (Alpha) ++result;

                return result;
            }
        }

        /// <summary>
        /// Parses a string in the format J:a:b(:Alpha)
        /// This notation is not valid for all combinations and has exceptions, e.g. 4:1:0 (where the height of the region is not 2 pixels but 4 pixels, so if 8 bits/component are used the media would be 9 bits/pixel) and 4:2:1.
        /// '4:2:1' is an obsolete term from a previous notational scheme, and very few software or hardware codecs use it. 
        /// Cb horizontal resolution is half that of Cr (and a quarter of the horizontal resolution of Y). 
        /// This exploits the fact that human eye has less spatial sensitivity to blue/yellow than to red/green. 
        /// NTSC is similar, in using lower resolution for blue/yellow than red/green, which in turn has less resolution than luma.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public bool TryParse(string input, out SamplingRatio ratio)
        {
            ratio = null; 
            
            if(string.IsNullOrWhiteSpace(input)) return false;

            string[] tokens = input.Split(':');

            long length = tokens.Length;

            if (length == 0) return false;

            ratio = new SamplingRatio();

            if (length >= 1) ratio.J = double.Parse(tokens[0]);

            if (length >= 2) ratio.a = double.Parse(tokens[1]);

            if (length >= 3) ratio.b = double.Parse(tokens[2]);

            //Might change J but shouldn't matter because J should always be equal to Alpha when present...
            if (length >= 4) ratio.Alpha = double.TryParse(tokens[3], out ratio.J);

            return true;
        }

        public override string ToString()
        {
            return Alpha ? string.Join(":", J, a, b, J) : string.Join(":", J, a, b);
        }

        public SamplingRatio() { }

        public SamplingRatio(double j, double a, double b, bool alpha)
        {
            J = j;

            this.a = a;

            this.b = b;

            Alpha = alpha;
        }

        //If implement Ratio then can use Ratio with J, a or b to increase or reduce.

        //Might want to Up/Downsample only J or A or B

        //public SamplingRatio DownSample(double factor)
        //{
        //    return new SamplingRatio(J / factor, a / factor, b / factor, Alpha);
        //}

        //public SamplingRatio UpSample(double factor)
        //{
        //    if (factor == 1) return this;

        //    return new SamplingRatio(J * factor, a * factor, b * factor, Alpha);
        //}

    }

    //4:4:4 can be represented as Full
    //4:2:4 Can be represented as Full, Half, Full
    //4:2:1 can be represented as Full, Half, Quarter
    //4:2:0 can be represented as Full, Half, None

    //public class PlaneInfo
    //{
    //    public const PlaneInfo None = new PlaneInfo() { HeightRatio = 0, WidthRatio = 0 };

    //    public const PlaneInfo Full = new PlaneInfo() { HeightRatio = 1, WidthRatio = 1 };

    //    public const PlaneInfo Half = new PlaneInfo() { HeightRatio = .5, WidthRatio = .5 };

    //    public const PlaneInfo Quarter = new PlaneInfo() { HeightRatio = .25, WidthRatio = .25 };

    //    public const PlaneInfo ThreeQuarter = new PlaneInfo() { HeightRatio = .75, WidthRatio = .75 };

    //    //FieldOrder
    //    double HeightRatio, WidthRatio;
    //}

    //public enum FieldOrder
    //{
    //    Unknown = 0,
    //    Top = 1,
    //    Bottom = 2,
    //    Left = 4,
    //    Right = 8
    //}

    //Could be a class, and could contain the logic for pixel translation

    //All formats would be registered before any Image could be created.

    //jCodec implements this with a non contigious allocation for the data which decreases performance
    //Both [,] and [] are about the same performance wise and an indexer for [,] can be made to read from the []
    //https://github.com/jcodec/jcodec/blob/master/src/main/java/org/jcodec/common/model/Picture8Bit.java

    //Color

    //Raster could then be x, y of Color

    public class Image : Media.Codec.MediaBuffer
    {
        //static Image Crop(Image source)

        //Needs bits per pixel to accurately calulcate size.
        static int CalculateSize(ColorSpace colorSpace, int width, int height)
        {
            int byteSize = width * height;

            //if bpp < 8 byteSize byteSize /= Common.Binary.BitsToBytes(bpp)

            switch (colorSpace)
            {
                case ColorSpace.YUV_420: return byteSize *= 3 / 2; //1 full plane and 1 half plane
                case ColorSpace.YUV_422:
                case ColorSpace.YUV_224: return byteSize *= 2; // 2 full planes
                case ColorSpace.YUV_444: return byteSize *= 3; // 3 full planes
                case ColorSpace.YUV_400://1 full plane
                default: return byteSize;
            }
        }

        protected readonly ColorSpace m_Format;
        protected readonly int m_Width;
        protected readonly int m_Height;
        protected readonly int m_BitsPerPixel;
        protected readonly Packing m_Packing;

       //Needs a FieldOrder, Top or Bottom, Left or Right

        public Image(ColorSpace format, int width, int height) 
            //Needs a static CalculateSize
            : base(Media.Codec.MediaType.Image, CalculateSize(format, width, height), Common.Binary.SystemByteOrder, 8, 4)
        {
            m_Format = format;
            m_Width = width;
            m_Height = height;
        }

        public int Width { get { return m_Width; } }
        
        public int Height { get { return m_Height; } }

        public int Size { get { return Data.Count; } }

        public int ComponentSize { get { return m_BitsPerPixel; } }

        public Packing Packing { get { return m_Packing; } }

        //public int this[int x, int y]
        //{
        //    get
        //    { //Packed ?
        //        return x * Height + y * BitsPerComponent;
        //      //Planar :
        //    }
        //}

        //Useful for returning only a single plane, e.g. R, G, B or A, Y, U or V etc.

        //Should be a member of Format...

        //public IEnumerable<byte> GetPlanarData(int plane)
        //{
        //    int offsetToPlane = 0;

        //    for (int y = 0; y < m_Height; ++y)
        //    {
        //        for (int x = 0; x < m_Width; ++x)
        //        {
        //            yield return Data[y + x + plane];

        //            //Not correct, needs BPP
        //            offsetToPlane += Size;
        //        }
        //    }
        //}

        //public void convertYUVtoRGB(int[] pixels)
        //{
        //    int scaleX, scaleY;

        //    switch (m_Format)
        //    {
        //        case ColorSpace.YUV_400: 
        //        case ColorSpace.YUV_444: scaleX = 1; scaleY = 1; break;
        //        case ColorSpace.YUV_420: scaleX = 2; scaleY = 2; break;
        //        case ColorSpace.YUV_422: scaleX = 1; scaleY = 2; break;
        //        case ColorSpace.YUV_224: scaleX = 2; scaleY = 1; break;
        //        default: scaleX = 1; scaleY = 1; break;
        //    }

        //    int base_y = 0;
        //    int base_u = base_y + m_Width * m_Height;
        //    int base_v = base_u + (m_Width / scaleX) * (m_Height / scaleY);
        //    int stride_y = m_Width;
        //    int stride_u = m_Width / scaleX;
        //    int stride_v = m_Width / scaleX;
        //    byte by = (byte)128;
        //    byte bu = (byte)128;
        //    byte bv = (byte)128;

        //    for (int y = 0; y < m_Height; y++)
        //    {
        //        for (int x = 0; x < m_Width; x++)
        //        {
        //            by = Data[base_y + stride_y * y + x];

        //            if (m_Format != ColorSpace.YUV_400) //444
        //            {
        //                bu = Data[base_u + stride_u * (y / scaleY) + (x / scaleX)];
        //                bv = Data[base_v + stride_v * (y / scaleY) + (x / scaleX)];
        //            }

        //            //Set the pixel
        //            pixels[m_Width * y + x] = Image.convertYuvPixel(by, bu, bv);
        //        }
        //    }
        //}

        //static int convertYuvPixel(byte y, byte u, byte v)
        //{
        //    int iy = y & 0xff;
        //    int iu = u & 0xff;
        //    int iv = v & 0xff;

        //    int miy = iy - 16, miv = iv - 128, miu = iu - 128;

        //    float fiy = 1.164f * miy;

        //    float fr = fiy + 1.596f * miv;
        //    float fg = fiy - 0.391f * miu - 0.813f * miv;
        //    float fb = fiy + 2.018f * miu;

        //    int ir = (int)(fr > 255 ? 255 : fr < 0 ? 0 : fr);
        //    int ig = (int)(fg > 255 ? 255 : fg < 0 ? 0 : fg);
        //    int ib = (int)(fb > 255 ? 255 : fb < 0 ? 0 : fb);

        //    return (ir << 16) | (ig << 8) | (ib);
        //}
    }

    //Drawing?

    //Will eventually need Font support...
}
