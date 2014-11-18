using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class SupplementalEncoderInformationType
    {
        public const byte PictureTiming = 0x01;

        public const byte FillerPayload = 0x03;

        public const byte UserDataUnregistered = 0x05;

        public const byte RecoveryPoint = 0x06;
    }
}
