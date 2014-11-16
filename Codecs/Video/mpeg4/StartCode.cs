using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg4
{
    public static class StartCode
    {
        public static byte[] Prefix = new byte[] { 0x00, 0x00, 0x01};

        public const byte Picture = 0x00;

        public static bool IsSlice(byte code) { return code >= 0x01 && code <= 0xAF; }

        public const byte VisalObjectSequence = 0xB0;

        public const byte SequenceHeader = 0xB3;

        public const byte SequenceError = 0xB4;

        public const byte SequenceExtension = 0xB5;

        public const byte SequenceEnd = 0xB7;

        public const byte GroupOfPictures = 0xB8;
    }
}
