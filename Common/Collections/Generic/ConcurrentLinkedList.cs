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

//http://referencesource.microsoft.com/#System/compmod/system/collections/generic/linkedlist.cs
//https://github.com/mono/mono/blob/d70777a3332af2d630d24adf620c2e548b92b56a/mcs/class/referencesource/System/compmod/system/collections/generic/linkedlist.cs

namespace Media.Common.Collections.Generic
{
    /// <summary>
    /// An implementation of a <see href="https://en.wikipedia.org/wiki/Linked_list">Linked List</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedList<T> : System.Collections.Generic.IEnumerable<T>
    {
        //Static Circular

        /// <summary>
        /// The head and tail.
        /// </summary>
        LinkedNode<T> Head, Tail;

        /// <summary>
        /// The count of contained nodes
        /// </summary>
        long m_Count = 0;

        /// <summary>
        /// Gets the amount of elements allowed to be contained.
        /// </summary>
        public int Capacity
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set;
        }

        /// <summary>
        /// Indicates how many elements are contained
        /// </summary>
        public long Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return System.Threading.Interlocked.Read(ref m_Count); }
        }

        /// <summary>
        /// Indicates if no elements are contained.
        /// </summary>
        public bool IsEmpty
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Count.Equals(Common.Binary.LongZero); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryAddFirst(ref T data)
        {
            long count = Count;

            if (count >= Capacity) return false;
            else if (count.Equals(Common.Binary.LongZero)) Head = new LinkedNode<T>(ref data);
            else Head.Next = (Head = new LinkedNode<T>(ref data)
            {
                Next = Head
            });

            System.Threading.Interlocked.Increment(ref m_Count);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryAddLast(ref T data)
        {
            long count = Count;

            if (count >= Capacity) return false;
            else if (count.Equals(Common.Binary.LongZero)) Tail = new LinkedNode<T>(ref data);
            else Tail.Next = (Tail = new LinkedNode<T>(ref data)
            {
                Next = Tail
            });

            System.Threading.Interlocked.Increment(ref m_Count);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected LinkedNode<T> AddFirst(ref T value)
        {
            if (object.ReferenceEquals(Head, LinkedNode<T>.Null))
            {
                Head = new LinkedNode<T>(ref value);
            }
            else
            {
                Head = LinkedNode<T>.InsertBefore(Head, ref value);
            }

            System.Threading.Interlocked.Increment(ref m_Count);

            return Head;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected LinkedNode<T> AddLast(ref T value)
        {
            if (object.ReferenceEquals(Head, LinkedNode<T>.Null))
            {
                Tail = new LinkedNode<T>(ref value);
            }
            else
            {
                Tail = LinkedNode<T>.InsertBefore(Head, ref value);
            }

            System.Threading.Interlocked.Increment(ref m_Count);

            return Tail;
        }

        protected LinkedNode<T> AddAfter(LinkedNode<T> node, ref T value)
        {
            LinkedNode<T> result = LinkedNode<T>.AddAfter(node, ref value);

            System.Threading.Interlocked.Increment(ref m_Count);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected LinkedNode<T> InsertBefore(LinkedNode<T> node, ref T value)
        {
            LinkedNode<T> result = LinkedNode<T>.InsertBefore(node, ref value);

            System.Threading.Interlocked.Increment(ref m_Count);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected System.Collections.Generic.IEnumerator<T> EnumeratorImplemenation()
        {
            LinkedNode<T> Current = Head;

            while (false.Equals(object.ReferenceEquals(Current, LinkedNode<T>.Null)))
            {
                yield return Current.Value;

                Current = Current.Next;
            }
        }

        //Clear

        //Add 

        //Insert

        //Remove / RemoveAt

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return EnumeratorImplemenation();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return EnumeratorImplemenation();
        }

        /// <summary>
        /// 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ConcurrentLinkedList()
        {
            //Reserved
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>                
        public ConcurrentLinkedList(int capacity) : this()
        {
            Capacity = capacity;
        }

    }
}

//Tests