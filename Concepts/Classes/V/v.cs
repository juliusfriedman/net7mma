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

namespace Media.Concepts.Classes.v
{
    //Surface

    //Radiation ...

    //Laminate - laminado or lamina

    public sealed class Vitro /*glass*/
    {
        //--
    }

    //Astringent => Surface

    //- Iluminar illuminare (illuminareris / illūminārēris)
    public interface Illuminareris : Media.Common.Interfaces.Interface { }

    //Gaze, Observe

    /// <summary>
    /// `prototypum`
    /// </summary>
    internal class Prototype : Common.Classes.Class, Illuminareris, Media.Common.Interfaces.InterClass
    {
        #region Delegate

        /// <summary>
        /// A <see cref="Prototype"/> which itself is derived unto <see cref="Prototype"/>
        /// </summary>
        internal class Delegate : Prototype
        {
            /// <summary>
            /// <see cref="System.Delegate"/> or <see cref="delegate"/>
            /// </summary>
            public System.Delegate SystemDelegate
            {
                get;
                internal protected set;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="Delegate"/>
        /// </summary>
        internal protected Delegate Delegation
        {
            get;
            set;
        }

        #endregion

        #region Class.InterClass

        /// <summary>
        /// If <see cref="Delegation"/> is <see cref="null"/> then `this` otherwise that.
        /// </summary>
        Common.Classes.Class Common.Interfaces.InterClass.Class
        {
            get { return System.Object.ReferenceEquals(Delegation, null) ? this : Delegation; }
        }

        #endregion
    }

    //http://stackoverflow.com/questions/38468498/adding-to-a-message-for-various-errors-on-form/38468636#38468636

    /// <summary>
    /// A function used in <see cref="Prototype"/>
    /// </summary>
    /// <param name="v"></param>
    public delegate void Validator(out bool v);

    /// <summary>
    /// A function used in <see cref="Prototype"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <param name="v"></param>
    public delegate void Validator<T>(T t, out bool v);

    /// <summary>
    /// An <see cref="Media.Common.Interfaces.Interface"/> which asseses;
    /// </summary>
    public interface IValid : Media.Common.Interfaces.Interface
    {
        /// <summary>
        /// `principium`, likely an instance of <see cref="Validator"/>
        /// </summary>
        /// <remarks>
        /// <see href="https://en.wiktionary.org/wiki/principium#Latin">Wikitionary</see> for example: `In principio erat Verbum et Verbum erat apud Deum et Deus erat Verbum.`
        /// </remarks>
        System.Delegate Principium { get; }
    }

    /// <summary>
    /// <see href="https://en.wiktionary.org/wiki/rudimentum#Latin">rudimentum#Latin@Second declension</see>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public interface IValid<T> : IValid
    {
        /// <summary>
        /// The <see cref="Validator{T}"/>
        /// </summary>
        Validator<T> Rudīmentum { get; }
    }

    /// <summary>
    /// The <see cref="Common.Classes.Abstraction"/> associated with <see cref="IValid"/>
    /// </summary>
    public class AbstractValidator : Common.Classes.Abstraction, IValid
    {
        /// <summary>
        /// <see cref="null"/>
        /// </summary>
        const AbstractValidator NilValidator = null;

        /// <summary>
        /// For expansion.
        /// </summary>
        Prototype Prototype = null;

        /// <summary>
        /// by default, <see cref="Media.Common.Extensions.Delegate.ActionExtensions.NoOp"/>
        /// </summary>
        internal protected System.Delegate Void = Media.Common.Extensions.Delegate.ActionExtensions.NoOp;

        /// <summary>
        /// <see cref="Prototype"/> or the underlying <see cref="Prototype.Delegate"/> unless <see cref="null"/>, then <see cref="Void"/>
        /// </summary>
        System.Delegate IValid.Principium
        {
            get
            {
                return object.ReferenceEquals(Prototype, null) ? 
                    Void : object.ReferenceEquals(Prototype.Delegation, null) 
                        ? Void : Prototype.Delegation.SystemDelegate ?? Void;
            }
        }

        /// <summary>
        /// Create's a new instance with the <see cref="NilValidator"/>
        /// </summary>
        public AbstractValidator() : this(NilValidator) { }

        /// <summary>
        /// Assigning a new <see cref="Prototype"/> if <paramref name="other"/> is <see cref="NilValidator"/>, otherwise the underlying <see cref="Prototype.Delegation"/>
        /// </summary>
        /// <param name="other"></param>
        internal protected AbstractValidator(AbstractValidator other)
        {
            if (object.ReferenceEquals(other, NilValidator)) Prototype = new Prototype();
            else Prototype = new Prototype()
            {
                Delegation = other.Prototype.Delegation
            };
        }
    }

    /// <summary>
    /// The implemenation of <see cref="AbstractValidator"/> and <see cref="IValid{T}"/>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public abstract class AbstractValidator<T> : AbstractValidator, IValid<T>
    {
        /// <summary>
        /// <see cref="Validator{T}"/>
        /// </summary>
        public Validator<T> Validator { get; internal protected set; }

        /// <summary>
        /// <see cref="IValid{T}.Rudīmentum"/>
        /// </summary>
        Validator<T> IValid<T>.Rudīmentum
        {
            get { return Validator; }
        }

        /// <summary>
        /// <see cref="IValid.Principium"/>
        /// </summary>
        System.Delegate IValid.Principium
        {
            get { return base.Void; }
        }
    }

    /// <summary>
    /// An implementation of <see cref="AbstractValidator{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Invalidator<T> : AbstractValidator<T>
    {
        /// <summary>
        /// <see cref="False"/>
        /// </summary>
        internal static bool Nil = false;

        /// <summary>
        /// If <paramref name="t"/> is <see cref="null"/>, <paramref name="v"/> is true, otherwise false.
        /// </summary>
        /// <param name="t">t</param>
        /// <param name="v">v</param>
        static void Invalid (T t, out bool v){
            v = object.ReferenceEquals(t, null);
        }

        /// <summary>
        /// The static implemenation of `for` the given, using <see cref="Invalidator{T}"/> as a `basis`
        /// </summary>
        static Invalidator<T> Invalidate = new Invalidator<T>()
        {
            Void = System.Delegate.CreateDelegate(typeof(Invalidator<T>), Common.Extensions.ExpressionExtensions.SymbolExtensions.GetMethodInfo(()=>Invalidator<T>.Invalid(default(T), out Nil)))
        };
    }
}
