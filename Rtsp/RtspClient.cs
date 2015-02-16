#region Copyright
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
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Media.Rtcp;
using Media.Rtp;
using Media.Sdp;
using System.Threading;

namespace Media.Rtsp
{
    /// <summary>
    /// Implements RFC 2326
    /// http://www.ietf.org/rfc/rfc2326.txt
    /// Provides facilities for communication with an RtspServer to establish one or more Rtp Transport Channels.
    /// </summary>
    public class RtspClient : Common.BaseDisposable, Media.Common.ISocketReference
    {

        public const int DefaultBufferSize = RtspMessage.MaximumLength * 2;

        public const double DefaultProtocolVersion = 1.0;

        public static readonly TimeSpan DefaultConnectionTime = TimeSpan.FromMilliseconds(500);

        #region Nested Types

        public enum ClientProtocolType
        {
            Tcp = ProtocolType.Tcp,
            Reliable = Tcp,
            Udp = ProtocolType.Udp,
            Unreliable = Udp,
            Http = 2,
            Secure = 4
        }

        #endregion

        #region Fields

        internal readonly Guid InternalId = Guid.NewGuid();

        ClientProtocolType m_RtspProtocol;

        ManualResetEventSlim m_InterleaveEvent = new ManualResetEventSlim(false);

        RtspMessage m_LastTransmitted;

        AuthenticationSchemes m_AuthenticationScheme;

        /// <summary>
        /// The current location the media
        /// </summary>
        Uri m_InitialLocation, m_CurrentLocation;

        /// <summary>
        /// The buffer this client uses for all requests 4MB * 2
        /// </summary>
        Common.MemorySegment m_Buffer;

        /// <summary>
        /// The remote IPAddress to which the Location resolves via Dns
        /// </summary>
        IPAddress m_RemoteIP;

        /// <summary>
        /// The remote RtspEndPoint
        /// </summary>
        EndPoint m_RemoteRtsp;

        /// <summary>
        /// The socket used for Rtsp Communication
        /// </summary>
        Socket m_RtspSocket;

        /// <summary>
        /// The protcol in which Rtsp data will be transpored from the server
        /// </summary>
        ProtocolType m_RtpProtocol;

        /// <summary>
        /// The session description associated with the media at Location
        /// </summary>
        SessionDescription m_SessionDescription;

        /// <summary>
        /// Keep track of timed values.
        /// </summary>
        TimeSpan m_RtspSessionTimeout = TimeSpan.FromSeconds(60),
            m_ConnectionTime = Utility.InfiniteTimeSpan,
            m_LastServerDelay = Utility.InfiniteTimeSpan,
            //Appendix G.  Requirements for Unreliable Transport of RTSP
            m_LastMessageRoundTripTime = DefaultConnectionTime;

        /// <summary>
        /// Keep track of certain values.
        /// </summary>
        int m_SentBytes, m_ReceivedBytes,
             m_RtspPort, 
             m_CSeq, m_RCSeq,
             m_SentMessages, m_ReTransmits,
             m_ReceivedMessages,
             m_PushedMessages,
             m_ResponseTimeoutInterval = (int)Utility.MicrosecondsPerMillisecond;

        HashSet<string> m_SupportedMethods = new HashSet<string>();

        internal string m_UserAgent = "ASTI RTP Client", m_SessionId = string.Empty;//, m_TransportMode;

        internal RtpClient m_RtpClient = new RtpClient();

        Timer m_KeepAliveTimer, m_ProtocolSwitchTimer;

        DateTime? m_BeginConnect, m_EndConnect, m_StartedPlaying;

        //List<Sdp.MediaDescription> For playing and paused. Could use a List<Tuple<TimeSpan, MediaDescription>>> to allow the timeline when pausing etc..

        List<MediaDescription> m_Playing = new List<MediaDescription>();

        bool m_TriedCredentials;

        NetworkCredential m_Credential;

        public Common.ILogging Logger;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or Sets the socket used for communication
        /// </summary>
        internal protected Socket RtspSocket
        {
            get { return m_RtspSocket; }
            set
            {
                m_RtspSocket = value;

                //Ensure not connected if the socket is removed
                if (m_RtspSocket == null)
                {
                    m_BeginConnect = m_EndConnect = null;

                    m_ConnectionTime = Utility.InfiniteTimeSpan;

                    return;
                }

                //If the socket is connected
                if (m_RtspSocket.Connected)
                {
                    //SO_CONNECT_TIME only exists on Windows...

                    //Set default values to indicate connected

                    m_BeginConnect = m_EndConnect = DateTime.UtcNow;

                    m_ConnectionTime = TimeSpan.Zero;

                    //Use the remote information from the existing socket rather than the location.

                    m_RemoteRtsp = m_RtspSocket.RemoteEndPoint;

                    if (m_RemoteRtsp is IPEndPoint)
                    {
                        IPEndPoint remote = (IPEndPoint)m_RemoteRtsp;

                        m_RemoteIP = remote.Address;
                    }
                }
            }
        }

        internal protected Common.MemorySegment Buffer
        {
            get { return m_Buffer; }
            set { m_Buffer = value; }
        }

        public bool SharesSocket
        {
            get
            {
                //The socket is shared with the GC
                if (IsDisposed) return true;

                //If the session has any playing media
                if (m_Playing.Count > 0)
                {
                    // A null or disposed client or one which is no longer connected cannot share the socket
                    if (m_RtpClient == null || m_RtpClient.IsDisposed || false == m_RtpClient.IsConnected) return false;

                    //If the transport is not null and the handle is equal to the rtsp socket's handle
                    if (((Common.ISocketReference)m_RtpClient).GetReferencedSockets().Any(s => s.Handle == m_RtspSocket.Handle))
                    {
                        //Indicate the socket is shared
                        return true;
                    }
                }

                //The socket is not shared
                return false;
            }
        }

        //Maybe AllowHostChange
        //public bool IgnoreRedirectOrFound { get; set; }

        /// <summary>
        /// Indicates if the client will take any `X-` headers and use them in future requests.
        /// </summary>
        public bool EchoXHeaders { get; set; }

        /// <summary>
        /// Indicates if the client will process messages which are pushed during the session.
        /// </summary>
        public bool IgnoreServerSentMessages { get; set; }

        /// <summary>
        /// Indicates the amount of messages which were transmitted more then one time.
        /// </summary>
        public int RetransmittedMessages { get { return m_ReTransmits; } }

        /// <summary>
        /// Indicates if the client has tried to Authenticate using the current <see cref="Credential"/>'s
        /// </summary>
        public bool TriedCredentials { get { return m_TriedCredentials; } }

        /// <summary>
        /// Indicates if Keep Alive Requests will be sent
        /// </summary>
        public bool DisableKeepAliveRequest { get; set; }

        /// <summary>
        /// The amount of <see cref="RtspMessage"/>'s sent by this instance.
        /// </summary>
        public int MessagesSent { get { return m_SentMessages; } }

        /// <summary>
        /// The amount of <see cref="RtspMessage"/>'s receieved by this instance.
        /// </summary>
        public int MessagesReceived { get { return m_ReceivedMessages; } }

        /// <summary>
        /// The amount of messages pushed by the remote party
        /// </summary>
        public int MessagesPushed { get { return m_PushedMessages; } }

        /// <summary>
        /// The amount of time taken to connect to the remote party.
        /// </summary>
        public TimeSpan ConnectionTime { get { return m_ConnectionTime; } }

        /// <summary>
        /// The amount of time taken since the response was received to the last <see cref="RtspMessage"/> sent.
        /// </summary>
        public TimeSpan LastMessageRoundTripTime { get { return m_LastMessageRoundTripTime; } }

        /// <summary>
        /// If indicated by the remote party the value of the 'delay' header from the Timestamp header.
        /// </summary>
        public TimeSpan LastServerDelay { get { return m_LastServerDelay; } }

        /// <summary>
        /// Indicates if the client has been assigned a <see cref="SessionId"/>
        /// </summary>
        public bool HasSession { get { return !string.IsNullOrWhiteSpace(m_SessionId); } }

        /// <summary>
        /// Gets the value of the id in the Session header if it was seen in a response.
        /// </summary>
        public string SessionId { get { return m_SessionId; } }

        /// <summary>
        /// As given by the OPTIONS response or set otherwise.
        /// </summary>
        public readonly HashSet<string> SupportedFeatures = new HashSet<string>();

        /// <summary>
        /// Values which will be set in the Required tag.
        /// </summary>
        public readonly HashSet<string> RequiredFeatures = new HashSet<string>();

        /// <summary>
        /// Any additional headers which may be required by the RtspClient.
        /// </summary>
        public readonly Dictionary<string, string> AdditionalHeaders = new Dictionary<string, string>();

        //Determine if Start and EndTime are worth having?

        /// <summary>
        /// If playing, the TimeSpan which represents the time this media started playing from.
        /// </summary>
        public TimeSpan? StartTime { get { try { return Client != null ? (TimeSpan?)Client.TransportContexts.Max(tc => tc.MediaStartTime) : null; } catch { return null; } } }

        /// <summary>
        /// If playing, the TimeSpan which represents the time the media will end.
        /// </summary>
        public TimeSpan? EndTime { get { try { return Client != null ? (TimeSpan?)Client.TransportContexts.Max(tc => tc.MediaEndTime) : null; } catch { return null; } } }

        //Remaining?

        /// <summary>
        /// If playing, indicates if the RtspClient is playing from a live source which means there is no absolute start or end time and seeking may not be supported.
        /// </summary>
        public bool LivePlay { get { return EndTime < TimeSpan.Zero; } }

        /// <summary>
        /// True if the RtspClient has received the Playing event, False if the RtspClient has received the Stopping event or otherwise such as the media has finished playing.
        /// </summary>
        //Should take into account if Paused? or Pausing everything should set m_Playing to false...
        public bool IsPlaying { get { return m_StartedPlaying.HasValue && m_Playing.Count > 0; } } //Should probably not check count? streams may be ended

        /// <summary>
        /// The DateTime in which the client started playing if playing, otherwise null.
        /// </summary>
        public DateTime? StartedPlaying { get { return m_StartedPlaying; } }

        /// <summary>
        /// Gets or Sets a value which indicates if the client will attempt an alternate style of connection if one cannot be established successfully.
        /// Usually only useful under UDP when NAT prevents RTP packets from reaching a client, it will then attempt TCP or HTTP transport.
        /// </summary>
        public bool AllowAlternateTransport { get; set; }

