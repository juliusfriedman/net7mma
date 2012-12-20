using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Media.Rtp;
using Media.Rtcp;
using Media.Rtsp.Server.Streams;

namespace Media.Rtsp
{
    /// <summary>
    /// Implementation of Rfc 2326 server 
    /// http://tools.ietf.org/html/rfc2326
    /// </summary>
    public class RtspServer
    {
        public const int DefaultPort = 554;

        #region Nested Types

        /// <summary>
        /// Encapsulated exceptions thrown from a RtspServer
        /// </summary>
        public class RtspServerException : Exception
        {
            public RtspServerException(string message) : base(message) { }

            public RtspServerException(string message, Exception inner  ) : base(message, inner) { }
        }
        
        #endregion

        #region Fields

        DateTime? m_Started;

        /// <summary>
        /// The port the RtspServer is listening on, defaults to 554
        /// </summary>
        int m_ServerPort = 554,
            //Counters for bytes sent and recieved
            m_Recieved, m_Sent;

        /// <summary>
        /// The socket used for recieving RtspRequests
        /// </summary>
        Socket m_TcpServerSocket, m_UdpServerSocket;

        /// <summary>
        /// The HttpListner used for handling Rtsp over Http
        /// </summary>
        HttpListener m_HttpListner;

        /// <summary>
        /// The endpoint the server is listening on
        /// </summary>
        EndPoint m_ServerEndPoint;

        /// <summary>
        /// The dictionary containing all streams the server is aggregrating
        /// </summary>
        Dictionary<Guid, RtspSourceStream> m_Streams = new Dictionary<Guid, RtspSourceStream>();

        /// <summary>
        /// The dictionary containing all the clients the server has sessions assocaited with
        /// </summary>
        Dictionary<Guid, RtspSession> m_Clients = new Dictionary<Guid, RtspSession>();

        /// <summary>
        /// The thread allocated to handle socket communication
        /// </summary>
        Thread m_ServerThread;

        /// <summary>
        /// Indicates to the ServerThread a stop has been requested
        /// </summary>
        bool m_StopRequested;

        /// <summary>
        /// Used to signal the server to recieve new clients
        /// </summary>
        ManualResetEvent allDone = new ManualResetEvent(false);

        //Handles the Restarting of streams which needs to be and disconnects clients which are inactive.
        internal Timer m_Maintainer;

        #endregion

        #region Propeties

        public TimeSpan Uptime { get { if (m_Started.HasValue) return DateTime.Now - m_Started.Value; return TimeSpan.MinValue; } }

        /// <summary>
        /// Indicates if the RtspServer is listening for requests on the ServerPort
        /// </summary>
        public bool Listening { get { return m_ServerThread != null; /*&& m_ServerThread.ThreadState == ThreadState.Running;*/ } }

        /// <summary>
        /// The port in which the RtspServer is listeing for request
        /// </summary>
        public int ServerPort { get { return m_ServerPort; } }

        /// <summary>
        /// The local endpoint for this RtspServer (The endpoint on which requests are recieved)
        /// </summary>
        public IPEndPoint LocalEndPoint { get { return m_TcpServerSocket.LocalEndPoint as IPEndPoint; } }

        /// <summary>
        /// Accesses a contained stream by id of the stream
        /// </summary>
        /// <param name="streamId">The unique identifer</param>
        /// <returns>The RtspClient assocaited with the given id if found, otherwise null</returns>
        public RtspSourceStream this[Guid streamId] { get { return GetStream(streamId); } }

        public List<RtspSourceStream> Streams { get { return m_Streams.Values.ToList(); } }

        /// <summary>
        /// The amount of streams the server is prepared to listen to
        /// </summary>
        public int TotalStreamCount { get { return m_Streams.Count; } }

        /// <summary>
        /// The amount of active streams the server is listening to
        /// </summary>
        public int ActiveStreamCount
        {
            get
            {
                if (TotalStreamCount == 0) return 0;
                return m_Streams.Where(s => s.Value.Listening == true).Count();
            }
        }

        /// <summary>
        /// The total amount of bytes the RtspServer recieved from remote RtspRequests
        /// </summary>
        public int TotalRtspBytesRecieved { get { return m_Recieved; } }

        /// <summary>
        /// The total amount of bytes the RtspServer sent in response to remote RtspRequests
        /// </summary>
        public int TotalRtspBytesSent { get { return m_Sent; } }

        /// <summary>
        /// The amount of bytes recieved from all contained streams in the RtspServer
        /// </summary>
        public int TotalStreamBytesRecieved
        {
            get
            {
                int total = 0;
                foreach (RtspSourceStream client in m_Streams.Values)
                {
                    total += client.Client.BytesRecieved;
                }
                return total;
            }
        }

