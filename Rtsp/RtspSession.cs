using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Media.Rtp;
using Media.Rtcp;
using System.Threading;

namespace Media.Rtsp
{
    /// <summary>
    /// Class which is created for each RtspClient which requests data from us
    /// </summary>
    internal class RtspSession
    {
        #region Fields

        internal RtspServer m_Server;
        
        internal Guid m_Id = Guid.NewGuid();

        //Recieved times
        internal DateTime m_LastSendersReportSent,
                          m_LastRtspRequestRecieved,
                          m_LastRecieversReportRecieved;

        //Sdp
        internal string m_SessionId;
        internal string m_SDPVersion;
        internal string m_CName;
        internal Media.Sdp.SessionDescription m_SessionDescription;

        //Reports
        internal ReceiversReport m_LastRecieversReport;
        internal SendersReport m_LastSendersReport;
        
        //The method from the last request
        internal RtspMessage.RtspMethod m_LastMethod;

        //Location from the last request
        internal Uri m_Location;

        //rtp and rtcp ports
        internal int m_ClientRtpPort, m_ClientRtcpPort,
                     m_ServerRtpPort, m_ServerRtcpPort,
                     //Last rtsp request sequence number
                     m_LastCSeq;
        
        //Channels for sending and recieving data
        internal byte m_RtpChannel = 0,
                     m_RtcpChannel = 1;

        //Unique identifier for this stream
        internal uint m_RtpSSRC;

        //Buffer for data
        internal byte[] m_Buffer = new byte[RtspMessage.MaximumLength];

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Sockets
        internal Socket m_RtspSocket, m_RtpSocket, m_RtcpSocket;

        //Packets first and last recieved
        internal RtpPacket m_FirstRtpPacket, m_LastRtpPacket;

        //Indicates if the client sent us a goodbye
        internal bool m_RtcpGoodbyeRecieved;

        IPEndPoint m_LocalRtp, m_LocalRtcp, m_RemoteRtp, m_RemoteRtcp;

        //These need to be indiviudal for rtp and rtcp and rtsp
        internal int m_RtspSent, m_RtspRecieved,
                     m_RtpSent, /*m_RtpRecieved,//We don't recieve Rtp in a session*/
                     m_RtcpSent, m_RtcpRecieved,
                     m_RtpPacketsSent;

        //Storage
        internal List<RtpPacket> m_RtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_RtcpPackets = new List<RtcpPacket>(), m_RtcpPacketSendLog = new List<RtcpPacket>();

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;

        #endregion

        #region Properties

        public Guid Id { get { return m_Id; } }

        public int SequenceNumber { get { return m_LastCSeq; } internal set { m_LastCSeq = value; } }

        public uint SynchronizationSourceIdentifier { get { return m_RtpSSRC; } }

        public string SessionId { get { return m_SessionId; } internal set { m_SessionId = value; } }

        public Uri Location { get { return m_Location; } internal set { m_Location = value; } }

        public RtspMessage.RtspMethod LastMethod { get { return m_LastMethod; } internal set { m_LastMethod = value; } }

        public ProtocolType TransportProtcol { get { return m_TransportProtocol; } internal set { m_TransportProtocol = value; } }

        public Media.Sdp.SessionDescription SessionDescription { get { return m_SessionDescription; } internal set { m_SessionDescription = value; } }

        public int RtpBytesSent { get { return m_RtpSent; } }

        public int RtpPacketsSent { get { return m_RtpPacketsSent; } }

        public int RtcpBytesSent { get { return m_RtcpSent; } }

        public RtcpPacket[] SentRtcpPackets { get { return m_RtcpPacketSendLog.ToArray(); } }

        #endregion

        #region Constructor

        public RtspSession(RtspServer server, Socket rtspSocket)
        {
            m_Server = server;
            m_RtspSocket = rtspSocket;
        }

