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

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Sends System.Drawing.Images over Rtp by encoding them as a RFC2435 Jpeg
    /// </summary>
    public class RFC2435Media : RtpSink
    {

        #region NestedTypes

        /// <summary>
        /// Implements RFC2435
        /// Encodes from a System.Drawing.Image to a RFC2435 Jpeg.
        /// Decodes a RFC2435 Jpeg to a System.Drawing.Image.
        ///  <see cref="http://tools.ietf.org/rfc/rfc2435.txt">RFC 2435</see>        
        ///  <see cref="http://en.wikipedia.org/wiki/JPEG"/>
        /// </summary>
        public class RFC2435Frame : Rtp.RtpFrame
        {
            #region Statics

            //Since the Frame type is overridden it only makes sense to adhere to some type of underlying interface which can also optionally provide the information about the profile.

            //This can be done via explicit interface implementation to keep the exposed implementation minimal.

            //e.g. IRFC2435Frame could inherit IRtpFrame and add certain fields.

            //A frame when looked at as a frame would have the API it has now 
            //When looked at as a IRtpProfileInformation it can have additional information


            //Probably not the final API as it smells like Java too much
            internal protected static class ProfileHeaderInformation //: RtpFrame.ProfileHeaderInformation
            {
                internal const bool HasVariableProfileHeaderSize = true;

                internal const int DataRestartIntervalHeaderSize = 4;

                internal const int QuantizationTableHeaderSize = 4; // + QTable Length

                internal const int MinimumProfileHeaderSize = 8; //+ 4 for the DRI if present, + 4 + QTable Length

                //Field name and size
                //static Dictionary<string, int> NamedFields = new Dictionary<string, int>() //Bits
                //{
                //    {"TypeSpecific", 8},
                //    {"FragmentOffset", 24},
                //    {"Type", 8},
                //    {"Q", 8}, //Quality
                //    {"Width", 8}, 
                //    {"Height", 8}, 
                //    //
                //    {"RestartInterval", 16}, 
                //    {"First", 1}, 
                //    {"Last", 1}, 
                //    {"RestartCount", 14}, 
                //    //
                //    {"MBZ", 8}, 
                //    {"Precision", 8}, 
                //    {"Length", 16}, 
                //    //  Quantization Table Data != 0
                //};

                public static int GetProfileHeaderSize(Rtp.RtpPacket packet)
                {
                    if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return 0;

                    int offset = packet.Payload.Offset + packet.HeaderOctets;

                    int result = MinimumProfileHeaderSize;

                    byte Type = packet.Payload.Array[offset + 4];

                    byte Q = packet.Payload.Array[offset + 5];

                    //DRI Present
                    if (Type > 63 && Type < 128) result += DataRestartIntervalHeaderSize;

                    //When FragmentOffset == 0 there is no QTables present.
                    if (false == (Common.Binary.ReadU24(packet.Payload.Array, offset, BitConverter.IsLittleEndian) == 0)) return result;

                                                                                                                 //Lookup
                    //Common.Binary.ReadBitsMSB(packet.Payload.Array, Common.Binary.BytesToBits(ref offset), NamedFields["FragmentOffset"]);

                    //Include the Qtables header itself.
                    result += QuantizationTableHeaderSize; 

                    //Read the length
                    if (Q >= 100) result += Common.Binary.Read16(packet.Payload.Array, offset + result, BitConverter.IsLittleEndian);

                                                                                                                 //Lookup
                    //Common.Binary.ReadBitsMSB(packet.Payload.Array, Common.Binary.BytesToBits(ref offset), NamedFields["Length"]);

                    return result;
                }

                //GetField -> base class

                //public static int GetType(Rtp.RtpPacket packet)
                //{

                //}
            }

            const int JpegMaxSizeDimension = 65500; //65535

            //public const int MaxWidth = 2048;

            //public const int MaxHeight = 4096;

            //RFC2435 Section 3.1.4 and 3.1.5

            public const int MaxWidth = 2040;

            public const int MaxHeight = 2040;            

            public const byte RtpJpegPayloadType = 26;

            internal static System.Drawing.Imaging.ImageCodecInfo JpegCodecInfo = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders().First(d => d.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="frame"></param>
            /// <param name="tables"></param>
            /// <param name="precision"></param>
            /// <returns></returns>
            public static bool GetQuantizationTables(RFC2435Frame frame, out IEnumerable<byte> tables, out byte precision, bool legacy = false)
            {
                //If the frame is null or empty
                if (Common.IDisposedExtensions.IsNullOrDisposed(frame) || frame.IsEmpty)
                {
                    precision = 0;

                    //No tables present.
                    tables = Common.MemorySegment.Empty;

                    //indicate failure
                    return false;
                }

                //Get the first packet.
                var packet = frame.Packets.First();

                //Skip Type-specific
                int offset = packet.Payload.Offset + packet.HeaderOctets + 1, end = packet.Payload.Count - packet.PaddingOctets;

                //The FragmentOffset must be 0 at the start of a new frame.
                if (end - offset < 8 || Common.Binary.ReadU24(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian) != 0)
                {
                    precision = 0;

                    //No tables present.
                    tables = Common.MemorySegment.Empty;

                    //indicate failure
                    return false;
                }

                uint Type = packet.Payload.Array[offset++];

                uint Quality = packet.Payload.Array[offset++];

                //Q = 0 is reserved.
                if (Quality == 0)
                {
                    precision = 0;

                    //no tables are present
                    tables = Common.MemorySegment.Empty;

                    //indicate failure
                    return false;

                }
                else if (Quality <= (legacy ? 99 : 127)) //Only present for values >= 128 (RFC2035 values >= 100, 0 was reserved and values 100 - 127 were not specified)
                {
                    precision = 0;

                    //use the default tables
                    tables = new Common.MemorySegment(CreateQuantizationTables(Type, Quality, 0, true));

                    //indicate failure
                    return false;
                }

                //Skip Width and Height, .... could possibly be 0....
                offset += 2;

                //Skip DataRestartInterval data
                if (Type > 63 && Type < 128) offset += 4;

                //Must be zero is not 0, this means there is an alignment issue and we should NOT attempt to read the rest of the Quantization table header.
                if (end - offset < 4 ||  (packet.Payload.Array[offset++]) != 0)
                {
                    precision = 0;

                    //no tables are present
                    tables = Common.MemorySegment.Empty;

                    //indicate failure
                    return false;
                }

                //read the precision byte
                precision = packet.Payload.Array[offset++];

                //Length of all tables
                ushort Length = Common.Binary.ReadU16(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian); //(ushort)(packet.Payload.Array[offset++] << 8 | packet.Payload.Array[offset++]);

                //If there is Table Data Read it from the payload, Length should never be larger than 128 * tableCount
                if (Length == 0 && Quality == byte.MaxValue/* && false == allowQualityLengthException*/) throw new InvalidOperationException("RtpPackets MUST NOT contain Q = 255 and Length = 0.");
                else if (Length == 0 || Length > end - offset)
                {
                    //If the indicated length is greater than that of the packet taking into account the offset and padding
                    //Or
                    //The length is 0

                    //Use default tables
                    tables = new Common.MemorySegment(CreateQuantizationTables(Type, Quality, precision, true));

                    return false;
                }

                //assign the tables present
                tables = new Common.MemorySegment(packet.Payload.Array, offset, (int)Length);

                //indicate success
                return true;
            }

            /// <summary>
            /// Creates RST header for JPEG/RTP packet.
            /// </summary>
            /// <param name="dri">dri Restart interval - number of MCUs between restart markers</param>
            /// <param name="f">optional first bit (defaults to 1)</param>
            /// <param name="l">optional last bit (defaults to 1)</param>
            /// <param name="count">optional number of restart markers (defaults to 0x3FFF)</param>
            /// <returns>Rst Marker</returns>
            [CLSCompliant(false)]
            public static byte[] CreateRtpJpegDataRestartIntervalMarker(ushort dri, bool f = true, bool l = true, ushort count = 0x3FFF)
            {
                //     0                   1                   2                   3
                //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                //|       Restart Interval        |F|L|       Restart Count       |
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                byte[] data = new byte[4];
                data[0] = (byte)((dri >> 8) & 0xFF);
                data[1] = (byte)dri;

                //Network ByteOrder            

                Media.Common.Binary.Write16(data, 2, BitConverter.IsLittleEndian, count);

                if (f) data[2] = (byte)((1) << 7);

                if (l) data[2] |= (byte)((1) << 6);

                return data;
            }

            public static byte[] CreateRtpJpegDataRestartIntervalMarker(short dri, bool f = true, bool l = true, short count = 0x3FFF)
            {
                return CreateRtpJpegDataRestartIntervalMarker((ushort)dri, f, l, (ushort)count);
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
            public static byte[] CreateRtpJpegHeader(int typeSpecific, long fragmentOffset, int jpegType, int quality, int width, int height, byte[] dri, byte precisionTable, List<byte> qTables)
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

                //Common.Binary.WriteNetwork24()
                //Common.Binary.GetBytes(fragmentOffset, BitConverter.IsLittleEndian)

                if (BitConverter.IsLittleEndian) fragmentOffset = Common.Binary.ReverseU32((uint)fragmentOffset);

                Media.Common.Extensions.List.ListExtensions.AddRange(RtpJpegHeader, BitConverter.GetBytes((uint)fragmentOffset), 1, 3);


                //(Jpeg)Type
                //http://tools.ietf.org/search/rfc2435#section-3.1.3
                RtpJpegHeader.Add((byte)jpegType);

                //http://tools.ietf.org/search/rfc2435#section-3.1.4 (Q)
                RtpJpegHeader.Add((byte)quality);

                //http://tools.ietf.org/search/rfc2435#section-3.1.5 (Width)
                RtpJpegHeader.Add((byte)(((width+7)&~7) >> 3));

                //http://tools.ietf.org/search/rfc2435#section-3.1.6 (Height)
                RtpJpegHeader.Add((byte)(((height+7)&~7) >> 3));

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
                    if (quality >= 128)
                    {
                        int qTablesCount = qTables.Count;

                        //Check for a table
                        if (quality == byte.MaxValue && qTablesCount == 0) throw new InvalidOperationException("Packets MUST NOT contain Q = 255 and Length = 0.");

                        //Check for overflow
                        if (qTablesCount > ushort.MaxValue) Common.Binary.CreateOverflowException("qTables", qTablesCount, ushort.MinValue.ToString(), ushort.MaxValue.ToString());

                        RtpJpegHeader.Add(0); //Must Be Zero      

                        RtpJpegHeader.Add(precisionTable);//PrecisionTable may be bit flagged to indicate 16 bit tables

                        //Add the Length field
                        if (BitConverter.IsLittleEndian) RtpJpegHeader.AddRange(BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)qTablesCount)));
                        else RtpJpegHeader.AddRange(BitConverter.GetBytes((ushort)qTablesCount));

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
            internal static byte[] CreateJPEGHeaders(byte typeSpec, byte jpegType, uint width, uint height, Common.MemorySegment tables, byte precision, ushort dri) //bool jfif
            {
                List<byte> result = new List<byte>();

                int tablesCount = tables.Count;

                result.Add(Media.Codecs.Image.Jpeg.Markers.Prefix);
                result.Add(Media.Codecs.Image.Jpeg.Markers.StartOfInformation);//SOI      
          
                //JFIF marker should be included here if jfif header is required. (16 more bytes)
                
                //JFXX marker would have thumbnail but is not required

                //Quantization Tables (if needed, pass an empty tables segment to omit)
                if(tables.Count > 0) result.AddRange(CreateQuantizationTableMarkers(tables, precision));

                //Data Restart Invertval
                if (dri > 0) result.AddRange(CreateDataRestartIntervalMarker(dri));

                //Start Of Frame

                /*
                   BitsPerSample / ColorComponents (1)
                   EncodingProcess	(1)
                 * Possible Values
                        0xc0 = Baseline DCT, Huffman coding 
                        0xc1 = Extended sequential DCT, Huffman coding 
                        0xc2 = Progressive DCT, Huffman coding 
                        0xc3 = Lossless, Huffman coding 
                 *      0xc4 = Huffman Table.
                        0xc5 = Sequential DCT, differential Huffman coding 
                        0xc6 = Progressive DCT, differential Huffman coding 
                        0xc7 = Lossless, Differential Huffman coding 
                 *      0xc8 = Extension
                        0xc9 = Extended sequential DCT, arithmetic coding 
                        0xca = Progressive DCT, arithmetic coding 
                        0xcb = Lossless, arithmetic coding 
                 *      0xcc =  DAC   = 0xcc,   define arithmetic-coding conditioning
                        0xcd = Sequential DCT, differential arithmetic coding 
                        0xce = Progressive DCT, differential arithmetic coding 
                        0xcf = Lossless, differential arithmetic coding
                 *      0xf7 = JPEG-LS Start Of Frame
                    ImageHeight	(2)
                    ImageWidth	(2) 
                    YCbCrSubSampling	(1)
                 * Possible Values
                        '1 1' = YCbCr4:4:4 (1 1) 
                        '1 2' = YCbCr4:4:0 (1 2) 
                        '1 4' = YCbCr4:4:1 (1 4) 
                        '2 1' = YCbCr4:2:2 (2 1) 
                        '2 2' = YCbCr4:2:0 (2 2) 
                        '2 4' = YCbCr4:2:1 (2 4) 
                        '4 1' = YCbCr4:1:1 (4 1) 
                        '4 2' = YCbCr4:1:0 (4 2)
                 */

                //Need a progrssive indication, problem is that CMYK and RGB also use that indication
                bool progressive = false; /* = typeSpec == Media.Codecs.Image.Jpeg.Markers.StartOfProgressiveFrame;
                if (progressive) typeSpec = 0;*/

                result.Add(Media.Codecs.Image.Jpeg.Markers.Prefix);

                //This is not soley based on progressive or not, this needs to include more types based on what is defined (above)
                if(progressive)
                    result.Add(Media.Codecs.Image.Jpeg.Markers.StartOfProgressiveFrame);//SOF
                else
                    result.Add(Media.Codecs.Image.Jpeg.Markers.StartOfBaselineFrame);//SOF

                //Todo properly build headers?
                //If only 1 table (AND NOT PROGRESSIVE)
                if(tablesCount == 64 && false == progressive)
                {
                    result.Add(0x00); //Length
                    result.Add(0x0b); //
                    result.Add(0x08); //Bits Per Components and EncodingProcess

                    result.Add((byte)(height >> 8)); //Height
                    result.Add((byte)height);

                    result.Add((byte)(width >> 8)); //Width
                    result.Add((byte)width);

                    result.Add(0x01); //Number of components
                    result.Add(0x00); //Component Number
                    result.Add((byte)(typeSpec > 0 ? typeSpec : 0x11)); //Horizontal Sampling Factor
                    result.Add(0x00); //Matrix Number
                }
                else
                {
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
                    result.Add((byte)((jpegType & 1) == 0 ? 0x21 : 0x22));

                    result.Add(0x00);//Matrix Number (Quant Table Id)?
                    result.Add(0x02);//Component Number

                    result.Add((byte)(typeSpec > 0 ? typeSpec : 0x11));//Horizontal or Vertical Sample 

                    result.Add((byte)(tablesCount == 64 ? 0x00 : 0x01));//Matrix Number

                    result.Add(0x03);//Component Number
                    result.Add((byte)(typeSpec > 0 ? typeSpec : 0x11));//Horizontal or Vertical Sample

                    result.Add((byte)(tablesCount == 64 ? 0x00 : 0x01));//Matrix Number      
                }

                //Huffman Tables, Check for progressive version?

                if (progressive)
                {
                    result.AddRange(CreateHuffmanTableMarker(lum_dc_codelens_p, lum_dc_symbols_p, 0, 0));
                    result.AddRange(CreateHuffmanTableMarker(chm_dc_codelens_p, chm_dc_symbols_p, 1, 0));
                }
                else
                {
                    result.AddRange(CreateHuffmanTableMarker(lum_dc_codelens, lum_dc_symbols, 0, 0));
                    result.AddRange(CreateHuffmanTableMarker(lum_ac_codelens, lum_ac_symbols, 0, 1));
                }
                

                //More then 1 table (AND NOT PROGRESSIVE)
                if (tablesCount > 64 && false == progressive)
                {
                    result.AddRange(CreateHuffmanTableMarker(chm_dc_codelens, chm_dc_symbols, 1, 0));
                    result.AddRange(CreateHuffmanTableMarker(chm_ac_codelens, chm_ac_symbols, 1, 1));
                }

                //Start Of Scan
                result.Add(Media.Codecs.Image.Jpeg.Markers.Prefix);
                result.Add(Media.Codecs.Image.Jpeg.Markers.StartOfScan);//Marker SOS

                //If only 1 table (AND NOT PROGRESSIVE)
                if (tablesCount == 64)
                {
                    result.Add(0x00); //Length
                    result.Add(0x08); //Length - 12
                    result.Add(0x01); //Number of components
                    result.Add(0x00); //Component Number
                    result.Add(0x00); //Matrix Number
                    
                }
                else
                {
                    result.Add(0x00); //Length
                    result.Add(0x0c); //Length - 12
                    result.Add(0x03); //Number of components
                    result.Add(0x01); //Component Number
                    result.Add(0x00); //Matrix Number

                    //Should be indicated from typeSpec...

                    result.Add(0x02); //Component Number
                    result.Add((byte)(progressive ? 0x10 : 0x11)); //Horizontal or Vertical Sample

                    result.Add(0x03); //Component Number
                    result.Add((byte)(progressive ? 0x10 : 0x11)); //Horizontal or Vertical Sample
                }


                if (progressive)
                {
                    result.Add(0x00); //Start of spectral
                    result.Add(0x00); //End of spectral
                    result.Add(0x01); //Successive approximation bit position (high, low)
                }
                else
                {
                    result.Add(0x00); //Start of spectral
                    result.Add(0x3f); //End of spectral (63)
                    result.Add(0x00); //Successive approximation bit position (high, low)
                }

                return result.ToArray();
            }


            static readonly Common.MemorySegment EndOfInformationMarkerSegment = new Common.MemorySegment(new byte[] { Media.Codecs.Image.Jpeg.Markers.Prefix, Media.Codecs.Image.Jpeg.Markers.EndOfInformation });

            // The default 'luma' and 'chroma' quantizer tables, in zigzag order and energy reduced
            static byte[] defaultQuantizers = new byte[]
        {
           // luma table: Psychovisual
           16, 11, 12, 14, 12, 10, 16, 14,
           13, 14, 18, 17, 16, 19, 24, 40,
           26, 24, 22, 22, 24, 49, 35, 37,
           29, 40, 58, 51, 61, 60, 57, 51,
           56, 55, 64, 72, 92, 78, 64, 68,
           87, 69, 55, 56, 80, 109, 81, 87,
           95, 98, 103, 104, 103, 62, 77, 113,
           121, 112, 100, 120, 92, 101, 103, 99,
           // chroma table:
           17, 18, 18, 24, 21, 24, 47, 26,
           26, 47, 99, 66, 56, 66, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99,
           99, 99, 99, 99, 99, 99, 99, 99
        };

            static byte[] rfcQuantizers = new byte[]
        {
           // luma table:
            //From RFC2435 / Jpeg Spec
            16, 11, 10, 16, 24, 40, 51, 61,
            12, 12, 14, 19, 26, 58, 60, 55,
            14, 13, 16, 24, 40, 57, 69, 56,
            14, 17, 22, 29, 51, 87, 80, 62,
            18, 22, 37, 56, 68, 109, 103, 77,
            24, 35, 55, 64, 81, 104, 113, 92,
            49, 64, 78, 87, 103, 121, 120, 101,
            72, 92, 95, 98, 112, 100, 103, 99,
           // chroma table:
            //From RFC2435 / Jpeg Spec
            17, 18, 24, 47, 99, 99, 99, 99,
            18, 21, 26, 66, 99, 99, 99, 99,
            24, 26, 56, 99, 99, 99, 99, 99,
            47, 66, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99
        };
            
            //http://www.jatit.org/volumes/Vol70No3/24Vol70No3.pdf
            static byte[] psychoVisualQuantizers = new byte[]
        {
           // luma table:
           16, 14, 13, 15, 19, 28, 37, 55,
           14, 13, 15, 19, 28, 37, 55, 64,
           13, 15, 19, 28, 37, 55, 64, 83,
           15, 19, 28, 37, 55, 64, 83, 103,
           19, 28, 37, 55, 64, 83, 103, 117,
           28, 37, 55, 64, 83, 103, 117, 117,
           37, 55, 64, 83, 103, 117, 117, 111,
           55, 64, 83, 103, 117, 117, 111, 90,
           //chroma table
           18, 18, 23, 34, 45, 61, 71, 9,
           18, 23, 34, 45, 61, 71, 92, 92,
           23, 34, 45, 61, 71, 92, 92, 104,
           34, 45, 61, 71, 92, 92, 104, 115,
           45, 61, 71, 92, 92, 104, 115, 119,
           61, 71, 92, 92, 104, 115, 119, 112,
           71, 92, 92, 104, 115, 119, 112, 106,
           92, 92, 104, 115, 119, 112, 106, 100
        };

            /// <summary>
            /// Creates a Luma and Chroma Table in ZigZag order using the default quantizers specified in RFC2435
            /// </summary>
            /// <param name="type">Should be used to determine the sub sambling and table count or atleast which table to create? (currently not used)</param>
            /// <param name="Q">The quality factor</param>            
            /// <param name="precision"></param>
            /// <param name="useRfcQuantizer"></param>
            /// <returns>luma and chroma tables</returns>
            internal static byte[] CreateQuantizationTables(uint type, uint Q, byte precision, bool useRfcQuantizer, bool clamp = true, int maxQ = 100, bool psychoVisualQuantizer = false)
            {
                //Ensure not the reserved value.
                if (Q == 0) throw new InvalidOperationException("Q == 0 is reserved.");

                //RFC2035 did not specify a quantization table header and uses the values 0 - 127 to define this.
                //RFC2035 also does not specify what to do with Quality values 100 - 127
                //RFC2435 Other values [between 1 and 99 inclusive but] less than 128 are reserved
   
                //As per RFC2435 4.2.
                //if (Q >= 100) throw new InvalidOperationException("Q >= 100, a dynamically defined quantization table is used, which might be specified by a session setup protocol.");

                byte[] quantizer = useRfcQuantizer ? rfcQuantizers : psychoVisualQuantizer ? psychoVisualQuantizers : defaultQuantizers;

                //This is because Q can be 1 - 128 and values 100 - 127 may produce different Seed values however the standard only defines for Q 1 => 100
                //The higher values sometimes round or don't depending on the system they were generated in or the decoder of the system and are typically found in progressive images.

                //Note that FFMPEG uses slightly different quantization tables (as does this implementation) which are saturated for viewing within the psychovisual threshold.
                

                //Factor restricted to range of 1 and 100 (or maxQ)
                int factor = (int)(clamp ? Common.Binary.Clamp(Q, 1, maxQ) : Q);

                // 4.2 Text
                // S = 5000 / Q          for  1 <= Q <= 50
                //   = 200 - 2 * Q       for 51 <= Q <= 99

                //Seed quantization value for values less than or equal to 50, ffmpeg uses 1 - 49... @ https://ffmpeg.org/doxygen/2.3/rtpdec__jpeg_8c_source.html
                //Following the RFC @ Appendix A https://tools.ietf.org/html/rfc2435#appendix-A

                //This implementation differs slightly in that it uses the text from 4.2 literally.
                int q = (Q <= 50 ? (int)(5000 / factor) : 200 - factor * 2);

                //Create 2 quantization tables from Seed quality value using the RFC quantizers
                int tableSize = (precision > 0 ? 128 : 64);/// quantizer.Length / 2;
                
                //The tableSize should depend on the bit in the precision table.
                //This implies that the count of tables must be given... or that the math determining this needs to be solid..
                byte[] resultTables = new byte[tableSize * 2]; //two tables being returned... (should allow for only 1?)

                //bool luma16 = Common.Binary.GetBit(precision, 0), chroma16 = Common.Binary.GetBit(precision, 1);

                //Iterate for each element in the tableSize (the default quantizers are 64 bytes each in 8 bit form)

                int destLuma = 0, destChroma = 128;

                for (int lumaIndex = 0, chromaIndex = 64; lumaIndex < 64; ++lumaIndex, ++chromaIndex)
                {
                    //Check the bit in the precision table for the value which indicates if the tables are 16 bit or 32 bit?
                    //Normally, it would be read from the precision Byte when decoding but because of how this function is called 
                    //the value for precision as given is probably applicable for both tables because having a mixed set of 8 and 16 bit tables is not very likley
                    //Would need to refactor to write luma and then write chroma incase one is 16 bit and the other is not..... not very likely

                    //8 Bit tables       
                    if (precision == 0)
                    {
                        //Clamp with Min, Max (Should be written in correct bit order)
                        //Luma
                        resultTables[lumaIndex] = (byte)Common.Binary.Min(Common.Binary.Max((quantizer[lumaIndex] * q + 50) / 100, 1), byte.MaxValue);

                        //Chroma
                        resultTables[chromaIndex] = (byte)Common.Binary.Min(Common.Binary.Max((quantizer[chromaIndex] * q + 50) / 100, 1), byte.MaxValue);
                    }
                    else //16 bit tables
                    {

                        //Using the 8 bit table offset create the value and copy it to its 16 bit offset
                        
                        //Luma
                        if (BitConverter.IsLittleEndian)
                            BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)Common.Binary.Min(Common.Binary.Max((quantizer[lumaIndex] * q + 50) / 100, 1), byte.MaxValue))).CopyTo(resultTables, destLuma);
                        else
                            BitConverter.GetBytes((ushort)Common.Binary.Min(Math.Max((quantizer[lumaIndex] * q + 50) / 100, 1), byte.MaxValue)).CopyTo(resultTables, destLuma);

                        destLuma += 2;

                        //Chroma
                        if (BitConverter.IsLittleEndian)
                            BitConverter.GetBytes(Common.Binary.ReverseU16((ushort)Common.Binary.Min(Common.Binary.Max((quantizer[chromaIndex] * q + 50) / 100, 1), byte.MaxValue))).CopyTo(resultTables, destChroma);
                        else
                            BitConverter.GetBytes((ushort)Common.Binary.Min(Common.Binary.Max((quantizer[chromaIndex] * q + 50) / 100, 1), byte.MaxValue)).CopyTo(resultTables, destChroma);

                        destChroma += 2;
                    }
                }

                return resultTables;
            }

            /// <summary>
            /// Creates a Jpeg QuantizationTableMarker for each table given in the tables
            /// The precision must be the same for all tables when using this function.
            /// </summary>
            /// <param name="tables">The tables verbatim, either 1 or 2 (Lumiance and Chromiance)</param>
            /// <param name="precisionTable">The byte which indicates which table has 16 bit coeffecients</param>
            /// <returns>The table with marker and prefix and Pq/Tq byte</returns>
            internal static byte[] CreateQuantizationTableMarkers(Common.MemorySegment tables, byte precisionTable)
            {
                //List<byte> result = new List<byte>();

                int tableCount = tables.Count / (precisionTable > 0 ? 128 : 64);

                //Invalid sized tables....
                if (tables.Count % tableCount > 0) tableCount = 1;

                //??Some might have more then 3?
                if (tableCount > 3) throw new ArgumentOutOfRangeException("tableCount");

                int tableSize = tables.Count / tableCount;

                //The len includes the 2 bytes for the length and a single byte for the Lqcd
                byte len = (byte)(tableSize + 3);

                //Each tag is 4 bytes (prefix and tag) + 2 for len = 4 + 1 for Precision and TableId 
                byte[] result = new byte[(5 * tableCount) + (tableSize * tableCount)];

                //1 Table

                //Define QTable
                result[0] = Media.Codecs.Image.Jpeg.Markers.Prefix;
                result[1] = Media.Codecs.Image.Jpeg.Markers.QuantizationTable;

                result[2] = 0;//Len
                result[3] = len;

                //Pq / Tq
                result[4] = (byte)(precisionTable << 8 > 0 ? 8 : 0); // Precision and table (id 0 filled by shift)

                //First table. Type - Lumiance usually when two
                System.Array.Copy(tables.Array, tables.Offset, result, 5, tableSize);

                //2 Tables
                if (tableCount > 1)
                {
                    result[tableSize + 5] = Media.Codecs.Image.Jpeg.Markers.Prefix;
                    result[tableSize + 6] = Media.Codecs.Image.Jpeg.Markers.QuantizationTable;

                    result[tableSize + 7] = 0;//Len LSB
                    result[tableSize + 8] = len;

                    //Pq / Tq
                    result[tableSize + 9] = (byte)(precisionTable << 7 > 0 ? 8 : 1);//Precision and table Id 1

                    //Second Table. Type - Chromiance usually when two
                    System.Array.Copy(tables.Array, tables.Offset + tableSize, result, 10 + tableSize, tableSize);
                }

                //3 Tables
                if (tableCount > 2)
                {
                    result[tableSize + 10] = Media.Codecs.Image.Jpeg.Markers.Prefix;
                    result[tableSize + 11] = Media.Codecs.Image.Jpeg.Markers.QuantizationTable;

                    result[tableSize + 12] = 0;//Len LSB
                    result[tableSize + 13] = len;

                    //Pq / Tq
                    result[tableSize + 14] = (byte)(precisionTable << 6 > 0 ? 8 : 2);//Precision and table Id 2

                    //Second Table. Type - Chromiance usually when two
                    System.Array.Copy(tables.Array, tables.Offset + tableSize, result, 14 + tableSize, tableSize);
                }

                return result;
            }

            //Lumiance

            //JpegHuffmanTable StdDCLuminance

            static byte[] lum_dc_codelens = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
                        //Progressive
                        lum_dc_codelens_p = { 0, 2, 3, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            static byte[] lum_dc_symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                        //Progressive
                        lum_dc_symbols_p = { 0, 2, 3, 0, 1, 4, 5, 6, 7 }; //lum_dc_symbols_p = { 0, 0, 2, 1, 3, 4, 5, 6, 7}; Work for TestProg but not TestImgP

            //JpegHuffmanTable StdACLuminance

            static byte[] lum_ac_codelens = { 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };

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
            
            //JpegHuffmanTable StdDCChrominance
            static byte[] chm_dc_codelens = { 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
                        //Progressive
                        chm_dc_codelens_p = { 0, 3, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            static byte[] chm_dc_symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                        //Progressive
                        chm_dc_symbols_p = { 0, 1, 2, 3, 0, 4, 5 };

            //JpegHuffmanTable StdACChrominance

            static byte[] chm_ac_codelens = { 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };

            static byte[] chm_ac_symbols = 
            {
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

            //Todo, Move most of the logic here to Media.Codecs.Image.Jpeg

            /// <summary>
            /// Average AC table values to estimate compression level, doesn't work well with 16 bit tables.
            /// </summary>
            /// <param name="precisionTable"></param>
            /// <param name="tables"></param>
            /// <param name="offset"></param>
            /// <param name="length"></param>
            /// <returns></returns>
            /// <remarks>Ported from <see href="http://www.hackerfactor.com/src/jpegquality.c">Hacker Factory</see></remarks>
            public static int DetermineAverageQuality(byte precisionTable, IList<byte> tables, int offset, int length)
            {
                /* Quantization tables have 1 DC value and 63 AC values */

                /** Average AC table values to estimate compression level **/

                //Todo, Optimize

                float Diff = 0;  /* difference between quantization tables */

                float[] QualityAvg = new float[] { 0, 0, 0 };

                float QualityF = 0; /* quality as a float */

                int Quality = 0; /* quality as an integer */

                float Total = 0;

                float TotalNum = 0;

                int Index = 0;

                int tableSize = (precisionTable > 0 ? 128 : 64);

                int tableCount = length / tableSize;

                bool precision = false;

                int sourceOffset = offset;

                for (int i = 0; i < tableCount; ++i)
                {
                    Total = 0;

                    TotalNum = 0;

                    sourceOffset = 0;
                    
                    //Todo, ensure MSB....
                    precision = Common.Binary.GetBit(ref precisionTable, i);

                    //Set the tableSize based on the precision bit
                    tableSize = precision ? 128 : 64;

                    while (TotalNum < tableSize)
                    {
                        //Ignoring the first value, Total the values of the AC components
                        if (TotalNum != 0) Total += precision ? Common.Binary.ReadU16(tables, (i * tableSize) + sourceOffset, BitConverter.IsLittleEndian) : tables[(i * tableSize) + (int)TotalNum];

                        //Count components
                        TotalNum++;

                        if (precision) sourceOffset += 2;
                    }

                    //For tables less than 3  (0, 1, 2)
                    if (Index < 3)
                    {
                        //Set the average to 100
                        QualityAvg[Index] = (float)(100.0 - Total / TotalNum);

                        //Copy 100 as the default value to any forward tables remaining
                        for (int z = Index + 1; z < 3; z++) QualityAvg[z] = QualityAvg[Index];
                    }

                    if (Index > 0)
                    {
                        /* Diff is a really rough estimate for converting YCrCb to RGB */
                        Diff = (float)(Math.Abs(QualityAvg[0] - QualityAvg[1]) * 0.49);

                        Diff += (float)(Math.Abs(QualityAvg[0] - QualityAvg[2]) * 0.49);

                        //Modified to only happen on non 0 cases (may not be correct)
                        if (Diff > 0)
                        {
                            /* If you know that Cr==Cb and don't mind a little more error,
                            then you can take a short-cut and simply say
                            Diff = Abs(QualityAvg[0]-QualityAvg[1]); */
                            QualityF = (float)((QualityAvg[0] + QualityAvg[1] + QualityAvg[2]) / 3.0 + Diff);

                            Quality = (int)(QualityF + 0.5); /* round quality to int */
                        }
                    }

                    Index++;
                }

                //////Modified to return the correct results in certain cases
                ////if (precision && (QualityAvg[0] < 0 || QualityAvg[1] < 0 || QualityAvg[2] < 0))
                ////{
                ////    int test = Common.Binary.Abs((int)QualityAvg[1]);
                ////    if (test >= 5 && test <= 14) Quality = (int)(100 - Diff + 1);//test;
                ////    if(Quality >= 50) Quality = Common.Binary.Abs(Quality - 50);
                ////}

                //Don't return values less than 0, use diff instead
                return (int)(Quality <= 0 ? Diff : Quality);
            }

            /// <summary>
            /// Determines the quality of the given table at the offset
            /// </summary>
            /// <param name="precision">Indicates if the value is 16 bit</param>
            /// <param name="table">The quantization table data (after the id and table type)</param>
            /// <param name="offset">The offset to the data</param>
            /// <returns>The quality determined</returns>
            /// <remarks>Ported from <see href="https://github.com/socoola/Zheyang/blob/master/rtp-jpeg.c">socoola/Zheyang</see></remarks>
            public static int DetermineQuality(bool precision, IList<byte> table, int offset)
            {
                int seed = 100 * (precision ? Common.Binary.ReadU16(table, offset, BitConverter.IsLittleEndian) : table[offset]) / defaultQuantizers[0];

                return seed == 0 ? 0 : seed > 100 ? 5000 / seed : 100 - (seed >> 1);
            }

            //Todo, move to JPEG.
            internal static byte[] CreateHuffmanTableMarker(byte[] codeLens, byte[] symbols, int tableNo, int tableClass)
            {
                List<byte> result = new List<byte>();
                result.Add(Media.Codecs.Image.Jpeg.Markers.Prefix);
                result.Add(Media.Codecs.Image.Jpeg.Markers.HuffmanTable);
                result.Add(0x00); //Legnth
                result.Add((byte)(3 + codeLens.Length + symbols.Length)); //Length
                result.Add((byte)((tableClass << 4) | tableNo)); //Id
                result.AddRange(codeLens);//Data
                result.AddRange(symbols);
                return result.ToArray();
            }

            internal static byte[] CreateDataRestartIntervalMarker(ushort dri)
            {
                return new byte[] { Media.Codecs.Image.Jpeg.Markers.Prefix, Media.Codecs.Image.Jpeg.Markers.DataRestartInterval, 0x00, 0x04, (byte)(dri >> 8), (byte)(dri) };
            }

            #endregion

            #region Constructor

            static RFC2435Frame() { if (JpegCodecInfo == null) throw new NotSupportedException("The system must have a Jpeg Codec installed."); }

            /// <summary>
            /// Creates an empty JpegFrame
            /// </summary>
            public RFC2435Frame() : base(RFC2435Frame.RtpJpegPayloadType) { MaxPackets = 2048; }

            /// <summary>
            /// Creates a new JpegFrame from an existing RtpFrame which has the JpegFrame PayloadType
            /// </summary>
            /// <param name="f">The existing frame</param>
            public RFC2435Frame(Rtp.RtpFrame f) : base(f) { if (PayloadType != RFC2435Frame.RtpJpegPayloadType) throw new ArgumentException("Expected the payload type 26, Found type: " + f.PayloadType); }

            /// <summary>
            /// Creates a shallow copy an existing JpegFrame
            /// </summary>
            /// <param name="f">The JpegFrame to copy</param>
            public RFC2435Frame(RFC2435Frame f) : base(f, true, true) { } //{ m_Buffer = f.m_Buffer; }

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
            /// <param name="bytesPerPacket">The maximum amount of octets of each RtpPacket</param>
            public static RFC2435Frame Packetize(System.Drawing.Image existing, int imageQuality = 100, bool interlaced = false, int? ssrc = null, int? sequenceNo = 0, long? timeStamp = 0, int bytesPerPacket = 1456)
            {
                if (imageQuality <= 0) throw new NotSupportedException("Only qualities 1 - 100 are supported");
                
                System.Drawing.Image image;

                bool disposeImage = false;

                //If the data is larger then supported resize
                if (existing.Width > JpegMaxSizeDimension || existing.Height > JpegMaxSizeDimension)
                {
                    image = existing.GetThumbnailImage(MaxWidth, MaxHeight, null, IntPtr.Zero);

                    disposeImage = true;
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

                    // Set the quality (Quality == 100 on GDI is prone to decoding errors?) It doesn't accept values > 100.
                    //This should probably clamp to 99
                    parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)(imageQuality >= 100 ? 100 : imageQuality));

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

                    if (disposeImage) image.Dispose();

                    //When the quality is >= 100 specify that the quantization tables will be included by using >= 128 for quality. (RFC2035 used >= 100)
                    //ffmpeg recently allows Q=100 to be calculated so this may need to change to > 100
                    return new RFC2435Frame(temp, imageQuality >= 100 ? 128 : imageQuality, ssrc, sequenceNo, timeStamp, bytesPerPacket);
                }

            }

            //Todo, use MarkerReader from JPEG
            /// <summary>
            /// Creates a <see cref="http://tools.ietf.org/search/rfc2435">RFC2435 Rtp Frame</see> using the given parameters.
            /// </summary>
            /// <param name="jpegStream">The stream which contains JPEG formatted data and starts with a StartOfInformation Marker</param>
            /// <param name="qualityFactor">The value to utilize in the RFC2435 Q field, a value >= 100 causes the Quantization Tables to be sent in band.</param>
            /// <param name="typeSpecific">The value of the TypeSpeicifc field in the each RtpPacket created</param>
            /// <param name="ssrc">The optional Id of the media</param>
            /// <param name="sequenceNo">The optional sequence number for the first packet in the frame.</param>
            /// <param name="timeStamp">The optional Timestamp to use for each packet in the frame.</param>
            /// <param name="bytesPerPacket">The amount of bytes each RtpPacket will contain</param>
            /// <param name="sourceList">The <see cref="SourceList"/> to be included in each packet of the frame</param>
            public RFC2435Frame(System.IO.Stream jpegStream, int? qualityFactor = null, int? ssrc = null, int? sequenceNo = 0, long? timeStamp = 0, int bytesPerPacket = 1456, RFC3550.SourceList sourceList = null)
                : this()
            {
                //Ensure qualityFactor can be stored in a byte
                if (qualityFactor.HasValue && (qualityFactor > byte.MaxValue || qualityFactor == 0))
                    throw Common.Binary.CreateOverflowException("qualityFactor", qualityFactor, 1.ToString(), byte.MaxValue.ToString());

                //Store the constant size of the RtpHeader (12) and sourceList
                int sourceListSize = sourceList != null ? sourceList.Size : 0, protocolOverhead = Rtp.RtpHeader.Length + sourceListSize,
                    precisionTableIndex = 0; //The index of the precision table.

                //Ensure some data will fit
                if (bytesPerPacket < protocolOverhead + 8) throw new InvalidOperationException("Each RtpPacket in the RtpFrame must contain at least RtpHeader (12 octets) + sourceList as well as the RtpJpeg Header (8 octets + possibly more).");

                //Set the id of all subsequent packets.
                SynchronizationSourceIdentifier = ssrc ?? -1;

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

                //Stream must support Seeking?
                //Should be in Container.JPEG?

                //From the beginning of the stream
                jpegStream.Seek(0, System.IO.SeekOrigin.Begin);

                //Check for the Start of Information Marker
                if (jpegStream.ReadByte() != Media.Codecs.Image.Jpeg.Markers.Prefix && jpegStream.ReadByte() != Media.Codecs.Image.Jpeg.Markers.StartOfInformation)
                    throw new NotSupportedException("Data does not start with Start Of Information Marker");

                //Check for the End of Information Marker, //If present do not include it.
                jpegStream.Seek(-1, System.IO.SeekOrigin.End);

                long streamLength = jpegStream.Length,
                    //Check for the eoi since we don't read in aligned MCU's yet
                    endOffset = jpegStream.ReadByte() == Media.Codecs.Image.Jpeg.Markers.EndOfInformation ? streamLength - 2 : streamLength;

                //From the beginning of the buffered stream after the Start of Information Marker
                jpegStream.Seek(2, System.IO.SeekOrigin.Begin);

                int FunctionCode, //Describes the content of the marker
                    CodeSize, //The lengof the marker segment
                    Ssrc = SynchronizationSourceIdentifier;//Cached Ssrc of all packets.

                //Variables used for Timestamp of each RtpPacket
                ushort Timestamp = (ushort)timeStamp,
                    SequenceNo = (ushort)(sequenceNo), //And sequenceNumber
                    Lr = 0, Ri = 0; //Used for RestartIntervals

                //The current packet consists of a RtpHeader and the encoded payload.
                //The encoded payload of the first packet will consist of the RtpJpegHeader (8 octets) as well as QTables and coeffecient data releated to the image.
                Rtp.RtpPacket currentPacket = new Rtp.RtpPacket(new byte[(streamLength < bytesPerPacket ? (int)streamLength : bytesPerPacket)], 0)
                {
                    Version = 2,

                    Timestamp = Timestamp,

                    SequenceNumber = SequenceNo++,

                    PayloadType = RFC2435Frame.RtpJpegPayloadType,

                    SynchronizationSourceIdentifier = Ssrc
                };

                //Apply source list
                if (sourceListSize > 0)
                {
                    currentPacket.ContributingSourceCount = sourceList.Count;
                    sourceList.TryCopyTo(currentPacket.Payload.Array, currentPacket.Payload.Offset);
                }


                //Offset into the stream
                long streamOffset = 2,
                    //Where we are in the current packet payload after the sourceList and extensions
                    currentPacketOffset = currentPacket.Payload.Offset + currentPacket.HeaderOctets;

                //TODO, use MARKER READER FROM JPEG

                //TODO ALIGN MCU's/ RESTART INTERVALS

                //TODO SUPPORT RFC2035 MCU TYPES (FRAGMENTOFFSET if possible)

                //Find a Jpeg Tag while we are not at the end of the stream
                //Tags come in the format 0xFFXX
                while ((FunctionCode = jpegStream.ReadByte()) != -1)
                {
                    ++streamOffset;

                    //If the prefix is a tag prefix then read another byte as the Tag
                    if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.Prefix)
                    {
                        //Get the underlying FunctionCode
                        FunctionCode = jpegStream.ReadByte();

                        ++streamOffset;

                        //If we are at the end break
                        if (FunctionCode == -1) break;

                        //Ensure not padded
                        if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.Prefix
                            ||
                            // ff00 is the escaped form of 0xff, will never be encountered the way data is read after the SOF
                            FunctionCode == 0) continue;

                        //Last Tag
                        if (FunctionCode == Media.Codecs.Image.Jpeg.Markers.EndOfInformation) break;

                        //Read the Marker Length

                        //Read Length Bytes
                        byte h = (byte)jpegStream.ReadByte(), l = (byte)jpegStream.ReadByte();

                        streamOffset += 2;

                        //Calculate Length
                        CodeSize = h * 256 + l;

                        //Correct Length
                        CodeSize -= 2; //Not including their own length

                        //Determine what to do based on the FunctionCode
                        switch (FunctionCode)
                        {
                            case Media.Codecs.Image.Jpeg.Markers.QuantizationTable:
                                {
                                    //Note that RFC2035 allowed 100 - 127 to specify this.

                                    //This is skipping the tables, it should probably continue and ensure the Quality value given will hold or adjust it if necessary.
                                    if (Quality < 128 || CodeSize < 1) goto default;

                                    byte compound = (byte)jpegStream.ReadByte();//Read Table Id (And Precision which is in the same byte)

                                    ++streamOffset;

                                    //Precision of 1 indicates a 16 bit table 128 bytes per table, 0 indicates 8 bits 64 bytes per table
                                    bool precision = compound > 15;

                                    //byte tableId = (byte)(compound & 0xf);

                                    int tagSizeMinusOne = CodeSize - 1;

                                    //If there is no data in the tag continue
                                    if (tagSizeMinusOne <= 0) continue;

                                    byte[] table = new byte[tagSizeMinusOne];

                                    //Set a bit in the precision table to indicate 16 bit coefficients
                                    if (precision) RtpJpegPrecisionTable |= (byte)(1 << precisionTableIndex);

                                    //Move the precisionIndex
                                    ++precisionTableIndex;

                                    //Read the remainder of the data into the table array
                                    jpegStream.Read(table, 0, tagSizeMinusOne);

                                    //Move the stream offset
                                    streamOffset += tagSizeMinusOne;

                                    //Add the table array to the table blob
                                    QuantizationTables.AddRange(table);                                    

                                    break;
                                }
                            //Should store, must be present for 0 height and width in SOF
                            //case Media.Codecs.Image.Jpeg.Markers.DefineNumberOfLines:
                            //    {
                            //        //ScanLines = (ushort)(jpegStream.ReadByte() * 256 + jpegStream.ReadByte());
                            //        //streamOffset += 2;
                            //        break;
                            //    }
                            //I assume this could really be based on the first few bits (startOf) 0xc where the 0 indicates baseline, etc.
                            case Media.Codecs.Image.Jpeg.Markers.StartOfBaselineFrame:
                                //Extended sequential DCT
                            case Media.Codecs.Image.Jpeg.Markers.StartOfProgressiveFrame:
                                //Lossless
                                //etc
                                {
                                    #region Using other types of frames

                                    //Thus if you wanted to signal that you could give the last 4 bits of the FunctionCode to the RtpJpegType or RtpJpegTypeSpecific.
                                    //I use RtpJpegTypeSpecific to signal the sampling factors because there are 4 unused bits which is all that is needed, this value is also not usually passed to the bitstream so changing it usually does not get it into the bit stream or interfere with receivers.
                                    //I could also use the RtpJpeg field to signal the FrameTime, it is only checked to be 0 in the RFC to determine the sampling factors, other values here shouldn't matter depending on how the receiver interprets this however the SOF0 WILL be written at the receiver side so it's best not to use this field to signal anything

                                    //RtpJpegType |= (byte)(FunctionCode << 28);
                                    //Or by masking out what we may expect...
                                    //RtpJpegType = (byte)(FunctionCode & 0xc0);

                                    #endregion

                                    //If there is no data in the tag continue. (9 is probably the minimum, unless there are 0 components?)
                                    if (CodeSize <= 6) throw new InvalidOperationException("Invalid StartOfFrame");

                                    //Read the StartOfFrame Marker
                                    byte[] data = new byte[CodeSize];

                                    int offset = 0;
                                    
                                    //Read CodeSize bytes from the stream into data and skip the first byte being read.
                                    jpegStream.Read(data, offset++, CodeSize);

                                    //If this is a SOF2 (progressive) marker the type should be set appropraitely.

                                    //@0 - Sample precision – Specifies the precision in bits for the samples of the components in the frame (1)
                                    //++offset;

                                    //Y Number of lines [Height] (2)
                                    Height = Common.Binary.ReadU16(data, ref offset, BitConverter.IsLittleEndian);

                                    //X Number of lines [Width] (2)
                                    Width = Common.Binary.ReadU16(data, ref offset, BitConverter.IsLittleEndian);

                                    //When width, height == 0, DNL Marker should be present..

                                    //Check for SubSampling to set the RtpJpegType from the Luma component

                                    //http://tools.ietf.org/search/rfc2435#section-4.1
                                    //Type numbers 2-5 are reserved and SHOULD NOT be used.

                                    //if (data[7] != 0x21) RtpJpegType |= 1;

                                    //Nf - Number of image components in frame (Channels)
                                    int Nf = data[offset++];

                                    //Hi: Horizontal sampling factor – Specifies the relationship between the component horizontal dimension
                                    //and maximum image dimension X (see http://www.w3.org/Graphics/JPEG/itu-t81.pdf A.1.1); also specifies the number of horizontal data units of component
                                    //Ci in each MCU, when more than one component is encoded in a scan                                                        

                                    //Each component takes 3 bytes Ci, {Hi, Vi,} Tqi

                                    //Vi: Vertical sampling factor – Specifies the relationship between the component vertical dimension and
                                    //maximum image dimension Y (see http://www.w3.org/Graphics/JPEG/itu-t81.pdf A.1.1); also specifies the number of vertical data units of component Ci in
                                    //each MCU, when more than one component is encoded in a scan                                                                 

                                    //Experimental Support for Any amount of samples.
                                    if (Nf > 1)
                                    {
                                        //Check remaining components (Chroma)
                                        //for (int i = 1; i < numberOfComponents; ++i) if (data[7 + i * 3] != 17) throw new Exception("Only 1x1 chroma blocks are supported.");

                                        // Add all of the necessary components to the frame, if there is the data specified for the component.
                                        for (int tableId = 0; 
                                            //Ensure count and offsets
                                            tableId < Nf 
                                                && 
                                            offset + 3 < CodeSize; 
                                            //Increase tableId
                                            ++tableId)
                                        {
                                            byte compId = data[offset++];
                                            byte samplingFactors = data[offset++];
                                            byte qTableId = data[offset++];

                                            byte sampleHFactor = (byte)(samplingFactors >> 4);
                                            byte sampleVFactor = (byte)(samplingFactors & 0x0f);

                                            //Check for 1x1 (Default supported)
                                            if (sampleHFactor != 1 || sampleVFactor != 1)
                                            {
                                                //Not 2x1 must be flagged
                                                if ((sampleHFactor != 2 || sampleVFactor != 1))
                                                {
                                                    if (tableId == 0) RtpJpegType |= 1;
                                                    else if (tableId > 0 && samplingFactors != RtpJpegTypeSpecific)
                                                    {
                                                        //Experimentally
                                                        //Signal the sampling factor of the component into the RtpJpegTypeSpecific
                                                        RtpJpegTypeSpecific ^= samplingFactors;
                                                    }
                                                }
                                            }
                                        }
                                    }//Nf == 1 Single Component, Flag in Subsampling if required
                                    else if(Nf == 1)
                                    {
                                        //H ! 2x1
                                        if (data[++offset] != 0x21) RtpJpegType |= 1;
                                        //V ! 1x1
                                        if (data[++offset] != 0x11 && data[offset] > 0) RtpJpegTypeSpecific = data[offset];
                                    }
                                    //else //Nf == 0, There are no defined components
                                    //{
                                        //offset should be equal to CodeSize or undefined data resides.
                                    //}

                                    //Move stream offset (Already read CodeSize bytes)
                                    streamOffset += CodeSize;

                                    continue;
                                }
                            case Media.Codecs.Image.Jpeg.Markers.DataRestartInterval:
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
                                    Lr = (ushort)(jpegStream.ReadByte() * 256 + jpegStream.ReadByte());

                                    Ri = (ushort)(jpegStream.ReadByte() * 256 + jpegStream.ReadByte());

                                    streamOffset += 4;

                                    //Set the first bit now, set the last bit on the last packet
                                    RtpJpegRestartInterval = CreateRtpJpegDataRestartIntervalMarker(Lr, true, false);

                                    //Increase RtpJpegType by 64
                                    RtpJpegType |= 0x40;

                                    continue;
                                }
                            case Media.Codecs.Image.Jpeg.Markers.StartOfScan: //Last marker encountered
                                {
                                    long pos = streamOffset;

                                    //Read the number of Component Selectors
                                    byte Ns = (byte)jpegStream.ReadByte();

                                    ++streamOffset;

                                    //Should be equal to the number of components from the Start of Scan
                                    if (Ns > 0)
                                    {
                                        //Should check tableInfo  does not exceed the number of HuffmanTables
                                        //The number of Quant tables MAY be equal to the number of huffman tables so if there are (64 / QuantizationTables.Count) Tables
                                        //int tableCount = QuantizationTables.Count / (RtpJpegPrecisionTable > 0 ? 128 : 64);

                                        //Should ALSO verify count of data remaining or check result of ReadByte for -1.

                                        //loop to extract the information
                                        for (int i = 0; i < Ns; ++i)
                                        {
                                            // Component ID, packed byte containing the Id for the
                                            // AC table and DC table.
                                            byte componentID = (byte)jpegStream.ReadByte();

                                            byte tableInfo = (byte)jpegStream.ReadByte();

                                            streamOffset += 2;

                                            //Restrict or throw exception? (See notes above)
                                            //if (tableInfo > tableCount - 1) tableInfo = (byte)(tableCount - 1);

                                            //Decode DC and AC Values if require
                                            //int DC = (tableInfo >> 4) & 0x0f;
                                            //int AC = (tableInfo) & 0x0f;
                                        }
                                    }

                                    //These sometimes matter and thus would also need to be signaled.
                                    byte startSpectralSelection = (byte)(jpegStream.ReadByte());

                                    byte endSpectralSelection = (byte)(jpegStream.ReadByte());

                                    byte successiveApproximation = (byte)(jpegStream.ReadByte());

                                    streamOffset += 3;

                                    if (pos + CodeSize != streamOffset) throw new InvalidOperationException("Invalid StartOfScan Marker");

                                    //Check for alternate endSpectral (63 = default)
                                    //if (RtpJpegType > 0 && Ns > 0 && endSpectralSelection != 0x3f) RtpJpegTypeSpecific = Media.Codecs.Image.Jpeg.Markers.StartOfProgressiveFrame;
                                    
                                    //Todo, determine which tables to look at based on how many tables and the type of sub sampling.                                    

                                    //Should attempt to ensure quality is correct by looking at at tables.
                                    //int determinedQuality = RFC2435Frame.DetermineAverageQuality(RtpJpegPrecisionTable, QuantizationTables, 0, QuantizationTables.Count);

                                    //If the quality was incorrect set then use the quality the image required.
                                    //if (Quality != determinedQuality) Quality = (byte)determinedQuality;

                                    //Create RtpJpegHeader and CopyTo currentPacket advancing currentPacketOffset
                                    //If Quality >= 100 then the QuantizationTableHeader + QuantizationTables also reside here (after any RtpRestartMarker if present).
                                    //Note RFC2034 allows 100 Max where as RFC2425 allows 127 Max
                                    byte[] RtpJpegHeader = CreateRtpJpegHeader(RtpJpegTypeSpecific, 0, RtpJpegType, Quality, Width, Height, RtpJpegRestartInterval, RtpJpegPrecisionTable, QuantizationTables);

                                    //Copy the first header
                                    RtpJpegHeader.CopyTo(currentPacket.Payload.Array, currentPacketOffset);

                                    //Advance the offset the size of the profile header.
                                    currentPacketOffset += RtpJpegHeader.Length;

                                    //Determine how many bytes remanin in the payload after adding the first RtpJpegHeader which also contains the QTables                            
                                    long remainingPayloadOctets = bytesPerPacket;

                                    int profileHeaderSize = Ri > 0 ? 12 : 8; //Determine if the profile header also contains the RtpRestartMarker

                                    //rtp jpeg header already copied so not included (currentPacketOffset already moved) 
                                    remainingPayloadOctets -= currentPacketOffset + Rtp.RtpHeader.Length;

                                    //How much remains in the stream relative to the endOffset
                                    long streamRemains = endOffset - streamOffset;

                                    //Todo, should align restart intervals to ensure partial decoding is possible.

                                    //Todo if Ri > 0, each packet can only contain 1 Ri to properly support partial decoding
                                    //Must align intervals.
                                    //if (Ri > 0)
                                    //{
                                    //remainingPayloadOctets = //Calculate from RST marker
                                    //}

                                    //Type 2 or 3 is only a specific MCU

                                    //A RtpJpegHeader which must be in the Payload of each Packet (8 Bytes without QTables and RestartInterval)
                                    //RtpJpegPrecisionTable is the the same when the same qTables are being used and will not be included when QTables.Count == 0
                                    //When Ri > 0 an additional 4 bytes occupy the Payload to represent the RST Marker

                                    //The quant and dri headers only appear in the first packet, remove them from the header now.
                                    RtpJpegHeader = RtpJpegHeader.Take(profileHeaderSize).ToArray();

                                    if (profileHeaderSize != RtpJpegHeader.Length) throw new InvalidOperationException("Incorrect profileHeaderSize.");

                                    //Only the lastPacket contains the marker, determine when reading
                                    bool lastPacket = false;

                                    int justRead;

                                    //While we are not done reading
                                    while (streamRemains > 0)
                                    {
                                        //Read what we need into the packet (Should be done according to CodeSize.)
                                        do
                                        {
                                            justRead = jpegStream.Read(currentPacket.Payload.Array, (int)(currentPacketOffset), (int)remainingPayloadOctets);

                                            if (justRead < 0)
                                            {
                                                if (jpegStream.CanRead) continue;
                                                else throw new InvalidOperationException("Cannot read remaining data from image.");
                                            }

                                            streamOffset += justRead;

                                            remainingPayloadOctets -= justRead;

                                        } while (remainingPayloadOctets > 0);

                                        //Update how much remains in the stream
                                        streamRemains = endOffset - streamOffset;

                                        //Add current packet to the frame
                                        Add(currentPacket);

                                        //Remove the reference to the currentPacket added
                                        currentPacket = null;

                                        if (streamRemains <= 0) break;
                                        //Determine if we need to adjust the size and add the packet
                                        else if (streamRemains < bytesPerPacket)
                                        {
                                            //8 for the RtpJpegHeader and this will cause the Marker be to set in the next packet created
                                            bytesPerPacket = (int)streamRemains + Rtp.RtpHeader.Length + profileHeaderSize;

                                            if (sourceList != null) bytesPerPacket += sourceList.Size;

                                            lastPacket = true;

                                            //Set the last bit of the Dri Header in the last packet if Ri > 0 (first bit is still set! might have to unset??)
                                            if (Ri > 0) RtpJpegHeader[10] ^= (byte)(1 << 7);
                                        }

                                        //Make next packet which consists of a RtpHeader and the remaining remainingPayloadOctets
                                        currentPacket = new Rtp.RtpPacket(new byte[bytesPerPacket], 0)
                                        {
                                            Timestamp = Timestamp,
                                            SequenceNumber = SequenceNo++,
                                            SynchronizationSourceIdentifier = Ssrc,
                                            PayloadType = RFC2435Frame.RtpJpegPayloadType,
                                            Marker = lastPacket,
                                            Version = 2
                                        };

                                        //Apply source list
                                        if (sourceList != null)
                                        {
                                            currentPacket.ContributingSourceCount = sourceList.Count;
                                            sourceList.TryCopyTo(currentPacket.Payload.Array, 0);
                                        }

                                        //Check for FragmentOffset to exceed 24 bits
                                        if (streamOffset > Common.Binary.U24MaxValue) Common.Binary.Write24(RtpJpegHeader, 1, BitConverter.IsLittleEndian, (uint)(streamOffset - Common.Binary.U24MaxValue));
                                        else Common.Binary.Write24(RtpJpegHeader, 1, BitConverter.IsLittleEndian, (uint)streamOffset);

                                        //Copy header
                                        RtpJpegHeader.CopyTo(currentPacket.Payload.Array, currentPacket.Payload.Offset);

                                        //Set offset in packet (the length of the RtpJpegHeader (calculated via profileHeaderSize))
                                        
                                        //We are in the payload @ offset + sourceList / extensions + profileHeaderSize
                                        currentPacketOffset = currentPacket.Payload.Offset + currentPacket.HeaderOctets + profileHeaderSize;

                                        //reset the remaning remainingPayloadOctets
                                        remainingPayloadOctets = bytesPerPacket - (currentPacketOffset + Rtp.RtpHeader.Length);
                                    }

                                    //Done here
                                    return;
                                }
                            default:
                                {

                                    //C4 = Huffman
                                    //Might have to read huffman to determine compatiblity tableClass 1 is Lossless?)
                                    //E1 = JpegThumbnail 160x120 Adobe XMP
                                    //E2 = ICC ColorProfile
                                    jpegStream.Seek(CodeSize, System.IO.SeekOrigin.Current);

                                    streamOffset += CodeSize;

                                    continue;
                                }
                        }
                    }
                }
            }

            #endregion

            #region Fields

            /// <summary>
            /// Provied access the to underlying buffer where the image is stored.
            /// </summary>
            //public System.IO.MemoryStream Buffer { get; protected set; }

            #endregion

            #region Properties

            public override bool IsComplete
            {
                get
                {
                    //First use the existing IsComplete logic.
                    if (false == base.IsComplete) return false;

                    Rtp.RtpPacket packet = Packets[0];

                    //The FragmentOffset must be 0 at the start of a new frame.

                    //Use Payload rather than the array because MemorySegment will fix the offset to start at Payload.Offset
                    //Or
                    //Use packet.Payload.Array and  packet.Payload.Offset + packet.HeaderOctets + 1
                    return Common.Binary.ReadU24(packet.Payload, packet.HeaderOctets + 1, BitConverter.IsLittleEndian) == 0;
                }
            }            

            #endregion

            #region Methods

            //This will essentially provide only the data in the payload, the q tables are never returned.
            //This is useful for if the tables are already known or not needed.

            public override Common.MemorySegment Assemble(Rtp.RtpPacket packet, bool useExtensions = false, int profileHeaderSize = 0)
            {
                int offset = packet.Payload.Offset + packet.HeaderOctets;

                profileHeaderSize = ProfileHeaderInformation.GetProfileHeaderSize(packet);

                return base.Assemble(packet, useExtensions, profileHeaderSize);
            }

            public override void Depacketize(Rtp.RtpPacket packet) { ProcessPacket(packet, false, true); }

            //Was Depacketize
            //Todo
            public void ProcessPacket(Rtp.RtpPacket packet, bool rfc2035Quality = false, bool useRfcQuantizer = false) //Should give tables here and should have option to check for EOI
            {

                if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

                //Depacketize the single packet to Depacketized.

                /*
                 * 
                 * 3.1.  JPEG header
                   Each packet contains a special JPEG header which immediately follows
                   the RTP header.  The first 8 bytes of this header, called the "main
                   JPEG header", are as follows:

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   | Type-specific |              Fragment Offset                  |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |      Type     |       Q       |     Width     |     Height    |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 * 
                 All fields in this header except for the Fragment Offset field MUST
                 remain the same in all packets that correspond to the same JPEG
                 frame.

                 A Restart Marker header and/or Quantization Table header may follow this header, depending on the values of the Type and Q fields.                 
                 */

                byte TypeSpecific, Type, Quality,
                    //A byte which is bit mapped, each bit indicates 16 bit coeffecients for the table .
                PrecisionTable = 0;

                uint FragmentOffset, Width, Height;

                ushort RestartInterval = 0, RestartCount;

                //Payload starts at the offset of the first PayloadOctet within the Payload segment after the sourceList or extensions.
                int offset = packet.Payload.Offset + packet.HeaderOctets,
                    padding = packet.PaddingOctets,
                    end = (packet.Payload.Count - padding),
                    count = end - offset;
                
                
                            //ProfileHeaderInformation.MinimumProfileHeaderSize

                //Need 8 bytes.
                if (count < 8) throw new InvalidOperationException("Invalid packet.");

                //if (packet.Extension) throw new NotSupportedException("RFC2035 nor RFC2435 defines extensions.");

                Common.MemorySegment tables;

                //We will depacketize something and may need to inspect the last bytes of the memory added.
                Common.MemorySegment depacketized = null;

                //Decode RtpJpeg Header

                //Should verify values after first packet....

                TypeSpecific = (packet.Payload.Array[offset++]);

                FragmentOffset = Common.Binary.ReadU24(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian); //(uint)(packet.Payload.Array[offset++] << 16 | packet.Payload.Array[offset++] << 8 | packet.Payload.Array[offset++]);

                //Todo, should preserve order even when FragmentOffset and Sequence Number wraps.
                                                                                            //May not provide the correct order when sequenceNumber approaches ushort.MaxValue...
                int packetKey = GetPacketKey(packet.SequenceNumber);//Depacketized.Count > 0 ? Depacketized.Keys.Last() + 1 : 0; //packet.Timestamp - packet.SequenceNumber;

                //If fragment offset wraps this packet will not be able to be added.
                //packetKey += (int)FragmentOffset;

                //Already contained.
                if (Depacketized.ContainsKey(packetKey)) return;

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

                Type = (packet.Payload.Array[offset++]);

                //Check for a RtpJpeg Type of less than 5 used in RFC2035 for which RFC2435 is the errata
                if (false == rfc2035Quality &&
                    Type >= 2 && Type <= 5)
                {
                    //Should allow for 2035 decoding seperately
                    throw new InvalidOperationException("Type numbers 2-5 are reserved and SHOULD NOT be used.  Applications based on RFC 2035 should be updated to indicate the presence of restart markers with type 64 or 65 and the Restart Marker header.");
                }

                Quality = packet.Payload.Array[offset++];

                if (Quality == 0) throw new InvalidOperationException("Quality == 0 is reserved.");

                //Should round?

                //Should use 256 ..with 8 modulo? 227x149 is a good example and is in the jpeg reference

                Width = (ushort)(packet.Payload.Array[offset++] * 8);// in 8 pixel multiples

                //0 values are not specified in the rfc
                if (Width == 0) Width = MaxWidth;

                Height = (ushort)(packet.Payload.Array[offset++] * 8);// in 8 pixel multiples

                //0 values are not specified in the rfc
                if (Height == 0) Height = MaxHeight;

                //It is worth noting you can send higher resolution pictures may be sent and these values will simply be ignored in such cases or the receiver will have to know to use a 
                //divisor other than 8 to obtain the values when decoding


                /*
                 3.1.3.  Type: 8 bits

                   The type field specifies the information that would otherwise be
                   present in a JPEG abbreviated table-specification as well as the
                   additional JFIF-style parameters not defined by JPEG.  Types 0-63 are
                   reserved as fixed, well-known mappings to be defined by this document
                   and future revisions of this document.  Types 64-127 are the same as
                   types 0-63, except that restart markers are present in the JPEG data
                   and a Restart Marker header appears immediately following the main
                   JPEG header.  Types 128-255 are free to be dynamically defined by a
                   session setup protocol (which is beyond the scope of this document).
                 */
                //Restart Interval 64 - 127
                if (Type > 63 && Type < 128) //Might not need to check Type < 128 but done because of the above statement
                {

                                                 //ProfileHeaderInformation.DataRestartIntervalHeaderSize

                    if ((count = end - offset) < 4) throw new InvalidOperationException("Invalid packet.");

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
                    RestartInterval = Common.Binary.ReadU16(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian);//(ushort)(packet.Payload.Array[offset++] << 8 | packet.Payload.Array[offset++]);

                    //Discard first and last bits...
                    RestartCount = (ushort)(Common.Binary.ReadU16(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian) & 0x3FFF); //((packet.Payload.Array[offset++] << 8 | packet.Payload.Array[offset++]) & 0x3fff);
                }

                // A Q value of 255 denotes that the  quantization table mapping is dynamic and can change on every frame.
                // Decoders MUST NOT depend on any previous version of the tables, and need to reload these tables on every frame.

                //I check for the buffer position to be 0 because on large images which exceed the size allowed FragmentOffset wraps.
                //Due to my 'updates' [which will soon be seperated from the RFC2435 implementation into another e.g. a new RFC or seperate class.]
                //One cannot use the TypeSpecific field because it's not valid and I have also allowed for TypeSpecific to be set from the StartOfFrame marker to allow:
                //1) Correct component numbering when component numbers do not start at 0 or use non incremental indexes.
                //2) Allow for the SubSampling to be indicated in that same field when not 1x1
                // 2a) For CMYK or RGB one would also need to provide additional data such as Huffman tables and count (the same for Quantization information)
                //Could keep TotalFragmentOffset rather than local to not check the buffer position
                if (FragmentOffset == 0 && Depacketized.Count == 0)
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
                    else if (Quality >= (rfc2035Quality ? 100 : 128)) //RFC2035 uses 0->100 where RFC2435 uses 0 ->127 but values 100 - 127 are not specified in the algorithm provided and should possiblly use the alternate quantization tables
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

                                                    //ProfileHeaderInformation.QuantizationTableHeaderSize

                        if ((count = end - offset) < 4) throw new InvalidOperationException("Invalid packet.");

                        //This can be used to determine incorrectly parsing this data for a RFC2035 packet which does not include a table when the quality is >= 100                            
                        if ((packet.Payload.Array[offset]) != 0)
                        {
                            //Sometimes helpful in determining this...
                            //useRfcQuantizer = Quality > 100;

                            //offset not moved into what would be the payload

                            //create default tables.
                            tables = new Common.MemorySegment(CreateQuantizationTables(Type, Quality, PrecisionTable, useRfcQuantizer)); //clamp, maxQ, psycovisual
                        }
                        else
                        {
                            //MBZ was just read and is 0
                            ++offset;

                            //Read the PrecisionTable (notes below)
                            PrecisionTable = (packet.Payload.Array[offset++]);

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
                            ushort Length = Common.Binary.ReadU16(packet.Payload.Array, ref offset, BitConverter.IsLittleEndian); //(ushort)(packet.Payload.Array[offset++] << 8 | packet.Payload.Array[offset++]);

                            //If there is Table Data Read it from the payload, Length should never be larger than 128 * tableCount
                            if (Length == 0 && Quality == byte.MaxValue/* && false == allowQualityLengthException*/) throw new InvalidOperationException("RtpPackets MUST NOT contain Q = 255 and Length = 0.");
                            else if (Length == 0 || Length > end - offset)
                            {
                                //If the indicated length is greater than that of the packet taking into account the offset and padding
                                //Or
                                //The length is 0

                                //Use default tables
                                tables = new Common.MemorySegment(CreateQuantizationTables(Type, Quality, PrecisionTable, useRfcQuantizer));
                            }

                            //Copy the tables present
                            tables = new Common.MemorySegment(packet.Payload.Array, offset, (int)Length);

                            offset += (int)Length;
                        }
                    }
                    else // Create them from the given Quality parameter
                    {
                        tables = new Common.MemorySegment(CreateQuantizationTables(Type, Quality, PrecisionTable, useRfcQuantizer));
                    }

                    //Potentially make instance level properties for the tables so they can be accessed again easily.

                    depacketized = new Common.MemorySegment(CreateJPEGHeaders(TypeSpecific, Type, Width, Height, tables, PrecisionTable, RestartInterval));

                    //Generate the JPEG Header after reading or generating the QTables
                    //Ensure always at the first index of the Depacketized list. (FragmentOffset - 1)
                    Depacketized.Add(packetKey - 1, depacketized);

                    //tables.Dispose();
                    //tables = null;
                }                

                //If there is no more data in the payload then the data which needs to be checked in already in the Depacketized list or assigned.
                if ((count = end - offset) > 0)
                {
                    //Store the added segment to check for the EOI
                    depacketized = new Common.MemorySegment(packet.Payload.Array, offset, count);

                    //Add the data which is depacketized
                    Depacketized.Add(packetKey++, depacketized);
                }

                //When the marker is present it indicates it is the last packet related to the frame.
                if (/*packet.SequenceNumber == m_HighestSequenceNumber &&*/ packet.Marker)
                {
                    //Get the last value added if depacketized was not already assigned.
                    if (depacketized == null) depacketized = Depacketized.Values.Last();

                    //Check for EOI and if note present Add it at the FragmentOffset + 1
                    if (depacketized.Array[depacketized.Count - 2] != Media.Codecs.Image.Jpeg.Markers.EndOfInformation)
                        Depacketized.Add(packetKey, EndOfInformationMarkerSegment);
                }

                //depacketized = null;
            }

            //Allow a PrepareBuffer with tables

            //PrepareBufferRFC2035

            ////PrepareBufferRFC2435

            ////PrepareBufferPsychoVisual

            //Todo - Remove 'Image'

            //Todo, remove or use new overload
            /// <summary>
            /// Writes the packets to a memory stream and creates the default header and quantization tables if necessary.
            /// </summary>
            public virtual void  PrepareBuffer(bool allowLegacyPackets = false, bool allowIncomplete = false, bool useRfcQuantizer = false) //clamp, maxQ, psychoVisual
            {
                if (IsEmpty) throw new ArgumentException("This Frame IsEmpty. (Contains no packets)");
                else if (false == allowIncomplete && false == IsComplete) throw new ArgumentException("This Frame not Complete");

                foreach (Rtp.RtpPacket packet in Packets) ProcessPacket(packet, allowLegacyPackets, useRfcQuantizer);

                base.PrepareBuffer();
            }

            /// <summary>
            /// Creates a image from the processed packets in the memory stream.
            /// </summary>
            /// <param name="useEmbeddedColorManagement">Passed to Image.FromStream</param>
            /// <param name="validateImageData">Passsed to Image.FromStream</param>
            /// <returns>The image created from the packets</returns>
            /// <notes>If <see cref="Dispose"/> is called the image data will be invalidated</notes>
            public System.Drawing.Image ToImage(bool useEmbeddedColorManagement = true, bool validateImageData = false)
            {
                if (IsDisposed) return null;

                try
                {
                    if (m_Buffer == null || false == m_Buffer.CanRead) PrepareBuffer();

                    return System.Drawing.Image.FromStream(m_Buffer, useEmbeddedColorManagement, validateImageData); 
                }
                catch
                {
                    throw;
                }
            }

            //Todo, design see the base class notes
            //protected internal override void RemoveAt(int index, bool disposeBuffer = true)
            //{
            //    base.RemoveAt(index, disposeBuffer);
            //}

            #endregion


            //Todo, System.Drawing may not be available.
            #region Operators

            public static implicit operator System.Drawing.Image(RFC2435Frame f) { return f.ToImage(); }

            public static implicit operator RFC2435Frame(System.Drawing.Image f) { return Packetize(f); }

            #endregion
        }

        #endregion

        #region Fields

        //Should be moved to SourceStream? Should have Fps and calculate for developers?
        protected int clockRate = 9;//kHz //90 dekahertz

        //Should be moved to RtpSourceSource;
        protected readonly int sourceId = RFC3550.Random32(RFC2435Frame.RtpJpegPayloadType); //Doesn't really matter what seed was used, if really desired could be set in constructor

        protected Queue<Rtp.RtpFrame> m_Frames = new Queue<Rtp.RtpFrame>();

        //RtpClient so events can be sourced to Clients through RtspServer
        protected Rtp.RtpClient m_RtpClient;

        //Watches for files if given in constructor
        protected System.IO.FileSystemWatcher m_Watcher;

        protected int m_FramesPerSecondCounter = 0;

        #endregion

        public const int DefaultQuality = 80;

        static List<string> SupportedImageFormats = new List<string>(System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().SelectMany(enc => enc.FilenameExtension.Split((char)Common.ASCII.SemiColon)).Select(s=>s.Substring(1).ToLowerInvariant()));

        #region Propeties

        public virtual double FramesPerSecond { get { return Math.Max(m_FramesPerSecondCounter, 1) / Math.Abs(Uptime.TotalSeconds); } }

        public virtual int Width { get; protected set; } //EnsureDimensios

        public virtual int Height { get; protected set; }

        public virtual int Quality { get; protected set; }

        public virtual bool Interlaced { get; protected set; }

        //Should also allow payloadsize e.g. BytesPerPacketPayload to be set here?

        /// <summary>
        /// Implementes the SessionDescription property for RtpSourceStream
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return m_RtpClient; } }

        #endregion

        #region Constructor

        public RFC2435Media(string name, string directory = null, bool watch = true)
            : base(name, new Uri("file://" + System.IO.Path.GetDirectoryName(directory)))
        {

            if (Quality <= 0) Quality = DefaultQuality;

            //If we were told to watch and given a directory and the directory exists then make a FileSystemWatcher
            if (System.IO.Directory.Exists(base.Source.LocalPath) && watch)
            {
                m_Watcher = new System.IO.FileSystemWatcher(base.Source.LocalPath);
                m_Watcher.EnableRaisingEvents = true;
                m_Watcher.NotifyFilter = System.IO.NotifyFilters.CreationTime;
                m_Watcher.Created += FileCreated;
            }
        }

        public RFC2435Media(string name, string directory, bool watch, int width, int height, bool interlaced, int quality = DefaultQuality)
            :this(name, directory, watch)
        {
            Width = width;

            Height = height;

            Interlaced = interlaced;

            Quality = quality;

            EnsureDimensions();
        }

        #endregion

        #region Methods

        void EnsureDimensions()
        {
            int over;

            Math.DivRem(Width, Common.Binary.BitsPerByte, out over);

            if (over > 0) Width += over;

            Math.DivRem(Height, Common.Binary.BitsPerByte, out over);

            if (over > 0) Height += over;
        }

        //SourceStream Implementation
        public override void Start()
        {
            if (m_RtpClient != null) return;

            //Create a RtpClient so events can be sourced from the Server to many clients without this Client knowing about all participants
            //If this class was used to send directly to one person it would be setup with the recievers address
            m_RtpClient = new Rtp.RtpClient();

            SessionDescription = new Sdp.SessionDescription(0, "v√ƒ", Name);
            SessionDescription.Add(new Sdp.Lines.SessionConnectionLine()
            {
                ConnectionNetworkType = Sdp.Lines.SessionConnectionLine.InConnectionToken,
                ConnectionAddressType = Sdp.SessionDescription.WildcardString,
                ConnectionAddress = System.Net.IPAddress.Any.ToString()
            });

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, RFC2435Media.RFC2435Frame.RtpJpegPayloadType));

            //Indicate control to each media description contained
            SessionDescription.Add(new Sdp.SessionDescriptionLine("a=control:*"));

            //Ensure the session members know they can only receive
            SessionDescription.Add(new Sdp.SessionDescriptionLine("a=sendonly")); //recvonly?
            
            //that this a broadcast.
            SessionDescription.Add(new Sdp.SessionDescriptionLine("a=type:broadcast"));
            

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Generic.Dictionary to support various tracks

            //Create a context
            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1,  //data and control channel id's (can be any number and should not overlap but can...)
                sourceId, //A randomId which was alredy generated 
                SessionDescription.MediaDescriptions.First(), //This is the media description we just created.
                false, //Don't enable Rtcp reports because this source doesn't communicate with any clients
                1, // This context is not in discovery
                0)
                {
                    //Never has to send
                    SendInterval = Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan,
                    //Never has to recieve
                    ReceiveInterval = Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan,
                    //Assign a LocalRtp so IsActive is true
                    LocalRtp = Common.Extensions.IPEndPoint.IPEndPointExtensions.Any,
                    //Assign a RemoteRtp so IsActive is true
                    RemoteRtp = Common.Extensions.IPEndPoint.IPEndPointExtensions.Any
                }); //This context is always valid from the first rtp packet received

            //Add the control line, could be anything... this indicates the URI which will appear in the SETUP and PLAY commands
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=video"));

            //Add the line with the clock rate in ms, obtained by TimeSpan.TicksPerMillisecond * clockRate            

            //Make the thread
            m_RtpClient.m_WorkerThread = new System.Threading.Thread(SendPackets);
            m_RtpClient.m_WorkerThread.TrySetApartmentState(System.Threading.ApartmentState.MTA);
            //m_RtpClient.m_WorkerThread.IsBackground = true;
            //m_RtpClient.m_WorkerThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            m_RtpClient.m_WorkerThread.Name = "SourceStream-" + Id;

            //If we are watching and there are already files in the directory then add them to the Queue
            if (m_Watcher != null && false == string.IsNullOrWhiteSpace(base.Source.LocalPath) && System.IO.Directory.Exists(base.Source.LocalPath))
            {
                //Get all files in the path
                foreach (string file in System.IO.Directory.GetFiles(base.Source.LocalPath))
                {
                    //If there is not a codec continue
                    if (false == SupportedImageFormats.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) continue;

                    try
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Encoding: " + file);
#endif
                        //Packetize the Image adding the resulting Frame to the Queue (Encoded implicitly with operator)
                        using (var image = System.Drawing.Image.FromFile(file)) Packetize(image);
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Done Encoding: " + file);
#endif
                    }
                    catch (Exception ex)
                    {
                        Common.ILoggingExtensions.LogException(RtpClient.Logger, ex);
                    }
                }

                //If we have not been stopped already
                if (/*State != StreamState.Started && */ m_RtpClient.m_WorkerThread != null)
                {
                    //Only ready after all pictures are in the queue
                    Ready = true;
                    State = StreamState.Started;
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
                State = StreamState.Started;
                m_RtpClient.m_WorkerThread.Start();
            }

            //Finally the state is set to Started
            base.Start();
        }

        public override void Stop()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Stopped");
