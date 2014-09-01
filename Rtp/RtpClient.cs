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
    public class RtpClient : IDisposable, Media.Common.IThreadOwner, Media.Common.ISocketOwner
    {
        #region Statics

        //Udp Hole Punch
        //Might want a seperate method for this... (WakeupRemote)
        //Most routers / firewalls will let traffic back through if the person from behind initiated the traffic.
        //Send some bytes to ensure the reciever is awake and ready... (SIP or RELOAD may have something specific and better)
        //e.g Port mapping request http://tools.ietf.org/html/rfc6284#section-4.2 
        static byte[] WakeUpBytes = new byte[] { 0x70, 0x70, 0x70, 0x70 };

        internal static byte BigEndianFrameControl = 36, // ASCII => $,  Hex => 24  Binary => 100100
        LittleEndianFrameControl = 9;                   //                                    001001

        static uint RTP_SEQ_MOD = (1 << 16);

        //Should be instance properties on the TransportContext with better names
        const int MAX_DROPOUT = 500;
        const int MAX_MISORDER = 100;
        const int MIN_SEQUENTIAL = 2;        

        /// <summary>
        /// Describes the size (in bytes) of the 
        /// [MAGIC , CHANNEL, {LENGTH}] octets which preceed any TCP RTP / RTCP data When multiplexing data on a single TCP port over RTSP.
        /// </summary>
        internal const int TCP_OVERHEAD = 4; 
        //RTP/AVP/TCP Specifies only the Length bytes in network byte order. e.g. 2 bytes

        /// <summary>
        /// The default time assocaited with Rtcp report intervals for RtpClients.
        /// </summary>
        public static TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(96);


        public static RtpClient FromSessionDescription(Sdp.SessionDescription sessionDescription)
        {
            throw new NotImplementedException();
        }

        public static TransportContext FromMediaDescription(Sdp.MediaDescription mediaDescription)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides a RtpClient which is configured to send and receive on the given multicast address.
        /// </summary>
        /// <param name="groupAddress"></param>
        /// <param name="ttl"></param>
        /// <param name="sharedMemory"></param>
        /// <param name="inactivityTimeout"></param>
        /// <param name="rtcpEnabled"></param>
        /// <returns></returns>
        public static RtpClient Multicast(IPAddress groupAddress, int ttl, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool rtcpEnabled = true, bool incomingEvents = true)
        {
            return new RtpClient(groupAddress)
            {
                IncomingPacketEventsEnabled = incomingEvents,
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout
            };
        }

        /// <summary>
        /// Creates a RtpClient which is configured to Send and Receive on the given socket.
        /// </summary>
        /// <param name="existing">The exsiting socket to use</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Duplexed(Socket existing, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool incomingEvents = true)
        {
            //Should verify socket type is TCP and use new socket if required ?? new Socket...
            return new RtpClient(existing, sharedMemory, inactivityTimeout ?? TimeSpan.FromMilliseconds(existing.ReceiveTimeout))
            {
                IncomingPacketEventsEnabled = incomingEvents,
                FrameChangedEventsEnabled = true
            };
        }

        /// <summary>
        /// Creates a RtpClient which is configured to send to the given remote address.
        /// Built-in events for received RtpPackets will not be enforced and FrameChanged will not be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Sender(IPAddress remoteAddress, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool incomingEvents = true)
        {
            return new RtpClient(remoteAddress, sharedMemory)
            {
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout,
                FrameChangedEventsEnabled = false
            };
        }

        /// <summary>
        /// Creates a RtpClient which is configured to Receive from the given remote address
        /// Built-in events for received RtpPackets will be enfored and FrameChanged will be fired.
        /// </summary>
        /// <param name="remoteAddress">The remote address</param>
        /// <returns>A configured RtpClient</returns>
        public static RtpClient Participant(IPAddress remoteAddress, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool incomingEvents = true)
        {
            return new RtpClient(remoteAddress)
            {
                IncomingPacketEventsEnabled = incomingEvents,
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout,
                FrameChangedEventsEnabled = true
            };
        }

        public static RtpClient Participant(Sdp.SessionDescription sdp, ArraySegment<byte> sharedMemory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null, bool incomingEvents = true)
        {
            return new RtpClient(System.Net.IPAddress.Parse(sdp.Lines.OfType<Sdp.Lines.SessionConnectionLine>().FirstOrDefault().IPAddress))
            {
                IncomingPacketEventsEnabled = incomingEvents,
                InactivityTimeout = inactivityTimeout ?? DefaultTimeout,
                FrameChangedEventsEnabled = true
            };
        }

        static HashSet<RtpClient> FeedbackInstances = new HashSet<RtpClient>();

        static void SendFeedback(object sender, RtcpPacket received)
        {

            //Determine if RtcpPacket should have a ResponseType hash... could be created in each Type statically like PayloadTypeByte is defined and mapped.
            //E.g ResponseTypeByte = "225"
            //Either that or a CreateResponse method which returns a RtcpPacket constructed from the given

            var impl = RtcpPacket.GetImplementationForPayloadType((byte)received.PayloadType);

            if (impl != null)
            {
                //Create packet(s) which is a response for the received
                IEnumerable<RtcpPacket> packets = null;
                if (packets != null)
                {
                    if (sender is RtpClient)
                    {
                        RtpClient c = sender as RtpClient;
                        //What is needed
                        c.SendRtcpPackets(packets);
                    }
                }
            }
        }

        public static bool EnableFeedbackReports(RtpClient client)
        {
            if (FeedbackInstances.Add(client))
            {
                client.RtcpPacketReceieved += SendFeedback;
                return true;
            }
            return false;
        }

        public static bool DisableFeedbackReports(RtpClient client)
        {
            if (FeedbackInstances.Remove(client))
            {
                client.RtcpPacketReceieved -= SendFeedback;
                return true;
            }
            return false;
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///Contains the information and assets relevent to each stream in use by a RtpClient
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

            internal bool SendRtcpReports = true;

            //Ports we are using / will use
            internal int ServerRtpPort, ServerRtcpPort,  //Ports connected to
                ClientRtpPort, ClientRtcpPort; ///Ports connected from

            //The EndPoints connected to (once connected don't need the Ports)
            internal EndPoint LocalRtp, LocalRtcp, RemoteRtp, RemoteRtcp;

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

            //Intervals are 96 milliseconds by default.
            internal TimeSpan m_SendInterval = DefaultTimeout, m_ReceiveInterval = DefaultTimeout, m_InactiveTime = TimeSpan.Zero;

            //When packets are succesfully transferred the DateTime (utc) is copied in these variables and will reflect the point in time in which  the last 
            internal DateTime m_LastRtcpIn, m_LastRtcpOut,  //Rtcp packets were received and sent
                m_LastRtpIn, m_LastRtpOut; //Rtp packets were received and sent

            /// <summary>
            /// Indicates if Rtp is enabled on the TransportContext
            /// </summary>
            public readonly bool RtpEnabled = true;

            #endregion

            #region Properties

            public TimeSpan LastRtcpReportSent
            {
                get
                {
                    return m_LastRtcpOut == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtcpOut;
                }
            }

            public TimeSpan LastRtcpReportReceived
            {
                get
                {
                    return m_LastRtcpIn == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtcpIn;
                }
            }

            public TimeSpan LastRtpPacketReceived
            {
                get
                {
                    return m_LastRtpIn == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtpIn;
                }
            }

            public TimeSpan LastRtpPacketSent
            {
                get
                {
                    return m_LastRtpOut == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtpOut;
                }
            }

            /// <summary>
            /// Corresponds to the ID used by remote systems to identify this TransportContext, a table might be necessary if you want to use a different id in different places
            /// </summary>
            public int SynchronizationSourceIdentifier { get; internal protected set; }

            /// <summary>
            /// Corresponds to the ID used to identify remote parties.            
            /// Use a <see cref="Conference"/> if the size of the group or its members should be limited in some capacity.
            /// </summary>
            public int? RemoteSynchronizationSourceIdentifier { get; internal protected set; }

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

            /// <summary>
            /// <c>false</c> if LocalRtp.Port NOT EQ LocalRtcp.Port
            /// </summary>
            public bool LocalMultiplexing
            {
                get
                {
                    return Disposed ? false : LocalRtp.Equals(LocalRtcp);
                }
            }

            /// <summary>
            /// <c>false</c> if RemoteRtp.Port NOT EQ RemoteRtcp.Port
            /// </summary>
            public bool RemoteMultiplexing
            {
                get
                {
                    return Disposed ? false : RemoteRtp.Equals(RemoteRtcp);
                }
            }

            /// <summary>
            /// <c>false</c> if [RtpEnabled AND RtcpEnabled] AND [LocalMultiplexing OR RemoteMultiplexing]
            /// </summary>
            public bool Duplexing { get { try { return Disposed ? false : (RtpEnabled && RtcpEnabled) && (LocalMultiplexing || RemoteMultiplexing); } catch { return false; } } }

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
            public void UpdateJitterAndTimestamp(RtpPacket packet)
            {
                // RFC 3550 A.8.
                ulong newNtp = Utility.DateTimeToNptTimestamp(DateTime.UtcNow), transit = newNtp - Utility.DateTimeToNptTimestamp(packet.Transferred ?? packet.Created);
                NtpTimestamp = (long)newNtp;
                RtpTimestamp = packet.Timestamp;
                int d = (int)(transit - RtpTransit);
                RtpTransit = (uint)transit;
                if (d < 0) d = -d;
                RtpJitter = (uint)((1d / 16d) * ((double)d - RtpJitter));
            }
            
            /// <summary>
            /// Updates the RtpJitter field with respect to the given timestamp.
            /// </summary>
            /// <param name="packetTimestamp">The timestamp</param>
            //public void UpdateJitter(int packetTimestamp)
            //{
            //    // RFC 3550 A.8.
            //    ulong newNtp = Utility.DateTimeToNptTimestamp(DateTime.UtcNow - TimeSpan.FromMilliseconds(packetTimestamp)), transit = newNtp - (ulong)packetTimestamp;
            //    NtpTimestamp = (long)newNtp;
            //    int d = (int)(transit - RtpTransit);
            //    RtpTransit = (uint)transit;
            //    if (d < 0) d = -d;
            //    RtpJitter = (uint)((1d / 16d) * ((double)d - RtpJitter));
            //}

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
                //if (payloadLength == 0 && packet.PayloadType != 13) return false;
                //else if (packet.PayloadType == 13  || packet.PayloadType == 19) goto CheckSequenceNumber;

                if (packet.Header.IsCompressed || packet.PayloadType == 13) goto CheckSequenceNumber;

                // RFC 3550 A.1. Notes: Each TransportContext instance may be better suited to have a structure which defines this logic.

                //o  RTP version field must equal 2. (Derived Implementations must override this function)

                if (packet.Version != 2) return false;

                //o  The payload type must be known, and in particular it must not be equal to SR or RR.

                if (packet.PayloadType == Rtcp.SendersReport.PayloadType || packet.PayloadType == Rtcp.ReceiversReport.PayloadType) return false;

                //o  If the P bit is set, 
                if (packet.Padding && payloadLength > 0)
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
                   in applications where data is typically received from one source at a    ------ In scenarios with more then 1 participant is required a Conference class is used.
                   time.
                 */

                #endregion
                
            CheckSequenceNumber:                
                //Return the result of processing the verification of the sequence number according the RFC3550 A.1
                if (UpdateSequenceNumber(packet.SequenceNumber))
                {
                    SequenceNumber = packet.SequenceNumber;
                    return true;
                }

                return false;
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
            /// Randomizes the SequenceNumber
            /// </summary>
            public void RandomizeSequenceNumber() { SequenceNumber = Utility.Random.Next(); }

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
                    //RtpSocket.Blocking = false;
                    //RtpSocket.SendBufferSize = RtpSocket.ReceiveBufferSize = 0; //Use local buffer dont copy

                    //http://en.wikipedia.org/wiki/Type_of_service
                    //CS5,EF	40,46	5 :Critical - mainly used for voice RTP
                    //40 || 46 is used for RTP Audio per Wikipedia

                    //RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);
                    //RtpSocket.Ttl = 255;
                    //RtpSocket.UseOnlyOverlappedIO = true;
                    //RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IpTimeToLive, 255);

                    //RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);
                    ////RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.UseLoopback, true);

                    //RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.NoChecksum, true);

                    //Tell the network stack what we send and receive has an order
                    RtpSocket.DontFragment = true;
                    RtpSocket.MulticastLoopback = false;
                    RtpSocket.ReceiveTimeout = RtpSocket.SendTimeout = DefaultTimeout.Milliseconds;

                    #region Optional Parameters

                    //Set max ttl for slower networks
                    RtpSocket.Ttl = 255;

                    //May help if behind a router
                    //Allow Nat Traversal
                    //RtpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

                    //Set type of service
                    //For older networks (http://en.wikipedia.org/wiki/Type_of_service)
                    RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                    #endregion

                    //Check v4 of v6
                    if (localIp.IsIPv6Multicast || localIp.IsIPv6Multicast)
                    {
                        //JoinMulticastGroup
                    }

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
                        RtcpSocket.SendBufferSize = RtcpSocket.ReceiveBufferSize = 0;
                        //RtcpSocket.Blocking = false;

                        //RtcpSocket.Ttl = 255;
                        //RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        RtcpSocket.ReceiveTimeout = RtcpSocket.SendTimeout = DefaultTimeout.Milliseconds;

                        //Tell the network stack what we send and receive has an order
                        RtcpSocket.DontFragment = true;
                        RtcpSocket.MulticastLoopback = false;

                        //RtcpSocket.UseOnlyOverlappedIO = true;

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

                        if (localIp.IsIPv6Multicast || localIp.IsIPv6Multicast)
                        {
                            //JoinMulticastGroup
                        }

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
            /// Uses the given socket for the duplexed data
            /// </summary>
            /// <param name="duplexed">The socket to use</param>
            public void InitializeSockets(Socket duplexed)
            {

                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                
                LocalRtcp = LocalRtp = ((IPEndPoint)duplexed.LocalEndPoint);

                RemoteRtcp = RemoteRtp = LocalRtp.Create(((IPEndPoint)duplexed.RemoteEndPoint).Serialize());

                ServerRtcpPort = ServerRtpPort = ((IPEndPoint)RemoteRtp).Port;

                ClientRtcpPort = ClientRtpPort = ((IPEndPoint)LocalRtp).Port;

                //duplexed.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);
                
                RtpSocket = RtcpSocket = duplexed;

                RtpSocket.DontFragment = true;

                //Disable Nagle
                //RtpSocket.NoDelay = true;
            }

            /// <summary>
            /// Tcp Interleaving.... but could also support UDP...
            /// Would only indicate the Rtp and Rtcp occur on different ports
            /// </summary>
            /// <param name="rtpSocket"></param>
            /// <param name="rtcpSocket"></param>
            public void IntializeSockets(Socket rtpSocket, Socket rtcpSocket)
            {
                RtpSocket = rtpSocket;
                RtcpSocket = rtcpSocket;

                LocalRtcp = RtcpSocket.LocalEndPoint;
                RemoteRtcp = RtcpSocket.RemoteEndPoint;

                LocalRtp = RtpSocket.LocalEndPoint;
                RemoteRtp = RtpSocket.RemoteEndPoint;

                ServerRtcpPort = ((IPEndPoint)RemoteRtcp).Port;
                ServerRtpPort = ((IPEndPoint)RemoteRtp).Port;

                ClientRtcpPort = ((IPEndPoint)LocalRtcp).Port;
                ClientRtpPort = ((IPEndPoint)LocalRtp).Port;

                //if (rtpSocket.ProtocolType == ProtocolType.Tcp) rtpSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);


                //if (rtcpSocket.ProtocolType == ProtocolType.Tcp) rtcpSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);
            }

            public void CloseSockets()
            {
                if (Disposed) return;

                // A Field / Property indicating if the socket is owned and should be disposed may be useful here.
                //For now the client who owns the socket has this property

                //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                if (RtcpSocket != null && RtpSocket.Handle != RtcpSocket.Handle && RtcpSocket.Handle.ToInt64() > 0)
                {
                    if (RtcpSocket.ProtocolType == ProtocolType.Tcp) 
                        RtcpSocket.Disconnect(true);
                    else 
                        RtcpSocket.Dispose();
                    RtcpSocket = null;
                }

                //Close the RtpSocket
                if (RtpSocket != null && RtpSocket.Handle.ToInt64() > 0)
                {
                    if (RtpSocket.ProtocolType == ProtocolType.Tcp) 
                        RtpSocket.Disconnect(true);
                    else
                        RtpSocket.Dispose();
                    RtpSocket = null;
                }
            }

            /// <summary>
            /// Removes references to any reports receieved and resets the validation counters to 0.
            /// </summary>
            internal void ResetState(bool disposing = false)
            {

                if (RemoteSynchronizationSourceIdentifier.HasValue) RemoteSynchronizationSourceIdentifier = null;// default(int);

                Interlocked.Exchange(ref RtpPacketsSent, 0);

                Interlocked.Exchange(ref RtpBytesSent, 0);

                Interlocked.Exchange(ref RtcpPacketsSent, 0);

                Interlocked.Exchange(ref RtcpBytesSent, 0);

                ////////////////////

                Interlocked.Exchange(ref RtpPacketsReceived, 0);

                Interlocked.Exchange(ref RtpBytesRecieved, 0);

                Interlocked.Exchange(ref RtcpBytesRecieved, 0);

                Interlocked.Exchange(ref RtcpPacketsReceieved, 0);


            }

            /// <summary>
            /// Disposes the TransportContext and all underlying resources.
            /// </summary>
            public virtual void Dispose()
            {
                if (Disposed) return;

                Disposed = true;

                CloseSockets();
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

        //How RtpTransport is taking place
        internal ProtocolType m_TransportProtocol;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;
        internal bool m_StopRequested;

        //Outgoing Packets, Not a Queue because you cant re-order a Queue and you can't take a range from the Queue
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Created from an existing socket we should not close.
        internal bool m_SocketOwner = true;

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
        /// Raised when a complete RtpFrame was changed
        /// </summary>
        public event RtpFrameHandler RtpFrameChanged;

        /// <summary>
        /// Handles data which was interleaved when utilziing TCP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="memory"></param>
        //protected internal virtual void HandleInterleavedData(object sender, ArraySegment<byte> memory)
        //{
        //    //if(!Disposed && sender != this) ParseAndCompleteData(memory);
        //    if (!Disposed)
        //    {
        //        ParseAndCompleteData(memory);
        //    }
        //}


        //6.3.2 Initialization....
            //I will do no such thing, I will no have no table when no table is required such as be the case when no expectance is put on the identity of the recipient.
            //All such packets should be considered equal unless specifically negioated by means provided by an alternate mechanism such as SDP or the RTP-Info header and is beyond the scope of the RtpClient implementation [based on my interpretation that is.]
            //I could go on and on about this but I think we all get the point

        //6.3.3 Rtp or Rtcp

        protected internal virtual void HandleIncomingRtcpPacket(object rtpClient, RtcpPacket packet)
        {
            //Determine if the packet can be handled
            if (!IncomingPacketEventsEnabled || Disposed || packet == null || packet.Disposed) return;

            //Got a packet to handle
            //Raise an event for the packet received
            //* Note that the events should only expose Headers and the Payload should be Taboo unless within the implementation.*
            //A Mechanism would be required to subsequently retrieve the payload from the header if the packet is still `alive` e.g. not disposed.
            OnRtcpPacketReceieved(packet);

            //if (packet.Header.IsCompressed)
            //{
                //return;
            //}

            //Get a context for the packet by the identity of the receiver
            TransportContext transportContext = GetContextForPacket(packet);            

            //If there is no coresponding transportChannel 
            if (transportContext == null) //Should also check rtcp endabled
            {                
                //The packet is probably compressed.
                //Sending a report back indicates supported compression which will be supported in due time.
                //SendReports();

                if (packet.PayloadType == 13 || GetContextByPayloadType(packet.PayloadType) != null)
                {
                    //System.Diagnostics.Debug.WriteLine("Incoming RtcpPacket actually was Rtp. Type=" + packet.PayloadType + "  Ssrc=" + packet.SynchronizationSourceIdentifier + " Len=" + packet.Length);
                    OnRtpPacketReceieved(new RtpPacket(packet.Prepare().ToArray(), 0));
                    return;
                }

                //System.Diagnostics.Debug.WriteLine("Incoming RtcpPacket Dropped. Type=" + packet.PayloadType + "  Ssrc=" + packet.SynchronizationSourceIdentifier + " Len=" + packet.Length);

                //SendReports();

                return;

                //transportContext = TransportContexts.Last(tc=>tc.RtcpEnabled);

                //if(transportContext == null) return;

                //var newTransportContext = new TransportContext((byte)(transportContext.DataChannel + 2), (byte)(transportContext.ControlChannel + 2), RFC3550.Random32(packet.PayloadType), true, 0, 0);

                //newTransportContext.MediaDescription = transportContext.MediaDescription;

                //newTransportContext.RtpSocket = transportContext.RtpSocket;
                //newTransportContext.RtcpSocket = transportContext.RtcpSocket;

                //newTransportContext.ClientRtcpPort = transportContext.ClientRtcpPort;
                //newTransportContext.ClientRtpPort = transportContext.ClientRtpPort;
                //newTransportContext.LocalRtp = transportContext.LocalRtp;
                //newTransportContext.LocalRtcp = transportContext.LocalRtcp;

                //newTransportContext.ServerRtpPort = transportContext.ServerRtpPort;
                //newTransportContext.ServerRtcpPort = transportContext.ServerRtcpPort;
                //newTransportContext.RemoteRtp = transportContext.RemoteRtp;
                //newTransportContext.RemoteRtcp = transportContext.RemoteRtcp;

                //AddTransportContext(newTransportContext);

                //transportContext = newTransportContext;
            }
            else if (!transportContext.RtcpEnabled) return;
            else if (transportContext.SynchronizationSourceIdentifier == packet.SynchronizationSourceIdentifier)
            {
                SendGoodbye(transportContext, Encoding.UTF8.GetBytes("ssrc"));
                transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(transportContext.SynchronizationSourceIdentifier);
                transportContext.ResetState();
                //return;
            }

            //Get the payload type of the packet
            int payloadType = packet.PayloadType, partyId = packet.SynchronizationSourceIdentifier;

            //Make a copy of the packet now and only refer to this copy
            RtcpPacket localPacket = packet;

            //////A valid RtcpPacket is received if not complete then complete it now
            if (!localPacket.IsComplete)
            {
                //localPacket = packet.Clone(true, true, false);
                //The packet will use the existing buffer to complete itself if possible otherwise it WILL allocte memory
                localPacket.CompleteFrom(transportContext.RtcpSocket);
            }

            //Sample the clock
            transportContext.m_LastRtcpIn = DateTime.UtcNow;

            //Increment packets received for the valid context.
            Interlocked.Increment(ref transportContext.RtcpPacketsReceieved);

            //Keep track of the the bytes sent in the context
            Interlocked.Add(ref transportContext.RtcpBytesRecieved, localPacket.Length);

            //If the context is valid, AND the remote identify has a value and the packet identity is not the same then reset the state and account for the new identity
            if (transportContext.IsValid && transportContext.RemoteSynchronizationSourceIdentifier.HasValue && localPacket.SynchronizationSourceIdentifier != transportContext.RemoteSynchronizationSourceIdentifier)
            {
                //Tell the source we are no longer listening to the old identity
                //SendGoodbye(transportContext);

                //Reset state for the counters
                //transportContext.ResetState();

                //Assign the new remote ID
                transportContext.RemoteSynchronizationSourceIdentifier = localPacket.SynchronizationSourceIdentifier;

                //Send reports
                SendReports(transportContext);
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
        protected internal virtual void HandleFrameChange(object /*RtpClient*/ sender, RtpFrame frame)
        {
            ////TransportContext context = GetContextByPayloadType(frame.PayloadTypeByte);
            //////If there is a context
            ////if (context == null) return;
        }

        /// <summary>
        /// Updates counters and fires a FrameChanged event if required.
        /// </summary>
        /// <param name="sender">The object which raised the event</param>
        /// <param name="packet">The RtpPacket to handle</param>
        protected internal virtual void HandleIncomingRtpPacket(object/*RtpClient*/ sender, RtpPacket packet)
        {
            //Determine if the incoming packet should be handled
            if (!IncomingPacketEventsEnabled || Disposed || packet == null || packet.Disposed) return;

            //Not supported at the moment
            if (packet.Header.IsCompressed)
            {
                return;
            }            

            //Get the transportChannel for the packet by the payload type of the RtpPacket, not the SSRC because it may have not yet been defined.
            //This is not per RFC3550
            TransportContext transportContext = GetContextForPacket(packet);

            //If the context is still null then attempt to find one by the ssrc
            if (transportContext == null)
            {
                //System.Diagnostics.Debug.WriteLine("Unaddressed RTP Packet " + packet.SynchronizationSourceIdentifier + " PT =" + packet.PayloadType + " len =" + packet.Length);
                return;
            }

            //Check for a collision
            if (packet.SynchronizationSourceIdentifier == transportContext.SynchronizationSourceIdentifier)
            {
                transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(transportContext.SynchronizationSourceIdentifier);
                SendGoodbye(transportContext, Encoding.UTF8.GetBytes("ssrc"));
                return;
            }
            //If the transportContext was null we cannot handle this packet, e.g. because there is no context with a session description to match.
            else if (transportContext.Version != packet.Version)
            {
                //System.Diagnostics.Debug.WriteLine("Discarding packet version=" + packet.Version+ " type=" + packet.PayloadType + " len=" + packet.Length);
                return;
            }
            else if (transportContext.IsValid && transportContext.RemoteSynchronizationSourceIdentifier.HasValue && transportContext.RemoteSynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
            {
                transportContext.RemoteSynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
            }

            //Sample the clock
            transportContext.m_LastRtpIn = DateTime.UtcNow;

            //Make a copy of the packet now and only reference this packet
            RtpPacket localPacket = packet;

            if (!transportContext.ValidatePacketAndUpdateSequenceNumber(localPacket)) return;

            //if the packet is not complete then complete it now
            else if (!localPacket.IsComplete)
            {
                //localPacket = packet.Clone(true, true, true, true, false);
                localPacket.CompleteFrom(transportContext.RtpSocket);
            }

            //Increment RtpPacketsReceived for the context relating to the packet.
            Interlocked.Increment(ref transportContext.RtpPacketsReceived);

            //The counters for the bytes will now be be updated for the invalid packet
            Interlocked.Add(ref transportContext.RtpBytesRecieved, localPacket.Payload.Count());

            transportContext.UpdateJitterAndTimestamp(localPacket);

            //if (packet.PayloadType != transportContext.MediaDescription.MediaFormat || packet.Timestamp < transportContext.RtpTimestamp) return;
                //packet.PayloadType = transportContext.MediaDescription.MediaFormat;

            //Update values if in state
            //If the SSRC identifier in the packet is one that has been received before, then the packet is probably valid and checking if the sequence number is in the expected range provides further validation.
                                                                            //|| packet.SynchronizationSourceIdentifier != transportContext.RemoteSynchronizationSourceIdentifier
            //Fire an event now to let subscribers know a packet has arrived @ the client from the socket and is realated to a relevent context.
            OnRtpPacketReceieved(localPacket);

            if (!FrameChangedEventsEnabled) return;

            //if (packet.PayloadType != transportContext.MediaDescription.MediaFormat) return;

            try
            {
                //If we have not allocated a currentFrame
                if (transportContext.CurrentFrame == null)
                {
                    //make a frame
                    transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);
                }//Check to see if the frame belongs to the last frame
                else if (transportContext.LastFrame != null && packet.Timestamp == transportContext.LastFrame.Timestamp && localPacket.PayloadType == transportContext.MediaDescription.MediaFormat)
                {
                    //Add the packet clone to the current frame
                    transportContext.LastFrame.Add(new RtpPacket(packet.Prepare().ToArray(), 0));

                    //If the frame is complete then fire an event and make a new frame
                    if (transportContext.LastFrame.Complete)
                    {
                        //The LastFrame changed
                        OnRtpFrameChanged(transportContext.LastFrame);

                        transportContext.LastFrame = null;
                    }
                    else if (transportContext.LastFrame.Count > transportContext.LastFrame.MaxPackets)
                    {
                        //Backup of frames
                        transportContext.LastFrame.Dispose();
                        transportContext.LastFrame = null;
                    }

                    return;
                }//Check to see if the frame belongs to a new frame
                else if (transportContext.CurrentFrame != null && packet.Timestamp != transportContext.CurrentFrame.Timestamp && localPacket.PayloadType == transportContext.MediaDescription.MediaFormat)
                {
                    //Move the current frame to the LastFrame
                    transportContext.LastFrame = transportContext.CurrentFrame;

                    //Make a new frame in the transportChannel's CurrentFrame
                    transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);

                    //The LastFrame changed
                    OnRtpFrameChanged(transportContext.LastFrame);
                }
               
                //Add the packet clone to the current frame
                if(localPacket.PayloadType == transportContext.MediaDescription.MediaFormat)
                    transportContext.CurrentFrame.Add(new RtpPacket(localPacket.Prepare().ToArray(), 0));

                //If the frame is complete then fire an event and make a new frame
                if (transportContext.CurrentFrame.Complete)
                {
                    //Move the current frame to the LastFrame
                    transportContext.LastFrame = transportContext.CurrentFrame;

                    //Allow for a new frame to be allocated
                    transportContext.CurrentFrame = null;

                    //The LastFrame changed
                    OnRtpFrameChanged(transportContext.LastFrame);
                }
                else if (transportContext.CurrentFrame.Count > transportContext.CurrentFrame.MaxPackets)
                {
                    //Backup of frames
                    transportContext.CurrentFrame.Dispose();
                    transportContext.CurrentFrame = null;
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Increments the RtpBytesSent and RtpPacketsSent for the TransportChannel related to the packet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        protected internal virtual void HandleRtpPacketSent(object sender, RtpPacket packet)
        {
            if (Disposed || packet == null || !packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            if (transportContext == null) return;

            if (transportContext.UpdateSequenceNumber(packet.SequenceNumber))
            {
                //update the jitter
                transportContext.UpdateJitterAndTimestamp(packet);
            }

            //increment the counters
            Interlocked.Add(ref transportContext.RtpBytesSent, packet.Payload.Count());

            Interlocked.Increment(ref transportContext.RtpPacketsSent);

            //Sample the clock for when the last rtp packet was sent
            transportContext.m_LastRtpOut = packet.Transferred.Value;
        }

        protected internal virtual void HandleRtcpPacketSent(object sender, RtcpPacket packet)
        {

            if (Disposed || packet == null || !packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            if (transportContext == null) return;

            Interlocked.Add(ref transportContext.RtcpBytesSent, packet.Length);

            Interlocked.Increment(ref transportContext.RtcpPacketsSent);

            transportContext.m_LastRtcpOut = packet.Transferred.Value;
        }

        protected internal void OnInterleavedData(ArraySegment<byte> data)
        {
            if (!Disposed && InterleavedData != null) InterleavedData(this, data);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            if (!Disposed && RtpPacketReceieved != null) RtpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {
            if (!Disposed && RtcpPacketReceieved != null) RtcpPacketReceieved(this, packet);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtpPacketSent(RtpPacket packet)
        {
            if (!Disposed) RtpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketSent(RtcpPacket packet)
        {
            if (!Disposed) RtcpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtpFrameHandler for the given frame if FrameEvents are enabled
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        internal void OnRtpFrameChanged(RtpFrame frame)
        {
            if (!Disposed && RtpFrameChanged != null && FrameChangedEventsEnabled) RtpFrameChanged(this, frame);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the RtpClient) during reports
        /// </summary>
        public double MaximumRtcpBandwidthPercentage { get; set; }

        ///There should be a SendersPercentage and a ReceiversPercentage
        /// Per 6.2 RTCP Transmission Interval
        /// 
        ///The above property more accurately reflects the `limit called the "session bandwidth"`,
        ///and as is currently implemented when set to 0 allows unlimited reporting which is definitely not accurate to RFC3550 
        /// which does state that `Using two parameters allows RTCP reception reports to be turned off entirely for a particular session by setting the RTCP bandwidth for non-data-senders to zero while
        /// keeping the RTCP bandwidth for data senders non-zero so that sender reports can still be sent for inter-media synchronization.  Turning off RTCP reception reports is NOT RECOMMENDED...`
        /// 
        /// It is my interpretation and subsequently this implementation that setting the value to 0 currently causes unlimited reports to be sent.

        //It is also noted:
        //A higher-level session control protocol, which is beyond the scope of this document, may be needed.

        /// <summary>
        /// Gets or sets a value indicating the amount of seconds that a packet must be sent or received in before the RtpClient stops sending or receiving data.
        /// 96 Milliseconds by Default.
        /// </summary>
        public TimeSpan InactivityTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value which prevents Incoming Rtp and Rtcp packet events from being handled
        /// </summary>
        public bool IncomingPacketEventsEnabled { get; set; }

        public bool FrameChangedEventsEnabled { get; set; }

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
                if (!RtcpEnabled) return true;

                //If disposed no limit is imposed do not check
                if (!Disposed && MaximumRtcpBandwidthPercentage == 0) return false;

                int amountOfContexts = TransportContexts.Count();

                if (amountOfContexts == 0) return true;

                //Obtain the summation of the total bytes sent over the amount of context's
                long totalReceived = TotalBytesReceieved;

                if (totalReceived == 0) return false;

                //totalReceived /= amountOfContexts;

                long totalSent = TotalBytesSent;

                //totalSent /= amountOfContexts;

                return totalReceived / Uptime.TotalSeconds <= (totalSent / Uptime.TotalSeconds) / MaximumRtcpBandwidthPercentage;
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

        public long TotalRtpBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpBytesSent + RtpHeader.Length * c.RtpPacketsSent); } }

        public long TotalRtpBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpBytesRecieved + RtpHeader.Length * c.RtpPacketsSent); } }

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
                m_BufferLength = 4096;

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
            //RtpPacketReceieved += new RtpPacketHandler(HandleIncomingRtpPacket);
            //RtcpPacketReceieved += new RtcpPacketHandler(HandleIncomingRtcpPacket);
            RtpPacketSent += new RtpPacketHandler(HandleRtpPacketSent);
            RtcpPacketSent += new RtcpPacketHandler(HandleRtcpPacketSent);
            RtpFrameChanged += new RtpFrameHandler(HandleFrameChange);
            //InterleavedData += new InterleaveHandler(HandleInterleavedData);
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
        /// Creates a RtpClient using the given existing socket. Used for Duplexing Rtp and Rtcp on the same socket.
        /// The socket will not be disposed with the RtpClient
        /// </summary>
        /// <param name="existing">The existing Tcp Socket</param>
        /// <param name="memory"></param>
        /// <param name="inactivityTimeout"></param>
        RtpClient(Socket existing, ArraySegment<byte> memory = default(ArraySegment<byte>), TimeSpan? inactivityTimeout = null)
            : this(memory, inactivityTimeout)
        {
            m_RemoteAddress = ((IPEndPoint)existing.RemoteEndPoint).Address;
            m_SocketOwner = false;
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
        public virtual void Add(TransportContext context)
        {
            foreach (TransportContext c in TransportContexts)
            {
                //if (c.DataChannel == context.DataChannel || c.ControlChannel == context.ControlChannel) throw new RtpClientException("Requested Channel is already in use");
                if (c.SynchronizationSourceIdentifier == context.SynchronizationSourceIdentifier) throw new RtpClientException("Requested SSRC is already in use");
            }
            TransportContexts.Add(context);
        }

        #region Rtcp

        /// <summary>
        /// Sends a Goodbye to for all transportChannels, which will also stop the process sending or receiving after the Goodbye is sent
        /// //Needs SSRC
        /// </summary>
        public virtual void SendGoodbyes()
        {
            foreach (RtpClient.TransportContext tc in TransportContexts)
                 SendGoodbye(tc);
        }

        internal protected virtual int SendGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null)
        {
            //Make a Goodbye, indicate version in Client, allow reason for leaving 
            //Todo add other parties where null with SourceList
            GoodbyeReport goodBye = new GoodbyeReport(context.Version, ssrc ?? (int)context.SynchronizationSourceIdentifier, reasonForLeaving);

            IEnumerable<RtcpPacket> compound = goodBye.Yield();

            if (Disposed || context.RtcpSocket.Handle.ToInt32() <= 0 || context.TotalPacketsSent + context.TotalPacketsReceived == 0) return 0;

            //If the context has to report send operations
            if (context.RtpBytesSent > 0)
            {
                //Insert the last SendersReport as the first compound packet
                compound = Enumerable.Concat(CreateSendersReport(context, false).Yield(), compound);
            }

            //If the context has to report receive operations
            if (context.TotalPacketsReceived > 0 || compound.Count() == 1)
            {
                //Insert the last ReceiversReport as the first compound packet
                compound = Enumerable.Concat(CreateReceiversReport(context, false).Yield(), compound);
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
        internal protected virtual SendersReport CreateSendersReport(TransportContext context, bool empty)
        {

            bool includeBlocks = !empty; //&& context.LastRtcpReportSent != TimeSpan.Zero;

            SendersReport result = new SendersReport(context.Version, false, includeBlocks ? 1 : 0, (int)context.SynchronizationSourceIdentifier);

            DateTime now = DateTime.UtcNow;

            //Use the values from the TransportChannel
            //result.NtpTimestamp = 0;
            result.NtpTime = now;
            result.RtpTimestamp = context.RtpTimestamp;// +context.RtpTimestamp * 1000;

            //Counters
            result.SendersOctetCount = (int)context.RtpBytesSent;
            result.SendersPacketCount = (int)context.RtpPacketsSent;

            //If source blocks are included include them and calculate their statistics
            if (includeBlocks)
            {

                #region Delay and Fraction

                //Should be a single function and when backoff reached 0 RtcpDisabled should be true....

                //Currently if rtcp becomes `disabled` then Goodbye will not be handled and the source will eat cpu will timing out...

                //Should be performed in the Conference level, these values here will only 
                //should allow a backoff to occur in reporting and possibly eventually to be turned off.

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
                    (byte)fraction,
                    lost,
                    (int)context.SequenceNumber,
                    (int)context.RtpJitter,
                    //The middle 32 bits out of 64 in the NTP timestamp (as explained in Section 4) received as part of the most recent RTCP sender report (SR) packet from source SSRC_n. If no SR has been received yet, the field is set to zero.
                    (int)( (context.NtpTimestamp >> 16) << 32 ),
                    //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                    (int)(now - context.LastRtcpReportSent).TimeOfDay.TotalSeconds / 65535));
                    //(int)DateTime.UtcNow.Subtract(context.LastRtcpReportSent).TotalSeconds / 65535));
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="ReceiversReport"/> from the given context.
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="empty">Indicates if the report should be empty</param>
        /// <returns>The report created</returns>
        internal protected virtual ReceiversReport CreateReceiversReport(TransportContext context, bool empty)
        {
            ReceiversReport result = new ReceiversReport(context.Version, false, empty ? 0 : 1, (int)context.SynchronizationSourceIdentifier);

            if (!empty)
            {

                #region Delay and Fraction

                //Should be performed in the Conference level, these values here will only 
                //should allow a backoff to occur in reporting and possibly eventually to be turned off.
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
                       (byte)fraction,
                    lost,
                    (int)context.SequenceNumber,
                    (int)context.RtpJitter,
                    (int)(context.SendersReport != null ? Utility.DateTimeToNptTimestamp32(context.SendersReport.NtpTime) : 0),
                    (context.SendersReport != null ? ((DateTime.UtcNow - context.SendersReport.Created).Seconds / 65535) * 1000 : 0)
                ));

            }
            return result;
        }

        /// <summary>
        /// Sends a RtcpSendersReport for each TranportChannel
        /// </summary>
        public virtual void SendSendersReports()
        {
            if (!Disposed && !m_StopRequested) foreach (TransportContext tc in TransportContexts) SendSendersReport(tc);
        }

        /// <summary>
        /// Send any <see cref="SendersReport"/>'s required by the given context immediately reguardless of bandwidth state.
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        internal protected virtual int SendSendersReport(TransportContext context)
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
            //if (context.SendersReport.BlockCount == 0 || (DateTime.UtcNow - context.SourceDescription.Transferred) > InactivityTimeout) return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>().Concat((context.SourceDescription = CreateSourceDescription(context)).Yield()));
            //return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>());

            //Always send compound for now
            return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>().Concat((context.SourceDescription = CreateSourceDescription(context)).Yield()));
        }

        /// <summary>
        /// Send any <see cref="ReceiversReports"/> required by this RtpClient instance.
        /// </summary>
        public virtual void SendReceiversReports()
        {
            if (!Disposed && !m_StopRequested) foreach (TransportContext tc in TransportContexts) SendReceiversReport(tc);
        }

        /// <summary>
        /// Send any <see cref="ReceiversReports"/>'s required by the given context immediately reguardless of bandwidth state.
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        internal protected virtual int SendReceiversReport(TransportContext context)
        {
            if (context == null || !context.RtcpEnabled || context.Disposed || context.RtpBytesSent > 0) return 0;
            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            else if (context.SynchronizationSourceIdentifier == 0)
            {
                // Must be guaranteed to be unique per session
                context.SynchronizationSourceIdentifier = RFC3550.Random32((Rtcp.ReceiversReport.PayloadType));
            }

            //create and store the receivers report sent
            context.ReceiversReport = CreateReceiversReport(context, false);

            //Only the SourceDescription is omitted if required
            if (context.ReceiversReport.BlockCount == 0 || !RtcpBandwidthExceeded) return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>().Concat((context.SourceDescription = CreateSourceDescription(context)).Yield()));
            return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>());
        }

        /// <summary>
        /// Creates a <see cref="SourceDescriptionReport"/> from the given context.
        /// If <paramref name="cName"/> is null then <see cref="SourceDescriptionItem.CName"/> will be used.
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="cName">The optional cName to use</param>
        /// <returns>The created report</returns>
        internal protected virtual SourceDescriptionReport CreateSourceDescription(TransportContext context, SourceDescriptionItem cName = null)
        {
            //Todo, params context overload? overload with other Items
            return new SourceDescriptionReport(context.Version, false, 0, (int)context.RemoteSynchronizationSourceIdentifier) 
            { 
                new Rtcp.SourceDescriptionChunk((int)context.SynchronizationSourceIdentifier, cName ?? SourceDescriptionItem.CName)
                //Others...
            };
        }
        //string overload?

        /// <summary>
        /// Selects a TransportContext by matching the SynchronizationSourceIdentifier to the given sourceid
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns>The context which was identified or null if no context was found.</returns>
        internal protected virtual TransportContext GetContextBySourceId(int sourceId)
        {
            if (Disposed) return null;
            try
            {
                foreach (RtpClient.TransportContext tc in TransportContexts)
                    if (tc != null && tc.SynchronizationSourceIdentifier == sourceId || tc.RemoteSynchronizationSourceIdentifier == sourceId) return tc;
            }
            catch (InvalidOperationException) { return GetContextBySourceId(sourceId); }
            catch { if (!Disposed) throw; }
            return null;
        }

        internal protected virtual TransportContext GetContextByChannel(byte channel)
        {
            if (Disposed) return null;
            try
            {
                foreach (RtpClient.TransportContext tc in TransportContexts)
                    if (tc.DataChannel == channel || tc.ControlChannel == channel) return tc;
            }
            catch (InvalidOperationException) { return GetContextByChannel(channel); }
            catch { if (!Disposed) throw; }
            return null;
        }

        /// <summary>
        /// Selects a TransportContext by using the packet's Channel property
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        internal protected virtual TransportContext GetContextForPacket(RtcpPacket packet)
        {
            if (Disposed) return null;
            //Determine based on reading the packet this is where a RtcpReport class would be useful to allow reading the Ssrc without knownin the details about the type of report
            try { return GetContextBySourceId(packet.SynchronizationSourceIdentifier); }
            catch (InvalidOperationException) { return GetContextForPacket(packet); }
            catch { if (!Disposed) throw; }
            return null;
        }

        public virtual void EnquePacket(RtcpPacket packet)
        {
            if (Disposed || m_StopRequested || packet == null || packet.Disposed) return;
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

            SocketError error;

            //Combine them, adding padding where necessary to the last packet
            //byte[] data = RFC3550.ToCompoundBytes(packets).ToArray(); //packets.SelectMany(p => p.Prepare()).ToArray();
            
            //When sending more then one packet compound packets must be padded correctly.
            
            //Just `stack` the packets as indicated if sending, assume they are valid.
            int sent = SendData(packets.Count() > 1 ? RFC3550.ToCompoundBytes(packets).ToArray() : packets.First().Prepare().ToArray(), context.ControlChannel, context.RtcpSocket, context.RemoteRtcp, out error);    //SendData(packets.SelectMany(p => p.Prepare()).ToArray(), context.ControlChannel, context.RtcpSocket, context.RemoteRtcp, out error);

            int csent = 0;

            //If the compound bytes were completely sent then all packets have been sent
            if (error == SocketError.Success && sent >= TCP_OVERHEAD)
            {
                foreach (RtcpPacket packet in packets)
                {
                    //set sent
                    packet.Transferred = DateTime.UtcNow;

                    //Raise en event
                    OnRtcpPacketSent(packet);

                    csent += packet.Length;

                    if (csent > sent) break;
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
        public TransportContext GetContextForPacket(RtpPacket packet) { if (packet == null) return null; return GetContextBySourceId(packet.SynchronizationSourceIdentifier) ?? GetContextByPayloadType(packet.PayloadType); }

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
        public TransportContext GetContextByPayloadType(int payloadType) { return TransportContexts.FirstOrDefault(c => c != null && c.MediaDescription.MediaFormat == payloadType); }

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
            if (Disposed || m_StopRequested || packet == null || packet.Disposed) return;
            //Add a the packet to the outgoing
            m_OutgoingRtpPackets.Add(packet);
        }

        //Packets in the m_OutgoingRtpPackets list should be joined into a size which is appropraite for the network underlying MTU

        public void EnqueFrame(RtpFrame frame) { if (frame == null || frame.Disposed) return; foreach (RtpPacket packet in frame) EnquePacket(packet); }

        //This could be faster if the packets were joined, they are not because of memory usage. make larger packets if required...

        public void SendRtpFrame(RtpFrame frame) { if (frame == null || frame.Disposed) return; foreach (RtpPacket packet in frame) SendRtpPacket(packet); }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        public int SendRtpPacket(RtpPacket packet, int? ssrc = null)
        {
            if (packet == null || m_StopRequested) return 0;
            
            TransportContext transportContext = GetContextForPacket(packet);

            //If we don't have an transportChannel to send on or the transportChannel has not been identified
            if (transportContext == null) return 0;
            else if (transportContext.MediaDescription.MediaFormat != packet.PayloadType && transportContext.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
            {
                //Throw an exception if the payload type does not match
                throw new RtpClientException("Packet Payload is different then the expected MediaDescription. Expected: '" + transportContext.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }

            //How many bytes were sent
            int sent = 0;

            if (transportContext.SendersReport == null && transportContext.RtcpEnabled) SendSendersReport(transportContext);

            ////If no senders report has been sent then send the SendersReport
            //if (transportContext.SendersReport == null && transportContext.RtcpEnabled)
            //{
            //    sent += SendSendersReport(transportContext);
            //}

            //The error encountered in the senddata operation as given by the send method of the socket used.
            SocketError error;

            //If the transportContext is changed to automatically update the timestamp by frequency then use transportContext.RtpTimestamp
            //Socket always required that the data be an array, project it now which should only return a pointer to the sequence existing (with the parameters changed in the packet as given) thanks to Prepare.
            //sent += SendData(packet.Prepare(packet.PayloadType, transportContext.RemoteSynchronizationSourceIdentifier).ToArray(), transportContext.DataChannel, transportContext.RtpSocket, transportContext.RemoteRtp, out error);
            sent += SendData(packet.Prepare(null, ssrc, null, null).ToArray(), transportContext.DataChannel, transportContext.RtpSocket, transportContext.RemoteRtp, out error);

            //System.Diagnostics.Debug.WriteLine(packet.PayloadType + " => To" + transportContext.RemoteSynchronizationSourceIdentifier + " From=>" + transportContext.SynchronizationSourceIdentifier + " Orig:" + packet.SynchronizationSourceIdentifier);

            if (error == SocketError.Success && sent >= packet.Length)
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
                m_WorkerThread = new Thread(new ThreadStart(SendReceieve));
                m_WorkerThread.TrySetApartmentState(ApartmentState.MTA);
                m_WorkerThread.Priority = ThreadPriority.AboveNormal;
                //m_WorkerThread.IsBackground = true;
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
                tc.Dispose();       
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
        internal int TryReadFrameHeader(byte[] buffer, int offset, out byte channel)
        {
            //Must be assigned
            channel = default(byte);

            //https://www.ietf.org/rfc/rfc2326.txt

            //10.12 Embedded (Interleaved) Binary Data

            //If the buffer does not start with the magic byte this is not a RFC2326 frame, it could be a RFC4571 frame
            if (buffer == null || buffer[offset] != BigEndianFrameControl) return -1; //goto ReadLengthOnly;

            /*
             Stream data such as RTP packets is encapsulated by an ASCII dollar
            sign (24 hexadecimal), followed by a one-byte channel identifier,
            followed by the length of the encapsulated binary data as a binary,
            two-byte integer in network byte order. The stream data follows
            immediately afterwards, without a CRLF, but including the upper-layer
            protocol headers. Each $ block contains exactly one upper-layer
            protocol data unit, e.g., one RTP packet.

            The channel identifier is defined in the Transport header with the
            interleaved parameter(Section 12.39).

            When the transport choice is RTP, RTCP messages are also interleaved
            by the server over the TCP connection. As a default, RTCP packets are
            sent on the first available channel higher than the RTP channel. The
            client MAY explicitly request RTCP packets on another channel. This
            is done by specifying two channels in the interleaved parameter of
            the Transport header(Section 12.39).
             */

            //Assign the channel
            channel = buffer[offset + 1];

            //In stand alone operation mode the RtpClient should read only the length of the frame and decypher the contents based on the Payload.

            //SEE [COMEDIA]ly
            //http://tools.ietf.org/search/rfc4571#section-3

            //The ssrc may be useless due to middle boxes which have re-compresssed the data and sent it along with a new identifier....
            //The new identifier would be valid but would imply that the packet came from a different source (which since it was re-sampled it should)...
            //However
            //Based on my interpreatation the SSRC doesn't need to change @ all.
            //The packet's ContribuingSourceCount could be incremented by 1 (By the middle box who would subsequently add it's OWN identifier to the ContributingSourceList.)
            //The packet should become 4 bytes larger per hop that it is compressed or altered in due to the added entry.
            //If CC = 15 then no compression should be performed and the packet may need to be dropped if the destination is not within the next hop...

            //This would allow a receiver to dictate that middle boxes are causing unwanted compression or delay in the stream and subsequenlty the ability drop all packets from that middle box if required by iterating the contributing source list.
            //It would also allow such a receiver to either expediate or change the packet in another such way
            //IMHO IF THE ORIGINAL SSRC CHANGES before the packet reached the application THIS HAS DIRE CONSEQUENCES when the IDENTITY IS EXPECTED to be a particular value...
            //The application would have no way to verify that the data is indeed from Middle box X, Y or Z without using some form of verification i.e encryption.

            //Last but not least using 2 tcp sockets would be more performant but would require double the overhead from the provider, almost double the bandwidth (in protocol overhead) and definitely double the security issues.

            //RFC4571 - Out-of-band semantics
            //Section 2 does not define the RTP or RTCP semantics for closing a
            //TCP socket, or of any other "out of band" signal for the
            //connection.

            //With respect to Rtcp the sender should eventually timeout in the application, but the problem here lies in the fact the middle box has no control over that.
            //Thus the middle box it self will become conjested waiting for the timeout...
            //Additionally RTCP may not be enabled... if this is the case there would be no `Goodbye`
            //If RTCP was enabled then
            //Since the return route may not involve the same middle box which 'helped' it[the middlebox] may not get the `Goodbye` indication from the application participant,
            //Thus they would only timeout with respect to their own implementation rules for such,
            //BUT COULD ALSO receive another packet from another session which just happens to have the same SSRC
            //I / We would hope in such a case that the EndPoint would be different FROM the application's EndPoint because if it was not then that packet would subsequently routed to the application....
            
            //Lastly if the middle box compressed the data in any such way the payload indication would possibly be modified (and should be if the format changed)... thus breaking the compatability with the receiving application.
            //This implies that the Payload indication cannot change but the timestamp possibly could to reflect more delay if required but that should be handled by the application anyway.... not a middle box

            //Thus RTCP may be better suited for this type of 'change' e.g. each middle box could handle RtcpPackets to reflect the delay without changing the data within the rtp packet at all
            //upon receving a RtcpPacket The BlockCount could be incremented and an additional block could be added to indicate the metrics e.g. delay and jitter introduced by said middle box.
            //This would allow the receiving application to essentially ask that theat middle box not route packets any more or ask that it expedite routing et al.

            

        ///ReadLengthOnly:

            //Return the result of reversing the Unsigned 16 bit integer at the offset (A total of 4 byte)
            if (BitConverter.IsLittleEndian) return Common.Binary.ReverseU16((BitConverter.ToUInt16(buffer, offset + 2)));
            return BitConverter.ToUInt16(buffer, offset + 2);
        }

        /// <summary>
        /// Returns the amount of bytes read to completely read the RFC2326 frame.
        /// Where a - return value indicates over-reading.
        /// </summary>
        /// <param name="received"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        //http://tools.ietf.org/search/rfc4571
        internal int ReadRFC2326FrameHeader(int received,  out byte frameChannel, out RtpClient.TransportContext context, int? offset = null, byte[] buffer = null)
        {

            //There is no relevant TransportContext assoicated yet.
            context = null;

            //The channel of the frame - The Framing Method
            frameChannel = default(byte);

            if (received <= 0) return 0;

            buffer = buffer ?? m_Buffer;

            //Look for the frame control octet
            int mOffset = offset ?? m_BufferOffset, startOfFrame = Array.IndexOf<byte>(buffer, BigEndianFrameControl, mOffset, received);

            //If not found everything belongs to the upper layer
            if (startOfFrame == -1)
            {
                if (received > TCP_OVERHEAD)
                {
                    //System.Diagnostics.Debug.WriteLine("Interleaving: " + received);
                    OnInterleavedData(new ArraySegment<byte>(buffer, mOffset, received));
                }
                return 0;
            }
            else if (startOfFrame > offset) // If the start of the frame is not at the beginning of the buffer
            {
                do
                {
                    //Determine the amount of data which belongs to the upper layer
                    int upperLayerData = startOfFrame - mOffset;

                    //System.Diagnostics.Debug.WriteLine("Moved To = " + startOfFrame + " Of = " + received + " - Bytes = " + upperLayerData + " = " + Encoding.ASCII.GetString(m_Buffer, mOffset, startOfFrame - mOffset));

                    OnInterleavedData(new ArraySegment<byte>(buffer, mOffset, upperLayerData));

                    mOffset = startOfFrame;

                    received -= upperLayerData;

                    //If there is a context for the data then break looking for the frame control byte
                    if (GetContextByChannel(buffer[mOffset + 1]) != null) break;

                } while (received >= TCP_OVERHEAD && (startOfFrame = Array.IndexOf<byte>(buffer, BigEndianFrameControl, mOffset + 1, received - 1)) >= 0);
            }

            if (received < TCP_OVERHEAD) return 0;

            //Create the frameData segment to preserve the data boundaries
           // ArraySegment<byte> frameSegment = new ArraySegment<byte>(m_Buffer, mOffset, received);

            //NEEDS TO HANDLE CASES WHERE RFC4571 Framing are in play and no $ or Channel are used....

            //The amount of data needed for the frame
            int frameLength = TryReadFrameHeader(buffer, mOffset, out frameChannel);

            if (frameLength <= 0) return 0;// The null packet
          
            //Assign context if possible
            context = GetContextByChannel(frameChannel);

            //Determine how may how more bytes need to be read to complete the frame
            return frameLength;
        }

        /// <summary>
        /// Parses the data in the buffer for valid Rtcp and Rtcp packet instances.
        /// </summary>
        /// <param name="memory">The memory to parse</param>
        /// <param name="from">The socket which received the data into memory and may be used for packet completion.</param>
        internal virtual void ParseAndCompleteData(ArraySegment<byte> memory, bool parseRtcp = true, bool parseRtp = true, int? remaining = null)
        {
            if (parseRtcp == false && parseRtp == false) return;

            //Cache start, count and index
            int offset = memory.Offset, count = memory.Count, index = 0,
                //Calulcate remaining
            mRemaining = remaining ?? count - index;

            if (count <= 0) return;

            try
            {
                //Distinguishable RTP and RTCP Packets
                //http://tools.ietf.org/search/rfc5761#section-4

                //Observation 1) Rtp packets can only have a PayloadType from 64-95
                //However Rtcp Packets may also use PayloadTypes 72- 76.. (Reduced size...)

                //Observation 2) Rtcp Packets defined in RFC3550 Start at 200 (SR -> Goodbye) 204,
                // 209 - 223 is cited in the above as well as below
                //RTCP packet types in the ranges 1-191 and 224-254 SHOULD only be used when other values have been exhausted.


                //Common.CommonHeaderBits header = new Common.CommonHeaderBits(memory);

                //parseRtcp = header.RtcpPayloadType >= 64 && header.RtcpPayloadType <= 95 || GetContextByPayloadType(header.RtpPayloadType) != null;

                //parseRtp = !parseRtcp;

                //Iterate until index approaches remaining
                //while (mRemaining > 0)
                //{
                
                    //If rtcp should be parsed
                    if (mRemaining > RtcpHeader.Length && parseRtcp)
                    {
                        //Copy valid RtcpPackets out of the buffer now, if any packet is not complete it will be completed only if required.
                        foreach (RtcpPacket rtcp in RtcpPacket.GetPackets(memory.Array, offset + index, mRemaining))
                        {
                            //Raise an event for each packet.
                            //OnRtcpPacketReceieved(rtcp);
                            HandleIncomingRtcpPacket(this, rtcp);

                            //Move the offset the length of the packet parsed
                            index += rtcp.Length;
                            mRemaining -= rtcp.Length;
                        }

                    }
                    //If rtp is parsed
                    if (mRemaining > RtpHeader.Length && parseRtp)
                    {
                        //Create a packet from the data received, if the packet is not complete it will be completed only if required.
                        using (RtpPacket rtp = new RtpPacket(memory.Array.Skip(offset + index).Take(count).ToArray(), 0))
                        {
                            //Raise the event
                            HandleIncomingRtpPacket(this, rtp);

                            //Move the index past the length of the packet
                            index += rtp.Length;

                            //Calculate the amount of octets remaining in the segment.
                            mRemaining -= rtp.Length;
                        }
                    }

                    return;
                    
                //}
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
        internal protected virtual int ReceiveData(Socket socket, EndPoint remote, bool expectRtp = true, bool expectRtcp = true)
        {
            //Ensure the socket can poll
            if (Disposed || m_StopRequested || socket == null || socket.Handle.ToInt64() <= 0) return 0;

            //There is no error yet
            SocketError error = SocketError.SocketError;

            //Cache the offset at the time of the call
            int offset = m_BufferOffset,
                //Reeive data                
                received = socket.Receive(m_Buffer, offset, m_BufferLength, SocketFlags.None, out error);

            if (error != SocketError.Success) return -1;

            //If the receive was a success
            if (received > 0)
            {
                //Under TCP use Framing to obtain the length of the packet as well as the context.
                if (socket.ProtocolType == ProtocolType.Tcp)
                {
                    //Check for a partial frame header
                    while (TCP_OVERHEAD > received)
                    {
                        received += socket.Receive(m_Buffer, offset + received, TCP_OVERHEAD - received, SocketFlags.None, out error);
                        if (error != SocketError.Success && error != SocketError.TryAgain) return -1;
                    }

                    return ProcessFrameData(m_Buffer, offset, received);
                }

                //Use the data received to parse and complete any recieved packets, should take a parseState
                ParseAndCompleteData(new ArraySegment<byte>(m_Buffer, offset, received), expectRtcp, expectRtp);
            }

            //Return the amount of bytes received from this operation
            return received;
        }

        internal protected virtual int ProcessFrameData(byte[] buffer, int offset, int length)
        {
            //Determine which TransportContext will receive the data incoming
            TransportContext relevent = null;

            byte frameChannel;

            //Read the framing using the framing indicated.
            int frameLength;

            int remainingInBuffer = length, recievedTotal = remainingInBuffer;

            bool expectRtp, expectRtcp;

            //Todo
            //Handle receiving when no $ and Channel is presenent... e.g. RFC4751

            while (remainingInBuffer >= TCP_OVERHEAD)
            {
                //Parse the frameLength
                frameLength = ReadRFC2326FrameHeader(remainingInBuffer, out frameChannel, out relevent, offset, buffer);

                //Ignore large frames we can't store, set frameLength = to the bytes remaining in the buffer
                if (frameLength > m_BufferLength) frameLength = remainingInBuffer;
               
                //See how many more bytes are required from the wire
                int remainingInWire = frameLength - remainingInBuffer;

                if (remainingInWire > 0)
                {
                    if (offset + remainingInBuffer + remainingInWire + TCP_OVERHEAD > m_BufferLength)
                    {
                        //System.Diagnostics.Debug.WriteLine("Buffer Copy");
                        Array.Copy(buffer, offset, buffer, m_BufferOffset, remainingInBuffer);
                        offset = m_BufferOffset + remainingInBuffer;
                    }

                    //Frame starts here
                    int frameStart = offset - remainingInBuffer;

                    Socket socket = relevent != null && frameChannel == relevent.ControlChannel ? relevent.RtcpSocket : TransportContexts.First().RtpSocket;
                    SocketError error = SocketError.SocketError;

                    while (remainingInWire > 0)
                    {
                        int recievedFromWire = Utility.AlignedReceive(buffer, offset, remainingInWire, socket, out error);
                        if (error != SocketError.Success && error != SocketError.TryAgain) break;
                        else if (recievedFromWire <= 0) continue;
                        remainingInWire -= recievedFromWire;
                        offset += recievedFromWire;
                        recievedTotal += recievedFromWire;
                    }

                    //Set the offset to where it was before extra data was received - the data existing in the buffer.
                    offset = frameStart;

                    //If a socket error occured remove the context to no parsing occurs
                    if (error != SocketError.Success) relevent = null;

                }

                //If there any data in the frame and there is a relevent context
                if (frameLength > 0 && relevent != null)
                {
                    //Determine if Rtp or Rtcp should be parsed
                    expectRtp = !(expectRtcp = relevent.RtcpEnabled && frameChannel == relevent.ControlChannel);

                    //Determine what is less, what is remanining or the frameLength and use the smaller value
                    int minLenRem = Math.Min(remainingInBuffer - TCP_OVERHEAD, frameLength);

                    //Parse the data in the buffer
                    ParseAndCompleteData(new ArraySegment<byte>(buffer, offset + TCP_OVERHEAD, minLenRem), expectRtcp, expectRtp, minLenRem);
                }

                //Calulcate the amount of bytes to move the offset by
                int size = frameLength + TCP_OVERHEAD;

                //Move the offset
                offset += size;

                //Decrease remaining in buffer
                remainingInBuffer -= size;
            }

            return recievedTotal;
        }

        /// <summary>
        /// Sends the given data on the given channel
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="channel">The channel to send on (Udp doesn't use it)</param>
        /// <param name="useFrameControl">Indicates if `$` should preceed the data on the socket</param>
        /// <returns>The amount of bytes sent</returns>
        internal protected virtual int SendData(byte[] data, byte? channel, Socket socket, System.Net.EndPoint remote, out SocketError error, bool useFrameControl = true, bool useChannelId = true)
        {
            error = SocketError.SocketError;
            try
            {
                int sent = 0, length = 0;

                //Check there is valid data and a socket which is able to write and that the RtpClient is not stopping
                if (Disposed || m_StopRequested || data == null || socket == null || socket.Handle.ToInt64() <= 0) return sent;

                #region RFC3550 over Tcp via RFC4751 Interleaving Only

            Frame:

                //Under Tcp we must frame the data for the given channel
                if (socket.ProtocolType == ProtocolType.Tcp && channel.HasValue)
                {
                    //Create the data from the concatenation of the frame header and the data existing
                    //E.g. Under RTSP...Frame the Data in a PDU {$ C LEN ...}
                    length = data.Length;

                    if (useChannelId && useFrameControl)
                    {
                        data = Enumerable.Concat(BigEndianFrameControl.Yield(), channel.Value.Yield())
                            .Concat(BitConverter.IsLittleEndian ? BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)data.Length)) : BitConverter.GetBytes((ushort)data.Length))
                            .Concat(data).ToArray();
                        length += 4;
                    }
                    else
                    {
                        data = (BitConverter.IsLittleEndian ? BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)data.Length)) : BitConverter.GetBytes((ushort)data.Length)).Concat(data).ToArray();
                        length += 2;

                        if (useChannelId)
                        {
                            data = channel.Value.Yield().Concat(data).ToArray();
                            ++length;
                        }

                        if (useFrameControl)
                        {
                            data = BigEndianFrameControl.Yield().Concat(data).ToArray();
                            ++length;
                        }
                    }

                }
                else length = data.Length;

                #endregion

                if (length < data.Length)
                {
                    if (socket.ProtocolType == ProtocolType.Tcp && channel.HasValue)
                    {
                        data = data.Skip(4).ToArray();
                        goto Frame;
                    }
                    else length = data.Length;
                }

                //Send the frame keeping track of the bytes sent
                while (sent < length)
                {
                    sent += socket.Send(data, sent, length - sent, SocketFlags.None, out error);
                    if (error == SocketError.TryAgain) continue;
                    else if (error != SocketError.Success) break;
                }

                return sent;

            }
            catch
            {
                //Something bad happened, usually disposed already
                return -1;
            }
        }
        
        /// <summary>
        /// Sends any reports required for all owned TransportContexts using <see cref="SendReports"/>
        /// </summary>
        /// <returns>A value indicating if reports were immediately sent</returns>        
        public virtual bool SendReports()
        {
            if (m_StopRequested) return false;

            bool sent = false;

            foreach (TransportContext tc in TransportContexts)
            {
                if (!tc.Disposed && tc.RtcpEnabled && tc.SendRtcpReports && SendReports(tc))
                {
                    sent = true;
                }
            }

            return sent;            
        }

        /// <summary>
        /// Sends any <see cref="RtcpReport"/>'s immediately for the given <see cref="TransportContext"/> if <see cref="RtcpBandwidthExceeded"/> is false.
        /// </summary>
        /// <param name="context">The <see cref="TransportContext"/> to send a report for</param>
        /// <returns>A value indicating if reports were sent</returns>
        internal virtual bool SendReports(TransportContext context)
        {
            //Check for the stop signal (or disposal)
            if (m_StopRequested || Disposed ||  //Otherwise
                !context.RtcpEnabled
                || //Or Rtcp Bandwidth for this RtpClient has been exceeded
                RtcpBandwidthExceeded) return false; //No reports can be sent.


            //keep track of the bytes sent in this call
            int bytesSent = 0;

            //Start with a sequence of empty packets

            //If the last reports were sent in less time than alloted by the m_SendInterval
            if (MaximumRtcpBandwidthPercentage == 0 || context.LastRtcpReportSent == TimeSpan.Zero || context.LastRtcpReportSent >= context.m_SendInterval)
            {

                //If Rtp data was sent then send a Senders Report.
                if (context.RtpBytesSent > 0)
                {
                    //Send Senders Reports
                    bytesSent += SendSendersReport(context);                                       
                }

                //Send Receivers Reports
                bytesSent += SendReceiversReport(context);
            }

            if (bytesSent > 0)
            {
                //Indicate when the last rtcp reports were sent now
                context.m_LastRtcpOut = DateTime.UtcNow;

                //try
                //{
                //    var backoff = TimeSpan.FromMilliseconds(context.RtcpBytesSent / Uptime.TotalSeconds);

                //    //Update the sent interval with backoff.
                //    context.m_SendInterval = context.m_SendInterval.Add(backoff);
                //}
                //catch { }

                context.m_InactiveTime = TimeSpan.Zero;
            }
            

            //Indicate if reports were sent in this interval
            return bytesSent > 0;
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

            if (Disposed || m_StopRequested || context.LastRtpPacketReceived < context.m_ReceiveInterval || context.LastRtcpReportReceived < context.m_ReceiveInterval)
            {
                return false;
            }

            //Calulcate for the currently inactive time period
            if (context.Goodbye == null && context.LastRtcpReportReceived > context.m_ReceiveInterval && context.LastRtpPacketReceived > context.m_ReceiveInterval)
            {

                context.m_InactiveTime = DateTime.UtcNow - lastActivity;

                if (context.m_InactiveTime > context.m_ReceiveInterval + context.m_SendInterval)
                {
                    SendGoodbye(context);
                    inactive = true;
                }
            }

            return inactive;
        }

        /// <summary>
        /// Entry point of the m_WorkerThread. Handles sending out RtpPackets and RtcpPackets in buffer and handling any incoming RtcpPackets.
        /// Sends a Goodbye and exits if no packets are sent of recieved in a certain amount of time
        /// </summary>
        void SendReceieve()
        {
            Begin:
            try
            {

                DateTime lastOperation = DateTime.UtcNow;

                //Until aborted
                while (!m_StopRequested)
                {
                    #region Recieve Incoming Data

                    foreach (TransportContext context in TransportContexts)
                    {
                        ProcessReceive(context, ref lastOperation);
                    }

                    if (m_OutgoingRtcpPackets.Count + m_OutgoingRtpPackets.Count == 0)
                    {
                        //Should also check for bit rate before sleeping
                        System.Threading.Thread.Sleep(TransportContexts.Count);
                        continue;
                    }

                    #endregion

                    #region Handle Outgoing RtcpPackets

                    if (m_OutgoingRtcpPackets.Count > 0)
                    {
                        int remove = m_OutgoingRtcpPackets.Count;

                        if (SendRtcpPackets(m_OutgoingRtcpPackets.GetRange(0, remove)) > 0) lastOperation = DateTime.UtcNow;

                        m_OutgoingRtcpPackets.RemoveRange(0, remove);
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (m_OutgoingRtpPackets.Count > 0)
                    {
                        //Could check for timestamp more recent then packet at 0  on transporContext and discard...
                        //Send only A few at a time to share with rtcp
                        int sent = 0;

                        foreach (RtpPacket packet in m_OutgoingRtpPackets.ToArray())
                        {
                            if (packet == null || packet.Disposed) ++sent;
                            //If the entire packet was sent
                            else if (SendRtpPacket(packet) >= packet.Length)
                            {
                                ++sent;
                                lastOperation = DateTime.UtcNow;
                            }
                            else break;
                        }

                        m_OutgoingRtpPackets.RemoveRange(0, sent);                        
                    }


                    #endregion
                }
            }
            catch { if (!m_StopRequested)  goto Begin; }        
        }

         void ProcessReceive(TransportContext context, ref DateTime lastOperation)
        {
            try
            {
                //Ensure a context was given
                if (context == null || context.Disposed) return;

                //Receive Data on the RtpSocket and RtcpSocket, summize the amount of bytes received from each socket.

                int receivedRtp = 0, receivedRtcp = 0;

                bool duplexing = context.Duplexing, rtpEnabled = context.RtpEnabled, rtcpEnabled = context.RtcpEnabled;

                //If receiving Rtp AND the last Rtp reception occured in more then the time alloted from the m_ReceiveInterval
                if (rtpEnabled
                    //Check if the socket can read data
                    && context.RtpSocket.Poll((int)Math.Round(context.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                {
                    receivedRtp += ReceiveData(context.RtpSocket, context.RemoteRtp, rtpEnabled, duplexing);
                    lastOperation = DateTime.UtcNow;
                }

                //if Rtcp is enabled
                if (rtcpEnabled
                    && 
                    (context.LastRtcpReportReceived == TimeSpan.Zero || context.LastRtcpReportReceived >= context.m_ReceiveInterval)
                    &&
                    context.RtcpSocket.Poll((int)Math.Round(context.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                {

                    receivedRtcp += ReceiveData(context.RtcpSocket, context.RemoteRtcp, duplexing, rtcpEnabled);
                    lastOperation = DateTime.UtcNow;
                    if (SendReports(context)) lastOperation = DateTime.UtcNow;
                    else if (receivedRtcp == -1 && context.m_InactiveTime > TimeSpan.Zero && context.Goodbye == null && SendGoodbyeIfInactive(lastOperation, context)) throw new Common.Exception<RtpClient>(this, "Goodbye Sent");
                }

            }
            catch (AccessViolationException) { System.Threading.Thread.Sleep(context.m_ReceiveInterval); }
            catch
            {
                m_StopRequested = !Connected;
                return;
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

            DisableFeedbackReports(this);

            //RtpPacketReceieved -= new RtpPacketHandler(HandleIncomingRtpPacket);
            //RtcpPacketReceieved -= new RtcpPacketHandler(HandleIncomingRtcpPacket);
            RtpPacketSent -= new RtpPacketHandler(HandleRtpPacketSent);
            RtcpPacketSent -= new RtcpPacketHandler(HandleRtcpPacketSent);
            //RtpFrameChanged -= new RtpFrameHandler(RtpClient_RtpFrameChanged);
            //InterleavedData -= new InterleaveHandler(HandleInterleavedData);            

            Utility.Abort(ref m_WorkerThread);
        }

        IEnumerable<System.Threading.Thread> Common.IThreadOwner.OwnedThreads
        {
            get { return Disposed ? null : Utility.Yield(m_WorkerThread); }
        }

        IEnumerable<Socket> Common.ISocketOwner.OwnedSockets
        {
            get
            {

                if (Disposed) yield break;

                foreach (TransportContext tc in TransportContexts)
                {
                    yield return tc.RtpSocket;

                    if (tc.RtpSocket.ProtocolType == ProtocolType.Tcp || tc.Duplexing) continue;

                    yield return tc.RtcpSocket;
                }
            }
        }
    }
}
