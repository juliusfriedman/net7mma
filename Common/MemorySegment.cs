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

        public int Count { get { return m_Length; } }

        public int Offset { get { return m_Offset; } }

        public byte[] Array { get { return m_Array; } }

        public MemorySegment(byte[] reference)  : base()
        {
            if (reference == null) throw new ArgumentNullException("reference");
            m_Array = reference;
            m_Length = m_Array.Length;
        }

        public MemorySegment(byte[] reference, int offset): this(reference)
        {
            int arrayLen = m_Array.Length;
            if (offset > arrayLen) throw new ArgumentOutOfRangeException("offset");
            m_Offset = offset;
            m_Length = arrayLen - m_Offset;
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
        }

        internal void SetLength(int length) { m_Length = length; }

        public override void Dispose()
        {
            //if (m_Owner) m_Array = null;
            m_Offset = m_Length = -1;
            base.Dispose();
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return m_Array.Skip(m_Offset).Take(m_Length).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_Array.Skip(m_Offset).Take(m_Length).GetEnumerator();
        }

        public byte this[int index]
        {
            get { return m_Array[m_Offset + index]; }
            set { m_Array[m_Offset + index] = value; }
        }

        //To MemoryStream
    }
}
