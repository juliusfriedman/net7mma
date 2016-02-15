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

//http://tools.ietf.org/html/rfc6184

using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Media.Rtsp.Server.MediaTypes
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6184">RFC6184</see> which is used for H.264 Encoded video.
    /// </summary>
    public class RFC6184Media : RFC2435Media //Todo use RtpSink not RFC2435Media
    {
        //Some MP4 Related stuff
        //https://github.com/fyhertz/libstreaming/blob/master/src/net/majorkernelpanic/streaming/mp4/MP4Parser.java

        //C# h264 elementary stream stuff
        //https://bitbucket.org/jerky/rtp-streaming-server

        /// <summary>
        /// Implements Packetization and Depacketization of packets defined in <see href="https://tools.ietf.org/html/rfc6184">RFC6184</see>.
        /// </summary>
        public class RFC6184Frame : Rtp.RtpFrame
        {

            public static byte[] CreateSingleTimeAggregationUnit(int? DON = null, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n => Common.Binary.GetBytes((short)n.Length, BitConverter.IsLittleEndian).Concat(n));

                //STAP - B has DON at the very beginning
                if (DON.HasValue)
                {
                    data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationB).Concat(Common.Binary.GetBytes((short)DON, BitConverter.IsLittleEndian)).Concat(data);
                }//STAP - A
                else data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA).Concat(data);

                return data.ToArray();
            }

            public static byte[] CreateMultiTimeAggregationUnit(int DON, byte dond, int tsOffset, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n =>
                {
                    byte[] lengthBytes = new byte[2];
                    Common.Binary.Write16(lengthBytes, 0, BitConverter.IsLittleEndian, (short)n.Length);

                    //GetBytes

                    //DOND
                    //TS OFFSET

                    byte[] tsOffsetBytes = new byte[3];

                    Common.Binary.Write24(tsOffsetBytes, 0, BitConverter.IsLittleEndian, tsOffset);

                    return Media.Common.Extensions.Linq.LinqExtensions.Yield(dond).Concat(lengthBytes).Concat(n);
                });

                //MTAP has DON at the very beginning
                data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16).Concat(Media.Common.Binary.GetBytes((short)DON, BitConverter.IsLittleEndian)).Concat(data);

                return data.ToArray();
            }

            public static byte[] CreateMultiTimeAggregationUnit(int DON, byte dond, short tsOffset, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n =>
                {
                    byte[] lengthBytes = new byte[2];
                    Common.Binary.Write16(lengthBytes, 0, BitConverter.IsLittleEndian, (short)n.Length);

                    //Common.Binary.GetBytes((short)n.Length, BitConverter.IsLittleEndian);

                    //DOND

                    //TS OFFSET

                    byte[] tsOffsetBytes = new byte[2];

                    Common.Binary.Write16(tsOffsetBytes, 0, BitConverter.IsLittleEndian, tsOffset);
                    
                    return Media.Common.Extensions.Linq.LinqExtensions.Yield(dond).Concat(tsOffsetBytes).Concat(lengthBytes).Concat(n);
                });

                //MTAP has DON at the very beginning
                data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24).Concat(Media.Common.Binary.GetBytes((short)DON, BitConverter.IsLittleEndian)).Concat(data);

                return data.ToArray();
            }

            public RFC6184Frame(byte payloadType) : base(payloadType) { }

            public RFC6184Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC6184Frame(RFC6184Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            //Keep a list of encountered nal types so it can be augmented when Packetized or vice versa. Should store offset also?

            List<byte> m_ContainedNalTypes = new List<byte>();

            public bool ContainsSupplementalEncoderInformation
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.SupplementalEncoderInformation);
                }
            }

            public bool ContainsSequenceParameterSet
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet);
                }
            }

            public bool ContainsPictureParameterSet
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.PictureParameterSet);
                }
            }

            public bool ContainsInstantaneousDecoderRefresh
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.InstantaneousDecoderRefresh);
                }
            }

            public bool ContainsCodedSlice
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.CodedSlice);
                }
            }

            /// <summary>
            /// After Packetization or Depacketization, will indicate the types of Nal units contained in the data of the frame.
            /// </summary>
            public System.Collections.ObjectModel.ReadOnlyCollection<byte> ContainedUnitTypes
            {
                get
                {
                    return m_ContainedNalTypes.AsReadOnly();
                }
            }

            /// <summary>
            /// Creates any <see cref="Rtp.RtpPacket"/>'s required for the given nal
            /// </summary>
            /// <param name="nal">The nal</param>
            /// <param name="mtu">The mtu</param>
            public virtual void Packetize(byte[] nal, int mtu = 1500, int? DON = null)
            {
                if (nal == null) return;

                int nalLength = nal.Length;

                int offset = 0;

                if (nalLength >= mtu)
                {
                    //Consume the original header and move the offset into the data
                    byte nalHeader = nal[offset++],
                        nalFNRI = (byte)(nalHeader & 0xE0), //Extract the F and NRI bit fields
                        nalType = (byte)(nalHeader & Common.Binary.FiveBitMaxValue), //Extract the Type
                        fragmentType = (byte)(DON.HasValue ? Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB : Media.Codecs.Video.H264.NalUnitType.FragmentationUnitA),
                        fragmentIndicator = (byte)(nalFNRI | fragmentType);//Create the Fragment Indicator Octet

                    //Store the nalType contained
                    m_ContainedNalTypes.Add(nalType);

                    //No Marker yet
                    bool marker = false;

                    //Get the highest sequence number
                    int highestSequenceNumber = HighestSequenceNumber;

                    //Consume the bytes left in the nal
                    while (offset < nalLength)
                    {
                        //Get the data required which consists of the fragmentIndicator, Constructed Header and the data.
                        IEnumerable<byte> data;

                        //Build the Fragmentation Header

                        //First Packet
                        if (offset == 1)
                        {
                            //FU (A/B) Indicator with F and NRI
                            //Start Bit Set with Original NalType

                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(((byte)(0x80 | nalType))));
                        }
                        else if (offset + mtu > nalLength)
                        {
                            //End Bit Set with Original NalType
                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(((byte)(0x40 | nalType))));

                            //Rtp marker bit is also set
                            marker = true;
                        }
                        else//For packets other than the start or end
                        {
                            //No Start, No End
                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(nalType));
                        }

                        //Add the data the fragment data from the original nal
                        data = Enumerable.Concat(data, nal.Skip(offset).Take(mtu));

                        //FU - B has DON at the very beginning
                        if (fragmentType == Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB && highestSequenceNumber == 0)
                        {
                            byte[] DONBytes = new byte[2];
                            Common.Binary.Write16(DONBytes, 0, BitConverter.IsLittleEndian, (short)DON);
                            data = Enumerable.Concat(DONBytes, data);
                        }
                        
                        //Add the packet using the next highest sequence number
                        Add(new Rtp.RtpPacket(2, false, false, marker, PayloadTypeByte, 0, SynchronizationSourceIdentifier, ++highestSequenceNumber, 0, data.ToArray()));

                        //Move the offset
                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, true, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));
            }

            /// <summary>
            /// Creates <see cref="Buffer"/> with a H.264 RBSP from the contained packets
            /// </summary>
            public virtual void Depacketize()
            {
                DisposeBuffer();

                this.Buffer = new MemoryStream();

                //Get all packets in the frame
                foreach (Rtp.RtpPacket packet in m_Packets)
                    ProcessPacket(packet);

                //Order by DON?
                this.Buffer.Position = 0;
            }

            /// <summary>
            /// Depacketizes a single packet.
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="containsSps"></param>
            /// <param name="containsPps"></param>
            /// <param name="containsSei"></param>
            /// <param name="containsSlice"></param>
            /// <param name="isIdr"></param>
            internal protected virtual void ProcessPacket(Rtp.RtpPacket packet)
            {

                //From the beginning of the data in the actual payload
                int payloadOffset = packet.Payload.Offset, offset = payloadOffset + packet.HeaderOctets,
                    count = packet.Payload.Count - (offset + packet.PaddingOctets); //until the end of the actual payload

                //Obtain the data of the packet (without source list or padding)
                byte[] packetData = packet.Payload.Array; //PayloadData.ToArray();

                //Must have at least 2 bytes
                if (count <= 2) return;

                //Determine if the forbidden bit is set and the type of nal from the first byte
                byte firstByte = packetData[offset];

                //bool forbiddenZeroBit = ((firstByte & 0x80) >> 7) != 0;

                byte nalUnitType = (byte)(firstByte & Common.Binary.FiveBitMaxValue);                

                //TODO

                //o  The F bit MUST be cleared if all F bits of the aggregated NAL units are zero; otherwise, it MUST be set.
                //if (forbiddenZeroBit && nalUnitType <= 23 && nalUnitType > 29) throw new InvalidOperationException("Forbidden Zero Bit is Set.");

                //Optomize setting out parameters, could be done with a label or with a static function.

                //Determine what to do
                switch (nalUnitType)
                {
                    //Reserved - Ignore
                    case Media.Codecs.Video.H264.NalUnitType.Unknown:
                    case Media.Codecs.Video.H264.NalUnitType.PayloadContentScalabilityInformation:
                    case Media.Codecs.Video.H264.NalUnitType.Reserved:
                        {
                            //May have 4 byte NAL header.
                            //Do not handle
                            return;
                        }
                    case Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA: //STAP - A
                    case Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationB: //STAP - B
                    case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16: //MTAP - 16
                    case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24: //MTAP - 24
                        {
                            //Move to Nal Data
                            ++offset;

                            //Todo Determine if need to Order by DON first.
                            //EAT DON for ALL BUT STAP - A
                            if (nalUnitType != Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA) offset += 2;

                            //Consume the rest of the data from the packet
                            while (offset < count)
                            {
                                //Determine the nal unit size which does not include the nal header
                                int tmp_nal_size = Common.Binary.Read16(packetData, offset, BitConverter.IsLittleEndian);
                                offset += 2;

                                //If the nal had data then write it
                                if (tmp_nal_size > 0)
                                {

                                    //Store the nalType contained
                                    m_ContainedNalTypes.Add(nalUnitType);

                                    //For DOND and TSOFFSET
                                    switch (nalUnitType)
                                    {
                                        case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16:// MTAP - 16
                                            {
                                                //SKIP DOND and TSOFFSET
                                                offset += 3;
                                                goto default;
                                            }
                                        case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24:// MTAP - 24
                                            {
                                                //SKIP DOND and TSOFFSET
                                                offset += 4;
                                                goto default;
                                            }
                                        default:
                                            {
                                                //Read the nal header but don't move the offset
                                                //byte nalHeader = (byte)(packetData[offset] & Common.Binary.FiveBitMaxValue);

                                                //Store the nalType contained
                                                //m_ContainedNalTypes.Add(nalHeader);

                                                //>= 6 && <= 8
                                                //if (nalHeader == 6 || nalHeader == 7 || nalHeader == 8) Buffer.WriteByte(0);

                                                WriteStartCode(ref packetData[offset]);

                                                //Done reading
                                                break;
                                            }
                                    }

                                    //Write the start code
                                    //Buffer.Write(Media.Codecs.Video.H264.NalUnitType.StartCode, 0, 3);

                                    //Write the nal header and data
                                    Buffer.Write(packetData, offset, tmp_nal_size);

                                    //Move the offset past the nal
                                    offset += tmp_nal_size;
                                }
                            }

                            //No more data in packet.
                            return;
                        }
                    case Media.Codecs.Video.H264.NalUnitType.FragmentationUnitA: //FU - A
                    case Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB: //FU - B
                        {
                            /*
                             Informative note: When an FU-A occurs in interleaved mode, it
                             always follows an FU-B, which sets its DON.
                             * Informative note: If a transmitter wants to encapsulate a single
                              NAL unit per packet and transmit packets out of their decoding
                              order, STAP-B packet type can be used.
                             */
                            //Need 2 bytes
                            if (count > 2)
                            {
                                //Read the Header
                                byte FUHeader = packetData[++offset];

                                bool Start = ((FUHeader & 0x80) >> 7) > 0;

                                bool End = ((FUHeader & 0x40) >> 6) > 0;

                                //bool Receiver = (FUHeader & 0x20) != 0;

                                //if (Receiver) throw new InvalidOperationException("Receiver Bit Set");

                                //Move to data
                                ++offset;

                                //Todo Determine if need to Order by DON first.
                                //DON Present in FU - B
                                if (nalUnitType == 29) offset += 2;

                                //Should verify count... just consumed 1 - 3 bytes and only required 2.

                                //Determine the fragment size
                                int fragment_size = count - offset;

                                //If the start bit was set
                                if (Start)
                                {
                                    //Reconstruct the nal header
                                    //Use the first 3 bits of the first byte and last 5 bites of the FU Header
                                    byte nalHeader = (byte)((firstByte & 0xE0) | (FUHeader & Common.Binary.FiveBitMaxValue));

                                    //Store the nalType contained
                                    //m_ContainedNalTypes.Add((byte)(nalHeader & Common.Binary.FiveBitMaxValue));

                                    //Emulation prevention byte...
                                    //if (nalHeader == 6 || nalHeader == 7 || nalHeader == 8) Buffer.WriteByte(0);

                                    //Write the start code
                                    //Buffer.Write(Media.Codecs.Video.H264.NalUnitType.StartCode, 0, 3);

                                    WriteStartCode(ref nalHeader);

                                    //Write the re-construced header
                                    Buffer.WriteByte(nalHeader);
                                }

                                //If the size was valid
                                if (fragment_size > 0)
                                {
                                    //Write the data of the fragment.
                                    Buffer.Write(packetData, offset, fragment_size);
                                }

                                //Allow If End to Write End Sequence?

                                if (End) Buffer.WriteByte(Media.Codecs.Video.H264.NalUnitType.EndOfSequence);
                            }

                            //No more data?
                            return;
                        }
                    default:
                        {
                            //Store the nalType contained
                            //m_ContainedNalTypes.Add(nalUnitType);

                            //if (nalUnitType == 6 || nalUnitType == 7 || nalUnitType == 8) Buffer.WriteByte(0);

                            //Write the start code
                            //Buffer.Write(Media.Codecs.Video.H264.NalUnitType.StartCode, 0, 3);

                            WriteStartCode(ref nalUnitType);

                            //Write the nal heaer and data data
                            Buffer.Write(packetData, offset, count - offset);

                            return;
                        }
                }
            }

            internal void DisposeBuffer()
            {
                if (Buffer != null)
                {
                    Buffer.Dispose();
                    Buffer = null;
                }

                m_ContainedNalTypes.Clear();
            }

            protected virtual void WriteStartCode(ref byte nalHeader)
            {
                //Determine the type of Nal
                byte nalType = (byte)(nalHeader & Common.Binary.FiveBitMaxValue);
                
                //Store the nalType contained
                m_ContainedNalTypes.Add(nalType);

                //Determine if the Emulation prevention byte is required.
                switch (nalType)
                {
                    case Media.Codecs.Video.H264.NalUnitType.SupplementalEncoderInformation://6:                    
                    case Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet://7:
                    case Media.Codecs.Video.H264.NalUnitType.PictureParameterSet://8:
                    case Media.Codecs.Video.H264.NalUnitType.AccessUnitDelimiter://9
                    //case Media.Codecs.Video.H264.NalUnitType.CodedSlice:
                    //case Media.Codecs.Video.H264.NalUnitType.SliceExtension:
                        {
                            //Write the extra 0 byte to the Buffer
                            Buffer.WriteByte(0);

                            //Handle as normal
                            goto default;
                        }
                    default:
                        {
                            //Write the start code to the Buffer
                            Buffer.Write(Media.Codecs.Video.H264.NalUnitType.StartCode, 0, 3);

                            //Done
                            return;
                        }
                }
            }

            public override void Dispose()
            {
                if (IsDisposed) return;
                base.Dispose();                
                DisposeBuffer();
            }

            //To go to an Image...
            //Look for a SliceHeader in the Buffer
            //Decode Macroblocks in Slice
            //Convert Yuv to Rgb
        }

        #region Fields

        //Should be created dynamically

        //http://www.cardinalpeak.com/blog/the-h-264-sequence-parameter-set/

        //TODO, Use a better starting point e.g. https://github.com/jordicenzano/h264simpleCoder/blob/master/src/CJOCh264encoder.h or the OpenH264 stuff @ https://github.com/cisco/openh264

        protected byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        protected byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 },
            slice_header1 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x84, 0x21, 0xa0 },
            slice_header2 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x94, 0x21, 0xa0 };

        //bool useSliceHeader1 = true;
        
        byte[] macroblock_header = { 0x0d, 0x00 };

        #endregion

        #region Constructor

        public RFC6184Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            clockRate = 90;
        }

        #endregion

        #region Methods

        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove JPEG Track
            SessionDescription.RemoveMediaDescription(0);
            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the given payload type            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 96));

            //Add the control line and media attributes to the Media Description
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 H264/90000"));

            //Sps and pps should be given...
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=fmtp:96 profile-level-id=" + Common.Binary.ReadU24(sps, 4, false == BitConverter.IsLittleEndian).ToString("X2") + ";sprop-parameter-sets=" + Convert.ToBase64String(sps, 4, sps.Length - 4) + ',' + Convert.ToBase64String(pps, 4, pps.Length - 4)));

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, sourceId));
        }

        //Move to Codec/h264
        //H264Encoder

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public override void Packetize(System.Drawing.Image image)
        {
            try
            {
                //Make the width and height correct
                using (var thumb = image.Width != Width || image.Height != Height ? image.GetThumbnailImage(Width, Height, null, IntPtr.Zero) : image)
                {
                    //Ensure the transformation will work.
                    if (thumb.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb) throw new NotSupportedException("Only ARGB is currently supported.");

                    //Create a new frame
                    var newFrame = new RFC6184Frame(96); //should all payload type to come from the media description...

                    //Get RGB Stride
                    System.Drawing.Imaging.BitmapData data = ((System.Drawing.Bitmap)thumb).LockBits(new System.Drawing.Rectangle(0, 0, thumb.Width, thumb.Height),
                               System.Drawing.Imaging.ImageLockMode.ReadOnly, thumb.PixelFormat);

                    //MUST Convert the bitmap to yuv420
                    //switch on image.PixelFormat
                    //Utility.YUV2RGBManaged()
                    //Utility.ABGRA2YUV420Managed(image.Width, image.Height, data.Scan0);
                    //etc

                    //Todo use Media.Image.Transformations                   

                    byte[] yuv = Media.Codecs.Image.ColorConversions.ABGRA2YUV420Managed(thumb.Width, thumb.Height, data.Scan0);

                    ((System.Drawing.Bitmap)image).UnlockBits(data);

                    data = null;

                    List<IEnumerable<byte>> macroBlocks = new List<IEnumerable<byte>>();

                    //For each h264 Macroblock in the frame
                    for (int i = 0; i < Height / 16; i++)
                        for (int j = 0; j < Width / 16; j++)
                            macroBlocks.Add(EncodeMacroblock(i, j, yuv)); //Add an encoded macroblock to the list

                    macroBlocks.Add(new byte[] { 0x80 });//Stop bit (Wasteful by itself)

                    //Packetize the data
                    newFrame.Packetize(macroBlocks.SelectMany(mb => mb).ToArray());

                    //Add the frame
                    AddFrame(newFrame);

                    yuv = null;

                    macroBlocks.Clear();

                    macroBlocks = null;
                }
            }
            catch { throw; }
        }

        IEnumerable<byte> EncodeMacroblock(int i, int j, byte[] yuvData)
        {

            IEnumerable<byte> result = Media.Common.MemorySegment.EmptyBytes;

            int frameSize = Width * Height;
            int chromasize = frameSize / 4;

            int yIndex = 0;
            int uIndex = frameSize;
            int vIndex = frameSize + chromasize;

            //If not the first macroblock in the slice
            if (!((i == 0) && (j == 0))) result = macroblock_header;
            else //There are offsets to the pixel values
            {
                int offset = i * Height + j * Width;

                if (offset > 0)
                {
                    yIndex += offset;
                    uIndex += offset;
                    vIndex += offset;
                }
            }

            //Take the Luma Values
            result = result.Concat(yuvData.Skip(yIndex ).Take(16 * 8));

            //Take the Chroma Values
            result = result.Concat(yuvData.Skip(uIndex ).Take(8 * 8));

            result = result.Concat(yuvData.Skip(vIndex ).Take(8 * 8));

            return result;
        }

        #endregion
    }
}