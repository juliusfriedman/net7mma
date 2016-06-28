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
    /// The implementation is fast (does not does not take more than 5 - 10 operations for any call) and can push and pop from multiple threads with only minimal cache and branch misprediction. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedStackSlim<T> : IEnumerable<T>
    {
        //Todo, Consolidate with LinkedList

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

        #region Fields

        #region Unused [For comparison]

        //Using AddLast / AddFirst
        //readonly System.Collections.Generic.LinkedList<T> LinkedList = new LinkedList<T>();

        //Using TryEnqueue / TryDequeue
        //readonly System.Collections.Concurrent.ConcurrentStack<T> ConcurrentQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        #endregion

        /// <summary>
        /// Cache of last node as it is added to the list.
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

        #endregion

        #region Constrcutor

        /// <summary>
        /// Constructs a LinkedStack
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public ConcurrentLinkedStackSlim()
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Try to Pop an element
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>Space Complexity S(5), Time Complexity O(2-7)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T t)
        {
            //Keep the read node on the stack
            Node onStack;

            //VolatileRead, Compare
            if (true.Equals(Count <= Common.Binary.LongZero))
            {
                //Store
                t = default(T);

                //Return
                return false;
            }

            //Load First or Last
            onStack = First ?? Last;

            //Load, Value
            t = onStack.Value;

            //Exchange @ First => onStack.Next
            System.Threading.Interlocked.Exchange<Node>(ref First, onStack.Next);

            //Decrement (1) @ Count
            System.Threading.Interlocked.Decrement(ref m_Count);

            //Return true
            return true;
        }

        /// <summary>
        /// Try to peek at the <see cref="First"/> element
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
        /// Push an element by repeatedly calling <see cref="TryPush"/> until the operation succeeds.
        /// </summary>
        /// <param name="t"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Push(T t)
        {
            bool added = false;

            do added = TryPush(ref t);
            while (false.Equals(added));
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        /// <remarks>Space Complexity S(2), Time Complexity O(3)</remarks>
        public bool TryPush(ref T t)
        {
            //Compare, (Store, Allocate, Store) or (Store, Store, Allocate)

            if (Object.ReferenceEquals(Last, Node.Null)) First = Last = new Node(ref t);
            else First = new Node(ref t)
            {
                Next = First
            };

            //Increment (1) @ Count
            System.Threading.Interlocked.Increment(ref m_Count);

            return true;
        }

        /// <summary>
        /// Sets First and Last to null and Count = 0.
        /// </summary>
        /// <remarks>Space Complexity S(6), Time Complexity O(6 - Count)</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Clear(bool all, out Node head, out Node tail)
        {
            if (false.Equals(all))
            {
                head = First;

                tail = Last;

                Last = Node.Null;
            }
            else
            {
                head = System.Threading.Interlocked.Exchange(ref First, Node.Null);

                tail = System.Threading.Interlocked.Exchange(ref Last, Node.Null);
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> GetEnumerator()
        {
            Node Current = Last;

            while (false.Equals(Object.ReferenceEquals(Current, Node.Null)))
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
    internal class ConcurrentLinkedStackSlimTests
    {
        readonly Media.Common.Collections.Generic.ConcurrentLinkedStackSlim<long> LinkedStack = new Common.Collections.Generic.ConcurrentLinkedStackSlim<long>();

        long LastInputOutput = 0;

        int Amount = 100;

        int ThreadCount = Environment.ProcessorCount;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestsPush()
        {
            if (LinkedStack.IsEmpty != true) throw new System.Exception("IsEmpty Not True");

            if (LinkedStack.Count != 0) throw new System.Exception("Count Not 0");

            LinkedStack.Push(LastInputOutput++);

            if (LinkedStack.IsEmpty != false) throw new System.Exception("IsEmpty Not False");

            if (LinkedStack.Count != 1) throw new System.Exception("Count Not 1");

            if (LinkedStack.Last == null) throw new System.Exception("Last is null");

            if (false == LinkedStack.TryPush(ref LastInputOutput)) throw new System.Exception("TryEnqueue Not True");

            if (LinkedStack.IsEmpty != false) throw new System.Exception("IsEmpty Not False");

            if (LinkedStack.Count != 2) throw new System.Exception("Count Not 2");

            if (LinkedStack.Last == null) throw new System.Exception("Last is null");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestsPop()
        {
            if (LinkedStack == null) throw new System.Exception("LinkedStack is null");

            if (LinkedStack.IsEmpty) throw new System.Exception("LinkedStack IsEmpty");

            if (LinkedStack.Count != 2) throw new System.Exception("LinkedStack Count Not 2");

            if (false == LinkedStack.TryPop(out LastInputOutput)) throw new System.Exception("TryPop Not True");

            if (LinkedStack.Last == null) throw new System.Exception("Last is null");

            if (LinkedStack.IsEmpty) throw new System.Exception("LinkedStack IsEmpty");

            if (LinkedStack.Count != 1) throw new System.Exception("LinkedStack Count Not 1");

            if (LastInputOutput != 1) throw new System.Exception("LastInputOutput Not 1");

            if (false == LinkedStack.TryPop(out LastInputOutput)) throw new System.Exception("TryPop Not True");

            //The Last node is always left in place to prevent NRE
            //if (LinkedStack.Last != null) throw new System.Exception("Last is null");

            if (false == LinkedStack.IsEmpty) throw new System.Exception("LinkedStack Not IsEmpty");

            if (LinkedStack.Count != 0) throw new System.Exception("LinkedStack Count Not 0");

            if (LastInputOutput != 0) throw new System.Exception("LastInputOutput Not 0");

            if (LinkedStack.TryPop(out LastInputOutput)) throw new System.Exception("TryPop Not False");

            if (false == LinkedStack.TryPush(ref LastInputOutput)) throw new System.Exception("TryPush Not True");

            if (LinkedStack.Count != 1) throw new System.Exception("LinkedStack Count Not 1");

            if (false == LinkedStack.TryPop(out LastInputOutput)) throw new System.Exception("TryPush Not True");

            if (false == LinkedStack.IsEmpty) throw new System.Exception("LinkedStack Not IsEmpty");

            if (LinkedStack.Count != 0) throw new System.Exception("LinkedStack Count Not 0");
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

                    if (LinkedStack.TryPush(ref LastInputOutput))
                    {
                        System.Console.WriteLine("pushThread: " + LastInputOutput);

                        ++countIn;

                        mre.Set();

                        System.Threading.Thread.Sleep(1);
                    }
                }

                if (LinkedStack.IsEmpty) System.Console.WriteLine("pushThread Empty");

                System.Console.WriteLine("pushThread Exit");

            });

            int countOut = 0;

            //In another thread write
            System.Threading.Thread dequeueThread = new System.Threading.Thread(() =>
            {
                while (countOut < Amount)
                {
                    long dequeue;

                    if (LinkedStack.TryPop(out dequeue))
                    {
                        ++countOut;

                        System.Console.WriteLine("popThread: " + dequeue);

                        mre.Set();

                        System.Threading.Thread.Sleep(2);
                    }
                }

                //if (false == LinkedStack.IsEmpty) throw new System.Exception("popThread");

                System.Console.WriteLine("popThread Exit");

            });

            enqueueThread.Start();

            dequeueThread.Start();

            while (countOut == 0 && countIn == 0) mre.WaitOne(0);

            while (countOut < Amount)
            {
                mre.Reset();

                System.Console.WriteLine("Count: " + LinkedStack.Count + "," + "CountIn: " + countIn + "," + "CountOut: " + countOut);

                new System.Threading.Thread(() =>
                {
                    try
                    {
                        System.Console.WriteLine("Enumerate Count: " + LinkedStack.Count);

                        long peek = 0;

                        if (LinkedStack.TryPeek(ref peek)) System.Console.WriteLine("Enumerate TryPeek: " + peek);

                        if (false == LinkedStack.IsEmpty)
                        {
                            System.Console.WriteLine("Enumerate Last: " + LinkedStack.Last.Value);

                            //Increases test time by 10 and keeps the main thread busy
                            ////foreach (long value in LinkedStack)
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

            if (false == LinkedStack.IsEmpty) throw new System.Exception("IsEmpty");

            System.Console.WriteLine("Count: " + LinkedStack.Count);
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
            System.Threading.Thread[] pushThreads = new System.Threading.Thread[ThreadCount];

            System.Threading.Thread[] popThreads = new System.Threading.Thread[ThreadCount];

            System.Threading.Thread[] enumerateThreads = new System.Threading.Thread[ThreadCount];

            Func<System.Threading.Thread> createEnumerateThread = () =>
            {

                if (enumerateCount >= ThreadCount) enumerateCount = 0;

                return enumerateThreads[enumerateCount] = new System.Threading.Thread(() =>
                {
                    try
                    {
                        long peek = 0;

                        if (LinkedStack.TryPeek(ref peek))
                        {
                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> TryPeek: " + peek);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Count" + LinkedStack.Count);
                        }
                        else if (false == LinkedStack.IsEmpty)
                        {
                            if (LinkedStack.TryPeek(ref peek)) System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> First = " + System.Threading.Thread.VolatileRead(ref LinkedStack.Last.Value));

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Last = " + System.Threading.Thread.VolatileRead(ref LinkedStack.Last.Value));
                        }
                    }
                    catch (Exception)
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
                pushThreads[t] = new System.Threading.Thread(() =>
                {
                    int threadLocalCountIn = 0;

                    while (threadLocalCountIn < MultiThreadAmount)
                    {
                        ++LastInputOutput;

                        if (Common.Binary.IsEven(ref LastInputOutput) && LinkedStack.TryPush(ref LastInputOutput))
                        {
                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " @ TryPush => " + LastInputOutput);

                            ++threadLocalCountIn;

                            System.Threading.Interlocked.Increment(ref statLevelCountIn);

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                        else
                        {
                            LinkedStack.Push(LastInputOutput);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " @ Push => " + LastInputOutput);

                            ++threadLocalCountIn;

                            System.Threading.Interlocked.Increment(ref statLevelCountIn);

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                    }

                    if (LinkedStack.IsEmpty) System.Console.WriteLine("pushThread Empty");

                    System.Console.WriteLine("pushThread Exit");

                })
                {
                    ApartmentState = System.Threading.ApartmentState.MTA,

                    Priority = System.Threading.ThreadPriority.Normal,

                    Name = "pushThreads_" + t
                };

                popThreads[t] = new System.Threading.Thread(() =>
                {
                    int threadLocalCountOut = 0;

                    while (threadLocalCountOut < MultiThreadAmount)
                    {
                        long pop;

                        if (LinkedStack.TryPop(out pop))
                        {
                            ++threadLocalCountOut;

                            System.Threading.Interlocked.Increment(ref stackLevelCountOut);

                            System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + ": " + pop);

                            //if(dequeue <= dequeueLast) throw new System.Exception("Unexpected value");

                            sharedResetEvent.Set();

                            System.Threading.Thread.Yield();
                        }
                    }

                    //if (false == LinkedStack.IsEmpty) throw new System.Exception("dequeueThread");

                    System.Console.WriteLine("popThread Exit");

                })
                {
                    Priority = System.Threading.ThreadPriority.BelowNormal,

                    ApartmentState = System.Threading.ApartmentState.MTA,

                    Name = "popThreads_" + t
                };

                enumerateThreads[t] = createEnumerateThread();
            }

            System.Linq.ParallelEnumerable.ForAll(pushThreads.AsParallel(), t => t.Start());

            System.Linq.ParallelEnumerable.ForAll(popThreads.AsParallel(), t => t.Start());

            while (stackLevelCountOut == 0 && statLevelCountIn == 0) sharedResetEvent.WaitOne(0);

            while (stackLevelCountOut < product)
            {
                sharedResetEvent.Reset();

                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + "=> Count: " + LinkedStack.Count + "," + "CountIn: " + statLevelCountIn + "," + "CountOut: " + stackLevelCountOut);

                (enumerateThreads.FirstOrDefault(t => t.ThreadState == System.Threading.ThreadState.Unstarted) ?? createEnumerateThread()).Start();

                sharedResetEvent.WaitOne(ThreadCount);
            }

            if (statLevelCountIn != stackLevelCountOut) throw new System.Exception("count");

            if (false == LinkedStack.IsEmpty) throw new System.Exception("IsEmpty");

            System.Console.WriteLine("Count: " + LinkedStack.Count);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TestZThreading()
        {
            System.Linq.ParallelEnumerable.ForAll(new Action[] { TestsPush, TestsPop, TestsThreading, TestsMultiThreading }.AsParallel(), (a) =>
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
                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Count = " + LinkedStack.Count);

                System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Last = " + LinkedStack.Last.Value);
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
                    if (true == LinkedStack.IsEmpty && false == (LastInputOutput == ThreadCount * (((10 >> 3) << 7) - 28))) throw new System.Exception("IsEmpty");
                }
                catch
                {
                    System.Console.WriteLine(System.Threading.Thread.CurrentThread.Name + " => Exception");
                }
            }
        }
    }
}