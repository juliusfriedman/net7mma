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
using System.IO;
using System.Linq;
using System.Text;
using Media.Container;

namespace Media.Containers.BaseMedia
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
    //https://dvcs.w3.org/hg/html-media/raw-file/tip/media-source/isobmff-byte-stream-format.html
    public class BaseMediaReader : MediaFileStream
    {

        static DateTime IsoBaseDateUtc = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //Todo Make Generic.Dictionary and have a ToTextualConvention that tries the Generic.Dictionary first. (KnownParents)        

        /// <summary>
        /// <see href="http://www.mp4ra.org/atoms.html">MP4REG</see>
        /// </summary>
        public static List<string> ParentBoxes = new List<string>()
        {
            "moof", //movie fragment
            "mfhd", //movie fragment header
            "traf", //track fragment
            //tfhd track fragment header
            //trun track fragment run
            //sbgp sample-to-group 
            //sgpd sample group description 
            //subs sub-sample information 
            //saiz sample auxiliary information sizes 
            //saio sample auxiliary information offsets 
            //tfdt track fragment decode time 
            "mfra", //movie framgment radom access
            //"tfra", //8.8.10 track fragment random access
            //"mfro", //* 8.8.11 movie fragment random access offset 
            "moov",
            "trak",
            "mdia",
            //"mdhd",
            //"hdlr",
            "minf",
            "dinf",
            "stbl",
            "edts",            
            "stsd",
            //"tkhd", //Track Header
            "tref", //Track Reference Container
            "trgr", //Track Grouping Indicator
            "skip",
            //"udta",
            "mvex", //movie extends box
            //mehd 8.8.2 movie extends header box
            //trex * 8.8.3 track extends defaults
            //leva 8.8.13 level assignment 
            "cprt",
            "strk", //sub track
            //"stri", //sub track information box
            //"strd" //sub track definition box
            "meta",
            "iloc",
            "ipro",
            "sinf",
            //frma 8.12.2 original format box
            //schm 8.12.5 scheme type box
            //schi 8.12.6 scheme information box 
            "fiin", //file delivery item information 
            "paen", //partition entry 
            "segr", //file delivery session group 
            "gitn", //group id to name 
            "meco" //additional metadata container 
        };

        //TryRegisterParentBox
        //Try UnregisterParentBox

        //Should be int type..

        const int MinimumSize = IdentifierSize + LengthSize, IdentifierSize = 4, LengthSize = IdentifierSize;

        public static string ToUTF8FourCharacterCode(byte[] identifier, int offset = 0, int count = 4)
        {
            return ToEncodedFourCharacterCode(Encoding.UTF8, identifier, offset, count);
        }

        public static string ToEncodedFourCharacterCode(Encoding encoding, byte[] identifier, int offset, int count)
        {
            if (encoding == null) throw new ArgumentNullException("encoding");

            if (identifier == null) throw new ArgumentNullException("identifier");

            if (offset + count > identifier.Length) throw new ArgumentOutOfRangeException("offset and count must relfect a position within identifier.");

            return encoding.GetString(identifier, offset, count);
        }

        public BaseMediaReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public BaseMediaReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public BaseMediaReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public BaseMediaReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, null, bufferSize, true) { }        

        //int[] names?
        public IEnumerable<Node> ReadBoxes(long offset, long count, params string[] names)
        {
            long positionStart = Position;

            Position = offset;

            foreach (var box in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(ToUTF8FourCharacterCode(box.Identifier)))
                {
                    yield return box;
                    continue;
                }

                //Ensure the TotalSize is correctly set.

                count -= box.TotalSize > count ? box.TotalSize - box.DataSize : box.TotalSize;

                if (count <= 0 /*&& m_Position >= m_Length*/) break;

            }

            Position = positionStart;

            yield break;
        }

        public IEnumerable<Node> ReadBoxes(long offset = 0, params string[] names) { return ReadBoxes(offset, Length - offset, names); }

        public Node ReadBox(string name, long offset, long count)
        {
            long positionStart = Position;

            Node result = ReadBoxes(offset, count, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public Node ReadBox(string name, long offset = 0) { return ReadBox(name, offset, Length - offset); }

        public static byte[] ReadIdentifier(Stream stream)
        {
            //if (Remaining < IdentifierSize) return null;

            byte[] identifier = new byte[IdentifierSize];

            stream.Read(identifier, 0, IdentifierSize);

            return identifier;
        }

        public static long ReadLength(Stream stream, out int bytesRead, byte[] buffer = null, int offset = 0)
        {
            //4.2 Object Structure 
            bytesRead = 0;
            long length = 0;
            byte[] lengthBytes = buffer ?? new byte[LengthSize];
            do
            {
                /*
                 * if (size==1) {
                    unsigned int(64) largesize;
                    } else if (size==0) {
                    // box extends to end of file
                    } 
                 */

                bytesRead += stream.Read(lengthBytes, offset, LengthSize);
                //Check byte 3 == 1?
                length = (lengthBytes[offset] << 24) + (lengthBytes[offset + 1] << 16) + (lengthBytes[offset + 2] << 8) + lengthBytes[offset + 3];
            } while (length == 1 || (length & 0xffffffff) == 0);
            return length;
        }

        public Node ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            //int lengthBytesRead = 0;

            //long length = ReadLength(this, out lengthBytesRead);

            //byte[] identifier = ReadIdentifier(this);

            //return new Node(this, identifier, lengthBytesRead, Position, length, length <= Remaining);

            Common.MemorySegment lot = new Common.MemorySegment(IdentifierSize);
            //int count = 0; while(0 > (count -= Read(lot.Array, count, lot.Count - count))) { }

            int lengthBytesRead = 0;

            long length = ReadLength(this, out lengthBytesRead, lot.Array);

            //while (Remaining < IdentifierSize && Buffering) { }
                                                                   //Only read the length, the identifier is read next
            return new Node(this, lot, IdentifierSize, LengthSize, Position + IdentifierSize, length, //determine Complete by reading the identifier
                Read(lot.Array, 0, IdentifierSize) + lengthBytesRead >= MinimumSize && length <= Remaining);  //Could also inline the ReadLength(this, out lengthBytesRead, lot.Array) and do the IsComplete check in the Node constructor based on Master.Remaining
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node next = ReadNext();
                if (next == null) yield break;

                yield return next;

                //Parent boxes contain other boxes so do not skip them, parse right into their data
                if (ParentBoxes.Contains(ToUTF8FourCharacterCode(next.Identifier))) continue;

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

        public override string ToTextualConvention(Node node)
        {
            if (node.Master.Equals(this)) return BaseMediaReader.ToUTF8FourCharacterCode(node.Identifier);
            return base.ToTextualConvention(node);
        }

        public bool HasProtection
        {
            //pssh/cenc
            //TrackLevel Encryption = tenc
            get { return ReadBoxes(Root.DataOffset, "ipro", "sinf").Count() >= 1; }
        }

        DateTime? m_Created, m_Modified;

        public DateTime Created
        {
            get
            {
                if (false == m_Created.HasValue) ParseMovieHeader();
                return m_Created.Value;
            }
        }

        public DateTime Modified
        {
            get
            {
                if (false == m_Modified.HasValue) ParseMovieHeader();
                return m_Modified.Value;
            }
        }

        ulong? m_TimeScale;

        TimeSpan? m_Duration;

        public TimeSpan Duration
        {
            get
            {
                if (false == m_Duration.HasValue) ParseMovieHeader();
                return m_Duration.Value;
            }
        }

        float? m_PlayRate, m_Volume;

        public float PlayRate
        {
            get
            {
                if (false == m_PlayRate.HasValue) ParseMovieHeader();
                return m_PlayRate.Value;
            }
        }

        public float Volume
        {
            get
            {
                if (false == m_Volume.HasValue) ParseMovieHeader();
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
                if (false == m_NextTrackId.HasValue) ParseMovieHeader();
                return m_NextTrackId.Value;
            }
        }

        protected void ParseMovieHeader()
        {
            ulong duration;

            //Obtain the timeScale and duration from the LAST? mdhd box, can do but is more latent if the file is large...
            using (var mediaHeader = ReadBox("mvhd", Root.Offset)) // ReadBoxes(Root.Offset, "mvhd").LastOrDefault())
            {
                if (mediaHeader == null) throw new InvalidOperationException("Cannot find 'mvhd' box.");

                int offset = 0;

                int versionAndFlags = Common.Binary.Read32(mediaHeader.Data, offset, BitConverter.IsLittleEndian), version = versionAndFlags >> 24 & 0xff;

                offset += 4;

                ulong created = 0, modified = 0;

                switch (version)
                {
                    case 0:
                        {
                            created = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            break;
                        }

                    case 1:
                        {

                            created = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            modified = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            m_TimeScale = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            duration = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                            offset += 4;

                            break;
                        }
                    default: throw new NotSupportedException();
                }

                //Rate Volume NextTrack

                m_PlayRate = Common.Binary.Read32(mediaHeader.Data, offset, BitConverter.IsLittleEndian) / 65536f;

                offset += 4;

                m_Volume = Common.Binary.ReadU16(mediaHeader.Data, offset, BitConverter.IsLittleEndian) / 256f;

                offset += 2;

                m_Matrix = mediaHeader.Data.Skip(offset).Take(36).ToArray();

                offset += 36;

                offset += 28;

                m_NextTrackId = Common.Binary.Read32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                offset += 4;

                m_Created = IsoBaseDateUtc.AddMilliseconds(created * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);

                m_Modified = IsoBaseDateUtc.AddMilliseconds(modified * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);

                m_Duration = TimeSpan.FromSeconds((double)duration / (double)m_TimeScale.Value);
            }
        }

        //Should be a better box... (meta ,moov, mfra?)?
        public override Node TableOfContents
        {
            get { return ReadBoxes(Root.Offset, "stco", "co64").FirstOrDefault(); }
        }

        List<Track> m_Tracks;

        public override IEnumerable<Track> GetTracks() //bool enabled tracks only?
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

            byte[] codecIndication = Media.Common.MemorySegment.EmptyBytes;

            //Get Duration from mdhd, some files have more then one mdhd.
            if (false == m_Duration.HasValue) ParseMovieHeader();

            //For each trak box in the file
            //TODO Make only a single pass, the data required should always be in the RawData of trakBox
            //E,g, use trackBox.Data stream and switch on identifer name contained in Data
            foreach (var trakBox in ReadBoxes(Root.Offset, "trak").ToArray())
            {
                //MAKE ONLY A SINGLE PASS HERE TO REDUCE IO
                //using (var stream = trakBox.DataStream)
                //{
                //    int bytesRead = 0;

                //    long length = 0, streamPosition = stream.Position, streamLength = stream.Length;

                //    byte[] identifier;

                //    //Note could use RawData from trakBox
                //    //Would just need a way to ReadLength and Identifier from byte[] rather than Stream.

                //    //While there is data in the stream
                //    while (streamPosition < streamLength)
                //    {
                //        //Read the length
                //        length = ReadLength(stream, out bytesRead);

                //        //Read the identifier
                //        identifier = ReadIdentifier(stream);

                //        //Determine what to do
                //        switch (ToFourCharacterCode(identifier))
                //        {
                //            // Next Node has data
                //            case "trak": continue;
                //            case "tkhd":
                //                {
                //                    break;
                //                }
                //            case "mdhd":
                //                {
                //                    break;
                //                }
                //            case "stsd":
                //                {
                //                    break;
                //                }
                //            case "stts":
                //                {
                //                    break;
                //                }
                //            case "stsz":
                //                {
                //                    break;
                //                }
                //            case "stco":
                //                {
                //                    break;
                //                }
                //            case "st64":
                //                {
                //                    break;
                //                }
                //            case "hdlr":
                //                {
                //                    break;
                //                }
                //            case "name":
                //                {
                //                    break;
                //                }
                //            default:
                //                {
                //                    streamPosition = stream.Position += length;
                //                    continue;
                //                }
                //        }
                //    }
                //}

                //Should come right after trak header
                var trakHead = ReadBox("tkhd", trakBox.Offset);

                int offset = 0;

                int version = trakHead.Data[offset++], flags = Common.Binary.Read24(trakHead.Data, offset, BitConverter.IsLittleEndian);

                offset += 3;

                enabled = ((flags & 1) == flags);

                inMovie = ((flags & 2) == flags);

                inPreview = ((flags & 3) == flags);

                if (version == 0)
                {
                    created = Common.Binary.ReadU32(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    modified = Common.Binary.ReadU32(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;
                }
                else
                {
                    created = Common.Binary.ReadU64(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 8;

                    modified = Common.Binary.ReadU64(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 8;
                }

                trackId = Common.Binary.Read32(trakHead.Data, offset, BitConverter.IsLittleEndian);

                //Skip
                offset += 8;

                //Get Duration
                if (version == 0)
                {
                    duration = Common.Binary.ReadU32(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;
                }
                else
                {
                    duration = Common.Binary.ReadU64(trakHead.Data, offset, BitConverter.IsLittleEndian);

                    offset += 8;
                }

                if (duration == 4294967295L) duration = ulong.MaxValue;

                //Reserved
                offset += 8;

                int layer = Common.Binary.ReadU16(trakHead.Data, offset, BitConverter.IsLittleEndian);

                offset += 2;

                int altGroup = Common.Binary.ReadU16(trakHead.Data, offset, BitConverter.IsLittleEndian);

                offset += 2;

                float volume = Common.Binary.ReadU16(trakHead.Data, offset, BitConverter.IsLittleEndian) / 256;

                //Skip int and Matrix
                offset += 40;

                //Width
                width = Common.Binary.Read32(trakHead.Data, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;

                offset += 4;
                //Height

                height = Common.Binary.Read32(trakHead.Data, offset, BitConverter.IsLittleEndian) / ushort.MaxValue;

                offset += 4;

                ulong trackTimeScale = m_TimeScale.Value, trackDuration = duration;

                DateTime trackCreated = m_Created.Value, trackModified = m_Modified.Value;

                //Read the mediaHeader (use overload with count to ensure we do not over read)
                var mediaHeader = ReadBox("mdhd", trakBox.Offset);
                if (mediaHeader != null)
                {
                    offset = 0;

                    version = mediaHeader.Data[offset++];

                    flags = Common.Binary.Read24(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                    offset += 3;

                    ulong mediaCreated, mediaModified, timescale, mediaduration;

                    if (version == 0)
                    {

                        mediaCreated = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaModified = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        timescale = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        mediaduration = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 4;
                    }
                    else
                    {
                        mediaCreated = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaModified = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        timescale = Common.Binary.ReadU32(mediaHeader.Data, offset, BitConverter.IsLittleEndian);

                        offset += 8;

                        mediaduration = Common.Binary.ReadU64(mediaHeader.Data, offset, BitConverter.IsLittleEndian);
                    }

                    trackTimeScale = timescale;

                    trackDuration = mediaduration;

                    trackCreated = IsoBaseDateUtc.AddMilliseconds(mediaCreated * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);

                    trackModified = IsoBaseDateUtc.AddMilliseconds(mediaModified * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);
                }

                var sampleToTimeBox = ReadBox("stts", trakBox.Offset);

                List<Tuple<long, long>> entries = new List<Tuple<long, long>>();

                List<long> offsets = new List<long>();

                List<int> sampleSizes = new List<int>();

                if (sampleToTimeBox != null)
                {
                    //Skip Flags and Version
                    offset = LengthSize;

                    int entryCount = Common.Binary.Read32(sampleToTimeBox.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Sample Count Sample Duration
                        entries.Add(new Tuple<long, long>(Common.Binary.Read32(sampleToTimeBox.Data, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(sampleToTimeBox.Data, offset + 4, BitConverter.IsLittleEndian)));
                        offset += MinimumSize;
                    }
                }

                var sampleToSizeBox = ReadBox("stsz", trakBox.Offset);

                if (sampleToSizeBox != null)
                {
                    //Skip Flags and Version
                    offset = MinimumSize;

                    int defaultSize = Common.Binary.Read32(sampleToSizeBox.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    int count = Common.Binary.Read32(sampleToSizeBox.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    if (defaultSize == 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            sampleSizes.Add(Common.Binary.Read32(sampleToSizeBox.Data, offset, BitConverter.IsLittleEndian));

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
                        //Skip Flags and Version
                        offset = MinimumSize;

                        int chunkCount = Common.Binary.Read32(chunkOffsetsBox.Data, offset, BitConverter.IsLittleEndian);

                        offset += 4;

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            offsets.Add(Common.Binary.Read64(chunkOffsetsBox.Data, offset, BitConverter.IsLittleEndian));

                            offset += 8;
                        }
                    }

                }
                else
                {
                    //Skip Flags and Version
                    offset = LengthSize;

                    int chunkCount = Common.Binary.Read32(chunkOffsetsBox.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < chunkCount; ++i)
                    {
                        offsets.Add((long)Common.Binary.Read32(chunkOffsetsBox.Data, offset, BitConverter.IsLittleEndian));

                        offset += 4;
                    }
                }

                TimeSpan calculatedDuration = TimeSpan.FromSeconds(trackDuration / (double)trackTimeScale);

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                var hdlr = ReadBox("hdlr", trakBox.Offset);

                string comp = ToUTF8FourCharacterCode(hdlr.Data, LengthSize), sub = ToUTF8FourCharacterCode(hdlr.Data, LengthSize * 2);

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

                if (nameBox != null) name = Encoding.UTF8.GetString(nameBox.Data);

                ulong sampleCount = (ulong)sampleSizes.Count();

                if (sampleCount == 0 && entries.Count > 0) sampleCount = (ulong)entries[0].Item1;

                double rate = mediaType == Sdp.MediaType.audio ? trackTimeScale : (double)((double)sampleCount / ((double)trackDuration / trackTimeScale));

                var sampleDescriptionBox = ReadBox("stsd", trakBox.Offset);

                //H264
                // stsd/avc1/avcC contains a field 'lengthSizeMinusOne' specifying the length. But the default is 4.

                int sampleDescriptionCount = sampleDescriptionBox == null ? 0 : Common.Binary.Read32(sampleDescriptionBox.Data, LengthSize, BitConverter.IsLittleEndian);

                byte channels = 0, bitDepth = 0;

                offset = MinimumSize;

                if (sampleDescriptionCount > 0)
                {
                    for (int i = 0; i < sampleDescriptionCount; ++i)
                    {
                        int len = Common.Binary.Read32(sampleDescriptionBox.Data, offset, BitConverter.IsLittleEndian) - 4;
                        offset += 4;

                        var sampleEntry = sampleDescriptionBox.Data.Skip(offset).Take(len);
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

                                    //esds box for codec specific data.

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
                    //Skip Flags and Version
                    offset = LengthSize;

                    int entryCount = Common.Binary.Read32(elst.Data, offset, BitConverter.IsLittleEndian);

                    offset += 4;

                    for (int i = 0; i < entryCount; ++i)
                    {
                        //Edit Duration, MediaTime, Rate
                        edits.Add(new Tuple<int, int, float>(Common.Binary.Read32(elst.Data, offset, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.Data, offset + 4, BitConverter.IsLittleEndian),
                            Common.Binary.Read32(elst.Data, offset + 8, BitConverter.IsLittleEndian) / ushort.MaxValue));

                        offset += 12;
                    }

                    if (edits.Count > 0 && edits[0].Item2 > 0)
                    {
                        startTime = TimeSpan.FromMilliseconds(edits[0].Item2);
                    }

                    elst = null;
                }

                Track createdTrack = new Track(trakBox, name, trackId, trackCreated, trackModified, (long)sampleCount, width, height, startTime, calculatedDuration, rate, mediaType, codecIndication, channels, bitDepth, enabled);

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
