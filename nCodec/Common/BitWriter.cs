using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Common
{
    public class BitWriter
    {

        private MemoryStream buf;
        private int curInt;
        private int mcurBit;
        private int initPos;

        public BitWriter(MemoryStream buf)
        {
            this.buf = buf;
            initPos = buf.position();
        }

        private BitWriter(MemoryStream os, int curBit, int curInt, int initPos)
        {
            this.buf = os;
            this.mcurBit = curBit;
            this.curInt = curInt;
            this.initPos = initPos;
        }

        public void flush()
        {
            int toWrite = (mcurBit + 7) >> 3;
            for (int i = 0; i < toWrite; i++)
            {
                buf.put((byte)(curInt >> 24));
                curInt <<= 8;
            }
        }

        private void putInt(int i)
        {
            buf.put((byte)(i >> 24));
            buf.put((byte)(i >> 16));
            buf.put((byte)(i >> 8));
            buf.put((byte)i);
        }

        public void writeNBit(int value, int n)
        {
            if (n > 32)
                throw new ArgumentException("Max 32 bit to write");
            if (n == 0)
                return;
            value &= -1 >> (32 - n);
            if (32 - mcurBit >= n)
            {
                curInt |= value << (32 - mcurBit - n);
                mcurBit += n;
                if (mcurBit == 32)
                {
                    putInt(curInt);
                    mcurBit = 0;
                    curInt = 0;
                }
            }
            else
            {
                int secPart = n - (32 - mcurBit);
                curInt |= value >> secPart;
                putInt(curInt);
                curInt = value << (32 - secPart);
                mcurBit = secPart;
            }
        }

        public void write1Bit(int bit)
        {
            curInt |= bit << (32 - mcurBit - 1);
            ++mcurBit;
            if (mcurBit == 32)
            {
                putInt(curInt);
                mcurBit = 0;
                curInt = 0;
            }
        }

        public int curBit()
        {
            return mcurBit & 0x7;
        }

        public BitWriter fork()
        {
            return new BitWriter(buf.duplicate(), mcurBit, curInt, initPos);
        }

        public int position()
        {
            return ((buf.position() - initPos) << 3) + mcurBit;
        }

        public MemoryStream getBuffer()
        {
            return buf;
        }
    }
}
