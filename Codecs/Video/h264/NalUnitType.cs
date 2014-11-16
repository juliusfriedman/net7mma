using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.H264
{
    public static class NalUnitType
    {
        public static byte[] StartCode = new byte[] { 0x00, 0x00, 0x01 };

        public const byte Unknown = 0;

        public const byte Slice = 1;

        public const byte PartitionA = 2;

        public const byte PartitionB = 3;

        public const byte PartitionC = 4;

        public const byte IDRSlice = 5;

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

        public const byte SingleTimeAggregationA = 24;

        public const byte SingleTimeAggregationB = 25;

        public const byte MultiTimeAggregation16 = 26;

        public const byte MultiTimeAggregation24 = 27;

        public const byte FragmentationUnitA = 28;

        public const byte FragmentationUnitB = 29;

        public const byte PayloadContentScalabilityInformation = 30;

        public const byte Reserved = 31;

        public const byte NonInterleavedMultiTimeAggregation = Reserved;
    }
}
