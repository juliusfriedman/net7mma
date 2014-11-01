using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Nut
{
    /// <summary>
    /// Provides an implementation of the Nut Container defined by (MPlayer, FFmpeg and Libav)
    /// </summary>
    /// <notes><see cref="http://ffmpeg.org/~michael/nut.txt">Specification</see></notes>
    //https://www.ffmpeg.org/doxygen/2.3/nut_8c_source.html
    //http://people.xiph.org/~xiphmont/containers/nut/nut-english.txt
    //https://github.com/lu-zero/nut/wiki/Specification
    //https://github.com/Distrotech/libnut/blob/master/docs/nut.txt
    //https://github.com/lu-zero/nut/blob/master/docs/nut4cc.txt
    //https://github.com/lu-zero/nut/blob/master/docs/nutissues.txt
    public class NutReader : MediaFileStream
    {

        [Flags]
        public enum FrameFlags
        {
            Unknown = 0,
            Key = 1,
            EOR = 2,
            CodedPTS = 8,
            StreamId = 16,
            SizeMSB = 32,
            Checksum = 64,
            Reserved = 128,
            HeaderIndex = 1024,
            MatchTime = 2048,
            Coded = 4096,
            Invalid = 8192
        }

        #region Constants

        const int MinimumVersion = 2, MaximumVersion = 4, StableVersion = 3, 
            LengthBits = 7,
            IdentifierSize = LengthBits + 1, 
            MinimumSize = IdentifierSize + 1, 
            IdentifierBytesSize = IdentifierSize - 1,
            MaximumHeaderOptions = 256,
            ForwardPointerWithChecksum = 4096, MultiByteLength = 0x80, LengthMask = sbyte.MaxValue, MaximumEllisionTotal = 1024, DefaultMaxDistance = MaximumEllisionTotal * 32 - 1;

        const byte NutByte = (byte)'N';

        #endregion

        /// <summary>
        /// Defines the start codes used by the container format.
        /// </summary>
        public enum StartCode : ulong
        {
            Frame = 0,
            Main = 0x7A561F5F04ADUL + (((ulong)(NutByte << 8) + 'M') << 48),
            Stream = 0x11405BF2F9DBUL + (((ulong)(NutByte << 8) + 'S') << 48),
            SyncPoint = 0xE4ADEECA4569UL + (((ulong)(NutByte << 8) + 'K') << 48),
            Index = 0xDD672F23E64EUL + (((ulong)(NutByte << 8) + 'X') << 48),
            Info = 0xAB68B596BA78UL  + (((ulong)(NutByte << 8) + 'I') << 48)
        }

        #region Statics        

        public static string ToTextualConvention(byte[] identifier, int offset = 0)
        {
            return ((StartCode)Common.Binary.ReadU64(identifier, offset, BitConverter.IsLittleEndian)).ToString();
        }

        #endregion

        public NutReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public NutReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Node> ReadTags(long offset, long count, params StartCode[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var tag in this)
            {
                if (names == null || names.Count() == 0 || names.Contains((StartCode)Common.Binary.ReadU64(tag.Identifier, 0, BitConverter.IsLittleEndian)))
                    yield return tag;

                count -= tag.Size;

                if (count <= 0) break;

            }

            Position = position;
        }

        public Node ReadTag(StartCode name, long position)
        {
            long positionStart = Position;

            Node result = ReadTags(position, Length - position, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public long DecodeVeraibleLength(System.IO.Stream stream, out int bytesRead)
        {

            bytesRead = 0;

            if (stream.Length - stream.Position < 1) return 0;

            unchecked
            {
                long val = 0;
                int tmp;
                bytesRead = 0;
                do
                {
                    tmp = stream.ReadByte();
                    val = ((val << LengthBits) + (tmp & LengthMask));
                    ++bytesRead;
                } while ((tmp & MultiByteLength) > 0);
                return val;
            }
        }

        string m_FileIdString;

        public string FileIdString
        {
            get
            {
                if (m_FileIdString == null) ParseFileIdString();
                return m_FileIdString;
            }
        }

        void ParseFileIdString()
        {
            if (!string.IsNullOrEmpty(m_FileIdString)) return;

            List<byte> bytes = new List<byte>(24);

            while (Remaining > 0 && Position < DefaultMaxDistance)
            {
                byte read = (byte)ReadByte();

                bytes.Add(read);

                if (read == default(byte)) break;
            }

            m_FileIdString = Encoding.UTF8.GetString(bytes.ToArray());
        }

        int? m_Version, m_StreamCount, m_MaxDistance;

        List<long> m_TimeBases;

        public List<long> TimeBases
        {
            get
            {
                if (m_TimeBases == null) ParseMainHeader();
                return m_TimeBases;
            }
        }

        long? m_EllisionHeaderCount, m_MainHeaderFlags;

        List<long> m_EllisionHeaders;

        public int Version
        {
            get
            {
                if (!m_Version.HasValue) ParseMainHeader();
                return m_Version.Value;
            }
        }

        public bool IsStableVersion { get { return Version >= StableVersion; } }

        public int StreamCount
        {
            get
            {
                if (!m_StreamCount.HasValue) ParseMainHeader();
                return m_StreamCount.Value;
            }
        }

        public int MaximumDistance
        {
            get
            {
                if (!m_MaxDistance.HasValue) ParseMainHeader();
                return m_MaxDistance.Value;
            }
        }

        public long EllisionHeaderCount
        {
            get
            {
                if (!m_EllisionHeaderCount.HasValue) ParseMainHeader();
                return m_EllisionHeaderCount.Value;
            }
        }

        public List<long> EllisionHeaders
        {
            get
            {
                if (m_EllisionHeaders == null) ParseMainHeader();
                return m_EllisionHeaders;
            }
        }

        //Shouldn't need the 8th item or the 6th item
        public List<Tuple<long, long, long, long, long, long, long, Tuple<long>>> HeaderOptions
        {
            get
            {
                if (m_HeaderOptions == null) ParseMainHeader();
                return m_HeaderOptions;
            }
        }

        List<Tuple<long, long, long, long, long, long, long, Tuple<long>>> m_HeaderOptions;

        //MainPackage?
        void ParseMainHeader()
        {
            if (m_MaxDistance.HasValue) return;            

            using (var root = Root) using(var stream = root.Data)
            {
                int bytesRead = 0;
                m_Version = (int)DecodeVeraibleLength(stream, out bytesRead);

                if (m_Version < MinimumVersion || m_Version > MaximumVersion) throw new InvalidOperationException("Unsupported Version");

                m_StreamCount = (int)DecodeVeraibleLength(stream, out bytesRead);

                m_MaxDistance = (int)DecodeVeraibleLength(stream, out bytesRead);

                if (m_MaxDistance > 65536) throw new InvalidOperationException("Invalid MaxiumDistance (Must be less than 65536) Found: " + m_MaxDistance.Value);

                int timeBaseCount = (int)DecodeVeraibleLength(stream, out bytesRead);

                if (timeBaseCount == 0) throw new InvalidOperationException("No Timebase");

                m_TimeBases = new List<long>(timeBaseCount);

                //None should be 0
                //None should have a GCD != 1
                //None must be > 1 << 31
                //None should be equal
                for (int i = 0; i < timeBaseCount; ++i)
                    m_TimeBases.Add(DecodeVeraibleLength(stream, out bytesRead) / DecodeVeraibleLength(stream, out bytesRead));

                long tmp_pts = 0;
                long tmp_mul = 1;
                long tmp_stream = 0;
                long tmp_match = 1 - (1 << 62);
                long tmp_head_idx = 0;
                long tmp_flag = 0;
                long tmp_fields = 0;
                long tmp_size = 0;
                long tmp_res = 0;

                long count = 0;

                //This is essentially an index?
                m_HeaderOptions = new List<Tuple<long, long, long, long, long, long, long, Tuple<long>>>();

                for (int i = 0; i < MaximumHeaderOptions;)
                {
                    tmp_flag = DecodeVeraibleLength(stream, out bytesRead);
                    tmp_fields = DecodeVeraibleLength(stream, out bytesRead);

                    //Signed
                    if (tmp_fields > 0) tmp_pts = DecodeVeraibleLength(stream, out bytesRead);

                    if (tmp_fields > 1) tmp_mul = DecodeVeraibleLength(stream, out bytesRead);

                    if (tmp_fields > 2) tmp_stream = DecodeVeraibleLength(stream, out bytesRead);

                    if (tmp_fields > 3) tmp_size = DecodeVeraibleLength(stream, out bytesRead);
                    else tmp_size = 0;

                    if (tmp_fields > 4) tmp_res = DecodeVeraibleLength(stream, out bytesRead);
                    tmp_res = 0;

                    if (tmp_fields > 5) count = DecodeVeraibleLength(stream, out bytesRead);
                    else
                    {
                        if (tmp_size > tmp_mul) throw new InvalidOperationException("count underflow");
                        count = tmp_mul - tmp_size;
                        if (count == 0) throw new InvalidOperationException("count is 0");
                    }

                    if (tmp_fields > 6)
                    {
                        //Signed
                        tmp_match = DecodeVeraibleLength(stream, out bytesRead);
                        
                        //Sanity
                        if (tmp_match <= -32768 || tmp_match >= 32768 
                            &&
                            tmp_match != 1 - (1 << 62)) throw new InvalidOperationException("absolute delta match time must be less than 32768");
                    }

                    if (tmp_fields > 7) tmp_head_idx = DecodeVeraibleLength(stream, out bytesRead);

                    //for (int j = 8; j < tmp_fields; ++j) tmp_res = DecodeLength(stream, out bytesRead);
                    while (tmp_fields-- > 8) DecodeVeraibleLength(stream, out bytesRead);

                    if (count == 0 || i + count > MaximumHeaderOptions) throw new InvalidOperationException("Invalid count for header: " + i + ", count: " + count);

                    //Read the HeaderOption (should also be bounded by length?)
                    for (int j = 0; j < count && i < MaximumHeaderOptions; j++, i++)
                    {
                        if (tmp_stream > m_StreamCount) throw new InvalidOperationException("Illegal stream number:" + tmp_stream + ", " + m_StreamCount);

                        if (i == NutByte)
                        {
                            m_HeaderOptions.Add(new Tuple<long, long, long, long, long, long, long, Tuple<long>>((long)FrameFlags.Invalid, 0, 0, 0, 0, 0, 0, new Tuple<long>(0)));
                            j--;
                            continue;
                        }
                        /* Must read this because header_idx is found here
                         * Must read this because reserved_count is defined here
                            flags[i]= tmp_flag;
                            stream_id[i]= tmp_stream;
                            data_size_mul[i]= tmp_mul;
                            data_size_lsb[i]= tmp_size + j;
                            pts_delta[i]= tmp_pts;
                            reserved_count[i]= tmp_res;
                            match_time_delta[i]= tmp_match;
                            header_idx[i]= tmp_head_idx;
                        */
                        m_HeaderOptions.Add(new Tuple<long, long, long, long, long, long, long, Tuple<long>>(tmp_flag, tmp_stream, tmp_mul, tmp_size + j, tmp_pts, tmp_match, tmp_head_idx, new Tuple<long>(tmp_res)));
                    }
                }

                if ((FrameFlags)m_HeaderOptions[NutByte].Item1 != FrameFlags.Invalid) throw new InvalidOperationException("Invalid Header Tables");

                m_EllisionHeaderCount = Math.Min(DecodeVeraibleLength(stream, out bytesRead), MultiByteLength);

                //The first Ellision Header must be 0
                /*
                elision_header[header_idx] (vb)
                For frames with a final size <= 4096 this header is prepended to the
                frame data. That is if the stored frame is 4000 bytes and the
                elision_header is 96 bytes then it is prepended, if it is 97 byte then it
                is not.
                elision_header[0] is fixed to a length 0 header.
                The length of each elision_header except header 0 MUST be < 256 and >0.
                The sum of the lengthes of all elision_headers MUST be <=1024.
                */

                m_EllisionHeaders = new List<long>((int)m_EllisionHeaderCount + 1) { 0 };

                //Read Ellision Headers (Note bounded by length)
                for (int h = 0, end = (int)(stream.Length - 1); h < m_EllisionHeaderCount && stream.Position < end; ++h)
                {
                    long headerLength = DecodeVeraibleLength(stream, out bytesRead);

                    //FROM FFMPEG Should ensure every value is > 0 && < 256
                    if (headerLength < 0 || headerLength > MaximumHeaderOptions) throw new InvalidOperationException("headerLength Must be > 0 && < 256, found: " + headerLength);

                    m_EllisionHeaders.Add(headerLength);
                }

                if (m_EllisionHeaderCount > 0 && m_EllisionHeaders.Sum() > MaximumEllisionTotal)  throw new InvalidOperationException("Invalid Ellision Header Summation");

                //Usually has a BROADCAST flag?
                m_MainHeaderFlags = DecodeVeraibleLength(stream, out bytesRead);
                //reserved_bytes
            }
        }

        public Node ReadNext()
        {
            //long offset = Position;

            byte nextByte = (byte)ReadByte();

            if (nextByte == NutByte)
            {
                byte[] identifier = new byte[] { NutByte, 0, 0, 0, 0, 0, 0, 0 };

                Read(identifier, 1, IdentifierBytesSize);

                int lengthSize = 0;

                long length = DecodeVeraibleLength(this, out lengthSize);

                return new Node(this, identifier, Position, length, length <= Remaining);
            }
            else
            {
                //  if (avio_tell(bc) > nut->last_syncpoint_pos + nut->max_distance) 
                //if (Position > (m_MaxDistance ?? DefaultMaxDistance)) throw new InvalidOperationException("Last frame must have been damaged.");

                /*
                            Frame Coding

                            Each frame begins with a "framecode", a single byte which indexes a
                            table in the main header. This table can associate properties such as
                            stream ID, size, relative timestamp, keyframe flag, etc. with the
                            frame that follows, or allow the values to be explicitly coded
                            following the framecode byte. By careful construction of the framecode
                            table in the main header, an average overhead of significantly less
                            than 2 bytes per frame can be achieved for single-stream files at low
                            bitrates.
                            Framecodes can also be flagged as invalid, and seeing such a framecode
                            indicates a damaged file. The frame code 0x4E ('N') is a special invalid
                            framecode which marks the next packet as a NUT packet, and not a frame.
                            The following 7 bytes, combined with 'N', are the full startcode of
                            the NUT packet.
                */

                FrameFlags frameFlags = (FrameFlags)HeaderOptions[nextByte].Item1;

                //Check for invalid flag
                if (frameFlags.HasFlag(FrameFlags.Invalid)) throw new InvalidOperationException("FrameCodes must not have the flag \"FrameFlags.Invalid\"");

                long reserved_count = HeaderOptions[nextByte].Rest.Item1;

                int header_idx = (int)HeaderOptions[nextByte].Item7;

                long size_mul = HeaderOptions[nextByte].Item3;

                long size_msb = 0, size_lsb = HeaderOptions[nextByte].Item4;

                int bytesRead;

                long length = size_lsb, temp;

                //Check to see if the real flags are in the data
                if (frameFlags.HasFlag(FrameFlags.Coded))
                {
                    temp = DecodeVeraibleLength(this, out bytesRead);
                    frameFlags ^= (FrameFlags)temp;
                }

                //Check for invalid flag
                if (frameFlags.HasFlag(FrameFlags.Invalid)) throw new InvalidOperationException("FrameCodes must not have the flag \"FrameFlags.Invalid\"");

                if (frameFlags.HasFlag(FrameFlags.StreamId))
                {
                    temp = DecodeVeraibleLength(this, out bytesRead);

                    //Checks...
                }

                if (frameFlags.HasFlag(FrameFlags.CodedPTS))
                {
                    temp = DecodeVeraibleLength(this, out bytesRead);
                }
                else
                {
                    //Todo
                    //Decode PTS
                }

                //Check to see if the size is coded in the data
                if (frameFlags.HasFlag(FrameFlags.SizeMSB)) size_msb = DecodeVeraibleLength(this, out bytesRead);

                if (frameFlags.HasFlag(FrameFlags.MatchTime)) temp = DecodeVeraibleLength(this, out bytesRead);

                if (frameFlags.HasFlag(FrameFlags.HeaderIndex)) temp = DecodeVeraibleLength(this, out bytesRead);

                //7  FLAG_RESERVED    If set, reserved_count is coded in the frame header.
                //reserved_count[frame_code] (v)
                //MUST be <256.
                if (frameFlags.HasFlag(FrameFlags.Reserved))
                {
                    reserved_count = DecodeVeraibleLength(this, out bytesRead);

                    //while (reserved_count-- > 0)

                    //for (int i = 0; i < temp; ++i)
                    //{
                    //    DecodeLength(this, out bytesRead);
                    //    if (bytesRead == 0) break;//?
                    //}
                }

                //from MainHeader
                //if (length > MaximumDistance) header_idx = 0;

                //length -= EllisionHeaders[header_idx];

                length = size_msb * size_mul + size_lsb;

                /*
                EOR frames MUST be zero-length and must be set keyframe.
               All streams SHOULD end with EOR, where the pts of the EOR indicates the (NOT AN EXCEPTION CASE)
               end presentation time of the final frame.
               An EOR set stream is unset by the first content frame.
               EOR can only be unset in streams with zero decode_delay .
               FLAG_CHECKSUM MUST be set if the frame's data_size is strictly greater than
               2*max_distance or the difference abs(pts-last_pts) is strictly greater than
               max_pts_distance (where pts represents this frame's pts and last_pts is
               defined as below).

                */

                if (frameFlags.HasFlag(FrameFlags.EOR))
                {
                    if (!frameFlags.HasFlag(FrameFlags.Key)) throw new InvalidOperationException("EOR Frames must be key");

                    if (length != 0) throw new InvalidOperationException("EOR Frames must have size 0");
                }
                

                if (frameFlags.HasFlag(FrameFlags.Checksum))
                {
                    //Checksum

                    //Position += 4;

                    //Do this so the Frame can be optionall CRC'd by the Enumerator if CheckCRC is true
                    //length += 4;
                }
                else if (length > (2 * MaximumDistance)) throw new InvalidOperationException("frame size > 2 max_distance and no checksum");

                //Can store 6 more bytes in identifier

                return new Node(this, new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)frameFlags }, Position, length, length <= Remaining);
            }
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node result = ReadNext();

                if (result == null) yield break;

                yield return result;

                //Determine if store offset of last syncpoint...

                //Determine if discard frame.

                Skip(result.Size);
            }
        }

        List<Track> m_Tracks;

        Dictionary<int, Node> m_StreamPackages;

        void ParseStreamPackages()
        {
            if (m_StreamPackages != null) return;

            m_StreamPackages = new Dictionary<int, Node>();

            int bytesRead = 0;

            /*
            Streams

            A NUT file consists of one or more streams, intended to be presented
            simultaneously in synchronization with one another. Use of streams as
            independent entities is discouraged, and the nature of NUT's ordering
            requirements on frames makes it highly disadvantageous to store
            anything except the audio/video/subtitle/etc. components of a single
            presentation together in a single NUT file. Nonlinear playback order,
            scripting, and such are topics outside the scope of NUT, and should be
            handled at a higher protocol layer should they be desired (for
            example, using several NUT files with an external script file to
            control their playback in combination).

            A single media encoding format is associated with each stream. The
            stream headers convey properties of the encoding, such as video frame
            dimensions, sample rates, and the compression standard ("codec") used
            (if any). Stream headers may also carry with them an opaque, binary
            object in a codec-specific format, containing global parameters for
            the stream such as codebooks. Both the compression format and whatever
            parameters are stored in the stream header (including NUT fields and
            the opaque global header object) are constant for the duration of the
            stream.

            Each stream has a last_pts context. For compression, every frame's
            pts is coded relatively to the last_pts. In order for demuxing to resume
            from arbitrary points in the file, all last_pts contexts are reset by
            syncpoints.
            */

            //Read all Stream Packages
            foreach (var tag in ReadTags(FileIdString.Length, Length - FileIdString.Length, StartCode.Stream).ToArray()) 
                m_StreamPackages.Add((int)DecodeVeraibleLength(tag.Data, out bytesRead), tag);

            //if (StreamCount != m_StreamPackages.Count) throw new InvalidOperationException("StreamCount does not match Packages with a Stream StartCode.");
        }

        public override IEnumerable<Track> GetTracks()
        {
            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }

            long position = Position;

            ParseMainHeader();

            ParseStreamPackages();

            List<Track> tracks = new List<Track>();

            foreach (var streamId in m_StreamPackages.Keys)
            {
                //Parse the Stream Package

                Node streamPackage = m_StreamPackages[streamId];

                long timeBase = m_TimeBases[streamId];

                using (var stream = streamPackage.Data)
                {
                    int bytesRead = 0;

                    long tempStreamId = DecodeVeraibleLength(stream, out bytesRead);

                    if (tempStreamId != streamId) throw new InvalidOperationException("Stream Package Mismatch");

                    long streamClass = DecodeVeraibleLength(stream, out bytesRead);

                    Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                    byte[] codecIndication = new byte[4];

                    //if(streamClass > 3)

                    switch (streamClass)
                    {
                        case 0:
                            {
                                mediaType = Sdp.MediaType.video;
                                goto default;
                            }
                        case 1:
                            {
                                mediaType = Sdp.MediaType.audio;
                                goto default;
                            }
                        case 2: //Subtitle
                            {
                                mediaType = Sdp.MediaType.text;
                                goto default;
                            }
                        case 3: //Subtitle
                            {
                                mediaType = Sdp.MediaType.data;
                                goto default;
                            }
                        default:
                            {
                                stream.Read(codecIndication, 0, (int)DecodeVeraibleLength(stream, out bytesRead));
                                break;
                            }
                    }
                    
                    //timeBaseId
                    long tempTimeBase = DecodeVeraibleLength(stream, out bytesRead);

                    //if (tempTimeBase != timeBase) throw new InvalidOperationException("Stream Timebase Mismatch");

                    if (tempTimeBase != timeBase) timeBase = m_TimeBases[(int)tempTimeBase];

                    long msb_pts_shift = DecodeVeraibleLength(stream, out bytesRead);
                    long pts_distance = DecodeVeraibleLength(stream, out bytesRead);
                    long decode_delay = DecodeVeraibleLength(stream, out bytesRead);
                    long stream_flags = DecodeVeraibleLength(stream, out bytesRead);

                    long duration = msb_pts_shift * pts_distance;

                    duration += (int)m_HeaderOptions[streamId].Item6 << (int)msb_pts_shift;

                    //extraData for codec
                    stream.Position += DecodeVeraibleLength(stream, out bytesRead) + bytesRead;

                    int width = 0, height = 0;

                    double rate = 0;

                    byte channels = 0, bitDepth = 0;

                    switch (mediaType)
                    {
                        case Sdp.MediaType.video:
                            {
                                //Already read
                                rate = pts_distance;

                                width = (int)DecodeVeraibleLength(stream, out bytesRead);
                                height = (int)DecodeVeraibleLength(stream, out bytesRead);
                                //Aspect Ration (num/dum)

                                DecodeVeraibleLength(stream, out bytesRead);
                                DecodeVeraibleLength(stream, out bytesRead);

                                //csp type
                                bitDepth = (byte)DecodeVeraibleLength(stream, out bytesRead);
                                break;
                            }
                        case Sdp.MediaType.audio:
                            {
                                //Already read
                                duration = pts_distance;

                                //Rational
                                rate = DecodeVeraibleLength(stream, out bytesRead);
                                rate /= DecodeVeraibleLength(stream, out bytesRead);
                                channels = (byte)DecodeVeraibleLength(stream, out bytesRead);
                                break;
                            }
                    }

                    //Map codec indication to 4cc?

                    //foreach(var tag in  ReadTags(Frame))
                    //determine id from frame and count sample

                    //Or use pts_distance
                    int sampleCount = 0;

                    //Duration is off by about 30 seconds? must include rate or some conversion...

                    Track created = new Track(streamPackage, string.Empty, streamId, FileInfo.CreationTimeUtc, FileInfo.LastWriteTimeUtc, sampleCount, height, width, TimeSpan.FromMilliseconds(decode_delay), TimeSpan.FromSeconds(duration), rate, mediaType, codecIndication, channels, bitDepth);

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

        public override Node Root
        {
            get { return ReadTag(StartCode.Main, FileIdString.Length); }
        }

        public override Node TableOfContents
        {
            get { using (var root = Root) return ReadTag(StartCode.Index, root.Offset + root.Size); }
        }
    }
}
