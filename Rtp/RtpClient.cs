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

        //Rtsp over Http might require an new Socket, in that case existing would be null
        public static RtpClient Interleaved(Socket existing)
        {
            //Should verify socket type is TCP
            return new RtpClient(existing);
        }

        public static RtpClient Sender(IPAddress remoteAddress)
        {
            return new RtpClient(remoteAddress)
            {
                IncomingPacketEventsEnabled = false,
                m_IncomingPacketEventsEnabled = false
            };
        }

        public static RtpClient Receiever(IPAddress remoteAddress)
        {
            return new RtpClient(remoteAddress)
            {
                OutgoingPacketEventsEnabled = false
            };
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// New construct to hold information relevant to each channel in udp this will less useful but still applicable
        /// Counters for Rtp and Rtcp should be kept here for proper calculation.
        /// Should also be used to allow sources to be send to sinks by id using the correct ssrc
        /// This is relevant if the source has video on channel 0 and the sink gets the video on channel 2
        /// Should be created in Rtsp Setup requests or in discovery of sdp in standalone
        /// --- Might Call it Session and move the events here also
        /// </summary>
        internal class Interleave
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
            public Sdp.MediaDescription MediaDescription { get; protected set; }

            internal Socket RtpSocket, RtcpSocket;

            //Is Rtcp Enabled on this Interleave (when false will not send / recieve reports)
            public bool RtcpEnabled = true;

            //Ports we are using / will use
            internal int ServerRtpPort, ServerRtcpPort, ClientRtpPort, ClientRtcpPort;

            //The EndPoints connected to (once connected don't need the Ports)
            internal IPEndPoint LocalRtp, LocalRtcp, RemoteRtp, RemoteRtcp;

            /// <summary>
            /// The sequence number of the last RtpPacket sent or recieved on this channel
            /// </summary>
            public ushort SequenceNumber { get; internal set; }

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

            //Indicates if we sent or recieved a Goodbye
            public bool GoodbyeSent { get; internal set; }
            public bool GoodbyeRecieved { get; internal set; }

            internal Interleave(byte dataChannel, byte controlChannel, uint ssrc, bool rtcpEnabled = true)
            {
                DataChannel = dataChannel;
                ControlChannel = controlChannel;
                //if they both are the same then this could mean duplexing
                SynchronizationSourceIdentifier = ssrc;
                RtcpEnabled = rtcpEnabled;
            }

            internal Interleave(byte dataChannel, byte controlChannel, uint ssrc, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true)
                : this(dataChannel, controlChannel, ssrc, rtcpEnabled)
            {
                MediaDescription = mediaDescription;
            }

            internal Interleave(byte dataChannel, byte controlChannel, uint ssrc, Sdp.MediaDescription mediaDescription, Socket socket, bool rtcpEnabled = true)
                : this(dataChannel, controlChannel, ssrc, mediaDescription, rtcpEnabled)
            {
                RtpSocket = RtcpSocket = socket;
            }

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            internal void UpdateJitter(RtpPacket packet)
            {
                // RFC 3550 A.8.
                ulong transit = (Utility.DateTimeToNtpTimestamp(DateTime.UtcNow) - packet.TimeStamp);
                int d = (int)(transit - RtpTransit);
                RtpTransit = (uint)transit;
                if (d < 0) d = -d;
                RtpJitter += (uint)((1d / 16d) * ((double)d - RtpJitter));
            }

            internal void ResetCounters(uint sequenceNumber)
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
            internal bool UpdateSequenceNumber(RtpPacket packet)
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
                    RtpMaxSeq = (uint)packet.SequenceNumber;
                }
                else if (udelta <= RTP_SEQ_MOD - MAX_MISORDER)
                {
                    /* the sequence number made a very large jump */
                    if (packet.SequenceNumber == RtpBadSeq)
                    {
                        /*
                         * Two sequential packets -- assume that the other side
                         * restarted without telling us so just re-sync
                         * (i.e., pretend this was the first packet).
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
            internal void InitializeSockets(IPAddress localIp, IPAddress remoteIp, int localRtpPort, int localRtcpPort, int remoteRtpPort, int remoteRtcpPort)
            {

                GoodbyeRecieved = GoodbyeSent = false;
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                try
                {
                    //Setup the RtpSocket
                    RtpSocket = new Socket(remoteIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    RtpSocket.Bind(LocalRtp = new IPEndPoint(localIp, ClientRtpPort = localRtpPort));
                    RtpSocket.Connect(RemoteRtp = new IPEndPoint(remoteIp, ServerRtpPort = remoteRtpPort));
                    RtpSocket.DontFragment = true;
                    //RtpSocket.ReceiveBufferSize = RtpSocket.SendBufferSize = RtpPacket.MaxPacketSize;
                    RtpSocket.ReceiveBufferSize = RtpPacket.MaxPacketSize;
                    //May help if behind a router
                    //RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);
                    //Send some bytes to ensure the result is open, if we get a SocketException the port is closed
                    try { RtpSocket.SendTo(WakeUpBytes, RemoteRtp); }
                    catch (SocketException) { }

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
                        RtcpSocket.DontFragment = true;
                        //RtcpSocket.ReceiveBufferSize = RtcpSocket.SendBufferSize = RtpPacket.MaxPacketSize;
                        RtcpSocket.ReceiveBufferSize = RtpPacket.MaxPacketSize;
                        //May help if behind a router
                        //RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);
                        try { RtcpSocket.SendTo(WakeUpBytes, RemoteRtcp); }
                        catch (SocketException) { }

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
            internal void InitializeSockets(Socket socket)
            {
                GoodbyeRecieved = GoodbyeSent = false;
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                RemoteRtcp = RemoteRtp = ((IPEndPoint)socket.RemoteEndPoint);
                LocalRtcp = LocalRtp = ((IPEndPoint)socket.LocalEndPoint);
                ServerRtcpPort = ServerRtpPort = RemoteRtp.Port;
                RtpSocket = RtcpSocket = socket;
            }

            internal void CloseSockets()
            {
                //We don't close tcp sockets and if we are a Tcp socket the Rtcp and Rtp Socket are the same
                if (RtpSocket == null || RtcpSocket.ProtocolType == ProtocolType.Tcp) return;

                //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                if (RtcpSocket != null && RtpSocket.Handle != RtcpSocket.Handle && (int)RtcpSocket.Handle != -1)
                {
                    RtcpSocket.Dispose();
                    RtcpSocket = null;
                }

                //Close the RtpSocket
                if (RtpSocket != null && (int)RtpSocket.Handle != -1)
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
        internal byte[] m_Buffer = new byte[RtpPacket.MaxPacketSize + 4];

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;

        //Outgoing Packets
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Created from an existing socket we should not close?
        internal bool m_SocketOwner = true;

        //Channels for sending and receiving (Should be DataChannel)
        internal List<Interleave> Interleaves = new List<Interleave>();

        internal IPAddress m_RemoteAddress;

        //If we don't want to keep track of the current frames
        internal bool m_IncomingFrameEventsEnabled = true;

        //If we don't want to keep track of Packet Events
        internal bool m_IncomingPacketEventsEnabled = true, m_OutgoingPacketEventsEnabled;

        #endregion

        #region Events

        public delegate void InterleaveHandler(RtpClient sender, ArraySegment<byte> slice);
        public delegate void RtpPacketHandler(RtpClient sender, RtpPacket packet);
        public delegate void RtcpPacketHandler(RtpClient sender, RtcpPacket packet);
        public delegate void RtpFrameHandler(RtpClient sender, RtpFrame frame);

        public event InterleaveHandler InterleavedData;
        public event RtpPacketHandler RtpPacketReceieved;
        public event RtcpPacketHandler RtcpPacketReceieved;
        public event RtpPacketHandler RtpPacketSent;
        public event RtcpPacketHandler RtcpPacketSent;
        public event RtpFrameHandler RtpFrameChanged;

        internal virtual void RtpClient_RtpFrameChanged(RtpClient sender, RtpFrame frame)
        {
            //We only handle our own packets
            if (this != sender || !m_IncomingFrameEventsEnabled) return;

            //Get the interleave associated with the frame
            Interleave interleave = Interleaves.Where(i => i.SynchronizationSourceIdentifier == frame.SynchronizationSourceIdentifier).First();

            //Senders don't send recievers reports... (might want to have flags e.g. RtcpEnabled)
            if (interleave.RtpPacketsSent > 0)
            {
                return;
            }

            //Determine if we should send a recievers report and source description
            if ((interleave.RecieversReport == null && interleave.RtpPacketsReceieved > RtpFrame.MaxPackets) || interleave.RecieversReport != null && interleave.RecieversReport.Sent != null && (DateTime.UtcNow - interleave.RecieversReport.Sent.Value).TotalSeconds > 150)
            {
                SendReceiverssReports();
                SendSourceDescriptions();
            }

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
            ////if (interleave.RtpSocket.ProtocolType != ProtocolType.Tcp) try
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

            ////        BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(interleave.SequenceNumber) + 1).CopyTo(ReloadAck, 1);

            ////        //(for int f = interleave.CurrentFrame.HighestSequenceNumber; i = interleave.CurrentFrame.Count; i >= 0 --i){
            ////        // Put each sequence number in the ack and then set the bits...
            ////        //}

            ////        System.Text.Encoding.UTF8.GetBytes(Environment.MachineName).CopyTo(ReloadAck, ReloadAck.Length - 9);

            ////        interleave.RtpSocket.Send(ReloadAck);
            ////    }
            ////    catch { }

        }

        internal virtual void RtpClient_RtcpPacketReceieved(RtpClient sender, RtcpPacket packet)
        {
            //Ensure this is our event and we are handling it
            if (this != sender || !m_IncomingPacketEventsEnabled) return;

            Interleave interleave = GetInterleaveForPacket(packet);

            //If there is no coresponding interleave or Rtcp is not enabled then return
            if (interleave == null || !interleave.RtcpEnabled) return;

            //Increment the counters for the interleave
            ++interleave.RtcpPacketsReceieved;
            interleave.RtcpBytesRecieved += packet.Length;

            if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport || (int)packet.PacketType == 72)
            {
                //Store the senders report
                interleave.SendersReport = new SendersReport(packet);

                //The first senders report recieved will assign the SynchronizationSourceIdentifier if not already assigned
                if (interleave.SynchronizationSourceIdentifier == 0)
                {
                    interleave.SynchronizationSourceIdentifier = interleave.SendersReport.SynchronizationSourceIdentifier;
                }

                interleave.NtpTimestamp = interleave.SendersReport.RtpTimestamp;
                interleave.RtpTimestamp = interleave.SendersReport.RtpTimestamp;

                //Create a corresponding RecieversReport
                interleave.RecieversReport = CreateReceiversReport(interleave);
                //Send the packet to calculate the correct send time... (Should be scheduled)
                SendRtcpPacket(interleave.RecieversReport.ToPacket(interleave.ControlChannel));
                interleave.RecieversReport.Sent = DateTime.UtcNow;

                //Should also send source description
                interleave.SourceDescription = CreateSourceDescription(interleave);
                SendRtcpPacket(interleave.SourceDescription.ToPacket(interleave.ControlChannel));
                interleave.SourceDescription.Sent = DateTime.UtcNow;

            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport || (int)packet.PacketType == 73)
            {
                if (interleave.CurrentFrame == null) return;
                //http://www.freesoft.org/CIE/RFC/1889/19.htm
                interleave.RecieversReport = new ReceiversReport(packet);
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SourceDescription || (int)packet.PacketType == 74)
            {
                //Might record ssrc here
                interleave.SourceDescription = new SourceDescription(packet);
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye || (int)packet.PacketType == 75)
            {
                //Maybe the server should be aware when this happens?
                interleave.GoodbyeRecieved = true;

                //TODO THIS SHOULD ONLY OCCUR IF m_Interleaves.All(i=> i.GoodbyeRecieved)
                Disconnect();

                //If we have not send a goodbye then we need to send
                //if (!interleave.GoodbyeSent)
                //{
                //SendGoodbye(interleave);
                //}
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.ApplicationSpecific || (int)packet.PacketType == 76)
            {
                //This is for the application, they should have their own events
            }
        }

        internal virtual void RtpClient_RtpPacketReceieved(RtpClient sender, RtpPacket packet)
        {
            //Ensure this is our event and we are handling it
            if (this != sender || !m_IncomingPacketEventsEnabled) return;

            //Get the interleave for the packet
            Interleave interleave = GetInterleaveForPacket(packet);

            //If the interleave was null
            if (interleave == null)
            {
                //We cannot handle this packet
                return;
            }

            ++interleave.RtpPacketsReceieved;
            interleave.RtpBytesRecieved += packet.Length;

            //Update values
            interleave.SequenceNumber = packet.SequenceNumber;
            //interleave.RtpTimestamp = packet.TimeStamp;

            //If we recieved a packet before we have identified who it is coming from
            if (interleave.SynchronizationSourceIdentifier == 0)
            {
                interleave.SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Recieved First RtpPacket Before Interleaved was identified");
                System.Diagnostics.Debug.WriteLine("Updating SSRC From " + interleave.SynchronizationSourceIdentifier + " To " + packet.SynchronizationSourceIdentifier);
#endif
            }

            //Might not want to accept this... right now prior sessions sometime use the same ssrc so this is allowing it
            if (interleave.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
            {
                interleave.SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Recieved RtpPacket With difference SSRC on " + interleave.LocalRtp + " From " + interleave.RemoteRtp);
                System.Diagnostics.Debug.WriteLine("Updating SSRC From " + interleave.SynchronizationSourceIdentifier + " To " + packet.SynchronizationSourceIdentifier);
#endif
            }

            if (interleave.CurrentFrame == null)
            {
                interleave.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
                interleave.SequenceNumber = packet.SequenceNumber;
            }

            //If the interleaves identifier is not the same as the packet then we will not handle this packet
            if ((interleave.CurrentFrame != null && interleave.CurrentFrame.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier) || (interleave.CurrentFrame != null && interleave.CurrentFrame.SynchronizationSourceIdentifier != interleave.SynchronizationSourceIdentifier))
            {
                //it could be an injection or something else
                return;
            }

            //Update the Jitter of the Interleave
            interleave.UpdateJitter(packet);

            //If the interleave's CurrentFrame's TimeStamp does not match the packet TimeStamp or the Ssrc's do not match            
            if (interleave.CurrentFrame.TimeStamp != packet.TimeStamp)
            {
                //This is possibly a new frame
                interleave.LastFrame = interleave.CurrentFrame;

                //If the lastFrame had any packets then fire the event so it may be handled
                if (!interleave.LastFrame.Empty)
                {
                    //Fire the event
                    RtpFrameChanged(this, interleave.LastFrame);
                }

                //Make a new frame in the interleave's CurrentFrame
                interleave.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
            }

            //Add the packet to the current frame
            interleave.CurrentFrame.Add(packet);

            //If the frame is compelted then fire an event and make a new frame
            if (interleave.CurrentFrame.Complete)
            {
                //Make the LastFrame the CurrentFrame
                interleave.LastFrame = interleave.CurrentFrame;

                //Make a new frame in the CurrentFrame
                interleave.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);

                //Fire the event on the LastFrame
                RtpFrameChanged(this, interleave.LastFrame);

            }
            else if (interleave.CurrentFrame.Count > 60)
            {
                //Backup of frames
                interleave.CurrentFrame.RemoveAllPackets();
            }
        }

        internal virtual void RtpClient_RtpPacketSent(RtpClient sender, RtpPacket packet)
        {
            if (!m_OutgoingPacketEventsEnabled) return;

            Interleave interleave = GetInterleaveForPacket(packet);

            if (interleave == null) return;

            //increment the counters
            interleave.RtpBytesSent += packet.Length;
            ++interleave.RtpPacketsSent;
        }

        internal virtual void RtpClient_RtcpPacketSent(RtpClient sender, RtcpPacket packet)
        {
            if (!m_OutgoingPacketEventsEnabled) return;
            Interleave interleave = sender.GetInterleaveForPacket(packet);
            if (interleave == null) return;
            //Increment the counters
            interleave.RtcpBytesSent += packet.Length + RtcpPacket.RtcpHeaderLength;
            ++interleave.RtcpPacketsSent;
        }

        internal virtual void RtpClient_InterleavedData(RtpClient sender, ArraySegment<byte> slice)
        {
            //Check for the magic byte
            if (slice.Array[slice.Offset] == MAGIC)
            {
                //Slice should be channel
                byte frameChannel = slice.Array[slice.Offset + 1];

                //The type to check for RTP or RTCP
                byte payload = slice.Array[slice.Offset + 5];

                //If the frameChannel matches a DataChannel and the Payload type matches
                if (Interleaves.Any(i => i.MediaDescription.MediaFormat == (byte)(payload & 0x7f) && i.DataChannel == frameChannel))
                {
                    //make a packet
                    RtpPacket packet = new RtpPacket(slice.Array, slice.Offset);

                    //Asign the channel
                    packet.Channel = frameChannel;

                    //Fire the event
                    OnRtpPacketReceieved(packet);
                }
                else if (RtcpPacket.IsKnownPacketType(payload) && Interleaves.Any(i => i.ControlChannel == frameChannel))
                {
                    //ushort length = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(slice.Array, offsetStart + 2));
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
        /// Raises the RtpFrameHandler for the given frame
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        internal void OnRtpFrameChanged(RtpFrame frame)
        {
            RtpFrameChanged(this, frame);
        }

        #endregion

        #region Properties

        public long TotalRtpPacketsSent { get { return Interleaves.Sum(i => i.RtpPacketsSent); } }

        public long TotalRtpBytesSent { get { return Interleaves.Sum(i => i.RtpBytesSent); } }

        public long TotalRtpBytesReceieved { get { return Interleaves.Sum(i => i.RtpBytesRecieved); } }

        public long TotalRtpPacketsReceieved { get { return Interleaves.Sum(i => i.RtpPacketsReceieved); } }

        public long TotalRtcpPacketsSent { get { return Interleaves.Sum(i => i.RtcpPacketsSent); } }

        public long TotalRtcpBytesSent { get { return Interleaves.Sum(i => i.RtcpBytesSent); } }

        public long TotalRtcpPacketsReceieved { get { return Interleaves.Sum(i => i.RtcpPacketsReceieved); } }

        public long TotalRtcpBytesReceieved { get { return Interleaves.Sum(i => i.RtcpBytesRecieved); } }

        public int? InactivityTimeoutSeconds { get; set; }

        public bool Connected { get { return m_WorkerThread != null; } }

        /// <summary>
        /// Gets or sets a value which prevents a FrameChanged event from being handled on the RtpClient
        /// </summary>
        public bool FrameEventsEnabled { get { return m_IncomingFrameEventsEnabled; } set { m_IncomingFrameEventsEnabled = false; } }

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

        //public bool RtcpEnabled { get { return Interleaves.All(i => i.RtcpEnabled); } set { Interleaves.All(i => i.RtcpEnabled = value); } }

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

        //Provides method for adding interleaves from outside if Interleave is not exposed..
        //E.g. AddMedia(MediaDescription)
        //AddAll(SessionDescription)

        internal void AddInterleave(Interleave interleave)
        {
            lock (Interleaves)
            {
                if (Interleaves.Any(i => i.DataChannel == interleave.DataChannel || i.ControlChannel == interleave.ControlChannel)) throw new RtpClientException("ChannelId " + interleave.DataChannel + " is already in use");
                Interleaves.Add(interleave);
            }
        }

        #region Rtcp

        /// <summary>
        /// Sends a Goodbye to for all interleaves
        /// //Needs SSRC
        /// </summary>
        internal void SendGoodbyes() { Interleaves.ForEach(SendGoodbye); }

        internal void SendGoodbye(Interleave i)
        {
            //If we have assigned an id
            if (i.SynchronizationSourceIdentifier != 0)
            {
                //Check why both need to be set
                i.GoodbyeRecieved = i.GoodbyeSent = true;
                if (i.RtcpSocket != null)
                {
                    //Might want to store the goodbye incase the response is not recieved in a reasonable amount of time
                    SendRtcpPacket(new Rtcp.Goodbye((uint)i.SynchronizationSourceIdentifier).ToPacket(i.ControlChannel));
                }
            }
            else // Just indicate we did
            {
                i.GoodbyeSent = i.GoodbyeRecieved = true;
            }
        }

        internal SendersReport CreateSendersReport(Interleave i, bool includeBlocks = true)
        {
            SendersReport result = new SendersReport(i.SynchronizationSourceIdentifier);

            //Calculate the RtpTime

            //Corresponds to the same time as the NTP timestamp (above), but in the same units and with the same random offset as the RTP timestamps in data packets. 
            //This correspondence may be used for intra- and inter-media synchronization for sources whose NTP timestamps are synchronized, 
            //and may be used by media- independent receivers to estimate the nominal RTP clock frequency. Note that in most cases this timestamp will not be equal to the RTP timestamp in any adjacent data packet. 
            //Rather, it is calculated from the corresponding NTP timestamp using the relationship between the RTP timestamp counter and real time as maintained by periodically checking the wallclock time at a sampling instant.

            //Need to calculate this correctly based on the MediaDescription sample rate	                    
            Sdp.SessionDescriptionLine rtpmap = i.MediaDescription.Lines.Where(l => l.Parts[0].StartsWith("rtpmap")).FirstOrDefault();

            //If there was a RtpMap attribute line
            if (rtpmap != null)
            {
                //Make a Ntp Timestamp
                result.NtpTimestamp = Utility.DateTimeToNtpTimestamp(DateTime.UtcNow);

                //Example line codec / samplerate / channels
                //a=rtpmap:96[1]mpeg4-generic/44100/2 (44.1kHz)
                //a=rtpmap:98[1]H264/90000 (90kHz)

                //https://tools.ietf.org/id/draft-petithuguenin-avt-multiple-clock-rates-01.html

                //Get the clockrate of the media from the line and convert to kHz
                //double clockRate = uint.Parse(rtpmap.Parts[0].Split('/')[1]) / 1000;

                //Get the clockrate of the media from the line
                uint clockRate = uint.Parse(rtpmap.Parts[0].Split('/')[1], System.Globalization.CultureInfo.InvariantCulture);

                //Calculate RtpTimestamp using NtpTimestamp and clockrate
                //result.RtpTimestamp = (uint)(result.NtpTimestamp * clockRate);

                //clockRate is in seconds format
                result.RtpTimestamp = (uint)(Utility.NptTimestampToDateTime(result.NtpTimestamp).Ticks / TimeSpan.TicksPerSecond * clockRate);
            }
            else
            {
                //Just use the Timestamp's from the Interleave
                result.NtpTimestamp = i.NtpTimestamp;
            }

            //This always comes from the last senders report for the Interleave if present
            if (i.RtpTimestamp > 0) result.RtpTimestamp = i.RtpTimestamp;

            //Counters
            result.SendersOctetCount = (uint)i.RtpBytesSent;
            result.SendersPacketCount = (uint)i.RtpPacketsSent;

            //If source blocks are included include them and calculate their statistics
            if (includeBlocks)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(i.RtpSeqCycles + i.RtpMaxSeq);
                int expected = (int)(extended_max - i.RtpBaseSeq + 1);
                int lost = (int)(expected - i.RtpPacketsReceieved);
                int expected_interval = (int)(expected - i.RtpExpectedPrior);
                i.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(i.RtpPacketsReceieved - i.RtpReceivedPrior);
                i.RtpReceivedPrior = (uint)i.RtpPacketsReceieved;
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

                DateTime? lastSent = i.SendersReport != null && i.SendersReport.Sent.HasValue ? i.SendersReport.Sent : null;

                //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                result.Blocks.Add(new ReportBlock((uint)i.SynchronizationSourceIdentifier)
                {
                    CumulativePacketsLost = lost,
                    FractionLost = (uint)fraction,
                    InterArrivalJitter = i.RtpJitter,
                    //The middle 32 bits out of 64 in the NTP timestamp (as explained in Section 4) received as part of the most recent RTCP sender report (SR) packet from source SSRC_n. If no SR has been received yet, the field is set to zero.
                    LastSendersReport = (uint)(lastSent.HasValue ? Utility.DateTimeToNtpTimestamp32(lastSent.Value) : 0),
                    //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                    DelaySinceLastSendersReport = (uint)(lastSent.HasValue ? ((DateTime.UtcNow - lastSent.Value).TotalSeconds / 65536) : 0),
                    ExtendedHigestSequenceNumber = (uint)i.SequenceNumber
                });
            }

            return result;
        }

        internal ReceiversReport CreateReceiversReport(Interleave i, bool includeBlocks = true)
        {
            ReceiversReport result = new ReceiversReport(i.SynchronizationSourceIdentifier);

            if (includeBlocks)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(i.RtpSeqCycles + i.RtpMaxSeq);
                int expected = (int)(extended_max - i.RtpBaseSeq + 1);
                int lost = (int)(expected - i.RtpPacketsReceieved);
                int expected_interval = (int)(expected - i.RtpExpectedPrior);
                i.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(i.RtpPacketsReceieved - i.RtpReceivedPrior);
                i.RtpReceivedPrior = (uint)i.RtpPacketsReceieved;
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

                //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                result.Blocks.Add(new ReportBlock((uint)i.SynchronizationSourceIdentifier)
                {
                    CumulativePacketsLost = lost,
                    FractionLost = (uint)fraction,
                    InterArrivalJitter = i.RtpJitter,
                    LastSendersReport = (uint)(i.SendersReport != null ? Utility.DateTimeToNtpTimestamp(i.SendersReport.Created.Value) : 0),
                    DelaySinceLastSendersReport = (uint)(i.SendersReport != null ? ((DateTime.UtcNow - i.SendersReport.Created.Value).Milliseconds / 65535) * 1000 : 0),
                    ExtendedHigestSequenceNumber = (uint)i.SequenceNumber
                });

            }
            return result;
        }

        /// <summary>
        /// Sends a RtcpSenders report to the client for each Interleave
        /// </summary>
        internal void SendSendersReports()
        {
            Interleaves.ForEach(SendSendersReport);
        }

        internal void SendSendersReport(Interleave interleave)
        {
            interleave.SendersReport = CreateSendersReport(interleave, false);
            SendRtcpPacket(interleave.SendersReport.ToPacket());
            interleave.SendersReport.Sent = DateTime.UtcNow;
        }

        internal void SendReceiverssReports()
        {
            Interleaves.ForEach(SendReceiversReport);
        }

        internal void SendReceiversReport(Interleave interleave)
        {
            interleave.RecieversReport = CreateReceiversReport(interleave);
            SendRtcpPacket(interleave.RecieversReport.ToPacket());
            interleave.RecieversReport.Sent = DateTime.UtcNow;
        }

        internal void SendSourceDescriptions()
        {
            Interleaves.ForEach(SendSourceDescription);
        }

        internal void SendSourceDescription(Interleave interleave)
        {
            interleave.SourceDescription = CreateSourceDescription(interleave);
            SendRtcpPacket(interleave.SourceDescription.ToPacket());
            interleave.SourceDescription.Sent = DateTime.UtcNow;
        }

        internal SourceDescription CreateSourceDescription(Interleave interleave)
        {
            return new SourceDescription(interleave.SynchronizationSourceIdentifier) { SourceDescription.SourceDescriptionItem.CName };
        }

        internal Interleave GetInterleaveForPacket(RtcpPacket packet)
        {
            return Interleaves.Where(i => packet.Channel == i.ControlChannel).FirstOrDefault();
        }

        public void EnquePacket(RtcpPacket packet)
        {
            //Find channel for packet using Channel
            //var cd = GetInterleaveForPacket(packet);
            lock (m_OutgoingRtcpPackets)
            {
                m_OutgoingRtcpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Sends a RtcpPacket to the Rtcp port of the connected client
        /// Might need a SendCompund method to take an array and calculate the compound size and send for more then 1
        /// </summary>
        /// <param name="packet">The RtcpPacket to send</param>
        public int SendRtcpPacket(RtcpPacket packet)
        {
            Interleave interleave = GetInterleaveForPacket(packet);
            //If we don't have an interleave to send on or the interleave has not been identified
            if (interleave == null || interleave.SynchronizationSourceIdentifier == 0)
            {
                //Add it back
                //m_OutgoingRtcpPackets.Add(packet);

                //Return
                return 0;
            }
            //Send the packet
            int sent = SendData(packet.ToBytes(), interleave.ControlChannel, interleave.RtcpSocket);

            //If we actually sent anything
            if (sent > 0)
            {
                OnRtcpPacketSent(packet);
            }
            else
            {
                //Failed?
            }

            return sent;
        }

        #endregion

        #region Rtp

        internal Interleave GetInterleaveForPacket(RtpPacket packet)
        {
            return Interleaves.Where(cd => cd.MediaDescription.MediaFormat == packet.PayloadType).FirstOrDefault();
        }

        /// <summary>
        /// Adds a packet to the queue of outgoing RtpPackets
        /// </summary>
        /// <param name="packet">The packet to enqueue</param> (used to take the RtpCLient too but we can just check the packet payload type
        public void EnquePacket(RtpPacket packet)
        {
            //Ensure the packet can be sent
            Interleave interleave = GetInterleaveForPacket(packet);
            //If the format of packets on the interleave does not mach
            if (interleave.MediaDescription.MediaFormat != packet.PayloadType)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + interleave.MediaDescription.MediaFormat + "' Found: '" + packet.Payload + "'");
            }

            lock (m_OutgoingRtpPackets)
            {
                //Add a the packet to the outgoing
                m_OutgoingRtpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        public int SendRtpPacket(RtpPacket packet)
        {
            Interleave interleave = GetInterleaveForPacket(packet);
            //If we don't have an interleave to send on or the interleave has not been identified
            if (interleave == null || interleave.SynchronizationSourceIdentifier == 0)
            {
                //Add it back for now (Let the source decide if it's too late when it gets there)
                m_OutgoingRtpPackets.Add(packet);

                //Return
                return 0;
            }
            else if (interleave.MediaDescription.MediaFormat != packet.PayloadType)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + interleave.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }

            //Send the bytes
            int sent = SendData(packet.ToBytes(interleave.SynchronizationSourceIdentifier), interleave.DataChannel, interleave.RtpSocket);

            //If we sent anything
            if (sent > 0)
            {
                OnRtpPacketSent(packet);
            }
            else
            {
                //Failed?
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
                        //Abort
                        m_WorkerThread.Abort();
                    }
                }

                //Reset the state of the m_WorkerThread
                m_WorkerThread = null;
            }

            //Dispose Interleve Sockets
            Interleaves.ForEach(i =>
            {
                i.CloseSockets();
            });

            //Counters go away with the interleaves
            Interleaves.Clear();

            //Empty buffers
            m_OutgoingRtpPackets.Clear();
            m_OutgoingRtcpPackets.Clear();
        }

        /// <summary>
        /// Recieved data on a given channel
        /// </summary>
        /// <param name="channel">The channel to recieve on, determines the socket</param>
        /// <returns>The number of bytes recieved</returns>             
        internal int RecieveData(byte channel, Socket socket)
        {
            try
            {
                //If there is no socket or there are no bytes to be recieved
                if (socket == null || /*!socket.Connected ||*/ socket.Available <= 0)
                {
                    //Return 
                    return 0;
                }

                int recieved = 0;

                //For Udp we can just recieve and incrmement
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //Recieve as many bytes as are available on the socket
                    recieved += socket.Receive(m_Buffer, recieved, Math.Min(socket.Available, m_Buffer.Length), SocketFlags.None);

                    //Determine what kind of packet this is 
                    //The type to check for RTP or RTCP

                    byte payload = m_Buffer[1];

                    //If the frameChannel matches a DataChannel and the Payload type matches
                    if (Interleaves.Any(i => i.MediaDescription.MediaFormat == (byte)(payload & 0x7f)))
                    {
                        RtpPacket packet = new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, recieved));
                        packet.Channel = channel;

                        //The end point could apparently have also changed here
                        //GetInterleaveForPacket(packet).RemoteRtp = (IPEndPoint)socket.RemoteEndPoint;

                        OnRtpPacketReceieved(packet);
                    }
                    else if (RtcpPacket.IsKnownPacketType(payload))
                    {
                        foreach (RtcpPacket p in RtcpPacket.GetPackets(new ArraySegment<byte>(m_Buffer, 0, recieved)))
                        {
                            p.Channel = channel;
                            RtcpPacketReceieved(this, p);
                        }
                    }
                }
                else
                {

                    //For Tcp we must recieve the frame headers if we are using RTP

                    //Recieve a byte
                    while (1 > recieved)
                    {
                        recieved += socket.Receive(m_Buffer, recieved, 1, SocketFlags.None);
                    }

                    //If we don't have the $ then we must read one byte at a time until we do
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
                    while (4 > recieved)
                    {
                        //Recieve the bytes 1 at a time
                        recieved += socket.Receive(m_Buffer, recieved, 1, SocketFlags.None);
                    }

                    //Since we are interleaving the channel may not match the channel we are recieving on but we have to handle the message so we will recieve it anyway
                    byte frameChannel = m_Buffer[1];

                    //If the channel is not recognized
                    if (!(Interleaves.ToList().Any(i => i.DataChannel == frameChannel || i.ControlChannel == frameChannel)))
                    {
                        //This is data for a channel we do not interleave on could be an injection or something else
                        return recieved;
                    }

                    //Decode the length since the channel is known
                    ushort supposedLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Buffer, 2));

                    //Store the actual length recieved which is the minima of the RtpPacket.MaxPayloadSize and the supposedLength
                    int length = socket.Receive(m_Buffer, recieved, Math.Min(RtpPacket.MaxPayloadSize, (int)supposedLength), SocketFlags.None);

                    //If we recieved less then we were supposed to recieve the rest
                    while (length < supposedLength)
                    {
                        //Increase the amount of bytes we recieved up to supposedLength recieving at the correct index for the correct size
                        length += socket.Receive(m_Buffer, recieved + length, (supposedLength - length), SocketFlags.None);
                    }

                    //Increment recieved for length
                    recieved += length;

                    //Create a slice from the data
                    //Fire the event
                    OnInterleavedData(new ArraySegment<byte>(m_Buffer, 0, length));

                }
                //Return the amount of bytes we recieved
                return recieved;
            }
            catch
            {
                throw;
            }
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
                    if (error == SocketError.MessageSize)
                    {
                        //Resize the buffer
                        socket.SendBufferSize = data.Length + 1;
                        //Send again
                        sent = socket.Send(data, 0, data.Length - sent, SocketFlags.None, out error);
                    }
                    //If the send was not successful throw an error with the errorCode
                    if (error != SocketError.Success) throw new SocketException((int)error);
                }
                else
                {
                    //Under Tcp we must create and send the frame on the given channel
                    List<byte> buffer = new List<byte>(4 + data.Length);
                    buffer.Add(MAGIC);
                    buffer.Add(channel);
                    buffer.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)data.Length)));
                    buffer.AddRange(data);
                    //Send the frame incrementing the bytes sent
                    //sent += socket.Send(buffer.ToArray());
                    data = buffer.ToArray();
                    sent = socket.Send(data, sent, data.Length - sent, SocketFlags.None, out error);                    
                    //If the send was not successful throw an error with the errorCode
                    if (error != SocketError.Success) throw new SocketException((int)error);
                }
            }
