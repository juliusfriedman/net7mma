using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Rtcp;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Media.Rtp
{
    /// <summary>
    /// Implementation of Rtp RFC3550 and RFC4751
    /// Automatically responds to Rtcp Packets if enabled and required.
    /// Todo:
    /// Add logic for stand alone operation (rtp:// Uri)
    /// Abstract participants into a list so each client can clearly identify who it's sending too.
    /// </summary>
    public class RtpClient
    {
        #region Statics

        //Udp Hole Punch
        //Might want a seperate method for this... (WakeupRemote)
        //Most routers / firewalls will let traffic back through if the person from behind initiated the traffic.
        //Send some bytes to ensure the reciever is awake and ready... (SIP or RELOAD may have something specific and better)
        //e.g Port mapping request http://tools.ietf.org/html/rfc6284#section-4.2 
        static byte[] WakeUpBytes = new byte[] { 0x70, 0x70, 0x70, 0x70 };

        internal static byte MAGIC = 36; // $

        static uint RTP_SEQ_MOD = (1 << 16);

        //const
        static int MAX_DROPOUT = 3000;
        static int MAX_MISORDER = 100;
        static int MIN_SEQUENTIAL = 2;
        
        /// <summary>
        /// Creates a RtpClient which is configured to Receive on the given Tcp socket.
        /// </summary>
        /// <param name="existing">The exsiting Tcp socket to use</param>
        /// <returns>A configured RtpClient</returns>
        //Rtsp over Http might require an new Socket, in that case existing would be null?
        public static RtpClient Interleaved(Socket existing)
        {
            //Should verify socket type is TCP and use new socket if required ?? new Socket...
            return new RtpClient(existing);
        }

        /// <summary>
        /// Creates a RtpClient which is configured to send to the given remote address.
        /// Built-in events for received RtpPackets will not be enforced and FrameChanged will not be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Sender(IPAddress remoteAddress)
        {
            return new RtpClient(remoteAddress)
            {
                IncomingPacketEventsEnabled = false,
            };
        }

        /// <summary>
        /// Creates a RtpClient which is configured to Receive from the given remote address
        /// Built-in events for received RtpPackets will be enfored and FrameChanged will be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Receiever(IPAddress remoteAddress)
        {
            return new RtpClient(remoteAddress);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// New construct to hold information relevant to each channel in udp this will less useful but still applicable
        /// Counters for Rtp and Rtcp should be kept here for proper calculation.
        /// Should also be used to allow sources to be send to sinks by id using the correct ssrc
        /// This is relevant if the source has video on channel 0 and the sink gets the video on channel 2
        /// Should be created in Rtsp Setup requests or in discovery of sdp in standalone
        /// </summary>
        public class TransportContext
        {
            //The id of the channel 0 - 255
            public readonly byte DataChannel, ControlChannel;

            //The ssrc packets are sent out with under this channel
            public uint SynchronizationSourceIdentifier { get; internal set; }

            //Any frames for this channel
            internal volatile RtpFrame CurrentFrame, LastFrame;

            /// <summary>
            /// MediaDescription which contains information about the type of Media on the Interleave
            /// </summary>
            public Sdp.MediaDescription MediaDescription { get; internal set; }

            internal Socket RtpSocket, RtcpSocket;

            //Is Rtcp Enabled on this Interleave (when false will not send / recieve reports)
            public bool RtcpEnabled = true;

            //Ports we are using / will use
            internal int ServerRtpPort, ServerRtcpPort, ClientRtpPort, ClientRtcpPort;

            //The EndPoints connected to (once connected don't need the Ports)
            internal IPEndPoint LocalRtp, LocalRtcp, RemoteRtp, RemoteRtcp;

            /// <summary>
            /// SequenceNumber of the channel
            /// </summary>
            ushort m_SequenceNumber;

            /// <summary>
            /// The sequence number of the last RtpPacket sent or recieved on this channel, if set to 0 the value 1 will be used instead.
            /// </summary>
            public ushort SequenceNumber { get { return m_SequenceNumber; } internal set { if (value == 0) value = 1; m_SequenceNumber = value; } }

            /// <summary>
            /// The RtpTimestamp from the last SendersReport recieved or created;
            /// </summary>
            public uint RtpTimestamp { get; internal set; }

            /// <summary>
            /// The NtpTimestamp from the last SendersReport recieved or created
            /// </summary>
            public ulong NtpTimestamp { get; internal set; }

            //bytes and packet counters
            internal long RtpBytesSent, RtpBytesRecieved,
                         RtcpBytesSent, RtcpBytesRecieved,
                         RtpPacketsSent, RtcpPacketsSent,
                         RtpPacketsReceieved, RtcpPacketsReceieved;

            //Used for Rtp and Rtcp Transport Calculations (Should be moved into Channel)
            internal uint RtpTransit,
                //Count of bytes recieved prior to the reception of a report
                RtpReceivedPrior,
                //Count of bytes expected prior to the recpetion of a report
                RtpExpectedPrior,
                //The amount of times the Seq number has cycled
                RtpSeqCycles,
                //The amount of base RTP Sequences encountered
                RtpBaseSeq,
                //The highest Sequence recieved by the RtpClient
                RtpMaxSeq,
                //Rtp Probation value
                RtpProbation,
                //The amount of bad RTP Sequences encountered
                RtpBadSeq,
                //Jitter value
                RtpJitter;

            //Reports
            public ReceiversReport RecieversReport { get; internal set; }
            public SendersReport SendersReport { get; internal set; }
            public SourceDescription SourceDescription { get; internal set; }
            public Goodbye Goodbye { get; internal set; }

            //public readonly List<RtcpPacket> ControlLog; //Only if RtcpEnabled

            public TransportContext(byte dataChannel, byte controlChannel, uint ssrc, bool rtcpEnabled = true)
            {
                DataChannel = dataChannel;
                ControlChannel = controlChannel;
                //if they both are the same then this could mean duplexing
                SynchronizationSourceIdentifier = ssrc;
                RtcpEnabled = rtcpEnabled;
            }

            public TransportContext(byte dataChannel, byte controlChannel, uint ssrc, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true)
                : this(dataChannel, controlChannel, ssrc, rtcpEnabled)
            {
                MediaDescription = mediaDescription;
            }

            public TransportContext(byte dataChannel, byte controlChannel, uint ssrc, Sdp.MediaDescription mediaDescription, Socket socket, bool rtcpEnabled = true)
                : this(dataChannel, controlChannel, ssrc, mediaDescription, rtcpEnabled)
            {
                RtpSocket = RtcpSocket = socket;
            }

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            public void UpdateJitter(RtpPacket packet)
            {
                // RFC 3550 A.8.
                ulong transit = (Utility.DateTimeToNptTimestamp(DateTime.UtcNow) - packet.TimeStamp);
                int d = (int)(transit - RtpTransit);
                RtpTransit = (uint)transit;
                if (d < 0) d = -d;
                RtpJitter += (uint)((1d / 16d) * ((double)d - RtpJitter));
            }

            public void ResetCounters(uint sequenceNumber)
            {
                RtpBaseSeq = RtpMaxSeq = (uint)sequenceNumber;
                RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
                RtpSeqCycles = RtpReceivedPrior = (uint)(RtpPacketsReceieved = 0);
            }

            /// <summary>
            /// Currently Unused...
            /// Increments packets recieved and determines if the sequence number is valid in the current state
            /// </summary>
            /// <param name="packet"></param>
            /// <returns>True if the packet was in state, otherwise false</returns>
            public bool UpdateSequenceNumber(RtpPacket packet)
            {
                // RFC 3550 A.1.

                ushort udelta = (ushort)(packet.SequenceNumber - RtpMaxSeq);

                /*
                * Source is not valid until MIN_SEQUENTIAL packets with
                * sequential sequence numbers have been received.
                */
                if (RtpProbation > 0)
                {
                    /* packet is in sequence */
                    if (packet.SequenceNumber == RtpMaxSeq + 1)
                    {
                        RtpProbation--;
                        RtpMaxSeq = (uint)packet.SequenceNumber;
                        if (RtpProbation == 0)
                        {
                            ResetCounters((uint)packet.SequenceNumber);
                            return true;
                        }
                    }
                    else
                    {
                        RtpProbation = (uint)(MIN_SEQUENTIAL - 1);
                        RtpMaxSeq = (uint)packet.SequenceNumber;
                    }

                    return false;
                }
                else if (udelta < MAX_DROPOUT)
                {
                    /* in order, with permissible gap */
                    if (packet.SequenceNumber < RtpMaxSeq)
                    {
                        /*
                        * Sequence number wrapped - count another 64K cycle.
                        */
                        RtpSeqCycles += RTP_SEQ_MOD;
                    }
                    RtpMaxSeq = packet.SequenceNumber;
                }
                else if (udelta <= RTP_SEQ_MOD - MAX_MISORDER)
                {
                    /* the sequence number made a very large jump */
                    if (packet.SequenceNumber == RtpBadSeq)
                    {
                        /*
                         * Two sequential packets -- assume that the other side
                         * restarted without telling us so just re-sync
                         * (c.e., pretend this was the first packet).
                        */
                        ResetCounters((uint)packet.SequenceNumber);
                    }
                    else
                    {
                        //Set the bag sequence to the packets sequence + 1 with the bits of the seq wraps -1 off
                        RtpBadSeq = (uint)((packet.SequenceNumber + 1) & (RTP_SEQ_MOD - 1));
                        return false;
                    }
                }
                else
                {
                    /* duplicate or reordered packet */
                    //return false;
                }
                RtpPacketsReceieved++;
                
                return true;
            }

            /// <summary>
            /// Creates the required sockets for the Interleave and updates the associatd Properties and Fields
            /// </summary>
            /// <param name="localIp"></param>
            /// <param name="remoteIp"></param>
            /// <param name="localRtpPort"></param>
            /// <param name="localRtcpPort"></param>
            /// <param name="remoteRtpPort"></param>
            /// <param name="remoteRtcpPort"></param>
            /// <param name="wakeup"></param>
            /// <notes>
            /// Attention Freebox Stb Users!!!!
            /// Please use 0 For remoteRtpPort and remoteRtcpPort as the Freebox Stb does not use the correct Rtp or Rtcp ports indicated in the Describe request.
            /// </notes>
            public void InitializeSockets(IPAddress localIp, IPAddress remoteIp, int localRtpPort, int localRtcpPort, int remoteRtpPort = 0, int remoteRtcpPort = 0, bool wakeup = true)
            {

                Goodbye = null;
                SendersReport = null;
                RecieversReport = null;
                SourceDescription = null;
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                if (localIp.AddressFamily != remoteIp.AddressFamily) throw new RtpClientException("localIp and remoteIp AddressFamily must match.");
                try
                {
                    //Setup the RtpSocket
                    RtpSocket = new Socket(remoteIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    RtpSocket.Bind(LocalRtp = new IPEndPoint(localIp, ClientRtpPort = localRtpPort));
                    RtpSocket.Connect(RemoteRtp = new IPEndPoint(remoteIp, ServerRtpPort = remoteRtpPort));
                    RtpSocket.SendTimeout  = RtpSocket.ReceiveTimeout = 0;
                    
                    //Tell the network stack what we send and receive has an order
                    RtpSocket.DontFragment = true;

                    //Set max ttl for slower networks
                    //RtpSocket.Ttl = 255;

                    //May help if behind a router
                    //Allow Nat Traversal
                    RtpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                    //Set type of service
                    RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                    if (wakeup)
                    {
                        //Send some bytes to ensure the result is open, if we get a SocketException the port is closed
                        try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtp); }
                        catch (SocketException) { }
                    }

                    //If we sent Rtp and Rtcp on the same socket (might mean duplex)
                    if (remoteRtpPort == remoteRtcpPort)
                    {
                        RtcpSocket = RtpSocket;
                    }
                    else if (RtcpEnabled)
                    {

                        //Setup the RtcpSocket
                        RtcpSocket = new Socket(remoteIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                        RtcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        RtcpSocket.Bind(LocalRtcp = new IPEndPoint(localIp, ClientRtcpPort = localRtcpPort));
                        RtcpSocket.Connect(RemoteRtcp = new IPEndPoint(remoteIp, ServerRtcpPort = remoteRtcpPort));
                        RtcpSocket.SendTimeout = RtcpSocket.ReceiveTimeout = 0;

                        //Tell the network stack what we send and receive has an order
                        RtcpSocket.DontFragment = true;

                        //Set max ttl for slower networks
                        //RtcpSocket.Ttl = 255;

                        //May help if behind a router
                        //Allow Nat Traversal
                        RtcpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                        //Set type of service
                        RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        if (wakeup)
                        {
                            try { RtcpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                            catch (SocketException) { }
                        }

                    }
                }
                catch
                {
                    throw;
                }
            }

            /// <summary>
            /// Uses the given socket for the Interleave and updates the associatd Properties and Fields
            /// </summary>
            /// <param name="socket">The socket to use</param>
            public void InitializeSockets(Socket socket)
            {
                Goodbye = null;
                SendersReport = null;
                RecieversReport = null;
                SourceDescription = null;
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                RemoteRtcp = RemoteRtp = ((IPEndPoint)socket.RemoteEndPoint);
                LocalRtcp = LocalRtp = ((IPEndPoint)socket.LocalEndPoint);
                ServerRtcpPort = ServerRtpPort = RemoteRtp.Port;
                RtpSocket = RtcpSocket = socket;
                //Disable Nagle
                RtpSocket.NoDelay = true;
            }

            public void CloseSockets()
            {
                //We don't close tcp sockets and if we are a Tcp socket the Rtcp and Rtp Socket are the same
                if (RtpSocket == null || RtcpSocket.ProtocolType == ProtocolType.Tcp) return;

                //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                if (RtcpSocket != null && RtpSocket.Handle != RtcpSocket.Handle && RtcpSocket.Handle.ToInt64() > 0)
                {
                    RtcpSocket.Dispose();
                    RtcpSocket = null;
                }

                //Close the RtpSocket
                if (RtpSocket != null && RtpSocket.Handle.ToInt64() > 0)
                {
                    RtpSocket.Dispose();
                    RtpSocket = null;
                }
            }
        }

        /// <summary>
        /// Encapsulates exceptions encountered by the RtpClient
        /// </summary>
        public class RtpClientException : Exception
        {
            public RtpClientException(string message) : base(message) { }
            public RtpClientException(string message, Exception innerEx) : base(message, innerEx) { }
        }

        #endregion

        #region Fields

        //Buffer for data
        internal byte[] m_Buffer = new byte[RtpPacket.MaxPacketSize + 4]; // 4 for RFC2326 + RFC4571 bytes ($,id,{len0,len1})

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;

        //Outgoing Packets
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Created from an existing socket we should not close?
        internal bool m_SocketOwner = true;

        internal List<TransportContext> TransportContexts = new List<TransportContext>();

        internal IPAddress m_RemoteAddress;

        //If we don't want to keep track of Packet Events these would be false
        //This one controls incoming counters and FrameChanged when marker packets arrive
        internal bool m_IncomingPacketEventsEnabled = true, 
            //This one controls outgoing counters
            m_OutgoingPacketEventsEnabled = true;

        #endregion

        #region Events

        public delegate void InterleaveHandler(RtpClient sender, ArraySegment<byte> slice);
        public delegate void RtpPacketHandler(RtpClient sender, RtpPacket packet);
        public delegate void RtcpPacketHandler(RtpClient sender, RtcpPacket packet);
        public delegate void RtpFrameHandler(RtpClient sender, RtpFrame frame);

        /// <summary>
        /// Raised when Interleaved Data is recieved
        /// </summary>
        public event InterleaveHandler InterleavedData;

        /// <summary>
        /// Raised when a RtpPacket is received
        /// </summary>
        public event RtpPacketHandler RtpPacketReceieved;

        /// <summary>
        /// Raised when a RtcpPacket is received
        /// </summary>
        public event RtcpPacketHandler RtcpPacketReceieved;

        /// <summary>
        /// Raised when a RtpPacket has been sent
        /// </summary>
        public event RtpPacketHandler RtpPacketSent;
        
        /// <summary>
        /// Raised when a RtcpPacket has been sent
        /// </summary>
        public event RtcpPacketHandler RtcpPacketSent;

        /// <summary>
        /// Fired when the CurrentFrame changes on a used TransportChannel
        /// </summary>
        public event RtpFrameHandler RtpFrameChanged;

        /// <summary>
        /// Depreceated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="frame"></param>
        internal static void RtpClient_RtpFrameChanged(RtpClient sender, RtpFrame frame)
        {
            //////Send the ack for the last 32 frames in UDP
            ////// Use packets in the last frame or the currentFrame
            //////Hopefully this doens't have to happen more / less then that
            //////http://tools.ietf.org/html/draft-ietf-p2psip-base-23
            /////*    enum { data(128), ack(129), (255) } FramedMessageType;
            ////struct {
            ////    FramedMessageType       type;

            ////    select (type) {
            ////    case data:
            ////        uint32              sequence;
            ////        opaque              message<0..2^24-1>;

            ////    case ack:
            ////        uint32              ack_sequence;
            ////        uint32              received;
            ////    };
            ////} FramedMessage;
            ////*/
            ////if (transportChannel.RtpSocket.ProtocolType != ProtocolType.Tcp) try
            ////    {
            ////        //ack_seq---------  //recieved--------                          
            ////        byte[] ReloadAck = new byte[] { 0x81,0xc9,0x00,0x07,0x8e,0x6e,0x0f,0x22,
            ////                                //acked_Frames----------------------- (up to 32) bits sets for consecutives
            ////                                0x42,0xd9,0x7a,0x05,0x00,0xff,0xff,0xff,0x00,0x01,0x00,0x8a,0x00,0x00,0x0a,0x2f,0x6b,0x40,0xb9,0x16,0x00,0x01,0x0c,0x3d
            ////                                      //msg
            ////                                ,0x81,0xca,0x00,0x04,0x8e,0x6e,0x0f,0x22,
            ////                                0x01,0x06,0x4a,
            ////                                //MachineName
            ////                                0x61,0x79,0x2d,0x50,0x43,0x00,0x00,0x00,0x00 };

            ////        BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(transportChannel.SequenceNumber) + 1).CopyTo(ReloadAck, 1);

            ////        //(for int f = transportChannel.CurrentFrame.HighestSequenceNumber; i = transportChannel.CurrentFrame.Count; i >= 0 --i){
            ////        // Put each sequence number in the ack and then set the bits...
            ////        //}

            ////        System.Text.Encoding.UTF8.GetBytes(Environment.MachineName).CopyTo(ReloadAck, ReloadAck.Length - 9);

            ////        transportChannel.RtpSocket.Send(ReloadAck);
            ////    }
            ////    catch { }

        }

        internal static void RtpClient_RtcpPacketReceieved(RtpClient rtpClient, RtcpPacket packet)
        {
            if (packet == null) return;

            TransportContext transportContext = rtpClient.GetContextForPacket(packet);

            //If there is no coresponding transportChannel or Rtcp is not enabled then return
            if (transportContext == null || !transportContext.RtcpEnabled) return;

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Received RtcpPacket: " + packet.PacketType + " From: " + transportContext.RemoteRtcp);
#endif
            //Synchronize
            lock (transportContext)
            {
                //Increment the counters for the transportChannel
                ++transportContext.RtcpPacketsReceieved;
                transportContext.RtcpBytesRecieved += packet.Length;

                if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport || (byte)packet.PacketType == 72)
                {
                    //Store the senders report
                    transportContext.SendersReport = new SendersReport(packet);

                    if (transportContext.SynchronizationSourceIdentifier != transportContext.SendersReport.SynchronizationSourceIdentifier)
                    {

#if DEBUG
                        //The first senders report recieved will assign the SynchronizationSourceIdentifier if not already assigned
                        if (transportContext.SynchronizationSourceIdentifier == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Assigned First SSRC = " + transportContext.SendersReport.SynchronizationSourceIdentifier);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Sender Changed SSRC = " + transportContext.SendersReport.SynchronizationSourceIdentifier);
                        }               
#endif
                        //Assign the ssrc
                        transportContext.SynchronizationSourceIdentifier = transportContext.SendersReport.SynchronizationSourceIdentifier;

                        //Get rid of the Goodbye if there was one
                        transportContext.Goodbye = null;
                    }

                    //asssign the timestamps
                    transportContext.NtpTimestamp = transportContext.SendersReport.NtpTimestamp;
                    transportContext.RtpTimestamp = transportContext.SendersReport.RtpTimestamp;

                    //Should be scheduled

                    //Create a corresponding RecieversReport
                    rtpClient.SendReceiversReport(transportContext);

                    //Should also send source description
                    rtpClient.SendSourceDescription(transportContext);
                }
                else if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport || (byte)packet.PacketType == 73)
                {
                    transportContext.RecieversReport = new ReceiversReport(packet);

                    //Should be scheduled

                    //Send a senders report
                    rtpClient.SendSendersReport(transportContext);
                }
                else if (packet.PacketType == RtcpPacket.RtcpPacketType.SourceDescription || (byte)packet.PacketType == 74)
                {
                    //Might record ssrc here
                    transportContext.SourceDescription = new SourceDescription(packet);
                }
                else if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye || (byte)packet.PacketType == 75)
                {
                    //Maybe the server should be aware when this happens?
                    transportContext.Goodbye = new Goodbye(packet);

                    if (transportContext.Goodbye.BlockCount > 0)
                    {
                        foreach (Rtcp.Goodbye.GoodbyeChunk chunk in transportContext.Goodbye)
                        {
                            if (transportContext.SynchronizationSourceIdentifier == chunk.SynchronizationSourceIdentifier)
                            {

                                //Should check chunk.Reason... if Reason.Contains("collision") ....

                                //TODO THIS SHOULD ONLY OCCUR IF TransportContexts.All(i=> i.Goodbye != null)
                                rtpClient.SendGoodbye(transportContext);
                            }
                        }
                    }
                }
                //else if (packet.PacketType == RtcpPacket.RtcpPacketType.ApplicationSpecific || (int)packet.PacketType == 76)
                //{
                //    //This is for the application, they should have their own events
                //}
            }
        }

        /// <summary>
        /// Updates counters and fires a FrameChanged event if required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        internal static void RtpClient_RtpPacketReceieved(RtpClient sender, RtpPacket packet)
        {
            if (packet == null || !sender.m_IncomingPacketEventsEnabled) return;

            //Get the transportChannel for the packet
            TransportContext transportContext = sender.GetContextForPacket(packet);

            //If the transportChannel was null
            if (transportContext == null)
            {
                //We cannot handle this packet
                return;
            }

            //Synchronize
            lock (transportContext)
            {
                //Maintain counters
                ++transportContext.RtpPacketsReceieved;
                transportContext.RtpBytesRecieved += packet.Length;

                //Update values if in state
                if (transportContext.UpdateSequenceNumber(packet))
                {
                    //Update the Jitter of the Interleave
                    transportContext.UpdateJitter(packet);

                    //Set the sequence number of the context
                    transportContext.SequenceNumber = packet.SequenceNumber;
                }

                //If we recieved a packet before we have identified who it is coming from
                if (transportContext.SynchronizationSourceIdentifier == 0)
                {
                    transportContext.SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
                }

                //This occurs when you disconnect and re-connect again mostly.
                //Eventually might not want to accept this... right now prior sessions sometime use the same ssrc if not timed out so this is allowing it
                if (transportContext.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
                {
                    transportContext.SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
                }

                //If we have not allocated a currentFrame
                if (transportContext.CurrentFrame == null)
                {
                    transportContext.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
                    transportContext.SequenceNumber = packet.SequenceNumber;
                }
                //If the transportChannels identifier is not the same as the packet then we will not handle this packet
                else if (transportContext.CurrentFrame != null && 
                    transportContext.CurrentFrame.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier || 
                    transportContext.CurrentFrame.SynchronizationSourceIdentifier != transportContext.SynchronizationSourceIdentifier)
                {
                    //it could be an injection or something else
                    return;
                }

                //If the transportChannel's CurrentFrame's TimeStamp does not match the packet TimeStamp or the Ssrc's do not match            
                if (transportContext.CurrentFrame.Timestamp != packet.TimeStamp)
                {
                    //This is possibly a new frame
                    transportContext.LastFrame = transportContext.CurrentFrame;

                    //If the lastFrame had any packets then fire the event so it may be handled
                    if (!transportContext.LastFrame.Empty)
                    {
                        //Fire the event
                        sender.RtpFrameChanged(sender, transportContext.LastFrame);
                    }

                    //Make a new frame in the transportChannel's CurrentFrame
                    transportContext.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
                }

                //Add the packet to the current frame
                transportContext.CurrentFrame.Add(packet);

                //If the frame is compelted then fire an event and make a new frame
                if (transportContext.CurrentFrame.Complete)
                {
                    //Make the LastFrame the CurrentFrame
                    transportContext.LastFrame = transportContext.CurrentFrame;

                    //Make a new frame in the CurrentFrame
                    transportContext.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);

                    //Fire the event on the LastFrame
                    sender.RtpFrameChanged(sender, transportContext.LastFrame);
                }
                else if (transportContext.CurrentFrame.Count > RtpFrame.MaxPackets)
                {
                    //Backup of frames
                    transportContext.CurrentFrame.RemoveAllPackets();
                }
            }
        }

        /// <summary>
        /// Increments the RtpBytesSent and RtpPacketsSent for the TransportChannel related to the packet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        internal static void RtpClient_RtpPacketSent(RtpClient sender, RtpPacket packet)
        {
            if (packet == null ||!sender.m_OutgoingPacketEventsEnabled) return;

            TransportContext transportContext = sender.GetContextForPacket(packet);

            if (transportContext == null) return;

            //increment the counters
            Interlocked.Add(ref transportContext.RtpBytesSent, packet.Length);
            Interlocked.Increment(ref transportContext.RtpPacketsSent);
            
            //transportContext.RtpBytesSent += packet.Length;
            //++transportContext.RtpPacketsSent;
        }

        internal static void RtpClient_RtcpPacketSent(RtpClient sender, RtcpPacket packet)
        {
            if (!sender.m_OutgoingPacketEventsEnabled) return;

            TransportContext transportContext = sender.GetContextForPacket(packet);

            if (transportContext == null) return;

            //Increment the counters
            //transportContext.RtcpBytesSent += packet.PacketLength;
            //++transportContext.RtcpPacketsSent;

            Interlocked.Add(ref transportContext.RtcpBytesSent, packet.Length);
            Interlocked.Increment(ref transportContext.RtcpPacketsSent);
        }

        internal virtual void RtpClient_InterleavedData(RtpClient sender, ArraySegment<byte> slice)
        {
            //Check for the magic byte
            if (slice.Array[slice.Offset] == MAGIC)
            {
                //Slice should be channel
                byte frameChannel = slice.Array[slice.Offset + 1];

                //The type to check for RTP or RTCP
                int version = slice.Array[slice.Offset + 4] >> 6;

                if (version != 2) return;

                //The type to check for RTP or RTCP
                byte payload = slice.Array[slice.Offset + 5];

                //If the frameChannel matches a DataChannel and the Payload type matches
                if (TransportContexts.Any(c => c.MediaDescription.MediaFormat == (byte)(payload & 0x7f) && c.DataChannel == frameChannel))
                {
                    //make a packet
                    RtpPacket packet = new RtpPacket(slice.Array, slice.Offset);

                    //Asign the channel
                    packet.Channel = frameChannel;

                    //Fire the event
                    OnRtpPacketReceieved(packet);
                }
                else if (RtcpPacket.IsKnownPacketType(payload) && TransportContexts.Any(c => c.ControlChannel == frameChannel))
                {
                    //Add all packs in the m_Buffer to the storage
                    foreach (RtcpPacket p in RtcpPacket.GetPackets(slice))
                    {
                        p.Channel = frameChannel;
                        //++m_RtcpPacketsReceieved;
                        RtcpPacketReceieved(this, p);
                    }
                }
            }
        }

        internal void OnInterleavedData(ArraySegment<byte> slice)
        {
            if (InterleavedData != null) InterleavedData(this, slice);
        }

        internal void OnInterleavedData(byte[] slice)
        {
            if (InterleavedData != null) InterleavedData(this, new ArraySegment<byte>(slice));
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            RtpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {
            RtcpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtpPacketSent(RtpPacket packet)
        {
            RtpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketSent(RtcpPacket packet)
        {
            RtcpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtpFrameHandler for the given frame if FrameEvents are enabled
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        internal void OnRtpFrameChanged(RtpFrame frame)
        {
            RtpFrameChanged(this, frame);
        }

        #endregion

        #region Properties

        public long TotalRtpPacketsSent { get { return TransportContexts.Sum(c => c.RtpPacketsSent); } }

        public long TotalRtpBytesSent { get { return TransportContexts.Sum(c => c.RtpBytesSent); } }

        public long TotalRtpBytesReceieved { get { return TransportContexts.Sum(c => c.RtpBytesRecieved); } }

        public long TotalRtpPacketsReceieved { get { return TransportContexts.Sum(c => c.RtpPacketsReceieved); } }

        public long TotalRtcpPacketsSent { get { return TransportContexts.Sum(c => c.RtcpPacketsSent); } }

        public long TotalRtcpBytesSent { get { return TransportContexts.Sum(c => c.RtcpBytesSent); } }

        public long TotalRtcpPacketsReceieved { get { return TransportContexts.Sum(c => c.RtcpPacketsReceieved); } }

        public long TotalRtcpBytesReceieved { get { return TransportContexts.Sum(c => c.RtcpBytesRecieved); } }

        public int? InactivityTimeoutSeconds { get; set; }

        public bool Connected { get { return m_WorkerThread != null; } }

        /// <summary>
        /// Gets or sets a value which prevents Incoming Rtp and Rtcp packet events from being handled
        /// </summary>
        public bool IncomingPacketEventsEnabled { get { return m_IncomingPacketEventsEnabled; } set { m_IncomingPacketEventsEnabled = false; } }

        /// <summary>
        /// Gets or sets a value which prevents Outgoing Rtp and Rtcp packet events from being handled
        /// </summary>
        public bool OutgoingPacketEventsEnabled { get { return m_OutgoingPacketEventsEnabled; } set { m_OutgoingPacketEventsEnabled = false; } }

        /// <summary>
        /// The RemoteAddress of the RtpClient
        /// </summary>
        public IPAddress RemoteAddress { get { return m_RemoteAddress; } }

        //public bool RtcpEnabled { get { return Interleaves.All(c => c.RtcpEnabled); } set { Interleaves.All(c => c.RtcpEnabled = value); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Assigns the events necessary for operation
        /// </summary>
        RtpClient()
        {
            InactivityTimeoutSeconds = 15;
            RtpPacketReceieved += new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved += new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpPacketSent += new RtpPacketHandler(RtpClient_RtpPacketSent);
            RtcpPacketSent += new RtcpPacketHandler(RtpClient_RtcpPacketSent);
            RtpFrameChanged += new RtpFrameHandler(RtpClient_RtpFrameChanged);
            InterleavedData += new InterleaveHandler(RtpClient_InterleavedData);
        }

        /// <summary>
        /// Creates a RtpClient Sender or Reciever using Udp
        /// </summary>
        /// <param name="address">The remote address</param>
        /// <param name="rtpPort">The rtp port</param>
        /// <param name="rtcpPort">The rtcp port</param>
        RtpClient(IPAddress address)
            : this()
        {

            m_RemoteAddress = address;
            m_TransportProtocol = ProtocolType.Udp;

        }

        /// <summary>
        /// Creates a Interleaved RtpClient using the given existing socket (Tcp)
        /// </summary>
        /// <param name="existing">The existing Tcp Socket</param>
        RtpClient(Socket existing)
            : this()
        {
            m_RemoteAddress = ((IPEndPoint)existing.RemoteEndPoint).Address;
            m_SocketOwner = false;
            m_TransportProtocol = existing.ProtocolType;
        }

        //Removes listeners
        ~RtpClient()
        {
            RtpPacketReceieved -= new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved -= new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpPacketSent -= new RtpPacketHandler(RtpClient_RtpPacketSent);
            RtcpPacketSent -= new RtcpPacketHandler(RtpClient_RtcpPacketSent);
            RtpFrameChanged -= new RtpFrameHandler(RtpClient_RtpFrameChanged);
            InterleavedData -= new InterleaveHandler(RtpClient_InterleavedData);
        }

        #endregion

        #region Methods

        public void AddTransportContext(TransportContext context)
        {
            lock (TransportContexts)
            {
                if (TransportContexts.Any(c => c.DataChannel == context.DataChannel || c.ControlChannel == context.ControlChannel)) throw new RtpClientException("Requested Channel is already in use");
                else TransportContexts.Add(context);
                //Should be announced here?
            }
        }

        public void RemoveTransportContext(TransportContext context)
        {
            lock (TransportContexts)
            {
                if(TransportContexts.Contains(context))
                {
                    TransportContexts.Remove(context);
                }
                //Should be announced here?
            }
        }

        #region Rtcp

        /// <summary>
        /// Sends a Goodbye to for all transportChannels, which will also stop the process sending or receiving after the Goodbye is sent
        /// //Needs SSRC
        /// </summary>
        public void SendGoodbyes() { TransportContexts.ForEach(SendGoodbye); }

        internal void SendGoodbye(TransportContext context)
        {
            //Make a Goodbye
            context.Goodbye = new Rtcp.Goodbye(context.ControlChannel);

            context.Goodbye.Add(new Goodbye.GoodbyeChunk(context.SynchronizationSourceIdentifier));

            if (context.RtcpSocket != null)
            {
                //Might want to store the goodbye incase the response is not recieved in a reasonable amount of time
                SendRtcpPacket(context.Goodbye);
                context.Goodbye.Sent = DateTime.UtcNow;
            }
        }

        internal SendersReport CreateSendersReport(TransportContext context, bool includeBlocks = true)
        {
            SendersReport result = new SendersReport(context.SynchronizationSourceIdentifier)
            {
                Channel = context.ControlChannel
            };

            //Use the values from the TransportChannel
            result.NtpTimestamp = context.NtpTimestamp;
            result.RtpTimestamp = context.RtpTimestamp;

            //Counters
            result.SendersOctetCount = (uint)context.RtpBytesSent;
            result.SendersPacketCount = (uint)context.RtpPacketsSent;

            //If source blocks are included include them and calculate their statistics
            if (includeBlocks)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(context.RtpSeqCycles + context.RtpMaxSeq);
                int expected = (int)(extended_max - context.RtpBaseSeq + 1);
                int lost = (int)(expected - context.RtpPacketsReceieved);
                int expected_interval = (int)(expected - context.RtpExpectedPrior);
                context.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(context.RtpPacketsReceieved - context.RtpReceivedPrior);
                context.RtpReceivedPrior = (uint)context.RtpPacketsReceieved;
                int lost_interval = expected_interval - received_interval;
                if (expected_interval == 0 || lost_interval <= 0)
                {
                    fraction = 0;
                }
                else
                {
                    fraction = (lost_interval << 8) / expected_interval;
                }

                #endregion

                DateTime now = DateTime.UtcNow;
                DateTime? lastSent = context.SendersReport != null && context.SendersReport.Sent.HasValue ? context.SendersReport.Sent : null; 

                //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                result.Add(new ReportBlock((uint)context.SynchronizationSourceIdentifier)
                {
                    CumulativePacketsLost = lost,
                    FractionLost = (uint)fraction,
                    InterArrivalJitter = context.RtpJitter,
                    //The middle 32 bits out of 64 in the NTP timestamp (as explained in Section 4) received as part of the most recent RTCP sender report (SR) packet from source SSRC_n. If no SR has been received yet, the field is set to zero.
                    LastSendersReport = (uint)(lastSent.HasValue ? Utility.DateTimeToNptTimestamp32(lastSent.Value) : 0),
                    //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                    DelaySinceLastSendersReport = (uint)(lastSent.HasValue ? ((now - lastSent.Value).TotalSeconds / 65536) : 0),
                    ExtendedHigestSequenceNumber = (uint)context.SequenceNumber
                });
            }

            return result;
        }

        internal ReceiversReport CreateReceiversReport(TransportContext context, bool includeBlocks = true)
        {
            ReceiversReport result = new ReceiversReport(context.SynchronizationSourceIdentifier)
            {
                Channel = context.ControlChannel
            };

            if (includeBlocks)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(context.RtpSeqCycles + context.RtpMaxSeq);
                int expected = (int)(extended_max - context.RtpBaseSeq + 1);
                int lost = (int)(expected - context.RtpPacketsReceieved);
                int expected_interval = (int)(expected - context.RtpExpectedPrior);
                context.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(context.RtpPacketsReceieved - context.RtpReceivedPrior);
                context.RtpReceivedPrior = (uint)context.RtpPacketsReceieved;
                int lost_interval = expected_interval - received_interval;
                if (expected_interval == 0 || lost_interval <= 0)
                {
                    fraction = 0;
                }
                else
                {
                    fraction = (lost_interval << 8) / expected_interval;
                }

                #endregion

                DateTime now = DateTime.UtcNow;

                //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                result.Add(new ReportBlock((uint)context.SynchronizationSourceIdentifier)
                {
                    CumulativePacketsLost = lost,
                    FractionLost = (uint)fraction,
                    InterArrivalJitter = context.RtpJitter,
                    LastSendersReport = (uint)(context.SendersReport != null ? Utility.DateTimeToNptTimestamp(context.SendersReport.Created.Value) : 0),
                    DelaySinceLastSendersReport = (uint)(context.SendersReport != null ? ((now - context.SendersReport.Created.Value).Milliseconds / 65535) * 1000 : 0),
                    ExtendedHigestSequenceNumber = (uint)context.SequenceNumber
                });

            }
            return result;
        }

        /// <summary>
        /// Sends a RtcpSendersReport for each TranportChannel
        /// </summary>
        public void SendSendersReports()
        {
            TransportContexts.ForEach(SendSendersReport);
        }

        internal void SendSendersReport(TransportContext context)
        {
            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            if (context.SynchronizationSourceIdentifier == 0)
            {
                // Guaranteed to be unique per session
                // Does not follow RFC Generation guidelines but is more performant and just as unique
                context.SynchronizationSourceIdentifier = (uint)(DateTime.UtcNow.Ticks & context.RtpSocket.Handle.ToInt64() ^ (context.DataChannel | context.ControlChannel));
            }

            //First report include no blocks (No last senders report)
            context.SendersReport = CreateSendersReport(context, context.SendersReport != null);
            SendRtcpPacket(context.SendersReport);
            context.SendersReport.Sent = DateTime.UtcNow;
        }

        public void SendReceiversReports()
        {
            TransportContexts.ForEach(SendReceiversReport);
        }

        internal void SendReceiversReport(TransportContext context)
        {
            context.RecieversReport = CreateReceiversReport(context, context.RecieversReport != null);
            SendRtcpPacket(context.RecieversReport);
            context.RecieversReport.Sent = DateTime.UtcNow;
        }

        public void SendSourceDescriptions()
        {
            TransportContexts.ForEach(SendSourceDescription);
        }

        internal void SendSourceDescription(TransportContext transportChannel)
        {
            transportChannel.SourceDescription = CreateSourceDescription(transportChannel);
            SendRtcpPacket(transportChannel.SourceDescription);
            transportChannel.SourceDescription.Sent = DateTime.UtcNow;
        }

        internal SourceDescription CreateSourceDescription(TransportContext context)
        {
            return new SourceDescription(context.ControlChannel) 
            { 
                new SourceDescription.SourceDescriptionChunk(context.SynchronizationSourceIdentifier, SourceDescription.SourceDescriptionItem.CName)
            };
        }

        /// <summary>
        /// Selects a TransportContext by matching the SynchronizationSourceIdentifier to the given sourceid
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        internal TransportContext GetContextBySourceId(uint sourceId)
        {
            return TransportContexts.Where(c => c.SynchronizationSourceIdentifier == sourceId).FirstOrDefault();
        }

        /// <summary>
        /// Selects a TransportContext by using the packet's Channel property
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        internal TransportContext GetContextForPacket(RtcpPacket packet)
        {
            if (!packet.Channel.HasValue) return null;
            return TransportContexts.Where(c => packet.Channel == c.ControlChannel).FirstOrDefault();
        }

        public void EnquePacket(RtcpPacket packet)
        {
            lock (m_OutgoingRtcpPackets)
            {
                m_OutgoingRtcpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Sends a RtcpPacket to the Rtcp port of the connected client
        /// </summary>
        /// <param name="packet">The RtcpPacket to send</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public int SendRtcpPacket(RtcpPacket packet)
        {
            TransportContext transportContext = GetContextForPacket(packet);

            //If we don't have an transportChannel to send on or the transportChannel has not been identified or Rtcp is Disabled
            if (transportContext == null || transportContext.SynchronizationSourceIdentifier == 0 || !transportContext.RtcpEnabled)
            {
                //Return
                return 0;
            }

            //Send the packet
            int sent = SendData(packet.ToBytes(), transportContext.ControlChannel, transportContext.RtcpSocket);

            //If we actually sent anything (Tcp will be + 4 bytes)
            if (sent >= packet.PacketLength)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Successfuly Sent RtcpPacket: " + packet.PacketType + " To: " + transportContext.RemoteRtcp);
#endif
                //Raise en event
                OnRtcpPacketSent(packet);
            }
            else//Failed?
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Sending RtcpPacket: " + packet.PacketType + "Failed!");
#endif
            }

            return sent;
        }

        #endregion

        #region Rtp

        /// <summary>
        /// Selects a TransportContext for a RtpPacket by matching the packet's PayloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public TransportContext GetContextForPacket(RtpPacket packet) { return GetContextByPayloadType(packet.PayloadType); }

        /// <summary>
        /// Selects a TransportContext for a RtpPacket by matching the packet's PayloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public TransportContext GetContextForFrame(RtpFrame frame) { return GetContextByPayloadType(frame.PayloadType); }

        /// <summary>
        /// Selects a TransportContext by matching the given payloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        public TransportContext GetContextByPayloadType(byte payloadType) { return TransportContexts.Where(cd => cd.MediaDescription.MediaFormat == payloadType).FirstOrDefault(); }

        /// <summary>
        /// Selects a TransportContext by matching the given socket handle to the TransportContext socket's handle
        /// </summary>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        public TransportContext GetContextBySocketHandle(IntPtr socketHandle) { return TransportContexts.Where(cd => cd.RtpSocket.Handle == socketHandle || cd.RtcpSocket.Handle == socketHandle).FirstOrDefault(); }

        /// <summary>
        /// Selects a TransportContext by matching the given socket handle to the TransportContext socket's handle
        /// </summary>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        public TransportContext GetContextBySocket(Socket socket) { return GetContextBySocketHandle(socket.Handle); }

        /// <summary>
        /// Adds a packet to the queue of outgoing RtpPackets
        /// </summary>
        /// <param name="packet">The packet to enqueue</param> (used to take the RtpCLient too but we can just check the packet payload type
        public void EnquePacket(RtpPacket packet)
        {
            lock (m_OutgoingRtpPackets)
            {
                //Add a the packet to the outgoing
                m_OutgoingRtpPackets.Add(packet);
            }
        }

        public void EnqueFrame(RtpFrame frame) { foreach (RtpPacket packet in frame) EnquePacket(packet); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void SendRtpFrame(RtpFrame frame) { foreach (RtpPacket packet in frame) SendRtpPacket(packet); }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public int SendRtpPacket(RtpPacket packet)
        {
            TransportContext transportContext = GetContextForPacket(packet);

            //If we don't have an transportChannel to send on or the transportChannel has not been identified
            if (transportContext == null || transportContext.SynchronizationSourceIdentifier == 0)
            {
                //Return
                return 0;
            }
            else if (transportContext.MediaDescription.MediaFormat != packet.PayloadType)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + transportContext.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }

            //Send the bytes
            int sent = SendData(packet.ToBytes(transportContext.SynchronizationSourceIdentifier), transportContext.DataChannel, transportContext.RtpSocket);

            //If we sent anything (Tcp will be + 4 bytes)
            if (sent >= packet.Length)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Successfully Sent RtpPacket To: " + transportContext.RemoteRtp);
#endif
                OnRtpPacketSent(packet);
            }
            else//Failed?
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Sending RtpPacket To: " + transportContext.RemoteRtp + " Failed!");
#endif
            }

            return sent;
        }

        #endregion

        #region Socket

        /// <summary>
        /// Binds and Connects the required sockets
        /// </summary>
        public void Connect()
        {
            //If the worker thread is already active then return
            if (m_WorkerThread != null) return;

            //Create the workers thread and start it.
            m_WorkerThread = new Thread(new ThreadStart(SendRecieve));
            m_WorkerThread.Name = "RtpClient-" + m_RemoteAddress.ToString();
            m_WorkerThread.Start();

        }

        /// <summary>
        /// Sends the Rtcp Goodbye and disposes the Rtp and Rtcp Sockets if we are not in Tcp Transport
        /// </summary>
        public void Disconnect()
        {
            //Tell the client we are disconnecting
            SendGoodbyes();

            //If the worker thread is working
            if (m_WorkerThread != null)
            {
                //If the worker is running
                if (m_WorkerThread.ThreadState == ThreadState.Running)
                {
                    //Attempt to join
                    if (!m_WorkerThread.Join(1000))
                    {
                        try
                        {
                            //Abort
                            m_WorkerThread.Abort();
                        }
                        catch { }
                    }
                }

                //Reset the state of the m_WorkerThread
                m_WorkerThread = null;
            }

            //Dispose Interleve Sockets
            TransportContexts.ForEach(c => c.CloseSockets());

            //Counters go away with the transportChannels
            lock (TransportContexts) TransportContexts.Clear();

            //Empty buffers
            lock (m_OutgoingRtpPackets) m_OutgoingRtpPackets.Clear();
            lock (m_OutgoingRtcpPackets) m_OutgoingRtcpPackets.Clear();
        }

        /// <summary>
        /// Recieved data on a given channel
        /// </summary>
        /// <param name="channel">The channel to recieve on, determines the socket</param>
        /// <returns>The number of bytes recieved</returns>             
        //Here we might need a way to receive from all participants rather than just one Socket under Udp
        internal int RecieveData(byte channel, Socket socket)
        {
            SocketError error = SocketError.SocketError;
            int received = 0;
            try
            {
                //If there is no socket or there are no bytes to be recieved
                if (socket == null || socket != null && !socket.Connected || socket.Available <= 0)
                {
                    //Return 
                    return 0;
                }

                //For Udp we can just recieve and incrmement
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //Recieve as many bytes as are available on the socket up to the buffer length (no frame bytes)
                    received = socket.Receive(m_Buffer, received, Math.Min(socket.Available,  m_Buffer.Length), SocketFlags.None, out error);

                    //If the send was not successful throw an error with the errorCode
                    if (error != SocketError.Success) throw new SocketException((int)error);

                    //Determine what kind of packet this is 
                    //The type to check for RTP or RTCP

                    if (received <= RtcpPacket.RtcpHeaderLength) return received;

                    //The offset we are at in the buffer
                    int offset = 0;

                    //Process the bytes in the buffer
                ProcessBuffer:
                    if (offset < received)
                    {
                        //Get the version
                        int version = m_Buffer[offset] >> 6;

                        if (version != 2) return received;

                        byte payload = m_Buffer[offset + 1];

                        TransportContext context;

                        //Handle any RtcpPackets
                        if (RtcpPacket.IsKnownPacketType(payload))
                        {
                            //Should be matching based on c.RemoteRtcp and ((IPEndPoint)socket.RemoteEndPoint)
                            context = TransportContexts.Where(c => c.ControlChannel == channel).FirstOrDefault();

                            if (context == null) return received;

                            //Handle each packet advancing the offset
                            foreach (RtcpPacket p in RtcpPacket.GetPackets(new ArraySegment<byte>(m_Buffer, offset, received - offset)))
                            {
                                p.Channel = context.ControlChannel;
                                RtcpPacketReceieved(this, p);
                                offset += p.PacketLength;
                            }

                            goto ProcessBuffer;
                        }
                        else
                        {
                            context = GetContextByPayloadType((byte)(payload & 0x7f));
                            
                            if (context == null) return received;

                            RtpPacket packet = new RtpPacket(new ArraySegment<byte>(m_Buffer, offset, received - offset));
                            packet.Channel = context.DataChannel;

                            //The end point could apparently have also changed here from udp in certain cirsumstances / configurations. Maybe have a setting to allow this
                            //GetInterleaveForPacket(packet).RemoteRtp = (IPEndPoint)socket.RemoteEndPoint;

                            OnRtpPacketReceieved(packet);

                            offset += packet.Length;

                            goto ProcessBuffer;
                        }
                    }
                    
                }
                else
                {

                    //For Tcp we must recieve the frame headers if we are using RTP

                    //Recieve a byte
                    while (1 > received)
                    {
                        received += socket.Receive(m_Buffer, received, 1, SocketFlags.None);
                    }

                    //If we don't have the $ then we must read one byte at a time until we do
                    //This is most likey a RFC2326 Rtsp Request interleaved between an incoming RtpPacket
                    if (m_Buffer[0] != MAGIC)
                    {
                        //We need a place to put the data which is unframed
                        List<byte> signalData = new List<byte>();
                        //Add the first byte of the unframed data
                        signalData.Add(m_Buffer[0]);
                        //While the first byte is not Magic
                        do
                        {
                            //Recive 1 byte
                            socket.Receive(m_Buffer, 0, 1, SocketFlags.None);

                            //If this is the MAGIC byte we are done
                            if (m_Buffer[0] == MAGIC) break;

                            //Add the byte to the signal data
                            signalData.Add(m_Buffer[0]);

                            //Do this only while we are not at the MAGIC byte
                        } while (m_Buffer[0] != MAGIC);

                        //Raise the event
                        OnInterleavedData(signalData.ToArray());
                    }

                    //The first byte in the buffer is MAGIC

                    //Receive 3 bytes more to get the channel and length
                    while (4 > received)
                    {
                        //Recieve the bytes 1 at a time
                        received += socket.Receive(m_Buffer, received, 1, SocketFlags.None);
                    }

                    //Since we are interleaving the channel may not match the channel we are recieving on but we have to handle the message so we will recieve it anyway
                    byte frameChannel = m_Buffer[1];

                    //If the channel is not recognized
                    //if (!(Channels.ToList().Any(c => c.DataChannel == frameChannel || c.ControlChannel == frameChannel)))
                    //{
                    //    //This is data for a transportChannel we do not interleave on could be an injection or something else
                    //    return received;
                    //}

                    //Decode the length since the channel is known
                    ushort supposedLength = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(m_Buffer, 2));

                    //Sometimes the length is wrong
                    supposedLength = (ushort)Math.Min((ushort)RtpPacket.MaxPayloadSize, supposedLength);

                    //Store the actual length recieved which is the minima of the RtpPacket.MaxPayloadSize and the supposedLength
                    int length = socket.Receive(m_Buffer, received, supposedLength, SocketFlags.None);

                    //If we recieved less then we were supposed to recieve the rest
                    while (length < supposedLength)
                    {
                        //Increase the amount of bytes we recieved up to supposedLength recieving at the correct index for the correct size
                        length += socket.Receive(m_Buffer, received + length, (supposedLength - length), SocketFlags.None);
                    }

                    //Increment recieved for length
                    received += length;

                    //Create a slice from the data
                    //Fire the event
                    OnInterleavedData(new ArraySegment<byte>(m_Buffer, 0, length));
                }
            }
