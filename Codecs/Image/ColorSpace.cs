using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image
{
    /// <summary>
    /// Represents a <see href="https://en.wikipedia.org/wiki/Color_space">ColorSpace</see>
    /// </summary>
    public class ColorSpace : Media.Common.BaseDisposable
    {

        #region Statics

        public static readonly ColorSpace RGB = new ColorSpace(3, new int[] {0, 1, 2 }, new int[] { 0, 0, 0}, new int[] { 0, 0, 0 }, false);

        public static readonly ColorSpace BGR = new ColorSpace(3, new int[] { 2, 1, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

        public static readonly ColorSpace ARGB = new ColorSpace(4, new int[] { 0, 1, 2, 3 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

        public static readonly ColorSpace BGRA = new ColorSpace(4, new int[] { 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

        public static readonly ColorSpace YUV = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

        public static readonly ColorSpace VUY = new ColorSpace(3, new int[] { 2, 1, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, false);

        public static readonly ColorSpace CMYK = new ColorSpace(4, new int[] { 0, 1, 2, 3 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

        public static readonly ColorSpace KYMC = new ColorSpace(4, new int[] { 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, false);

        public static readonly ColorSpace CMYKA = new ColorSpace(5, new int[] { 0, 1, 2, 3, 4 }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0 }, false);

        public static readonly ColorSpace AKYMC = new ColorSpace(5, new int[] { 4, 3, 2, 1, 0 }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0 }, false);

        public static readonly ColorSpace HSL = new ColorSpace(3);

        public static readonly ColorSpace LSH = new ColorSpace(3, new int[] { 2, 1, 0 });

        #endregion

        /// <summary>
        /// The minimum number of planes any ColorSpace can have.
        /// </summary>
        public const int MinimumPlanes = 1;
        
        /// <summary>
        /// The maximum number of planes any ColorSpace can have.
        /// </summary>
        public const int MaximumPlanes = 5;

        /// <summary>
        /// The amount of planes in the ColorSpace.
        /// </summary>
        public readonly int ComponentCount;

        /// <summary>
        /// An array which represents the order of the planes
        /// </summary>
        internal readonly protected int[] m_Planes;

        /// <summary>
        /// An array which represents the divisor applied to the width of a plane.
        /// </summary>
        internal readonly protected int[] m_Widths;

        /// <summary>
        /// An array which represents the divisor applied to the height of a plane.
        /// </summary>
        internal readonly protected int[] m_Heights;

        /// <summary>
        /// Creates a new ColorSpace with the given configuration.
        /// </summary>
        /// <param name="componentCount">The amount of planes in the ColorSpace</param>
        /// <param name="planes">The array which represents the order of the planes</param>
        /// <param name="widths">The array which represents the width changes in each plane </param>
        /// <param name="heights">The array which represents the height changes in each plane</param>
        /// <param name="shouldDispose">Indicates if the instance should be able to be disposed. (True by default)</param>
        public ColorSpace(int componentCount, int[] planes, int[] widths, int[] heights, bool shouldDispose = true)
            :base(shouldDispose)
        {
            //Validate the componentCount
            if (componentCount < MinimumPlanes || componentCount > MaximumPlanes) throw new ArgumentOutOfRangeException("componentCount", string.Format("Must be < {0} and > {1}.", MinimumPlanes, MaximumPlanes));
            this.ComponentCount = componentCount;

            //Validate and set the remaining properties, each given array must have the correct amount of elements.

            if (planes == null || planes.Length < ComponentCount) throw new ArgumentOutOfRangeException("planes", string.Format("Must be have {0} elements.", ComponentCount));
            this.m_Planes = planes;

            if (widths == null || widths.Length < ComponentCount) throw new ArgumentOutOfRangeException("widths", string.Format("Must be have {0} elements.", ComponentCount));
            this.m_Widths = widths;

            if (heights == null || heights.Length < ComponentCount) throw new ArgumentOutOfRangeException("heights", string.Format("Must be have {0} elements.", ComponentCount));
            this.m_Heights = heights;
        }

        /// <summary>
        /// Creates a ColorSpace with the given number of components.
        /// </summary>
        /// <param name="componentCount">The number of components in the ColorSpace</param>
        public ColorSpace(int componentCount)
            : this(componentCount, Enumerable.Range(0, componentCount).ToArray(), new int[componentCount], new int[componentCount])
        {

        }

        /// <summary>
        /// Creates a ColorSpace with the given number of components and plane order.
        /// </summary>
        /// <param name="componentCount">The number of components in the ColorSpace</param>
        /// <param name="planes">The array which represents the order of the planes</param>
        public ColorSpace(int componentCount, int[] planes)
            : this(componentCount, planes, new int[componentCount], new int[componentCount])
        {

        }

        /// <summary>
        /// Clones a ColorSpace
        /// </summary>
        /// <param name="other">The ColorSpace to clone</param>
        internal protected ColorSpace(ColorSpace other)
            : this(other.ComponentCount, other.m_Planes, other.m_Widths, other.m_Heights)
        {

        }
    }
}
