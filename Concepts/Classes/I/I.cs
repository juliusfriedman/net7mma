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
namespace Media.Concepts.Classes.I
{
    #region ICore

    /// <summary>
    /// `incu`
    /// </summary>
    internal interface ICore : Media.Common.Interfaces.Interface
    {
        //#$
    }

    #endregion

    #region IPtr, IPointer

    public interface IPtr : Media.Common.Interfaces.Interface
    {
        System.IntPtr IntPtr { get; }
    }

    public interface IPointer : IPtr
    {
        /// <summary>
        /// The version of the ptr
        /// </summary>
        byte Version { get; }

        //The size of the ptr, which can be used to determine how `Offset` and `Length` are calulcated when used in conjunction with `Version`      
        byte Size { get; }

        //The offset of the ptr
        short Offset { get; }

        //The length of the ptr
        short Length { get; }
    }

    #endregion

    #region IStructure

    /// <summary>
    /// A interface which represents a ValueType
    /// </summary>
    public interface IStructure : Media.Common.Interfaces.Interface
    {
        /// <summary>
        /// The underlying <see cref="System.ValueType"/>
        /// </summary>
        System.ValueType ValueType { get; }
    }

    #endregion

    #region IStructure<T>

    /// <summary>
    /// A interface which represents <see cref="{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IStructure<T> : IStructure
    {
        /// <summary>
        /// The underlying <see cref="{T}"/>
        /// </summary>
        T Element { get; set; }
    }

    #endregion

    #region Structure<T>

    /// <summary>
    /// A structure which consists of only <see cref="{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Structure<T> : IStructure<T>
    {
        T Value;

        T IStructure<T>.Element
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }

        System.ValueType IStructure.ValueType
        {
            get { return this; }
        }

        public Structure(T element)
        {
            Value = element;
        }

        #region Overrides

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        #endregion
    }

    #endregion

    #region IReference

    /// <summary>
    /// A interface which consists of only <see cref="System.Object"/>
    /// </summary>
    public interface IReference : Media.Common.Interfaces.Interface
    {
        /// <summary>
        /// The underlying <see cref="System.Object"/>
        /// </summary>
        object Object { get; set; }
    }

    #endregion

    #region IReference<T>

    /// <summary>
    /// A interface which specifies at least one concrete specification of <see cref="{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReference<T> : IReference
    {
        IStructure<T> Element { get; set; }
    }

    #endregion

    //{S}pecific

    #region Reference<T>

    public class Reference<T> : IReference<T>
    {
        IStructure<T> Value;

        IStructure<T> IReference<T>.Element
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }

        public Reference(T element)
        {
            Value = new Structure<T>(element);
        }

        public Reference(IStructure<T> structure)
        {
            Value = structure;
        }

        object IReference.Object
        {
            get
            {
                return Value.Element;
            }
            set
            {
                Value.Element = (T)value;
            }
        }

        #region Overrides

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        #endregion

    }

    #endregion

    #region ContrivedReference<U, T>

    public abstract class ContrivedReference<U, T> : 
        Reference<T>, //base
        IReference<U>, //this
        Media.Common.Interfaces.IComposed<T>
        where U : IReference<U>
        where T : class, IReference<T>
    {
        IStructure<U> Contrived;

        public ContrivedReference(U u)
            : base(u as T)
        {
            Contrived = (IStructure<U>)u;
        }

        public ContrivedReference(U u, T t)
            : base(t)
        {
            Contrived = (IStructure<U>)u;
        }

        IStructure<U> IReference<U>.Element
        {
            get
            {
                return Contrived;
            }
            set
            {
                Contrived = value;
            }
        }

        object IReference.Object
        {
            get
            {
                return Contrived.Element;
            }
            set
            {
                Contrived.Element = (U)value;
            }
        }

        T Media.Common.Interfaces.IComposed<T>.ComposedElement
        {
            get { return ((IStructure<T>)this).Element; }
        }

        object Media.Common.Interfaces.IComposed.ComposedObject
        {
            get { return ((IStructure<T>)this).ValueType; }
        }
    }

    #endregion

    #region

    sealed class Atonement
    {

    }

    sealed class Declension
    {

    }

    sealed class Astringent
    {

    }

    #endregion
}
