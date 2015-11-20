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

        public static readonly MemorySegment Empty = new MemorySegment(EmptyBytes, false);

        protected readonly byte[] m_Array;

        protected long m_Offset, m_Length;

        //public readonly Binary.ByteOrder ByteOrder;

        public virtual int Count { get { return (int)m_Length; } protected set { m_Length = value; } }

        public virtual long LongLength { get { return m_Length; } protected set { m_Length = value; } }

        public virtual int Offset { get { return (int)m_Offset; } protected set { m_Offset = value; } }

        public virtual byte[] Array { get { return m_Array; } /* protected set { m_Array = value; } */ }

        public MemorySegment(byte[] reference, bool shouldDispose = true)  : base()
        {
            if (reference == null) throw new ArgumentNullException("reference");
            
            m_Array = reference;

            m_Length = m_Array.LongLength;

            //ByteOrder = Binary.SystemEndian;

            ShouldDispose = shouldDispose;
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

        public override void Dispose()
        {
            base.Dispose();

            //m_Array = Media.Common.MemorySegment.EmptyBytes;
            //m_Offset = m_Length = 0;

            //Don't remove the reference to the array
            //if (m_Owner) m_Array = null;
            
            //Don't change the offset or length 
            //m_Offset = m_Length = -1;
        }

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

        //To MemoryStream

        //ToArray

        //ICollection.CopyTo
    }

    //MemorySegmentPointer Array is obtained with unsafe or function...    

    //Enumerable version

    //public EnumerableMemorySegment(IEnumerable<byte> source, int offset, int length)
    //{
    //    //m_Array = source.GetEnumerator()

    //    m_Offset = (uint)offset;

    //    m_Length = (uint)length;
    //}
}
