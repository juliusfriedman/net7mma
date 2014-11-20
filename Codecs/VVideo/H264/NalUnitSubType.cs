using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class NalUnitSubType
    {
        public const byte Reserved = 0x00;

        public const byte SingleNalUnitPacket = 0x01;

        public const byte AggregationPacket = 0x02;

        public static bool IsReserved(byte subType) { return subType == NalUnitSubType.Reserved || subType >= 3 && subType <= Common.Binary.FiveBitMaxValue; }
    }
}
