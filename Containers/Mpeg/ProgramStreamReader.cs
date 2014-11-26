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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// Represents the logic necessary to read Mpeg Program Streams. (.VOB, .EVO, .ps, .psm .m2ts etc.)
    /// Program stream (PS or MPEG-PS) is a container format for multiplexing digital audio, video and more. The PS format is specified in MPEG-1 Part 1 (ISO/IEC 11172-1) and MPEG-2 Part 1, Systems (ISO/IEC standard 13818-1[6]/ITU-T H.222.0[4][5]). The MPEG-2 Program Stream is analogous and similar to ISO/IEC 11172 Systems layer and it is forward compatible.[7][8]
    /// ProgramStreams are created by combining one or more Packetized Elementary Streams (PES), which have a common time base, into a single stream.
    /// </summary>
    public class ProgramStreamReader : PacketizedElementaryStreamReader
    {
        public ProgramStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public ProgramStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public ProgramStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        //Might need the data not the identifier
        public static string ToTextualConvention(byte[] identifier)
        {
            return PacketizedElementaryStreamReader.ToTextualConvention(identifier);
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
            if (Common.Binary.ReadU24(identifier, 0, !BitConverter.IsLittleEndian) != Common.Binary.ReadU24(Mpeg.StartCodes.Prefix, 0, !BitConverter.IsLittleEndian)) throw new InvalidOperationException("Cannot Find StartCode Prefix.");

            int length = 0, lengthSize = PacketizedElementaryStreamReader.LengthSize;

            //Determine which type of node we are dealing with
            switch (identifier[3])
            {
                case Mpeg.StreamTypes.PackHeader: //Also Mpeg.StreamTypes.PackHeader
                    {
                        //No bytes are related to the length yet
                        lengthSize = 0;

                        //MPEG 1 Used only 2 bits 0 1
                        //MPEG 2 Used 4 bits 0 0 0 1
                        byte next = (byte)ReadByte();

                        //Determine which version
                        switch (next >> 6)
                        {
                            case 0: //MPEG 1
                                {
                                    //Read 10 more bytes when getting the data
                                    Array.Resize(ref identifier, 12);
                                    Read(identifier, IdentifierSize + 1, 7);
                                    break;
                                }
                            default:
                                {
                                    //Read 10 more bytes when getting the data
                                    Array.Resize(ref identifier, 14);
                                    Read(identifier, IdentifierSize + 1, 9);
                                    //SCR and SCR_ext together are the System Clock Reference, a counter driven at 27MHz, used as a reference to synchronize streams. The clock is divided by 300 (to match the 90KHz clocks such as PTS/DTS), the quotient is SCR (33 bits), the remainder is SCR_ext (9 bits)

                                    //Program Mux Rate - This is a 22 bit integer specifying the rate at which the program stream target decoder receives the Program Stream during the pack in which it is included. The value of program_mux_rate is measured in units of 50 bytes/second. The value 0 is forbidden.

                                    //Include Stuffing length with mask (00000111) reversed bits
                                    length = (byte)(identifier[13] & 0x07);
                                    break;
                                }
                        }

                        //Put the 4th byte back
                        identifier[IdentifierSize] = next;

                        break;
                    }
                //case Mpeg.StreamTypes.SystemHeader:
                //    {
                //        //Might need to parse the data of this Node.
                //        goto default; //(It is just a PES but contains data for the stream)
                //    }
                //case Mpeg.StreamTypes.ProgramStreamMap: //PSM
                //    {
                //        break;
                //    }
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
            get { throw new NotImplementedException(); }
        }

        //Maybe from the PacketizedElementaryStreamReader
        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<Container.Track> GetTracks()
        {
            //Find each ProgramStream Marker (Pack Header Format) http://en.wikipedia.org/wiki/MPEG_program_stream#Coding_details
            //Parse to create a track.
            return Enumerable.Empty<Container.Track>();
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
