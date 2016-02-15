//Marked for removal.

////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Text;
////using System.Threading.Tasks;

//////I don't like how you can't tell if the components are packed or planer in the ColorSpace

//////I don't like how you still can't tell the precision of a format or if it has an Alpha channel
//////Not to mention the Alpha channel can be represented in less pixels than the luma or chroma planes in some cases

//////Av does this with AVComponentDescriptor, AVPixFmtDescriptor defines the format for a picture

//////I played with porting them as ComponentDescriptor and PixelFormat, they are commented out for now because they still don't replace ColorSpace,
//////Namely because PixelFormat still doesn't allow you to define a PlaneOrder although it really doesn't matter because:
////// PixelFormat can have more components than planes, it can also have multiple components per plane?
////// PixelFormat RGB vs BGR looks exactly the same only you can't tell which plane is R, B or G

////// Seems like I will have to add name here and then use the name to map to the plane order to map them.
//////YUVXXX [0] => y, [1] => u, [2] => v, Planar = true
//////YUYVXXX [0] => y, [1] => u, [2] => v, Planar = false (determined by repeating Y)

////namespace Media.Codecs.Image
////{
////    ///// <summary>
////    ///// Represents a <see href="https://en.wikipedia.org/wiki/Color_space">ColorSpace</see>
////    ///// </summary>
////    //public class ColorSpace : Media.Common.BaseDisposable //Media.Codec.Format
////    //{

////    //    //Would need to know count of components in advance... not to mention half luma or choma
////    //    //static int[] Full = new int[] { 0, 0, 0 }, Half = new int[] { 0, 1, 1 };

////    //    #region Statics

////    //    public static readonly ColorSpace RGB = new ColorSpace(3, new int[] {0, 1, 2 }, new int[] { 0, 0, 0}, new int[] { 0, 0, 0 }, false);

////    //    public static readonly ColorSpace BGR = new ColorSpace(3, new int[] { 2, 1, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

////    //    public static readonly ColorSpace ARGB = new ColorSpace(4, new int[] { 0, 1, 2, 3 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace BGRA = new ColorSpace(4, new int[] { 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace YUV = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

////    //    public static readonly ColorSpace VUY = new ColorSpace(3, new int[] { 2, 1, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

////    //    public static readonly ColorSpace CMYK = new ColorSpace(4, new int[] { 0, 1, 2, 3 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace KYMC = new ColorSpace(4, new int[] { 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace CMYKA = new ColorSpace(5, new int[] { 0, 1, 2, 3, 4 }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace AKYMC = new ColorSpace(5, new int[] { 4, 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0 }, false);

////    //    public static readonly ColorSpace HSL = new ColorSpace(3);

////    //    public static readonly ColorSpace LSH = new ColorSpace(3, new int[] { 2, 1, 0 });

////    //    #endregion

////    //    /// <summary>
////    //    /// The minimum number of planes any ColorSpace can have.
////    //    /// </summary>
////    //    public const int MinimumPlanes = 1;
        
////    //    /// <summary>
////    //    /// The maximum number of planes any ColorSpace can have.
////    //    /// </summary>
////    //    public const int MaximumPlanes = 5;

////    //    /// <summary>
////    //    /// The amount of planes in the ColorSpace.
////    //    /// </summary>
////    //    public readonly int ComponentCount;

////    //    /// <summary>
////    //    /// An array which represents the order of the planes
////    //    /// </summary>
////    //    public readonly int[] Planes;

////    //    /// <summary>
////    //    /// An array which represents the divisor applied to obtain the width of a plane.
////    //    /// </summary>
////    //    public readonly int[] Widths;

////    //    /// <summary>
////    //    /// An array which represents the divisor applied to obtain the height of a plane.
////    //    /// </summary>
////    //    public readonly int[] Heights;

