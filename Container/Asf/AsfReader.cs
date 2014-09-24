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
            Guid id = offset > 0 ? new Guid(identifier.Skip(offset).ToArray()) : new Guid(identifier);

            string result;

            if (!IdentifierLookup.TryGetValue(id, out result)) result = "Unknown";

            return result;
        }

        public AsfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public AsfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Element ReadElement(string path)
        {
            throw new NotImplementedException();
        }

        public Element ReadNext()
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

            return new Element(this, identifier, offset, length - MinimumSize, length <= Remaining);
        }

        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;

                Skip(next.Size);
            }
        }      

        public override Element Root
        {
            get
            {
                long position = Position;

                Position = 0;

                Element root = ReadNext();

                Position = position;

                return root;
            }
        }

        public override Element TableOfContents
        {
            get { return ReadElement("AsfFileConfiguration") ?? ReadElement("AsfStreamConfiguration"); }
        }

        public override IEnumerable<Track> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