        ~RtspSession()
        {
            Disconnect();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets up the Ssrc and Transport Sockets and starts the worker thread.
        /// </summary>
        /// <param name="rtpPort">The clients rtpPort</param>
        /// <param name="rtcpPort">The clients rtcpPort</param>
        internal void InitializeRtp(int rtpPort, int rtcpPort)
        {
            //Must have called RtspServer.CreateSourceDescription on this session first!!!!

            //Need to handle reinit
            if (m_WorkerThread != null) return;

            //Store the client ports
            m_ClientRtpPort = rtpPort;
            m_ClientRtcpPort = rtcpPort;
            //Create a ssrc
            m_RtpSSRC = (uint)DateTime.UtcNow.Ticks;

            //Setup the transport sockets
            if (m_TransportProtocol == ProtocolType.Tcp)
            {
                //For Tcp the sockets are all the same....
                m_RtpSocket = m_RtcpSocket = m_RtspSocket;
                m_ServerRtpPort = RtspServer.FindOpenTCPPort();
                m_ServerRtcpPort = m_ServerRtpPort + 1;
            }
            else
            {
                m_ServerRtpPort = RtspServer.FindOpenUDPPort();
                m_ServerRtcpPort = m_ServerRtpPort + 1;

                //Get the remoteEndPoint
                IPEndPoint m_Remote = (IPEndPoint)m_RtspSocket.RemoteEndPoint;
                m_RemoteRtp = new IPEndPoint(m_Remote.Address, m_ClientRtpPort);
                m_RemoteRtcp = new IPEndPoint(m_Remote.Address, m_ClientRtcpPort);

                //Non interleaved over udp required two sockets
                m_RtpSocket = new Socket(m_Remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                m_RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_LocalRtp = new IPEndPoint(IPAddress.Any, m_ServerRtpPort);//GetV4IPAddress()
                m_RtpSocket.Bind(m_LocalRtp);
                m_RtpSocket.Connect(m_Remote.Address, m_ClientRtpPort);
                m_RtpSocket.Blocking = false;
                m_RtpSocket.DontFragment = true;

                m_RtcpSocket = new Socket(m_Remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                m_RtcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_LocalRtcp = new IPEndPoint(IPAddress.Any, m_ServerRtcpPort);//GetV4IPAddress()
                m_RtcpSocket.Bind(m_LocalRtcp);
                m_RtcpSocket.Connect(m_Remote.Address, m_ClientRtcpPort);
                m_RtcpSocket.Blocking = false;
                m_RtpSocket.DontFragment = true;
            }

            //Create the workers thread and start it.
            m_WorkerThread = new Thread(new ThreadStart(SendRecieve));
            m_WorkerThread.Name = "RtspSession-" + m_SessionId;
            m_WorkerThread.Start();
        }

        #region Rtcp

        /// <summary>
        /// Handles the recpetion of a single RtcpPacket
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void HandleRtcpPacket(RtcpPacket packet)
        {
            if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport)
            {
                if (m_LastRtpPacket == null) return;
                //http://www.freesoft.org/CIE/RFC/1889/19.htm
                m_LastRecieversReportRecieved = DateTime.UtcNow;
                m_LastRecieversReport = new Rtcp.ReceiversReport(packet);
                SendSendersReport();
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
            }
        }

        /// <summary>
        /// Handles receving RtcpPackets from the RtcpSocket and places them into the RTPPackets storage.
        /// </summary>
        internal void RecieveRtcp()
        {
            //If there are bytes to recieved
            if (m_RtcpSocket.Available <= 0) return;
            if (m_TransportProtocol != ProtocolType.Tcp)
            {
                //For Udp we can just recieve and incrmement
                m_RtcpRecieved += m_RtcpSocket.Receive(m_Buffer);
            }
            else
            {
                //For Tcp we must recieve the frame
                int r = m_RtcpSocket.Receive(m_Buffer, 4, SocketFlags.None);

                //If we don't have the $ then
                if (m_Buffer[0] != 36)
                {
                    //This is probably a RtspMessage.. should probably invoke a method on the server
                    return;
                }

                //Get the channel
                byte channel = (byte)m_Buffer[1];

                //If the channel is not recognized
                if (channel != m_RtpChannel && channel != m_RtcpChannel)
                {
                    //This is probably a RtspMessage
                    return;
                }

                //Decode the length
                short length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Buffer, 2));

                //Recieve length bytes
                r += m_RtcpSocket.Receive(m_Buffer, Math.Min(length, m_Buffer.Length), SocketFlags.None);

                //Increment the Rtcp counter
                m_RtcpRecieved += r;
            }

            //Add all packs in the m_Buffer to the storage
            foreach (RtcpPacket p in RtcpPacket.GetPackets(m_Buffer))
            {
                m_RtcpPackets.Add(p);
            }
        }

