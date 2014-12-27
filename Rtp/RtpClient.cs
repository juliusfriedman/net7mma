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

namespace Media.Rtp
{
    /// <summary>
    /// Provides an implementation of the <see cref="http://tools.ietf.org/html/rfc3550"> Real Time Protocol </see>.
    /// A RtpClient typically allows one <see cref="System.Net.Socket"/> to communicate (via RTP) to another <see cref="System.Net.Socket"/> via <see cref="RtpClient.TransportContext"/>'s in which some <see cref="SessionDescription"/> has been created.
    /// </summary>
    public class RtpClient : Common.BaseDisposable, Media.Common.IThreadReference, Media.Common.ISocketReference
    {
        #region Constants / Statics

        //Possibly should be moved to RFC3550

        public const string RtpProtcolScheme = "rtp", AvpProfileIdentifier = "avp", RtpAvpProfileIdentifier = "RTP/AVP";

        //Udp Hole Punch
        //Might want a seperate method for this... (WakeupRemote)
        //Most routers / firewalls will let traffic back through if the person from behind initiated the traffic.
        //Send some bytes to ensure the reciever is awake and ready... (SIP or RELOAD may have something specific and better)
        //e.g Port mapping request http://tools.ietf.org/html/rfc6284#section-4.2 
        static byte[] WakeUpBytes = new byte[] { 0x70, 0x70, 0x70, 0x70 };

        internal const byte BigEndianFrameControl = 36;//, // ASCII => $,  Hex => 24  Binary => 100100
        //LittleEndianFrameControl = 9;                   //                                     001001

        //The poing at which rollover occurs on the SequenceNumber
        const uint RTP_SEQ_MOD = (1 << 16); //65536

        const int DefaultMaxDropout = 500, DefaultMaxMisorder = 100, DefaultMinimumSequentalRtpPackets = 2;

        /// <summary>
        /// Describes the size (in bytes) of the 
        /// [MAGIC , CHANNEL, {LENGTH}] octets which preceed any TCP RTP / RTCP data When multiplexing data on a single TCP port over RTSP.
        /// </summary>
        internal const int InterleavedOverhead = 4;
        //RTP/AVP/TCP Specifies only the Length bytes in network byte order. e.g. 2 bytes

        /// <summary>
        /// The default time assocaited with Rtcp report intervals for RtpClients. (Almost 5 seconds)
        /// </summary>
        public static readonly TimeSpan DefaultReportInterval = TimeSpan.FromSeconds(4.96);

        /// <summary>
        /// Read the RFC2326 amd RFC4751 Frame header.
        /// Returns the amount of bytes in the frame.
        /// Outputs the channel of the frame in the channel variable.
        /// </summary>
        /// <param name="buffer">The data containing the RFC4751 frame</param>
        /// <param name="offset">The offset in the </param>
        /// <param name="channel">The byte which will contain the channel if the reading succeeded</param>
        /// <param name="readFrameByte">Indicates if the frameByte should be read (RFC2326)</param>
        /// <param name="frameByte">Indicates the frameByte to read</param>
        /// <returns> -1 If the buffer does not contain a RFC2326 / RFC4751 frame at the offset given</returns>
        internal static int TryReadFrameHeader(byte[] buffer, int offset, out byte channel, bool readFrameByte = true, byte frameByte = BigEndianFrameControl)
        {
            //Must be assigned
            channel = default(byte);

            //https://www.ietf.org/rfc/rfc2326.txt

            //10.12 Embedded (Interleaved) Binary Data

            //If the buffer does not start with the magic byte this is not a RFC2326 frame, it could be a RFC4571 frame
            if (readFrameByte && buffer == null || buffer[offset] != frameByte) return -1; //goto ReadLengthOnly;

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

            //Assign the channel if reading framed.
            if(readFrameByte)channel = buffer[offset + 1];

            #region Babble

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

            #endregion
            //Return the result of reversing the Unsigned 16 bit integer at the offset (A total of 4 byte)
            return Common.Binary.ReadU16(buffer, offset + 2, BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Will create a <see cref="RtpClient"/> based on the given parameters
        /// </summary>
        /// <param name="sessionDescription"></param>
        /// <param name="sharedMemory"></param>
        /// <param name="incomingEvents"></param>
        /// <param name="rtcpEnabled"></param>
        /// <returns></returns>
        public static RtpClient FromSessionDescription(Sdp.SessionDescription sessionDescription, Common.MemorySegment sharedMemory = null, bool incomingEvents = true, bool rtcpEnabled = true, Socket existingSocket = null, int? rtpPort = null, int? rtcpPort = null, int remoteSsrc = 0, int minimumSequentialRtpPackets = 2)
        {
            if (sessionDescription == null) throw new ArgumentNullException("sessionDescription");

            Sdp.Lines.SessionConnectionLine connectionLine = new Sdp.Lines.SessionConnectionLine(sessionDescription.ConnectionLine);

            IPAddress remoteIp = IPAddress.Parse(connectionLine.IPAddress), localIp = Utility.GetFirstIPAddress(remoteIp.AddressFamily);

            RtpClient participant = new RtpClient(sharedMemory, incomingEvents);

            byte lastChannel = 0;

            bool hasSocket = existingSocket != null;

            foreach (Media.Sdp.MediaDescription md in sessionDescription.MediaDescriptions)
            {
                TransportContext tc = TransportContext.FromMediaDescription(sessionDescription, lastChannel++, lastChannel++, md, rtcpEnabled, remoteSsrc, minimumSequentialRtpPackets);

                //Find range info in the SDP
                var rangeInfo = md.RangeLine;

                //If there is a range directive
                if (rangeInfo == null)
                {
                    rangeInfo = sessionDescription.RangeLine;
                    if (rangeInfo != null)
                    {
                        string type;
                        Sdp.SessionDescription.TryParseRange(rangeInfo.Parts[0], out type, out tc.m_StartTime, out tc.m_EndTime);
                    }
                    //else if (sessionDescription.TimeDescriptions.Count > 0)
                    //{
                    //tc.MediaStartTime = TimeSpan.FromMilliseconds();
                    //tc.MediaEndTime = TimeSpan.FromMilliseconds();
                    //}
                }
                
                //Check for udp if no existing socket was given
                if (!hasSocket && string.Compare(md.MediaProtocol, Media.Rtp.RtpClient.RtpAvpProfileIdentifier, true) == 0)
                {
                    int localPort = Utility.FindOpenPort(ProtocolType.Udp);
                    tc.Initialize(localIp, remoteIp, localPort++, localPort++, rtpPort ?? md.MediaPort, rtcpPort ?? md.MediaPort + 1);
                }
                else if (hasSocket)//If had a socket use it
                {
                    tc.Initialize(existingSocket);
                }
                else
                {
                    tc.Initialize(localIp, remoteIp, rtpPort ?? md.MediaPort);
                }

                //Add the context
                participant.Add(tc);
            }

            //Return the participant
            return participant;
        }

        //Should come up with a better way to do this.

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
        public class TransportContext : Common.BaseDisposable, Common.ISocketReference
        {
            #region Statics

            public static TransportContext FromMediaDescription(Sdp.SessionDescription sessionDescription, byte dataChannel, byte controlChannel, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true, int remoteSsrc = 0, int minimumSequentialpackets = 2)
            {

                if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

                TransportContext tc = new TransportContext(dataChannel, controlChannel, RFC3550.Random32(Media.Rtcp.SourceDescriptionReport.PayloadType), mediaDescription, rtcpEnabled, remoteSsrc, minimumSequentialpackets);

                int reportReceivingEvery = rtcpEnabled ?  (int)DefaultReportInterval.TotalMilliseconds : 0, reportSendingEvery = rtcpEnabled ? (int)DefaultReportInterval.TotalMilliseconds : 0;

                if (rtcpEnabled)
                {
                    foreach (Media.Sdp.SessionDescriptionLine line in mediaDescription.BandwidthLines)
                    {
                        //Should be constant
                        if (line.Parts[0].StartsWith("RR"))
                        {
                            reportReceivingEvery = int.Parse(line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries)[1]);
                        }

                        if (line.Parts[0].StartsWith("RS"))
                        {
                            reportSendingEvery = int.Parse(line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries)[1]);
                        }

                        //if (line.Parts[0].StartsWith("AS"))
                        //{
                        //    applicationSpecific = int.Parse(line.Parts[0].Split(Colon, StringSplitOptions.RemoveEmptyEntries)[1]);
                        //}
                    }
                }

                bool rtcpDisabled = !rtcpEnabled && reportReceivingEvery + reportSendingEvery == 0;

                //If Rtcp is not disabled then this will set the read and write timeouts.
                if (!rtcpDisabled)
                {
                    tc.m_ReceiveInterval = TimeSpan.FromSeconds(reportReceivingEvery / Utility.MicrosecondsPerMillisecond);
                    tc.m_SendInterval = TimeSpan.FromSeconds(reportSendingEvery / Utility.MicrosecondsPerMillisecond);
                }

                //check for range in mediaDescription

                var rangeInfo = mediaDescription.RangeLine ?? (sessionDescription != null ? sessionDescription.RangeLine : null);

                if (rangeInfo != null)
                {
                    string type;
                    Media.Sdp.SessionDescription.TryParseRange(rangeInfo.Parts[0], out type, out tc.m_StartTime, out tc.m_EndTime);
                }

                return tc;
            }

            public static GoodbyeReport CreateGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null)
            {
                //Make a Goodbye, indicate version in Client, allow reason for leaving 
                //Todo add other parties where null with SourceList
                return new GoodbyeReport(context.Version, ssrc ?? (int)context.SynchronizationSourceIdentifier, reasonForLeaving);
            }

            /// <summary>
            /// Creates a <see cref="SendersReport"/> from the given context.
            /// Note, If empty is false and no previous <see cref="SendersReport"/> was sent then the report will be empty anyway.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="empty">Specifies if the report should have any report blocks if possible</param>
            /// <returns>The report created</returns>
            public static SendersReport CreateSendersReport(TransportContext context, bool empty)
            {
                //Create a SendersReport
                SendersReport result = new SendersReport(context.Version, false, 0, context.SynchronizationSourceIdentifier);

                //Use the values from the TransportChannel (Use .NtpTimestamp = 0 to Disable NTP)[Should allow for this to be disabled]
                result.NtpTimestamp = context.NtpTimestamp;

                //Note that in most cases this timestamp will not be equal to the RTP timestamp in any adjacent data packet.  Rather, it MUST be  calculated from the corresponding NTP timestamp using the relationship between the RTP timestamp counter and real time as maintained by periodically checking the wallclock time at a sampling instant.
                result.RtpTimestamp = context.RtpTimestamp; 

                //Counters
                result.SendersOctetCount = (int)context.RtpBytesSent;
                result.SendersPacketCount = (int)context.RtpPacketsSent;

                //Ensure there is a remote party
                empty = !(!empty && context.RemoteSynchronizationSourceIdentifier != null);

                //If source blocks are included include them and calculate their statistics
                if (!empty)
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
                        (int)((context.NtpTimestamp >> 16) << 32),
                        //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                        (int)context.LastRtcpReportSent.TotalSeconds / ushort.MaxValue));
                }
                
