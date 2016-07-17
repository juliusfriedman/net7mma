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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Media.Common;
using Media.Rtp;
using Media.Rtcp;
using Media.Rtsp.Server.MediaTypes;

namespace Media.Rtsp
{
    /// <summary>
    /// Implementation of Rtsp / RFC2326 server 
    /// http://tools.ietf.org/html/rfc2326
    /// Suppports Reliable(Rtsp / Tcp or Rtsp / Http) and Unreliable(Rtsp / Udp) connections
    /// </summary>
    public class RtspServer : Common.BaseDisposable, Common.ISocketReference, Common.IThreadReference
    {
        public const int DefaultPort = 554;

        //Milliseconds
        internal const int DefaultReceiveTimeout = 500;
        
        //Milliseconds
        internal const int DefaultSendTimeout = 500;

        internal static void ConfigureRtspServerThread(Thread thread)
        {
            thread.TrySetApartmentState(ApartmentState.MTA);

            thread.Priority = ThreadPriority.BelowNormal;
        }

        internal static void ConfigureRtspServerSocket(Socket socket, Socket prototype = null)
        {
            int i4; bool i4b = false;
            ConfigureRtspServerSocket(socket, out i4, ref i4b, prototype);
        }

        internal static void ConfigureRtspServerSocket(Socket socket, out int interFrameGap, Socket prototype = null)
        {
            bool i4b = false;
            ConfigureRtspServerSocket(socket, out interFrameGap, ref i4b, prototype);
        }

        internal static void ConfigureRtspServerSocket(Socket socket, out int interFrameGap, ref bool reconfigure, Socket prototype = null)
        {
            if (object.ReferenceEquals(socket, null)) throw new ArgumentNullException("socket");

            if (reconfigure.Equals(false))
            {
                //Ensure the address can be re-used
                Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(socket));

                //Windows >= 10 and Some Unix
                Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableUnicastPortReuse(socket));
            }

            //Tcp confgiuration
            if (socket.ProtocolType == ProtocolType.Tcp)
            {                
                // Set option that allows socket to close gracefully without lingering.
                Media.Common.Extensions.Socket.SocketExtensions.DisableLinger(socket);

                //Windows options
                if (Common.Extensions.OperatingSystemExtensions.IsWindows)
                {
                    // Not useful except for debugging the network 
                    //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpTimestamp(socket);

                    //Retransmit for 0 sec
                    Media.Common.Extensions.Socket.SocketExtensions.DisableTcpRetransmissions(socket);

                    // Enable No Syn Retries
                    Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableTcpNoSynRetries(socket));

                    // Enable CongestionAlgorithm
                    Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableTcpCongestionAlgorithm(socket));

