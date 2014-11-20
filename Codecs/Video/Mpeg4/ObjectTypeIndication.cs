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
}
