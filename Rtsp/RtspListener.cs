using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Media.Rtcp;
using Media.Rtp;
using Media.Sdp;
using System.Threading;

namespace Media.Rtsp
{
    public class RtspListener
    {
        #region Nested Types

        public class RtspListenerException : Exception
        { 
            //Might choose to put a RtspRequest here 
            public RtspListenerException(string message) : base(message){}
            public RtspListenerException(string message, Exception inner) : base(message, inner) { }
        }

        #endregion

        #region Fields

        List<RtcpPacket> m_RtcpPackets = new List<RtcpPacket>();

        /// <summary>
        /// The location the media
        /// </summary>
        Uri m_RtpLocation;

        /// <summary>
        /// The buffer this client uses for all requests 4MB
        /// </summary>
        byte[] m_Buffer = new byte[4096];

        /// <summary>
        /// The remote IPAddress to which the Location resolves via Dns
        /// </summary>
        IPAddress m_RemoteIP;

        /// <summary>
        /// The remote RtpEndPoint (We Recieve RtpPackets from)
        /// </summary>
        EndPoint m_RemoteRTP, 
            ///The Remote RtcpEndPoint (We Recieve RtcpPackets from)
            m_RemoteRTCP, 
            //The Local RtpEndPoint (We Listen on for RtpPackets)
            m_LocalRTP,
            //The Local RtcpEndPoint (We Listen on for RtcpPackets)
            m_LocalRTCP, 
            //The RemoteRtspEndPoint (We send and recieve Rtsp Messages)
            m_RemoteRtsp;

        /// <summary>
        /// The socket used for Rtsp Communication
        /// </summary>
        Socket m_RtspSocket, 
            ///The socket used for RtpCommunication (May be an alias to RtspSocket if m_RtpProtocol == Tcp)
            m_RtpSocket,
            ///The socket used for RtcpCommunication (May be an alias to RtspSocket if m_RtpProtocol == Tcp)
            m_RtcpSocket;

        /// <summary>
        /// The network stream of the RtspSocket
        /// </summary>
        NetworkStream m_RtspStream;

        /// <summary>
        /// The protcol in which Rtp and Rtcp data will be transpored from the server
        /// </summary>
        ProtocolType m_RtpProtocol;

        /// <summary>
        /// The last method used in a request to the RtspServer
        /// </summary>
        RtspMessage.RtspMethod m_LastMethod;

        /// <summary>
        /// The session description associated with the media at Location
        /// </summary>
        SessionDescription m_SessionDescription;

        /// <summary>
        /// Need to seperate counters
        /// </summary>
        int m_Sent, m_Recieved,
            m_RtspTimeout, m_RtspPort, m_RtpPort, m_RtcpPort, m_Ssrc, m_Seq, m_CSeq, m_ServerRtpPort, m_ServerRtcpPort, m_LastRtpSequenceNumber;

        List<RtspMessage.RtspMethod> m_SupportedMethods = new List<RtspMessage.RtspMethod>();

        string m_UserAgent = "ASTI RTP Client", m_SessionId, m_TransportMode, m_Range;

        #endregion

        #region Properties

        //public List<RtpPacket> RTPPackets { get { return m_RtpPackets; } }

        public List<RtcpPacket> RTCPPackets { get { return m_RtcpPackets; } }

        /// <summary>
        /// The identifier of the source media
        /// </summary>
        public int SynchronizationSourceIdentifier { get { return m_Ssrc; } }

        /// <summary>
        /// The location to the Media on the Rtsp Server
        /// </summary>
        public Uri Location { get; internal set; }

        /// <summary>
        /// Indicates if the RtspClient is connected to the remote host
        /// </summary>
        public bool Connected { get { return m_RtspSocket.Connected; } }

        /// <summary>
        /// The network credential to utilize in RtspRequests
        /// </summary>
        public NetworkCredential Credential { get; set; }

        /// <summary>
        /// Indicates if the RtspClient has started listening for RtpData
        /// </summary>
        public bool Listening { get { return m_RtpThread != null && m_RtpThread.ThreadState == ThreadState.Running; } }

        /// <summary>
        /// The amount of bytes sent by the RtspClient
        /// </summary>
        public int BytesSent { get { return m_Sent; } }

