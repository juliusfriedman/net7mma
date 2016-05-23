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
using System.Linq;

#endregion

namespace Media.Common.Collections.Generic
{
    /// <summary>
    /// Provides an implementation of LinkedList in which entries are only removed from the Next Node.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedQueue<T>
    {
        #region Nested Types

        /// <summary>
        /// A node which has a reference to the next node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal sealed class Node
        {
            internal const Node Null = null;

            public T Value;

            public Node Next;
                     
            //Create and have no value, Deleted, Has Value
            //Flags, Allocated, Deleted, Stored

            public Node(ref T data)
            {
                this.Value = data;
            }
        }

        #endregion

        #region Statics

        //Could make methods to call Set on LinkedListNode<T> using reflection.

        internal static System.Reflection.ConstructorInfo Constructor;

        internal static System.Reflection.PropertyInfo ListProperty, NextProperty, PreviousProperty, ValueProperty;

        static ConcurrentLinkedQueue()
        {
            //implementations may change the name
            //var fields = typeof(LinkedListNode<T>).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            //System.TypedReference tr = __makeref(obj, T);
            //fields[0].SetValueDirect(tr, obj);

            //or the ctor
            var ctors = typeof(LinkedListNode<T>).GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            Constructor = ctors.LastOrDefault();
            //ctors[0].Invoke(new object[] { list, value });

            //Could also use properties but it's a tad slower...
            var props = typeof(LinkedListNode<T>).GetProperties();

            ListProperty = props.Where(p => p.Name == "List").FirstOrDefault();

            NextProperty = props.Where(p => p.Name == "Next").FirstOrDefault();

            PreviousProperty = props.Where(p => p.Name == "Previous").FirstOrDefault();

            ValueProperty = props.Where(p => p.Name == "Value").FirstOrDefault();

            //props[0].SetMethod.Invoke(obj, new object[] { list });
            //props[1].SetMethod.Invoke(obj, new object[] { next });
            //props[2].SetMethod.Invoke(obj, new object[] { prev });
            //props[3].SetMethod.Invoke(obj, new object[] { value });
        }

        public static void Circular(ConcurrentLinkedQueue<T> queue)
        {
            //queue.Last.Next = queue.First.Previous = queue.Last;

            //new LinkedListNode<T>(queue.Last.Value)
            //{
            //    //List = null, //internal constructor...
            //    //Next = null,
            //    //Last = null,
            //    //Previous = null,
            //    Value = default(T)
            //};

            if (queue == null) return;

            ////If the queue was empty
            //if (queue.IsEmpty)
            //{
            //    queue.Last = new LinkedListNode<T>(default(T));

            //    queue.First = new LinkedListNode<T>(default(T));
            //}

            ////First.Previous = queue.Last
            //PreviousProperty.SetValue(queue.First, queue.Last);

            ////Last.Next = queue.First
            //NextProperty.SetValue(queue.Last, queue.First);
        }

        #endregion

        #region Fields

        //Using AddLast / AddFirst
        //readonly System.Collections.Generic.LinkedList<T> LinkedList = new LinkedList<T>();

        //Using TryEnqueue / TryDequeue
        //readonly System.Collections.Concurrent.ConcurrentQueue<T> ConcurrentQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        /// <summary>
        /// Cache of first and last nodes before they are added to the list.
        /// </summary>
        internal Node First, Last;

        /// <summary>
        /// The count of contained nodes
        /// </summary>
        long m_Count = 0;

        //Todo
        //Capacity, ICollection

        #endregion

        #region Properties

        /// <summary>
        /// Indicates how many elements are contained
        /// </summary>
        public long Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return System.Threading.Thread.VolatileRead(ref m_Count); }
        }
        
        /// <summary>
        /// Indicates if no elements are contained.
        /// </summary>
        public bool IsEmpty
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Count.Equals(0); }
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
        /// <remarks>Space Complexity S(4), Time Complexity O(2)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T t)
        {
            //Compare and Decrement
            if (Count <= 0 || Object.ReferenceEquals(First, Node.Null))
            {
                //Store
                t = default(T);

                //Return
                return false;
            }

            //Load And Store
            t = First.Value;

            //Exchange
            System.Threading.Interlocked.Exchange<Node>(ref First, First.Next);

            //Decrement count
            System.Threading.Interlocked.Decrement(ref m_Count);

            //Return
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
            if (Object.ReferenceEquals(First, Node.Null)) return false;

            t = First.Value;

            return true;
        }

        /// <summary>
        /// Enqueue an element
        /// </summary>
        /// <param name="t"></param>
        public void Enqueue(T t) { TryEnqueue(ref t); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        /// <remarks>Space Complexity S(2), Time Complexity O(2)</remarks>
        public bool TryEnqueue(ref T t)
        {
            Node newNode = new Node(ref t);

            if (Object.ReferenceEquals(First, Node.Null)) Last = First = newNode;
            else Last = Last.Next = newNode;

            System.Threading.Interlocked.Increment(ref m_Count);

            return true;
        }

        /// <summary>
        /// Sets First and Last to null and Calls Clear on the LinkedList.
        /// </summary>
        /// <remarks>Space Complexity S(5), Time Complexity O(Count)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Clear(bool all, out Node head, out Node tail)
        {
            if (false == all)
            {
                head = First;

                tail = Last;

                First = Last = Node.Null;
            }
            else
            {
                System.Threading.Interlocked.Exchange(ref First, Last);

                head = tail = System.Threading.Interlocked.Exchange(ref Last, Node.Null);
            }

            System.Threading.Interlocked.Exchange(ref m_Count, 0);
        }

        public void Clear(bool all = true)
        {
            Node First, Last;

            Clear(all, out First, out Last);
        }

        #endregion

    }
}


