#region Copyright
/*.--.-.-.-.-.-..-.-.*/
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
/*
 * `Superposition` of `Layer(s)` programming in C#, Julius Richard Friedman, v// © : 1986 [2016, 2017, 2018, 2019, 2020]
 * This work is dedicated to `[Pk, Jf, Wf, NaS-f, jmcj, scap, Au, Zamknij się; inter alia, EtS, et aL...]` and with regard to the folowing;
 * $,ман,Lek,؋,ƒ,лв,₡,¥,₱,Kč,kr,₪,£,﷼,Rp,₮,₦,₩,₫,Z$
 * `of all my days [sic] of all my time; forever and one more` - `To each their own; individualistic and unique; free will;`
 * `be (living) on borrowed time` . used to say that someone has continued to survive against expectations, with the implication that this will not be for much longer .
 */
#endregion
namespace Media.Common.Interfaces
{
    #region Unrelated

    interface ISpace : Interface { } //@S

    internal interface ITime : Interface { }//@T

    class SpaceTime : ISpace, ITime { }

    /* Finite, In, De,
     *  `The theory of occupation` States many things, of which is:
     Where as `Space` has [among many] a property `Temperature`,
     The property `Temperature` is not [of] `Space` itself but exists within `Space`
     * 
     Where `Time` has the ability to reference a `Temperature` within `Space`,
     The property `Temperature` may be transient however from the perspective of `Time`,
     `Space` is bound unto itself through `Time` as understood in the concepts of `General Relativity`.
     * 
     Thus the `Temperature` exists infinitely, infinitely in `Space` by virtue of the face[fact] that before it exists in the constituent dimension `Space` it must have sufficient constituent `Time` to do so;
     * eternally through perpetuity, 
     * perpetually in `Time` through the bound reference of implicit `Space` it will occupy. 
     *      
     */

    #endregion

    /// <summary>
    /// In physics, (of waves) having no definite or stable phase relationship.
    /// </summary>
    public interface ICoherent : Interface { }

    #region Symbol

    #region Reference / Precedent / etc

    //Sorta like Ecma script Symbol.

    //https://github.com/clojure/clojure/blob/master/src/jvm/clojure/lang/Keyword.java

    #endregion

    #region ISymbolic

    /// <summary>
    /// 
    /// </summary>
    public interface ISymbolic : Common.Interfaces.Interface { }

    /// <summary>
    /// 
    /// </summary>
    public class Symbol : ISymbolic
    {
        //-
    }

    #endregion

    #endregion

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    internal interface IBridge : ICoherent, /**/ Interface
    {
        /// <summary>
        /// 
        /// </summary>
        InterStruct Bridge { get; }
    }

    #region IKey, IWord, IKeyWord, Keyword

    /// <summary>
    /// 
    /// </summary>
    public interface IKey : ISymbolic { }

    /// <summary>
    /// 
    /// </summary>
    public interface IWord : ISymbolic { }

    /// <summary>
    /// 
    /// </summary>
    public interface IKeyWord : IKey, IWord { }

    /// <summary>
    /// 
    /// </summary>
    public class Keyword : IKeyWord, IBridge
    {
        //
        InterStruct Structure;
        //

        internal protected Keyword()
        {
            
        }

        InterStruct IBridge.Bridge
        {
            get { return Structure; }
        }

