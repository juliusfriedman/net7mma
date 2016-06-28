using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class Header
    {
        private string p;
        private string p1;
        private int p2;

        public Header(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }

        public Header(string p1, int p2)
        {
            // TODO: Complete member initialization
            this.p1 = p1;
            this.p2 = p2;
        }

        internal object getFourcc()
        {
            throw new NotImplementedException();
        }

        internal int getBodySize()
        {
            throw new NotImplementedException();
        }

        internal void write(System.IO.MemoryStream dup)
        {
            throw new NotImplementedException();
        }

        internal void setBodySize(int p)
        {
            throw new NotImplementedException();
        }

        internal static Header read(System.IO.MemoryStream input)
        {
            throw new NotImplementedException();
        }

        public static object gclass { get; set; }
    }
}
