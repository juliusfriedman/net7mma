using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp
{

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
        MovedTemporarily = 302,
        SeeOther = 303,
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
        SessionNotFound = 454,
        UnsupportedTransport = 461,

        // 5xx Server Error.
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeOut = 504,
        RTSPVersionNotSupported = 505,
        OptionNotSupported = 551,
    }

    /// <summary>
    /// RFC2326 7.1:
    /// </summary>
    public class RtspResponse : RtspMessage
    {

        public static RtspResponse FromHttpBytes(byte[] message)
        {
            //Get as string
            string messageString = System.Text.Encoding.UTF8.GetString(message);
            //SHOULD READ CONTENT LENGTH
            //Find end of body
            int indexOfBody = messageString.IndexOf("\r\n\r\n");
            //Base64 Decode
            byte[] base64decoded = System.Convert.FromBase64String(System.Text.Encoding.UTF8.GetString(message, 0, message.Length - indexOfBody));
            return new RtspResponse(base64decoded);
        }

        #region Properties

        /// <summary>
        /// Indicates the StatusCode of the RtspResponse
        /// </summary>
        public RtspStatusCode StatusCode { get; set; }

        #endregion

        #region Constructor

        public RtspResponse() : base(RtspMessageType.Response) { }

        public RtspResponse(byte[] packet, int offset = 0) : base(packet, offset)
        {
            if (m_FirstLine == null) StatusCode = RtspStatusCode.Unknown;
            else
            {
                string[] parts = m_FirstLine.Split(' ');
                //Version = float.Parse(parts[0].Replace(MessageIdentifier + '/', string.Empty));
                StatusCode = (RtspStatusCode)Convert.ToInt32(parts[1]);
                //parts[1] is the Textual Convention for the Status Code
            }
        }

        public override byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add the Method and the Uri
            result.AddRange(Encoding.GetBytes(VersionString + " " + ((int)StatusCode).ToString() + " " + StatusCode.ToString() + CRLF));

            //Get the base bytes
            result.AddRange(base.ToBytes());

            return result.ToArray();
        }

        #endregion
    }
}
