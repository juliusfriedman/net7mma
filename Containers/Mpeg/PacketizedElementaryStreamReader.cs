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
    /// <see href="https://en.wikipedia.org/wiki/Packetized_elementary_stream">Wikipedia Packetized elementary stream</see>
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

        public PacketizedElementaryStreamReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, null, bufferSize, true) { } 

        //Methods for reading the PES OptionalHeader and StuffingLength...

        /// <summary>
        /// Reads 4 bytes from the given stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ReadIdentifier(System.IO.Stream stream)
        {
            //Read the PES Header (Prefix and StreamId)
            byte[] identifier = new byte[IdentifierSize];

            stream.Read(identifier, 0, IdentifierSize);

            return identifier;
        }

        /// <summary>
        /// Reads two bytes from the given stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ReadLength(System.IO.Stream stream)
        {
            //Read Length
            byte[] lengthBytes = new byte[LengthSize];

            stream.Read(lengthBytes, 0, LengthSize);

            return lengthBytes;
        }

        /// <summary>
        /// Decodes the PES Length from the given bytes at the given offset.
        /// Requires at least 2 bytes.
        /// </summary>
        /// <param name="lengthBytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int DecodeLength(byte[] lengthBytes, int offset = 0)
        {
            return Common.Binary.ReadU16(lengthBytes, offset, Common.Binary.IsLittleEndian);
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

                //Determine if the node holds data required.
                switch (next.Identifier[3])
                {
                    case Mpeg.StreamTypes.ProgramStreamMap:
                        {
                            ParseProgramStreamMap(next);
                            break;
                        }
                }

                Skip(next.DataSize);
            }
        }

        //Entry {esId, { esType, esData } }
        protected System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<byte, byte[]>> m_ProgramStreams = new System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<byte, byte[]>>();

        protected virtual void ParseProgramStreamMap(Container.Node node)
        {
            if (node.Identifier[3] != Mpeg.StreamTypes.ProgramStreamMap) return;

            int dataLength = node.Data.Length;

            if (dataLength < 4) return;

            byte mapId = node.Data[0];

            byte reserved = node.Data[1];

            ushort infoLength = Common.Binary.ReadU16(node.Data, 2, Common.Binary.IsLittleEndian);

            ushort mapLength = Common.Binary.ReadU16(node.Data, 4 + infoLength, Common.Binary.IsLittleEndian);

            int offset = 4 + infoLength + 2;

            int crcOffset = dataLength - 4;

            //While data remains in the map
            while (offset < crcOffset)
            {
                //Find out the type of item it is
                byte esType = node.Data[offset++];

                //Find out the elementary stream id
                byte esId = node.Data[offset++];

                //find out how long the info is
                ushort esInfoLength = Common.Binary.ReadU16(node.Data, offset, Common.Binary.IsLittleEndian);

                //Get a array containing the info
                byte[] esData = esInfoLength == 0 ? Media.Common.MemorySegment.EmptyBytes : node.Data.Skip(offset).Take(esInfoLength).ToArray();

                //Create the entry
                var entry = new Tuple<byte, byte[]>(esType, esData);

                //should keep entries until crc is updated if present and then insert.

                //Add it to the ProgramStreams
                m_ProgramStreams.AddOrUpdate(esId, entry, (id, old) => entry);

                //Move the offset
                offset += 2 + esInfoLength;
            }

            //Map crc
        }

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return PacketizedElementaryStreamReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
        }

        public override Container.Node Root
        {
            //Should be ReadNext()
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
