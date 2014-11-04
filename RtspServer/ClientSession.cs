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
using Media.Rtsp.Server.Streams;
using System.Threading;

namespace Media.Rtsp
{
    /// <summary>
    /// Represent the resources in use by remote parties connected to a RtspServer.
    /// </summary>
    internal class ClientSession : Common.BaseDisposable
    {
        //Needs to have it's own concept of range using the Storage...

        #region Fields

        //Session storage
        //Counters for authenticate and attempts should use static key names, maybe use a dictionary..
        internal System.Collections.Hashtable Storage = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());

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

        internal Dictionary<RtpClient.TransportContext, SourceStream> Attached = new Dictionary<RtpClient.TransportContext, SourceStream>();

        /// <summary>
        /// A one to many collection which is keyed by the source media's SSRC to which subsequently the values are packets which also came from the source
        /// </summary>
        internal Common.ConcurrentThesaurus<int, RtpPacket> PacketBuffer = new Common.ConcurrentThesaurus<int, RtpPacket>();

        /// <summary>
        /// The server which created this ClientSession
        /// </summary>
        internal RtspServer m_Server;
        
        //The Id of the client
        internal Guid m_Id = Guid.NewGuid();

        /// <summary>
        /// Counters for sent and received bytes
        /// </summary>
        internal int m_Receieved, m_Sent;
        
        //Buffer for data
        internal Common.MemorySegment m_Buffer;

        //Sockets
        internal Socket m_RtspSocket;

        //The last response sent to this client session
        internal RtspMessage LastResponse;

        //RtpClient for transport of media
        internal RtpClient m_RtpClient;

        internal byte[] m_SendBuffer;

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

        public readonly EndPoint RemoteEndPoint;
           
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

            m_RtspSocket = rtspSocket;

            if (buffer == null)
                m_Buffer = new Common.MemorySegment(RtspMessage.MaximumLength); // new byte[m_BufferLength = RtspMessage.MaximumLength];
            else
                m_Buffer = buffer;

            //Assign the remote endPoint, IPPacketInformation provides thus for UDP
            RemoteEndPoint = rtspSocket.RemoteEndPoint;

