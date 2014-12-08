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

                //If > 1500 an access unit for each 1500 bytes needs to be created.

            }

            public void Depacketize(bool readHeaderLength = true, int sizeLength = 0, int indexLength = 0, int indexDeltaLength = 0)
            {
                #region Expired Draft Notes

                /* https://tools.ietf.org/html/draft-ietf-avt-rtp-mpeg4
                 4.2.1. ES_ID Signaling through the RTP Header
                In a similar manner with the FlexMux tools [14]
                (codeMux mode), we transmit in the RTP packet header a
                code value that indicates the RTP payload organization. In
                such a way to make a correspondence between each
                ES_ID and its associated reduced SL packet, which is
                carried in the RTP payload. This code field may be
                mapped into the SSRC RTP header field (see Figure 4).
                Nevertheless, this approach induces additional out of band
                signaling of the correspondence tables between the
                codeMux field and the associated ES_IDs.
                In addition, the dynamic behavior of MPEG-4 scene
                (e.g. apparition of a new ESs during the MPEG-4 session)
                induces a continuous signaling of the correspondence
                tables, which are exposed to loss. This will result in a
                multiplexing blocking, then a decoding blocking. 
                 */

                //In short to comply with this draft a compatibility flag would be given and 2 more bytes would be skipped per AU.
                //These 2 bytes contain the SL Header
                //Then there is the ES_ID (2 bytes) right after the the AU Headers Length 

                //Thus a total of 4 bytes need to be skipped only if trying to work with this old draft format.
                #endregion

                #region Audio Notes

                /*
                 AU Headers Length: is a two bytes field that specifies:
                    The length in bits of the concatenated AU-headers. If the
                    concatenated AU-headers consume a non-integer number
                    of bytes, up to 7 zero-padding bits must be inserted at the
                    end (PAD field) in order to achieve byte-alignment of the
                    AU Header Section.
                     * 
                    [SL Packets Only (Draft)] ES_ID: is a two bytes field that specifies the ES_ID
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

                #endregion

                //For each packet select the result of
                this.Buffer = new MemoryStream(this.Distinct().SelectMany(rtp =>
                {
                    
                    //Create a sorted list to allow the de-interleaving of access units if required.
                    //Todo only create if needed?
                    SortedList<int, IEnumerable<byte>> accessUnits = new SortedList<int, IEnumerable<byte>>();

                    //From the beginning of the data in the actual payload
                    int offset = rtp.NonPayloadOctets, 
                        max = rtp.Payload.Count - rtp.PaddingOctets, //until the end of the actual payload
                        auIndex = 0, //Indicates the serial number of the associated Access Unit
                        auIndexDelta = 0; //The AU-Index-delta field is an unsigned integer that specifies the serial number of the associated AU as the difference with respect to the serial number of the previous Access Unit.

                    /*
                       The AU-headers are configured using MIME format parameters and MAY be empty.  
                       If the AU-header is configured empty, the AU-headers-length
                       field SHALL NOT be present and consequently the AU Header Section is
                       empty.  If the AU-header is not configured empty, then the AU-
                       headers-length is a two octet field that specifies the length in bits
                       of the immediately following AU-headers, excluding the padding bits.
                    */

                    /*  https://gstrtpmp4adepay.c#L189
                    * Parse StreamMuxConfig according to ISO/IEC 14496-3:
                    *
                    * audioMuxVersion           == 0 (1 bit)
                    * allStreamsSameTimeFraming == 1 (1 bit)
                    * numSubFrames              == rtpmp4adepay->numSubFrames (6 bits)
                    * numProgram                == 0 (4 bits)
                    * numLayer                  == 0 (3 bits)
                    *
                    * We only require audioMuxVersion == 0;
                    *
                    * The remaining bit of the second byte and the rest of the bits are used
                    * for audioSpecificConfig which we need to set in codec_info.
                    */

                    //Object Type 5 Bits.

                    //SampleRate Index 4 Bits.

                    //Channels 4 Bits

                    //If SampleRate Index == 15, 24 Bits of SampleRate

                    //For object types 1-7 Then Parse the Frame Flags. (Which indicate a frameLength of 960?)

                    //Determine the AU Headers Length (in bits)
                    int auHeaderLength = 0,
                        auSize = 0, //AU-Size And the length of the underlying Elementary Stream Data for that access unit
                        parsedUnits = 0;

                    //If we are reading the Access Unit Header Length
                    if (readHeaderLength)
                    {
                        //Then read it
                        auHeaderLength = Common.Binary.ReadU16(rtp.Payload, offset, BitConverter.IsLittleEndian);

                        //If the value was positive
                        if (auHeaderLength > 0)
                        {
                            //Convert bits to bytes
                            auHeaderLength /= 8;

                            //Move the offset
                            offset += 2;
                        }
                    }
                    #region No AU Headers Length

                    // The AU Headers Length is either not present or known..
                    //{
                    //    //Read the 'ES_ID'
                    //    //ushort esId = Common.Binary.ReadU16(rtp.Payload, offset, BitConverter.IsLittleEndian);

                    //    //if (esId == 16) //This is AAC Audio 00 10?
                    //}

                    #endregion

                    //Look for Access Units in the packet
                    while (offset < max)
                    {
                        //AU Headers

                        //sizeLength is the amount of bits set in composite which are used for the size of the access unit

                        //indexLength is the amount of bits set in composite which are used for the index of the access unit

                        //If there was an AU HeadersLegth The size and index and related are given from a configuration
                        if (auHeaderLength > 0)
                        {
                            //Read a variable size integer given by the auHeaderLength  usually (0 - 2  bytes)
                            long composite = Common.Binary.ReadInteger(rtp.Payload, offset, auHeaderLength, BitConverter.IsLittleEndian);

                            //Move the offset past the bytes read
                            offset += auHeaderLength;

                            //The size of the esData is given by removing the bits used for the index
                            auSize = (int)composite >> indexLength;

                            //The index of the access unit is given by removing the bits used for the size
                            auIndex = (int)composite << sizeLength;

                            /*
                             AU-Index-delta: The AU-Index-delta field is an unsigned integer that
                              specifies the serial number of the associated AU as the difference
                              with respect to the serial number of the previous Access Unit.
                              Hence, for the n-th (n>1) AU, the serial number is found from:

                              AU-Index(n) = AU-Index(n-1) + AU-Index-delta(n) + 1

                              If the AU-Index field is present in the first AU-header in the AU
                              Header Section, then the AU-Index-delta field MUST be present in
                              any subsequent (non-first) AU-header.  When the AU-Index-delta is
                              coded with the value 0, it indicates that the Access Units are
                              consecutive in decoding order.  An AU-Index-delta value larger
                              than 0 signals that interleaving is applied.
                             */

                            //For the first access unit determine the auIndexDelta
                            if (parsedUnits == 0 && indexDeltaLength > 0)
                            {
                                //The delta index is given by removing the bits in the auIndex which are used for the index itself.
                                auIndexDelta = auIndex << indexDeltaLength;

                                //Interleaving is applied
                                //if (auIndexDelta > 0) 
                            }
                        }
                        else //auHeaderLength is 0
                        {
                            //Assume that there is no information related to size or index and all data in this packet belongs to a single access unit.
                            return rtp.Payload.Skip(offset);
                        }

                        #region Zero sizeLength or Zero indexLength
                        //The size and index need to be determined before proceeding (this is an example for AAC lbr and AAC hbr)
                        //{

                        //    //////16 Bits (or more) contains the size and the index
                        //    ////ushort composite = Common.Binary.ReadU16(rtp.Payload, offset, BitConverter.IsLittleEndian);

                        //    //////Move the offset
                        //    ////offset += 2;

                        //    //////This specifically applies for Audio media type AAC-hbr

                        //    //////The aac size is 13 bits
                        //    ////esDataLength = composite >> 3;

                        //    //////The index is 3 bits, when interleaving the access units should be sorted by this before being written to the resulting stream.
                        //    ////accessUnitIndex = composite & 7;

                        //    //////This specifically applies for Audio media type AAC-lbr

                        //    //////The aac size is 6 bits
                        //    ////esDataLength = composite >> 2;

                        //    //////The index is 2 bits
                        //    ////accessUnitIndex = composite & 3;
                        //}
                        #endregion

                        //Return the data which belongs to the access unit
                        IEnumerable<byte> accessUnit = rtp.Payload.Skip(offset).Take(auSize);

                        //Add the Access Unit to the list and move to the next in the packet payload
                        accessUnits.Add(auIndex, accessUnit);

                        //Keep track of the amount of access units parsed
                        ++parsedUnits;
                    }

                    //Return the access units in decoding order
                    return accessUnits.SelectMany(au=> au.Value);

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