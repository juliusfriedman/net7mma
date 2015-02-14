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
        internal static int TryReadFrameHeader(byte[] buffer, int offset, out byte channel, bool readFrameByte = true, byte frameByte = BigEndianFrameControl, bool readChannel = true)
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
            if (readChannel) channel = buffer[offset + 1];

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
                participant.TryAddContext(tc);
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

            #region RFC3556 Bandwidth

            const string RecieveBandwidthToken = "RR", SendBandwdithToken = "RS", ApplicationSpecificBandwidthToken = "AS";

            internal static Sdp.SessionDescriptionLine DisabledReceiveLine = new Sdp.SessionDescriptionLine("b=RR:0");

            internal static Sdp.SessionDescriptionLine DisabledSendLine = new Sdp.SessionDescriptionLine("b=RS:0");

            internal static Sdp.SessionDescriptionLine DisabledApplicationSpecificLine = new Sdp.SessionDescriptionLine("b=AS:0");

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                string token;

                return TryParseBandwidthLine(line, out token, out result);
            }

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out string token, out int result)
            {
                token = string.Empty;

                result = -1;

                if (line == null || line.Type != Sdp.SessionDescription.BandwidthType) return false;

                string[] tokens = line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2) return false;

                token = tokens[0];

                return int.TryParse(tokens[1], out result);
            }

            public static bool TryParseRecieveBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.SessionDescription.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(RecieveBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseSendBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.SessionDescription.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(SendBandwdithToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseGetApplicationSpecificBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.SessionDescription.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(ApplicationSpecificBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseBandwidthDirectives(Media.Sdp.MediaDescription mediaDescription, out int rrDirective, out int rsDirective, out int asDirective)
            {
                rrDirective = rsDirective = asDirective = -1;

                if (mediaDescription == null) return false;

                int parsed = -1;

                string token = string.Empty;

                foreach (Media.Sdp.SessionDescriptionLine line in mediaDescription.BandwidthLines)
                {
                    if (TryParseBandwidthLine(line, out token, out parsed))
                    {
                        switch (token)
                        {
                            case RecieveBandwidthToken:
                                rrDirective = parsed;
                                continue;
                            case SendBandwdithToken:
                                rsDirective = parsed;
                                continue;
                            case ApplicationSpecificBandwidthToken:
                                asDirective = parsed;
                                continue;

                        }
                    }
                }

                //Determine if rtcp is disabled
                return parsed >= 0;
            }

            #endregion

            public static TransportContext FromMediaDescription(Sdp.SessionDescription sessionDescription, byte dataChannel, byte controlChannel, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true, int remoteSsrc = 0, int minimumSequentialpackets = 2)
            {

                if (mediaDescription == null) throw new ArgumentNullException("mediaDescription");

                TransportContext tc = new TransportContext(dataChannel, controlChannel, RFC3550.Random32(Media.Rtcp.SourceDescriptionReport.PayloadType), mediaDescription, rtcpEnabled, remoteSsrc, minimumSequentialpackets);

                int reportReceivingEvery = 0, 
                    reportSendingEvery = 0, 
                    asData = 0;

                if (rtcpEnabled)
                {
                    //Set to the default interval
                    reportSendingEvery = reportReceivingEvery = (int)DefaultReportInterval.TotalMilliseconds;

                    //If any lines were parsed
                    if (TryParseBandwidthDirectives(mediaDescription, out reportReceivingEvery, out reportSendingEvery, out asData))
                    {
                        //Determine if rtcp is disabled
                        bool rtcpDisabled = reportReceivingEvery == 0 && reportSendingEvery == 0;

                        //If Rtcp is not disabled then this will set the read and write timeouts.
                        if (false == rtcpDisabled)
                        {
                            /*
                             For the RTP A/V Profile [2], which specifies that the default RTCP
                                interval algorithm defined in the RTP spec [1] is to be used, at
                                least RS/(RS+RR) of the RTCP bandwidth is dedicated to active data
                                senders.  If the proportion of senders to total participants is less
                                than or equal to RS/(RS+RR), each sender gets RS divided by the
                                number of senders.  When the proportion of senders is greater than
                                RS/(RS+RR), the senders get their proportion of the sum of these
                                parameters, which means that a sender and a non-sender each get the
                                same allocation.  Therefore, it is not possible to constrain the data
                                senders to use less RTCP bandwidth than is allowed for non-senders.
                                A few special cases are worth noting:
                             */

                            tc.IsRtcpEnabled = true;

                            if(reportReceivingEvery > 0) tc.m_ReceiveInterval = TimeSpan.FromSeconds(reportReceivingEvery / Utility.MicrosecondsPerMillisecond);

                            if (reportSendingEvery > 0) tc.m_SendInterval = TimeSpan.FromSeconds(reportSendingEvery / Utility.MicrosecondsPerMillisecond);

                            //Todo
                            //Should set MaximumRtcpBandwidthPercentage

                        }
                        else if (rtcpEnabled) tc.IsRtcpEnabled = false;
                    }
                }
               

                //check for range in mediaDescription

                var rangeInfo = mediaDescription.RangeLine ?? (sessionDescription != null ? sessionDescription.RangeLine : null);

                if (rangeInfo != null)
                {
                    string type;
                    Media.Sdp.SessionDescription.TryParseRange(rangeInfo.Parts[0], out type, out tc.m_StartTime, out tc.m_EndTime);
                }

                //rtcp-mux is handled in the Initialize call

                return tc;
            }

            public static GoodbyeReport CreateGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null, RFC3550.SourceList sourcesLeaving = null)
            {
                //Make a Goodbye, indicate version in Client, allow reason for leaving 
                //Todo add other parties where null with SourceList
                return new GoodbyeReport(context.Version, ssrc ?? (int)context.SynchronizationSourceIdentifier, sourcesLeaving ?? new RFC3550.SourceList((uint)context.SynchronizationSourceIdentifier), reasonForLeaving);
            }

            /// <summary>
            /// Creates a <see cref="SendersReport"/> from the given context.
            /// Note, If empty is false and no previous <see cref="SendersReport"/> was sent then the report will be empty anyway.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="empty">Specifies if the report should have any report blocks if possible</param>
            /// <returns>The report created</returns>
            /// TODO, Allow an alternate ssrc
            public static SendersReport CreateSendersReport(TransportContext context, bool empty, bool rfc = true)
            {
                //Create a SendersReport
                SendersReport result = new SendersReport(context.Version, false, 0, context.SynchronizationSourceIdentifier);

                //Use the values from the TransportChannel (Use .NtpTimestamp = 0 to Disable NTP)[Should allow for this to be disabled]
                result.NtpTimestamp = context.NtpTimestamp;

                //Note that in most cases this timestamp will not be equal to the RTP timestamp in any adjacent data packet.  Rather, it MUST be  calculated from the corresponding NTP timestamp using the relationship between the RTP timestamp counter and real time as maintained by periodically checking the wallclock time at a sampling instant.
                result.RtpTimestamp = context.RtpTimestamp; 

                //Counters
                result.SendersOctetCount = (int)(rfc ? context.RfcRtpBytesSent : context.RtpBytesSent);
                result.SendersPacketCount = (int)context.RtpPacketsSent;

                //Ensure there is a remote party
                //If source blocks are included include them and calculate their statistics
                if (false == empty && context.RemoteSynchronizationSourceIdentifier != null && context.RemoteSynchronizationSourceIdentifier.Value != 0 && context.TotalPacketsSent > 0)
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

                if (false == empty && context.RemoteSynchronizationSourceIdentifier != null && context.RemoteSynchronizationSourceIdentifier.Value != 0 && context.TotalRtpPacketsReceieved > 0)
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
            internal long RfcRtpBytesSent, RfcRtpBytesRecieved, 
                         RtpBytesSent, RtpBytesRecieved,
                         RtcpBytesSent, RtcpBytesRecieved,
                         RtpPacketsSent, RtcpPacketsSent,
                         RtpPacketsReceived, RtcpPacketsReceived;

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

            internal TimeSpan m_SendInterval = DefaultReportInterval, m_ReceiveInterval = DefaultReportInterval, m_InactiveTime = Utility.InfiniteTimeSpan,m_StartTime = TimeSpan.Zero, m_EndTime = Utility.InfiniteTimeSpan;

            //When packets are succesfully transferred the DateTime (utc) is copied in these variables and will reflect the point in time in which  the last 
            internal DateTime m_FirstPacketReceived, m_FirstPacketSent, m_LastRtcpIn, m_LastRtcpOut,  //Rtcp packets were received and sent
                m_LastRtpIn, m_LastRtpOut; //Rtp packets were received and sent

            /// <summary>
            /// Keeps track of any failures which occur when sending or receieving data.
            /// </summary>
            internal protected int m_FailedRtpTransmissions, m_FailedRtcpTransmissions, m_FailedRtpReceptions, m_FailedRtcpReceptions;

            /// <summary>
            /// Used to ensure packets are allowed.
            /// </summary>
            internal protected ushort m_MimumPacketSize = 8, m_MaximumPacketSize = ushort.MaxValue;

            #endregion

            #region Properties

            /// <summary>
            /// Sets or gets the applications-specific state associated with the TransportContext.
            /// </summary>
            public Object ApplicationContext { get; set; }

            public int MinimumPacketSize
            {
                get { return (int)m_MimumPacketSize; }
                set { m_MimumPacketSize = (ushort)value; }
            }

            public int MaximumPacketSize
            {
                get { return (int)m_MaximumPacketSize; }
                set { m_MaximumPacketSize = (ushort)value; }
            }

            public bool HasAnyActivity
            {
                get { return HasRtpActivity || HasRtcpActivity; }
            }

            public bool HasRtpActivity
            {
                get
                {
                    //Check for Rtp Receive Activity if receiving
                    return HasReceivedRtpWithinReceiveInterval
                        || //Check for Rtp Send Activity if sending
                        HasSentRtpWithinSendInterval;
                }
            }

            public bool HasRtcpActivity
            {
                get
                {
                    //Check for Rtcp Receive Activity if receiving
                    return HasReceivedRtcpWithinReceiveInterval
                        || //Check for Rtcp Send Activity if sending
                        HasSentRtcpWithinSendInterval;
                }
            }

            public bool HasReceivedRtpWithinReceiveInterval
            {
                get
                {
                    return TotalRtpPacketsReceieved > 0 && m_LastRtpIn != DateTime.MinValue && LastRtpPacketReceived < m_ReceiveInterval;
                }
            }

            public bool HasSentRtpWithinSendInterval
            {
                get
                {
                    return TotalRtpPacketsSent > 0 && m_LastRtpOut != DateTime.MinValue && LastRtpPacketSent < m_SendInterval;
                }
            }

            public bool HasReceivedRtcpWithinReceiveInterval
            {
                get
                {
                    return TotalRtcpPacketsReceieved > 0 && m_LastRtcpIn != DateTime.MinValue && LastRtcpReportReceived < m_ReceiveInterval;
                }
            }

            public bool HasSentRtcpWithinSendInterval
            {
                get
                {
                    return TotalRtcpPacketsSent > 0 && m_LastRtcpOut != DateTime.MinValue && LastRtcpReportSent < m_SendInterval;
                }
            }

            /// <summary>
            /// Indicates if the RemoteParty is known by a unique id other than 0.
            /// </summary>
            internal bool InDiscovery { get { return RemoteSynchronizationSourceIdentifier == 0; } }

            /// <summary>
            /// Gets or Sets a value which indicates if the Rtp and Rtcp Sockets should be Disposed when Dispose is called.
            /// </summary>
            public bool LeaveOpen { get; set; }

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
            public bool IsConnected
            {
                get { return false == IsDisposed && (LocalRtp != null || LocalRtcp != null) && (RemoteRtp != null || RemoteRtcp != null); }
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
                    if (IsDisposed || !IsRtcpEnabled) return true;

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
                    return IsDisposed || m_FirstPacketSent == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_FirstPacketSent;
                }
            }

            /// <summary>
            /// The amount of time the TransportContext has been receiving packets.
            /// </summary>
            public TimeSpan TimeReceiving
            {
                get
                {
                    return IsDisposed || m_FirstPacketReceived == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_FirstPacketReceived;
                }
            }

            ///// <summary>
            ///// Indicates if the context has been Sending or Receiving for more time then allowed.
            ///// </summary>
            //public bool MediaEnded
            //{
            //    get
            //    {
            //        return !IsContinious && TimeSending == Utility.InfiniteTimeSpan && TimeReceiving == TimeSending;
            //    }
            //}

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
                    return m_LastRtcpOut == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_LastRtcpOut;
                }
            }

            /// <summary>
            /// Gets the time in which the last Rtcp reports were received.
            /// </summary>
            public TimeSpan LastRtcpReportReceived
            {
                get
                {
                    return m_LastRtcpIn == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_LastRtcpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was received.
            /// </summary>
            public TimeSpan LastRtpPacketReceived
            {
                get
                {
                    return m_LastRtpIn == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_LastRtpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was transmitted.
            /// </summary>
            public TimeSpan LastRtpPacketSent
            {
                get
                {
                    return m_LastRtpOut == DateTime.MinValue ? Utility.InfiniteTimeSpan : DateTime.UtcNow - m_LastRtpOut;
                }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when sending RtcpPackets
            /// </summary>
            public int FailedRtcpTransmissions { get { return m_FailedRtcpTransmissions; } }

            /// <summary>
            /// Indicates the amount of times a failure has occured when sending RtpPackets
            /// </summary>
            public int FailedRtpTransmissions { get { return m_FailedRtpTransmissions; } }

            /// <summary>
            /// Indicates the amount of times a failure has occured when receiving RtcpPackets
            /// </summary>
            public int FailedRtcpReceptions { get { return m_FailedRtcpReceptions; } }

            /// <summary>
            /// Indicates the amount of times a failure has occured when receiving RtpPackets
            /// </summary>
            public int FailedRtpReceptions { get { return m_FailedRtpReceptions; } }

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
            /// Indicates if the Rtcp is enabled and the LocalRtp is equal to the LocalRtcp
            /// </summary>
            public bool LocalMultiplexing
            {
                get { return IsDisposed || IsRtcpEnabled == false || LocalRtp == null ? false : LocalRtp.Equals(LocalRtcp); }
            }

            /// <summary>
            /// Indicates if the Rtcp is enabled and the RemoteRtp is equal to the RemoteRtcp
            /// </summary>
            public bool RemoteMultiplexing
            {
                get { return IsDisposed || IsRtcpEnabled == false || RemoteRtp == null ? false : RemoteRtp.Equals(RemoteRtcp); }
            }
            
            /// <summary>
            /// <c>false</c> if NOT [RtpEnabled AND RtcpEnabled] AND [LocalMultiplexing OR RemoteMultiplexing]
            /// </summary>
            public bool IsDuplexing
            {
                get
                {
                    if (IsDisposed) return false;

                    try { return (IsRtpEnabled && IsRtcpEnabled) && (LocalMultiplexing || RemoteMultiplexing); }
                    catch { return false; }
                }
            }

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
            public long TotalPacketsReceived { get { return RtpPacketsReceived + RtcpPacketsReceived; } }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalPacketsSent { get { return RtpPacketsSent + RtcpPacketsSent; } }

            /// <summary>
            /// The total amount of RtpPackets sent
            /// </summary>
            public long TotalRtpPacketsSent { get { return IsDisposed ? 0 : RtpPacketsSent; } }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been sent.
            /// </summary>
            public long RtpPayloadBytesSent { get { return IsDisposed ? 0 : RtpBytesSent; } }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been received.
            /// </summary>
            public long RtpPayloadBytesRecieved { get { return IsDisposed ? 0 : RtpBytesRecieved; } }

            /// <summary>
            /// The total amount of bytes related to Rtp sent (including headers)
            /// </summary>
            public long TotalRtpBytesSent { get { return IsDisposed ? 0 : RtpBytesSent + RtpHeader.Length * RtpPacketsSent; } }

            /// <summary>
            /// The total amount of bytes related to Rtp received
            /// </summary>
            public long TotalRtpBytesReceieved { get { return IsDisposed ? 0 : RtpBytesRecieved + RtpHeader.Length * RtpPacketsSent; } }

            /// <summary>
            /// The total amount of RtpPackets received
            /// </summary>
            public long TotalRtpPacketsReceieved { get { return IsDisposed ? 0 : RtpPacketsReceived; } }

            /// <summary>
            /// The total amount of RtcpPackets recieved
            /// </summary>
            public long TotalRtcpPacketsSent { get { return IsDisposed ? 0 : RtcpPacketsSent; } }

            /// <summary>
            /// The total amount of sent bytes related to Rtcp 
            /// </summary>
            public long TotalRtcpBytesSent { get { return IsDisposed ? 0 : RtcpBytesSent; } }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) received
            /// </summary>
            public long TotalBytesReceieved { get { return IsDisposed ? 0 : TotalRtcpBytesReceieved + TotalRtpBytesReceieved; } }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalBytesSent { get { return IsDisposed ? 0 : TotalRtcpBytesSent + TotalRtpBytesSent; } }

            /// <summary>
            /// The total amount of RtcpPackets received
            /// </summary>
            public long TotalRtcpPacketsReceieved { get { return IsDisposed ? 0 : RtcpPacketsReceived; } }

            /// <summary>
            /// The total amount of bytes related to Rtcp received
            /// </summary>
            public long TotalRtcpBytesReceieved { get { return IsDisposed ? 0 : RtcpBytesRecieved; } }

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

                if (ssrc == senderSsrc && ssrc != 0) throw new InvalidOperationException("ssrc and senderSsrc must be unique.");

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
            /// Assigns a Non Zero value to <see cref="SynchronizationSourceIdentifier"/> to a random value based on the given seed.
            /// The value will also be different than <see cref="RemoteSynchronizationSourceIdentifier"/>.
            /// </summary>
            internal protected void AssignIdentity(int seed = Rtcp.SendersReport.PayloadType)
            {
                if (SynchronizationSourceIdentifier == 0)
                {
                    //Generate the id per RFC3550
                    do SynchronizationSourceIdentifier = RFC3550.Random32(seed);
                    while (SynchronizationSourceIdentifier == 0 || SynchronizationSourceIdentifier == RemoteSynchronizationSourceIdentifier);
                }
            }

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
                m_InactiveTime = Utility.InfiniteTimeSpan;
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
                if (IsDisposed || IsConnected) return;

                if (localRtp.Address.AddressFamily != remoteRtp.Address.AddressFamily) Common.ExceptionExtensions.RaiseTaggedException<TransportContext>(this, "localIp and remoteIp AddressFamily must match.");
                else if (punchHole) punchHole = false == Utility.IsOnIntranet(remoteRtp.Address); //Only punch a hole if the remoteIp is not on the LAN by default.
                
                //Erase previously set values on the TransportContext.
                //RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                //Set now if not already set
                AssignIdentity();

                try
                {
                    //Setup the RtpSocket
                    RtpSocket = new Socket(localRtp.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RtpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    RtpSocket.Bind(LocalRtp = localRtp);
                    RtpSocket.Connect(RemoteRtp = remoteRtp);
                    RtpSocket.SendTimeout = RtpSocket.ReceiveTimeout = (int)(m_ReceiveInterval.TotalMilliseconds / 2);
                    //RtpSocket.Blocking = false;
                    //RtpSocket.SendBufferSize = RtpSocket.ReceiveBufferSize = 0; //Use local buffer dont copy

                    
                    
                    //Tell the network stack what we send and receive has an order
                    if (RtpSocket.AddressFamily == AddressFamily.InterNetwork)
                    {
                        //http://en.wikipedia.org/wiki/Type_of_service
                        //CS5,EF	40,46	5 :Critical - mainly used for voice RTP
                        //40 || 46 is used for RTP Audio per Wikipedia
                        //48 is Internet
                        //Set type of service
                        RtpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                        RtpSocket.DontFragment = true;
                    }
                    
                    RtpSocket.MulticastLoopback = false;
                    
                    RtpSocket.SendBufferSize = 0;

                    //RtpSocket.ReceiveTimeout = RtpSocket.SendTimeout = DefaultReportInterval.Milliseconds;

                    #region Optional Parameters

                    //Set max ttl for slower networks
                    RtpSocket.Ttl = 255;

                    //May help if behind a router
                    //Allow Nat Traversal
                    //RtpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

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
                        catch (SocketException)
                        {
                            //The port was not open, allow the next recieve to determine the port
                            RemoteRtp = new IPEndPoint(((IPEndPoint)RemoteRtp).Address, 0);
                        }//We don't care about the response or any issues during the holePunch
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
                        RtcpSocket.SendTimeout = RtcpSocket.ReceiveTimeout = (int)(m_ReceiveInterval.TotalMilliseconds / 2);
                        

                        //Tell the network stack what we send and receive has an order
                        if (RtpSocket.AddressFamily == AddressFamily.InterNetwork)
                        {
                            RtcpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47);

                            RtcpSocket.DontFragment = true;
                        }

                        RtcpSocket.MulticastLoopback = false;

                        RtcpSocket.SendBufferSize = 0;

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
                            catch (SocketException)
                            {
                                //The port was not open, allow the next recieve to determine the port
                                RemoteRtcp = new IPEndPoint(((IPEndPoint)RemoteRtcp).Address, 0);
                            }
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
                if (IsDisposed || IsConnected) return;
                if (local.Address.AddressFamily != remote.Address.AddressFamily) Common.ExceptionExtensions.RaiseTaggedException<TransportContext>(this, "localIp and remoteIp AddressFamily must match.");
                
                //Erase previously set values on the TransportContext.
                //RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                try
                {
                    //Setup the RtcpSocket / RtpSocket
                    RtcpSocket = RtpSocket = new Socket(local.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    //Dont fragment
                    if (RtpSocket.AddressFamily == AddressFamily.InterNetwork) RtpSocket.DontFragment = true;

                    RtpSocket.Connect(RemoteRtp = RemoteRtcp = remote);

                    LocalRtp = LocalRtcp = RtpSocket.LocalEndPoint;

                    //Use expedited data as defined in RFC-1222. This option can be set only once; after it is set, it cannot be turned off.
                    RtpSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Expedited, true);

                    RtpSocket.SendBufferSize = 0;

                    AssignIdentity();

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
            //TODO Must allow leaveOpen for existing sockets
            public void Initialize(Socket rtpSocket, Socket rtcpSocket)
            {
                if (rtpSocket == null) throw new ArgumentNullException("rtpSocket");

                //Maybe should just be set to the rtpSocket?
                if (rtcpSocket == null) throw new ArgumentNullException("rtcpSocket");
                
                //RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                RtpSocket = rtpSocket;

                RtpSocket.DontFragment = true;

                RtpSocket.SendTimeout = RtpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds / 2);

                bool punchHole = RtpSocket.ProtocolType != ProtocolType.Tcp && !Utility.IsOnIntranet(((IPEndPoint)rtpSocket.RemoteEndPoint).Address); //Only punch a hole if the remoteIp is not on the LAN by default.

                RtcpSocket = rtcpSocket;

                RtcpSocket.DontFragment = true;

                LocalRtcp = RtcpSocket.LocalEndPoint;
                RemoteRtcp = RtcpSocket.RemoteEndPoint;

                LocalRtp = RtpSocket.LocalEndPoint;
                RemoteRtp = RtpSocket.RemoteEndPoint;

                //If a different socket is used for rtcp configure it also
                if (RtpSocket.Handle != RtcpSocket.Handle)
                {
                    RtcpSocket.SendTimeout = RtcpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds / 2);
                }

                if (punchHole)
                {
                    //new RtcpPacket(Version, Rtcp.ReceiversReport.PayloadType, 0, 0, SynchronizationSourceIdentifier, 0);
                    try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch

                    //Check for the same socket.
                    if (RtpSocket.Handle == RtcpSocket.Handle) return;

                    try { RtcpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch
                }

                AssignIdentity();

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
                if (false == IsConnected || IsDisposed) return;

                //Don't remove end points because a re-connect could occur?
                //LocalRtp = LocalRtcp = RemoteRtp = RemoteRtcp = null;

                if (LeaveOpen)
                {
                    RtpSocket = RtcpSocket = null;
                }
                else
                {
                    //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                    if (RtcpSocket != null && RtpSocket.Handle != RtcpSocket.Handle) RtcpSocket.Close();

                    //Close the RtpSocket
                    if (RtpSocket != null) RtpSocket.Close();

                    RtpSocket = RtcpSocket = null;
                }

                //Why erase stats?
                //m_FirstPacketReceived = DateTime.MinValue;

                //m_FirstPacketSent = DateTime.MinValue;
            }

            /// <summary>
            /// Removes references to any reports receieved and resets the validation counters to 0.
            /// </summary>
            internal void ResetState()
            {

                if (RemoteSynchronizationSourceIdentifier.HasValue) RemoteSynchronizationSourceIdentifier = null;// default(int);

                RfcRtpBytesSent = RtpPacketsSent = RtpBytesSent = RtcpPacketsSent = RtcpBytesSent = RtpPacketsReceived = RtpBytesRecieved = RtcpBytesRecieved = RtcpPacketsReceived = m_FailedRtcpTransmissions = m_FailedRtpTransmissions = 0;

                //Interlocked.Exchange(ref RtpPacketsSent, 0);

                //Interlocked.Exchange(ref RtpBytesSent, 0);

                //Interlocked.Exchange(ref RtcpPacketsSent, 0);

                //Interlocked.Exchange(ref RtcpBytesSent, 0);

                //////////////////////

                //Interlocked.Exchange(ref RtpPacketsReceived, 0);

                //Interlocked.Exchange(ref RtpBytesRecieved, 0);

                //Interlocked.Exchange(ref RtcpBytesRecieved, 0);

                //Interlocked.Exchange(ref RtcpPacketsReceieved, 0);

                //Interlocked.Exchange(ref m_FailedRtcpTransmissions, 0);

                //Interlocked.Exchange(ref m_FailedRtpTransmissions, 0);
            }

            /// <summary>
            /// Disposes the TransportContext and all underlying resources.
            /// </summary>
            public override void Dispose()
            {
                if (IsDisposed) return;

                IsDisposed = true;

                DisconnectSockets();
            }

            #endregion

            IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
            {
                if (IsDisposed) yield break;

                if (RtpSocket != null)
                {
                    yield return RtpSocket;

                    if (RtpSocket.ProtocolType == ProtocolType.Tcp || IsDuplexing) yield break;
                }

                if (RtcpSocket != null) yield return RtcpSocket;
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

        //Outgoing Packets, Not a Queue because you cant re-order a Queue (in place) and you can't take a range from the Queue (in a single operation)
        //Those things aside, ordering is not performed here and only single packets are iterated and would eliminate the need for removing after the operation.
        //Benchmark with Queue and ConcurrentQueue and a custom impl.
        internal List<RtpPacket> m_OutgoingRtpPackets = new List<RtpPacket>();
        internal List<RtcpPacket> m_OutgoingRtcpPackets = new List<RtcpPacket>();

        //Unless I missed something that damn HashSet is good for nothing except hashing.
        //The list also allows duplicates if required.
        internal IList<TransportContext> TransportContexts = new List<TransportContext>();

        internal readonly Guid InternalId = Guid.NewGuid();

        public Common.ILogging Logger;

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
            if (IsDisposed ||  false == RtcpEnabled || false == IncomingPacketEventsEnabled || packet == null || packet.IsDisposed) return;

            //Raise an event for the rtcp packet received
            OnRtcpPacketReceieved(packet);

            //Get a context for the packet by the identity of the receiver
            TransportContext transportContext = null;

            //Cache the ssrc of the packet's sender.
            int partyId = packet.SynchronizationSourceIdentifier;

            //See if there is a context for the remote party. (Allows 0)
            transportContext = GetContextBySourceId(partyId);

            //Only if the packet was not addressed to a unique party with the id of 0
            if (partyId != 0 &&
                transportContext == null || 
                transportContext.InDiscovery) //The remote party has not yet been identified.
            {
                //Cache the payloadType
                int payloadType = packet.PayloadType;

                if(payloadType == Rtcp.ReceiversReport.PayloadType &&  //The packet is a RecieversReport                                       
                    packet.BlockCount > 0)//There is at least 1 block
                {
                    //Create a wrapper around the packet to access the ReportBlocks
                    using (var rr = new Rtcp.ReceiversReport(packet, false))
                    {
                        //Iterate each contained ReportBlock
                        foreach (Rtcp.IReportBlock reportBlock in rr)
                        {
                            int blockId = reportBlock.BlockIdentifier;

                            //Attempt to obtain a context by the identifier in the report block
                            transportContext = GetContextBySourceId(blockId);

                            //If there was a context and the remote party has not yet been identified.
                            if (transportContext != null && transportContext.InDiscovery)
                            {
                                //Identify the remote party by this id.
                                transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + "RR blockId=" + blockId);

                                //Stop looking for a context.
                                break;
                            }
                        }
                    }
                }
                else if (payloadType == Rtcp.GoodbyeReport.PayloadType
                    &&
                    partyId == transportContext.RemoteSynchronizationSourceIdentifier &&
                    packet.BlockCount > 0) //The GoodbyeReport report from a remote party
                {
                    //Create a wrapper around the packet to access the source list
                    using (var gb = new Rtcp.GoodbyeReport(packet, false))
                    {
                        using (var sl = gb.GetSourceList())
                        {
                            //Iterate each party leaving
                            foreach (int party in sl)
                            {
                                //Attempt to obtain a context by the identifier in the report block
                                transportContext = GetContextBySourceId(party);

                                //If there was a context
                                if (transportContext != null && 
                                    false == transportContext.IsDisposed)
                                {
                                    //Send report now if possible.
                                    bool reportsSent = SendReports(transportContext);

                                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Recieved Goodbye @ " + transportContext.SynchronizationSourceIdentifier + " from=" + partyId + " reportSent=" + reportsSent);

                                    //Stop looking for a context.
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (payloadType == Rtcp.SendersReport.PayloadType) //The senders report from a remote party                    
                {
                    //If there is a context
                    if (transportContext != null)
                    {
                        //The context is valid and still discovering a remote identity
                        if (transportContext.IsValid && transportContext.InDiscovery)
                        {
                            //Assign it
                            transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                            Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + " SR=" + partyId);

                        } //If the context has been identified by an identity other than the remote party of the packet                    
                        else if (transportContext.RemoteSynchronizationSourceIdentifier != partyId)
                        {
                            //Attempt to obtain a context by the identity used previously
                            transportContext = GetContextBySourceId(partyId);

                            //If ther is no longer a context or the context cannot handle the packet
                            if (transportContext == null ||
                                transportContext.IsDisposed)
                            {
                                goto NoContext;
                            }

                            //If the context needs a remote identity and is still not yet valid
                            if (transportContext.InDiscovery && false == transportContext.IsValid)
                            {
                                //Assign it
                                transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + " SR=" + partyId);
                            }

                        }
                    }//Validate by using the blocks of the report if possible
                    else if (packet.BlockCount > 0) 
                    {
                        //Create a wrapper around the packet to access the ReportBlocks
                        using (var rr = new Rtcp.SendersReport(packet, false))
                        {
                            //Iterate each contained ReportBlock
                            foreach (Rtcp.IReportBlock reportBlock in rr)
                            {
                                int blockId = reportBlock.BlockIdentifier;

                                //Attempt to obtain a context by the identifier in the report block
                                var context = GetContextBySourceId(reportBlock.BlockIdentifier);

                                //If there was a context
                                if (context != null)
                                {
                                    //if the context found identifies the context assumed
                                    if (context.SynchronizationSourceIdentifier == transportContext.SynchronizationSourceIdentifier)
                                    {
                                        //Identify the remote party by this id.
                                        transportContext.RemoteSynchronizationSourceIdentifier = packet.SynchronizationSourceIdentifier;

                                        Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + "SR blockId=" + blockId);

                                        //Remove any reference
                                        context = null;

                                        //Stop looking for a context.
                                        break;
                                    }
                                }
                            }

                            //might not have checked anything if the list was 'incomplete'..
                        }
                    }
                }
            }

            //Handle Goodbyes with a positive blockcount but no  sourcelist...?

        NoContext:

            //If no transportContext could be found
            if (transportContext == null)
            {
                //Attempt to see if this was a rtp packet by using the RtpPayloadType
                int rtpPayloadType = packet.Header.First16Bits.RtpPayloadType;

                if (rtpPayloadType == 13 || GetContextByPayloadType(rtpPayloadType) != null)
                {
                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtcpPacket - Incoming RtcpPacket actually was Rtp. Ssrc= " + partyId + " Type=" + rtpPayloadType + " Len=" + packet.Length);

                    //Raise an event for the 'RtpPacket' received.
                    OnRtpPacketReceieved(new RtpPacket(packet.Prepare().ToArray(), 0));

                    //Don't do anything else
                    return;
                }

                //Could attempt to find the context in which this packet is trying to communicate with if we had a RemoteEndPoint indicating where the packet was received from...
                //Cannot find a context because there may be more then one context which has not yet been identified
                //Could attempt to check that there is only 1 context and then if not yet valid assign the identity...
                //if(TransportContexts.Count == 1) ...

                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtcpPacket - No Context for packet " + partyId + "@" + packet.PayloadType);

                //Don't do anything else.
                return;
            }
            
            //There is a transportContext

            //If there is a collision in the unique identifiers
            if (transportContext.SynchronizationSourceIdentifier == partyId)
            {
                //Handle it.
                HandleCollision(transportContext);
            }

            //Make a copy of the packet now and only refer to this copy
            RtcpPacket localPacket = packet;

            #region Unused [Packet Completion]

            //Complete the RtcpPacket if required.
            //while (!localPacket.IsComplete)
            //{
            //    //Complete the packet.
            //    int received = localPacket.CompleteFrom(transportContext.RtcpSocket, localPacket.Payload);
            //}

            #endregion

            //Last Rtcp packet was received right now now.
            transportContext.m_LastRtcpIn = packet.Created;

            //The context is active.
            transportContext.m_InactiveTime = Utility.InfiniteTimeSpan;

            //Don't worry about overflow
            unchecked
            {
                //Increment packets received for the valid context.
                ++transportContext.RtcpPacketsReceived;

                //Keep track of the the bytes sent in the context
                transportContext.RtcpBytesRecieved += localPacket.Length;

                //Set the time when the first rtcp packet was recieved
                if (transportContext.m_FirstPacketReceived == DateTime.MinValue) transportContext.m_FirstPacketReceived = packet.Created;
            }

            #region Unused [Handle if packet was Goodbye]

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

            #endregion       
        }

        protected internal virtual void HandleCollision(TransportContext transportContext)
        {

            if (transportContext == null) throw new ArgumentNullException("transportContext");

            if (transportContext.IsDisposed) throw new ObjectDisposedException("transportContext");

            Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleCollision - Ssrc=" + transportContext.SynchronizationSourceIdentifier + " - RSsrc=" + transportContext.RemoteSynchronizationSourceIdentifier);

            //Send a goodbye and indicate why.
            SendGoodbye(transportContext, Encoding.UTF8.GetBytes("ssrc"));

            //Assign a new random ssrc which is not equal to the remote parties.
            //Noting that you could use the same ssrc +/-N here also or a base from the number of parties etc.

            //This may deserve an event, 'OnCollision'

            do transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(transportContext.SynchronizationSourceIdentifier);
            while (transportContext.SynchronizationSourceIdentifier == transportContext.RemoteSynchronizationSourceIdentifier);

            //Reset counters from this point forward
            transportContext.ResetState();
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
            if (false == IncomingPacketEventsEnabled || false == RtpEnabled || IsDisposed || packet == null || packet.IsDisposed) return;

            //Fire an event now to let subscribers know a packet has arrived
            OnRtpPacketReceieved(packet);

            //Not supported at the moment
            if (packet.Header.IsCompressed)
            {
                return;
            }

            //Get the transportContext for the packet by the sourceId then by the payload type of the RtpPacket, not the SSRC alone because it may have not yet been defined.
            //Noting that this is not per RFC3550
            //This is because this implementation allows for the value 0 to be used as a discovery mechanism.
            TransportContext transportContext = GetContextForPacket(packet);

            //If the context is still null
            if (transportContext == null)
            {
                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "Unaddressed RTP Packet " + packet.SynchronizationSourceIdentifier + " PT =" + packet.PayloadType + " len =" + packet.Length);

                //Do nothing else.
                return;
            }

            //Cache the ssrc
            int partyId = packet.SynchronizationSourceIdentifier;

            //Check for a collision
            if (partyId == transportContext.SynchronizationSourceIdentifier)
            {
                //Handle it
                HandleCollision(transportContext);
            }

            #region Unused [Handles Client InDiscovery When IsValid is false]

            //////If the context is NOT valid AND the context is in discovery mode of the remote party
            ////if (false == transportContext.IsValid && transportContext.InDiscovery)
            ////{
            ////    //Assign an id at this time
            ////    transportContext.RemoteSynchronizationSourceIdentifier = partyId;
            ////}

            #endregion

            //If the packet was not addressed to the context but the context is valid AND the context is NOT in discovery mode.
            if (partyId != transportContext.RemoteSynchronizationSourceIdentifier
                &&
                transportContext.IsValid)
            {
                //Reset the state if not discovering
                if (false == transportContext.InDiscovery)
                {
                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket SSRC Mismatch @ " + transportContext.SynchronizationSourceIdentifier + '-' + transportContext.RemoteSynchronizationSourceIdentifier + " / " + partyId + ". ResetState");

                    transportContext.ResetState();
                }

                //Assign the id of the remote party.
                transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier);
            }

            //Don't worry about overflow.
            unchecked
            {
                int packetLength = packet.Length;

                //If the packet is not valid then
                if (false == transportContext.ValidatePacketAndUpdateSequenceNumber(packet) ||
                    packetLength > transportContext.MaximumPacketSize ||
                    packetLength < transportContext.MinimumPacketSize)
                {
                    //Increment for a failed reception 
                    ++transportContext.m_FailedRtpReceptions;

                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Failed Reception " +
                             "(= " + transportContext.m_FailedRtpReceptions + ") @" + transportContext.SynchronizationSourceIdentifier + 
                            " seq=" + packet.SequenceNumber + " len= " + packet.Length);

                    //Only proceeed further in the context is valid
                    //if(transportContext.IsValid) return;
                }

                //Increment RtpPacketsReceived for the context relating to the packet.
                ++transportContext.RtpPacketsReceived;

                //Ensure the id matches.
                if (transportContext.InDiscovery && transportContext.IsValid) transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                //The counters for the bytes will now be be updated (without the 12 octets of the header)
                //increment the counters (Only use the Payload.Count per the RFC) (new Erratta Submitted)
                //http://www.rfc-editor.org/errata_search.php?rfc=3550
                transportContext.RtpBytesRecieved += packet.Payload.Count;


                //Please not due to the 'consensus' achieved for this standard (RFC 1889 / RFC3550 / RFC3551)
                //The counters for the rtp bytes sent are specifically counted only to reveal average data rate...
                //A Senders report may only indicate the values which are allowed in the rfc. (Probably so middle boxes can't be detected)
                //Otherwise it's not complaint but no one will figure out how or why since its not supposed to effect annex calulcations...
                //Additionally the jitter caluclations would be messed up in most cases where a sourcelist or padding is used because it doesn't take those values into account
                //This implemenation doesn't suffer from this non-sense.

                transportContext.RfcRtpBytesRecieved += packet.Length - (packet.Header.Size + packet.HeaderOctets + packet.PaddingOctets);

                //Set the time when the first RtpPacket was received if required
                if (transportContext.m_FirstPacketReceived == DateTime.MinValue) transportContext.m_FirstPacketReceived = packet.Created;

                //Update the SequenceNumber and Timestamp and calulcate Inter-Arrival (Mark the context as active)
                transportContext.UpdateJitterAndTimestamp(packet);

                //Set the last rtp in after inter-arrival has been calculated.
                transportContext.m_LastRtpIn = packet.Created;

                //If the instance does not handle frame changed events then return
                if (false == FrameChangedEventsEnabled) return;
            }
            
            //Take local variable of frame if multiple threads may access this logic
            //ThreadLocal<RtpFrame> currentFrame = new ThreadLocal<RtpFrame>(()=>transportContext.CurrentFrame, false), lastFrame = new ThreadLocal<RtpFrame>(()=>transportContext.LastFrame, false)

            //Note ALSO
            //If the ssrc changed mid stream but the data is still somehow relevent to the lastFrame or currentFrame
            //Then the ssrc of the packet must be changed or the ssrc of the frame must be changed before adding the packet.

            int payloadType = packet.PayloadType;

            //If we have not allocated a currentFrame
            if (transportContext.CurrentFrame == null)
            {
                //make a frame
                transportContext.CurrentFrame = new RtpFrame(payloadType, packet.Timestamp, packet.SynchronizationSourceIdentifier);
            }//Check to see if the frame belongs to the last frame
            else if (transportContext.LastFrame != null && packet.Timestamp == transportContext.LastFrame.Timestamp && transportContext.MediaDescription.PayloadTypes.Contains(payloadType))
            {
                //Create a new packet from the localPacket so it will not be disposed when the packet is disposed.
                if (false == transportContext.LastFrame.IsComplete) transportContext.LastFrame.Add(new RtpPacket(packet.Prepare().ToArray(), 0));

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
            else if (transportContext.CurrentFrame != null && packet.Timestamp != transportContext.CurrentFrame.Timestamp && transportContext.MediaDescription.PayloadTypes.Contains(payloadType))
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
                transportContext.CurrentFrame = new RtpFrame(packet.PayloadType, packet.Timestamp, packet.SynchronizationSourceIdentifier);

                //The LastFrame changed
                OnRtpFrameChanged(transportContext.LastFrame);
            }

            //If there is a current frame
            if (transportContext.CurrentFrame != null)
            {
                //If the payload of the localPacket matched the media description then create a new packet from the localPacket so it will not be disposed when the packet is disposed.
                if (transportContext.MediaDescription.PayloadTypes.Contains(payloadType)) transportContext.CurrentFrame.Add(new RtpPacket(packet.Prepare().ToArray(), 0));

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
        }

        /// <summary>
        /// Increments the RtpBytesSent and RtpPacketsSent for the TransportChannel related to the packet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        protected internal virtual void HandleRtpPacketSent(object sender, RtpPacket packet)
        {
            if (IsDisposed || packet == null || false == packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            if (transportContext == null) return;

            unchecked
            {

                //This allows detection of middle boxes, make a seperate sample, default implementation should be 'compliant'...

                //increment the counters (Only use the Payload.Count per the RFC) (new Erratta Submitted)
                //http://www.rfc-editor.org/errata_search.php?rfc=3550
                transportContext.RtpBytesSent += packet.Payload.Count;


                //Please not due to the 'consensus' achieved for this standard (RFC 1889 / RFC3550 / RFC3551)
                //The counters for the rtp bytes sent are specifically counted only to reveal average data rate...
                //A Senders report may only indicate the values which are allowed in the rfc. (Probably so middle boxes can't be detected)
                //Otherwise it's not complaint but no one will figure out how or why since its not supposed to effect annex calulcations...
                //Additionally the jitter caluclations would be messed up in most cases where a sourcelist or padding is used because it doesn't take those values into account
                //This implemenation doesn't suffer from this non-sense.

                transportContext.RfcRtpBytesSent += packet.Length - (packet.Header.Size + packet.HeaderOctets + packet.PaddingOctets);

                ++transportContext.RtpPacketsSent;

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

                //Common.ILoggingExtensions.Log(Logger, "Rtp Packet Sent");

            }
        }

        protected internal virtual void HandleRtcpPacketSent(object sender, RtcpPacket packet)
        {

            if (IsDisposed || packet == null || false == packet.Transferred.HasValue) return;

            TransportContext transportContext = GetContextForPacket(packet);

            //if there is no context there is nothing to do.
            if (transportContext == null) return;

            unchecked
            {
                //Update the counters for the amount of bytes in the RtcpPacket including the header and any padding.
                transportContext.RtcpBytesSent += packet.Length;

                //Update the amount of packets sent
                ++transportContext.RtcpPacketsSent;

                //Mark the context as active immediately.
                transportContext.m_InactiveTime = Utility.InfiniteTimeSpan;

                //Get the time the packet was sent
                DateTime sent = packet.Transferred.Value;

                //Store the last time a RtcpPacket was sent
                transportContext.m_LastRtcpOut = sent;

                //Set the time the first packet was sent.
                if (transportContext.m_FirstPacketSent == DateTime.MinValue) transportContext.m_FirstPacketSent = sent;

                //if (Logger != null) Logger.Log("Rtcp Packet Sent");
            }

            //Backoff based on ConverganceTime?
        }

        protected internal void OnInterleavedData(byte[] data, int offset, int length)
        {
            if (IsDisposed) return;
            InterleaveHandler action = InterleavedData;

            if (action == null) return;

            foreach (InterleaveHandler handler in action.GetInvocationList())
            {
                try { handler(this, data, offset, length); }
                catch { continue; }
            }
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtpPacketReceieved(RtpPacket packet)
        {
            if (IsDisposed || false == IncomingPacketEventsEnabled) return;

            RtpPacketHandler action = RtpPacketReceieved;

            if (action == null) return;

            foreach (RtpPacketHandler handler in action.GetInvocationList())
            {
                try { handler(this, packet); }
                catch { continue; }
            }
        }

        //Should ensure the semantic that all callers who observe this event are aware if the packet was already handled or not.

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtcpPacketReceieved(RtcpPacket packet)
        {
            if (IsDisposed || false == IncomingPacketEventsEnabled) return;
            RtcpPacketHandler action = RtcpPacketReceieved;

            if (action == null) return;

            foreach (RtcpPacketHandler handler in action.GetInvocationList())
            {
                try { handler(this, packet); }
                catch { continue; }
            }
        }

        /// <summary>
        /// Raises the RtpFrameHandler for the given frame if FrameEvents are enabled
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        internal void OnRtpFrameChanged(RtpFrame frame)
        {
            if (IsDisposed || false == FrameChangedEventsEnabled) return;
            RtpFrameHandler action = RtpFrameChanged;

            if (action == null) return;

            foreach (RtpFrameHandler handler in action.GetInvocationList())
            {
                try { handler(this, frame); }
                catch { continue; }
            }
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        protected internal void OnRtpPacketSent(RtpPacket packet)
        {
            if (IsDisposed) return;
            RtpPacketSent(this, packet);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        internal void OnRtcpPacketSent(RtcpPacket packet)
        {
            if (IsDisposed) return;
            RtcpPacketSent(this, packet);
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
        public virtual bool IsConnected { get { return !IsDisposed && m_WorkerThread != null && m_WorkerThread.IsAlive; } }

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
                if (!RtcpEnabled || IsDisposed) return true;

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
        public long TotalRtpPacketsSent { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsSent); } }

        /// <summary>
        /// The total amount of Rtp bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalRtpBytesSent { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.TotalRtpBytesSent); } }

        /// <summary>
        /// The total amount of Rtp bytes received of all contained TransportContexts
        /// </summary>
        public long TotalRtpBytesReceieved { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.TotalRtpBytesReceieved); } }

        /// <summary>
        /// The total amount of Rtp packets received of all contained TransportContexts
        /// </summary>
        public long TotalRtpPacketsReceieved { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtpPacketsReceived); } }

        /// <summary>
        /// The total amount of Rtcp packets sent of all contained TransportContexts
        /// </summary>
        public long TotalRtcpPacketsSent { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsSent); } }

        /// <summary>
        /// The total amount of Rtcp bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalRtcpBytesSent { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesSent); } }

        /// <summary>
        /// The total amount of bytes received of all contained TransportContexts
        /// </summary>
        public long TotalBytesReceieved { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.TotalBytesReceieved); } }

        /// <summary>
        /// The total amount of bytes sent of all contained TransportContexts
        /// </summary>
        public long TotalBytesSent { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.TotalBytesSent); } }

        /// <summary>
        /// The total amount of Rtcp packets received of all contained TransportContexts
        /// </summary>
        public long TotalRtcpPacketsReceieved { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtcpPacketsReceived); } }

        /// <summary>
        /// The total amount of Rtcp bytes received of all contained TransportContexts
        /// </summary>
        public long TotalRtcpBytesReceieved { get { return IsDisposed ? 0 : TransportContexts.Sum(c => c.RtcpBytesRecieved); } }

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
                //Need an IP or the default IP to ensure the MTU Matches.
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
        public virtual bool TryAddContext(TransportContext context, bool checkDataChannel = true, bool checkControlChannel = true, bool checkLocalIdentity = true, bool checkRemoteIdentity = true)
        {
            try
            {
                //If checking anything, iterate for each context `c` already added
                if (checkDataChannel || checkControlChannel || checkLocalIdentity || checkRemoteIdentity) foreach (TransportContext c in TransportContexts)
                    {
                        //If checking channels
                        if (checkDataChannel || checkControlChannel)
                        {
                            //If checking the data channel
                            if (checkDataChannel && c.DataChannel == context.DataChannel || c.ControlChannel == context.DataChannel)
                            {
                                Common.ExceptionExtensions.RaiseTaggedException(c, "Requested Data Channel is already in use by the context in the Tag");

                                goto ReturnFalse;
                            }

                            //if checking the control channel
                            if (checkControlChannel && c.ControlChannel == context.ControlChannel || c.DataChannel == context.ControlChannel)
                            {
                                Common.ExceptionExtensions.RaiseTaggedException(c, "Requested Control Channel is already in use by the context in the Tag");

                                goto ReturnFalse;
                            }

                        }

                        //if chekcking local identifier
                        if (checkLocalIdentity && c.SynchronizationSourceIdentifier == context.SynchronizationSourceIdentifier)
                        {
                            Common.ExceptionExtensions.RaiseTaggedException(c, "Requested Local SSRC is already in use by the context in the Tag");

                            goto ReturnFalse;
                        }

                        //if chekcking remote identifier (and it has been defined)
                        if (checkRemoteIdentity && false == context.InDiscovery && false == c.InDiscovery && c.RemoteSynchronizationSourceIdentifier == context.RemoteSynchronizationSourceIdentifier)
                        {
                            Common.ExceptionExtensions.RaiseTaggedException(c, "Requested Remote SSRC is already in use by the context in the Tag");

                            goto ReturnFalse;
                        }
                    }

                TransportContexts.Add(context);

                return true;
            }
            catch
            {
                goto ReturnFalse;
            }
        ReturnFalse: return false;
        }

        public virtual void AddContext(TransportContext context) { TryAddContext(context); }

        /// <summary>
        /// Removes the given <see cref="TransportContext"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual bool TryRemoveContext(TransportContext context)
        {
            try
            {
                return TransportContexts.Remove(context);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets any <see cref="TransportContext"/> used by this instance.
        /// </summary>
        /// <returns>The <see cref="TransportContexts"/> used by this instance.</returns>
        public virtual IEnumerable<TransportContext> GetTransportContexts()
        {
            if (IsDisposed) return Enumerable.Empty<TransportContext>();
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
        /// Creates any <see cref="RtcpReport"/>'s which are required by the implementation.
        /// The <see cref="SendersReport"/> and <see cref="ReceiversReport"/> (And accompanying <see cref="SourceDescriptionReport"/> if bandwidth allows) are created for the given context.
        /// </summary>
        /// <param name="context">The context to prepare Rtcp reports for</param>
        /// <param name="checkBandwidth">Indicates if the bandwidth of the RtpCliet or Context given should be checked.</param>
        /// <param name="storeReports">Indicates if the reports created should be stored on the corresponding properties of the instace.</param>
        /// <returns>The RtcpReport created.</returns>
        public virtual IEnumerable<RtcpReport> PrepareReports(TransportContext context, bool checkBandwidth = true, bool storeReports = true)
        {
            //Start with a sequence of empty packets
            IEnumerable<RtcpReport> compound = Enumerable.Empty<RtcpReport>();

            //If Rtp data was sent then send a Senders Report.
            if (context.RtpPacketsSent > 0)
            {
                //Insert the last SendersReport as the first compound packet
                if (storeReports)
                    compound = Enumerable.Concat((context.SendersReport = TransportContext.CreateSendersReport(context, false)).Yield(), compound);
                else
                    compound = Enumerable.Concat(TransportContext.CreateSendersReport(context, false).Yield(), compound);
            }

            //If Rtp data was received then send a Receivers Report.
            if (context.RtpPacketsReceived > 0)
            {
                //Insert the last ReceiversReport as the first compound packet
                if (storeReports)
                    compound = Enumerable.Concat((context.ReceiversReport = TransportContext.CreateReceiversReport(context, false)).Yield(), compound);
                else
                    compound = Enumerable.Concat(TransportContext.CreateReceiversReport(context, false).Yield(), compound);
            }

            //If there are any packets to be sent and we don't care about bandwidth or the bandwidth is not exceeded
            if (checkBandwidth == false && compound.Any() && false == context.RtcpBandwidthExceeded)
            {
                //Include the SourceDescription
                if (storeReports)
                    compound = Enumerable.Concat(compound, (context.SourceDescription = TransportContext.CreateSourceDescription(context)).Yield());
                else
                    compound = Enumerable.Concat(TransportContext.CreateSourceDescription(context).Yield(), compound);
            }

            //Could also put a Goodbye for inactivity ... :) Currently handled by SendGoodbye

            return compound;
        }

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
                if (false == tc.IsDisposed && tc.IsRtcpEnabled && SendReports(tc))
                {
                    sentAny = true;
                }
            }

            return sentAny;
        }

        /// <summary>
        /// Sends a Goodbye to for all contained TransportContext, which will also stop the process sending or receiving after the Goodbye is sent
        /// </summary>
        public virtual void SendGoodbyes()
        {
            foreach (RtpClient.TransportContext tc in TransportContexts)
                SendGoodbye(tc, null, tc.SynchronizationSourceIdentifier);
        }

        /// <summary>
        /// Sends a GoodbyeReport and stores it in the <paramref name="context"/> given if the <paramref name="ssrc"/> is also given and is equal to the <paramref name="context.SynchronizationSourceIdentifier"/>
        /// </summary>
        /// <param name="context">The context of the report</param>
        /// <param name="reasonForLeaving">An optional reason why the report is being sent.</param>
        /// <param name="ssrc">The optional identity to use in he report.</param>
        /// <param name="force">Indicates if the call should be forced. <see cref="IsRtcpEnabled"/></param>
        /// <returns></returns>
        internal protected virtual int SendGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null, bool force = false)
        {
            //Check if the Goodbye can be sent.
            if (IsDisposed //If the context is disposed
                && //AND the call has not been forced AND the context IsRtcpEnabled 
                (false == force && true == context.IsRtcpEnabled) 
                // OR there is no RtcpSocket
                || context.RtcpSocket == null
                //Of the final Goodbye was sent.
                || context.Goodbye != null && context.Goodbye.Transferred.HasValue)
            {
                //Indicate nothing was sent
                return 0;
            }

            //Make a Goodbye, indicate version in Client, allow reason for leaving 
            //Todo add other parties where null with SourceList
            GoodbyeReport goodBye = TransportContext.CreateGoodbye(context, reasonForLeaving, ssrc ?? context.SynchronizationSourceIdentifier);

            //Noting that I think the SourceDescription is not really requried because the Goodbye would optionally have a SourceList which is leaving.

            //Store the Goodbye in the context if the ssrc was given and it was for the context given.
            if(ssrc.HasValue && ssrc.Value == context.SynchronizationSourceIdentifier) context.Goodbye = goodBye;

            //Send the packet and return the amount of bytes which resulted.
            return SendRtcpPackets(Enumerable.Concat(PrepareReports(context, false, true), goodBye.Yield()));            
        }

        /// <summary>
        /// Sends a <see cref="Rtcp.SendersReport"/> for each TranportChannel if allowed by the <see cref="MaximumRtcpBandwidth"/>
        /// </summary>
        public virtual void SendSendersReports()
        {
            if (false == IsDisposed && false == m_StopRequested) foreach (TransportContext tc in TransportContexts) SendSendersReport(tc);
        }

        /// <summary>
        /// Send any <see cref="SendersReport"/>'s required by the given context immediately reguardless of <see cref="MaximumRtcpBandwidth"/>
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected virtual int SendSendersReport(TransportContext context, bool force = false)
        {
            //Determine if the SendersReport can be sent.
            if (IsDisposed //If the context is disposed
                && //AND the call has not been forced AND the context IsRtcpEnabled 
                (false == force && true == context.IsRtcpEnabled)
                // OR there is no RtcpSocket
                || context.RtcpSocket == null)
            {
                //Indicate nothing was sent
                return 0;
            }

            //Ensure the SynchronizationSourceIdentifier of the transportChannel is assigned
            context.AssignIdentity();

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
            if (false == IsDisposed && false == m_StopRequested) foreach (TransportContext tc in TransportContexts) SendReceiversReport(tc);
        }

        /// <summary>
        /// Send any <see cref="ReceiversReports"/>'s required by the given context immediately reguardless <see cref="MaximumRtcpBandwidth"/>
        /// Return the amount of bytes sent when sending the reports.
        /// </summary>
        /// <param name="context">The context</param>
        internal protected virtual int SendReceiversReport(TransportContext context, bool force = false)
        {
            //Determine if the ReceiversReport can be sent.
            if (IsDisposed //If the context is disposed
                && //AND the call has not been forced AND the context IsRtcpEnabled 
                (false == force && true == context.IsRtcpEnabled)
                // OR there is no RtcpSocket
                || context.RtcpSocket == null)
            {
                //Indicate nothing was sent
                return 0;
            }
            
            //Ensure the SynchronizationSourceIdentifier of the transportContext is assigned
            context.AssignIdentity();

            //create and store the receivers report sent
            context.ReceiversReport = TransportContext.CreateReceiversReport(context, false);

            //If the bandwidth is not exceeded also send a SourceDescription
            if (false == AverageRtcpBandwidthExceeded)
            {
                return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>().Concat((context.SourceDescription = TransportContext.CreateSourceDescription(context)).Yield()));
            }

            //Just send the ReceiversReport
            return SendRtcpPackets(context.ReceiversReport.Yield<RtcpPacket>());
        }

        /// <summary>
        /// Selects a TransportContext by matching the SynchronizationSourceIdentifier to the given sourceid
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns>The context which was identified or null if no context was found.</returns>
        internal protected virtual TransportContext GetContextBySourceId(int sourceId)
        {
            if (IsDisposed) return null;
            try
            {
                foreach (RtpClient.TransportContext tc in TransportContexts)
                    if (tc != null && tc.SynchronizationSourceIdentifier == sourceId || tc.RemoteSynchronizationSourceIdentifier == sourceId) return tc;
            }
            catch (InvalidOperationException) { return GetContextBySourceId(sourceId); }
            catch { if (false == IsDisposed) throw; }
            return null;
        }

        //DataChannel ControlChannel or overload?

        internal protected virtual TransportContext GetContextByChannel(byte channel)
        {
            if (IsDisposed) return null;
            try
            {
                foreach (RtpClient.TransportContext tc in TransportContexts)
                    if (tc.DataChannel == channel || tc.ControlChannel == channel) return tc;
            }
            catch (InvalidOperationException) { return GetContextByChannel(channel); }
            catch { if (false == IsDisposed) throw; }
            return null;
        }

        /// <summary>
        /// Selects a TransportContext by using the packet's Channel property
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        internal protected virtual TransportContext GetContextForPacket(RtcpPacket packet)
        {
            if (IsDisposed) return null;
            //Determine based on reading the packet this is where a RtcpReport class would be useful to allow reading the Ssrc without knownin the details about the type of report
            try { return GetContextBySourceId(packet.SynchronizationSourceIdentifier); }
            catch (InvalidOperationException) { return GetContextForPacket(packet); }
            catch { if (false == IsDisposed) throw; }
            return null;
        }

        public virtual void EnquePacket(RtcpPacket packet)
        {
            if (IsDisposed || m_StopRequested || packet == null || packet.IsDisposed) return;
            m_OutgoingRtcpPackets.Add(packet);
        }


        /// <summary>
        /// Sends the given packets, this function assumes all packets sent belong to the same party.
        /// </summary>
        /// <param name="packets"></param>
        /// <returns></returns>
        public virtual int SendRtcpPackets(IEnumerable<RtcpPacket> packets, out SocketError error, bool force = false)
        {

            error = SocketError.SocketError;

            if (IsDisposed || packets == null || packets.Count() == 0) return 0;

            TransportContext context = GetContextForPacket(packets.First());

            //If we don't have an transportContext to send on or the transportContext has not been identified or Rtcp is Disabled
            if (false == force && context == null || context.SynchronizationSourceIdentifier == 0 || false == context.IsRtcpEnabled)
            {
                //Return
                return 0;
            }

            //Todo Determine from Context to use control channel and length. (Check MediaDescription)

            //When sending more then one packet compound packets must be padded correctly.

            //Use ToCompoundBytes to ensure that all compound packets are correctly formed.
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

        public virtual int SendRtcpPackets(IEnumerable<RtcpPacket> packets)
        {
            SocketError error;
            return SendRtcpPackets(packets, out error);
        }

        internal virtual bool SendReports(TransportContext context, bool force = false)
        {
            SocketError error;
            return SendReports(context, out error, force);
        }

        /// <summary>
        /// Sends any <see cref="RtcpReport"/>'s immediately for the given <see cref="TransportContext"/> if <see cref="AverageRtcpBandwidthExceeded"/> is false.
        /// </summary>
        /// <param name="context">The <see cref="TransportContext"/> to send a report for</param>
        /// <param name="error"></param>
        /// <param name="force"></param>
        /// <returns>A value indicating if reports were sent</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal virtual bool SendReports(TransportContext context, out SocketError error, bool force = false)
        {
            //Ensure set
            error = SocketError.SocketError;

            //Check for the stop signal (or disposal)
            if (false == force && m_StopRequested || IsDisposed ||  //Otherwise
                false == context.IsRtcpEnabled
                || //Or Rtcp Bandwidth for this context or RtpClient has been exceeded
                context.RtcpBandwidthExceeded || AverageRtcpBandwidthExceeded
                || context.Goodbye != null) return false; //No reports can be sent.


            //If forced or the last reports were sent in less time than alloted by the m_SendInterval
            //Indicate if reports were sent in this interval
            return force || context.LastRtcpReportSent == Utility.InfiniteTimeSpan || context.LastRtcpReportSent > context.m_SendInterval ?
                 SendRtcpPackets(PrepareReports(context, true, true), out error) > 0
                 :
                 false;
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

            if (IsDisposed
                || 
                m_StopRequested 
                ||
                context.HasRtpActivity 
                ||
                context.HasRtcpActivity
                || //If the context has a continous flow OR the general Uptime is less then context MediaEndTime
                (false == context.IsContinious && Uptime < context.MediaEndTime))
            {
                return false;
            }

            //Calulcate for the currently inactive time period
            if (context.Goodbye == null &&
                false == context.HasAnyActivity)
            {
                //Set the amount of time inactive
                context.m_InactiveTime = DateTime.UtcNow - lastActivity;

                //Determine if the context is not inactive too long
                if (context.m_InactiveTime >= context.m_ReceiveInterval + context.m_SendInterval)
                {
                    //send a goodbye
                    SendGoodbye(context, null, context.SynchronizationSourceIdentifier);                    

                    //mark inactive
                    inactive = true;

                    //Disable further service
                    context.IsRtpEnabled = context.IsRtcpEnabled = false;
                }
                else if (context.m_InactiveTime >= context.m_ReceiveInterval + context.m_SendInterval)
                {
                    //send a goodbye but don't store it
                    inactive = SendGoodbye(context) <= 0;
                }
            }

            //indicate a goodbye was sent and a context is now inactive.
            return inactive;
        }

        #endregion

        #region Rtp
     
        public TransportContext GetContextForMediaDescription(Sdp.MediaDescription mediaDescription)
        {
            if (mediaDescription == null) return null;
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
        public TransportContext GetContextByPayloadType(int payloadType) { return TransportContexts.FirstOrDefault(c => c != null && false == c.IsDisposed && c.MediaDescription.PayloadTypes.Contains(payloadType)); }

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
            if (IsDisposed || m_StopRequested || packet == null || packet.IsDisposed) return;
            
            //Add a the packet to the outgoing
            m_OutgoingRtpPackets.Add(packet);
        }

        public void EnqueFrame(RtpFrame frame) { if (frame == null || frame.IsDisposed) return; foreach (RtpPacket packet in frame) EnquePacket(packet); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void SendRtpFrame(RtpFrame frame, out SocketError error, int? ssrc = null) { error = SocketError.SocketError; if (frame == null || frame.IsDisposed) return; foreach (RtpPacket packet in frame) SendRtpPacket(packet, out error, ssrc); }

        public void SendRtpFrame(RtpFrame frame, int? ssrc = null)
        {
            SocketError error;
            SendRtpFrame(frame, out error, ssrc);
        }

        /// <summary>
        /// Sends a RtpPacket to the connected client.
        /// </summary>
        /// <param name="packet">The RtpPacket to send</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public int SendRtpPacket(RtpPacket packet, out SocketError error, int? ssrc = null) //Should give SocketError and should be compatible with the Prepare signature.
        {
            error = SocketError.SocketError;

            if (packet == null || packet.IsDisposed || m_StopRequested) return 0;

            TransportContext transportContext = GetContextForPacket(packet);

            //If we don't have an transportContext to send on or the transportContext has not been identified
            if (transportContext == null) return 0;
            
            //If the mediaDescription of the context does not specify the packets payload type AND
            if (false == transportContext.MediaDescription.PayloadTypes.Contains(packet.PayloadType)
                && //The packet was addressed from or to a transportContext the client recognizes
                transportContext.SynchronizationSourceIdentifier == (ssrc ?? packet.SynchronizationSourceIdentifier))
            {
                //Throw an exception
                Common.ExceptionExtensions.RaiseTaggedException<RtpClient>(this, "Packet from '" + ssrc + "' PayloadType is different then the expected MediaDescription.MediaFormat Expected: '" + transportContext.MediaDescription.MediaFormat + "' Found: '" + packet.PayloadType + "'");
            }

            //How many bytes were sent
            int sent = 0;

            #region Unused [Sends a SendersReport if one was not already]

            //Send a SendersReport before any data is sent.
            //if (transportContext.SendersReport == null && transportContext.IsRtcpEnabled) SendSendersReport(transportContext);

            #endregion

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

        public int SendRtpPacket(RtpPacket packet, int? ssrc = null)
        {
            SocketError error;
            return SendRtpPacket(packet, out error, ssrc);
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
                //m_WorkerThread.Priority = ThreadPriority.AboveNormal;
                //m_WorkerThread.IsBackground = true;
                m_WorkerThread.Name = "RtpClient-" + InternalId;
                Started = DateTime.UtcNow;
                m_StopRequested = false;

                m_WorkerThread.Start();
            }
            catch (ObjectDisposedException) { return; }
            catch(Exception ex)
            {
                if (Logger != null) Logger.Log(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Sends the Rtcp Goodbye and signals a stop in the worker thread.
        /// </summary>
        public void Disconnect()
        {
            if (IsDisposed || false == IsConnected) return;

            SendGoodbyes();

            m_StopRequested = true;

            foreach (var tc in TransportContexts) if(tc.IsConnected) tc.DisconnectSockets();

            m_WorkerThread = null;
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
        int ReadRFC2326FrameHeader(int received, out byte frameChannel, out RtpClient.TransportContext context, ref int offset, out bool raisedEvent, byte[] buffer = null)
        {

            //There is no relevant TransportContext assoicated yet.
            context = null;

            //The channel of the frame - The Framing Method
            frameChannel = default(byte);

            raisedEvent = false;

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

                raisedEvent = true;

                //Indicate the amount of data consumed.
                return received;
            }
            else if (startOfFrame > offset) // If the start of the frame is not at the beginning of the buffer
            {
                //Determine the amount of data which belongs to the upper layer
                int upperLayerData = startOfFrame - mOffset;

                //System.Diagnostics.Debug.WriteLine("Moved To = " + startOfFrame + " Of = " + received + " - Bytes = " + upperLayerData + " = " + Encoding.ASCII.GetString(m_Buffer, mOffset, startOfFrame - mOffset));                

                OnInterleavedData(buffer, mOffset, upperLayerData);

                raisedEvent = true;

                //Indicate length from offset until next possible frame. (should always be positive, if somehow -1 is returned this will signal a end of buffer to callers)
                
                //If there is more data related to upperLayerData it will be evented in the next run. (See RtspClient ProcessInterleaveData notes)
                return upperLayerData;
            }

            //If there is not enough data for a frame header return
            if (mOffset + InterleavedOverhead > bufferLength) return -1;

            //Todo Determine from Context to use control channel and length. (Check MediaDescription)
            //NEEDS TO HANDLE CASES WHERE RFC4571 Framing are in play and no $ or Channel are used....

            //The amount of data needed for the frame
            frameLength = TryReadFrameHeader(buffer, mOffset, out frameChannel, true, BigEndianFrameControl, true);

            //Assign a context if there is a frame of any size
            if (frameLength >= 0)
            {
                //Assign the context
                context = GetContextByChannel(frameChannel);

                //Increase the result by the size of the header
                frameLength += InterleavedOverhead;
            }

            //Return the amount of bytes or -1 if any error occured.
            return frameLength;
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
            if (IsDisposed || data == null || socket == null) return 0;

            int sent = 0;

            try
            {
                int length = 0;
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

                //////Check for the socket to be writable in 1 msec or less
                ////if (false == socket.Poll((int)Utility.MicrosecondsPerMillisecond, SelectMode.SelectRead))
                ////{
                ////    //Indicate the operation has timed out
                ////    error = SocketError.TimedOut;

                ////    return sent;
                ////}

                //Send the frame keeping track of the bytes sent
                while (sent < length)
                {                    
                    //Send whatever can be sent
                    sent += socket.SendTo(data, sent, length - sent, SocketFlags.None, remote);
                }
            
                //Success
                error = SocketError.Success;

                return sent;
            }
            catch (SocketException ex)
            {
                error = (SocketError)ex.ErrorCode;

                return sent;
            }
            catch
            {
                //Something bad happened, usually disposed already
                return sent;
            }
        }

        /// <summary>
        /// Recieves data on a given socket and endpoint
        /// </summary>
        /// <param name="socket">The socket to receive data on</param>
        /// <returns>The number of bytes recieved</returns>             
        internal protected virtual int ReceiveData(Socket socket, ref EndPoint remote, out SocketError error, bool expectRtp = true, bool expectRtcp = true)
        {
            //Nothing bad happened yet.
            error = SocketError.SocketError;

            //Ensure the socket can poll
            if (IsDisposed || m_StopRequested || socket == null || m_Buffer.IsDisposed || remote == null) return 0;

            bool tcp = socket.ProtocolType == ProtocolType.Tcp;

            //Cache the offset at the time of the call
            int offset = m_Buffer.Offset, received = 0;

            try
            {
                received = socket.ReceiveFrom(m_Buffer.Array, offset, m_Buffer.Count, SocketFlags.None, ref remote);

                error = SocketError.Success;

                //If the receive was a success
                if (received > 0)
                {

                    //Under TCP use Framing to obtain the length of the packet as well as the context.
                    if (tcp) return ProcessFrameData(m_Buffer.Array, offset, received, socket);

                    //Use the data received to parse and complete any recieved packets, should take a parseState
                    using (var memory = new Common.MemorySegment(m_Buffer.Array, offset, received)) ParseAndCompleteData(memory, expectRtcp, expectRtp);
                }

            }
            catch (SocketException se)
            {
                error = (SocketError)se.ErrorCode;

                return received;
            }
            catch { throw; }

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

            //Determine which TransportContext will receive the data incoming
            TransportContext relevent = null;

            //The channel of the data
            byte frameChannel = 0;

            //Get the length of the given buffer (Should actually use m_Buffer.Count when using our own buffer)
            int bufferLength = buffer.Length,
                //The indicates length of the data
                frameLength = 0,
                //The amount of data remaining in the buffer
                remainingInBuffer = Utility.Clamp(count, count, bufferLength - offset),
                //The amount of data received (which is already equal to what is remaining in the buffer)
                recievedTotal = remainingInBuffer;

            //Determine if Rtp or Rtcp is coming in or some other type (could be combined with expectRtcp and expectRtp == false)
            bool expectRtp = false, expectRtcp = false, incompatible = true, raisedEvent = false;

            //If anything remains on the socket the value will be calulcated.
            int remainingOnSocket = 0;

            //TODO handle receiving when no $ and Channel is presenent... e.g. RFC4571
            //Would only be 2 then...
            int sessionRequired = InterleavedOverhead;

            //While not disposed and there is data remaining (within the buffer)
            while (false == IsDisposed &&
                remainingInBuffer > 0 &&
                offset < bufferLength)
            {
                //Assume not rtp or rtcp and that the data is compatible with the session
                expectRtp = expectRtcp = incompatible = false;

                //If a header can be read
                if (remainingInBuffer >= sessionRequired)
                {
                    //Determine if an event was raised each time there was at least the required amount of data.
                    raisedEvent = false;

                    //Parse the frameLength from the given buffer, take changes to the offset through the function.
                    frameLength = ReadRFC2326FrameHeader(remainingInBuffer, out frameChannel, out relevent, ref offset, out raisedEvent, buffer);

                    //If a frame was found (Including the null packet)
                    if (frameLength >= 0)
                    {
                        #region Babble

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

                        #endregion

                        //If there WAS a context
                        if (relevent != null)
                        {
                            //Verify minimum and maximum packet sizes allowed by context. (taking into account the amount of bytes in the ALF)
                            if (frameLength < relevent.MinimumPacketSize + sessionRequired ||
                                frameLength > relevent.MaximumPacketSize + sessionRequired)
                            {
                                //mark as incompatible
                                incompatible = true;

                                //ToDo
                                //Make CreateLogString function

                                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "ProcessFrameData - Irregular Packet of " + frameLength + " for Channel " + frameChannel + " remainingInBuffer=" + remainingInBuffer);

                                //jump
                                goto CheckPacketAttributes;
                            }

                            //TODO Independent framing... (e.g. no $)[ only 4 bytes not 6 ]
                            //If all that remains is the frame header then receive more data. 6 comes from (InterleavedOverhead + CommonHeaderBits.Size)
                            //We need more data to be able to verify the frame.
                            if (remainingInBuffer <= 6)
                            {
                                //Remove the context
                                relevent = null;

                                goto CheckRemainingData;

                                ////Only receive this many more bytes for now.
                                //remainingOnSocket = X - remainingInBuffer;

                                ////Receive the rest of the data indicated by frameLength. (Should probably only receive up to X more bytes then make another receive if needed)
                                //goto GetRemainingData;
                            }

                            //Use CommonHeaderBits on the data after the Interleaved Frame Header
                            using (var common = new Media.RFC3550.CommonHeaderBits(buffer[offset + InterleavedOverhead], buffer[offset + InterleavedOverhead + 1]))
                            {
                                //Check the version...
                                incompatible = common.Version != relevent.Version;

                                //If this is a valid context there must be at least a RtpHeader's worth of data in the buffer. 
                                //If this was a RtcpPacket with only 4 bytes it wouldn't have a ssrc and wouldn't be valid to be sent.
                                if (false == incompatible &&
                                    (frameChannel == relevent.DataChannel &&
                                    remainingInBuffer <= Rtp.RtpHeader.Length + sessionRequired)
                                    ||
                                    (frameChannel == relevent.ControlChannel &&
                                    remainingInBuffer <= Rtcp.RtcpHeader.Length + sessionRequired))
                                {
                                    //Remove the context
                                    relevent = null;

                                    goto EndUsingHeader;

                                    ////Only receive this many more bytes for now.
                                    //remainingOnSocket = 16 - remainingInBuffer;

                                    ////Receive the rest of the data indicated by frameLength. (Should probably only receive up to 6 more bytes then make another receive if needed)
                                    //goto GetRemainingData;
                                }


                                //Perform a set of checks and set weather or not Rtp or Rtcp was expected.                                  
                                if (false == incompatible)
                                {
                                    //Determine if the packet is Rtcp by looking at the expected channel and the relvent control channel
                                    if (expectRtcp = frameChannel == relevent.ControlChannel)
                                    {
                                        //Rtcp

                                        //Could check payload type 

                                        //Store any rtcp length so we can verify its not 0 and then additionally ensure its value is not larger then the frameLength
                                        int rtcpLen;

                                        using (Rtcp.RtcpHeader header = new RtcpHeader(buffer, offset + InterleavedOverhead))
                                        {
                                            //Get the length in 'words' (by adding one)
                                            //A length of 0 means 1 word
                                            //A length of 65535 means only the header (no ssrc [or payload])
                                            ushort lengthInWordsPlusOne = (ushort)(header.LengthInWordsMinusOne + 1);

                                            //Convert to bytes
                                            rtcpLen = lengthInWordsPlusOne * 4;

                                            //Check that the supposed  amount of contained words is greater than or equal to the frame length conveyed by the application layer framing
                                            incompatible = rtcpLen > frameLength;

                                            //if rtcpLen >= ushort.MaxValue the packet spans multiple segments unless a large buffer is used.

                                            if (false == incompatible && //It was not already ruled incomaptible
                                                lengthInWordsPlusOne > 0 && //If there is supposed to be SSRC in the packet
                                                header.Size > Rtcp.RtcpHeader.Length && //The header ACTUALLY contains enough bytes to have a SSRC
                                                false == relevent.InDiscovery)//The remote context knowns the identity of the remote stream                                                 
                                            {
                                                //Perform another lookup and check compatibility
                                                incompatible = (GetContextBySourceId(header.SendersSynchronizationSourceIdentifier)) == null;
                                            }
                                        }
                                    }

                                    //Determine if the packet is Rtp by looking at the expected channel and the relvent data channel
                                    if (expectRtp = frameChannel == relevent.DataChannel)
                                    {
                                        //Rtp

                                        //Check the PayloadType (may overlap with other streams but not RTCP)
                                        //Determine compatibility with the context expected
                                        incompatible = GetContextByPayloadType(common.RtpPayloadType) != relevent;

                                        //If the packet was incompatible (due to mis matched RtpPayloadType)
                                        //OR the context is not discovering the identity check the SSRC.
                                        if (incompatible || false == relevent.InDiscovery)
                                        {
                                            using (Rtp.RtpHeader header = new RtpHeader(buffer, offset + InterleavedOverhead))
                                            {
                                                //The context was obtained by the frameChannel
                                                //Use the SSRC to determine where it should be handled.
                                                //If there is no context the packet is incompatible
                                                incompatible = (GetContextBySourceId(header.SynchronizationSourceIdentifier)) == null;

                                                //(Could also check SequenceNumber to prevent duplicate packets from being processed.)

                                                ////Verify extensions (handled by ValidatePacket)
                                                //if (header.Extension)
                                                //{

                                                //}

                                            }
                                        }
                                    }
                                }
                            EndUsingHeader:
                                ;
                            }
                        }

                        //Log state.
                        //if (relevent == null) Media.Common.ILoggingExtensions.Log(Logger, InternalId + "-ProcessFrameData - No Context for Channel " + frameChannel + " frameLength=" + frameLength + " remainingInBuffer=" + remainingInBuffer);
                        //else Media.Common.ILoggingExtensions.Log(Logger, InternalId + "ProcessFrameData " + frameChannel + " frameLength=" + frameLength + " remainingInBuffer=" + remainingInBuffer);
                        
                    CheckPacketAttributes:

                        if (incompatible)
                        {
                            //If there was a context then incrment for failed receptions
                            if (relevent != null)
                            {
                                if (expectRtp) ++relevent.m_FailedRtpReceptions;

                                if (expectRtcp) ++relevent.m_FailedRtcpReceptions;
                            }

                            Media.Common.ILoggingExtensions.Log(Logger, InternalId + "ProcessFrameData - Incompatible Packet frameLength=" + frameLength + " for Channel " + frameChannel + " remainingInBuffer=" + remainingInBuffer);
                        }
                        //If frameLength was 0 or the frame was larger than we can store then interleave the header for handling if required   
                        //incompatible may not be true here.
                        else if (frameLength == 0 || frameLength > bufferLength)
                        {
                            //Could check incompatible to determine if to should move further.

                            //Just because there is no assoicated context on the client does not mean the packet is not useful elsewhere in Transport.

                            //TODO It may be possible to let the event reiever known how much is available here.
                            if (frameLength == 0)
                            {
                                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "ProcessFrameData - Null Packet for Channel " + frameChannel + " remainingInBuffer=" + remainingInBuffer);
                            }
                            else //If there was a context then increment for failed receptions only for large packets
                            {
                                if (expectRtp) ++relevent.m_FailedRtpReceptions;

                                if (expectRtcp) ++relevent.m_FailedRtcpReceptions;

                                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "ProcessFrameData - Large Packet of " + frameLength + " for Channel " + frameChannel + " remainingInBuffer=" + remainingInBuffer);
                            }
                        }
                        else goto CheckRemainingData;

                        //The packet was incompatible or larger than the buffer

                        //Determine how much we can move
                        int toMove = Math.Min(remainingInBuffer, sessionRequired);

                        //TODO It may be possible to let the event reiever known how much is available here.

                        //Indicate what was received if not already done
                        if (false == raisedEvent) OnInterleavedData(buffer, offset, toMove);

                        //Move the offset
                        offset += toMove;

                        //Decrease by the length
                        remainingInBuffer -= toMove;

                        //Do another pass
                        continue;

                    }//else there was a frameLength of -1 this indicates there is not enough bytes for a header.
                }
                else//There is not enough data in the buffer as defined by sessionRequired.
                {
                    //unset the frameLength read
                    frameLength = -1;

                    //unset the context read
                    relevent = null;
                }

            //At this point there may be either less sessionRequired or not enough for a complete frame.
            CheckRemainingData:

                //See how many more bytes are required from the wire
                //If the frameLength is less than 0 AND there are less then are sessionRequired remaining in the buffer
                remainingOnSocket = frameLength < 0 && remainingInBuffer < sessionRequired ?
                    sessionRequired - remainingInBuffer //Receive enough to complete the header
                        : //Otherwise if the frameLength larger then what remains in the buffer allow for the buffer to be filled or nothing else remains.
                    frameLength > remainingInBuffer ? frameLength - remainingInBuffer : 0;

                //GetRemainingData:

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
                    while (false == IsDisposed && remainingOnSocket > 0)
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
                    if (relevent == null) continue;
                }

                //If there any data in the frame and there is a relevent context
                if (false == IsDisposed && frameLength > 0)
                {
                    //If there was a context
                    if (relevent != null)
                    {
                        //Parse the data in the buffer
                        using (var memory = new Common.MemorySegment(buffer, offset + InterleavedOverhead, frameLength - InterleavedOverhead)) ParseAndCompleteData(memory, expectRtcp, expectRtp, memory.Count);
                    }

                    //Decrease remaining in buffer
                    remainingInBuffer -= frameLength;

                    //Move the offset
                    offset += frameLength;

                    //Ensure large frames are completely received by receiving the rest of the frame now. (this only occurs for packets being skipped)
                    if (frameLength > bufferLength)
                    {
                        //Remove the context
                        relevent = null;

                        //Determine how much remains
                        remainingOnSocket = frameLength - bufferLength;

                        //If there is anything left
                        if (remainingOnSocket > 0)
                        {
                            //Set the new length of the frame based on the length of the buffer
                            frameLength -= bufferLength;

                            //Set what is remaining
                            remainingInBuffer = 0;

                            //Use all the buffer
                            offset = m_Buffer.Offset;

                            //go to receive it
                            goto CheckRemainingData;
                        }
                    }
                }
            }

            //Handle any data which remains if not already
            if (false == raisedEvent && remainingInBuffer > 0)
            {
                OnInterleavedData(buffer, offset, remainingInBuffer);
            }

            //Return the number of bytes recieved
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
                    //Just use the payload type to avoid confusion, payload types for Rtcp and Rtp cannot and should not overlap
                    parseRtcp = !(parseRtcp = GetContextByPayloadType(header.RtpPayloadType) != null);

                    //Could also lookup the ssrc
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
            //Don't worry about overflow.
            unchecked
            {
            Begin:
                try
                {

                    DateTime lastOperation = DateTime.UtcNow;

                    SocketError lastError = SocketError.SocketError;

                    //Until aborted
                    while (false == m_StopRequested)
                    {
                        //Keep how much time has elapsed thus far
                        TimeSpan taken = DateTime.UtcNow - lastOperation;

                        //Stop if nothing has happed in at least 5 seconds
                        m_StopRequested = taken > DefaultReportInterval;

                        #region Recieve Incoming Data

                        //Loop each context, newly added contexts will be seen on each iteration
                        for (int i = 0, e = TransportContexts.Count; false == m_StopRequested && i < e; ++i)
                        {
                            //If there are no contexts
                            if (e == 0)
                            {
                                //Relinquish priority
                                System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                                //yeild the time slice
                                System.Threading.Thread.Sleep(0);

                                //Do another iteration
                                continue;
                            }

                            if (i >= TransportContexts.Count) continue;

                            //Obtain a context
                            TransportContext tc = TransportContexts[i];

                            //Check for a context which is able to receive data
                            if (tc == null || tc.IsDisposed || false == tc.IsConnected
                                ||//If the context does not have continious media it must only receive data for the duration of the media.
                                false == tc.IsContinious && tc.TimeRemaining < TimeSpan.Zero ||
                                tc.Goodbye != null) continue;

                            //Receive Data on the RtpSocket and RtcpSocket, summize the amount of bytes received from each socket.

                            //Reset the error.
                            lastError = SocketError.SocketError;

                            //Ensure priority is above normal
                            System.Threading.Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                            int receivedRtp = 0, receivedRtcp = 0;

                            bool duplexing = tc.IsDuplexing, rtpEnabled = tc.IsRtpEnabled, rtcpEnabled = tc.IsRtcpEnabled;

                            //If receiving Rtp and the socket is able to read
                            if (false == m_StopRequested && rtpEnabled
                                //Check if the socket can read data
                                && tc.RtpSocket.Poll((int)Math.Round(tc.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                            {
                                //Receive RtpData
                                receivedRtp += ReceiveData(tc.RtpSocket, ref tc.RemoteRtp, out lastError, rtpEnabled, duplexing);

                                //Check if an error occured
                                if (lastError != SocketError.Success)
                                {
                                    //Increment for failed receptions
                                    ++tc.m_FailedRtpReceptions;

                                    //Determine if the socket is out of sync
                                    if (lastError == SocketError.ConnectionAborted || lastError == SocketError.ConnectionReset)
                                    {
                                        System.Threading.Thread.Sleep(0);
                                    }
                                }
                                else //If anything was received, even 0 bytes then the context is active
                                    if (receivedRtp >= 0) lastOperation = DateTime.UtcNow;
                            }

                            //if Rtcp is enabled
                            if (false == m_StopRequested && rtcpEnabled)
                            {
                                if (//The last report was never received or recieved longer ago then required
                                    (tc.LastRtcpReportReceived == Utility.InfiniteTimeSpan || tc.LastRtcpReportReceived >= tc.m_ReceiveInterval)
                                    &&//And the socket can read
                                    tc.RtcpSocket.Poll((int)Math.Round(tc.m_ReceiveInterval.TotalMicroseconds(), MidpointRounding.ToEven), SelectMode.SelectRead))
                                {
                                    //ReceiveRtcp Data
                                    receivedRtcp += ReceiveData(tc.RtcpSocket, ref tc.RemoteRtcp, out lastError, duplexing, rtcpEnabled);

                                    //Check if an error occured
                                    if (lastError != SocketError.Success)
                                    {
                                        //Increment for failed receptions
                                        ++tc.m_FailedRtcpReceptions;

                                        //Determine if the socket is out of sync
                                        if (lastError == SocketError.ConnectionAborted || lastError == SocketError.ConnectionReset)
                                        {
                                            System.Threading.Thread.Sleep(0);
                                        }
                                    }
                                    else if (receivedRtcp >= 0) lastOperation = DateTime.UtcNow;
                                }

                                //Try to send reports for the latest packets or a goodbye if inactive.
                                if (SendReports(tc, out lastError) || SendGoodbyeIfInactive(lastOperation, tc)) lastOperation = DateTime.UtcNow;

                                //Log when not default or success
                                if (lastError != SocketError.SocketError && lastError != SocketError.Success)
                                {
                                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "SocketError = " + lastError + " lastOperation = " + lastOperation + " taken = " + taken);
                                }
                            }
                        }

                        //If there are no outgoing packets
                        if (m_OutgoingRtcpPackets.Count + m_OutgoingRtpPackets.Count == 0)
                        {
                            //Check if not already lowest priority
                            if (System.Threading.Thread.CurrentThread.Priority != ThreadPriority.Lowest)
                            {
                                //Relinquish priority
                                System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                                continue;
                            }
                            else
                            {
                                //Waste time
                                System.Threading.Thread.Sleep(0);

                                continue;
                            }
                        }                        

                        #endregion

                        #region Handle Outgoing RtcpPackets

                        int remove = m_OutgoingRtcpPackets.Count;

                        if (remove > 0)
                        {
                            //Todo, do a TakeWhile and sort by something which will allow packets which have different parties or channels.

                            //Try and send the lot of them
                            if (SendRtcpPackets(m_OutgoingRtcpPackets.Take(remove)) > 0)
                            {
                                lastOperation = DateTime.UtcNow;

                                //Remove what was attempted to be sent (don't try to send again)
                                m_OutgoingRtcpPackets.RemoveRange(0, remove);
                            }
                        }

                        #endregion

                        #region Handle Outgoing RtpPackets

                        remove = m_OutgoingRtpPackets.Count;

                        if (remove > 0)
                        {
                            //Could check for timestamp more recent then packet at 0  on transporContext and discard...
                            //Send only A few at a time to share with rtcp

                            //If more than 1 thread is accessing this logic one could declare another varaible to compare what was supposed to be removed with what is actually being removed.
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
                                if (packet == null || packet.IsDisposed || sendContext == null || sendContext.IsDisposed || sendContext.Goodbye != null)
                                {
                                    ++remove;

                                    continue;
                                }

                                SocketError error;
                                                                
                                if (SendRtpPacket(packet, out error, sendContext.SynchronizationSourceIdentifier) >= packet.Length && 
                                    error == SocketError.Success)
                                {
                                    ++remove;

                                    lastOperation = DateTime.UtcNow;
                                }
                                else
                                {

                                    //There was an error sending the packet

                                    //Only do this in TCP to avoid duplicate data retransmission?

                                    //Indicate to remove another packet
                                    //++remove;

                                    break;
                                }

                                //If this was a marker packet then stop for now
                                if (packet.Marker) break;

                                //Could also check timestamp in cases where marker is not being set
                                //if (lastTimestamp.HasValue && packet.Timestamp != lastTimestamp) break;
                                //lastTimestamp = packet.Timestamp;
                            }

                            //If any packets should be removed remove them now
                            if (remove > 0) m_OutgoingRtpPackets.RemoveRange(0, remove);
                        }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    //Check for the Thread being aborted.
                    if (m_StopRequested = ex is ThreadAbortException)
                    {
                        //Handle that
                        Thread.ResetAbort();

                        //Return
                        return;
                    }

                    //Check that a stop has not been requested
                    if (false == m_StopRequested)
                    {
                        //Sleep away atleast 1msec
                        System.Threading.Thread.Sleep(Utility.Clamp(TransportContexts.Count, 1, TransportContexts.Count));

                        //Go to the top of the loop
                        goto Begin;
                    }
                }
            }
        }

        #endregion

        #region Overloads

        public override string ToString()
        {
            return string.Join(((char)Common.ASCII.HyphenSign).ToString(), base.ToString(), InternalId);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Calls <see cref="Disconnect"/> and disposes all contained <see cref="RtpClient.TransportContext"/>.
        /// Stops the raising of any events.
        /// Removes the Logger
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            Disconnect();

            base.Dispose();

            //Dispose contexts
            foreach (TransportContext tc in TransportContexts) tc.Dispose();
            
            //Counters go away with the transportChannels
            TransportContexts.Clear();

            //Remove my handler (should be done when set to null anyway)
            RtpPacketSent -= new RtpPacketHandler(HandleRtpPacketSent);
            RtcpPacketSent -= new RtcpPacketHandler(HandleRtcpPacketSent);

            //Stop raising events
            RtpPacketSent = null;
            RtcpPacketSent = null;
            RtpPacketReceieved = null;
            RtcpPacketReceieved = null;
            InterleavedData = null;

            //Send abort signal
            Utility.TryAbort(ref m_WorkerThread);

            DisableFeedbackReports(this);

            //Empty packet buffers
            m_OutgoingRtpPackets.Clear();
            m_OutgoingRtcpPackets.Clear();

            //Remove the buffer
            m_Buffer.Dispose();
            m_Buffer = null;
            
            //Unset the logger
            Logger = null;
        }

        #endregion

        IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads() { return IsDisposed ? null : Utility.Yield(m_WorkerThread); }

        /// <summary>
        /// Gets any sockets which are utilized by this RtpClient.
        /// If a socket is used more than one time it will be given that many times.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
        {
            if (IsDisposed) yield break;

            //Determine if a Unique parameter should be given but the caller can use Distinct() anyway
           // HashSet<Socket> known = new HashSet<Socket>();

            foreach (TransportContext tc in TransportContexts)
            {
                if (tc.IsDisposed) continue;

                foreach (Socket referenced in ((Common.ISocketReference)tc).GetReferencedSockets())
                {
                    if (referenced == null) continue;

                    yield return referenced;
                }
            }
        }
    }
}
