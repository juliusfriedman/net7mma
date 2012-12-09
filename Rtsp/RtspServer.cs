using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Media.Rtp;
using Media.Rtcp;

namespace Media.Rtsp
{
    /// <summary>
    /// Implementation of Rfc 2326 server 
    /// http://tools.ietf.org/html/rfc2326
    /// </summary>
    public class RtspServer
    {
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

        /// <summary>
        /// The port the RtspServer is listening on, defaults to 554
        /// </summary>
        int m_ServerPort = 554,
            //Counters for bytes sent and recieved
            m_Recieved, m_Sent;

        /// <summary>
        /// The socket used for recieving RtspRequests
        /// </summary>
        Socket m_ServerSocket;

        /// <summary>
        /// The endpoint the server is listening on
        /// </summary>
        EndPoint m_ServerEndPoint;

        /// <summary>
        /// The dictionary containing all streams the server is aggregrating
        /// </summary>
        Dictionary<Guid, RtspStream> m_Streams = new Dictionary<Guid, RtspStream>();

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

        #endregion

        #region Propeties

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
        public IPEndPoint LocalEndPoint { get { return m_ServerSocket.LocalEndPoint as IPEndPoint; } }

        /// <summary>
        /// Accesses a contained stream by id of the stream
        /// </summary>
        /// <param name="streamId">The unique identifer</param>
        /// <returns>The RtspClient assocaited with the given id</returns>
        public RtspStream this[Guid streamId] { get { return m_Streams[streamId]; } }

        /// <summary>
        /// The amount of streams the server is prepared to listen to
        /// </summary>
        public int StreamCount { get { return m_Streams.Count; } }

        /// <summary>
        /// The amount of active streams the server is listening to
        /// </summary>
        public int ActiveStreamCount
        {
            get
            {
                if (StreamCount == 0) return 0;
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
                foreach(RtspStream client in m_Streams.Values.ToList())
                {
                    total += client.Listener.BytesRecieved;
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
                foreach (RtspStream stream in m_Streams.Values.ToList())
                {
                    total += stream.Listener.BytesSent;
                }
                return total;
            }
        }

        #endregion

        #region Methods

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
            rtspSessionId = rtspSessionId.Trim();
            return m_Clients.Values.Where(c => c.SessionId != null && c.SessionId.Equals(rtspSessionId)).FirstOrDefault();
        }       

        #endregion

        #region Stream Collection

        /// <summary>
        /// Adds a stream to the server. If the server is already started then the stream will also be started
        /// </summary>
        /// <param name="location">The uri of the stream</param>
        public void AddStream(RtspStream stream)
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
            lock (m_Streams)
            {
                return m_Streams.ContainsKey(streamId);
            }
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
                RtspStream client = m_Streams[streamId];
                client.Listener.Disconnect();
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

        /// <summary>
        /// TODO :: SHould handle /<GUID> requests and should handle /archive requests
        /// </summary>
        /// <param name="mediaLocation"></param>
        /// <returns></returns>
        public RtspStream FindStreamByLocation(Uri mediaLocation)
        {
            string val = mediaLocation.ToString();

            RtspStream found = null;

            if (val.Contains("live"))
            {

                foreach (RtspStream stream in m_Streams.Values.ToList())
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
                //Need facilites for creating a RtspStream from an archive file
            }

            return found;
        }

        #endregion

        #region Server Logic

