using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg2
{
    /// <summary>
    /// As defined in Table 6-1 ISO13818-2.
    /// </summary>
    public static class StartCode
    {
        public static byte[] Prefix = Media.Codecs.Video.Mpeg1.StartCode.Prefix;

        public const byte Picture = 0x00;

        //0 - 31
        //Probably Key Frames.
        public static bool IsVideoObjectStartCode(byte b) { return b >= Picture && b <= Common.Binary.FiveBitMaxValue; }

        public static bool IsReserved(byte b) { return b == 0xB0 || b == 0xB1 || b == 0xB6; }

        public const byte VisalObjectSequence = 0xB0;

        public const byte End = 0xB1;

        public const byte UserData = 0xB2;

        public const byte SequenceHeader = 0xB3;

        public const byte SequenceError = 0xB4;

        public const byte Extension = 0xB5;

        public const byte SequenceEnd = 0xB7;

        public const byte Group = 0xB8;

        public static bool IsSystemStartCode(byte b) { return b >= 0xB9 && b <= byte.MaxValue; }
    }
}
