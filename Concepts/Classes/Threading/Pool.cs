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

#region Reference

//https://github.com/dotnet/corefx/blob/e425ca81ce808229346178bff57811159c007fa4/src/System.Buffers/src/System/Buffers/ArrayPool.cs
//https://github.com/dotnet/corefxlab/tree/master/src/System.Buffers.Experimental/System/Buffers
//https://github.com/mattwar/argo/blob/master/src/argo/Utilities/ArrayPool.cs

#endregion

namespace Media.Concepts.Classes.Threading
{
    /// <summary>
    /// Pool implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T>
    {
        internal const Pool<T> NullPool = null;

        //Todo, could probably use ArraySegment to provide indirection to array...
        //https://github.com/KrzysztofCwalina/corefxlab/blob/7060d18047c2fd1e319005ff682b481003cc978c/demos/LowAllocationWebServer/Framework/SharedData.cs

        #region Nested Types

        internal struct Lease : Media.Common.IDisposed
        {
            //static System.Type LeaseType = typeof(Lease);

            //4 bytes
            internal int Id;

            //Could just be id...
            //4 bytes
            internal int Emurality; // Active >= 0

            //@ 8

            //References (4 or more bytes)
            internal Pool<T> Lessor;

            //References (4 or more bytes)
            internal T[] Lessee;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Lease(Pool<T> lessor, int id, int emure, T[] lessee, bool shouldDispose = true)                
            {
                Lessor = lessor;

                Id = id;

                Emurality = emure;

                Lessee = lessee;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                //Check if already disposed.
                if (Lessor == NullPool || Lessee == NullArray || Emurality < 0) return;

                //if the lease was tracked
                if (Lessor.m_Leases.Remove(this))
                {
                    if (Emurality > 0)
                    {
                        //The lease was already active
                    }
                    else
                    {
                        //The lease was not active, put it into the lessors pool at the below water offsets
                        Lessor.m_Pool[Id] = Lessee;

                        //Check if the above water offset was allocated and is of a smaller size, swap them if so the next time the same thread takes a lease it can have the option to use the larger allocation
                        //if(Lessor.m_Pool[-Id] == null)
                    }

                    //Remove the reference
                    Lessor = NullPool;

                    //Remove the reference
                    Lessee = NullArray;

                    //Inactive
                    Emurality = int.MinValue;

                    //Zero the memory
                    //System.Runtime.InteropServices.Marshal.DestroyStructure(Unsafe.AddressOf(ref this), LeaseType);
                }
            }

            bool Common.IDisposed.IsDisposed
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return Lessor == NullPool || Lessee == NullArray || Emurality < 0; }
            }

