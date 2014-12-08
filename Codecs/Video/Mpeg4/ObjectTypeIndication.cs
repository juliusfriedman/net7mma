using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg4
{
    /// <summary>
    /// This type implements the ObjectTypeIndication used in MPEG-4 systems to indicate the type of data contained in a stream. 
    /// Applications for a new codec type will also automatically receive an object type indication.
    /// <see href="http://www.mp4ra.org/object.html#obj-a">MP4REG</see>
    /// </summary>
    public static class ObjectTypeIndication
    {
        public const byte Forbidden = 0x00;

        public const byte SystemsA = 0x01;

        public const byte SystemsB = 0x02;

        public const byte InteractionStream = 0x03;

        public const byte ExtendedBIFS = 0x04;

        public const byte AFXStream = 0x04;

        public const byte FontDataStream = 0x04;

        public const byte SynthetisedTexture = 0x04;

        public const byte TextStream = 0x04;

        public const byte LASeRStream = 0x04;

        public const byte SimpleAggregationFormatStream = 0x04;

        /// <summary>
        /// ITU-T Recommendation H.264
        /// </summary>
        public const byte H264 = 0x04;

        /// <summary>
        /// Parameter Sets for ITU-T Recommendation H.264
        /// </summary>
        public const byte H264ParameterSets = 0x04;

        /// <summary>
        /// Audio ISO/IEC 14496-3 (d)
        /// </summary>
        public const byte Audio = 0x40;

        public const byte VisualSimpleProfile = 0x60;

        public const byte VisualMainProfile = 0x61;

        public const byte VisualSNRProfile = 0x62;

        public const byte VisualSpatialProfile = 0x63;

        public const byte VisualHighProfile = 0x64;

        public const byte Visual422Profile = 0x65;

        public const byte AudioMainProfile = 0x66;

        public const byte AudioLowComplexityProfile = 0x66;

        public const byte AudioScaleableSamplingRateProfile = 0x66;

        public const byte Audio13818 = 0x69;

        public const byte Visual11172 = 0x6A;

        public const byte Audio11172 = 0x6B;

        public const byte Visual10918 = 0x6C;

        public const byte PortableNetworkGraphics = 0x6D;

        /// <summary>
        /// Visual ISO/IEC 15444-1 (JPEG 2000)
        /// </summary>
        public const byte Visual15444 = 0x6E;

        public const byte VoiceEVRC = 0xA0;

        public const byte VoiceSMV = 0xA1;

        public const byte CompactMultimediaFormat = 0xA2;

        public const byte VideoVC1 = 0xA3;

        public const byte VideoDirac = 0xA4;

        public const byte AudioAC3 = 0xA5;

        public const byte AudioEnhancedAC3 = 0xA6;

        public const byte AudioDRA = 0xA7;

        public const byte G719 = 0xA8;

        public const byte DTSCoherent = 0xA9;

        public const byte DTSHDHighResolution = 0xAA;

        public const byte DTSHDMaster = 0xAB;

        public const byte DTSExpress = 0xAC;

        /// <summary>
        /// 13K Voice
        /// </summary>
        public const byte Voice13K = 0xE1;

        public const byte Unspecified = byte.MaxValue;

        public static bool IsUserPrivate(byte b) { return (b >= 0xC0 && b <= 0xE0 || b >= 0xE2 && b <= 0xFE); }
    }

    /*http://wiki.multimedia.cx/index.php?title=MPEG-4_Audio#Audio_Object_Types
         MPEG-4 Audio
            Company: ISO
            Samples: http://samples.mplayerhq.hu/MPEG-4/
            Samples: http://samples.mplayerhq.hu/A-codecs/AAC/
            Samples: sample repo at standards.iso.org
            Sample Docs: sample docs
            Specification links:

            MPEG-4 Audio: ISO/IEC 14496-3:2009
            Conformance: ISO/IEC 14496-26:2010
            Contents [hide] 
            1 MPEG-4 Audio
            2 Subparts
            3 Audio Specific Config
            4 Audio Object Types
            5 Sampling Frequencies
            6 Channel Configurations
            MPEG-4 Audio
            MPEG-4 includes a system for handling a diverse group of audio formats in a uniform matter. Each format is assigned a unique Audio Object Type (AOT) to represent it. The common format Global header shared by all AOTs is called the Audio Specific Config.

            Subparts
            Subpart 0: Overview
            Subpart 1: Main (Systems Interaction)
            Subpart 2: Speech coding - HVXC
            Subpart 3: Speech coding - CELP
            Subpart 4: General Audio coding (GA) - AAC, TwinVQ, BSAC
            Subpart 5: Structured Audio (SA)
            Subpart 6: Text To Speech Interface (TTSI)
            Subpart 7: Parametric Audio Coding - HILN
            Subpart 8: Parametric coding for high quality audio - SSC (and Parametric Stereo)
            Subpart 9: MPEG-1/2 Audio in MPEG-4
            Subpart 10: Lossless coding of oversampled audio - DST
            Subpart 11: Audio lossless coding - ALS
            Subpart 12: Scalable lossless coding - SLS
            Audio Specific Config
            The Audio Specific Config is the global header for MPEG-4 Audio:

            5 bits: object type
            if (object type == 31)
                6 bits + 32: object type
            4 bits: frequency index
            if (frequency index == 15)
                24 bits: frequency
            4 bits: channel configuration
            var bits: AOT Specific Config
            Audio Object Types
            MPEG-4 Audio Object Types:

            0: Null
            1: AAC Main
            2: AAC LC (Low Complexity)
            3: AAC SSR (Scalable Sample Rate)
            4: AAC LTP (Long Term Prediction)
            5: SBR (Spectral Band Replication)
            6: AAC Scalable
            7: TwinVQ
            8: CELP (Code Excited Linear Prediction)
            9: HXVC (Harmonic Vector eXcitation Coding)
            10: Reserved
            11: Reserved
            12: TTSI (Text-To-Speech Interface)
            13: Main Synthesis
            14: Wavetable Synthesis
            15: General MIDI
            16: Algorithmic Synthesis and Audio Effects
            17: ER (Error Resilient) AAC LC
            18: Reserved
            19: ER AAC LTP
            20: ER AAC Scalable
            21: ER TwinVQ
            22: ER BSAC (Bit-Sliced Arithmetic Coding)
            23: ER AAC LD (Low Delay)
            24: ER CELP
            25: ER HVXC
            26: ER HILN (Harmonic and Individual Lines plus Noise)
            27: ER Parametric
            28: SSC (SinuSoidal Coding)
            29: PS (Parametric Stereo)
            30: MPEG Surround
            31: (Escape value)
            32: Layer-1
            33: Layer-2
            34: Layer-3
            35: DST (Direct Stream Transfer)
            36: ALS (Audio Lossless)
            37: SLS (Scalable LosslesS)
            38: SLS non-core
            39: ER AAC ELD (Enhanced Low Delay)
            40: SMR (Symbolic Music Representation) Simple
            41: SMR Main
            42: USAC (Unified Speech and Audio Coding) (no SBR)
            43: SAOC (Spatial Audio Object Coding)
            44: LD MPEG Surround
            45: USAC
            Sampling Frequencies
            There are 13 supported frequencies:

            0: 96000 Hz
            1: 88200 Hz
            2: 64000 Hz
            3: 48000 Hz
            4: 44100 Hz
            5: 32000 Hz
            6: 24000 Hz
            7: 22050 Hz
            8: 16000 Hz
            9: 12000 Hz
            10: 11025 Hz
            11: 8000 Hz
            12: 7350 Hz
            13: Reserved
            14: Reserved
            15: frequency is written explictly
            Channel Configurations
            These are the channel configurations:

            0: Defined in AOT Specifc Config
            1: 1 channel: front-center
            2: 2 channels: front-left, front-right
            3: 3 channels: front-center, front-left, front-right
            4: 4 channels: front-center, front-left, front-right, back-center
            5: 5 channels: front-center, front-left, front-right, back-left, back-right
            6: 6 channels: front-center, front-left, front-right, back-left, back-right, LFE-channel
            7: 8 channels: front-center, front-left, front-right, side-left, side-right, back-left, back-right, LFE-channel
            8-15: Reserved
         */
}
