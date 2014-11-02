using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.BaseMedia
{
    /// <summary>
    /// Represents the logic necessary to read ISO Complaint Base Media Format Files.
    /// <see href="http://en.wikipedia.org/wiki/ISO_base_media_file_format">Wikipedia</see>
    /// Formats include QuickTime (.qt, .mov, .mp4, .m4v, .m4a), 
    /// Microsoft Smooth Streaming (.ismv, .isma, .ismc), 
    /// JPEG2000 (.jp2, .jpf, .jpx), Motion JPEG2000 (.mj2, .mjp2), 
    /// 3GPP/3GPP2 (.3gp, .3g2), Adobe Flash (.f4v, .f4p, .f4a, .f4b) and other conforming format extensions.
    /// Samsung Video (.svi)
    /// </summary>
    public class BaseMediaReader : MediaFileStream
    {

        static DateTime IsoBaseDateUtc = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //Todo Make Dictionary and have a ToTextualConvention that tries the Dictionary first. (KnownParents)        
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

        //int[] names?
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

        public byte[] ReadIdentifier(Stream stream)
        {
            //if (Remaining < IdentifierSize) return null;
            
            byte[] identifier = new byte[IdentifierSize];

            stream.Read(identifier, 0, IdentifierSize);

            return identifier;
        }

        public long ReadLength(Stream stream, out int bytesRead)
        {
            //if (Remaining < LengthSize) return bytesRead = 0;
            bytesRead = 0;
            long length = 0;
            byte[] lengthBytes = new byte[LengthSize];
            do
            {
                bytesRead += stream.Read(lengthBytes, 0, LengthSize);
                length = (lengthBytes[0] << 24) + (lengthBytes[1] << 16) + (lengthBytes[2] << 8) + lengthBytes[3];
            } while (length == 1 || (length & 0xffffffff) == 0);
            return length;
        }

        public Node ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            int lengthBytesRead = 0;

            long length = ReadLength(this, out lengthBytesRead);

            byte[] identifier = ReadIdentifier(this);

            //int nonDataBytes = IdentifierSize + lengthBytesRead;

            //Could give this, identifier, Position, length - nonDataBytes
            //Would pose the problem of not being able to deterine the lengthBytesRead from Node.Offset
            return  new Node(this, identifier, lengthBytesRead, offset, length, length <= Remaining);
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
                
                //Here using MinimumSize is technically not correct, it should be `Skip(next.Size - IdentifierSize + lengthBytesCount);`
                //When the Node only reflects the data count then this would be simply `Skip(next.Size);`
                Skip(next.DataSize - MinimumSize);
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

                int versionAndFlags = Common.Binary.Read32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian), version = versionAndFlags >> 24 & 0xff;

                offset += 4;

                ulong created = 0, modified = 0;

                switch (version)
                {
                    case 0:
                        {                            
                            created = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            break;
                        }

                    case 1:
                        {
                            
                            created = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            break;
                        }
                    default: throw new NotSupportedException();
                }

                //Rate Volume NextTrack

                m_PlayRate = Common.Binary.Read32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian) / 65536f;

                offset += 4;

                m_Volume = Common.Binary.ReadU16(mediaHeader.RawData, offset, BitConverter.IsLittleEndian) / 256f;

                offset += 2;

                m_Matrix = mediaHeader.RawData.Skip(offset).Take(36).ToArray();

                offset += 36;

                offset += 28;

                m_NextTrackId = Common.Binary.Read32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

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

            byte[] codecIndication = Utility.Empty;

            //Get Duration from mdhd, some files have more then one mdhd.
            if(!m_Duration.HasValue) ParseMovieHeader();

            //For each trak box in the file
            //TODO Make only a single pass, the data required should always be in the RawData of trakBox
            //E,g, use trackBox.Data stream and switch on identifer name contained in Data
            foreach (var trakBox in ReadBoxes(Root.Offset, "trak").ToArray())
            {
                //MAKE ONLY A SINGLE PASS HERE TO REDUCE IO
                //using (var stream = trakBox.Data)
                //{
                //    stream.Position += MinimumSize;

                //    int bytesRead = 0;

                //    long length = ReadLength(stream, out bytesRead);

                //    byte[] identifier = ReadIdentifier(stream);

                //    while (stream.Position < stream.Length)
                //    {
                //        switch (ToFourCharacterCode(identifier))
                //        {
                //            case "tkhd":
                //                {
                //                    break;
                //                }
                //            default:
                //                {
                //                    stream.Position += length; 
                //                    continue;
                //                }
                //        }
                //    }
                //}

                //Should come right after trak header
                var trakHead = ReadBox("tkhd", trakBox.Offset);

                int offset = MinimumSize;

                int version = trakHead.RawData[offset++], flags = Common.Binary.Read24(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                offset += 3;

                enabled = ((flags & 1) == flags);

                inMovie = ((flags & 2) == flags);

                inPreview = ((flags & 3) == flags);

                if (version == 0)
                {
                    created = Common.Binary.ReadU32(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    modified = Common.Binary.ReadU32(trakHead.RawData, offset, BitConverter.IsLittleEndian);
                    
                    offset += 4;
                }
                else
                {
                    created = Common.Binary.ReadU64(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 8;

                    modified = Common.Binary.ReadU64(trakHead.RawData, offset, BitConverter.IsLittleEndian);
                    
                    offset += 8;
                }

                trackId = Common.Binary.Read32(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                //Skip
                offset += 8;

                //Get Duration
                if (version == 0)
                {
                    duration = Common.Binary.ReadU32(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;
                }
                else
                {
                    duration = Common.Binary.ReadU64(trakHead.RawData, offset, BitConverter.IsLittleEndian);
                    
                    offset += 8;
                }

                if (duration == 4294967295L) duration = ulong.MaxValue;

                //Reserved
                offset += 8;

                int layer = Common.Binary.ReadU16(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                offset += 2;

                int altGroup = Common.Binary.ReadU16(trakHead.RawData, offset, BitConverter.IsLittleEndian);

                offset += 2;

                float volume = Common.Binary.ReadU16(trakHead.RawData, offset, BitConverter.IsLittleEndian) / 256;

                //Skip int and Matrix
                offset += 40;
                
                //Width
                width = Common.Binary.Read32(trakHead.RawData, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;
                
                offset += 4;
                //Height

                height = Common.Binary.Read32(trakHead.RawData, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;

                offset += 4;

                ulong trackTimeScale = m_TimeScale.Value, trackDuration = duration;

                DateTime trackCreated = m_Created.Value, trackModified = m_Modified.Value;

                //Read the mediaHeader (use overload with count to ensure we do not over read)
                var mediaHeader = ReadBox("mdhd", trakBox.Offset);
                if (mediaHeader != null)
                {
                    offset = MinimumSize;

                    version = mediaHeader.RawData[offset++];

                    flags = Common.Binary.Read24(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 3;

                    ulong mediaCreated, mediaModified, timescale, mediaduration;

                    if (version == 0)
                    {

                        mediaCreated = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaModified = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        timescale = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaduration = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 4;
                    }
                    else
                    {
                        mediaCreated = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaModified = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        timescale = Common.Binary.ReadU32(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaduration = Common.Binary.ReadU64(mediaHeader.RawData, offset, BitConverter.IsLittleEndian);
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

                    int entryCount = Common.Binary.Read32(sampleToTimeBox.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Sample Count Sample Duration
                        entries.Add(new Tuple<long, long>(Common.Binary.Read32(sampleToTimeBox.RawData, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(sampleToTimeBox.RawData, offset + 4, BitConverter.IsLittleEndian)));
                        offset += MinimumSize;
                    }
                }

                var sampleToSizeBox = ReadBox("stsz", trakBox.Offset);

                if (sampleToSizeBox != null)
                {

                    offset = MinimumSize + IdentifierSize;

                    int defaultSize = Common.Binary.Read32(sampleToSizeBox.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    int count = Common.Binary.Read32(sampleToSizeBox.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    if (defaultSize == 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            sampleSizes.Add(Common.Binary.Read32(sampleToSizeBox.RawData, offset, BitConverter.IsLittleEndian));

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

                        int chunkCount = Common.Binary.Read32(chunkOffsetsBox.RawData, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            offsets.Add(Common.Binary.Read64(chunkOffsetsBox.RawData, offset, BitConverter.IsLittleEndian));

                            offset += 8;
                        }
                    }

                }
                else
                {
                    offset = MinimumSize + IdentifierSize;

                    int chunkCount = Common.Binary.Read32(chunkOffsetsBox.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < chunkCount; ++i)
                    {
                        offsets.Add((long)Common.Binary.Read32(chunkOffsetsBox.RawData, offset, BitConverter.IsLittleEndian));

                        offset += 4;
                    }
                }

                TimeSpan calculatedDuration = TimeSpan.FromSeconds(trackDuration / (double)trackTimeScale);

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                var hdlr = ReadBox("hdlr", trakBox.Offset);

                string comp = ToFourCharacterCode(hdlr.RawData, MinimumSize + IdentifierSize), sub = ToFourCharacterCode(hdlr.RawData, MinimumSize * 2);

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
                    int size = (int)(nameBox.DataSize - 12);

                    byte[] nameBytes = new byte[size];
                    using (var stream = nameBox.DataStream)
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


                int sampleDescriptionCount = Common.Binary.Read32(sampleDescriptionBox.RawData, 12, BitConverter.IsLittleEndian);

                byte channels = 0, bitDepth = 0;

                offset = 16;

                if (sampleDescriptionCount > 0)
                {
                    for (int i = 0; i < sampleDescriptionCount; ++i)
                    {
                        int len = Common.Binary.Read32(sampleDescriptionBox.RawData, offset, BitConverter.IsLittleEndian) - 4;
                        offset += 4;

                        var sampleEntry = sampleDescriptionBox.RawData.Skip(offset).Take(len);
                        offset += len;

                        switch (mediaType)
                        {
                            case Sdp.MediaType.audio:
                                {
                                    //Maybe == mp4a
                                    codecIndication = sampleEntry.Take(4).ToArray();

                                    //32, 16, 16 (dref index)
                                    version = Common.Binary.Read16(sampleEntry, 8, BitConverter.IsLittleEndian);

                                    //Revision 16, Vendor 32

                                    //ChannelCount 16
                                    channels = (byte)Common.Binary.ReadU16(sampleEntry, 20, BitConverter.IsLittleEndian);

                                    //SampleSize 16 (A 16-bit integer that specifies the number of bits in each uncompressed sound sample. Allowable values are 8 or 16. Formats using more than 16 bits per sample set this field to 16 and use sound description version 1.)
                                    bitDepth = (byte)Common.Binary.ReadU16(sampleEntry, 22, BitConverter.IsLittleEndian);

                                    //CompressionId 16
                                    var compressionId = sampleEntry.Skip(24).Take(2);

                                    //Decode to a WaveFormatID (16 bit)
                                    int waveFormatId = Common.Binary.Read16(compressionId, 0, BitConverter.IsLittleEndian);

                                    //The compression ID is set to -2 and redefined sample tables are used (see “Redefined Sample Tables”).
                                    if (-2 == waveFormatId)
                                    {
                                        //var waveAtom = ReadBox("wave", sampleDescriptionBox.Offset);
                                        //if (waveAtom != null)
                                        //{
                                        //    flags = Common.Binary.Read24(waveAtom.Raw, 9, BitConverter.IsLittleEndian);
                                        //    //Extrack from flags?
                                        //}
                                    }//If the formatId is known then use it
                                    else if (waveFormatId > 0) codecIndication = compressionId.ToArray();

                                    //@ 26

                                    //PktSize 16

                                    //sr 32

                                    rate = (double)Common.Binary.ReadU32(sampleEntry, 28, BitConverter.IsLittleEndian) / 65536F;

                                    //@ 32

                                    if (version > 1)
                                    {

                                        //36 total

                                        rate = BitConverter.Int64BitsToDouble(Common.Binary.Read64(sampleEntry, 32, BitConverter.IsLittleEndian));
                                        channels = (byte)Common.Binary.ReadU32(sampleEntry, 40, BitConverter.IsLittleEndian);

                                        //24 More Bytes
                                    }

                                    //else 16 more if version == 1
                                    //else 2 more if version == 0

                                    //@ esds for mp4a

                                    //http://www.mp4ra.org/object.html
                                    // @ +4 +4 +11 == ObjectTypeIndication

                                    break;
                                }
                            case Sdp.MediaType.video:
                                {
                                    codecIndication = sampleEntry.Take(4).ToArray();

                                    //SampleEntry overhead = 8
                                    //Version, Revision, Vendor, TemporalQUal, SpacialQual, Width, Height, hRes,vRes, reversed, FrameCount, compressorName, depth, clrTbl, (extensions)

                                    //Width @ 28
                                    width = Common.Binary.ReadU16(sampleEntry, 28, BitConverter.IsLittleEndian);
                                    //Height @ 30
                                    height = Common.Binary.ReadU16(sampleEntry, 30, BitConverter.IsLittleEndian);

                                    //hres, vres, reserved = 12

                                    //FrameCount @ 44 (A 16-bit integer that indicates how many frames of compressed data are stored in each sample. Usually set to 1.)

                                    //@46

                                    //30 bytes compressor name (1 byte length) + 1

                                    //@78

                                    bitDepth = (byte)Common.Binary.ReadU16(sampleEntry, 78, BitConverter.IsLittleEndian);

                                    break;
                                }
                        }

                        continue;

                    }
                    
                }

                

                //Also contains channels and bitDept info

                //byte bitDepth;

                //Check for esds if codecIndication is MP4 or MP4A

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

                var elst = ReadBox("elst", trakBox.Offset);

                List<Tuple<int, int, float>> edits = new List<Tuple<int, int, float>>();

                TimeSpan startTime = TimeSpan.Zero;

                if (elst != null)
                {
                    offset = MinimumSize + IdentifierSize;

                    int entryCount = Common.Binary.Read32(elst.RawData, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Edit Duration, MediaTime, Rate
                        edits.Add(new Tuple<int, int, float>(Common.Binary.Read32(elst.RawData, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.RawData, offset + 4, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.RawData, offset + 8, BitConverter.IsLittleEndian) / ushort.MaxValue));

                        offset += 12;
                    }

                    if (edits.Count > 0 && edits[0].Item2 > 0)
                    {
                        startTime = TimeSpan.FromMilliseconds(edits[0].Item2);
                    }

                    elst = null;
                }

                Track createdTrack = new Track(trakBox, name, trackId, trackCreated, trackModified, (long)sampleCount, width, height, startTime, calculatedDuration, rate, mediaType, codecIndication, channels, bitDepth);

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
