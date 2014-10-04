using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.BaseMedia
{
    /// <summary>
    /// Represents the logic necessary to read ISO Complaint Base Media Format Files.
    /// <see href="http://en.wikipedia.org/wiki/ISO_base_media_file_format">Wikipedia</see>
    /// Formats include QuickTime (.mov, .mp4, .m4v, .m4a), 
    /// Microsoft Smooth Streaming (.ismv, .isma, .ismc), 
    /// JPEG2000 (.jp2, .jpf, .jpx), Motion JPEG2000 (.mj2, .mjp2), 
    /// 3GPP/3GPP2 (.3gp, .3g2), Adobe Flash (.f4v, .f4p, .f4a, .f4b) and other conforming format extensions.
    /// </summary>
    public class BaseMediaReader : MediaFileStream
    {

        static DateTime IsoBaseDateUtc = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static List<string> ParentBoxes = new List<string>()
        {
            "moof",
            "moov",
            "trak",
            "mdia",
            "minf",
            "dinf",
            "stbl",
            "edts",            
            "stsd",
            "udta"
        };

        const int MinimumSize = IdentifierSize + LengthSize, IdentifierSize = 4, LengthSize = 4;

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return ToFourCharacterCode(Encoding.UTF8, identifier, offset, count); }

        public static string ToFourCharacterCode(Encoding encoding, byte[] identifier, int offset, int count)
        {
            if (encoding == null) throw new ArgumentNullException("encoding");

            if (identifier == null) throw new ArgumentNullException("identifier");

            if (offset + count > identifier.Length) throw new ArgumentOutOfRangeException("offset and count must relfect a position within identifier.");

            return encoding.GetString(identifier, offset, count);
        }

        public BaseMediaReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public BaseMediaReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<Element> ReadBoxes(long offset = 0, params string[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var box in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(ToFourCharacterCode(box.Identifier)))
                {
                    yield return box;
                }
            }

            Position = position;
        }

        public Element ReadBox(string name, long offset = 0) { return ReadBoxes(offset, name).FirstOrDefault(); }

        public byte[] ReadIdentifier()
        {
            if (Remaining < IdentifierSize) return null;
            
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            return identifier;
        }

        public long ReadLength(out int bytesRead)
        {
            if (Remaining < LengthSize) return bytesRead = 0;
            bytesRead = 0;
            long length = 0;
            byte[] lengthBytes = new byte[LengthSize];
            do
            {
                Read(lengthBytes, 0, LengthSize);
                length = (lengthBytes[0] << 24) + (lengthBytes[1] << 16) + (lengthBytes[2] << 8) + lengthBytes[3];
                bytesRead += 4;
            } while (length == 1 || (length & 0xffffffff) == 0);
            return length;
        }

        public Element ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            int lengthBytesRead = 0;

            long length = ReadLength(out lengthBytesRead);

            byte[] identifier = ReadIdentifier();

            int nonDataBytes = IdentifierSize + lengthBytesRead;

            return  new Element(this, identifier, offset, length, length <= Remaining);
        }

        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;

                //Parent boxes contain other boxes so do not skip them, parse right into their data
                if (ParentBoxes.Contains(ToFourCharacterCode(next.Identifier))) continue;
                
                Skip(next.Size - MinimumSize);
            }
        }

        /// <summary>
        /// Reads an Element with the given name which occurs at the current Position or later
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Element this[string name]
        {
            get
            {
                return ReadBoxes(Position, name).FirstOrDefault();
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

        public bool HasProtection
        {
            //pssh/cenc
            //TrackLevel Encryption = tenc
            get { return ReadBoxes(Root.Offset, "ipro", "sinf").Count() >= 1; }
        }

        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (m_Created.HasValue) return m_Created.Value;
                ParseMovieHeader();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (m_Modified.HasValue) return m_Modified.Value;
                ParseMovieHeader();
                return m_Modified.Value;
            }
        }

        ulong? m_TimeScale;
        
        TimeSpan? m_Duration;
        
        public TimeSpan Duration
        {
            get
            {
                if (m_Duration.HasValue) return m_Duration.Value;
                ParseMovieHeader();
                return m_Duration.Value;
            }
        }

        float? m_PlayRate, m_Volume;

        public float PlayRate
        {
            get { 
                if (m_PlayRate.HasValue) return m_PlayRate.Value;
                ParseMovieHeader();
                return m_PlayRate.Value;
            }
        }

        public float Volume
        {
            get
            {
                if (m_Volume.HasValue) return m_Volume.Value;
                ParseMovieHeader();
                return m_Volume.Value;
            }
        }

        byte[] m_Matrix;

        public byte[] Matrix
        {
            get
            {
                if (m_Matrix != null) return m_Matrix;
                ParseMovieHeader();
                return m_Matrix;
            }
        }

        int? m_NextTrackId;

        public int NextTrackId
        {
            get
            {
                if (m_NextTrackId.HasValue) return m_NextTrackId.Value;
                ParseMovieHeader();
                return m_NextTrackId.Value;
            }
        }

        protected void ParseMovieHeader()
        {
            ulong duration;

            //Obtain the timeScale and duration from the LAST mdhd box
            using (var mediaHeader = ReadBox("mvhd", Root.Offset))
            {
                using (var stream = mediaHeader.Data)
                {
                    stream.Position += 8;

                    byte[] buffer = new byte[8];

                    stream.Read(buffer, 0, 4);

                    int versionAndFlags = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian), version = versionAndFlags >> 24 & 0xff;

                    ulong created = 0, modified = 0;

                    switch (version)
                    {
                        case 0:
                            {
                                stream.Read(buffer, 0, 4);
                                created = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 4);
                                modified = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 4);
                                m_TimeScale = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 4);
                                duration = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                                break;
                            }

                        case 1:
                            {


                                stream.Read(buffer, 0, 8);
                                created = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 8);
                                modified = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 4);
                                m_TimeScale = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                                stream.Read(buffer, 0, 8);
                                duration = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                                break;
                            }
                        default: throw new NotSupportedException();
                    }

                    //Rate Volume NextTrack

                    stream.Read(buffer, 0, 4);

                    m_PlayRate = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian) / 65536f;

                    stream.Read(buffer, 0, 2);

                    m_Volume = Common.Binary.ReadU16(buffer, 0, BitConverter.IsLittleEndian) / 256f;

                    m_Matrix = new byte[36];

                    stream.Read(m_Matrix, 0, 36);

                    stream.Position += 24;

                    stream.Read(buffer, 0, 4);

                    m_NextTrackId = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                    m_Created = IsoBaseDateUtc.AddMilliseconds(created * Utility.MicrosecondsPerMillisecond);

                    m_Modified = IsoBaseDateUtc.AddMilliseconds(modified * Utility.MicrosecondsPerMillisecond);

                    m_Duration = TimeSpan.FromSeconds((double)duration / (double)m_TimeScale.Value);
                }


            }
        }

        public override Element TableOfContents
        {
            get { return ReadBox("stco") ?? ReadBox("co64"); }
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

            ulong created = 0, modified = 0, duration = 0;

            int width, height;

            bool enabled, inMovie, inPreview;

            //Get Duration from mdhd, some files have more then one mdhd.
            if(!m_Duration.HasValue) ParseMovieHeader();

            //For each trak box in the file
            foreach (var trakBox in ReadBoxes(Root.Offset, "trak").ToArray())
            {
                var trakHead = ReadBox("tkhd", trakBox.Offset);

                using (var stream = trakHead.Data)
                {
                    stream.Position += 8;

                    byte[] buffer = new byte[8];

                    stream.Read(buffer, 0, 4);

                    int version = buffer[0], flags = Common.Binary.Read24(buffer, 1, BitConverter.IsLittleEndian);

                    enabled = ((flags & 1) == flags);

                    inMovie = ((flags & 2) == flags);

                    inPreview = ((flags & 3) == flags);

                    if (version == 0)
                    {

                        stream.Read(buffer, 0, 4);
                        created = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);
                        modified = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    }
                    else
                    {
                        stream.Read(buffer, 0, 8);
                        created = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 8);
                        modified = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);
                    }

                    stream.Read(buffer, 0, 4);

                    trackId = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                    //Skip
                    stream.Read(buffer, 0, 4);

                    //Get Duration
                    if (version == 0)
                    {
                        stream.Read(buffer, 0, 4);
                        duration = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    }
                    else
                    {
                        stream.Read(buffer, 0, 8);
                        duration = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);
                    }

                    if (duration == 4294967295L) duration = ulong.MaxValue;

                    //Reserved
                    stream.Read(buffer, 0, 4);
                    stream.Read(buffer, 0, 4);

                    stream.Read(buffer, 0, 2);
                    int layer = Common.Binary.ReadU16(buffer, 0, BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, 2);
                    int altGroup = Common.Binary.ReadU16(buffer, 0, BitConverter.IsLittleEndian);

                    stream.Read(buffer, 0, 2);
                    float volume = Common.Binary.ReadU16(buffer, 0, BitConverter.IsLittleEndian) / 256;

                    //Skip
                    stream.Read(buffer, 0, 2);

                    //Matrix of 9 int?
                    stream.Position += 9 * 4;

                    //Width
                    stream.Read(buffer, 0, 4);

                    width = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian) / ushort.MaxValue;
                    //Height

                    stream.Read(buffer, 0, 4);
                    height = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian) / ushort.MaxValue;
                }

                ulong trackTimeScale = m_TimeScale.Value, trackDuration = duration;

                DateTime trackCreated = m_Created.Value, trackModified = m_Modified.Value;

                //Read the mediaHeader
                var mediaHeader = ReadBox("mdhd", trakBox.Offset);

                using (var stream = mediaHeader.Data)
                {
                    stream.Position += 8;

                    byte[] buffer = new byte[8];

                    stream.Read(buffer, 0, 4);

                    int version = buffer[0], flags = Common.Binary.Read24(buffer, 1, BitConverter.IsLittleEndian);

                    ulong mediaCreated, mediaModified, timescale, mediaduration;

                    if (version == 0)
                    {

                        stream.Read(buffer, 0, 4);
                        mediaCreated = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);
                        mediaModified = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);
                        timescale = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);
                        mediaduration = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    }
                    else
                    {
                        stream.Read(buffer, 0, 8);
                        mediaCreated = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 8);
                        mediaModified = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);
                        timescale = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 8);
                        mediaduration = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);
                    }

                    trackTimeScale = timescale;

                    trackDuration = mediaduration;

                    trackCreated = IsoBaseDateUtc.AddMilliseconds(mediaCreated * Utility.MicrosecondsPerMillisecond);

                    trackModified = IsoBaseDateUtc.AddMilliseconds(mediaModified * Utility.MicrosecondsPerMillisecond);

                }

                var sampleToTimeBox = ReadBox("stts", trakBox.Offset);

                List<Tuple<long, long>> entries = new List<Tuple<long, long>>();

                List<long> offsets = new List<long>();

                List<int> sampleSizes = new List<int>();

                if (sampleToTimeBox != null)
                {
                    using (var stream = sampleToTimeBox.Data)
                    {

                        //12 comes from Version, Flags int in FullBox + 8 for box name and length
                        stream.Position += 12;

                        byte[] buffer = new byte[8];

                        stream.Read(buffer, 0, 4);

                        int entryCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < entryCount; ++i)
                        {
                            stream.Read(buffer, 0, 8);

                            //Sample Count Sample Duration
                            entries.Add(new Tuple<long, long>(Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian), 
                                Common.Binary.Read32(buffer, 4, BitConverter.IsLittleEndian)));
                        }
                    }
                }


                var sampleToSizeBox = ReadBox("stsz", trakBox.Offset);

                if (sampleToSizeBox != null)
                {
                    using (var stream = sampleToSizeBox.Data)
                    {

                        stream.Position += 12;

                        byte[] buffer = new byte[4];

                        stream.Read(buffer, 0, 4);

                        int defaultSize = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);

                        int count = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        if (defaultSize == 0)
                        {
                            for (int i = 0; i < count; ++i)
                            {
                                stream.Read(buffer, 0, 4);

                                sampleSizes.Add(Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian));
                            }
                        }
                    }
                }

                var chunkOffsetsBox = ReadBox("stco", trakBox.Offset);
                if (chunkOffsetsBox == null)
                {
                    chunkOffsetsBox = ReadBox("co64", trakBox.Offset);

                    if(chunkOffsetsBox != null) using (var stream = chunkOffsetsBox.Data)
                    {
                        stream.Position += 12;

                        byte[] buffer = new byte[8];

                        stream.Read(buffer, 0, 4);

                        int chunkCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            stream.Read(buffer, 0, 8);

                            offsets.Add(Common.Binary.Read64(buffer, 0, BitConverter.IsLittleEndian));
                        }
                    }

                }
                else
                {
                    using (var stream = chunkOffsetsBox.Data)
                    {
                        stream.Position += 12;

                        byte[] buffer = new byte[4];

                        stream.Read(buffer, 0, 4);

                        int chunkCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            stream.Read(buffer, 0, 4);

                            offsets.Add((long)Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian));
                        }
                    }
                }

                TimeSpan calculatedDuration = TimeSpan.FromSeconds(trackDuration / (double)trackTimeScale);

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                var hdlr = ReadBox("hdlr", trakBox.Offset);

                string comp, sub;

                using (var stream = hdlr.Data)
                {
                    stream.Position += 12;

                    byte[] buffer = new byte[8];

                    stream.Read(buffer, 0, 8);

                    comp = ToFourCharacterCode(buffer, 0);

                    sub = ToFourCharacterCode(buffer, 4);

                }

                switch (sub)
                {
                    case "vide": mediaType = Sdp.MediaType.video; break;
                    case "soun": mediaType = Sdp.MediaType.audio; break;
                    case "text": mediaType = Sdp.MediaType.text; break;
                    case "tmcd": mediaType = Sdp.MediaType.timing; break;
                    default: break;
                }

                var nameBox = ReadBox("name", trakBox.Offset);

                string name = string.Empty;

                if (nameBox != null)
                {
                    int size = (int)(nameBox.Size - 12);

                    byte[] nameBytes = new byte[size];
                    using (var stream = nameBox.Data)
                    {
                        stream.Position += 12;
                        stream.Read(nameBytes, 0, size);
                        name = Encoding.UTF8.GetString(nameBytes);
                    }
                }

                ulong sampleCount = (ulong)sampleSizes.Count();

                if (sampleCount == 0) sampleCount = (ulong)entries[0].Item1;

                double rate = mediaType == Sdp.MediaType.audio ? trackTimeScale : (double)((double)sampleCount / ((double)trackDuration / trackTimeScale));

                var sampleDescriptionBox = ReadBox("stsd", trakBox.Offset);

                byte[] codecIndication = new byte[4];

                //byte bitDepth;

                using(var stream = sampleDescriptionBox.Data)
                {
                    stream.Position += 20;
                    stream.Read(codecIndication, 0, 4);

                    //if (stream.Length > 24 + 38)
                    //{
                    //    //There is a Media Sample Description which contains the bit dept and number of channels etc...

                    //    //https://developer.apple.com/library/Mac/documentation/QuickTime/QTFF/QTFFChap3/qtff3.html#//apple_ref/doc/uid/TP40000939-CH205-74522

                    //This should be read to get the channels and bitdept for audio and bitdept for video as well as number of components / format

                    //    stream.Position += 38;

                    //    byte[] bitd = new byte[2];

                    //    stream.Read(bitd, 0, 2);

                    //    bitDepth = (byte)Common.Binary.ReadU16(bitd, 0, BitConverter.IsLittleEndian);
                    //}
                }

                //Check for esds if codecIndication is MP4 or MP4A

                var elst = ReadBox("elst", trakBox.Offset);

                List<Tuple<int, int, float>> edits = new List<Tuple<int, int, float>>();

                TimeSpan startTime = TimeSpan.Zero;

                if (elst != null)
                {
                    using (var stream = elst.Data)
                    {

                        //12 comes from Version, Flags int in FullBox + 8 for box name and length
                        stream.Position += 12;

                        byte[] buffer = new byte[12];

                        stream.Read(buffer, 0, 4);

                        int entryCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < entryCount; ++i)
                        {
                            stream.Read(buffer, 0, 12);

                            //Edit Duration, MediaTime, Rate
                            edits.Add(new Tuple<int, int, float>(Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian),
                                Common.Binary.Read32(buffer, 4, BitConverter.IsLittleEndian),
                                Common.Binary.Read32(buffer, 8, BitConverter.IsLittleEndian) / ushort.MaxValue));
                        }
                    }

                    if (edits.Count > 0 && edits[0].Item2 > 0)
                    {
                        startTime = TimeSpan.FromMilliseconds(edits[0].Item2);
                    }

                    elst = null;
                }

                Track createdTrack = new Track(trakBox, name, trackId, trackCreated, trackModified, (long)sampleCount, width, height, startTime, calculatedDuration, rate, mediaType, codecIndication);

                tracks.Add(createdTrack);

                yield return createdTrack;
            }

            m_Tracks = tracks;

            Position = position;
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {

            //Could be moved to track.

            //Track has sample count.

            //Could make a SampleEnumerator which takes the list of sampleSizes and the list of offsets
            throw new NotImplementedException();
        }
    }
}