        /// <summary>
        /// Finds and removes inactive clients.
        /// Determined by the time of the sessions last RecieversReport or the last RtspRequestRecieved (get parameter must be sent to keep from timing out)
        /// </summary>
        internal void PollClients()
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
                if ((DateTime.UtcNow - session.m_LastRecieversReportRecieved).TotalMinutes > 2 || (DateTime.UtcNow - session.m_LastRtspRequestRecieved).TotalMinutes > 2)
                {

                    if (!session.m_RtcpGoodbyeRecieved)
                    {
                        session.SendGoodbye();
                    }

                    RemoveSession(session);
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
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Bind the server Socket to the server EndPoint
            m_ServerSocket.Bind(m_ServerEndPoint);

            //Set the backlog
            m_ServerSocket.Listen(1024);

            //Create a thread to handle client connections
            m_ServerThread = new Thread(new ThreadStart(RecieveLoop));

            m_ServerThread.Name = "RtspServer@" + m_ServerPort;

            m_ServerThread.Start();
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

            //Disconnect the server
            //m_ServerSocket.Disconnect(true);

            //Shutdown the socket
            //m_ServerSocket.Shutdown(SocketShutdown.Both);

            //Dispose the socket
            m_ServerSocket.Dispose();            
        }

        /// <summary>
        /// Starts all streams contained in the video server
        /// </summary>
        internal virtual void StartStreams()
        {
            foreach(RtspStream stream in m_Streams.Values.ToList())
            {
                stream.Start();
            }
        }

        /// <summary>
        /// Stops all contained streams from streaming
        /// </summary>
        internal virtual void StopStreams()
        {
            foreach (RtspStream stream in m_Streams.Values.ToList())
            {
                stream.Stop();
            }
        }

