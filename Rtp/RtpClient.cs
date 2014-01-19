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
using System.Text;
using Media.Rtcp;
using System.Net.Sockets;
using System.Net;
using System.Threading;

//Todo, Provide a RtpConference class or integrate the capability to send and recieve to multiple parties

namespace Media.Rtp
{    
    /// <summary>
    /// Provides an implementation of the <see cref="http://tools.ietf.org/html/rfc3550"> Real Time Protocol </see>.
    /// A RtpClient typically allows one <see cref="System.Net.Socket"/> to communicate (via RTP) to another <see cref="System.Net.Socket"/> via <see cref="RtpClient.TransportContext"/>'s in which some <see cref="SessionDescription"/> has been created.
    /// </summary>
    public class RtpClient : IDisposable
    {
        #region Statics

        //Udp Hole Punch
        //Might want a seperate method for this... (WakeupRemote)
        //Most routers / firewalls will let traffic back through if the person from behind initiated the traffic.
        //Send some bytes to ensure the reciever is awake and ready... (SIP or RELOAD may have something specific and better)
        //e.g Port mapping request http://tools.ietf.org/html/rfc6284#section-4.2 
        static byte[] WakeUpBytes = new byte[] { 0x70, 0x70, 0x70, 0x70 };

        internal static byte BigEndianFrameControl = 36, // ASCII => $,  Hex => 24  Binary => 100100
        LittleEndianFrameControl = 9;                   //                                   001001

        static uint RTP_SEQ_MOD = (1 << 16);

        //Should be instance properties on the TransportContext with better names
        const int MAX_DROPOUT = 3000;
        const int MAX_MISORDER = 100;
        const int MIN_SEQUENTIAL = 2;

        const int TCP_OVERHEAD = 4; //MAGIC , CHANNEL, {LENGTH}

        public static TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(96);


        /// <summary>
        /// Provides a RtpClient which is configured to send and receive ont he given multicast address.
        /// </summary>
        /// <param name="groupAddress"></param>
        /// <param name="ttl"></param>
        /// <param name="sharedMemory"></param>
        /// <param name="inactivityTimeout"></param>
        /// <param name="rtcpEnabled"></param>
        /// <returns></returns>
        public static RtpClient Multicast(IPAddress groupAddress, int ttl, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool rtcpEnabled = true, int ssrcRemote = 0, int minSeqPackets = 0)
        {
            throw new NotImplementedException();

            /////ssrcRemote should equal null

            //RtpClient result = new RtpClient(groupAddress, sharedMemory, inactivityTimeout);
            //RtpClient.TransportContext multicastContext = new RtpClient.TransportContext(0, 1, Utility.Random32(7), rtcpEnabled, ssrcRemote, minSeqPackets);

            ////multicastContext.InitializeSockets();
            //Set ttl or provide a overload

            //result.AddTransportContext(multicastContext);
            //multicastContext = null;
            //return result;
        }

        /// <summary>
        /// Creates a RtpClient which is configured to Receive on the given Tcp socket.
        /// </summary>
        /// <param name="existing">The exsiting Tcp socket to use</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Interleaved(Socket existing, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool legacyFraming = false)
        {
            //Should verify socket type is TCP and use new socket if required ?? new Socket...
            return new RtpClient(existing, sharedMemory, inactivityTimeout ?? TimeSpan.FromMilliseconds(existing.ReceiveTimeout), legacyFraming);
        }

        /// <summary>
        /// Creates a RtpClient which is configured to send to the given remote address.
        /// Built-in events for received RtpPackets will not be enforced and FrameChanged will not be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Sender(IPAddress remoteAddress, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null)
        {
            return new RtpClient(remoteAddress, sharedMemory)
            {
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout
            };
        }

        /// <summary>
        /// Creates a RtpClient which is configured to Receive from the given remote address
        /// Built-in events for received RtpPackets will be enfored and FrameChanged will be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Participant(IPAddress remoteAddress, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null)
        {
            return new RtpClient(remoteAddress)
            {
                IncomingPacketEventsEnabled = true,
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout
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
        /// </summary>
        public class TransportContext
        {

            #region Fields

            public readonly int Version = 2;

            //The id of the channel 0 - 255
            public readonly byte DataChannel, ControlChannel;

            //Any frames for this channel
            internal RtpFrame CurrentFrame, LastFrame;

            /// <summary>
            /// The sockets used for Transport of Rtp / Rtcp and Interleaved data
            /// </summary>
            internal Socket RtpSocket, RtcpSocket;

            /// <summary>
            /// Indicates if Rtcp will be used on this TransportContext
            /// </summary>
            /// <remarks>
            /// Once Rtcp is enabled it cannot be disabled.
            /// </remarks>
            public readonly bool RtcpEnabled = true;

            //Ports we are using / will use
            internal int ServerRtpPort, ServerRtcpPort, ClientRtpPort, ClientRtcpPort;

            //The EndPoints connected to (once connected don't need the Ports)
            internal IPEndPoint LocalRtp, LocalRtcp, RemoteRtp, RemoteRtcp;

            /// <summary>
            /// SequenceNumber of the channel, starts at 0, wraps to 1 when set through the property.
            /// </summary>
            ushort m_SequenceNumber;

            public readonly int MinimumSequentialValidRtpPackets = MIN_SEQUENTIAL;

            //bytes and packet counters
            internal long RtpBytesSent, RtpBytesRecieved,
                         RtcpBytesSent, RtcpBytesRecieved,
                         RtpPacketsSent, RtcpPacketsSent,
                         RtpPacketsReceived, RtcpPacketsReceieved;

            internal ushort RtpMaxSeq;//The highest Sequence recieved by the RtpClient

            //Used for Rtp and Rtcp Transport Calculations (Should be moved into State Structure)
            internal uint RtpTransit,
                //Count of bytes recieved prior to the reception of a report
                RtpReceivedPrior,
                //Count of bytes expected prior to the recpetion of a report
                RtpExpectedPrior,
                //The amount of times the Seq number has cycled
                RtpSeqCycles,
                //The amount of base RTP Sequences encountered
                RtpBaseSeq,
                //Rtp Probation value
                RtpProbation,
                //The amount of bad RTP Sequences encountered
                RtpBadSeq,
                //Jitter value
                RtpJitter;

            internal TimeSpan m_LastActivity;

            #endregion

            #region Properties

            /// <summary>
            /// Corresponds to the ID used by remote systems to identify this TransportContext, a table might be necessary if you want to use a different id in different places
            /// </summary>
            public int SynchronizationSourceIdentifier { get; internal protected set; }

            /// <summary>
            /// Corresponds to the ID used to identify remote parties.            
            /// If more then one party is present e.g. in a multicast scenario this value reflects the last valid ID from either the last RtpPacket or RtcpPacket received.
            /// Use a <see cref="Conference"/> if the size of the group or its members should be limited in some capacity.
            /// </summary>
            public int RemoteSynchronizationSourceIdentifier { get; internal protected set; }

            /// <summary>
            /// MediaDescription which contains information about the type of Media on the Interleave
            /// </summary>
            public Sdp.MediaDescription MediaDescription { get; internal set; }

            /// <summary>
            /// Determines if the source has recieved at least <see cref="MinimumSequentialValidRtpPackets"/> RtpPackets
            /// </summary>
            public virtual bool IsValid { get { return RtpPacketsReceived >= MinimumSequentialValidRtpPackets; } }

            /// <summary>
            /// Indicates if the TransportContext has been Disposed.
            /// </summary>
            public bool Disposed { get; protected set; }

            public bool SentReports { get { return RtcpEnabled && SendersReport != null && SendersReport.Transferred.HasValue || ReceiversReport != null && ReceiversReport.Transferred.HasValue; } }

            public bool SentPackets { get { return TotalPacketsSent > 0; } }

            public bool ReceivedPackets { get { return TotalPacketsReceived > 0; } }

            /// <summary>
            /// The last <see cref="ReceiversReport"/> sent or received by this RtpClient.
            /// </summary>
            public ReceiversReport ReceiversReport { get; internal set; }

            /// <summary>
            /// The last <see cref="SendersReport"/> sent or received by this RtpClient.
            /// </summary>
            public SendersReport SendersReport { get; internal set; }

            /// The last <see cref="SourceDescriptionReport"/> sent or received by this RtpClient.
            public SourceDescriptionReport SourceDescription { get; internal set; }

            /// The last <see cref="SourceDescriptionReport"/> sent or received by this RtpClient.
            public GoodbyeReport Goodbye { get; internal set; }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) receieved
            /// </summary>
            public long TotalPacketsReceived { get { return RtpPacketsReceived + RtcpPacketsReceieved; } }

            /// <summary>
            /// /// The total amount of packets (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalPacketsSent { get { return RtpPacketsSent + RtcpPacketsSent; } }

            /// <summary>            
            /// The sequence number of the last RtpPacket sent or recieved on this channel, if set to 0 the value 1 will be used instead.
            /// </summary>
            /// <remarks>
            /// The value 0 is used only during the inital transmission in this implementation.
            /// </remarks>
            public int SequenceNumber { get { return (short)m_SequenceNumber; } internal set { if (value == 0) value = 1; m_SequenceNumber = (ushort)value; } }

            /// <summary>
            /// The RtpTimestamp from the last SendersReport recieved or created;
            /// </summary>
            /// TODO Back with logic to increase by frequency?
            public int RtpTimestamp { get; internal set; }

            /// <summary>
            /// The NtpTimestamp from the last SendersReport recieved or created
            /// </summary>
            public long NtpTimestamp { get; internal set; }

            #endregion

            #region Constructor / Destructor

            /// <summary>
            /// Creates a TransportContext from the given parameters
            /// </summary>
            /// <param name="dataChannel"></param>
            /// <param name="controlChannel"></param>
            /// <param name="ssrc"></param>
            /// <param name="rtcpEnabled"></param>
            /// <param name="senderSsrc"></param>
            /// <param name="minimumSequentialRtpPackets"></param>
            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2)
            {
                DataChannel = dataChannel;
                ControlChannel = controlChannel;
                //if they both are the same then this could mean duplexing
                SynchronizationSourceIdentifier = ssrc;
                RtcpEnabled = rtcpEnabled;
                
                //If 0 then all packets are answered
                RemoteSynchronizationSourceIdentifier = senderSsrc;

                //MinimumSequentialValidRtpPackets should be equal to 0 when RemoteSynchronizationSourceIdentifier is null I think, this essentially means respond to all inquiries.
                //A confrence may be able to contain this type of behavior better if required.
                MinimumSequentialValidRtpPackets = minimumSequentialRtpPackets;
            }

            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2)
                : this(dataChannel, controlChannel, ssrc, rtcpEnabled, senderSsrc, minimumSequentialRtpPackets)
            {
                MediaDescription = mediaDescription;
            }

            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, Sdp.MediaDescription mediaDescription, Socket socket, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2)
                : this(dataChannel, controlChannel, ssrc, mediaDescription, rtcpEnabled, senderSsrc, minimumSequentialRtpPackets)
            {
                RtpSocket = RtcpSocket = socket;
            }

