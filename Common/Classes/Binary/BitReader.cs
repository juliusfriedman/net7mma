using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common//.Binary
{
    /// <summary>
    /// Allows for reading bits from a <see cref="System.IO.Stream"/> with a variable sized buffer (should be a streamreader?)
    /// </summary>
    public class BitReader : BaseDisposable
    {
        #region Fields

        MemorySegment m_Cache = new MemorySegment(32);

        System.IO.Stream m_Source;

        int m_ByteIndex = 0, m_BitIndex = 0; long m_StreamPosition, m_StreamLength;

        //ByteOrder?

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

        public bool Find(byte[] bitPattern, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public bool ReverseFind(byte[] bitPattern, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (IsDisposed || m_Source == null || !m_Source.CanSeek) return -1;
            return m_StreamPosition = m_Source.Seek(offset, origin);
        }

        /// <summary>
        /// Reads the given amount of bits into the cache.
        /// </summary>
        /// <param name="count">The amount of bits to read</param>
        internal void ReadBitsInternal(int count)
        {
            if (count <= 0) return;

            if (false == HasMoreData) return;

            int bitsRemain = Common.Binary.BitsPerByte - m_BitIndex;

            if (bitsRemain < count) return;

            int bytesToRead = count <= Common.Binary.BitsPerByte ? 1 : count % 8;

            if (bytesToRead + m_ByteIndex < m_Cache.Count) return;

            m_ByteIndex = 0;

            while (bytesToRead > 0)
            {
                int bytesRead = m_Source.Read(m_Cache.Array, m_Cache.Offset + m_ByteIndex, bytesToRead);

                m_StreamPosition += bytesRead;

                m_StreamLength -= bytesRead;

                bytesToRead -= bytesRead;
            }
        }
        
        //Peeking shouldn't move index...

        public bool PeekBit()
        {
            if (m_BitIndex >= Common.Binary.BitsPerByte)
            {
                m_BitIndex = 0;

                ++m_ByteIndex;
            }

            if (m_ByteIndex >= m_Cache.Count) ReadBitsInternal(m_Cache.Count * Common.Binary.BitsPerByte);

            return Common.Binary.ReadBinaryInteger(m_Cache.Array, m_ByteIndex, m_BitIndex, false) > 0;

        }

        public byte Peek8(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitsPerByte);

            return reverse ? Common.Binary.ReverseU8(m_Cache[m_ByteIndex]) : m_Cache[m_ByteIndex];
        }

        public short Peek16(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.DoubleBitSize);

            return (short)Common.Binary.ReadBinaryInteger(m_Cache.Array, m_ByteIndex, (int)Binary.DoubleBitSize, reverse);
        }

        public int Peek24(bool reverse = false)
        {
            ReadBitsInternal(Binary.TripleBitSize);

            return (int)Common.Binary.ReadBinaryInteger(m_Cache.Array, m_ByteIndex, (int)Binary.TripleBitSize, reverse);
        }

        public int Peek32(bool reverse = false)
        {
            ReadBitsInternal(Binary.QuadrupleBitSize);

            return (int)Common.Binary.ReadBinaryInteger(m_Cache.Array, m_ByteIndex, Binary.QuadrupleBitSize, reverse);
        }

        public long Peek64(bool reverse = false)
        {
            ReadBitsInternal(Binary.OctupleBitSize);

            return (long)Common.Binary.ReadBinaryInteger(m_Cache.Array, m_ByteIndex, Binary.OctupleBitSize, reverse);
        }
        
        [CLSCompliant(false)]
        public ulong PeekBits(int count, bool reverse = false)
        {
            return (ulong)Common.Binary.ReadBinaryInteger(m_Cache.Array, 0, count, reverse);
        }

        public bool ReadBit()
        {
            bool result = PeekBit();

            ++m_BitIndex;

            return result;
        }

        public byte Read8(bool reverse = false)
        {
            byte result = Peek8(reverse);

            ++m_ByteIndex;

            return result;
        }

        public short Read16(bool reverse = false)
        {
            short result = Peek16(reverse);

            m_ByteIndex += 2;

            return result;
        }

        public int Read24(bool reverse = false)
        {
            int result = Peek24(reverse);

            m_ByteIndex += 3;

            return result;
        }

        public int Read32(bool reverse = false)
        {
            int result = Peek32(reverse);

            m_ByteIndex += 4;

            return result;
        }

        public long Read64(bool reverse = false)
        {
            long result = Peek64(reverse);
            
            m_ByteIndex += 8;

            return result;
        }

        [CLSCompliant(false)]
        public ulong ReadBits(int count, bool reverse = false)
        {
            ulong result = PeekBits(count, reverse);

            if (count > Common.Binary.BitsPerByte) m_ByteIndex += count % Common.Binary.BitsPerByte;

            m_BitIndex += count;

            return result;
            
        }

        #endregion

        #region Overrides

        public override void Dispose()
        {
            if (IsDisposed) return;

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

