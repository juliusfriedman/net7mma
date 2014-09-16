using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Allows for reading bits from a <see cref="System.IO.Stream"/> with a variable sized buffer
    /// </summary>
    public class BitReader : BaseDisposable
    {
        #region Fields

        MemorySegment m_Cache = new MemorySegment(32);

        System.IO.Stream m_Source;

        int m_ByteIndex = 0, m_BitIndex = 0; long m_StreamPosition, m_StreamLength;

        bool m_LeaveOpen;

        #endregion

        #region Properties

        public bool HasMoreData { get { return m_StreamPosition < m_StreamLength; } }

        public long Position
        {
            get { return m_StreamPosition; }
            set
            {
                if (m_Source == null || !m_Source.CanSeek) return;
                m_StreamPosition = m_Source.Position = value;
            }
        }

        public long Length { get { return m_StreamLength; } }

        #endregion

        #region Constructor / Destructor

        public BitReader(System.IO.Stream source, int cacheSize = 32, bool leaveOpen = false)
            : base()
        {
            if (source == null) throw new ArgumentNullException("source");

            m_Source = source;

            m_StreamPosition = m_Source.Position;

            m_StreamLength = m_Source.Length;

            m_LeaveOpen = leaveOpen;

            m_Cache = new MemorySegment(cacheSize);
        }
        ~BitReader() { Dispose(); }

        #endregion

        #region Methods

        public long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (Disposed || m_Source == null || !m_Source.CanSeek) return -1;
            return m_StreamPosition = m_Source.Seek(offset, origin);
        }

        internal void ReadBitsInternal(int count)
        {
            if (count <= 0) return;

            if (!HasMoreData) return;

            int bytesToRead = count % 8;

            if (bytesToRead + m_ByteIndex < m_Cache.Count) return;

            //Todo adjust for remaining bits when m_BitIndex < Common.Binary.BitSize

            m_ByteIndex = 0;

            while (bytesToRead > 0)
            {
                int bytesRead = m_Source.Read(m_Cache.Array, m_Cache.Offset + m_ByteIndex, bytesToRead);
                m_StreamPosition += bytesRead;
                m_StreamLength -= bytesRead;
                bytesToRead -= bytesRead;
            }
        }

        public bool ReadBit()
        {
            if (m_BitIndex >= Common.Binary.BitSize)
            {
                m_BitIndex = 0;
                ++m_ByteIndex;
            }

            if (m_ByteIndex >= m_Cache.Count) ReadBitsInternal(m_Cache.Count * Common.Binary.BitSize);

            return m_Cache[m_ByteIndex] << m_BitIndex++ > 0;
        }

        public byte Read8(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize);
            return reverse ? Common.Binary.ReverseU8(m_Cache[++m_ByteIndex]) : m_Cache[++m_ByteIndex];
        }

        public short Read16(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 2);
            return Common.Binary.Read16(m_Cache.Array, m_ByteIndex, reverse);
        }

        public int Read24(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 3);
            return Common.Binary.Read24(m_Cache.Array, m_ByteIndex, reverse);
        }

        public int Read32(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 4);
            return Common.Binary.Read32(m_Cache.Array, m_ByteIndex, reverse);
        }

        public long Read64(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 8);
            return Common.Binary.Read64(m_Cache.Array, m_ByteIndex, reverse);
        }

        #endregion

        #region Overrides

        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            m_StreamLength = m_StreamPosition = -1;

            m_Cache.Dispose();
            m_Cache = null;

            if (m_LeaveOpen) return;

            m_Source.Dispose();
            m_Source = null;
        }

        #endregion
    }
}
