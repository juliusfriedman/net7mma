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
    /// Represents the logic necessary to read Mpeg Program Streams. (.VOB, .EVO, .ps)
    /// Program stream (PS or MPEG-PS) is a container format for multiplexing digital audio, video and more. The PS format is specified in MPEG-1 Part 1 (ISO/IEC 11172-1) and MPEG-2 Part 1, Systems (ISO/IEC standard 13818-1[6]/ITU-T H.222.0[4][5]). The MPEG-2 Program Stream is analogous and similar to ISO/IEC 11172 Systems layer and it is forward compatible.[7][8]
    /// ProgramStreams are created by combining one or more Packetized Elementary Streams (PES), which have a common time base, into a single stream.
    /// </summary>
    //http://www.incospec.com/resources/webinars/files/MPEG%20101%20Demyst%20Analysis%20&%20Picture%20Symptoms%2020110808_opt.pdf
    public class ProgramStreamReader : Media.Container.MediaFileStream, Media.Container.IMediaContainer
    {
        internal const int IdentifierSize = 4, LengthSize = 0, MinimumSize = IdentifierSize + LengthSize;

        public ProgramStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public ProgramStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public ProgramStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        //Might need the data not the identifier
        public static string ToTextualConvention(byte[] identifier)
        {
            //Should determine based on header code, 00, 00, 01, XX
            //Mpeg.StartCodes.ToTextualConvention(identifier[3])
            return "PsUnit";
        }

        //Better to inherit from then accumulate packets when a PES Length == 0 or no PayloadStartUnitIndicator is set
        PacketizedElementaryStreamReader pes;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Container.Node ReadNext()
        {
            //Read 14 bytes (Reads too many, only should read 4)
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            //Check for sync
            if(identifier[2] != 0x01) throw new InvalidOperationException("Cannot Find StartCode Prefix.");

            int length = 0, lengthSize = LengthSize;

            switch (identifier[3])
            {
                case Mpeg.StartCodes.EndCode:
                    {
                        break;
                    } //Could not also be handled as a PES?
                case Mpeg.StartCodes.SyncByte: //Pack Header - Mpeg.StreamTypes.PackHeader
                    {
                        //Always 14 +
                        //Read 10 more bytes when getting the data
                        Array.Resize(ref identifier, 14);
                        Read(identifier, IdentifierSize, 10);
                        //Include Stuffing length with mask (00000111) reversed bits
                        length = (byte)(identifier[13] & 0x07);
                        break;
                    }
                //Handle as PES
                //case Mpeg.StartCodes.SystemHeader: //Systems Header, etc have length after code
                //    {
                //        //Length does not include bytes read (00 00 01, BB, XX, XX)
                //        //Read 2 byte length
                //        lengthSize = 2;
                //        byte[] lengthBytes = new byte[lengthSize];
                //        Read(lengthBytes, 0, lengthSize);
                //        //This is the Descriptors count which follow when divided by 3.
                //        length = Common.Binary.ReadU16(lengthBytes, 0, BitConverter.IsLittleEndian);
                //        break;
                //    }
                default: //PESPacket
                    {
                        if(pes == null) pes = new PacketizedElementaryStreamReader(this, System.IO.FileAccess.Read);
                        //Could just have a static method to read the TsUnit from and would eliminate seeking back or having to keep an instance.
                        
                        //Could also inherit from PacketizedElementaryStreamReader
                        var result = pes.ReadNext();

                        //Update this streams position based on the size of the result from the PES
                        Position += result.TotalSize;

                        return result;
                        
                        //Should accumulate then return a new Node with the first offset and Sum of the total size
                    }
            }

            return new Media.Container.Node(this, identifier, lengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Container.Node> GetEnumerator()
        {
            while (Remaining >= MinimumSize)
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
            //Find each PESPacket within the ProgramStream Marker
            //create a PacketizedElementaryStreamReader from this, with FileAccess.Read
            //return the GetTracks from that
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        //GetPacketizedElementaryStreams?
    }
}
