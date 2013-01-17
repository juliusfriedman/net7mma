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
        PreconditionFailed = 412,
        SessionNotFound = 454,
        UnsupportedTransport = 461,

        // 5xx Server Error.
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeOut = 504,
        VersionNotSupported = 505,
        OptionNotSupported = 551,
    }

    /// <summary>
    /// RFC2326 7.1:
    /// </summary>
    public class RtspResponse : RtspMessage
    {
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
            if (m_FirstLine == null || MessageType == RtspMessageType.Invalid) StatusCode = RtspStatusCode.Unknown;
            else
            {
                try
                {
                    StatusCode = (RtspStatusCode)int.Parse(m_FirstLine.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
                    //parts[2] is the Textual Convention for the Status Code
                }
                catch
                {
                    StatusCode = RtspStatusCode.Unknown;
                }
            }
        }

        public override byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add the Method and the Uri
            result.AddRange(Encoding.GetBytes(MessageIdentifier + '/' + Version.ToString("0.0") + " " + ((int)StatusCode).ToString() + " " + StatusCode.ToString() + CRLF));

            //Get the base bytes
            result.AddRange(base.ToBytes());

            return result.ToArray();
        }

        #endregion
    }
}
