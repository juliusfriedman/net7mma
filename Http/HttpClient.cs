using Media.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Media.Http
{
    //Good chance to implement some type of interface over Clients E.g. IClient or ITransportClient

    //ITransportSession

    //Expand concepts to RtspClient and RtpClient


    public class HttpClient : Common.BaseDisposable
    {

        #region Constants and Statics

        //Todo use SocketConfiguration
        /// <summary>
        /// Handle the configuration required for the given socket
        /// </summary>
        /// <param name="socket"></param>
        internal static void ConfigureHttpSocket(Socket socket)
        {
            if (socket == null) throw new ArgumentNullException("Socket");

            Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(socket);
            //socket.ExclusiveAddressUse = false;
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            //Don't buffer send.
            socket.SendBufferSize = 0;

            //Don't buffer receive.
            socket.ReceiveBufferSize = 0;

            //Dont fragment
            if (socket.AddressFamily == AddressFamily.InterNetwork) socket.DontFragment = true;

            //Rtsp over Tcp
            if (socket.ProtocolType == ProtocolType.Tcp)
            {
                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpNoSynRetries(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpTimestamp(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.SetTcpOffloadPreference(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpCongestionAlgorithm(socket);

                // Set option that allows socket to close gracefully without lingering.
                Media.Common.Extensions.Socket.SocketExtensions.DisableLinger(socket);

                //Retransmit for 0 sec
                if(Common.Extensions.OperatingSystemExtensions.IsWindows) Media.Common.Extensions.Socket.SocketExtensions.DisableTcpRetransmissions(socket);

                //If both send and receieve buffer size are 0 then there is no coalescing when nagle's algorithm is disabled
                Media.Common.Extensions.Socket.SocketExtensions.DisableTcpNagelAlgorithm(socket);
                //socket.NoDelay = true;

                //Allow more than one byte of urgent data
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpExpedited(socket);
                //socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);

                //Receive any urgent data in the normal data stream
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpOutOfBandDataInLine(socket);
            }
        }

        public const int DefaultBufferSize = HttpMessage.MaximumLength * 2;

        public const double DefaultProtocolVersion = 1.0;

        public static readonly TimeSpan DefaultConnectionTime = TimeSpan.FromMilliseconds(500);

        #endregion

        #region Events

        public delegate void HttpClientAction(HttpClient sender, object args);

        public delegate void RequestHandler(HttpClient sender, HttpMessage request);

        public delegate void ResponseHandler(HttpClient sender, HttpMessage request, HttpMessage response);

        public event HttpClientAction OnConnect;

        internal protected void OnConnected()
        {
            if (IsDisposed) return;

            HttpClientAction action = OnConnect;

            if (action == null) return;

            foreach (HttpClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }

        }

        public event RequestHandler OnRequest;

        internal protected void Requested(HttpMessage request)
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

        internal protected void Received(HttpMessage request, HttpMessage response)
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

        public event HttpClientAction OnDisconnect;

        internal void OnDisconnected()
        {
            if (IsDisposed) return;

            HttpClientAction action = OnDisconnect;

            if (action == null) return;

            foreach (HttpClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }
        }

        #endregion

        #region Fields

        internal readonly Guid InternalId = Guid.NewGuid();

        readonly System.Threading.ManualResetEventSlim m_InterleaveEvent;

        AuthenticationSchemes m_AuthenticationScheme;

        NetworkCredential m_Credential;

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
        EndPoint m_RemoteHttp;

        /// <summary>
        /// Keep track of certain values.
        /// </summary>
        int m_SentBytes, m_ReceivedBytes,
             m_HttpPort,
             m_SentMessages, m_ReTransmits,
             m_ReceivedMessages,
             m_PushedMessages,
             m_ResponseTimeoutInterval = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond;

        /// <summary>
        /// Keep track of timed values.
        /// </summary>
        TimeSpan m_SessionTimeout = TimeSpan.FromSeconds(60), //The default...
            m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

        DateTime? m_BeginConnect, m_EndConnect;

        public bool SendUserAgent, DateRequests, EchoXHeaders, AutomaticallyReconnect, Strict, SendChunked;

        HttpMessage m_LastTransmitted;

        Socket m_HttpSocket;

        /// <summary>
        /// Any additional headers which may be required by the RtspClient.
        /// </summary>
        public readonly Dictionary<string, string> AdditionalHeaders = new Dictionary<string, string>();

        string m_UserAgent = "ASTI HTTP Client", m_AuthorizationHeader;

        /// <summary>
        /// A ILogging instance
        /// </summary>
        public Common.ILogging Logger;

        //Really needs to be Connection or session will also need to refer to a connection
        internal Dictionary<string, HttpSession> m_Sessions = new Dictionary<string, HttpSession>();

        /// <summary>
        /// The inital, previous and current location's of the HttpClient
        /// </summary>
        Uri m_InitialLocation, m_PreviousLocation, m_CurrentLocation;

        #endregion

        #region Properties

        public EndPoint RemoteEndpoint { get { return m_RemoteHttp; } }

        /// <summary>
        /// Gets or Sets the method which is called when the <see cref="RtspSocket"/> is created, 
        /// typically during the call to <see cref="Connect"/>
        /// By default <see cref="ConfigureHttpSocket"/> is utilized.
        /// </summary>
        public Action<Socket> ConfigureSocket { get; set; }

        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        /// <notes>May want to do a partial receive for 1 byte which would take longer but indicate if truly connected. Udp may not be Connected.</notes>
        public bool IsConnected { get { return false == IsDisposed && m_ConnectionTime >= TimeSpan.Zero && m_HttpSocket != null; } }

        /// <summary>
        /// Gets or Sets the ReadTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketReadTimeout
        {
            get { return IsDisposed || m_HttpSocket == null ? -1 : m_HttpSocket.ReceiveTimeout; }
            set { if (IsDisposed || m_HttpSocket == null) return; m_HttpSocket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets or Sets the WriteTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketWriteTimeout
        {
            get { return IsDisposed || m_HttpSocket == null ? -1 : m_HttpSocket.SendTimeout; }
            set { if (IsDisposed || m_HttpSocket == null) return; m_HttpSocket.SendTimeout = value; }
        }

        /// <summary>
        /// The UserAgent sent with every RtspRequest
        /// </summary>
        public string UserAgent
        {
            get { return m_UserAgent; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("UserAgent cannot consist of only null or whitespace."); 
                m_UserAgent = value;
            }
        }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential
        {
            get { return m_Credential; }
            set { m_Credential = value; m_AuthorizationHeader = null; }
        }

        /// <summary>
        /// The type of AuthenticationScheme to utilize in RtspRequests
        /// </summary>
        public AuthenticationSchemes AuthenticationScheme
        {
            get { return m_AuthenticationScheme; }
            set
            {
                if (value == m_AuthenticationScheme) return;
                if (value != AuthenticationSchemes.Basic && value != AuthenticationSchemes.Digest && value != AuthenticationSchemes.None) throw new System.InvalidOperationException("Only None, Basic and Digest are supported");
                else m_AuthenticationScheme = value;
            }
        }

        /// <summary>
        /// The amount of bytes sent by the RtspClient
        /// </summary>
        public int BytesSent { get { return m_SentBytes; } }

        /// <summary>
        /// The amount of bytes recieved by the RtspClient
        /// </summary>
        public int BytesRecieved { get { return m_ReceivedBytes; } }

        /// <summary>
        /// Gets or sets a value indicating of the HttpSocket should be left open when Disposing.
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// The version of Http the client will utilize in messages
        /// </summary>
        public double ProtocolVersion { get; set; }

        /// <summary>
        /// Indicates if the client has tried to Authenticate using the current <see cref="Credential"/>'s
        /// </summary>
        public bool TriedCredentials { get { return false == string.IsNullOrWhiteSpace(m_AuthorizationHeader); } }

        /// <summary>
        /// The amount of <see cref="HttpMessage"/>'s sent by this instance.
        /// </summary>
        public int MessagesSent { get { return m_SentMessages; } }

        /// <summary>
        /// The amount of <see cref="HttpMessage"/>'s receieved by this instance.
        /// </summary>
        public int MessagesReceived { get { return m_ReceivedMessages; } }

        /// <summary>
        /// The amount of messages pushed by the remote party
        /// </summary>
        public int MessagesPushed { get { return m_PushedMessages; } }

        /// <summary>
        /// Gets or Sets the socket used for communication
        /// </summary>
        internal protected Socket HttpSocket
        {
            get { return m_HttpSocket; }
            set
            {
                m_HttpSocket = value;

                //Ensure not connected if the socket is removed
                if (m_HttpSocket == null)
                {
                    m_BeginConnect = m_EndConnect = null;

                    m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

                    return;
                }

                //If the socket is connected
                if (m_HttpSocket.Connected)
                {
                    //SO_CONNECT_TIME only exists on Windows...
                    //There are options if the stack supports it elsewhere.

                    //Set default values to indicate connected

                    m_BeginConnect = m_EndConnect = DateTime.UtcNow;

                    m_ConnectionTime = TimeSpan.Zero;

                    //Use the remote information from the existing socket rather than the location.

                    m_RemoteHttp = m_HttpSocket.RemoteEndPoint;

                    if (m_RemoteHttp is IPEndPoint)
                    {
                        IPEndPoint remote = (IPEndPoint)m_RemoteHttp;

                        m_RemoteIP = remote.Address;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or Sets the buffer used for data reception
        /// </summary>
        internal protected Common.MemorySegment Buffer
        {
            get { return m_Buffer; }
            set { m_Buffer = value; }
        }

        /// <summary>
        /// The amount of time taken to connect to the remote party.
        /// </summary>
        public TimeSpan ConnectionTime { get { return m_ConnectionTime; } }

        /// <summary>
        /// Gets or Sets amount the fraction of time the client will wait during a responses for a response without blocking.
        /// If less than or equal to 0 the value 1 will be used.
        /// </summary>
        public int ResponseTimeoutInterval
        {
            get { return m_ResponseTimeoutInterval; }
            set { m_ResponseTimeoutInterval = Binary.Clamp(value, 1, int.MaxValue); }
        }

        /// <summary>
        /// The last HttpMessage received by this HttpCliient from the remote EndPoint.
        /// </summary>
        public HttpMessage LastTransmitted { get { return m_LastTransmitted; } }

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

                        if (m_InitialLocation == null) m_InitialLocation = value;

                        //Backup the current location, (needs history list?)
                        m_PreviousLocation = m_CurrentLocation;

                        m_CurrentLocation = value;

                        switch (m_CurrentLocation.HostNameType)
                        {
                            case UriHostNameType.IPv4:
                            case UriHostNameType.IPv6:

                                m_RemoteIP = IPAddress.Parse(m_CurrentLocation.DnsSafeHost);

                                break;
                            case UriHostNameType.Dns:

                                if (m_HttpSocket != null)
                                {

                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault(a => a.AddressFamily == m_HttpSocket.AddressFamily);

                                    if (m_RemoteIP == null) throw new NotSupportedException("The given Location uses a HostNameType which is not the same as the underlying socket's address family. " + m_CurrentLocation.HostNameType + ", " + m_HttpSocket.AddressFamily + " And as a result no remote IP could be obtained to complete the connection.");
                                }
                                else
                                {
                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault();
                                }

                                break;

                            default: throw new NotSupportedException("The given Location uses a HostNameType which is not supported. " + m_CurrentLocation.HostNameType);
                        }

                        m_HttpPort = m_CurrentLocation.Port;

                        //Validate ports, should throw? should also use default for for scheme...
                        if (m_HttpPort <= ushort.MinValue || m_HttpPort > ushort.MaxValue) m_HttpPort = HttpMessage.TransportDefaultPort;

                        //Make a IPEndPoint 
                        m_RemoteHttp = new IPEndPoint(m_RemoteIP, m_HttpPort);
                    }
                }
                catch (Exception ex)
                {
                    Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Could not resolve host from the given location. See InnerException.", ex);

                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the Uri which was used first with this instance.
        /// </summary>
        public Uri InitialLocation { get { return m_InitialLocation; } }

        /// <summary>
        /// Gets the Uri which was used directly before the <see cref="CurrentLocation"/> with this instance.
        /// </summary>
        public Uri PreviousLocation { get { return m_PreviousLocation; } }

        /// <summary>
        /// Indicates if the RtspClient is currently sending or receiving data.
        /// </summary>
        public bool InUse { get { return false == m_InterleaveEvent.IsSet && m_InterleaveEvent.Wait((int)((m_SessionTimeout.TotalMilliseconds + 1) / m_ResponseTimeoutInterval)); } }

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The absolute location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize and will not deviate from the protocol is no data is received, if null it will be determined from the location Scheme</param>
        /// <param name="existing">An existing Socket</param>
        /// <param name="leaveOpen"><see cref="LeaveOpen"/></param>
        public HttpClient(Uri location, int bufferSize = DefaultBufferSize, Socket existing = null, bool leaveOpen = false, int responseTimeoutInterval = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            if (location == null) throw new ArgumentNullException("location");

            if (false == location.IsAbsoluteUri)
            {
                if (existing == null) throw new ArgumentException("Must be absolute unless a socket is given", "location");
                if (existing.Connected) location = Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.ToUri(((IPEndPoint)existing.RemoteEndPoint), HttpMessage.TransportScheme);
                else if (existing.IsBound) location = Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.ToUri(((IPEndPoint)existing.LocalEndPoint), HttpMessage.TransportScheme);
                else throw new InvalidOperationException("location must be specified when existing socket must be connected or bound.");
            }

            //Check the Scheme
            if (false == location.Scheme.StartsWith(HttpMessage.MessageIdentifier, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Uri Scheme must start with http", "location");

            //Set the location and determines the m_RtspProtocol and IP Protocol.
            CurrentLocation = location;

            //If there is an existing socket
            if (existing != null)
            {
                //Use it
                HttpSocket = existing;
            }

            //If no socket is given a new socket will be created

            //Check for a bufferSize of specified - unspecified value
            //Cases of anything less than or equal to 0 mean use the existing ReceiveBufferSize if possible.
            if (bufferSize <= 0) bufferSize = m_HttpSocket != null ? m_HttpSocket.ReceiveBufferSize : 0;

            //Create the segment given the amount of memory required if possible
            if (bufferSize > 0) m_Buffer = new Common.MemorySegment(bufferSize);
            else m_Buffer = new Common.MemorySegment(DefaultBufferSize); //Use 8192 bytes

            //If leave open is set the socket will not be disposed.
            LeaveOpen = leaveOpen;

            //Set the protocol version to use in requests.
            ProtocolVersion = DefaultProtocolVersion;
            
            //Could create a RtpClient to prevent accidental errors, (would be easier for attaching logger)

            ConfigureSocket = ConfigureHttpSocket;

            m_ResponseTimeoutInterval = responseTimeoutInterval;

            m_InterleaveEvent = new System.Threading.ManualResetEventSlim(true, m_ResponseTimeoutInterval);
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize, if null it will be determined from the location Scheme</param>
        /// <param name="bufferSize">The amount of bytes the client will use during message reception, Must be at least 4096 and if larger it will also be shared with the underlying RtpClient</param>
        public HttpClient(string location, int bufferSize = DefaultBufferSize)
            : this(new Uri(location), bufferSize) //UriDecode?
        {
            //Check for a null Credential and UserInfo in the Location given.
            if (Credential == null && false == string.IsNullOrWhiteSpace(CurrentLocation.UserInfo))
            {
                //Parse the given cred from the location
                Credential = Media.Common.Extensions.Uri.UriExtensions.ParseUserInfo(CurrentLocation);

                //Remove the user info from the location (may not have @?)
                m_InitialLocation = CurrentLocation = new Uri(CurrentLocation.AbsoluteUri.Replace(CurrentLocation.UserInfo + (char)Common.ASCII.AtSign, string.Empty).Replace(CurrentLocation.UserInfo, string.Empty));
            }
        }

        ~HttpClient()
        {
            Dispose();            
        }

        #endregion

        #region Methods

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
                if (false == force && IsConnected) return;

                //Deactivate any existing previous socket and erase connect times.
                if (m_HttpSocket != null) DisconnectSocket();

                //Create the socket
                m_HttpSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                
                //Configure the socket
                if (ConfigureSocket != null) ConfigureSocket(m_HttpSocket);

                //We started connecting now.
                m_BeginConnect = DateTime.UtcNow;

                //Handle the connection attempt (Assumes there is already a RemoteRtsp value)
                ProcessEndConnect(null);

            }
            catch (Exception ex)
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
        /// Increases the <see cref="SocketWriteTimeout"/> AND <see cref="SocketReadTimeout"/> by the time it took to establish the connection in milliseconds * 2.
        /// 
        /// </summary>
        /// <param name="state">Ununsed.</param>
        protected virtual void ProcessEndConnect(object state, int multiplier = 2)//should be vaarible in class
        {
            try
            {
                if (m_RemoteHttp == null) throw new InvalidOperationException("A remote end point must be assigned");

                //Try to connect
                m_HttpSocket.Connect(m_RemoteHttp);

                //Sample the clock after connecting
                m_EndConnect = DateTime.UtcNow;

                //Calculate the connection time.
                m_ConnectionTime = m_EndConnect.Value - m_BeginConnect.Value;

                //Set the read and write timeouts based upon such a time (should include a min of the m_RtspSessionTimeout.)
                if (m_ConnectionTime > TimeSpan.Zero) SocketWriteTimeout = SocketReadTimeout += (int)(m_ConnectionTime.TotalMilliseconds * multiplier);

                //Don't block
                //m_RtspSocket.Blocking = false;

                //Raise the Connected event.
                OnConnected();
            }
            catch { throw; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Disconnect(bool disconnectSocket = false)
        {
            if (disconnectSocket) DisconnectSocket();
        }

        /// <summary>
        /// If <see cref="IsConnected"/> nothing occurs.
        /// Disconnects the RtspSocket if Connected and <see cref="LeaveOpen"/> is false.  
        /// Sets the <see cref="ConnectionTime"/> to <see cref="Utility.InfiniteTimepan"/> so IsConnected is false.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void DisconnectSocket(bool force = false)
        {
            //If not connected and not forced return
            if (false == IsConnected && false == force) return;

            //When disconnecting the credentials must be used again when re-connecting.
            m_AuthorizationHeader = null;

            //Raise an event
            OnDisconnected();

            //If there is a socket
            if (m_HttpSocket != null)
            {
                //If LeaveOpen was false and the socket is not shared.
                if (false == LeaveOpen )
                {
                    //Dispose the socket
                    m_HttpSocket.Dispose();
                }

                //Set the socket to null (no longer will Share Socket)
                m_HttpSocket = null;
            }

            //Indicate not connected.
            m_BeginConnect = m_EndConnect = null;

            m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
        }

        /// <summary>
        /// DisconnectsSockets, Connects and optionally reconnects the Transport if reconnectClient is true.
        /// </summary>
        /// <param name="reconnectClient"></param>
        internal protected virtual void Reconnect()
        {
            DisconnectSocket();

            Connect();
        }

        public HttpMessage SendHttpMessage(HttpMessage message)
        {
            SocketError error;

            return SendHttpMessage(message, out error);
        }

        public HttpMessage SendHttpMessage(HttpMessage message, out SocketError error, bool useClientProtocolVersion = true, bool hasResponse = true, int attempts = 0)
        {
            //Indicate a send has not been attempted
            error = SocketError.SocketError;

            //Don't try to send if already disposed.
            CheckDisposed();

            bool wasBlocked = false;

            unchecked
            {
                try
                {
                    int retransmits = 0, attempt = attempts, //The attempt counter itself
                        sent = 0, received = 0, //counter for sending and receiving locally
                        offset = 0, length = 0;

                    //Wait for the smallest amount of time possible.
                    //int pollTime = (int)(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(m_RtspSocket)) + Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(m_ConnectionTime)); //(int)Math.Round(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(m_RtspSessionTimeout) / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMillisecond, MidpointRounding.ToEven)

                    //Half of the session timeout in milliseconds
                    int halfTimeout = (int)(m_SessionTimeout.TotalMilliseconds / 2);

                    byte[] buffer = null;

                    #region Check for a message

                    bool wasConnected = IsConnected;

                    //If there is no message to send then check for response
                    if (message == null) goto Connect;

                    #endregion

                    #region useClientProtocolVersion

                    //Ensure the request version matches the protocol version of the client if enforceVersion is true.
                    if (useClientProtocolVersion && message.Version != ProtocolVersion) message.Version = ProtocolVersion;

                    #endregion

                    #region Additional Headers

                    //Use any additional headers if given
                    if (AdditionalHeaders.Count > 0) foreach (var additional in AdditionalHeaders) message.AppendOrSetHeader(additional.Key, additional.Value);

                    #endregion

                    #region ContentEncoding

                    //Add the content encoding header if required
                    if (false == message.ContainsHeader(HttpHeaders.ContentEncoding) &&
                        message.ContentEncoding.WebName != HttpMessage.DefaultEncoding.WebName)
                        message.SetHeader(HttpHeaders.ContentEncoding, message.ContentEncoding.WebName);

                    #endregion

                    #region DateRequests

                    //Set the Date header if required
                    if (DateRequests && false == message.ContainsHeader(HttpHeaders.Date))
                        message.SetHeader(HttpHeaders.Date, DateTime.UtcNow.ToString("r"));

                    #endregion

                    #region SendUserAgent

                    //Add the user agent if required
                    if (SendUserAgent &&
                        false == message.ContainsHeader(HttpHeaders.UserAgent))
                    {
                        message.SetHeader(HttpHeaders.UserAgent, m_UserAgent);
                    }


                    #endregion

                    #region Credentials

                    //If there not already an Authorization header and there is an AuthenticationScheme utilize the information in the Credential
                    if (false == message.ContainsHeader(HttpHeaders.Authorization) &&
                        m_AuthenticationScheme != AuthenticationSchemes.None &&
                        Credential != null)
                    {
                        //Basic
                        if (m_AuthenticationScheme == AuthenticationSchemes.Basic)
                        {
                            message.SetHeader(HttpHeaders.Authorization, HttpHeaders.BasicAuthorizationHeader(message.ContentEncoding, Credential));
                        }
                        else if (m_AuthenticationScheme == AuthenticationSchemes.Digest)
                        {
                            //Could get values from m_LastTransmitted.
                            //Digest
                            message.SetHeader(HttpHeaders.Authorization,
                                HttpHeaders.DigestAuthorizationHeader(message.ContentEncoding, message.HttpMethod, message.Location, Credential, null, null, null, null, null, false, null, message.Body));
                        }
                        else if (m_AuthenticationScheme != AuthenticationSchemes.None)
                        {
                            message.SetHeader(HttpHeaders.Authorization, m_AuthenticationScheme.ToString());
                        }
                    }
                    #endregion

                    

                Connect:
                    #region Connect
                    //Wait for any existing requests to finish first
                    wasBlocked = InUse;

                    //If was block wait for that to finish
                    //if (wasBlocked) m_InterleaveEvent.Wait();

                    if (false == wasConnected && false == (wasConnected = IsConnected)) Connect();

                    //If the client is not connected then nothing can be done.

                    //Othewise we are connected
                    if (false == (wasConnected = IsConnected)) return null;

                    //Set the block if a response is required.
                    if (hasResponse && false == wasBlocked) m_InterleaveEvent.Reset();

                    //If nothing is being sent this is a receive only operation
                    if (message == null) goto NothingToSend;

                    #endregion

                    #region Prepare To Send

                    //Determine if the message should be sent in chunks
                    bool sendChunked = SendChunked && false == string.IsNullOrWhiteSpace(message.Body) || message.Version > 1.0 && string.Compare(message[HttpHeaders.TransferEncoding], "chunked", true) == 0;

                    //Protocol version < 1.1 does not support Transfer-Encoding.
                    if (sendChunked && ProtocolVersion < 1.1)
                    {
                        message.RemoveHeader(HttpHeaders.TransferEncoding);

                        foreach (var eh in message.GetEntityHeaders())
                        {
                            message.SetHeader(eh, message.GetEntityHeader(eh));

                            message.RemoveEntityHeader(eh);
                        }
                    }

                    //May not have to make a distinct case for multipart... the data would already be in the body...
                    bool multiPart = string.Compare(message[HttpHeaders.ContentType], "multipart/form-data", true) == 0;                 

                    

                    //Maybe should not require a Strict option?
                    //Checks for ContentLength and TransferEncoding and removes the ContentLength
                    //Checks for HostHeader
                    #region Strict

                    if (Strict)
                    {
                        #region MessageLength

                        if (message.Version >= 1.1 && message.ContentLength >= 0 && (sendChunked || message.ContainsHeader(HttpHeaders.TransferEncoding))) message.RemoveHeader(HttpHeaders.ContentLength);

                        #endregion

                        #region Host Headers

                        //http://www.w3.org/Protocols/rfc2616/rfc2616-sec19.html
                        // A client that sends an HTTP/1.1 request MUST send a Host header.
                        if (message.Version >= 1.1 && false == message.ContainsHeader(HttpHeaders.Host))
                        {
                            message.SetHeader(HttpHeaders.Host, message.Location.Host);

                            //Maybe should modify location to only be Path and Query

                            //if (message.Location.IsAbsoluteUri) message.Location = new Uri(message.Location.PathAndQuery);
                        }

                        #endregion

                        #region Invalid Protocol Headers

                        //Check for chunked when protocol is less than or equal to 1.0 and remove.

                        //Other checks.

                        #endregion
                    }

                    #endregion                    

                    //Get the bytes of the request, should only send headers if ContentLength == -1
                    buffer = sendChunked || multiPart ? message.Prepare(true, true, false, false).ToArray() : message.ToBytes();

                    offset = m_Buffer.Offset;

                    length = buffer.Length;

                    #endregion

                Send:
                    #region Send
                    //If the message was Transferred previously
                    if (message.Transferred.HasValue)
                    {
                        //Make the message not Transferred
                        message.Transferred = null;

                        //Increment counters for retransmit
                        ++retransmits;

                        ++m_ReTransmits;
                    }

                    sent += m_HttpSocket.Send(buffer, sent, length - sent, SocketFlags.None, out error);

                    #region Auto Reconnect

                    if (AutomaticallyReconnect &&
                        error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset)
                    {
                        //Check for the host to have dropped the connection
                        if (error == SocketError.ConnectionReset)
                        {
                            //Check if the client was connected already
                            if (wasConnected && false == IsConnected)
                            {
                                Reconnect();

                                goto Send;
                            }
                        }

                        throw new SocketException((int)error);
                    }

                    #endregion

                    //If this is not a re-transmit
                    if (sent >= length)
                    {
                        //Set the time when the message was transferred if this is not a retransmit.
                        message.Transferred = DateTime.UtcNow;

                        //Fire the event (sets Transferred)
                        Requested(message);

                        //Increment for messages sent or the messages retransmitted.
                        ++m_SentMessages;

                        //Increment our byte counters for Rtsp
                        m_SentBytes += sent;

                        //Attempt to receive so start attempts back at 0
                        sent = attempt = 0;

                        //Release the reference to the array
                        buffer = null;

                        //If the message is chunked or multipart then send the body now.
                        if (sendChunked) m_SentBytes += SendChunkedBody(message, out error, m_Buffer.Count);
                        else if (multiPart) m_SentBytes += SendMultipartBody(message, out error, m_Buffer.Count, "", "");

                        //If there was any entity headers send then now
                        if (message.EntityHeaderCount > 0)
                        {
                            buffer = message.PrepareEntityHeaders().ToArray();

                            length = buffer.Length;

                            //Handle this better, should increase attempts when error occurs...
                            while (sent < length && (error == SocketError.Success || error == SocketError.TryAgain || error == SocketError.TimedOut)) sent += m_HttpSocket.Send(buffer, sent, length - sent, SocketFlags.None, out error);

                            m_SentBytes += sent;

                            sent = length = 0;
                            
                            //Release the reference to the array
                            buffer = null;
                        }

                    }
                    else if (sent < length && ++attempt < m_ResponseTimeoutInterval)
                    {
                        //Make another attempt @
                        //Sending the rest
                        goto Send;
                    }

                    #endregion

                NothingToSend:
                    #region NothingToSend
                    //Check for no response.
                    if (false == hasResponse) return null;

                    #endregion

                //Receive some data (only referenced by the check for disconnection)
                Receive:
                    #region Receive
                    //If we can receive 
                    //if (m_RtspSocket != null && m_RtspSocket.Poll(pollTime, SelectMode.SelectRead))
                    //{
                    //Receive
                    received += m_HttpSocket.Receive(m_Buffer.Array, offset, m_Buffer.Count, SocketFlags.None, out error);
                    //}

                    #region Auto Reconnect

                    if (AutomaticallyReconnect &&
                        error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset)
                    {
                        //Check for the host to have dropped the connection
                        if (error == SocketError.ConnectionReset)
                        {
                            //Check if the client was connected already
                            if (wasConnected && false == IsConnected)
                            {
                                Reconnect();

                                goto Receive;
                            }
                        }

                        throw new SocketException((int)error);
                    }

                    #endregion

                    //If anything was received
                    if (received > 0)
                    {
                        //Data is received

                        //Otherwise just process the data via the event.
                        ProcessMessageData(this, m_Buffer.Array, offset, received);

                    }
                    else //Nothing was received
                    {
                        //Check for fatal exceptions
                        if (error != SocketError.ConnectionAborted && error != SocketError.ConnectionReset && error != SocketError.Interrupted)
                        {
                            if (++attempt <= m_ResponseTimeoutInterval) goto Wait;
                        }

                        //Raise the exception
                        if (message != null) throw new SocketException((int)error);
                        else return null;
                    }

                    #endregion

                //Wait for the response while the amount of data received was less than RtspMessage.MaximumLength
                Wait:
                    #region Waiting for response, Backoff or Retransmit
                    DateTime lastAttempt = DateTime.UtcNow;

                    //Wait while
                    while (false == IsDisposed &&//The client connected and is not disposed AND
                        //There is no last transmitted message assigned AND it has not already been disposed
                        (m_LastTransmitted == null || m_LastTransmitted.IsDisposed)
                        //AND the client is still allowed to wait
                        && ++attempt <= m_ResponseTimeoutInterval)
                    {
                        //Wait a small amount of time for the response because the cancellation token was not used...
                        if (IsDisposed)
                        {
                            return null;
                        }
                        else
                        {
                            //Wait a little more
                            System.Threading.Thread.Sleep(0);
                        }

                        //Check for any new messages
                        if (m_LastTransmitted != null) goto HandleResponse;

                        //Calculate how much time has elapsed
                        TimeSpan taken = DateTime.UtcNow - lastAttempt;

                        int readTimeout = SocketReadTimeout;

                        #region Timeouts

                        ////If more time has elapsed than allowed by reading
                        //if (taken > m_LastMessageRoundTripTime && taken.TotalMilliseconds >= readTimeout)
                        //{
                        //    //Check if we can back off further
                        //    if (taken.TotalMilliseconds >= halfTimeout) break;
                        //    else if (readTimeout < halfTimeout)
                        //    {
                        //        //Backoff
                        //        /*pollTime += (int)(Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond */
                        //        SocketWriteTimeout = SocketReadTimeout *= 2;
                        //    }

                        //    //If the client was not disposed re-trasmit the request if there is not a response pending already.
                        //    //Todo allow an option for this feature? (AllowRetransmit)
                        //    if (false == IsDisposed && m_LastTransmitted == null /*&& request.Method != RtspMethod.PLAY*/)
                        //    {
                        //        //handle re-transmission under UDP
                        //        if (m_HttpSocket.ProtocolType == ProtocolType.Udp)
                        //        {
                        //            //Retransmit the exact same data
                        //            goto Send;
                        //        }
                        //    }
                        //}

                        #endregion

                    }

                    #endregion

                HandleResponse:
                    #region HandleResponse

                    //Update counters for any data received.
                    m_ReceivedBytes += received;

                    //If nothing was received wait for cache to clear.
                    if (null == m_LastTransmitted)
                    {
                        System.Threading.Thread.Sleep(0);
                    }

                    #region Notes

                    //m_LastTransmitted is either null or not
                    //if it is not null it may not be the same response we are looking for. (mostly during threaded sends and receives)
                    //this could be dealt with by using a hash `m_Transactions` which holds requests which are sent and a space for their response if desired.
                    //Then a function GetMessage(message) would be able to use that hash to get the outgoing or incoming message which resulted.
                    //The structure of the hash would allow any response to be stored.

                    #endregion

                    //Check for the response if there was a message sent.
                    if (hasResponse &&
                        m_LastTransmitted != null && message != null &&
                        m_LastTransmitted.MessageType == HttpMessageType.Response)
                    {
                        //Calculate the amount of time taken to receive the message.
                        TimeSpan lastMessageRoundTripTime = (m_LastTransmitted.Created - (message.Transferred ?? message.Created));

                        //Ensure positive values for the RTT
                        if (lastMessageRoundTripTime < TimeSpan.Zero) lastMessageRoundTripTime = lastMessageRoundTripTime.Negate();

                        //TODO
                        //REDIRECT (Handle loops)
                        //if(m_LastTransmitted.StatusCode == RtspStatusCode.MovedPermanently)

                        switch (m_LastTransmitted.HttpStatusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                {
                                    //If we were not authorized and we did not give a nonce and there was an WWWAuthenticate header given then we will attempt to authenticate using the information in the header

                                    //If there was a WWWAuthenticate header in the response
                                    if (m_LastTransmitted.ContainsHeader(HttpHeaders.WWWAuthenticate) &&
                                        Credential != null) //And there have been Credentials assigned
                                    {
                                        Received(message, m_LastTransmitted);

                                        //Return the result of Authenticating with the given request and response (forcing the request if the credentails have not already been tried)
                                        return Authenticate(message, m_LastTransmitted);
                                    }

                                    //break
                                    break;
                                }
                            case HttpStatusCode.HttpVersionNotSupported:
                                {
                                    //if enforcing the version
                                    if (useClientProtocolVersion)
                                    {
                                        //Read the version from the response
                                        ProtocolVersion = m_LastTransmitted.Version;

                                        //Send the request again.
                                        return SendHttpMessage(message, out error, useClientProtocolVersion);
                                    }

                                    //break
                                    break;
                                }
                            default: break;
                        }

                        #region EchoXHeaders

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

                        #endregion

                        //Could have a remaining property which is set in parse body

                        //Http 1.0 without content-length header or with content-length and not yet completed.
                        if (received > 0 && m_LastTransmitted != null && 
                            ((m_LastTransmitted.ContentLength == -1 && m_LastTransmitted.Version >= 1.0) 
                            || 
                            false == m_LastTransmitted.IsComplete))
                        {
                            //Clear received counter
                            received = 0;

                            //Attempt to receive again
                            goto Receive;
                        }

                        #region UpdateSession

                        //Update the session related
                        //HttpSession related;

                        //if (m_Sessions.TryGetValue(m_SessionId, out related))
                        //{
                        //    related.UpdateMessages(message, m_LastTransmitted);

                        //    related = null;
                        //}

                        #endregion

                        //Raise an event for the message received
                        Received(message, m_LastTransmitted);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    Common.ILoggingExtensions.Log(Logger, ToString() + "@SendHttpMessage: " + ex.Message);
                }
                finally
                {
                    
                    //Determine if the host will close the connection
                    if (m_LastTransmitted != null && m_LastTransmitted[HttpHeaders.Connection] == "close")
                    {
                        Disconnect();
                    }

                    //Unblock (should not be needed)
                    if (false == wasBlocked) m_InterleaveEvent.Set();

                    //Check for Connection: close?
                }

                //Return the result
                return m_LastTransmitted;

            }//Unchecked
        }

        public HttpMessage SendChunkedMessage(HttpMessage message)
        {
            //Ensure 1.1
            if(message.Version < 1.1) message.Version = 1.1;

            //Determine if message had an existing encoding
            string transferEncoding = message.GetHeader(HttpHeaders.TransferEncoding);

            //If there was no transfer encoding or the transfer encoding is not chunked then set it to chunked
            if (string.IsNullOrWhiteSpace(transferEncoding) || (transferEncoding != "chunked")) message.SetHeader(HttpHeaders.TransferEncoding, "chunked");

            //Send the message and return the response.
            return SendHttpMessage(message);
        }


        public HttpMessage SendOptions(Uri location = null)
        {
            //Create an options request
            using (var httpOptions = new HttpMessage(HttpMessageType.Request, ProtocolVersion)
            {
                HttpMethod = Http.HttpMethod.OPTIONS,
                Location = location ?? CurrentLocation
            })
            {
                //Send the request and get the response
                using (var response = SendHttpMessage(httpOptions))
                {
                    //If there was a response
                    if (response != null)
                    {
                        //Get the header Allow
                        string allowed = response.GetHeader(HttpHeaders.Allow);

                        //If the allow header was not null
                        if (false == string.IsNullOrWhiteSpace(allowed))
                        {
                            //add to allowed methods, split on ','
                        }
                    }

                    return response;
                }
            }
        }

        public HttpMessage SendHead(Uri location = null)
        {
            //Create an options request
            using (var httpHead = new HttpMessage(HttpMessageType.Request, ProtocolVersion)
            {
                HttpMethod = Http.HttpMethod.HEAD,
                Location = location ?? CurrentLocation
            })
            {
                return SendHttpMessage(httpHead);
            }
        }

        public HttpMessage SendDelete(Uri location = null)
        {
            //Create an options request
            using (var httpDelete = new HttpMessage(HttpMessageType.Request, ProtocolVersion)
            {
                HttpMethod = Http.HttpMethod.DELETE,
                Location = location ?? CurrentLocation
            })
            {
                return SendHttpMessage(httpDelete);
            }
        }

        int SendChunkedBody(HttpMessage message, out SocketError error, int chunkSize)
        {
            error = SocketError.Success;

            byte[] toSend = message.ContentEncoding.GetBytes(message.Body);

            int totalSent = 0, index = 0, bytesRemaining = toSend.Length, justSent = 0, check = 0;

            byte[] ChunkHeader;

            while (bytesRemaining > 0 && error == SocketError.Success)
            {
                //Determine how much remains in the chunk
                int currentChunkSize = Binary.Min(chunkSize, bytesRemaining);

                //Create the chunk header
                ChunkHeader = message.ContentEncoding.GetBytes(currentChunkSize.ToString("X") + Environment.NewLine);

                //Keep track of what will be sent
                justSent = 0;

                //Check for the ChunkHeader to be completely sent.
                check = ChunkHeader.Length;

                //While there are bytes unsent, send them
                while (justSent < check && error == SocketError.Success) justSent += m_HttpSocket.Send(ChunkHeader, justSent, check - justSent, SocketFlags.None, out error);

                //Update the total
                totalSent += justSent;

                //Keep track of what will be sent
                justSent = 0;

                //Check for the chunkSize to be completely sent.
                check = chunkSize;

                //While there are bytes unsent, send them
                while (justSent < check && error == SocketError.Success) justSent += m_HttpSocket.Send(toSend, index + justSent, chunkSize - justSent, SocketFlags.None, out error);

                //Update the total
                totalSent += justSent;

                //Update how much remains
                bytesRemaining -= chunkSize;

                //Update the index in the chunkData
                index += chunkSize;
            }

            //Send 0 Chunk to complete

            //Create the 0 length ChunkHeader
            ChunkHeader = message.ContentEncoding.GetBytes(0.ToString("X") + Environment.NewLine);

            //Keep track of what will be sent
            justSent = 0;

            //Check for the chunkSize to be completely sent.
            check = chunkSize;

            //While there are bytes unsent, send them
            while (justSent < check && error == SocketError.Success) justSent += m_HttpSocket.Send(toSend, index + justSent, chunkSize - justSent, SocketFlags.None, out error);

            //Update the total
            totalSent += justSent;

            //Check for Trailer and send trailer

            string trailer = message.GetHeader(HttpHeaders.Trailer);

            if (false == string.IsNullOrWhiteSpace(trailer) && false == message.ContainsHeader(trailer))
            {
                //Send Trailer header(s)...
            }

            return totalSent;
        }

        int SendMultipartBody(HttpMessage message, out SocketError error, int chunkSize, string disposition, string contentType)
        {

            int totalSent = 0, boundarySize = 0;

            //extract boundary from contentType

            //http://www.w3.org/TR/html401/interact/forms.html#h-17.13.4.2

            //Boundary is always '--' + boundary

        //User-Agent: curl/7.21.2 (x86_64-apple-darwin)
        //Host: localhost:8080
        //Accept: */*
        //Content-Length: 1143
        //Expect: 100-continue
        //Content-Type: multipart/form-data; boundary=----------------------------83ff53821b7c


            //Should already be sent by this point, all headers

            //Time to send a boundary specified by the Content-Type, possibly a Disposition and then another Content-Type for the streams data

        //------------------------------83ff53821b7c
        //Content-Disposition: form-data; name="img"; filename="a.png"
        //Content-Type: application/octet-stream

        //?PNG

        //IHD?wS??iCCPICC Profilex?T?kA?6n??Zk?x?"IY?hE?6?bk
        //Y?<ߡ)??????9Nyx?+=?Y"|@5-?M?S?%?@?H8??qR>?׋??inf???O?????b??N?????~N??>?!?
        //??V?J?p?8?da?sZHO?Ln?}&???wVQ?y?g????E??0
        // ??
        //   IDAc????????-IEND?B`?

            //Stream data sent, output another boundary and possibly a Disposition and then another Content-Type for the streams data
            //This would be a second call
        
        //------------------------------83ff53821b7c
        //Content-Disposition: form-data; name="foo"
        //bar

            //Third call

        //------------------------------83ff53821b7c--

            //Stream data sent, output another boundary and return total sent.

            error = SocketError.Success;
            return 0;
        }

        //SendMultipart and SendChunked methods without a HttpMessage required?

        //SendFiles... etc

        /// <summary>
        /// Uses the given request to Authenticate the RtspClient when challenged.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public virtual HttpMessage Authenticate(HttpMessage request, HttpMessage response = null, bool force = false)
        {
            //If not forced and already TriedCredentials then return the response given.
            if (false == force && TriedCredentials && response == null) return response;

            //http://tools.ietf.org/html/rfc2617
            //3.2.1 The WWW-Authenticate Response Header
            //Example
            //WWW-Authenticate: Basic realm="nmrs_m7VKmomQ2YM3:", Digest realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525"\r\n

            //Needs to handle multiple auth types

            string authenticateHeader = response != null ? response[HttpHeaders.WWWAuthenticate] : string.Empty;

            if (false == string.IsNullOrWhiteSpace(m_AuthorizationHeader) && false == authenticateHeader.Contains("stale")) authenticateHeader = m_AuthorizationHeader;

            //Note should not be using ASCII, the request and response have the characters already encoded.

            //Should also be a hash broken up by key appropriately.

            //Should also handle when baseParts has 0 length

            string[] baseParts = authenticateHeader.Split(Media.Common.Extensions.Linq.LinqExtensions.Yield(((char)Common.ASCII.Space)).ToArray(), 2, StringSplitOptions.RemoveEmptyEntries);

            if (baseParts.Length > 1) baseParts = Media.Common.Extensions.Linq.LinqExtensions.Yield(baseParts[0]).Concat(baseParts[1].Split(HttpHeaders.Comma).Select(s => s.Trim())).ToArray();

            if (string.Compare(baseParts[0].Trim(), "basic", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Basic)
            {
                AuthenticationScheme = AuthenticationSchemes.Basic;

                request.SetHeader(HttpHeaders.Authorization, HttpHeaders.BasicAuthorizationHeader(request.ContentEncoding, Credential));

                //Recurse the call with the info from then authenticate header
                return SendHttpMessage(request);

            }
            else if (string.Compare(baseParts[0].Trim(), "digest", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Digest)
            {
                AuthenticationScheme = AuthenticationSchemes.Digest;

                //May use a different algorithmm
                string algorithm = baseParts.Where(p => p.StartsWith("algorithm", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(algorithm)) algorithm = "MD5";
                else
                {
                    algorithm = algorithm.Trim();
                    if (string.Compare(algorithm.Substring(9), "MD5", true) == 0) algorithm = "MD5";
                    else Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(response, "See the response in the Tag.", new NotSupportedException("The algorithm indicated in the authenticate header is not supported at this time. Create an issue for support."));
                }

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
                if (false == string.IsNullOrWhiteSpace(cnonce))
                {

                    if (m_LastTransmitted != null)
                    {
                        cnonce = "";
                    }

                    cnonce = cnonce.Substring(7).Replace("\"", string.Empty).Replace("\'", string.Empty);//cnonce = cnonce.Replace("cnonce=", string.Empty);
                }

                string uri = baseParts.Where(p => p.StartsWith("uri", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); //parts.Where(p => p.Contains("uri")).FirstOrDefault();
                bool rfc2069 = false == string.IsNullOrWhiteSpace(uri) && false == uri.Contains(HttpHeaders.HyphenSign);

                if (false == string.IsNullOrWhiteSpace(uri))
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

                //Set the header and store it for use later. Could take MethodString...
                request.SetHeader(HttpHeaders.Authorization, m_AuthorizationHeader = HttpHeaders.DigestAuthorizationHeader(request.ContentEncoding, request.HttpMethod, request.Location, Credential, qop, nc, nonce, cnonce, opaque, rfc2069, algorithm, request.Body));

                //Todo 'Authorization' property?

                //Recurse the call with the info from then authenticate header
                return SendHttpMessage(request);
            }
            else
            {
                throw new NotSupportedException("The given Authorization type is not supported, '" + baseParts[0] + "' Please use Basic or Digest.");
            }
        }

        void ProcessMessageData(object sender, byte[] data, int offset, int length)
        {
            if (length == 0) return;

            //Cache offset and count, leave a register for received data (should be calulated with length)
            int received = 0;

            //If m_LastTransmitted != null
            //Check m_LastTransmitted for a Transfer-Encoding of "chunked"
            //https://en.wikipedia.org/wiki/Chunked_transfer_encoding#Format

            //If present then ParseChunkData

            unchecked
            {
                //Validate the data received
                HttpMessage message = new HttpMessage(data, offset, length);

                //Determine what to do with the interleaved message
                switch (message.MessageType)
                {
                    //Handle new requests or responses
                    case HttpMessageType.Request:
                    case HttpMessageType.Response:
                        {
                            //Sometimes the content may contain characters which don't belong to a new message, validate this by checking the version
                            //if (message.Version == 0.0) goto case HttpMessageType.Invalid;

                            if (message.Version != ProtocolVersion) goto case HttpMessageType.Invalid;

                            //Calculate the length of what was received
                            received = length;

                            if (received > 0)
                            {
                                //Increment for messages received
                                ++m_ReceivedMessages;

                                //If not playing an interleaved stream, Complete the message if not complete (Should maybe check for Content-Length)
                                while (false == message.IsComplete)
                                {
                                    //Take in some bytes from the socket
                                    int justReceived = message.CompleteFrom(m_HttpSocket, m_Buffer);

                                    if (justReceived == 0) break;

                                    //Incrment for justReceived
                                    received += justReceived;

                                    //Ensure we are not doing to much receiving
                                    if (message.ContentLength > 0 && received > HttpMessage.MaximumLength + message.ContentLength) break;
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
                                m_LastTransmitted = message;

                                //Need a method to get a Session by a Message.
                                //Update the messge on the session..

                                //if the message was a request and is complete handle it now.
                                if (m_LastTransmitted.MessageType == HttpMessageType.Request &&
                                    false == InUse)
                                {
                                    ProcessServerSentRequest(m_LastTransmitted);
                                }

                            }

                            goto default;
                        }
                    case HttpMessageType.Invalid:
                        {
                            //If there was a previous message then dispose the invalid message
                            if (m_LastTransmitted != null)
                            {
                                //Dispose the invalid message
                                message.Dispose();

                                message = null;
                            }//Otherwise keep the invalid message and allow to be completed later
                            else
                            {
                                m_LastTransmitted = message;

                                goto default;
                            }
                           

                            //If playing and interleaved stream AND the last transmitted message is NOT null and is NOT Complete then attempt to complete it
                            if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted))
                            {
                                //RtspMessage local = m_LastTransmitted;

                                //Take note of the length of the last transmitted message.
                                int lastLength = m_LastTransmitted.Length;

                                //Create a memory segment and complete the message as required from the buffer.
                                using (var memory = new Media.Common.MemorySegment(data, offset, length))
                                {
                                    //Use the data recieved to complete the message and not the socket
                                    int justReceived = false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) ? m_LastTransmitted.CompleteFrom(null, memory) : 0;

                                    //If anything was received
                                    if (justReceived > 0)
                                    {
                                        //Account for what was just recieved.
                                        received += justReceived;

                                        //No data was consumed don't raise another event.
                                        if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) && lastLength == m_LastTransmitted.Length) received = 0;
                                    }

                                    //handle the completion of a request sent by the server if allowed.
                                    if (received > 0 &&
                                        false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) &&
                                        m_LastTransmitted.MessageType == HttpMessageType.Request &&
                                        false == InUse) //dont handle if waiting for a resposne...
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
                                else if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) &&
                                    m_LastTransmitted.MessageType == HttpMessageType.Response) //and was a response
                                {
                                    //Otherwise indicate a message has been received now. (for responses only)
                                    Received(m_LastTransmitted, null);
                                }

                                //Handle any data remaining in the buffer
                                if (received < length)
                                {
                                    //(Must ensure Length property of RtspMessage is exact).
                                    ProcessMessageData(sender, data, received, length - received);
                                }
                            }

                            //done
                            return;
                        }
                }
            }
        }      

        private void ProcessServerSentRequest(HttpMessage m_LastTransmitted)
        {

            //Todo

            //throw new NotImplementedException();

            return;
        }

        #endregion
    }
}
