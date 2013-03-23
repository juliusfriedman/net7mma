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

        //Session storage
        internal System.Collections.Hashtable Storage = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());

        //Counters for authenticate and attempts

        internal List<RtpClient.TransportContext> SourceContexts = new List<RtpClient.TransportContext>();

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

        //Used for Rtsp over Udp (Todo Use Socket)
        internal UdpClient m_Udp;

        //Used for Rtsp over Http (Todo Use Socket)
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
                    //Send on its own thread
            try { m_RtpClient.EnquePacket(packet); }
            catch { }
        }

        /// <summary>
        /// Called for each RtcpPacket recevied in the source RtpClient
        /// </summary>
        /// <param name="stream">The listener from which the packet arrived</param>
        /// <param name="packet">The packet which arrived</param>
        internal void OnSourceRtcpPacketRecieved(RtpClient stream, RtcpPacket packet)
        {
            try
            {
                //E.g. when Stream Location changes on the fly etc.
                if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
                {
                    RtpClient.TransportContext trasnportContext = m_RtpClient.GetContextForPacket(packet);

                    if (trasnportContext != null)
                    {
                        m_RtpClient.SendGoodbye(trasnportContext);
                    }

                }
                else if (packet.PacketType == RtcpPacket.RtcpPacketType.SendersReport)
                {
                    //The source stream recieved a senders report                
                    //Update the RtpTimestamp and NtpTimestamp for our clients also
                    SendersReport sr = new SendersReport(packet);

                    RtpClient.TransportContext trasnportContext = m_RtpClient.GetContextForPacket(packet);

                    if (trasnportContext != null)
                    {
                        trasnportContext.NtpTimestamp = sr.NtpTimestamp;
                        trasnportContext.RtpTimestamp = sr.RtpTimestamp;
                    }
                }
            }
            catch { }
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
            //Should only be attaching for the source transportChannels related to the stream
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

        internal void CreateSessionDescription(RtpSource stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            if (m_SessionDescription != null) return;

            string sessionId = Utility.DateTimeToNptTimestamp(DateTime.UtcNow).ToString(), sessionVersion = Utility.DateTimeToNptTimestamp(DateTime.UtcNow).ToString();
                                        
            string originatorString = "ASTI-Media-Server " + sessionId + " " + sessionVersion + " IN IP4 " + ((IPEndPoint)m_RtspSocket.LocalEndPoint).Address.ToString();

            string sessionName = "ASTI Streaming Session" ; // + stream.Name 

            //Make the new SessionDescription
            m_SessionDescription = new Sdp.SessionDescription(stream.SessionDescription);

            m_SessionDescription.SessionName = sessionName;
            m_SessionDescription.OriginatorAndSessionIdentifier = originatorString;

            //As noted in [RTP3550], the use of RTP without RTCP is strongly discouraged.            
            if (stream.m_DisableQOS)
            {
                //However, if a sender does not wish to send RTCP packets in a media session, the sender MUST add the lines "b=RS:0" AND "b=RR:0" to the media description (from [RFC3556]).
                foreach (Sdp.MediaDescription md in m_SessionDescription.MediaDescriptions)
                {
                    md.Add(new Sdp.SessionDescriptionLine("b=RS:0"));
                    md.Add(new Sdp.SessionDescriptionLine("b=RR:0"));

                    Media.Sdp.SessionDescriptionLine controlLine = md.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("control"))).FirstOrDefault();

                    if (controlLine != null)
                    {
                        md.RemoveLine(md.Lines.IndexOf(controlLine));

                        string protcol = "rtsp";
                        //check for rtspu later...

                        md.Add(new Sdp.SessionDescriptionLine("a=control:" + protcol + "://" + ((IPEndPoint)(m_RtspSocket.LocalEndPoint)).Address + "/live/" + stream.Id));
                    }

                }
            }

            SessionId = sessionId;
            //m_SDPVersion = sessionVersion; //Should be retrieved from  OriginatorAndSessionIdentifier after setting?
        }

        #endregion                       
    }
}