            ~TransportContext()
            {
                Dispose();
            }

            #endregion
            
            #region Methods

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            public void UpdateJitter(RtpPacket packet) { UpdateJitter(packet.Timestamp); }
            
            /// <summary>
            /// Updates the RtpJitter field with respect to the given timestamp.
            /// </summary>
            /// <param name="packetTimestamp">The timestamp</param>
            public void UpdateJitter(int packetTimestamp)
            {
                // RFC 3550 A.8.
                ulong newNtp = Utility.DateTimeToNptTimestamp(DateTime.UtcNow), transit = newNtp - (ulong)NtpTimestamp;
                NtpTimestamp = (long)newNtp;
                int d = (int)(transit - RtpTransit);
                RtpTransit = (uint)transit;
                if (d < 0) d = -d;
                RtpJitter = (uint)((1d / 16d) * ((double)d - RtpJitter));
            }

            /// <summary>
            /// Resets the variables used in packets validation based on the given parameter.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number to reset to.</param>
            public void ResetRtpValidationCounters(int sequenceNumber)
            {
                RtpBaseSeq = RtpMaxSeq = (ushort)sequenceNumber;
                RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
                RtpSeqCycles = RtpReceivedPrior = (uint)(RtpPacketsReceived = 0);
            }

            /// <summary>
            /// Performs checks on the packet which can be overriden in a derrived implementation
            /// </summary>
            /// <param name="packet"></param>
            /// <returns></returns>
            public virtual bool ValidatePacketAndUpdateSequenceNumber(RtpPacket packet)
            {

                /*NOTE : 
                 * http://www.ietf.org/rfc/rfc3551.txt
                 * 
                  Static payload type 13 was assigned to the Comfort Noise (CN) payload format defined in RFC 3389.  
                  Payload type 19 was marked reserved because it had been temporarily allocated to an earlier version of Comfort Noise
                  present in some draft revisions of this document.
                 */

                int payloadLength = packet.Payload.Count;

                //If there is no Payload return, this prevents injection by utilizing just a RtpHeader which happens to be valid.
                //I can think of no good reason to allow this in this implementation, if required dervive and ensure that RTCP is not better suited for whatever is being done.
                //The underlying goto CheckSequenceNumber is what is used to performed this check currently.
                if (payloadLength == 0 && packet.PayloadType != 13) return false;
                else if (packet.PayloadType == 13 /* || packet.PayloadType == 19*/) goto CheckSequenceNumber;

                // RFC 3550 A.1. Notes: Each TransportContext instance may be better suited to have a structure which defines this logic.

                //o  RTP version field must equal 2. (Derived Implementations must override this function)

                if (packet.Version != 2) return false;

                //o  The payload type must be known, and in particular it must not be equal to SR or RR.

                if (packet.PayloadType == Rtcp.SendersReport.PayloadType || packet.PayloadType == Rtcp.ReceiversReport.PayloadType) return false;

                //o  If the P bit is set, 
                if (packet.Padding)
                {
                    //Obtain the last byte in the packet because it is used 
                    //then the last octet of the packet must contain a valid octet count
                    byte supposedPadding = packet.Payload.Array[packet.Payload.Offset + payloadLength - 1];

                    //In particular, less than the total packet length minus the header size.
                    if (supposedPadding > payloadLength) return false;
                }

                ///  o  The length of the packet must be consistent with CC and payload type (if payloads have a known length this is checked with the IsComplete property).
                if (packet.ContributingSourceCount > 0 && payloadLength < packet.ContributingSourceCount * 4) return false;

                //Only performed to ensure validity
                if (packet.Extension)
                {
                    //o  The X bit must be zero if the profile does not specify that the
                    //   header extension mechanism may be used.  
                    //   Otherwise, the extension
                    //   length field must be less than the total packet size minus the
                    //   fixed header length and padding.
                    if (packet.ExtensionOctets > payloadLength) return false;
                }

                #region Notes on RFC3550 Implementation

                /*
                  The validity check can be made stronger requiring more than two
                    packets in sequence.  The disadvantages are that a larger number of
                    initial packets will be discarded (or delayed in a queue) and that
                    high packet loss rates could prevent validation.  However, because
                    the RTCP header validation is relatively strong, if an RTCP packet is
                    received from a source before the data packets, the count could be
                    adjusted so that only two packets are required in sequence.  If
                    initial data loss for a few seconds can be tolerated, an application
                    MAY choose to discard all data packets from a source until a valid
                    RTCP packet has been received from that source.
                 * 
                 * Please Note: This is why packets are stored in the CurrentFrame of the TransportContext. (To avoid loss where possible)
                 * A property exists for disabling the handling of RtpPackets which are incoming or outgoing.
                 * 
                 * Derived implementations may want to perform additional checks noted below inter alia.
                 * 
                 Depending on the application and encoding, algorithms may exploit
                   additional knowledge about the payload format for further validation.
                   For payload types where the timestamp increment is the same for all
                   packets, 
                 * the timestamp values can be predicted from the previous                  ------ Note:
                   packet received from the same source using the sequence number           ------ The source is not valid until MIN_SEQUENTIAL have been received.
                   difference (assuming no change in payload type).                         ------ This implementation maskes no assumptions about the Timestamp property.

                   A strong "fast-path" check is possible since with high probability       ------ Note:
                   the first four octets in the header of a newly received RTP data         ------  This implementation is engineered with the state of mind that certain profiles
                   packet will be just the same as that of the previous packet from the     ------  may REQUIRE that Padding or Extensions only be present in RtpPacket N of a RtpFrame X
                   same SSRC except that the sequence number will have increased by one.    ------  Thus this check is NOT performed. The SequenceNumber of the TransportContext is assigned in the HandleIncomingRtpPacket function AFTER the sender is valid.
                   
                 * Similarly, a single-entry cache may be used for faster SSRC lookups      ------ Note: This implementation utilizes the single-entry cache once MIN_SEQUENTIAL have been received.
                   in applications where data is typically received from one source at a    ------ In scenarios with more then 1 participant a Conference class is used.
                   time.
                 */

                #endregion
                
            CheckSequenceNumber:                
                //Return the result of processing the verification of the sequence number according the RFC3550 A.1
                return UpdateSequenceNumber(packet.SequenceNumber);
            }
            
