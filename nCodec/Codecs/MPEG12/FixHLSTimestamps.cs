using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class FixHLSTimestamps : FixTimestamp
    {
        private long[] lastPts = new long[256];

        public static void main(String[] args)
        {
            String wildCard = args[0];
            int startIdx = int.Parse(args[1]);

            new FixHLSTimestamps().doIt(wildCard, startIdx);
        }

        private void doIt(String wildCard, int startIdx)
        {
            Arrays.fill(lastPts, -1);
            for (int i = startIdx; ; i++)
            {
                string file = String.Format(wildCard, i);
                //System.out.println(file.getAbsolutePath());
                if (!System.IO.File.Exists(file))
                    break;
                this.fix(file);
            }
        }

        protected override long doWithTimestamp(long streamId, long pts, bool isPts)
        {
            if (!isPts)
                return pts;
            if (lastPts[streamId] == -1)
            {
                lastPts[streamId] = pts;
                return pts;
            }
            if (isVideo(streamId))
            {
                lastPts[streamId] += 3003;
                return lastPts[streamId];
            }
            else if (isAudio(streamId))
            {
                lastPts[streamId] += 1920;
                return lastPts[streamId];
            }
            throw new Exception("Unexpected!!!");
        }
    }
}
