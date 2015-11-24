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
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Media.Rtp;
using Media.Rtcp;
using Media.Rtsp.Server.MediaTypes;
using System.Threading;

namespace Media.Rtsp//.Server
{
    /// <summary>
    /// Represent the resources in use by remote parties connected to a RtspServer.
    /// </summary>
    internal class ClientSession : Common.BaseDisposable
    {
        //Needs to have it's own concept of range using the Storage...

        //Store authentication related values here also.

        #region Fields


        //The time the session was created.
        public readonly DateTime Created = DateTime.UtcNow;

        //Session storage
        //Counters for authenticate and attempts should use static key names, maybe use a dictionary..
        internal System.Collections.Hashtable Storage = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());

        //Keep track of the last send or receive when using Async
        internal IAsyncResult LastRecieve, LastSend;

        /// <summary>
        /// The RtpClient.TransportContext instances which provide valid data to this ClientSession.
        /// </summary>
        //internal HashSet<RtpClient.TransportContext> SourceContexts = new HashSet<RtpClient.TransportContext>();

        /// <summary>
        /// A HashSet of SourceStreams attached to the ClientSession which provide the events for Rtp, Rtcp, and Interleaved data.
        /// Instances in this collection are raising events which are being handled in the OnSourcePacketRecieved Method
        /// </summary>
        //internal HashSet<SourceStream> AttachedSources = new HashSet<SourceStream>();

        internal readonly Dictionary<RtpClient.TransportContext, Media.Rtsp.Server.SourceMedia> Attached = new Dictionary<RtpClient.TransportContext, Media.Rtsp.Server.SourceMedia>();

        internal readonly HashSet<Guid> Playing = new HashSet<Guid>();

        /// <summary>
        /// A one to many collection which is keyed by the source media's SSRC to which subsequently the values are packets which also came from the source
        /// Should be a Guid and be the Id of the Media.
        /// </summary>
        internal Common.Collections.Generic.ConcurrentThesaurus<int, RtpPacket> PacketBuffer = new Common.Collections.Generic.ConcurrentThesaurus<int, RtpPacket>();

        /// <summary>
        /// The server which created this ClientSession
        /// </summary>
        internal RtspServer m_Server, m_Contained;
        
        //The Id of the client
        internal Guid m_Id = Guid.NewGuid();

        /// <summary>
        /// Counters for sent and received bytes
        /// </summary>
        internal int m_Receieved, m_Sent;
        
        //Buffer for data
        internal Common.MemorySegment m_Buffer;

        //Rtsp Socket
        internal Socket m_RtspSocket;

        //The last response sent to this client session
        internal RtspMessage LastResponse;

        //RtpClient for transport of media
        internal RtpClient m_RtpClient;

        internal byte[] m_SendBuffer;

        internal bool m_IsDisconnected;

        //Use the m_RtpClient to determine if Bandwidth is exceeded and Buffer packets until not exceeded.
        //internal double MaximumBandwidth = 0;
        
        internal EndPoint RemoteEndPoint;

        #endregion

        #region Properties

        public Guid Id { get; internal set; }

        public string SessionId { get; internal set; }

