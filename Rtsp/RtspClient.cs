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
    public class RtspClient : IDisposable
    {        

        internal static char[] TimeSplit = new char[] { '-', ';' };

        #region Nested Types

        public class RtspClientException : Exception
        {
            //Might choose to put a RtspRequest here 
            public RtspClientException(string message) : base(message) { }
            public RtspClientException(string message, Exception inner) : base(message, inner) { }
        }

        public enum ClientProtocolType
        {
            Tcp = ProtocolType.Tcp,
            Reliable = Tcp,
            Udp = ProtocolType.Udp,
            Unreliable = Udp,
            Http = 2
        }

        #endregion

        #region Fields

        bool m_ForcedProtocol;

        ClientProtocolType m_RtspProtocol;

        ManualResetEvent m_InterleaveEvent = new ManualResetEvent(false);

        RtspResponse m_LastRtspResponse;

        AuthenticationSchemes m_AuthenticationScheme;

        /// <summary>
        /// The location the media
        /// </summary>
        Uri m_Location;

        /// <summary>
        /// The buffer this client uses for all requests 4MB
        /// </summary>
        byte[] m_Buffer = new byte[RtspMessage.MaximumLength];

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
        /// The last method used in a request to the RtspServer
        /// </summary>
        RtspMethod m_LastMethod;

        /// <summary>
        /// The session description associated with the media at Location
        /// </summary>
        SessionDescription m_SessionDescription;

        /// <summary>
        /// Need to seperate counters and other stuff
        /// </summary>
        int m_SentBytes, m_RecievedBytes,
            m_RtspTimeoutSeconds, m_RtspPort, m_CSeq,
            m_ProtocolSwitchSeconds = 10, m_RetryCount = 5;

        List<RtspMethod> m_SupportedMethods = new List<RtspMethod>();

        string m_UserAgent = "ASTI RTP Client", m_SessionId, m_TransportMode;

        internal RtpClient m_RtpClient;

        Timer m_KeepAliveTimer, m_ProtocolSwitchTimer;

        bool m_Live;

        TimeSpan? m_StartTime, m_EndTime;

        #endregion

        #region Properties

        /// <summary>
        /// If playing, the TimeSpan which represents the time this media started playing from.
        /// </summary>
        public TimeSpan? StartTime { get { return m_StartTime; } }

        /// <summary>
        /// If playing, the TimeSpan which represents the time the media will end.
        /// </summary>
        public TimeSpan? EndTime { get { return m_EndTime; } }

        /// <summary>
        /// If playing, indicates if the RtspClient is playing from a live source which means there is no absolute start or end time and seeking may not be supported.
        /// </summary>
        public bool LivePlay { get { return m_Live; } }

        /// <summary>
        /// The amount of time in seconds in which the RtspClient will switch protocols if no Packets have been recieved.
        /// </summary>
        public int ProtocolSwitchSeconds { get { return m_ProtocolSwitchSeconds; } set { m_ProtocolSwitchSeconds = value; } }

        /// <summary>
        /// The amount of times each RtspRequest will be sent if a response is not recieved in ReadTimeout
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }

        /// <summary>
        /// The last method requested
        /// </summary>
        public RtspMethod LastMethod { get { return m_LastMethod; } }


        //The last RtspResponse recieved by the RtspClient
        public RtspResponse LastResponse { get { return m_LastRtspResponse; } }

        /// <summary>
        /// The ClientProtocolType the RtspClient is using Reliable (Tcp), Unreliable(Udp) or Http(Tcp)
        /// </summary>
        public ClientProtocolType RtspProtocol { get { return m_RtspProtocol; } set { m_RtspProtocol = value; } }

        /// <summary>
        /// The ProtocolType the RtspClient will setup for underlying RtpClient.
        /// </summary>
        public ProtocolType RtpProtocol { get { return m_RtpProtocol; } set { if (value != ProtocolType.Udp || value != ProtocolType.Tcp) throw new ArgumentException(); m_RtpProtocol = value; } }

        /// <summary>
        /// Gets or sets location to the Media on the Rtsp Server and updates Remote information and ClientProtocol if required by the change.
        /// If the RtspClient was listening then it will be stopped and started again
        /// </summary>
        public Uri Location
        {
            get { return m_Location; }
            set
            {
                try
                {
                    //If Different
                    if (m_Location != value)
                    {

                        bool wasListening = Listening;

                        if (wasListening) StopListening();

                        m_Location = value;

                        //(Should allow InterNetworkV6)
                        m_RemoteIP = System.Net.Dns.GetHostAddresses(m_Location.Host).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

                        m_RtspPort = m_Location.Port;

                        //Validate prots
                        if (m_RtspPort <= ushort.MinValue || m_RtspPort > ushort.MaxValue) m_RtspPort = RtspServer.DefaultPort;

                        //Determine protocol
                        if (m_Location.Scheme == RtspMessage.ReliableTransport) m_RtspProtocol = ClientProtocolType.Tcp;
                        else if (m_Location.Scheme == RtspMessage.UnreliableTransport) m_RtspProtocol = ClientProtocolType.Udp;
                        else m_RtspProtocol = ClientProtocolType.Http;

                        //Make a IPEndPoint 
                        m_RemoteRtsp = new IPEndPoint(m_RemoteIP, m_RtspPort);

                        if (wasListening) StartListening();
                    }
                }
                catch (Exception ex)
                {
                    throw new RtspClientException("Could not resolve host from the given location", ex);
                }
            }
        }

        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        public bool Connected { get { return m_RtspSocket != null && m_RtspSocket.Connected || m_RtspProtocol == ClientProtocolType.Udp; } }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential { get; set; }

        /// <summary>
        /// The type of AuthenticationScheme to utilize in RtspRequests
        /// </summary>
        public AuthenticationSchemes AuthenticationScheme { get { return m_AuthenticationScheme; } set { if (value == m_AuthenticationScheme) return; if (value != AuthenticationSchemes.Basic && value != AuthenticationSchemes.Digest && value != AuthenticationSchemes.None) throw new System.InvalidOperationException("Only None, Basic and Digest are supported"); else m_AuthenticationScheme = value; } }

        /// <summary>
        /// Indicates if the RtspClient has started listening for Rtp Packets
        /// </summary>
        public bool Listening { get { return Connected && m_RtpClient != null && m_RtpClient.Connected; } }

        /// <summary>
        /// The amount of bytes sent by the RtspClient
        /// </summary>
        public int BytesSent { get { return m_SentBytes; } }

        /// <summary>
        /// The amount of bytes recieved by the RtspClient
        /// </summary>
        public int BytesRecieved { get { return m_RecievedBytes; } }

        /// <summary>
        /// The current SequenceNumber of the RtspClient
        /// </summary>
        public int ClientSequenceNumber { get { return m_CSeq; } }

        /// <summary>
        /// Gets the SessionDescription provided by the server for the media at <see cref="Location"/>
        /// </summary>
        public SessionDescription SessionDescription { get { return m_SessionDescription; } internal set { m_SessionDescription = value; } }

        /// <summary>
        /// Gets the methods supported by the server recieved in the options request.
        /// </summary>
        public Rtsp.RtspMethod[] SupportedMethods { get { return m_SupportedMethods.ToArray(); } }

        /// <summary>
        /// The RtpClient associated with this RtspClient
        /// </summary>
        public RtpClient Client { get { return m_RtpClient; } }

        /// <summary>
        /// Gets or Sets the ReadTimeout of the underlying NetworkStream / Socket
        /// </summary>
        public int ReadTimeout { get { return m_RtspSocket.ReceiveTimeout; } set { m_RtspSocket.ReceiveTimeout = value; } }

        /// <summary>
        /// Gets or Sets the WriteTimeout of the underlying NetworkStream / Socket
        /// </summary>
        public int WriteTimeout { get { return m_RtspSocket.SendTimeout; } set { m_RtspSocket.SendTimeout = value; } }

        /// <summary>
        /// The UserAgent sent with every RtspRequest
        /// </summary>
        public string UserAgent { get { return m_UserAgent; } set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("UserAgent cannot consist of only null or whitespace."); m_UserAgent = value; } }

        /// <summary>
        /// Indicates if the Client is using Rtsp (Reliable, Http)Tcp or (Unreliable)Udp
        /// </summary>
        public ClientProtocolType RtspProtocolType { get { return m_RtspProtocol; } }

        /// <summary>
        /// Indicates if the protocol the Client is performing Rtp over
        /// </summary>
        public ProtocolType RtpProtocolType { get { return m_RtpProtocol; } }

        #endregion

        #region Constructor

        static RtspClient()
        {
            if (!UriParser.IsKnownScheme(RtspMessage.ReliableTransport))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.ReliableTransport, 554);

            if (!UriParser.IsKnownScheme(RtspMessage.UnreliableTransport))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.UnreliableTransport, 555);
        }

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The absolute location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize and will not deviate from the protocol is no data is received, if null it will be determined from the location Scheme</param>
        public RtspClient(Uri location, ClientProtocolType? rtpProtocolType = null)
        {
            if (!location.IsAbsoluteUri) throw new ArgumentException("Must be absolute", "location");
            if (!(location.Scheme == RtspMessage.ReliableTransport || location.Scheme == RtspMessage.UnreliableTransport || location.Scheme == System.Uri.UriSchemeHttp)) throw new ArgumentException("Uri Scheme must be rtsp or rtspu or http", "location");

            //Set the location and determines the m_RtspProtocol
            Location = location;

            OnDisconnect += RtspClient_OnDisconnect;

            //If the client has specified a Protcol to use then use it
            if (rtpProtocolType.HasValue)
            {
                //The Protocol was forced.
                m_ForcedProtocol = true;

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
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize, if null it will be determined from the location Scheme</param>
        public RtspClient(string location, ClientProtocolType? rtpProtocolType = null) : this(new Uri(location), rtpProtocolType) { }

        ~RtspClient()
        {
            OnDisconnect -= RtspClient_OnDisconnect;            
        }

        #endregion

        #region Events

        public delegate void RtspClientAction(RtspClient sender, object args);

        public delegate void RequestHandler(RtspClient sender, RtspRequest request);

        public delegate void ResponseHandler(RtspClient sender, RtspRequest request, RtspResponse response);

        public event RtspClientAction OnConnect;

        internal void OnConnected() { if (OnConnect != null) OnConnect(this, EventArgs.Empty); }

        public event RequestHandler OnRequest;

        internal void Requested(RtspRequest request) { if (OnRequest != null) OnRequest(this, request); }

        public event ResponseHandler OnResponse;

        internal void Received(RtspRequest request, RtspResponse response) { if (OnResponse != null) OnResponse(this, request, response); }

        public event RtspClientAction OnDisconnect;

        internal void OnDisconnected() { if (OnDisconnect != null) OnDisconnect(this, EventArgs.Empty); }

        public event RtspClientAction OnPlay;

        internal void Playing() { if (OnPlay != null) OnPlay(this, EventArgs.Empty); }

        public event RtspClientAction OnStop;

        internal void Stopping(MediaDescription mediaDescription = null) { if (OnStop != null) OnStop(this, mediaDescription); }

        #endregion

        #region Methods

        static void RtspClient_OnDisconnect(RtspClient sender, object args)
        {
            if (sender.m_KeepAliveTimer != null)
            {
                sender.m_KeepAliveTimer.Dispose();
                sender.m_KeepAliveTimer = null;
            }
        }

        /// <summary>
        /// Handles Interleaved Data for the RtspClient
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="slice"></param>
        void m_RtpClient_InterleavedData(RtpClient sender, ArraySegment<byte> slice)
        {
            //If the slice begins with R (which is what all RTSP messages begin with
            if (slice.Array[0] == 'R')
            {


#if DEBUG
                System.Diagnostics.Debug.WriteLine("InterleavedData (Rtsp): " + System.Text.Encoding.ASCII.GetString(slice.Array));
#endif

                //Update counters
                m_RecievedBytes += slice.Count - slice.Offset;

                //Add the response to out list
                m_LastRtspResponse = new RtspResponse(slice.Array, slice.Offset);
                
                //Clear the event so whoever is waiting can get the response
                m_InterleaveEvent.Set();
            }
        }

        /// <summary>
        /// Increments and returns the current SequenceNumber
        /// </summary>
        internal int NextClientSequenceNumber() { return ++m_CSeq; }

        public void StartListening()
        {
            if (Listening && Client.TotalRtpBytesReceieved > 0) return;
            try
            {
                try
                {
                    //Connect the socket
                    Connect();
                }
                catch (Exception ex)
                {
                    throw new RtspClientException("Could not get Connect to Remote Host: " + ex.Message, ex);
                }

                try
                {
                    //Send the options
                    SendOptions();
                }
                catch (Exception ex)
                {
                    throw new RtspClientException("Could not get Options: " + ex.Message, ex);
                }

                //If we can describe
                if (m_SupportedMethods.Contains(RtspMethod.DESCRIBE))
                {
                    try
                    {
                        //Send describe
                        SendDescribe();
                    }
                    catch (Exception ex)
                    {
                        throw new RtspClientException("Could not Describe: " + ex.Message, ex);
                    }
                }


                //If we can setup
                if (m_SupportedMethods.Contains(RtspMethod.SETUP))
                {
                    //For each MediaDescription in the SessionDecscription
                    foreach (Sdp.MediaDescription md in SessionDescription.MediaDescriptions)
                    {
                        try
                        {                           
                            //Send a setup
                            SendSetup(md);
                        }
                        catch(Exception ex)
                        {
                            throw new RtspClientException("Could not Setup: " + ex.Message, ex);                            
                        }
                    }
                }

                //If we can play
                if (m_SupportedMethods.Contains(RtspMethod.PLAY))
                {
                    try
                    {
                        //Find range info in the SDP
                        var rangeInfo = SessionDescription.Lines.Where(l => l.Parts.Any(p => p.Contains("range"))).FirstOrDefault();

                        //If there is a range directive
                        if (rangeInfo != null)
                        {
                            string[] parts = rangeInfo.Parts[0].Replace("range:", string.Empty).Split('-');

                            string rangeType = null;

                            //Determine if the SDP also contains the format specifier
                            if (parts[0].Contains("="))
                            {
                                rangeType = parts[0].Substring(0, parts[0].IndexOf('='));

                                //Ensure first part only contains value not specifier
                                parts[0] = parts[0].Replace(rangeType, string.Empty);
                                parts[0] = parts[0].Replace("=", string.Empty);
                            }

                            //If there is a start and end time
                            if (parts.Length > 1)
                            {
                                //Send the play with the indicated start and end time
                                SendPlay(Location, TimeSpan.Parse(parts[0].Trim()), TimeSpan.Parse(parts[1].Trim()), rangeType);
                            }
                            else
                            {
                                //Send the play with the indicated start time only
                                SendPlay(Location, TimeSpan.Parse(parts[0].Trim()), null, rangeType);
                            }
                        }
                        else
                        {
                            //Send to default play
                            SendPlay();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new RtspClientException("Could not Play: " + ex.Message, ex);
                    }
                }

            }
            catch
            {
                throw;
            }
        }

        public void StopListening()
        {
            if (!Listening) return;
            Disconnect();
        }

        public void Connect()
        {
            try
            {
                if (Connected) return;
                else if (m_RtspProtocol ==  ClientProtocolType.Http || m_RtspProtocol == ClientProtocolType.Reliable)
                {
                    m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }
                else if (m_RtspProtocol == ClientProtocolType.Unreliable)
                {
                    m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                }
                m_RtspSocket.Connect(m_RemoteRtsp);
                ReadTimeout = 5000;

                OnConnected();
            }
            catch (Exception ex)
            {
                throw new RtspClientException("Could not connect to remote host", ex);
            }
        }

        public void Disconnect()
        {
            try
            {
                //Get rid of the timers

                if (m_ProtocolSwitchTimer != null)
                {
                    m_ProtocolSwitchTimer.Dispose();
                    m_ProtocolSwitchTimer = null;
                }

                if (m_KeepAliveTimer != null)
                {
                    m_KeepAliveTimer.Dispose();
                    m_KeepAliveTimer = null;
                }

                //Determine if we need to do anything
                if (Listening && !string.IsNullOrWhiteSpace(m_SessionId))
                {

                    //Close the RtpClient
                    if (m_RtpClient != null)
                    {
                        m_RtpClient.Disconnect();
                    }
               

                    //Send the Teardown
                    try
                    {                        
                        SendTeardown();
                    }
                    catch 
                    {
                        //We may not recieve a response if the socket is closed in a violatile fashion on the sending end
                        //And we realy don't care
                    }

                }
                
                //Get rid of this socket
                if (m_RtspSocket != null)
                {
                    m_RtspSocket.Dispose();
                    m_RtspSocket = null;
                }

                //Fire an event
                OnDisconnected();
            }
            catch { }
        }

        #endregion

        #region Rtsp

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal RtspResponse SendRtspRequest(RtspRequest request)
        {
            try
            {
                if (!Connected) throw new RtspClientException("You must first Connect before sending a request");

                if (request[RtspHeaders.UserAgent] == null)
                {
                    request.SetHeader(RtspHeaders.UserAgent, m_UserAgent);
                }

                //If there is an AuthenticationScheme utilize the information in the Credential
                if (m_AuthenticationScheme != AuthenticationSchemes.None && Credential != null)
                {
                    if (m_AuthenticationScheme == AuthenticationSchemes.Basic)
                    {
                        //Encoding should be a property on the Listener which defaults to utf8
                        request.SetHeader(RtspHeaders.Authroization, "Basic " + Convert.ToBase64String(request.Encoding.GetBytes(Credential.UserName + ':' + Credential.Password)));
                    }
                    else if (m_AuthenticationScheme == AuthenticationSchemes.Digest)
                    {
                        //Digest Impl

                        //Digest RFC2069
                        /* Example header -
                         * 
                         Authorization: Digest username="Mufasa",
                             realm="testrealm@host.com",
                             nonce="dcd98b7102dd2f0e8b11d0f600bfb0c093",
                             uri="/dir/index.html",
                             qop=auth,
                             nc=00000001,
                             cnonce="0a4f113b",
                             response="6629fae49393a05397450978507c4ef1",
                             opaque="5ccc069c403ebaf9f0171e9517f40e41"
                         */

                        //Need to calculate based on hash  

                        string username, realm, nonce, nc, cnonce, uri, qop, opaque, response;

                        username = "username=" + Credential.UserName;

                        realm = "/";

                        nc = "nc=00000001";

                        {
                            int a = Utility.Random.Next(int.MaxValue);
                            nonce = "nonce=" + (a + (uint)(a - Utility.Random.Next(int.MaxValue))).ToString("X");
                        }

                        cnonce = "cnonce=" + Utility.Random.Next(int.MaxValue).ToString("X");

                        uri = "uri=\""+Location.PathAndQuery + '"';

                        qop = "qop=auth";

                        {
                            int a = Utility.Random.Next(int.MaxValue);
                            opaque = "opaque=" + (a + (uint)(a - Utility.Random.Next(int.MaxValue))).ToString("X");
                        }

                        //http://en.wikipedia.org/wiki/Digest_access_authentication
                        //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                        byte[] HA1 = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(Credential.UserName + ':' + realm + ':' + Credential.Password));

                        //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                        byte[] HA2 = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(request.Method + ':' + uri));

                        //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                        byte[] ResponseHash = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(Convert.ToString(HA1).Replace("-", string.Empty) + ':' + nonce + ':' + nc + ':' + cnonce + ':' + qop + ':' + Convert.ToString(HA2).Replace("-", string.Empty)));

                        response = "response=" + Convert.ToString(ResponseHash).Replace("-", string.Empty);

                        request.SetHeader(RtspHeaders.Authroization, "Digest " + string.Join(",", username, realm, nonce, uri, qop, nc, cnonce, response, opaque));
                    }
                }

                ///Use the sessionId if present
                if (m_SessionId != null)
                {
                    request.SetHeader(RtspHeaders.Session, m_SessionId);
                }

                //Get the next Sequence Number
                request.CSeq = NextClientSequenceNumber();

                //Set the last method
                m_LastMethod = request.Method;

                //Get the bytes of the request
                byte[] buffer = m_RtspProtocol == ClientProtocolType.Http ? RtspRequest.ToHttpBytes(request) : request.ToBytes();

                //Erase last response
                m_LastRtspResponse = null;

                //If we are Interleaving we must recieve with respect with data which is being transportChanneld
                if (request.Method != RtspMethod.SETUP && request.Method != RtspMethod.TEARDOWN && request.Method != RtspMethod.PLAY && m_RtpClient != null && m_RtpClient.Connected && m_RtpClient.m_TransportProtocol != ProtocolType.Udp)
                {
                    //Reset the transportChannel event
                    m_InterleaveEvent.Reset();

                    //Assign an event for transportChanneld data before we write
                    m_RtpClient.InterleavedData += m_RtpClient_InterleavedData;
                    
                    int attempt = 1;

                Resend:
                    //Write the message on the transportChanneld socket
                    lock (m_RtspSocket)
                    {
                        //Send the data
                        m_RtspSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                    }

                    //Raise the event
                    Requested(request);

                    //Increment our byte counters for Rtsp
                    m_SentBytes += buffer.Length;

                    //Wait for the event as long we we are allowed, if we didn't recieve a response try again
                    if (!m_InterleaveEvent.WaitOne(ReadTimeout) && (m_RetryCount > 0 && ++attempt <= m_RetryCount)) goto Resend;

                    //Remove the event
                    m_RtpClient.InterleavedData -= m_RtpClient_InterleavedData;                    
                }
                else// If we are not yet interleaving or using Udp just use the socket
                {
                    lock (m_RtspSocket)
                    {
                        int attempt = 0;
                    Resend:
                        try
                        {

                            //Send the bytes
                            m_RtspSocket.Send(buffer);

                            //Fire the event
                            Requested(request);

                            //Increment our byte counters for Rtsp
                            m_SentBytes += buffer.Length;

                            m_RecievedBytes += m_RtspSocket.Receive(m_Buffer);

                            m_LastRtspResponse = new RtspResponse(m_Buffer);
                        }
                        catch
                        {
                            if (m_RetryCount > 0 && ++attempt <= m_RetryCount) goto Resend;
                            m_LastRtspResponse = null;
                        }
                    }
                }

                //We we have nothing to return
                if (m_LastRtspResponse == null) return m_LastRtspResponse;

                //Fire the event
                Received(request, m_LastRtspResponse);

                //Return the result
                return m_LastRtspResponse;
            }
            catch (RtspClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspClientException("An error occured during the request", ex);
            }
            finally
            {
                //Check for a SessionId or Updated unless this is a Teardown
                if (request.Method != RtspMethod.TEARDOWN && m_LastRtspResponse != null)
                {
                    //Check for SessionId if the response contains it
                    string sessionHeader = m_LastRtspResponse[RtspHeaders.Session];
                    //If there is a session header it may contain the option timeout
                    if (!string.IsNullOrEmpty(sessionHeader))
                    {
                        if (sessionHeader.Contains(';'))
                        {
                            //Get the values
                            string[] temp = sessionHeader.Split(';');

                            //Check for any values
                            if (temp.Length > 0)
                            {
                                //Get the SessionId if present
                                m_SessionId = temp[0].Trim();

                                //Check for a timeout
                                if (temp.Length > 1 && temp[1].StartsWith("timeout="))
                                {
                                    //If there is a timeout we may want to setup a timer on these seconds to send a GET_PARAMETER
                                    m_RtspTimeoutSeconds = Convert.ToInt32(temp[1].Replace("timeout=", string.Empty));
                                }
                            }
                        }
                        else
                        {
                            m_SessionId = sessionHeader;
                            m_RtspTimeoutSeconds = 60;
                        }
                    }
                }
            }
        }        

        public RtspResponse SendOptions()
        {
            RtspResponse response = SendRtspRequest(new RtspRequest(RtspMethod.OPTIONS, Location));

            if (response == null || response.StatusCode != RtspStatusCode.OK) throw new RtspClientException("Unable to get options");

            m_SupportedMethods.Clear();

            string publicMethods = response[RtspHeaders.Public];

            if (string.IsNullOrEmpty(publicMethods)) return response;

            foreach (string method in publicMethods.Split(','))
            {
                m_SupportedMethods.Add((RtspMethod)Enum.Parse(typeof(RtspMethod), method.Trim()));
            }

            //Should also store Supported:

            return response;
        }

        /// <summary>
        /// Assigns the SessionDescription returned from the server
        /// </summary>
        /// <returns></returns>
        public RtspResponse SendDescribe()
        {
            RtspRequest describe = new RtspRequest(RtspMethod.DESCRIBE, Location);
            describe.SetHeader(RtspHeaders.Accept, "application/sdp");

            RtspResponse response = SendRtspRequest(describe);

            if (response.StatusCode != RtspStatusCode.OK) throw new RtspClientException("Unable to describe media: " + response.m_FirstLine);

            try
            {
                m_SessionDescription = new Sdp.SessionDescription(response.Body);
            }
            catch (SessionDescriptionException ex)
            {
                throw new RtspClientException("Invalid Session Description", ex);
            }

            return response;
        }

        public RtspResponse SendTeardown(MediaDescription mediaDescription = null)
        {
            RtspResponse response = null;
            //Indicate we are stopping
            Stopping(mediaDescription);
            try
            {
                Uri location;

                if (mediaDescription != null)
                {
                    SessionDescriptionLine attributeLine = mediaDescription.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("control"))).FirstOrDefault();
                    location = new Uri(Location.OriginalString + '/' + attributeLine.Parts.Where(p => p.Contains("control")).FirstOrDefault().Replace("control:", string.Empty));
                }
                else
                {
                    location = Location;
                }

                return SendRtspRequest(new RtspRequest(RtspMethod.TEARDOWN, location));                
            }
            catch (RtspClient.RtspClientException)
            {
                return response;
            }
            catch
            {
                throw;
            }
            finally
            {
                m_SessionId = null;               
            }
        }

        public RtspResponse SendSetup(MediaDescription mediaDescription)
        {
            SessionDescriptionLine attributeLine = mediaDescription.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("control"))).FirstOrDefault();
            if(attributeLine == null)
            {
                throw new RtspClientException("Unable to find control directive in the given MediaDesription");
            }
            
            Uri location = new Uri(Location.OriginalString + '/' + attributeLine.Parts.Where(p => p.Contains("control")).FirstOrDefault().Replace("control:", string.Empty));

            //Send the setup
            return SendSetup(location, mediaDescription);
        }

        internal RtspResponse SendSetup(Uri location, MediaDescription mediaDescription, bool useMediaProtocol = true)//False to use manually set protocol
        {
            if (location == null) throw new ArgumentNullException("location");
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");
            try
            {
                RtspRequest setup = new RtspRequest(RtspMethod.SETUP, location ?? Location);

                //If we need to use Tcp
                if ((useMediaProtocol && mediaDescription.MediaProtocol.Contains("TCP")) || m_RtpProtocol == ProtocolType.Tcp)
                {
                    //Ask for an transportChannel
                    if (m_RtpClient != null && m_RtpClient.TransportContexts.Count > 0)
                    {
                        RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                        setup.SetHeader(RtspHeaders.Transport, "RTP/AVP/TCP;unicast;interleaved=" + (lastContext.DataChannel + 2) + '-' + (lastContext.ControlChannel + 2));
                    }
                    else
                    {
                        //Suppsed to be "TCP/RTP/AVP" as per RFC4751
                        setup.SetHeader(RtspHeaders.Transport, "RTP/AVP/TCP;unicast;interleaved=0-1");
                    }
                }
                else // We need to find an open Udp Port
                {
                    //Might want to reserver this port now by making a socket...
                    int openPort = Utility.FindOpenPort(ProtocolType.Udp, 30000, true); //Should allow this to be given or set as a property MinimumUdpPort, MaximumUdpPort

                    if (openPort == -1) throw new RtspClientException("Could not find open Udp Port");
                    //else if (MaximumUdp.HasValue && openPort > MaximumUdp)
                    //{
                        //Handle port out of range
                    //}    
                    setup.SetHeader(RtspHeaders.Transport, m_SessionDescription.MediaDescriptions[0].MediaProtocol + ";unicast;client_port=" + openPort + '-' + (openPort + 1));
                }

                //Get the response for the setup
                RtspResponse response = SendRtspRequest(setup);

                //Response not OK
                if (response.StatusCode != RtspStatusCode.OK)
                {
                    //Transport requested not valid
                    if (response.StatusCode == RtspStatusCode.UnsupportedTransport && m_RtpProtocol != ProtocolType.Tcp)
                    {
                        goto SetupTcp;
                    }
                    else if (response.StatusCode == RtspStatusCode.SessionNotFound)
                    {
                        //StopListening(); 
                        //return null;
                        SendTeardown();
                        m_SessionId = null;
                        return SendSetup(location, mediaDescription);
                    }
                    else
                    {
                        throw new RtspClientException("Unable to setup media: " + response.m_FirstLine);
                    }
                }

                string transportHeader = response[RtspHeaders.Transport];

#if DEBUG
                System.Diagnostics.Debug.WriteLine("RtspClient Got Response Transport Header: " + transportHeader);
#endif

                //We need a transportHeader with RTP
                if (string.IsNullOrEmpty(transportHeader) || !transportHeader.Contains("RTP")) throw new RtspClient.RtspClientException("Cannot setup media, Invalid Transport Header in Rtsp Response: " + transportHeader);

                //Get the parts of information from the transportHeader
                string[] parts = transportHeader.Split(';');

                //Values in the header we need
                int clientRtpPort = -1, clientRtcpPort = -1, serverRtpPort = -1, serverRtcpPort = -1, ssrc = 0;

                //Get the Ssrc cause we need it first and it sometimes comes at the end
                string ssrcPart = parts.Where(p => p.StartsWith("ssrc")).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(ssrcPart))
                {
                    //Get rid of the beginning
                    ssrcPart = ssrcPart.Replace("ssrc=", string.Empty).Trim();

                    if (!ssrcPart.StartsWith("0x") && !int.TryParse(ssrcPart, out ssrc)) //plain int                        
                        ssrc = int.Parse(ssrcPart, System.Globalization.NumberStyles.HexNumber); //hex
                }

                //If there are Bandwidth lines with RR:0 and RS:0
                IEnumerable<SessionDescriptionLine> rtcpLines = mediaDescription.Lines.Where(l => l.Type == 'b' && l.Parts.Count > 1 && (l.Parts[0] == "RR" || l.Parts[0] == "RS") && l.Parts[1] == "0");

                //Some providers disable Rtcp for one reason or another, it is strongly not recommended
                //If there are two lines which match the criteria then disable Rtcp
                //Rtcp is disabled, RtcpEnabled is the logic inverse of this (!rtcpDisabled)
                bool rtcpDisabled = rtcpLines != null && rtcpLines.Count() == 2;

                //Get the source, we need it first and sometimes it comes at the end
                string sourcePart = parts.Where(p => p.StartsWith("source")).FirstOrDefault();

                //Cache this to prevent having to go to get it every time down the line
                IPAddress sourceIp = null;

                if (!string.IsNullOrWhiteSpace(sourcePart))
                {
                    //Get rid of the beginning
                    sourcePart = sourcePart.Replace("source=", string.Empty).Trim();

                    //Try it
                    IPAddress.TryParse(sourcePart, out sourceIp);
                }

                //Ensure not null
                if (sourceIp == null)
                {
                    sourceIp = ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address;
                }
                
                ///The transport header contains the following information 
                foreach (string part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;
                    else if (part == "unicast" || part.StartsWith("source") || part.StartsWith("ssrc")) { continue; }
                    
                    //Handle the ones we need as they occur
                    if (part.Equals("RTP/AVP")) m_RtpProtocol = ProtocolType.Udp;
                    else if (part.Equals("UDP/RTP/AVP") || part.Equals("RTP/AVP/UDP")) m_RtpProtocol = ProtocolType.Udp;
                    else if (part.Equals("TCP/RTP/AVP") || part.Equals("RTP/AVP/TCP")) m_RtpProtocol = ProtocolType.Tcp;
                    else if (part == "multicast")
                    {
                        //Todo Implement multicast send, recieve
                    }
                    else if (part.StartsWith("client_port="))
                    {
                        string[] clientPorts = part.Replace("client_port=", string.Empty).Split('-');
                        if (clientPorts.Length > 1)
                        {
                            clientRtpPort = int.Parse(clientPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                            clientRtcpPort = int.Parse(clientPorts[1], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            throw new RtspClientException("Server indicated Udp but did not provide a port pair: " + transportHeader);
                        }

                    }
                    else if (part.StartsWith("mode="))
                    {
                        //PLAY, ....
                        m_TransportMode = part.Replace("mode=", string.Empty).Trim();
                    }
                    else if (part.StartsWith("interleaved="))
                    {
                        //Should only be for Tcp
                        string[] channels = part.Replace("interleaved=", string.Empty).Split('-');
                        if (channels.Length > 1)
                        {
                            RtpClient.TransportContext transportContext = new RtpClient.TransportContext(byte.Parse(channels[0], System.Globalization.CultureInfo.InvariantCulture), byte.Parse(channels[1], System.Globalization.CultureInfo.InvariantCulture), (uint)ssrc, mediaDescription, m_RtspSocket, !rtcpDisabled);

                            //If there is not a client
                            if (m_RtpClient == null)
                            {
                                //Create a Interleaved reciever
                                m_RtpClient = RtpClient.Interleaved(m_RtspSocket);
                            }

                            try
                            {
                                //try to add the transportChannel
                                m_RtpClient.AddTransportContext(transportContext);
                                
                                //and initialize the client from the RtspSocket
                                transportContext.InitializeSockets(m_RtspSocket);
                            }
                            catch
                            {
                                throw new RtspClientException("Server responded with channel already in use: " + transportHeader);
                            }
                        }
                        else
                        {
                            throw new RtspClientException("Server indicated Tcp Transport but did not provide a channel pair: " + transportHeader);
                        }
                    }
                    else if (part.StartsWith("server_port="))
                    {
                        string[] serverPorts = part.Replace("server_port=", string.Empty).Split('-');

                        //This is not in any RFC including 2326
                        //If there is not a port pair then this must be a tcp response unless the server is duplexing rtp and rtcp
                        if (serverPorts.Length == 1)
                        {
                            //Duplexing?
                            serverRtpPort = serverRtcpPort = int.Parse(serverPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                            //Check if the port is 554 which means they must want Interleaved?
                            if (serverRtpPort == 554) goto SetupTcp;
                        }
                        else
                        {
                            //Parse the ports
                            serverRtpPort = int.Parse(serverPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                            serverRtcpPort = int.Parse(serverPorts[1], System.Globalization.CultureInfo.InvariantCulture);

                            //Handle duplexing....
                            if (serverRtpPort == serverRtcpPort)
                            {
                                //Duplexing....
                            }

                            //If we need to make a client then do so
                            if (m_RtpClient == null)
                            {
                                if (m_RtpProtocol == ProtocolType.Udp)
                                {
                                    //Create a Udp Reciever
                                    m_RtpClient = RtpClient.Receiever(m_RemoteIP);
                                }                                
                            }
                            
                            //Add the transportChannel for the mediaDescription
                            if(m_RtpClient.TransportContexts.Count == 0)
                            {
                                RtpClient.TransportContext newContext = new RtpClient.TransportContext(0, 1, (uint)ssrc, mediaDescription, !rtcpDisabled);
                                newContext.InitializeSockets(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.AddTransportContext(newContext);
                            }
                            else
                            {
                                RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                                RtpClient.TransportContext nextContext = new RtpClient.TransportContext((byte)(lastContext.DataChannel + 2), (byte)(lastContext.ControlChannel + 2), (uint)ssrc, mediaDescription, !rtcpDisabled);
                                nextContext.InitializeSockets(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.AddTransportContext(nextContext);
                            }
                        }
                    }
#if DEBUG
                    else //The part is not handled
                    {

                        System.Diagnostics.Debug.WriteLine("Unhandled Rtsp Response Transport Header Part: " + part);
                    }
#endif
                }               

                //Connect and wait for Packets
                m_RtpClient.Connect();

                return response;
            }
            catch (RtspClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspClientException("Unable not setup media: ", ex);
            }
            //Setup for Interleaved
        SetupTcp:
            {
                Client.m_SocketOwner = false;
                Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Tcp;

                //Clear existing transportChannels
                m_RtpClient.TransportContexts.Clear();

                //Recurse call to ensure propper setup
                return SendSetup(location, mediaDescription);
            }
        }

        internal void SwitchProtocols(object state = null)
        {
            //If there is no socket or the protocol was forced return
            if (m_RtspSocket == null && m_ForcedProtocol) return;

            //If the client has not recieved any bytes and we have not already switched to Tcp
            else if (m_RtpProtocol != ProtocolType.Tcp && Client.TotalRtpBytesReceieved <= 0)
            {
                //Reconnect without losing the events on the RtpClient
                Client.m_SocketOwner = false;
                Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Tcp;

                //Disconnect to allow the server to reset state
                Disconnect();

                //Clear existing transportChannels
                m_RtpClient.TransportContexts.Clear();

                //Start again
                StartListening();
            }
            else if (m_RtpProtocol == ProtocolType.Tcp)
            {
                //Switch back to Udp?
                throw new NotImplementedException("Switch from Tcp to Udp Not Implemented.");

                //Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Udp;

                ////Disconnect to allow the server to reset state
                //Disconnect();

                ////Clear existing transportChannels
                //m_RtpClient.TransportContexts.Clear();

                ////Start again
                //StartListening();
            }
        }

        public RtspResponse SendPlay(Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "ntp", string rangeFormat = null)
        {
            try
            {
                RtspRequest play = new RtspRequest(RtspMethod.PLAY, location ?? Location);

                play.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startTime, endTime, rangeType, rangeFormat));

                RtspResponse response = SendRtspRequest(play);

                if (response.StatusCode != RtspStatusCode.OK) throw new RtspClientException("Unable to play media: " + response.m_FirstLine);

                string rtpInfo = response[RtspHeaders.RtpInfo];

                //If needed and given
                int startRtpSequence = -1;

                //should throw not found RtpInfo
                if (!string.IsNullOrEmpty(rtpInfo))
                {
                    string[] pieces = rtpInfo.Split(',');
                    foreach (string piece in pieces)
                    {
                        if (piece.Trim().StartsWith("url="))
                        {
                            //Location = new Uri(piece.Replace("url=", string.Empty).Trim());
                        }
                        else if (piece.Trim().StartsWith("seqno="))
                        {
                            startRtpSequence = Convert.ToInt32(piece.Replace("seqno=", string.Empty).Trim());
                        }
#if DEBUG
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("RtspClient Encountered unhandled Rtp-Info part: " + piece);
                        }
#endif
                    }
                }

                string rangeString = response[RtspHeaders.Range];

                //Should throw if RtpInfo was not present
                if (!string.IsNullOrEmpty(rangeString))
                {                    
                    string[] times = rangeString.Trim().Split('=');
                    if(times.Length > 1)
                    {
                        //Determine Format
                        if (times[0] == "npt")//ntp=1.060-20
                        {
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);
                            if (times[0].ToLowerInvariant() == "now") m_Live = true;
                            else if (times.Length == 1)
                            {
                                if(times[0].Contains(':'))
                                {
                                    m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    m_StartTime = TimeSpan.FromSeconds(double.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                                }
                                //Only start is live?
                                m_Live = true;
                            }
                            else if (times.Length == 2)
                            {
                                if (times[0].Contains(':'))
                                {
                                    m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    m_EndTime = TimeSpan.Parse(times[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    m_StartTime = TimeSpan.FromSeconds(double.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                                    m_EndTime = TimeSpan.FromSeconds(double.Parse(times[1].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                                }
                            }
                            else throw new RtspClientException("Invalid Range Header Received: " + rangeString);
                        }
                        else if (times[0] == "smpte")//smpte=0:10:20-;time=19970123T153600Z
                        {
                            //Get the times into the times array skipping the time from the server (order may be first so I explicitly did not use Substring overload with count)
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries).Where(s=> !s.StartsWith("time=")).ToArray();
                            if (times[0].ToLowerInvariant() == "now") m_Live = true;
                            else if (times.Length == 1)
                            {
                                m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                //Only start is live?
                                m_Live = true;
                            }
                            else if (times.Length == 2)
                            {
                                m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                m_EndTime = TimeSpan.Parse(times[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else throw new RtspClientException("Invalid Range Header Received: " + rangeString);
                        }
                        else if (times[0] == "clock")//clock=19961108T142300Z-19961108T143520Z
                        {
                            //Get the times into times array
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);
                            //Check for live
                            if (times[0].ToLowerInvariant() == "now") m_Live = true;
                            //Check for start time only
                            else if (times.Length == 1)
                            {
                                DateTime now = DateTime.UtcNow, startDate;
                                ///Parse and determine the start time
                                if (DateTime.TryParse(times[0].Trim(), out startDate))
                                {
                                    //Time in the past
                                    if (now > startDate) m_StartTime = now - startDate;
                                    //Future?
                                    else m_StartTime = startDate - now;
                                }
                                //Only start is live?
                                m_Live = true;
                            }
                            else if (times.Length == 2)
                            {
                                DateTime now = DateTime.UtcNow, startDate, endDate;
                                ///Parse and determine the start time
                                if (DateTime.TryParse(times[0].Trim(), out startDate))
                                {
                                    //Time in the past
                                    if (now > startDate) m_StartTime = now - startDate;
                                    //Future?
                                    else m_StartTime = startDate - now;
                                }
                                
                                ///Parse and determine the end time
                                if (DateTime.TryParse(times[1].Trim(), out endDate))
                                {
                                    //Time in the past
                                    if (now > endDate) m_EndTime = now - endDate;
                                    //Future?
                                    else m_EndTime = startDate - now;
                                }
                            }
                            else throw new RtspClientException("Invalid Range Header Received: " + rangeString);
                        }
                        
                    }
                }

                //If there is a timeout ensure it gets utilized
                if (m_RtspTimeoutSeconds != 0 && m_KeepAliveTimer == null)
                {
                    //Use half the timeout to protect against dialation
                    m_KeepAliveTimer = new Timer(new TimerCallback(SendKeepAlive), null, m_RtspTimeoutSeconds * 1000 / 2, m_RtspTimeoutSeconds * 1000 / 2);
                }

                //Connect the client (if already connected this will not do anything, might want to change the semantic though)
                m_RtpClient.Connect();

                //Raise the playing event
                Playing();

                return response;
            }
            catch (RtspClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspClientException("Unable to play media", ex);
            }
            finally
            {
                //Setup a timer to determine if we are recieving data... if we are then then we must switch
                if (m_RtpClient != null && m_RtpProtocol == ProtocolType.Udp)
                {
                    //If we have a timeout to switch the protocols and the protocol has not been forced
                    if (m_ProtocolSwitchSeconds > 0 && !m_ForcedProtocol)
                    {
                        //Setup a timer, should be accessible from the instance...
                        m_ProtocolSwitchTimer = new System.Threading.Timer(new TimerCallback(SwitchProtocols), null, m_ProtocolSwitchSeconds * 1000, System.Threading.Timeout.Infinite);
                    }
                }
            }
        }

        internal void SendKeepAlive(object state)
        {
            try 
            {
                //Darwin DSS and other servers might not support GET_PARAMETER
                if (m_SupportedMethods.Contains(RtspMethod.GET_PARAMETER))
                {
                    SendGetParameter(null);
                }
                else if (m_SupportedMethods.Contains(RtspMethod.OPTIONS)) //If at least options is supported
                {
                    SendOptions();
                }

                bool total = false;

                //Check all channels to ensure there is flowing information
                Client.TransportContexts.ForEach(c =>
                {
                    //If there is not on the one which is not flowing we have to tear it down with its track name and then perform setup again while the media is still running on other transportChannels
                    if (c.RtpBytesRecieved <= 0 && !total)
                    {
                        try
                        {
                            //Just one stream gets torn down
                            SendTeardown(c.MediaDescription);

                            System.Threading.Thread.Sleep(ReadTimeout);

                            //Setup 
                            SendSetup(c.MediaDescription);

                            //And hopefully played
                            SendPlay();
                        }
                        catch
                        {
                            //Indicate total so we don't try again if this happens the first time
                            total = true;

                            System.Threading.Thread.Sleep(ReadTimeout);

                            //The server might not support disconnecting a single stream so stop all of them and try again
                            Disconnect();

                            //Start again
                            StartListening();
                        }
                    }
                });
            }
            catch
            {
                if (m_KeepAliveTimer != null)
                {
                    m_KeepAliveTimer.Dispose(); 
                    m_KeepAliveTimer = null;
                }
            }
        }

        public RtspResponse SendGetParameter(string body = null)
        {
            try
            {
                RtspRequest get = new RtspRequest(RtspMethod.GET_PARAMETER, Location);
                get.Body = body;
                RtspResponse response = SendRtspRequest(get);
                return response;
            }
            catch (RtspClientException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopListening();
        }

        #endregion
    }
}
