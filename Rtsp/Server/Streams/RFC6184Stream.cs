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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Media.Rtsp.Server.Streams
{

    /// <summary>
    /// Sends System.Drawing.Images over Rtp by encoding them as a RFC2435 Jpeg then Wrapping in a RFC6184 H.264 RBSP [Raw Byte Sequence Payload].
    /// </summary>
    public class RFC6184Stream : RFC2435Stream
    {

        //Logic will be incorperated into the (De)Packetize method of the Frame
        //https://code.google.com/p/android-rcs-ims-stack/source/browse/trunk/core/src/com/orangelabs/rcs/core/ims/protocol/rtp/codec/video/h264/H264RtpHeaders.java?r=275
        
        //C++ http://svn.pjsip.org/repos/pjproject/trunk/pjmedia/src/pjmedia-codec/h264_packetizer.c

        public class RFC6184Headers
        {
            /**
             * AVC NAL picture parameter
             */
            public static int AVC_NALTYPE_FUA = 28;

            private static int FU_INDICATOR_SIZE = 1;
            private static int FU_HEADER_SIZE = 1;

            /**
             * First Header - The FU indicator octet
             */
            private bool FUI_F;
            private int FUI_NRI;
            private byte FUI_TYPE;

            /**
             * Second Header - The FU header
             */
            private bool FUH_S;
            private bool FUH_E;
            private bool FUH_R;
            private byte FUH_TYPE;

            private bool hasFUHeader;

            public RFC6184Headers(byte[] rtpPacketData)
            {
                // Get FU indicator
                byte data_FUI = rtpPacketData[0];

                this.FUI_F = ((data_FUI >> 7) & 0x01) != 0;
                this.FUI_NRI = ((data_FUI >> 5) & 0x07);
                this.FUI_TYPE = (byte)(data_FUI & 0x1f);
                this.hasFUHeader = false;

                if (FUI_TYPE == AVC_NALTYPE_FUA)
                {
                    // Get FU header
                    byte data_FUH = rtpPacketData[1];
                    this.FUH_S = (data_FUH & 0x80) != 0;
                    this.FUH_E = (data_FUH & 0x40) != 0;
                    this.FUH_R = (data_FUH & 0x20) != 0;
                    this.FUH_TYPE = (byte)(data_FUH & 0x1f);
                    this.hasFUHeader = true;
                }
            }

            /**
            * Is Frame Non Interleaved
            *
            * @return Is Frame Non Interleaved
            */
            public bool isFrameNonInterleaved()
            { // not fragmented
                return (FUI_TYPE == AVC_NALTYPE_FUA);
            }

            /**
             * Header Size
             *
             * @return Header Size
             */
            public int getHeaderSize()
            {
                int headerSize = FU_INDICATOR_SIZE;
                if (hasFUHeader)
                {
                    headerSize += FU_HEADER_SIZE;
                }
                return headerSize;
            }

            /**
             * Get NAL Header
             *
             * @return NAL Header
             */
            public byte getNALHeader()
            {
                // Compose and copy NAL header
                if (hasFUHeader)
                {
                    return (byte)(((getFUI_F() ? 1 : 0) << 7) | (FUI_NRI << 5) | (FUH_TYPE & 0x1F));
                }
                else
                {
                    return (byte)(((getFUI_F() ? 1 : 0) << 7) | (FUI_NRI << 5) | (FUI_TYPE & 0x1F));
                }
            }

            /**
             * Verifies if packet is a code slice of a IDR picture
             *
             * @param packet packet to verify
             * @return <code>True</code> if it is, <code>false</code> otherwise
             */
            public bool isIDRSlice()
            {
                if (FUI_TYPE == (byte)0x05)
                {
                    return true;
                }

                if (isFrameNonInterleaved() && FUH_TYPE == (byte)0x05)
                {
                    return true;
                }

                return false;
            }

            /**
             * Verifies if packet is a code slice of a NON IDR picture
             *
             * @param packet packet to verify
             * @return <code>True</code> if it is, <code>false</code> otherwise
             */
            public bool isNonIDRSlice()
            {
                if (FUI_TYPE == (byte)0x01)
                {
                    return true;
                }

                if (isFrameNonInterleaved() && FUH_TYPE == (byte)0x01)
                {
                    return true;
                }

                return false;
            }

            /**
             * Get FUI_F
             *
             * @return FUI_F
             */
            public bool getFUI_F()
            {
                return FUI_F;
            }

            /**
             * Get FUI_NRI
             *
             * @return FUI_NRI
             */
            public int getFUI_NRI()
            {
                return FUI_NRI;
            }

            /**
             * Get FUI_TYPE
             *
             * @return FUI_TYPE
             */
            public byte getFUI_TYPE()
            {
                return FUI_TYPE;
            }

            /**
             * Get FUH_S
             *
             * @return FUH_S
             */
            public bool getFUH_S()
            {
                return FUH_S;
            }

            /**
             * Get FUH_E
             *
             * @return FUH_E
             */
            public bool getFUH_E()
            {
                return FUH_E;
            }

            /**
             * Get FUH_R
             *
             * @return FUH_R
             */
            public bool getFUH_R()
            {
                return FUH_R;
            }

            /**
             * Get FUH_TYPE
             *
             * @return FUH_TYPE
             */
            public byte getFUH_TYPE()
            {
                return FUH_TYPE;
            }
        }

        //Todo NalUnitReader, AVCC and Annex b
        public class NalUnitHeader
        {

            public enum NalUnitType
            {

                RESERVED,
                CODE_SLICE_NON_IDR_PICTURE,
                CODE_SLICE_DATA_PARTITION_A,
                CODE_SLICE_DATA_PARTITION_B,
                CODE_SLICE_DATA_PARTITION_C,
                CODE_SLICE_IDR_PICTURE,
                SEQUENCE_PARAMETER_SET,
                PICTURE_PARAMETER_SET,
                STAP_A,
                STAP_B,
                MTAP16,
                MTAP24,
                FU_A,
                FU_B,
                OTHER_NAL_UNIT

            }

            /**
             * Forbidden zero bit
             */
            private bool forbiddenZeroBit;

            /**
             * NAL Reference id
             */
            private int nalRefId;

            /**
             * NAL Unit Type
             */
            private NalUnitType decodeNalUnitType;

            /**
             * Class constructor
             *
             * @param forbiddenZeroBit Forbidden zero bit
             * @param nalRefId NAL Reference id
             * @param nalUnitType NAL Unit Type value
             */
            private NalUnitHeader(bool forbiddenZeroBit, int nalRefId, int nalUnitType)
            {
                this.forbiddenZeroBit = forbiddenZeroBit;
                this.nalRefId = nalRefId;
                this.decodeNalUnitType = (NalUnitType)nalUnitType;
            }

            /**
             * Checks if the Forbidden Zero Bit is set.
             *
             * @return <code>True</code> if it is, <code>false</code> false otherwise.
             */
            public bool isForbiddenBitSet()
            {
                return forbiddenZeroBit;
            }

            /**
             * Gets the NAL Reference ID
             *
             * @return NAL Reference ID
             */
            public int getNalRefId()
            {
                return nalRefId;
            }

            /**
             * Gets the NAL Unit Type
             *
             * @return
             */
            public NalUnitType getNalUnitType()
            {
                return decodeNalUnitType;
            }

            /**
             * Verifies if the H264 packet is Single NAL Unit
             *
             * @return <code>True</code> if it is, <code>false</code> false otherwise.
             */
            public bool isSingleNalUnitPacket()
            {
                return decodeNalUnitType == NalUnitType.CODE_SLICE_IDR_PICTURE
                        || decodeNalUnitType == NalUnitType.CODE_SLICE_NON_IDR_PICTURE
                        || decodeNalUnitType == NalUnitType.CODE_SLICE_DATA_PARTITION_A
                        || decodeNalUnitType == NalUnitType.CODE_SLICE_DATA_PARTITION_B
                        || decodeNalUnitType == NalUnitType.CODE_SLICE_DATA_PARTITION_C
                        || decodeNalUnitType == NalUnitType.SEQUENCE_PARAMETER_SET
                        || decodeNalUnitType == NalUnitType.PICTURE_PARAMETER_SET
                        || decodeNalUnitType == NalUnitType.OTHER_NAL_UNIT;
            }

            /**
             * Verifies if the H264 packet is an Aggregation Packet
             *
             * @return <code>True</code> if it is, <code>false</code> false otherwise.
             */
            public bool isAggregationPacket()
            {
                return decodeNalUnitType == NalUnitType.STAP_A || decodeNalUnitType == NalUnitType.STAP_B
                        || decodeNalUnitType == NalUnitType.MTAP16
                        || decodeNalUnitType == NalUnitType.MTAP24;
            }

            /**
             * Verifies if the H264 packet is a Fragmentation Unit Packet
             *
             * @return <code>True</code> if it is, <code>false</code> false otherwise.
             */
            public bool isFragmentationUnit()
            {
                return decodeNalUnitType == NalUnitType.FU_A || decodeNalUnitType == NalUnitType.FU_B;
            }

            /**
             * Extracts the NAL Unit header from a H264 Packet
             *
             * @param h264Packet H264 Packet
             * @return {@link NalUnitHeader} Extracted NAL Unit Header
             * @throws {@link RuntimeException} If the H264 packet data is null
             */
            public static NalUnitHeader extract(byte[] h264Packet)
            {
                if (h264Packet == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid H264 packet");
                }

                NalUnitHeader header = new NalUnitHeader(false, 0, 0);
                extract(h264Packet, header);

                return header;
            }

            /**
             * Extracts the NAL Unit header from a H264 Packet. Puts the extracted info
             * in the given header object
             *
             * @param h264Packet H264 packet
             * @param header Header object to fill with data
             * @throws {@link RuntimeException} If the H264 packet data is null or the
             *         header is null;
             */
            public static void extract(byte[] h264Packet, NalUnitHeader header)
            {
                if (h264Packet == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid H264 packet");
                }

                if (header == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid header packet");
                }

                byte headerByte = h264Packet[0];

                header.forbiddenZeroBit = ((headerByte & 0x80) >> 7) != 0;
                header.nalRefId = ((headerByte & 0x60) >> 5);
                int nalUnitType = (headerByte & 0x1f);
                header.decodeNalUnitType = (NalUnitType)nalUnitType;
            }

            /**
             * Extracts the NAL Unit header from a H264 Packet
             *
             * @param h264Packet H264 Packet
             * @return {@link NalUnitHeader} Extracted NAL Unit Header
             * @throws {@link RuntimeException} If the H264 packet data is null
             */
            public static NalUnitHeader extract(int position, byte[] h264Packet)
            {
                if (h264Packet == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid H264 packet");
                }

                NalUnitHeader header = new NalUnitHeader(false, 0, 0);
                extract(position, h264Packet, header);

                return header;
            }

            /**
             * Extracts the NAL Unit header from a H264 Packet. Puts the extracted info
             * in the given header object
             *
             * @param h264Packet H264 packet
             * @param header Header object to fill with data
             * @throws {@link RuntimeException} If the H264 packet data is null or the
             *         header is null;
             */
            public static void extract(int position, byte[] h264Packet, NalUnitHeader header)
            {
                if (h264Packet == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid H264 packet");
                }

                if (header == null)
                {
                    throw new Exception("Cannot extract H264 header. Invalid header packet");
                }

                byte headerByte = h264Packet[position];

                header.forbiddenZeroBit = ((headerByte & 0x80) >> 7) != 0;
                header.nalRefId = ((headerByte & 0x60) >> 5);
                int nalUnitType = (headerByte & 0x1f);
                header.decodeNalUnitType = (NalUnitType)nalUnitType;
            }
        }       

        //To Make Packets
        //https://code.google.com/p/android-rcs-ims-stack/source/browse/trunk/core/src/com/orangelabs/rcs/core/ims/protocol/rtp/codec/video/h264/JavaPacketizer.java?r=275

        //To De Packetize
        //https://code.google.com/p/android-rcs-ims-stack/source/browse/trunk/core/src/com/orangelabs/rcs/core/ims/protocol/rtp/codec/video/h264/JavaDepacketizer.java?r=275

        //Some MP4 Related stuff
        //https://github.com/fyhertz/libstreaming/blob/master/src/net/majorkernelpanic/streaming/mp4/MP4Parser.java

        //C# h264 elementary stream stuff
        //https://bitbucket.org/jerky/rtp-streaming-server

        /// <summary>
        /// Handles the creation of Stap and Frag packets from a large nal as well the creation of single large nals from Stap and Frag
        /// </summary>
        /// <todo>
        /// Needs a Nal class
        /// </todo>
        public class RFC6184Frame : Rtp.RtpFrame
        {

            public RFC6184Frame(byte payloadType) : base(payloadType) { }

            public void Packetize(byte[] nal)
            {

                if (nal.Length > 1500)
                {
                    //Make a Fragment
                }

                //Add RtpPackets for fragments / data
            }

            internal System.IO.MemoryStream Buffer { get; set; }

            //PrepareBuffer
            public void Depacketize()
            {

                List<byte[]> NalUnits = new List<byte[]>();

                foreach (Rtp.RtpPacket packet in m_Packets.Values.Distinct())
                {

                    RFC6184Headers headers = new RFC6184Headers(packet.Coefficients.ToArray());

                    //Forbidden
                    if (headers.getFUI_F()) continue;

                    byte nalHeader = headers.getNALHeader();

                    int offset = headers.getHeaderSize(), length = packet.Coefficients.Count() - offset, posSeq = packet.SequenceNumber;

                    // Fragmentation Units (FU-A) have NALs separated through several
                    // RTP packets
                    if (headers.getFUI_TYPE() == RFC6184Headers.AVC_NALTYPE_FUA)
                    {

                        bool hasStart = headers.getFUH_S(), hasEnd = headers.getFUH_E();
                    }

                    NalUnits.Add(packet.Coefficients.Skip(offset).Take(length).ToArray());
                }

                //Write assembled chunks to buffer.
                foreach (var nal in NalUnits) Buffer.Write(nal, 0, nal.Length);
            }

            public override bool Complete
            {
                get
                {
                    if (!base.Complete) return false;


                    //if (!reassembledDataHasStart || !reassembledDataHasEnd)
                    //{
                    //    return false; // has start and end chunk
                    //}

                    // Validate chunk sizes between start and end pos
                    //int posCurrent = reassembledDataPosSeqStart;
                    //while ((posCurrent & VIDEO_DECODER_MAX_PAYLOADS_CHUNKS_MASK) != reassembledDataPosSeqEnd)
                    //{
                    //    // need more data?
                    //    if (reassembledDataSize[posCurrent & VIDEO_DECODER_MAX_PAYLOADS_CHUNKS_MASK] <= 0)
                    //    {
                    //        return false;
                    //    }
                    //    posCurrent++;
                    //}
                    //// Validate last chunk
                    //if (reassembledDataSize[reassembledDataPosSeqEnd] <= 0)
                    //{
                    //    return false;
                    //}

                    // TODO: if some of the last ones come in after the marker, there
                    // will be blank squares in the lower right.
                    return true;
                }
            }

            /*
         // Extracts the NAL Unit Header from the Input Buffer
        extractNalUnitHeader(input);

        if (mNalUnitHeader.isFragmentationUnit()) {
            return handleFragmentationUnitPacket(input, output);
        } else if (mNalUnitHeader.isAggregationPacket()) {
            return handleAggregationPacket(input, output);
        } else {
            return handleSingleNalUnitPacket(input, output);
        }
         */



            /*
             private int handleAggregationPacket(Buffer input, Buffer output) {
        // Get data
        byte[] bufferData = (byte[]) input.getData();
        if (aggregationPositon + 1 >= bufferData.length) {
            // No more data in aggregation packet
            aggregationPositon = 1;
            output.setDiscard(true);
            return BUFFER_PROCESSED_OK;
        }

        // Get NALU size
        int nalu_size = ((bufferData[aggregationPositon] << 8) | bufferData[aggregationPositon+1]);
        aggregationPositon+=2;
        if (aggregationPositon + nalu_size > bufferData.length) {
            // Not a correct packet
            aggregationPositon = 1;
            return BUFFER_PROCESSED_FAILED;
        }

        // Get NALU HDR
        extractNalUnitHeader(aggregationPositon, input);
        if (mNalUnitHeader.isSingleNalUnitPacket()) {
            // Create output buffer
            byte[] data = new byte[nalu_size];
            System.arraycopy(bufferData, aggregationPositon, data, 0, nalu_size);
            aggregationPositon+=nalu_size;

            // Set buffer
            output.setData(data);
            output.setLength(data.length);
            output.setOffset(0);
            output.setTimeStamp(input.getTimeStamp());
            output.setSequenceNumber(input.getSequenceNumber());
            output.setVideoOrientation(input.getVideoOrientation());
            output.setFormat(input.getFormat());
            output.setFlags(input.getFlags());

            return INPUT_BUFFER_NOT_CONSUMED;
        } else {
            // Not a correct packet
            aggregationPositon = 1;
            return BUFFER_PROCESSED_FAILED;
        }
    }
             * 
             * 
             * 
              private int handleSingleNalUnitPacket(Buffer input, Buffer output) {
        // Create output buffer
        byte[] bufferData = (byte[]) input.getData();
        int bufferDataLength = bufferData.length;
        byte[] data = new byte[bufferDataLength];
        System.arraycopy(bufferData, 0, data, 0, bufferDataLength);

        // Set buffer
        output.setData(data);
        output.setLength(data.length);
        output.setOffset(0);
        output.setTimeStamp(input.getTimeStamp());
        output.setSequenceNumber(input.getSequenceNumber());
        output.setVideoOrientation(input.getVideoOrientation());
        output.setFormat(input.getFormat());
        output.setFlags(input.getFlags());

        return BUFFER_PROCESSED_OK;
    }
             * 
             * 
             * 
             * private int handleFragmentationUnitPacket(Buffer input, Buffer output) {
        if (!input.isDiscard()) {
            assemblersCollection.put(input);
            if (assemblersCollection.getLastActiveAssembler().complete()) {
                assemblersCollection.getLastActiveAssembler().copyToBuffer(output);
                assemblersCollection.removeOldestThan(input.getTimeStamp());
                return BUFFER_PROCESSED_OK;
            } else {
                output.setDiscard(true);
                return OUTPUT_BUFFER_NOT_FILLED;
            }
        } else {
            output.setDiscard(true);
            return OUTPUT_BUFFER_NOT_FILLED;
        }
    }

             */

            //To go to an Image...
            //Look for a SliceHeader in the Buffer
            //Decode Macroblocks in Slice
            //Convert Yuv to Rgb

        }

        #region Propeties

        //Should be created dynamically

        //http://www.cardinalpeak.com/blog/the-h-264-sequence-parameter-set/

        //TODO, Use a better starting point e.g. https://github.com/jordicenzano/h264simpleCoder/blob/master/src/CJOCh264encoder.h or the OpenH264 stuff @ https://github.com/cisco/openh264

        byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 },
            slice_header1 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x84, 0x21, 0xa0 },
            slice_header2 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x94, 0x21, 0xa0 };

        bool useSliceHeader1 = true;
        
        byte[] macroblock_header = { 0x0d, 0x00 };

        #endregion

        #region Constructor

        public RFC6184Stream(int width, int height, string name, string directory = null, bool watch = true)
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

        //SourceStream Implementation
        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove JPEG Track
            SessionDescription.RemoveMediaDescription(0);

            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, RtpSource.RtpMediaProtocol, 96));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 H264/90000"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=fmtp:96 profile-level-id="+ Common.Binary.ReadU24(sps, 4, false).ToString("X2") +";sprop-parameter-sets=" + Convert.ToBase64String(sps, 4, sps.Length  - 4) + ',' + Convert.ToBase64String(pps, 4, pps.Length - 4)));
        }


        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public override void Packetize(System.Drawing.Image image)
        {
            lock (m_Frames)
            {
                try
                {
                    //Make the width and height correct
                    using (image = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero))
                    {
                        //Create a new frame
                        var newFrame = new RFC6184Frame(96);

                        //Convert the bitmap to yuv420
                        byte[] yuv = Utility.ABGRA2YUV420Managed((Bitmap)image);

                        List<IEnumerable<byte>> macroBlocks = new List<IEnumerable<byte>>();

                        //For each h264 Macroblock in the frame
                        for (int i = 0; i < Height / 16; i++)
                            for (int j = 0; j < Width / 16; j++)
                                macroBlocks.Add(EncodeMacroblock(i, j, yuv)); //Add an encoded macroblock to the list

                        macroBlocks.Add(new byte[] { 0x80 });//Stop bit (Wasteful by itself)

                        int seq = 0;

                        IEnumerable<byte> packetData = Utility.Empty;

                        //Build the RtpPacket data from the MacroBlocks
                        foreach (IEnumerable<byte> macroBlock in macroBlocks)
                        {
                            //If there is more data then allowed in the packet
                            if (packetData.Count() > 1024)
                            {

                                //A Fragment Unit Header is probably required here to indicate the NAL is fragmented
                                if (newFrame.Count == 0)
                                {
                                    packetData = packetData.Concat(new byte[] { (byte)(0x28 & 0x1f), (byte)(1 << 7 | (0x28 & 0x1f)) });
                                }
                                else
                                {
                                    //Should probably set End bit in the last packet 1 << 6
                                    packetData = packetData.Concat(new byte[]{ (byte)(0x28 & 0x1f), (byte)(0x28 & 0x1f) });
                                }

                                //Add a packet to the frame with the existing data
                                newFrame.Add(new Rtp.RtpPacket(2, false, false, false, 96, 0, sourceId, ++seq, 0, packetData.ToArray()));

                                //reset the data for the next packet
                                packetData = Utility.Empty;
                            }
                            
                            //If this was the first packet added include the sps and pps in band
                            if (newFrame.Count == 0)
                            {
                                packetData = packetData.Concat(sps.Concat(pps).Concat(useSliceHeader1 ? slice_header1 : slice_header2).Concat(macroBlock));

                                //Alternate slice headers for next frame
                                useSliceHeader1 = !useSliceHeader1;
                            }
                            else //Otherwise just use the macroBlock
                            {
                                packetData = packetData.Concat(macroBlock);
                            }
                        }

                        //Set the marker in the RtpHeader
                        newFrame.Last().Marker = true;

                        //Set the last bit in the Nal header, Forbidden bit already set
                        newFrame.Last().Payload[newFrame.Last().NonPayloadOctets + 1] |= 1 << 6;

                        //Add the frame
                        AddFrame(newFrame);

                        yuv = null;

                        macroBlocks.Clear();

                        macroBlocks = null;
                    }
                }
                catch { throw; }
            }
        }

        //Thanks !!
        //http://www.cardinalpeak.com/blog/worlds-smallest-h-264-encoder/

        IEnumerable<byte> EncodeMacroblock(int i, int j, byte[] data)
        {

            IEnumerable<byte> result = Utility.Empty;

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
            result = result.Concat(data.Skip(yIndex ).Take(16 * 8));

            //Take the Chroma Values
            result = result.Concat(data.Skip(uIndex ).Take(8 * 8));

            result = result.Concat(data.Skip(vIndex ).Take(8 * 8));

            return result;
        }

        #endregion
    }
}