﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp
{
    /// <summary>
    /// Header Definitions from RFC2326
    /// http://www.ietf.org/rfc/rfc2326.txt
    /// </summary>
    public sealed class RtspHeaders
    {
        public const string Allow = "Allow";
        public const string Accept = "Accept";
        public const string AcceptEncoding = "Accept-Encoding";
        public const string AcceptLanguage = "Accept-Language";
        public const string Authroization = "Authorization";
        public const string Connection = "Connection";
        public const string ContentBase = "Content-Base";
        public const string ContentEncoding = "Content-Encoding";
        public const string ContentLanguage = "Content-Language";
        public const string ContentLength = "Content-Length";
        public const string ContentLocation = "Content-Location";
        public const string ContentType = "Content-Type";
        public const string CSeq = "CSeq";
        public const string From = "From";
        public const string Expires = "Expires";
        public const string LastModified = "Last-Modified";
        public const string IfModifiedSince = "If-Modified-Since";
        public const string Location = "Location";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string Public = "Public";
        public const string Range = "Range";
        public const string Referer = "Referer";
        public const string RetryAfter = "Retry-After";
        public const string Server = "Server";
        public const string Session = "Session";
        public const string Transport = "Transport";
        public const string Required = "Required";
        public const string RtpInfo = "RTP-Info";
        public const string UserAgent = "User-Agent";
        public const string Vary = "Vary";
        public const string WWWAuthenticate = "WWW-Authenticate";
        

        private RtspHeaders() { }

        //Ensure this format is correct
        internal const string NtpFormat = "h'.'fff";

        public static string RangeHeader(TimeSpan start, TimeSpan end)
        {
            return "npt=" + start.ToString(NtpFormat) + '-' + end.ToString(NtpFormat);
        }
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
    public abstract class RtspMessage
    {
        #region Nested Types

        /// <summary>
        /// Thrown when parsing a RtpMessage fails
        /// </summary>
        public class RtspMessageException
            : Exception
        {
            public RtspMessage RtspMessage { get; protected set; }

            public RtspMessageException(string message)
                : base(message) { }

            public RtspMessageException(string message, Exception innerException)
                : base(message, innerException) { }

            public RtspMessageException(string message, Exception innerException, RtspMessage rtspMessage)
                : base(message, innerException) { RtspMessage = rtspMessage; }
        }

        #endregion

        #region Statics

        //New Line
        public const string CRLF = "\r\n";

        public const string ReliableTransport = "rtsp";
        public const string UnreliableTransport = "rtspu";
        public const int MaximumLength = 4096;

        //String which identifies a Rtsp Request or Response
        internal const string MessageIdentifier = "RTSP";
        private static string[] HeaderSplit = new string[] { CRLF };
        private static char[] HeaderValueSplit = new char[] { ':' };

        public static byte[] ToHttpBytes(RtspMessage message, int minorVersion = 0, string sessionCookie = null, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.Unused)
        {

            if (message.MessageType == RtspMessageType.Invalid) return null;

            //Our result in a List
            List<byte> result = new List<byte>();

            //Our RtspMessage base64 encoded
            byte[] messageBytes;

            //Either a RtspRequest or a RtspResponse
            if (message is RtspRequest)
            {
                RtspRequest request = message as RtspRequest;

                //Get the body of the HttpRequest
                messageBytes = request.Encoding.GetBytes(System.Convert.ToBase64String(request.ToBytes()));
                
                //Form the HttpRequest
                result.AddRange(request.Encoding.GetBytes("GET " + request.Location + " HTTP 1." + minorVersion.ToString() + CRLF));
                result.AddRange(request.Encoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(request.Encoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(request.Encoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(request.Encoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));
                
                if (!string.IsNullOrWhiteSpace(sessionCookie))
                {
                    result.AddRange(request.Encoding.GetBytes("x-sessioncookie: " + System.Convert.ToBase64String(request.Encoding.GetBytes(sessionCookie)) + CRLF));
                }

                result.AddRange(request.Encoding.GetBytes(CRLF + CRLF));

                result.AddRange(messageBytes);
            }
            else
            {
                RtspResponse response = message as RtspResponse;

                //Get the body of the HttpResponse
                messageBytes = response.Encoding.GetBytes(System.Convert.ToBase64String(response.ToBytes()));

                //Form the HttpResponse
                result.AddRange(response.Encoding.GetBytes("HTTP/1." + minorVersion.ToString() + " " + (int)statusCode + " " + statusCode + CRLF));
                result.AddRange(response.Encoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(response.Encoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(response.Encoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(response.Encoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));
                result.AddRange(response.Encoding.GetBytes("Expires:Sun, 9 Jan 1972 00:00:00 GMT" + CRLF));
                result.AddRange(response.Encoding.GetBytes(CRLF + CRLF));

                result.AddRange(messageBytes);
            }

            return result.ToArray();
        }

        public static RtspMessage FromHttpBytes(byte[] message, int offset, Encoding encoding = null)
        {
            //Sanity
            if (message == null) return null;
            if (offset > message.Length) throw new ArgumentOutOfRangeException("offset");

            //Use a default encoding if none was given
            if (encoding == null) encoding = Encoding.UTF8;

            //The message we will attempt to return
            RtspMessage result = null;            

            //Parse the HTTP 
            string Message = encoding.GetString(message, offset, message.Length - offset);

            //Find the end of all the headers
            int headerEnd = Message.IndexOf(CRLF + CRLF);

            //Get the Http Body, It occurs after all the headers which ends with \r\n\r\n and is Base64 Encoded.
            string Body = Message.Substring(headerEnd);

            //Might want to provide the headers as an out param

            //Get the bytes of the underlying RtspMessage by decoding the Http Body which was encoded in base64
            byte[] rtspMessage = System.Convert.FromBase64String(Body);

            //Resposne
            if (Message.StartsWith("HTTP"))
            {
                result = new RtspResponse(rtspMessage);                
            }
            else if (Message.StartsWith("GET"))//Request
            {
                result = new RtspRequest(rtspMessage);
            }

            //Done
            return result;
        }

        #endregion

        #region Fields

        double m_Version;

        /// <summary>
        /// The firstline of the RtspMessage and the Body
        /// </summary>
        internal string m_FirstLine, m_Body;

        /// <summary>
        /// Dictionary containing the headers of the RtspMessage
        /// </summary>
        Dictionary<string, string> m_Headers = new Dictionary<string, string>();
       

        #endregion

        #region Properties        

        public double Version { get { return m_Version; } set { m_Version = value; } }

        /// <summary>
        /// The body of the RtspMessage
        /// </summary>
        public string Body
        {
            get { return m_Body; }
            set
            {
                m_Body = value;
                if (string.IsNullOrWhiteSpace(m_Body))
                {
                    RemoveHeader(RtspHeaders.ContentLength);
                }
                else
                {
                    SetHeader(RtspHeaders.ContentLength, this.Encoding.GetByteCount(m_Body).ToString());
                }
            }
        }

        /// <summary>
        /// Indicates if this RtspMessage is a request or a response
        /// </summary>
        public RtspMessageType MessageType { get; internal set; }

        /// <summary>
        /// Indicates the CSeq of this RtspMessage
        /// </summary>
        public int CSeq { get { return Convert.ToInt32(GetHeader(RtspHeaders.CSeq)); } set { SetHeader(RtspHeaders.CSeq, value.ToString()); } }

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
        /// The encoding of this RtspMessage. (Defaults to UTF-8)
        /// </summary>
        public Encoding Encoding { get; set; }

        //public float Version { get; set; }

        #endregion

        #region Constructor        

        /// <summary>
        /// Constructs a RtspMessage
        /// </summary>
        /// <param name="messageType"></param>
        public RtspMessage(RtspMessageType messageType) { Version = 1.0;  MessageType = messageType; Encoding = Encoding.UTF8; }

        public RtspMessage(byte[] packet, int offset = 0)
        {
            try
            {
                //Should determine encoding...
                Encoding = Encoding.UTF8;

                //Should check the first few bytes before doing this?

                string Message = Encoding.GetString(packet, offset, offset > 0 ? packet.Length - offset : packet.Length);

                int endFistLinePosn = Message.IndexOf(CRLF, offset);

                if (endFistLinePosn == -1)
                {
                    MessageType = RtspMessageType.Invalid;
                    return;
                }

                //Store the first line for derived types since this is the only thing they need
                m_FirstLine = Message.Substring(offset, endFistLinePosn);

                int miLen = MessageIdentifier.Length;

                if (Message.Contains(MessageIdentifier))
                {
                    //Get the message type
                    MessageType = m_FirstLine.Substring(offset, miLen) == MessageIdentifier ? RtspMessageType.Response : RtspMessageType.Request;
                }
                else
                {
                    MessageType = RtspMessageType.Invalid;
                    return;
                }

                #region FirstLine Version, (Method / Location or StatusCode)

                //Could assign version, then assign Method and Location
                if (MessageType == RtspMessageType.Request)
                {
                    //C->S[0]SETUP[1]rtsp://example.com/media.mp4/streamid=0[2]RTSP/1.0
                    Version = double.Parse(m_FirstLine.Split(' ')[2].Replace(MessageIdentifier + '/', string.Empty));
                }
                else
                {
                    //S->C[0]RTSP/1.0[1]200[2]OK
                    Version = double.Parse(m_FirstLine.Split(' ')[0].Replace(MessageIdentifier + '/', string.Empty));
                }

                #endregion

                //Determine if we should decode more
                if (packet.Length - endFistLinePosn > miLen)
                {
                    int endHeaderPosn = Message.IndexOf(CRLF + CRLF);

                    int headerLength = 0, crlfLen = CRLF.Length;

                    //If there is no end of the header then
                    //Assume flakey implementation if message does not contain the required CRLFCRLF sequence and treat the message as having no body.
                    if (endHeaderPosn == -1) headerLength = Message.Length - endFistLinePosn - crlfLen;
                    else headerLength = endHeaderPosn - endFistLinePosn - crlfLen;

                    // Get the headers 
                    foreach (string raw in Message.Substring(endFistLinePosn + crlfLen, headerLength).Split(HeaderSplit, StringSplitOptions.RemoveEmptyEntries))
                    {
                        //We only want the first 2 sub strings to allow for headers which have a ':' in the data
                        //E.g. Rtp-Info: rtsp://....
                        string[] parts = raw.Split(HeaderValueSplit, 2);
                        if (parts.Length > 1) SetHeader(parts[0].Trim(), parts[1].Trim());
                    }

                    //Get the body
                    if (endHeaderPosn != -1)
                    {
                        Body = Message.Substring(endHeaderPosn + 4); //crlfLen * 2
                        //Should verify content - length header?
                        //if(!ContainsHeader(ContentLength) && Body.Length == int.Parse(GetHeader(ContentLength))){
                        // throw new RtspMessageException("Invalid Content-Length Header");
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RtspMessageException(ex.Message, ex, this);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an array of all headers present in the RtspMessage
        /// </summary>
        /// <returns>The array containing all present headers</returns>
        public string[] GetHeaders() { return m_Headers.Keys.ToArray(); }

        /// <summary>
        /// Gets a header value
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <returns>The header value if found, otherwise null.</returns>
        public string GetHeader(string name)
        {
            string header = null;
            m_Headers.TryGetValue(name, out header);
            return header;
        }

        /// <summary>
        /// Sets or adds a header value
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <param name="value">The value of the header</param>
        public void SetHeader(string name, string value)
        {
            if (m_Headers.ContainsKey(name)) m_Headers[name] = value;
            else m_Headers.Add(name, value);
        }

        public void AppendOrSetHeader(string name, string value)
        {
            if (!ContainsHeader(name)) SetHeader(name, value);
            else m_Headers[name] += ';' + value;
        }

        /// <summary>
        /// Indicates if the RtspMessage contains a named header
        /// </summary>
        /// <param name="name">The name of the header to check for</param>
        /// <returns>True if contained, otherwise false</returns>
        public bool ContainsHeader(string name)
        {
            return m_Headers.ContainsKey(name);
        }

        /// <summary>
        /// Removes a header from the RtspMessage
        /// </summary>
        /// <param name="name">The name of the header to remove</param>
        /// <returns>True if removed, false otherwise</returns>
        public bool RemoveHeader(string name)
        {
            return m_Headers.Remove(name);
        }

        /// <summary>
        /// Creates a Packet from the RtspMessage which can be sent on the network
        /// </summary>
        /// <returns>The packet which represents this RtspMessage</returns>
        public virtual byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            byte[] encodedCRLF = Encoding.GetBytes(CRLF);

            //Write headers
            foreach (KeyValuePair<string, string> header in m_Headers/*.OrderBy((key) => key.Key).Reverse()*/)
            {
                result.AddRange(Encoding.GetBytes(header.Key + ": " + header.Value));
                result.AddRange(encodedCRLF);
            }

            //End Header
            result.AddRange(encodedCRLF);

            //Write body if required
            if (ContainsHeader(RtspHeaders.ContentLength))
            {
                result.AddRange(Encoding.GetBytes(Body));
            }

            return result.ToArray();
        }

        #endregion
    }
}
