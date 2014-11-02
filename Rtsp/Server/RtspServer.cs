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
using Media.Rtp;
using Media.Rtcp;
using Media.Rtsp.Server.Streams;

namespace Media.Rtsp
{
    /// <summary>
    /// Implementation of Rtsp / RFC2326 server 
    /// http://tools.ietf.org/html/rfc2326
    /// Suppports Reliable(Rtsp / Tcp or Rtsp / Http) and Unreliable(Rtsp / Udp) connections
    /// </summary>
    public class RtspServer : Common.BaseDisposable
    {
        public const int DefaultPort = 554;

        public const int DefaultReceiveTimeout = 1000;

        public const int DefaultSendTimeout = 1000;

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

        int m_MaximumClients = 1024;

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
        Dictionary<Guid, IMediaStream> m_Streams = new Dictionary<Guid, IMediaStream>();

        /// <summary>
        /// The dictionary containing all the clients the server has sessions assocaited with
        /// </summary>
        Dictionary<Guid, ClientSession> m_Clients = new Dictionary<Guid, ClientSession>();

        public Media.Rtsp.Server.RtspStreamArchiver Archiver { get; set; }

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
        ManualResetEventSlim allDone = new ManualResetEventSlim(false);

        //Handles the Restarting of streams which needs to be and disconnects clients which are inactive.
        internal Timer m_Maintainer;

        #endregion

        #region Propeties

        internal IEnumerable<ClientSession> Clients { get { return m_Clients.Values.AsEnumerable(); } }

        /// <summary>
        /// Stores Uri prefixes and their associated credentials.
        /// </summary>
        internal CredentialCache RequiredCredentials { get; set; }

        /// <summary>
        /// The Version of the RtspServer (used in responses)
        /// </summary>
        public double Version { get { return m_Version; } protected set { if (value < m_Version) throw new ArgumentOutOfRangeException(); m_Version = value; } }

        /// <summary>
        /// Indicates if requests require a User Agent
        /// </summary>
        public bool RequireUserAgent { get; set; }

        /// <summary>
        /// Indicates if setup requests require a Range Header
        /// </summary>
        public bool RequireRangeHeader { get; set; }

        /// <summary>
        /// The name of the server (used in responses)
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// The amount of time before the RtpServer will remove a session if no Rtsp activity has occured.
        /// </summary>
        /// <remarks>Probably should back this property and ensure that the value is !0</remarks>
        public int RtspClientInactivityTimeoutSeconds { get; set; }

        //Check access on these values, may not need to make them available public

        /// <summary>
        /// Gets or sets the ReceiveTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int ReceiveTimeout { get { return m_TcpServerSocket.ReceiveTimeout; } set { m_TcpServerSocket.ReceiveTimeout = value; } }

        /// <summary>
        /// Gets or sets the SendTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int SendTimeout { get { return m_TcpServerSocket.SendTimeout; } set { m_TcpServerSocket.SendTimeout = value; } }

        ///// <summary>
        ///// The amount of time before the RtpServer will remove a session if no Rtp activity has occured.
        ///// </summary>
        //public int ClientRtpInactivityTimeoutSeconds { get; set; }

        //For controlling Port ranges, Provide events so Upnp support can be plugged in? PortClosed/PortOpened(ProtocolType, startPort, endPort?)
        public int? MinimumUdpPort { get; set; } 
        internal int? MaximumUdpPort { get; set; }

        /// <summary>
        /// The maximum amount of connected clients
        /// </summary>
        public int MaximumClients { get { return m_MaximumClients; } set { if (value <= 0) throw new ArgumentOutOfRangeException(); m_MaximumClients = value; } }

        /// <summary>
        /// The amount of time the server has been running
        /// </summary>
        public TimeSpan Uptime { get { if (m_Started.HasValue) return DateTime.UtcNow - m_Started.Value; return TimeSpan.Zero; } }

        /// <summary>
        /// Indicates if the RtspServer is listening for requests on the ServerPort
        /// </summary>
        public bool Listening { get { return m_ServerThread != null && m_ServerThread.ThreadState.HasFlag(ThreadState.Running); } }

        /// <summary>
        /// The port in which the RtspServer is listening for requests
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
        public IMediaStream this[Guid streamId] { get { return GetStream(streamId); } }

        /// <summary>
        /// The streams contained in the server
        /// </summary>
        /// <remarks>Change to SourceStream and move counters there</remarks>
        public IEnumerable<IMediaStream> Streams { get { lock (m_Streams) return m_Streams.Values.AsEnumerable(); } }

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
                return Streams.Where(s => s.State == SourceStream.StreamState.Stopped && s.Ready == true).Count();
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
        /// The amount of bytes recieved from all contained streams in the RtspServer (Might want to log the counters seperately so the totals are not lost with the streams or just not provide the property)
        /// </summary>
        public long TotalStreamBytesRecieved
        {
            get
            {
                return Streams.OfType<RtpSource>().Sum(s => s.RtpClient != null ? s.RtpClient.TotalRtpBytesReceieved : 0);
            }
        }

        /// <summary>
        /// The amount of bytes sent to all contained streams in the RtspServer (Might want to log the counters seperately so the totals are not lost with the streams or just not provide the property)
        /// </summary>
        public long TotalStreamBytesSent
        {
            get
            {
                return Streams.OfType<RtpSource>().Sum(s => s.RtpClient != null ? s.RtpClient.TotalRtpBytesSent : 0);
            }
        }

        public int ConnectedClients { get { return m_Clients.Count; } }

        public Rtsp.Server.RtspServerLogger Logger { get; set; }

        public bool HttpEnabled { get { return m_HttpPort != -1; } }

        public bool UdpEnabled { get { return m_UdpPort != -1; } }

