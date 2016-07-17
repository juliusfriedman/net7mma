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
using Media.Common;
using Media.Rtcp;
using System.Net.Sockets;
using System.Net;
using System.Threading;

//Todo, Provide a RtpConference class or integrate the capability to send and recieve to multiple parties

//-2653043566447805112

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
    public partial class RtpClient : Common.BaseDisposable, Media.Common.IThreadReference
    {
        #region Nested Types

        /// <summary>
        ///Contains the information and assets relevent to each stream in use by a RtpClient
        /// </summary>
        public class TransportContext : Common.SuppressedFinalizerDisposable, Common.ISocketReference
        {
            #region Statics

            //Todo
            internal static byte[] CreateApplicationLayerFraming(TransportContext context)
            {
                //Determine  how many bytes, independent uses 2 where as rtsp uses 4

                //Determine if RFC4571 via the Connection line etc.

                int size = InterleavedOverhead;

                byte[] result = new byte[size];

                return result;
            }

            //ReadApplictionLayerFraming should be here also...

            //The virtuals could probably be moved here such as PrepareReports etc.

            internal static void ConfigureRtpRtcpSocket(Socket socket) //,Common.ILogging = null
            {
                if (socket == null) throw new ArgumentNullException("socket");

                Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(socket));

                //RtpSocket.Blocking = false;

                //RtpSocket.SendBufferSize = RtpSocket.ReceiveBufferSize = 0; //Use local buffer dont copy

                //IP Options for InterNetwork
                if (socket.AddressFamily == AddressFamily.InterNetwork)
                {
                    //http://en.wikipedia.org/wiki/Type_of_service
                    //CS5,EF	40,46	5 :Critical - mainly used for voice RTP
                    //40 || 46 is used for RTP Audio per Wikipedia
                    //48 is Internetwork Control
                    //56 is Network Control
                    //Set type of service

                    Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 47));

                    //Tell the network stack what we send and receive has an order
                    Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.DontFragment = true);
                }                

                //Don't buffer sending
                Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.SendBufferSize = 0);
                
                if (socket.ProtocolType == ProtocolType.Tcp)
                {
                    //Retransmit for 0 sec
                    if (Common.Extensions.OperatingSystemExtensions.IsWindows)
                    {
                        //Disable Retransmission
                        Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.DisableTcpRetransmissions(socket));

                        // Enable No Syn Retries
                        Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.EnableTcpNoSynRetries(socket));

                        // Set OffloadPreferred
                        Media.Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.SetTcpOffloadPreference(socket));

                    }

                    //Don't buffer receiving
                    Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.ReceiveBufferSize = 0);
                    

                    //If both send and receieve buffer size are 0 then there is no coalescing when nagle's algorithm is disabled
                    Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => Media.Common.Extensions.Socket.SocketExtensions.DisableTcpNagelAlgorithm(socket));
                }
                else if (socket.ProtocolType == ProtocolType.Udp)
                {
                    //Set max ttl for slower networks
                    Common.Extensions.Exception.ExceptionExtensions.ResumeOnError(() => socket.Ttl = 255);

                    //May help if behind a router
                    //Allow Nat Traversal
                    //RtpSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                }

                //Todo, set RecieveTimeout.. (done in From methods)

                //socket.SendTimeout = 1;
                //socket.ReceiveTimeout = 1;
            }

            public static TransportContext FromMediaDescription(Sdp.SessionDescription sessionDescription, 
                byte dataChannel, byte controlChannel, 
                Sdp.MediaDescription mediaDescription, 
                bool rtcpEnabled = true, int remoteSsrc = 0, int minimumSequentialpackets = 2, 
                IPAddress localIp = null, IPAddress remoteIp = null, 
                int? rtpPort = null, int? rtcpPort = null, 
                bool connect = false, 
                Socket existingSocket = null, 
                Action<Socket> configure = null)
            {
                //Must have a mediaDescription
                if (Common.IDisposedExtensions.IsNullOrDisposed(mediaDescription)) throw new ArgumentNullException("mediaDescription");

                //If there is no sdp there must be a local and remoteIp
                if (Common.IDisposedExtensions.IsNullOrDisposed(sessionDescription) && (object.ReferenceEquals(localIp, null) || object.ReferenceEquals(remoteIp, null))) throw new InvalidOperationException("Must have a sessionDescription or the localIp and remoteIp cannot be established.");

                //If no remoteIp was given attempt to parse it from the sdp
                if (object.ReferenceEquals(remoteIp, null))
                {
                    Sdp.SessionDescriptionLine cLine = mediaDescription.ConnectionLine;

                    //Try the sesion level if the media level doesn't have one
                    if (object.ReferenceEquals(cLine, null)) cLine = sessionDescription.ConnectionLine;

                    //Attempt to parse the IP, if failed then throw an exception.
                    if (object.ReferenceEquals(cLine, null)
                        ||
                        false.Equals(IPAddress.TryParse(new Sdp.Lines.SessionConnectionLine(cLine).Host, out remoteIp))) throw new InvalidOperationException("Cannot determine remoteIp from ConnectionLine");
                }                

                //For AnySourceMulticast the remoteIp would be a multicast address.
                bool multiCast = System.Net.IPAddress.Broadcast.Equals(remoteIp) || Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(remoteIp);

                //If no localIp was given determine based on the remoteIp
                //--When there is no remoteIp this should be done first to determine if the sender is multicasting.
                if (object.ReferenceEquals(localIp, null)) localIp = multiCast ? Media.Common.Extensions.Socket.SocketExtensions.GetFirstMulticastIPAddress(remoteIp.AddressFamily) : Media.Common.Extensions.Socket.SocketExtensions.GetFirstUnicastIPAddress(remoteIp.AddressFamily);

                //The localIp and remoteIp should be on the same network otherwise they will need to be mapped or routed.
                //In most cases this can be mapped.
                if (false.Equals(localIp.AddressFamily == remoteIp.AddressFamily)) throw new InvalidOperationException("local and remote address family must match, please create an issue and supply a capture.");

                //Todo, need TTL here.

                //Should also probably store the network interface.

                int ttl = 255;

                //If no remoteSsrc was given then check for one
                if (remoteSsrc.Equals(0))
                {
                    //Check for SSRC Attribute Line on the Media Description
                    //a=ssrc:<ssrc-id> <attribute>
                    //a=ssrc:<ssrc-id> <attribute>:<value>

                    Sdp.SessionDescriptionLine ssrcLine = mediaDescription.SsrcLine;

                    //To use typed line

                    if (object.ReferenceEquals(ssrcLine,null).Equals(false))
                    {
                        string part = ssrcLine.GetPart(1);

                        if (false == string.IsNullOrWhiteSpace(part))
                        {
                            remoteSsrc = part[0] == '-' ? (int)uint.Parse(part) : int.Parse(part);
                        }
                    }
                }

                //Create the context
                TransportContext tc = new TransportContext(dataChannel, controlChannel, RFC3550.Random32(Media.Rtcp.SourceDescriptionReport.PayloadType), mediaDescription,
                    rtcpEnabled, remoteSsrc, minimumSequentialpackets);

                int reportReceivingEvery = 0, 
                    reportSendingEvery = 0, 
                    asData = 0;

                //If rtcp is enabled
                if (rtcpEnabled)
                {
                    //Set to the default interval
                    reportSendingEvery = reportReceivingEvery = (int)DefaultReportInterval.TotalMilliseconds;

                    //Todo should be using the BandwidthLine type and IsDisabled property of instance
                    //Then would have access to BandwidthTypeString on instance also.

                    //If any bandwidth lines were parsed
                    if (Media.Sdp.Lines.SessionBandwidthLine.TryParseBandwidthDirectives(mediaDescription, out reportReceivingEvery, out reportSendingEvery, out asData))
                    {
                        //Determine if rtcp is disabled in the media description
                        bool rtcpDisabled = reportReceivingEvery.Equals(0) && reportSendingEvery.Equals(0);

                        //If Rtcp is not disabled then this will set the read and write timeouts.
                        if (false.Equals(rtcpDisabled))
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

                            if (reportReceivingEvery > 0) tc.m_ReceiveInterval = TimeSpan.FromSeconds(reportReceivingEvery / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);

                            if (reportSendingEvery > 0) tc.m_SendInterval = TimeSpan.FromSeconds(reportSendingEvery / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond);

                            //Todo
                            //Should set MaximumRtcpBandwidthPercentage

                            //Todo, should set Send and ReceiveTimeout

                            //Todo, specify Report Interval seperately..

                        }//Disable rtcp (already checked to be enabled)
                        //else if (rtcpEnabled) tc.IsRtcpEnabled = false;
                    }
                }

                //Check Time Description? use start and end rather than range? (Only if not 0 0) then...

                //var timeDesc = sessionDescription.TimeDescriptions.FirstOrDefault();

                //if (timeDesc != null)
                //{
                //    ///
                //}

                //check for range in mediaDescription

                //Another hacky way would be to simply leave EndTime null.... 

                var rangeInfo = mediaDescription.RangeLine ?? (Common.IDisposedExtensions.IsNullOrDisposed(sessionDescription).Equals(false) ? sessionDescription.RangeLine : null);

                if (object.ReferenceEquals(rangeInfo, null).Equals(false) && rangeInfo.m_Parts.Count > 0)
                {
                    string type;

                    Media.Sdp.SessionDescription.TryParseRange(rangeInfo.m_Parts.First(), out type, out tc.m_StartTime, out tc.m_EndTime);
                }

                //https://www.ietf.org/rfc/rfc3605.txt

                //rtcpAttribute indicates if RTCP should use a special port and not be dervied from the RtpPort algorithmically 

                //"a=rtcp:" 

                /*
                 
                  Example encodings could be:

                    m=audio 49170 RTP/AVP 0
                    a=rtcp:53020

                    m=audio 49170 RTP/AVP 0
                    a=rtcp:53020 IN IP4 126.16.64.4

                    m=audio 49170 RTP/AVP 0
                    a=rtcp:53020 IN IP6 2001:2345:6789:ABCD:EF01:2345:6789:ABCD
                 
                 */

                Sdp.SessionDescriptionLine rtcpLine = mediaDescription.RtcpLine;

                if (object.ReferenceEquals(rtcpLine,null).Equals(false))
                {
                    //Todo...

                    throw new NotImplementedException("Make a thread if you need rtcp AttributeField support immediately.");
                }

                //rtcp-mux is handled in the Initialize call

                //tc.ConfigureSocket = given;

                //Todo, should verify ports against PortRange when HasPortRange == true.

                //if (mediaDescription.PortRange.HasValue) //mediaDescription.HasPortRange
                //{

                //}

                //Handle connect
                if (connect)
                {
                    //Determine if a socket was given or if it will be created.
                    bool hasSocket = object.ReferenceEquals(existingSocket, null).Equals(false);

                    //If a configuration has been given then set that configuration in the TransportContext.
                    if (object.ReferenceEquals(configure, null).Equals(false)) tc.ConfigureSocket = configure;

                    //Check for udp if no existing socket was given
                    if (hasSocket.Equals(false) && string.Compare(mediaDescription.MediaProtocol, Media.Rtp.RtpClient.RtpAvpProfileIdentifier, true) == 0)
                    {
                        //Find a local port
                        int localPort = Media.Common.Extensions.Socket.SocketExtensions.ProbeForOpenPort(ProtocolType.Udp);

                        if (localPort < 0) throw new ArgumentOutOfRangeException("Cannot find an open port.");

                        //Create the sockets and connect
                        tc.Initialize(localIp, remoteIp, //LocalIP, RemoteIP
                            localPort.Equals(0) ? localPort : localPort++, //LocalRtp
                            localPort.Equals(0) ? localPort : localPort++, //LocalRtcp                            
                            rtpPort ?? mediaDescription.MediaPort, //RemoteRtp
                            rtcpPort ?? (false.Equals(mediaDescription.MediaPort.Equals(0)) ? mediaDescription.MediaPort + 1 : mediaDescription.MediaPort)); //RemoteRtcp
                    }
                    else if (hasSocket)//If had a socket use it
                    {
                        tc.Initialize(existingSocket);
                    }
                    else //Create the sockets and connect (TCP)
                    {
                        tc.Initialize(localIp, remoteIp, rtpPort ?? mediaDescription.MediaPort);
                    }

                    //Needs ttl here.

                    //Should also check for the ConnectionAddress even if remoteIp was given...

                    if (multiCast)
                    {
                        //remoteIp should be groupAdd from media c= line.

                        //If the address cannot be joined then an exception will occur here.
                        try
                        {

                            Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(tc.RtpSocket, remoteIp);

                            Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(tc.RtpSocket, ttl);

                            if (rtcpEnabled && tc.RtcpSocket.Handle != tc.RtpSocket.Handle)
                            {
                                Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(tc.RtcpSocket, remoteIp);

                                Common.Extensions.Socket.SocketExtensions.SetMulticastTimeToLive(tc.RtcpSocket, ttl);
                            }
                        }

                        catch
                        {
                            //Handle in application.
                        }
                    }
                }

                //Return the context created
                return tc;
            }

            public static GoodbyeReport CreateGoodbye(TransportContext context, byte[] reasonForLeaving = null, int? ssrc = null, RFC3550.SourceList sourcesLeaving = null)
            {
                //Make a Goodbye, indicate version in Client, allow reason for leaving 
                //Todo add other parties where null with SourceList
                return new GoodbyeReport(context.Version, ssrc ?? (int)context.SynchronizationSourceIdentifier, sourcesLeaving, reasonForLeaving);
            }

            /// <summary>
            /// Creates a <see cref="SendersReport"/> from the given context and updates the RtpExpectedPrior and RtpReceivedPrior accordingly.
            /// Note, If empty is false and no previous <see cref="SendersReport"/> was sent then the report will be empty anyway.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="empty">Specifies if the report should have any report blocks if possible</param>
            /// <returns>The report created</returns>
            /// TODO, Allow an alternate ssrc
            public static SendersReport CreateSendersReport(TransportContext context, bool empty, bool rfc = true)
            {
                //Create a SendersReport
                SendersReport result = new SendersReport(context.Version, 0, context.SynchronizationSourceIdentifier);

                //Use the values from the TransportChannel (Use .NtpTimestamp = 0 to Disable NTP)[Should allow for this to be disabled]
                result.NtpTimestamp = context.SenderNtpTimestamp + context.SenderNtpOffset;

                if (result.NtpTimestamp == 0) result.NtpDateTime = DateTime.UtcNow;

                //Note that in most cases this timestamp will not be equal to the RTP timestamp in any adjacent data packet.  Rather, it MUST be  calculated from the corresponding NTP timestamp using the relationship between the RTP timestamp counter and real time as maintained by periodically checking the wallclock time at a sampling instant.
                result.RtpTimestamp = context.SenderRtpTimestamp;

                //If no data has been received this value will be 0, set it to the expected value based on the time.
                if (result.RtpTimestamp == 0) result.RtpTimestamp = (int)Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp32(result.NtpDateTime);

                //Counters
                result.SendersOctetCount = (int)(rfc ? context.RfcRtpBytesSent : context.RtpBytesSent);
                result.SendersPacketCount = (int)context.RtpPacketsSent;

                //Ensure there is a remote party
                //If source blocks are included include them and calculate their statistics
                if (false.Equals(empty) && false.Equals(context.InDiscovery) && context.IsValid && context.TotalPacketsSent > 0)
                {
                    uint fraction, lost;

                    RFC3550.CalculateFractionAndLoss(ref context.RtpBaseSeq, ref context.RtpMaxSeq, ref context.RtpSeqCycles, ref context.ValidRtpPacketsReceived, ref context.RtpReceivedPrior, ref context.RtpExpectedPrior, out fraction, out lost);

                    //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                    result.Add(new ReportBlock((int)context.RemoteSynchronizationSourceIdentifier,
                        (byte)fraction,
                        (int)lost,
                        context.SendSequenceNumber,
                        (int)context.SenderJitter,
                        //The middle 32 bits out of 64 in the NTP timestamp (as explained in Section 4) received as part of the most recent RTCP sender report (SR) packet from source SSRC_n. If no SR has been received yet, the field is set to zero.
                        (int)((context.SenderNtpTimestamp >> 16) << 32),
                        //The delay, expressed in units of 1/65536 seconds, between receiving the last SR packet from source SSRC_n and sending this reception report block. If no SR packet has been received yet from SSRC_n, the DLSR field is set to zero.
                        context.LastRtcpReportSent > TimeSpan.MinValue ? (int)context.LastRtcpReportSent.TotalSeconds / ushort.MaxValue : 0));
                }
                
                return result;
            }

            /// <summary>
            /// Creates a <see cref="ReceiversReport"/> from the given context and updates the RtpExpectedPrior and RtpReceivedPrior accordingly.
            /// </summary>
            /// <param name="context">The context</param>
            /// <param name="empty">Indicates if the report should be empty</param>
            /// <returns>The report created</returns>
            public static ReceiversReport CreateReceiversReport(TransportContext context, bool empty)
            {
                ReceiversReport result = new ReceiversReport(context.Version, 0, context.SynchronizationSourceIdentifier);

                //if (false == empty && false == context.InDiscovery && context.IsValid && context.TotalRtpPacketsReceieved > 0)
                if (false.Equals(empty) && context.TotalRtpPacketsReceieved > 0)
                {
                    uint fraction, lost;

                    RFC3550.CalculateFractionAndLoss(ref context.RtpBaseSeq, ref context.RtpMaxSeq, ref context.RtpSeqCycles, ref context.ValidRtpPacketsReceived, ref context.RtpReceivedPrior, ref context.RtpExpectedPrior, out fraction, out lost);

                    //Create the ReportBlock based off the statistics of the last RtpPacket and last SendersReport
                    result.Add(new ReportBlock((int)context.RemoteSynchronizationSourceIdentifier,
                        (byte)fraction,
                        (int)lost,
                        context.RecieveSequenceNumber,
                        (int)context.RtpJitter >> 4,//The last report may not be null but may be disposed and time is probably invalid if so, in such a case use LastRtcpReportRecieved                    
                        (int)(false == Common.IDisposedExtensions.IsNullOrDisposed(context.SendersReport) ? Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp32(context.SendersReport.NtpDateTime) : context.LastRtcpReportReceived > TimeSpan.MinValue ? Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp32(DateTime.UtcNow - context.LastRtcpReportReceived) : 0),
                        (context.SendersReport != null ? ((DateTime.UtcNow - context.SendersReport.Created).Seconds / ushort.MaxValue) * 1000 : 0) //If also sending senders reports this logic may not be correct
                        //context.LastRtcpReportSent > TimeSpan.MinValue ? (int)context.LastRtcpReportSent.TotalSeconds / ushort.MaxValue : 0)
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
                    //Todo, should have ip / port etc to identify multiple connections to the same server
                    new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk((int)context.SynchronizationSourceIdentifier, Common.Extensions.Linq.LinqExtensions.Yield((cName ?? Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.CName)).Concat(items ?? System.Linq.Enumerable.Empty<Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem>()))
                };
            }

            //FrameGenerator => RtpPacketHandler

            #endregion

            #region Fields

            /// <summary>
            /// The version of packets which the TransportContents handles
            /// </summary>
            public int Version = 2;

            /// <summary>
            /// The amount of <see cref="RtpPacket"/>'s which must be received before IsValid is true.
            /// </summary>
            public int MinimumSequentialValidRtpPackets = RFC3550.DefaultMinimumSequentalRtpPackets;

            public int MaxMisorder = RFC3550.DefaultMaxMisorder;

            public int MaxDropout = RFC3550.DefaultMaxDropout;

            /// <summary>
            /// The channels which identity the TransportContext.
            /// </summary>
            public byte DataChannel, ControlChannel;

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

            //The current, highest received and highest sent Sequence numbers recieved by the RtpClient
            internal ushort m_SequenceNumber, m_LastSentSequenceNumber, RtpMaxSeq;

            //Used for Rtp and Rtcp Transport Calculations (Should be moved into State Structure)
            internal uint RtpTransit, SenderTransit,
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
                RtpJitter, SenderJitter,
                //Valid amount of rtp packets recieved 
                ValidRtpPacketsReceived;

            internal TimeSpan m_SendInterval = DefaultReportInterval, m_ReceiveInterval = DefaultReportInterval,
                m_InactiveTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan,
                m_StartTime = TimeSpan.Zero, m_EndTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
                //Used to allow a specific reporting interval (would proably need varaibles for send and receive to allow full customization...)
                //m_ContextReportInterval = DefaultReportInterval;

            //When packets are succesfully transferred the DateTime (utc) is copied in these variables and will reflect the point in time in which  the last 
            internal DateTime m_FirstPacketReceived, m_FirstPacketSent,
                m_LastRtcpIn, m_LastRtcpOut,  //Rtcp packets were received and sent
                m_LastRtpIn, m_LastRtpOut, //Rtp packets were received and sent
                m_Initialized;//When initialize was called.

            //TimeRange?

            /// <summary>
            /// Keeps track of any failures which occur when sending or receieving data.
            /// </summary>
            internal protected int m_FailedRtpTransmissions, m_FailedRtcpTransmissions, m_FailedRtpReceptions, m_FailedRtcpReceptions;

            /// <summary>
            /// Used to ensure packets are allowed.
            /// </summary>
            ushort m_MimumPacketSize = 8, m_MaximumPacketSize = ushort.MaxValue;

            /// <summary>
            /// To allow multiple receivers as set by <see cref="MaximumRemoteIdentities"/>
            /// </summary>
            //public readonly List<int> Recievers = new List<int>();

            internal readonly HashSet<System.Net.IPAddress> MulticastGroups = new HashSet<IPAddress>();

            #endregion

            #region Properties

            /// <summary>
            /// Indicates if duplicate packets will be tolerated
            /// </summary>
            public bool AllowDuplicatePackets
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// Indicates if multiple payload types will be tolerated in the <see cref="CurrentFrame"/> or <see cref="LastFrame"/> during 'Add'
            /// </summary>
            public bool AllowsMultiplePayloadTypes
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// Indicates if out of order packets will be tolerated
            /// </summary>
            public bool AllowOutOfOrderPackets
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            ///// <summary>
            ///// A value which indicates the maximum amount of remote sources this context will accept data from.
            ///// </summary>
            //public int MaximumRemoteIdentities
            //{
            //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //    get;
            //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //    set;
            //}

            /// <summary>
            /// A value which is used when during <see cref="Initialize"/> to set the <see cref="RecieveBufferSize"/> relative to the size of <see cref="ContextMemory"/>
            /// </summary>
            public int RecieveBufferSizeMultiplier
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            public Action<Socket> ConfigureSocket
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// Sets or gets the applications-specific state associated with the TransportContext.
            /// </summary>
            public Object ApplicationContext
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// Gets or sets the MemorySegment used by this context.
            /// </summary>
            public MemorySegment ContextMemory
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// The smallest packet which may be sent or recieved on the TransportContext.
            /// </summary>
            public int MinimumPacketSize
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return (int)m_MimumPacketSize; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set { m_MimumPacketSize = (ushort)value; }
            }

            /// <summary>
            /// The largest packet which may be sent or recieved on the TransportContext.
            /// </summary>
            public int MaximumPacketSize
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return (int)m_MaximumPacketSize; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set { m_MaximumPacketSize = (ushort)value; }
            }

            public bool HasAnyRecentActivity
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return HasRecentRtpActivity || HasRecentRtcpActivity; }
            }


            public bool HasRecentRtpActivity
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    //Check for Rtp Receive Activity if receiving
                    return HasReceivedRtpWithinReceiveInterval
                        || //Check for Rtp Send Activity if sending
                        HasSentRtpWithinSendInterval;
                }
            }

            public bool HasRecentRtcpActivity
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return TotalRtpPacketsReceieved >= 0 &&
                        false.Equals(m_LastRtpIn.Equals(DateTime.MinValue)) &&
                        false.Equals(m_ReceiveInterval.Equals(Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan)) &&
                        LastRtpPacketReceived < m_ReceiveInterval;
                }
            }

            public bool HasSentRtpWithinSendInterval
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return IsActive && TotalRtpPacketsSent >= 0 &&
                        false.Equals(m_LastRtpOut.Equals(DateTime.MinValue)) && 
                        false.Equals(m_SendInterval.Equals(Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan)) && 
                        LastRtpPacketSent < m_SendInterval;
                }
            }

            public bool HasReceivedRtcpWithinReceiveInterval
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return TotalRtcpPacketsReceieved >= 0 &&
                        false.Equals(m_LastRtcpIn.Equals(DateTime.MinValue)) &&
                        false.Equals(m_ReceiveInterval.Equals(Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan)) &&
                        LastRtcpReportReceived < m_ReceiveInterval;
                }
            }

            public bool HasSentRtcpWithinSendInterval
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return TotalRtcpPacketsSent >= 0 && 
                        false.Equals(m_LastRtcpOut == DateTime.MinValue) &&
                        false.Equals(m_SendInterval == Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan) && 
                        LastRtcpReportSent < m_SendInterval;
                }
            }

            /// <summary>
            /// Indicates if the RemoteParty is known by a unique id other than 0 unless <see cref="MinimumSequentialValidRtpPackets"/> have been recieved
            /// </summary>
            internal bool InDiscovery
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return ValidRtpPacketsReceived < MinimumSequentialValidRtpPackets && RemoteSynchronizationSourceIdentifier.Equals(Common.Binary.Zero); }
            }

            /// <summary>
            /// Gets or Sets a value which indicates if the Rtp and Rtcp Sockets should be Disposed when Dispose is called.
            /// </summary>
            public bool LeaveOpen
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            //Any frames for this channel
            public RtpFrame CurrentFrame
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            public RtpFrame LastFrame
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;

                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            //for testing purposes
            //internal RtpFrame Holding
            //{
            //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //    get;

            //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //    internal protected set;
            //}

            /// <summary>
            /// The socket used for Transport of Rtp and Interleaved data
            /// </summary>
            public Socket RtpSocket
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            /// <summary>
            /// The socket used for Transport of Rtcp and Interleaved data
            /// </summary>
            public Socket RtcpSocket
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            /// <summary>
            /// Indicates if the TransportContext has a <see cref="LocalRtp"/> or <see cref="LocalRtcp"/> EndPoint, usually established in Initialize
            /// </summary>
            public bool IsActive
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (IsRtpEnabled)
                    {
                        return false.Equals(object.ReferenceEquals(RtpSocket, null)) && false.Equals(object.ReferenceEquals(LocalRtp, null));
                    }
                    else if (IsRtcpEnabled)
                    {
                        return false.Equals(object.ReferenceEquals(RtcpSocket, null)) && false.Equals(object.ReferenceEquals(LocalRtcp, null));
                    }

                    return false;
                }
            }

            /// <summary>
            /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the TransportContext) during reports
            /// </summary>
            public double MaximumRtcpBandwidthPercentage
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            /// Indicates if the amount of bandwith currently utilized for Rtcp reporting has exceeded the amount of bandwidth allowed by the <see cref="MaximumRtcpBandwidthPercentage"/> property.
            /// </summary>
            public bool RtcpBandwidthExceeded
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (false.Equals(IsRtcpEnabled) || IsDisposed) return true;

                    double maximumRtcpBandwidthPercentage = MaximumRtcpBandwidthPercentage;

                    //If disposed no limit is imposed do not check
                    if (maximumRtcpBandwidthPercentage.Equals(Common.Binary.DoubleZero)) return false;

                    long totalReceived = TotalBytesReceieved;

                    if (totalReceived.Equals(Common.Binary.LongZero)) return false;

                    long totalRtcp = TotalRtcpBytesSent + TotalRtcpBytesReceieved;

                    if (totalRtcp.Equals(Common.Binary.LongZero)) return false;

                    return totalRtcp >= totalReceived / maximumRtcpBandwidthPercentage;
                }
            }

            /// <summary>
            /// The amount of time the TransportContext has been sending packets.
            /// </summary>
            public TimeSpan TimeSending
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return IsDisposed || m_FirstPacketSent.Equals(DateTime.MinValue) ? 
                        Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan 
                        : 
                        DateTime.UtcNow - m_FirstPacketSent;
                }
            }

            /// <summary>
            /// The amount of time the TransportContext has been receiving packets.
            /// </summary>
            public TimeSpan TimeReceiving
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return IsDisposed || m_FirstPacketReceived.Equals(DateTime.MinValue) ? 
                        Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan 
                        : 
                        DateTime.UtcNow - m_FirstPacketReceived;
                }
            }

            ///// <summary>
            ///// Indicates if the context has been Sending or Receiving for more time then allowed.
            ///// </summary>
            //public bool MediaEnded
            //{
            //    get
            //    {
            //        return !IsContinious && TimeSending == Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan && TimeReceiving == TimeSending;
            //    }
            //}

            /// <summary>
            /// The time at which the media starts
            /// </summary>
            public TimeSpan MediaStartTime
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_StartTime; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set { m_StartTime = value; }
            }

            /// <summary>
            /// The time at which the media ends
            /// </summary>
            public TimeSpan MediaEndTime
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_EndTime; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set { m_EndTime = value; }
            }

            public TimeSpan MediaDuration
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsContinious ? Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan : m_EndTime - m_StartTime; }
            }

            /// <summary>
            /// Indicates if the <see cref="MediaEndTime"/> is <see cref="Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan"/>. (Has no determined end time)
            /// </summary>
            public bool IsContinious
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_EndTime == Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan; }
            }

            /// <summary>
            /// <see cref="Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan"/> if <see cref="IsContinious"/>,
            /// othewise the amount of time remaining in the media.
            /// </summary>
            public TimeSpan TimeRemaining
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsContinious ? m_EndTime : TimeSpan.FromTicks(m_EndTime.Ticks - (Math.Max(TimeReceiving.Ticks, TimeSending.Ticks))); }
            }

            /// <summary>
            /// Allows getting or setting of the interval which occurs between data transmissions
            /// </summary>
            public TimeSpan SendInterval
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_SendInterval; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set { m_SendInterval = value; }
            }

            /// <summary>
            /// Allows gettings or setting of the interval which occurs between data receptions
            /// </summary>
            public TimeSpan ReceiveInterval
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_ReceiveInterval; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set { m_ReceiveInterval = value; }
            }

            /// <summary>
            /// Gets the time in which in TranportContext was last active for a send or receive operation
            /// </summary>
            public TimeSpan InactiveTime
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_InactiveTime; }
            }


            /// <summary>
            /// Gets the time in which the last Rtcp reports were sent.
            /// </summary>
            public TimeSpan LastRtcpReportSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_LastRtcpOut.Equals(DateTime.MinValue) ? TimeSpan.MinValue : DateTime.UtcNow - m_LastRtcpOut;
                }
            }

            /// <summary>
            /// Gets the time in which the last Rtcp reports were received.
            /// </summary>
            public TimeSpan LastRtcpReportReceived
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_LastRtcpIn.Equals(DateTime.MinValue) ? TimeSpan.MinValue : DateTime.UtcNow - m_LastRtcpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was received.
            /// </summary>
            public TimeSpan LastRtpPacketReceived
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_LastRtpIn.Equals(DateTime.MinValue) ? TimeSpan.MinValue : DateTime.UtcNow - m_LastRtpIn;
                }
            }

            /// <summary>
            /// Gets the time in which the last RtpPacket was transmitted.
            /// </summary>
            public TimeSpan LastRtpPacketSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_LastRtpOut.Equals(DateTime.MinValue) ? TimeSpan.MinValue : DateTime.UtcNow - m_LastRtpOut;
                }
            }

            /// <summary>
            /// Gets the time since <see cref="Initialize was called."/>
            /// </summary>
            public TimeSpan TimeActive
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_Initialized.Equals(DateTime.MinValue) ? TimeSpan.MinValue : DateTime.UtcNow - m_Initialized;
                }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when sending RtcpPackets
            /// </summary>
            public int FailedRtcpTransmissions
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_FailedRtcpTransmissions; }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when sending RtpPackets
            /// </summary>
            public int FailedRtpTransmissions
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_FailedRtpTransmissions; }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when receiving RtcpPackets
            /// </summary>
            public int FailedRtcpReceptions
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_FailedRtcpReceptions; }
            }

            /// <summary>
            /// Indicates the amount of times a failure has occured when receiving RtpPackets
            /// </summary>
            public int FailedRtpReceptions
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return m_FailedRtpReceptions; }
            }

            /// <summary>
            /// Corresponds to the ID used by remote systems to identify this TransportContext, a table might be necessary if you want to use a different id in different places
            /// </summary>
            public int SynchronizationSourceIdentifier
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            /// <summary>
            /// Corresponds to the ID used to identify remote parties.            
            /// Use a <see cref="Conference"/> if the size of the group or its members should be limited in some capacity.
            /// </summary>
            public int RemoteSynchronizationSourceIdentifier
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            /// <summary>
            /// MediaDescription which contains information about the type of Media on the Interleave
            /// </summary>
            public Sdp.MediaDescription MediaDescription
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set;
            }

            /// <summary>
            /// Determines if the source has recieved at least <see cref="MinimumSequentialValidRtpPackets"/> RtpPackets
            /// </summary>
            public /*virtual*/ bool IsValid
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return ValidRtpPacketsReceived >= MinimumSequentialValidRtpPackets; }
            }

            /// <summary>
            /// Indicates if the Rtcp is enabled and the LocalRtp is equal to the LocalRtcp
            /// </summary>
            public bool LocalMultiplexing
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed || IsRtcpEnabled == false || LocalRtp == null ? false : LocalRtp.Equals(LocalRtcp); }
            }

            /// <summary>
            /// Indicates if the Rtcp is enabled and the RemoteRtp is equal to the RemoteRtcp
            /// </summary>
            public bool RemoteMultiplexing
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed || IsRtcpEnabled == false || RemoteRtp == null ? false : RemoteRtp.Equals(RemoteRtcp); }
            }
            
            /// <summary>
            /// <c>false</c> if NOT [RtpEnabled AND RtcpEnabled] AND [LocalMultiplexing OR RemoteMultiplexing]
            /// </summary>
            public bool IsDuplexing
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (IsDisposed) return false;

                    return (IsRtpEnabled && IsRtcpEnabled) && (LocalMultiplexing || RemoteMultiplexing);
                }
            }

            /// <summary>
            /// The last <see cref="ReceiversReport"/> sent or received by this RtpClient.
            /// </summary>
            public ReceiversReport ReceiversReport
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            /// <summary>
            /// The last <see cref="SendersReport"/> sent or received by this RtpClient.
            /// </summary>
            public SendersReport SendersReport
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            /// The last <see cref="SourceDescriptionReport"/> sent or received by this RtpClient.
            public SourceDescriptionReport SourceDescription
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            /// The last <see cref="GoodbyeReport"/> sent or received by this RtpClient.
            public GoodbyeReport Goodbye
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) receieved
            /// </summary>
            public long TotalPacketsReceived
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return RtpPacketsReceived + RtcpPacketsReceived; }
            }

            /// <summary>
            /// The total amount of packets (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalPacketsSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return RtpPacketsSent + RtcpPacketsSent; }
            }

            /// <summary>
            /// The total amount of RtpPackets sent
            /// </summary>
            public long TotalRtpPacketsSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpPacketsSent; }
            }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been sent.
            /// </summary>
            public long RtpPayloadBytesSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpBytesSent; }
            }

            /// <summary>
            /// The amount of bytes in all rtp packets payloads which have been received.
            /// </summary>
            public long RtpPayloadBytesRecieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpBytesRecieved; }
            }

            /// <summary>
            /// The total amount of bytes related to Rtp sent (including headers)
            /// </summary>
            public long TotalRtpBytesSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpBytesSent + RtpHeader.Length * RtpPacketsSent; }
            }

            /// <summary>
            /// The total amount of bytes related to Rtp received
            /// </summary>
            public long TotalRtpBytesReceieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpBytesRecieved + RtpHeader.Length * RtpPacketsSent; }
            }

            /// <summary>
            /// The total amount of RtpPackets received
            /// </summary>
            public long TotalRtpPacketsReceieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtpPacketsReceived; }
            }

            /// <summary>
            /// The total amount of RtcpPackets recieved
            /// </summary>
            public long TotalRtcpPacketsSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtcpPacketsSent; }
            }

            /// <summary>
            /// The total amount of sent bytes related to Rtcp 
            /// </summary>
            public long TotalRtcpBytesSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtcpBytesSent; }
            }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) received
            /// </summary>
            public long TotalBytesReceieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : TotalRtcpBytesReceieved + TotalRtpBytesReceieved; }
            }

            /// <summary>
            /// The total amount of received bytes (both Rtp and Rtcp) sent
            /// </summary>
            public long TotalBytesSent
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : TotalRtcpBytesSent + TotalRtpBytesSent; }
            }

            /// <summary>
            /// The total amount of RtcpPackets received
            /// </summary>
            public long TotalRtcpPacketsReceieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtcpPacketsReceived; }
            }

            /// <summary>
            /// The total amount of bytes related to Rtcp received
            /// </summary>
            public long TotalRtcpBytesReceieved
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return IsDisposed ? 0 : RtcpBytesRecieved; }
            }

            /// <summary>            
            /// Gets the sequence number of the last RtpPacket recieved on this channel
            /// </summary>
            public int RecieveSequenceNumber
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return (short)m_SequenceNumber; }

                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set { m_SequenceNumber = (ushort)value; }
            }

            public int SendSequenceNumber
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return (short)m_LastSentSequenceNumber; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal protected set { m_LastSentSequenceNumber = (ushort)value; }
            }

            public int RtpTimestamp
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            public int SenderRtpTimestamp
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            /// <summary>
            /// The NtpTimestamp from the last SendersReport recieved or created
            /// </summary>
            public long NtpTimestamp
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            public long SenderNtpTimestamp
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                internal set;
            }

            //Allows for time difference between the source and the client when issuing reports, will be added to any NtpTimestamp created.

            public long NtpOffset
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            public long SenderNtpOffset
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            #endregion

            #region Constructor

            /// <summary>
            /// Creates a TransportContext from the given parameters
            /// </summary>
            /// <param name="dataChannel"></param>
            /// <param name="controlChannel"></param>
            /// <param name="ssrc"></param>
            /// <param name="rtcpEnabled"></param>
            /// <param name="senderSsrc"></param>
            /// <param name="minimumSequentialRtpPackets"></param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2, Action<System.Net.Sockets.Socket> configure = null, bool shouldDispose = true)
                :base(shouldDispose)
            {
                //MaximumRemoteIdentities = 1;

                //Todo, threshold?
                AllowOutOfOrderPackets = true;

                if (dataChannel.Equals(controlChannel)) throw new InvalidOperationException("dataChannel and controlChannel must be unique.");

                if (ssrc.Equals(senderSsrc) && false.Equals(ssrc.Equals(0))) throw new InvalidOperationException("ssrc and senderSsrc must be unique.");

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

                //Assign the function responsible for configuring the socket
                ConfigureSocket = configure ?? ConfigureRtpRtcpSocket;

                //Use the default unless assigned after creation
                RecieveBufferSizeMultiplier = DefaultRecieveBufferSizeMultiplier;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, Sdp.MediaDescription mediaDescription, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2, bool shouldDispose = true)
                : this(dataChannel, controlChannel, ssrc, rtcpEnabled, senderSsrc, minimumSequentialRtpPackets, null, shouldDispose)
            {
                MediaDescription = mediaDescription;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public TransportContext(byte dataChannel, byte controlChannel, int ssrc, Sdp.MediaDescription mediaDescription, Socket socket, bool rtcpEnabled = true, int senderSsrc = 0, int minimumSequentialRtpPackets = 2, bool shouldDispose = true)
                : this(dataChannel, controlChannel, ssrc, mediaDescription, rtcpEnabled, senderSsrc, minimumSequentialRtpPackets, shouldDispose)
            {
                RtpSocket = RtcpSocket = socket;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Assigns a Non Zero value to <see cref="SynchronizationSourceIdentifier"/> to a random value based on the given seed.
            /// The value will also be different than <see cref="RemoteSynchronizationSourceIdentifier"/>.
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected void AssignIdentity(int seed = SendersReport.PayloadType)
            {
                if (SynchronizationSourceIdentifier.Equals(Common.Binary.Zero))
                {
                    //Generate the id per RFC3550
                    do SynchronizationSourceIdentifier = RFC3550.Random32(seed);
                    while (SynchronizationSourceIdentifier.Equals(Common.Binary.Zero) || SynchronizationSourceIdentifier.Equals(RemoteSynchronizationSourceIdentifier));
                }
            }

            /// <summary>
            /// Calculates RTP Interarrival Jitter as specified in RFC 3550 6.4.1.
            /// </summary>
            /// <param name="packet">RTP packet.</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void UpdateJitterAndTimestamp(RtpPacket packet)//bool sent
            {
                //Determine to update sent or received values
                bool sentPacket = packet.Transferred.HasValue;

                // RFC 3550 A.8.
                //Determine the time the last packet was sent or received
                TimeSpan arrivalDifference = (sentPacket ? LastRtpPacketSent : LastRtpPacketReceived);

                if (sentPacket)
                {
                    RFC3550.CalulcateJitter(ref arrivalDifference, ref SenderJitter, ref SenderTransit);

                    //Update the Sender RtpTimestamp on the Context
                    SenderRtpTimestamp = packet.Timestamp;

                    //Update the Sender NtpTimestamp on the Context.
                    SenderNtpTimestamp = (long)Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(packet.Transferred ?? packet.Created);
                }
                else //Handle as a recieved packet
                {

                    RFC3550.CalulcateJitter(ref arrivalDifference, ref RtpJitter, ref RtpTransit);

                    //Update the RtpTimestamp on the Context
                    RtpTimestamp = packet.Timestamp;

                    //Update the NtpTimestamp on the Context.
                    NtpTimestamp = (long)Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(sentPacket ? packet.Transferred.Value : packet.Created);
                }

                //Context is not inactive.
                m_InactiveTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
            }

            /// <summary>
            /// Resets the variables used in packets validation based on the given parameter.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number to reset to.</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ResetRtpValidationCounters(int sequenceNumber)
            {
                ushort val = (ushort)sequenceNumber;

                RFC3550.ResetRtpValidationCounters(ref val, ref RtpBaseSeq, ref RtpMaxSeq, ref RtpBadSeq, ref RtpSeqCycles, ref RtpReceivedPrior, ref ValidRtpPacketsReceived);
            }

            /// <summary>
            /// Performs checks on the packet which can be overriden in a derrived implementation
            /// </summary>
            /// <param name="packet"></param>
            /// <returns></returns>
            public virtual bool ValidatePacketAndUpdateSequenceNumber(RtpPacket packet) //out int codeReason
            {

                /*NOTE : 
                 * http://www.ietf.org/rfc/rfc3551.txt
                 * 
                  Static payload type 13 was assigned to the Comfort Noise (CN) payload format defined in RFC 3389.  
                  Payload type 19 was marked reserved because it had been temporarily allocated to an earlier version of Comfort Noise
                  present in some draft revisions of this document.
                 */

                //If there is no Payload return, this prevents injection by utilizing just a RtpHeader which happens to be valid.
                //I can think of no good reason to allow this in this implementation, if required dervive and ensure that RTCP is not better suited for whatever is being done.
                //The underlying goto CheckSequenceNumber is what is used to performed this check currently.
                //if (payloadLength == 0 && packet.PayloadType != 13) return false;
                //else if (packet.PayloadType == 13  || packet.PayloadType == 19) goto CheckSequenceNumber;

                if (packet.Header.IsCompressed || packet.PayloadType.Equals(13)) goto CheckSequenceNumber;

                // RFC 3550 A.1. Notes: Each TransportContext instance may be better suited to have a structure which defines this logic.

                //o  RTP version field must equal 2.

                if (false.Equals(packet.Version.Equals(Version))) return false;

                //o  The payload type must be known, and in particular it must not be equal to SR or RR.

                int check = packet.PayloadType;

                //Check the payload type is known and not equal to sr or rr.
                if (check.Equals(SendersReport.PayloadType) || check.Equals(ReceiversReport.PayloadType) || false.Equals(MediaDescription.PayloadTypes.Contains(check))) return false;

                //Space complex
                int payloadLength = packet.Payload.Count;

                //o  If the P bit is set, Padding must be less than the total packet length minus the header size.
                if (packet.Padding && payloadLength > 0 && packet.PaddingOctets >= payloadLength) return false;

                check = packet.ContributingSourceCount;

                ///  o  The length of the packet must be consistent with CC and payload type (if payloads have a known length this is checked with the IsComplete property).
                if (check > 0 && payloadLength < check * Common.Binary.BytesPerInteger) return false;

                //Only performed to ensure validity
                if (packet.Extension)
                {
                    //o  The X bit must be zero if the profile does not specify that the
                    //   header extension mechanism may be used.  
                    //   Otherwise, the extension
                    //   length field must be less than the total packet size minus the
                    //   fixed header length and padding.

                    //Read the amount of paddingOctets
                    check = packet.PaddingOctets;

                    //Ensure the padding is valid first
                    if (check >= payloadLength) return false;

                    //Ensure the above is also true.
                    if (packet.ExtensionOctets > payloadLength - check) return false;
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

                check = packet.SequenceNumber;

                //Return the result of processing the verification of the sequence number according the RFC3550 A.1
                if (UpdateSequenceNumber(ref check))
                {
                    //Update the SequenceNumber
                    RecieveSequenceNumber = check;

                    return true;
                }
                else if (check < RecieveSequenceNumber) return true;

                return false;
            }


            public bool UpdateSequenceNumber(int sequenceNumber) //,bool probe = false
            {
                return UpdateSequenceNumber(ref sequenceNumber);
            }

            /// <summary>
            /// Performs checks in accorance with RFC3550 A.1 and returns a value indicating if the given sequence number is in state.
            /// </summary>
            /// <param name="sequenceNumber">The sequenceNumber to check.</param>
            /// <returns>True if in state, otherwise false.</returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            [System.CLSCompliant(false)]
            public bool UpdateSequenceNumber(ref int sequenceNumber) 
            {
                ushort val = (ushort)sequenceNumber;

                /*bool result = */ return RFC3550.UpdateSequenceNumber(ref val, 
                    ref RtpBaseSeq, ref RtpMaxSeq, ref RtpBadSeq, 
                    ref RtpSeqCycles, ref RtpReceivedPrior, ref RtpProbation, 
                    ref ValidRtpPacketsReceived, 
                    ref MinimumSequentialValidRtpPackets, ref MaxMisorder, ref MaxDropout);
                
                //return result;
            }

            /// <summary>
            /// Randomizes the SequenceNumber
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RandomizeSequenceNumber()
            {
                RecieveSequenceNumber = Utility.Random.Next();
            }

            #region Initialize            

            //Todo allow for Leave Open...

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

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Initialize(IPEndPoint localRtp, IPEndPoint remoteRtp, IPEndPoint localRtcp, IPEndPoint remoteRtcp, bool punchHole = true)
            {
                if (IsDisposed || IsActive) return;

                m_Initialized = DateTime.UtcNow;

                if (localRtp.Address.AddressFamily.Equals(remoteRtp.Address.AddressFamily).Equals(false)) Media.Common.TaggedExceptionExtensions.RaiseTaggedException<TransportContext>(this, "localIp and remoteIp AddressFamily must match.");
                else if (punchHole) punchHole = Media.Common.Extensions.IPAddress.IPAddressExtensions.IsOnIntranet(remoteRtp.Address).Equals(false); //Only punch a hole if the remoteIp is not on the LAN by default.
                
                //Erase previously set values on the TransportContext.
                //RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                //Set now if not already set
                AssignIdentity();               

                try
                {
                    //Create the RtpSocket
                    RtpSocket = new Socket(localRtp.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    //Configure it
                    ConfigureSocket(RtpSocket);

                    //Apply the send and receive timeout based on the ReceiveInterval
                    RtpSocket.SendTimeout = RtpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds) >> 1;

                    LocalRtp = localRtp;

                    RemoteRtp = remoteRtp;

                    try
                    {
                        //Assign the LocalRtp EndPoint and Bind the socket to that EndPoint
                        RtpSocket.Bind(LocalRtp);

                        //Assign the RemoteRtp EndPoint and Bind the socket to that EndPoint
                        RtpSocket.Connect(RemoteRtp);
                    }
                    catch
                    {
                        //Can't bind or connect
                    }

                    ////Handle Multicast joining (Might need to track interface)
                    //if (Common.Extensions.IPEndPoint.IPEndPointExtensions.IsMulticast(remoteRtp))
                    //{
                    //    Common.Extensions.Socket.SocketExtensions.JoinMulticastGroup(RtpSocket, remoteRtp.Address, ttl);
                    //}

                    //Determine if holepunch is required

                    //Todo, send reports, don't use proprietary messages
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
                    if (remoteRtp.Equals(remoteRtcp))
                    {
                        RtcpSocket = RtpSocket;
                    }
                    else if (IsRtcpEnabled)
                    {
                        //Create the RtcpSocket
                        RtcpSocket = new Socket(localRtp.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                        //Configure it
                        ConfigureSocket(RtcpSocket);

                        //Apply the send and receive timeout based on the ReceiveInterval
                        RtcpSocket.SendTimeout = RtcpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds) >> 1;

                        LocalRtcp = localRtcp;

                        RemoteRtcp = remoteRtcp;

                        try
                        {
                            //Assign the LocalRtcp EndPoint and Bind the socket to that EndPoint
                            RtcpSocket.Bind(LocalRtcp);
                            
                            //Assign the RemoteRtcp EndPoint and Bind the socket to that EndPoint
                            RtcpSocket.Connect(RemoteRtcp);
                        }
                        catch
                        {
                            //Can't bind or connect
                        }

                        //Todo, send reports, don't use proprietary messages

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

                    //Setup the receive buffer size for all sockets of this context to use memory defined in excess of the context memory to ensure a high receive rate in udp
                    if (RecieveBufferSizeMultiplier >= 0 && 
                        false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(ContextMemory)) &&
                        ContextMemory.Count > 0)
                    {
                        //Ensure the receive buffer size is updated for that context.
                        Media.Common.ISocketReferenceExtensions.SetReceiveBufferSize(((Media.Common.ISocketReference)this), RecieveBufferSizeMultiplier * ContextMemory.Count);
                    }

                }
                catch
                {
                    throw;
                }
            }

            #region Tcp

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
                LocalRtp = local;

                RemoteRtp = remote;

                Socket socket = new Socket(local.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                ConfigureSocket(socket);

                Initialize(socket);

                //This reference is not needed anymore
                socket = null;
            }

            /// <summary>
            /// Uses the given socket for the duplexed data
            /// </summary>
            /// <param name="duplexed">The socket to use</param>
            public void Initialize(Socket duplexed)
            {
                if (IsDisposed || IsActive) return;

                //If the socket is not exclusively using the address
                if (false.Equals(duplexed.ExclusiveAddressUse))
                {
                    //Duplicte the socket's type for a Rtcp socket.
                    Socket rtcpSocket = new Socket(duplexed.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    //Configure the duplicate
                    ConfigureSocket(rtcpSocket);

                    //Initialize with the duplicate socket
                    Initialize(duplexed, rtcpSocket);

                    //Should log, can't initialize
                    if (IsActive.Equals(false)) rtcpSocket.Dispose();

                    //This reference is no longer needed.
                    rtcpSocket = null;
                }
                else Initialize(duplexed, duplexed); //Otherwise use the existing socket twice
            }

            #endregion

            #region Existing Sockets (Could be Mixed Tcp and Udp)

            /// <summary>
            /// Used to provide sockets which are already bound and connected for use in rtp and rtcp operations
            /// </summary>
            /// <param name="rtpSocket"></param>
            /// <param name="rtcpSocket"></param>
            //TODO Must allow leaveOpen for existing sockets
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Initialize(Socket rtpSocket, Socket rtcpSocket)
            {
                if (IsDisposed || IsActive) return;

                if (object.ReferenceEquals(rtpSocket, null)) throw new ArgumentNullException("rtpSocket");

                //Maybe should just be set to the rtpSocket?
                if (object.ReferenceEquals(rtcpSocket, null)) throw new ArgumentNullException("rtcpSocket");

                //RtpBytesRecieved = RtpBytesSent = RtcpBytesRecieved = RtcpBytesSent = 0;

                m_Initialized = DateTime.UtcNow;

                RtpSocket = rtpSocket;

                RtpSocket.SendTimeout = RtpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds) >> 1;                

                bool punchHole = false.Equals(RtpSocket.ProtocolType == ProtocolType.Tcp) && false.Equals(Media.Common.Extensions.IPAddress.IPAddressExtensions.IsOnIntranet(((IPEndPoint)RtpSocket.RemoteEndPoint).Address)); //Only punch a hole if the remoteIp is not on the LAN by default.

                if (object.ReferenceEquals(RemoteRtp, null)) RemoteRtp = RtpSocket.RemoteEndPoint;

                if (object.ReferenceEquals(LocalRtp, null))
                {
                    LocalRtp = RtpSocket.LocalEndPoint;

                    if (false.Equals(RtpSocket.IsBound))
                    {
                        RtpSocket.Bind(LocalRtp);
                    }

                    if (false.Equals(RtpSocket.Connected))
                    {
                        try { RtpSocket.Connect(RemoteRtp); }
                        catch { /*Only tcp must succeed*/ }
                    }
                }

                //If a different socket is used for rtcp configure it also
                if (object.ReferenceEquals((RtcpSocket = rtcpSocket), null).Equals(false))
                {
                    //If the socket is not the same as the RtcpSocket configure it also
                    if ((RtpSocket.Handle == RtcpSocket.Handle).Equals(false))
                    {
                        RtcpSocket.SendTimeout = RtcpSocket.ReceiveTimeout = (int)(ReceiveInterval.TotalMilliseconds) >> 1;  

                        LocalRtcp = RtcpSocket.LocalEndPoint;

                        RemoteRtcp = RtcpSocket.RemoteEndPoint;

                        if (object.ReferenceEquals(LocalRtcp, null).Equals(false) && false.Equals(RtcpSocket.IsBound)) RtcpSocket.Bind(LocalRtcp);

                        if (object.ReferenceEquals(RemoteRtcp, null).Equals(false) && false.Equals(RtcpSocket.Connected)) try { RtcpSocket.Connect(RemoteRtcp); }
                            catch { /*Only tcp must succeed*/ }
                    }
                    else
                    {
                        //Just assign the same end points from the rtp socket.

                        if (object.ReferenceEquals(LocalRtcp, null)) LocalRtcp = LocalRtp;

                        if (object.ReferenceEquals(RemoteRtcp, null)) RemoteRtcp = RemoteRtp;
                    }
                }
                else RtcpSocket = RtpSocket;

                //Todo, send reports, don't use proprietary messages
                if (punchHole)
                {
                    //new RtcpPacket(Version, Rtcp.ReceiversReport.PayloadType, 0, 0, SynchronizationSourceIdentifier, 0);

                    try { RtpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch

                    //Check for the same socket, don't send more than 1 wake up sequence to a unique socket.
                    //IntPtr doesn't expose Equals(IntPtr), it's because it would be possible to be either an int or long but it should be able to be done against it's own type... its only void* anyway...
                    //Todo, PtrCompare?
                    if (RtpSocket.Handle == RtcpSocket.Handle) return;

                    try { RtcpSocket.SendTo(WakeUpBytes, 0, WakeUpBytes.Length, SocketFlags.None, RemoteRtcp); }
                    catch (SocketException) { }//We don't care about the response or any issues during the holePunch
                }

                AssignIdentity();

                Goodbye = null;
            }            

            #endregion

            #endregion

            //Todo Seperate Initialize and Connect and PunchHole (come up with a better name for PunchHole)

            //Todo HandleException / Event

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void DisconnectSockets()
            {
                if (IsActive.Equals(false) || IsDisposed) return;

                if (LeaveOpen)
                {
                    //Maybe should drop multicast group....

                    RtpSocket = RtcpSocket = null;
                }
                else
                {
                    foreach (System.Net.IPAddress groupAddress in MulticastGroups)
                    {
                        //Todo, TryLeave. If false shutdown is coming anyway...
                        Media.Common.Extensions.Socket.SocketExtensions.LeaveMulticastGroup(RtpSocket, groupAddress);
                    }

                    MulticastGroups.Clear();

                    //For Udp the RtcpSocket may be the same socket as the RtpSocket if the sender/reciever is duplexing
                    if (object.ReferenceEquals(RtcpSocket, null).Equals(false) && RtpSocket.Handle.Equals(RtcpSocket.Handle).Equals(false)) RtcpSocket.Close();

                    //Close the RtpSocket
                    if (object.ReferenceEquals(RtpSocket, null).Equals(false)) RtpSocket.Close();

                    RtpSocket = RtcpSocket = null;
                }

                //Remove the end points
                LocalRtp = LocalRtcp = RemoteRtp = RemoteRtcp = null;

                //Why erase stats?
                //m_FirstPacketReceived = DateTime.MinValue;

                //m_FirstPacketSent = DateTime.MinValue;
            }

            //Usually called when a ssrc collision occurs 

            /// <summary>
            /// Resets the RemoteSynchronizationSourceIdentifier and packet counters values.
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal void ResetState()
            {
                //if (RemoteSynchronizationSourceIdentifier.HasValue) RemoteSynchronizationSourceIdentifier = null;// default(int);

                RemoteSynchronizationSourceIdentifier = 0;

                //Set all to 0
                RfcRtpBytesSent = RtpPacketsSent = RtpBytesSent = RtcpPacketsSent = 
                    RtcpBytesSent = RtpPacketsReceived = RtpBytesRecieved = RtcpBytesRecieved = 
                        RtcpPacketsReceived = m_FailedRtcpTransmissions = m_FailedRtpTransmissions = m_FailedRtcpReceptions = m_FailedRtpReceptions = 0;
            }

            /// <summary>
            /// Disposes the TransportContext and all underlying resources.
            /// </summary>
            public override void Dispose()
            {
                if (IsDisposed) return;

                base.Dispose();

                //If the instance should dispose
                if (ShouldDispose)
                {
                    //Disconnect sockets
                    DisconnectSockets();

                    //Remove references to the context memory and the application context
                    ContextMemory = null;

                    ApplicationContext = null;                    
                }
            }

            #endregion

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            IEnumerable<Socket> Common.ISocketReference.GetReferencedSockets()
            {
                if (IsDisposed) yield break;

                if (object.ReferenceEquals(RtpSocket, null).Equals(false))
                {
                    yield return RtpSocket;

                    if (RtpSocket.ProtocolType == ProtocolType.Tcp || IsDuplexing) yield break;
                }

                //Todo, these may be the same sockets...
                if (object.ReferenceEquals(RtcpSocket, null).Equals(false)) yield return RtcpSocket;
            }
        }

        #endregion

        #region Events        

        /// <summary>
        /// Provides a function signature which is used to process data at a given offset and length.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public delegate void InterleavedDataHandler(object sender, byte[] data, int offset, int length);
        
        /// <summary>
        /// Provides a funtion signature which is used to process RtpPacket's
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        /// <param name="tc"></param>
        public delegate void RtpPacketHandler(object sender, RtpPacket packet = null, TransportContext tc = null);
        
        /// <summary>
        /// Provides a function signature which is used to process RtcpPacket's
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        /// <param name="tc"></param>
        public delegate void RtcpPacketHandler(object sender, RtcpPacket packet = null, TransportContext tc = null);

        /// <summary>
        /// Provides a function signature which is used to process RtpFrame's
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="frame"></param>
        /// <param name="tc"></param>
        /// <param name="final"></param>
        public delegate void RtpFrameHandler(object sender, RtpFrame frame = null, TransportContext tc = null, bool final = false);

        /// <summary>
        /// Provides a function signature which is used to provide status of the RtpClient
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void RtpClientAction(RtpClient sender, object args);

        //Todo, Determine if events for unknown ssrc and version are helpful

        //OnUnknownIdentify(object sender, IPacket packet)

        //Combine with 
        //=> bool AllocateContextForUnknownParticipants
        //To Create a new TransportContext for this participant.

        //OnUnknownVersion(object sender, IPacket packet)

        //Determine if events for loss of packets is useful and what to provide

        //If RFC3550 had a Source class then the source would be provided here, otherwise the TransportContext
        //To have another type of class PacketLossInformation would require more logic for no purpose at this level.
        //Furthermore PacketLossInformation can be determined from the TransportContext.
        //Thus the TransportContext should have a structure which can represent this information.

        //OnPacketLoss(object sender, PacketLossInformation info)

        /// <summary>
        /// Raised when non rtp protocol data is recieved.
        /// </summary>
        public event InterleavedDataHandler OutOfBandData;

        //Todo, Add and Remove pattern would allow ThreadEvents to be automatically turned on and off based on amount of connected handlers..

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
        /// Raised when a complete RtpFrame was changed due to a packet being added, removed or updated.
        /// </summary>
        public event RtpFrameHandler RtpFrameChanged;

        #region Table of Participants

        //6.3.2 Initialization....
        //I will do no such thing, I will no have no table when no table is required such as be the case when no expectance is put on the identity of the recipient.
        //All such packets should be considered equal unless specifically negioated by means provided by an alternate mechanism such as SDP or the RTP-Info header and is beyond the scope of the RtpClient implementation [based on my interpretation that is.]
        //I could go on and on about this but I think we all get the point

        //In most cases this table would only contain 1 entry anyway...

        #endregion

        //6.3.3 Rtp or Rtcp
        protected internal virtual void HandleIncomingRtcpPacket(object rtpClient, RtcpPacket packet, RtpClient.TransportContext transportContext = null)
        {
            //Determine if the packet can be handled
            if (false.Equals(RtcpEnabled) || IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;

            int packetLength = packet.Length;            

            //Cache the ssrc of the packet's sender.
            int partyId = packet.SynchronizationSourceIdentifier,
                packetVersion = packet.Version;

            //See if there is a context for the remote party. (Allows 0)
            transportContext = transportContext ?? GetContextBySourceId(partyId);

            //Todo, if PersistIncomingRtcpReports reports then Clone the packet and store it on the context.

            //Raise an event for the rtcp packet received.
            OnRtcpPacketReceieved(packet, transportContext);

            //Compressed or no ssrc
            if (packet.IsCompressed || packetLength < Common.Binary.BytesPerLong || false.Equals(HandleIncomingRtcpPackets))
            {
                //Return
                return;
            }//else if there is a context and the version doesn't match.
            else if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) && false.Equals(transportContext.Version.Equals(packetVersion)))
            {
                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtcpPacket Invalid Version, Found =>" + packetVersion + ", Expected =>" + transportContext.Version);

                //Do nothing else.
                return;
            }

            //Only if the packet was not addressed to a unique party with the id of 0 and there is a null context or the context is in discovery.
            if (false.Equals(partyId.Equals(0)) && false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) && transportContext.InDiscovery)                
            {
                //Cache the payloadType and blockCount
                int blockCount = packet.BlockCount;

                //Before checking the type ensure there is a party id and block count
                if (blockCount.Equals(0))
                {
                    //If there was a context and the remote party has not yet been identified.
                    if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) &&
                        transportContext.InDiscovery &&
                        transportContext.Version.Equals(packetVersion))
                    {
                        //Identify the remote party by this id.
                        transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                        //Check packet loss...

                        Media.Common.ILoggingExtensions.Log(Logger, ToString() + "@HandleIncomingRtcpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + "RR blockId=" + partyId);                                                
                    }
                    
                    return;
                }

                //Check the type because there is at least 1 block
                int payloadType = packet.PayloadType;

                if(payloadType == ReceiversReport.PayloadType)
                {
                    //Create a wrapper around the packet to access the ReportBlocks
                    using (ReceiversReport rr = new ReceiversReport(packet, false))
                    {
                        //Iterate each contained ReportBlock
                        foreach (IReportBlock reportBlock in rr)
                        {
                            int blockId = reportBlock.BlockIdentifier;

                            if (blockCount.Equals(0)) continue;

                            //Attempt to obtain a context by the identifier in the report block
                            transportContext = GetContextBySourceId(blockId);

                            //If there was a context and the remote party has not yet been identified.
                            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) && 
                                transportContext.InDiscovery &&
                                transportContext.Version.Equals(packetVersion))
                            {
                                //Identify the remote party by this id.
                                transportContext.RemoteSynchronizationSourceIdentifier = blockId;

                                //Check packet loss...

                                Media.Common.ILoggingExtensions.Log(Logger, ToString() + "@HandleIncomingRtcpPacket Set RemoteSynchronizationSourceIdentifier @ RR " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + "RR blockId=" + blockId);

                                //Stop looking for a context.
                                break;
                            }
                        }
                    }
                }
                else if (payloadType == GoodbyeReport.PayloadType) //The GoodbyeReport report from a remote party
                {
                    //Create a wrapper around the packet to access the source list
                    using (GoodbyeReport gb = new GoodbyeReport(packet, false))
                    {
                        using (RFC3550.SourceList sourceList = gb.GetSourceList())
                        {
                            //Iterate each party leaving
                            foreach (int party in sourceList)
                            {
                                if (party.Equals(0)) continue;

                                //Attempt to obtain a context by the identifier in the report block
                                transportContext = GetContextBySourceId(party);

                                //If there was a context
                                if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) && 
                                    transportContext.Version.Equals(packetVersion))
                                {
                                    //Send report now if possible.
                                    bool reportsSent = SendReports(transportContext);                                    

                                    Media.Common.ILoggingExtensions.Log(Logger, ToString() + "@HandleIncomingRtcpPacket Recieved Goodbye @ " + transportContext.SynchronizationSourceIdentifier + " from=" + partyId + " reportSent=" + reportsSent);

                                    transportContext.ResetRtpValidationCounters(transportContext.m_SequenceNumber);

                                    //Stop looking for a context.
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (payloadType == SendersReport.PayloadType) //The senders report from a remote party                    
                {
                    //Create a wrapper around the packet to access the ReportBlocks
                    using (SendersReport sr = new SendersReport(packet, false))
                    {
                        //Iterate each contained ReportBlock
                        foreach (IReportBlock reportBlock in sr)
                        {
                            int blockId = reportBlock.BlockIdentifier;

                            if (blockCount.Equals(0)) continue;

                            //Attempt to obtain a context by the identifier in the report block
                            transportContext = GetContextBySourceId(blockId);

                            //If there was a context and the remote party has not yet been identified.
                            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) && 
                                transportContext.Version == packetVersion &&
                                transportContext.RemoteSynchronizationSourceIdentifier.Equals(0))
                            {
                                
                                //Identify the remote party by this id.
                                transportContext.RemoteSynchronizationSourceIdentifier = blockId;

                                //Check packet loss...

                                Media.Common.ILoggingExtensions.Log(Logger, ToString() + "@HandleIncomingRtcpPacket Set RemoteSynchronizationSourceIdentifier @ SR " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier + "RR blockId=" + blockId);

                                //Stop looking for a context.
                                break;
                            }
                        }
                    }
                }                
            }

            //Handle Goodbyes with a positive blockcount but no  sourcelist...?

        //NoContext:

            //If no transportContext could be found
            if (Common.IDisposedExtensions.IsNullOrDisposed(transportContext))
            {
                //Attempt to see if this was a rtp packet by using the RtpPayloadType
                int rtpPayloadType = packet.Header.First16Bits.RtpPayloadType;

                //Todo, make constant 13 Silence...
                if (rtpPayloadType.Equals(13) || false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext = GetContextByPayloadType(rtpPayloadType))))
                {
                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtcpPacket - Incoming RtcpPacket actually was Rtp. Ssrc= " + partyId + " Type=" + rtpPayloadType + " Len=" + packet.Length);

                    //Raise an event for the 'RtpPacket' received. 
                    //Todo Use the existing reference / memory of the RtcpPacket) or provide an implicit way to cast 
                    using (RtpPacket rtp = new RtpPacket(packet.Prepare().ToArray(), 0))
                    {
                        OnRtpPacketReceieved(rtp, transportContext);
                    }

                    //Don't do anything else
                    return;
                }

                //Could attempt to find the context in which this packet is trying to communicate with if we had a RemoteEndPoint indicating where the packet was received from...
                //Cannot find a context because there may be more then one context which has not yet been identified
                //Could attempt to check that there is only 1 context and then if not yet valid assign the identity...
                //if(TransportContexts.Count == 1) ...

                //Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtcpPacket - No Context for packet " + partyId + "@" + packet.PayloadType);

                //Could create context for partyId here.

                //Todo, OutOfBand(RtcpPacket)

                //Don't do anything else.
                return;
            }
            
            //There is a transportContext

            //If there is a collision in the unique identifiers
            if (transportContext.SynchronizationSourceIdentifier.Equals(partyId))
            {
                //Handle it.
                HandleIdentityCollision(transportContext);
            }

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
            transportContext.m_InactiveTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

            //Don't worry about overflow
            unchecked
            {
                //Increment packets received for the valid context.
                ++transportContext.RtcpPacketsReceived;

                //Keep track of the the bytes sent in the context
                transportContext.RtcpBytesRecieved += packet.Length;

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

            //if (goodBye && packet.BlockCount > 0) transportContext.m_SendInterval = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan; //Then never send reports again?

            #endregion       

            //OnRtcpPacketProcessed(this, packet, transportContext);
        }

        protected internal virtual void HandleIdentityCollision(TransportContext transportContext)
        {

            if (transportContext == null) throw new ArgumentNullException("transportContext");

            if (transportContext.IsDisposed) throw new ObjectDisposedException("transportContext");

            Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleCollision - Ssrc=" + transportContext.SynchronizationSourceIdentifier + " - RSsrc=" + transportContext.RemoteSynchronizationSourceIdentifier);

            //Send a goodbye and indicate why.
            SendGoodbye(transportContext, System.Text.Encoding.UTF8.GetBytes("ssrc"));

            //Assign a new random ssrc which is not equal to the remote parties.
            //Noting that you could use the same ssrc +/-N here also or a base from the number of parties etc.

            //This may deserve an event, 'OnCollision'

            do transportContext.SynchronizationSourceIdentifier = RFC3550.Random32(transportContext.SynchronizationSourceIdentifier);
            while (transportContext.SynchronizationSourceIdentifier == transportContext.RemoteSynchronizationSourceIdentifier);

            //Reset counters from this point forward
            transportContext.ResetState();

            //reset counters?
        }

        //Reserved, possibly would be used for seperate finalization
        protected internal virtual void HandleFrameChange(object /*RtpClient*/ sender, RtpFrame frame = null, TransportContext tc = null)
        {
            //TransportContext context = tc ?? GetContextByPayloadType(frame.PayloadTypeByte);
            ////If there is a context
            //if (context == null) return;
        }

        /// <summary>
        /// Updates counters and fires a FrameChanged event if required.
        /// </summary>
        /// <param name="sender">The object which raised the event</param>
        /// <param name="packet">The RtpPacket to handle</param>
        protected internal virtual void HandleIncomingRtpPacket(object/*RtpClient*/ sender, RtpPacket packet, RtpClient.TransportContext transportContext = null)
        {
            //sender maybe not this
            //if (false == this.Equals(sender)) return;

            //Determine if the incoming packet CAN be handled
            if (false.Equals(RtpEnabled) || IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;

            //Should check right here incase the packet was incorrectly mapped to rtp from rtcp by checking the payload type to be in the reserved range for rtcp conflict avoidance.

            //Get the transportContext for the packet by the sourceId then by the payload type of the RtpPacket, not the SSRC alone because it may have not yet been defined.
            //Noting that this is not per RFC3550
            //This is because this implementation allows for the value 0 to be used as a discovery mechanism.

            //Notes that sometimes multiple payload types are being sent from a sender, in such cases the transportContext may incorrectly be selected here.
            //E.g.if payloadTypes of two contexts overlap and the ssrc is not well defined for each.
            transportContext =  transportContext ?? GetContextForPacket(packet);

            //Raise the event for the packet. (Could add valid here)
            OnRtpPacketReceieved(packet, transportContext);            

            //If the client shouldn't handle the packet then return.            
            if (false.Equals(HandleIncomingRtpPackets) || packet.IsCompressed)
            {
                return;
            }

            #region TransportContext Handles Packet

            //If the context is still null
            if (Common.IDisposedExtensions.IsNullOrDisposed(transportContext))
            {
                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Unaddressed RTP Packet " + packet.SynchronizationSourceIdentifier + " PT =" + packet.PayloadType + " len =" + packet.Length);

                //Do nothing else.
                return;
            }

            #region Unused [Handles packet version validation

            //Already checked in ValidatePacket
            //int packetVersion = packet.Version;

            ////If the version doesn't match.
            //if (transportContext.Version != packetVersion)
            //{
            //    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Invalid Version, Found =>" + packetVersion + ", Expected =>" + transportContext.Version);

            //    //Do nothing else.
            //    return;
            //}

            #endregion

            #region Unused [Handles PayloadType validation]

            //Cache the payload type of the packet being handled
            //int payloadType = packet.PayloadType;

            //Checked in ValidatePacketAndUpdateSequenceNumber
            ////If the packet payload type has not been defined in the MediaDescription
            //if (false == transportContext.MediaDescription.PayloadTypes.Contains(payloadType))
            //{
            //    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket RTP Packet PT =" + packet.PayloadType + " is not in Media Description. (" + transportContext.MediaDescription.MediaDescriptionLine + ") ");

            //    //Do nothing else.
            //    return;
            //}

            #endregion

            //Cache the ssrc
            int partyId = packet.SynchronizationSourceIdentifier;               

            //Check for a collision
            if (partyId.Equals(transportContext.SynchronizationSourceIdentifier))
            {
                //Handle it
                HandleIdentityCollision(transportContext);
            }

            #region Unused [Handles TransportContext.InDiscovery When TransportContext.IsValid is false]

            //////If the context is NOT valid AND the context is in discovery mode of the remote party
            ////if (false == transportContext.IsValid && transportContext.InDiscovery)
            ////{
            ////    //Assign an id at this time
            ////    transportContext.RemoteSynchronizationSourceIdentifier = partyId;
            ////}

            #endregion

            #region Unused [Handles remote identify switching]

            //////If the packet was not addressed to the context AND the context is valid
            ////if (partyId != transportContext.RemoteSynchronizationSourceIdentifier
            ////    &&
            ////    transportContext.IsValid)
            ////{

            ////    //Reset the state if not discovering
            ////    if (false == transportContext.InDiscovery)
            ////    {
            ////        Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket SSRC Mismatch @ " + transportContext.SynchronizationSourceIdentifier + "<->" + transportContext.RemoteSynchronizationSourceIdentifier + "||" + partyId + ". ResetState");

            ////        transportContext.ResetState();
            ////    }

            ////    //Assign the id of the remote party.
            ////    transportContext.RemoteSynchronizationSourceIdentifier = partyId;

            ////    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Set RemoteSynchronizationSourceIdentifier @ " + transportContext.SynchronizationSourceIdentifier + " to=" + transportContext.RemoteSynchronizationSourceIdentifier);
            ////}    

            #endregion           

            //Don't worry about overflow.
            unchecked
            {
                int packetLength = packet.Length;

                //if (packetLength <= RtpHeader.Length)
                //{
                //    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Header Only " +
                //             " Context seq=" + transportContext.RecieveSequenceNumber +
                //             " Packet pt= (" + transportContext.MediaDescription.MediaType + ")" + packet.PayloadType +
                //            " seq=" + packet.SequenceNumber +
                //            " len= " + packetLength);

                //    //return;
                //}

                int pt = packet.PayloadType;

                //Todo, offer a reason via out why the packet is not valid to reduce overhead of checking from the Validate function

                //If the packet sequence number is not valid
                if (false.Equals(transportContext.ValidatePacketAndUpdateSequenceNumber(packet)))
                {

                    //If the pt is not in the media description this is out of band data and the packet was already evented.
                    if (false.Equals(transportContext.AllowsMultiplePayloadTypes) && false.Equals(transportContext.MediaDescription.PayloadTypes.Contains(pt))) return;
                    //If duplicate packets are not allowed
                    //else if (false.Equals(transportContext.AllowDuplicatePackets) && transportContext.RecieveSequenceNumber >= packet.SequenceNumber) return;
                    //If the context does not allow out of order packets return.
                    else if (false.Equals(transportContext.AllowOutOfOrderPackets)) return;

                    //Increment for a failed reception, possibly rename
                    ++transportContext.m_FailedRtpReceptions;

                    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket Failed Reception " +
                             "(= " + transportContext.m_FailedRtpReceptions + ") @" + transportContext.SynchronizationSourceIdentifier +
                             " Context seq=" + transportContext.RecieveSequenceNumber +
                             " Packet pt= (" + transportContext.MediaDescription.MediaType + ")" + pt +
                            " seq=" + packet.SequenceNumber +
                            " len= " + packetLength);

                    //Todo, Event for discontuity... (see above notes on could)
                }
                else ++transportContext.ValidRtpPacketsReceived; //Increase the amount of valid rtp packets recieved when ValidatePacketAndUpdateSequenceNumber is true

                #region Identity and version Seperation

                //If IsValid then ensure the RemoteSynchronizationSourceIdentifier is set.
                //if (transportContext.InDiscovery || transportContext.IsValid && false.Equals(partyId.Equals(transportContext.RemoteSynchronizationSourceIdentifier)))
                //{
                //    //If not yet set, set the remote id 
                //    if (transportContext.RemoteSynchronizationSourceIdentifier.Equals(0))
                //    {
                //        transportContext.RemoteSynchronizationSourceIdentifier = partyId;

                //        Media.Common.ILoggingExtensions.Log(Logger, "HandleIncomingRtpPacket@ transportContext.IsValid, RemoteSynchronizationSourceIdentifier Initialized = " + partyId + "MediaType = " + transportContext.MediaDescription.MediaType);

                //        //SendReports(transportContext, true);
                //    }
                //    else
                //    {
                //        //Todo, this allows multiple different 'sending' identities to a single receiver.

                //        //There are multiple uses for this such as stream layering.
                //        //This would be more compliant by using the CSRC field and the result would be such that all complaint Mixers would be able to be used.
                //        //You would also more easily be able to extract the layered streams and provide the seperated streams.

                //        //switch (transportContext.MaximumRecievers)
                //        //{
                //        //    case 0:
                //        //        {
                //        //            AddContext(new TransportContext(transportContext.DataChannel, transportContext.ControlChannel, transportContext.SynchronizationSourceIdentifier, transportContext.IsRtcpEnabled, partyId, 0)
                //        //            {
                //        //                MediaDescription = transportContext.MediaDescription
                //        //            }, false, false, false, false);

                //        //            transportContext.Recievers.Add(partyId);

                //        //            Media.Common.ILoggingExtensions.Log(Logger, "HandleIncomingRtpPacket@ Added New Context Party = " + partyId + "PT = " + packet.PayloadType);

                //        //            break;
                //        //        }
                //        //    default:
                //        //        {
                //        //            if (transportContext.Recievers.Count < transportContext.MaximumRecievers)
                //        //            {
                //        //                goto case 0;
                //        //            }

                //        //            Media.Common.ILoggingExtensions.Log(Logger, "HandleIncomingRtpPacket@ Too Many Contexts (" + TransportContexts.Count + ")-(" + transportContext.MaximumRecievers + ") Party = " + partyId + "PT = " + packet.PayloadType);

                //        //            //Send a goodbye for now. //System.Text.Encoding.UTF8.GetBytes("b\\a\\ndwi\\d\\th")
                //        //            SendGoodbye(transportContext, System.Text.Encoding.UTF8.GetBytes("limit"), partyId, true, null, false);

                //        //            break;

                //        //        }
                //        //}

                //        Media.Common.ILoggingExtensions.Log(Logger, "HandleIncomingRtpPacket@ transportContext.IsValid@(" + transportContext.RemoteSynchronizationSourceIdentifier + "), Unknown Sender = " + partyId + " PT = " + packet.PayloadType);

                //        //SendGoodbye(transportContext, System.Text.Encoding.UTF8.GetBytes("ssrc"), transportContext.SynchronizationSourceIdentifier, true, null, false);

                //        //Option AssumeIdentity / CoalesceIdentities
                //        if(partyId.Equals(0)) packet.SynchronizationSourceIdentifier = transportContext.RemoteSynchronizationSourceIdentifier;
                //    }
                //}

                #endregion

                //Increment RtpPacketsReceived for the context relating to the packet.
                ++transportContext.RtpPacketsReceived;               

                //The counters for the bytes will now be be updated (without the 12 octets of the header)
                //increment the counters (Only use the Payload.Count per the RFC) (new Erratta Submitted)
                //http://www.rfc-editor.org/errata_search.php?rfc=3550
                transportContext.RtpBytesRecieved += packet.Payload.Count;

                //Please note due to the 'consensus' achieved for this standard (RFC 1889 / RFC3550 / RFC3551)
                //The counters for the rtp bytes sent are specifically counted only to reveal average data rate...
                //A Senders report may only indicate the values which are allowed in the rfc. (Probably so middle boxes can't be detected)
                //Otherwise it's not complaint but no one will figure out how or why since its not supposed to effect annex calulcations...
                //Additionally the jitter caluclations would be messed up in most cases where a sourcelist or padding is used because it doesn't take those values into account
                //This implemenation doesn't suffer from this non-sense.

                transportContext.RfcRtpBytesRecieved += packetLength - (packet.Header.Size + packet.HeaderOctets + packet.PaddingOctets);

                //Set the time when the first RtpPacket was received if required
                if (transportContext.m_FirstPacketReceived == DateTime.MinValue) transportContext.m_FirstPacketReceived = packet.Created;

                //Update the SequenceNumber and Timestamp and calulcate Inter-Arrival (Mark the context as active)
                transportContext.UpdateJitterAndTimestamp(packet);

                //Set the last rtp in after inter-arrival has been calculated.
                transportContext.m_LastRtpIn = packet.Created;

                //If the instance does not handle frame changed events then return
                if (false.Equals(HandleFrameChanges)) return;

                #region HandleFrameChanges

                //Note
                //If the ssrc changed mid stream but the data is still somehow relevent to the lastFrame or currentFrame
                //Then the ssrc of the packet must be changed or the ssrc of the frame must be changed before adding the packet.

                //Todo, Add yet another Frame to increase the chances that late packets arrive

                //NextFrame, CurrentFrame, LastFrame

                int packetTimestamp = packet.Timestamp;

                //If a CurrentFrame was not allocated
                if (Common.IDisposedExtensions.IsNullOrDisposed(transportContext.CurrentFrame))
                {
                    //make a frame with the copy of the packet
                    transportContext.CurrentFrame = new RtpFrame(packet.Clone(true, true, true, true, false, true))
                    {
                        AllowsMultiplePayloadTypes = transportContext.AllowsMultiplePayloadTypes
                    };

                    //The LastFrame changed
                     OnRtpFrameChanged(transportContext.CurrentFrame, transportContext);

                    //Nothing else to do
                    return;

                }//Check to see if the frame belongs to the last frame
                else if (false.Equals(IDisposedExtensions.IsNullOrDisposed(transportContext.LastFrame)) 
                    &&
                    packetTimestamp.Equals(transportContext.LastFrame.Timestamp))
                {
                    //If the packet was added to the frame
                    if (transportContext.LastFrame.TryAdd(packet.Clone(true, true, true, true, false, true), transportContext.AllowDuplicatePackets))
                    {
                        bool final = transportContext.LastFrame.Count >= transportContext.LastFrame.MaxPackets;

                        //The LastFrame changed so fire an event
                        OnRtpFrameChanged(transportContext.LastFrame, transportContext, final);

                        //Backup of frames in LastFrame.
                        if (final)
                        {
                            Common.ILoggingExtensions.Log(Logger, InternalId + "HandleFrameChanges => LastFrame Disposing @ " + transportContext.LastFrame.Count);

                            transportContext.LastFrame.Dispose();

                            transportContext.LastFrame = null;
                        }

                    }
                    else
                    {
                        //Could jump to case log
                        Common.ILoggingExtensions.Log(Logger, InternalId + "HandleFrameChanges => transportContext.LastFrame @ TryAdd failed, (" + packet.PayloadType + ") RecieveSequenceNumber = " + transportContext.RecieveSequenceNumber + ", PacketSequenceNumber = " + packet.SequenceNumber + " Timestamp = " + packetTimestamp + "=>" + packetTimestamp.Equals(transportContext.LastFrame.Timestamp) + "[" + transportContext.LastFrame.LowestSequenceNumber + "," + transportContext.LastFrame.HighestSequenceNumber + "]" + ". HasMarker = " + transportContext.LastFrame.HasMarker);
                    }
                    
                    //Nothing else to do
                    return;

                }//Check to see if the frame belongs to a new frame
                else if (false.Equals(IDisposedExtensions.IsNullOrDisposed(transportContext.CurrentFrame)) 
                    && 
                    false.Equals(packetTimestamp.Equals(transportContext.CurrentFrame.Timestamp)))
                {
                    //////We already set to the value of packet.SequenceNumber in UpdateSequenceNumber.
                    //////Before cycling packets check the packets sequence number.
                    //int pseq = transportContext.RecieveSequenceNumber; //packet.SequenceNumber;

                    ////Only allow newer timestamps but wrapping should be allowed.
                    ////When the timestamp is lower and the sequence number is not in order this is a re-ordered packet. (needs to correctly check for wrapping sequence numbers)
                    //if (packetTimestamp < transportContext.CurrentFrame.Timestamp)
                    //{
                    //    //Could jump to case log
                    //    Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleFrameChanges Ignored SequenceNumber " + pseq + " @ " + packetTimestamp + ". Current Timestamp =" + transportContext.CurrentFrame.Timestamp + ", Current LowestSequenceNumber = " + transportContext.CurrentFrame.LowestSequenceNumber);

                    //    return;
                    //}

                    //Dispose the last frame, it's going out of scope.
                    if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(transportContext.LastFrame)))
                    {
                        //Indicate the frame is going out of scope
                        OnRtpFrameChanged(transportContext.LastFrame, transportContext, true);

                        transportContext.LastFrame.Dispose();

                        transportContext.LastFrame = null;

                        //Move the frame to be finalized
                        //transportContext.Holding = transportContext.LastFrame;
                    }

                    //Move the current frame to the LastFrame
                    transportContext.LastFrame = transportContext.CurrentFrame;

                    //make a frame with the copy of the packet
                    transportContext.CurrentFrame = new RtpFrame(packet.Clone(true, true, true, true, false, true))
                    {
                        AllowsMultiplePayloadTypes = transportContext.AllowsMultiplePayloadTypes
                    };

                    //The current frame changed
                    OnRtpFrameChanged(transportContext.CurrentFrame, transportContext);

                    return;
                }//Check to see if the frame belongs to the current frame
                else if (false.Equals(IDisposedExtensions.IsNullOrDisposed(transportContext.CurrentFrame)) &&
                   packetTimestamp.Equals(transportContext.CurrentFrame.Timestamp))
                {
                    //If the packet was added to the frame
                    if (transportContext.CurrentFrame.TryAdd(packet.Clone(true, true, true, true, false, true), transportContext.AllowDuplicatePackets))
                    {
                        bool final = transportContext.CurrentFrame.Count >= transportContext.CurrentFrame.MaxPackets;

                        //The CurrentFrame changed
                        OnRtpFrameChanged(transportContext.CurrentFrame, transportContext, final);

                        //Backup of frames in CurrentFrame
                        if (final)
                        {
                            Common.ILoggingExtensions.Log(Logger, InternalId + "HandleFrameChanges => CurrentFrame Disposing @ " + transportContext.CurrentFrame.Count);

                            transportContext.CurrentFrame.Dispose();

                            transportContext.CurrentFrame = null;
                        }
                    }
                    else
                    {
                        //Could jump to case log but would need to know current frame, nameof won't work here because the indirection
                        Common.ILoggingExtensions.Log(Logger, InternalId + "HandleFrameChanges => transportContext.CurrentFrame@TryAdd failed, (" + packet.PayloadType + ") RecieveSequenceNumber = " + transportContext.RecieveSequenceNumber + ", PacketSequenceNumber = " + packet.SequenceNumber + ", Timestamp = " + packetTimestamp + "=>" + packetTimestamp.Equals(transportContext.CurrentFrame.Timestamp) + "[" + transportContext.CurrentFrame.LowestSequenceNumber + "," + transportContext.CurrentFrame.HighestSequenceNumber + "]" + ". HasMarker = " + transportContext.CurrentFrame.HasMarker);
                    }

                    return;
                }

                Media.Common.ILoggingExtensions.Log(Logger, InternalId + "HandleIncomingRtpPacket HandleFrameChanged ("+packet.PayloadType+") @ " + packetTimestamp + " Does not belong to any frame.");

                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Handles the logic of updating counters for the packet sent if <see cref="OutgoingRtpPacketEventsEnabled"/> is true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        protected internal virtual void HandleOutgoingRtpPacket(object sender, RtpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false.Equals(HandleOutgoingRtpPackets) || IDisposedExtensions.IsNullOrDisposed(packet) || false.Equals(packet.Transferred.HasValue)) return;

            #region TransportContext Handles Packet

            TransportContext transportContext = tc ?? GetContextForPacket(packet);

            if (Media.Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) return;

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

                //Just update the sequence number for the packet being sent
                transportContext.m_LastSentSequenceNumber = (ushort)packet.SequenceNumber;

                //If the packet was in sequence (does not really have to be checked, the jitter and timestamp should be updated anyway...
                //if (transportContext.UpdateSequenceNumber(packet.SequenceNumber))
                //{
                    //Calculate inter-arrival and mark the context as active
                    transportContext.UpdateJitterAndTimestamp(packet);
                //}

                //Store the time the last RtpPacket was sent.
                transportContext.m_LastRtpOut = sent;

                //Attempt to raise the event
                OnRtpPacketSent(packet, transportContext);
            }

            #endregion
        }

        /// <summary>
        /// Handles the logic of updating counters for the packet sent if <see cref="OutgoingRtcpPacketEventsEnabled"/> is true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>OutgoingRtcpPacketEventsEnabled
        protected internal virtual void HandleOutgoingRtcpPacket(object sender, RtcpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || IDisposedExtensions.IsNullOrDisposed(packet) || false.Equals(HandleOutgoingRtcpPackets) || false.Equals(packet.Transferred.HasValue)) return;

            #region TransportContext Handles Packet

            TransportContext transportContext = tc ?? GetContextForPacket(packet);

            //if there is no context there is nothing to do.
            if (Common.IDisposedExtensions.IsNullOrDisposed(transportContext)) return;

            unchecked
            {
                //Update the counters for the amount of bytes in the RtcpPacket including the header and any padding.
                transportContext.RtcpBytesSent += packet.Length;

                //Update the amount of packets sent
                ++transportContext.RtcpPacketsSent;

                //Mark the context as active immediately.
                transportContext.m_InactiveTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

                //Get the time the packet was sent
                DateTime sent = packet.Transferred.Value;

                //Store the last time a RtcpPacket was sent
                transportContext.m_LastRtcpOut = sent;

                //Set the time the first packet was sent.
                if (transportContext.m_FirstPacketSent == DateTime.MinValue) transportContext.m_FirstPacketSent = sent;

                //Attempt to raise the event
                OnRtcpPacketSent(packet, transportContext);
            }

            //Backoff based on ConverganceTime?

            #endregion
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal void OnOutOfBandData(byte[] data, int offset, int length)
        {
            if (IsDisposed) return;

            InterleavedDataHandler action = OutOfBandData;

            if (object.ReferenceEquals(action, null) || data == null || length.Equals(Common.Binary.Zero)) return;

            if (m_ThreadEvents)
            {
                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(null, new Common.Classes.PacketBase(data, offset, length, true, true), true, true));

                m_EventReady.Set();

                return;
            }

            foreach (InterleavedDataHandler handler in action.GetInvocationList())
            {
                try { handler(this, data, offset, length); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); return; }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelOutOfBandData(Media.Common.Classes.PacketBase packet = null)
        {
            if (IsDisposed) return;

            InterleavedDataHandler action = OutOfBandData;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet) || packet.Length.Equals(Common.Binary.LongZero)) return;

            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
                try { ((InterleavedDataHandler)(d))(this, packet.Data, 0, (int)packet.Length); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); }                
            });

            //Don't have to waste cycles on this thread calling dispose...
            //Todo, check if ShouldDispose was set to false in event..
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(packet))) Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        /// <summary>
        /// Raises the RtpPacket Handler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal void OnRtpPacketReceieved(RtpPacket packet, TransportContext tc = null)
        {
            if (IsDisposed || false.Equals(IncomingRtpPacketEventsEnabled)) return;

            RtpPacketHandler action = RtpPacketReceieved;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            bool shouldDispose = packet.ShouldDispose;

            if (shouldDispose) SetShouldDispose(packet, false, false);

            if (m_ThreadEvents)
            {
                //Use a clone of the packet and data into a new reference so it can stay alive for the event.
                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(tc, packet.Clone(true, true, true, true, false, true), false, true));

                m_EventReady.Set();

                //todo, should call dispose is finalizer was missed...
                SetShouldDispose(packet, true, false);

                return;
            }

            foreach (RtpPacketHandler handler in action.GetInvocationList())
            {
                if (packet.IsDisposed || IsDisposed) break;
                try { handler(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); break; }
            }

            //Allow the packet to be destroyed if an event did not already change this.
            if (shouldDispose && packet.ShouldDispose.Equals(false) && false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(packet))) Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Recieving
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected internal void OnRtcpPacketReceieved(RtcpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false.Equals(IncomingRtcpPacketEventsEnabled) || IsDisposed) return;

            RtcpPacketHandler action = RtcpPacketReceieved;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            bool shouldDispose = packet.ShouldDispose;

            if (shouldDispose) SetShouldDispose(packet, false, false);

            if (m_ThreadEvents)
            {
                //Todo, only clone if ShouldDispose is true.

                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(tc, packet.Clone(true, true, false), false, true));

                m_EventReady.Set();

                //todo, should call dispose is finalizer was missed...
                SetShouldDispose(packet, true, false);

                return;
            }

            foreach (RtcpPacketHandler handler in action.GetInvocationList())
            {
                if (packet.IsDisposed || IsDisposed) break;
                try { handler(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); break; }
            }

            //Allow the packet to be destroyed if an event did not already change this.
            if (shouldDispose && packet.ShouldDispose.Equals(false) && false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(packet))) Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        /// <summary>
        /// Raises the RtpFrameHandler for the given frame if FrameEvents are enabled
        /// </summary>
        /// <param name="frame">The frame to raise the RtpFrameHandler with</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void OnRtpFrameChanged(RtpFrame frame = null, TransportContext tc = null, bool final = false)
        {
            if (IsDisposed || false.Equals(FrameChangedEventsEnabled) || IsDisposed) return;            

            RtpFrameHandler action = RtpFrameChanged;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(frame) || frame.IsEmpty) return;

            bool shouldDispose = frame.ShouldDispose;

            if (shouldDispose) SetShouldDispose(frame, false, false);

            if (m_ThreadEvents)
            {
                                                                                                //new RtpFrame(frame)
                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(tc, frame, final, true));

                m_EventReady.Set();

                return;
            }

            foreach (RtpFrameHandler handler in action.GetInvocationList())
            {
                if (IDisposedExtensions.IsNullOrDisposed(frame) || IsDisposed) break;
                try { handler(this, frame, tc, final); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); break; }
            }

            //On final events set ShouldDispose to true, do not call Dispose
            if (final && shouldDispose && Common.IDisposedExtensions.IsNullOrDisposed(frame).Equals(false) && frame.ShouldDispose.Equals(false)) SetShouldDispose(frame, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelRtpFrameChanged(RtpFrame frame = null, TransportContext tc = null, bool final = false)
        {
            if (IsDisposed || false.Equals(FrameChangedEventsEnabled) || IsDisposed) return;

            RtpFrameHandler action = RtpFrameChanged;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(frame) || frame.IsEmpty) return;

            bool shouldDispose = frame.ShouldDispose;

            if (shouldDispose) SetShouldDispose(frame, false, false);

            //RtpFrameHandler would need the cast up front.
            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(frame) || IsDisposed) return;
                try { ((RtpFrameHandler)(d))(this, frame, tc, final); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); }                
            });

            //On final events set ShouldDispose to true, do not call Dispose
            if (final && shouldDispose && Common.IDisposedExtensions.IsNullOrDisposed(frame).Equals(false) && frame.ShouldDispose.Equals(false)) SetShouldDispose(frame, true, false);
        }

        //IPacket overload could reduce code but would cost time to check type.

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelRtpPacketRecieved(RtpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false.Equals(HandleIncomingRtpPackets) || IsDisposed) return;

            RtpPacketHandler action = RtpPacketReceieved;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //RtpFrameHandler would need the cast up front.
            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
                try { ((RtpPacketHandler)(d))(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); }
            });

            //Allow the packet to be disposed, do not call dispose now.
            //Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelRtpPacketSent(RtpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false == HandleOutgoingRtpPackets || IsDisposed) return;

            RtpPacketHandler action = RtpPacketSent;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //RtpFrameHandler would need the cast up front.
            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
                try { ((RtpPacketHandler)(d))(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex);}               
            });
            
            //allow packet to be disposed...
            //Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelRtcpPacketRecieved(RtcpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false.Equals(HandleIncomingRtcpPackets) || IsDisposed) return;

            RtcpPacketHandler action = RtcpPacketReceieved;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //RtpFrameHandler would need the cast up front.
            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
                try { ((RtcpPacketHandler)(d))(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); }
                //finally { packet.Dispose(); }
            });

            //Allow the packet to be disposed, do not call dispose now.
            //Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void ParallelRtcpPacketSent(RtcpPacket packet = null, TransportContext tc = null)
        {
            if (IsDisposed || false == HandleOutgoingRtcpPackets || IsDisposed) return;

            RtcpPacketHandler action = RtcpPacketSent;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //RtpFrameHandler would need the cast up front.
            ParallelEnumerable.ForAll(action.GetInvocationList().AsParallel(), (d) =>
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
                try { ((RtcpPacketHandler)(d))(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); }
                //finally { packet.Dispose(); }
            });

            //Allow the packet to be disposed, do not call dispose now.
            //Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }
            

        /// <summary>
        /// Raises the RtpPacket Handler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void OnRtpPacketSent(RtpPacket packet, TransportContext tc = null)
        {
            if (IsDisposed || false == OutgoingRtpPacketEventsEnabled || IsDisposed) return;

            RtpPacketHandler action = RtpPacketSent;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;

            //bool shouldDispose = packet.ShouldDispose;

            //if (shouldDispose) SetShouldDispose(packet, false, false);

            if (m_ThreadEvents)
            {
                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(tc, packet, false, true));

                m_EventReady.Set();

                return;
            }

            foreach (RtpPacketHandler handler in action.GetInvocationList())
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) break;
                try { handler(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); break; }
            }

            //if(shouldDispose && false == packet.IsDisposed) Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        /// <summary>
        /// Raises the RtcpPacketHandler for Sending
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void OnRtcpPacketSent(RtcpPacket packet, TransportContext tc = null)
        {
            if (IsDisposed || false == OutgoingRtcpPacketEventsEnabled || IsDisposed) return;

            RtcpPacketHandler action = RtcpPacketSent;

            if (object.ReferenceEquals(action, null) || IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //bool shouldDispose = packet.ShouldDispose;

            //if (shouldDispose) SetShouldDispose(packet, false, false);

            if (m_ThreadEvents)
            {
                m_EventData.Enqueue(new Tuple<TransportContext, Common.BaseDisposable, bool, bool>(tc, packet, false, true));

                return;
            }

            foreach (RtcpPacketHandler handler in action.GetInvocationList())
            {
                if (IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) break;
                try { handler(this, packet, tc); }
                catch (Exception ex) { Common.ILoggingExtensions.LogException(Logger, ex); break; }
            }

            //if (shouldDispose) Common.BaseDisposable.SetShouldDispose(packet, true, false);
        }

        //Frame sent.

        #endregion

        #region Properties

        //Todo, determine if packets should just not be enqueued anymore
        //Should also apply for Rtcp.       

        /// <summary>
        /// Used in applications to determine send thresholds.
        /// </summary>
        public int MaximumOutgoingPackets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected set;
        }

        /// <summary>
        /// Gets the number of RtpPacket instances queued to be sent.
        /// </summary>
        public int OutgoingRtpPacketCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return m_OutgoingRtpPackets.Count;
            }
        }

        /// <summary>
        /// Gets or sets a value which indicates if the socket operations for sending will use the IList overloads.
        /// </summary>
        public bool IListSockets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return m_IListSockets;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                //Todo, the objects may be in use on the curent call
                //if (m_ThreadEvents)
                //{

                //}

                m_IListSockets = value;
            }
        }

        /// <summary>
        /// Gets or sets a value which indicates if events will be threaded or not.
        /// If threading is enabled the call will block until the event thread has started.
        /// </summary>
        public bool ThreadEvents //Enable
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ThreadEvents; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (false.Equals(IsActive)) return;

                if (value.Equals(m_ThreadEvents)) return;

                //Update the value.
                m_ThreadEvents = value;

                if (value == true)
                {
                    if (m_EventThread == null || EventsStarted.Equals(DateTime.MinValue))
                    {
                        //Create the event thread
                        m_EventThread = new Thread(new ThreadStart(HandleEvents), Common.Extensions.Thread.ThreadExtensions.MinimumStackSize);

                        //Configure
                        ConfigureThread(m_EventThread); //should pass name and logging.

                        //Assign name
                        m_EventThread.Name = "RtpClient-Events-" + InternalId;

                        //Start highest
                        m_EventThread.Priority = ThreadPriority.Highest;

                        //Start thread
                        m_EventThread.Start();
                    }
                    
                    //Wait for the start while the value was not changed and the thread is not started.
                    while (m_ThreadEvents && EventsStarted == DateTime.MinValue && false.Equals(m_EventThread == null)) m_EventReady.Wait(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);
                }
                else
                {
                    //Not started
                    EventsStarted = DateTime.MinValue;

                    //Set lowest priority on event thread.
                    m_EventThread.Priority = ThreadPriority.Lowest;

                    //Abort and free the thread.
                    Common.Extensions.Thread.ThreadExtensions.AbortAndFree(ref m_EventThread);

                    //Handle any remaining events so the packets in Queue don't get disposed...
                    //while (m_ThreadEvents == false && EventsStarted == DateTime.MinValue && false == m_EventData.IsEmpty)
                    //{
                    //    HandleEvent();

                    //    //m_EventReady.Wait(m_EventReady.SpinCount >> 2);
                    //}

                    //Ensure Cleared
                    //if (m_ThreadEvents == false && EventsStarted == DateTime.MinValue) m_EventData.Clear();
                }
            }
        }

        public Action<Thread> ConfigureThread { get; set; }

        /// <summary>
        /// The maximum amount of bandwidth Rtcp can utilize (of the overall bandwidth available to the RtpClient) during reports
        /// </summary>
        public double AverageMaximumRtcpBandwidthPercentage
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

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
        /// Gets or sets a value which allows the OnRtpPacketEvent to be raised.
        /// </summary>
        public bool IncomingRtpPacketEventsEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the OnRtcpPacketEvent to be raised.
        /// </summary>
        public bool IncomingRtcpPacketEventsEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the OnRtpPacketSent to be raised.
        /// </summary>
        public bool OutgoingRtpPacketEventsEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the OnRtcpPacketSent to be raised.
        /// </summary>
        public bool OutgoingRtcpPacketEventsEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the instance to handle any incoming RtpPackets
        /// </summary>
        public bool HandleIncomingRtpPackets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the instance to handle any incoming RtcpPackets
        /// </summary>
        public bool HandleIncomingRtcpPackets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the instance to handle any outgoing RtpPackets
        /// </summary>
        public bool HandleOutgoingRtpPackets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the instance to handle any outgoing RtcpPackets
        /// </summary>
        public bool HandleOutgoingRtcpPackets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which prevents <see cref="RtpFrameChanged"/> from being fired.
        /// </summary>
        public bool FrameChangedEventsEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets a value which allows the instance to create a RtpFrame based on the incoming rtp packets.
        /// </summary>
        public bool HandleFrameChanges
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets the value will be used as the CName when creating RtcpReports
        /// </summary>
        public string ClientName
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get;
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set;
        }

        /// <summary>
        /// Gets or sets the list of additional items which will be sent with the SourceDescriptionReport if AverageRtcpBandwidthExceeded is not exceeded.
        /// </summary>
        public readonly List<SourceDescriptionReport.SourceDescriptionItem> AdditionalSourceDescriptionItems = new List<SourceDescriptionReport.SourceDescriptionItem>();

        /// <summary>
        /// Gets a value indicating if the RtpClient is not disposed and the WorkerThread is alive.
        /// </summary>
        public virtual bool IsActive
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsDisposed.Equals(false) &&
                    Started.Equals(DateTime.MinValue).Equals(false) && 
                    object.ReferenceEquals(m_WorkerThread, null).Equals(false) &&
                    (m_WorkerThread.IsAlive || m_StopRequested.Equals(false));
            }
        }

        /// <summary>
        /// Gets a value which indicates if any underlying <see cref="RtpClient.TransportContext"/> owned by this RtpClient instance utilizes Rtcp.
        /// </summary>
        public bool RtcpEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return TransportContexts.Any(c => c.IsRtcpEnabled); }
        }

        /// <summary>
        /// Gets a value which indicates if any underlying <see cref="RtpClient.TransportContext"/> owned by this RtpClient instance utilizes Rtcp.
        /// </summary>
        public bool RtpEnabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return TransportContexts.Any(c => c.IsRtpEnabled); }
        }

        /// <summary>
        /// Indicates if the amount of bandwith currently utilized for Rtcp reporting has exceeded the amount of bandwidth allowed by the <see cref="AverageMaximumRtcpBandwidthPercentage"/> property.
        /// </summary>
        public bool AverageRtcpBandwidthExceeded
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                if (false.Equals(RtcpEnabled) || IsDisposed) return true;

                //If disposed no limit is imposed do not check

                double averageMaximumRtcpBandwidthPercentage = AverageMaximumRtcpBandwidthPercentage;

                if (averageMaximumRtcpBandwidthPercentage.Equals(Common.Binary.DoubleZero)) return false;

                int amountOfContexts = TransportContexts.Count;

                if (amountOfContexts.Equals(Common.Binary.Zero)) return true;

                //Obtain the summation of the total bytes sent over the amount of context's
                long totalReceived = TotalBytesReceieved;

                if (totalReceived.Equals(Common.Binary.LongZero)) return false;

                long totalRtcp = TotalRtcpBytesSent + TotalRtcpBytesReceieved;

                if (totalRtcp.Equals(Common.Binary.LongZero)) return false;

                return totalRtcp >= totalReceived / averageMaximumRtcpBandwidthPercentage;
            }
        }

        #region Bandwidth and Uptime and Counters

        /// <summary>
        /// The Date and Time the RtpClient was Connected
        /// </summary>
        public DateTime Started { get; private set; }

        public DateTime EventsStarted { get; private set; }

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

        #region Constructor

        static RtpClient()
        {
            //Todo make static with port. static readonly int DefaultRtpTransportPort. (see if Rtcp also has one)
            if (false == UriParser.IsKnownScheme(RtpProtcolScheme)) UriParser.Register(new HttpStyleUriParser(), RtpProtcolScheme, 9670);
        }

        RtpClient(bool shouldDispose = true)
            :base(shouldDispose)
        {
            AverageMaximumRtcpBandwidthPercentage = DefaultReportInterval.TotalSeconds;

            ConfigureThread = ConfigureRtpThread;
        }

        /// <summary>
        /// Assigns the events necessary for operation and creates or assigns memory to use as well as inactivtyTimout.
        /// </summary>
        /// <param name="memory">The optional memory segment to use</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(Common.MemorySegment memory = null, bool incomingPacketEventsEnabled = true, bool frameChangedEventsEnabled = true, bool outgoingPacketEvents = true, bool shouldDispose = true)
            : this(shouldDispose)
        {
            if (memory == null)
            {
                //Determine a good size based on the MTU (this should cover most applications)
                //Need an IP or the default IP to ensure the MTU Matches, use 1600 because 1500 is unaligned.
                m_Buffer = new Common.MemorySegment(1600);
            }
            else
            {
                m_Buffer = memory;

                if (m_Buffer.Count < RtpHeader.Length) throw new ArgumentOutOfRangeException("memory", "memory.Count must contain enough space for a RtpHeader");
            }

            //RtpPacketReceieved += new RtpPacketHandler(HandleIncomingRtpPacket);
            //RtcpPacketReceieved += new RtcpPacketHandler(HandleIncomingRtcpPacket);
            //RtpPacketSent += new RtpPacketHandler(HandleOutgoingRtpPacket);
            //RtcpPacketSent += new RtcpPacketHandler(HandleOutgoingRtcpPacket);
            //InterleavedData += new InterleaveHandler(HandleInterleavedData);

            //Allow events to be raised
            HandleIncomingRtpPackets = HandleIncomingRtcpPackets = IncomingRtpPacketEventsEnabled = IncomingRtcpPacketEventsEnabled = incomingPacketEventsEnabled;

            //Fire events for packets received and Allow events to be raised
            HandleOutgoingRtpPackets = HandleOutgoingRtcpPackets = OutgoingRtpPacketEventsEnabled = OutgoingRtcpPacketEventsEnabled = outgoingPacketEvents;

            //Handle frame changes and Allow frame change events to be raised
            HandleFrameChanges = FrameChangedEventsEnabled = frameChangedEventsEnabled;
        }

        /// <summary>
        /// Creates a RtpClient instance using the given array as a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use</param>
        /// <param name="offset">The offset to start using the buffer at</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(byte[] buffer, int offset = 0, bool incomingPacketEventsEnabled = true, bool frameChangedEventsEnabled = true, bool outgoingPacketEvents = true, bool shouldDispose = true)
            : this(new Common.MemorySegment(buffer, offset), incomingPacketEventsEnabled, frameChangedEventsEnabled, outgoingPacketEvents, shouldDispose) { }

        /// <summary>
        /// Creates a RtpClient instance using the given array as a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use</param>
        /// <param name="offset">The offset to start using the buffer at</param>
        /// <param name="count">The amount of bytes to use in the buffer</param>
        /// <param name="incomingPacketEventsEnabled"><see cref="IncomingPacketEventsEnabled"/></param>
        /// <param name="frameChangedEventsEnabled"><see cref="FrameChangedEventsEnabled"/></param>
        public RtpClient(byte[] buffer, int offset, int count, bool incomingPacketEventsEnabled = true, bool outgoingPacketEvents = true, bool frameChangedEventsEnabled = true, bool shouldDispose = true) 
            : this(new Common.MemorySegment(buffer, offset, count), incomingPacketEventsEnabled, frameChangedEventsEnabled, outgoingPacketEvents, shouldDispose) { }

        #endregion

        #region Overloads               

        public override string ToString()
        {
            return string.Join(((char)Common.ASCII.HyphenSign).ToString(), base.ToString(), InternalId);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Calls <see cref="Deactivate"/> and disposes all contained <see cref="RtpClient.TransportContext"/>.
        /// Stops the raising of any events.
        /// Removes the Logger
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (false.Equals(disposing) || false.Equals(ShouldDispose)) return;

            base.Dispose(ShouldDispose);

            if (false.Equals(IsDisposed)) return;

            DisposeAndClearTransportContexts();

            //Stop raising events
            RtpPacketSent = null;
            RtcpPacketSent = null;
            RtpPacketReceieved = null;
            RtcpPacketReceieved = null;
            OutOfBandData = null;

            //Send abort signal to all threads contained.
            //Todo, maybe offer Delegate AbortDelegate..
            Media.Common.IThreadReferenceExtensions.AbortAndFreeAll(this);

            //Empty packet buffers
            m_OutgoingRtpPackets.Clear();

            //m_OutgoingRtpPackets = null;

            m_OutgoingRtcpPackets.Clear();

            //m_OutgoingRtcpPackets = null;

            ThreadEvents = false;

            m_EventData.Clear();

            //Allow a waiting thread to exit
            m_EventReady.Set();

            m_EventReady.Reset();

            AdditionalSourceDescriptionItems.Clear();

            ClientName = null;

            //Remove the buffer
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(m_Buffer)))
            {
                m_Buffer.Dispose();

                m_Buffer = null;
            }

            Media.Common.ILoggingExtensions.Log(Logger, GetType().Name + "("+ ToString() + ")@Dipose - Complete");

            //Unset the logger
            Logger = null;
        }

        #endregion

        IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads()
        {
            IEnumerable<System.Threading.Thread> threads = System.Linq.Enumerable.Empty<System.Threading.Thread>();

            if (IsDisposed) return threads;

            if (IsActive) threads = threads.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(m_WorkerThread));

            if (m_ThreadEvents) threads = threads.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(m_EventThread));

            return threads;
        }
    }
}
