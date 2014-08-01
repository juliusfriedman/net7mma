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
using System.Text;

namespace Media.Rtsp.Server.Streams
{

    //Todo Seperate ImageStream from JpegRtpImageSource

    //public class ImageStream : SourceStream
    //{
    //    public Rtp.RtpFrame CreateFrames() { }
    //    public virtual void Encoode() { }
    //    public virtual void Decode() { }
    //}

    /// <summary>
    /// Sends System.Drawing.Images over Rtp by encoding them as a RFC2435 Jpeg
    /// </summary>
    public class RFC2435Stream : RtpSink
    {

        #region NestedTypes

        /// <summary>
        /// Implements RFC2435
        /// Encodes from a System.Drawing.Image to a RFC2435 Jpeg.
        /// Decodes a RFC2435 Jpeg to a System.Drawing.Image.
        ///  <see cref="http://tools.ietf.org/rfc/rfc2435.txt">RFC 2435</see>
        ///  <see cref="http://www.jpeg.org/public/fcd15444-10.pdf">Jpeg Spec</see>
        /// </summary>
        public class RFC2435Frame : Rtp.RtpFrame
        {
            #region Statics

            public const int MaxWidth = 2048;

            public const int MaxHeight = 4096;

            public const byte RtpJpegPayloadType = 26;

            internal static System.Drawing.Imaging.ImageCodecInfo JpegCodecInfo = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders().First(d => d.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);

            /// <summary>
            /// Markers which are contained in a valid Jpeg Image
            /// <see cref="http://www.jpeg.org/public/fcd15444-10.pdf">A.1 Extended capabilities</see>
            /// </summary>
            public sealed class JpegMarkers
            {
                static JpegMarkers() { }

                /// <summary>
                /// In every marker segment the first two bytes after the marker shall be an unsigned value [In Network Endian] that denotes the length in bytes of 
                /// the marker segment parameters (including the two bytes of this length parameter but not the two bytes of the marker itself). 
                /// </summary>

                public const byte Prefix = 0xff;

                public const byte TextComment = 0xfe;

                public const byte StartOfFrame = 0xc0;

                public const byte HuffmanTable = 0xc4;

                public const byte StartOfInformation = 0xd8;

                public const byte AppFirst = 0xe0;

                public const byte AppLast = 0xee;

                public const byte EndOfInformation = 0xd9;

                public const byte QuantizationTable = 0xdb;

                public const byte DataRestartInterval = 0xdd;

                public const byte StartOfScan = 0xda;
            }

            /// <summary>
            /// Creates RST header for JPEG/RTP packet.
            /// </summary>
            /// <param name="dri">dri Restart interval - number of MCUs between restart markers</param>
            /// <param name="f">optional first bit (defaults to 1)</param>
            /// <param name="l">optional last bit (defaults to 1)</param>
            /// <param name="count">optional number of restart markers (defaults to 0x3FFF)</param>
            /// <returns>Rst Marker</returns>
            static byte[] CreateRtpJpegDataRestartIntervalMarker(ushort dri, bool f = true, bool l = true, ushort count = 0x3FFF)
            {
                //     0                   1                   2                   3
                //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                //|       Restart Interval        |F|L|       Restart Count       |
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                byte[] data = new byte[4];
                data[0] = (byte)((dri >> 8) & 0xFF);
                data[1] = (byte)dri;

                //Network Endian            

                IEnumerable<byte> countBytes = BitConverter.GetBytes(count);

                if (BitConverter.IsLittleEndian) countBytes = countBytes.Reverse();
                countBytes.ToArray().CopyTo(data, 2);

                if (f) data[2] = (byte)((1) << 7);

                if (l) data[2] |= (byte)((1) << 6);

                return data;
            }

            /// <summary>
            /// Utility function to create RtpJpegHeader either for initial packet or template for further packets
            /// </summary>
            /// <param name="typeSpecific"></param>
            /// <param name="fragmentOffset"></param>
            /// <param name="jpegType"></param>
            /// <param name="quality"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <param name="dri"></param>
            /// <param name="qTables"></param>
            /// <returns></returns>
            static byte[] CreateRtpJpegHeader(uint typeSpecific, long fragmentOffset, uint jpegType, uint quality, uint width, uint height, byte[] dri, byte precisionTable, List<byte> qTables)
            {
                List<byte> RtpJpegHeader = new List<byte>();

                /*
                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                | Type-specific |              Fragment Offset                  |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |      Type     |       Q       |     Width     |     Height    |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                //Type specific
                //http://tools.ietf.org/search/rfc2435#section-3.1.1
                RtpJpegHeader.Add((byte)typeSpecific);

                //Three byte fragment offset
                //http://tools.ietf.org/search/rfc2435#section-3.1.2

                if (BitConverter.IsLittleEndian) fragmentOffset = Common.Binary.ReverseU32((uint)fragmentOffset);

                RtpJpegHeader.AddRange(BitConverter.GetBytes((uint)fragmentOffset), 1, 3);


                //(Jpeg)Type
                //http://tools.ietf.org/search/rfc2435#section-3.1.3
                RtpJpegHeader.Add((byte)jpegType);

                //http://tools.ietf.org/search/rfc2435#section-3.1.4 (Q)
                RtpJpegHeader.Add((byte)quality);

                //http://tools.ietf.org/search/rfc2435#section-3.1.5 (Width)
                RtpJpegHeader.Add((byte)(width / 8));

                //http://tools.ietf.org/search/rfc2435#section-3.1.6 (Height)
                RtpJpegHeader.Add((byte)(height / 8));

                //If this is the first packet
                if (fragmentOffset == 0)
                {
                    //http://tools.ietf.org/search/rfc2435#section-3.1.7 (Restart Marker header)
                    if (jpegType >= 63 && dri != null)
                    {
                        //Create a Rtp Restart Marker, Set first and last
                        RtpJpegHeader.AddRange(CreateRtpJpegDataRestartIntervalMarker(Common.Binary.ReadU16(dri, 0, BitConverter.IsLittleEndian)));
                    }

                    //Handle Quantization Tables if provided
                    if (quality >= 100)
                    {
                        //Check for a table
                        if (qTables.Count < 64) throw new InvalidOperationException("At least 1 quantization table must be included when quality >= 100");

                        //Check for overflow
                        if (qTables.Count > ushort.MaxValue) Common.Binary.CreateOverflowException("qTables", qTables.Count, ushort.MinValue.ToString(), ushort.MaxValue.ToString());

                        RtpJpegHeader.Add(0); //Must Be Zero      

                        RtpJpegHeader.Add(precisionTable);//PrecisionTable may be bit flagged to indicate 16 bit tables

                        //Add the Length field
                        if (BitConverter.IsLittleEndian) RtpJpegHeader.AddRange(BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)qTables.Count)));
                        else RtpJpegHeader.AddRange(BitConverter.GetBytes((ushort)qTables.Count));

                        //here qTables may have 16 bit precision and may need to be reversed if BitConverter.IsLittleEndian
                        RtpJpegHeader.AddRange(qTables);
                    }
                }

                return RtpJpegHeader.ToArray();
            }

            /// <summary>
            /// http://en.wikipedia.org/wiki/JPEG_File_Interchange_Format
            /// </summary>
            /// <param name="jpegType"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <param name="tables"></param>
            /// <param name="precision"></param>
            /// <param name="dri"></param>
            /// <returns></returns>
            internal static byte[] CreateJFIFHeader(byte jpegType, uint width, uint height, ArraySegment<byte> tables, byte precision, ushort dri)
            {
                List<byte> result = new List<byte>();
                result.Add(JpegMarkers.Prefix);
                result.Add(JpegMarkers.StartOfInformation);//SOI

                //Quantization Tables
                result.AddRange(CreateQuantizationTablesMarkers(tables, precision));

                //Data Restart Invertval
                if (dri > 0) result.AddRange(CreateDataRestartIntervalMarker(dri));

                //Start Of Frame

                /*
             
                   BitsPerSample / ColorComponents (1)
                   EncodingProcess	(1)
                 * Possible Values
                        0x0 = Baseline DCT, Huffman coding 
                        0x1 = Extended sequential DCT, Huffman coding 
                        0x2 = Progressive DCT, Huffman coding 
                        0x3 = Lossless, Huffman coding 
                        0x5 = Sequential DCT, differential Huffman coding 
                        0x6 = Progressive DCT, differential Huffman coding 
                        0x7 = Lossless, Differential Huffman coding 
                        0x9 = Extended sequential DCT, arithmetic coding 
                        0xa = Progressive DCT, arithmetic coding 
                        0xb = Lossless, arithmetic coding 
                        0xd = Sequential DCT, differential arithmetic coding 
                        0xe = Progressive DCT, differential arithmetic coding 
                        0xf = Lossless, differential arithmetic coding
                    ImageHeight	(2)
                    ImageWidth	(2) 
                    YCbCrSubSampling	(1)
                 * Possible Values
                        '1 1' = YCbCr4:4:4 (1 1) 
                        '1 2' = YCbCr4:4:0 (1 2) 
                        '2 1' = YCbCr4:2:2 (2 1)              
                        '2 2' = YCbCr4:2:0 (2 2) 
                        '4 1' = YCbCr4:1:1 (4 1) 
                        '4 2' = YCbCr4:1:0 (4 2)
             
                 */

                result.Add(JpegMarkers.Prefix);
                result.Add(JpegMarkers.StartOfFrame);//SOF
                result.Add(0x00); //Length
                result.Add(0x11); // Decimal 17 -> 15 bytes

                result.Add(0x08); //Bits Per Components and EncodingProcess

                result.Add((byte)(height >> 8)); //Height
                result.Add((byte)height);

                result.Add((byte)(width >> 8)); //Width
                result.Add((byte)width);

                result.Add(0x03);//Number of components

                result.Add(0x01);//Component Number

                //Set the Horizontal Sampling Factor
                result.Add((byte)(jpegType == 0 ? 0x21 : 0x22));

                result.Add(0x00);//Matrix Number (Quant Table Id)?
                result.Add(0x02);//Component Number
                result.Add(0x11);//Horizontal or Vertical Sample

                //ToDo - Handle 16 Bit Precision
                result.Add(1);//Matrix Number

                result.Add(0x03);//Component Number
                result.Add(0x11);//Horizontal or Vertical Sample

                //ToDo - Handle 16 Bit Precision
                result.Add(1);//Matrix Number      

                //Huffman Tables
                result.AddRange(CreateHuffmanTableMarker(lum_dc_codelens, lum_dc_symbols, 0, 0));
                result.AddRange(CreateHuffmanTableMarker(lum_ac_codelens, lum_ac_symbols, 0, 1));
                result.AddRange(CreateHuffmanTableMarker(chm_dc_codelens, chm_dc_symbols, 1, 0));
                result.AddRange(CreateHuffmanTableMarker(chm_ac_codelens, chm_ac_symbols, 1, 1));

                //Start Of Scan
                result.Add(JpegMarkers.Prefix);
                result.Add(JpegMarkers.StartOfScan);//Marker SOS
                result.Add(0x00); //Length
                result.Add(0x0c); //Length - 12
                result.Add(0x03); //Number of components
                result.Add(0x01); //Component Number
                result.Add(0x00); //Matrix Number
                result.Add(0x02); //Component Number
                result.Add(0x11); //Horizontal or Vertical Sample
                result.Add(0x03); //Component Number
                result.Add(0x11); //Horizontal or Vertical Sample
                result.Add(0x00); //Start of spectral
                result.Add(0x3f); //End of spectral (63)
                result.Add(0x00); //Successive approximation bit position (high, low)

                return result.ToArray();
            }

