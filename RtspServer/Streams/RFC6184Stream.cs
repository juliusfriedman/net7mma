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
    /// RFC6184 H.264
    /// </summary>
    public class RFC6184Stream : RFC2435Stream
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

                if (nalLength >= mtu)
                {
                    int offset = 0;

                    //Make a Fragment Indicator with start bit
                    byte[] FUI = new byte[] { (byte)(1 << 7), 0x00 };

                    while (offset < nalLength)
                    {

                        //Set the end bit
                        if (offset + mtu > nalLength)
                        {
                            FUI[0] |= (byte)(1 << 6);
                        }
                        else
                        {
                            //No Start, No End
                            FUI[0] = 0;
                        }

                        Add(new Rtp.RtpPacket(2, false, false, false, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, FUI.Concat(nal.Skip(offset).Take(mtu)).ToArray()));

                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, false, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));
            }

            public void Depacketize()
            {

                DisposeBuffer();

                Buffer = new MemoryStream();

                int offset = 0, count = 0;

                foreach (Rtp.RtpPacket packet in m_Packets.Values.Distinct())
                {
                    if (packet.Payload.Count <= 2) continue;

                    offset = 0;

                    count = packet.Payload.Count;

                    byte[] packetData = packet.Coefficients.ToArray();

                    byte nal_type = (byte)(packetData[offset++] & Common.Binary.FourBitMaxValue);

                    //Single unit Nal
                    if (nal_type >= 1 && nal_type <= 23)
                    {

                        // 7 and 8 are SPS and PPS

                        Buffer.Write(NalStart, 0, 3);

                        Buffer.Write(packetData, 0, count);
                    }
                    else if (nal_type == 24 || nal_type == 25 || nal_type == 26 || nal_type == 27) //STAP - A or STAP - B or MTAP - 16 or MTAP 24
                    {
                        int tmp_nal_size = count;

                        //EAT DON (check if this is the correct place to eat the don)
                        if (nal_type == 26 || nal_type == 27) offset += 2;

                        while (offset + tmp_nal_size < count)
                        {
                            Buffer.Write(NalStart, 0, 3);

                            tmp_nal_size = (byte)(packetData[offset++] << 8 | packetData[offset++]);

                            Buffer.Write(packetData, offset, tmp_nal_size);

                            offset += tmp_nal_size;
                            
                            //check type again?

                        }
                    }
                    else if (nal_type == 28 || nal_type == 29) //FU - A or FU - B
                    {

                        bool Start = (packetData[offset] & 0x80) > 0, End = (packetData[offset] & 0x40) > 0;

                        //byte Type = (byte)(packetData[offset] & Common.Binary.FourBitMaxValue),
                        //    NRI = (byte)((packetData[offset] & 0x60) >> 5);

                        offset += 2;

                        //EAT DON
                        if (nal_type == 29) offset += 2;

                        int fragment_size = count - offset;

                        if (fragment_size > 0)
                        {
                            if (Start) Buffer.Write(NalStart, 0, 3);

                            Buffer.Write(packetData, offset, fragment_size);
                        }
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

        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove JPEG Track
            SessionDescription.RemoveMediaDescription(0);

            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Media.Rtp.RtpClient.RtpAvpProfileIdentifier, 96));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 H264/90000"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=fmtp:96 profile-level-id="+ Common.Binary.ReadU24(sps, 4, false).ToString("X2") +";sprop-parameter-sets=" + Convert.ToBase64String(sps, 4, sps.Length  - 4) + ',' + Convert.ToBase64String(pps, 4, pps.Length - 4)));
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
                        byte[] yuv = Utility.ABGRA2YUV420Managed(data.Width, data.Height, data.Scan0);

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