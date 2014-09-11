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

        //https://code.google.com/p/android-rcs-ims-stack/source/browse/trunk/core/src/com/orangelabs/rcs/core/ims/protocol/rtp/codec/video/h264/H264RtpHeaders.java?r=275
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

        public class RFC6184Frame : Rtp.RtpFrame
        {

            public RFC6184Frame(byte payloadType) : base(payloadType) { }

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
              public boolean complete() {

            if (!rtpMarker) {
                return false; // need an rtp marker to signify end
            }

            if (!reassembledDataHasStart || !reassembledDataHasEnd) {
                return false; // has start and end chunk
            }

            // Validate chunk sizes between start and end pos
            int posCurrent = reassembledDataPosSeqStart;
            while ((posCurrent & VIDEO_DECODER_MAX_PAYLOADS_CHUNKS_MASK) != reassembledDataPosSeqEnd) {
                // need more data?
                if (reassembledDataSize[posCurrent & VIDEO_DECODER_MAX_PAYLOADS_CHUNKS_MASK] <= 0) {
                    return false;
                }
                posCurrent++;
            }
            // Validate last chunk
            if (reassembledDataSize[reassembledDataPosSeqEnd] <= 0) {
                return false;
            }

            // TODO: if some of the last ones come in after the marker, there
            // will be blank squares in the lower right.
            return true;
        }

             */

        }

        #region Propeties

        //http://www.cardinalpeak.com/blog/the-h-264-sequence-parameter-set/

        byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 };
        
        byte[] macroblock_header = { 0x0d, 0x00 };

        #endregion

        #region Constructor

        public RFC6184Stream(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, 240, 160, false, 99)
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
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=fmtp:96 packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg=="));
        }


        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        /// <param name="quality">The quality of the encoded image, 100 specifies the quantization tables are sent in band</param>
        public override void Packetize(System.Drawing.Image image)
        {
            lock (m_Frames)
            {
                try
                {
                    //Make the width and height correct
                    using (image = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero))
                    {
                        //Encode a JPEG (leave 40 bytes room in each packet for the h264 data)

                        //TODO Take RGB Stride and Convert to YUV

                        using (var jpegFrame = RFC2435Stream.RFC2435Frame.Packetize(image, Math.Max(99, Quality), Interlaced, (int)sourceId, 0, 0, 1300))
                        {
                            //Store the payload which is YUV Planar (420, 421, 422)
                            List<byte[]> data = new List<byte[]>();

                            //Create a new frame
                            var newFrame = new Rtp.RtpFrame(96);
                            
                            //project everything to a single array
                            byte[] yuv = jpegFrame.Assemble(false, 8).ToArray();

                            //Todo NAL Headers

                            //For each h264 Macroblock in the frame
                            for (int i = 0; i < Width / 16; i++)
                                for (int j = 0; j < Height / 16; j++)
                                    data.Add(EncodeMacroblock(i, j, yuv)); //Add a macroblock

                            data.Add(new byte[] { 0x80 });//Stop bit

                            int seq = 0;

                            foreach (byte[] b in data)
                            {
                                if (newFrame.Count == 0)
                                {
                                    newFrame.Add(new Rtp.RtpPacket(new Rtp.RtpHeader(2, false, false, false, 96, 0, 0, seq++, 0), slice_header.Concat(b)));
                                }
                                else
                                {
                                    newFrame.Add(new Rtp.RtpPacket(new Rtp.RtpHeader(2, false, false, false, 96, 0, 0, seq++, 0), b));
                                }
                            }

                            newFrame.Last().Marker = true;

                            yuv = null;

                            AddFrame(newFrame);
                        }
                    }
                }
                catch { throw; }
            }
        }

        //Thanks !!
        //http://www.cardinalpeak.com/blog/worlds-smallest-h-264-encoder/

        byte[] EncodeMacroblock(int i, int j, byte[] data)
        {

            IEnumerable<byte> result = Utility.Empty;

            int x, y;

            if (!((i == 0) && (j == 0))) result = macroblock_header;

            for (x = i * 16; x < (i + 1) * 16; x++)
                for (y = j * 16; y < (j + 1) * 16; y++)
                    result = result.Concat(data.Skip(x + y + 1).Take(1)); //fwrite(&frame.Y[x][y], 1, 1, stdout);
            for (x = i * 8; x < (i + 1) * 8; x++)
                for (y = j * 8; y < (j + 1) * 8; y++)
                    result = result.Concat(data.Skip(Width * Height + x * y).Take(1)); //fwrite(&frame.Cb[x][y], 1, 1, stdout);
            for (x = i * 8; x < (i + 1) * 8; x++)
                for (y = j * 8; y < (j + 1) * 8; y++)
                    result = result.Concat(data.Skip(Width * Height + x * y + 1).Take(1));//fwrite(&frame.Cr[x][y], 1, 1, stdout);

            return result.ToArray();
        }

        #endregion
    }
}