            // The default 'luma' and 'chroma' quantizer tables, in zigzag order:
            static byte[] defaultQuantizers = new byte[]
        {
           // luma table:
           16, 11, 12, 14, 12, 10, 16, 14,
           13, 14, 18, 17, 16, 19, 24, 40,
           26, 24, 22, 22, 24, 49, 35, 37,
           29, 40, 58, 51, 61, 60, 57, 51,
           56, 55, 64, 72, 92, 78, 64, 68,
           87, 69, 55, 56, 80, 109, 81, 87,
           95, 98, 103, 104, 103, 62, 77, 113,
           121, 112, 100, 120, 92, 101, 103, 99,
            //From RFC2435 / Jpeg Spec
            ////16, 11, 10, 16, 24, 40, 51, 61,
            ////12, 12, 14, 19, 26, 58, 60, 55,
            ////14, 13, 16, 24, 40, 57, 69, 56,
            ////14, 17, 22, 29, 51, 87, 80, 62,
            ////18, 22, 37, 56, 68, 109, 103, 77,
            ////24, 35, 55, 64, 81, 104, 113, 92,
            ////49, 64, 78, 87, 103, 121, 120, 101,
            ////72, 92, 95, 98, 112, 100, 103, 99,
           // chroma table:
           17, 18, 18, 24, 21, 24, 47, 26,
           26, 47, 99, 66, 56, 66, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99
            //From RFC2435 / Jpeg Spec
            ////17, 18, 24, 47, 99, 99, 99, 99,
            ////18, 21, 26, 66, 99, 99, 99, 99,
            ////24, 26, 56, 99, 99, 99, 99, 99,
            ////47, 66, 99, 99, 99, 99, 99, 99,
            ////99, 99, 99, 99, 99, 99, 99, 99,
            ////99, 99, 99, 99, 99, 99, 99, 99,
            ////99, 99, 99, 99, 99, 99, 99, 99,
            ////99, 99, 99, 99, 99, 99, 99, 99
        };

            /// <summary>
            /// Creates a Luma and Chroma Table in ZigZag order using the default quantizers specified in RFC2435
            /// </summary>
            /// <param name="Q">The quality factor</param>
            /// <returns>64 luma bytes and 64 chroma</returns>
            internal static byte[] CreateQuantizationTables(uint type, uint Q, byte precision)
            {
                if (Q >= 100) throw new InvalidOperationException("For Q >= 100, a dynamically defined quantization table is used, which might be specified by a session setup protocol.");

                //Factor restricted to range of 1 and 99
                int factor = (int)Math.Min(Math.Max(1, Q), 99);

                //Seed quantization value
                int q = (Q >= 1 && Q <= 50 ? (int)(5000 / factor) : 200 - factor * 2);

                //Create 2 quantization tables from Seed quality value using the RFC quantizers
                int tableSize = defaultQuantizers.Length / 2;
                byte[] resultTables = new byte[tableSize * 2];
                for (int lumaIndex = 0, chromaIndex = tableSize; lumaIndex < tableSize; ++lumaIndex, ++chromaIndex)
                {
                    //8 Bit tables
                    if (precision == 0)
                    {
                        //Clamp with Min, Max (Should be left in tact but endian is unknown on receiving side)
                        //Luma
                        resultTables[lumaIndex] = (byte)Math.Min(Math.Max((defaultQuantizers[lumaIndex] * q + 50) / 100, 1), byte.MaxValue);
                        //Chroma
                        resultTables[chromaIndex] = (byte)Math.Min(Math.Max((defaultQuantizers[chromaIndex] * q + 50) / 100, 1), byte.MaxValue);
                    }
                    else //16 bit tables
                    {
                        //Luma
                        if (BitConverter.IsLittleEndian)
                            BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)Math.Min(Math.Max((defaultQuantizers[lumaIndex] * q + 50) / 100, 1), byte.MaxValue))).CopyTo(resultTables, lumaIndex++);
                        else
                            BitConverter.GetBytes((ushort)Math.Min(Math.Max((defaultQuantizers[lumaIndex] * q + 50) / 100, 1), byte.MaxValue)).CopyTo(resultTables, lumaIndex++);

