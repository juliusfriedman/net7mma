using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public class VLC
    {

        private int[] codes;
        private int[] codeSizes;

        private int[] values;
        private int[] valueSizes;

        public VLC(int[] codes, int[] codeSizes)
        {
            this.codes = codes;
            this.codeSizes = codeSizes;

            invert();
        }

        public VLC(params String[] codes)
        {
            List<int> _codes = new List<int>();
            List<int> _codeSizes = new List<int>();
            foreach (string s in codes)
            {
                _codes.Add(int.Parse(s) << (32 - s.Length));
                _codeSizes.Add(s.Length);
            }
            this.codes = _codes.ToArray();
            this.codeSizes = _codeSizes.ToArray();

            invert();
        }

        private void invert()
        {
            List<int> values = new List<int>();
            List<int> valueSizes = new List<int>();
            invert(0, 0, 0, values, valueSizes);
            this.values = values.ToArray();
            this.valueSizes = valueSizes.ToArray();
        }

        private int invert(int startOff, int level, int prefix, List<int> values, List<int> valueSizes) {

        int tableEnd = startOff + 256;
        //values.fill(startOff, tableEnd, -1);
        //valueSizes.fill(startOff, tableEnd, 0);
        values.AddRange(Enumerable.Repeat(-1, tableEnd));
        valueSizes.AddRange(Enumerable.Repeat(0, tableEnd));

        int prefLen = level << 3;
        for (int i = 0; i < codeSizes.Length; i++) {
            if ((codeSizes[i] <= prefLen) || (level > 0 && (codes[i] >> (32 - prefLen)) != prefix))
                continue;

            int pref = codes[i] >> (32 - prefLen - 8);
            int code = pref & 0xff;
            int len = codeSizes[i] - prefLen;
            if (len <= 8) {
                for (int k = 0; k < (1 << (8 - len)); k++) {
                    values[startOff + code + k]= i;
                    valueSizes[startOff + code + k] = len;
                }
            } else {
                if (values[startOff + code] == -1) {
                    values[startOff + code] = tableEnd;
                    tableEnd = invert(tableEnd, level + 1, pref, values, valueSizes);
                }
            }
        }

        return tableEnd;
    }

        public int readVLC16(BitReader inb)
        {

            int s = inb.check16Bits();
            int b = s >> 8;
            int code = values[b];
            int len = valueSizes[b];

            if (len == 0)
            {
                b = (s & 0xff) + code;
                code = values[b];
                inb.skipFast(8 + valueSizes[b]);
            }
            else
                inb.skipFast(len);

            return code;
        }

        public int readVLC(BitReader inb) {

        int code = 0, len = 0, overall = 0, total = 0;
        for (int i = 0; len == 0; i++) {
            int s = inb.checkNBit(8);
            int ind = s + code;
            code = values[ind];
            len = valueSizes[ind];

            int bits = len != 0 ? len : 8;
            total += bits;
            overall = (overall << bits) | (s >> (8 - bits));
            inb.skip(bits);

            if (code == -1)
                throw new Exception("Invalid code prefix " + binary(overall, (i << 3) + bits));
        }

        // System.out.println("VLC: " + binary(overall, total));

        return code;
    }

        private String binary(int s, int len)
        {
            char[] symb = new char[len];
            for (int i = 0; i < len; i++)
            {
                symb[i] = (s & (1 << (len - i - 1))) != 0 ? '1' : '0';
            }
            return new String(symb);
        }

        public void writeVLC(BitWriter outb, int code)
        {
            outb.writeNBit(codes[code] >> (32 - codeSizes[code]), codeSizes[code]);
        }

        private String extracted(int num)
        {

            String str = (num & 0xff).ToString();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 8 - str.Length; i++)
                builder.Append('0');
            builder.Append(str);
            return builder.ToString();
        }

        public int[] getCodes()
        {
            return codes;
        }

        public int[] getCodeSizes()
        {
            return codeSizes;
        }
    }
}
