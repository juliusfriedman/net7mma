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
    public class RiffReader : MediaFileStream, IMediaContainer
    {

        #region CONSTANTS

        public const int DWORDSIZE = 4;
        public const int TWODWORDSSIZE = 8;
        public static readonly string RIFF4CC = "RIFF";
        public static readonly string RIFX4CC = "RIFX";
        public static readonly string ON24CC = "ON2 ";
        public static readonly string LIST4CC = "LIST";
        public static readonly string HDLR4CC = "hdlr";

        // Known file types
        public static readonly int ckidAVI = ToFourCC("AVI ");
        public static readonly int ckidWAV = ToFourCC("WAVE");
        public static readonly int ckidRMID = ToFourCC("RMID");


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

        #endregion

        static int MinimumSize = TWODWORDSSIZE, IdentifierSize = DWORDSIZE, LengthSize = DWORDSIZE;

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return FromFourCC(ToFourCC(Array.ConvertAll<byte, char>(identifier.Skip(offset).Take(count).ToArray(), Convert.ToChar))); }

        public RiffReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public RiffReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Element> ReadChunks(long offset = 0, params string[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var chunk in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(ToFourCharacterCode(chunk.Identifier)))
                {
                    yield return chunk;
                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Element ReadChunk(string name, long offset = 0)
        {
            long positionStart = Position;

            Element result = ReadChunks(offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public Element ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            bool complete = true;

            byte[] identifier = new byte[IdentifierSize];

            complete = (IdentifierSize == Read(identifier, 0, IdentifierSize));

            byte[] lengthBytes = new byte[LengthSize];

            complete = LengthSize == Read(lengthBytes, 0, LengthSize);

            long length = Common.Binary.Read32(lengthBytes, 0, false);

            //Calculate padded size (to word boundary)
            if (0 != (length & 1)) ++length;
            
            complete = length <= Remaining;

            return  new Element(this, identifier, offset, length, complete);
        }


        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > TWODWORDSSIZE)
            {
                Element next = ReadNext();
                
                //To use binary comparison
                string fourCC = FromFourCC(Common.Binary.Read32(next.Identifier, 0, false));

                if (fourCC == RIFF4CC || fourCC == RIFX4CC || fourCC == ON24CC || fourCC == LIST4CC) Skip(IdentifierSize);
                else Skip(next.Size);

                if (next != null) yield return next;
                else yield break;
            }
        }      

        public override Element Root
        {
            get
            {
                long position = Position;
                Element root = ReadChunks(0, RIFF4CC, RIFX4CC).FirstOrDefault();
                Position = position;
                return root;
            }
        }


        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (m_Created.HasValue) return m_Created.Value;
                ParseIdentity();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (m_Modified.HasValue) return m_Created.Value;
                ParseIdentity();
                return m_Modified.Value;
            }
        }

        void ParseIdentity()
        {
            using (var iditChunk = ReadChunk("IDIT", Root.Offset))
            {
                if (iditChunk != null) using (var stream = iditChunk.Data)
                    {
                        byte[] data = stream.ToArray();

                        int month = 0, day = 0, year = 0;
                        TimeSpan time = TimeSpan.Zero;

                        var parts = Encoding.UTF8.GetString(data, 8, data.Length - 8).Split(' ');

                        if (parts.Length > 1) month = DateTime.ParseExact(parts[1], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;

                        if (parts.Length > 1) day = int.Parse(parts[2]);

                        if (parts.Length > 2) time = TimeSpan.Parse(parts[3]);

                        if (parts.Length > 4) year = int.Parse(parts[4]);
                        else year = DateTime.Now.Year;
                        
                        m_Created = new DateTime(year, month, day, time.Hours, time.Minutes, time.Seconds);
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
                if (m_MicroSecPerFrame.HasValue) return m_MicroSecPerFrame.Value;
                ParseAviHeader();
                return m_MicroSecPerFrame.Value;
            }
        }

        public int MaxBytesPerSecond
        {
            get
            {
                if (m_MaxBytesPerSec.HasValue) return m_MaxBytesPerSec.Value;
                ParseAviHeader();
                return m_MaxBytesPerSec.Value;
            }
        }

        public int PaddingGranularity
        {
            get
            {
                if (m_PaddingGranularity.HasValue) return m_PaddingGranularity.Value;
                ParseAviHeader();
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
                if (m_Flags.HasValue) return m_Flags.Value;
                ParseAviHeader();
                return m_Flags.Value;
            }
        }

        public int TotalFrames
        {
            get
            {
                if (m_TotalFrames.HasValue) return m_TotalFrames.Value;
                ParseAviHeader();
                return m_TotalFrames.Value;
            }
        }

        public int InitialFrames
        {
            get
            {
                if (m_InitialFrames.HasValue) return m_InitialFrames.Value;
                ParseAviHeader();
                return m_InitialFrames.Value;
            }
        }

        public int SuggestedBufferSize
        {
            get
            {
                if (m_SuggestedBufferSize.HasValue) return m_SuggestedBufferSize.Value;
                ParseAviHeader();
                return m_SuggestedBufferSize.Value;
            }
        }

        public int Streams
        {
            get
            {
                if (m_Streams.HasValue) return m_Streams.Value;
                ParseAviHeader();
                return m_Streams.Value;
            }
        }

        public int Width
        {
            get
            {
                if (m_Width.HasValue) return m_Width.Value;
                ParseAviHeader();
                return m_Width.Value;
            }
        }

        public int Height
        {
            get
            {
                if (m_Height.HasValue) return m_Height.Value;
                ParseAviHeader();
                return m_Height.Value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (m_TotalFrames.HasValue) ParseAviHeader();
                return TimeSpan.FromMilliseconds((double)m_TotalFrames.Value * m_MicroSecPerFrame.Value / (double)Utility.MicrosecondsPerMillisecond);
            }
        }

        public int Reserved
        {
            get
            {
                if (m_Reserved.HasValue) return m_Reserved.Value;
                ParseAviHeader();
                return m_Reserved.Value;
            }
        }

        //TODO, Ensure ReadNext is providing all data in the element, Position may have to be augmented by MinimumSize
        //void ParseInformation()
        //{
        //    //Parse INFO 
        //    using (var chunk = ReadChunk("INFO", Root.Offset))
        //    {
        //        using (var stream = chunk.Data)
        //        {
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

        void ParseAviHeader()
        {
            using (var headerChunk = ReadChunk("avih", Root.Offset))
            {
                using (var stream = headerChunk.Data)
                {
                    stream.Position += IdentifierSize + LengthSize;

                    byte[] buffer = new byte[DWORDSIZE];

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_MicroSecPerFrame = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);
                    
                    stream.Read(buffer, 0, DWORDSIZE);
                    m_MaxBytesPerSec = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_PaddingGranularity = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_Flags = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_TotalFrames = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_InitialFrames = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_Streams = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_SuggestedBufferSize = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_Width = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_Height = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, DWORDSIZE);
                    m_Reserved = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);
                }
            }            
        }

        public override Element TableOfContents
        {
            get { return ReadChunks(Root.Offset, "idx1", "indx").FirstOrDefault(); }
        }

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
            foreach (var chunk in ReadChunks(Root.Offset, "strh").ToArray())
            {
                int sampleCount = TotalFrames, startTime = 0, timeScale = 0, duration = (int)Duration.TotalMilliseconds, width = Width, height = Height, rate = MicrosecondsPerFrame;

                string trackName = string.Empty;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Utility.Empty;

                byte channels = 0, bitDepth = 0;

                //sampleCount comes from lists with a wb or wc or wd id?

                //streamName comes from  "strn"

                //Expect 56 Bytes

                using (var stream = chunk.Data)
                {

                    stream.Position += IdentifierSize + LengthSize;

                    byte[] buffer = new byte[DWORDSIZE];

                    stream.Read(buffer, 0, IdentifierSize);

                    string fccType = Encoding.UTF8.GetString(buffer);

                    switch (fccType)
                    {
                        case "vids":
                            {
                                //avg_frame_rate = timebase
                                mediaType = Sdp.MediaType.video; 
                                break;
                            }
                        case "auds": mediaType = Sdp.MediaType.audio; break;
                        case "txts": mediaType = Sdp.MediaType.text; break;
                        case "data": mediaType = Sdp.MediaType.data; break;
                        default: break;
                    }

                    //fccHandler
                    codecIndication = new byte[IdentifierSize];
                    stream.Read(codecIndication, 0, IdentifierSize);

                    stream.Position += DWORDSIZE * 3;

                    //Scale
                    stream.Read(buffer, 0, IdentifierSize);
                    timeScale = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    //Rate
                    stream.Read(buffer, 0, IdentifierSize);
                    rate = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    if (!(timeScale > 0 && rate > 0))
                    {
                        rate = 25;
                        timeScale = 1;
                    }

                    //Start
                    stream.Read(buffer, 0, IdentifierSize);
                    startTime = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    //Length of stream (as defined in rate and timeScale above)
                    stream.Read(buffer, 0, IdentifierSize);
                    duration = Common.Binary.Read32(buffer, 0, !BitConverter.IsLittleEndian);

                    //SuggestedBufferSize

                    //Quality

                    //SampleSize

                    //RECT rcFrame (ushort left, top, right, bottom)

                    //Get strf for additional info.

                    if (mediaType == Sdp.MediaType.audio)
                    {

                        //Expand Codec Indication based on iD?

                        var strf = ReadChunk("strf", chunk.Offset);

                        if (strf != null)
                        {
                            //WaveFormat (EX) 
                            channels = (byte)Common.Binary.ReadU16(strf.Raw, 10, !BitConverter.IsLittleEndian);
                            bitDepth = (byte)Common.Binary.ReadU16(strf.Raw, 12, !BitConverter.IsLittleEndian);
                        }
                    }

                    var strn = ReadChunk("strn", chunk.Offset);

                    if (strn != null) trackName = Encoding.UTF8.GetString(strn.Raw, 8, (int)(strn.Size - 8));

                    //Hackup, should only get types based on media... right now just using all types
                    sampleCount = ReadChunks(Root.Offset, trackId.ToString("D2") + "dc", trackId.ToString("D2") + "wb", trackId.ToString("D2") + "tx", trackId.ToString("D2") + "ix").Count();

                    //Variable BitRate must also take into account the size of each chunk / nBlockAlign * duration per frame.

                    Track created = new Track(chunk, trackName, ++trackId, Created, Modified, sampleCount, height, width, TimeSpan.FromMilliseconds(startTime / timeScale), mediaType == Sdp.MediaType.audio ? TimeSpan.FromMilliseconds(sampleCount * Utility.MicrosecondsPerMillisecond / (double)rate * duration) : TimeSpan.FromMilliseconds(rate / duration), rate / timeScale, mediaType, codecIndication, channels, bitDepth);

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
