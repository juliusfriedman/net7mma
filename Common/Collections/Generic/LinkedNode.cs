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

namespace Media.Common.Collections.Generic
{
    /// <summary>
    /// A node which has a <see cref="Value"/> and <see cref="Flags"/> field and a reference to the <see cref="Next"/> and <see cref="Previous"/> node.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedNode<T>
    {
        #region Statics

        /// <summary>
        /// The Null LinkedNode
        /// </summary>
        public const LinkedNode<T> Null = null;

        /// <summary>
        /// Inserts a given LinkedNode before this LinkedNode
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static LinkedNode<T> InsertBefore(LinkedNode<T> node, ref T value)
        {
            LinkedNode<T> result = new LinkedNode<T>(ref value);

            result.Next = node;

            result.Previous = node.Previous;

            node.Previous.Next = result;

            node.Previous = result;

            return result;
        }

        /// <summary>
        /// Adds the given LinkedNode after this LinkedNode
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static LinkedNode<T> AddAfter(LinkedNode<T> node, ref T value)
        {
            LinkedNode<T> result = new LinkedNode<T>(ref value);

            result = LinkedNode<T>.InsertBefore(node.Next, ref value);

            return result;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The value
        /// </summary>
        //Todo, Atomic / IAtomic
        public T Value;

        /// <summary>
        /// The Next
        /// </summary>
        public LinkedNode<T> Next;

        /// <summary>
        /// The Previous
        /// </summary>
        public LinkedNode<T> Previous;

        /// <summary>
        /// Any flags use for <see cref="State"/>
        /// </summary>
        [System.CLSCompliant(false)]
        internal protected uint Flags;

        //Owner could be given in Flags or otherwise.

        #endregion

        #region Properties

        /// <summary>
        /// Use as defined by the implementation
        /// </summary>
        /// <remarks>
        ///  None, Allocated, Deleted, Stored, Native, etc
        /// </remarks>
        public int State
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return (int)Flags;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs the instance
        /// </summary>
        /// <param name="data"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public LinkedNode(ref T data)
        {
            this.Value = data;
        }

        #endregion

        //Equals() => Native ? Value.Equals(o) : base.Equals(o);

        //GetHashCode() => Native ? Value.GetHashCode() : base.GetHashCode();
    }
}
