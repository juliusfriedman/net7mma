﻿/*
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
using System.Text;

namespace Media.Rtsp.Server.MediaTypes
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6416">RFC6416</see> which is used for MPEG-4 Encoded video. (Formerly RFC3016)
    /// </summary>
    public class RFC6416Media : RFC3640Media
    {
        public class RFC6416Frame : Rtp.RtpFrame
        {

            #region Static

            static readonly Common.MemorySegment StartCodePrefixSegment = new Common.MemorySegment(Media.Containers.Mpeg.StartCodes.StartCodePrefix, false);

            public static Common.MemorySegment CreatePrefixedStartCodeSegment(byte byteCode)
            {
                return new Common.MemorySegment(new byte[] { 0x00, 0x00, 0x01, byteCode });
            }

            #endregion

            #region Constructor

            public RFC6416Frame(byte payloadType) : base(payloadType) { }

            public RFC6416Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC6416Frame(RFC6416Frame f) : base(f, true, true) { }

            #endregion

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
                        Add(new Rtp.RtpPacket(2, false, false, marker, PayloadType, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));

                        //Move the offset
                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, true, PayloadType, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));
            }

            //Should the ObjectType be allowed to be given...? 3 or 5 for Audio?

            //No longer needed
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
                  15 : AAC?
                  34 : MPEG-4 Visual Core Profile/Level 2
                  145: MPEG-4 Visual Advanced Real Time Simple Profile/Level 1
                 */

                m_Buffer = new MemoryStream(Media.Containers.Mpeg.StartCodes.StartCodePrefix. //00 00 01
                    Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.Mpeg4.StartCodes.VisualObjectSequence)).
                    Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(profileLevelId)). // B0 XX (ID)
                    Concat(Media.Containers.Mpeg.StartCodes.StartCodePrefix).Concat(Assemble()).ToArray()); // 00 00 01 XX (DATA)
            }

            public virtual void ProcessPacket(Rtp.RtpPacket packet, byte profileLevelId = 1)
            {
                int addIndex = packet.Timestamp - packet.SequenceNumber; // Depacketized.Count > 0 ? Depacketized.Keys.Last() : 0;

                //byte[] t = new byte[] { profileLevelId, Media.Codecs.Video.Mpeg4.StartCodes.VisualObjectSequence };

                //Common.MemorySegment a = new Common.MemorySegment(t, 0, 1);

                //Common.MemorySegment b = new Common.MemorySegment(t, 1, 1);

                //Creates 4 byte array each time with the first 3 bytes being the same, would need a way to combine MemorySegments for this to be any more efficient.
                Depacketized.Add(addIndex++, CreatePrefixedStartCodeSegment(Media.Codecs.Video.Mpeg4.StartCodes.VisualObjectSequence));

                //Depacketized.Add(addIndex++, StartCodePrefixSegment);

                //Depacketized.Add(addIndex++, b);

                //Or

                //Depacketized.Add(addIndex++, StartCodePrefixSegment);

                //Depacketized.Add(addIndex++, new Common.MemorySegment(new byte[] { Media.Codecs.Video.Mpeg4.StartCodes.VisualObjectSequence }));
                
                //What a waste, 4 bytes + to describe 1
                Depacketized.Add(addIndex++, new Common.MemorySegment(new byte[] { profileLevelId }));

                Depacketized.Add(addIndex++, StartCodePrefixSegment);

                // or

                //Depacketized.Add(addIndex++, a);

                //Depacketized.Add(addIndex++, StartCodePrefixSegment);

                Depacketized.Add(addIndex++, packet.PayloadDataSegment);
            }

            public override void Depacketize(Rtp.RtpPacket packet)
            {
                ProcessPacket(packet, 1);
            }

            //internal void DisposeBuffer()
            //{
            //    if (Buffer != null)
            //    {
            //        Buffer.Dispose();
            //        Buffer = null;
            //    }
            //}

            //public override void Dispose()
            //{
            //    if (IsDisposed) return;
            //    base.Dispose();
            //    DisposeBuffer();
            //}
        }

        #region Constructor

        public RFC6416Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(width, height, name, directory, watch)
        {

        }

        #endregion

        #region Methods

        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove generic MPEG Track
            SessionDescription.RemoveMediaDescription(0);
            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the given payload type         
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 96));
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.audio, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 97));

            //Add the control line for video
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 MP4V-ES/90000"));

            //Add the control line for audio
            SessionDescription.MediaDescriptions.Last().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions.Last().Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 MP4A-LATM/90000"));

            //Should be a field set in constructor.
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("fmtp:96 profile-level-id=1"));
            SessionDescription.MediaDescriptions.Last().Add(new Sdp.SessionDescriptionLine("fmtp:97 profile-level-id=15; profile=1;"));

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, sourceId));

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(2, 3, sourceId, SessionDescription.MediaDescriptions.Last(), false, sourceId));
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