        public RtspMessage LastRequest { get; internal set; }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return (IPEndPoint)m_RtspSocket.LocalEndPoint;
            }
        }

        public bool IsDisconnected { get { return m_IsDisconnected; } internal set { m_IsDisconnected = value; } }

        public bool SharesSocket
        {
            get
            {
                //The socket is shared with the GC
                if (IsDisposed) return true;

                //Fast path if it was already decided.
                if (IsDisconnected && LeaveOpen) return LeaveOpen;

                //If the session has any playing media
                if (Playing.Count > 0)
                {
                    // A null or disposed client or one which is no longer connected cannot share the socket
                    if (m_RtpClient == null || m_RtpClient.IsDisposed || false == m_RtpClient.IsActive) return false;

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

        public Socket RtspSocket
        {
            get { return m_RtspSocket; }
            internal set
            {
                m_RtspSocket = value;

                if (m_RtspSocket != null) RemoteEndPoint = m_RtspSocket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Gets or sets a value which indicates if the socket will be closed when Dispose is called.
        /// </summary>
        public bool LeaveOpen { get; set; }

        #endregion

        #region Constructor

        public ClientSession(RtspServer server, Socket rtspSocket, Common.MemorySegment buffer = null)
        {
            Id = Guid.NewGuid();

            //The RtspSession ID should be set here to prevent this session from accessing another session,
            //The only problem with this is that is how the nature of TCP may work also... e.g. the client may open and close connections at will in between requests.
            //This means any TCP connection can technially just as in UDP access another session so long as the SessionID is known.
            //Agents will attempt to check the EndPoint however if the packet was forged [and successfully transmitted] then the session is obtained through that mechanism...
            //TCP provides a `stronger` protection against this type of attack (forging) by default where as UDP does not and most large entities are their own provider and thus...

            m_Server = server;

            //Assign the socket and remote endPoint, IPPacketInformation provides thus for UDP
            RtspSocket = rtspSocket;

            if (m_RtspSocket == null) return;

            //m_RtspSocket.Blocking = false;

            //m_RtspSocket.ExclusiveAddressUse = true;

            //Configure TCP Sockets
            if (m_RtspSocket.ProtocolType == ProtocolType.Tcp)
            {
                //m_RtspSocket.NoDelay = true;

                m_RtspSocket.SendBufferSize = 0;

                m_RtspSocket.ReceiveBufferSize = 0;

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpNoSynRetries(m_RtspSocket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpTimestamp(m_RtspSocket);

                // canot specify from a child socket, SO_ACCEPT is already set.
                //Media.Common.Extensions.Socket.SocketExtensions.SetTcpOffloadPreference(m_RtspSocket);

                // Set option that allows socket to close gracefully without lingering.
                Media.Common.Extensions.Socket.SocketExtensions.DisableLinger(m_RtspSocket);

                //Retransmit for 0 sec
                Media.Common.Extensions.Socket.SocketExtensions.DisableTcpRetransmissions(m_RtspSocket);

                //If both send and receieve buffer size are 0 then there is no coalescing when nagle's algorithm is disabled
                Media.Common.Extensions.Socket.SocketExtensions.DisableTcpNagelAlgorithm(m_RtspSocket);
                //socket.NoDelay = true;

                //Allow more than one byte of urgent data
                Media.Common.Extensions.Socket.SocketExtensions.EnableTcpExpedited(m_RtspSocket);
                //socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);

                //Receive any urgent data in the normal data stream
                Media.Common.Extensions.Socket.SocketExtensions.EnableTcpOutOfBandDataInLine(m_RtspSocket);
            }

            if (m_RtspSocket.AddressFamily == AddressFamily.InterNetwork) m_RtspSocket.DontFragment = true;

            //m_RtspSocket.SendTimeout = m_RtspSocket.ReceiveTimeout = (int)(m_Server.RtspClientInactivityTimeout.TotalMilliseconds / 3);

            //zActual connect time...
            m_RtspSocket.SendTimeout = m_RtspSocket.ReceiveTimeout = (int)RtspClient.DefaultConnectionTime.TotalMilliseconds;

            //Create a buffer using the size of the largest message possible without a Content-Length header.
            //This helps to ensure that partial messages are not recieved by the server from a client if possible (should eventually allow much smaller)
            if (buffer == null)
                m_Buffer = new Common.MemorySegment(RtspMessage.MaximumLength);
            else
                m_Buffer = buffer;

            //Should be optional.

            //Start receiving data
            StartReceive();
        }

        #endregion

        #region Methods

        public void StartReceive()
        {
            long pollTime = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond;// (int)(m_RtspSocket.ReceiveTimeout * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);// Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroSeconds(Media.Common.Extensions.Socket.SocketExtensions.GetNetworkInterface(m_RtspSocket));

            //while the socket cannot read in 1msec or less 
            while (false == IsDisposed && false == m_RtspSocket.Poll((int)pollTime, SelectMode.SelectRead))
            {

                //Wait for the last recieve to complete
                //Might not need this when not using Async.
                if (LastRecieve != null)
                {
                    if (false == LastRecieve.IsCompleted)
                    {

                        WaitHandle wait = LastRecieve.AsyncWaitHandle;

                        Media.Common.Extensions.WaitHandle.WaitHandleExtensions.TryWaitOnHandleAndDispose(ref wait);
                    }
                }

                //If session is disposed or the socket is shared then jump
                if (SharesSocket) goto NotDisconnected;
            }

            //Ensure not disposed or marked disconnected
            if (IsDisposed) return;

            if(SharesSocket) goto NotDisconnected;

            //Begin to receive what is available
            LastRecieve = m_RtspSocket.BeginReceiveFrom(m_Buffer.Array, m_Buffer.Offset, m_Buffer.Count, SocketFlags.None, ref RemoteEndPoint, new AsyncCallback(m_Server.ProcessReceive), this);

        NotDisconnected:
            //Mark as not disconnected.
            IsDisconnected = false;
        }

        public void SendRtspData(byte[] data)
        {
            SendRtspData(data, 0, data.Length, SocketFlags.None, RemoteEndPoint);
        }

        public void SendRtspData(byte[] data, int offset, int length, SocketFlags flags = SocketFlags.None, EndPoint other = null)
        {
            if (data == null || length == 0 && SharesSocket) return;

            try
            {

                long pollTime = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond;// (int)(m_RtspSocket.ReceiveTimeout * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);// Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroSeconds(Media.Common.Extensions.Socket.SocketExtensions.GetNetworkInterface(m_RtspSocket));

                //while the socket cannot write in bit time
                while (false == IsDisposed && false == m_RtspSocket.Poll((int)pollTime, SelectMode.SelectWrite)) 
                {
                    ////Wait for the last send to complete
                    if (LastSend != null)
                    {
                        if (false == LastSend.IsCompleted)
                        {

                            WaitHandle wait = LastSend.AsyncWaitHandle;

                            Media.Common.Extensions.WaitHandle.WaitHandleExtensions.TryWaitOnHandleAndDispose(ref wait);
                        }
                    }
                }

                //If session is disposed then return
                if (IsDisposed) return;

                //Assign the buffer
                m_SendBuffer = data;

                //Mark as not disconnected.(now incase the call succeeds before marked)
                IsDisconnected = false;

                //The state is this session.
                LastSend = m_RtspSocket.BeginSendTo(m_SendBuffer, offset, length, flags, other ?? RemoteEndPoint, m_Server.ProcessSendComplete, this);

                //Mark as not disconnected.
                //IsDisconnected = false;
            }
            catch (Exception ex)
            {
                if (m_Server != null)
                {
                    //Log the excetpion
                    //m_Server.Logger.LogException(ex);

                    Media.Common.ILoggingExtensions.LogException(m_Server.Logger, ex);

                    //if a socket exception occured then handle it.
                    if (ex is SocketException) m_Server.HandleClientSocketException((SocketException)ex, this);
                }
            }
        }

        internal RtpClient.TransportContext GetSourceContext(RtpPacket packet)
        {
            try
            {
                foreach (RtpClient.TransportContext context in Attached.Keys)
                    if (packet.SynchronizationSourceIdentifier == context.RemoteSynchronizationSourceIdentifier) return context;
            }
            catch (InvalidOperationException)
            {
                return GetSourceContext(packet);
            }
            catch { }
            return null;
        }

        internal RtpClient.TransportContext GetSourceContext(Sdp.MediaDescription md)
        {
            try
            {
                foreach (RtpClient.TransportContext context in Attached.Keys)
                    if (md.MediaType == context.MediaDescription.MediaType) return context;
            }
            catch (InvalidOperationException)
            {
                return GetSourceContext(md);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Called for each RtpPacket received in the source RtpClient
        /// </summary>
        /// <param name="client">The RtpClient from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtpPacketRecieved(object client, RtpPacket packet = null, RtpClient.TransportContext tc = null)
        {

            //If the packet is null or not allowed then return
            if (Common.BaseDisposable.IsNullOrDisposed(packet) || m_RtpClient == null) return;

            Thread.BeginCriticalRegion();

            //Get a source context
            RtpClient.TransportContext localContext = null, sourceContext = tc ?? GetSourceContext(packet);

            //Get the sourceContext incase the same payload type was used more then once otherwise fallback to the context for the Payloadtype
            if (sourceContext != null)
            {                
                localContext = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);
            }
            else
            {
                sourceContext = localContext = m_RtpClient.GetContextByPayloadType(packet.PayloadType);
            }

            if (false == localContext.UpdateSequenceNumber(packet.SequenceNumber))
            {
                //Common.ILoggingExtensions.Log(m_Server.Logger, "Not valid sequence number " + packet.SequenceNumber + ", = " + localContext.SequenceNumber);

                goto Exit;
            }

            //If there is no context then don't send.
            //OR
            //If the context already sent the packet don't send                       //(make sure the sequence number didn't wrap)
            //if (context == null || context.SequenceNumber >= packet.SequenceNumber && sourceContext.SequenceNumber != packet.SequenceNumber) goto Exit;

            if (PacketBuffer.ContainsKey(sourceContext.SynchronizationSourceIdentifier))
            {
                PacketBuffer.Add(sourceContext.SynchronizationSourceIdentifier, packet);
            }
            else if (m_RtpClient != null)
            {
                //Send packet on Client Thread
                m_RtpClient.EnquePacket(packet);
            }

        Exit:
            Thread.EndCriticalRegion();

            return;
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(object stream, RtcpPacket packet)
        {

            if (Common.BaseDisposable.IsNullOrDisposed(packet) || m_RtpClient == null) return;

            //Should check for Goodbye and Deactivate this source

            //m_RtpClient.SendReports();

            //m_RtpClient.EnquePacket(new RtcpPacket(packet.Prepare().ToArray(), 0));

            //if (packet.PayloadType == Rtcp.SendersReport.PayloadType) // Reduced size...
            //{

            //    var sourceContext = SourceContexts.FirstOrDefault(tc => tc.RemoteSynchronizationSourceIdentifier == packet.SynchronizationSourceIdentifier);

            //    if (sourceContext == null) return;

            //    var context = m_RtpClient.GetContextByPayloadType(sourceContext.MediaDescription.MediaFormat);

            //    if (context == null) return;

            //    context.RtpTransit = sourceContext.RtpTransit;
            //    context.RtpJitter = sourceContext.RtpJitter;

                //using (Rtcp.SendersReport sr = new SendersReport(packet, false))
                //{
                //    context.NtpTimestamp = sr.NtpTimestamp;
                //    context.RtpTimestamp = sr.RtpTimestamp;

            //Could calulcate NtpOffset from difference here if desired.

                //    if (sr.BlockCount > 0)
                //    {
                //        Rtcp.IReportBlock reportBlock = sr.First(rb => SourceContexts.Any(sc => sc.RemoteSynchronizationSourceIdentifier == rb.BlockIdentifier));

                //        if (reportBlock != null)
                //        {
                //            ReportBlock block = (ReportBlock)reportBlock;

                //            context.SequenceNumber = block.ExtendedHighestSequenceNumberReceived;
                //            context.RtpJitter = (uint)block.InterarrivalJitterEstimate;
                //        }
                //    }
                //}

            //}

            //m_RtpClient.SendReports();
        }

        internal void RemoveAllAttachmentsAndClearPlaying()
        {
            try
            {
                //Get rid of any attachment this ClientSession had
                foreach (Media.Rtsp.Server.IMediaSource source in Attached.Values.ToList())
                {
                    //Remove the attached media
                    RemoveSource(source);
                }

                //Ensure nothing is playing
                Playing.Clear();

            }
            catch
            {
                // The list was being cleared already.
            }
        }

        /// <summary>
        /// Sends the Rtcp Goodbye and detaches all sources
        /// </summary>
        public override void Dispose()
        {

            if (IsDisposed) return;

            base.Dispose();

            RemoveAllAttachmentsAndClearPlaying();

            //Mark as disconnected
            IsDisconnected = true;

            //Deactivate the RtpClient so it's not hanging around wasting resources for nothing
            if (m_RtpClient != null)
            {
                try
                {
                    m_RtpClient.InterleavedData -= m_RtpClient_InterleavedData;

                    m_RtpClient.Dispose();

                    m_RtpClient = null;
                }
                catch { }
            }

            if (m_Buffer != null)
            {
                try
                {
                    m_Buffer.Dispose();

                    m_Buffer = null;
                }
                catch { }
            }

            if (m_RtspSocket != null)
            {
                try
                {
                    if (false == LeaveOpen) m_RtspSocket.Dispose();

                    m_RtspSocket = null;
                }
                catch { }
            }
          
            if (LastRequest != null)
            {
                try
                {
                    LastRequest.Dispose();

                    LastRequest = null;
                }
                catch { }
            }

            if (LastResponse != null)
            {
                try
                {
                    LastResponse.Dispose();

                    LastResponse = null;
                }
                catch { }
            }

            m_Server = m_Contained = null;
        }

        /// <summary>
        /// Process a Rtsp DESCRIBE.
        /// Re-writes the Sdp.SessionDescription in a manner which contains the values of the server and not of the origional source.
        /// </summary>
        /// <param name="describeRequest">The request received from the server</param>
        /// <param name="source">Tje source stream to describe</param>
        /// <returns>A RtspMessage with a Sdp.SessionDescription in the Body and ContentType set to application/sdp</returns>
        internal RtspMessage ProcessDescribe(RtspMessage describeRequest, Media.Rtsp.Server.IMedia source)
        {
            RtspMessage describeResponse = CreateRtspResponse(describeRequest);

            describeResponse.SetHeader(RtspHeaders.ContentType, Sdp.SessionDescription.MimeType);

            //Don't cache this SDP
            describeResponse.SetHeader(RtspHeaders.CacheControl, "no-cache");
            //describeResponse.SetHeader(RtspHeaders.Pragma, "no-cache");

            //If desired you will need a way to determine when the last time the sdp was modified, could use the sessionId from the SDP or its' created property etc.)
            //describeResponse.SetHeader(RtspHeaders.LastModified, 

            if (describeRequest.Location.ToString().ToLowerInvariant().Contains("live"))
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, "rtsp://" + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString() + "/live/" + source.Id + '/');
            }
            else
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, describeRequest.Location.ToString());
            }

            //Create a Session Description to describe the media requested
            using (var sessionDescription = CreateSessionDescription(source))
            {
                //Set the body
                describeResponse.Body = sessionDescription.ToString();

                //Clients sessionId is created from the Sdp's SessionId Line
                //if (string.IsNullOrWhiteSpace(SessionId)) SessionId = sessionDescription.SessionId;
            }

            //Return the resulting message
            return describeResponse;
        }

        internal RtspMessage ProcessPlay(RtspMessage playRequest, RtpSource source)
        {
            //Determine if the request can be processed.
            bool playAllowed = false;

            //Indicate why play would not be allowed.
            string information = string.Empty;

            //Check the there is an underlying transport which has not already been disposed.
            if (m_RtpClient == null
                ||
                true == m_RtpClient.IsDisposed)
            {
                //Indicate the SETUP needs to occur again.
                information = "Session Transport closed. Perform SETUP.";

                //REDIRECT TO SETUP?

            }
            else playAllowed = true;

            //Get the contexts which are available if play is allowed.
            IEnumerable<RtpClient.TransportContext> sourceAvailable = Enumerable.Empty<RtpClient.TransportContext>();

            //If there is a tranport which can communicate the media then determine if there is an applicable source.
            if (playAllowed)
            {
                //Query the source's transport for a context which has been attached to the session via SETUP.
                sourceAvailable = source.RtpClient.GetTransportContexts().Where(sc => GetSourceContext(sc.MediaDescription) != null);

                //If any context is available then this PLAY request can be honored.
                playAllowed = sourceAvailable.Any();

                //Information 
                information = "No Source Transport. Perform SETUP.";

                //REDIRECT TO SETUP?
            }

            //13.4.16 464 Data Transport Not Ready Yet
            //The data transmission channel to the media destination is not yet ready for carrying data.
            if (false == playAllowed)                
            {
                return CreateRtspResponse(playRequest, RtspStatusCode.DataTransportNotReadyYet, null, information);
            }

            //bool allowIncomingRtp = false;

            //Attach the packet events if not already attached (E.g. paused)
            if (false == Playing.Contains(source.Id))
            {
                //Attach events based on how the source will raise them.
                if (source.RtpClient.FrameChangedEventsEnabled) source.RtpClient.RtpFrameChanged += OnSourceFrameChanged;
                else source.RtpClient.RtpPacketReceieved += OnSourceRtpPacketRecieved;

                //Ensure playing
                Playing.Add(source.Id);
            }//else could skip ahead, already playing. Rtsp 2 allows this to be a keep alive.

            //Don't observe frame events in the local client.
            m_RtpClient.FrameChangedEventsEnabled = false;

            //m_RtpClient.IncomingPacketEventsEnabled = allowIncomingRtp;

            //Prepare a place to hold the response
            RtspMessage playResponse = CreateRtspResponse(playRequest);

            //Get the Range header
            string rangeHeader = playRequest[RtspHeaders.Range];
            
            TimeSpan? startRange = null, endRange = null;

            /*
             A PLAY request without a Range header is legal. It starts playing a
             stream from the beginning unless the stream has been paused. If a
             stream has been paused via PAUSE, stream delivery resumes at the
             pause point. If a stream is playing, such a PLAY request causes no
             further action and can be used by the client to test server liveness.
           */

            //Determine if the client wants to start playing from a specific point in time or until a specific point
            if (false == string.IsNullOrWhiteSpace(rangeHeader))
            {
                
                //TODO
                //If the source does not support seeking then a 456 must be returned.
                //Will require a property or convention in the SessionDescripton e.g. a=broadcast.
                //return CreateRtspResponse(playRequest, RtspStatusCode.HeaderFieldNotValidForResource);
                    
                string type; TimeSpan start, end;

                //If parsing of the range header was successful
                if (RtspHeaders.TryParseRange(rangeHeader, out type, out start, out end))
                {
                    //Determine the max start time
                    TimeSpan max = sourceAvailable.Max(tc => tc.MediaEndTime);                  

                    //Start playing from here
                    startRange = start;                    
                    
                    //End playing after this time if given and not unspecified
                    endRange = end;

                    //http://stackoverflow.com/questions/4672359/why-does-timespan-fromsecondsdouble-round-to-milliseconds

                    if(end != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan
                        &&
                        (end += Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) > max) return CreateRtspResponse(playRequest, RtspStatusCode.InvalidRange, null, "Invalid End Range");

                    //If the given time to start at is > zero
                    if (start > TimeSpan.Zero)
                    {
                        //If the maximum is not infinite and the start exceeds the max indicate this.
                        if (max != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan
                            &&
                            start > max) return CreateRtspResponse(playRequest, RtspStatusCode.InvalidRange, null, "Invalid Start Range");
                    }

                    //If the end time is infinite and the max is not infinite then the end is the max time.
                    if (end == Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan && max != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) endRange = end = max;

                    //If the start time is 0 and the end time is not infinite then start the start time to the uptime of the stream (how long it has been playing)
                    if (start == TimeSpan.Zero && end != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) startRange = start = source.RtpClient.Uptime;
                    else startRange = null;
                }
            }

            //Todo Process Scale, Speed, Bandwidth, Blocksize

            //Set Seek-Style to indicate if Seeking is supported.

            //Prepare the RtpInfo header
            //Iterate the source's TransportContext's to Augment the RtpInfo header for the current request
            List<string> rtpInfos = new List<string>();

            string lastSegment = playRequest.Location.Segments.Last();

            Sdp.MediaType mediaType;            

            //If the mediaType was specified
            if (Enum.TryParse(lastSegment, true, out mediaType))
            {
                var sourceContext = sourceAvailable.FirstOrDefault(tc => tc.MediaDescription.MediaType == mediaType);

                //AggreateOperationNotAllowed?
                if (sourceContext == null) return CreateRtspResponse(playRequest, RtspStatusCode.BadRequest, null, "Source Not Setup");

                var context = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                //Create the RtpInfo header for this context.

                //There should be a better way to get the Uri for the stream
                //E.g. ServerLocation should be used.

                //UriEnecode?

                bool hasAnyState = sourceContext.RtpPacketsReceived > 0 || sourceContext.RtpPacketsSent > 0 && false == context.InDiscovery;

                //RtpInfoDatum / SubHeader
                rtpInfos.Add(RtspHeaders.RtpInfoHeader(new Uri("rtsp://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address + "/live/" + source.Id + '/' + context.MediaDescription.MediaType.ToString()),
                    hasAnyState ? sourceContext.SequenceNumber : (int?)null,
                    hasAnyState ? sourceContext.RtpTimestamp : (int?)null,
                    hasAnyState ? (int?)null : context.SynchronizationSourceIdentifier));

                //Identify now to emulate GStreamer :P
                m_RtpClient.SendSendersReport(context);

                //Done with context.
                context = null;
            }
            else
            {
                foreach (RtpClient.TransportContext sourceContext in sourceAvailable)
                {
                    var context = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                    if (context == null) continue;

                    //Create the RtpInfo header for this context.

                    //UriEnecode?

                    //There should be a better way to get the Uri for the stream
                    //E.g. ServerLocation should be used.
                    bool hasAnyState = sourceContext.RtpPacketsReceived > 0 || sourceContext.RtpPacketsSent > 0 && false == context.InDiscovery;

                    rtpInfos.Add(RtspHeaders.RtpInfoHeader(new Uri("rtsp://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address + "/live/" + source.Id + '/' + context.MediaDescription.MediaType.ToString()),
                        hasAnyState ? sourceContext.SequenceNumber : (int?)null,
                        hasAnyState ? sourceContext.RtpTimestamp : (int?)null,
                        hasAnyState ? (int?)null : context.SynchronizationSourceIdentifier));

                    //Done with context.
                    context = null;
                }

                //Send all reports
                m_RtpClient.SendSendersReports();
            }          

            //Indicate the range of the play response. (`Range` will be 'now-' if no start or end was given)
            playResponse.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startRange, endRange));

            //Set the rtpInfo
            playResponse.SetHeader(RtspHeaders.RtpInfo, string.Join(", ", rtpInfos.ToArray()));


            //Todo
            //Set the MediaProperties header.

            //Ensure RtpClient is now connected connected so packets will begin to go out when enqued
            if (false == m_RtpClient.IsActive)
            {
                m_RtpClient.Activate();
                
                //m_RtpClient.m_WorkerThread.Priority = ThreadPriority.Highest;
            }

            //Return the response
            return playResponse;
        }

        void OnSourceFrameChanged(object sender, RtpFrame frame = null, RtpClient.TransportContext tc = null, bool final = false)
        {
            if (Common.BaseDisposable.IsNullOrDisposed(frame)) return;

            //if (false == final && false == frame.IsComplete) return;

            foreach (var packet in frame)
            {
                //if (packet.Transferred.HasValue) continue;

                OnSourceRtpPacketRecieved(sender, packet, tc);

                //packet.Transferred = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes all packets from the PacketBuffer related to the given source and enqueues them on the RtpClient of this ClientSession
        /// </summary>
        /// <param name="source">The RtpSource to check for packets in the PacketBuffer</param>
        internal void ProcessPacketBuffer(RtpSource source)
        {
            //Process packets from the PacketBuffer relevent to the Range Header
            IEnumerable<RtpPacket> packets;            

            //Iterate all TransportContext's in the Source
            foreach (RtpClient.TransportContext sourceContext in source.RtpClient.GetTransportContexts())
            {
                if (sourceContext == null) continue;
                //If the PacketBuffer has any packets related remove packets from the PacketBuffer
                if (PacketBuffer.Remove((int)sourceContext.SynchronizationSourceIdentifier, out packets))
                {
                    //Send them out, where does UpdateSequenceNumber get called? should be determined by the TransportContext to which the packet pertain
                    m_RtpClient.m_OutgoingRtpPackets.AddRange(packets.SkipWhile(rtp => rtp.SequenceNumber < sourceContext.SequenceNumber));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="sourceContext"></param>
        /// <returns></returns>
        /// //TODO Should be SourceMedia and SourceContext.
        internal RtspMessage ProcessSetup(RtspMessage request, RtpSource sourceStream, RtpClient.TransportContext sourceContext)
        {
            //Assign the sessionId now if it has not been assigned before.
            if (SessionId == null) SessionId = m_Id.GetHashCode().ToString();

            //We also have to send one back
            string returnTransportHeader = null;           

            //Create a response
            RtspMessage response = CreateRtspResponse(request);

            Sdp.MediaDescription mediaDescription = sourceContext.MediaDescription;

            bool rtcpDisabled = sourceStream.m_DisableQOS;

            //Values in the header we need
            int clientRtpPort = -1, clientRtcpPort = -1, serverRtpPort = -1, serverRtcpPort = -1, localSsrc = 0, remoteSsrc = 0;

            //Cache this to prevent having to go to get it every time down the line
            IPAddress sourceIp = IPAddress.Any;

            string mode;

            bool unicast, multicast, interleaved, multiplexing;

            byte dataChannel = 0, controlChannel = 1;

            //Get the transport header
            string transportHeader = request[RtspHeaders.Transport];

            //If that is not present we cannot determine what transport the client wants
            if (string.IsNullOrWhiteSpace(transportHeader) || 
                false == (transportHeader.Contains("RTP")) ||
                false == RtspHeaders.TryParseTransportHeader(transportHeader,
                    out localSsrc, out sourceIp, out serverRtpPort, out serverRtcpPort, out clientRtpPort, out clientRtcpPort,
                    out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast))
            {
                return CreateRtspResponse(request, RtspStatusCode.BadRequest, null, "Invalid Transport Header");
            }

            //RTCP-mux: when RTSP 2.0 is official... (Along with Server Sent Messages)

            //Check if the ssrc was 0 which indicates any id
            if (localSsrc == 0) localSsrc = RFC3550.Random32((int)sourceContext.MediaDescription.MediaType);

            //Could also randomize the setupContext sequenceNumber here.
            //We need to make an TransportContext in response to a setup
            RtpClient.TransportContext setupContext = null;            

             //Check for already setup stream and determine if the stream needs to be setup again or just updated
            if (Attached.ContainsKey(sourceContext))
            {
                //The contex may already existm should look first by ssrc.
                setupContext = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                //If the context exists
                if (setupContext != null)
                {
                    //Update the ssrc  if it doesn't match.
                    if (localSsrc != 0 && setupContext.SynchronizationSourceIdentifier != localSsrc)
                    {
                        setupContext.SynchronizationSourceIdentifier = localSsrc;

                        if (remoteSsrc != 0 && setupContext.RemoteSynchronizationSourceIdentifier != remoteSsrc) setupContext.RemoteSynchronizationSourceIdentifier = remoteSsrc;
                    }

                    multicast = Media.Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(((IPEndPoint)setupContext.RemoteRtp).Address);

                    interleaved = setupContext.RtpSocket.ProtocolType == ProtocolType.Tcp && SharesSocket;

                    //Then indicate the information for that context in the return transport header.
                    returnTransportHeader = RtspHeaders.TransportHeader(setupContext.MediaDescription.MediaProtocol, setupContext.SynchronizationSourceIdentifier, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, ((IPEndPoint)setupContext.RemoteRtp).Port, ((IPEndPoint)setupContext.RemoteRtcp).Port, ((IPEndPoint)setupContext.LocalRtp).Port, ((IPEndPoint)setupContext.LocalRtcp).Port, false == multicast, multicast, null, interleaved, setupContext.DataChannel, setupContext.ControlChannel);

                    setupContext.LeaveOpen = interleaved;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.Logger;

                    goto UpdateContext;
                }
            }

            //Should determine intervals here for Rtcp from SessionDescription

            //Should determine if aggregate operation is allowed

            //Maybe setting up both udp and tcp at the same time? clientRtpPort needs to be nullable.
            //Maybe better to just give tokens from the function ..
            //Without explicitly checking for !interleaved VLC will recieve what it thinks are RTSP responses unless RTSP Interleaved is Forced.
            //Was trying to Quicktime to pickup RTSP Interleaved by default on the first response but it doesn't seem that easy (quick time tries to switch but fails?)

            //If the source does not force TCP and interleaved was not given and this is a unicast or multicast connection
            if (false == interleaved && (unicast || multicast)) 
            {

                //Check requested transport is allowed by server
                if (sourceStream.ForceTCP)//The client wanted Udp and Tcp was forced
                {
                    //Return the result
                    var result = CreateRtspResponse(request, RtspStatusCode.UnsupportedTransport);

                    //Indicate interleaved is required.
                    result.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, null, null, null, null, null, false, null, true, dataChannel, controlChannel));

                    return result;
                }

                //QuickTime debug

                if (clientRtpPort == 0) clientRtpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(ProtocolType.Udp, 30000, true);                

                if (clientRtcpPort == 0) clientRtcpPort = clientRtpPort + 1;

                if (serverRtpPort == 0) serverRtpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(ProtocolType.Udp, 30000, true);

                if (serverRtcpPort == 0) serverRtcpPort = serverRtpPort + 1;

                //Ensure the ports are allowed to be used.
                if (m_Server.MaximumUdpPort.HasValue && 
                    (clientRtpPort > m_Server.MaximumUdpPort || clientRtcpPort > m_Server.MaximumUdpPort))
                {
                    //Handle port out of range
                    return CreateRtspResponse(request, RtspStatusCode.BadRequest, null, "Requested Udp Ports were out of range. Maximum Port = " + m_Server.MaximumUdpPort);
                }

                //Create sockets to reserve the ports.

                var localAddress = ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address;

                Socket tempRtp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, localAddress, clientRtpPort);

                Socket tempRtcp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, localAddress, clientRtcpPort);

                //Check if the client was already created.
                if (Common.BaseDisposable.IsNullOrDisposed(m_RtpClient))
                {
                    //Create a sender using a new segment on the existing buffer.
                    m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                    //Dont handle frame changed events from the client
                    m_RtpClient.FrameChangedEventsEnabled = false;

                    //Dont handle packets from the client
                    m_RtpClient.HandleIncomingRtpPackets = false;

                    //Attach the Interleaved data event
                    m_RtpClient.InterleavedData += m_RtpClient_InterleavedData;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.Logger;

                    //Use default data and control channel (Should be option?)
                    setupContext = new RtpClient.TransportContext(0, 1, localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);
                }
                else //The client was already created.
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().LastOrDefault();

                    if (lastContext != null) setupContext = new RtpClient.TransportContext((byte)(lastContext.DataChannel + 2), (byte)(lastContext.ControlChannel + 2), localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);
                    else setupContext = new RtpClient.TransportContext(dataChannel, controlChannel, localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);
                }

                //Initialize the Udp sockets
                setupContext.Initialize(localAddress, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, serverRtpPort, serverRtcpPort, clientRtpPort, clientRtcpPort);

                //Ensure the receive buffer size is updated for that context.
                Media.Common.ISocketReferenceExtensions.SetReceiveBufferSize(((Media.Common.ISocketReference)setupContext), m_Buffer.Count);

                ////Check if the punch packets made it out.
                //if ((setupContext.IsRtpEnabled && ((IPEndPoint)setupContext.RemoteRtp).Port == 0) 
                //    || 
                //    (setupContext.IsRtcpEnabled && ((IPEndPoint)setupContext.RemoteRtcp).Port == 0))
                //{
                //    //Response should be a 461 or we should indicate the remote party is not yet listening in the response 
                //    //Could also use StatusCode (100) with a reason phrase or header
                //}

                //Add the transportChannel
                m_RtpClient.TryAddContext(setupContext);

                //Create the returnTransportHeader (Should be setupContext.SynchronizationSourceIdentifier)
                returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier, localSsrc, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort, true, false, null, false, 0, 0);

                tempRtp.Dispose();

                tempRtp = null;

                tempRtcp.Dispose();

                tempRtcp = null;

            }

            //Should allow host to specify channels?
            //Check for 'interleaved' token or TCP being forced
            if (sourceStream.ForceTCP || interleaved) 
            {
                //Check if the client was already created.
                if (Common.BaseDisposable.IsNullOrDisposed(m_RtpClient))
                {
                    //Create a sender using a new segment on the existing buffer.
                    m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                    m_RtpClient.InterleavedData += m_RtpClient_InterleavedData;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.Logger;

                    #region Unused [Helps with debugging]

                    //m_RtpClient.RtcpPacketReceieved += m_RtpClient_RecievedRtcp;

                    //m_RtpClient.RtpPacketReceieved += m_RtpClient_RecievedRtp;

                    //m_RtpClient.RtcpPacketSent += m_RtpClient_SentRtcp;

                    #endregion

                    //Dont handle frame changed events from the client
                    m_RtpClient.FrameChangedEventsEnabled = false;

                    //Dont handle packets from the client
                    m_RtpClient.HandleIncomingRtpPackets = false;

                    //Create a new Interleave (don't use what was given as data or control channels)
                    setupContext = new RtpClient.TransportContext((byte)(dataChannel = 0), (byte)(controlChannel = 1), localSsrc, mediaDescription, m_RtspSocket, false == rtcpDisabled, remoteSsrc, 0);

                    //Add the transportChannel the client requested
                    m_RtpClient.TryAddContext(setupContext);

                    //Initialize the Interleaved Socket
                    setupContext.Initialize(m_RtspSocket, m_RtspSocket);
                }
                else //The client was already created
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().LastOrDefault();

                    //Don't use what was given as data or control channels
                    if (lastContext != null) setupContext = new RtpClient.TransportContext(dataChannel = (byte)(lastContext.DataChannel + 2), controlChannel = (byte)(lastContext.ControlChannel + 2), localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);
                    else setupContext = new RtpClient.TransportContext(dataChannel, controlChannel, localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);

                    //Add the transportChannel the client requested
                    m_RtpClient.TryAddContext(setupContext);

                    //Initialize the current TransportChannel with the interleaved Socket
                    setupContext.Initialize(m_RtspSocket, m_RtspSocket);
                }

                //Create the returnTransportHeader
                //returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", setupContext.SynchronizationSourceIdentifier, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, LocalEndPoint.Port, LocalEndPoint.Port, ((IPEndPoint)RemoteEndPoint).Port, ((IPEndPoint)RemoteEndPoint).Port, true, false, null, true, dataChannel, controlChannel);

                //Leave the socket open when disposing the RtpClient
                setupContext.LeaveOpen = true;

                returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, null, null, null, null, null, false, null, true, dataChannel, controlChannel);
            }
        
            //Add the new source
            Attached.Add(sourceContext, sourceStream);
        
            UpdateContext:

            //Synchronize the context sequence numbers
            setupContext.SequenceNumber = sourceContext.SequenceNumber;

            //Start and end times are always equal.
            setupContext.MediaStartTime = sourceContext.MediaStartTime;
            setupContext.MediaEndTime = sourceContext.MediaEndTime;           

            //Set the returnTransportHeader to the value above 
            response.SetHeader(RtspHeaders.Transport, returnTransportHeader);

            //Give the sessionid for the transport setup
            response.SetHeader(RtspHeaders.Session, SessionId);

            return response;
        }

        void m_RtpClient_InterleavedData(object sender, byte[] data, int offset, int length)
        {
            if (length <= 0) return;

            //The session is not disconnected 
            IsDisconnected = false;

            //Process the data received
            m_Server.ProcessClientBuffer(this, length);
        }

        void m_RtpClient_RecievedRtp(object sender, RtpPacket packet)
        {

            if (packet == null || packet.IsDisposed) return;
            else m_Server.Logger.LogException(new Exception("Recieved Rtp PacketType: " + packet.PayloadType + " - " + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            var context = m_RtpClient.GetContextForPacket(packet);

            if (context == null) m_Server.Logger.LogException(new Exception("Unknown Rtp Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else m_Server.Logger.LogException(new Exception("Rtp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Recieved PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        void m_RtpClient_RecievedRtcp(object sender, RtcpPacket packet)
        {

            if (packet == null || packet.IsDisposed) return;

            //Get an implementation for the packet recieved
            var implementation = Rtcp.RtcpPacket.GetImplementationForPayloadType((byte)packet.PayloadType);

            if (implementation == null) m_Server.Logger.LogException(new Exception("Recieved Unknown PacketType: " + packet.PayloadType + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else m_Server.Logger.LogException(new Exception("Recieved Rtcp PacketType: " + packet.PayloadType + " - " + implementation.Name + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            var context = m_RtpClient.GetContextForPacket(packet);

            if (context == null) m_Server.Logger.LogException(new Exception("Unknown Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else m_Server.Logger.LogException(new Exception("Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Recieved Rtcp PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        void m_RtpClient_SentRtcp(object sender, RtcpPacket packet)
        {

            if (packet == null || packet.IsDisposed) return;

            //Get an implementation for the packet recieved
            var implementation = Rtcp.RtcpPacket.GetImplementationForPayloadType((byte)packet.PayloadType);

            if (implementation == null) m_Server.Logger.LogException(new Exception("Sent Unknown PacketType: " + packet.PayloadType + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else m_Server.Logger.LogException(new Exception("Sent Rtcp PacketType: " + packet.PayloadType + " - " + implementation.Name + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            //If the context should have been synchronized then determine if a context can be found
            if (m_RtpClient.Uptime > RtpClient.DefaultReportInterval)
            {
                var context = m_RtpClient.GetContextForPacket(packet);

                if (context == null) m_Server.Logger.LogException(new Exception("Sent Unknown Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
                else m_Server.Logger.LogException(new Exception("Sent Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));
            }

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Sent Rtcp PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        internal RtspMessage ProcessPause(RtspMessage request, RtpSource source)
        {
            //If the source is attached
            if (Attached.ContainsValue(source))
            {
                //Iterate the source transport contexts
                foreach (RtpClient.TransportContext sourceContext in source.RtpClient.GetTransportContexts())
                {
                    //Adding the id will stop the packets from being enqueued into the RtpClient
                    PacketBuffer.Add((int)sourceContext.SynchronizationSourceIdentifier);
                }

                //Return the response
                return CreateRtspResponse(request);
            }

            //The source is not attached
            return CreateRtspResponse(request, RtspStatusCode.MethodNotValidInThisState);
            
        }

        /// <summary>
        /// Detaches the given SourceStream from the ClientSession
        /// </summary>
        /// <param name="source">The SourceStream to detach</param>
        /// <param name="session">The session to detach from</param>
        internal void RemoveSource(Media.Rtsp.Server.IMediaSource source)
        {
            if (source is RtpSource)
            {
                RtpSource rtpSource = source as RtpSource;
                if (rtpSource.RtpClient != null)
                {
                    //For each TransportContext in the RtpClient
                    foreach (RtpClient.TransportContext tc in rtpSource.RtpClient.GetTransportContexts()) Attached.Remove(tc);

                    //Attach events
                    //rtpSource.RtpClient.RtcpPacketReceieved -= OnSourceRtcpPacketRecieved;
                    rtpSource.RtpClient.RtpPacketReceieved -= OnSourceRtpPacketRecieved;
                    rtpSource.RtpClient.RtpFrameChanged -= OnSourceFrameChanged;
                }
            }

            Playing.Remove(source.Id);
        }

        /// <summary>
        /// Removes an attachment from a ClientSession to the given source where the media desciprtion 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="md"></param>
        /// <param name="session"></param>
        internal void RemoveMedia(Sdp.MediaDescription md)
        {
            //Determine if we have a source which corresponds to the mediaDescription given
            RtpClient.TransportContext sourceContext = Attached.Keys.FirstOrDefault(c => c.MediaDescription == md);

            //If the sourceContext is not null
            if (sourceContext != null)
            {
                //Remove the entry from the sessions routing table
                Attached.Remove(sourceContext);
            }
        }

        /*
         10.7 TEARDOWN

        The TEARDOWN request stops the stream delivery for the given URI,
        freeing the resources associated with it. If the URI is the
        presentation URI for this presentation, any RTSP session identifier
        associated with the session is no longer valid. Unless all transport
        parameters are defined by the session description, a SETUP request
        has to be issued before the session can be played again.
         */

        internal RtspMessage ProcessTeardown(RtspMessage request, RtpSource source)
        {
            //Determine if this is for only a single track or the entire shebang
            if (false == Attached.ContainsValue(source)) return CreateRtspResponse(request, RtspStatusCode.BadRequest);

            //Determine if we have the track
            string track = request.Location.Segments.Last().Replace("/", string.Empty);

            Sdp.MediaType mediaType;

            //For a single track
            if (Enum.TryParse <Sdp.MediaType>(track, true, out mediaType))
            {
                //bool GetContextBySdpControlLine... out mediaDescription
                RtpClient.TransportContext sourceContext = source.RtpClient.GetTransportContexts().FirstOrDefault(sc => sc.MediaDescription.MediaType == mediaType);

                //Cannot teardown media because we can't find the track they are asking to tear down
                if (sourceContext == null)
                {
                    return CreateRtspResponse(request, RtspStatusCode.NotFound);
                }
                else
                {
                    RemoveMedia(sourceContext.MediaDescription);
                }
            }
            else //Tear down all streams
            {
                RemoveSource(source);                
            }
                           
            //Return the response
            return CreateRtspResponse(request);
        }

        internal void ReleaseUnusedResources()
        {
            //Enumerate each context 'SETUP' in the session
            if (m_RtpClient != null)
            {
                //Iterate each context in the client
                foreach (var context in m_RtpClient.GetTransportContexts())
                {
                    //Could be a property of the transport.. (especially the rtpclient.)
                    //E.g. IsInactive

                    //If the context does not have any activity
                    if (false == context.HasAnyRecentActivity)
                    {
                        //Session level logger
                        //Context has no activity

                        //See if there is still a source for the context 
                        RtpClient.TransportContext sourceContext = GetSourceContext(context.MediaDescription);

                        //If there was a source context with activity
                        if (sourceContext != null && sourceContext.HasAnyRecentActivity)
                        {
                            //Remove the attachment from the source context to the session context
                            RemoveSource(Attached[sourceContext]);

                            //Session level logger
                            //Removed Attachment for sourceContext.Id

                            //Remove the reference to the sourceContext
                            sourceContext = null;
                        }
                    }
                }
            }


            //Get rid of any attachment this ClientSession had which no longer have a context.
            foreach (Media.Rtsp.Server.IMediaSource source in Attached.Keys.ToList().Where(s=> GetSourceContext(s.MediaDescription) == null))
            {
                //Remove the attached media
                RemoveSource(source);
            }

            //Remove rtp theads
            if (Playing.Count == 0)
            {
                if (m_RtpClient != null)
                {
                    m_RtpClient.Dispose();
                    m_RtpClient = null;
                }
            }
        }

        internal RtspMessage ProcessRecord(RtspMessage request, Media.Rtsp.Server.IMedia source)
        {
            //Can't record when no Archiver is present
            if (m_Server.Archiver == null) return CreateRtspResponse(request, RtspStatusCode.PreconditionFailed, null, "No Server Archiver.");

            //If already archiving then indicate created
            if (m_Server.Archiver.IsArchiving(source)) return CreateRtspResponse(request, RtspStatusCode.Created);

            //Start archiving
            m_Server.Archiver.Start(source);

            //Return ok response
            return CreateRtspResponse(request);
        }

        /// <summary>
        /// Creates a RtspResponse based on the SequenceNumber contained in the given RtspRequest
        /// </summary>
        /// <param name="request">The request to utilize the SequenceNumber from, if null the current SequenceNumber is used</param>
        /// <param name="statusCode">The StatusCode of the generated response</param>
        /// <returns>The RtspResponse created</returns>
        internal RtspMessage CreateRtspResponse(RtspMessage request = null, RtspStatusCode statusCode = RtspStatusCode.OK, string reasonPhrase = null, string body = null)
        {
            RtspMessage response = new RtspMessage(RtspMessageType.Response);

            response.RtspStatusCode = statusCode;

            /*
             12.4.1 400 Bad Request

           The request could not be understood by the server due to malformed
           syntax. The client SHOULD NOT repeat the request without
           modifications [H10.4.1]. If the request does not have a CSeq header,
           the server MUST NOT include a CSeq in the response.
             */

            if (request != null)
            {
                if (request.ContainsHeader(RtspHeaders.Session)) response.SetHeader(RtspHeaders.Session, request.GetHeader(RtspHeaders.Session));

                if (statusCode != RtspStatusCode.BadRequest) response.CSeq = request.CSeq;
            }
            else if (statusCode != RtspStatusCode.BadRequest && LastRequest.CSeq >= 0) response.CSeq = LastRequest.CSeq;

            //Include any reason phrase.
            if (false == string.IsNullOrWhiteSpace(reasonPhrase)) response.ReasonPhrase = reasonPhrase;

            //else response.ReasonPhrase = statusCode.ToString();

            //Include any body.
            if (false == string.IsNullOrWhiteSpace(body)) response.Body = body;

            return response;
        }


        /// <summary>
        /// Dynamically creates a Sdp.SessionDescription for the given SourceStream using the information already present and only re-writing the necessary values.
        /// </summary>
        /// <param name="stream">The source stream to create a SessionDescription for</param>
        /// <returns>The created SessionDescription</returns>
        internal Sdp.SessionDescription CreateSessionDescription(Media.Rtsp.Server.IMedia stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            //else if (SessionDescription != null) throw new NotImplementedException("There is already a m_SessionDescription for this session, updating is not implemented at this time");

            string sessionId = Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow).ToString(), sessionVersion = Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow).ToString();

            string originatorString = "ASTI-Media-Server " + sessionId + " " + sessionVersion + " IN " + (m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? "IP6 " : "IP4 " ) + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI-Streaming-Session-" + stream.Name;

            Sdp.SessionDescription sdp;

            RtpClient sourceClient;

            bool disableQos = false;

            if (stream is RtpSource)
            {
                RtpSource rtpSource = stream as RtpSource;
                //Make the new SessionDescription
                sdp = new Sdp.SessionDescription(rtpSource.SessionDescription.ToString());

                //Remove the old connection lines if they exist
                while(sdp.ConnectionLine != null) sdp.Remove(sdp.ConnectionLine, false);

                sourceClient = rtpSource.RtpClient;

                disableQos = rtpSource.m_DisableQOS;

            }
            else
            {
                sdp = new Sdp.SessionDescription(0);
            }

            //Change the DocumentVersion and update the name
            sdp.SessionName = sessionName;

            //Change the DocumentVersion and update the originator
            sdp.OriginatorAndSessionIdentifier = originatorString;

            //Type = broadcast
            //charset 

            string protcol = RtspMessage.MessageIdentifier.ToLowerInvariant(), controlLineBase = "a=control:" + protcol + "://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address.ToString() + "/live/" + stream.Id;
            //check for rtspu later...

            //Find an existing control line
            Media.Sdp.SessionDescriptionLine controlLine = sdp.ControlLine;

            //If there was one remove it
            while (controlLine != null)
            {
                sdp.Remove(controlLine);
                controlLine = sdp.ControlLine;
            }
            //Determine if session level control line should be present
            
            //Rewrite a new connection line
            string addressString = LocalEndPoint.Address.ToString();// +"/127/2";

            //int lastPort = Utility.FindOpenPort( stream.m_ForceTCP ? ProtocolType.Tcp : ProtocolType.Udp);

            //Indicate a port in the sdp, setup should also use this port, this should essentially reserve the port for the setup process...
            //if (!stream.m_ForceTCP)
                //addressString += "/127" +'/' +  lastPort + 1;
            //else 
                //addressString += + ((IPEndPoint)RemoteEndPoint).Port;

            
            //Check for the existing connectionLine
            Sdp.Lines.SessionConnectionLine connectionLine = sdp.ConnectionLine as Sdp.Lines.SessionConnectionLine;

            //Add the new line if needed
            if (connectionLine == null) sdp.ConnectionLine = connectionLine = new Sdp.Lines.SessionConnectionLine()
            {
                ConnectionAddress = addressString,
                ConnectionAddressType = m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? Media.Sdp.Lines.SessionConnectionLine.IP6 : Media.Sdp.Lines.SessionConnectionLine.IP4,
                ConnectionNetworkType = Media.Sdp.Lines.SessionConnectionLine.InConnectionToken
            };

            IEnumerable<Sdp.SessionDescriptionLine> bandwithLines;

            //Indicate that the server will not accept media as input for this session
            //Put the attribute in the Session Description,
            //Should check that its not already set?
            //sdp.Add(new Sdp.SessionDescriptionLine("a=recvonly"));

            //Remove any existing session range lines, don't upate the version
            while (sdp.RangeLine != null) sdp.Remove(sdp.RangeLine, false);

            //Todo add a Range line which shows the length of this media.

            //Iterate the source MediaDescriptions, could just create a new one with the fmt which contains the profile level information
            foreach (Sdp.MediaDescription md in sdp.MediaDescriptions)
            {               
                //Find a control line
                controlLine = md.ControlLine;

                //Rewrite it if present to reflect the appropriate MediaDescription
                while (controlLine != null)
                {
                    md.Remove(controlLine);
                    controlLine = md.ControlLine;
                }

                //Remove old bandwith lines
                bandwithLines = md.BandwidthLines;

                //Remove existing bandwidth information, should check for AS
                if(disableQos) foreach (Sdp.SessionDescriptionLine line in bandwithLines) md.Remove(line);

                //Remove all other alternate information
                //Should probably only remove certain ones.
                foreach (Sdp.SessionDescriptionLine line in md.Lines.Where(l => l.Parts.Any(p => p.Contains("alt"))).ToArray()) md.Remove(line);

                //Add a control line for the MedaiDescription (which is `rtsp://./Id/audio` (video etc)
                //Should be a TrackId and not the media type to allow more then one media type to be controlled.
                //e.g. Two audio streams or text streams is valid.
                md.Add(new Sdp.SessionDescriptionLine("a=control:" + "/live/" + stream.Id + '/' + md.MediaType));


                //Add the connection line for the media
                //md.Add(connectionLine);

                //Should check for Timing Info and update for playing streams

                if (disableQos)
                {
                    md.Add(Sdp.Lines.SessionBandwidthLine.DisabledSendLine);
                    md.Add(Sdp.Lines.SessionBandwidthLine.DisabledReceiveLine);
                    md.Add(Sdp.Lines.SessionBandwidthLine.DisabledApplicationSpecificLine);
                }
                //else
                //{
                //    //ToDo use whatever values are defined for the session's bandwidth atp
                //    md.Add(new Sdp.SessionDescriptionLine("b=RS:140"));
                //    md.Add(new Sdp.SessionDescriptionLine("b=RR:140"));

                //    md.Add(new Sdp.SessionDescriptionLine("b=AS:0")); //Determine if AS needs to be forwarded
                //}

                //Should actually reflect outgoing port for this session
                md.MediaPort = 0;

                //Determine if attached and set the MediaPort.
                if (m_RtpClient != null)
                {
                    var context = m_RtpClient.GetContextForMediaDescription(md);

                    if (context != null) md.MediaPort = ((IPEndPoint)context.RemoteRtp).Port;

                    //Set any other variables which may be been changed in the session
                }

                #region Independent TCP

                //if (!stream.m_ForceTCP)
                //{
                //    //md.MediaPort = lastPort;
                //    //lastPort += 2;
                //}
                //else
                //{
                //    //VLC `Blows up` when this happens
                //    //bad SDP "m=" line: m=audio 40563 TCP/RTP/AVP 96
                //    //md.MediaProtocol = "TCP/RTP/AVP";
                //    //fmt should be the same     

                //    //This mainly implies that stand-alone RTP over TCP is occuring anyway.

                //    //Since this code supports the RtspServer this is fine for now.

                //    //The RtpClient also deals with the framing from RTSP when used in conjunction with so..
                //    //This needs to be addressed in the RtpClient which allows currently allows Rtp and Rtcp to be duplexed in TCP and UDP
                //    //but does not handle the case of TCP when a sender wants to connect with 2 seperate TCP sockets as per RFC4571.

                //    //a=setup:passive
                //    //a=connection:new

                //    //The RtpClient would then need to have a RtcpSocket ready on the 'standby' just in case the remote end point connected and began sending the data.

                //    //The other way to hanle this would be use only a single socket in both cases and change the remote endpoint = 0.... and decypher the data based on the end point... e.g. the port.
                //}          

                #endregion


                //Verify Timing lines.
                //Lines should have a startTime equal the Uptime of the stream
                //Lines should have a stopTime equal to the EndTime of the stream.

            }

            //Top level stream control line (Should only be added if Aggregate Control of the stream is allowed.
            //sdp.Add(new Sdp.SessionDescriptionLine(controlLineBase));           

            return sdp;
        }

        #endregion                       
    }
}
