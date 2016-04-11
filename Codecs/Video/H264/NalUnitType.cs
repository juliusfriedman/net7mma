using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class NalUnitType
    {
        //Mpeg start codes
        public static byte[] StartCodePrefix = new byte[] { 0x00, 0x00, 0x01 };

        public const byte Unknown = 0;

        //IsVideoCodingLayer => True

        public const byte CodedSlice = 1;

        public const byte DataPartitionA = 2;

        public const byte DataPartitionB = 3;

        public const byte DataPartitionC = 4;

        public const byte InstantaneousDecoderRefresh = 5;

        //NonVideoCodingLayer

        public const byte SupplementalEncoderInformation = 6;

        public const byte SequenceParameterSet = 7;

        public const byte PictureParameterSet = 8;

        public const byte AccessUnitDelimiter = 9;

        public const byte EndOfSequence = 10;

        public const byte EndOfStream = 11;

        public const byte FillerData = 12;

        public const byte SequenceParameterSetExtension = 13;

        public const byte Prefix = 14;

        public const byte SequenceParameterSetSubset = 15;

        public const byte AuxiliarySlice = 19;

        public const byte SliceExtension = 20;

        //21 SliceExtensionForDepthView

        //24 DependencyRepresentationDelimiter in BluRay
        public const byte SingleTimeAggregationA = 24;

        public const byte SingleTimeAggregationB = 25;

        public const byte MultiTimeAggregation16 = 26;

        public const byte MultiTimeAggregation24 = 27;

        public const byte FragmentationUnitA = 28;

        public const byte FragmentationUnitB = 29;

        public const byte PayloadContentScalabilityInformation = 30;

        public const byte Reserved = 31;

        public static bool IsReserved(ref byte nalType)
        {

            switch (nalType)
            {
                case Reserved:
                //nalType >= 16 && nalType <= 18
                case 16:
                case 17:
                case 18:
                //nalType >= 22 && nalType <= 23
                case 22:
                case 23:
                    return true;
                default: return false;
            }

            //return nalType == Reserved || nalType >= 16 && nalType <= 18 || nalType >= 22 && nalType <= 23;
        }

        [CLSCompliant(false)]
        public static bool IsReserved(byte nalType) { return IsReserved(ref nalType); }

        public const byte NonInterleavedMultiTimeAggregation = Reserved;

        public static bool IsSlice(ref byte nalType)
        {
            switch (nalType)
            {
                case CodedSlice:
                case DataPartitionA:
                case InstantaneousDecoderRefresh:
                    return true;
                default: return false;
            }
        }

        [CLSCompliant(false)]
        public static bool IsSlice(byte nalType) { return IsSlice(ref nalType); }
    }
}
