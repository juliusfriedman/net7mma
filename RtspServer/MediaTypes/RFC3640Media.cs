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

namespace Media.Rtsp.Server.MediaTypes
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc3640">RFC3640</see> which is used for MPEG-4 Elementary Streams.
    /// </summary>
    public class RFC3640Media : RFC2435Media //RtpSink
    {
        public class RFC3640Frame : Rtp.RtpFrame
        {
            public RFC3640Frame(byte payloadType) : base(payloadType) { }

            public RFC3640Frame(Rtp.RtpFrame existing) : base(existing) { }

            public RFC3640Frame(RFC3640Frame f) : this((Rtp.RtpFrame)f) { Buffer = f.Buffer; }

            public System.IO.MemoryStream Buffer { get; set; }

            public void Packetize(byte[] accessUnit, int mtu = 1500)
            {
                throw new NotImplementedException();
            }

            public void Depacketize(int sizeLength = 0, int indexLength = 0)
            {
                //Au Headers
                //2 bytes length of headers
                //If length > 0
                //Length = (length + 7) / 8 (In bits)
                // ReadBytes Length
                // Calulcate header size
                //Need indexLength from Media Description (fmtp line)
                // headerSize = sizeLength + indexLength
                //Determine if CTS, DTS, etc is needed
                //Number of headers = Length / headerSize
                //for(int i =0; i < number of headers; ++i)
                // size += GetBits(sizeLength)
                // index += GetBits(indexLength)
                // Skip Length + 2.


                /*
                 AU Headers Length: is a two bytes field that specifies:
                    The length in bits of the concatenated AU-headers. If the
                    concatenated AU-headers consume a non-integer number
                    of bytes, up to 7 zero-padding bits must be inserted at the
                    end (PAD field) in order to achieve byte-alignment of the
                    AU Header Section.
                     * 
                    ES_ID: is a two bytes field that specifies the ES_ID
                    associated to the AUs carried in the reduced SL packet.
                    This field is common to all the AUs encapsulated into the
                    SL packet. This minimizes the overhead. The ES_ID field
                    is the only one that must be present in the SL packet
                    header.
                    For each Access Unit in the SL packet, there is exactly
                    one AU-header. Hence, the nth AU-header refers to the
                    nth AU.
                     * 
                    AU Size 
                     * Index/IndexDelta 
                     * CTS-Flag CTS-Delta
                     * DTS-Flag DTS-Delta
                    Optional fields
                     * 
                    AU Size: indicates the size in bytes of the associated
                    Access Unit in the reduced SL payload.
                    Index / IndexDelta: indicates the serial number of the
                    associated Access Unit (fragment). For each (in time)
                    consecutive AU or AU fragment, the serial number is
                    incremented with 1. The AU-Index-delta field is an
                    unsigned integer that specifies the serial number of the
                    associated AU as the difference with respect to the serial
                    number of the previous Access Unit. The Index field
                    appears only on the first AU Header of the reduced SL
                    packet.
                     * 
                    CTS-Flag: Indicates whether the CTS-delta field is
                    present. A value of 1 indicates that the field is present, a
                    value of 0 that it is not present.
                     * 
                    CTS-Delta: Encodes the CTS by specifying the value of
                    CTS as a 2's complement offset (delta) from the
                    timestamp in the RTP header of this RTP packet. The CTS
                    must use the same clock rate as the time stamp in the RTP
                    header.
                     * 
                    DTS-Flag: Indicates whether the DTS-delta field is
                    present. A value of 1 indicates that the field is present, a
                    value of 0 that it is not present.
                     * 
                    DTS-Delta: specifies the value of the DTS as a 2's
                    complement offset (delta) from the CTS timestamp. The
                    DTS must use the same clock rate as the time stamp in the
                    RTP header
                 */

                //Nafaa_PV2003_RTP4mux_camera_ready.pdf

                //For each packet select the result of
                this.Buffer = new MemoryStream(this.Distinct().SelectMany(rtp =>
                {
                    //Get the data
                    var coef = rtp.Coefficients;

                    //From the beginning of the data
                    int offset = 0;

                    //Read the AU Headers Length (in bits)
                    var auHeaderLength = Common.Binary.ReadU16(coef, offset, BitConverter.IsLittleEndian);

                    //Convert bits to bytes
                    auHeaderLength /= 8;

                    //Move the offset
                    offset += 2;

                    //16 Bits more contains the aacSize and the aacIndex
                    var composite = Common.Binary.ReadU16(coef, offset, BitConverter.IsLittleEndian);

                    //Move the offset
                    offset += 2;

                    //The aac size is 13 bits
                    var aacSize = composite & 0xFFF8;

                    //The index is 3 bits
                    var aacIndex = composite & 7;
                    
                    //Return the data which belongs to the access unit
                    return coef.Skip(offset).Take(aacSize);

                }).ToArray());
                
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

        public RFC3640Media(int width, int height, string name, string directory = null, bool watch = true)
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
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 mpeg4-generic/" + clockRate));
            
            //Should be a field set in constructor.
            /*
              streamType:
              The integer value that indicates the type of MPEG-4 stream that is
              carried; its coding corresponds to the values of the streamType,
              as defined in Table 9 (streamType Values) in ISO/IEC 14496-1.
             */
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=fmtp:96 streamtype=3; profile-level-id=1; mode=generic; objectType=2; config=0842237F24001FB400094002C0; sizeLength=10; CTSDeltaLength=16; randomAccessIndication=1; streamStateIndication=4"));

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