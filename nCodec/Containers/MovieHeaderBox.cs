using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using nVideo.Common;

namespace nVideo.Codecs.H264
{
    public class MovieHeaderBox : FullBox
    {
        private int timescale;
        private long duration;
        private float rate;
        private float volume;
        private long created;
        private long modified;
        private int[] matrix;
        private int nextTrackId;

        public static String fourcc()
        {
            return "mvhd";
        }

        public MovieHeaderBox(int timescale, long duration, float rate, float volume, long created, long modified,
                int[] matrix, int nextTrackId)
            : base(new Header(fourcc()))
        {


            this.timescale = timescale;
            this.duration = duration;
            this.rate = rate;
            this.volume = volume;
            this.created = created;
            this.modified = modified;
            this.matrix = matrix;
            this.nextTrackId = nextTrackId;
        }

        public MovieHeaderBox()
            : base(new Header(fourcc()))
        {

        }

        public int getTimescale()
        {
            return timescale;
        }

        public long getDuration()
        {
            return duration;
        }

        public int getNextTrackId()
        {
            return nextTrackId;
        }

        public float getRate()
        {
            return rate;
        }

        public float getVolume()
        {
            return volume;
        }

        public long getCreated()
        {
            return created;
        }

        public long getModified()
        {
            return modified;
        }

        public int[] getMatrix()
        {
            return matrix;
        }

        public void setTimescale(int newTs)
        {
            this.timescale = newTs;
        }

        public void setDuration(long duration)
        {
            this.duration = duration;
        }

        public void setNextTrackId(int nextTrackId)
        {
            this.nextTrackId = nextTrackId;
        }

        private int[] readMatrix(MemoryStream input)
        {
            int[] matrix = new int[9];
            for (int i = 0; i < 9; i++)
                matrix[i] = input.getInt();
            return matrix;
        }

        private float readVolume(MemoryStream input)
        {
            return (float)input.getShort() / 256f;
        }

        private float readRate(MemoryStream input)
        {
            return (float)input.getInt() / 65536f;
        }

        public override void parse(MemoryStream input)
        {
            base.parse(input);
            if (version == 0)
            {
                created = fromMovTime(input.getInt());
                modified = fromMovTime(input.getInt());
                timescale = input.getInt();
                duration = input.getInt();
            }
            else if (version == 1)
            {
                created = fromMovTime((int)input.getLong());
                modified = fromMovTime((int)input.getLong());
                timescale = input.getInt();
                duration = input.getLong();
            }
            else
            {
                throw new Exception("Unsupported version");
            }
            rate = readRate(input);
            volume = readVolume(input);
            StreamExtensions.skip(input, 10);
            matrix = readMatrix(input);
            StreamExtensions.skip(input, 24);
            nextTrackId = input.getInt();
        }

        private long fromMovTime(int p)
        {
            throw new NotImplementedException();
        }

        private long fromMovTime(short p)
        {
            throw new NotImplementedException();
        }

        protected override void doWrite(MemoryStream outb)
        {
            base.doWrite(outb);
            outb.putInt(toMovTime(created));
            outb.putInt(toMovTime(modified));
            outb.putInt(timescale);
            outb.putInt((int)duration);
            writeFixed1616(outb, rate);
            writeFixed88(outb, volume);
            //outb.put(new byte[10]);
            writeMatrix(outb);
            //outb.put(new byte[24]);
            outb.putInt(nextTrackId);
        }

        private int toMovTime(long created)
        {
            throw new NotImplementedException();
        }

        private void writeMatrix(MemoryStream outb)
        {
            for (int i = 0; i < Math.Min(9, matrix.Length); i++)
                outb.putInt(matrix[i]);
            for (int i = Math.Min(9, matrix.Length); i < 9; i++)
                outb.putInt(0);
        }

        private void writeFixed88(MemoryStream outb, float volume)
        {
            outb.putShort((short)(volume * 256));
        }

        private void writeFixed1616(MemoryStream outb, float rate)
        {
            outb.putInt((int)(rate * 65536));
        }
    }
}
