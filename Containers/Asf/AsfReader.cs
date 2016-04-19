
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Container;

namespace Media.Containers.Asf
{
    /// <summary>
    /// Represents the logic necessary to read files in the Advanced Systems Format (.asf, .wmv, .wma, .wtv[DVR_MS])
    /// </summary>
    public class AsfReader : MediaFileStream, IMediaContainer
    {
        static DateTime BaseDate = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        const int IdentifierSize = 16, LengthSize = 8, HeaderObjectReservedDataSize = 6, MinimumSize = IdentifierSize + LengthSize;

        public static class Identifier
        {
            public static readonly Guid HeaderObject = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid DataObject = new Guid("75B22636-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid SimpleIndexObject = new Guid("33000890-E5B1-11CF-89F4-00A0C90349CB");
            public static readonly Guid IndexObject = new Guid("D6E229D3-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid IndexParametersPlaceholderObject = new Guid("D9AADE20-7C17-4F9C-BC28-8555DD98E2A2");
            public static readonly Guid MediaObjectIndexObject = new Guid("FEB103F8-12AD-4C64-840F-2A1D2F7AD48C");
            public static readonly Guid TimecodeIndexObject = new Guid("3CB73FD0-0C4A-4803-953D-EDF7B6228F0C");
            public static readonly Guid FilePropertiesObject = new Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
            public static readonly Guid StreamPropertiesObject = new Guid("B7DC0791-A9B7-11CF-8EE6-00C00C205365");
            public static readonly Guid HeaderExtensionObject = new Guid("5FBF03B5-A92E-11CF-8EE3-00C00C205365");
            public static readonly Guid CodecListObject = new Guid("86D15240-311D-11D0-A3A4-00A0C90348F6");
            public static readonly Guid ScriptCommandObject = new Guid("1EFB1A30-0B62-11D0-A39B-00A0C90348F6");
            public static readonly Guid MarkerObject = new Guid("F487CD01-A951-11CF-8EE6-00C00C205365");
            public static readonly Guid BitrateMutualExclusionObject = new Guid("D6E229DC-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid ErrorCorrectionObject = new Guid("75B22635-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ContentDescriptionObject = new Guid("75B22633-668E-11CF-A6D9-00AA0062CE6C");
            public static readonly Guid ExtendedContentDescriptionObject = new Guid("D2D0A440-E307-11D2-97F0-00A0C95EA850");
            public static readonly Guid ContentBrandingObject = new Guid("2211B3FA-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid StreamBitratePropertiesObject = new Guid("7BF875CE-468D-11D1-8D82-006097C9A2B2");
            public static readonly Guid ContentEncryptionObject = new Guid("2211B3FB-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid ExtendedContentEncryptionObject = new Guid("298AE614-2622-4C17-B935-DAE07EE9289C");
            public static readonly Guid DigitalSignatureObject = new Guid("2211B3FC-BD23-11D2-B4B7-00A0C955FC6E");
            public static readonly Guid PaddingObject = new Guid("1806D474-CADF-4509-A4BA-9AABCB96AAE8");

            public static readonly Guid ExtendedStreamPropertiesObject = new Guid("14E6A5CB-C672-4332-8399-A96952065B5A");
            public static readonly Guid AdvancedMutualExclusionObject = new Guid("A08649CF-4775-4670-8A16-6E35357566CD");
            public static readonly Guid GroupMutualExclusionObject = new Guid("D1465A40-5A79-4338-B71B-E36B8FD6C249");
            public static readonly Guid StreamPrioritizationObject = new Guid("D4FED15B-88D3-454F-81F0-ED5C45999E24");
            public static readonly Guid BandwidthSharingObject = new Guid("A69609E6-517B-11D2-B6AF-00C04FD908E9");
            public static readonly Guid LanguageListObject = new Guid("7C4346A9-EFE0-4BFC-B229-393EDE415C85");
            public static readonly Guid MetadataObject = new Guid("C5F8CBEA-5BAF-4877-8467-AA8C44FA4CCA");
            public static readonly Guid MetadataLibraryObject = new Guid("44231C94-9498-49D1-A141-1D134E457054");
            public static readonly Guid IndexParametersObject = new Guid("D6E229DF-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid MediaObjectIndexParametersObject = new Guid("6B203BAD-3F11-48E4-ACA8-D7613DE2CFA7");
            public static readonly Guid TimecodeIndexParametersObject = new Guid("F55E496D-9797-4B5D-8C8B-604DFE9BFB24");
            public static readonly Guid CompatibilityObject = new Guid("26F18B5D-4584-47EC-9F5F-0E651F0452C9");
            public static readonly Guid AdvancedContentEncryptionObject = new Guid("43058533-6981-49E6-9B74-AD12CB86D58C");
            public static readonly Guid AudioMedia = new Guid("F8699E40-5B4D-11CF-A8FD-00805F5C442B");
            public static readonly Guid VideoMedia = new Guid("BC19EFC0-5B4D-11CF-A8FD-00805F5C442B");
            public static readonly Guid CommandMedia = new Guid("59DACFC0-59E6-11D0-A3AC-00A0C90348F6");
            public static readonly Guid JFIFMedia = new Guid("B61BE100-5B4E-11CF-A8FD-00805F5C442B");
            public static readonly Guid DegradableJPEGMedia = new Guid("35907DE0-E415-11CF-A917-00805F5C442B");
            public static readonly Guid FileTransferMedia = new Guid("91BD222C-F21C-497A-8B6D-5AA86BFC0185");
            public static readonly Guid BinaryMedia = new Guid("3AFB65E2-47EF-40F2-AC2C-70A90D71D343");

            public static readonly Guid ExtendedStreamTypeAudio = new Guid("31178c9d03e14528b5823df9db22f503");

            public static readonly Guid WebStreamMediaSubtype = new Guid("776257D4-C627-41CB-8F81-7AC7FF1C40CC");
            public static readonly Guid WebStreamFormat = new Guid("DA1E6B13-8359-4050-B398-388E965BF00C");

            public static readonly Guid NoErrorCorrection = new Guid("20FB5700-5B55-11CF-A8FD-00805F5C442B");
            public static readonly Guid AudioSpread = new Guid("BFC3CD50-618F-11CF-8BB2-00AA00B4E220");

            public static readonly Guid ContentEncryptionSystemWindowsMediaDRMNetworkDevices = new Guid("7A079BB6-DAA4-4e12-A5CA-91D38DC11A8D");

            public static readonly Guid Reserved1 = new Guid("ABD3D211-A9BA-11cf-8EE6-00C00C205365");
            public static readonly Guid Reserved2 = new Guid("86D15241-311D-11D0-A3A4-00A0C90348F6");
            public static readonly Guid Reserved3 = new Guid("4B1ACBE3-100B-11D0-A39B-00A0C90348F6");
            public static readonly Guid Reserved4 = new Guid("4CFEDB20-75F6-11CF-9C0F-00A0C90349CB");

            public static readonly Guid MutexLanguage = new Guid("D6E22A00-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid MutexBitrate = new Guid("D6E22A01-35DA-11D1-9034-00A0C90349BE");
            public static readonly Guid MutexUnknown = new Guid("D6E22A02-35DA-11D1-9034-00A0C90349BE");

            public static readonly Guid BandwidthSharingExclusive = new Guid("AF6060AA-5197-11D2-B6AF-00C04FD908E9");
            public static readonly Guid BandwidthSharingPartial = new Guid("AF6060AB-5197-11D2-B6AF-00C04FD908E9");

            public static readonly Guid PayloadExtensionSystemTimecode = new Guid("399595EC-8667-4E2D-8FDB-98814CE76C1E");
            public static readonly Guid PayloadExtensionSystemFileName = new Guid("E165EC0E-19ED-45D7-B4A7-25CBD1E28E9B");
            public static readonly Guid PayloadExtensionSystemContentType = new Guid("D590DC20-07BC-436C-9CF7-F3BBFBF1A4DC");
            public static readonly Guid PayloadExtensionSystemPixelAspectRatio = new Guid("1B1EE554-F9EA-4BC8-821A-376B74E4C4B8");
            public static readonly Guid PayloadExtensionSystemSampleDuration = new Guid("C6BD9450-867F-4907-83A3-C77921B733AD");
            public static readonly Guid PayloadExtensionSystemEncryptionSampleID = new Guid("6698B84E-0AFA-4330-AEB2-1C0A98D7A44D");
            public static readonly Guid PayloadExtensiondvrmstimingrepdata = new Guid("fd3cc02a06db4cfa801c7212d38745e4");
            public static readonly Guid PayloadExtensiondvrmsvidframerepdata = new Guid("dd6432cce22940db80f6d26328d2761f");
            public static readonly Guid PayloadExtensionSystemDegradableJPEG = new Guid("00E1AF06-7BEC-11D1-A582-00C04FC29CFB");
        }

        /// <summary>
        /// Holds a cache of all Fields in the Identifiers static type
        /// </summary>
        static Dictionary<Guid, string> IdentifierLookup;

        static AsfReader()
        {
            IdentifierLookup = new Dictionary<Guid, string>();

            foreach (var fieldInfo in typeof(Identifier).GetFields()) IdentifierLookup.Add((Guid)fieldInfo.GetValue(null), fieldInfo.Name);
        }


        public static string ToTextualConvention(byte[] identifier, int offset = 0)
        {
            if (identifier == null) return Media.Common.Extensions.String.StringExtensions.UnknownString;

            Guid id = offset > 0 || identifier.Length > 16 ? new Guid(identifier.Skip(offset).Take(IdentifierSize).ToArray()) : new Guid(identifier);

            string result;

            if (false == IdentifierLookup.TryGetValue(id, out result)) result = Media.Common.Extensions.String.StringExtensions.UnknownString;

            return result;
        }

        //Parse XML in ASX
        //return string[] of resources
        //FromAsx/GetResources(string)

        public AsfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public AsfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public AsfReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public AsfReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, null, bufferSize, true) { } 

        public IEnumerable<Node> ReadObjects(long offset = 0, params Guid[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var asfObject in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(new Guid(asfObject.Identifier.Take(IdentifierSize).ToArray())))
                {
                    yield return asfObject;
                    continue;
                }
            }

            Position = position;

            yield break;
        }

        //Not very useful since there aren't any rules I can find which state where StreamPropertiesObjects can appear.
        //public IEnumerable<Node> ReadObjects(long count, long offset, params Guid[] names)
        //{
        //    long position = Position;

        //    Position = offset;

        //    foreach (var asfObject in this)
        //    {
        //        count -= asfObject.TotalSize;

        //        if (names == null || names.Count() == 0 || names.Contains(new Guid(asfObject.Identifier.Take(IdentifierSize).ToArray())))
        //        {
        //            yield return asfObject;
        //        }

        //        if (count <= 0) break;
        //    }

        //    Position = position;

        //    yield break;
        //}

        public Node ReadObject(Guid name, long offset = 0)
        {
            //long positionStart = Position;

            Node result = ReadObjects(offset, name).FirstOrDefault();

            //Position = positionStart;

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
            long length = Common.Binary.Read64(lengthBytes, 0, Media.Common.Binary.IsBigEndian);

            if (length > MinimumSize) length -= MinimumSize;

            //For all objects besides the ASFHeaderObject the offset should equal the position.
            //The ASFHeaderObject is a special case because it is a "parent" Object
            if (identifier.SequenceEqual(Identifier.HeaderObject.ToByteArray()))
            {
                //Resize the identifer to encompass the reserved data
                Array.Resize(ref identifier, IdentifierSize + HeaderObjectReservedDataSize);
                
                //Take the reserved data out of the count of bytes in the node
                length -= Read(identifier, IdentifierSize, HeaderObjectReservedDataSize);
            }

            return new Node(this, identifier, LengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node next = ReadNext();

                if (next == null) yield break;
                
                yield return next;
                
                //Only skip nodes which don't have children.
                if(next.IdentifierSize == IdentifierSize) Skip(next.DataSize);
            }
        }      

        /// <summary>
        /// Returns the <see cref="Node"/> identified by <see cref="Identifier.HeaderObject"/>.
        /// The last 6 bytes of the Identifier of the returned Node contain the reserved data which preceeds the actual nodes in the data of the header.
        /// </summary>
        public override Node Root
        {
            get { return ReadObject(Identifier.HeaderObject); }
        }

        long? m_FileSize, m_NumberOfPackets, m_PlayTime, m_SendTime, m_Ignore, m_PreRoll, m_Flags, m_MinimumPacketSize, m_MaximumPacketSize, m_MaximumBitRate;

        public long FileSize
        {
            get
            {
                if (false == m_FileSize.HasValue) ParseFileProperties();
                return m_FileSize.Value;
            }
        }

        public long NumberOfPackets
        {
            get
            {
                if (false == m_NumberOfPackets.HasValue) ParseFileProperties();
                return m_NumberOfPackets.Value;
            }
        }
        public long SendTime
        {
            get
            {
                if (false == m_SendTime.HasValue) ParseFileProperties();
                return m_SendTime.Value;
            }
        }

        public long Ignore
        {
            get
            {
                if (false == m_Ignore.HasValue) ParseFileProperties();
                return m_Ignore.Value;
            }
        }

        public long Flags
        {
            get
            {
                if (false == m_Flags.HasValue) ParseFileProperties();
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
                if (false == m_MinimumPacketSize.HasValue) ParseFileProperties();
                return m_MinimumPacketSize.Value;
            }
        }

        public long MaximumPacketSize
        {
            get
            {
                if (false == m_MaximumPacketSize.HasValue) ParseFileProperties();
                return m_MaximumPacketSize.Value;
            }
        }

        public long MaximumBitRate
        {
            get
            {
                if (false == m_MaximumBitRate.HasValue) ParseFileProperties();
                return m_MaximumBitRate.Value;
            }
        }

        public TimeSpan PlayTime
        {
            get
            {
                if (false == m_PlayTime.HasValue) ParseFileProperties();
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
                if (false == m_PreRoll.HasValue) ParseFileProperties();
                return TimeSpan.FromMilliseconds((double)m_PreRoll.Value / (Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond));
            }
        }

        DateTime? m_Created, m_Modified;


        public DateTime Created
        {
            get
            {
                if (false == m_Created.HasValue) ParseFileProperties();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (false == m_Modified.HasValue) ParseFileProperties();
                return m_Modified.Value;
            }
        }

        void ParseFileProperties()
        {
            using (var fileProperties = ReadObject(Identifier.FilePropertiesObject, Root.DataOffset))
            {

                if (fileProperties == null) throw new InvalidOperationException("FilePropertiesObject not found");

                //FileId
                int offset = IdentifierSize;

                //FileSize 64
                m_FileSize = (long)Common.Binary.ReadU64(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 8;

                //Created 64
                m_Created = BaseDate.AddTicks((long)Common.Binary.ReadU64(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian));
                offset += 8;
                
                //NumberOfPackets 64
                m_NumberOfPackets = (long)Common.Binary.ReadU64(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 8;

                //PlayTime 64
                m_PlayTime = (long)Common.Binary.ReadU64(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 8;

                //SendTime 64
                m_SendTime = (long)Common.Binary.ReadU64(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 8;

                //PreRoll 32
                m_PreRoll = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;

                //Ignore 32
                m_Ignore = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;

                //Flags 32
                m_Flags = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;
                
                //MinimumPacketSize 32
                m_MinimumPacketSize = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;

                //MaximumPacketSize 32
                m_MaximumPacketSize = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;

                //MaximumBitRate 32
                m_MaximumBitRate = (long)Common.Binary.ReadU32(fileProperties.Data, offset, Media.Common.Binary.IsBigEndian);
                offset += 4;
                
                //Any more data it belongs to some kind of extension...
                //if(offset < fileProperties.DataSize)
            }

            m_Modified = FileInfo.LastWriteTimeUtc;
        }

        string m_Title, m_Author, m_Copyright, m_Comment, m_Rating;

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

        public string Rating
        {
            get
            {
                if (m_Rating == null) ParseContentDescription();
                return m_Rating;
            }
        }

        void ParseContentDescription()
        {
            using (var contentDescription = ReadObject(Identifier.ContentDescriptionObject, Root.DataOffset))
            {
                if(contentDescription != null && contentDescription.DataSize > 2)
                {
                    int offset = 2, len = Common.Binary.Read16(contentDescription.Data, offset, Media.Common.Binary.IsBigEndian), remaining = (int)contentDescription.DataSize;
                    remaining -= 2;

                    if (remaining > 0 && len > 0)
                    {
                        len = Media.Common.Extensions.Math.MathExtensions.Clamp(len, 0, remaining);
                        m_Title = Encoding.ASCII.GetString(contentDescription.Data, offset, len);
                        offset += len;
                        remaining -= len;
                    }
                    else m_Title = string.Empty;

                    if (remaining > 1)
                    {
                        len = Common.Binary.Read16(contentDescription.Data, offset, Media.Common.Binary.IsBigEndian);
                        offset += 2;
                        remaining -= 2;
                    }
                    else len = 0;

                    if (remaining > 0 && len > 0)
                    {
                        len = Media.Common.Extensions.Math.MathExtensions.Clamp(len, 0, (int)(contentDescription.DataSize - offset));
                        m_Author = Encoding.ASCII.GetString(contentDescription.Data, offset, len);
                        offset += len;
                        remaining -= len;
                    }
                    else m_Author = string.Empty;

                    if (remaining > 1)
                    {
                        len = Common.Binary.Read16(contentDescription.Data, offset, Media.Common.Binary.IsBigEndian);
                        offset += 2;
                        remaining -= 2;
                    }
                    else len = 0; 

                    if (remaining > 0 && len > 0)
                    {
                        len = Media.Common.Extensions.Math.MathExtensions.Clamp(len, 0, (int)(contentDescription.DataSize - offset));
                        m_Copyright = Encoding.ASCII.GetString(contentDescription.Data, offset, len);
                        offset += len;
                        remaining -= len;
                    }
                    else m_Copyright = string.Empty;

                    if (remaining > 1)
                    {
                        len = Common.Binary.Read16(contentDescription.Data, offset, Media.Common.Binary.IsBigEndian);
                        offset += 2;
                        remaining -= 2;
                    }
                    else len = 0; 

                    if (len > 0)
                    {
                        len = Media.Common.Extensions.Math.MathExtensions.Clamp(len, 0, (int)(contentDescription.DataSize - offset));
                        m_Comment = Encoding.ASCII.GetString(contentDescription.Data, offset, len);
                        offset += len;
                        offset += len;
                        remaining -= len;
                    }
                    else m_Comment = string.Empty;

                    if (remaining > 1)
                    {
                        len = Common.Binary.Read16(contentDescription.Data, offset, Media.Common.Binary.IsBigEndian);
                        offset += 2;
                        remaining -= 2;
                    }
                    else len = 0;

                    if (remaining > 0 && len > 0)
                    {
                        len = Media.Common.Extensions.Math.MathExtensions.Clamp(len, 0, (int)(contentDescription.DataSize - offset));
                        m_Rating = Encoding.ASCII.GetString(contentDescription.Data, offset, len);
                        offset += len;
                    }
                    else m_Rating = string.Empty;
                }
                else m_Title = m_Author = m_Copyright = m_Comment = m_Rating = string.Empty;
            }
        }

        //s ->keylen defines protection.

        public override Node TableOfContents
        {
            get { return ReadObject(Identifier.FilePropertiesObject, Root.DataOffset); }
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

            foreach (var asfObject in ReadObjects(Root.DataOffset, Identifier.StreamPropertiesObject).ToArray())
            {
                ulong sampleCount = 0, startTime = (ulong)PreRoll.TotalMilliseconds, timeScale = 1, duration = (ulong)Duration.TotalMilliseconds, width = 0, height = 0, rate = 0;

                string trackName = string.Empty;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Media.Common.MemorySegment.EmptyBytes;

                byte channels = 0, bitDepth = 0;

                int offset = 0;

                //Would keep as GUID but can't switch on the Guid
                string mediaTypeName = ToTextualConvention(asfObject.Data, offset);//, noCorrection;

                offset += IdentifierSize * 2;

                //stream.Read(buffer, 0, IdentifierSize);

                //noCorrection = ToTextualConvention(buffer, 0);

                //if (noCorrection != "ASFNoErrorCorrection") throw new InvalidOperationException("Invalid ASFStreamPropertiesObject");

                //TimeOffset
                startTime = Common.Binary.ReadU64(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 8;

                int typeSpecDataLen, eccDataLen;

                typeSpecDataLen = Common.Binary.Read32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                eccDataLen = Common.Binary.Read32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                short flags = Common.Binary.Read16(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 2;

                trackId = (flags & 0x7f);

                bool encrypted = (flags & 0x8000) == 1;

                //Reserved
                offset += 4;

                if (asfObject.DataSize - offset < eccDataLen + typeSpecDataLen) throw new InvalidOperationException("Invalid ASFStreamPropertiesObject");

                //Position At TypeSpecificData

                switch (mediaTypeName)
                {
                    case "VideoMedia":
                        {
                            //Read 32
                            //Read 32
                            //Read 8
                            //Read 16 SizeX
                            //Read 32 BytesPer BitmapInfoHeader
                            offset += 15;

                            mediaType = Sdp.MediaType.video;

                            //Read 32 Width
                            width = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 6;


                            //Maybe...
                            //Read 16 panes

                            //Read 16 BitDepth
                            bitDepth = (byte)Common.Binary.ReadU16(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 2;

                            codecIndication = asfObject.Data.Skip(offset).Take(4).ToArray();

                            offset += 4;

                            //32 image_size
                            //32 horizontal_pixels_per_meter
                            //32 vertical_pixels_per_meter
                            //32 used_colors_count
                            //32 important_colors_count

                            //Codec Specific Data (Varies)

                            break;
                        }
                    case "AudioMedia":
                        {
                            mediaType = Sdp.MediaType.audio;
                            //WaveHeader ... Used also in RIFF
                            //16 format_tag
                            codecIndication = asfObject.Data.Skip(offset).Take(2).ToArray();
                            //Expand Codec Indication based on iD?

                            offset += 2;

                            //16 number_channels
                            channels = (byte)Common.Binary.ReadU16(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);
                            offset += 2;
                            
                            //32 samples_per_second
                            rate = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 4;

                            //32 average_bytes_per_second
                            //16 block_alignment
                            offset += 6;

                            //16 bits_per_sample
                            bitDepth = (byte)Common.Binary.ReadU16(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);
                            break;
                        }
                    case "CommandMedia":
                        {
                            mediaType = Sdp.MediaType.control;
                            break;
                        }
                    case "DegradableJPEGMedia":
                        {
                            //Read 32 Width
                            width = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 4;

                            //Reserved32

                            mediaType = Sdp.MediaType.video;
                            codecIndication = Encoding.UTF8.GetBytes("JFIF");
                            break;
                        }
                    case "JFIFMedia":
                        {
                            //Read 32 Width
                            width = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

                            offset += 4;

                            //Read 32 Height
                            height = Common.Binary.ReadU32(asfObject.Data, offset, Media.Common.Binary.IsBigEndian);

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
                    case "FileTransferMedia":
                    case "BinaryData":
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
                    case "TextMedia":
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

                Track created = new Track(asfObject, trackName, trackId, Created, Modified, (int)sampleCount, (int)height, (int)width, TimeSpan.FromMilliseconds(startTime / timeScale), TimeSpan.FromMilliseconds(duration), 
                    //Frames Per Seconds
                    // duration is in milliseconds, converted to seconds, scaled
                    mediaType == Sdp.MediaType.video ?
                    duration * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
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

        public override string ToTextualConvention(Node node)
        {
            if (node.Master.Equals(this)) return AsfReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
