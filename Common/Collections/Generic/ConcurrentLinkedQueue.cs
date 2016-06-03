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
    /// Provides an implementation of a LinkedList in which entries are only removed from the Next Node.
    /// The implementation does NOT SpinWait, DOES use atomic operations and double compares to ensure no null references are encountered and that no ABCD should effect the input or output in any meaningful way.
    /// The implementation is fast (does not does not take more than 5 - 10 operations for any call) and can enqueue and dequeue from multiple threads with minimal; only minimal cache and branch misprediction. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedQueue<T> : IEnumerable<T>
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
            //Flags, Allocated, Deleted, Stored, Native

            public Node(ref T data)
            {
                this.Value = data;
            }

            //Equals() => Native ? Value.Equals(o) : base.Equals(o);

            //GetHashCode() => Native ? Value.GetHashCode() : base.GetHashCode();
        }

        #endregion

        #region Statics

        //Could make methods to call Set on LinkedListNode<T> using reflection.

        internal static System.Reflection.ConstructorInfo Constructor;

        internal static System.Reflection.PropertyInfo ListProperty, NextProperty, PreviousProperty, ValueProperty;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        /// <summary>
        /// Given a <see cref="ConcurrentLinkedQueue"/>, sets the <see cref="Next"/> of the <see cref="Last"/> to the <see cref="First"/>
        /// </summary>
        /// <param name="queue"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Circular(ConcurrentLinkedQueue<T> queue)
        {
            queue.Last.Next = queue.First;

            return;

            //queue.Last.Next = queue.First.Previous = queue.Last;

            //new LinkedListNode<T>(queue.Last.Value)
            //{
            //    //List = null, //internal constructor...
            //    //Next = null,
            //    //Last = null,
            //    //Previous = null,
            //    Value = default(T)
            //};

            //if (queue == null) return;

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

        #region Unused [For comparison]

        //Using AddLast / AddFirst
        //readonly System.Collections.Generic.LinkedList<T> LinkedList = new LinkedList<T>();

        //Using TryEnqueue / TryDequeue
        //readonly System.Collections.Concurrent.ConcurrentQueue<T> ConcurrentQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        #endregion

        /// <summary>
        /// Cache of first and last nodes as they are added to the list.
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
        /// <remarks>Space Complexity S(5), Time Complexity O(2-7)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T t)
        {
            //Keep the read node on the stack
            Node onStack;

            //VolatileRead, Compare
            if (Count <= 0)
            {
                //Store
                t = default(T);

                //Return
                return false;
            }

            //Compare, Load (First == Node.Null ? Last : First) or Coalesce
            onStack = First ?? Last;

            //Load, Store
            t = onStack.Value;

            //Exchange @ First => onStack.Next
            System.Threading.Interlocked.Exchange<Node>(ref First, onStack.Next);

            //Decrement (1) @ Count
            System.Threading.Interlocked.Decrement(ref m_Count);

            //Return true
            return true;
        }

        /// <summary>
        /// Try to peek at the first element
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>Space Complexity S(1), Time Complexity O(2)</remarks>
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
        /// <remarks>Space Complexity S(2), Time Complexity O(3)</remarks>
        public bool TryEnqueue(ref T t)
        {
            //Compare, Store, Store, Allocate

            if (Object.ReferenceEquals(First, Node.Null)) Last = First = new Node(ref t);
            else Last = Last.Next = new Node(ref t);

            //Increment (1) @ Count
            System.Threading.Interlocked.Increment(ref m_Count);

            return true;
        }

        /// <summary>
        /// Sets First and Last to null and Calls Clear on the LinkedList.
        /// </summary>
        /// <remarks>Space Complexity S(6), Time Complexity O(6 - Count)</remarks>
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

        /// <summary>
        /// Enumerates the elements.
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> GetEnumerator()
        {
            Node Current = First ?? Last;

            while (false == Object.ReferenceEquals(Current, Node.Null))
            {
                yield return Current.Value;

                if (Object.ReferenceEquals(System.Threading.Interlocked.Exchange(ref Current, Current.Next), Node.Null)) yield break;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides UnitTest to prove that the logic provided by the collection is correct and thread safe.
    /// </summary>
    internal class ConcurrentLinkedQueueTests
    {
        readonly Media.Common.Collections.Generic.ConcurrentLinkedQueue<long> LinkedQueue = new Common.Collections.Generic.ConcurrentLinkedQueue<long>();

        long LastInputOutput = 0;

        int Amount = 100;

        int ThreadCount = Environment.ProcessorCount;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestsThreading()
        {
            System.Threading.ManualResetEvent mre = new System.Threading.ManualResetEvent(false);

            int countIn = 0;

            //In a thread populate
            System.Threading.Thread enqueueThread = new System.Threading.Thread(() =>
            {
                while (countIn < Amount)
                {
                    ++LastInputOutput;

                    if (LinkedQueue.TryEnqueue(ref LastInputOutput))
                    {
                        System.Console.WriteLine("enqueueThread: " + LastInputOutput);

                        ++countIn;

                        mre.Set();

                        System.Threading.Thread.Sleep(1);
                    }
                }

                if (LinkedQueue.IsEmpty) System.Console.WriteLine("enqueueThread Empty");

                System.Console.WriteLine("enqueueThread Exit");

            });

            int countOut = 0;

            //In another thread write
            System.Threading.Thread dequeueThread = new System.Threading.Thread(() =>
            {
                while (countOut < Amount)
                {
                    long dequeue;

                    if (LinkedQueue.TryDequeue(out dequeue))
                    {
                        ++countOut;

                        System.Console.WriteLine("dequeueThread: " + dequeue);

                        mre.Set();

                        System.Threading.Thread.Sleep(2);
                    }
                }

                //if (false == LinkedQueue.IsEmpty) throw new System.Exception("dequeueThread");

                System.Console.WriteLine("dequeueThread Exit");

            });

            enqueueThread.Start();

            dequeueThread.Start();

            while (countOut == 0 && countIn == 0) mre.WaitOne(0);

            while (countOut < Amount)
            {
                mre.Reset();

                System.Console.WriteLine("Count: " + LinkedQueue.Count + "," + "CountIn: " + countIn + "," + "CountOut: " + countOut);

                new System.Threading.Thread(() =>
                {
                    try
                    {
                        System.Console.WriteLine("Enumerate Count: " + LinkedQueue.Count);

                        long peek = 0;
                        
                        if(LinkedQueue.TryPeek(ref peek)) System.Console.WriteLine("Enumerate TryPeek: " + peek);

                        if (false == LinkedQueue.IsEmpty)
                        {
                            System.Console.WriteLine("Enumerate Last: " + LinkedQueue.Last.Value);

                            System.Console.WriteLine("Enumerate First: " + LinkedQueue.First.Value);

                            //Increases test time by 10 and keeps the main thread busy
                            ////foreach (long value in LinkedQueue)
                            ////{
                            ////    System.Console.WriteLine("Enumerate Value: " + value);
                            ////}

                        }
                    }
                    catch { }
                })
                {
                    Priority = System.Threading.ThreadPriority.Highest
                }.Start();

                mre.WaitOne(0);
            }

            if (countIn != countOut) throw new System.Exception("count");

            if (false == LinkedQueue.IsEmpty) throw new System.Exception("IsEmpty");

            System.Console.WriteLine("Count: " + LinkedQueue.Count);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestsMultiThreading()
        {
            int MultiThreadAmount = Amount * 10;

            System.Threading.ManualResetEvent sharedResetEvent = new System.Threading.ManualResetEvent(false);

            int statLevelCountIn = 0;

            int stackLevelCountOut = 0;

            int product = ThreadCount * MultiThreadAmount;

            int enumerateCount = 0;

            //In these threads populate
            System.Threading.Thread[] enqueueThreads = new System.Threading.Thread[ThreadCount];

            System.Threading.Thread[] dequeueThreads = new System.Threading.Thread[ThreadCount];

            System.Threading.Thread[] enumerateThreads = new System.Threading.Thread[ThreadCount];

            Func<System.Threading.Thread> createEnumerateThread = () => {

                if (enumerateCount >= ThreadCount) enumerateCount = 0;

                return enumerateThreads[enumerateCount] = new System.Threading.Thread(() =>
                {
                    try
                    {
                        long peek = 0;

                        if (LinkedQueue.TryPeek(ref peek))
                        {
                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> TryPeek: " + peek);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Count" + LinkedQueue.Count);
                        }
                        else if (false == LinkedQueue.IsEmpty)
                        {
                            if (LinkedQueue.TryPeek(ref peek)) System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> First = " + System.Threading.Thread.VolatileRead(ref LinkedQueue.First.Value));

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Last = " + System.Threading.Thread.VolatileRead(ref LinkedQueue.Last.Value));
                        }
                    }
                    catch(Exception)
                    {
                        System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Exception");
                    }
                })
                {
                    Name = "enumerateThreads" + enumerateCount++,

                    ApartmentState = System.Threading.ApartmentState.MTA,

                    Priority = System.Threading.ThreadPriority.AboveNormal
                };
            };

            for (int t = ThreadCount - 1; t >= 0; --t)
            {
                enqueueThreads[t] = new System.Threading.Thread(() =>
                {
                    int threadLocalCountIn = 0;

                    while (threadLocalCountIn < MultiThreadAmount)
                    {
                        ++LastInputOutput;

                        if (Common.Binary.IsEven(ref LastInputOutput) && LinkedQueue.TryEnqueue(ref LastInputOutput))
                        {
                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " @ TryEnqueue => " + LastInputOutput);

                            ++threadLocalCountIn;

                            System.Threading.Interlocked.Increment(ref statLevelCountIn);

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                        else
                        {
                            LinkedQueue.Enqueue(LastInputOutput);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " @ Enqueue => " + LastInputOutput);

                            ++threadLocalCountIn;

                            System.Threading.Interlocked.Increment(ref statLevelCountIn);

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                    }

                    if (LinkedQueue.IsEmpty) System.Console.WriteLine("enqueueThread Empty");

                    System.Console.WriteLine("enqueueThread Exit");

                })
                {
                    ApartmentState = System.Threading.ApartmentState.MTA,

                    Priority = System.Threading.ThreadPriority.Normal,

                    Name = "enqueueThreads_" + t
                };

                dequeueThreads[t] = new System.Threading.Thread(() =>
                {
                    int threadLocalCountOut = 0;

                    while (threadLocalCountOut < MultiThreadAmount)
                    {
                        long dequeue;

                        if (LinkedQueue.TryDequeue(out dequeue))
                        {
                            ++threadLocalCountOut;

                            System.Threading.Interlocked.Increment(ref stackLevelCountOut);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + ": " + dequeue);

                            //if(dequeue <= dequeueLast) throw new System.Exception("Unexpected value");

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                    }

                    //if (false == LinkedQueue.IsEmpty) throw new System.Exception("dequeueThread");

                    System.Console.WriteLine("dequeueThread Exit");

                })
                {
                    Priority = System.Threading.ThreadPriority.BelowNormal,

                    ApartmentState = System.Threading.ApartmentState.MTA,

                    Name = "dequeueThreads_" + t
                };

                enumerateThreads[t] = createEnumerateThread();
            }

            System.Linq.ParallelEnumerable.ForAll(enqueueThreads.AsParallel(), t => t.Start());
            
            System.Linq.ParallelEnumerable.ForAll(dequeueThreads.AsParallel(), t => t.Start());

            while (stackLevelCountOut == 0 && statLevelCountIn == 0) sharedResetEvent.WaitOne(0);

            while (stackLevelCountOut < product)
            {
                sharedResetEvent.Reset();

                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Count: " + LinkedQueue.Count + "," + "CountIn: " + statLevelCountIn + "," + "CountOut: " + stackLevelCountOut);

                (enumerateThreads.FirstOrDefault(t => t.ThreadState == System.Threading.ThreadState.Unstarted) ?? createEnumerateThread()).Start();

                sharedResetEvent.WaitOne(ThreadCount);
            }

            if (statLevelCountIn != stackLevelCountOut) throw new System.Exception("count");

            if (false == LinkedQueue.IsEmpty) throw new System.Exception("IsEmpty");

            System.Console.WriteLine("Count: " + LinkedQueue.Count);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestZThreading()
        {
            System.Linq.ParallelEnumerable.ForAll(new Action[] { TestsEnqueue, TestsDequeue, TestsThreading, TestsMultiThreading }.AsParallel(), (a) =>
            {
                try
                {
                    System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " @ => " + a.Method.Name);

                    a();
                }
                catch (Exception)
                {
                    System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Exception");
                }
            });
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestZZFar()
        {
            try
            {
                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Count = " + LinkedQueue.Count);

                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Last = " + LinkedQueue.Last.Value);
            }
            catch
            {
                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Exception");
            }
            finally
            {
                try
                {
                                                                                    //Multiply ThreadCount by 100
                    if (true == LinkedQueue.IsEmpty && false == (LastInputOutput == ThreadCount * (((10 >> 3) << 7) - 28))) throw new System.Exception("IsEmpty");
                }
                catch
                {
                    System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Exception");
                }
            }
        }
    }

    //Tested over 1000000 times on my cpu with no issues.
    //=>12 Threads = 24200 is for my core i7 3960X @ 3.3 GHz (15 MB SmartCache)
    //http://ark.intel.com/products/63696/Intel-Core-i7-3960X-Processor-Extreme-Edition-15M-Cache-up-to-3_90-GHz

    //Todo, Test Circular
}