#if DEBUG
            catch (Exception ex)
            {
                if (ex is SocketException)
                {
                    System.Diagnostics.Debug.WriteLine("SocketException occured in RtpClient.RecieveData: " + ex.Message + ". SocketError = " + error + ", ErrorCode = " + (ex as SocketException).ErrorCode);

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Exception occured in RtpClient.RecieveData: " + ex.Message + ". SocketError = " + error);
                }
            }
#else
            catch {  }
#endif
            //Return the amount of bytes we recieved
            return received;
        }

        /// <summary>
        /// Sends the given data on the given channel
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="channel">The channel to send on (Udp doesn't use it)</param>
        /// <returns>The amount of bytes sent</returns>
        //Here we might need a way to send to all participants rather than just one Socket under Udp        
        internal int SendData(byte[] data, byte channel, Socket socket)
        {
            
            SocketError error = SocketError.SocketError;
            int sent = 0;
            if (data == null || socket == null) return sent;
            try
            {
                //Under Udp we can send the packet verbatim
                if (socket.ProtocolType == ProtocolType.Udp)
                {
                    //Send data
                    sent = socket.Send(data, sent, data.Length - sent, SocketFlags.None, out error);

                    //If there was an error with the size of the datagram
                    if (sent < data.Length || error == SocketError.MessageSize)
                    {                        
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Did not send entire datagram, SocketError = \"" + error + "\", Resending rest of the data (" + (data.Length - sent) + " Bytes) with SocketFlags.Truncated.");
#endif

                        //Send the rest using Truncated
                        sent += socket.Send(data, sent, data.Length - sent, SocketFlags.Truncated, out error);
                    }
                }
                else
                {
                    //Under Tcp we must frame the data for the given channel
                    List<byte> buffer = new List<byte>(4 + data.Length);
                    buffer.Add(MAGIC);
                    buffer.Add(channel);
                    buffer.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)data.Length)));
                    buffer.AddRange(data);
                    data = buffer.ToArray();

                    //Send the frame keeping track of the bytes sent
                    sent = socket.Send(data, sent, data.Length - sent, SocketFlags.None, out error);                    
                }

                //If the send was not successful throw an error with the errorCode
                if (error != SocketError.Success) throw new SocketException((int)error);
            }
