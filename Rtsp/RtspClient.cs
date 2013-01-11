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
    /// Communicates with an RtspServer to setup a RtpClient.    
    /// </summary>
    public class RtspClient
    {
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

        ClientProtocolType m_RtspProtocol;

        ManualResetEvent m_InterleaveEvent = new ManualResetEvent(false);

        RtspResponse m_LastRtspResponse;

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

        string m_UserAgent = "ASTI RTP Client", m_SessionId, m_TransportMode, m_Range;

        internal RtpClient m_RtpClient;

        Timer m_KeepAliveTimer, m_ProtocolSwitchTimer;

        #endregion

        #region Properties

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
        public ClientProtocolType RtspProtocol { get { return m_RtspProtocol; } }

        /// <summary>
        /// The location to the Media on the Rtsp Server
        /// </summary>
        public Uri Location
        {
            get { return m_Location; }
            internal set
            {
                try
                {
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
        public RtspClient(Uri location)
        {
            if (!location.IsAbsoluteUri) throw new ArgumentException("Must be absolute", "location");
            if (!(location.Scheme == RtspMessage.ReliableTransport || location.Scheme == RtspMessage.UnreliableTransport || location.Scheme == System.Uri.UriSchemeHttp)) throw new ArgumentException("Uri Scheme must be rtsp or rtspu or http", "location");

            Location = location;

            OnRequest += RtspClient_OnRequest;
            OnResponse += RtspClient_OnResponse;
            OnDisconnect += RtspClient_OnDisconnect;
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        public RtspClient(string location) : this(new Uri(location)) { }

        ~RtspClient()
        {
            OnRequest -= RtspClient_OnRequest;
            OnResponse -= RtspClient_OnResponse;
            OnDisconnect -= RtspClient_OnDisconnect;
            StopListening();
            if (m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();
        }

        #endregion

        #region Events

        public delegate void ConnectHandler(RtspClient sender);

        public delegate void DisconnectHandler(RtspClient sender);

        public delegate void RequestHandler(RtspClient sender, RtspRequest request);

        public delegate void ResponseHandler(RtspClient sender, RtspResponse response);

        public event ConnectHandler OnConnect;

        internal void OnConnected() { if (OnConnect != null) OnConnect(this); }

        public event RequestHandler OnRequest;

        internal void Requested(RtspRequest request) { if (OnRequest != null) OnRequest(this, request); }

        public event ResponseHandler OnResponse;

        internal void Received(RtspResponse response) { if (OnResponse != null) OnResponse(this, response); }

        public event DisconnectHandler OnDisconnect;

        internal void OnDisconnected() { if (OnDisconnect != null) OnDisconnect(this); }

        #endregion

        #region Methods

        void RtspClient_OnResponse(RtspClient sender, RtspResponse response)
        {
            //No Op
        }

        void RtspClient_OnRequest(RtspClient sender, RtspRequest request)
        {
            //No Op
        }

        void RtspClient_OnDisconnect(RtspClient sender)
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
                System.Diagnostics.Debug.WriteLine(System.Text.Encoding.ASCII.GetString(slice.Array));
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
                //Connect the socket
                Connect();
                //Send the options
                SendOptions();

                //If we can describe
                if (m_SupportedMethods.Contains(RtspMethod.DESCRIBE))
                {
                    //Send describe
                    SendDescribe();
                }

                int attempt = 1;

                //If we can setup
                if (m_SupportedMethods.Contains(RtspMethod.SETUP))
                {
                    //For each MediaDescription in the SessionDecscription
                    foreach (var md in SessionDescription.MediaDescriptions)
                    {
                    //Send the setup
                    SendSetup:
                        try
                        {
                            //Send a setup
                            SendSetup(md);
                        }
                        catch
                        {
                            //Sometimes this happened to fast during a reconnect
                            if (m_RetryCount > 0 && ++attempt <= m_RetryCount) goto SendSetup;
                            //If we eventually get through we will likely result in Tcp Mode
                            //I would just switch protocol before this point in a re-connect but the source might not support Tcp
                            //and it's best to leave that for a last cast which occurs after the play times out with the ProtocolSwitchTimer
                        }
                    }
                }

                //If we can play
                if (m_SupportedMethods.Contains(RtspMethod.PLAY))
                {
                    //Send the play
                    SendPlay();
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
                //Get rid of the timer
                if (m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();

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

                if (Credential != null)
                {
                    //Encoding should be a property on the Listener which defaults to utf8
                    request.SetHeader(RtspHeaders.Authroization, "Basic " + Convert.ToBase64String(request.Encoding.GetBytes(Credential.UserName + ':' + Credential.Password)));
                }

                if (m_SessionId != null)
                {
                    request.SetHeader(RtspHeaders.Session, m_SessionId);
                }

                request.CSeq = NextClientSequenceNumber();

                m_LastMethod = request.Method;

                byte[] buffer = m_RtspProtocol == ClientProtocolType.Http ? RtspRequest.ToHttpBytes(request) : request.ToBytes();

                //Erase last response
                m_LastRtspResponse = null;

                //If we are Interleaving we must recieve with respect with data which is being interleaved
                if (request.Method != RtspMethod.SETUP && request.Method != RtspMethod.TEARDOWN && request.Method != RtspMethod.PLAY && m_RtpClient != null && m_RtpClient.Connected && m_RtpClient.m_TransportProtocol != ProtocolType.Udp)
                {
                    //Reset the interleave event
                    m_InterleaveEvent.Reset();

                    //Assign an event for interleaved data before we write
                    m_RtpClient.InterleavedData += m_RtpClient_InterleavedData;
                    
                    int attempt = 1;

                Resend:
                    //Write the message on the interleaved socket
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
                Received(m_LastRtspResponse);

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
            
            //Create the location ('/' was '?') but apparently '/' is correct per RFC2326
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
                    //Ask for an interleave
                    if (m_RtpClient.Interleaves.Count > 0)
                    {
                        var lastInterleave = m_RtpClient.Interleaves.Last();
                        setup.SetHeader(RtspHeaders.Transport, "RTP/AVP/TCP;unicast;interleaved=" + (lastInterleave.DataChannel + 2) + '-' + (lastInterleave.ControlChannel + 2));
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
                IEnumerable<SessionDescriptionLine> rtcpLines = mediaDescription.Lines.Where(l => l.Type == 'b' && l.Parts.Count > 1 && (l.Parts[0] == "RR" || l.Parts[0] == "RS") && l.Parts[1] == "1");

                //Some providers disable Rtcp for one reason or another, it is strongly not recommended
                bool rtcpDisabled = false;

                //If there are two lines which match the criteria then disable Rtcp
                if (rtcpLines != null && rtcpLines.Count() == 2)
                {             
                    //Rtcp is disabled, RtcpEnabled is the logic inverse of this (!rtcpDisabled)
                    rtcpDisabled = true;
                }

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
                            clientRtpPort = int.Parse(clientPorts[0]);
                            clientRtcpPort = int.Parse(clientPorts[1]);
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
                    else if (part.StartsWith("interleaved"))
                    {
                        //Should only be for Tcp
                        string[] channels = part.Replace("interleaved=", string.Empty).Split('-');
                        if (channels.Length > 1)
                        {
                            m_RtpClient.AddInterleave(new RtpClient.Interleave(byte.Parse(channels[0]), byte.Parse(channels[1]), (uint)ssrc, mediaDescription));
                            m_RtpClient.InitializeFrom(m_RtspSocket);
                        }
                        else
                        {
                            throw new RtspClientException("Server indicated Tcp but did not provide a channel pair: " + transportHeader);
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
                            serverRtpPort = serverRtcpPort = int.Parse(serverPorts[0]);
                            //Check if the port is 554 which means they must want Interleaved?
                            if (serverRtpPort == 554) goto SetupTcp;
                        }
                        else
                        {
                            //Parse the ports
                            serverRtpPort = int.Parse(serverPorts[0]);
                            serverRtcpPort = int.Parse(serverPorts[1]);

                            //Handle duplexing....
                            if (serverRtpPort == serverRtcpPort)
                            {
                                //Duplexing....
                            }

                            //If we need to make a client then do so
                            if (m_RtpClient == null)
                            {
                                //Create a reciever
                                m_RtpClient = RtpClient.Receiever(m_RemoteIP);
                            }
                            
                            //Add the interleave for the mediaDescription
                            if(m_RtpClient.Interleaves.Count == 0)
                            {
                                RtpClient.Interleave newInterleave = new RtpClient.Interleave(0, 1, (uint)ssrc, mediaDescription);
                                newInterleave.RtcpEnabled = !rtcpDisabled;
                                newInterleave.InitializeSockets(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.AddInterleave(newInterleave);
                            }
                            else
                            {
                                var last = m_RtpClient.Interleaves.Last();
                                RtpClient.Interleave newInterleave = new RtpClient.Interleave((byte)(last.DataChannel + 2), (byte)(last.ControlChannel + 2), (uint)ssrc, mediaDescription);
                                newInterleave.RtcpEnabled = !rtcpDisabled;
                                newInterleave.InitializeSockets(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.AddInterleave(newInterleave);
                            }
                        }
                    }
#if DEBUG
                    else //The part is not needed
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
                //Reconnect without losing the events on the RtpClient
                m_RtpProtocol = ProtocolType.Tcp;
                m_RtpClient.InitializeFrom(m_RtspSocket);

                //Clear existing interleaves
                m_RtpClient.Interleaves.Clear();

                //Recurse call to ensure propper setup
                return SendSetup(location, mediaDescription);
            }
        }

        internal void SwitchProtocols(object state = null)
        {
            if (m_RtspSocket == null) return;
            //If the client has not recieved any bytes and we have not already switched to Tcp
            if (m_RtpProtocol != ProtocolType.Tcp && Client.TotalRtpBytesReceieved <= 0) //m_RtpClient.Interleaves.All(i => i.RtpBytesRecieved >= 0))
            {
                //Reconnect without losing the events on the RtpClient
                m_RtpProtocol = ProtocolType.Tcp;
                m_RtpClient.InitializeFrom(m_RtspSocket);

                //Disconnect to allow the server to reset state
                Disconnect();

                //Clear existing interleaves
                m_RtpClient.Interleaves.Clear();

                //Start again
                StartListening();
            }
            else if (m_RtpProtocol == ProtocolType.Tcp)
            {
                //Switch back to Udp?
            }

            //Remove the timer
            m_ProtocolSwitchTimer.Dispose();
            m_ProtocolSwitchTimer = null;
        }

        public RtspResponse SendPlay(Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            try
            {
                RtspRequest play = new RtspRequest(RtspMethod.PLAY, location ?? Location);

                //Inlclude a range so we get back a range
                //play.AppendOrSetHeader(RtspHeaders.Range,"npt=0.000-");
                string rangeHeader = "npt=";

                if (startTime.HasValue) rangeHeader += startTime.Value.ToString("0.000");
                else rangeHeader += "0.000-";

                if (endTime.HasValue) rangeHeader += endTime.Value.ToString("0.000");

                play.SetHeader(RtspHeaders.Range, rangeHeader);

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
                        else
                        {
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("RtspClient Encountered unhandled Rtp-Info part: " + piece);
#endif
                        }
                    }
                }

                //When the client plays 
                //if npt=now- (Stream in continious)
                //else if npt=0.000-x.xxx (stream needs to be cached so it can be played back from the server)

                string rangeString = response[RtspHeaders.Range];

                //Should throw if RtpInfo was present, Range requried RtpInfo
                if (!string.IsNullOrEmpty(rangeString))
                {
                    m_Range = rangeString.Trim();
                }

                //If there is a timeout ensure it gets utilized
                if (m_RtspTimeoutSeconds != 0 && m_KeepAliveTimer == null)
                {
                    m_KeepAliveTimer = new Timer(new TimerCallback(SendGetParameter), null, m_RtspTimeoutSeconds * 1000 / 2, m_RtspTimeoutSeconds * 1000 / 2);
                }

                //Connect the client (if already connected this will not do anything, might want to change the semantic though)
                m_RtpClient.Connect();

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
                    if (m_ProtocolSwitchSeconds > 0)
                    {
                        //Setup a timer, should be accessible from the instance...
                        m_ProtocolSwitchTimer = new System.Threading.Timer(new TimerCallback(SwitchProtocols), null, m_ProtocolSwitchSeconds * 1000, System.Threading.Timeout.Infinite);
                    }
                }
            }
        }

        internal void SendGetParameter(object state)
        {
            try 
            { 
                SendGetParameter(null);

                bool total = false;

                //Check all channels to ensure there is flowing information
                Client.Interleaves.ForEach(i =>
                {
                    //If there is not on the one which is not flowing we have to tear it down with its track name and then perform setup again while the media is still running on other interleaves
                    if (i.RtpBytesRecieved <= 0 && !total)
                    {
                        try
                        {
                            //Just one stream gets torn down
                            SendTeardown(i.MediaDescription);

                            System.Threading.Thread.Sleep(ReadTimeout);

                            //Setup 
                            SendSetup(i.MediaDescription);

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
    }
}
