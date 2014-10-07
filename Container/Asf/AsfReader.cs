using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Asf
{
    /// <summary>
    /// Represents the logic necessary to read files in the Advanced Systems Format (.asf)
    /// </summary>
    public class AsfReader : MediaFileStream, IMediaContainer
    {

        static DateTime BaseDate = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        const int IdentifierSize = 16, LengthSize = 8, MinimumSize = IdentifierSize + LengthSize;

        public static class Identifiers
        {
            public static readonly Guid ASFHeaderObject = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ASFDataObject = new Guid("75B22636-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ASFSimpleIndexObject = new Guid("33000890-E5B1-11CF-89F4-00A0C90349CB");
            public static readonly Guid ASFIndexObject = new Guid("D6E229D3-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ASFIndexParametersPlaceholderObject = new Guid("D9AADE20-7C17-4F9C-BC28-8555DD98E2A2");
            public static readonly Guid ASFMediaObjectIndexObject = new Guid("FEB103F8-12AD-4C64-840F-2A1D2F7AD48C");
            public static readonly Guid ASFTimecodeIndexObject = new Guid("3CB73FD0-0C4A-4803-953D-EDF7B6228F0C");
            public static readonly Guid ASFFilePropertiesObject = new Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
            public static readonly Guid ASFStreamPropertiesObject = new Guid("B7DC0791-A9B7-11CF-8EE6-00C00C205365");
            public static readonly Guid ASFHeaderExtensionObject = new Guid("5FBF03B5-A92E-11CF-8EE3-00C00C205365");
            public static readonly Guid ASFCodecListObject = new Guid("86D15240-311D-11D0-A3A4-00A0C90348F6");
            public static readonly Guid ASFScriptCommandObject = new Guid("1EFB1A30-0B62-11D0-A39B-00A0C90348F6");
            public static readonly Guid ASFMarkerObject = new Guid("F487CD01-A951-11CF-8EE6-00C00C205365");
            public static readonly Guid ASFBitrateMutualExclusionObject = new Guid("D6E229DC-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ASFErrorCorrectionObject = new Guid("75B22635-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ASFContentDescriptionObject = new Guid("75B22633-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ASFExtendedContentDescriptionObject = new Guid("D2D0A440-E307-11D2-97F0-00A0C95EA850");
            public static readonly Guid ASFContentBrandingObject = new Guid("2211B3FA-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid ASFStreamBitratePropertiesObject = new Guid("7BF875CE-468D-11D1-8D82-006097C9A2B2");
            public static readonly Guid ASFContentEncryptionObject = new Guid("2211B3FB-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid ASFExtendedContentEncryptionObject = new Guid("298AE614-2622-4C17-B935-DAE07EE9289C");
            public static readonly Guid ASFDigitalSignatureObject = new Guid("2211B3FC-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid ASFPaddingObject = new Guid("1806D474-CADF-4509-A4BA-9AABCB96AAE8");

            public static readonly Guid ASFExtendedStreamPropertiesObject = new Guid("14E6A5CB-C672-4332-8399-A96952065B5A");
            public static readonly Guid ASFAdvancedMutualExclusionObject = new Guid("A08649CF-4775-4670-8A16-6E35357566CD");
            public static readonly Guid ASFGroupMutualExclusionObject = new Guid("D1465A40-5A79-4338-B71B-E36B8FD6C249");
            public static readonly Guid ASFStreamPrioritizationObject = new Guid("D4FED15B-88D3-454F-81F0-ED5C45999E24");
            public static readonly Guid ASFBandwidthSharingObject = new Guid("A69609E6-517B-11D2-B6AF-00C04FD908E9");
            public static readonly Guid ASFLanguageListObject = new Guid("7C4346A9-EFE0-4BFC-B229-393EDE415C85");
            public static readonly Guid ASFMetadataObject = new Guid("C5F8CBEA-5BAF-4877-8467-AA8C44FA4CCA");
            public static readonly Guid ASFMetadataLibraryObject = new Guid("44231C94-9498-49D1-A141-1D134E457054");
            public static readonly Guid ASFIndexParametersObject = new Guid("D6E229DF-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ASFMediaObjectIndexParametersObject = new Guid("6B203BAD-3F11-48E4-ACA8-D7613DE2CFA7");
            public static readonly Guid ASFTimecodeIndexParametersObject = new Guid("F55E496D-9797-4B5D-8C8B-604DFE9BFB24");
            public static readonly Guid ASFCompatibilityObject = new Guid("26F18B5D-4584-47EC-9F5F-0E651F0452C9");
            public static readonly Guid ASFAdvancedContentEncryptionObject = new Guid("43058533-6981-49E6-9B74-AD12CB86D58C");
            public static readonly Guid ASFAudioMedia = new Guid("F8699E40-5B4D-11CF-A8FD-00805F5C442B");
            public static readonly Guid ASFVideoMedia = new Guid("BC19EFC0-5B4D-11CF-A8FD-00805F5C442B");
            public static readonly Guid ASFCommandMedia = new Guid("59DACFC0-59E6-11D0-A3AC-00A0C90348F6");
            public static readonly Guid ASFJFIFMedia = new Guid("B61BE100-5B4E-11CF-A8FD-00805F5C442B");
            public static readonly Guid ASFDegradableJPEGMedia = new Guid("35907DE0-E415-11CF-A917-00805F5C442B");
            public static readonly Guid ASFFileTransferMedia = new Guid("91BD222C-F21C-497A-8B6D-5AA86BFC0185");
            public static readonly Guid ASFBinaryMedia = new Guid("3AFB65E2-47EF-40F2-AC2C-70A90D71D343");

            public static readonly Guid ASFExtendedStreamTypeAudio = new Guid("31178c9d03e14528b5823df9db22f503");

            public static readonly Guid ASFWebStreamMediaSubtype = new Guid("776257D4-C627-41CB-8F81-7AC7FF1C40CC");
            public static readonly Guid ASFWebStreamFormat = new Guid("DA1E6B13-8359-4050-B398-388E965BF00C");

            public static readonly Guid ASFNoErrorCorrection = new Guid("20FB5700-5B55-11CF-A8FD-00805F5C442B");
            public static readonly Guid ASFAudioSpread = new Guid("BFC3CD50-618F-11CF-8BB2-00AA00B4E220");

            public static readonly Guid ASFContentEncryptionSystemWindowsMediaDRMNetworkDevices = new Guid("7A079BB6-DAA4-4e12-A5CA-91D38DC11A8D");

            public static readonly Guid ASFReserved1 = new Guid("ABD3D211-A9BA-11cf-8EE6-00C00C205365");
            public static readonly Guid ASFReserved2 = new Guid("86D15241-311D-11D0-A3A4-00A0C90348F6");
            public static readonly Guid ASFReserved3 = new Guid("4B1ACBE3-100B-11D0-A39B-00A0C90348F6");
            public static readonly Guid ASFReserved4 = new Guid("4CFEDB20-75F6-11CF-9C0F-00A0C90349CB");

            public static readonly Guid ASFMutexLanguage = new Guid("D6E22A00-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ASFMutexBitrate = new Guid("D6E22A01-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ASFMutexUnknown = new Guid("D6E22A02-35DA-11D1-9034-00A0C90349BE");

            public static readonly Guid ASFBandwidthSharingExclusive = new Guid("AF6060AA-5197-11D2-B6AF-00C04FD908E9");
            public static readonly Guid ASFBandwidthSharingPartial = new Guid("AF6060AB-5197-11D2-B6AF-00C04FD908E9");

            public static readonly Guid ASFPayloadExtensionSystemTimecode = new Guid("399595EC-8667-4E2D-8FDB-98814CE76C1E");
            public static readonly Guid ASFPayloadExtensionSystemFileName = new Guid("E165EC0E-19ED-45D7-B4A7-25CBD1E28E9B");
            public static readonly Guid ASFPayloadExtensionSystemContentType = new Guid("D590DC20-07BC-436C-9CF7-F3BBFBF1A4DC");
            public static readonly Guid ASFPayloadExtensionSystemPixelAspectRatio = new Guid("1B1EE554-F9EA-4BC8-821A-376B74E4C4B8");
            public static readonly Guid ASFPayloadExtensionSystemSampleDuration = new Guid("C6BD9450-867F-4907-83A3-C77921B733AD");
            public static readonly Guid ASFPayloadExtensionSystemEncryptionSampleID = new Guid("6698B84E-0AFA-4330-AEB2-1C0A98D7A44D");
            public static readonly Guid ASFPayloadExtensiondvrmstimingrepdata = new Guid("fd3cc02a06db4cfa801c7212d38745e4");
            public static readonly Guid ASFPayloadExtensiondvrmsvidframerepdata = new Guid("dd6432cce22940db80f6d26328d2761f");
            public static readonly Guid ASFPayloadExtensionSystemDegradableJPEG = new Guid("00E1AF06-7BEC-11D1-A582-00C04FC29CFB");
        }

        /// <summary>
        /// Holds a cache of all Fields in the Identifiers static type
        /// </summary>
        static Dictionary<Guid, string> IdentifierLookup;

        static AsfReader()
        {
            IdentifierLookup = new Dictionary<Guid, string>();

            foreach (var fieldInfo in typeof(Identifiers).GetFields()) IdentifierLookup.Add((Guid)fieldInfo.GetValue(null), fieldInfo.Name);
        }


        public static string ToTextualConvention(byte[] identifier, int offset = 0)
        {
            if (identifier == null) return "Unknown";

            Guid id = offset > 0 || identifier.Length > 16 ? new Guid(identifier.Skip(offset).Take(IdentifierSize).ToArray()) : new Guid(identifier);

            string result;

            if (!IdentifierLookup.TryGetValue(id, out result)) result = "Unknown";

            return result;
        }

        public AsfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public AsfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Node> ReadObjects(long offset = 0, params Guid[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var box in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(new Guid(box.Identifier)))
                {
                    yield return box;
                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Node ReadObject(Guid name, long offset = 0)
        {
            long positionStart = Position;

            Node result = ReadObjects(offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public Node ReadNext()
        {
            if (Remaining < MinimumSize) return null;

            long offset = Position;

            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            byte[] lengthBytes = new byte[LengthSize];

            Read(lengthBytes, 0, LengthSize);

            //24 bytes

            //Length in LittleEndian?
            long length = Common.Binary.Read64(lengthBytes, 0, !BitConverter.IsLittleEndian);

            if (length > MinimumSize) length -= MinimumSize;

            //For all objects besides the ASFHeaderObject the offset should equal the position.
            //The ASFHeaderObject is a special case because it is a "parent" Object
            if(!identifier.SequenceEqual(Identifiers.ASFHeaderObject.ToByteArray())) offset = Position;

            return new Node(this, identifier, offset, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node next = ReadNext();
                if (next == null) yield break;
                yield return next;

                //Because the ASFHeaderObject is a parent object it must be parsed for children
                if (next.Identifier.SequenceEqual(Identifiers.ASFHeaderObject.ToByteArray()))
                {
                    //Int 32 and two reserved bytes
                    Skip(6);
                    continue; //Parsing
                }
                //Skip past object data
                Skip(next.Size);
            }
        }      

        public override Node Root
        {
            get { return ReadObject(Identifiers.ASFHeaderObject, 0); }
        }

        long? m_FileSize, m_NumberOfPackets, m_PlayTime, m_SendTime, m_Ignore, m_PreRoll, m_Flags, m_MinimumPacketSize, m_MaximumPacketSize, m_MaximumBitRate;

        public long FileSize
        {
            get
            {
                if (!m_FileSize.HasValue) ParseFileProperties();
                return m_FileSize.Value;
            }
        }

        public long NumberOfPackets
        {
            get
            {
                if (!m_NumberOfPackets.HasValue) ParseFileProperties();
                return m_NumberOfPackets.Value;
            }
        }
        public long SendTime
        {
            get
            {
                if (!m_SendTime.HasValue) ParseFileProperties();
                return m_SendTime.Value;
            }
        }

        public long Ignore
        {
            get
            {
                if (!m_Ignore.HasValue) ParseFileProperties();
                return m_Ignore.Value;
            }
        }

        public long Flags
        {
            get
            {
                if (!m_Flags.HasValue) ParseFileProperties();
                return m_Flags.Value;
            }
        }

        public bool IsBroadcast
        {
            get { return (Flags & 1) > 0; }
        }

        public bool IsSeekable
        {
            get { return (Flags & 2) > 0; }
        }

        public long MinimumPacketSize
        {
            get
            {
                if (!m_MinimumPacketSize.HasValue) ParseFileProperties();
                return m_MinimumPacketSize.Value;
            }
        }

        public long MaximumPacketSize
        {
            get
            {
                if (!m_MaximumPacketSize.HasValue) ParseFileProperties();
                return m_MaximumPacketSize.Value;
            }
        }

        public long MaximumBitRate
        {
            get
            {
                if (!m_MaximumBitRate.HasValue) ParseFileProperties();
                return m_MaximumBitRate.Value;
            }
        }

        public TimeSpan PlayTime
        {
            get
            {
                if (!m_PlayTime.HasValue) ParseFileProperties();
                return TimeSpan.FromTicks(m_PlayTime.Value);
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return PlayTime - PreRoll;
            }
        }

        public TimeSpan PreRoll
        {
            get
            {
                if (!m_PreRoll.HasValue) ParseFileProperties();
                return TimeSpan.FromMilliseconds((double)m_PreRoll.Value / (Utility.NanosecondsPerSecond / Utility.MicrosecondsPerMillisecond));
            }
        }

        DateTime? m_Created, m_Modified;


        public DateTime Created
        {
            get
            {
                if (!m_Created.HasValue) ParseFileProperties();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (!m_Modified.HasValue) ParseFileProperties();
                return m_Modified.Value;
            }
        }

        void ParseFileProperties()
        {
            using (var fileProperties = ReadObject(Identifiers.ASFFilePropertiesObject, Root.Offset))
            {
                using (var stream = fileProperties.Data)
                {
                    //ASFFilePropertiesObject, Len
                    
                    //FileId
                    stream.Position += IdentifierSize;

                    byte[] buffer = new byte[8];

                    //FileSize 64
                    stream.Read(buffer, 0, 8);
                    m_FileSize = (long)Common.Binary.ReadU64(buffer, 0, !BitConverter.IsLittleEndian);

                    //Created 64
                    stream.Read(buffer, 0, 8);
                    m_Created = BaseDate.AddTicks((long)Common.Binary.ReadU64(buffer, 0, !BitConverter.IsLittleEndian));

                    //NumberOfPackets 64
                    stream.Read(buffer, 0, 8);
                    m_NumberOfPackets = (long)Common.Binary.ReadU64(buffer, 0, !BitConverter.IsLittleEndian);

                    //PlayTime 64
                    stream.Read(buffer, 0, 8);
                    m_PlayTime = (long)Common.Binary.ReadU64(buffer, 0, !BitConverter.IsLittleEndian);

                    //SendTime 64
                    stream.Read(buffer, 0, 8);
                    m_SendTime = (long)Common.Binary.ReadU64(buffer, 0, !BitConverter.IsLittleEndian);

                    //PreRoll 32
                    stream.Read(buffer, 0, 8);
                    m_PreRoll = (long)Common.Binary.ReadU32(buffer, 0, !BitConverter.IsLittleEndian);

                    //Ignore 32
                    m_Ignore = (long)Common.Binary.ReadU32(buffer, 4, !BitConverter.IsLittleEndian);

                    //Flags 32
                    stream.Read(buffer, 0, 8);
                    m_Flags = (long)Common.Binary.ReadU32(buffer, 0, !BitConverter.IsLittleEndian);

                    //MinimumPacketSize 32
                    m_MinimumPacketSize = (long)Common.Binary.ReadU32(buffer, 4, !BitConverter.IsLittleEndian);

                    //MaximumPacketSize 32
                    stream.Read(buffer, 0, 8);
                    m_MaximumPacketSize = (long)Common.Binary.ReadU32(buffer, 0, !BitConverter.IsLittleEndian);

                    //MaximumBitRate 32
                    m_MaximumBitRate = (long)Common.Binary.ReadU32(buffer, 4, !BitConverter.IsLittleEndian);

                    //Any more data it belongs to some kind of extension...
                }
            }

            m_Modified = FileInfo.LastWriteTimeUtc;
        }

        string m_Title, m_Author, m_Copyright, m_Comment;

        public string Title
        {
            get
            {
                if (m_Title == null) ParseContentDescription();
                return m_Title;
            }
        }

        public string Author
        {
            get
            {
                if (m_Author == null) ParseContentDescription();
                return m_Author;
            }
        }

        public string Copyright
        {
            get
            {
                if (m_Copyright == null) ParseContentDescription();
                return m_Copyright;
            }
        }

        public string Comment
        {
            get
            {
                if (m_Comment == null) ParseContentDescription();
                return m_Comment;
            }
        }

        void ParseContentDescription()
        {
            using (var contentDescription = ReadObject(Identifiers.ASFContentDescriptionObject, Root.Offset))
            {
                if(contentDescription != null) using (var stream = contentDescription.Data)
                {

                    //ASFContentDescriptionObject, Len
                    //stream.Position += MinimumSize;

                    byte[] buffer = new byte[32];

                    stream.Read(buffer, 0, 4);
                    int len1 = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, 4);
                    int len2 = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, 4);
                    int len3 = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, 4);
                    int len4 = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, len1);
                    m_Title = Encoding.ASCII.GetString(buffer, 0, len1);

                    stream.Read(buffer, 0, len2);
                    m_Author = Encoding.ASCII.GetString(buffer, 0, len1);

                    stream.Read(buffer, 0, len3);
                    m_Copyright = Encoding.ASCII.GetString(buffer, 0, len1);

                    stream.Read(buffer, 0, len4);
                    m_Comment = Encoding.ASCII.GetString(buffer, 0, len1);
                }
            }
            if (m_Title == null) m_Title = string.Empty;
            if (m_Author == null) m_Author = string.Empty;
            if (m_Copyright == null) m_Copyright = string.Empty;
            if (m_Comment == null) m_Comment = string.Empty;
        }

        //s ->keylen defines protection.

        public override Node TableOfContents
        {
            get { return ReadObject(Identifiers.ASFFilePropertiesObject, Root.Offset); }
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

            int trackId = 0;

            byte[] buffer = new byte[32];

            foreach (var element in ReadObjects(Root.Offset, Identifiers.ASFStreamPropertiesObject).ToArray())
            {
                ulong sampleCount = 0, startTime = (ulong)PreRoll.TotalMilliseconds, timeScale = 1, duration = (ulong)Duration.TotalMilliseconds, width = 0, height = 0, rate = 0;

                string trackName = string.Empty;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Utility.Empty;

                byte channels = 0, bitDepth = 0;

                int offset = 0;

                string mediaTypeName = ToTextualConvention(element.Raw, offset);//, noCorrection;

                offset += IdentifierSize * 2;

                //stream.Read(buffer, 0, IdentifierSize);

                //noCorrection = ToTextualConvention(buffer, 0);

                //if (noCorrection != "ASFNoErrorCorrection") throw new InvalidOperationException("Invalid ASFStreamPropertiesObject");

                //TimeOffset
                startTime = Common.Binary.ReadU64(element.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 8;

                int typeSpecDataLen, eccDataLen;

                typeSpecDataLen = Common.Binary.Read32(element.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                eccDataLen = Common.Binary.Read32(element.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                short flags = Common.Binary.Read16(element.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 2;

                trackId = (flags & 0x7f);

                bool encrypted = (flags & 0x8000) == 1;

                //Reserved
                offset += 4;

                if (element.Size - offset < eccDataLen + typeSpecDataLen) throw new InvalidOperationException("Invalid ASFStreamPropertiesObject");

                //Position At TypeSpecificData

                switch (mediaTypeName)
                {
                    case "ASFVideoMedia":
                        {
                            //Read 32
                            //Read 32
                            //Read 8
                            //Read 16 SizeX
                            //Read 32 SizeOf BitmapInfoHeader
                            offset += 15;

                            mediaType = Sdp.MediaType.video;

                            //Read 32 Width
                            width = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 6;


                            //Maybe...
                            //Read 16 panes

                            //Read 16 BitDepth
                            bitDepth = (byte)Common.Binary.ReadU16(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 2;

                            codecIndication = element.Raw.Skip(offset).Take(4).ToArray();

                            offset += 4;

                            //32 image_size
                            //32 horizontal_pixels_per_meter
                            //32 vertical_pixels_per_meter
                            //32 used_colors_count
                            //32 important_colors_count

                            //Codec Specific Data (Varies)

                            break;
                        }
                    case "ASFAudioMedia":
                        {
                            mediaType = Sdp.MediaType.audio;
                            //WaveHeader ... Used also in RIFF
                            //16 format_tag
                            codecIndication = element.Raw.Skip(offset).Take(2).ToArray();
                            //Expand Codec Indication based on iD?

                            offset += 2;

                            //16 number_channels
                            channels = (byte)Common.Binary.ReadU16(element.Raw, offset, !BitConverter.IsLittleEndian);
                            offset += 2;
                            
                            //32 samples_per_second
                            rate = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //32 average_bytes_per_second
                            //16 block_alignment
                            offset += 6;

                            //16 bits_per_sample
                            bitDepth = (byte)Common.Binary.ReadU16(element.Raw, offset, !BitConverter.IsLittleEndian);
                            break;
                        }
                    case "ASFCommandMedia":
                        {
                            mediaType = Sdp.MediaType.control;
                            break;
                        }
                    case "ASFDegradableJPEGMedia":
                        {
                            //Read 32 Width
                            width = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //Reserved32

                            mediaType = Sdp.MediaType.video;
                            codecIndication = Encoding.UTF8.GetBytes("JFIF");
                            break;
                        }
                    case "ASFJFIFMedia":
                        {
                            //Read 32 Width
                            width = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(element.Raw, offset, !BitConverter.IsLittleEndian);

                            offset += 4;

                            //Reserved16

                            //Reserved16

                            //Reserved16

                            //InterchangeDataLength
                            //InterchangeData

                            mediaType = Sdp.MediaType.video;
                            codecIndication = Encoding.UTF8.GetBytes("JFIF");

                            mediaType = Sdp.MediaType.video;
                            codecIndication = Encoding.UTF8.GetBytes("JFIF");
                            break;
                        }
                    case "ASFFileTransferMedia":
                    case "ASFBinaryData":
                        {
                            // Web Stream Format Data Size 16
                            // Fixed Sample HEader Size 16
                            // Version Number 16
                            // Reserved 16

                            //OR

                            //Total Header Length 16
                            //Part Number 16
                            //Total Part Count 16
                            //Sample Type 16

                            //URL String (Varies)
                            mediaType = Sdp.MediaType.data;
                            break;
                        }
                    case "ASFTextMedia":
                        {

                            //Name,Value pairs
                            mediaType = Sdp.MediaType.text;
                            break;
                        }
                }

                //Extension?
                //stream.Position += 16;

                //if (!IsBroadcast)
                //{
                //    //May have to adjust time...
                //    //m_PlayTime /  (10000000 / 1000) - m_StartTime;
                //}

                //Name comes from MetaData?

                //TODO Get Sample Count....

                //Parse Index....

                Track created = new Track(element, trackName, trackId, Created, Modified, (int)sampleCount, (int)height, (int)width, TimeSpan.FromMilliseconds(startTime / timeScale), TimeSpan.FromMilliseconds(duration), 
                    //Frames Per Seconds
                    // duration is in milliseconds, converted to seconds, scaled by 100 nanosecond units
                    mediaType == Sdp.MediaType.video ? 
                    duration * Utility.MicrosecondsPerMillisecond / Utility.NanosecondsPerSecond * Utility.MicrosecondsPerMillisecond
                    : 
                    rate, 
                    ///
                    mediaType, codecIndication, channels, bitDepth);

                yield return created;

                tracks.Add(created);
            }

            m_Tracks = tracks;

            Position = position;
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
