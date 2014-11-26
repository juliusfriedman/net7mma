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
    /// Represents the logic necessary to read a Packetized Elementary Stream.
    /// Packetized Elementary Stream (PES) is a specification in the MPEG-2 Part 1 (Systems) (ISO/IEC 13818-1) and ITU-T H.222.0[1][2] that defines carrying of elementary streams (usually the output of an audio or video encoder) in packets within MPEG program stream and MPEG transport stream.
    /// The elementary stream is packetized by encapsulating sequential data bytes from the elementary stream inside PES packet headers
    /// </summary>
    public class PacketizedElementaryStreamReader : Media.Container.MediaFileStream, Media.Container.IMediaContainer
    {
        internal const int IdentifierSize = 4, LengthSize = 2, MinimumSize = IdentifierSize + LengthSize;

        public static int GetStreamId(byte[] identifier, int offset = 3) { return identifier[offset]; }

        public static string ToTextualConvention(byte[] identifer, int offset = 0)
        {
            int streamId = GetStreamId(identifer);
            return string.Join("-", "StreamId",  streamId, '(', Media.Containers.Mpeg.StreamTypes.ToTextualConvention((byte)streamId), ')');
        }

        public PacketizedElementaryStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public PacketizedElementaryStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public PacketizedElementaryStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public static byte[] ReadIdentifier(System.IO.Stream stream)
        {
            //Read the PES Header (Prefix and StreamId)
            byte[] identifier = new byte[IdentifierSize];

            stream.Read(identifier, 0, IdentifierSize);

            return identifier;
        }

        public static byte[] ReadLength(System.IO.Stream stream)
        {
            //Read Length
            byte[] lengthBytes = new byte[LengthSize];

            stream.Read(lengthBytes, 0, LengthSize);

            return lengthBytes;
        }

        public static int DecodeLength(byte[] lengthBytes)
        {
            return Common.Binary.ReadU16(lengthBytes, 0, BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Reads a PES Header
        /// </summary>
        /// <returns>The Node which represents the PESPacket</returns>
        public virtual Container.Node ReadNext()
        {
            //Read the PES Header (Prefix and StreamId)
            byte[] identifier = ReadIdentifier(this);

            //https://github.com/peterdk/SupRip/blob/master/Bluray%20Sup.txt

            //Should check for PG Header (P OR p OR g, OR G)?
            //IF found Pts and Dts are present.
            //Not sure why this couldn't have just been in the Payload like everything else e.g the Optional PES Header..

            //Read and decode length
            int length = DecodeLength(ReadLength(this));

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

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return PacketizedElementaryStreamReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
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
