using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class FlatMBlockMapper : Mapper
    {
        private int frameWidthInMbs;
        private int firstMBAddr;

        public FlatMBlockMapper(int frameWidthInMbs, int firstMBAddr)
        {
            this.frameWidthInMbs = frameWidthInMbs;
            this.firstMBAddr = firstMBAddr;
        }

        public bool leftAvailable(int index)
        {
            int mbAddr = index + firstMBAddr;
            bool atTheBorder = mbAddr % frameWidthInMbs == 0;
            return !atTheBorder && (mbAddr > firstMBAddr);
        }

        public bool topAvailable(int index)
        {
            int mbAddr = index + firstMBAddr;
            return mbAddr - frameWidthInMbs >= firstMBAddr;
        }

        public int getAddress(int index)
        {
            return firstMBAddr + index;
        }

        public int getMbX(int index)
        {
            return getAddress(index) % frameWidthInMbs;
        }

        public int getMbY(int index)
        {
            return getAddress(index) / frameWidthInMbs;
        }

        public bool topRightAvailable(int index)
        {
            int mbAddr = index + firstMBAddr;
            bool atTheBorder = (mbAddr + 1) % frameWidthInMbs == 0;
            return !atTheBorder && mbAddr - frameWidthInMbs + 1 >= firstMBAddr;
        }

        public bool topLeftAvailable(int index)
        {
            int mbAddr = index + firstMBAddr;
            bool atTheBorder = mbAddr % frameWidthInMbs == 0;
            return !atTheBorder && mbAddr - frameWidthInMbs - 1 >= firstMBAddr;
        }
    }
}