////    //    /// <summary>
////    //    /// Creates a new ColorSpace with the given configuration.
////    //    /// </summary>
////    //    /// <param name="componentCount">The amount of planes in the ColorSpace</param>
////    //    /// <param name="planes">The array which represents the order of the planes</param>
////    //    /// <param name="widths">The array which represents the width changes in each plane </param>
////    //    /// <param name="heights">The array which represents the height changes in each plane</param>
////    //    /// <param name="shouldDispose">Indicates if the instance should be able to be disposed. (True by default)</param>
////    //    public ColorSpace(int componentCount, int[] planes, int[] widths, int[] heights, bool shouldDispose = true)
////    //        :base(shouldDispose)
////    //    {
////    //        //Validate the componentCount
////    //        if (componentCount < MinimumPlanes || componentCount > MaximumPlanes) throw new ArgumentOutOfRangeException("componentCount", string.Format("Must be < {0} and > {1}.", MinimumPlanes, MaximumPlanes));
////    //        this.ComponentCount = componentCount;

////    //        //Validate and set the remaining properties, each given array must have the correct amount of elements.

////    //        if (planes == null || planes.Length < ComponentCount) throw new ArgumentOutOfRangeException("planes", string.Format("Must be have {0} elements.", ComponentCount));
////    //        this.Planes = planes;

////    //        if (widths == null || widths.Length < ComponentCount) throw new ArgumentOutOfRangeException("widths", string.Format("Must be have {0} elements.", ComponentCount));
////    //        this.Widths = widths;

////    //        if (heights == null || heights.Length < ComponentCount) throw new ArgumentOutOfRangeException("heights", string.Format("Must be have {0} elements.", ComponentCount));
////    //        this.Heights = heights;
////    //    }

////    //    /// <summary>
////    //    /// Creates a ColorSpace with the given number of components.
////    //    /// </summary>
////    //    /// <param name="componentCount">The number of components in the ColorSpace</param>
////    //    public ColorSpace(int componentCount)
////    //        : this(componentCount, Enumerable.Range(0, componentCount).ToArray(), new int[componentCount], new int[componentCount])
////    //    {

////    //    }

////    //    /// <summary>
////    //    /// Creates a ColorSpace with the given number of components and plane order.
////    //    /// </summary>
////    //    /// <param name="componentCount">The number of components in the ColorSpace</param>
////    //    /// <param name="planes">The array which represents the order of the planes</param>
////    //    public ColorSpace(int componentCount, int[] planes)
////    //        : this(componentCount, planes, new int[componentCount], new int[componentCount])
////    //    {

////    //    }

////    //    /// <summary>
////    //    /// Creates a ColorSpace with the given number of components, plane order and plane sizes, the sizes are used for width and height.
////    //    /// </summary>
////    //    /// <param name="componentCount">The number of components in the ColorSpace</param>
////    //    /// <param name="planes">The array which represents the order of the planes</param>
////    //    /// <param name="sizes">The array which represents the value used to scale the plane size</param>
////    //    public ColorSpace(int componentCount, int[] planes, int[] sizes)
////    //        : this(componentCount, planes, sizes, sizes)
////    //    {

////    //    }

////    //    /// <summary>
////    //    /// Clones a ColorSpace
////    //    /// </summary>
////    //    /// <param name="other">The ColorSpace to clone</param>
////    //    internal protected ColorSpace(ColorSpace other)
////    //        : this(other.ComponentCount, other.Planes, other.Widths, other.Heights)
////    //    {

////    //    }

////    //    /// <summary>
////    //    /// Creates a derivative of the given colorspace with different plane sizes
////    //    /// </summary>
////    //    /// <param name="other"></param>
////    //    /// <param name="sizes"></param>
////    //    public ColorSpace(ColorSpace other, int[] sizes)
////    //        : this(other.ComponentCount, other.Planes, sizes, sizes)
////    //    {

////    //    }

////    //    /// <summary>
////    //    /// Creates a derivative of the given colorspace with different plane sizes and optional reverse plane order
////    //    /// </summary>
////    //    /// <param name="other"></param>
////    //    /// <param name="sizes"></param>
////    //    public ColorSpace(ColorSpace other, int[] widths, int[] heights, bool reverse = false)
////    //        : this(other.ComponentCount, reverse ? other.Planes.Reverse().ToArray() : other.Planes, widths, heights)
////    //    {

////    //    }
////    //}
////}
