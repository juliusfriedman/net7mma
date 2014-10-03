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
        /// Indicates a Matroska Segment EBML element.
        /// </summary>
        MatroskaSegment = 0x18538067,

        /// <summary>
        /// Indicates a Matroska Segment Info EBML element.
        /// </summary>
        MatroskaSegmentInfo = 0x1549A966,

        /// <summary>
        /// Indicates a Matroska Tracks EBML Element.
        /// </summary>
        MatroskaTracks = 0x1654AE6B,

        /// <summary>
        /// Indicates a Matroska Cues EBML element.
        /// </summary>
        MatroskaCues = 0x1C53BB6B,

        /// <summary>
        /// Indicates a Matroska CuePoint EBML element.
        /// </summary>
        MatroskaCuePoint = 0xBB,

        /// <summary>
        /// Indicates a Matroska CueTime EBML element.
        /// </summary>
        MatroskaCueTime = 0xB3,

        /// <summary>
        /// Indicates a Matroska CueTrackPositions EBML element.
        /// </summary>
        MatroskaCueTrackPositions = 0xB7,

        /// <summary>
        /// Indicates a Matroska CueTrack EBML element.
        /// </summary>
        MatroskaCueTrack = 0xF7,

        /// <summary>
        /// Indicates a Matroska CueClusterPosition EBML element.
        /// </summary>
        MatroskaCueClusterPosition = 0xF1,

        /// <summary>
        /// Indicates a Matroska CueRelativePosition EBML element.
        /// </summary>
        MatroskaCueRelativePosition = 0xF0,

        /// <summary>
        /// Indicates a Matroska CueDuration EBML element.
        /// </summary>
        MatroskaCueDuration = 0xB2,

        /// <summary>
        /// Indicates a Matroska CueBlockNumber EBML element.
        /// </summary>
        MatroskaCueBlockNumber = 0x5378,

        /// <summary>
        /// Indicates a Matroska CueCodecState EBML element.
        /// </summary>
        MatroskaCueCodecState = 0xEA,

        /// <summary>
        /// Indicates a Matroska MatroskaCueReference EBML element.
        /// </summary>
        MatroskaCueReference = 0xDB,

        /// <summary>
        /// Indicates a Matroska MatroskaCueRefTime EBML element.
        /// </summary>
        MatroskaCueRefTime = 0x96,

        /// <summary>
        /// Indicates a Matroska MatroskaCueRefCluster EBML element.
        /// </summary>
        MatroskaCueRefCluster = 0x97,

        /// <summary>
        /// Indicates a Matroska MatroskaCueRefNumber EBML element.
        /// </summary>
        MatroskaCueRefNumber = 0x535f,

        /// <summary>
        /// Indicates a Matroska MatroskaCueRefTime EBML element.
        /// </summary>
        MatroskaRefCueCodecState = 0xEB,

        /// <summary>
        /// Indicates a Matroska CueTrack EBML element.
        /// </summary>
        MatroskaRelativePosition = 0xF0,

        /// <summary>
        /// Indicates a Matroska CueTrackPosition EBML element.
        /// </summary>
        MatroskaCueTrackPosition = 0xF1,

        /// <summary>
        /// Indicates a Matroska Tags EBML element.
        /// </summary>
        MatroskaTags = 0x1254C367,

        /// <summary>
        /// Indicates a Matroska Seek Head EBML element.
        /// </summary>
        MatroskaSeekHead = 0x114D9B74,

        /// <summary>
        /// Indicates a Matroska Cluster EBML element.
        /// </summary>
        MatroskaCluster = 0x1F43B675,

        /// <summary>
        /// Indicates a Matroska Attachments EBML element.
        /// </summary>
        MatroskaAttachments = 0x1941A469,

        /// <summary>
        /// Indicates a Matroska Chapters EBML element.
        /// </summary>
        MatroskaChapters = 0x1043A770,

        /* IDs in the SegmentInfo master */

        /// <summary>
        /// Indicate a Matroska Code Scale EBML element.
        /// </summary>
        MatroskaTimeCodeScale = 0x2AD7B1,

        /// <summary>
        /// Indicates a Matroska Duration EBML element.
        /// </summary>
        MatroskaDuration = 0x4489,

        /// <summary>
        /// Indicates a Matroska Writing App EBML element.
        /// </summary>
        MatroskaWrittingApp = 0x5741,

        /// <summary>
        /// Indicates a Matroska Muxing App EBML element.
        /// </summary>
        MatroskaMuxingApp = 0x4D80,

        /// <summary>
        /// Indicates a Matroska SeekEntry EBML element.
        /// </summary>
        MatroskaSeekEntry = 0x4DBB,

        /// <summary>
        /// Indicates a Matroska SeekID EBML element.
        /// </summary>
        MatroskaSeekID = 0x53AB,

        /// <summary>
        /// Indicates a Matroska SeekPosition EBML element.
        /// </summary>
        MatroskaSeekPosition = 0x53AC,

        /// <summary>
        /// Indicate a Matroska Date UTC EBML element.
        /// </summary>
        MatroskaDateUTC = 0x4461,

        /// <summary>
        /// Indicate a Matroska Segment UID EBML element.
        /// </summary>
        MatroskaSegmentUID = 0x73A4,

        /// <summary>
        /// Indicate a Matroska Segment File Name EBML element.
        /// </summary>
        MatroskaSegmentFileName = 0x7384,

        /// <summary>
        /// Indicate a Matroska Prev UID EBML element.
        /// </summary>
        MatroskaPrevUID = 0x3CB923,

        /// <summary>
        /// Indicate a Matroska Prev File Name EBML element.
        /// </summary>
        MatroskaPrevFileName = 0x3C83AB,

        /// <summary>
        /// Indicate a Matroska Nex UID EBML element.
        /// </summary>
        MatroskaNexUID = 0x3EB923,

        /// <summary>
        /// Indicate a Matroska Nex File Name EBML element.
        /// </summary>
        MatroskaNexFileName = 0x3E83BB,

        /// <summary>
        /// Indicate a Matroska Title EBML element.
        /// </summary>
        MatroskaTitle = 0x7BA9,

        /// <summary>
        /// Indicate a Matroska Segment Family EBML element.
        /// </summary>
        MatroskaSegmentFamily = 0x4444,

        /// <summary>
        /// Indicate a Matroska Chapter Translate EBML element.
        /// </summary>
        MatroskaChapterTranslate = 0x6924,

        /* ID in the Tracks master */

        /// <summary>
        /// Indicate a Matroska Track Entry EBML element.
        /// </summary>
        MatroskaTrackEntry = 0xAE,

        /* IDs in the TrackEntry master */

        /// <summary>
        /// Indicate a Matroska Track Number EBML element.
        /// </summary>
        MatroskaTrackNumber = 0xD7,

        /// <summary>
        /// Indicate a Matroska Track UID EBML element.
        /// </summary>
        MatroskaTrackUID = 0x73C5,

        /// <summary>
        /// Indicate a Matroska Track Type EBML element.
        /// </summary>
        MatroskaTrackType = 0x83,

        /// <summary>
        /// Indicate a Matroska Track Audio EBML element.
        /// </summary>
        MatroskaTrackAudio = 0xE1,

        /// <summary>
        /// Indicate a Matroska Track Video EBML element.
        /// </summary>
        MatroskaTrackVideo = 0xE0,

        /// <summary>
        /// Indicate a Matroska Track Encoding EBML element.
        /// </summary>
        MatroskaContentEncodings = 0x6D80,

        /// <summary>
        /// Indicate a Matroska Codec ID EBML element.
        /// </summary>
        MatroskaCodecID = 0x86,

        /// <summary>
        /// Indicate a Matroska Codec Private EBML element.
        /// </summary>
        MatroskaCodecPrivate = 0x63A2,

        /// <summary>
        /// Indicate a Matroska Codec Name EBML element.
        /// </summary>
        MatroskaCodecName = 0x258688,

        /// <summary>
        /// Indicate a Matroska Track Name EBML element.
        /// </summary>
        MatroskaTrackName = 0x536E,

        /// <summary>
        /// Indicate a Matroska Track Language EBML element.
        /// </summary>
        MatroskaTrackLanguage = 0x22B59C,

        /// <summary>
        /// Indicate a Matroska Track Enabled EBML element.
        /// </summary>
        MatroskaTrackFlagEnabled = 0xB9,

        /// <summary>
        /// Indicate a Matroska Track Flag Default EBML element.
        /// </summary>
        MatroskaTrackFlagDefault = 0x88,

        /// <summary>
        /// Indicate a Matroska Track Flag Forced EBML element.
        /// </summary>
        MatroskaTrackFlagForced = 0x55AA,

        /// <summary>
        /// Indicate a Matroska Track Flag Lacing EBML element.
        /// </summary>
        MatroskaTrackFlagLacing = 0x9C,

        /// <summary>
        /// Indicate a Matroska Track Min Cache EBML element.
        /// </summary>
        MatroskaTrackMinCache = 0x6DE7,

        /// <summary>
        /// Indicate a Matroska Track Max Cache EBML element.
        /// </summary>
        MatroskaTrackMaxCache = 0x6DF8,

        /// <summary>
        /// Indicate a Matroska Track Default Duration EBML element.
        /// </summary>
        MatroskaTrackDefaultDuration = 0x23E383,

        /// <summary>
        /// Indicate a Matroska Track Time Code Scale EBML element.
        /// </summary>
        MatroskaTrackTimeCodeScale = 0x23314F,

        /// <summary>
        /// Indicate a Matroska Track Max Block Addition EBML element.
        /// </summary>
        MatroskaMaxBlockAdditionID = 0x55EE,

        /// <summary>
        /// Indicate a Matroska Track Attachment Link EBML element.
        /// </summary>
        MatroskaTrackAttachmentLink = 0x7446,

        /// <summary>
        /// Indicate a Matroska Track Overlay EBML element.
        /// </summary>
        MatroskaTrackOverlay = 0x6FAB,

        /// <summary>
        /// Indicate a Matroska Track Translate EBML element.
        /// </summary>
        MatroskaTrackTranslate = 0x6624,

        /// <summary>
        /// Indicate a Matroska Track Offset element.
        /// </summary>
        MatroskaTrackOffset = 0x537F,

        /// <summary>
        /// Indicate a Matroska Codec Settings EBML element.
        /// </summary>
        MatroskaCodecSettings = 0x3A9697,

        /// <summary>
        /// Indicate a Matroska Codec Info URL EBML element.
        /// </summary>
        MatroskaCodecInfoUrl = 0x3B4040,

        /// <summary>
        /// Indicate a Matroska Codec Download URL EBML element.
        /// </summary>
        MatroskaCodecDownloadUrl = 0x26B240,

        /// <summary>
        /// Indicate a Matroska Codec Decode All EBML element.
        /// </summary>
        MatroskaCodecDecodeAll = 0xAA,

        /* IDs in the TrackVideo master */
        /* NOTE: This one is here only for backward compatibility.
        * Use _TRACKDEFAULDURATION */

        /// <summary>
        /// Indicate a Matroska Video Frame Rate EBML element.
        /// </summary>
        MatroskaVideoFrameRate = 0x2383E3,

        /// <summary>
        /// Indicate a Matroska Video Display Width EBML element.
        /// </summary>
        MatroskaVideoDisplayWidth = 0x54B0,

        /// <summary>
        /// Indicate a Matroska Video Display Height EBML element.
        /// </summary>
        MatroskaVideoDisplayHeight = 0x54BA,

        /// <summary>
        /// Indicate a Matroska Video Display Unit EBML element.
        /// </summary>
        MatroskaVideoDisplayUnit = 0x54B2,

        /// <summary>
        /// Indicate a Matroska Video Pixel Width EBML element.
        /// </summary>
        MatroskaVideoPixelWidth = 0xB0,

        /// <summary>
        /// Indicate a Matroska Video Pixel Height EBML element.
        /// </summary>
        MatroskaVideoPixelHeight = 0xBA,

        /// <summary>
        /// Indicate a Matroska Video Pixel Crop Bottom EBML element.
        /// </summary>
        MatroskaVideoPixelCropBottom = 0x54AA,

        /// <summary>
        /// Indicate a Matroska Video Pixel Crop Top EBML element.
        /// </summary>
        MatroskaVideoPixelCropTop = 0x54BB,

        /// <summary>
        /// Indicate a Matroska Video Pixel Crop Left EBML element.
        /// </summary>
        MatroskaVideoPixelCropLeft = 0x54CC,

        /// <summary>
        /// Indicate a Matroska Video Pixel Crop Right EBML element.
        /// </summary>
        MatroskaVideoPixelCropRight = 0x54DD,

        /// <summary>
        /// Indicate a Matroska Video Flag Interlaced EBML element.
        /// </summary>
        MatroskaVideoFlagInterlaced = 0x9A,

        /// <summary>
        /// Indicate a Matroska Video Stereo Mode EBML element.
        /// </summary>
        MatroskaVideoStereoMode = 0x53B8,

        /// <summary>
        /// Indicate a Matroska Video Aspect Ratio Type EBML element.
        /// </summary>
        MatroskaVideoAspectRatioType = 0x54B3,

        /// <summary>
        /// Indicate a Matroska Video Colour Space EBML element.
        /// </summary>
        MatroskaVideoColourSpace = 0x2EB524,

        /// <summary>
        /// Indicate a Matroska Video Gamma Value EBML element.
        /// </summary>
        MatroskaVideoGammaValue = 0x2FB523,

        /* IDs in the TrackAudio master */

        /// <summary>
        /// Indicate a Matroska Audio Sampling Freq EBML element.
        /// </summary>
        MatroskaAudioSamplingFreq = 0xB5,

        /// <summary>
        /// Indicate a Matroska Audio Bit Depth EBML element.
        /// </summary>
        MatroskaAudioBitDepth = 0x6264,

        /// <summary>
        /// Indicate a Matroska Audio Channels EBML element.
        /// </summary>
        MatroskaAudioChannels = 0x9F,

        /// <summary>
        /// Indicate a Matroska Audio Channels Position EBML element.
        /// </summary>
        MatroskaAudioChannelsPositions = 0x7D7B,

        /// <summary>
        /// Indicate a Matroska Audio Output Sampling Freq EBML element.
        /// </summary>
        MatroskaAudioOutputSamplingFreq = 0x78B5,

        /* IDs in the Tags master */

        /// <summary>
        /// Indicate a Matroska Tag EBML element.
        /// </summary>
        MatroskaTag = 0x7373,

        /* in the Tag master */

        /// <summary>
        /// Indicate a Matroska Simple Tag EBML element.
        /// </summary>
        MatroskaSimpleTag = 0x67C8,

        /// <summary>
        /// Indicate a Matroska Targets EBML element.
        /// </summary>
        MatroskaTargets = 0x63C0,

        /* in the SimpleTag master */

        /// <summary>
        /// Indicate a Matroska Tag Name EBML element.
        /// </summary>
        MatroskaTagName = 0x45A3,

        /// <summary>
        /// Indicate a Matroska Tag String EBML element.
        /// </summary>
        MatroskaTagString = 0x4487,

        /// <summary>
        /// Indicate a Matroska Tag Language EBML element.
        /// </summary>
        MatroskaTagLanguage = 0x447A,

        /// <summary>
        /// Indicate a Matroska Tag Default EBML element.
        /// </summary>
        MatroskaTagDefault = 0x4484,

        /// <summary>
        /// Indicate a Matroska Tag Binary EBML element.
        /// </summary>
        MatroskaTagBinary = 0x4485
    }

    /// <summary>
    /// Represents the logic necessary to read files in the Matroska Extensible Binary Meta-Language [Ebml] (.mkv)
    /// </summary>
    public class MatroskaReader : MediaFileStream, IMediaContainer
    {
        const int DefaulTimeCodeScale = 1000000, DefaultMaxIdSize = 4, DefaultMaxSizeLength = 8;

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

        public Element ReadElement(Identifier identifier, long position = 0) { return ReadElement((int)identifier, position); }

        public Element ReadElement(int identifier, long position = 0) { return ReadElements(position, identifier.Yield().ToArray()).FirstOrDefault(); }

        public IEnumerable<Element> ReadElements(long position, params Identifier[] identifiers) { return ReadElements(position, identifiers.Cast<int>().ToArray()); }

        public IEnumerable<Element> ReadElements(long position, params int[] identifiers)
        {
            if (identifiers == null) yield break;

            long lastPosition = Position;

            Position = position;

            foreach (var element in this)
            {
                if (identifiers.Contains(Common.Binary.Read32(element.Identifier, 0, BitConverter.IsLittleEndian)))
                {
                    yield return element;
                }
            }

            Position = lastPosition;
        }

        public Element ReadNext()
        {
            if (Remaining <= 2) throw new System.IO.EndOfStreamException();

            int read = 0, rTotal = 0;

            bool complete = true;

            byte[] identifier = ReadIdentifier(this.m_Stream, out read);

            this.m_Position += read;

            rTotal += read;

            byte[] lengthBytes = ReadLength(this.m_Stream, out read);

            this.m_Position += read;

            rTotal += read;

            long length = Common.Binary.Read64(lengthBytes, 0, BitConverter.IsLittleEndian);

            complete = length <= Remaining;

            //The position of the Data is given here, the actual occurance of the data occurs at Position - rTotal
            //The data is already contained in indentifier and length though and should not be required again.
            return new Element(this, identifier, Position, length, complete);
        }

        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > 2)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;

                //Decode the Id
                Identifier found = (Identifier)Common.Binary.ReadU32(next.Identifier, 0, BitConverter.IsLittleEndian);

                //Determine what to do to read the next
                switch (found)
                {
                    //Some Items are top level and contain children (Only really segment should be listed?)
                    case Identifier.MatroskaTracks: //Track is listed for making Parsing Tracks easier for now
                    case Identifier.MatroskaSegment: continue;
                    //Otherwise skip the element's data to parse the next
                    default:
                        {
                            Skip(next.Size);
                            continue;
                        }
                }
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


        void ParseEbmlHeader()
        {

            using (var ebml = Root)
            {
                if(ebml != null) using (var stream = ebml.Data)
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
        }

        void ParseSegmentInfo()
        {
            using (var matroskaSegmentInfo = ReadElement(Identifier.MatroskaSegmentInfo, Root.Offset))
            {

                if (matroskaSegmentInfo != null) using (var stream = matroskaSegmentInfo.Data)
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
                            case Identifier.MatroskaDuration:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    //m_Duration = TimeSpan.FromMilliseconds((double)(Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian) * m_TimeCodeScale * 1000) / 1000000);

                                    //m_Duration = TimeSpan.FromSeconds(TimeSpan.FromMilliseconds((Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian) / TimeCodeScale) / TimeSpan.TicksPerMillisecond).TotalHours * m_TimeCodeScale);

                                    m_Duration = TimeSpan.FromTicks(Common.Binary.ReadInteger(buffer, 0, (int)length, !BitConverter.IsLittleEndian) / m_TimeCodeScale);

                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaDateUTC:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_Created = BaseDate.AddMilliseconds(Utility.NanosecondsPerSecond / Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian));
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaTimeCodeScale:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_TimeCodeScale = Common.Binary.ReadInteger(buffer, 0, (int)length, BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaTitle:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_Title = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaMuxingApp:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    m_MuxingApp = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaWrittingApp:
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

            if (!m_Created.HasValue) m_Created = (new System.IO.FileInfo(Location.LocalPath)).CreationTimeUtc;

            if (m_MuxingApp == null) m_MuxingApp = string.Empty;

            if (m_WritingApp == null) m_WritingApp = string.Empty;

            //Not in spec....
            m_Modified = (new System.IO.FileInfo(Location.LocalPath)).LastWriteTimeUtc;
        }        

        long m_TimeCodeScale = DefaulTimeCodeScale;

        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (m_Created.HasValue) return m_Created.Value;
                ParseSegmentInfo();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (m_Modified.HasValue) return m_Created.Value;
                ParseSegmentInfo();
                return m_Modified.Value;
            }
        }

        string m_DocType, m_MuxingApp, m_WritingApp, m_Title;

        int m_MaxIDLength = DefaultMaxIdSize, m_MaxSizeLength = DefaultMaxSizeLength;

        int? m_EbmlVersion, m_EbmlReadVersion, m_DocTypeVersion, m_DocTypeReadVersion;

        public long TimeCodeScale
        {
            get
            {
                if (m_MuxingApp == null) ParseSegmentInfo();
                return m_TimeCodeScale;
            }
        }

        public int EbmlVersion
        {
            get
            {
                if (m_EbmlVersion.HasValue) return m_EbmlVersion.Value;
                ParseEbmlHeader();
                return m_EbmlVersion.Value;
            }
        }

        public int EbmlReadVersion
        {
            get
            {
                if (m_EbmlVersion.HasValue) return m_EbmlVersion.Value;
                ParseEbmlHeader();
                return m_EbmlVersion.Value;
            }
        }

        public int EbmlDocTypeVersion
        {
            get
            {
                if (m_EbmlVersion.HasValue) return m_EbmlVersion.Value;
                ParseEbmlHeader();
                return m_EbmlVersion.Value;
            }
        }

        public int EbmlDocTypeReadVersion
        {
            get
            {
                if (m_EbmlVersion.HasValue) return m_EbmlVersion.Value;
                ParseEbmlHeader();
                return m_EbmlVersion.Value;
            }
        }

        public int EbmlMaxIdLength
        {
            get
            {
                if (m_DocTypeVersion.HasValue) return m_MaxIDLength;
                ParseEbmlHeader();
                return m_MaxIDLength;
            }
        }

        public int EbmlMaxSizeLength
        {
            get
            {
                if (m_DocTypeVersion.HasValue) return m_MaxSizeLength;
                ParseEbmlHeader();
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

        TimeSpan? m_Duration;

        public TimeSpan Duration
        {
            get
            {
                if (m_Duration.HasValue) return m_Duration.Value;
                ParseSegmentInfo();
                return m_Duration.Value;
            }
        }

        /// <summary>
        /// Returns the SeekHead element
        /// </summary>
        public override Element TableOfContents
        {
            //Could also give Cues?
            get { return ReadElement(Identifier.MatroskaSeekHead, Root.Offset); }
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
            foreach (var trackEntryElement in ReadElements(Root.Offset, Identifier.MatroskaTrackEntry).ToArray())
            {
                using (var stream = trackEntryElement.Data)
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
                            
                            case Identifier.MatroskaTrackVideo: 
                            case Identifier.MatroskaTrackAudio:
                                continue;
                            case Identifier.MatroskaTrackType:
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
                            case Identifier.MatroskaAudioBitDepth:
                                {
                                    bitsPerSample = (byte)stream.ReadByte();
                                    ++offset;
                                    continue;
                                }                           
                            case Identifier.MatroskaAudioChannels:
                                {
                                    channels = (byte)stream.ReadByte();
                                    ++offset;
                                    continue;
                                }
                            case Identifier.MatroskaTrackUID:
                            case Identifier.MatroskaTrackNumber:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    trackId = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaTrackDefaultDuration:
                                {
                                    //Really the sample Rate?
                                    //Number of nanoseconds (not scaled via TimecodeScale) per frame ('frame' in the Matroska sense -- one element put into a (Simple)Block).
                                    stream.Read(buffer, 0, (int)length);
                                    if (mediaType == Sdp.MediaType.video)rate =  Utility.NanosecondsPerSecond / (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    else rate = (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaVideoFrameRate:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    rate = (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    trackDuration = (ulong)(Utility.NanosecondsPerSecond * rate);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaTimeCodeScale:
                            case Identifier.MatroskaTrackTimeCodeScale://DEPRECATED, DO NOT USE. 
                                {
                                    //Really the sample Rate?
                                    //Number of nanoseconds (not scaled via TimecodeScale) per frame ('frame' in the Matroska sense -- one element put into a (Simple)Block).
                                    stream.Read(buffer, 0, (int)length);
                                    timeCodeScale = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaAudioSamplingFreq:
                                {
                                    //Ensure this is read correctly....
                                    stream.Read(buffer, 0, (int)length);
                                    rate = Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaAudioOutputSamplingFreq:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    //Rescale?
                                    rate = (double)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaVideoPixelWidth:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    width = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaVideoPixelHeight:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    height = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaTrackOffset://DEPRECATED, DO NOT USE.
                                {
                                    //A value to add to the Block's Timestamp. This can be used to adjust the playback offset of a track.
                                    stream.Read(buffer, 0, (int)length);
                                    startTime = (ulong)Common.Binary.ReadInteger(buffer, 0, (int)length, length > 1 && BitConverter.IsLittleEndian);
                                    offset += length;
                                    break;
                                }
                            case Identifier.MatroskaTrackName:
                                {
                                    stream.Read(buffer, 0, (int)length);
                                    trackName = Encoding.UTF8.GetString(buffer, 0, (int)length);
                                    offset += length;
                                    continue;
                                }
                            case Identifier.MatroskaCodecName:
                            case Identifier.MatroskaCodecID:
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

                    //Need to find all CueTimes to accurately describe duration and start time and sample count...
                    //Matroska is WONDERFUL                    
                    //Only do this one time for now...
                    if(sampleCount == 0) foreach (var elem in ReadElements(0, Identifier.MatroskaCues))
                    {
                        using (var cueStream = elem.Data)
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
                                    case Identifier.MatroskaCuePoint: continue;
                                    case Identifier.MatroskaCueTime:
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

                    trackId = trackDuration = height = width = startTime =  0;

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