                        //Chroma
                        if (BitConverter.IsLittleEndian)
                            BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)Math.Min(Math.Max((defaultQuantizers[chromaIndex] * q + 50) / 100, 1), byte.MaxValue))).CopyTo(resultTables, chromaIndex++);
                        else
                            BitConverter.GetBytes((ushort)Math.Min(Math.Max((defaultQuantizers[chromaIndex] * q + 50) / 100, 1), byte.MaxValue)).CopyTo(resultTables, chromaIndex++);
                    }
                }

                return resultTables;
            }

            /// <summary>
            /// Creates a Jpeg QuantizationTableMarker for each table given in the tables
            /// The precision must be the same for both tables when using this function.
            /// </summary>
            /// <param name="tables">The tables verbatim, either 1 or 2 (Lumiance and Chromiance)</param>
            /// <param name="precisionTable">The byte which indicates which table has 16 bit coeffecients</param>
            /// <returns>The table with marker and prefix and Pq/Tq byte/returns>
            internal static byte[] CreateQuantizationTablesMarkers(ArraySegment<byte> tables, byte precisionTable)
            {
                //List<byte> result = new List<byte>();

                int tableCount = tables.Count / (precisionTable > 0 ? 128 : 64);

                //??Some might have more then 2?
                if (tableCount > 2) throw new ArgumentOutOfRangeException("tableCount");

                int tableSize = tables.Count / tableCount;

                //The len includes the 2 bytes for the length and a single byte for the Lqcd
                byte len = (byte)(tableSize + 3);

                //Each tag is 4 bytes (prefix and tag) + 2 for len = 4 + 1 for Precision and TableId 
                byte[] result = new byte[(5 * tableCount) + (tableSize * tableCount)];

                //Define QTable
                result[0] = JpegMarkers.Prefix;
                result[1] = JpegMarkers.QuantizationTable;

                result[2] = 0;//Len
                result[3] = len;

                //Pq / Tq
                result[4] = (byte)(precisionTable << 4); // Precision and table (id 0 filled by shift)

                //First table. Type - Lumiance usually when two
                System.Array.Copy(tables.Array, tables.Offset, result, 5, tableSize);

                if (tableCount > 1)
                {
                    result[tableSize + 5] = JpegMarkers.Prefix;
                    result[tableSize + 6] = JpegMarkers.QuantizationTable;

                    result[tableSize + 7] = 0;//Len LSB
                    result[tableSize + 8] = len;

                    //Pq / Tq
                    result[tableSize + 9] = (byte)(precisionTable << 4 | 1);//Precision and table Id 1

                    //Second Table. Type - Chromiance usually when two
                    System.Array.Copy(tables.Array, tables.Offset + tableSize, result, 10 + tableSize, tableSize);
                }

                return result;
            }

            //Lumiance

            static byte[] lum_dc_codelens = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };

            static byte[] lum_dc_symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

            static byte[] lum_ac_codelens = { 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d }; //0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };

            static byte[] lum_ac_symbols = 
        {
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
            0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
            0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xa1, 0x08,
            0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0,
            0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0a, 0x16,
            0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2a, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
            0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
            0x7a, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
            0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7,
            0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6,
            0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5,
            0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4,
            0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
            0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea,
            0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
            0xf9, 0xfa
        };

            //Chromiance

            static byte[] chm_dc_codelens = { 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };

            static byte[] chm_dc_symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

            static byte[] chm_ac_codelens = { 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };

            static byte[] chm_ac_symbols = {
            0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
            0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
            0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
            0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0,
            0x15, 0x62, 0x72, 0xd1, 0x0a, 0x16, 0x24, 0x34,
            0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26,
            0x27, 0x28, 0x29, 0x2a, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
            0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            0x79, 0x7a, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96,
            0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5,
            0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4,
            0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3,
            0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2,
            0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
            0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9,
            0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
            0xf9, 0xfa
        };

            internal static byte[] CreateHuffmanTableMarker(byte[] codeLens, byte[] symbols, int tableNo, int tableClass)
            {
                List<byte> result = new List<byte>();
                result.Add(JpegMarkers.Prefix);
                result.Add(JpegMarkers.HuffmanTable);
                result.Add(0x00); //Legnth
                result.Add((byte)(3 + codeLens.Length + symbols.Length)); //Length
                result.Add((byte)((tableClass << 4) | tableNo)); //Id
                result.AddRange(codeLens);//Data
                result.AddRange(symbols);
                return result.ToArray();
            }

            internal static byte[] CreateDataRestartIntervalMarker(ushort dri)
            {
                return new byte[] { JpegMarkers.Prefix, JpegMarkers.DataRestartInterval, 0x00, 0x04, (byte)(dri >> 8), (byte)(dri) };
            }

            #endregion

            #region Constructor

            static RFC2435Frame() { if (JpegCodecInfo == null) throw new NotSupportedException("The system must have a Jpeg Codec installed."); }

            /// <summary>
            /// Creates an empty JpegFrame
            /// </summary>
            public RFC2435Frame() : base(RFC2435Frame.RtpJpegPayloadType) { }

            /// <summary>
            /// Creates a new JpegFrame from an existing RtpFrame which has the JpegFrame PayloadType
            /// </summary>
            /// <param name="f">The existing frame</param>
            public RFC2435Frame(Rtp.RtpFrame f) : base(f) { if (PayloadTypeByte != RFC2435Frame.RtpJpegPayloadType) throw new ArgumentException("Expected the payload type 26, Found type: " + f.PayloadTypeByte); }

            /// <summary>
            /// Creates a shallow copy an existing JpegFrame
            /// </summary>
            /// <param name="f">The JpegFrame to copy</param>
            public RFC2435Frame(RFC2435Frame f) : this((Rtp.RtpFrame)f) { Image = f.ToImage(); }

            /// <summary>
            /// 
            /// </summary>
            /// <summary>
            /// Creates a JpegFrame from a System.Drawing.Image
            /// </summary>
            /// <param name="jpeg">The Image to create a JpegFrame from</param>
            /// <param name="interlaced">A value indicating if the JPEG encoding should be interlaced</param>
            /// <param name="imageQuality">The optional quality to encode the image with, specify a value of 100 to send the quantization tables in band.</param>
            /// <param name="ssrc">The id of the party who is encoding the image</param>
            /// <param name="sequenceNo">The sequence number of the image being encoded</param>
            /// <param name="timeStamp">The Timestamp of the image being encoded</param>
            /// <param name="bytesPerPacketPayload">The maximum amount of octets of each RtpPacket Payload which contains part of the encoded image. This amount should encompass the RtpHeader (12 octets) as well as the Rtp Jpeg Header (8 Octets)</param>
            public static RFC2435Frame Packetize(System.Drawing.Image existing, int imageQuality = 100, bool interlaced = false, int? ssrc = null, int? sequenceNo = 0, long? timeStamp = 0, int bytesPerPacketPayload = 1292)
            {
                if (imageQuality <= 0 || imageQuality > 100) throw new NotSupportedException("Only qualities 1 - 100 are supported");

                System.Drawing.Image image;

                //If the data is larger then supported resize
                if (existing.Width > MaxWidth || existing.Height > MaxHeight)
                {
                    image = existing.GetThumbnailImage(MaxWidth, MaxHeight, null, IntPtr.Zero);
                }
                else //Otherwise use the image itself
                {
                    image = existing;
                }

                //Save the image in Jpeg format and request the PropertyItems from the Jpeg format of the Image
                using (System.IO.MemoryStream temp = new System.IO.MemoryStream(image.Height * image.Width * 3))
                {

                    //Create Encoder Parameters for the Jpeg Encoder
                    System.Drawing.Imaging.EncoderParameters parameters = new System.Drawing.Imaging.EncoderParameters(3);

                    // Set the quality
                    parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)imageQuality);

                    //Set the interlacing
                    if (interlaced)
                    {
                        parameters.Param[1] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.ScanMethod, (long)System.Drawing.Imaging.EncoderValue.ScanMethodInterlaced);

                        parameters.Param[2] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (long)System.Drawing.Imaging.EncoderValue.RenderNonProgressive);
                    }
                    else
                    {
                        parameters.Param[1] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.ScanMethod, (long)System.Drawing.Imaging.EncoderValue.ScanMethodNonInterlaced);

                        parameters.Param[2] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (long)System.Drawing.Imaging.EncoderValue.RenderProgressive);
                    }

                    image.Save(temp, JpegCodecInfo, parameters);

                    return new RFC2435Frame(temp, imageQuality, ssrc, sequenceNo, timeStamp, bytesPerPacketPayload);
                }

            }

            /// <summary>
            /// Creates a <see cref="http://tools.ietf.org/search/rfc2435">RFC2435 Rtp Frame</see> using the given parameters.
            /// </summary>
            /// <param name="jpegData">The stream which contains JPEG formatted data and starts with a StartOfInformation Marker</param>
            /// <param name="qualityFactor">The value to utilize in the RFC2435 Q field, a value >= 100 causes the Quantization Tables to be sent in band.</param>
            /// <param name="ssrc">The optional Id of the media</param>
            /// <param name="sequenceNo">The optional sequence number for the first packet in the frame.</param>
            /// <param name="timeStamp">The optional Timestamp to use for each packet in the frame.</param>
            /// <param name="bytesPerPacketPayload">The amount of bytes each RtpPacket will contain</param>
            public RFC2435Frame(System.IO.Stream jpegData, int? qualityFactor = null, int? ssrc = null, int? sequenceNo = 0, long? timeStamp = 0, int bytesPerPacketPayload = 1292, Common.SourceList sourceList = null)
                : this()
            {

                //Ensure qualityFactor can be stored in a byte
                if (qualityFactor.HasValue && (qualityFactor > byte.MaxValue || qualityFactor == 0))
                    throw Common.Binary.CreateOverflowException("qualityFactor", qualityFactor, 1.ToString(), byte.MaxValue.ToString());

                //Store the constant size which contains the RtpHeader (12) and Jpeg Profile header (8).
                int protocolOverhead = Rtp.RtpHeader.Length + 8, // + 4 * sourceList.Count
                    precisionTableIndex = 0; //The index of the precision table.

                //Ensure data will fit
                if (bytesPerPacketPayload < protocolOverhead) throw new InvalidOperationException("Each RtpPacket in the RtpFrame must contain a RtpHeader (12 octets) as well as the RtpJpeg Header (8 octets).");

                //Set the id of all subsequent packets.
                SynchronizationSourceIdentifier = ssrc ?? 0;

                byte RtpJpegTypeSpecific = 0,  //Type-specific - http://tools.ietf.org/search/rfc2435#section-3.1.1
                    RtpJpegType = 0, //Type - http://tools.ietf.org/search/rfc2435#section-3.1.3
                    Quality = (byte)(qualityFactor ?? 50); //Q - http://tools.ietf.org/search/rfc2435#section-3.1.4

                ushort Width = 0,  //http://tools.ietf.org/search/rfc2435#section-3.1.5
                    Height = 0; //http://tools.ietf.org/search/rfc2435#section-3.1.6

                //The byte which corresponds to the Precision field in the RtpJpeg profile header.
                byte RtpJpegPrecisionTable = 0;

                //The bytes which correspond to the DataRestartInterval of the RtpJpeg profile header (if RestartIntervals are used)
                byte[] RtpJpegRestartInterval = null;

                //Coeffecients which make up the sub-bands used to decode blocks of the jpeg encoded data
                //The lengths of each sub-band is given by the bit in the RtpJpegPrecisionTable 1 = 16 bit (128 byte), 0 = 8 bit (64 byte)
                List<byte> QuantizationTables = new List<byte>();

                //Use a buffered stream around the given stream 
                using (System.IO.BufferedStream temp = new System.IO.BufferedStream(jpegData))
                {
                    //From the beginning of the buffered stream
                    temp.Seek(0, System.IO.SeekOrigin.Begin);

                    //Check for the Start of Information Marker
                    if (temp.ReadByte() != JpegMarkers.Prefix && temp.ReadByte() != JpegMarkers.StartOfInformation)
                        throw new NotSupportedException("Data does not start with Start Of Information Marker");

                    //Check for the End of Information Marker, //If present do not include it.
                    temp.Seek(-1, System.IO.SeekOrigin.End);

                    long endOffset = temp.ReadByte() == JpegMarkers.EndOfInformation ? temp.Length - 2 : temp.Length;

                    //From the beginning of the buffered stream after the Start of Information Marker
                    temp.Seek(2, System.IO.SeekOrigin.Begin);

                    int FunctionCode, //Describes the content of the marker
                        CodeSize, //The lengof the marker segment
                        Ssrc = SynchronizationSourceIdentifier;//Cached Ssrc of all packets.

                    //Variables used for Timestamp of each RtpPacket
                    ushort Timestamp = (ushort)timeStamp,
                        SequenceNo = (ushort)(sequenceNo), //And sequenceNumber
                        Lr = 0, Ri = 0; //Used for RestartIntervals

                    //The current packet consists of a RtpHeader and the encoded payload.
                    //The encoded payload of the first packet will consist of the RtpJpegHeader (8 octets) as well as QTables and coeffecient data releated to the image.
                    Rtp.RtpPacket currentPacket = new Rtp.RtpPacket(new byte[protocolOverhead + (temp.Length < bytesPerPacketPayload ? (int)temp.Length : bytesPerPacketPayload)], 0)
                    {
                        Version = 2,

                        Timestamp = Timestamp,

                        SequenceNumber = SequenceNo++,

                        PayloadType = RFC2435Frame.RtpJpegPayloadType,

                        SynchronizationSourceIdentifier = Ssrc

                        //,SourceList = sourceList
                    };

                    //Where we are in the current packet payload
                    int currentPacketOffset = currentPacket.NonPayloadOctets;

                    //Find a Jpeg Tag while we are not at the end of the stream
                    //Tags come in the format 0xFFXX
                    while ((FunctionCode = temp.ReadByte()) != -1)
                    {
                        //If the prefix is a tag prefix then read another byte as the Tag
                        if (FunctionCode == JpegMarkers.Prefix)
                        {
                            //Get the underlying FunctionCode
                            FunctionCode = temp.ReadByte();

                            //If we are at the end break
                            if (FunctionCode == -1) break;

                            //Ensure not padded
                            if (FunctionCode == JpegMarkers.Prefix) continue;

                            //Last Tag
                            if (FunctionCode == JpegMarkers.EndOfInformation) break;

                            //Read the Marker Length

                            //Read Length Bytes
                            byte h = (byte)temp.ReadByte(), l = (byte)temp.ReadByte();

                            //Calculate Length
                            CodeSize = h * 256 + l;

                            //Correct Length
                            CodeSize -= 2; //Not including their own length

                            //QTables are copied when Quality is > 100
                            if (FunctionCode == JpegMarkers.QuantizationTable && Quality >= 100)
                            {
                                byte compound = (byte)temp.ReadByte();//Read Table Id (And Precision which is in the same byte)

                                //Precision of 1 indicates a 16 bit table 128 bytes per table, 0 indicates 8 bits 64 bytes per table
                                bool precision = compound > 15;

                                //byte tableId = (byte)(compound & 0xf);

                                int tagSizeMinusOne = CodeSize - 1;

                                byte[] table = new byte[tagSizeMinusOne];

                                //Set a bit in the precision table to indicate 16 bit coefficients
                                if (precision) RtpJpegPrecisionTable |= (byte)(1 << precisionTableIndex);

                                //Move the precisionIndex
                                ++precisionTableIndex;

                                //Read the remainder of the data into the table array
                                temp.Read(table, 0, tagSizeMinusOne);

                                //For 16 bit tables, the coefficients are  must be presented in network byte order.
                                if (precision && BitConverter.IsLittleEndian)
                                {
                                    for (int i = 0; i < table.Length - 1; i += 2)
                                    {
                                        byte swap = table[i];
                                        table[i] = table[i + 1];
                                        table[i + 1] = swap;
                                    }
                                }

                                //Add the table array to the table blob
                                QuantizationTables.AddRange(table);
                            }
                            else if (FunctionCode == JpegMarkers.StartOfFrame) //Only the data in the Baseline profile is utilized per RFC2435
                            {
                                //Read the StartOfFrame Marker
                                byte[] data = new byte[CodeSize];
                                temp.Read(data, 0, CodeSize);

                                //@0 - Sample precision – Specifies the precision in bits for the samples of the components in the frame (1)

                                //Y Number of lines [Height] (2)
                                Height = Common.Binary.ReadU16(data, 1, BitConverter.IsLittleEndian);

                                //X Number of lines [Width] (2)
                                Width = Common.Binary.ReadU16(data, 3, BitConverter.IsLittleEndian);

                                //Hi: Horizontal sampling factor – Specifies the relationship between the component horizontal dimension
                                //and maximum image dimension X (see http://www.w3.org/Graphics/JPEG/itu-t81.pdf A.1.1); also specifies the number of horizontal data units of component
                                //Ci in each MCU, when more than one component is encoded in a scan                                                        

                                http://tools.ietf.org/search/rfc2435#section-4.1
                                //Type numbers 2-5 are reserved and SHOULD NOT be used.
                                if (data[7] != 0x21) RtpJpegType |= 1;

                                //Vi: Vertical sampling factor – Specifies the relationship between the component vertical dimension and
                                //maximum image dimension Y (see http://www.w3.org/Graphics/JPEG/itu-t81.pdf A.1.1); also specifies the number of vertical data units of component Ci in
                                //each MCU, when more than one component is encoded in a scan

                                //Unless related to RtpJpegType must be zeroed on transmission and ignored on reception.
                                //RtpJpegTypeSpecific = data[8];
                            }
                            else if (FunctionCode == JpegMarkers.DataRestartInterval) //RestartInterval is copied
                            {
                                #region RFC2435 - Restart Marker Header

                                /*  http://tools.ietf.org/search/rfc2435#section-3.1.7
                             
                                3.1.7.  Restart Marker header

                               This header MUST be present immediately after the main JPEG header
                               when using types 64-127.  It provides the additional information
                               required to properly decode a data stream containing restart markers.

                                0                   1                   2                   3
                                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                               |       Restart Interval        |F|L|       Restart Count       |
                               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                               The Restart Interval field specifies the number of MCUs that appear
                               between restart markers.  It is identical to the 16 bit value that
                               would appear in the DRI marker segment of a JFIF header.  This value
                               MUST NOT be zero.

                               If the restart intervals in a frame are not guaranteed to be aligned
                               with packet boundaries, the F (first) and L (last) bits MUST be set
                               to 1 and the Restart Count MUST be set to 0x3FFF.  This indicates
                               that a receiver MUST reassemble the entire frame before decoding it.

                               To support partial frame decoding, the frame is broken into "chunks"
                               each containing an integral number of restart intervals. The Restart
                               Count field contains the position of the first restart interval in
                               the current "chunk" so that receivers know which part of the frame
                               this data corresponds to.  A Restart Interval value SHOULD be chosen
                               to allow a "chunk" to completely fit within a single packet.  In this
                               case, both the F and L bits of the packet are set to 1.  However, if
                               a chunk needs to be spread across multiple packets, the F bit will be
                               set to 1 in the first packet of the chunk (and only that one) and the
                               L bit will be set to 1 in the last packet of the chunk (and only that
                               one).
                             
                             */

                                #endregion

                                //http://www.w3.org/Graphics/JPEG/itu-t81.pdf
                                //Specifies the length of the parameters in the DRI segment shown in Figure B.9 (see B.1.1.4).
                                Lr = (ushort)(temp.ReadByte() * 256 + temp.ReadByte());

                                Ri = (ushort)(temp.ReadByte() * 256 + temp.ReadByte());

                                //Set the first bit now, set the last bit on the last packet
                                RtpJpegRestartInterval = CreateRtpJpegDataRestartIntervalMarker(Lr, true, false);

                                //Increase RtpJpegType by 64
                                RtpJpegType |= 0x40;
                            }
                            //Last Marker in Header before EntroypEncodedScan
                            else if (FunctionCode == JpegMarkers.StartOfScan)
                            {
                                long pos = temp.Position;

                                byte Ns = (byte)temp.ReadByte();

                                //Read past the Start of Scan (10 more bytes which end with the StartOfSpectral 0x3f00)                            
                                temp.Seek(3 + (Ns * 2), System.IO.SeekOrigin.Current);

                                //temp.Seek(3, System.IO.SeekOrigin.Current);

                                if (pos + CodeSize != temp.Position) throw new Exception("Error in StartOfScan");

                                //Create RtpJpegHeader and CopyTo currentPacket advancing currentPacketOffset
                                //If Quality >= 100 then the QuantizationTableHeader + QuantizationTables also reside here (after any RtpRestartMarker if present).
                                byte[] RtpJpegHeader = CreateRtpJpegHeader(RtpJpegTypeSpecific, 0, RtpJpegType, Quality, Width, Height, RtpJpegRestartInterval, RtpJpegPrecisionTable, QuantizationTables);

                                //Copy the first header
                                RtpJpegHeader.CopyTo(currentPacket.Payload.Array, currentPacket.Payload.Offset + currentPacketOffset);

                                //Advance the offset the size of the profile header.
                                currentPacketOffset += RtpJpegHeader.Length;

                                //Determine how many bytes remanin in the payload after adding the first RtpJpegHeader which also contains the QTables                            
                                int remainingPayloadOctets = bytesPerPacketPayload - currentPacketOffset,
                                    profileHeaderSize = Ri > 0 ? 12 : 8; //Determine if the profile header also contains the RtpRestartMarker

                                //How much remains in the stream relative to the endOffset
                                long streamRemains = endOffset - temp.Position;

                                //Todo if Ri > 0, each packet can only contain 1 Ri to properly support partial decoding
                                //if (Ri > 0)
                                //{
                                //remainingPayloadOctets = //Calculate from RST marker
                                //}

                                //A RtpJpegHeader which must be in the Payload of each Packet (8 Bytes without QTables and RestartInterval)
                                //RtpJpegPrecisionTable is the the same when the same qTables are being used and will not be included when QTables.Count == 0
                                //When Ri > 0 an additional 4 bytes occupy the Payload to represent the RST Marker
                                RtpJpegHeader = RtpJpegHeader.Take(profileHeaderSize).ToArray();

                                //Only the lastPacket contains the marker

                                bytesPerPacketPayload -= RtpJpegHeader.Length - (RtpJpegRestartInterval != null ? 4 : 0);

                                //Todo determine when reading
                                bool lastPacket = false;

                                //While we are not done reading
                                while (streamRemains > 0)
                                {
                                    //Read what we can into the packet
                                    remainingPayloadOctets -= temp.Read(currentPacket.Payload.Array, currentPacket.Payload.Offset + currentPacketOffset, remainingPayloadOctets);

                                    //Update how much remains in the stream
                                    streamRemains = endOffset - temp.Position;

                                    //Add current packet to the frame
                                    Add(currentPacket);

                                    //Remove the reference to the currentPacket added
                                    currentPacket = null;

                                    if (streamRemains <= 0) break;
                                    //Determine if we need to adjust the size and add the packet
                                    else if (streamRemains < bytesPerPacketPayload + protocolOverhead)
                                    {
                                        //8 for the RtpJpegHeader and this will cause the Marker be to set in the next packet created
                                        bytesPerPacketPayload = (int)streamRemains;
                                        lastPacket = true;

                                        //Set the last bit of the Dri Header in the last packet if Ri > 0
                                        if (Ri > 0) RtpJpegHeader[10] ^= (byte)(1 << 7);
                                    }

                                    //Make next packet which consists of a RtpHeader and the remaining remainingPayloadOctets
                                    currentPacket = new Rtp.RtpPacket(new byte[protocolOverhead + bytesPerPacketPayload], 0)
                                    {
                                        Timestamp = Timestamp,
                                        SequenceNumber = SequenceNo++,
                                        SynchronizationSourceIdentifier = Ssrc,
                                        PayloadType = RFC2435Frame.RtpJpegPayloadType,
                                        Marker = lastPacket,
                                        Version = 2
                                    };

                                    //Correct FragmentOffset in the RtpJpegHeader already created.
                                    if (BitConverter.IsLittleEndian) System.Array.Copy(BitConverter.GetBytes(Common.Binary.ReverseU32((uint)temp.Position)), 1, RtpJpegHeader, 1, 3);
                                    else System.Array.Copy(BitConverter.GetBytes((uint)temp.Position), 1, RtpJpegHeader, 1, 3);

                                    //Copy header
                                    RtpJpegHeader.CopyTo(currentPacket.Payload.Array, currentPacket.Payload.Offset);

                                    //Set offset in packet (the length of the RtpJpegHeader)
                                    currentPacketOffset = profileHeaderSize;

                                    //reset the remaning remainingPayloadOctets
                                    remainingPayloadOctets = bytesPerPacketPayload;
                                }
                            }
                            else //Skip past tag 
                            {
                                temp.Seek(CodeSize, System.IO.SeekOrigin.Current);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Fields

            //End result when encoding or decoding is cached in this member
            internal System.Drawing.Image Image;

            internal System.IO.MemoryStream Buffer;

            #endregion

            #region Methods

            /// <summary>
            /// Writes the packets to a memory stream and creates the default header and quantization tables if necessary.
            /// Assigns Image from the result
            /// </summary>
            internal virtual void ProcessPackets(bool allowLegacyPackets = false)
            {

                //if (!Complete) return;

                byte TypeSpecific, Type, Quality;
                ushort Width, Height, RestartInterval = 0, RestartCount = 0;
                uint FragmentOffset;
                //A byte which is bit mapped, each bit indicates 16 bit coeffecients for the table .
                byte PrecisionTable = 0;
                ArraySegment<byte> tables = default(ArraySegment<byte>);

                Buffer = new System.IO.MemoryStream();
                //Loop each packet
                foreach (Rtp.RtpPacket packet in m_Packets.Values)
                {
                    //Payload starts at the offset of the first PayloadOctet
                    int offset = packet.NonPayloadOctets;

                    //if (packet.Extension) throw new NotSupportedException("RFC2035 nor RFC2435 defines extensions.");

                    //Decode RtpJpeg Header

                    TypeSpecific = (packet.Payload.Array[packet.Payload.Offset + offset++]);
                    FragmentOffset = (uint)(packet.Payload.Array[packet.Payload.Offset + offset++] << 16 | packet.Payload.Array[packet.Payload.Offset + offset++] << 8 | packet.Payload.Array[packet.Payload.Offset + offset++]);

                    #region RFC2435 -  The Type Field

                    /*
                     4.1.  The Type Field

   The Type field defines the abbreviated table-specification and
   additional JFIF-style parameters not defined by JPEG, since they are
   not present in the body of the transmitted JPEG data.

   Three ranges of the type field are currently defined. Types 0-63 are
   reserved as fixed, well-known mappings to be defined by this document
   and future revisions of this document. Types 64-127 are the same as
   types 0-63, except that restart markers are present in the JPEG data
   and a Restart Marker header appears immediately following the main
   JPEG header. Types 128-255 are free to be dynamically defined by a
   session setup protocol (which is beyond the scope of this document).

   Of the first group of fixed mappings, types 0 and 1 are currently
   defined, along with the corresponding types 64 and 65 that indicate
   the presence of restart markers.  They correspond to an abbreviated
   table-specification indicating the "Baseline DCT sequential" mode,
   8-bit samples, square pixels, three components in the YUV color
   space, standard Huffman tables as defined in [1, Annex K.3], and a
   single interleaved scan with a scan component selector indicating
   components 1, 2, and 3 in that order.  The Y, U, and V color planes
   correspond to component numbers 1, 2, and 3, respectively.  Component
   1 (i.e., the luminance plane) uses Huffman table number 0 and
   quantization table number 0 (defined below) and components 2 and 3
   (i.e., the chrominance planes) use Huffman table number 1 and
   quantization table number 1 (defined below).

   Type numbers 2-5 are reserved and SHOULD NOT be used.  Applications
   based on previous versions of this document (RFC 2035) should be
   updated to indicate the presence of restart markers with type 64 or
   65 and the Restart Marker header.

   The two RTP/JPEG types currently defined are described below:

                            horizontal   vertical   Quantization
           types  component samp. fact. samp. fact. table number
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |       |  1 (Y)  |     2     |     1     |     0     |
         | 0, 64 |  2 (U)  |     1     |     1     |     1     |
         |       |  3 (V)  |     1     |     1     |     1     |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |       |  1 (Y)  |     2     |     2     |     0     |
         | 1, 65 |  2 (U)  |     1     |     1     |     1     |
         |       |  3 (V)  |     1     |     1     |     1     |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   These sampling factors indicate that the chrominance components of
   type 0 video is downsampled horizontally by 2 (often called 4:2:2)
   while the chrominance components of type 1 video are downsampled both
   horizontally and vertically by 2 (often called 4:2:0).

   Types 0 and 1 can be used to carry both progressively scanned and
   interlaced image data.  This is encoded using the Type-specific field
   in the main JPEG header.  The following values are defined:

      0 : Image is progressively scanned.  On a computer monitor, it can
          be displayed as-is at the specified width and height.

      1 : Image is an odd field of an interlaced video signal.  The
          height specified in the main JPEG header is half of the height
          of the entire displayed image.  This field should be de-
          interlaced with the even field following it such that lines
          from each of the images alternate.  Corresponding lines from
          the even field should appear just above those same lines from
          the odd field.

      2 : Image is an even field of an interlaced video signal.

      3 : Image is a single field from an interlaced video signal, but
          it should be displayed full frame as if it were received as
          both the odd & even fields of the frame.  On a computer
          monitor, each line in the image should be displayed twice,
          doubling the height of the image.
                     */

                    #endregion

                    Type = (packet.Payload.Array[packet.Payload.Offset + offset++]);

                    //Check for a RtpJpeg Type of less than 5 used in RFC2035 for which RFC2435 is the errata
                    if (!allowLegacyPackets && Type >= 2 && Type <= 5)
                    {
                        //Should allow for 2035 decoding seperately
                        throw new ArgumentException("Type numbers 2-5 are reserved and SHOULD NOT be used.  Applications based on RFC 2035 should be updated to indicate the presence of restart markers with type 64 or 65 and the Restart Marker header.");
                    }

                    Quality = packet.Payload.Array[packet.Payload.Offset + offset++];
                    Width = (ushort)(packet.Payload.Array[packet.Payload.Offset + offset++] * 8);// in 8 pixel multiples
                    Height = (ushort)(packet.Payload.Array[packet.Payload.Offset + offset++] * 8);// in 8 pixel multiples
                    //It is worth noting Rtp does not care what you send and more tags such as comments and or higher resolution pictures may be sent and these values will simply be ignored.

                    //Restart Interval 64 - 127
                    if (Type > 63 && Type < 128)
                    {
                        /*
                           This header MUST be present immediately after the main JPEG header
                           when using types 64-127.  It provides the additional information
                           required to properly decode a data stream containing restart markers.

                            0                   1                   2                   3
                            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                           |       Restart Interval        |F|L|       Restart Count       |
                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                         */
                        RestartInterval = (ushort)(packet.Payload.Array[packet.Payload.Offset + offset++] << 8 | packet.Payload.Array[packet.Payload.Offset + offset++]);
                        RestartCount = (ushort)((packet.Payload.Array[packet.Payload.Offset + offset++] << 8 | packet.Payload.Array[packet.Payload.Offset + offset++]) & 0x3fff);
                    }

                    // A Q value of 255 denotes that the  quantization table mapping is dynamic and can change on every frame.
                    // Decoders MUST NOT depend on any previous version of the tables, and need to reload these tables on every frame.
                    if (FragmentOffset == 0 /*Buffer.Position == 0*/)
                    {

                        //RFC2435 http://tools.ietf.org/search/rfc2435#section-3.1.8
                        //3.1.8.  Quantization Table header
                        /*
                         This header MUST be present after the main JPEG header (and after the
                            Restart Marker header, if present) when using Q values 128-255.  It
                            provides a way to specify the quantization tables associated with
                            this Q value in-band.
                         */
                        if (Quality == 0) throw new InvalidOperationException("(Q)uality = 0 is Reserved.");
                        else if (Quality >= 100)
                        {

                            /* http://tools.ietf.org/search/rfc2435#section-3.1.8
                             * Quantization Table Header
                             * -------------------------
                             0                   1                   2                   3
                             0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |      MBZ      |   Precision   |             Length            |
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |                    Quantization Table Data                    |
                            |                              ...                              |
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                             */

                            if ((packet.Payload.Array[packet.Payload.Offset + offset++]) != 0)
                            {
                                //Must Be Zero is Not Zero
                                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                            }

                            //Read the PrecisionTable (notes below)
                            PrecisionTable = (packet.Payload.Array[packet.Payload.Offset + offset++]);

                            #region RFC2435 Length Field

                            /*
                                 
                                    The Length field is set to the length in bytes of the quantization
                                    table data to follow.  The Length field MAY be set to zero to
                                    indicate that no quantization table data is included in this frame.
                                    See section 4.2 for more information.  If the Length field in a
                                    received packet is larger than the remaining number of bytes, the
                                    packet MUST be discarded.

                                    When table data is included, the number of tables present depends on
                                    the JPEG type field.  For example, type 0 uses two tables (one for
                                    the luminance component and one shared by the chrominance
                                    components).  Each table is an array of 64 values given in zig-zag
                                    order, identical to the format used in a JFIF DQT marker segment.

                             * PrecisionTable *
                             
                                    For each quantization table present, a bit in the Precision field
                                    specifies the size of the coefficients in that table.  If the bit is
                                    zero, the coefficients are 8 bits yielding a table length of 64
                                    bytes.  If the bit is one, the coefficients are 16 bits for a table
                                    length of 128 bytes.  For 16 bit tables, the coefficients are
                                    presented in network byte order.  The rightmost bit in the Precision
                                    field (bit 15 in the diagram above) corresponds to the first table
                                    and each additional table uses the next bit to the left.  Bits beyond
                                    those corresponding to the tables needed by the type in use MUST be
                                    ignored.
                                 
                                 */

                            #endregion

                            //Length of all tables
                            ushort Length = (ushort)(packet.Payload.Array[packet.Payload.Offset + offset++] << 8 | packet.Payload.Array[packet.Payload.Offset + offset++]);

                            //If there is Table Data Read it from the payload, Length should never be larger than 128 * tableCount
                            if (Length == 0 && Quality == byte.MaxValue) throw new InvalidOperationException("RtpPackets MUST NOT contain Q = 255 and Length = 0.");
                            else if (Length > packet.Payload.Count - offset) //If the indicated length is greater than that of the packet taking into account the offset
                                continue; // The packet must be discarded

                            //Copy the tables present
                            tables = new ArraySegment<byte>(packet.Payload.Array, packet.Payload.Offset + offset, (int)Length);
                            offset += (int)Length;
                        }
                        else // Create them from the given Quality parameter ** Duality (Unify Branch)
                        {
                            tables = new ArraySegment<byte>(CreateQuantizationTables(Type, Quality, PrecisionTable));
                        }

                        //Write the JFIF Header after reading or generating the QTables
                        byte[] header = CreateJFIFHeader(Type, Width, Height, tables, PrecisionTable, RestartInterval);
                        Buffer.Write(header, 0, header.Length);
                    }

                    //Write the Payload data from the offset
                    Buffer.Write(packet.Payload.Array, packet.Payload.Offset + offset, packet.Payload.Count - (offset + packet.PaddingOctets));
                }

                //Check for EOI Marker and write if not found
                if (Buffer.Position == Buffer.Length)
                {
                    Buffer.Seek(-1, System.IO.SeekOrigin.Current);
                    if (Buffer.ReadByte() != JpegMarkers.EndOfInformation)
                    {
                        Buffer.WriteByte(JpegMarkers.Prefix);
                        Buffer.WriteByte(JpegMarkers.EndOfInformation);
                    }
                }

                //Create the Image form the Buffer
                Image = System.Drawing.Image.FromStream(Buffer);
            }
            /// <summary>
            /// Creates a image from the processed packets in the memory stream
            /// </summary>
            /// <returns>The image created from the packets</returns>
            public System.Drawing.Image ToImage()
            {
                try
                {
                    if (Image == null) ProcessPackets();
                    if (Image != null) return Image.Clone() as System.Drawing.Image;
                    return null;
                }
                catch
                {
                    throw;
                }
            }

            public override void Dispose()
            {
                //Dispose only the Image
                DisposeImage();

                if (Buffer != null)
                {
                    Buffer.Dispose();
                    Buffer = null;
                }

                //Call dispose on the base class
                base.Dispose();
            }

            internal void DisposeImage()
            {
                if (Image != null)
                {
                    Image.Dispose();
                    Image = null;
                }
            }

            /// <summary>
            /// Removing All Packets in a JpegFrame destroys any Image associated with the Frame
            /// </summary>
            public override void RemoveAllPackets()
            {
                DisposeImage();
                base.RemoveAllPackets();
            }

            public override Rtp.RtpPacket Remove(int sequenceNumber)
            {
                DisposeImage();
                return base.Remove(sequenceNumber);
            }

            #region Jpeg

            //Maybe
            //public byte[] GetQuantizationTable(int index) { }
            //....

            //Allow conversion to and from 8/16 bit
            //Allow setting of QTables

            #endregion

            #endregion

            #region Operators

            public static implicit operator System.Drawing.Image(RFC2435Frame f) { return f.ToImage(); }

            public static implicit operator RFC2435Frame(System.Drawing.Image f) { return Packetize(f); }

            #endregion
        }

        #endregion

        #region Fields

        //Should be moved to SourceStream? Should have Fps and calculate for developers?
        protected int clockRate = 9;//kHz //90 dekahertz

        //Should be moved to SourceStream?
        protected readonly int sourceId = (int)DateTime.UtcNow.Ticks;

        protected Queue<Rtp.RtpFrame> m_Frames = new Queue<Rtp.RtpFrame>();

        //RtpClient so events can be sourced to Clients through RtspServer
        protected Rtp.RtpClient m_RtpClient;

        //Watches for files if given in constructor
        protected System.IO.FileSystemWatcher m_Watcher;

        protected int m_FramesPerSecondCounter = 0;

        #endregion

        #region Propeties

        public virtual double FramesPerSecond { get { return Math.Max(m_FramesPerSecondCounter, 1) / Math.Abs(Uptime.TotalSeconds); } }

        /// <summary>
        /// Implementes the SessionDescription property for RtpSourceStream
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return m_RtpClient; } }

        #endregion

        #region Constructor

        public RFC2435Stream(string name, string directory = null, bool watch = true)
            : base(name, new Uri("file://" + System.IO.Path.GetDirectoryName(directory)))
        {
            //If we were told to watch and given a directory and the directory exists then make a FileSystemWatcher
            if (System.IO.Directory.Exists(base.Source.LocalPath) && watch)
            {
                m_Watcher = new System.IO.FileSystemWatcher(base.Source.LocalPath);
                m_Watcher.EnableRaisingEvents = true;
                m_Watcher.NotifyFilter = System.IO.NotifyFilters.CreationTime;
                m_Watcher.Created += FileCreated;
            }            
        }

        #endregion

        #region Methods

        //SourceStream Implementation
        public override void Start()
        {
            if (m_RtpClient != null) return;

            //Create a RtpClient so events can be sourced from the Server to many clients without this Client knowing about all participants
            //If this class was used to send directly to one person it would be setup with the recievers address
            m_RtpClient = Rtp.RtpClient.Sender(System.Net.IPAddress.Any);

            SessionDescription = new Sdp.SessionDescription(1, "v√ƒ", Name );
            SessionDescription.Add(new Sdp.Lines.SessionConnectionLine()
            {
                NetworkType = "IN",
                AddressType = "*",
                Address = "0.0.0.0"
            });

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, RtpSource.RtpMediaProtocol, RFC2435Stream.RFC2435Frame.RtpJpegPayloadType));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));

            //Add the line with the clock rate in ms, obtained by TimeSpan.TicksPerMillisecond * clockRate            

            //Make the thread
            m_RtpClient.m_WorkerThread = new System.Threading.Thread(SendPackets);
            m_RtpClient.m_WorkerThread.TrySetApartmentState(System.Threading.ApartmentState.MTA);
            m_RtpClient.m_WorkerThread.IsBackground = true;
            m_RtpClient.m_WorkerThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            m_RtpClient.m_WorkerThread.Name = "RFC2435Stream-" + Id;

            //If we are watching and there are already files in the directory then add them to the Queue
            if (m_Watcher != null && !string.IsNullOrWhiteSpace(base.Source.LocalPath) && System.IO.Directory.Exists(base.Source.LocalPath))
            {
                foreach (string file in System.IO.Directory.GetFiles(base.Source.LocalPath, "*.jpg").AsParallel())
                {
                    try
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Encoding: " + file);
#endif
                        //Packetize the Image adding the resulting Frame to the Queue (Encoded implicitly with operator)
                        Packetize(System.Drawing.Image.FromFile(file));
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Done Encoding: " + file);
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Exception: " + ex);
#endif
                        continue;
                    }
                }

                //If we have not been stopped already
                if (/*State != StreamState.Started && */ m_RtpClient.m_WorkerThread != null)
                {
                    //Only ready after all pictures are in the queue
                    Ready = true;
                    m_RtpClient.m_WorkerThread.Start();
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Started");
#endif
            }
            else
            {
                //We are ready
                Ready = true;
                m_RtpClient.m_WorkerThread.Start();
            }
            base.Start();
        }

        public override void Stop()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Stopped");