        /// <summary>
        /// The amount of bytes recieved by the RtspClient
        /// </summary>
        public int BytesRecieved { get { return m_Recieved; } }

        /// <summary>
        /// The current SequenceNumber of the RtspClient
        /// </summary>
        public int SequencNumber { get { return m_CSeq; } }

        /// <summary>
        /// The current StreamSequencNumber "seqno=" recieved with the play request of the RtspClient
        /// </summary>
        public int StreamStartSequencNumber { get { return m_Seq; } }

        /// <summary>
        /// The current StreamSequencNumber "seqno=" recieved with the play request of the RtspClient
        /// </summary>
        public int LastStreamSequencNumber { get { return m_LastRtpSequenceNumber; } }

        /// <summary>
        /// Increments and returns the current SequenceNumber
        /// </summary>
        internal int NextSequenceNumber { get { return ++m_CSeq; } }

        /// <summary>
        /// Gett the SessionDescription provided by the server for the media at <see cref="Location"/>
        /// </summary>
        public SessionDescription SessionDescription { get { return m_SessionDescription; } }

        /// <summary>
        /// The methods supported by the server recieved in the options request of the RtspClient
        /// </summary>
        public Rtsp.RtspMessage.RtspMethod[] SupportedMethods { get { return m_SupportedMethods.ToArray(); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new RtspClient from the given uri in string form.
        /// E.g. 'rtsp://somehost/sometrack/
        /// </summary>
        /// <param name="location">The string which will be parsed to obtain the Location</param>
        public RtspListener(string location) : this(new Uri(location)) { }

        /// <summary>
        /// Creates a new RtspClient from the given uri
        /// </summary>
        /// <param name="location">The uri which will be used as the Location for the RtspClient</param>
        public RtspListener(Uri location)
        {
            Location = location;

            if (location.IsAbsoluteUri)
            {
                if (location.Scheme.StartsWith("rtsp") || location.Scheme.StartsWith("rtspu"))
                {
                    try
                    {
                        m_RemoteIP = System.Net.Dns.GetHostAddresses(location.Host)[0];
                    }
                    catch (Exception ex)
                    {
                        throw new RtspListenerException("Could not resolve host from the given location", ex);
                    }

                    m_RtspPort = 554;

                    m_RtpProtocol = ProtocolType.Udp;

                    m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    m_RemoteRtsp = new IPEndPoint(m_RemoteIP, m_RtspPort);

                }
            }

            OnRtcpPacketRecieved += new RtcpPacketHandler(RtspClient_OnRtcpPacketRecieved);
            OnRtpPacketRecieved += new RtpPacketHandler(RtspClient_OnRtpPacketRecieved);
        }

        /// <summary>
        /// Creates a RtspClient on a non standard Rtsp Port
        /// </summary>
        /// <param name="location">The location of the media</param>
        /// <param name="rtspPort">The port to the RtspServer is listening on</param>
        public RtspListener(Uri location, int rtspPort)
        {
            Location = location;

            try
            {
                m_RemoteIP = System.Net.Dns.GetHostAddresses(location.Host)[0];
            }
            catch (Exception ex)
            {
                throw new RtspListenerException("Could not resolve host from the given location", ex);
            }

            m_RtspPort = rtspPort;
            m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            OnRtcpPacketRecieved += new RtcpPacketHandler(RtspClient_OnRtcpPacketRecieved);
            OnRtpPacketRecieved += new RtpPacketHandler(RtspClient_OnRtpPacketRecieved);
        }

        ~RtspListener()
        {
           if(Listening) StopListening();
        }

        #endregion

        #region Methods

        static internal int FindOpenUDPPort(int start = 15000)
        {
            int port = start;

            foreach (IPEndPoint ep in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Where(ep => ep.Port >= port))
            {
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;
            }

            //Only odd ports
            if (port % 2 == 0) ++port;

            return port;
        }

        public void Connect()
        {
            try
            {
                m_RtspSocket.Connect(m_RemoteRtsp);
                m_RtspStream = new NetworkStream(m_RtspSocket);
                m_RtspStream.ReadTimeout = 2000;
            }
            catch(Exception ex)
            {
                throw new RtspListenerException("Could not connect to remote host", ex);
            }
        }

        public void Disconnect()
        {
            try
            {
                if (m_LastMethod != RtspMessage.RtspMethod.UNKNOWN && m_LastMethod != RtspMessage.RtspMethod.TEARDOWN)
                {
                    StopListening();
                    m_RtspStream.Dispose();                    
                }
                //m_RtspSocket.Disconnect(true);
                m_RtspSocket.Dispose();
            }
            catch
            {

            }
        }

        #endregion

        #region Rtsp        

        internal RtspResponse SendRtspRequest(RtspRequest request)
        {
            try
            {

                if (!Connected) throw new RtspListenerException("You must first Connect before sending a request");

                if (request[RtspMessage.RtspHeaders.UserAgent] == null)
                {
                    request.SetHeader(RtspMessage.RtspHeaders.UserAgent, m_UserAgent);
                }

                if (Credential != null)
                {
                    //Encoding should be a property on the Listener which defaults to utf8
                    request.SetHeader(RtspMessage.RtspHeaders.Authroization, "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Credential.UserName + ':' + Credential.Password)));
                }

                if (m_SessionId != null)
                {
                    request.SetHeader(RtspMessage.RtspHeaders.Session, m_SessionId);
                }

                request.CSeq = NextSequenceNumber;

                m_LastMethod = request.Method;

                byte[] buffer = request.ToBytes();

                lock (m_RtspStream)
                {

                    m_RtspStream.Write(buffer, 0, buffer.Length);

                    m_RtspStream.Flush();
                }
                
                m_Sent += buffer.Length;

                //m_Sent += m_RtspSocket.Send(request.ToBytes());

                //int rec = m_RtspSocket.Receive(m_Buffer);

                int rec;
            Rece:
                rec = 0;
                lock (m_RtspStream)
                {
                    rec = m_RtspStream.Read(m_Buffer, 0, m_Buffer.Length);
                }

                if (rec > 0)
                {
                    m_Recieved += rec;

                    try
                    {

                        RtspResponse result = new RtspResponse(m_Buffer);
                        return result;
                    }
                    catch (RtspMessage.RtspMessageException)
                    {
                        OnRtpRecieve(new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, rec)));
                        goto Rece;
                        //return null;
                    }
                }

                return null;
            }
            catch (RtspListenerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspListenerException("An error occured during the request", ex);
            }
        }

        public RtspResponse SendOptions()
        {
            RtspRequest options = new RtspRequest(RtspMessage.RtspMethod.OPTIONS, Location);
            RtspResponse response = SendRtspRequest(options);

            if (response == null || response.StatusCode != RtspResponse.ResponseStatusCode.OK) throw new RtspListenerException("Unable to get options");

            m_SupportedMethods.Clear();

            string publicMethods = response[RtspMessage.RtspHeaders.Public];

            if (string.IsNullOrEmpty(publicMethods)) return response;

            foreach (string method in publicMethods.Split(','))
            {
                m_SupportedMethods.Add((RtspMessage.RtspMethod)Enum.Parse(typeof(Rtsp.RtspMessage.RtspMethod), method));
            }

            return response;
        }

        public RtspResponse SendDescribe()
        {
            RtspRequest describe = new RtspRequest(RtspMessage.RtspMethod.DESCRIBE, Location);
            describe.SetHeader(RtspMessage.RtspHeaders.Accept, "application/sdp");

            RtspResponse response = SendRtspRequest(describe);

            if (response.StatusCode != RtspResponse.ResponseStatusCode.OK) throw new RtspListenerException("Unable to describe media: " + response.m_FirstLine);

            try
            {
                m_SessionDescription = new Sdp.SessionDescription(response.Body);
            }            
            catch (SessionDescription.SessionDescriptionException ex)
            {
                throw new RtspListenerException("Invalid Session Description", ex);
            }

            //The media Uri is combined with the media description of the track to control ?
            //Location = new Uri(Location.ToString() + '/' + m_SessionDescription.MediaDesciptions[0].GetAtttribute("control"));            

            return response;
        }

        public RtspResponse SendTeardown() 
        {            
            //Needs to Send RtpBye if Listening
            if (Listening)
            {
                try
                {
                    SendRtcpPacket(new Goodbye((uint)m_Ssrc).ToPacket());
                }
                catch
                {
                }
            }

            RtspResponse response = null;
            try
            {
                RtspRequest teardown = new RtspRequest(RtspMessage.RtspMethod.TEARDOWN, Location);
                response = SendRtspRequest(teardown);
                return response;
            }
            catch (RtspListener.RtspListenerException)
            {
                //During the teardown we sometimes misuse the buffer
                //In this case the packet was a RtpPacket we should have the RtspReqeust next
                System.Diagnostics.Debugger.Break();
                return response;
            }
            catch
            {
                throw;
            }
            finally
            {
                m_SessionId = null;
                m_CSeq = 0;
            }
        }

        public RtspResponse SendSetup()
        {
            try
            {
                RtspRequest setup = new RtspRequest(RtspMessage.RtspMethod.SETUP, Location);

                if (m_RtpProtocol == ProtocolType.Tcp) //m_SessionDescription.MediaDesciptions[0].MediaProtocol.Contains("TCP")
                {
                    m_RtcpPort = m_RtpPort = m_RtspPort;
                    setup.SetHeader(RtspMessage.RtspHeaders.Transport, "RTP/AVP/TCP;unicast;interleaved=0-1");
                }
                else
                {
                    m_RtpPort = FindOpenUDPPort();
                    m_RtcpPort = m_RtpPort + 1;
                    setup.SetHeader(RtspMessage.RtspHeaders.Transport, m_SessionDescription.MediaDesciptions[0].MediaProtocol + ";unicast;client_port=" + m_RtpPort + '-' + m_RtcpPort);
                }

                RtspResponse response = SendRtspRequest(setup);

                if (response.StatusCode != RtspResponse.ResponseStatusCode.OK) throw new RtspListenerException("Unable to setup media: " + response.m_FirstLine);

                string sessionHeader = response[RtspMessage.RtspHeaders.Session];

                //If there is a session header it may contain the option timeout
                if (!String.IsNullOrEmpty(sessionHeader))
                {
                    if (sessionHeader.Contains(';'))
                    {
                        string[] temp = sessionHeader.Split(';');

                        m_SessionId = temp[0];

                        //If there is a timeout we may want to setup a timer on these seconds to send a GET_PARAMETER
                        m_RtspTimeout = Convert.ToInt32(temp[1].Replace("timeout=", string.Empty));
                    }
                    else
                    {
                        m_SessionId = response[RtspMessage.RtspHeaders.Session];
                        m_RtspTimeout = 60000;
                    }

                }

                string transportHeader = response[RtspMessage.RtspHeaders.Transport];

                //We need a transportHeader
                if (String.IsNullOrEmpty(transportHeader)) throw new RtspListener.RtspListenerException("Cannot setup media, Invalid Transport Header in Rtsp Response");

                //Get the parts of information from the transportHeader
                string[] parts = transportHeader.Split(';');

                ///The transport header contains the following information 
                foreach (string part in parts)
                {
                    if (part.Equals("RTP/AVP")) m_RtpProtocol = ProtocolType.Udp;
                    else if (part.Equals("RTP/AVP/UDP")) m_RtpProtocol = ProtocolType.Udp;
                    else if (part.Equals("RTP/AVP/TCP")) m_RtpProtocol = ProtocolType.Tcp;
                    //else if (part == "unicast") {  }
                    else if (part.StartsWith("client_port="))
                    {
                        //m_ClientPort = Convert.ToInt32(part.Replace("client_port=", string.Empty));  
                        //Maybe should ensure they match what we sent to the server
                    }
                    else if (part.StartsWith("server_port="))
                    {
                        //m_ServerPort = Convert.ToInt32(part.Replace("Server_port=", string.Empty));
                        string[] ports = part.Replace("server_port=", string.Empty).Split('-');

                        //This is not in any RFC including 2326

                        if (ports.Length == 1)
                        {
                            //THIS IS INDICATING A TCP Transport
                            m_RtpProtocol = ProtocolType.Tcp;
                            m_ServerRtcpPort = m_ServerRtpPort = Convert.ToInt32(ports[0]);

                            #region Old Way Like VLC

                            //Disconnect();
                            //m_RtspSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            //Connect();

                            //We must use interleaved
                            //m_RtpProtocol = ProtocolType.Tcp;

                            //SendOptions();
                            //SendDescribe();

                            #endregion

                            //We must use interleaved
                            m_RtpProtocol = ProtocolType.Tcp;

                            m_RtpSocket = m_RtspSocket;

                            //Recurse call to ensure propper setup
                            return SendSetup();

                        }
                        else
                        {
                            m_RtpProtocol = ProtocolType.Udp;
                            //This is standard RFC Port Range (pair) for RTP AND RTCP
                            m_ServerRtpPort = Convert.ToInt32(ports[0]);
                            m_RemoteRTP = new IPEndPoint(m_RemoteIP, m_ServerRtpPort);

                            m_ServerRtcpPort = Convert.ToInt32(ports[1]);
                            m_RemoteRTCP = new IPEndPoint(m_RemoteIP, m_ServerRtcpPort);
                        }
                    }
                    else if (part.StartsWith("mode="))
                    {
                        m_TransportMode = part.Replace("mode=", string.Empty).Trim();
                    }
                    else if (part.StartsWith("ssrc="))
                    {
                        string tPart = part.Replace("ssrc=", string.Empty).Trim();
                        try
                        {
                            m_Ssrc = Convert.ToInt32(tPart);
                        }
                        catch
                        {
                            m_Ssrc = int.Parse(tPart, System.Globalization.NumberStyles.HexNumber);
                        }
                    }
                }

                return response;
            }
            catch (RtspListenerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspListenerException("Unable not setup media: ", ex);
            }
        }

        //Needs facilites for sending play for multiple tracks...

        public RtspResponse SendPlay()
        {
            try
            {
                RtspRequest play = new RtspRequest(RtspMessage.RtspMethod.PLAY, Location);

                play.SetHeader(RtspMessage.RtspHeaders.Range, "npt=" + m_Range + '-');

                RtspResponse response = SendRtspRequest(play);

                if (response.StatusCode != RtspResponse.ResponseStatusCode.OK) throw new RtspListenerException("Unable to play media: " + response.m_FirstLine);

                string rtpInfo = response[RtspMessage.RtspHeaders.RtpInfo];

                if (!string.IsNullOrEmpty(rtpInfo))
                {
                    string[] pieces = rtpInfo.Split(',');
                    foreach (string piece in pieces)
                    {
                        if (piece.StartsWith("url="))
                        {
                            //Location = new Uri(piece.Replace("url=", string.Empty));
                            m_RtpLocation = new Uri(piece.Replace("url=", string.Empty));
                        }
                        else if (piece.StartsWith("seqno="))
                        {
                            m_Seq = Convert.ToInt32(piece.Replace("seqno=", string.Empty));
                        }
                    }
                }

                string rangeString = response[RtspMessage.RtspHeaders.Range];

                if (!string.IsNullOrEmpty(rangeString))
                {
                    m_Range = rangeString.Replace("npt=", string.Empty).Replace("-", string.Empty);
                }

                return response;
            }
            catch (RtspListenerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RtspListenerException("Unable to play media", ex);
            }
        }

        #endregion

        #region Rtp

        void RtspClient_OnRtpPacketRecieved(RtspListener sender, RtpPacket packet)
        {
            if (this == sender)
            {
                if (packet.SequenceNumber > m_LastRtpSequenceNumber) m_LastRtpSequenceNumber = packet.SequenceNumber;
                //m_RtpPackets.Add(packet);
            }
        }

        void RtspClient_OnRtcpPacketRecieved(RtspListener sender, RtcpPacket packet)
        {
            if (this == sender)
            {
                //Should be handling these reports and sending back responses
                m_RtcpPackets.Add(packet);
            }
        }

        /// <summary>
        /// Event handler for a RtpPacket
        /// </summary>
        /// <param name="sender">The RtspClient who fired the event</param>
        /// <param name="packet">The RtpPacket recieved by the RtspClient</param>
        public delegate void RtpPacketHandler(RtspListener sender, RtpPacket packet);

        /// <summary>
        /// Event handler for a RtcpPacket
        /// </summary>
        /// <param name="sender">The RtspClient who fired the event</param>
        /// <param name="packet">The RtcpPacket recieved by the RtspClient</param>
        public delegate void RtcpPacketHandler(RtspListener sender, RtcpPacket packet);

        /// <summary>
        /// Fired when a RtpPacket is recieved
        /// </summary>
        public event RtpPacketHandler OnRtpPacketRecieved;
        
        /// <summary>
        /// Fired when a RtcpPacket is recieved
        /// </summary>
        public event RtcpPacketHandler OnRtcpPacketRecieved;

        /// <summary>
        /// The thread used for listenening for Rtp Data
        /// </summary>
        Thread m_RtpThread;

        internal void SendRtcpPacket(RtcpPacket packet)
        {
            if (m_RtpProtocol != ProtocolType.Tcp) m_Sent += m_RtcpSocket.SendTo(packet.ToBytes(), this.m_RemoteRTCP);
            else
            {
                m_Buffer[0] = 36;
                m_Buffer[1] = 1;
                byte[] message = packet.ToBytes();
                message.CopyTo(m_Buffer, 4);
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packet.Length)).CopyTo(m_Buffer, 2);
                //m_Sent += m_RtcpSocket.SendTo(m_Buffer, message.Length + 4, SocketFlags.None, this.m_RemoteRTCP);
                lock (m_RtspStream)
                {
                    m_RtspStream.Write(m_Buffer, 0, message.Length + 4);  
                    m_Sent += message.Length + 4; 
                }
            }
        }

