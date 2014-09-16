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

        public bool Find(byte[] bitPattern, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (Disposed || m_Source == null || !m_Source.CanSeek) return -1;
            return m_StreamPosition = m_Source.Seek(offset, origin);
        }

        /// <summary>
        /// Reads the given amount of bits into the cache.
        /// </summary>
        /// <param name="count">The amount of bits to read</param>
        internal void ReadBitsInternal(int count)
        {
            if (count <= 0) return;

            if (!HasMoreData) return;

            int bitsRemain = Common.Binary.BitSize - m_BitIndex;

            if (bitsRemain < count) return;

            int bytesToRead = count <= Common.Binary.BitSize ? 1 : count % 8;

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

        public bool PeekBit()
        {
            if (m_BitIndex >= Common.Binary.BitSize)
            {
                m_BitIndex = 0;
                ++m_ByteIndex;
            }

            if (m_ByteIndex >= m_Cache.Count) ReadBitsInternal(m_Cache.Count * Common.Binary.BitSize);

            return m_Cache[m_ByteIndex] << m_BitIndex > 0;
        }

        public byte Peek8(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize);
            return reverse ? Common.Binary.ReverseU8(m_Cache[m_ByteIndex]) : m_Cache[m_ByteIndex];
        }

        public short Peek16(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 2);
            return Common.Binary.Read16(m_Cache.Array, m_ByteIndex, reverse);
        }

        public int Peek24(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 3);
            return Common.Binary.Read24(m_Cache.Array, m_ByteIndex, reverse);
        }

        public int Peek32(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 4);
            return Common.Binary.Read32(m_Cache.Array, m_ByteIndex, reverse);
        }

        public long Peek64(bool reverse = false)
        {
            ReadBitsInternal(Common.Binary.BitSize * 8);
            return Common.Binary.Read64(m_Cache.Array, m_ByteIndex, reverse);
        }

        public ulong PeekBits(int count, bool reverse = false)
        {

            throw new NotImplementedException();

            int oddBits = count % 8;
            
            ulong result = 0;

            do switch (oddBits)
            {
                case 0: return result;
                case 1: return ReadBit() ? result + 1 : result + 0;
                case 8: return result + Read8(reverse);
                case 16: return result + (ulong)Read16(reverse);
                case 24: return result + (ulong)Read24(reverse);
                case 32: return result + (ulong)Read32(reverse);
                case 64: return result + (ulong)Read64(reverse);
                default:
                    {
                        //Some odd amount of bits remain, 2, 3, 4, 5, 6, 7, 15, 17, 18, 19, 20, 21, 22, 23, 25, 26, 27, 28, 29, 30, 31, 33, 34, 35, 36 -> 63

                        if (count > 64)
                        {
                            count -= 64;
                            goto case 64;
                        }

                        if (count % 2 <= 1)
                        {
                            oddBits = count % 2;
                            continue;
                        }
                        else
                        {
                            oddBits = count % 2;
                            //Read OddBits
                        }


                        //result += oddBits;
                        //count -= oddBits;

                        continue;
                    }
            } while (count > 0);

            return result;
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

        public ulong ReadBits(int count, bool reverse = false)
        {
            ulong result = PeekBits(count, reverse);

            if (count > Common.Binary.BitSize) m_ByteIndex += count % Common.Binary.BitSize;

            m_BitIndex += count;

            return result;
            
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
