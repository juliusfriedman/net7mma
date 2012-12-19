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
    /// Implementation of Rtp RFC3550
    /// Sends and Recieves Rtcp Packets according to the spec.
    /// UpdateSequenceNumber and CalculateJitter logic (Ensure Correct)
    /// Cleanup how we connect and instantiate the RtpClients
    /// Add logic for stand alone operation (rtp Uri) to sdp
    /// Should possibly abstract participants into a list so each client can clear identify who it's sending too.
    /// </summary>
    public class RtpClient
    {
        #region Nested Types

        /// <summary>
        /// Encapsulates exceptions encountered by the RtpClient
        /// </summary>
        public class RtpClientException : Exception
        {
            public RtpClientException(string message) : base(message) { }
            public RtpClientException(string message, Exception innerEx) : base(message, innerEx) { }
        }

        #endregion

        #region Statics

        static uint RTP_SEQ_MOD = (1 << 16);

        public static RtpClient Interleaved(Socket existing)
        {
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

        #region Fields

        ///SHOULD KEEP A ReportBlock rather then seperately allocated fields and counters :P

        //Recieved times
        internal DateTime m_LastSendersReportSent,
                          m_LastRecieversReportSent,
                          m_LastRecieversReportRecieved,
                          m_LastSendersReportRecieved;

        //Reports
        internal ReceiversReport m_LastRecieversReport;
        internal SendersReport m_LastSendersReport;


        //rtp and rtcp ports (Could go away in favor of the EndPoints)
        internal int m_ClientRtpPort, m_ClientRtcpPort,
                     m_ServerRtpPort, m_ServerRtcpPort;

        //bytes and packet counters
        internal int m_RtpSent, m_RtpRecieved,
                     m_RtcpSent, m_RtcpRecieved,
                     m_RtpPacketsSent, m_RtcpPacketsSent,
                     m_RtpPacketsReceieved, m_RtcpPacketsReceieved;

        //Used for Rtp and Rtcp Transport Calculations
        internal uint m_RtpTransit,
            //Count of bytes recieved prior to the reception of a report
            m_RtpReceivedPrior,
            //Count of bytes expected prior to the recpetion of a report
            m_RtpExpectedPrior,
            //The amount of times the Seq number has cycled
            m_RtpSeqCycles,
            //The amount of base RTP Sequences encountered
            m_RtpBaseSeq,
            //The highest Sequence recieved by the RtpClient
            m_RtpMaxSeq,
            //Rtp Probation value
            m_RtpProbation,
            //The amount of bad RTP Sequences encountered
            m_RtpBadSeq,
            //Unique identifier for this stream
            m_RtpSSRC,
            //Jitter value
            m_RtpJitter;

        //Channels for sending and recieving data
        internal byte m_RtpChannel = 0,
                     m_RtcpChannel = 1;

        //Buffer for data
        internal byte[] m_Buffer = new byte[RtpPacket.MaxSize];

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Sockets
        internal Socket m_RtpSocket, m_RtcpSocket;

        //Indicates if we send or the client sent us a goodbye
        internal bool m_RtcpGoodbyeRecieved, m_RtcpGoodbyeSent;

        //EndPoints
        IPAddress m_RemoteAddress;
        IPEndPoint m_LocalRtp, m_LocalRtcp, m_RemoteRtp, m_RemoteRtcp;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;

        //Storage
        //Packets first and last recieved
        internal RtpPacket m_FirstRtpPacket, m_LastRtpPacket;
        //Outgoing RtpPackets
        internal List<RtpPacket> m_RtpPackets = new List<RtpPacket>();
        //Incoming Rtcp Packing, Outgoing RtcpPackets
        internal List<RtcpPacket> m_RtcpPackets = new List<RtcpPacket>(), m_RtcpPacketSendLog = new List<RtcpPacket>();
        //Incoming RtpFrames last completed and current working
        volatile internal RtpFrame m_LastFrame, m_CurrentFrame;

        //Created from an existing socket we should not close?
        bool existingSocket;

        //Recieved from SourceDescription...
        internal string m_CName;

        #endregion

        #region Events

        public delegate void RtpPacketHandler(RtpClient sender, RtpPacket packet);
        public delegate void RtcpPacketHandler(RtpClient sender, RtcpPacket packet);
        public delegate void RtpFrameHandler(RtpClient sender, RtpFrame frame);

        public event RtpPacketHandler RtpPacketReceieved;
        public event RtcpPacketHandler RtcpPacketReceieved;
        public event RtpFrameHandler RtpFrameCompleted;

        internal virtual void RtpClient_RtpFrameCompleted(RtpClient sender, RtpFrame frame)
        {
            //No Op
            //Here events are to be hooked to get new frames
        }

        internal virtual void RtpClient_RtcpPacketReceieved(RtpClient sender, RtcpPacket packet)
        {
            if (this != sender) return;
            m_RtcpPackets.Add(packet);
            ++m_RtcpPacketsReceieved;
            if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport)
            {
                if (m_LastRtpPacket == null) return;
                //http://www.freesoft.org/CIE/RFC/1889/19.htm
                m_LastRecieversReportRecieved = DateTime.UtcNow;
                m_LastRecieversReport = new ReceiversReport(packet);
                SendSendersReport();
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport)
            {
                //Ensure source streams recieve Rtcp
                m_LastSendersReportRecieved = DateTime.UtcNow;
                //m_LastSendersReport = new SendersReport(packet);
                //SendReceiverssReport();//Should be scheduled...
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SourceDescription)
            {
                SourceDescription sd = new SourceDescription(packet);
                foreach (SourceDescription.SourceDescriptionItem sdi in sd.Items)
                {
                    if (sdi.DescriptionType == SourceDescription.SourceDescriptionType.CName)
                    {
                        m_CName = sdi.Text;
                    }
                }
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
            {
                //Maybe the server should be aware when this happens?
                m_RtcpGoodbyeRecieved = true;
                Disconnect();
            }
        }

        internal virtual void RtpClient_RtpPacketReceieved(RtpClient sender, RtpPacket packet)
        {
            //Ensure this is our event
            if (this != sender) return;
            
            //Set the first packet if required
            if (m_FirstRtpPacket == null) m_FirstRtpPacket = packet;

            //If a duplicate packet we don't need it
            //if (!UpdateSequenceNumber(packet)) return;

            //Set the last packet
            m_LastRtpPacket = packet;

            CalculateJitter(packet);

            //If we have not allocatd a frame allocte it
            if (m_CurrentFrame == null) m_CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);

            //Add the packet to the current frame
            m_CurrentFrame.AddPacket(packet);

            //If the frame is compelted then fire an event and make a new frame
            if (m_CurrentFrame.Complete)
            {
                //Store the last complted frame
                m_LastFrame = m_CurrentFrame;
                
                //Make a new frame
                m_CurrentFrame = new RtpFrame(packet.PayloadType, packet.TimeStamp, packet.SynchronizationSourceIdentifier);
                
                //Fire the event
                RtpFrameCompleted(this, m_LastFrame);
            }
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
        internal void OnRtpFrameReceived(RtpFrame frame)
        {
            RtpFrameCompleted(this, frame);
        }

        #endregion

        #region Properties

        public int RtpPacketsSent { get { return m_RtpPacketsSent; } }

        public int RtpBytesSent { get { return m_RtpSent; } }

        public int RtpBytesReceieved { get { return m_RtpRecieved; } }

        public List<RtcpPacket> SentRtcpPackets  {get { return m_RtcpPacketSendLog;} }

        public byte RtpChannel { get { return m_RtpChannel; } set { m_RtpChannel = value; } }

        public byte RtcpChannel { get { return m_RtcpChannel; } set { m_RtcpChannel = value; } }

        public int RtcpPacketsSent { get { return m_RtcpPacketsSent; } }

        public int RtcpBytesSent { get { return m_RtcpSent; } }

        public int RtcpBytesReceieved { get { return m_RtcpRecieved; } }

        public IPEndPoint LocalRtpEndPoint { get { return m_LocalRtp; } }

        public IPEndPoint RemoteRtpEndPoint { get { return m_RemoteRtp; } }

        public IPEndPoint LocalRtcpEndPoint { get { return m_LocalRtcp; } }

        public IPEndPoint RemoteRtcpEndPoint { get { return m_RemoteRtcp; } }

        public uint SynchronizationSourceIdentifier { get { return m_RtpSSRC; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Assigns the events necessary for operation
        /// </summary>
        RtpClient()
        {
            RtpPacketReceieved += new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved += new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpFrameCompleted += new RtpFrameHandler(RtpClient_RtpFrameCompleted);
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
            m_RtpSSRC = (uint)rtpPort;// Guaranteed to be unique per session

            //Non interleaved over udp required two sockets
            m_RtpSocket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            m_RtpSocket.Blocking = false;
            m_RtpSocket.DontFragment = true;

            m_RtcpSocket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_RtcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            m_RtcpSocket.Blocking = false;
            m_RtpSocket.DontFragment = true;         
        }

        /// <summary>
        /// Creates a Interleaved RtpClient using the given existing socket (Tcp)
        /// </summary>
        /// <param name="existing">The existing Tcp Socket</param>
        RtpClient(Socket existing) : this()
        {
            existingSocket = true;
            m_RemoteRtcp = m_RemoteRtp = ((IPEndPoint)existing.RemoteEndPoint);
            m_ClientRtpPort = m_ClientRtcpPort = m_RemoteRtp.Port;
            m_RemoteAddress = m_RemoteRtp.Address;
            m_TransportProtocol = existing.ProtocolType;
            m_RtpSocket = m_RtcpSocket = existing;
            //Create a ssrc
            m_RtpSSRC = (uint)existing.Handle << 32;// Guaranteed to be unique per session
        }

        //Calls disconnect and removes listeners
        ~RtpClient()
        {
            RtpPacketReceieved -= new RtpPacketHandler(RtpClient_RtpPacketReceieved);
            RtcpPacketReceieved -= new RtcpPacketHandler(RtpClient_RtcpPacketReceieved);
            RtpFrameCompleted -= new RtpFrameHandler(RtpClient_RtpFrameCompleted);
            Disconnect();
        }

        #endregion

        #region Methods

        #region Rtcp

        /// <summary>
        /// Sends a bye to the Rtcp port of the connected client
        /// </summary>
        internal void SendGoodbye()
        {
            m_RtcpGoodbyeSent = true;
            SendRtcpPacket(new Rtcp.Goodbye((uint)m_RtpSSRC).ToPacket());
        }

        /// <summary>
        /// When a ReceiversReport is recieved on a RtspSession it will need to send back a senders report with certian values calulcated correctly for playback.
        /// </summary>
        /// <param name="session">The session from which the ReceiversReport was  recieved</param>
        /// <returns>The complete SendersReport which should be sent back to the client who sent the ReceiversReport</returns>
        /// This probably needs to take a Recievers report to ensure that we are sending the correct information to each sender / reciever
        /// Should ensure this needs to be specific to each reciever though for now this is working...
        internal SendersReport CreateSendersReport()
        {
            //think this is right..
            SendersReport result = new SendersReport(m_LastRecieversReport.SynchronizationSourceIdentifier);

            result.NtpTimestamp = Utility.DateTimeToNtp32(DateTime.UtcNow);

            result.RtpTimestamp = m_LastRtpPacket.TimeStamp;//From the last rtpPacket

            result.SendersOctetCount = (uint)m_RtpSent;
            result.SendersPacketCount = (uint)m_RtpPacketsSent;

            #region Delay and Fraction

            //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

            //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
            int fraction = 0;
            uint extended_max = (uint)(m_RtpSeqCycles + m_RtpMaxSeq);
            int expected = (int)(extended_max - m_RtpBaseSeq + 1);
            int lost = (int)(expected - m_RtpPacketsReceieved);
            int expected_interval = (int)(expected - m_RtpExpectedPrior);
            m_RtpExpectedPrior = (uint)expected;
            int received_interval = (int)(m_RtpPacketsReceieved - m_RtpReceivedPrior);
            m_RtpReceivedPrior = (uint)m_RtpPacketsReceieved;
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
            result.Blocks.Add(new ReportBlock((uint)m_RtpSSRC)
            {
                CumulativePacketsLost = (uint)lost,
                FractionLost = (uint)fraction,
                InterArrivalJitter = m_RtpJitter,
                LastSendersReport = (uint)(m_LastSendersReport != null ? Utility.DateTimeToNtp32(m_LastSendersReportSent) : 0),
                DelaySinceLastSendersReport = (uint)(m_LastSendersReport != null ? ((DateTime.UtcNow - m_LastSendersReportSent).Milliseconds / 65535) * 1000 : 0),
                ExtendedHigestSequenceNumber = (uint)(m_LastRtpPacket != null ? m_LastRtpPacket.SequenceNumber : 0)
            });

            return result;
        }

        internal ReceiversReport CreateReceiversReport() { throw new NotImplementedException(); }

        /// <summary>
        /// Sends a RtcpSenders report to the client
        /// </summary>
        internal void SendSendersReport()
        {
            m_LastSendersReport = CreateSendersReport();
            SendRtcpPacket(m_LastSendersReport.ToPacket());
            m_LastSendersReportSent = DateTime.Now;
            
        }

        internal void SendReceiverssReport()
        {
            m_LastRecieversReport = CreateReceiversReport();
            SendRtcpPacket(m_LastRecieversReport.ToPacket());
            m_LastRecieversReportSent = DateTime.Now;
            SendSourceDescription();
        }

        private void SendSourceDescription()
        {
            //CreateSourceDecription();
            //Send
        }

        /// <summary>
        /// Sends a RtcpPacket to the Rtcp port of the connected client
        /// Might need a SendCompund method to take an array and calculate the compound size and send for more then 1
        /// </summary>
        /// <param name="packet">The RtcpPacket to send</param>
        internal void SendRtcpPacket(RtcpPacket packet)
        {
            int sent = SendData(packet.ToBytes(), m_RtcpChannel);
            if (sent > 0)
            {
                m_RtcpSent += sent;
                m_RtcpPacketSendLog.Add(packet);
                ++m_RtcpPacketsSent;
            }
        }

        #endregion

        #region Rtp

        /// <summary>
        /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        internal void CalculateJitter(RtpPacket packet)
        {
            // RFC 3550 A.8.
            int transit = (int)(Utility.DateTimeToNtp32(DateTime.Now) - packet.TimeStamp);
            int d = (int)(transit - m_RtpTransit);
            m_RtpTransit = (uint)transit;
            if (d < 0) d = -d;
            m_RtpJitter += (uint)((1d / 16d) * ((double)d - m_RtpJitter));
        }

        internal bool UpdateSequenceNumber(RtpPacket packet)
        {
            // RFC 3550 A.1.

            ushort udelta = (ushort)(packet.SequenceNumber - m_RtpMaxSeq);
            //const
            int MAX_DROPOUT = 3000;
            int MAX_MISORDER = 100;
            int MIN_SEQUENTIAL = 2;

            /*
            * Source is not valid until MIN_SEQUENTIAL packets with
            * sequential sequence numbers have been received.
            */
            if (m_RtpProbation > 0)
            {
                /* packet is in sequence */
                if (packet.SequenceNumber == m_RtpMaxSeq + 1)
                {
                    m_RtpProbation--;
                    m_RtpMaxSeq = (uint)packet.SequenceNumber;
                    if (m_RtpProbation == 0)
                    {
                        m_RtpBaseSeq = (uint)packet.SequenceNumber;
                        m_RtpMaxSeq = (uint)packet.SequenceNumber;
                        m_RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
                        m_RtpSeqCycles = 0;
                        m_RtpPacketsReceieved = 1;
                        m_RtpReceivedPrior = 0;
                        m_RtpReceivedPrior = 0;
                        return true;
                    }
                }
                else
                {
                    m_RtpProbation = (uint)(MIN_SEQUENTIAL - 1);
                    m_RtpMaxSeq = (uint)packet.SequenceNumber;
                }

                return false;
            }
            else if (udelta < MAX_DROPOUT)
            {
                /* in order, with permissible gap */
                if (packet.SequenceNumber < m_RtpMaxSeq)
                {
                    /*
                    * Sequence number wrapped - count another 64K cycle.
                    */
                    m_RtpSeqCycles += RTP_SEQ_MOD;
                }
                m_RtpMaxSeq = (uint)packet.SequenceNumber;
            }
            else if (udelta <= RTP_SEQ_MOD - MAX_MISORDER)
            {
                /* the sequence number made a very large jump */
                if (packet.SequenceNumber == m_RtpBadSeq)
                {
                    /*
                     * Two sequential packets -- assume that the other side
                     * restarted without telling us so just re-sync
                     * (i.e., pretend this was the first packet).
                    */
                    m_RtpBaseSeq = (uint)packet.SequenceNumber;
                    m_RtpMaxSeq = (uint)packet.SequenceNumber;
                    m_RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
                    m_RtpSeqCycles = 0;
                    m_RtpPacketsReceieved = 0;
                    m_RtpReceivedPrior = 0;
                    m_RtpReceivedPrior = 0;

                }
                else
                {
                    m_RtpBadSeq = (uint)((packet.SequenceNumber + 1) & (RTP_SEQ_MOD - 1));

                    return false;
                }
            }
            else
            {
                /* duplicate or reordered packet */
            }
            m_RtpPacketsReceieved++;

            return true;
        }

        /// <summary>
        /// Adds a packet to the queue of outgoing RtpPackets
        /// </summary>
        /// <param name="packet">The packet to enqueue</param> (used to take the RtpCLient too but we can just check the packet payload type
        internal void EnquePacket(RtpPacket packet)
        {
            lock (m_RtpPackets)
            {
                m_RtpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        internal void SendRtpPacket(RtpPacket packet)
        {
            int sent = SendData(packet.ToBytes(false, m_RtpSSRC), m_RtpChannel);
            if (sent > 0)
            {
                m_RtpSent += sent;
                ++m_RtpPacketsSent;
            }
        }

        #endregion

        #region Socket

        /// <summary>
        /// Binds and Connects the required sockets
        /// </summary>
        public void Connect()
        {

            if (m_WorkerThread != null) return;

            if (!existingSocket)
            {
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //Might need to store the reciever flag?
                    m_RtpSocket.Bind(m_LocalRtp);
                    m_RtpSocket.Connect(m_RemoteAddress, m_ServerRtpPort);
                    m_RtcpSocket.Bind(m_LocalRtcp);
                    m_RtcpSocket.Connect(m_RemoteRtcp);//Above should be like this too?
                    //m_RtcpSocket.Connect(m_RemoteAddress, m_ServerRtcpPort);
                }
                else
                {
                    m_RtpSocket.Bind(m_LocalRtp);
                    m_RtpSocket.Connect(m_RemoteAddress, m_ServerRtpPort);
                }
            }

            //Create the workers thread and start it.
            m_WorkerThread = new Thread(new ThreadStart(SendRecieve));
            m_WorkerThread.Name = "RtpClient-" + m_RtpSSRC;
            m_WorkerThread.Start();

        }

        /// <summary>
        /// Sends the Rtcp Goodbye and disposes the Rtp and Rtcp Sockets if we are not in Tcp Transport
        /// </summary>
        public void Disconnect()
        {
            if (m_WorkerThread != null)
            {
                //Tell the client we are disconnecting
                if (!m_RtcpGoodbyeRecieved) SendGoodbye();

                //If we are using Udp transport we can dispose our Sockets
                if (!existingSocket && m_TransportProtocol != ProtocolType.Tcp)
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

                //Attempt to join and if not abort the worker
                if (!m_WorkerThread.Join(1000)) m_WorkerThread.Abort();
                m_WorkerThread = null;
            }            
            //Empty buffers
            m_RtpPackets.Clear();
            m_RtcpPackets.Clear();
            
            //Reset Counters
            m_RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
            m_RtpSeqCycles = 0;
            m_RtpPacketsReceieved = 0;
            m_RtpReceivedPrior = 0;
            m_RtpReceivedPrior = 0;            
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
                Socket socket;
                int rec = 0;
                if (channel == m_RtpChannel) socket = m_RtpSocket;
                else socket = m_RtcpSocket;
                //If there are bytes to recieved
                if (socket == null || socket.Available <= 0) return rec;
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    //For Udp we can just recieve and incrmement
                    rec += socket.Receive(m_Buffer);

                    if (channel == m_RtpChannel)
                    {
                        OnRtpPacketReceieved(new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, rec)));
                        return rec;
                    }

                }
                else
                {
                    if (socket.Available > 0)
                    {
                        //For Tcp we must recieve the frame
                        rec = socket.Receive(m_Buffer, 4, SocketFlags.None);
                    }

                    //If we don't have the $ then
                    if (m_Buffer[0] != 36)
                    {
                        //Not a RtcpPacket
                        return rec;
                    }

                    byte rtpChannel = m_Buffer[1];

                    //If the channel is not recognized
                    if (rtpChannel != m_RtcpChannel && rtpChannel != m_RtpChannel)
                    {
                        //This is probably a RtspMessage
                        return rec;
                    }

                    //Decode the length
                    short length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Buffer, 2));

                    if (socket.Available > 0)
                    {
                        //Recieve length bytes
                        rec += socket.Receive(m_Buffer, Math.Min(length, m_Buffer.Length), SocketFlags.None);
                    }

                    if (rtpChannel == m_RtpChannel)
                    {
                        OnRtpPacketReceieved(new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, length)));
                        return rec;
                    }

                }

                if (rec > 0 && channel == m_RtcpChannel)
                {
                    //Add all packs in the m_Buffer to the storage
                    foreach (RtcpPacket p in RtcpPacket.GetPackets(m_Buffer))
                    {
                        //++m_RtcpPacketsReceieved;
                        RtcpPacketReceieved(this, p);
                    }
                    return rec;
                }

                return 0;
            }
            catch
            {
                //ToDO Handle this appropriately
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
        internal int SendData(byte[] data, byte channel)
        {
            int sent = 0;
            if (data == null) return sent;
            try
            {
                //Under Udp we can send the packet verbatim
                if (m_TransportProtocol == ProtocolType.Udp)
                {
                    if (channel == m_RtcpChannel)
                    {
                        sent = m_RtcpSocket.SendTo(data, m_RemoteRtcp);
                    }
                    else
                    {
                        sent = m_RtpSocket.SendTo(data, m_RemoteRtp);
                    }
                }
                else
                {
                    //Under Tcp we must create and send the frame on the given channel
                    //Create the frame header
                    m_Buffer[0] = 36;
                    m_Buffer[1] = channel;
                    //Create the length
                    BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length)).CopyTo(m_Buffer, 2);
                    //Copy the message
                    data.CopyTo(m_Buffer, 4);
                    //Send the frame incrementing the bytes sent
                    sent = m_RtpSocket.Send(m_Buffer, 4 + data.Length, SocketFlags.None);
                }
            }
            catch
            {
                //Writing the packet encounter an error
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

                //While we have not send or recieved a Goodbye
                while (!/*m_RtcpGoodbyeSent || */m_RtcpGoodbyeRecieved)
                {
                    System.Collections.IEnumerable toSend;

                    #region Send Out RtpPackets in Buffer

                    if (m_RtpPackets.Count > 0)
                    {
                        lock (m_RtpPackets)
                        {
                            toSend = m_RtpPackets.ToArray();
                            m_RtpPackets.Clear();
                        }
                        foreach (RtpPacket p in toSend)
                        {
                            ++m_RtpPacketsSent;
                            SendRtpPacket(p);
                        }
                        toSend = null;
                    }

                    #endregion

                    #region Recieve Incoming Data

                    int rec = 0;

                    //Recieve any incoming RtpPackets
                    rec = RecieveData(m_RtpChannel);

                    if (rec > 0)
                    {
                        lastRecieve = DateTime.Now;
                        m_RtpRecieved += rec;
                    }
                        

                    //Recieve any incoming RtcpPackets
                    rec = RecieveData(m_RtcpChannel);
                    
                    if (rec > 0)
                    {
                        lastRecieve = DateTime.Now;
                        m_RtcpRecieved += rec;
                    }

                    //if ((DateTime.Now - lastRecieve).TotalSeconds > 5)
                    //{
                    //    //Disconnect();
                    //}

                    #endregion

                    //If the last recieved report was longer then X mintes ago then Disconnect?

                    #region Handle Incoming RtcpPackets

                    if (m_RtcpPackets.Count > 0)
                    {
                        lock (m_RtcpPackets)
                        {
                            toSend = m_RtcpPackets.ToArray();
                            m_RtcpPackets.Clear();
                        }
                        foreach (RtcpPacket p in toSend)
                        {
                            OnRtcpPacketReceieved(p);
                        }
                        toSend = null;
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
