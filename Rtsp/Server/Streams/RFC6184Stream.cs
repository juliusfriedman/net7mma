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
        #region Propeties

        public readonly int Width = 240, Height = 160;

        //http://www.cardinalpeak.com/blog/the-h-264-sequence-parameter-set/

        byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 };
        
        byte[] macroblock_header = { 0x0d, 0x00 };

        #endregion

        #region Constructor

        public RFC6184Stream(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch)
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
        public override void Packetize(System.Drawing.Image image, int quality = 50, bool interlaced = false)
        {
            lock (m_Frames)
            {
                try
                {
                    //Make the width and height correct
                    using (image = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero))
                    {
                        //Encode a JPEG (leave 40 bytes room in each packet for the h264 data)
                        using (var jpegFrame = RFC2435Stream.RFC2435Frame.Packetize(image, quality, interlaced, (int)sourceId, 0, 0, 1300))
                        {
                            //Store the payload which is YUV Planar (420, 421, 422)
                            List<byte[]> data = new List<byte[]>();

                            //Create a new frame
                            var newFrame = new Rtp.RtpFrame(96);
                            
                            //project everything to a single array
                            byte[] yuv = jpegFrame.Assemble(false, 8).ToArray();

                            //For each h264 Macroblock in the frame
                            for (int i = 0; i < Width / 16; i++)
                                for (int j = 0; j < Height / 16; j++)
                                    data.Add(Macroblock(i, j, yuv)); //Add a macroblock

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

        byte[] Macroblock(int i, int j, byte[] data)
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