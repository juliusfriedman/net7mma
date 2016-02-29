namespace Media.Common//.Binary
{
    /// <summary>
    /// Allows for reading bits from a <see cref="System.IO.Stream"/> with a variable sized buffer (should be a streamreader?)
    /// </summary>
    public class BitReader : BaseDisposable
    {
        #region Fields

        internal readonly MemorySegment m_ByteCache;

        internal readonly System.IO.Stream m_BaseStream;

        internal protected int m_ByteIndex = 0, m_BitIndex = 0;

        internal bool m_LeaveOpen;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value which indicates if the <see cref="BaseStream"/> should be closed on <see cref="Dispose"/>
        /// </summary>
        public bool LeaveOpen { get { return m_LeaveOpen; } set { m_LeaveOpen = value; } }

        /// <summary>
        /// Gets a value which indicates the amount of bytes which are available without reading more data from the <see cref="BaseStream"/>
        /// </summary>
        public int BytesRemaining { get { return m_ByteCache.Count - m_ByteIndex; } }

        /// <summary>
        /// Gets a value which indicates the amount of bits which remain in the current Byte.
        /// </summary>
        public int BitsRemaining { get { return Common.Binary.BitsPerByte - m_BitIndex; } }

        /// <summary>
        /// Gets the <see cref="System.IO.Stream"/> from which the data is read.
        /// </summary>
        public System.IO.Stream BaseStream { get { return m_BaseStream; } }

        #endregion

        #region Constructor / Destructor

        public BitReader(System.IO.Stream source, int cacheSize = 32, bool leaveOpen = false)
            : base()
        {
            if (source == null) throw new System.ArgumentNullException("source");

            m_BaseStream = source;

            m_LeaveOpen = leaveOpen;

            m_ByteCache = new MemorySegment(cacheSize);
        }

        ~BitReader() { Dispose(); }

        #endregion

        #region Methods

        public bool Find(byte[] bitPattern, int offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public bool ReverseFind(byte[] bitPattern, int offset, int length)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Reads the given amount of bits into the cache.
        /// </summary>
        /// <param name="countOfBits">The amount of bits to read</param>
        internal void ReadBytesForBits(int countOfBits)
        {
            if (countOfBits <= 0) return;

            int bitsRemain = Common.Binary.BitsPerByte - m_BitIndex;

            if (bitsRemain < countOfBits) return;

            int bytesToRead = Common.Binary.BitsToBytes(countOfBits);

            if (bytesToRead + m_ByteIndex < m_ByteCache.Count) return;

            int bytesRead = 0;

            while (bytesToRead > 0)
            {
                bytesRead = m_BaseStream.Read(m_ByteCache.Array, m_ByteCache.Offset + m_ByteIndex + bytesRead, bytesToRead);

                bytesToRead -= bytesRead;
            }
        }

        /// <summary>
        /// Copies the bits which are left in the cache to the beginning of the cache
        /// </summary>
        internal void Recycle()
        {
            Common.Binary.CopyBitsTo(m_ByteCache.Array, m_ByteIndex, m_BitIndex, m_ByteCache.Array, 0, 0, Common.Binary.BytesToBits(m_ByteCache.Count - m_ByteIndex) + m_BitIndex);

            m_ByteIndex = m_BitIndex = 0;
        }
        
        public bool PeekBit()
        {
            return Common.Binary.GetBit(ref m_ByteCache.Array[m_ByteIndex], m_BitIndex);
        }

        public byte Peek8(bool reverse = false)
        {
            int bitIndex = m_BitIndex, byteIndex = m_ByteIndex;

            return (byte)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 8, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 8, Binary.Ūnus, Binary.BitsPerByte));
        }

        public short Peek16(bool reverse = false)
        {
            int bitIndex = m_BitIndex, byteIndex = m_ByteIndex;

            return (short)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 16, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 16, Binary.Ūnus, Binary.BitsPerByte));
        }

        public int Peek24(bool reverse = false)
        {
            int bitIndex = m_BitIndex, byteIndex = m_ByteIndex;

            return (int)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 24, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 24, Binary.Ūnus, Binary.BitsPerByte));
        }

        public int Peek32(bool reverse = false)
        {
            int bitIndex = m_BitIndex, byteIndex = m_ByteIndex;

            return (int)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 32, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 32, Binary.Ūnus, Binary.BitsPerByte));
        }

        public long Peek64(bool reverse = false)
        {
            int bitIndex = m_BitIndex, byteIndex = m_ByteIndex;

            return (int)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 64, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref byteIndex, ref bitIndex, 64, Binary.Ūnus, Binary.BitsPerByte));
        }
        
        [System.CLSCompliant(false)]
        public ulong PeekBits(int count, bool reverse = false)
        {
            return (ulong)Common.Binary.ReadBinaryInteger(m_ByteCache.Array, 0, count, reverse);
        }

        public bool ReadBit()
        {
            if (m_BitIndex > Common.Binary.BitsPerByte)
            {
                m_BitIndex = 0;

                ++m_ByteIndex;
            }

            return Common.Binary.GetBit(ref m_ByteCache.Array[m_ByteIndex], m_BitIndex++);
        }

        public byte Read8(bool reverse = false)
        {
            return (byte)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 8, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 8, Binary.Ūnus, Binary.BitsPerByte));
        }

        public short Read16(bool reverse = false)
        {
            return (short)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 16, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 16, Binary.Ūnus, Binary.BitsPerByte));
        }

        public int Read24(bool reverse = false)
        {
            return (int)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 24, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 24, Binary.Ūnus, Binary.BitsPerByte));
        }

        public int Read32(bool reverse = false)
        {
            return (int)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 32, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 32, Binary.Ūnus, Binary.BitsPerByte));
        }

        public long Read64(bool reverse = false)
        {
            return (long)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 64, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, 64, Binary.Ūnus, Binary.BitsPerByte));
        }

        [System.CLSCompliant(false)]
        public ulong ReadBits(int count, bool reverse = false)
        {
            return (ulong)(reverse ? Common.Binary.ReadReverseBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, count, Binary.Ūnus, Binary.BitsPerByte) : Common.Binary.ReadBinaryInteger(m_ByteCache.Array, ref m_ByteIndex, ref m_BitIndex, count, Binary.Ūnus, Binary.BitsPerByte));
        }

        public void ReadBits(int count, byte[] dest, int destByteOffset, int destBitOffset)
        {
            //Should accept dest and offsets for direct reads?
            //Would pass m_ByteIndex and m_BitIndex for normal cases.
            ReadBytesForBits(count);

            Common.Binary.CopyBitsTo(m_ByteCache.Array, m_ByteIndex, m_BitIndex, dest, destByteOffset, destBitOffset, count);
        }

        //ReadBigEndian16

        //ReadBigEndian32

        //ReadBigEndian64

        #endregion

        #region Overrides

        public override void Dispose()
        {
            if (IsDisposed || false == ShouldDispose) return;

            base.Dispose();

            m_ByteCache.Dispose();
            
            if (m_LeaveOpen) return;

            m_BaseStream.Dispose();
        }

        #endregion
    }
}