        /// <summary>
        /// The amount of time in seconds the KeepAlive request will be sent to the server after connected.
        /// If a GET_PARAMETER request is not supports OPTIONS will be sent instead.
        /// </summary>
        public TimeSpan RtspSessionTimeout { get { return m_RtspSessionTimeout; }
            set
            {
                m_RtspSessionTimeout = value;
                
                if (m_RtspSessionTimeout <= TimeSpan.Zero)
                {
                    if (m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();
                    m_KeepAliveTimer = null;
                }

                //Update the timer period (taking into account the last time a request was sent) if there is a timer.
                if (m_KeepAliveTimer != null) m_KeepAliveTimer.Change(m_LastTransmitted != null && m_LastTransmitted.Transferred.HasValue ? (m_RtspSessionTimeout - (DateTime.UtcNow - m_LastTransmitted.Created)) : m_RtspSessionTimeout, Utility.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Gets or Sets amount the fraction of time the client will wait during a responses for a response without blocking.
        /// If less than or equal to 0 the value 1 will be used.
        /// </summary>
        public int ResponseTimeoutInterval { get { return m_ResponseTimeoutInterval; } set { m_ResponseTimeoutInterval = value; if (m_ResponseTimeoutInterval <= 0) m_ResponseTimeoutInterval = 1; } }

        //The last RtspMessage transmittted by the RtspClient (Sent or Received)
        public RtspMessage LastTransmitted { get { return m_LastTransmitted; } }

        /// <summary>
        /// The ClientProtocolType the RtspClient is using Reliable (Tcp), Unreliable(Udp) or Http(Tcp)
        /// </summary>
        public ClientProtocolType RtspProtocol { get { return m_RtspProtocol; } }

        /// <summary>
        /// The ProtocolType the RtspClient will setup for underlying RtpClient.
        /// </summary>
        public ProtocolType RtpProtocol { get { return m_RtpProtocol; } }

        /// <summary>
        /// Gets or sets the current location to the Media on the Rtsp Server and updates Remote information and ClientProtocol if required by the change.
        /// If the RtspClient was listening then it will be stopped and started again
        /// </summary>
        public Uri CurrentLocation
        {
            get { return m_CurrentLocation; }
            set
            {
                try
                {
                    //If Different
                    if (m_CurrentLocation != value)
                    {

                        m_InitialLocation = m_CurrentLocation;

                        bool wasPlaying = IsPlaying;

                        if (wasPlaying) StopPlaying();

                        m_CurrentLocation = value;


                        switch (m_CurrentLocation.HostNameType)
                        {
                            case UriHostNameType.IPv4:
                            case UriHostNameType.IPv6:

                                m_RemoteIP = IPAddress.Parse(m_CurrentLocation.DnsSafeHost);

                                break;
                            case UriHostNameType.Dns:

                                

                                if (m_RtspSocket != null)
                                {
                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault(a=> a.AddressFamily == m_RtspSocket.AddressFamily);

                                    if (m_RemoteIP == null) throw new NotSupportedException("The given Location uses a HostNameType which is not the same as the underlying socket's address family. " + m_CurrentLocation.HostNameType + ", " + m_RtspSocket.AddressFamily + " And as a result no remote IP could be obtained to complete the connection." );
                                }
                                else
                                {
                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault();
                                }

                                break;

                            default: throw new NotSupportedException("The given Location uses a HostNameType which is not supported. " + m_CurrentLocation.HostNameType);
                        }

                        m_RtspPort = m_CurrentLocation.Port;

                        //Validate ports, should throw?
                        if (m_RtspPort <= ushort.MinValue || m_RtspPort > ushort.MaxValue) m_RtspPort = 554;

                        //Determine protocol
                        if (m_CurrentLocation.Scheme == RtspMessage.ReliableTransport) m_RtspProtocol = ClientProtocolType.Tcp;
                        else if (m_CurrentLocation.Scheme == RtspMessage.UnreliableTransport) m_RtspProtocol = ClientProtocolType.Udp;
                        else m_RtspProtocol = ClientProtocolType.Http;

                        //Make a IPEndPoint 
                        m_RemoteRtsp = new IPEndPoint(m_RemoteIP, m_RtspPort);

                        //Should take into account current time with StartTime?
                        if (wasPlaying) StartPlaying();
                    }
                }
                catch (Exception ex)
                {
                    Common.ExceptionExtensions.RaiseTaggedException(this, "Could not resolve host from the given location. See InnerException.", ex);
                }
            }
        }
        
        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        /// <notes>May want to do a partial receive for 1 byte which would take longer but indicate if truly connected. Udp may not be Connected.</notes>
        public bool IsConnected { get { return m_ConnectionTime >= TimeSpan.Zero && m_RtspSocket != null; } }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential { get { return m_Credential; } set { m_Credential = value; m_TriedCredentials = false; } }

        /// <summary>
        /// The type of AuthenticationScheme to utilize in RtspRequests
        /// </summary>
        public AuthenticationSchemes AuthenticationScheme { get { return m_AuthenticationScheme; } set { if (value == m_AuthenticationScheme) return; if (value != AuthenticationSchemes.Basic && value != AuthenticationSchemes.Digest && value != AuthenticationSchemes.None) throw new System.InvalidOperationException("Only None, Basic and Digest are supported"); else m_AuthenticationScheme = value; } }

        /// <summary>
        /// The amount of bytes sent by the RtspClient
        /// </summary>
        public int BytesSent { get { return m_SentBytes; } }

        /// <summary>
        /// The amount of bytes recieved by the RtspClient
        /// </summary>
        public int BytesRecieved { get { return m_ReceivedBytes; } }

        /// <summary>
        /// The current SequenceNumber of the RtspClient
        /// </summary>
        public int ClientSequenceNumber { get { return m_CSeq; } }

        /// <summary>
        /// The current SequenceNumber of the remote RTSP party
        /// </summary>
        public int RemoteSequenceNumber { get { return m_RCSeq; } }

        /// <summary>
        /// Gets the <see cref="MediaDescription"/>'s which pertain to media which is currently playing.
        /// </summary>
        public IEnumerable<MediaDescription> PlayingMedia { get { return m_Playing.AsEnumerable(); } }

        /// <summary>
        /// Gets or Sets the <see cref="SessionDescription"/> describing the media at <see cref="CurrentLocation"/>.
        /// </summary>
        public SessionDescription SessionDescription
        {
            get { return m_SessionDescription; }
            set
            {
                if (value == null) throw new ArgumentNullException("The SessionDescription cannot be null.");
                m_SessionDescription = value;
            }
        }

        /// <summary>
        /// Gets the methods supported by the server recieved in the options request.
        /// </summary>
        public string[] SupportedMethods { get { return m_SupportedMethods.ToArray(); } }

        /// <summary>
        /// The RtpClient associated with this RtspClient
        /// </summary>
        public RtpClient Client { get { return m_RtpClient; } }

        /// <summary>
        /// Gets or Sets the ReadTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketReadTimeout
        {
            get { return IsDisposed || m_RtspSocket == null ? -1 : m_RtspSocket.ReceiveTimeout; }
            set { if (IsDisposed || m_RtspSocket == null) return; m_RtspSocket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets or Sets the WriteTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketWriteTimeout
        {
            get { return IsDisposed || m_RtspSocket == null ? -1 : m_RtspSocket.SendTimeout; }
            set { if (IsDisposed || m_RtspSocket == null) return; m_RtspSocket.SendTimeout = value; }
        }

        /// <summary>
        /// The UserAgent sent with every RtspRequest
        /// </summary>
        public string UserAgent
        {
            get { return m_UserAgent; }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("UserAgent cannot consist of only null or whitespace."); m_UserAgent = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating of the RtspSocket should be left open when Disposing.
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// The version of Rtsp the client will utilize in messages
        /// </summary>
        public double ProtocolVersion { get; set; }

        #endregion

        #region Constructor

       

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The absolute location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize and will not deviate from the protocol is no data is received, if null it will be determined from the location Scheme</param>
        /// <param name="existing">An existing Socket</param>
        /// <param name="leaveOpen"><see cref="LeaveOpen"/></param>
        public RtspClient(Uri location, ClientProtocolType? rtpProtocolType = null, int bufferSize = DefaultBufferSize, Socket existing = null, bool leaveOpen = false)
        {
            if (location == null) throw new ArgumentNullException("location");

            if (false == location.IsAbsoluteUri)
            {
                if (existing == null) throw new ArgumentException("Must be absolute unless a socket is given", "location");
                if (existing.Connected) location = Common.IPEndPointExtensions.ToUri(((IPEndPoint)existing.RemoteEndPoint), (existing.ProtocolType == ProtocolType.Udp ? RtspMessage.UnreliableTransport : RtspMessage.ReliableTransport));
                else if (existing.IsBound) location = Common.IPEndPointExtensions.ToUri(((IPEndPoint)existing.LocalEndPoint), (existing.ProtocolType == ProtocolType.Udp ? RtspMessage.UnreliableTransport : RtspMessage.ReliableTransport));
                else throw new InvalidOperationException("location must be specified when existing socket must be connected or bound.");
            }


            if (false == (location.Scheme.StartsWith(RtspMessage.MessageIdentifier, StringComparison.InvariantCultureIgnoreCase) || location.Scheme != System.Uri.UriSchemeHttp)) throw new ArgumentException("Uri Scheme must be rtsp or rtspu or http", "location");

            //Set the location and determines the m_RtspProtocol and IP Protocol.
            CurrentLocation = location;

            //If the client has specified a Protcol to use then use it
            if (rtpProtocolType.HasValue)
            {
                //Determine if this means anything for Rtp Transport and set the field
                if (rtpProtocolType.Value == ClientProtocolType.Tcp || rtpProtocolType.Value == ClientProtocolType.Http)
                {
                    m_RtpProtocol = ProtocolType.Tcp;
                }
                else if(rtpProtocolType.Value == ClientProtocolType.Udp)
                {
                    m_RtpProtocol = ProtocolType.Udp;                    
                }
                else throw new ArgumentException("Must be Tcp or Udp.", "protocolType");
            }

            //If there is an existing socket
            if (existing != null)
            {
                //Use it
                RtspSocket = existing;
            }

            //If no socket is given a new socket will be created

            //Check for a bufferSize of specified - unspecified value
            //Cases of anything less than or equal to 0 mean use the existing ReceiveBufferSize if possible.
            if (bufferSize <= 0) bufferSize = m_RtspSocket != null ? m_RtspSocket.ReceiveBufferSize : 0;

            //Create the segment given the amount of memory required if possible
            if (bufferSize > 0) m_Buffer = new Common.MemorySegment(bufferSize);
            else m_Buffer = new Common.MemorySegment(DefaultBufferSize); //Use 8192 bytes

            //If leave open is set the socket will not be disposed.
            LeaveOpen = leaveOpen;

            //Set the protocol version to use in requests.
            ProtocolVersion = DefaultProtocolVersion;
            
            //Could create a RtpClient to prevent accidental errors, (would be easier for attaching logger)

            m_RtpClient = new RtpClient();
            m_RtpClient.InterleavedData += ProcessInterleaveData;
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize, if null it will be determined from the location Scheme</param>
        /// <param name="bufferSize">The amount of bytes the client will use during message reception, Must be at least 4096 and if larger it will also be shared with the underlying RtpClient</param>
        public RtspClient(string location, ClientProtocolType? rtpProtocolType = null, int bufferSize = DefaultBufferSize)
            : this(new Uri(location), rtpProtocolType, bufferSize) //UriDecode?
        {
            //Check for a null Credential and UserInfo in the Location given.
            if (Credential == null && false == string.IsNullOrWhiteSpace(CurrentLocation.UserInfo))
            {
                //Parse the given cred from the location
                Credential = Utility.ParseUserInfo(CurrentLocation);

                //Remove the user info from the location (may not have @?)
                CurrentLocation = new Uri(CurrentLocation.AbsoluteUri.Replace(CurrentLocation.UserInfo + (char)Common.ASCII.AtSign, string.Empty).Replace(CurrentLocation.UserInfo, string.Empty));
            }
        }

        ~RtspClient()
        {
            Dispose();
        }

        #endregion

        #region Events

        public delegate void RtspClientAction(RtspClient sender, object args);

        public delegate void RequestHandler(RtspClient sender, RtspMessage request);

        public delegate void ResponseHandler(RtspClient sender, RtspMessage request, RtspMessage response);

        public event RtspClientAction OnConnect;

        internal protected void OnConnected()
        {
            if (IsDisposed) return;

            RtspClientAction action = OnConnect;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }

        }

        public event RequestHandler OnRequest;

        internal protected void Requested(RtspMessage request)
        {
            if (IsDisposed) return;

            RequestHandler action = OnRequest;

            if (action == null) return;

            foreach (RequestHandler handler in action.GetInvocationList())
            {
                try { handler(this, request); }
                catch { continue; }
            }
        }

        public event ResponseHandler OnResponse;

        internal protected void Received(RtspMessage request, RtspMessage response)
        {
            if (IsDisposed) return;
            
            ResponseHandler action = OnResponse;

            if (action == null) return;

            foreach (ResponseHandler handler in action.GetInvocationList())
            {
                try { handler(this, request, response); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnDisconnect;

        internal void OnDisconnected()
        {
            if (IsDisposed) return;

            RtspClientAction action = OnDisconnect;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnPlay;

        internal protected void OnPlaying(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            //Is was not already playing then set the value
            if (false == m_StartedPlaying.HasValue) m_StartedPlaying = DateTime.UtcNow; 

            RtspClientAction action = OnPlay;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnStop;

        internal protected void OnStopping(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            //Is was already playing then set the value
            if (mediaDescription == null && true == m_StartedPlaying.HasValue || m_Playing.Count == 0) m_StartedPlaying = null;

            RtspClientAction action = OnStop;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnPause;

        internal protected void OnPausing(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            RtspClientAction action = OnPause;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        #endregion

        #region Methods

        internal protected virtual void Reconnect(bool reconnectClient = true)
        {
            DisconnectSocket();

            Connect();

            if (reconnectClient && IsPlaying && false == m_RtpClient.IsConnected) m_RtpClient.Connect();
        }

        internal protected virtual void ProcessRemoteGetParameter(RtspMessage get)
        {
            //Todo, Handle other parameters

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, get.Version, get.Encoding))
            {
                //Set the sequence number
                response.CSeq = get.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }
        }

        internal protected virtual void ProcessRemoteSetParameter(RtspMessage set)
        {        
            //MS-RTSP Send a server sent request similar to as follows

            /*
            SET_PARAMETER Location RTSP/1.0
            Content-Type: application/x-wms-extension-cmd
            X-Notice: 2101 "End-of-Stream Reached"
            RTP-Info: Track1, Track2
            X-Playlist-Gen-Id: 358
            Content-Length: 41
            Date: Wed, 04 Feb 2015 19:47:21 GMT
            CSeq: 1
            User-Agent: WMServer/9.1.1.5001\r\n(\r\n) [Breaks Wireshark and causes a freeze until the analyzer can recover (if it does)]
            Session: 6312817326953834859
            EOF: true
             */

            bool effectedMedia = false;

            string contentType = set[RtspHeaders.ContentType];

            if (false == string.IsNullOrWhiteSpace(contentType))
            {
                contentType = contentType.Trim();

                #region [MSRTSP - application/x-wms-extension-cmd]

                if (string.Compare(contentType, "application/x-wms-extension-cmd", true) == 0)
                {
                    string xNotice = set["X-Notice"];

                    if (false == string.IsNullOrWhiteSpace(xNotice)) //&& Boolean.Parse(set["EOF"])
                    {
                        string[] parts = xNotice.Trim().Split(RtspMessage.SpaceSplit, 2);

                        //Get rid of anything unrelated
                        string noticeIdValue = parts.FirstOrDefault();

                        //If something was extracted attempt to parse
                        if (false == string.IsNullOrWhiteSpace(noticeIdValue))
                        {
                            int noticeId;

                            //If the noticeId is 2101
                            if (int.TryParse(noticeIdValue, out noticeId) &&
                                noticeId == 2101)
                            {
                                //End Of Stream notice?

                                //Get the rtp-info header
                                string rtpInfo = set[RtspHeaders.RtpInfo];

                                string[] rtpInfos;

                                //Make a parser class which can be reused?

                                //If parsing of the header succeeded
                                if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
                                {
                                    //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                                    //Loop all found sub header values
                                    foreach (string rtpInfoValue in rtpInfos)
                                    {
                                        Uri uri;

                                        int? rtpTime;

                                        int? seq;

                                        int? ssrc;

                                        //If any value which was needed was found.
                                        if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc) && seq.HasValue)
                                        {
                                            //Just use the ssrc to lookup the context.
                                            if (ssrc.HasValue)
                                            {
                                                //Get the context created with the ssrc defined above
                                                RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                                                //If that context is not null then allow it's ssrc to change now.
                                                if (context != null)
                                                {
                                                    if (m_Playing.Remove(context.MediaDescription))
                                                    {

                                                        effectedMedia = true;

                                                        OnStopping(context.MediaDescription);

                                                        //m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                                    }

                                                    context = null;
                                                }
                                                else
                                                {
                                                    Common.ILoggingExtensions.Log(Logger, "Unknown context for ssrc = " + ssrc.Value);
                                                }
                                            }
                                            else if (uri != null)
                                            {
                                                //Need to get the context by the uri.
                                                //Location = rtsp://abc.com/live/movie
                                                //uri = rtsp://abc.com/live/movie/trackId=0
                                                //uri = rtsp://abc.com/live/movie/trackId=1
                                                //uri = rtsp://abc.com/live/movie/trackId=2

                                                //Get the context created with from the media description with the same resulting control uri
                                                RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                                                //If that context is not null then allow it's ssrc to change now.
                                                if (context != null)
                                                {
                                                    //The last packet will have the sequence number of seq.Value

                                                    if (m_Playing.Remove(context.MediaDescription))
                                                    {
                                                        effectedMedia = true;

                                                        OnStopping(context.MediaDescription);

                                                        //m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                                    }

                                                    context = null;
                                                }
                                                else
                                                {
                                                    Common.ILoggingExtensions.Log(Logger, "Unknown context for Uri = " + uri.AbsolutePath);
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            //Todo, Handle other parameters in the body

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, set.Version, set.Encoding))
            {
                //Indicate OK
                response.StatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = set.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }


            //Check that only the rtx media is playing and then remove session if so

            if (effectedMedia && m_Playing.Count == 1 && m_Playing.First().GetAbsoluteControlUri(CurrentLocation, SessionDescription).AbsoluteUri.EndsWith("rtx", StringComparison.OrdinalIgnoreCase))
            {
                //RemoveSession(m_SessionId);

                StopPlaying();
            }

        }

        internal protected virtual void ProcessRemoteEndOfStream(RtspMessage message)
        {
            //Not playing so we dont care
            if (false == IsPlaying) return;

            /* https://tools.ietf.org/html/draft-zeng-rtsp-end-of-stream-00
            An END_OF_STREAM request MUST include "CSeq", "Range" and "Session"
        headers.
        It SHOULD include "RTP-Info" header.
        The RTP-Info in server's END_OF_STREAM request
        is used to indicate the sequence number of
        the ending RTP packet for each media stream.

        An END_OF_STREAM requet MAY include a new "Reason" header,
        defined as a
        string, whose purpose is to allow the server to explain why stream
        has ended, and whose ABNF definition is given below:

                Reason     =  "Reason" ":"   Reason-Phrase CRLF
            */

            //Ensure Range and RtpInfo are present.
            string range = message[RtspHeaders.Range],

                rtpInfo = message[RtspHeaders.RtpInfo];

            if (string.IsNullOrWhiteSpace(range) || string.IsNullOrWhiteSpace(rtpInfo)) return;

            //Check what is ending...
            //if (m_LastTransmitted.Location == RtspMessage.Wildcard)
            //{

            //}else{
            // Must get stream by location to ensure request is relevent.
            //}

            string[] rtpInfos;

            //Make a parser class which can be reused?

            //If parsing of the header succeeded
            if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
            {
                //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                //Loop all found sub header values
                foreach (string rtpInfoValue in rtpInfos)
                {
                    Uri uri;

                    int? rtpTime;

                    int? seq;

                    int? ssrc;

                    //If any value which was needed was found.
                    if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc))
                    {
                        //Just use the ssrc to lookup the context.
                        if (ssrc.HasValue)
                        {
                            //Get the context created with the ssrc defined above
                            RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                            //If that context is not null then allow it's ssrc to change now.
                            if (context != null)
                            {
                                if (m_Playing.Remove(context.MediaDescription))
                                {
                                    OnStopping(context.MediaDescription);

                                    m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                }

                                context = null;
                            }
                        }
                        else if (uri != null)
                        {
                            //Need to get the context by the uri.
                            //Location = rtsp://abc.com/live/movie
                            //uri = rtsp://abc.com/live/movie/trackId=0
                            //uri = rtsp://abc.com/live/movie/trackId=1
                            //uri = rtsp://abc.com/live/movie/trackId=2

                            //Get the context created with from the media description with the same resulting control uri
                            RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                            //If that context is not null then allow it's ssrc to change now.
                            if (context != null)
                            {
                                if (m_Playing.Remove(context.MediaDescription))
                                {
                                    OnStopping(context.MediaDescription);

                                    m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                }

                                context = null;
                            }

                        }
                    }
                }
            }

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, message.Version, message.Encoding))
            {
                //Indicate OK
                response.StatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = message.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }

            StopPlaying();
        }

        //could handle EndOfStream as a PlayNotify...

        internal protected virtual void ProcessRemoteTeardown(RtspMessage teardown)
        {
            //If playing
            if (false == IsPlaying) return;

            //Check if everything is stopping
            if (Uri.Equals(teardown.Location, CurrentLocation) || teardown.Location == RtspMessage.Wildcard)
            {
                OnStopping();

                m_RtpClient.SendGoodbyes();
            }
            else//Use the Uri to determine what is shutting down
            {
                //Attempt to find the media by the uri given.
                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                //uri's follow the format /.././Type
                if (Enum.TryParse<Sdp.MediaType>(teardown.Location.Segments.Last(), true, out mediaType))
                {
                    //Find a contet for the type given
                    var context = Client.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.MediaType == mediaType);

                    //If a context was found
                    if (context != null)
                    {
                        //If it was playing
                        if (m_Playing.Contains(context.MediaDescription))
                        {
                            //Indicate this media is stopping now
                            OnStopping(context.MediaDescription);

                            m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                        }

                        //remove the reference to the context
                        context = null;
                    }
                }
            }

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, teardown.Version, teardown.Encoding))
            {
                //Indicate OK
                response.StatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = teardown.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }
        }

        internal protected virtual void ProcessRemotePlayNotify(RtspMessage playNotify)
        {
            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, playNotify.Version, playNotify.Encoding))
            {
                //Indicate OK
                response.StatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = playNotify.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }
        }

        protected virtual void ProcessServerSentRequest(RtspMessage toProcess = null)
        {

            if (toProcess == null) return;

            if (false == IgnoreServerSentMessages &&
                toProcess == null ||
                toProcess.MessageType != RtspMessageType.Request ||
                false == toProcess.IsComplete) return;

            //Check the sequence number
            int sequenceNumber = toProcess.CSeq;

            //Don't handle a request with an invalid remote sequence number
            if (sequenceNumber < m_RCSeq) return;

            //Update the remote sequence number
            m_RCSeq = sequenceNumber;

            //Increment handled pushed messages
            ++m_PushedMessages;

            //Raise an event for the request received.
            Received(toProcess, null);

            //Determine 
            string session = m_LastTransmitted[RtspHeaders.Session];

            if (false == string.IsNullOrWhiteSpace(m_SessionId) &&
                false == string.IsNullOrWhiteSpace(session))
            {
                //Not for the same session
                if (m_SessionId != session.Trim())
                {
                    return;
                }
            }

            //handle the message received
            switch (toProcess.Method)
            {
                case RtspMethod.TEARDOWN:
                    {
                        ProcessRemoteTeardown(toProcess);

                        return;
                    }
                case RtspMethod.ANNOUNCE:
                    {
                        if (Uri.Equals(toProcess.Location, CurrentLocation))
                        {
                            //Check for SDP content type and update the SessionDescription
                        }

                        return;
                    }
                case RtspMethod.GET_PARAMETER:
                    {

                        ProcessRemoteGetParameter(toProcess);

                        return;
                    }
                case RtspMethod.SET_PARAMETER:
                    {
                        ProcessRemoteSetParameter(toProcess);

                        break;
                    }
                case RtspMethod.PLAY_NOTIFY:
                    {
                        /*
                         There are two ways for the client to be informed about changes of
                        media resources in Play state.  The client will receive a PLAY_NOTIFY
                        request with Notify-Reason header set to media-properties-update (see
                        Section 13.5.2.  The client can use the value of the Media-Range to
                        decide further actions, if the Media-Range header is present in the
                        PLAY_NOTIFY request.  The second way is that the client issues a
                        GET_PARAMETER request without a body but including a Media-Range
                        header.  The 200 OK response MUST include the current Media-Range
                        header (see Section 18.30).
                         */

                        //Use the Uri to determine what is chaning.
                        if (Uri.Equals(toProcess.Location, CurrentLocation))
                        {
                            //See what is being notified.
                        }
                        
                        break;
                    }
                default:
                    {
                        
                        //Something else...


                        if (toProcess.MethodString == "END_OF_STREAM")
                        {

                            ProcessRemoteEndOfStream(toProcess);

                            return;
                        }

                         //Make a response to indicate the method is not supported
                        using (var response = new RtspMessage(RtspMessageType.Response, toProcess.Version, toProcess.Encoding))
                        {
                            //Indicate Not Allowed.
                            response.StatusCode = RtspStatusCode.NotImplemented;

                            //Set the sequence number
                            response.CSeq = toProcess.CSeq;

                            //Send it
                            using (SendRtspMessage(response, false, false)) ;
                        }


                        return;
                    }
            }
        }

        /// <summary>
        /// Handles Interleaved Data for the RtspClient by parsing the given memory for a valid RtspMessage.
        /// </summary>
        /// <param name="sender">The RtpClient instance which called this method</param>
        /// <param name="memory">The memory to parse</param>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        //Todo make a better name e.g. HandleReceivedData
        void ProcessInterleaveData(object sender, byte[] data, int offset, int length)
        {
            if (length == 0) return;

            //Cache offset and count, leave a register for received data (should be calulated with length)
            int received = 0;

            //Must contain textual data to be an interleaved rtsp request.
            //if (!Utility.FoundValidUniversalTextFormat(data, ref offset, ref length)) return; //See comments below if attempting to complete large packets.

            //Should check for BigEndianFrameControl @ offset which indicates a large packet or a packet under 8 bytes.
            //In such a case the length needs to be read and if the packet was larger than the buffer the next time this event fires the remaining data will be given
            //When reading sizes the frame size should ALWAYS be <= any Blocksize the server responded with (if any)
            //Then the data can be given back to the RtpClient with ProcessFrameData when the packet is complete.
            //If another packet arrives while one is being completed that is up to the implementation to deal with for now, other implementations just drop the data and give you no way to even receive it.            

            unchecked
            {
                //Validate the data received
                RtspMessage interleaved = new RtspMessage(data, offset, length);

                //Determine what to do with the interleaved message
                switch (interleaved.MessageType)
                {
                    //Handle new requests or responses
                    case RtspMessageType.Request:
                    case RtspMessageType.Response:
                        {
                            //Calculate the length of what was received
                            received = length;

                            if (received > 0)
                            {
                                //Increment for messages received
                                ++m_ReceivedMessages;

                                //If not playing an interleaved stream, Complete the message if not complete (Should maybe check for Content-Length)
                                while (false == SharesSocket && false == interleaved.IsComplete)
                                {
                                    //Take in some bytes from the socket
                                    int justReceived = interleaved.CompleteFrom(m_RtspSocket, m_Buffer);

                                    if (justReceived == 0) break;
                                         
                                    //Incrment for justReceived
                                    received += justReceived;

                                    //Ensure we are not doing to much receiving
                                    if (received > RtspMessage.MaximumLength + interleaved.ContentLength) break;
                                }

                                //Update counters
                                m_ReceivedBytes += received;

                                //Disposes the last message if it exists.
                                if (m_LastTransmitted != null)
                                {
                                    m_LastTransmitted.Dispose();

                                    m_LastTransmitted = null;
                                }

                                //Store the last message
                                m_LastTransmitted = interleaved;

                                //if the message was a request and is complete handle it now.
                                if (m_LastTransmitted.MessageType == RtspMessageType.Request &&                                    
                                    m_InterleaveEvent.IsSet && 
                                    interleaved.IsComplete)
                                {
                                    //Ensure suported methods contains the method requested.
                                    m_SupportedMethods.Add(interleaved.MethodString);

                                    ProcessServerSentRequest(m_LastTransmitted);
                                }

                            }

                            goto default;
                        }
                    case RtspMessageType.Invalid:
                        {
                            //Dispose the invalid message
                            interleaved.Dispose();

                            interleaved = null;

                            //If playing and interleaved stream AND the last transmitted message is NOT null and is NOT Complete then attempt to complete it
                            if (m_LastTransmitted != null)
                            {
                                //RtspMessage local = m_LastTransmitted;

                                //Take note of the length of the last transmitted message.
                                int lastLength = m_LastTransmitted.Length;

                                //Create a memory segment and complete the message as required from the buffer.
                                using (var memory = new Media.Common.MemorySegment(data, offset, length))
                                {
                                    //Use the data recieved to complete the message and not the socket
                                    int justReceived = m_LastTransmitted.CompleteFrom(null, memory);

                                    //If anything was received
                                    if (justReceived > 0)
                                    {
                                        //Account for what was just recieved.
                                        received += justReceived;

                                        //No data was consumed don't raise another event.
                                        if (lastLength == m_LastTransmitted.Length) received = 0;
                                    }

                                    //handle the completion of a request sent by the server if allowed.
                                    if (received > 0 &&
                                        m_LastTransmitted != null && false == m_LastTransmitted.IsDisposed &&
                                        m_LastTransmitted.MessageType == RtspMessageType.Request && 
                                        m_InterleaveEvent.IsSet) //dont handle if waiting for a resposne...
                                    {
                                        //Process the pushed message
                                        ProcessServerSentRequest(m_LastTransmitted);

                                        //then continue
                                    }
                                }
                            }

                            //Handle with default logic.
                            goto default;
                        }
                    default:
                        {
                            //If anything was received
                            if (received > 0)
                            {

                                //Todo could have TransactionCache with requests which are then paried to response here.

                                //Release the m_Interleaved event if it was set
                                if (false == m_InterleaveEvent.IsSet)
                                {
                                    //Thus allowing threads blocked by it to proceed.
                                    m_InterleaveEvent.Set();
                                } //Otherwise
                                else if (m_LastTransmitted != null && //Ensure there was a message
                                    false == m_LastTransmitted.IsDisposed && //Which was not disposed
                                    m_LastTransmitted.MessageType == RtspMessageType.Response) //and was a request
                                {
                                    //Otherwise indicate a message has been received now. (for responses only)
                                    Received(m_LastTransmitted, null);
                                }

                                //Handle any data remaining in the buffer
                                if (received < length)
                                {
                                    //(Must ensure Length property of RtspMessage is exact).
                                    ProcessInterleaveData(sender, data, received, length - received);
                                }
                            }

                            //done
                            return;
                        }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        /// <summary>
        /// Increments and returns the current SequenceNumber
        /// </summary>
        internal int NextClientSequenceNumber() { return ++m_CSeq; }

        //Should have end time also?
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StartPlaying(TimeSpan? start = null, TimeSpan? end = null, Sdp.MediaType? mediaType = null)
        {
            //Is already playing don't do anything
            if (IsPlaying) return;

            //Try to connect if not already connected.
            if (false == IsConnected) Connect();

            //Send the options if nothing was received before
            if (m_ReceivedMessages == 0) using (var options = SendOptions()) ;

            //Send describe if we need a session description
            if (SessionDescription == null) using (var describe = SendDescribe()) if (describe == null || describe.StatusCode != RtspStatusCode.OK) Common.ExceptionExtensions.RaiseTaggedException(describe, "Describe Response was null or not OK. See Tag.");

            //Determine if any context was present or created.
            bool hasContext = false;

            List<MediaDescription> setupMedia = new List<MediaDescription>();

            //For each MediaDescription in the SessionDecscription (ordered by the media type) and then reversed to ensure wms rtx going first (but it doesn't seem to matter anyway)
            //What could be done though is to use the detection of the rtx track to force interleaved playback.
            foreach (Sdp.MediaDescription md in SessionDescription.MediaDescriptions)//.OrderBy(md=> md.MediaType).Reverse())
            {
                //Don't setup unwanted streams
                if (mediaType.HasValue && md.MediaType != mediaType) continue;

                //If transport was already setup then see if the transport has a context for the media
                if (Client != null)
                {
                    //Get the context for the media
                    var context = Client.GetContextForMediaDescription(md);

                    //If there is a context which is not already playing and has not ended
                    if (context != null) if (false == m_Playing.Contains(context.MediaDescription))
                    {
                        //If the context is no longer receiving (should be a property on TransportContext but when pausing the RtpClient doesn't know about this)
                        if (context.TimeReceiving == context.TimeSending && context.TimeSending == Utility.InfiniteTimeSpan)
                        {
                            //Remove the context
                            Client.TryRemoveContext(context);

                            //Dispose it
                            context.Dispose();

                            //remove the reference
                            context = null;
                        }
                        //else context.Goodbye = null;
                    }
                    else
                    {
                        setupMedia.Add(md);

                        //The media is already playing
                        hasContext = true;

                        continue;
                    }
                }

                //Send a setup
                using (RtspMessage setup = SendSetup(md))
                {

                    if (setup == null)
                    {
                        hasContext = true;

                        setupMedia.Add(md);

                        continue;
                    }

                    //If the setup was okay
                    if (setup != null && setup.StatusCode == RtspStatusCode.OK)
                    {
                        //Only setup tracks if response was OK
                        hasContext = true;

                        //Add the media to the list of what was setup.
                        setupMedia.Add(md);

                        #region Unused Feature [NewSocketEachSetup]

                        //Testing if a new socket can be used with each setup
                        // if(NewSocketEachSetup) { Reconnect(); }

                        #endregion
                    }
                    //else if (setup.StatusCode == RtspStatusCode.UnsupportedTransport)
                    //{
                    //    //Check for a 'rtx' connection
                    //}
                }
            }

            //If we have a play context then send the play request.
            if (false == hasContext) throw new InvalidOperationException("Cannot Start Playing, No Tracks Setup.");

            //Send the play request
            using (RtspMessage play = SendPlay(CurrentLocation, start ?? StartTime, end ?? EndTime))
            {
                //If there was a response or not fire a playing event.
                if (play == null || 
                    play != null && 
                    play.MessageType == RtspMessageType.Invalid ||
                    play.StatusCode == RtspStatusCode.OK)
                {
                    foreach (var media in setupMedia) if (false == m_Playing.Contains(media)) m_Playing.Add(media);

                    OnPlaying();

                    //Set EndTime
                }
            }

            //Connect and wait for Packets
            if (m_RtpClient != null && false == m_RtpClient.IsConnected) m_RtpClient.Connect();

            TimeSpan halfSessionTimeWithConnection = TimeSpan.FromTicks(m_RtspSessionTimeout.Subtract(m_ConnectionTime).Ticks / 2);

            //If dueTime is zero (0), callback is invoked immediately. If dueTime is negative one (-1) milliseconds, callback is not invoked; the timer is disabled, but can be re-enabled by calling the Change method.
            //Setup a timer to send any requests to keep the connection alive and ensure media is flowing.
            //Subtract against the connection time... the averge rtt would be better
            if (m_KeepAliveTimer == null) m_KeepAliveTimer = new Timer(new TimerCallback(SendKeepAlive), null, halfSessionTimeWithConnection, Utility.InfiniteTimeSpan);

            TimeSpan protocolSwitchTime = halfSessionTimeWithConnection.Subtract(m_RtpClient.GetTransportContexts().Max(tc => tc.ReceiveInterval));

            //If the protocol switch feature is enabled.
            if (AllowAlternateTransport) m_ProtocolSwitchTimer = new System.Threading.Timer(new TimerCallback(SwitchProtocols), null, protocolSwitchTime, Utility.InfiniteTimeSpan);

            //Don't keep the tcp socket open when not required under Udp.
            //if (m_RtpProtocol == ProtocolType.Udp) DisconnectSocket();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StopPlaying()
        {
            if (IsDisposed || false == IsPlaying) return;
            try { Disconnect(); }
            catch (Exception ex) { Media.Common.ILoggingExtensions.Log(Logger, ex.Message); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Pause(MediaDescription mediaDescription = null, bool force = false)
        {
            //Don't pause if playing.
            if (false == force && false == IsPlaying) return;

            var context = Client.GetContextForMediaDescription(mediaDescription);

            //Dont pause media which is not setup unless forced.
            if (false == force && mediaDescription != null && context == null) return;

            //context.Goodbye = null;

            //Send the pause.
            SendPause(mediaDescription, force);
        }

        /// <summary>
        /// Sends a SETUP if not already setup and then a PLAY for the given.
        /// If nothing is given this would be equivalent to calling <see cref="StartPlaying"/>
        /// </summary>
        /// <param name="mediaDescription"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Play(MediaDescription mediaDescription = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt")
        {
            bool playing = IsPlaying;
            //If already playing and nothing was given then there is nothing to do
            if (playing && mediaDescription == null) return;
            else if (false == playing) //We are not playing and nothing was given.
            {
                //Start playing everything
                StartPlaying();

                //do nothing else
                return;
            }
            
            var context = Client.GetContextForMediaDescription(mediaDescription);

            //Dont setup media which is already setup.
            if (mediaDescription != null && context == null) return;            

            //setup the media description
            using (var setupResponse = SendSetup(mediaDescription))
            {
                //If the response was OKAY
                if (setupResponse != null && setupResponse.StatusCode == RtspStatusCode.OK)
                {

                    //context.Goodbye = null;

                    //Send the PLAY.
                    using (SendPlay(mediaDescription, startTime, endTime, rangeType)) ;
                }
            }            
        }

        /// <summary>
        /// If <see cref="IsConnected"/> and not forced an <see cref="InvalidOperationException"/> will be thrown.
        /// 
        /// <see cref="DisconnectSocket"/> is called if there is an existing socket.
        /// 
        /// Creates any required client socket stored the time the call was made and calls <see cref="ProcessEndConnect"/> unless an unsupported Proctol is specified.
        /// </summary>
        /// <param name="force">Indicates if a previous existing connection should be disconnected.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Connect(bool force = false)
        {
            try
            {
                //Ensure logic for UDP is correct, may have to store flag.
                if (false == force && IsConnected) throw new InvalidOperationException("Client already Connected.");
                
                //Disconnect any existing previous socket and erase connect times.
                if (m_RtspSocket != null) DisconnectSocket();
                

                //Based on the ClientProtocolType
                switch (m_RtspProtocol)
                {
                    case ClientProtocolType.Http:
                    case ClientProtocolType.Tcp:
                        {
                            /*  9.2 Reliability and Acknowledgements
                             If a reliable transport protocol is used to carry RTSP, requests MUST
                             NOT be retransmitted; the RTSP application MUST instead rely on the
                             underlying transport to provide reliability.
                             * 
                             If both the underlying reliable transport such as TCP and the RTSP
                             application retransmit requests, it is possible that each packet
                             loss results in two retransmissions. The receiver cannot typically
                             take advantage of the application-layer retransmission since the
                             transport stack will not deliver the application-layer
                             retransmission before the first attempt has reached the receiver.
                             If the packet loss is caused by congestion, multiple
                             retransmissions at different layers will exacerbate the congestion.
                             * 
                             If RTSP is used over a small-RTT LAN, standard procedures for
                             optimizing initial TCP round trip estimates, such as those used in
                             T/TCP (RFC 1644) [22], can be beneficial.
                             * 
                            The Timestamp header (Section 12.38) is used to avoid the
                            retransmission ambiguity problem [23, p. 301] and obviates the need
                            for Karn's algorithm.
                             * 
                           Each request carries a sequence number in the CSeq header (Section
                           12.17), which is incremented by one for each distinct request
                           transmitted. If a request is repeated because of lack of
                           acknowledgement, the request MUST carry the original sequence number
                           (i.e., the sequence number is not incremented).
                             * 
                           Systems implementing RTSP MUST support carrying RTSP over TCP and MAY
                           support UDP. The default port for the RTSP server is 554 for both UDP
                           and TCP.
                             * 
                           A number of RTSP packets destined for the same control end point may
                           be packed into a single lower-layer PDU or encapsulated into a TCP
                           stream. RTSP data MAY be interleaved with RTP and RTCP packets.
                           Unlike HTTP, an RTSP message MUST contain a Content-Length header
                           whenever that message contains a payload. Otherwise, an RTSP packet
                           is terminated with an empty line immediately following the last
                           message header.
                             * 
                            */

                            //Create the socket
                            m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            m_RtspSocket.ExclusiveAddressUse = false;

                            m_RtspSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                            // Set option that allows socket to close gracefully without lingering.
                            //e.g. DON'T Linger on close if unsent data is present. (Should be moved to ISocketReference)
                            m_RtspSocket.DontLinger();

                            //Use nagle's sliding window (disables send coalescing)
                            m_RtspSocket.NoDelay = true;

                            //Dont fragment
                            if(m_RtspSocket.AddressFamily == AddressFamily.InterNetwork) m_RtspSocket.DontFragment = true;

                            //Use expedited data as defined in RFC-1222. This option can be set only once; after it is set, it cannot be turned off.
                            m_RtspSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);

                            //Don't buffer send.
                            m_RtspSocket.SendBufferSize = 0;

                            break;
                        }
                    case ClientProtocolType.Udp:
                        {
                            //Create the socket
                            m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            break;
                        }
                    default: throw new NotSupportedException("The given ClientProtocolType is not supported.");
                }

                //We started connecting now.
                m_BeginConnect = DateTime.UtcNow;

                //Handle the connection attempt (Assumes there is already a RemoteRtsp value)
                ProcessEndConnect(null);

            }
            catch(Exception ex)
            {
                Common.ILoggingExtensions.Log(Logger, ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Calls Connect on the underlying socket.
        /// 
        /// Marks the time when the connection was established.
        /// 
        /// Increases the <see cref="SocketWriteTimeout"> AND <see cref="SocketReadTimeout"/> by the time it took to establish the connection in milliseconds * 2.
        /// 
        /// </summary>
        /// <param name="state">Ununsed.</param>
        protected virtual void ProcessEndConnect(object state)
        {
            try
            {
                if (m_RemoteRtsp == null) throw new InvalidOperationException("A remote end point must be assigned");

                //Try to connect
                m_RtspSocket.Connect(m_RemoteRtsp);

                //Sample the clock after connecting
                m_EndConnect = DateTime.UtcNow;

                //Calculate the connection time.
                m_ConnectionTime = m_EndConnect.Value - m_BeginConnect.Value;

                //Set the read and write timeouts based upon such a time (should include a min of the m_RtspSessionTimeout.)
                if (m_ConnectionTime > TimeSpan.Zero) SocketWriteTimeout = SocketReadTimeout += (int)(m_ConnectionTime.TotalMilliseconds * 2);

                //Raise the Connected event.
                OnConnected();
            }
            catch { throw; }
        }

        //handle exception, really needs to know what type of operation this was also.(read or write)
        //virtual void HandleSocketException(SocketException exception, bool wasReading, wasWriting)
        //{

        //}

        /// <summary>
        /// If <see cref="IsConnected"/> nothing occurs.
        /// Disconnects the RtspSocket if Connected and <see cref="LeaveOpen"/> is false.  
        /// Sets the <see cref="ConnectionTime"/> to <see cref="Utility.InfiniteTimepan"/> so IsConnected is false.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void DisconnectSocket(bool force = false)
        {
            //If not connected and not forced return
            if (false == IsConnected && false == force) return;
                
            //Raise an event
            OnDisconnected();

            //If there is a socket
            if (m_RtspSocket != null)
            {
                //If leave open was false
                if (false == LeaveOpen)
                {
                    #region The Great Debate on Closing

                    //Don't allow further sending
                    //m_RtspSocket.Shutdown(SocketShutdown.Send);

                    //Should receive any data in buffer while not getting 0?

                    //m_RtspSocket.Close();

                    //May take to long because of machine level settings.
                    //m_RtspSocket.Disconnect(true);

                    #endregion

                    m_RtspSocket.Dispose();
                }
                
                //Set the socket to null
                m_RtspSocket = null;
            }

            //Indicate not connected.
            m_BeginConnect = m_EndConnect = null;

            m_ConnectionTime = Utility.InfiniteTimeSpan;
        }

        /// <summary>
        /// Stops Sending any KeepAliveRequests.
        /// 
        /// Stops the Protocol Switch Timer.
        /// 
        /// If <see cref="IsPlaying"/> is true AND there is an assigned <see cref="SessionId"/>,
        /// Stops any playing media by sending a TEARDOWN for the current <see cref="CurrentLocation"/> 
        /// 
        /// Disconnects any connected Transport which is still connected.
        /// 
        /// Calls DisconnectSocket.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Disconnect()
        {
            //Get rid of the timers
            if (m_KeepAliveTimer != null)
            {
                m_KeepAliveTimer.Dispose();
                m_KeepAliveTimer = null;
            }

            if (m_ProtocolSwitchTimer != null)
            {
                m_ProtocolSwitchTimer.Dispose();
                m_ProtocolSwitchTimer = null;
            }

            //Determine if we need to do anything
            if (IsPlaying && false == string.IsNullOrWhiteSpace(m_SessionId))
            {
                //Send the Teardown
                try
                {
                    //Don't really care if the response is received or not
                    using (SendTeardown()) ;
                }
                catch
                {
                    //We may not recieve a response if the socket is closed in a violatile fashion on the sending end
                    //And we realy don't care
                }
                finally
                {
                    m_SessionId = string.Empty;
                }
            }     
       
            if (Client != null && Client.IsConnected) Client.Disconnect();

            DisconnectSocket();
        }

        #endregion

        #region Rtsp

        /// <summary>
        /// Uses the given request to Authenticate the RtspClient when challenged.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public virtual RtspMessage Authenticate(RtspMessage request, RtspMessage response = null, bool force = false)
        {
            if (false == force && m_TriedCredentials) return null;

            //http://tools.ietf.org/html/rfc2617
            //3.2.1 The WWW-Authenticate Response Header
            //Example
            //WWW-Authenticate: Basic realm="nmrs_m7VKmomQ2YM3:", Digest realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525"\r\n

            //Needs to handle multiple auth types

            string authenticateHeader = response != null ? response[RtspHeaders.WWWAuthenticate] : string.Empty;

            string[] baseParts = authenticateHeader.Split(((char)Common.ASCII.Space).Yield().ToArray(), 2, StringSplitOptions.RemoveEmptyEntries);

            if (baseParts.Length > 1) baseParts = baseParts[0].Yield().Concat(baseParts[1].Split(RtspHeaders.Comma).Select(s => s.Trim())).ToArray();

            if (string.Compare(baseParts[0].Trim(), "basic", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Basic)
            {
                AuthenticationScheme = AuthenticationSchemes.Basic;

                request.SetHeader(RtspHeaders.Authorization, RtspHeaders.BasicAuthorizationHeader(request.Encoding, Credential));

                //Indicate credentials were tried.
                m_TriedCredentials = true;

                //Recurse the call with the info from then authenticate header
                return SendRtspMessage(request);

            }
            else if (string.Compare(baseParts[0].Trim(), "digest", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Digest)
            {
                AuthenticationScheme = AuthenticationSchemes.Digest;

                string algorithm = "MD5";

                string username = baseParts.Where(p => p.StartsWith("username", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(username)) username = username.Substring(9);
                else username = Credential.UserName; //use the username of the credential.

                string realm = Credential.Domain;

                //Get the realm if we don't have one.
                if (string.IsNullOrWhiteSpace(realm))
                {
                    //Check for the realm token
                    realm = baseParts.Where(p => p.StartsWith("realm", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                    //If it was there
                    if (false == string.IsNullOrWhiteSpace(realm))
                    {
                        //Parse it
                        realm = realm.Substring(6).Replace("\"", string.Empty).Replace("\'", string.Empty).Trim();

                        //Store it
                        Credential.Domain = realm;
                    }
                }

                string nc = baseParts.Where(p => p.StartsWith("nc", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(nc)) nc = realm.Substring(3);

                string nonce = baseParts.Where(p => p.StartsWith("nonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(nonce)) nonce = nonce.Substring(6).Replace("\"", string.Empty).Replace("\'", string.Empty);

                string cnonce = baseParts.Where(p => p.StartsWith("cnonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();//parts.Where(p => string.Compare("cnonce", p, true) == 0).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(cnonce)) cnonce = cnonce.Substring(7).Replace("\"", string.Empty).Replace("\'", string.Empty);//cnonce = cnonce.Replace("cnonce=", string.Empty);

                string uri = baseParts.Where(p => p.StartsWith("uri", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); //parts.Where(p => p.Contains("uri")).FirstOrDefault();
                bool rfc2069 = false == string.IsNullOrWhiteSpace(uri) && !uri.Contains(RtspHeaders.HyphenSign);

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    if (rfc2069) uri = uri.Substring(4);
                    else uri = uri.Substring(11);
                }

                string qop = baseParts.Where(p => string.Compare("qop", p, true) == 0).FirstOrDefault();

                if (false == string.IsNullOrWhiteSpace(qop))
                {
                    qop = qop.Replace("qop=", string.Empty);
                    if (nc != null) nc = nc.Substring(3);
                }

                string opaque = baseParts.Where(p => p.StartsWith("opaque", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(opaque)) opaque = opaque.Substring(7);

                request.SetHeader(RtspHeaders.Authorization, RtspHeaders.DigestAuthorizationHeader(request.Encoding, request.Method, request.Location, Credential, qop, nc, nonce, cnonce, opaque, rfc2069, algorithm, request.Body));

                //Todo 'Authorization' property?

                //Indicate credentials were tried.
                m_TriedCredentials = true;

                //Recurse the call with the info from then authenticate header
                return SendRtspMessage(request);
            }
            else
            {
                throw new NotSupportedException("The given Authorization type is not supported, '" + baseParts[0] + "' Please use Basic or Digest.");
            }
        }

        //hasResponse could be set automatically by MessageType.

        public RtspMessage SendRtspMessage(RtspMessage message, bool useClientProtocolVersion = true, bool hasResponse = true)
        {
            SocketError result;
            return SendRtspMessage(message, out result, useClientProtocolVersion, hasResponse);
        }

        public RtspMessage SendRtspMessage(RtspMessage message, out SocketError error, bool useClientProtocolVersion = true, bool hasResponse = true)
        {
            //Indicate a send has not been attempted
            error = SocketError.SocketError;

            //Don't try to send if already disposed.
            CheckDisposed();

            try
            {                
                //Ensure the request version matches the protocol version of the client if enforceVersion is true.
                if (useClientProtocolVersion && message.Version != ProtocolVersion) message.Version = ProtocolVersion;

                //Add the user agent
                if (false == message.ContainsHeader(RtspHeaders.UserAgent))
                {
                    message.SetHeader(RtspHeaders.UserAgent, m_UserAgent);
                }

                //If there not already an Authorization header and there is an AuthenticationScheme utilize the information in the Credential
                if (false == message.ContainsHeader(RtspHeaders.Authorization) && m_AuthenticationScheme != AuthenticationSchemes.None && Credential != null)
                {
                    //Basic
                    if (m_AuthenticationScheme == AuthenticationSchemes.Basic)
                    {
                        message.SetHeader(RtspHeaders.Authorization, RtspHeaders.BasicAuthorizationHeader(message.Encoding, Credential));
                    }
                    else if (m_AuthenticationScheme == AuthenticationSchemes.Digest)
                    {
                        //Digest
                        message.SetHeader(RtspHeaders.Authorization,
                            RtspHeaders.DigestAuthorizationHeader(message.Encoding, message.Method, message.Location, Credential, null, null, null, null, null, false, null, message.Body));
                    }
                }

                //Add the content encoding header
                if (false == message.ContainsHeader(RtspHeaders.ContentEncoding)) message.SetHeader(RtspHeaders.ContentEncoding, message.Encoding.WebName);                

                //Set the date
                if (false == message.ContainsHeader(RtspHeaders.Date)) message.SetHeader(RtspHeaders.Date, DateTime.UtcNow.ToString("r"));

                ///Use the sessionId if present and not already contained.
                if (false == string.IsNullOrWhiteSpace(m_SessionId) && false == message.ContainsHeader(RtspHeaders.Session)) message.SetHeader(RtspHeaders.Session, m_SessionId);

                //Get the next Sequence Number and set it in the request. (If not already present)
                if (false == message.ContainsHeader(RtspHeaders.CSeq)) message.CSeq = NextClientSequenceNumber();

                //Set the Timestamp header if not already set to the amount of seconds since the connection started.
                string timestamp;

                if (false == message.ContainsHeader(RtspHeaders.Timestamp))
                {
                    timestamp = (DateTime.UtcNow - m_EndConnect ?? TimeSpan.Zero).TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    message.SetHeader(RtspHeaders.Timestamp, timestamp);
                }
                else timestamp = message[RtspHeaders.Timestamp];

                //Use any additional headers if given
                if (AdditionalHeaders.Count > 0) foreach (var additional in AdditionalHeaders) message.AppendOrSetHeader(additional.Key, additional.Value);

                //Wait for any existing requests to finish first
                bool wasBlocked = m_InterleaveEvent.IsSet;

                //If was block wait for that to finish
                //if (wasBlocked) m_InterleaveEvent.Wait();

                //Connect if not connected.
                bool wasConnected = IsConnected;

                if (false == wasConnected) Connect();

                //If the client is not connected then nothing can be done.
                if (false == IsConnected) return null;

                //We are connected.
                wasConnected = true;

                //Get the bytes of the request
                byte[] buffer = m_RtspProtocol == ClientProtocolType.Http ? RtspMessage.ToHttpBytes(message) : message.ToBytes();

                int retransmits = 0, attempt = 0, //The attempt counter itself
                    sent = 0, received = 0, //counter for sending and receiving locally
                    offset = m_Buffer.Offset, length = buffer.Length;

                //Set the block if a response is required.
                if(hasResponse) m_InterleaveEvent.Reset();                

            Send:
                unchecked
                {
                    //If we can write
                    if (m_RtspSocket.Poll((int)Math.Round(m_RtspSessionTimeout.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectWrite))
                    {
                        sent += m_RtspSocket.Send(buffer, sent, length - sent, SocketFlags.None, out error);
                    }

                    #region Auto Reconnect

                    //////Handle the error
                    ////if (sent < length || error != SocketError.Success)
                    ////{
                    ////    //Check for fatal errors
                    ////    if (error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset)
                    ////    {
                    ////        //Check for the host to have dropped the connection
                    ////        if (error == SocketError.ConnectionReset)
                    ////        {
                    ////            //Check if the client was connected already
                    ////            if (wasConnected && false == IsConnected)
                    ////            {
                    ////               Reconnect(true);

                    ////                goto Send;
                    ////            }
                    ////        }

                    ////        if (false == wasBlocked) m_InterleaveEvent.Set();

                    ////        return null;
                    ////    }
                    ////}

                    #endregion

                    //If this is not a re-transmit
                    if (sent >= length)
                    {
                        //Set the time when the message was transferred is this is not a retransmit.
                        message.Transferred = DateTime.UtcNow;

                        //Fire the event
                        Requested(message);

                        //Increment for messages sent or the messages retransmitted.
                        ++m_SentMessages;

                        //Increment our byte counters for Rtsp
                        m_SentBytes += sent;

                        //Attempt to receive so start attempts back at 0
                        sent = attempt = 0;
                    }
                    else if (sent < length && ++attempt < m_ResponseTimeoutInterval)
                    {
                        //Make another attempt @
                        //Sending the rest
                        goto Send;
                    }

                    //If 'hasResponse' was set and the message is not a request, then don't wait.
                    //if (hasResponse) hasResponse = message.MessageType == RtspMessageType.Request;

                    //Check for no response.
                    if (false == hasResponse) return null;

                    //If the socket is shared the response will be propagated via an event.
                    if (SharesSocket) goto Wait;

                    //Receive some data (only referenced by the check for disconnection)
                Receive:

                    //If we can
                    if (m_RtspSocket.Poll((int)Math.Round(m_RtspSessionTimeout.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                    {
                        received += m_RtspSocket.Receive(m_Buffer.Array, offset, m_Buffer.Count, SocketFlags.None, out error);
                    }

                    #region Auto Reconnect

                    ////Check for the host to have dropped the connection.
                    //if (error == SocketError.ConnectionReset)
                    //{
                    //    if (wasConnected && false == IsConnected)
                    //    {
                    //        Reconnect(true);                    
                    //    }

                    //    goto Receive;
                    //}

                    #endregion

                    //If anything was received
                    if (received > 0)
                    {
                        //TODO
                        //RtspClient.TransportContext must handle the reception because it must strip away the RTSP and process only the interleaved data in a frame.
                        //Right now just pass it to the RtpClient.
                        if (m_RtpClient != null && m_Buffer.Array[offset] == Media.Rtp.RtpClient.BigEndianFrameControl)
                        {
                            
                            //Some people just start sending packets hoping that the context will be created dynamically.
                            //I guess you could technically skip describe and just receive everything raising events as required...
                            //White / Black Hole Feature(s)?

                            //Deliver any data which was intercepted to the underlying Transport.
                            received -= m_RtpClient.ProcessFrameData(m_Buffer.Array, offset, received, m_RtspSocket);

                            //Should check that received >= 0?
                            //Then
                            //connect the underlying Transport (RtpClient) now 
                            m_RtpClient.Connect();

                            //Handle when the client received a lot of data and no response was found when interleaving.
                            //One possibility is transport packets such as Rtp or Rtcp.
                            if (received < 0) received = 0;
                        }
                        else
                        {
                            //Otherwise just process the data via the event.
                            ProcessInterleaveData(this, m_Buffer.Array, offset, received);
                        }
                    } //Nothing was received, if the socket is not shared
                    else if (false == SharesSocket)
                    {
                        //Check for fatal exceptions
                        if (error != SocketError.ConnectionAborted && error != SocketError.ConnectionReset)
                        {
                            if (++attempt <= m_ResponseTimeoutInterval) goto Receive;
                        }

                        //Connection was aborted.
                        if (false == wasBlocked) m_InterleaveEvent.Set();

                        return null;
                    }

                Wait: //Wait for the response unless the method requested is unknown
                    if (received < RtspMessage.MaximumLength && message.Method != RtspMethod.UNKNOWN)// && request.Method != RtspMethod.TEARDOWN)
                    {
                        //Wait while
                        while (false == IsDisposed //The client connected and is not disposed AND
                            //There is no last transmitted message assigned AND it has not already been disposed
                            && (m_LastTransmitted == null || false == m_LastTransmitted.IsDisposed)
                            //AND the client is still allowed to wait
                            && ++attempt <= m_ResponseTimeoutInterval)
                        {
                            //Wait a small amount of time for the response because the cancellation token was not used...
                            if (IsDisposed || m_InterleaveEvent.IsSet || m_InterleaveEvent.Wait((int)((m_RtspSessionTimeout.TotalMilliseconds + 1) / m_ResponseTimeoutInterval)))
                            {
                                //There may be a response
                                continue;
                            }
                            else
                            {
                                //Wait a little more
                                System.Threading.Thread.Sleep(0);
                            }

                            //Check for any new messages
                            if (m_LastTransmitted != null) goto GotResponse;

                            //Calculate how much time has elapsed
                            TimeSpan taken = DateTime.UtcNow - (message.Transferred ?? message.Created);

                            //If more time has elapsed than allowed by reading
                            if (taken > m_LastMessageRoundTripTime && taken.TotalMilliseconds >= SocketReadTimeout)
                            {

                                int halfTimeout = (int)(m_RtspSessionTimeout.TotalMilliseconds / 2);

                                //Check if we can back off further
                                if (taken.TotalMilliseconds > halfTimeout) break;
                                else if (SocketReadTimeout < halfTimeout)
                                {
                                    //Backoff
                                    SocketWriteTimeout = SocketReadTimeout *= 2;

                                    //Ensure the client transport is connected if previously playing and it has since disconnected.
                                    if (IsPlaying && false == m_RtpClient.IsConnected) m_RtpClient.Connect();
                                }

                                //If the client was not disposed re-trasmit the request if there is not a response pending already.
                                //Todo allow an option for this feature? (AllowRetransmit)
                                if (false == IsDisposed && m_LastTransmitted == null /*&& request.Method != RtspMethod.PLAY*/)
                                {
                                    //handle re-transmission
                                    if (m_RtspProtocol == ClientProtocolType.Unreliable)
                                    {

                                        //Make the client send the same exact request again.

                                        //Should change Timestamp?

                                        //If so must re serialize message or modify buffer data.

                                        //All messages must end with
                                        //\r\n\r\n

                                        ////modify the last char in the Timestamp header by increment and update the buffer.
                                        //Location is obtained by using the length of the body (which may be 0) and the difference of the entire message to get to the last char of the Timestamp header.
                                        //Utility.TryModifyString(timestamp, timestamp.Length - 1, (char)(buffer[(length - request.Body.Length) - 5] = (byte)(timestamp[timestamp.Length - 1] + 1)));

                                        ////Reset the header value
                                        //request.SetHeader(RtspHeaders.Timestamp, timestamp);

                                        ++retransmits;

                                        ++m_ReTransmits;

                                        goto Send;
                                    }                                    
                                }
                            }

                            //If not playing trying to receive again.
                            if (false == SharesSocket) goto Receive;
                        }
                    }

                GotResponse:
                    //Update counters for any data received.
                    m_ReceivedBytes += received;

                    //If nothing was received wait for cache to clear.
                    if (null == m_LastTransmitted)
                    {
                        System.Threading.Thread.Sleep(0);
                    }
                    else //m_LastTransmitted is not null
                    {
                        //Obtain the CSeq of the response if present.
                        int sequenceNumberSent = message.CSeq, sequenceNumberReceived = m_LastTransmitted.CSeq;

                        //If the sequence number was present and did not match then wait again
                        if (sequenceNumberReceived >= 0 && sequenceNumberReceived != sequenceNumberSent)
                        {
                            //Check if someone else is transmitting and wait
                            if (m_InterleaveEvent.IsSet) m_InterleaveEvent.Wait();
                            
                            //Check for a new response to have bet set by the event
                            if (sequenceNumberReceived == m_LastTransmitted.CSeq &&
                                sequenceNumberReceived != sequenceNumberSent)
                            {
                                //Reset the block
                                m_InterleaveEvent.Reset();

                                //Mark disposed
                                m_LastTransmitted.Dispose();

                                //Remove the message to avoid confusion
                                m_LastTransmitted = null;

                                //Allow more waiting
                                attempt = 0;

                                goto Wait;
                            }
                        }
                    } // end check m_LastTransmitted == null
                }//Unchecked

                #region Notes

                //m_LastTransmitted is either null or not
                //if it is not null it may not be the same response we are looking for. (mostly during threaded sends and receives)
                //this could be dealt with by using a hash `m_Transactions` which holds requests which are sent and a space for their response if desired.
                //Then a function GetMessage(message) would be able to use that hash to get the outgoing or incoming message which resulted.
                //The structure of the hash would allow any response to be stored.

                #endregion

                //If we were not authorized and we did not give a nonce and there was an WWWAuthenticate header given then we will attempt to authenticate using the information in the header
                //(Note for Vivontek you can still bypass the Auth anyway :)
                //http://www.coresecurity.com/advisories/vivotek-ip-cameras-rtsp-authentication-bypass
                if (false == m_TriedCredentials && 
                    m_LastTransmitted != null && 
                    m_LastTransmitted.MessageType == RtspMessageType.Response && 
                    m_LastTransmitted.StatusCode == RtspStatusCode.Unauthorized && 
                    m_LastTransmitted.ContainsHeader(RtspHeaders.WWWAuthenticate) && 
                    Credential != null)
                {
                    //Return the result of Authenticating with the given request.
                    return Authenticate(message, m_LastTransmitted, true);
                }

                //Check for the response.
                if (m_LastTransmitted != null && m_LastTransmitted.MessageType == RtspMessageType.Response)
                {

                    //TODO
                    //REDIRECT (Handle loops)
                    //if(m_LastTransmitted.StatusCode == RtspStatusCode.MovedPermanently)

                    switch (m_LastTransmitted.StatusCode)
                    {
                        case RtspStatusCode.OK:
                            {
                                //Ensure message is added to supported methods.
                                m_SupportedMethods.Add(message.MethodString);

                                break;
                            }
                        case RtspStatusCode.NotImplemented: m_SupportedMethods.Remove(m_LastTransmitted.MethodString); break;
                        case RtspStatusCode.MethodNotValidInThisState: if (m_LastTransmitted.ContainsHeader(RtspHeaders.Allow)) SwitchProtocols(); break;
                        case RtspStatusCode.RtspVersionNotSupported:
                            {
                                //if enforcing the version
                                if (useClientProtocolVersion)
                                {
                                    //Read the version from the response
                                    ProtocolVersion = m_LastTransmitted.Version;

                                    //Send the request again.
                                    return SendRtspMessage(message, useClientProtocolVersion);
                                }

                                //break
                                break;
                            }
                        default: break;
                    }

                    //If the client should echo X headers
                    if (EchoXHeaders)
                    {
                        //iterate for any X headers 

                        foreach (var xHeader in m_LastTransmitted.GetHeaders().Where(h => h.Length > 2 && h[1] == Common.ASCII.HyphenSign && char.ToLower(h[0]) == 'x'))
                        {
                            //If contained already then update
                            if (AdditionalHeaders.ContainsKey(xHeader))
                            {
                                AdditionalHeaders[xHeader] += ((char)Common.ASCII.SemiColon).ToString() + m_LastTransmitted.GetHeader(xHeader).Trim();
                            }
                            else
                            {
                                //Add
                                AdditionalHeaders.Add(xHeader, m_LastTransmitted.GetHeader(xHeader).Trim());
                            }
                        }
                    }

                    //For any other request besides teardown update the sessionId and timeout
                    if (message.Method != RtspMethod.TEARDOWN)
                    {
                        //Check for a SessionId in the response.
                        if (m_LastTransmitted.ContainsHeader(RtspHeaders.Session))
                        {
                            //Get the header.
                            string sessionHeader = m_LastTransmitted[RtspHeaders.Session];

                            //If there is a session header it may contain the option timeout
                            if (false == string.IsNullOrWhiteSpace(sessionHeader))
                            {
                                //Check for session and timeout

                                //Get the values
                                string[] sessionHeaderParts = sessionHeader.Split(RtspHeaders.SemiColon);

                                int headerPartsLength = sessionHeaderParts.Length;

                                //Check if a valid value was given
                                if (headerPartsLength > 0)
                                {
                                    //Trim it of whitespace
                                    string value = sessionHeaderParts.FirstOrDefault(p=> false == string.IsNullOrWhiteSpace(p));

                                    //If we dont have an exiting id then this is valid if the header was completely recieved only.
                                    if (false == string.IsNullOrWhiteSpace(value) &&
                                        true == string.IsNullOrWhiteSpace(m_SessionId) || 
                                        value[0] != m_SessionId[0])
                                    {
                                        //Get the SessionId if present
                                        m_SessionId = sessionHeaderParts[0].Trim();

                                        //Check for a timeout
                                        if (sessionHeaderParts.Length > 1)
                                        {
                                            int timeoutStart = 1 + sessionHeaderParts[1].IndexOf(Media.Sdp.SessionDescription.EqualsSign);
                                            if (timeoutStart >= 0 && int.TryParse(sessionHeaderParts[1].Substring(timeoutStart), out timeoutStart))
                                            {
                                                //Should already be set...
                                                if (timeoutStart <= 0)
                                                {
                                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(60);//Default
                                                }
                                                else
                                                {
                                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(timeoutStart);
                                                }
                                            }
                                        }
                                    }

                                    //done
                                }
                                else if(string.IsNullOrWhiteSpace(m_SessionId))
                                {
                                    //The timeout was not present
                                    m_SessionId = sessionHeader.Trim();

                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(60);//Default
                                }
                            }
                        }
                    }

                    //Determine if delay was honored.
                    string timestampHeader = m_LastTransmitted.GetHeader(RtspHeaders.Timestamp);

                    //If there was a Timestamp header
                    if (false == string.IsNullOrWhiteSpace(timestampHeader))
                    {
                        timestampHeader = timestampHeader.Trim();

                        //check for the delay token
                        int indexOfDelay = timestampHeader.IndexOf("delay=");

                        //if present
                        if (indexOfDelay >= 0)
                        {
                            //attempt to calculate it from the given value
                            double delay = double.NaN;

                            if (double.TryParse(timestampHeader.Substring(indexOfDelay + 6).TrimEnd(), out delay))
                            {
                                //Set the value of the servers delay
                                m_LastServerDelay = TimeSpan.FromSeconds(delay);

                                //Could add it to the existing SocketReadTimeout and SocketWriteTimeout.
                            }
                        }
                        else
                        {
                            //MS servers don't use a ; to indicate delay
                            string[] parts = timestampHeader.Split(RtspMessage.SpaceSplit, 2);

                            //If there was something after the space
                            if (parts.Length > 1)
                            {
                                //attempt to calulcate it from the given value
                                double delay = double.NaN;

                                if (double.TryParse(parts[1].Trim(), out delay))
                                {
                                    //Set the value of the servers delay
                                    m_LastServerDelay = TimeSpan.FromSeconds(delay);
                                }
                            }

                        }
                    }

                    //Calculate the amount of time taken to receive the message.
                    TimeSpan lastMessageRoundTripTime = (m_LastTransmitted.Created - (message.Transferred ?? message.Created));

                    //Ensure positive values for the RTT
                    if (lastMessageRoundTripTime < TimeSpan.Zero) lastMessageRoundTripTime = lastMessageRoundTripTime.Negate();

                    //Assign it
                    m_LastMessageRoundTripTime = lastMessageRoundTripTime;

                    //Raise an event
                    Received(message, m_LastTransmitted);
                }
                //
                //Unblock (should not be needed)
                //else if (false == wasBlocked) m_InterleaveEvent.Set();

                //Return the result
                return m_LastTransmitted;
            }
            catch (Exception ex)
            {
                if (IsDisposed || ex is ObjectDisposedException) return null;

                if (ex is Common.TaggedException<RtpClient>) return null;

                throw;
            }
        }        

        /// <summary>
        /// Sends the Rtsp OPTIONS request
        /// </summary>
        /// <param name="useStar">The OPTIONS * request will be sent rather then one with the <see cref="RtspClient.CurrentLocation"/></param>
        /// <returns>The <see cref="RtspMessage"/> as a response to the request</returns>
        public RtspMessage SendOptions(bool useStar = false)
        {
            using(var options = new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.OPTIONS,
                Location = useStar ? null : CurrentLocation
            })
            {
                RtspMessage response = SendRtspMessage(options);

                if (response == null) Common.ExceptionExtensions.RaiseTaggedException(this, "Unable to get options, See InnerException.", new Common.TaggedException<RtspMessage>(response, "See Tag for Response."));
                else
                {
                    //Get the Public header which indicates the methods supported by the client
                    string publicMethods = response[RtspHeaders.Public];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(publicMethods))
                    {
                        //Process values in the Public header.
                        foreach (string method in publicMethods.Split(RtspHeaders.Comma))
                        {
                            m_SupportedMethods.Add(method.Trim());
                        }
                    }

                    string allowedMethods = response[RtspHeaders.Allow];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(allowedMethods))
                    {
                        //Process values in the Public header.
                        foreach (string method in allowedMethods.Split(RtspHeaders.Comma))
                        {
                            m_SupportedMethods.Add(method.Trim());
                        }
                    }

                    //Some servers only indicate different features at the SETUP level...

                    string supportedFeatures = response[RtspHeaders.Supported];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(supportedFeatures))
                    {
                        //Process values in the Public header.
                        foreach (string method in supportedFeatures.Split(RtspHeaders.Comma))
                        {
                             SupportedFeatures.Add(method.Trim());
                        }
                    }
                }

                return response;
            }
        }

        /// <summary>
        /// Assigns the SessionDescription returned from the server
        /// </summary>
        /// <returns></returns>
        public RtspMessage SendDescribe()
        {

            RtspMessage response = null;

            try
            {
                using (RtspMessage describe = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.DESCRIBE,
                    Location = CurrentLocation
                })
                {
                    #region Reference

                    // The DESCRIBE method retrieves the description of a presentation or
                    // media object identified by the request URL from a server. It may use
                    // the Accept header to specify the description formats that the client
                    // understands. The server responds with a description of the requested
                    // resource. The DESCRIBE reply-response pair constitutes the media
                    // initialization phase of RTSP.

                    #endregion

                    describe.SetHeader(RtspHeaders.Accept, Sdp.SessionDescription.MimeType);

                Describe:
                    response = SendRtspMessage(describe);                    

                    //Handle no response
                    if (response == null) Common.ExceptionExtensions.RaiseTaggedException(describe, "Unable to describe media, no response to DESCRIBE request. The request is in the Tag property.");

                    //Hanlde NotFound
                    if (response.StatusCode == RtspStatusCode.NotFound) Common.ExceptionExtensions.RaiseTaggedException(describe, "Unable to describe media, NotFound. The response is in the Tag property.");

                    //Wait for complete responses
                    if (false == response.IsComplete)
                    {
                        m_InterleaveEvent.Wait();
                    }

                    //Only handle responses for the describe request sent when sharing the socket
                    if (SharesSocket && response.CSeq != describe.CSeq)
                    {
                        describe.RemoveHeader(RtspHeaders.Timestamp);

                        goto Describe;
                    }

                    //Handle Found / Redirect
                    if (response.StatusCode == RtspStatusCode.Found || response.Method == RtspMethod.REDIRECT)
                    {
                        //Determine if there is a new location
                        string newLocation = response.GetHeader(RtspHeaders.Location).Trim();

                        //We start at our location
                        Uri baseUri = CurrentLocation;

                        //Get the contentBase header
                        string contentBase = response[RtspHeaders.ContentBase];

                        //If it was present
                        if (false == string.IsNullOrWhiteSpace(contentBase) && 
                            //Try to create it from the string
                            Uri.TryCreate(contentBase, UriKind.RelativeOrAbsolute, out baseUri))
                        {
                            //If it was not absolute
                            if (false == baseUri.IsAbsoluteUri)
                            {
                                //Try to make it absolute and if not try to raise an exception
                                if (false == Uri.TryCreate(CurrentLocation, baseUri, out baseUri))
                                {
                                    Common.ExceptionExtensions.TryRaiseTaggedException(contentBase, "See Tag. Can't parse ContentBase header.");
                                }
                                
                            }
                        }

                        Uri parsedLocation;

                        //UriDecode?

                        //Try to parse it if not null or empty
                        if (false == string.IsNullOrWhiteSpace(newLocation) &&
                            Uri.TryCreate(baseUri, newLocation, out parsedLocation) &&
                            parsedLocation != CurrentLocation) // and not equal the existing location
                        {
                            //Could only take the different part of the location with the following code
                            //parsedLocation.MakeRelativeUri(Location)
                            
                            //Redirect to the Location by setting Location. (Allows a new host)
                            CurrentLocation = parsedLocation;

                            //Send a new describe
                            return SendDescribe();
                        }
                    }

                    string contentType = response[RtspHeaders.ContentType];

                    //Handle any not ok response (allow Continue)
                    if (response.StatusCode >= RtspStatusCode.MultipleChoices && false == string.IsNullOrEmpty(contentType) && string.Compare(contentType.TrimStart(), Sdp.SessionDescription.MimeType, true) != 0)
                    {
                        Common.ExceptionExtensions.RaiseTaggedException(response.StatusCode, "Unable to describe media. The StatusCode is in the Tag property.");
                    }
                    else if (string.IsNullOrWhiteSpace(response.Body))
                    {
                        Common.ExceptionExtensions.RaiseTaggedException(this, "Unable to describe media, Missing Session Description");
                    }

                    #region MS-RTSP

                    //////Not really needed

                    ////string playListId = response["X-Playlist-Gen-Id"];

                    ////if (false == string.IsNullOrWhiteSpace(playListId))
                    ////{
                    ////    AdditionalHeaders.Add("X-Playlist-Gen-Id", playListId.Trim());
                    ////}

                    //// Should also do a SET_PARAMETER
                    //Content-type: application/x-rtsp-udp-packetpair;charset=UTF-8\r\n\r\n
                    //Content-Length: X \r\n
                    //type: high-entropy-packetpair variable-size

                    #endregion

                    //Try to create a session description even if there was no contentType so long as one was not specified against sdp.
                    m_SessionDescription = new Sdp.SessionDescription(response.Body);
                }
            }
            catch (Common.TaggedException<RtspClient>)
            {
                throw;
            }
            catch (Common.TaggedException<SessionDescription> sde)
            {
                Common.ExceptionExtensions.RaiseTaggedException(this, "Unable to describe media, Session Description Exception Occured.", sde);
            }
            catch (Exception ex) { if (ex is Media.Common.ITaggedException) throw ex; Common.ExceptionExtensions.RaiseTaggedException(this, "An error occured", ex); }

            //Return the response
            return response;
        }

        /// <summary>
        /// Sends a request which will remove the session given from the server using a TEARDOWN * request.
        /// </summary>
        /// <param name="sessionId">The sessionId to remove, if null the current <see cref="SessionId"/> will be used if possible.</param>
        /// <param name="closeConnection">Indicates if the `Connection` header of the request should be set to 'Close'</param>
        /// <returns></returns>
        public virtual RtspMessage RemoveSession(string sessionId, bool closeConnection = false)
        {
            using (var teardown = new RtspMessage(RtspMessageType.Request))
            {
                teardown.Method = RtspMethod.TEARDOWN;

                if(closeConnection) teardown.SetHeader(RtspHeaders.Connection, "Close");

                sessionId = sessionId ?? m_SessionId;

                if (false == string.IsNullOrWhiteSpace(sessionId)) teardown.SetHeader(RtspHeaders.Session, sessionId);

                OnStopping();

                try { return SendRtspMessage(teardown); }
                finally { m_SessionId = null; }
            }
        }

        public RtspMessage SendTeardown(MediaDescription mediaDescription = null, bool disconnect = false, bool force = false)
        {
            RtspMessage response = null;

            //Check if the session supports pausing a specific media item
            if (mediaDescription != null && false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

            //only send a teardown if not forced and the client is playing
            if (false == force && false == IsPlaying) return response;

            try
            {
                //If there is a client then stop the flow of this media now with RTP
                if (m_RtpClient != null)
                {
                    //Send a goodbye for all contexts if the mediaDescription was not given
                    if (mediaDescription == null) m_RtpClient.SendGoodbyes();
                    else//Find the context for the description
                    {
                        //Get a context
                        RtpClient.TransportContext context = m_RtpClient.GetContextForMediaDescription(mediaDescription);

                        //If context was determined then send a goodbye
                        if (context != null)
                        {
                            //Send a goodbye now (but still allow reception)
                            m_RtpClient.SendGoodbye(context);

                            //Remove the reference
                            context = null;
                        }
                    }
                }

                //Keep track of whats playing
                if (mediaDescription == null)
                {
                    m_Playing.Clear();
                    
                    //LeaveOpen = false;
                }
                else m_Playing.Remove(mediaDescription);

                //The media is stopping now.
                OnStopping(mediaDescription);
               
                //Return the result of the Teardown
                using (var teardown = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.TEARDOWN,
                    Location = mediaDescription != null ? mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) : CurrentLocation
                })
                {
                    //Set the close header if disconnecting
                    if (disconnect) teardown.SetHeader(RtspHeaders.Connection, "close");

                    //Send the request and if not closing the connecting then wait for a response
                    return SendRtspMessage(teardown, true, false == disconnect);
                }
                
            }
            catch (Common.TaggedException<RtspClient>)
            {
                return response;
            }
            catch
            {
                throw;
            }
            finally
            {
                //Ensure the sessionId is invalided when no longer playing if not forced
                if (false == force && false == IsPlaying) m_SessionId = null;
            }
        }

        public RtspMessage SendSetup(MediaDescription mediaDescription)
        {
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Send the setup
            return SendSetup(mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription), mediaDescription);
        }

        //Remove unicast...
        internal RtspMessage SendSetup(Uri location, MediaDescription mediaDescription, bool unicast = true)//False to use manually set protocol
        {
            if (location == null) throw new ArgumentNullException("location");

            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Todo Setup should only create a TransportContext which COULD then be given to a RtpClient 
            //This will allow for non RTP transports to be used such as MPEG-TS.
            //Must de-coulple the RtpClient and replace it

            //Determine if a client_port datum should be sent in the Transport header as a set, list or a single entry
            bool needsRtcp = true, multiplexing = false;

            #region [WMS Notes / Log]

            //Some sources indicate that rtx must be or must not be setup
            //They also say that only one ssrc should be sent
            //There are various different advices.

            //Check the spec if you have doubts https://msdn.microsoft.com/en-us/library/cc245238.aspx

            /* WMS Server
             1. 
            client -->
            SETUP rtsp://s-media1/spider_od/rtx RTSP/1.0
            Transport:
            RTP/AVP/UDP;unicast;client_port=1206-1207;ssrc=9cef6565;mode=PLAY

            server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004-5005;client_port=1206-1207;ssrc=e34
            90f0d;mode=PLAY

            2. 
            client -->
            SETUP rtsp://s-media1/spider_od/audio RTSP/1.0
            Transport: RTP/AVP/UDP;unicast;client_port=1208;ssrc=9a789797;mode=PLAY 

            Server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004;client_port=1208;ssrc=8873c0ac;mode
            =PLAY

            3. 
            client -->
            SETUP rtsp://s-media1/spider_od/video RTSP/1.0
            Transport: RTP/AVP/UDP;unicast;client_port=1208;ssrc=275f7979;mode=PLAY

            Server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004;client_port=1208;ssrc=8873c0cf;mode
            =PLAY
             */                        

            ////Keep the values parsed from the description
            //int rr, rs, a;

            ////Attempt to parse them
            //if (RtpClient.TransportContext.TryParseBandwidthDirectives(mediaDescription, out rr, out rs, out a) &&
            //    rr == 0 && //If the rr AND
            //    rs == 0/* && a == 0*/) // rs directive specified 0 (Should check AS?)
            //{
            //    //RTSP is not needed
            //    needsRtcp = false;
            //}

            ////Rtx streams for a WMS Server always require RTCP?
            ////Should ensure this convention doesn't interfere with names where are not for WMS
            ////Possible check server header in m_LastTransmitted
            //if (location.AbsoluteUri.EndsWith("rtx", StringComparison.OrdinalIgnoreCase)) needsRtcp = true;

            #endregion            

            try
            {
                //Should either create context NOW or use these sockets in the created context.

                //Create sockets to reserve the ports we think we will need.
                Socket rtpTemp = null, rtcpTemp = null;

                using (RtspMessage setup = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.SETUP,
                    Location =  location ?? CurrentLocation
                })
                {


                    if (m_InitialLocation != null)
                    {
                        if (Uri.TryCreate(m_InitialLocation, location, out location))
                        {
                            setup.Location = location;
                        }
                    }

                    //Values in the header we need
                    int clientRtpPort = -1, clientRtcpPort = -1,
                        serverRtpPort = -1, serverRtcpPort = -1,
                        //Darwin and Wowza uses this ssrc, VLC Gives a Unsupported Transport, WMS and most others seem to ignore it.
                        localSsrc = 0,//RFC3550.Random32(),  
                        remoteSsrc = 0;

                    //Cache this to prevent having to go to get it every time down the line
                    IPAddress sourceIp = IPAddress.Any;

                    string mode;

                    bool multicast = false, interleaved = m_RtpProtocol == ProtocolType.Tcp;

                    byte dataChannel = 0, controlChannel = 1;

                    int minimumPacketSize = 8, maximumPacketSize = (ushort)m_Buffer.Count;

                    //Todo Determine if Unicast or Multicast from mediaDescription ....?
                    string connectionType = unicast ? "unicast;" : "multicast";

                    //12.7 Blocksize
                    /*
                     This request header field is sent from the client to the media server
                        asking the server for a particular media packet size. This packet
                        size does not include lower-layer headers such as IP, UDP, or RTP.
                        The server is free to use a blocksize which is lower than the one
                        requested. The server MAY truncate this packet size to the closest
                        multiple of the minimum, media-specific block size, or override it
                        with the media-specific size if necessary. The block size MUST be a
                        positive decimal number, measured in octets. The server only returns
                        an error (416) if the value is syntactically invalid.
                     */

                    //This is important if the server can support it, it will ensure that packets can fit in the buffer.
                    //It also tells the server what our buffer size is so if they wanted they could intentionally make packets which allowed only a certain amount of bytes remaining in the buffer....
                    setup.SetHeader(RtspHeaders.Blocksize, m_Buffer.Count.ToString());

                    //TODO
                    // IF TCP was specified or the MediaDescription specified we need to use Tcp as specified in RFC4571
                    // DETERMINE if only 1 channel should be sent in the TransportHeader if we know RTCP is not going to be used. (doing so would force RTCP to stay disabled the entire stream unless a SETUP for just the RTCP occured, how does one setup just a RTCP session..)

                    //Send any supported headers?

                    //if (false == setup.ContainsHeader(RtspHeaders.Supported))
                    //{
                        //setup.SetHeader(RtspHeaders.Supported, "play.basic, play.scale, play.speed, con.persistent, con.independent, con.transient, rtp.mux, rtcp.mux, ts.mux, raw.mux, mux.*");
                        //setup.SetHeader(RtspHeaders.Supported, string.Join(Sdp.SessionDescription.SpaceString, SupportedFeatures));
                    //}

                    //Required: header (RequiredFeatures)
                    if (false == setup.ContainsHeader(RtspHeaders.Require))
                    {
                        setup.SetHeader(RtspHeaders.Require, string.Join(Sdp.SessionDescription.SpaceString, RequiredFeatures));
                    }

                    //Interleaved
                    if (interleaved)
                    {
                        //RTCP-mux:

                        //If there is already a RtpClient with at-least 1 TransportContext
                        if (m_RtpClient != null && m_RtpClient.GetTransportContexts().Any())
                        {
                            RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().Last();
                            if(lastContext != null) setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc != 0 ? localSsrc : (int?)null, null, null, null, null, null, true, false, null, true, dataChannel = (byte)(lastContext.DataChannel + 2), (needsRtcp ? (byte?)(controlChannel = (byte)(lastContext.ControlChannel + 2)) : null), RtspMethod.PLAY.ToString()));
                            else setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc != 0 ? localSsrc : (int?)null, null, null, null, null, null, true, false, null, true, dataChannel, (needsRtcp ? (byte?)controlChannel : null), RtspMethod.PLAY.ToString()));
                        }

                    }
                    else if (string.Compare(mediaDescription.MediaProtocol, RtpClient.RtpAvpProfileIdentifier, true) == 0) // We need to find an open Udp Port
                    {
                        //Revise
                        //Is probably Ip, set to Udp
                        m_RtpProtocol = ProtocolType.Udp;

                        //Could send 0 to have server pick port?                        

                        //Should allow this to be given or set as a property MinimumUdpPort, MaximumUdpPort                        
                        int openPort = Utility.FindOpenPort(ProtocolType.Udp, 10000, true); 

                        rtpTemp = Utility.ReservePort(SocketType.Dgram, ProtocolType.Udp, ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, clientRtpPort = openPort);

                        //Check for muxing of rtp and rtcp on the same physical port
                        if (mediaDescription.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts.Any(p => p.ToLowerInvariant() == "rtcp-mux")).Any())
                        {
                            //Might not 'need' it
                            needsRtcp = multiplexing = true;

                            //Use the same port
                            clientRtcpPort = clientRtpPort;
                        }
                        else if (needsRtcp) rtcpTemp = Utility.ReservePort(SocketType.Dgram, ProtocolType.Udp, ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, (clientRtcpPort = openPort + 1));
                        

                        if (openPort == -1) Common.ExceptionExtensions.RaiseTaggedException(this, "Could not find open Udp Port");
                        //else if (MaximumUdp.HasValue && openPort > MaximumUdp)
                        //{
                        //    Common.ExceptionExtensions.CreateAndRaiseException(this, "Found Udp Port > MaximumUdp. Found: " + openPort);
                        //}    
                        //Supposedly
                        //WMS Server will complain if there is a RTCP port and no RTCP is allowed.


                        //Should allow a Rtcp only setup? would be a different profile...
                        setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/UDP", localSsrc != 0 ? localSsrc : (int?)null, null, clientRtpPort, (needsRtcp ? (int?)(clientRtcpPort) : null), null, null, true, false, null, false, 0, 0, RtspMethod.PLAY.ToString()));
                    }
                    else throw new NotSupportedException("The required Transport is not yet supported.");

                    SocketError error;

                    bool triedTwoTimes = false;

                Setup:
                    //Get the response for the setup
                    RtspMessage response = SendRtspMessage(setup, out error);

                    //if there was no response then don't attempt to parse any but DO attempt to listen.
                    if (false == triedTwoTimes &&
                        error != SocketError.Success ||
                            response == null ||
                            response.MessageType != RtspMessageType.Response)
                    {
                        if (IsPlaying) Common.ExceptionExtensions.RaiseTaggedException(this, "No response to SETUP." + (false == SupportedMethods.Contains(RtspMethod.SETUP.ToString()) ? " The server may not support SETUP." : string.Empty));
                        else
                        {
                            //Handle host dropping the connection
                            if (error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset) Reconnect();

                            //make another request if we didn't already try.
                            if (false == triedTwoTimes)
                            {
                                //Use a new Sequence number
                                setup.RemoveHeader(RtspHeaders.CSeq);

                                //Use a new Timestamp
                                setup.RemoveHeader(RtspHeaders.Timestamp);

                                //Dont try again
                                triedTwoTimes = true;

                                goto Setup;
                            }
                        }
                    }
                    
                    //Ensure there was a response
                    if (response == null) goto NoResponse;

                    //Response not OK
                    if (response.StatusCode != RtspStatusCode.OK)
                    {
                        //Transport requested not valid
                        if (response.StatusCode == RtspStatusCode.UnsupportedTransport && m_RtpProtocol != ProtocolType.Tcp)
                        {
                            goto SetupTcp;
                        }
                        else if (response.StatusCode == RtspStatusCode.SessionNotFound && false == string.IsNullOrWhiteSpace(m_SessionId) && 
                            false == triedTwoTimes)
                        {
                            //Erase the old session id
                            m_SessionId = string.Empty;

                            //Attempt the setup again
                            return SendSetup(location, mediaDescription);
                        }
                        else
                        {
                            //If there was an initial location and that location's host is different that the current location's host
                            if (m_InitialLocation != null && location.Host != m_InitialLocation.Host)
                            {
                                //You would have thought that the resource we were directed to would be able to handle it's own DNS routing even when it's not tunneled through IPv4

                                //Try to use the old location
                                location = mediaDescription.GetAbsoluteControlUri(m_InitialLocation, SessionDescription);

                                triedTwoTimes = true;

                                goto Setup;
                            }

                            Common.ExceptionExtensions.RaiseTaggedException(response.StatusCode, "Unable to setup media. The status code is in the Tag property.");

                            return response;
                        }
                    }

                    //Handle the servers response for Blocksize                    

                    string blockSize = response[RtspHeaders.Blocksize];

                    if (false == string.IsNullOrWhiteSpace(blockSize))
                    {
                        //Extract the value (Should account for ';' in some way)
                        blockSize = Utility.ExtractNumber(blockSize.Trim());

                        try
                        {
                            //Parse it...
                            maximumPacketSize = int.Parse(blockSize, System.Globalization.NumberStyles.Integer);

                            //If the packets cannot fit in the buffer
                            if (maximumPacketSize > m_Buffer.Count)
                            {
                                //Try to allow processing
                                Common.ExceptionExtensions.RaiseTaggedException(maximumPacketSize, "Media Requires a Larger Buffer. (See Tag for value)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.ExceptionExtensions.TryRaiseTaggedException(this, "BlockSize of the response needs consideration.", ex);
                        }
                    }

                    //Handle Rtcp-Interval (eventually, or definitely when it becomes a standard header :P)

                    //Handle anything else

                NoResponse:

                    //We SHOULD have a valid TransportHeader in the response
                    //Get the transport header from the response if present.
                    string transportHeader = response != null ? response[RtspHeaders.Transport] : null;

                    //If there was no return transport header then we don't know what ports to utilize for reception.
                    if (string.IsNullOrWhiteSpace(transportHeader))
                    {
                        //Discover them when receiving from the host
                        serverRtpPort = 0;

                        serverRtcpPort = 0;
                    }
                    else
                    {
                        //Check for the RTP token to ensure the underlying tranport is supported.
                        //Eventually any type such as RAW etc will be supported.
                        if (false == transportHeader.Contains("RTP")
                        ||
                        false == RtspHeaders.TryParseTransportHeader(transportHeader,
                        out remoteSsrc, out sourceIp, out serverRtpPort, out serverRtcpPort, out clientRtpPort, out clientRtcpPort,
                        out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast))
                            Common.ExceptionExtensions.RaiseTaggedException(this, "Cannot setup media, Invalid Transport Header in Rtsp Response: " + transportHeader);
                    }

                    //If the server returns a channel which is already in use
                    //it then determines if there is an existing channel already utilized by this client with a different socket.
                    //If there is, then nothing neeed to be created just updated.
                    //Todo
                    //Care should be taken that the SDP is not directing us to connect to some unknown resource....

                    //Ensure we are not overlapping context's 
                    if (remoteSsrc != 0)
                    {
                        if (m_RtpClient != null && m_RtpClient.GetContextBySourceId(remoteSsrc) != null)
                        {
                            Common.ExceptionExtensions.RaiseTaggedException(this, "Cannot setup media, Identity Collision in `ssrc` datum: " + transportHeader);
                        }
                        else if (remoteSsrc == localSsrc) //If there was no indication back then discover the remote party
                        {
                            remoteSsrc = 0;
                        }
                    }
                   
                    //Just incase the source datum was not given
                    if (sourceIp == IPAddress.Any) sourceIp = ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address;

                    //Create the context (determine if the session rangeLine may also be given here, if it gets parsed once it doesn't need to be parsed again)
                    RtpClient.TransportContext created = null;

                    //If interleaved was present in the response then use a RTP/AVP/TCP Transport
                    if (interleaved)
                    {
                        
                        //No client or disposed
                        if (m_RtpClient != null && false == m_RtpClient.IsDisposed)
                        {
                            //Obtain the context via the given data channel
                            created = m_RtpClient.GetContextByChannel(dataChannel);

                            //If the control channel is the same then just update the client and ensure connected.
                            if (created != null && created.ControlChannel == controlChannel)
                            {
                                created.Initialize(m_RtspSocket);

                                m_RtpClient.Connect();

                                return response;
                            }
                        }
                        
                        //If a context was not already created
                        if (created == null || created.IsDisposed)
                        {
                            //Create the context if required
                            created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, dataChannel, controlChannel, mediaDescription, true, remoteSsrc, remoteSsrc != 0 ? 0 : 2);

                            //Set the identity to what we indicated to the server.
                            created.SynchronizationSourceIdentifier = localSsrc;

                            //Set the minimum packet size
                            created.MinimumPacketSize = minimumPacketSize;

                            //Set the maximum packet size
                            created.MaximumPacketSize = maximumPacketSize;
                        }

                        //If there is not a client
                        if (m_RtpClient == null || m_RtpClient.IsDisposed)
                        {                            
                            //Create a Duplexed reciever using the RtspSocket sharing the RtspClient's buffer's properties
                            m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                            //Attach an event for interleaved data
                            m_RtpClient.InterleavedData += ProcessInterleaveData;
                        }
                        else if (m_RtpProtocol != ProtocolType.Tcp) goto SetupTcp;

                        //If the source address contains the NAT IpAddress or the source is the same then just use the source.
                        if (IPAddress.Equals(sourceIp, ((IPEndPoint)m_RemoteRtsp).Address) || 
                            Utility.IsOnIntranet(sourceIp))
                        {
                            //Create from the existing socket
                            created.Initialize(m_RtspSocket);

                            //Don't close this socket when disposing.
                            LeaveOpen = created.LeaveOpen = true;
                        }
                        else
                        {
                            //Create a new socket
                            created.Initialize(Utility.GetFirstIPAddress(sourceIp.AddressFamily), sourceIp, serverRtpPort); //Might have to come from source string?

                            //When the RtspClient is disposed that socket will also be disposed.
                        }

                    }
                    else
                    {
                        //The server may response with the port used for the request which indicates that TCP should be used?
                        if (serverRtpPort == location.Port) goto SetupTcp;

                        //If we need to make a client then do so
                        if (m_RtpClient == null || m_RtpClient.IsDisposed)
                        {
                            //Create a Udp Reciever sharing the RtspClient's buffer's properties
                            m_RtpClient = new RtpClient(m_Buffer);

                            //Attach an event for interleaved data
                            m_RtpClient.InterleavedData += ProcessInterleaveData;
                        }
                        else if(created == null || created.IsDisposed)
                        {
                            //Obtain the context via the given local or remote id
                            created = localSsrc != 0 ? m_RtpClient.GetContextBySourceId(localSsrc) : remoteSsrc != 0 ? m_RtpClient.GetContextBySourceId(remoteSsrc) : null;

                            //If the control channel is the same then just update the client and ensure connected.
                            if (created != null && created.ControlChannel == controlChannel)
                            {
                                created.Initialize(m_RtspSocket);

                                m_RtpClient.Connect();

                                return response;
                            }
                        }

                        //Get the available context's
                        var availableContexts = m_RtpClient.GetTransportContexts().Where(tc => tc != null && false == tc.IsDisposed);

                        //If there are aren't any then create one using the default values
                        if (false == availableContexts.Any())
                        {
                            created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, 0, (byte)(multiplexing ? 0 : 1), mediaDescription, true, remoteSsrc, remoteSsrc != 0 ? 0 : 2);
                        }
                        else
                        {
                            RtpClient.TransportContext lastContext = availableContexts.LastOrDefault();

                            if (lastContext != null) created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, (byte)(lastContext.DataChannel + (multiplexing ? 1 : 2)), (byte)(lastContext.ControlChannel + (multiplexing ? 1 : 2)), mediaDescription, true, remoteSsrc, remoteSsrc != 0 ? 0 : 2);
                            else created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, (byte)dataChannel, (byte)controlChannel, mediaDescription, true, remoteSsrc, remoteSsrc != 0 ? 0 : 2);
                        }

                        created.Initialize(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);

                        //No longer need the temporary sockets
                        
                        if (rtpTemp != null)
                        {
                            rtpTemp.Dispose();

                            rtpTemp = null;
                        }

                        if (false == multiplexing && rtcpTemp != null)
                        {
                            rtcpTemp.Dispose();

                            rtcpTemp = null;
                        }
                    }

                    //if a context was created add it
                    if(created != null) m_RtpClient.AddContext(created, false == multiplexing, false == multiplexing, true, true);

                    //Setup Complete
                    return response;
                }
            }
            catch (Exception ex)
            {
                Common.ExceptionExtensions.RaiseTaggedException(this, "Unable to setup media. See InnerException", ex);
            }

        //Setup for Interleaved connection
        SetupTcp:
            {
                m_RtpProtocol = ProtocolType.Tcp;

                //Recurse call to ensure propper setup
                return SendSetup(location, mediaDescription);
            }
        }

        protected virtual void SwitchProtocols(object state = null)
        {

            if (AllowAlternateTransport && //If protocol switch is still allowed AND
                false == IsDisposed && m_RtpProtocol != ProtocolType.Tcp &&  //If not already Disposed and the protocol was not already specified as or configured to TCP
                IsPlaying) //AND the RtspClient IsPlaying Media
            {

                //Filter any context which is not playing, disposed or has activity
                var contextsWithoutFlow = Client.GetTransportContexts().Where(tc => m_Playing.Contains(tc.MediaDescription) && false == tc.IsDisposed && false == tc.HasAnyActivity);

                //If there are any context's which are not flowing
                if (contextsWithoutFlow.Any())
                {
                    try
                    {
                        //If the client has not recieved any bytes and we have not already switched to Tcp
                        if (m_RtpProtocol != ProtocolType.Tcp)
                        {
                            //Ensure Tcp protocol
                            m_RtpProtocol = ProtocolType.Tcp;
                        }
                        else if (m_RtpProtocol != ProtocolType.Udp)
                        {
                            //Ensure Udp protocol
                            m_RtpProtocol = ProtocolType.Udp;
                        }
                        else
                        {
                            //Ensure IP protocol
                            m_RtpProtocol = ProtocolType.IP;
                        }

                        //Stop all playback
                        StopPlaying();

                        //Start again
                        StartPlaying();
                    }
                    catch { }
                }
            }
            
            //If there is still a timer dispose of it at this point as it will no longer be required
            if(m_ProtocolSwitchTimer != null)
            {
                m_ProtocolSwitchTimer.Dispose();
                m_ProtocolSwitchTimer = null;
            }

        }

        public RtspMessage SendPlay(MediaDescription mediaDescription, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt")
        {
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Check if the session supports pausing a specific media item
            if (false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

            var context = Client.GetContextForMediaDescription(mediaDescription);

            if (context == null) throw new InvalidOperationException("The given mediaDescription has not been SETUP.");

            //Check if the media was previsouly playing
            if (mediaDescription != null && false == m_Playing.Contains(mediaDescription))
            {
                //Keep track of whats playing
                m_Playing.Add(mediaDescription);
                
                //Raise an event now.
                OnPlaying(mediaDescription);
            }

            //Send the play request
            return SendPlay(mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription), startTime ?? context.MediaStartTime, endTime ?? context.MediaEndTime, rangeType);
        }

        public RtspMessage SendPlay(Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt", bool force = false)
        {
            //If not forced
            if (false == force)
            {
                //Usually at least setup must occur so we must have sent and received a setup to actually play
                force = m_ReceivedMessages > 0 && m_SupportedMethods.Contains(RtspMethod.SETUP.ToString());

                //If not forced and the soure does not support play then throw an exception
                if (false == force && 
                    m_SupportedMethods.Count > 0 &&  //There are some methods supported
                    false == m_SupportedMethods.Contains(RtspMethod.PLAY.ToString())) throw new InvalidOperationException("Server does not support PLAY.");
            }

            m_RtpClient.Connect();

            try
            {
                using(RtspMessage play = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.PLAY,
                    Location = location ?? CurrentLocation
                })
                {
                    /*
                      A PLAY request without a Range header is legal. It starts playing a
                        stream from the beginning unless the stream has been paused. If a
                        stream has been paused via PAUSE, stream delivery resumes at the
                        pause point. If a stream is playing, such a PLAY request causes no
                        further action and can be used by the client to test server liveness.
                     */

                    //Maybe should not be set if no start or end time is given.
                    if (startTime.HasValue || endTime.HasValue) play.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startTime, endTime, rangeType));
                    else if (false == string.IsNullOrWhiteSpace(rangeType)) //otherwise is a non null or whitespace string was given for rangeType
                    {
                        //Use the given rangeType string verbtaim.
                        play.SetHeader(RtspHeaders.Range, rangeType);
                    }

                    //Send the response
                    RtspMessage response = SendRtspMessage(play);

                    //response may be null because the server dropped the response due to an invalid header on the request.

                    //Handle allowed problems with reception of the play response if already playing
                    if (false == IsPlaying &&
                        response == null || response != null && response.MessageType == RtspMessageType.Response)
                    {
                        //No response or invalid range.
                        if (response == null || response.StatusCode == RtspStatusCode.InvalidRange)
                        {
                            play.RemoveHeader(Rtsp.RtspHeaders.Range);

                            play.RemoveHeader(Rtsp.RtspHeaders.CSeq);

                            play.RemoveHeader(RtspHeaders.Timestamp);

                            return SendRtspMessage(play);
                        }
                        else if (response.StatusCode == RtspStatusCode.OK)
                        {

                            //Set EndTime based on Range

                            //string rangeHeader = response[RtspHeaders.Range];

                            //Should really only get the RtpInfo header if its needed....

                            //Get the rtp-info header
                            string rtpInfo = response[RtspHeaders.RtpInfo];

                            string[] rtpInfos;

                            //Make a parser class which can be reused?

                            //If parsing of the header succeeded
                            if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
                            {
                                //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                                //Loop all found sub header values
                                foreach (string rtpInfoValue in rtpInfos)
                                {
                                    Uri uri;

                                    int? rtpTime;

                                    int? seq;

                                    int? ssrc;

                                    //If any value which was needed was found.
                                    if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc))
                                    {
                                        //Just use the ssrc to lookup the context.
                                        if (ssrc.HasValue)
                                        {
                                            //Get the context created with the ssrc defined above
                                            RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                                            //If that context is not null then allow it's ssrc to change now.
                                            if (context != null)
                                            {
                                                context.RemoteSynchronizationSourceIdentifier = ssrc.Value;

                                                if (seq.HasValue) context.SequenceNumber = seq.Value;

                                                if (rtpTime.HasValue) context.RtpTimestamp = rtpTime.Value;

                                                //if (context.Goodbye != null) context.Goodbye = null;

                                                context = null;
                                            }
                                        }
                                        else if (uri != null)
                                        {
                                            //Need to get the context by the uri.
                                            //Location = rtsp://abc.com/live/movie
                                            //uri = rtsp://abc.com/live/movie/trackId=0
                                            //uri = rtsp://abc.com/live/movie/trackId=1
                                            //uri = rtsp://abc.com/live/movie/trackId=2

                                            //Get the context created with from the media description with the same resulting control uri
                                            RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                                            //If that context is not null then allow it's ssrc to change now.
                                            if (context != null)
                                            {
                                                if (ssrc.HasValue) context.RemoteSynchronizationSourceIdentifier = ssrc.Value;

                                                if (seq.HasValue) context.SequenceNumber = seq.Value;

                                                if (rtpTime.HasValue) context.RtpTimestamp = rtpTime.Value;

                                                //if (context.Goodbye != null) context.Goodbye = null;

                                                context = null;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }

                    return response;
                }
            }
            catch { throw; }
        }

        /// <summary>
        /// Sends a PAUSE Request
        /// </summary>
        /// <param name="location">The location to indicate in the request</param>
        /// <returns>The response</returns>
        public RtspMessage SendPause(MediaDescription mediaDescription = null, bool force = false)
        {
            //Ensure media has been setup unless forced.
            if (mediaDescription != null && false == force)
            {
                //Check if the session supports pausing a specific media item
                if (false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

                //Get a context for the media
                var context = Client.GetContextForMediaDescription(mediaDescription);

                //If there is no context then throw an exception.
                if (context == null) throw new InvalidOperationException("The given mediaDescription has not been SETUP.");

                context = null;
            }

            //Keep track of whats playing
            if (mediaDescription == null) m_Playing.Clear();
            else m_Playing.Remove(mediaDescription);

            //Fire the event now
            OnPausing(mediaDescription);

            //Send the pause request, determining if the request is for all media or just one.
            return SendPause(mediaDescription != null ? mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) : CurrentLocation, force);
        }


        public RtspMessage SendPause(Uri location = null, bool force = false)
        {
            //If the server doesn't support it
            if (false == m_SupportedMethods.Contains(RtspMethod.PAUSE.ToString()) && false == force) throw new InvalidOperationException("Server does not support PAUSE.");

            //if (!Playing) throw new InvalidOperationException("RtspClient is not Playing.");
            using (RtspMessage pause = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.PAUSE,
                    Location = location ?? CurrentLocation
                })
            {
                return SendRtspMessage(pause);                 
            }
        }        

        /// <summary>
        /// Sends a ANNOUNCE Request
        /// </summary>
        /// <param name="location">The location to indicate in the request, otherwise null to use the <see cref="CurrentLocation"/></param>
        /// <param name="sdp">The <see cref="SessionDescription"/> to ANNOUNCE</param>
        /// <returns>The response</returns>
        public RtspMessage SendAnnounce(Uri location, SessionDescription sdp, bool force = false)
        {
            if (false == SupportedMethods.Contains(RtspMethod.ANNOUNCE.ToString()) && false == force) throw new InvalidOperationException("Server does not support ANNOUNCE.");
            if (sdp == null) throw new ArgumentNullException("sdp");
            using (RtspMessage announce = new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.ANNOUNCE,
                Location = location ?? CurrentLocation                
            })
            {
                announce.Body = sdp.ToString();
                announce.SetHeader(RtspHeaders.ContentType, Sdp.SessionDescription.MimeType);
                return SendRtspMessage(announce);
            }
        }

        //SendRecord

        internal void SendKeepAlive(object state)
        {
            bool wasPlaying = false, wasConnected = false;

            try
            {
                //Thrown an exception if IsDisposed
                if (IsDisposed) return;

                wasPlaying = IsPlaying;

                //Save the state of the connection
                wasConnected = IsConnected;

                //If the keep alive request feature is not disabled and the session times out if not kept alive
                if (wasPlaying && IsPlaying &&
                    false == DisableKeepAliveRequest &&
                    m_RtspSessionTimeout > TimeSpan.Zero)
                {
                    //Don't send a keep alive the stream is ending before the next keep alive
                    if (EndTime.HasValue && EndTime.Value != Utility.InfiniteTimeSpan &&
                        EndTime.Value - ((DateTime.UtcNow - m_StartedPlaying.Value)) <= m_RtspSessionTimeout) return;

                    //Ensure transport is connected.
                    if (false == m_RtpClient.IsConnected) m_RtpClient.Connect();

                    //Check if GET_PARAMETER is supported.
                    if (m_SupportedMethods.Contains(RtspMethod.GET_PARAMETER.ToString()))
                    {
                        using (SendGetParameter(null)) ;
                    }
                    else if (m_SupportedMethods.Contains(RtspMethod.OPTIONS.ToString())) //If at least options is supported
                    {
                        using (SendOptions()) ;
                    }
                    else if (m_SupportedMethods.Contains(RtspMethod.PLAY.ToString())) //If at least PLAY is supported
                    {
                        using (SendPlay()) ;
                    }
                }

                //Only perform these actions if playing anything.
                if (wasPlaying)
                {
                    //Raise events for ended media.
                    foreach (var context in Client.GetTransportContexts())
                    {
                        if (context == null || context.IsDisposed || context.IsContinious || context.TimeReceiving < context.MediaEndTime) continue;

                        //Remove from the playing media and if it was contained raise an event.
                        if (m_Playing.Remove(context.MediaDescription)) OnStopping(context.MediaDescription);
                    }

                    bool aggregateControl = SessionDescription.SupportsAggregateMediaControl(CurrentLocation);

                    //Iterate the played items looking for ended media.
                    for (int i = 0, e = m_Playing.Count; i < e; ++i)
                    {
                        if (e > m_Playing.Count) break;

                        var media = m_Playing[i];

                        //Get a context
                        var context = Client.GetContextForMediaDescription(media);

                        //If there is a context ensure it has not ended and has recieved data within the context receive interval.
                        if (context == null ||
                            false == context.IsDisposed ||
                            context.Goodbye == null ||
                            true == context.IsContinious ||
                            context.TimeSending < context.MediaEndTime) continue;

                        //Teardown the media if the session supports AggregateControl (Todo, Each context may have it's own sessionId)
                        //Otherwise Remove from the playing media and if it was contained raise an event.
                        if (aggregateControl && m_Playing.Contains(media)) SendTeardown(media, true);
                        else if (m_Playing.Remove(media)) OnStopping(media);

                        //If there was a context for the media ensure it is removed and disposed from the underlying transport.
                        if (context != null)
                        {

                            //handle leaving open changes?
                            //if(context.LeaveOpen)

                            //Remove the context from the rtp client.
                            Client.TryRemoveContext(context);

                            //Dispose of it.
                            context.Dispose();

                            //Remove any reference
                            context = null;
                        }
                    }


                    //Ensure media is still flowing if still playing otherwise raise the stopping event.
                    if (IsPlaying) EnsureMediaFlow();
                    else if (wasPlaying) OnStopping(); //Ensure not already raised?
                }

                //Determine next time to send a keep alive
                if (m_KeepAliveTimer != null && IsPlaying)
                {
                    //Todo, Check if the media will end before the next keep alive is due before sending.

                    if (m_LastMessageRoundTripTime < m_RtspSessionTimeout) m_KeepAliveTimer.Change(TimeSpan.FromTicks(m_RtspSessionTimeout.Subtract(m_LastMessageRoundTripTime + m_ConnectionTime).Ticks / 2), Utility.InfiniteTimeSpan);
                }
            }
            catch (Exception ex) { if (false == IsDisposed) Common.ILoggingExtensions.Log(Logger, "Exception Occured in SendKeepAlive: " + ex.Message); }

            if (IsDisposed) return;

            //Raise the stopping event if not playing anymore
            if (true == wasPlaying && false == IsPlaying) OnStopping();

            //Disconnect if was previously disconnected so long as the ProtocolSwitchTimer is not activated.
            //Might need a flag to see if DisconnectSocket was called.
            if (m_ProtocolSwitchTimer == null && false == wasConnected && IsPlaying && true == IsConnected) DisconnectSocket();
        }

        public void EnsureMediaFlow()
        {
            //If not waiting to switch protocols
            if (m_ProtocolSwitchTimer == null && m_InterleaveEvent.IsSet && IsPlaying)
            {

                //If not playing anymore do nothing
                if (EndTime != Utility.InfiniteTimeSpan && DateTime.UtcNow - m_StartedPlaying.Value > EndTime)
                {
                    StopPlaying();

                    return;
                }

                //Determine if there any are contexts without data flow by findings contexts where a packet has not been received  OR the last packet was received more then the interval ago.
                var contextsWithoutDataFlow = Client.GetTransportContexts().Where(tc => tc.IsRtpEnabled && 
                                                                                    tc.RtpPacketsReceived == 0 && tc.RtpPacketsSent == 0 || 
                                                                                    (tc.LastRtpPacketReceived > tc.ReceiveInterval) && 
                                                                                    tc.IsRtcpEnabled &&
                                                                                    tc.Goodbye != null ||
                                                                                    tc.RtcpPacketsReceived == 0 ||
                                                                                    tc.LastRtcpReportSent > tc.ReceiveInterval);

                //If there are such contexts
                if (m_InterleaveEvent.IsSet && IsPlaying && contextsWithoutDataFlow.Any())
                {
                    //If the server doens't support pause then we cant pause.
                    bool supportPause = m_SupportedMethods.Contains(RtspMethod.PAUSE.ToString());

                    //If any media was pausedOrStopped.
                    bool pausedOrStoppedAnything = false;

                    //If we cannot stop a single media item we will set this to true.
                    bool stopAll = false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation);

                    //Iterate all inactive contexts.
                    if(false == stopAll) foreach (var context in contextsWithoutDataFlow.ToArray())
                    {
                        //Ensure still in playing
                        if (false == m_Playing.Contains(context.MediaDescription)) continue;

                        //Send a pause request if not already paused and the server supports PAUSE and there has been any activity on the context
                        if (supportPause && context.HasAnyActivity)
                        {
                            //If not going to be playing anymore do nothing
                            if (context.TimeRemaining >= context.ReceiveInterval + m_LastMessageRoundTripTime + m_LastServerDelay) continue;

                            //If the context is not continious and there is no more time remaining do nothing
                            if (false == context.IsContinious && context.TimeRemaining <= TimeSpan.Zero) continue;

                            //Send the PAUSE request
                            using (var pauseResponse = SendPause(context.MediaDescription))
                            {
                                //If the paused request was not a sucess then it's probably due to an aggregate operation
                                pausedOrStoppedAnything = pauseResponse != null && pauseResponse.StatusCode == RtspStatusCode.OK;

                                //Determine if we have to stop everything.
                                if (false == pausedOrStoppedAnything)
                                {
                                    //See if everything has to be stopped.
                                    stopAll = pauseResponse.StatusCode == RtspStatusCode.AggregateOpperationNotAllowed;

                                    //Could move this logic to the SendPause method which would check the response status code before returning the response and then wouldn't raise the Pause event.

                                    //Ensure external state is observed
                                    m_Playing.Add(context.MediaDescription);

                                    OnPlaying(context.MediaDescription);

                                }
                            }
                        }
                        else
                        {
                            //If not going to be playing anymore do nothing
                            if (context.TimeRemaining >= context.ReceiveInterval + m_LastMessageRoundTripTime + m_LastServerDelay) continue;

                            //If the context is not continious and there is no more time remaining do nothing
                            if (false == context.IsContinious && context.TimeRemaining <= TimeSpan.Zero) continue;

                            //We can't pause so STOP JUST THIS MEDIA
                            using (var teardownResponse = SendTeardown(context.MediaDescription))
                            {
                                //If the Teardown was not a success then it's probably due to an aggregate operation.
                                pausedOrStoppedAnything = teardownResponse == null || teardownResponse != null && teardownResponse.StatusCode == RtspStatusCode.OK;
                                
                                //Determine if we have to stop everything.
                                if (false == pausedOrStoppedAnything)
                                {
                                    //See if everything has to be stopped.
                                    stopAll = teardownResponse.StatusCode == RtspStatusCode.AggregateOpperationNotAllowed;

                                    //Could move this logic to the SendTeardown method which would check the response status code before returning the response and then wouldn't raise the Pause event.

                                    //Ensure external state is observed
                                    m_Playing.Add(context.MediaDescription);

                                    OnPlaying(context.MediaDescription);
                                }
                            }
                        }

                        //If we have to stop everything and the server doesn't support pause then stop iterating.
                        if (stopAll) break;

                        //The media was paused or stopped, so play it again if anything was received
                        if (pausedOrStoppedAnything && context.TotalBytesReceieved > 0)
                        {
                            //Ensure the context state allows for sending again.
                            context.Goodbye = null;

                            Play(context.MediaDescription);
                        }
                    }

                    //If everything needs to stop.
                    if (stopAll && IsPlaying &&  
                        EndTime.HasValue && 
                        EndTime.Value != Utility.InfiniteTimeSpan &&
                        //And there is enough time to attempt
                        DateTime.UtcNow - m_StartedPlaying.Value > EndTime.Value.Subtract(m_LastMessageRoundTripTime.Add(m_ConnectionTime.Add(m_LastServerDelay))))
                    {

                        if (supportPause)
                        {
                            //Pause all media
                            Pause();

                            //Start playing again
                            StartPlaying();
                        }
                        else
                        {
                            //If still connected
                            if (IsConnected)
                            {
                                //Just send a play to continue receiving whatever media is still sending.
                                using (SendPlay()) ;

                            }
                            else
                            {
                                //Stop playing everything
                                StopPlaying();

                                //Start playing everything
                                StartPlaying();
                            }
                        }
                    } 
                }
            }
        }

        public RtspMessage SendGetParameter(string body = null, string contentType = null, bool force = false)
        {
            //…Content-type: application/x-rtsp-packetpair for WMS

            //If the server doesn't support it
            if (false == m_SupportedMethods.Contains(RtspMethod.GET_PARAMETER.ToString()) && false == force) throw new InvalidOperationException("Server does not support GET_PARAMETER.");

            using (RtspMessage get = new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.GET_PARAMETER,
                Location = CurrentLocation,
                Body = body ?? string.Empty
            })
            {
                if (false == string.IsNullOrWhiteSpace(contentType)) get.SetHeader(RtspHeaders.ContentType, contentType);

                return SendRtspMessage(get);
            }
        }

        public RtspMessage SendSetParameter(string body = null, string contentType = null, bool force = false)
        {
            //If the server doesn't support it
            if (false == SupportedMethods.Contains(RtspMethod.SET_PARAMETER.ToString()) && false == force) throw new InvalidOperationException("Server does not support GET_PARAMETER.");

            using (RtspMessage set = new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.SET_PARAMETER,
                Location = CurrentLocation,
                Body = body ?? string.Empty
            })
            {
                if (false == string.IsNullOrWhiteSpace(contentType)) set.SetHeader(RtspHeaders.ContentType, contentType);

                return SendRtspMessage(set);
            }
        }

        #endregion

        #region Overloads

        public override string ToString()
        {
            return string.Join(((char)Common.ASCII.HyphenSign).ToString(), base.ToString(), InternalId);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Stops sending any Keep Alive Immediately and calls <see cref="StopPlaying"/>.
        /// If the <see cref="RtpClient"/> is not null:
        /// Removes the <see cref="ProcessInterleaveData"/> event
        /// Disposes the RtpClient and sets it to null.
        /// Disposes and sets the Buffer to null.
        /// Disposes and sets the InterleavedEvent to null.
        /// Disposes and sets the m_LastTransmitted to null.
        /// Disposes and sets the <see cref="RtspSocket"/> to null if <see cref="LeaveOpen"/> allows.
        /// Removes connection times so <see cref="IsConnected"/> is false.
        /// Stops raising any events.
        /// Removes any <see cref="Logger"/>
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            DisableKeepAliveRequest = true;

            StopPlaying();

            if (m_SessionDescription != null)
            {
                m_SessionDescription.Dispose();

                m_SessionDescription = null;
            }

            base.Dispose();

            if (m_RtpClient != null)
            {
                m_RtpClient.InterleavedData -= ProcessInterleaveData;
                if (false == m_RtpClient.IsDisposed) m_RtpClient.Dispose();
                m_RtpClient = null;
            }

            if (m_Buffer != null)
            {
                m_Buffer.Dispose();
                m_Buffer = null;
            }

            if (m_InterleaveEvent != null)
            {
                m_InterleaveEvent.Dispose();
                m_InterleaveEvent = null;
            }

            if (m_LastTransmitted != null)
            {
                m_LastTransmitted.Dispose();
                m_LastTransmitted = null;
            }

            if (m_RtspSocket != null)
            {
                if (false == LeaveOpen) m_RtspSocket.Dispose();
                m_RtspSocket = null;
            }

            m_BeginConnect = m_EndConnect = null;

            OnConnect = null;
            OnDisconnect = null;
            OnStop = null;
            OnPlay = null;
            OnPause = null;
            OnRequest = null;
            OnResponse = null;

            Logger = null;
        }

        #endregion

        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            if (IsDisposed) yield break;

            yield return m_RtspSocket;
        }
    }
}
