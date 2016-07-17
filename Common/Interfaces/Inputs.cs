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

namespace Media.Common.Interfaces
{
    interface ISpace : Interface { } //@S

    public interface ITime : Interface { }//@T

    class SpaceTime : ISpace, ITime { }

    /* Finite, In, De,
     *  `The theory of occupation` States many things, of which is:
     Where as `Space` has [among many] a property `Temperature`,
     The property `Temperature` is not [of] `Space` itself but exists within `Space`
     * 
     Where `Time` has the ability to reference a `Temperature` within `Space`,
     The property `Temperature` may be transient however from the perspective of `Time`,
     `Space` is bound unto itself through `Time` as understood in the conepts of `General Relativity`.
     * 
     Thus the `Temperature` exists infinitely, infinitely in `Space` by virtue of the face that before it exists in the dimension `Space` it must have `Time` to do so;
     * eternally through perpetuity, 
     * perpetually in `Time` through the bound reference of implicit `Space` it will occupy. 
     *      
     */

    /// <summary>
    /// In physics, (of waves) having no definite or stable phase relationship.
    /// </summary>
    public interface ICoherent : Interface { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface Is : ICoherent
    {
        bool Is { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface Has : ICoherent
    {
        bool Has { get; }
    }

    #region Internals

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    internal interface Bc : ICoherent
    {
        bool Bc { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    internal interface Am : ICoherent
    {
        bool Am { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    internal interface Fm : ICoherent
    {
        bool Fm { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    internal interface De : ICoherent
    {
        bool De { get; }
    }

    #endregion

    /// <summary>
    /// Represents an <see cref="ICoherent"/> <see cref="Interface"/> implementation of both <see cref="Is"/> and <see cref="Has"/>.
    /// </summary>
    /// <remarks>Duplexity is not asseded at this level</remarks>
    public interface Signal : ICoherent, Is, Has //, Am, De
    {
        //+-

        //culmination, nadir / high, low
    }

    //Uhf, Vfh, etc

    /// <summary>
    /// A <see cref="Signal"/>
    /// </summary>
    public interface Sink : Signal { }

    /// <summary>
    /// A <see cref="Signal"/>
    /// </summary>
    public interface Source : Signal { }

    /// <summary>
    /// Represents a coherent interface over <see cref="Signal"/>
    /// </summary>
    public interface Input : Signal
    {
        /// <summary>
        /// Is this instance input.
        /// </summary>
        bool IsInput { get; }

        /// <summary>
        /// Is this instance output.
        /// </summary>
        bool IsOutput { get; }
        
        /// <summary>
        /// Does this instance have input
        /// </summary>
        bool HasInput { get; }

        /// <summary>
        /// Does this instance have output
        /// </summary>
        bool HasOutput { get; }
    }

    /// <summary>    
    /// (of a machine) having two identical working units, operating together or independently, in a single framework or assembly.
    /// </summary>
    public interface IDuplex : Input
    {
        //@IDefinition!?!?

        /// <summary>
        /// The <see cref="Classes.Class"/> which defines the duplexity.
        /// </summary>
        Classes.Class Duplexity { get; }

        //@ Signal        
    }

    /// <summary>
    /// Represents a <see cref="IDuplex"/> of <see cref="Input"/>.
    /// </summary>
    public interface Transmission : IDuplex
    {
        /// <summary>
        /// 
        /// </summary>
        IDuplex InputSignal { get; }

        /// <summary>
        /// 
        /// </summary>
        IDuplex OutputSignal { get; }

        /// <summary>
        /// 
        /// </summary>
        IDuplex Ground { get; }

        /// <summary>
        /// The result of culmination is another <see cref="Transmission"/> ...
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        Transmission Coalesce(Transmission other);

        /// <summary>
        /// The result of isolation is another <see cref="Transmission"/> ...
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        Transmission Isolate(Transmission other);

        /// <summary>
        /// The result of disaccociation is another <see cref="Transmission"/> ...
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        Transmission Dissociate(Transmission other);

        //Union, Intersect, etc
    }
}