#endif

            Ready = false;

            Utility.Abort(ref m_RtpClient.m_WorkerThread);

            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Created -= FileCreated;
                m_Watcher.Dispose();
                m_Watcher = null;
            }

            if (m_RtpClient != null)
            {
                m_RtpClient.Disconnect();
                m_RtpClient = null;
            }

            m_Frames.Clear();

            SessionDescription = null;

            base.Stop();
        }

        /// <summary>
        /// Called to add a file to the Queue when it was created in the watched directory if the file was an Image.
        /// </summary>
        /// <param name="sender">The object who called this method</param>
        /// <param name="e">The FileSystemEventArgs which correspond to the file created</param>
        internal virtual void FileCreated(object sender, System.IO.FileSystemEventArgs e)
        {
            string path = e.FullPath.ToLowerInvariant();
            if (path.EndsWith("bmp") || path.EndsWith("jpg") || path.EndsWith("jpeg") || path.EndsWith("gif") || path.EndsWith("png") || path.EndsWith("emf") || path.EndsWith("exif") || path.EndsWith("gif") || path.EndsWith("ico") || path.EndsWith("tiff") || path.EndsWith("wmf"))
            {
                try { Packetize(System.Drawing.Image.FromFile(path)); }
                catch { throw; }
            }
        }

        /// <summary>
        /// Add a frame of existing packetized data
        /// </summary>
        /// <param name="frame">The frame with packets to send</param>
        public void AddFrame(Rtp.RtpFrame frame)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(frame); }
                catch { throw; }
            }
        }

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        /// <param name="quality">The quality of the encoded image, 100 specifies the quantization tables are sent in band</param>
        public virtual void Packetize(System.Drawing.Image image, int quality = 80, bool interlaced = false)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(RFC2435Stream.RFC2435Frame.Packetize(image, quality, interlaced, (int)sourceId)); }
                catch { throw; }
            }
        }

        //Needs to only send packets and not worry about updating the frame, that should be done by ImageSource

        internal override void SendPackets()
        {
            while (State == StreamState.Started)
            {
                try
                {
                    if (m_Frames.Count == 0)
                    {
                        System.Threading.Thread.Sleep(clockRate);
                        continue;
                    }

                    int period = (clockRate * 1000 / m_Frames.Count);

                    //Dequeue a frame or die
                    Rtp.RtpFrame frame = m_Frames.Dequeue();

                    //Get the transportChannel for the packet
                    Rtp.RtpClient.TransportContext transportContext = RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier);

                    if (transportContext != null)
                    {

                        DateTime now = DateTime.UtcNow;

                        //transportContext.RtpTimestamp += (uint)(clockRate * 1000 / (m_Frames.Count + 1));

                        transportContext.RtpTimestamp += period;

                        //transportContext.RtpTimestamp = (uint)(now.Ticks / TimeSpan.TicksPerMillisecond * clockRate);

                        //Iterate each packet and put it into the next frame (Todo In clock cycles)
                        //Again nothing to much to gain here in terms of parallelism (unless you want multiple pictures in the same buffer on the client)
                        foreach (Rtp.RtpPacket packet in frame)
                        {
                            //Copy the values before we signal the server
                            //packet.Channel = transportContext.DataChannel;
                            packet.SynchronizationSourceIdentifier = (int)sourceId;
                            packet.Timestamp = (int)transportContext.RtpTimestamp;

                            //Increment the sequence number on the transportChannel and assign the result to the packet
                            packet.SequenceNumber = ++transportContext.SequenceNumber;

                            //Fire an event so the server sends a packet to all clients connected to this source
                            RtpClient.OnRtpPacketReceieved(packet);
                        }

                        if (DecodeFrames && frame.PayloadTypeByte == 26) OnFrameDecoded((RFC2435Stream.RFC2435Frame)frame);

                        System.Threading.Interlocked.Increment(ref m_FramesPerSecondCounter);
                    }

                    //If we are to loop images then add it back at the end
                    if (Loop)
                    {
                        m_Frames.Enqueue(frame);
                    }

                    System.Threading.Thread.Sleep(clockRate);
                        
                }
                catch (OverflowException)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Source " + Id + " Overflow");
#endif
                    //m_FramesPerSecondCounter overflowed, take a break
                    System.Threading.Thread.Sleep(0);
                    continue;
                }
                catch (Exception ex)
                {
                    if (ex is System.Threading.ThreadAbortException) return;
                    continue;
                }
            }
        }

        #endregion
    }

    //public sealed class ChildRtpImageSource : ChildStream
    //{
    //    public ChildRtpImageSource(RtpImageSource source) : base(source) { }
    //}
}