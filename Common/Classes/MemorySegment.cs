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
#if UNSAFE
            get { unsafe { fixed (byte* p = &m_Array[m_Offset]) return *(p + index); } }
            set { unsafe { fixed (byte* p = &m_Array[m_Offset]) *(p + index) = value; } }
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

    //    public AlignedSegment(byte one)
    //    {
    //        this[0] = one;
    //    }
    //    public AlignedSegment(byte one, byte two)
    //        :this(one)
    //    {
    //        this[1] = two;
    //    }
    //    public AlignedSegment(byte one, byte two,  byte three)
    //        : this(one, two)
    //    {
    //        this[2] = three;
    //    }
    //    public AlignedSegment(byte one, byte two, byte three, byte fourc)
    //        : this(one, two, three)
    //    {
    //        this[3] = fourc;
    //    }
    //}

    #endregion

    #region Not yet used

    ////Deciding if I really need this...
    ///// <summary>
    ///// Used to crete a continious stream to locations of memory which may not be next to each other and could even overlap.
    ///// </summary>
    //public class SegmentStream : System.IO.Stream
    //{
    //    //Absolutely not necessary...
    //    ulong m_Position, m_Count;

    //    readonly List<Common.MemorySegment> Segments = new List<MemorySegment>();

    //    public void AddMemory(Common.MemorySegment segment)
    //    {
    //        Segments.Add(segment);

    //        m_Count += (ulong)segment.Count;
    //    }

    //    public void AddPersistedMemory(Common.MemorySegment segment)
    //    {
    //        Common.MemorySegment copy = new MemorySegment(segment.Count);

    //        System.Array.Copy(segment.Array, segment.Offset, copy.Array, 0, segment.Count);

    //        AddMemory(copy);
    //    }

    //    public void InsertMemory(int index, Common.MemorySegment toInsert)
    //    {
    //        Segments.Insert(index, toInsert);

    //        m_Count += (ulong)toInsert.Count;
    //    }

    //    public void InsertPersistedMemory(int index, Common.MemorySegment toInsert)
    //    {
    //        Common.MemorySegment copy = new MemorySegment(toInsert.Count);

    //        System.Array.Copy(toInsert.Array, toInsert.Offset, copy.Array, 0, toInsert.Count);

    //        InsertMemory(index, copy);
    //    }

    //    public void Free(int index)
    //    {
    //        if (index < 0 || index > Segments.Count) return;

    //        using (Common.MemorySegment segment = Segments[index])
    //        {
    //            Segments.RemoveAt(index);

    //            m_Count -= (ulong)segment.Count;
    //        }
    //    }

    //    public override bool CanRead
    //    {
    //        get { return true; }
    //    }

    //    public override bool CanSeek
    //    {
    //        get { return true; }
    //    }

    //    public override bool CanWrite
    //    {
    //        get { return true; }
    //    }

    //    public override void Flush()
    //    {
    //        return;
    //    }

    //    public override long Length
    //    {
    //        get { return (long)m_Count; }
    //    }

    //    public override long Position
    //    {
    //        get
    //        {
    //            return (long)m_Position;
    //        }
    //        set
    //        {
    //            ulong uvalue = (ulong)value;

    //            if (uvalue > m_Count)
    //            {
    //                m_Position = m_Count;

    //                return;
    //            }

    //            m_Position = uvalue;
    //        }
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //        //Needs to have the segmegment for the offset somewhere
    //    }

    //    public override long Seek(long offset, System.IO.SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //        //need to switch on origin and move m_Position 
    //        //Needs to have the segmegment for the offset somewhere
    //    }

    //    //Could have this clear or dispose memory...
    //    public override void SetLength(long value)
    //    {
    //        if (value <= 0) Clear();

    //        //Clear only up after value and resegment first segment offsets based on value if required.
    //    }

    //    /// <summary>
    //    /// Calls <see cref="Free"/> for each entry in <see cref="Segments"/>
    //    /// </summary>
    //    private void Clear()
    //    {
    //        for (int i = 0, e = Segments.Count; i < e; ++i) Free(i);
    //    }

    //    public void WritePersisted(byte[] buffer, int offset, int count)
    //    {
    //        AddPersistedMemory(new Common.MemorySegment(buffer, offset, count));
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        AddMemory(new Common.MemorySegment(buffer, offset, count));
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
