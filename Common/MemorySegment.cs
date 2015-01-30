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
        byte[] m_Array;

        int m_Offset, m_Length;

        //public readonly Binary.Endian Endian;

        public int Count { get { return m_Length; } protected set { m_Length = value; } }

        public int Offset { get { return m_Offset; } protected set { m_Offset = value; } }

        public byte[] Array { get { return m_Array; } protected set { m_Array = value; } }

        public MemorySegment(byte[] reference)  : base()
        {
            if (reference == null) throw new ArgumentNullException("reference");
            
            m_Array = reference;
            
            m_Length = m_Array.Length;

            //Endian = Binary.SystemEndian;
        }

        public MemorySegment(byte[] reference, int offset): this(reference)
        {
            m_Offset = offset;

            if (m_Offset > m_Length) throw new ArgumentOutOfRangeException("offset");
        }

        public MemorySegment(byte[] reference, int offset, int length)
            : this(reference, offset)
        {
            m_Length = length;

            if (m_Offset + m_Length > m_Array.Length) throw new ArgumentOutOfRangeException("length");
        }

        public MemorySegment(int size)
        {
            if (size < 0) throw new ArgumentException("size");
            
            m_Array = new byte[size];
            
            m_Offset = 0;
            
            m_Length = size;

            //Endian = Binary.SystemEndian;
        }

        public MemorySegment(MemorySegment other)
        {
            IsDisposed = other.IsDisposed;
            
            if (IsDisposed) return;

            m_Array = other.Array;

            m_Offset = other.Offset;

            m_Length = other.m_Length;

            //Endian = other.Endian;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;

            base.Dispose();

            //m_Array = Utility.Empty;
            //m_Offset = m_Length = 0;

            //Don't remove the reference to the array
            //if (m_Owner) m_Array = null;
            
            //Don't change the offset or length 
            //m_Offset = m_Length = -1;
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            for (int i = 0;  i < m_Length; ++i)
            {
                yield return m_Array[m_Offset + i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<byte>)this).GetEnumerator();
        }

        public byte this[int index]
        {
            get { return m_Array[m_Offset + index]; }
            set { m_Array[m_Offset + index] = value; }
        }

        //To MemoryStream

        //ToArray

        //ICollection.CopyTo
    }

    //Todo Profile using a concrete IEnumerator implementation which can be constructed given another with a start and offset.

    //VirtualEnumerable
}
