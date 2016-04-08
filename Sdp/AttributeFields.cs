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

namespace Media.Sdp
{
    //Maybe useful to have a Parameters class which is a subclass of ParametersBase to achieve the ability to register and remove grammar.
    //public sealed class Parameters{

    public sealed class AttributeFields
    {
        AttributeFields() { }

        #region NestedTypes

        //Would be useful for meta programming where certain attributes can appear
        //[System.AttributeUsage(System.AttributeTargets.Class)]
        //public class AttributeType : System.Attribute
        //{
        //    [Flags]
        //    public enum Level
        //    {
        //        Session = 1,
        //        Media = 2,
        //    }

        //    public Level AllowedLevel;

        //    public AttributeType(Level level)
        //    {
        //        this.AllowedLevel = level;
        //    }
        //}

        #endregion

        #region RFC3605 https://tools.ietf.org/html/rfc3605

        //Media Level only
        public const string Rtcp = "rtcp";

        #endregion

        #region RFC4566 https://tools.ietf.org/html/rfc4566

        //Session and Media Level               

        public const string RecieveOnly = "recvonly";

        public const string SendReceive = "sendrecv";

        public const string SendOnly = "sendonly";

        /// <summary>
        /// This specifies that the tools should be started in inactive mode.
        /// This is necessary for interactive conferences where users can put other users on hold. 
        /// No media is sent over an inactive media stream.  
        /// Note that an RTP-based system SHOULD still send RTCP, even if started inactive.
        /// It can be either a session or media-level attribute, and it is not dependent on charset.
        /// </summary>
        public const string Inactive = "inactive";

        /// <summary>
        /// Normally this is only used for a whiteboard or presentation tool.  
        /// It specifies the orientation of a the workspace on the screen.  
        /// It is a media-level attribute.  Permitted values are "portrait", "landscape", and "seascape" (upside-down landscape).  
        /// It is not dependent on charset.
        /// </summary>
        public const string Orient = "orient"; //  a=orient:<orientation>

        /// <summary>
        ///  Specifying the attribute "type:test" is suggested as a hint that, unless explicitly requested otherwise, 
        ///  receivers can safely avoid displaying this session description to users.
        /// </summary>
        public const string Type = "type"; //a=type:<conference type>

        //Session Only

        public const string Charset = "charset"; //   a=charset:<character set> //   a=charset:ISO-8859-1

        public const string SdpLang = "sdplang";

        public const string Lang = "lang";

        //Media only

        /// <summary>
        /// This gives the maximum video frame rate in frames/sec.  
        /// It is intended as a recommendation for the encoding of video data.
        /// Decimal representations of fractional values using the notation
        /// "<integer>.<fraction>" are allowed.  
        /// It is a media-level attribute, defined only for video media, and it is not dependent on charset.
        /// </summary>
        public const string Framerate = "framerate";

        /// <summary>
        /// This gives a suggestion for the quality of the encoding as an integer value.  
        /// The intention of the quality attribute for video is to specify a non-default trade-off between frame-rate and still-image quality.  
        /// For video, the value is in the range 0 to 10, with the following suggested meaning:
        /// 10 - the best still-image quality the compression scheme can give.
        /// 5  - the default behaviour given no quality suggestion.
        /// 0  - the worst still-image quality the codec designer thinks is still usable.
        /// It is a media-level attribute, and it is not dependent on charset.
        /// </summary>
        public const string Quality = "quality";

        public const string FormatType = "fmtp"; // a=fmtp:<format> <format specific parameters> // FormatAttribute

        //Session Level Only

        public const string Category = "cat"; //a=cat:<category>

        public const string Keywords = "keywords"; //   a=keywds:<keywords>

        /// <summary>
        /// This gives the name and version number of the tool used to create the session description.  
        /// It is a session-level attribute, and it is not dependent on charset.
        /// </summary>
        public const string Tool = "tool"; //a=tool:<name and version of tool>

        //Media Level Only

        /// <summary>
        /// This gives the length of time in milliseconds represented by the media in a packet.  
        /// This is probably only meaningful for audio data, but may be used with other media types if it makes sense.  
        /// It should not be necessary to know ptime to decode RTP or vat audio, and it is intended as a recommendation for the encoding/packetisation of audio.  
        /// It is a media-level attribute, and it is not dependent on charset.
        /// </summary>
        public const string PacketTime = "ptime"; //a=ptime:<packet time>

        /// <summary>
        /// This gives the maximum amount of media that can be encapsulated in each packet, expressed as time in milliseconds.  
        /// The time SHALL be calculated as the sum of the time the media present in the packet represents.  
        /// For frame-based codecs, the time SHOULD be an integer multiple of the frame size.  
        /// This attribute is  probably only meaningful for audio data, but may be used with other media types if it makes sense.  
        /// It is a media-level attribute, and it is not dependent on charset.  
        /// Note that this attribute was introduced after RFC 2327, and non-updated implementations will ignore this attribute.
        /// </summary>
        public const string MaximumPacketTime = "maxptime"; // a=maxptime:<maximum packet time>

        public const string RtpMap = "rtpmap"; //a=rtpmap:<payload type> <encoding name>/<clock rate> [/<encoding parameters>]

        #endregion

        #region RFC4145

        //Session and Media Level               

        public const string Setup = "setup";

        public const string Connection = "connection";

        #endregion

        #region RFC2326 https://tools.ietf.org/html/rfc2326

        //Session and Media Level               

        //[RFC2326][RFC-ietf-mmusic-rfc2326bis-40]
        public const string Range = "range";

        public const string Control = "control";

        //RFC-ietf-mmusic-rfc2326bis-40]
        public const string MTag = "mtag";

        #endregion

        #region RFC2848

        //Session and Media Level               

        public const string PhoneContext = "phone-context";

        #endregion

        #region RFC5576 https://tools.ietf.org/html/rfc5576 

        //@ 4.1 Media Level only

        //a=ssrc:<ssrc-id> <attribute>
        //a=ssrc:<ssrc-id> <attribute>:<value>

        public const string SynchronizationSourceIdentifier = "ssrc";

        //@ 4.2 Session Level only
        //a=ssrc-group:<semantics> <ssrc-id> ...

        public const string SynchronizationSourceIdentifierGroup = "ssrc-group";

        //Source Level only

        public const string CName = "cname"; //Canonical name

        public const string PreviousSynchronizationSourceIdentifier = "previous-ssrc"; //previous-ssrc

        //@ 6.3
        // a=ssrc:<ssrc> fmtp:<format> <format specific parameters>

        #endregion

        #region Others
        /* http://www.iana.org/assignments/sdp-parameters/sdp-parameters.xhtml#sdp-parameters-1
                att-field (both session and media level)	record	[RFC-ietf-siprec-protocol-18]
                att-field (both session and media level)	recordpref	[RFC-ietf-siprec-protocol-18]
                att-field (both session and media level)	rtcp-rgrp	[RFC-ietf-avtcore-rtp-multi-stream-optimisation-12]
                */

        #endregion
    }            

    //ProtoFields...
    //udp: denotes an unspecified protocol running over UDP.
    //RTP/AVP
    //RTP/SAVP

    //}
}
