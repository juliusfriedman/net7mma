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

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Media.Common
{
    /// <summary>
    /// Provides a reference to an array of byte with an optional offset.
    /// </summary>
    public class MemorySegment : BaseDisposable, IEnumerable<byte>
    {
        public static readonly byte[] EmptyBytes = new byte[0];

        //Length can be set by other classes through reflection.
        public static readonly MemorySegment Empty = new MemorySegment(EmptyBytes, false);

        internal protected readonly byte[] m_Array;

        internal protected long m_Offset, m_Length;

        //public readonly Binary.ByteOrder ByteOrder;

        //internal protected uint Flags;

        //IReadOnly

        public int Count { get { return (int)m_Length; } protected set { m_Length = value; } }

        public long LongLength { get { return m_Length; } protected set { m_Length = value; } }

        public int Offset { get { return (int)m_Offset; } protected set { m_Offset = value; } }

        public byte[] Array { get { return m_Array; } /* protected set { m_Array = value; } */ }

        public MemorySegment(byte[] reference, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (reference == null) throw new ArgumentNullException("reference");
            
            m_Array = reference;

            m_Length = m_Array.LongLength;

            //ByteOrder = Binary.SystemEndian;
        }

        public MemorySegment(byte[] reference, int offset, bool shouldDispose = true)
            : this(reference, shouldDispose)
        {
            m_Offset = (uint)offset;

            if (m_Offset > m_Length) throw new ArgumentOutOfRangeException("offset");
        }

        public MemorySegment(byte[] reference, int offset, int length, bool shouldDispose = true)
            : this(reference, offset, shouldDispose)
        {
            m_Length = length;

            if (m_Offset + m_Length > m_Array.LongLength) throw new ArgumentOutOfRangeException("length");
        }
        
        public MemorySegment(long size, bool shouldDispose = true)
        {
            if (size < 0) throw new ArgumentException("size");

            m_Array = new byte[size];

            m_Offset = 0;

            m_Length = size;

            ShouldDispose = shouldDispose;

            //ByteOrder = Binary.SystemEndian;
        }
        
        public MemorySegment(MemorySegment other)
        {
            IsDisposed = other.IsDisposed;
            
            if (IsDisposed) return;

            m_Array = other.Array;

            m_Offset = other.m_Offset;

            m_Length = other.m_Length;

            //ByteOrder = other.ByteOrder;
        }

        //public override void Dispose()
        //{
        //    base.Dispose();

        //    //m_Array = Media.Common.MemorySegment.EmptyBytes;
        //    //m_Offset = m_Length = 0;

        //    //Don't remove the reference to the array
        //    //if (m_Owner) m_Array = null;
            
        //    //Don't change the offset or length 
        //    //m_Offset = m_Length = -1;
        //}

        //Make an Enumerator implementation to help with Skip and Copy?

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            for (int i = 0; i < m_Length; ++i)
            {
#if UNSAFE
                unsafe { *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, m_Offset + i) = value; }
#elif NATIVE
                yield return System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + i));
#else
                yield return m_Array[m_Offset + i]; //this[i]
#endif
            }
        }

        //IEnumerator<byte> GetReverseEnumerator()
        //{
        //    for (uint i = m_Length - 1; i <= 0; --i)
        //    {
        //        yield return m_Array[m_Offset + i]; //this[i]
        //    }
        //}

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<byte>)this).GetEnumerator();
        }

        public byte this[int index]
        {
#if UNSAFE
            //Could also use UnsafeAddrOfPinnedArrayElement
            //get { unsafe { return *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, m_Offset + index); } }
            get { unsafe { fixed (byte* p = &m_Array[m_Offset]) return *(p + index); } }
            //set { unsafe { *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, m_Offset + index) = value; } }
            set { unsafe { fixed (byte* p = &m_Array[m_Offset]) *(p + index) = value; } }
#elif NATIVE
            get { return System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + index)); }
            set { System.Runtime.InteropServices.Marshal.WriteByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + index), value); }
#else
            get { return m_Array[m_Offset + index]; }
            set { m_Array[m_Offset + index] = value; }