        /// <summary>
        /// The loop where Rtsp Requests are recieved
        /// </summary>
        internal virtual void RecieveLoop()
        {
            while (!m_StopRequested)
            {
                allDone.Reset();
                m_ServerSocket.BeginAccept(new AsyncCallback(ProcessAccept), m_ServerSocket);

                while (!allDone.WaitOne(100))
                {
                    PollClients();
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

                clientSocket.BeginReceive(ci.m_Buffer, 0, ci.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessRecieve), ci);
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
        internal void ProcessRecieve(IAsyncResult ar)
        {
            //Get the client information
            RtspSession ci = (RtspSession)ar.AsyncState;

            if (null == ci) return;

            try
            {
                //Data is now in client buffer
                int rec = ci.m_RtspSocket.EndReceive(ar);

                if (rec > 0)
                {
                    m_Recieved += rec;
                    ci.m_RtspRecieved += rec;
                    RtspRequest request = new RtspRequest(ci.m_Buffer);
                    ProcessRtspRequest(request, ci);
                }
            }
            catch
            {
                ProcessInvalidRtspRequest(ci);
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

                ci.m_RtspSent += sent;

                m_Sent += sent;

                //Start recieve again (Might need to have a flag for the client because in Tcp this will not work so well 
                ci.m_RtspSocket.BeginReceive(ci.m_Buffer, 0, ci.m_Buffer.Length, SocketFlags.None, new AsyncCallback(ProcessRecieve), ci);
            }
            catch (SocketException ex)
            {
                //User bailed
                System.Diagnostics.Debug.WriteLine("Socket Exception in ProcessSend: " + ex.ToString());
            }
        }

        #endregion

        #region Rtsp Handlers

        /// <summary>
        /// Processes a RtspRequest based on the contents
        /// </summary>
        /// <param name="request">The rtsp Request</param>
        /// <param name="session">The client information</param>
        internal void ProcessRtspRequest(RtspRequest request, RtspSession session)
        {
            //All requests need the CSeq
            if (!request.ContainsHeader(RtspMessage.RtspHeaders.CSeq)) ProcessInvalidRtspRequest(session);

            //If there is a body and no content-length
            if (string.IsNullOrWhiteSpace(request.Body) && !request.ContainsHeader(RtspMessage.RtspHeaders.ContentLength)) ProcessInvalidRtspRequest(session);

            //Optional
            //if (!request.ContainsHeader(RtspMessage.RtspHeaders.UserAgent)) ProcessInvalidRtspRequest(session, RtspResponse.ResponseStatusCode.InternalServerError);
            
            //Need the property for this
            //if (request.Version != ServerVersion) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.RTSPVersionNotSupported);

            //Synchronize the server and client
            session.Location = request.Location;
            session.LastMethod = request.Method;
            session.SequenceNumber = request.CSeq;
            session.m_LastRtspRequestRecieved = DateTime.UtcNow;

            //Determine the handler for the request and process it
            switch (request.Method)
            {
                case RtspMessage.RtspMethod.OPTIONS:
                    {
                        ProcessRtspOptions(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.DESCRIBE:
                    {
                        ProcessRtspDescribe(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.SETUP:
                    {
                        ProcessRtspSetup(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.PLAY:
                    {
                        ProcessRtspPlay(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.PAUSE:
                    {
                        ProcessRtspPause(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.TEARDOWN:
                    {
                        ProcessRtspTeardown(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.GET_PARAMETER:
                    {
                        ProcessGetParameter(request, session);
                        break;
                    }
                case RtspMessage.RtspMethod.UNKNOWN:
                default:                
                    {
                        ProcessInvalidRtspRequest(session, RtspResponse.ResponseStatusCode.MethodNotAllowed);
                        break;
                    }
            }

        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="response">The RtspResponse to send</param>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessSendRtspResponse(RtspResponse response, RtspSession ci)
        {
            try
            {
                byte[] buffer = response.ToBytes();
                ci.m_RtspSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ProcessSend), ci);                
            }
            catch (SocketException)
            {
                //Most likely a tear down
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="ci">The client session to send the response on</param>
        /// <param name="code">The status code of the response if other than BadRequest</param>
        internal void ProcessInvalidRtspRequest(RtspSession ci, RtspResponse.ResponseStatusCode code = RtspResponse.ResponseStatusCode.BadRequest)
        {
            RtspResponse response = new RtspResponse();
            response.StatusCode = code;
            response.CSeq = ci.SequenceNumber;
            //Should allow a reason to be put into the response somehow
            ProcessSendRtspResponse(response, ci);
        }

        /// <summary>
        /// Sends a Rtsp LocationNotFound Response
        /// </summary>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessLocationNotFoundRtspRequest(RtspSession ci)
        {
            ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.NotFound);
        }        

        internal void ProcessRtspOptions(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("OPTIONS " + request.Location);

            RtspStream found = FindStreamByLocation(request.Location);

            //No stream with name
            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(ci);
                return;
            }

            RtspResponse resp = ci.CreateRtspResponse(request);
            //resp.SetHeader(RtspMessage.RtspHeaders.Public, "OPTIONS, DESCRIBE, SETUP, PLAY, TEARDOWN, GET_PARAMETER");
            resp.SetHeader(RtspMessage.RtspHeaders.Public, " DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER"/*, OPTIONS"*/);
            ProcessSendRtspResponse(resp, ci);

        }

        internal void ProcessRtspDescribe(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("DESCRIBE " + request.Location);

            string acceptHeader = request[RtspMessage.RtspHeaders.Accept];

            if (string.IsNullOrWhiteSpace(acceptHeader) || acceptHeader.Trim() != "application/sdp") ProcessInvalidRtspRequest(ci);

            RtspStream found = FindStreamByLocation(request.Location);

            if (found == null) ProcessLocationNotFoundRtspRequest(ci);

            if (!ProcessAuthenticateRequest(request, found)) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.Unauthorized);

            RtspResponse resp = ci.CreateRtspResponse(request);


            resp.SetHeader(RtspMessage.RtspHeaders.ContentBase, "rtsp://" + ((IPEndPoint)ci.m_RtspSocket.LocalEndPoint).Address.ToString() + "/live/" + found.Name);//Might should be Id and not name
            resp.SetHeader(RtspMessage.RtspHeaders.ContentType, "application/sdp");

            //Create the SDP from the found media
            CreateSessionDescription(found, ci);
            resp.Body = ci.SessionDescription.ToString();

            ProcessSendRtspResponse(resp, ci);
        }

        internal void ProcessRtspSetup(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("SETUP " + request.Location);

            RtspStream found = FindStreamByLocation(request.Location);
            if (found == null) ProcessLocationNotFoundRtspRequest(ci);
            if (!ProcessAuthenticateRequest(request, found))
            {
                ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.Unauthorized);
                return;
            }

            //Create ci SessionID
            //Allocate ports in session     

            string transportHeader = request[RtspMessage.RtspHeaders.Transport];

            if (string.IsNullOrWhiteSpace(transportHeader)) ProcessInvalidRtspRequest(ci);

            string clientPortDirective = null; //comes from transportHeader client_port=

            string[] parts = transportHeader.Split(';');

            ProtocolType requestedProtcolType = ProtocolType.Udp;

            for (int i = 0, e = parts.Length; i < e; ++i)
            {
                string part = parts[i].Trim();
                if (part == "RTP/AVP" || part == "RTP/AVP/UDP") requestedProtcolType = ProtocolType.Udp;
                else if (part == "RTP/AVP/TCP") requestedProtcolType = ProtocolType.Tcp;
                else if (part.StartsWith("interleaved="))
                {
                    string [] channels = part.Split('-');
                    if (channels.Length > 1)
                    {
                        ci.m_RtpChannel = (byte)Convert.ToInt32(channels[0]);
                        ci.m_RtpChannel = (byte)Convert.ToInt32(channels[1]);
                    }
                }
                else if (part.StartsWith("client_port="))
                {
                    clientPortDirective = part.Replace("client_port=", string.Empty);
                }
            }

            string returnTransportHeader = null;

            //If there was no client port we cannot setup the media
            if (clientPortDirective == null) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.InternalServerError);

            string[] ports = clientPortDirective.Split('-');
            if (ports.Length > 1)
            {
                //The client requests Udp
                ci.TransportProtcol = ProtocolType.Udp;
                ci.InitializeRtp(Convert.ToInt32(ports[0]), Convert.ToInt32(ports[1]));
                returnTransportHeader = "RTP/AVP/UDP;unicast;client_port=" + clientPortDirective + ";server_port=" + ci.m_ServerRtpPort + "-" + ci.m_ServerRtcpPort + ";mode=\"PLAY\";ssrc=" + ci.m_RtpSSRC;
            }
            else
            {
                //The client requests Tcp
                ci.TransportProtcol = ProtocolType.Tcp;
                ci.InitializeRtp(Convert.ToInt32(ports[0]), Convert.ToInt32(ports[0]));
                returnTransportHeader = "RTP/AVP/TCP;unicast;client_port=" + clientPortDirective + ";server_port=" + ci.m_ServerRtpPort + ";mode=\"PLAY\";ssrc=" + ci.m_RtpSSRC;
            }

            //Create the response
            RtspResponse resp = ci.CreateRtspResponse(request);
            resp.AppendHeader(RtspMessage.RtspHeaders.Session, "timeout=60");
            resp.SetHeader(RtspMessage.RtspHeaders.Transport, returnTransportHeader);

            //Send the response
            ProcessSendRtspResponse(resp, ci);
        }

        internal void ProcessRtspPlay(RtspRequest request, RtspSession ci)
        {

            System.Diagnostics.Debug.WriteLine("PLAY " + request.Location);

            //if there is a sessionid present
            string sessionId = request[Rtsp.RtspMessage.RtspHeaders.Session];

            if (string.IsNullOrWhiteSpace(sessionId)) ProcessInvalidRtspRequest(ci);
            RtspSession session = FindSessionByRtspSessionId(sessionId);
            if (session == null) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.SessionNotFound);

            RtspStream found = FindStreamByLocation(request.Location);
            if (found == null) ProcessLocationNotFoundRtspRequest(ci);
            if (!ProcessAuthenticateRequest(request, found)) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.Unauthorized);

            //Hook events on found stream to send data to appropriate sockets on RtspSession              
            found.Listener.OnRtpPacketRecieved += ci.OnSourceRtpPacketRecieved;
            found.Listener.OnRtcpPacketRecieved += ci.OnSourceRtcpPacketRecieved;

            string rangeHeader = request[Rtsp.RtspMessage.RtspHeaders.Range];

            if (string.IsNullOrWhiteSpace(rangeHeader))
            {
                ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.BadRequest);
                return;
            }

            //Create a response
            RtspResponse response = ci.CreateRtspResponse(request);

            //Wait to ensure sequence number is correct... stupid but... the only other wait is to hand the request off or use a manual event. Since we are on a thread I dont think this is that bad
            //while (ci.m_FirstRtpPacket == null) Thread.Sleep(100);
            
            //Might should be Id and not name
            response.SetHeader(Rtsp.RtspMessage.RtspHeaders.RtpInfo, "url=rtsp://" + ((IPEndPoint)(ci.m_RtspSocket.LocalEndPoint)).Address + "/live/" + found.Name + "/video");//seqno=" + ci.m_FirstRtpPacket.SequenceNumber);// + ";rtptime=" + ci.m_LastRtpPacket.TimeStamp);
            response.SetHeader(Rtsp.RtspMessage.RtspHeaders.Range, "npt=0.000-0.000");

            ProcessSendRtspResponse(response, ci);
            
        }

        internal void ProcessRtspPause(RtspRequest request, RtspSession ci)
        {

            //if there is a sessionid present
            string sessionId = request[Rtsp.RtspMessage.RtspHeaders.Session];

            if (string.IsNullOrWhiteSpace(sessionId)) ProcessInvalidRtspRequest(ci);
            RtspSession session = FindSessionByRtspSessionId(sessionId);
            if (session == null) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.SessionNotFound);

            RtspStream found = FindStreamByLocation(request.Location);
            if (found == null) ProcessLocationNotFoundRtspRequest(ci);
            if (!ProcessAuthenticateRequest(request, found))
            {
                ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.Unauthorized);
                return;
            }

            //unhook events, will be re hooked in play
            found.Listener.OnRtpPacketRecieved -= ci.OnSourceRtpPacketRecieved;
            found.Listener.OnRtcpPacketRecieved -= ci.OnSourceRtcpPacketRecieved;

            //Might need to add some headers
            ProcessSendRtspResponse(ci.CreateRtspResponse(request), ci);

        }     

        internal void ProcessRtspTeardown(RtspRequest request, RtspSession ci)
        {
            System.Diagnostics.Debug.WriteLine("TEARDOWN " + request.Location);

            try
            {
                //if there is a sessionid present
                string sessionId = request[Rtsp.RtspMessage.RtspHeaders.Session];

                if (string.IsNullOrWhiteSpace(sessionId)) ProcessInvalidRtspRequest(ci);
                RtspSession session = FindSessionByRtspSessionId(sessionId);
                if (session == null) ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.SessionNotFound);

                RtspStream found = FindStreamByLocation(request.Location);
                if (found == null) ProcessLocationNotFoundRtspRequest(ci);
                if (!ProcessAuthenticateRequest(request, found))
                {
                    ProcessInvalidRtspRequest(ci, RtspResponse.ResponseStatusCode.Unauthorized);
                    return;
                }

                //unhook events on found stream to send data to appropriate sockets on RtspSession              
                found.Listener.OnRtpPacketRecieved -= ci.OnSourceRtpPacketRecieved;
                found.Listener.OnRtcpPacketRecieved -= ci.OnSourceRtcpPacketRecieved;

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
                RemoveSession(ci);
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
        internal bool ProcessAuthenticateRequest(RtspRequest request, RtspStream source)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (source == null) throw new ArgumentNullException("source");

            //If the source has no password then there is nothing to determine
            if (source.RtspCredential == null) return true;
            //If the request does not have the authorization header then there is nothing else to determine
            if (!request.ContainsHeader(RtspMessage.RtspHeaders.Authroization)) return false;

            //Get the header
            string header = request[RtspMessage.RtspHeaders.Authroization];
            //Remove the parts
            header = header.Replace("Basic ", string.Empty).Trim();
            //Get the decoded value
            header = request.Encoding.GetString(Convert.FromBase64String(header));
            //Get the parts
            string[] parts = header.Split(':');
            //If not enough parts nothing to compare
            if (parts.Length < 1) return false;
            //Return the determination by comparison
            return parts[0] == source.m_RtspCred.UserName && parts[2] == source.m_RtspCred.Password;
        }

        #endregion

        #region Npt

        internal static DateTime TimestampToDateTime(UInt64 seconds, UInt64 fractions)
        {
            UInt64 ticks = (seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L);
            if ((seconds & 0x80000000L) == 0)
            {
                return Epoch1 + TimeSpan.FromTicks((Int64)ticks);
            }
            else
            {
                return Epoch + TimeSpan.FromTicks((Int64)ticks);
            }
        }

        internal static UInt64[] DateTimeToTimestamp(DateTime dateTime)
        {
            DateTime baseDate;

            if (dateTime >= Epoch1) baseDate = Epoch1;
            else baseDate = Epoch;

            UInt64 ticks = (UInt64)(dateTime - baseDate).Ticks;
            UInt64 seconds = ticks / TimeSpan.TicksPerSecond;
            UInt64 fractions = ((ticks % TimeSpan.TicksPerSecond) * 0x100000000L) / TimeSpan.TicksPerSecond;

            return new UInt64[] { seconds, fractions };
        }
     
        static DateTime Epoch1 = new DateTime(2036, 2, 7, 6, 28, 16).ToUniversalTime();

        static DateTime Epoch = new DateTime(1900, 1, 1, 1, 0, 0).ToUniversalTime();

        #endregion

        #region Server Utility Functions

        /// <summary>
        /// Creates a SessionDescription based on the given stream 
        /// </summary>
        /// <param name="stream">The stream to create the SessionDescription from</param>
        /// <param name="session">The session to store the SessionDescription to</param>
        static internal void CreateSessionDescription(RtspStream stream, RtspSession session)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (session == null) throw new ArgumentNullException("session");

            //Get the original so we can rewrite it
            Media.Sdp.SessionDescription origional = stream.Listener.SessionDescription;

            //http://www.ietf.org/rfc/rfc2327.txt - Page 8 the "o=" field.
            //Might choose to move this stuff to the SessionDescription Constructor

            //Should be 2 NTP Timestamps
            ulong[] parts = DateTimeToTimestamp(DateTime.Now.ToUniversalTime());
            string sessionId = parts[0].ToString(),
                sessionVersion = parts[1].ToString();

            string originatorString = "- " + sessionId + " " + sessionVersion + " IN IP4 " + ((IPEndPoint)session.m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI Streaming Session"; // + stream.Name 

            //Make the new SessionDescription
            Media.Sdp.SessionDescription result = new Sdp.SessionDescription(origional.Version, originatorString, sessionName);

            //Copy the old one
            origional.CopyTo(result);

            //If we were given a session than update it with the id, version and SessionDescription
            session.SessionId = sessionId;
            session.m_SDPVersion = sessionVersion;
            session.m_SessionDescription = result;
        }

        static internal int FindOpenUDPPort(int start = 30000)
        {
            int port = start;

            foreach (IPEndPoint ep in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Where(ep => ep.Port >= port))
            {
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;
            }

            //Only odd ports
            if (port % 2 == 0) return port;
            return ++port;
        }

        static internal int FindOpenTCPPort(int start = 30000)
        {
            int port = start;

            foreach (IPEndPoint ep in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Where(ep => ep.Port >= port))
            {
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;
            }

            //Only odd ports
            if (port % 2 == 0) ++port;

            return port;
        }

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        static internal IPAddress GetV4IPAddress()
        {
            return GetFirstIPAddress(System.Net.Sockets.AddressFamily.InterNetwork);
        }

        static internal IPAddress GetFirstIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == addressFamily) return ip;
            return IPAddress.Loopback;
        }

        #endregion

        #endregion
    }
}
