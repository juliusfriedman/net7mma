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

namespace Media.Common.Interfaces
{
    /// <summary>
    /// Represents a lower and upper bound
    /// </summary>
    public interface Indexable
    {
        /// <summary>
        /// 
        /// </summary>
        long LowerBound { get; }

        /// <summary>
        /// 
        /// </summary>
        long UpperBound { get; }
    }

    /// <summary>
    /// Represents a <see cref="Indexable"/> <see cref="System.Collections.IList"/>
    /// </summary>
    public interface IndexableList : Indexable, System.Collections.IList
    {

    }

    /// <summary>
    /// Represents a generic <see cref="Indexable"/> instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface Indexable<T> : Indexable
    {
        //indexer...
        //T GetItem(long index)
    }

    /// <summary>
    /// An <see cref="IndexableList{T}"/> <see cref="System.Collections.Generic.List{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of element</typeparam>
    /// <remarks>abstract so the implementation can decide how to utilize the indexers</remarks>
    public abstract class IndexableList<T> : System.Collections.Generic.List<T>, Indexable<T>, Media.Common.Interfaces.IMutable
    {
        /// <summary>
        /// 
        /// </summary>
        bool m_IsReadOnly, m_IsWriteOnly, m_Mutable = true;

        /// <summary>
        /// Bounds.
        /// </summary>
        long m_LowerBound;

        /// <summary>
        /// 
        /// </summary>
        public long UpperBound
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Count; }
        }

        /// <summary>
        /// 
        /// </summary>
        public long LowerBound
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_LowerBound; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_LowerBound = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_IsReadOnly; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { if (m_Mutable && false.Equals(m_IsReadOnly)) m_IsReadOnly = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsWriteOnly
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_IsWriteOnly; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { if (m_Mutable && m_IsReadOnly.Equals(false) && m_IsWriteOnly.Equals(false)) m_IsWriteOnly = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Mutable
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Mutable; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { if (false.Equals(m_IsReadOnly) && m_Mutable) m_Mutable = value; }
        }
    }

    //SpannedIndexableList etc.

    //Todo, should implement a List which maintains index order and item order seperately. 
}
