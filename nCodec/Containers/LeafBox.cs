using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class LeafBox : Box
    {
        private MemoryStream data;

        public LeafBox(Header atom)
            : base(atom)
        {

        }

        public LeafBox(Header atom, MemoryStream data)
            : base(atom)
        {
            this.data = data;
        }

        public override void parse(MemoryStream input)
        {
            data = StreamExtensions.read(input, (int)header.getBodySize());
        }

        public MemoryStream getData()
        {
            return data.duplicate();
        }

        protected override void doWrite(MemoryStream outb)
        {
            StreamExtensions.write(outb, data);
        }
    }
}
