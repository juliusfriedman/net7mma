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

        public static int GetStreamId(Container.Node node) { return node.Identifier[3]; }

        public static string ToTextualConvention(Media.Container.Node node)
        {
            throw new NotImplementedException();
        }

        public PacketizedElementaryStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public PacketizedElementaryStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Reads a PES Header
        /// </summary>
        /// <returns>The Node which represents the PESPacket</returns>
        public Container.Node ReadNext()
        {
            //Read the PES Header (Prefix and StreamId)
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
