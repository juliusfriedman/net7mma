using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class CAVLCReader
    {

        private CAVLCReader()
        {

        }

        public static int readNBit(BitReader bits, int n, String message)
        {
            int val = bits.readNBit(n);

            trace(message, val);

            return val;
        }

        private static void trace(string message, int val)
        {
            throw new NotImplementedException();
        }

        public static int readUE(BitReader bits)
        {
            int cnt = 0;
            while (bits.read1Bit() == 0 && cnt < 31)
                cnt++;

            int res = 0;
            if (cnt > 0)
            {
                long val = bits.readNBit(cnt);

                res = (int)((1 << cnt) - 1 + val);
            }

            return res;
        }

        public static int readUE(BitReader bits, String message)
        {
            int res = readUE(bits);

            trace(message, res);

            return res;
        }

        public static int readSE(BitReader bits, String message)
        {
            int val = readUE(bits);

            val = Utility.golomb2Signed(val);

            trace(message, val);

            return val;
        }

        public static bool readBool(BitReader bits, String message)
        {

            bool res = bits.read1Bit() == 0 ? false : true;

            trace(message, res ? 1 : 0);

            return res;
        }

        public static int readU(BitReader bits, int i, String s)
        {
            return (int)readNBit(bits, i, s);
        }

        public static int readTE(BitReader bits, int max)
        {
            if (max > 1)
                return readUE(bits);
            return ~bits.read1Bit() & 0x1;
        }

        public static int readME(BitReader bits, String s)
        {
            return readUE(bits, s);
        }

        public static int readZeroBitCount(BitReader bits, String message)
        {
            int count = 0;
            while (bits.read1Bit() == 0 && count < 32)
                count++;

            trace(message, count);

            return count;
        }

        public static bool moreRBSPData(BitReader bits)
        {
            return !(bits.remaining() < 32 && bits.checkNBit(1) == 1 && (bits.checkNBit(24) << 9) == 0);
        }
    }
}
