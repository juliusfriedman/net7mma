﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Http
{
    /// <summary>
    /// The status codes utilized in RFC7231 or HttpMessages given in response to a request.
    /// https://tools.ietf.org/html/rfc7231#section-6
    /// </summary>
    public enum HttpStatusCode
    {
        Unknown = 0,
        // 1xx Informational.
        Continue = 100,

        // 2xx Success.
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,

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
        HttpVersionNotSupported = 505,
        OptionNotSupported = 551,
    }

    /// <summary>
    /// Enumeration to describe the available Http Methods, used in requests.
    /// https://tools.ietf.org/html/rfc7231#section-4
    /// </summary>
    public enum HttpMethod
    {
        UNKNOWN,
        OPTIONS,
        CONNECT,
        DELETE,
        GET,
        HEAD,
        PATCH,
        POST,
        PUT,
        TRACE
    }

    /// <summary>
    /// Enumeration to indicate the type of HttpMessage
    /// </summary>
    public enum HttpMessageType
    {
        Invalid = 0,
        Request = 1,
        Response = 2,
    }

    /// <summary>
    /// Base class of HttpRequest and HttpResponse
    /// </summary>
    public class HttpMessage : Common.BaseDisposable, Common.IPacket
    {
        #region Statics

        //Should probably also out the MultipartContent created.
        public static HttpMessage CreateMultipart(out MultipartContent multiPartcontent, byte[] boundary, System.Text.Encoding encoding = null, params Tuple<string, string, byte[], byte[]>[] contents)
        {
            throw new NotImplementedException();

            //Should use MultipartContent...

            multiPartcontent = new MultipartContent(boundary);

            encoding = encoding ?? DefaultEncoding;

            HttpMessage result = new HttpMessage();

            List<byte> data = new List<byte>();

            result.SetHeader(HttpHeaders.ContentType, "multipart/mixed; boundary=" + encoding.GetString(boundary));

            byte[] boundaryPrefix = encoding.GetBytes("--");

            //Iterate each content item
            foreach (var content in contents)
            {
                //Don't append null items or items without any data
                if (content == null || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(content.Item4)) continue;

                //Output Headers of Content part

                //Item1 = Content-Disposition
                //Disposition
                if (false == string.IsNullOrWhiteSpace(content.Item1)) data.AddRange(encoding.GetBytes(HttpHeaders.ContentDisposition + ":" + content.Item1));

                //Item2 = Content-Type
                if (false == string.IsNullOrWhiteSpace(content.Item1)) data.AddRange(encoding.GetBytes(HttpHeaders.ContentType + ":" + content.Item2));

                //The actual data.
                data.AddRange(content.Item4);

                //The boundary
                data.AddRange(boundaryPrefix);
                data.AddRange(content.Item3 ?? boundary);
            }

            //Set the body
            result.Body = encoding.GetString(data.ToArray());

            return result;
        }

        /// <summary>
        /// 0 = HeaderName
        /// 1 = Space Character
        /// 2 = Colon Character (Seperator)
        /// 3 = HeaderValue
        /// 4 = EndLine
        /// </summary>
        internal const string DefaultHeaderFormat = "{0}{2}{1}{3}{4}";

        //Using a space here causes hell to open with VLC and QuickTime.
        //"{0}{1}{2}{1}{3}{1}{4}";
        //Or here
        //"{0}{2}{1}{3}{1}{4}"; 

        public static Uri Wildcard = new Uri("*", UriKind.RelativeOrAbsolute);

        public const double DefaultVersion = 1.0;

        //Used to format the version string
        public static string VersionFormat = "0.0";

        //System encoded 'Carriage Return' => \r and 'New Line' => \n
        internal const string CRLF = "\r\n";

        //The scheme of Uri's of RtspMessage's
        public const string TransportScheme = "http"; //System.Uri.UriSchemeHttp;

        public const int TransportDefaultPort = 80;

        public const string SecureTransportScheme = "https"; //System.Uri.UriSchemeHttps;

        public const int SecureTransportDefaultPort = 443;

        //The maximum amount of bytes any HttpMessage can contain before a Content-Length header.
        public const int MaximumLength = 8192;

        //String which identifies a Http Request or Response
        public const string MessageIdentifier = "HTTP";

        //String which can be used to delimit a HttpMessage for preprocessing
        internal static string[] HeaderLineSplit = new string[] { CRLF };

        //String which is used to split Header values of the HttpMessage
        internal static char[] HeaderNameValueSplit = new char[] { (char)Common.ASCII.Colon };

        //String which is used to split Header values of the HttpMessage
        internal static char[] SpaceSplit = new char[] { (char)Common.ASCII.Space };

        //Should be instance variable and calulcated from Protocol
        internal static int MinimumStatusLineSize = 9; //'HTTP/X.X ' 

        public static readonly Encoding DefaultEncoding = System.Text.Encoding.UTF8;

        public static HttpMessage FromString(string data, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new InvalidOperationException("data cannot be null or whitespace.");

            if (encoding == null) encoding = HttpMessage.DefaultEncoding;

            return new HttpMessage(encoding.GetBytes(data), 0, encoding);
        }

        #endregion

        #region Fields

        public readonly string Protocol;

        protected bool m_StatusLineParsed, m_HeadersParsed;

        internal protected char[] m_EncodedLineEnds = Media.Common.UTF8.LineEndingCharacters,
            m_EncodedWhiteSpace = Media.Common.UTF8.WhiteSpaceCharacters,
            m_EncodedForwardSlash = Media.Common.UTF8.ForwardSlashCharacters,
            m_EncodedColon = Media.Common.UTF8.ColonCharacters,
            m_EncodedSemiColon = Media.Common.UTF8.SemiColonCharacters;

        string m_HeaderFormat = DefaultHeaderFormat, m_StringWhiteSpace, m_StringEndLine, m_StringColon, m_StringSemiColon;

        //Should expose string in format for outside parsing...

        protected double m_Version;

        protected int m_StatusCode;

        /// <summary>
        /// The body of the message
        /// </summary>
        internal protected string m_ReasonPhrase, m_Body = string.Empty;

        //Should be a Thesarus to support duplicates.
        /// <summary>
        /// Generic.Dictionary containing the headers of the HttpMessage
        /// </summary>
        readonly protected Dictionary<string, string> m_Headers = new Dictionary<string, string>(), m_EntityHeaders = new Dictionary<string, string>();

        //readonly ValueType...

        /// <summary>
        /// The Date and Time the message was created.
        /// </summary>
        public readonly DateTime Created = DateTime.UtcNow;

        /// <summary>
        /// The method of the message
        /// </summary>
        public string MethodString = string.Empty;

        /// <summary>
        /// Gets the <see cref="HttpMethod"/> which can be parsed from the <see cref="MethodString"/>
        /// </summary>
        public HttpMethod HttpMethod
        {
            get
            {
                HttpMethod parsed = HttpMethod.UNKNOWN;

                if (false == string.IsNullOrWhiteSpace(MethodString)) Enum.TryParse<HttpMethod>(MethodString, true, out parsed);

                return parsed;
            }
            set { MethodString = value.ToString(); }
        }

        /// <summary>
        /// The location of the message which is not usually utilized in responses.
        /// </summary>
        public Uri Location;

        protected Encoding m_HeaderEncoding = DefaultEncoding, m_ContentDecoder = DefaultEncoding;

        //Buffer to place data which is not complete
        protected System.IO.MemoryStream m_Buffer;

        //Set when parsing the first line if not already parsed, indicates the position of the beginning of the header data in m_Buffer.
        protected int m_HeaderOffset = 0;

        //Caches the content-length when parsed.
        protected int m_ContentLength = -1;

        //long m_RawLength = 0;

        #endregion

        #region Properties

        //Todo...

        //MaximumHeaders

        //MaximumLength

        /// <summary>
        /// Gets or sets any ReasonPhrase assoicaited with the message. If the <see cref="MessageType"/> is equal to Response...
        /// </summary>
        public string ReasonPhrase
        {
            get { return m_ReasonPhrase; }
            set
            {
                //Maybe should allow setting even though its not used...
                if (MessageType == HttpMessageType.Response) m_ReasonPhrase = value;
            }
        }

        /// <summary>
        /// Indicates if the instance will be disposed when Dispose is called.
        /// </summary>
        public bool IsPersistent
        {
            get
            {
                return ShouldDispose == false;
            }
            set
            {
                ShouldDispose = value == false;
            }
        }

        /// <summary>
        /// Gets or Sets the string associated with the formatting of the headers
        /// </summary>
        public string HeaderFormat
        {
            get { return m_HeaderFormat; }
            internal protected set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("The Header Format must not be null or consist only of Whitespace");

                m_HeaderFormat = value;
            }
        }

        /// <summary>
        /// Indicates if the Headers have been parsed completely.
        /// </summary>
        public bool HeadersParsed { get { return m_HeadersParsed; } }

        /// <summary>
        /// Indicates if the StatusLine has been parsed completely.
        /// </summary>
        public bool StatusLineParsed { get { return m_StatusLineParsed; } }

        //Used for GetContentDecoder 
        public bool FallbackToDefaultEncoding { get; set; }

        /// <summary>
        /// Indicates if invalid headers will be allowed to be added to the message.
        /// </summary>
        public bool AllowInvalidHeaders { get; set; }

        /// <summary>
        /// Indicates the UserAgent of this HttpRquest
        /// </summary>
        public String UserAgent
        {
            get { return GetHeader(HttpHeaders.UserAgent); }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(); SetHeader(HttpHeaders.UserAgent, value); }
        }

        public int StatusCode
        {
            get { return m_StatusCode; }
            set
            {
                m_StatusCode = value;

                if (false == CanHaveBody) m_Body = string.Empty;
            }
        }

        /// <summary>
        /// Indicates the StatusCode of the HttpResponse.
        ///  A value of 200 or less usually indicates success.
        /// </summary>
        public HttpStatusCode HttpStatusCode
        {
            get { return (HttpStatusCode)m_StatusCode; }
            set { StatusCode = (int)value; }
        }


        public double Version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        /// <summary>
        /// The length of the HttpMessage in bytes.
        /// (Calculated from the values parsed so some whitespace may be omitted, usually within +/- 4 bytes of the actual length)
        /// </summary>
        public int Length
        {

            //TODO See m_RawLength;

            get
            {
                if (IsDisposed && false == IsPersistent) return 0;

                try
                {
                    int length = 0, lineEndsLength = m_EncodedLineEnds.Length, whitespaceLength = m_HeaderEncoding.GetByteCount(m_EncodedWhiteSpace);

                    if (MessageType == HttpMessageType.Request || MessageType == HttpMessageType.Invalid)
                    {
                        length += m_HeaderEncoding.GetByteCount(HttpMethod.ToString());

                        length += whitespaceLength;

                        length += m_HeaderEncoding.GetByteCount(Location == null ? HttpMessage.Wildcard.ToString() : Location.ToString());

                        length += whitespaceLength;

                        length += m_HeaderEncoding.GetByteCount(HttpMessage.MessageIdentifier);

                        length += whitespaceLength;

                        length += m_HeaderEncoding.GetByteCount(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture));

                        length += lineEndsLength;
                    }
                    else if (MessageType == HttpMessageType.Response)
                    {
                        length += m_HeaderEncoding.GetByteCount(HttpMessage.MessageIdentifier);

                        length += whitespaceLength;

                        length += m_HeaderEncoding.GetByteCount(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture));

                        length += whitespaceLength;

                        length += m_HeaderEncoding.GetByteCount(((int)HttpStatusCode).ToString());

                        if (false == string.IsNullOrWhiteSpace(m_ReasonPhrase))
                        {

                            length += whitespaceLength;

                            length += m_HeaderEncoding.GetByteCount(m_ReasonPhrase);
                        }

                        length += lineEndsLength;
                    }

                    //Should also count headers when body is empty... and then add body length

                    //m_Headers.Count *  means each header has a ':' and spacing sequence, + lineEndsLength for the end headersequence
                    return length + (string.IsNullOrEmpty(m_Body) ? 0 : m_HeaderEncoding.GetByteCount(m_Body)) + (m_Headers.Count > 0 ? m_Headers.Count * (1 + lineEndsLength) + lineEndsLength + m_Headers.Sum(s => m_HeaderEncoding.GetByteCount(s.Key) + m_HeaderEncoding.GetByteCount(s.Value)) : 0);

                    //return length + (string.IsNullOrEmpty(m_Body) ? 0 : m_HeaderEncoding.GetByteCount(m_Body)) + PrepareHeaders().Count();
                }
                catch (InvalidOperationException)
                {
                    return Length; //length
                }
                catch (NullReferenceException)
                {
                    if (IsDisposed) return 0;

                    return Length;
                }
                catch { throw; }
            }
        }

        /// <summary>
        /// Indicates if the Message can have a Body
        /// </summary>
        public virtual bool CanHaveBody
        {
            get
            {
                //http://greenbytes.de/tech/webdav/rfc2616.html#rfc.section.4.4

                if (StatusCode >= 100 && StatusCode <= 199) return false;

                switch (HttpStatusCode)
                {
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.NotModified:
                    case HttpStatusCode.Found:
                        return false;
                    default: return true;
                }
            }
        }

        //Todo, should be binary
        /// <summary>
        /// The body of the HttpMessage
        /// </summary>
        public string Body
        {
            get { return m_Body; }
            set
            {
                //Ensure the body is allowed
                if (false == CanHaveBody) throw new InvalidOperationException("Messages with StatusCode of NotModified or Found MUST not have a Body.");

                m_Body = value;

                if (string.IsNullOrWhiteSpace(m_Body))
                {
                    RemoveHeader(HttpHeaders.ContentLength);

                    RemoveHeader(HttpHeaders.ContentEncoding);

                    m_ContentDecoder = null;

                    m_ContentLength = 0;
                }
                else
                {

                    //Should not be done for Multipart or Chunked...

                    Encoding contentEncoding = ContentEncoding;

                    //Get the length of the body
                    m_ContentLength = contentEncoding.GetByteCount(m_Body);

                    //Ensure all requests end with a CRLF
                    //if (false == m_Body.EndsWith(CRLF)) m_Body += CRLF;

                    //Set the Content-Length
                    SetHeader(HttpHeaders.ContentLength, m_ContentLength.ToString());

                    //Set the Content-Encoding
                    SetHeader(HttpHeaders.ContentEncoding, contentEncoding.WebName);

                    contentEncoding = null;
                }
            }
        }

        /// <summary>
        /// Indicates if this HttpMessage is a request or a response
        /// </summary>
        public HttpMessageType MessageType { get; internal protected set; }

        /// <summary>
        /// Gets the Content-Length of this HttpMessage, if found and parsed; otherwise -1.
        /// </summary>
        public int ContentLength
        {
            get
            {
                ParseContentLength(m_HeadersParsed);

                return m_ContentLength;
            }
            internal protected set
            {
                //Use the unsigned representation
                SetHeader(HttpHeaders.ContentLength, ((uint)(m_ContentLength = value)).ToString());
            }
        }

        public int HeaderCount { get { return m_Headers.Count; } }

        public int EntityHeaderCount { get { return m_EntityHeaders.Count; } }

        public int TotalHeaderCount { get { return HeaderCount + EntityHeaderCount; } }

        /// <summary>
        /// Accesses the header value 
        /// </summary>
        /// <param name="header">The header name</param>
        /// <returns>The header value</returns>
        public string this[string header]
        {
            get { return GetHeader(header); }
            set { SetHeader(header, value); }
        }        

        /// <summary>
        /// Gets or Sets the encoding of the headers of this HttpMessage. (Defaults to UTF-8).
        /// When a non-ASCII character set is used the MIME encoded word syntax <see href="https://tools.ietf.org/html/rfc2231">rfc2231</see> shall be used.
        /// </summary
        public Encoding HeaderEncoding
        {
            get { return m_HeaderEncoding; }
            set
            {
                if (IsDisposed && false == IsPersistent) return;

                if (m_HeaderEncoding == value) return;

                m_HeaderEncoding = value;

                //Should set headers to indicate..

                //Re-Encode values used in the header

                m_EncodedLineEnds = m_HeaderEncoding.GetChars(Media.Common.UTF8.LineEndingBytes);

                m_EncodedWhiteSpace = m_HeaderEncoding.GetChars(Media.Common.UTF8.WhiteSpaceBytes);

                m_EncodedColon = m_HeaderEncoding.GetChars(Media.Common.UTF8.ColonBytes);

                m_EncodedSemiColon = m_HeaderEncoding.GetChars(Media.Common.UTF8.SemiColonBytes);

                m_EncodedForwardSlash = m_HeaderEncoding.GetChars(Media.Common.UTF8.ForwardSlashBytes);

                CacheStrings();
            }
        }

        /// <summary>
        /// Gets or Sets the encoding of this HttpMessage. (Defaults to UTF-8)
        /// When set the `Content-Encoding` header is also set to the 'WebName' of the given Encoding.
        /// </summary>
        public Encoding ContentEncoding
        {
            get { return ParseContentEncoding(false == m_HeadersParsed, FallbackToDefaultEncoding); }
            set
            {
                if (m_ContentDecoder == value) return;

                m_ContentDecoder = value;

                SetHeader(HttpHeaders.ContentEncoding, m_ContentDecoder.WebName);
            }
        }

        /// <summary>
        /// Indicates when the HttpMessage was transferred if sent.
        /// </summary>
        public DateTime? Transferred { get; set; }

        /// <summary>
        /// Indicates if the HttpMessage is complete
        /// </summary>
        public virtual bool IsComplete
        {
            get
            {
                //Disposed is complete 
                if (IsDisposed && false == IsPersistent) return IsDisposed;

                //If the status line was not parsed
                if (false == m_StatusLineParsed &&
                    MessageType == HttpMessageType.Invalid ||  //All requests must have a StatusLine OR
                    m_Buffer != null && m_Buffer.CanRead && // Be parsing the StatusLine
                    m_Buffer.Length <= MinimumStatusLineSize) return false;

                //Messages without complete header sections are not complete
                if (false == ParseHeaders()) return false;

                //Check for Trailer header (maybe TE)
                string trailerHeader = GetHeader(HttpHeaders.Trailer);

                if (false == string.IsNullOrWhiteSpace(trailerHeader) && false == ContainsHeader(trailerHeader)) return false;

                //If the message can have a body
                if (CanHaveBody)
                {
                    //Determine if the body is present
                    bool hasNullBody = string.IsNullOrWhiteSpace(m_Body);

                    //Determine if the count of the octets in the body is greater than or equal to the supposed amount
                    return false == ParseContentLength(hasNullBody) && hasNullBody && m_ContentLength > 0 ? m_HeadersParsed : ContentEncoding.GetByteCount(m_Body) >= m_ContentLength;
                }

                //The message is complete
                return true;
            }
        }

        #endregion

        #region Constructor

        static HttpMessage()
        {
            if (false == UriParser.IsKnownScheme(HttpMessage.TransportScheme))
                UriParser.Register(new HttpStyleUriParser(), HttpMessage.TransportScheme, HttpMessage.TransportDefaultPort);
            
            if (false == UriParser.IsKnownScheme(HttpMessage.SecureTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), HttpMessage.SecureTransportScheme, HttpMessage.SecureTransportDefaultPort);
        }

        /// <summary>
        /// Reserved
        /// </summary>
        protected HttpMessage(string protocol = HttpMessage.MessageIdentifier) : base() { Protocol = protocol;}

        /// <summary>
        /// Constructs a HttpMessage
        /// </summary>
        /// <param name="messageType">The type of message to construct</param>
        public HttpMessage(HttpMessageType messageType, double? version = DefaultVersion, Encoding contentEncoding = null, bool shouldDispse = true, string protocol = HttpMessage.MessageIdentifier)
            : base(shouldDispse)
        {
            MessageType = messageType; 
            
            Version = version ?? DefaultVersion;

            if (contentEncoding != null) ContentEncoding = contentEncoding;

            m_StatusLineParsed = m_HeadersParsed = true;

            Protocol = protocol ?? string.Empty;

            CacheStrings();
        }

        /// <summary>
        /// Creates a HttpMessage from the given bytes
        /// </summary>
        /// <param name="bytes">The byte array to create the HttpMessage from</param>
        /// <param name="offset">The offset within the bytes to start creating the message</param>
        public HttpMessage(byte[] bytes, int offset = 0, Encoding encoding = null) : this(bytes, offset, bytes.Length - offset, encoding) { }

        public HttpMessage(Common.MemorySegment data, Encoding encoding = null) : this(data.Array, data.Offset, data.Count, encoding) { }

        public HttpMessage(byte[] data, int offset, int length, Encoding contentEncoding = null, bool shouldDispose = true, string protocol = HttpMessage.MessageIdentifier)
            : base(shouldDispose)
        {

            Protocol = protocol ?? string.Empty;

            CacheStrings();

            //Sanely

            length = Media.Common.Extensions.Math.MathExtensions.Clamp(length, 0, data.Length);

            //length could be > data.Length or offset could be allowed to be negitive...
            if (data == null || offset < 0 || length == 0)
            {
                return;
            }

            //use the supplied encoding if present.
            if (contentEncoding != null &&
                contentEncoding != ContentEncoding)
            {
                //Set the Content-Encoding header
                ContentEncoding = contentEncoding;
            }

            //Syntax, what syntax? there is no syntax ;)

            int start = offset, count = length;//, firstLineLength = -1;

            //Http in the encoding of the request
            //byte[] encodedIdentifier = Encoding.GetBytes(MessageIdentifier); int encodedIdentifierLength = encodedIdentifier.Length;

            //int requiredEndLength = 1; //2.0 specifies that CR and LF must be present

            //Skip any non character data.
            while (false == char.IsLetter((char)data[start]))
            {
                if (--count <= 0) return;
                ++start;
            }

            //Create the buffer
            m_Buffer = new System.IO.MemoryStream(count);

            //Write the data to the buffer
            m_Buffer.Write(data, start, count);

            //Attempt to parse the data given as a StatusLine.
            if (false == ParseStatusLine()) return;

            //A valid looking first line has been found...
            //Parse the headers and body if present

            if (m_HeaderOffset < count)
            {
                //The count of how many bytes are used to take up the header is given by
                //The amount of bytes (after the first line PLUS the length of CRLF in the encoding of the message) minus the count of the bytes in the packet
                int remainingBytes = count - m_HeaderOffset;

                //If the scalar is valid
                if (remainingBytes > 0 && m_HeaderOffset + remainingBytes <= count)
                {
                    //Position the buffer, indicate no headers remain in the buffer
                    //m_Buffer.Position = m_HeaderOffset = 0;

                    //Write that data
                    //m_Buffer.Write(data, start + headerStart, remainingBytes);

                    //Ensure the length is set
                    //m_Buffer.SetLength(remainingBytes);

                    //Position the buffer
                    //m_Buffer.Position = 0;

                    //Parse the headers and body
                    ParseBody();
                }
            } //All messages SHOULD have at least a CSeq header.
            //else MessageType = HttpMessageType.Invalid;
        }

        /// <summary>
        /// Creates a HttpMessage by copying the properties of another.
        /// </summary>
        /// <param name="other">The other HttpMessage</param>
        public HttpMessage(HttpMessage other)
        {
            MethodString = other.MethodString;

            m_Body = other.m_Body;
            m_Headers = other.m_Headers;
            m_StatusCode = other.m_StatusCode;
            m_Version = other.m_Version;

            m_HeaderEncoding = other.m_HeaderEncoding;
            m_ContentDecoder = other.m_ContentDecoder;

            CacheStrings();
        }

        ~HttpMessage() { Dispose(); }

        #endregion

        #region Methods

        void CacheStrings()
        {
            m_StringWhiteSpace = m_HeaderEncoding.GetString(m_HeaderEncoding.GetBytes(m_EncodedWhiteSpace));

            m_StringColon = m_HeaderEncoding.GetString(m_HeaderEncoding.GetBytes(m_EncodedColon));

            m_StringSemiColon = m_HeaderEncoding.GetString(m_HeaderEncoding.GetBytes(m_EncodedSemiColon));

            m_StringEndLine = m_HeaderEncoding.GetString(m_HeaderEncoding.GetBytes(m_EncodedLineEnds));
        }

        protected void DisposeBuffer()
        {
            if (m_Buffer != null && m_Buffer.CanWrite) m_Buffer.Dispose();

            m_Buffer = null;
        }

        virtual protected bool ParseStatusLine(bool force = false)
        {

            if (IsDisposed && false == IsPersistent) return m_StatusLineParsed;

            if (m_StatusLineParsed && false == force) return true;

            //Dont rely on the message type obtained previously
            //if (MessageType != HttpMessageType.Invalid) return true;

            //Determine how much data is present.
            int count = (int)m_Buffer.Length, index = -1;


            //Todo, should be Protocol.Length + 6

            //Ensure enough data is availble to parse.
            if (count <= MinimumStatusLineSize) return false;

            //Always from the beginning of the buffer.
            m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);

            //Get what we believe to be the first line
            //... containing the method to be applied to the resource,the identifier of the resource, and the protocol version in use;
            //Todo, should use EncodingExtensions if the header is allowed to be in an alternate format.
            //Should store and only parse as needed?
            string StatusLine;// = Media.Common.ASCII.ReadLine(m_Buffer, Encoding);

            long read;

            bool sawDelemit;

            //If it was not present then do not parse further
            if (false == (sawDelemit = Media.Common.Extensions.Encoding.EncodingExtensions.ReadDelimitedDataFrom(HeaderEncoding, m_Buffer, m_EncodedLineEnds, m_Buffer.Length, out StatusLine, out read, true)) && read < MinimumStatusLineSize)
            {
                MessageType = HttpMessageType.Invalid;

                return false;
            }

            //Cache the length of what we read.

            //m_HeaderOffset is still set when parsing fails but that shouldn't be an issue because it's reset when called again.

            //Take the ByteCount of what was read
            m_HeaderOffset = m_HeaderEncoding.GetByteCount(StatusLine);//(int)--read;

            //Trim any whitespace
            StatusLine = StatusLine.TrimStart();

            //Determine where `HTTP` occurs.
            index = StatusLine.IndexOf(Protocol);

            //use the index of the Protocol to determine the MessageType
            switch (index)
            {
                //If it was not present then do not parse further
                case -1:
                    {
                        MessageType = HttpMessageType.Invalid;

                        return false;
                    }
                case 0: MessageType = HttpMessageType.Response; break;
                default: MessageType = HttpMessageType.Request; break;
            }

            //Make an array of sub strings delemited by ' ', should max at 3 entries
            string[] parts = StatusLine.Split(SpaceSplit, 3);

            //There must be 2 parts or parsing will not occur.
            int partsLength = parts.Length;

            if (partsLength < 2) MessageType = HttpMessageType.Invalid;

            //switch>

            //Could assign version, then assign Method and Location
            if (MessageType == HttpMessageType.Request)
            {
                //C->S[0]SETUP[1]Http://example.com/media.mp4/streamid=0[2]Http/1.0()                  

                MethodString = parts[0].Trim();

                //Extract PrecisionNumber should use EncodingExtensions
                //UriDecode?

                string part = string.Empty;// = parts[2];

                if (string.IsNullOrWhiteSpace(MethodString) ||
                    false == Uri.TryCreate(parts[1], UriKind.RelativeOrAbsolute, out Location) || //Ensure Location and the version can be parsed.
                    partsLength > 2 && string.IsNullOrEmpty(part = parts[2]) || part.Length <= Protocol.Length || //assign part, check for null, empty or <= only the protocol string
                    false == double.TryParse(Media.Common.ASCII.ExtractPrecisionNumber(part.Substring(Protocol.Length)), //Try to parse the version at the protocol length
                            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m_Version))
                {
                    return false;
                }
            }
            else if (MessageType == HttpMessageType.Response)
            {
                //S->C[0]Http/1.0[1]200[2]OK()

                string part = parts[0];

                //Extract PrecisionNumber should use EncodingExtensions
                if (false == int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out m_StatusCode) ||
                    string.IsNullOrEmpty(part) || part.Length <= Protocol.Length || //Check for null, empty or <= only the protocol string
                    false == double.TryParse(Media.Common.ASCII.ExtractPrecisionNumber(part.Substring(Protocol.Length)), //Try to parse the version at the protocol length
                            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m_Version))
                {
                    return false;
                }

                //Must keep reason phrase..
                if (partsLength >= 3) m_ReasonPhrase = parts[2];

            }
            else if (m_Buffer.Length > MaximumLength) 
            {
                MessageType = HttpMessageType.Invalid;

                //Maybe should not dispose... (ShouldDispose)
                DisposeBuffer();
            }

            //The status line was parsed if the message is not invalid and the delemit was encountered (If not the reason phrase may be incomplete...)
            return m_StatusLineParsed = MessageType != HttpMessageType.Invalid && sawDelemit;
        }

        virtual protected bool ParseHeaders(bool force = false)
        {
            try
            {
                if (IsDisposed && false == IsPersistent) return m_HeadersParsed;

                //Headers were parsed if there is already a body.
                if (m_HeadersParsed && false == force) return true;

                //Need 2 empty lines to end the header section
                int emptyLine = 0; //This should be a class level varible

                //m_HeadersParsed should maintain the amount of emptyLines encountered through all parsing unless force is true.

                //Ensure at the beginning of the buffer.
                m_Buffer.Seek(m_HeaderOffset, System.IO.SeekOrigin.Begin);

                //Keep track of the position
                long position = m_Buffer.Position, max = m_Buffer.Length;

                //Reparsing should clear headers?
                if (force) m_Headers.Clear();

                bool readingValue = false;

                //Store the headerName 
                string headerName = null;

                //Header value to append to
                StringBuilder headerValue = new StringBuilder();

                long remains = 0, justRead = 0;

                bool sawDelemit = false;

                Exception encountered;

                string rawLine;

                string[] parts;

                //While we didn't find the end of the header section in the local call (buffer may be in use)
                while (false == IsDisposed && m_Buffer.CanRead && emptyLine <= 2 && (remains = max - position) > 0)
                {
                    //Determine if any of the delimits were found
                    sawDelemit = Media.Common.Extensions.Encoding.EncodingExtensions.ReadDelimitedDataFrom(HeaderEncoding, m_Buffer, m_EncodedLineEnds, remains, out rawLine, out justRead, out encountered, true);

                    //Check for the empty line
                    if (string.IsNullOrWhiteSpace(rawLine))
                    {
                        ////LWS means a new line in the value which can be safely ignored.
                        if (false == readingValue)
                        {
                            //Count the empty lines
                            ++emptyLine;
                        }

                        //Append the value to the last headerValue
                        headerValue.Append(rawLine);

                        //Stop parsing when an exception occurs (even if more data remains)
                        if (encountered != null) break;

                        //Do update the position to the position of the buffer
                        position = m_Buffer.Position; //don't use justRead, BinaryReader and ReadChars is another great function (Fallback encoder may backtrack, may also decide output buffer is too small based on the same back track)

                        //Do another iteration
                        continue;
                    }
                    else emptyLine = 1;

                    //We only want the first 2 sub strings to allow for headers which have a ':' in the data
                    //E.g. Rtp-Info: Http://....
                    parts = rawLine.Split(HeaderNameValueSplit, 2);

                    //not a valid header
                    if (parts.Length <= 1 || string.IsNullOrWhiteSpace(parts[0]))
                    {

                        readingValue = true;

                        headerValue.Append(rawLine);

                        //If there is not a header name and there more data try to read the next line
                        if (remains > 0) goto UpdatePosition;

                        //When only 1 char is left it could be `\r` or `\n` which is another line end
                        //Or `$` 'End Delemiter' (Reletive End Support (No RFC Draft [yet]) it indicates an end of section.

                        //back track
                        m_Buffer.Seek(justRead, System.IO.SeekOrigin.End);

                        break;
                    }
                    else
                    {
                        //If there was a previous header and value being prepared
                        if (false == string.IsNullOrWhiteSpace(headerName))
                        {
                            //Set the value
                            SetHeader(headerName, headerValue.ToString().Trim());

                            //Clear the buffer
                            headerValue.Clear();

                            //Indicate no longer reading the value
                            readingValue = false;
                        }

                        //Set the headername
                        headerName = parts[0];

                        //Append the header value
                        headerValue.Append(parts[1]);

                        //Determine if the readingValue should be set
                        readingValue = string.IsNullOrWhiteSpace(headerValue.ToString());
                    }


                UpdatePosition:

                    //Move the position
                    position = m_Buffer.Position; //Just ignore justRead for now

                    //Could peek at the buffer of the memory stream to determine if the next char is related to the header...
                }

                //If there is a non null value for headerName the headerValue has not been written
                if (false == string.IsNullOrWhiteSpace(headerName))
                {
                    SetHeader(headerName, headerValue.ToString().Trim());
                }

                //There may be control characters from the last header still in the buffer, (ParseBody handles this)            

                //Check that an end header section was seen or that the delemit was encountered
                return m_HeadersParsed = emptyLine >= 2 || sawDelemit;
            }
            catch { return false; }
        }

        virtual protected bool ParseTrailer()
        {
            try
            {
                //Need 2 empty lines to end the trailer section
                int emptyLine = 0; //This should be a class level varible

                //Keep track of the position
                long position = m_Buffer.Position, max = m_Buffer.Length;

                bool readingValue = false;

                //Store the headerName 
                string headerName = null;

                //Header value to append to
                StringBuilder headerValue = new StringBuilder();

                long remains = 0, justRead = 0;

                bool sawDelemit = false;

                Exception encountered;

                string rawLine;

                string[] parts;

                //While we didn't find the end of the header section in the local call (buffer may be in use)
                while (false == IsDisposed && m_Buffer.CanRead && emptyLine <= 2 && (remains = max - position) > 0)
                {
                    //Determine if any of the delimits were found
                    sawDelemit = Media.Common.Extensions.Encoding.EncodingExtensions.ReadDelimitedDataFrom(HeaderEncoding, m_Buffer, m_EncodedLineEnds, remains, out rawLine, out justRead, out encountered, true);

                    //Check for the empty line
                    if (string.IsNullOrWhiteSpace(rawLine))
                    {
                        ////LWS means a new line in the value which can be safely ignored.
                        if (false == readingValue)
                        {
                            //Count the empty lines
                            ++emptyLine;
                        }

                        //Append the value to the last headerValue
                        headerValue.Append(rawLine);

                        //Stop parsing when an exception occurs (even if more data remains)
                        if (encountered != null) break;

                        //Do update the position to the position of the buffer
                        position = m_Buffer.Position; //don't use justRead, BinaryReader and ReadChars is another great function (Fallback encoder may backtrack, may also decide output buffer is too small based on the same back track)

                        //Do another iteration
                        continue;
                    }
                    else emptyLine = 1;

                    //We only want the first 2 sub strings to allow for headers which have a ':' in the data
                    //E.g. Rtp-Info: Http://....
                    parts = rawLine.Split(HeaderNameValueSplit, 2);

                    //not a valid header
                    if (parts.Length <= 1 || string.IsNullOrWhiteSpace(parts[0]))
                    {

                        readingValue = true;

                        headerValue.Append(rawLine);

                        //If there is not a header name and there more data try to read the next line
                        if (remains > 0) goto UpdatePosition;

                        //When only 1 char is left it could be `\r` or `\n` which is another line end
                        //Or `$` 'End Delemiter' (Reletive End Support (No RFC Draft [yet]) it indicates an end of section.

                        //back track
                        m_Buffer.Seek(justRead, System.IO.SeekOrigin.End);

                        break;
                    }
                    else
                    {
                        //If there was a previous header and value being prepared
                        if (false == string.IsNullOrWhiteSpace(headerName))
                        {
                            //Set the value
                            SetEntityHeader(headerName, headerValue.ToString().Trim());

                            //Clear the buffer
                            headerValue.Clear();

                            //Indicate no longer reading the value
                            readingValue = false;
                        }

                        //Set the headername
                        headerName = parts[0];

                        //Append the header value
                        headerValue.Append(parts[1]);

                        //Determine if the readingValue should be set
                        readingValue = string.IsNullOrWhiteSpace(headerValue.ToString());
                    }


                UpdatePosition:

                    //Move the position
                    position = m_Buffer.Position; //Just ignore justRead for now

                    //Could peek at the buffer of the memory stream to determine if the next char is related to the header...
                }

                //If there is a non null value for headerName the headerValue has not been written
                if (false == string.IsNullOrWhiteSpace(headerName))
                {
                    SetEntityHeader(headerName, headerValue.ToString().Trim());
                }

                //There may be control characters from the last header still in the buffer, (ParseBody handles this)            

                //Check that an end header section was seen or that the delemit was encountered
                return emptyLine >= 2 || sawDelemit;
            }
            catch { return false; }
        }

        internal protected virtual bool ParseContentLength(bool force = false)
        {
            if (IsDisposed && false == IsPersistent) return false;

            if (false == force && m_ContentLength >= 0) return false;

            //See if there is a Content-Length header
            string contentLength = GetHeader(HttpHeaders.ContentLength);

            //If the value was null or empty then do nothing
            //If a message is received with both a Transfer-Encoding header field and a Content-Length header field, the latter must be ignored.
            if (string.IsNullOrWhiteSpace(contentLength) || false == string.IsNullOrWhiteSpace(GetHeader(HttpHeaders.TransferEncoding))) return false;

            //If there is a header parse it's value.
            //Should use EncodingExtensions
            if (false == int.TryParse(Media.Common.ASCII.ExtractNumber(contentLength), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out m_ContentLength))
            {
                //There was not a content-length in the format '1234'

                //Determine if alternate format parsing is allowed...

                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the <see cref="ContentEncoding"/> for the 'Content-Encoding' header if found.
        /// </summary>
        /// <param name="raiseWhenNotFound">If true and the requested encoding cannot be found an exception will be thrown.</param>
        /// <returns>The <see cref="System.Text.Encoding"/> requested or the <see cref="System.Text.Encoding.Default"/> if the reqeusted could not be found.</returns>
        internal protected virtual Encoding ParseContentEncoding(bool force = false, bool raiseWhenNotFound = false)
        {
            //If the message is disposed then no parsing can occur
            if (IsDisposed && false == IsPersistent) return null;

            if (force == false && m_ContentDecoder != null) return m_ContentDecoder;

            //Get the content encoding required by the headers for the body
            string contentEncoding = GetHeader(HttpHeaders.ContentEncoding);

            //Use the existing content decoder if set.
            System.Text.Encoding contentDecoder = m_ContentDecoder ?? m_HeaderEncoding;

            //If there was a content-Encoding header then set it now;
            if (false == string.IsNullOrWhiteSpace(contentEncoding))
            {
                //Check for the requested encoding
                contentEncoding = contentEncoding.Trim();

                System.Text.EncodingInfo requested = System.Text.Encoding.GetEncodings().FirstOrDefault(e => string.Compare(e.Name, contentEncoding, false, System.Globalization.CultureInfo.InvariantCulture) == 0);

                if (requested != null) contentDecoder = requested.GetEncoding();
                else if (true == raiseWhenNotFound) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(contentEncoding, "The given message was encoded in a Encoding which is not present on this system and no fallback encoding was acceptible to decode the message. The tag has been set the value of the requested encoding");
                else contentDecoder = System.Text.Encoding.Default;
            }

            //Use the default encoding if given utf8
            if (contentDecoder.WebName == m_HeaderEncoding.WebName) contentDecoder = m_HeaderEncoding;

            return m_ContentDecoder = contentDecoder;
        }

        internal protected virtual bool ParseBody(out int remaining, bool force = false) //bool requireContentLength = true
        {
            remaining = 0;

            //If the message is disposed then the body is parsed.
            if (IsDisposed && false == IsPersistent) return false;

            //If the message is invalid or
            if (false == force && MessageType == HttpMessageType.Invalid) return true; //or the message is complete then return true

            //If no headers could be parsed then don't parse the body
            if (false == ParseHeaders()) return false;

            //Transfer-Encoding chunked will be present if headers are parsed and the message is chunked.

            //If the message cannot have a body it is parsed.
            if (false == CanHaveBody) return true;

            //If there was no buffer or an unreadable buffer then no parsing can occur
            if (m_Buffer == null || false == m_Buffer.CanRead) return false;

            //Quite possibly should be long
            int max = (int)m_Buffer.Length;

            //Empty body or no ContentLength
            if (m_ContentLength == 0)
            {
                //m_Body = string.Empty;

                return true;
            }

            int position = (int)m_Buffer.Position,
                   available = max - position;

            //Calculate how much data remains based on the ContentLength
            //remaining = m_ContentLength - m_Body.Length;

            if (available == 0) return false;

            //Get the decoder to use for the body
            Encoding decoder = ParseContentEncoding(false, FallbackToDefaultEncoding);

            if (decoder == null) return false;

            int existingBodySize = decoder.GetByteCount(m_Body);


            //available should only be used if chunked encoding is not used.

            remaining = ParseContentLength() ? m_ContentLength - existingBodySize : available;

            if (remaining == 0) return true;

            //Get the array of the memory stream
            byte[] buffer = m_Buffer.GetBuffer();

            //Ensure no control characters were left from parsing of the header values if more data is available then remains
            //only do this one time and only if the Body was not already started parsing.
            if (existingBodySize == 0 && Array.IndexOf<char>(m_EncodedLineEnds, decoder.GetChars(buffer, position, 1)[0]) >= 0)
            {
                ++position;

                --available;
            }

            //Get the body of the message which is the amount of bytes remaining based on the current position in parsing
            if (available > 0)
            {
                //If there is chunked encoding it must be read in chunks.
                if (Version > 1.0 && GetHeader(HttpHeaders.TransferEncoding) == "chunked")
                {
                    int chunkSize = -1;

                    long lastPos = position;

                    //While data is available and there is positive or 0 chunk size
                    while (available > 0 && (chunkSize = ParseChunk(ref position, available)) >= 0)
                    {
                        //Recalculate what is available to consume
                        available = max - position;

                        //Consume the line ends /r/n after a chunk size
                        while (available > 0 && Array.IndexOf<char>(m_EncodedLineEnds, decoder.GetChars(buffer, position, 1)[0]) >= 0)
                        {
                            ++position;

                            --available;
                        }

                        //If the chunk is larger than what is availble then stop
                        if (chunkSize > available)
                        {
                            //Could consume what is availalbe and keep state.

                            remaining = chunkSize - available;

                            m_Buffer.Seek(m_HeaderOffset = (int)lastPos, System.IO.SeekOrigin.Begin);

                            return true;
                        }

                        //Consume the chunk
                        if (chunkSize > 0)
                        {
                            m_Body += decoder.GetString(buffer, position, chunkSize);

                            //Move the position in the buffer
                            position += chunkSize;
                        }

                        //Recalculate what is availalbe to consume
                        available = max - position;

                        //Update the last position
                        lastPos = position;

                        //Consume the line ends /r/n after a chunk data
                        while (available > 0 && Array.IndexOf<char>(m_EncodedLineEnds, decoder.GetChars(buffer, position, 1)[0]) >= 0)
                        {
                            m_Buffer.Position = lastPos = ++position;

                            --available;
                        }
                    }

                    //Must read the last chunk with a 0 size
                    if (chunkSize != 0 && available > 0)
                    {
                        remaining = available;
                        m_Buffer.Seek(m_HeaderOffset = (int)lastPos, System.IO.SeekOrigin.Begin);
                        return true;
                    }
                    else if (chunkSize == 0)
                    {
                        //Trailer

                        string trailer = GetHeader(HttpHeaders.Trailer);

                        if (false == string.IsNullOrWhiteSpace(trailer) && false == ContainsHeader(trailer))
                        {
                            if (ParseTrailer())
                            {
                                //Remove the TransferEncoding header and set the content length
                                RemoveHeader(HttpHeaders.TransferEncoding); //should probably only removed chunked key.
                                ContentLength = decoder.GetByteCount(m_Body);
                            }
                        }
                    }

                    #region Notes

                    /*
                     
                     19.4.6 Introduction of Transfer-Encoding

                    HTTP/1.1 introduces the Transfer-Encoding header field (section 14.41). Proxies/gateways MUST remove any transfer-coding prior to forwarding a message via a MIME-compliant protocol.

                    A process for decoding the "chunked" transfer-coding (section 3.6) can be represented in pseudo-code as:

                           length := 0
                           read chunk-size, chunk-extension (if any) and CRLF
                           while (chunk-size > 0) {
                              read chunk-data and CRLF
                              append chunk-data to entity-body
                              length := length + chunk-size
                              read chunk-size and CRLF
                           }
                           read entity-header
                           while (entity-header not empty) {
                              append entity-header to existing header fields
                              read entity-header
                           }
                           Content-Length := length
                           Remove "chunked" from Transfer-Encoding
                     
                     */

                    #endregion

                }
                else //Content-Length style message
                {
                    if (existingBodySize == 0)
                        m_Body = decoder.GetString(buffer, position, Media.Common.Binary.Min(available, remaining));
                    else                     //Append to the existing body
                        m_Body += decoder.GetString(buffer, position, Media.Common.Binary.Min(available, remaining));
                }

                //No longer needed, and would interfere with CompleteFrom logic.
                DisposeBuffer();

                //Body was parsed or started to be parsed.
                return true;
            }

            //The body was not parsed
            return false;
        }

        internal protected virtual bool ParseBody(bool force = false)
        {
            if (IsDisposed && false == IsPersistent) return false;

            int remains;

            return ParseBody(out remains, force);
        }

        /// <summary>
        /// Creates a '<see cref="System.String"/>' representation of the HttpMessage including all binary data contained.
        /// </summary>
        /// <returns>A <see cref="System.String"/> which contains the entire message itself in the encoding of the HttpMessage</returns>
        public virtual string ToEncodedString()
        {
            if (IsDisposed && false == IsPersistent) goto Exit;
            if(HeaderEncoding != null) try
            {
                return HeaderEncoding.GetString(Prepare(true, true, false, false).ToArray())
                +
                ContentEncoding.GetString(Prepare(false, false, true, false).ToArray())
                +
                HeaderEncoding.GetString(Prepare(false, false, false, true).ToArray());
            }
            catch { goto Exit; }
        Exit:
            return string.Empty;
        }

        /// <summary>
        /// See <see cref="ToEncodedString"/>
        /// </summary>
        /// <returns>A string which contains the entire message itself in the encoding of the HttpMessage converted to the <see cref="System.Text.Encoding.Default"/> </returns>
        public override string ToString() { return System.Text.Encoding.Default.GetString(System.Text.Encoding.Default.GetBytes(ToEncodedString())); }

        /// <summary>
        /// Gets an array of all headers present in the HttpMessage
        /// </summary>
        /// <returns>The array containing all present headers</returns>
        public virtual IEnumerable<string> GetHeaders() { return m_Headers.Keys.ToList(); }

        public virtual IEnumerable<string> GetEntityHeaders() { return m_EntityHeaders.Keys.ToList(); }

        /// <summary>
        /// Gets a header value with cases insensitivity.
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <returns>The header value if found, otherwise null.</returns>
        internal string GetHeaderValue(string name, out string actualName)
        {
            actualName = null;
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return null;
            foreach (string headerName in GetHeaders())
                if (string.Compare(name, headerName, true) == 0) //headerName.Equals(name, StringComparison.OrdinalIgnoreCase);
                {
                    actualName = headerName;
                    return m_Headers[headerName];
                }
            return null;
        }

        internal string GetEntityHeaderValue(string name, out string actualName)
        {
            actualName = null;
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return null;
            foreach (string headerName in GetEntityHeaders())
                if (string.Compare(name, headerName, true) == 0) //headerName.Equals(name, StringComparison.OrdinalIgnoreCase);
                {
                    actualName = headerName;
                    return m_EntityHeaders[headerName];
                }
            return null;
        }

        public virtual string GetHeader(string name)
        {
            return GetHeaderValue(name, out name);
        }

        public virtual string GetEntityHeader(string name)
        {
            return GetEntityHeaderValue(name, out name);
        }

        /// <summary>
        /// Sets or adds a header value, the value given is not checked for validity.
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <param name="value">The value of the header</param>
        public virtual void SetHeader(string name, string value)
        {
            //If the name is no name then the value is not relevant
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return;

            //Unless all headers are allowed, validate the header name.
            if (false == AllowInvalidHeaders &&
                false == char.IsLetter(name[0])) return; // || false == string.IsNullOrWhiteSpace(value) && CountChars(value, m_StringColon) > 1, throw InvalidOperationException()

            //Trim any whitespace from the name
            name = name.Trim();

            //Keep a place to determine if the given name was already encountered in a different case
            string actualName = null;

            //value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

            //If the header with the same name has already been encountered set the value otherwise add the value
            if (ContainsHeader(name, out actualName)) m_Headers[actualName] = value;
            else
            {
                m_Headers.Add(name, value);

                OnHeaderAdded(name, value);
            }
        }

        public virtual void SetEntityHeader(string name, string value)
        {
            //If the name is no name then the value is not relevant
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return;

            //Unless all headers are allowed, validate the header name.
            if (false == AllowInvalidHeaders &&
                false == char.IsLetter(name[0])) return; // || false == string.IsNullOrWhiteSpace(value) && CountChars(value, m_StringColon) > 1, throw InvalidOperationException()

            //Trim any whitespace from the name
            name = name.Trim();

            //Keep a place to determine if the given name was already encountered in a different case
            string actualName = null;

            //value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

            //If the header with the same name has already been encountered set the value otherwise add the value
            if (ContainsEntityHeader(name, out actualName)) m_EntityHeaders[actualName] = value;
            else
            {
                m_EntityHeaders.Add(name, value);

                OnHeaderAdded(name, value);
            }
        }

        public virtual void AppendOrSetHeader(string name, string value) //string joiner
        {
            //Empty names are not allowed.
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return;

            //Check that invalid headers are not allowed and if so that there is a valid named header with a valid value.
            if (false == AllowInvalidHeaders &&  // || false == char.IsLetterOrDigit(value[0])
                false == char.IsLetter(name[0])) // || false == string.IsNullOrWhiteSpace(value) && CountChars(value, m_StringColon) > 1, throw InvalidOperationException()
            {
                //If not return
                return;
            }

            //The name given to append or set may be set in an alternate case
            string containedHeader = null;

            //Trim any whitespace from the name and value
            name = name.Trim();

            bool nullValue = string.IsNullOrWhiteSpace(value);

            value = nullValue ? string.Empty : value.Trim();

            //If not already contained then set, otherwise append the value given
            if (false == ContainsHeader(name, out containedHeader)) SetHeader(name, value); //joiner ?? 
            else if (false == nullValue)
            {
                string containedValue = m_Headers[containedHeader];

                m_Headers[containedHeader] = string.IsNullOrWhiteSpace(containedValue) ? value : containedValue + m_StringSemiColon + value; //Might be a list header with ,?
            }
        }

        public virtual void AppendOrSetEntityHeader(string name, string value) //string joiner
        {
            //Empty names are not allowed.
            if (IsDisposed && false == IsPersistent || string.IsNullOrWhiteSpace(name)) return;

            //Check that invalid headers are not allowed and if so that there is a valid named header with a valid value.
            if (false == AllowInvalidHeaders &&  // || false == char.IsLetterOrDigit(value[0])
                false == char.IsLetter(name[0])) // || false == string.IsNullOrWhiteSpace(value) && CountChars(value, m_StringColon) > 1, throw InvalidOperationException()
            {
                //If not return
                return;
            }

            //The name given to append or set may be set in an alternate case
            string containedHeader = null;

            //Trim any whitespace from the name and value
            name = name.Trim();

            bool nullValue = string.IsNullOrWhiteSpace(value);

            value = nullValue ? string.Empty : value.Trim();

            //If not already contained then set, otherwise append the value given
            if (false == ContainsEntityHeader(name, out containedHeader)) SetEntityHeader(name, value); //joiner ?? 
            else if (false == nullValue)
            {
                string containedValue = m_Headers[containedHeader];

                m_EntityHeaders[containedHeader] = string.IsNullOrWhiteSpace(containedValue) ? value : containedValue + m_StringSemiColon + value; //Might be a list header with ,?
            }
        }

        /// <summary>
        /// Indicates of the HttpMessage contains a header with the given name.
        /// </summary>
        /// <param name="name">The name of the header to find</param>
        /// <param name="headerName">The value which is actually the name of the header searched for</param>
        /// <returns>True if contained, otherwise false</returns>
        internal bool ContainsHeader(string name, out string headerName)
        {
            headerName = null;

            if (IsDisposed && false == IsPersistent) return false;

            if (string.IsNullOrWhiteSpace(name)) return false;

            //Get the header value of the given headerName
            string headerValue = GetHeaderValue(name, out headerName);

            //The name was contained if name is not null
            return headerName != null;
        }

        internal bool ContainsEntityHeader(string name, out string headerName)
        {
            headerName = null;

            if (IsDisposed && false == IsPersistent) return false;

            if (string.IsNullOrWhiteSpace(name)) return false;

            //Get the header value of the given headerName
            string headerValue = GetEntityHeaderValue(name, out headerName);

            //The name was contained if name is not null
            return headerName != null;
        }

        public virtual bool ContainsHeader(string name)
        {
            if (IsDisposed && false == IsPersistent) return false;

            return ContainsHeader(name, out name);
        }

        public virtual bool ContainsEntityHeader(string name)
        {
            if (IsDisposed && false == IsPersistent) return false;

            return ContainsEntityHeader(name, out name);
        }

        /// <summary>
        /// Removes a header from the HttpMessage
        /// </summary>
        /// <param name="name">The name of the header to remove</param>
        /// <returns>True if removed, false otherwise</returns>
        public virtual bool RemoveHeader(string name)
        {
            //If disposed then don't proceed
            if (IsDisposed && false == IsPersistent) return false;

            //If there is a null or empty header it is not contained.
            if (string.IsNullOrWhiteSpace(name)) return false;

            //Determine if the header is contained
            string headerValue = GetHeaderValue(name, out name);

            //If the stored header name  is null the header can be removed
            if (false == string.IsNullOrWhiteSpace(name))
            {
                //Store the result of the remove operation
                bool removed = m_Headers.Remove(name);

                //If the header was removed
                if (removed)
                {
                    //Implement the remove 
                    OnHeaderRemoved(name, headerValue);

                    //Don't reference the header any more
                    headerValue = null;
                }

                //return the result of the remove operation
                return removed;
            }

            //The header was not contained
            return false;
        }

        public virtual bool RemoveEntityHeader(string name)
        {
            //If disposed then don't proceed
            if (IsDisposed && false == IsPersistent) return false;

            //If there is a null or empty header it is not contained.
            if (string.IsNullOrWhiteSpace(name)) return false;

            //Determine if the header is contained
            string headerValue = GetEntityHeaderValue(name, out name);

            //If the stored header name  is null the header can be removed
            if (false == string.IsNullOrWhiteSpace(name))
            {
                //Store the result of the remove operation
                bool removed = m_Headers.Remove(name);

                //If the header was removed
                if (removed)
                {
                    //Implement the remove 
                    OnHeaderRemoved(name, headerValue);

                    //Don't reference the header any more
                    headerValue = null;
                }

                //return the result of the remove operation
                return removed;
            }

            //The header was not contained
            return false;
        }

        /// <summary>
        /// Called when a header is removed
        /// </summary>
        /// <param name="headerName"></param>
        protected virtual void OnHeaderRemoved(string headerName, string headerValue)
        {
            if (IsDisposed && false == IsPersistent) return;

            //If there is a null or empty header ignore
            if (string.IsNullOrWhiteSpace(headerName)) return;

            //The lower case invariant name and determine if action is needed
            switch (headerName.ToLowerInvariant())
            {
                case "content-encoding":
                    {
                        m_ContentDecoder = null;

                        break;
                    }
            }
        }

        //SupportsHeader, would help to eliminate unsupported headers...
        //Possibly static on each Message type implementation

        /// <summary>
        /// Called when a header is added
        /// </summary>
        /// <param name="headerName"></param>
        protected virtual void OnHeaderAdded(string headerName, string headerValue)
        {
            //Content-Length
            //Cseq
            //Etc
        }

        /// <summary>
        /// Creates a Packet from the HttpMessage which can be sent on the network, If the Location is null the <see cref="WildCardLocation will be used."/>
        /// </summary>
        /// <returns>The packet which represents this HttpMessage</returns>
        public virtual byte[] ToBytes()
        {
            if (IsDisposed && false == IsPersistent) return Common.MemorySegment.EmptyBytes;

            //List<byte> result = new List<byte>(HttpMessage.MaximumLength);

            //result.AddRange(PrepareStatusLine().ToArray());

            IEnumerable<byte> result = PrepareStatusLine();

            if (MessageType == HttpMessageType.Invalid) return result.ToArray();

            //Add the header bytes
            //result.AddRange(PrepareHeaders());

            result = Enumerable.Concat(result, PrepareHeaders());

            //Add the body bytes
            //result.AddRange(PrepareBody());

            result = Enumerable.Concat(result, PrepareBody());

            return result.ToArray();
        }

        /// <summary>
        /// Prepares the sequence of bytes which correspond to the options given
        /// </summary>
        /// <param name="includeStatusLine"></param>
        /// <param name="includeHeaders"></param>
        /// <param name="includeBody"></param>
        /// <returns></returns>
        public virtual IEnumerable<byte> Prepare(bool includeStatusLine, bool includeHeaders, bool includeBody, bool includeEntity)
        {
            if (includeStatusLine && includeHeaders && includeBody) return ToBytes();

            IEnumerable<byte> result = Media.Common.MemorySegment.EmptyBytes;

            if (includeStatusLine) result = PrepareStatusLine();

            if (includeHeaders) result = Enumerable.Concat(result, PrepareHeaders());

            if (includeBody) result = Enumerable.Concat(result, PrepareBody(m_EntityHeaders.Count > 0));

            if (includeEntity) result = Enumerable.Concat(result, PrepareEntityHeaders());

            return result;
        }

        /// <summary>
        /// Prepares the sequence of bytes which correspond to the Message in it's current state.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<byte> Prepare()
        {
            return Prepare(true, true, true, true);
        }

        /// <summary>
        /// Creates the sequence of bytes which corresponds to the StatusLine
        /// </summary>
        /// <param name="includeEmptyLine"></param>
        /// <returns></returns>
        public virtual IEnumerable<byte> PrepareStatusLine(bool includeEmptyLine = true)
        {
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;

            if (IsDisposed && false == IsPersistent) return sequence; //yield break;

            System.Text.Encoding headerEncoding = m_HeaderEncoding;

            if (headerEncoding == null) return sequence;

            //StatusLine?
            //foreach (byte b in headerEncoding.GetBytes(StatusLine)

            //switch

            if (MessageType == HttpMessageType.Request || MessageType == HttpMessageType.Invalid)
            {
                //foreach (byte b in headerEncoding.GetBytes(MethodString)) yield return b;

                IEnumerable<byte> whiteSpace = headerEncoding.GetBytes(m_EncodedWhiteSpace);

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(MethodString));

                //foreach (byte b in headerEncoding.GetBytes(m_EncodedWhiteSpace)) yield return b;

                sequence = Enumerable.Concat(sequence, whiteSpace);

                //UriEncode?

                //foreach (byte b in headerEncoding.GetBytes(Location == null ? HttpMessage.Wildcard.ToString() : Location.ToString())) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(Location == null ? HttpMessage.Wildcard.ToString() : Location.ToString()));

                //foreach (byte b in headerEncoding.GetBytes(m_EncodedWhiteSpace)) yield return b;

                sequence = Enumerable.Concat(sequence, whiteSpace);

                //Could skip conversion if default encoding.
                //foreach (byte b in headerEncoding.GetBytes(Protocol)) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(Protocol));

                //foreach (byte b in headerEncoding.GetBytes(m_EncodedForwardSlash)) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(m_EncodedForwardSlash));

                //foreach (byte b in headerEncoding.GetBytes(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture))) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (MessageType == HttpMessageType.Response)
            {
                //Could skip conversion if default encoding.
                //foreach (byte b in headerEncoding.GetBytes(Protocol)) yield return b;

                IEnumerable<byte> whiteSpace = headerEncoding.GetBytes(m_EncodedWhiteSpace);

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(Protocol));

                //foreach (byte b in headerEncoding.GetBytes(m_EncodedForwardSlash)) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(m_EncodedForwardSlash));

                //foreach (byte b in headerEncoding.GetBytes(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture))) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture)));

                //foreach (byte b in headerEncoding.GetBytes(m_EncodedWhiteSpace)) yield return b;

                sequence = Enumerable.Concat(sequence, whiteSpace);

                //foreach (byte b in headerEncoding.GetBytes(((int)HttpStatusCode).ToString())) yield return b;

                sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes((StatusCode).ToString()));

                //foreach (byte b in headerEncoding.GetBytes(StatusCode.ToString())/*.ToString*/) yield return b;

                if (false == string.IsNullOrWhiteSpace(m_ReasonPhrase))
                {
                    //foreach (byte b in headerEncoding.GetBytes(m_EncodedWhiteSpace)) yield return b;

                    sequence = Enumerable.Concat(sequence, whiteSpace);

                    //foreach (byte b in headerEncoding.GetBytes(m_ReasonPhrase)) yield return b;

                    sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(m_ReasonPhrase));
                }
            }

            //if (includeEmptyLine && m_EncodedLineEnds != null) foreach (byte b in headerEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

            if (includeEmptyLine && m_EncodedLineEnds != null) sequence = Enumerable.Concat(sequence, headerEncoding.GetBytes(m_EncodedLineEnds));

            headerEncoding = null;

            return sequence;
        }

        /// <summary>
        /// Creates the sequence of bytes which correspond to the Headers.
        /// </summary>
        /// <param name="includeEmptyLine"></param>
        /// <returns></returns>
        public virtual IEnumerable<byte> PrepareHeaders(bool includeEmptyLine = true)
        {
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;

            if (IsDisposed && false == IsPersistent) return sequence;

            //if (m_HeaderEncoding.WebName != "utf-8" && m_HeaderEncoding.WebName != "ascii") throw new NotSupportedException("Mime format is not yet supported.");

            //Could have a format string allowed here
            //If there is a format then the logic changes to format the string in the given format and then use the encoding to return the bytes.

            //e.g if(false == string.IsNullOrEmptyOrWhiteSpace(m_HeaderFormat)) { format header using format and then encode to bytes and return that }

            if (m_EncodedWhiteSpace == null) return sequence;

            IEnumerable<byte> whiteSpace = m_HeaderEncoding.GetBytes(m_EncodedWhiteSpace);

            //Write headers that have values
            foreach (KeyValuePair<string, string> header in m_Headers /*.OrderBy((key) => key.Key).Reverse()*/)
            {
                //if (string.IsNullOrWhiteSpace(header.Value)) continue;

                //Create the formated header and return the bytes for it
                //foreach (byte b in PrepareHeader(header.Key, header.Value)) yield return b;

                sequence = Enumerable.Concat(sequence, PrepareHeader(header.Key, header.Value));
            }

            if (includeEmptyLine && m_EncodedLineEnds != null) sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(m_EncodedLineEnds)); //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

            return sequence;
        }

        public virtual IEnumerable<byte> PrepareEntityHeaders(bool includeEmptyLine = false)
        {
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;

            if (IsDisposed && false == IsPersistent) return sequence;

            //if (m_HeaderEncoding.WebName != "utf-8" && m_HeaderEncoding.WebName != "ascii") throw new NotSupportedException("Mime format is not yet supported.");

            //Could have a format string allowed here
            //If there is a format then the logic changes to format the string in the given format and then use the encoding to return the bytes.

            //e.g if(false == string.IsNullOrEmptyOrWhiteSpace(m_HeaderFormat)) { format header using format and then encode to bytes and return that }

            //Write headers that have values
            foreach (KeyValuePair<string, string> header in m_EntityHeaders /*.OrderBy((key) => key.Key).Reverse()*/)
            {
                //if (string.IsNullOrWhiteSpace(header.Value)) continue;

                //Create the formated header and return the bytes for it
                //foreach (byte b in PrepareHeader(header.Key, header.Value)) yield return b;

                sequence = Enumerable.Concat(sequence, PrepareHeader(header.Key, header.Value));
            }

            if (includeEmptyLine && m_EncodedLineEnds != null) sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(m_EncodedLineEnds)); //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

            return sequence;
        }

        /// <summary>
        /// Uses the <see cref="m_HeaderFormat"/> to format the given header and value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>The bytes which are encoded in <see cref="m_HeaderEncoding"/></returns>
        internal protected virtual IEnumerable<byte> PrepareHeader(string name, string value)
        {
            return PrepareHeaderWithFormat(name, value, m_HeaderFormat);
        }

        /// <summary>
        /// Prepares a sequence of bytes using the given format
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        internal protected virtual IEnumerable<byte> PrepareHeaderWithFormat(string name, string value, string format)
        {
            if (IsDisposed && false == IsPersistent) return Common.MemorySegment.Empty;

            //When no format or when using the default header format use the optomized path
            if (string.IsNullOrWhiteSpace(format) || format == DefaultHeaderFormat)
            {
                IEnumerable<byte> sequence = m_HeaderEncoding.GetBytes(name);

                //foreach (byte b in m_HeaderEncoding.GetBytes(name)) yield return b;

                //foreach (byte b in m_EncodedColon) yield return b;

                sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(m_EncodedColon));

                if (false == string.IsNullOrWhiteSpace(value))
                {
                    //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedWhiteSpace)) yield return b;

                    sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(m_EncodedWhiteSpace));

                    //foreach (byte b in m_HeaderEncoding.GetBytes(value)) yield return b;

                    sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(value));
                }

                //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

                sequence = Enumerable.Concat(sequence, m_HeaderEncoding.GetBytes(m_EncodedLineEnds));

                return sequence;
            }

            return m_HeaderEncoding.GetBytes(string.Format(format, name, m_StringWhiteSpace, m_StringColon, value, m_StringEndLine));

            //Use the given format
            //foreach (byte b in m_HeaderEncoding.GetBytes(string.Format(format, name, m_StringWhiteSpace, m_StringColon, value, m_StringEndLine))) yield return b;
        }

        /// <summary>
        /// Creates the sequence of bytes which correspond to the Body.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<byte> PrepareBody(bool includeEmptyLine = false)
        {
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;

            if (IsDisposed && false == IsPersistent || false == CanHaveBody) return sequence;

            if (false == string.IsNullOrWhiteSpace(m_Body))
            {
                //foreach (byte b in ContentEncoding.GetBytes(m_Body)/*.Take(m_ContentLength)*/) yield return b;

                sequence = Enumerable.Concat(sequence, ContentEncoding.GetBytes(m_Body));
            }

            //includeEmptyLine = m_ContentLength < 0;

            if (includeEmptyLine && m_EncodedLineEnds != null) sequence = Enumerable.Concat(sequence, ContentEncoding.GetBytes(m_EncodedLineEnds));  //foreach (byte b in m_HeaderEncoding.GetBytes(m_EncodedLineEnds)) yield return b;

            return sequence;
        }

        //CookieCollection or CookieEnumerator
        IEnumerable<HttpCookie> GetCookies()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of all resourced used by the HttpMessage
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            //Call the base implementation
            base.Dispose();

            //If the message is persistent then leave the buffer in place.
            if (IsPersistent) return;

            //No longer needed.
            DisposeBuffer();

            //Clearing local references (Will change output of ToString())
            m_HeaderEncoding = m_ContentDecoder = null;

            m_EncodedColon = m_EncodedForwardSlash = m_EncodedLineEnds = m_EncodedSemiColon = m_EncodedWhiteSpace = null;

            m_HeaderFormat = m_StringWhiteSpace = m_StringColon = m_StringEndLine = m_Body = null;

            m_Headers.Clear();
        }

        public override int GetHashCode()
        {
            return Created.GetHashCode() ^ (int)((int)MessageType | (int)HttpMethod ^ (int)HttpStatusCode) ^ (string.IsNullOrWhiteSpace(m_Body) ? Length : m_Body.GetHashCode()) ^ (m_Headers.Count);
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is HttpMessage)) return false;

            HttpMessage other = obj as HttpMessage;

            //Fast path doesn't show true equality.
            //other.Created != Created

            return other.MessageType == MessageType
                &&
                other.Version == Version
                &&
                other.MethodString == MethodString
                //&&
                // other.m_Headers.Count == m_Headers.Count 
                &&
                other.GetHeaders().All(ContainsHeader)
                &&
                string.Compare(other.m_Body, m_Body, false) == 0;
            //&&               
            //other.Length == Length;
        }

        #endregion

        #region Operators

        public static bool operator ==(HttpMessage a, HttpMessage b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(HttpMessage a, HttpMessage b) { return false == (a == b); }

        #endregion

        #region IPacket

        public virtual bool IsCompressed { get { return false; } }

        DateTime Common.IPacket.Created
        {
            get { return Created; }
        }


        bool Common.IPacket.IsReadOnly
        {
            get { return false; }
        }

        long Common.IPacket.Length
        {
            get { return (long)Length; }
        }

        /// <summary>
        /// Completes the HttpMessage from either the buffer or the socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {
            if (IsDisposed && false == IsPersistent) return 0;

            bool hasSocket = socket != null, hasBuffer = false == buffer.IsDisposed && buffer.Count > 0;

            //If there is no socket or no data available in the buffer nothing can be done
            if (false == hasSocket && false == hasBuffer)
            {
                return 0;
            }

            //Don't check IsComplete because of the notion of how a HttpMessage can be received.
            //There may be additional headers which are available before the body          

            int received = 0;

            if (false == hasSocket)
            {
                //Create the buffer if it was null
                if (m_Buffer == null || false == m_Buffer.CanWrite)
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
                if (buffer != null && false == buffer.IsDisposed && buffer.Count > 0)
                {
                    //Write the new data
                    m_Buffer.Write(buffer.Array, buffer.Offset, received += buffer.Count);

                    //Go to the beginning
                    m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);
                }
            }

            //If the status line was not parsed return the number of bytes written, reparse if there are no headers parsed yet.
            if (false == ParseStatusLine(MessageType == HttpMessageType.Invalid || false == m_StatusLineParsed)) return received;
            else if(m_Buffer != null && m_Buffer.CanSeek) m_Buffer.Seek(m_HeaderOffset, System.IO.SeekOrigin.Begin); // Seek past the status line.

            //Determine if there can be and is a body already
            bool hasNullBody = CanHaveBody && string.IsNullOrWhiteSpace(m_Body);

            //Force the re-parsing of headers unless the body has started parsing.
            
            //CompleteFrom test fails because of this, works correctly in real world.

            //We don't have to reparse the headers unless the whole message is in the buffer, we can guess this by checking the length of the m_Buffer.Length to be >= Length
            //This is not reliable since the buffer could be larger than the length and still not have all data...

            if (false == ParseHeaders(hasNullBody && false == m_HeadersParsed)) return received;

            //Reparse any content-length if it was not already parsed or was a 0 value and the body is still null
            //if (m_ContentLength <= 0 && false == ParseContentLength(hasNullBody)) return received;

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
                if (false == ParseBody(out remaining, false) && remaining > 0)
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
                            m_Body += decoder.GetString(buffer.Array, offset, Media.Common.Binary.Min(remaining, justReceived));

                            //Decrement for what was justReceived
                            remaining -= justReceived;

                            //Increment for what was justReceived
                            received += justReceived;
                        }

                        //If any socket error occured besides a timeout or a block then stop trying to receive.
                        if (error != System.Net.Sockets.SocketError.Success || error != System.Net.Sockets.SocketError.TimedOut || error != System.Net.Sockets.SocketError.TryAgain) break;
                    }
                }
            }
            else ParseBody(true);

            //Return the amount of bytes consumed.
            return received;
        }

        int ParseChunk(ref int offset, int length) //out string extensions
        {
        //Top:
            if (length <= 0) return -1;

            string ChunkLine;

            long read;

            bool sawDelemit;

            //If it was not present then do not parse further
            if (false == (sawDelemit = Media.Common.Extensions.Encoding.EncodingExtensions.ReadDelimitedDataFrom(HeaderEncoding, m_Buffer, m_EncodedLineEnds, length, out ChunkLine, out read, true)))
            {
                return -1;
            }

            // /r or /n by itself, if there is another byte consume it.
            if (read == 1 && sawDelemit && length > 1)
            {
                ++offset;
                
                //No Recursion
                //--length;
                //goto Top;
                
                return ParseChunk(ref offset, --length); 
            }

            int ChunkLength = -1;

            //Split out any optional values proceeding the ChunkSize, there may be a chunk-extension (this is currently ignored)
            string[] parts = ChunkLine.TrimEnd().Split(HttpHeaders.ValueSplit, 1);

            //If there is a part which corresponds to the ChunkLength
            if (parts.Length > 0)
            {
                //Try to parse it, if parsing fails indicate such with -1
                if (false == int.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ChunkLength)) return -1;

                //Move the offset, parsing the ChunkLength succeeded
                offset += (int)read;
            }

            //Return the ChunkLength
            return ChunkLength;
        }

        #endregion
    }
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the HttpMessage class is correct
    /// </summary>
    internal class HttpMessgeUnitTests
    {
        public void TestRequestsSerializationAndDeserialization()
        {
            string TestLocation = "Http://someServer.com", TestBody = "Body Data ! 1234567890-A";

            foreach (Media.Http.HttpMethod method in Enum.GetValues(typeof(Media.Http.HttpMethod)))
            {
                using (Media.Http.HttpMessage request = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Request))
                {
                    request.Location = new Uri(TestLocation);

                    request.HttpMethod = method;

                    request.Version = 7;

                    byte[] bytes = request.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpMethod == request.HttpMethod &&
                        serialized.Location == request.Location &&
                        serialized.Location == request.Location) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Request Serialization Testing Failed!");
                        }
                    }

                    //Check again with Wildcard (*)
                    request.Location = Media.Http.HttpMessage.Wildcard;

                    bytes = request.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpMethod == request.HttpMethod &&
                        serialized.Location == request.Location &&
                        serialized.Location == request.Location) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Request Serialization Testing Failed With Wildcard Location!");
                        }
                    }

                    //Test again with a body
                    request.Body = TestBody;

                    bytes = request.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpStatusCode == request.HttpStatusCode &&
                        serialized.Version == request.Version &&
                        string.Compare(serialized.Body, TestBody, false) == 0) ||
                        false == serialized.IsComplete || false == request.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed With Body!");
                        }
                    }

                    bytes = request.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpStatusCode == request.HttpStatusCode &&
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

            foreach (Media.Http.HttpStatusCode statusCode in Enum.GetValues(typeof(Media.Http.HttpStatusCode)))
            {
                using (Media.Http.HttpMessage response = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Response)
                {
                    Version = 7,
                    HttpStatusCode = statusCode
                })
                {
                    byte[] bytes = response.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpStatusCode == response.HttpStatusCode &&
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

                        using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                        {
                            if (false == (serialized.HttpStatusCode == response.HttpStatusCode &&
                            serialized.Version == response.Version &&
                            string.Compare(serialized.Body, response.Body, false) == 0) ||
                            false == serialized.IsComplete || false == response.IsComplete)
                            {
                                throw new Exception("Response Serialization Testing Failed With Body!");
                            }
                        }
                    }

                    bytes = response.ToBytes();

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (false == (serialized.HttpStatusCode == response.HttpStatusCode &&
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

            using (Media.Http.HttpMessage response = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Response)
            {
                Version = 7,
                HttpStatusCode = (Media.Http.HttpStatusCode)7
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
                if (response.HeaderCount != 1) throw new Exception("Header Without Value Not Allowed");

                byte[] bytes = response.ToBytes();

                using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                {
                    if (serialized.HttpStatusCode != response.HttpStatusCode ||
                    serialized.Version != response.Version ||
                        //There must only be one header
                    serialized.HeaderCount != 1 ||
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

                using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                {
                    if (serialized.HttpStatusCode != response.HttpStatusCode ||
                    serialized.Version != response.Version ||
                        //There must only be one header
                    serialized.HeaderCount != 1 ||
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

                    using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                    {
                        if (serialized.HttpStatusCode != response.HttpStatusCode ||
                        serialized.Version != response.Version ||
                        string.Compare(serialized.Body, response.Body) != 0 ||
                            //Both must be complete
                        false == serialized.IsComplete || false == response.IsComplete)
                        {
                            throw new Exception("Response Serialization Testing Failed With Body!");
                        }
                    }
                }

                bytes = response.ToBytes();

                using (Media.Http.HttpMessage serialized = new Media.Http.HttpMessage(bytes))
                {
                    if (serialized.HttpStatusCode != response.HttpStatusCode ||
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

        public void TestMessageSerializationAndDeserializationFromString()
        {
            string TestMessage = @"GET /index.html HTTP/1.1\r\n";

            using (Media.Http.HttpMessage message = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.MessageType != Media.Http.HttpMessageType.Request ||
                               message.HttpMethod != Media.Http.HttpMethod.GET ||
                               message.Version != 1.1 ||
                               message.Location.OriginalString != "/index.html") throw new Exception("Did not output expected result for request");
            }

            //Change the message, Include a single header with a value
            TestMessage = "GET /index.html HTTP/1.1\r\nHost: www.example.com\r\n\r\n";

            using (Media.Http.HttpMessage message = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.MessageType != Media.Http.HttpMessageType.Request ||
                    message.HttpMethod != Media.Http.HttpMethod.GET ||
                    message.Version != 1.1 ||
                    message.HeaderCount != 1 ||
                    message.GetHeader("Host") != "www.example.com" ||
                    message.Location.OriginalString != "/index.html") throw new Exception("Did not output expected result for request");
            }

            //Change the message, don't specify a location
            TestMessage = "GET / HTTP/1.1\r\nHost: www.example.com\r\n\r\n";

            using (Media.Http.HttpMessage message = Media.Http.HttpMessage.FromString(TestMessage))
            {
                if (message.MessageType != Media.Http.HttpMessageType.Request ||
                    message.HttpMethod != Media.Http.HttpMethod.GET ||
                    message.Version != 1.1 ||
                    message.HeaderCount != 1 ||
                    message.GetHeader("Host") != "www.example.com" ||
                    message.Location.OriginalString != "/") throw new Exception("Did not output expected result for request");
            }

            //Change the message, Introduce leading garbadge, Testing only single character's to end the lines of the headers
            TestMessage = "!@#$%^&*()_+-=\rHTTP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: funky-feature\n";

            using (Media.Http.HttpMessage message = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                //After parsing a message with only \n or \r as end lines the resulting output will be longer because it will now have \r\n (unless modified)
                //It must never be less but it can be equal to.

                if (message.MessageType != Media.Http.HttpMessageType.Response ||
                    message.Version != 1.0 ||
                    message.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    message.HeaderCount != 2 ||
                    output.Length <= message.Length) throw new Exception("Invalid response output length");

            }

            //Change the message, use white space and a combination of \r, \n and both to end the headers
            TestMessage = "HTTP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            //The body portion of further test message's
            string TestBody = "Body Data ! 1234567890-A";

            using (Media.Http.HttpMessage response = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.MessageType != Media.Http.HttpMessageType.Response ||
                    response.Version != 1.0 ||
                    response.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }


            //Change the message, don't white space but do use a combination of \r, \n and both to end the headers
            TestMessage = "HTTP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: funky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Http.HttpMessage response = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.MessageType != Media.Http.HttpMessageType.Response ||
                    response.Version != 1.0 ||
                    response.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                    string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check soon to be depreceated leading white space support in the headers..
            TestMessage = "HTTP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Http.HttpMessage response = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.MessageType != Media.Http.HttpMessageType.Response ||
                    response.Version != 1.0 ||
                    response.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check corner case of leading white space
            TestMessage = "HTTP/1.0 551 Option not supported\nCSeq: 302\nUnsupported: \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Http.HttpMessage response = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.MessageType != Media.Http.HttpMessageType.Response ||
                    response.Version != 1.0 ||
                    response.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    response.HeaderCount != 3 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

            //Check corner case of leading white space
            TestMessage = "HTTP/1.0 551 Option not supported\nWord-Of-The-Day: The Fox Jumps Over\r\tthe brown dog.\rCSeq:\r302\nUnsupported:\r \r\n \r \n \r\nfunky-feature\nContent-Length:24\r\n\rBody Data ! 1234567890-ABCDEF\r\n";

            using (Media.Http.HttpMessage response = Media.Http.HttpMessage.FromString(TestMessage))
            {
                string output = response.ToString();

                if (response.MessageType != Media.Http.HttpMessageType.Response ||
                    response.Version != 1.0 ||
                    response.HttpStatusCode != Media.Http.HttpStatusCode.OptionNotSupported ||
                    response.HeaderCount != 4 ||
                    output.Length <= response.Length ||
                     string.Compare(response.Body, TestBody) != 0) throw new Exception("Invalid response output length");
            }

        }

        //Todo, must test in TE chunks, etc as per http spec, not rtsp

        //public void TestCompleteFrom()
        //{
        //    using (Media.Http.HttpMessage message = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Response))
        //    {
        //        message.HttpStatusCode = Media.Http.HttpStatusCode.OK;

        //        //Include the session header
        //        message.SetHeader(Media.Http.HttpHeaders.Session, "A9B8C7D6");

        //        //This header should be included (it contains an invalid header directly after the end line data)
        //        message.SetHeader(Media.Http.HttpHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");

        //        //This header should be included
        //        message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aHTTP/1.0");

        //        //This header should be ignored
        //        message.SetHeader("$", string.Empty);

        //        //Set the date header
        //        message.SetHeader(Media.Http.HttpHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

        //        //Create a buffer from the message
        //        byte[] buffer = message.Prepare().ToArray();

        //        //Cache the size of the buffer and the offset in parsing it.
        //        int size = buffer.Length, offset;

        //        //Test for every possible offset in the message
        //        for (int i = 0; i < size; ++i)
        //        {
        //            //Reset the offset
        //            offset = 0;

        //            //Complete a message in chunks
        //            using (Media.Http.HttpMessage toComplete = new Http.HttpMessage(Media.Common.MemorySegment.EmptyBytes))
        //            {

        //                //Store the sizes encountered
        //                List<int> chunkSizes = new List<int>();

        //                int currentSize = size;

        //                //While data remains
        //                while (currentSize > 0)
        //                {
        //                    //Take a random sized chunk of at least 1 byte
        //                    int chunkSize = Utility.Random.Next(1, currentSize);

        //                    //Store the size of the chunk
        //                    chunkSizes.Add(chunkSize);

        //                    //Make a segment to that chunk
        //                    using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
        //                    {
        //                        //Keep track of how much data was just used to complete the message using that chunk
        //                        int justUsed = toComplete.CompleteFrom(null, chunkData);

        //                        //Ensure the chunk was totally consumed
        //                        if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

        //                        //Move the offset
        //                        offset += chunkSize;

        //                        //Decrese size
        //                        currentSize -= chunkSize;
        //                    }

        //                    //Do another iteration
        //                }

        //                //Verify the message
        //                if (toComplete.IsComplete != message.IsComplete ||
        //                    toComplete.HttpStatusCode != message.HttpStatusCode ||
        //                    toComplete.Version != message.Version ||
        //                    toComplete.HeaderCount != message.HeaderCount ||
        //                    toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0)) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

        //                //The header UserAgent should be different as it contains an invalid header in the message
        //                //Todo determine if this should be overlooked in Equals?
        //                //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
        //            }
        //        }
        //    }
        //}

        //public void TestCompleteFromWithBody()
        //{
        //    using (Media.Http.HttpMessage message = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Response, 1.0, Media.Http.HttpMessage.DefaultEncoding)
        //    {
        //        HttpStatusCode = Media.Http.HttpStatusCode.OK,
        //        UserAgent = "$UserAgent $007\r\n$\0\0\aHttp/1.0",
        //        Body = "$00Q\r\n$\0:\0"
        //    })
        //    {
        //        //Shoudn't matter
        //        message.HttpStatusCode = Media.Http.HttpStatusCode.OK;

        //        //Include the session header
        //        message.SetHeader(Media.Http.HttpHeaders.Session, "A9B8C7D6");

        //        //This header should be included (it contains an invalid header directly after the end line data)
        //        message.SetHeader(Media.Http.HttpHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");

        //        //This header should be included
        //        message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aHTTP/1.0");

        //        //This header should be ignored
        //        message.SetHeader("$", string.Empty);

        //        //Set the date header
        //        message.SetHeader(Media.Http.HttpHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

        //        //Create a buffer from the message
        //        byte[] buffer = message.Prepare().ToArray();

        //        //Cache the size of the buffer and the offset in parsing it.
        //        int size = buffer.Length, offset;

        //        //Test for every possible offset in the message
        //        for (int i = 0; i < size; ++i)
        //        {
        //            //Reset the offset
        //            offset = 0;

        //            //Complete a message in chunks
        //            using (Media.Http.HttpMessage toComplete = new Http.HttpMessage(Media.Common.MemorySegment.EmptyBytes))
        //            {

        //                //Store the sizes encountered
        //                List<int> chunkSizes = new List<int>();

        //                int currentSize = size;

        //                //While data remains
        //                while (currentSize > 0)
        //                {
        //                    //Take a random sized chunk of at least 1 byte
        //                    int chunkSize = Utility.Random.Next(1, currentSize);

        //                    //Store the size of the chunk
        //                    chunkSizes.Add(chunkSize);

        //                    //Make a segment to that chunk
        //                    using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
        //                    {
        //                        //Keep track of how much data was just used to complete the message using that chunk
        //                        int justUsed = toComplete.CompleteFrom(null, chunkData);

        //                        //Ensure the chunk was totally consumed
        //                        if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

        //                        //Move the offset
        //                        offset += chunkSize;

        //                        //Decrese size
        //                        currentSize -= chunkSize;
        //                    }

        //                    //Do another iteration
        //                }

        //                //Verify the message
        //                if (toComplete.IsComplete != message.IsComplete ||
        //                    toComplete.HttpStatusCode != message.HttpStatusCode ||
        //                    toComplete.Version != message.Version ||
        //                    toComplete.HeaderCount != message.HeaderCount ||
        //                    toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0) ||
        //                    string.Compare(toComplete.Body, message.Body, false) != 0) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

        //                //The header UserAgent should be different as it contains an invalid header in the message
        //                //Todo determine if this should be overlooked in Equals?
        //                //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
        //            }
        //        }
        //    }
        //}

        //public void TestCompleteFromWith0LengthBody()
        //{
        //    using (Media.Http.HttpMessage message = new Media.Http.HttpMessage(Media.Http.HttpMessageType.Response, 1.0, Media.Http.HttpMessage.DefaultEncoding)
        //    {
        //        HttpStatusCode = Media.Http.HttpStatusCode.OK,
        //        UserAgent = "$UserAgent $007\r\n$\0\0\aHttp/1.0",
        //        Body = string.Empty
        //    })
        //    {

        //        //Set the Content-Length
        //        message.SetHeader(Media.Http.HttpHeaders.ContentLength, (0).ToString());

        //        //Set the Content-Encoding
        //        message.SetHeader(Media.Http.HttpHeaders.ContentEncoding, message.ContentEncoding.WebName);

        //        //Shoudn't matter
        //        message.HttpStatusCode = Media.Http.HttpStatusCode.OK;

        //        //Include the session header
        //        message.SetHeader(Media.Http.HttpHeaders.Session, "A9B8C7D6");

        //        //This header should be included (it contains an invalid header directly after the end line data)
        //        message.SetHeader(Media.Http.HttpHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");

        //        //This header should be included
        //        message.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aHTTP/1.0");

        //        //This header should be ignored
        //        message.SetHeader("$", string.Empty);

        //        //This header should not be ignored, it's a multiline value
        //        message.SetHeader("Word of the day", "The quick brown fox \r\tJumps over the lazy dog.");

        //        //Set the date header
        //        message.SetHeader(Media.Http.HttpHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));

        //        //Create a buffer from the message
        //        byte[] buffer = message.Prepare().ToArray();

        //        //Cache the size of the buffer and the offset in parsing it.
        //        int size = buffer.Length, offset;

        //        //Test for every possible offset in the message
        //        for (int i = 0; i < size; ++i)
        //        {
        //            //Reset the offset
        //            offset = 0;

        //            //Complete a message in chunks
        //            using (Media.Http.HttpMessage toComplete = new Http.HttpMessage(Media.Common.MemorySegment.EmptyBytes))
        //            {

        //                //Store the sizes encountered
        //                List<int> chunkSizes = new List<int>();

        //                int currentSize = size;

        //                //While data remains
        //                while (currentSize > 0)
        //                {
        //                    //Take a random sized chunk of at least 1 byte
        //                    int chunkSize = Utility.Random.Next(1, currentSize);

        //                    //Store the size of the chunk
        //                    chunkSizes.Add(chunkSize);

        //                    //Make a segment to that chunk
        //                    using (Common.MemorySegment chunkData = new Common.MemorySegment(buffer, offset, chunkSize))
        //                    {
        //                        //Keep track of how much data was just used to complete the message using that chunk
        //                        int justUsed = toComplete.CompleteFrom(null, chunkData);

        //                        //Ensure the chunk was totally consumed
        //                        if (justUsed != chunkSize) throw new Exception("TestCompleteFrom Failed! Did not consume all chunkData.");

        //                        //Move the offset
        //                        offset += chunkSize;

        //                        //Decrese size
        //                        currentSize -= chunkSize;
        //                    }

        //                    //Do another iteration
        //                }


        //                //Verify the message
        //                if (toComplete.IsComplete != message.IsComplete ||
        //                    toComplete.HttpStatusCode != message.HttpStatusCode ||
        //                    toComplete.Version != message.Version ||
        //                    toComplete.HeaderCount != message.HeaderCount ||
        //                    toComplete.GetHeaders().Where(h => message.ContainsHeader(h)).Any(h => string.Compare(toComplete[h], message[h]) > 0) ||
        //                    string.Compare(toComplete.Body, message.Body) != 0) throw new Exception("TestCompleteFrom Failed! ChunkSizes =>" + string.Join(",", chunkSizes));

        //                //The header UserAgent should be different as it contains an invalid header in the message
        //                //Todo determine if this should be overlooked in Equals?
        //                //if (toComplete == message) throw new Exception("TestCompleteFrom Failed! Found equal message");
        //            }
        //        }
        //    }
        //}
    }
}