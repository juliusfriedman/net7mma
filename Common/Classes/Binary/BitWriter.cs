namespace Media.Common//.Binary
{
    /// <summary>
    /// Allows for writing bits from a <see cref="System.IO.Stream"/> with a variable sized buffer
    /// </summary>
    public class BitWriter : Common.BaseDisposable
    {
        #region Fields

        internal readonly MemorySegment m_ByteCache;

        internal readonly System.IO.Stream m_BaseStream;

        //Depreciate
        internal readonly Binary.ByteOrder m_ByteOrder = Binary.SystemByteOrder;

        int m_ByteIndex = 0, m_BitIndex = 0; 
        
        internal bool m_LeaveOpen;

        #endregion

        #region Properties
        
        //IsAligned

        /// <summary>
        /// Gets or sets a value which indicates if the <see cref="BaseStream"/> should be closed on <see cref="Dispose"/>
        /// </summary>
        public bool LeaveOpen { get { return m_LeaveOpen; } set { m_LeaveOpen = value; } }

        /// <summary>
        /// Gets a value which indicates the amount of bytes which are available to write flushing to the <see cref="BaseStream"/>
        /// </summary>
        public int BytesRemaining { get { return m_ByteCache.Count - m_ByteIndex; } }

        /// <summary>
        /// Gets a value which indicates the amount of bits which remain in the current Byte.
        /// </summary>
        public int BitsRemaining { get { return Common.Binary.BitsPerByte - m_BitIndex; } }

        /// <summary>
        /// Gets the <see cref="System.IO.Stream"/> from which the data is written.
        /// </summary>
        public System.IO.Stream BaseStream { get { return m_BaseStream; } }

        #endregion

        #region Constructor / Destructor

        public BitWriter(System.IO.Stream source, int cacheSize = 32, bool leaveOpen = false)
            : base()
        {
            if (source == null) throw new System.ArgumentNullException("source");

            m_BaseStream = source;

            m_LeaveOpen = leaveOpen;

            m_ByteCache = new MemorySegment(cacheSize);
        }

        ~BitWriter() { Dispose(); }

        #endregion

        #region Methods

        //Align()

        public void Flush()
        {
            int toWrite = m_ByteCache.Count - m_ByteIndex;

            if (m_BitIndex > 0) ++toWrite;

            if (toWrite <= 0) return;

            m_BaseStream.Write(m_ByteCache.Array, m_ByteCache.Offset + m_ByteIndex, toWrite);

            m_ByteIndex = m_BitIndex = 0;
        }

        public void WriteBit(bool value)
        {
            if (m_BitIndex >= Common.Binary.BitsPerByte)
            {
                m_BitIndex = 0;

                ++m_ByteIndex;
            }
         
            //Set the bit and move the bit index
            Binary.ExchangeBit(ref m_ByteCache.Array[m_ByteIndex], m_BitIndex++, value);
        }

        //Write8(reverse)

        //Write16(reverse)

        //Write24(reverse)

        //Write32(reverse)

        //Write64(reverse)

        //WriteNBit(reverse)

        //WriteBigEndian16

        //WriteBigEndian32

        //WriteBigEndian64

        //Should check against m_ByteOrder

        #endregion

        #region Overrides

        public override void Dispose()
        {
            //Write remaining bits
            Flush();

            if (IsDisposed) return;

            base.Dispose();

            m_ByteCache.Dispose();

            if (m_LeaveOpen) return;

            m_BaseStream.Dispose();
        }

        #endregion

    }
}
