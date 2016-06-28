using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Common
{
    public class BitReader
    {
        protected int deficit;
        protected int curInt;
        private int initPos;

        private System.IO.MemoryStream bb;

        public BitReader(System.IO.MemoryStream iss)
        {
            // TODO: Complete member initialization
            this.bb = iss;
        }

        private BitReader(BitReader other)
        {
            bb = other.bb.duplicate();
            curInt = other.curInt;
            deficit = other.deficit;
        }

        public BitReader fork()
        {
            return new BitReader(this);
        }

        public void terminate()
        {
            int putBack = (32 - deficit) >> 3;
            bb.Position = bb.Position - putBack;
        }

        public int position()
        {
            return (int)((bb.Position - initPos - 4) << 3) + deficit;
        }

        /**
         * Stops this bit reader. Returns underlying ByteBuffer pointer to the next
         * byte unread byte
         */
        public void stop()
        {
            bb.Position = bb.Position - ((32 - deficit) >> 3);
        }

        public int checkAllBits()
        {
            return curInt;
        }
        public int readInt()
        {
            if (bb.remaining() >= 4)
            {
                deficit -= 32;
                return ((bb.get() & 0xff) << 24) | ((bb.get() & 0xff) << 16) | ((bb.get() & 0xff) << 8) | (bb.get() & 0xff);
            }
            else
                return readIntSafe();
        }

        private int readIntSafe()
        {
            deficit -= (bb.remaining() << 3);
            int res = 0;
            if (bb.hasRemaining())
                res |= bb.get() & 0xff;
            res <<= 8;
            if (bb.hasRemaining())
                res |= bb.get() & 0xff;
            res <<= 8;
            if (bb.hasRemaining())
                res |= bb.get() & 0xff;
            res <<= 8;
            if (bb.hasRemaining())
                res |= bb.get() & 0xff;
            return res;
        }

        public int read1Bit()
        {

            int ret = curInt >> 31;
            curInt <<= 1;
            ++deficit;
            if (deficit == 32)
            {
                curInt = readInt();
            }
            // System.out.println(ret);

            return ret;
        }

        public bool moreData()
        {
            int remaining = bb.remaining() + 4 - ((deficit + 7) >> 3);
            return remaining > 1 || (remaining == 1 && curInt != 0);
        }

        public int readNBit(int n)
        {
            if (n > 32)
                throw new Exception("Can not read more then 32 bit");

            int nn = n;

            int ret = 0;
            if (n + deficit > 31)
            {
                ret |= (curInt >> deficit);
                n -= 32 - deficit;
                ret <<= n;
                deficit = 32;
                curInt = readInt();
            }

            if (n != 0)
            {
                ret |= curInt >> (32 - n);
                curInt <<= n;
                deficit += n;
            }

            // for(--nn; nn >=0; nn--)
            // System.out.print((ret >> nn) & 1);
            // System.out.println();

            return ret;
        }

        public int remaining()
        {
            return (bb.remaining() << 3) + 32 - deficit;
        }

        public bool isByteAligned()
        {
            return (deficit & 0x7) == 0;
        }

        public int skip(int bits)
        {
            int left = bits;

            if (left + deficit > 31)
            {
                left -= 32 - deficit;
                deficit = 32;
                if (left > 31)
                {
                    int skip = Math.Min(left >> 3, bb.remaining());
                    bb.Position = bb.Position + skip;
                    left -= skip << 3;
                }
                curInt = readInt();
            }

            deficit += left;
            curInt <<= left;

            return bits;
        }

        public int skipFast(int bits)
        {
            deficit += bits;
            curInt <<= bits;

            return bits;
        }

        public int align()
        {
            return (deficit & 0x7) > 0 ? skip(8 - (deficit & 0x7)) : 0;
        }

        public int check24Bits()
        {
            if (deficit > 16)
            {
                deficit -= 16;
                curInt |= nextIgnore16() << deficit;
            }

            if (deficit > 8)
            {
                deficit -= 8;
                curInt |= nextIgnore() << deficit;
            }

            return curInt >> 8;
        }

        public int check16Bits()
        {
            if (deficit > 16)
            {
                deficit -= 16;
                curInt |= nextIgnore16() << deficit;
            }
            return curInt >> 16;
        }

        public int readFast16(int n)
        {
            if (n == 0)
                return 0;
            if (deficit > 16)
            {
                deficit -= 16;
                curInt |= nextIgnore16() << deficit;
            }

            int ret = curInt >> (32 - n);
            deficit += n;
            curInt <<= n;

            return ret;
        }

        public int checkNBit(int n)
        {
            if (n > 24)
                throw new Exception("Can not check more then 24 bit");

            while (deficit + n > 32)
            {
                deficit -= 8;
                curInt |= nextIgnore() << deficit;
            }
            int res = curInt >> (32 - n);
            // for (int i = n - 1; i >= 0; i--) {
            // System.out.print((res >> i) & 0x1);
            // }
            // System.out.println();
            return res;
        }

        private int nextIgnore16()
        {
            return bb.remaining() > 1 ? bb.getShort() & 0xffff : (bb.hasRemaining() ? ((bb.get() & 0xff) << 8) : 0);
        }

        private int nextIgnore()
        {
            return bb.hasRemaining() ? bb.get() & 0xff : 0;
        }

        public int curBit()
        {
            return deficit & 0x7;
        }
    }
}