        /// <summary>
        /// Sends a bye to the Rtcp port of the connected client
        /// </summary>
        internal void SendGoodbye()
        {
            SendRtcpPacket(new Rtcp.Goodbye((uint)m_RtpSSRC).ToPacket());
        }

        /// <summary>
        /// Sends a RtcpSenders report to the client
        /// </summary>
        internal void SendSendersReport()
        {
            m_LastSendersReport = CreateSendersReport();
            m_LastSendersReportSent = DateTime.Now;
            SendRtcpPacket(m_LastSendersReport.ToPacket());
        }

        /// <summary>
        /// Sends a RtcpPacket to the Rtcp port of the connected client
        /// Might need a SendCompund method to take an array and calculate the compound size and send for more then 1
        /// </summary>
        /// <param name="packet">The RtcpPacket to send</param>
        internal void SendRtcpPacket(RtcpPacket packet)
        {
            int sent = SendData(packet.ToBytes(), m_RtpChannel);
            if (sent > 0)
            {
                m_RtcpSent += sent;
                m_RtcpPacketSendLog.Add(packet);
            }
        }

        #endregion

        /// <summary>
        /// Called for each RtpPacket received in the source RtspStream's RtspListener
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtpPacketRecieved(RtspListener stream, RtpPacket packet)
        {
            //Rewrite the ssrc
            packet.SynchronizationSourceIdentifier = m_RtpSSRC;
            //If this is our firstPacket we will save it for reference
            if (m_FirstRtpPacket == null) m_FirstRtpPacket = packet;
            //Store this packet as our last packet
            m_LastRtpPacket = packet;
            //Add the packet to our storage
            lock (m_RtpPackets)
            {
                m_RtpPackets.Add(packet);
            }            
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtspStream's RtspListener
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(RtspListener stream, RtcpPacket packet)
        {
            //SendRtcpPacket(packet);
            
            //E.g. when Stream Location changes on the fly ... could maybe also have events for started and stopped on the listener?
            if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye) Disconnect();

            //maybe their recievers reports shoudl trigger us sending one to our client?
            //Maybe handle these but we dont need them we only need to be aware of packets from the client, not the source.
        }

        /// <summary>
        /// Sends a RtpPacket to the connected client
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        internal void SendRtpPacket(RtpPacket packet)
        {
            int sent = SendData(packet.ToBytes(), m_RtpChannel);
            if(sent > 0)
            {
                m_RtpSent += sent;
                ++m_RtpPacketsSent;
            }
        }

