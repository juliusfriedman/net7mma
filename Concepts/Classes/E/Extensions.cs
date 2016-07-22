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

namespace Media.Concepts.Classes.E
{
    /// <summary>
    /// An <see cref="Media.Common.Interfaces.Interface"/> which unifies types of extensions.
    /// </summary>
    public interface IExtensions : Media.Common.Interfaces.Interface { }

    /// <summary>
    /// An <see cref="Media.Common.Classes.Abstraction"/> of <see cref="IExtensions"/> which is intended to provide operators for `==` and `!=`
    /// </summary>
    public abstract class Extensions : Media.Common.Classes.Abstraction, IExtensions
    {
        public const Extensions NilExtensions = null;

        /// <summary>
        /// <see cref="Equals"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Extensions a, object b)
        {
            return object.ReferenceEquals(b, NilExtensions) ? object.ReferenceEquals(a, NilExtensions) : a.Equals(b);
        }

        /// <summary>
        /// Uses <see cref="@operator@=="/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Extensions a, object b) { return (a == b).Equals(false); }

        /// <summary>
        /// Returns this instance casted to a <see cref="System.ValueType"/>
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static implicit operator System.ValueType(Extensions a)
        {
            return a;
        }

        //ReferenceType not allowed
        //public static implicit operator Extensions(object a)
        //{
        //    return a;
        //}

        //public static implicit operator object(Extensions a)
        //{
        //    return a;
        //}

        //bool, !

        //int, ~, >, <, >=, <=

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe bool Equals(Extensions other)
        {
            return object.ReferenceEquals(this, NilExtensions) ? object.ReferenceEquals(other, NilExtensions) : Unsafe.AddressOf(this) == Unsafe.AddressOf(other);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) return true;

            if ((obj is Extensions).Equals(false)) return false;

            return Equals(obj as Extensions);
        }

        //Todo, @IHashCode{}:ICode{},IHash{}
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
