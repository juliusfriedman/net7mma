using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class RefPicMarkingIDR
    {
        bool discardDecodedPics;
        bool useForlongTerm;

        public RefPicMarkingIDR(bool discardDecodedPics, bool useForlongTerm)
        {
            this.discardDecodedPics = discardDecodedPics;
            this.useForlongTerm = useForlongTerm;
        }

        public bool isDiscardDecodedPics()
        {
            return discardDecodedPics;
        }

        public bool isUseForlongTerm()
        {
            return useForlongTerm;
        }
    }
}
