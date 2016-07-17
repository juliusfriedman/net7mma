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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Media.Rtsp
{
    #region Reference https://tools.ietf.org/html/rfc2326

    //    11 Status Code Definitions

    //   Where applicable, HTTP status [H10] codes are reused. Status codes
    //   that have the same meaning are not repeated here. See Table 1 for a
    //   listing of which status codes may be returned by which requests.

    //11.1 Success 2xx

    //11.1.1 250 Low on Storage Space

    //   The server returns this warning after receiving a RECORD request that
    //   it may not be able to fulfill completely due to insufficient storage
    //   space. If possible, the server should use the Range header to
    //   indicate what time period it may still be able to record. Since other
    //   processes on the server may be consuming storage space
    //   simultaneously, a client should take this only as an estimate.

    //11.2 Redirection 3xx

    //   See [H10.3].

    //   Within RTSP, redirection may be used for load balancing or
    //   redirecting stream requests to a server topologically closer to the
    //   client.  Mechanisms to determine topological proximity are beyond the
    //   scope of this specification.

    //RFC 2326              Real Time Streaming Protocol            April 1998


    //11.3 Client Error 4xx

    //11.3.1 405 Method Not Allowed

    //   The method specified in the request is not allowed for the resource
    //   identified by the request URI. The response MUST include an Allow
    //   header containing a list of valid methods for the requested resource.
    //   This status code is also to be used if a request attempts to use a
    //   method not indicated during SETUP, e.g., if a RECORD request is
    //   issued even though the mode parameter in the Transport header only
    //   specified PLAY.

    //11.3.2 451 Parameter Not Understood

    //   The recipient of the request does not support one or more parameters
    //   contained in the request.

    //11.3.3 452 Conference Not Found

    //   The conference indicated by a Conference header field is unknown to
    //   the media server.

    //11.3.4 453 Not Enough Bandwidth

    //   The request was refused because there was insufficient bandwidth.
    //   This may, for example, be the result of a resource reservation
    //   failure.

    //11.3.5 454 Session Not Found

    //   The RTSP session identifier in the Session header is missing,
    //   invalid, or has timed out.

    //11.3.6 455 Method Not Valid in This State

    //   The client or server cannot process this request in its current
    //   state.  The response SHOULD contain an Allow header to make error
    //   recovery easier.

    //11.3.7 456 Header Field Not Valid for Resource

    //   The server could not act on a required request header. For example,
    //   if PLAY contains the Range header field but the stream does not allow
    //   seeking.


    //Schulzrinne, et. al.        Standards Track                    [Page 42]

    //RFC 2326              Real Time Streaming Protocol            April 1998


    //11.3.8 457 Invalid Range

    //   The Range value given is out of bounds, e.g., beyond the end of the
    //   presentation.

    //11.3.9 458 Parameter Is Read-Only

    //   The parameter to be set by SET_PARAMETER can be read but not
    //   modified.

    //11.3.10 459 Aggregate Operation Not Allowed

    //   The requested method may not be applied on the URL in question since
    //   it is an aggregate (presentation) URL. The method may be applied on a
    //   stream URL.

    //11.3.11 460 Only Aggregate Operation Allowed

    //   The requested method may not be applied on the URL in question since
    //   it is not an aggregate (presentation) URL. The method may be applied
    //   on the presentation URL.

    //11.3.12 461 Unsupported Transport

    //   The Transport field did not contain a supported transport
    //   specification.

    //11.3.13 462 Destination Unreachable

    //   The data transmission channel could not be established because the
    //   client address could not be reached. This error will most likely be
    //   the result of a client attempt to place an invalid Destination
    //   parameter in the Transport field.

    //11.3.14 551 Option not supported

    //   An option given in the Require or the Proxy-Require fields was not
    //   supported. The Unsupported header should be returned stating the
    //   option for which there is no support.

    #endregion

    /// <summary>
    /// The status codes utilized in RFC2326 Messages given in response to a request
    /// </summary>
    public enum RtspStatusCode
    {
        Unknown = 0,
        // 1xx Informational.
        Continue = 100,

        // 2xx Success.
        OK = 200,
        Created = 201,
        LowOnStorageSpace = 250,

        // 3xx Redirection.
        MultipleChoices = 300,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,

        // 4xx Client Error.
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeOut = 408,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        RequestMessageBodyTooLarge = 413,
        RequestUriTooLarge = 414,
        UnsupportedMediaType = 415,
        ParameterNotUnderstood = 451,
        Reserved = 452,
        NotEnoughBandwidth = 453,
        SessionNotFound = 454,
        MethodNotValidInThisState = 455,
        HeaderFieldNotValidForResource = 456,
        InvalidRange = 457,
        ParameterIsReadOnly = 458,
        AggregateOpperationNotAllowed = 459,
        OnlyAggregateOpperationAllowed = 460,
        UnsupportedTransport = 461,
        DestinationUnreachable = 462,
        DestinationProhibited = 463,
        DataTransportNotReadyYet = 464,
        NotificationReasonUnknown = 465,
        KeyManagementError = 466,

        ConnectionAuthorizationRequired = 470,
        ConnectionCredentialsNotAcception = 471,
        FaulireToEstablishSecureConnection = 472,

        // 5xx Server Error.
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeOut = 504,
        RtspVersionNotSupported = 505,
        OptionNotSupported = 551,
    }

    /// <summary>
    /// Enumeration to describe the available Rtsp Methods, used in responses
    /// </summary>
    public enum RtspMethod
    {
        UNKNOWN,
        OPTIONS,
        ANNOUNCE,
        DESCRIBE,
        REDIRECT,
        SETUP,
        GET_PARAMETER,
        SET_PARAMETER,
        PLAY,
        PLAY_NOTIFY,
        PAUSE,
        RECORD,
        TEARDOWN
    }

    /// <summary>
    /// Enumeration to indicate the type of RtspMessage
    /// </summary>
    public enum RtspMessageType
    {
        Invalid = 0,
        Request = 1,
        Response = 2,
    }

    /// <summary>
    /// Base class of RtspRequest and RtspResponse
    /// </summary>
    public class RtspMessage : Media.Http.HttpMessage
    {
        #region Statics

        //The scheme of Uri's of RtspMessage's
        public const string ReliableTransportScheme = "rtsp";

        public const int ReliableTransportDefaultPort = 554;

        //The scheme of Uri's of RtspMessage's which are usually being transported via udp
        public const string UnreliableTransportScheme = "rtspu";

        public const int UnreliableTransportDefaultPort = 555;

        //`Secure` RTSP...
        public const string SecureTransportScheme = "rtsps";

        public const int SecureTransportDefaultPort = 322;

        //`Secure` RTSP...
        public const string TcpTransportScheme = "rtspt";

        //The maximum amount of bytes any RtspMessage can contain.
        public const int MaximumLength = 4096;

        //String which identifies a Rtsp Request or Response
        public const string MessageIdentifier = "RTSP";

        internal protected static char[] SpaceSplit = Http.HttpMessage.SpaceSplit;

        public static byte[] ToHttpBytes(RtspMessage message, int majorVersion = 1, int minorVersion = 0, string sessionCookie = null, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.Unused)
        {

            if (message.RtspMessageType == RtspMessageType.Invalid) return null;

            //Our result in a List
            List<byte> result = new List<byte>();

            //Our RtspMessage base64 encoded
            byte[] messageBytes;

            //Either a RtspRequest or a RtspResponse
            if (message.RtspMessageType == RtspMessageType.Request)
            {
                //Get the body of the HttpRequest
                messageBytes = message.ContentEncoding.GetBytes(System.Convert.ToBase64String(message.ToBytes()));

                //Form the HttpRequest Should allow POST and MultiPart
                result.AddRange(message.ContentEncoding.GetBytes("GET " + message.Location + " HTTP " + majorVersion.ToString() + "." + minorVersion.ToString() + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));

                if (false == string.IsNullOrWhiteSpace(sessionCookie))
                {
                    result.AddRange(message.ContentEncoding.GetBytes("x-sessioncookie: " + System.Convert.ToBase64String(message.ContentEncoding.GetBytes(sessionCookie)) + CRLF));
                }

                result.AddRange(message.ContentEncoding.GetBytes(CRLF));
                result.AddRange(message.ContentEncoding.GetBytes(CRLF));

                result.AddRange(messageBytes);
            }
            else
            {
                //Get the body of the HttpResponse
                messageBytes = message.ContentEncoding.GetBytes(System.Convert.ToBase64String(message.ToBytes()));

                //Form the HttpResponse
                result.AddRange(message.ContentEncoding.GetBytes("HTTP/" + majorVersion.ToString() + "." + minorVersion.ToString() + " " + (int)statusCode + " " + statusCode + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes("Expires:Sun, 9 Jan 1972 00:00:00 GMT" + CRLF));
                result.AddRange(message.ContentEncoding.GetBytes(CRLF));
                result.AddRange(message.ContentEncoding.GetBytes(CRLF));

                result.AddRange(messageBytes);
            }

            return result.ToArray();
        }

        public static RtspMessage FromHttpBytes(byte[] message, int offset, Encoding encoding = null, bool bodyOnly = false)
        {
            //Sanity
            if (message == null) return null;
            if (offset > message.Length) throw new ArgumentOutOfRangeException("offset");

            //Use a default encoding if none was given
            if (encoding == null) encoding = RtspMessage.DefaultEncoding;

            //Parse the HTTP 
            string Message = encoding.GetString(message, offset, message.Length - offset);

            //Find the end of all the headers
            int headerEnd = Message.IndexOf(CRLF + CRLF);

            //Get the Http Body, It occurs after all the headers which ends with \r\n\r\n and is Base64 Encoded.
            string Body = Message.Substring(headerEnd);

            //Might want to provide the headers as an out param /.

            //Get the bytes of the underlying RtspMessage by decoding the Http Body which was encoded in base64
            byte[] rtspMessage = System.Convert.FromBase64String(Body);

            //Done
            return new RtspMessage(rtspMessage);
        }

        public static RtspMessage FromString(string data, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new InvalidOperationException("data cannot be null or whitespace.");

            if (encoding == null) encoding = RtspMessage.DefaultEncoding;

            return new RtspMessage(encoding.GetBytes(data), 0, encoding);
        }

        #endregion

        #region Fields

        int m_CSeq = -1;

        //long m_RawLength = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="RtspMethod"/> which can be parsed from the <see cref="MethodString"/>
        /// </summary>
        public RtspMethod RtspMethod
        {
            get
            {
                RtspMethod parsed = RtspMethod.UNKNOWN;

                if (false == string.IsNullOrWhiteSpace(MethodString)) Enum.TryParse<RtspMethod>(MethodString, true, out parsed);

                return parsed;
            }
            set { MethodString = value.ToString(); }
        }

        /// <summary>
        /// Indicates the StatusCode of the RtspResponse.
        ///  A value of 200 or less usually indicates success.
        /// </summary>
        public RtspStatusCode RtspStatusCode
        {
            get { return (RtspStatusCode)m_StatusCode; }
            set
            {
                m_StatusCode = (int)value;

                if (false == CanHaveBody) m_Body = string.Empty;
            }
        }

        ///// <summary>
        ///// Indicates if the Message can have a Body
        ///// </summary>
        //public override bool CanHaveBody
        //{
        //    get
        //    {
        //        return false == (MessageType == RtspMessageType.Response &&
        //        (RtspStatusCode == RtspStatusCode.NotModified || RtspStatusCode == RtspStatusCode.Found));
        //    }
        //}

        public override bool IsComplete
        {
            get
            {                

                //Disposed is complete 
                if (IsDisposed && IsPersistent.Equals(false)) return false;

                //If the status line was not parsed
                if (m_StatusLineParsed.Equals(false) || //All requests must have a StatusLine OR
                    object.ReferenceEquals(m_Buffer, null).Equals(false) &&  // Be parsing the StatusLine
                    m_Buffer.Length <= MinimumStatusLineSize) return false;

                if (false.Equals(string.IsNullOrWhiteSpace(ParsedProtocol)) && false.Equals(ParsedProtocol.Equals(Protocol, StringComparison.OrdinalIgnoreCase)))
                {
                    return base.IsComplete;
                }

                //Messages without complete header sections are not complete
                if (false.Equals(ParseHeaders()) /*&& m_CSeq == -1*/) return false;

                //Don't check for any required values, only that the end of headers was seen.
                //if (MessageType == HttpMessageType.Request && CSeq == -1 || //All requests must contain a sequence number
                //    //All successful responses should also contain one
                //    MessageType == HttpMessageType.Response && StatusCode <= HttpStatusCode.OK && CSeq == -1) return false;

                //If the message can have a body
                if (CanHaveBody)
                {
                    //Determine if the body is present
                    bool hasNullBody = string.IsNullOrWhiteSpace(m_Body);

                    //Ensure content-length was parsed. (reparse)
                    //ParseContentLength(hasNullBody);

                    //Messages with ContentLength AND no Body are not complete.
                    //Determine if the count of the octets in the body is greater than or equal to the supposed amount

                    return ParseContentLength(hasNullBody).Equals(false) && hasNullBody && m_ContentLength > 0 ? m_HeadersParsed : ContentEncoding.GetByteCount(m_Body) >= m_ContentLength;

                    //return hasNullBody && m_ContentLength > 0 ? false : false == hasNullBody && m_ContentLength <= 0 || (ContentEncoding.GetByteCount(m_Body) >= m_ContentLength);

                }

                //The message is complete
                return true;
            }
        }

        /// <summary>
        /// Indicates if this RtspMessage is a request or a response
        /// </summary>
        public /*new*/ RtspMessageType RtspMessageType { get { return (RtspMessageType)base.MessageType; } internal set { base.MessageType = (Http.HttpMessageType)value; } }

        /// <summary>
        /// Gets or Sets the CSeq of this RtspMessage, if found and parsed; otherwise -1.
        /// </summary>
        public int CSeq
        {
            get
            {
                //Reparse unless already parsed the headers
                ParseSequenceNumber(m_HeadersParsed);

                return m_CSeq;
            }
            set
            {
                //Use the unsigned representation
                if (m_CSeq != value) SetHeader(RtspHeaders.CSeq, ((uint)(m_CSeq = value)).ToString());
            }
        }        

        #endregion

        #region Constructor

        static RtspMessage()
        {
            /*
             5004 UDP - used for delivering data packets to clients that are streaming by using RTSPU.
             5005 UDP - used for receiving packet loss information from clients and providing synchronization information to clients that are streaming by using RTSPU.
 
             See also: port 1755 - Microsoft Media Server (MMS) protocol
             */

            //Should be done in RtspMessage constructor...

            if (false == UriParser.IsKnownScheme(RtspMessage.ReliableTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.ReliableTransportScheme, RtspMessage.ReliableTransportDefaultPort);

            if (false == UriParser.IsKnownScheme(RtspMessage.TcpTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.TcpTransportScheme, RtspMessage.ReliableTransportDefaultPort);

            if (false == UriParser.IsKnownScheme(RtspMessage.UnreliableTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.UnreliableTransportScheme, RtspMessage.UnreliableTransportDefaultPort);

            if (false == UriParser.IsKnownScheme(RtspMessage.SecureTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.SecureTransportScheme, RtspMessage.SecureTransportDefaultPort);
        }

        /// <summary>
        /// Reserved
        /// </summary>
        internal protected RtspMessage() : base(RtspMessage.MessageIdentifier) { }

        /// <summary>
        /// Constructs a RtspMessage
        /// </summary>
        /// <param name="messageType">The type of message to construct</param>
        public RtspMessage(RtspMessageType messageType, double? version = 1.0, Encoding contentEncoding = null, bool shouldDispse = true)
            : base((Http.HttpMessageType)messageType, version, contentEncoding, shouldDispse, RtspMessage.MessageIdentifier)
        {

        }

        /// <summary>
        /// Creates a RtspMessage from the given bytes
        /// </summary>
        /// <param name="bytes">The byte array to create the RtspMessage from</param>
        /// <param name="offset">The offset within the bytes to start creating the message</param>
        public RtspMessage(byte[] bytes, int offset = 0, Encoding encoding = null) : this(bytes, offset, bytes.Length - offset, encoding) { }

        public RtspMessage(Common.MemorySegment data, Encoding encoding = null) : this(data.Array, data.Offset, data.Count, encoding) { }

        /// <summary>
        /// Creates a managed representation of an abstract RtspMessage concept from RFC2326.
        /// </summary>
        /// <param name="packet">The array segment which contains the packet in whole at the offset of the segment. The Count of the segment may not contain more bytes than a RFC2326 message may contain.</param>
        /// <reference>
        /// RFC2326 - http://tools.ietf.org/html/rfc2326 - [Page 19]
        /// 4.4 Message Length
        ///When a message body is included with a message, the length of that
        ///body is determined by one of the following (in order of precedence):
        ///1.     Any response message which MUST NOT include a message body
        ///        (such as the 1xx, 204, and 304 responses) is always terminated
        ///        by the first empty line after the header fields, regardless of
        ///        the entity-header fields present in the message. (Note: An
        ///        empty line consists of only CRLF.)
        ///2.     If a Content-Length header field (section 12.14) is present,
        ///        its value in bytes represents the length of the message-body.
        ///        If this header field is not present, a value of zero is
        ///        assumed.
        ///3.     By the server closing the connection. (Closing the connection
        ///        cannot be used to indicate the end of a request body, since
        ///        that would leave no possibility for the server to send back a
        ///        response.)
        ///Note that RTSP does not (at present) support the HTTP/1.1 "chunked"
        ///transfer coding(see [H3.6]) and requires the presence of the
        ///Content-Length header field.
        ///    Given the moderate length of presentation descriptions returned,
        ///    the server should always be able to determine its length, even if
        ///    it is generated dynamically, making the chunked transfer encoding
        ///    unnecessary. Even though Content-Length must be present if there is
        ///    any entity body, the rules ensure reasonable behavior even if the
        ///    length is not given explicitly.
        /// </reference>        
        public RtspMessage(byte[] data, int offset, int length, Encoding contentEncoding = null, bool shouldDispose = true)
            :base(data, offset, length, contentEncoding, shouldDispose, RtspMessage.MessageIdentifier)
        {

            
        }

        /// <summary>
        /// Creates a RtspMessage by copying the properties of another.
        /// </summary>
        /// <param name="other">The other RtspMessage</param>
        public RtspMessage(RtspMessage other) : base(other)
        {
            
        }

        #endregion

        #region Methods

        public override IEnumerable<byte> PrepareBody(bool includeEmptyLine = false)
        {

            IEnumerable<byte> result = Common.MemorySegment.EmptyBytes;

            if (IsDisposed && false == IsPersistent) return result;

            if (m_ContentLength > 0)
            {
                //foreach (byte b in ContentEncoding.GetBytes(m_Body)/*.Take(m_ContentLength)*/) yield return b;

                result = Enumerable.Concat(result, ContentEncoding.GetBytes(m_Body));

            }

            if (includeEmptyLine && m_EncodedLineEnds != null)
            {
                //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

                result = Enumerable.Concat(result, m_HeaderEncoding.GetBytes(m_EncodedLineEnds));
            }

            return result;
        }

        override protected bool ParseBody(out int remaining, bool force = false) //bool requireContentLength = true
        {
            remaining = 0;

            //If the message is disposed then the body is parsed.
            if (IsDisposed && IsPersistent.Equals(false)) return false;

            //If the message is invalid or
            if (force.Equals(false) && RtspMessageType == RtspMessageType.Invalid ||
                //false == string.IsNullOrWhiteSpace(m_Body) || //or body was already started parsing
                IsComplete) return true; //or the message is complete then return true

            //If no headers could be parsed then don't parse the body
            if (ParseHeaders().Equals(false)) return false;

            //If there is no ContentLength then do not parse the body, this could be allowed to parse further...
            //requireContentLength && 
            if (m_ContentLength < 0 && ParseContentLength().Equals(false)) return false;

            //Empty body or no ContentLength
            //If the message cannot have a body it is parsed.
            if (m_ContentLength == 0 || false == CanHaveBody) return true;

            //Get the decoder to use for the body
            Encoding decoder = ParseContentEncoding(false, FallbackToDefaultEncoding);

            if (object.ReferenceEquals(decoder, null)) return false;

            int existingBodySize = decoder.GetByteCount(m_Body);

            //Calculate how much data remains based on the ContentLength
            remaining = m_ContentLength - existingBodySize;

            if (remaining.Equals(0)) return true;

            //If there was no buffer or an unreadable buffer then no parsing can occur
            if (object.ReferenceEquals(m_Buffer, null) || m_Buffer.CanRead.Equals(false)) return false;

            //Quite possibly should be long
            int max = (int)m_Buffer.Length;

            int position = (int)m_Buffer.Position,
                   available = max - position;

            if (available.Equals(0)) return false;

            //Get the array of the memory stream
            byte[] buffer = m_Buffer.GetBuffer();

            //Ensure no control characters were left from parsing of the header values if more data is available then remains
            while (existingBodySize == 0 && available > 0 && Array.IndexOf<char>(m_EncodedLineEnds, decoder.GetChars(buffer, position, 1)[0]) >= 0)
            {
                ++position;

                --available;
            }

            //Get the body of the message which is the amount of bytes remaining based on the current position in parsing
            if (available > 0)
            {
                if (existingBodySize.Equals(0))
                    m_Body = decoder.GetString(buffer, position, Media.Common.Binary.Min(available, remaining));
                else                     //Append to the existing body
                    m_Body += decoder.GetString(buffer, position, Media.Common.Binary.Min(available, remaining));

                //No longer needed, and would interfere with CompleteFrom logic.
                DisposeBuffer();

                //Body was parsed or started to be parsed.
                return true;
            }

            //The body was not parsed
            return false;
        }

        /// <summary>
        /// Completes the RtspMessage from either the buffer or the socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {
            if (IsDisposed && false.Equals(IsPersistent)) return 0;

            bool hasSocket = object.ReferenceEquals(socket, null).Equals(false), 
                 hasBuffer = buffer.IsDisposed.Equals(false) && buffer.Count > 0;

            //If there is no socket or no data available in the buffer nothing can be done
            if (false.Equals(hasSocket) && false.Equals(hasBuffer))
            {
                return 0;
            }

            //Don't check IsComplete because of the notion of how a HttpMessage can be received.
            //There may be additional headers which are available before the body          

            int received = 0;

            if (false.Equals(hasSocket))
            {
                //Create the buffer if it was null
                if (object.ReferenceEquals(m_Buffer, null) || false.Equals(m_Buffer.CanWrite))
                {
                    m_Buffer = new System.IO.MemoryStream();

                    m_HeaderOffset = 0;
                }
                else
                {
                    //Otherwise prepare to append the buffer
                    m_Buffer.Seek(0, System.IO.SeekOrigin.End);

                    //Update the length
                    m_Buffer.SetLength(m_Buffer.Length + buffer.Count);
                }

                //If there was a buffer
                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(buffer)) && buffer.Count > 0)
                {
                    if (object.ReferenceEquals(m_Buffer, null)) return received;

                    //Write the new data
                    if (m_Buffer.CanWrite) m_Buffer.Write(buffer.Array, buffer.Offset, received += buffer.Count);

                    //Go to the beginning
                    if(m_Buffer.CanSeek) m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);
                }
            }

            //If the status line was not parsed return the number of bytes written, reparse if there are no headers parsed yet.
            if (false.Equals(ParseStatusLine(RtspMessageType == RtspMessageType.Invalid) || false.Equals(m_StatusLineParsed))) return received;
            else if (false.Equals(object.ReferenceEquals(m_Buffer, null)) && m_Buffer.CanSeek) m_Buffer.Seek(m_HeaderOffset, System.IO.SeekOrigin.Begin); // Seek past the status line.

            //Determine if there can be and is a body already
            bool hasNullBody = CanHaveBody && string.IsNullOrWhiteSpace(m_Body);

            //Force the re-parsing of headers unless the body has started parsing.
            if (false.Equals(ParseHeaders(hasNullBody))) return received;

            //Reparse any content-length if it was not already parsed or was a 0 value and the body is still null
            if (m_ContentLength <= 0 && false.Equals(ParseContentLength(hasNullBody))) return received;

            //Http closes the connection when there is no content-length...

            //If there is a socket
            if (hasSocket)
            {
                //Use the content decoder (reparse if the body was null)
                Encoding decoder = ParseContentEncoding(hasNullBody, FallbackToDefaultEncoding);

                //Calulcate the amount of bytes in the body
                //int encodedBodyCount = decoder.GetByteCount(m_Body);

                //Determine how much remaing
                int remaining;

                //If there are remaining octetes then complete the HttpMessage
                if (false.Equals(ParseBody(out remaining, false) && remaining > 0))
                {
                    //Store the error
                    System.Net.Sockets.SocketError error = System.Net.Sockets.SocketError.SocketError;

                    //Keep track of whats received as of yet and where
                    int justReceived = 0, offset = buffer.Offset;

                    //While there is something to receive.
                    while (remaining > 0)
                    {
                        //Receive remaining more if there is a socket otherwise use the remaining data in the buffer when no socket is given.
                        justReceived = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(buffer.Array, offset, remaining, socket, out error);

                        //If anything was present then add it to the body.
                        if (justReceived > 0)
                        {
                            //Concatenate the result into the body
                            //Todo, copy the bytes. The body may not be a usable string.

                            m_Body += decoder.GetString(buffer.Array, offset, Media.Common.Binary.Min(remaining, justReceived));

                            //Decrement for what was justReceived
                            remaining -= justReceived;

                            //Increment for what was justReceived
                            received += justReceived;
                        }

                        //If any socket error occured besides a timeout or a block then stop trying to receive.
                        //if (error != System.Net.Sockets.SocketError.Success || error != System.Net.Sockets.SocketError.TimedOut || error != System.Net.Sockets.SocketError.TryAgain) break;
                        switch (error)
                        {
                            case System.Net.Sockets.SocketError.Success:
                            case System.Net.Sockets.SocketError.TimedOut:
                            case System.Net.Sockets.SocketError.TryAgain:
                                continue;
                            default: goto End;
                        }
                    }
                }
            }
            else ParseBody(true);

        End:
            //Return the amount of bytes consumed.
            return received;
        }

        internal protected virtual bool ParseSequenceNumber(bool force = false)
        {
            //If the message is disposed then no parsing can occur
            if (IsDisposed && false.Equals(IsPersistent)) return false;

            if (false.Equals(force) && m_CSeq >= 0) return false;

            //See if there is a Content-Length header
            string sequenceNumber = GetHeader(RtspHeaders.CSeq);

            //If the value was null or empty then do nothing
            if (string.IsNullOrWhiteSpace(sequenceNumber)) return false;

            //If there is a header parse it's value.
            //Should use EncodingExtensions
            if (false.Equals(int.TryParse(Media.Common.ASCII.ExtractNumber(sequenceNumber), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out m_CSeq)))
            {
                //There was not a content-length in the format '1234'

                //Determine if alternate format parsing is allowed...

                return false;
            }

            return true;
        }        

        protected override void OnHeaderAdded(string headerName, string headerValue)
        {
            if (string.Compare(headerName, Http.HttpHeaders.TransferEncoding, true) == 0) throw new InvalidOperationException("Protocol: " + Protocol + ", does not support TrasferEncoding.");

            base.OnHeaderAdded(headerName, headerValue);
        }

        /// <summary>
        /// Called when a header is removed
        /// </summary>
        /// <param name="headerName"></param>
        protected override void OnHeaderRemoved(string headerName, string headerValue)
        {
            if (IsDisposed && false.Equals(IsPersistent)) return;

            //If there is a null or empty header ignore
            if (string.IsNullOrWhiteSpace(headerName)) return;

            //The lower case invariant name and determine if action is needed
            switch (headerName.ToLowerInvariant())
            {
                case "cseq":
                    {
                        m_CSeq = -1;

                        break;
                    }
                default:
                    {
                        base.OnHeaderRemoved(headerName, headerValue);

                        break;
                    }
            }
        }

        #endregion

        #region Overrides        

        public override int GetHashCode()
        {
            return Created.GetHashCode() ^ (int)((int)RtspMessageType | (int)RtspMethod ^ (int)RtspStatusCode) ^ (string.IsNullOrWhiteSpace(m_Body) ? Length : m_Body.GetHashCode()) ^ (m_Headers.Count ^ CSeq);
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false.Equals((obj is RtspMessage))) return false;

            RtspMessage other = obj as RtspMessage;

            //Fast path doesn't show true equality.
            //other.Created != Created

            return other.RtspMessageType == RtspMessageType
                &&
                other.Version == Version
                &&
                other.MethodString == MethodString
                //&&
                // other.m_Headers.Count == m_Headers.Count 
                &&
                other.GetHeaders().All(ContainsHeader)
                &&
                other.m_CSeq == m_CSeq
                &&
                string.Compare(other.m_Body, m_Body, false) == 0;
                //&&               
                //other.Length == Length;
        }

        #endregion

        #region Operators

        public static bool operator ==(RtspMessage a, RtspMessage b)
        {
            return object.ReferenceEquals(b, null) ? object.ReferenceEquals(a, null) : a.Equals(b);
        }

        public static bool operator !=(RtspMessage a, RtspMessage b) { return (a == b).Equals(false); }

        #endregion
    }
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtspMessage class is correct
    /// </summary>
    internal class RtspMessgeUnitTests
    {

        public void TestRequestsSerializationAndDeserialization()
        {
            string TestLocation = "rtsp://someServer.com", TestBody = "Body Data ! 1234567890-A";

            foreach (Media.Rtsp.RtspMethod method in Enum.GetValues(typeof(Media.Rtsp.RtspMethod)))
            {
                using (Media.Rtsp.RtspMessage request = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request))
                {
                    request.Location = new Uri(TestLocation);

                    request.RtspMethod = method;

                    request.Version = 7;

                    request.CSeq = 7;

                    byte[] bytes = request.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspMethod == request.RtspMethod &&
                        serialized.Location == request.Location &&
                        serialized.CSeq == request.CSeq &&
                        serialized.Location == request.Location) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Request Serialization Testing Failed!");
                        }
                    }

                    //Check again with Wildcard (*)
                    request.Location = Media.Rtsp.RtspMessage.Wildcard;

                    bytes = request.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspMethod == request.RtspMethod &&
                        serialized.Location == request.Location &&
                        serialized.CSeq == request.CSeq &&
                        serialized.Location == request.Location) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Request Serialization Testing Failed With Wildcard Location!");
                        }
                    }
                    
                    //Test again with a body
                    request.Body = TestBody;

                    bytes = request.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspStatusCode == request.RtspStatusCode &&
                        serialized.CSeq == request.CSeq &&
                        serialized.Version == request.Version &&
                        string.Compare(serialized.Body, TestBody, false) == 0) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed With Body!");
                        }
                    }

                    //Test again without a CSeq
                    request.RemoveHeader(Media.Rtsp.RtspHeaders.CSeq);

                    bytes = request.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspStatusCode == request.RtspStatusCode &&
                        serialized.CSeq == request.CSeq &&
                        serialized.Version == request.Version &&
                        string.Compare(serialized.Body, TestBody, false) == 0) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed Without CSeq!");
                        }
                    }

                }
            }
        }

        public void TestResponsesSerializationAndDeserialization()
        {

            string TestBody = "Body Data ! 1234567890-A";

            foreach (Media.Rtsp.RtspStatusCode statusCode in Enum.GetValues(typeof(Media.Rtsp.RtspStatusCode)))
            {
                using(Media.Rtsp.RtspMessage response = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response)
                {
                    Version = 7,
                    CSeq = 7,
                    RtspStatusCode = statusCode
                })
                {
                    byte[] bytes = response.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspStatusCode == response.RtspStatusCode &&
                        serialized.CSeq == response.CSeq &&
                        serialized.Version == response.Version) ||
                        false == serialized.IsComplete || false == response.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed!");
                        }
                    }

                    if (response.CanHaveBody)
                    {
                        //Test again with a body
                        response.Body = TestBody;

                        bytes = response.ToBytes();

                        using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                        {
                            if (false == (serialized.RtspStatusCode == response.RtspStatusCode &&
                            serialized.CSeq == response.CSeq &&
                            serialized.Version == response.Version &&
                            string.Compare(serialized.Body, response.Body, false) == 0) ||
                            false == serialized.IsComplete || false == response.IsComplete)
                            {
                                throw new Exception("Response Serialization Testing Failed With Body!");
                            }
                        }
                    }

                    //Test again without a CSeq
                    response.RemoveHeader(Media.Rtsp.RtspHeaders.CSeq);

                    bytes = response.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (false == (serialized.RtspStatusCode == response.RtspStatusCode &&
                        serialized.CSeq == response.CSeq &&
                        serialized.Version == response.Version &&
                        string.Compare(serialized.Body, response.Body, false) == 0) ||
                        false == serialized.IsComplete || false == response.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed Without CSeq!");
                        }
                    }
                }
            }
        }

        public void TestHeaderSerializationAndDeserialization()
        {

            string TestHeaderName = "h", TestHeaderValue = "v", TestBody = "Body Data ! 1234567890-A";

            using (Media.Rtsp.RtspMessage response = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response)
            {
                Version = 7,
                CSeq = 7,
                RtspStatusCode = (Media.Rtsp.RtspStatusCode)7
            })
            {
                //Add a header which should be ignored
                response.SetHeader(string.Empty, null);

                if (response.HeaderCount > 1) throw new Exception("Invalid Header Allowed");

                //Add a header which should be ignored
                response.AppendOrSetHeader(null, string.Empty);

                if (response.HeaderCount > 1) throw new Exception("Invalid Header Allowed");

                //Add a header which should not be ignored and should not be serialized
                response.AppendOrSetHeader(TestHeaderName, null);

                //Ensure the count is respected
                if (response.HeaderCount != 2) throw new Exception("Header Without Value Not Allowed");

                byte[] bytes = response.ToBytes();

                using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                {
                    if (serialized.RtspStatusCode != response.RtspStatusCode ||
                    serialized.CSeq != response.CSeq ||
                    serialized.Version != response.Version ||
                        //There must only be one header
                    serialized.HeaderCount != 2 ||
                        //The TestHeaderName must not be present because it was not given a value
                    false == serialized.ContainsHeader(TestHeaderName) ||
                        //Both must be complete
                        false == serialized.IsComplete || false == response.IsComplete)
                    {
                        throw new Exception("Response Header Serialization Testing Failed!");
                    }
                }
                
                //Set the value now
                response.AppendOrSetHeader(TestHeaderName, TestHeaderValue);

                bytes = response.ToBytes();

                using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                {
                    if (serialized.RtspStatusCode != response.RtspStatusCode ||
                    serialized.CSeq != response.CSeq &&
                    serialized.Version != response.Version ||
                        //There must only be one header
                    serialized.HeaderCount != 2 ||
                    false == serialized.ContainsHeader(TestHeaderName) ||
                        //The TestHeaderValue must be exactly the same
                    string.Compare(serialized[TestHeaderName], TestHeaderValue, false) != 0 ||
                        //Both must be complete
                        false == serialized.IsComplete || false == response.IsComplete)
                    {
                        throw new Exception("Response Header Serialization Testing Failed!");
                    }
                }

                if (response.CanHaveBody)
                {
                    //Test again with a body
                    response.Body = TestBody;

                    bytes = response.ToBytes();

                    using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                    {
                        if (serialized.RtspStatusCode != response.RtspStatusCode ||
                        serialized.CSeq != response.CSeq ||
                        serialized.Version != response.Version ||
                        string.Compare(serialized.Body, response.Body) != 0 ||
                            //Both must be complete
                        false == serialized.IsComplete || false == response.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed With Body!");
                        }
                    }
                }

                //Test again without a CSeq
                response.RemoveHeader(Media.Rtsp.RtspHeaders.CSeq);

                bytes = response.ToBytes();

                using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
                {
                    if (serialized.RtspStatusCode != response.RtspStatusCode ||
                        serialized.CSeq != response.CSeq ||
                        serialized.Version != response.Version ||
                        string.Compare(serialized.Body, response.Body, false) != 0 ||
                        //Both must be complete
                        false == serialized.IsComplete || false == response.IsComplete)
                    {
                        throw new Exception("Response Serialization Testing Failed Without CSeq!");
                    }
                }
            }
        }

        public void TestMessageSerializationAndDeserializationFromHexString()
        {
            //Make a byte[] from the hex string
            byte[] bytes = Media.Common.Extensions.String.StringExtensions.HexStringToBytes("525453502f312e3020323030204f4b0d0a435365633a20310d0a5075626c69633a2044455343524942452c2054454152444f574e2c2053455455502c20504c41592c2050415553450d0a0d0a");

            //Make a message from the bytes
            using (Media.Rtsp.RtspMessage serialized = new Media.Rtsp.RtspMessage(bytes))
            {
                //Ensure the message length is not larger then the binary length
                if (serialized.Length > bytes.Length) throw new Exception("Length Test Failed");

                //Because the message is a response it may not have a CSeq
                //Look closely.... 'Csec'
                if (serialized.RtspMessageType != Media.Rtsp.RtspMessageType.Response && 
                    (serialized.CSeq >= 0 || false == serialized.IsComplete)) throw new Exception("TestInvalidMessageDeserializationFromString Failed!");

                //Todo test making a hex string... 
                //Notes Binary needs a ToHexString method...
                //string toHex = BitConverter.ToString(serialized.ToBytes());

            }
        }

        public void TestMessageSerializationAndDeserializationFromString()
        {
            string TestMessage = @"ANNOUNCE / RTSP/1.0\n\n";

            using (Media.Rtsp.RtspMessage message = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.RtspMessageType != Media.Rtsp.RtspMessageType.Request ||
                               message.RtspMethod != Media.Rtsp.RtspMethod.ANNOUNCE ||
                               message.Version != 1.0) throw new Exception("Did not output expected result for invalid message");
            }

            //Change the message, Include a single header with a value
            TestMessage = "GET_PARAMETER * RTSP/1.0\n\nTest: Value\n\n";

            using (Media.Rtsp.RtspMessage message = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.RtspMessageType != Media.Rtsp.RtspMessageType.Request ||
                    message.RtspMethod != Media.Rtsp.RtspMethod.GET_PARAMETER ||
                    message.Version != 1.0 ||
                    message.HeaderCount != 1 ||
                    message.GetHeader("Test") != "Value") throw new Exception("Did not output expected result for invalid request");
            }

            //Change the message, don't specify a location
            TestMessage = "DESCRIBE / RTSP/1.0\nSession:\n\n";

            using (Media.Rtsp.RtspMessage message = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.RtspMessageType != Media.Rtsp.RtspMessageType.Request ||
                    message.RtspMethod != Media.Rtsp.RtspMethod.DESCRIBE ||
                    message.Location.OriginalString != "/" ||
                    message.Version != 1.0 || message.HeaderCount != 1) throw new Exception("Did not output expected result for invalid request");
            }

            //Change the message, include a location and some headers, use a duplicate cSeq
            TestMessage = "SETUP rtsp://server.com/foo/bar/baz.rm RTSP/1.0\nCSeq: 302\rCseq: 304\rRequire: funky-feature\rFunky-Parameter: funkystuff\n"; ;

            using (Media.Rtsp.RtspMessage message = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.RtspMessageType != Media.Rtsp.RtspMessageType.Request ||
                    message.RtspMethod != Media.Rtsp.RtspMethod.SETUP ||
                    message.Location.OriginalString != "rtsp://server.com/foo/bar/baz.rm" ||
                    message.Version != 1.0 ||
                    message.CSeq != 304 ||
                    message.HeaderCount != 3) throw new Exception("Did not output expected result for invalid request");
            }

            //Change the message, Introduce leading garbadge, Testing only single character's to end the lines of the headers
            TestMessage = "!@#$%^&*()_+-=\rRTSP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: funky-feature\n";

            using (Media.Rtsp.RtspMessage message = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                //After parsing a message with only \n or \r as end lines the resulting output will be longer because it will now have \r\n (unless modified)
                //It must never be less but it can be equal to.

                if (message.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    message.Version != 1.0 ||
                    message.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    message.CSeq != 302 ||
                    message.HeaderCount != 2 ||
                    output.Length <= message.Length) throw new Exception("Invalid response output length");

            }

            //Change the message, use white space and a combination of \r, \n and both to end the headers
            TestMessage = "RTSP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            //The body portion of further test message's
            string TestBody = "Body Data ! 1234567890-A";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    response.CSeq != 302 ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }


            //Change the message, don't white space but do use a combination of \r, \n and both to end the headers
            TestMessage = "RTSP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: funky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    response.CSeq != 302 ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                    string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check soon to be depreceated leading white space support in the headers..
            TestMessage = "RTSP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    response.CSeq != 302 ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check corner case of leading white space
            TestMessage = "RTSP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    response.CSeq != 302 ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check corner case of leading white space
            TestMessage = "RTSP/1.0 551 Option not supported\nWord-Of-The-Day: The Fox Jumps Over\r\tthe brown dog.\rCSeq:\r302\nUnsupported:\r \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.RtspStatusCode != Media.Rtsp.RtspStatusCode.OptionNotSupported ||
                    response.CSeq != 302 ||
                    response.HeaderCount != 4 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check that Parent protocol messages can be parsed...
            TestMessage = "HTTP/1.0 4510 Whatever\nWord-Of-The-Day: The Fox Jumps Over\r\tthe brown dog.\rCSeq:\r307\nUnsupported:\r \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Rtsp.RtspMessage response = Media.Rtsp.RtspMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.ParsedProtocol != "HTTP" ||
                    response.RtspMessageType != Media.Rtsp.RtspMessageType.Response ||
                    response.Version != 1.0 ||
                    response.StatusCode != 4510 ||
                    response.CSeq != 307 ||
                    response.HeaderCount != 4 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }
           
        }

        //Should be in RtspHeaders..
        public void TestParseTransportHeader()
        {

            string testVector = @"Transport: RTP/AVP/UDP;unicast;client_port=10000-10001;mode=""PLAY""";

            //Transport: RTP/AVP;multicast;destination=232.248.50.1;source=10.0.57.24;port=18888-18889;ttl=255

            int ssrc, ttl,
                rtpServerPort, rtcpServerPort,
                rtpClientPort, rtcpClientPort;

            bool unicast, multicast,
                interleaved;

            byte dataChannel, controlChannel;

            System.Net.IPAddress sourceIp, destinationIp;

            //never give protocol, some weird client or server may have this wonderful datum at random places.
            string protocol,
                mode;

            if (false == Media.Rtsp.RtspHeaders.TryParseTransportHeader(testVector, out ssrc, out sourceIp, out rtpServerPort, out rtcpServerPort, out rtpClientPort, out rtcpClientPort, out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast, out destinationIp, out ttl))
            {
                throw new Exception("Unexpected TryParseTransportHeader result");
            }

            if (false == unicast) throw new Exception("unicast");

            if (rtpClientPort != 10000) throw new Exception("rtpClientPort");

            if (rtcpClientPort != 10001) throw new Exception("rtcpClientPort");
            
            if (string.Compare(mode, "\"PLAY\"") != 0) throw new Exception("mode");

            testVector = @"Transport: RTP/AVP;multicast;destination=232.248.50.1;source=10.0.57.24;port=18888-18889;ttl=255";

            if (false == Media.Rtsp.RtspHeaders.TryParseTransportHeader(testVector, out ssrc, out sourceIp, out rtpServerPort, out rtcpServerPort, out rtpClientPort, out rtcpClientPort, out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast, out destinationIp, out ttl))
            {
                throw new Exception("Unexpected TryParseTransportHeader result");
            }

            if (false == multicast) throw new Exception("multicast");

            //Warning where as it should be client_port.

            //this example uses 'port' to specify both client and server.
            if (rtpServerPort != 18888) throw new Exception("rtpClientPort");

            if (rtcpServerPort != 18889) throw new Exception("rtcpClientPort");

            if (rtpClientPort != 18888) throw new Exception("rtpClientPort");

            if (rtcpClientPort != 18889) throw new Exception("rtcpClientPort");

            if (sourceIp.ToString() != "10.0.57.24") throw new Exception("sourceIp");

            if (destinationIp.ToString() != "232.248.50.1") throw new Exception("sourceIp");

            if (ttl != 255) throw new Exception("ttl");
        }

        public void TestCompleteFrom()
        {
            using (Media.Rtsp.RtspMessage message = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response))
            {
                message.RtspStatusCode = Media.Rtsp.RtspStatusCode.OK;

                //Set the cseq through the SetHeader method ...
                message.SetHeader(Media.Rtsp.RtspHeaders.CSeq, 34.ToString());

                //Ensure that worked
                if (message.CSeq != 34) throw new InvalidOperationException("Message CSeq not set correctly with SetHeader.");

                //Include the session header
                message.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");
                
                //This header should be included (it contains an invalid header directly after the end line data)
                message.SetHeader(Media.Rtsp.RtspHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");
                
                //This header should be included
                message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aRTSP/1.0");
                
                //This header should be ignored
                message.SetHeader("$", string.Empty);
                
                //Set the date header
                message.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

                //Create a buffer from the message
                byte[] buffer = message.Prepare().ToArray();

                //Cache the size of the buffer and the offset in parsing it.
                int size = buffer.Length, offset;

                //Test for every possible offset in the message
                for (int i = 0; i < size; ++i)
                {
                    //Reset the offset
                    offset = 0;

                    //Complete a message in chunks
                    using (Media.Rtsp.RtspMessage toComplete = new Rtsp.RtspMessage(Media.Common.MemorySegment.EmptyBytes))
                    {

                        //Store the sizes encountered
                        List<int> chunkSizes = new List<int>();

                        int currentSize = size;

                        //While data remains
                        while (currentSize > 0)
                        {
                            //Take a random sized chunk of at least 1 byte
                            int chunkSize = Utility.Random.Next(1, currentSize);

                            //Store the size of the chunk
                            chunkSizes.Add(chunkSize);

                            //Make a segment to that chunk
                            using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
                            {
                                //Keep track of how much data was just used to complete the message using that chunk
                                int justUsed = toComplete.CompleteFrom(null, chunkData);

                                //Ensure the chunk was totally consumed
                                if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

                                //Move the offset
                                offset += chunkSize;

                                //Decrese size
                                currentSize -= chunkSize;
                            }

                            //Do another iteration
                        }

                        //Verify the message
                        if (toComplete.IsComplete != message.IsComplete || 
                            toComplete.RtspStatusCode != message.RtspStatusCode ||
                            toComplete.CSeq != message.CSeq ||
                            toComplete.Version != message.Version ||
                            toComplete.HeaderCount != message.HeaderCount ||
                            toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0)) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

                        //The header UserAgent should be different as it contains an invalid header in the message
                        //Todo determine if this should be overlooked in Equals?
                        //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
                    }
                }
            }
        }

        public void TestCompleteFromWithBody()
        {
            using (Media.Rtsp.RtspMessage message = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response, 1.0, Media.Rtsp.RtspMessage.DefaultEncoding)
                        {
                            RtspStatusCode = Media.Rtsp.RtspStatusCode.OK,
                            CSeq = Media.Utility.Random.Next(byte.MinValue, int.MaxValue),
                            UserAgent = "$UserAgent $007\r\n$\0\0\aRTSP/1.0",
                            Body = "$00Q\r\n$\0:\0"
                        })
            {
                //Shoudn't matter
                message.RtspStatusCode = Media.Rtsp.RtspStatusCode.OK;

                //Set the cseq through the SetHeader method ...
                message.SetHeader(Media.Rtsp.RtspHeaders.CSeq, 34.ToString());

                //Ensure that worked
                if (message.CSeq != 34) throw new InvalidOperationException("Message CSeq not set correctly with SetHeader.");

                //Include the session header
                message.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");

                //This header should be included (it contains an invalid header directly after the end line data)
                message.SetHeader(Media.Rtsp.RtspHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");

                //This header should be included
                message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aRTSP/1.0");

                //This header should be ignored
                message.SetHeader("$", string.Empty);

                //Set the date header
                message.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

                //Create a buffer from the message
                byte[] buffer = message.Prepare().ToArray();

                //Cache the size of the buffer and the offset in parsing it.
                int size = buffer.Length, offset;

                //Test for every possible offset in the message
                for (int i = 0; i < size; ++i)
                {
                    //Reset the offset
                    offset = 0;

                    //Complete a message in chunks
                    using (Media.Rtsp.RtspMessage toComplete = new Rtsp.RtspMessage(Media.Common.MemorySegment.EmptyBytes))
                    {

                        //Store the sizes encountered
                        List<int> chunkSizes = new List<int>();

                        int currentSize = size;

                        //While data remains
                        while (currentSize > 0)
                        {
                            //Take a random sized chunk of at least 1 byte
                            int chunkSize = Utility.Random.Next(1, currentSize);

                            //Store the size of the chunk
                            chunkSizes.Add(chunkSize);

                            //Make a segment to that chunk
                            using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
                            {
                                //Keep track of how much data was just used to complete the message using that chunk
                                int justUsed = toComplete.CompleteFrom(null, chunkData);

                                //Ensure the chunk was totally consumed
                                if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

                                //Move the offset
                                offset += chunkSize;

                                //Decrese size
                                currentSize -= chunkSize;
                            }

                            //Do another iteration
                        }

                        //Verify the message
                        if (toComplete.IsComplete != message.IsComplete || 
                            toComplete.RtspStatusCode != message.RtspStatusCode ||
                            toComplete.CSeq != message.CSeq ||
                            toComplete.Version != message.Version ||
                            toComplete.HeaderCount != message.HeaderCount ||
                            toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0) ||
                            string.Compare(toComplete.Body, message.Body, false) != 0) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

                        //The header UserAgent should be different as it contains an invalid header in the message
                        //Todo determine if this should be overlooked in Equals?
                        //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
                    }
                }
            }
        }

        public void TestCompleteFromWith0LengthBody()
        {
            using (Media.Rtsp.RtspMessage message = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response, 1.0, Media.Rtsp.RtspMessage.DefaultEncoding)
            {
                RtspStatusCode = Media.Rtsp.RtspStatusCode.OK,
                CSeq = Media.Utility.Random.Next(byte.MinValue, int.MaxValue),
                UserAgent = "$UserAgent $007\r\n$\0\0\aRTSP/1.0",
                Body = string.Empty
            })
            {

                //Set the Content-Length
                message.SetHeader(Media.Rtsp.RtspHeaders.ContentLength, (0).ToString());

                //Set the Content-Encoding
                message.SetHeader(Media.Rtsp.RtspHeaders.ContentEncoding, message.ContentEncoding.WebName);

                //Shoudn't matter
                message.RtspStatusCode = Media.Rtsp.RtspStatusCode.OK;

                //Set the cseq through the SetHeader method ...
                message.SetHeader(Media.Rtsp.RtspHeaders.CSeq, 34.ToString());

                //Ensure that worked
                if (message.CSeq != 34) throw new InvalidOperationException("Message CSeq not set correctly with SetHeader.");

                //Include the session header
                message.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");

                //This header should be included (it contains an invalid header directly after the end line data)
                message.SetHeader(Media.Rtsp.RtspHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");

                //This header should be included
                message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aRTSP/1.0");

                //This header should be ignored
                message.SetHeader("$", string.Empty);

                //This header should not be ignored, it's a multiline value
                message.SetHeader("Word of the day", "The quick brown fox \r\tJumps over the lazy dog.");

                //Set the date header
                message.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

                //Create a buffer from the message
                byte[] buffer = message.Prepare().ToArray();

                //Cache the size of the buffer and the offset in parsing it.
                int size = buffer.Length, offset;

                //Test for every possible offset in the message
                for (int i = 0; i < size; ++i)
                {
                    //Reset the offset
                    offset = 0;

                    //Complete a message in chunks
                    using (Media.Rtsp.RtspMessage toComplete = new Rtsp.RtspMessage(Media.Common.MemorySegment.EmptyBytes))
                    {
                        //Store the sizes encountered
                        List<int> chunkSizes = new List<int>();

                        int currentSize = size;

                        //While data remains
                        while (currentSize > 0)
                        {
                            //Take a random sized chunk of at least 1 byte
                            int chunkSize = Utility.Random.Next(1, currentSize);

                            //Store the size of the chunk
                            chunkSizes.Add(chunkSize);

                            //Make a segment to that chunk
                            using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
                            {
                                //Keep track of how much data was just used to complete the message using that chunk
                                int justUsed = toComplete.CompleteFrom(null, chunkData);

                                //Ensure the chunk was totally consumed
                                if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

                                //Move the offset
                                offset += chunkSize;

                                //Decrese size
                                currentSize -= chunkSize;
                            }

                            //Do another iteration
                        }


                        //Verify the message
                        if (toComplete.IsComplete != message.IsComplete ||
                            toComplete.RtspStatusCode != message.RtspStatusCode ||
                            toComplete.CSeq != message.CSeq ||
                            toComplete.Version != message.Version ||
                            toComplete.HeaderCount != message.HeaderCount ||
                            toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0) ||
                            string.Compare(toComplete.Body, message.Body) != 0) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

                        //The header UserAgent should be different as it contains an invalid header in the message
                        //Todo determine if this should be overlooked in Equals?
                        //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
                    }
                }
            }
        }
    }
}
