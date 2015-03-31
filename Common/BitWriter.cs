using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common//.Binary
{
    /// <summary>
    /// Allows for writing bits from a <see cref="System.IO.Stream"/> with a variable sized buffer
    /// </summary>
    public class BitWriter : Common.BaseDisposable
    {
        #region Fields

        MemorySegment m_Cache = new MemorySegment(32);

        System.IO.Stream m_Source;

        int m_ByteIndex = 0, m_BitIndex = 0; long m_StreamPosition, m_StreamLength;

        Binary.ByteOrder m_ByteOrder = Binary.SystemByteOrder;

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

        public BitWriter(System.IO.Stream source, int cacheSize = 32, bool leaveOpen = false)
            : base()
        {
            if (source == null) throw new ArgumentNullException("source");

            m_Source = source;

            m_StreamPosition = m_Source.Position;

            m_StreamLength = m_Source.Length;

            m_LeaveOpen = leaveOpen;

            m_Cache = new MemorySegment(cacheSize);
        }

        ~BitWriter() { Dispose(); }

        #endregion

        #region Methods

        public long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (IsDisposed || m_Source == null || !m_Source.CanSeek) return -1;
            return m_StreamPosition = m_Source.Seek(offset, origin);
        }

        public void Flush()
        {
            if (m_BitIndex > 0)
            {
                //Handle by writing NBit Common.BitSize - m_BitIndex;
            }

            int toWrite = m_Cache.Count - m_ByteIndex;

            if (toWrite <= 0) return;

            m_Source.Write(m_Cache.Array, m_Cache.Offset + m_ByteIndex, toWrite);

            m_StreamPosition += toWrite;

            m_BitIndex = m_BitIndex = 0;
        }

        public void WriteBit(bool value)
        {

            if (m_BitIndex >= Common.Binary.BitsPerByte)
            {
                m_BitIndex = 0;

                ++m_ByteIndex;
            }

            Binary.SetBit(m_Cache.Array, m_BitIndex, value);

            //Move the bit index
            ++m_BitIndex;
        }

        //Write8

        //Write16

        //Write24

        //Write32

        //Write64

        //WriteNBit

        public void WriteEndian(byte[] data, Common.Binary.ByteOrder byteOrder)
        {
            m_Source.Write(Common.Binary.ConvertFromBigEndian(data, byteOrder), 0, data.Length);
        }
        public void WriteEndian(int data, Common.Binary.ByteOrder byteOrder)
        {
            byte[] intBytes = BitConverter.GetBytes(data);

            if (m_ByteOrder == Binary.ByteOrder.Little)
            {
                Array.Reverse(intBytes);
            }

            m_Source.Write(Common.Binary.ConvertFromBigEndian(intBytes, byteOrder), 0, Common.Binary.BytesPerInteger);
        }
        public void WriteEndian(Int64 source, Common.Binary.ByteOrder byteOrder)
        {
            byte[] intBytes = BitConverter.GetBytes(source);

            if (m_ByteOrder == Binary.ByteOrder.Little)
            {
                Array.Reverse(intBytes);
            }

            m_Source.Write(Common.Binary.ConvertFromBigEndian(intBytes, byteOrder), 0, Common.Binary.BytesPerLong);
        }
        public void WriteEndian(string source, Common.Binary.ByteOrder byteOrder)
        {
            m_Source.Write(Common.Binary.ConvertFromBigEndian(Encoding.UTF8.GetBytes(source), byteOrder), 0, source.Length);
        }

        #endregion

        #region Overrides

        public override void Dispose()
        {
            //Write remaining bits
            Flush();

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
