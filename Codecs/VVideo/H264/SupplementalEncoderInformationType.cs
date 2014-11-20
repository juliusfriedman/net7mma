using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class SupplementalEncoderInformationType
    {
        public const byte BufferingPeriod = 0;

        public const byte PictureTiming = 1;

        public const byte PanScan = 2;

        public const byte FillerPayload = 3;

        public const byte UserDataRegistered = 4; //ITU T T35

        public const byte UserDataUnregistered = 5;

        public const byte RecoveryPoint = 6;

        public const byte ReferencePictureMarkingRepetition = 7;

        public const byte SparePicture = 8;

        public const byte SceneInformation = 9;

        public const byte SubsequentSequenceInformation = 10;

        public const byte SubsequentSequenceLayerCharacteristics = 11;

        public const byte SubsequentSequenceCharacteristics = 12;

        public const byte FullFrameFreeze = 13;

        public const byte FullFrameRelease = 14;

        public const byte FullFrameSnapshot = 15;

        public const byte ProgressiveRefinementSegmentStart = 16;

        public const byte ProgressiveRefinementSegmentEnd = 17;

        public const byte MotionConstrainedSliceGroupSet = 18;

        public const byte FilmGrainCharacteristics = 19;

        public const byte DeblockingFilterDisplayPreference = 20;

        public const byte StereoVideoInformation = 21;

        public const byte PostFilterHints = 22;

        public const byte ToneMapping = 23;
    }
}

