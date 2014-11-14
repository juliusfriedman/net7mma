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
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6416">RFC6416</see> which is used for MPEG-4 Encoded video.
    /// </summary>
    public class RFC6416Media : RFC2435Media //RtpSink
    {
        public class RFC6416Frame : Rtp.RtpFrame
        {
            static byte[] StartCode = new byte[] { 0x00, 0x00, 0x01 };

            static byte VisalObjectSequenceStart = 0xB0;

            public RFC6416Frame(byte payloadType) : base(payloadType) { }

            public RFC6416Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC6416Frame(RFC6416Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] nal, int mtu = 1500)
            {
                if (nal == null) return;

                int nalLength = nal.Length;

                //May have length in front ? (AVC Nal)
                //if(nal[0] == 0x01)

                if (nalLength >= mtu)
                {
                    int offset = 0;

                    bool marker = false;

                    while (offset < nalLength)
                    {
                        //Set the end bit if no more data remains
                        if (offset + mtu > nalLength) marker = true;
                    
                        //Add the packet
                        Add(new Rtp.RtpPacket(2, false, false, marker, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));

                        //Move the offset
                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, true, PayloadTypeByte, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));
            }

            /// <summary>
            /// Write the required prerequesite data to the Buffer along with the Video Frame.
            /// </summary>
            /// <param name="profileLevelId">The profileLevelId (if not 1)</param>
            public void Depacketize(byte profileLevelId = 1)
            {
                DisposeBuffer();

                if (profileLevelId < 1) throw new ArgumentException("Must be a valid Mpeg 4 Profile Level Id with a value >= 1", "profileLevelId");

                /*
                  Example usages for the "profile-level-id" parameter are:
                  1  : MPEG-4 Visual Simple Profile/Level 1
                  34 : MPEG-4 Visual Core Profile/Level 2
                  145: MPEG-4 Visual Advanced Real Time Simple Profile/Level 1
                 */

                Buffer = new MemoryStream(StartCode.Concat(VisalObjectSequenceStart.Yield()).Concat(profileLevelId.Yield()).Concat(StartCode).Concat(Assemble()).ToArray());
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
        }

        #region Constructor

        public RFC6416Media(int width, int height, string name, string directory = null, bool watch = true)
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

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 MP4V-ES/90000"));
            //Should be a field set in constructor.
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("fmtp:96 profile-level-id=1"));

            m_RtpClient.Add(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));
        }

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public override void Packetize(System.Drawing.Image image)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}