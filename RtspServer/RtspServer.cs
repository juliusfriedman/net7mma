﻿/*
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
        int m_ServerPort = 554;
            //Counters for bytes sent and recieved
        long m_Recieved, m_Sent;

        IPAddress m_ServerIP;

        /// <summary>
        /// The socket used for recieving RtspRequests
        /// </summary>
        Socket m_TcpServerSocket, m_UdpServerSocket;

        /// <summary>
        /// The maximum number of clients allowed to be connected at one time.
        /// </summary>
        int m_MaximumClients = short.MaxValue; //32767

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
        Dictionary<Guid, IMedia> m_MediaStreams = new Dictionary<Guid, IMedia>();

        /// <summary>
        /// The dictionary containing all the clients the server has sessions assocaited with
        /// </summary>
        Dictionary<Guid, ClientSession> m_Clients = new Dictionary<Guid, ClientSession>(short.MaxValue);

        public Media.Rtsp.Server.RtspStreamArchiver Archiver { get; set; }

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

        internal IEnumerable<ClientSession> Clients
        {
            get
            {
                try
                {
                    return m_Clients.Values.ToList();
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
        public TimeSpan RtspClientInactivityTimeout { get; set; }

        //Check access on these values, may not need to make them available public

        /// <summary>
        /// Gets or sets the ReceiveTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int ReceiveTimeout { get { return m_TcpServerSocket.ReceiveTimeout; } set { m_TcpServerSocket.ReceiveTimeout = value; } }

        /// <summary>
        /// Gets or sets the SendTimeout of the TcpSocket used by the RtspServer
        /// </summary>
        public int SendTimeout { get { return m_TcpServerSocket.SendTimeout; } set { m_TcpServerSocket.SendTimeout = value; } }

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
        /// Indicates if the RtspServer has a worker process which is listening for requests on the ServerPort.
        /// </summary>
        /// <notes>m_TcpServerSocket.IsBound could also be another check or property.</notes>
        public bool IsRunning { get { return m_ServerThread != null && m_ServerThread.IsAlive; } }

        /// <summary>
        /// The port in which the RtspServer is listening for requests
        /// </summary>
        public int ServerPort { get { return m_ServerPort; } }

        /// <summary>
        /// The local endpoint for this RtspServer (The endpoint on which requests are recieved)
        /// </summary>
        public IPEndPoint LocalEndPoint { get { return m_TcpServerSocket.LocalEndPoint as IPEndPoint; } }

        //Todo make sure name is correct.

        /// <summary>
        /// The streams contained in the server
        /// </summary>
        public IEnumerable<IMedia> MediaStreams
        {
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
        public int TotalStreamCount { get { return m_MediaStreams.Count; } }

        /// <summary>
        /// The amount of active streams the server is listening to
        /// </summary>
        public int ActiveStreamCount
        {
            get
            {                
                if (TotalStreamCount == 0) return 0;
                return MediaStreams.Where(s => s.State == SourceMedia.StreamState.Stopped && s.Ready == true).Count();
            }
        }
        
        /// <summary>
        /// The total amount of bytes the RtspServer recieved from remote RtspRequests
        /// </summary>
        public long TotalRtspBytesRecieved { get { return m_Recieved; } }

        /// <summary>
        /// The total amount of bytes the RtspServer sent in response to remote RtspRequests
        /// </summary>
        public long TotalRtspBytesSent { get { return m_Sent; } }

        /// <summary>
        /// The amount of bytes sent or recieved from all contained streams in the RtspServer (Might want to log the counters seperately so the totals are not lost with the streams or just not provide the property)
        /// </summary>
        public long TotalStreamedBytes
        {
            get
            {
                //Right now this counter only indicate rtp/rtcp traffic, rtsp or other protocol level counters are excluded.
                //This also excludes http sources right now :(
                //This will eventually be different and subsequently easier to determine. (For instance if you wanted only rtsp or rtmp or something else).
                //IMedia should expose this in some aspect.
                return MediaStreams.OfType<RtpSource>().Sum(s => s.RtpClient != null ? s.RtpClient.TotalBytesSent + s.RtpClient.TotalBytesReceieved : 0);
            }
        }

        public int ConnectedClients { get { return m_Clients.Count; } }

        public Rtsp.Server.RtspServerLogger Logger { get; set; }

        public bool HttpEnabled { get { return m_HttpPort != -1; } }

        public bool UdpEnabled { get { return m_UdpPort != -1; } }

        #endregion

        #region Delegates

        /// <summary>
        /// A function which creates a RtspResponse.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="response">The response created, (Can be null and nothing will be sent)</param>
        /// <returns>True if NO futher processing is required otherwise false.</returns>
        public delegate bool RtspRequestHandler(RtspMessage request, out RtspMessage response);

        internal Dictionary<RtspMethod, RtspRequestHandler> m_RequestHandlers = new Dictionary<RtspMethod, RtspRequestHandler>();

        public void AddRequestHandler(RtspMethod method, RtspRequestHandler handler) { try { m_RequestHandlers.Add(method, handler); } catch { throw; } }

        public bool RemoveRequestHandler(RtspMethod method) { return m_RequestHandlers.Remove(method); }

        #endregion

        //Provide no immediate benefit
        //#region Events

        ////RequestReceived
        ////ResponseSent

        //#endregion

        #region Constructor

        public RtspServer(AddressFamily addressFamily = AddressFamily.InterNetwork, int listenPort = DefaultPort)
            : this(new IPEndPoint(Utility.GetFirstIPAddress(addressFamily), listenPort)) { }

        public RtspServer(IPAddress listenAddress, int listenPort) 
            : this(new IPEndPoint(listenAddress, listenPort)) { }

        public RtspServer(IPEndPoint listenEndPoint)
        {
            //Handle this according to RFC
            RtspClientInactivityTimeout = TimeSpan.FromSeconds(60);
            ServerName = "ASTI Media Server RTSP/" + Version.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture);
            m_ServerEndPoint = listenEndPoint;
            m_ServerIP = listenEndPoint.Address;
            m_ServerPort = listenEndPoint.Port;
            RequiredCredentials = new CredentialCache();
        }

        #endregion

        #region Methods

        int m_HttpPort = -1;
        public void EnableHttpTransport(int port = 80) 
        {
            if (m_HttpListner == null)
            {
                try
                {
                    m_HttpListner = new HttpListener();
                    m_HttpPort = port;
                    m_HttpListner.Prefixes.Add("http://*:" + port + "/");
                    m_HttpListner.Start();
                    //m_HttpListner.BeginGetContext(new AsyncCallback(ProcessHttpRtspRequest), this);
                }
                catch (Exception ex)
                {
                    throw new RtspServerException("Error Enabling Http on Port '" + port + "' : " + ex.Message, ex);
                }
            }
        }

        
        /*
         2.0 Draft specifies that servers now respond with a 501 to any rtspu request, we can blame that on lack of interest...
         */

        //Impove this...
        int m_UdpPort = -1;

        //Should allow multiple endpoints for any type of service.

        public void EnableUnreliableTransport(int port = 555, AddressFamily addressFamily = AddressFamily.InterNetwork) 
        {
            if (m_UdpServerSocket == null)
            {
                try
                {
                    m_UdpPort = port;

                    EndPoint inBound;

                    m_UdpServerSocket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
                    m_UdpServerSocket.Bind(inBound = new IPEndPoint(Utility.GetFirstIPAddress(addressFamily), port));

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

        /// <summary>
        /// Stops the server and removes all streams
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            base.Dispose();
         
            Stop();

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

        #region Session Collection

        internal bool TryAddSession(ClientSession session)
        {
            Exception any = null;
            try
            {
                if (Logger != null) Logger.Log("Adding Client: " + session.Id);

                if (Common.Collections.DictionaryExtensions.TryAdd(m_Clients, session.Id, session, out any))
                {
                    session.m_Contained = this;
                }

                return session.m_Contained == this;
            }
            catch(Exception)
            {
                return false;
            }
            finally
            {
                if (any != null && Logger != null) Logger.LogException(any);
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
                if (false == session.IsDisposed)
                {
                    //If there is a logger indicate the session is disposing
                    if (Logger != null) Logger.Log("Disposing Client: " + session.Id + " @ " + DateTime.UtcNow);

                    //Dispose the session
                    session.Dispose();
                }

                //If the client was already removed indicate this in the logs
                if (false == Common.Collections.DictionaryExtensions.TryRemove(m_Clients, session.Id, out any) && Logger != null) Logger.Log("Client Already Removed(" + session.Id + ")");

                //Indicate success
                return true;
            }
            catch (Exception)
            {
                //Indicate if a failure occured
                return session == null || session.IsDisposed;
            }
            finally
            {
                if (any != null && Logger != null) Logger.LogException(any);
            }
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
        internal IEnumerable<ClientSession> GetSessions(string rtspSessionId)
        {
            //If there was no id then nothing can be returned
            if (string.IsNullOrWhiteSpace(rtspSessionId)) return Enumerable.Empty<ClientSession>();

            //Trim the id given to ensure no whitespace is present.
            rtspSessionId = rtspSessionId.Trim();

            //Return all clients which match the given id.
            return Clients.Where(c => !string.IsNullOrWhiteSpace(c.SessionId) && c.SessionId.Equals(rtspSessionId));
        }              

        #endregion

        #region Stream Collection

        /// <summary>
        /// Adds a stream to the server. If the server is already started then the stream will also be started
        /// </summary>
        /// <param name="location">The uri of the stream</param>
        public bool TryAddMedia(IMedia stream)
        {
            Exception any = null;
            try
            {
                //Try to add the stream to the dictionary of contained streams.
                bool result = Common.Collections.DictionaryExtensions.TryAdd(m_MediaStreams, stream.Id, stream, out any);

                if (result && IsRunning) stream.Start();

                return result;
            }
            catch
            {

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
                if (any != null && Logger != null) Logger.LogException(any);
            }
        }

        /// <summary>
        /// Indicates if the RtspServer contains the given streamId
        /// </summary>
        /// <param name="streamId">The id of the stream</param>
        /// <returns>True if the stream is contained, otherwise false</returns>
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
        public bool TryRemoveMedia(Guid streamId, bool stop = true)
        {
            Exception any = null;
            try
            {
                IMedia source = GetStream(streamId);

                if (source != null && stop) source.Stop();

                if (RequiredCredentials != null)
                {
                    RemoveCredential(source, "Basic");
                    RemoveCredential(source, "Digest");
                }

                return Common.Collections.DictionaryExtensions.TryRemove(m_MediaStreams, streamId, out any);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (any != null && Logger != null) Logger.LogException(any);
            }
        }

        public IMedia GetStream(Guid streamId)
        {
            IMedia result;
            m_MediaStreams.TryGetValue(streamId, out result);
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="mediaLocation"></param>
        /// <returns></returns>
        internal IMedia FindStreamByLocation(Uri mediaLocation)
        {
            IMedia found = null;

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
                foreach (IMedia stream in MediaStreams)
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
        public void AddCredential(IMedia source, NetworkCredential credential, string authType)
        {
            RequiredCredentials.Add(source.ServerLocation, authType, credential);
        }

        /// <summary>
        /// Removes a Credential from the CredentialCache of the RtspServer for the given SourceStream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="authType"></param>
        public void RemoveCredential(IMedia source, string authType)
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
            List<ClientSession> inactive = new List<ClientSession>();

            //Allow some small varaince
            DateTime maintenanceStarted = DateTime.UtcNow + Utility.InfiniteTimeSpan;

            //Iterate each connected client
            foreach (ClientSession session in Clients)
            {
                //If the session is not in the dictionary then it cannot be removed.
                if (session == null
                    ||  //Or if the session was created after maintenanceStarted
                    session.Created > maintenanceStarted
                    || //Or if the LastRequest was created after maintenanceStarted
                    session.LastRequest != null && session.LastRequest.Created > maintenanceStarted
                    || //Or if the LastResponse was created after maintenanceStarted
                    session.LastResponse != null && session.LastResponse.Created > maintenanceStarted)
                {
                    //Do not attempt to perform any disconnection of the session
                    continue;
                }

                //Remove any inactive source attachments.
                session.ReleaseUnusedResources();

                //Don't disconnect sessions which are playing media if activity can be determined by the transport (rtp or rtcp etc)
                if (session.Playing.Count > 0) continue;

                //If the session was disposed OR
                if (session.IsDisposed
                    ||//There was no last request
                    session.LastRequest == null
                    ||//OR there is a last request AND
                    (session.LastRequest != null
                    && //The last request was created longer ago than required to keep clients active
                    (maintenanceStarted - session.LastRequest.Created) > RtspClientInactivityTimeout)
                    ||//There was no last response
                    session.LastResponse == null
                    || //OR there is a last response AND
                    (session.LastResponse != null 
                    && //The last response was transferred longer ago than required to keep clients active
                    session.LastResponse.Transferred.HasValue 
                    && (maintenanceStarted - session.LastResponse.Transferred) > RtspClientInactivityTimeout))
                    
                {
                    TryDisposeAndRemoveSession(session);
                }
            }
        }


        /// <summary>
        /// Restarted streams which should be Listening but are not
        /// </summary>
        internal void RestartFaultedStreams(object state = null) { RestartFaultedStreams(); }
        internal void RestartFaultedStreams()
        {
            foreach (IMedia stream in MediaStreams.Where(s => s.State == RtspSource.StreamState.Started && s.Ready == false))
            {

                if(Logger != null) Logger.Log("Stopping Stream: " + stream.Name + " Id=" + stream.Id);

                //Ensure Stopped
                stream.Stop();

                if (IsRunning)
                {
                    if (Logger != null) Logger.Log("Starting Stream: " + stream.Name + " Id=" + stream.Id);

                    //try to start it again
                    stream.Start();
                }
                
            }
        }

        /// <summary>
        /// Starts the RtspServer and listens for requests.
        /// Starts all streams contained in the server
        /// </summary>
        public virtual void Start()
        {
            //Dont allow starting when disposed
            CheckDisposed();

            //If we already have a thread return
            if (IsRunning) return;

            //Indicate start was called
            if (Logger != null) Logger.Log("Server Started @ " + DateTime.UtcNow);

            //Create the server Socket
            m_TcpServerSocket = new Socket(m_ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //If multiple instances can bind then this should be set
            //m_TcpServerSocket.ExclusiveAddressUse = false;

            //Bind the server Socket to the server EndPoint
            m_TcpServerSocket.Bind(m_ServerEndPoint);

            //Set the backlog
            m_TcpServerSocket.Listen(MaximumClients);

            //Set the recieve timeout
            m_TcpServerSocket.ReceiveTimeout = DefaultReceiveTimeout;

            //Set the send timeout
            m_TcpServerSocket.SendTimeout = DefaultSendTimeout;

            //Create a thread to handle client connections
            m_ServerThread = new Thread(new ThreadStart(RecieveLoop));
            
            //Configure the thread
            m_ServerThread.Name = ServerName + "@" + m_ServerPort;
            m_ServerThread.TrySetApartmentState(ApartmentState.MTA);
            m_ServerThread.Priority = ThreadPriority.BelowNormal;

            //Start it
            m_ServerThread.Start();

            //Timer for maintaince
            m_Maintainer = new Timer(new TimerCallback(MaintainServer), null, TimeSpan.FromTicks(RtspClientInactivityTimeout.Ticks / 2), Utility.InfiniteTimeSpan);

            if (m_UdpPort != -1) EnableUnreliableTransport(m_UdpPort);
            if (m_HttpPort != -1) EnableHttpTransport(m_HttpPort);

            //Start streaming from m_Streams before server can accept connections.
            StartStreams();

            //Erase any prior stats
            m_Sent = m_Recieved = 0;

            //Indicate when start was finished.
            m_Started = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes Inactive Sessions and Restarts Faulted Streams
        /// </summary>
        /// <param name="state">Reserved</param>
        internal virtual void MaintainServer(object state = null)
        {
            if (m_Maintaining || IsDisposed || m_Maintainer == null || m_StopRequested) return;

            m_Maintaining = true;

            new Thread(new ThreadStart(() =>
            {

                try 
                {
                    Logger.Log("RestartFaultedStreams");

                    RestartFaultedStreams(state); 
                }
                catch (Exception ex)
                {
                    if (Logger != null) Logger.LogException(ex);
                }

                try
                {
                    Logger.Log("DisconnectAndRemoveInactiveSessions");

                    DisconnectAndRemoveInactiveSessions(state);
                }
                catch (Exception ex)
                {
                    if (Logger != null) Logger.LogException(ex);
                }

                m_Maintaining = false;

            }))
            {
                Priority = m_ServerThread.Priority
            }.Start();

            if (m_Maintainer != null) m_Maintainer.Change(TimeSpan.FromTicks(RtspClientInactivityTimeout.Ticks / 2), Utility.InfiniteTimeSpan);
        }

        /// <summary>
        /// Stops recieving RtspRequests and stops streaming all contained streams
        /// </summary>
        public virtual void Stop()
        {
            //If there is not a server thread return
            if (IsDisposed || m_StopRequested || !IsRunning) return;

            //Stop listening for new clients
            m_StopRequested = true;

            if (Logger != null)
            {
                Logger.Log("Connected Clients:" + ConnectedClients);
                Logger.Log("Server Stopped @ " + DateTime.UtcNow);
                Logger.Log("Uptime:" + Uptime);
                Logger.Log("Sent:" + m_Sent);
                Logger.Log("Receieved:" + m_Recieved);
            }

            //Stop other listeners
            DisableHttpTransport();

            DisableUnreliableTransport();

            //Stop maintaining the server
            if (m_Maintainer != null)
            {
                m_Maintainer.Dispose();
                m_Maintainer = null;
            }
            
            //Stop listening to source streams
            StopStreams();

            //Wait for the thread maintaining the server to complete (should store thread not m_Maintaining)
            while (m_Maintaining) System.Threading.Thread.Sleep(0);

            //Remove all clients
            foreach (ClientSession session in Clients)
            {
                //Remove the session
                TryDisposeAndRemoveSession(session);
            }

            //Abort the worker from receiving clients
            if (IsRunning) Utility.Abort(ref m_ServerThread);

            //Dispose the server socket
            if (m_TcpServerSocket != null)
            {
                //Dispose the socket
                m_TcpServerSocket.Dispose();
                m_TcpServerSocket = null;
            }
          
            //Erase statistics
            m_Started = null;            

            //Allow restart
            m_StopRequested = false;
        }

        /// <summary>
        /// Starts all streams contained in the video server
        /// </summary>
        internal virtual void StartStreams()
        {
            foreach (IMedia stream in MediaStreams)
            {
                if (m_StopRequested) return;

                if (stream == null || stream.Disabled) continue;

                try
                {
                    if (Logger != null) Logger.Log("Starting Stream:" + stream.Id);

                    new Thread(stream.Start)
                    {
                        Priority = m_ServerThread.Priority
                    }.Start();
                }
                catch(Exception ex)
                {
                    if (Logger != null) Logger.LogException(ex);
                    continue;
                }
            }
        }

        /// <summary>
        /// Stops all contained streams from streaming
        /// </summary>
        internal virtual void StopStreams()
        {
            foreach (IMedia stream in MediaStreams)
            {
                if (m_StopRequested) return;

                if (stream == null || stream.Disabled) continue;

                try
                {
                    if (Logger != null) Logger.Log("Stopping Stream:" + stream.Id);

                    new Thread(stream.Stop)
                    {
                        Priority = m_ServerThread.Priority
                    }.Start();
                }
                catch (Exception ex)
                {
                    if (Logger != null) Logger.LogException(ex);
                    continue;
                }
            }
        }        

        /// <summary>
        /// The loop where Rtsp Requests are recieved
        /// </summary>
        internal virtual void RecieveLoop()
        {
        Begin:
            try
            {
                //While running
                while (IsRunning)
                {
                    //If we can accept
                    if (m_StopRequested == false && m_Clients.Count < m_MaximumClients)
                    {
                        //Start acceping with a 0 size buffer
                        IAsyncResult iar = m_TcpServerSocket.BeginAccept(0, new AsyncCallback(ProcessAccept), m_TcpServerSocket);

                        //Get the timeout from the socket
                        int timeOut = m_TcpServerSocket.ReceiveTimeout;

                        //If the timeout is infinite only wait for the default
                        if (timeOut <= 0) timeOut = DefaultReceiveTimeout;

                        //The timeout is always half of the total.
                        timeOut /= 2;

                        //Check for nothing to do.
                        if (iar == null || iar.CompletedSynchronously) continue;

                        //use the handle to wait for the result which is obtained by calling EndAccept.
                        using (var handle = iar.AsyncWaitHandle)
                        {
                            //Wait half using the event
                            while (!m_StopRequested && !iar.IsCompleted && !iar.AsyncWaitHandle.WaitOne(timeOut))
                            {
                                //Wait the other half looking for the stop
                                if (m_StopRequested || iar.IsCompleted || !iar.AsyncWaitHandle.WaitOne(timeOut)) continue;

                                //Relinquish time slice
                                System.Threading.Thread.Sleep(0);

                                //Should ensure that not waiting more then a certain amount of time here
                            }
                        }
                    }

                    //Relinquish time slice
                    System.Threading.Thread.Sleep(0);
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
            catch(Exception ex)
            {
                if (Logger != null) Logger.LogException(ex);

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
            if (ar == null) return;
            
            //The ClientSession created
            try
            {
                //The Socket needed to create a ClientSession
                Socket clientSocket = null;

                //See if there is a socket in the state object
                Socket server = (Socket)ar.AsyncState;

                //If there is no socket then an accept has cannot be performed
                if (server == null) return;

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
                    //They are not discarded just not receieved until the first receive.
                    clientSocket = server.EndAccept(ar);
                }
                else
                {
                    throw new Exception("This server can only accept connections from Tcp or Udp sockets");
                }

                //there must be a client socket.
                if (clientSocket == null) throw new InvalidOperationException("clientSocket is null");

                //If the server is not runing dispose any connected socket
                if (false == IsRunning)
                {
                    //If there is a logger then indicate what is happening
                    if (Logger != null) Logger.Log("Accepted Socket while not running @ " + clientSocket.LocalEndPoint + " From: " + clientSocket.RemoteEndPoint + " Disposing.");

                    //Try to dispose the socket
                    try { clientSocket.Dispose(); }
                    catch { }

                    //The server is no longer running.
                    return;
                }

                //Make a session
                ClientSession session = CreateSession(clientSocket);

                //If there is a logger log the accept
                if (Logger != null) Logger.Log("Accepted Client: " + session.Id + " @ " + session.Created);
            }
            catch(Exception ex)//Using begin methods you want to hide this exception to ensure that the worker thread does not exit because of an exception at this level
            {
                //If there is a logger log the exception
                if (IsRunning && Logger != null) Logger.LogException(ex);
            }
        }


        internal ClientSession CreateSession(Socket rtspSocket)
        {
            //Return a new client session
            return new ClientSession(this, rtspSocket);
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
            if (session == null || session.IsDisposed || session.m_RtspSocket == null) return;

            //Take note of where we are receiving from
            EndPoint inBound = session.RemoteEndPoint;

            try
            {
                //Declare how much was recieved
                int received = session.m_RtspSocket.EndReceiveFrom(ar, ref inBound);

                //Let the client propagate the message
                if (session.m_RtpClient != null && session.m_RtpClient.IsConnected)
                {
                    received -= session.m_RtpClient.ProcessFrameData(session.m_Buffer.Array, session.m_Buffer.Offset, received, session.m_RtspSocket);

                    return;
                }

                //If we received anything
                if (received > 0)
                {
                    //The session is not disconnected 
                    session.IsDisconnected = false;

                    //Count for the server
                    m_Recieved += received;

                    //Count for the client
                    session.m_Receieved += received;

                    //Process the data in the buffer
                    ProcessClientBuffer(session, received);
                }
                else //Zero  bytes were received either due to consumption of data or lack thereof.
                {
                    //If there is a rtp client but it was disposed or disconnected
                    if (session.m_RtpClient == null || session.m_RtpClient.IsDisposed || !session.m_RtpClient.IsConnected)
                    {
                        //Mark the session as Disconnected
                        session.IsDisconnected = true;

                        //Try a send to ensure that there is no connection error, also begin another recieve.
                        ProcessSendRtspMessage(null, session);
                    }
                }
            }
            catch (Exception ex)
            {
                //Something happened during the session
                if (Logger != null) Logger.LogException(ex);

                //if a socket exception occured then handle it.
                if (session != null && ex is SocketException) HandleClientSocketException((SocketException)ex, session);                                     
            }
        }

        internal void ProcessClientBuffer(ClientSession session, int received)
        {
            if (session == null || received <= 0) return;

            try
            {
                //Use a segment around the data received which is already in the buffer.
                using (Common.MemorySegment data = new Common.MemorySegment(session.m_Buffer.Array, session.m_Buffer.Offset, received))
                {
                    //Ensure the message is really Rtsp
                    using (RtspMessage request = new RtspMessage(data))
                    {
                        //Check for validity
                        if (request.MessageType != RtspMessageType.Invalid)
                        {
                            //Process the request
                            ProcessRtspRequest(request, session);

                            return;
                        }

                        //Otherwise if LastRequest is not disposed then 
                        if (session.LastRequest != null && !session.LastRequest.IsDisposed) 
                        {
                            //Attempt to complete it
                            received = session.LastRequest.CompleteFrom(null, data);

                            //If nothing was recieved then do nothing.
                            if (received == 0) return;

                            //Account for the received data
                            session.m_Receieved += received;

                            //Determine how to process the messge

                            switch (session.LastRequest.MessageType)
                            {
                                case RtspMessageType.Response:
                                case RtspMessageType.Invalid:
                                    {
                                        //Ensure the session is still connected.
                                        session.SendRtspData(Utility.Empty);

                                        return;
                                    }
                                case RtspMessageType.Request:
                                    {
                                        //Wait the rest of the time slice waiting for more data
                                        System.Threading.Thread.Sleep(0);

                                        //Process the request (it may not be complete yet)
                                        ProcessRtspRequest(session.LastRequest, session);

                                        return;
                                    }
                                //case RtspMessageType.Response:
                                //    {
                                //        //A Response was received...
                                //        goto case RtspMessageType.Invalid;
                                //    }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                if (Logger != null) Logger.LogException(ex);
            }
        }

        internal void HandleClientSocketException(SocketException se, ClientSession cs)
        {
            if(se == null || cs == null || cs.IsDisposed) return;

            switch (se.SocketErrorCode)
            {
                case SocketError.TimedOut:
                    {

                        //Clients interleaving shouldn't time out ever
                        if (cs.m_RtspSocket.ProtocolType == ProtocolType.Tcp)
                        {
                            if (Logger != null) Logger.Log("Client:" + cs.Id + " Timeouts when interleaving");

                             if(cs.Playing.Count > 0) return;
                        }

                        //Increase send and receive timeout
                        cs.m_RtspSocket.SendTimeout = cs.m_RtspSocket.ReceiveTimeout += (int)(RtspClient.DefaultConnectionTime.TotalMilliseconds);

                        if (Logger != null) Logger.Log("Increased Client:" + cs.Id + " Timeouts to: " + cs.m_RtspSocket.SendTimeout);

                        //Try to receive again
                        cs.StartReceive();

                        return;
                    }
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.Disconnecting:
                case SocketError.Shutdown:
                case SocketError.NotConnected:
                    {
                        if (Logger != null) Logger.Log("Marking Client:" + cs.Id + " Disconnected");

                        //Mark the client as disconnected.
                        cs.IsDisconnected = true;
                        
                        return;
                    }
                default:
                    {
                        if (Logger != null) Logger.LogException(se);

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
            if (ar == null) return;

            ClientSession session = (ClientSession)ar.AsyncState;

            try
            {
                //Don't do anything if the session cannot be acted on.
                if (session == null || session.IsDisposed || session.IsDisconnected) return;

                //Ensure the bytes were completely sent..
                int sent = session.m_RtspSocket.EndSendTo(ar);

                //See how much was needed.
                int neededLength = session.m_SendBuffer.Length;

                //Determine if the send was complete
                if (sent >= neededLength)
                {

                    //Once completely sent increment for the bytes sent.
                    unchecked
                    {
                        session.m_Sent += sent;

                        m_Sent += sent;
                    }

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
                if (Logger != null) Logger.LogException(ex);


                //handle the socket exception
                if (session != null && ex is SocketException) HandleClientSocketException((SocketException)ex, session);
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
                //Log the reqeust now
                if (Logger != null) Logger.LogRequest(request, session);

                //Ensure we have a session and request and that the server is still running and that the session is still contained in this server
                if (request == null || request.IsDisposed || session == null || session.IsDisposed)
                {
                    return;
                }//Ensure the session is added to the connected clients if it has not been already.
                else if (session.m_Contained == null) TryAddSession(session);
                
                //Check for session moved from another process.
                //else if (session.m_Contained != null && session.m_Contained != this) return;

                //All requests need the CSeq
                if (!request.ContainsHeader(RtspHeaders.CSeq))
                {
                    ProcessInvalidRtspRequest(session);

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
                    string requestedSessionId = request.GetHeader(RtspHeaders.Session).Trim();

                    //If there is a null or empty session id this request is invalid.
                    if (string.IsNullOrWhiteSpace(requestedSessionId))
                    {
                        //Send back a BadRequest.
                        ProcessInvalidRtspRequest(session);

                        return;
                    }

                    //If the given session does not have a sessionId or does not match the sessionId requested.
                    if (session.SessionId != requestedSessionId)
                    {
                        //Find any session which has the given id.
                        IEnumerable<ClientSession> matches = GetSessions(requestedSessionId);

                        //Atttempt to get the correct session
                        ClientSession correctSession = matches.FirstOrDefault(c => !c.IsDisconnected);

                        //If no session could be found by the given sessionId
                        if (correctSession == null)
                        {
                            //If the session with the request doesn't have a sessionid assigned yet
                            if (string.IsNullOrWhiteSpace(session.SessionId))
                            {
                                //allow the assignment
                                session.SessionId = request.GetHeader(RtspHeaders.Session).Trim();
                            }
                            else
                            {
                                //Indicate the session requested could not be found.
                                ProcessInvalidRtspRequest(session, RtspStatusCode.SessionNotFound);

                                return;
                            }
                        }
                        else //There was a session found by the given Id
                        {
                            //Determine if another property would be better suited to convey this.

                            //Set the last response time 'transferred' to now to ensure the client is not disconnected.
                            if(correctSession.LastResponse != null) correctSession.LastResponse.Transferred = DateTime.UtcNow;

                            //The session is no longer disconnected
                            correctSession.IsDisconnected = false;

                            //Log the EndPoint change
                            if (Logger != null) Logger.Log("Session: " + correctSession.Id + " @ " + correctSession.RemoteEndPoint + " Reconnected From new EndPoint: " + session.RemoteEndPoint + " New SessionId:" + session.Id);

                            //update the value of session socket of the correction session.
                            correctSession.RtspSocket = session.RtspSocket;

                            //Leave the session's socket open if it becomes inactive because it is now used by the existing session.
                            correctSession.LeaveOpen = session.LeaveOpen = true;

                            //update the value of session to act on
                            session = correctSession;
                        }
                    }
                }
                
                //Check for out of order or duplicate requests.
                if (session.LastRequest != null)
                {
                    //Out of order
                    if (request.CSeq < session.LastRequest.CSeq)
                    {
                        ProcessInvalidRtspRequest(session);

                        return;
                    }
                }

                //Dispose any last request.
                if (session.LastRequest != null && !session.LastRequest.IsDisposed) session.LastRequest.Dispose();

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

                //Version - Should check request.Version != Version and that earlier versions are supprted.
                if (request.Version > Version)
                {
                    //ConvertToMessage

                    ProcessInvalidRtspRequest(session, RtspStatusCode.RtspVersionNotSupported);
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
                    if (m_RequestHandlers.TryGetValue(request.Method, out custom))
                    {
                        //Then create the response
                        RtspMessage response;

                        //By invoking the handler, if true is returned
                        if (custom(request, out response))
                        {
                            //Use the response created by the custom handler
                            ProcessSendRtspMessage(response, session);

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
                if (m_StopRequested && (request.Method != RtspMethod.TEARDOWN && request.Method != RtspMethod.GET_PARAMETER && request.Method != RtspMethod.OPTIONS))
                {
                    ProcessInvalidRtspRequest(session, RtspStatusCode.BadRequest);

                    return;
                }

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
                    case RtspMethod.REDIRECT: //Client can't redirect a server
                        {
                            ProcessInvalidRtspRequest(session, RtspStatusCode.BadRequest);
                            break;
                        }
                    case RtspMethod.UNKNOWN:
                    default:
                        {                            
                            //Per 2.0 Draft
                            ProcessInvalidRtspRequest(session, RtspStatusCode.NotImplemented);
                            break;
                        }
                }
            }
            catch (Exception ex)
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

            if (!found.Ready || Archiver == null)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }
            
            using (var resp = session.ProcessRecord(request, found))
            {
                ProcessSendRtspMessage(resp, session);
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
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }
            
            using (var resp = session.CreateRtspResponse(request))
            {
                resp.Method = RtspMethod.REDIRECT;
                resp.AppendOrSetHeader(RtspHeaders.Location, "rtsp://" + session.LocalEndPoint.Address.ToString() + "/live/" + found.Id);
                ProcessSendRtspMessage(resp, session);
            }
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="message">The RtspResponse to send</param> If this was byte[] then it could handle http
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessSendRtspMessage(RtspMessage message, ClientSession session)
        {
            //Check Require Header
            //       And
            /* Add Unsupported Header if needed
            Require: play.basic, con.persistent
                       (basic play, TCP is supported)
            setup.playing means that setup and teardown can be used in the play state.
            */

            //If we have a session
            if (session == null || session.IsDisposed) return;

            try
            {
                //if there is a message to send
                if (message != null && false == message.IsDisposed)
                {
                    //AddServerHeaders()->

                    if (!message.ContainsHeader(RtspHeaders.Server)) message.SetHeader(RtspHeaders.Server, ServerName);

                    if (!message.ContainsHeader(RtspHeaders.Date)) message.SetHeader(RtspHeaders.Date, DateTime.UtcNow.ToString("r"));

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

                    if (!message.ContainsHeader(RtspHeaders.Timestamp)
                        &&
                        session.LastRequest != null
                        &&
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

                    //Check for a session header
                    if (!string.IsNullOrWhiteSpace(sess))
                    {
                        //Add the timeout header if there was a session header.
                        if (RtspClientInactivityTimeout > TimeSpan.Zero && !sess.Contains("timeout")) message.AppendOrSetHeader(RtspHeaders.Session, "timeout=" + (int)(RtspClientInactivityTimeout.TotalSeconds / 2));
                    }

                    //Log response
                    if (Logger != null) Logger.LogResponse(message, session);

                    //Dispose the last response
                    if (session.LastResponse != null) session.LastResponse.Dispose();

                    //Oops
                    //if (session.m_RtspSocket.ProtocolType == ProtocolType.Tcp && session.Attached.Count > 0) response.SetHeader("Ignore", "$0\09\r\n$\0:\0");
                    session.SendRtspData((session.LastResponse = message).ToBytes());

                    //Indicate the session is not disconnected
                    session.IsDisconnected = false;

                    //Indicate the last response was sent now.
                    session.LastResponse.Transferred = DateTime.UtcNow;
                }
                else
                {
                    //Test the connectivity and start another receive
                    session.SendRtspData(Utility.Empty);
                }
            }
            catch (Exception ex)
            {
                if (Logger != null) Logger.LogException(ex);

                //if a socket exception occured then handle it.
                if (session != null && ex is SocketException) HandleClientSocketException((SocketException)ex, session);
            }            
        }

        /// <summary>
        /// Sends a Rtsp Response on the given client session
        /// </summary>
        /// <param name="ci">The client session to send the response on</param>
        /// <param name="code">The status code of the response if other than BadRequest</param>
        //Should allow a header to be put into the response or a KeyValuePair<string,string> headers
        internal void ProcessInvalidRtspRequest(ClientSession session, RtspStatusCode code = RtspStatusCode.BadRequest, string body = null)
        {
            //Create and Send the response
            ProcessInvalidRtspRequest(session != null ? session.CreateRtspResponse(null, code, body) : new RtspMessage(RtspMessageType.Response) { StatusCode = code, Body = body }, session);
        }

        internal void ProcessInvalidRtspRequest(RtspMessage response, ClientSession session) { ProcessSendRtspMessage(response, session); }

        /// <summary>
        /// Sends a Rtsp LocationNotFound Response
        /// </summary>
        /// <param name="ci">The session to send the response on</param>
        internal void ProcessLocationNotFoundRtspRequest(ClientSession ci)
        {
            ProcessInvalidRtspRequest(ci, RtspStatusCode.NotFound);
        }

        internal virtual void ProcessAuthorizationRequired(IMedia source, ClientSession session)
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

                Uri relativeLocation = source.ServerLocation;

                //Check for Digest first - Todo Finish implementation
                if ((requiredCredential = RequiredCredentials.GetCredential(relativeLocation, "Digest")) != null)
                {
                    //Might need to store values qop nc, cnonce and nonce in session storage for later retrival                    

                    //Should use auth-int and qop

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
            if (request.Location.ToString() != "*" && false == string.IsNullOrWhiteSpace(request.Location.LocalPath) && request.Location.LocalPath.Length > 1)
            {
                IMedia found = FindStreamByLocation(request.Location);

                //No stream with name
                if (found == null)
                {
                    ProcessLocationNotFoundRtspRequest(session);
                    return;
                }

                //See if RECORD is supported?

            }

            //Check for additional options of the stream... e.g. allow recording or not

            using (RtspMessage resp = session.CreateRtspResponse(request))
            {
                resp.SetHeader(RtspHeaders.Public, "OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, TEARDOWN, GET_PARAMETER");

                //Allow for Authorized
                resp.SetHeader(RtspHeaders.Allow, "ANNOUNCE, RECORD, SET_PARAMETER");

                //Add from handlers?
                //if(m_RequestHandlers.Count > 0) string.Join(" ", m_RequestHandlers.Keys.ToArray())

                //Should allow server to have certain options removed from this result
                //ClientSession.ProcessOptions

                ProcessSendRtspMessage(resp, session);
            }
        }

        /// <summary>
        /// Decribes the requested stream
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        internal void ProcessRtspDescribe(RtspMessage request, ClientSession session)
        {
            string acceptHeader = request[RtspHeaders.Accept];

            //If an Accept header was given it must reflect a content-type of SDP.
            if (!string.IsNullOrWhiteSpace(acceptHeader) 
                &&
                string.Compare(acceptHeader, Sdp.SessionDescription.MimeType, true,  System.Globalization.CultureInfo.InvariantCulture) > 0)
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
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
                return;
            }

            using (var resp = session.ProcessDescribe(request, found)) ProcessSendRtspMessage(resp, session);
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
                //This allows probing for streams even if not authenticated....
                ProcessLocationNotFoundRtspRequest(session);
                return;
            }            
            else if (!AuthenticateRequest(request, found))
            {
                ProcessAuthorizationRequired(found, session);
                return;
            }
            else if (!found.Ready)
            {
                ProcessInvalidRtspRequest(session, RtspStatusCode.PreconditionFailed);
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
            using (RtspMessage resp = session.ProcessSetup(request, found, sourceContext))
            {
                //Send the response
                ProcessSendRtspMessage(resp, session);
            }
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

            using (RtspMessage resp = session.ProcessPlay(request, found))
            {
                //Send the response to the client
                ProcessSendRtspMessage(resp, session);

                if (resp.StatusCode == RtspStatusCode.OK)
                {
                    //Take the range into account given.
                    session.ProcessPacketBuffer(found);
                }
            }
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

            //session.m_RtpClient.m_WorkerThread.Priority = ThreadPriority.BelowNormal;

            using (var resp = session.ProcessPause(request, found))
            {
                //Might need to add some headers
                ProcessSendRtspMessage(resp, session);
            }
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

                ProcessSendRtspMessage(resp, session);

                session.ReleaseUnusedResources();

                //Attempt to remove the sessionId when nothing is playing... why?
                //10.4 SETUP says we would have to bundle pipelined requests.
                //That is contradictory.

                //if (session.Playing.Count == 0) session.SessionId = null;
            }
        }

        /// <summary>
        /// Handles the GET_PARAMETER RtspRequest
        /// </summary>
        /// <param name="request">The GET_PARAMETER RtspRequest to handle</param>
        /// <param name="ci">The RtspSession from which the request was receieved</param>
        internal void ProcessGetParameter(RtspMessage request, ClientSession session)
        {
            //TODO Determine API
            //packets_sent
            //jitter
            //rtcp_interval
            using (var resp = session.CreateRtspResponse(request))
            {
                resp.SetHeader(RtspHeaders.Connection, "Keep-Alive");
                ProcessSendRtspMessage(resp, session);
            }
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
            //Should have a way to call SendSetParamter on the source if required.
            //Should allow sever parameters to be set?
            using (var resp = session.CreateRtspResponse(request))
            {
                //Content type
                ProcessSendRtspMessage(resp, session);
            }
        }        

        /// <summary>
        /// Authenticates a RtspRequest against a RtspStream
        /// </summary>
        /// <param name="request">The RtspRequest to authenticate</param>
        /// <param name="source">The RtspStream to authenticate against</param>
        /// <returns>True if authroized, otherwise false</returns>
        public virtual bool AuthenticateRequest(RtspMessage request, IMedia source)
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
                    //realm may be present? 
                    //Basic realm="''" dasfhadfhsaghf

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

                    using (var md5 = Utility.CreateMD5HashAlgorithm())
                    {
                        //http://en.wikipedia.org/wiki/Digest_access_authentication
                        //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                        byte[] HA1 = md5.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", requiredCredential.UserName, realm.Replace("realm=", string.Empty), requiredCredential.Password)));

                        //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                        byte[] HA2 = md5.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", request.Method, uri.Replace("uri=", string.Empty))));

                        //No QOP No NC
                        //See http://en.wikipedia.org/wiki/Digest_access_authentication
                        //http://tools.ietf.org/html/rfc2617

                        //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                        byte[] ResponseHash = md5.ComputeHash(request.Encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", Convert.ToString(HA1).Replace("-", string.Empty), nonce.Replace("nonce=", string.Empty), nc.Replace("nc=", string.Empty), cnonce.Replace("cnonce=", string.Empty), qop.Replace("qop=", string.Empty), Convert.ToString(HA2).Replace("-", string.Empty))));

                        //return the result of a mutal hash creation via comparison
                        return ResponseHash.SequenceEqual(Utility.HexStringToBytes(response.Replace("response=", string.Empty)));
                    }
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
    
        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            yield return m_TcpServerSocket;

            if (m_UdpServerSocket != null) yield return m_UdpServerSocket;

            //Get socket using reflection?
            //if (m_HttpListner != null) yield return m_HttpListner.;
        }

        IEnumerable<Thread> Common.IThreadReference.GetReferencedThreads()
        {
             return Utility.Yield(m_ServerThread);
        }
    }
}