#endif
        }

        #region Unused 

        //...
        //internal byte[] this[params object[] arg]
        //{
        //    get
        //    {
        //        if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(arg)) return Common.MemorySegment.EmptyBytes;
        //        var a = new ArgIterator((System.RuntimeArgumentHandle)arg[0]);
        //        var tr = a.GetNextArg();
        //        System.TypedReference.SetTypedReference(tr, this);
        //        return m_Array;
        //    }
        //}

        //Raw copy/copyto
        //internal T Get<T> (int offset)
        //{
        //    return default(T);
        //}

        #endregion

        //Methods for copying an array of memory or constructor?

    }

    //Should probably enforce usability with additional derivations, Todo

    //UsableMemorySegment, IUsable

    //ReadOnlyMemorySegment, IReadOnly

    //Should propably be the base class of MemorySegment because it's a lower order concept
    /// <summary>
    /// Extends MemorySegment with the ability to store certain bit offsets
    /// </summary>
    public class BitSegment : MemorySegment
    {
        long m_BitOffset, m_BitCount;

        public int BitCount { get { return (int)m_BitCount; } protected set { m_BitCount = value; } }

        public long LongBitCount { get { return m_BitCount; } protected set { m_BitCount = value; } }

        public int BitOffset { get { return (int)m_BitOffset; } protected set { m_BitOffset = value; } }

        public BitSegment(int bitSize, bool shouldDispose = true) : base(Common.Binary.BitsToBytes(ref bitSize), shouldDispose) { m_BitCount = bitSize; }

        public BitSegment(byte[] reference, int bitOffset, int bitCount, bool shouldDispose = true)
            : base(reference, Common.Binary.BitsToBytes(ref bitOffset), Common.Binary.BitsToBytes(ref bitCount), shouldDispose)
        {
            m_BitOffset = bitOffset;

            m_BitCount = bitCount;
        }

        //reference may be null

        public BitSegment(byte[] reference, int bitOffset, bool shouldDispose = true) : this(reference, bitOffset, Common.Binary.BytesToBits(reference.Length) - bitOffset, shouldDispose) { }

        public BitSegment(byte[] reference) : this(reference, 0, Common.Binary.BytesToBits(reference.Length)) { }


        //Would have to implement a Copy virtual method to ensure that bitOffsets were not accidentally copied using Array.Copy

    }

    /// <summary>
    /// Provides useful extension methods for the <see cref="MemorySegment"/> class
    /// </summary>
    public static class MemorySegmentExtensions
    {
        //public static System.IO.MemoryStream ToMemoryStream() readable, writeable, publicablyVisible...

        public static byte[] ToArray(this MemorySegment segment)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) return null;

            if (segment.Count == 0) return MemorySegment.EmptyBytes;

            byte[] copy = new byte[segment.LongLength];

            CopyTo(segment, copy, 0, segment.Count);

            //Copy the rest
            if (segment.LongLength > segment.Count) Array.Copy(segment.Array, segment.Offset + segment.Count, copy, segment.Count, segment.LongLength - segment.Count);

            return copy;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.ArraySegment<byte> ToByteArraySegment(this MemorySegment segment)
        {
            return new ArraySegment<byte>(segment.Array, segment.Offset, segment.Count);
        }
        
        /// <summary>
        /// Copies all bytes from the segment to dest
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="dest"></param>
        /// <param name="destinationIndex">The offset in <paramref name="dest"/> to start copying</param>
        public static void CopyTo(this MemorySegment segment, byte[] dest, int destinationIndex)
        {
            CopyTo(segment, dest, destinationIndex, segment.Count);
        }

        /// <summary>
        /// Copies bytes from the segment to the dest
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="dest"></param>
        /// <param name="destinationIndex">The offset in <paramref name="dest"/> to start copying</param>
        /// <param name="length">The amount of bytes to copy from <paramref name="segment"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(this MemorySegment segment, byte[] dest, int destinationIndex, int length)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) return;

            //could check dest and also verify length 

            Array.Copy(segment.Array, segment.Offset, dest, destinationIndex, length);
        }

        //make Left / Right or expect the callers to use -length when they need to...
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static MemorySegment Subset(this MemorySegment segment, int offset, int length, bool shouldDispose = true)
        {
            //Should propably enforce that offset and length do not supercede existing length or this is not a true subset.
            return new MemorySegment(segment.Array, offset, length, shouldDispose);
        }

        public static int Find(byte[] source, int start, int count, MemorySegment first, params MemorySegment[] segments)
        {
            int found = 0;

            int needed = count;

            first = null;

            foreach (var segment in segments)
            {
                //Search for the partial match in the segment
                found = Utility.ContainsBytes(segment.Array, ref start, ref count, source, start, needed);

                //If it was found
                if (found >= 0)
                {
                    //If not already set then set it
                    if(first == null) first = segment;

                    //Subtract from needed and if 0 remains break
                    if ((needed -= found) == 0) break;

                    //Continue
                    continue;
                }
                
                //Reset the count, the match needs to be found in order.
                needed = count;

                //Reset to no first segment
                first = null;
            }

            //return the index or the last partial match.
            return found;
        }
    }

    #region Musing

    //The AlignedByteSegment could store it's values in the m_Offset, keep m_Array null and use a special m_Count or not
    //Coulbe be IntPtr for aligned access also.... but that would be super abusive and non intuitive...
    //The problem would be that unsafe access would be required and it would look ugly, it could look slightly nicer using int or long as shown.

    //public class AlignedSegment
    //{
    //    int Member = 0;

    //    public int Count { get; protected set; }

    //    public unsafe byte this[int index]
    //    {
    //        get { fixed (int* x = &Member) return *(((byte*)x) + index); }
    //        set { fixed (int* x = &Member) *(((byte*)x) + index) = value; }
    //    }

    //These copy by value, e.g. dereference the ref to to the copy.
    //There would have to be a setter for byte* to use this with single bytes...
    //The meaning would not be the same as you would think if the value of one changes after this call.
    //To achive that a byte* version would be need and it would have to be fixed..
    //Fixing is bad enough and to fix a single byte is even worse...

    //    public AlignedSegment(ref byte one)
    //    {
    //        this[0] = one;
    //    }
    //    public AlignedSegment(ref byte one, byte two)
    //        :this(one)
    //    {
    //        this[1] = two;
    //    }
    //    public AlignedSegment(ref byte one, ref byte two,  ref byte three)
    //        : this(one, two)
    //    {
    //        this[2] = three;
    //    }
    //    public AlignedSegment(ref byte one, ref byte two, ref byte three, ref byte fourc)
    //        : this(one, two, three)
    //    {
    //        this[3] = fourc;
    //    }
    //}

    #endregion

    #region Concepts...

    ///////////////////////////////////
    //SpannedMemorySegment / MemorySegmentList

    //MemorySegmentPointer Array is obtained with unsafe or function...    

    //Enumerable version

    //public EnumerableMemorySegment(IEnumerable<byte> source, int offset, int length)
    //{
    //    //m_Array = source.GetEnumerator()

    //    m_Offset = (uint)offset;

    //    m_Length = (uint)length;
    //}

    #endregion
}


namespace Media.UnitTests
{

    public class MemorySegmentTests
    {
        //Todo
    }
}