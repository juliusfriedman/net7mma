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
    public class RtspClient : Common.BaseDisposable, Media.Common.ISocketOwner
    {
        internal static char[] TimeSplit = new char[] { '-', ';' };

        internal static char[] SpaceSplit = new char[] { ' ', ',' };

        internal static string[] Colon = new string[] { ":" };

        internal static char EQ = '=';

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

        bool m_ForcedProtocol;

        ClientProtocolType m_RtspProtocol;

        ManualResetEventSlim m_InterleaveEvent = new ManualResetEventSlim(false);

        RtspMessage m_LastTransmitted;

        AuthenticationSchemes m_AuthenticationScheme;

        /// <summary>
        /// The location the media
        /// </summary>
        Uri m_Location;

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

        TimeSpan m_RtspTimeout;

        /// <summary>
        /// Need to seperate counters and other stuff
        /// </summary>
        int m_SentBytes, m_ReceivedBytes,
             m_RtspPort, m_CSeq,
            m_RetryCount = 5;

        HashSet<RtspMethod> m_SupportedMethods = new HashSet<RtspMethod>();

        string m_UserAgent = "ASTI RTP Client", m_SessionId, m_TransportMode;

        internal RtpClient m_RtpClient;

        Timer m_KeepAliveTimer, m_ProtocolSwitchTimer;

        bool m_Playing = false;

        TimeSpan? m_StartTime, m_EndTime;

        DateTime? m_StartedListening;

        #endregion

        #region Properties

        /// <summary>
        /// Any additional headers which may be required by the RtspClient.
        /// </summary>
        public readonly Dictionary<string, string> AdditionalHeaders = new Dictionary<string, string>();

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
        public bool LivePlay { get { return !m_EndTime.HasValue; } }

        /// <summary>
        /// True if the RtspClient has received the Playing event, False if the RtspClient has received the Stopping event or otherwise such as the media has finished playing.
        /// </summary>
        public bool Playing { get { return Connected && (m_StartedListening != null && LivePlay ? m_Playing : m_EndTime.HasValue ? (DateTime.UtcNow - m_StartedListening < m_EndTime.Value) : m_Playing); } }

        public DateTime? StartedListening { get { return m_StartedListening; } }

        /// <summary>
        /// The amount of time in seconds in which the RtspClient will switch protocols if no Packets have been recieved.
        /// </summary>
        //public int ProtocolSwitchTime   { get { return SocketReadTimeout; } set { SocketReadTimeout = value; if (m_ProtocolSwitchTimer != null) if (SocketReadTimeout == 0) m_ProtocolSwitchTimer.Dispose(); else m_ProtocolSwitchTimer.Change(m_LastTransmitted != null ? (int)(1000 * (SocketReadTimeout - (DateTime.Now - m_LastTransmitted.Created).TotalSeconds)) : SocketReadTimeout, SocketReadTimeout); } }
        public TimeSpan ProtocolSwitchTime
        {
            //When this property is accessed it should be checking for the flow of all streams, if one is not flowing it should be torn down and re connected if possible.
            //See Send Keep Alive
            get { return  TimeSpan.FromMilliseconds(SocketReadTimeout); }
            set
            {
                int truncatedTotalSeconds = (int)value.TotalSeconds;
                SocketReadTimeout = truncatedTotalSeconds;
                if (m_ProtocolSwitchTimer != null)
                    if (truncatedTotalSeconds == 0) 
                        m_ProtocolSwitchTimer.Dispose(); 
                    else
                        m_ProtocolSwitchTimer.Change(value, value);
            }
        }
            

        /// <summary>
        /// The amount of time in seconds the KeepAlive request will be sent to the server after connected.
        /// If a GET_PARAMETER request is not supports OPTIONS will be sent instead.
        /// </summary>
        public TimeSpan KeepAliveTimeout { get { return m_RtspTimeout; }
            set
            {
                m_RtspTimeout = value;
                
                if (m_RtspTimeout <= TimeSpan.Zero)
                {
                    if (m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();
                    m_KeepAliveTimer = null;
                }
                
                if (m_KeepAliveTimer != null) m_KeepAliveTimer.Change(m_LastTransmitted != null ? (m_RtspTimeout - (DateTime.UtcNow - m_LastTransmitted.Created)) : m_RtspTimeout, m_RtspTimeout);

                //Update the timer period
                
            }
        }

        /// <summary>
        /// The amount of times each RtspRequest will be sent if a response is not recieved in ReadTimeout
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }

        //The last RtspMessage transmittted by the RtspClient (Sent or Received)
        public RtspMessage LastTransmitted { get { return m_LastTransmitted; } }

        /// <summary>
        /// The ClientProtocolType the RtspClient is using Reliable (Tcp), Unreliable(Udp) or Http(Tcp)
        /// </summary>
        public ClientProtocolType RtspProtocol { get { return m_RtspProtocol; } set { m_RtspProtocol = value; } }

        /// <summary>
        /// The ProtocolType the RtspClient will setup for underlying RtpClient.
        /// </summary>
        public ProtocolType RtpProtocol { get { return m_RtpProtocol; } set { if (value != ProtocolType.Udp && value != ProtocolType.Tcp) throw new ArgumentException(); m_RtpProtocol = value; } }

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

                        bool wasPlaying = Playing;

                        if (wasPlaying) StopPlaying();

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

                        if (wasPlaying) StartPlaying();
                    }
                }
                catch (Exception ex)
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(this, "Could not resolve host from the given location. See InnerException.", ex);
                }
            }
        }

        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        public bool Connected { get { return m_RtspSocket != null && m_RtspSocket.Connected; } }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential { get; set; }

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
        /// Gets or Sets the ReadTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketReadTimeout { get { return m_RtspSocket.ReceiveTimeout; } set { m_RtspSocket.ReceiveTimeout = value; } }

        /// <summary>
        /// Gets or Sets the WriteTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketWriteTimeout { get { return m_RtspSocket.SendTimeout; } set { m_RtspSocket.SendTimeout = value; } }

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

            if (!UriParser.IsKnownScheme(RtspMessage.UnreliableTransport))
                UriParser.Register(new HttpStyleUriParser(), RtspMessage.SecureTransport, 322);
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize, if null it will be determined from the location Scheme</param>
        /// <param name="bufferSize">The amount of bytes the client will use during message reception, Must be at least 4096 and if larger it will also be shared with the underlying RtpClient</param>
        public RtspClient(string location, ClientProtocolType? rtpProtocolType = null, int bufferSize = 8192)
            : this(new Uri(location), rtpProtocolType, bufferSize)
        {
        }

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The absolute location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize and will not deviate from the protocol is no data is received, if null it will be determined from the location Scheme</param>
        public RtspClient(Uri location, ClientProtocolType? rtpProtocolType = null, int bufferSize = 8192)
        {
            if (!location.IsAbsoluteUri) throw new ArgumentException("Must be absolute", "location");
            if (!(location.Scheme == RtspMessage.ReliableTransport || location.Scheme == RtspMessage.UnreliableTransport || location.Scheme == System.Uri.UriSchemeHttp)) throw new ArgumentException("Uri Scheme must be rtsp or rtspu or http", "location");

            //Set the location and determines the m_RtspProtocol
            Location = location;

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

            if (bufferSize < RtspMessage.MaximumLength) throw new ArgumentOutOfRangeException("Buffer size must be at least RtspMessage.MaximumLength (4096)");
            m_Buffer = new Common.MemorySegment(bufferSize);
        }

        

        ~RtspClient()
        {
            if (m_RtpClient != null) m_RtpClient.InterleavedData -= ProcessInterleaveData;
        }

        #endregion

        #region Events

        public delegate void RtspClientAction(RtspClient sender, object args);

        public delegate void RequestHandler(RtspClient sender, RtspMessage request);

        public delegate void ResponseHandler(RtspClient sender, RtspMessage request, RtspMessage response);

        public event RtspClientAction OnConnect;

        internal void OnConnected() { if (OnConnect != null) OnConnect(this, EventArgs.Empty); }

        public event RequestHandler OnRequest;

        internal void Requested(RtspMessage request) { if (OnRequest != null) OnRequest(this, request); }

        public event ResponseHandler OnResponse;

        internal void Received(RtspMessage request, RtspMessage response) { if (OnResponse != null) OnResponse(this, request, response); }

        public event RtspClientAction OnDisconnect;

        internal void OnDisconnected() { m_Playing = false; if (OnDisconnect != null) OnDisconnect(this, EventArgs.Empty); }

        public event RtspClientAction OnPlay;

        internal void OnPlaying() { m_StartedListening = DateTime.UtcNow; m_Playing = true; if (OnPlay != null) OnPlay(this, EventArgs.Empty); }

        public event RtspClientAction OnStop;

        internal void OnStopping(MediaDescription mediaDescription = null) { m_Playing = (mediaDescription != null); if (OnStop != null) OnStop(this, mediaDescription); }

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
        /// Handles Interleaved Data for the RtspClient by parsing the given memory for a valid RtspMessage.
        /// </summary>
        /// <param name="sender">The RtpClient instance which called this method</param>
        /// <param name="memory">The memory to parse</param>
        void ProcessInterleaveData(object sender, byte[] data, int offset, int length)
        {
            //Cache offset and count, leave a register for received data (should be calulated with length)
            int received = 0;

            //Check for letter
            if (char.IsLetter((char)data[offset]))//Utility.ContainsBytes(memory.Array, ref offset, ref sliceCount, (Common.ASCII.LineFeed.Yield().Concat(Common.ASCII.NewLine.Yield())).ToArray(), 0, 2) >= 0)  //Utility.FoundValidUniversalTextFormat(memory.Array, ref offset, ref sliceCount))
            {
                //Validate the data
                RtspMessage interleaved = new RtspMessage(data, offset, length);

                //Determine what to do with the interleaved message
                switch (interleaved.MessageType)
                {
                    //If the message is invalid 
                    case RtspMessageType.Invalid:
                        {
                            goto SetEvent;
                        }
                    case RtspMessageType.Request: //Event for pushed messages?
                    case RtspMessageType.Response:
                        {
                            //Store the last message
                            m_LastTransmitted = interleaved;

                            //Complete the message if not complete
                            if (!interleaved.IsComplete)
                            {
                                interleaved.CompleteFrom(m_RtspSocket, m_Buffer);
                            }

                            break;
                        }
                }

                //Update counters
                System.Threading.Interlocked.Add(ref m_ReceivedBytes, length + received);

            SetEvent:

                //Clear the event so whoever is waiting can get the response
                m_InterleaveEvent.Set();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        /// <summary>
        /// Increments and returns the current SequenceNumber
        /// </summary>
        internal int NextClientSequenceNumber() { return ++m_CSeq; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StartPlaying(TimeSpan? start = null, Sdp.MediaType? mediaType = null)
        {

            // If already listening and we have started to receive then there is nothing to do 
            if (Playing) return;

            try
            {

                if (!Connected)
                {
                    Connect();                    
                }
        

                //Send the options
                using (SendOptions()) { }
            }
            catch (Exception ex)
            {
                Common.ExceptionExtensions.CreateAndRaiseException(this, "Could not get Options response. See InnerException", ex);
                return;
            }

            try
            {
                //Send describe
                using (SendDescribe()) {  }
            }
            catch (Exception ex)
            {
                Common.ExceptionExtensions.CreateAndRaiseException(this, "Could not get Describe response. See InnerException", ex);
                return;
            }

            //Erase all Contexts before start listening again
            if (m_RtpClient != null) m_RtpClient.TransportContexts.Clear();

            bool setupTracks = false;

            //For each MediaDescription in the SessionDecscription
            foreach (Sdp.MediaDescription md in SessionDescription.MediaDescriptions)
            {
                
                //Don't setup unwanted streams
                if (mediaType.HasValue && md.MediaType != mediaType) continue; 

                try
                {
                    //Send a setup
                    using (RtspMessage setup = SendSetup(md))
                    {
                        //If the setup was okay
                        if (setup.StatusCode == RtspStatusCode.OK)
                        {
                            //Find range info in the SDP
                            var rangeInfo = SessionDescription.RangeLine;

                            //If there is a range directive
                            if (rangeInfo != null)
                            {
                                //range: = 6
                                string[] parts = rangeInfo.Parts[0].Substring(6).Split(TimeSplit[0], EQ);

                                string rangeType = parts[0]; //npt, etc

                                double seconds = 0;

                                if (parts[1] != "now" && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                                {
                                    m_StartTime = TimeSpan.FromSeconds(seconds);
                                }

                                //If there is a start and end time
                                if (parts.Length > 1)
                                {
                                    if (!string.IsNullOrWhiteSpace(parts[2]) && double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                                    {
                                        m_EndTime = TimeSpan.FromSeconds(seconds);
                                    }
                                }
                            }
                        }
                    }

                    setupTracks = true;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (setupTracks) using (RtspMessage play = SendPlay(Location, m_StartTime, m_EndTime))
            {
                OnPlaying();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StopPlaying()
        {
            try
            {
                if (!Playing) return;
                else Disconnect();
            }
            catch { }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Connect()
        {
            try
            {
                IAsyncResult connectResult = null;
                if (Connected) return;
                else if (m_RtspSocket == null)
                {
                    if (m_RtspProtocol == ClientProtocolType.Http || m_RtspProtocol == ClientProtocolType.Reliable)
                    {
                        m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        // Set option that allows socket to close gracefully without lingering.
                        //e.g. DON'T Linger on close if unsent data is present.
                        m_RtspSocket.DontLinger();
                        
                        m_RtspSocket.NoDelay = true;

                        connectResult = m_RtspSocket.BeginConnect(m_RemoteRtsp, new AsyncCallback((iar) =>
                        {
                            try
                            {
                                m_RtspSocket.EndConnect(iar);
                                OnConnected();
                            }
                            catch { }
                        }), null);

                    }
                    else if (m_RtspProtocol == ClientProtocolType.Unreliable)
                    {
                        m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    }
                    else throw new NotSupportedException("The given ClientProtocolType is not supported.");
                }
               
                if(connectResult != null) while (!connectResult.IsCompleted) System.Threading.Thread.Yield();

            }
            catch
            {
                throw;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Disconnect()
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
            if (Playing && !string.IsNullOrWhiteSpace(m_SessionId))
            {

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

                //Fire an event
                OnDisconnected();
            }            
        }

        #endregion

        #region Rtsp

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public RtspMessage SendRtspRequest(RtspMessage request)
        {
            try
            {
                if (!Connected)
                {
                    Connect();
                }               

                //Add the user agent
                if (!request.ContainsHeader(RtspHeaders.UserAgent))
                {
                    request.SetHeader(RtspHeaders.UserAgent, m_UserAgent);
                }

                //If there not already an Authorization header and there is an AuthenticationScheme utilize the information in the Credential
                if (!request.ContainsHeader(RtspHeaders.Authorization) && m_AuthenticationScheme != AuthenticationSchemes.None && Credential != null)
                {
                    //Basic
                    if (m_AuthenticationScheme == AuthenticationSchemes.Basic)
                    {
                        request.SetHeader(RtspHeaders.Authorization, RtspHeaders.BasicAuthorizationHeader(request.Encoding, Credential));
                    }
                    else if (m_AuthenticationScheme == AuthenticationSchemes.Digest)
                    {
                        //Digest
                        request.SetHeader(RtspHeaders.Authorization,
                            RtspHeaders.DigestAuthorizationHeader(request.Encoding, request.Method, request.Location, Credential, null, null, null, null, null, false, null, request.Body));
                    }
                }

                //Add the content encoding header
                if (!request.ContainsHeader(RtspHeaders.ContentEncoding))
                {
                    request.SetHeader(RtspHeaders.ContentEncoding, request.Encoding.EncodingName);
                }

                ///Use the sessionId if present
                if (m_SessionId != null)
                {
                    request.SetHeader(RtspHeaders.Session, m_SessionId);
                }

                //Get the next Sequence Number
                request.CSeq = NextClientSequenceNumber();

                //Use any additional headers if given
                if (AdditionalHeaders.Count > 0) foreach (var additional in AdditionalHeaders) request.AppendOrSetHeader(additional.Key, additional.Value);

                //Get the bytes of the request
                byte[] buffer = m_RtspProtocol == ClientProtocolType.Http ? RtspMessage.ToHttpBytes(request) : request.ToBytes();

              
                int attempt = 0, //The attempt counter itself
                    sent = 0, received = 0, //counter for sending and receiving locally
                    offset = 0, length = buffer.Length, max = RtspMessage.MaximumLength - length; //The offsets into the buffer and the maximum amounts

                //The error which will be ignored incase non-blocking sockets are being used.
                SocketError error = SocketError.Success;

                #region Reference

                /*
                    RFC2326 - http://tools.ietf.org/html/rfc2326 [Page 18]
                 
                    RTSP is a text-based protocol and uses the ISO 10646 character set in
                    UTF-8 encoding (RFC 2279 [21]). Lines are terminated by CRLF, but
                    receivers should be prepared to also interpret CR and LF by
                    themselves as line terminators.
                    
                See also RFC 2326 - http://tools.ietf.org/html/rfc2326 [Page 28]               
                */

                #endregion

                //if (m_RtspSocket.ProtocolType == ProtocolType.Tcp && Playing) sent += m_RtpClient.SendData(buffer, null, m_RtspSocket, m_RtspSocket.RemoteEndPoint, out error, false);
                sent += m_RtspSocket.Send(buffer, sent, length - sent, SocketFlags.None, out error);


                //If we could not send the message indicate so
                if (sent < length || error == SocketError.ConnectionReset) return null;
                
                //Fire the event
                Requested(m_LastTransmitted = request);

                //Increment our byte counters for Rtsp
                m_SentBytes += sent;

                //Attempt to receive
                attempt = 0;

                //Set the block
                m_InterleaveEvent.Reset();

                //Jump ahead to wait
                if (Playing && m_RtpProtocol == ProtocolType.Tcp) goto Wait;

            Receive:
                received = m_RtspSocket.Receive(m_Buffer.Array, offset, max, SocketFlags.None, out error);

                if (error == SocketError.ConnectionReset) return null;

                //If anything was received
                if (received > 0)
                {

                    //je
                    if (error == SocketError.TryAgain || error == SocketError.TimedOut) goto Receive;

                    //If the buffer had the start of frame check for the start of the upper layer message
                    if (m_Buffer[offset] == RtpClient.BigEndianFrameControl) m_RtpClient.ProcessFrameData(m_Buffer.Array, offset, received, m_RtspSocket);
                    else ProcessInterleaveData(this, m_Buffer.Array, m_Buffer.Offset, received);
                    //Count use SetLength on memory not to create a new segment, would also give the option of getting more data if needed.
                    //The only thing in such a case is that if \r\n happen to be in the buffer then this is interpreted as a body.
                }

            Wait:
                //We have not yet received a response, wait on the interleave event for the amount of time specified, if signaled a response was created
                while (m_LastTransmitted == null || m_LastTransmitted.MessageType != RtspMessageType.Response && ++attempt < m_RetryCount)                     
                    if (m_InterleaveEvent.Wait(SocketReadTimeout)) break;
                    else if (!Playing || m_RtpProtocol == ProtocolType.Udp) goto Receive; //jne

                //If we were not authroized and we did not give a nonce and there was an Authentiate header given then we will attempt to authenticate using the information in the header
                if (m_LastTransmitted != null && m_LastTransmitted.StatusCode == RtspStatusCode.Unauthorized && m_LastTransmitted.ContainsHeader(RtspHeaders.WWWAuthenticate) && Credential != null)
                {
                    //http://tools.ietf.org/html/rfc2617
                    //3.2.1 The WWW-Authenticate Response Header
                    //Example
                    //WWW-Authenticate: Digest realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525"\r\n

                    string authenticateHeader = m_LastTransmitted[RtspHeaders.WWWAuthenticate];

                    string[] baseParts = authenticateHeader.Split(SpaceSplit, 1);

                    if (string.Compare(baseParts[0].Trim(), "basic", true) == 0)
                    {
                        AuthenticationScheme = AuthenticationSchemes.Basic;

                        request.SetHeader(RtspHeaders.Authorization, RtspHeaders.BasicAuthorizationHeader(request.Encoding, Credential));

                        //Recurse the call with the info from then authenticate header
                        return SendRtspRequest(request);

                    }
                    else if (string.Compare(baseParts[0].Trim(), "digest", true) == 0)
                    {
                        AuthenticationScheme = AuthenticationSchemes.Digest;

                        //string[] parts = authenticateHeader.Replace(baseParts[0], string.Empty).Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);

                        string[] parts = authenticateHeader.Split(SpaceSplit, StringSplitOptions.RemoveEmptyEntries);

                        string algorithm = "MD5";

                        //string username = parts.Where(p => string.Compare("username", p, true) == 0).FirstOrDefault();
                        //if (username != null) username = username.Replace("username=", string.Empty);

                        string username = parts.Where(p => p.StartsWith("username",StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(username )) username = username.Substring (9);
 

                        //string realm = parts.Where(p => string.Compare("realm", p, true) == 0).FirstOrDefault();
                        //if (realm != null) realm = realm.Replace("realm=", string.Empty);

                        string realm = parts.Where(p => p.StartsWith("realm", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
						if (!string.IsNullOrWhiteSpace(realm))
						{
							realm = realm.Substring("realm=".Length).Replace("\"",string.Empty).Replace("\'",string.Empty);
							if (Credential != null) Credential.Domain = realm;
						}

                        //string nc = parts.Where(p => string.Compare("nc", p, true) == 0).FirstOrDefault();
                        //if (nc != null) nc = realm.Replace("nc=", string.Empty);

                        string nc = parts.Where(p => p.StartsWith("nc", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(nc)) nc = realm.Substring("nc=".Length);

                        //string nonce = parts.Where(p => string.Compare("nonce", p, true) == 0).FirstOrDefault();
                        //if (nonce != null) nonce = realm.Replace("nonce=", string.Empty);

                        string nonce = parts.Where(p => p.StartsWith("nonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
						if (!string.IsNullOrWhiteSpace(nonce)) nonce = nonce.Substring("nonce=".Length).Replace("\"", string.Empty).Replace("\'", string.Empty);

                        string cnonce = parts.Where(p => p.StartsWith("cnonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();//parts.Where(p => string.Compare("cnonce", p, true) == 0).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(cnonce)) cnonce = cnonce.Substring(7).Replace("\"", string.Empty).Replace("\'", string.Empty);//cnonce = cnonce.Replace("cnonce=", string.Empty);

                        string uri = parts.Where(p => p.StartsWith("uri", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); //parts.Where(p => p.Contains("uri")).FirstOrDefault();

                        bool rfc2069 = !string.IsNullOrWhiteSpace(uri) && !uri.Contains(TimeSplit[0]);

                        if (!string.IsNullOrWhiteSpace(uri))
                        {
                            //if (rfc2069) uri = uri.Replace("uri=", string.Empty);
                            //else uri = uri.Replace("digest-uri=", string.Empty);

                            if (rfc2069) uri = uri.Substring(4);
                            else uri = uri.Substring(11);
                        }

                        string qop = parts.Where(p => string.Compare("qop", p, true) == 0).FirstOrDefault();

                        if (!string.IsNullOrWhiteSpace(qop))
                        {
                            qop = qop.Replace("qop=", string.Empty);
                            //if (nc != null) nc = nc.Replace("nc=", string.Empty);
                            if (nc != null) nc = nc.Substring(3);
                        }

                        //string opaque = parts.Where(p => string.Compare("opaque", p, true) == 0).FirstOrDefault();
                        string opaque = parts.Where(p => p.StartsWith("opaque", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(opaque)) opaque = opaque.Substring(7);
                        //opaque = opaque.Replace("opaque=", string.Empty);

                        //string response = parts.Where(p => string.Compare("response", p, true) == 0).FirstOrDefault();
                        //if (response != null) response = response.Replace("response=", string.Empty);

                        request.SetHeader(RtspHeaders.Authorization, RtspHeaders.DigestAuthorizationHeader(request.Encoding, request.Method, request.Location, Credential, qop, nc, nonce, cnonce, opaque, rfc2069, algorithm, request.Body));

                        //Recurse the call with the info from then authenticate header
                        return SendRtspRequest(request);
                    }
                }

                if (m_LastTransmitted != null)
                {
                    //TODO
                    //REDIRECT (Handle loops)
                    //if(m_LastTransmitted.StatusCode == RtspStatusCode.MovedPermanently)

                    switch (m_LastTransmitted.StatusCode)
                    {
                        case RtspStatusCode.NotImplemented: m_SupportedMethods.Remove(m_LastTransmitted.Method); break;
                        case RtspStatusCode.MethodNotValidInThisState: if (m_LastTransmitted.ContainsHeader(RtspHeaders.Allow)) SwitchProtocols(); break;
                        default: break;
                    }

                    //Check for a SessionId and Timeout if we don't already have one unless this is a GET_PARAMETER or TEARDOWN
                    if (string.IsNullOrWhiteSpace(m_SessionId) && (request.Method != RtspMethod.TEARDOWN && m_LastTransmitted.ContainsHeader(RtspHeaders.Session)))
                    {
                        string sessionHeader = m_LastTransmitted[RtspHeaders.Session];

                        //If there is a session header it may contain the option timeout
                        if (!string.IsNullOrWhiteSpace(sessionHeader))
                        {
                            //Check for session and timeout

                            //Get the values
                            string[] temp = sessionHeader.Split(TimeSplit[1]);

                            //Check for any values
                            if (temp.Length > 0)
                            {
                                //Get the SessionId if present
                                m_SessionId = temp[0].Trim();

                                //Check for a timeout
                                if (temp.Length > 1)
                                {
                                    int timeoutStart = 1 + temp[1].IndexOf(EQ);
                                    if (timeoutStart > 0 && int.TryParse(temp[1].Substring(timeoutStart), out timeoutStart))
                                    {
                                        if (timeoutStart <= 0)
                                        {
                                            m_RtspTimeout = TimeSpan.FromSeconds(60);//Default
                                        }
                                        else
                                        {
                                            m_RtspTimeout = TimeSpan.FromSeconds(timeoutStart);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //The timeout was not present
                                m_SessionId = sessionHeader.Trim();
                            }
                        }
                    }

                    if (m_LastTransmitted != null) Received(request, m_LastTransmitted);
                }

                //Return the result
                return m_LastTransmitted;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Sends the Rtsp OPTIONS request
        /// </summary>
        /// <param name="useStar">The OPTIONS * request will be sent rather then one with the <see cref="RtspClient.Location"/></param>
        /// <returns>The <see cref="RtspMessage"/> as a response to the request</returns>
        public RtspMessage SendOptions(bool useStar = false)
        {
            RtspMessage response = SendRtspRequest(new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.OPTIONS,
                Location = useStar ? null : Location
            });

            if (response == null || response.StatusCode != RtspStatusCode.OK) Common.ExceptionExtensions.CreateAndRaiseException(this, "Unable to get options");
            else
            {
                m_SupportedMethods.Clear();

                string publicMethods = response[RtspHeaders.Public];

                if (string.IsNullOrWhiteSpace(publicMethods)) return response;

                foreach (string method in publicMethods.Split(SpaceSplit[1]))
                {
                    m_SupportedMethods.Add((RtspMethod)Enum.Parse(typeof(RtspMethod), method.Trim(), true));
                }

                //Should also store Supported:
            }
            
            return response;
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
                RtspMessage describe = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.DESCRIBE,
                    Location = Location
                };

                #region Reference

                 // The DESCRIBE method retrieves the description of a presentation or
                 // media object identified by the request URL from a server. It may use
                 // the Accept header to specify the description formats that the client
                 // understands. The server responds with a description of the requested
                 // resource. The DESCRIBE reply-response pair constitutes the media
                 // initialization phase of RTSP.

                #endregion

                describe.SetHeader(RtspHeaders.Accept, Sdp.SessionDescription.MimeType);

                response = SendRtspRequest(describe);

                if (response == null) Common.ExceptionExtensions.CreateAndRaiseException(describe, "Unable to describe media, no response to DESCRIBE request. The request is in the Tag property.");
                else if (response.StatusCode != RtspStatusCode.OK)
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(response.StatusCode, "Unable to describe media. The StatusCode is in the Tag property.");
                }
                else if (response.GetHeader(RtspHeaders.ContentType).Trim() != Sdp.SessionDescription.MimeType || string.IsNullOrWhiteSpace(response.Body))
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(this, "Unable to describe media, Missing Session Description");
                }
                
                m_SessionDescription = new Sdp.SessionDescription(response.Body);
            }
            catch (Common.Exception<RtspClient>)
            {
                throw;
            }
            catch (Common.Exception<SessionDescription>)
            {
                Common.ExceptionExtensions.CreateAndRaiseException(this, "Unable to describe media, Session Description Exception Occured.");
            }
            catch(Exception ex) { Common.ExceptionExtensions.CreateAndRaiseException(this, "An error occured", ex); }


            return response;
        }

        public RtspMessage SendTeardown(MediaDescription mediaDescription = null)
        {
            RtspMessage response = null;
            try
            {
                Uri location;

                if (mediaDescription != null)
                {
                    SessionDescriptionLine attributeLine = mediaDescription.ControlLine;
                    location = new Uri(Location.OriginalString + '/' + attributeLine.Parts.FirstOrDefault(p => p.Contains("control")).Replace("control:", string.Empty));
                }
                else
                {
                    location = Location;
                }

                OnStopping(mediaDescription);

                //If there is a client
                if (m_RtpClient != null)
                {
                    //Send a goodbye for all contexts if the mediaDescription was not given
                    if (mediaDescription == null) m_RtpClient.SendGoodbyes();
                    else//Find the context for the description
                    {
                        RtpClient.TransportContext context = m_RtpClient.GetContextForMediaDescription(mediaDescription);
                        //If availabnle then send a goodbye
                        if (context != null)
                        {
                            m_RtpClient.SendGoodbye(context);
                            context = null;
                        }
                    }
                }

                //Return the result of the Teardown
                return SendRtspRequest(new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.TEARDOWN,
                    Location = Location
                });
            }
            catch (Common.Exception<RtspClient>)
            {
                return response;
            }
            catch
            {
                throw;
            }
        }

        public RtspMessage SendSetup(MediaDescription mediaDescription)
        {
            SessionDescriptionLine controlLine = mediaDescription.ControlLine;
            Uri location = null;
            //If there is a control line in the SDP it contains the URI used to setup and control the media
            if (controlLine != null)
            {
                string controlPart = controlLine.Parts.Where(p => p.Contains("control")).FirstOrDefault();

                //If there is a controlPart in the controlLine
                if (!string.IsNullOrWhiteSpace(controlPart))
                {
                    //Prepare the part
                    controlPart = controlPart.Split(Colon, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                    //Determine if its a Absolute Uri
                    if (controlPart.StartsWith(RtspMessage.ReliableTransport, StringComparison.OrdinalIgnoreCase) || controlPart.StartsWith(RtspMessage.UnreliableTransport, StringComparison.OrdinalIgnoreCase))
                    {
                        location = new Uri(controlPart);
                    }
                    else //Or Relative
                    {
                        location = new Uri(Location.OriginalString + '/' + controlPart);
                    }
                }
            }            
            //Send the setup
            return SendSetup(location ?? Location, mediaDescription);
        }

        internal RtspMessage SendSetup(Uri location, MediaDescription mediaDescription, bool unicast = true)//False to use manually set protocol
        {
            if (location == null) throw new ArgumentNullException("location");
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");
            try
            {
                RtspMessage setup = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.SETUP,
                    Location = location ?? Location
                };

                //Todo Determine if Multicast
                string connectionType = unicast ? "unicast;" : "multicast";

                // TCP was specified or the MediaDescription specified we need to use Tcp as specified in RFC4571
                if (m_RtpProtocol == ProtocolType.Tcp)
                {
                    //m_SessionDescription.MediaDescriptions[0].MediaProtocol SHOULD == RTP/AVP/TCP

                    //If there is already a RtpClient with at-least 1 TransportContext
                    if (m_RtpClient != null && m_RtpClient.TransportContexts.Count > 0)
                    {
                        RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                        setup.SetHeader(RtspHeaders.Transport, "RTP/AVP/TCP;" + connectionType + "interleaved=" + (lastContext.DataChannel + 2) + TimeSplit[0] + (lastContext.ControlChannel + 2));
                    }
                    else
                    {
                        setup.SetHeader(RtspHeaders.Transport, "RTP/AVP/TCP;" + connectionType + "interleaved=0-1");
                    }
                }
                else if(string.Compare(mediaDescription.MediaProtocol, "RTP/AVP", true) == 0) // We need to find an open Udp Port
                {
                    m_RtpProtocol = ProtocolType.Udp;
                    //Might want to reserver this port now by making a socket...
                    int openPort = Utility.FindOpenPort(ProtocolType.Udp, 30000 + (m_RtpClient != null ? 2 : 0), true); //Should allow this to be given or set as a property MinimumUdpPort, MaximumUdpPort

                    if (openPort == -1) Common.ExceptionExtensions.CreateAndRaiseException(this, "Could not find open Udp Port");
                    //else if (MaximumUdp.HasValue && openPort > MaximumUdp)
                    //{
                    //    Common.ExceptionExtensions.CreateAndRaiseException(this, "Found Udp Port > MaximumUdp. Found: " + openPort);
                    //}    
                    setup.SetHeader(RtspHeaders.Transport, m_SessionDescription.MediaDescriptions[0].MediaProtocol + ";" + connectionType + "client_port=" + openPort + TimeSplit[0] + (openPort + 1));
                }

                //Get the response for the setup
                RtspMessage response = SendRtspRequest(setup);

                if (response == null) Common.ExceptionExtensions.CreateAndRaiseException(this, "No response to SETUP");

                //Response not OK
                else if (response.StatusCode != RtspStatusCode.OK)
                {
                    //Transport requested not valid
                    if (response.StatusCode == RtspStatusCode.UnsupportedTransport && m_RtpProtocol != ProtocolType.Tcp)
                    {
                        goto SetupTcp;
                    }
                    else if (response.StatusCode == RtspStatusCode.SessionNotFound)
                    {
                        SendTeardown();
                        return SendSetup(location, mediaDescription);
                    }
                    else
                    {
                        Common.ExceptionExtensions.CreateAndRaiseException(response.StatusCode, "Unable to setup media. The status code is in the Tag property.");
                    }
                }

                string transportHeader = response[RtspHeaders.Transport];

#if DEBUG
                System.Diagnostics.Debug.WriteLine("RtspClient Got Response Transport Header: " + transportHeader);
#endif

                //We need a transportHeader with RTP
                if (string.IsNullOrEmpty(transportHeader) || !transportHeader.Contains("RTP")) Common.ExceptionExtensions.CreateAndRaiseException(this, "Cannot setup media, Invalid Transport Header in Rtsp Response: " + transportHeader);

                //Get the parts of information from the transportHeader
                string[] parts = transportHeader.Split(TimeSplit[1]);

                //Values in the header we need
                int clientRtpPort = -1, clientRtcpPort = -1, serverRtpPort = -1, serverRtcpPort = -1, ssrc = 0;

                //If there are Bandwidth lines with RR:0 and RS:0
                //IEnumerable<SessionDescriptionLine> rtcpLines = mediaDescription.Lines.Where(l => l.Type == 'b' && l.Parts.Count > 1 && (l.Parts[0] == "RR" || l.Parts[0] == "RS") && l.Parts[1] == "0");

                //http://www.ietf.org/rfc/rfc4566.txt
                //5.8.  Bandwidth ("b=")

                //Some providers disable Rtcp for one reason or another, it is strongly not recommended
                //If there are two lines which match the criteria then disable Rtcp
                bool rtcpDisabled = false;

                // RS                    RR                     AS
                int reportSendingEvery = 96, reportReceivingEvery= 96;//, applicationSpecific;

                TimeSpan contextReportInterval = RtpClient.DefaultReportInterval;

                foreach (SessionDescriptionLine line in mediaDescription.BandwidthLines)
                {
                    if (line.Parts[0].StartsWith("RR"))
                    {
                        reportReceivingEvery = int.Parse(line.Parts[0].Split(Colon, StringSplitOptions.RemoveEmptyEntries)[1]);                        
                    }

                    if (line.Parts[0].StartsWith("RS"))
                    {
                        reportSendingEvery = int.Parse(line.Parts[0].Split(Colon, StringSplitOptions.RemoveEmptyEntries)[1]);
                    }

                    //if (line.Parts[0].StartsWith("AS"))
                    //{
                    //    applicationSpecific = int.Parse(line.Parts[0].Split(Colon, StringSplitOptions.RemoveEmptyEntries)[1]);
                    //}
                }

                rtcpDisabled = reportReceivingEvery + reportSendingEvery == 0;

                //If Rtcp is not disabled then this will set the read and write timeouts.
                if (!rtcpDisabled)
                {
                    //contextReportInterval = TimeSpan.FromMilliseconds((reportReceivingEvery + reportSendingEvery) / 2);
                    SocketWriteTimeout = reportSendingEvery;
                    SocketReadTimeout = reportReceivingEvery;
                    ProtocolSwitchTime = contextReportInterval;
                }
                else contextReportInterval = TimeSpan.MaxValue;
                
                //Cache this to prevent having to go to get it every time down the line
                IPAddress sourceIp = ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address;

                ///The transport header contains the following information, this needs to be trimmed
                for (int i = 0, e = parts.Length; i < e; ++i)
                {
                    //Trim the part
                    string part = parts[i].Trim();

                    if (string.IsNullOrWhiteSpace(part)) continue;
                    else if (string.Compare(part, "unicast", true, System.Globalization.CultureInfo.InvariantCulture) == 0) { continue; }
                    else if (part.StartsWith("source=", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        string sourcePart = part.Substring(7, part.Length - 7);

                        if (!IPAddress.TryParse(sourcePart, out sourceIp))
                        {
                            Common.ExceptionExtensions.CreateAndRaiseException(this, "Source Part did not contain a valid IP Address");
                        }
                    }
                    else if (part.StartsWith("ssrc=", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        string ssrcPart = part.Substring(5).Trim();

                        if (!ssrcPart.StartsWith("0x", true, System.Globalization.CultureInfo.InvariantCulture) && !int.TryParse(ssrcPart, out ssrc)) //plain int                        
                            ssrc = int.Parse(ssrcPart.Substring(2), System.Globalization.NumberStyles.HexNumber); //hex
                    }
                    else if (string.Compare(part, "multicast", true, System.Globalization.CultureInfo.InvariantCulture) == 0)
                    {
                        //Todo Implement multicast send, recieve...
                        throw new NotImplementedException();
                    }
                    else if (part.StartsWith("client_port=", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        string[] clientPorts = part.Substring(12).Split(TimeSplit[0]);

                        if (clientPorts.Length > 1)
                        {
                            clientRtpPort = int.Parse(clientPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                            clientRtcpPort = int.Parse(clientPorts[1], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Common.ExceptionExtensions.CreateAndRaiseException(this, "Server indicated Udp but did not provide a port pair: " + transportHeader);
                        }

                    }
                    else if (part.StartsWith("mode=", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        //PLAY, ....
                        m_TransportMode = part.Substring(5).Trim();
                    }
                    else if (part.StartsWith("interleaved=", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        //Should only be for Tcp
                        string[] channels = part.Substring(12).Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);

                        if (channels.Length > 1)
                        {
                            RtpClient.TransportContext transportContext = new RtpClient.TransportContext(byte.Parse(channels[0], System.Globalization.CultureInfo.InvariantCulture),
                                byte.Parse(channels[1], System.Globalization.CultureInfo.InvariantCulture),
                                RFC3550.Random32(Rtcp.ReceiversReport.PayloadType), mediaDescription, m_RtspSocket, !rtcpDisabled, ssrc, 2);

                            transportContext.m_SendInterval = TimeSpan.FromMilliseconds(reportSendingEvery);
                            transportContext.m_ReceiveInterval = TimeSpan.FromMilliseconds(reportReceivingEvery);

                            //If there is not a client
                            if (m_RtpClient == null)
                            {

                                int bufferSize = m_Buffer.Count - RtspMessage.MaximumLength;

                                Common.MemorySegment memory = null;

                                if (bufferSize > 0) memory = new Common.MemorySegment(m_Buffer.Array, RtspMessage.MaximumLength, bufferSize);

                                //Create a Duplexed reciever using the RtspSocket
                                m_RtpClient = RtpClient.Duplexed(m_RtspSocket, memory);

                                m_RtpClient.InterleavedData += ProcessInterleaveData;
                            }                            

                            //try to add the transportChannel
                            m_RtpClient.Add(transportContext);

                            //and initialize the client from the RtspSocket
                            transportContext.Initialize(m_RtspSocket);
                        }
                        else
                        {
                            Common.ExceptionExtensions.CreateAndRaiseException(transportHeader, "Server indicated Tcp Transport but did not provide a channel pair. The header is in the Tag property.");
                        }
                    }
                    else if (part.StartsWith("server_port=", true, System.Globalization.CultureInfo.InvariantCulture) && m_RtpProtocol == ProtocolType.Udp) //Don't allow parsing of server_port when in Interleaved
                    {
                        string[] serverPorts = part.Substring(12).Split(TimeSplit[0]);

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
                            //if (serverRtpPort == serverRtcpPort)
                            //{
                            //    //Duplexing....
                            //}

                            //If we need to make a client then do so
                            if (m_RtpClient == null)
                            {
                                if (m_RtpProtocol == ProtocolType.Udp)
                                {
                                    int bufferSize = m_Buffer.Count - RtspMessage.MaximumLength;

                                    Common.MemorySegment memory = null;

                                    if (bufferSize > 0) memory = new Common.MemorySegment(m_Buffer.Array, RtspMessage.MaximumLength, bufferSize);

                                    //Create a Udp Reciever
                                    m_RtpClient = RtpClient.Participant(m_RemoteIP, memory);
                                }
                                else Media.Common.ExceptionExtensions.CreateAndRaiseException<RtspClient>(this, "RtpProtocol is not Udp and Server required Udp Transport.");
                            }

                            //Valid amounts of 2 should be only if ssrc is 0 e.g. ssrc > 0 : 0 : 2

                            //Add the transportChannel for the mediaDescription
                            if (m_RtpClient.TransportContexts.Count == 0)
                            {
                                RtpClient.TransportContext newContext = new RtpClient.TransportContext(0, 1, RFC3550.Random32(Rtcp.ReceiversReport.PayloadType), mediaDescription, !rtcpDisabled, ssrc, 2);
                                newContext.m_SendInterval = TimeSpan.FromMilliseconds(reportSendingEvery);
                                newContext.m_ReceiveInterval = TimeSpan.FromMilliseconds(reportReceivingEvery);
                                newContext.Initialize(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.Add(newContext);
                            }
                            else
                            {
                                RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                                RtpClient.TransportContext nextContext = new RtpClient.TransportContext((byte)(lastContext.DataChannel + 2), (byte)(lastContext.ControlChannel + 2), RFC3550.Random32(Rtcp.ReceiversReport.PayloadType), mediaDescription, !rtcpDisabled, ssrc, 2);
                                nextContext.m_SendInterval = TimeSpan.FromMilliseconds(reportSendingEvery);
                                nextContext.m_ReceiveInterval = TimeSpan.FromMilliseconds(reportReceivingEvery);
                                nextContext.Initialize(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);
                                m_RtpClient.Add(nextContext);
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

                //Setup Complete
                return response;
            }
            catch (Exception ex)
            {
                Common.ExceptionExtensions.CreateAndRaiseException(this, "Unable to setup media. See InnerException", ex);
            }
        //Setup for Interleaved
        SetupTcp:
            {
                Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Tcp;

                //Clear existing transportChannels
                m_RtpClient.TransportContexts.Clear();

                //Recurse call to ensure propper setup
                return SendSetup(location, mediaDescription);
            }
        }

        internal void SwitchProtocols(object state = null)
        {
            //If there is no socket or the protocol was forced return`
            if (m_RtspSocket == null || m_ForcedProtocol || Playing) return;

            //If the client has not recieved any bytes and we have not already switched to Tcp
            else if (m_RtpProtocol != ProtocolType.Tcp)
            {
                //Reconnect without losing the events on the RtpClient
                Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Tcp;

                //Disconnect to allow the server to reset state
                Disconnect();

                //Start again
                StartPlaying();
            }
            else if (m_RtpProtocol == ProtocolType.Tcp)
            {
                //Switch back to Udp?
                throw new NotImplementedException("Switch from Tcp to Udp Not (YET) Implemented.");

                //This seems to werk though :p

                //Client.m_TransportProtocol = m_RtpProtocol = ProtocolType.Udp;

                ////Disconnect to allow the server to reset state
                //Disconnect();

                ////Clear existing transportChannels
                //m_RtpClient.TransportContexts.Clear();

                ////Start again
                //StartListening();
            }

            m_ProtocolSwitchTimer.Dispose();
            m_ProtocolSwitchTimer = null;

        }

        //http://www.ietf.org/rfc/rfc2326.txt 10.5 PLAY

        /*
          C->S: PLAY rtsp://audio.example.com/audio RTSP/1.0
           CSeq: 835
           Session: 12345678
           Range: npt=10-15

         C->S: PLAY rtsp://audio.example.com/audio RTSP/1.0
               CSeq: 836
               Session: 12345678
               Range: npt=20-25

         C->S: PLAY rtsp://audio.example.com/audio RTSP/1.0
               CSeq: 837
               Session: 12345678
               Range: npt=30-
         */

        public RtspMessage SendPlay(Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt", string rangeFormat = null)
        {
            try
            {
                RtspMessage play = new RtspMessage(RtspMessageType.Request)
                {
                    Method = RtspMethod.PLAY,
                    Location = location ?? Location
                };

                play.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startTime, endTime, rangeType, rangeFormat));

                RtspMessage response = SendRtspRequest(play);

                //Handle allowed problems with reception
                if (response == null) goto NoResponsePlaying;
                else if (response.StatusCode == RtspStatusCode.Unknown) goto NoResponsePlaying;
                else if (response.StatusCode == RtspStatusCode.InvalidRange)
                {
                    play.RemoveHeader(Rtsp.RtspHeaders.Range);
                    ++play.CSeq;
                    return SendRtspRequest(play);
                }

                //Should check for RtpInfo
                //And if present validate context with it, e.g. reduce requried packets and set ssrcs and start sequence.

                //string rtpInfo = response[RtspHeaders.RtpInfo];

                //If needed and given
//                int startRtpSequence = -1;

//                //should throw not found RtpInfo
//                if (!string.IsNullOrEmpty(rtpInfo))
//                {
//                    string[] pieces = rtpInfo.Split(SpaceSplit[1]);
//                    foreach (string piece in pieces)
//                    {
//                        if (piece.Trim().StartsWith("url="))
//                        {
//                            //Location = new Uri(piece.Replace("url=", string.Empty).Trim());
//                        }
//                        else if (piece.Trim().StartsWith("seqno="))
//                        {
//                            startRtpSequence = Convert.ToInt32(piece.Replace("seqno=", string.Empty).Trim());
//                        }
//#if DEBUG
//                        else
//                        {
//                            System.Diagnostics.Debug.WriteLine("RtspClient Encountered unhandled Rtp-Info part: " + piece);
//                        }
//#endif
//                    }
//                }

                string rangeString = response[RtspHeaders.Range];

                //Should throw if RtpInfo was not present
                if (!string.IsNullOrEmpty(rangeString))
                {
                    string[] times = rangeString.Trim().Split(EQ);
                    if (times.Length > 1)
                    {
                        //Determine Format
                        if (times[0] == "npt")//ntp=1.060-20
                        {
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);
                            if (times[0].ToLowerInvariant() == "now") m_StartTime = TimeSpan.Zero;
                            else if (times.Length == 1)
                            {
                                if (times[0].Contains(':'))
                                {
                                    m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    m_StartTime = TimeSpan.FromSeconds(double.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                                }
                                //Only start is live?
                                //m_Live = true;
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
                            else Common.ExceptionExtensions.CreateAndRaiseException(this, "Invalid Range Header Received: " + rangeString);
                        }
                        else if (times[0] == "smpte")//smpte=0:10:20-;time=19970123T153600Z
                        {
                            //Get the times into the times array skipping the time from the server (order may be first so I explicitly did not use Substring overload with count)
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith("time=")).ToArray();
                            if (times[0].ToLowerInvariant() == "now") m_StartTime = TimeSpan.Zero;
                            else if (times.Length == 1)
                            {
                                m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                //Only start is live?
                                //m_Live = true;
                            }
                            else if (times.Length == 2)
                            {
                                m_StartTime = TimeSpan.Parse(times[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                m_EndTime = TimeSpan.Parse(times[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else Common.ExceptionExtensions.CreateAndRaiseException(this, "Invalid Range Header Received: " + rangeString);
                        }
                        else if (times[0] == "clock")//clock=19961108T142300Z-19961108T143520Z
                        {
                            //Get the times into times array
                            times = times[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);
                            //Check for live
                            if (times[0].ToLowerInvariant() == "now") m_StartTime = TimeSpan.Zero;
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
                                //m_Live = true;
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
                            else Common.ExceptionExtensions.CreateAndRaiseException(this, "Invalid Range Header Received: " + rangeString);
                        }

                    }
                }

            NoResponsePlaying:

                //If we have a timeout to switch the protocols and the protocol has not been forced
                if (!Playing && (!m_ForcedProtocol && SocketReadTimeout > 0) && m_RtpProtocol != ProtocolType.Tcp)
                {
                    //Setup a timer, should be accessible from the instance...
                    m_ProtocolSwitchTimer = new System.Threading.Timer(new TimerCallback(SwitchProtocols), null, m_RtspTimeout, System.Threading.Timeout.InfiniteTimeSpan);
                }

                //If there is a timeout ensure it gets utilized
                if (m_RtspTimeout > TimeSpan.Zero && m_KeepAliveTimer == null)
                {
                    //Use half the timeout to protect against dialation
                    m_KeepAliveTimer = new Timer(new TimerCallback(SendKeepAlive), null, m_RtspTimeout.Milliseconds, -1);
                }

                //Connect and wait for Packets
                if(!m_RtpClient.Connected) m_RtpClient.Connect();

                return response;
            }
            catch { throw; }
        }

        internal void SendKeepAlive(object state)
        {

            try
            {

                if (!Connected) Connect();

                if (!Connected) return;
                //Darwin DSS and other servers might not support GET_PARAMETER
                else if (m_SupportedMethods.Contains(RtspMethod.GET_PARAMETER))
                {
                    SendGetParameter(null);
                }
                else if (m_SupportedMethods.Contains(RtspMethod.OPTIONS)) //If at least options is supported
                {
                    SendOptions();
                }

                m_KeepAliveTimer.Change(m_RtspTimeout, System.Threading.Timeout.InfiniteTimeSpan);

            }
            catch
            {
                if(m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();
                m_KeepAliveTimer = null;
                //Disconnect();
            }
        }

        public RtspMessage SendGetParameter(string body = null)
        {
            RtspMessage get = new RtspMessage(RtspMessageType.Request)
            {
                Method = RtspMethod.GET_PARAMETER,
                Location = Location,
                Body = body ?? string.Empty //"\r\n$\00"
            };
            return SendRtspRequest(get);
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            if (Disposed) return;

            StopPlaying();

            base.Dispose();

            if (m_RtpClient != null)
            {
                if (!m_RtpClient.Disposed) m_RtpClient.Dispose();
                m_RtpClient = null;
            }

            if (m_Buffer != null)
            {
                m_Buffer.Dispose();
                m_Buffer = null;
            }

        }

        #endregion

        IEnumerable<Socket> Common.ISocketOwner.OwnedSockets
        {
            get { return m_RtspSocket.Yield(); }
        }
    }
}