            /// <summary>
            /// Performs checks in accorance with RFC3550 A.1 and returns a value indicating if the given sequence number is in state.
            /// </summary>
            /// <param name="sequenceNumber">The sequenceNumber to check.</param>
            /// <returns>True if in state, otherwise false.</returns>
            public bool UpdateSequenceNumber(int sequenceNumber)
            {
                // RFC 3550 A.1.

                ushort udelta = (ushort)(sequenceNumber - RtpMaxSeq);

                /*
                * Source is not valid until MIN_SEQUENTIAL packets with
                * sequential sequence numbers have been received.
                */
                if (RtpProbation > 0)
                {
                    /* packet is in sequence */
                    if (sequenceNumber == RtpMaxSeq + 1)
                    {
                        RtpProbation--;
                        RtpMaxSeq = (ushort)sequenceNumber;
                        //If no more probation is required then reset the coutners and indicate the packet is in state
                        if (RtpProbation == 0)
                        {
                            ResetRtpValidationCounters(sequenceNumber);
                            return true;
                        }
                    }
                    //The sequence number is not as expected
                    
                    //Reset probation
                    RtpProbation = (uint)(MinimumSequentialValidRtpPackets - 1);

                    //Reset the sequence number
                    RtpMaxSeq = (ushort)sequenceNumber;

                    //The packet is not in state
                    return false;
                }
                else if (udelta < MAX_DROPOUT)
                {
                    /* in order, with permissible gap */
                    if (sequenceNumber < RtpMaxSeq)
                    {
                        /*
                        * Sequence number wrapped - count another 64K cycle.
                        */
                        RtpSeqCycles += RTP_SEQ_MOD;
                    }

                    //Set the maximum sequence number
                    RtpMaxSeq = (ushort)sequenceNumber;
                }
                else if (udelta <= RTP_SEQ_MOD - MAX_MISORDER)
                {
                    /* the sequence number made a very large jump */
                    if (sequenceNumber == RtpBadSeq)
                    {
                        /*
                         * Two sequential packets -- assume that the other side
                         * restarted without telling us so just re-sync
                         * (i.e., pretend this was the first packet).
                        */
                        ResetRtpValidationCounters(sequenceNumber);
                    }
                    else
                    {
                        //Set the bad sequence to the packets sequence + 1 masking off the bits which correspond to the bits of the sequenceNumber which may have wrapped since SequenceNumber is 16 bits.
                        RtpBadSeq = (uint)((sequenceNumber + 1) & (RTP_SEQ_MOD - 1));
                        return false;
                    }
                }
                else
                {
                    /* duplicate or reordered packet */
                    return false;
                }
                
                //Events count packet reception

                //The RtpPacket is in state
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
            /// <param name="punchHole"></param>
            /// <notes>
            /// Attention Freebox Stb Users!!!! -- Todo make an option to allow on the first receive to adjust port?
            /// Please use 0 For remoteRtpPort and remoteRtcpPort as the Freebox Stb does not use the correct Rtp or Rtcp ports indicated in the Describe request.
            /// </notes>
            public void InitializeSockets(IPAddress localIp, IPAddress remoteIp, int localRtpPort, int localRtcpPort, int remoteRtpPort = 0, int remoteRtcpPort = 0, bool punchHole = true)
            {
                //Erase previously set values on the TransportContext.
                ResetState();

                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                if (localIp.AddressFamily != remoteIp.AddressFamily) throw new RtpClientException("localIp and remoteIp AddressFamily must match.");
                else if (!punchHole) punchHole = !Utility.IsOnIntranet(remoteIp); //Only punch a hole if the remoteIp is not on the LAN by default.

                try
                {
                    //Setup the RtpSocket
                    RtpSocket = new Socket(remoteIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    RtpSocket.Bind(LocalRtp = new IPEndPoint(localIp, ClientRtpPort = localRtpPort));
                    RtpSocket.Connect(RemoteRtp = new IPEndPoint(remoteIp, ServerRtpPort = remoteRtpPort));
                    RtpSocket.Blocking = false;
                    //Allow for higher send rates at the cost of more memory
                    RtpSocket.SendBufferSize = 0; //Use local buffer dont copy
                    RtpSocket.ReceiveBufferSize = 0; //Use local buffer dont copy

                    //Tell the network stack what we send and receive has an order
                    RtpSocket.DontFragment = true;

                    #region Optional Parameters

                    //Set max ttl for slower networks
                    //RtpSocket.Ttl = 255;

                    //May help if behind a router
                    //Allow Nat Traversal
                    //RtpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

                    //Set type of service
                    //For older networks (http://en.wikipedia.org/wiki/Type_of_service)
                    //RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                    #endregion

                    if (punchHole)
                    {
                        //Send some bytes to ensure the result is open, if we get a SocketException the port is closed
                        try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtp); }
                        catch (SocketException) { } //We don't care about the response or any issues during the holePunch
                    }

                    //If Duplexing Rtp and Rtcp (on the same socket)
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
                        RtcpSocket.SendBufferSize = 0;
                        RtcpSocket.Blocking = false;
                        RtcpSocket.ReceiveBufferSize = 0;
                        
                        //Tell the network stack what we send and receive has an order
                        RtcpSocket.DontFragment = true;

                        #region Optional Parameters

                        //Set max ttl for slower networks
                        //RtcpSocket.Ttl = 255;

                        //May help if behind a router
                        //Allow Nat Traversal
                        //RtcpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

                        //Set type of service
                        //For older networks (http://en.wikipedia.org/wiki/Type_of_service)
                        //RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        #endregion

                        if (punchHole)
                        {
                            try { RtcpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                            catch (SocketException) { }//We don't care about the response or any issues during the holePunch
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
                ResetState();
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
                if (Disposed) return;

                // A Field / Property indicating if the socket is owned and should be disposed may be useful here.
                //For now the client who owns the socket has this property

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

            /// <summary>
            /// Removes references to any reports receieved and resets the validation counters to 0.
            /// </summary>
            void ResetState(bool disposing = false)
            {

                if (Disposed) return;

                if (disposing)
                {
                    if (Goodbye != null && !Goodbye.Disposed)
                    {
                        Goodbye = null;
                        Goodbye.Dispose();
                    }
                    if (ReceiversReport != null && !ReceiversReport.Disposed)
                    {
                        ReceiversReport = null;
                        ReceiversReport.Dispose();
                    }
                    if (SourceDescription != null && !SourceDescription.Disposed)
                    {
                        SourceDescription = null;
                        SourceDescription.Dispose();
                    }
                }

                if(!disposing) ResetRtpValidationCounters(0);
            }

            /// <summary>
            /// Disposes the TransportContext and all underlying resources.
            /// </summary>
            public virtual void Dispose()
            {
                if (Disposed) return;

                Disposed = true;

                CloseSockets();

                ResetState(true);
            }

            #endregion

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
        //Used in ReceiveData, Each TransportContext gets a chance to receive into the buffer, when the recieve completes the data is parsed if there is any then the next TransportContext goes.
        //Doing this in parallel doesn't really offset much because the decoder must be able to handle the data and if you back log the decoder you are just wasting cycles.
        internal byte[] m_Buffer;
        internal int m_BufferOffset, m_BufferLength;
        internal byte[] m_FrameHeader = new byte[] { BigEndianFrameControl, 0, 0, 0};

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;
        internal bool m_StopRequested;

        //Outgoing Packets, Not a Queue because you cant re-order a Queue and you can't take a range from the Queue
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Created from an existing socket we should not close.
        internal bool m_SocketOwner = true,
            m_LegacyFraming; //Indicates if RFC4571 framing semantics should be used.. e.g. more then 1 packet in a frame

        //Unless I missed something that damn HashSet is good for nothing except hashing.
        internal IList<TransportContext> TransportContexts = new List<TransportContext>();

        internal IPAddress m_RemoteAddress;

        internal readonly Guid m_Id = Guid.NewGuid();

        #endregion

        #region Events

        public delegate void InterleaveHandler(object sender, ArraySegment<byte> slice);
        public delegate void RtpPacketHandler(object sender, RtpPacket packet);
        public delegate void RtcpPacketHandler(object sender, RtcpPacket packet);
        public delegate void RtpFrameHandler(object sender, RtpFrame frame);

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
        /// Raised when a complete RtpFrame was receieved
        /// </summary>
        public event RtpFrameHandler RtpFrameComplete;

        /// <summary>
        /// Handles data which was interleaved when utilziing TCP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="memory"></param>
        internal void HandleInterleavedData(object sender, ArraySegment<byte> memory)
        {
            if(!Disposed) ParseAndCompleteData(memory);
        }

        internal void HandleIncomingRtcpPacket(object rtpClient, RtcpPacket packet)
        {
            //Determine if the packet can be handled
            if (packet == null || packet.Disposed || Disposed) return;

            TransportContext transportContext = GetContextForPacket(packet);

            //If there is no coresponding transportChannel or Rtcp is not enabled then return
            if (transportContext == null || !transportContext.RtcpEnabled) return;

            //Get the payload type of the packet
            int payloadType = packet.PayloadType, partyId = packet.SynchronizationSourceIdentifier;

            bool valid = transportContext.IsValid;

            //If the context is valid and the identifier does not match then return (unless specifically given null)
            if (valid && partyId != transportContext.RemoteSynchronizationSourceIdentifier) return;
                //Otherwise if the packet is not the initial SendersReport or ReceiversReport return.
            else if (payloadType != Rtcp.SendersReport.PayloadType && payloadType != Rtcp.ReceiversReport.PayloadType) return;

            //Make a copy of the packet now and only refer to this copy
            RtcpPacket localPacket = packet.Clone(true, true, false);

            //A valid RtcpPacket is received if not complete complete it now
            if (!localPacket.IsComplete)
            {
                localPacket.CompleteFrom(transportContext.RtcpSocket);
            }

            Interlocked.Add(ref transportContext.RtcpBytesRecieved, packet.Length);
            Interlocked.Increment(ref transportContext.RtcpPacketsReceieved);

            int localBytesSent = 0;

            //Determine the action based on the payload, an event for rtcp packet report should be created?
            switch (payloadType)
            {
                case Rtcp.SendersReport.PayloadType:
                    {
                        //If the id of the sender is the same then a collision in identifieres has occured.
                        if (partyId == transportContext.SynchronizationSourceIdentifier)
                        {
                            //Send a goodbye indicating the collision
                            localBytesSent = SendGoodbye(transportContext, Encoding.Default.GetBytes("ssrc collision"));

                            //Reassign SSRC
                            transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(7);
                        }
                        else if (!valid) //Otherwise assign the Id from the initial report
                        {
                            transportContext.RemoteSynchronizationSourceIdentifier = partyId;
                            localBytesSent = SendReceiversReport(transportContext);
                        }

                        goto default;
                    }
                case Rtcp.ReceiversReport.PayloadType:
                    {
                        //If the id of the sender is the same then a collision in identifieres has occured.
                        if (partyId == transportContext.SynchronizationSourceIdentifier)
                        {
                            //Send a goodbye indicating the collision
                            localBytesSent = SendGoodbye(transportContext, Encoding.Default.GetBytes("ssrc collision"));

                            //Reassign SSRC
                            transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(7);
                        }
                        else if (!valid) //Otherwise assign the Id from the initial report
                        {
                            transportContext.RemoteSynchronizationSourceIdentifier = partyId;
                            localBytesSent = SendSendersReport(transportContext);
                        }
                        goto default;
                    }
                case Rtcp.SourceDescriptionReport.PayloadType:
                    {
                        //Just assign the member nif required, no other action is yet required at this time
                        //using(transportContext.SourceDescription = new SourceDescriptionReport(localPacket)) if(!transportContext.SourceDescription.HasCName)///....
                        goto default;
                    }
                case Rtcp.GoodbyeReport.PayloadType:
                    {
                        //Using the goodbye interpretation of the localPacket
                        using (GoodbyeReport goodbye = new GoodbyeReport(localPacket, false))
                        {
                            //Check for ssrc collicion
                            if (goodbye.HasReasonForLeaving && string.Compare(Encoding.Default.GetString(goodbye.ReasonForLeaving.ToArray()), "ssrc", true) < 0)
                            {
                                //Send a goodbye indicating the collision
                                SendGoodbye(transportContext, Encoding.Default.GetBytes("ssrc collision"));

                                //Reset the SSRC
                                transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(7);

                                //Send new reports
                                SendReports(transportContext);
                            }
                            else //Otherwise just send a goodbye back
                            {
                                //Send a Goodbye which causes a Goodbye to be stored.
                                SendGoodbye(transportContext);
                            }
                        }
                        goto default;
                    }
                default: 
                    {
                        // If Any RtcpPacket should cause a report to be sent back if bandwidth allows.
                        //SendReports();
                        if (localPacket.ShouldDispose)
                        {
                            localPacket.Dispose();
                            localPacket = null;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// This function is the implemtnation of the following logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="frame"></param>
        /*RFC 3551                    RTP A/V Profile                    July 2003    [Page 7]
                 For applications which send either no packets or occasional comfort-
                 noise packets during silence, the first packet of a talkspurt, that
                 is, the first packet after a silence period during which packets have
                 not been transmitted contiguously, SHOULD be distinguished by setting
                 the marker bit in the RTP data header to one.  The marker bit in all
                 other packets is zero.  The beginning of a talkspurt MAY be used to
                 adjust the playout delay to reflect changing network delays.
                 Applications without silence suppression MUST set the marker bit to
                 zero.
             
        */
        internal void RtpClient_RtpFrameCompleted(object /*RtpClient*/ sender, RtpFrame frame)
        {
            TransportContext context = GetContextByPayloadType(frame.PayloadTypeByte);
            //If there is a context
            if (context == null) return;
            
            //if the reports were not sent
            if (!SendReports(context))
            {
                //Schedule
            }
        }

        /// <summary>
        /// Updates counters and fires a FrameChanged event if required.
        /// </summary>
        /// <param name="sender">The object which raised the event</param>
        /// <param name="packet">The RtpPacket to handle</param>
        internal void HandleIncomingRtpPacket(object/*RtpClient*/ sender, RtpPacket packet)
        {
            //Determine if the incoming packet should be handled
            if (!IncomingPacketEventsEnabled || Disposed || packet == null || packet.Disposed) return;

            //Get the transportChannel for the packet by the payload type
            TransportContext transportContext = GetContextForPacket(packet);

            //If the transportContext was null we cannot handle this packet
            if (transportContext == null || transportContext.Version != packet.Version) return;
       
            //Update values if in state
            //If the SSRC identifier in the packet is one that has been received before, then the packet is probably valid and checking if the sequence number is in the expected range provides further validation.
            if (!transportContext.ValidatePacketAndUpdateSequenceNumber(packet)) return;

            //If the context is only valid for a certain party then ignore the packet
            if (transportContext.RemoteSynchronizationSourceIdentifier != 0 && packet.SynchronizationSourceIdentifier != transportContext.RemoteSynchronizationSourceIdentifier) return;

            //Make a copy of the packet now and only reference this packet
            RtpPacket localPacket = packet.Clone(true, true, true, true, false);

            //if the packet is not complete then complete it
            if (!localPacket.IsComplete) 
            {
                localPacket.CompleteFrom(transportContext.RtpSocket);
            }

            //increment the counters            
            Interlocked.Increment(ref transportContext.RtpPacketsReceived);
            Interlocked.Add(ref transportContext.RtpBytesRecieved, localPacket.Length);

            //Update the Jitter of the Interleave
            transportContext.UpdateJitter(localPacket);

            //Set the sequence number of the context NOW that the sender is VALID
            transportContext.SequenceNumber = (short)localPacket.SequenceNumber;

            //If we have not allocated a currentFrame
            if (transportContext.CurrentFrame == null)
            {
                //make a frame
                transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);
            }
            else//There is already a frame allocated
            {
                //Move the current frame to the LastFrame
                transportContext.LastFrame = transportContext.CurrentFrame;

                //Make a new frame in the transportChannel's CurrentFrame
                transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);
            }

            //Add the packet to the current frame
            transportContext.CurrentFrame.Add(localPacket);

            //If the frame is complete then fire an event and make a new frame
            if (transportContext.CurrentFrame.Complete)
            {
                //Move the current frame to the LastFrame
                transportContext.LastFrame = transportContext.CurrentFrame;

                //Allow for a new frame to be allocated
                transportContext.CurrentFrame = null;

                //Only fire frame events when the the transportContext is valid
                if(transportContext.IsValid) OnRtpFrameComplete(transportContext.LastFrame);
            }
            else if (transportContext.CurrentFrame.Count > transportContext.CurrentFrame.MaxPackets)
            {
                //Backup of frames
                transportContext.CurrentFrame.RemoveAllPackets();
            }

            if (transportContext.ReceiversReport == null || transportContext.ReceiversReport.Transferred == null) SendReceiversReport(transportContext);

        }

        /// <summary>
        /// Increments the RtpBytesSent and RtpPacketsSent for the TransportChannel related to the packet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        internal void RtpClient_RtpPacketSent(object sender, RtpPacket packet)
        {
            if (packet == null || !packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            if (transportContext == null) return;

            if (transportContext.ValidatePacketAndUpdateSequenceNumber(packet))
            {
                transportContext.UpdateJitter(packet);

                //increment the counters
                Interlocked.Add(ref transportContext.RtpBytesSent, packet.Length);

                Interlocked.Increment(ref transportContext.RtpPacketsSent);
            }
        }

        internal void RtpClient_RtcpPacketSent(object sender, RtcpPacket packet)
        {

            if (packet == null || !packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            if (transportContext == null) return;

            Interlocked.Add(ref transportContext.RtcpBytesSent, packet.Length);

            Interlocked.Increment(ref transportContext.RtcpPacketsSent);
        }

        internal void OnInterleavedData(ArraySegment<byte> data)
        {
            if(!Disposed) InterleavedData(this, data);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            if (!Disposed && packet != null) RtpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {            
            if(!Disposed && packet != null) RtcpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtpPacketSent(RtpPacket packet)
        {            
            if (!Disposed && packet != null) RtpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketSent(RtcpPacket packet)
        {
            if (!Disposed && packet != null) RtcpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtpFrameHandler for the given frame if FrameEvents are enabled
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        internal void OnRtpFrameComplete(RtpFrame frame)
        {
            if (!Disposed && frame != null) RtpFrameComplete(this, frame);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the RtpClient should attempt to parse more than one packet per frame as consistent with <see href="http://tools.ietf.org/search/rfc4571">RFC4571</see>
        /// </summary>
        public bool LegacyFraming { get { return m_LegacyFraming; } set { m_LegacyFraming = value; } }

        /// <summary>
        /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the RtpClient) during reports
        /// </summary>
        public double MaximumRtcpBandwidthPercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the amount of seconds that a packet must be sent or received in before the RtpClient stops sending or receiving data.
        /// 96 Milliseconds by Default.
        /// </summary>
        public TimeSpan InactivityTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value which prevents Incoming Rtp and Rtcp packet events from being handled
        /// </summary>
        public bool IncomingPacketEventsEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating if the RtpClient has been Disposed.
        /// </summary>
        public bool Disposed { get; protected set; }

        /// <summary>
        /// Gets a value indicating if the RtpClient is not disposed and the WorkerThread is alive.
        /// </summary>
        public virtual bool Connected { get { return !Disposed && m_WorkerThread != null && m_WorkerThread.IsAlive; } }

        /// <summary>
        /// The RemoteAddress of the RtpClient
        /// </summary>
        public IPAddress RemoteAddress { get { return m_RemoteAddress; } }

        /// <summary>
        /// Gets a value which indicates if any underlying <see cref="RtpClient.TrasnportContext"/> owned by this RtpClient instance utilizes Rtcp.
        /// </summary>
        public bool RtcpEnabled { get { return TransportContexts.Any(c => c.RtcpEnabled); } }

        /// <summary>
        /// Indicates if the amount of bandwith currently utilized for Rtcp reporting has exceeded the amount of bandwidth allowed by the <see cref="MaximumRtcpBandwidthPercentage"/> property.
        /// </summary>
        public bool RtcpBandwidthExceeded
        {
            get
            {
                //If no limit is imposed OR The current percentage of utilized bandwidth is less than than the allowed.
                if (!Disposed && MaximumRtcpBandwidthPercentage == 0) return false;

                long total = TotalBytesReceieved + TotalRtcpBytesSent;

                if (total == 0) return false;

                return total / Uptime.TotalSeconds <= (TotalRtcpBytesReceieved + TotalRtcpBytesSent / Uptime.TotalSeconds) / MaximumRtcpBandwidthPercentage;
            }
        }

        #region Bandwidth and Uptime and Counters

        /// <summary>
        /// The Date and Time the RtpClient was Connected
        /// </summary>
        public DateTime Started { get; private set; }

        /// <summary>
        /// The amount of time the RtpClient has been Connected.
        /// </summary>
        public TimeSpan Uptime { get { return DateTime.UtcNow - Started; } }

        public long TotalRtpPacketsSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsSent); } }

        public long TotalRtpBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpBytesSent); } }

        public long TotalRtpBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpBytesRecieved); } }

        public long TotalRtpPacketsReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsReceived); } }

