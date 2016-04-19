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

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// <see href="https://en.wikipedia.org/wiki/MPEG_program_stream">MPEG Program stream</see>
    /// Represents the logic necessary to read Mpeg Program Streams. (.VOB, .EVO, .ps, .psm .m2ts etc.)
    /// Program stream (PS or MPEG-PS) is a container format for multiplexing digital audio, video and more. The PS format is specified in MPEG-1 Part 1 (ISO/IEC 11172-1) and MPEG-2 Part 1, Systems (ISO/IEC standard 13818-1[6]/ITU-T H.222.0[4][5]). The MPEG-2 Program Stream is analogous and similar to ISO/IEC 11172 Systems layer and it is forward compatible.[7][8]
    /// ProgramStreams are created by combining one or more Packetized Elementary Streams (PES), which have a common time base, into a single stream.
    /// </summary>
    public class ProgramStreamReader : PacketizedElementaryStreamReader
    {

        //Header classes...

        public ProgramStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public ProgramStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public ProgramStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public ProgramStreamReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, bufferSize) { } 

        //Might need the data not the identifier
        public static string ToTextualConvention(byte[] identifier)
        {
            return PacketizedElementaryStreamReader.ToTextualConvention(identifier);
        }

        /// <summary>
        /// <see cref="Mpeg.StartCodes"/> for names.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public IEnumerable<Container.Node> FindNodes(long offset, long count, params byte[] names)
        {
            long position = Position;

            Position = offset;

            foreach (Container.Node node in this)
            {
                //If no names were specified or they were and the identifier marker was contained return the node
                if (names == null || names.Length == 0 || names.Contains(node.Identifier[3])) yield return node;
                
                //Decrease by TotalSize of the node
                count -= node.TotalSize;

                //if no more bytes are allow stop
                if (count <= 0) break;
            }

            //Reset the position
            Position = position;
        }

        /// <summary>
        /// <see cref="Mpeg.StartCodes"/> for names.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Container.Node FindNode(byte name, long offset, long count)
        {
            long position = Position;

            Container.Node result = FindNodes(offset, count, name).FirstOrDefault();

            Position = position;

            return result;
        }      

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Container.Node ReadNext()
        {
            //Read a code
            byte[] identifier = PacketizedElementaryStreamReader.ReadIdentifier(this);

            //Check for sync
            if (Common.Binary.ReadU24(identifier, 0, Media.Common.Binary.IsBigEndian) != Common.Binary.ReadU24(Mpeg.StartCodes.StartCodePrefix, 0, Media.Common.Binary.IsBigEndian)) throw new InvalidOperationException("Cannot Find StartCode Prefix.");

            int length = 0, lengthSize = PacketizedElementaryStreamReader.LengthSize;

            //Determine which type of node we are dealing with
            switch (identifier[3])
            {
                case Mpeg.StreamTypes.PackHeader:
                    {
                        //No bytes are related to the length yet
                        lengthSize = 0;

                        //MPEG 1 Used only 2 bits 0 1
                        //MPEG 2 Used 4 bits 0 0 0 1
                        byte next = (byte)ReadByte();

                        //We are at the 5th byte. (IdentifierSize + 1)
                        int offset = 5;

                        //Determine which version of the Pack Header this is
                        switch (next >> 6)
                        {
                            case 0: //MPEG 1
                                {
                                    //Read 7 more bytes (The rest of the MPEG 1 Pack Header)
                                    Array.Resize(ref identifier, 12);
                                    Read(identifier, offset, 7);

                                    break;
                                }
                            default: //MPEG 2
                                {
                                    //Read 9 more bytes (the rest of the MPEG 2 Pack Header)
                                    Array.Resize(ref identifier, 14);
                                    Read(identifier, offset, 9);

                                    //Include Stuffing length with mask (00000111) reversed bits
                                    length = (byte)(identifier[13] & 0x07);

                                    break;
                                }
                        }

                        //Put the 4th byte back
                        identifier[IdentifierSize] = next;

                        break;
                    }
                case Mpeg.StreamTypes.ProgramEnd:
                    {
                        //End of Program Stream.
                        break;
                    }
                default: //PESPacket
                    {
                        //lengthSize already set
                        length = PacketizedElementaryStreamReader.DecodeLength(PacketizedElementaryStreamReader.ReadLength(this));
                        break;
                    }
            }

            return new Media.Container.Node(this, identifier, lengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Container.Node> GetEnumerator()
        {
            while (Remaining >= PacketizedElementaryStreamReader.IdentifierSize)
            {
                Container.Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                //Determine if the node holds data required.
                switch (next.Identifier[3])
                {
                    case Mpeg.StreamTypes.PackHeader:
                        {
                            //Decoder the SCR
                            double scr;
                            ParsePackHeader(next, out scr);

                            //Set the SystemClockRate
                            m_SystemClockRate = scr;

                            break;
                        }
                    case Mpeg.StreamTypes.SystemHeader:
                        {
                            //Parse the system Header
                            ParseSystemsHeader(next);
                            break;
                        }
                    case Mpeg.StreamTypes.ProgramStreamMap:
                        {
                            ParseProgramStreamMap(next);
                            break;
                        }
                }

                Skip(next.DataSize);
            }
        }

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return ProgramStreamReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
        }

        //Find a Pack Header?
        public override Container.Node Root
        {
            get
            {
                long position = Position;

                //Should just be read from position....
                Container.Node result = FindNode(Mpeg.StreamTypes.PackHeader, 0, Length);

                if(CanSeek) Position = position;

                return result;
            }
        }

        //Maybe from the PacketizedElementaryStreamReader
        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        double? m_SystemClockRate;

        /// <summary>
        /// Decodes the SystemClockRate found in the Root node.
        /// </summary>
        public double SystemClockRate
        {
            get
            {
                if (false == m_SystemClockRate.HasValue)
                {
                    double result;
                    ParsePackHeader(Root, out result);
                    m_SystemClockRate = result;
                }

                return m_SystemClockRate.Value;
            }
        }

        //probably not be needed..
        //int? m_PackHeaderVersion;

        ///// <summary>
        ///// Decodes the Mpeg Version from the Pack Header found in the Root node.
        ///// (Adds 1 to the value found because MPEG 1 used 0 and MPEG 2 used 1).
        ///// </summary>
        //public int PackHeaderVersion
        //{
        //    get
        //    {
        //        if (!m_PackHeaderVersion.HasValue)
        //        {
        //            using (var root = Root) m_PackHeaderVersion = 1 + root.Identifier[4] >> 6;
        //        }
        //        return m_PackHeaderVersion.Value;
        //    }
        //}

        protected virtual void ParsePackHeader(Container.Node node, out double scr)
        {
            //Indicate no value found
            scr = double.NaN;

            //If the identifier is not the PackHeader then do nothing
            if (node.Identifier[3] != Mpeg.StreamTypes.PackHeader) return;

            //Declare the high and low parts for decoding
            double high = 0;
            uint low = 0;

            //m_PackHeaderVersion could be ser here..

             //Determine which version of the Pack Header this is
            switch (node.Identifier[4] >> 6)
            {
                case 0: //MPEG 1
                    {
                        //Decode the SCR

                        //Todo, use ReadBigEndianInteger or BitStream
                        high = (double)((node.Identifier[5] >> 3) & 0x01);

                        low = ((uint)((node.Identifier[5] >> 1) & 0x03) << 30) |
                            (uint)(node.Identifier[6] << 22) |
                            (uint)((node.Identifier[7] >> 1) << 15) |
                            (uint)(node.Identifier[8] << 7) |
                            (uint)(node.Identifier[9] << 1);

                        break;
                    }
                default: //MPEG 2
                    {
                        //Decode the SCR

                        //Todo, use ReadBigEndianInteger or BitStream
                        high = (double)((node.Identifier[5] & 0x20) >> 5);

                        low = ((uint)((node.Identifier[5] & 0x18) >> 3) << 30) |
                            (uint)((node.Identifier[5] & 0x03) << 28) |
                            (uint)(node.Identifier[6] << 20) |
                            (uint)((node.Identifier[7] & 0xF8) << 12) |
                            (uint)((node.Identifier[7] & 0x03) << 13) |
                            (uint)(node.Identifier[8] << 5) |
                            (uint)(node.Identifier[9] >> 3);

                        //Determine if this should be also decoded here.
                        //Program Mux Rate - This is a 22 bit integer specifying the rate at which the program stream target decoder receives the Program Stream during the pack in which it is included. The value of program_mux_rate is measured in units of 50 bytes/second. The value 0 is forbidden.

                        break;
                    }
            }

            //SCR and SCR_ext together are the System Clock Reference, a counter driven at 27MHz, used as a reference to synchronize streams. The clock is divided by 300 (to match the 90KHz clocks such as PTS/DTS), the quotient is SCR (33 bits), the remainder is SCR_ext (9 bits)
            scr = (((high * 0x10000) * 0x10000) + low) / 90000.0;
        }

        System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<bool, ushort, Container.Node>> m_StreamBoundEntries = new System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<bool, ushort, Container.Node>>();

        protected virtual void ParseSystemsHeader(Container.Node node)
        {
            if (node.Identifier[3] != Mpeg.StreamTypes.SystemHeader) return;

            //These values or the entire node should probably also be stored.

            //22-bit unsigned integer. Must be greater than or equal to (>=) the maximum value of the program_mux_rate coded in any pack of the program stream. 
            //For DVD-Video this value should be 25200 decimal.
            uint rateBound = Common.Binary.ReadU24(node.Data, 0, BitConverter.IsLittleEndian);
            rateBound &= 0x80000001; //2147483649

            //Make a 'checked' read
            byte temp = node.Data[3];

            //6-bit unsigned integer, ranging from 0 to 32 inclusive. Must be greater than or equal to (>=) the maximum number of audio streams in the program stream. 
            //ISO 13818-1 states this should be the MPEG audio streams, but DVD-Video counts all audio streams. 
            //For DVD-Video this should be the number of audio streams of any type, from 0 to 8 inclusive.
            byte audioBound = (byte)(temp & 0xFC); //252

            //1-bit boolean. If TRUE (1) the program stream is multiplexed at a fixed bitrate. 
            //For DVD-Video this flag should be FALSE (0).
            bool fixedBitrate = (temp & 2) > 0;

            //1-bit boolean. If TRUE (1) the program stream meets the requirements of a "Constrained System parameter Program Stream". 
            //For DVD-Video this flag must be FALSE (0).
            bool csps = (temp & 1) > 0;

            //1-bit boolean. TRUE (1) indicates that there is a specified constant rational relationship between the audio sampling rate and the system_clock_frequency (27MHz). 
            //For DVD-Video this flag should be TRUE (1).

            //Make a 'checked' read
            temp = node.Data[4];

            bool system_audio_lock = (temp & 128) > 0;

            //1-bit boolean. TRUE (1) indicates that there is a specified constant rational relationship between the video picture rate and the system_clock_frequency (27MHz). 
            //For DVD-Video this flag should be TRUE (1). 
            //The PAL/SECAM ratio is 1080000 system clocks (3600 90KHz clocks) per displayed picture. 
            //The NTSC ratio is 900900 system clocks (3003 90KHz clocks) per displayed picture. This rate differs slightly from the nominal rate for NTSC, but is fixed, and consistent with ITU-601.

            bool system_video_lock = (temp & 64) > 0;

            //5-bit unsigned integer, ranging from 0 to 16 inclusive. Must be greater than or equal to (>=) the maximum number of video streams in the program stream. 
            //For DVD-Video this value will always be 1.

            byte video_boud = (byte)(temp & 0xE0);
            
            //1-bit boolean. If CSPS_flag is TRUE (1) this specifies which restraint is applicable to the packet rate, otherwise the flag has no meaning. 
            //For DVD-Video this flag must be FALSE (0).

            //Make a 'checked' read
            temp = node.Data[5];

            bool packRateRestriction = (temp & 128) > 0;

            //7-bit reserved. Should always equal 111 1111.
            if ((temp & 0x7F) != 0x7F) throw new InvalidOperationException("Systems Header Reserved Bits are not Reserved");

            //Stream bound entries start at byte 6
            int offset = 6;

            //Note: While the System header is flexible, for DVD-Video the length and content are fixed. There must be 4 stream_bound entries:
            while (offset < node.DataSize)
            {
                /*
                 8-bit unsigned integer. Indicates to which stream the following buffer bound applies. 
                    1011 1000 (0xB8) indicates all audio streams. 
                    1011 1001 (0xB9) indicates all video streams. 
                    Any other value must be greater than or equal to 1011 1100 (0xBC) and refers to the stream as defined for PES stream ID
                 */
                byte streamId = node.Data[offset++];

                //Use constants from StreamType and see if a switch could be better.
                if (streamId < 0xBC && streamId != 0xB8 && streamId != 0xB9) throw new InvalidOperationException("All Entries in the System Header must apply to a stream with an id >= 0xBC");

                /*
                 1-bit boolean. False (0) indicates the multiplier is 128, TRUE (1) indicates the multiplier is 1024. 
                 Must be 0 for audio streams, 1 for video streams. May be 0 or 1 for other types.
                 */
                //2 reserved bits (32 = 00100000)
                bool bufferBoundScale = (node.Data[offset] & 32) > 0;

                /*
                 13-bit unsigned integer. When multiplied by either 128 or 1024, as indicated by P-STD_buffer_bound_scale, 
                 defines a value that is greater than or equal to the maximum P-STD buffer size for all packets of the designated stream in the entire program stream.
                 */
                //3 bits masked out (0x1FFF = 8191 = 0001111111111111)
                ushort stdBufferSizeBound = (ushort)(Common.Binary.ReadU16(node.Data, ref offset, BitConverter.IsLittleEndian) & 0x1FFF);

                //Create the the StreamBound Entry
                var entry = new Tuple<bool, ushort, Container.Node>(bufferBoundScale, stdBufferSizeBound, node);

                //Add or Update given the streamId
                m_StreamBoundEntries.AddOrUpdate(streamId, entry, (a, old) => entry);
            }
        }

        public override IEnumerable<Container.Track> GetTracks()
        {
            //Use the StreamBound entries in the SystemsHeader to determine what media is contained.
            foreach (var entry in m_StreamBoundEntries)
            {
                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                switch (entry.Key)
                {
                    case 0xB8: //All Audio
                        {
                            mediaType = Sdp.MediaType.audio;
                            break;
                        }
                    case 0xB9: //All Video
                        {
                            mediaType = Sdp.MediaType.video;
                            break;
                        }
                    case Mpeg.StreamTypes.PrivateStream1: //0xBD
                        {
                            //AC3 SubId From 0x80 to 0x87
                            //DTS SubId From 0x88 to 0x8F
                            //LPCM SubId From 0xAD to 0xA7
                            //Subtitles SubId From 0x20 to 0x3F 
                            break;
                        }
                    default:
                        {
                            if (Mpeg.StreamTypes.IsMpeg1or2AudioStream(entry.Key)) mediaType = Sdp.MediaType.audio;
                            else if (Mpeg.StreamTypes.IsMpeg1or2VideoStream(entry.Key)) mediaType = Sdp.MediaType.video;
                            break;
                        }
                }

                //Give the System Header as the 'header'
                yield return new Container.Track(entry.Value.Item3, string.Empty, entry.Key, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0, mediaType, BitConverter.GetBytes(0));
            }
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
