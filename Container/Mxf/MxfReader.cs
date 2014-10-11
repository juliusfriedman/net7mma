using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Mxf
{
    /// <summary>
    /// Represents the logic necessary to read files in the Material Exchange Format
    /// </summary>
    public class MxfReader : MediaFileStream, IMediaContainer
    {
        /// <summary>
        /// Defines the known types in the Material Exchange Format
        /// </summary>
        public static class Identifier
        {

            /* 
            - Identifier Format Information -
                1 Object Identifier 06h
                2 Label size 0Eh
                3 Designator 2Bh ISO, ORG
                4 Designator 34h SMPTE
                5 Registry Category Designator 02h KLV Sets & Packs
                6 Registry Designator 05h Defined-Length Packs (no length fields)
                7 Structure Designator 01h Set / Pack registry
                8 Version Number vvh Registry Version in which the specific Key
                    first appeared
                9 Item Designator 0Dh Organizationally registered
                10 Organization 01h AAF
                11 Application 02h MXF File Structure
                12 Structure Version 01h Version 1
                13 Structure Kind 01h MXF File Structure sets & packs
                14 Set / Pack Kind Sections 7.2, 7.3, 7.4 Header Partition, Body Partition or Footer
                    Partition
                15 Partition Status Sections 7.2, 7.3, 7.4 Open and Incomplete (01h)
                    Closed and Incomplete (02h)
                    Open and Complete (03h)
                    Closed and Complete (04h)
                16 Reserved 00h
                Byte 15 provides four alternate Partition Pack Key values for the combinations provided by an Open or
                Closed Partition, and Complete or Incomplete Header Metadata as defined in Section 6.2.3.
                Other MXF standards that define other types of Partitions may define additional values of byte 15.
            Note: SMPTE 410M is such an MXF standard.
             */


            //Header, Body and Footer Klv - The last 2 bytes indicate type and open or closed.                                                         //Type, //Status, //Reserved
            public static Guid HeaderPartitionPack = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x05, 0x01, 0x01, 0x0D, 0x01, 0x02, 0x01, 0x01, 0x02, 0x00, 0x00 });
            
            //For tracks the last 4 are the trackId 
            public static Guid TrackPack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x01, 0x02, 0x01, 0x01, 0x0D, 0x01, 0x03, 0x01, 0, 0, 0, 0 });

            /*
             8.2 Generic Universal Label for All Operational Patterns
            The value of the Operational Pattern UL used to identify any MXF Operational Pattern shall be defined in the
            table below.
            Table 9 – Value of the MXF Operational Pattern Identification Universal Label
             Byte No. Description Value (hex) Meaning
             1 Object Identifier 06h
             2 Label size 0Eh
             3 Designator 2Bh ISO, ORG
             4 Designator 34h SMPTE
             5 Registry Category Designator 04h Labels
             6 Registry Designator 01h Labels
             7 Structure Designator 01h Labels
             8 Version Number vvh Registry Version in which the Label first appeared
             9 Item Designator 0Dh Organizationally Registered
             10 Organization 01h AAF Association
             11 Application 02h Operational Patterns
             12 Structure Version 01h Version 1
             13 Operational Pattern Definition xxh Item Complexity
             14~16 Definition depends on byte 13 xxh
             */

            public static Guid OperationalPattern = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x0D, 0x01, 0x02, 0x01, 0, 0, 0, 0 });


            //Value 8 may be different >0 ? - ff
            public static Guid PrimerPack = new Guid(new byte[] { 0x06, 0x0E, 0x2B , 0x34 , 0x02 , 0x05 , 0x01 , 0x01 , 0x0D , 0x01 , 0x02 , 0x01 , 0x01 , 0x05 , 0x01 , 0x00 });

            public static Guid RandomIndexPack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x05 , 0x01 , 0x01 , 0x0D , 0x01 , 0x02 , 0x01 , 0x01 , 0x11 , 0x01 , 0x00 });

            public static Guid PartitionMetadata = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x05, 0x01, 0x01, 0x0d, 0x01, 0x02, 0x01, 0x01, 0x04, 0x04, 0x00 });

            public static Guid EncryptedEssenceContainer = new Guid ( new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x04, 0x01, 0x01, 0x07, 0x0d, 0x01, 0x03, 0x01, 0x02, 0x0b, 0x01, 0x00 });

            public static Guid FillerPack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x01, 0x01, 0x01, 0x02, 0x03, 0x01, 0x02, 0x10, 0x01, 0x00, 0x00, 0x00 });

            //Rp210 version number byte (8) is different
            public static Guid LegacyFillerPack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x01, 0x01, 0x01, 0x01, 0x03, 0x01, 0x02, 0x10, 0x01, 0x00, 0x00, 0x00 });

            public static Guid Index = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x02, 0x01, 0x01, 0x10, 0x01, 0x00 });

            //public static Guid StructuralMetadata = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00 });

            //Structural MetaData Sets

            public static Guid InterchangeObject = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00 });

            public static Guid StructuralComponent = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x00 });

            public static Guid Segment = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x03, 0x00 });

            public static Guid Event = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x06, 0x00 });

            public static Guid CommentMarker = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x08, 0x00 });

            public static Guid Preface = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x2F, 0x00 });

            public static Guid Identification = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x30, 0x00 });

            public static Guid ContentStorage = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x18, 0x00 });

            public static Guid EssenceContainerData = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x23, 0x00 });

            public static Guid GenericDescriptor = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x24, 0x00 });

            public static Guid FileDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x25, 0x00 });

            public static Guid GenericPackage = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x34, 0x00 });

            public static Guid GenericTrack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x38, 0x00 });

            public static Guid SubDescriptor = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x59, 0x00 });

            public static Guid MaterialPackage = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x36, 0x00 });

            public static Guid SourcePackage = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x37, 0x00 });

            public static Guid TimelineTrack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x3B, 0x00 });

            public static Guid EventTrack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x39, 0x00 });

            public static Guid StaticTrack = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x3A, 0x00 });

            public static Guid Sequence = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x0F, 0x00 });

            public static Guid SourceClip = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x11, 0x00 });            

            public static Guid TimecodeComponent = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x14, 0x00 });

            public static Guid DMSegment = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x41, 0x00 });

            public static Guid DMSourceClip = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x45, 0x00 });

            public static Guid Filler = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x09, 0x00 });

            public static Guid PackageMarkerObject = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x60, 0x00 });

            // Also 6th Byte allowed to be 13

            public static Guid FillerAlt = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x13, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x09, 0x00 });

            public static Guid PackageMarkerObjectAlt = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x13, 0x01, 0x01, 0x0D, 0x01, 0x01, 0x01, 0x01, 0x01, 0x60, 0x00 });            

            //

            public static Guid GenericPictureEssenceDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x27, 0x00 });

            public static Guid CDCIEssenceDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x28, 0x00 });

            public static Guid RGBAEssenceDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x29, 0x00 });

            public static Guid GenericSoundEssenceDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x42, 0x00 });

            public static Guid GenericDataEssenceDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x43, 0x00 });

            public static Guid MultipleDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x44, 0x00 });

            public static Guid NetworkLocator = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x32, 0x00 });

            public static Guid TextLocator = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x33, 0x00 });

            public static Guid  ApplicationPlugInObject = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x61, 0x00 });

            public static Guid ApplicationReferencedObject = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x62, 0x00 });

            //6th byte allowed to be 13
            
            public static Guid  ApplicationPlugInObjectAlt = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x13, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x61, 0x00 });

            public static Guid ApplicationReferencedObjectAlt = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x13, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x62, 0x00 });

            //

            public static Guid ApplicationObject = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x66, 0x00 });

            //

           //D-10 Mapping 0x06,0x0e,0x2b,0x34,0x04,0x01,0x01,0x01,0x0d,0x01,0x03,0x01,0x02,0x01,0x00,0x00 = MPEG2Video

            public static Guid MPEG2VideoDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x51, 0x00 });

            public static Guid WaveAudioDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x48, 0x00 });

            public static Guid AES3PCMDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x47, 0x00 });

            public static Guid J2KPictureDescriptor = new Guid(new byte[] { 0x06, 0x0e, 0x2b, 0x34, 0x02, 0x53, 0x01, 0x01, 0x0d, 0x01, 0x01, 0x01, 0x01, 0x01, 0x5a, 0x00 });            

            //Codecs

            public static Guid MPEG2_XDCam = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x03, 0x04, 0x01, 0x02, 0x02, 0x01, 0x04, 0x03, 0x00 });

            public static Guid MPEG2_ML = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x03, 0x04, 0x01, 0x02, 0x02, 0x01, 0x01, 0x11, 0x00 });

            public static Guid MPEG2_D10_PAL = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x01, 0x02, 0x02, 0x01, 0x02, 0x01, 0x01 });

            public static Guid MPEG2_HL = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x03, 0x04, 0x01, 0x02, 0x02, 0x01, 0x03, 0x03, 0x00 });

            public static Guid MPEG2_HL_422_I = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x03, 0x04, 0x01, 0x02, 0x02, 0x01, 0x04, 0x02, 0x00 });

            public static Guid MPEG4_XDCam_Proxy = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x03, 0x04, 0x01, 0x02, 0x02, 0x01, 0x20, 0x02, 0x03 });

            public static Guid DV_25_PAL = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x01, 0x02, 0x02, 0x02, 0x01, 0x02, 0x00 });

            //Jpeg
            /*
             This "dark" EssenceContainer contains JPEG and uncompressed audio in an undocumented interleaved fashion. Fields of video are compressed as JPEG images and arranged in "clumps" of 18 fields, followed by uncompressed audio data, followed by the next clump of JPEGs and so on.
             */

            //Sony Long GOP?
            //06-0E-2B-34-02-05-01-01-0D-01-03-01-04-01-01-00

            //?
            //06 0e 2b 34 01 01 01 05 03 01 02 20 01 00 00 00

            public static Guid Dark = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x02, 0x05, 0x01, 0x01, 0x0d, 0x01, 0x03, 0x01, 0x18, 0x01, 0x02, 0x01 });

            public static Guid JPEG2000 = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x07, 0x04, 0x01, 0x02, 0x02, 0x03, 0x01, 0x01, 0x00 });

            public static Guid Raw = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x01, 0x02, 0x01, 0x7F, 0x00, 0x00, 0x00 });

            public static Guid VC3_DNXD = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x01, 0x02, 0x02, 0x03, 0x02, 0x00, 0x00 });

            public static Guid AVC_INTRA = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x0A, 0x04, 0x01, 0x02, 0x02, 0x01, 0x32, 0x00, 0x00 });

            public static Guid V210 = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x0A, 0x04, 0x01, 0x02, 0x01, 0x01, 0x02, 0x02, 0x00 });

            public static Guid PCM_S16LE_1 = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x02, 0x02, 0x01, 0x00, 0, 0, 0 });

            public static Guid PCM_S16LE_2 = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x02, 0x02, 0x01, 0x7F, 0, 0, 0 });

            public static Guid PCM_S16LE_3 = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x02, 0x02, 0x01, 0x01, 0, 0, 0 });

            public static Guid PCM_S16BE = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x07, 0x04, 0x02, 0x02, 0x01, 0x7E, 0, 0, 0 });

            public static Guid PCM_ALAW = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x04, 0x04, 0x02, 0x02, 0x02, 0x03, 0x01, 0x01, 0x00 });

            public static Guid AC3 = new Guid ( new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x02, 0x02, 0x02, 0x03, 0x02, 0x01, 0x00 });

            public static Guid MP2 = new Guid(new byte[] { 0x06, 0x0E, 0x2B, 0x34, 0x04, 0x01, 0x01, 0x01, 0x04, 0x02, 0x02, 0x02, 0x03, 0x02, 0x05, 0x00 });
        }

        //Byte 14 of any Identifier
        public enum PartitionKind
        {
            Unknown = 0,
            Other = 1,
            Header = 2,
            Body = 3,
            Footer = 4,
        }

        //Byte 15 of any Identifier
        public enum PartitionStatus
        {
            Unknown = 0,
            OpenAndIncomplete = 1,
            ClosedAndIncomplete = 2,
            OpenAndComplete = 3,
            ClosedAndComplete = 4
        }

        //Byte 13 of operational pattern
        public enum OperationalPatternItemComplexity
        {
            Unknown = 0,
            SingleItem = 1,
            PlayListItems = 2,
            EditItems = 3
            //10h - 7h = Specialized
        }

        //Byte 14 of operational pattern
        public enum OperationalPatternPackageComplexity
        {
            Unknown = 0,
            SinglePackage = 1,
            GangedPackages = 2,
            AlternatePackages = 3
        }

        //Byte 15 in a OperationalPattern is bit wise
        // 0 Value = 1 Marker bit
        // 1 = 0, internal Essence, 1 = external Essence
        // 2 = 0 = stream file, 1 = non stream file
        // 3 = 0 = uniTack, 1 = multiTrack
        // 7 - 4 Reserved for future use...

        #region Constants

        const int IdentifierSize = 16, MinimumSizeLength = 1, MinimumSize = IdentifierSize + MinimumSizeLength, UniqueIdBytes = 12, MultiByteLength = 0x80; //128

        const string PictureTrack = "Picture Track", AudioTrack = "Audio Track", TextTrack = "Text Track", TimecodeTrack = "Timecode Track";

        #endregion

        #region Statics
        
        /// <summary>
        /// KeyLengthValues have a UL (Identifier) which is 16 bytes.
        /// UL is Similar to OID.
        /// The last 4 bytes define various information about the UL which does not change it's value.
        /// </summary>
        internal class IdentifierComparer : IEqualityComparer<Guid>
        {
            public bool Equals(Guid a, Guid b)
            {
                if (a == null || b == null) return false;
                return a.ToByteArray().Take(UniqueIdBytes).SequenceEqual(b.ToByteArray().Take(UniqueIdBytes));
            }

            public int GetHashCode(Guid a) { return a.GetHashCode(); }
        }

        static IEqualityComparer<Guid> CompareUniquePart = new IdentifierComparer();

        public static string ToTextualConvention(byte[] identifier, int offset = 0)
        {
            if (identifier == null) return Utility.Unknown;

            var uniquePart = identifier.Skip(offset).Take(UniqueIdBytes);
            
            if (Identifier.HeaderPartitionPack.ToByteArray().Take(UniqueIdBytes).SequenceEqual(uniquePart)) return "HeaderPartitionPack"; //Last 4 bytes is version and open or closed
            //else if (Identifier.StructuralMetaData.ToByteArray().Take(UniqueIdBytes).SequenceEqual(uniquePart)) return "StructuralMetaData"; //Last 4 bytes is version and open or closed
            //else if (Identifier.GenericDataEssenceDescriptor.ToByteArray().Take(UniqueIdBytes).SequenceEqual(uniquePart)) return "GenericDataEssenceDescriptor";
            else if (Identifier.OperationalPattern.ToByteArray().Take(UniqueIdBytes).SequenceEqual(uniquePart)) return "OperationalPattern";
            else if (Identifier.TrackPack.ToByteArray().Take(UniqueIdBytes).SequenceEqual(uniquePart)) return "TrackData"; //Track Id is last 4 bytes?

            Guid id = offset > 0 || identifier.Length > 16 ? new Guid(identifier.Skip(offset).Take(IdentifierSize).ToArray()) : new Guid(identifier);

            string result;

            if (!IdentifierLookup.TryGetValue(id, out result)) result = Utility.Unknown;

            return result;
        }

        /// <summary>
        /// Decodes the BER Length from the given packet at the given position
        /// </summary>
        public static long DecodeLength(System.IO.Stream stream, out int read)
        {
            read = 0;
            
            long length = stream.ReadByte();

            ++read;

            if (length >= MultiByteLength)
            {
                length &= ~MultiByteLength;

                if (length == 0) throw new InvalidOperationException("BER32 Indefinite Length Not Supported.");

                if (length > 8) throw new InvalidOperationException("BER32 Lengths larger than 8 are Not Supported.");

                int end = (int)(length & sbyte.MaxValue);

                //Value is 0
                length = 0;

                //While there are bytes in the value
                while(end-- > 0)
                {
                    length <<= 8;
                    length |= (byte)(stream.ReadByte());
                    ++read;
                }

                if (length < 0) throw new InvalidOperationException("BER32 Lengths > 2^63 are not Supported.");
            }

            return length;
        }

        /// <summary>
        /// Holds a cache of all Fields in the Identifiers static type
        /// </summary>
        static Dictionary<Guid, string> IdentifierLookup;

        static MxfReader()
        {
            IdentifierLookup = new Dictionary<Guid, string>();

            foreach (var fieldInfo in typeof(Identifier).GetFields()) IdentifierLookup.Add((Guid)fieldInfo.GetValue(null), fieldInfo.Name);
        }

        #endregion

        //struct Op ?

        public MxfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public MxfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        int? m_RunInSize, m_MajorVersion, m_MinorVersion, m_IndexByteCount, m_KagSize;

        Guid? m_OperationalPattern;

        public Version HeaderVersion
        {
            get
            {
                if (!m_MajorVersion.HasValue) ParseHeader();
                return new Version(m_MajorVersion.Value, m_MinorVersion.Value);
            }
        }

        public int AlignmentGridByteSize
        {
            get
            {
                if (!m_KagSize.HasValue) ParseHeader();
                return m_KagSize.Value;
            }
        }

        public int IndexByteCount
        {
            get
            {
                if (!m_IndexByteCount.HasValue) ParseHeader();
                return m_IndexByteCount.Value;
            }
        }

        public bool HasRunIn
        {
            get
            {
                if (!m_RunInSize.HasValue) ParseHeader();
                return m_RunInSize.Value > 0;
            }
        }

        public int RunInSize
        {
            get
            {
                if (!m_RunInSize.HasValue) ParseHeader();
                return m_RunInSize.Value;
            }
        }

        public Guid OperationalPattern
        {
            get
            {
                if (!m_OperationalPattern.HasValue) ParseHeader();
                return m_OperationalPattern.Value;
            }
        }

        //Operational Pattern Definition - Item Complexity
        public OperationalPatternItemComplexity ItemComplexity
        {
            get
            {
                if (!m_OperationalPattern.HasValue) ParseHeader();
                return (OperationalPatternItemComplexity)m_OperationalPattern.Value.ToByteArray()[13];
            }
        }

        //Depends on byte 13
        //public OperationalPatternPackageComplexity PackageComplexity
        //{
        //    get
        //    {
        //        if (!m_OperationalPattern.HasValue) ParseHeader();
        //        return (OperationalPatternPackageComplexity)m_OperationalPattern.Value.ToByteArray()[14];
        //    }
        //}

        void ParseHeader()
        {
            long position = Position;

            using (var headerPartition = Root)
            {

                int offset = 0;

                //Determine if any Runin was present (data which is not part of the file header)
                m_RunInSize = (int)headerPartition.Offset - 20;

                m_MajorVersion = Common.Binary.Read16(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                
                m_MinorVersion = Common.Binary.Read16(headerPartition.Raw, offset + 2, BitConverter.IsLittleEndian);

                offset += 4;

                m_KagSize = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);

                offset += 4;

                long thisPartition = Common.Binary.Read64(headerPartition.Raw, offset, BitConverter.IsLittleEndian),
                    previousPartition = Common.Binary.Read64(headerPartition.Raw, offset + 8, BitConverter.IsLittleEndian),
                    footerPartitionOffset = Common.Binary.Read64(headerPartition.Raw, offset + 16, BitConverter.IsLittleEndian),
                    headerByteCount = Common.Binary.Read64(headerPartition.Raw, offset + 32, BitConverter.IsLittleEndian);

                m_IndexByteCount = (int)Common.Binary.Read64(headerPartition.Raw, offset + 40, BitConverter.IsLittleEndian);
                
                //Header should be first partition and have no previous 
                if (thisPartition != 0 || previousPartition != 0) throw new InvalidOperationException("Invalid HeaderParition");

                offset += 40;

                int indexSid = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                offset += 4;

                //Cannot have a index sid when no byte count, dont throw here.
                if (indexSid <= 0 && m_IndexByteCount > 0) m_IndexByteCount = 0;

                //Specfies offset to body (essence container segment)

                long bodyOffset = Common.Binary.Read64(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                offset += 8;

                int bodySid = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                offset += 4;

                m_OperationalPattern = new Guid(headerPartition.Raw.Skip(offset).Take(16).ToArray());

                offset += 16;

                int batchLen = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);

                offset += 4;

                //List<Guid> batches = new List<Guid>();

                //if (batchLen > 0)
                //{
                //    int itemLen = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                //    offset += 4;

                //    for (int i = 0; i < batchLen; ++i)
                //    {
                //        batches.Add(new Guid(headerPartition.Raw.Skip(offset).Take(itemLen).ToArray()));
                //        offset += itemLen;
                //    }
                //}

                PartitionStatus status = (PartitionStatus)Root.Identifier[14];

                /*
                //Check for metaData and identifier to be open or closed and complete
                if (headerByteCount > 0 && status == PartitionStatus.OpenAndComplete || status == PartitionStatus.ClosedAndComplete)
                {

                    //read all metaData within Header parition

                    //if(indexByteCount > 0) //ParseIndex?

                    
                }
                else */

                if (footerPartitionOffset > 0 && (status == PartitionStatus.OpenAndIncomplete || status == PartitionStatus.ClosedAndIncomplete))
                {
                    Position = footerPartitionOffset;

                    using (var footer = ReadNext())
                    {

                        offset = 0;

                        m_MajorVersion = Common.Binary.Read16(footer.Raw, offset, BitConverter.IsLittleEndian);

                        m_MinorVersion = Common.Binary.Read16(footer.Raw, offset + 2, BitConverter.IsLittleEndian);

                        offset += 4;

                        m_KagSize = Common.Binary.Read32(footer.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        thisPartition = Common.Binary.Read64(footer.Raw, offset, BitConverter.IsLittleEndian);
                        previousPartition = Common.Binary.Read64(footer.Raw, offset + 8, BitConverter.IsLittleEndian);
                        footerPartitionOffset = Common.Binary.Read64(footer.Raw, offset + 16, BitConverter.IsLittleEndian);
                        headerByteCount = Common.Binary.Read64(footer.Raw, offset + 32, BitConverter.IsLittleEndian);

                        if (thisPartition != footerPartitionOffset) throw new InvalidOperationException("Invalid FooterParition");

                        m_IndexByteCount = (int)Common.Binary.Read64(footer.Raw, offset + 40, BitConverter.IsLittleEndian);

                        //this partition should equal Footer
                        
                        offset += 40;

                        indexSid = Common.Binary.Read32(footer.Raw, offset, BitConverter.IsLittleEndian);
                        offset += 4;

                        bodyOffset = Common.Binary.Read64(footer.Raw, offset, BitConverter.IsLittleEndian);
                        offset += 8;

                        bodySid = Common.Binary.Read32(footer.Raw, offset, BitConverter.IsLittleEndian);
                        offset += 4;

                        if(bodyOffset != 0 || bodySid != 0) throw new InvalidOperationException("Invalid FooterPartition");

                        m_OperationalPattern = new Guid(footer.Raw.Skip(offset).Take(16).ToArray());

                        offset += 16;

                        //batchLen = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);

                        //offset += 4;

                        //List<Guid> batches = new List<Guid>();

                        //if (batchLen > 0)
                        //{
                        //    int itemLen = Common.Binary.Read32(headerPartition.Raw, offset, BitConverter.IsLittleEndian);
                        //    offset += 4;

                        //    for (int i = 0; i < batchLen; ++i)
                        //    {
                        //        batches.Add(new Guid(headerPartition.Raw.Skip(offset).Take(itemLen).ToArray()));
                        //        offset += itemLen;
                        //    }
                        //}

                        //status = (PartitionStatus)footer.Identifier[14];

                        //if (headerByteCount > 0 && status == PartitionStatus.OpenAndComplete || status == PartitionStatus.ClosedAndComplete)
                        //{

                        //}

                    }

                }
            }

            Position = position;
        }

        DateTime? m_PrefaceLastModifiedDate;

        short? m_PrefaceVersion;

        public short PrefaceVersion
        {
            get
            {
                if (!m_PrefaceVersion.HasValue) ParsePreface();
                return m_PrefaceVersion.Value;
            }
        }

        public DateTime PrefaceLastModifiedDate
        {
            get
            {
                if (!m_PrefaceLastModifiedDate.HasValue) ParsePreface();
                return m_PrefaceLastModifiedDate.Value;
            }
        }

        void ParsePreface()
        {
            using (var preface = ReadObject(Identifier.Preface, true, Root.Offset + Root.Size))
            {
                int offset = 0, lenth = (int)preface.Size;

                while (offset < lenth)
                {

                    short tag = Common.Binary.Read16(preface.Raw, offset, BitConverter.IsLittleEndian),
                        tagLen = Common.Binary.Read16(preface.Raw, offset + 2, BitConverter.IsLittleEndian);

                    offset += 4;

                    switch (tag)
                    {
                        case 0x3b02:
                            {
                                m_PrefaceLastModifiedDate = new DateTime((int)Common.Binary.ReadU16(preface.Raw, offset, BitConverter.IsLittleEndian),
                                    (int)preface.Raw[offset + 2],
                                    (int)preface.Raw[offset + 3],
                                    (int)preface.Raw[offset + 4],
                                    (int)preface.Raw[offset + 5],
                                    (int)preface.Raw[offset + 6],
                                    (int)preface.Raw[offset + 7],
                                    DateTimeKind.Utc);
                                goto default;
                            }
                        case 0x3b05:
                            {
                                //Should be 259
                                //Also found 258 and lower
                                //Sometimes found something higher....
                                m_PrefaceVersion = (short)Common.Binary.ReadInteger(preface.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                goto default;
                            }
                        #region Unused
                        //case 0x3b03:
                        //    {
                        //        //ContentStorage
                        //        //ul
                        //        goto default;
                        //    }
                        //case 0x3b06:
                        //    {
                        //        //Identifications 
                        //        //Array of StrongRef
                        //        //8 + 16n

                        //        //Read len (8)
                        //        //Read 16 * len
                        //        goto default;
                        //    }
                        //case 0x3b07:
                        //    {
                        //        m_ObjectModelVersion = (int)Common.Binary.ReadInteger(preface.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                        //        goto default;
                        //    }
                        //case 0x3b08:
                        //    {
                        //        m_PrimaryPackage = new Guid(preface.Raw.Skip(offset).Take(tagLen).ToArray());
                        //        goto default;
                        //    }
                        //case 0x3b09:
                        //    {
                        //        //Matches m_OperationalPattern
                        //        m_PrefaceOperationalPattern = new Guid(preface.Raw.Skip(offset).Take(tagLen).ToArray());
                        //        goto default;
                        //    }
                        //case 0x3b0a: //(List of all tracks?)
                        //    {
                        //        //EssenceContainers
                        //        // This Batch shall contain all values that appear in the Essence Container Property of all File Descriptors
                        //        //(including the Multiple Descriptor) of all Top-Level File Packages in the File that describe internal Essence. 
                        //        //Batch of UL
                        //        //Read len (8)
                        //        //Read 16 * len
                        //        goto default;
                        //    }
                        #endregion
                        default: offset += tagLen; continue;
                    }

                }
            }

            if (!m_PrefaceLastModifiedDate.HasValue && !m_PrefaceVersion.HasValue) throw new InvalidOperationException("Invalid Preface Object");
        }

        string m_CompanyName, m_ProductName, m_ProductVersion, m_Platform;

        public string CompanyName
        {
            get
            {
                if (m_CompanyName == null) ParseIdentification();
                return m_CompanyName;
            }
        }
        public string ProductName
        {
            get
            {
                if (m_ProductName == null) ParseIdentification();
                return m_ProductName;
            }
        }

        public string ProductVersion
        {
            get
            {
                if (m_ProductVersion == null) ParseIdentification();
                return m_ProductVersion;
            }
        }

        public string Platform
        {
            get
            {
                if (m_Platform == null) ParseIdentification();
                return m_Platform;
            }
        }

        Guid? m_ProductUID;

        public Guid ProductUID
        {
            get
            {
                if (!m_ProductUID.HasValue) ParseIdentification();
                return m_ProductUID.Value;
            }
        }

        DateTime? m_IdentificationModificationDate;

        public DateTime IdentificationModificationDate
        {
            get
            {
                if (!m_IdentificationModificationDate.HasValue) ParseIdentification();
                return m_IdentificationModificationDate.Value;
            }
        }

        void ParseIdentification()
        {
            using (var preface = ReadObject(Identifier.Identification, true, Root.Offset + Root.Size))
            {
                int offset = 0, lenth = (int)preface.Size;

                while (offset < lenth)
                {

                    short tag = Common.Binary.Read16(preface.Raw, offset, BitConverter.IsLittleEndian),
                        tagLen = Common.Binary.Read16(preface.Raw, offset + 2, BitConverter.IsLittleEndian);

                    offset += 4;

                    switch (tag)
                    {
                        case 0x3c01:
                            {
                                m_CompanyName = Encoding.BigEndianUnicode.GetString(preface.Raw, offset, tagLen);
                                goto default;
                            }
                        case 0x3c02:
                            {
                                m_ProductName = Encoding.BigEndianUnicode.GetString(preface.Raw, offset, tagLen);
                                goto default;
                            }
                        //case 0x3c03:
                        //    {
                        //        //Product Version 10 byte type
                                  //Suprisingly NOT REQUIRED
                        //        //ALL 2 bytes

                        //        //Major
                        //        //Minor
                        //        //Patch
                        //        //Build
                        //        //Release

                        //        goto default;
                        //    }
                        case 0x3c04:
                            {
                                m_ProductVersion = Encoding.BigEndianUnicode.GetString(preface.Raw, offset, tagLen);
                                goto default;
                            }
                        case 0x3c05:
                            {
                                m_ProductUID = new Guid(preface.Raw.Skip(offset).Take(tagLen).ToArray());
                                goto default;
                            }
                        case 0x3c06:
                            {
                                m_IdentificationModificationDate = new DateTime((int)Common.Binary.ReadU16(preface.Raw, offset, BitConverter.IsLittleEndian),
                                   (int)preface.Raw[offset + 2],
                                   (int)preface.Raw[offset + 3],
                                   (int)preface.Raw[offset + 4],
                                   (int)preface.Raw[offset + 5],
                                   (int)preface.Raw[offset + 6],
                                   (int)preface.Raw[offset + 7],
                                   DateTimeKind.Utc);
                                goto default;
                            }
                        case 0x3c08:
                            {
                                m_Platform = Encoding.BigEndianUnicode.GetString(preface.Raw, offset, tagLen);
                                goto default;
                            }
                        default: offset += tagLen; continue;
                    }

                }
            }

            if (m_Platform == null) m_Platform = string.Empty;

            if (!m_IdentificationModificationDate.HasValue
                || !m_IdentificationModificationDate.HasValue
                //Important?
                || !m_ProductUID.HasValue
                || null == m_ProductName || null == m_ProductVersion || null == m_CompanyName) throw new InvalidOperationException("Invalid Preface Object");

        }

        string m_MaterialName;

        DateTime? m_MaterialCreationDate, m_MaterialModifiedDate;

        public DateTime MaterialCreationDate
        {
            get
            {
                if (!m_MaterialCreationDate.HasValue) ParseMaterialPackage();
                return m_MaterialCreationDate.Value;
            }
        }

        public DateTime MaterialModifiedDate
        {
            get
            {
                if (!m_MaterialModifiedDate.HasValue) ParseMaterialPackage();
                return m_MaterialModifiedDate.Value;
            }
        }

        void ParseMaterialPackage()
        {
            using (var preface = ReadObject(Identifier.MaterialPackage, true, Root.Offset + Root.Size))
            {
                int offset = 0, lenth = (int)preface.Size;

                while (offset < lenth)
                {

                    short tag = Common.Binary.Read16(preface.Raw, offset, BitConverter.IsLittleEndian),
                        tagLen = Common.Binary.Read16(preface.Raw, offset + 2, BitConverter.IsLittleEndian);

                    offset += 4;

                    switch (tag)
                    {
                        //case 0x4403:
                        //    {
                        //        //Array of StrongRef (Tracks)
                        //        // Read 4 byte count of list
                        //        //TrackId List (16 * count)
                        //        //Standard indicated 8 but seems 32 is right?

                        //        long count = Common.Binary.Read32(preface.Raw, offset, BitConverter.IsLittleEndian);

                        //        int localOffset = offset + 4;

                        //        List<Guid> idens = new List<Guid>();

                        //        for (int i = 0; i < count; ++i)
                        //        {
                        //            idens.Add(new Guid(preface.Raw.Skip(localOffset).Take(IdentifierSize).ToArray()));
                        //            localOffset += IdentifierSize;
                        //        }

                        //        goto default;
                        //    }
                        case 0x4402:
                            {
                                m_MaterialName = Encoding.BigEndianUnicode.GetString(preface.Raw, offset, tagLen);
                                goto default;
                            }
                        case 0x4404:
                            {
                                m_MaterialModifiedDate = new DateTime((int)Common.Binary.ReadU16(preface.Raw, offset, BitConverter.IsLittleEndian),
                                   (int)preface.Raw[offset + 2],
                                   (int)preface.Raw[offset + 3],
                                   (int)preface.Raw[offset + 4],
                                   (int)preface.Raw[offset + 5],
                                   (int)preface.Raw[offset + 6],
                                   (int)preface.Raw[offset + 7],
                                   DateTimeKind.Utc);
                                goto default;
                            }
                        case 0x4405:
                            {
                                m_MaterialCreationDate = new DateTime((int)Common.Binary.ReadU16(preface.Raw, offset, BitConverter.IsLittleEndian),
                                   (int)preface.Raw[offset + 2],
                                   (int)preface.Raw[offset + 3],
                                   (int)preface.Raw[offset + 4],
                                   (int)preface.Raw[offset + 5],
                                   (int)preface.Raw[offset + 6],
                                   (int)preface.Raw[offset + 7],
                                   DateTimeKind.Utc);
                                goto default;
                            }
                        default: offset += tagLen; continue;
                    }

                }
            }

            if (!m_MaterialModifiedDate.HasValue || !m_MaterialCreationDate.HasValue) throw new InvalidOperationException("Invalid MaterialPackage");
        }

        #region From FileInfo

        //From FileInfo

        public DateTime FileCreationTimeUtc
        {
            get { return FileInfo.CreationTimeUtc; }
        }

        public DateTime FileLastWriteTimeUtc
        {
            get { return FileInfo.LastWriteTimeUtc; }
        }

        #endregion

        public IEnumerable<Node> ReadObjects(long offset = 0, bool exact = false, params Guid[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var mxfObject in this)
            {
                if (names == null || names.Count() == 0 || (exact ? names.Contains(new Guid(mxfObject.Identifier)) : names.Contains(new Guid(mxfObject.Identifier), CompareUniquePart)))
                {
                    yield return mxfObject;
                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Node ReadObject(Guid name, bool exact = false, long offset = 0)
        {
            long positionStart = Position;

            Node result = ReadObjects(offset, exact, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public Node ReadNext()
        {
            if (Remaining < MinimumSize) return null;

            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            int sizeLength = 0;

            long length = DecodeLength(this, out sizeLength);

            if (sizeLength < MinimumSizeLength) throw new InvalidOperationException("Cannot Decode Length");

            return new Node(this, identifier, Position, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator() 
        {
            while (Remaining > MinimumSize)
            {
                Node next = ReadNext();
                if (next == null) yield break;
                yield return next;

                Skip(next.Size);
            }
        }

        List<Track> m_Tracks;

        public override IEnumerable<Track> GetTracks()
        {

            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }

            var tracks = new List<Track>();

            long position = Position;

            DateTime trackCreated = MaterialCreationDate, trackModified = MaterialModifiedDate;

            int lastTrackId = -1;

            //Get all the TimelineTrack objects
            foreach (var timelineTrackObject in ReadObjects(Root.Offset + Root.Size, false, Identifier.TimelineTrack).ToArray()) 
            {

                int offset = 0, lenth = (int)timelineTrackObject.Size, trackId = 0, trackNumber = 0;
                
                string trackName = string.Empty;

                //Essence?
                byte[] codecIndication = Utility.Empty;

                double startTime = 0, origin = 0, duration = 0, editRate = 0, rate = 0;

                while (offset < lenth)
                {

                    short tag = Common.Binary.Read16(timelineTrackObject.Raw, offset, BitConverter.IsLittleEndian), 
                        tagLen = Common.Binary.Read16(timelineTrackObject.Raw, offset + 2, BitConverter.IsLittleEndian);
                    
                    offset += 4;

                    switch (tag)
                    {
                        case 0x4801:
                            {
                                //TrackId
                                trackId = (int)Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);

                                if (lastTrackId == trackId)
                                {
                                    //Break while
                                    offset = lenth;

                                    //Indicate to skip
                                    trackNumber = 0;
                                }

                                goto default;
                            }
                        case 0x4804:
                            {

                                /*
                                 The value of the Track Number should be set to zero in all Material Package and Lower-Level Source Package
                                    Essence Tracks and in all Descriptive Metadata Tracks.
                                    Note: Some MXF encoders create files that contain non-zero Track Number Properties in Material Package Essence Tracks.
                                    Non-zero values of the Track Number Property in Essence Tracks of Material or Lower-Level Source Packages,
                                    and non-zero values of the Track Number Property in Descriptive Metadata Tracks should be treated as Dark
                                    Metadata.
                                 */

                                //TrackNumber
                                trackNumber = (int)Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);

                                //if(trackNumber < 0) //Dark MetaData?

                                //Break loop and continue to next timelineTrack
                                if (trackNumber == 0) offset = lenth;
                                
                                goto default;
                            }
                        case 0x4802:
                            {
                                //TrackName
                                trackName = Encoding.BigEndianUnicode.GetString(timelineTrackObject.Raw, offset, tagLen);
                                goto default;
                            }
                        case 0x4b01:
                            {
                                //Edit Rate in hertz
                                editRate = Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                goto default;
                            }
                        case 0x4b02:
                            {
                                //Origin 
                                //Start Time in Edit Units
                                origin = Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                goto default;
                            }
                        //case 0x4803:
                        //    {
                        //        //Sequence (Defines the Sequence Set)....
                        //        goto default;
                        //    }
                        case 0x0201:
                            {
                                //Data Definition
                                //UL, Specifies the data type of this set
                                codecIndication = timelineTrackObject.Raw.Skip(offset).Take(tagLen).ToArray();
                                goto default;
                            }
                        case 0x0202: 
                            {
                                //Duration of Sequence (in units of Edit Rate)
                                duration = Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                goto default;
                            }
                        default : offset += tagLen; continue;
                    }

                }

                //Master Timecode?
                if (trackNumber == 0) continue;

                //Required to describe track duration and start time as well as rate
                if(rate == 0 || duration == 0 || codecIndication == Utility.Empty) using (var trackTimecodeComponent = ReadObject(Identifier.TimecodeComponent, true, timelineTrackObject.Offset + timelineTrackObject.Size))
                {
                    offset = 0;

                    lenth = (int)(trackTimecodeComponent == null ? 0 : trackTimecodeComponent.Size);

                    while (offset < lenth)
                    {

                        short tag = Common.Binary.Read16(trackTimecodeComponent.Raw, offset, BitConverter.IsLittleEndian),
                            tagLen = Common.Binary.Read16(trackTimecodeComponent.Raw, offset + 2, BitConverter.IsLittleEndian);

                        offset += 4;

                        switch (tag)
                        {
                            //case 0x1501: //may not be needed? usually come out to the same thing?
                            //    {
                            //        //Start Timecode (Position Type) (8 byte)
                            //        //Converted to integer frame count from -> 00:00:00:00
                            //        double trackStart = Common.Binary.ReadInteger(trackTimecodeComponent.Raw, offset, tagLen, BitConverter.IsLittleEndian);

                            //        if (trackStart != origin) throw new InvalidOperationException("trackStart should equal origin?");

                            //        startTime = origin = trackStart;

                            //        goto default;
                            //    }
                            case 0x0201:
                                {
                                    //Data Definition
                                    //UL, Specifies the data type of this set
                                    codecIndication = timelineTrackObject.Raw.Skip(offset).Take(tagLen).ToArray();
                                    goto default;
                                }
                            case 0x0202:
                                {
                                    //Duration of Sequence (in units of Edit Rate)
                                    duration = Common.Binary.ReadInteger(timelineTrackObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case 0x1502:
                                {
                                    //Rounded Timecode Base (UInt16) (2 byte)
                                    //Nearest Integer Frames Per Second
                                    rate = (int)Common.Binary.ReadInteger(trackTimecodeComponent.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            //case 0x1503: { goto default; } // DropFrame (Boolean 1 byte) Specifies wheater timecode is drop frame, non Drop Frame = 0
                            default: offset += tagLen; continue;
                        }
                    }
                }

                //Should have been present and had a positive value by now... if they are 0 then will have to be attained by finding all frames and calulcating

                if (rate == 0) rate = 1;

                if (duration == 0) duration = 1;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                //Dont deterimine with the string, use the codecIndication
                switch (trackName)
                {
                    case PictureTrack: mediaType = Sdp.MediaType.video; break;
                    case AudioTrack: mediaType = Sdp.MediaType.audio; break;
                    case TimecodeTrack: mediaType = Sdp.MediaType.timing; break;
                    case TextTrack: mediaType = Sdp.MediaType.text; break;
                }

                int width = 0, height = 0;

                byte channels = 0, bitDepth = 0;

                //codecIndication is a UL right now
                //Get real codec 4cc and additional info from codecIndication UL

                //It seems SourceClip marks the next object as the codecIndication...

                using (var sourceClip = ReadObject(Identifier.SourceClip, true, timelineTrackObject.Offset + timelineTrackObject.Size))
                {
                    long pos = Position;

                    Position = sourceClip.Offset + sourceClip.Size;

                    codecIndication = ReadNext().Identifier;

                    Position = pos;
                }

                using (var trackCodecObject = ReadObject(new Guid(codecIndication), true, timelineTrackObject.Offset + timelineTrackObject.Size))
                {
                    offset = 0;

                    lenth = (int)(trackCodecObject == null ? 0 : trackCodecObject.Size);

                    while (offset < lenth)
                    {

                        short tag = Common.Binary.Read16(trackCodecObject.Raw, offset, BitConverter.IsLittleEndian),
                            tagLen = Common.Binary.Read16(trackCodecObject.Raw, offset + 2, BitConverter.IsLittleEndian);

                        offset += 4;

                        switch (tag)
                        {                
                            //case 0x3215: //signal standard.. 1 byte enum
                            //case 0x3001: //SampleRate //The rate of non-divisible, contiguously accessible units of the byte stream of an Essence Element (not the Essence (Pixel) sampling clock rate)
                            //case 0x3002: duration 8 byte in edit units
                            //case 0x3004 essence container...
                            //case 0x3005: ul to identify a codec compatible with this essence container...
                            //case 0x320c:
                            //    {
                            //        //Interlace or Progrssive
                            //        // 0 = fullframe
                            //        //1 = seperate fields
                            //        //2 = single field
                            //        //3 = mixed fields
                            //        //4 =  segmented frame
                            //    }
                            //case 0x3201 PictureEssenceCoding ... 16 byte ul
                            case 0x3202:
                            case 0x3204:
                                {
                                    height = (int)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case 0x3203:
                            case 0x3205:
                                {
                                    width = (int)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case 0x3d01:
                            case 0x3301:
                                {
                                    bitDepth = (byte)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case 0x3303:
                                {
                                    //Color Sitting - 0 Specifies how to compute subsampled color difference values

                                    //Not sure if this is correct
                                    bitDepth *= trackCodecObject.Raw[offset];
                                    goto default;
                                }
                            //case 0x3302:
                            //    {
                            //        //Horizontal Sub Sampling
                            //        bitDepth *= (byte)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                            //        goto default;
                            //    }
                            //case 0x3308:
                            //    {
                            //        //Vertical Sub Sampling
                            //        bitDepth *= (byte)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                            //        goto default;
                            //    }
                            //case 0x3309:
                            //    {
                            //        //Number of bits per alpha sub sample?
                            //        int AlphaSubSampl = (int)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                            //        goto default;
                            //    }
                            case 0x3d03:
                                {
                                    //Audio Sampling Rate
                                    rate = Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            //3d06 SoundEssenceCompression
                            case 0x3d07:
                                {
                                    channels = (byte)Common.Binary.ReadInteger(trackCodecObject.Raw, offset, tagLen, BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            default: offset += tagLen; continue;
                        }
                    }
                }

                //Convert codecIndication to 4cc?

                //Get sampleCount if index is available? or count all frames...

                int sampleCount = 0;

                //Todo check calulcations for duration and startTime

                Track created = new Track(timelineTrackObject, trackName, trackId, trackCreated, trackModified, sampleCount, height, width, TimeSpan.FromSeconds(startTime * editRate), TimeSpan.FromSeconds(1 / duration * 1 / editRate), rate, mediaType, codecIndication, channels, bitDepth);

                yield return created;

                tracks.Add(created);

                lastTrackId = trackId;
            }

            Position = position;

            m_Tracks = tracks;
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides either the First Header, Body or Footer ParitionPack found in the file
        /// </summary>
        public override Node Root { get { return ReadObject(Identifier.HeaderPartitionPack, false, 0); } }

        /// <summary>
        /// Provides the IndexElement if possible
        /// </summary>
        public override Node TableOfContents
        {
            get { return IndexByteCount > 0 ? ReadObject(Identifier.Index, true, Root.Offset) : null; }
        }
    }
}