        #endregion

        #region Events

        //RequestReceived
        //ResponseSent

        //Expose Session?
        //SessionCreated
        //SessionDisconnected

        #endregion

        #region Constructor

        public RtspServer(int listenPort = DefaultPort)
        {
            //Handle this according to RFC
            RtspClientInactivityTimeoutSeconds = 60;
            ServerName = "ASTI Media Server RTSP " + Version; //, RTSP " + Version; //Google does this, but it causes unintended results with VLC and other players in some cases (which may be needed to get crappy software revamped)
            m_ServerPort = listenPort;
            RequiredCredentials = new CredentialCache();
        }

        #endregion

        #region Methods

        int m_HttpPort = -1;
        public void EnableHttpTransport(int port = 80) 
        {

            throw new NotImplementedException();

            //if (m_HttpListner == null)
            //{
            //    try
            //    {
            //        m_HttpListner = new HttpListener();
            //        m_HttpPort = port;
            //        m_HttpListner.Prefixes.Add("http://*:" + port + "/");
            //        m_HttpListner.Start();
            //        m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), null);
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new RtspServerException("Error Enabling Http on Port '" + port + "' : " + ex.Message, ex);
            //    }
            //}
        }

        
        /*
         2.0 Draft specifies that servers now respond with a 501 to any rtspu request, we can blame that on lack of interest...
         */

        //Impove this...
        int m_UdpPort = -1;

        public void EnableUnreliableTransport(int port = 555, bool ipV6 = false) 
        {
            if (m_UdpServerSocket == null)
            {
                try
                {
                    m_UdpPort = port;

                    EndPoint inBound;

                    if (ipV6)
                    {
                        m_UdpServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        m_UdpServerSocket.Bind(inBound = new IPEndPoint(Utility.GetFirstIPAddress(AddressFamily.InterNetworkV6), port));
                    }
                    else
                    {
                        m_UdpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        m_UdpServerSocket.Bind(inBound = new IPEndPoint(Utility.GetFirstV4IPAddress(), port));
                    }

                    //Include the IP Header
                    m_UdpServerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    m_UdpServerSocket.BeginReceiveFrom(Utility.Empty, 0, 0, SocketFlags.Partial, ref inBound, ProcessAccept, m_UdpServerSocket);
                }
                catch (Exception ex)
                {
                    throw new RtspServerException("Error Enabling Udp on Port '" + port + "' : " + ex.Message, ex);
                }
            }
        }

        public void DisableHttpTransport()
        {
            if (m_HttpListner != null)
            {
                m_HttpListner.Stop();
                m_HttpListner.Close();
                m_HttpListner = null;
            }
        }

        public void DisableUnreliableTransport()
        {
            if (m_UdpServerSocket != null)
            {
                m_UdpServerSocket.Shutdown(SocketShutdown.Both);
                m_UdpServerSocket.Dispose();
                m_UdpServerSocket = null;
            }
        }

        /// <summary>
        /// Stops the server and removes all streams
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();
         
            Stop();

            foreach (var stream in m_Streams.ToArray())
            {
                stream.Value.Dispose();
                m_Streams.Remove(stream.Key);
            }
        }

        #region Session Collection

        internal void AddSession(ClientSession session)
        {
            m_Clients.Add(session.Id, session);
        }

        internal bool RemoveSession(ClientSession session)
        {
            if (session == null) return false;            
            session.Dispose();
            return m_Clients.Remove(session.Id);
        }

        internal bool ContainsSession(ClientSession session)
        {
            return m_Clients.ContainsKey(session.Id);
        }

        internal ClientSession GetSession(Guid id)
        {
            ClientSession result;
            m_Clients.TryGetValue(id, out result);
            return result;
        }