                    // Set OffloadPreferred
                    Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.SetTcpOffloadPreference(socket));
                }

                //If both send and receieve buffer size are 0 then there is no coalescing when socket's algorithm is disabled
                Media.Common.Extensions.Socket.SocketExtensions.DisableTcpNagelAlgorithm(socket);
                //socket.NoDelay = true;

                //Allow more than one byte of urgent data
                Media.Common.Extensions.Socket.SocketExtensions.EnableTcpExpedited(socket);

                //Receive any urgent data in the normal data stream
                Media.Common.Extensions.Socket.SocketExtensions.EnableTcpOutOfBandDataInLine(socket);  
             

                //Todo, MaxSegmentSize

            }
            //else if(socket.ProtocolType == ProtocolType.Udp)
            //{
                
            //}

            if (socket.AddressFamily == AddressFamily.InterNetwork) socket.DontFragment = true;

            //Better performance for 1 core...
            if (System.Environment.ProcessorCount <= 1) socket.UseOnlyOverlappedIO = true;


            if (object.ReferenceEquals(prototype, null) || (socket.Handle == prototype.Handle).Equals(false))
            {
                //Set or reset the recieve timeout
                socket.ReceiveTimeout = DefaultReceiveTimeout;

                //Set or reset the send timeout
                socket.SendTimeout = DefaultSendTimeout;

                //Get the interframegap
                interFrameGap = (int)Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(socket));

                //Use 1/10 th of the total inter packet gap
                interFrameGap /= 10;

            }
            else
            {
                //Caulcate the poll timeout. use one tick causes higher cpu for now but reduces poll collisions.
                //10 ticks / 1μs  is missing data in high bandwidth environments with receive buffer = 0

                double InterframeGapμs = Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(prototype));

                System.TimeSpan InterframeGap = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.FromMicroseconds(InterframeGapμs);

                interFrameGap = (int)InterframeGapμs;

                //(int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);  
                //(int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond);  

                //Use 1/10 th of the total inter packet gap
                interFrameGap /= 10;

                //m_SocketPollMicroseconds >>= 2;

                //Ensure to use whatever is smaller, the inter packet gap or the default of 50,000 microseconds
                interFrameGap = Media.Common.Binary.Min(interFrameGap, (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(RtspClient.DefaultConnectionTime));

                //Set the send and receive timeout from the default connection time, it will backoff as required
                //m_RtspSocket.SendTimeout = m_RtspSocket.ReceiveTimeout = (int)RtspClient.DefaultConnectionTime.TotalMilliseconds;

                prototype.SendTimeout = prototype.ReceiveTimeout = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.Max(RtspClient.DefaultConnectionTime, InterframeGap).TotalMilliseconds;

                //Create a buffer using the size of the largest message possible without a Content-Length header.
                //This helps to ensure that partial messages are not recieved by the server from a client if possible (should eventually allow much smaller)                
            }

        }

        #region Fields

        DateTime? m_Started;

        /// <summary>
        /// The port the RtspServer is listening on, defaults to 554
        /// </summary>
        internal int m_ServerPort = DefaultPort;
            //Counters for bytes sent and recieved
        internal long m_Recieved, m_Sent;

        internal IPAddress m_ServerIP;

        /// <summary>
        /// The socket used for recieving RtspRequests
        /// </summary>
        Socket m_TcpServerSocket, m_UdpServerSocket;

        /// <summary>
        /// The maximum number of clients allowed to be connected at one time.
        /// </summary>
        int m_MaximumSessions = short.MaxValue; //32767

        /// <summary>
        /// The amount of clients to allow in the Accept stte
        /// </summary>
        int m_MaximumConnections = -1;

        /// <summary>
        /// The version of the Rtsp protocol in use by the server
        /// </summary>
        double m_Version = 1.0;

        /// <summary>
        /// The HttpListner used for handling Rtsp over Http
        /// Todo, use Socket on Designated Port
        /// </summary>
        HttpListener m_HttpListner;

        /// <summary>
        /// The endpoint the server is listening on
        /// </summary>
        EndPoint m_ServerEndPoint;

        /// <summary>
        /// The dictionary containing all streams the server is aggregrating
        /// </summary>
        Dictionary<Guid, Media.Rtsp.Server.IMedia> m_MediaStreams = new Dictionary<Guid, Media.Rtsp.Server.IMedia>();

        /// <summary>
        /// The dictionary containing all the clients the server has sessions assocaited with
        /// </summary>
        readonly Dictionary<Guid, ClientSession> m_Sessions = new Dictionary<Guid, ClientSession>(short.MaxValue);

        readonly ManualResetEventSlim m_AcceptSignal = new ManualResetEventSlim(false, DefaultReceiveTimeout);

        /// <summary>
        /// The thread allocated to handle socket communication
        /// </summary>
        Thread m_ServerThread;

        /// <summary>
        /// Indicates to the ServerThread a stop has been requested
        /// </summary>
        bool m_StopRequested, m_Maintaining;

        //Handles the Restarting of streams which needs to be and disconnects clients which are inactive.
        internal Timer m_Maintainer;

        #endregion

        #region Propeties

        public Media.Rtsp.Server.RtspStreamArchiver Archiver
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        internal IEnumerable<ClientSession> Clients
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                try
                {
                    return m_Sessions.Values.ToList();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Stores Uri prefixes and their associated credentials.
        /// </summary>
        internal CredentialCache RequiredCredentials
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// The Version of the RtspServer (used in responses)
        /// </summary>
        public double Version
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Version; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set
            {
                //Once set higher it cannot be set lower...
                if (value < m_Version) throw new ArgumentOutOfRangeException(); 
                
                m_Version = value;
            }
        }

        /// <summary>
        /// Indicates if requests require a User Agent
        /// </summary>
        public bool RequireUserAgent
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// The name of the server (used in responses)
        /// </summary>
        public string ServerName
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// The amount of time before the RtpServer will remove a session if no Rtsp activity has occured.
        /// </summary>
        /// <remarks>Probably should back this property and ensure that the value is !0</remarks>
        public TimeSpan RtspClientInactivityTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        //Check access on these values, may not need to make them available public

        /// <summary>
        /// Gets or sets the ReceiveTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int ReceiveTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_TcpServerSocket.ReceiveTimeout; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_TcpServerSocket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the SendTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int SendTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_TcpServerSocket.SendTimeout; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_TcpServerSocket.SendTimeout = value; }
        }

        //For controlling Port ranges, Provide events so Upnp support can be plugged in? PortClosed/PortOpened(ProtocolType, startPort, endPort?)
        public int? MinimumUdpPort
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        internal int? MaximumUdpPort
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// The maximum amount of connected clients
        /// </summary>
        public int MaximumSessions
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MaximumSessions; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { if (value <= 0) throw new ArgumentOutOfRangeException(); m_MaximumSessions = value; }
        }

        /// <summary>
        /// The maximum length of the pending connections queue.
        /// </summary>
        public int MaximumConnections
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MaximumConnections; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { if (value <= 0) value = -1; m_MaximumConnections = value; }
        }

        /// <summary>
        /// The amount of time the server has been running
        /// </summary>
        public TimeSpan Uptime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Started.HasValue ? DateTime.UtcNow - m_Started.Value : TimeSpan.Zero; }
        }

        /// <summary>
        /// Indicates if the RtspServer has a worker process which is actively listening for requests on the ServerPort.
        /// </summary>
        /// <notes>m_TcpServerSocket.IsBound could also be another check or property.</notes>
        public bool IsRunning
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return false == IsDisposed &&
                    false == m_StopRequested &&
                    m_Started.HasValue &&
                     m_ServerThread != null;

                #region Unused IsRunning

                //Applied from
                //https://msdn.microsoft.com/en-us/library/system.threading.threadstate%28v=vs.110%29.aspx
                //(m_ServerThread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0;

                #endregion
            }
        }

        /// <summary>
        /// The port in which the RtspServer is listening for requests
        /// </summary>
        public int ServerPort
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ServerPort; }
        }

        /// <summary>
        /// If <see cref="IsRunning"/> is true and the server's socket is Bound.
        /// The local endpoint for this RtspServer (The endpoint on which requests are recieved)        
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return IsRunning && m_TcpServerSocket != null && m_TcpServerSocket.IsBound ? m_TcpServerSocket.LocalEndPoint as IPEndPoint : null; }
        }

        //Todo make sure name is correct.

        /// <summary>
        /// The streams contained in the server
        /// </summary>
        public IEnumerable<Media.Rtsp.Server.IMedia> MediaStreams
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                try
                {
                    return m_MediaStreams.Values.ToList();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// The amount of streams configured the server.
        /// </summary>
        public int TotalStreamCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MediaStreams.Count; }
        }

        /// <summary>
        /// The amount of active streams the server is listening to
        /// </summary>
        public int ActiveStreamCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {                
                if (TotalStreamCount == 0) return 0;
                return MediaStreams.Where(s => s.State == Media.Rtsp.Server.SourceMedia.StreamState.Started && s.Ready == true).Count();
            }
        }
        
        /// <summary>
        /// The total amount of bytes the RtspServer recieved from remote RtspRequests
        /// </summary>
        public long TotalRtspBytesRecieved
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Recieved; }
        }

        /// <summary>
        /// The total amount of bytes the RtspServer sent in response to remote RtspRequests
        /// </summary>
        public long TotalRtspBytesSent
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Sent; }
        }

        /// <summary>
        /// The amount of bytes sent or recieved from all contained streams in the RtspServer (Might want to log the counters seperately so the totals are not lost with the streams or just not provide the property)
        /// </summary>
        public long TotalStreamedBytes
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //Right now this counter only indicate rtp/rtcp traffic, rtsp or other protocol level counters are excluded.
                //This also excludes http sources right now :(
                //This will eventually be different and subsequently easier to determine. (For instance if you wanted only rtsp or rtmp or something else).
                //IMedia should expose this in some aspect.
                return MediaStreams.OfType<RtpSource>().Sum(s => false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(s.RtpClient)) ? s.RtpClient.TotalBytesSent + s.RtpClient.TotalBytesReceieved : 0);
            }
        }

        //Todo, provide port information etc.

        /// <summary>
        /// Indicates the amount of active connections the server has accepted.
        /// </summary>
        public int ActiveConnections
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Sessions.Count; }
        }

        /// <summary>
        /// The <see cref="RtspServerLogger"/> used for logging in the server.
        /// </summary>
        public Rtsp.Server.RtspServerLogger Logger
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// The <see cref="RtspServerLogger"/> used for logging in each ClientSession
        /// </summary>
        public Rtsp.Server.RtspServerLogger ClientSessionLogger
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        public bool HttpEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_HttpPort != -1; }
        }

        public bool UdpEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_UdpPort != -1; }
        }

        #endregion

        #region Custom Request Handlers

        /// <summary>
        /// A function which creates a RtspResponse.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="response">The response created, (Can be null and nothing will be sent)</param>
        /// <returns>True if NO futher processing is required otherwise false.</returns>
        public delegate bool RtspRequestHandler(RtspMessage request, out RtspMessage response);

        //Should support multiple handlers per method, use ConcurrentThesarus

        internal readonly Dictionary<RtspMethod, RtspRequestHandler> m_RequestHandlers = new Dictionary<RtspMethod, RtspRequestHandler>();

        public bool TryAddRequestHandler(RtspMethod method, RtspRequestHandler handler)
        {
            try
            {
                AddRequestHandler(method, handler);

                return true;
            }
            catch (Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);

                return false;
            }
        }

        public void AddRequestHandler(RtspMethod method, RtspRequestHandler handler)
        {
            Exception any;

            if (Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryAdd(m_RequestHandlers, method, handler, out any).Equals(false))
            {
                Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Custom Handler already registered", any);
            }
        }

        public bool TryRemoveRequestHandler(RtspMethod method)
        {
            try
            {
                RemoveRequestHandler(method);

                return true;
            }
            catch(Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);

                return false;
            }
        }

        public void RemoveRequestHandler(RtspMethod method)
        {
            Exception any;

            if (Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(m_RequestHandlers, method, out any).Equals(false))
            {
                Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Custom Handler already removed", any);
            }
        }

        #endregion

        //Delegates for client session.... would be better to use API of RtspClient and start working on revising the server to use RtspClient.

        #region Constructor

        //Todo
        //Allow for joining of server instances to support multiple end points.

        public RtspServer(AddressFamily addressFamily = AddressFamily.InterNetwork, int listenPort = DefaultPort, bool shouldDispose = true)
            : this(new IPEndPoint(Media.Common.Extensions.Socket.SocketExtensions.GetFirstUnicastIPAddress(addressFamily), listenPort), shouldDispose) { }

        public RtspServer(IPAddress listenAddress, int listenPort, bool shouldDispose = true) 
            : this(new IPEndPoint(listenAddress, listenPort), shouldDispose) { }

        public RtspServer(IPEndPoint listenEndPoint, bool shouldDispose = true)
            :base(shouldDispose)
        {
            RtspClientInactivityTimeout = TimeSpan.FromSeconds(60);
            
            //Check syntax of Server header to ensure allowed format...
            ServerName = "ASTI Media Server RTSP\\" + Version.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture);
            
            m_ServerEndPoint = listenEndPoint;
            
            m_ServerIP = listenEndPoint.Address;
            
            m_ServerPort = listenEndPoint.Port;

            RequiredCredentials = new CredentialCache();            

            ConfigureThread = ConfigureRtspServerThread;            

            //Todo, NetworkInterface should be notified on various events...
            //Todo, ConfigureRtspServerSocket should be called again when the interface has been reset or the speed changes.

        }

        #endregion

        #region Methods

        #region Enable and Disable Alternate Transport Protocols

        int m_HttpPort = -1;

        int m_SocketPollMicroSeconds;

        //Testing with external requests... could also be used for in bound requests but state would be needed to seperate the logic
        protected virtual void ProcessHttpRtspRequest(IAsyncResult iar)
        {

            if (object.ReferenceEquals(iar,null).Equals(false)) return;

            var context = m_HttpListner.EndGetContext(iar);

            m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), null);

            if (object.ReferenceEquals(context, null)) return;

            if (context.Request.AcceptTypes.Any(t => string.Compare(t, "application/x-rtsp-tunnelled") == 0))
            {
                if (context.Request.HasEntityBody)
                {
                    using (var ips = context.Request.InputStream)
                    {
                        long size = context.Request.ContentLength64;

                        byte[] buffer = new byte[size];

                        int offset = 0;

                        while (size > 0)
                        {
                            size -= offset += ips.Read(buffer, offset, (int)size);
                        }

                        //Ensure the message is really Rtsp
                        RtspMessage request = new RtspMessage(System.Convert.FromBase64String(context.Request.ContentEncoding.GetString(buffer)));

                        //Check for validity
                        if (request.RtspMessageType != RtspMessageType.Invalid)
                        {
                            //Process the request when complete
                            if (request.IsComplete)
                            {
                                ProcessRtspRequest(request, new ClientSession(this, null), false);

                                return;
                            }

                        }
                    }
                }
                else //No body
                {
                    RtspMessage request = new RtspMessage(RtspMessageType.Request)
                    {
                        Location = new Uri(RtspMessage.ReliableTransportScheme + "://" + context.Request.LocalEndPoint.Address.ToString() +  context.Request.RawUrl),
                        RtspMethod = RtspMethod.DESCRIBE
                    };

                    foreach (var header in context.Request.Headers.AllKeys)
                    {
                        request.SetHeader(header, context.Request.Headers[header]);
                    }

                    ProcessRtspRequest(request, new ClientSession(this, null), false);
                }
            }
        }

        public void EnableHttpTransport(int port = 80) 
        {
            throw new NotImplementedException();

            //Must be admin...

            //Todo, use HttpClient / HttpServer

            if (m_HttpListner == null)
            {
                try
                {
                    m_HttpListner = new HttpListener();
                    m_HttpPort = port;
                    m_HttpListner.Prefixes.Add("http://*:" + port + "/");
                    m_HttpListner.Start();
                    m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), this);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error Enabling Http on Port '" + port + "' : " + ex.Message, ex);
                }
            }
        }

        
        /*
         2.0 Draft specifies that servers now respond with a 501 to any rtspu request, we can blame that on lack of interest...
         */

        //Impove this...
        int m_UdpPort = -1;

        //Should allow multiple endpoints for any type of service.

        public void EnableUnreliableTransport(int port = RtspMessage.UnreliableTransportDefaultPort, AddressFamily addressFamily = AddressFamily.InterNetwork) 
        {
            //Should be a list of EndPoints.

            if (m_UdpServerSocket == null)
            {
                try
                {
                    m_UdpPort = port;

                    EndPoint inBound;

                    m_UdpServerSocket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
                    m_UdpServerSocket.Bind(inBound = new IPEndPoint(Media.Common.Extensions.Socket.SocketExtensions.GetFirstUnicastIPAddress(addressFamily), port));

                    //Include the IP Header
                    m_UdpServerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    //Maybe use Peek
                    m_UdpServerSocket.BeginReceiveFrom(Media.Common.MemorySegment.EmptyBytes, 0, 0, SocketFlags.Partial, ref inBound, ProcessAccept, m_UdpServerSocket);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error Enabling Udp on Port '" + port + "' : " + ex.Message, ex);
                }
            }
        }

        public void DisableHttpTransport()
        {
            //Existing sessions will still be active
            if (m_HttpListner != null)
            {
                m_HttpListner.Stop();
                m_HttpListner.Close();
                m_HttpListner = null;
            }
        }

        public void DisableUnreliableTransport()
        {
            //Existing sessions will still be active
            if (m_UdpServerSocket != null)
            {
                m_UdpServerSocket.Shutdown(SocketShutdown.Both);
                m_UdpServerSocket.Dispose();
                m_UdpServerSocket = null;
            }
        }

        #endregion

        #region Session Collection

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryAddSession(ClientSession session)
        {
            Exception any = null;

            try
            {
                Common.ILoggingExtensions.Log(Logger, "Adding Client: " + session.Id);

                if (Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryAdd(m_Sessions, session.Id, session, out any))
                {
                    session.m_Contained = this;
                }

                return Object.ReferenceEquals(session.m_Contained, this);
            }
            catch(Exception ex)
            {
                any = ex;

                return false;
            }
            finally
            {
                Common.ILoggingExtensions.LogException(Logger, any);
            }
        }

        /// <summary>
        /// Disposes and tries to remove the session from the Clients Collection.
        /// </summary>
        /// <param name="session">The session to remove</param>
        /// <returns>True if the session was disposed</returns>
        internal bool TryDisposeAndRemoveSession(ClientSession session)
        {
            if (session == null) return false;

            Exception any = null;
            try
            {
                //If the session was not disposed
                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(session)))
                {
                    //indicate the session is disposing
                    Common.ILoggingExtensions.Log(Logger, "Disposing Client: " + session.Id + " @ " + DateTime.UtcNow);

                    //Dispose the session
                    session.Dispose();
                }

                //If the client was already removed indicate this in the logs
                if (false.Equals(Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(m_Sessions, session.Id, out any))) Common.ILoggingExtensions.Log(Logger, "Client Already Removed(" + session.Id + ")");

                //Indicate success
                return true;
            }
            catch (Exception ex)
            {
                any = ex;

                //Indicate if a failure occured
                return Common.IDisposedExtensions.IsNullOrDisposed(session);
            }
            finally
            {
                Common.ILoggingExtensions.LogException(Logger, any);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool ContainsSession(ClientSession session)
        {
            return m_Sessions.ContainsKey(session.Id);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal ClientSession GetSession(Guid id)
        {
            ClientSession result;
            m_Sessions.TryGetValue(id, out result);
            return result;
        }

        /* 3.4 Session Identifiers
        -
            Session identifiers are opaque strings of arbitrary length. Linear
            white space must be URL-escaped. A session identifier MUST be chosen
            randomly and MUST be at least eight octets long to make guessing it
            more difficult. (See Section 16.) */

        /// <summary>
        /// Returns any session which has been asssigned the given <see cref="ClientSession.SessionId"/>
        /// </summary>
        /// <param name="rtspSessionId">The value which usually comes from the 'Session' header</param>
        /// <returns>Any clients which have been assigned the given <see cref="ClientSession.SessionId"/> </returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<ClientSession> GetSessions(string rtspSessionId)
        {
            //If there was no id then nothing can be returned
            if (string.IsNullOrWhiteSpace(rtspSessionId)) return Enumerable.Empty<ClientSession>();

            //Trim the id given to ensure no whitespace is present.
            rtspSessionId = rtspSessionId.Trim();

            //Return all clients which match the given id.
            return Clients.Where(c => string.Equals (rtspSessionId, c.SessionId));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<ClientSession> GetSessions(System.IntPtr handle)
        {
            //Return all clients which match the given handle.
            return Clients.Where(c => false.Equals(c.IsDisconnected) && object.ReferenceEquals(c.m_RtspSocket, null).Equals(false) && c.m_RtspSocket.Handle == handle);
        }     

        #endregion

        #region Stream Collection

        /// <summary>
        /// Adds a stream to the server. If the server is already started then the stream will also be started
        /// </summary>
        /// <param name="location">The uri of the stream</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryAddMedia(Media.Rtsp.Server.IMedia stream)
        {
            Exception any = null;
            try
            {
                //Try to add the stream to the dictionary of contained streams.
                bool result = Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryAdd(m_MediaStreams, stream.Id, stream, out any);

                if (result && IsRunning) stream.Start();

                return result;
            }
            catch(Exception ex)
            {
                any = ex;
                //Possibly already added.

                //If we are listening start the stram
                if (IsRunning && ContainsMedia(stream.Id))
                {
                    stream.Start();

                    return true;
                }

                return false;
            }
            finally
            {
                Common.ILoggingExtensions.LogException(Logger, any);
            }
        }

        /// <summary>
        /// Indicates if the RtspServer contains the given streamId
        /// </summary>
        /// <param name="streamId">The id of the stream</param>
        /// <returns>True if the stream is contained, otherwise false</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool ContainsMedia(Guid streamId)
        {
            return m_MediaStreams.ContainsKey(streamId);
        }

        /// <summary>
        /// Stops and Removes a stream from the server
        /// </summary>
        /// <param name="streamId">The id of the stream</param>
        /// <param name="stop">True if the stream should be stopped when removed</param>
        /// <returns>True if removed, otherwise false</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryRemoveMedia(Guid streamId, bool stop = true)
        {
            Exception any = null;
            try
            {
                Media.Rtsp.Server.IMedia source = GetStream(streamId);

                if (source != null && stop) source.Stop();

                if (RequiredCredentials != null)
                {
                    RemoveCredential(source, "Basic");
                    RemoveCredential(source, "Digest");
                }

                return Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(m_MediaStreams, streamId, out any);
            }
            catch(Exception ex)
            {
                any = ex;

                return false;
            }
            finally
            {
                Common.ILoggingExtensions.LogException(Logger, any);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Media.Rtsp.Server.IMedia GetStream(Guid streamId)
        {
            Media.Rtsp.Server.IMedia result;
            m_MediaStreams.TryGetValue(streamId, out result);
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="mediaLocation"></param>
        /// <returns></returns>
        internal Media.Rtsp.Server.IMedia FindStreamByLocation(Uri mediaLocation)
        {
            Media.Rtsp.Server.IMedia found = null;

            string streamBase = null, streamName = null;

            streamBase = mediaLocation.Segments.Any(s=>s.ToLowerInvariant().Contains("live")) ? "live" : "archive";

            streamName = mediaLocation.Segments.Last().ToLowerInvariant().Replace("/", string.Empty).Trim();

            Sdp.MediaType mediaType;

            int trackId, symbolIndex;

            if (string.IsNullOrWhiteSpace(streamName)) streamName = mediaLocation.Segments[mediaLocation.Segments.Length - 1].ToLowerInvariant().Replace("/", string.Empty).Trim();
            else if ((symbolIndex = streamName.IndexOf('=')) >= 0 && int.TryParse(Media.Common.ASCII.ExtractNumber(streamName, symbolIndex, streamName.Length), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out trackId) 
                     ||
                     Enum.TryParse<Sdp.MediaType>(streamName, out mediaType)) streamName = mediaLocation.Segments[mediaLocation.Segments.Length - 2].ToLowerInvariant().Replace("/", string.Empty).Trim();

            //If either the streamBase or the streamName is null or Whitespace then return null (no stream)
            if (string.IsNullOrWhiteSpace(streamBase) || string.IsNullOrWhiteSpace(streamName)) return null;

            //handle live streams
            if (streamBase.Equals("live", StringComparison.InvariantCultureIgnoreCase))
            {
                int diff;

                foreach (Media.Rtsp.Server.IMedia stream in MediaStreams)
                {
                    //To lower is used to prevent certain issues

                    diff = string.Compare(stream.Name.ToLowerInvariant(), streamName, StringComparison.OrdinalIgnoreCase);

                    //If the name matches the streamName or stream Id then we found it
                    if (diff == 0)
                    {
                        found = stream;

                        break;
                    }

                    //Check the Id
                    diff = string.Compare(stream.Id.ToString().ToLowerInvariant(), streamName, StringComparison.OrdinalIgnoreCase);

                    if (diff == 0)
                    {
                        found = stream;

                        break;
                    }

                    //Try aliases of streams
                    if (found == null)
                    {
                        foreach (string alias in stream.Aliases)
                        {

                            diff = string.Compare(alias.ToLowerInvariant(), streamName, StringComparison.OrdinalIgnoreCase);

                            if (diff == 0)
                            {
                                found = stream;

                                break;
                            }

                        }
                    }

                    //Try by media description?

                }
            }
            else if(streamBase == "archive")
            {
                //Check MediaStreams for existing ArchivedMedia, if found return.

                //If Uri contains an Id then attempt lookup and return if found, otherwise

                //Find Archive File by Uri, ensure directly is allowed by checking enabled property based on file in dir.

                //Archiver.BaseDirectory contains the directory where all archived sources are kept, search for a directory name which matched the uri

                //Create a ArchivedMedia, add to the list of contained media if enabled and

                //Use Archiver.CanPlayback() / CreatePlayback.

                //Return found
            }
            //else if (streamBase == "playback") //maynot be needed if handled in archiver
            //{
            //    //Check existing archives being played back, there should be a property in the Archiver to seperate these.
            //}

            return found;
        }

        #endregion

        #region Server Logic

        /// <summary>
        /// Adds a Credential to the CredentialCache of the RtspServer for the given SourceStream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="credential"></param>
        /// <param name="authType"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void AddCredential(Media.Rtsp.Server.IMedia source, NetworkCredential credential, string authType)
        {
            RequiredCredentials.Add(source.ServerLocation, authType, credential);
        }

        /// <summary>
        /// Removes a Credential from the CredentialCache of the RtspServer for the given SourceStream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="authType"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void RemoveCredential(Media.Rtsp.Server.IMedia source, string authType)
        {
            RequiredCredentials.Remove(source.ServerLocation, authType);
        }

        /// <summary>
        /// Finds and removes inactive clients.
        /// Determined by the time of the sessions last RecieversReport or the last RtspRequestRecieved (get parameter must be sent to keep from timing out)
        /// </summary>
        internal void DisconnectAndRemoveInactiveSessions(object state = null) { DisconnectAndRemoveInactiveSessions(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void DisconnectAndRemoveInactiveSessions()
        {
            //Allow some small varaince
            DateTime maintenanceStarted = DateTime.UtcNow + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

            //Iterate each connected client
            foreach (ClientSession session in Clients)
            {
                if (session == null) continue;

                //Check for inactivity at the RtspLevel first and then the RtpLevel.
                if (session.Created > maintenanceStarted
                    || //Or if the LastRequest was created after maintenanceStarted
                    false.Equals(session.LastRequest == null) && session.LastRequest.Created > maintenanceStarted
                    || //Or if the LastResponse was created after maintenanceStarted
                    false.Equals(session.LastResponse == null) && session.LastResponse.Created > maintenanceStarted
                    || //Or if there were no resources released
                    false.Equals(session.IsDisconnected) && false == session.ReleaseUnusedResources())
                {
                    //Do not attempt to perform any disconnection of the session
                    continue;
                }

                //The session is disposed OR
                if (IDisposedExtensions.IsNullOrDisposed(session) || session.IsDisconnected
                    &&
                    //If the session has an attached source but no client OR the RtpClient is disposed or not active
                    session.Playing.Count >= 0 && (IDisposedExtensions.IsNullOrDisposed(session.m_RtpClient) || false.Equals(session.m_RtpClient.IsActive))
                    ||//There was no last request
                    session.LastRequest == null
                    ||//OR there is a last request AND
                    (false.Equals(session.LastRequest == null)
                    && //The last request was created longer ago than required to keep clients active
                    (maintenanceStarted - session.LastRequest.Created) > RtspClientInactivityTimeout)
                    ||//There was no last response
                    session.LastResponse == null
                    || //OR there is a last response AND
                    (false.Equals(session.LastResponse == null)
                    && //The last response was transferred longer ago than required to keep clients active
                    session.LastResponse.Transferred.HasValue 
                    && (maintenanceStarted - session.LastResponse.Transferred) > RtspClientInactivityTimeout))
                    
                {
                    //Dispose the session
                    TryDisposeAndRemoveSession(session);
                }
            }
        }


        /// <summary>
        /// Restarted streams which should be Listening but are not
        /// </summary>
        internal void RestartFaultedStreams(object state = null) { RestartFaultedStreams(); }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void RestartFaultedStreams()
        {
            foreach (Media.Rtsp.Server.IMedia stream in MediaStreams.Where(s => s.State == RtspSource.StreamState.Started && s.Ready == false))
            {

                Common.ILoggingExtensions.Log(Logger, "Stopping Stream: " + stream.Name + " Id=" + stream.Id);

                //Ensure Stopped
                stream.Stop();

                if (IsRunning)
                {
                    Common.ILoggingExtensions.Log(Logger, "Starting Stream: " + stream.Name + " Id=" + stream.Id);

                    //try to start it again
                    stream.Start();
                }
                
            }
        }        

        /// <summary>
        /// Starts the RtspServer and listens for requests.
        /// Starts all streams contained in the server
        /// </summary>
        public virtual void Start(bool allowAddressReuse = false, bool allowPortReuse = false)
        {
            //Dont allow starting when disposed
            CheckDisposed();

            //If we already have a thread return
            if (IsRunning) return;

            //Allowed to run
            m_Maintaining = m_StopRequested = false;

            //Indicate start was called
            Common.ILoggingExtensions.Log(Logger, "Server Started @ " + DateTime.UtcNow);

            //Create the server Socket
            m_TcpServerSocket = new Socket(m_ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if(object.ReferenceEquals(ConfigureSocket, null)) ConfigureSocket = (Socket server) =>
            {
                bool reconfigureSocket = false;

                ConfigureRtspServerSocket(m_TcpServerSocket, out m_SocketPollMicroSeconds, ref reconfigureSocket, m_UdpServerSocket);

                Media.Common.Extensions.Socket.SocketExtensions.ChangeTcpKeepAlive(m_TcpServerSocket, m_SocketPollMicroSeconds, m_SocketPollMicroSeconds);           
            };

            //Configure the socket
            ConfigureSocket(m_TcpServerSocket);

            //Bind the server Socket to the server EndPoint
            m_TcpServerSocket.Bind(m_ServerEndPoint);

            //Set the backlog
            m_TcpServerSocket.Listen(m_MaximumConnections);

            //Ensure the address can be re-used
            if(allowAddressReuse) Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(m_TcpServerSocket));

            //Windows >= 10 and Some Unix
            if (allowPortReuse) Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableUnicastPortReuse(m_TcpServerSocket));

            //Create a thread to handle client connections
            m_ServerThread = new Thread(new ThreadStart(AcceptLoop), Common.Extensions.Thread.ThreadExtensions.MinimumStackSize);
            
            //Configure the thread
            m_ServerThread.Name = ServerName + "@" + m_ServerPort;

            ConfigureThread(m_ServerThread);

            //Erase any prior stats
            m_Sent = m_Recieved = 0;

            //Start it
            m_ServerThread.Start();

            //Timer for maintaince ( one quarter of the ticks)
            m_Maintainer = new Timer(new TimerCallback(MaintainServer), null, TimeSpan.FromTicks(RtspClientInactivityTimeout.Ticks >> 2), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);

            if (m_UdpPort >= 0) EnableUnreliableTransport(m_UdpPort);

            if (m_HttpPort >= 0) EnableHttpTransport(m_HttpPort);

            //Start streaming from m_Streams before server can accept connections.
            StartStreams();

            while (m_Started.HasValue.Equals(false)) System.Threading.Thread.Sleep(0);

            Media.Common.Extensions.Socket.SocketExtensions.ChangeTcpKeepAlive(m_TcpServerSocket, m_SocketPollMicroSeconds, m_SocketPollMicroSeconds << 1);
        }

        /// <summary>
        /// Removes Inactive Sessions and Restarts Faulted Streams
        /// </summary>
        /// <param name="state">Reserved</param>
        internal virtual void MaintainServer(object state = null)
        {
            if (m_Maintaining || IsDisposed || m_StopRequested) return;

            if (IsRunning && false == m_Maintaining)
            {
                m_Maintaining = true;

                try
                {

                    new Thread(new ThreadStart(() =>
                    {

                        try
                        {
                            Common.ILoggingExtensions.Log(Logger, "RestartFaultedStreams");

                            RestartFaultedStreams(state);
                        }
                        catch (Exception ex)
                        {
                            //if (Logger != null) Logger.LogException(ex);
                            Common.ILoggingExtensions.LogException(Logger, ex);
                        }

                        try
                        {
                            Common.ILoggingExtensions.Log(Logger, "DisconnectAndRemoveInactiveSessions");

                            DisconnectAndRemoveInactiveSessions(state);
                        }
                        catch (Exception ex)
                        {
                            Common.ILoggingExtensions.LogException(Logger, ex);
                        }

                        foreach (Media.Rtsp.Server.IMedia readyStream in MediaStreams)
                        {
                            readyStream.TrySetLogger(Logger);
                        }

                        m_Maintaining = false;

                        if (IsRunning && m_Maintainer != null) m_Maintainer.Change(TimeSpan.FromTicks(RtspClientInactivityTimeout.Ticks >> 2), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);

                    }), Common.Extensions.Thread.ThreadExtensions.MinimumStackSize)
                    {
                        Priority = m_ServerThread.Priority
                    }.Start();
                }
                catch (Exception ex)
                {
                    Common.ILoggingExtensions.LogException(Logger, ex);

                    m_Maintaining = false.Equals(m_Maintainer == null);
                }
            }
        }

        bool m_LeaveOpen;

        /// <summary>
        /// Stops recieving RtspRequests and stops streaming all contained streams
        /// </summary>
        public virtual void Stop(bool leaveOpen = false)
        {
            //If there is not a server thread return
            if (false.Equals(IsRunning)) return;

            //Stop listening for new clients
            m_StopRequested = true;

            Common.ILoggingExtensions.Log(Logger, "@Stop - StopRequested");
            Common.ILoggingExtensions.Log(Logger, "Connected Clients:" + ActiveConnections);
            Common.ILoggingExtensions.Log(Logger, "Server Stopped @ " + DateTime.UtcNow);
            Common.ILoggingExtensions.Log(Logger, "Uptime:" + Uptime);
            Common.ILoggingExtensions.Log(Logger, "Sent:" + m_Sent);
            Common.ILoggingExtensions.Log(Logger, "Receieved:" + m_Recieved);
            
            //Todo, leaveOpen...

            //Stop other listeners
            DisableHttpTransport();

            DisableUnreliableTransport();

            //Stop maintaining the server
            if (false.Equals(m_Maintainer == null))
            {
                m_Maintainer.Dispose();

                m_Maintainer = null;

                //m_Maintaining = false;
            }            
            
            //Stop listening to source streams
            StopStreams();

            //Remove all clients
            foreach (ClientSession session in Clients)
            {
                //Remove the session
                TryDisposeAndRemoveSession(session);
            }

            //Abort the worker from receiving clients
            Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref m_ServerThread);

            //Dispose the server socket
            if (object.ReferenceEquals(m_TcpServerSocket, null).Equals(false))                
            {
                //Dispose the socket if not leaving it open...
                if (leaveOpen.Equals(false)) m_TcpServerSocket.Dispose();

                m_TcpServerSocket = null;
            }
          
            //Erase statistics
            m_Started = null;

            Common.ILoggingExtensions.Log(Logger, "@Stop.Complete");
        }

        /// <summary>
        /// Starts all streams contained in the video server in parallel
        /// </summary>
        internal virtual void StartStreams()
        {
            foreach (Media.Rtsp.Server.IMedia stream in MediaStreams)
            {
                if (m_StopRequested) return;

                if (Common.IDisposedExtensions.IsNullOrDisposed(stream) || stream.Disabled) continue;

                try
                {
                    new Thread(() =>
                    {
                        try
                        {
                            Common.ILoggingExtensions.Log(Logger, "Starting Stream: " + stream.Name + " Id=" + stream.Id);

                            stream.Start();
                        }
                        catch (Exception ex)
                        {
                            Common.ILoggingExtensions.LogException(Logger,ex);

                            Common.ILoggingExtensions.Log(Logger, "Cannot Start Stream: " + stream.Name + " Id=" + stream.Id);
                        }

                    }, Common.Extensions.Thread.ThreadExtensions.MinimumStackSize)
                    {
                        Priority = ThreadPriority.AboveNormal
                    }.Start();
                }
                catch(Exception ex)
                {
                    Common.ILoggingExtensions.LogException(Logger, ex);

                    continue;
                }
            }
        }

        /// <summary>
        /// Stops all contained streams from streaming in parallel
        /// </summary>
        internal virtual void StopStreams()
        {
            foreach (Media.Rtsp.Server.IMedia stream in MediaStreams)
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(stream) || stream.Disabled) continue;

                try
                {
                    new Thread(() =>
                    {
                        try
                        {
                            Common.ILoggingExtensions.Log(Logger, "Stopping Stream: " + stream.Name + " Id=" + stream.Id);

                            stream.Stop();
                        }
                        catch (Exception ex)
                        {
                            Common.ILoggingExtensions.LogException(Logger, ex);

                            Common.ILoggingExtensions.Log(Logger, "Cannot Stop Stream: " + stream.Name + " Id=" + stream.Id);
                        }
                    }, Common.Extensions.Thread.ThreadExtensions.MinimumStackSize)
                    {
                        Priority = ThreadPriority.AboveNormal
                    }.Start();
                }
                catch (Exception ex)
                {
                    Common.ILoggingExtensions.LogException(Logger, ex);
                    continue;
                }
            }
        }        

        /// <summary>
        /// The loop where Tcp connections are accepted
        /// </summary>
        internal void AcceptLoop()
        {
            IAsyncResult lastAccept = null;

            //Indicate Thread Started
            m_Started = DateTime.UtcNow;

            DateTime lastAcceptStarted = m_Started.Value;

            int timeOut, halfTimeout;

        Begin:            

            try
            {
                //Get the timeout
                timeOut = m_TcpServerSocket.ReceiveTimeout;

                //If the timeout is infinite only wait for the default
                if (timeOut <= 0) timeOut = DefaultReceiveTimeout;

                //The timeout is always half of the total.
                halfTimeout = timeOut >>= 1;

                //While running
                while (IsRunning)
                {
                    if (m_StopRequested) break;
                    else if (m_Sessions.Count < m_MaximumSessions)
                    {
                        //If not already accepting
                        if (lastAccept == null || lastAccept.IsCompleted)
                        {
                            //Start a thread acceping with a 0 size buffer
                            lastAccept = m_TcpServerSocket.BeginAccept(0, new AsyncCallback(ProcessAccept), m_TcpServerSocket);

                            //Sample the clock
                            lastAcceptStarted = DateTime.UtcNow;
                        }

                        //Ensure lowest priority
                        System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                        #region Unused[Using Wait Handle]

                        ////use the handle on the already allocated result which was obtained by calling BeginAccept.
                        //using (WaitHandle handle = lastAccept.AsyncWaitHandle)
                        //{
                        //    //Wait half the timeout using the event
                        //    while (IsRunning && false.Equals(lastAccept.IsCompleted) && false.Equals(handle.WaitOne(halfTimeout)))
                        //    {

                        //        if (accept.Wait(halfTimeout)) break;

                        //        ////Don't wait longer than the full timeout
                        //        //if (((int)(DateTime.UtcNow - lastAcceptStarted).TotalMilliseconds) >= timeOut)
                        //        //{
                        //        //    //Force the accept thread to end now
                        //        //    System.Net.Sockets.Socket stale = m_TcpServerSocket.EndAccept(lastAccept);

                        //        //    //If there is a socket
                        //        //    if (false.Equals(stale == null))
                        //        //    {
                        //        //        //Dispose it
                        //        //        stale.Dispose();
                        //        //    }
                        //        //}
                        //    }
                        //}

                        #endregion

                        //While running and not completed, wait timeOut
                        while (false.Equals(lastAccept.IsCompleted) &&
                            false.Equals(lastAccept.AsyncWaitHandle.WaitOne(halfTimeout)) &&
                            IsRunning)
                        {
                            //If the signal was recieved then stop looping
                            if (m_AcceptSignal.Wait(halfTimeout)) break;
                        }

                        //Reset the signal
                        m_AcceptSignal.Reset();

                        //Ensure below normal priorty
                        System.Threading.Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                    }
                    else
                    {
                        //Try to defer to another thread and if not possible sleep
                        if(false.Equals(System.Threading.Thread.Yield())) System.Threading.Thread.Sleep(timeOut);
                        else System.Threading.Thread.Sleep(halfTimeout);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                //Stop is now requested
                m_StopRequested = true;

                //Handle the abort
                Thread.ResetAbort();

                //All workers threads which exist now exit.
                return;
            }
            catch (Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);

                if (m_StopRequested) return;

                goto Begin;
            }
        }

        #endregion

        #region Socket Methods

        /// <summary>
        /// Handles the accept of rtsp client sockets into the server
        /// </summary>
        /// <param name="ar">IAsyncResult with a Socket object in the AsyncState property</param>
        internal void ProcessAccept(IAsyncResult ar)
        {
            try
            {
                //Signal to start another thread to accept another client
                m_AcceptSignal.Set();

                int interframeGap = 0;
                
                bool reconfigure = false;

                if (object.ReferenceEquals(ar, null) || false.Equals(ar.IsCompleted)) return;

                //The Socket needed to create a ClientSession
                Socket clientSocket = null;

                //See if there is a socket in the state object
                Socket server = (Socket)ar.AsyncState;

                //If there is no socket then an accept has cannot be performed
                if (object.ReferenceEquals(server, null)) return;

                //If this is the inital receive for a Udp or the server given is UDP
                if (server.ProtocolType == ProtocolType.Udp)
                {
                    //Should always be 0 for our server any servers passed in
                    int acceptBytes = server.EndReceive(ar);

                    //Create a new socket for the client
                    clientSocket = new Socket(server.RemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    //Bind it
                    clientSocket.Bind(server.LocalEndPoint);

                    //Connect it
                    clientSocket.Connect(server.RemoteEndPoint);

                    ConfigureRtspServerSocket(server, out interframeGap, ref reconfigure, clientSocket);

                    //Start receiving again if this was our server
                    if (m_UdpServerSocket.Handle == server.Handle)
                        m_UdpServerSocket.BeginReceive(Media.Common.MemorySegment.EmptyBytes, 0, 0, SocketFlags.Partial, ProcessAccept, m_UdpServerSocket);
                }
                else if (server.ProtocolType == ProtocolType.Tcp) //Tcp
                {
                    //The clientSocket is obtained from the EndAccept call, possibly bytes ready from the accept
                    //They are not discarded just not receieved until the first receive.
                    if (server.IsBound && IsRunning)
                    {
                        clientSocket = server.EndAccept(ar);
                        
                        ConfigureRtspServerSocket(server, out interframeGap, ref reconfigure, clientSocket);
                    }
                }
                else
                {
                    throw new Exception("This server can only accept connections from Tcp or Udp sockets");
                }

                //there must be a client socket.
                if (object.ReferenceEquals(clientSocket, null)) return;                

                //If the server is not runing dispose any connected socket
                if (m_Sessions.Count >= m_MaximumSessions || false.Equals(IsRunning))
                {
                    //If there is a logger then indicate what is happening
                    Common.ILoggingExtensions.Log(Logger, "Accepted Socket @ " + clientSocket.LocalEndPoint + " From: " + clientSocket.RemoteEndPoint + " Disposing. ConnectedClient=" + m_Sessions.Count);

                    //Could send a response 429 or 453...

                    //Todo, keep the message allocated?
                    //use a temporary message
                    using (RtspMessage tempMsg = new Rtsp.RtspMessage(RtspMessageType.Response, this.Version)
                    {
                        StatusCode = 429, //https://tools.ietf.org/html/rfc6585
                        ReasonPhrase = "Shutting Down / Too Many Clients", //Check count again to give specific reason
                        //Body = "We only alllow " + m_MaximumConnections + " to this server..."
                    })
                    {

                        //Check count again to give specific time..
                        //https://www.ietf.org/mail-archive/web/mmusic/current/msg08350.html

                        //Tell the clients when they should attempt to retry again.
                        tempMsg.AppendOrSetHeader(RtspHeaders.RetryAfter, ((int)(RtspClientInactivityTimeout.TotalSeconds)).ToString());

                        //If the session is too much just send out normally.
                        clientSocket.Send(tempMsg.ToBytes());

                        clientSocket.Dispose();

                        //Send it, Should have flag not to begin receive again?
                        //ProcessSendRtspMessage(tempMsg, tempSession, true);
                    }

                    //The server is no longer running.
                    return;
                }

                //Allocate a session, copy 12 bytes + into the stack
                ClientSession session = new ClientSession(this, clientSocket, null, true, interframeGap, true);

                //Try to add it now
                bool added = TryAddSession(session);

                //If there is a logger log the accept
                Common.ILoggingExtensions.Log(Logger, "Accepted Client: " + session.Id + " @ " + session.RemoteEndPoint + System.Environment.NewLine + session.Created + " Added =" + added);

                #region Unused Signal

                //Signal the handle and wait for it to be disposed
                //System.Threading.WaitHandle.SignalAndWait(ar.AsyncWaitHandle, ar.AsyncWaitHandle, DefaultReceiveTimeout, true);

                #endregion

            }
            catch (Exception ex)
            {
                //If there is a logger log the exception
                Common.ILoggingExtensions.LogException(Logger, ex);                
            }
        }

        /// <summary>
        /// Handles the recieving of sockets data from a ClientSession's RtspSocket
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessReceive(IAsyncResult ar)
        {
            if (object.ReferenceEquals(ar, null) || 
                ar.IsCompleted.Equals(false) || 
                IsRunning.Equals(false)) return;

            //Get the client information from the result
            ClientSession session = (ClientSession)ar.AsyncState;

            //Check if the recieve can proceed.
            if (Common.IDisposedExtensions.IsNullOrDisposed(session) ||
                session.IsDisconnected || 
                session.HasRuningServer.Equals(false)) return;

            try
            {
                //Take note of where we are receiving from
                EndPoint inBound = session.RemoteEndPoint;

                //Declare how much was recieved
                int received = session.m_RtspSocket.EndReceiveFrom(ar, ref inBound);                
                
                //If we received any application layer data
                if (received > 0)
                {

                    //If there is no session return or the session was just disposed
                    if (IDisposedExtensions.IsNullOrDisposed(session) || object.ReferenceEquals(session.m_RtspSocket, null)) return;

                    //Let the client propagate the message if the socket is shared
                    if (session.SharesSocket)
                    {
                        received -= session.m_RtpClient.ProcessFrameData(session.m_Buffer.Array, session.m_Buffer.Offset, received, session.m_RtspSocket);

                        return;
                    }

                    //The session is not disconnected 
                    session.IsDisconnected = false;

                    //Count for the server
                    m_Recieved += received;

                    //Count for the client
                    session.m_Receieved += received;

                    //Process the data in the buffer
                    ProcessClientBuffer(session, received);
                }
            }
            catch (Exception ex)
            {
                //Something happened during the session
                Common.ILoggingExtensions.LogException(Logger, ex);

                //if a socket exception occured then handle it.
                if (IDisposedExtensions.IsNullOrDisposed(session).Equals(false) && ex is SocketException) HandleClientSocketException((SocketException)ex, session);                                     
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal void ProcessClientBuffer(ClientSession session, int received)
        {
            if (received <= 0 || IDisposedExtensions.IsNullOrDisposed(session) || session.IsDisconnected) return;

            try
            {
                //Use a segment around the data received which is already in the buffer.
                using (Common.MemorySegment data = new Common.MemorySegment(session.m_Buffer.Array, session.m_Buffer.Offset, received))
                {
                    //May be valid request data
                    //if (data[0].Equals(RtpClient.BigEndianFrameControl))
                    //{
                    //    if (Common.IDisposedExtensions.IsNullOrDisposed(session.m_RtpClient)) return;

                    //    received -= session.m_RtpClient.ProcessFrameData(session.m_Buffer.Array, session.m_Buffer.Offset, received, session.m_RtspSocket);
                    //}

                    if (received <= 0) return;

                    //using (Common.MemorySegment unprocessedData = new Common.MemorySegment(session.m_Buffer.Array, data.Offset + received, received))
                    //{
                    //Ensure the message is really Rtsp
                    RtspMessage request = new RtspMessage(data);

                    //Check for validity
                    if (request.RtspMessageType == RtspMessageType.Request)
                    {
                        //Process the request when complete
                        if (request.IsComplete)
                        {
                            ProcessRtspRequest(request, session);

                            return;
                        }

                    } //Otherwise if LastRequest is not disposed then attempt completion with the invalid data
                    else if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(session.LastRequest)))
                    {
                        //Indicate a CompleteFrom is occuring
                        Media.Common.ILoggingExtensions.Log(Logger, "Session:" + session.Id + " Attempting to complete previous mesage with buffer of " + data.Count + " bytes.");

                        //Attempt to complete it
                        received = session.LastRequest.CompleteFrom(null, data);

                        //If nothing was recieved then do nothing.
                        if (received.Equals(0))
                        {
                            Media.Common.ILoggingExtensions.Log(Logger, "Session:" + session.Id + "Did not use buffer of " + data.Count + " bytes.");
                        }
                        else
                        {
                            Media.Common.ILoggingExtensions.Log(Logger, "Session:" + session.Id + " used " + received + " of buffer bytes");
                        }

                        //Account for the received data
                        session.m_Receieved += received;

                        //Determine how to process the messge

                        switch (session.LastRequest.RtspMessageType)
                        {
                            case RtspMessageType.Response:
                            case RtspMessageType.Invalid:
                                {
                                    //Log the invalid request
                                    Media.Common.ILoggingExtensions.Log(Logger, "Received Invalid Message:" + request + " \r\nFor Session:" + session.Id);

                                    //Ensure the session is still connected and wait for more data.
                                    if (session.IsDisconnected.Equals(false))
                                    {
                                        session.SendRtspData(Media.Common.MemorySegment.EmptyBytes);
                                    }

                                    return;
                                }
                            case RtspMessageType.Request:
                                {
                                    //If the request is complete now then process it
                                    if (session.LastRequest.IsComplete)
                                    {
                                        //Process the request (it may not be complete yet)
                                        ProcessRtspRequest(session.LastRequest, session);
                                    }

                                    //return

                                    goto case RtspMessageType.Invalid;
                                }
                        }
                    }

                    //New request is being made...
                    if (Common.IDisposedExtensions.IsNullOrDisposed(session.LastRequest).Equals(false)) session.LastRequest.Dispose();

                    //Store it for now to allow completion.
                    session.LastRequest = request;

                    //Ensure the session is still connected.
                    if (session.m_RtspSocket.Available.Equals(0)) session.SendRtspData(Media.Common.MemorySegment.EmptyBytes);
                    else session.StartReceive();
                    //}
                }
            }
            catch(Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);
            }
        }

        internal void HandleClientSocketException(SocketException se, ClientSession cs)
        {
            if (object.ReferenceEquals(se, null) || Common.IDisposedExtensions.IsNullOrDisposed(cs)) return;

            Common.ILoggingExtensions.Log(Logger, "HandleClientSocketException@Client:" + cs.Id + "=" + se.SocketErrorCode.ToString());

            switch (se.SocketErrorCode)
            {
                case SocketError.TimedOut:
                    {
                        //If there is still a socket
                        if (object.ReferenceEquals(cs.m_RtspSocket, null).Equals(false))
                        {
                            //Clients interleaving shouldn't time out ever
                            if (cs.m_RtspSocket.ProtocolType == ProtocolType.Tcp)
                            {
                                if (cs.Playing.Count > 0)
                                {
                                    Common.ILoggingExtensions.Log(Logger, "Client:" + cs.Id + " Playing, SharesSocket = " + cs.SharesSocket);

                                    return;
                                }
                            }

                            //Increase send and receive timeout
                            cs.m_RtspSocket.SendTimeout = cs.m_RtspSocket.ReceiveTimeout += (int)(RtspClient.DefaultConnectionTime.TotalMilliseconds);

                            Common.ILoggingExtensions.Log(Logger, "Increased Client:" + cs.Id + " Timeouts to: " + cs.m_RtspSocket.SendTimeout);

                            //Try to receive again
                            cs.StartReceive();
                        }

                        return;
                    }
                case SocketError.NotConnected:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.Disconnecting:
                case SocketError.Shutdown:
                    {
                        Common.ILoggingExtensions.Log(Logger, "HandleClientSocketException@Client:" + cs.Id + ", Disposing");

                        TryDisposeAndRemoveSession(cs);

                        break;
                    }
                default:
                    {                        
                        if (cs.IsDisposed.Equals(false) &&
                            object.ReferenceEquals(cs.m_RtspSocket, null).Equals(false))
                        {
                            if (cs.IsDisconnected = cs.m_RtspSocket.Poll(cs.m_SocketPollMicroseconds, SelectMode.SelectWrite) && cs.m_RtspSocket.Poll(cs.m_SocketPollMicroseconds, SelectMode.SelectRead))
                            {
                                Common.ILoggingExtensions.Log(Logger, "Marking Client:" + cs.Id + " Disconnected");

                                cs.ReleaseUnusedResources();
                            }
                        }
                        
                        return;
                    }
            }
        }

        /// <summary>
        /// Handles the sending of responses to clients which made requests
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessSendComplete(IAsyncResult ar)
        {
            unchecked
            {
                if (object.ReferenceEquals(ar, null) || ar.IsCompleted.Equals(false)) return;

                ClientSession session = (ClientSession)ar.AsyncState;

                try
                {

                    //Don't do anything if the session cannot be acted on.
                    if (IDisposedExtensions.IsNullOrDisposed(session) || session.IsDisconnected || false.Equals(session.HasRuningServer)) return;

                    //Ensure the bytes were completely sent..
                    int sent = session.m_RtspSocket.EndSendTo(ar);

                    //See how much was needed.
                    int neededLength = session.m_SendBuffer.Length;

                    //Determine if the send was complete
                    if (sent >= neededLength)
                    {
                        session.m_Sent += sent;

                        m_Sent += sent;

                        //start receiving again
                        session.StartReceive();

                        return;
                    }
                    else
                    {
                        //Determine how much remains
                        int remains = neededLength - sent;

                        if (remains > 0)
                        {
                            //Start sending the rest of the data
                            session.SendRtspData(session.m_SendBuffer, sent, remains);
                        }

                        #region Unused [Handle remaining data with SocketFlags.Partial]

                        //Don't even get me started on Partial ...

                        //session.LastSend = session.m_RtspSocket.BeginSendTo(session.m_SendBuffer, sent, remains, SocketFlags.Partial, session.RemoteEndPoint, new AsyncCallback(ProcessSendComplete), session);

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Common.ILoggingExtensions.LogException(Logger, ex);

                    //handle the socket exception
                    if (IDisposedExtensions.IsNullOrDisposed(session).Equals(false) && ex is SocketException) HandleClientSocketException((SocketException)ex, session);
                }
            }
        }

        #endregion

        #region Rtsp Request Handling Methods

        /// <summary>
        /// Processes a RtspRequest based on the contents
        /// </summary>
        /// <param name="request">The rtsp Request</param>
        /// <param name="session">The client information</param>
        internal void ProcessRtspRequest(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            try
            {
                //Log the reqeust now
                if (Common.IDisposedExtensions.IsNullOrDisposed(Logger).Equals(false)) Logger.LogRequest(request, session);

                //Ensure we have a session and request and that the server is still running and that the session is still contained in this server
                if (IDisposedExtensions.IsNullOrDisposed(request) || IDisposedExtensions.IsNullOrDisposed(session))
                {
                    return;
                }//Ensure the session is added to the connected clients if it has not been already.
                else if (object.ReferenceEquals(session.m_Contained, null)) TryAddSession(session);
                
                //Check for session moved from another process.
                //else if (session.m_Contained != null && session.m_Contained != this) return;

                //Turtle power!
                if (request.MethodString.Equals("REGISTER", StringComparison.OrdinalIgnoreCase))
                {
                    //Send back turtle food.
                    ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, 
                        session.RtspSocket.ProtocolType == ProtocolType.Tcp ? "FU_WILL_NEVER_SUPPORT_THIS$00\a" : string.Empty, 
                        sendResponse);

                    return;
                }

                //All requests need the CSeq
                if (request.ContainsHeader(RtspHeaders.CSeq).Equals(false))
                {
                    //if(request.HeaderCount == 0)

                    //Send back a BadRequest.
                    ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, null, sendResponse);

                    return;
                }

                /*
                 12.37 Session

               This request and response header field identifies an RTSP session
               started by the media server in a SETUP response and concluded by
               TEARDOWN on the presentation URL. The session identifier is chosen by
               the media server (see Section 3.4). Once a client receives a Session
               identifier, it MUST return it for any request related to that
               session.  A server does not have to set up a session identifier if it
               has other means of identifying a session, such as dynamically
               generated URLs.
                 */

                //Determine if the request is specific to a session
                if (request.ContainsHeader(RtspHeaders.Session))
                {
                    //Determine what session is being acted on.
                    string requestedSessionId = request.GetHeader(RtspHeaders.Session);

                    //If there is a null or empty session id this request is invalid.
                    if (string.IsNullOrWhiteSpace(requestedSessionId))
                    {
                        //Send back a BadRequest.
                        ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, null, sendResponse);

                        return;
                    }
                    else requestedSessionId = requestedSessionId.Trim();

                    //If the given session does not have a sessionId or does not match the sessionId requested.
                    if (session.SessionId.Equals(requestedSessionId).Equals(false))
                    {
                        //Find any session which has the given id.
                        IEnumerable<ClientSession> matches = GetSessions(requestedSessionId);

                        //Atttempt to get the correct session
                        ClientSession correctSession = matches.LastOrDefault();//(c => false == c.IsDisconnected);

                        //If no session could be found by the given sessionId
                        if (Common.IDisposedExtensions.IsNullOrDisposed(correctSession))
                        {

                            //Indicate the session requested could not be found.
                            ProcessInvalidRtspRequest(session, RtspStatusCode.SessionNotFound, null, sendResponse);

                            return;
                        }
                        else //There was a session found by the given Id
                        {
                            //Indicate the last request of this session was as given
                            session.LastRequest = request;

                            //The LastResponse is updated to be the value of whatever the correctSessions LastResponse is
                            // session.LastResponse = correctSession.LastResponse;

                            //Process the request from the correctSession but do not send a response.
                            ProcessRtspRequest(request, correctSession, false);

                            session.LastResponse = correctSession.LastResponse;

                            //Take the created response and sent it to the new session using it as the last response.
                            ProcessSendRtspMessage(session.LastResponse, session, sendResponse);

                            return;
                        }
                    }
                }
                
                //Check for out of order or duplicate requests.
                if (Common.IDisposedExtensions.IsNullOrDisposed(session.LastRequest).Equals(false))
                {
                    //Out of order
                    if (request.CSeq < session.LastRequest.CSeq)
                    {
                        //Send back a BadRequest.
                        ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, null, sendResponse);

                        return;
                    }
                }


                //Dispose any last request.
                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(session.LastRequest))) session.LastRequest.Dispose();

                //Synchronize the server and client since this is not a duplicate
                session.LastRequest = request;

                //Determine if we support what the client requests in `Require` Header
                if (request.ContainsHeader(RtspHeaders.Require))
                {
                    //Certain features are requried... tcp etc.
                    //Todo ProcessRequired(
                }

                //If there is a body and no content-length
                if (string.IsNullOrWhiteSpace(request.Body).Equals(false) &&
                    request.ContainsHeader(RtspHeaders.ContentLength).Equals(false))
                {
                    //Send back a BadRequest.
                    ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, null, sendResponse);

                    return;
                }

                //Optional Checks

                //UserAgent
                if (RequireUserAgent && 
                    request.ContainsHeader(RtspHeaders.UserAgent).Equals(false))
                {
                    //Send back a BadRequest.
                    ProcessInvalidRtspRequest(session, Rtsp.RtspStatusCode.BadRequest, null, sendResponse);

                    return;
                }

                #region What a joke and a waste of time

                //Minor version reflects changes made to the protocol but not the 'general message parsing' `algorithm`

                //Thus, RTSP/2.4 is a lower version than RTSP/2.13, which in turn is lower than RTSP/12.3.
                //Leading zeros SHALL NOT be sent and MUST be ignored by recipients.

                #endregion

                //Version - Should check request.Version != Version and that earlier versions are supprted.
                if (request.Version > Version)
                {
                    //Todo, IfCanReceiveVersion(Version), 
                    //ConvertToMessageVersion(v)
                    //ConvertToProtocol(m, v)

                    ProcessInvalidRtspRequest(session, RtspStatusCode.RtspVersionNotSupported, null, sendResponse);

                    return;
                }

                //4.2.  RTSP IRI and URI, Draft requires 501 response for rtspu iri but not for udp sockets using a rtsp location....
                //Should check request.Location.Scheme to not be rtspu but it was allowed previously...                

                //If any custom handlers were registered.
                if (m_RequestHandlers.Count > 0)
                {
                    //Determine if there is a custom handler for the mthod
                    RtspRequestHandler custom;

                    //If there is
                    if (m_RequestHandlers.TryGetValue(request.RtspMethod, out custom))
                    {
                        //Then create the response
                        RtspMessage response;

                        //By invoking the handler, if true is returned
                        if (custom(request, out response))
                        {
                            //Use the response created by the custom handler
                            ProcessSendRtspMessage(response, session, sendResponse);                            

                            //Return because the custom handler has handled the request.
                            return;
                        }
                    }
                }

                //From this point if Authrorization is required and the stream exists
                //The server will responsd with AuthorizationRequired when it should NOT have probably respoded with that at this point.
                //The problem is that RequiredCredentails uses a Uri format by ID.
                //We could get a stream and then respond accordingly but that is how it currently works and it allows probing of streams which is what not desirable in some cases
                //Thus we have to use the location of the request and see if RequiredCredentials has anything which matches root.
                //This would force everything to have some type of authentication which would also be applicable to all lower level streams in the uri in the credential cache.
                //I could also change up the semantic and make everything Uri based rather then locations
                //Then it would also be easier to make /audio only passwords etc.

                //When stopping only handle teardown and keep alives
                if (m_StopRequested && 
                    (request.ContainsHeader(RtspHeaders.Connection).Equals(false) ||
                    (request.RtspMethod != RtspMethod.TEARDOWN && request.RtspMethod != RtspMethod.GET_PARAMETER && request.RtspMethod != RtspMethod.OPTIONS)))
                {
                    //Maybe a METHOD NOT VALID IN THIS STATE with a Reason Pharse "Shutting Down"
                    ProcessInvalidRtspRequest(session, RtspStatusCode.BadRequest, null, sendResponse);

                    return;
                }

                //Determine the handler for the request and process it
                switch (request.RtspMethod)
                {
                    case RtspMethod.ANNOUNCE: //C->S announcement...
                        {
                            //An Allow header field must be present in a 405 (Method not allowed) 
                            //This should be able to pre provided in the Invalid function via headers.
                            ProcessInvalidRtspRequest(session, RtspStatusCode.MethodNotAllowed, null, sendResponse);
                            break;
                        }
                    case RtspMethod.OPTIONS:
                        {
                            ProcessRtspOptions(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.DESCRIBE:
                        {
                            ProcessRtspDescribe(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.SETUP:
                        {
                            ProcessRtspSetup(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.PLAY:
                        {
                            ProcessRtspPlay(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.RECORD:
                        {
                            ProcessRtspRecord(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.PAUSE:
                        {
                            ProcessRtspPause(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.TEARDOWN:
                        {
                            ProcessRtspTeardown(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.GET_PARAMETER:
                        {
                            ProcessGetParameter(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.SET_PARAMETER:
                        {
                            ProcessSetParameter(request, session, sendResponse);
                            break;
                        }
                    case RtspMethod.REDIRECT: //Client can't redirect a server
                        {
                            ProcessInvalidRtspRequest(session, RtspStatusCode.BadRequest, null, sendResponse);
                            break;
                        }
                    case RtspMethod.UNKNOWN:
                    default:
                        {                            
                            //Per 2.0 Draft
                            ProcessInvalidRtspRequest(session, RtspStatusCode.NotImplemented, null, sendResponse);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                //Log it
                Common.ILoggingExtensions.LogException(Logger, ex);
            }
        }

        private void ProcessRtspRecord(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            var found = FindStreamByLocation(request.Location);

            if (Common.IDisposedExtensions.IsNullOrDisposed(found))
            {
                ProcessLocationNotFoundRtspRequest(session, sendResponse);
                return;
            }

            //Todo, pass session to auto update
            if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
            {
                ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                return;
            }
            //else
            //{
            //    session.LastAuthenticated = DateTime.UtcNow
            //}

            if (found.Ready.Equals(false) || Common.IDisposedExtensions.IsNullOrDisposed(Archiver))
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);
                return;
            }

            var resp = session.ProcessRecord(request, found);

            ProcessSendRtspMessage(resp, session, sendResponse);
        }

        internal void ProcessAnnounce(RtspMessage request, ClientSession session, bool sendResponse = true)
        {

            Uri announceLocation = new Uri(Rtsp.Server.SourceMedia.UriScheme + request.MethodString);

            if (AuthenticateRequest(request, announceLocation).Equals(false))
            {
                ProcessAuthorizationRequired(announceLocation, session, sendResponse);
                return;
            }

            //Check for body and content type, if SDP create and parse.

            //Determine if there is an existing Media and update, otherwise

            //Determine sink type, and create.

            //TryAddMedia

            //Create OK Response with Location header set if required.

        }

        internal void ProcessRedirect(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            var found = FindStreamByLocation(request.Location);

            if (Common.IDisposedExtensions.IsNullOrDisposed(found))
            {
                ProcessLocationNotFoundRtspRequest(session, sendResponse);
                return;
            }

            if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
            {
                ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                return;
            }

            if (found.Ready.Equals(false))
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);
                return;
            }


            var resp = session.CreateRtspResponse(request);
            resp.RtspMethod = RtspMethod.REDIRECT;
            resp.AppendOrSetHeader(RtspHeaders.Location, "rtsp://" + session.LocalEndPoint.Address.ToString() + "/live/" + found.Id);
            ProcessSendRtspMessage(resp, session, sendResponse);

        }

        #region Push Support

        internal void ProcessPushRtspMessage(RtspMessage message, ClientSession session, bool sendResponse = true)
        {
            ProcessSendRtspMessage(message, session, sendResponse);
        }

        internal void ProcessPushRtspMessage(RtspMessage message, IEnumerable<ClientSession> sessions, bool sendResponse = true)
        {
            foreach (ClientSession session in sessions)
            {
                ProcessSendRtspMessage(message, session, sendResponse);
            }
        }

        #endregion

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="message">The RtspResponse to send</param> If this was byte[] then it could handle http
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessSendRtspMessage(RtspMessage message, ClientSession session, bool sendResponse = true)
        {

            //Todo have a SupportedFeatures and RequiredFeatures HashSet.

            //Check Require Header
            //       And
            /* Add Unsupported Header if needed
            Require: play.basic, con.persistent, setup.playing
                       (basic play, TCP is supported)
            setup.playing means that setup and teardown can be used in the play state.
            method. is method support, method.announce, method.record
            */

            //If we have a session
            if (IDisposedExtensions.IsNullOrDisposed(session)) return;

            try
            {
                //if there is a message to send
                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(message)))
                {
                    //AddServerHeaders()->

                    //PrepareOutgoingMessage() ->

                    //@ProcessRtspMessage
                    //ValidateIncomingMessage() ->

                    if (false.Equals(message.ContainsHeader(RtspHeaders.Server))) message.SetHeader(RtspHeaders.Server, ServerName);

                    //if (false.Equals(message.ContainsHeader(RtspHeaders.Date))) message.SetHeader(RtspHeaders.Date, DateTime.UtcNow.ToString("r"));

                    //12.4 Allow
                    if (message.RtspStatusCode == RtspStatusCode.MethodNotAllowed && 
                        message.ContainsHeader(RtspHeaders.Allow).Equals(false))
                    {
                        ////Authenticated users can see allowed parameters but this is related to the current request URI
                        //Should determine what methods can be called on the current uri, GetAllowedMethods(session, uri)
                        //if (session.HasAuthenticated)
                        //{
                        //    message.SetHeader(RtspHeaders.Allow, "OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER, REDIRECT, ANNOUNCE, RECORD, SET_PARAMETER");
                        //}
                        //else
                        //{
                            message.SetHeader(RtspHeaders.Allow, "OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER, REDIRECT");
                        //}
                    }

                    #region RFC2326 12.38 Timestamp / Delay

                    /*
                         12.38 Timestamp

                           The timestamp general header describes when the client sent the
                           request to the server. The value of the timestamp is of significance
                           only to the client and may use any timescale. The server MUST echo
                           the exact same value and MAY, if it has accurate information about
                           this, add a floating point number indicating the number of seconds
                           that has elapsed since it has received the request. The timestamp is
                           used by the client to compute the round-trip time to the server so
                           that it can adjust the timeout value for retransmissions.

                           Timestamp  = "Timestamp" ":" *(DIGIT) [ "." *(DIGIT) ] [ delay ]
                           delay      =  *(DIGIT) [ "." *(DIGIT) ]
                         */

                    if (false.Equals(message.ContainsHeader(RtspHeaders.Timestamp)) &&
                        false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(session.LastRequest)) &&
                        session.LastRequest.ContainsHeader(RtspHeaders.Timestamp))
                    {
                        ////Apparently not joined with ;
                        ////message.SetHeader(RtspHeaders.Timestamp, session.LastRequest[RtspHeaders.Timestamp] + "delay=" + (DateTime.UtcNow - session.LastRequest.Created).TotalSeconds);

                        //Set the value of the Timestamp header as given
                        message.AppendOrSetHeader(RtspHeaders.Timestamp, session.LastRequest[RtspHeaders.Timestamp]);

                        //Add a delay datum
                        message.AppendOrSetHeader(RtspHeaders.Timestamp, "delay=" + (DateTime.UtcNow - session.LastRequest.Created).TotalSeconds);
                    }

                    #endregion

                    string sess = message.GetHeader(RtspHeaders.Session);

                    //Check for a session header, or add one if possible
                    if (string.IsNullOrWhiteSpace(sess).Equals(false))
                    {
                        //Add the timeout header if there was a session header.
                        if (RtspClientInactivityTimeout > TimeSpan.Zero &&
                            false.Equals(sess.Contains(RtspHeaderFields.Session.Timeout)))
                        {
                            message.AppendOrSetHeader(RtspHeaders.Session, RtspHeaderFields.Session.Timeout + "=" + (((int)(RtspClientInactivityTimeout.TotalSeconds)) >> 1).ToString());
                        }
                    }
                    else if(string.IsNullOrWhiteSpace(session.SessionId).Equals(false))
                    {
                        message.SetHeader(RtspHeaders.Session, session.SessionId + ";" +
                            RtspHeaderFields.Session.Timeout + "=" + (((int)(RtspClientInactivityTimeout.TotalSeconds)) >> 1).ToString());
                    }

                    //Oops
                    //if (session.m_RtspSocket.ProtocolType == ProtocolType.Tcp && session.Attached.Count > 0) response.SetHeader("Ignore", "$0\09\r\n$\0:\0");

                    //Dispose the last response
                    if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(session.LastResponse))) session.LastResponse.Dispose();

                    //Todo
                    //Content-Encoding should be the same as the request's if possible..

                    //Log response
                    Media.Common.ILoggingExtensions.Log(Logger, message.ToString());

                    //If sending a response
                    if (sendResponse) session.SendRtspData((session.LastResponse = message).ToBytes());
                    //Otherwise just update the property
                    else session.LastResponse = message;

                    //Indicate the session is not disconnected
                    //Indicate the last response was sent now.
                    session.IsDisconnected = session.IsDisposed.Equals(false) && (session.LastResponse.Transferred = DateTime.UtcNow).HasValue.Equals(false);
                }
                else
                {
                    //Test the connectivity and start another receive
                    if (session.m_RtspSocket.Available.Equals(0)) session.SendRtspData(Media.Common.MemorySegment.EmptyBytes);
                    else session.StartReceive();
                }
            }
            catch (Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);

                //if a socket exception occured then handle it.
                if (Common.IDisposedExtensions.IsNullOrDisposed(session).Equals(false) && ex is SocketException) HandleClientSocketException((SocketException)ex, session);
            }            
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="ci">The client session to send the response on</param>
        /// <param name="code">The status code of the response if other than BadRequest</param>
        /// //should allow reason phrase to be set
        //Should allow a header to be put into the response or a KeyValuePair<string,string> headers
        internal void ProcessInvalidRtspRequest(ClientSession session, RtspStatusCode code = RtspStatusCode.BadRequest, /*string reasonPhrase = null,*/ string body = null, bool sendResponse = true)
        {
            //Create and Send the response
            ProcessInvalidRtspRequest(Common.IDisposedExtensions.IsNullOrDisposed(session).Equals(false) ? session.CreateRtspResponse(null, code, body: body) : new RtspMessage(RtspMessageType.Response) { RtspStatusCode = code, Body = body, CSeq = Utility.Random.Next() }, session);
        }

        internal void ProcessInvalidRtspRequest(RtspMessage response, ClientSession session, bool sendResponse = true) { ProcessSendRtspMessage(response, session, sendResponse); }

        /// <summary>
        /// Sends a Rtsp LocationNotFound Response
        /// </summary>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessLocationNotFoundRtspRequest(ClientSession ci, bool sendResponse = true)
        {
            ProcessInvalidRtspRequest(ci, RtspStatusCode.NotFound, null, sendResponse);
        }

        internal virtual void ProcessAuthorizationRequired(Uri sourceLocation, ClientSession session, bool sendResponse = true)
        {

            RtspMessage response = new RtspMessage(RtspMessageType.Response);

            response.CSeq = session.LastRequest.CSeq;

            string authHeader = session.LastRequest.GetHeader(RtspHeaders.Authorization);

            RtspStatusCode statusCode;

            bool noAuthHeader = string.IsNullOrWhiteSpace(authHeader);

            if (noAuthHeader && session.LastRequest.ContainsHeader(RtspHeaders.AcceptCredentials)) statusCode = RtspStatusCode.ConnectionAuthorizationRequired;
            //If the last request did not have an authorization header
            else if (noAuthHeader)
            {
                /* -- http://tools.ietf.org/html/rfc2617
                 
    qop
     Indicates what "quality of protection" the client has applied to
     the message. If present, its value MUST be one of the alternatives
     the server indicated it supports in the WWW-Authenticate header.
     These values affect the computation of the request-digest. Note
     that this is a single token, not a quoted list of alternatives as
     in WWW- Authenticate.  This directive is optional in order to
     preserve backward compatibility with a minimal implementation of
     RFC 2617 [6], but SHOULD be used if the server indicated that qop
     is supported by providing a qop directive in the WWW-Authenticate
     header field.

   cnonce
     This MUST be specified if a qop directive is sent (see above), and
     MUST NOT be specified if the server did not send a qop directive in
     the WWW-Authenticate header field.  The cnonce-value is an opaque
     quoted string value provided by the client and used by both client
     and server to avoid chosen plaintext attacks, to provide mutual
     authentication, and to provide some message integrity protection.
     See the descriptions below of the calculation of the response-
     digest and request-digest values.     
                 
                 */

                //Could retrieve values from last Request if needed..
                //string realm = "//", nOnceCount = "00000001";

                //Should handle multiple types of auth

                //Should store the nonce and cnonce values on the session
                statusCode = RtspStatusCode.Unauthorized;

                NetworkCredential requiredCredential = null;

                string authenticateHeader = null;

                //Check for Digest first - Todo Finish implementation
                if (object.ReferenceEquals(requiredCredential = RequiredCredentials.GetCredential(sourceLocation, RtspHeaderFields.Authorization.Digest), null).Equals(false))
                {
                    //Might need to store values qop nc, cnonce and nonce in session storage for later retrival                    

                    //Should use auth-int and qop

                    authenticateHeader = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username={0},realm={1},nonce={2},cnonce={3}", requiredCredential.UserName, (string.IsNullOrWhiteSpace(requiredCredential.Domain) ? ServerName : requiredCredential.Domain), ((long)(Utility.Random.Next(int.MaxValue) << 32 | (Utility.Random.Next(int.MaxValue)))).ToString("X"), Utility.Random.Next(int.MaxValue).ToString("X"));
                }
                else if (object.ReferenceEquals(requiredCredential = RequiredCredentials.GetCredential(sourceLocation, RtspHeaderFields.Authorization.Basic), null).Equals(false))
                {
                    authenticateHeader = "Basic realm=\"" + (string.IsNullOrWhiteSpace(requiredCredential.Domain) ? ServerName : requiredCredential.Domain + '"');
                }

                if (string.IsNullOrWhiteSpace(authenticateHeader).Equals(false))
                {
                    response.SetHeader(RtspHeaders.WWWAuthenticate, authenticateHeader);
                }
            }
            else //Authorization header was present but data was incorrect
            {
                //Parse type from authHeader

                string[] parts = authHeader.Split((char)Common.ASCII.Space);

                string authType = null;

                if (parts.Length > 0)
                {
                    authType = parts[0];
                    authHeader = parts[1];
                }

                ////should check to ensure wrong type was not used e.g. basic in place of digest...
                //if (string.Compare(authType, RtspHeaderFields.Authorization.Digest, true) == 0)
                //{
                //    //if (session.Storage["nOnce"] != null)
                //    //{
                //    //    //Increment NonceCount
                //    //}
                //}

                //Increment session attempts?

                statusCode = RtspStatusCode.Forbidden;
            }
            
            //Set the status code
            response.RtspStatusCode = statusCode;

            //Send the response
            ProcessInvalidRtspRequest(response, session, sendResponse);
        }

        /// <summary>
        /// Provides the Options this server supports
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspOptions(RtspMessage request, ClientSession session, bool sendResponse = true)
        {

            //See if the location requires a certain stream
            if (request.Location.Equals(RtspMessage.Wildcard).Equals(false) && 
                string.IsNullOrWhiteSpace(request.Location.LocalPath).Equals(false) && 
                request.Location.LocalPath.Length > 1)
            {
                Media.Rtsp.Server.IMedia found = FindStreamByLocation(request.Location);

                //No stream with name
                if (Common.IDisposedExtensions.IsNullOrDisposed(found))
                {
                    ProcessLocationNotFoundRtspRequest(session, sendResponse);
                    return;
                }

                //Todo, just remove play from the header.
                if (found.Ready.Equals(false))
                {
                    ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);
                    return;
                }

                //Todo, Found should be used to build the Allowed options.

                //If the stream required authentication then SETUP and PLAY should not appear unless authenticated?

                //See if RECORD is supported?
            }

            //Check for additional options of the stream... e.g. allow recording or not

            RtspMessage resp = session.CreateRtspResponse(request);

            //Todo should be a property and should not return PLAY or PAUSE for not ready, should only show Teardown if sessions exist from this ip.
            resp.SetHeader(RtspHeaders.Public, "OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER"); //, REDIRECT

            //Todo should be a property
            //Allow for Authorized (should only send if authenticated?)
            resp.SetHeader(RtspHeaders.Allow, "ANNOUNCE, RECORD, SET_PARAMETER");

            //Add from handlers?
            //if(m_RequestHandlers.Count > 0) string.Join(" ", m_RequestHandlers.Keys.ToArray())

            //ApplyCacheControl
            resp.SetHeader(RtspHeaders.CacheControl, RtspHeaders.Cache̱Control.NoCache);

            resp.SetHeader(RtspHeaders.Supported, string.Join(resp.Comma, 
                    RtspHeaders.Supporte̱d.PlayBasic, 
                    RtspHeaders.Supporte̱d.PersistentConnection, 
                    RtspHeaders.Supporte̱d.SetupPlaying));

            //Should allow server to have certain options removed from this result
            //ClientSession.ProcessOptions

            ProcessSendRtspMessage(resp, session, sendResponse);

            resp = null;
        }

        /// <summary>
        /// Decribes the requested stream
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspDescribe(RtspMessage request, ClientSession session, bool sendResponse = true)
        {

            if (request.Location.Equals(RtspMessage.Wildcard))
            {
                var resp = session.CreateRtspResponse(request);

                ProcessSendRtspMessage(resp, session, sendResponse);

                resp = null;
            }
            else
            {
                string acceptHeader = request[RtspHeaders.Accept];

                //If an Accept header was given it must reflect a content-type of SDP.
                if (string.IsNullOrWhiteSpace(acceptHeader).Equals(false) &&
                    string.Compare(acceptHeader, Sdp.SessionDescription.MimeType, true, System.Globalization.CultureInfo.InvariantCulture) > 0)
                {
                    ProcessInvalidRtspRequest(session);

                    return;
                }

                Media.Rtsp.Server.IMedia found = FindStreamByLocation(request.Location);// as RtpSource;

                if (Common.IDisposedExtensions.IsNullOrDisposed(found))
                {
                    ProcessLocationNotFoundRtspRequest(session, sendResponse);

                    return;
                }

                if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
                {
                    ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);

                    return;
                }

                if (found.Ready.Equals(false))
                {
                    ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);

                    return;
                }

                var resp = session.ProcessDescribe(request, found);
                
                ProcessSendRtspMessage(resp, session, sendResponse);
            
                resp = null;

            }
        }

        /// <summary>
        /// Sets the given session up, TODO Make functions which help with creation of TransportContext and Initialization
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspSetup(RtspMessage request, ClientSession session, bool sendResponse = true)
        {

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (Common.IDisposedExtensions.IsNullOrDisposed(found))
            {
                //This allows probing for streams even if not authenticated....
                ProcessLocationNotFoundRtspRequest(session, sendResponse);
                return;
            }
            else if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
            {
                ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                return;
            }
            else if (found.Ready.Equals(false))
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);
                return;
            }


            //The source is ready

            //Determine if we have the track
            string track = request.Location.Segments.Last().Replace("/", string.Empty);


            int trackId, symbolIndex;

            Sdp.MediaDescription mediaDescription;

            if ((symbolIndex = track.IndexOf('=')) >= 0 && int.TryParse(Media.Common.ASCII.ExtractNumber(track, symbolIndex, track.Length), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out trackId)) mediaDescription = found.SessionDescription.GetMediaDescription(trackId - 1);
            else mediaDescription = found.SessionDescription.MediaDescriptions.FirstOrDefault(md => string.Compare(track, md.MediaType.ToString(), true, System.Globalization.CultureInfo.InvariantCulture).Equals(0));

            ////Find the MediaDescription for the request based on the track variable
            //foreach (Sdp.MediaDescription md in found.SessionDescription.MediaDescriptions)
            //{
            //    Sdp.SessionDescriptionLine attributeLine = md.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("control"))).FirstOrDefault();
            //    if (attributeLine != null) 
            //    {
            //        string actualTrack = attributeLine.Parts.Where(p => p.Contains("control")).First().Replace("control:", string.Empty);
            //        if(actualTrack == track || actualTrack.Contains(track))
            //        {
            //            mediaDescription = md;
            //            break;
            //        }
            //    }
            //}

            //Cannot setup media
            if (Common.IDisposedExtensions.IsNullOrDisposed(mediaDescription))
            {
                ProcessLocationNotFoundRtspRequest(session, sendResponse);
                return;
            }

            //Add the state information for the source
            RtpClient.TransportContext sourceContext = found.RtpClient.GetContextForMediaDescription(mediaDescription);

            //If the source has no TransportContext for that format or the source has not recieved a packet yet
            if (Common.IDisposedExtensions.IsNullOrDisposed(sourceContext) || sourceContext.InDiscovery)
            {
                //Stream is not yet ready
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);

                return;
            }

            //Create the response and initialize the sockets if required
            RtspMessage resp = session.ProcessSetup(request, found, sourceContext);
            
            //Send the response
            ProcessSendRtspMessage(resp, session, sendResponse);

            resp = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspPlay(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            //New method...
        TryCreateResponse:
            RtspMessage resp = null;

            try
            {
                RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

                if (Common.IDisposedExtensions.IsNullOrDisposed(found))
                {
                    ProcessLocationNotFoundRtspRequest(session, sendResponse);

                    return;
                }

                if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
                {
                    ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                    return;
                }
                else if (found.Ready.Equals(false))
                {
                    //Stream is not yet ready
                    ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed, null, sendResponse);
                    return;
                }

                resp = session.ProcessPlay(request, found);
                
                //Send the response to the client
                ProcessSendRtspMessage(resp, session, sendResponse);

                if (resp.RtspStatusCode > RtspStatusCode.Unknown && 
                    resp.RtspStatusCode <= RtspStatusCode.OK)
                {
                    //Take the range into account given.
                    session.ProcessPacketBuffer(found);
                }
                
            }
            catch(Exception ex)
            {
                Common.ILoggingExtensions.LogException(Logger, ex);

                if (Common.IDisposedExtensions.IsNullOrDisposed(resp).Equals(false) &&
                    resp.Transferred.HasValue) return;

                if (session.IsDisconnected.Equals(false)) goto TryCreateResponse;

            }
            finally
            {
                resp = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspPause(RtspMessage request, ClientSession session, bool sendResponse = true)
        {

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (Common.IDisposedExtensions.IsNullOrDisposed(found))
            {
                ProcessLocationNotFoundRtspRequest(session, sendResponse);
                return;
            }

            if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
            {
                ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                return;
            }

            //session.m_RtpClient.m_WorkerThread.Priority = ThreadPriority.BelowNormal;

            var resp = session.ProcessPause(request, found);
            
            //Might need to add some headers
            ProcessSendRtspMessage(resp, session, sendResponse);

            resp = null;

        }

        /// <summary>
        /// Ends the client session
        /// </summary>
        /// <param name="request">The Teardown request</param>
        /// <param name="session">The session which recieved the request</param>
        internal void ProcessRtspTeardown(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            //If there was a location which was not a wildcard attempt to honor it.
            if (request.Location.Equals(RtspMessage.Wildcard).Equals(false))
            {
                RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

                if (Common.IDisposedExtensions.IsNullOrDisposed(found))
                {
                    ProcessLocationNotFoundRtspRequest(session, sendResponse);
                    return;
                }

                if (AuthenticateRequest(request, found.ServerLocation).Equals(false))
                {
                    ProcessAuthorizationRequired(found.ServerLocation, session, sendResponse);
                    return;
                }

                //Send the response
                var resp = session.ProcessTeardown(request, found);

                ////Keep track if LeaveOpen should be set (based on if the session still shared the socket)`
                //if (false == (session.LeaveOpen = session.SharesSocket))
                //{
                //    //if it doesn't then inform that close may occur?
                //    resp.AppendOrSetHeader(RtspHeaders.Connection, RtspHeaderFields.Connection.Close);
                //}


                ProcessSendRtspMessage(resp, session, sendResponse);

                session.ReleaseUnusedResources();

                resp = null;
                //Attempt to remove the sessionId when nothing is playing... why?
                //10.4 SETUP says we would have to bundle pipelined requests.
                //That is contradictory.

                //if (session.Playing.Count == 0) session.SessionId = null;

                string connectionHeader = request[RtspHeaders.Connection];

                if (string.IsNullOrWhiteSpace(connectionHeader).Equals(false))
                {
                    if (string.Compare(connectionHeader.Trim(), RtspHeaderFields.Connection.Close, true).Equals(0))
                    {
                        TryDisposeAndRemoveSession(session);
                    }
                }
            }
            else
            {
                //Create the response
                var resp = session.CreateRtspResponse(request);

                //Todo 
                //Make a RtpInfo header for each stream ending...

                //Stop transport level activity if required
                if (Common.IDisposedExtensions.IsNullOrDisposed(session.m_RtpClient).Equals(false) &&
                    session.m_RtpClient.IsActive) session.m_RtpClient.SendGoodbyes();

                //Remove all attachments and clear playing
                session.RemoveAllAttachmentsAndClearPlaying();

                //Send the response
                ProcessSendRtspMessage(resp, session, sendResponse);

                //Release any unused resources at this point
                session.ReleaseUnusedResources();

                //Remove the sessionId
                session.SessionId = null;

                resp = null;

                string connectionHeader = request[RtspHeaders.Connection];

                if (string.IsNullOrWhiteSpace(connectionHeader).Equals(false) &&
                    connectionHeader.IndexOf(RtspHeaderFields.Connection.Close,  StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    TryDisposeAndRemoveSession(session);
                }
            }
        }

        /// <summary>
        /// Handles the GET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessGetParameter(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            //TODO Determine API
            //packets_sent
            //jitter
            //rtcp_interval

            //Todo if HasBody, check for expected parameter types e.g. text/ 
            //if found and supported parse body and create response based on input.

            var resp = session.CreateRtspResponse(request);

            resp.SetHeader(RtspHeaders.Connection, RtspHeaderFields.Connection.KeepAlive);

            ProcessSendRtspMessage(resp, session, sendResponse);

            resp = null;
        }

        /// <summary>
        /// Handles the SET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessSetParameter(RtspMessage request, ClientSession session, bool sendResponse = true)
        {
            //Could be used for PTZ or other stuff
            //Should have a way to determine to forward send parameters... public bool ForwardSetParameter { get; set; }
            //Should have a way to call SendSetParamter on the source if required.
            //Should allow sever parameters to be set?
            var resp = session.CreateRtspResponse(request);

            //Content type
            ProcessSendRtspMessage(resp, session, sendResponse);

            resp = null;
        }        

        /// <summary>
        /// Authenticates a RtspRequest against a RtspStream
        /// </summary>
        /// <param name="request">The RtspRequest to authenticate</param>
        /// <param name="source">The RtspStream to authenticate against</param>
        /// <returns>True if authroized, otherwise false</returns>
        public virtual bool AuthenticateRequest(RtspMessage request, Uri sourceLocation)
        {

            if (Common.IDisposedExtensions.IsNullOrDisposed(request)) return false; //throw new ArgumentNullException("request");

            if (object.ReferenceEquals(sourceLocation, null)) return false;

            //There are no rules to validate
            if (object.ReferenceEquals(RequiredCredentials, null)) return true;

            string authHeader = request.GetHeader(RtspHeaders.Authorization);

            string[] parts = null;

            string authType = null;

            NetworkCredential requiredCredential = null;

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                authType = "basic";

                //if source is null then the use of the request.location will have to work.
                //Uri loc = source == null ? request.Location : source.ServerLocation;

                //Try *
                //requiredCredential = RequiredCredentials.GetCredential(source.ServerLocation, "*");

                

                //Try to get the basic cred
                requiredCredential = RequiredCredentials.GetCredential(sourceLocation, authType);

                //If there was nothing for basic then try digest
                if (object.ReferenceEquals(requiredCredential, null)) requiredCredential = RequiredCredentials.GetCredential(sourceLocation, authType = "digest");
            }
            else
            {
                parts = authHeader.Split((char)Common.ASCII.Space);

                if (parts.Length > 0)
                { 
                    authType = parts[0];

                    authHeader = parts[1];
                }

                requiredCredential = RequiredCredentials.GetCredential(sourceLocation, authType);
            }

            //Should check method?, ensure announce and record is allowed from anywhere

            //A CredentialCacheEx with a NetworkCredentialEx may help API wise

            //If there is no rule to validate
            if (object.ReferenceEquals(requiredCredential, null)) return true;
            else
            {
                //Verify against credential

                //If the request does not have the authorization header then there is nothing else to determine
                if (string.IsNullOrWhiteSpace(authHeader)) return false;

                //Wouldn't have to have a RemoteAuthenticationScheme if we stored the Nonce and CNonce on the session... then allowed either or here based on the header

                

                //If the SourceAuthenticationScheme is Basic and the header contains the BASIC indication then validiate using BASIC authentication
                if (authType.IndexOf(RtspHeaderFields.Authorization.Basic, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    //realm may be present? 
                    //Basic realm="''" dasfhadfhsaghf

                    //Get the decoded value
                    authHeader = request.ContentEncoding.GetString(Convert.FromBase64String(authHeader));

                    //Get the parts
                    parts = authHeader.Split(':');

                    //If enough return the determination by comparison as the result
                    return parts.Length > 1 && (parts[0].Equals(requiredCredential.UserName) && parts[1].Equals(requiredCredential.Password));
                }
                else if (authType.IndexOf(RtspHeaderFields.Authorization.Digest, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    //http://tools.ietf.org/html/rfc2617
                    //Digest RFC2617
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
                     * 
                     * 
                     * Example Convo
                     * 
                     * ANNOUNCE rtsp://216.224.181.197/bstream.sdp RTSP/1.0
        CSeq: 1
        Content-Type: application/sdp
        User-Agent: C.U.
        Authorization: Digest username="gidon", realm="null", nonce="null", uri="/bstream.sdp", response="239fcac559661c17436e427e75f3d6a0"
        Content-Length: 313

        v=0
        s=CameraStream
        m=video 5006 RTP/AVP 96
        b=RR:0
        a=rtpmap:96 H264/90000
        a=fmtp:96 packetization-mode=1;profile-level-id=42000c;sprop-parameter-sets=Z0IADJZUCg+I,aM44gA==;
        a=control:trackID=0
        m=audio 5004 RTP/AVP 96
        b=AS:128
        b=RR:0
        a=rtpmap:96 AMR/8000
        a=fmtp:96 octet-align=1;
        a=control:trackID=1


        RTSP/1.0 401 Unauthorized
        Server: DSS/6.0.3 (Build/526.3; Platform/Linux; Release/Darwin Streaming Server; State/Development; )
        Cseq: 1
        WWW-Authenticate: Digest realm="Streaming Server", nonce="e5c0b7aff71820962027d73f55fe48c8"


        ANNOUNCE rtsp://216.224.181.197/bstream.sdp RTSP/1.0
        CSeq: 2
        Content-Type: application/sdp
        User-Agent: C.U.
        Authorization: Digest username="gidon", realm="Streaming Server", nonce="e5c0b7aff71820962027d73f55fe48c8", uri="/bstream.sdp", response="6e3aa3be3f5c04a324491fe9ab341918"
        Content-Length: 313

        v=0
        s=CameraStream
        m=video 5006 RTP/AVP 96
        b=RR:0
        a=rtpmap:96 H264/90000
        a=fmtp:96 packetization-mode=1;profile-level-id=42000c;sprop-parameter-sets=Z0IADJZUCg+I,aM44gA==;
        a=control:trackID=0
        m=audio 5004 RTP/AVP 96
        b=AS:128
        b=RR:0
        a=rtpmap:96 AMR/8000
        a=fmtp:96 octet-align=1;
        a=control:trackID=1


        RTSP/1.0 200 OK
        Server: DSS/6.0.3 (Build/526.3; Platform/Linux; Release/Darwin Streaming Server; State/Development; )
        Cseq: 2
                     * 
                     * 
                     */

                    parts = authHeader.Split((char)Common.ASCII.Comma);

                    string username, realm, nonce, nc, cnonce, uri, qop, opaque, response;

                    username = parts.Where(p => p.StartsWith("username")).FirstOrDefault();

                    realm = parts.Where(p => p.StartsWith("realm")).FirstOrDefault();

                    nc = parts.Where(p => p.StartsWith("nc")).FirstOrDefault();

                    nonce = parts.Where(p => p.StartsWith("nonce")).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(nonce)) nonce = string.Empty;

                    cnonce = parts.Where(p => p.StartsWith("cnonce")).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(cnonce)) cnonce = string.Empty;

                    uri = parts.Where(p => p.StartsWith("uri")).FirstOrDefault();

                    qop = parts.Where(p => p.StartsWith("qop")).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(qop)) qop = string.Empty;

                    opaque = parts.Where(p => p.StartsWith("opaque")).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(opaque)) opaque = string.Empty;

                    response = parts.Where(p => p.StartsWith("response")).FirstOrDefault();

                    if (string.IsNullOrEmpty(username) ||
                        username.Equals(requiredCredential.UserName).Equals(false) ||
                        string.IsNullOrWhiteSpace(realm) || 
                        string.IsNullOrWhiteSpace(uri) || 
                        string.IsNullOrWhiteSpace(response)) return false;

                    //http://en.wikipedia.org/wiki/Digest_access_authentication
                    //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                    byte[] HA1 = Cryptography.MD5.GetHash(request.ContentEncoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", requiredCredential.UserName, realm.Replace("realm=", string.Empty), requiredCredential.Password)));

                    //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                    byte[] HA2 = Cryptography.MD5.GetHash(request.ContentEncoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", request.RtspMethod, uri.Replace("uri=", string.Empty))));

                    //No QOP No NC
                    //See http://en.wikipedia.org/wiki/Digest_access_authentication
                    //http://tools.ietf.org/html/rfc2617

                    //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                    byte[] ResponseHash = Cryptography.MD5.GetHash(request.ContentEncoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", 
                        Convert.ToString(HA1).Replace("-", string.Empty), 
                        nonce.Replace("nonce=", string.Empty), 
                        nc.Replace("nc=", string.Empty), 
                        cnonce.Replace("cnonce=", string.Empty), 
                        qop.Replace("qop=", string.Empty), 
                        Convert.ToString(HA2).Replace("-", string.Empty))));

                    //return the result of a mutal hash creation via comparison
                    return ResponseHash.SequenceEqual(Media.Common.Extensions.String.StringExtensions.HexStringToBytes(response.Replace("response=", string.Empty)));
                }
                //else if (source.RemoteAuthenticationScheme == AuthenticationSchemes.IntegratedWindowsAuthentication && (header.Contains("ntlm") || header.Contains("integrated")))
                //{
                //    //Check windows creds
                //    throw new NotImplementedException();
                //}            

            }

            //Did not authenticate
            return false;
        }

        #endregion

        #endregion        
    
        #region IDisposable

        /// <summary>
        /// Calls <see cref="Stop"/> 
        /// Removes and Disposes all contained streams
        /// Removes all custom request handlers
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed || false.Equals(ShouldDispose)) return;

            base.Dispose();

            Stop(m_LeaveOpen);

            foreach (var stream in m_MediaStreams.ToList())
            {
                stream.Value.Dispose();
                m_MediaStreams.Remove(stream.Key);
            }

            //Clear streams
            m_MediaStreams.Clear();

            //Clear custom handlers
            m_RequestHandlers.Clear();
        }

        #endregion

        //Could be explicit implementation but would require casts.. determine how much cleaner the API is without them and reduce if necessary

        public Action<Socket> ConfigureSocket
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        public Action<Thread> ConfigureThread
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            if (IsDisposed) yield break;

            yield return m_TcpServerSocket;

            if (m_UdpServerSocket != null) yield return m_UdpServerSocket;

            //Get socket using reflection?
            //if (m_HttpListner != null) yield return m_HttpListner.;

            //ClientSession should be a CollectionType which implements ISocketReference
            //This logic should then appear in that collection.

            //Closing the server socket will also close client sockets.

            //////All client session sockets are technically referenced...
            ////foreach (var client in Clients)
            ////{
            ////    //If the client is null or disposed or disconnected continue
            ////    if (Common.IDisposedExtensions.IsNullOrDisposed(client) || client.IsDisconnected) continue;

            ////    //The clients Rtsp socket is referenced.
            ////    yield return client.m_RtspSocket;

            ////    //If there is a RtpClient which is not disposed
            ////    if (false == Common.IDisposedExtensions.IsNullOrDisposed(client.m_RtpClient))
            ////    {
            ////        //Enumerate the transport context for sockets
            ////        foreach (var tc in client.m_RtpClient.GetTransportContexts())
            ////        {
            ////            //If the context is null or diposed continue
            ////            if (Common.IDisposedExtensions.IsNullOrDisposed(tc)) continue;

            ////            //Cast to ISocketReference and call GetReferencedSockets to obtain any sockets of the context
            ////            foreach (var s in ((Common.ISocketReference)tc).GetReferencedSockets())
            ////            {
            ////                //Return them
            ////                yield return s;
            ////            }
            ////        }
            ////    }
            ////}
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        IEnumerable<Thread> Common.IThreadReference.GetReferencedThreads()
        {
             return Media.Common.Extensions.Linq.LinqExtensions.Yield(m_ServerThread);
        }
    }
}