#if DEBUG
            catch (Exception ex)
            {
                if (ex is SocketException)
                {
                    System.Diagnostics.Debug.WriteLine("SocketException occured in RtpClient.SendData: " + ex.Message + ". SocketError = " + error + ", ErrorCode = " + (ex as SocketException).ErrorCode);

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Exception occured in RtpClient.SendData: " + ex.Message + ". SocketError = " + error);
                }
            }
#else
            catch {  }
#endif
            return sent;
        }

        /// <summary>
        /// Entry point of the m_WorkerThread. Handles sending RtpPackets in buffer and handling any incoming RtcpPackets
        /// </summary>
        internal void SendRecieve()
        {
            try
            {
                DateTime lastTransmit = DateTime.UtcNow;

                //Until aborted
                while (true)//m_Interleaves.ToList().All( i=> !i.GoodbyeRecieved /*&& i.GoodbyeSent*/))
                {
                    //Everything we send is IEnumerable
                    System.Collections.IEnumerable toSend;

                    #region Handle Outgoing RtcpPackets

                    if (m_OutgoingRtcpPackets.Count > 0)
                    {
                        lock (m_OutgoingRtcpPackets)
                        {
                            toSend = m_OutgoingRtcpPackets.ToArray();
                            m_OutgoingRtcpPackets.Clear();
                        }
                        foreach (RtcpPacket p in toSend)
                        {
                            if (SendRtcpPacket(p) > 0) lastTransmit = DateTime.UtcNow;
                            if (GetInterleaveForPacket(p).GoodbyeSent) break;
                        }
                        toSend = null;
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (m_OutgoingRtpPackets.Count > 0) //&& m_Interleaves.All(i=>i.RtpSocket != null)
                    {
                        lock (m_OutgoingRtpPackets)
                        {
                            //Potentially a lot of packets in m_OutGoingPackets
                            toSend = m_OutgoingRtpPackets.ToArray();//Where(p=>GetInterleaveForPacket(p).RtpSocket != null && i.RtcpEnabled)
                            m_OutgoingRtpPackets.Clear();
                        }
                        foreach (RtpPacket p in toSend)
                        {
                            if(SendRtpPacket(p) > 0) lastTransmit = DateTime.UtcNow;
                            if (GetInterleaveForPacket(p).GoodbyeSent) break;
                        }
                        toSend = null;
                    }

                    #endregion

                    #region Recieve Incoming Data

                    lock (Interleaves)
                    {

                        try
                        {
                            //Enumerate each interleave and recive data
                            Interleaves.ForEach(i =>
                            {
                                if (RecieveData(i.DataChannel, i.RtpSocket) > 0) lastTransmit = DateTime.UtcNow;
                                else if (RecieveData(i.ControlChannel, i.RtcpSocket) > 0) lastTransmit = DateTime.UtcNow;
                            });
                        }
                        catch (SocketException)
                        {
                            //The remote host something or other
                            //If this is happening often the Udp client disconnected
                            //Should eventually be disconnected at the server level but might want to add logic here for better standalone operation
                        }

                        if (InactivityTimeoutSeconds.HasValue && InactivityTimeoutSeconds > 0 && (DateTime.UtcNow - lastTransmit).TotalSeconds >= InactivityTimeoutSeconds)
                        {
                            Disconnect();
                            break;
                        }

                    }
                    #endregion

                }
            }
            catch
            {
                //Thread aborted
                //return;
            }
        }

        #endregion

        #endregion
    }
}
