﻿/*
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

namespace Media.Containers.Ogg
{
    /// <summary>
    /// Provides an implementation of the Ogg Container defined by Xiph.org and <see cref="http://www.ietf.org/rfc/rfc3533.txt">RFC3533</see>.
    /// The implementation is compatible with ogg, oga, ogv, ogx and ogm files and others including spx and opus.
    /// </summary>
    public class OggReader : MediaFileStream
    {

        #region Constants

        static byte[] VorbisBytes = System.Text.Encoding.UTF8.GetBytes("vorbis");

        const int MaximumPageSize = 65307, IdentifierSize = 8, MinimumSize = 20 + IdentifierSize, MinimumReadSize = MinimumSize - 1, PageVersionOffset = 4, PageSegmentCountOffset = 26;

        //OggDS
        const int PackTypeHeader = 0x01,
            PacketTypeComment = 0x03,
            PacketTypeCodeBook = 0x05,
            PacketTypeBits = 0x07,
            PacketIsSyncPoint = 0x08,
            PacketLengthBits = 0x0c0,
            PacketLengthBits2 = 0x02; 

        #endregion

        /// <summary>
        /// Used to identify the first 4 - 8 bytes of a page.
        /// </summary>
        public enum CapturePattern : long
        {
            Unknown = 0,
            Oggs = 1399285583,
            fishead = 28254585843050854,
            fisbone = 28550397319932262,
            index = 517097156201
        }

        [Flags]
        public enum HeaderType : byte
        {
            None = 0,
            //set: page contains data of a packet continued from the previous page
            //unset: page contains a fresh packet
            Continuation = 1,
            // set: this is the first page of a logical bitstream (bos)
            //unset: this page is not a first page
            FirstPage = 2,
            //set: this is the last page of a logical bitstream (eos)
            //unset: this page is not a last page
            LastPage = 4
        }

        #region Statics

        public static HeaderType GetHeaderType(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");
            return (HeaderType)node.Identifier[5];
        }

        public static long GetGranulePosition(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return Common.Binary.Read64(node.Identifier, 6, !BitConverter.IsLittleEndian);
        }

        public static int GetSerialNumber(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return (int)Common.Binary.ReadU32(node.Identifier, 14, !BitConverter.IsLittleEndian);
        }

        public static int GetSequenceNumber(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return (int)Common.Binary.ReadU32(node.Identifier, 18, !BitConverter.IsLittleEndian);
        }

        public static int GetCrc(Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            return (int)Common.Binary.ReadU32(node.Identifier, 22, !BitConverter.IsLittleEndian);
        }

        #endregion

        public OggReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public OggReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public OggReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public override string ToTextualConvention(Node page)
        {
            if (page.Master.Equals(this))
            {
                //Get the capture pattern from the identifier which should equal OggSxxxx
                CapturePattern result = (CapturePattern)Common.Binary.ReadU64(page.Identifier, 0, !BitConverter.IsLittleEndian);
                
                //If there is no data then just unmask the capture pattern by limiting it to 4 bytes
                if (page.DataSize <= 0) return ((CapturePattern)((ulong)result & uint.MaxValue)).ToString();
                
                //Read the capture pattern from the data
                result = (CapturePattern)Common.Binary.ReadU64(page.Data, 0, !BitConverter.IsLittleEndian);
                
                //Determine if this is a known capture pattern.
                switch (result)
                {
                    case CapturePattern.fishead:
                    case CapturePattern.fisbone:
                    case CapturePattern.index: return result.ToString();
                    //Was already Oggs, just return that
                    default: return CapturePattern.Oggs.ToString();
                } 
            }

            return base.ToTextualConvention(page);
        }

        public IEnumerable<Node> ReadPages(long offset, long count, params CapturePattern[] names) { return ReadPages(offset, count, null, names); }

        public IEnumerable<Node> ReadPages(long offset, long count, HeaderType? headerFlags, params CapturePattern[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var page in this)
            {
                CapturePattern found = (CapturePattern)Common.Binary.ReadU64(page.Identifier, 0, !BitConverter.IsLittleEndian);

                if (headerFlags.HasValue && GetHeaderType(page) != headerFlags.Value) continue;

                if (names == null || names.Count() == 0 || names.Contains(found)) yield return page;
                else if( page.DataSize > 0)
                {
                    //Get the capture pattern
                    found = (CapturePattern)Common.Binary.ReadU64(page.Data, 0, !BitConverter.IsLittleEndian);
                    
                    //See if the given list contained it
                    if(names.Contains(found)) yield return page;
                }

                //Could check raw for identifier

                count -= page.TotalSize;

                if (count <= 0) break;

                continue;
            }

            Position = position;
        }

        public Node ReadPage(CapturePattern identifier, HeaderType? headerFlags = null, long position = 0)
        {
            long positionStart = Position;

            Node result = ReadPages(position, Length - position, headerFlags, identifier).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        public Node ReadNext()
        {
            long offset = Position, length = 0;

            byte[] identifier = new byte[MinimumReadSize];

            Read(identifier, 0, MinimumReadSize);

            //Take a 8 bytes and convert to an ID( & uint.MaxValue to get OggS)
            //is this worth it?
            //CapturePattern found = (CapturePattern)(Common.Binary.ReadU64(identifier, 0, !BitConverter.IsLittleEndian));

            //Check version
            if (identifier[PageVersionOffset] > 0) throw new InvalidOperationException("Only Version 0 is Defined.");

            byte pageSegmentCount = identifier[PageSegmentCountOffset];

            if (pageSegmentCount < 1 || Remaining < pageSegmentCount) throw new InvalidOperationException("Invalid Header Page");

            int lengthSize = 0;

            // (segment_table) @ 27 - number_page_segments Bytes containing the lacing
            //values of all segments in this page.  Each Byte contains one
            //lacing value.

            //Read a byte at a time to determine the length
            //Could also verify CRC as reading
            while (pageSegmentCount-- > 0)
            {
                length += ReadByte();
                ++lengthSize;
            }

            return new Node(this, identifier, lengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining >= MinimumReadSize)
            {
                Node next = ReadNext();

                if(next == null) yield break;

                yield return next;
                
                //Have a CheckCRC 

                //if true then crc check

                Skip(next.DataSize);
            }
        }

        List<Track> m_Tracks;

        Dictionary<int, Node> m_PageBegins, m_PageEnds;

        void ParsePages()
        {

            if (m_PageBegins != null || m_PageEnds != null) return;

            m_PageBegins = new Dictionary<int,Node>();

            m_PageEnds = new Dictionary<int, Node>();

            long position = Position;

            using (var root = Root) Position = root.Offset;

            //Iterate all pages
            foreach (Node page in this)
            {
                //Last Page or Index 
                if (page.DataSize > 0)
                {
                    //Ensure not a skeleton or index

                    //Decode the CapturePattern from the data
                    CapturePattern pattern = (CapturePattern)(Common.Binary.ReadU64(page.Data, 0, !BitConverter.IsLittleEndian));

                    //Determine what to do
                    switch (pattern)
                    {
                        case CapturePattern.fisbone:
                        case CapturePattern.fishead:
                        case CapturePattern.index:
                            continue;
                        default: break;
                    }

                    //Get the pageHeaderType
                    HeaderType pageHeaderType = GetHeaderType(page);

                    //Read Serial
                    int serial = GetSerialNumber(page);

                    if ((pageHeaderType == HeaderType.None || pageHeaderType.HasFlag(HeaderType.FirstPage)) && !m_PageBegins.ContainsKey(serial))
                    {
                        //Add the page
                        m_PageBegins.Add(serial, page);
                    }

                    if (pageHeaderType.HasFlag(HeaderType.LastPage) && !m_PageEnds.ContainsKey(serial))
                    {
                        //Found last page for stream

                        //Add the page
                        m_PageEnds.Add(serial, page);
                    }
                }
            }

            Position = position;
        }

        public override IEnumerable<Track> GetTracks()
        {

            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }

            ParsePages();

            List<Track> tracks = new List<Track>();

            foreach (var streamBegin in m_PageBegins)
            {

                //use serialNumber?
                int serialNumber = streamBegin.Key, sampleCount = 0, height = 0, width = 0;

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte[] codecIndication = Utility.Empty;

                double rate = 0, duration = 0;

                byte channels = default(byte), bitDepth = default(byte);

                //The startPage
                Node startPage = streamBegin.Value;

                Node endPage;

                //If no Page End then treat stream as disabled
                if (!m_PageEnds.TryGetValue(serialNumber, out endPage)) endPage = startPage;

                //How about a Generic 'XCODECMAP' mapping?
                //It would make all mappings easier to decode
                //4cc, width, height, colorspace, bitsperplane, rate
                //formatId, channels, sampleRate

                /*http://www.ietf.org/rfc/rfc5334.txt
                                      Codec Identifier             | Codecs Parameter
                       -----------------------------------------------------------
                        char[5]: 'BBCD\0'            | dirac
                        char[5]: '\177FLAC'          | flac
                        char[7]: '\x80theora'        | theora
                        char[7]: '\x01vorbis'        | vorbis
                        char[8]: 'CELT    '          | celt
                        char[8]: 'CMML\0\0\0\0'      | cmml
                        char[8]: '\213JNG\r\n\032\n' | jng
                        char[8]: '\x80kate\0\0\0'    | kate
                        char[8]: 'OggMIDI\0'         | midi
                        char[8]: '\212MNG\r\n\032\n' | mng
                        char[8]: 'PCM     '          | pcm
                        char[8]: '\211PNG\r\n\032\n' | png
                        char[8]: 'Speex   '          | speex
                        char[8]: 'YUV4MPEG'          | yuv4mpeg
                     * OGM (Avi Stream Header follows) with 56 bytes
                        char[8]: '\1video\0\0'       | video
                        char[8]: '\1text\0\0\0\'     | text
                        char[8]: '\1sound\0\0'       | audio
                        char[8]: '\1audio\0\0'       | audio
                     */

                //OggDS uses Data[0] to determine the PacketType.
                //This is because Ogg didn't have an Info or Meta Packet Type at the time.

                //Subsequently Ogg also used Data[0] -> 7 to indicate a specific codec.

                //Meta is a Vorbis Comment.

                //There is also the 'Direct Show Samples embedded in Ogg' variation for OGM....

                //Determine what to do based on data in the page
                byte firstByte = startPage.Data[0];
                switch (firstByte)
                {
                    //https://wiki.xiph.org/OggText
                    //Note where is Text iden handling?
                    case 1: //vorbis (or OGM) Header
                        {
                            byte identifyingByte = startPage.Data[4];

                            //if identifyingByte has PacketTypeBits then this is OGM.

                            //Could also check + 96
                            //0x05589f80 - VideoInfo
                            //0x05589f81 - WaveFormatEx

                            switch (identifyingByte)
                            {
                                case (byte)'e': //VIDeO or `DirEct Show Samples embedded in Ogg` (35)
                                    {
                                        //Parse Either AVI BitmapInfoHeader or AVI VideoInfoHeader
                                        mediaType = Sdp.MediaType.video;

                                        //BitmapInfoHeader
                                        //Read 32 Width
                                        width = (int)Common.Binary.ReadU32(startPage.Data, 176, !BitConverter.IsLittleEndian);

                                        //Read 32 Height
                                        height = (int)Common.Binary.ReadU32(startPage.Data, 180, !BitConverter.IsLittleEndian);

                                        //Maybe...
                                        //Read 16 panes 

                                        //Read 16 BitDepth
                                        bitDepth = (byte)(int)Common.Binary.ReadU16(startPage.Data, 182, !BitConverter.IsLittleEndian);

                                        //Read codec
                                        codecIndication = startPage.Data.Skip(68).Take(4).ToArray();

                                        break;
                                    }
                                case (byte)'n': //SOUnD
                                case (byte)'i': //AUDiO
                                    {
                                        //Parse Either AVI WaveFormat or AVI WaveFormatEx
                                        mediaType = Sdp.MediaType.audio;

                                        //CodecIndication
                                        //*(ogg_int16_t*)(p+124),  // formattag
                                        codecIndication = startPage.Data.Skip(124).Take(2).ToArray();
                                        
                                        //WaveFormat (EX) 
                                        //*(ogg_int16_t*)(p+126),  // channels
                                        channels = (byte)Common.Binary.ReadU16(startPage.Data, 126, !BitConverter.IsLittleEndian);
                                        //*(ogg_int16_t*)(p+136),  // blockalign
                                        //*(ogg_int32_t*)(p+132),  // avgbytespersec
                                        //*(ogg_int32_t*)(p+128),  // samplespersec
                                        //*(ogg_int16_t*)(p+138),  // bitspersample
                                        bitDepth = (byte)Common.Binary.ReadU16(startPage.Data, 138, !BitConverter.IsLittleEndian);
                                        //p+142,				   // extradata
                                        //*(ogg_int16_t*)(p+140),  // extrasize
                                        //*(ogg_int32_t*)(p+40));  // buffersize

                                        break;
                                    }
                                case (byte)'t': //TEXt
                                    {
                                        //Handle Subtitle
                                        mediaType = Sdp.MediaType.text;
                                        break;
                                    }
                                default: //Vorbis
                                    {
                                        mediaType = Sdp.MediaType.audio;
                                        codecIndication = startPage.Data.Take(7).ToArray();

                                        //Version 4 bytes

                                        channels = startPage.Data[11];

                                        rate = Common.Binary.Read32(startPage.Data, 12, !BitConverter.IsLittleEndian);

                                        /* http://www.xiph.org/vorbis/doc/Vorbis_I_spec.html
                                            All three fields set to the same value implies a fixed rate, or tightly bounded, nearly fixed-rate bitstream
                                            Only nominal set implies a VBR or ABR stream that averages the nominal bitrate
                                            Maximum and or minimum set implies a VBR bitstream that obeys the bitrate limits
                                            None set indicates the encoder does not care to speculate.
                                            */

                                        //bitrate upper 4

                                        //should check if > 0

                                        //bitrate nominal 4 (Note that 8000 comes from 8 * 1000 and is probably an incorrect calulcation)
                                        bitDepth = (byte)(Common.Binary.ReadU32(startPage.Data, 20, !BitConverter.IsLittleEndian) / 8000);

                                        //should check if > 0

                                        //bitrate lower 4

                                        break;
                                    }
                            }

                            break;
                        }
                    case 3:// Vorbis Comment
                        {
                            //Check for vorbis
                            string detect = System.Text.Encoding.UTF8.GetString(startPage.Data, 11, 15);

                            switch (detect)
                            {
                                //Shitty ElphelOgm Has no BeginOfPage also has no Info Header.
                                case "ElphelOgm v 0.1":
                                    {
                                        mediaType = Sdp.MediaType.video;
                                        codecIndication = System.Text.Encoding.UTF8.GetBytes("MJPG");
                                        rate = 30;
                                        //Other details come from the JPEG Stream itself.
                                        break;
                                    }
                                //Another Hall of Shamer with no BeginOfPage and no Info Header
                                case "Xiphophorus lib":
                                    {
                                        mediaType = Sdp.MediaType.video;
                                        codecIndication = Utility.Empty;
                                        rate = 30;
                                        break;
                                    }
                                default: //Unknown Type
                                    {
                                        mediaType = Sdp.MediaType.unknown;
                                        rate = 0;
                                        codecIndication = Utility.Empty;
                                        break;
                                    }
                            }
                            break;
                        }
                    //case 5: continue;// Vorbis Codebook
                    case 0x80:
                        {
                            if (startPage.Data[1] == 'k') //kate
                            {
                                mediaType = Sdp.MediaType.text;
                                goto default;
                            }
                            else //theora
                            {
                                mediaType = Sdp.MediaType.video;
                                codecIndication = startPage.Data.Take(7).ToArray();

                                //Theora Mapping

                                //Resolution
                                //width = Common.Binary.Read16(page.Raw, 10, BitConverter.IsLittleEndian) << 4,
                                //height = Common.Binary.Read16(page.Raw, 12, BitConverter.IsLittleEndian) << 4;

                                //Display Resolution
                                width = (int)Common.Binary.ReadU24(startPage.Data, 14, BitConverter.IsLittleEndian);
                                height = (int)Common.Binary.ReadU24(startPage.Data, 17, BitConverter.IsLittleEndian);

                                //Offset
                                //X page.Raw[20]
                                //Y page.Raw[21]

                                //Frames Per Seconds 4 byte fps_numerator, 4 byte fps_denominator          
                                rate = Common.Binary.Read64(startPage.Data, 22, BitConverter.IsLittleEndian);
                                rate /= Common.Binary.Read64(startPage.Data, 26, BitConverter.IsLittleEndian);

                                //4 byte aspect_numerator        
                                //4 byte aspect_denominator                              
                                //double aspectRation = Common.Binary.Read64(page.Raw, 28, BitConverter.IsLittleEndian);

                                //Color Space
                                //bitDepth = page.Raw[36];

                                //Enumerated as

                                //0 Undefined
                                //1 Rec. 470M (see Section 4.3.1). Rec. 470M (Rec. ITU-R BT.470-6 System M/NTSC with Rec. ITU-R BT.601-5)

                                //2 Rec. 470BG (see Section 4.3.2).Rec. 470BG (Rec. ITU-R BT.470-6 Systems B and G with Rec. ITU-R BT.601-5)

                                //3 Reserved.
                                //... 255

                                //3 byte target_BitRate

                                //Qual (5 bit)

                                //KFGShift,(6 bit) PF (2 bit), resv (2 bit)

                                //Pf is PixelFormat 2 bit field
                                //0 4:2:0 (see Section 4.4.3).
                                //1 Reserved.
                                //2 4:2:2 (see Section 4.4.2).
                                //3 4:4:4 (see Section 4.4.1).

                                //calculate BitDept from above?
                                //if(bitDepth == 0) bitDepth = 8;
                                //else bitDepth *= 8; //16, 24
                            }
                            break;
                        }
                    case 177: //FLAC
                        {
                            mediaType = Sdp.MediaType.audio;
                            codecIndication = startPage.Data.Take(5).ToArray();

                            //define OGG_FLAC_METADATA_TYPE_STREAMINFO 0x7F
                            if ((startPage.Data[6] & 1) == 0x7f)
                            {
                                //StreamInfoStart
                                //FLAG (4 bytes)
                                //MajorVersion (1 byte)
                                //MinorVersion, (1 byte)
                                //HeaderCount (2 bytes)
                                //fLaC
                                //MetadataBlock header
                            }

                            break;
                        }
                    case (byte)'B': //Dirac
                        {
                            mediaType = Sdp.MediaType.video;
                            codecIndication = startPage.Data.Take(5).ToArray();

                            //Dirac Mapping
                            //http://diracvideo.org/download/mapping-specs/dirac-mapping-ogg-1.0.pdf

                            break;
                        }
                    case (byte)'C': //CXXX
                        {
                            if (startPage.Data[1] == 'M') //CMML
                            {
                                mediaType = Sdp.MediaType.text;
                                codecIndication = startPage.Data.Take(5).ToArray();
                            }
                            else //CELT
                            {
                                mediaType = Sdp.MediaType.audio;

                                rate = Common.Binary.Read32(startPage.Data, 36, !BitConverter.IsLittleEndian);

                                channels = (byte)Common.Binary.ReadU32(startPage.Data, 40, !BitConverter.IsLittleEndian);

                                //frame_size

                                //overlap

                                //bytes per packet

                                //extra headers

                                //caulate BitDepth?

                                goto default;
                            }
                            break;
                        }
                    case (byte)'O': //OggMIDI
                        {
                            /*
                             0                   1                   2                   3
                             0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1| Byte
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            | Identifier 'OggMIDI\0'                                        | 0-3
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |                                                               | 4-7
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |        version number         |         time format           | 8-11
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |           timebase            |                                 12-13
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                             * 
                             * The header packet begins with the character sequence 'OggMIDI\0' for codec identification.
                            This is followed by an 8 bit version field. The version must be 0 for the mapping described here.
                            The time format field is also 8 bits, and describes the timestamp format used by the midi data. It is interpreted as follows:
                            1 one beat per quarter note
                            24, 25, 29, 30, (50, 59, 60?) corresponding smpte frame
                            other values are undefined
                            The timebase occupies the final 16 bits and gives the number of ticks per time format unit.
                             */
                            mediaType = Sdp.MediaType.audio;
                            goto default;
                        }
                    case (byte)'P': //PCM
                        {
                            mediaType = Sdp.MediaType.audio;

                            //PCM Format
                            codecIndication = startPage.Data.Skip(4).Take(4).ToArray();

                            //Sample rate
                            rate = Common.Binary.ReadU32(startPage.Data, 12, BitConverter.IsLittleEndian);

                            //Number of significant bits
                            bitDepth = startPage.Data[15];

                            //Number of Channels (< 256)
                            channels = startPage.Data[16];

                            //16  [uint] Maximum number of frames per packet
                            //32  [uint] Number of extra header packets

                            //DO NOT obtain codecIndication again
                            continue;
                        }
                    case (byte)'S': //Speex
                        {
                            if (startPage.Data[2] == 'e')
                            {
                                //https://wiki.xiph.org/OggSpeex

                                /*
                                  0                   1                   2                   3
                                  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1| Byte
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | speex_string: Identifier char[8]: 'Speex   '                  | 0-3
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 |                                                               | 4-7
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | speex_version: char[20]                                       | 8-11
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 |                                                               | 12-15
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 |                                                               | 16-19
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 |                                                               | 20-23
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 |                                                               | 24-27
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | speex_version_id                                              | 28-31
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | header_size                                                   | 32-35
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | rate                                                          | 36-39
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | mode                                                          | 40-43
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | mode_bitstream_version                                        | 44-47
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | nb_channels                                                   | 48-51
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | bitrate                                                       | 52-55
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | frame_size                                                    | 56-59
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | vbr                                                           | 60-63
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | frames_per_packet                                             | 64-67
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | extra_headers                                                 | 68-71
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | reserved1                                                     | 72-75
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 | reserved2                                                     | 76-79
                                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                 */

                                mediaType = Sdp.MediaType.audio;

                                //rate
                                rate = Common.Binary.ReadU32(startPage.Data, 36, BitConverter.IsLittleEndian);

                                //channels
                                channels = (byte)Common.Binary.ReadU32(startPage.Data, 48, BitConverter.IsLittleEndian);

                                //bitrate
                                bitDepth = (byte)Common.Binary.ReadU32(startPage.Data, 52, BitConverter.IsLittleEndian);
                            }
                            else //SPOTS
                            {
                                mediaType = Sdp.MediaType.video;


                                //https://wiki.xiph.org/OggSpots

                                /*
                                  0                   1                   2                   3
                                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1| Byte
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Identifier 'SPOTS\0\0\0'                                      | 0-3
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                |                                                               | 4-7
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Version major                 | Version minor                 | 8-11
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Granulerate numerator                                         | 12-15
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                |                                                               | 16-19
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Granulerate denominator                                       | 20-23
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                |                                                               | 24-27
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Granuleshift  | RESERVED FOR LATER USE                        | 28-31
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Image-Format                                                  | 32-35
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                |                                                               | 36-39
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Display width                 | Display height                | 40-43
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | BG-Color                                                      | 44-47
                                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                | Align-Horiz   | Align-Vert    | Options                       | 48-51
                                 */

                                codecIndication = startPage.Data.Skip(32).Take(8).ToArray();

                                width = Common.Binary.Read16(startPage.Data, 40, !BitConverter.IsLittleEndian);

                                height = Common.Binary.Read16(startPage.Data, 42, !BitConverter.IsLittleEndian);

                                //Attain additional info from data within?
                            }

                            //Do not obtain codecIndication
                            continue;
                        }
                    case (byte)'T': //Tracking
                        {

                            //TRCK \0\0\0\0

                            //https://wiki.xiph.org/Tracking

                            mediaType = Sdp.MediaType.text;

                            goto default;
                        }
                    case (byte)'U': //UVS 
                        {
                            //https://wiki.xiph.org/OggUVS

                            /*
                             32  "UVS " Codec Identifier Word 0
                            32  "    "  Codec Identifier Word 1
                            16  [uint]  Version Major (breaks backwards compatability to increment)
                            16  [uint]  Version Minor (backwards compatable, ie, more supported format id's)
                            16  [uint]  Display Width
                            16  [uint]  Display Height
                            16  [uint]  Pixel Aspect Ratio Numerator
                            16  [uint]  Pixel Aspect Ratio Denominator
                            16  [uint]  Field Rate Numerator
                            16  [uint]  Field Rate Denominator
                            32  [uint]  Timebase (hz)
                            32  [uint]  Field Image Size (in bytes)
                            32  [uint]  Number of extra headers
                            32  [enum]  Colorspace ( 1 = RGB, 2 = YCbCr)
                            31  [uint]  Reserved
                                1  [flg]   Interlaced
                            32  [enum]  Layout ID
                              * 0x32315659   OGGUVS_FMT_YV12      8-bpp Y plane, followed by 8-bpp 2×2 V and U planes.
                              * 0x56555949   OGGUVS_FMT_IYUV      8-bpp Y plane, followed by 8-bpp 2×2 U and V planes.
                              * 0x32595559   OGGUVS_FMT_YUY2      UV downsampled 2:1 horizontally, ordered Y0 U0 Y1 V0
                              * 0x59565955   OGGUVS_FMT_UYVY      UV downsampled 2:1 horizontally, ordered U0 Y0 V0 Y1
                              * 0x55595659   OGGUVS_FMT_YVYU      UV downsampled 2:1 horizontally, ordered Y0 V0 Y1 U0
                              * 0x80808081   OGGUVS_FMT_RGB24DIB  8 bits per component, stored BGR, rows aligned to a
                                                                    32 bit boundary, rows stored bottom first.
                              * 0x80808082   OGGUVS_FMT_RGB32DIB  8 bits per component, stored BGRx (x is don't care)
                                                                    rows stored bottom first.
                              * 0x80808083   OGGUVS_FMT_ARGBDIB   8 bits per component, stored BGRA, rows stored bottom
                             */

                            width = Common.Binary.Read16(startPage.Data, 12, !BitConverter.IsLittleEndian);
                            height = Common.Binary.Read16(startPage.Data, 14, !BitConverter.IsLittleEndian);
                            rate = Common.Binary.Read64(startPage.Data, 20, !BitConverter.IsLittleEndian);

                            //timebase @ 28

                            //Set BitDepth if possible
                            switch (Common.Binary.ReadU32(startPage.Data, 40, !BitConverter.IsLittleEndian))
                            {
                                case 0x80808081: //OGGUVS_FMT_RGB24DIB
                                case 0x80808082: //OGGUVS_FMT_RGB32DIB
                                case 0x80808083: //OGGUVS_FMT_ARGBDIB
                                case 0x32315659: //OGGUVS_FMT_YV12
                                case 0x56555949:
                                    bitDepth = 8;
                                    break;
                                case 0x32595559: //OGGUVS_FMT_YUY2
                                case 0x59565955: //OGGUVS_FMT_UYVY
                                case 0x55595659: //OGGUVS_FMT_YVYU
                                    break;
                            }

                            goto default;
                        }
                    case (byte)'Y': //YUV4MPEG 
                        {
                            //https://wiki.xiph.org/OggYUV4MPEG

                            //A YUV4MPEG file begins with a single text line which defined the stream parameters like image size, framerate, chroma subsampling and so on. For example:

                            //YUV4MPEG2 W352 H288 F30000:1001 Ip A128:117
                            goto default;
                        }
                    //http://en.wikipedia.org/wiki/JPEG_Network_Graphics
                    case 213: //JNG
                    case 212: //MNG https://wiki.xiph.org/OggMNG#Motivation
                    case 211: //PNG
                        {
                            mediaType = Sdp.MediaType.video;
                            codecIndication = startPage.Data.Take(8).ToArray();
                            break;
                        }
                    default: //Not sure, probably a new or undocumented mapping?
                        {
                            codecIndication = startPage.Data.Take(8).ToArray();
                            break;
                        }
                }

                //Gainule Position
                //A Special value of -1 indicates that no packets finish on this page.
                duration = Common.Binary.Read64(endPage.Identifier, 6, !BitConverter.IsLittleEndian);

                //Sequence Number
                sampleCount = Common.Binary.Read32(endPage.Identifier, 18, !BitConverter.IsLittleEndian);

                //Calulcate duration (Might be different for OGM, to handle that when OGM is encounted set a flag)
                switch (mediaType)
                {
                    case Sdp.MediaType.audio:
                        {
                            duration /= rate;
                            break;
                        }
                    case Sdp.MediaType.video:
                        {
                            //usually less
                            //duration = sampleCount / rate;

                            duration = Math.Max(sampleCount / rate, (duration / Utility.MicrosecondsPerMillisecond) - sampleCount / rate);

                            //Usually closer to actual time unless the above was greater
                            //duration /= 1000;
                            //duration -= sampleCount / rate;

                            break;
                        }
                }

                Track created = new Track(startPage, string.Empty,
                    //Serial Number
                      serialNumber,
                    //Created, Modified
                      FileInfo.CreationTimeUtc, FileInfo.LastWriteTimeUtc, 
                    //SampleCount
                      sampleCount,
                    //Width Height
                       width, height,
                    //Start Time (Gainule Position from startPage)
                      TimeSpan.FromMilliseconds(Common.Binary.Read64(startPage.Identifier, 6, !BitConverter.IsLittleEndian)), 
                    //Duration
                      TimeSpan.FromSeconds(duration),
                    //Framerate mediaType
                      rate, mediaType,
                    //Codec, channels bitDepth
                      codecIndication, channels, bitDepth);

                //Add track
                tracks.Add(created);

                //Yeild it
                yield return created;
            }

            m_Tracks = tracks;
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            /*
             OGM Data packets

            0x0000	Bit0	0 = Data packet 1
		            Bit1	Bit 2 of len bytes
		            Bit3	1 = This sample is a syncpoint
					            ("keyframe")
		            Bit4	Currently not used
		            Bit5	Currently not used
		            Bit6&7	Bit 0 and 1 of len bytes

		            len bytes is the number of following bytes
		            with sample length in media time
		
		            0 =	default len (from header packet)

             0x0001	Len bytes specified with bits  1 7 6,
		            LowByte ... HighByte
             ..
             0x000x Data

             */

            throw new NotImplementedException();
        }

        public override Node Root
        {
            get
            {
                long position = Position;
                var result = ReadPages(0, Length, CapturePattern.fishead, CapturePattern.Oggs).FirstOrDefault();
                Position = position;
                return result;
            }
        }

        public override Node TableOfContents
        {
            get
            {
                using (var root = Root)
                {
                    long position = Position;
                    var result = ReadPages(root.DataOffset + root.DataSize, Length - root.TotalSize, CapturePattern.fishead, CapturePattern.fisbone, CapturePattern.index).FirstOrDefault();
                    Position = position;
                    return result;
                }
            }
        }

        public DateTime Created { get { return FileInfo.CreationTimeUtc; } }

        public DateTime Modified { get { return FileInfo.LastWriteTimeUtc; } }
    }
}
