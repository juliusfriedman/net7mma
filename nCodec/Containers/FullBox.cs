using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.H264
{
    public class FullBox : Box
    {

        public FullBox(Header atom)
            : base(atom)
        {

        }

        protected byte version;
        protected int flags;

        public override void parse(MemoryStream input)
        {
            int vf = input.getInt();
            version = (byte)((vf >> 24) & 0xff);
            flags = vf & 0xffffff;
        }

        protected override void doWrite(MemoryStream outb)
        {
            outb.putInt((version << 24) | (flags & 0xffffff));
        }

        public byte getVersion()
        {
            return version;
        }

        public int getFlags()
        {
            return flags;
        }

        public void setVersion(byte version)
        {
            this.version = version;
        }

        public void setFlags(int flags)
        {
            this.flags = flags;
        }
    }
}
