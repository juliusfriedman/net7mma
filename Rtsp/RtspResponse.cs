using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp
{
    /// <summary>
    /// RFC2326 7.1:
    /// Status-Line =   RTSP-Version SP Status-Code SP Reason-Phrase CRLF
    /// </summary>
    public class RtspResponse : RtspMessage
    {
        #region Static

        static string SeperateCapitols(string text, char with = ' ')
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != with)
                {
                    newText.Append(with);
                }
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        #endregion

        #region Nested Types

        public enum ResponseStatusCode
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
            SessionNotFound = 461,

            // 5xx Server Error.
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeOut = 504,
            RTSPVersionNotSupported = 505,
            OptionNotSupported = 551,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates the StatusCode of the RtspResponse
        /// </summary>
        public ResponseStatusCode StatusCode { get; set; }

        #endregion

        #region Constructor

        public RtspResponse() : base(RtspMessageType.Response) { }

        public RtspResponse(byte[] packet) : base(packet)
        {
            if (m_FirstLine == null) StatusCode = ResponseStatusCode.Unknown;
            else
            {
                string[] parts = m_FirstLine.Split(' ');

                StatusCode = (ResponseStatusCode)Convert.ToInt32(parts[1]);
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
