﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Image.Jpeg
{

    //Would be helpful to have a Stream with a buffer for skipping
    
    //Should allow marker reading on it's own outside of the RFC2435 class to decouple logic.
    public class MarkerReader
    {

        System.IO.Stream jpegStream;

        int streamOffset, streamLength;

        public Marker current;

        public IEnumerable<Marker> ReadMarkers()
        {
            int FunctionCode, CodeSize = 0;

            int prefixCount = 0;

            //Find a Jpeg Tag while we are not at the end of the stream
            //Tags come in the format 0xFFXX
            while ((FunctionCode = jpegStream.ReadByte()) != -1)
            {
                ++streamOffset;

                //If the prefix is a tag prefix then read another byte as the Tag
                if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.Prefix)
                {
                    //Increase the count of prefix bytes
                    ++prefixCount;

                    //Get the underlying FunctionCode
                    FunctionCode = jpegStream.ReadByte();

                    ++streamOffset;

                    //If we are at the end break
                    if (FunctionCode == -1) break;

                    //Ensure not padded
                    if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.Prefix
                        ||
                        FunctionCode == 0) continue;

                    //Last Tag
                    if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.StartOfInformation
                        ||
                        FunctionCode == Media.Codecs.Image.Jpeg.Markers.EndOfInformation) goto AtMarker;

                    //Read the Marker Length

                    //Read Length Bytes
                    byte h = (byte)jpegStream.ReadByte(), l = (byte)jpegStream.ReadByte();

                    streamOffset += 2;

                    //Calculate Length
                    CodeSize = h * 256 + l;

                    //Correct Length
                    CodeSize -= 2; //Not including their own length

                AtMarker:

                    current = new Marker()
                    {
                        PrefixLength = prefixCount,
                        Code = (byte)FunctionCode,
                        Length = CodeSize + 2,
                        Data = new byte[CodeSize]
                    };

                    jpegStream.Read(current.Data, 0, CodeSize);

                    yield return current;
                }
            }
        }
    }

    //Needs to implement a common class if the elements can be reused => 
    public class Marker
    {
        //OriginOffset

        //Should probably include data of prefix since entropy encoded sections can use ff 00 or ff 01

        public int PrefixLength;

        public byte Code;

        public int Length; //Can't exceed 65535

        //DataSize => Length > 0 ? Length - 2 : 0;

        public byte[] Data;

        //TotalSize => PrefixLength + 1 + Length

        public IEnumerable<byte> Prepare() //bool includePrefix, includeCode, includeLength, includeData...
        {
            if(PrefixLength > 0) foreach(byte b in Enumerable.Repeat<byte>(Jpeg.Markers.Prefix, PrefixLength)) yield return b;

            yield return Code;

            if (Code == Markers.StartOfInformation || Code == Markers.EndOfInformation) yield break;

            foreach (byte b in Common.Binary.GetBytes((short)Length, false == BitConverter.IsLittleEndian)) yield return b;

            if (Length > 0) foreach (byte b in Data) yield return b;

            //Should project entire sequence rather than return one at a time.
            //IEnumerable<byte> result...
        }
    }
}
