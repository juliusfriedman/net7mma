using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using nVideo.Common;

namespace nVideo.Codecs.H264
{
    public class SampleEntry : NodeBox
    {

        private short drefInd;

        public SampleEntry(Header header)
            : base(header)
        {
        }

        public SampleEntry(Header header, short drefInd)
            : base(header)
        {
            this.drefInd = drefInd;
        }

        public override void parse(MemoryStream input)
        {
            input.getInt();
            input.getShort();

            drefInd = input.getShort();
        }

        protected void parseExtensions(MemoryStream input)
        {
            base.parse(input);
        }

        protected override void doWrite(MemoryStream outb)
        {
            //outb.put(new byte[] { 0, 0, 0, 0, 0, 0 });
            outb.putShort(drefInd); // data ref index
        }

        protected void writeExtensions(MemoryStream outb)
        {
            base.doWrite(outb);
        }

        public short getDrefInd()
        {
            return drefInd;
        }

        public void setDrefInd(short ind)
        {
            this.drefInd = ind;
        }

        public void setMediaType(String mediaType)
        {
            header = new Header(mediaType);
        }
    }
}
