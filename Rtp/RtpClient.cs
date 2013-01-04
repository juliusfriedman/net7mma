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
    /// Sends and Recieves Rtcp Packets according to the spec.    
    /// ToDo:
    /// Figure out if each Interleave needs it's own Socket for Udp
    /// Add logic for stand alone operation (rtp Uri) to sdp
    /// Abstract participants into a list so each client can clearly identify who it's sending too.
    /// </summary>
    public class RtpClient
    {
        #region Statics

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

        public static RtpClient Sender(IPAddress remoteAddress, int rtpPort, int rtcpPort)
        {
            return new RtpClient(remoteAddress, rtpPort, rtcpPort);
        }

        public static RtpClient Receiever(IPAddress remoteAddress, int rtpPort, int rtcpPort)
        {
            return new RtpClient(remoteAddress, rtpPort, rtcpPort, true);
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
        internal class Interleave
        {
            //The id of the channel 0 - 255
            internal readonly byte DataChannel, ControlChannel;

            //The ssrc packets are sent out with under this channel
            internal uint SynchronizationSourceIdentifier;

            //Any frames for this channel
            internal RtpFrame CurrentFrame, LastFrame;

            //Allow for mapping from source to sink allows to see what type of media and format it expects on a given channel
            public Sdp.MediaDescription MediaDescription { get; protected set; }

            ////I THINK THE PORTS ARE THE REASON FOR THE VIDEO / AUDIO NOT COMING OUT RIGHT
            //I am using this to test if that is the case
            internal Socket RtpSocket, RtcpSocket;
            
            internal int ServerRtpPort, ServerRtcpPort, ClientRtpPort, ClientRtcpPort;
            internal IPEndPoint RemoteRtp, RemoteRtcp;
            

            //bytes and packet counters
            internal int RtpBytesSent, RtpBytesRecieved,
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
            internal ReceiversReport RecieversReport;
            internal SendersReport SendersReport;
            internal SourceDescription SourceDescription;

            internal bool GoodbyeSent, GoodbyeRecieved;

            internal Interleave(byte rtp, byte rtcp, uint ssrc)
            {
                DataChannel = rtp;
                ControlChannel = rtcp;
                SynchronizationSourceIdentifier = ssrc;
            }

            internal Interleave(byte rtp, byte rtcp, uint ssrc, Sdp.MediaDescription mediaDescription)
                :this(rtp, rtcp, ssrc)
            {
                MediaDescription = mediaDescription;
            }

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            internal void UpdateJitter(RtpPacket packet)
            {
                // RFC 3550 A.8.
                uint transit = (Utility.DateTimeToNtp32(DateTime.Now) - packet.TimeStamp);
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


            public RtpPacket LastRtpPacket { get; set; }
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

        //rtp and rtcp ports (Could go away in favor of the EndPoints)
        internal int m_ClientRtpPort, m_ClientRtcpPort,
                     m_ServerRtpPort, m_ServerRtcpPort;

        //Buffer for data
        internal byte[] m_Buffer = new byte[RtpPacket.MaxPacketSize + 4];

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Sockets
        internal Socket m_RtpSocket, m_RtcpSocket;

        //EndPoints
        IPAddress m_RemoteAddress;
        IPEndPoint m_LocalRtp, m_LocalRtcp, m_RemoteRtp, m_RemoteRtcp;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;

        //Outgoing Packets
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();
        
        //Created from an existing socket we should not close?
        bool m_SocketOwner;        

        //Channels for sending and receiving (Should be DataChannel)
        internal List<Interleave> m_Interleaves = new List<Interleave>();      

        #endregion

        #region Events

        public delegate void InterleaveHandler(RtpClient sender, ArraySegment<byte> slice);
        public delegate void RtpPacketHandler(RtpClient sender, RtpPacket packet);
        public delegate void RtcpPacketHandler(RtpClient sender, RtcpPacket packet);
        public delegate void RtpFrameHandler(RtpClient sender, RtpFrame frame);

        public event InterleaveHandler InterleavedData;
        public event RtpPacketHandler RtpPacketReceieved;
        public event RtcpPacketHandler RtcpPacketReceieved;
        public event RtpFrameHandler RtpFrameChanged;

        internal virtual void RtpClient_RtpFrameChanged(RtpClient sender, RtpFrame frame)
        {
            //No Op
            //Here events are to be hooked to get new frames
        }

        internal virtual void RtpClient_RtcpPacketReceieved(RtpClient sender, RtcpPacket packet)
        {
            if (this != sender) return;
            
            Interleave interleave = GetInterleaveForPacket(packet);
            
            if (interleave == null) return;
            
            //Increment the counters for the interleave
            ++interleave.RtcpPacketsReceieved;
            interleave.RtcpBytesRecieved += packet.Length;

            if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport)
            {
                if (interleave.LastRtpPacket == null) return;
                //http://www.freesoft.org/CIE/RFC/1889/19.htm
                interleave.RecieversReport = new ReceiversReport(packet);
                //SendSendersReport();
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport)
            {
                //Ensure source streams recieve Rtcp
                interleave.SendersReport = new SendersReport(packet);
                
                //Ensure this is correct
                if (interleave.SynchronizationSourceIdentifier == 0)
                {
                    interleave.SynchronizationSourceIdentifier = interleave.SendersReport.SynchronizationSourceIdentifier;
                }

                //Create a corresponding RecieversReport
                interleave.RecieversReport = CreateReceiversReport(interleave);
                //Change the RecieversReport into a RtcpPacket

                //Send the packet to calculate the correct send time...
                //Should be scheduled (figure out how to set sent after sending. Could do a check on the type if the packet being sent in SendRtcpPacket)
                SendRtcpPacket(interleave.RecieversReport.ToPacket(interleave.ControlChannel));
                interleave.RecieversReport.Sent = DateTime.UtcNow;
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SourceDescription)
            {
                //Might record ssrc here
                interleave.SourceDescription = new SourceDescription(packet);               
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
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
        }

        internal virtual void RtpClient_RtpPacketReceieved(RtpClient sender, RtpPacket packet)
        {
            //Ensure this is our event
            if (this != sender) return;
            
            Interleave interleave = GetInterleaveForPacket(packet);

            //If the interleave was null
            if (interleave == null)
            {
                //We cannot handle this packet
                return;
            }

            ++interleave.RtpPacketsReceieved;
            interleave.RtpBytesRecieved += packet.Length;

            //TODO Determine if this is correct
            if (interleave.SynchronizationSourceIdentifier == 0)
            {
                interleave.SynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
            }

            //If the interleave's CurrentFrame is null allocate one based on the packet recieved
            if (interleave.CurrentFrame == null)
            {
                interleave.CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
            }

            if (interleave.CurrentFrame.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
            {
                //should be ignored.. it could be an injection
                return;
            }

            //Set the last packet of the interleave
            interleave.LastRtpPacket = packet; //Could make property Update Jitter

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
            interleave.CurrentFrame.AddPacket(packet);

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

        internal virtual void RtpClient_InterleavedData(RtpClient sender, ArraySegment<byte> slice)
        {
            int offsetStart = 0;
            //ParseSlice:
            if (offsetStart < slice.Array.Length && slice.Array[offsetStart] == MAGIC)
            {
                //Slice should be channel
                byte frameChannel = slice.Array[offsetStart + 1];

                //The type to check for RTP or RTCP
                byte payload = slice.Array[offsetStart + 5];

                //If the frameChannel matches a DataChannel and the Payload type matches
                if (m_Interleaves.Any(i => i.MediaDescription.MediaFormat == (byte)(payload & 0x7f) && i.DataChannel == frameChannel))
                {
                    //make a packet
                    RtpPacket packet = new RtpPacket(slice.Array, offsetStart);
                    
                    //Asign the channel
                    packet.Channel = frameChannel;

                    //Decode the length
                    ushort alength = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(slice.Array, offsetStart + 2));

                    //If the packet length is bigger than expected resize the payload
                    if (packet.Length > alength)
                    {
                        //The length includes the RTPHeader Length for some reason
                        Array.Resize<byte>(ref packet.m_Payload, alength - RtpPacket.RtpHeaderLength);
                    }
                    
                    //Move the offset
                    offsetStart += packet.Length;

                    //Fire the event
                    OnRtpPacketReceieved(packet);
                    
                    //Check for more packets
                    ////if (offsetStart < slice.Array.Length)
                    ////{
                    ////    while (offsetStart < slice.Array.Length && slice.Array[offsetStart] != MAGIC) offsetStart++;
                    ////    goto ParseSlice;
                    ////}
                }
                else if ((payload >= (byte)RtcpPacket.RtcpPacketType.SendersReport && payload <= (byte) RtcpPacket.RtcpPacketType.ApplicationSpecific) && m_Interleaves.Any(i => i.ControlChannel == frameChannel))
                {
                    //ushort length = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(slice.Array, offsetStart + 2));
                    //Add all packs in the m_Buffer to the storage
                    foreach (RtcpPacket p in RtcpPacket.GetPackets(slice))
                    {
                        p.Channel = frameChannel;
                        //++m_RtcpPacketsReceieved;
                        RtcpPacketReceieved(this, p);
                        offsetStart += p.Length;
                    }

                    ////if (offsetStart < slice.Array.Length)
                    ////{
                    ////    while (offsetStart < slice.Array.Length && slice.Array[offsetStart] != MAGIC) offsetStart++;
                    ////    goto ParseSlice;
                    ////}

                }
                else
                {
                    ////offsetStart++;
                    ////while (offsetStart < slice.Array.Length && slice.Array[offsetStart] != MAGIC) offsetStart++;
                    ////goto ParseSlice;
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
        /// Raises the RtpPacket Handler
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            RtpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {
            RtcpPacketReceieved(this, packet);
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

        public int TotalRtpPacketsSent { get { return m_Interleaves.Sum(i => i.RtpPacketsSent); } }

        public int TotalRtpBytesSent { get { return m_Interleaves.Sum(i => i.RtpBytesSent); } }

        public int TotalRtpBytesReceieved { get { return m_Interleaves.Sum(i => i.RtpBytesRecieved); } }

        public int TotalRtpPacketsReceieved { get { return m_Interleaves.Sum(i => i.RtpPacketsReceieved); } }

        public int TotalRtcpPacketsSent { get { return m_Interleaves.Sum(i => i.RtcpPacketsSent); } }

        public int TotalRtcpBytesSent { get { return m_Interleaves.Sum(i => i.RtcpBytesSent); } }

        public int TotalRtcpPacketsReceieved { get { return m_Interleaves.Sum(i => i.RtcpPacketsReceieved); } }

        public int TotalRtcpBytesReceieved { get { return m_Interleaves.Sum(i => i.RtcpBytesRecieved); } }

        public IPEndPoint LocalRtpEndPoint { get { return m_LocalRtp; } }

        public IPEndPoint RemoteRtpEndPoint { get { return m_RemoteRtp; } }

        public IPEndPoint LocalRtcpEndPoint { get { return m_LocalRtcp; } }

        public IPEndPoint RemoteRtcpEndPoint { get { return m_RemoteRtcp; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Assigns the events necessary for operation
        /// </summary>
        RtpClient()
        {
            RtpPacketReceieved += new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved += new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpFrameChanged += new RtpFrameHandler(RtpClient_RtpFrameChanged);
            InterleavedData += new InterleaveHandler(RtpClient_InterleavedData);
        }

        /// <summary>
        /// Creates a RtpClient Sender or Reciever
        /// </summary>
        /// <param name="address">The remote address</param>
        /// <param name="rtpPort">The rtp port</param>
        /// <param name="rtcpPort">The rtcp port</param>
        RtpClient(IPAddress address, int rtpPort, int rtcpPort, bool recevier = false)
            :this()
        {

            m_RemoteAddress = address;

            //Handle the role reversal
            if (recevier)
            {
                m_ClientRtpPort = Utility.FindOpenUDPPort(30000);
                m_ClientRtcpPort = m_ClientRtpPort + 1;
                //Store the client ports
                m_ServerRtpPort = rtpPort;
                m_ServerRtcpPort = rtcpPort;
            }
            else
            {
                //Was for senders
                //m_RtcpSocket.Bind(m_LocalRtcp);
                //m_RtcpSocket.Connect(address, m_ClientRtcpPort);

                m_ServerRtpPort = Utility.FindOpenUDPPort(30000);
                m_ServerRtcpPort = m_ServerRtpPort + 1;
                //Store the client ports
                m_ClientRtpPort = rtpPort;
                m_ClientRtcpPort = rtcpPort;
            }

            //Might need to swap  these above to ensurce rtcp is recieved when we are a recevier
            m_LocalRtp = new IPEndPoint(IPAddress.Any, m_ServerRtpPort);
            m_LocalRtcp = new IPEndPoint(IPAddress.Any, m_ServerRtcpPort);
            m_RemoteRtp = new IPEndPoint(m_RemoteAddress, m_ClientRtpPort);
            m_RemoteRtcp = new IPEndPoint(m_RemoteAddress, m_ClientRtcpPort);

            m_TransportProtocol = ProtocolType.Udp;

            //Create a ssrc
            //m_RtpSSRC = (uint)(DateTime.Now.Ticks ^ rtpPort);// Guaranteed to be unique per session

            //Non interleaved over udp required two sockets for ease ...... could do with just one
            m_RtpSocket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_RtpSocket.ReceiveBufferSize = RtpPacket.MaxPacketSize;
            
            //m_RtpSocket.Blocking = false;
            m_RtpSocket.DontFragment = true;

            m_RtcpSocket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_RtcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_RtcpSocket.ReceiveBufferSize = RtpPacket.MaxPacketSize;

            //m_RtcpSocket.Blocking = false;
            m_RtpSocket.DontFragment = true;         
        }

        /// <summary>
        /// Creates a Interleaved RtpClient using the given existing socket (Tcp)
        /// </summary>
        /// <param name="existing">The existing Tcp Socket</param>
        RtpClient(Socket existing) : this()
        {
            m_SocketOwner = false;
            m_RemoteRtcp = m_RemoteRtp = ((IPEndPoint)existing.RemoteEndPoint);
            m_ClientRtpPort = m_ClientRtcpPort = m_RemoteRtp.Port;
            m_RemoteAddress = m_RemoteRtp.Address;
            m_TransportProtocol = existing.ProtocolType;
            m_RtpSocket = m_RtcpSocket = existing;
            //Create a ssrc
            //m_RtpSSRC = (uint)(DateTime.Now.Ticks ^ existing.Handle.ToInt64());// Guaranteed to be unique per session
        }

        //Calls disconnect and removes listeners
        ~RtpClient()
        {
            RtpPacketReceieved -= new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved -= new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpFrameChanged -= new RtpFrameHandler(RtpClient_RtpFrameChanged);
            InterleavedData -= new InterleaveHandler(RtpClient_InterleavedData);
            Disconnect();
        }

        #endregion

        #region Methods

        #region Rtcp

        internal void AddInterleave(Interleave interleave)
        {
            lock (m_Interleaves)
            {
                if (m_Interleaves.Any(i => i.DataChannel == interleave.DataChannel || i.ControlChannel == interleave.ControlChannel)) throw new RtpClientException("ChannelId " + interleave.DataChannel + " is already in use");
                m_Interleaves.Add(interleave);
            }
        }

        /// <summary>
        /// Sends a bye to the Rtcp port of the connected client
        /// //Needs SSRC
        /// </summary>
        internal void SendGoodbye() { m_Interleaves.ForEach(SendGoodbye); }
        internal void SendGoodbye(Interleave i)
        {
            //If we have assigned an id
            if (i.SynchronizationSourceIdentifier != 0)
            {
                //Check why both need to be set
                i.GoodbyeRecieved = i.GoodbyeSent = true;
                SendRtcpPacket(new Rtcp.Goodbye((uint)i.SynchronizationSourceIdentifier).ToPacket(i.ControlChannel));
            }
            else // Just indicate we did
            {
                i.GoodbyeSent = i.GoodbyeRecieved = true;
            }
        }

        /// <summary>
        /// When a ReceiversReport is recieved on a RtspSession it will need to send back a senders report with certian values calulcated correctly for playback.
        /// </summary>
        /// <param name="session">The session from which the ReceiversReport was  recieved</param>
        /// <returns>The complete SendersReport which should be sent back to the client who sent the ReceiversReport</returns>
        /// This probably needs to take a Recievers report to ensure that we are sending the correct information to each sender / reciever
        /// Should ensure this needs to be specific to each reciever though for now this is working...
        //SHOULD TAKE A SSRC or channel
        internal SendersReport CreateSendersReport(Interleave i)
        {
            //This needs to the given ssrc or the ssrc for the rtcp channel
            SendersReport result = new SendersReport(i.SynchronizationSourceIdentifier);

            result.NtpTimestamp = Utility.DateTimeToNtp32(DateTime.UtcNow);

            result.RtpTimestamp = i.LastRtpPacket.TimeStamp;//From the last rtpPacket

            result.SendersOctetCount = (uint)i.RtpBytesSent;
            result.SendersPacketCount = (uint)i.RtpPacketsSent;

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
                CumulativePacketsLost = (uint)lost,
                FractionLost = (uint)fraction,
                InterArrivalJitter = i.RtpJitter,
                LastSendersReport = (uint)(i.SendersReport != null ? Utility.DateTimeToNtp32(i.SendersReport.Sent.Value) : 0),
                DelaySinceLastSendersReport = (uint)(i.SendersReport != null ? ((DateTime.UtcNow - i.SendersReport.Sent.Value).Milliseconds / 65535) * 1000 : 0),
                ExtendedHigestSequenceNumber = (uint)(i.CurrentFrame != null && i.CurrentFrame.Count > 0 ? i.CurrentFrame.HighestSequenceNumber : i.LastRtpPacket.SequenceNumber)
            });

            return result;
        }

        internal ReceiversReport CreateReceiversReport(Interleave i)
        { //This needs to the given ssrc or the ssrc for the rtcp channel
            ReceiversReport result = new ReceiversReport(i.SynchronizationSourceIdentifier);

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
                CumulativePacketsLost = (uint)lost,
                FractionLost = (uint)fraction,
                InterArrivalJitter = i.RtpJitter,
                LastSendersReport = (uint)(i.SendersReport != null ? Utility.DateTimeToNtp32(i.SendersReport.Created.Value) : 0),
                DelaySinceLastSendersReport = (uint)(i.SendersReport != null ? ((DateTime.UtcNow - i.SendersReport.Created.Value).Milliseconds / 65535) * 1000 : 0),
                ExtendedHigestSequenceNumber = (uint)(i.CurrentFrame != null && i.CurrentFrame.Count > 0 ? i.CurrentFrame.HighestSequenceNumber : i.LastRtpPacket != null ? i.LastRtpPacket.SequenceNumber : 0)
            });            

            return result;
        }

        //Should be moved to the interleave or be make to take an interleave

        /// <summary>
        /// Sends a RtcpSenders report to the client
        /// //should be plural?
        /// </summary>
        internal void SendSendersReport()
        {
            //m_LastSendersReport = CreateSendersReport();
            //SendRtcpPacket(m_LastSendersReport.ToPacket());
            //m_LastSendersReportSent = DateTime.Now;
            
        }

        internal void SendReceiverssReport()
        {
            //m_LastRecieversReport = CreateReceiversReport();
            //SendRtcpPacket(m_LastRecieversReport.ToPacket());
            //m_LastRecieversReportSent = DateTime.Now;
            //SendSourceDescription();
        }

        private void SendSourceDescription()
        {
            //CreateSourceDecription();
            //Send
        }

        internal Interleave GetInterleaveForPacket(RtcpPacket packet)
        {
            //Here if the channel just happens to be the same it will get send on a wrong channel... 
            return m_Interleaves.Where(i => packet.Channel == i.ControlChannel).FirstOrDefault();
        }

        /// <summary>
        /// Adds a packet to the queue of outgoing RtpPackets
        /// </summary>
        /// <param name="packet">The packet to enqueue</param> (used to take the RtpCLient too but we can just check the packet payload type
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
        public void SendRtcpPacket(RtcpPacket packet)
        {
            Interleave interleave = GetInterleaveForPacket(packet);
            if (interleave == null || interleave.SynchronizationSourceIdentifier == 0)
            {
                //Add it back
                //m_OutgoingRtcpPackets.Add(packet);

                //Return
                return;
            }
            int sent = SendData(packet.ToBytes(), interleave.ControlChannel, interleave.RemoteRtcp);
            if (sent > 0)
            {
                interleave.RtcpBytesSent += sent;
                ++interleave.RtcpPacketsSent;
            }
        }

        #endregion

        #region Rtp

        /// <summary>
        /// Adds a packet to the queue of outgoing RtpPackets
        /// </summary>
        /// <param name="packet">The packet to enqueue</param> (used to take the RtpCLient too but we can just check the packet payload type
        public void EnquePacket(RtpPacket packet)
        {
            //Ensure the packet can be sent
            Interleave interleave = GetInterleaveForPacket(packet);
            if (interleave.MediaDescription.MediaFormat != packet.PayloadType)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + interleave.MediaDescription.MediaFormat + "' Found: '" + packet.Payload + "'");
            }
            lock (m_OutgoingRtpPackets)
            {
                m_OutgoingRtpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        public void SendRtpPacket(RtpPacket packet)
        {
            Interleave interleave = GetInterleaveForPacket(packet);
            if (interleave == null || interleave.SynchronizationSourceIdentifier == 0)
            {
                //Add it back
                //m_OutgoingRtpPackets.Add(packet);

                //Return
                return;
            } 
            else if (interleave.MediaDescription.MediaFormat != packet.PayloadType)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + interleave.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }
            //If Enable Mixing
            //packet.ContributingSources.Add(packet.SynchronizationSourceIdentifier);
            int sent = SendData(packet.ToBytes(interleave.SynchronizationSourceIdentifier), interleave.DataChannel, interleave.RemoteRtp);
            if (sent > 0)
            {
                interleave.RtpBytesSent += sent;
                ++interleave.RtpPacketsSent;
            }
        }

        #endregion

        internal Interleave GetInterleaveForPacket(RtpPacket packet)
        {
            return m_Interleaves.Where(cd => cd.MediaDescription.MediaFormat == packet.PayloadType).FirstOrDefault();
        }

        #region Socket

        /// <summary>
        /// Binds and Connects the required sockets
        /// </summary>
        public void Connect()
        {
            //If the worker thread is already active then return
            if (m_WorkerThread != null) return;

            //If we own the socket
            if (m_SocketOwner)
            {
                //If the protocol is Udp
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //Bind the socket so recieve on m_LocalRtp 
                    m_RtpSocket.Bind(m_LocalRtp);
                    
                    //Connect the socket so RemoteEndpoint is m_RemoteAddress
                    m_RtpSocket.Connect(m_RemoteAddress, m_ServerRtpPort);
                    
                    //Bind the socket so recieve on m_LocalRtcp
                    m_RtcpSocket.Bind(m_LocalRtcp);
                    
                    //Connect the socket so RemoteEndPoint is m_RemoteRtcp
                    m_RtcpSocket.Connect(m_RemoteRtcp);//Above should be like this too?
                }
                else
                {
                    //Bind the socket to recieve on m_LocalRtp
                    m_RtpSocket.Bind(m_LocalRtp);

                    //Connect the socket to m_RemoteAddress
                    m_RtpSocket.Connect(m_RemoteAddress, m_ServerRtpPort);
                }
            }

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
            //If the worker thread is working
            if (m_WorkerThread != null)
            {
                //Tell the client we are disconnecting
                SendGoodbye();

                //If we are using Udp transport we can dispose our Sockets
                if (m_SocketOwner && m_TransportProtocol != ProtocolType.Tcp)
                {
                    if (m_RtcpSocket != null)
                    {
                        m_RtcpSocket.Dispose();
                        m_RtcpSocket = null;
                    }

                    if (m_RtpSocket != null)
                    {
                        m_RtpSocket.Dispose();
                        m_RtpSocket = null;
                    }
                }

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
            m_Interleaves.ForEach(i =>
            {
                if (i.RtpSocket != null)
                {
                    i.RtpSocket.Close();
                    i.RtpSocket = null;
                }

                if (i.RtcpSocket != null)
                {
                    i.RtcpSocket.Close();
                    i.RtcpSocket = null;
                }
            });
            //Empty buffers
            m_OutgoingRtpPackets.Clear();
            m_OutgoingRtcpPackets.Clear();
        }


        /// <summary>
        /// Recieved data on a given channel
        /// </summary>
        /// <param name="channel">The channel to recieve on, determines the socket</param>
        /// <returns>The number of bytes recieved</returns>             
        internal int RecieveData(byte channel)
        {
            try
            {
                Socket socket = null;

                //Find the socket based on the given channel
                m_Interleaves.ForEach(i =>
                {
                    //if (i.DataChannel == channel) socket = m_RtpSocket;
                    //else if (i.ControlChannel == channel) socket = m_RtcpSocket;

                    //We need to use the interleave socket unless it has not yet been created
                    if (i.DataChannel == channel)
                    {
                        socket = i.RtpSocket;
                        if (socket == null) socket = m_RtpSocket;//Probably Tcp
                    }
                    else
                    {
                        socket = i.RtcpSocket;
                        if (socket == null) socket = m_RtcpSocket;//Probably Tcp
                    }
                });

                //If there is no socket or there are no bytes to be recieved
                if (socket == null || !socket.Connected || socket.Available <= 0)
                {
                    //Return 
                    return 0;
                }

                int recieved = 0;

                //For Udp we can just recieve and incrmement
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //Recieve as many bytes as are available on the socket
                    recieved += socket.Receive(m_Buffer, recieved, socket.Available, SocketFlags.None);

                    //Use the channel again to determine how to handle the response if it is a valid RtpPacket or RtcpPacket
                    if (recieved > RtpPacket.RtpHeaderLength && m_Interleaves.Any(i => i.DataChannel == channel))
                    {
                        RtpPacket packet = new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, recieved));
                        packet.Channel = channel;
                        OnRtpPacketReceieved(packet);
                    }
                    else if (m_Interleaves.Any(i => i.DataChannel == channel))
                    {
                        foreach (RtcpPacket p in RtcpPacket.GetPackets(m_Buffer))
                        {
                            p.Channel = channel;
                            RtcpPacketReceieved(this, p);
                        }
                    }
                }
                else
                {

                    //For Tcp we must recieve the frame headers 
                    //SEE RFC4751
                    //Magic byte for RTSP, pure TCP/RTP/AVP there is no Magic byte see => http://tools.ietf.org/html/rfc4571#page-2)

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

                            if (m_Buffer[0] == MAGIC) break;

                            //Add the byte to the signal data
                            signalData.Add(m_Buffer[0]);
                        } while (m_Buffer[0] != MAGIC);

                        //Raise the event
                        OnInterleavedData(signalData.ToArray());
                    }

                    //Receive 3 bytes more to get the channel and length
                    while (4 > recieved)
                    {
                        //Recieve the bytes 1 at a time
                        recieved += socket.Receive(m_Buffer, recieved, 1, SocketFlags.None);
                    }

                    //Since we are interleaving the channel may not match the channel we are recieving on but we have to handle the message
                    byte frameChannel = m_Buffer[1];

                    //If the channel is not recognized
                    if (!(m_Interleaves.ToList().Any(i => i.DataChannel == frameChannel || i.ControlChannel == frameChannel)))
                    {
                        //This is data for a channel we do not interleave on could be an injection or something else
                        return recieved;
                    }

                    //Decode the length since the channel is known
                    ushort supposedLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Buffer, 2));

                    //Store the actual length recieved which is the minima of the RtpPacket.MaxPayloadSize and the supposedLength
                    int length = socket.Receive(m_Buffer, recieved, Math.Min(RtpPacket.MaxPayloadSize, (int)supposedLength), SocketFlags.None);

                    //If we recieved less then we were supposed to
                    while (length < supposedLength)
                    {
                        //Increase the amount of bytes we recieved up to supposedLength
                        length += socket.Receive(m_Buffer, recieved + length, (supposedLength - length), SocketFlags.None);
                    }

                    //Recieve the rest of the message into the buffer at the offset 0
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
                return 0;
            }
        }


        /// <summary>
        /// Sends the given data on the given channel
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="channel">The channel to send on</param>
        /// <returns>The amount of bytes sent</returns>
        /// //Here we might need a way to send to all participants rather than just one Socket
        public int SendData(byte[] data, byte channel, IPEndPoint point  = null)
        {
            int sent = 0;
            if (data == null) return sent;
            try
            {
                //Under Udp we can send the packet verbatim
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    if (m_Interleaves.Any(i => i.ControlChannel == channel))
                    {
                        //sent = m_RtcpSocket.SendTo(data, point ?? m_RemoteRtcp);
                        var il = m_Interleaves.Where(i => i.ControlChannel == channel).First();
                        sent += il.RtcpSocket.SendTo(data, point);
                    }
                    else
                    {
                        //sent = m_RtpSocket.SendTo(data, point ?? m_RemoteRtp);
                        //We have to have a seperate socket per Interleave ....
                        var il = m_Interleaves.Where(i => i.DataChannel == channel).First();
                        sent += il.RtpSocket.SendTo(data, point);
                    }
                }
                else
                {
                    //Under Tcp we must create and send the frame on the given channel
                    //Create the frame header
                    m_Buffer[0] = MAGIC;
                    m_Buffer[1] = channel;
                    //Create the length
                    BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)data.Length)).CopyTo(m_Buffer, 2);
                    //Copy the message
                    data.CopyTo(m_Buffer, 4);
                    //Send the frame incrementing the bytes sent
                    sent = m_RtpSocket.Send(m_Buffer, 4 + data.Length, SocketFlags.None);
                }
            }
            catch
            {
                return sent;
            }
            return sent;
        }


        /// <summary>
        /// Entry point of the m_WorkerThread. Handles sending RtpPackets in buffer and handling any incoming RtcpPackets
        /// </summary>
        internal void SendRecieve()
        {
            try
            {
                DateTime lastRecieve = DateTime.Now;

                //While we have not recieved a Goodbye
                while (m_Interleaves.ToList().Any( i=> !i.GoodbyeRecieved /*&& i.GoodbyeSent*/))
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
                            SendRtcpPacket(p);
                        }
                        toSend = null;
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (m_OutgoingRtpPackets.Count > 0) //&& m_Interleaves.All(i=>i.RtpSocket != null)
                    {
                        lock (m_OutgoingRtpPackets)
                        {
                            toSend = m_OutgoingRtpPackets.ToArray();//Where(p=>GetInterleaveForPacket(p).RtpSocket != null)
                            m_OutgoingRtpPackets.Clear();
                        }
                        foreach (RtpPacket p in toSend)
                        {
                            SendRtpPacket(p);
                        }
                        toSend = null;
                    }

                    #endregion

                    #region Recieve Incoming Data

                    lock (m_Interleaves)
                    {
                        m_Interleaves.ForEach(i =>
                        {
                            RecieveData(i.DataChannel);
                            RecieveData(i.ControlChannel);
                        });

                        //if ((DateTime.Now - lastRecieve).TotalSeconds > 5)
                        //{
                        //    //Disconnect();
                        //}

                        //If last send was long ago may need to send some type of keep alive?

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
