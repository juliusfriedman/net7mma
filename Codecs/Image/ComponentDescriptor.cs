using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Should be in Codec and should be more like MediaFormat

namespace Media.Codecs.Image
{
    ///// <summary>
    ///// Describes a component and how to access it 
    ///// </summary>
    //public class ComponentDescriptor
    //{
    //    /// <summary>
    //    /// The index of the plane which contains the component
    //    /// </summary>
    //    public readonly int Plane;

    //    /// <summary>
    //    /// Number of elements between 2 horizontally consecutive pixels minus 1.
    //    /// Elements are bits for bitstream formats, bytes otherwise.
    //    /// </summary>
    //    public readonly int StepMinus1 = 3;

    //    /// <summary>
    //    /// Number of elements before the component of the first pixel plus 1.
    //    /// Elements are bits for bitstream formats, bytes otherwise.
    //    /// </summary>
    //    public readonly int OffsetPlus1 = 3;

    //    /// <summary>
    //    /// Number of least significant bits that must be shifted away to get the value
    //    /// </summary>
    //    public readonly int Shift = 3;

    //    /// <summary>
    //    /// Number of bits in the component minus 1
    //    /// </summary>
    //    public readonly int DepthMinus1 = 4;

    //    /// <summary>
    //    /// Constructs a new ComponentDescriptor with the given configuration
    //    /// </summary>
    //    /// <param name="plane"><see cref="Plane"/></param>
    //    /// <param name="stepMinus1"><see cref="StepMinus1"/></param>
    //    /// <param name="offsetPlus1"><see cref="OffsetPlus1"/></param>
    //    /// <param name="shift"><see cref="Shift"/></param>
    //    /// <param name="depthMinus1"><see cref="DepthMinus1"/></param>
    //    public ComponentDescriptor(int plane, int stepMinus1, int offsetPlus1, int shift, int depthMinus1)
    //    {
    //        Plane = plane;

    //        StepMinus1 = stepMinus1;

    //        OffsetPlus1 = offsetPlus1;

    //        Shift = shift;

    //        DepthMinus1 = depthMinus1;
    //    }
    //}
}