            //Begin to receive what is available
            LastRecieve = m_RtspSocket.BeginReceiveFrom(m_Buffer.Array, m_Buffer.Offset, m_Buffer.Count, SocketFlags.None, ref RemoteEndPoint, new AsyncCallback(m_Server.ProcessReceive), this);
        }

        #endregion

        #region Methods

        public void SendRtspData(byte[] data)
        {
            try
            {
                if (m_RtspSocket.Connected)
                {
                    m_SendBuffer = data;
                    LastSend = m_RtspSocket.BeginSendTo(m_SendBuffer, 0, m_SendBuffer.Length, SocketFlags.None, RemoteEndPoint, new AsyncCallback(m_Server.ProcessSend), this);//Begin to Send the response over the RtspSocket
                }
            }
            catch (Exception ex) { m_Server.Logger.LogException(ex); }
        }

        RtpClient.TransportContext GetSourceContextForPacket(RtpPacket packet)
        {
            foreach (RtpClient.TransportContext context in Attached.Keys)
                if (packet.SynchronizationSourceIdentifier == context.RemoteSynchronizationSourceIdentifier) return context;
            return null;
        }

        RtpClient.TransportContext GetSourceContextForPacket(Sdp.MediaDescription md)
        {
            foreach (RtpClient.TransportContext context in Attached.Keys)
                if (md.MediaType == context.MediaDescription.MediaType) return context;
            return null;
        }

        /// <summary>
        /// Called for each RtpPacket received in the source RtpClient
        /// </summary>
        /// <param name="client">The RtpClient from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtpPacketRecieved(object client, RtpPacket packet)
        {

            //If the packet is null or not allowed then return
            if (packet == null || packet.Disposed || m_RtpClient == null) return;
            
            //Get a source context
            RtpClient.TransportContext context = null, sourceContext = GetSourceContextForPacket(packet);

            //Get the sourceContext incase the same payload type was used more then once otherwise fallback to the context for the Payloadtype
            if (sourceContext != null)
            {
                context = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);
            }
            else
            {
                sourceContext = context = m_RtpClient.GetContextByPayloadType(packet.PayloadType);
            }

            if (context == null) return;

            var localPacket = packet;
            
            if(PacketBuffer.ContainsKey(sourceContext.SynchronizationSourceIdentifier)) 
            {
                PacketBuffer.Add(sourceContext.SynchronizationSourceIdentifier, new Rtp.RtpPacket(packet.Prepare(context.MediaDescription.MediaFormat, context.SynchronizationSourceIdentifier).ToArray(), 0));
            }
            else if (m_RtpClient != null)
            {
                int sent = m_RtpClient.SendRtpPacket(localPacket, context.SynchronizationSourceIdentifier);

#if DEBUG
                if(sent < packet.Length) System.Diagnostics.Debug.WriteLine("Truncated : " + (packet.Length - sent).ToString() + " bytes of RtpPacket");
#endif

            }
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(object stream, RtcpPacket packet)
        {

            if (packet == null || packet.Disposed || m_RtpClient == null) return;

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

        /// <summary>
        /// Sends the Rtcp Goodbye and detaches all sources
        /// </summary>
        public override void Dispose()
        {
            try
            {
                //Get rid of any attachment this ClientSession had
                foreach (var source in Attached.Values)
                {
                    RemoveSource(source);
                }

                //End the final send
                if (LastSend != null && !LastSend.IsCompleted)
                {
                    try
                    {
                        EndPoint inBound = RemoteEndPoint;

                        //Ensure the bytes were completely sent..
                        int sent = m_RtspSocket.EndSendTo(LastSend);
                    }
                    catch { }
                }

                //End the final receive
                if (LastRecieve != null && !LastRecieve.IsCompleted)
                {
                    try
                    {
                        //Take note of whre we are receiving from
                        EndPoint inBound = RemoteEndPoint;

                        int received = m_RtspSocket.EndReceiveFrom(LastRecieve, ref inBound);
                    }
                    catch { }
                }

                //Disconnect the RtpClient so it's not hanging around wasting resources for nothing
                if (m_RtpClient != null)
                {
                    m_RtpClient.SendGoodbyes();

                    m_RtpClient.InterleavedData -= m_Server.ProcessRtspInterleaveData;

                    m_RtpClient.Dispose();
                    
                    m_RtpClient = null;
                }

                if (m_RtspSocket != null)
                {
                    m_RtspSocket.Shutdown(SocketShutdown.Both);
                    m_RtspSocket.Dispose();
                    m_RtspSocket = null;
                }

                if (m_Buffer != null)
                {
                    m_Buffer.Dispose();
                    m_Buffer = null;
                }

            }
            catch { return; }
            
        }

        /// <summary>
        /// Process a Rtsp DESCRIBE.
        /// Re-writes the Sdp.SessionDescription in a manner which contains the values of the server and not of the origional source.
        /// </summary>
        /// <param name="describeRequest">The request received from the server</param>
        /// <param name="source">Tje source stream to describe</param>
        /// <returns>A RtspMessage with a Sdp.SessionDescription in the Body and ContentType set to application/sdp</returns>
        internal RtspMessage ProcessDescribe(RtspMessage describeRequest, SourceStream source)
        {
            RtspMessage describeResponse = CreateRtspResponse(describeRequest);

            describeResponse.SetHeader(RtspHeaders.ContentType, Sdp.SessionDescription.MimeType);

            if (describeRequest.Location.ToString().ToLowerInvariant().Contains("live"))
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, "rtsp://" + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString() + "/live/" + source.Id + '/');
            }
            else
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, describeRequest.Location.ToString());
            }

            describeResponse.Body = CreateSessionDescription(source).ToString();

            return describeResponse;
        }

        internal RtspMessage ProcessPlay(RtspMessage playRequest, RtpSource source)
        {
            
            ///TODO MAY ALREADY BE PLAYING OR PAUSED>....
            ///
            ///If the client was paused then simply calling ProcessPacketBuffer will resume correctly without any further processing required
            ///So long as the Source's RtpClient.TransportContext RtpTimestamp is updated to reflect the value given in the playRequest...
            ///
            if (!Attached.ContainsValue(source))
            {
                return CreateRtspResponse(playRequest, RtspStatusCode.BadRequest, "Source Not Setup");
            }

            //Else is already attached but may not be playing...

            //Prepare a place to hold the response
            RtspMessage playResponse = CreateRtspResponse(playRequest);

            //Get the Range header
            string rangeString = playRequest[RtspHeaders.Range];
            TimeSpan? startRange = null, endRange = null;

            //If that is not present we cannot determine where the client wants to start playing from
            if (!string.IsNullOrWhiteSpace(rangeString))
            {
                string type; TimeSpan start, end;
                if (Media.Sdp.SessionDescription.TryParseRange(rangeString, out type, out start, out end))
                {
                    startRange = start;
                    endRange = end;
                }

                //Set the range Range for the playResponse
                playResponse.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startRange, endRange));
            }
            //else
            //{
                //Should come from sourceContext tc.MediaStartTime, tc.MediaEndTime
                //playResponse.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startRange, endRange));
            //}

            //Prepare the RtpInfo header
            //Iterate the source's TransportContext's to Augment the RtpInfo header for the current request

            List<string> rtpInfos = new List<string>();

            foreach (RtpClient.TransportContext tc in source.RtpClient.TransportContexts)
            {
                var context = m_RtpClient.GetContextForMediaDescription(tc.MediaDescription);

                if (context == null) continue;

                rtpInfos.Add(RtspHeaders.RtpInfoHeader(new Uri("rtsp://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address + "/live/" + source.Id + '/' + context.MediaDescription.MediaType.ToString()), 
                    tc.SequenceNumber, tc.RtpTimestamp, context.SynchronizationSourceIdentifier));

                //Identify now to emulate GStreamer :P
                m_RtpClient.SendSendersReport(context);
                
                //Done with context.
                context = null;
            }            

            //Sent the rtpInfo
            playResponse.AppendOrSetHeader(RtspHeaders.RtpInfo, string.Join(", ", rtpInfos.ToArray()));

            //May double attach events if was already playing or paused
            if (source.RtpClient.FrameChangedEventsEnabled) source.RtpClient.RtpFrameChanged += OnSourceFrameChanged;
            else source.RtpClient.RtpPacketReceieved += OnSourceRtpPacketRecieved;

            //Ensure RtpClient is now connected connected so packets will begin to go out when enqued
            if (!m_RtpClient.Connected) m_RtpClient.Connect();

            //Return the response
            return playResponse;
        }

        void OnSourceFrameChanged(object sender, RtpFrame frame)
        {
            foreach (var packet in frame) OnSourceRtpPacketRecieved(sender, packet);
        }

        /// <summary>
        /// Removes all packets from the PacketBuffer related to the given source and enqueues them on the RtpClient of this ClientSession
        /// </summary>
        /// <param name="source">The RtpSource to check for packets in the PacketBuffer</param>
        internal void ProcessPacketBuffer(RtpSource source)
        {
            //Process packets from the PacketBuffer relevent to the Range Header
            IList<RtpPacket> packets;            

            //Iterate all TransportContext's in the Source
            foreach (RtpClient.TransportContext sourceContext in source.RtpClient.TransportContexts.DefaultIfEmpty())
            {
                if (sourceContext == null) continue;
                //If the PacketBuffer has any packets related remove packets from the PacketBuffer
                if (PacketBuffer.Remove((int)sourceContext.SynchronizationSourceIdentifier, out packets) && packets.Count > 0)
                {
                    //Send them out
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
        internal RtspMessage ProcessSetup(RtspMessage request, RtpSource sourceStream, RtpClient.TransportContext sourceContext)
        {
            //We also have to send one back
            string returnTransportHeader = null;

            //Create a response
            RtspMessage response = CreateRtspResponse(request);

            //Check for already setup stream
            if (Attached.ContainsKey(sourceContext)) return CreateRtspResponse(request, RtspStatusCode.BadRequest, "Stream Already Setup");

            Sdp.MediaDescription mediaDescription = sourceContext.MediaDescription;

            bool rtcpDisabled = sourceStream.m_DisableQOS;

            //Values in the header we need
            int clientRtpPort = -1, clientRtcpPort = -1, serverRtpPort = -1, serverRtcpPort = -1, ssrc = 0;

            //Cache this to prevent having to go to get it every time down the line
            IPAddress sourceIp = IPAddress.Any;

            string mode;

            bool unicast, multicast, interleaved;

            byte dataChannel = 0, controlChannel = 1;

            //Get the transport header
            string transportHeader = request[RtspHeaders.Transport];

            //If that is not present we cannot determine what transport the client wants
            if (string.IsNullOrWhiteSpace(transportHeader) || !(transportHeader.Contains("RTP"))
                || !RtspHeaders.TryParseTransportHeader(transportHeader,
                    out ssrc, out sourceIp, out serverRtpPort, out serverRtcpPort, out clientRtpPort, out clientRtcpPort,
                    out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast))
            {
                return CreateRtspResponse(request, RtspStatusCode.BadRequest);
            }

            //Create a unique 32 bit id if required
            if(ssrc == 0) ssrc = RFC3550.Random32((int)sourceContext.MediaDescription.MediaType);            

            //Could also randomize the setupContext sequenceNumber here.
            //We need to make an TransportContext in response to a setup
            RtpClient.TransportContext setupContext = null;            

            //Should determine intervals here for Rtcp from SessionDescription

            //Check for TCP being forced and then for given udp ports
            if (interleaved) 
            {
                //Check if the client was already created.
                if (m_RtpClient == null)
                {
                    //Create a new RtpClient
                    m_RtpClient = new RtpClient(m_Buffer);

                    m_RtpClient.InterleavedData += m_Server.ProcessRtspInterleaveData;

                    m_RtpClient.FrameChangedEventsEnabled = false;

                    //Create a new Interleave
                    setupContext = new RtpClient.TransportContext((byte)(dataChannel = 0), (byte)(controlChannel = 1), ssrc, mediaDescription, m_RtspSocket, !rtcpDisabled);

                    //Add the transportChannel the client requested
                    m_RtpClient.Add(setupContext);

                    //Initialize the Interleaved Socket
                    setupContext.Initialize(m_RtspSocket);
                }
                else //The client was already created
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                    
                    setupContext = new RtpClient.TransportContext(dataChannel = (byte)(lastContext.DataChannel + 2), controlChannel = (byte)(lastContext.ControlChannel + 2), ssrc, mediaDescription);

                    //Add the transportChannel the client requested
                    m_RtpClient.Add(setupContext);

                    //Initialize the current TransportChannel with the interleaved Socket
                    setupContext.Initialize(m_RtspSocket);
                }

                setupContext.m_SendInterval = TimeSpan.FromMilliseconds(192);
                setupContext.m_ReceiveInterval = TimeSpan.FromMilliseconds(192);
                setupContext.MediaStartTime = sourceContext.MediaStartTime;
                setupContext.MediaEndTime = sourceContext.MediaEndTime;

                //Create the returnTransportHeader
                returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", ssrc, m_Server.LocalEndPoint.Address, null, null, null, null, true, false, null, true, dataChannel, controlChannel);
            }            
            else //Not Interleaved
            {
                if (clientRtpPort == 0) clientRtpPort = Utility.FindOpenPort(ProtocolType.Udp, m_Server.MinimumUdpPort ?? 30000, true);
                
                if (clientRtcpPort == 0) clientRtcpPort = clientRtpPort + 1;

                if (serverRtpPort == 0) serverRtpPort = Utility.FindOpenPort(ProtocolType.Udp, m_Server.MinimumUdpPort ?? 30000, true);

                if (serverRtcpPort == 0) serverRtcpPort = serverRtpPort + 1;

                //Check requested transport is allowed by server
                if (sourceStream.ForceTCP)//The client wanted Udp and Tcp was forced
                {
                    return CreateRtspResponse(request, RtspStatusCode.UnsupportedTransport);
                }
                else if (m_Server.MaximumUdpPort.HasValue && (clientRtpPort > m_Server.MaximumUdpPort || clientRtcpPort > m_Server.MaximumUdpPort))
                {
                    //Handle port out of range
                    return CreateRtspResponse(request, RtspStatusCode.BadRequest, "Requested Udp Ports were out of range. Maximum Port = " + m_Server.MaximumUdpPort);
                }

                //Check if the client was already created.
                if (m_RtpClient == null)
                {
                    //Create a sender
                    m_RtpClient = new RtpClient(m_Buffer);
                    
                    //Use default data and control channel
                    setupContext = new RtpClient.TransportContext(0, 1, ssrc, mediaDescription, !rtcpDisabled);
                }                
                else //The client was already created.
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.TransportContexts.Last();
                    setupContext = new RtpClient.TransportContext((byte)(lastContext.DataChannel + 2), (byte)(lastContext.ControlChannel + 2), ssrc, mediaDescription, !rtcpDisabled);                    
                }

                //Initialize the Udp sockets
                setupContext.Initialize(((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);

                //Add the transportChannel
                m_RtpClient.Add(setupContext);

                //Create the returnTransportHeader
                returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier, ssrc, m_Server.LocalEndPoint.Address, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort, true, false, null, false, 0, 0);
                
            }

            //Synchronize the context sequence numbers
            setupContext.SequenceNumber = sourceContext.SequenceNumber;
            setupContext.m_SendInterval = TimeSpan.FromMilliseconds(192);
            setupContext.m_ReceiveInterval = TimeSpan.FromMilliseconds(192);
            setupContext.MediaStartTime = sourceContext.MediaStartTime;
            setupContext.MediaEndTime = sourceContext.MediaEndTime;

            //Add the new source
            Attached.Add(sourceContext, sourceStream);
        
            //Set the returnTransportHeader to the value above 
            response.SetHeader(RtspHeaders.Transport, returnTransportHeader);

            return response;
        }

        internal RtspMessage ProcessPause(RtspMessage request, RtpSource source)
        {
            //If the source is attached
            if (Attached.ContainsValue(source))
            {
                //Iterate the source transport contexts
                foreach (RtpClient.TransportContext sourceContext in source.RtpClient.TransportContexts)
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
        internal void RemoveSource(SourceStream source)
        {
            if (source is RtpSource)
            {
                RtpSource rtpSource = source as RtpSource;
                if (rtpSource.RtpClient != null)
                {
                    //For each TransportContext in the RtpClient
                    foreach (RtpClient.TransportContext tc in rtpSource.RtpClient.TransportContexts) Attached.Remove(tc);

                    //Attach events
                    //rtpSource.RtpClient.RtcpPacketReceieved -= OnSourceRtcpPacketRecieved;
                    rtpSource.RtpClient.RtpPacketReceieved -= OnSourceRtpPacketRecieved;
                    rtpSource.RtpClient.RtpFrameChanged -= OnSourceFrameChanged;
                }
            }
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

        internal RtspMessage ProcessTeardown(RtspMessage request, RtpSource source)
        {
            //Determine if this is for only a single track or the entire shebang
            if (!Attached.ContainsValue(source)) return CreateRtspResponse(request, RtspStatusCode.BadRequest);

            //Determine if we have the track
            string track = request.Location.Segments.Last().Replace("/", string.Empty);

            Sdp.MediaType mediaType;

            //For a single track
            if (Enum.TryParse <Sdp.MediaType>(track, true, out mediaType))
            {
                //bool GetContextBySdpControlLine... out mediaDescription
                RtpClient.TransportContext sourceContext = Attached.Keys.FirstOrDefault(sc => sc.MediaDescription.MediaType == mediaType);

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

        internal RtspMessage ProcessRecord(RtspMessage request, IMediaStream source)
        {
            //Can't record when no Archiver is present
            if (m_Server.Archiver == null) return CreateRtspResponse(request, RtspStatusCode.PreconditionFailed);

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
        internal RtspMessage CreateRtspResponse(RtspMessage request = null, RtspStatusCode statusCode = RtspStatusCode.OK, string body = null)
        {
            bool inRequest = request != null;

            RtspMessage response = new RtspMessage(RtspMessageType.Response);
            response.StatusCode = statusCode;

            response.CSeq = request != null ? request.CSeq : LastRequest != null ? LastRequest.CSeq : 1;
            if (!string.IsNullOrWhiteSpace(SessionId))
                response.SetHeader(RtspHeaders.Session, SessionId);

            /*
             RFC2326 - Page57
             * 12.38 Timestamp

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
            if (inRequest)
            {
                string timestamp = request.GetHeader(RtspHeaders.Timestamp);
                if (!string.IsNullOrWhiteSpace(timestamp))
                {
                    response.SetHeader(RtspHeaders.Timestamp, timestamp);
                    //Calculate Delay?
                }
            }

            if (!string.IsNullOrWhiteSpace(body)) response.Body = body;

            return response;
        }


        /// <summary>
        /// Dynamically creates a Sdp.SessionDescription for the given SourceStream using the information already present and only re-writing the necessary values.
        /// </summary>
        /// <param name="stream">The source stream to create a SessionDescription for</param>
        /// <returns>The created SessionDescription</returns>
        internal Sdp.SessionDescription CreateSessionDescription(SourceStream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            //else if (SessionDescription != null) throw new NotImplementedException("There is already a m_SessionDescription for this session, updating is not implemented at this time");

            string sessionId = Utility.DateTimeToNptTimestamp(DateTime.UtcNow).ToString(), sessionVersion = Utility.DateTimeToNptTimestamp(DateTime.UtcNow).ToString();

            string originatorString = "ASTI-Media-Server " + sessionId + " " + sessionVersion + " IN " + (m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? "IP6 " : "IP4 " ) + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI Streaming Session"; // + stream.Name 

            Sdp.SessionDescription sdp;

            RtpClient sourceClient;

            if (stream is RtpSource)
            {
                RtpSource rtpSource = stream as RtpSource;
                //Make the new SessionDescription
                sdp = new Sdp.SessionDescription(rtpSource.SessionDescription.ToString());

                sourceClient = rtpSource.RtpClient;
            }
            else sdp = new Sdp.SessionDescription(1);
            sdp.SessionName = sessionName;
            sdp.OriginatorAndSessionIdentifier = originatorString;

            string protcol = "rtsp", controlLineBase = "a=control:" + protcol + "://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address.ToString() + "/live/" + stream.Id;
            //check for rtspu later...

            //Find an existing control line
            Media.Sdp.SessionDescriptionLine controlLine = sdp.ControlLine;

            //If there was one remove it
            while (controlLine != null)
            {
                sdp.RemoveLine(sdp.Lines.IndexOf(controlLine));
                controlLine = sdp.ControlLine;
            }
            
            //Find an existing connection line
            Sdp.Lines.SessionConnectionLine connectionLine = sdp.ConnectionLine as Sdp.Lines.SessionConnectionLine;

            //Remove the old connection line
            if (connectionLine != null) sdp.RemoveLine(sdp.Lines.IndexOf(connectionLine));

            //Rewrite a new connection line
            string addressString = LocalEndPoint.Address.ToString();// +"/127/2";

            Utility.FindOpenPort( stream.m_ForceTCP ? ProtocolType.Tcp : ProtocolType.Udp);

            //Indicate a port in the sdp, setup should also use this port, this should essentially reserve the port for the setup process...
            //if (!stream.m_ForceTCP)
                //addressString += "/127" +'/' +  lastPort + 1;
            //else 
                //addressString += + ((IPEndPoint)RemoteEndPoint).Port;

            connectionLine = new Sdp.Lines.SessionConnectionLine()
            {
                Address = addressString,
                AddressType = m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? "IP6" : "IP4",
                NetworkType = "IN",
            };

            //Add the new line
            sdp.Add(connectionLine, false);

            //Add the information line if not present
            if (sdp.Lines.Where(l => l.Type == 'i').Count() == 0) sdp.Add(new Sdp.SessionDescriptionLine("i=" + stream.Name));

            IEnumerable<Sdp.SessionDescriptionLine> bandwithLines;

            //Indicate the recipient does not need to send
            sdp.Add(new Sdp.SessionDescriptionLine("a=recvonly"));

            //Iterate the source MediaDescriptions, could just create a new one with the fmt which contains the profile level information
            foreach (Sdp.MediaDescription md in sdp.MediaDescriptions)
            {               
                //Find a control line
                controlLine = md.ControlLine;

                //Rewrite it if present to reflect the appropriate MediaDescription
                while (controlLine != null)
                {
                    md.RemoveLine(md.Lines.IndexOf(controlLine));
                    controlLine = md.ControlLine;
                }

                //Remove old bandwith lines
                bandwithLines = md.BandwidthLines;

                //Remove existing bandwidth information, should check for AS
                if(stream.m_DisableQOS) foreach (Sdp.SessionDescriptionLine line in bandwithLines) md.RemoveLine(md.Lines.IndexOf(line));

                //Remove all other alternate information
                foreach (Sdp.SessionDescriptionLine line in md.Lines.Where(l => l.Parts.Any(p => p.Contains("alt"))).ToArray()) md.RemoveLine(md.Lines.IndexOf(line));

                //Add a control line for the MedaiDescription (which is `rtsp://./Id/audio` (video etc)
                md.Add(new Sdp.SessionDescriptionLine("a=control:" + "/live/" + stream.Id + '/' + md.MediaType));

                //Should check for Timing Info and update for playing streams

                if (stream.m_DisableQOS)
                {
                    md.Add(new Sdp.SessionDescriptionLine("b=RS:0"));
                    md.Add(new Sdp.SessionDescriptionLine("b=RR:0"));
                    md.Add(new Sdp.SessionDescriptionLine("b=AS:0"));
                }
                else//Should not be hardcoded
                {
                    md.Add(new Sdp.SessionDescriptionLine("b=RS:192"));
                    md.Add(new Sdp.SessionDescriptionLine("b=RR:192"));

                    md.Add(new Sdp.SessionDescriptionLine("b=AS:0")); //Determine if AS needs to be forwarded
                }

                //Should actually reflect outgoing port
                md.MediaPort = 0;

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
            }

            //Top level stream control line
            sdp.Add(new Sdp.SessionDescriptionLine(controlLineBase));

            //Clients sessionId is created from the Sdp
            SessionId = sessionId;

            return sdp;
        }

        #endregion                       
    }
}