        /// <summary>
        /// The amount of bytes sent to all contained streams in the RtspServer
        /// </summary>
        public int TotalStreamBytesSent
        {
            get
            {
                int total = 0;
                foreach (RtspSourceStream stream in m_Streams.Values)
                {
                    total += stream.Client.BytesSent;
                }
                return total;
            }
        }

        public int ConnectedClients { get { return m_Clients.Count; } }

        public RtspServerLogger Logger { get; set; }

        #endregion

        #region Constructor

        public RtspServer(int listenPort = DefaultPort)
        {
            m_ServerPort = listenPort;
        }

        #endregion

        #region Methods

        int m_HttpPort = -1;
        public void EnableHttp(int port = 80) 
        {
            if (m_HttpListner == null)
            {
                m_HttpListner = new HttpListener();
                m_HttpPort = port;
                m_HttpListner.Prefixes.Add("http://*:"+port+"/");
                m_HttpListner.Start();                
                m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), null);
            }
        }

        int m_UdpPort = -1;
        public void EnableUdp(int port = 554) 
        {
            if (m_UdpServerSocket != null)
            {
                m_UdpPort = port;
                m_UdpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_UdpServerSocket.Bind(new IPEndPoint(Utility.GetV4IPAddress(), port));
                RtspSession temp = new RtspSession(this, null);
                temp.m_RtspSocket = m_UdpServerSocket;
                m_UdpServerSocket.BeginReceive(temp.m_Buffer, 0, temp.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessReceive), temp);
            }
        }

        public void DisableHttp()
        {
            if (m_HttpListner != null)
            {
                m_HttpListner.Stop();
                m_HttpListner.Close();
                m_HttpListner = null;
            }
        }

        public void DisableUdp()
        {
            if (m_UdpServerSocket != null)
            {
                m_UdpServerSocket.Shutdown(SocketShutdown.Both);
                m_UdpServerSocket.Dispose();
                m_UdpServerSocket = null;
            }
        }

        #region Session Collection

        internal void AddSession(RtspSession session)
        {
            lock (m_Clients)
            {
                m_Clients.Add(session.Id, session);
            }
        }

        internal bool RemoveSession(RtspSession session)
        {
            lock (m_Clients)
            {
                return m_Clients.Remove(session.Id);
            }
        }

        internal bool ContainsSession(RtspSession session)
        {
            return m_Clients.ContainsKey(session.Id);
        }

        internal RtspSession GetSession(Guid id)
        {
            RtspSession result;
            m_Clients.TryGetValue(id, out result);
            return result;
        }

        internal RtspSession FindSessionByRtspSessionId(string rtspSessionId)
        {
            if (string.IsNullOrWhiteSpace(rtspSessionId)) return null;
            rtspSessionId = rtspSessionId.Trim();
            return m_Clients.Values.Where(c => c.SessionId != null && c.SessionId.Equals(rtspSessionId)).FirstOrDefault();
        }               

        #endregion

        #region Stream Collection

        /// <summary>
        /// Adds a stream to the server. If the server is already started then the stream will also be started
        /// </summary>
        /// <param name="location">The uri of the stream</param>
        public void AddStream(RtspSourceStream stream)
        {
            if (ContainsStream(stream.Id)) throw new RtspServerException("Cannot add the given stream because it is already contained in the RtspServer");
            else
            {
                lock (m_Streams)
                {
                    m_Streams.Add(stream.Id, stream);
                }

                if (Listening) stream.Start(); //ThreadPool.QueueUserWorkItem(new WaitCallback(stream.Start));
            }
        }

        /// <summary>
        /// Indicates if the RtspServer contains the given streamId
        /// </summary>
        /// <param name="streamId">The id of the stream</param>
        /// <returns>True if the stream is contained, otherwise false</returns>
        public bool ContainsStream(Guid streamId)
        {
            return m_Streams.ContainsKey(streamId);
        }

        /// <summary>
        /// Stops and Removes a stream from the server
        /// </summary>
        /// <param name="streamId">The id of the stream</param>
        /// <returns>True if removed, otherwise false</returns>
        public bool RemoveStream(Guid streamId)
        {
            try
            {
                RtspSourceStream client = m_Streams[streamId];
                client.Stop();
                //client.Client.Disconnect();
                lock (m_Streams)
                {
                    return m_Streams.Remove(streamId);
                }
            }
            catch
            {
                return false;
            }
        }

        public RtspSourceStream GetStream(Guid streamId)
        {
            RtspSourceStream result;
            m_Streams.TryGetValue(streamId, out result);
            return result;
        }

        /// <summary>
        /// TODO :: SHould handle /GUID requests and should handle /archive requests
        /// </summary>
        /// <param name="mediaLocation"></param>
        /// <returns></returns>
        internal RtspSourceStream FindStreamByLocation(Uri mediaLocation, RtspSession ci = null)
        {
            string val = mediaLocation.ToString();

            RtspSourceStream found = null;

            //mediaLocation.Segments

            if (val.Contains("live"))
            {

                foreach (RtspSourceStream stream in m_Streams.Values.ToList())
                    if (val.Contains(stream.Name))
                    {
                        found = stream;
                        break;
                    }
                    else foreach (string alias in stream.m_Aliases)
                            if (val.Contains(alias))
                            {
                                found = stream;
                                break;
                            }
            }
            else
            {
                if (ci == null) return null;
                //Need facilites for creating a RtspStream from an archive file
                //Should have a static constructor RtspArchivedStream.FromMediaLocation(Url location)
                //Needs the ci who requests this media to attached the archives stream to... 
            }

            return found;
        }

        #endregion

        #region Server Logic

        /// <summary>
        /// Finds and removes inactive clients.
        /// Determined by the time of the sessions last RecieversReport or the last RtspRequestRecieved (get parameter must be sent to keep from timing out)
        /// </summary>
        internal void DisconnectAndRemoveInactiveSessions(object o) { DisconnectAndRemoveInactiveSessions(); }
        internal void DisconnectAndRemoveInactiveSessions()
        {
            //Find inactive clients and remove..

            RtspSession[] clients;
            lock (m_Clients)
            {
                 clients = m_Clients.Values.ToArray();
            }
            //Iterate and find inactive sessions
            foreach (RtspSession session in clients)
            {
                if ((DateTime.UtcNow - session.m_LastRtspRequestRecieved).TotalMinutes > 2 || session.m_RtpClient != null && session.m_RtpClient.m_LastRecieversReport != null && (DateTime.UtcNow - session.m_RtpClient.m_LastRecieversReportRecieved).TotalMinutes > 2)
                {

                    if (session.m_RtpClient != null && !session.m_RtpClient.m_RtcpGoodbyeRecieved)
                    {
                        session.m_RtpClient.SendGoodbye();
                    }

                    RemoveSession(session);
                }
            }
        }

        /// <summary>
        /// Restarted streams which should be Listening but are not
        /// </summary>
        internal void RestartFaultedStreams(Object o) { RestartFaultedStreams(); }
        internal void RestartFaultedStreams()
        {
            var toStart = default(List<RtspSourceStream>);
            lock (m_Streams)
            {
                toStart = m_Streams.Values.Where(s => s.State == RtspSourceStream.StreamState.Started && s.Listening == false).ToList();
            }
            foreach (RtspSourceStream stream in toStart)
            {
                try
                {
                    stream.Client.Disconnect();
                    stream.Start();
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Starts the RtspServer and listens for requests.
        /// Starts all streams contained in the server
        /// </summary>
        public virtual void Start()
        {
            //If we already have a thread return
            if (m_ServerThread != null) return;

            m_StopRequested = false;

            //Start streaming from m_Streams
            StartStreams();

            //Start listening for requests....

            ///Create the server EndPoint
            m_ServerEndPoint = new IPEndPoint(IPAddress.Any, m_ServerPort);

            //Create the server Socket
            m_TcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Bind the server Socket to the server EndPoint
            m_TcpServerSocket.Bind(m_ServerEndPoint);

            //Set the backlog
            m_TcpServerSocket.Listen(1024);

            //Create a thread to handle client connections
            m_ServerThread = new Thread(new ThreadStart(RecieveLoop));

            m_ServerThread.Name = "RtspServer@" + m_ServerPort;

            m_ServerThread.Start();

            m_Maintainer = new Timer(new TimerCallback((object o) =>
            {

                DisconnectAndRemoveInactiveSessions();
                RestartFaultedStreams();

            }), null, 10000, 10000);

            m_Started = DateTime.Now;

            if (m_UdpPort != -1) EnableUdp(m_UdpPort);
            if (m_HttpPort != -1) EnableHttp(m_HttpPort);

        }

        /// <summary>
        /// Stops recieving RtspRequests and stops streaming all contained streams
        /// </summary>
        public virtual void Stop()
        {
            //If there is not a server thread return
            if (m_ServerThread == null) return;

            m_StopRequested = true;

            //Stop listening on client streams
            StopStreams();

            //Stop listening for requests

            //Abort the thread
            m_ServerThread.Abort();

            //Free the member so we can start again
            m_ServerThread = null;

            m_Maintainer.Dispose();

            //Dispose the socket
            m_TcpServerSocket.Dispose();

            m_Started = null;

            DisableHttp();
            DisableUdp();

        }

        /// <summary>
        /// Starts all streams contained in the video server
        /// </summary>
        internal virtual void StartStreams()
        {
            foreach (RtspSourceStream stream in m_Streams.Values.ToList())
            {                
                stream.Start();
            }
        }

        /// <summary>
        /// Stops all contained streams from streaming
        /// </summary>
        internal virtual void StopStreams()
        {
            foreach (RtspSourceStream stream in m_Streams.Values.ToList())
            {                
                stream.Stop();
            }
        }        

        //MIght need another for Udp
        /// <summary>
        /// The loop where Rtsp Requests are recieved
        /// </summary>
        internal virtual void RecieveLoop()
        {
            while (!m_StopRequested)
            {
                allDone.Reset();

                m_TcpServerSocket.BeginAccept(new AsyncCallback(ProcessAccept), m_TcpServerSocket);                
                
                while (!allDone.WaitOne())
                {
                    System.Threading.Thread.Yield();
                }
            }
        }

        #endregion

        #region Socket Methods

        /// <summary>
        /// Handles the accept of rtsp client sockets into the server
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessAccept(IAsyncResult ar)
        {
            allDone.Set();
            try
            {
                Socket svr = (Socket)ar.AsyncState;
                
                Socket clientSocket = svr.EndAccept(ar);

                System.Diagnostics.Debug.WriteLine("Accepted connection from: {0}", clientSocket.RemoteEndPoint);

                RtspSession ci = new RtspSession(this, clientSocket);

                AddSession(ci);

                clientSocket.BeginReceive(ci.m_Buffer, 0, ci.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessReceive), ci);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Accept failed with: {0}", ex);
            }
        }

        /// <summary>
        /// Handles the recieving of sockets data from a rtspClient
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessReceive(IAsyncResult ar)
        {
            //Get the client information
            RtspSession session = (RtspSession)ar.AsyncState;
            int received = 0;
            RtspRequest req = null;
            try
            {
                if (session.m_RtspSocket.ProtocolType == ProtocolType.Udp)                
                {
                    //If this is the inital receive
                    if (m_UdpServerSocket == session.m_RtspSocket)
                    {
                        received = m_UdpServerSocket.EndReceive(ar);

                        RtspSession temp = new RtspSession(this, null);
                        m_UdpServerSocket.BeginReceive(temp.m_Buffer, 0, temp.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessReceive), temp);

                        //Might need plumbing to store endpoints for sessions
                        IPEndPoint remote = (IPEndPoint)m_UdpServerSocket.RemoteEndPoint;
                        session.m_Udp = new UdpClient();
                        session.m_Udp.Connect(remote);
                        session.m_RtspSocket = session.m_Udp.Client;

                        //Parse the reqeut to determine if there is actually an existing session

                        req = new RtspRequest(session.m_Buffer);
                        if (req.ContainsHeader(RtspHeaders.Session))
                        {
                            var existing = FindSessionByRtspSessionId(req.GetHeader(RtspHeaders.Session));
                            if (existing == null) throw new RtspServerException("Session Not Found");
                            else
                            {
                                //Might be incorrect... e.g. we might want to keep the new session and not update the existing...
                                //E.g if they connect with Tcp then Udp or the other way around
                                session.m_Udp = existing.m_Udp;
                                session = existing;
                            }
                        }
                        else
                        {
                            AddSession(session);
                        }

                    }
                    else //This is a repeated recieve
                    {
                        IPEndPoint remote = null;
                        session.m_Buffer = session.m_Udp.EndReceive(ar, ref remote);
                        received = session.m_Buffer.Length;
                    }
                }
                else if (session.m_RtspSocket.ProtocolType == ProtocolType.Tcp)
                {
                    received = session.m_RtspSocket.EndReceive(ar);
                }

                //If we received anything
                if (received > 0)
                {
                    //If we are in TcpTransport this could be a RtpPacket or a RtcpPacket... we will need to check for the $ then the channel and raise the event if necessary                
                    if (session.m_RtspSocket.ProtocolType == ProtocolType.Tcp && session.m_Buffer[0] == 36)//We have a Rtp Packet in the buffer
                    {
                        //To ensure that we actaully read the packet and raised an event
                        if (session.m_RtpClient.RecieveData(session.m_Buffer[1]) < received)
                        {
                            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                            else throw new Exception("Recieved Less then the RtpPacket Size");
                        }
                    }
                    else //Just process the request
                    {
                        ProcessRtspRequest(req != null ? req : req = new RtspRequest(session.m_Buffer), session);
                    }
                }
            }
            catch
            {
                ProcessInvalidRtspRequest(session);
            }
            finally
            {
                req = null;
                m_Recieved += received;
                session.m_Receieved += received;
            }
        }

        /// <summary>
        /// Handles the sending of responses to clients which made requests
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessSend(IAsyncResult ar)
        {
            try
            {
                RtspSession ci = (RtspSession)ar.AsyncState;

                int sent = ci.m_RtspSocket.EndSend(ar);

                ci.m_Sent += sent;

                m_Sent += sent;

                //Start recieve again (Interleaved Rtp over Rtsp might have problems with this sharing)
                if (ci.m_RtspSocket.ProtocolType == ProtocolType.Tcp)
                    ci.m_RtspSocket.BeginReceive(ci.m_Buffer, 0, ci.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessReceive), ci);
                else
                    ci.m_Udp.BeginReceive(new AsyncCallback(ProcessReceive), ci);
            }
            catch (SocketException ex)
            {
                //User bailed
                System.Diagnostics.Debug.WriteLine("Socket Exception in ProcessSend: " + ex.ToString());
            }
        }

        #endregion

        #region Rtsp Request Handling Methods

        internal void ProcessHttpRtspRequest(IAsyncResult state)
        {
            HttpListenerContext context = m_HttpListner.EndGetContext(state);
                
            m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), null);

            //Ignore invalid request or return 500? TransportInvalid?
            if (context.Request.Headers.Get("Accept") != "application/x-rtsp-tunnelled")
            {
                context.Response.Close();                
                return;
            }

            
            //http://comments.gmane.org/gmane.comp.multimedia.live555.devel/5896
            //http://cgit.freedesktop.org/gstreamer/gst-plugins-base/tree/gst-libs/gst/rtsp/gstrtspconnection.c?id=88110ea67e7d5240a7262dbb9c4e5d8db565cccf
            //Can't find anythingin RFC
            //MAY ALSO NEED ICE AND STUN?

            int len = int.Parse(context.Request.Headers.Get("Content-Length"));
            byte[] buffer = new byte[len];
            //Get RtspRequest from Body and base64 decode as request
            int rec = context.Request.InputStream.Read(buffer, 0, len);
            RtspRequest request = new RtspRequest(System.Convert.FromBase64String(System.Text.Encoding.UTF8.GetString(buffer, 0, len)));
            
            RtspSession ci;
            if (request.ContainsHeader(RtspHeaders.Session)) // Attempt to find existing session
            {
                ci = FindSessionByRtspSessionId(request[RtspHeaders.Session]);
                if (ci == null) goto Response;
            }
            else // Create a new session
            {
                ci = new RtspSession(this, null);
                ci.m_Http = context;
            }

            //Process request
            ProcessRtspRequest(request, ci);
        
        Response:
            //Use ci.LastResponse to send response
            RtspResponse response = ci != null && ci.m_LastResponse != null ? ci.m_LastResponse : new RtspResponse()
            {
                CSeq = request.CSeq,
                StatusCode = RtspStatusCode.SessionNotFound
            };

            context.Response.AddHeader("Accept", "application/x-rtsp-tunnelled");
            context.Response.AddHeader("Pragma", "no-cache");
            context.Response.AddHeader("Cache-Control", "no-cache");

            buffer = response.ToBytes();

            buffer = response.Encoding.GetBytes(Convert.ToBase64String(buffer));

            context.Response.AddHeader("Content-Length", buffer.Length.ToString());

            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            context.Response.OutputStream.Close();

            context.Response.Close();
        }

        /// <summary>
        /// Processes a RtspRequest based on the contents
        /// </summary>
        /// <param name="request">The rtsp Request</param>
        /// <param name="session">The client information</param>
        internal void ProcessRtspRequest(RtspRequest request, RtspSession session)
        {
            //Log Request
            if (Logger != null) Logger.LogRequest(request, session);

            session.m_LastRequest = request;

            //All requests need the CSeq
            if (!request.ContainsHeader(RtspHeaders.CSeq))
            {
                ProcessInvalidRtspRequest(session);
                return;
            }

            //If there is a body and no content-length
            if (string.IsNullOrWhiteSpace(request.Body) && !request.ContainsHeader(RtspHeaders.ContentLength))
            {
                ProcessInvalidRtspRequest(session);
                return;
            }

            //Optional
            //if (!request.ContainsHeader(RtspHeaders.UserAgent)) ProcessInvalidRtspRequest(session, ResponseStatusCode.InternalServerError);
            
            //Need the property for this
            //if (request.Version != ServerVersion) ProcessInvalidRtspRequest(ci, ResponseStatusCode.RTSPVersionNotSupported);

            //Synchronize the server and client
            session.LastRequest = request;
            session.m_LastRtspRequestRecieved = DateTime.UtcNow;

            //Determine the handler for the request and process it
            switch (request.Method)
            {
                case RtspMethod.OPTIONS:
                    {
                        ProcessRtspOptions(request, session);
                        break;
                    }
                case RtspMethod.DESCRIBE:
                    {
                        ProcessRtspDescribe(request, session);
                        break;
                    }
                case RtspMethod.SETUP:
                    {
                        ProcessRtspSetup(request, session);
                        break;
                    }
                case RtspMethod.PLAY:
                    {
                        ProcessRtspPlay(request, session);
                        break;
                    }
                case RtspMethod.RECORD:
                    {
                        //Not yet implimented
                        goto default;
                    }
                case RtspMethod.PAUSE:
                    {
                        ProcessRtspPause(request, session);
                        break;
                    }
                case RtspMethod.TEARDOWN:
                    {
                        ProcessRtspTeardown(request, session);
                        break;
                    }
                case RtspMethod.GET_PARAMETER:
                    {
                        ProcessGetParameter(request, session);
                        break;
                    }
                case RtspMethod.UNKNOWN:
                default:                
                    {
                        ProcessInvalidRtspRequest(session, RtspStatusCode.MethodNotAllowed);
                        break;
                    }
            }

        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// May need to be modified for Http and Udp to use SentTo
        /// </summary>
        /// <param name="response">The RtspResponse to send</param> If this was byte[] then it could handle http
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessSendRtspResponse(RtspResponse response, RtspSession ci)
        {
            try
            {
                ci.m_LastResponse = response;

                byte[] buffer = response.ToBytes();

                if (ci.m_Http != null)
                {
                    //Don't http handle
                    return;
                }

                if(ci.m_RtspSocket.ProtocolType == ProtocolType.Tcp)
                    ci.m_RtspSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ProcessSend), ci);
                else
                    ci.m_RtspSocket.BeginSendTo(buffer, 0, buffer.Length,  SocketFlags.None, ci.m_RtspSocket.RemoteEndPoint, new AsyncCallback(ProcessSend), ci);                
            }
            catch (SocketException)
            {
                //Most likely a tear down
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Logger != null)
                {
                    Logger.LogResponse(response, ci);
                }
            }
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="ci">The client session to send the response on</param>
        /// <param name="code">The status code of the response if other than BadRequest</param>
        internal void ProcessInvalidRtspRequest(RtspSession ci, RtspStatusCode code = RtspStatusCode.BadRequest)
        {            
            //Should allow a reason to be put into the response somehow
            ProcessSendRtspResponse(ci.CreateRtspResponse(null, code), ci);
        }

        /// <summary>
        /// Sends a Rtsp LocationNotFound Response
        /// </summary>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessLocationNotFoundRtspRequest(RtspSession ci)
        {
            ProcessInvalidRtspRequest(ci, RtspStatusCode.NotFound);
        }

        internal void ProcessAuthorizationRequired(RtspSession ci)
        {
            ProcessInvalidRtspRequest(ci, ci.LastRequest.ContainsHeader(RtspHeaders.Authroization) ? RtspStatusCode.Forbidden : RtspStatusCode.Unauthorized);
        }

        internal void ProcessRtspOptions(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("OPTIONS " + request.Location);

            RtspSourceStream found = FindStreamByLocation(request.Location, ci);

            //No stream with name
            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            }

            RtspResponse resp = ci.CreateRtspResponse(request);
            
            //resp.SetHeader(RtspHeaders.Public, "OPTIONS, DESCRIBE, SETUP, PLAY, TEARDOWN, GET_PARAMETER"); //Causes VLC to try options again and again
            resp.SetHeader(RtspHeaders.Public, " DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER"/*, OPTIONS"*/); //Options is really not needed anyway

            ProcessSendRtspResponse(resp, ci);
        }

        internal void ProcessRtspDescribe(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("DESCRIBE " + request.Location);

            string acceptHeader = request[RtspHeaders.Accept];

            if (string.IsNullOrWhiteSpace(acceptHeader) || acceptHeader.Trim() != "application/sdp")
            {
                ProcessInvalidRtspRequest(ci);
                return;
            }

            RtspSourceStream found = FindStreamByLocation(request.Location, ci);

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(ci);
                return;
            }

            if (!found.Listening)
            {
                ProcessInvalidRtspRequest(ci, RtspStatusCode.MethodNotAllowed);
                return;
            }

            //Chould check to see if ci has an existing session desciprtion

            RtspResponse resp = ci.CreateRtspResponse(request);

            if (request.Location.ToString().ToLowerInvariant().Contains("live"))
            {
                resp.SetHeader(RtspHeaders.ContentBase, "rtsp://" + ((IPEndPoint)ci.m_RtspSocket.LocalEndPoint).Address.ToString() + "/live/" + found.Name);//Might should be Id and not name
            }
            else
            {
                resp.SetHeader(RtspHeaders.ContentBase, request.Location.ToString());
            }

            resp.SetHeader(RtspHeaders.ContentType, "application/sdp");
            

            //Create the SDP from the found media
            ci.CreateSessionDescription(found);
            resp.Body = ci.SessionDescription.ToString();

            ProcessSendRtspResponse(resp, ci);
        }

        internal void ProcessRtspSetup(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("SETUP " + request.Location);

            RtspSourceStream found = FindStreamByLocation(request.Location, ci);
            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            } 

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(ci);
                return;
            }

            //Create ci SessionID
            //Allocate ports in session     

            string transportHeader = request[RtspHeaders.Transport];

            if (string.IsNullOrWhiteSpace(transportHeader))
            {
                ProcessInvalidRtspRequest(ci);
                return;
            }

            string clientPortDirective = null; //comes from transportHeader client_port=

            string[] parts = transportHeader.Split(';');

            //ProtocolType requestedProtcolType = ProtocolType.Udp;

            for (int i = 0, e = parts.Length; i < e; ++i)
            {
                string part = parts[i].Trim();
                //if (part == "RTP/AVP" || part == "RTP/AVP/UDP") requestedProtcolType = ProtocolType.Udp;
                //else if (part == "RTP/AVP/TCP") requestedProtcolType = ProtocolType.Tcp;
                //else if (part.StartsWith("interleaved="))
                if (part.StartsWith("interleaved="))
                {
                    string [] channels = part.Split('-');
                    if (channels.Length > 1)
                    {
                        ci.m_RtpClient.m_RtpChannel = (byte)Convert.ToInt32(channels[0]);
                        ci.m_RtpClient.m_RtpChannel = (byte)Convert.ToInt32(channels[1]);
                    }
                }
                else if (part.StartsWith("client_port="))
                {
                    clientPortDirective = part.Replace("client_port=", string.Empty);
                }
            }

            string returnTransportHeader = null;

            //If there was no client port we cannot setup the media
            if (clientPortDirective == null)
            {
                ProcessInvalidRtspRequest(ci, RtspStatusCode.BadRequest);
                return;
            }

            string[] ports = clientPortDirective.Split('-');
            if (ports.Length > 1)
            {
                //The client requests Udp
                ci.InitializeRtp(Convert.ToInt32(ports[0]), Convert.ToInt32(ports[1]));
                returnTransportHeader = "RTP/AVP/UDP;unicast;client_port=" + clientPortDirective + ";server_port=" + ci.m_RtpClient.m_ServerRtpPort + "-" + ci.m_RtpClient.m_ServerRtcpPort + ";mode=\"PLAY\";ssrc=" + ci.m_RtpClient.m_RtpSSRC;
            }
            else
            {
                //The client requests Tcp
                ci.InitializeRtp(Convert.ToInt32(ports[0]), Convert.ToInt32(ports[0]));
                returnTransportHeader = "RTP/AVP/TCP;unicast;client_port=" + clientPortDirective + ";server_port=" + ci.m_RtpClient.m_ServerRtpPort + ";mode=\"PLAY\";ssrc=" + ci.m_RtpClient.m_RtpSSRC;
            }


            //Create the response
            RtspResponse resp = ci.CreateRtspResponse(request);
            resp.AppendOrSetHeader(RtspHeaders.Session, "timeout=60");
            resp.SetHeader(RtspHeaders.Transport, returnTransportHeader);

            //Send the response
            ProcessSendRtspResponse(resp, ci);
        }

        internal void ProcessRtspPlay(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("PLAY " + request.Location);

            RtspSession session = FindSessionByRtspSessionId(request[RtspHeaders.Session]);
            if (session == null)
            {
                ProcessInvalidRtspRequest(ci, RtspStatusCode.SessionNotFound);
                return;
            }

            RtspSourceStream found = FindStreamByLocation(request.Location, ci);
            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(ci);
                return;
            }

            //Hackup
            if (request.Location.ToString().ToLowerInvariant().Contains("archive"))
            {
                //Should handle play from the Play method here because of rewind and FF
            }
            else
            {

                ci.Attach(found);
            }

            string rangeHeader = request[RtspHeaders.Range];

            if (string.IsNullOrWhiteSpace(rangeHeader))
            {
                ProcessInvalidRtspRequest(ci, RtspStatusCode.BadRequest);
                return;
            }

            //Create a response
            RtspResponse response = ci.CreateRtspResponse(request);

            //Might should be Id and not name
            response.SetHeader(RtspHeaders.RtpInfo, "url=rtsp://" + ((IPEndPoint)(ci.m_RtspSocket.LocalEndPoint)).Address + "/live/" + found.Name + "/video");
            response.SetHeader(RtspHeaders.Range, "npt=0.000-0.000");

            ProcessSendRtspResponse(response, ci);
            
        }

        internal void ProcessRtspPause(RtspRequest request, RtspSession ci)
        {

            RtspSession session = FindSessionByRtspSessionId(request[RtspHeaders.Session]);
            if (session == null)
            {
                ProcessInvalidRtspRequest(ci, RtspStatusCode.SessionNotFound);
                return;
            }

            RtspSourceStream found = FindStreamByLocation(request.Location, ci);
            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(ci);
                return;
            }

            ci.Detach(found);

            //Might need to add some headers
            ProcessSendRtspResponse(ci.CreateRtspResponse(request), ci);

        }

        internal void ProcessRtspTeardown(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("TEARDOWN " + request.Location);

            try
            {
                RtspSession session = FindSessionByRtspSessionId(request[RtspHeaders.Session]);

                if (session == null)
                {
                    ProcessInvalidRtspRequest(ci, RtspStatusCode.SessionNotFound);
                    return;
                }

                RtspSourceStream found = FindStreamByLocation(request.Location, ci);

                if (found == null)
                {
                    ProcessLocationNotFoundRtspRequest(ci);
                    return;
                }

                if (!AuthenticateRequest(request, found))
                {
                    ProcessAuthorizationRequired(ci);
                    return;
                }

                if (request.Location.ToString().ToLowerInvariant().Contains("archive"))
                {
                    //Disconnect for archive
                }
                else
                {
                    ci.Detach(found);
                }

                //Send Goodbye
                //Close ports allocated in session
                ci.Disconnect();

                ProcessSendRtspResponse(ci.CreateRtspResponse(request), ci);
            }
            catch
            {
                //What
            }
            finally
            {
                if (ci.m_Attached.Count == 0) RemoveSession(ci);
            }
        }

        /// <summary>
        /// Handles the GET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessGetParameter(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("GET_PARAMETER " + request.Location);
            ProcessSendRtspResponse(ci.CreateRtspResponse(request), ci);
        }

        /// <summary>
        /// Handles the SET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessSetParameter(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("SET_PARAMETER " + request.Location);
            //Could be used for PTZ or other stuff
            //Should have a way to determine to forward send parameters... public bool ForwardSetParameter { get; set; }
            //Should have a way to call SendSetParamter on the RtspSession.Listener
            ProcessSendRtspResponse(ci.CreateRtspResponse(request), ci);
        }

        /// <summary>
        /// Authenticates a RtspRequest against a RtspStream
        /// </summary>
        /// <param name="request">The RtspRequest to authenticate</param>
        /// <param name="source">The RtspStream to authenticate against</param>
        /// <returns>True if authroized, otherwise false</returns>
        internal bool AuthenticateRequest(RtspRequest request, RtspSourceStream source)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (source == null) throw new ArgumentNullException("source");

            //If the source has no password then there is nothing to determine
            if (source.RemoteCredential == null) return true;
            //If the request does not have the authorization header then there is nothing else to determine
            if (!request.ContainsHeader(RtspHeaders.Authroization)) return false;

            //Get the header
            string header = request[RtspHeaders.Authroization].ToLower();
            if (header.Contains("basic"))
            {
                //Remove the parts
                header = header.Replace("basic ", string.Empty).Trim();
                //Get the decoded value
                header = request.Encoding.GetString(Convert.FromBase64String(header));
                //Get the parts
                string[] parts = header.Split(':');
                //If not enough parts nothing to compare
                if (parts.Length < 1) return false;
                //Return the determination by comparison
                return parts[0].Equals(source.RemoteCredential.UserName) && parts[2].Equals(source.RemoteCredential.Password);
            }
            else if (header.Contains("digest"))
            {
                //Digest RFC2069
                /*
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

                //http://en.wikipedia.org/wiki/Digest_access_authentication
                //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                //H1 = MD5(source.m_RtspCred.UserName +':' + realm + ':' + source.m_RtspCred.Password)
                //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                //H2 = MD5(request.Method +':' + uri)
                //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                //ResponseHash = MD5( HA1 + ':' + nonce + ':' + nc + ':' + cnonce + ':' + "qop=auth" + ':' + HA2);
            }

            //If invalid just end here with proper code? AuthRequired if the Request has no header
            //More likley modify to return a StatusCOde
            //or Forbidden if it didn't match
            //200 instead of TRUE?

            return false;
        }

        #endregion

        #endregion
    }
}
