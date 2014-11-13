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

namespace Media.Rtsp.Server.Media
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6184">RFC6184</see> which is used for H.264 Encoded video.
    /// </summary>
    public class RFC6184Media : RFC2435Media //RtpSink
    {
        //Some MP4 Related stuff
        //https://github.com/fyhertz/libstreaming/blob/master/src/net/majorkernelpanic/streaming/mp4/MP4Parser.java

        //C# h264 elementary stream stuff
        //https://bitbucket.org/jerky/rtp-streaming-server

        /// <summary>
        /// Handles the creation of Stap and Frag packets from a large nal as well the creation of single large nals from Stap and Frag
        /// </summary>
        public class RFC6184Frame : Rtp.RtpFrame
        {
            /// <summary>
            /// Emulation Prevention
            /// </summary>
            static byte[] NalStart = { 0x00, 0x00, 0x01 };

            public RFC6184Frame(byte payloadType) : base(payloadType) { }

            public RFC6184Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC6184Frame(RFC6184Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] nal, int mtu = 1500)
            {
                if (nal == null) return;

                int nalLength = nal.Length;

                int offset = 0;

                //No Start Codes (May contain length of nal?)
                if (nal[3] == 1)
                {
                    offset += 3;
                    nalLength -= 3;
                }

                if (nalLength >= mtu)
                {
                    //Make a Fragment Indicator with start bit
                    byte[] FUI = new byte[] { (byte)(1 << 7), 0x00 };

                    bool marker = false;

                    while (offset < nalLength)
                    {
                        //Set the end bit if no more data remains
                        if (offset + mtu > nalLength)
                        {
                            FUI[0] |= (byte)(1 << 6);
                            marker = true;
                        }
                        else if (offset > 0) //For packets other than the start
                        {
                            //No Start, No End
                            FUI[0] = 0;
                        }

                        //Add the packet
                        Add(new Rtp.RtpPacket(2, false, false, marker, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, FUI.Concat(nal.Skip(offset).Take(mtu)).ToArray()));

                        //Move the offset
                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, true, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal.Skip(offset).ToArray()));
            }

            public void Depacketize() { bool sps, pps, sei; Depacketize(out sps, out pps, out sei); }

            public void Depacketize(out bool containsSps, out bool containsPps, out bool containsSei)
            {
                containsSps = containsPps = containsSei = false;
                DisposeBuffer();

                MemoryStream Buffer = new MemoryStream();

                int offset = 0, count = 0;

                //Get all packets in the frame
                foreach (Rtp.RtpPacket packet in m_Packets.Values.Distinct())
                {
                    //Starting at offset 0
                    offset = 0;

                    //Obtain the data of the packet (without source list or padding)
                    byte[] packetData = packet.Coefficients.ToArray();

                    //Cache the length
                    count = packetData.Length;

                    //Must have at least 2 bytes
                    if (count <= 2) continue;

                    //Determine if the forbidden bit is set and the type of nal from the first byte
                    byte firstByte = packetData[offset];

                    //bool forbiddenZeroBit = ((firstByte & 0x80) >> 7) != 0;

                    byte nalUnitType = (byte)(firstByte & Common.Binary.FiveBitMaxValue);

                    //o  The F bit MUST be cleared if all F bits of the aggregated NAL units are zero; otherwise, it MUST be set.
                    //if (forbiddenZeroBit && nalUnitType <= 23 && nalUnitType > 29) throw new InvalidOperationException("Forbidden Zero Bit is Set.");

                    //Determine what to do
                    switch (nalUnitType)
                    {
                        //Ignores
                        case 0:
                        case 30:
                        case 31:
                            continue; //Next Packet
                        case 24: //STAP - A
                        case 25: //STAP - B
                        case 26: //MTAP - 16
                        case 27: //MTAP - 24
                            {
                                //Todo Determine if need to Order by DON first.
                                //EAT DON for ALL BUT STAP - A
                                if (nalUnitType != 24) offset += 2;

                                //Consume the rest of the data from the packet
                                while (offset < count)
                                {
                                    //Determine the nal size (RFC Indicates that Size might include the DOND and TSOFFSET when present)
                                    int tmp_nal_size = Common.Binary.Read16(packetData, offset, BitConverter.IsLittleEndian);
                                    offset += 2;

                                    //Determine if MTAPs Require that DOND and TS OFFSET be removed.

                                    //If the nal had data then write it
                                    if (tmp_nal_size > 0)
                                    {
                                        //int headerOffset = offset;

                                        //For DOND and TSOFFSET
                                        switch (nalUnitType)
                                        {
                                            case 25:// MTAP - 16
                                                {
                                                    //headerOffset += 3;
                                                    offset += 3;
                                                    goto default;
                                                }
                                            case 26:// MTAP - 24
                                                {
                                                    //headerOffset += 4;
                                                    offset += 4;
                                                    goto default;
                                                }
                                            default:
                                                {
                                                    //byte nalHeader = (byte)(packetData[headerOffset] & Common.Binary.FiveBitMaxValue);
                                                    byte nalHeader = (byte)(packetData[offset] & Common.Binary.FiveBitMaxValue);

                                                    if (nalHeader > 5)
                                                    {
                                                        //Could have been SPS / PPS / SEI
                                                        if (!containsSei && nalHeader == 6) containsSei = true;

                                                        if (!containsPps && nalHeader == 7) containsPps = true;

                                                        if (!containsSps && nalHeader == 8) containsSps = true;
                                                    }

                                                    break;
                                                }
                                        }

                                        //Write the start code
                                        Buffer.Write(NalStart, 0, 3);

                                        //Write the nal data
                                        Buffer.Write(packetData, offset, tmp_nal_size);

                                        //Move the offset
                                        offset += tmp_nal_size;
                                    }
                                }

                                //Next Packet
                                continue;
                            }
                        case 28: //FU - A
                        case 29: //FU - B
                            {
                        /*
                         Informative note: When an FU-A occurs in interleaved mode, it
                         always follows an FU-B, which sets its DON.
                         * Informative note: If a transmitter wants to encapsulate a single
                          NAL unit per packet and transmit packets out of their decoding
                          order, STAP-B packet type can be used.
                         */
                                //Indicator = firstByte;

                                if (count > 1)
                                {
                                    //Read the Header
                                    byte FUHeader = packetData[++offset];

                                    bool Start = ((FUHeader & 0x80) >> 7) > 0;

                                    //bool End = ((FUHeader & 0x40) >> 6) > 0;

                                    bool Receiver = (FUHeader & 0x20) != 0;

                                    if (Receiver) throw new InvalidOperationException("Receiver Bit Set");

                                    //Move to data
                                    ++offset;

                                    //Todo Determine if need to Order by DON first.
                                    //DON Present in FU - B (Determine if should be kept)
                                    if (nalUnitType == 29) offset += 2;

                                    //Determine the fragment size
                                    int fragment_size = count - offset;

                                    //If the size was valid
                                    if (fragment_size > 0)
                                    {
                                        //If the start bit was set
                                        if (Start)
                                        {
                                            //Write the start code
                                            Buffer.Write(NalStart, 0, 3);

                                            //Reconstruct the nal header
                                            //Use the first 3 bits of the first byte and last 5 bites of the FU Header
                                            byte nalHeader = (byte)((firstByte & 0xE0) | (FUHeader & Common.Binary.FiveBitMaxValue));

                                            //Write the re-construced header
                                            Buffer.WriteByte(nalHeader);

                                            //Could have been SPS / PPS / SEI
                                            if (nalHeader > 5)
                                            {
                                                if (!containsSei && nalHeader == 6) containsSei = true;

                                                if (!containsPps && nalHeader == 7) containsPps = true;

                                                if (!containsSps && nalHeader == 8) containsSps = true;
                                            }
                                        }

                                        //Write the data of the fragment.
                                        Buffer.Write(packetData, offset, fragment_size);
                                    }
                                }

                                //Next Packet
                                continue;
                            }
                        default:
                            {
                                // 6 SEI, 7 and 8 are SPS and PPS
                                if (nalUnitType > 5)
                                {
                                    if (!containsSei && nalUnitType == 6) containsSei = true;

                                    if (!containsPps && nalUnitType == 7) containsPps = true;

                                    if (!containsSps && nalUnitType == 8) containsSps = true;
                                }

                                //Write the start code
                                Buffer.Write(NalStart, 0, 3);

                                //Write the nal data
                                Buffer.Write(packetData, offset, count - offset);

                                //Next Packet
                                continue;
                            }
                    }
                }

                //Assign the buffer
                this.Buffer = Buffer;
            }

            internal void DisposeBuffer()
            {
                if (Buffer != null)
                {
                    Buffer.Dispose();
                    Buffer = null;
                }
            }

            public override void Dispose()
            {
                if (Disposed) return;
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

        byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 },
            slice_header1 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x84, 0x21, 0xa0 },
            slice_header2 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x94, 0x21, 0xa0 };

        bool useSliceHeader1 = true;
        
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

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 96));

            //Add the control line and media attributes to the Media Description
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 H264/90000"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=fmtp:96 profile-level-id=" + Common.Binary.ReadU24(sps, 4, !BitConverter.IsLittleEndian).ToString("X2") + ";sprop-parameter-sets=" + Convert.ToBase64String(sps, 4, sps.Length - 4) + ',' + Convert.ToBase64String(pps, 4, pps.Length - 4)));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));
        }

        //Move to Codec/h264
        //H264Encoder

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
                    using (var thumb = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero))
                    {
                        //Create a new frame
                        var newFrame = new RFC6184Frame(96);


                        //Get RGB Stride
                        System.Drawing.Imaging.BitmapData data = ((System.Drawing.Bitmap)image).LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                                   System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);

                        //Convert the bitmap to yuv420

                        //switch on image.PixelFormat

                        //Utility.YUV2RGBManaged()
                        // Utility.ABGRA2YUV420Managed(image.Width, image.Height, data.Scan0);

                        byte[] yuv = new byte[image.Width * image.Height];

                        ((System.Drawing.Bitmap)image).UnlockBits(data);

                        data = null;

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