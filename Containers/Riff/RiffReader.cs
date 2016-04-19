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
using Media.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Containers.Riff
{
    /// <summary>
    /// Represents the logic necessary to read files in the Resource Interchange File Format Format (.avi)
    /// </summary>
    /// <notes>
    /// <see href="http://www.alexander-noe.com/video/documentation/avi.pdf">Extremely Helpful</see>
    /// </notes>
    public class RiffReader : MediaFileStream, IMediaContainer
    {

        #region FourCharacterCode

        //Chunk Types?
        public enum FourCharacterCode
        {
            //File Headers
            RIFF = 1179011410,
            RIFX = 1481001298,
            ON2 = 540167759,
            odml = 1819108463,
            //AVI Header
            avih = 1751742049,
            //Extended Header
            dmlh = 1751936356,
            ds64 = 875983716,
            //File Types
            AVI = 541677121,
            AVIX = 1481201217,
            AVI_ = 424236609,
            AVIF = 1179211329,
            ON2f = 1714572879,
            AMV = 542526785,
            WAVE = 1163280727,
            RF64 = 875972178,
            RMID = 1145654610,
            //Types
            LIST = 1414744396,
            hdlr = 1919706216,
            rec = 543384946,
            //Chunks
            JUNK = 1263424842,
            ISMP = 1347244873, //Timecode
            INFO = 1330007625,
            IDIT = 1414087753,
            INAM = 1296125513,
            ISTR = 1381258057,
            ISFT = 1413894985,
            IART = 1414676809,
            IWRI = 1230133065,
            ICMT = 1414349641,
            IGRN = 1314015049,
            ICRD = 1146241865,
            IPRT = 1414680649,
            IFRM = 1297237577,
            ICOP = 1347371849,
            //MovieId
            MID = 541346125,
            TITL = 1280592212,
            COMM = 1296912195,
            GENR = 1380861255,
            PRT1 = 827609680,
            PRT2 = 844386896,
            nctg = 1735680878,  //Nikon Tags
            CASI =  1230192963, //CASIO
            Zora = 1634889562,  //
            //Stream Chunks
            movi = 1769369453,
            strh = 1752331379,
            strf = 1718776947,
            strl = 1819440243,
            strn = 1852994675,
            strd = 1685222515,
            //Extended Video Properties
            vprp = 1886548086,
            //Sample Chunks
            dc = 1667510000, //DIB (Video)
            db = 1650730000, //DIBCompressed
            wb = 1651970000, //WaveBytes (Audio)
            tx = 2020880000, //Text
            ix = 2020150000, //Index
            pc = 1668290000, //PalChange
            //Index Chunks
            idx1 = 829973609,
            indx = 2019847785,
            //Stream Types
            iavs = 1937138025, //Interleaved Audio + Video
            vids = 1935960438,
            auds = 1935963489,
            data = 1635017060,
            mids = 1935960429,
            txts = 1937012852
        }

        #endregion

        [Flags]
        internal enum MainHeaderFlags : uint
        {
            HasIndex = 0x00000010U,
            MustUseIndex = 0x00000020U,
            IsInterleaved = 0x00000100U,
            TrustChunkType = 0x00000800U,
            WasCaptureFile = 0x00010000U,
            Copyrighted = 0x000200000U,
        }

        //internal enum IndexType : byte
        //{
        //    Indexes = 0x00,
        //    Chunks = 0x01,
        //    Data = 0x80,
        //}

        #region Constants

        public const int DWORDSIZE = 4, TWODWORDSSIZE = 8, MinimumSize = TWODWORDSSIZE, IdentifierSize = DWORDSIZE, LengthSize = DWORDSIZE;

        #endregion

        #region FourCC conversion methods

        //string GetSubType?

        public static string FromFourCC(int FourCC)
        {
            char[] chars = new char[4];
            chars[0] = (char)(FourCC & 0xFF);
            chars[1] = (char)((FourCC >> 8) & 0xFF);
            chars[2] = (char)((FourCC >> 16) & 0xFF);
            chars[3] = (char)((FourCC >> 24) & 0xFF);

            return new string(chars);
        }

        public static int ToFourCC(string FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC strings must be [exactly] 4 characters long " + FourCC);
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char[] FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC char arrays must be [exactly] 4 characters long " + new string(FourCC));
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char c0, char c1, char c2, char c3)
        {
            int result = ((int)c3) << 24
                        | ((int)c2) << 16
                        | ((int)c1) << 8
                        | ((int)c0);

            return result;
        }

        public static int ToFourCC(byte c0, byte c1, byte c2, byte c3) { return ToFourCC((char)c0, (char)c1, (char)c2, (char)c3); }

        public static bool HasSubType(FourCharacterCode fourCC)
        {
            return RiffReader.ParentChunks.Contains(fourCC);
        }

        public static readonly HashSet<FourCharacterCode> ParentChunks = new HashSet<FourCharacterCode>()
        {
            FourCharacterCode.RIFF,
            FourCharacterCode.RIFX, 
            FourCharacterCode.RF64, 
            FourCharacterCode.ON2,
            FourCharacterCode.odml,
            FourCharacterCode.LIST,
        };

        public static bool HasSubType(Node chunk)
        {

            if (chunk == null) throw new ArgumentNullException("chunk");

            FourCharacterCode fourCC = (FourCharacterCode)ToFourCC(chunk.Identifier[0], chunk.Identifier[1], chunk.Identifier[2], chunk.Identifier[3]);

            return HasSubType(fourCC);

            //switch(fourCC)
            //{
            //    case FourCharacterCode.RIFF:
            //    case FourCharacterCode.RIFX:
            //    case FourCharacterCode.RF64:
            //    case FourCharacterCode.ON2:
            //    case FourCharacterCode.odml:
            //    case FourCharacterCode.LIST:
            //        return true;
            //    default:
            //        return false;
            //}
        }

        public static FourCharacterCode GetSubType(Node chunk)
        {
            if (chunk == null) throw new ArgumentNullException("chunk");

            return (FourCharacterCode)(HasSubType(chunk) ? ToFourCC(chunk.Identifier[4], chunk.Identifier[5], chunk.Identifier[6], chunk.Identifier[7]) : ToFourCC(chunk.Identifier[0], chunk.Identifier[1], chunk.Identifier[2], chunk.Identifier[3]));
        }

        #endregion        

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4)
        {
            //May have different results on different systems...
            return FromFourCC(ToFourCC(Array.ConvertAll<byte, char>(identifier.Skip(offset).Take(count).ToArray(), Convert.ToChar)));
        }

        public RiffReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public RiffReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public RiffReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public RiffReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, null, bufferSize, true) { }        

        public IEnumerable<Node> ReadChunks(long offset = 0, params FourCharacterCode[] names)
        {
            return ReadChunks(offset, Array.ConvertAll<FourCharacterCode, int>(names, value => (int)value));
        }

        //Should have count but there is no indication where strh can occur

        public IEnumerable<Node> ReadChunks(long offset = 0, params int[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var chunk in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(Common.Binary.Read32(chunk.Identifier, 0, Media.Common.Binary.IsBigEndian)))
                {
                    yield return chunk;
                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Node ReadChunk(string name, long offset = 0) { return ReadChunk((FourCharacterCode)ToFourCC(name), offset); }

        public Node ReadChunk(FourCharacterCode name, long offset = 0)
        {
            long positionStart = Position;

            Node result = ReadChunks(offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        //Typically found in the ds64 chunk.
        ulong m_DataSize;

        /// <summary>
        /// Gets the size to use when a node with length == 0xFFFFFFFF is found.
        /// </summary>
        public long DataSize
        {
            get { return (long)m_DataSize; }
            internal protected set { m_DataSize = (ulong)value; }
        }

        //Determined by the first call to ReadNext.
        bool? m_Needs64BitInfo;

        /// <summary>
        /// Indicates if the file has a header chunk which has additional information about the data contained.
        /// </summary>
        public bool Has64BitHeader
        {
            get
            {
                //Call Root to call ReadNext which sets m_Needs64BitInfo.Value the first time.
                if (false == m_Needs64BitInfo.HasValue) return Root != null && m_Needs64BitInfo.Value;

                //Return the known value.
                return m_Needs64BitInfo.Value;
            }
            internal protected set
            {
                m_Needs64BitInfo = value;
            }
        }

        public Node ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();
            
            byte[] identifier = new byte[IdentifierSize];

            byte[] lengthBytes = new byte[LengthSize];

            int read = Read(identifier, 0, IdentifierSize);

            read += Read(lengthBytes, 0, LengthSize);

            ulong length = (ulong)Common.Binary.Read32(lengthBytes, 0, Media.Common.Binary.IsBigEndian);

            int identifierSize = IdentifierSize;

            //Get the fourCC of the node
            FourCharacterCode fourCC = (FourCharacterCode)Common.Binary.Read32(identifier, 0, Media.Common.Binary.IsBigEndian);

            //Determine if 64 bit support is needed by inspecting the first node encountered.
            if (false == m_Needs64BitInfo.HasValue)
            {
                //There may be other nodes to account for also...
                m_Needs64BitInfo = fourCC == FourCharacterCode.RF64;
            }

            //Determine if an identifier follows
            if(RiffReader.HasSubType(fourCC))
            {
                //Resize the identifier to make room for the sub type
                Array.Resize(ref identifier, MinimumSize);

                //Read the sub type
                read += Read(identifier, IdentifierSize, IdentifierSize);

                //Not usually supposed to read the identifier
                length -= IdentifierSize;

                //Adjust for the bytes read.
                identifierSize += IdentifierSize;
            }

            //If this is a 64 bit entry
            if (length == uint.MaxValue)
            {
                //use the dataSize (0 for the first node, otherwise whatever was found)
                length = m_DataSize;

                //There are so may ways to handle this it's not funny, this seems to the most documented but probably one of the ugliest.
                //Not to mention this doesn't really give you compatiblity and doesn't contain a failsafe.

                //If files can be found which still don't work I will adjust this logic as necessary.
            }

            //return a new node,                                             Calculate length as padded size (to word boundary)
            return new Node(this, new Common.MemorySegment(identifier), identifierSize, LengthSize, Position, (long)(0 != (length & 1) ? ++length : length), 
                read >= MinimumSize && length <= (ulong)Remaining); //determine Complete
        }


        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > TWODWORDSSIZE)
            {
                Node next = ReadNext();

                if (next == null) yield break;
                               
                yield return next;

                if (m_Needs64BitInfo.Value && //If the file needs information from the ds64 node
                    //The value must not have been read before and not found to be 0
                    m_DataSize == 0 && 
                    //There must be at least 28 bytes in a junk / ds64 chunk
                    next.DataSize >= 28 &&
                    //This is the ds64 chunk
                    FourCharacterCode.ds64 == (FourCharacterCode)Common.Binary.Read32(next.Identifier, 0, Media.Common.Binary.IsBigEndian))
                {

                    m_DataSize = (ulong)Common.Binary.Read64(next.Data, MinimumSize, Media.Common.Binary.IsBigEndian);

                    //if this is found to be == 0 then what?

                    /*
                     struct DataSize64Chunk // declare DataSize64Chunk structure
                    {
                     * next.Identifier[0]
                    char chunkId[4]; // ‘ds64’
                     * Not stored
                    unsigned int32 chunkSize; // 4 byte size of the ‘ds64’ chunk
                     * next.Data[0]
                    unsigned int32 riffSizeLow; // low 4 byte size of RF64 block
                    unsigned int32 riffSizeHigh; // high 4 byte size of RF64 block
                    unsigned int32 dataSizeLow; // low 4 byte size of data chunk
                    unsigned int32 dataSizeHigh; // high 4 byte size of data chunk
                    unsigned int32 sampleCountLow; // low 4 byte sample count of fact chunk
                    unsigned int32 sampleCountHigh; // high 4 byte sample count of fact chunk
                    unsigned int32 tableLength; // number of valid entries in array “table”
                    chunkSize64 table[ ];
                    };
                     */
                }

                //If this is a list parse into the list
                if (HasSubType(next)) continue;
                //Otherwise skip the data of the chunk
                else Skip(next.DataSize);
            }
        }        

        public override Node Root
        {
            get
            {
                long position = Position;
                
                Node root = ReadChunks(0, FourCharacterCode.RIFF, FourCharacterCode.RIFX, FourCharacterCode.RF64, FourCharacterCode.ON2, FourCharacterCode.odml).FirstOrDefault();
                
                Position = position;                

                return root;
            }
        }

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return RiffReader.ToFourCharacterCode(node.Identifier);
            return base.ToTextualConvention(node);
        }


        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (false == m_Created.HasValue) ParseIdentity();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (false == m_Modified.HasValue) ParseIdentity();
                return m_Modified.Value;
            }
        }

        void ParseIdentity()
        {
            using (var iditChunk = ReadChunk(FourCharacterCode.IDIT, Root.Offset))
            {
                if (iditChunk != null)
                {
                    //Store the creation time.
                    DateTime createdDateTime = FileInfo.CreationTimeUtc;

                    int day = 0, year = 0;

                    TimeSpan time = TimeSpan.Zero;

                    //parts of the date in string form
                    var parts = Encoding.UTF8.GetString(iditChunk.Data).Split((char)Common.ASCII.Space);

                    //cache the split length
                    int partsLength = parts.Length;

                    //If there are parts
                    if (partsLength > 0)
                    {
                        //Thanks bartmeirens!

                        //try parsing with current culture
                        if (false == DateTime.TryParseExact(parts[1], "MMM", System.Globalization.CultureInfo.CurrentCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out createdDateTime))
                        {
                            //: parse using invariant (en-US)
                            if(false == DateTime.TryParseExact(parts[1], "MMM", System.Globalization.CultureInfo.CurrentCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out createdDateTime))
                            {
                                //The month portion of the result contains the data, the rest is blank
                                createdDateTime = FileInfo.CreationTimeUtc;
                            }
                        }

                        if (partsLength > 1) day = int.Parse(parts[2]);
                        else day = FileInfo.CreationTimeUtc.Day;

                        if (partsLength > 2)
                        {
                            if (false == TimeSpan.TryParse(parts[3], out time))
                            {
                                time = FileInfo.CreationTimeUtc.TimeOfDay;
                            }
                        }
                        else time = FileInfo.CreationTimeUtc.TimeOfDay;

                        if (partsLength > 4) year = int.Parse(parts[4]);
                        else year = FileInfo.CreationTimeUtc.Year;

                        m_Created = new DateTime(year, createdDateTime.Month, day, time.Hours, time.Minutes, time.Seconds, DateTimeKind.Utc);
                    }
                    else m_Created = FileInfo.CreationTimeUtc;
                }
                else m_Created = FileInfo.CreationTimeUtc;
            }

            m_Modified = FileInfo.LastWriteTimeUtc;
        }

        int? m_MicroSecPerFrame, m_MaxBytesPerSec, m_PaddingGranularity, m_Flags, m_TotalFrames, m_InitialFrames, m_Streams, m_SuggestedBufferSize, m_Width, m_Height, m_Reserved;

        public int MicrosecondsPerFrame
        {
            get
            {
                if (false == m_MicroSecPerFrame.HasValue) ParseAviHeader();
                return m_MicroSecPerFrame.Value;
            }
        }

        public int MaxBytesPerSecond
        {
            get
            {
                if (false == m_MaxBytesPerSec.HasValue) ParseAviHeader();
                return m_MaxBytesPerSec.Value;
            }
        }

        public int PaddingGranularity
        {
            get
            {
                if (false == m_PaddingGranularity.HasValue) ParseAviHeader();
                return m_PaddingGranularity.Value;
            }
        }

        public int Flags
        {
            get
            {
                if (false == m_Flags.HasValue) ParseAviHeader();
                return m_Flags.Value;
            }
        }

        public bool HasIndex { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.HasIndex); } }

        public bool MustUseIndex { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.MustUseIndex); } }

        public bool IsInterleaved { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.IsInterleaved); } }

        public bool TrustChunkType { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.TrustChunkType); } }

        public bool WasCaptureFile { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.WasCaptureFile); } }

        public bool Copyrighted { get { return ((MainHeaderFlags)Flags).HasFlag(MainHeaderFlags.Copyrighted); } }

        public int TotalFrames
        {
            get
            {
                if (false == m_TotalFrames.HasValue) ParseAviHeader();
                return m_TotalFrames.Value;
            }
        }

        public int InitialFrames
        {
            get
            {
                if (false == m_InitialFrames.HasValue) ParseAviHeader();
                return m_InitialFrames.Value;
            }
        }

        public int SuggestedBufferSize
        {
            get
            {
                if (false == m_SuggestedBufferSize.HasValue) ParseAviHeader();
                return m_SuggestedBufferSize.Value;
            }
        }

        public int Streams
        {
            get
            {
                if (false == m_Streams.HasValue) ParseAviHeader();
                return m_Streams.Value;
            }
        }

        public int Width
        {
            get
            {
                if (false == m_Width.HasValue) ParseAviHeader();
                return m_Width.Value;
            }
        }

        public int Height
        {
            get
            {
                if (false == m_Height.HasValue) ParseAviHeader();
                return m_Height.Value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (false == m_TotalFrames.HasValue) ParseAviHeader();
                return TimeSpan.FromMilliseconds((double)m_TotalFrames.Value * m_MicroSecPerFrame.Value / (double)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);
            }
        }

        public int Reserved
        {
            get
            {
                if (false == m_Reserved.HasValue) ParseAviHeader();
                return m_Reserved.Value;
            }
        }

        //void ParseInformation()
        //{

        //    //Should take a Node infoChunk and return a String[]?

        //    //Parse INFO (Should allow list filtering in read chunk)
        //    using (var chunk = ReadChunk(FourCharacterCode.INFO, Root.Offset))
        //    {
        //        //Two ways, either get a Chunk of info and look the stream of get the tags you want using ReadChunk
        //        using (var stream = chunk.Data)
        //        {
        //            //ISFT - Software
        //            //INAM - Title
        //            //ISTR - Performers 
        //            //IART - AlbumArtists 
        //            //IWRI - Composers 
        //            //ICMT - Comment
        //            //IGRN - Geners
        //            //ICRD - Year
        //            //IPRT - Track
        //            //IFRM - TrackCount
        //            //ICOP - Copyright 
        //        }
        //    }
        //}

        //void ParseMoiveId()
        //{
        //    //MID - MOVIE ID
        //    //Parse IART - Performers
        //    //Parse TITL - Title
        //    //Parse COMM - Comment 
        //    //Parse GENR - Genres  
        //    //Parse PRT1 - Track  
        //    //Parse PRT2 - TrackCount
        //}

        //void ParseOdmlHeader() { /*Total Number of Frames in File?*/ }

        void ParseAviHeader()
        {
            //Must be present!
            using (var headerChunk = ReadChunk(FourCharacterCode.avih, Root.Offset))
            {
                if (headerChunk == null) throw new InvalidOperationException("no 'avih' Chunk found");

                int offset = 0;

                m_MicroSecPerFrame = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_MaxBytesPerSec = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_PaddingGranularity = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);
                
                m_Flags = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_TotalFrames = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_InitialFrames = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_Streams = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_SuggestedBufferSize = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_Width = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_Height = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);

                m_Reserved = Common.Binary.Read32(headerChunk.Data, ref offset, Media.Common.Binary.IsBigEndian);
            }            
        }

        /// <summary>
        /// If <see cref="HasIndex"/> then either the 'idx1' or 'indx' chunk, otherwise the 'avhi', 'dmlh' or 'ds64' chunk.
        /// </summary>
        public override Node TableOfContents
        {
            get { return HasIndex ? ReadChunks(Root.Offset, FourCharacterCode.idx1, FourCharacterCode.indx).FirstOrDefault() : ReadChunks(Root.Offset, FourCharacterCode.avih, FourCharacterCode.dmlh, FourCharacterCode.ds64).FirstOrDefault(); }
        }

        //Index1Entry
        //Bool isKeyFram
        //Index--
        //Offset 
        //Size

        IEnumerable<Track> m_Tracks;

        public override IEnumerable<Track> GetTracks()
        {

            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }

            long position = Position;

            var tracks = new List<Track>();

            int trackId = 0;

            //strh has all track level info, strn has stream name..
            foreach (var strhChunk in ReadChunks(Root.Offset, FourCharacterCode.strh).ToArray())
            {
                int offset = 0, sampleCount = TotalFrames, startTime = 0, timeScale = 0, duration = (int)Duration.TotalMilliseconds, width = Width, height = Height, rate = MicrosecondsPerFrame;

                string trackName = string.Empty;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Media.Common.MemorySegment.EmptyBytes;

                byte channels = 0, bitDepth = 0;

                //Expect 56 Bytes

                FourCharacterCode fccType = (FourCharacterCode)Common.Binary.Read32(strhChunk.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                switch (fccType)
                {
                    case FourCharacterCode.iavs:
                        {
                            //Interleaved Audio and Video
                            //Should be audio and video samples together....?
                            //Things like this need a Special TrackType, MediaType doens't really cut it.
                            break;
                        }
                    case FourCharacterCode.vids:
                        {
                            //avg_frame_rate = timebase
                            mediaType = Sdp.MediaType.video;

                            sampleCount = ReadChunks(Root.Offset, ToFourCC(trackId.ToString("D2") + FourCharacterCode.dc.ToString()),
                                                                  ToFourCC(trackId.ToString("D2") + FourCharacterCode.db.ToString())).Count();
                            break;
                        }
                    case FourCharacterCode.mids: //Midi
                    case FourCharacterCode.auds:
                        {
                            mediaType = Sdp.MediaType.audio;

                            sampleCount = ReadChunks(Root.Offset, ToFourCC(trackId.ToString("D2") + FourCharacterCode.wb.ToString())).Count();

                            break;
                        }
                    case FourCharacterCode.txts:
                        {
                            sampleCount = ReadChunks(Root.Offset, ToFourCC(trackId.ToString("D2") + FourCharacterCode.tx.ToString())).Count();
                            mediaType = Sdp.MediaType.text; break;
                        }
                    case FourCharacterCode.data:
                        {
                            mediaType = Sdp.MediaType.data; break;
                        }
                    default: break;
                }

                //fccHandler
                codecIndication = strhChunk.Data.Skip(offset).Take(4).ToArray();

                offset += 4 + (DWORDSIZE * 3);

                //Scale
                timeScale = Common.Binary.Read32(strhChunk.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                //Rate
                rate = Common.Binary.Read32(strhChunk.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                //Defaults??? Should not be hard coded....
                if (false == (timeScale > 0 && rate > 0))
                {
                    rate = 25;
                    timeScale = 1;
                }

                //Start
                startTime = Common.Binary.Read32(strhChunk.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                //Length of stream (as defined in rate and timeScale above)
                duration = Common.Binary.Read32(strhChunk.Data, offset, Media.Common.Binary.IsBigEndian);

                offset += 4;

                //SuggestedBufferSize

                //Quality

                //SampleSize

                //RECT rcFrame (ushort left, top, right, bottom)

                //Get strf for additional info.

                switch (mediaType)
                {
                    case Sdp.MediaType.video:
                        {
                            using (var strf = ReadChunk(FourCharacterCode.strf, strhChunk.Offset))
                            {
                                if (strf != null)
                                {
                                    //BitmapInfoHeader
                                    //Read 32 Width
                                    width = (int)Common.Binary.ReadU32(strf.Data, 4, Media.Common.Binary.IsBigEndian);

                                    //Read 32 Height
                                    height = (int)Common.Binary.ReadU32(strf.Data, 8, Media.Common.Binary.IsBigEndian);

                                    //Maybe...
                                    //Read 16 panes 

                                    //Read 16 BitDepth
                                    bitDepth = (byte)(int)Common.Binary.ReadU16(strf.Data, 14, Media.Common.Binary.IsBigEndian);

                                    //Read codec
                                    codecIndication = strf.Data.Skip(16).Take(4).ToArray();
                                }
                            }

                            break;
                        }
                    case Sdp.MediaType.audio:
                        {
                            //Expand Codec Indication based on iD?

                            using (var strf = ReadChunk(FourCharacterCode.strf, strhChunk.Offset))
                            {
                                if (strf != null)
                                {
                                    //WaveFormat (EX) 
                                    codecIndication = strf.Data.Take(2).ToArray();
                                    channels = (byte)Common.Binary.ReadU16(strf.Data, 2, Media.Common.Binary.IsBigEndian);
                                    bitDepth = (byte)Common.Binary.ReadU16(strf.Data, 4, Media.Common.Binary.IsBigEndian);
                                }
                            }

                            
                            break;
                        }
                    //text format....
                    default: break;
                }

                using (var strn = ReadChunk(FourCharacterCode.strn, strhChunk.Offset))
                {
                    if (strn != null) trackName = Encoding.UTF8.GetString(strn.Data, 8, (int)(strn.DataSize - 8));

                    //Variable BitRate must also take into account the size of each chunk / nBlockAlign * duration per frame.

                    Track created = new Track(strhChunk, trackName, ++trackId, Created, Modified, sampleCount, height, width,
                        TimeSpan.FromMilliseconds(startTime / timeScale),
                        mediaType == Sdp.MediaType.audio ?
                            TimeSpan.FromSeconds((double)duration / (double)rate) :
                            TimeSpan.FromMilliseconds((double)duration * m_MicroSecPerFrame.Value / (double)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond),
                        rate / timeScale, mediaType, codecIndication, channels, bitDepth);

                    yield return created;

                    tracks.Add(created);
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
