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
using Media.Common;
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
    public class RtspClient : Common.BaseDisposable, Media.Common.ISocketReference
    {

        //Todo use SocketConfiguration
        /// <summary>
        /// Handle the configuration required for the given socket
        /// </summary>
        /// <param name="socket"></param>
        internal static void ConfigureRtspSocket(Socket socket)
        {
            if (socket == null) throw new ArgumentNullException("Socket");

            Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(socket);
            //socket.ExclusiveAddressUse = false;
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            //It was reported that Mono on iOS has a bug with SendBufferSize, ReceiveBufferSize and by looking further possibly SetSocketOption in general...
            //Mono goes through too much trouble to verify socket options and should probably just pass them along to the native layer.
            //SendBufferSize,ReceiveBufferSize and SetSocketOption is supposedly fixed in the latest versions but still do too much option verification...

            //Don't buffer send.
            Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.SendBufferSize = 0);

            //Don't buffer receive.
            Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.ReceiveBufferSize = 0);

            //Dont fragment
            if (socket.AddressFamily == AddressFamily.InterNetwork) Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.DontFragment = true);

            //Rtsp over Tcp
            if (socket.ProtocolType == ProtocolType.Tcp)
            {
                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpNoSynRetries(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpTimestamp(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.SetTcpOffloadPreference(socket);

                //
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpCongestionAlgorithm(socket);

                // Set option that allows socket to close gracefully without lingering.
                Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.DisableLinger(socket));

                //Retransmit for 0 sec
                if (Common.Extensions.OperatingSystemExtensions.IsWindows) Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.DisableTcpRetransmissions(socket));

                //https://en.wikipedia.org/wiki/TCP_delayed_acknowledgment

                //If both send and receieve buffer size are 0 then there is no coalescing when nagle's algorithm is disabled
                Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.DisableTcpNagelAlgorithm(socket));
                //socket.NoDelay = true;

                //Allow more than one byte of urgent data
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpExpedited(socket);
                //socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);

                //Receive any urgent data in the normal data stream
                //Media.Common.Extensions.Socket.SocketExtensions.EnableTcpOutOfBandDataInLine(socket);
            }
        }

        public const int DefaultBufferSize = RtspMessage.MaximumLength * 2;

        public const double DefaultProtocolVersion = 1.0;

        public static readonly TimeSpan DefaultConnectionTime = TimeSpan.FromMilliseconds(500);

        public static readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromSeconds(60);

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

        #region Internal Read Only

        internal readonly Guid InternalId = Guid.NewGuid();

        internal readonly ManualResetEventSlim m_InterleaveEvent;

        internal readonly List<MediaDescription> m_Playing = new List<MediaDescription>(); //Each entry should have it's own start time? Created property?

        //Really needs to be Connection or session will also need to refer to a connection
        internal readonly Dictionary<string, RtspSession> m_Sessions = new Dictionary<string, RtspSession>();

        #endregion

        #region Private

        ClientProtocolType m_RtspProtocol;

        RtspMessage m_LastTransmitted;

        AuthenticationSchemes m_AuthenticationScheme;

        string m_AuthorizationHeader; //could also just store the parts or use a StringCollection

        //Todo
        //readonly List<Uri> m_History = new List<Uri>();

        /// <summary>
        /// The current location the media
        /// </summary>
        Uri m_InitialLocation, m_PreviousLocation, m_CurrentLocation;

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

        /// <summary>
        /// Keep track of timed values.
        /// </summary>
        TimeSpan m_RtspSessionTimeout = DefaultSessionTimeout,
            m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan,
            m_LastServerDelay = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan,
            //Appendix G.  Requirements for Unreliable Transport of RTSP
            m_LastMessageRoundTripTime = DefaultConnectionTime;

        /// <summary>
        /// Keep track of certain values.
        /// </summary>
        int m_SentBytes, m_ReceivedBytes,
             m_RtspPort,
             m_CSeq, m_RCSeq, //-1 values, rtsp 2. indicates to start at 0...
             m_SentMessages, m_ReTransmits,
             m_ReceivedMessages,
             m_PushedMessages,
             m_MaximumTransactionAttempts,
             m_SocketPollMicroseconds;

        //Todo, Two timers? should use a single thread instead....
        Timer m_KeepAliveTimer, m_ProtocolMonitor;

        DateTime? m_BeginConnect, m_EndConnect, m_StartedPlaying;

        //Todo,
        //List<Sdp.MediaDescription> For playing and paused. Could use a List<Tuple<TimeSpan, MediaDescription>>> to allow the timeline when pausing etc..

        NetworkCredential m_Credential;

        #endregion

        #region Internal Private

        internal string m_UserAgent = "ASTI RTSP Client", m_SessionId = string.Empty;//, m_TransportMode;

        //Should not be tied to RTP, needs to be another interface which has Activate, etc RtspTransport might be a possible design, would required all Transports to be registered

        internal RtpClient m_RtpClient;

        #endregion

        #region Public

        /// <summary>
        /// As given by the OPTIONS response or set otherwise.
        /// </summary>
        public readonly HashSet<string> SupportedFeatures = new HashSet<string>();

        /// <summary>
        /// Values which will be set in the Required tag.
        /// </summary>
        public readonly HashSet<string> RequiredFeatures = new HashSet<string>();

        /// <summary>
        /// Any additional headers which may be required by the RtspClient.
        /// </summary>
        public readonly Dictionary<string, string> AdditionalHeaders = new Dictionary<string, string>();

        /// <summary>
        /// Gets the methods supported by the server recieved in the options request.
        /// </summary>
        public readonly HashSet<string> SupportedMethods = new HashSet<string>();

        //Todo, should be property with protected set.

        /// <summary>
        /// A ILogging instance
        /// </summary>
        public Common.ILogging Logger;

        #endregion

        #endregion

        #region Properties

        #region Automatically Implemented

        /// <summary>
        /// Gets or sets a value indicating of the RtspSocket should be left open when Disposing.
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// The version of Rtsp the client will utilize in messages
        /// </summary>
        public double ProtocolVersion { get; set; }

        /// <summary>
        /// Indicates if the <see cref="StartedPlaying"/> property will be set as a result of handling the Play event.
        /// </summary>
        public bool HandlePlayEvent { get; set; }

        /// <summary>
        /// Indicates if the <see cref="StartedPlaying"/> will not have a value as a result of handling the Stop event.
        /// </summary>
        public bool HandleStopEvent { get; set; }

        /// <summary>
        /// Allows the order of media to be determined when <see cref="StartPlaying"/>  is called
        /// </summary>
        public Action<IEnumerable<MediaDescription>> SetupOrder { get; set; }

        //Todo make SocketConfiguration

        /// <summary>
        /// Gets or Sets the method which is called when the <see cref="RtspSocket"/> is created, 
        /// typically during the call to <see cref="Connect"/>
        /// By default <see cref="ConfigureRtspSocket"/> is utilized.
        /// </summary>
        public Action<Socket> ConfigureSocket { get; set; }

        /// <summary>
        /// Indicates if the client will try to automatically reconnect during send or receive operations.
        /// </summary>
        public bool AutomaticallyReconnect { get; set; }

        /// <summary>
        /// Indicates if the client will automatically disconnect the RtspSocket after StartPlaying is called.
        /// </summary>
        public bool AutomaticallyDisconnect { get; set; }

        /// <summary>
        /// Indicates if the client will send a <see cref="KeepAliveRequest"/> during <see cref="StartPlaying"/> if no data is flowing immediately after the PLAY response is recieved.
        /// </summary>
        public bool SendKeepAliveImmediatelyAfterStartPlaying { get; set; }

        /// <summary>
        /// Indicates if the client will add the Timestamp header to outgoing requests.
        /// </summary>
        public bool TimestampRequests { get; set; }

        /// <summary>
        /// Indicates if the client will use the Timestamp header to incoming responses.
        /// </summary>
        public bool CalculateServerDelay { get; set; }

        /// <summary>
        /// Indicates if the client will send the Blocksize header during the SETUP request.
        /// The value of which will reflect the <see cref="Buffer.Count"/>
        /// </summary>
        public bool SendBlocksize { get; set; }

        /// <summary>
        /// Indicates if the Date header should be sent during requests.
        /// </summary>
        public bool DateRequests { get; set; }

        /// <summary>
        /// Indicates if the RtspClient will send the UserAgent header.
        /// </summary>
        public bool SendUserAgent { get; set; }

        //Maybe AllowHostChange
        //public bool IgnoreRedirectOrFound { get; set; }

        /// <summary>
        /// Indicates if the client will take any `X-` headers and use them in future requests.
        /// </summary>
        public bool EchoXHeaders { get; set; }

        /// <summary>
        /// Indicates if the client will process messages which are pushed during the session.
        /// </summary>
        public bool IgnoreServerSentMessages { get; set; }

        /// <summary>
        /// Indicates if Keep Alive Requests will be sent
        /// </summary>
        public bool DisableKeepAliveRequest { get; set; }

        /// <summary>
        /// Gets or Sets a value which indicates if the client will attempt an alternate style of connection if one cannot be established successfully.
        /// Usually only useful under UDP when NAT prevents RTP packets from reaching a client, it will then attempt TCP or HTTP transport.
        /// </summary>
        public bool AllowAlternateTransport { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets or sets the maximum amount of microseconds the <see cref="RtspSocket"/> will wait before performing an operations.
        /// </summary>
        public int SocketPollMicroseconds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SocketPollMicroseconds; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_SocketPollMicroseconds = value; }
        }

        /// <summary>
        /// Gets the remote <see cref="EndPoint"/>
        /// </summary>
        public EndPoint RemoteEndpoint
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RemoteRtsp; }
        }

        /// <summary>
        /// Indicates if the RtspClient is currently sending or receiving data.
        /// </summary>
        public bool InUse
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return false == m_InterleaveEvent.IsSet && false == m_InterleaveEvent.Wait(ConnectionTime); //m_InterleaveEvent.Wait(1);
            }
        }

        /// <summary>
        /// Gets or Sets the socket used for communication
        /// </summary>
        internal protected Socket RtspSocket
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtspSocket; }
            set
            {
                m_RtspSocket = value;

                //Ensure not connected if the socket is removed
                if (m_RtspSocket == null)
                {
                    m_BeginConnect = m_EndConnect = null;

                    m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

                    return;
                }

                m_SocketPollMicroseconds = (int)Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(m_RtspSocket));

                //If the socket is connected
                if (m_RtspSocket.Connected)
                {
                    //SO_CONNECT_TIME only exists on Windows...
                    //There are options if the stack supports it elsewhere.

                    //Set default values to indicate connected

                    m_BeginConnect = m_EndConnect = DateTime.UtcNow;

                    m_ConnectionTime = TimeSpan.Zero;

                    //Use the remote information from the existing socket rather than the location.

                    m_RemoteRtsp = m_RtspSocket.RemoteEndPoint;

                    if (m_RemoteRtsp is IPEndPoint)
                    {
                        IPEndPoint remote = (IPEndPoint)m_RemoteRtsp;

                        m_RemoteIP = remote.Address;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or Sets the buffer used for data reception
        /// </summary>
        internal protected Common.MemorySegment Buffer
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Buffer; }
            set { m_Buffer = value; }
        }

        /// <summary>
        /// Indicates if the RtspClient shares the <see cref="RtspSocket"/> with the underlying Transport.
        /// </summary>
        public bool SharesSocket
        {
            get
            {
                //The socket is shared with the GC
                if (IsDisposed) return true;

                // A null or disposed client or one which is no longer connected cannot share the socket
                if (Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient) || false == m_RtpClient.IsActive) return false;

                //The socket is shared if there is a context using the same socket
                var context = m_RtpClient.GetContextBySocket(m_RtspSocket);

                return false == Common.IDisposedExtensions.IsNullOrDisposed(context) && context.IsActive && context.HasAnyRecentActivity;
            }
        }

        /// <summary>
        /// Indicates the amount of messages which were transmitted more then one time.
        /// </summary>
        public int RetransmittedMessages
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ReTransmits; }
        }

        /// <summary>
        /// Indicates if the client has tried to Authenticate using the current <see cref="Credential"/>'s
        /// </summary>
        public bool TriedCredentials
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == string.IsNullOrWhiteSpace(m_AuthorizationHeader); }
        }        

        /// <summary>
        /// The amount of <see cref="RtspMessage"/>'s sent by this instance.
        /// </summary>
        public int MessagesSent
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SentMessages; }
        }

        /// <summary>
        /// The amount of <see cref="RtspMessage"/>'s receieved by this instance.
        /// </summary>
        public int MessagesReceived
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ReceivedMessages; }
        }

        /// <summary>
        /// The amount of messages pushed by the remote party
        /// </summary>
        public int MessagesPushed
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_PushedMessages; }
        }

        /// <summary>
        /// The amount of time taken to connect to the remote party.
        /// </summary>
        public TimeSpan ConnectionTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ConnectionTime; }
        }

        /// <summary>
        /// The amount of time taken since the response was received to the last <see cref="RtspMessage"/> sent.
        /// </summary>
        public TimeSpan LastMessageRoundTripTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_LastMessageRoundTripTime; }
        }

        /// <summary>
        /// If indicated by the remote party the value of the 'delay' header from the Timestamp header.
        /// </summary>
        public TimeSpan LastServerDelay
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_LastServerDelay; }
        }

        /// <summary>
        /// Indicates if the client has been assigned a <see cref="SessionId"/>
        /// </summary>
        public bool HasSession
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Sessions.Count() > 0; }
        }// false == string.IsNullOrWhiteSpace(m_SessionId)

        /// <summary>
        /// Gets the value of the Session header as it was seen in a response.
        /// When set will override any existing Session header previously seen.
        /// </summary>
        public string SessionId
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SessionId; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_SessionId = value; }
        }

        /// <summary>
        /// Any SessionId's received in a response.
        /// </summary>
        public IEnumerable<string> SessionIds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Sessions.Keys; }
        }

        //Determine if Start and EndTime are worth having?

        /// <summary>
        /// If playing, the TimeSpan which represents the time this media started playing from.
        /// </summary>
        public TimeSpan? StartTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Common.IDisposedExtensions.IsNullOrDisposed(Client) ? null : (TimeSpan?)Client.TransportContexts.Max(tc => tc.MediaStartTime);
            }
        }

        /// <summary>
        /// If playing, the TimeSpan which represents the time the media will end.
        /// </summary>
        public TimeSpan? EndTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Common.IDisposedExtensions.IsNullOrDisposed(Client) ? null : (TimeSpan?)Client.TransportContexts.Max(tc => tc.MediaEndTime);
            }
        }

        //Remaining?

        /// <summary>
        /// If playing, indicates if the RtspClient is playing from a live source which means there is no absolute start or end time and seeking may not be supported.
        /// </summary>
        public bool LivePlay
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return EndTime == Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan; }
        }

        /// <summary>
        /// Indicates if there is any media being played by the RtspClient at the current time.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                //If started playing
                if (m_Playing.Count > 0 && m_StartedPlaying.HasValue)
                {
                    //Try to determine playing status from the transport
                    try
                    {
                        //If not playing anymore do nothing
                        if (EndTime != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan &&
                            DateTime.UtcNow - m_StartedPlaying.Value > EndTime)
                        {
                            return false;
                        }

                        //return true;

                        //If the media is playing the RtspClient is only playing if the socket is shared or the Transport is connected.
                        return SharesSocket || m_RtpClient.IsActive;
                    }
                    catch (Exception ex)
                    {
                        Media.Common.ILoggingExtensions.Log(Logger, ToString() + "@IsPlaying - " + ex.Message);
                    }
                }

                //The RtspClient is not playing
                return false;
            }
        }

        /// <summary>
        /// The DateTime in which the client started playing if playing, otherwise null.
        /// </summary>
        public DateTime? StartedPlaying
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_StartedPlaying; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected set
            {
                m_StartedPlaying = value;
            }
        }        

        /// <summary>
        /// The amount of time in seconds the KeepAlive request will be sent to the server after connected.
        /// If a GET_PARAMETER request is not supports OPTIONS will be sent instead.
        /// </summary>
        public TimeSpan RtspSessionTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtspSessionTimeout; }
            set
            {
                m_RtspSessionTimeout = value;

                if (m_RtspSessionTimeout <= TimeSpan.Zero)
                {
                    //Don't send a request to keep the connection alive
                    DisableKeepAliveRequest = true;

                    if (m_KeepAliveTimer != null) m_KeepAliveTimer.Dispose();

                    m_KeepAliveTimer = null;
                }

                //This is probably wrong, the time should be relative to all requests and not just the last...
                if (m_KeepAliveTimer != null) m_KeepAliveTimer.Change(m_LastTransmitted != null && m_LastTransmitted.Transferred.HasValue ? (m_RtspSessionTimeout - (DateTime.UtcNow - m_LastTransmitted.Created)) : m_RtspSessionTimeout, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Gets or Sets amount the fraction of time the client will wait during a responses for a response without blocking.
        /// If less than or equal to 0 the value 1 will be used.
        /// </summary>
        public int ResponseTimeoutInterval
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MaximumTransactionAttempts; }
            set { m_MaximumTransactionAttempts = Binary.Clamp(value, 1, int.MaxValue); }
        }

        //The last RtspMessage received by the RtspClient from the remote EndPoint.
        public RtspMessage LastTransmitted
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_LastTransmitted; }
        }

        /// <summary>
        /// The ClientProtocolType the RtspClient is using Reliable (Tcp), Unreliable(Udp) or Http(Tcp)
        /// </summary>
        public ClientProtocolType RtspProtocol
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtspProtocol; }
        }

        //Should not be tied to RTP

        /// <summary>
        /// The ProtocolType the RtspClient will setup for underlying RtpClient.
        /// </summary>
        public ProtocolType RtpProtocol
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtpProtocol; }
        }

        /// <summary>
        /// Gets or sets the current location to the Media on the Rtsp Server and updates Remote information and ClientProtocol if required by the change.
        /// If the RtspClient was listening then it will be stopped and started again
        /// </summary>
        public Uri CurrentLocation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_CurrentLocation; }
            set
            {
                try
                {
                    //If Different
                    if (m_CurrentLocation != value)
                    {

                        if (m_InitialLocation == null) m_InitialLocation = value;

                        //Backup the current location, (needs history list?)
                        m_PreviousLocation = m_CurrentLocation;

                        bool wasPlaying = IsPlaying;

                        if (wasPlaying) StopPlaying();

                        m_CurrentLocation = value;

                        switch (m_CurrentLocation.HostNameType)
                        {
                            case UriHostNameType.IPv4:
                            case UriHostNameType.IPv6:

                                m_RemoteIP = IPAddress.Parse(m_CurrentLocation.DnsSafeHost);

                                break;
                            case UriHostNameType.Dns:

                                if (m_RtspSocket != null)
                                {

                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault(a => a.AddressFamily == m_RtspSocket.AddressFamily);

                                    if (m_RemoteIP == null) throw new NotSupportedException("The given Location uses a HostNameType which is not the same as the underlying socket's address family. " + m_CurrentLocation.HostNameType + ", " + m_RtspSocket.AddressFamily + " And as a result no remote IP could be obtained to complete the connection.");
                                }
                                else
                                {
                                    //Will use IPv6 by default if possible.
                                    m_RemoteIP = System.Net.Dns.GetHostAddresses(m_CurrentLocation.DnsSafeHost).FirstOrDefault();
                                }

                                break;

                            default: throw new NotSupportedException("The given Location uses a HostNameType which is not supported. " + m_CurrentLocation.HostNameType);
                        }

                        m_RtspPort = m_CurrentLocation.Port;

                        //Validate ports, should throw? should also use default port for scheme
                        if (m_RtspPort <= ushort.MinValue || m_RtspPort > ushort.MaxValue) m_RtspPort = RtspMessage.ReliableTransportDefaultPort;

                        //Determine protocol
                        if (m_CurrentLocation.Scheme == RtspMessage.ReliableTransportScheme) m_RtspProtocol = ClientProtocolType.Tcp;
                        else if (m_CurrentLocation.Scheme == RtspMessage.UnreliableTransportScheme) m_RtspProtocol = ClientProtocolType.Udp;
                        else m_RtspProtocol = ClientProtocolType.Http;

                        //Make a IPEndPoint 
                        m_RemoteRtsp = new IPEndPoint(m_RemoteIP, m_RtspPort);

                        //Should take into account current time with StartTime?
                        if (wasPlaying) StartPlaying();
                    }
                }
                catch (Exception ex)
                {
                    Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Could not resolve host from the given location. See InnerException.", ex);

                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the Uri which was used first with this instance.
        /// </summary>
        public Uri InitialLocation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_InitialLocation; }
        }

        /// <summary>
        /// Gets the Uri which was used directly before the <see cref="CurrentLocation"/> with this instance.
        /// </summary>
        public Uri PreviousLocation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_PreviousLocation; }
        }

        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        /// <notes>May want to do a partial receive for 1 byte which would take longer but indicate if truly connected. Udp may not be Connected.</notes>
        public bool IsConnected
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == IsDisposed && m_ConnectionTime >= TimeSpan.Zero && m_RtspSocket != null /*&& m_RtspSocket.Connected*/; }
        }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Credential; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                m_Credential = value;

                m_AuthorizationHeader = null;
            }
        }

        /// <summary>
        /// The type of AuthenticationScheme to utilize in RtspRequests, if this is not set then the Credential will not send until it has been determined from a Not Authroized response.
        /// </summary>
        public AuthenticationSchemes AuthenticationScheme
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_AuthenticationScheme; }
            set
            {
                if (value == m_AuthenticationScheme) return;

                if (value != AuthenticationSchemes.Basic && value != AuthenticationSchemes.Digest && value != AuthenticationSchemes.None) throw new System.InvalidOperationException("Only None, Basic and Digest are supported");

                m_AuthenticationScheme = value;

                m_AuthorizationHeader = null;
            }
        }

        /// <summary>
        /// The amount of bytes sent by the RtspClient
        /// </summary>
        public int BytesSent
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SentBytes; }
        }

        /// <summary>
        /// The amount of bytes recieved by the RtspClient
        /// </summary>
        public int BytesRecieved
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ReceivedBytes; }
        }

        /// <summary>
        /// The current SequenceNumber of the RtspClient
        /// </summary>
        public int ClientSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_CSeq; }
        }

        /// <summary>
        /// The current SequenceNumber of the remote RTSP party
        /// </summary>
        public int RemoteSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RCSeq; }
        }

        /// <summary>
        /// Gets the <see cref="MediaDescription"/>'s which pertain to media which is currently playing.
        /// </summary>
        public IEnumerable<MediaDescription> PlayingMedia
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Playing.AsEnumerable(); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="SessionDescription"/> describing the media at <see cref="CurrentLocation"/>.
        /// </summary>
        public SessionDescription SessionDescription
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SessionDescription; }
            set
            {
                //if (value == null) throw new ArgumentNullException("The SessionDescription cannot be null.");
                m_SessionDescription = value;
            }
        }

        /// <summary>
        /// The RtpClient associated with this RtspClient
        /// </summary>
        public RtpClient Client
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RtpClient; }
        }

        /// <summary>
        /// Gets or Sets the ReadTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketReadTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return IsDisposed || m_RtspSocket == null ? -1 : m_RtspSocket.ReceiveTimeout; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { if (IsDisposed || m_RtspSocket == null) return; m_RtspSocket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets or Sets the WriteTimeout of the underlying NetworkStream / Socket (msec)
        /// </summary>
        public int SocketWriteTimeout
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return IsDisposed || m_RtspSocket == null ? -1 : m_RtspSocket.SendTimeout; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { if (IsDisposed || m_RtspSocket == null) return; m_RtspSocket.SendTimeout = value; }
        }

        /// <summary>
        /// The UserAgent sent with every RtspRequest
        /// </summary>
        public string UserAgent
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_UserAgent; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("UserAgent cannot consist of only null or whitespace."); m_UserAgent = value; }
        }

        #endregion

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The absolute location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize and will not deviate from the protocol is no data is received, if null it will be determined from the location Scheme</param>
        /// <param name="existing">An existing Socket</param>
        /// <param name="leaveOpen"><see cref="LeaveOpen"/></param>
        public RtspClient(Uri location, ClientProtocolType? rtpProtocolType = null, int bufferSize = DefaultBufferSize, Socket existing = null, bool leaveOpen = false, int maximumTransactionAttempts = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            if (location == null) throw new ArgumentNullException("location");

            if (false == location.IsAbsoluteUri)
            {
                if (existing == null) throw new ArgumentException("Must be absolute unless a socket is given", "location");
                if (existing.Connected) location = Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.ToUri(((IPEndPoint)existing.RemoteEndPoint), (existing.ProtocolType == ProtocolType.Udp ? RtspMessage.UnreliableTransportScheme : RtspMessage.ReliableTransportScheme));
                else if (existing.IsBound) location = Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.ToUri(((IPEndPoint)existing.LocalEndPoint), (existing.ProtocolType == ProtocolType.Udp ? RtspMessage.UnreliableTransportScheme : RtspMessage.ReliableTransportScheme));
                else throw new InvalidOperationException("location must be specified when existing socket must be connected or bound.");
            }

            //Check the Scheme
            if (false == location.Scheme.StartsWith(RtspMessage.MessageIdentifier, StringComparison.InvariantCultureIgnoreCase)
                &&
               false == location.Scheme.StartsWith(System.Uri.UriSchemeHttp, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Uri Scheme must start with rtsp or http", "location");

            //Set the location and determines the m_RtspProtocol and IP Protocol.
            CurrentLocation = location;

            //If the client has specified a Protcol to use then use it
            if (rtpProtocolType.HasValue)
            {
                //Determine if this means anything for Rtp Transport and set the field
                if (rtpProtocolType.Value == ClientProtocolType.Tcp || rtpProtocolType.Value == ClientProtocolType.Http)
                {
                    m_RtpProtocol = ProtocolType.Tcp;
                }
                else if (rtpProtocolType.Value == ClientProtocolType.Udp)
                {
                    m_RtpProtocol = ProtocolType.Udp;
                }
                else throw new ArgumentException("Must be Tcp or Udp.", "protocolType");
            }

            //If there is an existing socket
            if (existing != null)
            {
                //Use it
                RtspSocket = existing;
            }

            //If no socket is given a new socket will be created

            //Check for a bufferSize of specified - unspecified value
            //Cases of anything less than or equal to 0 mean use the existing ReceiveBufferSize if possible.
            if (bufferSize <= 0) bufferSize = m_RtspSocket != null ? m_RtspSocket.ReceiveBufferSize : 0;

            //Create the segment given the amount of memory required if possible
            if (bufferSize > 0) m_Buffer = new Common.MemorySegment(bufferSize);
            else m_Buffer = new Common.MemorySegment(DefaultBufferSize); //Use 8192 bytes

            //If leave open is set the socket will not be disposed.
            LeaveOpen = leaveOpen;

            //Set the protocol version to use in requests.
            ProtocolVersion = DefaultProtocolVersion;

            //Could create a RtpClient to prevent accidental errors, (would be easier for attaching logger)

            m_RtpClient = new RtpClient(m_Buffer);

            m_RtpClient.InterleavedData += ProcessInterleaveData;

            ConfigureSocket = ConfigureRtspSocket;

            HandlePlayEvent = HandleStopEvent = true;

            m_MaximumTransactionAttempts = maximumTransactionAttempts;            

            m_InterleaveEvent = new ManualResetEventSlim(true, m_MaximumTransactionAttempts);
        }

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        /// <param name="rtpProtocolType">The type of protocol the underlying RtpClient will utilize, if null it will be determined from the location Scheme</param>
        /// <param name="bufferSize">The amount of bytes the client will use during message reception, Must be at least 4096 and if larger it will also be shared with the underlying RtpClient</param>
        public RtspClient(string location, ClientProtocolType? rtpProtocolType = null, int bufferSize = DefaultBufferSize)
            : this(new Uri(location), rtpProtocolType, bufferSize) //UriDecode?
        {
            //Check for a null Credential and UserInfo in the Location given.
            if (Credential == null && false == string.IsNullOrWhiteSpace(CurrentLocation.UserInfo))
            {
                //Parse the given cred from the location
                Credential = Media.Common.Extensions.Uri.UriExtensions.ParseUserInfo(CurrentLocation);

                //Remove the user info from the location (may not have @?)
                m_InitialLocation = CurrentLocation = new Uri(CurrentLocation.AbsoluteUri.Replace(CurrentLocation.UserInfo + (char)Common.ASCII.AtSign, string.Empty).Replace(CurrentLocation.UserInfo, string.Empty));
            }
        }

        ~RtspClient()
        {
            Dispose();
        }

        #endregion

        #region Events

        public delegate void RtspClientAction(RtspClient sender, object args);

        public delegate void RequestHandler(RtspClient sender, RtspMessage request);

        public delegate void ResponseHandler(RtspClient sender, RtspMessage request, RtspMessage response);

        public event RtspClientAction OnConnect;

        internal protected void OnConnected()
        {
            if (IsDisposed) return;

            RtspClientAction action = OnConnect;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }

        }

        public event RequestHandler OnRequest;

        internal protected void Requested(RtspMessage request)
        {
            if (IsDisposed) return;

            RequestHandler action = OnRequest;

            if (action == null) return;

            foreach (RequestHandler handler in action.GetInvocationList())
            {
                try { handler(this, request); }
                catch { continue; }
            }
        }

        public event ResponseHandler OnResponse;

        internal protected void Received(RtspMessage request, RtspMessage response)
        {
            if (IsDisposed) return;

            ResponseHandler action = OnResponse;

            if (action == null) return;

            foreach (ResponseHandler handler in action.GetInvocationList())
            {
                try { handler(this, request, response); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnDisconnect;

        internal void OnDisconnected()
        {
            if (IsDisposed) return;

            RtspClientAction action = OnDisconnect;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnPlay;

        internal protected void OnPlaying(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            //Is was not already playing then set the value
            if (HandlePlayEvent && false == m_StartedPlaying.HasValue)
            {
                //Set started playing
                m_StartedPlaying = DateTime.UtcNow;

                //Active the RtpClient
                //if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient)) m_RtpClient.Activate();
            }

            RtspClientAction action = OnPlay;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnStop;

        internal protected void OnStopping(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            //Is was already playing then set the value
            if (HandleStopEvent && mediaDescription == null && true == m_StartedPlaying.HasValue || m_Playing.Count == 0) m_StartedPlaying = null;

            RtspClientAction action = OnStop;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        public event RtspClientAction OnPause;

        internal protected void OnPausing(MediaDescription mediaDescription = null)
        {
            if (IsDisposed) return;

            RtspClientAction action = OnPause;

            if (action == null) return;

            foreach (RtspClientAction handler in action.GetInvocationList())
            {
                try { handler(this, mediaDescription); }
                catch { continue; }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// DisconnectsSockets, Connects and optionally reconnects the Transport if reconnectClient is true.
        /// </summary>
        /// <param name="reconnectClient"></param>
        internal protected virtual void Reconnect(bool reconnectClient = true)
        {
            DisconnectSocket();

            Connect();

            if (reconnectClient && IsPlaying && false == m_RtpClient.IsActive) m_RtpClient.Activate();
        }

        //ProcessRemoteAnnounce

        internal protected virtual void ProcessRemoteGetParameter(RtspMessage get)
        {
            //Todo, Handle other parameters

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, get.Version, get.ContentEncoding))
            {
                //Set the sequence number
                response.CSeq = get.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }
        }

        internal protected virtual void ProcessRemoteSetParameter(RtspMessage set)
        {
            //MS-RTSP Send a server sent request similar to as follows

            /*
            SET_PARAMETER Location RTSP/1.0
            Content-Type: application/x-wms-extension-cmd
            X-Notice: 2101 "End-of-Stream Reached"
            RTP-Info: Track1, Track2
            X-Playlist-Gen-Id: 358
            Content-Length: 41
            Date: Wed, 04 Feb 2015 19:47:21 GMT
            CSeq: 1
            User-Agent: WMServer/9.1.1.5001\r\n(\r\n) [Breaks Wireshark and causes a freeze until the analyzer can recover (if it does)]
            Session: 6312817326953834859
            EOF: true
             */

            bool effectedMedia = false;

            string contentType = set[RtspHeaders.ContentType];

            if (false == string.IsNullOrWhiteSpace(contentType))
            {
                contentType = contentType.Trim();

                #region [MSRTSP - application/x-wms-extension-cmd]

                if (string.Compare(contentType, "application/x-wms-extension-cmd", true) == 0)
                {
                    string xNotice = set["X-Notice"];

                    if (false == string.IsNullOrWhiteSpace(xNotice)) //&& Boolean.Parse(set["EOF"])
                    {
                        string[] parts = xNotice.Trim().Split(RtspMessage.SpaceSplit, 2);

                        //Get rid of anything unrelated
                        string noticeIdValue = parts.FirstOrDefault();

                        //If something was extracted attempt to parse
                        if (false == string.IsNullOrWhiteSpace(noticeIdValue))
                        {
                            int noticeId;

                            //If the noticeId is 2101
                            if (int.TryParse(noticeIdValue, out noticeId) &&
                                noticeId == 2101)
                            {
                                //End Of Stream notice?

                                //Get the rtp-info header
                                string rtpInfo = set[RtspHeaders.RtpInfo];

                                string[] rtpInfos;

                                //Make a parser class which can be reused?

                                //If parsing of the header succeeded
                                if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
                                {
                                    //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                                    //Loop all found sub header values
                                    foreach (string rtpInfoValue in rtpInfos)
                                    {
                                        Uri uri;

                                        int? rtpTime;

                                        int? seq;

                                        int? ssrc;

                                        //If any value which was needed was found.
                                        if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc) && seq.HasValue)
                                        {
                                            //Just use the ssrc to lookup the context.
                                            if (ssrc.HasValue)
                                            {
                                                //Get the context created with the ssrc defined above
                                                RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                                                //If that context is not null then allow it's ssrc to change now.
                                                if (context != null)
                                                {
                                                    if (m_Playing.Remove(context.MediaDescription))
                                                    {

                                                        effectedMedia = true;

                                                        OnStopping(context.MediaDescription);

                                                        //m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                                    }

                                                    context = null;
                                                }
                                                else
                                                {
                                                    Common.ILoggingExtensions.Log(Logger, "Unknown context for ssrc = " + ssrc.Value);
                                                }
                                            }
                                            else if (uri != null)
                                            {
                                                //Need to get the context by the uri.
                                                //Location = rtsp://abc.com/live/movie
                                                //uri = rtsp://abc.com/live/movie/trackId=0
                                                //uri = rtsp://abc.com/live/movie/trackId=1
                                                //uri = rtsp://abc.com/live/movie/trackId=2

                                                //Get the context created with from the media description with the same resulting control uri
                                                RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                                                //If that context is not null then allow it's ssrc to change now.
                                                if (context != null)
                                                {
                                                    //The last packet will have the sequence number of seq.Value

                                                    if (m_Playing.Remove(context.MediaDescription))
                                                    {
                                                        effectedMedia = true;

                                                        OnStopping(context.MediaDescription);

                                                        //m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                                    }

                                                    context = null;
                                                }
                                                else
                                                {
                                                    Common.ILoggingExtensions.Log(Logger, "Unknown context for Uri = " + uri.AbsolutePath);
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            //Todo, Handle other parameters in the body

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, set.Version, set.ContentEncoding))
            {
                //Indicate OK
                response.RtspStatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = set.CSeq;

                //Send it
                using (SendRtspMessage(response, false, false)) ;
            }


            //Check that only the rtx media is playing and then remove session if so

            if (effectedMedia && m_Playing.Count == 1 && m_Playing.First().GetAbsoluteControlUri(CurrentLocation, SessionDescription).AbsoluteUri.EndsWith("rtx", StringComparison.OrdinalIgnoreCase))
            {
                //RemoveSession(m_SessionId);

                StopPlaying();
            }

        }

        internal protected virtual void ProcessRemoteEndOfStream(RtspMessage message)
        {
            //Not playing so we dont care
            if (false == IsPlaying) return;

            /* https://tools.ietf.org/html/draft-zeng-rtsp-end-of-stream-00
            An END_OF_STREAM request MUST include "CSeq", "Range" and "Session"
        headers.
        It SHOULD include "RTP-Info" header.
        The RTP-Info in server's END_OF_STREAM request
        is used to indicate the sequence number of
        the ending RTP packet for each media stream.

        An END_OF_STREAM requet MAY include a new "Reason" header,
        defined as a
        string, whose purpose is to allow the server to explain why stream
        has ended, and whose ABNF definition is given below:

                Reason     =  "Reason" ":"   Reason-Phrase CRLF
            */

            //Ensure Range and RtpInfo are present.
            string range = message[RtspHeaders.Range],

                rtpInfo = message[RtspHeaders.RtpInfo];

            if (string.IsNullOrWhiteSpace(range) || string.IsNullOrWhiteSpace(rtpInfo)) return;

            //Check what is ending...
            //if (m_LastTransmitted.Location == RtspMessage.Wildcard)
            //{

            //}else{
            // Must get stream by location to ensure request is relevent.
            //}

            string[] rtpInfos;

            //Make a parser class which can be reused?

            //If parsing of the header succeeded
            if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
            {
                //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                //Loop all found sub header values
                foreach (string rtpInfoValue in rtpInfos)
                {
                    Uri uri;

                    int? rtpTime;

                    int? seq;

                    int? ssrc;

                    //If any value which was needed was found.
                    if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc))
                    {
                        //Just use the ssrc to lookup the context.
                        if (ssrc.HasValue)
                        {
                            //Get the context created with the ssrc defined above
                            RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                            //If that context is not null then allow it's ssrc to change now.
                            if (context != null)
                            {
                                if (m_Playing.Remove(context.MediaDescription))
                                {
                                    OnStopping(context.MediaDescription);

                                    m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                }

                                context = null;
                            }
                        }
                        else if (uri != null)
                        {
                            //Need to get the context by the uri.
                            //Location = rtsp://abc.com/live/movie
                            //uri = rtsp://abc.com/live/movie/trackId=0
                            //uri = rtsp://abc.com/live/movie/trackId=1
                            //uri = rtsp://abc.com/live/movie/trackId=2

                            //Get the context created with from the media description with the same resulting control uri
                            RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                            //If that context is not null then allow it's ssrc to change now.
                            if (context != null)
                            {
                                if (m_Playing.Remove(context.MediaDescription))
                                {
                                    OnStopping(context.MediaDescription);

                                    m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                                }

                                context = null;
                            }

                        }
                    }
                }
            }

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, message.Version, message.ContentEncoding))
            {
                //Indicate OK
                response.RtspStatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = message.CSeq;

                //Send it
                using (var serverResponse = SendRtspMessage(response, false, false))
                {
                    RtspSession related;

                    if (m_Sessions.TryGetValue(m_SessionId, out related))
                    {
                        related.UpdatePushedMessages(message, serverResponse);

                        related = null;
                    }
                }
            }

            StopPlaying();
        }

        //could handle EndOfStream as a PlayNotify...

        internal protected virtual void ProcessRemoteTeardown(RtspMessage teardown)
        {
            //If playing
            if (false == IsPlaying) return;

            //Check if everything is stopping
            if (Uri.Equals(teardown.Location, CurrentLocation) || teardown.Location == RtspMessage.Wildcard)
            {
                OnStopping();

                m_RtpClient.SendGoodbyes();
            }
            else//Use the Uri to determine what is shutting down
            {
                //Attempt to find the media by the uri given.
                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                //uri's follow the format /.././Type
                if (Enum.TryParse<Sdp.MediaType>(teardown.Location.Segments.Last(), true, out mediaType))
                {
                    //Find a contet for the type given
                    var context = Client.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.MediaType == mediaType);

                    //If a context was found
                    if (context != null)
                    {
                        //If it was playing
                        if (m_Playing.Contains(context.MediaDescription))
                        {
                            //Indicate this media is stopping now
                            OnStopping(context.MediaDescription);

                            m_RtpClient.SendGoodbye(context, null, context.SynchronizationSourceIdentifier, false);
                        }

                        //remove the reference to the context
                        context = null;
                    }
                }
            }

            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, teardown.Version, teardown.ContentEncoding))
            {
                //Indicate OK
                response.RtspStatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = teardown.CSeq;

                //Send it
                using (RtspMessage serverResponse = SendRtspMessage(response, false, false))
                {
                    RtspSession related;

                    if (m_Sessions.TryGetValue(m_SessionId, out related))
                    {
                        related.UpdatePushedMessages(teardown, serverResponse);

                        related = null;
                    }
                }
            }
        }

        //https://www.ietf.org/proceedings/60/slides/mmusic-8.pdf
        //Announce? Maybe it's depreceated maybe it's not...
        //Event codes are in the header.?
        //WTF is play_notify then...

        /*
         
        +-------------+-------------------------+---------------------------+
        | Notice-code | Notice-string           | Description               |
        +-------------+-------------------------+---------------------------+
        | 1103        | Playout Stalled         | -/-                       |
        |             |                         |                           |
        | 1104        | Playout Resumed         | Temporarily stopped       |
        |             |                         |                           |
        | 2101        | End-of-Stream Reached   | Content terminated        |
        |             |                         |                           |
        | 2103        | Transition              | In transition             |
        |             |                         |                           |
        | 2104        | Start-of-Stream Reached | Returned to the initial   |
        |             |                         | content                   |
        |             |                         |                           |
        | 2306        | Continuous Feed         | Live finished             |
        |             | Terminated              |                           |
        |             |                         |                           |
        | 2401        | Ticket Expired          | Viewing right expired     |
        |             |                         |                           |
        | 4400        | Error Reading Content   | Data read error           |
        |             | Data                    |                           |
        |             |                         |                           |
        | 5200        | Server Resource         | Resource cannot be        |
        |             | Unavailable             | obtained                  |
        |             |                         |                           |
        | 5401        | Downstream Failure      | Stream could not be       |
        |             |                         | obtained                  |
        |             |                         |                           |
        | 5402        | Client Session          | -/-                       |
        |             | Terminated              |                           |
        |             |                         |                           |
        | 5403        | Server Shutting Down    | -/-                       |
        |             |                         |                           |
        | 5404        | Internal Server Error   | -/-                       |
        |             |                         |                           |
        | 5501        | End-of-Window_term      | -/-                       |
        |             |                         |                           |
        | 5502        | End-of-Contract_term    | -/-                       |
        +-------------+-------------------------+---------------------------+
         
         */

        internal protected virtual void ProcessRemotePlayNotify(RtspMessage playNotify)
        {
            //Make a response
            using (var response = new RtspMessage(RtspMessageType.Response, playNotify.Version, playNotify.ContentEncoding))
            {
                //Indicate OK
                response.RtspStatusCode = RtspStatusCode.OK;

                //Set the sequence number
                response.CSeq = playNotify.CSeq;

                //Send it
                using (RtspMessage serverResponse = SendRtspMessage(response, false, false))
                {
                    RtspSession related;

                    //should use playNotify SessionHeader.

                    if (m_Sessions.TryGetValue(m_SessionId, out related))
                    {
                        related.UpdatePushedMessages(playNotify, serverResponse);

                        related = null;
                    }
                }
            }
        }

        protected virtual void ProcessServerSentRequest(RtspMessage toProcess = null)
        {
            if (false == IgnoreServerSentMessages &&
                toProcess == null ||
                toProcess.MessageType != RtspMessageType.Request ||
                false == toProcess.IsComplete) return;

            //Ensure suported methods contains the method requested.
            SupportedMethods.Add(toProcess.MethodString);

            //Check the sequence number
            int sequenceNumber = toProcess.CSeq;

            //Don't handle a request with an invalid remote sequence number
            if (sequenceNumber < m_RCSeq) return;

            //Update the remote sequence number
            m_RCSeq = sequenceNumber;

            //Increment handled pushed messages
            ++m_PushedMessages;

            //Raise an event for the request received.
            Received(toProcess, null);

            //Determine 
            string session = m_LastTransmitted[RtspHeaders.Session];

            if (false == string.IsNullOrWhiteSpace(m_SessionId) &&
                false == string.IsNullOrWhiteSpace(session))
            {
                //Not for the same session
                if (m_SessionId != session.Trim())
                {
                    return;
                }
            }

            //handle the message received
            switch (toProcess.RtspMethod)
            {
                case RtspMethod.TEARDOWN:
                    {
                        ProcessRemoteTeardown(toProcess);

                        return;
                    }
                case RtspMethod.ANNOUNCE:
                    {
                        //https://www.ietf.org/proceedings/60/slides/mmusic-8.pdf
                        //Announce is sometimes used for this, special EventType header.

                        //Check for codes present...

                        if (Uri.Equals(toProcess.Location, CurrentLocation))
                        {
                            //Check for SDP content type and update the SessionDescription
                        }

                        return;
                    }
                case RtspMethod.GET_PARAMETER:
                    {

                        ProcessRemoteGetParameter(toProcess);

                        return;
                    }
                case RtspMethod.SET_PARAMETER:
                    {
                        ProcessRemoteSetParameter(toProcess);

                        break;
                    }
                case RtspMethod.PLAY_NOTIFY:
                    {
                        /*
                         There are two ways for the client to be informed about changes of
                        media resources in Play state.  The client will receive a PLAY_NOTIFY
                        request with Notify-Reason header set to media-properties-update (see
                        Section 13.5.2.  The client can use the value of the Media-Range to
                        decide further actions, if the Media-Range header is present in the
                        PLAY_NOTIFY request.  The second way is that the client issues a
                        GET_PARAMETER request without a body but including a Media-Range
                        header.  The 200 OK response MUST include the current Media-Range
                        header (see Section 18.30).
                         */

                        //Use the Uri to determine what is chaning.
                        if (Uri.Equals(toProcess.Location, CurrentLocation))
                        {
                            //See what is being notified.
                        }

                        break;
                    }
                default:
                    {

                        //Something else...
                        if (string.Compare(toProcess.MethodString, "END_OF_STREAM", true) == 0)
                        {
                            //Should be merged with Teardown.
                            ProcessRemoteEndOfStream(toProcess);

                            return;
                        }

                        //Make a response to indicate the method is not supported
                        using (var response = new RtspMessage(RtspMessageType.Response, toProcess.Version, toProcess.ContentEncoding))
                        {
                            //Indicate Not Allowed.
                            response.RtspStatusCode = RtspStatusCode.NotImplemented;

                            //Todo
                            //Should use MethodNotAllowed and set Allow header with supported methods.

                            //Set the sequence number
                            response.CSeq = toProcess.CSeq;

                            //Send it
                            using (var serverResposne = SendRtspMessage(response, false, false))
                            {
                                RtspSession related;

                                if (m_Sessions.TryGetValue(session, out related))
                                {
                                    related.UpdatePushedMessages(toProcess, serverResposne);

                                    related = null;
                                }
                            }
                        }


                        return;
                    }
            }
        }

        /// <summary>
        /// Handles Interleaved Data for the RtspClient by parsing the given memory for a valid RtspMessage.
        /// </summary>
        /// <param name="sender">The RtpClient instance which called this method</param>
        /// <param name="memory">The memory to parse</param>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        //Todo make a better name e.g. HandleReceivedData
        void ProcessInterleaveData(object sender, byte[] data, int offset, int length)
        {
            if (length == 0) return;

            //Cache offset and count, leave a register for received data (should be calulated with length)
            int received = 0;

            //Must contain textual data to be an interleaved rtsp request.
            //if (!Utility.FoundValidUniversalTextFormat(data, ref offset, ref length)) return; //See comments below if attempting to complete large packets.

            //Should check for BigEndianFrameControl @ offset which indicates a large packet or a packet under 8 bytes or a packet not handled by the RtpClient.
            //In such a case the length needs to be read and if the packet was larger than the buffer the next time this event fires the remaining data will be given
            //When reading sizes the frame size should ALWAYS be <= any Blocksize the server responded with (if any)
            //Then the data can be given back to the RtpClient with ProcessFrameData when the packet is complete.
            //If another packet arrives while one is being completed that is up to the implementation to deal with for now, other implementations just drop the data and give you no way to even receive it.            
            //Some servers may use this type of data to indicate special processing e.g. WMS for PacketPairs or RDT etc.

            unchecked
            {
                //Validate the data received
                RtspMessage interleaved = new RtspMessage(data, offset, length);

                //Determine what to do with the interleaved message
                switch (interleaved.MessageType)
                {
                    //Handle new requests or responses
                    case RtspMessageType.Request:
                    case RtspMessageType.Response:
                        {
                            //Calculate the length of what was received
                            received = length;

                            if (received > 0)
                            {
                                //Increment for messages received
                                ++m_ReceivedMessages;

                                //If not playing an interleaved stream, Complete the message if not complete (Should maybe check for Content-Length)
                                while (false == SharesSocket && false == interleaved.IsComplete)
                                {
                                    //Take in some bytes from the socket
                                    int justReceived = interleaved.CompleteFrom(m_RtspSocket, m_Buffer);

                                    if (justReceived == 0) break;

                                    //Incrment for justReceived
                                    received += justReceived;

                                    //Ensure we are not doing to much receiving
                                    if (interleaved.ContentLength > 0 && received > RtspMessage.MaximumLength + interleaved.ContentLength) break;
                                }

                                //Update counters
                                m_ReceivedBytes += received;

                                //Disposes the last message if it exists.
                                if (m_LastTransmitted != null)
                                {
                                    m_LastTransmitted.Dispose();

                                    m_LastTransmitted = null;
                                }

                                //Store the last message
                                m_LastTransmitted = interleaved;

                                //Need a method to get a Session by a Message.
                                //Update the messge on the session..

                                //if the message was a request and is complete handle it now.
                                if (m_LastTransmitted.MessageType == RtspMessageType.Request &&
                                    false == InUse)
                                {
                                    ProcessServerSentRequest(m_LastTransmitted);
                                }

                            }

                            goto default;
                        }
                    case RtspMessageType.Invalid:
                        {
                            //Dispose the invalid message
                            interleaved.Dispose();

                            interleaved = null;

                            //If playing and interleaved stream AND the last transmitted message is NOT null and is NOT Complete then attempt to complete it
                            if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted))
                            {
                                //RtspMessage local = m_LastTransmitted;

                                //Take note of the length of the last transmitted message.
                                int lastLength = m_LastTransmitted.Length;

                                //Create a memory segment and complete the message as required from the buffer.
                                using (var memory = new Media.Common.MemorySegment(data, offset, length))
                                {
                                    //Use the data recieved to complete the message and not the socket
                                    int justReceived = false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) ? m_LastTransmitted.CompleteFrom(null, memory) : 0;

                                    //If anything was received
                                    if (justReceived > 0)
                                    {
                                        //Account for what was just recieved.
                                        received += justReceived;

                                        //No data was consumed don't raise another event.
                                        if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) && lastLength == m_LastTransmitted.Length) received = 0;
                                    }

                                    //handle the completion of a request sent by the server if allowed.
                                    if (received > 0 &&
                                        false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) &&
                                        m_LastTransmitted.MessageType == RtspMessageType.Request &&
                                        false == InUse) //dont handle if waiting for a resposne...
                                    {
                                        //Process the pushed message
                                        ProcessServerSentRequest(m_LastTransmitted);

                                        //then continue
                                    }
                                }
                            }

                            //Handle with default logic.
                            goto default;
                        }
                    default:
                        {
                            //If anything was received
                            if (received > 0)
                            {

                                //Todo could have TransactionCache with requests which are then paried to response here.

                                //Release the m_Interleaved event if it was set
                                if (false == m_InterleaveEvent.IsSet)
                                {
                                    //Thus allowing threads blocked by it to proceed.
                                    m_InterleaveEvent.Set();
                                } //Otherwise
                                else if (false == IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted) &&
                                    m_LastTransmitted.MessageType == RtspMessageType.Response) //and was a response
                                {
                                    //Otherwise indicate a message has been received now. (for responses only)
                                    Received(m_LastTransmitted, null);
                                }

                                //Handle any data remaining in the buffer
                                if (received < length)
                                {
                                    //(Must ensure Length property of RtspMessage is exact).
                                    ProcessInterleaveData(sender, data, received, length - received);
                                }
                            }

                            //done
                            return;
                        }
                }
            }
        }

        /// <summary>
        /// Increments and returns the current <see cref="ClientSequenceNumber"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal int NextClientSequenceNumber() { return ++m_CSeq; }

        //Should have end time also?
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StartPlaying(TimeSpan? start = null, TimeSpan? end = null, IEnumerable<Sdp.MediaType> mediaTypes = null) //should allow to re describe ,bool forceDescribe = false
        {
            //Is already playing don't do anything
            if (IsPlaying) return;

            //Try to connect if not already connected.
            if (false == IsConnected) Connect();

            //Send the options if nothing was received before
            if (m_ReceivedMessages == 0) using (var options = SendOptions())
                {
                    if (options == null || options.RtspStatusCode != RtspStatusCode.OK) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(options, "Options Response was null or not OK. See Tag.");
                }

            //Check if Describe is allowed or that a SessionDescription is present.
            if (false == SupportedMethods.Contains(RtspMethod.DESCRIBE.ToString()) && SessionDescription == null) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(SupportedMethods, "SupportedMethods does not allow Describe and SessionDescription is null. See Tag with SupportedMessages.");

            //Check for automatic disconnect
            if (AutomaticallyDisconnect) Disconnect(true);

        Describe:
            //Send describe if we need a session description
            if (SessionDescription == null) using (var describe = SendDescribe())
                {
                    if (describe == null || describe.RtspStatusCode != RtspStatusCode.OK) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(describe, "Describe Response was null or not OK. See Tag.");

                    describe.IsPersistent = false;
                }

        Setup:
            //Determine if any context was present or created.
            bool hasContext = false, triedAgain = false;

            List<MediaDescription> setupMedia = new List<MediaDescription>();

            //Get the media descriptions in the session description to setup
            var toSetup = SessionDescription.MediaDescriptions;

            //////Should check the TimeDescriptions to ensure media is active (Before chaning the order)
            ////if (SessionDescription.TimeDescriptions.Count() > 0)
            ////{

            ////}

            //Windows Media Server May require an order like so.
            //SetupOrder = (mds) => mds.OrderBy(md=> md.MediaType).Reverse();

            //If a SetupOrder has been defined then use it
            if (SetupOrder != null)
            {
                SetupOrder(toSetup);
            }

            //For each MediaDescription in the SessionDecscription (ordered by the media type) and then reversed to ensure wms rtx going first (but it doesn't seem to matter anyway)
            //What could be done though is to use the detection of the rtx track to force interleaved playback.
            foreach (Sdp.MediaDescription md in toSetup)
            {
                //Don't setup unwanted streams
                if (mediaTypes != null && false == mediaTypes.Any(t => t == md.MediaType)) continue;

                //Should be able to be SETUP if in the MediaDescriptions....
                //if (md.MediaType == MediaType.application) continue;

                //If transport was already setup then see if the transport has a context for the media
                if (Client != null)
                {
                    //Get the context for the media
                    var context = Client.GetContextForMediaDescription(md);

                    //If there is a context which is not already playing and has not ended
                    if (context != null) if (false == m_Playing.Contains(context.MediaDescription))
                        {
                            //If the context is no longer receiving (should be a property on TransportContext but when pausing the RtpClient doesn't know about this)
                            if (context.TimeReceiving == context.TimeSending && context.TimeSending == Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan)
                            {
                                //Remove the context
                                Client.TryRemoveContext(context);

                                //Dispose it
                                context.Dispose();

                                //remove the reference
                                context = null;
                            }
                            //else context.Goodbye = null;
                        }
                        else
                        {
                            setupMedia.Add(md);

                            //The media is already playing
                            hasContext = true;

                            continue;
                        }
                }

                //Send a setup while there was a bad request or no response.
                do using (RtspMessage setup = SendSetup(md))
                    {

                        #region Unused feature [Continue Without Setup Response]

                        //If the setup doesn't get a response sometimes the source starts sending data anyway....
                        //Happens especially when a rtspclient uses TCP, disconnects and then re-connects using a new socket...
                        //This no longer occurs unless forced and the previous SessionDescripton can be used (if known) to create a MediaDescription.

                        //In short if you take over a rtsp session and you don't have the SDP from when it was created 
                        //then you can still send and receive data for the connection but you wouldn't know what streams come from what channel
                        //unless you had some prior knowledge (from a session you created previously) or didn't really care (being malicious)
                        //if (setup == null)
                        //{
                        //    hasContext = true;

                        //    setupMedia.Add(md);

                        //    continue;
                        //}

                        #endregion

                        //No response?
                        if (setup == null)
                        {
                            if (triedAgain)
                            {
                                Reconnect();

                                continue;
                            }

                            triedAgain = true;

                            if (InUse) continue;

                            m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond);

                            continue;
                        }

                        RtspStatusCode setupStatusCode = setup.RtspStatusCode;

                        //If the setup was okay
                        if (setupStatusCode <= RtspStatusCode.OK)
                        {
                            //setup.IsPersistent = true;

                            //Only setup tracks if response was OK
                            hasContext = true;

                            //Add the media to the list of what was setup.
                            setupMedia.Add(md);

                            //setup.IsPersistent = false;

                            break;

                            #region Unused Feature [NewSocketEachSetup]

                            //Testing if a new socket can be used with each setup
                            // if(NewSocketEachSetup) { Reconnect(); }

                            #endregion
                        }
                        else if (setupStatusCode == RtspStatusCode.NotFound)
                        {

                            //Sometimes the host is not yet ready, this could be true for cases when hosts uses dynamic uri's which don't yet exists during pipelining etc.
                            if (false == triedAgain)
                            {
                                triedAgain = true;

                                if (InUse) continue;

                                m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);
                            }
                            else
                            {
                                Reconnect();

                                SessionDescription.Dispose();

                                SessionDescription = null;

                                goto Describe;
                            }

                            continue;
                        }
                        //else if (setupStatusCode == RtspStatusCode.Unauthorized)
                        //{
                        //    //Some servers use this when they are restarting and there is no auth loaded yet.
                        //}
                        //3 or more should switch
                        //else if (setup.StatusCode == RtspStatusCode.UnsupportedTransport)
                        //{
                        //    //Check for a 'rtx' connection
                        //}

                        //The request had a code which indicated that SETUP was not a success.

                    } while (IsConnected); //2 attempts each attempt...
            }

            //If we have a play context then send the play request.
            if (false == hasContext) throw new InvalidOperationException("Cannot Start Playing, No Tracks Setup.");

            //set to false for play request.
            triedAgain = false;

            //Ensure service is avilable.
            bool serviceUnavailable = false;

            //Send the play request while a OKAY response was not received
            do using (RtspMessage play = SendPlay(InitialLocation, start ?? StartTime, end ?? EndTime))
                {
                    //Check for a response
                    bool hasResponse = false == Common.IDisposedExtensions.IsNullOrDisposed(play) && play.MessageType == RtspMessageType.Response;

                    //If there was a response
                    if (hasResponse)
                    {
                        RtspStatusCode playStatusCode = play.RtspStatusCode;

                        //If the response was a success
                        if (playStatusCode <= RtspStatusCode.OK)
                        {
                            //Enumerate the setup media and add it to the playing list.
                            foreach (var media in setupMedia) if (false == m_Playing.Contains(media)) m_Playing.Add(media);

                            //Fire an event
                            OnPlaying();

                            //play.IsPersistent = false;

                            //Set EndTime
                            //Stop attempting to send play requests.
                            break;
                        }
                        else if (play.RtspStatusCode == RtspStatusCode.ServiceUnavailable)
                        {
                            if (serviceUnavailable && triedAgain) throw new InvalidOperationException("Cannot Start Playing, ServiceUnavailable");

                            else continue;
                        }
                        else if (playStatusCode == RtspStatusCode.MethodNotAllowed || playStatusCode == RtspStatusCode.MethodNotValidInThisState)
                        {
                            //If already tried again then retry setup.
                            if (triedAgain) goto Setup;
                        }

                        //Set triedAgain
                        triedAgain = true;
                    }
                    else //No response...
                    {
                        if (triedAgain)
                        {

                            if (serviceUnavailable)
                            {
                                serviceUnavailable = false;

                                continue;
                            }

                            //Disconnect and Reconnect
                            Reconnect();

                            //Active the RtpClient
                            //m_RtpClient.Activate();
                        }

                        triedAgain = true;
                    }

                    if (InUse) continue;

                    m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                    continue;

                } while (IsConnected);

            //Ensure the RtpClient is still active.
            m_RtpClient.Activate();

            //Should be an option..
            //Initiate a keep alive now if data is still not flowing.
            if (SendKeepAliveImmediatelyAfterStartPlaying && Client.TotalBytesReceieved == 0) SendKeepAliveRequest(null);

            TimeSpan halfSessionTimeWithConnection = TimeSpan.FromTicks(m_RtspSessionTimeout.Subtract(m_ConnectionTime).Ticks >> 1);

            //If dueTime is zero (0), callback is invoked immediately. If dueTime is negative one (-1) milliseconds, callback is not invoked; the timer is disabled, but can be re-enabled by calling the Change method.
            //Setup a timer to send any requests to keep the connection alive and ensure media is flowing.
            //Subtract against the connection time... the averge rtt would be better
            if (m_KeepAliveTimer == null) m_KeepAliveTimer = new Timer(new TimerCallback(SendKeepAliveRequest), null, halfSessionTimeWithConnection, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);

            //Watch for pushed messages.
            m_ProtocolMonitor = new System.Threading.Timer(new TimerCallback(MonitorProtocol), null, m_ConnectionTime.Add(LastMessageRoundTripTime), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);

            //Don't keep the tcp socket open when not required under Udp.

            //Todo, should check for Udp, but hopefully people who use this know what they are doing...
            //m_RtpProtocol == ProtocolType.Udp
            if (AutomaticallyDisconnect) DisconnectSocket();
        }

        //Params?

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StopPlaying(MediaDescription mediaDescription, bool force = false)
        {
            //If the media was playing
            if (false == force && PlayingMedia.Contains(mediaDescription))
            {
                using (RtspMessage resposne = SendTeardown(mediaDescription, false, force)) ;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StopPlaying(IEnumerable<MediaDescription> mediaDescriptions, bool force = false)
        {
            foreach (MediaDescription mediaDescription in mediaDescriptions)
                StopPlaying(mediaDescription, force);
        }

        //Params?

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StopPlaying(bool disconnectSocket = true)
        {
            if (IsDisposed || false == IsPlaying) return;
            try { Disconnect(disconnectSocket); }
            catch (Exception ex) { Media.Common.ILoggingExtensions.Log(Logger, ex.Message); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Pause(MediaDescription mediaDescription = null, bool force = false)
        {
            //Don't pause if playing.
            if (false == force && false == IsPlaying) return;

            var context = Client.GetContextForMediaDescription(mediaDescription);

            //Dont pause media which is not setup unless forced.
            if (false == force && mediaDescription != null && context == null) return;

            //context.Goodbye = null;

            //Send the pause.
            SendPause(mediaDescription, force);
        }

        /// <summary>
        /// Sends a SETUP if not already setup and then a PLAY for the given.
        /// If nothing is given this would be equivalent to calling <see cref="StartPlaying"/>
        /// </summary>
        /// <param name="mediaDescription"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Play(MediaDescription mediaDescription = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt")
        {
            bool playing = IsPlaying;
            //If already playing and nothing was given then there is nothing to do
            if (playing && mediaDescription == null) return;
            else if (false == playing) //We are not playing and nothing was given.
            {
                //Start playing everything
                StartPlaying();

                //do nothing else
                return;
            }

            var context = Client.GetContextForMediaDescription(mediaDescription);

            //Dont setup media which is already setup.
            if (mediaDescription != null && context == null) return;

            //setup the media description
            using (var setupResponse = SendSetup(mediaDescription))
            {
                //If the response was OKAY
                if (setupResponse != null && setupResponse.RtspStatusCode == RtspStatusCode.OK)
                {

                    //context.Goodbye = null;

                    //Send the PLAY.
                    using (SendPlay(mediaDescription, startTime, endTime, rangeType)) ;
                }
            }
        }

        /// <summary>
        /// If <see cref="IsConnected"/> and not forced an <see cref="InvalidOperationException"/> will be thrown.
        /// 
        /// <see cref="DisconnectSocket"/> is called if there is an existing socket.
        /// 
        /// Creates any required client socket stored the time the call was made and calls <see cref="ProcessEndConnect"/> unless an unsupported Proctol is specified.
        /// </summary>
        /// <param name="force">Indicates if a previous existing connection should be disconnected.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Connect(bool force = false)
        {
            try
            {
                //Ensure logic for UDP is correct, may have to store flag.
                if (false == force && IsConnected) return;

                //If there is an RtpClient already connected then attempt to find a socket used by the client with the EndPoint
                //required to be connected to
                if (m_RtpClient != null && m_RtpClient.IsActive)
                {
                    //Todo, should be interface.
                    foreach (RtpClient.TransportContext transportContext in m_RtpClient.GetTransportContexts())
                    {
                        //If disposed continue, should be handled in GetTransportContexts()..
                        if (Common.IDisposedExtensions.IsNullOrDisposed(transportContext) || false == transportContext.IsActive) continue;

                        //Get the sockets in reference by the context
                        foreach (Socket socket in ((ISocketReference)transportContext).GetReferencedSockets())
                        {
                            //Check for the socket to not be disposed...
                            if (false == socket.Connected) continue;

                            IPEndPoint ipendPoint = (IPEndPoint)socket.RemoteEndPoint;

                            if (ipendPoint.Address.Equals(m_RemoteIP) && ipendPoint.Port == m_RtspPort && socket.Connected)
                            {
                                //Assign the socket (Update ConnectionTime etc)>
                                RtspSocket = socket;

                                //m_InterleaveEvent.Reset();

                                return;
                            }
                        }

                    }
                }

                //Wait for existing writes
                //m_InterleaveEvent.Wait();

                //Deactivate any existing previous socket and erase connect times.
                if (m_RtspSocket != null) DisconnectSocket();

                //Based on the ClientProtocolType
                switch (m_RtspProtocol)
                {
                    case ClientProtocolType.Http:
                    case ClientProtocolType.Tcp:
                        {
                            /*  9.2 Reliability and Acknowledgements
                             If a reliable transport protocol is used to carry RTSP, requests MUST
                             NOT be retransmitted; the RTSP application MUST instead rely on the
                             underlying transport to provide reliability.
                             * 
                             If both the underlying reliable transport such as TCP and the RTSP
                             application retransmit requests, it is possible that each packet
                             loss results in two retransmissions. The receiver cannot typically
                             take advantage of the application-layer retransmission since the
                             transport stack will not deliver the application-layer
                             retransmission before the first attempt has reached the receiver.
                             If the packet loss is caused by congestion, multiple
                             retransmissions at different layers will exacerbate the congestion.
                             * 
                             If RTSP is used over a small-RTT LAN, standard procedures for
                             optimizing initial TCP round trip estimates, such as those used in
                             T/TCP (RFC 1644) [22], can be beneficial.
                             * 
                            The Timestamp header (Section 12.38) is used to avoid the
                            retransmission ambiguity problem [23, p. 301] and obviates the need
                            for Karn's algorithm.
                             * 
                           Each request carries a sequence number in the CSeq header (Section
                           12.17), which is incremented by one for each distinct request
                           transmitted. If a request is repeated because of lack of
                           acknowledgement, the request MUST carry the original sequence number
                           (i.e., the sequence number is not incremented).
                             * 
                           Systems implementing RTSP MUST support carrying RTSP over TCP and MAY
                           support UDP. The default port for the RTSP server is 554 for both UDP
                           and TCP.
                             * 
                           A number of RTSP packets destined for the same control end point may
                           be packed into a single lower-layer PDU or encapsulated into a TCP
                           stream. RTSP data MAY be interleaved with RTP and RTCP packets.
                           Unlike HTTP, an RTSP message MUST contain a Content-Length header
                           whenever that message contains a payload. Otherwise, an RTSP packet
                           is terminated with an empty line immediately following the last
                           message header.
                             * 
                            */

                            //Create the socket
                            m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            break;
                        }
                    case ClientProtocolType.Udp:
                        {
                            //Create the socket
                            m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            break;
                        }
                    default: throw new NotSupportedException("The given ClientProtocolType is not supported.");
                }

                if (ConfigureSocket != null) ConfigureSocket(m_RtspSocket);                

                //We started connecting now.
                m_BeginConnect = DateTime.UtcNow;

                //Handle the connection attempt (Assumes there is already a RemoteRtsp value)
                ProcessEndConnect(null);

            }
            catch (Exception ex)
            {
                Common.ILoggingExtensions.Log(Logger, ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Calls Connect on the underlying socket.
        /// 
        /// Marks the time when the connection was established.
        /// 
        /// Increases the <see cref="SocketWriteTimeout"/> AND <see cref="SocketReadTimeout"/> by the time it took to establish the connection in milliseconds * 2.
        /// 
        /// </summary>
        /// <param name="state">Ununsed.</param>
        protected virtual void ProcessEndConnect(object state, int multiplier = 2)//should be vaarible in class
        {
            try
            {
                if (m_RemoteRtsp == null) throw new InvalidOperationException("A remote end point must be assigned");

                //Try to connect
                m_RtspSocket.Connect(m_RemoteRtsp);

                //Sample the clock after connecting
                m_EndConnect = DateTime.UtcNow;

                //Calculate the connection time.
                m_ConnectionTime = m_EndConnect.Value - m_BeginConnect.Value;

                //When timeouts are set then ensure they are within the amount of time the connection took to establish
                if ((SocketWriteTimeout + SocketReadTimeout) > 0)
                {
                    //Possibly in a VM the timing may be off (Hardware Abstraction Layer BUGS) and if the timeout occurs a few times witin the R2 the socket may be closed
                    //To prefent this check the value first.
                    int multipliedConnectionTime = (int)(m_ConnectionTime.TotalMilliseconds * multiplier);

                    //If it took longer than 50 msec to connect 
                    if (multipliedConnectionTime > SocketWriteTimeout || multipliedConnectionTime > SocketReadTimeout)
                    {
                        //Set the read and write timeouts based upon such a time (should include a min of the m_RtspSessionTimeout.)
                        if (m_ConnectionTime > TimeSpan.Zero) SocketWriteTimeout = SocketReadTimeout += (int)(m_ConnectionTime.TotalMilliseconds * multiplier);
                        //....else 
                    }
                }

                //Determine the poll time now.
                m_SocketPollMicroseconds = (int)Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(m_RtspSocket));

                //Don't block (possibly another way to work around the issue)
                //m_RtspSocket.Blocking = false;

                //Raise the Connected event.
                OnConnected();
            }
            catch { throw; }
        }

        //handle exception, really needs to know what type of operation this was also.(read or write)
        //virtual void HandleSocketException(SocketException exception, bool wasReading, wasWriting)
        //{

        //}

        /// <summary>
        /// If <see cref="IsConnected"/> nothing occurs.
        /// Disconnects the RtspSocket if Connected and <see cref="LeaveOpen"/> is false.  
        /// Sets the <see cref="ConnectionTime"/> to <see cref="Utility.InfiniteTimepan"/> so IsConnected is false.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void DisconnectSocket(bool force = false)
        {
            //If not connected and not forced return
            if (false == IsConnected && false == force) return;

            //When disconnecting the credentials must be used again when re-connecting.
            m_AuthorizationHeader = null;

            //Raise an event
            OnDisconnected();

            //If there is a socket
            if (m_RtspSocket != null)
            {
                //If LeaveOpen was false and the socket is not shared.
                if (false == LeaveOpen && false == SharesSocket)
                {
                    #region The Great Debate on Closing

                    //Don't allow further sending
                    //m_RtspSocket.Shutdown(SocketShutdown.Send);

                    //Should receive any data in buffer while not getting 0?

                    //m_RtspSocket.Close();

                    //May take to long because of machine level settings.
                    //m_RtspSocket.Deactivate(true);

                    #endregion

                    //Dispose the socket
                    m_RtspSocket.Dispose();
                }

                //Set the socket to null (no longer will Share Socket)
                m_RtspSocket = null;

                //Reset the event to prevent further writing on this instance because the socket is still in use and now is owned by the RtpClient.
                // m_InterleaveEvent.Reset();
            }

            //Indicate not connected.
            m_BeginConnect = m_EndConnect = null;

            m_ConnectionTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
        }

        /// <summary>
        /// Stops Sending any KeepAliveRequests.
        /// 
        /// Stops the Protocol Switch Timer.
        /// 
        /// If <see cref="IsPlaying"/> is true AND there is an assigned <see cref="SessionId"/>,
        /// Stops any playing media by sending a TEARDOWN for the current <see cref="CurrentLocation"/> 
        /// 
        /// Disconnects any connected Transport which is still connected.
        /// 
        /// Calls DisconnectSocket.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Disconnect(bool disconnectSocket = false)
        {
            //Get rid of the timers
            if (m_KeepAliveTimer != null)
            {
                m_KeepAliveTimer.Dispose();
                m_KeepAliveTimer = null;
            }

            if (m_ProtocolMonitor != null)
            {
                m_ProtocolMonitor.Dispose();
                m_ProtocolMonitor = null;
            }

            //Determine if we need to do anything
            if (IsPlaying && false == string.IsNullOrWhiteSpace(m_SessionId))
            {
                //Send the Teardown
                try
                {
                    //Don't really care if the response is received or not (indicate to close the connection)
                    using (SendTeardown(null, true)) ;
                }
                catch
                {
                    //We may not recieve a response if the socket is closed in a violatile fashion on the sending end
                    //And we realy don't care
                    //ILoggingExtensions.Log(Logger, @ex)
                }
                finally
                {
                    m_SessionId = string.Empty;
                }
            }

            if (Client != null && Client.IsActive) Client.Deactivate();

            if (disconnectSocket) DisconnectSocket();
        }

        #endregion

        #region Rtsp

        /// <summary>
        /// Uses the given request to Authenticate the RtspClient when challenged.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public virtual RtspMessage Authenticate(RtspMessage request, RtspMessage response = null, bool force = false)
        {
            //If not forced and already TriedCredentials and there was no response then return null.
            if (false == force && TriedCredentials && response == null) return response;

            #region Example header

            //http://tools.ietf.org/html/rfc2617
            //3.2.1 The WWW-Authenticate Response Header
            //Example
            //WWW-Authenticate: Basic realm="nmrs_m7VKmomQ2YM3:", Digest realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525"\r\n

            #endregion

            //If there was a response get the WWWAuthenticate header from it.

            string authenticateHeader = response != null ? response[RtspHeaders.WWWAuthenticate] : string.Empty;

            //Basic auth shouldn't expire, but to be supported there should be an AuthenticationState class which
            //holds the state for Authentication, e.g. LastAuthenticationTime, Attempts etc.
            //Then using that we can really narrow down if the Auth is expired or just not working.

            //For now, if there was no header or if we already tried to authenticate and the header doesn't contain "stale" then return the response given.
            if (string.IsNullOrWhiteSpace(authenticateHeader) ||
                TriedCredentials &&
                authenticateHeader.IndexOf("stale", StringComparison.OrdinalIgnoreCase) < 0) return response;

            //Note should not be using ASCII, the request and response have the characters already encoded.

            //Should also be a hash broken up by key appropriately.

            //Get the tokens in the header
            //Todo, use response.m_StringWhiteSpace to ensure the encoding is parsed correctly...
            string[] baseParts = authenticateHeader.Split(Media.Common.Extensions.Linq.LinqExtensions.Yield(((char)Common.ASCII.Space)).ToArray(), 2, StringSplitOptions.RemoveEmptyEntries);

            //If nothing was in the header then return the response given.
            if (baseParts.Length == 0) return response;
            else if (baseParts.Length > 1) baseParts = Media.Common.Extensions.Linq.LinqExtensions.Yield(baseParts[0]).Concat(baseParts[1].Split(RtspHeaders.Comma).Select(s => s.Trim())).ToArray();

            if (string.Compare(baseParts[0].Trim(), "basic", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Basic)
            {
                AuthenticationScheme = AuthenticationSchemes.Basic;

                request.SetHeader(RtspHeaders.Authorization, m_AuthorizationHeader = RtspHeaders.BasicAuthorizationHeader(request.ContentEncoding, Credential));

                request.RemoveHeader(RtspHeaders.Timestamp);
                request.RemoveHeader(RtspHeaders.CSeq);

                //Recurse the call with the info from then authenticate header
                return SendRtspMessage(request);

            }
            else if (string.Compare(baseParts[0].Trim(), "digest", true) == 0 || m_AuthenticationScheme == AuthenticationSchemes.Digest)
            {
                AuthenticationScheme = AuthenticationSchemes.Digest;

                //May use a different algorithmm
                string algorithm = baseParts.Where(p => p.StartsWith("algorithm", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(algorithm)) algorithm = "MD5";
                else
                {
                    algorithm = algorithm.Trim();
                    if (string.Compare(algorithm.Substring(9), "MD5", true) == 0) algorithm = "MD5";
                    else Media.Common.TaggedExceptionExtensions.RaiseTaggedException(response, "See the response in the Tag.", new NotSupportedException("The algorithm indicated in the authenticate header is not supported at this time. Create an issue for support."));
                }

                string username = baseParts.Where(p => p.StartsWith("username", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(username)) username = username.Substring(9);
                else username = Credential.UserName; //use the username of the credential.

                string realm = Credential.Domain;

                //Get the realm if we don't have one.
                if (string.IsNullOrWhiteSpace(realm))
                {
                    //Check for the realm token
                    realm = baseParts.Where(p => p.StartsWith("realm", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                    //If it was there
                    if (false == string.IsNullOrWhiteSpace(realm))
                    {
                        //Parse it
                        realm = realm.Substring(6).Replace("\"", string.Empty).Replace("\'", string.Empty).Trim();

                        //Store it
                        Credential.Domain = realm;
                    }
                }

                string nc = baseParts.Where(p => p.StartsWith("nc", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(nc)) nc = realm.Substring(3);

                string nonce = baseParts.Where(p => p.StartsWith("nonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(nonce)) nonce = nonce.Substring(6).Replace("\"", string.Empty).Replace("\'", string.Empty);

                string cnonce = baseParts.Where(p => p.StartsWith("cnonce", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();//parts.Where(p => string.Compare("cnonce", p, true) == 0).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(cnonce))
                {

                    if (m_LastTransmitted != null)
                    {
                        cnonce = "";
                    }

                    cnonce = cnonce.Substring(7).Replace("\"", string.Empty).Replace("\'", string.Empty);//cnonce = cnonce.Replace("cnonce=", string.Empty);
                }

                string uri = baseParts.Where(p => p.StartsWith("uri", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); //parts.Where(p => p.Contains("uri")).FirstOrDefault();
                bool rfc2069 = false == string.IsNullOrWhiteSpace(uri) && false == uri.Contains(RtspHeaders.HyphenSign);

                if (false == string.IsNullOrWhiteSpace(uri))
                {
                    if (rfc2069) uri = uri.Substring(4);
                    else uri = uri.Substring(11);
                }

                string qop = baseParts.Where(p => string.Compare("qop", p, true) == 0).FirstOrDefault();

                if (false == string.IsNullOrWhiteSpace(qop))
                {
                    qop = qop.Replace("qop=", string.Empty);
                    if (nc != null) nc = nc.Substring(3);
                }

                string opaque = baseParts.Where(p => p.StartsWith("opaque", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (false == string.IsNullOrWhiteSpace(opaque)) opaque = opaque.Substring(7);

                //Set the header and store it for use later.
                request.SetHeader(RtspHeaders.Authorization, m_AuthorizationHeader = RtspHeaders.DigestAuthorizationHeader(request.ContentEncoding, request.RtspMethod, request.Location, Credential, qop, nc, nonce, cnonce, opaque, rfc2069, algorithm, request.Body));

                //Todo 'Authorization' property?

                request.RemoveHeader(RtspHeaders.Timestamp);
                request.RemoveHeader(RtspHeaders.CSeq);

                //Recurse the call with the info from then authenticate header
                return SendRtspMessage(request);
            }
            else
            {
                throw new NotSupportedException("The given Authorization type is not supported, '" + baseParts[0] + "' Please use Basic or Digest.");
            }
        }

        //hasResponse could be set automatically by MessageType.

        public RtspMessage SendRtspMessage(RtspMessage message, bool useClientProtocolVersion = true, bool hasResponse = true)
        {
            SocketError result;

            int sequenceNumber;

            return SendRtspMessage(message, out result, out sequenceNumber, useClientProtocolVersion, hasResponse);
        }

        //public virtual void PrepareForTransport(RtspMessage message)
        //{

        //}

        public virtual void Timestamp(RtspMessage message)
        {
            string timestamp = (DateTime.UtcNow - m_EndConnect ?? TimeSpan.Zero).TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

            message.SetHeader(RtspHeaders.Timestamp, timestamp);
        }

        public RtspMessage SendRtspMessage(RtspMessage message, out SocketError error, out int sequenceNumber, bool useClientProtocolVersion = true, bool hasResponse = true, int attempts = 0)
        {
            //Indicate a send has not been attempted
            error = SocketError.SocketError;

            //Indicate the sequence number has not been observed
            sequenceNumber = -1;

            //Don't try to send if already disposed.
            CheckDisposed();

            bool wasBlocked = false;

            //Check for illegal feeding of turtles
            if (false == IDisposedExtensions.IsNullOrDisposed(message) && string.Compare("REGISTER", message.MethodString, true) == 0 && false == string.IsNullOrWhiteSpace(UserAgent)) throw new InvalidOperationException("Please don't feed the turtles.");

            unchecked
            {
                try
                {
                    int retransmits = 0, attempt = attempts, //The attempt counter itself
                        sent = 0, received = 0, //counter for sending and receiving locally
                        offset = 0, length = 0;

                    //Wait for the smallest amount of time possible.
                    //int pollTime = (int)(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(m_RtspSocket)) + Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(m_ConnectionTime)); //(int)Math.Round(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(m_RtspSessionTimeout) / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMillisecond, MidpointRounding.ToEven)

                    //Half of the session timeout in milliseconds
                    int halfTimeout = (int)(m_RtspSessionTimeout.TotalMilliseconds / 2);

                    byte[] buffer = null;

                    #region Check for a message

                    bool wasConnected = IsConnected;

                    //If there is no message to send then check for response
                    if (message == null) goto Connect;

                    #endregion

                    #region useClientProtocolVersion

                    //Ensure the request version matches the protocol version of the client if enforceVersion is true.
                    if (useClientProtocolVersion && message.Version != ProtocolVersion) message.Version = ProtocolVersion;

                    #endregion

                    #region Additional Headers

                    //Use any additional headers if given
                    if (AdditionalHeaders.Count > 0) foreach (var additional in AdditionalHeaders) message.AppendOrSetHeader(additional.Key, additional.Value);

                    #endregion

                    #region CSeq

                    //Get the next Sequence Number and set it in the request. (If not already present)
                    if (false == message.ContainsHeader(RtspHeaders.CSeq)) sequenceNumber = message.CSeq = NextClientSequenceNumber();
                    else sequenceNumber = message.CSeq;

                    #endregion

                    #region ContentEncoding

                    //Add the content encoding header if required
                    if (false == message.ContainsHeader(RtspHeaders.ContentEncoding) &&
                        message.ContentEncoding.WebName != RtspMessage.DefaultEncoding.WebName)
                        message.SetHeader(RtspHeaders.ContentEncoding, message.ContentEncoding.WebName);

                    #endregion

                    #region DateRequests

                    //Set the Date header if required
                    if (DateRequests && false == message.ContainsHeader(RtspHeaders.Date))
                        message.SetHeader(RtspHeaders.Date, DateTime.UtcNow.ToString("r"));

                    #endregion

                    #region SessionId

                    //Set the Session header if required and not already contained.
                    if (false == string.IsNullOrWhiteSpace(m_SessionId) &&
                        false == message.ContainsHeader(RtspHeaders.Session)) message.SetHeader(RtspHeaders.Session, m_SessionId);

                    #endregion

                    #region SendUserAgent

                    //Add the user agent if required
                    if (SendUserAgent &&
                        false == message.ContainsHeader(RtspHeaders.UserAgent))
                    {
                        message.SetHeader(RtspHeaders.UserAgent, m_UserAgent);
                    }


                    #endregion

                    #region Credentials

                    //If there not already an Authorization header and there is an AuthenticationScheme utilize the information in the Credential
                    if (false == message.ContainsHeader(RtspHeaders.Authorization) &&
                        m_AuthenticationScheme != AuthenticationSchemes.None && //Using this as an unknown value at first..
                        Credential != null)
                    {
                        //Basic
                        if (m_AuthenticationScheme == AuthenticationSchemes.Basic)
                        {
                            message.SetHeader(RtspHeaders.Authorization, RtspHeaders.BasicAuthorizationHeader(message.ContentEncoding, Credential));
                        }
                        else if (m_AuthenticationScheme == AuthenticationSchemes.Digest)
                        {
                            //Could get values from m_LastTransmitted.
                            //Digest
                            message.SetHeader(RtspHeaders.Authorization,
                                RtspHeaders.DigestAuthorizationHeader(message.ContentEncoding, message.RtspMethod, message.Location, Credential, null, null, null, null, null, false, null, message.Body));
                        }
                        else if (m_AuthenticationScheme != AuthenticationSchemes.None)
                        {
                            message.SetHeader(RtspHeaders.Authorization, m_AuthenticationScheme.ToString());
                        }
                    }

                    #endregion

                Timestamp:
                    #region Timestamp
                    //If requests should be timestamped
                    if (TimestampRequests) Timestamp(message);

                    //Take note of the timestamp of the message out
                    string timestampSent = message[RtspHeaders.Timestamp];

                    //Get the bytes of the request
                    buffer = m_RtspProtocol == ClientProtocolType.Http ? RtspMessage.ToHttpBytes(message) : message.ToBytes();

                    offset = m_Buffer.Offset;

                    length = buffer.Length;
                    #endregion

                Connect:
                    #region Connect
                    //Wait for any existing requests to finish first
                    wasBlocked = InUse;

                    //If was block wait for that to finish
                    //if (wasBlocked) m_InterleaveEvent.Wait();

                    if (false == wasConnected && false == (wasConnected = IsConnected)) Connect();

                    //If the client is not connected then nothing can be done.

                    //Othewise we are connected
                    if (false == (wasConnected = IsConnected)) return null;

                    //Set the block if a response is required.
                    if (hasResponse && false == wasBlocked) m_InterleaveEvent.Reset();


                    //If nothing is being sent this is a receive only operation
                    if (message == null) goto NothingToSend;

                    #endregion

                Send:
                    #region Send
                    //If the message was Transferred previously
                    if (message.Transferred.HasValue)
                    {
                        //Make the message not Transferred
                        message.Transferred = null;

                        //Increment counters for retransmit
                        ++retransmits;

                        ++m_ReTransmits;
                    }

                    //Because SocketReadTimeout or SocketWriteTimeout may be 0 do a read to avoid the abort of the connection.
                    //TCP RST occurs when the ACK is missed so keep the window open.
                    if (IsConnected
                        &&
                        Common.Extensions.Socket.SocketExtensions.CanRead(m_RtspSocket, m_SocketPollMicroseconds))
                    {
                        //Receive if data is actually available.
                        goto Receive;
                    }

                    //If we can write before the session will end
                    if (IsConnected
                        &&
                        Common.Extensions.Socket.SocketExtensions.CanWrite(m_RtspSocket, m_SocketPollMicroseconds))
                    {
                        sent += m_RtspSocket.Send(buffer, sent, length - sent, SocketFlags.None, out error);
                    }

                    #region Auto Reconnect

                    if (AutomaticallyReconnect &&
                        (error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset))
                    {
                        //Check for the host to have dropped the connection
                        if (error == SocketError.ConnectionReset)
                        {
                            //Check if the client was connected already
                            if (wasConnected && false == IsConnected)
                            {
                                Reconnect(true);

                                goto Send;
                            }
                        }

                        throw new SocketException((int)error);
                    }

                    #endregion

                    //If this is not a re-transmit
                    if (sent >= length)
                    {
                        //Set the time when the message was transferred if this is not a retransmit.
                        message.Transferred = DateTime.UtcNow;

                        //Fire the event (sets Transferred)
                        Requested(message);

                        //Increment for messages sent or the messages retransmitted.
                        ++m_SentMessages;

                        //Increment our byte counters for Rtsp
                        m_SentBytes += sent;

                        //Attempt to receive so start attempts back at 0
                        /*sent = */
                        attempt = 0;

                        //Release the reference to the array
                        buffer = null;
                    }
                    else if (sent < length && ++attempt < m_MaximumTransactionAttempts)
                    {
                        //Make another attempt @
                        //Sending the rest
                        goto Send;
                    }

                    #endregion

                NothingToSend:
                    #region NothingToSend
                    //Check for no response.
                    if (false == hasResponse) return null;

                    //If the socket is shared the response will be propagated via an event.
                    if (SharesSocket) goto Wait;
                    #endregion

                //Receive some data (only referenced by the check for disconnection)
                Receive:
                    #region Receive

                    //Let cache clear
                    //m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond);

                    //If IsConnected and we can receive 
                    if (IsConnected
                        &&                                                              //or this is the last attempt at recieving a messge
                        Common.Extensions.Socket.SocketExtensions.CanRead(m_RtspSocket, m_SocketPollMicroseconds) /*|| message != null && attempts == m_ResponseTimeoutInterval*/)
                    {
                        //Receive
                        received += m_RtspSocket.Receive(m_Buffer.Array, offset, m_Buffer.Count, SocketFlags.None, out error);
                    }

                    #region Auto Reconnect

                    if (AutomaticallyReconnect &&
                        (error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset))
                    {
                        //Check for the host to have dropped the connection
                        if (error == SocketError.ConnectionReset)
                        {
                            //Check if the client was connected already
                            if (wasConnected && false == IsConnected)
                            {
                                Reconnect(true);

                                goto Receive;
                            }
                        }

                        throw new SocketException((int)error);
                    }

                    #endregion

                    //If anything was received
                    if (received > 0)
                    {
                        //TODO
                        //RtspClient.TransportContext must handle the reception because it must strip away the RTSP and process only the interleaved data in a frame.
                        //Right now just pass it to the RtpClient.
                        if (m_RtpClient != null && m_Buffer.Array[offset] == Media.Rtp.RtpClient.BigEndianFrameControl)
                        {
                            //Some people just start sending packets hoping that the context will be created dynamically.
                            //I guess you could technically skip describe and just receive everything raising events as required...
                            //White / Black Hole Feature(s)? *cough*QuickTime*cough*

                            //Deliver any data which was intercepted to the underlying Transport.
                            //Any data handled in the rtp layer is should not count towards what was received.
                            //This can throw off the call if SharesSocket changes during the life of this call.
                            //In cases where this is off it can be fixed by using Clamp, it usually only occurs when someone is disconnecting.
                            received -= m_RtpClient.ProcessFrameData(m_Buffer.Array, offset, received, m_RtspSocket);

                            //Handle when the client received a lot of data and no response was found when interleaving.
                            //One possibility is transport packets such as Rtp or Rtcp.
                            if (received < 0) received = 0; // Common.Binary.Clamp(received, 0, m_Buffer.Count);                           
                        }
                        else
                        {
                            //Otherwise just process the data via the event.
                            ProcessInterleaveData(this, m_Buffer.Array, offset, received);
                        }
                    } //Nothing was received, if the socket is not shared
                    else if (false == SharesSocket)
                    {
                        //Check for non fatal exceptions and continue to wait
                        if (error != SocketError.ConnectionAborted || error != SocketError.ConnectionReset /*|| error != SocketError.Interrupted || error == SocketError.Success*/)
                        {
                            //We don't share the socket so go to recieve again (note if this is the timer thread this can delay outgoing requests)
                            if ((message != null && message.Transferred.HasValue && (int)(DateTime.UtcNow - message.Transferred.Value).TotalMilliseconds <= m_MaximumTransactionAttempts)
                                ||
                                ++attempt <= m_MaximumTransactionAttempts)
                            {
                                goto Wait;
                            }
                        }

                        //Todo, this isn't really needed once there is a thread monitoring the protocol.
                        //Right now it probably isn't really needed either.
                        //Raise the exception (may be success to notify timer thread...)
                        if (message != null) throw new SocketException((int)error);
                        else return null;
                    }

                    #endregion

                //Wait for the response while the amount of data received was less than RtspMessage.MaximumLength
                Wait:
                    #region Waiting for response, Backoff or Retransmit
                    DateTime lastAttempt = DateTime.UtcNow;

                    //Wait while
                    while (false == IsDisposed &&//The client connected and is not disposed AND
                        //There is no last transmitted message assigned AND it has not already been disposed
                        Common.IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted)
                        //AND the client is still allowed to wait
                        && ++attempt <= m_MaximumTransactionAttempts)
                    {
                        //Wait a small amount of time for the response because the cancellation token was not used...
                        if (IsDisposed)
                        {
                            return null;
                        }
                        else
                        {
                            //Wait a little more
                            m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);
                        }

                        //Check for any new messages
                        if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_LastTransmitted)) goto HandleResponse;

                        //Calculate how much time has elapsed
                        TimeSpan taken = DateTime.UtcNow - lastAttempt;

                        int readTimeout = SocketReadTimeout;

                        //If more time has elapsed than allowed by reading
                        if (readTimeout > 0 && taken > m_LastMessageRoundTripTime && taken.TotalMilliseconds >= readTimeout)
                        {
                            //Check if we can back off further
                            if (taken.TotalMilliseconds >= halfTimeout) break;
                            else if (readTimeout < halfTimeout)
                            {
                                //Backoff
                                /*pollTime += (int)(Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond */
                                SocketWriteTimeout = SocketReadTimeout *= 2;

                                //Ensure the client transport is connected if previously playing and it has since disconnected.
                                if (IsPlaying &&
                                    m_RtpClient != null &&
                                    false == m_RtpClient.IsActive) m_RtpClient.Activate();
                            }

                            //If the client was not disposed re-trasmit the request if there is not a response pending already.
                            //Todo allow an option for this feature? (AllowRetransmit)
                            if (false == IsDisposed && m_LastTransmitted == null /*&& request.Method != RtspMethod.PLAY*/)
                            {
                                //handle re-transmission under UDP
                                if (m_RtspSocket.ProtocolType == ProtocolType.Udp)
                                {
                                    //Change the Timestamp if TimestampRequests is true
                                    if (TimestampRequests)
                                    {
                                        //Reset what to send.
                                        sent = 0;

                                        goto Timestamp;
                                    }

                                    //Reset what was sent so far.
                                    sent = 0;

                                    //Retransmit the exact same data
                                    goto Send;
                                }
                            }
                        }

                        //If not sharing socket trying to receive again.
                        if (false == SharesSocket)
                        {
                            //This can throw the offsets off if was previously sharing socket
                            //received = 0;

                            //if (InUse) System.Threading.Thread.Sleep(0);

                            //m_InterleaveEvent.Wait(message == null ? Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond : Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                            m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                            //If we have a message to send and did not send it then goto send.
                            //message.Transferred.HasValue
                            if (message != null && sent == 0) goto Send;
                            goto Receive;
                        }
                    }

                    #endregion

                HandleResponse:
                    #region HandleResponse

                    //Update counters for any data received.
                    m_ReceivedBytes += received;

                    //If nothing was received wait for cache to clear.
                    if (null == m_LastTransmitted)
                    {
                        m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);
                    }
                    else if (message != null) //If there was a message sent
                    {
                        //Could also check session header.

                        //Obtain the CSeq of the response if present.
                        int sequenceNumberSent = message.CSeq, sequenceNumberReceived = m_LastTransmitted.CSeq;

                        //If the sequence number was present and did not match then wait again
                        if (sequenceNumberReceived != sequenceNumberSent)
                        {
                            //Reset the block
                            m_InterleaveEvent.Reset();

                            //Mark disposed
                            m_LastTransmitted.Dispose();

                            //Remove the message to avoid confusion
                            m_LastTransmitted = null;

                            //Allow more waiting
                            attempt = received = 0;

                            goto Wait;
                        }
                        //else if (m_LastTransmitted.ContainsHeader(RtspHeaders.Timestamp))
                        //{
                        //    //Todo
                        //    //Double check the Timestamp portion received is what was sent.
                        //    //if it's not this is a response to an older request which was retransmitted.
                        //}
                    } // end check m_LastTransmitted == null


                    #region Notes

                    //m_LastTransmitted is either null or not
                    //if it is not null it may not be the same response we are looking for. (mostly during threaded sends and receives)
                    //this could be dealt with by using a hash `m_Transactions` which holds requests which are sent and a space for their response if desired.
                    //Then a function GetMessage(message) would be able to use that hash to get the outgoing or incoming message which resulted.
                    //The structure of the hash would allow any response to be stored.

                    #endregion

                    //Check for the response if there was a message sent.
                    if (hasResponse &&
                        message != null && m_LastTransmitted != null &&
                        m_LastTransmitted.MessageType == RtspMessageType.Response)
                    {
                        //Calculate the amount of time taken to receive the message.
                        TimeSpan lastMessageRoundTripTime = (m_LastTransmitted.Created - (message.Transferred ?? message.Created));

                        //Ensure positive values for the RTT
                        //if (lastMessageRoundTripTime < TimeSpan.Zero) lastMessageRoundTripTime = lastMessageRoundTripTime.Negate();

                        //Assign it
                        m_LastMessageRoundTripTime = lastMessageRoundTripTime.Duration();

                        //TODO
                        //REDIRECT (Handle loops)
                        //if(m_LastTransmitted.StatusCode == RtspStatusCode.MovedPermanently)

                        switch (m_LastTransmitted.RtspStatusCode)
                        {
                            case RtspStatusCode.OK:
                                {
                                    //Ensure message is added to supported methods.
                                    SupportedMethods.Add(message.MethodString);

                                    break;
                                }
                            case RtspStatusCode.NotImplemented:
                                {
                                    SupportedMethods.Remove(m_LastTransmitted.MethodString);

                                    break;
                                }
                            case RtspStatusCode.MethodNotValidInThisState:
                                {
                                    if (m_LastTransmitted.ContainsHeader(RtspHeaders.Allow)) MonitorProtocol();

                                    break;
                                }
                            case RtspStatusCode.Unauthorized:
                                {
                                    //If we were not authorized and we did not give a nonce and there was an WWWAuthenticate header given then we will attempt to authenticate using the information in the header

                                    #region [Vivontek Information]
                                    //(Note for Vivontek you can still bypass the Auth anyway :)
                                    //http://www.coresecurity.com/advisories/vivotek-ip-cameras-rtsp-authentication-bypass
                                    #endregion

                                    //If there was a WWWAuthenticate header in the response
                                    if (m_LastTransmitted.ContainsHeader(RtspHeaders.WWWAuthenticate) &&
                                        Credential != null) //And there have been Credentials assigned
                                    {
                                        Received(message, m_LastTransmitted);

                                        //Return the result of Authenticating with the given request and response (forcing the request if the credentails have not already been tried)
                                        return Authenticate(message, m_LastTransmitted);
                                    }

                                    //break
                                    break;
                                }
                            case RtspStatusCode.RtspVersionNotSupported:
                                {
                                    //if enforcing the version
                                    if (useClientProtocolVersion)
                                    {
                                        //Read the version from the response
                                        ProtocolVersion = m_LastTransmitted.Version;

                                        //Send the request again. SHOULD USE out error, 
                                        return SendRtspMessage(message, useClientProtocolVersion);
                                    }

                                    //break
                                    break;
                                }
                            default: break;
                        }

                        #region EchoXHeaders

                        //If the client should echo X headers
                        if (EchoXHeaders)
                        {
                            //iterate for any X headers 

                            foreach (var xHeader in m_LastTransmitted.GetHeaders().Where(h => h.Length > 2 && h[1] == Common.ASCII.HyphenSign && char.ToLower(h[0]) == 'x'))
                            {
                                //If contained already then update
                                if (AdditionalHeaders.ContainsKey(xHeader))
                                {
                                    AdditionalHeaders[xHeader] += ((char)Common.ASCII.SemiColon).ToString() + m_LastTransmitted.GetHeader(xHeader).Trim();
                                }
                                else
                                {
                                    //Add
                                    AdditionalHeaders.Add(xHeader, m_LastTransmitted.GetHeader(xHeader).Trim());
                                }
                            }
                        }

                        #endregion

                        #region Parse Session Header

                        //For any other request besides teardown update the sessionId and timeout
                        if (message.RtspMethod != RtspMethod.TEARDOWN)
                        {
                            //Get the header.
                            string sessionHeader = m_LastTransmitted[RtspHeaders.Session];

                            //If there is a session header it may contain the option timeout
                            if (false == string.IsNullOrWhiteSpace(sessionHeader))
                            {
                                //Check for session and timeout

                                //Get the values
                                string[] sessionHeaderParts = sessionHeader.Split(RtspHeaders.SemiColon);

                                int headerPartsLength = sessionHeaderParts.Length;

                                //Check if a valid value was given
                                if (headerPartsLength > 0)
                                {
                                    //Trim it of whitespace
                                    string value = sessionHeaderParts.LastOrDefault(p => false == string.IsNullOrWhiteSpace(p));

                                    //If we dont have an exiting id then this is valid if the header was completely recieved only.
                                    if (false == string.IsNullOrWhiteSpace(value) &&
                                        true == string.IsNullOrWhiteSpace(m_SessionId) ||
                                        value[0] != m_SessionId[0])
                                    {
                                        //Get the SessionId if present
                                        m_SessionId = sessionHeaderParts[0].Trim();

                                        //Check for a timeout
                                        if (sessionHeaderParts.Length > 1)
                                        {
                                            int timeoutStart = 1 + sessionHeaderParts[1].IndexOf(Media.Sdp.SessionDescription.EqualsSign);
                                            if (timeoutStart >= 0 && int.TryParse(sessionHeaderParts[1].Substring(timeoutStart), out timeoutStart))
                                            {
                                                //Should already be set...
                                                if (timeoutStart <= 0)
                                                {
                                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(60);//Default
                                                }
                                                else
                                                {
                                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(timeoutStart);
                                                }
                                            }
                                        }
                                    }

                                    //done
                                }
                                else if (string.IsNullOrWhiteSpace(m_SessionId))
                                {
                                    //The timeout was not present
                                    m_SessionId = sessionHeader.Trim();

                                    m_RtspSessionTimeout = TimeSpan.FromSeconds(60);//Default
                                }
                            }
                        }

                        #endregion

                        #region CalculateServerDelay

                        if (CalculateServerDelay)
                        {
                            string timestamp;

                            RtspHeaders.TryParseTimestamp(m_LastTransmitted[RtspHeaders.Timestamp], out timestamp, out m_LastServerDelay);

                            timestamp = null;
                        }

                        #endregion

                        #region UpdateSession

                        //Update the session related
                        RtspSession related;

                        if (m_Sessions.TryGetValue(m_SessionId, out related))
                        {
                            related.UpdateMessages(message, m_LastTransmitted);

                            related = null;
                        }

                        #endregion

                        //Raise an event for the message received
                        Received(message, m_LastTransmitted);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    Common.ILoggingExtensions.Log(Logger, ToString() + "@SendRtspMessage: " + ex.Message);
                }
                finally
                {
                    //Unblock (should not be needed)
                    if (false == wasBlocked) m_InterleaveEvent.Set();
                }

                //Return the result
                return message != null && m_LastTransmitted != null && message.CSeq == m_LastTransmitted.CSeq ? m_LastTransmitted : null;

            }//Unchecked
        }

        /// <summary>
        /// Sends the Rtsp OPTIONS request
        /// </summary>
        /// <param name="useStar">The OPTIONS * request will be sent rather then one with the <see cref="RtspClient.CurrentLocation"/></param>
        /// <returns>The <see cref="RtspMessage"/> as a response to the request</returns>
        public RtspMessage SendOptions(bool useStar = false, string sessionId = null)
        {
            using (var options = new RtspMessage(RtspMessageType.Request)
            {
                RtspMethod = RtspMethod.OPTIONS,
                Location = useStar ? null : CurrentLocation,
                IsPersistent = true,
            })
            {
                if (false == string.IsNullOrWhiteSpace(sessionId)) options.SetHeader(RtspHeaders.Session, sessionId);

                RtspMessage response = SendRtspMessage(options);

                if (response != null)
                {
                    //Get the Public header which indicates the methods supported by the client
                    string publicMethods = response[RtspHeaders.Public];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(publicMethods))
                    {
                        //Process values in the Public header.
                        foreach (string method in publicMethods.Split(RtspHeaders.Comma))
                        {
                            SupportedMethods.Add(method.Trim());
                        }
                    }

                    //Should have a way to keep the allowed seperate.
                    string allowedMethods = response[RtspHeaders.Allow];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(allowedMethods))
                    {
                        //Process values in the Public header.
                        foreach (string method in allowedMethods.Split(RtspHeaders.Comma))
                        {
                            SupportedMethods.Add(method.Trim());
                        }
                    }

                    //Some servers only indicate different features at the SETUP level...

                    string supportedFeatures = response[RtspHeaders.Supported];

                    //If there is Not such a header then return the response
                    if (false == string.IsNullOrWhiteSpace(supportedFeatures))
                    {
                        //Process values in the Public header.
                        foreach (string method in supportedFeatures.Split(RtspHeaders.Comma))
                        {
                            SupportedFeatures.Add(method.Trim());
                        }
                    }
                }
                else if (false == IsPlaying) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Unable to get options, See InnerException.", new Common.TaggedException<RtspMessage>(response, "See Tag for Response."));

                options.IsPersistent = false;

                return response;
            }
        }

        /// <summary>
        /// Assigns the SessionDescription returned from the server
        /// </summary>
        /// <returns></returns>
        public RtspMessage SendDescribe() //bool force, bool followRedirects
        {

            RtspMessage response = null;

            try
            {
                using (RtspMessage describe = new RtspMessage(RtspMessageType.Request)
                {
                    RtspMethod = RtspMethod.DESCRIBE,
                    Location = CurrentLocation,
                    IsPersistent = true
                })
                {
                    #region Reference

                    // The DESCRIBE method retrieves the description of a presentation or
                    // media object identified by the request URL from a server. It may use
                    // the Accept header to specify the description formats that the client
                    // understands. The server responds with a description of the requested
                    // resource. The DESCRIBE reply-response pair constitutes the media
                    // initialization phase of RTSP.

                    #endregion

                    describe.SetHeader(RtspHeaders.Accept, Sdp.SessionDescription.MimeType);

                Describe:
                    response = SendRtspMessage(describe);

                    //Handle no response
                    //If the remote end point is just sending Interleaved Binary Data out of no where it is possible to continue without a SessionDescription

                    if (response == null) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(describe, "Unable to describe media, no response to DESCRIBE request. The request is in the Tag property.");
                    else response.IsPersistent = true;

                    //Hanlde NotFound
                    if (response.RtspStatusCode == RtspStatusCode.NotFound) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(describe, "Unable to describe media, NotFound. The response is in the Tag property.");

                    if (false == response.IsComplete)
                    {
                        //Wait for complete responses
                        if (SharesSocket)
                        {
                            m_InterleaveEvent.Wait();
                        }
                        else
                        {
                            response.CompleteFrom(m_RtspSocket, m_Buffer);
                        }
                    }

                    //Only handle responses for the describe request sent when sharing the socket
                    if (response.CSeq != describe.CSeq)
                    {
                        describe.RemoveHeader(RtspHeaders.Timestamp);

                        goto Describe;
                    }

                    //don't handle erroneous responses
                    if (describe.RtspStatusCode > RtspStatusCode.OK)
                    {
                        return describe;
                    }


                    //Handle Found / Redirect
                    //When the response is <= OK the content-base header is present it should also be checked for NAT
                    //If the remote party needs it's NAT Address to be used then uncomment out the check for <= OK 
                    if (response.RtspStatusCode <= Rtsp.RtspStatusCode.OK ||
                        response.RtspStatusCode == RtspStatusCode.Found ||
                        response.RtspMethod == RtspMethod.REDIRECT)
                    {
                        //Determine if there is a new location
                        string newLocation = response.GetHeader(RtspHeaders.Location);

                        if (false == string.IsNullOrWhiteSpace(newLocation))
                        {
                            newLocation = newLocation.Trim();
                        }

                        //We start at our location
                        Uri baseUri = CurrentLocation;

                        //Get the contentBase header
                        string contentBase = response[RtspHeaders.ContentBase];

                        //If it was present
                        if (false == string.IsNullOrWhiteSpace(contentBase) &&
                            //Try to create it from the string
                            Uri.TryCreate(contentBase, UriKind.RelativeOrAbsolute, out baseUri))
                        {
                            //If it was not absolute
                            if (false == baseUri.IsAbsoluteUri)
                            {
                                //Try to make it absolute and if not try to raise an exception
                                if (false == Uri.TryCreate(CurrentLocation, baseUri, out baseUri))
                                {
                                    Media.Common.TaggedExceptionExtensions.RaiseTaggedException(contentBase, "See Tag. Can't parse ContentBase header.");
                                }
                            }

                            //The new location is given by
                            newLocation = baseUri.ToString();
                        }

                        Uri parsedLocation;

                        //UriDecode?

                        //Try to parse it if not null or empty
                        if (false == string.IsNullOrWhiteSpace(newLocation) &&
                            Uri.TryCreate(baseUri, newLocation, out parsedLocation) &&
                            parsedLocation != CurrentLocation) // and not equal the existing location
                        {

                            if (parsedLocation.IsAbsoluteUri &&
                                parsedLocation.OriginalString.Last() == (char)Common.ASCII.ForwardSlash)
                            {

                                //Could only take the different part of the location with the following code
                                //parsedLocation.MakeRelativeUri(Location)

                                //Redirect to the Location by setting Location. (Allows a new host)
                                m_CurrentLocation = new Uri(parsedLocation.OriginalString.Substring(0, parsedLocation.OriginalString.Length - 1));
                            }
                            else
                            {

                                //Could only take the different part of the location with the following code
                                //parsedLocation.MakeRelativeUri(Location)

                                //Redirect to the Location by setting Location. (Allows a new host and Connects when using CurrentLoction)
                                m_CurrentLocation = parsedLocation;
                            }


                            //Send a new describe
                            if (response.RtspStatusCode == RtspStatusCode.Found || response.RtspMethod == RtspMethod.REDIRECT)
                            {
                                //the old response would possibly leak.
                                response.IsPersistent = false;

                                //response.Dispose();

                                //response = null;

                                return response = SendDescribe() ?? response;
                            }
                        }
                    }

                    string contentType = response[RtspHeaders.ContentType];

                    //Handle any not ok response (allow Continue)
                    //Handle MultipleChoice for Moved or ContentType...
                    if (response.RtspStatusCode >= RtspStatusCode.MultipleChoices && false == string.IsNullOrEmpty(contentType) && string.Compare(contentType.TrimStart(), Sdp.SessionDescription.MimeType, true) != 0)
                    {
                        Media.Common.TaggedExceptionExtensions.RaiseTaggedException(response.RtspStatusCode, "Unable to describe media. The StatusCode is in the Tag property.");
                    }
                    else if (string.IsNullOrWhiteSpace(response.Body))
                    {
                        Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Unable to describe media, Missing Session Description");
                    }

                    #region MS-RTSP

                    //////Not really needed

                    ////string playListId = response["X-Playlist-Gen-Id"];

                    ////if (false == string.IsNullOrWhiteSpace(playListId))
                    ////{
                    ////    AdditionalHeaders.Add("X-Playlist-Gen-Id", playListId.Trim());
                    ////}

                    //// Should also do a SET_PARAMETER
                    //Content-type: application/x-rtsp-udp-packetpair;charset=UTF-8\r\n\r\n
                    //Content-Length: X \r\n
                    //type: high-entropy-packetpair variable-size

                    #endregion

                    //Try to create a session description even if there was no contentType so long as one was not specified against sdp.
                    m_SessionDescription = new Sdp.SessionDescription(response.Body);

                    //No longer being used.
                    describe.IsPersistent = false;
                }
            }
            catch (Common.TaggedException<RtspClient>)
            {
                throw;
            }
            catch (Common.TaggedException<SessionDescription> sde)
            {
                Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Unable to describe media, Session Description Exception Occured.", sde);
            }
            catch (Exception ex) { if (ex is Media.Common.ITaggedException) throw ex; Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "An error occured", ex); }

            //Return the response
            return response;
        }

        /// <summary>
        /// Sends a request which will remove the session given from the server using a TEARDOWN * request.
        /// </summary>
        /// <param name="sessionId">The sessionId to remove, if null the current <see cref="SessionId"/> will be used if possible.</param>
        /// <param name="closeConnection">Indicates if the `Connection` header of the request should be set to 'Close'</param>
        /// <returns></returns>
        public virtual RtspMessage RemoveSession(string sessionId, bool closeConnection = false)
        {
            using (var teardown = new RtspMessage(RtspMessageType.Request))
            {
                teardown.RtspMethod = RtspMethod.TEARDOWN;

                if (closeConnection) teardown.SetHeader(RtspHeaders.Connection, "Close");

                sessionId = sessionId ?? m_SessionId;

                if (false == string.IsNullOrWhiteSpace(sessionId)) teardown.SetHeader(RtspHeaders.Session, sessionId);

                //Calling on stopping here indicates all sessions end...
                //SHould get the session by id and then use it's media description in the event.
                OnStopping();

                try { return SendRtspMessage(teardown); }
                finally { m_SessionId = null; }
            }
        }

        public RtspMessage SendTeardown(MediaDescription mediaDescription = null, bool disconnect = false, bool force = false)
        {
            RtspMessage response = null;

            //Check if the session supports pausing a specific media item
            if (mediaDescription != null && false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

            //only send a teardown if not forced and the client is playing
            if (false == force && false == IsPlaying) return response;

            try
            {
                //If there is a client then stop the flow of this media now with RTP
                if (m_RtpClient != null)
                {
                    //Send a goodbye for all contexts if the mediaDescription was not given
                    if (mediaDescription == null)
                    {
                        if (false == SharesSocket) m_RtpClient.Deactivate();
                        else m_RtpClient.SendGoodbyes();
                    }
                    else//Find the context for the description
                    {
                        //Get a context
                        RtpClient.TransportContext context = m_RtpClient.GetContextForMediaDescription(mediaDescription);

                        //If context was determined then send a goodbye
                        if (context != null)
                        {
                            //Send a goodbye now (but still allow reception)
                            m_RtpClient.SendGoodbye(context);

                            //Dispose context?

                            //Remove the reference
                            context = null;
                        }
                    }
                }

                //Keep track of whats playing
                if (mediaDescription == null)
                {
                    m_Playing.Clear();

                    //LeaveOpen = false;
                }
                else m_Playing.Remove(mediaDescription);

                //The media is stopping now.
                OnStopping(mediaDescription);

                //Return the result of the Teardown
                using (var teardown = new RtspMessage(RtspMessageType.Request)
                {
                    RtspMethod = RtspMethod.TEARDOWN,
                    Location = mediaDescription != null ? mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) : CurrentLocation
                })
                {
                    //Set the close header if disconnecting
                    if (disconnect) teardown.SetHeader(RtspHeaders.Connection, "close");

                    //Send the request and if not closing the connecting then wait for a response
                    return SendRtspMessage(teardown, true, false == disconnect);
                }

            }
            catch (Common.TaggedException<RtspClient>)
            {
                return response;
            }
            catch
            {
                throw;
            }
            finally
            {
                //Ensure the sessionId is invalided when no longer playing if not forced
                if (false == force && false == IsPlaying) m_SessionId = null;
            }
        }

        public RtspMessage SendSetup(MediaDescription mediaDescription)
        {
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Send the setup
            return SendSetup(mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription), mediaDescription);
        }

        /// <summary>
        /// Sends a SETUP Rtsp Request based on the given parameters. If no response is obtained a second response will be sent.
        /// If no response is recieved the client WILL attempt to proceed as if SETUP has succeeded and setup any transport resources which would be required.
        /// If the response indicates the session is not found and there is not already an existing <see cref="SessionId"/> the request will be repeated without a Session header.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mediaDescription"></param>
        /// <param name="unicast"></param>
        /// <returns></returns>
        //Remove unicast... and allow for session based setup
        internal RtspMessage SendSetup(Uri location, MediaDescription mediaDescription, bool unicast = true)//False to use manually set protocol
        {
            if (location == null) throw new ArgumentNullException("location");

            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Todo Setup should only create a TransportContext which COULD then be given to a RtpClient 
            //This will allow for non RTP transports to be used such as MPEG-TS.

            //Could also use out bool allocatedContext...

            //Must de-coulple the RtpClient and replace it

            //Todo, Add CreateSession logic here and give the session to SendSetup.
            //Move this logic to the overload with the RtspSession,

            //e.g. something like this SendSetup(GetOrCreateSession(mediaDescription))
            //But should ensure that sessions are not created when they are not supposed to be e.g. when the response is not a success. 

            //Determine if a client_port datum should be sent in the Transport header as a set, list or a single entry
            bool needsRtcp = true, multiplexing = false;

            #region [WMS Notes / Log]

            //Some sources indicate that rtx must be or must not be setup
            //They also say that only one ssrc should be sent
            //There are various different advices.

            //Check the spec if you have doubts https://msdn.microsoft.com/en-us/library/cc245238.aspx

            /* WMS Server
             1. 
            client -->
            SETUP rtsp://s-media1/spider_od/rtx RTSP/1.0
            Transport:
            RTP/AVP/UDP;unicast;client_port=1206-1207;ssrc=9cef6565;mode=PLAY

            server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004-5005;client_port=1206-1207;ssrc=e34
            90f0d;mode=PLAY

            2. 
            client -->
            SETUP rtsp://s-media1/spider_od/audio RTSP/1.0
            Transport: RTP/AVP/UDP;unicast;client_port=1208;ssrc=9a789797;mode=PLAY 

            Server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004;client_port=1208;ssrc=8873c0ac;mode
            =PLAY

            3. 
            client -->
            SETUP rtsp://s-media1/spider_od/video RTSP/1.0
            Transport: RTP/AVP/UDP;unicast;client_port=1208;ssrc=275f7979;mode=PLAY

            Server <--
            RTSP/1.0 200 OK
            Transport:
            RTP/AVP/UDP;unicast;server_port=5004;client_port=1208;ssrc=8873c0cf;mode
            =PLAY
             */

            ////Keep the values parsed from the description
            //int rr, rs, a;

            ////Attempt to parse them
            //if (RtpClient.TransportContext.TryParseBandwidthDirectives(mediaDescription, out rr, out rs, out a) &&
            //    rr == 0 && //If the rr AND
            //    rs == 0/* && a == 0*/) // rs directive specified 0 (Should check AS?)
            //{
            //    //RTSP is not needed
            //    needsRtcp = false;
            //}

            ////Rtx streams for a WMS Server always require RTCP?
            ////Should ensure this convention doesn't interfere with names where are not for WMS
            ////Possible check server header in m_LastTransmitted
            //if (location.AbsoluteUri.EndsWith("rtx", StringComparison.OrdinalIgnoreCase)) needsRtcp = true;

            #endregion

            try
            {
                //Should either create context NOW or use these sockets in the created context.

                //Create sockets to reserve the ports we think we will need.
                Socket rtpTemp = null, rtcpTemp = null;

                using (RtspMessage setup = new RtspMessage(RtspMessageType.Request)
                {
                    RtspMethod = RtspMethod.SETUP,
                    Location = location ?? CurrentLocation
                })
                {

                    //Values in the header we need
                    int clientRtpPort = -1, clientRtcpPort = -1,
                        serverRtpPort = -1, serverRtcpPort = -1,
                        //Darwin and Wowza uses this ssrc, VLC/Live Gives a Unsupported Transport, WMS and most others seem to ignore it.
                        localSsrc = 0,//RFC3550.Random32(),  
                        remoteSsrc = 0;

                    //Should check this.
                    //m_RtspSocket.LocalEndPoint.AddressFamily != AddressFamily.InterNetwork && m_RtspSocket.LocalEndPoint.AddressFamily != AddressFamily.InterNetworkV6

                    //Cache this to prevent having to go to get it every time down the line
                    IPAddress localIp = ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address,
                        sourceIp = localIp.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, //Todo, may not be Internetwork...
                        destinationIp = sourceIp;

                    string mode;

                    //Todo, check the MediaDescription ConnectionLine for the ConnectionAddress to have a Multicast address
                    bool multicast = false, interleaved = m_RtpProtocol == ProtocolType.Tcp;

                    byte dataChannel = 0, controlChannel = 1;

                    //Todo, get this from the connection address.
                    int ttl = 255;

                    //8? should be determine by rtcp enabled and the type of packets, rtcp 4, rtp 12
                    int minimumPacketSize = 8, maximumPacketSize = (ushort)m_Buffer.Count;

                    //12.7 Blocksize
                    /*
                     This request header field is sent from the client to the media server
                        asking the server for a particular media packet size. This packet
                        size does not include lower-layer headers such as IP, UDP, or RTP.
                        The server is free to use a blocksize which is lower than the one
                        requested. The server MAY truncate this packet size to the closest
                        multiple of the minimum, media-specific block size, or override it
                        with the media-specific size if necessary. The block size MUST be a
                        positive decimal number, measured in octets. The server only returns
                        an error (416) if the value is syntactically invalid.
                     */

                    //This is important if the server can support it, it will ensure that packets can fit in the buffer.
                    //It also tells the server what our buffer size is so if they wanted they could intentionally make packets which allowed only a certain amount of bytes remaining in the buffer....

                    //This NEEDS TO BE BASED AROUND THE LIMITS OF THE NETWORK MTU AND MSS
                    if (SendBlocksize) setup.SetHeader(RtspHeaders.Blocksize, m_Buffer.Count.ToString());

                    //TODO
                    // IF TCP was specified or the MediaDescription specified we need to use Tcp as specified in RFC4571
                    // DETERMINE if only 1 channel should be sent in the TransportHeader if we know RTCP is not going to be used. (doing so would force RTCP to stay disabled the entire stream unless a SETUP for just the RTCP occured, how does one setup just a RTCP session..)

                    //Send any supported headers?

                    //if (false == setup.ContainsHeader(RtspHeaders.Supported))
                    //{
                    //setup.SetHeader(RtspHeaders.Supported, "play.basic, play.scale, play.speed, con.persistent, con.independent, con.transient, rtp.mux, rtcp.mux, ts.mux, raw.mux, mux.*");
                    //setup.SetHeader(RtspHeaders.Supported, string.Join(Sdp.SessionDescription.SpaceString, SupportedFeatures));
                    //}

                    //Required: header (RequiredFeatures)
                    if (RequiredFeatures.Count > 0 && false == setup.ContainsHeader(RtspHeaders.Require))
                    {
                        setup.SetHeader(RtspHeaders.Require, string.Join(Sdp.SessionDescription.SpaceString, RequiredFeatures));
                    }

                    //Todo, Have field / property for PortRanges
                    //keep track to avoid exceptions if possible.
                    int lastPortUsed = 10000;

                    //Interleaved
                    if (interleaved)
                    {

                        //Check for option UseNewConnectionForEachTrack
                        //If enabled then create a new socket

                        //else{

                        //RTCP-mux:

                        //If there is already a RtpClient with at-least 1 TransportContext
                        if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                        {
                            RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().Last();

                            if (lastContext != null && lastContext.IsActive)
                            {
                                setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc != 0 ? localSsrc : (int?)null, null, null, null, null, null, true, false, null, true, dataChannel = (byte)(lastContext.DataChannel + 2), (needsRtcp ? (byte?)(controlChannel = (byte)(lastContext.ControlChannel + 2)) : null), null));
                            }
                            else
                            {
                                setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/TCP", localSsrc != 0 ? localSsrc : (int?)null, null, null, null, null, null, true, false, null, true, dataChannel, (needsRtcp ? (byte?)controlChannel : null), null));
                            }
                        }

                    }
                    else if (string.Compare(mediaDescription.MediaProtocol, RtpClient.RtpAvpProfileIdentifier, true) == 0) // We need to find an open Udp Port
                    {

                        //Revise
                        //Is probably Ip, set to Udp
                        m_RtpProtocol = ProtocolType.Udp;

                        //If multicast then the port doesn't have to be reserved, may need reuse port.
                        if (false == multicast)
                        {
                            //Could send 0 to have server pick port?                        

                            if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                            {
                                RtpClient.TransportContext lastContext = m_RtpClient.GetTransportContexts().Last();

                                if (lastContext != null && lastContext.IsActive)
                                {
                                    lastPortUsed = ((IPEndPoint)lastContext.LocalRtcp).Port + 1;
                                }
                                else
                                {
                                    lastPortUsed = 9999;
                                }
                            }

                            //Should allow this to be given or set as a property MinimumUdpPort, MaximumUdpPort                        
                            int openPort = Media.Common.Extensions.Socket.SocketExtensions.ProbeForOpenPort(ProtocolType.Udp, lastPortUsed + 1, true);

                            if (openPort == -1) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Could not find open Udp Port");
                            //else if (MaximumUdp.HasValue && openPort > MaximumUdp)
                            //{
                            //    Media.Common.Extensions.Exceptions.ExceptionExtensions.CreateAndRaiseException(this, "Found Udp Port > MaximumUdp. Found: " + openPort);
                            //}    

                            rtpTemp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, clientRtpPort = openPort);

                            //Check for muxing of rtp and rtcp on the same physical port
                            if (mediaDescription.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts.Any(p => p.ToLowerInvariant() == "rtcp-mux")).Any())
                            {
                                //Might not 'need' it
                                needsRtcp = multiplexing = true;

                                //Use the same port
                                clientRtcpPort = clientRtpPort;
                            }
                            else if (needsRtcp)
                            {
                                //Should probably check for open port again...

                                rtcpTemp = Media.Common.Extensions.Socket.SocketExtensions.ReservePort(SocketType.Dgram, ProtocolType.Udp, ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address, (clientRtcpPort = (openPort == ushort.MaxValue || openPort == 0 ? openPort : openPort + 1)));
                            }
                        }

                        //Supposedly
                        //WMS Server will complain if there is a RTCP port and no RTCP is allowed.
                        //More then likely only Ross will complain or his shitty software.

                        //Should allow a Rtcp only setup? would be a different profile...
                        setup.SetHeader(RtspHeaders.Transport, RtspHeaders.TransportHeader(RtpClient.RtpAvpProfileIdentifier + "/UDP", localSsrc != 0 ? localSsrc : (int?)null, null, clientRtpPort, (needsRtcp ? (int?)(clientRtcpPort) : null), null, null, false == multicast, multicast, null, false, 0, 0, RtspMethod.PLAY.ToString()));
                    }
                    else throw new NotSupportedException("The required Transport is not yet supported.");

                    SocketError error;

                    bool triedTwoTimes = false;

                    int sequenceNumber;

                Setup:
                    //Get the response for the setup
                    RtspMessage response = SendRtspMessage(setup, out error, out sequenceNumber);

                    //if there was no response then don't attempt to parse any but DO attempt to listen.
                    if (false == triedTwoTimes &&
                        (response == null //The response is null
                        ||
                        //If the error was not success OR
                        (error != SocketError.Success
                        || //The response is not null and was not actually a response OR the sequenceNumber does not match.
                        (response.MessageType != RtspMessageType.Response || response.CSeq != sequenceNumber))))
                    {
                        if (IsPlaying) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "No response to SETUP." + (false == SupportedMethods.Contains(RtspMethod.SETUP.ToString()) ? " The server may not support SETUP." : string.Empty));
                        else
                        {
                            //Handle host dropping the connection
                            if (error == SocketError.ConnectionAborted || error == SocketError.ConnectionReset)
                            {
                                if (AutomaticallyReconnect) Reconnect();
                                else Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Connection Aborted or Reset and AutomaticallyReconnect is false.");
                            }

                            //make another request if we didn't already try.
                            if (false == triedTwoTimes)
                            {
                                //Use a new Sequence number
                                setup.RemoveHeader(RtspHeaders.CSeq);

                                //Use a new Timestamp
                                setup.RemoveHeader(RtspHeaders.Timestamp);

                                //Dont try again
                                triedTwoTimes = true;

                                //Wait to clear caches.
                                m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                                goto Setup;
                            }
                        }
                    }

                    //Get a session ready                    
                    RtspSession session;

                    //Create a RtspSession if there is not already one assoicated with the current sessionId
                    if (false == m_Sessions.TryGetValue(SessionId, out session))
                    {
                        //Create a session
                        session = new RtspSession(setup, response)
                        {
                            EnableKeepAliveRequest = false == DisableKeepAliveRequest,
                            ControlLocation = location
                        };

                        //Only if there was a sessionId found
                        if (false == string.IsNullOrWhiteSpace(session.SessionId))
                        {
                            //Add the session created
                            m_Sessions.Add(SessionId, session);
                        }
                    }

                    //Ensure there was a response
                    //if (response == null) goto NoResponse;

                    //Todo, check if this is how events should be handled.
                    if (null == response) return response;

                    //Response not OK
                    if (response.RtspStatusCode != RtspStatusCode.OK)
                    {
                        //Transport requested not valid
                        if (response.RtspStatusCode == RtspStatusCode.UnsupportedTransport && m_RtpProtocol != ProtocolType.Tcp)
                        {
                            goto SetupTcp;
                        }
                        else if (response.RtspStatusCode == RtspStatusCode.SessionNotFound && //If the session was not found
                            false == string.IsNullOrWhiteSpace(m_SessionId) && //And there IS an existing session id
                            false == triedTwoTimes) //And setup has not already been attempted two times.
                        {

                            //Dispose old sessions?

                            //Erase the old session id
                            m_SessionId = string.Empty;

                            //Attempt the setup again
                            return SendSetup(location, mediaDescription);
                        }
                        else //Not Ok and not Session Not Found
                        {
                            //If there was an initial location and that location's host is different that the current location's host
                            if (m_InitialLocation != null && location.Host != m_InitialLocation.Host)
                            {
                                //You would have thought that the resource we were directed to would be able to handle it's own DNS routing even when it's not tunneled through IPv4

                                //Try to use the old location
                                location = mediaDescription.GetAbsoluteControlUri(m_InitialLocation, SessionDescription);

                                //Don't try again
                                triedTwoTimes = true;

                                goto Setup;
                            }

                            //Media.Common.TaggedExceptionExtensions.RaiseTaggedException(response.StatusCode, "Unable to setup media. The status code is in the Tag property.");

                            //Return the response given
                            return response;
                        }
                    }

                    //Handle the servers response for Blocksize                    

                    string blockSize = response[RtspHeaders.Blocksize];

                    if (false == string.IsNullOrWhiteSpace(blockSize))
                    {
                        //Extract the value (Should account for ';' in some way)
                        blockSize = Media.Common.ASCII.ExtractNumber(blockSize.Trim());

                        try
                        {
                            //Parse it...
                            maximumPacketSize = int.Parse(blockSize, System.Globalization.NumberStyles.Integer);

                            //If the packets cannot fit in the buffer
                            if (maximumPacketSize > m_Buffer.Count)
                            {
                                //Try to allow processing
                                Media.Common.TaggedExceptionExtensions.RaiseTaggedException(maximumPacketSize, "Media Requires a Larger Buffer. (See Tag for value)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Media.Common.TaggedExceptionExtensions.RaiseTaggedException(response, "BlockSize of the response needs consideration. (See Tag for response)", ex);
                        }
                    }

                    //Handle Rtcp-Interval (eventually, or definitely when it becomes a standard header :P)

                    //Handle anything else

                    //When jumping here we return null even though we allocate a context.
                NoResponse:

                    //We SHOULD have a valid TransportHeader in the response
                    //Get the transport header from the response if present.
                    session.TransportHeader = response != null ? response[RtspHeaders.Transport] : null;

                    //If there was no return transport header then we don't know what ports to utilize for reception.
                    if (string.IsNullOrWhiteSpace(session.TransportHeader))
                    {
                        //Discover them when receiving from the host
                        serverRtpPort = 0;

                        serverRtcpPort = 0;
                    }
                    else
                    {
                        //Check for the RTP token to ensure the underlying tranport is supported.
                        //Eventually any type such as RAW etc will be supported.
                        if (false == session.TransportHeader.Contains("RTP")
                        ||
                        false == RtspHeaders.TryParseTransportHeader(session.TransportHeader,
                        out remoteSsrc, out sourceIp, out serverRtpPort, out serverRtcpPort, out clientRtpPort, out clientRtcpPort,
                        out interleaved, out dataChannel, out controlChannel, out mode, out unicast, out multicast, out destinationIp, out ttl))
                            Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Cannot setup media, Invalid Transport Header in Rtsp Response: " + session.TransportHeader);
                    }

                    //If the server returns a channel which is already in use
                    //it then determines if there is an existing channel already utilized by this client with a different socket.
                    //If there is, then nothing neeed to be created just updated.
                    //Todo
                    //Care should be taken that the SDP is not directing us to connect to some unknown resource....

                    //Just incase the source datum was not given, only for unicast connections.
                    if (false == multicast && sourceIp.Equals(IPAddress.Any) || sourceIp.Equals(IPAddress.IPv6Any)) sourceIp = ((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address;

                    //If multicast was given check the destination address and if was not specified use the sourceIp.
                    if (multicast && (destinationIp.Equals(IPAddress.Any) || destinationIp.Equals(IPAddress.IPv6Any))) destinationIp = sourceIp;

                    //Create the context (determine if the session rangeLine may also be given here, if it gets parsed once it doesn't need to be parsed again)
                    RtpClient.TransportContext created = null;

                    //If interleaved was present in the response then use a RTP/AVP/TCP Transport
                    if (interleaved)
                    {
                        //If there is a client which is not disposed
                        if (m_RtpClient != null && false == m_RtpClient.IsDisposed)
                        {
                            //Obtain a context via the given data channel or control channel
                            created = m_RtpClient.GetContextByChannels(dataChannel, controlChannel);

                            //If the control channel is the same then just update the socket used by the context.
                            if (created != null &&
                                false == created.IsDisposed)
                            {
                                //created's Rtp and Rtcp Socket could be changed right here...
                                if (m_RtspSocket != null) created.Initialize(m_RtspSocket, m_RtspSocket);

                                //Test using a new socket
                                //if (m_RtspSocket != null) created.Initialize((IPEndPoint)m_RtspSocket.LocalEndPoint, (IPEndPoint)m_RtspSocket.RemoteEndPoint);

                                //Store the SessionId on the context.
                                created.ApplicationContext = SessionId;

                                session.Context = created;

                                //Not using the reference anymore
                                created = null;

                                return response;
                            }
                        }

                        //If a context was not already created
                        if (created == null || created.IsDisposed)
                        {
                            //Todo, should still be sourceIp...

                            //Create the context if required.. (Will be created with Sdp Address)
                            created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, dataChannel, controlChannel, mediaDescription, needsRtcp, remoteSsrc, remoteSsrc != 0 ? 0 : 2, localIp, sourceIp);

                            //Set the identity to what we indicated to the server.
                            created.SynchronizationSourceIdentifier = localSsrc;

                            //Set the minimum packet size
                            created.MinimumPacketSize = minimumPacketSize;

                            //Set the maximum packet size
                            created.MaximumPacketSize = maximumPacketSize;
                        }

                        //If there is not a client
                        if (m_RtpClient == null || m_RtpClient.IsDisposed)
                        {
                            //Create a Duplexed reciever using the RtspSocket sharing the RtspClient's buffer's properties
                            m_RtpClient = new RtpClient(new Common.MemorySegment(m_Buffer));

                            //Attach an event for interleaved data
                            m_RtpClient.InterleavedData += ProcessInterleaveData;
                        }
                        else if (m_RtpProtocol != ProtocolType.Tcp) goto SetupTcp;

                        //If the source address contains the NAT IpAddress or the source is the same then just use the source.
                        if (IPAddress.Equals(sourceIp, ((IPEndPoint)m_RemoteRtsp).Address) ||
                            Media.Common.Extensions.IPAddress.IPAddressExtensions.IsOnIntranet(sourceIp))
                        {
                            //Create from the existing socket (may need reuse port)
                            created.Initialize(m_RtspSocket, m_RtspSocket);

                            //Test using a new socket
                            //if (m_RtspSocket != null) created.Initialize((IPEndPoint)m_RtspSocket.LocalEndPoint, (IPEndPoint)m_RtspSocket.RemoteEndPoint);

                            //Don't close this socket when disposing. (The RtpClient will dispose it)
                            //LeaveOpen = true;
                        }
                        else
                        {
                            //maybe multicast...

                            //Create a new socket's
                            created.Initialize(multicast ? Media.Common.Extensions.Socket.SocketExtensions.GetFirstMulticastIPAddress(sourceIp.AddressFamily) : Media.Common.Extensions.Socket.SocketExtensions.GetFirstUnicastIPAddress(sourceIp.AddressFamily),
                                sourceIp, serverRtpPort); //Might have to come from source string?

                            //Handle ttl if present..
                            if (ttl > 0)
                            {
                                created.RtpSocket.Ttl = (short)ttl;

                                if (needsRtcp && created.RtpSocket.Handle != created.RtcpSocket.Handle) created.RtcpSocket.Ttl = (short)ttl;
                            }

                            if (multicast)
                            {
                                //Should store address for drop and sometimes should send rtcp to this address e.g. AnySourceMulticast rtp or rtcp.
                                Media.Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(created.RtpSocket, destinationIp);

                                Media.Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(created.RtpSocket, ttl);

                                if (created.RtcpSocket.Handle != created.RtpSocket.Handle)
                                {
                                    Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(created.RtcpSocket, destinationIp);

                                    Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(created.RtcpSocket, ttl);
                                }
                            }

                            //When the RtspClient is disposed that socket will also be disposed.
                        }

                    }
                    else
                    {
                        //The server may respond with the port used for the request which indicates that TCP should be used?
                        if (serverRtpPort == location.Port) goto SetupTcp;

                        //If we need to make a client then do so
                        if (m_RtpClient == null || m_RtpClient.IsDisposed)
                        {
                            //Create a Udp Reciever sharing the RtspClient's buffer's properties
                            m_RtpClient = new RtpClient(m_Buffer);

                            //Attach an event for interleaved data
                            m_RtpClient.InterleavedData += ProcessInterleaveData;
                        }
                        else if (created == null || created.IsDisposed)
                        {
                            //Obtain the context via the given local or remote id
                            created = localSsrc != 0 ? m_RtpClient.GetContextBySourceId(localSsrc) : remoteSsrc != 0 ? m_RtpClient.GetContextBySourceId(remoteSsrc) : null;

                            //If the control channel is the same then just update the client and ensure connected.
                            if (created != null && created.ControlChannel == controlChannel)
                            {
                                created.Initialize(m_RtspSocket);

                                //m_RtpClient.Connect();

                                created.ApplicationContext = SessionId;

                                session.Context = created;

                                return response;
                            }
                        }

                        //Get the available context's
                        var availableContexts = m_RtpClient.GetTransportContexts().Where(tc => tc != null && false == tc.IsDisposed);

                        //If there are aren't any then create one using the default values
                        if (false == availableContexts.Any())
                        {
                            created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, 0, (byte)(multiplexing ? 0 : 1), mediaDescription, needsRtcp, remoteSsrc, remoteSsrc != 0 ? 0 : 2, localIp, sourceIp);
                        }
                        else
                        {
                            //OrderBy(c=>c.ControlChannel - c.DataChannel) to get the highest, then would need to determine if at max and could wrap... e.g. getAvailableContextNumber()
                            RtpClient.TransportContext lastContext = availableContexts.LastOrDefault();

                            //Todo, destinationIp should still be sourceIp.

                            if (lastContext != null) created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, (byte)(lastContext.DataChannel + (multiplexing ? 1 : 2)), (byte)(lastContext.ControlChannel + (multiplexing ? 1 : 2)), mediaDescription, needsRtcp, remoteSsrc, remoteSsrc != 0 ? 0 : 2, localIp, sourceIp);
                            else created = RtpClient.TransportContext.FromMediaDescription(SessionDescription, (byte)dataChannel, (byte)controlChannel, mediaDescription, needsRtcp, remoteSsrc, remoteSsrc != 0 ? 0 : 2, localIp, sourceIp);
                        }

                        created.ContextMemory = m_RtpClient.m_Buffer;

                        created.Initialize(localIp, sourceIp, clientRtpPort, clientRtcpPort, serverRtpPort, serverRtcpPort);

                        //Handle ttl if present..
                        if (ttl > 0)
                        {
                            created.RtpSocket.Ttl = (short)ttl;

                            if (needsRtcp && created.RtpSocket.Handle != created.RtcpSocket.Handle) created.RtcpSocket.Ttl = (short)ttl;
                        }

                        //Todo, if the desitionIp is not equal to the sourceIp must also be handled (addressFamily also)

                        if (multicast
                            &&
                            // && false == sourceIp.Equals(destinationIp) 
                            //&& 
                            Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(destinationIp))
                        {
                            Media.Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(created.RtpSocket, destinationIp);

                            Media.Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(created.RtpSocket, ttl);

                            if (created.RtcpSocket.Handle != created.RtpSocket.Handle)
                            {
                                Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(created.RtcpSocket, destinationIp);

                                Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(created.RtcpSocket, ttl);
                            }
                        }

                        //No longer need the temporary sockets

                        if (rtpTemp != null)
                        {
                            rtpTemp.Dispose();

                            rtpTemp = null;
                        }

                        if (false == multiplexing && rtcpTemp != null)
                        {
                            rtcpTemp.Dispose();

                            rtcpTemp = null;
                        }
                    }

                    //if a context was created add it
                    if (created != null)
                    {
                        m_RtpClient.AddContext(created, false == multiplexing, false == multiplexing);

                        //Store the sessionId in the ApplicationContext.
                        created.ApplicationContext = SessionId;

                        //Store the context in the session
                        session.Context = created;
                    }

                    //Setup Complete
                    return response;
                }
            }
            catch (Exception ex)
            {
                Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Unable to setup media. See InnerException", ex);

                return m_LastTransmitted;
            }

        //Setup for Interleaved connection
        SetupTcp:
            {
                m_RtpProtocol = ProtocolType.Tcp;

                //Recurse call to ensure propper setup
                return SendSetup(location, mediaDescription);
            }
        }

        protected virtual void MonitorProtocol(object state = null)
        {
            if (m_ProtocolMonitor == null) return;

            bool keepAlives = DisableKeepAliveRequest;

            //Should check for KeepAlive header in previous requests?

            //If not already Disposed and the protocol was not already specified as or configured to TCP
            if (false == IsDisposed &&  //And
                IsPlaying) //Still playing
            {
                //Filter any context which is not playing, disposed or has activity
                //Todo should have a HasRequiredActivity
                //To quickly fix this I am just checking that nothing was sent or received
                //Notes that when sending only that one ALSO needs to determine `should something still be received` ?

                //Monitor the protocol for incoming messages
                if (false == SharesSocket && false == InUse && Common.Extensions.Socket.SocketExtensions.CanRead(m_RtspSocket))
                {
                    DisableKeepAliveRequest = true;

                    //Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: Receiving Data");

                    SocketError error;

                    int cseq;

                    using (var response = SendRtspMessage(null, out error, out cseq, false, true, m_MaximumTransactionAttempts))
                    {
                        if (error == SocketError.Success && response != null) Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: (" + error + ") Received =>" + response.ToString());
                    }

                    //Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: Data Received");

                    DisableKeepAliveRequest = keepAlives;
                }

                //If protocol switch is still allowed AND still playing
                if (false == IsDisposed && AllowAlternateTransport && IsPlaying && m_RtpProtocol != ProtocolType.Tcp)
                {
                    //Filter the contexts which have received absolutely NO data.
                    var contextsWithoutFlow = Client.GetTransportContexts().Where(tc => tc != null &&
                        m_Playing.Contains(tc.MediaDescription) &&
                        tc.TotalBytesReceieved == 0 && tc.TotalPacketsSent == 0
                        && tc.TimeActive > tc.ReceiveInterval);
                    //tc.TimeSending > tc.ReceiveInterval);

                    //InactiveTime or ActiveTime on the tc. (Another value)

                    //If there are any context's which are not flowing but are playing
                    if (contextsWithoutFlow.Count() >= m_Playing.Count)// and the amount of them is greater than or equal to what the rtsp client is playing
                    {
                        try
                        {
                            //If the client has not already switched to Tcp
                            if (m_RtpProtocol != ProtocolType.Tcp)
                            {
                                //Ensure Tcp protocol
                                m_RtpProtocol = ProtocolType.Tcp;
                            }
                            else if (m_RtpProtocol != ProtocolType.Udp)
                            {
                                //Ensure Udp protocol
                                m_RtpProtocol = ProtocolType.Udp;
                            }
                            else
                            {
                                //Ensure IP protocol
                                m_RtpProtocol = ProtocolType.IP;
                            }

                            //Stop sending them for now
                            if (false == keepAlives) DisableKeepAliveRequest = true;

                            //Wait for any existing request to complete
                            while (InUse) m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                            Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: StopPlaying");

                            //Stop all playback
                            StopPlaying(false);

                            //If this is not done then when StartPlaying is called again 
                            //SendSetup will may find a Context which exists with the same ssrc.
                            //It should be determined then if the context can be updated or not with the new socket.
                            //It would only save a small amount of memory
                            m_RtpClient.DisposeAndClearTransportContexts();

                            //Cache
                            while (IsPlaying || InUse)
                            {
                                m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                                Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: Waiting for IsPlaying to be false.");
                            }


                            Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: StartPlaying");

                            //Start again
                            StartPlaying();

                            //Restore value
                            DisableKeepAliveRequest = keepAlives;
                        }
                        catch (Exception ex)
                        {
                            Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: " + ex.Message);
                        }
                    }
                }
            }

            //If there is still a timer change it based on the last messages round trip time, should be relative to all messages...
            if (false == IsDisposed && m_ProtocolMonitor != null)
                try { m_ProtocolMonitor.Change(m_ConnectionTime.Add(LastMessageRoundTripTime), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan); }
                catch (Exception ex) { Common.ILoggingExtensions.Log(Logger, ToString() + "@MonitorProtocol: " + ex.Message); }
        }

        public RtspMessage SendPlay(MediaDescription mediaDescription, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt")
        {
            if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

            //Check if the session supports pausing a specific media item
            if (false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

            var context = Client.GetContextForMediaDescription(mediaDescription);

            if (context == null) throw new InvalidOperationException("The given mediaDescription has not been SETUP.");

            //Check if the media was previsouly playing
            if (mediaDescription != null && false == m_Playing.Contains(mediaDescription))
            {
                //Keep track of whats playing
                m_Playing.Add(mediaDescription);

                //Raise an event now.
                OnPlaying(mediaDescription);
            }

            //Send the play request
            return SendPlay(mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription), startTime ?? context.MediaStartTime, endTime ?? context.MediaEndTime, rangeType);
        }

        public RtspMessage SendPlay(Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt", bool force = false)
        {
            int sequenceNumber;

            return SendPlay(out sequenceNumber, location, startTime, endTime, rangeType, force);
        }

        public RtspMessage SendPlay(out int sequenceNumber, Uri location = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string rangeType = "npt", bool force = false)
        {
            //CheckDisposed?

            sequenceNumber = -1;

            //If not forced
            if (false == force)
            {
                //Usually at least setup must occur so we must have sent and received a setup to actually play
                force = m_ReceivedMessages > 0 && SupportedMethods.Contains(RtspMethod.SETUP.ToString());

                //If not forced and the soure does not support play then throw an exception
                if (false == force &&
                    SupportedMethods.Count > 0 &&  //There are some methods supported
                    false == SupportedMethods.Contains(RtspMethod.PLAY.ToString())) throw new InvalidOperationException("Server does not support PLAY.");
            }

            //Check that the Timing description of the session description allows play?           

            try
            {
                using (RtspMessage play = new RtspMessage(RtspMessageType.Request)
                {
                    RtspMethod = RtspMethod.PLAY,
                    Location = location ?? CurrentLocation
                })
                {
                    /*
                      A PLAY request without a Range header is legal. It starts playing a
                        stream from the beginning unless the stream has been paused. If a
                        stream has been paused via PAUSE, stream delivery resumes at the
                        pause point. If a stream is playing, such a PLAY request causes no
                        further action and can be used by the client to test server liveness.
                     */

                    //Maybe should not be set if no start or end time is given.
                    if (startTime.HasValue || endTime.HasValue) play.SetHeader(RtspHeaders.Range, RtspHeaders.RangeHeader(startTime, endTime, rangeType));
                    else if (false == string.IsNullOrWhiteSpace(rangeType)) //otherwise is a non null or whitespace string was given for rangeType
                    {
                        //Use the given rangeType string verbtaim.
                        play.SetHeader(RtspHeaders.Range, rangeType);
                    }

                    //Store any error
                    SocketError error;

                    //Send the response
                    RtspMessage response = SendRtspMessage(play, out error, out sequenceNumber);

                    //response may be null because the server dropped the response due to an invalid header on the request.

                    //Handle allowed problems with reception of the play response if already playing
                    if (error == SocketError.Success &&
                        false == IsPlaying &&
                        (response == null || response != null && response.MessageType == RtspMessageType.Response))
                    {
                        //No response or invalid range.
                        if (response == null || response.RtspStatusCode == RtspStatusCode.InvalidRange)
                        {
                            //if (response == null)
                            //{
                            //    //If there is transport
                            //    if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                            //    {
                            //        //Connect the client now.
                            //        m_RtpClient.Activate();
                            //    }

                            //    //return response;
                            //}

                            play.RemoveHeader(Rtsp.RtspHeaders.Range);

                            play.RemoveHeader(Rtsp.RtspHeaders.CSeq);

                            play.RemoveHeader(RtspHeaders.Timestamp);

                            return SendRtspMessage(play);
                        }
                        else if (response.RtspStatusCode <= RtspStatusCode.OK)
                        {
                            //If there is transport
                            if (false == Common.IDisposedExtensions.IsNullOrDisposed(m_RtpClient))
                            {
                                //Connect the client now.
                                m_RtpClient.Activate();
                            }

                            //Set EndTime based on Range

                            //string rangeHeader = response[RtspHeaders.Range];

                            //Should really only get the RtpInfo header if its needed....

                            //Get the rtp-info header
                            string rtpInfo = response[RtspHeaders.RtpInfo];

                            string[] rtpInfos;

                            //Make a parser class which can be reused?

                            //If parsing of the header succeeded
                            if (RtspHeaders.TryParseRtpInfo(rtpInfo, out rtpInfos))
                            {
                                //Notes that more then 1 value here indicates AggregateControl is supported at the server but possibly not the session?

                                //Loop all found sub header values
                                foreach (string rtpInfoValue in rtpInfos)
                                {
                                    Uri uri;

                                    int? rtpTime;

                                    int? seq;

                                    int? ssrc;

                                    //If any value which was needed was found.
                                    if (RtspHeaders.TryParseRtpInfo(rtpInfoValue, out uri, out seq, out rtpTime, out ssrc))
                                    {
                                        //Just use the ssrc to lookup the context.
                                        if (ssrc.HasValue)
                                        {
                                            //Get the context created with the ssrc defined above
                                            RtpClient.TransportContext context = m_RtpClient.GetContextBySourceId(ssrc.Value);

                                            //If that context is not null then allow it's ssrc to change now.
                                            if (context != null)
                                            {
                                                context.RemoteSynchronizationSourceIdentifier = ssrc.Value;

                                                if (seq.HasValue) context.RecieveSequenceNumber = seq.Value;

                                                if (rtpTime.HasValue) context.RtpTimestamp = rtpTime.Value;

                                                //if (context.Goodbye != null) context.Goodbye = null;

                                                context = null;
                                            }
                                        }
                                        else if (uri != null)
                                        {
                                            //Need to get the context by the uri.
                                            //Location = rtsp://abc.com/live/movie
                                            //uri = rtsp://abc.com/live/movie/trackId=0
                                            //uri = rtsp://abc.com/live/movie/trackId=1
                                            //uri = rtsp://abc.com/live/movie/trackId=2

                                            //Get the context created with from the media description with the same resulting control uri
                                            RtpClient.TransportContext context = m_RtpClient.GetTransportContexts().FirstOrDefault(tc => tc.MediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) == uri);

                                            //If that context is not null then allow it's ssrc to change now.
                                            if (context != null)
                                            {
                                                if (ssrc.HasValue) context.RemoteSynchronizationSourceIdentifier = ssrc.Value;

                                                if (seq.HasValue) context.RecieveSequenceNumber = seq.Value;

                                                if (rtpTime.HasValue) context.RtpTimestamp = rtpTime.Value;

                                                //if (context.Goodbye != null) context.Goodbye = null;

                                                context = null;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }

                    return response;
                }
            }
            catch { throw; }
        }

        /// <summary>
        /// Sends a PAUSE Request
        /// </summary>
        /// <param name="location">The location to indicate in the request</param>
        /// <returns>The response</returns>
        public RtspMessage SendPause(MediaDescription mediaDescription = null, bool force = false)
        {
            int cseq;
            return SendPause(out cseq, mediaDescription, force);
        }

        public RtspMessage SendPause(out int sequenceNumber, MediaDescription mediaDescription = null, bool force = false)
        {
            //Ensure media has been setup unless forced.
            if (mediaDescription != null && false == force)
            {
                //Check if the session supports pausing a specific media item
                if (false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation)) throw new InvalidOperationException("The SessionDescription does not allow aggregate control.");

                //Get a context for the media
                var context = Client.GetContextForMediaDescription(mediaDescription);

                //If there is no context then throw an exception.
                if (context == null) throw new InvalidOperationException("The given mediaDescription has not been SETUP.");

                context = null;
            }

            //Keep track of whats playing
            if (mediaDescription == null) m_Playing.Clear();
            else m_Playing.Remove(mediaDescription);

            //Fire the event now
            OnPausing(mediaDescription);

            //Send the pause request, determining if the request is for all media or just one.
            return SendPause(out sequenceNumber, mediaDescription != null ? mediaDescription.GetAbsoluteControlUri(CurrentLocation, SessionDescription) : CurrentLocation, force);
        }

        public RtspMessage SendPause(out int sequenceNumber, Uri location = null, bool force = false)
        {
            //If the server doesn't support it
            if (false == SupportedMethods.Contains(RtspMethod.PAUSE.ToString()) && false == force) throw new InvalidOperationException("Server does not support PAUSE.");

            //if (!Playing) throw new InvalidOperationException("RtspClient is not Playing.");
            using (RtspMessage pause = new RtspMessage(RtspMessageType.Request)
            {
                RtspMethod = RtspMethod.PAUSE,
                Location = location ?? CurrentLocation
            })
            {
                SocketError error;
                return SendRtspMessage(pause, out error, out sequenceNumber);
            }
        }

        /// <summary>
        /// Sends a ANNOUNCE Request
        /// </summary>
        /// <param name="location">The location to indicate in the request, otherwise null to use the <see cref="CurrentLocation"/></param>
        /// <param name="sdp">The <see cref="SessionDescription"/> to ANNOUNCE</param>
        /// <returns>The response</returns>
        public RtspMessage SendAnnounce(Uri location, SessionDescription sdp, bool force = false)
        {
            if (false == SupportedMethods.Contains(RtspMethod.ANNOUNCE.ToString()) && false == force) throw new InvalidOperationException("Server does not support ANNOUNCE.");
            if (sdp == null) throw new ArgumentNullException("sdp");
            using (RtspMessage announce = new RtspMessage(RtspMessageType.Request)
            {
                RtspMethod = RtspMethod.ANNOUNCE,
                Location = location ?? CurrentLocation
            })
            {
                announce.Body = sdp.ToString();
                announce.SetHeader(RtspHeaders.ContentType, Sdp.SessionDescription.MimeType);
                return SendRtspMessage(announce);
            }
        }

        //SendRecord

        internal void SendKeepAliveRequest(object state)
        {
            bool wasPlaying = false, wasConnected = false;

            try
            {
                //Thrown an exception if IsDisposed
                if (IsDisposed) return;

                wasPlaying = IsPlaying;

                //Save the state of the connection
                wasConnected = IsConnected;

                //If the keep alive request feature is not disabled and the session times out if not kept alive
                if (wasPlaying && IsPlaying &&
                    false == DisableKeepAliveRequest &&
                    m_RtspSessionTimeout > TimeSpan.Zero)
                {
                    //Don't send a keep alive if the stream is ending before the next keep alive would be sent.
                    if (EndTime.HasValue && EndTime.Value != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan &&
                        EndTime.Value - ((DateTime.UtcNow - m_StartedPlaying.Value)) <= m_RtspSessionTimeout) return;

                    //Ensure transport is connected. (will be done in play...)
                    //if (false == m_RtpClient.IsConnected) m_RtpClient.Connect();

                    while (InUse) m_InterleaveEvent.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

                    DisableKeepAliveRequest = true;

                    foreach (var session in m_Sessions)
                    {
                        //If the session itself doesn't support keep alive then continue.
                        if (false == session.Value.EnableKeepAliveRequest) continue;

                        //Using the SessionId 
                        //session.Key

                        //perform the request

                        //Note, that each session may use a totally different connection.

                        //Check if GET_PARAMETER is supported.
                        if (SupportedMethods.Contains(RtspMethod.GET_PARAMETER.ToString()))
                        {
                            //Need the message itself to update the session..
                            using (SendGetParameter(null, null, session.Value.SessionId, false)) ;
                        }
                        else if (SupportedMethods.Contains(RtspMethod.OPTIONS.ToString())) //If at least options is supported
                        {
                            using (SendOptions(session.Value.ControlLocation == RtspMessage.Wildcard, session.Value.SessionId)) ;
                        }
                        else if (SupportedMethods.Contains(RtspMethod.PLAY.ToString())) //If at least PLAY is supported
                        {
                            using (SendPlay()) ; //Sessionid overload
                        }
                    }


                    DisableKeepAliveRequest = false;
                }

                //Should be done in Monitor Protocol

                //Only perform these actions if playing anything.
                if (wasPlaying)
                {
                    //Raise events for ended media.
                    foreach (var context in Client.GetTransportContexts())
                    {
                        if (context == null || context.IsDisposed || context.IsContinious || context.TimeReceiving < context.MediaEndTime) continue;

                        //Remove from the playing media and if it was contained raise an event.
                        if (m_Playing.Remove(context.MediaDescription)) OnStopping(context.MediaDescription);
                    }

                    bool aggregateControl = SessionDescription.SupportsAggregateMediaControl(CurrentLocation);

                    //Iterate the played items looking for ended media.
                    for (int i = 0, e = m_Playing.Count; i < e; ++i)
                    {
                        if (e > m_Playing.Count) break;

                        var mediaDescription = m_Playing[i];

                        //Get a context
                        var context = Client.GetContextForMediaDescription(mediaDescription);

                        //If there is a context ensure it has not ended and has recieved data within the context receive interval.
                        if (context == null ||
                            false == context.IsDisposed ||
                            context.Goodbye == null ||
                            true == context.IsContinious ||
                            context.TimeSending < context.MediaEndTime) continue;

                        //Teardown the media if the session supports AggregateControl
                        //(Todo, Each context may have it's own sessionId)
                        //Also the Server may have already stopped the media...
                        if (aggregateControl && m_Playing.Contains(mediaDescription)) using (SendTeardown(mediaDescription, true)) ;
                        else if (m_Playing.Remove(mediaDescription))
                        {//Otherwise Remove from the playing media and if it was contained raise an event.
                            OnStopping(mediaDescription);
                        }

                        //If there was a context for the media ensure it is removed and disposed from the underlying transport.
                        if (context != null)
                        {

                            //handle leaving open changes?
                            //if(context.LeaveOpen)

                            //Remove the context from the rtp client.
                            Client.TryRemoveContext(context);

                            //Dispose of it.
                            context.Dispose();

                            //Remove any reference
                            context = null;
                        }
                    }


                    //Ensure media is still flowing if still playing otherwise raise the stopping event.
                    if (IsPlaying) EnsureMediaFlows();
                    else if (wasPlaying) OnStopping(); //Ensure not already raised?
                }

                //Determine next time to send a keep alive
                if (m_KeepAliveTimer != null && IsPlaying)
                {
                    //Todo, Check if the media will end before the next keep alive is due before sending.

                    if (m_LastMessageRoundTripTime < m_RtspSessionTimeout) m_KeepAliveTimer.Change(TimeSpan.FromTicks(m_RtspSessionTimeout.Subtract(m_LastMessageRoundTripTime + m_ConnectionTime).Ticks / 2), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);
                }

            }
            catch (Exception ex) { Common.ILoggingExtensions.Log(Logger, ToString() + "@SendKeepAlive: " + ex.Message); }

            //Raise the stopping event if not playing anymore
            //if (true == wasPlaying && false == IsPlaying) OnStopping();

            //Deactivate if was previously disconnected so long as the ProtocolSwitchTimer is not activated.
            //Might need a flag to see if DisconnectSocket was called.
            //if (m_ProtocolSwitchTimer == null && false == wasConnected && IsPlaying && true == IsConnected) DisconnectSocket();
        }

        public void EnsureMediaFlows()
        {

            if (InUse) return;

            DisableKeepAliveRequest = true;

            //If not waiting to switch protocols
            if (m_ProtocolMonitor == null && IsPlaying)
            {

                //If not playing anymore do nothing
                if (EndTime != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan &&
                    DateTime.UtcNow - m_StartedPlaying.Value > EndTime)
                {
                    StopPlaying();

                    return;
                }

                //Determine what contexts are playing and have set a goodbye
                var contextsWithGoodbye = Client.GetTransportContexts().Where(tc => tc != null &&
                    m_Playing.Contains(tc.MediaDescription) &&
                    tc.Goodbye != null);

                //If there are such contexts
                if (m_InterleaveEvent.IsSet && IsPlaying && contextsWithGoodbye.Any())
                {
                    //If the server doens't support pause then we cant pause.
                    bool supportPause = SupportedMethods.Contains(RtspMethod.PAUSE.ToString());

                    //If any media was pausedOrStopped.
                    bool pausedOrStoppedAnything = false;

                    //If we cannot stop a single media item we will set this to true.
                    bool stopAll = false == SessionDescription.SupportsAggregateMediaControl(CurrentLocation);

                    //Iterate all inactive contexts.
                    if (false == stopAll) foreach (var context in contextsWithGoodbye.ToArray())
                        {
                            //Ensure still in playing
                            if (false == m_Playing.Contains(context.MediaDescription) || context.HasAnyRecentActivity) continue;

                            int requestCseq;

                            //Send a pause request if not already paused and the server supports PAUSE and there has been any activity on the context
                            if (supportPause)
                            {
                                //If not going to be playing anymore do nothing
                                if (context.TimeRemaining >= context.ReceiveInterval + m_LastMessageRoundTripTime + m_LastServerDelay) continue;

                                //If the context is not continious and there is no more time remaining do nothing
                                if (false == context.IsContinious && context.TimeRemaining <= TimeSpan.Zero) continue;

                                //Send the PAUSE request
                                using (var pauseResponse = SendPause(out requestCseq, context.MediaDescription))
                                {

                                    //If the paused request was not a sucess then it's probably due to an aggregate operation
                                    //Determine if we have to stop everything.
                                    if (pauseResponse == null || pauseResponse.RtspStatusCode <= RtspStatusCode.OK)
                                    {

                                        //Sometime the server doesn't respond to pause or the response gets lost.
                                        if (pauseResponse == null || pauseResponse.MessageType == RtspMessageType.Invalid)
                                        {
                                            //Wait up until the time another request is sent.
                                            m_InterleaveEvent.Wait(m_RtspSessionTimeout);

                                            //Need a way to be able to check the request's sequence number..

                                            //if(m_LastTransmitted != null && m_LastTransmitted.CSeq == pauseReqeust.SequnceNumber)

                                            stopAll = true;
                                        }
                                        else   //See if everything has to be stopped.
                                            stopAll = pauseResponse.RtspStatusCode == RtspStatusCode.AggregateOpperationNotAllowed;

                                        //If the request failed then nothing was paused yet
                                        pausedOrStoppedAnything = false;

                                        //Could move this logic to the SendPause method which would check the response status code before returning the response and then wouldn't raise the Pause event.

                                        //Ensure external state is observed
                                        m_Playing.Add(context.MediaDescription);

                                        OnPlaying(context.MediaDescription);

                                    }
                                }
                            }
                            else
                            {
                                //If not going to be playing anymore do nothing
                                if (context.TimeRemaining >= context.ReceiveInterval + m_LastMessageRoundTripTime + m_LastServerDelay) continue;

                                //If the context is not continious and there is no more time remaining do nothing
                                if (false == context.IsContinious && context.TimeRemaining <= TimeSpan.Zero) continue;

                                //We can't pause so STOP JUST THIS MEDIA
                                using (var teardownResponse = SendTeardown(context.MediaDescription))
                                {
                                    //If the Teardown was not a success then it's probably due to an aggregate operation.
                                    //If the paused request was not a sucess then it's probably due to an aggregate operation
                                    //Determine if we have to stop everything.
                                    if (teardownResponse == null || teardownResponse.RtspStatusCode <= RtspStatusCode.OK)
                                    {

                                        //Sometime the server doesn't respond to pause or the response gets lost.
                                        if (teardownResponse == null || teardownResponse.MessageType == RtspMessageType.Invalid)
                                        {
                                            //Wait up until the time another request is sent.
                                            m_InterleaveEvent.Wait(m_RtspSessionTimeout);

                                            //Need a way to be able to check the request's sequence number..

                                            //if(m_LastTransmitted != null && m_LastTransmitted.CSeq == pauseReqeust.SequnceNumber)

                                            stopAll = true;
                                        }
                                        else   //See if everything has to be stopped.
                                            stopAll = teardownResponse.RtspStatusCode == RtspStatusCode.AggregateOpperationNotAllowed;

                                        //If the request failed then nothing was paused yet
                                        pausedOrStoppedAnything = false;

                                        //Could move this logic to the SendPause method which would check the response status code before returning the response and then wouldn't raise the Pause event.

                                        //Ensure external state is observed
                                        m_Playing.Add(context.MediaDescription);

                                        OnPlaying(context.MediaDescription);

                                    }
                                }
                            }

                            //If we have to stop everything and the server doesn't support pause then stop iterating.
                            if (stopAll) break;

                            //The media was paused or stopped, so play it again if anything was received
                            if (pausedOrStoppedAnything && context.TotalBytesReceieved > 0)
                            {
                                //context.Goodbye.Dispose();

                                //Ensure the context state allows for sending again.
                                context.Goodbye = null;

                                //Try to play the media again
                                try { Play(context.MediaDescription); }
                                catch
                                {
                                    //Ensure external state is observed, the media is still playing
                                    m_Playing.Add(context.MediaDescription);

                                    OnPlaying(context.MediaDescription);
                                }
                            }
                        }

                    //If everything needs to stop.
                    if (stopAll && IsPlaying &&
                        EndTime.HasValue &&
                        EndTime.Value != Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan &&
                        //And there is enough time to attempt
                        DateTime.UtcNow - m_StartedPlaying.Value > EndTime.Value.Subtract(m_LastMessageRoundTripTime.Add(m_ConnectionTime.Add(m_LastServerDelay)))
                        && contextsWithGoodbye.All(tc => tc != null && false == tc.HasAnyRecentActivity))
                    {

                        if (supportPause)
                        {
                            //Pause all media
                            Pause();

                            //Start playing again
                            StartPlaying();
                        }
                        else
                        {
                            //If still connected
                            if (IsConnected)
                            {
                                //Just send a play to continue receiving whatever media is still sending.
                                using (SendPlay()) ;

                            }
                            else
                            {
                                //Stop playing everything
                                StopPlaying();

                                //Start playing everything
                                StartPlaying();
                            }
                        }
                    }
                }
            }

            DisableKeepAliveRequest = false;
        }

        public RtspMessage SendGetParameter(string body = null, string contentType = null, string sessionId = null, bool force = false)
        {
            //…Content-type: application/x-rtsp-packetpair for WMS

            //If the server doesn't support it
            if (false == SupportedMethods.Contains(RtspMethod.GET_PARAMETER.ToString()) && false == force) throw new InvalidOperationException("Server does not support GET_PARAMETER.");

            //Need a session id
            using (RtspMessage get = new RtspMessage(RtspMessageType.Request)
            {
                RtspMethod = RtspMethod.GET_PARAMETER,
                Location = CurrentLocation,
                Body = body ?? string.Empty
            })
            {
                if (false == string.IsNullOrWhiteSpace(contentType)) get.SetHeader(RtspHeaders.ContentType, contentType);

                if (false == string.IsNullOrWhiteSpace(sessionId)) get.SetHeader(RtspHeaders.Session, sessionId);

                return SendRtspMessage(get);
            }
        }

        public RtspMessage SendSetParameter(string body = null, string contentType = null, string sessionId = null, bool force = false)
        {
            //If the server doesn't support it
            if (false == SupportedMethods.Contains(RtspMethod.SET_PARAMETER.ToString()) && false == force) throw new InvalidOperationException("Server does not support GET_PARAMETER.");

            using (RtspMessage set = new RtspMessage(RtspMessageType.Request)
            {
                RtspMethod = RtspMethod.SET_PARAMETER,
                Location = CurrentLocation,
                Body = body ?? string.Empty
            })
            {
                if (false == string.IsNullOrWhiteSpace(contentType)) set.SetHeader(RtspHeaders.ContentType, contentType);

                if (false == string.IsNullOrWhiteSpace(sessionId)) set.SetHeader(RtspHeaders.Session, sessionId);

                return SendRtspMessage(set);
            }
        }

        #endregion

        #region Overloads

        public override string ToString()
        {
            return string.Join(((char)Common.ASCII.HyphenSign).ToString(), base.ToString(), InternalId);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Stops sending any Keep Alive Immediately and calls <see cref="StopPlaying"/>.
        /// If the <see cref="RtpClient"/> is not null:
        /// Removes the <see cref="ProcessInterleaveData"/> event
        /// Disposes the RtpClient and sets it to null.
        /// Disposes and sets the Buffer to null.
        /// Disposes and sets the InterleavedEvent to null.
        /// Disposes and sets the m_LastTransmitted to null.
        /// Disposes and sets the <see cref="RtspSocket"/> to null if <see cref="LeaveOpen"/> allows.
        /// Removes connection times so <see cref="IsConnected"/> is false.
        /// Stops raising any events.
        /// Removes any <see cref="Logger"/>
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed || false == ShouldDispose) return;

            if (m_ProtocolMonitor != null)
            {
                m_ProtocolMonitor.Dispose();

                m_ProtocolMonitor = null;
            }

            DisableKeepAliveRequest = true;

            StopPlaying();

            if (m_RtpClient != null)
            {
                m_RtpClient.InterleavedData -= ProcessInterleaveData;

                if (false == m_RtpClient.IsDisposed) m_RtpClient.Dispose();

                m_RtpClient = null;
            }

            //Finally set Disposed to true
            base.Dispose();

            if (m_Buffer != null)
            {
                m_Buffer.Dispose();
                m_Buffer = null;
            }

            if (m_LastTransmitted != null)
            {
                m_LastTransmitted.Dispose();
                m_LastTransmitted = null;
            }

            m_InterleaveEvent.Dispose();

            if (m_RtspSocket != null)
            {
                if (false == LeaveOpen) m_RtspSocket.Dispose();
                m_RtspSocket = null;
            }

            if (m_SessionDescription != null)
            {
                m_SessionDescription.Dispose();

                m_SessionDescription = null;
            }

            m_AuthorizationHeader = null;

            m_BeginConnect = m_EndConnect = null;

            OnConnect = null;
            OnDisconnect = null;
            OnStop = null;
            OnPlay = null;
            OnPause = null;
            OnRequest = null;
            OnResponse = null;

            Logger = null;
        }

        #endregion

        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            if (IsDisposed) yield break;

            //Technically any sockets which are used by a session should also be returned.

            yield return m_RtspSocket;
        }
    }
}
