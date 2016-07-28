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
    internal class ClientSession : Common.SuppressedFinalizerDisposable, Media.Common.ILoggingReference  //ISocketReference....
    {
        #region Statics, Biasi {Not basis} , or `True Time`; `Fabrica` but not `Fabricated`; `begotten` not `made`

        //higher cpu than IsGoodUnlessNullOrDisposed
        static int TryAssessIsBadPacketThreshold(RtpPacket a, RtpClient.TransportContext b, int threshold = Common.Binary.Zero)
        {
            try
            {
                return (int)Deicide(a, b, threshold, Common.Binary.One);
            }
            catch
            {
                return Common.Binary.NegativeOne;
            }
        }

        static double Deicide(RtpPacket a, RtpClient.TransportContext b, int t = 0, int s = 0)
        {
            if (a.Transferred.HasValue)
            {
                TimeSpan da = b.LastRtpPacketSent;

                //After

                if (da.Equals(Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan)) return Common.Binary.Zero;
                else if(a.Transferred.Value.Ticks <= da.Ticks) return Common.Binary.NegativeOne;

                //-- fast path the transfer check, if less than between b.LastRtpPacketSent

                TimeSpan dy = (DateTime.UtcNow - a.Transferred.Value).Duration();

                if (dy <= b.LastRtpPacketSent) return Common.Binary.One;

                //--

                TimeSpan dt = b.LastRtpPacketSent > Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan ? b.LastRtpPacketSent.Subtract(dy).Duration() : b.LastRtpPacketSent;

                //Has the time since the last transfer occured elapsed in a quantity which is greater than or equal to the sendr interval?
                return dt >= b.m_SendInterval ?
                    //does that time in 100z of a nanos minus that of the senders jitter differ from the rtp (receive) jitter in such a way that it is greater or equal?
                    dt.Ticks - b.SenderJitter >= b.RtpJitter ?
                      Common.Binary.NegativeOne : Common.Binary.Zero
                    //if not return {0}, <0>, -|0|
                      : -Common.Binary.Zero;
            }
            else
            {
                int packetSequenceNumber = a.SequenceNumber;

                return packetSequenceNumber - b.SendSequenceNumber + (- t + s);
            }
        }

        ///---

        static int IsBadPacket(Media.Common.IPacket a, RtpClient.TransportContext b)
        {
            if (IsGoodUnlessNullOrDisposed(a, b) <= 0) return Common.Binary.NegativeOne;

            if (a is RtpPacket) return TryAssessIsBadPacketThreshold(a as RtpPacket, b);

            return IsGoodUnlessNullOrDisposed(a, b);
        }

        ///---

        static int IsGoodUnlessNullOrDisposed(Media.Common.IPacket a, RtpClient.TransportContext b)
        {
            return Common.IDisposedExtensions.IsNullOrDisposed(a) || Common.IDisposedExtensions.IsNullOrDisposed(b) ? Common.Binary.NegativeOne : Common.Binary.One;
        }

        ///---
        ///

        #endregion

        //Needs to have it's own concept of range using the Storage...

        //Store authentication related values here also.

        //This logic will still be needed even when there are RtspClient's or TransportClient's in use or otherwise, just a [slight] variation of it.

        #region Fields

        //internal bool HasAuthenticated;

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
        /// Used to assign the function which decides if packets `re good or bad.
        /// </summary>
        internal Func<RtpPacket, RtpClient.TransportContext, int /*double*/> BadPacketDecision = IsBadPacket;

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

        internal bool m_IsDisconnected, m_NeedsReconfiguration;

        //Use the m_RtpClient to determine if Bandwidth is exceeded and Buffer packets until not exceeded.
        //internal double MaximumBandwidth = 0;
        
        internal EndPoint RemoteEndPoint;

        internal int m_SocketPollMicroseconds;

        internal Common.ILogging Logger;

        #endregion

        #region Properties

        internal bool HasRuningServer
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Common.IDisposedExtensions.IsNullOrDisposed(m_Server).Equals(false) && m_Server.IsRunning; }
        }

        public Guid Id
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal set;
        }

        public string SessionId
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal set;
        }

        public RtspMessage LastRequest
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal set;
        }

        public IPEndPoint LocalEndPoint
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return (IPEndPoint)m_RtspSocket.LocalEndPoint;
            }
        }

        public bool IsDisconnected
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_IsDisconnected; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal set { m_IsDisconnected = value; }
        }

        public bool SharesSocket
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
                    if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient) || m_RtpClient.IsActive.Equals(false)) return false;

                    //If the transport is not null and the handle is equal to the rtsp socket's handle
                    if (m_RtpClient.GetTransportContexts().Any(tc=> Common.IDisposedExtensions.IsNullOrDisposed(tc).Equals(false)
                        && //Castclass
                        ((Common.ISocketReference)tc).GetReferencedSockets().Any(s => s.Handle == m_RtspSocket.Handle)))
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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtspSocket; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal set
            {
                m_RtspSocket = value;

                if (object.ReferenceEquals(m_RtspSocket, null).Equals(false)) RemoteEndPoint = m_RtspSocket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Gets or sets a value which indicates if the socket will be closed when Dispose is called.
        /// </summary>
        public bool LeaveOpen
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        #endregion

        //Handlers,, or use RtspClient and handle via OnReceived...

        #region Constructor

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ClientSession(RtspServer server, Socket rtspSocket, Common.MemorySegment buffer = null, bool startReceive = true, int interFrameGap = 0, bool shouldDispose = true)
            :base(shouldDispose)
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

            //If there is no socket
            if (object.ReferenceEquals(m_RtspSocket, null))
            {
                //If receive should start then throw an exception, otherwise return
                if (startReceive) throw new ArgumentNullException("rtspSocket");
                
                return;
            }           

            //Todo, pool all memory in a single contiguous allocation using the server.AllocateClientBuffer
            
            //If the buffer is null or disposed create a new buffer of 4096 bytes.
            if (Common.IDisposedExtensions.IsNullOrDisposed(buffer))
                m_Buffer = new Common.MemorySegment(RtspMessage.MaximumLength);
            else
                m_Buffer = buffer;

            m_SocketPollMicroseconds = interFrameGap;

            //Optional
            //if (m_RtspSocket.ProtocolType == ProtocolType.Tcp) Media.Common.Extensions.Socket.SocketExtensions.ChangeTcpKeepAlive(m_RtspSocket, 0, m_SocketPollMicroseconds);

            //Start receiving data if indicated
            if(startReceive) StartReceive();
        }

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void StartReceive()
        {
            //while the socket cannot read in m_SocketPollMicroseconds or less 
            while (false.Equals(IsDisposed) && 
                false.Equals(IsDisconnected) && 
                HasRuningServer && 
                false.Equals(m_RtspSocket.Poll(m_SocketPollMicroseconds, SelectMode.SelectRead)))
            {
                //Wait for the last recieve to complete
                //Might not need this when not using Async.
                if (object.ReferenceEquals(LastRecieve, null).Equals(false))
                {
                    if (false.Equals(LastRecieve.IsCompleted))
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

            if (object.ReferenceEquals(m_RtspSocket, null).Equals(false) && HasRuningServer) LastRecieve = m_RtspSocket.BeginReceiveFrom(m_Buffer.Array, m_Buffer.Offset, m_Buffer.Count, SocketFlags.None, ref RemoteEndPoint, new AsyncCallback(m_Server.ProcessReceive), this);

        NotDisconnected:
            //Mark as not disconnected.
            IsDisconnected = false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendRtspData(byte[] data)
        {
            SendRtspData(data, 0, data.Length, SocketFlags.None, RemoteEndPoint);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendRtspData(byte[] data, int offset, int length, SocketFlags flags = SocketFlags.None, EndPoint other = null)
        {
            //Check response being sent twice sometimes..

            int check;
            //Check for no data or 0 length when sharing socket.
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(data, out check) ||
                length - offset > check ||
                (length.Equals(Common.Binary.Zero) && SharesSocket)) return;

            try
            {
                //while the socket cannot write in bit time
                while (IsDisconnected.Equals(false) &&
                       IsDisposed.Equals(false) &&
                       HasRuningServer &&
                       m_RtspSocket.Poll(m_SocketPollMicroseconds, SelectMode.SelectWrite).Equals(false)) 
                {
                    ////Wait for the last send to complete
                    if (object.ReferenceEquals(LastSend, null).Equals(false))
                    {
                        if (false.Equals(IsDisconnected) && LastSend.IsCompleted.Equals(false))
                        {
                            WaitHandle wait = LastSend.AsyncWaitHandle;

                            Media.Common.Extensions.WaitHandle.WaitHandleExtensions.TryWaitOnHandleAndDispose(ref wait);
                        }
                        else if (false.Equals(IsDisconnected) && m_RtspSocket.Poll(m_SocketPollMicroseconds, SelectMode.SelectRead))
                        {
                            if(HasRuningServer) StartReceive();

                            WaitHandle wait = LastRecieve.AsyncWaitHandle;

                            Media.Common.Extensions.WaitHandle.WaitHandleExtensions.TryWaitOnHandleAndDispose(ref wait);
                        }
                    }
                }

                //If session is disposed then return
                if (IsDisposed || HasRuningServer.Equals(false)) return;

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
                if (HasRuningServer && IsDisposed.Equals(false))
                {
                    //Log the excetpion
                    Media.Common.ILoggingExtensions.LogException(m_Server.Logger, ex);

                    //if a socket exception occured then handle it.
                    if (ex is SocketException) m_Server.HandleClientSocketException((SocketException)ex, this);

                    //if not disposed mark disconnected
                    IsDisconnected = object.ReferenceEquals(m_RtspSocket, null) || IsDisposed || HasRuningServer.Equals(false);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void AssignSessionId()
        {
            //Assign the sessionId now if it has not been assigned before.
            if (IsDisconnected.Equals(false) && IsDisposed.Equals(false) &&
                string.IsNullOrWhiteSpace(SessionId)) SessionId = m_Id.GetHashCode().ToString();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtpClient.TransportContext GetSourceContext(int ssrc)
        {
            try
            {
                foreach (RtpClient.TransportContext context in Attached.Keys)
                    if (ssrc.Equals(context.RemoteSynchronizationSourceIdentifier)) return context;
            }
            catch (InvalidOperationException)
            {
                return GetSourceContext(ssrc);
            }
            catch { }
            return null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtpClient.TransportContext GetSourceContext(RtpPacket packet)
        {
            try
            {
                foreach (RtpClient.TransportContext context in Attached.Keys)
                    if (packet.SynchronizationSourceIdentifier.Equals(context.RemoteSynchronizationSourceIdentifier)) return context;
            }
            catch (InvalidOperationException)
            {
                return GetSourceContext(packet);
            }
            catch { }
            return null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void OnSourceRtpPacketRecieved(object client, RtpPacket packet = null, RtpClient.TransportContext tc = null)
        {
            //Check if both the packet and our client are null or disposed already.
            if (IsDisconnected  ||
                Common.IDisposedExtensions.IsNullOrDisposed(this) || 
                Common.IDisposedExtensions.IsNullOrDisposed(packet) || 
                Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)) return;

            Thread.BeginCriticalRegion();

            //Get a source context
            RtpClient.TransportContext localContext = null, sourceContext = tc ?? GetSourceContext(packet);

            //Get the sourceContext incase the same payload type was used more then once otherwise fallback to the context for the Payloadtype
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(sourceContext)))
            {                
                localContext = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);
            }

            //Todo, revise PerPacket on the source.

            //If there is not context or the sequence number is unchanged or the value is not within the allowed gap
            //When frame change events are enabled this doesn't matter as the event model takes care of skipping the packets for now.
            if (Common.IDisposedExtensions.IsNullOrDisposed(localContext)) goto Exit;

            //Todo, option RawFlow... do not validate just Enqueue...

            int packetSequenceNumber = packet.SequenceNumber;

            //If this packet was already sent then attempt nothing.
            //if (packetSequenceNumber - localContext.SendSequenceNumber </*=*/ 0) goto Exit;

            if (BadPacketDecision(packet, localContext) < 0) goto Exit;

            //If the packet seqeuence is out of order in reception to the client
            if (m_RtpClient.IsActive && 
                false.Equals(localContext.UpdateSequenceNumber(ref packetSequenceNumber)) && 
                false.Equals(localContext.AllowOutOfOrderPackets))
            {
                //Check how many packets were receieved out of order.
                if (++localContext.m_FailedRtpReceptions > localContext.MinimumSequentialValidRtpPackets)
                {
                    localContext.AllowOutOfOrderPackets = true;
                }
                else
                {

                    //Todo, event on server for client loss...

                    //Todo, event on client for client loss..

                    Common.ILoggingExtensions.Log(m_Server.ClientSessionLogger, "UpdateSequenceNumber Failed -> " + localContext.MediaDescription.MediaType + " , PacketSequenceNumber = " + packet.SequenceNumber + ", SendSequenceNumber = " + localContext.SendSequenceNumber + " RecieveSequenceNumber = " + localContext.RecieveSequenceNumber);

                    //And the packet was not already delivered previously.
                    if (false.Equals(localContext.SendSequenceNumber.Equals(localContext.RecieveSequenceNumber)))
                    {
                        Common.ILoggingExtensions.Log(m_Server.ClientSessionLogger, "Dropping -> " + localContext.MediaDescription.MediaType + " , PacketSequenceNumber = " + packet.SequenceNumber + ", SendSequenceNumber = " + localContext.SendSequenceNumber + " RecieveSequenceNumber = " + localContext.RecieveSequenceNumber);

                        goto Exit;
                    }
                }
            }            


            if (PacketBuffer.ContainsKey(sourceContext.SynchronizationSourceIdentifier))
            {
                PacketBuffer.Add(sourceContext.SynchronizationSourceIdentifier, packet);

                //Todo, check packet buffer size...

            }
            else
            {
                //Send packet on Client Thread, use a new data reference
                //m_RtpClient.EnquePacket(packet.Clone());

                //use the existing packet reference
                m_RtpClient.EnquePacket(packet);

                //Todo, can turn on threading.

                //if (m_RtpClient.OutgoingRtpPacketCount > 100 && false == m_RtpClient.ThreadEvents)
                //{
                //    m_RtpClient.ThreadEvents = true;
                //}else{
                //    m_RtpClient.ThreadEvents = false;
                //}
            }

        Exit:
            Thread.EndCriticalRegion();            

            //Todo, check packet buffer size...

            return;
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void OnSourceRtcpPacketRecieved(object stream, RtcpPacket packet, Rtp.RtpClient.TransportContext tc = null)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet) 
                || 
                Common.IDisposedExtensions.IsNullOrDisposed(tc) 
                || 
                Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)) return;

            bool shouldDispose = packet.ShouldDispose;

            if(shouldDispose) Common.BaseDisposable.SetShouldDispose(packet, false, false);

            //If this is a senders report.
            if (packet.PayloadType == Rtcp.SendersReport.PayloadType)
            {
                RtpClient.TransportContext localContext = m_RtpClient.GetContextForMediaDescription(tc.MediaDescription);

                if (Common.IDisposedExtensions.IsNullOrDisposed(localContext)) return;

                localContext.SenderTransit = tc.RtpTransit;
                localContext.SenderJitter = tc.RtpJitter;

                using (Rtcp.SendersReport sr = new SendersReport(packet, false))
                {
                    //Some senders may disable timestamps by using 0 here
                    localContext.SenderNtpTimestamp = sr.NtpTimestamp;
                    localContext.SenderRtpTimestamp = sr.RtpTimestamp;

                    //Could calulcate NtpOffset from difference here if desired. e.g. NtpOffset is given by 
                    //context.NtpTimestamp -  Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow)

                    //Most senders don't use blocks anyway...
                    if (sr.BlockCount > 0)
                    {
                        Rtcp.IReportBlock reportBlock = sr.FirstOrDefault(rb => rb.BlockIdentifier == tc.RemoteSynchronizationSourceIdentifier);

                        if (object.ReferenceEquals(reportBlock, null).Equals(false))
                        {
                            ReportBlock block = (ReportBlock)reportBlock;

                            //check size of block before accessing properties.

                            //Fast forward...
                            localContext.SendSequenceNumber = block.ExtendedHighestSequenceNumberReceived;

                            //should add to jitter with +=
                            localContext.SenderJitter = (uint)block.InterarrivalJitterEstimate;
                        }
                    }
                }

            }

            if(shouldDispose) Common.BaseDisposable.SetShouldDispose(packet, true, false);

            //Could send reports right now to ensure the clients of this stream gets the time from the source ASAP
            //m_RtpClient.SendReports();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (false.Equals(disposing)) return;

            base.Dispose(ShouldDispose);

            if (false.Equals(IsDisposed)) return;

            RemoveAllAttachmentsAndClearPlaying();

            //Mark as disconnected
            IsDisconnected = true;

            //Deactivate the RtpClient so it's not hanging around wasting resources for nothing
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)))
            {

                m_RtpClient.RtpPacketReceieved -= m_RtpClient_RecievedRtp;

#if DEBUG

                //m_RtpClient.RtcpPacketReceieved -= m_RtpClient_RecievedRtcp;

                //m_RtpClient.RtcpPacketSent -= m_RtpClient_SentRtcp;

                //m_RtpClient.RtpPacketSent -= m_RtpClient_SentRtp;

#endif
                try
                {
                    m_RtpClient.OutOfBandData -= ProcessClientSessionBuffer;

                    m_RtpClient.Dispose();

                    m_RtpClient = null;
                }
                catch { }
            }



            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_Buffer)))
            {
                try
                {
                    m_Buffer.Dispose();

                    m_Buffer = null;
                }
                catch { }
            }

            if (object.ReferenceEquals(m_RtspSocket, null).Equals(false))
            {
                try
                {
                    if (false.Equals(LeaveOpen)) m_RtspSocket.Dispose();

                    m_RtspSocket = null;
                }
                catch { }
            }

            if (object.ReferenceEquals(LastRequest, null).Equals(false))
            {
                try
                {
                    LastRequest.Dispose();

                    LastRequest = null;
                }
                catch { }
            }

            if (object.ReferenceEquals(LastResponse, null).Equals(false))
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtspMessage ProcessDescribe(RtspMessage describeRequest, Media.Rtsp.Server.IMedia source)
        {
            //Assign the session ID now to allow connections to be reduced when possible.
            //AssignSessionId();

            RtspMessage describeResponse = CreateRtspResponse(describeRequest);

            describeResponse.SetHeader(RtspHeaders.ContentType, Sdp.SessionDescription.MimeType);

            //Don't cache this SDP
            describeResponse.SetHeader(RtspHeaders.CacheControl, RtspHeaders.Cache̱Control.NoCache);
            //describeResponse.SetHeader(RtspHeaders.Pragma, "no-cache");

            //If desired you will need a way to determine when the last time the sdp was modified, could use the sessionId from the SDP or its' created property etc.)
            //describeResponse.SetHeader(RtspHeaders.LastModified, 

            //Todo, server.LivePath

            if (describeRequest.Location.ToString().ToLowerInvariant().Contains("live"))
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, "rtsp://" + ((IPEndPoint)m_RtspSocket.LocalEndPoint).ToString() + "/live/" + source.Id + '/');
            }
            else
            {
                describeResponse.SetHeader(RtspHeaders.ContentBase, describeRequest.Location.ToString());
            }

            //Todo, could store it but would cost memory.

            //Todo, have session level prepare request delegate etc...

            //Create a Session Description to describe the media requested
            using (var sessionDescription = CreateSessionDescription(source))
            {
                //Set the body
                describeResponse.Body = sessionDescription.ToString();

                //describeResponse.SetHeader(RtspHeaders.LastModified, DateTime.Now.ToString("r"));

                //Clients sessionId is created from the Sdp's SessionId Line
                //if (string.IsNullOrWhiteSpace(SessionId)) SessionId = sessionDescription.SessionId;
            }

            //Return the resulting message
            return describeResponse;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtspMessage ProcessPlay(RtspMessage playRequest, RtpSource source)
        {

            //Todo, PlayHandler, PauseHandler

            //Determine if the request can be processed.
            bool playAllowed = false;

            //Indicate why play would not be allowed.
            string information = string.Empty;

            //Check the there is an underlying transport which has not already been disposed.
            if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
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
                sourceAvailable = source.RtpClient.GetTransportContexts().Where(sc => Common.IDisposedExtensions.IsNullOrDisposed(GetSourceContext(sc.MediaDescription)).Equals(false));

                //If any context is available then this PLAY request can be honored.
                playAllowed = sourceAvailable.Any();

                //Information 
                information = "No Source Transport. Perform SETUP.";

                //REDIRECT TO SETUP?
            }

            //13.4.16 464 Data Transport Not Ready Yet
            //The data transmission channel to the media destination is not yet ready for carrying data.
            if (playAllowed.Equals(false))                
            {
                return CreateRtspResponse(playRequest, RtspStatusCode.DataTransportNotReadyYet, null, information);
            }

            //bool allowIncomingRtp = false;

            //Attach the packet events if not already attached (E.g. paused)
            if (Playing.Contains(source.Id).Equals(false))
            {
                //Attach events based on how the source will raise them.
                if (source.RtpClient.FrameChangedEventsEnabled)
                {
                    source.RtpClient.RtpFrameChanged += OnSourceFrameChanged;

                    //If you need thread events check now..

                    //could also be done in maintain server

                }
                else
                {
                    source.RtpClient.RtpPacketReceieved += OnSourceRtpPacketRecieved;

                    //If you need thread events check now..

                    //could also be done in maintain server
                }

                //If the source `needs` Rtcp because the source sends data over Rtcp such as PLI etc... pass it through.
                //Note that just because the source sent us a PLI doesn't mean the client needs it...
                //You should probably NEVER pass RTCP through... Time jumping or not...
                if(source.PassthroughRtcp) source.RtpClient.RtcpPacketReceieved += OnSourceRtcpPacketRecieved;

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
            if (string.IsNullOrWhiteSpace(rangeHeader).Equals(false))
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

                    if(end.Equals(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan).Equals(false)
                        &&
                        (end += Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) > max) return CreateRtspResponse(playRequest, RtspStatusCode.InvalidRange, null, "Invalid End Range");

                    //If the given time to start at is > zero
                    if (start > TimeSpan.Zero)
                    {
                        //If the maximum is not infinite and the start exceeds the max indicate this.
                        if (max.Equals(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan).Equals(false)
                            &&
                            start > max) return CreateRtspResponse(playRequest, RtspStatusCode.InvalidRange, null, "Invalid Start Range");
                    }

                    //If the end time is infinite and the max is not infinite then the end is the max time.
                    if (end.Equals(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) && max.Equals(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan).Equals(false)) endRange = end = max;

                    //If the start time is 0 and the end time is not infinite then start the start time to the uptime of the stream (how long it has been playing)
                    if (start.Equals(TimeSpan.Zero) && end.Equals(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan).Equals(false)) startRange = start = source.RtpClient.Uptime;
                    else startRange = null;
                }
            }

            //Todo Process Scale, Speed, Bandwidth, Blocksize

            //Set Seek-Style to indicate if Seeking is supported.

            //Prepare the RtpInfo header
            //Iterate the source's TransportContext's to Augment the RtpInfo header for the current request
            List<string> rtpInfos = new List<string>();

            //rtsp://10.0.57.48/live/4899afda-facf-4332-8cfb-7ff5e79b6d04 
            //Check for bugs... playRequest.Location.Segments.Last()
            string lastSegment = playRequest.Location.Segments.Last();

            Sdp.MediaType mediaType;            

            //If the mediaType was specified there will be /audio or video and that will compare to the lastSegment, 3 previously would be parsed as text etc.
            if (Enum.TryParse(lastSegment, true, out mediaType) && string.Compare(lastSegment, mediaType.ToString(), true).Equals(0))
            {
                var sourceContext = sourceAvailable.FirstOrDefault(tc => tc.MediaDescription.MediaType == mediaType);

                //AggreateOperationNotAllowed?
                if (Common.IDisposedExtensions.IsNullOrDisposed(sourceContext)) return CreateRtspResponse(playRequest, RtspStatusCode.BadRequest, null, "Source Not Setup");

                //Get the context.
                RtpClient.TransportContext context = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                //Copy the sourceContext RtpTimestamp. (Because we sending reports right after this)
                //context.SenderNtpTimestamp = sourceContext.NtpTimestamp;
                //context.SenderRtpTimestamp = sourceContext.RtpTimestamp;

                //Create the RtpInfo header for this context.

                //There should be a better way to get the Uri for the stream
                //E.g. ServerLocation should be used.

                //UriEnecode?

                bool hasAnyState = sourceContext.RtpPacketsReceived > 0 || sourceContext.RtpPacketsSent > 0 && context.InDiscovery.Equals(false);

                //RtpInfoDatum / SubHeader
                rtpInfos.Add(RtspHeaders.RtpInfoHeader(new Uri("rtsp://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).ToString() + "/live/" + source.Id + '/' + context.MediaDescription.MediaType.ToString()),
                    hasAnyState ? sourceContext.RecieveSequenceNumber : (int?)null,
                    hasAnyState ? sourceContext.RtpTimestamp : (int?)null,
                    hasAnyState ? (int?)null : context.SynchronizationSourceIdentifier));

                //Identify now to emulate GStreamer :P
                m_RtpClient.SendSendersReport(context);

                //Done with context.
                context = null;
            }
            else
            {

                //Should check for 'track' segment, possibly should change logic at FindStreamByLocation

                foreach (RtpClient.TransportContext sourceContext in sourceAvailable)
                {
                    var context = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                    if (Common.IDisposedExtensions.IsNullOrDisposed(context)) continue;

                    //Copy the sourceContext RtpTimestamp. (May help with time jumping in some cases...)(Because we sending reports right after this)
                    //context.SenderNtpTimestamp = sourceContext.NtpTimestamp;
                    //context.SenderRtpTimestamp = sourceContext.RtpTimestamp;

                    //Create the RtpInfo header for this context.

                    //UriEnecode?

                    //There should be a better way to get the Uri for the stream
                    //E.g. ServerLocation should be used.
                    bool hasAnyState = sourceContext.RtpPacketsReceived > 0 || sourceContext.RtpPacketsSent > 0 && context.InDiscovery.Equals(false);

                    rtpInfos.Add(RtspHeaders.RtpInfoHeader(new Uri("rtsp://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address + "/live/" + source.Id + '/' + context.MediaDescription.MediaType.ToString()),
                        hasAnyState ? sourceContext.RecieveSequenceNumber : (int?)null,
                        hasAnyState ? sourceContext.RtpTimestamp : (int?)null,
                        hasAnyState ? (int?)null : context.SynchronizationSourceIdentifier));

                    //Done with context.
                    context = null;
                }

                //Send all reports now, ala GStreamer
                m_RtpClient.SendSendersReports();
            }          

            //Indicate the range of the play response. (`Range` will be 'now-' if no start or end was given)
            playResponse.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startRange, endRange));

            //Set the rtpInfo
            playResponse.SetHeader(RtspHeaders.RtpInfo, string.Join(", ", rtpInfos.ToArray()));


            //Todo
            //Set the MediaProperties header.

            //Ensure RtpClient is now connected connected so packets will begin to go out when enqued
            if (false.Equals(m_RtpClient.IsActive))
            {
                m_RtpClient.Activate();
                
                //m_RtpClient.m_WorkerThread.Priority = ThreadPriority.Highest;

                //Could have taken frames from sourceContext and used them here...

                //LastFrame, CurrentFrame etc.
            }

            //Return the response
            return playResponse;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void OnSourceFrameChanged(object sender, RtpFrame frame = null, RtpClient.TransportContext tc = null, bool final = false)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(frame)) return;

            //SkipIncompleteFrame option?

            //Wait for the last possible moment to send frames.
            if (false.Equals(final)) return;

            //Loop and observe changes each iteration
            for (int i = 0; i < frame.Count; ++i) OnSourceRtpPacketRecieved(sender, frame[i], tc);

            //foreach (var packet in frame)
            //{
            //    //if (packet.Transferred.HasValue) continue;

            //    OnSourceRtpPacketRecieved(sender, packet, tc);

            //    //packet.Transferred = DateTime.UtcNow;
            //}
        }

        /// <summary>
        /// Removes all packets from the PacketBuffer related to the given source and enqueues them on the RtpClient of this ClientSession
        /// </summary>
        /// <param name="source">The RtpSource to check for packets in the PacketBuffer</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
                    m_RtpClient.m_OutgoingRtpPackets.AddRange(packets.SkipWhile(rtp => rtp.SequenceNumber < sourceContext.RecieveSequenceNumber));
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtspMessage ProcessSetup(RtspMessage request, RtpSource sourceStream, RtpClient.TransportContext sourceContext)
        {
            //Assign the sessionId now if it has not been assigned before.
            AssignSessionId();

            /*
             C.1.8 Entity Tag	
 
    	     The optional "a=etag" attribute identifies a version of the session	
 	         description. It is opaque to the client. SETUP requests may include	
             this identifier in the If-Match field (see section 12.22) to only	
 	         allow session establishment if this attribute value still corresponds	
 	         to that of the current description. The attribute value is opaque and	
             may contain any character allowed within SDP attribute values.	
 
 
                  a=etag:158bb3e7c7fd62ce67f12b533f06b83a	
       	 	     One could argue that the "o=" field provides identical	
    
    	     functionality. However, it does so in a manner that would put	
    	     constraints on servers that need to support multiple session	
    	     description types other than SDP for the same piece of media	
             
             */


            //Todo, There may be more then 128 streams setup under a single connection in TCP, in such cases respond with an error because interleaved TCP does not support this
            //That would be unless independent TCP transport is being used or UDP
            if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient).Equals(false)
                && m_RtpClient.IsActive)
            {
                //SharesSocket would be a better indication.

                var interleavedConexts = m_RtpClient.GetTransportContexts().Where(t => t.RtpSocket.ProtocolType == ProtocolType.Tcp && 
                    t.RtcpSocket.ProtocolType == ProtocolType.Tcp &&
                    t.MediaDescription.ConnectionLine.m_Parts.Any(p => p.IndexOf("TCP") >= 0).Equals(false)); //Exclude independent connections

                //SetupHandler=>

                //RtpClient.Extensions.GetAvailableInterleavedChannels(socket)

                //a 256 byte array of channels in use would only be so useful as scanning it would take time.

                HashSet<byte> unique = new HashSet<byte>();

                foreach (var tc in interleavedConexts)
                {
                    unique.Add(tc.DataChannel);

                    unique.Add(tc.ControlChannel);
                }

                
                if (unique.Count >= byte.MaxValue)
                {
                    //Return error
                    return CreateRtspResponse(request, RtspStatusCode.BadRequest, "InterleavedChannels", "All channels are currently in use, please open a new connection for interleaved transport. The channels in use are included below:\t\r\n" + string.Join(Environment.NewLine, unique));
                }
                else
                {
                    //If there are less than 255 channels then one must be open...

                    //Find first open pair...

                    int openChannels = unique.Distinct().Count() - byte.MaxValue;

                    if (openChannels < 1)
                    {
                        return CreateRtspResponse(request, RtspStatusCode.BadRequest, "InterleavedChannels", "Please use an existing channel which is not in the list included below:\t\r\n" + string.Join(Environment.NewLine, unique));
                    }
                }

            }

            //We also have to send one back
            string returnTransportHeader = null;           

            //Create a response
            RtspMessage response = CreateRtspResponse(request);

            Sdp.MediaDescription mediaDescription = sourceContext.MediaDescription;

            bool rtcpDisabled = sourceStream.m_DisableQOS;

            //Values in the header we need
            int clientRtpPort = -1, clientRtcpPort = -1, serverRtpPort = -1, serverRtcpPort = -1, localSsrc = 0, remoteSsrc = 0;

            //Cache this to prevent having to go to get it every time down the line
            IPAddress sourceIp = IPAddress.Any, destinationIp = sourceIp;

            string mode;

            bool unicast, multicast, interleaved, multiplexing;

            byte dataChannel = 0, controlChannel = 1;

            int ttl;

            //Get the transport header
            string transportHeader = request[RtspHeaders.Transport];

            //If that is not present we cannot determine what transport the client wants
            if (string.IsNullOrWhiteSpace(transportHeader) || 
                false.Equals((transportHeader.Contains("RTP"))) ||
                false.Equals(RtspHeaders.TryParseTransportHeader(transportHeader,
                    out localSsrc, out sourceIp, out serverRtpPort, out serverRtcpPort, out clientRtpPort, out clientRtcpPort,
                    out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast, out destinationIp, out ttl)))
            {
                return CreateRtspResponse(request, RtspStatusCode.BadRequest, null, "Invalid Transport Header");
            }

            //RTCP-mux: when RTSP 2.0 is official... (Along with Server Sent Messages)

            //Todo, destination datum support.

            //Todo, option for UniqueIdentity

            //Check if the ssrc was 0 which indicates any id
            if (localSsrc == 0)
            {
                //use a new id if your using IListSockets as the stack can merge the buffers into a single pdu.
                //Or
                //If the remote ssrc needs to be changed then use a random one
                //localSsrc = RFC3550.Random32((int)sourceContext.MediaDescription.MediaType);

                //Use the same id to keep the packet headers the same.
                localSsrc = sourceContext.RemoteSynchronizationSourceIdentifier;

                //If localSsrc is still 0 then the identity needs to be unique on the channel.
            }

            //Could also randomize the setupContext sequenceNumber here.
            //We need to make an TransportContext in response to a setup
            RtpClient.TransportContext setupContext = null;

            //Check for an existing ssrc
            //The ssrc is in use already...
            if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient).Equals(false) &&                
                Common.IDisposedExtensions.IsNullOrDisposed(setupContext = m_RtpClient.GetContextBySourceId(localSsrc)).Equals(false) &&
                setupContext.InDiscovery.Equals(false))
            {
                //SocketEndPoint... for Protocol

                return CreateRtspResponse(request, RtspStatusCode.BadRequest, null, "Ssrc already in use. @ " + setupContext.RemoteSynchronizationSourceIdentifier + "/" + setupContext.SynchronizationSourceIdentifier
                    + "," + 
                    (setupContext.IsRtcpEnabled ? ((IPEndPoint)(setupContext.LocalRtp)).ToString() + ((IPEndPoint)(setupContext.RemoteRtp)).ToString() : string.Empty)
                    + "-" +
                    (setupContext.IsRtcpEnabled ? ((IPEndPoint)(setupContext.LocalRtcp)).ToString() + ((IPEndPoint)(setupContext.RemoteRtcp)).ToString() : string.Empty));
            }

             //Check for already setup stream and determine if the stream needs to be setup again or just updated
            if (Attached.ContainsKey(sourceContext))
            {
                //The contex may already existm should look first by ssrc or name.
                setupContext = m_RtpClient.GetContextForMediaDescription(sourceContext.MediaDescription);

                //If the context exists
                if (Common.IDisposedExtensions.IsNullOrDisposed(setupContext).Equals(false))
                {
                    //Todo, should requiure auth, allow the ssrc to change from remote
                    ////Update the ssrc  if it doesn't match.
                    //if (localSsrc != 0 && setupContext.SynchronizationSourceIdentifier != localSsrc)
                    //{
                    //    setupContext.SynchronizationSourceIdentifier = localSsrc;

                    //    if (remoteSsrc != 0 && setupContext.RemoteSynchronizationSourceIdentifier != remoteSsrc) setupContext.RemoteSynchronizationSourceIdentifier = remoteSsrc;
                    //}

                    multicast = Media.Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(((IPEndPoint)setupContext.RemoteRtp).Address);

                    interleaved = setupContext.RtpSocket.ProtocolType == ProtocolType.Tcp && SharesSocket;

                    //Then indicate the information for that context in the return transport header.
                    returnTransportHeader = RtspHeaders.TransportHeader(setupContext.MediaDescription.MediaProtocol, setupContext.SynchronizationSourceIdentifier, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, ((IPEndPoint)setupContext.RemoteRtp).Port, ((IPEndPoint)setupContext.RemoteRtcp).Port, ((IPEndPoint)setupContext.LocalRtp).Port, ((IPEndPoint)setupContext.LocalRtcp).Port, false == multicast, multicast, null, interleaved, setupContext.DataChannel, setupContext.ControlChannel);

                    setupContext.LeaveOpen = interleaved;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.ClientSessionLogger;

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
            if (interleaved.Equals(false) && (unicast || multicast)) 
            {

                //Check requested transport is allowed by server
                if (sourceStream.ForceTCP)//The client wanted Udp and Tcp was forced
                {
                    //Return the result
                    RtspMessage result = CreateRtspResponse(request, RtspStatusCode.UnsupportedTransport);

                    //Indicate interleaved is required.
                    result.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, null, null, null, null, null, false, null, true, dataChannel, controlChannel));

                    return result;
                }

                //QuickTime debug

                if (clientRtpPort.Equals(0)) clientRtpPort = Media.Common.Extensions.Socket.SocketExtensions.ProbeForOpenPort(ProtocolType.Udp, 30000, true);

                if (clientRtcpPort.Equals(0)) clientRtcpPort = clientRtpPort + 1;

                if (serverRtpPort.Equals(0)) serverRtpPort = Media.Common.Extensions.Socket.SocketExtensions.ProbeForOpenPort(ProtocolType.Udp, 30000, true);

                if (serverRtcpPort.Equals(0)) serverRtcpPort = serverRtpPort + 1;

                //Ensure the ports are allowed to be used.
                if (m_Server.MaximumUdpPort.HasValue && 
                    (clientRtpPort > m_Server.MaximumUdpPort || clientRtcpPort > m_Server.MaximumUdpPort))
                {
                    //Handle port out of range
                    return CreateRtspResponse(request, RtspStatusCode.BadRequest, null, "Requested Udp Ports were out of range. Maximum Port = " + m_Server.MaximumUdpPort);
                }

                //Create sockets to reserve the ports.

                IPAddress localAddress = ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address;

                Socket tempRtp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, localAddress, clientRtpPort);

                Socket tempRtcp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, localAddress, clientRtcpPort);

                //Check if the client was already created.
                if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                {
                    //Create a sender using a new segment on the existing buffer.
                    m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                    //Dont handle frame changed events from the client
                    m_RtpClient.FrameChangedEventsEnabled = false;

                    //Dont handle packets from the client
                    m_RtpClient.HandleIncomingRtpPackets = false;

                    //Attach the Interleaved data event
                    m_RtpClient.OutOfBandData += ProcessClientSessionBuffer;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.ClientSessionLogger;

                    //Use default data and control channel (Should be option?)
                    setupContext = new RtpClient.TransportContext(0, 1, localSsrc, mediaDescription, false == rtcpDisabled, remoteSsrc, 0);
                }
                else //The client was already created.
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().LastOrDefault();

                    if (Common.IDisposedExtensions.IsNullOrDisposed(lastContext).Equals(false)) setupContext = new RtpClient.TransportContext((byte)(lastContext.DataChannel + 2), (byte)(lastContext.ControlChannel + 2), localSsrc, mediaDescription, rtcpDisabled.Equals(false), remoteSsrc, 0);
                    else setupContext = new RtpClient.TransportContext(dataChannel, controlChannel, localSsrc, mediaDescription, rtcpDisabled.Equals(false), remoteSsrc, 0);
                }

                //Todo, allow for other memory, this is already shared via the RtpClient ...

                //set the memory for the context
                setupContext.ContextMemory = m_Buffer;

                //Initialize the Udp sockets
                setupContext.Initialize(localAddress, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, serverRtpPort, serverRtcpPort, clientRtpPort, clientRtcpPort);

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

                if (false.Equals(m_RtspSocket.ProtocolType == ProtocolType.Tcp))
                {
                    //Handle (maybe add Location header with tcp:// or rtspt://)
                    return CreateRtspResponse(request, RtspStatusCode.Found, "TransportProtocol", "Interleaved Transport requires a new connection to this host.");
                }

                //Check if the client was already created.
                if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                {
                    //Create a sender using a new segment on the existing buffer.
                    m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                    m_RtpClient.OutOfBandData += ProcessClientSessionBuffer;

                    //Attach logger (have option?)
                    m_RtpClient.Logger = m_Server.ClientSessionLogger;

                    m_RtpClient.RtpPacketReceieved += m_RtpClient_RecievedRtp;

#if DEBUG

                    //m_RtpClient.RtcpPacketReceieved += m_RtpClient_RecievedRtcp;

                    //m_RtpClient.RtcpPacketSent += m_RtpClient_SentRtcp;

                    //m_RtpClient.RtpPacketSent += m_RtpClient_SentRtp;

#endif

                    //Dont handle frame changed events from the client
                    m_RtpClient.FrameChangedEventsEnabled = false;

                    //Dont handle packets from the client
                    m_RtpClient.HandleIncomingRtpPackets = false;

                    //Create a new Interleave (don't use what was given as data or control channels)
                    setupContext = new RtpClient.TransportContext((byte)(dataChannel = 0), 
                        (byte)(controlChannel = 1), 
                        localSsrc, 
                        mediaDescription, 
                        m_RtspSocket, 
                        rtcpDisabled.Equals(false), 
                        remoteSsrc, 0);

                    //Initialize the Interleaved Socket
                    setupContext.Initialize(m_RtspSocket, m_RtspSocket);
                }
                else //The client was already created
                {
                    //Have to calculate next data and control channel
                    RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().LastOrDefault();

                    //Don't use what was given as data or control channels
                    if (Common.IDisposedExtensions.IsNullOrDisposed(lastContext).Equals(false)) setupContext = new RtpClient.TransportContext(dataChannel = (byte)(lastContext.DataChannel + 2), 
                        controlChannel = (byte)(lastContext.ControlChannel + 2), 
                        localSsrc, 
                        mediaDescription, 
                        rtcpDisabled.Equals(false), 
                        remoteSsrc, 
                        0);
                    else setupContext = new RtpClient.TransportContext(dataChannel, 
                        controlChannel, 
                        localSsrc, 
                        mediaDescription, 
                        rtcpDisabled.Equals(false), 
                        remoteSsrc, 
                        0);

                    //Initialize the current TransportChannel with the interleaved Socket
                    setupContext.Initialize(m_RtspSocket, m_RtspSocket);
                }

                if (Common.IDisposedExtensions.IsNullOrDisposed(setupContext).Equals(false))
                {
                    //Add the transportChannel the client requested
                    if (m_RtpClient.TryAddContext(setupContext).Equals(false))
                    {
                        //Channel or ssrc is in use...
                    }

                    //Create the returnTransportHeader
                    //returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", setupContext.SynchronizationSourceIdentifier, ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, LocalEndPoint.Port, LocalEndPoint.Port, ((IPEndPoint)RemoteEndPoint).Port, ((IPEndPoint)RemoteEndPoint).Port, true, false, null, true, dataChannel, controlChannel);

                    //Leave the socket open when disposing the RtpClient
                    setupContext.LeaveOpen = m_RtspSocket.Handle == setupContext.RtpSocket.Handle || m_RtspSocket.Handle == setupContext.RtcpSocket.Handle;
                }

                returnTransportHeader = RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", 
                    localSsrc, 
                    ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, 
                    null, null, null, null, null, 
                    false, 
                    null, 
                    true, 
                    dataChannel, controlChannel);
            }
        
            //Add the new source
            Attached.Add(sourceContext, sourceStream);
        
        UpdateContext:

            //Synchronize the context sequence numbers
            setupContext.RecieveSequenceNumber = setupContext.SendSequenceNumber = sourceContext.RecieveSequenceNumber;
            setupContext.RtpTimestamp = setupContext.SenderRtpTimestamp = sourceContext.RtpTimestamp;

            //Start and end times are always equal.
            setupContext.MediaStartTime = sourceContext.MediaStartTime;
            setupContext.MediaEndTime = sourceContext.MediaEndTime;           

            //Set the returnTransportHeader to the value above 
            response.SetHeader(RtspHeaders.Transport, returnTransportHeader);

            //Done in SendRtspResponse.
            //Give the sessionid for the transport setup
            //response.SetHeader(RtspHeaders.Session, SessionId);

            //Todo, see if this can be calulcated based on requirements etc.
            //Set the amount of packets which are allowed to be queued, if greater than this amount threading is turned on.
            //m_RtpClient.MaximumOutgoingPackets = 1000;

            //Activate now.
            //m_RtpClient.Activate();

            return response;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void ProcessClientSessionBuffer(object sender, byte[] data, int offset, int length)
        {
            //Process the data received
            if(false.Equals(IsDisconnected)) m_Server.ProcessClientBuffer(this, length);

            //Handle high usage when client disconnects.
            if (length.Equals(0) && m_RtpClient.IsActive)
            {
                //should also check activity on each context to properly determine :)

                Common.ILoggingExtensions.Log(m_Server.ClientSessionLogger, "0 Byte packet with Active RtpClient, Marking IsDisconnected = true");

                IsDisconnected = true;

                m_RtpClient.Deactivate();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void m_RtpClient_RecievedRtp(object sender, RtpPacket packet, RtpClient.TransportContext tc = null)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet) || Common.IDisposedExtensions.IsNullOrDisposed(tc)) return;
            
            //Allow the context to keep track of the packets received
            ++tc.RtpPacketsReceived;

            Common.ILoggingExtensions.Log(m_Server.ClientSessionLogger, "Session Recieved Rtp PacketType: " + packet.PayloadType + " - " + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier);

            //if (packet == null || packet.IsDisposed) return;
            //else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Recieved Rtp PacketType: " + packet.PayloadType + " - " + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            //var context = tc ?? m_RtpClient.GetContextForPacket(packet);

            //if (context == null) Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Unknown Rtp Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            //else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Rtp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Recieved PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void m_RtpClient_RecievedRtcp(object sender, RtcpPacket packet, RtpClient.TransportContext tc = null)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //Get an implementation for the packet recieved
            var implementation = Rtcp.RtcpPacket.GetImplementationForPayloadType((byte)packet.PayloadType);

            if (object.ReferenceEquals(implementation, null)) Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Recieved Unknown PacketType: " + packet.PayloadType + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Recieved Rtcp PacketType: " + packet.PayloadType + " - " + implementation.Name + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            var context = tc ?? m_RtpClient.GetContextForPacket(packet);

            if (Common.IDisposedExtensions.IsNullOrDisposed(context)) m_Server.Logger.LogException(new Exception("Unknown Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Recieved Rtcp PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void m_RtpClient_SentRtcp(object sender, RtcpPacket packet, RtpClient.TransportContext tc = null)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //Get an implementation for the packet recieved
            var implementation = Rtcp.RtcpPacket.GetImplementationForPayloadType((byte)packet.PayloadType);

            if (object.ReferenceEquals(implementation, null)) Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent Unknown PacketType: " + packet.PayloadType + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent Rtcp PacketType: " + packet.PayloadType + " - " + implementation.Name + " Packet Ssrc = " + packet.SynchronizationSourceIdentifier));

            //If the context should have been synchronized then determine if a context can be found
            if (m_RtpClient.Uptime > RtpClient.DefaultReportInterval)
            {
                var context = tc ?? m_RtpClient.GetContextForPacket(packet);

                if (Common.IDisposedExtensions.IsNullOrDisposed(context)) Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent Unknown Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
                else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent Rtcp Packet Ssrc = " + packet.SynchronizationSourceIdentifier + " RemoteId = " + context.RemoteSynchronizationSourceIdentifier + " LocalId = " + context.SynchronizationSourceIdentifier));
            }

            //Crash... check bugs in compiler.. emited wrong instruction...
            //m_Server.Logger.LogException(new Exception("Sent Rtcp PacketType: " + packet.PayloadType + " - " + implementation != null ? implementation.Name : string.Empty));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void m_RtpClient_SentRtp(object sender, RtpPacket packet, RtpClient.TransportContext tc = null)
        {
            var context = tc ?? m_RtpClient.GetContextForPacket(packet);

            if (Common.IDisposedExtensions.IsNullOrDisposed(context)) Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent Unknown Packet Ssrc = " + packet.SynchronizationSourceIdentifier));
            else Common.ILoggingExtensions.LogException(m_Server.ClientSessionLogger, new Exception("Sent (" + context.MediaDescription.MediaType + ") Rtp Packet Timestamp = " + packet.Timestamp + " SequenceNumber = " + packet.SequenceNumber + " , SenderSequenceNumber" + context.SendSequenceNumber + " , RecieveSequenceNumber" + context.RecieveSequenceNumber));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtspMessage ProcessPause(RtspMessage request, RtpSource source)
        {
            //Aggregate control..

            //Todo, PlayHandler, PauseHandler

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void RemoveSource(Media.Rtsp.Server.IMediaSource source)
        {
            if (source is RtpSource)
            {
                RtpSource rtpSource = source as RtpSource;
                if (Common.IDisposedExtensions.IsNullOrDisposed(rtpSource.RtpClient).Equals(false))
                {
                    //For each TransportContext in the RtpClient
                    foreach (RtpClient.TransportContext tc in rtpSource.RtpClient.GetTransportContexts()) Attached.Remove(tc);

                    //Detach events
                    rtpSource.RtpClient.RtcpPacketReceieved -= OnSourceRtcpPacketRecieved;
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void RemoveMedia(Sdp.MediaDescription md)
        {
            //Determine if we have a source which corresponds to the mediaDescription given
            RtpClient.TransportContext sourceContext = Attached.Keys.FirstOrDefault(c => c.MediaDescription.Equals(md));

            //If the sourceContext is not null
            if (Common.IDisposedExtensions.IsNullOrDisposed(sourceContext).Equals(false))
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal RtspMessage ProcessTeardown(RtspMessage request, RtpSource source)
        {
            //Determine if this is for only a single track or the entire shebang
            if (false == Attached.ContainsValue(source)) return CreateRtspResponse(request, RtspStatusCode.BadRequest);

            //Determine if we have the track
            string track = request.Location.Segments.Last().Replace("/", string.Empty);

            int symbolIndex = 0;

            //Todo, may contain any complaint string...

            //Check for `=`

            if ((symbolIndex = track.IndexOf((char)Common.ASCII.EqualsSign)) >= 0)
            {
                track = track.Substring(symbolIndex);

                //The track variable now contains a number or string...

                //GetSourceContext()

            }
            else
            {
                 Sdp.MediaType mediaType;

                //For a single track
                if (Enum.TryParse <Sdp.MediaType>(track, true, out mediaType))
                {
                    //bool GetContextBySdpControlLine... out mediaDescription
                    RtpClient.TransportContext sourceContext = source.RtpClient.GetTransportContexts().FirstOrDefault(sc => sc.MediaDescription.MediaType == mediaType);

                    //Cannot teardown media because we can't find the track they are asking to tear down
                    if (Common.IDisposedExtensions.IsNullOrDisposed(sourceContext))
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
            }

            //Return the response
            return CreateRtspResponse(request);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool ReleaseUnusedResources()
        {
            if (IsDisposed) return false;

            bool released = false;

            RtpClient.TransportContext sourceContext;

            //Todo, Determine to use m_Server.ClientSessionLogger, or m_Server.Logger

            //Enumerate each context 'SETUP' in the session
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)))
            {
                IList<RtpClient.TransportContext> contexts = m_RtpClient.TransportContexts;

                //Iterate each context in the client
                for (int i = 0; i < contexts.Count; ++i)
                {
                    //Scope the context
                    RtpClient.TransportContext context = contexts[i];

                    if (Common.IDisposedExtensions.IsNullOrDisposed(context)) continue;

                    //Could be a property of the transport.. (especially the rtpclient.)
                    //E.g. IsInactive

                    //If the context does not have any activity
                    if (false.Equals(context.HasAnyRecentActivity) && m_RtpClient.Uptime >= context.m_ReceiveInterval.Add(context.m_SendInterval))
                    {
                        //Session level logger
                        //Context has no activity

                        Common.ILoggingExtensions.Log(m_Server.Logger, "Session Inactive - " + SessionId);

                        //Sources still attached cause higher usage.

                        //See if there is still a source for the context 
                        sourceContext = GetSourceContext(context.MediaDescription);

                        //Todo, if source is no active it should probably be removed.

                        //If there was a source context AND the source has activity
                        if (Common.IDisposedExtensions.IsNullOrDisposed(sourceContext).Equals(false) && false.Equals(sourceContext.IsActive) /*|| false.Equals(sourceContext.HasAnyRecentActivity)*/)
                        {
                            //Get the attached source
                            Media.Rtsp.Server.SourceMedia sourceMedia;

                            //if there is a source still attached
                            if (Attached.TryGetValue(sourceContext, out sourceMedia))
                            {
                                //If the source is not disposed
                                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(sourceMedia)))
                                {
                                    //Removed Attachment for sourceContext.Id
                                    Common.ILoggingExtensions.Log(m_Server.Logger, "Session Source Inactive, Removing SourceMedia = " + sourceMedia.Id);

                                    //Remove the attachment from the source context to the session context
                                    RemoveSource(sourceMedia);

                                    //Remove the reference to the sourceContext
                                    sourceContext = null;

                                    //Remove the referqence to the sourceMedia
                                    sourceMedia = null;

                                    released = true;
                                }
                            }
                        }

                        //If the context was removed
                        if (m_RtpClient.TryRemoveContext(context))
                        {
                            //Dispose the context and indicate in release
                            context.Dispose();

                            released = true;
                        }

                        if (m_RtpClient.TransportContexts.Count.Equals(0))
                        {
                            m_RtpClient.Deactivate();

                            released = true;
                        }

                    }
                    else //The context has activity, check the source (Will be checked below)
                    {
                        //See if there is still a source for the context 
                        sourceContext = GetSourceContext(context.MediaDescription);

                        //Todo, if source is no active it should probably be removed.

                        //If there was a source context AND the source has activity
                        if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(sourceContext)) && sourceContext.IsActive && false.Equals(sourceContext.HasAnyRecentActivity))
                        {
                            //Get the attached source
                            Media.Rtsp.Server.SourceMedia sourceMedia;

                            //if there is a source still attached
                            if (Attached.TryGetValue(sourceContext, out sourceMedia))
                            {
                                //If the source is not disposed
                                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(sourceMedia)))
                                {
                                    //Removed Attachment for sourceContext.Id
                                    Common.ILoggingExtensions.Log(m_Server.Logger, "Session Source Inactive, Removing SourceMedia = " + sourceMedia.Id);

                                    //Remove the attachment from the source context to the session context
                                    RemoveSource(sourceMedia);

                                    //Remove the reference to the sourceContext
                                    sourceContext = null;

                                    //Remove the reference to the sourceMedia
                                    sourceMedia = null;

                                    //If the context was removed
                                    if (m_RtpClient.TryRemoveContext(context))
                                    {
                                        //Dispose the context and indicate in release
                                        context.Dispose();

                                        released = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            released = false;
                        }
                    }
                }

                contexts = null;
            }

            //Not needed because the source may not be attached just yet
            //try
            //{
            //    //Check all remaining attachments
            //    foreach (var attachment in Attached)
            //    {
            //        var source = attachment.Value;

            //        var localContext = attachment.Key;

            //        //Use the transportContext MediaDescription to obtain the sourceContext
            //        var sourceContext = GetSourceContext(localContext.MediaDescription);

            //        //If the sourceContext is null
            //        if (sourceContext == null || false == sourceContext.HasAnyRecentActivity)
            //        {
            //            //Removed Attachment for sourceContext.Id
            //            Common.ILoggingExtensions.Log(m_Server.Logger, "Session " + (sourceContext == null ? "source is null" :  "sourceContext has no recent activity") +", Removing SourceMedia = " + source.Id);

            //            //Remove the attachment from the source context to the session context
            //            RemoveSource(source);

            //            //Dispose the context and indicate in release
            //            localContext.Dispose();

            //            localContext = null;

            //            released = true;
            //        }

            //        //Remove refs
            //        localContext = sourceContext = null;

            //        source = null;
            //    }
            //}
            //catch
            //{
            //    //
            //}


            //Remove rtp theads
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)) && m_RtpClient.IsActive)
            {
                if (Playing.Count.Equals(0))
                {
                    released = true;

                    m_RtpClient.Dispose();
                    
                    m_RtpClient = null;
                }
            }

            return released;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        /// 
        //todo, should not allow body or should also allow content-type
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

            if (Common.IDisposedExtensions.IsNullOrDisposed(request).Equals(false))
            {
                if (request.ContainsHeader(RtspHeaders.Session)) response.SetHeader(RtspHeaders.Session, request.GetHeader(RtspHeaders.Session));

                if (false.Equals(statusCode == RtspStatusCode.BadRequest)) response.CSeq = request.CSeq;
            }//Request is null, check the statusCode, if not BadRequest check for a LastRequest and use that CSeq.
            else if (false.Equals(statusCode == RtspStatusCode.BadRequest) && 
                false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(LastRequest)) && 
                LastRequest.CSeq >= 0) response.CSeq = LastRequest.CSeq;
            //Otherwise no CSeq is provided in response...

            //Include any reason phrase.
            if (false.Equals(string.IsNullOrWhiteSpace(reasonPhrase))) response.ReasonPhrase = reasonPhrase;

            //Include any body if provided and the response is allowed to have a body.
            if (false.Equals(string.IsNullOrWhiteSpace(body)) && response.CanHaveBody) response.Body = body;

            //return the response.
            return response;
        }


        /// <summary>
        /// Dynamically creates a Sdp.SessionDescription for the given SourceStream using the information already present and only re-writing the necessary values.
        /// </summary>
        /// <param name="stream">The source stream to create a SessionDescription for</param>
        /// <returns>The created SessionDescription</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal Sdp.SessionDescription CreateSessionDescription(Media.Rtsp.Server.IMedia stream)
        {
            //Todo, NullOrDisposedException,
            if (Common.IDisposedExtensions.IsNullOrDisposed(stream)) throw new Media.Common.Extensions.Exception.ExceptionExtensions.ArgumentNullOrDisposedException("stream", stream);
            //else if (SessionDescription != null) throw new NotImplementedException("There is already a m_SessionDescription for this session, updating is not implemented at this time");

            string addressString = ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionId = Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow).ToString(), sessionVersion = Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow).ToString();

            string originatorString = "ASTI-Media-Server " + sessionId + " " + sessionVersion + " IN " + (m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? "IP6 " : "IP4 " ) + addressString;

            string sessionName = "ASTI-Streaming-Session " + stream.Name;

            Sdp.SessionDescription sdp;

            RtpClient sourceClient;

            bool disableQos = false;

            if (stream is RtpSource)
            {
                RtpSource rtpSource = stream as RtpSource;
                //Make the new SessionDescription
                sdp = new Sdp.SessionDescription(rtpSource.SessionDescription.ToString());

                //Remove the old connection lines if they exist
                while(sdp.Remove(sdp.ConnectionLine, false)) ;

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

            //Todo, protocol should be determined from server and request.
            string protcol = LastRequest.Location.Scheme,      
                controlLineBase = "a=control:" + protcol + "://" + addressString + "/live/" + stream.Id;

            //Find an existing control line
            Media.Sdp.SessionDescriptionLine controlLine;

            //If there was one remove it
            while (sdp.Remove(controlLine = sdp.ControlLine)) ;

            //Cannot set, becaue will be under modification
            sdp.ControlLine = new Sdp.SessionDescriptionLine("a=control:*");
            //sdp.Add(new Sdp.SessionDescriptionLine("a=control:*"));

            //Todo, check if HasMultipleAddrsses in the connectionLine should be changed...

            //Determine if session level control line should be present
            
            //Rewrite a new connection line
            //string addressString = LocalEndPoint.Address.ToString();// +"/127/2";

            //int lastPort = Utility.FindOpenPort( stream.m_ForceTCP ? ProtocolType.Tcp : ProtocolType.Udp);

            //Indicate a port in the sdp, setup should also use this port, this should essentially reserve the port for the setup process...
            //if (!stream.m_ForceTCP)
                //addressString += "/127" +'/' +  lastPort + 1;
            //else 
                //addressString += + ((IPEndPoint)RemoteEndPoint).Port;
            
            //Check for the existing connectionLine
            Sdp.Lines.SessionConnectionLine connectionLine = sdp.ConnectionLine as Sdp.Lines.SessionConnectionLine;

            //Add the new line if needed

            if (object.ReferenceEquals(connectionLine, null)) sdp.ConnectionLine = connectionLine = new Sdp.Lines.SessionConnectionLine()
            {
                ConnectionAddress = addressString,
                ConnectionAddressType = m_RtspSocket.AddressFamily == AddressFamily.InterNetworkV6 ? Media.Sdp.Lines.SessionConnectionLine.IP6 : Media.Sdp.Lines.SessionConnectionLine.IP4,
                ConnectionNetworkType = Media.Sdp.Lines.SessionConnectionLine.InConnectionToken,
                //TimeToLive = 255,
                //NumberOfAddresses = 1

            };

            IEnumerable<Sdp.SessionDescriptionLine> bandwithLines;

            //Indicate that the server will not accept media as input for this session
            //Put the attribute in the Session Description,
            //Should check that its not already set?
            //sdp.Add(new Sdp.SessionDescriptionLine("a=recvonly"));

            //Remove any existing session range lines, don't update the version
            //while (sdp.Remove(sdp.RangeLine, false)) ;

            //Todo add a Range line which shows the length of this media.
            //use the UpTime to caulcate the range...
            //needs RangeLine impl to be OOP

            //Add the sesison control line
            //sdp.Add(new Sdp.SessionDescriptionLine(controlLineBase));

            int trackId = 1;

            //Iterate the source MediaDescriptions, could just create a new one with the fmt which contains the profile level information
            foreach (Sdp.MediaDescription md in sdp.MediaDescriptions)
            {               
                //Find a control line
                //Rewrite it if present to reflect the appropriate MediaDescription
                while (md.Remove(controlLine = md.ControlLine))
                {
                    controlLine = md.ControlLine;
                }

                //Remove old bandwith lines
                bandwithLines = md.BandwidthLines;

                //Remove existing bandwidth information, should check for AS
                if(disableQos) foreach (Sdp.SessionDescriptionLine line in bandwithLines) md.Remove(line);

                //Remove all other alternate information
                //Should probably only remove certain ones.
                foreach (Sdp.SessionDescriptionLine line in md.Lines.Where(l => l.Parts.Any(p => p.Contains("alt")))) md.Remove(line);

                //Add a control line for the MedaiDescription (which is `rtsp://./Id/audio` (video etc)
                //Should be a TrackId and not the media type to allow more then one media type to be controlled.
                
                //e.g. Two audio streams or text streams is valid but is ambigious because /audio would not be specific to one or the other
                //md.Add(new Sdp.SessionDescriptionLine(controlLineBase + "/" + md.MediaType));

                //trackId= is not required... I am just musing around with the format other libs expect
                md.Add(new Sdp.SessionDescriptionLine("a=control:trackID=" + trackId++));

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

                //    md.Add(new Sdp.SessionDescriptionLine("b=AS:0")); //Determine if AS etc needs to be forwarded or handled
                //}

                //Should actually reflect outgoing port for this session
                //At this point the media is probably not yet setup...
                md.MediaPort = 0;

                //Determine if attached and set the MediaPort.
                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)))
                {
                    RtpClient.TransportContext context = m_RtpClient.GetContextForMediaDescription(md);

                    //If there exists a context which is null or not disposed then provide the port from that context
                    if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(context)))
                    {
                        //The port field of the media description is set to the remote rtp port.
                        md.MediaPort = ((IPEndPoint)context.RemoteRtp).Port;

                        //Set any other variables which may be been changed in the session

                        //OnDescriptionCreation

                        context = null;
                    }
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

        #region ILoggingReference

        bool Common.ILoggingReference.TrySetLogger(Common.ILogging logger)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(this)) return false;

            Logger = logger;

            return true;
        }

        bool Common.ILoggingReference.TryGetLogger(out Common.ILogging logger)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(this))
            {
                logger = null;

                return false;
            }

            logger = Logger;

            return true;
        }

        #endregion
    }
}
