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

        //MinimumStatusLineSize = 8...

        //Override ParseStatusLine or have instance variable.

    }
}
