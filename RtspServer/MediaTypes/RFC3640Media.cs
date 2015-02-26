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

            //note the data needed as parameters comes from the SDP `config=`
            //Could have a ParseConfig function.

            /// <summary>
            /// Creates the ADTS Frame Header as specified
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="profileId"></param>
            /// <param name="frequencyIndex"></param>
            /// <param name="channelConfiguration"></param>
            /// <param name="packetLen"></param>
            public static byte[] CreateADTSHeader(int profileId, int frequencyIndex, int channelConfiguration, int packetLen)
            {

                /*
                   http://wiki.multimedia.cx/index.php?title=ADTS

		           ADTS Fixed Header: these don't change from frame to frame
                 * 
		           syncword                                       12    always: '111111111111'
		           ID                                              1    0: MPEG-4, 1: MPEG-2
		           MPEG layer                                      2    If you send AAC in MPEG-TS, set to 0
		           protection_absent                               1    0: CRC present; 1: no CRC
		           profile                                         2    0: AAC Main; 1: AAC LC (Low Complexity); 2: AAC SSR (Scalable Sample Rate); 3: AAC LTP (Long Term Prediction)
		           sampling_frequency_index                        4    15 not allowed
		           private_bit                                     1    usually 0
		           channel_configuration                           3
		           original/copy                                   1    0: original; 1: copy
		           home                                            1    usually 0
		           emphasis                                        2    only if ID == 0 (ie MPEG-4)  // not present in some documentation?

		           * ADTS Variable Header: these can change from frame to frame
		           copyright_identification_bit                    1
		           copyright_identification_start                  1
		           aac_frame_length                               13    length of the frame including header (in bytes)
		           adts_buffer_fullness                           11    0x7FF indicates VBR
		           no_raw_data_blocks_in_frame                     2

		           * ADTS Error check
		           crc_check                                      16    only if protection_absent == 0
                 */

                byte[] header = new byte[7];

                //http://www.p23.nl/projects/aac-header/

                //http://my-tech-knowledge.blogspot.com/2008/02/aac-parsing-over-rfc3640.html

                int nFinalLength = packetLen + 7;

                // fill in ADTS data
                header[0] = byte.MaxValue;

                header[1] = (byte)0xF1; //Sync

                header[2] = (byte)(((profileId - 1) << 6) + (frequencyIndex << 2) + (channelConfiguration >> 2));

                header[3] = (byte)(((channelConfiguration & 0x3) << 6) + (nFinalLength >> 11));

                header[4] = (byte)((nFinalLength & 0x7FF) >> 3);

                header[5] = (byte)(((nFinalLength & 7) << 5) + 0x1F);

                //http://blog.olivierlanglois.net/index.php/2008/09/12/aac_adts_header_buffer_fullness_field

                //Should be bit rate

                header[6] = (byte)0xFC;

                return header;
            }
            

            /// <summary>
            /// Todo, break down logic, ParseHeaders, ParseAuxiliaryData, ParseAccessUnits
            /// </summary>
            /// <param name="headersPresent"></param>
            /// <param name="sizeLength"></param>
            /// <param name="indexLength"></param>
            /// <param name="indexDeltaLength">The AU-Index-delta field is an unsigned integer that specifies the serial number of the associated AU as the difference with respect to the serial number of the previous Access Unit.</param>
            /// <param name="CTSDeltaLength"></param>
            /// <param name="DTSDeltaLength"></param>
            /// <param name="auxDataSizeLength"></param>
            /// <param name="randomAccessIndication"></param>
            /// <param name="streamStateIndication"></param>
            public void Depacketize(bool headersPresent = true, int profileId = 0, int channelConfiguration = 0, int frequencyIndex = 0, int sizeLength = 0, int indexLength = 0, int indexDeltaLength = 0, int CTSDeltaLength = 0, int DTSDeltaLength = 0, int auxDataSizeLength = 0, bool randomAccessIndication = false, int streamStateIndication = 0, int defaultAuSize = 0, IEnumerable<byte> frameHeader = null, bool addPadding = false, bool includeAuHeaders = false, bool includeAuxData = false) 
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

                //Create the buffer as required by the profile.
                this.Buffer = new MemoryStream(headersPresent ? this.SelectMany(rtp =>
                {                                       
                    //From the beginning of the data in the actual payload
                    int offset = rtp.HeaderOctets,
                        max = rtp.Payload.Count - (offset + rtp.PaddingOctets), //until the end of the actual payload
                        auIndex = 0, //Indicates the serial number of the associated Access Unit
                        auHeadersAvailable = 0; //The amount of Au Headers in the Au Header section

                    #region  3.2.  RTP Payload Structure

                    /*
                      

                    3.2.1.  The AU Header Section

                        When present, the AU Header Section consists of the AU-headers-length
                        field, followed by a number of AU-headers, see Figure 2.

                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+- .. -+-+-+-+-+-+-+-+-+-+
                            |AU-headers-length|AU-header|AU-header|      |AU-header|padding|
                            |                 |   (1)   |   (2)   |      |   (n)   | bits  |
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+- .. -+-+-+-+-+-+-+-+-+-+

                                        Figure 2: The AU Header Section

                        1) The AU-headers are configured using MIME format parameters and MAY be
                        empty.  If the AU-header is configured empty, the AU-headers-length
                        field SHALL NOT be present and consequently the AU Header Section is
                        empty.  If the AU-header is not configured empty, then the AU-
                        headers-length is a two octet field that specifies the length in bits
                        of the immediately following AU-headers, excluding the padding bits.

                        2) Each AU-header is associated with a single Access Unit (fragment)
                        contained in the Access Unit Data Section in the same RTP packet.
                     
                        3) For each contained Access Unit (fragment), there is exactly one AU-
                        header.  Within the AU Header Section, the AU-headers are bit-wise
                        concatenated in the order in which the Access Units are contained in
                        the Access Unit Data Section.  Hence, the n-th AU-header refers to
                        the n-th AU (fragment).  If the concatenated AU-headers consume a
                        non-integer number of octets, up to 7 zero-padding bits MUST be
                        inserted at the end in order to achieve octet-alignment of the AU
                        Header Section.
                    */

                    #endregion

                    #region Reference

                    //http://www.netmite.com/android/mydroid/donut/external/opencore/protocols/rtp_payload_parser/rfc_3640/src/rfc3640_payload_parser.cpp

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

                    #endregion

                    //Determine the AU Headers Length (in bits)
                    int auHeaderLengthBits = Common.Binary.ReadU16(rtp.Payload, offset, BitConverter.IsLittleEndian),
                        auHeaderLengthBytes = 0,
                        auSize = 0, //AU-Size And the length of the underlying Elementary Stream Data for that access unit
                        parsedUnits = 0,
                        auHeaderOffset = offset,
                        auxHeaderOffset = 0,
                        auxLengthBytes = 0,     
                        bitOffset = 0, 
                        maxBitOffset = max * Media.Common.Binary.BitSize;

                    //Ensure offset of reading bits matches the offset of the memory segment
                    //Could also have enumerable ReadBits overload
                    offset += 2 + rtp.Payload.Offset;
                   
                    //If there was any auHeaders
                    if (auHeaderLengthBits > 0)
                    {
                        //Convert bits to bytes
                        auHeaderLengthBytes = ((auHeaderLengthBits + 7) / 8);

                        //Check enough bytes are available (2 is the site of the RFC Profile Header read above)
                        if (auHeaderLengthBytes >= max - 2) throw new InvalidOperationException("Invalid Rfc Headers?");

                        //Skip the Au Headers Section
                        Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, auHeaderLengthBits, BitConverter.IsLittleEndian);

                        //This many bits were read
                        int usedBits = sizeLength + indexDeltaLength;

                        if (auHeaderLengthBits != usedBits) throw new InvalidOperationException("Invalid Au Headers?");

                        //Determine the amount of AU Headers possibly contained
                        auHeadersAvailable = 1 + (auHeaderLengthBits - usedBits) / usedBits;
                    }

                    #region No AU Headers Length

                    // The AU Headers Length is either not present or known..
                    //{
                    //    //Read the 'ES_ID'
                    //    //ushort esId = Common.Binary.ReadU16(rtp.Payload, offset, BitConverter.IsLittleEndian);

                    //    //if (esId == 16) //This is AAC Audio 00 10?
                    //}

                    #endregion

                    //Parse AU Headers should be seperate logic?

                    //Create a sorted list to allow the de-interleaving of access units if required.
                    SortedList<int, IEnumerable<byte>> accessUnits = new SortedList<int, IEnumerable<byte>>();               

                    //Now skip over the aux data region.
                    if (0 != auxDataSizeLength)
                    {
                        //Store the offset where the auxData occurs
                        auxHeaderOffset = offset;

                        //Read the size in bits
                        int auxDataSizeBits = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, indexLength, BitConverter.IsLittleEndian);

                        //Calculate the amount of bytes in the auxillary data section
                        auxLengthBytes = ((auxDataSizeBits + 7) / 8);

                        if (max - offset < auxLengthBytes) throw new InvalidOperationException("Invalid Au Aux Data?");

                        //Skip the bits indicated
                        Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, auxDataSizeBits, BitConverter.IsLittleEndian);
                    }

                    // as per 3) skip padding
                    if (bitOffset > 0)
                    {
                        //Skip past any padding left in the last byte
                        ++offset;

                        bitOffset = 0;
                    }

                    //Default auxData
                    IEnumerable<byte> auxillaryData = Utility.Empty;

                    //If there was any auxillary data
                    if (auxLengthBytes > 0)
                    {
                        //Get the auxData
                        auxillaryData = rtp.Payload.Skip(auxHeaderOffset).Take(auxLengthBytes);
                    }

                    //Create the ADTS header
                    //byte[] adtsHeader = CreateADTSHeader(profileId, frequencyIndex, channelConfiguration, auSize);

                    //Look for Access Units in the packet
                    while (offset < max)
                    {
                        /*
                          AU-size: Indicates the size in octets of the associated Access Unit
                          in the Access Unit Data Section in the same RTP packet.  When the
                          AU-size is associated with an AU fragment, the AU size indicates
                          the size of the entire AU and not the size of the fragment.  In
                          this case, the size of the fragment is known from the size of the
                          AU data section.  This can be exploited to determine whether a
                          packet contains an entire AU or a fragment, which is particularly
                          useful after losing a packet carrying the last fragment of an AU.
                         */

                        if (sizeLength > 0)
                        {
                            //In bytes
                            auSize = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, sizeLength, BitConverter.IsLittleEndian);
                        }
                        else auSize = defaultAuSize;

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

                        if (parsedUnits == 0 && indexLength > 0)
                            auIndex = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, indexLength, BitConverter.IsLittleEndian);
                        else if (indexDeltaLength > 0)
                            auIndex = auIndex /* - 1*/ + (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, indexDeltaLength, BitConverter.IsLittleEndian); /* + 1*/;

                        //Interleaving is applied
                        //if (auIndexDelta > 0) 

                        //From RFC3640: "The CTS-flag field MUST be present in each AU-header
                        //               if the length of the CTS-delta field is signaled to
                        //               be larger than zero."
                        if (0 != CTSDeltaLength)
                        {
                            bool CTSFlag = Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, 1, BitConverter.IsLittleEndian) > 0;
                            if (CTSFlag)
                            {
                                //int CTSDelta = getIntFromBitArray(headerBits, bitOffset, CTSDeltaLength);
                                int CTSDelta = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, indexLength, BitConverter.IsLittleEndian); ;
                            }
                        }

                        if (0 != DTSDeltaLength)
                        {
                            bool DTSFlag = Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, 1, BitConverter.IsLittleEndian) > 0;
                            if (DTSFlag)
                            {
                                //int DTSDelta = getIntFromBitArray(headerBits, bitOffset, CTSDeltaLength);
                                int DTSDelta = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, indexLength, BitConverter.IsLittleEndian);
                            }
                        }

                        if (randomAccessIndication)
                        {
                            bool RAPFlag = Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, 1, BitConverter.IsLittleEndian) > 0;
                        }

                        //StreamState
                        //https://www.ietf.org/proceedings/54/slides/avt-6.pdf                      
                        if (0 != streamStateIndication)
                        {
                            int streamState = (int)Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, streamStateIndication, BitConverter.IsLittleEndian);

                            Media.Common.Binary.ReadBits(rtp.Payload.Array, ref offset, ref bitOffset, streamState, BitConverter.IsLittleEndian);
                        }

                         // as per 3) skip padding
                        if (bitOffset > 0)
                        {
                            //Skip past any padding left in the last byte
                            ++offset;

                            bitOffset = 0;
                        }

                        //Get the header which corresponds to this access unit
                        var accessUnitHeader = rtp.Payload.Skip(auHeaderOffset).Take(auHeaderLengthBytes);

                        //Used a  auHeader, if there is more then one move the offset to the next header
                        if (--auHeadersAvailable > 0) auHeaderOffset += auHeaderLengthBytes;

                        //Stop for incomplete access units.
                        //Detected by the length of the buffer being less then the length of all contained data (could have a signal for this)
                        if (auSize > max - offset) break;

                        //Project the data in the payload from the offset of the access unit until its declared size.
                        var accessUnitData = rtp.Payload.Array.Skip(offset).Take(auSize);
                        
                        //Prepend the accessUnitHeaer with the data to create a depacketized au
                        var depacketizedAccessUnit = includeAuHeaders ? Enumerable.Concat(accessUnitHeader, accessUnitData) : accessUnitData;

                        //If there aux data then add it after the Au header we just added
                        if (includeAuxData && auxLengthBytes > 0)
                        {
                            //Add the auxillary data
                            depacketizedAccessUnit = Enumerable.Concat(depacketizedAccessUnit, auxillaryData);
                        }

                        //If a frameHeader is required for each accessUnit then prepend it here
                        //if (previouslyIncompleteAccessUnitWithAuxHeaderAndData != null)
                        //{
                        //    //May have to write the length in the frameData
                        //    depacketizedAccessUnit = Enumerable.Concat(frameHeader, depacketizedAccessUnit);

                        //    previouslyIncompleteAccessUnitWithAuxHeaderAndData = null;
                        //}
                        //else
                        //{
                        //    //Could just update the length but
                        //    //This is a bit field though and needs to have the WriteBits methods available then
                        //    //Media.Common.Binary.Write16(adtsHeader, 6, false, (short)auSize);

                        //    //Create the header for the frame, (should only be done once and the length should be updated each time)
                        //    //Should also have the ADTS header only 1 time, and only after the frameLength is known.
                        //    depacketizedAccessUnit = Enumerable.Concat(CreateADTSHeader(profileId, frequencyIndex, channelConfiguration, auSize), depacketizedAccessUnit);
                        //}

                        //Add padding if required.. ?Allow custom
                        if(addPadding)
                        { 
                            int required = max - offset;

                            if (required > 0) depacketizedAccessUnit = Enumerable.Concat(depacketizedAccessUnit, Enumerable.Repeat<byte>(byte.MinValue, required));
                        }

                        //Add the Access Unit to the list
                        accessUnits.Add(auIndex, depacketizedAccessUnit); 

                        //Move the byte offset leaving the bit offset in tact
                        offset += auSize;

                        //Keep track of the amount of access units parsed
                        ++parsedUnits;
                    }

                    //Return the access units in decoding order
                    return accessUnits.SelectMany(au=> au.Value); //Enumerable.Concat(CreateADTSHeader(profileId, frequencyIndex, channelConfiguration, accessUnits.Sum(au=> au.Value.Count())), accessUnits.SelectMany(au=> au.Value));

                }).ToArray() : this.Assemble().ToArray());
                
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
                if (IsDisposed) return;

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
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 mpeg4-generic/" + clockRate));
            
            //Should be a field set in constructor.
            /*
              streamType:
              The integer value that indicates the type of MPEG-4 stream that is
              carried; its coding corresponds to the values of the streamType,
              as defined in Table 9 (streamType Values) in ISO/IEC 14496-1.
             */
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=fmtp:96 streamtype=3; profile-level-id=1; mode=generic; objectType=2; config=0842237F24001FB400094002C0; sizeLength=10; CTSDeltaLength=16; randomAccessIndication=1; streamStateIndication=4"));

            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions.First(), false, 0));
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