            bool Common.IDisposed.ShouldDispose
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return Emurality >= 0; }
            }
        }   

        #endregion

        #region Statics

        const double Whole = 100.0;

        internal static readonly System.Type ElementType = typeof(T);

        internal const T[] NullArray = null;

        internal const T[][] NullMultiArray = null;

        internal static readonly T[] EmptyArray = new T[0] { };

        internal static readonly T[][] EmptyPool = new T[0][] { };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void AllocatePool(Pool<T> pool, int size)
        {
            if (size < 0) pool.m_Pool = (T[][])System.Array.CreateInstance(ElementType, new int[] { -size, 0 }, new int[] { size, 0 });
            else pool.m_Pool = new T[size][];

            if(pool.m_DisableNull) for (int i = size - 1; i >= 0; --i)
            {
                pool.m_Pool[i] = EmptyArray;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void ResizePool(Pool<T> pool, int size)
        {
            //int previousSize = pool.m_Pool.Length;

            System.Array.Resize<T[]>(ref pool.m_Pool, size);

            //for (int i = size - 1; i >= previousSize; --i)
            //{
            //    pool.m_Pool[i] = EmptyArray;
            //}

        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static int SortByLength(T[] x, T[] y)
        {
            int a, b;

            Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(x, out a);

            Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(y, out b);

            return a - b;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slots"></param>
        public Pool(int slots, bool disableNull = false, System.Comparison<T[]> sorter = null)
        {
            m_DisableNull = disableNull;

            Sorter = sorter;

            m_Leases = new System.Collections.Generic.HashSet<Lease>();

            if (slots < 0) slots = System.Environment.ProcessorCount << 4;

            AllocatePool(this, slots);
        }

        #endregion

        #region Fields

        //Todo, make a single array implementation the base and then extend for multi dimensional

        /// <summary>
        /// The pool
        /// </summary>
        T[][] m_Pool = NullMultiArray;

        //     <= -1,        >= 0
        //T[][]m_BelowWater, m_AboveWater;

        int m_Capacity = -1;

        bool m_DisableNull;

        readonly System.Collections.Generic.ICollection<Lease> m_Leases;

        #endregion

        #region Properties

        /// <summary>
        /// Probably not such a good idea... at least in this form.
        /// The idea was to show how much memory was available vs what was used based on the leases.
        /// A better approach would be to Take Size over m_Leases.Sum(l=>l.Emure || Lessee.Length)
        /// </summary>
        public double Occupancy
        {
            get
            {
                if(Slots.Equals(0)) return -(Whole * m_Leases.Count);
                else if (m_Leases.Count.Equals(0)) return -(Whole * Slots);
                return (m_Leases.Count / Slots) / Whole;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Capacity
        {
            get
            {
                return m_Capacity;
            }
        }

        public int Slots
        {
            get
            {
                return (m_Pool ?? EmptyPool).Length;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Size
        {
            get
            {
                int result = 0;

                int length;

                for (int i = m_Pool.Length - 1; i >= 0; --i)
                {
                    if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(m_Pool[i], out length)) continue;

                    result += length;
                }

                return result;
            }
        }

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void AllocateLeaf(int index, int size, out T[] result)
        {
            EnsureCapacity(index);

            result = m_Pool[index] = new T[size];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void ReallocateLeaf(ref T[] leaf, int size)
        {
            System.Array.Resize(ref leaf, size);
        }

        public T[] Get(System.Threading.Thread access, int size)
        {
            return Get ((access ?? System.Threading.Thread.CurrentThread).ManagedThreadId, size);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public T[] Get(int id, int size)
        {
            //Todo, allow for certain `magical` offsets
            //if(id < 0)

            //Access the leaf by id
            T[] leaf = m_Pool[id];

            int existingSize;

            //Ensure not null
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(leaf, out existingSize))
            {
                AllocateLeaf(id, size, out leaf);
            }
            else if (existingSize < size) //or smaller than size
            {
                ReallocateLeaf(ref leaf, size);
            }

            //return
            return leaf;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int poolSize)
        {
            int existingSize;

            //Ensure not null
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(m_Pool, out existingSize))
            {
                AllocatePool(this, poolSize);
            }
            else if (existingSize <= poolSize)
            {
                ResizePool(this, poolSize << 1);
            }
            //else if (m_Pool[poolSize] == null)
            //{
            //    m_Pool[poolSize] = EmptyArray;
            //}
        }

        public void EnsureCapacity(int id, int size)
        {
            Get (id, size);
        }

        public void EnsureCapacity(System.Threading.Thread access, int size)
        {
            Get (access, size);
        }

        System.Comparison<T[]> Sorter;

        public void Sort()
        {
            System.Array.Sort<T[]>(m_Pool, Sorter ?? SortByLength);
        }

        public void Empty()
        {
            AllocatePool(this, m_Pool.Length >> 2);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Set(T[] data, int index)
        {
            if (m_DisableNull && null == data) return;

            EnsureCapacity(index);

            m_Pool [index] = data;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public T[] Find(int size)
        {
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(m_Pool)) goto Null;

            T[] result;

            for (int i = m_Pool.Length - 1, e = -1; i >= 0; --i)
            {
                result = m_Pool[i];

                if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(result, out e) || false.Equals(e.Equals(size))) continue;

                return result;
            }

        Null:
            return null;
        }

        public T[] FindOrCreate(int size, System.Threading.Thread access)
        {
            return Find (size) ?? Get (access, size);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public T[] Rent(int size, System.Threading.Thread access)
        {
            int id = (access ?? System.Threading.Thread.CurrentThread).ManagedThreadId;

            //idea was to allow for the lease to initally be seperated from the pool and then be integrated in at a later time depending on emure

            //int emure = 1;

            //if (Occupancy >= Whole)
            //{
            //    if (m_Capacity >= 0 && Slots >= Capacity) return NullArray;

            //    //id <<= Slots;

            //    //emure = -1;
            //}

            //Ensure the capacity of pool with respect to the thread
            EnsureCapacity (id);

            Lease lease = new Lease(this, id, /*emure * */size, Get(id, size));

            //Track the reference
            m_Leases.Add(lease);

            //Return the allocated array
            return lease.Lessee;

            //If Lease is a reference type...

            ////Ensure the allocation takes places
            //using (System.Threading.ThreadLocal<Lease> leaseAllocation = new System.Threading.ThreadLocal<Lease>())
            //{
            //    //A reference to a lease reference
            //    Lease lease;

            //    //Allocate the lease instance while it was not created previously
            //    do leaseAllocation.Value = lease = new Lease(this, id, /*emure * */size, Get(id, size));
            //    while (leaseAllocation.IsValueCreated.Equals(false));

            //    //After the instance is allocated the members must also be allocated
            //    using (System.Threading.ThreadLocal<T[]> arrayAllocation = new System.Threading.ThreadLocal<T[]>())
            //    {
            //        //Create a reference to the array while it was not created previously
            //        do arrayAllocation.Value = lease.Lessee;
            //        while (arrayAllocation.IsValueCreated.Equals(false));

            //        //Track the reference
            //        m_Leases.Add(lease);

            //        //Return the allocated array
            //        return lease.Lessee;
            //    }
            //}
        }

        #endregion
    }
    
    //Todo
    ///// <summary>
    ///// Extends Pool to have only a multidimensional array
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public class ContigiousPool<T> : Pool<T>
    //{
    //    public ContigiousPool(int size, bool disableNull = false, System.Comparison<T[]> sorter = null)
    //        : base(size, disableNull, sorter)
    //    {

    //    }
    //}
}

namespace Media.UnitTests
{
    internal class PoolTests
    {
        /// <summary>
        /// Performs a test that will allocate close to all of the working set for the CLR under default configuration (8 GB) 1,000,000 allocations.
        /// At 50,000 allocations the overhead of the allocated arrays (12b * 50,000(o) = 600,000b) becomes higher than that of the pool itself.
        /// The pool reflects 50% occupancy in such conditions even with lease allocations of 1000b the pool memory is only 192000b
        /// </summary>
        /// <remarks>
        /// Anymore and you will get Fatal Runtime Errors, you used to get OOM but I guess they are removing the obvious in favor of optomizations and domain knowledge.
        /// </remarks>
        public void TestPool()
        {
            Media.Concepts.Classes.Threading.Pool<byte> test = new Media.Concepts.Classes.Threading.Pool<byte>(-1);

            System.Collections.Generic.List<byte[]> allocations = new System.Collections.Generic.List<byte[]>();

            int minimumAllocation = 1000;

            const string Busy = "Busy...";

            System.Threading.WaitCallback workItem = (o) =>
            {
                int size = Utility.Random.Next(minimumAllocation / 100);

                System.Console.WriteLine("Begin Allocation => " + size);

                allocations.Add(test.Rent(size, System.Threading.Thread.CurrentThread));

                System.Console.WriteLine("End Allocation => " + size);
            };

            System.Action notifyAndRead = () =>
            {
                System.Console.WriteLine("Requested Allocation");

                System.Console.WriteLine("Occupancy => " + test.Occupancy);

                System.Console.WriteLine("Size => " + test.Size);

                System.Console.WriteLine("Slots => " + test.Slots);
            };

            while (allocations.Count < minimumAllocation)
            {
                if (System.Threading.ThreadPool.QueueUserWorkItem(workItem)) notifyAndRead();
                else System.Console.WriteLine(Busy);
            }

            minimumAllocation *= 10;

            while (allocations.Count < minimumAllocation)
            {
                if (System.Threading.ThreadPool.QueueUserWorkItem(workItem)) notifyAndRead();
                else System.Console.WriteLine(Busy);
            }

            minimumAllocation *= 10;

            while (allocations.Count < minimumAllocation)
            {
                if (System.Threading.ThreadPool.QueueUserWorkItem(workItem)) notifyAndRead();
                else System.Console.WriteLine(Busy);
            }

            minimumAllocation *= 10;

            while (allocations.Count < minimumAllocation)
            {
                if (System.Threading.ThreadPool.QueueUserWorkItem(workItem)) notifyAndRead();
                else System.Console.WriteLine(Busy);
            }

            System.Console.WriteLine("@" + minimumAllocation);
            System.Console.WriteLine("Occupancy => " + test.Occupancy);
            System.Console.WriteLine("Size => " + test.Size);
            System.Console.WriteLine("Slots => " + test.Slots);
        }
    }
}

