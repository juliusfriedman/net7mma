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

#region Using Statements

using Media.Common.Extensions.Generic.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;//ILookup

#endregion

namespace Media.Common.Collections.Generic
{
    /// <summary>
    /// Provides an implementation of LinkedList in which entries are only removed at the Head or Tail.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedQueue<T>
    {
        #region Fields

        /// <summary>
        /// The LinkedList which the Queue utilizes
        /// </summary>
        readonly System.Collections.Generic.LinkedList<T> LinkedList = new LinkedList<T>();

        /// <summary>
        /// Cache of first and last nodes.
        /// </summary>
        LinkedListNode<T> First, Last;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates how many elements are contained
        /// </summary>
        public int Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return LinkedList.Count; }
        }
        
        /// <summary>
        /// Indicates if no elements are contained.
        /// </summary>
        public bool IsEmpty
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return LinkedList.Count == 0; }
        }

        #endregion


        #region Constrcutor

        /// <summary>
        /// Constructs a LinkedQueue
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        
        public ConcurrentLinkedQueue()
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Try to Dequeue an element
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>Space Complexity S(1), Time Complexity O(2)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]        
        public bool TryDequeue(ref T t)
        {
            if (Count == 0) return false;
            else if (Last == null && First != null) Last = First.Next;

            if (Last == null) Last = Last.Previous;

            t = Last.Value;

            if (Last.List != null)
            {
                LinkedListNode<T> last = Last;

                Last = last.Previous;

                LinkedList.Remove(last);
            }
            else
            {
                do Last = First.Next;
                while (Last.Next != null);
            }

            return true;
        }

        /// <summary>
        /// Try to peek at the first element
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>Space Complexity S(1), Time Complexity O(1)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(ref T t)
        {
            if (Count == 0) return false;

            t = Last.Value;

            return true;
        }

        /// <summary>
        /// Enqueue an element
        /// </summary>
        /// <param name="t"></param>
        public void Enqueue(T t)
        {
            Enqueue(ref t);
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        /// <remarks>Space Complexity S(3), Time Complexity O(2) worst cast</remarks>
        public void Enqueue(ref T t)
        {
            //Set the head
            First = LinkedList.AddFirst(t);

            //If there is still one element and the last element is null assign it.
            if(Count > 0 && Last == null) Last = First;
        }

        /// <summary>
        /// Sets First and Last to null and Calls Clear on the LinkedList.
        /// </summary>
        /// <remarks>Space Complexity S(0), Time Complexity O(Count)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Clear(bool all = true)
        {
            First = Last = null;

            if(all) LinkedList.Clear();
        }

        #endregion

    }
}
