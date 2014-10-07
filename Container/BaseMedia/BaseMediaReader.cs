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
        public IEnumerable<Node> ReadBoxes(long offset = 0, params string[] names)
        {
            long positionStart = Position;

            Position = offset;

            foreach (var box in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(ToFourCharacterCode(box.Identifier)))
                {
                    yield return box;
                    continue;
                }
            }

            Position = positionStart;

            yield break;
        }

        public Node ReadBox(string name, long offset = 0)
        {
            long positionStart = Position;

            Node result = ReadBoxes(offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

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

        public Node ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            int lengthBytesRead = 0;

            long length = ReadLength(out lengthBytesRead);

            byte[] identifier = ReadIdentifier();

            //int nonDataBytes = IdentifierSize + lengthBytesRead;

            return  new Node(this, identifier, offset, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node next = ReadNext();
                if (next == null) yield break;
                
                yield return next;

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
        public Node this[string name]
        {
            get
            {
                return ReadBoxes(Position, name).FirstOrDefault();
            }
        }

        public override Node Root { get { return ReadBox("ftyp", 0); } }

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
                if (!m_Created.HasValue) ParseMovieHeader();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (!m_Modified.HasValue) ParseMovieHeader();
                return m_Modified.Value;
            }
        }

        ulong? m_TimeScale;
        
        TimeSpan? m_Duration;
        
        public TimeSpan Duration
        {
            get
            {
                if (!m_Duration.HasValue) ParseMovieHeader();
                return m_Duration.Value;
            }
        }

        float? m_PlayRate, m_Volume;

        public float PlayRate
        {
            get { 
                if (!m_PlayRate.HasValue) ParseMovieHeader();
                return m_PlayRate.Value;
            }
        }

        public float Volume
        {
            get
            {
                if (!m_Volume.HasValue) ParseMovieHeader();
                return m_Volume.Value;
            }
        }

        byte[] m_Matrix;

        public byte[] Matrix
        {
            get
            {
                if (m_Matrix == null) ParseMovieHeader();
                return m_Matrix;
            }
        }

        int? m_NextTrackId;

        public int NextTrackId
        {
            get
            {
                if (!m_NextTrackId.HasValue) ParseMovieHeader();
                return m_NextTrackId.Value;
            }
        }

        protected void ParseMovieHeader()
        {
            ulong duration;

            //Obtain the timeScale and duration from the LAST mdhd box
            using (var mediaHeader = ReadBox("mvhd", Root.Offset))
            {

                int offset = MinimumSize;

                int versionAndFlags = Common.Binary.Read32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian), version = versionAndFlags >> 24 & 0xff;

                offset += 4;

                ulong created = 0, modified = 0;

                switch (version)
                {
                    case 0:
                        {                            
                            created = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            break;
                        }

                    case 1:
                        {
                            
                            created = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            break;
                        }
                    default: throw new NotSupportedException();
                }

                //Rate Volume NextTrack

                m_PlayRate = Common.Binary.Read32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian) / 65536f;

                offset += 4;

                m_Volume = Common.Binary.ReadU16(mediaHeader.Raw, offset, BitConverter.IsLittleEndian) / 256f;

                offset += 2;

                m_Matrix = mediaHeader.Raw.Skip(offset).Take(36).ToArray();

                offset += 36;

                offset += 28;

                m_NextTrackId = Common.Binary.Read32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                offset += 4;

                m_Created = IsoBaseDateUtc.AddMilliseconds(created * Utility.MicrosecondsPerMillisecond);

                m_Modified = IsoBaseDateUtc.AddMilliseconds(modified * Utility.MicrosecondsPerMillisecond);

                m_Duration = TimeSpan.FromSeconds((double)duration / (double)m_TimeScale.Value);
            }
        }

        //Should be a better box...
        public override Node TableOfContents
        {
            get { return ReadBoxes(Root.Offset, "stco", "co64").FirstOrDefault(); }
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

                int offset = MinimumSize;

                int version = trakHead.Raw[offset++], flags = Common.Binary.Read24(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                offset += 3;

                enabled = ((flags & 1) == flags);

                inMovie = ((flags & 2) == flags);

                inPreview = ((flags & 3) == flags);

                if (version == 0)
                {
                    created = Common.Binary.ReadU32(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    modified = Common.Binary.ReadU32(trakHead.Raw, offset, BitConverter.IsLittleEndian);
                    
                    offset += 4;
                }
                else
                {
                    created = Common.Binary.ReadU64(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 8;

                    modified = Common.Binary.ReadU64(trakHead.Raw, offset, BitConverter.IsLittleEndian);
                    
                    offset += 8;
                }

                trackId = Common.Binary.Read32(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                //Skip
                offset += 8;

                //Get Duration
                if (version == 0)
                {
                    duration = Common.Binary.ReadU32(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;
                }
                else
                {
                    duration = Common.Binary.ReadU64(trakHead.Raw, offset, BitConverter.IsLittleEndian);
                    
                    offset += 8;
                }

                if (duration == 4294967295L) duration = ulong.MaxValue;

                //Reserved
                offset += 8;

                int layer = Common.Binary.ReadU16(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                offset += 2;

                int altGroup = Common.Binary.ReadU16(trakHead.Raw, offset, BitConverter.IsLittleEndian);

                offset += 2;

                float volume = Common.Binary.ReadU16(trakHead.Raw, offset, BitConverter.IsLittleEndian) / 256;

                //Skip int and Matrix
                offset += 40;
                
                //Width
                width = Common.Binary.Read32(trakHead.Raw, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;
                
                offset += 4;
                //Height

                height = Common.Binary.Read32(trakHead.Raw, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;

                offset += 4;

                ulong trackTimeScale = m_TimeScale.Value, trackDuration = duration;

                DateTime trackCreated = m_Created.Value, trackModified = m_Modified.Value;

                //Read the mediaHeader
                var mediaHeader = ReadBox("mdhd", trakBox.Offset);
                if (mediaHeader != null)
                {
                    offset = MinimumSize;

                    version = mediaHeader.Raw[offset++];

                    flags = Common.Binary.Read24(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 3;

                    ulong mediaCreated, mediaModified, timescale, mediaduration;

                    if (version == 0)
                    {

                        mediaCreated = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaModified = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        timescale = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaduration = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;
                    }
                    else
                    {
                        mediaCreated = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaModified = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        timescale = Common.Binary.ReadU32(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaduration = Common.Binary.ReadU64(mediaHeader.Raw, offset, BitConverter.IsLittleEndian);
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
                    offset = MinimumSize + IdentifierSize;

                    int entryCount = Common.Binary.Read32(sampleToTimeBox.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Sample Count Sample Duration
                        entries.Add(new Tuple<long, long>(Common.Binary.Read32(sampleToTimeBox.Raw, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(sampleToTimeBox.Raw, offset + 4, BitConverter.IsLittleEndian)));
                        offset += MinimumSize;
                    }
                }

                var sampleToSizeBox = ReadBox("stsz", trakBox.Offset);

                if (sampleToSizeBox != null)
                {

                    offset = MinimumSize + IdentifierSize;

                    int defaultSize = Common.Binary.Read32(sampleToSizeBox.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    int count = Common.Binary.Read32(sampleToSizeBox.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    if (defaultSize == 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            sampleSizes.Add(Common.Binary.Read32(sampleToSizeBox.Raw, offset, BitConverter.IsLittleEndian));

                            offset += 4;
                        }
                    }
                }

                var chunkOffsetsBox = ReadBox("stco", trakBox.Offset);

                if (chunkOffsetsBox == null)
                {
                    chunkOffsetsBox = ReadBox("co64", trakBox.Offset);

                    if (chunkOffsetsBox != null)
                    {
                        offset = MinimumSize + IdentifierSize;

                        int chunkCount = Common.Binary.Read32(chunkOffsetsBox.Raw, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            offsets.Add(Common.Binary.Read64(chunkOffsetsBox.Raw, offset, BitConverter.IsLittleEndian));

                            offset += 8;
                        }
                    }

                }
                else
                {
                    offset = MinimumSize + IdentifierSize;

                    int chunkCount = Common.Binary.Read32(chunkOffsetsBox.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < chunkCount; ++i)
                    {
                        offsets.Add((long)Common.Binary.Read32(chunkOffsetsBox.Raw, offset, BitConverter.IsLittleEndian));

                        offset += 4;
                    }
                }

                TimeSpan calculatedDuration = TimeSpan.FromSeconds(trackDuration / (double)trackTimeScale);

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                var hdlr = ReadBox("hdlr", trakBox.Offset);

                string comp = ToFourCharacterCode(hdlr.Raw, MinimumSize + IdentifierSize), sub = ToFourCharacterCode(hdlr.Raw, MinimumSize * 2);

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

                byte[] codecIndication = sampleDescriptionBox.Raw.Skip(20).Take(4).ToArray();

                //Also contains channels and bitDept info

                //byte bitDepth;

                //using(var stream = sampleDescriptionBox.Data)
                //{
                //    stream.Position += 20;
                //    stream.Read(codecIndication, 0, 4);

                //    //if (stream.Length > 24 + 38)
                //    //{
                //    //    //There is a Media Sample Description which contains the bit dept and number of channels etc...

                //    //    //https://developer.apple.com/library/Mac/documentation/QuickTime/QTFF/QTFFChap3/qtff3.html#//apple_ref/doc/uid/TP40000939-CH205-74522

                //    //This should be read to get the channels and bitdept for audio and bitdept for video as well as number of components / format

                //    //    stream.Position += 38;

                //    //    byte[] bitd = new byte[2];

                //    //    stream.Read(bitd, 0, 2);

                //    //    bitDepth = (byte)Common.Binary.ReadU16(bitd, 0, BitConverter.IsLittleEndian);
                //    //}
                //}

                //Check for esds if codecIndication is MP4 or MP4A

                var elst = ReadBox("elst", trakBox.Offset);

                List<Tuple<int, int, float>> edits = new List<Tuple<int, int, float>>();

                TimeSpan startTime = TimeSpan.Zero;

                if (elst != null)
                {
                    offset = MinimumSize + IdentifierSize;

                    int entryCount = Common.Binary.Read32(elst.Raw, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Edit Duration, MediaTime, Rate
                        edits.Add(new Tuple<int, int, float>(Common.Binary.Read32(elst.Raw, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.Raw, offset + 4, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.Raw, offset + 8, BitConverter.IsLittleEndian) / ushort.MaxValue));

                        offset += 12;
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