        public long TotalRtcpPacketsSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsSent); } }

        public long TotalRtcpBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesSent); } }

        public long TotalBytesReceieved { get { return Disposed ? 0 : TotalRtcpBytesReceieved + TotalRtpBytesReceieved; } }

        public long TotalBytesSent { get { return Disposed ? 0 : TotalRtcpBytesSent + TotalRtpBytesSent; } }

        public long TotalRtcpPacketsReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsReceieved); } }

        public long TotalRtcpBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesRecieved); } }

        #endregion

        #endregion

        #region Constructor / Destructor

        RtpClient()
        {
            MaximumRtcpBandwidthPercentage = 25;
        }

        /// <summary>
        /// Assigns the events necessary for operation and creates or assigns memory to use as well as inactivtyTimout.
        /// </summary>
        /// <param name="memory">The optional memory segment to use</param>
        /// <param name="inactivityTimeout">The optional timeout which defaults to 96 ms.</param>
        RtpClient(ArraySegment<byte> memory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null) : this()
        {
            if (memory == default(ArraySegment<byte>))
            {
                //Determine a good size
                
                //m_BufferLength = (RtpPacket.MaxPacketSize + RtcpHeader.RtcpHeaderLength);// 4 for RFC2326 + RFC4571 bytes ($,id,{len0,len1})
                m_BufferLength = 1504;

                m_Buffer = new byte[m_BufferLength];
                m_BufferOffset = 0;
            }
            else
            {
                m_Buffer = memory.Array;
                m_BufferOffset = memory.Offset;
                m_BufferLength = memory.Count;
            }

            InactivityTimeout = inactivityTimeout ?? DefaultTimeout;
            RtpPacketReceieved += new RtpPacketHandler(HandleIncomingRtpPacket);
            RtcpPacketReceieved += new RtcpPacketHandler(HandleIncomingRtcpPacket);
            RtpPacketSent += new RtpPacketHandler(RtpClient_RtpPacketSent);
            RtcpPacketSent += new RtcpPacketHandler(RtpClient_RtcpPacketSent);
            RtpFrameComplete += new RtpFrameHandler(RtpClient_RtpFrameCompleted);
            InterleavedData += new InterleaveHandler(HandleInterleavedData);
        }

        /// <summary>
        /// Creates a RtpClient Sender or Reciever using Udp
        /// </summary>
        /// <param name="address">The remote address</param>
        /// <param name="rtpPort">The rtp port</param>
        /// <param name="rtcpPort">The rtcp port</param>
        RtpClient(IPAddress address, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null)
            : this(sharedMemory, inactivityTimeout)
        {
            m_RemoteAddress = address;
            m_TransportProtocol = ProtocolType.Udp;

        }

        /// <summary>
        /// Creates a Interleaved RtpClient using the given existing socket (Tcp)
        /// </summary>
        /// <param name="existing">The existing Tcp Socket</param>
        /// <param name="memory"></param>
        /// <param name="inactivityTimeout"></param>
        /// <param name="legacyFraming">Indicates if RFC4571 Framing should be used.</param>
        RtpClient(Socket existing, ArraySegment<byte> memory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool legacyFraming = false)
            : this(memory, inactivityTimeout)
        {
            m_RemoteAddress = ((IPEndPoint)existing.RemoteEndPoint).Address;
            m_SocketOwner = false;
            m_LegacyFraming = legacyFraming;
            m_TransportProtocol = existing.ProtocolType;
        }

        //Removes listeners and references to objects
        ~RtpClient()
        {
            Dispose();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a the given context to the instances owned by this client. 
        /// Throws a RtpClientException if the given context conflicts in channel either data or control with that of one which is already owned by the instance.
        /// </summary>
        /// <param name="context">The context to add</param>
        public virtual void AddTransportContext(TransportContext context)
        {
            if (TransportContexts.Any(c => c.DataChannel == context.DataChannel || c.ControlChannel == context.ControlChannel)) throw new RtpClientException("Requested Channel is already in use");
            TransportContexts.Add(context);
        }

        #region Rtcp

        /// <summary>
        /// Sends a Goodbye to for all transportChannels, which will also stop the process sending or receiving after the Goodbye is sent
        /// //Needs SSRC
        /// </summary>
        public void SendGoodbyes()
        {
            foreach (RtpClient.TransportContext tc in TransportContexts)
                 SendGoodbye(tc);
        }

        internal int SendGoodbye(TransportContext context, byte[] reasonForLeaving = null)
        {
            //Make a Goodbye, indicate version in Client, allow reason for leaving 
            //Todo add other parties where null with SourceList
            GoodbyeReport goodBye = new GoodbyeReport(context.Version, (int)context.SynchronizationSourceIdentifier, reasonForLeaving);

            IEnumerable<RtcpPacket> compound = goodBye.Yield();

            if (Disposed || context.RtcpSocket.Handle.ToInt32() <= 0 || context.TotalPacketsSent + context.TotalPacketsReceived == 0) return 0;

            //If the context has to report receive operations
            if (context.RtpBytesRecieved > 0)
            {
                //Insert the last ReceiversReport as the first compound packet
                compound = Enumerable.Concat(CreateReceiversReport(context, false).Yield(), compound);
            }

            //If the context has to report send operations
            if (context.RtpBytesSent > 0)
            {
                //Insert the last SendersReport as the first compound packet
                compound = Enumerable.Concat(CreateSendersReport(context, false).Yield(), compound);
            }

            //Store the Goodbye in the context
            context.Goodbye = goodBye;

            //Send the packet
            return SendRtcpPackets(compound);
        }

        /// <summary>
        /// Creates a <see cref="SendersReport"/> from the given context.
        /// Note, If empty is false and no previous <see cref="SendersReport"/> was sent then the report will be empty anyway.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="empty">Specifies if the report should have any report blocks if possible</param>
        /// <returns>The report created</returns>
        internal SendersReport CreateSendersReport(TransportContext context, bool empty)
        {

            bool includeBlocks = !empty && context.SendersReport != null && context.SendersReport.Transferred.HasValue;

            SendersReport result = new SendersReport(context.Version, false, includeBlocks ? 1 : 0, (int)context.SynchronizationSourceIdentifier);

            //Use the values from the TransportChannel
            result.NtpTimestamp = (long)Utility.DateTimeToNptTimestamp(DateTime.UtcNow);
            result.RtpTimestamp = (int)context.RtpTimestamp;

            //Counters
            result.SendersOctetCount = (int)context.RtpBytesSent;
            result.SendersPacketCount = (int)context.RtpPacketsSent;

            //If source blocks are included include them and calculate their statistics
            if (includeBlocks)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(context.RtpSeqCycles + context.RtpMaxSeq);
                int expected = (int)(extended_max - context.RtpBaseSeq + 1);
                int lost = (int)(expected - context.RtpPacketsReceived);
                int expected_interval = (int)(expected - context.RtpExpectedPrior);
                context.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(context.RtpPacketsReceived - context.RtpReceivedPrior);
                context.RtpReceivedPrior = (uint)context.RtpPacketsReceived;
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
                DateTime lastSent = now - context.m_LastActivity;

                //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                result.Add(new Rtcp.ReportBlock((int)context.SendersReport.Header.SendersSynchronizationSourceIdentifier,
                    (byte)lost,
                    fraction,
                    (int)context.RtpJitter,
                    //The middle 32 bits out of 64 in the NTP timestamp (as explained in Section 4) received as part of the most recent RTCP sender report (SR) packet from source SSRC_n. If no SR has been received yet, the field is set to zero.
                    (int)Utility.DateTimeToNptTimestamp32(lastSent),
                    //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                    (int)((now - lastSent).TotalSeconds / 65536),
                    context.SequenceNumber));
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="ReceiversReport"/> from the given context.
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="empty">Indicates if the report should be empty</param>
        /// <returns>The report created</returns>
        internal ReceiversReport CreateReceiversReport(TransportContext context, bool empty)
        {
            ReceiversReport result = new ReceiversReport(context.Version, false, empty ? 0 : 1, (int)context.SynchronizationSourceIdentifier);

            if (!empty)
            {

                #region Delay and Fraction

                //http://www.koders.com/csharp/fidFF28DE8FE7C75389906149D7AC8C23532310F079.aspx?s=socket

                //// RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                int fraction = 0;
                uint extended_max = (uint)(context.RtpSeqCycles + context.RtpMaxSeq);
                int expected = (int)(extended_max - context.RtpBaseSeq + 1);
                int lost = (int)(expected - context.RtpPacketsReceived);
                int expected_interval = (int)(expected - context.RtpExpectedPrior);
                context.RtpExpectedPrior = (uint)expected;
                int received_interval = (int)(context.RtpPacketsReceived - context.RtpReceivedPrior);
                context.RtpReceivedPrior = (uint)context.RtpPacketsReceived;
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

                result.Add(new Rtcp.ReportBlock((int)context.RemoteSynchronizationSourceIdentifier,
                    (byte)lost,
                    fraction,
                    (int)context.RtpJitter,
                    (int)(context.SendersReport != null ? Utility.DateTimeToNptTimestamp(context.SendersReport.Created) : 0),
                    (context.SendersReport != null ? ((DateTime.UtcNow - context.SendersReport.Created).Milliseconds / 65535) * 1000 : 0),
                    context.SequenceNumber
                ));

            }
            return result;
        }

        /// <summary>
        /// Sends a RtcpSendersReport for each TranportChannel
        /// </summary>
        public void SendSendersReports()
        {
            if (!Disposed && !m_StopRequested) foreach (TransportContext tc in TransportContexts) SendSendersReport(tc);
        }

        /// <summary>
        /// Send any <see cref="SendersReport"/>'s required by the given context immediately reguardless of bandwidth state.
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        internal int SendSendersReport(TransportContext context)
        {
            if (context == null || !context.RtcpEnabled || context.Disposed) return 0;

            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            if (context.SynchronizationSourceIdentifier == 0)
            {
                //Generate the id per RFC3550
                context.SynchronizationSourceIdentifier = RFC3550.Random32(Rtcp.SendersReport.PayloadType);
            }

            //First report include no blocks (No last senders report), store the report being sent
            context.SendersReport = CreateSendersReport(context, false);

            //Only if the bandwidth is not exceeded or absolutely required
            if (context.SendersReport.BlockCount == 0) return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>().Concat((context.SourceDescription = CreateSourceDescription(context)).Yield()));
            return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>());
        }

        /// <summary>
        /// Send any <see cref="ReceiversReports"/> required by this RtpClient instance.
        /// </summary>
        public void SendReceiversReports()
        {
            if (!Disposed && !m_StopRequested) foreach (TransportContext tc in TransportContexts.AsParallel()) SendReceiversReport(tc);
        }

        /// <summary>
        /// Send any <see cref="ReceiversReports"/>'s required by the given context immediately reguardless of bandwidth state.
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        internal int SendReceiversReport(TransportContext context)
        {
            if (context == null || !context.RtcpEnabled || context.Disposed) return 0;
            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            else if (context.SynchronizationSourceIdentifier == 0)
            {
                // Must be guaranteed to be unique per session
                context.SynchronizationSourceIdentifier = RFC3550.Random32((Rtcp.ReceiversReport.PayloadType));
            }

            //create and store the receivers report sent
            context.ReceiversReport = CreateReceiversReport(context, false);

            //Only the SourceDescription is omitted if required
            if (context.ReceiversReport.BlockCount == 0 || context.SourceDescription == null) return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>().Concat((context.SourceDescription = CreateSourceDescription(context)).Yield()));
            return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>());
        }

        /// <summary>
        /// Creates a <see cref="SourceDescriptionReport"/> from the given context.
        /// If <paramref name="cName"/> is null then <see cref="SourceDescriptionItem.CName"/> will be used.
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="cName">The optional cName to use</param>
        /// <returns>The created report</returns>
        internal SourceDescriptionReport CreateSourceDescription(TransportContext context, SourceDescriptionItem cName = null)
        {
            //Todo, params context overload? overload with other Items
            return new SourceDescriptionReport(context.Version, false, 0, (int)context.SynchronizationSourceIdentifier) 
            { 
                new Rtcp.SourceDescriptionChunk((int)context.SynchronizationSourceIdentifier, cName ?? SourceDescriptionItem.CName)
            };
        }
        //string overload?

        /// <summary>
        /// Selects a TransportContext by matching the SynchronizationSourceIdentifier to the given sourceid
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns>The context which was identified or null if no context was found.</returns>
        internal TransportContext GetContextBySourceId(int sourceId)
        {
            if (Disposed) return null;
            try { return TransportContexts.FirstOrDefault(c => c.SynchronizationSourceIdentifier == sourceId || c.RemoteSynchronizationSourceIdentifier == sourceId); }
            catch { if (!Disposed) throw; }
            return null;
        }

        /// <summary>
        /// Selects a TransportContext by using the packet's Channel property
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        internal TransportContext GetContextForPacket(RtcpPacket packet)
        {
            if (Disposed) return null;
            //Determine based on reading the packet this is where a RtcpReport class would be useful to allow reading the Ssrc without knownin the details about the type of report
            try { return GetContextBySourceId(packet.SynchronizationSourceIdentifier); }
            catch { if (!Disposed) throw; }
            return null;
        }

        public void EnquePacket(RtcpPacket packet)
        {
            if (Disposed || m_StopRequested) return;
            m_OutgoingRtcpPackets.Add(packet);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public int SendRtcpPackets(IEnumerable<RtcpPacket> packets)
        {

            if (Disposed) return 0;

            TransportContext context = GetContextForPacket(packets.First());

            //If we don't have an transportChannel to send on or the transportChannel has not been identified or Rtcp is Disabled
            if (context == null || context.SynchronizationSourceIdentifier == 0 || !context.RtcpEnabled)
            {
                //Return
                return 0;
            }

            //Combine them, adding padding where necessary to the last packet
            byte[] data = packets.SelectMany(p => p.Prepare()).ToArray();
            
            int sent = SendData(data, context.ControlChannel, context.RtcpSocket);

            //If the compound bytes were completely sent then all packets have been sent
            if(sent >= data.Length)
            {
                foreach (RtcpPacket packet in packets)
                {
                    //set sent
                    packet.Transferred = DateTime.UtcNow;

                    //Raise en event
                    OnRtcpPacketSent(packet);
                }
            }

            return sent;
        }

        #endregion

        #region Rtp

        //public TransportContext GetContextById(int scrc)
        //{
        //    return TransportContexts.FirstOrDefault(c => c.SynchronizationSourceIdentifier == scrc || c.RemoteSynchronizationSourceIdentifier == scrc);
        //}

        public TransportContext GetContextForMediaDescription(Sdp.MediaDescription mediaDescription)
        {
            return TransportContexts.FirstOrDefault(c => c.MediaDescription.MediaType == mediaDescription.MediaType && c.MediaDescription.MediaFormat == mediaDescription.MediaFormat);
        }

        /// <summary>
        /// Selects a TransportContext for a RtpPacket by matching the packet's PayloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public TransportContext GetContextForPacket(RtpPacket packet) { if (packet == null) return null; return GetContextByPayloadType(packet.PayloadType); }

        /// <summary>
        /// Selects a TransportContext for a RtpPacket by matching the packet's PayloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public TransportContext GetContextForFrame(RtpFrame frame) { if (frame == null) return null; return GetContextByPayloadType(frame.PayloadTypeByte); }

        /// <summary>
        /// Selects a TransportContext by matching the given payloadType to the TransportContext's MediaDescription.MediaFormat
        /// </summary>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        public TransportContext GetContextByPayloadType(int payloadType) { return TransportContexts.FirstOrDefault(c => c.MediaDescription.MediaFormat == payloadType); }

        /// <summary>
        /// Selects a TransportContext by matching the given socket handle to the TransportContext socket's handle
        /// </summary>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        public TransportContext GetContextBySocketHandle(IntPtr socketHandle) { return TransportContexts.FirstOrDefault(c => c.RtpSocket.Handle == socketHandle || c.RtcpSocket.Handle == socketHandle); }

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
            if (Disposed || m_StopRequested) return;
            //Add a the packet to the outgoing
            m_OutgoingRtpPackets.Add(packet);
        }

        public void EnqueFrame(RtpFrame frame) { foreach (RtpPacket packet in frame) EnquePacket(packet); }

        public void SendRtpFrame(RtpFrame frame) { foreach (RtpPacket packet in frame) SendRtpPacket(packet); }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        public int SendRtpPacket(RtpPacket packet)
        {
            if (packet == null || m_StopRequested) return 0;
            
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

            if (transportContext.SendersReport == null || transportContext.SendersReport.Transferred == null) SendSendersReport(transportContext);

            int sent = 0;            

            //If the transportContext is changed to automatically update the timestamp by frequence then use transportContext.RtpTimestamp
             sent += SendData(packet.Prepare(packet.PayloadType,  (int)transportContext.SynchronizationSourceIdentifier).ToArray(), transportContext.DataChannel, transportContext.RtpSocket);

            //If we sent completely then (Tcp will be + 4 bytes)
            if (sent >= packet.Length)
            {
                packet.Transferred = DateTime.UtcNow;

                OnRtpPacketSent(packet);
            }

            return sent;
        }

        #endregion

        #region Socket

        /// <summary>
        /// Creates a worker thread and resets the stop variable
        /// </summary>
        public void Connect()
        {
            try
            {
                //If the worker thread is already active then return
                if (m_WorkerThread != null) return;

                //Create the workers thread and start it.
                m_WorkerThread = new Thread(new ThreadStart(SendRecieve));
                m_WorkerThread.TrySetApartmentState(ApartmentState.MTA);
                m_WorkerThread.Priority = ThreadPriority.AboveNormal;
                m_WorkerThread.IsBackground = true;
                m_WorkerThread.Name = "RtpClient-" + m_RemoteAddress.ToString() + m_Id;
                Started = DateTime.UtcNow;
                m_StopRequested = false;

                m_WorkerThread.Start();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Sends the Rtcp Goodbye and disposes the Rtp and Rtcp Sockets if we are not in Tcp Transport
        /// </summary>
        public void Disconnect()
        {
            if (Disposed || !Connected) return;
            
            SendGoodbyes();

            m_StopRequested = true;  

            //Dispose Interleve Sockets
            foreach (TransportContext tc in TransportContexts)
                tc.CloseSockets();       
        }

        /// <summary>
        /// Read the RFC4751 Frame header.
        /// Returns the amount of bytes in the frame.
        /// Outputs the channel of the frame in the channel variable.
        /// </summary>
        /// <param name="buffer">The data containing the RFC4751 frame</param>
        /// <param name="offset">The offset in the </param>
        /// <param name="channel">The byte which will contain the channel if the reading succeeded</param>
        /// <returns> -1 If the buffer does not contain a RFC4751 at the offset given</returns>
        internal int TryReadFrameHeader(ArraySegment<byte> buffer, out byte channel)
        {
            //Must be assigned
            channel = default(byte);

            //if (buffer.Array[buffer.Offset] == BlackMagic) System.Diagnostics.Debugger.Break();

            //If the buffer does not start with the magic byte this is not a RFC4751 frame
            if (buffer.Count < 4 || buffer.Array[buffer.Offset] != BigEndianFrameControl) return -1;
            
            //Assign the channel
            channel = buffer.Array[buffer.Offset + 1];

            //Return the result of reversing the Unsigned 16 bit integer at the offset (A total of 4 byte)
            if (BitConverter.IsLittleEndian) return Common.Binary.ReverseU16(BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2));
            return BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
        }

        /// <summary>
        /// Returns the amount of bytes read to completely read the RFC2326 frame.
        /// Where a - return value indicates over-reading.
        /// </summary>
        /// <param name="received"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal int ReadRFC2326FrameHeader(int received,  out byte frameChannel, out RtpClient.TransportContext context)
        {
            //There is no relevant TransportContext assoicated yet.
            context = null;

            //The channel of the frame
            frameChannel = default(byte);

            //Create the frameData segment to preserve existing data
            ArraySegment<byte> frameSegment = new ArraySegment<byte>(m_Buffer, m_BufferOffset, received);

            //The amount of data needed for the frame
            int frameLength = TryReadFrameHeader(frameSegment, out frameChannel);

            //If an error occured while reading the frame header then propagate the data via the InterleavedData event
            if (frameLength == -1) 
            {
                OnInterleavedData(frameSegment);
                return 0;
            }

            /*
             Each $ block contains exactly one upper-layer protocol data unit, e.g., one RTP packet.   
             * 
             *
                 S->C: $\000{2 byte length}{"length" bytes data, w/RTP header}
                 S->C: $\000{2 byte length}{"length" bytes data, w/RTP header}
                 S->C: $\001{2 byte length}{"length" bytes  RTCP packet}
             * 
             * No more no less, Exactly one (When not m_LegacyFraming which is checked by ParseAndCompleteData) */

            //Determine the relevant transportcontext if any
            foreach(TransportContext tc in TransportContexts)
                if (tc.DataChannel == frameChannel || tc.ControlChannel == frameChannel) { context = tc; break; }

            //Determine how may how more bytes need to be read to complete the frame
            return frameLength;
        }

        /// <summary>
        /// Parses the data in the buffer for valid Rtcp and Rtcp packet instances.
        /// </summary>
        /// <param name="memory">The memory to parse</param>
        /// <param name="from">The socket which received the data into memory and may be used for packet completion.</param>
        internal virtual void ParseAndCompleteData(ArraySegment<byte> memory, bool parseRtcp = true, bool parseRtp = true)
        {
            if (parseRtcp == false && parseRtp == false) return;

            try
            {
                //Cache start, count and index
                int offset = memory.Offset, count = memory.Count, index = 0,
                    //Calulcate remaining
                    remaining = count - index;

                //Iterate until index approaches remaining
                while (remaining > 0 && (parseRtp || parseRtcp))
                {
                    //If rtcp should be parsed
                    if (parseRtcp || m_LegacyFraming)
                    {
                        //We didn't parse any yet.
                        parseRtcp = false;

                        //Copy valid RtcpPackets out of the buffer now, if any packet is not complete it will be completed only if required.
                        foreach (RtcpPacket rtcp in RFC3550.FromCompoundBytes(memory.Array, offset + index, remaining))
                        {
                            //Raise an event for each packet.
                            OnRtcpPacketReceieved(rtcp);

                            //Move the offset the length of the packet parsed
                            index += rtcp.Length;
                        }


                        //Calculate the amount of octets remaining in memory.
                        remaining -= index;

                        //Only 1 packet per frame when not legacy framing
                        if (parseRtcp && !m_LegacyFraming) return;
                    }

                    //If rtp is parsed
                    if (parseRtp || m_LegacyFraming) 
                    {
                        //Atleast 12 bytes are needed here for a valid RtpHeader
                        if (remaining < RtpHeader.Length) return;

                        //We didn't parse any yet.
                        parseRtcp = false;

                        //Create a packet from the data received, if the packet is not complete it will be completed only if required.
                        using (RtpPacket rtp = new RtpPacket(memory.Array.Skip(offset + index).Take(remaining).ToArray(), 0))
                        {
                            //Raise the event
                            OnRtpPacketReceieved(rtp);

                            //Move the index past the length of the packet
                            index += rtp.Length;
                        }

                        //Calculate the amount of octets remaining in the segment.
                        remaining = count - index;

                        //Only 1 packet per frame when not legacy framing
                        if (parseRtp && !m_LegacyFraming) return;
                    }
                }
            }
            catch //Any exception
            {
                //Return immediately
                return;
            }
        }
        
        /// <summary>
        /// Recieved data on a given channel
        /// </summary>
        /// <param name="socket">The socket to receive data on</param>
        /// <returns>The number of bytes recieved</returns>             
        internal protected virtual int RecieveData(Socket socket, bool expectRtp = true, bool expectRtcp = true)
        {
            //Ensure the socket can poll
            if (Disposed || m_StopRequested || socket == null || socket.Handle.ToInt64() <= 0) return 0;

            //There is no error yet
            SocketError error = SocketError.SocketError;

            //Cache the offset at the time of the call
            int offset = m_BufferOffset, 
                //Reeive data
                received = socket.Receive(m_Buffer, offset, m_BufferLength, SocketFlags.None, out error);

            //If the receive was a success
            if (received > 0)
            {
                //Under TCP use Framing to obtain the length of the packet as well as the context.
                if (socket.ProtocolType == ProtocolType.Tcp)
                {
                    //Determine which TransportContext will receive the data incoming
                    TransportContext relevent = null;

                    byte frameChannel;

                    //Read the framing using the framing indicated.
                    int frameLength = ReadRFC2326FrameHeader(received, out frameChannel, out relevent);

                    //There is no context to receive the data
                    if (relevent == null) 
                    {
                        //Make a copy of the amount of bytes to skip related to the frame
                        int toSkip = frameLength - received, canSkip = toSkip > m_BufferLength ? m_BufferLength : toSkip;

                        //While a byte remains to skip, read it into the buffer decrementing toSkip the amount of bytes read / skipped.
                        while (toSkip > 0)
                        {                            
                            int justReceived = Utility.AlignedReceive(m_Buffer, offset, canSkip, socket);
                            toSkip -= justReceived;
                            received += justReceived;
                        }

                        //Do not complete data
                        return received + toSkip;
                    }
                    else
                    {
                        //Determine to parse one or both
                        expectRtp = m_LegacyFraming || !(expectRtcp = relevent.RtcpEnabled && frameChannel == relevent.ControlChannel);
                        offset += TCP_OVERHEAD;//Framing
                        received -= TCP_OVERHEAD;
                    }
                } 
                
                //Use the data received to parse and complete any recieved packets, should take a parseState
                ParseAndCompleteData(new ArraySegment<byte>(m_Buffer, offset, received), expectRtcp, expectRtp);
            }

            //Return the amount of bytes received from this operation
            return received;
        }

        /// <summary>
        /// Sends the given data on the given channel
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="channel">The channel to send on (Udp doesn't use it)</param>
        /// <returns>The amount of bytes sent</returns>
        internal protected virtual int SendData(byte[] data, byte? channel, Socket socket)
        {
            try
            {
                SocketError error = SocketError.SocketError;
                int sent = 0, length = data.Length;

                //Check there is valid data and a socket which is able to write and that the RtpClient is not stopping
                if (Disposed || m_StopRequested || data == null || socket == null || socket.Handle.ToInt64() <= 0) return sent;

                #region RFC3550 over Tcp via RFC4751 Interleaving Only

                //Under Tcp we must frame the data for the given channel
                if (channel.HasValue && socket.ProtocolType == ProtocolType.Tcp)
                {
                    m_FrameHeader[1] = channel.Value;

                    if(BitConverter.IsLittleEndian)
                        BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)data.Length)).CopyTo(m_FrameHeader, 2);
                    else
                        BitConverter.GetBytes((ushort)data.Length).CopyTo(m_FrameHeader, 2);

                    int header = 0;

                    header = socket.Send(m_FrameHeader, 0, 4, SocketFlags.None, out error);

                    while (header < 4)
                    {
                        header += socket.Send(m_FrameHeader, header, 4 - header, SocketFlags.None, out error);
                        if (error != SocketError.Success && error != SocketError.WouldBlock) break;
                    }

                    //if (error != SocketError.Success) throw new SocketException((int)error);
                }

                #endregion

                #region RFC3550 over Udp (Socket may also be in use by a RtspClient over Udp ala RFC2326 unreliable transport)

                if (socket.ProtocolType == ProtocolType.Tcp || socket.ProtocolType == ProtocolType.Udp)
                {
                    //Send the frame keeping track of the bytes sent
                    while (sent < length)
                    {
                        sent += socket.Send(data, sent, length - sent, SocketFlags.None, out error);
                        if (error != SocketError.Success && error != SocketError.WouldBlock) break;
                    }

                    //If the send was not successful throw an error with the errorCode
                    //if (error != SocketError.Success) throw new SocketException((int)error);
                    if (channel.HasValue && socket.ProtocolType == ProtocolType.Tcp) sent += 4;

                    return sent;
                }

                #endregion

                return sent;
            }
            catch
            {
                throw;
            }
        }
        
        /// <summary>
        /// Sends any reports required for all owned TransportContexts using <see cref="SendReports"/>
        /// </summary>
        /// <returns>A value indiating if reports were immediately sent</returns>
        public virtual bool SendReports()
        {
            if (m_StopRequested) return false;

            bool sent = false;

            foreach (TransportContext tc in TransportContexts.ToArray().AsParallel())
                if (!tc.Disposed && tc.RtcpEnabled) sent = SendReports(tc);

            return sent;
        }

        /// <summary>
        /// Sends any <see cref="RtcpReport"/>'s immediately for the given <see cref="TransportContext"/> if <see cref="RtcpBandwidthExceeded"/> is false.
        /// </summary>
        /// <param name="context">The <see cref="TransportContext"/> to send a report for</param>
        /// <returns>A value indicating if reports were sent</returns>
        internal virtual bool SendReports(TransportContext context)
        {
            //If the TransportContext is not owned by the client then no meaningful reports can be sent
            if (Disposed || m_StopRequested || RtcpBandwidthExceeded || !TransportContexts.Contains(context)) return false;

            //Declare the bool on the stack
            bool reportsSent = false;

            //Start with a sequence of empty packets

            //If the context has to report send operations
            if (context.RtpBytesSent > 0)
            {
                //Indicate there are reports to send
                reportsSent = true;

                //Send them now (May be scheduled)
                SendSendersReport(context);
            }

            //If the context has to report receive operations
            if (context.RtpBytesRecieved > 0)
            {
                //Indicate there are reports to send
                reportsSent = true;

                //Send them now (May be scheduled)
                SendReceiversReport(context);
            }
            
            //Indicate if reports were to be sent
            return reportsSent;
        }

        /// <summary>
        /// Sends a RtcpGoodbye Immediately if we have not recieved a packet in the required time.
        /// </summary>
        /// <param name="lastActivity">The time the lastActivity has occured on the context (sending or recieving)</param>
        /// <param name="context">The context to check against</param>
        /// <returns>True if the connection is inactive and a Goodebye was attempted to be sent to the remote party</returns>
        internal virtual bool SendGoodbyeIfInactive(DateTime lastActivity, TransportContext context)
        {
            bool inactive = false;

            //if (!context.RtcpEnabled)
            //{
            //    //No RTCP, Handle differently?
            //}

            //Calulcate for the currently inactive time period
            context.m_LastActivity = DateTime.UtcNow - lastActivity;

            if (Disposed || m_StopRequested) return true;
            else if (context.m_LastActivity > InactivityTimeout && (context.TotalPacketsReceived > 0))
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
            Begin:
            try
            {

                DateTime lastOperation = DateTime.UtcNow;                

                //Until aborted
                while (!m_StopRequested)
                {
                    //Everything we send is IEnumerable
                    System.Collections.IEnumerable toSend;

                    #region Recieve Incoming Data

                    //Enumerate each context and receive data, if received update the lastActivity
                    foreach (TransportContext context in TransportContexts.ToArray().AsParallel())
                    {
                        //Ensure a context was given
                        if (context == null || context.Disposed || context.Goodbye != null) continue;

                        //Receive Data on the RtpSocket and RtcpSocket
                        if ((RecieveData(context.RtpSocket, true, false) + RecieveData(context.RtcpSocket, false, true)) <= 0)
                        {
                            //Data was received for the context, set the m_LastActivity property and determine if reports are required to be sent
                            if ((context.m_LastActivity = (DateTime.UtcNow - lastOperation)) >= InactivityTimeout)
                            {                            
                                //If reports were sent sample the clock, otherwise check for more inactivity based on the last activity
                                if (SendReports(context)) lastOperation = DateTime.UtcNow;
                                else if (SendGoodbyeIfInactive(lastOperation, context)) return;
                            }
                        }
                        else lastOperation = DateTime.UtcNow;//Store sample of clock
                    }

                    toSend = null;

                    #endregion

                    #region Handle Outgoing RtcpPackets

                    if (!Disposed && m_OutgoingRtcpPackets.Count > 0)
                    {
                        int remove = m_OutgoingRtpPackets.Count;

                        toSend = m_OutgoingRtcpPackets.GetRange(0, remove);

                        foreach (RtcpPacket packet in toSend)
                        {
                            //If we sent or received a goodbye
                            //If we send a goodebye
                            TransportContext context = GetContextForPacket(packet);

                            //For some reason the clientsession only has one context...
                            if (context == null) continue;

                            if (SendRtcpPackets(packet.Yield()) >= packet.Length) lastOperation = DateTime.UtcNow;
                        }

                        m_OutgoingRtcpPackets.RemoveRange(0, remove);

                        toSend = null;
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (!Disposed && m_OutgoingRtpPackets.Count > 0)
                    {
                        //Could check for timestamp more recent then packet at 0  on transporContext and discard...
                        //Send only A few at a time to share with rtcp
                        int remove = m_OutgoingRtpPackets.Count;

                        toSend = m_OutgoingRtpPackets.GetRange(0, remove);

                        int sent = 0;

                        foreach (RtpPacket packet in toSend)
                        {
                            //If we sent or received a goodbye
                            //If we send a goodebye
                            TransportContext context = GetContextForPacket(packet);

                            if (context == null) continue; //Context .Goodbye != null?
                            else if (packet.Timestamp < context.RtpTimestamp) continue;

                            ++sent;

                            //If the entire packet was sent
                            if (SendRtpPacket(packet) >= packet.Length) lastOperation = DateTime.UtcNow;
                            else if (SendReports(context)) lastOperation = DateTime.UtcNow;
                        }

                        m_OutgoingRtpPackets.RemoveRange(0, sent);

                        toSend = null;
                    }

                    #endregion

                    if (!m_StopRequested) SendReports();

                }
            }
            catch (Exception ex)
            {
                if (Disposed) return;
                if (ex is ThreadInterruptedException || ex is InvalidOperationException) goto Begin;
                else if (ex is ThreadAbortException || ex is ObjectDisposedException) return;
                else if (ex is SocketException)
                {
                    SocketException sex = ex as SocketException;

                    if (sex.SocketErrorCode == SocketError.ConnectionAborted) return;
                    else if (sex.SocketErrorCode == SocketError.ConnectionReset || sex.SocketErrorCode == SocketError.TimedOut) goto Begin;
                    else return;
                }
                else throw;
            }            
        }

        #endregion

        #endregion

        public virtual void Dispose()
        {
            if (Disposed) return;

            Disconnect();

            Disposed = true;

            GC.SuppressFinalize(this);

            //Counters go away with the transportChannels
            TransportContexts.Clear();

            //Empty buffers
            m_OutgoingRtpPackets.Clear();
            m_OutgoingRtcpPackets.Clear();

            RtpPacketReceieved -= new RtpPacketHandler(HandleIncomingRtpPacket);
            RtcpPacketReceieved -= new RtcpPacketHandler(HandleIncomingRtcpPacket);
            RtpPacketSent -= new RtpPacketHandler(RtpClient_RtpPacketSent);
            RtcpPacketSent -= new RtcpPacketHandler(RtpClient_RtcpPacketSent);
            //RtpFrameChanged -= new RtpFrameHandler(RtpClient_RtpFrameChanged);
            InterleavedData -= new InterleaveHandler(HandleInterleavedData);            

            //If the worker is running
            if (m_WorkerThread != null && m_WorkerThread.ThreadState.HasFlag(ThreadState.Running))
            {
                //Attempt to join
                if (!m_WorkerThread.Join(1000))
                {
                    try
                    {
                        //Abort
                        m_WorkerThread.Abort();
                    }
                    catch { } //Cancellation not supported
                }
                
                //Reset the state of the m_WorkerThread
                m_WorkerThread = null;       
            }
        }
    }
}
