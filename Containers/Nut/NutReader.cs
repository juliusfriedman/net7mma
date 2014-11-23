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
using System.Linq;
using System.Text;
using Media.Container;

namespace Media.Containers.Nut
{
    /// <summary>
    /// Provides an implementation of the Nut Container defined by (MPlayer, FFmpeg and Libav)
    /// </summary>
    /// <notes><see cref="http://ffmpeg.org/~michael/nut.txt">Specification</see></notes>
    //http://wiki.multimedia.cx/index.php?title=NUT
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
            SideMetaData = 256,
            HeaderIndex = 1024,
            MatchTime = 2048,
            Coded = 4096,
            Invalid = 8192
        }

        [Flags]
        public enum HeaderFlags
        {
            Unknown = 0,
            Broadcast = 1,
            Pipe = 2
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

        public static bool IsFrame(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return node.Identifier[0] == default(byte);
        }

        public static bool IsNutNode(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return node.Identifier[0] == NutByte;
        }

        public static FrameFlags GetFrameFlags(NutReader reader, Node node)
        {

            if(node == null) throw new ArgumentNullException("node");

            if (!IsFrame(node)) return FrameFlags.Unknown;

            return (FrameFlags)reader.HeaderOptions[node.Identifier[IdentifierBytesSize]].Item1;
        }

        public static int GetStreamId(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (!IsFrame(node)) return -1;

            return node.Identifier[6];
        }

        public static byte[] GetFrameHeader(NutReader reader, Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (!IsFrame(node)) return Utility.Empty;

            return reader.EllisionHeaders[node.Identifier[5]];
        }

        public static byte[] GetFrameData(NutReader reader, Node node, out byte[] sideData, out byte[] metaData)
        {
            FrameFlags flags = GetFrameFlags(reader, node);

            sideData = metaData = null;

            //Always include the frame header
            IEnumerable<byte> frameData = GetFrameHeader(reader, node);

            //Check if data needs to be removed
            if (flags.HasFlag(FrameFlags.SideMetaData))
            {
                //Compatibility
                //If SizeData in the header was set this is a draft version and the data is at the end of the frame.
                long sidedata_size = reader.HeaderOptions[node.Identifier[IdentifierBytesSize]].Rest.Item2;

                //Check for that condition
                if (sidedata_size > 0)
                {
                    //Use the value which was already decoded when reading the frame.
                    sidedata_size = node.Identifier[4];

                    metaData = null; //Not included in spec.

                    int dataSize = (int)(node.DataSize - sidedata_size);

                    frameData = Enumerable.Concat(frameData,  node.Data.Take(dataSize));

                    sideData = node.Data.Skip(dataSize).ToArray();
                }
                else //Current Spec
                {
                    int bytesReadNow = 0, bytesReadTotal = 0;
                    //Get a stream of the data
                    using (var stream = node.DataStream)
                    {
                        int size = (int)reader.DecodeVariableLength(stream, out bytesReadNow);
                        
                        //Side Data Count (From Info) and read
                        if (size > 0)
                        {
                            bytesReadTotal += bytesReadNow;

                            sideData = new byte[size];

                            reader.Read(sideData, 0, size);

                            bytesReadTotal += size;
                        }

                        //Meta Data Count (From Info) and read
                        size = (int)reader.DecodeVariableLength(stream, out bytesReadNow);

                        if (size > 0)
                        {
                            bytesReadTotal += bytesReadNow;

                            metaData = new byte[size];

                            reader.Read(sideData, 0, size);

                            bytesReadTotal += size;
                        }

                        //The position of this stream is now @ the end of the data which does not belong to the frame itself.
                        frameData = Enumerable.Concat(frameData,  node.Data.Skip(bytesReadTotal));
                    }
                }
            }
            else //The data of the frame is as it is
            {
                frameData = Enumerable.Concat(frameData, node.Data);
            }

            //Return the allocated array
            return frameData.ToArray();
        }

        #endregion

        public NutReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public NutReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public NutReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Node> ReadTags(long offset, long count, params StartCode[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var tag in this)
            {
                if (names == null || names.Count() == 0 || names.Contains((StartCode)Common.Binary.ReadU64(tag.Identifier, 0, BitConverter.IsLittleEndian)))
                    yield return tag;

                count -= tag.TotalSize;

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

        public long DecodeVariableLength(System.IO.Stream stream, out int bytesRead)
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

        public DateTime Created { get { return FileInfo.CreationTimeUtc; } }

        public DateTime Modified { get { return FileInfo.LastWriteTimeUtc; } }

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

        int? m_MajorVersion, m_MinorVersion, m_StreamCount, m_MaxDistance;

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

        List<byte[]> m_EllisionHeaders;

        public Version Version
        {
            get
            {
                if (!m_MajorVersion.HasValue) ParseMainHeader();
                return new Version(m_MajorVersion.Value, m_MinorVersion ?? 0);
            }
        }

        public bool IsStableVersion { get { return Version.Major >= StableVersion; } }

        public bool HasMainHeaderFlags { get { return Version.Major > StableVersion; } }

        public HeaderFlags MainHeaderFlags
        {
            get
            {
                if (!HasMainHeaderFlags) return HeaderFlags.Unknown;
                return (HeaderFlags)m_MainHeaderFlags.Value;
            }
        }

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

        public List<byte[]> EllisionHeaders
        {
            get
            {
                if (m_EllisionHeaders == null) ParseMainHeader();
                return m_EllisionHeaders;
            }
        }

        //(Main)Frame(Headers/Options) is possibly a better name.
        //Shouldn't need the 8th, 9th item or the 6th item
        public List<Tuple<long, long, long, long, long, long, long, Tuple<long, long>>> HeaderOptions
        {
            get
            {
                if (m_HeaderOptions == null) ParseMainHeader();
                return m_HeaderOptions;
            }
        }

        List<Tuple<long, long, long, long, long, long, long, Tuple<long, long>>> m_HeaderOptions;

        void ParseMainHeader()
        {
            if (m_MaxDistance.HasValue) return;            

            using (var root = Root) using(var stream = root.DataStream)
            {
                int bytesRead = 0, end = (int)root.DataSize;

                m_MajorVersion = (int)DecodeVariableLength(stream, out bytesRead);

                if (m_MajorVersion < MinimumVersion || m_MajorVersion > MaximumVersion) throw new InvalidOperationException("Unsupported Version");

                //2.4 reads minor version
                if (m_MajorVersion > StableVersion) m_MinorVersion = (int)DecodeVariableLength(stream, out bytesRead);

                m_StreamCount = (int)DecodeVariableLength(stream, out bytesRead);

                m_MaxDistance = (int)DecodeVariableLength(stream, out bytesRead);

                if (m_MaxDistance > 65536) throw new InvalidOperationException("Invalid MaxiumDistance (Must be less than 65536) Found: " + m_MaxDistance.Value);

                int timeBaseCount = (int)DecodeVariableLength(stream, out bytesRead);

                if (timeBaseCount == 0) throw new InvalidOperationException("No Timebase");

                m_TimeBases = new List<long>(timeBaseCount);

                //None should be 0
                //None should have a GCD != 1
                //None must be > 1 << 31
                //None should be equal
                for (int i = 0; i < timeBaseCount; ++i)
                    m_TimeBases.Add(DecodeVariableLength(stream, out bytesRead) / DecodeVariableLength(stream, out bytesRead));

                long tmp_pts = 0;
                long tmp_mul = 1;
                long tmp_stream = 0;
                long tmp_match = 1 - (1 << 62);
                long tmp_head_idx = 0;
                long tmp_flag = 0;
                long tmp_fields = 0;
                long tmp_size = 0;
                long tmp_res = 0;
                long tmp_side = 0;

                long count = 0;

                //This is essentially an index, could be byte[]64... but spec is under development...
                m_HeaderOptions = new List<Tuple<long, long, long, long, long, long, long, Tuple<long, long>>>();

                for (int i = 0; i < MaximumHeaderOptions;)
                {
                    tmp_flag = DecodeVariableLength(stream, out bytesRead);
                    tmp_fields = DecodeVariableLength(stream, out bytesRead);

                    //Signed
                    if (tmp_fields > 0) tmp_pts = DecodeVariableLength(stream, out bytesRead);

                    if (tmp_fields > 1) tmp_mul = DecodeVariableLength(stream, out bytesRead);

                    if (tmp_fields > 2) tmp_stream = DecodeVariableLength(stream, out bytesRead);

                    if (tmp_fields > 3) tmp_size = DecodeVariableLength(stream, out bytesRead);
                    else tmp_size = 0;

                    if (tmp_fields > 4) tmp_res = DecodeVariableLength(stream, out bytesRead);
                    tmp_res = 0;

                    if (tmp_fields > 5) count = DecodeVariableLength(stream, out bytesRead);
                    else
                    {
                        if (tmp_size > tmp_mul) throw new InvalidOperationException("count underflow");
                        count = tmp_mul - tmp_size;
                        if (count == 0) throw new InvalidOperationException("count is 0");
                    }

                    if (tmp_fields > 6)
                    {
                        //Signed
                        tmp_match = DecodeVariableLength(stream, out bytesRead);
                        
                        //Sanity
                        if (tmp_match <= short.MinValue || tmp_match > short.MaxValue 
                            &&
                            tmp_match != 1 - (1 << 62)) throw new InvalidOperationException("absolute delta match time must be less than or equal to short.MaxValue");
                    }

                    if (tmp_fields > 7) tmp_head_idx = DecodeVariableLength(stream, out bytesRead);

                    if (tmp_fields > 8) tmp_side = DecodeVariableLength(stream, out bytesRead);

                    //for (int j = 8; j < tmp_fields; ++j) tmp_res = DecodeLength(stream, out bytesRead);
                    while (tmp_fields-- > 8) DecodeVariableLength(stream, out bytesRead);

                    if (count == 0 || i + count > MaximumHeaderOptions) throw new InvalidOperationException("Invalid count for header: " + i + ", count: " + count);

                    //Read the HeaderOption (should also be bounded by length?)
                    for (int j = 0; j < count && i < MaximumHeaderOptions; j++, i++)
                    {
                        if (tmp_stream > m_StreamCount) throw new InvalidOperationException("Illegal stream number:" + tmp_stream + ", " + m_StreamCount);

                        if (i == NutByte)
                        {
                            m_HeaderOptions.Add(new Tuple<long, long, long, long, long, long, long, Tuple<long, long>>((long)FrameFlags.Invalid, 0, 0, 0, 0, 0, 0, new Tuple<long, long>(0, 0)));
                            j--;
                            continue;
                        }

                        m_HeaderOptions.Add(new Tuple<long, long, long, long, long, long, long, Tuple<long, long>>(tmp_flag, tmp_stream, tmp_mul, tmp_size + j, tmp_pts, tmp_match, tmp_head_idx, new Tuple<long, long>(tmp_res, tmp_side)));
                    }
                }

                if ((FrameFlags)m_HeaderOptions[NutByte].Item1 != FrameFlags.Invalid) throw new InvalidOperationException("Invalid Header Tables");

                //The number of distinct non empty elision headers.
                //MUST be <128.
                m_EllisionHeaderCount = Math.Min(DecodeVariableLength(stream, out bytesRead), MultiByteLength);

                if (m_EllisionHeaderCount >= MultiByteLength) throw new InvalidOperationException("Invalid Header Tables");

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
                
                //The first Ellision Header must be 0
                m_EllisionHeaders = new List<byte[]>((int)m_EllisionHeaderCount + 1) { Utility.Empty };

                long position = stream.Position;

                //Read Ellision Headers (Note bounded by length of header)
                for (int h = 0; h < m_EllisionHeaderCount && position < end; ++h)
                {
                    long headerLength = DecodeVariableLength(stream, out bytesRead);

                    position += bytesRead;

                    //FROM FFMPEG Should ensure every value is > 0 && < 256
                    if (headerLength < 0 || headerLength > MaximumHeaderOptions) throw new InvalidOperationException("headerLength Must be > 0 && < 256, found: " + headerLength);

                    //Get the header data
                    byte[] headerData = new byte[headerLength];

                    stream.Read(headerData, 0, (int)headerLength);

                    //Add the header
                    m_EllisionHeaders.Add(headerData);

                    position += headerLength;
                }

                //Sanity
                if (m_EllisionHeaderCount > 0 && m_EllisionHeaders.Sum(h=> h.Length) > MaximumEllisionTotal)  throw new InvalidOperationException("Invalid Ellision Header Summation");

                // flags had been effectively introduced in version 4.
                // I have also allowed that if there is 4 more bytes then this is read from what is possibly reserved
                if (HasMainHeaderFlags && end - position > 4)
                {
                    //Usually has a BROADCAST flag?
                    m_MainHeaderFlags = DecodeVariableLength(stream, out bytesRead);
                }
                else m_MainHeaderFlags = (long)HeaderFlags.Unknown;

                //reserved_bytes can be ignored.
            }
        }

        /// <summary>
        /// Reads the next Tag or Frame.
        /// If <see cref="IsFrame"/> is true then <see cref="Container.Node.LengthSize"/> will be negitive to indicate it's variable length from <see cref="Container.Node.Position"/>
        /// </summary>
        /// <returns>The <see cref="Container.Node"/> found</returns>
        public Node ReadNext()
        {
            //long offset = Position;

            byte nextByte = (byte)ReadByte();

            if (nextByte == NutByte)
            {
                byte[] identifier = new byte[] { NutByte, 0, 0, 0, 0, 0, 0, 0 };

                Read(identifier, 1, IdentifierBytesSize);

                int lengthSize = 0;

                long length = DecodeVariableLength(this, out lengthSize);

                return new Node(this, identifier, lengthSize, Position, length, length <= Remaining);
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

                long streamId = HeaderOptions[nextByte].Item2;

                long reserved_count = HeaderOptions[nextByte].Rest.Item1;

                int header_idx = (int)HeaderOptions[nextByte].Item7;

                long size_mul = HeaderOptions[nextByte].Item3;

                long size_msb = 0, size_lsb = HeaderOptions[nextByte].Item4, 
                     // Size in bytes at the end inside data which represent frame sidedata and frame metadata.
                     sidedata_size = HeaderOptions[nextByte].Rest.Item2;

                int bytesReadTotal = 0, bytesReadNow;

                long length = size_lsb, temp;

                //Check to see if the real flags are in the data
                if (frameFlags.HasFlag(FrameFlags.Coded))
                {
                    frameFlags ^= (FrameFlags)(DecodeVariableLength(this, out bytesReadNow));
                    bytesReadTotal += bytesReadNow;
                }

                //Check for invalid flag
                if (frameFlags.HasFlag(FrameFlags.Invalid)) throw new InvalidOperationException("FrameCodes must not have the flag \"FrameFlags.Invalid\"");

                //Check for StreamId
                if (frameFlags.HasFlag(FrameFlags.StreamId))
                {
                    streamId = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                    if (streamId > MaximumHeaderOptions) throw new InvalidOperationException("StreamId cannot be > 256"); 
                }

                //Check for the PTS
                //Should probably be stored in Identifier.
                if (frameFlags.HasFlag(FrameFlags.CodedPTS))
                {
                    temp = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                }
                else
                {
                    //Todo
                    //Decode PTS
                }

                //Check to see if the size is coded in the data
                if (frameFlags.HasFlag(FrameFlags.SizeMSB))
                {
                    size_msb = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                }

                if (frameFlags.HasFlag(FrameFlags.MatchTime))
                {
                    temp = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                }

                //Check for alternate header index
                if (frameFlags.HasFlag(FrameFlags.HeaderIndex))
                {
                    header_idx = (int)DecodeVariableLength(this, out bytesReadNow);
                    if (header_idx > m_HeaderOptions.Count()) throw new InvalidOperationException("Invalid header index found: '" +  header_idx + "'. Cannot indicate a header which does not exist");
                    bytesReadTotal += bytesReadNow;
                }

                //7  FLAG_RESERVED    If set, reserved_count is coded in the frame header.
                //reserved_count[frame_code] (v)
                //MUST be <256.
                if (frameFlags.HasFlag(FrameFlags.Reserved))
                {
                    reserved_count = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                }

                //Could slightly optomize this with a version but due to draft status its easier to do a single check to find out if there are any bytes 'extra'.

                //Get an indication of how many bytes remain after the reserved bytes would be read.
                long reservedToRead = reserved_count - sidedata_size;

                //Check for additional fields only for Draft Compatibility (20130327)
                if (reservedToRead > 0 && frameFlags.HasFlag(FrameFlags.SideMetaData))
                {
                    //Optionally side data size can be specified here, this was not required because the structure contains the sidedata_size implicitly
                    //1 - Because it is structured as a Info packet
                    //2 - Because in the latest version there is also meta data right after
                    /*
                    +    if(frame_flags&FLAG_SIDEDATA)
                    +        sidedata_size                   v
                    +    for(i=0; i<frame_res - !(frame_flags&FLAG_SIDEDATA); i++)
                    */
                    sidedata_size = DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                }

                //Read any reserved data
                while (reservedToRead > 0)
                {
                    DecodeVariableLength(this, out bytesReadNow);
                    bytesReadTotal += bytesReadNow;
                    reservedToRead -= bytesReadNow;
                }

                //from MainHeader
                length = size_msb * size_mul + size_lsb;

                //Frames with a final size of les than ForwardPointerWithChecksum (4096) will have the header data preprended.
                //thus TotalLength cannot include EllisionHeaders[header_idx] length
                if (length > ForwardPointerWithChecksum) header_idx = 0;
                else length -= EllisionHeaders[header_idx].Length;

                /*
               EOR frames MUST be zero-length and must be set keyframe.
               All streams SHOULD end with EOR, where the pts of the EOR indicates the (NOT AN EXCEPTION CASE)
               end presentation time of the final frame.
               An EOR set stream is unset by the first content frame.
               EOR can only be unset in streams with zero decode_delay .
               FLAG_CHECKSUM MUST be set if the frame's data_size is strictly greater than
               2*max_distance or the difference abs(pts-last_pts) is strictly greater than
               max_pts_distance (where pts represents this frame's pts and last_pts is defined as below).
                */                

                //Ensure Key Frame for Eor and that Length is positive
                if (frameFlags.HasFlag(FrameFlags.EOR))
                {
                    if (!frameFlags.HasFlag(FrameFlags.Key)) throw new InvalidOperationException("EOR Frames must be key");

                    if (length != 0) throw new InvalidOperationException("EOR Frames must have size 0");
                }
                
                //Check for Checksum flag only because if it is present length is not checked.
                if (frameFlags.HasFlag(FrameFlags.Checksum))
                {
                    //Checksum

                    //Position += 4;

                    //Do this so the Frame can be optionall CRC'd by the Enumerator if CheckCRC is true
                    //length += 4;
                }
                else if (!(HasMainHeaderFlags && !MainHeaderFlags.HasFlag(HeaderFlags.Pipe))
                    && 
                    length > (2 * MaximumDistance)) throw new InvalidOperationException("frame size > 2 max_distance and no checksum");               

                //Can store 3 more bytes in identifier
                //LengthSize is negitive which indicates its variable length from Position
                return new Node(this, new byte[] { 0, 0, 0, 0, (byte)sidedata_size, (byte)header_idx, (byte)streamId, nextByte }, bytesReadTotal - IdentifierSize, Position, length, length <= Remaining);
            }
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Node result = ReadNext();

                if (result == null) yield break;

                yield return result;                

                Skip(result.DataSize);               
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
                m_StreamPackages.Add((int)DecodeVariableLength(tag.DataStream, out bytesRead), tag);

            //if (StreamCount != m_StreamPackages.Count) throw new InvalidOperationException("StreamCount does not match Packages with a Stream StartCode.");
        }

        //void ParseInfoPackage / SideMetaData (Optional)

        //void ParseSyncPoint

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

                using (var stream = streamPackage.DataStream)
                {
                    int bytesRead = 0;

                    long tempStreamId = DecodeVariableLength(stream, out bytesRead);

                    if (tempStreamId != streamId) throw new InvalidOperationException("Stream Package Mismatch");

                    long streamClass = DecodeVariableLength(stream, out bytesRead);

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
                                stream.Read(codecIndication, 0, (int)DecodeVariableLength(stream, out bytesRead));
                                break;
                            }
                    }
                    
                    //timeBaseId
                    long tempTimeBase = DecodeVariableLength(stream, out bytesRead);

                    //if (tempTimeBase != timeBase) throw new InvalidOperationException("Stream Timebase Mismatch");

                    if (tempTimeBase != timeBase) timeBase = m_TimeBases[(int)tempTimeBase];

                    long msb_pts_shift = DecodeVariableLength(stream, out bytesRead);
                    long pts_distance = DecodeVariableLength(stream, out bytesRead);
                    long decode_delay = DecodeVariableLength(stream, out bytesRead);
                    long stream_flags = DecodeVariableLength(stream, out bytesRead);

                    long duration = msb_pts_shift * pts_distance;

                    duration += (int)m_HeaderOptions[streamId].Item6 << (int)msb_pts_shift;

                    //extraData for codec
                    stream.Position += DecodeVariableLength(stream, out bytesRead) + bytesRead;

                    int width = 0, height = 0;

                    double rate = 0;

                    byte channels = 0, bitDepth = 0;

                    switch (mediaType)
                    {
                        case Sdp.MediaType.video:
                            {
                                //Already read
                                rate = pts_distance;

                                width = (int)DecodeVariableLength(stream, out bytesRead);
                                height = (int)DecodeVariableLength(stream, out bytesRead);
                                //Aspect Ration (num/dum)

                                DecodeVariableLength(stream, out bytesRead);
                                DecodeVariableLength(stream, out bytesRead);

                                //csp type
                                bitDepth = (byte)DecodeVariableLength(stream, out bytesRead);
                                break;
                            }
                        case Sdp.MediaType.audio:
                            {
                                //Already read
                                duration = pts_distance;

                                //Rational
                                rate = DecodeVariableLength(stream, out bytesRead);
                                rate /= DecodeVariableLength(stream, out bytesRead);
                                channels = (byte)DecodeVariableLength(stream, out bytesRead);
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
            //Get all frames.

            //Determine if store offset of last syncpoint...

            //Determine if discard frame.

            throw new NotImplementedException();
        }

        public override Node Root
        {
            get { return ReadTag(StartCode.Main, FileIdString.Length); }
        }

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return NutReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
        }

        public override Node TableOfContents
        {
            get { using (var root = Root) return ReadTag(StartCode.Index, root.DataOffset + root.DataSize); }
        }
    }
}
