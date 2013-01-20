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
        //Needs to have it's own concept of range

        #region Fields

        internal List<RtpClient.Interleave> m_SourceInterleaves = new List<RtpClient.Interleave>();

        internal List<RtpSource> m_Attached = new List<RtpSource>();

        internal RtspServer m_Server;
        
        internal Guid m_Id = Guid.NewGuid();

        //Recieved times
        internal DateTime m_LastRtspRequestRecieved;

        //Sdp
        internal string m_SessionId; //m_SDPVersion; //Version should be returned from the SDP but when changged should send announce in certain cases?

        internal Media.Sdp.SessionDescription m_SessionDescription;

        //The method from the last request
        internal RtspRequest m_LastRequest;

        //rtp and rtcp ports
        internal int m_Receieved, m_Sent;
        
        //Buffer for data
        internal byte[] m_Buffer = new byte[RtspMessage.MaximumLength];

        //Sockets
        internal Socket m_RtspSocket;

        //Used for Rtsp over Udp
        internal UdpClient m_Udp;

        //Used for Rtsp over Http
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

        #endregion

        #region Methods

        /// <summary>
        /// Called for each RtpPacket received in the source RtpClient
        /// </summary>
        /// <param name="client">The RtpClient from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtpPacketRecieved(RtpClient client, RtpPacket packet)
        {
            //Get the interleave for the packet from the RtpClient of the Session
            RtpClient.Interleave interleave = m_RtpClient.GetInterleaveForPacket(packet);

            //Nothing we need
            if (interleave == null) return;

            //Send on its own thread (Happens quicker)
            m_RtpClient.EnquePacket(packet);

            //On the first available thread
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                //If the source has not been assigned we need to send a senders report to identify us
                if (interleave.SynchronizationSourceIdentifier == 0)
                {
                    //Send the senders report for the interleave (Which will create SynchronizationSourceIdentifier) 
                    SendSendersReport(interleave);
                }

                //Raise an event
                m_RtpClient.OnRtpPacketReceieved(packet);

                //If Enable Mixing
                //packet.ContributingSources.Add(packet.SynchronizationSourceIdentifier);

                //Identify the packet as our own
                //packet.SynchronizationSourceIdentifier = interleave.SynchronizationSourceIdentifier;

                //Send right away
                //m_RtpClient.SendData(packet.ToBytes(interleave.SynchronizationSourceIdentifier), interleave.DataChannel, interleave.RtpSocket);
            }));
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(RtpClient stream, RtcpPacket packet)
        {
            //On the first available thread
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                //E.g. when Stream Location changes on the fly ... could maybe also have events for started and stopped on the listener?
                if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
                {
                    Disconnect();
                }
                else if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport)
                {
                    //The source stream recieved a senders report                
                    //Update the RtpTimestamp and NtpTimestamp for our clients also
                    SendersReport sr = new SendersReport(packet);
                    RtpClient.Interleave interleave = m_RtpClient.GetInterleaveForPacket(packet);
                    if (interleave != null)
                    {
                        interleave.NtpTimestamp = sr.NtpTimestamp;
                        interleave.RtpTimestamp = sr.RtpTimestamp;
                    }
                }
            }));
        }

        /// <summary>
        /// Sends the Rtcp Goodbye and detaches all sources
        /// </summary>
        internal void Disconnect()
        {

            //Detach all attached streams
            m_Attached.ForEach(Detach);

            //Disconnect the RtpClient
            if (m_RtpClient != null)
            {
                m_RtpClient.Disconnect();
                m_RtpClient = null;
            }
            
            //Not really used atm
            if (m_Udp != null) m_Udp.Close();
            m_Http = null;
            
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

        internal void Attach(RtpSource stream)
        {
            if (m_Attached.Contains(stream)) return;
            //Should only be attaching for the source interleaves related to the stream
            //e.b. m_SourceInterleaves.Where(...
            stream.RtpClient.RtcpPacketReceieved += OnSourceRtcpPacketRecieved;
            stream.RtpClient.RtpPacketReceieved += OnSourceRtpPacketRecieved;
            m_Attached.Add(stream);
        }

        internal void Detach(RtpSource stream)
        {
            if (!m_Attached.Contains(stream)) return;
            if (stream.RtpClient != null)
            {
                stream.RtpClient.RtcpPacketReceieved -= OnSourceRtcpPacketRecieved;
                stream.RtpClient.RtpPacketReceieved -= OnSourceRtpPacketRecieved;
            }
            m_Attached.Remove(stream);
        }

        internal void CreateSessionDescription(SourceStream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            if (m_SessionDescription != null) return;

            string sessionId = Utility.DateTimeToNtpTimestamp(DateTime.UtcNow).ToString(), sessionVersion = Utility.DateTimeToNtpTimestamp(DateTime.UtcNow).ToString();
                                        
            string originatorString = "ASTI-RtspServer " + sessionId + " " + sessionVersion + " IN IP4 " + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI Streaming Session"; // + stream.Name 

            //Make the new SessionDescription
            m_SessionDescription = new Sdp.SessionDescription(stream.SessionDescription);

            m_SessionDescription.SessionName = sessionName;
            m_SessionDescription.OriginatorAndSessionIdentifier = originatorString;

            //As noted in [RTP3550], the use of RTP without RTCP is strongly discouraged.            
            if (stream.m_DisableSendStastics)
            {
                //However, if a sender does not wish to send RTCP packets in a media session, the sender MUST add the lines "b=RS:0" AND "b=RR:0" to the media description (from [RFC3556]).
                foreach (Sdp.MediaDescription md in m_SessionDescription.MediaDescriptions)
                {
                    md.Add(new Sdp.SessionDescriptionLine("b=RS:0"));
                    md.Add(new Sdp.SessionDescriptionLine("b=RR:0"));
                }
            }

            SessionId = sessionId;
            //m_SDPVersion = sessionVersion; //Should be retrieved from  OriginatorAndSessionIdentifier after setting?
        }

        #endregion        
    

        internal void SendSendersReports()
        {
            m_RtpClient.Interleaves.ForEach(SendSendersReport);
        }

        internal void SendSendersReport(RtpClient.Interleave interleave)
        {

            //Assign the ssrc if it has not been yet
            if (interleave.SynchronizationSourceIdentifier == 0)
            {
                // Guaranteed to be unique per session
                // Does not follow RFC Generation guidelines but is more performant and just as unique
                interleave.SynchronizationSourceIdentifier = (uint)(DateTime.UtcNow.Ticks & interleave.RtpSocket.Handle.ToInt64() ^ (interleave.DataChannel | interleave.ControlChannel));
                //Create a Senders Report
                interleave.SendersReport = m_RtpClient.CreateSendersReport(interleave, false);
            }
            else
            {
                //Create a Senders Report with blocks
                interleave.SendersReport = m_RtpClient.CreateSendersReport(interleave);
            }
            
            //Send the packet and update the time it was sent
            m_RtpClient.SendRtcpPacket(interleave.SendersReport.ToPacket());
            interleave.SendersReport.Sent = DateTime.UtcNow;            
        }
    }
}
