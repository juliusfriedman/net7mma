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

namespace Media.Container.Mpeg
{
    /// <summary>
    /// Represents the logic necessary to read Mpeg Transport Streams.
    /// </summary>
    public class TransportStreamReader : Media.Container.MediaFileStream, Media.Container.IMediaContainer
    {
        public const byte Marker = 0x47, VariableLength = 0x20, NonZeroLength = 0x10;

        public const int IdentifierSize = 4, LengthSize = 0, PacketLength = 188;

        //Forms a dependence on Codecs.dll.
        //Maybe move StreamType to here
        public static string ToTextualConvention(Container.Node node) { return node.DataSize == 0 ? "AdaptionFieldOnlyPacket" : Media.Codecs.Video.Mpeg4.StreamType.ToTextualConvention(node.RawData[0]); }

        public static int GetStreamId(Container.Node node) { return node.Identifier[1]; }

        public static bool HasTransportErrorIndicator(Container.Node node) { return (node.Identifier[1] & 0x80) > 0; }

        public static bool HasPayloadUnitStartIndicator(Container.Node node) { return (node.Identifier[1] & 0x40) > 0; }

        public static bool HasTransportPriority(Container.Node node) { return (node.Identifier[1] & VariableLength) > 0; }

        public static int GetProgramId(Container.Node node) { return (node.Identifier[1] << 3) * 256 + node.Identifier[2]; }

        public static bool HasAdaptationField(Container.Node node) { return (node.Identifier[3] & VariableLength) != 0; }

        public static int TransportScramblingControl(Container.Node node) { return (node.Identifier[3] << 3) & 0xF0; }        

        public static int AdaptationFieldControl(Container.Node node) { return (node.Identifier[3] << 4); }

        public static int AdaptationFieldContinuityCounter(Container.Node node) { return node.Identifier[3] & 0xF; }

        public static byte[] GetAdaptationFieldData(Container.Node node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (node.LengthSize < 1 || !HasAdaptationField(node)) return Utility.Empty;

            int size = node.LengthSize - 1;

            long position = node.Master.BaseStream.Position, neededPosition = node.Offset + IdentifierSize + 1;

            node.Master.BaseStream.Seek(neededPosition, System.IO.SeekOrigin.Begin);

            byte[] data = new byte[size];

            node.Master.BaseStream.Read(data, 0, size);
            
            node.Master.BaseStream.Seek(position, System.IO.SeekOrigin.Begin);

            return data;
        }

        //Values from AdaptationField

        public TransportStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public TransportStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        ///// <summary>
        ///// Always seeks in different of the offset given and the modulo ofr the TransportStream PacketLength (188)
        ///// </summary>
        ///// <param name="offset"></param>
        ///// <param name="origin"></param>
        ///// <returns></returns>
        //public override long Seek(long offset, System.IO.SeekOrigin origin) { return base.Seek(offset - (offset % PacketLength), origin); }

        ///// <summary>
        ///// Skips in terms of packets
        ///// </summary>
        ///// <param name="count"></param>
        ///// <returns></returns>
        //public override long Skip(long count) { return base.Skip(count); }

        public Container.Node ReadNext()
        {
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            if (identifier[0] != Marker) throw new InvalidOperationException("Cannot Find Marker");

            //The last byte determine if there is a variable length (adaptation) field
            byte last = identifier[3];

            //Determine the length of the packet and amount of bytes required to read the length
            int length = PacketLength - IdentifierSize, lengthSize = LengthSize;

            //Check for varible length field
            if ((last & VariableLength) != 0)
            {
                //Determine its size by reading a byte
                lengthSize = (byte)((ReadByte()) & byte.MaxValue);
                
                //If it has any more data skip past it (obtained with read at DataOffset - LengthSize)
                if (lengthSize > 0) Skip(lengthSize);

                //Do include the size byte in the length size
                lengthSize++;

                //Don't include the variable length data in the size
                length -= lengthSize;
            }

            //Check for Zero Length Flag (only variable length field)
            if ((last & NonZeroLength) == 0) length = 0;

            //Return the Node
            return new Container.Node(this, identifier, lengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Container.Node> GetEnumerator()
        {
            while (Remaining >= PacketLength)
            {
                Container.Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                Skip(next.DataSize);
            }
        }  

        public override Container.Node Root
        {
            get
            {
                long position = Position;
                var result = this.FirstOrDefault();
                Position = position;
                return result;
            }
        }

        //First with ProgramStreamMap?
        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        //Read all with Start of Payload then determine from Stream Ids.
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
