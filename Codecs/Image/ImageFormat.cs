using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image
{

    public class ImageFormat : Codec.MediaFormat
    {
        #region Statics

        public const byte AlphaChannelId = (byte)'a';

        public const byte PreMultipliedAlphaChannelId = (byte)'A';

        //Possibly a type which has multiplied and straight types... 
        //public const byte MixedAlphaChannelId = (byte)'@';

        public const byte DeltaChannelId = (byte)'d';

        //

        public const byte LumaChannelId = (byte)'y';

        public const byte ChromaMajorChannelId = (byte)'u';

        public const byte ChromaMinorChannelId = (byte)'v';

        //

        public const byte RedChannelId = (byte)'r';

        public const byte GreenChannelId = (byte)'g';

        public const byte BlueChannelId = (byte)'b';

        //Printing...

        public const byte CyanChannelId = (byte)'c';

        public const byte MagentaChannelId = (byte)'m';

        //Capital of Luma
        public const byte YellowChannelId = (byte)'Y';

        //Key
        public const byte KChannelId = (byte)'k';

        //Functions for reading lines are in the type which corresponds, e.g. Image.

        //Could have support here for this given a MediaBuffer and forced / known format...

        #endregion

        #region Known Image Formats

        public static ImageFormat WithoutAlphaComponent(ImageFormat other)
        {
            return new ImageFormat(other.ByteOrder, other.DataLayout, other.Components.Where(c => c.Id != AlphaChannelId));
        }

        public static ImageFormat WithProceedingAlphaComponent(ImageFormat other, int sizeInBits)
        {
            return new ImageFormat(other.ByteOrder, other.DataLayout, other.Components.Where(c => c.Id != AlphaChannelId).Concat(Common.Extensions.Linq.LinqExtensions.Yield(new Codec.MediaComponent(AlphaChannelId, sizeInBits))));
        }

        public static ImageFormat WithPreceedingAlphaComponent(ImageFormat other, int sizeInBits)
        {
            return new ImageFormat(other.ByteOrder, other.DataLayout, Common.Extensions.Linq.LinqExtensions.Yield(new Codec.MediaComponent(AlphaChannelId, sizeInBits)).Concat(other.Components.Where(c => c.Id != AlphaChannelId)));
        }

        public static ImageFormat Binary(int bitsPerComponent) //Bayer / Binary 
        {
            return new ImageFormat(Common.Binary.ByteOrder.Little, Codec.DataLayout.Packed, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(DeltaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat Monochrome(int bitsPerComponent)
        {
            return new ImageFormat(Common.Binary.ByteOrder.Little, Codec.DataLayout.Packed, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat RGB(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(RedChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat ARGB(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId :AlphaChannelId, bitsPerComponent),
                new Codec.MediaComponent(RedChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat RGBA(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(RedChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent),
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat BGR(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout  dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(RedChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat BGRA(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(RedChannelId, bitsPerComponent),
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat ABGR(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent),
                new Codec.MediaComponent(BlueChannelId, bitsPerComponent),
                new Codec.MediaComponent(GreenChannelId, bitsPerComponent),
                new Codec.MediaComponent(RedChannelId, bitsPerComponent)                
            });
        }

        public static ImageFormat YUV(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            //Uglier version of the constructor
            //public static readonly ImageFormat YUV = new ImageFormat(Common.Binary.ByteOrder.Little, Codec.DataLayout.Packed, 3, 8, new byte[] { LumaChannelId, ChromaMajorChannelId, ChromaMinorChannelId });
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat YUVA(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent),
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat AYUV(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent),
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat VUY(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat VUYA(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(LumaChannelId, bitsPerComponent),
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : AlphaChannelId, bitsPerComponent)
            });
        }

        public static ImageFormat AVUY(int bitsPerComponent, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed, bool premultipliedAlpha = false)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(AlphaChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMinorChannelId, bitsPerComponent),
                new Codec.MediaComponent(ChromaMajorChannelId, bitsPerComponent),
                new Codec.MediaComponent(premultipliedAlpha ? PreMultipliedAlphaChannelId : LumaChannelId, bitsPerComponent)                
            });
        }

        //Supports 565 formats... etc.

        public static ImageFormat VariableYUV(int[] sizes, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(LumaChannelId, sizes[0]),
                new Codec.MediaComponent(ChromaMajorChannelId, sizes[1]),
                new Codec.MediaComponent(ChromaMinorChannelId, sizes[2])
            });
        }

        public static ImageFormat VariableRGB(int[] sizes, Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(RedChannelId, sizes[0]),
                new Codec.MediaComponent(GreenChannelId, sizes[1]),
                new Codec.MediaComponent(BlueChannelId, sizes[2])
            });
        }

        //Those formats used by the Variable functions.

        public static ImageFormat RGB_565(Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(RedChannelId, 5),
                new Codec.MediaComponent(GreenChannelId, 6),
                new Codec.MediaComponent(BlueChannelId, 5)
            });
        }

        //32 bit -> 2 bit alpha 10 bit r, g, b
        public static ImageFormat ARGB_230(Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(AlphaChannelId, 2),
                new Codec.MediaComponent(RedChannelId, 10),
                new Codec.MediaComponent(GreenChannelId, 10),
                new Codec.MediaComponent(BlueChannelId, 10)
            });
        }

        public static ImageFormat YUV_565(Common.Binary.ByteOrder byteOrder = Common.Binary.ByteOrder.Little, Codec.DataLayout dataLayout = Codec.DataLayout.Packed)
        {
            return new ImageFormat(byteOrder, dataLayout, new Codec.MediaComponent[]
            {
                new Codec.MediaComponent(LumaChannelId, 5),
                new Codec.MediaComponent(ChromaMajorChannelId, 6),
                new Codec.MediaComponent(ChromaMinorChannelId, 5)
            });
        }

        public static ImageFormat WithSubSampling(ImageFormat other, int[] sampling)
        {
            //if (System.Linq.Enumerable.SequenceEqual(other.Widths, sampling) && System.Linq.Enumerable.SequenceEqual(other.Heights, sampling)) return other;

            return new ImageFormat(other, sampling);
        }

        public static ImageFormat WithSubSampling(ImageFormat other, int[] widthSampling, int[] heightSampling)
        {
            //if (System.Linq.Enumerable.SequenceEqual(other.Widths, widthSampling) && System.Linq.Enumerable.SequenceEqual(other.Heights, heightSampling)) return other;

            return new ImageFormat(other, widthSampling, heightSampling);
        }

        public static ImageFormat Packed(ImageFormat other)
        {
            return new ImageFormat(Codec.MediaFormat.Packed(other));
        }

        public static ImageFormat Planar(ImageFormat other)
        {
            return new ImageFormat(Codec.MediaFormat.Planar(other));
        }

        public static ImageFormat SemiPlanar(ImageFormat other)
        {
            return new ImageFormat(Codec.MediaFormat.SemiPlanar(other));
            //return new ImageFormat(other.ByteOrder, Codec.DataLayout.SemiPlanar, other.Components);
        }

        #endregion

        //Used for subsampling.
        public int[] Widths, Heights;

        #region Constructors

        public ImageFormat(Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, int components, int bitsPerComponent, byte[] componentIds)
            : base(Codec.MediaType.Image,byteOrder, dataLayout, components, bitsPerComponent, componentIds)
        {
            //No sub sampling
            Heights = Widths = new int[components];
        }

        public ImageFormat(Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, int components, int[] componentSizes, byte[] componentIds)
            : base(Codec.MediaType.Image, byteOrder, dataLayout, components, componentSizes, componentIds)
        {
            //No sub sampling
            Heights = Widths = new int[components];
        }

        public ImageFormat(Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, System.Collections.Generic.IEnumerable<Codec.MediaComponent> components)
            : base(Codec.MediaType.Image, byteOrder, dataLayout, components)
        {
            //No sub sampling
            Heights = Widths = new int[Components.Length];
        }

        public ImageFormat(Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, params Codec.MediaComponent[] components)
            : base(Codec.MediaType.Image, byteOrder, dataLayout, components)
        {
            //No sub sampling
            Heights = Widths = new int[Components.Length];
        }

        public ImageFormat(ImageFormat other, params Codec.MediaComponent[] components)
            : base(other, other.ByteOrder, other.DataLayout, components)
        {
            Widths = other.Widths;

            Heights = other.Heights;
        }

        public ImageFormat(ImageFormat other, int[] sampling, params Codec.MediaComponent[] components)
            : base(other, other.ByteOrder, other.DataLayout, components) //: this(other,  components)
        {
            if (sampling == null) throw new System.ArgumentNullException("sampling");

            if (sampling.Length < Components.Length) throw new System.ArgumentOutOfRangeException("sampling", "Must have the same amount of elements as Components");

            //This needs to be able to reflect 4:4:4 or less
            //This is how this needs to look.
            
            //Sub Sampling | int | Example
            //           4 |   0 | 8 >> 0 = 8
            //           2 |   1 | 8 >> 1 = 4
            //           1 |   2 | 8 >> 2 = 2
            //        0.25 |   3 | 8 >> 3 = 1
            //           0 |   -1| skip

            Widths = Heights = sampling;
        }

        public ImageFormat(ImageFormat other, int[] widths, int[] heights, params Codec.MediaComponent[] components)
            : base(other, other.ByteOrder, other.DataLayout, components) //: this(other,  components)
        {
            if (widths == null) throw new System.ArgumentNullException("widths");

            if (widths.Length < Components.Length) throw new System.ArgumentOutOfRangeException("widths", "Must have the same amount of elements as Components");

            if (heights == null) throw new System.ArgumentNullException("heights");

            if (heights.Length < Components.Length) throw new System.ArgumentOutOfRangeException("widths", "Must have the same amount of elements as Components");

            Widths = widths;

            Heights = heights;
        }

        public ImageFormat(Codec.MediaFormat format)
            : base(format)
        {
            if (format == null) throw new System.ArgumentNullException("format");

            if (format.MediaType != Codec.MediaType.Image) throw new System.ArgumentException("format.MediaType", "Must be Codec.MediaType.Image.");
        }

        #endregion

        #region Properties

        public bool IsSubSampled { get { return Widths.Any(c => c > 0) || Heights.Any(h => h > 0); } }

        public Codec.MediaComponent AlphaComponent { get { return GetComponentById(AlphaChannelId); } }

        public bool HasAlphaComponent { get { return AlphaComponent != null; } }

        public bool IsPremultipliedWithAplha
        {
            get
            {
                Codec.MediaComponent alphaComponent = AlphaComponent;
                return alphaComponent!= null && alphaComponent.Id == PreMultipliedAlphaChannelId;
            }
        }

        #endregion

    }

    //Marked for removal
    ///// <summary>
    ///// Describes a PixelFormat
    ///// </summary>
    //public class PixelFormat
    //{
    //    #region KnownFormats

    //    public static PixelFormat YUV420p = new PixelFormat("yuv420p", 3, 1, 1, PixelFormatFlags.Planar,
    //        new ComponentDescriptor(0, 0, 1, 0, 7), 
    //        new ComponentDescriptor(1, 0, 1, 0, 7), 
    //        new ComponentDescriptor(2, 0, 1, 0, 7));

    //    public static PixelFormat YUYV422 = new PixelFormat("yuyv422", 3, 1, 0, PixelFormatFlags.None,
    //        new ComponentDescriptor(0, 1, 1, 0, 7),
    //        new ComponentDescriptor(0, 3, 2, 0, 7),
    //        new ComponentDescriptor(0, 3, 4, 0, 7));

    //    public static PixelFormat YVYU422 = new PixelFormat("yvyu422", 3, 1, 0, PixelFormatFlags.None,
    //        new ComponentDescriptor(0, 1, 1, 0, 7),
    //        new ComponentDescriptor(0, 3, 2, 0, 7),
    //        new ComponentDescriptor(0, 3, 4, 0, 7));

    //    public static PixelFormat RGB24 = new PixelFormat("rgb24", 3, 0, 0, PixelFormatFlags.RGB,
    //        new ComponentDescriptor(0, 2, 1, 0, 7),
    //        new ComponentDescriptor(0, 2, 2, 0, 7),
    //        new ComponentDescriptor(0, 2, 3, 0, 7));

    //    public static PixelFormat BGR24 = new PixelFormat("bgr24", 3, 0, 0, PixelFormatFlags.RGB,
    //       new ComponentDescriptor(0, 2, 1, 0, 7),
    //       new ComponentDescriptor(0, 2, 2, 0, 7),
    //       new ComponentDescriptor(0, 2, 3, 0, 7));

    //    #endregion

    //    #region Fields

    //    /// <summary>
    //    /// The name of the format
    //    /// </summary>
    //    public readonly string Name;

    //    /// <summary>
    //    /// The number of components in the format
    //    /// </summary>
    //    public readonly int Components;

    //    /// <summary>
    //    /// Amount to shift the luma width right to find the chroma width.
    //    /// </summary>
    //    public readonly int Log2ChromaWidth;
        
    //    /// <summary>
    //    /// Amount to shift the luma height right to find the chroma height.
    //    /// </summary>
    //    public readonly int Log2ChromaHeight;

    //    /// <summary>
    //    /// Other information
    //    /// </summary>
    //    public readonly PixelFormatFlags Flags;

    //    /// <summary>
    //    /// Parameters that describe how pixels are packed. 
    //    /// If the format has chroma components, they must be stored in ComponentDescriptions[1] and ComponentDescriptions[2].
    //    /// </summary>
    //    public ComponentDescriptor[] ComponentDescriptions;

    //    #endregion

    //    #region Constructor

    //    /// <summary>
    //    /// Constructs a new PixelFormat with the given configuration
    //    /// </summary>
    //    /// <param name="name"></param>
    //    /// <param name="numberOfComponents"></param>
    //    /// <param name="log2ChromaWidth"></param>
    //    /// <param name="log2ChromaHeight"></param>
    //    /// <param name="flags"></param>
    //    public PixelFormat(string name, int numberOfComponents, int log2ChromaWidth, int log2ChromaHeight, PixelFormatFlags flags, params ComponentDescriptor[] components)
    //    {
    //        if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("name", "Cannot be null or consist only of Whitespace.");
    //        Name = name;

    //        if (numberOfComponents <= 0) throw new System.ArgumentException("numberOfComponents", "Must be > than 0");
    //        Components = numberOfComponents;

    //        Log2ChromaHeight = log2ChromaHeight;

    //        Log2ChromaWidth = log2ChromaWidth;

    //        if (components == null || components.Length < Components) throw new System.ArgumentException("components", "Must be present and have the length indicated by numberOfComponents.");
    //        ComponentDescriptions = components;
    //    }

    //    #endregion
    //}

    //[Flags]
    //public enum PixelFormatFlags : byte
    //{
    //    None = 0,
    //    BigEndian = 1,
    //    Pal = 2,
    //    BitStream = 4,
    //    HWAccel = 8,
    //    Planar = 16,
    //    RGB = 32,//Packed
    //    PseudoPal = 64,
    //    Alpha = 128
    //}

    //public static class Extensions
    //{
    //    public static bool HasAlpha(this PixelFormat pf)
    //    {
    //        return pf != null && pf.Flags.HasFlag(PixelFormatFlags.Alpha);
    //    }

    //    public static bool IsPlanar(this PixelFormat pf)
    //    {
    //        return pf != null && pf.Flags.HasFlag(PixelFormatFlags.Planar);
    //    }

    //    public static bool IsPacked(this PixelFormat pf)
    //    {
    //        return false == IsPlanar(pf);
    //    }

    //    public static int NumberOfPlanes(this PixelFormat pf)
    //    {
    //        return pf.Components;
    //        //return pf.ComponentDescriptions.Length;
    //        //return pf.ComponentDescriptions.Sum(p => p.Plane > 0 ? 1 : 0);
    //    }

    //Basically all this supports and Read and Write API
    //A more managed solution would be IEnumerable<MemorySegment> or IEnumerable<byte[]> which would enumerate the lines for you

    //    public static void ReadImageLine(byte[] dst, byte[] data, int[] lineSize, PixelFormat pf, int x, int y, int c, int w, int read_pal_component)
    //    {
    //        ComponentDescriptor comp = pf.ComponentDescriptions[c];
    //        int plane = comp.Plane;
    //        int depth = comp.DepthMinus1 + 1;
    //        int mask = (1 << depth) - 1;
    //        int step = comp.StepMinus1 + 1;
    //        PixelFormatFlags flags = pf.Flags;

    //        int dstPtr = 0;

    //        if (flags.HasFlag(PixelFormatFlags.BitStream))
    //        {
    //            int skip = x * step + comp.OffsetPlus1 - 1;
    //            int p = data[plane] + y * lineSize[plane] + (skip >> 3);
    //            int shift = 8 - depth - (skip & 7);

    //            while (w-- > 0)
    //            {
    //                int val = (p >> shift) & mask;
    //                if (read_pal_component > 0)
    //                    val = data[1][4 * val + c];
    //                shift -= step;
    //                p -= shift >> 3;
    //                shift &= 7;
    //                dst[dstPtr++] = (byte)val;
    //            }
    //        }
    //        else
    //        {
    //            int p = data[plane] + y * lineSize[plane] + x * step + comp.OffsetPlus1 - 1;

    //            bool is_8bit = comp.Shift + depth <= 8 ? true : false;

    //            if (is_8bit)
    //                p += !!(flags & AV_PIX_FMT_FLAG_BE);

    //            while (w-- > 0)
    //            {
    //                int val = is_8bit ? p : flags & AV_PIX_FMT_FLAG_BE ? AV_RB16(p) : AV_RL16(p);

    //                val = (val >> comp.Shift) & mask;
    //                if (read_pal_component)
    //                    val = data[1][4 * val + c];
    //                p += step;
    //                dst[dstPtr++] = (byte)val;
    //            }
    //        }
    //    }
    //}
}
