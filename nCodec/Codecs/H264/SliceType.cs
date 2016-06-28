using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public enum SliceType
    {
        P, B, I, SP, SI
    }

    public static class SliceTypeExtensions
    {
        public static bool isIntra(this SliceType s)
        {
            return s == SliceType.I || s == SliceType.SI;
        }

        public static bool isInter(this SliceType s)
        {
            return s != SliceType.I && s != SliceType.SI;
        }
    }
}