#endif
            base.Stop();          

            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Created -= FileCreated;
                m_Watcher.Dispose();
                m_Watcher = null;
            }

            m_Frames.Clear();

            SessionDescription = null;
        }

        /// <summary>
        /// Called to add a file to the Queue when it was created in the watched directory if the file was an Image.
        /// </summary>
        /// <param name="sender">The object who called this method</param>
        /// <param name="e">The FileSystemEventArgs which correspond to the file created</param>
        internal virtual void FileCreated(object sender, System.IO.FileSystemEventArgs e)
        {
            string path = e.FullPath.ToLowerInvariant();

            if (false == SupportedImageFormats.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) return;

            try { Packetize(System.Drawing.Image.FromFile(path)); }
            catch { throw; }
        }

        /// <summary>
        /// Add a frame of existing packetized data
        /// </summary>
        /// <param name="frame">The frame with packets to send</param>
        public void AddFrame(Rtp.RtpFrame frame)
        {
            try { m_Frames.Enqueue(frame); }
            catch { throw; }
        }

        /// <summary>
        /// Packetize's an Image for Sending.
        /// If <see cref="Width"/> or <see cref="Height"/> are not set then they will be set from the given image.
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        /// <param name="quality">The quality of the encoded image, 100 specifies the quantization tables are sent in band</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Packetize(System.Drawing.Image image)
        {
            try
            {
                if (Width == 0 && Height == 0 || Width == image.Width && Height == image.Height)
                {
                    Width = image.Width;

                    Height = image.Height;

                    m_Frames.Enqueue(RFC2435Media.RFC2435Frame.Packetize(image, Quality, Interlaced, (int)sourceId));
                }
                else if (image.Width != Width || image.Height != Height)
                {
                    using (var thumb = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero))
                    {
                        m_Frames.Enqueue(RFC2435Media.RFC2435Frame.Packetize(thumb, Quality, Interlaced, (int)sourceId));
                    }
                }
            }
            catch { throw; }
        }

        //Needs to only send packets and not worry about updating the frame, that should be done by ImageSource

        //This logic is general enough, that it could go in RtpSource...
        internal override void SendPackets()
        {
            m_RtpClient.FrameChangedEventsEnabled = false;

            unchecked
            {
                while (State == StreamState.Started)
                {
                    try
                    {
                        if (m_Frames.Count == 0 && State == StreamState.Started)
                        {
                            if (m_RtpClient.IsActive) m_RtpClient.m_WorkerThread.Priority = System.Threading.ThreadPriority.Lowest;

                            System.Threading.Thread.Sleep(clockRate);

                            continue;
                        }

                        int period = (clockRate * 1000 / m_Frames.Count);

                        //Dequeue a frame or die
                        Rtp.RtpFrame frame = m_Frames.Dequeue();

                        if (Common.IDisposedExtensions.IsNullOrDisposed(frame)) continue;

                        //Get the transportChannel for the packet
                        Rtp.RtpClient.TransportContext transportContext = m_RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier);

                        //If there is a context
                        if (transportContext != null)
                        {
                            //Increase priority
                            m_RtpClient.m_WorkerThread.Priority = System.Threading.ThreadPriority.AboveNormal;

                            //Ensure HasRecievedRtpWithinSendInterval is true
                            //transportContext.m_LastRtpIn = DateTime.UtcNow;

                            transportContext.RtpTimestamp += period;

                            frame.Timestamp = (int)transportContext.RtpTimestamp;

                            //Todo, should not copy packets

                            //Take all the packet from the frame                            
                            var packets = frame.ToArray();

                            //Clear the frame to reset sequence numbers (could add method to do this)
                            frame.RemoveAllPackets();

                            //Todo, should provide access to property or provide a method which updates this property.

                            //Iterate each packet in the frame
                            foreach (Rtp.RtpPacket packet in packets)
                            {
                                //Copy the values before we signal the server
                                //packet.Channel = transportContext.DataChannel;
                                packet.SynchronizationSourceIdentifier = (int)sourceId;

                                packet.Timestamp = transportContext.RtpTimestamp;

                                //Assign next sequence number
                                switch (transportContext.RecieveSequenceNumber)
                                {
                                    case ushort.MaxValue:
                                        packet.SequenceNumber = transportContext.RecieveSequenceNumber = 0; 
                                        break;
                                    //Increment the sequence number on the transportChannel and assign the result to the packet
                                    default: 
                                        packet.SequenceNumber = ++transportContext.RecieveSequenceNumber;
                                        break;
                                }

                                //Fire an event so the server sends a packet to all clients connected to this source
                                if (false == m_RtpClient.FrameChangedEventsEnabled) m_RtpClient.OnRtpPacketReceieved(packet, transportContext);

                                //Put the packet back to ensure the timestamp and other values are correct.
                                frame.Add(packet);

                                //Update the jitter and timestamp
                                transportContext.UpdateJitterAndTimestamp(packet);
                                
                                //Todo, should provide access to property or provide a method which updates this property.

                                //Ensure HasSentRtpWithinSendInterval is true
                                //transportContext.m_LastRtpOut = DateTime.UtcNow;
                            }

                            packets = null;

                            //Modified frame HasMissingPackets because SequenceNumbers were modified and the SortedList's keys were assigned to the previous sequence numbers

                            //Fire a frame changed event manually
                            if (m_RtpClient.FrameChangedEventsEnabled) m_RtpClient.OnRtpFrameChanged(frame, transportContext);

                            //Check for if previews should be updated (only for the jpeg type for now)
                            if (DecodeFrames && frame.PayloadType == RFC2435Frame.RtpJpegPayloadType) OnFrameDecoded((RFC2435Media.RFC2435Frame)frame);

                            ++m_FramesPerSecondCounter;

                        }

                        //If we are to loop images then add it back at the end
                        if (Loop)
                        {
                            m_Frames.Enqueue(frame);
                        }

                        System.Threading.Thread.Sleep(clockRate);
                    }


                    catch (Exception ex)
                    {
                        if (ex is System.Threading.ThreadAbortException)
                        {
                            //Handle the abort
                            System.Threading.Thread.ResetAbort();

                            Stop();

                            return;
                        }

                        //TryRaiseex

                        continue;
                    }
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

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RFC2435Frame class is correct
    /// </summary>
    internal class RFC2435UnitTest
    {


        public void Test_CreateQuantizationTables_And_DetermineQuality()
        {
            //Loop all Quality values from 1 -> 100
            for (int i = 1; i <= 100; ++i)
            {
                byte[] tables = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.CreateQuantizationTables(0, (uint)i, 0, false);

                int determinedLumaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 0),
                    determinedChromaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 64);
                    //averageQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineAverageQuality(0, tables, 0, tables.Length);

                //If the quality is not determined correctly from the tables created this is an exception
                if (Common.Binary.Abs(determinedLumaQuality - i) > 5) throw new InvalidOperationException("Invalid Luma Quality Detected");

                if (Common.Binary.Abs(determinedChromaQuality - i) > 5) throw new InvalidOperationException("Invalid Chroma Quality Detected");

                //if (Common.Binary.Abs(averageQuality - i) > 10) throw new InvalidOperationException("Invalid Average Quality Detected");

                tables = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.CreateQuantizationTables(0, (uint)i, 0, true);

                determinedLumaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 0);

                determinedChromaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 64);

                //If the quality is not determined correctly from the tables created this is an exception
                if (Common.Binary.Abs(determinedLumaQuality - i) > 5) throw new InvalidOperationException("Invalid Luma Quality Detected");

                if (Common.Binary.Abs(determinedChromaQuality - i) > 5) throw new InvalidOperationException("Invalid Chroma Quality Detected");

                //if (Common.Binary.Abs(averageQuality - i) > 10) throw new InvalidOperationException("Invalid Average Quality Detected");

                tables = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.CreateQuantizationTables(0, (uint)i, 0, false, true, 100, true);

                determinedLumaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 0);

                determinedChromaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(false, tables, 64);

                //If the quality is not determined correctly from the tables created this is an exception
                if (Common.Binary.Abs(determinedLumaQuality - i) > 5) throw new InvalidOperationException("Invalid Luma Quality Detected");

                if (Common.Binary.Abs(i - determinedChromaQuality) > 7) throw new InvalidOperationException("Invalid Chroma Quality Detected");

                //if (Common.Binary.Abs(averageQuality - i) > 10) throw new InvalidOperationException("Invalid Average Quality Detected");

                tables = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.CreateQuantizationTables(0, (uint)i, 1, true);

                determinedLumaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(true, tables, 0);
                
                determinedChromaQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineQuality(true, tables, 128);

                //averageQuality = Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.DetermineAverageQuality(byte.MaxValue, tables, 0, tables.Length);

                //If the quality is not determined correctly from the tables created this is an exception
                if (Common.Binary.Abs(determinedLumaQuality - i) > 10) throw new InvalidOperationException("Invalid Luma Quality Detected");

                if (Common.Binary.Abs(determinedChromaQuality - i) > 10) throw new InvalidOperationException("Invalid Chroma Quality Detected");

                //if (Common.Binary.Abs(averageQuality - i) > 10) throw new InvalidOperationException("Invalid Average Quality Detected");
            }

        }

        public void TestDecoding()
        {
            byte[][] jpegPackets = new byte[][]
                {
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e26c6b82a24dce0864000000004132381f0054ffffe5d6a414c5a916980e029d4829d400628c514b400507a514374a40463bd4a053179c7d6a414864b10e33daa602a28ba63f1a9450264d6eb9901f4a9a6fba05456df7cfd2a4b8246d18a04478e0d14849c1e307de8f98fa0fd6980ea514d19c727f2e29c00c7393f8d0038714fcfcabf53fd2a30a318ebf539a7055ce42afe5400ec80704807eb4a0823820fd29013da9f92437278534804073d9bf234bf81a3340a6028cf391d7dc52e4e3803f3a052e69000dddc2fe7ffd6a5e72064027f1ff003d68a33cafe3fd28017e6c755ffbe7ff00af4a037f787e5ffd7a334b400a79183c7d29029ebb8fe94a3a75a5eb40114884ff0019fd2a0492f6d8ba40c8413bbe64cfb7f4ab6464f14dda777e1fd4d00402f7551c6e84ff00db214efb76ad9e1e1e9de21530534bb4d0043f6fd5874687fefd0a8eeaff005492dda291e2d8e30711804fe3568a9c5412c6cd4f4032824bceec7e140593241038f7ad1f20fa5279077671d40a7a08cf226eca3f127fc29009fba2fe67fc2b47c86f4a3c83e940144799dc0fc29a5dc0cf96c7f2ad0f24fa527927d280288924ce3ca61f88a77da9a32003267fd9563fcaae793c8e29a22c81914015a3d69d64d897b2ab7a6f22ac9d62f3690d792952390cf907f3a0c4076a4f287a5164045f6b59410c903e7ae23507f31cd355adf3f35b447fe04c3fad4e60523a537ecd1ff7450529497521f2ad59b263907b2c800fd41ab766d696cce544c778031907f962a1fb32f3d7f334f8ad3272ae411ef43437524d59b3654ee5070467b375ac6d7acf7a19d3a8fbdfe356196ef18172c47e14d93ed6c9b1ca3afbafff005e85a1072b22ed07a5462674380d91ee2aeea16d240edb9786e47bd669041071d2958b4ec5e5a7ad3074a914532478a5a414b40052d14521852374a5a6b9e0d0009ce2a414c5eb4f1523278f951ea38a90531704023d29e2989966dbf8a966fbe3e945b8c2b1f7c536627cde3047d6810946690838ce47e549827ab1fc2801c7a7f9f5a75340ec4934a1476247e26801dd29770032481f8d376af7507f0a55c0381c71db8a0072ba1fe35fce9ea701860f2a7a0a6034ea0050dc7009fc28dc47f09fd3fc6814500286c8fba47d697e6c8c053f8fff005a85383f851400a198e0607e7ffd6a71dc71c8e33dbe94da5a003e7fef2ffdf27fc69c377f787e5ffd7a4140e2900edd8ea7f4a7727b9a631f947d453b3400b8ff0069bf4a503dcd3734b9a00500e3ef1fd29467fbc69b9a33c50029381924fe9487af7a6b1f90fe1fcc519cd301dc01de8279c8eb4dcd2d001923d0d197edb7f2a3345001cf723fef9ffebd2faff85349e07d7fa1a5a003273f747e74a07a85a334669dc0085f4cfe149853fc3fca9734669008557b8a4d8a4f43f952e73f9d2f14ee030c480f5c53d220071400339a7601ebd6810bb290ad2ed19e9f95371900f3f9d0052d52d4cf6c71f7979fad72b30dbb97bd76e41f5ac2d4f4cc192753f2804914d6a34670e9520a60a916801c28a296800a28a5a004a6b73c53e98c32690c72f534f14d4e87eb4f03240a4326518e076269e29a3900fad3c5325966dc1f2c9f7a6487329a9213fbb5a89c1f30e0f7f4a5d400d148727be31e9401fed1fd2980ea75315413d4fe74b8fae3dcd201d4ece08edc7f8d302803ee8fca9c303b0fca9806f51fc429db8638607e9464f1c9a72f240340081b3d8fe4680c3d0fe469173b17af4a7520004b03856e7a76a70247546fd3fc693f0a5a00371e9b1bf4a033ff0073f5a514a38073400996271b571fef7ff5a9d96e9b57fefaff00eb52528a0018bed230bf9fff005a90193d1697a52f7a004fde7aa0fce973274cafeb477a70e87fcf7a40372fdcafe5465b1d4528eb45301a7710471f97ff005e8cb67ffad4ec51400dcb7a0a5cbff747e74ea3191400d05b1d07e74b96f6fcff00fad4eeb49400d25b03a139ed9ff0a379cfdd34ec514009b89e369fcc51bbdbf514b46060"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e36c6b82a24dce0864000005904132381f0054ffff50026f19e87f2a5cfb1fc8d181463da8010301c1207e346f5fef0fce9481e951cb3470ff00ac9029f4cf3f95004a187ad3b354fedb69de61ff007c9ff0a9167b76008962fc58034c45acd266abbc91201c924f6534c92e4229f2c166f73c5202cb100124e07a9acbd4b50b77b3963490ef3c0e383f8d57ba9ae66c873f2fa0e05526423a8229ad067fffd0e6c548b4c1520a621d4514b4c0051452d2012987ef0a90d33196a431c83826a4438653ef4d5fbb4e033fce901328c000f514f1483ad380aa116a21845a84f2e6ac27dc5c7a556e72791f95480a69074a0a9f523e94801fef1a603a9d4dda3dff003a5031d09fccd003852d2607700d1b5777dd5fca801c08f5a55650c0b30033eb48147a0fca9e09c75a0062b2850370e9eb4f0ca7f887e7467d697f1a003231d68c8f7fca9c3a3727ee9fe547e3400d0c3d0ffdf269d95c1eb9f4da6968a402647be7e86947e3f952e3de96801b9e78068ddec7f1c53c7193ec7f95371c50026f3fdd3f98ff001a50f9cfc87f31fe34b8a5a00404f5d87f31fe3464ff0071bf4ff1a5a3bd0210b3019f2cfe63fc69771e30a48c7b7f8d2ae7701ef4d8ff00d5afd2818b96ec8c7f2ff1a3271f71bf4a7514084ddfec9fca82ca3d7f234b9a5ebde801bb97b13ff7c9ff000a370ff6bfef934e1d40f7a6a93b475e94c00b0079047d41a37a91d452d19c77a40207539e40e7b9146f51fc4bf9d2e4d35dd510b390147249a00cfd5351f20f93037ce47ccc3f87ff00af58de61ce49249a8679ccb3bb9eacc4d393e6207a9c55dac3274cbb6d5193e956e3b60a3748727fba0d321dd0ae026d1ea475ab1b95d772f38ea3d286c405fd3a5391b70eb51a8c92a0f5e94c864d9361ba1a9026f95ced270e2a3911979ebef4eb888839fc8d352e0f46eb4c0a02a45a81240c323f1a90381401352679a8bcca50fcd171930a5a6a9a750021a60fbff853e9a07cf9a0648a3814f41938f634d5e83e94e4e1c7d6a409c528eb40a515422daf083e955c77ab2d800fb0aad8273cd20148e47d2905260ff78f1f4a500ff78fe9400e14a29b838ce7da9c063de80140a5fe2fc29001e94bb47a7ea6980a29c3d69a140e31fa9a70008c60700e334805a00a4dab8fb8bf952854fee2fe54c0763e56ff0074e3f2a5c7b534469c7eed78f6a5d887f817f2a005e9d6972319cf140007455fc8538e319c0e0e3a5201a31cf238a3207714bf97e54b9fa7e54009b970791c8346e1ea297d38fd2941fa7e5400dde83ab0fce93cc41fc43f3a7d2124e39ffebd0037cc4e9b87e746f43c6f5fce9f934162473400d8dd19c00c0f3d8d355d0281b8640ee7a54993fe4527e03f2a004f313fbebf9d2865ecc3f3a383d71f95040c1f9474f4a042e41c7239e9ef4b9c5376a631b17f2a5d898e113f2a431473401814df2a31c845fca8d898e514fe14c43a8a6f969fdd1f951b107f08a005c5676bae534d6c1c0660a7dfbff4ad0daa3b7ea6b3f5c883e9923027e421b193eb8feb42dc0e633cd6869d1237ef646c60e17dcd45a7d9fda5fcc7e224ebc753e95a6ca493b0231ff6b93f8537d860c7e5e0e47e62a03218dc3af1ea29cb3491365a31b3a103a53e4891d77c6721bb51610b90db644c61b8fa1a64ea7938c11cd560ed03151f74f6ab41c4a80f714016addc5c5be09f987bd56910027b54769298ae31d89c62afdc440fcca3834d7603934b96120feee79fa55f06b24301d56b4a0cf960b1249ee6a6dd06a362714a29b4a290c951aa4cd400e29e1a9dc093b5087934d07342fde3f4a00997ee8a72fde07d0d347000a7c7d4ff009ef40c9c538534528a64975feeb7d2ab0a9d8fca73e9558671d6900ea052156f5ebfe7d2902b7f7cfe9fe1401263e51f5ff1a514cc1c6371fd29429fef9fd2801e053853304f563f90a7153c7cc7a7a5301c29c3a1fa530039fbc6976f1f78f3400e5e94b4c0a3fbcdf9d3828f56fce801d4b4985c1c96e013d68c0ff6bf3a403a97"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e46c6b82a24dce086400000b204132381f0054fffff83fe043f91a610b8eff009d280a3fbd8ff7a801d9a334d2abfed7fdf546d4ff006bf3a007514c2ab83f7b804fdea36af4cb7fdf5400ff006a0e083cd370bfed7fdf546140c7cdcf5f9a801d4714ddaa07f1ff00df549b54ff007ffefaa007d19a6054f57ffbea8c023abfe74807d04fcadf4a60007f13fe74b8c64091b07e94c07f18a01a660e7efb0fc07f851b7fe9a37e43fc29012525371e8edfa7f851b4e4e5db8fa7f85310fa4ec69a41cfde3fa7f8520c83f78d201c4d473a09609232400ca5727a0c8eb4ac4ff7bf4a8249776554823d453b814ed63482d520ce081cfb9ef4e28072a391cf14d650463a9a760140dc8f7140036d201c707f4a80a142c8bd3ef2fb1a9c125581fd2a22dcfb8aa02b4c04aa4e30c2a04768ce71c54ec76b93400b93e9d7148632470cc1d7f1ad5b7904d6c0e791c1acd923001c0c77a9f4f930ac87a75a13d44cffd1e018735aabd2b2dba8ad45e940d8f14b4828a403e9453452d00480d2a7de63ed4ccd393bd3113835245cb5462a48bef50b702714e5eb4c14f4fbc3eb4c92d9fbadf4aae3a5586fbadf4aaa338edf95201cddbe9452127da8e7da818e14e14d00fa81f85033ea3f2a0078a7139c7b0c5339f51f952807aeefaf14c43c52f14c00fa8fca972402723039e9400fa3eb4d00fafe94b83fde1f95031dd9bfdd3fca97a5306ef518208e9465b1fc39a403e8cd34163d428fc68f9bb053f8d003e9293e6edb7f5a4c9c76fce801dd430ff64ff2a3229a0b772a7f1347cc7a6dfce801d9a334d39cff000fe67fc28c1f55fccff85003b34669b83eabefc9ff000a5c1f55c7d4ff008500283c8a4e9c7bd261bfd9c7d4ff0085261b24928727fbc7fc2801d9a5cf34986ff67fefa3fe1498600e42f1ee7fc2900ecd28351e5b3fc3ff007d1ff0a0eecff0ff00df47fc2981266827e63f5f5a8f2c38c2fe7ffd6a425813f77f3a404b9a696a8f79f41f9d4534db46de869004f30cec04e3be2a14932c78c7a544ec7b8eb429c8aa403f8009c71dfda9c32808c6e5ef4d00920746ec734e39ce7eeb77aa4891a7e53b94f06a29796c8e2a46ef8fc4544fd29b1a2bc9d314c57c01ec69d272bef50ab135032c96ca1ef4c47d8fc1a01dd190bf7b1d2aab9753c8a10194dd6b517a565b75ad4031414f71d4a2929452014528a414b400ea7af7a60a7af7a6226ef5245f7aa3a922fbd4202714f8ff00d62fd453053e3e245fa8aa24b6c3e571ed55874ab2ff0075be955097ed8c7d2900ea29997cf6fc07ff005e9416effcbffaf40127f0e7de814c0cd8c1e99a505bd07e74012528a8f737f747e74ecb7a0fcfff00ad400f0697aab7fba7f953327d07e746e3b4fca7a1140126697351873fdc6fd29777fb27f4a007e68cd3777b1a4dc7fbadfa5031f9a527f9d30b74055bf2a378c1f95bafa5003f3453030feebfe546f07b1ffbe4d003a8a6eef66ffbe4d2e7d9bfef93400b9a3b1e33d3f9d349f66ffbe4d058608c1e7fd934807514ddc3d0fe546e1eb400e3499a6ef5f5a4f317fbc2810fcd19e0fb8351f98bfde14d32af76a00973485aa1332e3ef0fce9a6653d0d219317a6990d426418f5a8ddd88eb8149b1a43e5b9daa554fcdd33e95485cb070b2e39fe2ff1a7b1a608f77515372f97427dd81c83b73c8ee28385c11f75b91ef512830e327e4f53dbff00ad522edc9560769ea07f31ef5ac75337a0866f2d4b7240a48f5156f9641c763515cc676ed5391fceb3d8943822af624da322939073ed51b904706b285e14e0838a78be51f36e1523b16e4e149aa72c9b109ce297fb421c105bf0c550b99c4bc20217de95865986e9b821b9ab22549461bad62d488ee07cac7e9400d6ed5a8bcf3596ddab4a1ff56bf414ba14f724a5ef45148428a5a414b40c70a70ea7e94d1da9c3a9fa53113d4917dea8874152467069f502714f4fbcbf5a60a72fde1cd324bac72aff004aaa3a54cc7e56fa55604fd45201c4fa500d34b1c7ddcfe34819b3"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e56c6b82a24dce0864000010b04132381f0054fffff77f5a00929d5182719c714e073d8d031d4bdcd373ed4e279ffeb502141a5a6823dff2a51d3a11f85301d45276ea3f3a3728ea40a431d9c0a5a6ee5c1f987d3346e5c7de1f9d003e8e948083d0e47ad008f5a042d1476a3f1a06049d8719cf1fce8cfbd1fc2df87f31494085dc71d4d1b8fad373de93340c76e3eb49be9a4d34d201c5cf3834cde71d4d2134d270326900e2e7b1a6b4840e4d44d30e8bc9a66d673f3526ca516c93ce24e14fe34e5567fbc49a548c0a99462a7565a8a433ca02a274c022a777da0e0556676249c50323f28934e58c8a91589ed4166a120b8c75f9483d0f159d1cdf6693c9909f2fa29f4f6abd2be0649a856249183c8623e818918fc856a8ce43649719eebe955e4d8e7fbdfceb44db41275b8419ff006bfc6952c628db2971183ea581ad2cccae604ef1a292b8354ddcb9c9fcab6b58b2fdcf9e2589994fcc170091d33c562e322a5ab14840334edb428a91690c848a54ce78a95d011c546a763f3d2803ffd2e04fdd15a507faa4ff00747f2ace6185fc6b42d8ee850e7b629805c4fe4a0200249c60d5717ef9e5171ed9a2fcf2833eb552a40bc35019ff005440ff007b3527dbe1cf01ff00103fc6b368a606bfdb2df1feb47e47fc2a78dd24c9460dc763586ab9fe203eb5a3a6295f34641e9c839f5a76d00d25fba3e94f4ebfe7d6a35e829ebf7852ea05814e069a296a892d1380df4a80743531ef55d4919e32290c75029b939e94a1b9fba6801ffc3f8d2d341f969c0e7b1a005a5a4e9d69411f4fad021714e1de99b973d47e74ecf0453187a5281834528a0051fa51f8500d1f4a004dabe83f2a36af1f2af3df14bd694f6fc7fa521081547f0afe548550ff0008a75140c618d08c6da6ac11a8c2a607a0a9290f1400d31a63a7eb48513fbbfa9a7122a369157bd201485f43f99a6909e873f5a699189e17f3a6ec24658fe149b4528b06741c0049fa9a80ab31e4f156360c7149b38a9bb2d4522248c0a9940f4a43c0a4078a56289460d2e714c069bbc034c43d8e2984f34d3275a89e6029d809d719a64a76f3daaac972171cf354afaf5f688d4904f24d52422d2cc5e4dc4371d306ac09037024247a15008fe79ac8b7bac48be601e9bb15a22fe204e0003b605691b33195c9c24cc542ca013d8afb5485275e1a407fe00b555efa238c4ae08fee81c5385fdbb280f2c84f73cd55913a84aaecacace30c307e51d2b9d9a330ccd193d0f5f5add92e6d9ba3c86b335058982bc4493d1b2293486afd8a629ea6983919a70a82897b544eb5229a185301b263cafd6acda902dd3d79cfe755ca3328152421a35c75a8191de106618ecbcd57a9e546672d8eb4d68994904107de9a8b15c8a9f1ecddf38247b1c50c9800e73ebed4f8770395e9f81f7fe95496ba85c7791bbee06c9e83afa7f88ab7a6a85f34039cedea31dbff00af54c8f9b9e08c01807d3ffad57ac881348a083919c83ee47f9f6c5392424cbebd053d3ef0a8d4f14f53cd665966969a2973544167a8f7a857bd48a781eb8a8416c9f978a9ea31d4b4c2cdfddcd01cff0074d302414e15187cf634e0c33dff002a0078fad2ff001114cde3b9fce94b0dfd410475cd003e82148c1507ea29052170a2801f85c63031f4a8257f98220efc9a492e02b0fee914d170992697a1a463d5976309b39a46319e15ab366be646564380a0e7dfa5576bd6770ea31ea2914ec6fa44ac9f78e69ad132ff00137e9542def43460670c0d69c13aca8324669ea269321c7fb47f4a4fab1fcaa6c2b923a1151c96fcf2c78a5727908d9c01f7b9fa530b3b1f947e952a46818014e718ed4aeca505d4ac6373cb1fca85451daa53934dc019e691495b6131934bb714d2d8a6efc7534ec03ce05349c75a8da403a530c993cd1615c91a8c8c542d2530cdd853b0ae4acc73d6a17723bd2fcc7a71f5a679633963ba98ae337b374e698e0f526a72706a1734137206cfa552bc189578c7cbfd6af1aa57873301e8b40884"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e66c6b82a24dce0864000016404132381f0054ffff54ab92a00f4a881a9e219142286ed39ebcd1b5bd6a62bcf4ed46da762799918527bd0d16e523352e29d8a2c2bb3331b588a51525d2ed9c9ec79a8fbd302453cd39ba546a6a4ea2802c04f6a369a9714d2547522a0a23d84f6a3611c8e3e94e32a8ef4c338a3501ad0ee1b7a0a58a31164e18fd1b149f68fca9a66f73549b42b21cca36eddc71fed0e9d7fc6a7b5e26e0e783fd31fa0aabe771de93cdc76aae662b1b0b923a5381c1e6b14cb4799e82a0a3a0de80f2cbf9d1e7443aca83eac2b037b7f74d26e3e94c563a51756eaa333c7d3b3034c49e220b09171ea4e2b9e0fc82714a650c72319c629580e83ed5075f393f3a05d5bffcf64ffbeab9edc7dbf3a4dcde869858e8fed70631e727fdf54f173013feba3ffbe85731b9b1d0d1bcfbd0163a913427fe5ac7ff007d0a63dc463a3a9ff810ae677fd68f30fbd034748668f190c9cfa11514b70369c3027eb581e67b9a3cc38ea6958ae6b6c8d9331231511663d2b2f79f5346f3ea6958398d0604f079a31c702b3f71f5a37fbd3b05cd152c3b60d5db6bb652339ac2de7fbc68dedfde340ae75f0dd2b364b01f8d5cf36271f7c67eb5c2ef6c1f98fe74798d8fbe7f3a7a05cec4baab7de1f9d23cc3b11f9d71fe637f7cfe749e637f7cfe74ac87cc755e70276f269a5ce7815cbf9aff00df3f9d1e6bff007cfe740731d2b39eb511973c0ae7fcd7fef9fce8f3e41d1cd02b9bdb656e8a7f1a4f2a4279e2b105d4dd9cd385e5c0e923502b9b62203a827ea69c32bc04c0fad627dbee78fdeb63eb4a2fe7fef9a00d92c7fbb4c2c7d2b306a32f7c7e54e1a8b13caf14845e2c7d2a176f6a87edc87b1a5fb446dfc58a000b7354aeff00e3e5b9cf03f955ddc846778155e4b69657670cadec0d302a8ab76e32bef9a85ada6542761fcc1abba0f9c9a86d21d4321e08233fe7142436f4145bccc462190e7d10d4c34ebb38c40dcfa903f9d6e07dc33bb20d2f9880805c027a735b7b331e731d747ba23398c7b16ff01522e88f8f9a7507d00cd69fda2219fde29c7a734a6e23c9037363d14d57b342e7673bad699f65b68e60fbfe6dadc63af4fe46b1fd2bafd7e30fa5cc7fb8430fcf1fd6b901d2a6714b62e2ee28352a9a82a5435051ffd3e28c92375e3f1a72c13b8cac6e47a8535d0c70c51f31c6aa71d40c549bb91ce695c0c05d32e588cc640f52c2ac268929fbcf18f71935b05801c9a703d7b5176332468673cce07fdb3ffebd4aba1c381ba57fc0015a4082dc7ad29231eb4aec0cf1a25be4fef66cfd47f851fd8b075f326c7d473fa568839efd4e7eb485875cf5e28bb028ae8d6ffdf94ffc0850ba3db02325cfb16abe0f1cf1c7eb46e19c03faf7a2e053fec7b3233b1bf163c528d26c87fcb1e3fde273fad5cce013499ec3a0a40561a65a60fee138f5a71d36cc83fe8e9cf3c2e3356437048e734bbb9c023f2a00a8747b23d2118ea4ee34d3a359124f94718e30c7fce6af170727d4d359f923a7e14c0a2745b5ec1c64766a61d0edcf49261f423fc2b44b020e467f1a370ec3b73c52bb03306830923f7b311f5193fa507408ff008677c7d2b5377cd8e3fc68046d04019ce3a51760639d0307fe3e0f5feed29f0f92702eb07d3cbcff005ad72d8c734a1b04f1f951760629f0fb6de2eb27feb9ff00f5e81a0371fe94bff7efff00af5b6a49c81d7d3d7f0a427e5f5c1efda8bb0315bc3edc95b907d3f77ffd7a4fec0931cdc203feed6e9249e831fca82df3727f1a2ec2c617f6036de6e577771b334dfec1971feb90ff00c06b7f23247f5c1c527054e401ebe868bb0b187fd832647fa427fdf34cfec2b8c91e647f956f1395cf71d7d7ff00af4a5b1d7eb47330b1cf7f62dce0fcd167f1ff000a43a25d7f7a13f427fc2ba1eadd7ebeb4700e32083d3068e660738744bbf48b1fef527f62dd7fd32fccff00857464e78eb9a550303a1039e3a8ff000a39981ccb68d7833b62538ff6a9ada4de29c0b7cfd18574fbb3ce707d"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e76c6b82a24dce086400001bd04132381f0054ffff85030dce3af5c7f9e69f330396fecbbcff009f7f6cef1fe34d6d36ed5771b67c7b735d59c6d05719f6ff0039147b8e413f9d2e6607206d2e075b79bfef83482de563858a427d94d763c03f30ea70783cff009fad2329f9b8c8cf6ff3c53e6038b23071bb9f7a073fc42bb32028278fa8fe9f9d364860914ac912360e4ee03147301c7e1bd47e7461c73b0e2baa7d2ecdc11e4201d7e5183fa74a8a4d12c88e2364279dc18f1ec339a7703992e47506944b8ade7d023c9549e507ae1b0703dfa540de1f982b32cd1be0e065719a77423316e4af4623f1a992f39c9623f5a7cba45d267300600e32adfe3555ed5d090d1c898eb95e28d00d08eed41182bf435723be242ef8d5c0ea76e78fa8ff000ae7f61fe1707f1a50f2c67b8aa52627147427517ce638d703d39a9e1fb7dd10d0a0dbdd83ae07d7935cf25e927f78037b9ebf9d5b86ec160cb294707209ea3f11cd5a9bea4b876352f34bbe7b1b89279630150b6d5c9c81cfb7a5727d09ae98ead76b6cf1485648e4429b98648041ee3fad73d2dbc91927195f5155269ad184535b915394d32941e6b328eb81c9e7ad0c493d3005333b48f5a5393cf4ac8a1e0f1803da80c0672718a457ce00eb8fd68c6d6c1c03400e624f183e94aac074391d78a61048f9793d739a0719dddbb5003893df81fcc53b77503e94d6249270703ae2856caf5e6801c0f383c13eb4a5b23233c9a631258f181d3e94bb8703de801c080682491c67ad34924fa0ed9ed499e3a8a0078c0c671d78f634fdc319071fd2a33c92070befdbd2954e01e4673cfbd20149ec4633d68ddd71ce29adf4edc8c5341e4f20e3bfb5031c09db9c7ff5c53830ce3af14c6ced18ff00eb520e1b9c0e7a1ed401206279e7af73cd05f83fa8a667e66ef46e3819cff5fc6801e4e48200c7af6a507040e83b7b546241f29c8ebd694fde3c0f704d004b920b0207d2807d704fbff2a8f76dc7a7ad286c820f4ee3fc2801fb8ed3c1e3f3146e20e72067d075a8f3f2f3d47bd0082bc751d7d68025c9519c0c0ec29b9e71d41e462985896c82077e074a4248f4fa0e38a00933c0c93814bc15c3753ce7b1a8b782777233dc52a92b9e4fe9400fce579f5e8687624024f43d7b7d298cc41240c81edc8a4073c7eb9eb40123b639dbc93d3b1fa1a42dc7e3d4f6a8b7018c67d7ad2861b704f03f4a404c5b92412013f518a69620f20e31eb9fcaa2c83e871dc77a524b160464d004aac738e718edda9524c703827be3afe1500fbb8186f6ef406e80f071d718e280262c071b8633c7b0a50c57db38c7afe750863b71ce3a70339a52db94ed3c0edd87f853026dfb32a3b0eb4abc0190760007ff005ea0c83c7a7bf7a15ce57691bb9ef4089f714db938f46ec3f1a787c050ebfc59e7f9feb55f76541381923a7434e2323e6c8cf43d680278c0753eb9c93ffd6a4ddc9504039edfdda89986e3f31ce00191d7f1c5383827691c85e49a603976b1c918e4914f110703e501b39c7f9e690380c0e15c0e01009efe9fe14c42a141182231cfa1cd160209f4f86600cb10c919271822a94ba14441314af1f190a791f8e6b5d58ab67396c64e38fd69f20575f97ef018c74383fa53423959f48b88c9fdd895473943827f0aa0d11562012187f0b8da6bb8d87e765e8081c7503fa77a867b582e130f1ab7381c0e0d3b81c724d2c0dd4a91d8d598ee6373f30dadea3a1fc2b46e742658cbdbc99033f238e3f0f4ac79ad9e23878da327b3743f434f40259ed1586e52a323823a1aa52c4f0b61d48ee0fad4892bc4719fc2adc7224f90e47cdd41ef4fd446cb0c739a71f99381c53412c33d31edf952efc1da41e0f6a82814ede0f5f6a736493edc1a8cf049c71ed4e0410719e9da801e8d92077e6939c64d35720f23f3f5a18ee5c8f5e78e2901296183eb8ed4ce411ffeacd11800f6fc4d0ec00033db38ce6801c5861b9ce0fa522821b9047a8e942b673efdff00cfd689093ce723bfad00389fbd83c8ee"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac9e86c6b82a24dce0864000021604132381f0054ffff29809cf208cf5a58c1cfbe2918e5474f7f5340c90900f50303b7ad301fc7d7de923e08e727a7b9a5246d3e94843b2318271fe1ef519e5b3bb27d690151820f4e413daa50704f1ce3383dfeb40c030dbcff003fe54d6383f78600e7da987193919f5f7a72b0080824e3f4a00721c9e483c704f4a0b7cbc7ff00ae98f82402303d40e9426327f223d680154eeedd7af1d69e18000e3e9e94de0ab63b7e42980e7a1c8ee280243cb9c67fc69439047cc39e326914fc839e0704ff004a463d323231d3d2801c0eec8c67db348391d0f1de9bd0a923a77a729e573c1cf1ed40006033c741d051bfe5c839c77cf7a8b383939f63e94e04e7af27fce6801ed9eb819c76ff000a376477f7a6e415209edd7d6901e71923dc7340126492194819efe94849c64773e94d5cedc1ce3a9e29a486f73fca801e4e79009c0c6476a5e841c9c7d3ad478e38cfa641a556c704e0e38e2900a4e307bf5c1a52475c8e4f2a4734de1b006d38e7d0d1b77200d8393c022801dea768ff000a463823ae3b1f5a692791dcfbf5a771c020f03a6320d300ce0eee55ba93d8d3b39071c81d7de9a303033927d0d3771c1e7a9ce08a00909fe2ce7233834310d8fba7f2c8a6ab10720820f1d7a51bb8033f28f5ef480783f2eef43c7a5395d87cc99ce3271d0d3385383953df1d0fd7f3a1beff00cb8c91c907ad301fbb71560003d80ea7f0a50c0a8504ab13df8cfe3516edbf373c76ed4e0d8c907728c67bfa714c097ccdc18a9f9b39c63d7ffd74eddbdb19c01824f539a889076b1e98fc6804ed073b8e727d47e0681165be642cdc8c70cbd07d452093707604718404700d40aeca03a3721b1c0c1a95b9662304a9ee319e29889e370b8e80a1009ce0d48c55dc4ab8dc0fcc71d7ae2abe496f94ed738c64f3f5fd280fb8657e5ded9233d053027552429009e39f424d45736f0dc26d28ae9807079ea6a5120c676f04f049e73fe453c6d0c791e5b1079e0e07e9e94c472fa968725b92d0e5e33fc27923e86b1991a363d4107041ea3eb5e88f18e55c72727247e23f4ac5d5b465955a48815914f271d78e845303ffd4cb560a40c52105464f4f6a4db824ff002a19b23a9f5a4317702a7927e94b19c373f2fd7b5317af4c7f4a6b9dc473c7f2a4048f20ee7ea33d2951b8e4e3d69a8db547603f4a6b6ede724803d0d003df1bfb7b63f953d1be51fdd5fd334c183c6d19e878ff003cd34b367938faf6a007b6edc4743e9d734e0dc7d29a5549e87d7af4a6ef3f7474fe540c7b90af8190476a55da5724027b7a9a68f9b07b9edea69378048cf1d33400e39180840efed40391f363ebe946dcae492077f6fa5231d87819efcf5a4039c80bcfca47f9c9a6862cc37127dbd6932581c30ce738c6734608008391d381d3da8024e074e9ea0ff2a8cb1e9b980fe468dc4f40403c73d076a5c36776738e3eb4301c06465b24e7079eb48cdb700120fd78a4dc5491839c601a4fbcab818c718ef4002b6e6e724771520000ebd3a9cff2a8cee538ea41c714bbf041008fe94009bf820371dfda9f8054f4e3bfad33e604fcdd3be282db08c83d3f2a007b10067001e3a7614de49c1c7b107ad0a49dcbbb6f39e475a369e871827818a007646e0c00cf6148f8c8c67ea2a32d80474cf7a5cee1d402380290c50dce7f8fe9d69fbb83cf1dcfad3303180d9c75e7a5264e36f3c7e3400e6fbdf7707d451d08e483ee38a6e339ebbb3d7b51c01c1200e99cd003b760f3f80a187ce413c67a8a66e3d33c9e87229c02b0208031d4e280149c907391eb8a52c71918dc69300a9c2803a0038c53377a962bf81ff3d68024638dc06719e845378041c63bf3da80d9ebca839c52a96ce3243376f6a0042c020fe74e2d90cc36819ec699807d149efd01a0390c4e39fae41a0093072a7a9efc62901246dc60fe7e94d07d4707a9a0b751c11f4cfe54087839dca73bb8007229c3a818071c9f5a8b386c838c0ef9a37108304fbf7a6048b9c819cfd383"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("809ac9e96c6b82a24dce0864000026f04132381f0054ffff481b0719e1874231499ca9618ebda82795e79c753eb400e0c31924851c53f6805b772477fbc076a88825429c03ee38fce8520e53680c4f0739cff9340126ec12571d09f94ff4a914e0850393f787d7dbf1a8b25d89e0f1dc75a7249c120023ae1b0476fc4531132c980801051b9c6738eb4e472a55f8da320f383eff00ceab2315249c9078cf5ebc9e6a556eaa51b6800291f5a7702c7982351b4038041040cfe1d8d4e842ca533b9540507b727bd53040493700a171bba0cf3edd4d488320861b4eecf3c8ce71d7ff00d754988bb9f31594e558824853cfb63d6a53fbccb003ab1c0cf5edfe78aa81cab938d8103753c364f1fa5588a45e8003b4052c07e7f514d12cffd9")
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame restartFrame = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    restartFrame.Add(interpreted);
                }

                //Draw the frame
                using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            //Sony Camera DRI Test

            jpegPackets = new byte[][]
                {
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e7c000011987b36807900000000406328170040ffffce4ea383d867eeb0ee723b7d3f5ab3dc64eeda549271cf3cfd7a7039e9d7141d0590012080bc1edd0f6038079c76ff00f5d4a002411db047bfa803b9fd3b76cd0048a00ce47276ed193b474f9483df3f81c53fa161cf6c91ce31c1181820fb134077ea48ab9ce376463f87ae73e8383ec476a93190381e992473ededdfb8a0527cbd0930cd81b47a631c93f9727d78a7a8da70411f302071d811d0fd3a76a0896dd751eab838dbdc371c7271e83d2a4036e0019c91dbaf1d0640e739e99a09d13d16ddc906de39c609e72a01e7d33e9edfe25fb09e7fba48e3bf1d3a720e3f4346d606efd2de83d4741b0f19ec01e7a64e46083d0f3526dcf0173bb1db18f7ebd78e3e94123f6e33c9caf2471907a60301c719f51f4e4d201c9c29db91dc0c92b92405ebf303fe450efa595c05c67e5d87f3e99edc7048cf3ef4f8c75f900c11f788e0e71ebc8c29efde9376eba80edb82405c727b8231d3039ea48e9ce4538201b41c77c118386ebcf070d8ed9fc3a511d12bbb802aedddc1ebc7ca4679c7183914e08370c1e323a72477ef8cf23af4edc51f121f637ecc630368ed83fa720f4fa5757663e61d3afd31c9e4f2707aff00faa8b257d03d11dee88bf32f043027bf03d723d7a63f5af4db6f92c6e5c9c058243c9c7453d73d3a7ad2d6cadadfccd3e2ba5a2f4ff827e17fc75bafb67c51f13b6ecedbb31e58648099e87bf51f4fcebca63c71c81907d7207a9f7e3fcf5aeaedf21c7645c8463073dc8e99e9fccf5ce7f3ab6e3e5dc186307f1f61ea4f3de828a8ca0a9c9cf439c8edc6473c1e7fcf5a85bb0ce704638073ec403c3671d4e3de81a57febfad481f9e9c1e7a639f6e3819ff26abb11cf03b7a7ebef9f7a0a4eda5ed6effd6ff32abe4f400107ae17ebc67b63be7a9aae4e38c63a0f51ee0fb9c71cd05ab3b36ba89271900e381d404faaf03eef1eff00c8d66cdc1233d091dff0c647cdd7e99ef41565b5b43f4da31bb18cf6fbc323a1c0e7193c7e556141000e3a91c6320ff746187f9fceb9ce7271c6464e0e4614119206338ce49e6a552467033bb03257af7c7cbc8e71ee280255ec71c8ce7924007838fc7069e32718c1e381b403ce793cf07207f9c50175b5c95576f7f9b9e3a1231db20f3ef521efcf420671d318fc78e2825cad7d4773ce19bb7c9b792471d89c139fc6a743d1401bb1d1875f4cf3c720f51f8d0676dedaa5e43864b0e01e87b74c75185e0ff875a7e3201f52067b1c74073f79bd4e73c5026eed928c6471dcaf0073d0e783f29fc7bf534f53f2fcfd7057e50368c63ae40c7ebef4301fe8b9f98e3a0ea476c77efd8f7a979efce31fc3c7b06e33cfd7ad0215542f6cf20fd71e98fba79e87f3e29c3e52300f5c1ebc1e08031d392091ff00d7a2e0295fba71dfd4f1f4207a53f1d39c8cf61d4fa9e79e9cfe752dad535b00e03ef0f9b827f85b27dc8cf3c81c678a5e4e061783f4c0f6f7c629735b44b5f243ee18c9c1183c71ea3a7393c77f5f6ef4a072a08ee38c1cf1c10011907d7fc8a22eda3febef11d0d8ff00070383c6e1cf6e300703f4aeb6cd704678e0f1c9ce3b9c8f4fa75a2f6b24bddfebe43f99dde88b97000e38e31d33dc63a1c9af4294ecd1b5020818b598fb03b0f5f4a56bb693d0d63a475fc4fc15f8a5299be2078a65670dff001369d73d46338c0c75ae21158e481d3fd907f13cf4e6bb5ff90e2ac917a2daaa49233cf0a39e0f4e3a9e6a5f330b93d0f418ebdb9c8e79a43206604638c71fc239f623b5573c02338ce3be3d46473c3707fc680216e41f51927a027eef20938c7a8fff00555663c7cdd327380067b67eee323ffd541a5a364eccad20ebc10324f4e4e3b8c74fd6ab16c647619edcfd78a0172a6bfa4432484e7241fc07a1e4673d703b71fad66ca49c36491b89e9d7bf1cfaf6cf7a0d37badcfd3b5e58376c8eb9f94e3a91dbf3c55a55cb0c8cf239cf5c64e07a9c7e58ae739fe64ea71d88efcf423d071c9ef8c54ca3960c3b60e0e091d7"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e7d000011987b36807900000598406328170040ffff241c64fe14012fcc07cc1b8278da067191800bf1f293eb4f5047f11c0e39507d7007a1fe74112d6eeff0b2551d7a91c8fa7a6ec9ebd3b77a900236e7380c73c74edc7e1ed9a05adda527a7f5fd68483918f986181e9c76e4e073d3d33f853f9c81ce411b4edc1efcf1c1edc1a081d8e9827193dfe538fa1f6eb522a038c7b2f00e0939e4b0e8681128e3eb91d4600f6381d7079e9dea45e181c8ee02f1f376ce0e73d47e540120006e4c11d31f7883cf46e7d41eff00e35260fae7207f0b76ec79e0fafd7f1a0071ce32013c01c1232b8c609ea0edc7f8d3f1d4e7bf1b94807a7276f51c7a75a971d9db5f2ea03b68396500724617a7b9070083d7b528edc93d08e3afd7b8fc4fd28e5d9b6f401e14f4c8dbc8c633f8afa1e7fcf5a5dbd32c4f7efc6df4cf51c76fcfd0e5b3bae9d005c6323ae3be4fd0018ce7fcf5a55555750c4f51dc1c1ed8f4e40a2daff35ff003a1b21d00031c71d988c1c7cc383d79aebacd727273d4af41c76c807a1a5ca9ad9a1ec77da2282ca39e31fe19e9c1f7ff00f5d76da9b797e1cd55cb7ddb298f418184e9d3ae45528a8ec5c65bdf6febb9f809e3894dc78c7c4921270fabde77e98976e707bf15cfa8181d7b8c673c9eed8e9dbe95d25c7e15664cbce016cf4ea3a8191c7a62a4eb9f976e093cff0031efc50323dbefc6723ae49f5e9c1f7e698475c0e7241e3b8efd792281959fbe4f70391927af23079edc5577e36828724e7bfcddb006d183f8f7a069bdb74579070c3241c9cf6c01db8ce4f4ff003c5536e324139ce0fafd4a8e879ebd3d7d282a324959951f3ce09c8e3eee73cf619e3e954a4e09c8e39e9f2e318ec1bbd05a77be9b1fa8318c9e41c163cf193eb9079cf5e3fad5851cedc76e7e6eb838e41e73cfa9fe55ce604ea3a37270d83d393ea00e8339ec4fe153853f37231b863a0c9e9d17af6cff003a01f5d4970063ae720638f980ed8cf041ff003dea418000c67e623a73f4e7241cfe3fce821cadff000094600c72579fe1cedeb9c7cdec7fcf34e0c148049e848c82380081b8f4eb8fc3d2821e8dd99fffd0de0cbc019ce718c1e3be327a7ddfd2a55c8eab8f98751bb1df705ea0f7031fe140127ab02dd7193ebcf7c7b74e6946dc1fafb027db20e3248ed9fa5004c3a05c609feef6f739a91557b818cfa8f9781c8dbd7273dff3a00973f3038c121474c671fde05467f2f7f7a914e08039da40fba39049e0fcd80303f4ef8346ba6ba00fc02c401bb24fca546471dcf75ce393eb4f18c1e9d0f6e72a7f3fd6a5b5b5fa80ed99c6e238246eeff8fcbd319c76f7e94f55038c0eb83c7a679f6fc3f3ed4f5e8c07151c6073cf1b72ddc9ceec9cf43d29a1718008230472a78393c371f29eb8a495a2d3603f1dd8f523e5c71c738fba79ff003d28dabbd78fe2e99191cf5386f5e49183f514a3ae96d62074364b8c7076e7d300e38c0279cf3fe4d76164bf7480a01206700f4ec7078eff00feaab7e633bfd1130e01241e83e5fcf2727fcf15d378a9fc9f07eb7264aedb09b248c71b0f63df8142f5dcbb68da765fd6e7f3fdafc826d7f599323e6d4ef4f23afefe41838ebd2b3e31803a7e1f8633e83df35d2f77ea5c764591d8118ce33d307e9f8638a0ff00c0bb678ec3b7bf19a4310e70460e338c71c1ee4e5b8350b0e831c724f3d3b640ecdc76f5a00acfb46738e3d4743ea30bcf23d3f5aad21ea304104e4e0003f878e393cd03daeae5693924f5c77c741c70c3b74aa6e40cf20af1d07523d30393fe4503493bf468a8e473c0c8c0c29ce47be47273ed9aa523019c9ef8e99f4e0e392682bdd8d9def7febfaec7ea0c7f3647cc1703e5c360fb0ff6bdbf4ab4855b2486078cf0003e81700e381d3b639ae732b59b64ea71839257231827232485cf6c71d8f1c54e3918c3647d7f21c70700d026d2bbbff5fd22753804038e41e739efb48cf4efec69ea7a64639e081fa0f4e68336dab92af41df0c40c8e4739232791fd3e95201d4e7d4e707af380c4"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e7e000011987b36807900000b30406328170040ffff71eb4123f073cf5c927f5f53d7a7b7f3ab00b0c67236f3d7000c6371c0e38ee3f5a003a0fcbe5c74ce0e092dc1e7a548bdb9f4fe1fbb8e99c2f5cf5e4e7a838e680255c0fe20304fe07a6401df07f9f15285236f1db903824f3c050b8fd73400f072b82090086e9c8f503d3e9c81d876a914119382a4e33c9c1ed9e17d3b73f4a36024fbb91f3fa0e1c657af1c1e3be2a4db81df68cf6e0ee18cf5e3e6150fb35bb0765bb15546dda473c61876e7b63bff002f5a9318c8c7461cb0e84f73c63f1ef8a5b7bb6f40ed60e0f383d863e9dc1fa0ff003d29c07cca7e63c9ce4648e718f6232714455af776febcc00e3ae7ff001dc118e847afb926818e3201390470369c1ea73f78f1d334e2dfbdd581d2588fbbc7cbf2b63278cf047cc38239ff001aec2c54700e76e075edf5e7ae7f9e29a57b3bebe407a0688318e4e738f9b827dfdfdb8c0abff1166fb37c3fd7df2462c27e7a13fba7e87b0e3f0a717ef256ebdb434d22b77aa3f00efa432dfdeb807f7977712678fe295ce7d7b5310e0633dc7f0f0075e3b83fad753ddfa971d9684ca76f4cf24e323f32076fc7ff00af4a71c673d7b8c723038c8e3fcf5a431ac3d46724718033f5c8e4ff002fe71b743c907af231c039c74c038c6322802ab7009c7239db8c73d0123b1f4aa6c40c6e278cf4feb9e41e07140fa34cab2b7ca7e6e39edebce0e7a7359eefd8678c8201e9f975e282fb2e56fef28cae093f374c8e9d474e7d07f8551793a9dbb874e075e08c63a9a02364f6d7f2fbcfd4f52c30c029193ce546719c8ea7e6e7fcf5ab51e73918c9c753d0f5c7dee783dbf2ae730da56d5bf52652dc64742474c6463827774e9ebcd58dc406185e08c71c9f51c2f439183da821dd37d0941385e48dd8e739527d1739038cd4aa0e78c6ee0938ea083d3fba78fd68249149e4018ebd3773ea475fcb1d07d2a4524b6d5ea7e5c80704e3a938183923bfe14012af50a19bf88e3031c73c819f5efeb4f53c8040032719057181824e7a8e077ea680245e1b38f43d3240e4007040393ef9a900ce79cf27777dbf98e79cff009ea03696e4abf7873cfb9c67a8c600254f1c75a7ed5e01206473d723dc6d38073c7e3d28174d35250b84036e339e17d474079182393d33cfd69e870a092dc1e491827b76e41dc39eb8a3e4473356bbf95bfafcc9402032827ab670cbc8e4f0abd38fe7d334f8c0627e53c03c01f77070493dba7b9a4ecf46f629a4ee9ebf9922aa91ce1b924f38c1e783b80c9e3aff00f5a94824f4539cf00f18f4ee738fc6a1dd5aefaff5b82f26be5fd68c90ab0e8065719240cf6e0e4f5f6e734838e3ee82c3ef02318f40a3f5e6946efa7e1fd7ea520ec01391cafcc4720752463ae7f2a178604b639dbefe80101b27a9ea7e87bd5ec928ab81d1d972cbc7f0803006095238e84e71d39eff0085769601b8381c618fb7b3607ca7f0eb4e29adddc0f41d0c1c0c01d471b71f8f3ed9ec6b3be33dcfd93e17788a4c818b09f270b91fb97e47bf4fce885f9927dfb8f449b6cfc15762d24871c995bfbb93f313dfa54cbf4e38cf0338f43edfcaba9eefd4d63ac56e4ab93804ede838c367b8ce4f1c0a5eec39c8c8e40e79c64fa7f9eb48a1a0f0783db93c13ec38efcf3db350b6460ed23f2e31c646381d4e0fb5005493ef0e07b93b463ae07ccbcf7fc6a9b9ea38c6464f73ce4ff09c7b1c8edd68295ba94267c0e41edf7ba73d011ebf8d67cb267b0e72738c03ee320f5fd280dad6e867c8fc13cf73d338ee4fb1cd5091f8f7c9390393f4eb8ef9e3fa5038b77beecfd5a8f3d39e411f3167c81ce3dcd5b50a403b71c9e769ec4e323f87ad616df439afabfd4994f2793ce3a03f37a1031c9fcfbd4e33cee3cf1c953c9071c8c6734872eb796a878c3719c31c76edd39f51803fcf153e3a67f840e83ae703231d4d040f519c952095078248f7c8f9b240c1e302a5552413eb8efd781c803db3c718fc280255ea0a9cf3fdd278191f31fcf1ff00eb353038c6"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e7f000011987b368079000010c8406328170040ffff4ff08e8766791c9e79607f11400e55191ebc0e9c9e70402df871dea5030c41e4303f7471c120f233ce71cff92132766ae48171900f19f4ec4f523a83edc9c67b53f04e071c118c638273d71df8ff00f5502e68a93d7f02c2f1b403803a123d3b0eb91ededd28098246796ddd4723d8fe2476a0cdb6f73fffd1f43e41527927a8c03f32e41e99078ededd6a655380a339041c61781923d7a01efebc76a3e465ccfa2b12e32cb924ff00c0727a11807773d3d4fd3229c17a725baf24739fa8ec79acdb4da4d356ee559d9bbb7e8c0216e548e8067eef383cfddc83c74fff005d054f3d7af46c1ce39c75e4ff003aad2cff00cc719746f50618c6461704e727b71e9c0ff3eb42af2a7271918e78007738edf9ff00314edb59ff0090dbb6a749629d09c1e40c01d0752338e4f3f5edd39aed2c0671cf4e07d78ce73d0f1eb4bd55be7fd7dc33d0f46521bfe04bc60f3ec41edc1c5711fb475e0b2f847e2072c17759dc270464929b4003b1cfd7bd382b4959e97febd07f23f0d1473d8f27a81c1ec3af1ffd7a9c70070dd4ff000e3db23d7d2ba4d96896a4cbd8739e01fcf8079ff3ed474031fe7dcff9efd68181e7827f1efd7a63bd567382df30f6e0f1e84f607f0e2802948719e40e473b8f3ef9c71f4aa521ebf312324f5ce3ebcf1d3d7fc68033e76ea3dc9e011bb18e060f07f1acff009e662912bbb647caa3a1f53d81fe74149db4572dff00c239ad4e81a2b4775e9852727d4281d49ef5cf6a1617ba7398efad2e2d9ce70268dd33fee92003dbbd68a9f35f95dda1f338a69defe67eaaaf7e7bff000904329c1f9815c03d323a55d5e0825bfefa38f4e011d08c715caaeed148e5274e727391d79edf8139cfb7f3a99540c0c03c8ef8c8fef1c74fcf81c71d286bb3726165a9617a118e84f6e48e38f7fbdedd69c3e5c8c0e33cb019038e703bee1ebc60fb54887f1c1046727381c9c763ce47d7af7f7a9b38e01e727a8e80e0e41edc8fc2807b3b0fdd92412c739e0e4f03b9e99eddfb7d6a553d319e08ee0f1fed0fc3a7f3a03e64b9c93f8633ea067a28fe4475a97a606480406c1f9b033e9d8f1f5e3f0a087ab6ada47b0e191f78b37240ce38ebd003e84e7af078f4ab0b96c8c903273d0018ce54e7a7238a0cdbbb6c90630473c827f847b7cb9eb8c74c9fad3c8ce781b9f182091f74738ce46e07391eff0085021ea47cca0038c38fbeb8fef0076f1ce7b63f0a981e00238c0ea0707d33d41c8f51d681f64480f0475618038e84f40c08e4fbe29f90413d70002720e48ee319c1cf6a0ab6fafc3fa7cc38e573f7b030caa73d4107e6040e07f914bf2e4a8271c638c63a104e3a639ee7fad4db75a2457345b4eec4395e33cfdee303238ebcfb7f93cd3872cbc1dc0819ce7203678046413d307f0a76b5bad8bf43a4b1c00b83fc4b8e47d08201c06e4fa576d61d81248c8c707e982a4723d727fc6876d13ea07a268abd383c30209ed939c0c70318c8af19fdade5107c23d455582ef42bc0009276fcbd383cf43eb5705efc3c9a07b357d0fc5e4ddce0e393dbb0edce4679f5ff001a9fae3d88ec07b7bf3ffd7ad8e85b2b138e318182bd49183df8f6e3b5213c118e33e8067b64827079ff002680236ea3a600079ed8c9c007d89ed55a439ce49ea71c01bb19c1e7a1f5a00a121ea33d3e831f9718ace964c64e48ebdfdfb75ec3da8032e47791d6246cb39e0e324039c1386c03f87d2bb9f0e6991c6d1ef4324b23aaa00b9777276800632cc4f4cd3f2b170767e6cfd3efd9fbe0041ac59c1aa6b9621fcd0ae8b2c7f2c60f65c7392bc13dfd2bd2be3a7ecade1ad6bc357074ed3ede3b88a26292431ee749155fe7e3e60e1b6e064fa71442a72cd35f64baf4fdc76d4f9e90edc1382300641c027d42e3078ed56d3e5c101874fe11d38041f9864722b08adeef4feba1c24ea490a403818ddc7ca3d0e474e87393faf153f5c10c79eb9271eb8193c1fcb2314e51e5bd9e8ffab0130079c678e09eb9cf7f98"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e80000011987b36807900001660406328170040fffff00e7bd3871c13dc9e99cfd3fba71ea39faf15004ca3249e77617be0b608ebc71f28fad4cb9d80807e5207cccb81d3f889c91f81c504b6ded7baf21c011c11f74818c6d27d8707279fa54801ea46029078cf3d3b1ebd4f18edeb412ddefd6ddae4abf7416c819e77607e078cf5edf9f4a72e7036927fe039181ebe9d3ae7d2815e5ad9dedfd752751c8e3ea14f7ec38cf3807bfb54cb9553819f9b6e7033bbbe31f439383d3d33413f3245e18919f9b19f5cf233c2f00e38fd6a5da0a8fe1fae7af07039e4f03d3afb502150ed23393c90720e083ebf29c8f4fcaa75c8e3703f315042e79f4383c1fa9cfe19a007056e72bd719ce41c72b81c8c9c8edf8d3c29390aa7bf5ee476fcbaf3f5a0a575aa1dc0c64e075c608c6471c6ce79cf4e87f500e84823938f943038ea0e3a1c77f5fad02f514ae7a039e9c0e99c723d7f0fc3dc453bd720f0de9d73c7cc41e3a74fd3a54be6d7a7f5e85a697dad3d0e9f4f07e55e369e0007b8f5dd80071fe78aedb4f1d3208c9032c02e303a71d3fc3bd0b5d6eff4293777eebfebd4f46d1474cf73edf4e0f3e9fe7ad7ce3fb69ddf93f0c12dcb0fdfdc409d70a49914027f239fafae2b4a6bdf8eba5c6ff55f99f9091f6182707d38f4ec33d33f954dcf3807aed3c1e40ec7d0f5effa56c7447644abf7470c304f4c71cf6c0e0f03ad0dc0383ce3b81c9ed8f5a0640c7a678e98ebcfd3278aacc7afd58139c29e31b7da802a49dc74c63a1279f4381c1eb5897526dcf39c104fddcfd2802a698e249d9cf4dd8f7383d81ebdfb57d6dfb36f8065f1df8d6d9e480c965a74a87054953283920f60428ee3f8ab49ae58afee9a52579abed13f72b46b1b3f0d6956f616f1a466389158a8da41c018f6e7f9d1757d1dcc32432e191d4a9cf23d320f40735847bf57b9abd6fe67e632c4c720f183fc2777a1c107927d8d595889c704e3209098cf1d474e3f967ad351b5ecdab9e713471e707aee61f87aa807ab707b7ff5ecac4381b71cf4c631ee063a67afbf7a99745abb790132c78071c7a9cf55f718c6738fa62a458cf19ce17e6ea0823a1f71d33d47e5516dc3be849e51e472060f52067078209e879f5fc73522c639183c63afafa9048e6912a57bab6dfd7f5a92888ff77a63b6339e003c654f1f4fe74f10b647054e0751d39e9ce71f4141127696d6f4dc944582580e73b790d91df276af3db1f4e6a4f2718c0fcc743d081c1c77ff00eb71413dc996123033c8c7257a003a0e793d3ffad8a9442400707181c719e3b8e38eb9c7ad02261170483939041c73e841c8e46d3ea318c54cb131c81d0e187cb8c803ee83d41e71dbe94002c5904104f279c64861f778e7d3dff9d4ab167072392148d839cf19c15e0fa1e39ef400a23c6173fc409e319c71963df1c73fad486361d41e0f5c939db9ebbba9ebf5edcd0002203afa770720f424f73f9fe9522c5d391d47453bb838c8e793e9da8017c9fbb81c827ef2e4139ea71f77f5ebdbad3d60f9c1da7823a83c7d32bb88c8ff00eb5035a58fffd2fa26c22c6df9476e31dba71c7bf4f6fa576ba75be368033d38dbc1e01c7f873f9d0446edebb2f2fc8f44d1a2c6df94753d31fa6070dc7e9e95f21fedd17063f05e916831ba4bc84721571925b9007070055d3f8e3ea36ded6d9a3f2b238fa1cb75cf4fc32471e9fad4db31c63d0f00f3e80815a9d0972a7d592aa6dc67afe583d3f0a4913b60fe27af5c903d6828af22e41dcd9ff80f3c76381c7e5551bd88effc3d3af031c9248fd680336662bb8918e0f6e7dc9f5e8315cdea52feedf1d3a038c67d78c7078ffebd34aed2b010d866284b60fcfe8b9cfbb738e99f5cd7ed27ec3de054d33c2316bb7100f3eec79fb8c2413bf91825b93f374eb8efdeb4ac9c6069415dd57db4febee3ed9d62ef9619c0cf618c638c1fcab9b378707e6c81c75e47f874ac96965d8d4f815620718ddc13d94e0fa8da3a753eb5692103195c9e475ce4f4c823a1e9ea7daa63757bbb9e76c5810e"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e81000011987b36807900001bf8406328170040ffff58824e7381c03cf7000278e9dea709c0206e5393c8270071cf1c1cf51fe4b96cff00afcc09638b183c11939e7dba1c7d3be3152f95904606031c71bbd79073c707918fcf8ace7d3525bd5ab3f9122c38c617239ee707b02403f2fe3eb5388ba70720a8e3b76e03741c7d3f1a9dfa129a8dd2d57f5fd6da132c38c601562482081eff00776f46c7b0e94e587819c71c9001edf51fae6911dfd4788d812406ebfddc67d864f4ff003e95388871b81ea31c74c67a60f3ce31c8fa8a0448222318e848ea7ff413f43dfd7a54eb1679da3e504750464f03036f278fd78a0099621c8e07f174e7b020fa9e0734f58f0dcf620f0070064f5238c73de801fe5e48e0f6273f75483d075c9c1f6feb4e11e771c6e009183d3191d307ef74e3afbd003fcac63a82c0e486ff0080e18773eb83dbde9cb111904839feeaf51e8723238e87a718ce2801eb0f5c86c8ee4601f73e84e3afe94f48b18ebd7f880ce7a0030011f81c5003a38b90704743d383c11d8f07824707ebd6ac241d3238c8e7201ce49c0e39fa7340fe674da7c0410319e79e339c8e840e873d78c715d9e9f0fdde3dfa7031fd7ad03e67d1d8f42d2a201d7030370c81c7b631e95f097ede73edb1f0ddb6719b956c7ddced89ce48c024e0faff002ad292f7d7ccbbdd2d6eee8fcd1894e00c0182327d7903071d38ebcd580a474e7e83823a60fa7ff5ab436526b4b6ff00d6849b76f638fafea78e7a71fe4d364518ee707d4707d4f391f9d068e4b62a48bc1cf5c01d4f5f4faf4aa3230e4ed6dbcf6c631eb9ff003fad0331ee7001f539071d874c723049e7fc6b93d564c6d507ab73cfa76c76ef9ff26aa1f1202ee9919b9b8d3ed40e66ba822201ebbe55196e3e9fe79afe897e0169b1e8bf0db4744428ed6509f940523e453cfcc31f7bd78cfb66ab13bc55cd28694a6edbc92fb91d4eb37782c73dc75efedf5ff3cd734d77c9c9c6323ae318c75e95997d5a6f53e438ad6419ca1f941c6006c377030783d781f955a5b46c8ea3a7246483ea70bfbbe9fe7ba8bba5a1c1f32d2daf46057a753ceff63903f2fd78a956d18e481bb6e7276fa7f06475f6cf4f5a61dd762716d20edd70464739524363238f4e3fc2a5169270410339ea31cf5249ee78c7e352e2b5d353193b92259c983db27d8e57d30477c7afd78a9859b80060a1518c118ebd481904faff009cd435a26debe82ec4a2ce43d01c1238230a49ec7f23fe1eb2fd8e41838009cf2ddf1d8f038c81ee6a044bf6361d473c8e83afa0ee3ffad4ff00b1c983900755079cfa64f3c76fce8192ada3839da71c672791fed63a7af7e2a6168fc92719c741bb9cf5c91c11f875f4a044cb68fc1da3233c7a63b0cfdd6fca9e2cdc71b54e42b0c0ebcf5278c1c75ff390078b5906cf954f0467fda193d76f5cd2fd91f208403279ca9e48c0c938faff0089a007fd91f0bc0f9707039f5e4e7a7e9ee3bd3859c983e9ce0e323e51c823a0fcc7e3400ffb1be4614a9c91f74f38c7551c03fe3522dacaa471824907e5e9f4fef0ce3a723140138b490e09ceec91d08fc47183c9ff00f5779d2d1f206ddff30e73f8e0af638e31cfe140ce9acad1f2a40c77200271819cf038e07e95da58da95da482381dbe9cf3d0f27a7f85016b599dbe9503061c7192791c8fa007dbffaf5f9d1fb79c8d26a9e1ab618c2195b03a7cb185c9f524b71e95ad2f8d795ca4be1ef73f3de281fd1b8f63f91f6ab4b6ee402339fc79ebc0e9cd59d09c7695f41fe43820800ff007b23a1e809f43c74e7fa535e093181cf23a03d07a81d4f1cd0118f35ddccd9627c1383c123bf1efedd055092273918fc71fcc7e1ff00eba6f5355a5ae615c8c77e3001e9827dfdcf35c6ea68ed2c31fb8eff00ae33ea055d3f8d5c3a33aaf0bd9b378834142010da8da0e4641fdeaf38f5cff9e95fd15fc3a430f8174851c6db280607cb80100c7cbd3a1a789de1ea6945da94bb737e88ccd6da7f9b691d475e3233d720fa8f7ae499a7e4718e3920fe"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e82000011987b36807900002190406328170040ffff67079ac85f69e97d4f048d9339f4c31381c7b9e38edf8f1cd5f8d90e3f8707391b79f61f2fdef4e98f6eb59465cb757d0e34ef7ee8b086338390001d1b3f4cf2dc3559429bbe603a10395e7ea7d78f5ab6fdd6ff00afebc896f9644ca46480380c3b9c8c03c818ce31fe7153204ce189fc41c9f7273d876f7a953dee66eda5958b0020e84e47a1e8c33f3007a7d3f0f7ab0aaa7009e573c85c1ed9c7030381dff3ed37bab357b0899553904b76270a011db1db2781dfe86a6454183b771c918cecc0cf5c679f715204aa9174c0ea7d70391f80e871c54a228d71ce54f00118ef9c673df27fc4f4a009a38e3c1c8c05cf524e00e78f53d3fc2a75863eb8e493db07bf0320f3f8fe66802558138c13ce73c71c63af1c739fff005d4cb0a8e71eb8e07e6318cf4e99ef407e03fece8a339dbcfa738e47a63f2e9520850eec02467eee3763048c9e38ff00eb501bec396d90e3000008257807dfa9e5bdbaf14e16e8df3606ec750013c0c807d7af3fce8017ecc99c8e09c75f9141f503922a55b64190491f3678039f7391df9c1e9cf4a00916d90f420f4ea3d0f519ebfe78ab11dbc5b80c60923a0c7d01e38e9e9f8d00743650c5f29c818c6781cffbdcf1d064f7aea2d7cae32d8c1c631d3d31d71c0a00eb74df29581071c82471cf3db39e6bf33ff6df65b8f16e871aee65486e0fca70491b17279ed9eb5ad2f8b6e8ca8e8d1f10c7683a103ee919dbc1fa74c77ebfa0ab71da0181807ef7d4f3d03639fcfa559d516ade9f88f6b64eb80dc7f11f941ce32338ddd3d78aaf2dbe00e8d927b8e38c673d3fcfe140d4acd24b43ffd3f86e788727b7fba9cfd720e3b71c7e75913db925540e41c0e0807b8030323a7bd74ae967a9d073d791eddf95f507db9e7183c9db8ae3af230b3c4c7eeee0bc6381ea7079e95a4236bdd01d7787e48e1d7344949fb9a8da374e78957a73c75afe85be1acc975e04d21d795fb1c2c304f07cb56c1f7e9c7ea69577cce1a7536a69468cadd25fa7fc022d661e5b8e39f7cfb8f4ebf5ae64db0fee8ec4fcabc6dedbb6f03f1acc2367767ca897678cb15209eaa781d3a638e055b5bd27862bd33c11caf4f97d0f1efefcd44636f5383abd4b097dc0e7e5c601c633839c0eb83ebebcd4e9a830e371382588c741d39c753edcd292b5db5ff07f1dc9b3ed65d8b03530ac406c1040e474e477fae7183dff001a99753ce064f18ec403ee3d7a73fcaa5ae5da464483521b4e181c30e4a8c8c1e49e7d33f9f5ab0355562724707f8b68c0c631efd6a476b6e89d7535e49604700e02104f390495caf43f81f6ab0baaa7cbc85192472b8e0738f4c1cfb0ee79a044835445c1ddc606327ef7a1e460fd7f2a9bfb5e2e006e9838c8383c8c11ff00ebebf85004ababa123e7e464f207403aafa0e79f4ebe952aeae809f9c6ee08391c8f738e3a8c7f9345b742b3bfc5a13aeb2993871cfb72327049f53f4c8f5a9d7574231bc75e30383cf4ebc7ff005e936d6cae4b5adefae848bac271f30ebdb6838040047a738edc77a906aea7386041ce3246307b9cf4383fad0af657412bd92fb4872eb083f89770e3a8f9b1c0201e9c7bfe229dfdb2986c1c1c8ea471f89efc1eff009f77df42af17617fb693a070781fdde7f33c9c9e94e1ad21e72383dd87e9f37047a0a3b8c906b51f1f3773d860f5f4e87a714d7d7915c7ce3b16391c9eddbdfde8036acf5f46032cc31d79ea7a6466ba4b7d746402fd31dfd8f07381da803a7d3f5f4c8f9c704e7f3ea481cfe75f9dbfb5dea02efc5fa6124710dc7cd91c64a7af5ce3d7b56b47e3575dc71d1ab9f242cc833f32e073f2f6f4c67ffd63e952fdaa3c672b9fe1f9783ee78ff3f5ad1a6b747545b776e5a22192f9173f30624e303ffae0e78ff3dea9cda8aa86da79f461c1faf4c715518bd1db416964ad777febcfe462cba929c92c339ce577700f000e3dfd7e82ab0b957c8ce724fafe478e7ad16beb7ff816f99aaf52bcd08911863bf1f2f5e9903e5f"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e83000011987b36807900002728406328170040ffff948f4f4fcab89d5ed1e221b195ddd429e793d78383c1ab8ab25a0c96d1590dadc039f226865ebff3ce4078e0f3f2d7efbfece1afc7aefc38d370e0b476b08c120e709b403b54e3af3eff009d4d656e4febc8da9fc1512f2ff23d1f588b96c0c7200e39273d2b9b307b6318e70063dfa75e3d78acccd5d5d27f81f9f89ad823ef1ea73db27d573ee79e953aeb0991927af1cfe181e9f99c7b50725b6b3b138d5c649dff0030231838c678c74e0ff3f5a97fb63a9ce003c1500600e3030300f2734acf5edfd798c78d679ff598e9ef8f6383cff5a7aeb60721cf523aed07a86e0743cfaf7a89436b74224be2bafcc986b43f85ba8c1e73939e87ea0fa9c7d69e35ac64ef3dfe6c75078e012403c7d47af350d5b4688e5beade9dc906b5c0c48fc803a91f97a739a91759e87cc2791f518edc0e013fce90ad6bea49fdb5b7f8b8cf623b77279c0e3804d2ff006e85230f9071dbf00a31c8e9d3340870d7b93f360938273d474dbd7d3a8f6a77f6e1e01908ca95ee31db9dbd7a7a9feb4012aeb9b7077edda4639f4f4e3007d6a51af03d641d48e0e7701c771c71c50164b6561dfdbca769dc3827b28c018e0f381d391efd8d27fc2401b2108dc07f16e504703279c74e9cfa55257ddd8022d7241c3c81b9cfcd86c63b0c75f7a9cebbbbab0eb91855f9707b73c723d7da895afa1318daf7dc69d7875f331cf50464f6e483cfd69175f3c9de3dc96ceeebfdeebdf8a9287bf884818f31300f505874ede83e82a83788b2c49933c9cfcdc8f61c71cf5ff2681fc8d9b5f11e369dc31c7cdb80cfa1038c9e95d1daf8a49c032671d307951df1c9fcbf3a00e9acbc55b48db201f30c9cf2391c9c8eb8af88ff0069bd6e39fc4b64e64ddb607fcdcaf273907a72315b515eff0096a528bdeda2f2dffaf99f2e49ad20cfcc3233d4f071f8e01ace97c4080b1322f20f0c436ef71bba9e38ae9e55d8d6fcba25fd7e86749e258d7204808e7ae3d3a67fce3f5ac7baf14c5d0c9f2e7a6ecfb739c678fc7de9db6d47fa19c7c4f13b604dc70304f03be0e3a7d2b7f4ed5965e4303d3b29ef8ce0f19e9dbf0a56b5f437845beb73a5b6983e0e4e3e8063a8edd0f351dfdbf9f0b02a3bf040c9c0c12d9e9c0fad2d535a2b2febe405cf0ce8dfda08d6abb73c8c10833fddc16e87d81fcebf51bf638d7decac1fc3978fb1a063180c7aa9c6d233d4f247e03344d5e2ff00ad8da97da4faafc8fbab54b0ddb8a8ca920f4ebed5cf369bd709c1fd49ec78ebef588d452e87e430d571c6f030464ee1c76e3278fa54abac0ebbb764f6c76c718cfe7cfe34fe47138bbe89930d5c103e7ec0f523f3f53f89fad3ceb23032c0f1c91f9727764118f4ff001a0566afa0e5d61464ee3ff7d28ddd3a7a9fc69c3591c82e7391fc40939ec71dff003c0a42b5ed75b1326ae07572464e70d8239e838e0f15606ae31cbb71e879f61fe78f7a4e29df4d49e5d1abff005fd74b8bfdaea3197f4e327273d40f9bdbdff1a986b083f8bd71f30f4ef8fe2ebc54f22efa8f955ac35b5a5195320e327ef0c7d48ed4dfeda8c6d21c9e4742b8f4cae5ba0fc3ad4fb37dc4e0b4b6828d72304fef01e806597248f6e7f9f6a906bd1e39719cedebdfa678e869fb37dc5c8b41dfdbf1600f379e99240fc4f5cf38268ff8482153feb0648e0ee5e4f738fe99f5fad1ecfcff00025c75765a09ff00090c2300c89f98fa73c8c920f1d7f3a77fc24108c6664fc0afe63dff00cfbd1ecfcc6a9f7627fc2476e3fe5a2e79e376083827000e87f1a5ff00849a01f299d770c77fc483d36b7228f67e62e4dafa7f5b7a919f145b28199d79e73bb0b8ee467af6c533fe129b60bfebd4f3d8af27d5b073fae68f66fbe857b35d5942e7c69650a12f703b039603affbc78fc47e358eff001034e4c03771a7bb491f3e8bc1ebf5aa8d2bf4b8f9108bf13b4b88fef2fa3e081cc89cfd0af039f7a6b7c65d121c07d46118ff00a68a4923d7e618e01f5aaf65be9f88e315db7336fb"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e84000011987b36807900002cc0406328170040fffff688d074f88b1d4611b0919f3d0e31dc00c41fe67d0d7ca9f17be38e95e29bcb4bbb3bc12ba2324bb9b90470b8cfb01f4fd6b68536aefb0592be9d8f069fe22c672a1f1d7f8b907d0f3edffd7ac49fe2067a4cdce7f880e9ce0e3df15455ef66bfaff82644be3cdd91bd9bd8b73ffd7ef58f3f8dcb13b5c8c640e4633e9df1fe7ad3b37d02f7ea5783c6cc2601e4ea460923af391cf5eff957b2f85fc43e74699932095ce4f4cf718fc28b5b7474d1d7a9ed5a5ddef5520939c0ebd71dc10793c7a8eb5d5e14c60124f4ce074278c72393f89a8e5bdeeefafddf884b46d177c21a9c1a2f88a3fb5362ddcede72a833d8e7a0ff000e86bedef0278cb4ff000f6af69aad8c9fbb9022dcac67207006f2460633efc0fc6aeda3ec3bfc3e5747e96f837c5ba6f8b74db79239a3797cb4e8cbc9c0e0e3ab73577c53aa58f86f4db8bdbb9638bc98ddbe6703eeae727d06071dab9dab368aa92718b68fffd4f82478d4e010e3273d180ee32064f3f9d4abe36200cb74cf723db07079fa7f3af40bbbe97d070f1b80768753cf3f373c7af3c9e94eff0084dcaf0241f8b7afa8279f6a04dbee40fe3cdb952ebc74c9fc339fe11c533fe160720798319fef7a0ce793d71d38fc68b2d7422eefa2d0917c7e40389b0463bb75ee7bf3c53bfe161edc6263dff8873ee403cfe5f8d165d87af607f88ec9d5c1271cee273fed1f418e9f4a88fc4c0a083364646467a9f7ec6972a57d09f7affd6be7f919779f163ca055a75ec701b92793ea7e6c0e7f9d7352fc6711e479f9e4f61c104fe9c51cb1d7404f7badbfaee517f8e08bc7da3d7f8f1b893c93fad5293e3b05c85b8e7fdfc81ee71d4f1fca8e58f625c96cddd7cb4febd4a127c7bc6e1f6a07923ab0e9db86ebc55393f6803f37fa4e3078cb37718c0c53e55a683934d3d59037ed047a1bce0f1d5b23e9f9d427f6823820de9ec3efb71dfa669fb3b7d91a94744ba95a4fda0dc1e2ece3d37139ed8e7fcff3aa52fed0b2f5176e40c8e1b3eb9cf7e7d01a6a9f921ef7d3e133e4fda167e82e24ce4f19393e84739fd7fad664bfb41ea1ced9dfb8ea79ebd0eee323b53f67dda1736f68bfebfad8e3b56f8d9af5ec8ccba8cb1a1c008a7ea3030783c57352fc52d724cb1d52e37640fbedc01ea074e9ef55c9dbfcc2f77cbfd7e0cc79be23eb72125f52bbe720fefdc1e38cf1d38aca9bc73a9c992f7f727927fd73f3f4f9bae4d3e5f37f9095d79fe6655c78b2ee5077dcccf923ef4ec73d7ae5b83d6b325f10ca48f9df938c64f3edcf5e3e94f952d914b57e4556d7a5cb7cc49e7277703b6707aff9fad52975c7e4961bfa6739f6e83af1daa5257ba7f70f449746537d724c7de3bbdce7f1f6e87bd566d665da7e63fcff0011f974abb6e16d36d4ae35693925cf7efd3d3d71f9d7b17c3cf15cd0ca96f339f29d9546e27e5dbcf248feb59cdecadb1d1415adaee7d85e1ad623b848f0d9c63182dc7b9e38ed8af51b6ba464186e00c7527a8ea70b93dba8accd6a5b4d0a97aa1f0c3e52ad90f9e87d46381f4cd753e15f1d3e90e2def24c47903731e1803ebfc2dc7e03da9edb3323eacf007ed250783940377bed146530c59a2e3380aa7e61e98e7f9d723f1bbf6bebff001869efa4683248a24531c972a5d02ee18dc3272ce32703d79e3a53a705cea72f862ff214d39a51be9b1f9a23c790af3e70e3df0476c73c03f8507c7d18cfefb1d7049ebf4f9b93ef5261cd25b2d1f6213f1062500098738e30bf9e4139fe755e4f88712823ce1dfbafe6412718ed40e52daeff00233e5f88d11e3ce1e9c11b401cf427d40eff00e3544fc424dd81718c93fc6bcf418c13d78f43410e56564f72c47e3f5247eff238fe25fae4fcdc9cf6cd5b3e3952b91718e99c9e47e23d80fa7eb409cf75d8acfe38e73e76ec7b818fa7cdcfd7dab32e7c7593feb989c8fe3ebe848ee7818ff2682799eb77b9ce5d78c259720ccd8e40c36318e327d0fb7e95833ebecec7f7ce7aff0010cfcbc7"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e85000011987b36807900003258406328170040ffff4079a09bdbd0ca97592738773ee4f24fb67a7e5dea849aa31cfef189ec33dfd093d7fcf41416ed6d3f4febf12ac9a931fe33ce09c9e07aedff00f5d566d408ddf31edf4f6c1ec73ffebab5b356724425b159f512bd588e41fbd8cfd3d6abb6a58cfcddcf718efd7f3f43ee7bd696d1277febb956b37d2dfd77213a99ecc7a7af4edd07d2a26d489ee4e01ea7a93f8f278a124af62f78beb7febe446750c918739e474ede8bcf3de98da80e9b8e4f1c8ea3d303e83d714ccef6ba18750cff00167b74c63ea48ebc0cff002a84df139cb11d3a1e83a719e878e28f90d47677b7e1fe644d7f8c1393c8ce3bfaf6e48c66a06bfe7a93d7bf27b73fe45054777aedfd75206bee49dc7a762077e873d39f61e9ef559af08cf2d81edd7b63d8faf6a4b4bdddede562afaaff0081fd7c880de1271b9be5e0e48efdfdff000cf6a89eedcf058f3ea3907dfdf8f514691b596fd8add3bc9dd109b96191924e4f6ebef9ed50b5c67825ba8efe9edb7af3eb4b996aecca92d129abf98c373db3d06083f788e9dba9ff001aeefc257bb275407e6122e14e39f7f61ed512575cd635a4d73592d169ff000e7d67e10d676ac5fbc3818e32391c7233c83c57bae9baba3a2fcc7271dbe98007afe3506937b2b1b526a2ac872ddb1d7a7b7538ff00eb573f3dda92df3679f5073efcf53e941999925d02080ddcf19c8fae07e39feb545a7e4f4233d028e4fb71d79ffebd007e591f8b3aa724020938e1d9801ed9233d0f6f6f7a60f8a9ab9fe263d07dee4e490060ff002a4a51ebfd7fc39cd24f99592b2233f12f5671f2cacbdb93d3d8eee871efcd20f1f6ad27deb961dba9e47a1cf5f7abe6bd9a4ade8438b7657febc870f196a32119b873d4101bf0e39fe9562dfc55745b9b8938f57e4838e7afcdfad48a4ddd7f74ebec7c472b601b890f41f78a9038e5bb67a71ffebae920d66460312b724ff11e3b73f3601eb4fb8692deebfafc0ba35576fe36ea4753f91f98e0fa74fc69ff00da1d72cdcf3c927207241f9b9e9485a592b6b718da86ecf2475ee3e9c7a7d2a06d43a12dc638e7d3b9c1e4fbe681c95bd5959f50e9cb1ce7bf6f539ce3a7ff005ea06d4003c3027ea383ea3191d3fcfada8b77d09dada1036a1d72d9ebce7f538e98aacd7fd72c7b6327a7b75ebf8d546f6764bfaf987abdcaed7e392589ebdf9ebd067a722ab3dff5393d49ce47e201fc38ebd29dedf13b5cb96a9595fcc84df31c727bf527d0f5e78a69bb233cb8c60f00fe47239aa04bdd7795d790a6ea42a0ec97fef9e9efd0fa53bce9dbfe594bd307e5718f4cf14b99772b97449c6dfd77ff00871035c11c4720e0ff0001ebedcf418fa53c4778d91f679b391d8e0fb9f5c63f0a9735d06a1ccb5e9fd6e2fd975072545b4a7a7f0e3f019eb8e3bd28d33546200b693a818c74cf707f2f5a4e7d8152bd9cbdd251a0eb0d802d9ba81d4e0f538e9c7e7c54a9e17d6dfa427d3bf1f5c8e48f6fe54b9f7d3434f67b2be88b09e0cd6e4230871eea7923a83839078e95613c01ac3672a7391d11b1f539279e7f0a94dad996a295f4ba2dafc38d55b82ec1b91811b71eb8cf4ab29f0bb5163cbc9ce7f87f50318ff3d68bb5d587226ddd5cd183e14dd120b098e31ce3803d0f1d78ae834ef878da7ceafb64dc31cb6e238c71f951abeeec38a51bd95be47a369f04ba78451b805f63c63d3d3a576363e2592df0189ea38cfebfa522af7e86faf8b7e5e5c67d9b6e7b71f3704f1c7eb4dff849924382e3b77191ed8c7d78a044bfdb28d8f9ce72c7a8f61c7381cd2ff69a1cfcd8ebd4fd3a7271f9d007e37ac8c33c8fae3f519c64d3fcd2bd7380463d0f6e9d8f5edf8d739c7cb6697f5f88f598e7ae339e879ed9073d381d7faf352adc380464e4e7a03f98cf39fa55f3592496dfd7de5c135769e97d8985cb707736e07d3a8f435a105db6e1c9381fc27a0e7a8ea3fcf356a5a5dab12e2d6ef7feba9d2586a1246ca09e73c363001e80fddeb8fcebadb6d54b1cb3"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e86000011987b368079000037f0406328170040fffff180482dd7a72bb8753fe4d514a374efba7f7fddfe67ffd5fe549752dc00de4a8c0d993d78e9f2f5ab2ba8704061d4f190013d3818c0ff00eb57a4a2ddedd0cdc5733f7843a9e323783927827818c0cfdde473d3b542da9e7037f739191cf51ce073deab95a7a7bbf313765cadbfebf12ab6a7d4ee27a0c63381e99c8cfd3b559b5796e9c6dddb7200e48cf38c96c7cb576ddb5fa825b257f33b2d3fc3ef71cb02c0e472b9079c71f2fcc7fcf6aeaa0f03ac98261cf03f1c74c823d2a6e9de293348c796fa9757e1ea360984f1d4a81c7bf0bc76fcaa64f87898e6dcf6e8b9ce339278ff003eb445493d515db42d2fc39438ff004739edf21071e98c1c0e3f955d8fe1c2631e47a8c6d23231d08239a2d6fb2bfaf2ea2d8bf1fc398c63f70a08238c7a76000e38357a2f8771823f70bd4754e9dbd38ce28e4b8ef7566f4469c5f0e11b69fb3a8e47400edcf38e471ef5a51fc378f8ff0047cf4e42607a63eef278ebfaf7a5ecfcff00afebcc7cdbfe86943f0d14e36db01c1e768e7b678071db8ebfceb522f8668307eceb8e074e38eb901783803bd57247b0b99a4d5f43461f86f0e47fa3a9c679c0fcfd9ab463f87912f5b70dd7b1dbc7d791df8f7a7ca9741a9bf534e0f87d09c0fb3ab119fe0da476c9e39f6ad18bc01073881720e0e54107af2001c9ce68b2edf80f99ef6d7fafeb72f47e0288e54c0bc67f83a0f45f4e7fcf7ad587e1fc442ab440b0c720003e9d3af18ff38a56d5ae5497a09c9b2e8f01c0a0edb75e0fde0a79c74fa74e7ad737a978261495774640de32547505802463a138f5ef435eec95ac272db999ec9e0ef801a4789208495462d8e1fa838f76c9fcabd2dff627b1bd8c3c08e19b3f77e561939f90ee5e300f56e31d6a54134b53d2a94a2a116b7b1c0f88bf61ad7a2577d2e7bc882676fcc640c7ae4ef56c01c7461f5af9fb5ffd997e2568523a88fed0b1b1c7fa3bae54772ca4f3fcbf314dc23ad9ec70f3dae9ad99e7d73f0e3c7fa76e5b9d2652509fb85b919ea03c639ac79f4cf1069f9fb5e9b7716d0013e5effcb6e491c7f9153c8fa6a5dfccfc8f120dc0e463ebd7a700f6fa726a4121c904ff0088f7e3a31fa66b976bea72cb469a7ff07f51de6e3209ce0e79047ffab8a3cdc639ee7b91903b91b79e7a7f9342f37f80a4a496aed6f97c89d25ea09249c76e3e9920e3afad684321c9c1cf4f7fcc0e9ffd7a7a27bfddfd68169e8ef7b7f5d8d38a42b820fae72381d7818ea7a715a31de32f1bbb85fc7d7ebfcab64efb3097359b4f434975320ae6423b67279f51c0e4f27fce6ae2ea2481f39001c7cc7048e9838e8473915716e3d3dd239a4eebb8e6d408c7cfc7d0e0e4ff00bdcfe27f0a81b50ce4f99f2e41fbc1bd803f5e29e92d2fb797f914e2f4bfa6fb0905cb4d3ac5b8e19c0c63f1c1e7dbff00af5edde0fd13ed1e5ee52c09cf03afbe29f3593d36dbfae845fded2fa3fe99f46e81e15dc885603b46dc90071dbf03efff00ebaf53d3bc26a026cb73e84b263f334a1d5dcd1cfb2d4e9a2f062b46095391bbaaf18f4e53f966ad45e0a00f08571d32ab93f500838c77feb9abbf912a4f6b97a1f0729e0c6092064e071e83d8f538e7ad5c5f0822e47967d17e4ebea402bd71dbd698733ef7255f07a2e0987ae70db40cf5f958e318e07d2ae43e1051d61f41c20c1ed90578ed47cc5cd2ee6b41e15518c46309818da327d873c1c75fceb76dfc1f113feafd01f9739e07038c1fd3eb42d3a8295aead7f534e3f0a2020794719f4fbc338c8cff001633c0cfd7d6eaf8610f0221800e55936e4e4723b9fd7af6a362478f0ba0c284500e0f43838ebfc391fd3d2a7ff84697682635cf38014fd300b0c13c8e28ee545d9ebb12c7e1d8d7384195c2e081c76c0ec3a74ebeb578682801c2ff007472a3f103191d3fcf4a872b68d58bba4bdddbfaee4b1e869fdc51d3391c83923e5c8e9c76e3e95a89a2461176c64ae0023ae769ed951b7a9ebf98a4e5d53fc07be97ba1"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801a9e87000011987b36807900003d88406328170040ffffeda4aaa1f914ae5b8214118f424f4f6fe75c56b7a528e4a01861f7976e7d31c0cf4f438c545deba932d2caf7b1ecbf0d6e92d3ca5dcab82ab8c0e7dc91d08e6bedcf07ebaab0c6a5c7cbb41ea39e41ce0e47be47e75b43e147ad29374636d5348f66b4bed3ae611e6c10b7017ee05cf1d47033deb95d6b40f0e5fef325a26e39ce234383ef9fe5438adad6389fc4d5b7f23c9f5af869e16bbdd8b683e70799225e3df2071c2f4f4af25d7be03f87b5047f2ed6ddb2319545c8ea72b9079c023159f2ca3b31f22d743f8c613b12467f51f4e703ae69e267e403f4c6323dfdb915ca70c649ecb543bce7cf5e3dc0c1ec7b71d39a5f35f232c7db8e9ee09ce28f2b17777bd9ff005ea598d89240cf5eb9edd813d8e7fcf7ad680be0104e33ea7afe7cfeb549b4ad7d8b8c934eeadf334a3278e0f7c9cf279e09f7e3356a31d08196e08e7a7bf5e4f1ff00ebab86a9ddf50d236b22eac40e0e7f103e63838ee3d8f7ff00ebdd54f53d8678e838eb8e9c7bd5f632bd9a6d8be5e339273c8e9f8f1b8f1da831e73fdd1918cf700f1fe7ff00af43f245bb36ecf5f5b7e66c6836fe76a3046c07ccdd36e30011d7d0ee3d6bed3f016801a28401d36e38e833ce720e3f90fc29bf852f525df46debe96febef3eb1f0f78715238f08371dbcb2e36f04e40efdb9e31ef5e9d63a18f942a90c4027e5c903a02c0af1ebdbda88e8d19ce56eba9d65b6848d85f2c311c15db9c63bb7ca48e3b77f5abe3418d5588886413c05eb8ed82bc638382462b6f208caf6bdc823d254b1f93637385c60367be18601dd9fcfd6ac47a42e3263380e33f27e381bb233f9d174ba8396974ae4dfd9409004640007f0939eb803819e9532e92a3691174e8413907a0c9ec7db3fe34aebb8295fa688bf1e92030daa4f4ff967f2e4e300e0671c7ad6ac7a72a29ca8c81c7cb8527ae0e472739efc7bd2ba49db7f3ff820e49752e4160bb43824fcac371000071d795e4f078cd489691ed2cc3736ee318e467a8f97fc7a1a86dc93d3444f3767f78cfb322eecf6383c281e992493c638efef9a6adba12782085c619476f5c678f4c7ad4ddaeb62f9862ac44ed2a0118e4a1fbbc7a8f6a914c7b54e4155dd8254763d18b75edf974a3bea3204b8863e09524310091d707079e73ce727b66ad7db20dbc3e4b67b71c0fbcbdc1c67b75f5a2d7be9b0d3b5ec2c773015619e724838209e9cf3d4e077ae2f5e962e71923777ea48ee727af4ff00eb520f2bdd2363c29a8f9522052c30c3047619e41f4e6bea0f0b6b0e5211b8e7e5009078ce338c74e739e3f0ad6324a28f5d37ec62ba9eeda46a92b463049e339c74f7391c1c76a9eeb537009cb9193d48da3839230791cd545f35f43924dc5dd239cb9d51c64673cf4239007607b73543fb589ea4f5f6e33db939fc3da996ba687f0cfe981ffd73e871f4fd69c3231d3bf71fae6bcf3cfdeda92283c0cf70dd074f6f5eddb8a9546e207eb8edebd79fce99a6ba2dd7e3f22fc11e7af4e38007af7e7aff009f6ad881718c67d390bdfb823bff009f7a766ae8695d5daebe5a9a91479c020f18fc71df83c37a7a7d6b4a28738e4f600e4648f5f41df8ada2b45643ba8a49a69fa9a91da30c1c3020f65193fc20b0ce7bfd3a75eb5756d98630a720f39c63e87d0fb76a6b5d1bb19b56d1adc4fb33280082bdbeefb7decfae6a1301cedc1e07a75c1edfe78a1ab5d7629d371d2daede9f71bfe17848d5a05c36438f46dbea4faf6e33fe35fa11f0d6c91e28189cb0087183907df0415e707209e9d2a9a6a0b4ea66f75ab3ec0d034d530c655540cec2493bba0f997033dbaf5fa835e97a6690bb826de0003214641f43cf078eb8fd6882d518d496f75748ffd6f9e2cf4948f395dafca8078ec0e09ee7800e383bbad589b4f884636954601b80a38fe2c0c37ca7771d3f035ec7c8f3a3aa5a9946cd0fdd50719ce146e3ee0bf4fc8e7d2a692d6358c6d382ac319c1c3e30490073d78f4f5a8b735ee9a486df2dc8a6"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("809a9e88000011987b36807900004320406328170040ffff820d8a3ccc1c019dcb96ee413ebd703bfad5691a0418538c11d08665e99ce57d877e39fa555ad6d76136da6d2febf101736ca41dff0036006f94727d14af0063d7ff00ad5623beb7c4819c1f97008e40e800caf53c75edd3159ca2d75b8a2dcb4b0b0df441766e6dd82148239ea31cf538c7b0e951bea308c64a9dac3233e8718c678e073cf7a694748db998da96977cafd3fcccd9756505d14c7ebd181ebc607a7a7d2b3a6d6b6bb2971d0f0064b3fb024fe78a4d72cb6d0d231bdee66beb2cb920e0e31c1e4741c818cf7c73f8f534c7d59fcb3b5cf461b49c67d41e7927ebed49f2f466a95b43065d6260c32e37e71d7b73c7bf24e3fad68da6a4cf18049666c0e58707d720703fc69276d98e2af64ff0372da525480c7732e49238033d3e65e4ff002f5ac1d65778505d88de0745e39ea71d0e738038f7a454e2935a1bde19848923c70370c90076e0608f7ebd7eb5f4a78563e21ce78c7ae7b7523a03fd3d39a7e47a4972d38ddf43e80d2202d18dc060000647240ee373706acdcc05b8cfaae48fd4fcbedeff00d6b68ae55639a4f55e473f7368d9f949e7b01839c75ce7278fa7bd575b139e578c0e8791ea4f3d7ad5c62e438b5aa4b43f")
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame restartFrame = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    restartFrame.Add(interpreted);
                }

                //Draw the frame
                using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            //DRI Test with Q Tables

            jpegPackets = new byte[][]
                {
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac52dfc1120a71f940d280000000041ff64380054ffff0000008024191b201b1724201e202927242b365b3b363232366f4f54425b84748a8881747f7d91a3d1b1919ac69d7d7fb6f7b8c6d8dfeaecea8daffffffee3ffd1e5eae12729293630366b3b3b6be1967f96e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1562c46727f3a15dbfbc7f3a5a674354049b9bd4fe74ddcc5fef1e3de97b669a9d09a007ee6f53f9d2ee6f53f9d25252017737a9fce8dcdfde3f9d149400bb9bfbc7f3a6b3374c9fce969a7ad003b71f53f9d26e6f534525002316f53f9d26e6c753f9d29a68a0032490327f3a9371c75351af2f525002ee6f53f9d2ee6f53f9d36968017737f78fe746e6f53f9d251400bb9bfbc7f3a5dcdfde3f9d36968017737f78fe746e6f53f9d2514842ee6fef1fce8dcdfde3f9d2514c05dcdfde3f9d26e6f53f9d145200dcdea7f3a82e99b6819353d47326f5f7a6051418566fc295325873523465531df34d40435228b409f5351c89bb27bd0ac694374c8a40550a43e6ad46e7d4d324c1e688cd3116431f53f9d2ee6fef1fce980d2d002966fef1fcea392461d18d38d4131e68010cae41e6abb13eb5275a8cf4a602827d68c9f5a414b40064fad193eb451400b93eb464fad2514805c9f5a327d6928a60193eb464fa9a28a40193eb464fad14940064fad1939eb452138a009fccfdd819356490610371dc2a92e4e2adb1f997e523e5e69a11547fad19269e1c81824f5e6a3932b283e86a661ce08e28034d49500063d3d694b37f78fe755206c48cb9cd5aa0634b37f78fe74c2cdea7f3a71a8dba1a4040ec49e49fcea2763eb52377a85a900dc9f53464fa9a28a00327d4d193ea68a4a005c9f5a327d4d14500193ea68c9f534b8a72a66801bcfa9a728627ad48230294a8a6022a63a9a9701aa16042f5a62c8c0f5a00b061534c300a724c0f5a937a9ef480ac6dfdea1953cb3c9abe597d4554bbc134c08510bf4a93ecefe95259af157d680328c0e3b1a698d87635b381e948514f614018db5853706b68c319fe1150c96f1fa50065f3454d2a053c5478e2802239a61a91a99400da5a3145001494b494005145140051451401b948c32297b52d500ccfcb8a728c2d348c1c53c74a0028a28a402514b45002537f88d38f1482800a4a5a2801a699de9e69ad400b1f734fa6a0c0a7d001451450014b45140828a28a0028a2968012968a2800a4a5a290052114ea0d302abe771a611e952ca3e6a8cd494354d381e951a9a7d000ff76a356c76a90fdda544a00557f6a56955464d382d41703069885172b9e4114d76dcc48a82a55e94ec0390734184f6e452a55950b8c55c55c4523111d8d3769f5ab8fdea22293885c8307d29335a0b6c8cbde98d6c3b7f2a9b0ca79a2a76b73e94cf25bde8023a5a531b0ed4de4751400514668a00292969280129ac69d4d7a403e0cbc8066af4c856451b89e954ec8667157a5f9ee1154e0e7ad31156e9082411834f61c2e7b8a96ea36f370e72734c61f741a6c01728437bd5f46dca0d29b556b7e3ef019a8add5847834807b544fd0d4cdd2a193a1a432bb74a85bad4add2a26eb4806d14b45007ffd0a5452e29ca84d201b8a72a134f084529dc3b53000a0520f95b1d8d381cd04645021c28a6a9c8a7d002aa8638351cb6c464af352c7f7aa7a068ccc943cd4a920ee2ad490ab8f4354e48590fb500492ed2995eb549d8fad4eae470691d11f91c1a009ad50ecc8356c6ea82df01319ab2b48002b7ad2ed61de9c28a0630b11d4545248306a626ab4e4629814e43934dc6053c8e69ac38a04567eb4dcd3d8734c3400a28a074a28012929d494009452d1400945145006ed145154035b96fa52af4c522f3cd0783400ea28a2800a28a29008dd281d291a9680128345068010d31ba538d34f2c050048bc0a5a28a042d14514005"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac52efc1120a71f940d28000004ea41ff64380054ffff145140051452d00145252d00145145002d14514802968a2980c6424e4542d13135669719a00a821c52f975676d1b28195c439a788f1560251b681106daad7430df85686caa97ab823e94019fdea502a2032f8ab18e2980fb703773e95617048e2ab2ab01919a5dcc3f88d301f211fad47de82d9a58d4bb851d6988b91bc8102e063de9e25f58ff005a034ca319047d29f1972c772ae31e948058f636728463de9dfbb5e0a8a0b6d3c00298d201f792818c916df272083517d9e27380c4549be33d4107d29404fef73405c81ec3825594e2abbd9ba9fba7f0ad78e1565cf0734efb38e71fcea42e601858530a30ed5d17d9d46edcb9cfad549ad509f95714586631a637356668b6b95073ce2a296164383d68026d3d7f7b9f6a9f045c07519e7815158614331238ab360c24bbe9f745021ee19d94c83e63d6a3719980c74e2ac4a70d9e986a62a86ba000ef4d88d38d06c1f4a6189403814f55db4afd38a91945c60e2a093bd3e69312115139f97340c81ba5447ad4afc0151e32690094aa858f02a4f21c0ce2951f671d0d000b1e3ad3a9f90dec69a4629884a28cd26e1400857b8e28c91d453b228cd0047b86ec8ef52034d74047bd22a823ad004d17deab02abc4809a9f663a1a4342d35b18e68c35285f5a0651b98c2fcc3806ab83cd5dbeff00562b3549df8a622cab7be2acc331ce08cd43e436d047342abc673cf1480bde6e072298675a619c3263bd445b3d85032469bd0544c59bad28fa529a044610d3240429f6ab005413360114c0a858679a61a56eb4da005ed494eed49400514514009452d2500145145006e5078068a46e98aa005e9430c8a514500229ed4ea61e0fb53a90051451400dfe2a5a41dcd2d0014868a434008691797fa529a13bfd6801f4514b40828a28a401451450014b4514005145140051451400b45252d002d1452d300a514829c280014e0290528a00514b8a052d023fffd1bd8aa97ebf2a9ab955af86625fad324ca4c0979ab19523ad56271253c1a632713b28c0da40f6a6b4c4ff00081f4a8fad054d002eea96d9cacea57ad406a4b66db3038cd17035d65948e5054f938ce2a9addb0eabfa52b5e31e828113993032541a8ccb093ca103d6a1fb537f914a2e8679fd45004b881cf04e7d291a346390cbf9d209e239c01519087b8a009823a7dc240edcd491f9a1f2c7231559557d7f2356030db80fda8b012484b2e055592371c824605398b1079fcaa277915490fda98ccf3feb973eb4e9d7748c0fad31f24e7bd4ccc2501bf8b1c8a4056d9b7e51d4d5fd1d30d2b1aa8ca54e48ad0d2b8b791bde90316552c1a9b63ff1f209ec2a78f079f53512622bb20f01b8cd3e8234720d364c6da081b7da9931c464d48ccc98e5cfd6a3c9efcd39cf34ca431ae7279a8c86ce51f069cf953b874ef4e1b5866980df36e3bf351b3b31c3281ef53ed23a1a383c30a0445fbd03dbd69c0337de929de56394247b544cac0fcc3f114012f91df71349e40fef1a8fcd65e869cb3127e61400be49ecc68f29c747a779aa7bd3c10475a0087322751914a920dd8a9aa39230c323822802c4153d56b46ca9cf5156690d05145140ca97ff0070566a7320fad68ea1d0567c7feb07d6988d78bee0fa549b41e08a647f7454829010490283902986302acb544d40c8b6d0452d140087806aa4dc8cd5c7fb86a9cbd2988a8dd6929c7ad20eb400a7a52538d250025141a4cd002d2519a2800a28a28037290f2ff4a5a68e4935403a8a28a40045229a5a69e0d003a90f00d2d35fa0a0017a52d1da8a004a4a5a4a00434abf769a7a53c5002d2d14521051451400514514005145140051451400b45252d0014b494b400b4a29294530169452528a00514b4829d400a2969052814082a2ba1984fb1a9a9b20dd1b0f6a6060c9c3d3c51329dd4018a431c3ad4bfc383508ce78a97391cf5a6808cd"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac52ffc1120a71f940d2800000a5841ff64380054ffff3a3e1c51b73428c302451602d2b1c50307b9a4c0ed8a304500291ef4678c1a0311da83f31f4a00303a8eb413fe7346dc5348f45a0077d09a6927d4d2723b1a7ab718e6801bbc8fe234acec54fcd9a319effa531f80471400c2ebdc53639023938e2987af34dea69016dfe65c86ca1fd2a4b659becc42711e7afad536de130a78ad783e5d3d077a005810ac6a0d2490198b7639e0d4a9d0534dc793bb77dd1cfe34086c5313f249c30ed4fba6c45f5aac41b81e629c3f5a86e27938475c63bd032363cd3734992683d2a462ee0462a32a54e53f2a7792a7d47e347938e8e69808b30ce1b83526430a89a173d48351989d4f071408b4a78a755555997de9e1e41d52802531a1ea2986043d0914c3330fe1349f68f553400bf6527a30a431c89da945cafd2a55955ba1a00ae1d81e49a78958d4eb1799d40c5249046a3ae0d016228e46590e3bd4e267a885bb81bc1a4791957951f85031ed74c1f00714e170c474aa425c37233561668caf19cd210f9c89472306a18e100824d3a4940a689d49c5302ea48bd2a40e3d6a982beb4f504f209a4172c31a898d26581e69a5b2681dc5a5a606e69770f5a0057fb86a9bf4ab5230d9559fa5302a30e6851cd2b75a551c5002514ec518a00611498a7eda36d00331498a7eda36d00328a52b40a00dbed48bd286e94a3a5300a28a2803ffd27d21e94b494c0407b5237de0283c1a072c6801d45145002521a5a434009d5853e98bf7a9f4805a29296800a28a281051451400514514005145140052d251400b4b494b400b4a29052d002d28a4a753014528a414e14000a70a414ea042514b45005736b1efddcfd2a8cf1e1c803bd6ae29a514f55068031ca95e4534935a5344bfdd155cc2be940ca7934a18e473564c0bef49f671eb400e04fa9fca97771834df2dbb350564cf068024054fa5263fce6a31bc750694311d47e94c079269013e829bbc13d29e0a9e08a000f27a52b000719fcea2791578c0a4525867a7b5003f27a8cd35c9dbc9a7e4e314c947c9d4d0057279a0534f5a7478dc33eb480706ed5ac5f11228acb68f74b85f5e2b4826d08a79c5005a5e8b59fa83805877ad28c648acabe52d2b7d68e808b76bfea97e9515e619b06a6b71841f4aaf39cb9a408aa5197ee9fc29ace71820d4d4d75dc30281912cdeb5287069820f5349e491f75a8112163d850bfad4444a29bbe414016b3ed464fa5575909ead8a901cff001d00499f514d2a8dd40a4f987420d27998fbcb8a000dba374a916c063ae0d2c4ca790c01f7a9fcc23d0fd0d03206b6910616438a6ac6f19cba96f7ab3e729e09a50f838ce4520182446523383ef557eca5dc8ce2aeb468fd547e150bc253e68d8f1d8d0046b61ea6a58ed523e7ad209dd3fd62fe22a65955870680285f280dc0c5578066400d58bf397a82dbfd68a181a61171f7452e00e828ed484d031ae698769ed8a56a6d0210c471ea29a4606315282477a060f5a62b15ca8238348d19dbc54ed19032b4919fe161405ca063e69db302b485ba3fb539acc15f9680b995b694255b92d5d3f873512c6fbfee9140116ca4d95a0b00c734e36cb40ae666da4db5a5f6514cfb36680b99e529365681b534c36c7d281dcb07a8a7537ab52d318b45149400b4945140087a522d29e869bd2801d45145002521a5a69ef400a94fa6a70053a90052d251400b451450014514500145149400b45252d00145145021696928a005a70a68a70a00514e14da70a6028a753453a801452d2514085a5a4a5a0028a28a008a5155249110e09c55e619158d7472e6802dac88dd181a764565834f5761d188fc690cd2c0a368aa02e241fc59fad48b767bafe54016b6d1b6a15bb43d722a45991870c2980a5298e3007bd3c4a87a30aaf3c999540ec6801fe52939c734bb2a45e94ec50041b29194918a9f1404c9c0a00a9e41ce451f667ea056bc36"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac530fc1120a71f940d2800000fc641ff64380054ffff7900bf1569608d470a3f1a008a3b585911820071da9258f0e31d053da658e65880eb4e7eb480620c1aa170a44817aee357c746354e4f9ae947a0cd3604ea36a1f61542439357e43884d67b75a96343695ba0c1c1a28600f5a10319bf07e618a7034c2181c0e47bd3012ad82314c09e8c0f4a62b0a783408428a7b530c087a7152d14010980f66342c32e701b3535390e0d007fffd36797281f346ad49d3ac4c3e86adee346e34ae32aee8bf8830fad383c18c6ec54e4a9eaa2a32913755a04426474fb8c185396ed5861b8a56b78cfdd383519b507a35004f148a5704d0d1231ca9da7daaa340eac0034e533afbd0045701b790c734c80e2514b2b3337239a6056ce475a606a678a426a9a4cea30c334efb4e7b1a404ed4c660a39a8fcef6350cb26ea2c32dac818641a506a844e509a984d408b7bb68245200255c8e0d41e702b8a58a60a39a6265989ca1c355b5208aa0d346ebcf5a48ee8a300791412d1a27a5465067a522ce8c38614edc0f4a0418a69e29d9a6b50034f0b4018148393f4a71e94005252d0280205f5a5a074a299a077a28a2800a28a28010d2119141eb4b400d5f4a5a43c1a5a0029add29693f885201e2969296800a28a2800a28a2800a5a4a5a004a28a2800a28a280168a4a5a0414b494b400b4e14d14e1400a294520a70a00514ea68a514c05a5a4a5a005a28a5a0028a296810c90e2363ed58939cbd6cdc9c426b125e58d034474b4945031734669292900ecd3e39366ef7151d140031c9a013907273494500585ba7500119a916f17b822a9d18a00d15b98dbf8856869e124dcf90715cfe2aee952791705b9c15e40ef408e8aa09aee183ef373e82a8ddde4a633b7e5cd64ef3e6ee6249f7a00d16bc57bd47e8a5ab4cb035ce39e2b434b91dd1b731600f19a00d23f74d52419bb63e9571bee554b7e6476f7a18125c9c4607ad52ef56aecf41e82ab6293286f4a4a73533342131c3a8a49103639a334531119423a7342be383520a0a861c8a0050734b516d64e9c8f4a7ab03400ea28a4271400e04d1b8fad3334b9a4314b1a50c7151e72696818fdf49ba984d19a402bb647b8a04bf2f5a43c8cd412b6d045302295c972454d6c4b1e6aa8e4d5bb618a044eca08c115098b072b5331a6e681909931c15aace726acce3e5cd5534c420eb53a1f51502fde15692801eacb8e94b115c9c8a70031d29102ee391484cb51a467b0a592d518640a6c6a0f438a937b4670dc8f5a7726e544813760e454df666032921a7cca3a8a5864dc3140322ccf1f51b8534dcf660455c2702a128ae4961400892291c1a7e78aacf6f8398ce29be64b1f0c32280b16e8a812e14f5e2a60e0f434088e8a5a299a09451450014514500347534b40f5a280108e2901a7534f06800348bf78d2d09ce4fad201d4b40a2800a28a2800a5a28a004a2968a004a28a2800a28a2800a5a28a042d1452d0028a51494a2801453853453850028a51494e14c05a2814b408294514b40051452d007ffd4b17c7110158efcb1ad5d40f41ed592dd698909494b4940c2928a29005145140051451400b4b494b400b562d321c91e955aaee9eb90c6802590975209cd543030c923a55e75c76aad34ac06d23f1a622ab5696940794c7be6b318f35a9a50fdc13ea69017e4384fc2ab5a8e33ea6a6b83888fd29900c463e94010dc1cb9a880a7c872c693b549444c32699b453cd255086edf4349923a8a7d250200734ea6ed07da8c37ad003b34d650791c1a307d6976fbd0030391c352b303de9c6306abc9f29f4340138a4cd422618e68330a432614a4d57338a699cd160276346eaac652693793de8b016448338cd433e738a88935303e6479ee3ad302141cd5a81c671dea2119eb4a8855f7669302d31a8649028a4965c0e2aab3963cd031eb29dd86fba689531c8e86a2a922906363f4ed4c43173baac2353447b5b3"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac531fc1120a71f940d280000153441ff64380054ffffdaa4da0d004a0f14e0a0d41f32fb8a96370690993db55a2a19706aadb1eb56c5043dc80e57e46fc0d47f71f70e956a4dacb86aa528283da98d6a59cee1ed494d8db318a7a8e29083148c3da9f4d6a60446046ea2986d88fb8c6a714ea02e45494b453340a4a5a4a00283d28a46a003b514b49400521a5a43400ccfcb4e4fbb4d61daa414805a28a2800a5a4a5a0028a28a0028a28a0028a28a0028a28a0414b494b40052d140a00514a2929c2801452d20a750028a5a414e14c402968a5140052d1450014b450280337506f9c8f4ace3576f9b3237d6a91a0684a4a5a4a062514b8a4a40145145001451450021a41914ea4a00335a9a6afee73ea6b32b66c1716e9f4a04c9d90375acfbc8ca9ad4c557bc8f31e6988c535ada48ff0046fab1acc91706b4f491fe8e3fde3fce90d966e8fc84503e58cfd28b8e580f7a24e23fad0c1154f5a1ba504e0e4d559e764936f047b54944b45229c8a5aa109494b4502014b4829d40828a28a002a9dc9f98d5caa5707e6340c8296928a062d1499a280168a4a722963814085552c78ab5145b053a28b68a9b1c526c640f50b1c55871559fad0313ef0c1a85860d4c2a17eb4c436929692802cc0e40c1e454f807a5568871538a40498a4f2c1e9c1a33c5394f14098b0318fad5b562c38aaf0286073dea4c3427232528219385ee79a86e46074e2a6460c322a2ba385154816e5752531e9569181031504233d7914f28d19ca723d290326a69a4470c297b5021052d2528a0647451453340ef451450014d3d453a9a3a9a005a28a2803fffd55a4a5a4a0069fbc05494c1cbfd29f400514b4500145145020a28a5a0625145140828a28a0028a28a002968a280169692968016945253850028a51494ea00514a28a514c42d28a4a5a002968a2800a0f009f6a5a6ca711b1f6a00c5ba3973f5aaf534df33e29a2227b8a0688aac410874dc4d44e854e08ab76cb8896818d36cbd8d30db1ec455bc5348a405336ec3b530c4476abd4734019e50d26d35a0541ea0530c487b62802890692ae98076351b5b9a6056adfb6016251ed58a6220f22af5b4cce369ed482c681703bd47248aca549eb50934ccd170e529ccbf35686983108fa9aab7239ddeb56f4e23c8e3df340992c9ccaa2898f00503998d3663f31a18d1566385ace6e587b9abb70783549799145005d4e053a914f14b4084a29370f5a5cd00029d48296810514514005519cfcc6af76acf9bef1a0688e8a4a2818b45252d000012702af5bc3b464f5a8ed61dc77115780c52602018a08a7518a43207155a4eb56e41c55593ad0044c70b501352c838a86a842d277a29475a00b11f4a956a38c71520eb4807f6a09c0a3b52757028132d40300558c8c60f4aae870a2a7541dfad066420f952601f94f4a2e9c6d029f3420a1238239aa4ee5c807ad514bb96adc612a6351c5c28a591f0292248e4c09015eb520ce2a38d493b8f5a94d03128a28a006514b45334128a28a0029a3a529e94500145068a004a434b487804d00227527de9f4d418514ea402d14514005145140052d14940828a28a0028a5a280128a5a2800a28a5a0029c292968014528a414e1400a294520a5140870a51494a2980b4b40a5a002969052d020a8ae8e20352d56be6c4407a9a00cb638c91d738a7a0c404fad421c64ab743dfd2a43227961770a0a18c7f77cf63c55d886d451ed59eee0f03a53d6ed95403ce0628197e92a98bc1dc53c5da1a4058a315109d0f714e1203de801d8a4c51b8519a00314514b4086914dce0e4751526283121a0637cd34d325452c655fe5278a9238db193400c7258559b0658d58138269850fa54720f9714c0d28c824906a290f24d25b8db08fa5239e0d4b029dc1e0d5688069706ac5c7dd1505bffada6059f2c81f2b7e74e5257975240f4a70e94b920100f5a045395fe6cad4d1b654506104e734be52fb"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac532fc1120a71f940d2800001aa241ff64380054ffff8a007834ecd44223d98d2ed907a1a00928a8b791f7948a70707a1a0438f435464425aafaf26a6f250f3b68198fe59a3cb35ae608fd293c84f4a00caf2cd3a384b3815a4615f4a548c03c0a18cfffd674681140a7d388a4c548c4c5380a314e038a00af2d5393ad5c9aa9c9c9a008f1b8715132115623e0d48555bb531143069547356cc20d22c201a60220e29e3ad3826282306900b42fdfa334b1f5269899607dd07d2aca9c8a86300ae29e8db721ba8a466c7c842c649f4acb1f33e4559bb9f2a5474aaf6dc9a7d0a5a22d44d85a50371c9fc2a39176b8c743538e82810a060514b494084a29693340c6d14514cb128a28a0621ea052d37f8a96800a28a28010d35ba538d35b9205201c3a53a9052d0014514500145145002d145140828a4a280168a29280168a4a5a002945252d0028a51494a28016945029450028a70a68a70a0428a70a68a70a6028a5a4a5a002968a2800aa5a8b7007b55dacdd45be73ed4019a7ad25145050535a9d486801940a5c518a4026697711d09a3149400f1338ee69eb72e3ae0d4345005b5bb1dc1a996e10f7ace1d694d006a8753d08a7641aca5623a1a912670c3278a0468ed0680369c5311891d6877340c949c556b839c62869b3c544cdb980f7a606927108a8e4e95274503daa293920548152ef8c0f4155edff00d654f7a7e76a821fbc2a9817874a2a30597a8fc4538383de900ea4a3345021c28a05140829ad1a9ed8fa53a8a006057561b4eef6a9d251d1b83ef4d4fbd53155718619a005068a88c4c9f71b23d0d279a57efa9140c948a551c5355830e0d3c74a4c6843462968c5218629d8e29053c8f969a029cf54dfad5b9ea9b75a43163eb526df4a8d33d6a5072299226694504669338a603c9c0a4039cd2039eb4e278a008e538a6249b7834a4ef27daa2738a04cd2b77c914cbbdcc72b9aad6929df8ad78a2568b0475a113b3319dc18f07ef53ad0ee6c517d188e42052d82724d0c6f62f940d8c8a02e29d4b8a081290d2d06801b498a7534d03180e68a68e0d3b34cd028a290f009a0045e4934b483a52d001494b494009483efd29a45ea4d201f4b494b4005145140051451400b45251400514514005145140052d252d020a5a4a51400b4a2929450028a7520a5a00514e14d14e14085a70a68a75301452d2528a005a29296800ac7be6cc8df5ad83c027d2b0ae9b2e7eb4022bd252d25050b4869692901ffd7cba2968a004a314b450026290d3a90d0034706949cd145002ad29a4ce28a604b1ce63183c8a79b80455622931480b6a37e4fb6692325a54cfad1112236c7a53ad86675a00d1350e7327d0d3a76db1935141d41f4a10152e8e5dbeb4c83ef8a26393441feb050c0bc3a521507b528e9450219b48e87f3a4dc47de14fa514008ac0d3b34d280f238349965ea323d6801f45343034b9a043d3ef54e2a0420354e0d002d21e6968a0637681d0629c3a521a5ed52500a7520a78a004029cdf76814927dda60509cf3555bad5998f3554d218e4e08f7a9318e45562e41c54fbf8069923b9a502a3df4f56a60494c7036d3b34d93eed0055df86a463934c73f35490aef19a009ac5375c28f7ade384427b0158d6ac2098311c568ddce16d8907ef0e29adc9ea645dc9e64a4fa9ab7689b50550037cd5ad12ed4149ee2931e2968a28244a434a69280129a69c69a68191b0a4069d4d61de99a0ea6b7dda01a1ba81400bda8a28a002928a434800d09d29add29c3a5003852d251400b45145001451450014514500145145001451450014b4945002d2d252d02169452528a007528a414a280169c29a29c280169d4d14a2810ea5a6d28a602d2d252d0032538898fb5614e72d5b574d880fbd61ca72e690d0ca4a28a63169296929005145140051451400521a5a4c500252d18a5c50037a9a75262969809d6971450680264388cfd6a7b3e66271daa"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac533fc1120a71f940d280000201041ff64380054ffffa838c558b40c4bb2f6a404d74ff2814b1f11b1ff0064d5799b738ab07881a9a0667cc7e6a6ab60e6897ef532901712e171cf06a412a9ee2b3e94038cd00686e069c0d668661dcd3c4ce3bd0234734551172c3ad3c5d7a8a00b2c80f23834d0c41c3706a317287da9fe6a38c122801273c0c75cd5c8892a33e9545ba8c9ca83d6aec6c368c1c8a00929475a4cd2ad0021eb4bda90f5a5a92872d3e98bd6a4a00414c9ba549514c78a60519aab355894d573d69032173f354887e4a77959e69bb08e4550870a954544bcd4ca2900ea6487e5a7d325fbb4c0a4fd6ac5a8e2a0239a9e03b7143132ef97919033eb514c92050a738ed9ab96e334d9cef703d2844a2bdbdb608635740c0a545c014a6913b8da28a4a60068a290d0006928a4340c8e8a28a66830f069472d4a4669a0e0d003a8a28a000d21a290d2010f614f14c1f7a9f400a28a28a00fffd0751452d00145145001451450014514500145149400b451450028a5a4a5a005a5148296810b4e14d14e1400b4a2929450028a753453a80169452528a042d1494b4015afdb110158afcb1ad5d45ba0f6ac93d681a128a28a062d251450014514500145252d00029714829c280131494e34c26801e48ed4d348296980669d8a6f7a908c500340c9ab306e8d1b038351275c9e953c7868f83c934011b732ad5a9b882ab1ff005e2acdcffa85a108cb93ef1a6d39fef1a6d21852e78c5251400514514005145140051451400a188ef53dbcc54fdefc2abd3e319a00d017429c2e80aa4169c145005bfb40a5fb48aa7b68c1a560b973ed60535af8f602aa61a908f6a6172cfdb64f6a6b5c3b77155ca9a4c1a02e4a4b1ea69319a6027bd3c75a42b8f191c1a17a91467e6a3a3d3018579f969e8fd8f0690f0c29e5430e6818ecd23636f34cc94ebc8f5a246ca7d68020501a4353950101f7a8e25c31a99fee8140997edf88f77a5463e79b34f07ca840f51496e39268e84ec89fa0a6934f34c3412368a2928181a4a53494001a4a0d1414911d14514ca10f4a6e38cd39ba502801a0f6a7535877a5078a401494b494002f5269d4d5e94ea005a28a2800a28a2800a5a28a0028a28a00292968a004a28a5140051452d0014b494b400b4a29053a81053a9052d0028a5a414a280169c29052d002d2d2528a002968a281199a8b6643ed59a6b6ee6d4cae58739aa8f62c3f84d0333a8ab6d68c3b5466dd876a064145486161da9a508ed400da29769a4a0028a28a005a514da70a000d30d3e987ad0028a7520a298851d69c4d354f3411cd031e093c55b82302353ea6a9ef50befeb57a16c8897d07348081bfe3e7f1ab375fea16a9eeff004843ea6aedcffc7b8fad3ea2325fef1a4a73fde3498a43128a314a14d002514f119a51113da8023a2a5f21bd29de437a50041454ff00676f4a77d99bd2802b54b12e6878f69c1a7c6500e4d003b69f5a30d4b943d1eac8b62541cd202af23b52827d2a7fb3b6690dbb8ed45c2c47452b42e3d698558531587f149814ccb7a5286f5a047fffd1af814c3f7a82d48393412387de3431c114c76da6a17949a0a4587618fa5396407bd51dc7d680483d6803478350c8a548c74a7c472a289bee8a00647d4d4c837caab504679ab36a09b9dfd9450265999b80be952c230a0d5794e5f8ee6ad20c28a193214d30d38d34d0489494b48690c4268a0d2532920a5a28a651151451400d6ea29693f8a96800a69e29d4879a004cd21e868e8690f6f734807af414b451400b451450014b494b4005145140051451400514525002d145140052d252d0014a28a5a0414e14829450028a514829d40053a9052d0028a5a414e14082968a28016969296800a5a28a004201ea01a698a36eaa29f4b401035ac67d4544d62a7a1ab9494019cda79ec2a17b161d8d6bd140184f68c3b542d111dab7a6c63a0aa170bc6476a572923376e29315"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac534fc1120a71f940d280000257e41ff64380054ffff2b360f2053772f714c06d35bad49f2fad31864f1400828a4c114134c072539a913a52b75a402019ad38a308a0fa2e6b350648fad6b1e124f65c51d40cb90e2453ee2afcc736e6b3a6fbd5794efb7cfa8a7d40ce7fbd4bc01449f7a834804ddcd384c47614ca28027171fecd3d6e57d2aa514017c4e87bd3d5c374acda9a172a0d022f668cd56f38d0252680b0c9b973516d35640dc7a54a21c8fba28b8ca410922b5d388c0f6a8162c1e82a7ed52d8d02fdea92a34fbd528a00911411c8cd3648633fc35247d286a60537b54ec715564876f435a2fd2a9cb4ae16293020d01b069d275a88d3158495f26a2273431e692980514514016e07f96899f38150236053987cb9a0055ce722ac5bdca47bf79eb5549c0a8e811ab091238c74abfdab3b4e1919ad1ed4887b8869b4e34c2681053734134532920a2968a6585252d14010d14507a5003472c69691475a75002521a5a4a004614d1cb0f6a71a45fbc6900fa28a5a0028a28a002968a2800a28a2800a28a2800a28a2800a28a2800a5a28a005a5a4a514085a51482945003a96929450028a514829c28016969052d021696929680168a296800a28a5a0028a2968012929d494009452d25004331aa927208ab52f5aacf50f72d6c7fffd2a528c1a86adce9deab118a10022163815712db8e696d6201771eb56b149b1a45292df8e2aa3a115aecbc5569a20684c1a29c63e5a52a49e94e5f95b69a9703eb4c447127ef147a9ad193fd54bf855583e6b8518e956a4ff5727d28ea064cbf7aacdab660dbe9c55697ef5496adb588f5a008a51f35349a9271f31a8c296140094952088fa52f927d28022a2a61037a52fd9dbd28020a9625c8a78b76f4a9e28b6af228022d86a48828fbc39a9367b521514b50b92a95ec2a406a989029e0d4827cf6a4d3196339a71a8637dc46454ec290c44eb528151c62a5029a112a0e291a9c9f769ad4c085fa55394d5a90f154e53d690cace79a89fa53dbad31fa53110d149de9698828a4a2801ea79a958fcb5053873de80109a052b2e281401ada72e2107d6ae9aab6aeab128c8e953f98a7bd246429351934ace2984e7a532921453a900c53a9962514b49400514514011535ba53a984e5b02801c3a514514009494b4868012923e84fa9a56e013420c28a403a969296800a5a4a5a0028a28a0028a28a0028a28a0028a28a0028a28a005a28a2810b4a2929d400a294520a51400ea5148296801696929c2810a052d20a51400b4b4829d40052d252d0014b45140828a29680128a28a062514b49408af275aaf20ab0fd6a17159bdcd5159c641155648f15758544cb9eb4d31324878402a753552362a769e9d8d5856a0a4487a540fcd48cdc52a445b9a6040b6de6726a3993ca1ea2af10c981c552b876248229a1343ac46642ded5349f71be94db24c216c76a7b0ed4126549f7a9f08f985366187a921eb4c04b814b6a3e5391dea6640e39a5540a3028017028a42c0501b340870a7520a5a004a28a2801290f208a5a39c8c75a00a6f1906902bf626ae4b27cea1d306a58846cbd01a00ab6a6433a8278ad161491469bc1039a7c9d6a594848c54805363e9520eb400f1d2a37a97b54325302bca6a94a6adca6a9cb486406a37e94f34c7e94c4458cd25381c521a62128a50334fda280194e419348460d488b819a005db9a4db53242cdd297c86ce31408886e1d09a963121fe23f9d3d6d5cf6ab31c3b7b50024519ee4d5855c50ab814ea630a28a2800a28a280128a28a00aec4b1daa7ea697001c0ed42a85500503a93400b45145002521a5a4a4035ba53c74a61e48a78a005a28a280168a28a0028a28a0028a28a00fffd37d14514005145140051451400b4b494b4085a51494a280145385369c2801453a9a29d400a294520a5a043a9692968016969052d002d1452d0014514b40828a28a0028a28a004a28a0d032b"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac535fc1120a71f940d2800002aec41ff64380054ffffb75a89c7152b75a8deb366840c2a322a62298450042c2955f1c1a6cd906a20c7bd5016f39ab51e59460f3598b363ad4f0dd28382d8cfad160b969e2663cb0154a7428c558e6ad79d193932023daa9ccc198953d69a0668c09b6d548ee2a17eb570e16d947b55273d6864942e97e726883afe14e98831e3b83d692014c09c51452d022ac84934444eff006a99e30c78a1230b40128e94b4828a0028a292800a6b1c01f5a753641f21f620d003d958cc33f3002a4f2e36e9953ed4cb76df2337e1560a86ea2800b746593ef6462a497ad240a55cf3918a593ef54b290b1f4a9075a6274a78eb400fed5049539e955e434015a4aa72d5b92aa4b40c80d31fa53cd31fa5342223c1a4a0f5a05310e534f1eb5152e4d002b1c9a910e500a8aa48fb50068db0c026a58f9726a28f88ea789702824973c51452d31a41494b4503128a28a0028a28a004a28a28022ed4d5e9437434a3a500145149400521a5a4348041cb53a9abd49a75002d145140052d252d00145145001451450014514500145251400b45145002d2d252d0216969052d0038528a4a5a00514e14d14ea00514a29296810ea5a414b400a2945252d002d2d20a5a002969296810514514005145212075a005a4a3228c8a065690e0d445aa796324f150989bd2a1a2d3233453fca6f4a431b01d2958644f80a49e45539767553f855895895231cd56581e47c62ad213110e47229ce8319152bdb98876c5407354211300f229c480dc5341c76a490f4c1a00d380ff00a22f27ef1a639f94d476cffb855f434e94e109a960572334e8d715186cb54aa45003a8a28a620a51494a2801d494b494005251485b1400b514cdf21c53b24d24a0085a8024b13fbafc6ade6a9d902231efcd5acd20258bef1a57eb4d87ef1a7b75a4ca42af4a70eb482945002b1e2abc86a663c5577340c824aa92d5b93a55496802034c90f14f34c93a534222eb45145311ffd4c6a28a280169f1025a98064d5eb583b9a00b51ae40ab00629a8b8a92980514514005251450014514500145251400514514015dfb0f7a7534f327d052d002d25141a004a434a69a6900abd29d483a52d002d1494b4005145140052d251400b494514005145140051451400b45145002d2d252d021453853453850028a51494a2801d4b494b400b4b48296810ea5a4a5a005a090064d1492a83110681886641d48a6fdaa3fef0aa12bede0541b8934ae5729aff698fd4528b843fc42b277d2efa2e1ca6b8990ff0010a70914f7ac7df4a243eb45c5ca6c6e159f7f3491b131b62a11330fe2348ce1fef7345c394ae2fee47753f853d7539875407f1a779719fe1a3c98cf6345c761cbab1fe28cfe06a45d563eeac3f0a83ecf1fa9a4fb3a7ad17158b8ba9407ab63eb522dec2dfc6bf9d67fd997fbd49f655f5145c2c6a068243d14e6a2728a488c63d4d5116a077a97f79b36ee1f5a77416165650bb49aa92100706a63149fdf14860662376da574162a8249e94a519bb55f482151924934a5c2fdc887d4d3b80f8e0d8883afca334490b3ae0034f69cff0009c0c52a4831966e7d296805516327a54896aca30454f2dd8451b4673508ba66fbc38c53d040612074a8991bd3152f9e69448ac706802131ba804f7a68dc3b55a95b7703902a1231400cdc7d0d264fa53c9a69602801304fb51b4521907ad279a3d6801f4c6532fca3b546d2e4e054d03aa83cd004f126c403d29f51f9cbeb479cbeb401621fbd529aad6f2067383564f4a96521452d368340c4735039a91cd42c69011495525ab4f5565a60434d97b53bbd366ed4d12444525296cd25300a503346d27a55ab6b72c41228016da0ce0915a51c6169238c28a96800a5a4a2980514514005252d250014b494500145149400b452514015d796634ea647f769f40052514520129a69c69bde801f45252d002d1494b400b45251400b452514"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac865fc1a77471f940d2800002aec41ff64380054ffff051452d0212968a280128a5a28189494b41a00acfd6a271533f5a8dab37b9a22bb0a61152914d2280216142be383493641e951063dea80b79cd5a8cfca08acc59b153c376aa704e05160b969e4c1fb84fe15425f998e3804d5b37833c60d5199f796c0c5340cd3b74c5a29f51513f5ab7c2daa01e82a9b9eb4325142e57e626887afe14e98831fb834900a604f452d1408ab2139a22277e2a678c31cd09185a00957a52d20a2800a28a4a0029ac7007d69d4c907c84fa11401232b19803f30514f11c6dd3e53ed4d81b7c8cdf85582a0f5140096e8cb272d918a924eb490290fd72314b27dea96521d18e29e3ad353a53c75a0079e95049539e955a4a00af274aa72d5b93a5539681909a63f4a71a6bf4a6844478a4a0f5a05310e534f15152e4d002b1c9a910e500a8aa48fb500685b70335347cb935147f2c75344b814124dda8c514b4c6909452d25030a28a2800a4a5a4a0028a28a0086917a50c7e534a3a500145149400521a5a4348068fbdf4a7d357a9a75002d1451400b45252d001451450014514500145145001494b450014514b40052d252d0214528a4a5a0070a5a4a7500029d494b400b4e14d14e1408514b4829450028a51451400b4b494b40052d145020a28a2800a296909028185251b85191eb4015a43835096ab324658f150989bd2a1a2d32234549e4b7a5218d80270695877217c0524f4aa72eceaa7f0ab12b12a4639aacb03c8f8c55a426221c8e4539906322a57b73171c63d45407354211719e78a790a0f5a6038ed4921e983401a701ff00445193f78d31cf06a3b67fdc2afa1cd3a538526a5815c8cd3a35c5461b2d52a906801f494b494c414a292945003a929692800a4a33484e28016a299be438a7649e94d940111a0096c88f281f7ab79aa76408887d6ad520258bef1a571cd361fbc69ec39a4ca42af4a70a414a2801cc78aad21a9d8f155de8195e4e95525ab7274aa92d0040699274a79a649d0534491514514c0fffd4c6a28a280169f1025a9aa326aedac1d0914016a35c815600a445c53e9805145140052514500145145001494b494005145140103f61ef4b4d3cb8f614ea002928a2800a69a5ed4d348055e94ea41d29680168a4a5a0028a28a00296928a00296928a005a2928a0028a28a005a28a28016945252d021453a9a29c2801453853453850028a51494a2801c2969a29d4085a5a414a280169490064d2524aa0c441a0043320eac29bf6a8ffbd54257dbc0a83712695cbe535c5cc7ea2945c467f88564ef34bbcd170e535c4c87f8853848a7b8ac7df4a243eb45c5ca6c6e159fa84d246c4c6d8150899877348ce1fef7345c394ae2fee54f553f8548ba9cc3aa03f8d2f9711fe1a4f263f7a2e3b0f5d58ff1447f0352aeab1f7561f8557f223f5349f674f5a2e2b17575380f57c7d4548b7b0b74917f3acefb32ff007a93ec8a7b8fca8b858d4cc121e8a6a2728a4ac6307bd5116a074352fef366ddc3eb4ee82c2caca176935524200e0d4c6293fbe0d21859b1bb6d170b154312694a3376abf1c10a8c9249f6a71609f72207eb45c058e02889ea5452c90b32e0034f69cff09c0c53924e32cdcfa52d00a82c64fee9a912d594608ab12dd840368cd402e998fcc38c53d0406123b544c8de952f9e69448ac7078a0084c6ea0123ad34123b1ab72b670a3902a0e9400cddec69371f4a7e69a580a004f98fb51b7d690c83d693cd5f5a007d3194cb951da98d2e78152c0eaa3af34013c49b100f4a92a2f397d68f397d6802c43f7aa5355e090339c1ab352ca40296929334008e6a0735239a858d21913d5496ad3d559698109a6cb4eef4d9874a68922229294b669298052819a369356adadcb1048e2801d6d067922b4638f0052471802a5a005a2928a602d2514500145149400b452514005145140051452500575e598fbd3a9b1fdda7500145149480"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac866fc1a77471f940d280000305a41ff64380054ffff29a694d37bd003e969a296801696928a005a2928a005a29296800a28a2800a28a2800a28a280168a4a5a005a5a4a51408514e14da70a005a70a68a5a007528a414a2801453a9a29d400a2945252d021e8326a1ba7c291561788f3dcd66ddcbd45265229c872c69b9a6934669143b34b9a6669734863b34669b9a5cd003b34669b9a33400fcd19a6d1400fdd46ea66696801fba8dd4ccd19a007eea37533345007fffd58f751bcd328a82c7ee346fa651400fdc68dc69945003f7d1ba994940126ea4dc2994668112893031d6977e7af350e68cd3b858900f9b96c8a6b228f7a6e697751715866c07a2fe74e5880eb4bba97753b8ac2ed5f4a9a145dbd0541b854f148a063340ac4bb17fba29362ff7453b70228a602c6a15f818a9f3500eb530352ca42d231a334d6340c639a898d398d464d2018f55a5ab0d504bd29815fbd24dda834921cd34490d281938a306ad59c1be50cc381cd302c436ca000464d5b48c2f414a07b5385002d1494530168a4a280168a4a2800a28a4a005a2928a005a4a28a0028a4a280215e169690514005252d252010d2203b72686e869ebd05002e28a29680128a5a280128a5a314009452d14009452e28a004a28a3140051462971400514628a005a5069294500381a5069b4b4087e69734c14b400fa766a314ecd003c52d301a5cd00494a2a3cd38373400c9ee0a82abdb8aca9998b1cd5b98f3d7deab30cd228828a795a4c5201b4b9a314940c5cd2e6928a005cd2e69b4b400b9a334945201d9a334da2801d9a334945003b34669b45031d9a334dcd19a007668a6e68cd003a8a6e68cd003a8a6e68cd002d1499a33400b452668cd0216928cd26680168cd251400668dd4945301eb230e86a55b923ad56a334017d2e14f7c54eb2023ad64e69cb2b2f43408d5dd4d66aa4b747bd4a260dde90c918d46c682d504b2851ef400e770a39355259b3c0a6492163c9a8c9aa485714b1a4cd25491465db029887431976ad38630a29b042105581c50028e28a4a2800cd1452530168a29280168a4cd1400b45251400b4525140051494500145145004545341a5a40148683494008dd3f1a7822a36e94809a00981a5a8b79f4a5dfed4012d151ef14bbc7ad003e8a6eef7a5cd002d1499a5cd0014519a2800a5a4a280168c514b401ffd6929692945002814a0520a70a00028a5db40a514009b47ad2eda7514009b68da69c29c05003314629f4b8a00651dbf0a931ed4bb68119efc935130ab13c450d57348a1845348a79a422900cc52629f8a314011e28c53f146280198a314ec518a006d14ec518a006d14ec52628189452e28c5002514b8a314009452e28c5002514628c50014518a31400668a3149400b49451400668cd149400b45251400b452519a005a4a3349400b49451400525145020a03114945003fcd603ad42ef9343b76151f5a761094b8a5a55058e0530044dcc00ad1b78428e9cd36da0da327a9ab606280147028a29280168a4a280168a4a280168a4a2800a28a2800a28a4a005a2928a005a4a28a601452514010723ad2834a79a6918a403a92933466801ad4b41ea296800c518a5a2801314629d45003714629d4500379f5a5cb7ad2d14009b8d2ee3e94628c5001be9778a4c518a0076e1eb4e0d51e28c5004b9a5cd458f7a39f5a009f34a0d400b7ad38335004e0d2d401cfa53849ed401353aa1120a70907ad004b4b51871ea29c1a801e296981a9c185003852d34114ecd000ca186186455692cf3ca1cfb55aa28032de1653c822a32a456c9e473cd46d6f1b76c7d29580c9c5262b49ac94f46fcea26b17ed834ac32962931565ad641fc26a330b0ed4011514f287d29369f4a006d14ec1a4c500368a7628c500368a7628c500368c53b1498a004a29714500262929d8a2801b462968a402629314ea2980dc518a5a2801b8a314ea280198a314ea31400c"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac867fc1a77471f940d28000035c841ff64380054ffffc514ec518a006514e229a450025145140052514500148c7029dd064d44c7269a013ad2134a69bd4d310019357ada0c727ad476d164e4d5f5181400e030296928a005a2928a005a2928a005a4a28a00ffd7928a28a0028a4a280168a4a2800a28a2800a28a4a0028a28a00652514530108a6d3a90d20107269d48b4b40052d145001452d14009452d140094b45140094b4514005145140052d252d0014b451400a294520a5a005029714829d4083028da296968013651b29c296801a148e84d280e3bd38528a004cb8a5dec3b53a8a004f30f714a25f634b8146d1400a251ef4a241eb4dda3d28d8280b92071ea2977545b051b3de80b9386a320f502a1da477a5f9bd680b8f2919eaa29a6088ff0d265a9771f4a0069b58cfad30d9a7ad4bbfda9777d68b2020364bfdefd29a6c47f78559dc29777bd1640533627d4527d89bdaaee68cd2b0ccf368d9c527d91fd2b408cd54926951986ddc3b114582e55788a1c1eb521b4902825719a459165951181c961d7bd6c49f768b01866261da90a11d6b57cb20e5866aadca92c70b8a2c1729628c53ca9a6332af5a561dc4c518a4570c714e2450037146297228e280131462971462801b453b1498a006d14ea4a004a4c53a92801a453715252114011e28029d8a6b9da314c0639e78a67414bd79a6934c421a7c6b96a6a2e4d58550066802c45c0a9d4d57435329a009334520a5a005a2928a005a4a28a0028a28a0028a29280168a4a280168a4a2800a28a2800a28a2800a2928a004c0a302968acf9a5fd7fc319f34bfaff861368f4a368f4a5a2a94a56febfc8a4e4d7f5fe426d028c0a5a2b493d0b93d04c0a302968acb9a5fd7fc319f34bfaff86131462968aa4e5fd7fc3149cbfaff00861314628a2aaeff00aff8629f35bfaff20a28a2b149b6d366314ddd5c28a28a739382dff209c9c56ff90514515ccdd4deff00d7dc73b753bff5f70514514ed5bb3fc7fc876add9ff5f20a334515b376dffafc4d5bb7f5ff00045c9a326928aa4db5a7f5f89a4758ff005fe62ee3eb46f6f5a4a2b797325aab7f5e86b3e64b543b7b7ad1e637ad368ac2effaff008731bb7fd7fc11de63fafe94be63fafe94ca29ebfd7fc38d2febfa63fcd7f5fd28f35ffbdfa5328aae5bff005ff04ae5bafebfcc7f9cff00defd297ce93fbdfa5474567157934cca2aeda24f3a4fef7e9479f27f7bf4a8e8a2768aff0087ff00803a968ad3f524f3e4fef7e9479f27f7bf4151d15cae52eff8ff00c1396f2beff8ff00c124fb44bfdefd052fda25fef7e82a2a293725d7f1ff008227292ebf8ffc125fb44bfdff00d051f6897fbdfa0a8a9db7fcfe75d7797f5ff0c75de5fd7fc30ffb44bfdffd051f6897fbdfa0a8c8c52552e66bfaff002295dafebfc897ed12ff007bf4147da25fef7e82a2a2b492d0b92d093cf97fbdfa0a3cf93fbdfa0a8e8acb5febfe18cf5febfe1893cf93fbdfa0a3cf97fbdfa0a8e8aab3febfe18ab3febfe1893ed12ff7bf414df31c74634da2ad26559d850c4481c7de5e86a537739182ff00a0a868ac926db4651527744a2e251d1ff4a6b48edd5a994529b715bfe4151b8f5fc8ffd0461bbad37c98ff00bbfad3e8ae27ed3bbfc7fc8e2fde777f8ff90cf253fbbfad2ec5c6314f3494ad57cff1ff0020b55f3fc7fc867949fdda3ca4f4fd69f45744938eff00d7e26eef1dff00afc46f96be9fad2ec5f4a5a29c755a7f5f894b55fd7f98dd8be946c5f4a7515bca2d2d51b4d38ad46ec5f4a362fa53a8ac37febfe0986aff00aff8233cb5f4a3cb5f4a7d1556effd7e255bbff5f88cf2d7d28f2d3d29f45572e9a17caeda7f5f88cf2d3d29a608cf55fd4d4b495924b9acd992576d37f891fd9e2fee7ea693ecd0ff0073f5352d144ec969f9b14f45a7e6c8c5bc43a2fea69c2241fc3fad3e8ae6bcbbfe3ff04e6bcba3fc7fe08d08a3b5280052"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("809ac868fc1a77471f940d2800003b3641ff64380054ffffd143e75d7f1ff820dcd75fc7fe0852e4d2515d5797f5ff000c755e57febfc85c9a326928aa5ccd7f5fe452bb5fd7f90b93eb464fad2515a49591a49590b93464d251597bdfd7fc319fbdfd7fc30b93464d2515567fd7fc31493febfe183269726928abb32acc33464d1456714f99a328a7768334668a28a8dc57f5ff00042a371d0334668a2b97f78deff9ff0091ce9546f46ff1ff0020a28a286aaaeff8ff00909aaabbfe3fe47fffd9"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("801ac86bfc1a9a6f1f940d2800000a5841ff64380054ffff6da1461813d28b08b4ac718a060f7349807a628c11ff00eba06291ef4678c1c528623b521c31f4a00001d475a0b668db8ef4d2327eefeb40877be4d3493ea68e47634e56e307340c4de7fbc6959d8a9f9b3498cf7fd298fc29e94086175ee29239023938e2a33d79a6f04d2196dfe65c86dc87f4a92d96616c42711e7afad536de130a78ad783e5b0407ae28016042b1a834490198b63839e0d489d0534dc08776ec6d1ce3de810914c4fc9270c3b53ae9b117d6ab303703cc5386eb50dc4f2708eb8228191b1e69b9a4c9341cd48c5dc08ed51952a729d3d29de4a1f51f8d1e4fa39a6022cc3386e0fbd4990c2a2685cf520d4662753c1a045a53c53aaaaaccbef4f0d20ea9401218d0f514d3021e848a61998755349f68f5068017ecc4f4614863913b528b95efc54ab2ab0e0d00571230ea4d3c4ac6a711798391c512411a8eb834010c7232c871dea7599fa9a885bb81bc50f232af2a3f0a0639ae983e31c53c5c311d2a8f9df37233561668caf19cd021f3112af23150470e0824f4a7c928069ab3a938a00ba922e319a9030f5aa60ae3ad3d013c83482e58635131a4cb0eb4d2d9340ee28a5ef4c0c29770cf5a0057fb86a9bf4ab5230d9559fa5302a375a14734add6954714009453b14628018452629fb68db400cc52629fb682b400ca2948a28036fa0a451c50dd29474a60145145007fffd27d211c52d14c06834372c0507834839626801d45145002521a5a4a004eac29f4c5fbd4fa005a28a290828a28a0028ef45140051451400b49451400b451450014b494b400b4b494a28014528a0528a6028a5140a5a0428a70a414b4005252d14015fec91efddcf5e954678f0e401deb5b14d28a7aa8fca9818c54af229a49ad29a25fee8aae615f4a432a64d018e473564c0b49f671eb400e048eff00a53b771838a6796dd988a0ac9d8d00480a9f4a4c7f9cd46378ea297711d47e9400f24d2027da9bbc13d29e0a9ea29801c13cd2b000719a89e455ec29149619e9ed400fc9ed9a473f2f5a702718a6ca3e4ea6802b1340a69eb4e8f1b867d6900e0c7a56b6f022451598d16e970bd33c569042aa80f38a00b2bd0567dfb8191deb4a319c5655fa1699b8ef40916ed7fd52d4777866c1a9adc6231f4aad7072e690d154a32fdd34d6738c115353645dcbc5171912cc3a1a9438229820f534792c3eeb5021e589e94abeb50912ad377c83ad005acfb5193e955c484f56c5480e7f8e8024cfa8a69546ea0527cdd88349e663ef2914001b746e82a45b018eb834b1321e43007d0d4fe611e87e940c81ada4418590e29ab1bc6d9752def567ce43c134a1c038ce452018b223a95071f5aabf656772338abad1c6fd56a1780a7cd1b9e3b1a008d6c3d4d4b1daac673d6916775c798a71ea2a65955ba1a00a37ca15b8155a0199003566fce5eabdb7fad143034c2281f7452e00e828ed41a0631cd30ed3db14ad4da0406238c8e45308c0a941228183d698ac572a0af04d23c7f2f153b4781f2d2467b30a02e5031f34edb815a5f6747e69cd640afcb40ae8cadb4a12adc96ae9fc39a89637dff74d0322d949b0d682c031c8a77d996815cccdb46dad1fb28a67d9b3405ccf2949b2b40da1a61b66f4a0772c9ea29693ab52d318514514005145250007a53569c7a1a6f422801d45251400521a290f4a0054a75357ee8a7520168a28a041451450014514500145145001451450014b4945002d28a4a5a005a51494a2801c294520a514c070a70a68a75021696928a005a5a4a280168a28a008a5155249110e1881579c706b16e8e5cd0c0b6b22374229dc565834f5761d188a06696051b6a80b89077cfd6a45bb3dd7f2a00b7b6936d42b7687ae454ab3230c8614006cf6a638c01ef520950f46155e79332a81eb400ff28139c7346ca957a52e28020294329231536294264e00a00a7e41ce41a3eccfd40e"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("809a9e88000011987b36807900004320406328170040ffff820d8a3ccc1c019dcb96ee413ebd703bfad5691a0418538c11d08665e99ce57d877e39fa555ad6d76136da6d2febf101736ca41dff0036006f94727d14af0063d7ff00ad5623beb7c4819c1f97008e40e800caf53c75edd3159ca2d75b8a2dcb4b0b0df441766e6dd82148239ea31cf538c7b0e951bea308c64a9dac3233e8718c678e073cf7a694748db998da96977cafd3fcccd9756505d14c7ebd181ebc607a7a7d2b3a6d6b6bb2971d0f0064b3fb024fe78a4d72cb6d0d231bdee66beb2cb920e0e31c1e4741c818cf7c73f8f534c7d59fcb3b5cf461b49c67d41e7927ebed49f2f466a95b43065d6260c32e37e71d7b73c7bf24e3fad68da6a4cf18049666c0e58707d720703fc69276d98e2af64ff0372da525480c7732e49238033d3e65e4ff002f5ac1d65778505d88de0745e39ea71d0e738038f7a454e2935a1bde19848923c70370c90076e0608f7ebd7eb5f4a78563e21ce78c7ae7b7523a03fd3d39a7e47a4972d38ddf43e80d2202d18dc060000647240ee373706acdcc05b8cfaae48fd4fcbedeff00d6b68ae55639a4f55e473f7368d9f949e7b01839c75ce7278fa7bd575b139e578c0e8791ea4f3d7ad5c62e438b5aa4b43f")
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame restartFrame = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    try { restartFrame.Add(interpreted); }
                    catch { break; } //jpegPackets has more then one frame
                }

                try
                {
                    //Draw the frame
                    using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                catch //Frame is not complete
                {
                    restartFrame.PrepareBuffer(false, true);
                    using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            //Ganz Camera, Dynamic Quality using Rtp Extensions (OnVif)?
            jpegPackets = new byte[][]
                {
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f32fcd5280a9c2f18b100f000610088471c0263413241b11040000000012020000130200000000000001ff503c00000080120c0d100d0b12100e10141312151b2c1d1b18181b362729202c403944433f393e3d47506657474b614d3d3e59795a61696d72737245557d867c6f856670726e1314141b171b341d1d346e493e496e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6e6ea718f9453f18148bf7694f4fa53b88a56dcde486af639aa565ccb21f7abbd47d28181a61fad3cd30d0030d30934f6eb4c3d29009da8a3a9a3ad2186690d2d21eb4000e4f352a702a3ef8a9568403cf4a78e2987a0a93b7154890152c1feb0fd2a2a960eac7da9a06364e951d49276a8cd2601486968348614b483b52d001de83d68c5068013b52fe94514803349476f7a5c503128a5c525001cf141e68141a0028ef4bc5250019a0f34514800529c9e94946680014a690504738a00338a5fa52629698099a5a3b51d6810d3484d2e7349d6900d27ad21a5a4c5318d34ded4ea6d200cd0c68fad07d281122e4014b48bf769698099a75252d001cd1f8d078a319a004ce68cf1462938a00327a514628340076a0d19cd250006824d1da8a00caba1b2ecfe75a4877203ed54f511f321c0efcd58b56cc0b430253d68e940a28013b521a514878a003348734506800cf39a4a38cd21340003cd01be63f4a6fd6971cd005c51c5239da84fb53bb0a82f1fcb80e3a9aa11169e3e573ea6ae0f7a86cd364207ad4dce2818869869e699da900d34c2314f34c3d4521880518a28ed4001a438c0a5cd14860bc9a997b542bd6a65a684c77f10a92a3fe315253100a9a11856fa5435345feacd50991c951e29f21f9a9b52084a29681de818514bd29280168a28a0028c734829690c4a08a5a280128a5a4348031487ad29a4a005a28a2800a28a33f9d00028c51450014b494bd280003345028a004eb41a5f6a4a004a6fd29c4e4d2371400d3d29a69d4d340084530d3e99400b8cf14374a286e3b50048bd296917a529a62129c3a52528f5a000d1d3a51450037ad29ce3a51da909c8a003345148680147e941e949466800393487a504fbd07a0a4054d457f74a7d0d2d89cc583da9f7a375bb541a7b70c3f1a605ca5e9403477fc2801290f4a5ebd6928013148734bce290f1400def43714bc76a6ff004a4018cd14678a338a605e1d2a95c1f36e563ec3ad5de3154ad479974ec7b5508b8060014a29452668010d34f4a53d69a7a5218d34c3d69e698dd690c6f23ad29a43cd2ff5a401463a5252f4a062af5a997afad429d6a61d69a10a3eff00e15275a62fdfa7d52242a68ffd57e35154a9c45f8d00c8a4fbd4da593ef534f4a430a5e2938a5c66800a2947bd25002e3bd1de8c639a290c28a297b5020ed49de834a7f9d2189498a5cd07ad0025069690d001da834b4940051d29451400828ef4bda8a0028a0507ad0213241e2968a5ec681898c506971c527b5003690d2f7a43c0a0069a69a776a43400d3cd369dde9a45002514b4878a00957b518a17a52d31094fc714da5cd002f1de909ef4b8c9e293eb4009d6908c734519a0028e9477a4340076a0d1d8d14006293bd145004738cc2c3daa8d89225c568b0cae3d6b2edcedb8fc68034e83476a38c5000381486969a6900b9a439a2909cd30129a694f39a422800ef477a424f5a33cd022eb9c4649f4aada78cab37a9ab127fa96fa543603107e35405a1494bda90d0034f3d29a69c69a690c69e4530d3cf4a6639a4313ad14519a00290d2f7a438fca90c72f5a996a143cd4c29a131c9f78d3e991f535255084a9c7faa150d4e7fd5afd28115d8fcc69314add734dfe74862d0292968017b51499a280171c51c1e28e94014861474a5c514084e71452d148625068c51de800a434a2834009452d14c028c514b4804a28ef4d278a005a5a419a5a00051d29bbb9eb4a39ef"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f42fcd5280a9c2f18b100f000610088471c0263413241b1104000000001202000013020000000004d801ff503c400b41e9451ef400d3487da9c7ad2500308a6e29f4d3d6801a69b4b49de8401d69187345045004abd296917a52d31076a5ed494be9400869739a293bd001c01498a3ad14001a4a5ef45002514514009db8a0d0683da8013bd658f96ecf6c1ad4acb978bb6fad080d3cd1da917a529e4520131cd2371cd2d21a6027279a4a764537f1a0043c51d28ef49dfbd0020e451de8cd2679a00b73ff00a86fa532cbfd40a74dff001eedf4a2d07ee00aa027ef487ad141a0061a69a79eb4c348069a6f7a71a613ce69318519c0a3b5140c3bd0718a3349de90122fb548b51254cbd29a10e8f9cfd69fd45323185a7fd2a841539fbabf4a83bd4eddbe9409958f24d21a3bd07b5218bda8a28071400bfd2931cd1476a402f414bf4a4a2818bde8a3ad140076a4a52692900507a51477a002834507ad0007da8a28a005c62928cd1dc5001430a07ad19a00419a71a3141a0089976b53d78e68c669c062980521a5a434806e69093474e941a0069e94d3d69c69a6801a69bf4a7114d3c520171f8d21a51d290f269812af4a5fe5483a52d3100a5a4a7014009cd21a5a43d68013b5078a5a43d280101f6a314a290f14001a4f5a01cd2f6a004e941eb451da8013159571c5db56ae6b2ae7fe3e9bde84069a1ca8f4a09a44fb83d314bda900629ad4eed4d2298071e94d3ef4ea426801bde90f14eef4d34009477a33477e6811666ff8f63f4a5b3ff502925ff8f73eb8a5b4ff00523156326ef48697bf3487da90086987914f34c34806b1e2994f6e94dfd290c4cd29a6834eeb40c4c521eb4bd68e94807a54a2a34a90700d3421f1fdda75353ee8a5ef5421472454d2544bf7854b277a04cafd690d1452187ad1e9452f1400b4868a5a401db8a5a4cf1476a062f4a33462822801314bda8a4a402f6a4a503ad25000283452f5a004ed4519e68ea2800ef4514bd29808297bd145201294d1f5a3da800a3a71451de80128c52f4a4e8280129a694f5a43400d348694f4a69a6021eb4d3efd29c7d69bd45200a46ebc528e941eb401281c5068038a29880734bda938a5a00290d2f6a0d0037141a2834000e690e3145079e28013b526334ea43ea28b009c8147514b45002565dd7fc7e1ad4acabbff008fb342034633f22fd29d9e29917fab5fa53cd00264d21fe54a38a43f9d00069bde97de92800e69bdf14b49400639a43d68349de802d4bc5a9fa5167ff1eeb44bff001ec7e9459ffc7b8ab113e690d28a4a431b4d3ef4e34d3d2900d34c2371c7e74fc126ad585a099e5dc78504d2b0ca6106318a19761c1e9567ecb215c8144b11300623953834b52b42b5275a3046051de81122549fc26a34a90f4a62245fba296917a52d508727df14f97a1a647f7c53a5fba6811051476a290c296928eb400ee82931c51d697ad2001d697bd14b40c4a5a4a29005252d250014514500028a5ed49d0d00140a39a5a00290d2d21a005a0d20e94b4005252f6a09c500028a28a003f0a43d2979c5079a006f5a434bc8a69eb400d3d690d293c714de6801bd6933cd3b14def40071487ad291cf14bb4934751130148453954e29761a760198a5c73522a7ad05453b01181cd2ec269f8c734bba8b0116ca5f289a526977d0219e59a4f28fd69e5e903e28019e5b0e314dda476a9bcca76f047228195b141e9560843da9ad0719539a2c041597798fb61ad56523a8acabd1fe9749017e23fbb5c7a53f34c8bfd52d38f5340076f4a43ef45235160133edc507da8c7141cf4ef400dc64fb514bc826928010fad27b7ad1cfe5477e6802dcc3fd18fd28b3ff00502927ff008f63f4a5b41fb85ab1137ad21a75348a431ad4d3da9cd4da4027522ae5bb15b1639c027693f99aaed1845439e4d4bbf6e9bb3d65fe940cb96d30f25770e9c5364546de07208e6aa5bbe237ef8148eec24c03d40a2e16"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f52fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000000a3401ff503cd42fa158cc6e9d08c1fad55ef575f2f0caa7965e6a91a43244e5411d3a53cf4a86162495fc6a63d07d69a06ac4a3a50451453207c5f7c512f43509b9589c77a919c3c7b81c8340ecf723a0f340a2900741476a28068016969051400b4b49f4a290c5ed41a4a2801690d1452016928eb41e940076a3b51db9a3bd002d149474a602d275a5a2800a29296900940e39a28ed4c05a071494b40080d0682291ba520109a42297b5211400d3c521a78527b54821f5aab0ae57db9a72c24d595880eb5204028b05caeb08f4a77962a7c0c5318d315c6e296985f14d693b5003cd30b542f70aa3e6602aa49a8283f2f34017cbf3485ab29af9c93818a164b897ee83ed480d32e3d699e60f5aa8b6f330f9dcd48b6a07de24fe34013eecd04d208d54714103d2818b9a37526df7a42083d6801e1f02956520d444e0734668116bcd571f3555bab0599fcc89b07d28ce3a52890af7a00448d914061822822ac24ab20daff9d324848e54e4500406908a776a6d2188290fde34a3ad21c5001eb4878e4d0091d6933c66801293bd2d277a00b571ff001ec69d6dc40bf4a65c9c5b1a92dffd4afd2ac44d4d3c52d110124bb4d218c3d6987a53dc812327f7698690033e08cf6a797dd0b01d9f34c7e08f5c52c0a647f2c701bad4f5366bddb976083099ecc2992424aa90395e2a4867d90faec3834e174ad80463bd5e8657634a849558f475c1ace65c311e87157e77ddcaf45e6a9cdf7dbeb52c695c21da1f0393834f3dbeb50c0a564563eb539fbc3eb4d0e6494d76da858f614ea82f0ed878ee6990b5653662e726a7b59b9d87a5566620f1e9423609e39a83a5ad2c6952500e5430ee28354733d05a05251400b9a5cd266933400fed452678a334805ce6834dcd28340c5a0d2678a09a4000d2d3734678a00774341eb4d06973400b452678a09a00534519a33c5001452678a5ef4c028a3bd1400be941a4ed475a041475a334f48cb76a2c047d4f1522404f5a992100f4a9828c5558572358b6f6a52b4f240a89e5029880e0530b8150c971e955de527bf14ae345a79c0a81a7cd5492755ef5526b966e871482c68cb72918e4e4d529af998e1702a1486594f03af7356a2b1518327cc698ca404931e326ac45a7b1e5db03d2afa44a838029f4ae0411da469d064fbd4bb428a75079a40373da834a2838c5002527b52d0680128a0f1499a001b918aa5148dbb69e466ae39c293e8335462e645fad3405ce8290f4f7a7d55b9b830c8a08c8228113838e952c5395a855b207bd19a00b6c8928cae0355674284823148b215ab28eb3aed7ebd8d0054349534b118cfb7ad4278345804a4ef4efad3690094639eb46391484f6c5302c5d1ff471f5a9a1ff0052bf4a82f3fd528f7ab118fdd0cfa5500e271cd52372d1cfb96adca76c6c7dab29ce5aa59a41131b92d725bb1356c9c8c8acb3d7ad685bb6f887ad24392b1338f954d4fa6ffaf3f4a9ad6c8dcc5bba0048a64313417e1083c516d4774e3618aacb76f17690f1526a917937415781b455858835e42f8e8dcd4faadbf9a89277538abb18df533ee623118e3e7e7514cbb8bc9bb2a7d0569ea10ee785ff00bac0557d6630258df1d78a4d1507aa2a041c7d69245c4d8f7a9547cc052dd262653ea28b04991d56be240503b8357046c4138e0543736b2481480781434386e66b2e0f140c924f6ab9f629b01b6fb62a36b4963c657afa54d8df9912dabef8b1e87152914cb2b76d92100f18a79a6613dc4a3b5028a090a4ed4b49400a0d14838eb41a005ce28cd277a2900b9a3349499e6818b4b4d06973400b9e6969b9a2801d9e2933477a4a007519a4cd19e680169734da5a005cd149da8a042e714e505b814e8e12dc9ab51c3b69d82e451418ab023c0a700169ad2003ad5122f02a29250a3ad452dc7606aabb9279a571"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f62fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000000f9001ff503cd89a4b9c938aaece58f34d63c54324c074eb48761cf205eb5564b82471c53198b1e724d3a384b1cb74a2c043b5a43f2d5882d54365b9a991028e29ebf7a8b812a2851c0a5ef4a3f3a4fe2a00297bd252d2003d2928ef41fa50014d3cd38d2500277a3b7345068013eb41a3b525004574fb613ea6a0b55cc83d05174dbe40bd854d6ea1573eb4c096a86a40fca7daafd52d4865568027b73ba25fa0a7b29ed51da1cdba1f51521a18911e79f4a50fb4f06875dc3dea2fba706819a114e1d76c9c8a8a784a723907a1a850d5b824c8d8e32a681150e69bf5ab13c1b1fe5e41e951796d9a008c9e4d1839a97cae7934ef2973cd0032ecfeea3fad5b4fb83e95467398a2fad5e5fba3e954323b9ff52de9598e7e6ad79233246ca3a915912290d8a86690d868eb572c49c30cd450dac929e38c0ce4d68dbd9792e9939dc29a41268dbd247fa103fed1a7dcc6a64593b818aaf0bf931ba8e81a95a6de9c76aa31ea2c68564dc7a0ab7232bc7b49eb50337eed87aa822ab824c479efc5005b988688e7b54171b6731a9edcd1231d8e3b6ea8ff008d0ffb2280444500989f4356046b26d661c8a8e45c4c7df9ab6ab800503b808c6318a360a940a0ae0d32480a0da46298d1820715608eb4c238a064291804803ad519d3129ad251f3d50bbe273498106de690af14ea0f4a450cdb46da7d19a4047b6976d3a8a0066dc526de2a4a28022db46da928a008b6f14b8a900e28c52022c1c5152e2931cd003292a4c51b6818c141a76da31d2801bda969714a8bbdb028b08400b70055a8adf3c9a921b7c01c55a550a2ad21363122c0e94a7814ace00aa735c7614089259828f7aa724a58d3198b1a69ef5371d83351bb00093448e1064d562ece496e946e3164959f81c0a8705b85a94217fa54a1001d28d808d220b4f1ec297ad36900e14a9d69b4e8fef5004c0d1de80692980efe5494668ce29005141a4a003a8a4a5a28012834b4633400da648db509a931c75aa97b260041d4f5a008a252ec58d5c5185c7b5410151851daacefe98a6c42556be89a48d76824d5bdc69377148657b48996050c306a62b8e0d1bb8a4cfaf6a6211573d694c4ac3e619a33f9d21340c7aaaaf4029c5aa3dd467ad201fbb34d269b9a4cd003f3d6932334d3499e680217398223ef5a2a7e507dab2cff00c7b0f66ad253fba5c7a55813c4428626ab9823f337e39ce68c9c9a14920548d684f1800f4ea2a76e911a853ef8a98ffaa4f626988998f320f614d43fb9cfa9a18fef987aad37fe58afe34c45cedff00a8547eebfe042a73dff00dca8d07eec7fbd4c4249f75ffdea523ee7d2893f8f1fdea53f713e9400b20cb03ed561470a6a271955fa54c9f705022403a50c3914a3b507ad3111b0e6998e0d4ac39a8cf434808f18acdbb7fdfb0ad36e958b76c7ed2fec693290a1851baa0dd46fa9289f75216a8777146fa009b34bbaa1df46fa00973466a2dd406a404d9cd2547ba8dd4c093a52d47ba80d4012504d337505a801e7ad14cdd46ea007668ce2933cd38216edd684022aef703b1abd6d6655b3da9d696786c9e9575884181556b12d8c2028a8a49428e4d47717013bd509262e7ad0d8244b34c58f1d2a0cd2669335250b9a8e695635a6cb308c7bfa556c963b9ff01400125db73f4f4a9638b71cb74f4a7451f3b987e15366988020ed48539a783403eb52323f2f9a6f9753eea4c8a0080a71cd313ef1ab07150918734c07e696981a9775201e3a51dea3dff9505e8024cf3484e0d45ba9375004dba9a5ea204e3e941340126fe682dc5479e28268015a4daa49ed59d2ca5999bae7a54f772e06c1f8d55443237b0a680b16bc0e7a9ab638c55745c0a9c1e05003f349475a290094a7a514114084a4c714fc71494c06d18a7803a77a31400cc77a29f8a4c50033bf1d28a791498a"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f72fcd5280a9c2f18b100f000610088471c0263413241b1104000000001202000013020000000014ec01ff503c00a9270b2af4c1cd5f80e6dd3e954ae576cce3fbc2ad5936eb603d2a8649fc54e03834d3d69cbd6a064e3aa9a94ffab1ec6a15fb80fa1a98f311fad301f27fad5f714a46234a47ea87d0539ffd58f63544968f5faad317ee2ffbd4fcf2bfeed347fab5ff007a98818732503fd5ad3c8fde38f514d51fbb1f5a0093198c7b54c83e5151a7298a953eed021fe941eb40a0d31086a3c75a90d318d2018c38ac3bbff8f97fad6db1e2b0eeff00e3e64fad265221a5c0a4ef4b5250b8146d1452e69006c149b0528a3340c4d83d68f2fd0d38500d0030a1a361a9290d0047b1a970d5275a28022e71d28c9a968efd28022c9a5049ed521c71c53e34cb0cd3421f05b97157ededb8f98702a5b684100f6ab123055c0abd896c899b60c0aa5737614601e68bab8ea16a830dc739a96c6908f296392699bb34bb3de9361a430dd51cb3841ef4d9e4118f53daab0259b71e5bd2801c0966dcdc93d054f1a7396eb491c24727ad49834c07038a5cd339a4e6900fddc5293c54793da93340126ea3351eeeb48188a00909a8dcf24d217c0c9a6b1ca13eb480703d4d2fbd301a5cf4a602fe349f8d20a09e680149a4fc683d28fa0a00286c9a071ef47bf6a005e9d69aedb57753aaaddc993b0741d6802bbb176c9ea6adc316c8ea0b48f79dc4703a55f03834c430264669e06052814e02801a314b8a70146281098a314a05380a006eda36d380a31400ddbef46da78146280198e68c53f1f8d18a02e47b68db526da4c7340152f86190d2e9edfbb61e869f7cb9873e955b4d63bd87e34c6681e49a51da9285e6a0a274e8454a9ca11ea2a046c1a9a3e334c09243848cfe14f07391ed9a8e4e621ec694b60237af14c45c4fe1a00f931ef9a45ea3e94e27057d0d51239cf7a451ce287fb8dec69a1b94a044cbc11528e2a1eb262a61d45021f475a41476a60213d6a36a79e99a61ea2900d23afb561ddf17327d6b70f43589779372e7de9329105283da9075a5a9285145252e78a062834b4dcd19a403b3466901e68cd002f4a5a4a2900a7ad1e94669334c05a334669546f6e2801d1a16e715760b6cb004516d16718ed5a20045fc2b45a10d88488d31546e6e7b034b7571818159eec58f349b0480b6e269b9a3345414266a1b8b85887fb4692e6e444b8ea6b3cb3cd27a934580765a471dc9357a08760c9fbdeb496f00880f5f5a9f39a7700a43d2973c525200a4e28ef8a43d680108a4c0e694f4a426801b8a68e4d3bb5250035d72a69107cb8f6a737dda48f914c0403b52e3245498a0019a00605a36d4e14628da28020028c54db45054500438a315295a6b61464f41408af3c8234f7aa38677c0ea6a49e42ee4fa54f6b0617737534c09a2408800a928c52d0014be9466826810b4e14cce29d9e2801452d337814bbf34087d18a6eee39a50f4c070146290352e6800a3146452e41a004c518e69739ef4645004138dd130f6acdb03b6e48f5ad4238ac98c98ef07d71422cd73483ad07a5038a963448b5346791ef50af5a954f0690132f2854d211fb819fe134039fc453cf31b8f6cd52132743911d29fb8bec4d353eec7f5a730fdd9f6635449238f95feb518e36d49d777d01a67f08fad004e3ef03eb530eb5029071ed537a5021e3bd1da8a4278a621a7a531b834e269add690c6b1001cf4ac4bbc1b993078cd4dabdd943e521c1ea7159f0cbbc60f5a96cb5176b926326a3925d8ded52b1db1b37a74aa9f7b3bb238a96cd211beacb6082011de8a8216dbf4ab140a51b31314b474a3341214514160064902800a5a8fcd5cf06a369b748113ad0058a5c1f4a58d76f5e4d4864da281116092055b820f980a6db45b882456924415326ad21362c3188d78a8aea63b70295e5c702a16c1e49a64949f731279a8f69ad00ab48635c7415362ae67e2abdcdc888601f9ab5b"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f82fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000001a4801ff503cc953daabc9a7c4ed965a561dcc01be7938e49ad1b6b6112f3c9abb1d8451fdd18a79b71da9b15cac05153f914cf25b34ac323a4353792d8a69818516021a0d486239a6946f4a006521ef4e2a7d290a9a006d3477a5c734639a0046fbb446286fbb4a9d2801e071498e694518e6810f1d296901a5a06251467149ce7340076aa57937f02fe353dc4be5c7ef59c0348e314201f6d1798f93d05688500714c862089802a4a180948452e6834804341a5a4c702980869338ef4bda9bd4d2033fed0fe6e33c66afa9e335944fefcffbd5a8bf745310fddc505a9b9e293afd6801fbf14be60a8b1476a0097cca5f378eb50678e7ad274a770b16449cd1e6555e4526e39a0562e8e959173c5de7deb5c74ac9bb199643fdda651aa0e541a777cd4709dd129f6a767352c689569eb518e0d3c548c955c020679eb528efee0d5176c4c1b3d0d5a8d832835498e51b2b9690fca3d8834f3d1feb9a854fca07ad481bbf6354664ca7047b80299db14e2709c76a6b1c329f5a62057db7417fbc2ada9e6b3a63b6ea33e8455f539268064a0d213c520a6b1a090269ad413f3531c9da6819cddf3f9973231e9b88a8118ab0229d3f3237b9a888c77acd9d2b62f4ac1adc608c9e4d5466cfe54a325314840247bd26545590a4e0f1d2a579ca22e392475a8338623ae78a5656d99c7039fa508535a0199cf534cfb4fcf8cf4a85d8e0e0d458c71dcd59897bed2c075a8f7b124b126a0573902a4079fc29087b4981ef5674d8cb3339fa0aa04e4f35b1a7a6db71efcd30263c0a6c4a647a594e4ed15620023519a1032cc402019ed45d5d045a826b80abb6a8cee5e99362d2cdb864d065aa88e4714f0d9c7a526c762c799479a7d6a0cd19e690ec59129f5a3cdaad9a5dd45c2c581253bcc15577114bba8b8ac5a0e28dcb8aadbcd01e8b858b3914706abefa5df401315149b0547e652efa2e03bcb1e94d30834bbe977d00466dc7a530db8a9f7d01a98155adb8a05b6055bcd19140153c9349e49cd5ce28c0a00a822346c356f029368a00a9b4fa535f8524f6abbb01a8de20df4a41730e7dd23e483562da0d8371ea6aff00d9949e94ff002b03a5302b007d2822acf9549e4d202b1181462ac18693c83e94ec057ed8a43567eccde949f666f4a2c172b9a4e9560db30a8cc441e94582e61bff00af3f5ad54e80567bc0c25ce3ab56884206286806fe941f5a5c7ad21cd21899e2929690f4a04277f7a434ece4d373f9d0014dcf34b49de802ee702b3997ccf3feb5a1dab3d1b224c0cee6a6c695d92a4856d9941e6a4b7971804f5aa8e70302941c1e0f4e6a2f73a39558d515229a8227df186f5eb52af6a0c88653894e7a67153dab7c8467a5417036cdcf43cd166ff395f5a16e6b257897d5bdfa1cd480f047a557535329f9bea2ad1ce59077061ed9a473945f6a6a36194fa8c52b0fdd91ef4c447707e75357a23900fa8aa371ca29ab56a7f76a6840f62c8a6b1a334d634c90279a637434134d6340ce6671b6571e86a3f4f7ab37ea56e58f66391554f5159b3a16c3e2387a4e5b3db15259ff00af07d2b4becb0c83711d7ae28b07358c903756bd85b84b722519de3a1f4a72430c67e5514f6971d29a562253bab239cba411ddc883a2b1c547b860fad6b5e5b2c88e5461cf39ac424e714c8177fcd9ed5246c5b24d426a6847c9f5ef400e1d6b66d8ed85571d2b3ed21dc77115a9127a70680248e20c7703cd49336c4e3a8a58c64e48c11515de48e281106e2ed48fd69ca36ad35e81911e29cad48dcd20eb49813039a5cd301a766900eef4948296800cd145148600d19e68a2988334669283400edd8a5dd4cef451701fba977d479e28cd0049ba977d459a334012efa5f32a1cf34b9a00983fbd287a83751ba811387a52f9aafba97753027df4bbaa0dd46ee28026dd4e1c"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7f92fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000001fa401ff503cd401b356ede3ddc9a680548891d2a416e2ac2a80294d326e402014be528f4a90e6a37cf34c426c514985a6366a32c680b9290a69a635351ef2281252b05c8decd18e70291ad01a98494e12501728bda1ed50bdb30ed5ac194d042b5055cc468c8a61522b6cc08c7a544f62a7a51641732314cad47b03daa07b371da8b05ca47f2a43c54cf114ed5091cd2b0cb3b8853f4aa10e4231ed9cd5f3c21fa567c23109f7a52d8b86e318f6a075a57e49634de84541d068d93e508f4ab5d2a8587de6f7157c74a0c65b915d3664e7d054709c4ab83d2af5f5b6cb4865033bbafb552b68f75d20ff006ba53b6a689a712e8386a901e466a39789580a507a55181633951ed52b77f7a814f4a949fbbef5448928dd063d39a9ed4fee85404e32bed525b90a319a10742d6ec8a693c530360534b7a5310e2698e460d34b530b673487629dc44b2ae18553365fed1ad36a8c8a4526d15638443f53eb53c6fc114ac370a6edc0a03724dc29a5a929a7a5210d63915813f133fd6b7dba5615de05c30f7a60404f27157ed622e147b5548a23249815bd656e0002801f0c38033c558dbfc2df81a9bcb5098c5438f9b07f0a0076ec0c1aaf2c9f371d2a495b02aae72e290131fbb51375a90f4a8da801869b4e3d292801c0d3a980d385201d9a5cd369680141a5cd36973400b9a33cd20c51de800a28a0d0000d1477a2801334b452500028cd140a0028e94521e9400b9a4cd19a4f6a005079a5cd368ce6801d9a375368a009e105dc56a45845e6a959a7cbb88a7492106a912cd0dc28e0d672ce45482e08ef5449708f4a6914c865df52d02232b4d31d4b8e28c645005731d30c59ab45690ad0053f2a9a508ab7b68d94014f0c28dc455bf2e90c540102c9522c9ef4862151b215e9401604bebd29db90f5aa81f079a5dfc714013cb68922f1d6b2aeac9a36e9c568473156eb5683473ae1875a02e739271137b0a4b2b5f3acc01d492734b264c4e31d455bd214a5be1b822a5ea6ab4d4c5906d62bdc53464fe15a77964d24c5a11c93c8a62e95203fbc38a9b1b732b0cd3979663d318aba29c96c234c7031e953c310055b191b8669d886cd28e34bad3d6361c118a823d3e2b56dc32cd8ea6ae2c838551807d2a0966e703d6a8cd37b14a680b4bba982220d5a9dc2b102a3948dabee2818c44395c52bb718f4a9071220f4150375348439096193f4a7c47e723daa343841f5a7a1c49f514013eecd266901a334c00f4a8fae69c4f14dcf3400d6a61a7b5333da90c6f4a43d294d373c5002678a69f6346686071d2900c26b16e949ba603b9ad9cd55100370d27ad3012ced8281ea6b6604dab55ada31c1ab99c0a04c52d513377a576e2a266a008a56c9e0f4a8d064e68901ce452a74a431e4f151b75cd3cf351b75a004269b4b484f34000eb4ea6e7f2a5ce2900f1d28ce69a0d2d003a9453734a79a00751499e946680173466933c504d218514668a620a3bd14500148296901a0009a28cd1d6800a434b499e78a002834668a00434b8ce0514f886e900a00d08576c155a59003cd5c71b61ac5ba94f9c066ad10cb81875a19c76aab249b62eb4b03975a606ad97dde6ae555b11f20ab78a091314629682280108a6d38d277a60263346da5a0b05524f41400d380324e28c861c5645e5fbc92111fdd15269f70e5b0f401a4cbcd44eb564f22a1930a324d2029c89cd40cc54f3d2adb0ddc8e955662075a0040f4f8e521b8aad9c1a783cd00468abb803561240060015581f9f34e53cd41b16525264cd279a5a5c9351467e7a43c350048c71262a5889dae3d39a8a5ea0f622a58b058fb8a622da3e4a9a819b3263de9603945a62e3cfe7d6988499b2e69cf92ca3db151c830e6a4c03283400ece66fa540c706a543894d4520c31a4c051fead4fbd3c1f9d69a8df2ed3f5149dc1a0"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7fa2fcd5280a9c2f18b100f000610088471c0263413241b11040000000012020000130200000000250001ff503c0973d69775309e6933400fcd373cd2669b9eb400e26999e68278a4a00463c543249b6a427159977390e714866842416ab6aa08ae7a1ba6561d6b5acef03f0dc5301d731ece45431265aac5cb06e052c11e39a009506d14ac69090298c681031e6a363cd2b1e2984d218d6e681c0a4ce4d2d0006a36eb4fed4c6a00434d3c9a534de9400b4b9a4a3a500385283cd341a75201734b9fa520e6973400b9e28149da8ef400bde8345140077a5cd277a2800cd1452d0027a514668a00293b52d21e94007141eb45140076a3bd03ad1f5a004ab16ab992abd5cb25ef4d032cdcb858f158d2a069735a17d26056696cb55a207c8a5d40152dbc5b131ed490e33cd38be1b1401b1683110ab354ad64f9055a0f4123a90f4a4de2973c50021e29a4f1431c5465a980f2d8aa57f39da235ead52c92e05663c9be7cfa5000b1014bbfcb2315263e5e2a0947349b19722ba39192719a9b50b9575c271c565062050ee48a00d08e70d1246012d569b4b263df230ddfddf4aa5a5b2c4c6793f84617eb449a93b39cb7e14010ca851b1e94d1d6a4b870fb58544281118ed4ecf3518e829e391599b120eb4e639c5460d3c1c8a603faa63b8a9223820d459f9b1da9c0e180a045888edc8f434de935229f9cfb8a56fbc4fb5310c63920d3e33f3fd0d45fc20fbd3a33fbc340c7f46634c90ee00d39cf04d46ad9520d021acdb5d2a43d6abcc79cd4aad94cf7a43240dfa52134d06826988703c526693345030ce69a4d148680118e6b1af47ef7eb5aec7159f7614b93dfa500548d7a7ad58472b822a20306824d3e822dc77477fcdcd6a452064c8ac0cd5cb3b9d87693c5219a64d358d26fcf4a6934800d349e0d06984e4d002ad147d28ce68014d31a949a6b50037e9484f4a33499fca80168eb499a5fad003852e69a297348070a5a6e69d400a28a4fa528a00296928a005a2901a280168a4cd1de8016928cd140051da8a2800a2928a005cd251da8a0005685a8c47541796ad15f92214d09946fdb935411fe61f5ab1a83d508d8e7f1aa1246a85dab9a8d7e639a619ff77ef535a465947bd081e868598263ab8aa7078a4b5884718a533a06db4c819821aa407e5a760114d6e298159dc9627d2abb4a7762a794819aaad83d28011e4c826a9a9c35597e10d55ef5251651c1148eb9aade66ca7a4e1bbf4a00714a89c63ad4e18114c900a0059d8c76e8074c64d510e4b77e6ae5d1dd066a8c432f8a0762fc24b46334f14d8bee9029e3ad08965703029c298a72829e3a03506a3d7ad386734ce94ecf3400f61f3538f6348dd01f6a09fddfe34c09437cca7f0a40df7877a6a9ca83ef49d26fad3108adf3633c1a01db20a4031252375a0099ce548a8b3839a7e777e22a26ef4004c3826961395a241951f4a6c3c01480941e28cf34d1d6973e94c075266933c52668014d3598019348ee14649acdbabadc4a8e94016a59862a8bb6f6cd34c848c6693a534805a69f6a33484f3400bda941c62999a3773401a96b3ee5c1ea2ac66b263936b7bd68433075eb52048c78e2905213934a68017a8a4e9499a322900b4d34b4d34c069a4a538e29b9fad002d1477cd1db9a0070a5a6e696900e06969a29d9a00506969a0d1400bda969b9e29680014b499146680168a28a0028cd2519e2800a334514005068cd19a0028eb494b400f846e9055cb96d883e955ed17327d2a5bc3918154896635d3976e6a2403ad4930cc948a38143348224c60026b5ac1338acd6da31deb4ec2452c2888aa17eee6f261e3a9ac47bc2b2e7356b549b9233d056193bd8b55a32b5ceb2d2512440835248702a8690dbad96ad5cb6d898d21193a85d10db41e0547693efe339aa776e5e4c7a9a5b2ca4fb7da86334df9435571cf15708f96aa81f311486452465c714c4818020719abe899c548221e94c195110"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7fb2fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000002a5c01ff503c80053fcb25c67bd5b110c51e5e3068114b50c2205154621939a9efdcbc873c54109fce932917a1fb98f7a93bd351768c77a90508965284e531e95203505b7522a7e950cd47d29a68a777a06499cad03ee9a683c51d2810e53f2d0edcab0ed499c7e3484fcbf4a6039befe691fad213cd19c8a043c1e0535a8079a43c8a00339e29178cd318e0e476a731e33eb400f534679a62d2e6801d9fce9ace14649a46600552b89f24e0f02801b75704f4354d7e6e6876ded4e1d29a00ed4a0f634d34678aa01c69a4d19cd21a402fad34fad2e4734849c8a402e7ae6a5b79b61355f3927d28cf3401b10bef8c354955eccfee179a9b34980b466928a005a43d28a434009e94def4e34dcd0002969052d0028a28a3bd003bb52d369d4805a290519a005078a29294d001de9693bd1d68014519a28f5a0028a28a004cd2d252f6a004a297ad25002e28eb40e945005bb21c1349382cc6a5b6188e9ae36a927ad5a24c99970c6a25ea2a698658d44bc5266b01f31e38ab7a3b1694f3daa94dd2ae6867f7c6888aa8fd514966ac719c151dcd74d7f686404ad53b5d2c998338e0568b631bd8bda64263b6507d29f7dfea1be956d136a003b543729ba23f4a423949789b27b1ab1a7a996e0b629d7168cd2600ad0b1b5f263c7734319284e2abcd6ed9ca8ad009c54f14409e6905ca56d6cf20195c7bd587b5603a55f50a053b834c9328c4c3391d2980035a9345ba3603bd508adcab90d8a0663dec24b9e28b1b32cc64906117f535b2d026ecb0045417120dbb5400a3b0a4cab94cf2c7de940a6af5a78eb48465dbf1201eb562aa21dae09f5ab878a966a3c714b4c078a75218ea5269bda96810a4d2668a43400a3afd6807e6fad213c8348fd698879383499f9a90f5a1bae6801b25337e500f435238cf4a880fbc2802543c52e6989d29b3c8235f7a0065d4db57683d6b3647ddc67ad3a6972726a34e79a60394601a5ee28a43c629807d2933c519a3b1a0050690f4a4cd04f18a401da93345275228003d39eb499e7f0a18e49a6e680342c65c8db5749ac5572a411c55e82e838c370693405ca29a0e45148076691a81453010d27a504d2671d680168f43494b400b475ef4519a4028a703c7d29b4b400b4bda9a297ad002d148296800a5feb4945002d149d2968016933c51450004d1451400507a7d28a2800ed4a393494e4fbc334c0bf11db1d57b89b9c548ad85e2abbc7b9b9aa449525e6a1039ebd2afcf06d4aa078353235804dd2ad68a71727f0aad28e2a5d29b17429c02a1d530047ad35700e3029c39514813e7cd59ce3fb54522e454d4c619a00a6d082d9c0a9163f6a936e4d3d56900c11fcbd29c8a41a9957029c178a622024d49167934fdb4a053101aa732b17e3bd5ca6381914865461b63c5675c1e4d695c1c29aca9cf5a18d1128a7f6a6ae69fdea46630e2ada366353ed54c1e2acc3f73e952cd5138c5381a8d4d385201e3ad28a6669680168ed41a426800c714372051fd68fe1a6007a0341e80d1da81c8c5020eab4c042924f4e94e07b66a29ff00d59fad003c1db9cf6aa37536e3f4a9279fe5183dab3e4727bd0026edcd93528181c5561d7ad4f1b6720d301c690f34a69a7f9d300ef47afa51ed4ded9a401e99a434bd29bd0d003bad29e05341a5639a0061346291a81c52014f6a7038a6e7a500d005b82e4af07240aba8eae320d64038cd4b1cc50f07b5006af6a0f5a8219d5c60f06a626800eb494bc52134000a5fa5203cd2e7d2900a28a28ebd2800a763bd34734ecf1400a3b51d293d28a005a338a4f7a5fc680168ed499a3d68017b50281cd140051de8ef466800fa51da8a3a8a0005068145002d3d065aa3eb53c0b934c091db6479aaab724be39ab3763094cb38914866aa4484e1da3ced20566b0c3735bd79347e56d5ac19181931da"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("901ab7fc2fcd5280a9c2f18b100f000610088471c0263413241b110400000000120200001302000000002fb801ff503c93d8d2039f91cf6a4b46d97008a77f0d470102e133d334a25545a1d089662b9504d4f66f23b1de08a7417912c401f4a9a19565e52ad339ac4b8a4229f8a08a6046169e0629714b4083145145001476a292800a6bd3a9add2802a5c7435953f35a97278359537271431a1abd05480734c1c54910ccaa3d4d48cc550054b110322a302950e1aa4d4b03a714e1d2980e4d385218ecd28e3ad20efeb4a280149e293d2969ac714085cd2d37a71d6824e2980ff004a63361a8ce4034d7e4d021ce70062a3b9902c7f5a767e4aceb9909931e940c8e47cb1a858d2e7bd30d3100a72d34528a009836451ef51a9229fd714c038a438a5ee69bd6800a4a5cd273f85200a338a4e4519e4668010d038a4279a3e9400ea290d06818b4be949476e2908955b156a2b9e406fcea9027a52e71c8f4a606a6e07914a6a8c5315c5594943e290130eb4b4ca750035df68a1240fcf7a4948dbcf7aac0e0e05005ecfa52f1fa55459c83835655f777a007d0290514805a3b519a280147ad1494b400bda8a4a28016928e945002d19f5a4cd1400b451de8fad301455eb48f201aa2a32456bda47f28a684cab7e31c540461462acdf8fde01ef5048a42e6a88b904a7119c9e6b3d88df56269724d542df36454b35a65a0014a80f12035623e63a824e1a9477349ec69db8ca726b674f5c47c5615abeeda82ba2b35db101567332c62940a3145324314628a0f140099a3bd149400b4868a2801090393d2aa9bb477da3a543aadcec411a9c13d6b38cdb1171da9d80d1b9395359729f9c7b55d77dd103ea2a8c9cb54b290e153db01e616fee826ab8ab09f2c1237af02901860f149921863a52e69aaf9c83c549a93834f56c1cd408e31d6a4122f62290c9875a01e6981c1e94a38e940893bd31cf1406a463914c0507e5fc297d69a9d29c0f4fd680018dbf8d0fc8a0704d21fbb4008a7e53597727131ad2ce05665d9fdf1f4a0445900f5a693d29690f514c0334a2908a075a007678a556f7a6f6a3bd00499c9a4ed40347af34c0290d2d2669009d290f51466819cd031bde9690f04d28f6a042f5a3ad251400e14639a4ed473d4500381a5ef4da5a0076718a7ab9045440f4a504fe068034a17dcb9cd49556d5b8ab39a404571e94c850ee27ad4b2aee5e2a28490d834009327cfc51978ea497ef8a25381ed4012c126f1528aa76cd96a9ddca914012d07ad203914b9a402d1494b400b9e28f5f4a4a2980b452514805a28a3de801681494b4c0921199056ddb2614563d9ae64add85709f8552224675e0cce2a1bcf960fc2a694e6e3155b53902c44536246149212c7eb480e71513b65e9e0f4a991b40d0831b79a867e1a9f6c79a4b81cd4a3496c58d3799c57556e3f762b97d28fef2ba9887c82b439592d068a4a648519a3bd21a000f2293bd2d21a002909a5a64c71131f6a00e6f51b8f32e98e78cd576932b8cd47704994fd69a32ecaa3b9c5531a46c29ff00475cfa5563cb1ab520da807a0aaa3a93eb50c6870f4a9663b2d547af26a303269d7c71b50765a00e68cec6986463de803af146d63d071506a2076c9e68ded9a76c63da93cb61d8d30144ce3bd4b1de3a81939a80823a8a4c71401a31de293cd4e2547c608ac7e69448ca7de8b01b2a71c53bbd664376c0fcdc8ab893abf43cd2027ef9a09edeb4dcf34d63903da8007e16a85ca73bbb55fcee5cd559d72868028d349cd2934d23a530245e98a4c629a09fc69e3914084ce28a4c51da801c3ad381e29945003ff1a4345373d6800eb4bda914538f4a06348e69bcd3cfd2931ed484252f5a43c5140c5a327d6928cd021c282693b51400ef4a3b0a43da8fe94016ad5b0706ae76acc472a7357229c1e0f5a00b38a4d8339a14d2e680219012c30292561c0353e3350dc47b871d6900cb5fbf8a9a51922ab46ad18c9a94"),
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("909ab7fd2fcd5280a9c2f18b100f000610088471c0263413241b11040000000012020000130200000000351401ff503c4a1881de981653a53a98bd29dda900bde9693de92801dda834945002d28a4a280168ed4941e94c05a3ad14bf85005db05f9b3ef5b23e588fd2b2f4f5e95a929db01ab466ccb0374c6a86ae70302afc3cc85ab2f557cb9cf4a6f704631fbff8d4a09a84fdea9474a966b1dcb96c707eb4eb91c0a8edce1871535c0ca66a3a9b3d897493fbf02bac8cfca2b90d31b1702bad81b282b53925b92d145250485068a4a0028cd1486800a8e71fb97fa5494d6e548f5a00e42e54894e6ac6956e659f7b0f953f5ad39f4b5965cf407d2ac25ba5bc7b50600a6d8ca5727ad5541562e4f26a05e3b54b1a25857322d4576dba735620e3737a0aa323e5c9a06b729246a3b52951fc34f5c9a1b8a92c84c6b9c8e2954034fc64f4a6ec19ce483487710c62a27895870b8a9892319e94ec0207a50329bda9032a735032153f30ad329d2a378c39e45023371c539242a6accb00cfca2a06420f229a02d4377c80e6ac860c0f3c56563078a96298a107b51602f46f86c1ef55ee5f0480691a5cfcc38c556918b3669580666931e94a28c631e94ec205e7eb4a0f5e7a528a6e6801719e86929450d4005140a2818a0d2019a5033daa554a04371c1a0d3fd78a6919a06467a9a91a4dc98207142a82df31c0f5a6b8018e0e45210d381f434dcf4a9dbcb11fdd20d42680101ebeb4668e33476a602f6a3bd20a3bd0028e07b52e69b9a3e9400b9a706c739a68a33d2802dc370475ab692060306b281a96398a914ac069e68fad431ccafdf9a985201ac99155c42c24cd5b18c629719a68045c81cd3bd693a52e4629000a5a4cd2ff003a00297de9b9a5a002814034b4001a3ad1474a603a94724536a48865c0a00d4d3d381576e8e22a86c570a29f7edb500f5ad1193292701ab17526cc86b680fdd9c77ace9ec9e46cd1d4a46115c9cd4814e38ad55d2cf7eb49369e22424f349a4527a94a13f30ab520cc7f8556036bd5beb18e2b3b58e85b105a36c9d7eb5d6d9b66315c82fcb283ef5d469afba115a2d8e69ee68668a4068eb4ccc5a4a29334001a29290f26801734945358fcb4001c66a298f19a6216693da89fee9fa530332e0fcd4c5a598e5c014807152ca252765ab1fef7159edc9abd787646a9ed544f5a4ca8908e94873f5a0723ad069142fd29067348073d78a71a4021e56981580f978f6a939a3d6980cdcd8f987e54a083d29c4534a1dd9a060541a63c21b8f6a79ce78a6e587279140156480afb8a8b69ad1055aa0961eeb401546734d20e6a5dbcd215a04461734a1714fc734e2b4c0876e2931cd4a471481334011eda5238a942d2f9740100524d48b112338a9d62a700a38a432244c52907352f07da9a57268018c0906998ab5c6d395a88212df2d0021da17e65e6a045667f947356a5ddb76b0e0d471c793f7b140865c3480056007e14c8e30e39600d3e653bb05b3ef4aa220bf3039a405765dac4673f4a4a76dcb1c734346cb8c8c5301bdb34869c178a4c5001499e29d8e948474a00051463d28a00075a507149de8a007ab953c1ab90dcf406a80e297763d680361181c1cd38566c3395201abb14c1e9013519a414bda800a5a4c73474a005a5a68231d697f4a0051d68a281c75a0414bda9296818a2a7b75dcf500156ec972e29a1336ad57082a0d44fdd156e2185151cf107393568cd9453000cd3f8f4a90228ed4f08319a0772bfe155ee91997a5681000ce2a09f9434ac099ce4ab8739ab11f29f5a6dd262434eb7e57152ce98bd0ad2aed7addd224ca62b1a75c1cd5fd224c1c74aa8ec6350e841a5cd354e452d3321693ad149400a69334526680149a69ef4b902a33201de80170074aaf7270a6a5570dd3b556ba38534c0cc91c097ad491f2e076cd665d4999ce0d24170eaf8cf5a4ca2fddcbbe627b5573d697a8a4ef50cb3ffd9"),
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame restartFrame = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    try { restartFrame.Add(interpreted); }
                    catch { break; } //jpegPackets has more then one frame
                }

                try
                {
                    //Draw the frame
                    using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                catch //Frame is not complete
                {
                    restartFrame.PrepareBuffer(false, true);
                    using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

        }

        public void TestEncodingAndDecoding()
        {
            string TestDirectory = "./Media/JpegTest";

            if (false == System.IO.Directory.Exists(TestDirectory)) throw new System.InvalidOperationException("TestDirectory does not exist!");

            //Currently 444 is the only type not really passing even when it's own tables are included.
            //Also Progressive imagages, some due to the wrong tag and others due to the spectral.

            //The standard also doesn't really work for images with only 1 huffman table so to get around that you have to specify quality 128 to ensure that the tables are included. (only 64 bytes)
            //This will also depend on the receiver who should check this length to determine the number of components.

            //Used in tests below
            Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame f = null;

            //For each file in the JpegTest directory
            foreach (string fileName in System.IO.Directory.GetFiles(TestDirectory))
            {
                using (var jpegStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
                {

                    if (f != null) if (!f.IsDisposed) f.Dispose();

                    f = null;

                    //should set start sequence number and ensure that contents are equal even when sequence wraps.

                    //Create a JpegFrame from the stream knowing the quality the image was encoded at (No Encoding performed, only Packetization With Quant Tables)
                    f = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame(jpegStream, 100);

                    //Save the JpegFrame as a Image (Decoding performed)
                    using (System.Drawing.Image jpeg = f)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Try again with the RFC Quantizer
                    f.PrepareBuffer(false, false, true);

                    using (System.Drawing.Image jpeg = f)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)
                    System.IO.File.Delete("result.jpg");
                }

                //Try with 128
                using (var jpegStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
                {
                    //Create a JpegFrame from the stream knowing the quality the image was encoded at (No Encoding performed, only Packetization with Quant Tables)
                    f = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame(jpegStream, 128);

                    //Save the JpegFrame as a Image (Decoding performed)
                    using (System.Drawing.Image jpeg = f)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)
                    System.IO.File.Delete("result.jpg");
                }

                //Create a JpegFrame from existing RtpPackets
                using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame x = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
                {
                    foreach (Media.Rtp.RtpPacket p in f) x.Add(p);

                    //Save JpegFrame as Image
                    using (System.Drawing.Image jpeg = x)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                    System.IO.File.Delete("result.jpg");
                }

                //Try with 99
                using (var jpegStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
                {
                    //Create a JpegFrame from the stream knowing the quality the image was encoded at (No Encoding performed, only Packetization Without Quant Tables)
                    f = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame(jpegStream, 99);

                    //Save the JpegFrame as a Image (Decoding performed)
                    using (System.Drawing.Image jpeg = f)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)
                    System.IO.File.Delete("result.jpg");
                }

                //Create a JpegFrame from existing RtpPackets
                using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame x = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
                {
                    foreach (Media.Rtp.RtpPacket p in f) x.Add(p);

                    //Save JpegFrame as Image
                    using (System.Drawing.Image jpeg = x)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                    System.IO.File.Delete("result.jpg");
                }

                using (var jpegStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
                {
                    //Create a JpegFrame from the stream knowing the quality the image was encoded at (No Encoding performed, only Packetization Without Quant Tables)
                    f = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame(jpegStream, 50);

                    //Save the JpegFrame as a Image (Decoding performed)
                    using (System.Drawing.Image jpeg = f)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)
                    System.IO.File.Delete("result.jpg");
                }

                //Create a JpegFrame from existing RtpPackets
                using (Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame x = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame())
                {
                    foreach (Media.Rtp.RtpPacket p in f) x.Add(p);

                    //Save JpegFrame as Image
                    using (System.Drawing.Image jpeg = x)
                    {
                        jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                    System.IO.File.Delete("result.jpg");
                }
            }
        }
    }
}