                return result;
            }

            /// <summary>
            /// Creates a <see cref="ReceiversReport"/> from the given context.
            /// </summary>
            /// <param name="context">The context</param>
            /// <param name="empty">Indicates if the report should be empty</param>
            /// <returns>The report created</returns>
            public static ReceiversReport CreateReceiversReport(TransportContext context, bool empty)
            {
                ReceiversReport result = new ReceiversReport(context.Version, false, 0, context.SynchronizationSourceIdentifier);

                empty = !(!empty && context.RemoteSynchronizationSourceIdentifier != null && context.TotalRtpPacketsReceieved > 0);

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
                        (int)context.RtpJitter >> 4,
                        (int)(context.SendersReport != null ? Utility.DateTimeToNptTimestamp32(context.SendersReport.NtpTime) : 0),
                        (context.SendersReport != null ? ((DateTime.UtcNow - context.SendersReport.Created).Seconds / ushort.MaxValue) * 1000 : 0)
                    ));

                }

                return result;
            }

            /// <summary>
            /// Creates a <see cref="SourceDescriptionReport"/> from the given context.
            /// If <paramref name="cName"/> is null then <see cref="SourceDescriptionItem.CName"/> will be used.
            /// </summary>
            /// <param name="context">The context</param>
            /// <param name="cName">The optional cName to use</param>
            /// <returns>The created report</returns>
            public static SourceDescriptionReport CreateSourceDescription(TransportContext context, Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem cName = null, IEnumerable<Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem> items = null)
            {
                //Todo, params context overload? overload with other Items
                return new SourceDescriptionReport(context.Version) 
                { 
                    new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk((int)context.SynchronizationSourceIdentifier, cName ?? Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.CName),
                    //items.SelectMany(i=> new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk((int)context.SynchronizationSourceIdentifier, i))
                };
            }

            #endregion

            #region Fields

            /// <summary>
            /// The version of packets which the TransportContents handles
            /// </summary>
            public readonly int Version = 2;

            /// <summary>
            /// The amount of <see cref="RtpPacket"/>'s which must be received before IsValid is true.
            /// </summary>
            public readonly int MinimumSequentialValidRtpPackets = DefaultMinimumSequentalRtpPackets;

            /// <summary>
            /// The channels which identity the TransportContext.
            /// </summary>
            public readonly byte DataChannel, ControlChannel;

            /// <summary>
            /// Indicates if Rtp is enabled on the TransportContext
            /// </summary>
            public bool IsRtpEnabled = true;

            /// <summary>
            /// Indicates if Rtcp will be used on this TransportContext
            /// </summary>
            public bool IsRtcpEnabled = true;

            //The EndPoints connected to (once connected don't need the Ports unless 0 is used to determine the port)
            internal protected EndPoint LocalRtp, LocalRtcp, RemoteRtp, RemoteRtcp;
            
            //bytes and packet counters
            internal long RtpBytesSent, RtpBytesRecieved,
                         RtcpBytesSent, RtcpBytesRecieved,
                         RtpPacketsSent, RtcpPacketsSent,
                         RtpPacketsReceived, RtcpPacketsReceieved;

            internal ushort m_SequenceNumber, RtpMaxSeq;//The highest Sequence recieved by the RtpClient

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

            internal TimeSpan m_SendInterval = DefaultReportInterval, m_ReceiveInterval = DefaultReportInterval, m_InactiveTime = TimeSpan.Zero,m_StartTime = TimeSpan.Zero, m_EndTime = Utility.InfiniteTimeSpan;

            //When packets are succesfully transferred the DateTime (utc) is copied in these variables and will reflect the point in time in which  the last 
            internal DateTime m_FirstPacketReceived, m_FirstPacketSent, m_LastRtcpIn, m_LastRtcpOut,  //Rtcp packets were received and sent
                m_LastRtpIn, m_LastRtpOut; //Rtp packets were received and sent

            /// <summary>
            /// Keeps track of any failures which occur when sending data.
            /// </summary>
            internal protected int m_FailedRtpTransmissions, m_FailedRtcpTransmissions;

            #endregion

            #region Properties

            //Any frames for this channel
            public RtpFrame CurrentFrame { get; internal protected set; }

            public RtpFrame LastFrame { get; internal protected set; }

            /// <summary>
            /// The socket used for Transport of Rtp and Interleaved data
            /// </summary>
            public Socket RtpSocket { get; internal protected set; }

            /// <summary>
            /// The socket used for Transport of Rtcp and Interleaved data
            /// </summary>
            public Socket RtcpSocket { get; internal protected set; }

            /// <summary>
            /// Indicates if the TransportContext has been connected.
            /// </summary>
            public bool Connected
            {
                get { return (LocalRtp != null || LocalRtcp != null) && (RemoteRtp != null || RemoteRtcp != null); }
            }

            /// <summary>
            /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the TransportContext) during reports
            /// </summary>
            public double MaximumRtcpBandwidthPercentage { get; set; }

            /// <summary>
            /// Indicates if the amount of bandwith currently utilized for Rtcp reporting has exceeded the amount of bandwidth allowed by the <see cref="MaximumRtcpBandwidthPercentage"/> property.
            /// </summary>
            public bool RtcpBandwidthExceeded
            {
                get
                {
                    if (Disposed || !IsRtcpEnabled) return true;

                    //If disposed no limit is imposed do not check
                    if (MaximumRtcpBandwidthPercentage == 0) return false;

                    long totalReceived = TotalBytesReceieved;

                    if (totalReceived == 0) return false;

                    long totalRtcp = TotalRtcpBytesSent + TotalRtcpBytesReceieved;

                    if (totalRtcp == 0) return false;

                    return totalRtcp >= totalReceived / MaximumRtcpBandwidthPercentage;
                }
            }

            /// <summary>
            /// The amount of time the TransportContext has been sending packets.
            /// </summary>
            public TimeSpan TimeSending
            {
                get
                {
                    return Disposed || m_FirstPacketSent == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_FirstPacketSent;
                }
            }

            /// <summary>
            /// The amount of time the TransportContext has been receiving packets.
            /// </summary>
            public TimeSpan TimeReceiving
            {
                get
                {
                    return Disposed || m_FirstPacketReceived == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_FirstPacketReceived;
                }
            }

            /// <summary>
            /// The time at which the media starts
            /// </summary>
            public TimeSpan MediaStartTime
            {
                get { return m_StartTime; }
                internal protected set { m_StartTime = value; }
            }

            /// <summary>
            /// The time at which the media ends
            /// </summary>
            public TimeSpan MediaEndTime
            {
                get { return m_EndTime; }
                internal protected set { m_EndTime = value; }
            }

            /// <summary>
            /// Indicates if the <see cref="MediaEndTime"/> is <see cref="Utility.InfiniteTimeSpan"/>. (Has no determined end time)
            /// </summary>
            public bool IsContinious { get { return m_EndTime == Utility.InfiniteTimeSpan; } }

            /// <summary>
            /// <see cref="Utility.InfiniteTimeSpan"/> if <see cref="IsContinious"/>,
            /// othewise the amount of time remaining in the media.
            /// </summary>
            public TimeSpan TimeRemaining { get { return IsContinious ? m_EndTime : TimeSpan.FromTicks(m_EndTime.Ticks - (Math.Max(TimeReceiving.Ticks, TimeSending.Ticks))); } }

            /// <summary>
            /// Allows getting or setting of the interval which occurs between data transmissions
            /// </summary>
            public TimeSpan SendInterval
            {
                get { return m_SendInterval; }
                set { m_SendInterval = value; }
            }

            /// <summary>
            /// Allows gettings or setting of the interval which occurs between data receptions
            /// </summary>
            public TimeSpan ReceiveInterval
            {
                get { return m_ReceiveInterval; }
                set { m_ReceiveInterval = value; }
            }

            /// <summary>
            /// Gets the time in which in TranportContext was last active for a send or receive operation
            /// </summary>
            public TimeSpan InactiveTime { get { return m_InactiveTime; } }

            /// <summary>
            /// Gets the time in which the last Rtcp reports were sent.
            /// </summary>
            public TimeSpan LastRtcpReportSent
            {
                get
                {
                    return m_LastRtcpOut == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtcpOut;
                }
            }

            /// <summary>
            /// Gets the time in which the last Rtcp reports were received.
            /// </summary>
            public TimeSpan LastRtcpReportReceived
            {
                get
                {
                    return m_LastRtcpIn == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtcpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was received.
            /// </summary>
            public TimeSpan LastRtpPacketReceived
            {
                get
                {
                    return m_LastRtpIn == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was transmitted.
            /// </summary>
            public TimeSpan LastRtpPacketSent
            {
                get
                {
                    return m_LastRtpOut == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - m_LastRtpOut;
                }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when sending RtcpPackets
            /// </summary>
            public int FailedRtcpTransmissions { get { return m_FailedRtcpTransmissions; } }

            /// <summary>
            /// Indicates the amount of times a failure has occured when senidng RtpPackets
            /// </summary>
            public int FailedRtpTransmissions { get { return m_FailedRtpTransmissions; } }

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
            public Sdp.MediaDescription MediaDescription { get; internal protected set; }

            /// <summary>
            /// Determines if the source has recieved at least <see cref="MinimumSequentialValidRtpPackets"/> RtpPackets
            /// </summary>
            public virtual bool IsValid { get { return RtpPacketsReceived >= MinimumSequentialValidRtpPackets; } }

            /// <summary>
            /// <c>false</c> if LocalRtp.Port NOT EQ LocalRtcp.Port
            /// </summary>
            public bool LocalMultiplexing { get { return Disposed ? false : LocalRtp.Equals(LocalRtcp); } }

            /// <summary>
            /// <c>false</c> if RemoteRtp.Port NOT EQ RemoteRtcp.Port
            /// </summary>
            public bool RemoteMultiplexing { get { return Disposed ? false : RemoteRtp.Equals(RemoteRtcp); } }
            
            /// <summary>
            /// <c>false</c> if NOT [RtpEnabled AND RtcpEnabled] AND [LocalMultiplexing OR RemoteMultiplexing]
            /// </summary>
            public bool Duplexing { get { try { return Disposed ?  false : (IsRtpEnabled && IsRtcpEnabled) && (LocalMultiplexing || RemoteMultiplexing); } catch { return false; } } }

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

            /// The last <see cref="GoodbyeReport"/> sent or received by this RtpClient.
            public GoodbyeReport Goodbye { get; internal set; }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) receieved
            /// </summary>
            public long TotalPacketsReceived { get { return RtpPacketsReceived + RtcpPacketsReceieved; } }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalPacketsSent { get { return RtpPacketsSent + RtcpPacketsSent; } }

            /// <summary>
            /// The total amount of RtpPackets sent
            /// </summary>
            public long TotalRtpPacketsSent { get { return Disposed ? 0 : RtpPacketsSent; } }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been sent.
            /// </summary>
            public long RtpPayloadBytesSent { get { return Disposed ? 0 : RtpBytesSent; } }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been received.
            /// </summary>
            public long RtpPayloadBytesRecieved { get { return Disposed ? 0 : RtpBytesRecieved; } }

            /// <summary>
            /// The total amount of bytes related to Rtp sent (including headers)
            /// </summary>
            public long TotalRtpBytesSent { get { return Disposed ? 0 : RtpBytesSent + RtpHeader.Length * RtpPacketsSent; } }

            /// <summary>
            /// The total amount of bytes related to Rtp received
            /// </summary>
            public long TotalRtpBytesReceieved { get { return Disposed ? 0 : RtpBytesRecieved + RtpHeader.Length * RtpPacketsSent; } }

            /// <summary>
            /// The total amount of RtpPackets received
            /// </summary>
            public long TotalRtpPacketsReceieved { get { return Disposed ? 0 : RtpPacketsReceived; } }

            /// <summary>
            /// The total amount of RtcpPackets recieved
            /// </summary>
            public long TotalRtcpPacketsSent { get { return Disposed ? 0 : RtcpPacketsSent; } }

            /// <summary>
            /// The total amount of sent bytes related to Rtcp 
            /// </summary>
            public long TotalRtcpBytesSent { get { return Disposed ? 0 : RtcpBytesSent; } }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) received
            /// </summary>
            public long TotalBytesReceieved { get { return Disposed ? 0 : TotalRtcpBytesReceieved + TotalRtpBytesReceieved; } }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalBytesSent { get { return Disposed ? 0 : TotalRtcpBytesSent + TotalRtpBytesSent; } }

            /// <summary>
            /// The total amount of RtcpPackets received
            /// </summary>
            public long TotalRtcpPacketsReceieved { get { return Disposed ? 0 : RtcpPacketsReceieved; } }

            /// <summary>
            /// The total amount of bytes related to Rtcp received
            /// </summary>
            public long TotalRtcpBytesReceieved { get { return Disposed ? 0 : RtcpBytesRecieved; } }

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
                if (dataChannel == controlChannel) throw new InvalidOperationException("dataChannel and controlChannel must be unique.");

                if (ssrc == senderSsrc) throw new InvalidOperationException("ssrc and senderSsrc must be unique.");

                if (minimumSequentialRtpPackets < 0) throw new InvalidOperationException("minimumSequentialRtpPackets must be >= 0");

                DataChannel = dataChannel;
                
                ControlChannel = controlChannel;
                
                SynchronizationSourceIdentifier = ssrc;
                
                IsRtcpEnabled = rtcpEnabled;

                //If 0 then all packets are answered
                RemoteSynchronizationSourceIdentifier = senderSsrc;

                //MinimumSequentialValidRtpPackets should be equal to 0 when RemoteSynchronizationSourceIdentifier is null I think, this essentially means respond to all inquiries.
                //A confrence may be able to contain this type of behavior better if required.
                MinimumSequentialValidRtpPackets = minimumSequentialRtpPackets;

                //Default bandwidth restriction
                MaximumRtcpBandwidthPercentage = DefaultReportInterval.TotalSeconds;
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

            ~TransportContext() { Dispose(); }

            #endregion

            #region Methods

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            public void UpdateJitterAndTimestamp(RtpPacket packet)
            {
                // RFC 3550 A.8.
                //Determine the time the last packet was sent or received
                TimeSpan arrivalDifference = (packet.Transferred.HasValue ? LastRtpPacketSent : LastRtpPacketReceived);

                //Calulcate the RtpJitter using the interarrival difference and set the RtpTransit
                RtpJitter += ((RtpTransit = (uint)arrivalDifference.TotalMilliseconds) - ((RtpJitter + 8) >> 4));

                //Update the RtpTimestamp on the Context
                RtpTimestamp = packet.Timestamp;

                //Update the NtpTimestamp on the Context.
                NtpTimestamp = (long)Utility.DateTimeToNptTimestamp(packet.Transferred ?? packet.Created);

                //Context is not inactive.
                m_InactiveTime = TimeSpan.Zero;
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
                //if (payloadLength == 0 && packet.PayloadType != 13) return false;
                //else if (packet.PayloadType == 13  || packet.PayloadType == 19) goto CheckSequenceNumber;

                if (packet.Header.IsCompressed || packet.PayloadType == 13) goto CheckSequenceNumber;

                // RFC 3550 A.1. Notes: Each TransportContext instance may be better suited to have a structure which defines this logic.

                //o  RTP version field must equal 2.

                if (packet.Version != Version) return false;

                //o  The payload type must be known, and in particular it must not be equal to SR or RR.

                if (packet.PayloadType == Rtcp.SendersReport.PayloadType || packet.PayloadType == Rtcp.ReceiversReport.PayloadType) return false;

                //o  If the P bit is set, Padding must be less than the total packet length minus the header size.
                if (packet.Padding && payloadLength > 0 && packet.PaddingOctets > payloadLength) return false;

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
                else if (udelta < DefaultMaxDropout)
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
                else if (udelta <= RTP_SEQ_MOD - DefaultMaxMisorder)
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
            /// Creates the required Udp sockets for the TransportContext and updates the assoicated Properties and Fields
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
            public void Initialize(IPAddress localIp, IPAddress remoteIp, int localRtpPort, int localRtcpPort, int remoteRtpPort = 0, int remoteRtcpPort = 0, bool punchHole = true)
            {
                Initialize(new IPEndPoint(localIp, localRtpPort), new IPEndPoint(remoteIp, remoteRtpPort), new IPEndPoint(localIp, localRtcpPort), new IPEndPoint(remoteIp, remoteRtcpPort), punchHole);
            }

            public void Initialize(IPEndPoint localRtp, IPEndPoint remoteRtp, IPEndPoint localRtcp, IPEndPoint remoteRtcp, bool punchHole = true)
            {
                if (Disposed || Connected) return;

                if (localRtp.Address.AddressFamily != remoteRtp.Address.AddressFamily) Common.ExceptionExtensions.CreateAndRaiseException<TransportContext>(this, "localIp and remoteIp AddressFamily must match.");
                else if (punchHole) punchHole = !Utility.IsOnIntranet(remoteRtp.Address); //Only punch a hole if the remoteIp is not on the LAN by default.
                
                //Erase previously set values on the TransportContext.
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                try
                {
                    //Setup the RtpSocket
                    RtpSocket = new Socket(localRtp.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    RtpSocket.Bind(LocalRtp = localRtp);
                    RtpSocket.Connect(RemoteRtp = remoteRtp);
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

                    //RtpSocket.ReceiveTimeout = RtpSocket.SendTimeout = DefaultReportInterval.Milliseconds;

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

                    //Check for Multicast
                    //if (localIp.IsMulticast())
                    //{
                    //    //JoinMulticastGroup
                    //}

                    if (punchHole)
                    {
                        //Send some bytes to ensure the result is open, if we get a SocketException the port is closed
                        //new RtpPacket(Version, false, false, false, MediaDescription.MediaFormat, SynchronizationSourceIdentifier, RemoteSynchronizationSourceIdentifier ?? 0, 0, 0, null);
                        try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtp); }
                        catch (SocketException) { } //We don't care about the response or any issues during the holePunch
                    }

                    //If Duplexing Rtp and Rtcp (on the same socket)
                    if (remoteRtp == remoteRtcp)
                    {
                        RtcpSocket = RtpSocket;
                    }
                    else if (IsRtcpEnabled)
                    {

                        //Setup the RtcpSocket
                        RtcpSocket = new Socket(localRtcp.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                        RtcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        RtcpSocket.Bind(LocalRtcp = localRtcp);
                        RtcpSocket.Connect(RemoteRtcp = remoteRtcp);
                        //RtcpSocket.SendBufferSize = RtcpSocket.ReceiveBufferSize = 0;
                        //RtcpSocket.Blocking = false;

                        //RtcpSocket.Ttl = 255;
                        //RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        //RtcpSocket.ReceiveTimeout = RtcpSocket.SendTimeout = DefaultReportInterval.Milliseconds;

                        //Tell the network stack what we send and receive has an order
                        RtcpSocket.DontFragment = true;
                        RtcpSocket.MulticastLoopback = false;

                        //RtcpSocket.UseOnlyOverlappedIO = true;

                        #region Optional Parameters

                        //Set max ttl for slower networks
                        RtcpSocket.Ttl = 255;

                        //May help if behind a router
                        //Allow Nat Traversal
                        //RtcpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

                        //Set type of service
                        //For older networks (http://en.wikipedia.org/wiki/Type_of_service)
                        //RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        #endregion

                        //Check for Multicast
                        //if (localIp.IsMulticast())
                        //{
                        //    //JoinMulticastGroup
                        //}

                        if (punchHole)
                        {
                            //new RtcpPacket(Version, Rtcp.ReceiversReport.PayloadType, 0, 0, SynchronizationSourceIdentifier, 0);
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
            /// Creates the required Tcp socket for the TransportContext and updates the assoicated Properties and Fields
            /// </summary>
            /// <param name="localIp"></param>
            /// <param name="remoteIp"></param>
            /// <param name="remotePort"></param>
            public void Initialize(IPAddress localIp, IPAddress remoteIp, int remotePort)
            {
                Initialize(new IPEndPoint(localIp, remotePort), new IPEndPoint(remoteIp, remotePort));
            }
            
            /// <summary>
            /// Creates a Tcp socket on from local to remote and sets the RtpSocket and RtcpSocket to that socket.
            /// </summary>
            /// <param name="local"></param>
            /// <param name="remote"></param>
            public void Initialize(IPEndPoint local, IPEndPoint remote)
            {
                if (Disposed || Connected) return;
                if (local.Address.AddressFamily != remote.Address.AddressFamily) Common.ExceptionExtensions.CreateAndRaiseException<TransportContext>(this, "localIp and remoteIp AddressFamily must match.");
                //Erase previously set values on the TransportContext.
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;
                try
                {
                    //Setup the RtcpSocket / RtpSocket
                    RtcpSocket = RtpSocket = new Socket(local.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    //RtpSocket.Bind(LocalRtp = LocalRtcp = local);
                    RtpSocket.Connect(RemoteRtp = RemoteRtcp = remote);
                    LocalRtp = LocalRtcp = RtpSocket.LocalEndPoint;
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
            public void Initialize(Socket duplexed)
            {
                Initialize(duplexed, duplexed);
            }

            /// <summary>
            /// Used to provide sockets which are already bound and connected for use in rtp and rtcp operations
            /// </summary>
            /// <param name="rtpSocket"></param>
            /// <param name="rtcpSocket"></param>
            public void Initialize(Socket rtpSocket, Socket rtcpSocket)
            {
                bool punchHole = !Utility.IsOnIntranet(((IPEndPoint)rtpSocket.RemoteEndPoint).Address); //Only punch a hole if the remoteIp is not on the LAN by default.
                RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                RtpSocket = rtpSocket;
                RtpSocket.DontFragment = true;

                RtcpSocket = rtcpSocket;
                RtcpSocket.DontFragment = true;

                LocalRtcp = RtcpSocket.LocalEndPoint;
                RemoteRtcp = RtcpSocket.RemoteEndPoint;

                LocalRtp = RtpSocket.LocalEndPoint;
                RemoteRtp = RtpSocket.RemoteEndPoint;

                if (punchHole)
                {
                    //new RtcpPacket(Version, Rtcp.ReceiversReport.PayloadType, 0, 0, SynchronizationSourceIdentifier, 0);
                    try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch

                    try { RtcpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch
                }

            }            

            /// <summary>
            /// Receives data on the given socket
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="socket"></param>
            /// <param name="remote"></param>
            /// <returns>The amount of bytes received</returns>
 
            /// <summary>
            /// Closes the Rtp and Rtcp Sockets
            /// </summary>
            public void DisconnectSockets()
            {
                if (!Connected || Disposed) return;

                //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                if (RtcpSocket != null && RtpSocket.Handle != RtcpSocket.Handle && RtcpSocket.Handle.ToInt64() > 0)
                        RtcpSocket.Close();

                //Close the RtpSocket
                if (RtpSocket != null && RtpSocket.Handle.ToInt64() > 0)
                    RtpSocket.Close();

                LocalRtp = LocalRtcp = RemoteRtp = RemoteRtcp = null;

                m_FirstPacketReceived = DateTime.MinValue;
                m_FirstPacketSent = DateTime.MinValue;
            }

            /// <summary>
            /// Removes references to any reports receieved and resets the validation counters to 0.
            /// </summary>
            internal void ResetState()
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

                Interlocked.Exchange(ref m_FailedRtcpTransmissions, 0);

                Interlocked.Exchange(ref m_FailedRtpTransmissions, 0);
            }

            /// <summary>
            /// Disposes the TransportContext and all underlying resources.
            /// </summary>
            public override void Dispose()
            {
                if (Disposed) return;

                Disposed = true;

                DisconnectSockets();
            }

            #endregion

            IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
            {
                if (Disposed) yield break;

                yield return RtpSocket;

                if (RtpSocket.ProtocolType == ProtocolType.Tcp || Duplexing) yield break;

                yield return RtcpSocket;
            }
        }

        #endregion

        #region Fields

        //Buffer for data
        //Used in ReceiveData, Each TransportContext gets a chance to receive into the buffer, when the recieve completes the data is parsed if there is any then the next TransportContext goes.
        //Doing this in parallel doesn't really offset much because the decoder must be able to handle the data and if you back log the decoder you are just wasting cycles.
        Common.MemorySegment m_Buffer;

        //Each session gets its own thread to send and recieve
        internal Thread m_WorkerThread;
        internal bool m_StopRequested;

        //Outgoing Packets, Not a Queue because you cant re-order a Queue and you can't take a range from the Queue
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Unless I missed something that damn HashSet is good for nothing except hashing.
        internal IList<TransportContext> TransportContexts = new List<TransportContext>();

        internal readonly Guid m_Id = Guid.NewGuid();

        #endregion

        #region Events

        public delegate void InterleaveHandler(object sender, byte[] data, int offset, int length);
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

                //SendReports();
                OnRtcpPacketReceieved(packet);

                return;
              
            }
            else if (!transportContext.IsRtcpEnabled) return;
            else if (transportContext.SynchronizationSourceIdentifier == packet.SynchronizationSourceIdentifier)
            {
                SendGoodbye(transportContext, Encoding.UTF8.GetBytes("ssrc"));
                transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(transportContext.SynchronizationSourceIdentifier);
                transportContext.ResetState();
            }

            //Get the payload type of the packet
            int payloadType = packet.PayloadType, partyId = packet.SynchronizationSourceIdentifier;

            //Make a copy of the packet now and only refer to this copy
            RtcpPacket localPacket = packet;

            //Complete the RtcpPacket if required.
            //while (!localPacket.IsComplete)
            //{
            //    //Complete the packet.
            //    int received = localPacket.CompleteFrom(transportContext.RtcpSocket, localPacket.Payload);
            //}

            //Last Rtcp packet was received right now now.
            transportContext.m_LastRtcpIn = packet.Created;

            //The context is active.
            transportContext.m_InactiveTime = TimeSpan.Zero;

            //Increment packets received for the valid context.
            Interlocked.Increment(ref transportContext.RtcpPacketsReceieved);

            //Keep track of the the bytes sent in the context
            Interlocked.Add(ref transportContext.RtcpBytesRecieved, localPacket.Length);

            //Set the time when the first rtcp packet was recieved
            if (transportContext.m_FirstPacketReceived == DateTime.MinValue) transportContext.m_FirstPacketReceived = packet.Created;

            //Fire event for the packet
            OnRtcpPacketReceieved(packet);

            //bool goodBye = packet.PayloadType == Rtcp.GoodbyeReport.PayloadType;

            ////If the context is valid, AND the remote identify has a value and the packet identity is not the same then reset the state and account for the new identity
            //if (transportContext.IsValid && transportContext.RemoteSynchronizationSourceIdentifier.HasValue && localPacket.SynchronizationSourceIdentifier != transportContext.RemoteSynchronizationSourceIdentifier)
            //{
            //    //Tell the source we are no longer listening to the old identity
            //    //SendGoodbye(transportContext);

            //    //Reset state for the counters
            //    //transportContext.ResetState();

            //    //Assign the new remote ID (EVENT?)
            //    transportContext.RemoteSynchronizationSourceIdentifier = localPacket.SynchronizationSourceIdentifier;

            //    //Send reports if we can unless this is a Goodbye
            //    /*if (!goodBye) */SendReports(transportContext);                
            //}

            //if (goodBye && packet.BlockCount > 0) transportContext.m_SendInterval = Utility.InfiniteTimeSpan; //Then never send reports again?
        }

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

            //Fire an event now to let subscribers know a packet has arrived
            OnRtpPacketReceieved(packet);

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
            else if (!transportContext.IsValid || (!transportContext.RemoteSynchronizationSourceIdentifier.HasValue || transportContext.RemoteSynchronizationSourceIdentifier.Value == 0))//&& transportContext.RemoteSynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier
            {
                transportContext.RemoteSynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
            }
            else if (transportContext.IsValid && (transportContext.RemoteSynchronizationSourceIdentifier.HasValue && transportContext.RemoteSynchronizationSourceIdentifier != 0 && packet.SynchronizationSourceIdentifier != transportContext.RemoteSynchronizationSourceIdentifier))
            {
                transportContext.ResetState();
                transportContext.RemoteSynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;
            }

            //Make a copy of the packet now and only reference this packet
            RtpPacket localPacket = packet;

            ////if the packet is not complete then complete it now
            //while (!localPacket.IsComplete)
            //{
            //    //Complete the packet
            //    int received = localPacket.CompleteFrom(transportContext.RtpSocket, localPacket.Payload);
            //}

            //Fire an event now to let subscribers know a packet has arrived @ the client from the socket and is realated to a relevent context.
            OnRtpPacketReceieved(localPacket);

            //If the packet is not valid then nothing further needs to be done as invalid count is maintained by the ValidatePacketAndUpdateSequenceNumber function.
            if (!transportContext.ValidatePacketAndUpdateSequenceNumber(localPacket)) return;

            //Increment RtpPacketsReceived for the context relating to the packet.
            Interlocked.Increment(ref transportContext.RtpPacketsReceived);

            //The counters for the bytes will now be be updated
            Interlocked.Add(ref transportContext.RtpBytesRecieved, localPacket.Payload.Count);

            //Set the time when the first RtpPacket was received if required
            if (transportContext.m_FirstPacketReceived == DateTime.MinValue) transportContext.m_FirstPacketReceived = packet.Created;

            //Update the SequenceNumber and Timestamp and calulcate Inter-Arrival (Mark the context as active)
            transportContext.UpdateJitterAndTimestamp(localPacket);

            //Set the last rtp in after inter-arrival has been calculated.
            transportContext.m_LastRtpIn = packet.Created;

            //If the instance does not handle frame changed events then return
            if (!FrameChangedEventsEnabled) return;

            //If we have not allocated a currentFrame
            if (transportContext.CurrentFrame == null)
            {
                //make a frame
                transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);
            }//Check to see if the frame belongs to the last frame
            else if (transportContext.LastFrame != null && packet.Timestamp == transportContext.LastFrame.Timestamp && localPacket.PayloadType == transportContext.MediaDescription.MediaFormat)
            {
                //Create a new packet from the localPacket so it will not be disposed when the packet is disposed.
                if (!transportContext.LastFrame.IsComplete) transportContext.LastFrame.Add(new RtpPacket(localPacket.Prepare().ToArray(), 0));

                //If the frame is complete then fire an event and make a new frame
                if (transportContext.LastFrame.IsComplete)
                {
                    //The LastFrame changed
                    OnRtpFrameChanged(transportContext.LastFrame);

                    //Remove
                    transportContext.LastFrame.Dispose();
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
                //Dispose the last frame if available
                if (transportContext.LastFrame != null)
                {
                    transportContext.LastFrame.Dispose();
                    transportContext.LastFrame = null;
                }


                //Move the current frame to the LastFrame
                transportContext.LastFrame = transportContext.CurrentFrame;

                //Make a new frame in the transportChannel's CurrentFrame
                transportContext.CurrentFrame = new RtpFrame(localPacket.PayloadType, localPacket.Timestamp, localPacket.SynchronizationSourceIdentifier);

                //The LastFrame changed
                OnRtpFrameChanged(transportContext.LastFrame);
            }

            //If the payload of the localPacket matched the media description then create a new packet from the localPacket so it will not be disposed when the packet is disposed.
            if (localPacket.PayloadType == transportContext.MediaDescription.MediaFormat) transportContext.CurrentFrame.Add(new RtpPacket(localPacket.Prepare().ToArray(), 0));

            //If the frame is complete then fire an event and make a new frame
            if (transportContext.CurrentFrame.IsComplete)
            {
                //Dispose the last frame if available
                if (transportContext.LastFrame != null)
                {
                    transportContext.LastFrame.Dispose();
                    transportContext.LastFrame = null;
                }

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

            //increment the counters (Only use the Payload.Count per the RFC) (new Erratta Submitted)
            Interlocked.Add(ref transportContext.RtpBytesSent, packet.Payload.Count);

            Interlocked.Increment(ref transportContext.RtpPacketsSent);

            //Sample the clock for when the last rtp packet was sent
            DateTime sent = packet.Transferred.Value;

            //Set the time the first packet was sent.
            if (transportContext.m_FirstPacketSent == DateTime.MinValue) transportContext.m_FirstPacketSent = sent;

            //If the packet was in sequence
            if (transportContext.UpdateSequenceNumber(packet.SequenceNumber))
            {
                //Calculate inter-arrival and mark the context as active
                transportContext.UpdateJitterAndTimestamp(packet);
            }

            //Store the time the last RtpPacket was sent.
            transportContext.m_LastRtpOut = sent;
        }

        protected internal virtual void HandleRtcpPacketSent(object sender, RtcpPacket packet)
        {

            if (Disposed || packet == null || !packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            //if there is no context there is nothing to do.
            if (transportContext == null) return;

            //Update the counters for the amount of bytes in the RtcpPacket including the header and any padding.
            Interlocked.Add(ref transportContext.RtcpBytesSent, packet.Length);

            //Update the amount of packets sent
            Interlocked.Increment(ref transportContext.RtcpPacketsSent);

            //Mark the context as active immediately.
            transportContext.m_InactiveTime = TimeSpan.Zero;

            //Get the time the packet was sent
            DateTime sent = packet.Transferred.Value;

            //Store the last time a RtcpPacket was sent
            transportContext.m_LastRtcpOut = sent;

            //Set the time the first packet was sent.
            if (transportContext.m_FirstPacketSent == DateTime.MinValue) transportContext.m_FirstPacketSent = sent;

            //Backoff based on ConverganceTime?
        }

        protected internal void OnInterleavedData(byte[] data, int offset, int length)
        {
            if (!Disposed && InterleavedData != null)
            {
                foreach (InterleaveHandler handler in InterleavedData.GetInvocationList())
                {
                    try
                    {
                        handler(this, data, offset, length);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            if (!Disposed && IncomingPacketEventsEnabled && RtpPacketReceieved != null)
            {
                foreach (RtpPacketHandler handler in RtpPacketReceieved.GetInvocationList())
                {
                    try
                    {
                        handler(this, packet);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {
            if (!Disposed && IncomingPacketEventsEnabled && RtcpPacketReceieved != null)
            {
                foreach (RtcpPacketHandler handler in RtcpPacketReceieved.GetInvocationList())
                {
                    try
                    {
                        handler(this, packet);
                    }
                    catch { }
                }
            }
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
            if (!Disposed && FrameChangedEventsEnabled && RtpFrameChanged != null)
            {
                foreach (RtpFrameHandler handler in RtpFrameChanged.GetInvocationList())
                {
                    try
                    {
                        handler(this, frame);
                        //new Thread(new ThreadStart(() =>
                        //{
                        //    handler(this, frame);
                        //})).Start();
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the RtpClient) during reports
        /// </summary>
        public double AverageMaximumRtcpBandwidthPercentage { get; set; }

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
        /// Gets or sets a value which prevents Incoming Rtp and Rtcp packet events from being handled
        /// </summary>
        public bool IncomingPacketEventsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value which prevents <see cref="RtpFrameChanged"/> from being fired.
        /// </summary>
        public bool FrameChangedEventsEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating if the RtpClient is not disposed and the WorkerThread is alive.
        /// </summary>
        public virtual bool Connected { get { return !Disposed && m_WorkerThread != null && m_WorkerThread.IsAlive; } }

        /// <summary>
        /// Gets a value which indicates if any underlying <see cref="RtpClient.TransportContext"/> owned by this RtpClient instance utilizes Rtcp.
        /// </summary>
        public bool RtcpEnabled { get { return TransportContexts.Any(c => c.IsRtcpEnabled); } }

        /// <summary>
        /// Gets a value which indicates if any underlying <see cref="RtpClient.TransportContext"/> owned by this RtpClient instance utilizes Rtcp.
        /// </summary>
        public bool RtpEnabled { get { return TransportContexts.Any(c => c.IsRtpEnabled); } }

        /// <summary>
        /// Indicates if the amount of bandwith currently utilized for Rtcp reporting has exceeded the amount of bandwidth allowed by the <see cref="AverageMaximumRtcpBandwidthPercentage"/> property.
        /// </summary>
        public bool AverageRtcpBandwidthExceeded
        {
            get
            {
                if (!RtcpEnabled || Disposed) return true;

                //If disposed no limit is imposed do not check
                if (AverageMaximumRtcpBandwidthPercentage == 0) return false;

                int amountOfContexts = TransportContexts.Count();

                if (amountOfContexts == 0) return true;

                //Obtain the summation of the total bytes sent over the amount of context's
                long totalReceived = TotalBytesReceieved;

                if (totalReceived == 0) return false;

                long totalRtcp = TotalRtcpBytesSent + TotalRtcpBytesReceieved;

                if (totalRtcp == 0) return false;   

                return totalRtcp >= totalReceived / AverageMaximumRtcpBandwidthPercentage;
                
            }
        }

        #region Bandwidth and Uptime and Counters

        /// <summary>
        /// The Date and Time the RtpClient was Connected
        /// </summary>
        public DateTime Started { get; private set; }

        /// <summary>
        /// The amount of time the RtpClient has been recieving media
        /// </summary>
        public TimeSpan Uptime { get { return DateTime.UtcNow - Started; } }

        /// <summary>
        /// The total amount of RtpPackets sent of all contained TransportContexts
        /// </summary>
        public long TotalRtpPacketsSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsSent); } }

        /// <summary>
        /// The total amount of Rtp bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalRtpBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.TotalRtpBytesSent); } }

        /// <summary>
        /// The total amount of Rtp bytes received of all contained TransportContexts
        /// </summary>
        public long TotalRtpBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.TotalRtpBytesReceieved); } }

        /// <summary>
        /// The total amount of Rtp packets received of all contained TransportContexts
        /// </summary>
        public long TotalRtpPacketsReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsReceived); } }

        /// <summary>
        /// The total amount of Rtcp packets sent of all contained TransportContexts
        /// </summary>
        public long TotalRtcpPacketsSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsSent); } }

        /// <summary>
        /// The total amount of Rtcp bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalRtcpBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesSent); } }

        /// <summary>
        /// The total amount of bytes received of all contained TransportContexts
        /// </summary>
        public long TotalBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.TotalBytesReceieved); } }

        /// <summary>
        /// The total amount of bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalBytesSent { get { return Disposed ? 0 : TransportContexts.Sum(c => c.TotalBytesSent); } }

        /// <summary>
        /// The total amount of Rtcp packets received of all contained TransportContexts
        /// </summary>
        public long TotalRtcpPacketsReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsReceieved); } }

        /// <summary>
        /// The total amount of Rtcp bytes received of all contained TransportContexts
        /// </summary>
        public long TotalRtcpBytesReceieved { get { return Disposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesRecieved); } }

        #endregion

        #endregion

        #region Constructor / Destructor        

        static RtpClient()
        {
            if (!UriParser.IsKnownScheme(RtpProtcolScheme)) UriParser.Register(new HttpStyleUriParser(), RtpProtcolScheme, 9670);
        }

        RtpClient()
        {
            AverageMaximumRtcpBandwidthPercentage = DefaultReportInterval.TotalSeconds;
        }

        /// <summary>
        /// Assigns the events necessary for operation and creates or assigns memory to use as well as inactivtyTimout.
        /// </summary>
        /// <param name="memory">The optional memory segment to use</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(Common.MemorySegment memory = null, bool incomingPacketEventsEnabled = true, bool frameChangedEventsEnabled = true)
            : this()
        {
            if (memory == null)
            {
                //Determine a good size based on the MTU (this should cover most applications)
                m_Buffer = new Common.MemorySegment(1500);
            }
            else
            {
                m_Buffer = memory;

                if (m_Buffer.Count < RtpHeader.Length) throw new ArgumentOutOfRangeException("memory", "memory.Count must contain enough space for a RtpHeader");
            }

            //RtpPacketReceieved += new RtpPacketHandler(HandleIncomingRtpPacket);
            //RtcpPacketReceieved += new RtcpPacketHandler(HandleIncomingRtcpPacket);
            RtpPacketSent += new RtpPacketHandler(HandleRtpPacketSent);
            RtcpPacketSent += new RtcpPacketHandler(HandleRtcpPacketSent);
            //InterleavedData += new InterleaveHandler(HandleInterleavedData);

            IncomingPacketEventsEnabled = incomingPacketEventsEnabled;

            FrameChangedEventsEnabled = frameChangedEventsEnabled;

        }

        /// <summary>
        /// Creates a RtpClient instance using the given array as a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use</param>
        /// <param name="offset">The offset to start using the buffer at</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(byte[] buffer, int offset = 0, bool incomingPacketEventsEnabled = true, bool frameChangedEventsEnabled = true) : this(new Common.MemorySegment(buffer, offset), incomingPacketEventsEnabled, frameChangedEventsEnabled) { }

        /// <summary>
        /// Creates a RtpClient instance using the given array as a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use</param>
        /// <param name="offset">The offset to start using the buffer at</param>
        /// <param name="count">The amount of bytes to use in the buffer</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(byte[] buffer, int offset, int count, bool incomingPacketEventsEnabled = true, bool frameChangedEventsEnabled = true) : this(new Common.MemorySegment(buffer, offset, count), incomingPacketEventsEnabled, frameChangedEventsEnabled) { }

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
                if (c.SynchronizationSourceIdentifier == context.SynchronizationSourceIdentifier) Common.ExceptionExtensions.CreateAndRaiseException<RtpClient>(this, "Requested SSRC is already in use");
            }

            TransportContexts.Add(context);
        }

        /// <summary>
        /// Removes the given <see cref="TransportContext"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual bool Remove(TransportContext context) { return TransportContexts.Remove(context); }

        /// <summary>
        /// Gets any <see cref="TransportContext"/> used by this instance.
        /// </summary>
        /// <returns>The <see cref="TransportContexts"/> used by this instance.</returns>
        public virtual IEnumerable<TransportContext> GetTransportContexts()
        {
            if (Disposed) return Enumerable.Empty<TransportContext>();
            try
            {
                return TransportContexts.DefaultIfEmpty();
            }
            catch (InvalidOperationException)
            {
                return GetTransportContexts();
            }
            catch { throw; }
        }

        #region Rtcp

        /// <summary>
        /// Sends any reports required for all owned TransportContexts using <see cref="SendReports"/>
        /// </summary>
        /// <returns>A value indicating if reports were immediately sent</returns>        
        public virtual bool SendReports()
        {
            if (m_StopRequested) return false;

            bool sentAny = false;

            foreach (TransportContext tc in TransportContexts)
            {
                if (!tc.Disposed && tc.IsRtcpEnabled && SendReports(tc))
                {
                    sentAny = true;
                }
            }

            return sentAny;
        }

        /// <summary>
        /// Sends a Goodbye to for all transportChannels, which will also stop the process sending or receiving after the Goodbye is sent
        /// //Needs SSRC
        /// </summary>
        public virtual void SendGoodbyes()
        {
            foreach (RtpClient.TransportContext tc in TransportContexts)
                SendGoodbye(tc);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected virtual int SendGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null)
        {

            if (Disposed || context.RtcpSocket.Handle.ToInt32() <= 0 || context.SynchronizationSourceIdentifier == 0 || context.TotalPacketsSent + context.TotalPacketsReceived == 0) return 0;

            //Make a Goodbye, indicate version in Client, allow reason for leaving 
            //Todo add other parties where null with SourceList
            GoodbyeReport goodBye = TransportContext.CreateGoodbye(context, reasonForLeaving, ssrc);

            IEnumerable<RtcpPacket> compound = goodBye.Yield();

            //If Rtp data was sent then send a Senders Report.
            if (context.RtpPacketsSent > 0)
            {
                //Insert the last SendersReport as the first compound packet
                compound = Enumerable.Concat((context.SendersReport = TransportContext.CreateSendersReport(context, false)).Yield(), compound);
            }

            //If Rtp data was received then send a Receivers Report.
            if (context.RtpPacketsReceived > 0)
            {
                //Insert the last ReceiversReport as the first compound packet
                compound = Enumerable.Concat((context.ReceiversReport = TransportContext.CreateReceiversReport(context, false)).Yield(), compound);
            }

            //Store the Goodbye in the context
            context.Goodbye = goodBye;

            //Send the packet
            return SendRtcpPackets(compound);            
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected virtual int SendSendersReport(TransportContext context)
        {
            if (context == null || !context.IsRtcpEnabled || context.Disposed) return 0;

            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            if (context.SynchronizationSourceIdentifier == 0)
            {
                //Generate the id per RFC3550
                context.SynchronizationSourceIdentifier = RFC3550.Random32(Rtcp.SendersReport.PayloadType);
            }

            //First report include no blocks (No last senders report), store the report being sent
            context.SendersReport = TransportContext.CreateSendersReport(context, false);

            //Always send compound with SourceDescription for now
            return SendRtcpPackets(context.SendersReport.Yield<RtcpPacket>().Concat((context.SourceDescription = TransportContext.CreateSourceDescription(context)).Yield()));
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
            if (context == null || !context.IsRtcpEnabled || context.Disposed || context.RtpBytesSent > 0) return 0;
            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            else if (context.SynchronizationSourceIdentifier == 0)
            {
                // Must be guaranteed to be unique per session
                context.SynchronizationSourceIdentifier = RFC3550.Random32((Rtcp.ReceiversReport.PayloadType));
            }

            //create and store the receivers report sent
            context.ReceiversReport = TransportContext.CreateReceiversReport(context, false);

            //If the bandwidth is not exceeded also send a sdes
            if (!AverageRtcpBandwidthExceeded && context.RemoteSynchronizationSourceIdentifier.HasValue && context.RemoteSynchronizationSourceIdentifier.Value != 0) return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>().Concat((context.SourceDescription = TransportContext.CreateSourceDescription(context)).Yield()));
            return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>());
        }

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
        public virtual int SendRtcpPackets(IEnumerable<RtcpPacket> packets)
        {

            if (Disposed || packets == null || packets.Count() == 0) return 0;

            TransportContext context = GetContextForPacket(packets.First());

            //If we don't have an transportChannel to send on or the transportChannel has not been identified or Rtcp is Disabled
            if (context == null || context.SynchronizationSourceIdentifier == 0 || !context.IsRtcpEnabled)
            {
                //Return
                return 0;
            }

            SocketError error;

            //Todo Determine from Context to use control channel and length. (Check MediaDescription)

            //When sending more then one packet compound packets must be padded correctly.

            //Don't Just `stack` the packets as indicated if sending, assume they are valid.
            int sent = SendData(RFC3550.ToCompoundBytes(packets).ToArray(), context.ControlChannel, context.RtcpSocket, context.RemoteRtcp, out error);

            //If the compound bytes were completely sent then all packets have been sent
            if (error == SocketError.Success)
            {
                //Check to see each packet which was sent
                int csent = 0;

                //Iterate each managed packet to determine if it was completely sent.
                foreach (RtcpPacket packet in packets)
                {
                    //Increment for the length of the packet
                    csent += packet.Length;

                    //If more data was contained then sent don't set Transferred and raise and event
                    if (csent > sent)
                    {
                        ++context.m_FailedRtcpTransmissions;
                        break;
                    }

                    //set sent
                    packet.Transferred = DateTime.UtcNow;

                    //Raise en event
                    OnRtcpPacketSent(packet);
                }
            }

            return sent;
        }

        /// <summary>
        /// Sends any <see cref="RtcpReport"/>'s immediately for the given <see cref="TransportContext"/> if <see cref="AverageRtcpBandwidthExceeded"/> is false.
        /// </summary>
        /// <param name="context">The <see cref="TransportContext"/> to send a report for</param>
        /// <returns>A value indicating if reports were sent</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal virtual bool SendReports(TransportContext context)
        {
            //Check for the stop signal (or disposal)
            if (m_StopRequested || Disposed ||  //Otherwise
                !context.IsRtcpEnabled
                || //Or Rtcp Bandwidth for this context or RtpClient has been exceeded
                context.RtcpBandwidthExceeded || AverageRtcpBandwidthExceeded) return false; //No reports can be sent.


            //keep track of the bytes sent in this call
            int bytesSent = 0;

            //Start with a sequence of empty packets
            IEnumerable<RtcpPacket> compound = Enumerable.Empty<RtcpPacket>();

            //If the last reports were sent in less time than alloted by the m_SendInterval
            if (context.LastRtcpReportSent == TimeSpan.Zero || context.LastRtcpReportSent > context.m_SendInterval)
            {
                //If Rtp data was sent then send a Senders Report.
                if (context.RtpPacketsSent > 0)
                {
                    //Insert the last SendersReport as the first compound packet
                    compound = Enumerable.Concat((context.SendersReport = TransportContext.CreateSendersReport(context, false)).Yield(), compound);
                }

                //If Rtp data was received then send a Receivers Report.
                if (context.RtpPacketsReceived > 0)
                {
                    //Insert the last ReceiversReport as the first compound packet
                    compound = Enumerable.Concat((context.ReceiversReport = TransportContext.CreateReceiversReport(context, false)).Yield(), compound);
                }

                //Include the SourceDescription
                if (compound.Any() && !context.RtcpBandwidthExceeded) compound = Enumerable.Concat(compound, (context.SourceDescription = TransportContext.CreateSourceDescription(context)).Yield());

                //Send all reports as compound
                bytesSent = SendRtcpPackets(compound);                               
            }
            
            //Indicate if reports were sent in this interval
            return bytesSent > 0;
        }

        /// <summary>
        /// Sends a RtcpGoodbye Immediately if the given context:
        /// <see cref="IsRtcpEnabled"/>  and the context has not received a RtcpPacket during the last <see cref="ReceiveInterval"/>.
        /// OR
        /// <see cref="IsRtpEnabled"/> and the context <see cref="IsContinious"/> but <see cref="Uptime"/> is > the <see cref="MediaEndTime"/>
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
            if (context.Goodbye == null 
                && 
                (context.IsRtcpEnabled && context.LastRtcpReportReceived > context.m_ReceiveInterval) || (context.IsRtpEnabled && !context.IsContinious && Uptime > context.MediaEndTime))
            {
                //Set the amount of time inactive
                context.m_InactiveTime = DateTime.UtcNow - lastActivity;

                //Determine if the context is not inactive too long
                if (context.m_InactiveTime > context.m_ReceiveInterval + context.m_SendInterval)
                {
                    //send a goodbye
                    SendGoodbye(context);                    

                    //mark inactive
                    inactive = true;

                    //Disable further service
                    context.IsRtpEnabled = context.IsRtcpEnabled = false;
                }
            }

            //indicate a goodbye was sent and a context is now inactive.
            return inactive;
        }

        #endregion

        #region Rtp
     
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void SendRtpFrame(RtpFrame frame) { if (frame == null || frame.Disposed) return; foreach (RtpPacket packet in frame) SendRtpPacket(packet); }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public int SendRtpPacket(RtpPacket packet, int? ssrc = null)
        {
            if (packet == null || m_StopRequested) return 0;

            TransportContext transportContext = GetContextForPacket(packet);

            //If we don't have an transportChannel to send on or the transportChannel has not been identified
            if (transportContext == null) return 0;
            else if (transportContext.MediaDescription.MediaFormat != packet.PayloadType && transportContext.SynchronizationSourceIdentifier != packet.SynchronizationSourceIdentifier)
            {
                //Throw an exception if the payload type does not match
                Common.ExceptionExtensions.CreateAndRaiseException<RtpClient>(this, "Packet Payload is different then the expected MediaDescription. Expected: '" + transportContext.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }

            //How many bytes were sent
            int sent = 0;

            //Send a SendersReport before any data is sent.
            if (transportContext.SendersReport == null && transportContext.IsRtcpEnabled) SendSendersReport(transportContext);

            //The error encountered in the senddata operation as given by the send method of the socket used.
            SocketError error;

            //If the transportContext is changed to automatically update the timestamp by frequency then use transportContext.RtpTimestamp
            sent += SendData(packet.Prepare(null, ssrc, null, null).ToArray(), transportContext.DataChannel, transportContext.RtpSocket, transportContext.RemoteRtp, out error);
            
            if (error == SocketError.Success && sent >= packet.Length)
            {
                packet.Transferred = DateTime.UtcNow;

                OnRtpPacketSent(packet);
            }
            else
            {
                ++transportContext.m_FailedRtpTransmissions;
            }

            return sent;
        }

        #endregion

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

                //Ensure buffer is sized
                Media.Common.ISocketReferenceExtensions.SetReceiveBufferSize(((Media.Common.ISocketReference)this), m_Buffer.Count * m_Buffer.Count);

                m_WorkerThread.TrySetApartmentState(ApartmentState.MTA);
                m_WorkerThread.Priority = ThreadPriority.AboveNormal;
                //m_WorkerThread.IsBackground = true;
                m_WorkerThread.Name = "RtpClient-" + m_Id;
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
        /// Sends the Rtcp Goodbye and signals a stop in the worker thread.
        /// </summary>
        public void Disconnect()
        {
            if (Disposed || !Connected) return;

            SendGoodbyes();

            m_StopRequested = true;

            foreach (var tc in TransportContexts) if(tc.Connected) tc.DisconnectSockets();

            Utility.Abort(ref m_WorkerThread);
        }

        /// <summary>
        /// Returns the amount of bytes read to completely read the RFC2326 frame.
        /// Where a negitive return value indicates no more data remains.
        /// </summary>
        /// <param name="received"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        //http://tools.ietf.org/search/rfc4571
        //Should be ReadFrameHeader (bool rtsp, ...
        int ReadRFC2326FrameHeader(int received, out byte frameChannel, out RtpClient.TransportContext context, ref int offset, byte[] buffer = null)
        {

            //There is no relevant TransportContext assoicated yet.
            context = null;

            //The channel of the frame - The Framing Method
            frameChannel = default(byte);

            if (received <= 0) return -1;

            buffer = buffer ?? m_Buffer.Array;

            int bufferLength = buffer.Length;

            //Look for the frame control octet
            int mOffset = offset, startOfFrame = Array.IndexOf<byte>(buffer, BigEndianFrameControl, mOffset, received);

            int frameLength = 0;

            //If not found everything belongs to the upper layer
            if (startOfFrame == -1)
            {
                //System.Diagnostics.Debug.WriteLine("Interleaving: " + received);
                OnInterleavedData(buffer, mOffset, received);

                //Indicate no more data in buffer
                return received;
            }
            else if (startOfFrame > offset) // If the start of the frame is not at the beginning of the buffer
            {
                //Determine the amount of data which belongs to the upper layer
                int upperLayerData = startOfFrame - mOffset;

                //System.Diagnostics.Debug.WriteLine("Moved To = " + startOfFrame + " Of = " + received + " - Bytes = " + upperLayerData + " = " + Encoding.ASCII.GetString(m_Buffer, mOffset, startOfFrame - mOffset));                

                OnInterleavedData(buffer, mOffset, upperLayerData);

                //Indicate length from offset until next possible frame. (should always be positive, if somehow -1 is returned this will signal a end of buffer to callers)

                //If there is more data related to upperLayerData it will be evented in the next run. (See RtspClient ProcessInterleaveData notes)
                return upperLayerData;
            }

            //If there is not enough data for a frame header return
            if (mOffset + InterleavedOverhead > bufferLength) return -1;

            //Todo Determine from Context to use control channel and length. (Check MediaDescription)
            //NEEDS TO HANDLE CASES WHERE RFC4571 Framing are in play and no $ or Channel are used....

            //The amount of data needed for the frame
            frameLength = TryReadFrameHeader(buffer, mOffset, out frameChannel);

            //Assign a context if there is a frame of any size
            if (frameLength >= 0) context = GetContextByChannel(frameChannel);

            //Return the amount of bytes including the frame header
            return frameLength + InterleavedOverhead;
        }

        /// <summary>
        /// Sends the given data on the socket
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        /// <param name="socket"></param>
        /// <param name="remote"></param>
        /// <param name="error"></param>
        /// <param name="useFrameControl"></param>
        /// <param name="useChannelId"></param>
        /// <returns></returns>
        /// //Needs offset and count?
        internal protected virtual int SendData(byte[] data, byte? channel, Socket socket, System.Net.EndPoint remote, out SocketError error, bool useFrameControl = true, bool useChannelId = true)
        {
            error = SocketError.SocketError;
            //Check there is valid data and a socket which is able to write and that the RtpClient is not stopping
            if (Disposed || data == null || socket == null || socket.Handle.ToInt64() <= 0) return 0;
            try
            {
                int sent = 0, length = 0;
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
                        length += InterleavedOverhead;
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
                        int skip = 2;
                        if (useFrameControl) ++skip;
                        if (useChannelId) ++skip;
                        data = data.Skip(skip).ToArray();
                        goto Frame;
                    }
                    else length = data.Length;
                }

                //Send the frame keeping track of the bytes sent
                while (sent < length) 
                    sent += socket.SendTo(data, sent, length - sent, SocketFlags.None, remote);
            
                error = SocketError.Success;

                return sent;

            }
            catch (SocketException ex)
            {
                error = (SocketError)ex.ErrorCode;

                return -1;
            }
            catch
            {
                //Something bad happened, usually disposed already
                return -1;
            }
        }

        /// <summary>
        /// Recieves data on a given socket and endpoint
        /// </summary>
        /// <param name="socket">The socket to receive data on</param>
        /// <returns>The number of bytes recieved</returns>             
        internal protected virtual int ReceiveData(Socket socket, ref EndPoint remote, bool expectRtp = true, bool expectRtcp = true)
        {
            //Ensure the socket can poll
            if (Disposed || m_StopRequested || socket == null || socket.Handle.ToInt64() <= 0 || m_Buffer.Disposed) return 0;

            //There is no error yet
            SocketError error = SocketError.SocketError;

            bool tcp = socket.ProtocolType == ProtocolType.Tcp;

            //Cache the offset at the time of the call
            int offset = m_Buffer.Offset, received = 0;

            EndPoint recievedFrom = remote;

            IPEndPoint remoteIpEndPoint = (IPEndPoint) recievedFrom;

            try
            {
                received = socket.ReceiveFrom(m_Buffer.Array, offset, m_Buffer.Count, SocketFlags.None, ref recievedFrom);

                //If the receive was a success
                if (received > 0)
                {

                    //Under TCP use Framing to obtain the length of the packet as well as the context.
                    if (tcp)  return ProcessFrameData(m_Buffer.Array, offset, received, socket);

                    //When port 0 is used the port could change
                    if (remoteIpEndPoint.Port == 0)
                    {
                        //Set the IpEndPoint when in discovery mode to the port recieved from.
                        //remoteIpEndPoint = (IPEndPoint)recievedFrom;
                        //Update the transport context values...
                        remote = recievedFrom;
                    }

                    //Use the data received to parse and complete any recieved packets, should take a parseState
                    using (var memory = new Common.MemorySegment(m_Buffer.Array, offset, received)) ParseAndCompleteData(memory, expectRtcp, expectRtp);
                }

            }
            catch (SocketException se)
            {
                error = (SocketError)se.ErrorCode;
                return -1;
            }

            //Return the amount of bytes received from this operation
            return received;
        }

        /// <summary>
        /// Used to handle Tcp framing
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        internal protected virtual int ProcessFrameData(byte[] buffer, int offset, int count, Socket socket)
        {
            //If there is no buffer use our own buffer.
            if (buffer == null) buffer = m_Buffer.Array;

            //Get the length of the given buffer (Should actually use m_Buffer.Count when using our own buffer)
            int bufferLength = buffer.Length;

            //Determine which TransportContext will receive the data incoming
            TransportContext relevent = null;

            //The channel of the data
            byte frameChannel = 0;

            //The indicates length of the data
            int frameLength = 0;

            //The amount of data remaining in the buffer
            int remainingInBuffer = count, 
                //The amount of data received (which is already equal to what is remaining in the buffer)
                recievedTotal = remainingInBuffer;

            //Determine if Rtp or Rtcp is coming in.
            bool expectRtp, expectRtcp;

            //Handle receiving when no $ and Channel is presenent... e.g. RFC4751
            while (!Disposed && offset < bufferLength && remainingInBuffer > 0)
            {
                //If a header can be read
                if (remainingInBuffer >= InterleavedOverhead)
                {
                    //Parse the frameLength from the given buffer, take changes to the offset through the function.
                    frameLength = ReadRFC2326FrameHeader(remainingInBuffer, out frameChannel, out relevent, ref offset, buffer);

                    //If the NULL Packet was not found.
                    if (frameLength > 0)
                    {
                        //In some cases it appears that some implementations use a context which is not assigned to create "space" in the stream.
                        //This may happen if Bandwith or Blocksize directives are used from the Client to Server or if the Server is trying to limit bandwith to the client.
                        //The spec is clear that a Binary Frame Header which corresponds to a context which is not available must be skipped.

                        //If the frame length exceeds the buffer capacity then just attempt to complete it.

                        //If the frameLength is greater than Available on the socket (FIONREAD)
                        //Then the data MAY HAVE been incorrectly determined to be an interleaved frame
                        //remainingInBuffer + socket.Available

                        //In the end this doesn't matter because the Extension on the header could be set 
                        //Padding could be set etc
                        //This WILL cause InComplete to be false reguardless if the frame was totally receieved or not.

                        //e.g a RtpPacket with an Extension that has a Size > the frameLength recieved here.

                        // For example the header indicates channel 0 with a length of 65535
                        // The packet indicates 15 CSRC (60 bytes) , 65535 Words in the Extension Length (262140 bytes) Padding bit set.
                        // The packet must be 12 + 60 + 262140 bytes long + Padding.
                        // The frame indicates only 65535 bytes are available.....

                        //The same applies for RtcpPackets with a larger LengthInWords then indicates in the frame.

                        //NO MATTER WHAT if a context is found we shall ensure that the Version and Payload type matches.
                        //If the next frame header didn't match doesnt matter
                        if (relevent != null && frameLength > 0 && offset + frameLength < bufferLength && frameLength <= remainingInBuffer) //&& buffer[offset + frameLength] != BigEndianFrameControl 
                        {
                            using (var common = new Media.RFC3550.CommonHeaderBits(buffer[offset + InterleavedOverhead], buffer[offset + InterleavedOverhead + 1]))
                            {
                                //Check the version...
                                bool bad = common.Version != relevent.Version;

                                //Check the RtpPayloadType (AND SSRC) or SSRC, but they could change mid stream...
                                if (!bad) bad = frameChannel == relevent.DataChannel && (GetContextByPayloadType(common.RtpPayloadType) != relevent && GetContextBySourceId(Common.Binary.Read32(buffer, offset + InterleavedOverhead + 10, BitConverter.IsLittleEndian)) != relevent)
                                    || //If we have receieved the requried amount of RtpPackets or more there must be a Remote SSRC
                                    relevent.TotalRtpPacketsReceieved >= relevent.MinimumSequentialValidRtpPackets ? frameChannel == relevent.ControlChannel && GetContextBySourceId(Common.Binary.Read32(buffer, offset + 8, BitConverter.IsLittleEndian)) != relevent : false;

                                if (bad) frameLength = bufferLength + 1;
                            }
                        }

                        //Check for a length more then we can store. (atleast 1456 or 1500) e.g. frameLength >1456 && 
                        if (frameLength > bufferLength) //  && relevent != null || frameLength > 0 && frameLength < remainingInBuffer && buffer[offset + frameLength] != BigEndianFrameControl && char.IsLetter((char)buffer[offset + frameLength]))
                        {
                            //Indicate a header for a large frame was received.
                            OnInterleavedData(buffer, offset, InterleavedOverhead);

                            //Move the offset into the data section
                            offset += InterleavedOverhead;
                            remainingInBuffer -= InterleavedOverhead;

                            //Do another pass
                            continue;
                        }
                    }
                }
                else
                {
                    frameLength = -1;
                    relevent = null;
                }

                //See how many more bytes are required from the wire
                int remainingOnSocket = frameLength < 0 && remainingInBuffer < InterleavedOverhead ? InterleavedOverhead - remainingInBuffer 
                    : frameLength > remainingInBuffer ? frameLength - remainingInBuffer : 0;

            GetRemainingData:
                //If there is anymore data remaining on the wire
                if (remainingOnSocket > 0)
                {
                    //Align the buffer if anything remains on the socket.
                    Array.Copy(buffer, offset, buffer, m_Buffer.Offset, remainingInBuffer);

                    //Set the correct offset either way.
                    offset = m_Buffer.Offset + remainingInBuffer;

                    //Store the error if any
                    SocketError error = SocketError.SocketError;

                    //Get all the remaining data
                    while (!Disposed && remainingOnSocket > 0)
                    {
                        //Recieve from the wire the amount of bytes required (up to the length of the buffer)
                        int recievedFromWire = socket == null ? 0 : Utility.AlignedReceive(buffer, offset, remainingOnSocket, socket, out error);
                        
                        //Check for an error and then the allowed continue condition
                        if (error != SocketError.Success && error != SocketError.TryAgain) break;
                        
                        //If nothing was recieved try again.
                        if (recievedFromWire <= 0) continue;
                        
                        //Decrease what is remaining from the wire by what was received
                        remainingOnSocket -= recievedFromWire;
                        
                        //Move the offset
                        offset += recievedFromWire;

                        //Increment received
                        recievedTotal += recievedFromWire;

                        //Incrment remaining in buffer for what was recieved.
                        remainingInBuffer += recievedFromWire;                        
                    }
                    
                    //If a socket error occured remove the context so no parsing occurs
                    if (error != SocketError.Success) return recievedTotal;
                    
                    //Move back to where the frame started
                    offset -= remainingInBuffer;

                    //Do another pass if we haven't parsed a frame header.
                    if(relevent == null) continue;
                }

                //If there any data in the frame and there is a relevent context
                if (!Disposed && frameLength > 0)
                {
                    if (relevent != null)
                    {
                        //Determine if Rtp or Rtcp should be parsed
                        expectRtp = !(expectRtcp = relevent.IsRtcpEnabled && frameChannel == relevent.ControlChannel);

                        //Parse the data in the buffer
                        using (var memory = new Common.MemorySegment(buffer, offset + InterleavedOverhead, frameLength - InterleavedOverhead)) ParseAndCompleteData(memory, expectRtcp, expectRtp, memory.Count);
                    }

                    //Decrease remaining in buffer
                    remainingInBuffer -= frameLength;

                    //Move the offset
                    offset += frameLength;
                }

                //Ensure large frames are completely received by receiving the rest of the frame now. (this only occurs for packets being skipped)
                if (frameLength > bufferLength)
                {
                    remainingOnSocket = frameLength - bufferLength;
                    frameLength -= bufferLength;
                    remainingInBuffer = 0;
                    if (frameLength > 0) goto GetRemainingData;
                }
            }
            
            return recievedTotal;
        }

        /// <summary>
        /// Parses the data in the buffer for valid Rtcp and Rtcp packet instances.
        /// </summary>
        /// <param name="memory">The memory to parse</param>
        /// <param name="from">The socket which received the data into memory and may be used for packet completion.</param>
        internal protected virtual void ParseAndCompleteData(Common.MemorySegment memory, bool parseRtcp = true, bool parseRtp = true, int? remaining = null)
        {

            //handle demultiplex scenarios e.g. RFC5761
            if (parseRtcp == parseRtp)
            {
                //Double Negitive, Demux based on PayloadType? RFC5761?

                //Distinguishable RTP and RTCP Packets
                //http://tools.ietf.org/search/rfc5761#section-4

                //Observation 1) Rtp packets can only have a PayloadType from 64-95
                //However Rtcp Packets may also use PayloadTypes 72- 76.. (Reduced size...)

                //Observation 2) Rtcp Packets defined in RFC3550 Start at 200 (SR -> Goodbye) 204,
                // 209 - 223 is cited in the above as well as below
                //RTCP packet types in the ranges 1-191 and 224-254 SHOULD only be used when other values have been exhausted.

                using (Media.RFC3550.CommonHeaderBits header = new Media.RFC3550.CommonHeaderBits(memory))
                {
                    //Just use the payload type to avoid confusion, payload types cannot and should not overlap
                    parseRtcp = !(parseRtcp = GetContextByPayloadType(header.RtpPayloadType) != null);
                }
            }

            //Cache start, count and index
            int offset = memory.Offset, count = memory.Count, index = 0,
                //Calulcate remaining
            mRemaining = remaining ?? count - index;

            //If there is nothing left to parse then return
            if (count <= 0) return;

            //If rtcp should be parsed
            if (mRemaining >= RtcpHeader.Length && parseRtcp)
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
            if (mRemaining >= RtpHeader.Length && parseRtp)
            {
                using (var subMemory = new Common.MemorySegment(memory.Array, offset + index, mRemaining))
                {
                    using (RtpPacket rtp = new RtpPacket(subMemory))
                    {
                        //Raise the event
                        HandleIncomingRtpPacket(this, rtp);

                        //Move the index past the length of the packet
                        index += rtp.Length;

                        //Calculate the amount of octets remaining in the segment.
                        mRemaining -= rtp.Length;
                    }
                }
            }

            return;
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

                    int ContextCount = TransportContexts.Count;

                    for (int i = 0; i < ContextCount; ++i)
                    {
                        TransportContext tc = TransportContexts[i];

                        //Check for a context which is able to receive data
                        if (tc == null || tc.Disposed || !tc.Connected
                            ||//If the context does not have continious media it must only receive data for the duration of the media.
                            !tc.IsContinious && tc.TimeRemaining < TimeSpan.Zero) continue;

                        //Receive Data on the RtpSocket and RtcpSocket, summize the amount of bytes received from each socket.

                        int receivedRtp = 0, receivedRtcp = 0;

                        bool duplexing = tc.Duplexing, rtpEnabled = tc.IsRtpEnabled, rtcpEnabled = tc.IsRtcpEnabled;

                        //If receiving Rtp and the socket is able to read
                        if (rtpEnabled
                            //Check if the socket can read data
                            && tc.RtpSocket.Poll((int)Math.Round(tc.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                        {
                            //Receive RtpData
                            receivedRtp += ReceiveData(tc.RtpSocket, ref tc.RemoteRtp, rtpEnabled, duplexing);
                            if (receivedRtp > 0) lastOperation = DateTime.UtcNow;
                        }

                        //if Rtcp is enabled
                        if (rtcpEnabled)
                        {
                            if (//The last report was never received or recieved longer ago then required
                                (tc.LastRtcpReportReceived == TimeSpan.Zero || tc.LastRtcpReportReceived >= tc.m_ReceiveInterval)
                                &&//And the socket can read
                                tc.RtcpSocket.Poll((int)Math.Round(tc.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                            {
                                //ReceiveRtcp Data
                                receivedRtcp += ReceiveData(tc.RtcpSocket, ref tc.RemoteRtcp, duplexing, rtcpEnabled);
                                if (receivedRtcp > 0) lastOperation = DateTime.UtcNow;
                            }

                            //Try to send reports for the latest packets or continue if inactive
                            if (SendReports(tc)) lastOperation = DateTime.UtcNow;
                            else if (SendGoodbyeIfInactive(lastOperation, tc)) continue;//Don't throw for Goodbye
                        }
                    }

                    //Check for packets going out
                    if (m_OutgoingRtcpPackets.Count + m_OutgoingRtpPackets.Count == 0)
                    {
                        //Should also check for bit rate before sleeping
                        System.Threading.Thread.Sleep(TransportContexts.Count);
                        goto Begin;
                    }

                    #endregion

                    #region Handle Outgoing RtcpPackets

                    int remove = m_OutgoingRtcpPackets.Count;

                    if (m_OutgoingRtcpPackets.Count > 0)
                    {
                        //Try and send the lot of them
                        if (SendRtcpPackets(m_OutgoingRtcpPackets.Take(remove)) > 0) lastOperation = DateTime.UtcNow;

                        //Remove what was attempted to be sent (don't try to send again)
                        m_OutgoingRtcpPackets.RemoveRange(0, remove);
                    }

                    #endregion

                    #region Handle Outgoing RtpPackets

                    if (m_OutgoingRtpPackets.Count > 0)
                    {
                        //Could check for timestamp more recent then packet at 0  on transporContext and discard...
                        //Send only A few at a time to share with rtcp
                        remove = 0;

                        //int? lastTimestamp;

                        //Take the array to reduce exceptions
                        for (int i = 0, e = m_OutgoingRtpPackets.Count; i < e; ++i)
                        {
                            //Get a packet
                            RtpPacket packet = m_OutgoingRtpPackets[i];

                            //Get the context for the packet
                            TransportContext sendContext = GetContextForPacket(packet);

                            //Don't send packets which are disposed but do remove them
                            if (packet == null || packet.Disposed || sendContext == null)
                            {
                                ++remove;
                                continue;
                            }
                            else if (SendRtpPacket(packet, sendContext.SynchronizationSourceIdentifier) >= packet.Length)
                            {
                                ++remove;
                                lastOperation = DateTime.UtcNow;
                            }
                            else break;

                            //If this was a marker packet then stop for now
                            if (packet.Marker) break;

                            //Could also check timestamp in cases where marker is not being set
                            //if (lastTimestamp.HasValue && packet.Timestamp != lastTimestamp) break;
                            //lastTimestamp = packet.Timestamp;
                        }

                        if(remove > 0) m_OutgoingRtpPackets.RemoveRange(0, remove);
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                m_StopRequested = ex is ThreadAbortException;
                if (!m_StopRequested) goto Begin;
            }
        }

        #endregion

        public override void Dispose()
        {

            if (Disposed) return;

            Disconnect();

            base.Dispose();

            //Dispose contexts
            foreach (TransportContext tc in TransportContexts) tc.Dispose();
            
            //Counters go away with the transportChannels
            TransportContexts.Clear();

            RtpPacketSent -= new RtpPacketHandler(HandleRtpPacketSent);
            RtcpPacketSent -= new RtcpPacketHandler(HandleRtcpPacketSent);

            RtpPacketSent = null;
            RtcpPacketSent = null;
            RtpPacketReceieved = null;
            RtcpPacketReceieved = null;
            InterleavedData = null;

            Utility.Abort(ref m_WorkerThread);

            m_Buffer.Dispose();
            m_Buffer = null;

            //Empty buffers
            m_OutgoingRtpPackets.Clear();
            m_OutgoingRtcpPackets.Clear();

            DisableFeedbackReports(this);
        }

        IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads() { return Disposed ? null : Utility.Yield(m_WorkerThread); }

        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            if (Disposed) yield break;

            foreach (TransportContext tc in TransportContexts)
            {
                if (tc.Disposed) continue;
                foreach (Socket referenced in ((Common.ISocketReference)tc).GetReferencedSockets())
                {
                    yield return referenced;
                }
            }
        }
    }
}
