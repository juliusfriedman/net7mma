using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Media.Rtp;
using Media.Rtcp;
using System.Threading;
using Media.Rtsp.Server.Streams;

namespace Media.Rtsp
{
    /// <summary>
    /// Sends packets from a SourceStream to a remote client
    /// </summary>
    internal class ClientSession
    {
        #region Fields

        internal List<RtpClient.Interleave> m_SourceInterleaves = new List<RtpClient.Interleave>();

        internal RtspServer m_Server;
        
        internal Guid m_Id = Guid.NewGuid();

        //Recieved times
        internal DateTime m_LastRtspRequestRecieved;

        //Sdp
        internal string m_SessionId, m_SDPVersion; //Version should be returned from the SDP but when changged should send announce in certain cases?

        internal Media.Sdp.SessionDescription m_SessionDescription;

        //The method from the last request
        internal RtspRequest m_LastRequest;

        //rtp and rtcp ports
        internal int m_Receieved, m_Sent;
        
        //Buffer for data
        internal byte[] m_Buffer = new byte[RtspMessage.MaximumLength];

        //Sockets
        internal Socket m_RtspSocket;

        internal UdpClient m_Udp;

        internal HttpListenerContext m_Http;

        internal RtspResponse m_LastResponse;

        //RtpClient
        internal RtpClient m_RtpClient;

        #endregion

        #region Properties

        public Guid Id { get { return m_Id; } }

        public string SessionId { get { return m_SessionId; } internal set { m_SessionId = value; } }

        public RtspRequest LastRequest { get { return m_LastRequest; } internal set { m_LastRequest = value; m_LastRtspRequestRecieved = DateTime.UtcNow; } }

        public Media.Sdp.SessionDescription SessionDescription { get { return m_SessionDescription; } internal set { m_SessionDescription = value; } }

        #endregion

        #region Constructor

        public ClientSession(RtspServer server, Socket rtspSocket)
        {
            m_Server = server;
            m_RtspSocket = rtspSocket;
        }

        ~ClientSession()
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
            if (m_RtpClient != null) return;
            m_RtpClient = RtpClient.Sender(((IPEndPoint)m_RtspSocket.RemoteEndPoint).Address, rtpPort, rtcpPort);
            m_RtpClient.Connect();
        }