namespace Media.UnitTests
{
    internal class ConcurrentLinkedQueueTests
    {
        readonly Media.Common.Collections.Generic.ConcurrentLinkedQueue<int> LinkedQueue = new Common.Collections.Generic.ConcurrentLinkedQueue<int>();

        int LastInputOutput = 0;

        public void TestsEnqueue()
        {
            if (LinkedQueue.IsEmpty != true) throw new System.Exception("IsEmpty Not True");

            if (LinkedQueue.Count != 0) throw new System.Exception("Count Not 0");

            LinkedQueue.Enqueue(LastInputOutput++);

            if (LinkedQueue.IsEmpty != false) throw new System.Exception("IsEmpty Not False");

            if (LinkedQueue.Count != 1) throw new System.Exception("Count Not 1");

            if (LinkedQueue.First == null) throw new System.Exception("First is null");

            if (LinkedQueue.Last == null) throw new System.Exception("Last is null");

            if (false == LinkedQueue.TryEnqueue(ref LastInputOutput)) throw new System.Exception("TryEnqueue Not True");

            if (LinkedQueue.IsEmpty != false) throw new System.Exception("IsEmpty Not False");

            if (LinkedQueue.Count != 2) throw new System.Exception("Count Not 2");

            if (LinkedQueue.First == null) throw new System.Exception("First is null");

            if (LinkedQueue.Last == null) throw new System.Exception("Last is null");
        }

        public void TestsDequeue()
        {
            if (LinkedQueue == null) throw new System.Exception("LinkedQueue is null");

            if (LinkedQueue.IsEmpty) throw new System.Exception("LinkedQueue IsEmpty");

            if (LinkedQueue.Count != 2) throw new System.Exception("LinkedQueue Count Not 2");

            if (false == LinkedQueue.TryDequeue(out LastInputOutput)) throw new System.Exception("TryDequeue Not True");

            if (LinkedQueue.First == null) throw new System.Exception("First is null");

            if (LinkedQueue.Last == null) throw new System.Exception("Last is null");

            if (LinkedQueue.IsEmpty) throw new System.Exception("LinkedQueue IsEmpty");

            if (LinkedQueue.Count != 1) throw new System.Exception("LinkedQueue Count Not 1");

            if (LastInputOutput != 0) throw new System.Exception("LastInputOutput Not 0");

            if (false == LinkedQueue.TryDequeue(out LastInputOutput)) throw new System.Exception("TryDequeue Not True");

            if (LinkedQueue.First != null) throw new System.Exception("First is null");

            //The Last node is always left in place to prevent NRE
            //if (LinkedQueue.Last != null) throw new System.Exception("Last is null");

            if (false == LinkedQueue.IsEmpty) throw new System.Exception("LinkedQueue Not IsEmpty");

            if (LinkedQueue.Count != 0) throw new System.Exception("LinkedQueue Count Not 0");

            if (LastInputOutput != 1) throw new System.Exception("LastInputOutput Not 1");

            if (LinkedQueue.TryDequeue(out LastInputOutput)) throw new System.Exception("TryDequeue Not False");

            if (false == LinkedQueue.TryEnqueue(ref LastInputOutput)) throw new System.Exception("TryEnqueue Not True");

            if (LinkedQueue.Count != 1) throw new System.Exception("LinkedQueue Count Not 1");

            if (false == LinkedQueue.TryDequeue(out LastInputOutput)) throw new System.Exception("TryDequeue Not True");

            if (false == LinkedQueue.IsEmpty) throw new System.Exception("LinkedQueue Not IsEmpty");

            if (LinkedQueue.Count != 0) throw new System.Exception("LinkedQueue Count Not 0");
        }

        public void TestsThreading()
        {
            //In a thread populate
            //In another thread read
            //In another thread enumerate
            //In another thread write
        }
    }
}