        internal ClientSession GetSession(string rtspSessionId)
        {
            if (string.IsNullOrWhiteSpace(rtspSessionId)) return null;
            rtspSessionId = rtspSessionId.Trim();
            return Clients.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.SessionId) && c.SessionId.Equals(rtspSessionId));
        }              

        #endregion

        #region Stream Collection

        /// <summary>
        /// Adds a stream to the server. If the server is already started then the stream will also be started
        /// </summary>
        /// <param name="location">The uri of the stream</param>
        public void AddStream(IMediaStream stream)
        {
            if (ContainsStream(stream.Id)) throw new RtspServerException("Cannot add the given stream because it is already contained in the RtspServer");
            else
            {
                lock (m_Streams)
                {
                    //Remember to have clients indicate PlayFromStart if they want all sessions to start at 0
                    m_Streams.Add(stream.Id, stream);
                }

                //If we are listening start the stram
                if (Listening) stream.Start();
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
        /// <param name="stop">True if the stream should be stopped when removed</param>
        /// <returns>True if removed, otherwise false</returns>
        public bool RemoveStream(Guid streamId, bool stop = true)
        {
            try
            {
                if (m_Streams.ContainsKey(streamId))
                {
                    IMediaStream source = this[streamId];
                    if (stop) source.Stop();

                    if (RequiredCredentials != null)
                    {
                        RemoveCredential(source, "Basic");
                        RemoveCredential(source, "Digest");
                    }

                    lock (m_Streams)
                    {
                        return m_Streams.Remove(streamId);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }            
        }

        public IMediaStream GetStream(Guid streamId)
        {
            IMediaStream result;
            m_Streams.TryGetValue(streamId, out result);
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="mediaLocation"></param>
        /// <returns></returns>
        internal IMediaStream FindStreamByLocation(Uri mediaLocation)
        {
            IMediaStream found = null;

            string streamBase = null, streamName = null;

            streamBase = mediaLocation.Segments.Any(s=>s.ToLowerInvariant().Contains("live")) ? "live" : "archive";

            streamName = mediaLocation.Segments.Last().ToLowerInvariant().Replace("/", string.Empty);

            if (string.IsNullOrWhiteSpace(streamName)) streamName = mediaLocation.Segments[mediaLocation.Segments.Length - 1].ToLowerInvariant().Replace("/",string.Empty);
            else if (streamName == "video" || streamName == "audio") streamName = mediaLocation.Segments[mediaLocation.Segments.Length - 2].ToLowerInvariant().Replace("/", string.Empty);

            //If either the streamBase or the streamName is null or Whitespace then return null (no stream)
            if (string.IsNullOrWhiteSpace(streamBase) || string.IsNullOrWhiteSpace(streamName)) return null;


            //handle live streams
            if (streamBase == "live")
            {
                foreach (IMediaStream stream in Streams)
                {

                    //If the name matches the streamName or stream Id then we found it
                    if (stream.Name.ToLowerInvariant() == streamName || stream.Id.ToString().ToLowerInvariant() == streamName)
                    {
                        found = stream;
                        break;
                    }

                    //Try aliases of streams
                    if (found == null)
                    {
                        foreach (string alias in stream.Aliases)
                        {
                            if (alias.ToLowerInvariant() == streamName)
                            {
                                found = stream;
                                break;
                            }
                        }
                    }

                    //Try by media description?

                }
            }
            else
            {
                //Find Archive File by Uri
                //Create RtpSoure from file
                //Return found
            }

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
        public void AddCredential(SourceStream source, NetworkCredential credential, string authType)
        {
            RequiredCredentials.Add(source.ServerLocation, authType, credential);
        }

        /// <summary>
        /// Removes a Credential from the CredentialCache of the RtspServer for the given SourceStream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="authType"></param>
        public void RemoveCredential(IMediaStream source, string authType)
        {
            RequiredCredentials.Remove(source.ServerLocation, authType);
        }

        /// <summary>
        /// Finds and removes inactive clients.
        /// Determined by the time of the sessions last RecieversReport or the last RtspRequestRecieved (get parameter must be sent to keep from timing out)
        /// </summary>
        internal void DisconnectAndRemoveInactiveSessions(object state = null) { DisconnectAndRemoveInactiveSessions(); }

        internal void DisconnectAndRemoveInactiveSessions()
        {
            try
            {
                DateTime maintenanceStarted = DateTime.UtcNow;

                List<Guid> inactive = new List<Guid>();

                foreach (ClientSession session in Clients)
                {
                    if (session.Disposed)
                    {
                        inactive.Add(session.Id);
                        continue;
                    }

                    //Check if a GET_PARAMETER has NOT been received in the allowed time
                    if (RtspClientInactivityTimeoutSeconds != -1 && session.LastResponse != null && (maintenanceStarted - session.LastResponse.Created).TotalSeconds > RtspClientInactivityTimeoutSeconds)
                    {
                        inactive.Add(session.Id);
                        continue;
                    }

                    if (session.LastRequest != null && session.LastRequest.Method == RtspMethod.TEARDOWN && session.Attached.Count() == 0) inactive.Add(session.Id);

                }

                if (inactive.Count > 0) foreach (var id in inactive)
                    {
                        ClientSession cs;
                        if (m_Clients.TryGetValue(id, out cs)) RemoveSession(cs);
                        cs = null;
                    }

            }
            catch { return; }
        }


        /// <summary>
        /// Restarted streams which should be Listening but are not
        /// </summary>
        internal void RestartFaultedStreams(object state = null) { RestartFaultedStreams(); }
        internal void RestartFaultedStreams()
        {
            foreach (IMediaStream stream in Streams.Where(s => s.State == RtspSourceStream.StreamState.Started && s.Ready == false))
            {
                //Ensure Stopped
                stream.Stop();

                //try to start it again
                stream.Start();
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

            //Create the server Socket (Should allow InterNetworkV6)
            m_TcpServerSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //Bind the server Socket to the server EndPoint
            m_TcpServerSocket.Bind(m_ServerEndPoint);

            //Set the backlog
            m_TcpServerSocket.Listen(MaximumClients);

            //Set the recieve timeout
            m_TcpServerSocket.ReceiveTimeout = DefaultReceiveTimeout;

            m_TcpServerSocket.SendTimeout = DefaultSendTimeout;

            //Create a thread to handle client connections
            m_ServerThread = new Thread(new ThreadStart(RecieveLoop));
            m_ServerThread.Name = "RtspServer@" + m_ServerPort;
            m_ServerThread.TrySetApartmentState(ApartmentState.MTA);
            //m_ServerThread.Priority = ThreadPriority.BelowNormal;
            m_ServerThread.Start();

            //Should allow all this frequencies to be controlled with a property (used half the amount of the RtspClientInactivityTimeoutSeconds)
            int frequency = RtspClientInactivityTimeoutSeconds > 0 ? RtspClientInactivityTimeoutSeconds / 1000 : 3000;
            
            m_Maintainer = new Timer(new TimerCallback(MaintainServer), null, frequency, System.Threading.Timeout.Infinite);

            m_Started = DateTime.UtcNow;

            if (m_UdpPort != -1) EnableUnreliableTransport(m_UdpPort);
            if (m_HttpPort != -1) EnableHttpTransport(m_HttpPort);

        }

        /// <summary>
        /// Removes Inactive Sessions and Restarts Faulted Streams
        /// </summary>
        /// <param name="state">Reserved</param>
        internal virtual void MaintainServer(object state = null)
        {
            if (Disposed || m_Maintainer == null) return;

            RestartFaultedStreams(state);
            DisconnectAndRemoveInactiveSessions(state);

            int frequency = RtspClientInactivityTimeoutSeconds > 0 ? RtspClientInactivityTimeoutSeconds / 1000 : 3000;

            m_Maintainer.Change(frequency, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Stops recieving RtspRequests and stops streaming all contained streams
        /// </summary>
        public virtual void Stop()
        {
            //If there is not a server thread return
            if (m_ServerThread == null) return;

            //Stop listening for requests
            m_StopRequested = true;

            if (m_Maintainer != null)
            {
                m_Maintainer.Dispose();
                m_Maintainer = null;
            }

            //Stop listening on client streams
            StopStreams();

            //Remove all clients
            foreach (ClientSession cs in Clients.ToArray())
            {
                RemoveSession(cs);
            }

            Utility.Abort(ref m_ServerThread);

            //Dispose the socket
            m_TcpServerSocket.Dispose();

            //Stop other listeners
            DisableHttpTransport();
            DisableUnreliableTransport();

            //Erase statistics
            m_Started = null;
        }

        /// <summary>
        /// Starts all streams contained in the video server
        /// </summary>
        internal virtual void StartStreams()
        {
            foreach (IMediaStream stream in Streams)
            {
                try
                {
                    stream.Start();
                }
                catch
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Stops all contained streams from streaming
        /// </summary>
        internal virtual void StopStreams()
        {
            foreach (IMediaStream stream in Streams.ToArray())
            {
                try
                {
                    stream.Stop();
                }
                catch
                {
                    continue;
                }
            }
        }        

        /// <summary>
        /// The loop where Rtsp Requests are recieved
        /// </summary>
        internal virtual void RecieveLoop()
        {
            try
            {
                int timeOut = 7;

                while (!m_StopRequested)
                {
                    //If we can accept
                    if (m_Clients.Count < m_MaximumClients)
                    {
                        //Get the timeout from the socket
                        timeOut = m_TcpServerSocket.ReceiveTimeout;

                        //If the timeout is infinite only wait for the default
                        if (timeOut <= 0) timeOut = DefaultReceiveTimeout;

                        allDone.Reset();

                        //Start acceping 
                        var iar = m_TcpServerSocket.BeginAccept(0, new AsyncCallback(ProcessAccept), m_TcpServerSocket);

                        //Wait half using the event
                        while (!iar.IsCompleted && !allDone.Wait(timeOut / 2) && !m_StopRequested)
                        {
                            //Wait the other half looking for the stop
                            if (allDone.Wait(timeOut / 2) || m_StopRequested | iar.IsCompleted) break;
                            //Check not waiting all day...
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                m_StopRequested = true;
                //All workers threads which exist now exit.
                return;
            }
            catch { throw; }
        }

        #endregion

        #region Socket Methods

        /// <summary>
        /// Handles the accept of rtsp client sockets into the server
        /// </summary>
        /// <param name="ar">IAsyncResult with a Socket object in the AsyncState property</param>
        internal void ProcessAccept(IAsyncResult ar)
        {
            if (ar == null) goto End;
            
            //The ClientSession created
            ClientSession created = null;
            
            try
            {
                //The Socket needed to create a ClientSession
                Socket clientSocket = null;

                //See if there is a socket in the state object
                Socket server = (Socket)ar.AsyncState;

                //If there is no socket then an accept has cannot be performed
                if (server == null) goto End;

                //If this is the inital receive for a Udp or the server given is UDP
                if (server.ProtocolType == ProtocolType.Udp)
                {
                    //Should always be 0 for our server any servers passed in
                    int acceptBytes = server.EndReceive(ar);

                    //Start receiving again if this was our server
                    if (m_UdpServerSocket.Handle == server.Handle)
                        m_UdpServerSocket.BeginReceive(Utility.Empty, 0, 0, SocketFlags.Partial, ProcessAccept, m_UdpServerSocket);

                    //The client socket is the server socket under Udp
                    clientSocket = server;
                }
                else if(server.ProtocolType == ProtocolType.Tcp) //Tcp
                {
                    //The clientSocket is obtained from the EndAccept call, possibly bytes ready from the accept
                    clientSocket = server.EndAccept(ar);
                }
                else
                {
                    throw new Exception("This server can only accept connections from Tcp or Udp sockets");
                }

                //Make a temporary client (Could move semantics about begin recieve to ClientSession)
                created = CreateOrObtainSession(clientSocket);
            }
            catch(Exception ex)//Using begin methods you want to hide this exception to ensure that the worker thread does not exit because of an exception at this level
            {
                //If there is a logger log the exception
                if (Logger != null)
                    Logger.LogException(ex);

                ////If a session was created dispose of it
                //if (created != null)
                //    created.Disconnect();
            }

        End:
            allDone.Set();

            created = null;

            //Thread exit 0
            return;
        }


        internal ClientSession CreateOrObtainSession(Socket rtspSocket)
        {

            //Handle the transient sockets which may come from clients which close theirs previoulsy, this should only occur for the first request or a request after play.
            //In UDP all connections are such

            //Iterate clients looking for the socket handle
            foreach (ClientSession cs in Clients.ToArray())
            {
                //If there is already a socket then use that one
                if (cs.RemoteEndPoint == rtspSocket.RemoteEndPoint)
                {
                    return cs;
                }
            }

            //Create a new session with the new socket, there may be an existing session from another port or address at this point....
            //So long as that session does not attempt to access resources in another session given by 'sessionId' of that session then everything should be okay.
            ClientSession session = new ClientSession(this, rtspSocket);

            //Add the session
            AddSession(session);

            //Return a new client session
            return session;
        }

        /// <summary>
        /// Handles the recieving of sockets data from a ClientSession's RtspSocket
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessReceive(IAsyncResult ar)
        {

            if (ar == null || !ar.IsCompleted) return;

            //Get the client information from the result
            ClientSession session = (ClientSession)ar.AsyncState;

            //If there is no session return
            if (session == null) return;

            //Take note of whre we are receiving from
            EndPoint inBound = session.RemoteEndPoint;

            try
            {
                //The request received
                RtspMessage request = null;

                //Declare how much was recieved
                int received = session.m_RtspSocket.EndReceiveFrom(ar, ref inBound);

                //If we received anything
                if (received >= RtpClient.InterleavedOverhead)
                {
                    //Count for the server
                    Interlocked.Add(ref m_Recieved, received);

                    //Count for the client
                    Interlocked.Add(ref session.m_Receieved, received);

                    using (Common.MemorySegment data = new Common.MemorySegment(session.m_Buffer.Array, session.m_Buffer.Offset, received))
                    {
                        //Ensure the message is really Rtsp
                        using (request = new RtspMessage(data))
                        {
                            //Check for validity
                            if (request.MessageType != RtspMessageType.Invalid)
                            {
                                //Process the request
                                ProcessRtspRequest(request, session);
                            }
                        }
                    }
                }
                else//Start to recive again
                {
                    EndPoint inbound = session.RemoteEndPoint;
                    session.LastRecieve = session.m_RtspSocket.BeginReceiveFrom(session.m_Buffer.Array, session.m_Buffer.Offset, session.m_Buffer.Count, SocketFlags.None, ref inbound, new AsyncCallback(ProcessReceive), session);
                }
            }
            catch (Exception ex)
            {
                //Something happened during the session
                if (Logger != null) Logger.LogException(ex);
            }
        }

        /// <summary>
        /// Handles the sending of responses to clients which made requests
        /// </summary>
        /// <param name="ar">The asynch result</param>
        internal void ProcessSend(IAsyncResult ar)
        {
            if (ar == null || !ar.IsCompleted) return;
            try
            {
                ClientSession session = (ClientSession)ar.AsyncState;

                if (session == null) return;

                EndPoint remote = session.RemoteEndPoint;

                //Ensure the bytes were completely sent..
                int sent = session.m_RtspSocket.EndSendTo(ar);

                int neededLength = session.m_SendBuffer.Length;

                //This logic IS NOT be correct when only a partial send occurs
                if (sent >= neededLength)
                {
                    Interlocked.Add(ref session.m_Sent, sent);

                    Interlocked.Add(ref m_Sent, sent);

                    session.LastRecieve = session.m_RtspSocket.BeginReceiveFrom(session.m_Buffer.Array, session.m_Buffer.Offset, session.m_Buffer.Count, SocketFlags.None, ref remote, new AsyncCallback(ProcessReceive), session);
                }
                else
                {                   
                    session.LastSend = session.m_RtspSocket.BeginSendTo(session.m_SendBuffer, sent, neededLength - sent, SocketFlags.None, remote, new AsyncCallback(ProcessSend), session);
                }
            }
            catch (Exception ex)
            {
                if (Logger != null) Logger.LogException(ex);                    
            }
        }

        #endregion

        #region Rtsp Request Handling Methods

        /// <summary>
        /// Processes a RtspRequest based on the contents
        /// </summary>
        /// <param name="request">The rtsp Request</param>
        /// <param name="session">The client information</param>
        internal void ProcessRtspRequest(RtspMessage request, ClientSession session)
        {
            try
            {
                //Ensure we have a session and request
                if (request == null || session == null)
                {
                    //We can't identify the request or session
                    return;
                }

                //All requests need the CSeq
                if (!request.ContainsHeader(RtspHeaders.CSeq))
                {
                    ProcessInvalidRtspRequest(session);
                    return;
                }
                else if (session.LastRequest != null)
                {
                    //Duplicate Request
                    if (request.CSeq == session.LastRequest.CSeq)
                    {
                        ProcessSendRtspResponse(null, session);
                        return;
                    }
                    else if (request.CSeq < session.LastRequest.CSeq)
                    {
                        ProcessInvalidRtspRequest(session);
                        return;
                    }
                }

                //Synchronize the server and client since this is not a duplicate
                session.LastRequest = request;

                //Determine if we support what the client requests in `Require` Header
                if (request.ContainsHeader(RtspHeaders.Require))
                {
                    //Certain features are requried... tcp etc.
                    //Todo ProcessRequired(
                }

                //If there is a body and no content-length
                if (!string.IsNullOrWhiteSpace(request.Body) && !request.ContainsHeader(RtspHeaders.ContentLength))
                {
                    ProcessInvalidRtspRequest(session);
                    return;
                }

                //Optional Checks

                //UserAgent
                if (RequireUserAgent && !request.ContainsHeader(RtspHeaders.UserAgent))
                {
                    ProcessInvalidRtspRequest(session);
                    return;
                }

                //Minor version reflects changes made to the protocol but not the 'general message parsing' `algorithm`

                //Thus, RTSP/2.4 is a lower version than RTSP/2.13, which in turn is lower than RTSP/12.3.
               //Leading zeros SHALL NOT be sent and MUST be ignored by recipients.

                //Version
                if (request.Version > Version)
                {

                    //ConvertToMessage

                    ProcessInvalidRtspRequest(session, RtspStatusCode.RtspVersionNotSupported);
                    return;
                }

                //Log the reqeust now
                if (Logger != null) Logger.LogRequest(request, session);

                //Determine the handler for the request and process it
                switch (request.Method)
                {
                    case RtspMethod.ANNOUNCE:
                        {
                            ProcessInvalidRtspRequest(session, RtspStatusCode.MethodNotAllowed);
                            break;
                        }
                    case RtspMethod.OPTIONS:
                        {
                            ProcessRtspOptions(request, session);
                            //Check for pipline?
                            break;
                        }
                    case RtspMethod.DESCRIBE:
                        {
                            ProcessRtspDescribe(request, session);
                            //Check for pipline?
                            break;
                        }
                    case RtspMethod.SETUP:
                        {
                            ProcessRtspSetup(request, session);
                            //Check for pipline?
                            break;
                        }
                    case RtspMethod.PLAY:
                        {
                            ProcessRtspPlay(request, session);
                            //Check for previously pipelined
                            break;
                        }
                    case RtspMethod.RECORD:
                        {
                            ProcessRtspRecord(request, session);
                            break;
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
                    case RtspMethod.SET_PARAMETER:
                        {
                            ProcessSetParameter(request, session);
                            break;
                        }
                    // Redirect should only send not received
                    //case RtspMethod.REDIRECT:
                    //    {
                    //        ProcessRedirect(request, session);
                    //        break;
                    //    }
                    case RtspMethod.UNKNOWN:
                    default:
                        {
                            //Per 2.0 Draft
                            ProcessInvalidRtspRequest(session, RtspStatusCode.NotImplemented);                                                        
                            break;
                        }
                }
            }
            catch(Exception ex)
            {
                //Log it
                if (Logger != null) Logger.LogException(ex);
            }
        }

        private void ProcessRtspRecord(RtspMessage request, ClientSession session)
        {
            var found = FindStreamByLocation(request.Location);

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }

            if (!found.Ready)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.DataTransportNotReadyYet);
                return;
            }
            
            if (Archiver == null)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }
            
            using (var resp = session.ProcessRecord(request, found))
            {
                ProcessSendRtspResponse(resp, session);
            }
        }

        internal void ProcessRedirect(RtspMessage request, ClientSession session)
        {
            var found = FindStreamByLocation(request.Location);

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }

            if (!found.Ready)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.DataTransportNotReadyYet);
                return;
            }
            
            using (var resp = session.CreateRtspResponse(request))
            {
                resp.Method = RtspMethod.REDIRECT;
                resp.AppendOrSetHeader(RtspHeaders.Location, "rtsp://" + session.LocalEndPoint.Address.ToString() + "/live/" + found.Id);
                ProcessSendRtspResponse(resp, session);
            }
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="response">The RtspResponse to send</param> If this was byte[] then it could handle http
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessSendRtspResponse(RtspMessage response, ClientSession session)
        {
            //Check Require Header
            //       And
            /* Add Unsupported Header if needed
            Require: play.basic, con.persistent
                       (basic play, TCP is supported)
            setup.playing means that setup and teardown can be used in the play state.
            */
            try
            {
                //If we have a session
                if (session != null)
                {
                    if (response != null)
                    {
                        //AddServerHeaders()->

                        if (!response.ContainsHeader(RtspHeaders.Server)) response.SetHeader(RtspHeaders.Server, ServerName);

                        //if (!response.ContainsHeader(RtspHeaders.Date)) response.SetHeader(RtspHeaders.Date, ...);

                        string sess = response.GetHeader(RtspHeaders.Session);

                        //Not closing
                        if (!response.ContainsHeader(RtspHeaders.Connection) &&//Check for if timeout needs to be added
                            RtspClientInactivityTimeoutSeconds > 0 && !string.IsNullOrWhiteSpace(sess) && !sess.Contains("timeout")) response.AppendOrSetHeader(RtspHeaders.Session, "timeout=" + RtspClientInactivityTimeoutSeconds);


                        //Log response
                        if (Logger != null) Logger.LogResponse(response, session);

                        session.SendRtspData((session.LastResponse = response).ToBytes());
                    }
                    else
                    {
                        session.SendRtspData(Utility.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger != null) Logger.LogException(ex);                
            }            
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="ci">The client session to send the response on</param>
        /// <param name="code">The status code of the response if other than BadRequest</param>
        //Should allow a header to be put into the response or a KeyValuePair<string,string> headers
        internal void ProcessInvalidRtspRequest(ClientSession session, RtspStatusCode code = RtspStatusCode.BadRequest)
        {
            //Create and Send the response
            ProcessInvalidRtspRequest(session != null ? session.CreateRtspResponse(null, code) : new RtspMessage(RtspMessageType.Response) { StatusCode = code }, session);
        }

        internal void ProcessInvalidRtspRequest(RtspMessage response, ClientSession session) { ProcessSendRtspResponse(response, session); }

        /// <summary>
        /// Sends a Rtsp LocationNotFound Response
        /// </summary>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessLocationNotFoundRtspRequest(ClientSession ci)
        {
            ProcessInvalidRtspRequest(ci, RtspStatusCode.NotFound);
        }

        internal virtual void ProcessAuthorizationRequired(IMediaStream source, ClientSession session)
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

                //Should store the nonce and cnonce values on the session
                statusCode = RtspStatusCode.Unauthorized;

                NetworkCredential requiredCredential = null;

                string authenticateHeader = null;

                Uri relativeLocation = source.ServerLocation;

                //Check for Digest first - Todo Finish implementation
                if ((requiredCredential = RequiredCredentials.GetCredential(relativeLocation, "Digest")) != null)
                {
                    //Might need to store values qop nc, cnonce and nonce in session storage for later retrival
                    authenticateHeader = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username={0},realm={1},nonce={2},cnonce={3}", requiredCredential.UserName, (string.IsNullOrWhiteSpace(requiredCredential.Domain) ? ServerName : requiredCredential.Domain), ((long)(Utility.Random.Next(int.MaxValue) << 32 | (Utility.Random.Next(int.MaxValue)))).ToString("X"), Utility.Random.Next(int.MaxValue).ToString("X"));
                }
                else if ((requiredCredential = RequiredCredentials.GetCredential(relativeLocation, "Basic")) != null)
                {
                    authenticateHeader = "Basic realm=\"" + (string.IsNullOrWhiteSpace(requiredCredential.Domain) ? ServerName : requiredCredential.Domain + '"');
                }

                if (!string.IsNullOrWhiteSpace(authenticateHeader))
                {
                    response.SetHeader(RtspHeaders.WWWAuthenticate, authenticateHeader);
                }
            }
            else //Authorization header was present but data was incorrect
            {
                //Parse type from authHeader

                string[] parts = authHeader.Split(' ');

                string authType = null;

                if (parts.Length > 0)
                {
                    authType = parts[0];
                    authHeader = parts[1];
                }

                //should check to ensure wrong type was not used e.g. basic in place of digest...
                if (string.Compare(authType, "digest", true) == 0)
                {
                    //if (session.Storage["nOnce"] != null)
                    //{
                    //    //Increment NonceCount
                    //}
                }

                //Increment session attempts?

                statusCode = RtspStatusCode.Forbidden;
            }
            
            //Set the status code
            response.StatusCode = statusCode;

            //Send the response
            ProcessInvalidRtspRequest(response, session);
        }

        /// <summary>
        /// Provides the Options this server supports
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspOptions(RtspMessage request, ClientSession session)
        {
            //See if the location requires a certain stream
            if (request.Location.ToString() != "*")
            {
                IMediaStream found = FindStreamByLocation(request.Location);

                //No stream with name
                if (found == null)
                {
                    ProcessLocationNotFoundRtspRequest(session);
                    return;
                }

                //See if RECORD is supported?

            }

            //Check for additional options of the stream... e.g. allow recording or not

            RtspMessage resp = session.CreateRtspResponse(request);
            
            resp.SetHeader(RtspHeaders.Public, "OPTIONS, DESCRIBE, SETUP, PLAY, TEARDOWN, GET_PARAMETER"); //RECORD etc

            //Should allow server to have certain options removed from this result
            //ClientSession.ProcessOptions

            ProcessSendRtspResponse(resp, session);
        }

        /// <summary>
        /// Decribes the requested stream
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspDescribe(RtspMessage request, ClientSession session)
        {
            string acceptHeader = request[RtspHeaders.Accept];

            if (string.IsNullOrWhiteSpace(acceptHeader) || 
                string.Compare(acceptHeader, Sdp.SessionDescription.MimeType, true,  System.Globalization.CultureInfo.InvariantCulture) > 1)
            {
                ProcessInvalidRtspRequest(session);
                return;
            }

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }

            if (!found.Ready)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.DataTransportNotReadyYet);
                return;
            }

            ProcessSendRtspResponse(session.ProcessDescribe(request, found), session);
        }

        internal void ProcessRtspInterleaveData(object sender, byte[] data, int offset, int length)
        {

            ClientSession sessionFrom = sender as ClientSession;

            if(sessionFrom == null) 

            try
            {
                RtspMessage created = new RtspMessage(data, offset, length);

                if (created.MessageType != RtspMessageType.Invalid)
                {
                    //Persistently suspicious behavior: 2326 p 74
                    ClientSession referredTo = GetSession(created.GetHeader(RtspHeaders.Session));
                    
                    //Check the the session being reffered to comes from the session it was received from...
                    //if (sessionFrom != referredTo) 
                    //{
                    //    ProcessInvalidRtspRequest(sessionFrom, RtspStatusCode.Forbidden);
                    //    return;
                    //}
                    
                    //All should be well....
                    ProcessRtspRequest(created, referredTo);
                }
            }
            catch(Exception ex)
            {
                if (Logger != null)
                    Logger.LogException(ex);
            }
        }     

        /// <summary>
        /// Sets the given session up, TODO Make functions which help with creation of TransportContext and Initialization
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspSetup(RtspMessage request, ClientSession session)
        {

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (found == null)
            {                
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }            
            else if (!found.Ready)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }
            else if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }
            
            //The source is ready

            //Determine if we have the track
            string track = request.Location.Segments.Last().Replace("/", string.Empty);

            Sdp.MediaDescription mediaDescription = found.SessionDescription.MediaDescriptions.FirstOrDefault(md=> string.Compare(track, md.MediaType.ToString(), true, System.Globalization.CultureInfo.InvariantCulture) == 0);

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
            if (mediaDescription == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            //Add the state information for the source
            RtpClient.TransportContext sourceContext = found.RtpClient.GetContextForMediaDescription(mediaDescription);

            //If the source has no TransportContext for that format or the source has not recieved a packet yet
            if (sourceContext == null || sourceContext.SynchronizationSourceIdentifier == 0)
            {
                //Stream is not yet ready
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }
           
            //Create the response and initialize the sockets if required
            RtspMessage resp = session.ProcessSetup(request, found, sourceContext);

            //Send the response
            ProcessSendRtspResponse(resp, session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspPlay(RtspMessage request, ClientSession session)
        {
            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }                
            else if (!found.Ready)
            {
                //Stream is not yet ready
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }

            RtspMessage resp = session.ProcessPlay(request, found);

            //Send the response to the client
            ProcessSendRtspResponse(resp, session);

            session.m_RtpClient.m_WorkerThread.Priority = ThreadPriority.Highest;

            session.ProcessPacketBuffer(found);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspPause(RtspMessage request, ClientSession session)
        {

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }

            session.m_RtpClient.m_WorkerThread.Priority = ThreadPriority.BelowNormal;

            //Might need to add some headers
            ProcessSendRtspResponse(session.ProcessPause(request, found), session);
        }

        /// <summary>
        /// Ends the client session
        /// </summary>
        /// <param name="request">The Teardown request</param>
        /// <param name="session">The session which recieved the request</param>
        internal void ProcessRtspTeardown(RtspMessage request, ClientSession session)
        {

            RtpSource found = FindStreamByLocation(request.Location) as RtpSource;

            if (found == null)
            {
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }

            if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }

            //Send the response
            using (var resp = session.ProcessTeardown(request, found))
            {
                resp.AppendOrSetHeader(RtspHeaders.Connection, "close");
                ProcessSendRtspResponse(resp, session);
            }
        }

        /// <summary>
        /// Handles the GET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessGetParameter(RtspMessage request, ClientSession session)
        {
            //We should process the body and return the parameters requested
            ProcessSendRtspResponse(session.CreateRtspResponse(request), session);
        }

        /// <summary>
        /// Handles the SET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessSetParameter(RtspMessage request, ClientSession session)
        {
            //Could be used for PTZ or other stuff
            //Should have a way to determine to forward send parameters... public bool ForwardSetParameter { get; set; }
            //Should have a way to call SendSetParamter on the RtspSession.Listener
            ProcessSendRtspResponse(session.CreateRtspResponse(request), session);
        }        

        /// <summary>
        /// Authenticates a RtspRequest against a RtspStream
        /// </summary>
        /// <param name="request">The RtspRequest to authenticate</param>
        /// <param name="source">The RtspStream to authenticate against</param>
        /// <returns>True if authroized, otherwise false</returns>
        public virtual bool AuthenticateRequest(RtspMessage request, IMediaStream source)
        {

            if (request == null) throw new ArgumentNullException("request");
            if (source == null) throw new ArgumentNullException("source");

            //There are no rules to validate
            if (RequiredCredentials == null) return true;

            string authHeader = request.GetHeader(RtspHeaders.Authorization);

            string[] parts = null;

            string authType = null;

            NetworkCredential requiredCredential = null;

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                authType = "basic";
                requiredCredential = RequiredCredentials.GetCredential(source.ServerLocation, authType);
                if (requiredCredential == null) requiredCredential = RequiredCredentials.GetCredential(source.ServerLocation, authType = "digest");
            }
            else
            {
                parts = authHeader.Split(' ');
                if (parts.Length > 0)
                { 
                    authType = parts[0];
                    authHeader = parts[1];
                }
                requiredCredential = RequiredCredentials.GetCredential(source.ServerLocation, authType);
            }

            //If there is no rule to validate
            if (requiredCredential == null) return true;
            else
            {
                //Verify against credential

                //If the request does not have the authorization header then there is nothing else to determine
                if (string.IsNullOrWhiteSpace(authHeader)) return false;

                //Wouldn't have to have a RemoteAuthenticationScheme if we stored the Nonce and CNonce on the session... then allowed either or here based on the header

                //If the SourceAuthenticationScheme is Basic and the header contains the BASIC indication then validiate using BASIC authentication
                if (string.Compare(authType, "basic", true) == 0)
                {

                    //Get the decoded value
                    authHeader = request.Encoding.GetString(Convert.FromBase64String(authHeader));

                    //Get the parts
                    parts = authHeader.Split(':');

                    //If enough return the determination by comparison as the result
                    return parts.Length > 1 && (parts[0].Equals(requiredCredential.UserName) && parts[1].Equals(requiredCredential.Password));
                }
                else if (string.Compare(authType, "digest", true) == 0)
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

                    parts = authHeader.Split(',');

                    string username, realm, nonce, nc, cnonce, uri, qop, opaque, response;

                    username = parts.Where(p => p.StartsWith("username")).FirstOrDefault();

                    realm = parts.Where(p => p.StartsWith("realm")).FirstOrDefault();

                    nc = parts.Where(p => p.StartsWith("nc")).FirstOrDefault();

                    nonce = parts.Where(p => p.StartsWith("nonce")).FirstOrDefault();

                    if (nonce == null) nonce = string.Empty;

                    cnonce = parts.Where(p => p.StartsWith("cnonce")).FirstOrDefault();

                    if (cnonce == null) cnonce = string.Empty;

                    uri = parts.Where(p => p.StartsWith("uri")).FirstOrDefault();

                    qop = parts.Where(p => p.StartsWith("qop")).FirstOrDefault();

                    if (qop == null) qop = string.Empty;

                    opaque = parts.Where(p => p.StartsWith("opaque")).FirstOrDefault();

                    if (opaque == null) opaque = string.Empty;

                    response = parts.Where(p => p.StartsWith("response")).FirstOrDefault();

                    if (string.IsNullOrEmpty(username) || username != requiredCredential.UserName || string.IsNullOrWhiteSpace(realm) || string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(response)) return false;

                    //http://en.wikipedia.org/wiki/Digest_access_authentication
                    //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                    byte[] HA1 = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", requiredCredential.UserName, realm.Replace("realm=", string.Empty), requiredCredential.Password)));

                    //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                    byte[] HA2 = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", request.Method, uri.Replace("uri=", string.Empty))));

                    //No QOP No NC
                    //See http://en.wikipedia.org/wiki/Digest_access_authentication
                    //http://tools.ietf.org/html/rfc2617

                    //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                    byte[] ResponseHash = Utility.MD5HashAlgorithm.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", Convert.ToString(HA1).Replace("-", string.Empty), nonce.Replace("nonce=", string.Empty), nc.Replace("nc=", string.Empty), cnonce.Replace("cnonce=", string.Empty), qop.Replace("qop=", string.Empty), Convert.ToString(HA2).Replace("-", string.Empty))));

                    //return the result of a mutal hash creation via comparison
                    return ResponseHash.SequenceEqual(Utility.HexStringToBytes(response.Replace("response=", string.Empty)));
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
    }
}
