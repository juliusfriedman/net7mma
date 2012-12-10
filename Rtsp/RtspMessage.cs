using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace Media.Rtsp
{
    public class RtspMessage
    {

        #region Nested Types

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
            public const string RtpInfo = "RTP-Info";
            public const string UserAgent = "User-Agent";
            public const string Vary = "Vary";
            public const string WWWAuthenticate = "WWW-Authenticate";

            private RtspHeaders() { }

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
        /// Enumeration to describe the available Rtsp Methods
        /// </summary>
        public enum RtspMethod
        {
            UNKNOWN = 0,
            ANNOUNCE = 1,
            DESCRIBE = 2,
            GET_PARAMETER = 3,
            OPTIONS = 4,
            PAUSE = 5,
            PLAY = 6,
            RECORD = 7,
            REDIRECT = 8,
            SETUP = 9,
            SET_PARAMETER = 10,
            TEARDOWN = 11,
        }

        /// <summary>
        /// Thrown when parsing a RtpMessage fails
        /// </summary>
        public class RtspMessageException
            : Exception
        {
            public RtspMessageException(string message)
                : base(message) { }

            public RtspMessageException(string message, Exception innerException)
                : base(message, innerException) { }
        }
        

        #endregion

        #region Statics

        public const string VersionString = "RTSP/1.0";
        public const string CRLF = "\r\n";

        // RFC2326 9.2, default port for both TCP and UDP.
        public const int DefaultPort = 554;
        public const string ReliableTransport = "rtsp";
        public const string UnreliableTransport = "rtspu";
        // RFC2326 9.2, initial round trip time used for retransmits on unreliable transports.
        public const int MaximumLength = 4096;

        private const string MessageIdentifier = "RTSP";	// String that must be in a message buffer to be recognised as an RTSP message and processed.
        private static string[] HeaderSplit = new string[] { CRLF };

        #endregion

        #region Fields

        /// <summary>
        /// The firstline of the RtspMessage
        /// </summary>
        internal string m_FirstLine;

        /// <summary>
        /// Dictionary containing the headers of the RtspMessage
        /// </summary>
        Dictionary<string, string> m_Headers = new Dictionary<string, string>();

        #endregion

        #region Properties        

        /// <summary>
        /// The body of the RtspMessage
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Indicates if this RtspMessage is a request or a response
        /// </summary>
        public RtspMessageType MessageType { get; internal set; }

        /// <summary>
        /// Indicates the CSeq of this RtspMessage
        /// </summary>
        public int CSeq { get { return Convert.ToInt32(GetHeader(RtspHeaders.CSeq)); } set { SetHeader(RtspHeaders.CSeq, value.ToString()); } }

        //Add support for parsing out of the message like cseq if present??
        //public NetworkCredential Credential;

        /// <summary>
        /// Accesses the header value 
        /// </summary>
        /// <param name="header">The header name</param>
        /// <returns>The header value</returns>
        public string this[string header]
        {
            get
            {

                if (m_Headers.ContainsKey(header))
                    return m_Headers[header];
                return null;
            }
            set { m_Headers[header] = value; }
        }

        /// <summary>
        /// The encoding of this RtspMessage. (Defaults to UTF-8)
        /// </summary>
        public Encoding Encoding { get; set; }

        #endregion

        #region Constructor        

        /// <summary>
        /// Constructs a RtspMessage
        /// </summary>
        /// <param name="messageType"></param>
        public RtspMessage(RtspMessageType messageType) { MessageType = messageType; Encoding = Encoding.UTF8; }

        public RtspMessage(byte[] packet)
        {
            Encoding = Encoding.UTF8;

            //Encoding should probalby be a property
            string message = Encoding.GetString(packet);

            int endFistLinePosn = message.IndexOf(CRLF);

            if (endFistLinePosn == -1) throw new RtspMessageException("Could not find first line");

            //Store the first line for derived types since this is the only thing they need
            m_FirstLine = message.Substring(0, endFistLinePosn);

            //Get the message type
            MessageType = m_FirstLine.Substring(0, MessageIdentifier.Length) == MessageIdentifier ? RtspMessage.RtspMessageType.Response : RtspMessage.RtspMessageType.Request;

            //Could assign version

            //Determine if we should decode more
            if (packet.Length - endFistLinePosn > 4)
            {
                int endHeaderPosn = message.IndexOf(CRLF + CRLF);

                int len;

                //If there is no end of the header then
                //Assume flakey implementation if message does not contain the required CRLFCRLF sequence and treat the message as having no body.
                if (endHeaderPosn == -1) len = message.Length - endFistLinePosn - 2;
                else len = endHeaderPosn - endFistLinePosn - 2;

                // Get the headers 
                foreach (string raw in message.Substring(endFistLinePosn + 2, len).Split(HeaderSplit, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = raw.Split(':');
                    SetHeader(parts[0], parts[1]);
                }

                //Get the body
                if (endHeaderPosn != -1)
                {
                    Body = message.Substring(endHeaderPosn + 4);
                    //Should verify content - length header?
                }
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

        public void AppendHeader(string name, string value)
        {
            if (!m_Headers.ContainsKey(name)) SetHeader(name, value);
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

            //Write CSeq
            result.AddRange(Encoding.GetBytes(RtspHeaders.CSeq + ": " + CSeq + CRLF));

            //Determine if a body is present
            bool hasBody = !string.IsNullOrWhiteSpace(Body);

            //Write headers
            foreach (KeyValuePair<string, string> header in m_Headers.Where(kvp => kvp.Key != RtspHeaders.CSeq))
            {
                result.AddRange(Encoding.GetBytes(header.Key + ": " + header.Value + CRLF));
            }

            //Add the contentLength header
            if (hasBody)
            {
                result.AddRange(Encoding.GetBytes(RtspHeaders.ContentLength + ": " + Encoding.GetByteCount(Body) + CRLF));
            }
            
            //End Header
            result.AddRange(Encoding.GetBytes(CRLF));

            //Write body if required
            if (hasBody)
            {
                result.AddRange(Encoding.GetBytes(Body));
            }

            return result.ToArray();
        }

        #endregion
    }
}