        //KeywordStructure : =>
    }    

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Keyword<T> : Keyword, ITryGet<T>
    {

        #region Nested

        /// <summary>
        /// The `bridge` between <see cref="Keyword"/> and <see cref="string"/>
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public class StringClass : Classes.Class, ITryGet, ITryGet<StringClass>, InterClass
        {
            public const StringClass Nil = null;

            public static readonly StringClass Empty = new StringClass(string.Empty);

            [System.Runtime.InteropServices.FieldOffset(0)]
            object Object;

            [System.Runtime.InteropServices.FieldOffset(0)]
            string String;

            public StringClass(string the)
            {
                String = the;

                Object = this;
            }

            public StringClass(object o, string the)
            {
                String = the;

                Object = o;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Object.GetHashCode() ^ String.GetHashCode();
            }

            public override string ToString()
            {
                return String;
            }

            bool ITryGet.TryGet(out object t)
            {
                t = Object;

                return true;
            }

            bool ITryGet<StringClass>.TryGet(out StringClass t)
            {
                t = this;

                return true;
            }

            Classes.Class InterClass.Class
            {
                get { return this; }
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// 
        /// </summary>
        InterClass String;

        /// <summary>
        /// 
        /// </summary>
        T Key;

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public Keyword(T t, string name = null, bool context = false)
        {
            Key = t;

            String = new StringClass(context ? this : null, name);
        }

        #endregion

        #region ITryGet, ITryGet<T>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool ITryGet<T>.TryGet(out T t)
        {
            t = Key;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool ITryGet.TryGet(out object t)
        {
            t = Key;

            return true;
        }

        #endregion
    }

    #endregion

    #region Keywords

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Can : ICoherent, IKeyWord
    {
        bool Can { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface Can<T> : Can { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    /// <remarks>`cura`</remarks>
    public interface Care : ICoherent, IKeyWord
    {
        bool Care { get; }
    }

    public interface Care<T> : Care { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Is : ICoherent, IKeyWord
    {
        bool Is { get; }
    }

    public interface Is<T> : Is { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Has : ICoherent, IKeyWord
    {
        bool Has { get; }
    }

    public interface Has<T> : Has { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface In : ICoherent, IKeyWord
    {
        bool In { get; }
    }

    public interface In<T> : In { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Out : ICoherent, IKeyWord
    {
        bool Out { get; }
    }

    public interface Out<T> : Out { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface Enter : ICoherent, IKeyWord
    {
        bool Enter { get; }
    }

    public interface Enter<T> : Enter { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Leave : ICoherent, IKeyWord
    {
        bool Leave { get; }
    }

    public interface Leave<T> : Leave { }

    //Mask

    //Extract

    //If

    //Not

    //Compare

    //For

    //Do

    //While

    //Sign

    //Register

    //Declare

    //Assert

    //Insert

    //Before, After, Next, Current

    //Rotate

    //Get, Set

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface From : ICoherent, IKeyWord
    {
        bool From { get; }
    }

    public interface From<T> : From { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface To : ICoherent, IKeyWord
    {
        bool To { get; }
    }

    public interface To<T> : To { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="IKeyWord"/>
    /// </summary>
    public interface Convert : ICoherent, IKeyWord
    {
        bool Convert { get; }
    }

    public interface Convert<T> : Convert { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface Transient : ICoherent, IKeyWord
    {
        bool Transient { get; }
    }

    public interface Transient<T> : Transient { }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface Undefined : ICoherent, IKeyWord
    {
        bool Undefined { get; }
    }

    public interface Undefined<T> : Undefined { }

    #endregion

    #region Fields

    /// <summary>
    /// 
    /// </summary>
    public interface IField : ICoherent { }

    /// <summary>
    /// 
    /// </summary>
    public class Field : IField { }

    //Near, Far => IField

    //--

    //BA, BB, BT =? Register : IRegister, IHardware, IDuplex

    //--

    // Logics, Precedents, Rules and Procedures, Maths, => Intelligence, Sentience 

    //--

    //

    //ISystem, System

    //--

    //D, DS, FXM

    //SIMM, IMM, UIMM, etc (U, UI, |, ||, |||)

    //--

    #endregion

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/>
    /// </summary>
    public interface IModulation : ICoherent { }

    /// <summary>
    /// In electronics and telecommunications, 
    /// modulation is the process of varying one or more properties of a periodic waveform, 
    /// called the carrier signal, 
    /// with a modulating signal that typically contains information to be transmitted.
    /// </summary>
    public partial class Modulation : IModulation { }

    #region Internals

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    /// <remarks>If not of ([the] future, past)</remarks>
    internal interface Bc : ICoherent, IModulation
    {
        bool Bc { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    /// <remarks>Of modulation</remarks>
    internal interface Am : ICoherent, IModulation
    {
        bool Am { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    /// <remarks>Of modulation</remarks>
    internal interface Fm : ICoherent, IModulation
    {
        bool Fm { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    /// <remarks>If not of ([the] past, future)</remarks>
    internal interface De : ICoherent, IModulation
    {
        bool De { get; }
    }

    /// <summary>
    /// A <see cref="ICoherent"/> <see cref="Interface"/> which is not desinged for use from your code.
    /// </summary>
    /// <remarks>Of modulation</remarks>
    internal interface Um : ICoherent, IModulation
    {
        bool Um { get; }
    }

    #endregion    

    #region Signals

    //ISignal, Signal seperation

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

    //ISink, Sink & ISource, Source seperation

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
    /// <remarks>IMachineDuplex</remarks>
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

    #endregion
}
