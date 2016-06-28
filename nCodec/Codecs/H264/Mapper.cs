using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public interface Mapper
    {
        bool leftAvailable(int index);

        bool topAvailable(int index);

        int getAddress(int index);

        int getMbX(int mbIndex);

        int getMbY(int mbIndex);

        bool topRightAvailable(int mbIndex);

        bool topLeftAvailable(int mbIdx);
    }
}
