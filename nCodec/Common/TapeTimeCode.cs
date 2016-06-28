using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class TapeTimecode
    {
        private short hour;
        private byte minute;
        private byte second;
        private byte frame;
        private bool dropFrame;

        public TapeTimecode(short hour, byte minute, byte second, byte frame, bool dropFrame)
        {
            this.hour = hour;
            this.minute = minute;
            this.second = second;
            this.frame = frame;
            this.dropFrame = dropFrame;
        }

        public short getHour()
        {
            return hour;
        }

        public byte getMinute()
        {
            return minute;
        }

        public byte getSecond()
        {
            return second;
        }

        public byte getFrame()
        {
            return frame;
        }

        public bool isDropFrame()
        {
            return dropFrame;
        }

        public String toString()
        {
            return String.Format("%02d:%02d:%02d", hour, minute, second) + (dropFrame ? ";" : ":")
                    + String.Format("%02d", frame);
        }
    }
}
