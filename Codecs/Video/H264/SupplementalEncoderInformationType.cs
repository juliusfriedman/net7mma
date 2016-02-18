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

        public const byte ScalabilityInfo = 24;

        public const byte SubPicScalableLayer = 25;

        public const byte NonRequiredLayerRep = 26;

        public const byte PriorityLayerInfo = 27;

        public const byte LayersNotPresent = 28;

        public const byte LayerDependencyChange = 29;

        public const byte ScalableNesting = 30;

        public const byte BaseLayerTemporalHrd = 31;

        public const byte QualityLayerIntegrityCheck = 32;

        public const byte RedundantPicProperty = 33;

        public const byte Tl0DepRepIndex = 34;

        public const byte TlSwitchingPoint = 35;

        public const byte ParallelDecodingInfo = 36;

        public const byte MvcScalableNesting = 37;

        public const byte ViewScalabilityInfo = 38;

        public const byte MultiviewSceneInfo = 39;

        public const byte MultiviewAcquisitionInfo = 40;

        public const byte NonRequiredViewComponent = 41;

        public const byte ViewDependencyChange = 42;

        public const byte OperationPointsNotPresent = 43;

        public const byte BaseViewTemporalHrd = 44;

        public const byte FramePackingArrangement = 45;

        public const byte MultiviewViewPosition = 46;

        public const byte DisplayOrientation = 47;

        public const byte MvcdScalableNesting = 48;

        public const byte MvcdViewScalabilityInfo = 49;

        public const byte DepthRepresentationInfo = 50;

        public const byte ThreeDimensionalReferenceDisplaysInfo = 51;

        public const byte DepthTiming = 52;

        public const byte DepthSamplingInfo = 53;
    }
}

