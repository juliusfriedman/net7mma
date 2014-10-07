using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Riff
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
            //File Types
            AVI = 541677121,
            AVIX = 1481201217,
            AVI_ = 424236609,
            AVIF = 1179211329,
            ON2f = 1714572879,
            AMV = 542526785,
            WAVE = 1163280727,
            RMID = 1145654610,
            //Types
            LIST = 1414744396,
            HDLR = 1919706216,
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

        //[Flags]
        //internal enum MainHeaderFlags : uint
        //{
        //    HasIndex = 0x00000010U,
        //    MustUseIndex = 0x00000020U,
        //    IsInterleaved = 0x00000100U,
        //    TrustChunkType = 0x00000800U,
        //    WasCaptureFile = 0x00010000U,
        //    Copyrighted = 0x000200000U,
        //}

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
                throw new Exception("FourCC strings must be 4 characters long " + FourCC);
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
                throw new Exception("FourCC char arrays must be 4 characters long " + new string(FourCC));
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

        #endregion        

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return FromFourCC(ToFourCC(Array.ConvertAll<byte, char>(identifier.Skip(offset).Take(count).ToArray(), Convert.ToChar))); }

        public RiffReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public RiffReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Node> ReadChunks(long offset = 0, params FourCharacterCode[] names) { return ReadChunks(offset, Array.ConvertAll<FourCharacterCode, int>(names, value => (int) value)); }

        public IEnumerable<Node> ReadChunks(long offset = 0, params int[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var chunk in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(Common.Binary.Read32(chunk.Identifier, 0, !BitConverter.IsLittleEndian)))
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

        public Node ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            bool complete = true;

            byte[] identifier = new byte[IdentifierSize];

            complete = (IdentifierSize == Read(identifier, 0, IdentifierSize));

            byte[] lengthBytes = new byte[LengthSize];

            complete = LengthSize == Read(lengthBytes, 0, LengthSize);

            long length = Common.Binary.Read32(lengthBytes, 0, !BitConverter.IsLittleEndian);

            //Calculate padded size (to word boundary)
            if (0 != (length & 1)) ++length;

            FourCharacterCode name = (FourCharacterCode)Common.Binary.Read32(identifier, 0, !BitConverter.IsLittleEndian);

            //Certain tags do not take the length into account

            switch(name)
            {
                default : break;
                //case FourCharacterCode.INFO:
                case FourCharacterCode.IDIT:
                case FourCharacterCode.ISFT:
                case FourCharacterCode.INAM:
                case FourCharacterCode.ISTR:
                case FourCharacterCode.IART:
                case FourCharacterCode.IWRI:
                case FourCharacterCode.ICMT:
                case FourCharacterCode.IGRN:
                case FourCharacterCode.ICRD:
                case FourCharacterCode.IPRT:
                case FourCharacterCode.IFRM:
                case FourCharacterCode.ICOP:
                case FourCharacterCode.TITL:
                case FourCharacterCode.COMM:
                case FourCharacterCode.GENR:
                case FourCharacterCode.PRT1:
                case FourCharacterCode.PRT2:
                    //
                case FourCharacterCode.LIST:
                    offset = Position;
                    break;
            }

            return  new Node(this, identifier, offset, length, length <= Remaining);
        }


        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > TWODWORDSSIZE)
            {
                Node next = ReadNext();

                if (next == null) yield break;
                
                FourCharacterCode fourCC = (FourCharacterCode)Common.Binary.Read32(next.Identifier, 0, !BitConverter.IsLittleEndian);

                switch (fourCC)
                {
                    case FourCharacterCode.RIFF:
                    case FourCharacterCode.RIFX:
                    case FourCharacterCode.ON2:
                    case FourCharacterCode.LIST:
                        Skip(IdentifierSize);
                        break;
                    default:
                        Skip(next.Size);
                        break;
                }


                yield return next;
            }
        }      

        public override Node Root
        {
            get
            {
                long position = Position;
                Node root = ReadChunks(0, FourCharacterCode.RIFF, FourCharacterCode.RIFX).FirstOrDefault();
                Position = position;
                return root;
            }
        }


        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (!m_Created.HasValue) ParseIdentity();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (!m_Modified.HasValue) ParseIdentity();
                return m_Modified.Value;
            }
        }

        void ParseIdentity()
        {
            using (var iditChunk = ReadChunk(FourCharacterCode.IDIT, Root.Offset))
            {
                if (iditChunk != null)
                {
                    int month = 0, day = 0, year = 0;
                    TimeSpan time = TimeSpan.Zero;

                    var parts = Encoding.UTF8.GetString(iditChunk.Raw).Split(' ');

                    if (parts.Length > 1) month = DateTime.ParseExact(parts[1], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;

                    if (parts.Length > 1) day = int.Parse(parts[2]);

                    if (parts.Length > 2) time = TimeSpan.Parse(parts[3]);

                    if (parts.Length > 4) year = int.Parse(parts[4]);
                    else year = DateTime.Now.Year; //BaseDate?

                    m_Created = new DateTime(year, month, day, time.Hours, time.Minutes, time.Seconds, DateTimeKind.Utc);
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
                if (!m_MicroSecPerFrame.HasValue) ParseAviHeader();
                return m_MicroSecPerFrame.Value;
            }
        }

        public int MaxBytesPerSecond
        {
            get
            {
                if (!m_MaxBytesPerSec.HasValue) ParseAviHeader();
                return m_MaxBytesPerSec.Value;
            }
        }

        public int PaddingGranularity
        {
            get
            {
                if (!m_PaddingGranularity.HasValue) ParseAviHeader();
                return m_PaddingGranularity.Value;
            }
        }

        /*
         AVIF_HASINDEX, MUSTUSEINDEX, ISINTERLEAVED, WASCAPTUREFILE, COPYRIGHTED, TRUSTCKTYPE (OPEN DML ONLY)
         */

        public int Flags
        {
            get
            {
                if (!m_Flags.HasValue) ParseAviHeader();
                return m_Flags.Value;
            }
        }

        public int TotalFrames
        {
            get
            {
                if (!m_TotalFrames.HasValue) ParseAviHeader();
                return m_TotalFrames.Value;
            }
        }

        public int InitialFrames
        {
            get
            {
                if (!m_InitialFrames.HasValue) ParseAviHeader();
                return m_InitialFrames.Value;
            }
        }

        public int SuggestedBufferSize
        {
            get
            {
                if (!m_SuggestedBufferSize.HasValue) ParseAviHeader();
                return m_SuggestedBufferSize.Value;
            }
        }

        public int Streams
        {
            get
            {
                if (!m_Streams.HasValue) ParseAviHeader();
                return m_Streams.Value;
            }
        }

        public int Width
        {
            get
            {
                if (!m_Width.HasValue) ParseAviHeader();
                return m_Width.Value;
            }
        }

        public int Height
        {
            get
            {
                if (m_Height.HasValue) ParseAviHeader();
                return m_Height.Value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (!m_TotalFrames.HasValue) ParseAviHeader();
                return TimeSpan.FromMilliseconds((double)m_TotalFrames.Value * m_MicroSecPerFrame.Value / (double)Utility.MicrosecondsPerMillisecond);
            }
        }

        public int Reserved
        {
            get
            {
                if (!m_Reserved.HasValue) ParseAviHeader();
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
            using (var headerChunk = ReadChunk(FourCharacterCode.avih, Root.Offset))
            {
                int offset = IdentifierSize + LengthSize;

                m_MicroSecPerFrame = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_MaxBytesPerSec = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_PaddingGranularity = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);
                
                offset += 4;

                m_Flags = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_TotalFrames = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_InitialFrames = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_Streams = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_SuggestedBufferSize = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_Width = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_Height = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                m_Reserved = Common.Binary.Read32(headerChunk.Raw, offset, !BitConverter.IsLittleEndian);
            }            
        }

        //Could check hasIndex flag

        public override Node TableOfContents
        {
            get { return ReadChunks(Root.Offset, FourCharacterCode.idx1, FourCharacterCode.indx).FirstOrDefault(); }
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
            foreach (var chunk in ReadChunks(Root.Offset, FourCharacterCode.strh).ToArray())
            {
                int offset = MinimumSize, sampleCount = TotalFrames, startTime = 0, timeScale = 0, duration = (int)Duration.TotalMilliseconds, width = Width, height = Height, rate = MicrosecondsPerFrame;

                string trackName = string.Empty;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Utility.Empty;

                byte channels = 0, bitDepth = 0;

                //sampleCount comes from lists with a wb or wc or wd id?

                //streamName comes from  "strn"

                //Expect 56 Bytes

                FourCharacterCode fccType = (FourCharacterCode)Common.Binary.Read32(chunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                switch (fccType)
                {
                    case FourCharacterCode.iavs:
                        {
                            //Interleaved Audio and Video
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
                codecIndication = chunk.Raw.Skip(offset).Take(4).ToArray();

                offset += 4 + (DWORDSIZE * 3);

                //Scale
                timeScale = Common.Binary.Read32(chunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                //Rate
                rate = Common.Binary.Read32(chunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                if (!(timeScale > 0 && rate > 0))
                {
                    rate = 25;
                    timeScale = 1;
                }

                //Start
                startTime = Common.Binary.Read32(chunk.Raw, offset, !BitConverter.IsLittleEndian);

                offset += 4;

                //Length of stream (as defined in rate and timeScale above)
                duration = Common.Binary.Read32(chunk.Raw, offset, !BitConverter.IsLittleEndian);

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
                            var strf = ReadChunk(FourCharacterCode.strf, chunk.Offset);

                            if (strf != null)
                            {
                                //BitmapInfoHeader
                                //Read 32 Width
                                width = (int)Common.Binary.ReadU32(strf.Raw, 12, !BitConverter.IsLittleEndian);

                                //Read 32 Height
                                height = (int)Common.Binary.ReadU32(strf.Raw, 16, !BitConverter.IsLittleEndian);

                                //Maybe...
                                //Read 16 panes 

                                //Read 16 BitDepth
                                bitDepth = (byte)(int)Common.Binary.ReadU16(strf.Raw, 22, !BitConverter.IsLittleEndian);

                                //Read codec
                                codecIndication = strf.Raw.Skip(24).Take(4).ToArray();
                            }
                            break;
                        }
                    case Sdp.MediaType.audio:
                        {
                            //Expand Codec Indication based on iD?

                            var strf = ReadChunk(FourCharacterCode.strf, chunk.Offset);

                            if (strf != null)
                            {
                                //WaveFormat (EX) 
                                channels = (byte)Common.Binary.ReadU16(strf.Raw, 10, !BitConverter.IsLittleEndian);
                                bitDepth = (byte)Common.Binary.ReadU16(strf.Raw, 12, !BitConverter.IsLittleEndian);
                            }
                            break;
                        }
                    //text format....
                    default: break;
                }

                var strn = ReadChunk(FourCharacterCode.strn, chunk.Offset);

                if (strn != null) trackName = Encoding.UTF8.GetString(strn.Raw, 8, (int)(strn.Size - 8));

                //Hackup, should only get types based on media... right now just using all types
                sampleCount = ReadChunks(Root.Offset, ToFourCC(trackId.ToString("D2") + "dc"), ToFourCC(trackId.ToString("D2") + "wb"), ToFourCC(trackId.ToString("D2") + "tx"), ToFourCC(trackId.ToString("D2") + "ix")).Count();

                //Variable BitRate must also take into account the size of each chunk / nBlockAlign * duration per frame.

                Track created = new Track(chunk, trackName, ++trackId, Created, Modified, sampleCount, height, width,
                    TimeSpan.FromMilliseconds(startTime / timeScale),
                    mediaType == Sdp.MediaType.audio ?
                        TimeSpan.FromSeconds((double)duration / (double)rate) :
                        TimeSpan.FromMilliseconds((double)duration * m_MicroSecPerFrame.Value / (double)Utility.MicrosecondsPerMillisecond),
                    rate / timeScale, mediaType, codecIndication, channels, bitDepth);

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
