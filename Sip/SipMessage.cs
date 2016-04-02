using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Sip
{
    public class SipMessage : Media.Http.HttpMessage
    {
        #region Constants and Statics

        //The scheme of Uri's of SipMessage's
        public new const string TransportScheme = "sip";

        public new const int TransportDefaultPort = 5060;

        public new const string SecureTransportScheme = "sips";

        public new const int SecureTransportDefaultPort = 5061;

        //String which identifies a Http Request or Response
        public new const string MessageIdentifier = "SIP";

        public static SipMessage FromString(string data, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new InvalidOperationException("data cannot be null or whitespace.");

            if (encoding == null) encoding = SipMessage.DefaultEncoding;

            return new SipMessage(encoding.GetBytes(data), 0, encoding);
        }

        #endregion

        #region NestedTypes

        public enum SipMessageType
        {
            Invalid = 0,
            Request = 1,
            Response = 2,
        }

        public enum SipMethod
        {
            UNKNOWN,
            OPTIONS,
            ACK,
            BYE,
            CANCEL,
            REGISTER,
            PRACK,
            SUBSCRIBE,
            NOTIFY,
            PUBLISH,
            INFO,
            REFER,
            MESSAGE,
            UPDATE
        }

        public enum SipStatusCode
        {

        }

        #endregion

        #region Constructor

        static SipMessage()
        {
            if (false == UriParser.IsKnownScheme(SipMessage.TransportScheme))
                UriParser.Register(new HttpStyleUriParser(), SipMessage.TransportScheme, SipMessage.TransportDefaultPort);

            if (false == UriParser.IsKnownScheme(SipMessage.SecureTransportScheme))
                UriParser.Register(new HttpStyleUriParser(), SipMessage.SecureTransportScheme, SipMessage.SecureTransportDefaultPort);
        }

        /// <summary>
        /// Reserved
        /// </summary>
        internal protected SipMessage() : base(SipMessage.MessageIdentifier) { }

        /// <summary>
        /// Constructs a SipMessage
        /// </summary>
        /// <param name="messageType">The type of message to construct</param>
        public SipMessage(SipMessageType messageType, double? version = 1.0, Encoding contentEncoding = null, bool shouldDispse = true)
            : base((Http.HttpMessageType)messageType, version, contentEncoding, shouldDispse, SipMessage.MessageIdentifier)
        {

        }

        /// <summary>
        /// Creates a SipMessage from the given bytes
        /// </summary>
        /// <param name="bytes">The byte array to create the RtspMessage from</param>
        /// <param name="offset">The offset within the bytes to start creating the message</param>
        public SipMessage(byte[] bytes, int offset = 0, Encoding encoding = null) : this(bytes, offset, bytes.Length - offset, encoding) { }

        public SipMessage(Common.MemorySegment data, Encoding encoding = null) : this(data.Array, data.Offset, data.Count, encoding) { }

        /// <summary>
        /// Creates a managed representation of an abstract SipMessage
        /// </summary>
        /// <param name="packet">The array segment which contains the packet in whole at the offset of the segment. The Count of the segment may not contain more bytes than a RFC2326 message may contain.</param>
        public SipMessage(byte[] data, int offset, int length, Encoding contentEncoding = null, bool shouldDispose = true)
            :base(data, offset, length, contentEncoding, shouldDispose, SipMessage.MessageIdentifier)
        {

            
        }

        /// <summary>
        /// Creates a SipMessage by copying the properties of another.
        /// </summary>
        /// <param name="other">The other RtspMessage</param>
        public SipMessage(SipMessage other) : base(other)
        {
            
        }

        #endregion

        #region CSeq

        int m_CSeq;

        internal protected virtual bool ParseSequenceNumber(bool force = false)
        {
            //If the message is disposed then no parsing can occur
            if (IsDisposed && false == IsPersistent) return false;

            if (false == force && m_CSeq >= 0) return false;

            //See if there is a Content-Length header
            string sequenceNumber = GetHeader(SipHeaders.CSeq);

            //If the value was null or empty then do nothing
            if (string.IsNullOrWhiteSpace(sequenceNumber)) return false;

            //If there is a header parse it's value.
            //Should use EncodingExtensions
            if (false == int.TryParse(Media.Common.ASCII.ExtractNumber(sequenceNumber), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out m_CSeq))
            {
                //There was not a content-length in the format '1234'

                //Determine if alternate format parsing is allowed...

                return false;
            }

            return true;
        }

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
                if (m_CSeq != value) SetHeader(SipHeaders.CSeq, ((uint)(m_CSeq = value)).ToString());
            }
        }

        /// <summary>
        /// Called when a header is removed
        /// </summary>
        /// <param name="headerName"></param>
        protected override void OnHeaderRemoved(string headerName, string headerValue)
        {
            if (IsDisposed && false == IsPersistent) return;

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

        protected override void OnHeaderAdded(string headerName, string headerValue)
        {
            if (string.Compare(headerName, Http.HttpHeaders.TransferEncoding, true) == 0) throw new InvalidOperationException("Protocol: " + Protocol + ", does not support TrasferEncoding.");

            base.OnHeaderAdded(headerName, headerValue);
        }
    }
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the SipMessage class is correct
    /// </summary>
    internal class SipMessgeUnitTests
    {

        public void TestRequestsSerializationAndDeserialization()
        {

        }

        public void TestMessageSerializationAndDeserializationFromString()
        {
            string TestMessage = @"REGISTER / SIP/2.0\n\n";

            using (Media.Sip.SipMessage message = Media.Sip.SipMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.MessageType != Http.HttpMessageType.Request ||
                               message.MethodString != Media.Sip.SipMessage.SipMethod.REGISTER.ToString() ||
                               message.Version != 2.0) throw new Exception("Did not output expected result for invalid message");
            }

            TestMessage = @"ACK test:test@test.test.com:24343 SIP/2.0\n\n";

            using (Media.Sip.SipMessage message = Media.Sip.SipMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.MessageType != Http.HttpMessageType.Request ||
                               message.MethodString != Media.Sip.SipMessage.SipMethod.ACK.ToString() ||
                               message.Version != 2.0) throw new Exception("Did not output expected result for invalid message");
            }

            TestMessage = @"SIP/2.3 200 OKay";

            using (Media.Sip.SipMessage message = Media.Sip.SipMessage.FromString(TestMessage))
            {
                string output = message.ToString();

                if (message.MessageType != Http.HttpMessageType.Response ||
                               message.StatusCode != 200 ||
                               message.Version != 2.3 ||
                               message.ReasonPhrase != "OKay") throw new Exception("Did not output expected result for invalid message");
            }
        }

    }
}