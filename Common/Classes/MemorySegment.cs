using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                yield return m_Array[m_Offset + i]; //this[i]
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
            get { return m_Array[m_Offset + index]; }
            set { m_Array[m_Offset + index] = value; }
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

        public static System.ArraySegment<byte> ToByteArraySegment(this MemorySegment segment)
        {
            return new ArraySegment<byte>(segment.Array, segment.Offset, segment.Count);
        }

        public static void CopyTo(this MemorySegment segment, byte[] dest, int destinationIndex, int length)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) return;

            //could check dest and also verify length 

            Array.Copy(segment.Array, segment.Offset, dest, destinationIndex, length);
        }

        //make Left / Right or expect the callers to use -length when they need to...

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

    //SpannedMemorySegment / MemorySegmentList

    //MemorySegmentPointer Array is obtained with unsafe or function...    

    //Enumerable version

    //public EnumerableMemorySegment(IEnumerable<byte> source, int offset, int length)
    //{
    //    //m_Array = source.GetEnumerator()

    //    m_Offset = (uint)offset;

    //    m_Length = (uint)length;
    //}
}
