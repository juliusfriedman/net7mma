using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class SliceType
    {
        public const byte P = 0x00;

        public const byte B = 0x01;

        public const byte I = 0x02;

        public const byte SP = 0x03;

        public const byte SI = 0x04;

        public const byte PAlt = 0x05;

        public const byte BAlt = 0x06;

        public const byte IAlt = 0x07;

        public const byte SPAlt = 0x08;

        public const byte SIAlt = 0x09;

        public const byte Undefined = 0x0A;

        /// <summary>
        /// Determines if the given sliceType is a key type.
        /// </summary>
        /// <param name="sliceType"></param>
        /// <returns></returns>
        public static bool IsIntra(ref byte sliceType)
        {
            switch (sliceType)
            {
                case SliceType.I:
                case SliceType.SI:
                case SliceType.IAlt:
                case SliceType.SIAlt:
                    return true;
                default: return false;
            }
        }

        [CLSCompliant(false)]
        public static bool IsIntra(byte sliceType) { return IsIntra(ref sliceType); }
    }
}