        /// <summary>
        /// Called for each RtpPacket received in the source RtpClient
        /// </summary>
        /// <param name="client">The RtpClient from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtpPacketRecieved(RtpClient client, RtpPacket packet)
        {

            //Raise an event
            m_RtpClient.OnRtpPacketReceieved(packet);

            //Enque the recieved packet for sending
            m_RtpClient.EnquePacket(packet); 

            //Trying to experiment with VLC... If it keeps having to get hacked up like this then we will just use quicktime for testing

            //////var il = client.GetInterleaveForPacket(packet);
            //////if (il.MediaDescription.MediaType == Sdp.MediaType.audio)
            //////{
            //////    //Raise an event
            //////    m_RtpClient.OnRtpPacketReceieved(packet);

            //////    //Enque the recieved packet
            //////    m_RtpClient.EnquePacket(packet);
            //////}
            //////else
            //////{
            //////    //Send the packet right away....
            //////    m_RtpClient.SendRtpPacket(packet);

            //////    //Raise as event
            //////    m_RtpClient.OnRtpPacketReceieved(packet);
            //////}
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(RtpClient stream, RtcpPacket packet)
        {
            //E.g. when Stream Location changes on the fly ... could maybe also have events for started and stopped on the listener?
            if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
            {
                Disconnect();
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport)
            {
                //The source stream recieved a senders report                
            }
            else if (packet.PacketType == RtcpPacket.RtcpPacketType.ReceiversReport)
            {
                //The source stream recieved a recievers report                
            }
        }

        /// <summary>
        /// Sends the Rtcp Goodbye and detaches all sources
        /// </summary>
        internal void Disconnect()
        {
            if (m_RtpClient != null) m_RtpClient.Disconnect();
            if (m_Udp != null) m_Udp.Close();
            m_Http = null;
            m_Attached.ForEach(Detach);
        }

        /// <summary>
        /// Creates a RtspResponse based on the SequenceNumber contained in the given RtspRequest
        /// </summary>
        /// <param name="request">The request to utilize the SequenceNumber from, if null the current SequenceNumber is used</param>
        /// <param name="statusCode">The StatusCode of the generated response</param>
        /// <returns>The RtspResponse created</returns>
        internal RtspResponse CreateRtspResponse(RtspRequest request = null, RtspStatusCode statusCode = RtspStatusCode.OK)
        {
            RtspResponse response = new RtspResponse();
            response.StatusCode = statusCode;
            response.CSeq = request != null ? request.CSeq : m_LastRequest != null ? m_LastRequest.CSeq : 0;
            if (!string.IsNullOrWhiteSpace(m_SessionId))
            {
                response.SetHeader(RtspHeaders.Session, m_SessionId);
            }
            return response;
        }

        internal List<RtspSourceStream> m_Attached = new List<RtspSourceStream>();

        internal void Attach(RtspSourceStream stream)
        {
            if (m_Attached.Contains(stream)) return;
            stream.Client.Client.RtcpPacketReceieved += OnSourceRtcpPacketRecieved;
            stream.Client.Client.RtpPacketReceieved += OnSourceRtpPacketRecieved;
            m_Attached.Add(stream);
        }

        internal void Detach(RtspSourceStream stream)
        {
            if (!m_Attached.Contains(stream)) return;
            stream.Client.Client.RtcpPacketReceieved -= OnSourceRtcpPacketRecieved;
            stream.Client.Client.RtpPacketReceieved -= OnSourceRtpPacketRecieved;
            m_Attached.Remove(stream);
        }

        internal void CreateSessionDescription(RtspSourceStream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            if (m_SessionDescription != null) return;

            //Should be 2 NTP Timestamps
            ulong[] parts = Utility.DateTimeToNptTimestamp(DateTime.Now.ToUniversalTime());
            
            string sessionId = parts[0].ToString(), sessionVersion = parts[1].ToString();
                                        
            string originatorString = "ASTI-RtspServer " + sessionId + " " + sessionVersion + " IN IP4 " + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI Streaming Session"; // + stream.Name 

            //Make the new SessionDescription
            m_SessionDescription = new Sdp.SessionDescription(stream.SessionDescription);

            m_SessionDescription.SessionName = sessionName;
            m_SessionDescription.OriginatorAndSessionIdentifier = originatorString;

            SessionId = sessionId;
            m_SDPVersion = sessionVersion; //SHould be retrieved from  OriginatorAndSessionIdentifier after setting?
        }

        #endregion        
    
        internal void SendSendersReports()
        {
            m_RtpClient.m_Interleaves.ToList().ForEach(i =>
            {
                //Assign the ssrc if it has not been yet
                if (i.SynchronizationSourceIdentifier == 0)
                {
                    //Might need a way to only send a 24 byte report (ShortForm)
                    i.SynchronizationSourceIdentifier = (uint)(DateTime.Now.Ticks ^ i.DataChannel);// Guaranteed to be unique per session
                }

                //Create a Senders Report
                SendersReport sr = new SendersReport(i.SynchronizationSourceIdentifier);
                
                //Set the timestamp
                sr.NtpTimestamp = Utility.DateTimeToNtp64(DateTime.UtcNow);

                //Wait for the first RtpPacket if needed
                //while (i.LastRtpPacket == null) System.Threading.Thread.Yield();

                //If the LastRtpPacket is null we didn't send anything yet
                if (i.LastRtpPacket == null)
                {
                    //Middle bits
                    sr.RtpTimestamp = (uint)(sr.NtpTimestamp << 16);

                    //Set the senders octet count
                    sr.RtpTimestamp = sr.SendersPacketCount = sr.SendersOctetCount = 0;
                }
                else
                {
                    //Set the timestamp
                    sr.RtpTimestamp = i.LastRtpPacket.TimeStamp;

                    //Set the senders octet count
                    sr.SendersOctetCount = (uint)i.RtpBytesRecieved;

                    //Set the senders packet count
                    sr.SendersPacketCount = (uint)i.RtpPacketsReceieved; 
                }
                
                //Send the packet on the correct channel
                m_RtpClient.SendRtcpPacket(sr.ToPacket(i.ControlChannel));
                
                //The packet was sent now
                sr.Sent = DateTime.Now;

                //Update the senders report on the interleave
                i.SendersReport = sr;
            });
        }
    }
}
