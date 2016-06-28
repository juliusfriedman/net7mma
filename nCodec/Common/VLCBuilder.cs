using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public class VLCBuilder
    {

        private Dictionary<int, int> forward = new Dictionary<int, int>();
        private Dictionary<int, int> inverse = new Dictionary<int, int>();
        private List<int> codes = new List<int>();
        private List<int> codesSizes = new List<int>();

        public VLCBuilder()
        {
        }

        public VLCBuilder(int[] codes, int[] lens, int[] vals)
        {
            for (int i = 0; i < codes.Length; i++)
            {
                set(codes[i], lens[i], vals[i]);
            }
        }

        public VLCBuilder set(int val, String code)
        {
            set(int.Parse(code), code.Length, val); 

            return this;
        }

        public VLCBuilder set(int code, int len, int val)
        {
            codes.Add(code << (32 - len));
            codesSizes.Add(len);
            forward.Add(val, codes.Count() - 1);
            inverse.Add(codes.Count() - 1, val);

            return this;
        }

        public VLC getVLC() {
        //    return new VLC(codes.toArray(), codesSizes.toArray()) {
        //        public int readVLC(BitReader in) {
        //            return inverse.get(super.readVLC(in));
        //        }

        //        public int readVLC16(BitReader in) {
        //            return inverse.get(super.readVLC16(in));
        //        }

        //        public void writeVLC(BitWriter out, int code) {
        //            super.writeVLC(out, forward.get(code));
        //        }
        //    };

            return new VLC(Array.ConvertAll<int, string>(codes.ToArray(), Convert.ToString));

        }
    }
}
