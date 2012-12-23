using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtp
{
    /// <summary>
    /// Implements RFC2435
    /// </summary>
    public class JpegFrame : RtpFrame
    {

        #region Statics

        new public const byte PayloadType = 26;

        static byte Prefix = 0xff;

        const byte StartOfInformation = 0xd8;

        const byte EndOfInformation = 0xd9;
        
        static byte[] CreateJFIFHeader(uint type, uint width, uint height, ArraySegment<byte> tables, uint dri)
        {
            List<byte> result = new List<byte>();
            result.Add(Prefix);
            result.Add(StartOfInformation);

            result.Add(0xff);
            result.Add(0xe0);//AppFirst
            result.Add(0x00);
            result.Add(0x10);//length
            result.Add((byte)'J'); //Always equals "JFXX" (with zero following) (0x4A46585800)
            result.Add((byte)'F');
            result.Add((byte)'I');
            result.Add((byte)'F');
            result.Add(0x00);

            result.Add(0x01);//Version Major
            result.Add(0x01);//Version Minor

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

            result.AddRange(CreateQuantizationTablesMarker(tables));

            result.Add(0xff);
            result.Add(0xc0);//SOF
            result.Add(0x00);
            result.Add(0x11);
            result.Add(0x08);
            result.Add((byte)(height >> 8));
            result.Add((byte)height);
            result.Add((byte)(width >> 8));
            result.Add((byte)width);
            
            result.Add(0x03);
            result.Add(0x01);
            result.Add((byte)(type > 0 ? 0x22 : 0x21)); //Is Dri Present?
            result.Add(0x00);

            result.Add(0x02);
            result.Add(0x11);
            result.Add(0x01);

            result.Add(0x03);
            result.Add(0x11);
            result.Add(0x01);

            //Huffman Tables
            result.AddRange(CreateHuffmanTableMarker(lum_dc_codelens, lum_dc_symbols, 0, 0));
            result.AddRange(CreateHuffmanTableMarker(lum_ac_codelens, lum_ac_symbols, 0, 1));
            result.AddRange(CreateHuffmanTableMarker(chm_dc_codelens, chm_dc_symbols, 1, 0));
            result.AddRange(CreateHuffmanTableMarker(chm_ac_codelens, chm_ac_symbols, 1, 1));
            
            result.Add(0xff);
            result.Add(0xda);//Marker SOS
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

            result.Add(0xff);
            result.Add(0xdb);

            result.Add(0x00);
            result.Add((byte)(tableSize + 3));
            result.Add(0x00);

            for (int i = 0, e = tableSize; i < e; ++i)
            {
                result.Add(tables.Array[tables.Offset + i]);
            }

            //Chroma

            result.Add(0xff);
            result.Add(0xdb);

            result.Add(0x00);
            result.Add((byte)(tableSize + 3));
            result.Add(0x01);

            for (int i = tableSize, e = tables.Count; i < e; ++i)
            {
                result.Add(tables.Array[tables.Offset + i]);
            }

            return result.ToArray();

        }

        static byte[] lum_dc_codelens = {
        0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
        };

        static byte[] lum_dc_symbols = {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        static byte[] lum_ac_codelens = {
        0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d,
        };

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

        static byte[] chm_dc_codelens = {
        0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0,
        };

        static byte[] chm_dc_symbols = {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 
        };

        static byte[] chm_ac_codelens = {
        0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77,
        };

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
            result.Add(0xff);
            result.Add(0xc4);
            result.Add(0x00);
            result.Add((byte)(3 + codeLens.Length + symbols.Length));
            result.Add((byte)((tableClass << 4) | tableNo));
            result.AddRange(codeLens);
            result.AddRange(symbols);
            return result.ToArray();
        }

        static byte[] CreateDataRestartIntervalMarker(uint dri)
        {
            return new byte[] { 0xff, 0xdd, 0x00, 0x04, (byte)(dri >> 8), (byte)(dri) };
        }

        #endregion

        #region Constructor

        public JpegFrame() : base(JpegFrame.PayloadType) { }

        public JpegFrame(RtpFrame f) : base(f) { if (PayloadType != JpegFrame.PayloadType) throw new ArgumentException("Expected the payload type 26, Found type: " + f.PayloadType); }

        /// <summary>
        /// Creates a JpegFrame from a System.Drawing.Image
        /// Not Working. Still has some problems stripping tags.
        /// </summary>
        /// <param name="source">The Image to create a JpegFrame from</param>
        public JpegFrame(System.Drawing.Image source, uint? quality = null, uint? ssrc = null, uint? sequenceNo = null) : this()
        {
            //Must calculate correctly the Type, Quality, FragmentOffset and Dri
            uint TypeSpecific = 0, FragmentOffset = 0, Type = 0, Quality = quality ?? 25, Width = (uint)source.Width, Height = (uint)source.Height;

            byte[] DataRestartInterval = null; List<byte> QTables = new List<byte>();
            
            //Save the image in Jpeg format and request the PropertyItems from the Jpeg format of the Image
            using(System.IO.MemoryStream temp = new System.IO.MemoryStream())
            {

                System.Drawing.Imaging.ImageCodecInfo codecInfo = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders().Where(d => d.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid).Single();

                //  Set the quality
                System.Drawing.Imaging.EncoderParameters parameters = new System.Drawing.Imaging.EncoderParameters(4);                
                parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, Quality);
                parameters.Param[1] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 8);
                parameters.Param[2] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (int)System.Drawing.Imaging.EncoderValue.RenderNonProgressive);
                parameters.Param[3] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.ScanMethod, (int)System.Drawing.Imaging.EncoderValue.ScanMethodInterlaced);
                
                //Save the source to the temp stream
                source.Save(temp, codecInfo, parameters);

                //Enure at the beginning
                temp.Seek(0, System.IO.SeekOrigin.Begin);

                //Get the Jpeg Ready
                Image = System.Drawing.Image.FromStream(temp);

                //Determine if there are QTables and attemp to read them
                if (Image.PropertyIdList.Contains(0x5090) && Image.PropertyIdList.Contains(0x5091))
                {
                    int f = 0;
                    Quality = 128;
                    try
                    {
                        temp.Seek(0, System.IO.SeekOrigin.Begin);

                        Find:
                        //Find the Quantization Tables Tag
                        while (temp.ReadByte() != 0xDB)
                        {

                        }
                        //Try to read the QTables                        
                        byte m = (byte)temp.ReadByte(), l = (byte)temp.ReadByte();
                        int len = m * 256 + l;
                        
                        temp.ReadByte(); //Table type

                        byte[] table = new byte[len - 3];
                        temp.Read(table, 0, len - 3);
                        QTables.AddRange(table);
                        f++;
                        if (f < 2) goto Find;
                    }
                    catch
                    {
                        temp.Seek(0, System.IO.SeekOrigin.Begin);                        
                    }
                }
                else
                {
                    Quality = 127;
                }
 
                if (Image.PropertyIdList.Contains(0x0203))
                {
                    DataRestartInterval = Image.GetPropertyItem(0x0203).Value;
                }
                
                if (DataRestartInterval != null) Type = 64;
                else Type = 63;                

                int Tag, TagSize;

                DateTime ts = DateTime.Now;
                int sequenceNumber = (int)(sequenceNo ?? DateTime.Now.Ticks);

                RtpPacket packet = new RtpPacket();
                SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier = (ssrc ?? (uint)SynchronizationSourceIdentifier);
                packet.TimeStamp = (uint)Utility.DateTimeToNtp64(ts);
                packet.SequenceNumber = sequenceNumber;
                packet.PayloadType = JpegFrame.PayloadType;

                //Again 8 for divisors was too small... 4096 and larger can be supported just by changing that
                byte[] RtpJpegHeader = new byte[] { (byte)(TypeSpecific), (byte)(FragmentOffset), (byte)(FragmentOffset), (byte)(FragmentOffset), (byte)(Type), (byte)(Quality), (byte)(Width / 8), (byte)(Height / 8) };

                //Determine if we need to write OnVif Extension?
                //if (Width > 2048 || Height > 4096)
                //{
                //packet.Extensions = true;
                //}

                int at = 0;

                RtpJpegHeader.CopyTo(packet.Payload, at);
                FragmentOffset = (uint)(at = 8);

                //Only the first packet has FragmentOffset = 0
                RtpJpegHeader[3] = 8;

                //Write the First Packets Data
                {
                    List<byte> FirstPacketData = new List<byte>();

                    //Write the DataRestartInterval
                    if (DataRestartInterval != null)
                    {
                        FirstPacketData.AddRange(DataRestartInterval);
                    }

                    FirstPacketData.Add(0x00);//MBZ (Must Be Zero)

                    if (QTables != null)
                    {
                        //byte[] LTable = Image.GetPropertyItem(0x5090).Value;
                        //byte[] CTable = Image.GetPropertyItem(0x5091).Value;
                        //short length = (short)(LTable.Length + CTable.Length);
                        FirstPacketData.Add(0x00);//Precision (should be read from QTables Tag)
                        //FirstPacketData.AddRange(BitConverter.GetBytes((short)Utility.HostToNetworkOrderShort((ushort)length)));
                        //FirstPacketData.AddRange(LTable); //(should be copied while reading QTables Tag)
                        //FirstPacketData.AddRange(CTable);
                        FirstPacketData.AddRange(BitConverter.GetBytes((short)Utility.HostToNetworkOrderShort((ushort)QTables.Count)));
                        FirstPacketData.AddRange(QTables);
                    }
                    else
                    {
                        FirstPacketData.Add(0x00);//Precision (ISO/IEC 10918-1 -> Shall be 0 for 8-Bit Samples)
                        FirstPacketData.Add(0x00);//Length
                        FirstPacketData.Add(0x80);//Length
                        FirstPacketData.AddRange(CreateQuantizationTables(Quality));
                    }

                    FirstPacketData.ToArray().CopyTo(packet.Payload, at);
                    at += FirstPacketData.Count;
                }

                //Ensure at the begining (Might not need to since data was consumed to determine RtpJpegHeader)
                temp.Seek(0, System.IO.SeekOrigin.Begin);

                //While we are not at the end of the jpeg data
                while ((Tag = temp.ReadByte()) != -1)
                {
                    //If the prefix is a tag prefix then read another byte as the Tag
                    if (Tag == Prefix)
                    {
                        //We Found the Prefix
                    PrefixFound:
                        //Get the Tag
                        Tag = temp.ReadByte();

                        //If we are at the end break
                        if (Tag == -1) break;

                        //http://www.w3.org/Graphics/JPEG/itu-t81.pdf
                        //http://en.wikipedia.org/wiki/JPEG
                        //Determine What to do for each Tag

                        //Start and End Tag (No Length)
                        if (Tag == StartOfInformation || Tag == EndOfInformation) continue;
                        //Entropy Encoded Scan (No Length)
                        else if (Tag == 0) 
                        {
                            packet.Payload[at++] = Prefix;
                            packet.Payload[at++] = (byte)Tag;
                            
                            //Write a value at a time until a Prefix Tag is found
                            while ((Tag = temp.ReadByte()) != -1)
                            {                                
                                if (Tag == Prefix) goto PrefixFound;
                                else packet.Payload[at++] = (byte)Tag;

                                if (at >= Rtp.RtpPacket.MaxPayloadSize)
                                {
                                    //Add current packet
                                    AddPacket(packet);

                                    //Make next packet                                    
                                    packet = new RtpPacket()
                                    {
                                        TimeStamp = packet.TimeStamp,
                                        SequenceNumber = ++sequenceNumber,
                                        SynchronizationSourceIdentifier = (uint)SynchronizationSourceIdentifier,
                                        PayloadType = JpegFrame.PayloadType
                                    };

                                    //Correct FragmentOffset
                                    BitConverter.GetBytes((short)Utility.HostToNetworkOrderShort((ushort)temp.Position)).CopyTo(RtpJpegHeader, 1);

                                    //Copy header
                                    RtpJpegHeader.CopyTo(packet.Payload, 0);

                                    //Set offset in packet.Payload
                                    at = 8;
                                }
                            }
                        }
                        // Removed Tags: APPFirst, Quantization Tables, Huffman Tables and Data Restart Interval, Text Comment, Start of Frame, Start of Scan and RST
                        else if (Tag == 0xE0 || Tag == 0xDB || Tag == 0xC4 || Tag == 0xDD || Tag == 0xFE || Tag == 0xC0 || Tag == 0xDA || (Tag >= 0xD0 && Tag <= 0xD7))
                        {
                            //Read Length         
                            TagSize = (byte)temp.ReadByte() * 256 + (byte)temp.ReadByte();

                            //Seek past Tag contents
                            temp.Seek(TagSize, System.IO.SeekOrigin.Current);

                            continue;
                        }
                        // A Tag which is not Removed
                        else
                        {
                            //Write tag
                            packet.Payload[at++] = Prefix;
                            packet.Payload[at++] = (byte)Tag;

                            //Read and Write Length
                            TagSize = 256 * (packet.Payload[at++] = (byte)temp.ReadByte());
                            TagSize += packet.Payload[at++] = (byte)temp.ReadByte();

                            //Packet cannot fit the entire data of the tag
                            //Should probably set Frament Offset correctly
                            if (at + TagSize >= Rtp.RtpPacket.MaxPayloadSize)
                            {
                                int written = 0;
                                while (written < TagSize)
                                {
                                    //Determine how much is left
                                    int remaining = Rtp.RtpPacket.MaxPayloadSize - at;

                                    //Write what we can fit
                                    at += temp.Read(packet.Payload, at, remaining);

                                    //Indicate how much was written
                                    written += remaining;

                                    //Add current packet
                                    AddPacket(packet);

                                    //Make next packet                                    
                                    packet = new RtpPacket()
                                    {
                                        TimeStamp = packet.TimeStamp,
                                        SequenceNumber = ++sequenceNumber,
                                        SynchronizationSourceIdentifier = (uint)SynchronizationSourceIdentifier,
                                        PayloadType = JpegFrame.PayloadType
                                    };

                                    //Correct FragmentOffset
                                    BitConverter.GetBytes((short)Utility.HostToNetworkOrderShort((ushort)written)).CopyTo(RtpJpegHeader, 1);

                                    //Copy header
                                    RtpJpegHeader.CopyTo(packet.Payload, 0);

                                    //Set offset in packet.Payload
                                    at = 8;
                                }
                            }
                            else
                            {
                                //Write tag data
                                temp.Read(packet.Payload, at, TagSize);

                                //Advance ofset in packet.Payload
                                at += TagSize;
                            }
                        }
                    }
                }

                //Add the final packet
                AddPacket(packet);
            }

            //Set the complete marker on the last packet
            Packets.Last().Marker = Complete = true;
        }

        ~JpegFrame() { if (Image != null) { Image.Dispose(); Image = null; } }

        #endregion

        internal System.Drawing.Image Image;

        /// <summary>
        /// //Writes the packets to a memory stream and created the default header and quantization tables if necessary.
        /// </summary>
        internal void ProcessPackets()
        {
            ArraySegment<byte> tables = default(ArraySegment<byte>);
            int offset = 0;

            uint TypeSpecific, FragmentOffset, Type, Quality, Width, Height, DataRestartInterval = 0;

            using (System.IO.MemoryStream Buffer = new System.IO.MemoryStream())
            {
                for (int i = 0, e = this.Packets.Count; i < e; ++i)
                {
                    RtpPacket packet = this.Packets[i];

                    //Handle Extension Headers
                    if (packet.Extensions)
                    {
                        //This could be OnVif extension
                        //http://www.scribd.com/doc/50850591/225/JPEG-over-RTP
                        //Check in packet.m_ExtensionData

                        //Length in next two bytes
                        int len = packet.Payload[offset + 2] * 256 * packet.Payload[offset + 3];
                        //Then write to buffer
                        //Then continue from offset decoding RtpJpeg Header
                        //offset += 4 + len;
                        Buffer.Write(packet.Payload, offset, 2);
                        Buffer.WriteByte(packet.Payload[offset + 2]);
                        Buffer.WriteByte(packet.Payload[offset + 3]);
                        Buffer.Write(packet.Payload, offset + 4, len);
                    }

                    //RtpJpeg Header

                    TypeSpecific = (uint)(packet.Payload[offset++]);
                    FragmentOffset = (uint)(packet.Payload[offset++] << 16 | packet.Payload[offset++] << 8 | packet.Payload[offset++]);
                    Type = (uint)(packet.Payload[offset++]); //&1 for type
                    Quality = (uint)packet.Payload[offset++];
                    Width = (uint)(packet.Payload[offset++] * 8); // This should have been 128 or > and the standard would have worked for all resolutions
                    Height = (uint)(packet.Payload[offset++] * 8);// Now in certain highres profiles you will need an OnVif extension before the RtpJpeg Header

                    //Only occur in the first packet
                    if (FragmentOffset == 0)
                    {
                        if (Type > 63)
                        {
                            DataRestartInterval = (uint)(packet.Payload[offset++] << 8 | packet.Payload[offset++]);
                        }

                        if (Quality > 127)
                        {
                            if ((packet.Payload[offset++]) != 0)
                            {
                                //Must be Zero is Not Zero
                            }
                            uint Precision = (uint)(packet.Payload[offset++]);
                            //Might be more then 1 table...
                            uint Length = (uint)(packet.Payload[offset++] << 8 | packet.Payload[offset++]);
                            if (Length > 0)
                            {
                                tables = new ArraySegment<byte>(packet.Payload, offset, (int)Length);
                                offset += (int)Length;
                            }
                            else
                            {
                                tables = new ArraySegment<byte>(CreateQuantizationTables(Quality));
                            }
                        }
                        else
                        {
                            tables = new ArraySegment<byte>(CreateQuantizationTables(Quality));
                        }

                        //Write the header to the buffer if there are no Extensions
                        if (!packet.Extensions)
                        {                            
                            byte[] header = CreateJFIFHeader(Type, Width, Height, tables, DataRestartInterval);
                            Buffer.Write(header, 0, header.Length);
                        }

                    }

                    Buffer.Write(packet.Payload, offset, packet.Payload.Length - offset);

                }

                //Check for EOI Marker
                Buffer.Seek(Buffer.Length - 2, System.IO.SeekOrigin.Begin);

                if (Buffer.ReadByte() != Prefix && Buffer.ReadByte() != EndOfInformation)
                {
                    Buffer.WriteByte(Prefix);
                    Buffer.WriteByte(EndOfInformation);
                }

                //Go back to the beginning
                Buffer.Seek(0, System.IO.SeekOrigin.Begin);

                Image = System.Drawing.Image.FromStream(Buffer, false, true);
                //Should verify tables, dri or quality etc?
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
                if (Image != null) return Image;
                ProcessPackets();
                return Image;
            }
            catch
            {
                throw;
            }
        }

        public static implicit operator System.Drawing.Image(JpegFrame f) { return f.ToImage(); }
        public static implicit operator JpegFrame(System.Drawing.Image f) { return new JpegFrame(f); }

    }
}