        /// <summary>
        /// Fires the OnRtpPacketRecieved event
        /// </summary>
        /// <param name="packet">The packet</param>
        internal void OnRtpRecieve(RtpPacket packet) 
        {
            //RtpPacketHandler handler = OnRtpPacketRecieved;
            OnRtpPacketRecieved(this, packet);
        }

        /// <summary>
        /// Fires the OnRtcpPacketRecieved event
        /// </summary>
        /// <param name="packet">The packet</param>
        internal void OnRtcpRecieve(RtcpPacket packet)
        {
            //if (packet.PacketType == RtcpPacket.RtcpPacketType.Goodbye)
            //{
            //    StopListening();
            //}

            //RtcpPacketHandler handler = OnRtcpPacketRecieved;
            OnRtcpPacketRecieved(this, packet); //May need to have an option to send back responses on certain hosts
        }

        /// <summary>
        /// Starts listening for RtpData
        /// </summary>
        public void StartListening()
        {
            if (m_RtpThread != null) return;//Already listening
            if (m_LastMethod != RtspMessage.RtspMethod.PLAY) throw new RtspListenerException("You must send the play command before you start listening");

            if (m_RtpProtocol == ProtocolType.Tcp)
            {
                //Everything comes on a single socket just alias the others...
               m_RtcpSocket = m_RtpSocket = m_RtspSocket;
            }
            else
            {
                m_RtpSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                m_LocalRTP = new IPEndPoint(IPAddress.Any, m_RtpPort);
                //m_RtpSocket.Bind(LocalRtpEndpoint);
                m_RtpSocket.Bind(m_LocalRTP);
                m_RtpSocket.ReceiveTimeout = 1000;

                m_LocalRTCP = new IPEndPoint(IPAddress.Any, m_RtcpPort);
                m_RtcpSocket = new Socket(m_RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                //m_RtcpSocket.Bind(LocalRtcpEndpoint);
                m_RtcpSocket.Bind(m_LocalRTCP);
                m_RtcpSocket.ReceiveTimeout = 1000;
            }

            m_RtpThread = new Thread(new ThreadStart(RecieveLoop));
            m_RtpThread.Name = "RtpThread" + m_RemoteIP;
            m_RtpThread.Start();
        }

        /// <summary>
        /// Stop the listener from listening for RtpData
        /// </summary>
        public void StopListening()
        {
            if (m_RtpThread == null) return;//Should be Listening?
            try
            {
                SendTeardown();
                Disconnect();
            }
            catch(Exception ex)
            {
                //Should never happen remove 
                System.Diagnostics.Debugger.Break();
            }
            if (m_RtpThread != null)
            {
                m_RtpThread.Abort();
            }
        }

        /// <summary>
        /// The loop which is responsible for recieving data
        /// </summary>
        internal void RecieveLoop()
        {
            while (m_RtspSocket.Connected)
            {
                try
                {
                    if (m_RtpProtocol == ProtocolType.Tcp)
                    {
                        RecieveTcp();
                    }
                    else
                    {
                        RecieveUdp();
                    }
                }
                catch (SocketException sex)
                {
                    //Should never happen remove 
                    System.Diagnostics.Debugger.Break();                   
                }
                catch (Exception ex)
                {
                    //Should be thread abort or nothing
                    System.Diagnostics.Debugger.Break();                   
                    break;
                }
            }
        }

        /// <summary>
        /// Handles recieving interleaved Rtp Data
        /// </summary>
        internal void RecieveTcp()
        {
            try
            {
                //Ensure we are not end the end of the stream
                int temp = m_RtspStream.ReadByte();

                if (temp == -1) return;

                //Ensure the marker is present
                if (temp != 36) return;

                //Get the channel
                byte channel = (byte)m_RtspStream.ReadByte();

                //If the channel is not 1 this is a RtspRequest in the buffer

                //Get the length
                //byte[] lenBytes = new byte[/*2*/] { (byte)m_RtspStream.ReadByte(), (byte)m_RtspStream.ReadByte() };

                //lenBytes[0] = (byte)m_RtspStream.ReadByte();
                //lenBytes[1] = (byte)m_RtspStream.ReadByte();

                //Convert the length from network byte order to host byte order
                //short length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(lenBytes, 0));

                //byte[] raw = new byte[length];

                m_RtspStream.Read(m_Buffer, 0, 2);

                short length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Buffer, 0));

                int recievedTcp = 0;
                do
                    recievedTcp += m_RtspStream.Read(m_Buffer, recievedTcp, length); //r += m_RtspStream.Read(raw, 0, length);
                while (recievedTcp < length);

                if (channel == 0)
                {
                    //RtpPacket p = new RtpPacket(raw);
                    RtpPacket p = new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, length));
                    OnRtpRecieve(p);
                }
                else
                {
                    //RtcpPacket p = new RtcpPacket(raw);
                    foreach (RtcpPacket p in RtcpPacket.GetPackets(m_Buffer, 0))
                    {
                        OnRtcpRecieve(p);
                    }
                }

                m_Recieved += (4 + recievedTcp);
            }
            catch (SocketException)
            {
            }
            catch
            {
                throw;
            }

        }

        /// <summary>
        /// Handles recieving Rtp Data over Udp
        /// </summary>
        internal void RecieveUdp()
        {
            ///We need a variable to determine how much we recieved
            int udpRecieved;
            try
            {
                //If there is data to recieve
                if (m_RtpSocket.Available > 0)
                {
                    //Recieve the data into the buffer
                    udpRecieved = m_RtpSocket.ReceiveFrom(m_Buffer, ref m_RemoteRTP);
                    //If we recieved any bytes
                    if (udpRecieved > 0)
                    {
                        //Increment the amount of bytes recieved
                        m_Recieved += udpRecieved;

                        RtpPacket packet = new RtpPacket(new ArraySegment<byte>(m_Buffer, 0, udpRecieved));
                        OnRtpRecieve(packet);
                    }
                }
            }
            catch (SocketException)
            {
                //Should not happen unless a timeout
                //System.Diagnostics.Debugger.Break();
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                //If there is data to recieve
                if (m_RtcpSocket.Available > 0)
                {
                    //Recieve the data into the buffer
                    udpRecieved = m_RtcpSocket.ReceiveFrom(m_Buffer, ref m_RemoteRTCP);
                    //If we recieved any bytes
                    if (udpRecieved > 0)
                    {
                        //Increment the amount of bytes recieved
                        m_Recieved += udpRecieved;
                        foreach (RtcpPacket packet in Rtcp.RtcpPacket.GetPackets(m_Buffer))
                        {
                            OnRtcpRecieve(packet);
                        }
                    }
                }
            }
            catch (SocketException)
            {
            }
            catch (Exception)
            {
                throw;
            }
        }

        //internal void HandleSocketException(){}

        #endregion
    }
}
