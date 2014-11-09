using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Matroska
{
    /// <summary>
    /// Public enumeration listing the possible EBML element identifiers.
    /// </summary>
    public enum Identifier
    {
        /// <summary>
        /// Indicates an EBML Header element.
        /// </summary>
        EBMLHeader = 0x1A45DFA3,

        /// <summary>
        /// Indicates an EBML Version element.
        /// </summary>
        EBMLVersion = 0x4286,

        /// <summary>
        /// Indicates an EBML Read Version element.
        /// </summary>
        EBMLReadVersion = 0x42F7,

        /// <summary>
        /// Indicates an EBML Max ID Length element.
        /// </summary>
        EBMLMaxIDLength = 0x42F2,

        /// <summary>
        /// Indicates an EBML Max Size Length element.
        /// </summary>
        EBMLMaxSizeLength = 0x42F3,

        /// <summary>
        /// Indicates an EBML Doc Type element.
        /// </summary>
        EBMLDocType = 0x4282,

        /// <summary>
        /// Indicates an EBML Doc Type Version element.
        /// </summary>
        EBMLDocTypeVersion = 0x4287,

        /// <summary>
        /// Indicates an EBML Doc Type Read Version element.
        /// </summary>
        EBMLDocTypeReadVersion = 0x4285,

        /// <summary>
        /// Indicates an EBML Void element.
        /// </summary>
        EBMLVoid = 0xEC,

        //////////////////

        /// <summary>
        /// Indicates a  Segment EBML element.
        /// </summary>
        Segment = 0x18538067,

        /// <summary>
        /// Indicates a  Segment Info EBML element.
        /// </summary>
        SegmentInfo = 0x1549A966,

        /// <summary>
        /// Indicates a  Tracks EBML Element.
        /// </summary>
        Tracks = 0x1654AE6B,

        /// <summary>
        /// Indicates a  Cues EBML element.
        /// </summary>
        Cues = 0x1C53BB6B,

        /// <summary>
        /// Indicates a  CuePoint EBML element.
        /// </summary>
        CuePoint = 0xBB,

        /// <summary>
        /// Indicates a  CueTime EBML element.
        /// </summary>
        CueTime = 0xB3,

        /// <summary>
        /// Indicates a  CueTrackPositions EBML element.
        /// </summary>
        CueTrackPositions = 0xB7,

        /// <summary>
        /// Indicates a  CueTrack EBML element.
        /// </summary>
        CueTrack = 0xF7,

        /// <summary>
        /// Indicates a  CueClusterPosition EBML element.
        /// </summary>
        CueClusterPosition = 0xF1,

        /// <summary>
        /// Indicates a  CueRelativePosition EBML element.
        /// </summary>
        CueRelativePosition = 0xF0,

        /// <summary>
        /// Indicates a  CueDuration EBML element.
        /// </summary>
        CueDuration = 0xB2,

        /// <summary>
        /// Indicates a  CueBlockNumber EBML element.
        /// </summary>
        CueBlockNumber = 0x5378,

        /// <summary>
        /// Indicates a  CueCodecState EBML element.
        /// </summary>
        CueCodecState = 0xEA,

        /// <summary>
        /// Indicates a  CueReference EBML element.
        /// </summary>
        CueReference = 0xDB,

        /// <summary>
        /// Indicates a  CueRefTime EBML element.
        /// </summary>
        CueRefTime = 0x96,

        /// <summary>
        /// Indicates a  CueRefCluster EBML element.
        /// </summary>
        CueRefCluster = 0x97,

        /// <summary>
        /// Indicates a  CueRefNumber EBML element.
        /// </summary>
        CueRefNumber = 0x535f,

        /// <summary>
        /// Indicates a  CueRefTime EBML element.
        /// </summary>
        RefCueCodecState = 0xEB,

        /// <summary>
        /// Indicates a  CueTrack EBML element.
        /// </summary>
        RelativePosition = 0xF0,

        /// <summary>
        /// Indicates a  CueTrackPosition EBML element.
        /// </summary>
        CueTrackPosition = 0xF1,

        /// <summary>
        /// Indicates a  Tags EBML element.
        /// </summary>
        Tags = 0x1254C367,

        /// <summary>
        /// Indicates a  Seek Head EBML element.
        /// </summary>
        SeekHead = 0x114D9B74,

        /// <summary>
        /// Indicates a  Cluster EBML element.
        /// </summary>
        Cluster = 0x1F43B675,

        /// <summary>
        /// Indicates a  Attachments EBML element.
        /// </summary>
        Attachments = 0x1941A469,

        /// <summary>
        /// Indicates a  Chapters EBML element.
        /// </summary>
        Chapters = 0x1043A770,

        /* IDs in the SegmentInfo master */

        /// <summary>
        /// Indicate a  Code Scale EBML element.
        /// </summary>
        TimeCodeScale = 0x2AD7B1,

        /// <summary>
        /// Indicates a  Duration EBML element.
        /// </summary>
        Duration = 0x4489,

        /// <summary>
        /// Indicates a  Writing App EBML element.
        /// </summary>
        WrittingApp = 0x5741,

        /// <summary>
        /// Indicates a  Muxing App EBML element.
        /// </summary>
        MuxingApp = 0x4D80,

        /// <summary>
        /// Indicates a  SeekEntry EBML element.
        /// </summary>
        SeekEntry = 0x4DBB,

        /// <summary>
        /// Indicates a  SeekID EBML element.
        /// </summary>
        SeekID = 0x53AB,

        /// <summary>
        /// Indicates a  SeekPosition EBML element.
        /// </summary>
        SeekPosition = 0x53AC,

        /// <summary>
        /// Indicate a  Date UTC EBML element.
        /// </summary>
        DateUTC = 0x4461,

        /// <summary>
        /// Indicate a  Segment UID EBML element.
        /// </summary>
        SegmentUID = 0x73A4,

        /// <summary>
        /// Indicate a  Segment File Name EBML element.
        /// </summary>
        SegmentFileName = 0x7384,

        /// <summary>
        /// Indicate a  Prev UID EBML element.
        /// </summary>
        PrevUID = 0x3CB923,

        /// <summary>
        /// Indicate a  Prev File Name EBML element.
        /// </summary>
        PrevFileName = 0x3C83AB,

        /// <summary>
        /// Indicate a  Nex UID EBML element.
        /// </summary>
        NexUID = 0x3EB923,

        /// <summary>
        /// Indicate a  Nex File Name EBML element.
        /// </summary>
        NexFileName = 0x3E83BB,

        /// <summary>
        /// Indicate a  Title EBML element.
        /// </summary>
        Title = 0x7BA9,

        /// <summary>
        /// Indicate a  Segment Family EBML element.
        /// </summary>
        SegmentFamily = 0x4444,

        /// <summary>
        /// Indicate a  Chapter Translate EBML element.
        /// </summary>
        ChapterTranslate = 0x6924,

        /* ID in the Tracks master */

        /// <summary>
        /// Indicate a  Track Entry EBML element.
        /// </summary>
        TrackEntry = 0xAE,

        /* IDs in the TrackEntry master */

        /// <summary>
        /// Indicate a  Track Number EBML element.
        /// </summary>
        TrackNumber = 0xD7,

        /// <summary>
        /// Indicate a  Track UID EBML element.
        /// </summary>
        TrackUID = 0x73C5,

        /// <summary>
        /// Indicate a  Track Type EBML element.
        /// </summary>
        TrackType = 0x83,

        /// <summary>
        /// Indicate a  Track Audio EBML element.
        /// </summary>
        TrackAudio = 0xE1,

        /// <summary>
        /// Indicate a  Track Video EBML element.
        /// </summary>
        TrackVideo = 0xE0,

        /// <summary>
        /// Indicate a  Track Encoding EBML element.
        /// </summary>
        ContentEncodings = 0x6D80,

        /// <summary>
        /// Indicate a  Codec ID EBML element.
        /// </summary>
        CodecID = 0x86,

        /// <summary>
        /// Indicate a  Codec Private EBML element.
        /// </summary>
        CodecPrivate = 0x63A2,

        /// <summary>
        /// Indicate a  Codec Name EBML element.
        /// </summary>
        CodecName = 0x258688,

        /// <summary>
        /// Indicate a  Track Name EBML element.
        /// </summary>
        TrackName = 0x536E,

        /// <summary>
        /// Indicate a  Track Language EBML element.
        /// </summary>
        TrackLanguage = 0x22B59C,

        /// <summary>
        /// Indicate a  Track Enabled EBML element.
        /// </summary>
        TrackFlagEnabled = 0xB9,

        /// <summary>
        /// Indicate a  Track Flag Default EBML element.
        /// </summary>
        TrackFlagDefault = 0x88,

        /// <summary>
        /// Indicate a  Track Flag Forced EBML element.
        /// </summary>
        TrackFlagForced = 0x55AA,

        /// <summary>
        /// Indicate a  Track Flag Lacing EBML element.
        /// </summary>
        TrackFlagLacing = 0x9C,

        /// <summary>
        /// Indicate a  Track Min Cache EBML element.
        /// </summary>
        TrackMinCache = 0x6DE7,

        /// <summary>
        /// Indicate a  Track Max Cache EBML element.
        /// </summary>
        TrackMaxCache = 0x6DF8,

        /// <summary>
        /// Indicate a  Track Default Duration EBML element.
        /// </summary>
        TrackDefaultDuration = 0x23E383,

        /// <summary>
        /// Indicate a  Track Time Code Scale EBML element.
        /// </summary>
        TrackTimeCodeScale = 0x23314F,

        /// <summary>
        /// Indicate a  Track Max Block Addition EBML element.
        /// </summary>
        MaxBlockAdditionID = 0x55EE,

        /// <summary>
        /// Indicate a  Track Attachment Link EBML element.
        /// </summary>
        TrackAttachmentLink = 0x7446,

        /// <summary>
        /// Indicate a  Track Overlay EBML element.
        /// </summary>
        TrackOverlay = 0x6FAB,

        /// <summary>
        /// Indicate a  Track Translate EBML element.
        /// </summary>
        TrackTranslate = 0x6624,

        /// <summary>
        /// Indicate a  Track Offset element.
        /// </summary>
        TrackOffset = 0x537F,

        /// <summary>
        /// Indicate a  Codec Settings EBML element.
        /// </summary>
        CodecSettings = 0x3A9697,

        /// <summary>
        /// Indicate a  Codec Info URL EBML element.
        /// </summary>
        CodecInfoUrl = 0x3B4040,

        /// <summary>
        /// Indicate a  Codec Download URL EBML element.
        /// </summary>
        CodecDownloadUrl = 0x26B240,

        /// <summary>
        /// Indicate a  Codec Decode All EBML element.
        /// </summary>
        CodecDecodeAll = 0xAA,

        /* IDs in the TrackVideo master */
        /* NOTE: This one is here only for backward compatibility.
        * Use _TRACKDEFAULDURATION */

        /// <summary>
        /// Indicate a  Video Frame Rate EBML element.
        /// </summary>
        VideoFrameRate = 0x2383E3,

        /// <summary>
        /// Indicate a  Video Display Width EBML element.
        /// </summary>
        VideoDisplayWidth = 0x54B0,

        /// <summary>
        /// Indicate a  Video Display Height EBML element.
        /// </summary>
        VideoDisplayHeight = 0x54BA,

        /// <summary>
        /// Indicate a  Video Display Unit EBML element.
        /// </summary>
        VideoDisplayUnit = 0x54B2,

        /// <summary>
        /// Indicate a  Video Pixel Width EBML element.
        /// </summary>
        VideoPixelWidth = 0xB0,

        /// <summary>
        /// Indicate a  Video Pixel Height EBML element.
        /// </summary>
        VideoPixelHeight = 0xBA,

        /// <summary>
        /// Indicate a  Video Pixel Crop Bottom EBML element.
        /// </summary>
        VideoPixelCropBottom = 0x54AA,

        /// <summary>
        /// Indicate a  Video Pixel Crop Top EBML element.
        /// </summary>
        VideoPixelCropTop = 0x54BB,

        /// <summary>
        /// Indicate a  Video Pixel Crop Left EBML element.
        /// </summary>
        VideoPixelCropLeft = 0x54CC,

        /// <summary>
        /// Indicate a  Video Pixel Crop Right EBML element.
        /// </summary>
        VideoPixelCropRight = 0x54DD,

        /// <summary>
        /// Indicate a  Video Flag Interlaced EBML element.
        /// </summary>
        VideoFlagInterlaced = 0x9A,

        /// <summary>
        /// Indicate a  Video Stereo Mode EBML element.
        /// </summary>
        VideoStereoMode = 0x53B8,

        /// <summary>
        /// Indicate a  Video Aspect Ratio Type EBML element.
        /// </summary>
        VideoAspectRatioType = 0x54B3,

        /// <summary>
        /// Indicate a  Video Colour Space EBML element.
        /// </summary>
        VideoColourSpace = 0x2EB524,

        /// <summary>
        /// Indicate a  Video Gamma Value EBML element.
        /// </summary>
        VideoGammaValue = 0x2FB523,

        /* IDs in the TrackAudio master */

        /// <summary>
        /// Indicate a  Audio Sampling Freq EBML element.
        /// </summary>
        AudioSamplingFreq = 0xB5,

        /// <summary>
        /// Indicate a  Audio Bit Depth EBML element.
        /// </summary>
        AudioBitDepth = 0x6264,

        /// <summary>
        /// Indicate a  Audio Channels EBML element.
        /// </summary>
        AudioChannels = 0x9F,

        /// <summary>
        /// Indicate a  Audio Channels Position EBML element.
        /// </summary>
        AudioChannelsPositions = 0x7D7B,

        /// <summary>
        /// Indicate a  Audio Output Sampling Freq EBML element.
        /// </summary>
        AudioOutputSamplingFreq = 0x78B5,

        /* IDs in the Tags master */

        /// <summary>
        /// Indicate a  Tag EBML element.
        /// </summary>
        Tag = 0x7373,

        /* in the Tag master */

        /// <summary>
        /// Indicate a  Simple Tag EBML element.
        /// </summary>
        SimpleTag = 0x67C8,

        /// <summary>
        /// Indicate a  Targets EBML element.
        /// </summary>
        Targets = 0x63C0,

        /* in the SimpleTag master */

        /// <summary>
        /// Indicate a  Tag Name EBML element.
        /// </summary>
        TagName = 0x45A3,

        /// <summary>
        /// Indicate a  Tag String EBML element.
        /// </summary>
        TagString = 0x4487,

        /// <summary>
        /// Indicate a  Tag Language EBML element.
        /// </summary>
        TagLanguage = 0x447A,

        /// <summary>
        /// Indicate a  Tag Default EBML element.
        /// </summary>
        TagDefault = 0x4484,

        /// <summary>
        /// Indicate a  Tag Binary EBML element.
        /// </summary>
        TagBinary = 0x4485
    }

    /// <summary>
    /// Represents the logic necessary to read files in the (Matroska) Extensible Binary Meta-Language [Ebml] (.mkv, .mka, .mk3d, .webm).
    /// </summary>
    public class MatroskaReader : MediaFileStream, IMediaContainer
    {
        const int DefaulTimeCodeScale = (int)Utility.NanosecondsPerMillisecond, DefaultMaxIdSize = 4, DefaultMaxSizeLength = 8;

        static DateTime BaseDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static byte[] ReadIdentifier(System.IO.Stream reader, out int length)
        {

            //Lookup for length?

            //if(reader.Remaining < 2) return null;
            
            // Get the header byte
            Byte header_byte = (byte)reader.ReadByte();
            
            // Define a mask
            Byte mask = 0x80, id_length = 1;
            
            // Figure out the size in bytes
            while (id_length <= 4 && (header_byte & mask) == 0)
            {
                id_length++;
                mask >>= 1;
            }

            length = id_length;

            //if (id_length > 4) throw new InvalidOperationException("invalid EBML id size");

            // Now read the rest of the EBML ID
            if (id_length > 1)
            {
                int left = id_length - 1;
                byte[] remainder = new byte[left];
                reader.Read(remainder, 0, left);
                return header_byte.Yield().Concat(remainder).ToArray();
            }

            return header_byte.Yield().ToArray();
        }
         //
        static byte[] ReadLength(System.IO.Stream reader, out int length)
        {
            //if (reader.Remaining < 2) return null;

            // Get the header byte
            Byte header_byte = (byte)reader.ReadByte();

            //Lookup for length?

            // Define a mask
            Byte mask = 0x80, size_length = 1;

            // Figure out the size in bytes (Should check MaxSize in the header)
            while (size_length <= 8 && (header_byte & mask) == 0)
            {
                size_length++;
                mask >>= 1;
            }

            length = size_length;

            //if (size_length > 8) throw new InvalidOperationException("invalid EBML element size");

            header_byte &= (byte)(mask - 1);

            // Now read the rest of the EBML ID
            if (size_length > 1)
            {
                int left = size_length - 1;
                byte[] remainder = new byte[left];
                reader.Read(remainder, 0, left);
                return header_byte.Yield().Concat(remainder).ToArray();
            }

            return header_byte.Yield().ToArray();
        }

        public static string ToTextualConvention(byte[] identifier, int offset = 0)
        {
            return ((Identifier)Common.Binary.Read32(identifier, offset, BitConverter.IsLittleEndian)).ToString(); 
        }

        public MatroskaReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public MatroskaReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public Node ReadElement(Identifier identifier, long position = 0) { return ReadElement((int)identifier, position); }

        public Node ReadElement(int identifier, long position = 0)
        {
            long positionStart = Position;

            Node result = ReadElements(position, identifier).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public IEnumerable<Node> ReadElements(long position, params Identifier[] identifiers) { return ReadElements(position, identifiers.Cast<int>().ToArray()); }

        public IEnumerable<Node> ReadElements(long position, params int[] identifiers)
        {
            long lastPosition = Position;

            Position = position;

            foreach (var element in this)
            {
                if (identifiers == null || identifiers .Count () == 0 || identifiers.Contains(Common.Binary.Read32(element.Identifier, 0, BitConverter.IsLittleEndian)))
                {
                    yield return element;
                    continue;
                }
            }

            Position = lastPosition;

            yield break;
        }

        public Node ReadNext()
        {
            if (Remaining <= 2) throw new System.IO.EndOfStreamException();

            int read = 0, rTotal = 0;

            byte[] identifier = ReadIdentifier(this, out read);

            rTotal += read;

            byte[] lengthBytes = ReadLength(this, out read);

            rTotal += read;

            long length = Common.Binary.Read64(lengthBytes, 0, BitConverter.IsLittleEndian);

            return new Node(this, identifier, read, Position, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > 2)
            {
                Node next = ReadNext();
                if (next == null) yield break;
                yield return next;

                //Decode the Id
                //Determine what to do to read the next
                switch ((Identifier)Common.Binary.ReadU32(next.Identifier, 0, BitConverter.IsLittleEndian))
                {
                    //Some Items are top level and contain children (Only really segment should be listed?)
                    case Identifier.Tracks: //Track is listed for making Parsing Tracks easier for now
                    case Identifier.Segment: continue;
                    //Otherwise skip the element's data to parse the next
                    default:
                        {
                            Skip(next.DataSize);
                            continue;
                        }
                }
            }
        }      

        public override Node Root
        {
            get { return ReadElement(Identifier.EBMLHeader); }
        }

        void ParseEbmlHeader()
        {

            using (var ebml = Root)
            {
                if(ebml != null) using (var stream = ebml.DataStream)
                {
                    long offset = stream.Position, streamLength = stream.Length;

                    //Read the Tracks Segment Info Header
                    byte[] identifer, len, buffer = new byte[32];

                    long length;

                    int read = 0;

                    //Read all elements in the Segment Info Data
                    while (offset < streamLength)
                    {
                        identifer = ReadIdentifier(stream, out read);

                        Identifier found = (Identifier)Common.Binary.ReadInteger(identifer, 0, read, BitConverter.IsLittleEndian);

                        offset += read;

                        len = ReadLength(stream, out read);

                        offset += read;

                        length = Common.Binary.ReadInteger(len, 0, read, !BitConverter.IsLittleEndian);

                        //Determine what to do based on the found Identifier
                        switch (found)
                        {
                            case Identifier.EBMLHeader: continue;
                            case Identifier.EBMLVersion:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_EbmlVersion = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case Identifier.EBMLReadVersion:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_EbmlReadVersion = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case Identifier.EBMLMaxIDLength:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_MaxIDLength = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case Identifier.EBMLMaxSizeLength:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_MaxSizeLength = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case Identifier.EBMLDocType:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_DocType = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    goto default;
                                }
                            case Identifier.EBMLDocTypeVersion:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_DocTypeVersion = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    goto default;
                                }
                            case Identifier.EBMLDocTypeReadVersion:
                                {
                                    m_DocTypeReadVersion = (int)Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian);
                                    stream.Read(buffer, 0, (int)length);
                                    goto default;
                                }
                            default:
                                {
                                    offset += length;
                                    continue;
                                }
                        }
                    }
                }
            }

            //should ensure all have value.

        }

        void ParseSegmentInfo()
        {
            using (var matroskaSegmentInfo = ReadElement(Identifier.SegmentInfo, Root.DataOffset))
            {

                if (matroskaSegmentInfo != null) using (var stream = matroskaSegmentInfo.DataStream)
                {

                    long offset = stream.Position, streamLength = stream.Length;

                    //Read the Tracks Segment Info Header
                    byte[] identifer, len, buffer = new byte[32];

                    long length;

                    int read = 0;
                    //Read all elements in the Segment Info Data
                    while (offset < streamLength)
                    {

                        identifer = ReadIdentifier(stream, out read);

                        offset += read;

                        len = ReadLength(stream, out read);

                        offset += read;

                        length = Common.Binary.Read64(len, 0, BitConverter.IsLittleEndian);

                        Identifier found = (Identifier)Common.Binary.Read32(identifer, 0, BitConverter.IsLittleEndian);

                        //Determine what to do based on the found Identifier
                        switch (found)
                        {
                            case Identifier.Duration:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    //m_Duration = TimeSpan.FromMilliseconds((double)(Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian) * m_TimeCodeScale * 1000) / 1000000);

                                    //m_Duration = TimeSpan.FromSeconds(TimeSpan.FromMilliseconds((Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian) / TimeCodeScale) / TimeSpan.TicksPerMillisecond).TotalHours * m_TimeCodeScale);

                                    m_Duration = TimeSpan.FromTicks(Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian) / m_TimeCodeScale);

                                    offset += length;
                                    continue;
                                }
                            case Identifier.DateUTC:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_Created = BaseDate.AddMilliseconds(Utility.NanosecondsPerSecond / Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian));
                                    offset += length;
                                    continue;
                                }
                            case Identifier.TimeCodeScale:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_TimeCodeScale = Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.Title:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_Title = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MuxingApp:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_MuxingApp = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.WrittingApp:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_WritingApp = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            default:
                                {
                                    stream.Position += length;
                                    offset += length;
                                    continue;
                                }
                        }
                    }
                }
            }

            if (!m_Duration.HasValue) m_Duration = TimeSpan.Zero;

            if (!m_Created.HasValue) m_Created = FileInfo.CreationTimeUtc;

            if (m_MuxingApp == null) m_MuxingApp = string.Empty;

            if (m_WritingApp == null) m_WritingApp = string.Empty;

            //Not in spec....
            m_Modified = FileInfo.LastWriteTimeUtc;
        }        

        long m_TimeCodeScale = DefaulTimeCodeScale;

        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (!m_Created.HasValue) ParseSegmentInfo();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (!m_Modified.HasValue) ParseSegmentInfo();
                return m_Modified.Value;
            }
        }


        public long TimeCodeScale
        {
            get
            {
                if (m_MuxingApp == null) ParseSegmentInfo();
                return m_TimeCodeScale;
            }
        }

        public string MuxingApp
        {
            get
            {
                if (m_MuxingApp == null) ParseSegmentInfo();
                return m_MuxingApp;
            }
        }

        public string WritingApp
        {
            get
            {
                if (m_WritingApp == null) ParseSegmentInfo();
                return m_WritingApp;
            }
        }

        string m_DocType, m_MuxingApp, m_WritingApp, m_Title;

        int m_MaxIDLength = DefaultMaxIdSize, m_MaxSizeLength = DefaultMaxSizeLength;

        int? m_EbmlVersion, m_EbmlReadVersion, m_DocTypeVersion, m_DocTypeReadVersion;

        public int EbmlVersion
        {
            get
            {
                if (!m_EbmlVersion.HasValue) ParseEbmlHeader();
                return m_EbmlVersion.Value;
            }
        }

        public int EbmlReadVersion
        {
            get
            {
                if (!m_EbmlReadVersion.HasValue) ParseEbmlHeader();
                return m_EbmlReadVersion.Value;
            }
        }

        public int DocTypeVersion
        {
            get
            {
                if (!m_DocTypeVersion.HasValue) ParseEbmlHeader();
                return m_DocTypeVersion.Value;
            }
        }

        public int DocTypeReadVersion
        {
            get
            {
                if (!m_DocTypeReadVersion.HasValue) ParseEbmlHeader();
                return m_DocTypeReadVersion.Value;
            }
        }

        public int EbmlMaxIdLength
        {
            get
            {
                if (!m_DocTypeVersion.HasValue) ParseEbmlHeader();
                return m_MaxIDLength;
            }
        }

        public int EbmlMaxSizeLength
        {
            get
            {
                if (!m_DocTypeVersion.HasValue) ParseEbmlHeader();
                return m_MaxSizeLength;
            }
        }

        public string DocType
        {
            get
            {
                if (m_DocType == null) ParseEbmlHeader();
                return m_DocType;
            }
        }

        TimeSpan? m_Duration;

        public TimeSpan Duration
        {
            get
            {
                if (!m_Duration.HasValue) ParseSegmentInfo();
                return m_Duration.Value;
            }
        }

        /// <summary>
        /// Returns the SeekHead element.
        /// </summary>
        public override Node TableOfContents
        {
            //Could also give Cues?
            //Not parsed because some utilities which join files do not propertly create additional entries
            get { return ReadElement(Identifier.SeekHead, Root.DataOffset); }
        }
        
        List<Track> m_Tracks;

        public override IEnumerable<Track> GetTracks()
        {

            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }


            //Parse Seekheader, look for TrackEntry offsets?

            long position = Position;

            ulong trackId = 0, trackDuration = 0, height = 0, width = 0, startTime = 0, timeCodeScale = (ulong)m_TimeCodeScale, sampleCount = 0;

            double rate = 0;

            byte bitsPerSample = 0, channels = 0;

            string trackName = string.Empty;

            //CodecID, CodecName?

            byte[] codecIndication = Utility.Empty;

            var tracks = new List<Track>();

            Sdp.MediaType mediaType = Sdp.MediaType.unknown;

            //Tracks is the parent element of all TrackEntry
            foreach (var trackEntryElement in ReadElements(Root.DataOffset, Identifier.TrackEntry).ToArray())
            {
                using (var stream = trackEntryElement.DataStream)
                {
                    long offset = stream.Position, streamLength = stream.Length, length = 0;

                    int read = 0;

                    byte[] buffer = new byte[32], identifier;

                    //Read all elements in the Tracks Element's Data
                    while (offset < streamLength)
                    {
                        identifier = ReadIdentifier(stream, out read);

                        Identifier found = (Identifier)Common.Binary.Read32(identifier, 0, BitConverter.IsLittleEndian);

                        offset += read;

                        var len = ReadLength(stream, out read);

                        offset += read;

                        length = Common.Binary.Read64(len, 0, BitConverter.IsLittleEndian);

                        //Determine what to do based on the found Identifier
                        switch (found)
                        {
                            
                            case Identifier.TrackVideo: 
                            case Identifier.TrackAudio:
                                continue;
                            case Identifier.TrackType:
                                {
                                    byte info = (byte)stream.ReadByte();

                                    if ((info & 1) == (int)info) mediaType = Sdp.MediaType.video;
                                    else if ((info & 2) == (int)info) mediaType = Sdp.MediaType.audio;
                                    //Complex = 3
                                    //Logo = 0x10
                                    //Subtitle
                                    else if ((info & 0x11) == (int)info) mediaType = Sdp.MediaType.text;
                                    //Buttons = 0x12
                                    else if ((info & 0x20) == (int)info) mediaType = Sdp.MediaType.control;
                                    ++offset;
                                    continue;
                                }
                            case Identifier.VideoColourSpace:
                                {

                                    //length == 1 !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian? 

                                    stream.Read(buffer, 0, (int)length);
                                    bitsPerSample = (byte)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);

                                    //Maybe a fourCC indicating the sub sampling type...?

                                    offset += length;
                                    continue;
                                }
                            case Identifier.AudioBitDepth:
                                {
                                    bitsPerSample = (byte)stream.ReadByte();
                                    ++offset;
                                    continue;
                                }                           
                            case Identifier.AudioChannels:
                                {
                                    channels = (byte)stream.ReadByte();
                                    ++offset;
                                    continue;
                                }
                            case Identifier.TrackUID:
                            case Identifier.TrackNumber:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    trackId = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.TrackDefaultDuration:
                                {
                                    //Really the sample Rate?
                                    //Number of nanoseconds (not scaled via TimecodeScale) per frame ('frame' in the  sense -- one element put into a (Simple)Block).
                                    stream.Read(buffer, 0, (int)length);
                                    if (mediaType == Sdp.MediaType.video)rate =  Utility.NanosecondsPerSecond / (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    else rate = (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.VideoFrameRate: //DEPRECATED
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    rate = (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    trackDuration = (ulong)(Utility.NanosecondsPerSecond * rate);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.TimeCodeScale:
                            case Identifier.TrackTimeCodeScale://DEPRECATED, DO NOT USE. 
                                {
                                    //Really the sample Rate?
                                    //Number of nanoseconds (not scaled via TimecodeScale) per frame ('frame' in the  sense -- one element put into a (Simple)Block).
                                    stream.Read(buffer, 0, (int)length);
                                    timeCodeScale = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.AudioSamplingFreq:
                                {
                                    //Ensure this is read correctly....
                                    stream.Read(buffer, 0, (int)length);
                                    rate = BitConverter.Int64BitsToDouble(Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian));
                                    offset += length;
                                    continue;
                                }
                            case Identifier.AudioOutputSamplingFreq:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    //Rescale
                                    rate /= (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.VideoPixelWidth:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    width = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.VideoPixelHeight:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    height = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.TrackOffset://DEPRECATED, DO NOT USE.
                                {
                                    //A value to add to the Block's Timestamp. This can be used to adjust the playback offset of a track.
                                    stream.Read(buffer, 0, (int)length);
                                    startTime = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    break;
                                }
                            case Identifier.TrackName:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    trackName = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.CodecName:
                            case Identifier.CodecID:
                                {
                                    codecIndication = new byte[(int)length];
                                    stream.Read(codecIndication, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            default:
                                {
                                    //Move past any element inside not required to describe the track
                                    offset += length;
                                    stream.Position += length;
                                    continue;
                                }
                        }

                    }

                    //The bitDepth of video is possibly unknown at this point...

                    //If so the only way to determine it would be reading the actual video data and calculating from frameSize...

                    //Need to find all CueTimes to accurately describe duration and start time and sample count...
                    // is WONDERFUL                    
                    //Only do this one time for now...
                    if(sampleCount == 0) foreach (var elem in ReadElements(trackEntryElement.DataOffset, Identifier.Cues))
                    {
                        using (var cueStream = elem.DataStream)
                        {
                            long cueOffset = cueStream.Position, cueLength = cueStream.Length;

                            //Read all elements in the Tracks Element's Data
                            while (cueOffset < cueLength)
                            {
                                identifier = ReadIdentifier(cueStream, out read);

                                Identifier found = (Identifier)Common.Binary.Read32(identifier, 0, BitConverter.IsLittleEndian);

                                cueOffset += read;

                                var len = ReadLength(cueStream, out read);

                                cueOffset += read;

                                length = Common.Binary.Read64(len, 0, BitConverter.IsLittleEndian);

                                //Determine what to do based on the found Identifier
                                switch (found)
                                {
                                    case Identifier.CuePoint: continue;
                                    case Identifier.CueTime:
                                        {
                                            ++sampleCount;
                                            cueStream.Read(buffer, 0, (int)length);
                                            trackDuration += Media.Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                                            cueOffset += length;
                                            continue;
                                        }
                                    default:
                                        {
                                            cueStream.Position += length;
                                            cueOffset += length;
                                            continue;
                                        }
                                }
                            }
                        }
                    }

                    Track track = new Track(trackEntryElement, trackName, (int)trackId, m_Created.Value, m_Modified.Value, (int)sampleCount, (int)height, (int)width, TimeSpan.Zero, TimeSpan.FromMilliseconds((trackDuration / Utility.NanosecondsPerSecond) * timeCodeScale / TimeSpan.TicksPerMillisecond), rate, mediaType, codecIndication, channels, bitsPerSample);

                    yield return track;

                    tracks.Add(track);

                    //Reset reading properties

                    trackId = height = width = startTime =  0;

                    timeCodeScale = (ulong)m_TimeCodeScale;

                    rate = 0;

                    bitsPerSample = channels = 0;

                    trackName = string.Empty;

                    mediaType = Sdp.MediaType.unknown;
                }
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
