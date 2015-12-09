using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image
{
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
