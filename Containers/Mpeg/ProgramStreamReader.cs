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
    /// Represents the logic necessary to read Mpeg Program Streams.
    /// </summary>
    public class ProgramStreamReader : Media.Container.MediaFileStream, Media.Container.IMediaContainer
    {

        public enum StreamIdentifier : byte
        {
            ProgramStreamMap = 0xBC,
            PrivateStream1 = 0xBD,
            PaddingStream = 0xBE,
            PrivateStream2 = 0xBF,
            
            #region Numbered Audio Streams
            AudioStream0 = 0xC0,
            AudioStream1 = 0xC1,
            AudioStream2 = 0xC2,
            AudioStream3 = 0xC3,
            AudioStream4 = 0xC4,
            AudioStream5 = 0xC5,
            AudioStream6 = 0xC6,
            AudioStream7 = 0xC7,
            AudioStream8 = 0xC8,
            AudioStream9 = 0xC9,
            AudioStream10 = 0xCA,
            AudioStream11 = 0xCB,
            AudioStream12 = 0xCC,
            AudioStream13 = 0xCD,
            AudioStream14 = 0xCE,
            AudioStream15 = 0xCF,
            AudioStream16 = 0xD0,
            AudioStream17 = 0xD1,
            AudioStream18 = 0xD2,
            AudioStream19 = 0xD3,
            AudioStream20 = 0xD4,
            AudioStream21 = 0xD5,
            AudioStream22 = 0xD6,
            AudioStream23 = 0xD7,
            AudioStream24 = 0xD8,
            AudioStream25 = 0xD9,
            AudioStream26 = 0xDA,
            AudioStream27 = 0xDB,
            AudioStream28 = 0xDC,
            AudioStream29 = 0xDD,
            AudioStream30 = 0xDE,
            AudioStream31 = 0xDF,
            #endregion

            #region Numbered Video Streams
            VideoStream0 = 0xE0,
            VideoStream1 = 0xE1,
            VideoStream2 = 0xE2,
            VideoStream3 = 0xE3,
            VideoStream4 = 0xE4,
            VideoStream5 = 0xE5,
            VideoStream6 = 0xE6,
            VideoStream7 = 0xE7,
            VideoStream8 = 0xE8,
            VideoStream9 = 0xE9,
            VideoStream10 = 0xEA,
            VideoStream11 = 0xEB,
            VideoStream12 = 0xEC,
            VideoStream13 = 0xED,
            VideoStream14 = 0xEE,
            VideoStream15 = 0xEF,
            #endregion

            EcmStream = 0xF0,
            EmmStream = 0xF1,
            DsmCcStream = 0xF2,
            Iso13522Stream = 0xF3,
            TypeAStream = 0xF4,
            TypeBStream = 0xF5,
            TypeCStream = 0xF6,
            TypeDStream = 0xF7,
            TypeEStream = 0xF8,
            AncillaryStream = 0xF9,
            SlPacketizedStream = 0xFA,
            FlexMuxStream = 0xFB,
            // 0xFC-0xFE RESERVED
            ProgramStreamDirectory = 0xFF,
        }

        internal const byte EndCode = 0xB9;

        internal const int IdentifierSize = 4, LengthSize = 2, MinimumSize = IdentifierSize + LengthSize;

        public static bool IsReserved(StreamIdentifier identifier)
        {
            byte sid = (byte)identifier;
            return sid >= 0xFC && sid <= 0xFE;
        }


        public static StreamIdentifier GetStreamIdentifier(Media.Container.Node pesPacket)
        {
            if (pesPacket == null) throw new ArgumentNullException("pesPacket");
            return (StreamIdentifier)pesPacket.Identifier[3];
        }


        public static string ToTextualConvention(Media.Container.Node pesPacket)
        {
            StreamIdentifier sid = GetStreamIdentifier(pesPacket);
            if (IsReserved(sid)) return "Reserved";
            return sid.ToString();
        }

        public ProgramStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public ProgramStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }


        //Reads a PES Packet
        public Container.Node ReadNext()
        {
            //Read the PES Header
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            //Read Length
            byte[] lengthBytes = new byte[2];

            Read(lengthBytes, 0, LengthSize);

            //Determine length
            int length = Common.Binary.ReadU16(lengthBytes, 0, BitConverter.IsLittleEndian);

            //Optional PES Header and Stuffing Bytes if length > 0

            return new Media.Container.Node(this, identifier, LengthSize, Position, length, length <= Remaining);
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

        public override Container.Node Root
        {
            get { throw new NotImplementedException(); }
        }

        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<Container.Track> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }       
    }
}
