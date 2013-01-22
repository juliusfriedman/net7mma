using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// Implements RFC2435
    /// Encodes from a System.Drawing.Image to a RFC2435 Jpeg.
    /// Decodes a RFC2435 Jpeg to a System.Drawing.Image.
    ///  <see cref="http://tools.ietf.org/rfc/rfc2435.txt">RFC 2435</see>
    ///  <see cref="http://www.w3.org/Graphics/JPEG/itu-t81.pdf">Jpeg ITU Spec</see>
    ///  <see cref="http://en.wikipedia.org/wiki/JPEG">Wikipedia Jpeg Info</see>    
    /// </summary>
    public class JpegFrame : RtpFrame
    {
        #region Statics

        public const int MaxWidth = 2048;

        public const int MaxHeight = 4096;

        public const byte RtpJpegPayloadType = 26;

        const byte Prefix = 0xff;

        const byte TextComment = 0xfe;

        const byte StartOfFrame = 0xc0;

        const byte HuffmanTable = 0xc4;

        const byte StartOfInformation = 0xd8;

        const byte AppFirst = 0xe0;

        const byte AppLast = 0xee;

        const byte EndOfInformation = 0xd9;

        const byte QuantizationTable = 0xdb;

        const byte DataRestartInterval = 0xdd;

        const byte StartOfScan = 0xda;

        const byte EntroypEncodedScan = 0;

        /// <summary>
        /// Creates RST header for JPEG/RTP packet.
        /// </summary>
        /// <param name="dri">dri Restart interval - number of MCUs between restart markers</param>
        /// <param name="f">optional first bit (defaults to 1)</param>
        /// <param name="l">optional last bit (defaults to 1)</param>
        /// <param name="count">optional number of restart markers (defaults to 0x3FFF)</param>
        /// <returns>Rst Marker</returns>
        static byte[] CreateRtpDataRestartIntervalMarker(int dri, int f = 1, int l = 1, int count = 0x3FFF)
        {
            //     0                   1                   2                   3
            //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //|       Restart Interval        |F|L|       Restart Count       |
            //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            byte[] data = new byte[4];
            data[0] = (byte)((dri >> 8) & 0xFF);
            data[1] = (byte)dri;
            data[2] = (byte)((f & 1) << 7);
            data[2] |= (byte)((l & 1) << 6);
            data[2] |= (byte)((count >> 8 & 0xFF) & 0x3F);
            data[3] = (byte)count;
            return data;
        }

        /// <summary>
        /// Utility function to create RtpJpegHeader either for initial packet or template for further packets
        /// </summary>
        /// <param name="typeSpecific"></param>
        /// <param name="fragmentOffset"></param>
        /// <param name="type"></param>
        /// <param name="quality"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="dri"></param>
        /// <param name="qTables"></param>
        /// <returns></returns>
        static byte[] CreateRtpJpegHeader(uint typeSpecific, uint fragmentOffset, uint type, uint quality, uint width, uint height, byte[] dri, List<byte> qTables)
        {
            List<byte> RtpJpegHeader = new List<byte>();
            RtpJpegHeader.Add((byte)typeSpecific);
            
            //Three bytes
            RtpJpegHeader.Add(0);
            RtpJpegHeader.AddRange(BitConverter.GetBytes((short)Utility.ReverseUnsignedShort((ushort)fragmentOffset)));

            RtpJpegHeader.Add((byte)type);
            RtpJpegHeader.Add((byte)quality);
            RtpJpegHeader.Add((byte)(width / 8));
            RtpJpegHeader.Add((byte)(height / 8));

            //If this is the first packet and we have provided QTables then prepare them
            if (fragmentOffset == 0 && qTables != null)
            {
                //First packet gets the RtpDataRestartIntervalMarker
                if (dri != null)
                {
                    //RtpJpegHeader.AddRange(CreateRtpDataRestartIntervalMarker(dri));
                    RtpJpegHeader.AddRange(dri);
                }

                //If the tables have been provided use them
                if (qTables.Count > 0)
                {
                    RtpJpegHeader.Add(0); //Must Be Zero
                    RtpJpegHeader.Add(0x00);//Precision (Only 8 Bit Supported)
                    RtpJpegHeader.AddRange(BitConverter.GetBytes((short)Utility.ReverseUnsignedShort((ushort)qTables.Count)));
                    RtpJpegHeader.AddRange(qTables);
                }
                else //Otherwise create new tables from the quality
                {
                    RtpJpegHeader.Add(0); //Must Be Zero
                    RtpJpegHeader.Add(0x00);//Precision (ISO/IEC 10918-1 -> Shall be 0 for 8-Bit Samples)
                    RtpJpegHeader.Add(0x00);//Length
                    RtpJpegHeader.Add(0x80);//Length
                    RtpJpegHeader.AddRange(CreateQuantizationTables(quality));
                }
            }

            return RtpJpegHeader.ToArray();
        }

        static byte[] CreateJFIFHeader(uint type, uint width, uint height, ArraySegment<byte> tables, uint dri)
        {
            List<byte> result = new List<byte>();
            result.Add(Prefix);
            result.Add(StartOfInformation);

            result.Add(Prefix);
            result.Add(AppFirst);//AppFirst
            result.Add(0x00);
            result.Add(0x10);//length
            result.Add((byte)'J'); //Always equals "JFXX" (with zero following) (0x4A46585800)
            result.Add((byte)'F');
            result.Add((byte)'I');
            result.Add((byte)'F');
            result.Add(0x00);

            result.Add(0x01);//Version Major
            result.Add(0x02);//Version Minor

            result.Add(0x00);//Units

            result.Add(0x00);//Horizontal
            result.Add(0x01);

            result.Add(0x00);//Vertical
            result.Add(0x01);

            result.Add(0x00);//No thumb
            result.Add(0x00);//Thumb Data

            if (dri > 0)
            {
                result.AddRange(CreateDataRestartIntervalMarker(dri));
            }

            //Quantization Tables
            result.AddRange(CreateQuantizationTablesMarker(tables));

            //Huffman Tables
            result.AddRange(CreateHuffmanTableMarker(lum_dc_codelens, lum_dc_symbols, 0, 0));
            result.AddRange(CreateHuffmanTableMarker(lum_ac_codelens, lum_ac_symbols, 0, 1));
            result.AddRange(CreateHuffmanTableMarker(chm_dc_codelens, chm_dc_symbols, 1, 0));
            result.AddRange(CreateHuffmanTableMarker(chm_ac_codelens, chm_ac_symbols, 1, 1));

            //Start Of Frame
            result.Add(Prefix);
            result.Add(StartOfFrame);//SOF
            result.Add(0x00); //Length
            result.Add(0x11); // 17
            result.Add(0x08); //Bits Per Component
            result.Add((byte)(height >> 8)); //Height
            result.Add((byte)height);
            result.Add((byte)(width >> 8)); //Width
            result.Add((byte)width);
            
            result.Add(0x03);//Number of components
            result.Add(0x01);//Component Number
            result.Add((byte)(type > 0 ? 0x22 : 0x21)); //Horizontal or Vertical Sample
            result.Add(0x00);//Matrix Number
            result.Add(0x02);//Component Number
            result.Add(0x11);//Horizontal or Vertical Sample
            result.Add((byte)(tables.Count > 64 ? 1 : 0));//Matrix Number

            result.Add(0x03);//Component Number
            result.Add(0x11);//Horizontal or Vertical Sample
            result.Add((byte)(tables.Count > 64 ? 1 : 0));//Matrix Number            
            
            //Start Of Scan
            result.Add(Prefix);
            result.Add(StartOfScan);//Marker SOS
            result.Add(0x00);
            result.Add(0x0c);
            result.Add(0x03);
            result.Add(0x01);
            result.Add(0x00);
            result.Add(0x02);
            result.Add(0x11);
            result.Add(0x03);
            result.Add(0x11);
            result.Add(0x00);
            result.Add(0x3f);
            result.Add(0x00);

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

        /// <summary>
        /// Creates a Luma and Chroma Table using the default quantizer
        /// </summary>
        /// <param name="Q">The quality</param>
        /// <returns>64 luma bytes and 64 chroma</returns>
        static byte[] CreateQuantizationTables(uint Q)
        {
            //Factor restricted to range of 1 and 99
            int factor = (int)Math.Max(Math.Min(1, Q), 99);

            //Seed quantization value
            int q = (Q < 50 ? q = 5000 / factor : 200 - factor * 2);

            byte[] resultTables = new byte[128];

            //Create quantization table from Seed quality
            for (int i = 0; i < 128; ++i)
            {
                resultTables[i] = (byte)Math.Min(Math.Max((defaultQuantizers[i] * q + 50) / 100, 1), 255);
            }

            return resultTables;
        }

        static byte[] CreateQuantizationTablesMarker(ArraySegment<byte> tables)
        {
            List<byte> result = new List<byte>();

            int tableSize = tables.Count / 2; //2 tables same length

            //Luma

            result.Add(Prefix);
            result.Add(QuantizationTable);

            result.Add(0x00);//Length
            result.Add((byte)(tableSize + 3));

            result.Add(0x00);//Type - Lumiance

            for (int i = 0, e = tableSize; i < e; ++i)
            {
                result.Add(tables.Array[tables.Offset + i]);
            }

            //Chroma

            result.Add(Prefix);
            result.Add(QuantizationTable);

            result.Add(0x00);//Length
            result.Add((byte)(tableSize + 3));

            result.Add(0x01);//Type - Chromiance

            for (int i = tableSize, e = tables.Count; i < e; ++i)
            {
                result.Add(tables.Array[tables.Offset + i]);
            }

            return result.ToArray();

        }

        static byte[] lum_dc_codelens = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };

        static byte[] lum_dc_symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };



        static byte[] lum_ac_codelens = {0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };

        static byte[] lum_ac_symbols = {
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
        0xf9, 0xfa,
        };

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
        0xf9, 0xfa,
        };

        static byte[] CreateHuffmanTableMarker(byte[] codeLens, byte[] symbols, int tableNo, int tableClass)
        {
            List<byte> result = new List<byte>();
            result.Add(Prefix);
            result.Add(HuffmanTable);
            result.Add(0x00); //Legnth
            result.Add((byte)(3 + codeLens.Length + symbols.Length));
            result.Add((byte)((tableClass << 4) | tableNo)); //Id
            result.AddRange(codeLens);//Data
            result.AddRange(symbols);
            return result.ToArray();
        }

        static byte[] CreateDataRestartIntervalMarker(uint dri)
        {
            return new byte[] { Prefix, DataRestartInterval, 0x00, 0x04, (byte)(dri >> 8), (byte)(dri) };
        }        

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an empty JpegFrame
        /// </summary>
        internal JpegFrame() : base(JpegFrame.RtpJpegPayloadType) { }

        /// <summary>
        /// Creates a new JpegFrame from an existing RtpFrame which has the JpegFrame PayloadType
        /// </summary>
        /// <param name="f">The existing frame</param>
        public JpegFrame(RtpFrame f) : base(f) { if (PayloadType != JpegFrame.RtpJpegPayloadType) throw new ArgumentException("Expected the payload type 26, Found type: " + f.PayloadType); }

        public JpegFrame(JpegFrame f) : base(f) { if (PayloadType != JpegFrame.RtpJpegPayloadType) throw new ArgumentException("Expected the payload type 26, Found type: " + f.PayloadType); Image = f.ToImage(); }

        /// <summary>
        /// Creates a JpegFrame from a System.Drawing.Image
        /// </summary>
        /// <param name="source">The Image to create a JpegFrame from</param>
        public JpegFrame(System.Drawing.Image source, uint quality = 100, uint? ssrc = null, uint? sequenceNo = null, uint? timeStamp = null) : this()
        {
            //Must calculate correctly the Type, Quality, FragmentOffset and Dri
            uint TypeSpecific = 0, Type = 0, Quality = quality, Width = (uint)source.Width, Height = (uint)source.Height;

            byte[] RestartInterval = null; List<byte> QTables = new List<byte>();

            //Get the Jpeg Codec Info
            System.Drawing.Imaging.ImageCodecInfo codecInfo = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders().Where(d => d.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid).Single();

            //Save the image in Jpeg format and request the PropertyItems from the Jpeg format of the Image
            using(System.IO.MemoryStream temp = new System.IO.MemoryStream())
            {
                //Create Encoder Parameters for the Jpeg Encoder
                System.Drawing.Imaging.EncoderParameters parameters = new System.Drawing.Imaging.EncoderParameters(3);
                // Set the quality
                parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, Quality);
                // Set the render method to Progressive
                parameters.Param[1] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (int)System.Drawing.Imaging.EncoderValue.RenderProgressive);
                // Set the scan method to Progressive
                parameters.Param[2] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.ScanMethod, (int)System.Drawing.Imaging.EncoderValue.ScanMethodNonInterlaced);

                //Determine if there are multiple frames in the image
                //if(source.FrameDimensionsList.Count() > 0)
                //{
                //    System.Drawing.Imaging.FrameDimension dimension = new System.Drawing.Imaging.FrameDimension(source.FrameDimensionsList[0]);
                //    int frameCount = source.GetFrameCount(dimension);
                //    if (frameCount > 1)
                //    {
                //        ///Todo -  Handle Multiple Frames from a System.Drawing.Image (Gif) 
                //        ///Perhaps the sender should just SetActiveFrame and then we will use the active frame
                //    }
                //}

                if (source.Width > JpegFrame.MaxWidth || source.Height > JpegFrame.MaxHeight)
                {
                    using (System.Drawing.Image thumb = source.GetThumbnailImage(JpegFrame.MaxWidth, JpegFrame.MaxHeight, null, IntPtr.Zero))
                    {
                        //Save the source to the temp stream using the jpeg coded and given encoder params
                        thumb.Save(temp, codecInfo, parameters);
                    }
                }
                else
                {
                    //Save the source to the temp stream using the jpeg coded and given encoder params
                    source.Save(temp, codecInfo, parameters);
                }
                
                //Check for the EOI Marker
                temp.Seek(-1, System.IO.SeekOrigin.Current);

                //If present we will ignore it when creating the packets
                long endOffset = temp.ReadByte() == EndOfInformation ? temp.Length - 2 : temp.Length;

                //Enure at the beginning
                temp.Seek(0, System.IO.SeekOrigin.Begin);

                //Read the JPEG Back from the stream so it's pixel format is JPEG
                Image = System.Drawing.Image.FromStream(temp, false, true);

                //Determine if there are Quantization Tables which must be sent
                if (Image.PropertyIdList.Contains(0x5090) && Image.PropertyIdList.Contains(0x5091))
                {
                    //This is causing the QTables to be read on the reciever side
                    Quality |= 128;
                }
                else
                {
                    //Values less than 128 cause QTables to be generated on reciever side
                    Quality = 127;
                }

                //Determine if there is a DataRestartInterval
                if (Image.PropertyIdList.Contains(0x0203))
                {
                    RestartInterval = Image.GetPropertyItem(0x0203).Value;
                    Type = 64;
                }
                else
                {
                    Type = 63;
                }

                //used for reading the JPEG data
                int Tag, TagSize, 
                    //The max size of each Jpeg RtpPacket (Allow for any overhead)
                    BytesInPacket = RtpPacket.MaxPayloadSize - 200;

                //The current packet
                RtpPacket currentPacket = new RtpPacket(BytesInPacket);
                SynchronizationSourceIdentifier = currentPacket.SynchronizationSourceIdentifier = (ssrc ?? (uint)SynchronizationSourceIdentifier);
                currentPacket.TimeStamp = (uint)(timeStamp ?? Utility.DateTimeToNtpTimestamp(DateTime.UtcNow));
                currentPacket.SequenceNumber = (ushort)(sequenceNo ?? 1);
                currentPacket.PayloadType = JpegFrame.RtpJpegPayloadType;

                //Where we are in the current packet
                int currentPacketOffset = 0;

                //A RtpJpegHeader which must be in the Payload of each Packet (8 Bytes without QTables and RestartInterval)
                byte[] RtpJpegHeader = CreateRtpJpegHeader(TypeSpecific, 0, Type, Quality, Width, Height, RestartInterval, null);

                //Determine if we need to write OnVif Extension?
                if (Width > MaxWidth || Height > MaxHeight)
                {
                    //packet.Extensions = true;

                    //Write Extension Headers
                }

                //Ensure at the begining
                temp.Seek(0, System.IO.SeekOrigin.Begin);

                //Find a Jpeg Tag while we are not at the end of the stream
                while ((Tag = temp.ReadByte()) != -1)
                {                    
                    //If the prefix is a tag prefix then read another byte as the Tag
                    if (Tag == Prefix)
                    {                    
                        //Get the Tag
                        Tag = temp.ReadByte();

                        //If we are at the end break
                        if (Tag == -1) break;
                        
                        //Determine What to do for each Tag

                        //Start and End Tag (No Length)
                        if (Tag == StartOfInformation)
                        {
                            continue;
                        }
                        else if (Tag == EndOfInformation)
                        {
                            break;
                        }

                        //Read Length Bytes
                        byte h = (byte)temp.ReadByte(), l = (byte)temp.ReadByte();
                        
                        //Calculate Length
                        TagSize = h * 256 + l;

                        //Correct Length
                        TagSize -= 3; //Type byte and 2 Length bytes
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Encountered Tag " + Tag.ToString("X2") + ", Size " + TagSize.ToString());
#endif

                        //QTables are copied when Quality is > 127
                        if (Tag == QuantizationTable && Quality > 127)
                        {
                            //byte Precision = (byte)temp.ReadByte();//Discard Precision
                            //if (Precision != 0) throw new Exception("Only 8 Bit Precision is Supported");
                            
                            temp.ReadByte();//Discard Table Id (And Precision which is in the same byte)

                            byte[] table = new byte[TagSize];
                            
                            temp.Read(table, 0, TagSize);

                            QTables.AddRange(table);
                        }
                        else if (Tag == DataRestartInterval) //RestartInterval is copied
                        {
                            //Make DRI?      
                            //Type = 64;
                            //RestartInterval = CreateRtpDataRestartIntervalMarker((int)temp.Length, 1, 1, 0x3fff);
                            throw new NotImplementedException();
                        }                        
                        //Last Marker in Header before EntroypEncodedScan
                        else if (Tag == StartOfScan)
                        {
                            temp.ReadByte();//Discard Byte
                            
                            //Create RtpJpegHeader and CopyTo currentPacket advancing currentPacketOffset
                            byte[] data = CreateRtpJpegHeader(TypeSpecific, 0, Type, Quality, Width, Height, RestartInterval, QTables);

                            data.CopyTo(currentPacket.Payload, currentPacketOffset);

                            currentPacketOffset += data.Length;
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("Writing EntroypEncodedScan Data");
#endif

                            //Determine how much to read
                            int packetRemains = BytesInPacket - currentPacketOffset;

                            //How much remains in the stream relative to the endOffset
                            long streamRemains = endOffset - temp.Position;

                            //While we are not done reading
                            while (temp.Position < endOffset)
                            {
                                //Read what we can into the packet
                                packetRemains -= temp.Read(currentPacket.Payload, currentPacketOffset, packetRemains);

                                //Update how much remains
                                streamRemains = endOffset - temp.Position;

                                //Add current packet
                                Add(currentPacket);

                                //Determine if we need to adjust the size and add the packet
                                if (streamRemains < BytesInPacket - 8)
                                {
                                    //8 for the RtpJpegHeader and this will cause the Marker be to set
                                    packetRemains = (int)(streamRemains + 8);
                                }
                                else
                                {
                                    //Size is normal
                                    packetRemains = BytesInPacket;
                                }

                                //Make next packet                                    
                                currentPacket = new RtpPacket(packetRemains)
                                {
                                    TimeStamp = currentPacket.TimeStamp,
                                    SequenceNumber = (ushort)(currentPacket.SequenceNumber + 1),
                                    SynchronizationSourceIdentifier = currentPacket.SynchronizationSourceIdentifier,
                                    PayloadType = JpegFrame.RtpJpegPayloadType,
                                    Marker = packetRemains < BytesInPacket
                                };

                                //Correct FragmentOffset
                                BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)(temp.Position << 48))).CopyTo(RtpJpegHeader, 2);

                                //Copy header
                                RtpJpegHeader.CopyTo(currentPacket.Payload, 0);

                                //Set offset in packet.Payload
                                packetRemains -= currentPacketOffset = 8;
                            }
                        }
                        else //Skip past tag 
                        {
                            temp.Seek(TagSize, System.IO.SeekOrigin.Current);
                        }
                    }
                }

                //To allow the stream to be closed
                Image = new System.Drawing.Bitmap(Image);
            }
        }

        #endregion

        #region Fields

        //End result when encoding or decoding is cached in this member
        internal System.Drawing.Image Image;

        #endregion

        #region Methods

        /// <summary>
        /// Writes the packets to a memory stream and creates the default header and quantization tables if necessary.
        /// Assigns Image from the result
        /// </summary>
        internal void ProcessPackets()
        {
            uint TypeSpecific, FragmentOffset, Type, Quality, Width, Height, RestartInterval = 0;
            ArraySegment<byte> tables = default(ArraySegment<byte>);

            //Using a new MemoryStream for a Buffer
            using (System.IO.MemoryStream Buffer = new System.IO.MemoryStream())
            {
                //Loop each packet
                foreach(RtpPacket packet in this){
                //this.Packets.ForEach(packet =>
                //{
                    //Payload starts at offset 0
                    int offset = 0;

                    //Handle Extension Headers
                    if (packet.Extensions)
                    {
                        //This could be OnVif extension
                        //http://www.onvif.org/specs/stream/ONVIF-Streaming-Spec-v220.pdf

                        //Decode
                        //packet.ExtensionBytes;
                    }

                    //Decode RtpJpeg Header
                    TypeSpecific = (uint)(packet.Payload[offset++]);
                    FragmentOffset = (uint)(packet.Payload[offset++] << 16 | packet.Payload[offset++] << 8 | packet.Payload[offset++]);
                    Type = (uint)(packet.Payload[offset++]); //&1 for type
                    Quality = (uint)packet.Payload[offset++];
                    Width = (uint)(packet.Payload[offset++] * 8); // This should have been 128 or > and the standard would have worked for all resolutions
                    Height = (uint)(packet.Payload[offset++] * 8);// Now in certain highres profiles you will need an OnVif extension before the RtpJpeg Header

                    //Only occur in the first packet
                    if (FragmentOffset == 0)
                    {
                        if (Type >= 64 && Type <= 127)
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
                            RestartInterval = (uint)(packet.Payload[offset++] << 8 | packet.Payload[offset++]);
                            //offset++;
                            //offset++;
                        }

                        //If the quality > 127 there are usually Quantization Tables
                        if (Quality > 127)
                        {
                            if ((packet.Payload[offset++]) != 0)
                            {
                                //Must be Zero is Not Zero
                            }

                            //TableId and Precision
                            //If Precision == 1 then this is a 16 bit table...
                            //Also each bit should be checked to determine the number of tables?
                            byte Precision = (packet.Payload[offset++]);
                            //Might be more then 1 table...                            
                            //Determine by looking at each bit in Precision?
                            if (Precision > 0)
                            {
                                //Not Supported
                            }

                            //Length of all tables
                            ushort Length = (ushort)(packet.Payload[offset++] << 8 | packet.Payload[offset++]);

                            //If there is Table Data Read it
                            if (Length > 0)
                            {
                                tables = new ArraySegment<byte>(packet.Payload, offset, (int)Length);
                                offset += (int)Length;
                            }
                            else // Create it from the Quality
                            {
                                tables = new ArraySegment<byte>(CreateQuantizationTables(Quality & 128));
                            }
                        }
                        else // Create from the Quality
                        {
                            tables = new ArraySegment<byte>(CreateQuantizationTables(Quality));
                        }

                        //Write the header to the buffer if there are no Extensions
                        if (!packet.Extensions)
                        {
                            byte[] header = CreateJFIFHeader(Type, Width, Height, tables, RestartInterval);
                            Buffer.Write(header, 0, header.Length);
                        }
                        //else
                        //{
                        //    //Write header using Extensions...
                        //}

                    }

                    //Write the Payload data from the offset
                    Buffer.Write(packet.Payload, offset, packet.Payload.Length - offset);
                }//);

                //Check for EOI Marker
                Buffer.Seek(Buffer.Length - 1, System.IO.SeekOrigin.Begin);

                if (Buffer.ReadByte() != EndOfInformation)
                {
                    Buffer.WriteByte(Prefix);
                    Buffer.WriteByte(EndOfInformation);
                }

                //Go back to the beginning
                Buffer.Seek(0, System.IO.SeekOrigin.Begin);

                //This article explains in detail what exactly happens: http://support.microsoft.com/kb/814675
                //In short, for a lifetime of an Image constructed from a stream, the stream must not be destroyed.
                Image = new System.Drawing.Bitmap(System.Drawing.Image.FromStream(Buffer, false, true));
            }
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
                return new System.Drawing.Bitmap(Image);
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region Operators

        public static implicit operator System.Drawing.Image(JpegFrame f) { return f.ToImage(); }

        public static implicit operator JpegFrame(System.Drawing.Image f) { return new JpegFrame(f); }

        #endregion
    }
}