#if DEBUG
            catch (Exception ex)
            {
                string output = "Exception occured in RtpClient.SendData: " + ex.Message + ". SocketError = " + error + '"';
                if (ex is SocketException) output += ", ErrorCode = " + (ex as SocketException).ErrorCode + '"';
                System.Diagnostics.Debug.WriteLine(output);                
            }
#else
            catch {  }
#endif
            return sent;
        }

        /// <summary>
        /// Sends a RtcpGoodbye Immediately if we have not recieved a packet in the required time.
        /// </summary>
        /// <param name="lastActivity">The time the lastActivity has occured on the context (sending or recieving)</param>
        /// <param name="context">The context to check against</param>
        /// <returns>True if the connection is inactive and a Goodebye was attempted to be sent to the remote party</returns>
        internal bool SendGoodbyeIfInactive(DateTime lastActivity, TransportContext context)
        {
            bool inactive = false;

            if (context.RtcpEnabled)
            {
                //Check SR, RR last recieved / sent the same as below
                //if (true)
                //{
                //    //SendGoodbye(context);
                //    //inactive = true;
                //}
            }

            //If we have our own InactivityTimeout then enforce it (If Rtcp is disabled and the remote party does not send a Goodbye this may never happen)
            if (inactive || InactivityTimeoutSeconds.HasValue && InactivityTimeoutSeconds > 0 && (DateTime.UtcNow - lastActivity).TotalSeconds >= InactivityTimeoutSeconds)
            {
                SendGoodbye(context);
                inactive = true;
            }

            return inactive;
        }

        /// <summary>
        /// Entry point of the m_WorkerThread. Handles sending out RtpPackets and RtcpPackets in buffer and handling any incoming RtcpPackets.
        /// Sends a Goodbye and exits if no packets are sent of recieved in a certain amount of time
        /// </summary>
        internal void SendRecieve()
        {
            try
            {
                //Until aborted
                while (m_WorkerThread != null && m_WorkerThread.ThreadState != ThreadState.StopRequested)
                {
                    //Everything we send is IEnumerable
                    System.Collections.IEnumerable toSend;

                    #region Handle Outgoing RtcpPackets

                    if (m_OutgoingRtcpPackets.Count > 0)
                    {
                        lock (m_OutgoingRtcpPackets)
                        {
                            int remove = Math.Min(m_OutgoingRtpPackets.Count, RtpFrame.MaxPackets);
                            toSend = m_OutgoingRtcpPackets.GetRange(0, remove);
                            m_OutgoingRtcpPackets.RemoveRange(0, remove);
                        }

                        foreach (RtcpPacket packet in toSend)
                        {
                            //If the entire packet was sent
                            if (SendRtcpPacket(packet) >= packet.PacketLength)
                            {
                                //If we have received or sent a goodbyte
                                TransportContext context = GetContextForPacket(packet);
                                if (context.Goodbye != null) break;
                                else if (SendGoodbyeIfInactive(DateTime.UtcNow, context)) break;
                            }
                        }

                        toSend = null;
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (m_OutgoingRtpPackets.Count > 0)
                    {
                        lock (m_OutgoingRtpPackets)
                        {
                            //Could check for timestamp more recent then packet at 0  on transporContext and discard...
                            //Send only A few at a time to share with rtcp
                            int remove = Math.Min(m_OutgoingRtpPackets.Count, RtpFrame.MaxPackets);
                            toSend = m_OutgoingRtpPackets.GetRange(0, remove);
                            m_OutgoingRtpPackets.RemoveRange(0, remove);
                        }

                        foreach (RtpPacket packet in toSend)
                        {
                            //If the entire packet was sent
                            if (SendRtpPacket(packet) >= packet.Length)
                            {
                                //If we sent or received a goodbye
                                //If we send a goodebye
                                TransportContext context = GetContextForPacket(packet);
                                if (context.Goodbye != null) break;
                                else if (SendGoodbyeIfInactive(DateTime.UtcNow, context)) break;
                            }
                        }

                        toSend = null;
                    }

                    #endregion

                    #region Recieve Incoming Data

                    lock (TransportContexts)
                    {
                        try
                        {
                            bool inactive = false;

                            //Enumerate each context and receive data, if received update the lastActivity
                            TransportContexts.ForEach(c =>
                            {
                                if (RecieveData(c.DataChannel, c.RtpSocket) <= 0 || RecieveData(c.ControlChannel, c.RtcpSocket) <= 0)
                                {
                                    //If we have our own InactivityTimeout then enforce it
                                    inactive = SendGoodbyeIfInactive(DateTime.UtcNow, c);
                                }
                            });
                            
                            if(inactive) break;
                        }
                        catch (SocketException)
                        {
                            //The remote host something or other
                            //If this is happening often the Udp client disconnected
                            //Should eventually be disconnected at the server level but might want to add logic here for better standalone operation
                        }
                    }

                    #endregion

                }
            }
            catch { }
        }

        #endregion

        #endregion
    }
}