        /// <summary>
        /// Sends the given data on the given channel
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="channel">The channel to send on</param>
        internal int SendData(byte[] data, byte channel)
        {
            int sent = 0;
            if (data == null) return sent;
            try
            {
                //Under Udp we can send the packet verbatim
                if (m_TransportProtocol != ProtocolType.Tcp)
                {
                    //m_RtpSent += m_RtpSocket.Send(packet.ToBytes());
                    //Send the frame incrementing the bytes sent
                    sent = m_RtpSocket.SendTo(data, m_RemoteRtp);                    
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
        /// Sends the Rtcp Goodbye and disposes the Rtp and Rtcp Sockets if we are not in Tcp Transport
        /// </summary>
        internal void Disconnect()
        {
            if (m_RtcpSocket != null)
            {
                //Tell the client we are disconnecting
                if (!m_RtcpGoodbyeRecieved) SendGoodbye();

                //If we are using Udp transport we can dispose our Sockets
                if (m_TransportProtocol != ProtocolType.Tcp)
                {
                    m_RtcpSocket.Dispose();
                    m_RtcpSocket = null;
                    m_RtpSocket.Dispose();
                    m_RtcpSocket = null;
                }
                
                //Attempt to join and if not abort the worker
                if (!m_WorkerThread.Join(1000)) m_WorkerThread.Abort();
                m_WorkerThread = null;
            }
            //Empty buffers
            m_RtpPackets.Clear();
            m_RtcpPackets.Clear();
        }

        /// <summary>
        /// Entry point of the m_WorkerThread. Handles sending RtpPackets in buffer and handling any incoming RtcpPackets
        /// </summary>
        internal void SendRecieve()
        {
            try
            {
                //While the client is still listening and the RtspSocket is connected
                while (!m_RtcpGoodbyeRecieved && m_RtspSocket.Connected)
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

                    //Recieve any incoming RtcpPackets
                    RecieveRtcp();

                    #region Handle Incoming RtcpPackets

                    if (m_RtcpPackets.Count > 0)
                    {
                        lock (m_RtpPackets)
                        {
                            toSend = m_RtcpPackets.ToArray();
                            m_RtcpPackets.Clear();
                        }
                        foreach (RtcpPacket p in toSend)
                        {
                            HandleRtcpPacket(p);
                        }
                        toSend = null;
                    }

                    #endregion
                }
            }
            catch
            {
                //Thread aborted
                return;
            }
        }

        /// <summary>
        /// Creates a RtspResponse based on the SequenceNumber contained in the given RtspRequest
        /// </summary>
        /// <param name="request">The request to utilize the SequenceNumber from, if null the current SequenceNumber is used</param>
        /// <param name="statusCode">The StatusCode of the generated response</param>
        /// <returns>The RtspResponse created</returns>
        internal RtspResponse CreateRtspResponse(RtspRequest request = null, RtspResponse.ResponseStatusCode statusCode = RtspResponse.ResponseStatusCode.OK)
        {
            RtspResponse response = new RtspResponse();
            response.StatusCode = statusCode;
            response.CSeq = request != null ? request.CSeq : m_LastCSeq;
            if (!string.IsNullOrWhiteSpace(m_SessionId))
            {
                response.SetHeader(Rtsp.RtspMessage.RtspHeaders.Session, m_SessionId);
            }
            return response;
        }

        /// <summary>
        /// When a ReceiversReport is recieved on a RtspSession it will need to send back a senders report with certian values calulcated correctly for playback.
        /// </summary>
        /// <param name="session">The session from which the ReceiversReport was  recieved</param>
        /// <returns>The complete SendersReport which should be sent back to the client who sent the ReceiversReport</returns>
        internal SendersReport CreateSendersReport()
        {
            //think this is right..
            SendersReport result = new SendersReport(m_LastRecieversReport.SynchronizationSourceIdentifier);

            //Make sure these bytes are valid
            ulong[] parts = RtspServer.DateTimeToTimestamp(DateTime.Now.ToUniversalTime());
            result.NtpTimestamp = parts[0];

            result.RtpTimestamp = m_LastRtpPacket.TimeStamp;//From the last rtpPacket

            result.SendersOctetCount = (uint)m_RtpSent;
            result.SendersPacketCount = (uint)m_RtpPacketsSent;

            //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
            result.Blocks.Add(new ReportBlock((uint)m_RtpSSRC)
            {
                CumulativePacketsLost = 0,//Should be calculated
                FractionLost = 0,//Should be calculated
                InterArrivalJitter = 1000,//Should be calculated
                LastSendersReport = (uint)(m_LastSendersReport != null ? m_LastSendersReport.NtpTimestamp : 0), //Middle bits from last report?
                DelaySinceLastSendersReport = (uint)(m_LastSendersReport != null ? ((DateTime.Now - m_LastSendersReportSent).TotalSeconds * 65535) / 1000 : 0),
                ExtendedHigestSequenceNumber = (uint)(m_LastRtpPacket != null ? m_LastRtpPacket.SequenceNumber : 0)
            });

            return result;
        }

        #endregion
    }
}
