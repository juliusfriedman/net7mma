using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class GOPHeader
    {

        private TapeTimecode timeCode;
        private bool closedGop;
        private bool brokenLink;

        public GOPHeader(TapeTimecode timeCode, bool closedGop, bool brokenLink)
        {
            this.timeCode = timeCode;
            this.closedGop = closedGop;
            this.brokenLink = brokenLink;
        }

        public static GOPHeader read(MemoryStream bb)
        {
            BitReader inb = new BitReader(bb);
            bool dropFrame = inb.read1Bit() == 1;
            short hours = (short)inb.readNBit(5);
            byte minutes = (byte)inb.readNBit(6);
            inb.skip(1);

            byte seconds = (byte)inb.readNBit(6);
            byte frames = (byte)inb.readNBit(6);

            bool closedGop = inb.read1Bit() == 1;
            bool brokenLink = inb.read1Bit() == 1;

            return new GOPHeader(new TapeTimecode(hours, minutes, seconds, frames, dropFrame), closedGop, brokenLink);
        }

        public void write(MemoryStream os)
        {
            BitWriter outb = new BitWriter(os);
            if (timeCode == null)
                outb.writeNBit(0, 25);
            else
            {
                outb.write1Bit(timeCode.isDropFrame() ? 1 : 0);
                outb.writeNBit(timeCode.getHour(), 5);
                outb.writeNBit(timeCode.getMinute(), 6);
                outb.write1Bit(1);
                outb.writeNBit(timeCode.getSecond(), 6);
                outb.writeNBit(timeCode.getFrame(), 6);
            }
            outb.write1Bit(closedGop ? 1 : 0);
            outb.write1Bit(brokenLink ? 1 : 0);
        }

        public TapeTimecode getTimeCode()
        {
            return timeCode;
        }

        public bool isClosedGop()
        {
            return closedGop;
        }

        public bool isBrokenLink()
        {
            return brokenLink;
        }
    }
}
